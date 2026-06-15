import React, { useState, useRef, useEffect, useCallback } from 'react';
import { useEnumValues } from '../../../hooks/useEnumDictionary';
import { useTheme } from '../../../redux/hooks/useTheme';
import OneLayoutRowDesign from './OneLayoutRowDesign';
import { GridDesignLayout } from './GridDesignLayout';
import FormItemLayout from '../FormMasterDetail/MasterDetailFlexLayoutForm/FormItemLayout';
import DataGridLayout from '../FormMasterDetail/MasterDetailFlexLayoutForm/DataGridLayout';
import appHelper from '../../../helper/appHelper';

interface OneLayoutItemDesignProps {
  layoutItemExDto: any;
  controllerModel: any;
  dataModel: any;
  onDataModelChange: (dataModel: any) => void;
  transactionExDto?: any;
  onLayoutItemClick: (layoutItem: any) => void;
  onLayoutItemDragStart?: (layoutItemId: number) => void;
  onLayoutItemDragEnd?: () => void;
  onDropToNewItemButton?: (event: React.DragEvent, placeholderHostId: string | number) => void;
  onPasteToNewItemButton?: (layoutItem: any) => void;
  onInsertPlaceholderAtIndex?: (parentRow: any, insertAtIndex: number) => void;
  onDropToInsertBoundary?: (event: React.DragEvent, parentRow: any, insertAtIndex: number) => void;
  onInsertRowInSectionAtIndex?: (parentSection: any, insertAtIndex: number) => void;
  currentCutLayoutItem?: any;
  currentLayoutItem?: any;
  currentHoveredLayoutItemHostId?: string | number | null;
  onLayoutItemHover?: (hostId: string | number | null) => void;
  isMouseOverDesignPanel?: boolean;
  onOpenRowItemContextMenu?: (event: React.MouseEvent, layoutItem: any) => void;
  onOpenContainerContextMenu?: (event: React.MouseEvent, layoutItem: any) => void;
  onAddTabToContainer?: (tabContainer: any) => void;
  activeTabs?: Record<string, number | string>;
  onSetActiveTab?: (tabContainerId: string, tabId: number | string) => void;
}

