import React, { useState, useRef, useEffect, useMemo, useCallback } from 'react';
import { callAiAction, parseImageFieldValue, type AiActionInput } from '../../../../webapi/aiActionSvc';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { useEnumValues } from '../../../../hooks/useEnumDictionary';
import FormItemLayout from './FormItemLayout';
import DataGridLayout from './DataGridLayout';
import { isRuntimeTransactionFieldVisible } from './flexLayoutItemHelper';
import { useFormMasterDetailRuntimeConfig } from '../formMasterDetailRuntimeConfig';
import {
  getLayoutColSpan,
  getLayoutWidgetDisplayType,
  resolveLayoutRowType,
  resolveSectionType,
  resolveTabContainerType,
  shouldRenderRuntimeLayoutItem,
} from './flexLayoutItemHelper';

// ── AI Action auto-map helpers ────────────────────────────────────────────────

const findUnit = (units: any[], targetId: string): any => {
  for (const u of units ?? []) {
    if (String(u.Id) === targetId) return u;
    const found = findUnit(u.AppTransactionUnitList ?? u.Children ?? [], targetId);
    if (found) return found;
  }
  return null;
};

const buildDbNameLookup = (transactionExDto: any, unitId: string): Record<string, string> => {
  const unit = findUnit(transactionExDto?.AppTransactionUnitList ?? [], unitId);
  const lookup: Record<string, string> = {};
  for (const f of unit?.AppTransactionFieldList ?? []) {
    const dbName: string = f.DataBaseFieldName ?? f.dataBaseFieldName ?? '';
    if (!dbName) continue;
    lookup[dbName.toLowerCase().replace(/[\s_-]/g, '')] = dbName;
    const display = (f.DisplayName ?? f.displayName ?? '').toLowerCase().replace(/[\s_-]/g, '');
    if (display) lookup[display] = dbName;
  }
  return lookup;
};

const mapRowToDbColumns = (row: Record<string, any>, lookup: Record<string, string>): Record<string, any> => {
  const out: Record<string, any> = {};
  for (const [k, v] of Object.entries(row)) {
    const normalized = k.toLowerCase().replace(/[\s_-]/g, '');
    out[lookup[normalized] ?? k] = v;
  }
  return out;
};

// ─────────────────────────────────────────────────────────────────────────────

interface OneLayoutItemProps {
  layoutItemExDto: any;
  controllerModel: any;
  dataModel: any;
  onDataModelChange: (dataModel: any) => void;
  transactionExDto?: any;
  /** Row component for sections (breaks circular import with OneLayoutRow) */
  RowComponent?: React.ComponentType<any>;
}

