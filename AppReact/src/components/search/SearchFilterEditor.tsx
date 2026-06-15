/**
 * Shared Search Filter Editor – filter grid for search/view configuration.
 * Based on TransactionGroupEditor Template Filters tab (full: advanced options, Entity List of Value cell template).
 * Used by: TransactionGroupEditor (Template Filters tab), SearchEditor (Filters section).
 * When enableAdvancedOptions is false, renders a simpler grid (Field Name, Display Name, Control Type, Operation, Default Value, Visible, Read Only).
 */
import React, { useCallback, useEffect, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';

const CONTROL_TYPE_DDL = 1;

export interface SearchFilterEditorProps {
  /** Current filter rows (AppSearchFieldList). */
  filterList: any[];
  /** Callback when user adds a filter (parent pushes new row with correct shape). */
  onAddFilter: () => void;
  /** Callback when user removes a filter; component passes selected row item. */
  onRemoveFilter: (item: any) => void;
  /** Callback when any cell edit or list change marks dirty. */
  onMarkChange: () => void;
  /** Field Name column: DataMap from dataset columns (Id -> Id or Display). */
  filterColumnsDataMap: DataMap | null;
  /** Control Type column. */
  controlTypeDataMap: DataMap | null;
  /** Operation column. */
  criteriaOperatorDataMap: DataMap | null;
  /** When true, show Options (Show Advanced Columns), Entity List of Value cell template, and all advanced columns. */
  enableAdvancedOptions?: boolean;
  /** Entity list for Entity List of Value display + Datasource Selector (required when enableAdvancedOptions). */
  entityListForFilter?: { Id: number; Display?: string; Name?: string; Code?: string }[];
  /** Resolve entity Id to display text (required when enableAdvancedOptions). */
  getEntityCodeById?: (entityId: number | null | undefined) => string;
  /** Open Datasource Selector for this row (required when enableAdvancedOptions). */
  onOpenDatasourceSelector?: (item: any) => void;
  /** Open Entity Data Preview (required when enableAdvancedOptions). */
  onOpenEntityDataPreview?: (entityId: number | null | undefined) => void;
  /** Sub Control Type column (when enableAdvancedOptions). */
  subControlTypeDataMap?: DataMap | null;
  /** Internal Code column (when enableAdvancedOptions). */
  internalCodeDataMap?: DataMap | null;
  /** Resolve search field Id to display text (for Cascading/Inner Relation columns). */
  getSearchFieldNameById?: (fieldId: number | null | undefined) => string;
  /** Open Cascading Setting popup for this row. */
  onOpenCascadingConfig?: (item: any) => void;
  /** Open Inner Relationship popup for this row. */
  onOpenInnerRelationConfig?: (item: any) => void;
  /** Optional warning callback (e.g. "Select a filter row to remove"). */
  showWarning?: (message: string) => void;
  /** Message when filter list is empty. */
  emptyMessage?: string;
  /** Min height for grid container. */
  minHeight?: number;
  /** Optional ref to attach to the grid (e.g. for parent to read selection). */
  gridRef?: React.RefObject<any>;
  /** When true, remove extra margin around the "Filters" title row (Search Editor compact layout). */
  compactFilterHeader?: boolean;
  /** Clamp total height (header + grid). Grid area scrolls when content exceeds max. */
  sectionMinHeightPx?: number;
  sectionMaxHeightPx?: number;
  /** When true, grid area is flex-1 min-h-0 and grid height 100% (e.g. Search Editor 50/50 split). */
  flexFillParent?: boolean;
}

const SearchFilterEditor: React.FC<SearchFilterEditorProps> = ({
  filterList,
  onAddFilter,
  onRemoveFilter,
  onMarkChange,
  filterColumnsDataMap,
  controlTypeDataMap,
  criteriaOperatorDataMap,
  enableAdvancedOptions = false,
  entityListForFilter = [],
  getEntityCodeById = () => '',
  onOpenDatasourceSelector,
  onOpenEntityDataPreview,
  subControlTypeDataMap = null,
  internalCodeDataMap = null,
  getSearchFieldNameById = () => '',
  onOpenCascadingConfig,
  onOpenInnerRelationConfig,
  showWarning = () => {},
  emptyMessage = 'No filters. Click "Add Filter" to add one.',
  minHeight = 120,
  gridRef: gridRefProp,
  compactFilterHeader = false,
  sectionMinHeightPx,
  sectionMaxHeightPx,
  flexFillParent = false
}) => {
  const { theme } = useTheme();
  const sectionHeightClamped = sectionMaxHeightPx != null && sectionMaxHeightPx > 0;
  const gridAreaFlexFill = sectionHeightClamped || flexFillParent;
  const internalGridRef = useRef<any>(null);
  const gridRef = gridRefProp ?? internalGridRef;
  const optionsRef = useRef<HTMLDivElement>(null);

  const [filterCV, setFilterCV] = useState<CollectionView<any> | null>(null);
  const [showAdvancedColumns, setShowAdvancedColumns] = useState(false);
  const [optionsOpen, setOptionsOpen] = useState(false);

  useEffect(() => {
    setFilterCV(new CollectionView<any>(filterList ?? []));
  }, [filterList]);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (optionsRef.current && !optionsRef.current.contains(e.target as Node)) setOptionsOpen(false);
    };
    if (optionsOpen) document.addEventListener('click', handleClickOutside);
    return () => document.removeEventListener('click', handleClickOutside);
  }, [optionsOpen]);

  const handleRemoveFilter = useCallback(() => {
    const flex = gridRef?.current?.control ?? gridRef?.current;
    const row = flex?.selection?.row ?? -1;
    if (row < 0 || !flex?.rows?.[row]) {
      showWarning('Select a filter row to remove');
      return;
    }
    const item = flex.rows[row].dataItem;
    onRemoveFilter(item);
  }, [gridRef, onRemoveFilter, showWarning]);

  const sectionStyle =
    sectionHeightClamped && sectionMaxHeightPx != null
      ? {
          minHeight: sectionMinHeightPx ?? 100,
          maxHeight: sectionMaxHeightPx
        }
      : undefined;

  return (
    <div
      className={`w-full flex flex-col min-h-0 overflow-hidden ${sectionHeightClamped ? '' : 'flex-1'}`}
      style={sectionStyle}
    >
      <div
        className={`flex items-center justify-between px-3 ${theme.mainContentSection} ${
          compactFilterHeader ? 'pb-1.5 pt-0 mb-0 mt-0' : 'mb-1 mt-2'
        }`}
      >
        <div className={`pl-1 text-sm font-semibold shrink-0 ${theme.title}`}>Filters</div>
        <div className="flex items-center gap-2 shrink-0">
          <button type="button" onClick={onAddFilter} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
            <i className="fa-solid fa-plus mr-1" aria-hidden /> Add Filter
          </button>
          <button type="button" onClick={handleRemoveFilter} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
            <i className="fa-solid fa-minus mr-1" aria-hidden /> Remove Filter
          </button>
          {enableAdvancedOptions && (
            <div className="relative" ref={optionsRef}>
              <button
                type="button"
                onClick={() => setOptionsOpen((v) => !v)}
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} inline-flex items-center gap-1`}
              >
                <i className="fa-solid fa-gears" aria-hidden /> Options <i className="fa-solid fa-caret-down text-[10px]" aria-hidden />
              </button>
              {optionsOpen && (
                <ul className={`absolute right-0 top-full mt-1 py-1 min-w-[180px] rounded shadow-lg z-10 ${theme.mainContentSection} border`}>
                  <li>
                    <button
                      type="button"
                      className={`w-full text-left px-3 py-1.5 text-sm flex items-center gap-2 ${theme.title} hover:opacity-90`}
                      onClick={() => {
                        setShowAdvancedColumns((v) => !v);
                        setOptionsOpen(false);
                      }}
                    >
                      {showAdvancedColumns && <i className="fa-solid fa-check" aria-hidden />}
                      Show Advanced Columns
                    </button>
                  </li>
                </ul>
              )}
            </div>
          )}
        </div>
      </div>
      <div
        className={`w-full overflow-auto flex flex-col ${theme.mainContentSection} ${
          gridAreaFlexFill ? 'flex-1 min-h-0' : 'flex-1 min-h-[200px]'
        }`}
      >
        {filterCV && (enableAdvancedOptions ? (
          <FlexGrid
            ref={gridRef}
            itemsSource={filterCV}
            selectionMode="Row"
            style={
              gridAreaFlexFill
                ? { height: '100%', minHeight: 0, border: 'none' }
                : { minHeight, border: 'none' }
            }
            cellEditEnded={onMarkChange}
          >
            <FlexGridColumn binding="Sort" header="Sort" width={50} visible={showAdvancedColumns} />
            <FlexGridColumn binding="SysTableFiledPath" header="Field Name" width={180} dataMap={filterColumnsDataMap ?? undefined} />
            <FlexGridColumn binding="DisplayText" header="Display Name" width={160} />
            <FlexGridColumn binding="ControlType" header="Control Type" width={100} dataMap={controlTypeDataMap ?? undefined} />
            <FlexGridColumn binding="EntityId" header="Entity List of Value" width={200} isReadOnly={true}>
              <FlexGridCellTemplate
                cellType="Cell"
                template={(cell: any) => {
                  const item = cell.item;
                  if (!item) return null;
                  const isDDL = item.ControlType === CONTROL_TYPE_DDL;
                  const entityDisplay = getEntityCodeById(item.EntityId);
                  return (
                    <div className="flex items-center gap-1 w-full min-w-0">
                      {isDDL && onOpenDatasourceSelector && (
                        <button
                          type="button"
                          onClick={(e) => {
                            e.stopPropagation();
                            onOpenDatasourceSelector(item);
                          }}
                          className={`shrink-0 w-7 h-6 flex items-center justify-center rounded ${theme.button_default} text-xs`}
                          title="Select datasource"
                          aria-label="Select datasource"
                        >
                          <i className="fa-solid fa-search" aria-hidden />
                        </button>
                      )}
                      {item.EntityId != null && onOpenEntityDataPreview && (
                        <button
                          type="button"
                          onClick={(e) => {
                            e.stopPropagation();
                            onOpenEntityDataPreview(item.EntityId);
                          }}
                          className={`shrink-0 w-7 h-6 flex items-center justify-center rounded ${theme.button_default} text-xs`}
                          title="Entity Data Preview"
                          aria-label="Entity Data Preview"
                        >
                          <i className="fa-solid fa-database" aria-hidden />
                        </button>
                      )}
                      <span className={`truncate text-xs ${theme.label}`} title={entityDisplay}>
                        {entityDisplay || ''}
                      </span>
                    </div>
                  );
                }}
              />
            </FlexGridColumn>
            <FlexGridColumn binding="EntityId" header="EntityId" width={100} visible={showAdvancedColumns} />
            <FlexGridColumn binding="OperationId" header="Operation" width={100} dataMap={criteriaOperatorDataMap ?? undefined} />
            <FlexGridColumn binding="DefaultValue" header="Default Value" width={120} />
            <FlexGridColumn binding="PositionRow" header="Row" width={70} />
            <FlexGridColumn binding="PositionColumn" header="Column" width={70} />
            <FlexGridColumn binding="IsVisible" header="Is Visible" width={80} dataType="Boolean" />
            <FlexGridColumn binding="IsReadOnly" header="Is Read Only" width={90} dataType="Boolean" />
            <FlexGridColumn binding="SubControlType" header="Sub Control Type" width={130} dataMap={subControlTypeDataMap ?? undefined} visible={showAdvancedColumns} />
            <FlexGridColumn binding="ParentFieldId" header="Cascading Parent" width={150} isReadOnly={true} visible={showAdvancedColumns}>
              <FlexGridCellTemplate
                cellType="Cell"
                template={(cell: any) => {
                  const item = cell.item;
                  if (!item) return null;
                  const parentName = getSearchFieldNameById(item.ParentFieldId);
                  const isDDL = item.ControlType === CONTROL_TYPE_DDL;
                  return (
                    <div className="flex items-center gap-1 w-full min-w-0">
                      {isDDL && onOpenCascadingConfig && (
                        <button
                          type="button"
                          onClick={(e) => {
                            e.stopPropagation();
                            onOpenCascadingConfig(item);
                          }}
                          className={`shrink-0 w-7 h-6 flex items-center justify-center rounded ${theme.button_default} text-xs`}
                          title="Cascading Setting"
                          aria-label="Cascading Setting"
                        >
                          <i className="fa-solid fa-plus" aria-hidden />
                        </button>
                      )}
                      <span className={`truncate text-xs ${theme.label}`} title={parentName}>
                        {parentName || ''}
                      </span>
                    </div>
                  );
                }}
              />
            </FlexGridColumn>
            <FlexGridColumn binding="MasterEntityFieldlId" header="Inner Relation Parent" width={150} isReadOnly={true} visible={showAdvancedColumns}>
              <FlexGridCellTemplate
                cellType="Cell"
                template={(cell: any) => {
                  const item = cell.item;
                  if (!item) return null;
                  const masterName = getSearchFieldNameById(item.MasterEntityFieldlId);
                  return (
                    <div className="flex items-center gap-1 w-full min-w-0">
                      {onOpenInnerRelationConfig && (
                        <button
                          type="button"
                          onClick={(e) => {
                            e.stopPropagation();
                            onOpenInnerRelationConfig(item);
                          }}
                          className={`shrink-0 w-7 h-6 flex items-center justify-center rounded ${theme.button_default} text-xs`}
                          title="Inner Relationship"
                          aria-label="Inner Relationship"
                        >
                          <i className="fa-solid fa-plus" aria-hidden />
                        </button>
                      )}
                      <span className={`truncate text-xs ${theme.label}`} title={masterName}>
                        {masterName || ''}
                      </span>
                    </div>
                  );
                }}
              />
            </FlexGridColumn>
            <FlexGridColumn binding="InnerEntitySubscribeFiled" header="Inner Entity Subscribe Filed" width={180} isReadOnly={true} visible={showAdvancedColumns} />
            <FlexGridColumn binding="IsFilterByCurrentUser" header="Is Filter By Current User" width={150} dataType="Boolean" visible={showAdvancedColumns} />
            <FlexGridColumn binding="IsAutoPopulate" header="Is Auto Populate" width={100} dataType="Boolean" visible={showAdvancedColumns} />
            <FlexGridColumn binding="IsLoadOnDemand" header="Is Load On Demand" width={100} dataType="Boolean" visible={showAdvancedColumns} />
            <FlexGridColumn binding="IsChangedAutoExecute" header="IsChangedAutoExecute" width={180} dataType="Boolean" visible={showAdvancedColumns} />
            <FlexGridColumn binding="IsAllowMultipleSelect" header="Is Multiple Select" width={140} dataType="Boolean" visible={showAdvancedColumns} />
            <FlexGridColumn binding="IsSkipSearch" header="Is Skip Search" width={100} dataType="Boolean" visible={showAdvancedColumns} />
            <FlexGridColumn binding="EmInternalCodeRegistration" header="Internal Code" width={300} dataMap={internalCodeDataMap ?? undefined} visible={showAdvancedColumns} />
            <FlexGridColumn header="" binding="" width="*" allowSorting={false} isReadOnly={true} />
          </FlexGrid>
        ) : (
          <FlexGrid
            ref={gridRef}
            itemsSource={filterCV}
            selectionMode="Row"
            style={
              gridAreaFlexFill
                ? { height: '100%', minHeight: 0, border: 'none' }
                : { minHeight, border: 'none' }
            }
            cellEditEnded={onMarkChange}
          >
            <FlexGridColumn binding="SysTableFiledPath" header="Field Name" width={180} dataMap={filterColumnsDataMap ?? undefined} />
            <FlexGridColumn binding="DisplayText" header="Display Name" width={160} />
            <FlexGridColumn binding="ControlType" header="Control Type" width={100} dataMap={controlTypeDataMap ?? undefined} />
            <FlexGridColumn binding="OperationId" header="Operation" width={100} dataMap={criteriaOperatorDataMap ?? undefined} />
            <FlexGridColumn binding="DefaultValue" header="Default Value" width={120} />
            <FlexGridColumn binding="IsVisible" header="Visible" width={70} dataType="Boolean" />
            <FlexGridColumn binding="IsReadOnly" header="Read Only" width={80} dataType="Boolean" />
            <FlexGridColumn header="" binding="" width="*" allowSorting={false} isReadOnly={true} />
          </FlexGrid>
        ))}
        {filterCV && (!filterList?.length) && (
          <div className={`text-xs py-2 px-8 ${theme.label}`}>{emptyMessage}</div>
        )}
      </div>
    </div>
  );
};

export default SearchFilterEditor;