const OneLayoutItemDesign: React.FC<OneLayoutItemDesignProps> = ({
  layoutItemExDto,
  controllerModel,
  dataModel,
  onDataModelChange,
  transactionExDto,
  onLayoutItemClick,
  onLayoutItemDragStart,
  onLayoutItemDragEnd,
  onDropToNewItemButton,
  onPasteToNewItemButton,
  onInsertPlaceholderAtIndex,
  onDropToInsertBoundary,
  onInsertRowInSectionAtIndex,
  currentCutLayoutItem,
  currentLayoutItem,
  currentHoveredLayoutItemHostId,
  onLayoutItemHover,
  isMouseOverDesignPanel = false,
  onOpenRowItemContextMenu,
  onOpenContainerContextMenu,
  onAddTabToContainer,
  activeTabs = {},
  onSetActiveTab
}) => {
  const { theme } = useTheme();
  const layoutItemTypeEnum = useEnumValues('EmAppFormLayoutItemType');
  const _gridDisplayTypeEnum = useEnumValues('EmAppTransactionGridDisplayType');
  const [collapsedSections, setCollapsedSections] = useState<Set<string>>(new Set());
  // Track drag over state for placeholders (keyed by CurrentHostId)
  const [placeholderDragOver, setPlaceholderDragOver] = useState<Record<string | number, boolean>>({});
  // Track hover state for Tab Containers (keyed by Tab Container CurrentHostId)
  const [tabContainerHovered, setTabContainerHovered] = useState<Record<string | number, boolean>>({});
  const containerRef = useRef<HTMLDivElement>(null);

  

  // Standard drag and drop handlers following DesignPanel_DragAndDropStandards.md
  // These must be defined before any early returns to comply with React Hooks rules
  
  // Handle drag start
  const handleDragStart = useCallback((e: React.DragEvent) => {
    e.stopPropagation();
    
    e.dataTransfer.effectAllowed = 'move';
    
    // Store drag data
    const dragData = {
      layoutItemUiId: layoutItemExDto.CurrentHostId,
      layoutItemId: layoutItemExDto.Id
    };
    e.dataTransfer.setData('text/plain', JSON.stringify(dragData));
    
    try {
      e.dataTransfer.setData('application/drag-layout-item-ui-id', layoutItemExDto.CurrentHostId.toString());
    } catch (err) {
      console.warn('Failed to set custom MIME type data:', err);
    }
    
    // Delay calling onLayoutItemDragStart to avoid interrupting drag initialization
    // The state update from onLayoutItemDragStart can cause re-render which interferes with drag
    if (onLayoutItemDragStart) {
      // Use setTimeout to defer state update until after drag is fully initialized
      setTimeout(() => {
        onLayoutItemDragStart(layoutItemExDto.Id);
      }, 0);
    }
  }, [layoutItemExDto, onLayoutItemDragStart]);

  // Handle drag end
  const handleDragEnd = useCallback(() => {
    if (onLayoutItemDragEnd) {
      onLayoutItemDragEnd();
    }
    if (onLayoutItemHover) {
      onLayoutItemHover(null);
    }
  }, [onLayoutItemDragEnd, onLayoutItemHover]);

  // Handle mouse move - detect which element the mouse is actually over (for hover highlighting)
  const handleMouseMove = useCallback((e: React.MouseEvent) => {
    const target = e.target;
    const currentTarget = e.currentTarget as HTMLElement;
    
    // Validate that target is a valid Node
    if (!target || !(target instanceof Node) || !currentTarget || !(currentTarget instanceof Node)) {
      return;
    }
    
    // Find the closest draggable parent element starting from target
    let closestDraggable: HTMLElement | null = null;
    let element: HTMLElement | null = target as HTMLElement;
    
    // Walk up the DOM tree to find the closest draggable element
    while (element && element !== document.body && element instanceof HTMLElement) {
      if (element.hasAttribute && element.hasAttribute('draggable')) {
        closestDraggable = element;
        break;
      }
      element = element.parentElement;
    }
    
    // Only set hover if the closest draggable element is the currentTarget
    // This means mouse is directly over this element (not over a child draggable element)
    if (closestDraggable === currentTarget && onLayoutItemHover && layoutItemExDto.CurrentHostId) {
      onLayoutItemHover(layoutItemExDto.CurrentHostId);
    }
  }, [layoutItemExDto, onLayoutItemHover]);

  // Handle mouse leave - clear hover state when mouse leaves the element
  const createMouseLeaveHandler = useCallback(() => {
    return (e: React.MouseEvent) => {
      const relatedTarget = e.relatedTarget;
      const currentTarget = e.currentTarget as HTMLElement;
      
      // Validate that relatedTarget is a valid Node before using contains()
      if (relatedTarget && 
          relatedTarget instanceof Node && 
          currentTarget && 
          currentTarget instanceof Node &&
          currentTarget.contains(relatedTarget)) {
        // Check if relatedTarget is within a child draggable div
        let element: HTMLElement | null = relatedTarget as HTMLElement;
        while (element && element !== currentTarget) {
          if (element.hasAttribute && element.hasAttribute('draggable') && element !== currentTarget) {
            // Mouse is moving to a child draggable div, don't clear hover
            // The child div's onMouseMove will set its own hover
            return;
          }
          element = element.parentElement;
        }
      }
      
      // Mouse is leaving this element, clear hover
      if (onLayoutItemHover) {
        onLayoutItemHover(null);
      }
    };
  }, [onLayoutItemHover]);

  // Handle drag over
  const handleDragOver = useCallback((e: React.DragEvent) => {
    // Check if dragging over placeholder button
    const target = e.target as HTMLElement;
    let isPlaceholder = false;
    
    if (target && target.id && target.id.startsWith('NewItemButton_')) {
      isPlaceholder = true;
    } else {
      let current: HTMLElement | null = target;
      while (current && current !== e.currentTarget) {
        if (current.id && current.id.startsWith('NewItemButton_')) {
          isPlaceholder = true;
          break;
        }
        current = current.parentElement;
      }
    }
    
    // If not over placeholder, allow drop and update hover
    if (!isPlaceholder) {
      e.preventDefault();
      e.stopPropagation();
      e.dataTransfer.dropEffect = 'move';
      
      // Update hovered ID for visual feedback
      if (onLayoutItemHover && layoutItemExDto.CurrentHostId && currentHoveredLayoutItemHostId !== layoutItemExDto.CurrentHostId) {
        onLayoutItemHover(layoutItemExDto.CurrentHostId);
      }
    }
  }, [layoutItemExDto, currentHoveredLayoutItemHostId, onLayoutItemHover]);

  // Handle drop
  const handleDrop = useCallback((e: React.DragEvent) => {
    // Check if dropping on placeholder button
    const target = e.target as HTMLElement;
    let isPlaceholder = false;
    
    if (target && target.id && target.id.startsWith('NewItemButton_')) {
      isPlaceholder = true;
    } else {
      let current: HTMLElement | null = target;
      while (current && current !== e.currentTarget) {
        if (current.id && current.id.startsWith('NewItemButton_')) {
          isPlaceholder = true;
          break;
        }
        current = current.parentElement;
      }
    }
    
    // If not placeholder, handle drop (placeholder handles its own drop)
    if (!isPlaceholder) {
      e.preventDefault();
      e.stopPropagation();
      
      // Get dragged data
      const dragDataStr = e.dataTransfer.getData('text/plain');
      if (!dragDataStr) return;
      
      let dragData;
      try {
        dragData = JSON.parse(dragDataStr);
      } catch (err) {
        console.warn('Failed to parse drag data:', err);
        return;
      }
      
      const draggedHostId = dragData.layoutItemUiId;
      
      // Don't drop on self
      if (draggedHostId === layoutItemExDto.CurrentHostId) {
        return;
      }
      
      // Note: Actual drop handling is done by parent component via onDropToNewItemButton or onDropToInsertBoundary
      // This handler just prevents default behavior
    }
  }, [layoutItemExDto]);
  
  // Component to render section rows with horizontal boundaries
  const SectionRowsContainer: React.FC<{
    sectionRows: any[];
    layoutItemExDto: any;
    controllerModel: any;
    dataModel: any;
    onDataModelChange: (dataModel: any) => void;
    transactionExDto?: any;
    onLayoutItemClick: (layoutItem: any) => void;
    onLayoutItemDragStart?: (layoutItemId: number) => void;
    onLayoutItemDragEnd?: () => void;
    onDropToNewItemButton?: (event: React.DragEvent, placeholderHostId: string | number) => void;
    onPasteToNewItemButton?: (layoutItem: any) => void;
    onInsertPlaceholderAtIndex?: (parentRow: any, insertAtIndex: number) => void;
    onDropToInsertBoundary?: (event: React.DragEvent, parentRow: any, insertAtIndex: number) => void;
    onInsertRowInSectionAtIndex?: (parentSection: any, insertAtIndex: number) => void;
    currentCutLayoutItem?: any;
    currentLayoutItem?: any;
    currentHoveredLayoutItemHostId?: string | number | null;
    onLayoutItemHover?: (hostId: string | number | null) => void;
    isMouseOverDesignPanel?: boolean;
    onOpenRowItemContextMenu?: (event: React.MouseEvent, layoutItem: any) => void;
    onOpenContainerContextMenu?: (event: React.MouseEvent, layoutItem: any) => void;
    onAddTabToContainer?: (tabContainer: any) => void;
    activeTabs?: Record<string, number | string>;
    onSetActiveTab?: (tabContainerId: string, tabId: number | string) => void;
  }> = ({
    sectionRows,
    layoutItemExDto,
    controllerModel,
    dataModel,
    onDataModelChange,
    transactionExDto,
    onLayoutItemClick,
    onLayoutItemDragStart,
    onLayoutItemDragEnd,
    onDropToNewItemButton,
    onPasteToNewItemButton,
    onInsertPlaceholderAtIndex,
    onDropToInsertBoundary,
    onInsertRowInSectionAtIndex,
    currentCutLayoutItem,
    currentLayoutItem,
    currentHoveredLayoutItemHostId,
    onLayoutItemHover,
    isMouseOverDesignPanel,
    onOpenRowItemContextMenu,
    onOpenContainerContextMenu,
    onAddTabToContainer,
    activeTabs = {},
    onSetActiveTab
  }) => {
    const sectionRef = useRef<HTMLDivElement>(null);
    const [hoveredRowBoundaryIndex, setHoveredRowBoundaryIndex] = useState<number | null>(null);
    
    if (!sectionRows || sectionRows.length === 0) {
      return null;
    }
    
    const sortedRows = [...sectionRows].sort((a: any, b: any) => (a.FlowOrGridLayoutSortOrder || 0) - (b.FlowOrGridLayoutSortOrder || 0));
    
    return (
      <div
        ref={sectionRef}
        style={{ width: '100%', position: 'relative' }}
        onMouseMove={(e) => {
          e.stopPropagation();
          
          if (!sectionRef.current) {
            setHoveredRowBoundaryIndex(null);
            return;
          }
          
          const containerRect = sectionRef.current.getBoundingClientRect();
          const mouseY = e.clientY - containerRect.top;
          const BOUNDARY_THRESHOLD = 4; // Only show boundary when mouse is within 4px
          
          const rowElements = Array.from(sectionRef.current.querySelectorAll('[data-section-row-index]')) as HTMLElement[];
          
          if (rowElements.length > 0) {
            const firstRow = rowElements[0];
            const firstRect = firstRow.getBoundingClientRect();
            const firstTop = firstRect.top - containerRect.top;
            // Check if mouse is within 4px of the first row's top boundary
            if (mouseY >= firstTop - BOUNDARY_THRESHOLD && mouseY <= firstTop + BOUNDARY_THRESHOLD) {
              setHoveredRowBoundaryIndex(0);
              return;
            }
          }
          
          if (rowElements.length > 0) {
            const lastRow = rowElements[rowElements.length - 1];
            const lastRect = lastRow.getBoundingClientRect();
            const lastBottom = lastRect.bottom - containerRect.top;
            // Check if mouse is within 4px of the last row's bottom boundary
            if (mouseY >= lastBottom - BOUNDARY_THRESHOLD && mouseY <= lastBottom + BOUNDARY_THRESHOLD) {
              setHoveredRowBoundaryIndex(sortedRows.length);
              return;
            }
          }
          
          let foundBoundary = false;
          for (let i = 0; i < rowElements.length - 1; i++) {
            const currentRow = rowElements[i];
            const nextRow = rowElements[i + 1];
            
            if (!currentRow || !nextRow) continue;
            
            const currentRect = currentRow.getBoundingClientRect();
            const nextRect = nextRow.getBoundingClientRect();
            const currentBottom = currentRect.bottom - containerRect.top;
            const nextTop = nextRect.top - containerRect.top;
            
            // Calculate the boundary position between rows (midpoint)
            const boundaryY = (currentBottom + nextTop) / 2;
            
            // Check if mouse is within 4px of the boundary between rows
            if (mouseY >= boundaryY - BOUNDARY_THRESHOLD && mouseY <= boundaryY + BOUNDARY_THRESHOLD) {
              setHoveredRowBoundaryIndex(i + 1);
              foundBoundary = true;
              break;
            }
          }
          
          if (!foundBoundary) {
            setHoveredRowBoundaryIndex(null);
          }
        }}
        onMouseLeave={(e) => {
          e.stopPropagation();
          setHoveredRowBoundaryIndex(null);
        }}
      >
        {sortedRows.length > 0 && (
          <HorizontalRowBoundary
            containerRef={sectionRef}
            rowIndex={-1}
            insertIndex={0}
            isHovered={hoveredRowBoundaryIndex === 0}
            onMouseEnter={() => setHoveredRowBoundaryIndex(0)}
            onMouseLeave={() => setHoveredRowBoundaryIndex(null)}
            onDragOver={(e) => {
              e.preventDefault();
              e.stopPropagation();
              e.dataTransfer.dropEffect = 'move';
              setHoveredRowBoundaryIndex(0);
            }}
            onDrop={(e) => {
              if (onDropToInsertBoundary && sortedRows.length > 0) {
                e.preventDefault();
                e.stopPropagation();
                // Calculate insertIndex based on first row's FlowOrGridLayoutSortOrder
                const firstRow = sortedRows[0];
                const insertIndex = firstRow.FlowOrGridLayoutSortOrder || 0;
                // parentRow should be the Section itself (layoutItemExDto)
                onDropToInsertBoundary(e, layoutItemExDto, insertIndex);
              }
            }}
            onClick={(e) => {
              e.stopPropagation();
              e.preventDefault();
              appHelper.debugLog('Section first boundary clicked:', { 
                hasHandler: !!onInsertRowInSectionAtIndex, 
                sortedRowsLength: sortedRows.length,
                sectionId: layoutItemExDto?.CurrentHostId 
              });
              if (onInsertRowInSectionAtIndex) {
                // Calculate insertIndex based on first row's FlowOrGridLayoutSortOrder
                const insertIndex = sortedRows.length > 0 
                  ? (sortedRows[0].FlowOrGridLayoutSortOrder || 0)
                  : 0;
                // parentSection should be the Section itself (layoutItemExDto)
                appHelper.debugLog('Calling onInsertRowInSectionAtIndex with:', { insertIndex });
                onInsertRowInSectionAtIndex(layoutItemExDto, insertIndex);
              } else {
                appHelper.debugLog('onInsertRowInSectionAtIndex is not provided');
              }
            }}
          />
        )}
        
        {sortedRows.map((layoutRowExDto: any, rowIndex: number) => {
          // Ensure CurrentHostId exists, generate if missing
          if (!layoutRowExDto.CurrentHostId) {
            layoutRowExDto.CurrentHostId = appHelper.randomId();
          }
          
          const rowWithMode = {
            ...layoutRowExDto,
            GrandChildEditMode: layoutItemExDto.GrandChildEditMode
          };
          return (
            <React.Fragment key={layoutRowExDto.CurrentHostId}>
              <div data-section-row-index={rowIndex}>
                <OneLayoutRowDesign
                  layoutRowExDto={rowWithMode}
                  controllerModel={controllerModel}
                  dataModel={dataModel}
                  onDataModelChange={onDataModelChange}
                  transactionExDto={transactionExDto}
                  onLayoutItemClick={onLayoutItemClick}
                  onLayoutItemDragStart={onLayoutItemDragStart}
                  onLayoutItemDragEnd={onLayoutItemDragEnd}
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
              
              {rowIndex < sortedRows.length - 1 && (
                <HorizontalRowBoundary
                  containerRef={sectionRef}
                  rowIndex={rowIndex}
                  insertIndex={rowIndex + 1}
                  isHovered={hoveredRowBoundaryIndex === rowIndex + 1}
                  onMouseEnter={() => setHoveredRowBoundaryIndex(rowIndex + 1)}
                  onMouseLeave={() => setHoveredRowBoundaryIndex(null)}
                  onDragOver={(e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    e.dataTransfer.dropEffect = 'move';
                    setHoveredRowBoundaryIndex(rowIndex + 1);
                  }}
                  onDrop={(e) => {
                    if (onDropToInsertBoundary && sortedRows.length > rowIndex + 1) {
                      e.preventDefault();
                      e.stopPropagation();
                      // Calculate insertIndex based on next row's FlowOrGridLayoutSortOrder
                      const nextRow = sortedRows[rowIndex + 1];
                      const insertIndex = nextRow.FlowOrGridLayoutSortOrder || (rowIndex + 1);
                      // parentRow should be the Section itself (layoutItemExDto)
                      onDropToInsertBoundary(e, layoutItemExDto, insertIndex);
                    }
                  }}
                  onClick={(e) => {
                    e.stopPropagation();
                    e.preventDefault();
                    appHelper.debugLog('Section middle boundary clicked:', { 
                      hasHandler: !!onInsertRowInSectionAtIndex, 
                      rowIndex,
                      sortedRowsLength: sortedRows.length 
                    });
                    if (onInsertRowInSectionAtIndex) {
                      // Calculate insertIndex based on next row's FlowOrGridLayoutSortOrder
                      const insertIndex = sortedRows.length > rowIndex + 1
                        ? (sortedRows[rowIndex + 1].FlowOrGridLayoutSortOrder || (rowIndex + 1))
                        : (rowIndex + 1);
                      // parentSection should be the Section itself (layoutItemExDto)
                      appHelper.debugLog('Calling onInsertRowInSectionAtIndex with:', { insertIndex });
                      onInsertRowInSectionAtIndex(layoutItemExDto, insertIndex);
                    } else {
                      appHelper.debugLog('onInsertRowInSectionAtIndex is not provided');
                    }
                  }}
                />
              )}
            </React.Fragment>
          );
        })}
        
        {sortedRows.length > 0 && (
          <HorizontalRowBoundary
            containerRef={sectionRef}
            rowIndex={sortedRows.length}
            insertIndex={sortedRows.length}
            isHovered={hoveredRowBoundaryIndex === sortedRows.length}
            onMouseEnter={() => setHoveredRowBoundaryIndex(sortedRows.length)}
            onMouseLeave={() => setHoveredRowBoundaryIndex(null)}
            onDragOver={(e) => {
              e.preventDefault();
              e.stopPropagation();
              e.dataTransfer.dropEffect = 'move';
              setHoveredRowBoundaryIndex(sortedRows.length);
            }}
            onDrop={(e) => {
              if (onDropToInsertBoundary && sortedRows.length > 0) {
                e.preventDefault();
                e.stopPropagation();
                // Calculate insertIndex based on last row's FlowOrGridLayoutSortOrder
                const lastRow = sortedRows[sortedRows.length - 1];
                const insertIndex = (lastRow.FlowOrGridLayoutSortOrder || 0) + 1;
                // parentRow should be the Section itself (layoutItemExDto)
                onDropToInsertBoundary(e, layoutItemExDto, insertIndex);
              }
            }}
            onClick={(e) => {
              e.stopPropagation();
              e.preventDefault();
              appHelper.debugLog('Section last boundary clicked:', { 
                hasHandler: !!onInsertRowInSectionAtIndex, 
                sortedRowsLength: sortedRows.length 
              });
              if (onInsertRowInSectionAtIndex) {
                // Calculate insertIndex based on last row's FlowOrGridLayoutSortOrder
                const insertIndex = sortedRows.length > 0
                  ? ((sortedRows[sortedRows.length - 1].FlowOrGridLayoutSortOrder || 0) + 1)
                  : 0;
                // parentSection should be the Section itself (layoutItemExDto)
                appHelper.debugLog('Calling onInsertRowInSectionAtIndex with:', { insertIndex });
                onInsertRowInSectionAtIndex(layoutItemExDto, insertIndex);
              } else {
                appHelper.debugLog('onInsertRowInSectionAtIndex is not provided');
              }
            }}
          />
        )}
      </div>
    );
  };
  
  // HorizontalRowBoundary component
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
        
        const rowElements = Array.from(containerRef.current.querySelectorAll('[data-section-row-index]')) as HTMLElement[];
        const containerRect = containerRef.current.getBoundingClientRect();
        
        if (rowIndex === -1 && rowElements.length > 0) {
          const firstRow = rowElements[0];
          const firstRect = firstRow.getBoundingClientRect();
          const top = firstRect.top - containerRect.top;
          const left = 20; // Indent 20px from left to avoid overlap with vertical boundaries
          const width = containerRect.width - 40; // Reduce width by 40px (20px on each side)
          setPosition({ top, left, width });
        } else if (rowIndex === rowElements.length && rowElements.length > 0) {
          const lastRow = rowElements[rowElements.length - 1];
          const lastRect = lastRow.getBoundingClientRect();
          const top = lastRect.bottom - containerRect.top;
          const left = 20; // Indent 20px from left to avoid overlap with vertical boundaries
          const width = containerRect.width - 40; // Reduce width by 40px (20px on each side)
          setPosition({ top, left, width });
        } else if (rowIndex >= 0 && rowIndex < rowElements.length - 1) {
          const currentRow = rowElements[rowIndex];
          const nextRow = rowElements[rowIndex + 1];
          if (currentRow && nextRow) {
            const currentRect = currentRow.getBoundingClientRect();
            const nextRect = nextRow.getBoundingClientRect();
            const top = currentRect.bottom - containerRect.top;
            const left = 20; // Indent 20px from left to avoid overlap with vertical boundaries
            const width = Math.max(currentRect.width, nextRect.width) - 40; // Reduce width by 40px (20px on each side)
            setPosition({ top, left, width });
          } else {
            setPosition(null);
          }
        } else {
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
          height: '12px', // Increased from 8px for easier clicking
          zIndex: 200, // Level 2: Section boundaries
          pointerEvents: 'auto',
          opacity: isHovered ? 1 : 0, // Only show when hovered to avoid flickering
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
          e.dataTransfer.dropEffect = 'move';
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
        <button
          className="InsertBoundaryButton"
          style={{
            position: 'absolute',
            top: '50%',
            left: '25px',
            transform: 'translate(-12px, -50%)',
            zIndex: 202, // Level 2: Section boundaries (button)
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

  // Check if item should be visible
  if (!layoutItemExDto?.DomAttribute?.WidgetDisplayType) {
    return null;
  }

  const displayType = layoutItemExDto.DomAttribute.WidgetDisplayType;
  const domAttribute = layoutItemExDto.DomAttribute;
  
  // Build style from layout info
  let styleLayoutInfo = '';
  if (domAttribute.HeightValue) {
    styleLayoutInfo += `height:${domAttribute.HeightValue}px;`;
  }
  if (domAttribute.BackgroundColor) {
    styleLayoutInfo += `background-color:${domAttribute.BackgroundColor.trim()};`;
  }
  if (domAttribute.TextColor) {
    styleLayoutInfo += `color:${domAttribute.TextColor.trim()};`;
  }
  
  const colSpan = domAttribute.ColSpanValue || 24;
  
  const transFieldExDto = layoutItemExDto.ForeignAppTransactionFieldExDto;
  const isHideBinding = transFieldExDto 
    ? false
    : false;

  const isVisibleExpression = domAttribute.VisibleExpression || 'true';
  
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

  // Set active tab - use parent's onSetActiveTab if provided, otherwise use local state
  const setActiveTab = (tabContainerId: string, tabId: number | string) => {
    if (onSetActiveTab) {
      // Use parent's handler (from FormDesign) to persist across re-renders
      onSetActiveTab(tabContainerId, tabId);
    }
  };

  const isTabActive = (tabId: number | string, tabContainerId: string, defaultTabId: number | string) => {
    // Use parent's activeTabs if provided, otherwise fall back to default
    const activeTabId = activeTabs[tabContainerId] ?? defaultTabId;
    // Convert to string for comparison to handle number/string mismatches
    return String(activeTabId) === String(tabId);
  };

  // Render based on display type
  const renderContent = () => {
    // Section
    if (displayType === layoutItemTypeEnum?.Section) {
      const sectionId = `SectionBody_${layoutItemExDto.Id}_${controllerModel.uiId}`;
      const _isDefaultCollapsed = domAttribute.IsCollapsible && domAttribute.IsDefaultCollapsed;
      const isCollapsed = isSectionCollapsed(sectionId);
      const _shouldShowBody = true; // Always show in design mode
      const isTab = domAttribute.IsTab; // Check if this is a Tab (Section with IsTab: true)

      // If this is a Tab, directly render only the Stack Container (first child), nothing else
      if (isTab) {
        const stackContainer = layoutItemExDto.AppFormLayoutItem_List && layoutItemExDto.AppFormLayoutItem_List.length > 0
          ? layoutItemExDto.AppFormLayoutItem_List[0]
          : null;
        
        if (stackContainer) {
          // Render only the Stack Container, filling the entire Tab content area
          // Stack Container should be rendered as a normal Section (not a Tab), so it will show as a container
          return (
            <div style={{ width: '100%', height: '100%', minHeight: '100px' }} className="w-full h-full">
              <OneLayoutItemDesign
                layoutItemExDto={stackContainer}
                controllerModel={controllerModel}
                dataModel={dataModel}
                onDataModelChange={onDataModelChange}
                transactionExDto={transactionExDto}
                onLayoutItemClick={onLayoutItemClick}
                onLayoutItemDragStart={onLayoutItemDragStart}
                onLayoutItemDragEnd={onLayoutItemDragEnd}
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
                activeTabs={activeTabs}
                onSetActiveTab={onSetActiveTab}
                onAddTabToContainer={onAddTabToContainer}
              />
            </div>
          );
        }
        // If no Stack Container exists, return empty div
        return <div style={{ width: '100%', height: '100%' }} className="w-full h-full"></div>;
      }

      // Regular Section (not a Tab) - show header and content
      return (
        <div 
          style={{ width: '100%' }} 
          className="w-full"
          onClick={(e) => {
            const target = e.target as HTMLElement;
            const currentTarget = e.currentTarget as HTMLElement;
            const sectionContainer = currentTarget.closest('.LayoutItemContainer') as HTMLElement;
            
            if (!sectionContainer) return;
            
            // Only handle click if clicked on header or context button
            const isOnHeader = isClickOnHeader(target, sectionContainer);
            const isOnContextButton = isClickOnContextButton(target, sectionContainer);
            
            if (isOnHeader || isOnContextButton) {
              onLayoutItemClick(layoutItemExDto);
            }
            
            e.stopPropagation();
          }}
        >
          {/* Show header (in design mode, headers are always shown since isDesignMode is always true) */}
          <div
                className={`NoSelect Ctn-SectionHeader truncate overflow-hidden whitespace-nowrap cursor-pointer flex items-center border-b relative z-30 ${
                  domAttribute.IsCollapsible
                    ? 'justify-between px-3 py-2 bg-gray-100 dark:bg-gray-800 text-sm'
                    : 'justify-center px-1 h-[20px] text-xs'
                }`}
                style={{ zIndex: 30, pointerEvents: 'auto' }}
                onClick={(e) => {
                  // Handle click on header
                  // Allow click even when dragging - clicking will select the item and clear drag state
                  if (isDragging && onLayoutItemDragEnd) {
                    onLayoutItemDragEnd();
                  }
                  onLayoutItemClick(layoutItemExDto);
                  if (domAttribute.IsCollapsible) {
                    toggleSection(sectionId);
                  }
                  e.stopPropagation();
                }}
              >
                {domAttribute.IsCollapsible ? (
                  <>
                    <span>{domAttribute.DisplayName || 'Stack Container'}</span>
                    <div className="px-2 text-xs">
                      {!isCollapsed ? (
                        <i className="fa fa-chevron-circle-up"></i>
                      ) : (
                        <i className="fa fa-chevron-circle-down"></i>
                      )}
                    </div>
                  </>
                ) : (
                  <span>
                    {domAttribute.DisplayName || 'Stack Container'}
                  </span>
                )}
              </div>
          <SectionRowsContainer
            sectionRows={layoutItemExDto.AppFormLayoutItem_List}
            layoutItemExDto={layoutItemExDto}
            controllerModel={controllerModel}
            dataModel={dataModel}
            onDataModelChange={onDataModelChange}
            transactionExDto={transactionExDto}
            onLayoutItemClick={onLayoutItemClick}
            onLayoutItemDragStart={onLayoutItemDragStart}
            onLayoutItemDragEnd={onLayoutItemDragEnd}
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
      );
    }
    
    // Table Container - similar to Section but with table layout
    if (displayType === layoutItemTypeEnum?.TableContainer) {
      const tableRows = layoutItemExDto.AppFormLayoutItem_List
        ? [...(layoutItemExDto.AppFormLayoutItem_List || [])].sort((a: any, b: any) => (a.FlowOrGridLayoutSortOrder || 0) - (b.FlowOrGridLayoutSortOrder || 0))
        : [];
      
      return (
        <div 
          style={{ width: '100%' }} 
          className="w-full"
          onClick={(e) => {
            const target = e.target as HTMLElement;
            const currentTarget = e.currentTarget as HTMLElement;
            const containerElement = currentTarget.closest('.LayoutItemContainer') as HTMLElement;
            
            if (!containerElement) return;
            
            // Only handle click if clicked on header or context button
            const isOnHeader = isClickOnHeader(target, containerElement);
            const isOnContextButton = isClickOnContextButton(target, containerElement);
            
            if (isOnHeader || isOnContextButton) {
              onLayoutItemClick(layoutItemExDto);
            }
            
            e.stopPropagation();
          }}
        >
          {/* Table Container header */}
          <div
            className="NoSelect Ctn-TableContainerHeader cursor-pointer flex items-center justify-center px-1 h-[20px] text-xs border-b relative z-30"
            style={{ zIndex: 30, pointerEvents: 'auto' }}
            onClick={(e) => {
              // Handle click on header
              if (isDragging && onLayoutItemDragEnd) {
                onLayoutItemDragEnd();
              }
              onLayoutItemClick(layoutItemExDto);
              e.stopPropagation();
            }}
          >
            <span>{domAttribute.DisplayName || 'Table Container'}</span>
          </div>
          {/* Table Container rows - similar to Section */}
          <SectionRowsContainer
            sectionRows={tableRows}
            layoutItemExDto={layoutItemExDto}
            controllerModel={controllerModel}
            dataModel={dataModel}
            onDataModelChange={onDataModelChange}
            transactionExDto={transactionExDto}
            onLayoutItemClick={onLayoutItemClick}
            onLayoutItemDragStart={onLayoutItemDragStart}
            onLayoutItemDragEnd={onLayoutItemDragEnd}
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
      );
    }
    
    // Tab Container
    if (displayType === layoutItemTypeEnum?.TabContainer) {
      const tabItems = layoutItemExDto.AppFormLayoutItem_List
        ? [...(layoutItemExDto.AppFormLayoutItem_List || [])].sort((a: any, b: any) => (a.FlowOrGridLayoutSortOrder || 0) - (b.FlowOrGridLayoutSortOrder || 0))
        : [];
      
      if (tabItems.length === 0) return null;
      
      // Use Id if available, otherwise use CurrentHostId as fallback
      const defaultTabId = tabItems[0].Id || tabItems[0].CurrentHostId;
      const tabContainerId = (layoutItemExDto.Id || layoutItemExDto.CurrentHostId || '').toString();
      const tabContainerKey = layoutItemExDto.CurrentHostId || layoutItemExDto.Id;
      
      const isTabContainerHovered = tabContainerHovered[tabContainerKey] || false;
      
      return (
        <div 
          className="FormTabContainer w-full relative"
          style={{
            backgroundColor: '#40404022',
            borderRadius: '9px 9px 0px 0px'
          }}
          onMouseEnter={() => {
            // Set hover when entering Tab Container
            if (tabContainerKey) {
              setTabContainerHovered(prev => ({ ...prev, [tabContainerKey]: true }));
            }
          }}
          onMouseLeave={(e) => {
            // Clear hover when leaving Tab Container
            const relatedTarget = e.relatedTarget;
            // Only clear if not moving to a child element within the container
            if (!(relatedTarget instanceof Element) || !relatedTarget.closest('.FormTabContainer')) {
              if (tabContainerKey) {
                setTabContainerHovered(prev => ({ ...prev, [tabContainerKey]: false }));
              }
            }
          }}
          onContextMenu={(e) => {
            // Prevent default context menu and stop propagation to avoid triggering child tab's context menu
            e.preventDefault();
            e.stopPropagation();
            // Only open container context menu if clicking on Tab Container itself (not on child tabs)
            const target = e.target as HTMLElement;
            // Check if click is on Tab Container header or content area, but not on child tab content
            const isOnTabButton = target.closest('.FormTabButton');
            const isOnTabContent = target.closest('.TabContentArea');
            if (!isOnTabButton && !isOnTabContent && onOpenContainerContextMenu && layoutItemExDto) {
              onOpenContainerContextMenu(e, layoutItemExDto);
            }
          }}
        >
          {/* Container context menu button for Tab Container - bottom right, show on hover */}
          {controllerModel.isEnableFormConfigButtons && onOpenContainerContextMenu && (
            <button
              className="w-5 h-5 text-xs text-gray-300 hover:text-gray-500 absolute right-1 bottom-1"
              title="Container Menu"
              style={{
                pointerEvents: 'auto',
                zIndex: 100,
                display: isTabContainerHovered ? 'flex' : 'none',
                alignItems: 'center',
                justifyContent: 'center',
                backgroundColor: 'rgba(255, 255, 255, 0.8)',
                border: '1px solid #ccc',
                borderRadius: '3px'
              }}
              onMouseEnter={(e) => {
                e.stopPropagation();
                // Keep container hovered when hovering over the button
                if (tabContainerKey) {
                  setTabContainerHovered(prev => ({ ...prev, [tabContainerKey]: true }));
                }
                if (onLayoutItemHover && layoutItemExDto.CurrentHostId) {
                  onLayoutItemHover(layoutItemExDto.CurrentHostId);
                }
              }}
              onMouseLeave={(e) => {
                e.stopPropagation();
                // Don't clear hover when leaving button if still in container
                const relatedTarget = e.relatedTarget;
                if (!(relatedTarget instanceof Element) || !relatedTarget.closest('.FormTabContainer')) {
                  if (tabContainerKey) {
                    setTabContainerHovered(prev => ({ ...prev, [tabContainerKey]: false }));
                  }
                }
              }}
              onClick={(e) => {
                e.stopPropagation();
                e.preventDefault();
                if (onOpenContainerContextMenu && layoutItemExDto) {
                  onOpenContainerContextMenu(e, layoutItemExDto);
                }
              }}
              onContextMenu={(e) => {
                e.stopPropagation();
                e.preventDefault();
                if (onOpenContainerContextMenu && layoutItemExDto) {
                  onOpenContainerContextMenu(e, layoutItemExDto);
                }
              }}
            >
              <i className="fa fa-ellipsis-v"></i>
            </button>
          )}
          <div 
            className="FormTabHeader flex flex-wrap"
            style={{
              width: '100%',
              padding: '0px 1px',
              backgroundColor: '#404040ee',
              borderRadius: '9px 9px 0px 0px'
            }}
            onClick={(e) => {
              const target = e.target as HTMLElement;
              const currentTarget = e.currentTarget as HTMLElement;
              const containerElement = currentTarget.closest('.LayoutItemContainer') as HTMLElement;
              
              if (!containerElement) return;
              
              // Only handle click if clicked on header (tab buttons are part of header) or context button
              if (isClickOnHeader(target, containerElement) || isClickOnContextButton(target, containerElement)) {
                // Allow click even when dragging - clicking will select the item and clear drag state
                if (onLayoutItemClick) {
                  if (isDragging && onLayoutItemDragEnd) {
                    onLayoutItemDragEnd();
                  }
                  onLayoutItemClick(layoutItemExDto);
                }
              }
              
              e.stopPropagation();
            }}
          >
            {tabItems.map((aLayoutTab: any) => {
              const _tabWithMode = {
                ...aLayoutTab,
                GrandChildEditMode: layoutItemExDto.GrandChildEditMode
              };
              // Use Id if available, otherwise use CurrentHostId as fallback
              const tabId = aLayoutTab.Id || aLayoutTab.CurrentHostId;
              const isActive = isTabActive(tabId, tabContainerId, defaultTabId);
              
              return (
                <div
                  key={aLayoutTab.CurrentHostId || aLayoutTab.Id}
                  className={`FormTabButton cursor-pointer ${
                    isActive ? 'ActiveTab' : ''
                  }`}
                  style={{
                    flex: '1 1 auto',
                    width: '150px',
                    maxWidth: '200px',
                    height: '23px',
                    borderRadius: '8px 8px 0px 0px',
                    backgroundColor: isActive ? '#fff' : 'rgb(231,234,237)',
                    color: isActive ? '#222' : '#333',
                    fontWeight: 500,
                    padding: '3px 6px',
                    position: 'relative',
                    textOverflow: 'ellipsis',
                    overflow: 'hidden',
                    borderLeft: 'solid 1px rgba(0,0,0,0.03)',
                    marginTop: '1px',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between'
                  }}
                  onMouseEnter={(e) => {
                    if (!isActive) {
                      (e.currentTarget as HTMLElement).style.backgroundColor = 'rgb(241,244,247)';
                    }
                  }}
                  onMouseLeave={(e) => {
                    if (!isActive) {
                      (e.currentTarget as HTMLElement).style.backgroundColor = 'rgb(231,234,237)';
                    }
                  }}
                  onClick={(e) => {
                    e.stopPropagation();
                    // Click Tab Header to select the Tab for editing (like AngularJS)
                    if (onLayoutItemClick && aLayoutTab) {
                      onLayoutItemClick(aLayoutTab);
                    }
                    // Also switch to this tab
                    setActiveTab(tabContainerId, tabId);
                  }}
                >
                  <span style={{ flex: 1, overflow: 'hidden', textOverflow: 'ellipsis' }}>
                    {aLayoutTab.DomAttribute?.DisplayName || ''}
                  </span>
                  {/* Tab context menu button - inverted triangle, only show on hover */}
                  {controllerModel.isEnableFormConfigButtons && onOpenContainerContextMenu && (
                    <button
                      className="w-3 h-3 text-xs text-gray-500 hover:text-gray-700 TabButtonContextButton"
                      title="Tab Menu"
                      style={{
                        flexShrink: 0,
                        marginLeft: '4px',
                        padding: 0,
                        border: 'none',
                        background: 'transparent',
                        cursor: 'pointer',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        fontSize: '8px',
                        lineHeight: '1'
                      }}
                      onClick={(e) => {
                        e.stopPropagation();
                        e.preventDefault();
                        if (onOpenContainerContextMenu && aLayoutTab) {
                          onOpenContainerContextMenu(e, aLayoutTab);
                        }
                      }}
                      onContextMenu={(e) => {
                        e.stopPropagation();
                        e.preventDefault();
                        if (onOpenContainerContextMenu && aLayoutTab) {
                          onOpenContainerContextMenu(e, aLayoutTab);
                        }
                      }}
                    >
                      <i className="fa fa-caret-down"></i>
                    </button>
                  )}
                </div>
              );
            })}
            {/* Add Tab button and Tab Container context button - on the right side of tab header */}
            {controllerModel.isEnableFormConfigButtons && (
              <>
                {onAddTabToContainer && (
                  <button
                    className="w-5 h-5 text-xs text-gray-600 hover:text-gray-800 hover:bg-gray-200 rounded-full flex items-center justify-center"
                    title="Add Tab"
                    style={{
                      marginLeft: '4px',
                      marginTop: '1px',
                      backgroundColor: 'rgb(231,234,237)',
                      border: 'none',
                      cursor: 'pointer',
                      flexShrink: 0
                    }}
                    onClick={(e) => {
                      e.stopPropagation();
                      if (onAddTabToContainer && layoutItemExDto) {
                        onAddTabToContainer(layoutItemExDto);
                      }
                    }}
                  >
                    <i className="fa fa-plus" style={{ fontSize: '10px' }}></i>
                  </button>
                )}
                {/* Tab Container context button - gear icon, next to Add Tab button */}
                {onOpenContainerContextMenu && (
                  <button
                    className="w-5 h-5 text-xs text-gray-600 hover:text-gray-800 hover:bg-gray-200 rounded-full flex items-center justify-center"
                    title="Tab Container Menu"
                    style={{
                      marginLeft: '4px',
                      marginTop: '1px',
                      backgroundColor: 'rgb(231,234,237)',
                      border: 'none',
                      cursor: 'pointer',
                      flexShrink: 0
                    }}
                    onClick={(e) => {
                      e.stopPropagation();
                      e.preventDefault();
                      if (onOpenContainerContextMenu && layoutItemExDto) {
                        onOpenContainerContextMenu(e, layoutItemExDto);
                      }
                    }}
                    onContextMenu={(e) => {
                      e.stopPropagation();
                      e.preventDefault();
                      if (onOpenContainerContextMenu && layoutItemExDto) {
                        onOpenContainerContextMenu(e, layoutItemExDto);
                      }
                    }}
                  >
                    <i className="fa fa-gear" style={{ fontSize: '10px' }}></i>
                  </button>
                )}
              </>
            )}
            <div style={{ width: '100%', height: '3px', backgroundColor: 'white' }}></div>
          </div>
          <div style={{ width: '100%', padding: '0px 1px 1px 1px' }}>
            {tabItems.map((layoutTabSectionExDto: any) => {
              // Use Id if available, otherwise use CurrentHostId as fallback
              const tabId = layoutTabSectionExDto.Id || layoutTabSectionExDto.CurrentHostId;
              const isActive = isTabActive(tabId, tabContainerId, defaultTabId);
              if (!isActive) return null;
              
              const tabKey = layoutTabSectionExDto.CurrentHostId || layoutTabSectionExDto.Id;
              
              return (
                <div 
                  key={tabKey} 
                  className="TabContentArea"
                  style={{ width: '100%', height: '100%', minHeight: '100px', position: 'relative' }}
                  onContextMenu={(e) => {
                    // Prevent Tab content area from opening Tab Container's context menu
                    // Tab content should use its own context menu (if any)
                    e.stopPropagation();
                  }}
                >
                  <OneLayoutItemDesign
                    layoutItemExDto={layoutTabSectionExDto}
                    controllerModel={controllerModel}
                    dataModel={dataModel}
                    onDataModelChange={onDataModelChange}
                    transactionExDto={transactionExDto}
                    onLayoutItemClick={onLayoutItemClick}
                    onLayoutItemDragStart={onLayoutItemDragStart}
                    onLayoutItemDragEnd={onLayoutItemDragEnd}
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
              );
            })}
          </div>
        </div>
      );
    }
    
    // Grid (Data Grid)
    if (displayType === layoutItemTypeEnum?.Grid) {
      // IMPORTANT: Prefer finding unit from latest transactionExDto so UI reflects edits immediately.
      // layoutItemExDto.ForeignAppTransactionUnitExDto may be a stale reference.
      let unitExDto = undefined as any;
      
      if (transactionExDto && layoutItemExDto.GridTransactionUnitId) {
        const unitId = layoutItemExDto.GridTransactionUnitId;
        if (transactionExDto.AppTransactionUnitList) {
          unitExDto = transactionExDto.AppTransactionUnitList.find((unit: any) => unit.Id === unitId);
          
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

      // Fallback to unit reference on layout item
      if (!unitExDto) {
        unitExDto = layoutItemExDto.ForeignAppTransactionUnitExDto;
      }
      
      if (!unitExDto) {
        return (
          <div className="w-full border-2 border-dashed border-gray-300 rounded p-4 bg-gray-50">
            <div className="text-center text-gray-500">
              <i className="fa fa-table text-2xl mb-2"></i>
              <div className="text-sm font-semibold">Data Grid</div>
              <div className="text-xs mt-1 text-yellow-600">
                Unit not found (ID: {layoutItemExDto.GridTransactionUnitId})
              </div>
            </div>
          </div>
        );
      }
      
      // Design-mode grid placeholder (match Angular: shows columns + widths, column select, grid context menu)
      if (controllerModel?.isDesignMode === true) {
        return (
          <GridDesignLayout
            layoutItemExDto={layoutItemExDto}
            unitExDto={unitExDto}
            controllerModel={controllerModel}
            currentLayoutItem={currentLayoutItem}
            onLayoutItemClick={onLayoutItemClick}
            onOpenRowItemContextMenu={onOpenRowItemContextMenu}
          />
        );
      }

      return (
        <div className="w-full" style={{ position: 'relative' }}>
          <DataGridLayout
            unitExDto={unitExDto}
            unitId={unitExDto.Id}
            dataModel={dataModel}
            controllerModel={controllerModel}
            transactionExDto={transactionExDto}
            onDataModelChange={onDataModelChange}
          />
        </div>
      );
    }
    
    // NewItemAddButton (Placeholder)
    if (displayType === layoutItemTypeEnum?.NewItemAddButton) {
      const placeholderId = `NewItemButton_${layoutItemExDto.CurrentHostId}`;
      const hostId = layoutItemExDto.CurrentHostId;
      const isDragOver = placeholderDragOver[hostId] || false;
      
      const handleDragEnter = (e: React.DragEvent) => {
        e.preventDefault();
        e.stopPropagation();
        e.dataTransfer.dropEffect = 'move';
        setPlaceholderDragOver(prev => ({ ...prev, [hostId]: true }));
      };
      
      const handleDragOver = (e: React.DragEvent) => {
        e.preventDefault();
        e.stopPropagation();
        e.dataTransfer.dropEffect = 'move';
        setPlaceholderDragOver(prev => ({ ...prev, [hostId]: true }));
      };
      
      const handleDragLeave = (e: React.DragEvent) => {
        const relatedTarget = e.relatedTarget;
        if (!(relatedTarget instanceof Node) || !e.currentTarget.contains(relatedTarget)) {
          setPlaceholderDragOver(prev => ({ ...prev, [hostId]: false }));
        }
      };
      
      const handleDrop = (e: React.DragEvent) => {
        e.preventDefault();
        e.stopPropagation();
        setPlaceholderDragOver(prev => ({ ...prev, [hostId]: false }));
        if (onDropToNewItemButton) {
          onDropToNewItemButton(e, hostId);
        }
      };
      
      const handleClick = (e: React.MouseEvent) => {
        e.stopPropagation();
        
        if (currentCutLayoutItem && onPasteToNewItemButton) {
          onPasteToNewItemButton(layoutItemExDto);
          return;
        }
        
        if (onLayoutItemClick && layoutItemExDto) {
          onLayoutItemClick(layoutItemExDto);
        }
      };
      
      return (
        <div 
          id={placeholderId}
          className="w-full py-1"
          onDragEnter={handleDragEnter}
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
          onDrop={handleDrop}
          onClick={handleClick}
          style={{ 
            position: 'relative', 
            zIndex: 20, 
            pointerEvents: 'auto',            
            height: '50px', // Ensure height is auto, not stretched
           
            
          }}
        >
          <div 
            className={`h-full overflow-hidden text-center border-2 border-dashed rounded-md py-2 flex items-center justify-center transition-colors ${
              isDragOver || (currentLayoutItem?.CurrentHostId === layoutItemExDto.CurrentHostId)
                ? 'border-blue-600 bg-blue-200' 
                : 'border-blue-300 bg-blue-50 hover:border-blue-500 hover:bg-blue-100'
            }`}
            
          > 
            {currentCutLayoutItem ? (
              <div className="text-blue-400 text-sm w-full text-center overflow-hidden">
                <i className="fa fa-paste mr-2"></i>
                <span>Click To Paste Here</span>
              </div>
            ) : (
              <div className="text-blue-500 text-sm w-full text-center overflow-hidden">
                <i className="fa fa-hand-o-down mr-2"></i>
                <span>Placeholder</span>
              </div>
            )}
          </div>
        </div>
      );
    }
    
    // Content or Space
    if (displayType === layoutItemTypeEnum?.Content || displayType === layoutItemTypeEnum?.Space) {
      return (
        <div className="w-full p-2">
          <div className={`text-sm ${theme.title}`}>
            {domAttribute.DisplayName || (displayType === layoutItemTypeEnum?.Content ? 'Literal Content' : 'Space')}
          </div>
        </div>
      );
    }
    
    // Field or other types
    const aAppTransactionFieldExDto = layoutItemExDto.ForeignAppTransactionFieldExDto;
    
    if (aAppTransactionFieldExDto) {
      return (
        <div className="w-full h-full relative">
          <FormItemLayout
            layoutItemExDto={layoutItemExDto}
            controllerModel={controllerModel}
            dataModel={dataModel}
            onDataModelChange={onDataModelChange}
            transactionExDto={transactionExDto}
          />
        </div>
      );
    }
    
    // Command Action Button
    if (displayType === layoutItemTypeEnum?.CommandActionButton) {
      return (
        <div className="w-full p-2">
          <button className="px-4 py-2 bg-blue-500 text-white rounded">
            {domAttribute.DisplayName || 'Command Button'}
          </button>
        </div>
      );
    }
    
    // Linked Search
    if (displayType === layoutItemTypeEnum?.LinkedSearch) {
      return (
        <div className="w-full p-2">
          <div className="text-sm text-gray-500">
            Linked Search: {domAttribute.DisplayName || 'Search'}
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

  // Design mode specific states
  const isSelected = currentLayoutItem?.CurrentHostId === layoutItemExDto.CurrentHostId;
  const isHovered = currentHoveredLayoutItemHostId === layoutItemExDto.CurrentHostId;
  const isDragging = controllerModel?.draggingLayoutItemId === layoutItemExDto.Id;
  
  // Field type checks
  const isFieldType = !!layoutItemExDto.ForeignAppTransactionFieldExDto;
  const isSpaceType = displayType === layoutItemTypeEnum?.Space;
  const isLiteralContentType = displayType === layoutItemTypeEnum?.Content;
  
  // Check if this is a container type (Section, TabContainer, etc.)
  const isContainerType = displayType === layoutItemTypeEnum?.Section || 
                          displayType === layoutItemTypeEnum?.TabContainer;

  // Helper function to check if click is on container context button
  const isClickOnContextButton = (target: HTMLElement, currentTarget: HTMLElement): boolean => {
    if (!isContainerType) return false;
    
    // Check if click is on ContainerContextMenuButton
    const contextButton = target.closest('.ContainerContextMenuButton');
    if (contextButton) {
      // Verify the button belongs to this container
      const containerElement = contextButton.closest('.LayoutItemContainer') as HTMLElement;
      return containerElement === currentTarget;
    }
    
    return false;
  };
  
  // Helper function to check if click is on container header
  const isClickOnHeader = (target: HTMLElement, currentTarget: HTMLElement): boolean => {
    if (!isContainerType) return false;
    
    // Check Section header
    if (displayType === layoutItemTypeEnum?.Section) {
      const header = currentTarget.querySelector('.Ctn-SectionHeader');
      return header ? header.contains(target) : false;
    }
    
    // Check TabContainer header
    if (displayType === layoutItemTypeEnum?.TabContainer) {
      const header = currentTarget.querySelector('.FormTabHeader');
      return header ? header.contains(target) : false;
    }
    
    return false;
  };
  

  // Border style
  const isPlaceholderButton = displayType === layoutItemTypeEnum?.NewItemAddButton;
  let borderClass = 'border-2';
  
  if (isSelected && !isPlaceholderButton) {
    borderClass += ' border-blue-500 border-solid';
  } else if (isHovered && !isPlaceholderButton) {
    borderClass += ' border-blue-400 border-dashed';
  } else if (isDragging) {
    borderClass += ' border-blue-500 border-dotted';
  } else {
    borderClass += ' border-transparent';
  }

  // Determine if this item should be draggable
  const shouldBeDraggable = !isPlaceholderButton;
  
  // Determine if this item needs a design overlay to block runtime interactions
  // Field and Grid types need overlay to prevent clicking on INPUT, DDL buttons, etc.
  // Container types don't need overlay as they contain other layout items, not direct form controls
  const needsDesignOverlay = !isContainerType && 
                             !isPlaceholderButton &&
                             (isFieldType || (displayType === layoutItemTypeEnum?.Grid && controllerModel?.isDesignMode !== true));

  // For placeholder buttons, prevent vertical stretching
  const containerStyle = {
    ...styleObject, 
    position: 'relative' as const, 
    cursor: shouldBeDraggable ? 'move' : 'default',
    // Prevent vertical stretching for placeholder buttons
    ...(isPlaceholderButton ? { alignSelf: 'flex-start', height: 'auto' } : {})
  };

  return (
    <div
      ref={containerRef}
      className={`LayoutItemContainer LayoutItemRuntimeOrder${layoutItemExDto.ItemRuntimeOrder} CSpan_${colSpan} ${borderClass}`}
      style={containerStyle}
      data-item-id={layoutItemExDto.Id || layoutItemExDto.CurrentHostId}
      data-host-id={layoutItemExDto.CurrentHostId}
      draggable={shouldBeDraggable}
      onClick={(e) => {
        const target = e.target as HTMLElement;
        const currentTarget = e.currentTarget as HTMLElement;
        
        // For container types, only handle click if clicked on header or context button
        if (isContainerType) {
          if (!isClickOnHeader(target, currentTarget) && !isClickOnContextButton(target, currentTarget)) {
            return;
          }
        }
        
        // Handle click (child elements won't trigger this due to event bubbling control)
        // Allow click even when dragging - clicking will select the item and clear drag state
        if (onLayoutItemClick) {
          // If dragging, clear drag state first by calling dragEnd
          if (isDragging && onLayoutItemDragEnd) {
            onLayoutItemDragEnd();
          }
          onLayoutItemClick(layoutItemExDto);
        }
      }}
      onMouseMove={shouldBeDraggable ? handleMouseMove : undefined}
      onMouseLeave={shouldBeDraggable ? createMouseLeaveHandler() : undefined}
      onDragStart={shouldBeDraggable ? handleDragStart : undefined}
      onDragEnd={shouldBeDraggable ? handleDragEnd : undefined}
      onDragOver={shouldBeDraggable ? handleDragOver : undefined}
      onDrop={shouldBeDraggable ? handleDrop : undefined}
    >
      {renderContent()}

      {/* Design overlay - transparent layer to block runtime interactions (INPUT, DDL buttons, etc.) */}
      {/* This layer prevents clicks from reaching form controls but allows drag/hover events to pass through */}
      {needsDesignOverlay && (
        <div
          className="LayoutItemDesignOverlay absolute inset-0"
          style={{
            zIndex: 5, // Lower than drag/hover layers (z-40, z-50) but above content
            pointerEvents: 'auto', // Block pointer events to prevent clicking on underlying elements
            cursor: shouldBeDraggable ? 'move' : 'default',
            backgroundColor: 'transparent' // Fully transparent, only blocks interactions
          }}
          // Don't handle drag events here - let them bubble to parent container which has draggable attribute
          // Don't handle mouse move here - let it bubble to parent for hover detection
          onClick={(e) => {
            // Check if click is on a boundary element - if so, don't block it
            const target = e.target as HTMLElement;
            if (target.closest('.InsertBoundary') || target.closest('.InsertBoundaryButton')) {
              // Click is on a boundary - let it propagate
              return;
            }
            
            // Block click events - prevent clicking on underlying form controls
            e.stopPropagation();
            // Handle click to select the layout item
            // Allow click even when dragging - clicking will select the item and clear drag state
            if (onLayoutItemClick) {
              // If dragging, clear drag state first by calling dragEnd
              if (isDragging && onLayoutItemDragEnd) {
                onLayoutItemDragEnd();
              }
              onLayoutItemClick(layoutItemExDto);
            }
          }}
        />
      )}

      {/* Container context menu buttons */}
      {/* Note: Exclude Tab items (IsTab: true) - they use Tab-specific context menu button */}
      {displayType === layoutItemTypeEnum?.Section && 
       !domAttribute.IsTab && 
       controllerModel.isEnableFormConfigButtons && 
       onOpenRowItemContextMenu && (
        <>
          <button
            className="w-5 h-5 text-xs text-gray-300 hover:text-gray-500 ContainerContextMenuButton absolute right-1 top-1 z-40"
            title="Row Item Menu"
            onMouseEnter={(e) => {
              e.stopPropagation();
              if (containerRef.current) {
                containerRef.current.classList.add('is-hovered');
              }
              if (onLayoutItemHover && layoutItemExDto.CurrentHostId) {
                onLayoutItemHover(layoutItemExDto.CurrentHostId);
              }
            }}
            onMouseLeave={(e) => {
              e.stopPropagation();
            }}
            onClick={(e) => {
              e.stopPropagation();
              if (onOpenRowItemContextMenu && layoutItemExDto) {
                onOpenRowItemContextMenu(e, layoutItemExDto);
              } else if (onLayoutItemClick && layoutItemExDto) {
                onLayoutItemClick(layoutItemExDto);
              }
            }}
          >
            <i className="fa fa-gear"></i>
          </button>
          <button
            className="w-5 h-5 text-xs text-gray-300 hover:text-gray-500 ContainerContextMenuButton absolute right-1 bottom-1 z-40"
            title="Row Item Menu"
            onMouseEnter={(e) => {
              e.stopPropagation();
              if (containerRef.current) {
                containerRef.current.classList.add('is-hovered');
              }
              if (onLayoutItemHover && layoutItemExDto.CurrentHostId) {
                onLayoutItemHover(layoutItemExDto.CurrentHostId);
              }
            }}
            onMouseLeave={(e) => {
              e.stopPropagation();
            }}
            onClick={(e) => {
              e.stopPropagation();
              if (onOpenRowItemContextMenu && layoutItemExDto) {
                onOpenRowItemContextMenu(e, layoutItemExDto);
              } else if (onLayoutItemClick && layoutItemExDto) {
                onLayoutItemClick(layoutItemExDto);
              }
            }}
          >
            <i className="fa fa-gear"></i>
          </button>
        </>
      )}

      {/* Row item context menu button - for field + Space types (Space needs hover menu like Angular) */}
      {(isFieldType || isSpaceType || isLiteralContentType) && controllerModel.isEnableFormConfigButtons && onOpenRowItemContextMenu && (
        <button
          className="w-5 h-5 text-xs text-gray-300 hover:text-gray-500 ContainerContextMenuButton absolute right-1 top-2"
          title="Item Menu"
          style={{ 
            zIndex: 50,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center'
          }}
          onMouseEnter={(e) => {
            e.stopPropagation();
            if (containerRef.current) {
              containerRef.current.classList.add('is-hovered');
            }
            if (onLayoutItemHover && layoutItemExDto.CurrentHostId) {
              onLayoutItemHover(layoutItemExDto.CurrentHostId);
            }
          }}
          onMouseLeave={(e) => {
            e.stopPropagation();
          }}
          onClick={(e) => {
            e.stopPropagation();
            e.preventDefault();
            if (onOpenRowItemContextMenu && layoutItemExDto) {
              onOpenRowItemContextMenu(e, layoutItemExDto);
            }
          }}
        >
          <i className="fa fa-ellipsis-v"></i>
        </button>
      )}
    </div>
  );
};

export default OneLayoutItemDesign;