const OneLayoutItem: React.FC<OneLayoutItemProps> = ({
  layoutItemExDto,
  controllerModel,
  dataModel,
  onDataModelChange,
  transactionExDto,
  RowComponent
}) => {
  const { theme, t } = useTheme();
  const runtimeFieldConfig = useFormMasterDetailRuntimeConfig();
  const layoutItemTypeEnum = useEnumValues('EmAppFormLayoutItemType');
  const layoutRowType = resolveLayoutRowType(layoutItemTypeEnum);
  const sectionType = resolveSectionType(layoutItemTypeEnum);
  const tabContainerType = resolveTabContainerType(layoutItemTypeEnum);
  const _gridDisplayTypeEnum = useEnumValues('EmAppTransactionGridDisplayType');
  const [collapsedSections, setCollapsedSections] = useState<Set<string>>(new Set());
  const [activeTabs, setActiveTabs] = useState<Record<string, number>>({});
  const [isAiActionLoading, setIsAiActionLoading] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  // Render section rows (uses RowComponent prop to avoid circular import with OneLayoutRow)
  const renderSectionRows = (sectionRows: any[] | undefined) => {
    if (!sectionRows || sectionRows.length === 0 || !RowComponent) return null;
    const sortedRows = [...sectionRows]
      .filter((row: any) => shouldRenderRuntimeLayoutItem(row))
      .sort((a: any, b: any) => (a.FlowOrGridLayoutSortOrder || 0) - (b.FlowOrGridLayoutSortOrder || 0));
    if (sortedRows.length === 0) return null;
    return (
      <div style={{ width: '100%', position: 'relative' }}>
        {sortedRows.map((layoutRowExDto: any) => {
          const rowWithMode = {
            ...layoutRowExDto,
            GrandChildEditMode: layoutItemExDto.GrandChildEditMode
          };
          return (
            <RowComponent
              key={layoutRowExDto.Id || `section-row-${layoutRowExDto.FlowOrGridLayoutSortOrder}`}
              layoutRowExDto={rowWithMode}
              controllerModel={controllerModel}
              dataModel={dataModel}
              onDataModelChange={onDataModelChange}
              transactionExDto={transactionExDto}
              RowComponent={RowComponent}
            />
          );
        })}
      </div>
    );
  };

  // Reuse HorizontalRowBoundary component from FormLayoutDesignArea (reserved)
  const _HorizontalRowBoundary: React.FC<{
    containerRef: React.RefObject<HTMLDivElement>;
    rowIndex: number;
    insertIndex: number;
    isHovered: boolean;
    onMouseEnter: () => void;
    onMouseLeave: () => void;
    onDragOver: (e: React.DragEvent) => void;
    onDrop: (e: React.DragEvent) => void;
    onClick: (e: React.MouseEvent) => void;
  }> = ({ containerRef, rowIndex, insertIndex, isHovered, onMouseEnter, onMouseLeave, onDragOver, onDrop, onClick }) => {
    const [position, setPosition] = useState<{ top: number; left: number; width: number } | null>(null);
    
    useEffect(() => {
      const updatePosition = () => {
        if (!containerRef.current) return;
        
        const rowElements = Array.from(containerRef.current.querySelectorAll('[data-section-row-index]')) as HTMLElement[];
        const containerRect = containerRef.current.getBoundingClientRect();
        
        if (rowIndex === -1 && rowElements.length > 0) {
          // Top boundary: before first row
          const firstRow = rowElements[0];
          const firstRect = firstRow.getBoundingClientRect();
          const top = firstRect.top - containerRect.top;
          const left = 0;
          const width = containerRect.width;
          setPosition({ top, left, width });
        } else if (rowIndex === rowElements.length && rowElements.length > 0) {
          // Bottom boundary: after last row (rowIndex is sortedRows.length)
          const lastRow = rowElements[rowElements.length - 1];
          const lastRect = lastRow.getBoundingClientRect();
          const top = lastRect.bottom - containerRect.top;
          const left = 0;
          const width = containerRect.width;
          setPosition({ top, left, width });
        } else if (rowIndex >= 0 && rowIndex < rowElements.length - 1) {
          // Between rows (not the last row)
          const currentRow = rowElements[rowIndex];
          const nextRow = rowElements[rowIndex + 1];
          if (currentRow && nextRow) {
            const currentRect = currentRow.getBoundingClientRect();
            const nextRect = nextRow.getBoundingClientRect();
            // Position at the bottom of current row, not in the middle
            const top = currentRect.bottom - containerRect.top;
            const left = 0;
            // Use the width of the current row, not the entire container
            const width = Math.max(currentRect.width, nextRect.width);
            setPosition({ top, left, width });
          } else {
            setPosition(null);
          }
        } else {
          // Invalid rowIndex - don't render
          setPosition(null);
        }
      };
      
      updatePosition();
      window.addEventListener('resize', updatePosition);
      const observer = new MutationObserver(updatePosition);
      if (containerRef.current) {
        observer.observe(containerRef.current, { childList: true, subtree: true, attributes: true });
      }
      
      return () => {
        window.removeEventListener('resize', updatePosition);
        observer.disconnect();
      };
    }, [containerRef, rowIndex]);
    
    if (!position) return null;
    
    return (
      <div
        className="HorizontalRowBoundary"
        style={{
          position: 'absolute',
          top: `${position.top}px`,
          left: `${position.left}px`,
          width: `${position.width}px`,
          height: '8px',
          zIndex: 100,
          pointerEvents: 'auto',
          opacity: isHovered ? 1 : 0,          
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          transform: 'translateY(-4px)',
          cursor: 'pointer',
          backgroundColor: isHovered ? 'rgba(147, 197, 253, 0.2)' : 'transparent'
        }}
        onMouseEnter={(e) => {
          e.stopPropagation();
          onMouseEnter();
        }}
        onMouseLeave={(e) => {
          e.stopPropagation();
          onMouseLeave();
        }}
        onDragOver={(e) => {
          e.preventDefault();
          e.stopPropagation();
          e.dataTransfer.dropEffect = 'copy';
          onDragOver(e);
        }}
        onDrop={(e) => {
          e.preventDefault();
          e.stopPropagation();
          onDrop(e);
        }}
        onClick={(e) => {
          e.stopPropagation();
          onClick(e);
        }}
      >
        {/* Horizontal double lines */}
        <div
          style={{
            position: 'absolute',
            left: '0',
            right: '0',
            top: '0',
            height: '8px',
            zIndex: 101
          }}
        >
          {/* First line */}
          <div
            style={{
              position: 'absolute',
              left: '0',
              right: '0',
              top: '0px',
              height: '2px',
              backgroundColor: '#93c5fd',
            }}
          />
          {/* Second line */}
          <div
            style={{
              position: 'absolute',
              left: '0',
              right: '0',
              top: '5px',
              height: '2px',
              backgroundColor: '#93c5fd',
            }}
          />
        </div>
        {/* Insert button - positioned at left for horizontal boundaries */}
        <button
          className="InsertBoundaryButton"
          style={{
            position: 'absolute',
            top: '50%',
            left: '25px',
            transform: 'translate(-12px, -50%)',
            zIndex: 102,
            width: '24px',
            height: '24px',
            borderRadius: '50%',
            backgroundColor: '#6ee7b7',
            border: '2px solid white',
            color: 'white',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            cursor: 'pointer',
            boxShadow: '0 2px 4px rgba(0,0,0,0.2)',
            pointerEvents: 'auto'
          }}
          onClick={(e) => {
            e.stopPropagation();
            onClick(e);
          }}
          title="Insert new row"
        >
          <i className="fa fa-plus" style={{ fontSize: '12px' }}></i>
        </button>
      </div>
    );
  };

  // DomAttribute / domAttribute (API may return PascalCase or camelCase)
  const domAttribute = layoutItemExDto?.DomAttribute ?? layoutItemExDto?.domAttribute;
  const displayType = getLayoutWidgetDisplayType(layoutItemExDto);

  // Section collapse defaults (must be hooks at component top-level)
  const sectionId = useMemo(() => {
    if (displayType !== sectionType) return null;
    return `SectionBody_${layoutItemExDto.Id}_${controllerModel.uiId}`;
  }, [controllerModel.uiId, displayType, layoutItemExDto.Id, sectionType]);

  const isDefaultCollapsedSection = useMemo(() => {
    if (displayType !== sectionType) return false;
    const da = domAttribute ?? {};
    return Boolean((da.IsCollapsible ?? (da as any).isCollapsible) && (da.IsDefaultCollapsed ?? (da as any).isDefaultCollapsed));
  }, [displayType, domAttribute, sectionType]);

  useEffect(() => {
    if (!sectionId || !isDefaultCollapsedSection) return;
    setCollapsedSections((prev) => {
      if (prev.has(sectionId)) return prev;
      const next = new Set(prev);
      next.add(sectionId);
      return next;
    });
  }, [isDefaultCollapsedSection, sectionId]);

  // Require display type for layout logic (Angular: layoutItem.DomAttribute.WidgetDisplayType.HasValue)
  if (!displayType && displayType !== 0) {
    // Fallback: if we have a bound field, treat as field and render (API may omit DomAttribute in some paths)
    if (layoutItemExDto?.ForeignAppTransactionFieldExDto ?? layoutItemExDto?.foreignAppTransactionFieldExDto) {
      // Render as field with minimal domAttribute
      const fallbackDomAttribute = {
        ColSpanValue: 24,
        DisplayName: (layoutItemExDto?.ForeignAppTransactionFieldExDto ?? layoutItemExDto?.foreignAppTransactionFieldExDto)?.DisplayName ?? '',
        VisibleExpression: 'true',
      };
      const fieldDto = layoutItemExDto?.ForeignAppTransactionFieldExDto ?? layoutItemExDto?.foreignAppTransactionFieldExDto;
      return (
        <div className="w-full h-full relative p-1">
          <FormItemLayout
            layoutItemExDto={{ ...layoutItemExDto, DomAttribute: fallbackDomAttribute, ForeignAppTransactionFieldExDto: fieldDto }}
            controllerModel={controllerModel}
            dataModel={dataModel}
            onDataModelChange={onDataModelChange}
            transactionExDto={transactionExDto}
          />
        </div>
      );
    }
    return null;
  }


  const da = domAttribute || {};
  const boundFieldDto =
    layoutItemExDto.ForeignAppTransactionFieldExDto ?? layoutItemExDto.foreignAppTransactionFieldExDto;
  if (boundFieldDto && !isRuntimeTransactionFieldVisible(boundFieldDto)) {
    return null;
  }

  const Row = RowComponent;
  if (displayType === layoutRowType && Row) {
    return (
      <Row
        layoutRowExDto={layoutItemExDto}
        controllerModel={controllerModel}
        dataModel={dataModel}
        onDataModelChange={onDataModelChange}
        transactionExDto={transactionExDto}
        RowComponent={Row}
      />
    );
  }

  // Build style from layout info (support PascalCase from API)
  let styleLayoutInfo = '';
  const heightVal = da.HeightValue ?? (da as any).heightValue;
  if (heightVal) {
    if (displayType === sectionType) {
      styleLayoutInfo += `min-height:${heightVal}px;`;
    } else {
      styleLayoutInfo += `height:${heightVal}px;`;
    }
  }
  const bgColor = da.BackgroundColor ?? (da as any).backgroundColor;
  if (bgColor) {
    styleLayoutInfo += `background-color:${String(bgColor).trim()};`;
  }
  const textColor = da.TextColor ?? (da as any).textColor;
  if (textColor) {
    styleLayoutInfo += `color:${String(textColor).trim()};`;
  }

  // Get col span (default 24)
  const colSpan = getLayoutColSpan(layoutItemExDto);

  // Check visibility
  const transFieldExDto = layoutItemExDto.ForeignAppTransactionFieldExDto ?? layoutItemExDto.foreignAppTransactionFieldExDto;
  const isHideBinding = transFieldExDto 
    ? false // TODO: Implement GetNeedToHideBindField logic
    : false;
  
  const isVisibleExpression = (da.VisibleExpression ?? (da as any).visibleExpression) || 'true';
  // TODO: Evaluate isVisibleExpression dynamically
  
  if (isHideBinding || isVisibleExpression === 'false') {
    return null;
  }

  // Toggle section collapse
  const toggleSection = (sectionId: string) => {
    setCollapsedSections(prev => {
      const newSet = new Set(prev);
      if (newSet.has(sectionId)) {
        newSet.delete(sectionId);
      } else {
        newSet.add(sectionId);
      }
      return newSet;
    });
  };

  const isSectionCollapsed = (sectionId: string) => {
    return collapsedSections.has(sectionId);
  };

  // Set active tab
  const setActiveTab = (tabContainerId: string, tabId: number) => {
    setActiveTabs(prev => ({
      ...prev,
      [tabContainerId]: tabId
    }));
  };

  const isTabActive = (tabId: number, tabContainerId: string, defaultTabId: number) => {
    const activeTabId = activeTabs[tabContainerId] ?? defaultTabId;
    return activeTabId === tabId;
  };

  // Render based on display type
  const renderContent = () => {
    // Section
    if (displayType === sectionType) {
      const effectiveSectionId = sectionId ?? `SectionBody_${layoutItemExDto.Id}_${controllerModel.uiId}`;
      const isCollapsed = isSectionCollapsed(effectiveSectionId);
      const shouldShowBody = !isCollapsed;

      return (
        <div style={{ width: '100%' }} className="w-full">
          {/* Section header - only show when collapsible */}
          {(da.IsCollapsible ?? (da as any).isCollapsible) && (
            <div
              className={`NoSelect Ctn-SectionHeader cursor-pointer flex items-center justify-between px-3 py-2 text-sm border-b relative z-30 ${t('border_mainContentSection')} ${theme.mainContentSection} bg-[#00000008]`}
              style={{ zIndex: 30 }}
              onClick={(e) => {
                toggleSection(effectiveSectionId);
                e.stopPropagation();
                e.preventDefault();
              }}
            >
              <span>{(da.DisplayName ?? (da as any).displayName) || 'Stack Container'}</span>
              <div className="px-2 text-xs">
                {!isCollapsed ? (
                  <i className="fa fa-chevron-circle-up"></i>
                ) : (
                  <i className="fa fa-chevron-circle-down"></i>
                )}
              </div>
            </div>
          )}
          {shouldShowBody && renderSectionRows(layoutItemExDto.AppFormLayoutItem_List ?? layoutItemExDto.appFormLayoutItem_List)}
        </div>
      );
    }
    
    // Tab Container
    if (displayType === tabContainerType) {
      const tabList = layoutItemExDto.AppFormLayoutItem_List ?? layoutItemExDto.appFormLayoutItem_List;
      const tabItems = tabList
        ? [...(Array.isArray(tabList) ? tabList : [])].sort((a: any, b: any) => (a.FlowOrGridLayoutSortOrder ?? a.flowOrGridLayoutSortOrder ?? 0) - (b.FlowOrGridLayoutSortOrder ?? b.flowOrGridLayoutSortOrder ?? 0))
        : [];
      
      if (tabItems.length === 0) return null;
      
      const defaultTabId = tabItems[0].Id;
      const tabContainerId = layoutItemExDto.Id.toString();

      return (
        <div className="FormTabContainer w-full">
          <div className={`FormTabHeader flex border-b ${t('border_default')}`}>
            {tabItems.map((aLayoutTab: any) => {
              const _tabWithMode = {
                ...aLayoutTab,
                GrandChildEditMode: layoutItemExDto.GrandChildEditMode
              };
              const isActive = isTabActive(aLayoutTab.Id, tabContainerId, defaultTabId);

              return (
                <div
                  key={aLayoutTab.Id}
                  className={`FormTabButton relative cursor-pointer border-r px-4 py-1 text-xs transition-colors duration-200 ${
                    isActive ? `border-b-2 ${theme.tab_active}` : `${theme.tab}`
                  }`}
                  style={isActive ? { color: theme.param.text_tab_active } : undefined}
                  onClick={() => setActiveTab(tabContainerId, aLayoutTab.Id)}
                  role="tab"
                  aria-selected={isActive}
                >
                  {(aLayoutTab.DomAttribute ?? aLayoutTab.domAttribute)?.DisplayName ??
                    (aLayoutTab.DomAttribute ?? aLayoutTab.domAttribute)?.displayName ??
                    ''}
                </div>
              );
            })}
            <div className={`w-1 flex-auto border-b ${t('border_default')}`} aria-hidden />
          </div>
          <div className="w-full p-1">
            {tabItems.map((layoutTabSectionExDto: any) => {
              const isActive = isTabActive(layoutTabSectionExDto.Id, tabContainerId, defaultTabId);
              if (!isActive) return null;
              
              return (
                <div key={layoutTabSectionExDto.Id} className="w-full">
                  <OneLayoutItem
                    layoutItemExDto={layoutTabSectionExDto}
                    controllerModel={controllerModel}
                    dataModel={dataModel}
                    onDataModelChange={onDataModelChange}
                    transactionExDto={transactionExDto}
                    RowComponent={RowComponent}
                  />
                </div>
              );
            })}
          </div>
        </div>
      );
    }
    
    // Grid (Data Grid)
    if (displayType === layoutItemTypeEnum?.Grid) {
      let unitExDto = layoutItemExDto.ForeignAppTransactionUnitExDto;
      
      // Try to get unit from transactionExDto if ForeignAppTransactionUnitExDto is not available
      if (!unitExDto && transactionExDto && layoutItemExDto.GridTransactionUnitId) {
        // Try to find unit in transaction data
        const unitId = layoutItemExDto.GridTransactionUnitId;
        if (transactionExDto.AppTransactionUnitList) {
          // Search in root units
          unitExDto = transactionExDto.AppTransactionUnitList.find((unit: any) => unit.Id === unitId);
          
          // If not found, search in children
          if (!unitExDto) {
            transactionExDto.AppTransactionUnitList.forEach((rootUnit: any) => {
              if (rootUnit.Children) {
                const found = rootUnit.Children.find((child: any) => child.Id === unitId);
                if (found) {
                  unitExDto = found;
                }
              }
            });
          }
        }
      }
      
      if (!unitExDto) {
        // Runtime mode: return null if unit not found
        return null;
      }

      const rawGridDisplay =
        unitExDto?.EmGridViewDisplayType ?? (unitExDto as any)?.emGridViewDisplayType;
      const gridViewDisplayType = Number(rawGridDisplay);
      const isAvailableSelectRuntime =
        Number.isFinite(gridViewDisplayType) && (gridViewDisplayType === 5 || gridViewDisplayType === 6);
      
      return (
          <div className="w-full h-full flex flex-row items-start gap-1">
              <div className={`w-1 flex-auto h-full overflow-hidden ${isAvailableSelectRuntime ? 'flex flex-col' : ''}`}
                  style={{ position: 'relative' }}>
                   <DataGridLayout
                     unitExDto={unitExDto}
                     unitId={unitExDto.Id}
                     dataModel={dataModel}
                     controllerModel={controllerModel}
                     transactionExDto={transactionExDto}
                     onDataModelChange={onDataModelChange}
                   />
            </div>
          {controllerModel.isEnableFormConfigButtons && (
            <div className="flex-none self-start pt-9 shrink-0">
              <button
                type="button"
                className={`flex h-7 w-7 items-center justify-center rounded-[4px] text-xs ${theme.button_default}`}
                title="Edit Field / Unit"
                onClick={(e) =>
                  runtimeFieldConfig.openUnitContextMenu(
                    e,
                    Number(unitExDto.Id),
                    String(unitExDto.UnitDisplayName || unitExDto.UnitName || ''),
                    layoutItemExDto.Id ?? layoutItemExDto.CurrentHostId ?? ''
                  )
                }
              >
                <i className="fa-solid fa-gear" aria-hidden />
              </button>
            </div>
          )}
        </div>
      );
    }
    
    // NewItemAddButton (Placeholder) - like AngularJS
    // NewItemAddButton (Placeholder) - runtime doesn't need this, only design mode
    if (displayType === layoutItemTypeEnum?.NewItemAddButton) {
      return null;
    }
    
    // Content or Space
    if (displayType === layoutItemTypeEnum?.Content || displayType === layoutItemTypeEnum?.Space) {
      // TODO: Implement FlexLayoutLiteralContent component
      return (
        <div className="w-full p-2">
          <div className="text-sm text-gray-400">
            {(da.DisplayName ?? (da as any).displayName) || ''}
            {/* TODO: Render literal content */}
          </div>
        </div>
      );
    }
    
    // Field or other types
    const aAppTransactionFieldExDto = layoutItemExDto.ForeignAppTransactionFieldExDto;
    
    if (aAppTransactionFieldExDto) {
      return (
        <div className="flex h-full w-full min-w-0 flex-row items-start gap-1">
          <div className="w-1 h-full min-w-0 flex-auto">
            <FormItemLayout
              layoutItemExDto={layoutItemExDto}
              controllerModel={controllerModel}
              dataModel={dataModel}
              onDataModelChange={onDataModelChange}
              transactionExDto={transactionExDto}
            />
          </div>
          {controllerModel.isEnableFormConfigButtons && (
            <button
              type="button"
              className={`flex h-7 w-7 shrink-0 items-center justify-center self-start rounded-[4px] text-xs ${theme.button_default}`}
              title="Field Setting"
              onClick={(e) =>
                runtimeFieldConfig.openTransactionFieldEditor(
                  e,
                  Number(aAppTransactionFieldExDto.Id),
                  String(aAppTransactionFieldExDto.DisplayName || aAppTransactionFieldExDto.LabelDisplayBinding || ''),
                  layoutItemExDto.Id ?? layoutItemExDto.CurrentHostId ?? '',
                  false
                )
              }
            >
              <i className="fa-solid fa-gear" aria-hidden />
            </button>
          )}
        </div>
      );
    }
    
    // Command Action Button
    if (displayType === layoutItemTypeEnum?.CommandActionButton) {
      const cmdActionId = da.CommandActionId ?? (da as any).commandActionId;
      const cmdAction = transactionExDto?.CommandActionList?.find(
        (c: any) => String(c.Id) === String(cmdActionId)
      );

      const label = (da.DisplayName ?? (da as any).displayName)
        || cmdAction?.DisplayName
        || cmdAction?.displayName
        || 'Command Button';

      // ActionType 56 = AI_ACTION — driven by AppAISkill + ActionAttribute config
      const isAiAction = cmdAction && (
        (cmdAction.ActionType ?? cmdAction.actionType) === 56
      );

      const handleAiActionClick = async () => {
        if (!cmdAction) return;
        const cfg = (() => {
          try {
            const raw = cmdAction.ActionAttribute ?? cmdAction.actionAttribute;
            if (!raw) return {};
            return typeof raw === 'string' ? JSON.parse(raw) : raw;
          } catch { return {}; }
        })();

        const { inputBindings = [], skillName = '', outputBindings = [] } = cfg as {
          inputBindings: Array<{ fieldName: string; inputType: 'text' | 'image' }>;
          skillName: string;
          outputBindings: Array<{ outputKey: string; targetType: 'text_field' | 'child_grid'; targetName: string }>;
        };

        if (!skillName) {
          alert('AI Action: skillName is missing in ActionAttribute config.');
          return;
        }

        const currentFormData = dataModel?.currentFormData ?? dataModel;
        const inputs: AiActionInput[] = inputBindings.map((b) => {
          const raw = currentFormData?.DictOneToOneFields?.[b.fieldName];
          if (b.inputType === 'image') {
            const parsed = parseImageFieldValue(raw);
            return parsed
              ? { inputType: 'image', imageBase64: parsed.base64, mimeType: parsed.mimeType }
              : { inputType: 'text', textValue: '' };
          }
          return { inputType: 'text', textValue: raw != null ? String(raw) : '' };
        });

        setIsAiActionLoading(true);
        try {
          const result = await callAiAction(inputs, skillName);
          const parsed = JSON.parse(result.rawJson);

          if (outputBindings.length > 0) {
            let updatedFormData = { ...(currentFormData ?? {}) };
            for (const ob of outputBindings) {
              const value = parsed[ob.outputKey];
              if (value === undefined) continue;

              if (ob.targetType === 'text_field') {
                updatedFormData = {
                  ...updatedFormData,
                  DictOneToOneFields: {
                    ...(updatedFormData.DictOneToOneFields ?? {}),
                    [ob.targetName]: typeof value === 'string' ? value : JSON.stringify(value),
                  },
                  IsDirty: true,
                };
              } else if (ob.targetType === 'child_grid' && Array.isArray(value)) {
                const unitKey = String(ob.targetName);
                const lookup = buildDbNameLookup(transactionExDto, unitKey);
                const mappedRows = value.map((row: any) => mapRowToDbColumns(row, lookup));
                updatedFormData = {
                  ...updatedFormData,
                  DictOneToManyFields: {
                    ...(updatedFormData.DictOneToManyFields ?? {}),
                    [unitKey]: mappedRows,
                  },
                  IsDirty: true,
                };
              }
            }

            const updatedModel = dataModel?.currentFormData != null
              ? { ...dataModel, currentFormData: updatedFormData }
              : updatedFormData;
            onDataModelChange(updatedModel);
          }

          if (result.warnings) {
            // Non-blocking advisory — user can proceed
            console.warn('[AI Action]', result.warnings);
          }
        } catch (err: any) {
          alert(`AI Action failed: ${err?.message ?? 'Unknown error'}`);
        } finally {
          setIsAiActionLoading(false);
        }
      };

      return (
        <div className="w-full p-2">
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} ${isAiActionLoading ? 'opacity-60 cursor-not-allowed' : ''}`}
            onClick={isAiAction ? handleAiActionClick : undefined}
            disabled={isAiActionLoading}
            title={isAiAction ? `AI Action: ${cmdAction?.ActionAttribute ?? ''}` : label}
          >
            {isAiActionLoading ? (
              <span className="flex items-center gap-1">
                <i className="fa-solid fa-spinner fa-spin text-xs" aria-hidden />
                Processing...
              </span>
            ) : (
              <span className="flex items-center gap-1">
                {isAiAction && <i className="fa-solid fa-wand-magic-sparkles text-xs" aria-hidden />}
                {label}
              </span>
            )}
          </button>
        </div>
      );
    }
    
    // Linked Search
    if (displayType === layoutItemTypeEnum?.LinkedSearch) {
      // TODO: Implement LinkedSearchBox component
      return (
        <div className="w-full p-2">
          <div className="text-sm text-gray-500">
            Linked Search: {(da.DisplayName ?? (da as any).displayName) || 'Search'}
            {/* TODO: Render linked search box */}
          </div>
        </div>
      );
    }
    
    return null;
  };

  // Parse style string to object
  const styleObject: React.CSSProperties = { cursor: 'default' };
  if (styleLayoutInfo) {
    const styles = styleLayoutInfo.split(';').filter(s => s.trim());
    styles.forEach(style => {
      const [key, value] = style.split(':').map(s => s.trim());
      if (key && value) {
        const camelKey = key.replace(/-([a-z])/g, (g) => g[1].toUpperCase());
        (styleObject as any)[camelKey] = value;
      }
    });
  }

  // Runtime mode - no design mode features
  let borderClass = 'border-2 border-transparent';

  const content = renderContent();
  const isContainerType = displayType === sectionType || displayType === tabContainerType;
  if (content == null && !isContainerType) {
    return null;
  }

  return (
    <div
      ref={containerRef}
      className={`LayoutItemContainer LayoutItemRuntimeOrder${layoutItemExDto.ItemRuntimeOrder} CSpan_${colSpan} ${borderClass}`}
      style={{ ...styleObject, position: 'relative', backgroundColor: 'transparent' }}
      data-item-id={layoutItemExDto.Id || layoutItemExDto.CurrentHostId}
      data-host-id={layoutItemExDto.CurrentHostId}
    >
      {content}
    </div>
  );
};

export default OneLayoutItem;

