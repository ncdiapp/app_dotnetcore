import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { ComboBox } from '@mescius/wijmo.react.input';
import { CollectionView } from '@mescius/wijmo';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useEnumValues } from '../../../hooks/useEnumDictionary';
import { schemaMetadataService } from '../../../webapi/schemaMetaDataSvc';

export type TransactionFieldCascadingModalProps = {
  isOpen: boolean;
  onClose: () => void;
  onApply: (updatedField: any) => void;
  /** Field being edited (same reference as parent state). */
  currentField: any;
  appTransactionData: any;
};

function findUnitAndFields(
  appTransactionUnitList: any[] | undefined,
  unitId: number | null | undefined
): { unit: any | null; fields: any[] } {
  if (!appTransactionUnitList || unitId == null) return { unit: null, fields: [] };
  const walk = (units: any[]): any => {
    for (const u of units) {
      if (u?.Id === unitId) return u;
      if (u?.Children?.length) {
        const c = walk(u.Children);
        if (c) return c;
      }
    }
    return null;
  };
  const unit = walk(appTransactionUnitList);
  return { unit, fields: unit?.AppTransactionFieldList ?? [] };
}

const ddlLike = (ct: number | undefined, Em: Record<string, number> | null) => {
  if (ct == null || !Em) return false;
  return (
    ct === Em.DDL ||
    ct === Em.SearchAbleDDL ||
    ct === Em.AutoComplete ||
    ct === Em.RadioButtons ||
    ct === Em.Progress
  );
};

