import React, { useState, useRef, useEffect } from 'react';
import { useEnumValues } from '../../../hooks/useEnumDictionary';
import OneLayoutItemDesign from './OneLayoutItemDesign';
import appHelper from '../../../helper/appHelper';

interface OneLayoutRowDesignProps {
  layoutRowExDto: any;
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

const OneLayoutRowDesign: React.FC<OneLayoutRowDesignProps> = ({
  layoutRowExDto,
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
  const layoutItemTypeEnum = useEnumValues('EmAppFormLayoutItemType');
  const rowRef = useRef<HTMLDivElement>(null);
  
  // Track hover state for insert boundaries (between items)
  const [hoveredInsertIndex, setHoveredInsertIndex] = useState<number | null>(null);
  // Track hover state for left/right boundaries (before first item, after last item)
  const [hoveredLeftBoundary, setHoveredLeftBoundary] = useState<boolean>(false);
  const [hoveredRightBoundary, setHoveredRightBoundary] = useState<boolean>(false);
  
  // Get child layout items, ordered by FlowOrGridLayoutSortOrder
  // Include all items (including placeholder buttons) to maintain flex layout
  const childItems = layoutRowExDto?.AppFormLayoutItem_List 
    ? [...layoutRowExDto.AppFormLayoutItem_List].sort((a: any, b: any) => 
        (a.FlowOrGridLayoutSortOrder || 0) - (b.FlowOrGridLayoutSortOrder || 0)
      )
    : [];
  
  if (!childItems || childItems.length === 0) {
    return null;
  }
  
  // Filter out placeholder buttons only for insert boundary calculation
  const nonPlaceholderItems = childItems.filter((item: any) => 
    item.DomAttribute?.WidgetDisplayType !== layoutItemTypeEnum?.NewItemAddButton
  );

  let itemRuntimeOrder = 0;

  // Handle insert boundary hover
  const handleInsertBoundaryMouseEnter = (insertIndex: number) => {
    setHoveredInsertIndex(insertIndex);
  };

  const handleInsertBoundaryMouseLeave = () => {
    setHoveredInsertIndex(null);
  };

  // Handle click on insert boundary button
  const handleInsertBoundaryClick = (insertIndex: number) => {
    appHelper.debugLog('Insert boundary clicked (between items):', {
      hasHandler: !!onInsertPlaceholderAtIndex,
      insertIndex,
      rowHostId: layoutRowExDto?.CurrentHostId
    });
    if (onInsertPlaceholderAtIndex) {
      appHelper.debugLog('Calling onInsertPlaceholderAtIndex for insert boundary:', { insertIndex, rowHostId: layoutRowExDto?.CurrentHostId });
      onInsertPlaceholderAtIndex(layoutRowExDto, insertIndex);
    } else {
      appHelper.debugLog('Insert boundary click: handler not available');
    }
  };

  // Handle drag over insert boundary (use 'move' so drop fires when dragging form field with effectAllowed='move')
  const handleInsertBoundaryDragOver = (e: React.DragEvent, insertIndex: number) => {
    e.preventDefault();
    e.stopPropagation();
    e.dataTransfer.dropEffect = 'move';
    setHoveredInsertIndex(insertIndex);
  };

  // Handle drop on insert boundary
  const handleInsertBoundaryDrop = (e: React.DragEvent, insertIndex: number) => {
    if (onDropToInsertBoundary) {
      e.preventDefault();
      e.stopPropagation();
      onDropToInsertBoundary(e, layoutRowExDto, insertIndex);
      setHoveredInsertIndex(null);
    }
  };

  // Handle mouse move over row to detect when hovering between items or at edges
  const handleRowMouseMove = (e: React.MouseEvent) => {
    if (!rowRef.current) {
      setHoveredInsertIndex(null);
      setHoveredLeftBoundary(false);
      setHoveredRightBoundary(false);
      return;
    }
    
    const rowRect = rowRef.current.getBoundingClientRect();
    const mouseX = e.clientX - rowRect.left;
    const BOUNDARY_THRESHOLD = 6; // Half of 12px boundary width for easier detection
    
    // Find which non-placeholder items the mouse is between
    const itemElements = Array.from(rowRef.current.querySelectorAll('.LayoutItemContainer')) as HTMLElement[];
    const nonPlaceholderElements: HTMLElement[] = [];
    
    // Filter to only non-placeholder items
    itemElements.forEach((el) => {
      const hostId = el.getAttribute('data-host-id');
      if (hostId) {
        const item = childItems.find((item: any) => 
          (item.Id?.toString() === el.getAttribute('data-item-id')) ||
          (item.CurrentHostId?.toString() === hostId)
        );
        if (item && item.DomAttribute?.WidgetDisplayType !== layoutItemTypeEnum?.NewItemAddButton) {
          nonPlaceholderElements.push(el);
        }
      }
    });
    
    if (nonPlaceholderElements.length === 0) {
      setHoveredInsertIndex(null);
      setHoveredLeftBoundary(false);
      setHoveredRightBoundary(false);
      return;
    }
    
    // Check left boundary (before first item) - only show when mouse is within 4px
    const firstItem = nonPlaceholderElements[0];
    if (firstItem) {
      const firstRect = firstItem.getBoundingClientRect();
      const firstLeft = firstRect.left - rowRect.left;
      // Check if mouse is within 4px of the first item's left boundary
      if (mouseX >= firstLeft - BOUNDARY_THRESHOLD && mouseX <= firstLeft + BOUNDARY_THRESHOLD) {
        setHoveredLeftBoundary(true);
        setHoveredInsertIndex(null);
        setHoveredRightBoundary(false);
        return;
      }
    }
    
    // Check right boundary (after last item) - only show when mouse is within 4px
    const lastItem = nonPlaceholderElements[nonPlaceholderElements.length - 1];
    if (lastItem) {
      const lastRect = lastItem.getBoundingClientRect();
      const lastRight = lastRect.right - rowRect.left;
      // Check if mouse is within 4px of the last item's right boundary
      if (mouseX >= lastRight - BOUNDARY_THRESHOLD && mouseX <= lastRight + BOUNDARY_THRESHOLD) {
        setHoveredRightBoundary(true);
        setHoveredInsertIndex(null);
        setHoveredLeftBoundary(false);
        return;
      }
    }
    
    // Check boundaries between items
    let foundBoundary = false;
    setHoveredLeftBoundary(false);
    setHoveredRightBoundary(false);
    
    for (let i = 0; i < nonPlaceholderElements.length - 1; i++) {
      const currentItem = nonPlaceholderElements[i];
      const nextItem = nonPlaceholderElements[i + 1];
      
      if (!currentItem || !nextItem) continue;
      
      const currentRect = currentItem.getBoundingClientRect();
      const nextRect = nextItem.getBoundingClientRect();
      const currentRight = currentRect.right - rowRect.left;
      const nextLeft = nextRect.left - rowRect.left;
      
      // Calculate the boundary position between items (midpoint)
      const boundaryX = (currentRight + nextLeft) / 2;
      
      // Check if mouse is within threshold of the boundary between items
      if (mouseX >= boundaryX - BOUNDARY_THRESHOLD && mouseX <= boundaryX + BOUNDARY_THRESHOLD) {
        const hostId = currentItem.getAttribute('data-host-id');
        const currentItemData = nonPlaceholderItems.find((item: any) => 
          item.CurrentHostId?.toString() === hostId ||
          (item.Id?.toString() === currentItem.getAttribute('data-item-id'))
        );
        
        if (currentItemData) {
          const nonPlaceholderIdx = nonPlaceholderItems.findIndex((item: any) => 
            item.CurrentHostId === currentItemData.CurrentHostId ||
            (item.Id && currentItemData.Id && item.Id === currentItemData.Id)
          );
          
          if (nonPlaceholderIdx >= 0 && nonPlaceholderIdx < nonPlaceholderItems.length - 1) {
            // Use the next item's FlowOrGridLayoutSortOrder as insertIndex, or calculate based on position
            const nextItemData = nonPlaceholderItems[nonPlaceholderIdx + 1];
            const insertIndex = nextItemData?.FlowOrGridLayoutSortOrder ?? (currentItemData.FlowOrGridLayoutSortOrder || 0) + 1;
            appHelper.debugLog('Boundary hover detected:', {
              mouseX,
              boundaryX,
              currentItemIndex: nonPlaceholderIdx,
              currentItemSortOrder: currentItemData.FlowOrGridLayoutSortOrder,
              nextItemSortOrder: nextItemData?.FlowOrGridLayoutSortOrder,
              calculatedInsertIndex: insertIndex,
              rowHostId: layoutRowExDto?.CurrentHostId
            });
            setHoveredInsertIndex(insertIndex);
            foundBoundary = true;
            break;
          }
        }
      }
    }
    
    if (!foundBoundary) {
      setHoveredInsertIndex(null);
    }
  };

  const handleRowMouseLeave = () => {
    setHoveredInsertIndex(null);
    setHoveredLeftBoundary(false);
    setHoveredRightBoundary(false);
  };
  
  // Handle left boundary (before first item)
  const handleLeftBoundaryClick = () => {
    appHelper.debugLog('Left boundary clicked:', {
      hasHandler: !!onInsertPlaceholderAtIndex,
      nonPlaceholderItemsLength: nonPlaceholderItems.length,
      rowHostId: layoutRowExDto?.CurrentHostId
    });
    if (onInsertPlaceholderAtIndex && nonPlaceholderItems.length > 0) {
      const firstItem = nonPlaceholderItems[0];
      const insertIndex = firstItem.FlowOrGridLayoutSortOrder || 0;
      appHelper.debugLog('Calling onInsertPlaceholderAtIndex for left boundary:', { insertIndex, rowHostId: layoutRowExDto?.CurrentHostId });
      onInsertPlaceholderAtIndex(layoutRowExDto, insertIndex);
    } else {
      appHelper.debugLog('Left boundary click: handler not available or no items');
    }
  };
  
  // Handle right boundary (after last item)
  const handleRightBoundaryClick = () => {
    appHelper.debugLog('Right boundary clicked:', {
      hasHandler: !!onInsertPlaceholderAtIndex,
      nonPlaceholderItemsLength: nonPlaceholderItems.length,
      rowHostId: layoutRowExDto?.CurrentHostId
    });
    if (onInsertPlaceholderAtIndex && nonPlaceholderItems.length > 0) {
      const lastItem = nonPlaceholderItems[nonPlaceholderItems.length - 1];
      const insertIndex = (lastItem.FlowOrGridLayoutSortOrder || 0) + 1;
      appHelper.debugLog('Calling onInsertPlaceholderAtIndex for right boundary:', { insertIndex, rowHostId: layoutRowExDto?.CurrentHostId });
      onInsertPlaceholderAtIndex(layoutRowExDto, insertIndex);
    } else {
      appHelper.debugLog('Right boundary click: handler not available or no items');
    }
  };
  
  const handleLeftBoundaryDrop = (e: React.DragEvent) => {
    if (onDropToInsertBoundary && nonPlaceholderItems.length > 0) {
      e.preventDefault();
      e.stopPropagation();
      const firstItem = nonPlaceholderItems[0];
      const insertIndex = firstItem.FlowOrGridLayoutSortOrder || 0;
      onDropToInsertBoundary(e, layoutRowExDto, insertIndex);
    }
  };
  
  const handleRightBoundaryDrop = (e: React.DragEvent) => {
    if (onDropToInsertBoundary && nonPlaceholderItems.length > 0) {
      e.preventDefault();
      e.stopPropagation();
      const lastItem = nonPlaceholderItems[nonPlaceholderItems.length - 1];
      const insertIndex = (lastItem.FlowOrGridLayoutSortOrder || 0) + 1;
      onDropToInsertBoundary(e, layoutRowExDto, insertIndex);
    }
  };

  return (
    <div 
      ref={rowRef}
      className="FormLayoutRow Row24 relative"
      style={{ 
        position: 'relative',
        // Add padding in design mode to separate inner and outer boundaries
        paddingLeft: '10px',
        paddingRight: '10px'
      }}
      onMouseMove={handleRowMouseMove}
      onMouseLeave={handleRowMouseLeave}
    >    
      {childItems.map((layoutItemExDto: any, index: number) => {
        itemRuntimeOrder++;
        const itemWithOrder = {
          ...layoutItemExDto,
          GrandChildEditMode: layoutRowExDto.GrandChildEditMode,
          ItemRuntimeOrder: itemRuntimeOrder
        };
        
        // Check if this is a non-placeholder item for insert boundary calculation
        const isNonPlaceholder = layoutItemExDto.DomAttribute?.WidgetDisplayType !== layoutItemTypeEnum?.NewItemAddButton;
        const nonPlaceholderIndex = isNonPlaceholder 
          ? nonPlaceholderItems.findIndex((item: any) => 
              item.CurrentHostId === layoutItemExDto.CurrentHostId ||
              (item.Id && layoutItemExDto.Id && item.Id === layoutItemExDto.Id)
            )
          : -1;
        
        // Calculate insert index (after current item) - only for non-placeholder items
        // The insertIndex should be the next item's sort order, or current + 1
        const nextNonPlaceholderItem = nonPlaceholderItems[nonPlaceholderIndex + 1];
        const insertIndex = nextNonPlaceholderItem?.FlowOrGridLayoutSortOrder ?? (layoutItemExDto.FlowOrGridLayoutSortOrder || 0) + 1;
        const isHovered = hoveredInsertIndex === insertIndex && isNonPlaceholder;
        const shouldShowBoundary = isNonPlaceholder && nonPlaceholderIndex >= 0 && nonPlaceholderIndex < nonPlaceholderItems.length - 1;
        
        return (
          <React.Fragment key={layoutItemExDto.CurrentHostId || layoutItemExDto.Id || `item-${itemRuntimeOrder}`}>
            {/* Layout Item - must be rendered first to maintain flex layout */}
            <OneLayoutItemDesign
              layoutItemExDto={itemWithOrder}
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
            
            {/* Insert boundary line and button (after each non-placeholder item, except the last) - absolute positioned to not affect flex */}
            {shouldShowBoundary && (
              <InsertBoundaryMarker
                rowRef={rowRef}
                itemIndex={nonPlaceholderIndex}
                insertIndex={insertIndex}
                parentHostId={layoutRowExDto.CurrentHostId}
                isHovered={isHovered}
                nonPlaceholderItems={nonPlaceholderItems}
                onMouseEnter={() => handleInsertBoundaryMouseEnter(insertIndex)}
                onMouseLeave={handleInsertBoundaryMouseLeave}
                onDragOver={(e) => handleInsertBoundaryDragOver(e, insertIndex)}
                onDrop={(e) => handleInsertBoundaryDrop(e, insertIndex)}
                onClick={() => handleInsertBoundaryClick(insertIndex)}
                onLayoutItemHover={onLayoutItemHover}
              />
            )}
          </React.Fragment>
        );
      })}
      
      {/* Left boundary (before first item) */}
      {nonPlaceholderItems.length > 0 && (
        <InsertBoundaryMarker
          rowRef={rowRef}
          itemIndex={-1}
          insertIndex={nonPlaceholderItems[0]?.FlowOrGridLayoutSortOrder || 0}
          parentHostId={layoutRowExDto.CurrentHostId}
          isHovered={hoveredLeftBoundary}
          nonPlaceholderItems={nonPlaceholderItems}
          isLeftBoundary={true}
          onMouseEnter={() => setHoveredLeftBoundary(true)}
          onMouseLeave={() => setHoveredLeftBoundary(false)}
          onDragOver={(e) => {
            e.preventDefault();
            e.stopPropagation();
            e.dataTransfer.dropEffect = 'move';
            setHoveredLeftBoundary(true);
          }}
          onDrop={handleLeftBoundaryDrop}
          onClick={handleLeftBoundaryClick}
          onLayoutItemHover={onLayoutItemHover}
        />
      )}
      
      {/* Right boundary (after last item) */}
      {nonPlaceholderItems.length > 0 && (
        <InsertBoundaryMarker
          rowRef={rowRef}
          itemIndex={-2}
          insertIndex={(nonPlaceholderItems[nonPlaceholderItems.length - 1]?.FlowOrGridLayoutSortOrder || 0) + 1}
          parentHostId={layoutRowExDto.CurrentHostId}
          isHovered={hoveredRightBoundary}
          nonPlaceholderItems={nonPlaceholderItems}
          isRightBoundary={true}
          onMouseEnter={() => setHoveredRightBoundary(true)}
          onMouseLeave={() => setHoveredRightBoundary(false)}
          onDragOver={(e) => {
            e.preventDefault();
            e.stopPropagation();
            e.dataTransfer.dropEffect = 'move';
            setHoveredRightBoundary(true);
          }}
          onDrop={handleRightBoundaryDrop}
          onClick={handleRightBoundaryClick}
          onLayoutItemHover={onLayoutItemHover}
        />
      )}
      {/* <div className="testrowkey absolute top-1 left-1 bg-white text-[9px] z-1000">{layoutRowExDto.CurrentHostId}</div> */}
    </div>
  );
};

// Component to render insert boundary marker with dynamic positioning
const InsertBoundaryMarker: React.FC<{
  rowRef: React.RefObject<HTMLDivElement>;
  itemIndex: number;
  insertIndex: number;
  parentHostId?: string | number;
  isHovered: boolean;
  onMouseEnter: () => void;
  onMouseLeave: () => void;
  onDragOver: (e: React.DragEvent) => void;
  onDrop: (e: React.DragEvent) => void;
  onClick: () => void;
  nonPlaceholderItems: any[];
  isLeftBoundary?: boolean;
  isRightBoundary?: boolean;
  onLayoutItemHover?: (hostId: string | number | null) => void;
}> = ({ rowRef, itemIndex, insertIndex, parentHostId, isHovered, onMouseEnter, onMouseLeave, onDragOver, onDrop, onClick, nonPlaceholderItems, isLeftBoundary, isRightBoundary, onLayoutItemHover }) => {
  const [position, setPosition] = useState<{ left: number; top: number; height: number } | null>(null);
  const [baseZIndex, setBaseZIndex] = useState<number>(200); // Default to level 2
  
  // Determine z-index based on nesting level
  // Check if this row is inside a Section (has data-section-row-index ancestor)
  useEffect(() => {
    if (!rowRef.current) {
      setBaseZIndex(200); // Default to level 2
      return;
    }
    
    // Check if row is inside a Section by looking for data-section-row-index attribute
    let element: HTMLElement | null = rowRef.current.parentElement;
    while (element) {
      if (element.hasAttribute('data-section-row-index')) {
        setBaseZIndex(300); // Level 3: Row inside Section
        return;
      }
      element = element.parentElement;
    }
    setBaseZIndex(200); // Level 2: Top-level Row
  }, [rowRef]);
  
  useEffect(() => {
    const updatePosition = () => {
      if (!rowRef.current) return;
      
      // Find non-placeholder items by matching hostId
      const itemElements = Array.from(rowRef.current.querySelectorAll('.LayoutItemContainer')) as HTMLElement[];
      const nonPlaceholderElements: HTMLElement[] = [];
      
      itemElements.forEach((el) => {
        const hostId = el.getAttribute('data-host-id');
        if (hostId) {
          const item = nonPlaceholderItems.find((item: any) => 
            item.CurrentHostId?.toString() === hostId
          );
          if (item) {
            nonPlaceholderElements.push(el);
          }
        }
      });
      
      const rowRect = rowRef.current.getBoundingClientRect();
      
      if (isLeftBoundary && nonPlaceholderElements.length > 0) {
        // Left boundary: before first item
        const firstItem = nonPlaceholderElements[0];
        const firstRect = firstItem.getBoundingClientRect();
        const left = firstRect.left - rowRect.left;
        const top = firstRect.top - rowRect.top;
        const height = firstRect.height;
        setPosition({ left, top, height });
      } else if (isRightBoundary && nonPlaceholderElements.length > 0) {
        // Right boundary: after last item
        const lastItem = nonPlaceholderElements[nonPlaceholderElements.length - 1];
        const lastRect = lastItem.getBoundingClientRect();
        const left = lastRect.right - rowRect.left;
        const top = lastRect.top - rowRect.top;
        const height = lastRect.height;
        setPosition({ left, top, height });
      } else if (itemIndex >= 0 && itemIndex < nonPlaceholderElements.length - 1) {
        // Between items
        const currentItem = nonPlaceholderElements[itemIndex] as HTMLElement;
        const nextItem = nonPlaceholderElements[itemIndex + 1] as HTMLElement;
        
        if (currentItem && nextItem) {
          const currentRect = currentItem.getBoundingClientRect();
          const nextRect = nextItem.getBoundingClientRect();
          
          // Position boundary between the two items
          const left = (currentRect.right + nextRect.left) / 2 - rowRect.left;
          const top = Math.min(currentRect.top, nextRect.top) - rowRect.top;
          // Use the height of the taller item, not the combined height
          const height = Math.max(currentRect.height, nextRect.height);
          
          setPosition({ left, top, height });
        }
      } else {
        // Invalid itemIndex - don't render
        setPosition(null);
      }
    };
    
    updatePosition();
    window.addEventListener('resize', updatePosition);
    // Update on any layout changes
    const observer = new MutationObserver(updatePosition);
    if (rowRef.current) {
      observer.observe(rowRef.current, { childList: true, subtree: true, attributes: true });
    }
    
    return () => {
      window.removeEventListener('resize', updatePosition);
      observer.disconnect();
    };
  }, [rowRef, itemIndex, nonPlaceholderItems, isLeftBoundary, isRightBoundary]);
  
  if (!position) {
    return null;
  }
  
  return (
    <div
      className="InsertBoundary"
      data-parent-host-id={parentHostId != null ? String(parentHostId) : undefined}
      data-insert-index={insertIndex}
      style={{
        position: 'absolute',
        top: `${position.top}px`,
        height: `${position.height}px`,
        left: `${position.left}px`,
        width: '24px', // Wider hit area so drop lands on boundary when user aims at insert line
        zIndex: baseZIndex, // Level 2 (200) for top-level rows, Level 3 (300) for rows inside Section
        pointerEvents: 'auto',
        opacity: isHovered ? 1 : 0, // Only show when hovered
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        transform: 'translateX(-12px)', // Center the 24px hit area on the insert line
        cursor: 'pointer',
        backgroundColor: isHovered ? 'rgba(147, 197, 253, 0.2)' : 'transparent', // Only show background when hovered
        transition: 'opacity 0.15s, background-color 0.15s' // Smooth transition
      }}
      onMouseEnter={(e) => {
        e.stopPropagation();
        // Clear item hover state when hovering over boundary
        if (onLayoutItemHover) {
          onLayoutItemHover(null);
        }
        onMouseEnter();
      }}
      onMouseLeave={(e) => {
        e.stopPropagation();
        onMouseLeave();
      }}
      onMouseMove={(e) => {
        e.stopPropagation();
        // Prevent item hover events when mouse is over boundary
        if (onLayoutItemHover) {
          onLayoutItemHover(null);
        }
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
        e.preventDefault();
        appHelper.debugLog('InsertBoundary container clicked:', {
          hasOnClickHandler: !!onClick,
          insertIndex,
          isLeftBoundary,
          isRightBoundary
        });
        if (onClick) {
          onClick();
        } else {
          appHelper.debugLog('InsertBoundary container: onClick handler not provided');
        }
      }}
    >
      {/* Vertical double lines */}
      <div
        style={{
          position: 'absolute',
          top: '0',
          bottom: '0',
          left: '0px',
          width: '12px',
          zIndex: 201,
          pointerEvents: 'none' // Let clicks pass through to the boundary container
        }}
      >
        {/* First line */}
        <div
          style={{
            position: 'absolute',
            top: '0',
            bottom: '0',
            left: '2px', // Centered in the 12px width
            width: '3px', // Increased from 2px for better visibility
            backgroundColor: '#93c5fd',
          }}
        />
        {/* Second line */}
        <div
          style={{
            position: 'absolute',
            top: '0',
            bottom: '0',
            left: '7px', // Adjusted position for 12px boundary
            width: '3px', // Increased from 2px for better visibility
            backgroundColor: '#93c5fd',
          }}
        />
      </div>
      {/* Insert button - positioned at top for vertical boundaries */}
      <button
        className="InsertBoundaryButton"
        style={{
          position: 'absolute',
          top: '25px',
          left: isLeftBoundary 
            ? '6px' 
            : isRightBoundary 
            ? '6px' 
            : '50%',
          transform: isLeftBoundary 
            ? 'translate(-14px, -14px)' 
            : isRightBoundary 
            ? 'translate(-14px, -14px)' 
            : 'translate(-50%, -14px)',
          zIndex: baseZIndex + 2, // Button on top
          width: '28px', // Increased from 24px to accommodate index text
          height: '28px', // Increased from 24px to accommodate index text
          borderRadius: '50%',
          backgroundColor: '#6ee7b7',
          border: '2px solid white',
          color: 'white',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          cursor: 'pointer',
          boxShadow: '0 2px 4px rgba(0,0,0,0.2)',
          pointerEvents: 'auto',
          fontSize: '11px',
          fontWeight: 'bold',
          lineHeight: '1'
        }}
        onClick={(e) => {
          e.stopPropagation();
          e.preventDefault();
          appHelper.debugLog('InsertBoundary button clicked:', {
            hasOnClickHandler: !!onClick,
            insertIndex,
            isLeftBoundary,
            isRightBoundary
          });
          if (onClick) {
            onClick();
          } else {
            appHelper.debugLog('InsertBoundary button: onClick handler not provided');
          }
        }}
        title={`Insert new item at index ${insertIndex}`}
      >
        {insertIndex}
      </button>
    </div>
  );
};

export default OneLayoutRowDesign;
