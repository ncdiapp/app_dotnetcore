/**
 * Batch create Stored Procedure App APIs from a selected data source.
 */
import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
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
  ActionCode: string;
  Description?: string;
  CaptureSample: boolean;
  Parameters: SpParamRow[];
  Expanded?: boolean;
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
  dataSources,
  defaultDataSourceId,
  onClose,
  onCreated,
}) => {
  const { theme } = useTheme();
  const errorMessage = useErrorMessage();
  const [dataSourceId, setDataSourceId] = useState<number | null>(defaultDataSourceId);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [rows, setRows] = useState<SpPreviewRow[]>([]);
  const [selectedSpName, setSelectedSpName] = useState<string | null>(null);

  const collectionView = useMemo(() => new CollectionView<SpPreviewRow>(rows), [rows]);
  const selectedRow = rows.find((r) => r.SpName === selectedSpName) ?? null;
  const paramView = useMemo(
    () => new CollectionView<SpParamRow>(selectedRow?.Parameters ?? []),
    [selectedRow],
  );

  useEffect(() => {
    if (open) {
      setDataSourceId(defaultDataSourceId);
      setRows([]);
      setSelectedSpName(null);
    }
  }, [open, defaultDataSourceId]);

  const loadProcedures = useCallback(async () => {
    if (dataSourceId == null) {
      errorMessage.showError('Select a data source first.');
      return;
    }
    setLoading(true);
    try {
      const list = await integrationService.listStoredProceduresForApiBuilder(dataSourceId);
      const mapped: SpPreviewRow[] = list.map((p: any) => ({
        Include: false,
        Schema: p.Schema ?? p.schema,
        SpName: p.Name ?? p.name,
        FullName: p.FullName ?? p.fullName,
        ActionCode: p.SuggestedActionCode ?? p.suggestedActionCode ?? `AppSp_${p.Name ?? p.name}`,
        Description: p.Description ?? p.description ?? '',
        CaptureSample: false,
        Parameters: (p.Parameters ?? p.parameters ?? []).map((x: any) => ({
          Name: x.Name ?? x.name,
          Type: x.Type ?? x.type,
          Direction: x.Direction ?? x.direction,
          MaxLength: x.MaxLength ?? x.maxLength,
          Ordinal: x.Ordinal ?? x.ordinal,
          HasDefault: x.HasDefault ?? x.hasDefault,
          DefaultValue: x.DefaultValue ?? x.defaultValue,
        })),
      }));
      setRows(mapped);
      setSelectedSpName(mapped[0]?.SpName ?? null);
    } catch (e) {
      errorMessage.showError(e instanceof Error ? e.message : String(e));
    } finally {
      setLoading(false);
    }
  }, [dataSourceId, errorMessage]);

  const updateRow = useCallback((spName: string, patch: Partial<SpPreviewRow>) => {
    setRows((prev) => prev.map((r) => (r.SpName === spName ? { ...r, ...patch } : r)));
  }, []);

  const updateParamDefault = useCallback((spName: string, paramName: string, value: string) => {
    setRows((prev) =>
      prev.map((r) => {
        if (r.SpName !== spName) return r;
        return {
          ...r,
          Parameters: r.Parameters.map((p) =>
            (p.Name ?? '') === paramName ? { ...p, DefaultValue: value } : p,
          ),
        };
      }),
    );
  }, []);

  const selectAll = useCallback((include: boolean) => {
    setRows((prev) => prev.map((r) => ({ ...r, Include: include })));
  }, []);

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
          CaptureSample: r.CaptureSample,
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
      errorMessage.showInfo(`Created ${result?.Object?.createdCount ?? items.length} Stored Procedure API(s).`, true);
      onCreated();
      onClose();
    } catch (e) {
      errorMessage.showError(e instanceof Error ? e.message : String(e));
    } finally {
      setSaving(false);
    }
  }, [dataSourceId, rows, errorMessage, onCreated, onClose]);

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center bg-black/40">
      <div className={`w-[960px] max-w-[95vw] h-[82vh] flex flex-col rounded-[6px] border shadow-xl overflow-hidden ${theme.mainContentSection}`}>
        <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.title}`}>
          <div className="text-sm font-semibold">Create Stored Procedure APIs</div>
          <button type="button" className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`} onClick={onClose}>
            Close
          </button>
        </div>

        <div className="px-3 py-2 flex items-center gap-2 border-b">
          <label className={`w-28 text-xs ${theme.label}`}>Data Source</label>
          <select
            className={`h-1 flex-auto w-1 px-2 text-xs border ${theme.inputBox}`}
            value={dataSourceId ?? ''}
            onChange={(e) => setDataSourceId(e.target.value ? Number(e.target.value) : null)}
          >
            <option value="">Select...</option>
            {dataSources.map((ds) => (
              <option key={ds.Id} value={ds.Id}>
                {ds.DataSourceName ?? ds.Display ?? ds.Id} ({ds.Id})
              </option>
            ))}
          </select>
          <button
            type="button"
            disabled={loading || dataSourceId == null}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} disabled:opacity-60`}
            onClick={loadProcedures}
          >
            {loading ? 'Loading...' : 'Load SPs'}
          </button>
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => selectAll(true)}>
            Select all
          </button>
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => selectAll(false)}>
            Clear
          </button>
        </div>

        <div className="h-1 flex-auto flex flex-col min-h-0 px-3 py-2 gap-2">
          <div className="h-[55%] min-h-[180px] overflow-hidden border rounded-[4px]">
            <FlexGrid
              itemsSource={collectionView}
              autoGenerateColumns={false}
              selectionMode="Row"
              isReadOnly={false}
              className="w-full h-full !border-0"
              selectionChanged={(s: any) => {
                const flex = s?.control ?? s;
                const row = flex?.selection?.row;
                if (row == null || row < 0) return;
                const item = flex.rows[row]?.dataItem as SpPreviewRow | undefined;
                if (item) setSelectedSpName(item.SpName);
              }}
              cellEditEnded={(s: any) => {
                const flex = s?.control ?? s;
                const row = flex?.selection?.row;
                if (row == null || row < 0) return;
                const item = flex.rows[row]?.dataItem as SpPreviewRow | undefined;
                if (!item) return;
                updateRow(item.SpName, {
                  Include: !!item.Include,
                  ActionCode: item.ActionCode,
                  Description: item.Description,
                  CaptureSample: !!item.CaptureSample,
                });
              }}
            >
              <FlexGridColumn binding="Include" header="Include" width={70} />
              <FlexGridColumn binding="Schema" header="Schema" width={90} isReadOnly />
              <FlexGridColumn binding="SpName" header="SP Name" width={160} isReadOnly />
              <FlexGridColumn binding="ActionCode" header="API Code" width={200} />
              <FlexGridColumn binding="CaptureSample" header="Capture sample" width={110} />
              <FlexGridColumn binding="Description" header="Description" width="*" />
            </FlexGrid>
          </div>

          <div className={`text-xs ${theme.label}`}>
            Parameters for: <span className="font-semibold">{selectedRow?.FullName ?? selectedRow?.SpName ?? '(select a row)'}</span>
            {' — edit DefaultValue before generate'}
          </div>

          <div className="h-1 flex-auto min-h-[120px] overflow-hidden border rounded-[4px]">
            <FlexGrid
              itemsSource={paramView}
              autoGenerateColumns={false}
              selectionMode="Row"
              isReadOnly={false}
              className="w-full h-full !border-0"
              cellEditEnded={() => {
                if (!selectedRow) return;
                selectedRow.Parameters.forEach((p) => {
                  if (p.Name != null) updateParamDefault(selectedRow.SpName, p.Name, p.DefaultValue ?? '');
                });
              }}
            >
              <FlexGridColumn binding="Name" header="Name" width={140} isReadOnly />
              <FlexGridColumn binding="Type" header="Type" width={100} isReadOnly />
              <FlexGridColumn binding="Direction" header="Direction" width={90} isReadOnly />
              <FlexGridColumn binding="HasDefault" header="HasDefault" width={90} isReadOnly />
              <FlexGridColumn binding="DefaultValue" header="DefaultValue" width="*" />
            </FlexGrid>
          </div>
        </div>

        <div className="px-3 py-2 border-t flex justify-end gap-2">
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onClose} disabled={saving}>
            Cancel
          </button>
          <button
            type="button"
            className="px-3 py-1.5 text-sm rounded-[4px] text-white bg-green-500 hover:bg-green-600 disabled:opacity-60"
            disabled={saving || loading}
            onClick={handleGenerate}
          >
            {saving ? 'Generating...' : 'Generate APIs'}
          </button>
        </div>
      </div>
    </div>
  );
};

export default CreateStoredProcedureApisModal;