const TransactionFieldCascadingModal: React.FC<TransactionFieldCascadingModalProps> = ({
  isOpen,
  onClose,
  onApply,
  currentField,
  appTransactionData,
}) => {
  const { theme } = useTheme();
  const EmControl = useEnumValues('EmAppControlType');
  const EmCascade = useEnumValues('EmAppCascadingSourceType');

  const [ddParentId, setDdParentId] = useState<number | null>(null);
  const [dataRetrieveType, setDataRetrieveType] = useState<number | null>(null);
  const [relationTableObj, setRelationTableObj] = useState<any | null>(null);
  const [parentKeyField, setParentKeyField] = useState<string>('');
  const [childKeyField, setChildKeyField] = useState<string>('');
  const [databaseSchemaData, setDatabaseSchemaData] = useState<any[]>([]);
  const [tableColumns, setTableColumns] = useState<any[]>([]);
  const [busy, setBusy] = useState(false);

  const { unit, fields } = useMemo(
    () => findUnitAndFields(appTransactionData?.AppTransactionUnitList, currentField?.TransactionUnitId),
    [appTransactionData, currentField?.TransactionUnitId]
  );

  const parentDllFieldList = useMemo(() => {
    const Em = EmControl;
    const selfId = currentField?.Id;
    const out: Array<{ Id: number; Display: string }> = [];
    for (const f of fields) {
      if (!f || f.Id === selfId) continue;
      if (ddlLike(f.ControlType, Em)) {
        out.push({ Id: f.Id, Display: f.DataBaseFieldName || f.DisplayName || String(f.Id) });
      }
    }
    return out;
  }, [fields, currentField?.Id, EmControl]);

  const parentDllFieldCv = useMemo(() => new CollectionView(parentDllFieldList), [parentDllFieldList]);

  const relationalTableType = EmCascade?.RelationalTable ?? 1;

  const cascadingTypeOptions = useMemo(() => {
    if (!EmCascade) return [];
    const out: Array<{ label: string; value: number }> = [];
    Object.entries(EmCascade).forEach(([k, v]) => {
      if (typeof v === 'number' && isNaN(Number(k))) out.push({ label: k, value: v });
    });
    return out.sort((a, b) => a.value - b.value);
  }, [EmCascade]);

  useEffect(() => {
    if (!isOpen || !currentField) return;
    setDdParentId(currentField.DdlparentLevelId ?? null);
    setDataRetrieveType(currentField.DataRetrieveType ?? null);
    setParentKeyField(currentField.CascadingRelationTableParentKeyField ?? '');
    setChildKeyField(currentField.CascadingRelationTableChildKeyField ?? '');
    setRelationTableObj(null);
    setTableColumns([]);

    const dsId = appTransactionData?.DataSourceFrom;
    if (dsId == null) return;
    setBusy(true);
    schemaMetadataService
      .getDataSourceTableAndViewListFromCache(dsId, null, null)
      .then((list: any) => {
        const arr = Array.isArray(list) ? list : [];
        arr.forEach((t: any) => {
          t.UiId = t.UiId ?? `${t.SchemaOwner ?? ''}_${t.Name}`;
          t.Display = t.Display ?? t.Name;
        });
        setDatabaseSchemaData(arr);

        if (currentField.CascadingRelationTable) {
          const schemaOwner = currentField.CascadingRelationTableSchemaOwner ?? null;
          const name = currentField.CascadingRelationTable;
          const found = arr.find((t: any) => t.Name === name && (t.SchemaOwner || '') === (schemaOwner || ''));
          setRelationTableObj(found ?? { Name: name, SchemaOwner: schemaOwner, Display: name });

          return schemaMetadataService.getOneDatabaseTableSchema(name, dsId, schemaOwner).then((tableData: any) => {
            const cols = Array.isArray(tableData?.Columns) ? tableData.Columns : [];
            setTableColumns(cols);
          });
        }
        return undefined;
      })
      .catch(() => {
        setDatabaseSchemaData([]);
      })
      .finally(() => setBusy(false));
  }, [isOpen, currentField, appTransactionData?.DataSourceFrom]);

  const onRelationTableSelected = useCallback(
    async (tbl: any) => {
      setRelationTableObj(tbl || null);
      const dsId = appTransactionData?.DataSourceFrom;
      if (!tbl?.Name || dsId == null) {
        setTableColumns([]);
        return;
      }
      setBusy(true);
      try {
        const tableData = await schemaMetadataService.getOneDatabaseTableSchema(
          tbl.Name,
          dsId,
          tbl.SchemaOwner ?? null
        );
        const cols = Array.isArray(tableData?.Columns) ? tableData.Columns : [];
        setTableColumns(cols);
      } finally {
        setBusy(false);
      }
    },
    [appTransactionData?.DataSourceFrom]
  );

  const columnItems = useMemo(
    () => tableColumns.map((c: any) => ({ value: c.Name, label: c.Name })),
    [tableColumns]
  );

  const handleApply = async () => {
    if (!currentField) return;
    const next = { ...currentField, IsModified: true };
    next.DdlparentLevelId = ddParentId;
    next.DataRetrieveType = dataRetrieveType;

    if (ddParentId && dataRetrieveType === relationalTableType && relationTableObj) {
      next.CascadingRelationTableSchemaOwner = relationTableObj.SchemaOwner ?? '';
      next.CascadingRelationTable = relationTableObj.Name;
      next.CascadingRelationTableParentKeyField = parentKeyField || null;
      next.CascadingRelationTableChildKeyField = childKeyField || null;
    } else if (!ddParentId) {
      next.CascadingRelationTableSchemaOwner = null;
      next.CascadingRelationTable = null;
      next.CascadingRelationTableParentKeyField = null;
      next.CascadingRelationTableChildKeyField = null;
      next.DataRetrieveType = null;
    }

    onApply(next);
    onClose();
  };

  if (!isOpen) return null;

  const showRelationalBlock =
    !!ddParentId && dataRetrieveType === relationalTableType && EmCascade != null;

  return (
    <div
      data-prevent-field-setting-dismiss
      className="fixed inset-0 z-[7000] flex items-start justify-center pt-16 px-4 bg-black/40"
      role="dialog"
      aria-modal="true"
      onMouseDown={(e) => e.target === e.currentTarget && onClose()}
    >
      <div
        className={`w-full max-w-md max-h-[85vh] overflow-y-auto rounded border shadow-xl p-3 ${theme.mainContentSection}`}
        onMouseDown={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between mb-2">
          <div className={`text-sm font-semibold ${theme.title}`}>Filter By Field Setting</div>
          <button type="button" className={`rounded-[4px] px-2 py-1 text-sm ${theme.button_default}`} onClick={onClose}>
            ×
          </button>
        </div>
        {busy && <div className={`text-xs mb-2 ${theme.label}`}>Loading…</div>}

        <div className="space-y-3">
          <div>
            <label className={`block text-xs mb-1 ${theme.label}`}>Filter By Field</label>
            <ComboBox
              itemsSource={parentDllFieldCv}
              displayMemberPath="Display"
              selectedValuePath="Id"
              selectedValue={ddParentId}
              isEditable={false}
              placeholder="(None)"
              selectedIndexChanged={(s: any) => setDdParentId(s.selectedValue ?? null)}
              className={`w-full ${theme.inputBox} border`}
              style={{ height: 28 }}
            />
          </div>

          {!!ddParentId && (
            <div>
              <label className={`block text-xs mb-1 ${theme.label}`}>Data Retrieve Type</label>
              <select
                className={`w-full h-7 px-2 text-xs border ${theme.inputBox}`}
                value={dataRetrieveType ?? ''}
                onChange={(e) => {
                  const v = e.target.value ? Number(e.target.value) : null;
                  setDataRetrieveType(v);
                  if (v !== relationalTableType) {
                    setRelationTableObj(null);
                    setTableColumns([]);
                    setParentKeyField('');
                    setChildKeyField('');
                  }
                }}
              >
                <option value="">Select</option>
                {cascadingTypeOptions.map((o) => (
                  <option key={o.label} value={o.value}>
                    {o.label}
                  </option>
                ))}
              </select>
            </div>
          )}

          {showRelationalBlock && (
            <>
              <div>
                <label className={`block text-xs mb-1 ${theme.label}`}>Filter Relationship Data Table</label>
                <select
                  className={`w-full h-7 px-2 text-xs border ${theme.inputBox}`}
                  value={
                    relationTableObj
                      ? `${relationTableObj.SchemaOwner ?? ''}|||${relationTableObj.Name ?? ''}`
                      : ''
                  }
                  onChange={(e) => {
                    const v = e.target.value;
                    if (!v) {
                      setRelationTableObj(null);
                      setTableColumns([]);
                      return;
                    }
                    const [, name] = v.split('|||');
                    const tbl = databaseSchemaData.find((t: any) => t.Name === name && `${t.SchemaOwner ?? ''}|||${t.Name}` === v);
                    void onRelationTableSelected(tbl || null);
                  }}
                >
                  <option value="">Select table</option>
                  {databaseSchemaData.map((t: any) => (
                    <option key={`${t.SchemaOwner ?? ''}_${t.Name}`} value={`${t.SchemaOwner ?? ''}|||${t.Name}`}>
                      {t.Display ?? t.Name}
                    </option>
                  ))}
                </select>
              </div>
              {!!relationTableObj && (
                <>
                  <div>
                    <label className={`block text-xs mb-1 ${theme.label}`}>Mapping To parent key column</label>
                    <select
                      className={`w-full h-7 px-2 text-xs border ${theme.inputBox}`}
                      value={parentKeyField}
                      onChange={(e) => setParentKeyField(e.target.value)}
                    >
                      <option value="">Select</option>
                      {columnItems.map((c) => (
                        <option key={c.value} value={c.value}>
                          {c.label}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className={`block text-xs mb-1 ${theme.label}`}>Mapping To child key column</label>
                    <select
                      className={`w-full h-7 px-2 text-xs border ${theme.inputBox}`}
                      value={childKeyField}
                      onChange={(e) => setChildKeyField(e.target.value)}
                    >
                      <option value="">Select</option>
                      {columnItems.map((c) => (
                        <option key={`c-${c.value}`} value={c.value}>
                          {c.label}
                        </option>
                      ))}
                    </select>
                  </div>
                </>
              )}
            </>
          )}
        </div>

        <div className="flex justify-end gap-2 mt-4">
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onClose}>
            Cancel
          </button>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            onClick={() => void handleApply()}
          >
            OK
          </button>
        </div>
      </div>
    </div>
  );
};

export default TransactionFieldCascadingModal;
