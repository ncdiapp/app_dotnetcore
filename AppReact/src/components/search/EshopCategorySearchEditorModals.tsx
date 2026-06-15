import React, { useEffect, useMemo, useRef, useState } from 'react';
import { CollectionView } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import { FlexGrid, FlexGridCellTemplate, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import {
  EmAppControlTypeDDL,
  EmAppControlTypeTextBox,
  buildFilterOptionListFromCardView,
  findMaxOptionSort,
  parseCategoryTreeLevelsFromView,
  prepareNewViewField,
  validateCategoryTreeLevels,
  type CardMappingLevel,
  type FilterOptionRow,
  type LevelSetting,
} from './eshopCategorySearchEditorApply';
import { adminSvc } from '../../webapi/adminsvc';
import { useEnumValues } from '../../hooks/useEnumDictionary';

type ThemeTokens = {
  mainContentSection: string;
  title: string;
  label: string;
  inputBox: string;
  button_default: string;
};

function levelRowsFromParsed(parsed: ReturnType<typeof parseCategoryTreeLevelsFromView>): LevelSetting[] {
  return parsed.lv.map((x) => ({ ...x }));
}

type CategoryTreeSettingModalProps = {
  open: boolean;
  theme: ThemeTokens;
  dataSetId: number | string | null | undefined;
  dataSetName: string;
  dataSetOptions: { Id: number | string; Name?: string }[];
  onDataSetIdChange: (id: number | string | null) => void;
  onCreateDataSet: () => void;
  onEditDataSet: (dataSetId: number | string) => void;
  columnIds: string[];
  categoryView: any;
  onClose: () => void;
  onApply: (levels: number, rows: LevelSetting[]) => Promise<boolean>;
};

export const CategoryTreeSettingModal: React.FC<CategoryTreeSettingModalProps> = ({
  open,
  theme,
  dataSetId,
  dataSetName,
  dataSetOptions,
  onDataSetIdChange,
  onCreateDataSet,
  onEditDataSet,
  columnIds,
  categoryView,
  onClose,
  onApply,
}) => {
  const [levels, setLevels] = useState(1);
  const [rows, setRows] = useState<LevelSetting[]>(() =>
    levelRowsFromParsed(parseCategoryTreeLevelsFromView(null)),
  );

  useEffect(() => {
    if (!open || !categoryView) return;
    const parsed = parseCategoryTreeLevelsFromView(categoryView);
    setLevels(parsed.levels);
    setRows(levelRowsFromParsed(parsed));
  }, [open, categoryView]);

  if (!open) return null;

  const colOpts = columnIds.map((id) => ({ id, label: id }));

  const row = (i: number) => rows[i] ?? { Id: null, Name: null, Sort: null, EntityId: null };
  const setRow = (i: number, patch: Partial<LevelSetting>) => {
    setRows((prev) => {
      const next = [...prev];
      next[i] = { ...next[i], ...patch };
      return next;
    });
  };

  return (
    <div
      className="fixed inset-0 z-[1300] bg-black/40 flex items-center justify-center p-4"
      onClick={onClose}
    >
      <div
        className={`w-full max-w-[820px] max-h-[90vh] overflow-auto rounded border shadow-lg ${theme.mainContentSection}`}
        onClick={(e) => e.stopPropagation()}
      >
        <div className={`px-3 py-2 border-b flex items-center justify-between ${theme.mainContentSection}`}>
          <div className={`text-sm font-semibold ${theme.title}`}>Category Tree Setting</div>
          <button type="button" className={theme.button_default} onClick={onClose}>
            ×
          </button>
        </div>
        <div className="p-4 space-y-3 text-xs">
          <div>
            <div className={`mb-1 ${theme.label}`}>Dataset — query of category IDs and names</div>
            <div className="flex items-center gap-1">
              <div className="relative w-full max-w-md">
                <select
                  className={`w-full h-8 px-2 border rounded ${theme.inputBox}`}
                  value={dataSetId != null ? String(dataSetId) : ''}
                  onChange={(e) => {
                    const v = e.target.value;
                    onDataSetIdChange(v === '' ? null : v);
                  }}
                >
                  <option value="">—</option>
                  {dataSetOptions.map((d) => (
                    <option key={String(d.Id)} value={String(d.Id)}>
                      {d.Name || d.Id}
                    </option>
                  ))}
                </select>
                {!dataSetId && (
                  <div className="absolute left-0 top-[34px] z-20">
                    <div className="relative max-w-[520px] rounded border border-yellow-300 bg-yellow-50 text-yellow-950 shadow-md px-2 py-1 text-[11px] leading-tight">
                      <div className="absolute left-[18px] top-[-5px] w-[10px] h-[10px] rotate-45 bg-yellow-50 border-l border-t border-yellow-300" />
                      Select or create a dataset first. This dataset must include category level ID and Name columns
                      (and optional Sort columns).
                    </div>
                  </div>
                )}
              </div>
              <button
                type="button"
                className={`h-8 w-8 flex items-center justify-center rounded border ${theme.button_default}`}
                title="Create dataset"
                onClick={() => onCreateDataSet()}
              >
                <i className="fa-solid fa-plus" aria-hidden="true" />
              </button>
              {!!dataSetId && (
                <button
                  type="button"
                  className={`h-8 w-8 flex items-center justify-center rounded border ${theme.button_default}`}
                  title="Edit dataset"
                  onClick={() => onEditDataSet(dataSetId)}
                >
                  <i className="fa-solid fa-pencil" aria-hidden="true" />
                </button>
              )}
            </div>
            {dataSetName ? (
              <div className={`mt-1 ${theme.label}`}>Current: {dataSetName}</div>
            ) : null}
            {!dataSetId ? (
              <div className="mt-1 text-[11px] text-red-600">Please select or create a dataset</div>
            ) : null}
          </div>

          {!!dataSetId && (
            <>
              <div className="flex items-center gap-2">
                <span className={theme.label}>Total category levels</span>
                <input
                  type="number"
                  min={1}
                  max={5}
                  className={`w-16 h-7 px-1 border rounded ${theme.inputBox}`}
                  value={levels}
                  onChange={(e) => setLevels(Math.min(5, Math.max(1, Number(e.target.value) || 1)))}
                />
              </div>

              {Array.from({ length: levels }, (_, idx) => idx).map((idx) => {
                const lv = idx + 1;
                const r = row(idx);
                return (
                  <div key={lv} className="border rounded p-2 space-y-2">
                    <div className={`font-semibold ${theme.title}`}>Level {lv} category</div>
                    <div className="grid grid-cols-1 md:grid-cols-4 gap-2">
                      <label className="flex flex-col gap-1">
                        <span className={theme.label}>ID field</span>
                        <select
                          className={`h-7 px-1 border rounded ${theme.inputBox}`}
                          value={r.Id ?? ''}
                          onChange={(e) => setRow(idx, { Id: e.target.value || null })}
                        >
                          <option value="">—</option>
                          {colOpts.map((c) => (
                            <option key={c.id} value={c.id}>
                              {c.label}
                            </option>
                          ))}
                        </select>
                      </label>
                      <label className="flex flex-col gap-1">
                        <span className={theme.label}>Display field</span>
                        <select
                          className={`h-7 px-1 border rounded ${theme.inputBox}`}
                          value={r.Name ?? ''}
                          onChange={(e) => setRow(idx, { Name: e.target.value || null })}
                        >
                          <option value="">—</option>
                          {colOpts.map((c) => (
                            <option key={c.id} value={c.id}>
                              {c.label}
                            </option>
                          ))}
                        </select>
                      </label>
                      <label className="flex flex-col gap-1">
                        <span className={theme.label}>Display entity Id (optional)</span>
                        <input
                          type="number"
                          className={`h-7 px-1 border rounded ${theme.inputBox}`}
                          value={r.EntityId ?? ''}
                          onChange={(e) =>
                            setRow(idx, {
                              EntityId: e.target.value === '' ? null : Number(e.target.value),
                            })
                          }
                        />
                      </label>
                      <label className="flex flex-col gap-1">
                        <span className={theme.label}>Position / sort field</span>
                        <select
                          className={`h-7 px-1 border rounded ${theme.inputBox}`}
                          value={r.Sort ?? ''}
                          onChange={(e) => setRow(idx, { Sort: e.target.value || null })}
                        >
                          <option value="">—</option>
                          {colOpts.map((c) => (
                            <option key={c.id} value={c.id}>
                              {c.label}
                            </option>
                          ))}
                        </select>
                      </label>
                    </div>
                  </div>
                );
              })}
            </>
          )}
        </div>
        <div className={`px-3 py-2 border-t flex justify-end gap-2 ${theme.mainContentSection}`}>
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onClose}>
            Cancel
          </button>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            disabled={!dataSetId}
            onClick={async () => {
              if (!dataSetId) return;
              const err = validateCategoryTreeLevels(levels, rows);
              if (err) {
                window.alert(err);
                return;
              }
              const ok = await onApply(levels, rows);
              if (ok) onClose();
            }}
          >
            Save &amp; Close
          </button>
        </div>
      </div>
    </div>
  );
};

