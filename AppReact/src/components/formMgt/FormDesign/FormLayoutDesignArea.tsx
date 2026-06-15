import React, { useState, useRef, useCallback, useEffect, useMemo } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useEnumValues } from '../../../hooks/useEnumDictionary';
import OneLayoutRowDesign from './OneLayoutRowDesign';
import appHelper from '../../../helper/appHelper';

interface FormLayoutDesignAreaProps {
  formData: any;
  transactionData: any;
  currentLayoutItem: any;
  draggingLayoutItemId?: number | null;
  currentCutLayoutItem?: any;
  currentHoveredLayoutItemHostId?: string | number | null;
  isMouseOverDesignPanel?: boolean;
  onLayoutItemSelect: (layoutItem: any) => void;
  onLayoutItemChange: (layoutItem: any) => void;
  onAddLayoutItem: (itemType: number, transactionFieldId?: number, gridTransactionUnitId?: number, commandActionId?: number, linkedSearchId?: number) => void;
  onDropToNewItemButton?: (event: React.DragEvent, placeholderHostId: string | number) => void;
  onInsertPlaceholderAtIndex?: (parentRow: any, insertAtIndex: number) => void;
  onDropToInsertBoundary?: (event: React.DragEvent, parentRow: any, insertAtIndex: number) => void;
  onResolveInsertBoundaryDrop?: (event: React.DragEvent, parentHostId: string | number, insertIndex: number) => void;
  onInsertRowAtIndex?: (insertAtIndex: number) => void;
  onDropToRowBoundary?: (event: React.DragEvent, insertAtIndex: number) => void;
  onInsertRowInSectionAtIndex?: (parentSection: any, insertAtIndex: number) => void;
  onPasteToNewItemButton?: (layoutItem: any) => void;
  onLayoutItemHover?: (hostId: string | number | null) => void;
  onDesignPanelMouseMove?: (event: React.MouseEvent) => void;
  onDesignPanelMouseEnter?: () => void;
  onDesignPanelMouseLeave?: () => void;
  onDragStart?: (layoutItemId: number) => void;
  onDragEnd?: () => void;
  onOpenRowItemContextMenu?: (event: React.MouseEvent, layoutItem: any) => void;
  onOpenContainerContextMenu?: (event: React.MouseEvent, layoutItem: any) => void;
  onAddTabToContainer?: (tabContainer: any) => void;
  activeTabs?: Record<string, number | string>;
  onSetActiveTab?: (tabContainerId: string, tabId: number | string) => void;
}

