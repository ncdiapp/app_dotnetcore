/**
 * Batch create Stored Procedure App APIs from a selected data source.
 * SP list = Wijmo FlexGrid; parameters = FlexGridDetail (ExpandSingle).
 */
import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { FlexGridDetail } from '@mescius/wijmo.react.grid.detail';
import { CollectionView } from '@mescius/wijmo';
import * as wjGrid from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { integrationService } from '../../webapi/integrationsvc';
import { adminSvc } from '../../webapi/adminsvc';

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
  Description?: string;
  Parameters: SpParamRow[];
};

type Props = {
  open: boolean;
  dataSources: DataSourceItem[];
  defaultDataSourceId: number | null;
  onClose: () => void;
  onCreated: () => void;
};

const CreateStoredProcedureApisModal: React.FC<Props> = ({
  open,
  dataSources: dataSourcesProp,
  defaultDataSourceId,
  onClose,
  onCreated,
}) => {
  const { theme } = useTheme();
  const errorMessage = useErrorMessage();
  const [dataSources, setDataSources] = useState<DataSourceItem[]>(dataSourcesProp ?? []);
  const [dataSourceId, setDataSourceId] = useState<number | null>(defaultDataSourceId);
  const [loadingDs, setLoadingDs] = useState(false);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [rows, setRows] = useState<SpPreviewRow[]>([]);
  const flexRef = useRef<wjGrid.FlexGrid | null>(null);

  const collectionView = useMemo(() => new CollectionView<SpPreviewRow>(rows), [rows]);

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
    setRows([]);
    setIsFullscreen(false);

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
  }, [open, dataSourcesProp, defaultDataSourceId, normalizeDataSources, errorMessage]);

  const loadProcedures = useCallback(async () => {
    if (dataSourceId == null) {
      errorMessage.showError('Select a data source first.');
      return;
    }
    setLoading(true);
    try {
      const list = await integrationService.listStoredProceduresForApiBuilder(dataSourceId);
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
          Description: p.Description ?? p.description ?? '',
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
      setRows(mapped);
    } catch (e) {
      errorMessage.showError(e instanceof Error ? e.message : String(e));
    } finally {
      setLoading(false);
    }
  }, [dataSourceId, errorMessage]);

  const applyIncludeDefaults = useCallback((item: SpPreviewRow): SpPreviewRow => {
    const included = !!item.Include;
    const defaultCode = item.DefaultActionCode || `AppSp_${item.SpName}`;
    return {
      ...item,
      Include: included,
      DefaultActionCode: defaultCode,
      // Checked: show default (or keep user edit). Unchecked: hide API name.
      ActionCode: included
        ? (item.ActionCode?.trim() ? item.ActionCode.trim() : defaultCode)
        : '',
    };
  }, []);

  const syncRowFromGridItem = useCallback(
    (item: SpPreviewRow) => {
      if (!item?.SpName) return;
      const next = applyIncludeDefaults(item);
      // Keep grid item in sync when we fill default ActionCode.
      item.Include = next.Include;
      item.ActionCode = next.ActionCode;
      item.DefaultActionCode = next.DefaultActionCode;
      setRows((prev) =>
        prev.map((r) =>
          r.SpName === item.SpName
            ? {
                ...r,
                Include: next.Include,
                ActionCode: next.ActionCode,
                DefaultActionCode: next.DefaultActionCode,
                Description: item.Description,
                Parameters: item.Parameters,
              }
            : r,
        ),
      );
    },
    [applyIncludeDefaults],
  );

  const allSelected = rows.length > 0 && rows.every((r) => r.Include);
  const toggleSelectAll = useCallback(() => {
    const next = !allSelected;
    setRows((prev) =>
      prev.map((r) =>
        applyIncludeDefaults({
          ...r,
          Include: next,
          ActionCode: next ? r.DefaultActionCode || '' : '',
        }),
      ),
    );
  }, [allSelected, applyIncludeDefaults]);

  const handleGenerate = useCallback(async () => {
    if (dataSourceId == null) return;
    const items = rows.filter((r) => r.Include);
    if (!items.length) {
      errorMessage.showError('Select at least one stored procedure.');
      return;
    }
    setSaving(true);
    try {
      const result = await integrationService.batchCreateStoredProcedureApis({
        DataSourceId: dataSourceId,
        Items: items.map((r) => ({
          Schema: r.Schema,
          SpName: r.SpName,
          ActionCode: r.ActionCode,
          Description: r.Description,
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
    }
  }, [dataSourceId, rows, errorMessage, onCreated, onClose]);

  const detailTemplate = useCallback(
    (ctx: any) => {
      const item = ctx?.item as SpPreviewRow | undefined;
      if (!item) return null;
      const params = item.Parameters ?? [];
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
                      <input
                        className={`h-7 w-full max-w-[280px] px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
                        value={p.DefaultValue ?? ''}
                        placeholder={p.HasDefault ? '(use SP default)' : ''}
                        onChange={(e) => {
                          const val = e.target.value;
                          const name = p.Name ?? '';
                          p.DefaultValue = val;
                          setRows((prev) =>
                            prev.map((r) => {
                              if (r.SpName !== item.SpName) return r;
                              return {
                                ...r,
                                Parameters: r.Parameters.map((x) =>
                                  (x.Name ?? '') === name ? { ...x, DefaultValue: val } : x,
                                ),
                              };
                            }),
                          );
                        }}
                      />
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
            disabled={loadingDs}
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
          <button
            type="button"
            disabled={loading || loadingDs || dataSourceId == null}
            className={`px-3 py-1.5 text-sm rounded-[4px] border ${theme.button_default} disabled:opacity-60`}
            onClick={loadProcedures}
          >
            {loading ? 'Loading...' : 'Load SPs'}
          </button>
        </div>

        <div className={`px-3 py-1.5 text-xs border-b flex items-center gap-2 ${theme.label}`}>
          {rows.length > 0 && (
            <button
              type="button"
              className={`px-2 py-1 rounded-[4px] border ${theme.button_default}`}
              title={allSelected ? 'Unselect all' : 'Select all'}
              onClick={toggleSelectAll}
            >
              {allSelected ? 'Unselect all' : 'Select all'}
            </button>
          )}
          <span>
            Expand a row to edit parameters. Included: {includedCount} / {rows.length}
          </span>
          <span className="ml-auto">
            Tip: Generate auto-runs read-only SPs to store JsonSampleData / return columns; skips INSERT/UPDATE/DELETE/EXEC…
          </span>
        </div>

        <div className="h-1 flex-auto min-h-0 overflow-hidden px-3 py-2">
          {rows.length === 0 ? (
            <div className={`text-xs py-8 text-center ${theme.label}`}>
              {loading ? 'Loading stored procedures…' : 'Load SPs to begin.'}
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
              <FlexGridColumn binding="Include" header="Include" width={70} />
              <FlexGridColumn binding="Schema" header="Schema" width={90} isReadOnly />
              <FlexGridColumn binding="SpName" header="SP Name" width={160} isReadOnly />
              <FlexGridColumn binding="ActionCode" header="API Name" width={220} />
              <FlexGridColumn binding="Description" header="Description" width="*" />
            </FlexGrid>
          )}
        </div>

        <div className="px-3 py-2 border-t flex justify-end gap-2">
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
            disabled={saving || loading}
            onClick={handleGenerate}
          >
            <i className="fa-solid fa-check" aria-hidden />
            {saving ? 'Generating...' : 'Generate APIs'}
          </button>
        </div>
      </div>
    </div>
  );
};

export default CreateStoredProcedureApisModal;
