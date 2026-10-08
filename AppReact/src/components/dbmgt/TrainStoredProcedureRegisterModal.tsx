/**
 * Batch AI Train for Stored Procedure AI Register (like SP API Generator).
 */
import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useDispatch } from 'react-redux';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridDetail } from '@mescius/wijmo.react.grid.detail';
import { CollectionView } from '@mescius/wijmo';
import * as wjGrid from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { integrationService } from '../../webapi/integrationsvc';

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
  Usage?: string;
  Parameters: SpParamRow[];
};

type Props = {
  open: boolean;
  dataSources: DataSourceItem[];
  defaultDataSourceId: number | null;
  onClose: () => void;
  onTrained: () => void;
};

const ParamDefaultInput: React.FC<{ param: SpParamRow; inputClassName: string }> = ({
  param,
  inputClassName,
}) => {
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

const TrainStoredProcedureRegisterModal: React.FC<Props> = ({
  open,
  dataSources,
  defaultDataSourceId,
  onClose,
  onTrained,
}) => {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const errorMessage = useErrorMessage();
  const [dataSourceId, setDataSourceId] = useState<number | null>(defaultDataSourceId);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [publishToAgent, setPublishToAgent] = useState(true);
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

  useEffect(() => {
    if (!open) return;
    setDataSourceId(defaultDataSourceId);
    setPublishToAgent(true);
  }, [open, defaultDataSourceId]);

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
        replaceRows(
          list.map((p: any) => ({
            Include: false,
            Schema: p.Schema ?? p.schema,
            SpName: p.Name ?? p.name,
            FullName: p.FullName ?? p.fullName,
            Usage: p.Usage ?? p.usage ?? '',
            Parameters: (p.Parameters ?? p.parameters ?? []).map((x: any) => ({
              Name: x.Name ?? x.name,
              Type: x.Type ?? x.type,
              Direction: x.Direction ?? x.direction,
              MaxLength: x.MaxLength ?? x.maxLength,
              Ordinal: x.Ordinal ?? x.ordinal,
              HasDefault: x.HasDefault ?? x.hasDefault,
              DefaultValue: x.DefaultValue ?? x.defaultValue,
            })),
          })),
        );
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

  const rowsSnapshot = getRows();
  void uiTick;
  const includedCount = rowsSnapshot.filter((r) => r.Include).length;
  const allSelected = rowsSnapshot.length > 0 && rowsSnapshot.every((r) => r.Include);

  const setAllIncluded = useCallback((checked: boolean) => {
    getRows().forEach((r) => {
      r.Include = checked;
    });
    collectionView.refresh();
    flexRef.current?.invalidate();
    setUiTick((t) => t + 1);
  }, [getRows, collectionView]);

  const handleTrain = useCallback(async () => {
    if (dataSourceId == null) return;
    const items = getRows().filter((r) => r.Include);
    if (!items.length) {
      errorMessage.showError('Select at least one stored procedure.');
      return;
    }
    setSaving(true);
    dispatch(setIsBusy());
    try {
      const result = await integrationService.batchTrainStoredProcedureRegister({
        DataSourceId: dataSourceId,
        PublishToAgent: publishToAgent,
        Items: items.map((r) => ({
          Schema: r.Schema,
          SpName: r.SpName,
          Parameters: r.Parameters,
        })),
      });
      const warnings = result?.ValidationResult?.Items?.filter((i: any) => i.ItemType === 2) ?? [];
      warnings.forEach((w: any) => {
        if (w.LocalizedMessage) errorMessage.showWarning(w.LocalizedMessage);
      });
      if (result?.ValidationResult?.HasErrors) {
        const errs = result.ValidationResult.Items?.filter((i: any) => i.ItemType === 1) ?? [];
        errs.forEach((e: any) => errorMessage.showError(e.LocalizedMessage ?? 'Train failed'));
        return;
      }
      const n = typeof result?.Object?.trainedCount === 'number' ? result.Object.trainedCount : 0;
      if (n <= 0) {
        errorMessage.showError(
          warnings.length
            ? 'No SP was saved. See warnings above.'
            : 'No SP was saved.',
        );
        return;
      }
      errorMessage.showInfo(`Saved ${n} of ${items.length} stored procedure(s) to AI Register.`, true);
      onTrained();
      onClose();
    } catch (e) {
      errorMessage.showError(e instanceof Error ? e.message : String(e));
    } finally {
      setSaving(false);
      dispatch(setIsNotBusy());
    }
  }, [dataSourceId, getRows, publishToAgent, errorMessage, dispatch, onTrained, onClose]);

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
            {' — edit DefaultValue for sample capture'}
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
                  <th className="py-1 font-medium">DefaultValue</th>
                </tr>
              </thead>
              <tbody>
                {params.map((p) => (
                  <tr key={p.Name ?? p.Ordinal} className="border-t border-black/5">
                    <td className="py-1 pr-2">{p.Name}</td>
                    <td className="py-1 pr-2">{p.Type}</td>
                    <td className="py-1 pr-2">{p.Direction}</td>
                    <td className="py-1">
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

  return (
    <div className="fixed inset-0 z-[100] bg-black/40 flex items-center justify-center">
      <div
        className={`flex flex-col border shadow-xl overflow-hidden w-[1100px] max-w-[98vw] h-[80vh] rounded-[6px] ${theme.mainContentSection}`}
      >
        <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.title}`}>
          <div className="text-sm font-semibold">Stored Procedures AI Register</div>
          <button type="button" onClick={onClose} className="text-2xl leading-none w-9 h-9 hover:opacity-70" aria-label="Close">
            &times;
          </button>
        </div>

        <div className="px-3 py-2 flex items-center gap-2 border-b">
          <label className={`w-28 text-xs shrink-0 ${theme.label}`}>Data Source</label>
          <select
            className={`h-8 min-w-[280px] w-1 flex-auto px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
            value={dataSourceId ?? ''}
            onChange={(e) => setDataSourceId(e.target.value ? Number(e.target.value) : null)}
            disabled={saving}
          >
            <option value="">Select data source...</option>
            {dataSources.map((ds) => (
              <option key={ds.Id} value={ds.Id}>
                {ds.Display ?? `${ds.DataSourceName ?? ds.Id} (${ds.Id})`}
              </option>
            ))}
          </select>
        </div>

        <div className={`px-3 py-1.5 text-xs border-b flex items-center gap-2 ${theme.label}`}>
          <span>
            AI will analyze detail (description, input, output, sample). Included: {includedCount} /{' '}
            {rowsSnapshot.length}
          </span>
        </div>

        <div className="h-1 flex-auto min-h-0 overflow-hidden px-3 py-2">
          {rowsSnapshot.length === 0 ? (
            <div className={`text-xs py-8 text-center ${theme.label}`}>
              {loading ? 'Loading…' : dataSourceId == null ? 'Select a data source.' : 'No stored procedures.'}
            </div>
          ) : (
            <FlexGrid
              itemsSource={collectionView}
              autoGenerateColumns={false}
              selectionMode="Row"
              headersVisibility="All"
              className="w-full h-full !border-0"
              initialized={(s: any) => {
                flexRef.current = s?.control ?? s;
              }}
              cellEditEnded={() => setUiTick((t) => t + 1)}
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
              <FlexGridColumn binding="SpName" header="SP Name" width={300} isReadOnly />
              <FlexGridColumn binding="Usage" header="Usage" width="*" isReadOnly />
            </FlexGrid>
          )}
        </div>

        <div className="px-3 py-2 border-t flex items-center justify-end gap-3">
          <label className={`inline-flex items-center gap-1.5 text-xs mr-auto ${theme.label}`}>
            <input
              type="checkbox"
              checked={publishToAgent}
              onChange={(e) => setPublishToAgent(e.target.checked)}
              disabled={saving}
            />
            Publish to Agent
          </label>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] border ${theme.button_default} disabled:opacity-60`}
            onClick={onClose}
            disabled={saving}
          >
            Cancel
          </button>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] border ${theme.button_default} disabled:opacity-60 inline-flex items-center gap-1.5`}
            disabled={saving || loading || includedCount === 0}
            onClick={handleTrain}
          >
            <i className={`fa-solid ${saving ? 'fa-spinner fa-spin' : 'fa-plus'}`} aria-hidden />
            {saving ? 'Registering…' : 'Register Selected SP'}
          </button>
        </div>
      </div>
    </div>
  );
};

export default TrainStoredProcedureRegisterModal;