const FormLayoutDesignArea: React.FC<FormLayoutDesignAreaProps> = ({
  formData,
  transactionData,
  currentLayoutItem,
  draggingLayoutItemId = null,
  currentCutLayoutItem,
  currentHoveredLayoutItemHostId = null,
  isMouseOverDesignPanel = false,
  onLayoutItemSelect,
  onLayoutItemChange,
  onAddLayoutItem,
  onDropToNewItemButton,
  onInsertPlaceholderAtIndex,
  onDropToInsertBoundary,
  onResolveInsertBoundaryDrop,
  onInsertRowAtIndex,
  onDropToRowBoundary,
  onInsertRowInSectionAtIndex,
  onPasteToNewItemButton,
  onLayoutItemHover,
  onDesignPanelMouseMove,
  onDesignPanelMouseEnter,
  onDesignPanelMouseLeave,
  onDragStart,
  onDragEnd,
  onOpenRowItemContextMenu,
  onOpenContainerContextMenu,
  onAddTabToContainer,
  activeTabs = {},
  onSetActiveTab
}) => {
  const { theme: _theme } = useTheme();
  const layoutItemTypeEnum = useEnumValues('EmAppFormLayoutItemType');
  const [draggedItem, setDraggedItem] = useState<{ type: number; transactionFieldId?: number; gridTransactionUnitId?: number; commandActionId?: number; linkedSearchId?: number } | null>(null);
  const [dragOverIndex, setDragOverIndex] = useState<number | null>(null);
  const designAreaRef = useRef<HTMLDivElement>(null);
  const [hoveredRowBoundaryIndex, setHoveredRowBoundaryIndex] = useState<number | null>(null);

  // Listen for drag events from outside (AddFieldToolbox)
  React.useEffect(() => {
    const handleDragEnd = () => {
      setDraggedItem(null);
      setDragOverIndex(null);
    };

    document.addEventListener('dragend', handleDragEnd);

    return () => {
      document.removeEventListener('dragend', handleDragEnd);
    };
  }, []);

  // Get layout rows from formData
  // According to AngularJS structure: AppFormLayoutItemList is an array of rows,
  // each row has AppFormLayoutItem_List containing the items in that row
  const layoutRows = React.useMemo(() => {
    appHelper.debugLog('FormLayoutDesignArea: layoutRows useMemo triggered');
    appHelper.debugLog('FormLayoutDesignArea: formData:', {
      hasFormData: !!formData,
      rowCount: formData?.AppFormLayoutItemList?.length || 0,
      formDataId: formData?.Id,
      formDataName: formData?.Name
    });
    
    const rows = formData?.AppFormLayoutItemList || [];
    
    if (!rows || rows.length === 0) {
      appHelper.debugLog('FormLayoutDesignArea: No rows, returning empty array');
      return [];
    }

    // Sort rows by FlowOrGridLayoutSortOrder (like AngularJS does)
    const sortedRows = [...rows].sort((a: any, b: any) => 
      (a.FlowOrGridLayoutSortOrder || 0) - (b.FlowOrGridLayoutSortOrder || 0)
    );

    appHelper.debugLog('FormLayoutDesignArea: Processing rows:', {
      originalRowCount: rows.length,
      sortedRowCount: sortedRows.length,
      placeholderCount: sortedRows.flatMap((row: any) => 
        row.AppFormLayoutItem_List?.filter((item: any) => 
          item.DomAttribute?.WidgetDisplayType === layoutItemTypeEnum?.NewItemAddButton
        ) || []
      ).length
    });

    // Ensure each row has AppFormLayoutItem_List
    const result = sortedRows.map((row: any) => ({
      ...row,
      AppFormLayoutItem_List: row.AppFormLayoutItem_List || [],
      GrandChildEditMode: 1 // Default, can be overridden by transactionExDto
    }));
    
    appHelper.debugLog('FormLayoutDesignArea: layoutRows useMemo returning:', {
      resultRowCount: result.length
    });
    
    return result;
  }, [formData?.AppFormLayoutItemList]);

  // Handle drag start from toolbox (reserved for toolbox drag)
  const _handleDragStart = useCallback((e: React.DragEvent, itemType: number, transactionFieldId?: number, gridTransactionUnitId?: number, commandActionId?: number, linkedSearchId?: number) => {
    setDraggedItem({ type: itemType, transactionFieldId, gridTransactionUnitId, commandActionId, linkedSearchId });
    e.dataTransfer.effectAllowed = 'copy';
    e.dataTransfer.setData('text/plain', ''); // Required for Firefox
  }, []);

  // Handle drag over
  const handleDragOver = useCallback((e: React.DragEvent, rowIndex?: number) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'copy';
    if (rowIndex !== undefined) {
      setDragOverIndex(rowIndex);
    }
  }, []);

  // Handle drag leave
  const handleDragLeave = useCallback(() => {
    setDragOverIndex(null);
  }, []);

  // Handle drop
  const handleDrop = useCallback((e: React.DragEvent, targetRowIndex?: number) => {
    e.preventDefault();

    if (!draggedItem) {
      // Fallback: drop may have landed on row/design area but user intended boundary; check for layout-item drag and elementFromPoint
      const layoutItemIdData = e.dataTransfer.getData('application/drag-layout-item-ui-id');
      let layoutItemIdFromPlain: string | undefined;
      try {
        const plain = e.dataTransfer.getData('text/plain');
        if (plain) {
          const json = JSON.parse(plain);
          if (json.layoutItemUiId != null) layoutItemIdFromPlain = String(json.layoutItemUiId);
        }
      } catch (_) {}
      const hasLayoutItemDrag = (layoutItemIdData && layoutItemIdData.trim() !== '') || !!layoutItemIdFromPlain;
      if (hasLayoutItemDrag && onResolveInsertBoundaryDrop) {
        const el = document.elementFromPoint(e.clientX, e.clientY);
        const boundary = el?.closest?.('.InsertBoundary') as HTMLElement | null;
        const parentHostId = boundary?.getAttribute?.('data-parent-host-id');
        const insertIndexStr = boundary?.getAttribute?.('data-insert-index');
        if (parentHostId != null && insertIndexStr != null && insertIndexStr !== '') {
          const insertIndex = parseInt(insertIndexStr, 10);
          if (!isNaN(insertIndex)) {
            onResolveInsertBoundaryDrop(e, parentHostId, insertIndex);
            e.stopPropagation();
            setDraggedItem(null);
            setDragOverIndex(null);
            return;
          }
        }
      }
      e.stopPropagation();
      return;
    }

    e.stopPropagation();

    // Calculate the target row index
    let finalRowIndex = targetRowIndex;
    if (finalRowIndex === undefined) {
      // Add to the end
      finalRowIndex = layoutRows.length;
    }

    // Call onAddLayoutItem with the dragged item data
    onAddLayoutItem(
      draggedItem.type,
      draggedItem.transactionFieldId,
      draggedItem.gridTransactionUnitId,
      draggedItem.commandActionId,
      draggedItem.linkedSearchId
    );

    // Reset drag state
    setDraggedItem(null);
    setDragOverIndex(null);
  }, [draggedItem, layoutRows.length, onAddLayoutItem, onResolveInsertBoundaryDrop]);

  // Handle add row
  const handleAddRow = useCallback((afterRowIndex?: number) => {
    // Add a new row by adding a Space item
    const _targetRowIndex = afterRowIndex !== undefined ? afterRowIndex + 1 : layoutRows.length;
    onAddLayoutItem(layoutItemTypeEnum?.Space || 0);
  }, [layoutRows.length, layoutItemTypeEnum, onAddLayoutItem]);

  // Handle add column (add item to existing row)
  const handleAddColumn = useCallback((rowIndex: number) => {
    // Add a Space item to the specified row
    onAddLayoutItem(layoutItemTypeEnum?.Space || 0);
  }, [layoutItemTypeEnum, onAddLayoutItem]);

  // Handle add section
  const handleAddSection = useCallback((afterRowIndex?: number) => {
    onAddLayoutItem(layoutItemTypeEnum?.Section || 0);
  }, [layoutItemTypeEnum, onAddLayoutItem]);

  // Controller model for rendering (similar to MasterDetailFlexLayoutForm)
  // Use useMemo to ensure it updates when draggingLayoutItemId or currentLayoutItem changes
  const controllerModel = useMemo(() => ({
    uiId: `formDesign_${formData?.Id || 'new'}`,
    isEnableFormConfigButtons: true,
    isDesignMode: true,
    isPreview: false, // Ensure isPreview is explicitly set to boolean
    currentLayoutItemId: currentLayoutItem?.Id || null, // Pass selected item ID for visual feedback
    draggingLayoutItemId: (draggingLayoutItemId ?? null) as number | null // Pass dragging item ID for visual feedback
  }), [formData?.Id, currentLayoutItem?.Id, draggingLayoutItemId]);

  // Data model (minimal for design mode)
  const dataModel = {
    currentFormStructure: formData
  };

  const onDataModelChange = () => {
    // In design mode, data model changes are handled by parent
  };

  return (
    <div 
      ref={designAreaRef}
      className="w-full min-h-full relative FormDesignPanel"
      onDragOver={(e) => {
        e.preventDefault();
        handleDragOver(e);
      }}
      onDrop={(e) => handleDrop(e)}
      onDragLeave={handleDragLeave}
      onMouseMove={onDesignPanelMouseMove}
      onMouseEnter={onDesignPanelMouseEnter}
      onMouseLeave={onDesignPanelMouseLeave}
    >
      {layoutRows.length === 0 ? (
        // Empty state - show drop zone
        <div 
          className="w-full h-64 border-2 border-dashed border-gray-300 rounded-lg flex items-center justify-center bg-gray-50"
          onDragOver={(e) => handleDragOver(e, 0)}
          onDrop={(e) => handleDrop(e, 0)}
        >
          <div className="text-center text-gray-400">
            <i className="fa fa-arrow-down text-2xl mb-2"></i>
            <div className="text-sm">Drag fields here to start building your form</div>
            {formData?.AppFormLayoutItemList && formData.AppFormLayoutItemList.length > 0 && (
              <div className="mt-2 text-xs text-yellow-600">
                Note: Found {formData.AppFormLayoutItemList.length} rows but they may not have items
              </div>
            )}
          </div>
        </div>
      ) : (
        // Render layout rows with horizontal boundaries
        <div 
          className="space-y-2 relative"
          ref={designAreaRef}
          style={{ position: 'relative', minHeight: '100px' }}
          onMouseMove={(e) => {
            if (!controllerModel?.isDesignMode || !designAreaRef.current) {
              setHoveredRowBoundaryIndex(null);
              return;
            }
            
            const containerRect = designAreaRef.current.getBoundingClientRect();
            const mouseY = e.clientY - containerRect.top;
            
            // Find which rows the mouse is between
            const rowElements = Array.from(designAreaRef.current.querySelectorAll('[data-row-index]')) as HTMLElement[];
            
            // Check top boundary (before first row)
            if (rowElements.length > 0) {
              const firstRow = rowElements[0];
              const firstRect = firstRow.getBoundingClientRect();
              const firstTop = firstRect.top - containerRect.top;
              if (mouseY >= 0 && mouseY <= firstTop + 30) {
                setHoveredRowBoundaryIndex(0);
                return;
              }
            }
            
            // Check bottom boundary (after last row)
            if (rowElements.length > 0) {
              const lastRow = rowElements[rowElements.length - 1];
              const lastRect = lastRow.getBoundingClientRect();
              const lastBottom = lastRect.bottom - containerRect.top;
              if (mouseY >= lastBottom - 30 && mouseY <= containerRect.height) {
                setHoveredRowBoundaryIndex(layoutRows.length);
                return;
              }
            }
            
            // Check boundaries between rows
            let foundBoundary = false;
            for (let i = 0; i < rowElements.length - 1; i++) {
              const currentRow = rowElements[i];
              const nextRow = rowElements[i + 1];
              
              if (!currentRow || !nextRow) continue;
              
              const currentRect = currentRow.getBoundingClientRect();
              const nextRect = nextRow.getBoundingClientRect();
              const currentBottom = currentRect.bottom - containerRect.top;
              const nextTop = nextRect.top - containerRect.top;
              
              if (mouseY >= currentBottom - 30 && mouseY <= nextTop + 30) {
                setHoveredRowBoundaryIndex(i + 1);
                foundBoundary = true;
                break;
              }
            }
            
            if (!foundBoundary) {
              setHoveredRowBoundaryIndex(null);
            }
          }}
          onMouseLeave={() => {
            setHoveredRowBoundaryIndex(null);
          }}
        >
          {/* Top horizontal boundary (before first row) */}
          {controllerModel?.isDesignMode && layoutRows.length > 0 && (
            <HorizontalRowBoundary
              containerRef={designAreaRef}
              rowIndex={-1}
              insertIndex={0}
              isHovered={hoveredRowBoundaryIndex === 0}
              onMouseEnter={() => setHoveredRowBoundaryIndex(0)}
              onMouseLeave={() => setHoveredRowBoundaryIndex(null)}
              onDragOver={(e) => {
                e.preventDefault();
                e.stopPropagation();
                e.dataTransfer.dropEffect = 'copy';
                setHoveredRowBoundaryIndex(0);
              }}
              onDrop={(e) => {
                if (onDropToRowBoundary) {
                  e.preventDefault();
                  e.stopPropagation();
                  onDropToRowBoundary(e, 0);
                }
              }}
              onClick={(e) => {
                e.stopPropagation();
                e.preventDefault();
                appHelper.debugLog('Top level first boundary clicked:', { hasHandler: !!onInsertRowAtIndex });
                if (onInsertRowAtIndex) {
                  appHelper.debugLog('Calling onInsertRowAtIndex with:', 0);
                  onInsertRowAtIndex(0);
                } else {
                  appHelper.debugLog('onInsertRowAtIndex is not provided');
                }
              }}
            />
          )}
          
          {layoutRows.map((row: any, rowIndex: number) => (
            <div
              key={row.CurrentHostId || `row-${row.RowIndex || rowIndex}`}
              data-row-index={rowIndex}
              className={`relative group ${dragOverIndex === rowIndex ? 'ring-2 ring-blue-400' : ''}`}
              onDragOver={(e) => handleDragOver(e, rowIndex)}
              onDrop={(e) => handleDrop(e, rowIndex)}
              onDragLeave={handleDragLeave}
            >
              {/* Row controls (visible on hover in design mode) */}
              <div className="absolute -left-8 top-0 h-full flex items-center opacity-0 group-hover:opacity-100 transition-opacity z-10">
                <div className="flex flex-col gap-1">
                  <button
                    className="w-6 h-6 bg-blue-500 text-white text-xs rounded hover:bg-blue-600 flex items-center justify-center"
                    onClick={() => handleAddColumn(rowIndex)}
                    title="Add Column"
                  >
                    <i className="fa fa-plus"></i>
                  </button>
                  <button
                    className="w-6 h-6 bg-green-500 text-white text-xs rounded hover:bg-green-600 flex items-center justify-center"
                    onClick={() => handleAddRow(rowIndex)}
                    title="Add Row"
                  >
                    <i className="fa fa-plus"></i>
                  </button>
                  <button
                    className="w-6 h-6 bg-purple-500 text-white text-xs rounded hover:bg-purple-600 flex items-center justify-center"
                    onClick={() => handleAddSection(rowIndex)}
                    title="Add Section"
                  >
                    <i className="fa fa-square-o"></i>
                  </button>
                </div>
              </div>

              {/* Render the row */}
              {/* Don't add border to row wrapper if selected item is a section - sections have their own borders */}
              <div 
                className={`border-2 ${
                  currentLayoutItem && 
                  currentLayoutItem.DomAttribute?.WidgetDisplayType !== layoutItemTypeEnum?.Section &&
                  row.AppFormLayoutItem_List.some((item: any) => 
                    item.Id === currentLayoutItem.Id || 
                    item.CurrentHostId === currentLayoutItem.CurrentHostId
                  )
                    ? 'border-blue-500 bg-blue-50' 
                    : 'border-transparent'
                } rounded p-1 transition-colors`}
                onDragOver={(e) => {
                  // Allow drops to pass through to child elements
                  e.preventDefault();
                }}
              >
                <OneLayoutRowDesign
                  layoutRowExDto={row}
                  controllerModel={controllerModel}
                  dataModel={dataModel}
                  onDataModelChange={onDataModelChange}
                  transactionExDto={transactionData?.AppTransactionData}
                  onLayoutItemClick={onLayoutItemSelect}
                  onLayoutItemDragStart={onDragStart}
                  onLayoutItemDragEnd={onDragEnd}
                  onDropToNewItemButton={onDropToNewItemButton}
                  onPasteToNewItemButton={onPasteToNewItemButton}
                  onInsertPlaceholderAtIndex={onInsertPlaceholderAtIndex}
                  onDropToInsertBoundary={onDropToInsertBoundary}
                  onInsertRowInSectionAtIndex={onInsertRowInSectionAtIndex}
                  currentCutLayoutItem={currentCutLayoutItem}
                  currentLayoutItem={currentLayoutItem}
                  currentHoveredLayoutItemHostId={currentHoveredLayoutItemHostId}
                  onLayoutItemHover={onLayoutItemHover}
                  isMouseOverDesignPanel={isMouseOverDesignPanel}
                  onOpenRowItemContextMenu={onOpenRowItemContextMenu}
                  onOpenContainerContextMenu={onOpenContainerContextMenu}
                  onAddTabToContainer={onAddTabToContainer}
                  activeTabs={activeTabs}
                  onSetActiveTab={onSetActiveTab}
                />
              </div>
              
              {/* Horizontal boundary after this row (except for last row) */}
              {controllerModel?.isDesignMode && rowIndex < layoutRows.length - 1 && (
                <HorizontalRowBoundary
                  containerRef={designAreaRef}
                  rowIndex={rowIndex}
                  insertIndex={rowIndex + 1}
                  isHovered={hoveredRowBoundaryIndex === rowIndex + 1}
                  onMouseEnter={() => setHoveredRowBoundaryIndex(rowIndex + 1)}
                  onMouseLeave={() => setHoveredRowBoundaryIndex(null)}
                  onDragOver={(e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    e.dataTransfer.dropEffect = 'copy';
                    setHoveredRowBoundaryIndex(rowIndex + 1);
                  }}
                  onDrop={(e) => {
                    if (onDropToRowBoundary) {
                      e.preventDefault();
                      e.stopPropagation();
                      onDropToRowBoundary(e, rowIndex + 1);
                    }
                  }}
                  onClick={(e: React.MouseEvent) => {
                    e.stopPropagation();
                    e.preventDefault();
                    appHelper.debugLog('Top level middle boundary clicked:', { 
                      hasHandler: !!onInsertRowAtIndex, 
                      rowIndex 
                    });
                    if (onInsertRowAtIndex) {
                      appHelper.debugLog('Calling onInsertRowAtIndex with:', rowIndex + 1);
                      onInsertRowAtIndex(rowIndex + 1);
                    } else {
                      appHelper.debugLog('onInsertRowAtIndex is not provided');
                    }
                  }}
                />
              )}
            </div>
          ))}

          {/* Bottom horizontal boundary (after last row) */}
          {controllerModel?.isDesignMode && layoutRows.length > 0 && (
            <HorizontalRowBoundary
              containerRef={designAreaRef}
              rowIndex={layoutRows.length}
              insertIndex={layoutRows.length}
              isHovered={hoveredRowBoundaryIndex === layoutRows.length}
              onMouseEnter={() => setHoveredRowBoundaryIndex(layoutRows.length)}
              onMouseLeave={() => setHoveredRowBoundaryIndex(null)}
              onDragOver={(e) => {
                e.preventDefault();
                e.stopPropagation();
                e.dataTransfer.dropEffect = 'copy';
                setHoveredRowBoundaryIndex(layoutRows.length);
              }}
              onDrop={(e) => {
                if (onDropToRowBoundary) {
                  e.preventDefault();
                  e.stopPropagation();
                  onDropToRowBoundary(e, layoutRows.length);
                }
              }}
              onClick={(e: React.MouseEvent) => {
                e.stopPropagation();
                e.preventDefault();
                appHelper.debugLog('Top level last boundary clicked:', { 
                  hasHandler: !!onInsertRowAtIndex, 
                  layoutRowsLength: layoutRows.length 
                });
                if (onInsertRowAtIndex) {
                  appHelper.debugLog('Calling onInsertRowAtIndex with:', layoutRows.length);
                  onInsertRowAtIndex(layoutRows.length);
                } else {
                  appHelper.debugLog('onInsertRowAtIndex is not provided');
                }
              }}
            />
          )}
        </div>
      )}
    </div>
  );
};

