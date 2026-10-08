/**
 * Batch create Stored Procedure App APIs from a selected data source.
 * SP list = Wijmo FlexGrid; parameters = FlexGridDetail (ExpandSingle).
 */
import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridDetail } from '@mescius/wijmo.react.grid.detail';
import { CollectionView } from '@mescius/wijmo';
import * as wjGrid from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { integrationService } from '../../webapi/integrationsvc';
import { adminSvc } from '../../webapi/adminsvc';
import type { RootState } from '../../redux/store';

type DataSourceItem = { Id: number; Display?: string; DataSourceName?: string };

type SpParamRow = {
  Name?: string;
  Type?: string;
  Direction?: string;
  MaxLength?: number | null;
  Ordinal?: number;
  HasDefault?: boolean;
  DefaultValue?: string | null;
};

type SpPreviewRow = {
  Include: boolean;
  Schema?: string;
  SpName: string;
  FullName?: string;
  /** Editable API name when Include is checked. */
  ActionCode: string;
  /** Suggested default for ActionCode (filled on Include). */
  DefaultActionCode: string;
  /** SP usage hint for generator only (not API Description). */
  Usage?: string;
  Parameters: SpParamRow[];
};

type Props = {
  open: boolean;
  dataSources: DataSourceItem[];
  defaultDataSourceId: number | null;
  onClose: () => void;
  onCreated: () => void;
};

/** Local input so DefaultValue edits do not setRows / rebind the parent FlexGrid (which collapses detail). */
const ParamDefaultInput: React.FC<{
  param: SpParamRow;
  inputClassName: string;
}> = ({ param, inputClassName }) => {
  const [val, setVal] = useState(param.DefaultValue ?? '');
  return (
    <input
      className={inputClassName}
      value={val}
      placeholder={param.HasDefault ? '(use SP default)' : ''}
      onChange={(e) => {
        const next = e.target.value;
        setVal(next);
        param.DefaultValue = next;
      }}
    />
  );
};