type CardViewSettingModalProps = {
  open: boolean;
  theme: ThemeTokens;
  categoryView: any;
  eshopCardSearchExDto: any;
  dataSetId: number | string | null | undefined;
  dataSetName: string;
  dataSetOptions: { Id: number | string; Name?: string }[];
  onDataSetIdChange: (id: number | string | null) => void;
  onCreateDataSet: () => void;
  onEditDataSet: (id: number | string) => void;
  cardColumnIds: string[];
  searchFieldPaths: string[];
  onClose: () => void;
  onApply: (draft: {
    levels: CardMappingLevel[];
    rootGroupKeyField: string | null;
    cardViewFieldList: any[];
    deletedViewFieldIds: (number | string)[];
  }) => Promise<boolean>;
};

export const CardViewSettingModal: React.FC<CardViewSettingModalProps> = ({
  open,
  theme,
  categoryView,
  eshopCardSearchExDto,
  dataSetId,
  dataSetName,
  dataSetOptions,
  onDataSetIdChange,
  onCreateDataSet,
  onEditDataSet,
  cardColumnIds,
  searchFieldPaths,
  onClose,
  onApply,
}) => {
  const treeIdFields = useMemo(() => {
    const list = categoryView?.AppSearchViewFieldList ?? [];
    const out: { TreeLevel: number; Id: number | null; SysTableFiledPath: string }[] = [];
    for (const f of list) {
      if (f?.IsTreeNodeId && f.TreeLevel) {
        out.push({
          TreeLevel: Number(f.TreeLevel),
          Id: f.Id ?? null,
          SysTableFiledPath: String(f.SysTableFiledPath ?? ''),
        });
      }
    }
    return out.sort((a, b) => a.TreeLevel - b.TreeLevel);
  }, [categoryView]);

  const [lv, setLv] = useState<CardMappingLevel[]>(() =>
    Array.from({ length: 5 }, () => ({ ViewFieldId: null, SearchFieldName: null })),
  );
  const [rootGroup, setRootGroup] = useState<string | null>(null);
  const [cardViewFields, setCardViewFields] = useState<any[]>([]);
  const [deletedViewFieldIds, setDeletedViewFieldIds] = useState<(number | string)[]>([]);
  const [selectedFieldIndex, setSelectedFieldIndex] = useState(0);
  const [entityOptions, setEntityOptions] = useState<any[]>([]);
  const [entitySelectionItem, setEntitySelectionItem] = useState<any | null>(null);
  const [datasourceSelectorSelectedId, setDatasourceSelectorSelectedId] = useState<number | null>(null);
  const [columnPickerOpen, setColumnPickerOpen] = useState(false);
  const columnPickerGridRef = useRef<any>(null);
  const emAppControlType = useEnumValues('EmAppControlType');
  const mappingLevels = useMemo(() => {
    const items: { level: number; treeField: { TreeLevel: number; Id: number | null; SysTableFiledPath: string } }[] = [];
    for (let n = 1; n <= 5; n++) {
      const treeField = treeIdFields.find((t) => t.TreeLevel === n);
      if (treeField) items.push({ level: n, treeField });
    }
    return items;
  }, [treeIdFields]);
  const mappingTargetFieldOptions = useMemo(() => {
    const fromDataset = (cardColumnIds ?? []).filter((x) => String(x ?? '').trim() !== '');
    if (fromDataset.length > 0) return fromDataset;
    return (searchFieldPaths ?? []).filter((x) => String(x ?? '').trim() !== '');
  }, [cardColumnIds, searchFieldPaths]);
  const controlTypeList = useMemo(() => {
    if (!emAppControlType) return [];
    return Object.entries(emAppControlType).map(([key, id]) => ({ Id: id, Display: key }));
  }, [emAppControlType]);
  const cardFieldCv = useMemo(() => new CollectionView<any>(cardViewFields ?? []), [cardViewFields]);
  const fieldNameDataMap = useMemo(() => new DataMap((cardColumnIds ?? []).map((id) => ({ Id: id })), 'Id', 'Id'), [cardColumnIds]);
  const controlTypeDataMap = useMemo(() => new DataMap(controlTypeList, 'Id', 'Display'), [controlTypeList]);
  const columnPickerAvailableList = useMemo(() => (cardColumnIds ?? []).map((id) => ({ Id: id, Display: '' })), [cardColumnIds]);
  const columnPickerCV = useMemo(
    () => (columnPickerAvailableList.length ? new CollectionView(columnPickerAvailableList) : null),
    [columnPickerAvailableList],
  );
  const getEntityDisplay = (entityId: any): string => {
    if (entityId == null) return '';
    const hit = (entityOptions ?? []).find((e: any) => String(e?.Id) === String(entityId));
    return String(hit?.EntityCode ?? hit?.Name ?? entityId);
  };

  useEffect(() => {
    if (!open) return;
    const mapping = categoryView?.OtherSettingsDto?.EshopCategorySearchMapping;
    const next = Array.from({ length: 5 }, (_, i) => {
      const n = i + 1;
      const treeField = treeIdFields.find((t) => t.TreeLevel === n);
      return {
        ViewFieldId: treeField?.Id ?? mapping?.[`SourceViewColumnId${n}`] ?? null,
        SearchFieldName: mapping?.[`TargetSearchFieldName${n}`] ?? null,
      } as CardMappingLevel;
    });
    setLv(next);

    const cardView = eshopCardSearchExDto?.DefaultSearchViewExDto;
    const fields = Array.isArray(cardView?.AppSearchViewFieldList) ? cardView.AppSearchViewFieldList : [];
    setCardViewFields(fields.map((f: any) => ({ ...f })));
    setDeletedViewFieldIds([]);
    setSelectedFieldIndex(0);
    setColumnPickerOpen(false);
    setEntitySelectionItem(null);
    setDatasourceSelectorSelectedId(null);
    let rg: string | null = null;
    const fl = cardView?.AppSearchViewFieldList ?? [];
    for (const f of fl) {
      if (f?.IsGroupBy && f.SysTableFiledPath) {
        rg = String(f.SysTableFiledPath);
        break;
      }
    }
    setRootGroup(rg);
  }, [open, categoryView, eshopCardSearchExDto, treeIdFields]);
  useEffect(() => {
    if (!open) return;
    let cancelled = false;
    (async () => {
      try {
        const raw = await adminSvc.getMassEntitiesLookupItem('AppEntityInfo');
        const arr = Array.isArray(raw?.AppEntityInfo) ? raw.AppEntityInfo : Array.isArray(raw) ? raw : [];
        if (!cancelled) setEntityOptions(arr);
      } catch {
        if (!cancelled) setEntityOptions([]);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [open]);

  if (!open) return null;

  return (
    <div
      className="fixed inset-0 z-[1300] bg-black/40 flex items-center justify-center p-4"
      onClick={onClose}
    >
      <div
        className={`w-full max-w-[1200px] h-[90vh] rounded border shadow-lg ${theme.mainContentSection} flex flex-col relative`}
        onClick={(e) => e.stopPropagation()}
      >
        <div className={`px-3 py-2 border-b flex items-center justify-between ${theme.mainContentSection}`}>
          <div className={`text-sm font-semibold ${theme.title}`}>Card View Setting</div>
          <button type="button" className={theme.button_default} onClick={onClose}>
            ×
          </button>
        </div>
        <div className="p-4 space-y-3 text-xs flex-auto min-h-0 overflow-auto">
          <div>
            <div className={`mb-1 ${theme.label}`}>Dataset - Query of Item Info with Category Id</div>
            <div className="flex items-center gap-1">
              <div className="relative w-full max-w-md">
                <select
                  className={`w-full h-7 px-2 border rounded ${theme.inputBox}`}
                  value={dataSetId != null ? String(dataSetId) : ''}
                  onChange={(e) => onDataSetIdChange(e.target.value || null)}
                >
                  <option value="">—</option>
                  {dataSetOptions.map((d) => (
                    <option key={String(d.Id)} value={String(d.Id)}>
                      {d.Name || d.Id}
                    </option>
                  ))}
                </select>
                {!dataSetId && (
                  <div className="absolute left-0 top-[34px] z-20">
                    <div className="relative max-w-[520px] rounded border border-yellow-300 bg-yellow-50 text-yellow-950 shadow-md px-2 py-1 text-[11px] leading-tight">
                      <div className="absolute left-[18px] top-[-5px] w-[10px] h-[10px] rotate-45 bg-yellow-50 border-l border-t border-yellow-300" />
                      Please select or create a dataset.
                    </div>
                  </div>
                )}
              </div>
              <button type="button" className={`h-7 w-7 flex items-center justify-center rounded border ${theme.button_default}`} onClick={onCreateDataSet}>
                <i className="fa-solid fa-plus" aria-hidden="true" />
              </button>
              {!!dataSetId && (
                <button type="button" className={`h-7 w-7 flex items-center justify-center rounded border ${theme.button_default}`} onClick={() => onEditDataSet(dataSetId)}>
                  <i className="fa-solid fa-pencil" aria-hidden="true" />
                </button>
              )}
            </div>
            {!!dataSetName && <div className={`mt-1 ${theme.label}`}>Current: {dataSetName}</div>}
          </div>

          {!!dataSetId && (
            <>
              <div className="pt-2">
                <div className={`${theme.label} pb-2`}>Category Search Mapping:</div>
                <div className="space-y-3">
                  {Array.from({ length: Math.ceil(mappingLevels.length / 2) }).map((_, rowIdx) => {
                    const left = mappingLevels[rowIdx * 2];
                    const right = mappingLevels[rowIdx * 2 + 1];
                    return (
                      <div key={`map-row-${rowIdx}`} className="border-b pb-3">
                        <div className="grid grid-cols-2 gap-3">
                          {[left, right].map((m) => {
                            if (!m) return <div key={`empty-${rowIdx}`} />;
                            const i = m.level - 1;
                            return (
                              <label key={m.level} className="flex flex-col gap-1">
                                <span className={theme.label}>
                                  Mapping Level {m.level}: {m.treeField.SysTableFiledPath}
                                </span>
                                <select
                                  className={`h-7 px-1 border rounded ${theme.inputBox}`}
                                  value={lv[i]?.SearchFieldName ?? ''}
                                  onChange={(e) => {
                                    const v = e.target.value;
                                    setLv((prev) => {
                                      const c = [...prev];
                                      c[i] = { ...c[i], ViewFieldId: m.treeField.Id ?? null, SearchFieldName: v || null };
                                      return c;
                                    });
                                  }}
                                >
                                  <option value="">—</option>
                                  {mappingTargetFieldOptions.map((p) => (
                                    <option key={p} value={p}>
                                      {p}
                                    </option>
                                  ))}
                                </select>
                              </label>
                            );
                          })}
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>

              <label className="flex flex-col gap-1 max-w-md pt-1">
                <span className={theme.label}>Key Field (The Key Grouping Items Into One Card)</span>
                <select
                  className={`h-7 px-1 border rounded ${theme.inputBox}`}
                  value={rootGroup ?? ''}
                  onChange={(e) => setRootGroup(e.target.value || null)}
                >
                  <option value="">—</option>
                  {cardColumnIds.map((c) => (
                    <option key={c} value={c}>
                      {c}
                    </option>
                  ))}
                </select>
              </label>

              <div className="pt-2">
                <div className="flex items-center gap-3 pb-2">
                  <div className={`font-semibold ${theme.title}`}>Card View Fields</div>
                  <button
                    type="button"
                    className={`px-2 py-1 rounded ${theme.button_default}`}
                    onClick={() => {
                      if (cardColumnIds.length) setColumnPickerOpen(true);
                      else {
                        const cardView = eshopCardSearchExDto?.DefaultSearchViewExDto ?? { AppSearchViewFieldList: cardViewFields };
                        const nf = prepareNewViewField(cardView);
                        nf.IsModified = true;
                        setCardViewFields((prev) => [...prev, nf]);
                      }
                    }}
                  >
                    <i className="fa-solid fa-plus" aria-hidden="true" /> Add
                  </button>
                  <button
                    type="button"
                    className={`px-2 py-1 rounded ${theme.button_default}`}
                    disabled={cardViewFields.length === 0}
                    onClick={() => {
                      setCardViewFields((prev) => {
                        if (!prev.length) return prev;
                        const idx = Math.min(selectedFieldIndex, prev.length - 1);
                        const row = prev[idx];
                        if (row?.Id != null) {
                          setDeletedViewFieldIds((old) => [...old, row.Id]);
                        }
                        const next = prev.filter((_, i) => i !== idx);
                        setSelectedFieldIndex(Math.max(0, idx - 1));
                        return next;
                      });
                    }}
                  >
                    <i className="fa-solid fa-minus" aria-hidden="true" /> Remove
                  </button>
                </div>
                <div className="border rounded h-[46vh] min-h-[280px] overflow-hidden">
                  <FlexGrid
                    className="w-full h-full"
                    itemsSource={cardFieldCv}
                    selectionMode="Row"
                    headersVisibility="Column"
                    cellEditEnded={(s: any) => {
                      const flex = s?.control ?? s;
                      const rowIndex = flex?.selection?.row ?? -1;
                      const rowItem = rowIndex >= 0 ? flex?.rows?.[rowIndex]?.dataItem : null;
                      if (!rowItem) return;
                      rowItem.IsModified = true;
                      setCardViewFields((prev) => [...prev]);
                    }}
                    selectionChanged={(s: any) => {
                      const flex = s?.control ?? s;
                      const rowIndex = flex?.selection?.row ?? -1;
                      if (rowIndex >= 0) setSelectedFieldIndex(rowIndex);
                    }}
                  >
                    <FlexGridFilter />
                    <FlexGridColumn binding="Sort" header="Sort" width={60} />
                    <FlexGridColumn binding="SysTableFiledPath" header="Field Name" width={220} dataMap={fieldNameDataMap} />
                    <FlexGridColumn binding="DisplayText" header="Display" width={170} />
                    <FlexGridColumn binding="ControlType" header="Control Type" width={120} dataMap={controlTypeDataMap} />
                    <FlexGridColumn binding="EntityId" header="Entity" width={220} isReadOnly={true}>
                      <FlexGridCellTemplate
                        cellType="Cell"
                        template={(cell: any) => {
                          const item = cell.item;
                          if (!item) return null;
                          return (
                            <div className="flex items-center gap-1 min-w-0">
                              {item?.ControlType === EmAppControlTypeDDL && (
                                <button
                                  type="button"
                                  className={`w-6 h-6 rounded ${theme.button_default}`}
                                  onClick={(e) => {
                                    e.stopPropagation();
                                    setEntitySelectionItem(item);
                                    setDatasourceSelectorSelectedId(item?.EntityId ?? null);
                                  }}
                                  title="Select datasource"
                                >
                                  <i className="fa-solid fa-search" />
                                </button>
                              )}
                              {!!item?.EntityId && (
                                <button type="button" className={`w-6 h-6 rounded ${theme.button_default}`} title="Entity selected">
                                  <i className="fa-solid fa-database" />
                                </button>
                              )}
                              <span className={`truncate ${theme.label}`} title={getEntityDisplay(item?.EntityId)}>
                                {getEntityDisplay(item?.EntityId)}
                              </span>
                            </div>
                          );
                        }}
                      />
                    </FlexGridColumn>
                    <FlexGridColumn binding="IsVisible" header="Is Visible" dataType="Boolean" width={90} />
                    <FlexGridColumn header="" binding="" width="*" />
                  </FlexGrid>
                </div>
              </div>
            </>
          )}
        </div>
        {columnPickerOpen && (
          <div className="absolute inset-0 z-[1400] bg-black/20 flex items-center justify-center" onClick={() => setColumnPickerOpen(false)}>
            <div className={`rounded shadow-xl w-full max-w-lg flex flex-col ${theme.mainContentSection}`} style={{ height: '500px' }} onClick={(e) => e.stopPropagation()}>
              <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
                <div className={`text-sm font-semibold ${theme.title}`}>Field List</div>
                <button type="button" onClick={() => setColumnPickerOpen(false)} className="text-lg leading-none">&times;</button>
              </div>
              <div className="w-full h-1 flex-auto overflow-hidden p-2" style={{ minHeight: 0 }}>
                {columnPickerCV ? (
                  <FlexGrid
                    ref={columnPickerGridRef}
                    itemsSource={columnPickerCV}
                    isReadOnly={true}
                    selectionMode="ListBox"
                    className="w-full h-full"
                    selectionChanged={() => {
                      const flex = columnPickerGridRef.current?.control ?? columnPickerGridRef.current;
                      if (flex?.invalidate) flex.invalidate();
                    }}
                    formatItem={(s: any, e: any) => {
                      if (e.panel.cellType === 0) {
                        const row = e.panel.rows[e.row];
                        e.cell.innerHTML = row?.isSelected
                          ? '<div style="padding:0 5px;"><i class="fa-solid fa-check" style="color:#808080;"></i></div>'
                          : '<div style="padding:0 5px;"></div>';
                      }
                    }}
                  >
                    <FlexGridFilter />
                    <FlexGridColumn binding="Id" header="Field Name" width="*" />
                    <FlexGridColumn binding="Display" header="Data Type" width={100} />
                  </FlexGrid>
                ) : (
                  <div className={`text-sm ${theme.label} py-4`}>No dataset columns are available to add.</div>
                )}
              </div>
              <div className="flex justify-end gap-2 p-3 border-t">
                <button type="button" onClick={() => setColumnPickerOpen(false)} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>Cancel</button>
                <button
                  type="button"
                  className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                  onClick={() => {
                    const flex = columnPickerGridRef.current?.control ?? columnPickerGridRef.current;
                    if (!flex?.selectedRows?.length) {
                      window.alert('Please select at least one column');
                      return;
                    }
                    const selectedIds = flex.selectedRows
                      .map((r: any) => r?.dataItem?.Id ?? r?.dataItem?.ColumnId ?? null)
                      .filter(Boolean)
                      .map((x: any) => String(x));
                    if (!selectedIds.length) return;
                    const selectedSet = new Set(selectedIds);
                    const orderedIds = columnPickerAvailableList
                      .map((c: any) => String(c?.Id ?? c?.ColumnId ?? c))
                      .filter((id: string) => selectedSet.has(id));
                    setCardViewFields((prev) => {
                      const cardView = eshopCardSearchExDto?.DefaultSearchViewExDto ?? { AppSearchViewFieldList: prev };
                      let next = [...prev];
                      let maxSort = next.reduce((m, r) => Math.max(m, Number(r?.Sort) || 0), 0);
                      for (const col of orderedIds) {
                        const nf = prepareNewViewField(cardView);
                        maxSort += 1;
                        nf.Sort = maxSort;
                        nf.SysTableFiledPath = col;
                        nf.DisplayText = col;
                        nf.ControlType = EmAppControlTypeTextBox;
                        nf.IsVisible = true;
                        nf.IsModified = true;
                        next.push(nf);
                      }
                      return next;
                    });
                    setColumnPickerOpen(false);
                  }}
                >
                  Ok
                </button>
              </div>
            </div>
          </div>
        )}
        {entitySelectionItem != null && (
          <div className="absolute inset-0 z-[1400] flex items-center justify-center bg-black/40" role="dialog" aria-modal="true">
            <div className={`max-w-lg w-full max-h-[85vh] flex flex-col rounded-md overflow-hidden ${theme.mainContentSection} shadow-lg`}>
              <div className={`flex items-center justify-between px-4 py-2 border-b ${theme.title}`}>
                <span className="text-md font-semibold">Datasource Selector</span>
                <button type="button" onClick={() => { setEntitySelectionItem(null); setDatasourceSelectorSelectedId(null); }} className={`p-1 rounded ${theme.button_default}`} aria-label="Close">
                  <i className="fa-solid fa-times" aria-hidden />
                </button>
              </div>
              <div className="flex-1 min-h-0 overflow-auto p-2">
                <div className={`text-xs mb-2 ${theme.label}`}>Entity Code</div>
                <div className={`border ${theme.inputBox} rounded overflow-hidden`} style={{ minHeight: 200 }}>
                  {entityOptions.length === 0 ? (
                    <div className="p-3 text-xs">Loading entities...</div>
                  ) : (
                    <ul className="divide-y divide-gray-200 max-h-[50vh] overflow-auto">
                      {entityOptions.map((e: any) => (
                        <li
                          key={String(e?.Id)}
                          role="button"
                          tabIndex={0}
                          onClick={() => setDatasourceSelectorSelectedId(Number(e?.Id))}
                          onKeyDown={(ev) => (ev.key === 'Enter' || ev.key === ' ') && setDatasourceSelectorSelectedId(Number(e?.Id))}
                          className={`px-3 py-2 text-xs cursor-pointer hover:opacity-80 ${datasourceSelectorSelectedId === Number(e?.Id) ? theme.mainContentSection : ''}`}
                        >
                          {e?.Display ?? e?.EntityCode ?? e?.Name ?? String(e?.Id)}
                        </li>
                      ))}
                    </ul>
                  )}
                </div>
              </div>
              <div className={`flex justify-end gap-2 px-4 py-2 border-t ${theme.mainContentSection}`}>
                <button type="button" onClick={() => { setEntitySelectionItem(null); setDatasourceSelectorSelectedId(null); }} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>Close</button>
                <button
                  type="button"
                  onClick={() => {
                    if (!entitySelectionItem) return;
                    entitySelectionItem.EntityId = datasourceSelectorSelectedId ?? null;
                    entitySelectionItem.IsModified = true;
                    setCardViewFields((prev) => [...prev]);
                    setEntitySelectionItem(null);
                    setDatasourceSelectorSelectedId(null);
                  }}
                  className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                >
                  OK
                </button>
              </div>
            </div>
          </div>
        )}
        <div className={`px-3 py-2 border-t flex justify-end gap-2 ${theme.mainContentSection}`}>
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onClose}>
            Cancel
          </button>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            onClick={async () => {
              const ok = await onApply({
                levels: lv,
                rootGroupKeyField: rootGroup,
                cardViewFieldList: cardViewFields,
                deletedViewFieldIds,
              });
              if (ok) onClose();
            }}
            disabled={!dataSetId}
          >
            Save &amp; Close
          </button>
        </div>
      </div>
    </div>
  );
};

type ItemFilterSettingModalProps = {
  open: boolean;
  theme: ThemeTokens;
  cardDefaultView: any;
  columnIds: string[];
  onClose: () => void;
  onApply: (options: FilterOptionRow[]) => Promise<boolean>;
};

export const ItemFilterSettingModal: React.FC<ItemFilterSettingModalProps> = ({
  open,
  theme,
  cardDefaultView,
  columnIds,
  onClose,
  onApply,
}) => {
  const cols = useMemo(() => columnIds.map((id) => ({ Id: id })), [columnIds]);
  const [options, setOptions] = useState<FilterOptionRow[]>([]);
  const [entityOptions, setEntityOptions] = useState<any[]>([]);
  const [entitySelectionIndex, setEntitySelectionIndex] = useState<number | null>(null);
  const [entitySelectedId, setEntitySelectedId] = useState<number | null>(null);

  useEffect(() => {
    if (!open) return;
    setOptions(buildFilterOptionListFromCardView(cardDefaultView, cols));
  }, [open, cardDefaultView, cols]);
  useEffect(() => {
    if (!open) return;
    let cancelled = false;
    (async () => {
      try {
        const raw = await adminSvc.getMassEntitiesLookupItem('AppEntityInfo');
        const arr = Array.isArray(raw?.AppEntityInfo) ? raw.AppEntityInfo : Array.isArray(raw) ? raw : [];
        if (!cancelled) setEntityOptions(arr);
      } catch {
        if (!cancelled) setEntityOptions([]);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [open]);

  if (!open) return null;

  const updateOpt = (idx: number, patch: Partial<FilterOptionRow>) => {
    setOptions((prev) => {
      const n = [...prev];
      n[idx] = { ...n[idx], ...patch };
      return n;
    });
  };

  return (
    <div
      className="fixed inset-0 z-[1300] bg-black/40 flex items-center justify-center p-4"
      onClick={onClose}
    >
      <div
        className={`w-full max-w-[1100px] max-h-[90vh] overflow-auto rounded border shadow-lg ${theme.mainContentSection}`}
        onClick={(e) => e.stopPropagation()}
      >
        <div className={`px-3 py-2 border-b flex items-center justify-between ${theme.mainContentSection}`}>
          <div className={`text-sm font-semibold ${theme.title}`}>Eshop Card View Option Filter Setting</div>
          <button type="button" className={theme.button_default} onClick={onClose}>
            ×
          </button>
        </div>
        <div className="p-4 space-y-2 text-xs">
          <div className="flex gap-2 items-center">
            <button
              type="button"
              className={`px-2 py-1 rounded ${theme.button_default}`}
              onClick={() =>
                setOptions((prev) => [
                  ...prev,
                  {
                    Sort: findMaxOptionSort(prev) + 1,
                    Label: '',
                    Id: null,
                    Name: null,
                    EntityId: null,
                  },
                ])
              }
            >
              <i className="fa-solid fa-plus" aria-hidden="true" /> Add option
            </button>
          </div>
          <div className="space-y-3 max-h-[55vh] overflow-y-auto pr-1">
            {options.map((opt, idx) => (
              <div key={idx} className="border rounded p-2 grid grid-cols-6 gap-2 items-end">
                <button
                  type="button"
                  className={`h-7 px-2 rounded ${theme.button_default}`}
                  title="Remove"
                  onClick={() => setOptions((prev) => prev.filter((_, j) => j !== idx))}
                >
                  <i className="fa-solid fa-trash" aria-hidden="true" />
                </button>
                <label className="flex flex-col gap-1">
                  <span className={theme.label}>Sort</span>
                  <input
                    type="number"
                    min={1}
                    className={`h-7 px-1 border rounded ${theme.inputBox}`}
                    value={opt.Sort}
                    onChange={(e) => updateOpt(idx, { Sort: Number(e.target.value) || 1 })}
                  />
                </label>
                <label className="flex flex-col gap-1">
                  <span className={theme.label}>Option title</span>
                  <input
                    className={`h-7 px-1 border rounded ${theme.inputBox}`}
                    value={opt.Label}
                    onChange={(e) => updateOpt(idx, { Label: e.target.value })}
                  />
                </label>
                <label className="flex flex-col gap-1">
                  <span className={theme.label}>Option Id mapping</span>
                  <select
                    className={`h-7 px-1 border rounded ${theme.inputBox}`}
                    value={opt.Id ?? ''}
                    onChange={(e) => updateOpt(idx, { Id: e.target.value || null })}
                  >
                    <option value="">—</option>
                    {columnIds.map((c) => (
                      <option key={c} value={c}>
                        {c}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="flex flex-col gap-1">
                  <span className={theme.label}>Option display mapping</span>
                  <select
                    className={`h-7 px-1 border rounded ${theme.inputBox}`}
                    value={opt.Name ?? ''}
                    onChange={(e) => updateOpt(idx, { Name: e.target.value || null })}
                  >
                    <option value="">—</option>
                    {columnIds.map((c) => (
                      <option key={c} value={c}>
                        {c}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="flex flex-col gap-1">
                  <span className={theme.label}>Display entity Id</span>
                  <div className="flex items-center gap-1">
                    <button
                      type="button"
                      className={`h-7 w-7 rounded ${theme.button_default}`}
                      title="Select datasource"
                      onClick={() => {
                        setEntitySelectionIndex(idx);
                        setEntitySelectedId(opt.EntityId ?? null);
                      }}
                    >
                      <i className="fa-solid fa-database" aria-hidden="true" />
                    </button>
                    <input
                      readOnly
                      className={`h-7 px-1 border rounded flex-auto ${theme.inputBox}`}
                      value={
                        opt.EntityId == null
                          ? ''
                          : String(
                              entityOptions.find((e: any) => String(e?.Id) === String(opt.EntityId))?.EntityCode ??
                                opt.EntityId,
                            )
                      }
                    />
                  </div>
                </label>
              </div>
            ))}
          </div>
        </div>
        {entitySelectionIndex != null && (
          <div className="absolute inset-0 z-[1400] flex items-center justify-center bg-black/40" role="dialog" aria-modal="true">
            <div className={`max-w-lg w-full max-h-[85vh] flex flex-col rounded-md overflow-hidden ${theme.mainContentSection} shadow-lg`}>
              <div className={`flex items-center justify-between px-4 py-2 border-b ${theme.title}`}>
                <span className="text-md font-semibold">Datasource Selector</span>
                <button
                  type="button"
                  onClick={() => {
                    setEntitySelectionIndex(null);
                    setEntitySelectedId(null);
                  }}
                  className={`p-1 rounded ${theme.button_default}`}
                  aria-label="Close"
                >
                  <i className="fa-solid fa-times" aria-hidden />
                </button>
              </div>
              <div className="flex-1 min-h-0 overflow-auto p-2">
                <div className={`text-xs mb-2 ${theme.label}`}>Entity Code</div>
                <div className={`border ${theme.inputBox} rounded overflow-hidden`} style={{ minHeight: 200 }}>
                  {entityOptions.length === 0 ? (
                    <div className="p-3 text-xs">Loading entities...</div>
                  ) : (
                    <ul className="divide-y divide-gray-200 max-h-[50vh] overflow-auto">
                      {entityOptions.map((e: any) => (
                        <li
                          key={String(e?.Id)}
                          role="button"
                          tabIndex={0}
                          onClick={() => setEntitySelectedId(Number(e?.Id))}
                          onKeyDown={(ev) => (ev.key === 'Enter' || ev.key === ' ') && setEntitySelectedId(Number(e?.Id))}
                          className={`px-3 py-2 text-xs cursor-pointer hover:opacity-80 ${entitySelectedId === Number(e?.Id) ? theme.mainContentSection : ''}`}
                        >
                          {e?.Display ?? e?.EntityCode ?? e?.Name ?? String(e?.Id)}
                        </li>
                      ))}
                    </ul>
                  )}
                </div>
              </div>
              <div className={`flex justify-end gap-2 px-4 py-2 border-t ${theme.mainContentSection}`}>
                <button
                  type="button"
                  onClick={() => {
                    setEntitySelectionIndex(null);
                    setEntitySelectedId(null);
                  }}
                  className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                >
                  Close
                </button>
                <button
                  type="button"
                  onClick={() => {
                    if (entitySelectionIndex == null) return;
                    updateOpt(entitySelectionIndex, { EntityId: entitySelectedId ?? null });
                    setEntitySelectionIndex(null);
                    setEntitySelectedId(null);
                  }}
                  className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                >
                  OK
                </button>
              </div>
            </div>
          </div>
        )}
        <div className={`px-3 py-2 border-t flex justify-end gap-2 ${theme.mainContentSection}`}>
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onClose}>
            Cancel
          </button>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            onClick={async () => {
              const ok = await onApply(options);
              if (ok) onClose();
            }}
          >
            Save &amp; Close
          </button>
        </div>
      </div>
    </div>
  );
};

type ItemDetailSettingModalProps = {
  open: boolean;
  theme: ThemeTokens;
  cardDefaultView: any;
  viewFieldOptions: { Id: number | string; SysTableFiledPath?: string }[];
  transactionOptions: { Id: number | string; TransactionName?: string; Name?: string }[];
  onClose: () => void;
  onApply: (patch: {
    needSaveAsFromBaseId: number | null;
    logicKeyFieldId: number | null;
  }) => Promise<boolean>;
};

export const ItemDetailSettingModal: React.FC<ItemDetailSettingModalProps> = ({
  open,
  theme,
  cardDefaultView,
  viewFieldOptions,
  transactionOptions,
  onClose,
  onApply,
}) => {
  const [baseId, setBaseId] = useState<number | null>(null);
  const [logicKeyFieldId, setLogicKeyFieldId] = useState<number | null>(null);

  useEffect(() => {
    if (!open || !cardDefaultView) return;
    setBaseId(null);
    const os = cardDefaultView.OtherSettingsDto ?? {};
    setLogicKeyFieldId(os.LogicKeyFieldId ?? null);
  }, [open, cardDefaultView]);

  if (!open) return null;

  const txId = cardDefaultView?.OtherSettingsDto?.EshopCardViewItemDetailTransactionId;

  return (
    <div
      className="fixed inset-0 z-[1300] bg-black/40 flex items-center justify-center p-4"
      onClick={onClose}
    >
      <div
        className={`w-full max-w-[480px] rounded border shadow-lg ${theme.mainContentSection}`}
        onClick={(e) => e.stopPropagation()}
      >
        <div className={`px-3 py-2 border-b flex items-center justify-between ${theme.mainContentSection}`}>
          <div className={`text-sm font-semibold ${theme.title}`}>Eshop Card View Item Detail Setting</div>
          <button type="button" className={theme.button_default} onClick={onClose}>
            ×
          </button>
        </div>
        <div className="p-4 space-y-3 text-xs">
          <div className={theme.label}>
            Item detail data model (transaction):{' '}
            <span className="font-mono">{txId != null ? String(txId) : '—'}</span>
          </div>
          <label className="flex flex-col gap-1">
            <span className={theme.label}>Create / link from base product data model</span>
            <select
              className={`h-7 px-1 border rounded ${theme.inputBox}`}
              value={baseId != null ? String(baseId) : ''}
              onChange={(e) => setBaseId(e.target.value === '' ? null : Number(e.target.value))}
            >
              <option value="">— select —</option>
              {transactionOptions.map((t) => (
                <option key={String(t.Id)} value={String(t.Id)}>
                  {t.TransactionName || t.Name || t.Id}
                </option>
              ))}
            </select>
          </label>
          <p className={theme.label}>
            Choosing a base model sets <code>NeedToSaveAsFromEshopProductBaseDataModelId</code> and saves; the server
            assigns <code>EshopCardViewItemDetailTransactionId</code> after save-as (Angular parity).
          </p>
          <label className="flex flex-col gap-1">
            <span className={theme.label}>Key view field (maps to data model root PK) — LogicKeyFieldId</span>
            <select
              className={`h-7 px-1 border rounded ${theme.inputBox}`}
              value={logicKeyFieldId != null ? String(logicKeyFieldId) : ''}
              onChange={(e) => setLogicKeyFieldId(e.target.value === '' ? null : Number(e.target.value))}
            >
              <option value="">—</option>
              {viewFieldOptions.map((f) => (
                <option key={String(f.Id)} value={String(f.Id)}>
                  {f.SysTableFiledPath || f.Id}
                </option>
              ))}
            </select>
          </label>
        </div>
        <div className={`px-3 py-2 border-t flex justify-end gap-2 ${theme.mainContentSection}`}>
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onClose}>
            Cancel
          </button>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            onClick={async () => {
              const ok = await onApply({ needSaveAsFromBaseId: baseId, logicKeyFieldId });
              if (ok) onClose();
            }}
          >
            Save &amp; Close
          </button>
        </div>
      </div>
    </div>
  );
};
