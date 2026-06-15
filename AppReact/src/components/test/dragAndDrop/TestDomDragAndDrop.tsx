import React, { useState, useCallback } from 'react';

interface DivNode {
  id: string;
  children: DivNode[];
}

interface DragState {
  draggedId: string | null;
  hoveredId: string | null;
}

const TestDomDragAndDrop: React.FC = () => {
  // Initial nested structure matching the screenshot layout
  // Outer purple container -> Inner orange container -> Left/Right sections -> Top/Bottom areas -> Inner boxes
  const [divTree, setDivTree] = useState<DivNode>({
    id: 'root',
    children: [
      {
        id: 'A',
        children: [
          {
            id: 'B',
            children: [
              {
                id: 'C',
                children: [
                  {
                    id: 'D',
                    children: [
                      { id: 'E', children: [] },
                      { id: 'F', children: [] }
                    ]
                  },
                  {
                    id: 'G',
                    children: [
                      { id: 'H', children: [] },
                      { id: 'I', children: [] }
                    ]
                  }
                ]
              },
              {
                id: 'J',
                children: [
                  {
                    id: 'K',
                    children: [
                      { id: 'L', children: [] },
                      { id: 'M', children: [] }
                    ]
                  },
                  {
                    id: 'N',
                    children: [
                      { id: 'O', children: [] },
                      { id: 'P', children: [] }
                    ]
                  }
                ]
              }
            ]
          }
        ]
      }
    ]
  });

  const [dragState, setDragState] = useState<DragState>({
    draggedId: null,
    hoveredId: null
  });

  // Find node by ID in the tree
  const findNode = useCallback((tree: DivNode, id: string): DivNode | null => {
    if (tree.id === id) return tree;
    for (const child of tree.children) {
      const found = findNode(child, id);
      if (found) return found;
    }
    return null;
  }, []);

  // Remove node from tree
  const removeNode = useCallback((tree: DivNode, id: string): DivNode | null => {
    if (tree.id === id) return null;
    return {
      ...tree,
      children: tree.children
        .map(child => removeNode(child, id))
        .filter((node): node is DivNode => node !== null)
    };
  }, []);

  // Add node to target's children
  const addNodeToTarget = useCallback((tree: DivNode, targetId: string, nodeToAdd: DivNode): DivNode => {
    if (tree.id === targetId) {
      return {
        ...tree,
        children: [...tree.children, nodeToAdd]
      };
    }
    return {
      ...tree,
      children: tree.children.map(child => addNodeToTarget(child, targetId, nodeToAdd))
    };
  }, []);

  // Check if targetId is a descendant of sourceId (prevent dropping into own children)
  const isDescendant = useCallback((tree: DivNode, sourceId: string, targetId: string): boolean => {
    const sourceNode = findNode(tree, sourceId);
    if (!sourceNode) return false;
    
    const checkDescendant = (node: DivNode): boolean => {
      if (node.id === targetId) return true;
      return node.children.some(child => checkDescendant(child));
    };
    
    return checkDescendant(sourceNode);
  }, [findNode]);

  // Handle drag start
  const handleDragStart = useCallback((e: React.DragEvent, divId: string) => {
    e.stopPropagation();
    setDragState(prev => ({ ...prev, draggedId: divId }));
    e.dataTransfer.effectAllowed = 'move';
    e.dataTransfer.setData('text/plain', divId);
  }, []);

  // Handle drag end
  const handleDragEnd = useCallback(() => {
    setDragState({ draggedId: null, hoveredId: null });
  }, []);

  // Handle mouse move - detect which DIV the mouse is actually over
  const handleMouseMove = useCallback((e: React.MouseEvent, divId: string) => {
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
    // This means mouse is directly over this DIV (not over a child draggable DIV)
    if (closestDraggable === currentTarget) {
      setDragState(prev => ({ ...prev, hoveredId: divId }));
    }
  }, []);

  // Handle mouse leave - clear hover when mouse leaves this DIV
  const createMouseLeaveHandler = useCallback((divId: string) => {
    return (e: React.MouseEvent) => {
      const relatedTarget = e.relatedTarget;
      const currentTarget = e.currentTarget as HTMLElement;
      
      // Validate that relatedTarget is a valid Node before using contains()
      // relatedTarget can be null, or in some cases (like during drag) might not be a valid Node
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
      setDragState(prev => {
        if (prev.hoveredId === divId) {
          return { ...prev, hoveredId: null };
        }
        return prev;
      });
    };
  }, []);

  // Handle drag over
  const handleDragOver = useCallback((e: React.DragEvent, divId: string) => {
    e.preventDefault();
    e.stopPropagation();
    e.dataTransfer.dropEffect = 'move';
    
    // Update hovered ID
    if (dragState.hoveredId !== divId) {
      setDragState(prev => ({ ...prev, hoveredId: divId }));
    }
  }, [dragState.hoveredId]);

  // Handle drop
  const handleDrop = useCallback((e: React.DragEvent, targetId: string) => {
    e.preventDefault();
    e.stopPropagation();

    const draggedId = dragState.draggedId || e.dataTransfer.getData('text/plain');
    if (!draggedId || draggedId === targetId) {
      setDragState({ draggedId: null, hoveredId: null });
      return;
    }

    // Prevent dropping into own descendants
    if (isDescendant(divTree, draggedId, targetId)) {
      setDragState({ draggedId: null, hoveredId: null });
      return;
    }

    // Find the node to move
    const nodeToMove = findNode(divTree, draggedId);
    if (!nodeToMove) {
      setDragState({ draggedId: null, hoveredId: null });
      return;
    }

    // Remove from old location
    let newTree = removeNode(divTree, draggedId);
    if (!newTree) {
      setDragState({ draggedId: null, hoveredId: null });
      return;
    }

    // Add to new location (append to children)
    newTree = addNodeToTarget(newTree, targetId, nodeToMove);
    setDivTree(newTree);
    setDragState({ draggedId: null, hoveredId: null });
  }, [dragState.draggedId, divTree, findNode, removeNode, addNodeToTarget, isDescendant]);

  // Render a DIV node recursively
  const renderDiv = useCallback((node: DivNode, level: number = 0): JSX.Element => {
    const isHovered = dragState.hoveredId === node.id;
    const isDragged = dragState.draggedId === node.id;
    
    // Color scheme: alternate between purple and orange based on level
    const isPurple = level % 2 === 0;
    const bgColor = isPurple ? '#E6D5F7' : '#FFE5CC';
    const borderColor = isPurple ? '#B794D4' : '#FFB366';
    
    // Highlight color when hovered
    const finalBgColor = isHovered ? '#FF6B6B' : bgColor;
    const finalBorderColor = isHovered ? '#FF0000' : borderColor;

    // Default layout for all DIVs - horizontal layout with wrap
    return (
      <div
        key={node.id}
        draggable
        onDragStart={(e) => handleDragStart(e, node.id)}
        onDragEnd={handleDragEnd}
        onDragOver={(e) => handleDragOver(e, node.id)}
        onDrop={(e) => handleDrop(e, node.id)}
        onMouseMove={(e) => handleMouseMove(e, node.id)}
        onMouseLeave={createMouseLeaveHandler(node.id)}
        style={{
          backgroundColor: finalBgColor,
          border: `2px solid ${finalBorderColor}`,
          borderRadius: '4px',
          padding: node.children.length > 0 ? '15px' : '20px',
          margin: '5px',
          minHeight: node.children.length > 0 ? '100px' : '60px',
          minWidth: node.children.length > 0 ? '150px' : '100px',
          cursor: 'move',
          boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
          opacity: isDragged ? 0.5 : 1,
          transition: 'background-color 0.2s, border-color 0.2s',
          display: 'flex',
          flexDirection: 'row',
          flexWrap: 'wrap',
          gap: '10px',
          position: 'relative',
          alignItems: 'flex-start'
        }}
      >
        <div style={{ 
          fontSize: node.children.length > 0 ? '12px' : '10px', 
          fontWeight: 'bold', 
          position: 'absolute',
          top: '5px',
          left: '10px',
          zIndex: 10,
          color: isHovered ? '#FFFFFF' : '#333',
          pointerEvents: 'none'
        }}>
          {node.id}
        </div>
        {node.children.length > 0 && (
          <>
            {node.children.map(child => renderDiv(child, level + 1))}
          </>
        )}
      </div>
    );
  }, [dragState, handleDragStart, handleDragEnd, handleDragOver, handleDrop, handleMouseMove, createMouseLeaveHandler]);

  return (
    <div style={{ 
      padding: '20px',
      backgroundColor: '#FFFFFF',
      minHeight: '100vh'
    }}>
      <h2 style={{ marginBottom: '20px', color: '#333' }}>DOM Drag and Drop Test</h2>
      <p style={{ marginBottom: '20px', color: '#666' }}>
        Drag any DIV to another DIV to move it. Hover over a DIV to highlight it (only the current layer).
      </p>
      
      <div style={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'flex-start',
        padding: '20px'
      }}>
        {divTree.children.map(child => renderDiv(child, 0))}
      </div>
    </div>
  );
};

export default TestDomDragAndDrop;