// Component to render horizontal row boundary marker
const HorizontalRowBoundary: React.FC<{
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
      
      const rowElements = Array.from(containerRef.current.querySelectorAll('[data-row-index]')) as HTMLElement[];
      const containerRect = containerRef.current.getBoundingClientRect();
      
      if (rowIndex === -1 && rowElements.length > 0) {
        // Top boundary: before first row
        const firstRow = rowElements[0];
        const firstRect = firstRow.getBoundingClientRect();
        const top = firstRect.top - containerRect.top;
        const left = 20; // Indent 20px from left to avoid overlap with vertical boundaries
        const width = containerRect.width - 40; // Reduce width by 40px (20px on each side)
        setPosition({ top, left, width });
      } else if (rowIndex === rowElements.length && rowElements.length > 0) {
        // Bottom boundary: after last row (rowIndex is layoutRows.length)
        const lastRow = rowElements[rowElements.length - 1];
        const lastRect = lastRow.getBoundingClientRect();
        const top = lastRect.bottom - containerRect.top;
        const left = 20; // Indent 20px from left to avoid overlap with vertical boundaries
        const width = containerRect.width - 40; // Reduce width by 40px (20px on each side)
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
          const left = 20; // Indent 20px from left to avoid overlap with vertical boundaries
          // Use the width of the current row, not the entire container, and reduce by 40px
          const width = Math.max(currentRect.width, nextRect.width) - 40; // Reduce width by 40px (20px on each side)
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
  
  if (!position) {
    return null;
  }
  
  return (
    <div
      className="HorizontalRowBoundary"
      style={{
        position: 'absolute',
        top: `${position.top}px`,
        left: `${position.left}px`,
        width: `${position.width}px`,
        height: '12px', // Increased from 8px for easier clicking
        zIndex: 100, // Level 1: Top-level boundaries
        pointerEvents: 'auto',
        opacity: isHovered ? 1 : 0, // Only show when hovered to avoid flickering
        transition: 'opacity 0.2s',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        transform: 'translateY(-6px)', // Adjust transform to center the height
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
          top: '50%',
          transform: 'translateY(-50%)',
          height: '8px',
          zIndex: 101,
          pointerEvents: 'none' // Let clicks pass through to the boundary container
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
          left: '0px',
          transform: 'translate(-12px, -50%)',
          zIndex: 102, // Level 1: Top-level boundaries (button)
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

export default FormLayoutDesignArea;