const CreateStoredProcedureApisModal: React.FC<Props> = ({
  open,
  dataSources: dataSourcesProp,
  defaultDataSourceId,
  onClose,
  onCreated,
}) => {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const errorMessage = useErrorMessage();
  const isAiConfigured = useSelector(
    (s: RootState) => !!s.userSession?.userContext?.IsAiConfigured,
  );
  const [dataSources, setDataSources] = useState<DataSourceItem[]>(dataSourcesProp ?? []);
  const [dataSourceId, setDataSourceId] = useState<number | null>(defaultDataSourceId);
  const [loadingDs, setLoadingDs] = useState(false);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [generateAiDescription, setGenerateAiDescription] = useState(true);
  const [captureSampleOnGenerate, setCaptureSampleOnGenerate] = useState(false);
  /** Bumps header counters without replacing CollectionView (avoids detail collapse). */
  const [uiTick, setUiTick] = useState(0);
  const flexRef = useRef<wjGrid.FlexGrid | null>(null);
  const collectionView = useMemo(() => new CollectionView<SpPreviewRow>([]), []);

  const getRows = useCallback(
    () => (collectionView.sourceCollection as SpPreviewRow[]) ?? [],
    [collectionView],
  );

  const replaceRows = useCallback(
    (list: SpPreviewRow[]) => {
      collectionView.sourceCollection = list;
      collectionView.refresh();
      setUiTick((t) => t + 1);
    },
    [collectionView],
  );

  const bumpUi = useCallback(() => setUiTick((t) => t + 1), []);

  const normalizeDataSources = useCallback((list: any[]): DataSourceItem[] => {
    return (Array.isArray(list) ? list : [])
      .map((ds) => {
        const id = Number(ds?.Id ?? ds?.id);
        if (!id || id === 2147483647) return null;
        const name = ds.DataSourceName ?? ds.dataSourceName ?? ds.Display ?? ds.display ?? String(id);
        return {
          Id: id,
          DataSourceName: name,
          Display: `${name} (${id})`,
        } as DataSourceItem;
      })
      .filter(Boolean) as DataSourceItem[];
  }, []);

  useEffect(() => {
    if (!open) return;

    let cancelled = false;
    replaceRows([]);
    setIsFullscreen(false);
    setGenerateAiDescription(isAiConfigured);
    setCaptureSampleOnGenerate(false);

    const loadDs = async () => {
      setLoadingDs(true);
      try {
        let list = normalizeDataSources(dataSourcesProp ?? []);
        if (!list.length) {
          const raw = await adminSvc.getDataSourceRegisterList(false);
          list = normalizeDataSources(raw);
          if (!list.length) {
            const rawAll = await adminSvc.retrieveAllAppDataSourceRegisterExDto();
            list = normalizeDataSources(rawAll);
          }
        }
        if (cancelled) return;
        setDataSources(list);
        const preferred =
          (defaultDataSourceId != null && list.some((d) => d.Id === defaultDataSourceId)
            ? defaultDataSourceId
            : null) ?? list[0]?.Id ?? null;
        setDataSourceId(preferred);
      } catch (e) {
        if (!cancelled) {
          errorMessage.showError(e instanceof Error ? e.message : String(e));
          setDataSources([]);
          setDataSourceId(null);
        }
      } finally {
        if (!cancelled) setLoadingDs(false);
      }
    };

    void loadDs();
    return () => {
      cancelled = true;
    };
  }, [open, dataSourcesProp, defaultDataSourceId, normalizeDataSources, errorMessage, isAiConfigured, replaceRows]);

  useEffect(() => {
    if (!open) return;
    if (dataSourceId == null) {
      replaceRows([]);
      return;
    }
    let cancelled = false;
    (async () => {
      setLoading(true);
      replaceRows([]);
      try {
        const list = await integrationService.listStoredProceduresForApiBuilder(dataSourceId);
        if (cancelled) return;
        const mapped: SpPreviewRow[] = list.map((p: any) => {
          const suggested =
            p.SuggestedActionCode ?? p.suggestedActionCode ?? `AppSp_${p.Name ?? p.name}`;
          return {
            Include: false,
            Schema: p.Schema ?? p.schema,
            SpName: p.Name ?? p.name,
            FullName: p.FullName ?? p.fullName,
            ActionCode: '',
            DefaultActionCode: suggested,
            Usage: p.Usage ?? p.usage ?? p.Description ?? p.description ?? '',
            Parameters: (p.Parameters ?? p.parameters ?? []).map((x: any) => ({
              Name: x.Name ?? x.name,
              Type: x.Type ?? x.type,
              Direction: x.Direction ?? x.direction,
              MaxLength: x.MaxLength ?? x.maxLength,
              Ordinal: x.Ordinal ?? x.ordinal,
              HasDefault: x.HasDefault ?? x.hasDefault,
              DefaultValue: x.DefaultValue ?? x.defaultValue,
            })),
          };
        });
        replaceRows(mapped);
      } catch (e) {
        if (!cancelled) errorMessage.showError(e instanceof Error ? e.message : String(e));
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [open, dataSourceId, errorMessage, replaceRows]);

  const applyIncludeDefaults = useCallback((item: SpPreviewRow): void => {
    const included = !!item.Include;
    const defaultCode = item.DefaultActionCode || `AppSp_${item.SpName}`;
    item.Include = included;
    item.DefaultActionCode = defaultCode;
    // Checked: show default (or keep user edit). Unchecked: hide API name.
    item.ActionCode = included
      ? (item.ActionCode?.trim() ? item.ActionCode.trim() : defaultCode)
      : '';
  }, []);

  const syncRowFromGridItem = useCallback(
    (item: SpPreviewRow) => {
      if (!item?.SpName) return;
      applyIncludeDefaults(item);
      bumpUi();
    },
    [applyIncludeDefaults, bumpUi],
  );

  const rowsSnapshot = getRows();
  // uiTick ensures header re-reads Include flags after in-place edits
  void uiTick;
  const allSelected = rowsSnapshot.length > 0 && rowsSnapshot.every((r) => r.Include);
  const setAllIncluded = useCallback((checked: boolean) => {
    getRows().forEach((r) => {
      r.Include = checked;
      r.ActionCode = checked ? r.DefaultActionCode || '' : '';
      applyIncludeDefaults(r);
    });
    collectionView.refresh();
    flexRef.current?.invalidate();
    bumpUi();
  }, [getRows, applyIncludeDefaults, collectionView, bumpUi]);

  const handleGenerate = useCallback(async () => {
    if (dataSourceId == null) return;
    const items = getRows().filter((r) => r.Include);
    if (!items.length) {
      errorMessage.showError('Select at least one stored procedure.');
      return;
    }
    setSaving(true);
    dispatch(setIsBusy());
    try {
      const result = await integrationService.batchCreateStoredProcedureApis({
        DataSourceId: dataSourceId,
        GenerateAiDescription: isAiConfigured && generateAiDescription,
        CaptureSampleOnGenerate: captureSampleOnGenerate,
        Items: items.map((r) => ({
          Schema: r.Schema,
          SpName: r.SpName,
          ActionCode: r.ActionCode,
          Parameters: r.Parameters,
        })),
      });
      const warnings = result?.ValidationResult?.Items?.filter((i: any) => i.ItemType === 2) ?? [];
      warnings.forEach((w: any) => {
        if (w.LocalizedMessage) errorMessage.showWarning(w.LocalizedMessage);
      });
      if (result?.ValidationResult?.HasErrors) {
        const errs = result.ValidationResult.Items?.filter((i: any) => i.ItemType === 1) ?? [];
        errs.forEach((e: any) => errorMessage.showError(e.LocalizedMessage ?? 'Create failed'));
        return;
      }
      {
        const n = result?.Object?.createdCount ?? items.length;
        const captured = result?.Object?.sampleCaptured;
        const skipped = result?.Object?.sampleSkipped;
        const sampleNote =
          typeof captured === 'number'
            ? ` Samples: ${captured} captured${typeof skipped === 'number' && skipped > 0 ? `, ${skipped} skipped (not read-only)` : ''}.`
            : '';
        errorMessage.showInfo(`Created ${n} Stored Procedure API(s).${sampleNote}`, true);
      }
      onCreated();
      onClose();
    } catch (e) {
      errorMessage.showError(e instanceof Error ? e.message : String(e));
    } finally {
      setSaving(false);
      dispatch(setIsNotBusy());
    }
  }, [dataSourceId, getRows, errorMessage, onCreated, onClose, isAiConfigured, generateAiDescription, captureSampleOnGenerate, dispatch]);

  const detailTemplate = useCallback(
    (ctx: any) => {
      const item = ctx?.item as SpPreviewRow | undefined;
      if (!item) return null;
      const params = item.Parameters ?? [];
      const inputClass = `h-7 w-full max-w-[280px] px-2 text-xs border rounded-[4px] ${theme.inputBox}`;
      return (
        <div className={`pl-[100px] pr-3 py-2 ${theme.mainContentSection}`}>
          <div className={`text-xs mb-1.5 ${theme.label}`}>
            Parameters for <span className="font-semibold">{item.FullName ?? item.SpName}</span>
            {' — edit DefaultValue before generate'}
          </div>
          {params.length === 0 ? (
            <div className={`text-xs py-2 ${theme.label}`}>No parameters.</div>
          ) : (
            <table className="w-full text-xs border-collapse">
              <thead>
                <tr className={`${theme.label} text-left`}>
                  <th className="py-1 pr-2 font-medium">Name</th>
                  <th className="py-1 pr-2 font-medium">Type</th>
                  <th className="py-1 pr-2 font-medium">Direction</th>
                  <th className="py-1 pr-2 font-medium">HasDefault</th>
                  <th className="py-1 font-medium">DefaultValue</th>
                </tr>
              </thead>
              <tbody>
                {params.map((p) => (
                  <tr key={p.Name ?? p.Ordinal} className="border-t border-black/5">
                    <td className="py-1 pr-2 align-middle">{p.Name}</td>
                    <td className="py-1 pr-2 align-middle">{p.Type}</td>
                    <td className="py-1 pr-2 align-middle">{p.Direction}</td>
                    <td className="py-1 pr-2 align-middle">{p.HasDefault ? 'Yes' : 'No'}</td>
                    <td className="py-1 align-middle">
                      <ParamDefaultInput param={p} inputClassName={inputClass} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      );
    },
    [theme],
  );

  if (!open) return null;

  const rows = getRows();
  const includedCount = rows.filter((r) => r.Include).length;

  return (
    <div className={`fixed inset-0 z-[100] bg-black/40 ${isFullscreen ? 'flex' : 'flex items-center justify-center'}`}>
      <div
        className={`flex flex-col border shadow-xl overflow-hidden ${theme.mainContentSection} ${
          isFullscreen
            ? 'w-full h-full max-w-full max-h-full rounded-none'
            : 'w-[1280px] max-w-[98vw] h-[82vh] rounded-[6px]'
        }`}
      >
        <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.title}`}>
          <div className="text-sm font-semibold">Create Stored Procedure APIs</div>
          <div className="flex items-center gap-1">
            <button
              type="button"
              className={`w-8 h-7 flex items-center justify-center rounded-[4px] ${theme.button_default}`}
              onClick={() => setIsFullscreen((v) => !v)}
              title={isFullscreen ? 'Exit full screen' : 'Full screen'}
              aria-label={isFullscreen ? 'Exit full screen' : 'Full screen'}
            >
              <i className={`fa-solid ${isFullscreen ? 'fa-compress' : 'fa-expand'}`} aria-hidden />
            </button>
            <button
              type="button"
              onClick={onClose}
              className="text-2xl leading-none w-9 h-9 flex items-center justify-center hover:opacity-70"
              title="Close"
              aria-label="Close"
            >
              &times;
            </button>
          </div>
        </div>

        <div className="px-3 py-2 flex items-center gap-2 border-b">
          <label className={`w-28 text-xs shrink-0 ${theme.label}`}>Data Source</label>
          <select
            className={`h-8 min-w-[280px] w-1 flex-auto px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
            value={dataSourceId ?? ''}
            disabled={loadingDs || saving}
            onChange={(e) => setDataSourceId(e.target.value ? Number(e.target.value) : null)}
          >
            <option value="">{loadingDs ? 'Loading data sources...' : 'Select data source...'}</option>
            {dataSources.map((ds) => (
              <option key={ds.Id} value={ds.Id}>
                {ds.Display ?? `${ds.DataSourceName ?? ds.Id} (${ds.Id})`}
              </option>
            ))}
          </select>
          {!loadingDs && dataSources.length === 0 && (
            <span className="text-xs text-red-600 shrink-0">No data sources found</span>
          )}
        </div>

        <div className={`px-3 py-1.5 text-xs border-b flex items-center gap-2 ${theme.label}`}>
          <span>
            Expand a row to edit parameters. Included: {includedCount} / {rows.length}
          </span>
        </div>

        <div className="h-1 flex-auto min-h-0 overflow-hidden px-3 py-2">
          {rows.length === 0 ? (
            <div className={`text-xs py-8 text-center ${theme.label}`}>
              {loading ? 'Loading stored procedures…' : dataSourceId == null ? 'Select a data source.' : 'No stored procedures.'}
            </div>
          ) : (
            <FlexGrid
              itemsSource={collectionView}
              autoGenerateColumns={false}
              selectionMode="Row"
              isReadOnly={false}
              headersVisibility="All"
              className="w-full h-full !border-0"
              initialized={(s: any) => {
                const flex = s?.control ?? s;
                flexRef.current = flex;
                flex.formatItem.addHandler((sender: wjGrid.FlexGrid, e: wjGrid.FormatItemEventArgs) => {
                  if (e.panel !== sender.cells) return;
                  const binding = sender.columns[e.col]?.binding;
                  if (binding !== 'ActionCode') return;
                  const item = sender.rows[e.row]?.dataItem as SpPreviewRow | undefined;
                  if (!item?.Include) {
                    e.cell.textContent = '';
                    e.cell.classList.add('wj-state-disabled');
                    e.cell.style.opacity = '0.65';
                    e.cell.title = 'Check Include to set API name';
                  } else {
                    e.cell.classList.remove('wj-state-disabled');
                    e.cell.style.opacity = '';
                    e.cell.title = 'API name (editable)';
                  }
                });
              }}
              beginningEdit={(s: any, e: any) => {
                const flex = (s?.control ?? s) as wjGrid.FlexGrid;
                const binding = flex.columns[e.col]?.binding;
                if (binding === 'ActionCode') {
                  const item = flex.rows[e.row]?.dataItem as SpPreviewRow | undefined;
                  if (!item?.Include) e.cancel = true;
                }
              }}
              cellEditEnded={(s: any, e: any) => {
                const flex = (s?.control ?? s) as wjGrid.FlexGrid;
                const row = e?.row ?? flex?.selection?.row;
                if (row == null || row < 0) return;
                const item = flex.rows[row]?.dataItem as SpPreviewRow | undefined;
                if (item) {
                  syncRowFromGridItem(item);
                  flex.invalidate();
                }
              }}
            >
              <FlexGridDetail
                detailVisibilityMode="ExpandSingle"
                isAnimated={false}
                rowHasDetail={() => true}
                template={detailTemplate}
              />
              <FlexGridColumn binding="Include" header="" width={46} allowSorting={false}>
                <FlexGridCellTemplate
                  cellType="ColumnHeader"
                  template={() => (
                    <div className="flex h-full w-full items-center justify-center">
                      <input
                        type="checkbox"
                        checked={allSelected}
                        ref={(el) => {
                          if (el) el.indeterminate = includedCount > 0 && !allSelected;
                        }}
                        title={allSelected ? 'Unselect all' : 'Select all'}
                        onClick={(e) => e.stopPropagation()}
                        onChange={(e) => setAllIncluded(e.target.checked)}
                      />
                    </div>
                  )}
                />
              </FlexGridColumn>
              <FlexGridColumn binding="Schema" header="Schema" width={90} isReadOnly />
              <FlexGridColumn binding="SpName" header="SP Name" width={160} isReadOnly />
              <FlexGridColumn binding="ActionCode" header="API Name" width={220} />
              <FlexGridColumn binding="Usage" header="Usage" width="*" isReadOnly />
            </FlexGrid>
          )}
        </div>

        <div className="px-3 py-2 border-t flex items-center justify-end gap-3">
          <div className={`flex sgap-1.5 mr-auto text-xs ${theme.label}`}>
          {isAiConfigured && (
              <label
                className="inline-flex items-center gap-1.5 mr-10"
                title="When checked, Generate writes English ActionDescription via LLM (≤500 chars)"
              >
                <input
                  type="checkbox"
                  checked={generateAiDescription}
                  onChange={(e) => setGenerateAiDescription(e.target.checked)}
                  disabled={saving}
                />
                Populate API Description with AI
              </label>
            )}
            <label
              className="inline-flex items-center gap-1.5"
              title="When checked, Generate executes read-like SPs (Get/List/…) and saves the response as JsonSampleData"
            >
              <input
                type="checkbox"
                checked={captureSampleOnGenerate}
                onChange={(e) => setCaptureSampleOnGenerate(e.target.checked)}
                disabled={saving}
              />
              Execute Read-Only SP & Save Response on Generate
            </label>
           
          </div>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] border ${theme.button_default} disabled:opacity-60 inline-flex items-center gap-1.5`}
            onClick={onClose}
            disabled={saving}
          >
            <i className="fa-solid fa-xmark" aria-hidden />
            Cancel
          </button>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] border ${theme.button_default} disabled:opacity-60 inline-flex items-center gap-1.5`}
            disabled={saving || loading || includedCount === 0}
            onClick={handleGenerate}
          >
            <i className={`fa-solid ${saving ? 'fa-spinner fa-spin' : 'fa-check'}`} aria-hidden />
            {saving
              ? generateAiDescription
                ? 'Generating (AI)…'
                : 'Generating...'
              : 'Generate APIs'}
          </button>
        </div>
      </div>
    </div>
  );
};

export default CreateStoredProcedureApisModal;
