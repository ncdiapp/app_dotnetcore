/**
 * Stored Procedure AI Register — manage AI-trained SP catalog for agent discovery.
 */
import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useDispatch } from 'react-redux';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView, PropertyGroupDescription, SortDescription } from '@mescius/wijmo';
import * as wjGrid from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { useAlertConfirm } from '../common/AlertConfirmProvider';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { integrationService } from '../../webapi/integrationsvc';
import { adminSvc } from '../../webapi/adminsvc';
import TrainStoredProcedureRegisterModal from './TrainStoredProcedureRegisterModal';

type RegisterRow = {
  Id: number;
  DataSourceRegisterId: number;
  DataSourceName?: string;
  SchemaName?: string;
  SpName?: string;
  FullName?: string;
  Description?: string;
  InputJson?: string;
  OutputColumnsJson?: string;
  UsageText?: string;
  IsPublishedToAgent?: boolean;
  IsMissing?: boolean;
  HasAppApi?: boolean;
  AiAnalyzedAt?: string;
};

type DataSourceItem = { Id: number; Display?: string; DataSourceName?: string };

const StoredProcedureAiRegisterManagement: React.FC = () => {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { showError, showInfo, showWarning } = useErrorMessage();
  const { showConfirm } = useAlertConfirm();
  const [rows, setRows] = useState<RegisterRow[]>([]);
  const [dataSources, setDataSources] = useState<DataSourceItem[]>([]);
  const [filterDsId, setFilterDsId] = useState<number | null>(null);
  const [loading, setLoading] = useState(false);
  const [trainOpen, setTrainOpen] = useState(false);
  const [editRow, setEditRow] = useState<RegisterRow | null>(null);
  const [editDescription, setEditDescription] = useState('');
  const [editPublished, setEditPublished] = useState(true);
  const [publishDirty, setPublishDirty] = useState(false);
  const flexRef = useRef<wjGrid.FlexGrid | null>(null);
  /** Baseline IsPublishedToAgent by Id after last load/save. */
  const publishBaselineRef = useRef<Map<number, boolean>>(new Map());

  const collectionView = useMemo(() => {
    const cv = new CollectionView<RegisterRow>(rows);
    cv.groupDescriptions.clear();
    cv.sortDescriptions.clear();
    cv.sortDescriptions.push(new SortDescription('DataSourceName', true));
    cv.sortDescriptions.push(new SortDescription('SpName', true));
    cv.groupDescriptions.push(new PropertyGroupDescription('DataSourceName'));
    return cv;
  }, [rows]);

  const load = useCallback(async () => {
    setLoading(true);
    dispatch(setIsBusy());
    try {
      await integrationService.ensureStoredProcedureRegisterTable();
      const result = await integrationService.listStoredProcedureRegister(filterDsId);
      if (result?.ValidationResult?.HasErrors) {
        const errs = result.ValidationResult.Items?.filter((i: any) => i.ItemType === 1) ?? [];
        errs.forEach((e: any) => showError(e.LocalizedMessage ?? 'Load failed'));
        publishBaselineRef.current = new Map();
        setPublishDirty(false);
        setRows([]);
        return;
      }
      const items: RegisterRow[] = result?.Object?.items ?? [];
      const baseline = new Map<number, boolean>();
      items.forEach((r) => baseline.set(r.Id, !!r.IsPublishedToAgent));
      publishBaselineRef.current = baseline;
      setPublishDirty(false);
      setRows(items);
    } catch (e) {
      showError(e instanceof Error ? e.message : String(e));
    } finally {
      setLoading(false);
      dispatch(setIsNotBusy());
    }
  }, [dispatch, filterDsId, showError]);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const list = await adminSvc.retrieveAllAppDataSourceRegisterExDto();
        if (cancelled) return;
        const mapped = (Array.isArray(list) ? list : [])
          .map((ds: any) => {
            const id = Number(ds?.Id ?? ds?.id);
            if (!id || id === 2147483647) return null;
            const name = ds.DataSourceName ?? ds.dataSourceName ?? String(id);
            return { Id: id, DataSourceName: name, Display: `${name} (${id})` } as DataSourceItem;
          })
          .filter(Boolean) as DataSourceItem[];
        setDataSources(mapped);
      } catch {
        /* ignore */
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const selectedIds = useCallback(() => {
    const flex = flexRef.current;
    if (!flex) return [] as number[];
    const ids: number[] = [];
    for (let i = 0; i < flex.rows.length; i++) {
      const row = flex.rows[i];
      if (row?.isSelected && row.dataItem?.Id) ids.push(row.dataItem.Id);
    }
    if (ids.length === 0 && flex.selection?.row >= 0) {
      const item = flex.rows[flex.selection.row]?.dataItem as RegisterRow | undefined;
      if (item?.Id) ids.push(item.Id);
    }
    return ids;
  }, []);

  const handleDelete = useCallback(async () => {
    const ids = selectedIds();
    if (!ids.length) {
      showWarning('Select one or more rows to delete.');
      return;
    }
    const ok = await showConfirm(
      `Delete ${ids.length} register row(s)? This does not drop the database stored procedure.`,
      { title: 'Delete Confirmation', confirmLabel: 'Delete' },
    );
    if (!ok) return;
    dispatch(setIsBusy());
    try {
      const result = await integrationService.batchDeleteStoredProcedureRegister(ids);
      if (result?.ValidationResult?.HasErrors) {
        showError('Delete failed.');
        return;
      }
      showInfo(`Deleted ${result?.Object?.deletedCount ?? ids.length} row(s).`, true);
      await load();
    } catch (e) {
      showError(e instanceof Error ? e.message : String(e));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [selectedIds, showConfirm, showWarning, showError, showInfo, dispatch, load]);

  const collectPublishChanges = useCallback(() => {
    const source = (collectionView.sourceCollection as RegisterRow[]) ?? [];
    const changes: Array<{ Id: number; IsPublishedToAgent: boolean }> = [];
    source.forEach((r) => {
      if (!r?.Id) return;
      const current = !!r.IsPublishedToAgent;
      const baseline = publishBaselineRef.current.get(r.Id);
      if (baseline === undefined || baseline !== current) {
        changes.push({ Id: r.Id, IsPublishedToAgent: current });
      }
    });
    return changes;
  }, [collectionView]);

  const syncPublishDirty = useCallback(() => {
    setPublishDirty(collectPublishChanges().length > 0);
  }, [collectPublishChanges]);

  const handleSavePublish = useCallback(async () => {
    const changes = collectPublishChanges();
    if (!changes.length) {
      showWarning('No Published changes to save.');
      return;
    }
    dispatch(setIsBusy());
    try {
      const result = await integrationService.batchSavePublishStoredProcedureRegister(changes);
      if (result?.ValidationResult?.HasErrors) {
        showError('Save Publish failed.');
        return;
      }
      showInfo(`Saved publish for ${result?.Object?.updatedCount ?? changes.length} row(s).`, true);
      await load();
    } catch (e) {
      showError(e instanceof Error ? e.message : String(e));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [collectPublishChanges, dispatch, showWarning, showError, showInfo, load]);

  const openEdit = useCallback((row: RegisterRow) => {
    setEditRow(row);
    setEditDescription(row.Description ?? '');
    setEditPublished(!!row.IsPublishedToAgent);
  }, []);

  const saveEdit = useCallback(async () => {
    if (!editRow) return;
    dispatch(setIsBusy());
    try {
      const result = await integrationService.saveStoredProcedureRegister({
        Id: editRow.Id,
        Description: editDescription,
        IsPublishedToAgent: editPublished,
      });
      if (result?.ValidationResult?.HasErrors) {
        showError('Save failed.');
        return;
      }
      showInfo('Saved.', true);
      setEditRow(null);
      await load();
    } catch (e) {
      showError(e instanceof Error ? e.message : String(e));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [editRow, editDescription, editPublished, dispatch, showError, showInfo, load]);

  const defaultDsId = filterDsId ?? dataSources[0]?.Id ?? null;

  return (
    <div className="w-full h-full flex flex-col overflow-hidden">
      <div className="px-3 py-2 flex items-center gap-2 border-b shrink-0">
        <div className={`text-sm font-semibold mr-2 ${theme.title}`}>Stored Procedure AI Register</div>
        <label className={`text-xs ${theme.label}`}>Data Source</label>
        <select
          className={`h-8 min-w-[220px] px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
          value={filterDsId ?? ''}
          onChange={(e) => setFilterDsId(e.target.value ? Number(e.target.value) : null)}
        >
          <option value="">All</option>
          {dataSources.map((ds) => (
            <option key={ds.Id} value={ds.Id}>
              {ds.Display ?? ds.DataSourceName}
            </option>
          ))}
        </select>
        <div className="w-1 flex-auto" />
        <button
          type="button"
          className={`px-3 py-1.5 text-sm rounded-[4px] border ${theme.button_default}`}
          onClick={() => void load()}
          disabled={loading}
        >
          <i className="fa-solid fa-rotate mr-1" aria-hidden />
          Refresh
        </button>
        <button
          type="button"
          className={`px-3 py-1.5 text-sm rounded-[4px] border ${theme.button_default}`}
          onClick={() => setTrainOpen(true)}
        >
          <i className="fa-solid fa-plus mr-1" aria-hidden />
          Register SP
        </button>
        <button
          type="button"
          className={`px-3 py-1.5 text-sm rounded-[4px] border ${theme.button_default} disabled:opacity-60`}
          onClick={() => void handleSavePublish()}
          disabled={!publishDirty || loading}
          title={publishDirty ? 'Save Published checkbox changes' : 'Change a Published checkbox to enable'}
        >
          <i className="fa-solid fa-floppy-disk mr-1" aria-hidden />
          Save Publish
        </button>
        <button
          type="button"
          className={`px-3 py-1.5 text-sm rounded-[4px] border ${theme.button_default}`}
          onClick={() => void handleDelete()}
        >
          <i className="fa-solid fa-trash mr-1" aria-hidden />
          Delete
        </button>
      </div>

      <div className={`px-3 py-1 text-xs shrink-0 ${theme.label}`}>
        {loading
          ? 'Loading…'
          : `${rows.length} registered · Edit Published checkboxes then Save Publish · Used By API / Missing are read-only · Agent sees Published only`}
      </div>

      <div className="h-1 flex-auto min-h-0 px-3 pb-2">
        <FlexGrid
          itemsSource={collectionView}
          autoGenerateColumns={false}
          selectionMode="ListBox"
          headersVisibility="Column"
          showGroups={true}
          groupHeaderFormat="<span style='font-weight:600;'>{value}</span>"
          className="w-full h-full !border-0"
          initialized={(s: any) => {
            flexRef.current = s?.control ?? s;
          }}
        >
          <FlexGridColumn header="Action" width={70} isReadOnly>
            <FlexGridCellTemplate
              cellType="Cell"
              template={(cell: any) => {
                const item = cell.item as RegisterRow;
                return (
                  <button
                    type="button"
                    className="text-xs px-1 hover:opacity-70"
                    title="Edit description"
                    onClick={() => openEdit(item)}
                  >
                    <i className="fa-solid fa-pencil" aria-hidden />
                  </button>
                );
              }}
            />
          </FlexGridColumn>
          <FlexGridColumn binding="DataSourceName" header="Data Source From" width={160} isReadOnly visible={false} />
          <FlexGridColumn binding="SchemaName" header="Schema" width={80} isReadOnly />
          <FlexGridColumn binding="SpName" header="SP Name" width={250} isReadOnly />
          <FlexGridColumn binding="IsPublishedToAgent" header="Published" width={90} isReadOnly allowSorting={false}>
            <FlexGridCellTemplate
              cellType="Cell"
              template={(cell: any) => {
                const item = cell.item as RegisterRow;
                return (
                  <input
                    type="checkbox"
                    checked={!!item?.IsPublishedToAgent}
                    onClick={(ev) => ev.stopPropagation()}
                    onChange={(ev) => {
                      if (!item) return;
                      item.IsPublishedToAgent = ev.target.checked;
                      collectionView.refresh();
                      syncPublishDirty();
                    }}
                  />
                );
              }}
            />
          </FlexGridColumn>
          <FlexGridColumn binding="HasAppApi" header="Used By API" width={150} dataType="Boolean" isReadOnly />
          <FlexGridColumn binding="IsMissing" header="Missing" width={70} isReadOnly />
          <FlexGridColumn binding="Description" header="Description" width="*" isReadOnly />
        </FlexGrid>
      </div>

      <TrainStoredProcedureRegisterModal
        open={trainOpen}
        dataSources={dataSources}
        defaultDataSourceId={defaultDsId}
        onClose={() => setTrainOpen(false)}
        onTrained={() => void load()}
      />

      {editRow && (
        <div className="fixed inset-0 z-[110] bg-black/40 flex items-center justify-center">
          <div className={`w-[520px] max-w-[95vw] border rounded-[6px] shadow-xl p-3 ${theme.mainContentSection}`}>
            <div className={`text-sm font-semibold mb-2 ${theme.title}`}>
              Edit — {editRow.FullName ?? editRow.SpName}
            </div>
            <label className={`block text-xs mb-1 ${theme.label}`}>Description</label>
            <textarea
              className={`w-full h-28 px-2 py-1 text-xs border rounded-[4px] mb-2 ${theme.inputBox}`}
              value={editDescription}
              onChange={(e) => setEditDescription(e.target.value)}
            />
            <label className={`inline-flex items-center gap-1.5 text-xs mb-3 ${theme.label}`}>
              <input
                type="checkbox"
                checked={editPublished}
                onChange={(e) => setEditPublished(e.target.checked)}
              />
              IsPublishedToAgent
            </label>
            <div className="flex justify-end gap-2">
              <button
                type="button"
                className={`px-3 py-1.5 text-sm rounded-[4px] border ${theme.button_default}`}
                onClick={() => setEditRow(null)}
              >
                Cancel
              </button>
              <button
                type="button"
                className={`px-3 py-1.5 text-sm rounded-[4px] border ${theme.button_default}`}
                onClick={() => void saveEdit()}
              >
                Save
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default StoredProcedureAiRegisterManagement;
