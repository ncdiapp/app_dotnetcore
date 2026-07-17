import React, { useCallback, useMemo, useState } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useEnumValues } from '../../../hooks/useEnumDictionary';

interface GridDesignLayoutProps {
  layoutItemExDto: any;
  unitExDto: any;
  controllerModel: any;
  currentLayoutItem?: any;
  onLayoutItemClick?: (layoutItem: any) => void;
  onOpenRowItemContextMenu?: (event: React.MouseEvent, layoutItem: any) => void;
}

type GridField = {
  Id?: number;
  DataBaseFieldName?: string;
  DisplayName?: string;
  LabelDisplayBinding?: string;
  DisplayWidth?: number | string;
  SortOrder?: number;
  IsFormLayoutVisible?: boolean;
  ControlType?: number;
};

const MULTI_SELECT_PLACEHOLDER_OPTIONS = [
  { label: 'Option A', checked: true },
  { label: 'Option B', checked: false },
  { label: 'Option C', checked: true },
  { label: 'Option D', checked: false }
];

export const GridDesignLayout: React.FC<GridDesignLayoutProps> = ({
  layoutItemExDto,
  unitExDto,
  controllerModel,
  currentLayoutItem,
  onLayoutItemClick,
  onOpenRowItemContextMenu
}) => {
  const { theme } = useTheme();
  const gridDisplayTypeEnum = useEnumValues('EmAppTransactionGridDisplayType');
  const [hoveredFieldId, setHoveredFieldId] = useState<number | undefined>(undefined);

  const isMultipleSelectBox =
    Number(unitExDto?.EmGridViewDisplayType) ===
    Number(gridDisplayTypeEnum?.MultipleSelectBox ?? 6);

  const getFieldId = useCallback((field: GridField): number | undefined => {
    const anyField = field as any;
    const id = field?.Id ?? anyField?.TransactionFieldId ?? anyField?.FieldId;
    if (id === null || id === undefined) return undefined;
    const n = typeof id === 'number' ? id : parseInt(id.toString(), 10);
    return Number.isFinite(n) ? n : undefined;
  }, []);

  const fields: GridField[] = useMemo(() => {
    const list: GridField[] = unitExDto?.AppTransactionFieldList || [];
    return list
      .filter((f: GridField) => f.IsFormLayoutVisible === undefined || f.IsFormLayoutVisible === true)
      .sort((a: GridField, b: GridField) => (a.SortOrder || 0) - (b.SortOrder || 0));
    // Recompute when editing selected column so design panel reflects changes immediately
  }, [
    unitExDto?.AppTransactionFieldList,
    currentLayoutItem?.__isGridColumn,
    currentLayoutItem?.TransactionFieldId,
    currentLayoutItem?.ForeignAppTransactionFieldExDto?.DisplayName,
    currentLayoutItem?.ForeignAppTransactionFieldExDto?.DisplayWidth,
    currentLayoutItem?.ForeignAppTransactionFieldExDto?.SortOrder,
    currentLayoutItem?.ForeignAppTransactionFieldExDto?.IsFormLayoutVisible
  ]);

  const columns = useMemo(() => {
    return fields.map((f) => {
      const raw = f.DisplayWidth;
      const width =
        typeof raw === 'number'
          ? raw
          : raw
          ? parseInt(raw.toString(), 10)
          : 150;
      const finalWidth = Number.isFinite(width) ? width : 150;
      const fieldId = getFieldId(f);
      return {
        field: f,
        key: (fieldId ?? f.DataBaseFieldName ?? Math.random()).toString(),
        header: f.DisplayName || f.LabelDisplayBinding || f.DataBaseFieldName || '',
        width: Math.max(60, Math.min(500, finalWidth))
      };
    });
  }, [fields, getFieldId]);

  const selectedFieldId =
    currentLayoutItem?.__isGridColumn === true ? currentLayoutItem?.TransactionFieldId : undefined;
  const isGridColumnSelected =
    currentLayoutItem?.__isGridColumn === true &&
    currentLayoutItem?.GridTransactionUnitId === layoutItemExDto?.GridTransactionUnitId;

  const handleSelectColumn = (field: GridField) => {
    if (!onLayoutItemClick) return;
    const fieldId = getFieldId(field);
    if (fieldId === undefined) return;
    // Create a lightweight "column selection item" so FieldSettingToolbox can edit the column (DisplayName/DisplayWidth)
    onLayoutItemClick({
      __isGridColumn: true,
      GridTransactionUnitId: layoutItemExDto?.GridTransactionUnitId,
      TransactionFieldId: fieldId,
      ForeignAppTransactionFieldExDto: field, // will be normalized to dict dto by FormDesign.handleLayoutItemSelect
      DomAttribute: {
        IsBindingToDataField: true,
        // Match existing pattern where WidgetDisplayType holds ControlType for field items
        WidgetDisplayType: field.ControlType
      }
    });
  };

  const contextMenuButton =
    controllerModel?.isEnableFormConfigButtons && onOpenRowItemContextMenu ? (
      <button
        className="ContainerContextMenuButton absolute right-1 top-1"
        title="Grid Menu"
        style={{
          zIndex: 60,
          width: '22px',
          height: '22px',
          borderRadius: '3px',
          backgroundColor: 'rgba(255, 255, 255, 0.9)',
          border: '1px solid #ccc',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center'
        }}
        onClick={(e) => {
          e.preventDefault();
          e.stopPropagation();
          onOpenRowItemContextMenu(e, layoutItemExDto);
        }}
      >
        <i className="fa fa-ellipsis-v"></i>
      </button>
    ) : null;

  // Design-mode placeholder for MultipleSelectBox (checkbox tiles, no real data)
  if (isMultipleSelectBox) {
    const title =
      unitExDto?.UnitDisplayName || layoutItemExDto?.DomAttribute?.DisplayName || 'Multiple Select';
    return (
      <div className="w-full h-full relative flex flex-col">
        {contextMenuButton}
        <div
          className={`w-full h-full min-h-[120px] border rounded flex flex-col overflow-hidden ${theme.mainContentSection}`}
        >
          <div className={`px-2 py-1 border-b shrink-0 ${theme.mainContentSection}`}>
            <div className={`text-xs font-semibold ${theme.title}`}>{title}</div>
          </div>
          <div className={`min-h-0 flex-auto overflow-auto px-3 py-3 ${theme.mainContentSection}`}>
            <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
              {MULTI_SELECT_PLACEHOLDER_OPTIONS.map((opt) => (
                <label
                  key={opt.label}
                  className={`flex cursor-default items-center gap-2 rounded px-2 py-1.5 pointer-events-none ${theme.inputBox}`}
                >
                  <input
                    type="checkbox"
                    className="h-3 w-3 shrink-0 accent-current"
                    checked={opt.checked}
                    readOnly
                    tabIndex={-1}
                    aria-hidden
                  />
                  <span className={`min-w-0 flex-auto truncate text-[11px] ${theme.label}`}>
                    {opt.label}
                  </span>
                </label>
              ))}
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="w-full h-full relative flex flex-col">
      {contextMenuButton}

      {/* Header */}
      <div
        className="w-full border rounded flex flex-col"
        style={{
          backgroundColor: '#fff',
          overflow: 'hidden',
          height: '100%',
          minHeight: '180px'
        }}
      >
        <div
          className="px-2 py-1 border-b flex items-center justify-between"
          style={{ backgroundColor: '#f3f4f6' }}
        >
          <div className={`text-xs font-semibold ${theme.title}`}>
            Grid: {unitExDto?.UnitDisplayName || layoutItemExDto?.DomAttribute?.DisplayName || 'Grid'}
          </div>
        </div>

        {/* Column header + body scroll area */}
        <div style={{ overflow: 'auto', width: '100%', flex: 1, minHeight: 0 }}>
          <div
            style={{
              minWidth: columns.reduce((sum, c) => sum + c.width, 0) + 40,
              minHeight: '100%',
              display: 'flex',
              flexDirection: 'column'
            }}
          >
            {/* Column header row */}
            <div className="flex border-b" style={{ backgroundColor: '#fff' }}>
              {/* Row header */}
              <div
                className="flex-none border-r"
                style={{ width: 40, backgroundColor: '#f9fafb', height: 32 }}
              />

              {columns.map((col) => {
                const isSelected =
                  isGridColumnSelected && selectedFieldId && selectedFieldId === getFieldId(col.field);
                const fieldId = getFieldId(col.field);
                const isHovered = fieldId !== undefined && hoveredFieldId === fieldId;
                return (
                  <div
                    key={col.key}
                    className="flex-none border-r"
                    style={{
                      width: col.width,
                      height: 32,
                      display: 'flex',
                      alignItems: 'center',
                      gap: 6,
                      padding: '0 6px',
                      backgroundColor: isSelected ? 'rgba(59, 130, 246, 0.12)' : '#fff',
                      cursor: 'pointer',
                      ...(isSelected
                        ? { outline: '2px dashed #2563eb', outlineOffset: '-2px' }
                        : isHovered
                        ? { outline: '1px dashed #60a5fa', outlineOffset: '-2px' }
                        : {})
                    }}
                    onClick={(e) => {
                      e.preventDefault();
                      e.stopPropagation();
                      handleSelectColumn(col.field);
                    }}
                    onMouseEnter={() => setHoveredFieldId(fieldId)}
                    onMouseLeave={() => setHoveredFieldId(undefined)}
                  >
                    {/* Column select button */}
                    <button
                      title="Select Column"
                      className={[
                        'w-[18px] h-[18px] rounded-full border-0',
                        'inline-flex items-center justify-center flex-shrink-0',
                        'text-white transition-colors',
                        // Hover should be统一 bg-blue-400 per request
                        isSelected ? 'bg-blue-600 hover:bg-blue-400' : 'bg-blue-500 hover:bg-blue-400'
                      ].join(' ')}
                      onClick={(e) => {
                        e.preventDefault();
                        e.stopPropagation();
                        handleSelectColumn(col.field);
                      }}
                    >
                      <i className="fa fa-pencil" style={{ fontSize: 10, lineHeight: '1' }}></i>
                    </button>
                    <div
                      className="text-xs truncate"
                      title={col.header}
                      style={{ minWidth: 0 }}
                    >
                      {col.header}
                    </div>
                  </div>
                );
              })}
            </div>

            {/* Body rows (placeholder) */}
            {Array.from({ length: 4 }).map((_, rowIdx) => (
              <div key={rowIdx} className="flex border-b" style={{ backgroundColor: '#fff' }}>
                <div
                  className="flex-none border-r flex items-center justify-center"
                  style={{ width: 40, height: 28, backgroundColor: '#f9fafb', color: '#6b7280' }}
                >
                  +
                </div>
                {columns.map((col) => (
                  <div
                    key={`${rowIdx}-${col.key}`}
                    className="flex-none border-r"
                    style={{
                      width: col.width,
                      height: 28,
                      backgroundColor:
                        isGridColumnSelected && selectedFieldId === getFieldId(col.field)
                          ? 'rgba(59, 130, 246, 0.08)'
                          : rowIdx === 0
                          ? '#fefce8'
                          : '#fff'
                    }}
                    onClick={(e) => {
                      e.preventDefault();
                      e.stopPropagation();
                      handleSelectColumn(col.field);
                    }}
                  />
                ))}
              </div>
            ))}

            {/* Filler to visually "fill" remaining height like Angular */}
            <div style={{ flex: 1, backgroundColor: '#fff' }} />
          </div>
        </div>
      </div>
    </div>
  );
};

