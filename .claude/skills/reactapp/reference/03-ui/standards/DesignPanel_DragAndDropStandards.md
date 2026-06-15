# DOM Drag and Drop Design Standards

## Overview

This document provides comprehensive guidelines for implementing drag and drop functionality in React components, with special focus on handling nested draggable elements where hovering or dragging a child element should not affect parent elements.

## Reference Implementation

**Example Component**: `src/components/test/dragAndDrop/TestDomDragAndDrop.tsx`

This component demonstrates a complete implementation of nested drag and drop with proper hover handling.

---

## Core Principles

### 1. **Layer Isolation**
- When hovering over a child element, only the child should be highlighted, not the parent
- When dragging a child element, the parent should not be affected
- Each draggable element should be independently interactive

### 2. **Event Handling Strategy**
- Use JavaScript event handlers (`onMouseMove`, `onMouseLeave`) instead of CSS `:hover` for precise control
- Detect the closest draggable element to determine which element should be highlighted
- Prevent event bubbling when necessary to avoid parent interference

### 3. **State Management**
- Track both `draggedId` (currently being dragged) and `hoveredId` (currently hovered)
- Clear hover state appropriately when mouse leaves or drag ends
- Use React state to manage drag and hover states

---

## How Child Hover/Drag Doesn't Affect Parent

### The Problem

In nested DOM structures, when you hover over a child element, the browser's event system naturally bubbles events up to parent elements. This means:
- If you hover over a child DIV, the parent DIV's `:hover` CSS also triggers
- If you drag a child DIV, parent DIVs might also receive drag events
- This causes unwanted visual feedback on parent elements

### The Solution: DOM Traversal Logic

#### 1. **Hover Detection (`onMouseMove`)**

```tsx
const handleMouseMove = useCallback((e: React.MouseEvent, divId: string) => {
  const target = e.target;           // The actual element mouse is over (could be child)
  const currentTarget = e.currentTarget; // The element with this handler (parent)
  
  // Find the closest draggable element starting from the actual mouse position
  let closestDraggable: HTMLElement | null = null;
  let element: HTMLElement | null = target as HTMLElement;
  
  // Walk up the DOM tree to find the closest draggable element
  while (element && element !== document.body) {
    if (element.hasAttribute('draggable')) {
      closestDraggable = element;  // Found a draggable element
      break;
    }
    element = element.parentElement;  // Move up to parent
  }
  
  // KEY LOGIC: Only highlight if the closest draggable is THIS element
  // If closest draggable is a child, don't highlight the parent
  if (closestDraggable === currentTarget) {
    setDragState(prev => ({ ...prev, hoveredId: divId }));
  }
}, []);
```

**How It Works**:
1. **`e.target`** = 鼠标实际所在的元素（可能是子元素的内部元素，如 label、padding 区域等）
2. **`e.currentTarget`** = 绑定事件处理器的元素（父 DIV）
3. **向上遍历 DOM 树**：从 `target` 开始，向上查找最近的 `draggable` 元素
4. **判断逻辑**：
   - 如果最近的 draggable 元素是**子元素** → 不设置父元素的 hover（子元素会自己设置）
   - 如果最近的 draggable 元素是**当前元素** → 设置当前元素的 hover

**Example Scenario**:
```
<div id="PARENT" draggable onMouseMove={...}>
  <div id="CHILD" draggable onMouseMove={...}>
    <label>Text</label>
  </div>
</div>
```

当鼠标在 `<label>` 上时：
- `e.target` = `<label>`
- `e.currentTarget` (PARENT) = `<div id="PARENT">`
- 向上遍历：`<label>` → `<div id="CHILD">` (找到 draggable，这是子元素)
- `closestDraggable` = CHILD，`currentTarget` = PARENT
- `closestDraggable !== currentTarget` → **不设置 PARENT 的 hover**

当鼠标在 PARENT 的 padding 区域时：
- `e.target` = `<div id="PARENT">` 本身
- `e.currentTarget` (PARENT) = `<div id="PARENT">`
- 向上遍历：`<div id="PARENT">` (找到 draggable，这就是当前元素)
- `closestDraggable` = PARENT，`currentTarget` = PARENT
- `closestDraggable === currentTarget` → **设置 PARENT 的 hover**

#### 2. **Mouse Leave Detection (`onMouseLeave`)**

```tsx
const createMouseLeaveHandler = useCallback((divId: string) => {
  return (e: React.MouseEvent) => {
    const relatedTarget = e.relatedTarget;  // Where mouse is going
    const currentTarget = e.currentTarget;   // This element
    
    // Check if mouse is moving to a child draggable element
    if (relatedTarget && currentTarget.contains(relatedTarget)) {
      let element: HTMLElement | null = relatedTarget as HTMLElement;
      while (element && element !== currentTarget) {
        if (element.hasAttribute('draggable') && element !== currentTarget) {
          // Mouse is moving to a child draggable div, don't clear hover
          return;  // Keep current hover, child will set its own
        }
        element = element.parentElement;
      }
    }
    
    // Mouse is truly leaving, clear hover
    setDragState(prev => {
      if (prev.hoveredId === divId) {
        return { ...prev, hoveredId: null };
      }
      return prev;
    });
  };
}, []);
```

**How It Works**:
1. **`relatedTarget`** = 鼠标移动到的目标元素
2. **检查是否移动到子元素**：如果 `relatedTarget` 在当前元素内部，检查是否是子 draggable 元素
3. **如果移动到子元素**：不清除 hover（子元素会设置自己的 hover）
4. **如果离开元素**：清除 hover

**Example Scenario**:
从 PARENT 移动到 CHILD：
- `relatedTarget` = CHILD 或其子元素
- `currentTarget.contains(relatedTarget)` = true
- 向上遍历发现 CHILD 是 draggable → **不清除 PARENT 的 hover**（CHILD 会设置自己的 hover）

#### 3. **Drag Operations**

```tsx
const handleDragStart = useCallback((e: React.DragEvent, divId: string) => {
  e.stopPropagation();  // Prevent parent from receiving drag start
  setDragState(prev => ({ ...prev, draggedId: divId }));
  // ...
}, []);
```

**How It Works**:
- `e.stopPropagation()` 阻止事件冒泡到父元素
- 只有被拖拽的元素会设置 `draggedId`
- 父元素不会收到 `onDragStart` 事件

---

## Why `e.preventDefault()` and `e.stopPropagation()` Are Used

### `e.preventDefault()` - 阻止默认行为

**Purpose**: 阻止浏览器对事件的默认处理

**Where Used**:
1. **`onDragOver`** (必须):
   ```tsx
   e.preventDefault();  // CRITICAL: Without this, drop won't work!
   ```
   - HTML5 拖拽 API 要求：必须在 `onDragOver` 中调用 `preventDefault()` 才能允许 drop
   - 如果不调用，浏览器会阻止 drop 操作，`onDrop` 永远不会被触发

2. **`onDrop`**:
   ```tsx
   e.preventDefault();  // Prevent browser's default drop behavior
   ```
   - 阻止浏览器默认的 drop 行为（比如打开文件、导航到 URL 等）
   - 确保只有我们的代码处理 drop 操作

**NOT Used In**:
- `onDragStart`: 不需要，我们希望默认的拖拽视觉反馈
- `onDragEnd`: 不需要，默认行为是清理拖拽状态
- `onMouseMove`: 不需要，鼠标移动没有默认行为需要阻止
- `onMouseLeave`: 不需要，鼠标离开没有默认行为需要阻止

### `e.stopPropagation()` - 阻止事件冒泡

**Purpose**: 阻止事件向上传播到父元素

**Where Used**:
1. **`onDragStart`**:
   ```tsx
   e.stopPropagation();  // Prevent parent from receiving drag start
   ```
   - **为什么需要**：如果子元素开始拖拽，我们不希望父元素也收到 `onDragStart`
   - **场景**：拖拽 CHILD 时，PARENT 不应该也设置 `draggedId`

2. **`onDragOver`**:
   ```tsx
   e.stopPropagation();  // Prevent parent handlers from firing
   ```
   - **为什么需要**：当鼠标在子元素上拖拽时，父元素也会收到 `onDragOver`
   - **场景**：拖拽 CHILD 经过 PARENT 时，PARENT 不应该更新 hover 状态
   - **配合逻辑**：即使父元素收到事件，由于 `stopPropagation()`，父元素的 handler 不会执行

3. **`onDrop`**:
   ```tsx
   e.stopPropagation();  // Prevent parent from handling drop
   ```
   - **为什么需要**：drop 应该只被最内层的元素处理
   - **场景**：drop 到 CHILD 时，PARENT 不应该也处理 drop

**NOT Used In**:
- `onDragEnd`: 不需要，drag end 可以冒泡（所有元素都需要清理状态）
- `onMouseMove`: **故意不阻止**，让事件冒泡，但通过逻辑判断来决定是否设置 hover
- `onMouseLeave`: **故意不阻止**，让事件冒泡，但通过逻辑判断来决定是否清除 hover

### Why Not Stop Propagation in `onMouseMove`?

这是一个关键设计决策：

```tsx
// 我们不阻止冒泡，而是通过逻辑判断
const handleMouseMove = useCallback((e: React.MouseEvent, divId: string) => {
  // 不调用 e.stopPropagation()
  // 让事件冒泡到父元素
  
  // 但是通过查找 closestDraggable 来判断
  // 只有当前元素是最近的 draggable 时才设置 hover
  if (closestDraggable === currentTarget) {
    setDragState(prev => ({ ...prev, hoveredId: divId }));
  }
}, []);
```

**原因**：
1. **需要事件冒泡**：父元素也需要收到 `onMouseMove` 事件，以便检测鼠标是否移动到父元素的 padding 区域
2. **通过逻辑过滤**：即使父元素收到事件，通过 `closestDraggable === currentTarget` 判断，只有真正 hover 在父元素上时才设置 hover
3. **灵活性**：这样可以从子元素移动到父元素的 padding 区域时，父元素能正确响应

---

## Complete Event Flow Example

### Scenario: Hovering Over Child Element

```
DOM Structure:
<div id="A" draggable onMouseMove={handleMouseMove}>
  <div id="B" draggable onMouseMove={handleMouseMove}>
    <label>Text</label>
  </div>
</div>
```

**Mouse over `<label>` (inside B)**:

1. **Event fires on B**:
   - `e.target` = `<label>`
   - `e.currentTarget` = B
   - Find closest draggable: `<label>` → B (found!)
   - `closestDraggable === currentTarget` → ✅ Set B's hover

2. **Event bubbles to A**:
   - `e.target` = `<label>` (still)
   - `e.currentTarget` = A
   - Find closest draggable: `<label>` → B (found, this is a child!)
   - `closestDraggable !== currentTarget` → ❌ Don't set A's hover

**Result**: Only B is highlighted ✅

### Scenario: Dragging Child Element

```
Mouse drags B (child) over A (parent)
```

1. **`onDragStart` on B**:
   - `e.stopPropagation()` → Event doesn't bubble to A
   - Set `draggedId = 'B'`

2. **`onDragOver` on B** (while dragging):
   - `e.preventDefault()` → Allow drop
   - `e.stopPropagation()` → Event doesn't bubble to A
   - Set `hoveredId = 'B'`

3. **`onDragOver` on A** (if event somehow reaches):
   - Even if event reaches A, `stopPropagation()` prevents handler execution
   - A doesn't update hover state

**Result**: Only B shows drag/hover feedback ✅

---

## Summary Table

| Event Handler | `preventDefault()` | `stopPropagation()` | Reason |
|--------------|-------------------|---------------------|---------|
| `onDragStart` | ❌ No | ✅ Yes | Prevent parent from starting drag |
| `onDragEnd` | ❌ No | ❌ No | Can bubble, all elements need cleanup |
| `onDragOver` | ✅ **Must** | ✅ Yes | Allow drop + prevent parent handling |
| `onDrop` | ✅ Yes | ✅ Yes | Handle drop + prevent parent handling |
| `onMouseMove` | ❌ No | ❌ No | Need bubbling + logic filtering |
| `onMouseLeave` | ❌ No | ❌ No | Need bubbling + logic filtering |

---

## Required Event Handlers

### 1. `draggable` Attribute

**Purpose**: Enable HTML5 drag and drop API

**Implementation**:
```tsx
<div draggable>
```

**Notes**:
- Set `draggable={true}` or just `draggable` on all draggable elements
- This enables the native HTML5 drag and drop functionality

---

### 2. `onDragStart`

**Purpose**: Initialize drag operation, store dragged item ID

**Implementation**:
```tsx
const handleDragStart = useCallback((e: React.DragEvent, itemId: string) => {
  e.stopPropagation(); // Prevent event bubbling to parent DIVs
  
  e.dataTransfer.effectAllowed = 'move';
  e.dataTransfer.setData('text/plain', itemId);
  
  // CRITICAL: Delay state updates to avoid interrupting drag initialization
  // State updates cause re-renders which can interrupt the browser's drag operation
  setTimeout(() => {
    setDragState(prev => ({ ...prev, draggedId: itemId }));
  }, 0);
}, []);
```

**Key Points**:
- Always call `e.stopPropagation()` to prevent parent handlers from firing
- Set `effectAllowed` to 'move' (or 'copy' if appropriate)
- Store the dragged item ID in both state and `dataTransfer` for redundancy
- Use `text/plain` MIME type for compatibility
- **CRITICAL**: Delay state updates using `setTimeout` to prevent re-render from interrupting drag

**Why `stopPropagation()`**:
- When dragging a child element, we don't want the parent to also receive `onDragStart`
- This ensures only the actual dragged element sets `draggedId`

**Why Delay State Updates**:
- Drag initialization (setting `dataTransfer`, etc.) must complete synchronously
- State updates trigger React re-renders which can interrupt browser drag operation
- Using `setTimeout(..., 0)` defers state update to next event loop tick
- This ensures drag initialization completes before any re-render occurs
- **Without this delay**: First drag attempt may fail, requiring second attempt to succeed

**Drag Image**:
- Use browser's default drag image (semi-transparent copy of dragged element)
- Avoid custom drag images unless absolutely necessary
- Custom drag images can cause performance issues and visual inconsistencies
- To use default: Simply don't call `e.dataTransfer.setDragImage()`

**Usage**:
```tsx
<div draggable onDragStart={(e) => handleDragStart(e, node.id)}>
```

---

### 3. `onDragEnd`

**Purpose**: Clean up drag state when drag operation completes

**Implementation**:
```tsx
const handleDragEnd = useCallback(() => {
  setDragState({ draggedId: null, hoveredId: null });
}, []);
```

**Key Points**:
- Reset both `draggedId` and `hoveredId` to null
- Called whether drop was successful or cancelled
- No need to prevent propagation here (can bubble, all elements need cleanup)

**Usage**:
```tsx
<div draggable onDragEnd={handleDragEnd}>
```

---

### 4. `onDragOver`

**Purpose**: Allow drop on target element, update hover state

**Implementation**:
```tsx
const handleDragOver = useCallback((e: React.DragEvent, targetId: string) => {
  e.preventDefault(); // Required to allow drop
  e.stopPropagation(); // Prevent parent handlers
  e.dataTransfer.dropEffect = 'move';
  
  // Update hovered ID for visual feedback
  if (dragState.hoveredId !== targetId) {
    setDragState(prev => ({ ...prev, hoveredId: targetId }));
  }
}, [dragState.hoveredId]);
```

**Key Points**:
- **CRITICAL**: Must call `e.preventDefault()` to allow drop (HTML5 drag API requirement)
- Call `e.stopPropagation()` to prevent parent elements from handling the event
- Set `dropEffect` to provide visual feedback ('move', 'copy', 'link', or 'none')
- Update hover state for visual feedback during drag

**Why Both**:
- `preventDefault()`: Required by HTML5 API - without it, drop won't work
- `stopPropagation()`: When dragging over a child, parent shouldn't also update hover

**Usage**:
```tsx
<div draggable onDragOver={(e) => handleDragOver(e, node.id)}>
```

---

### 5. `onDrop`

**Purpose**: Handle the actual drop operation, move item to new location

**Implementation**:
```tsx
const handleDrop = useCallback((e: React.DragEvent, targetId: string) => {
  e.preventDefault();
  e.stopPropagation();

  const draggedId = dragState.draggedId || e.dataTransfer.getData('text/plain');
  if (!draggedId || draggedId === targetId) {
    setDragState({ draggedId: null, hoveredId: null });
    return;
  }

  // Prevent dropping into own descendants
  if (isDescendant(itemTree, draggedId, targetId)) {
    setDragState({ draggedId: null, hoveredId: null });
    return;
  }

  // Find the item to move
  const itemToMove = findNode(itemTree, draggedId);
  if (!itemToMove) {
    setDragState({ draggedId: null, hoveredId: null });
    return;
  }

  // Remove from old location
  let newTree = removeNode(itemTree, draggedId);
  if (!newTree) {
    setDragState({ draggedId: null, hoveredId: null });
    return;
  }

  // Add to new location (append to children)
  newTree = addNodeToTarget(newTree, targetId, itemToMove);
  setItemTree(newTree);
  setDragState({ draggedId: null, hoveredId: null });
}, [dragState.draggedId, itemTree, findNode, removeNode, addNodeToTarget, isDescendant]);
```

**Key Points**:
- Always call `e.preventDefault()` and `e.stopPropagation()`
- Get dragged ID from state (preferred) or `dataTransfer` (fallback)
- Validate: check if drop is valid (not same item, not into own descendants)
- Update data structure: remove from old location, add to new location
- Reset drag state after operation

**Why Both**:
- `preventDefault()`: Prevent browser's default drop behavior (open file, navigate, etc.)
- `stopPropagation()`: Drop should only be handled by the innermost element

**Validation Checks**:
1. Dragged ID exists and is different from target
2. Target is not a descendant of dragged item (prevents circular references)
3. Item to move exists in tree

**Usage**:
```tsx
<div draggable onDrop={(e) => handleDrop(e, node.id)}>
```

---

### 6. `onMouseMove`

**Purpose**: Detect which element the mouse is actually over (for hover highlighting)

**Implementation**:
```tsx
const handleMouseMove = useCallback((e: React.MouseEvent, itemId: string) => {
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
  if (closestDraggable === currentTarget) {
    setDragState(prev => ({ ...prev, hoveredId: itemId }));
  }
}, []);
```

**Key Points**:
- **Critical for nested elements**: Finds the closest draggable parent from the event target
- Only highlights if the closest draggable is the current element (not a child)
- Validates Node types to prevent errors
- This ensures parent elements don't get highlighted when hovering over children
- **Does NOT use `stopPropagation()`**: We want events to bubble so parent can detect mouse in padding area

**Why This Works**:
- When mouse is over a child element, `e.target` is the child
- We walk up the DOM tree to find the closest draggable element
- If the closest draggable is a child, we don't highlight the parent
- Only when the closest draggable is the current element do we highlight it

**Why No `stopPropagation()`**:
- We need events to bubble to parent elements
- Parent needs to detect when mouse moves to its padding area (not over child)
- Logic filtering (`closestDraggable === currentTarget`) prevents unwanted highlighting

**Usage**:
```tsx
<div draggable onMouseMove={(e) => handleMouseMove(e, node.id)}>
```

---

### 7. `onMouseLeave`

**Purpose**: Clear hover state when mouse leaves the element

**Implementation**:
```tsx
const createMouseLeaveHandler = useCallback((itemId: string) => {
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
      if (prev.hoveredId === itemId) {
        return { ...prev, hoveredId: null };
      }
      return prev;
    });
  };
}, []);
```

**Key Points**:
- **Critical validation**: Check that `relatedTarget` is a valid Node before using `contains()`
- Check if mouse is moving to a child draggable element
- If moving to child, don't clear hover (child will set its own hover)
- Only clear hover if mouse is truly leaving the element
- Use factory function pattern to create handler with item ID
- **Does NOT use `stopPropagation()`**: We want events to bubble for proper cleanup

**Why This Works**:
- `relatedTarget` tells us where the mouse is going
- If it's moving to a child draggable element, we keep the current hover
- If it's leaving the element entirely, we clear the hover
- This prevents flickering when moving between parent and child

**Why No `stopPropagation()`**:
- Similar to `onMouseMove`, we need events to bubble
- Logic filtering prevents unwanted state clearing

**Usage**:
```tsx
<div draggable onMouseLeave={createMouseLeaveHandler(node.id)}>
```

---

## Complete Event Handler Setup

Here's how to attach all event handlers to a draggable element:

```tsx
<div
  draggable
  onDragStart={(e) => handleDragStart(e, itemId)}
  onDragEnd={handleDragEnd}
  onDragOver={(e) => handleDragOver(e, itemId)}
  onDrop={(e) => handleDrop(e, itemId)}
  onMouseMove={(e) => handleMouseMove(e, itemId)}
  onMouseLeave={createMouseLeaveHandler(itemId)}
  style={{
    // Visual feedback based on state
    backgroundColor: isHovered ? '#FF6B6B' : normalColor,
    borderColor: isHovered ? '#FF0000' : normalBorderColor,
    opacity: isDragged ? 0.5 : 1,
    cursor: 'move',
    // ... other styles
  }}
>
  {/* Content */}
</div>
```

---

## State Management Pattern

### State Interface

```tsx
interface DragState {
  draggedId: string | null;  // ID of item currently being dragged
  hoveredId: string | null;  // ID of item currently hovered
}
```

### State Initialization

```tsx
const [dragState, setDragState] = useState<DragState>({
  draggedId: null,
  hoveredId: null
});
```

### State Updates

- **Drag Start**: Set `draggedId`
- **Drag Over**: Update `hoveredId` for visual feedback
- **Mouse Move**: Update `hoveredId` for hover highlighting
- **Mouse Leave**: Clear `hoveredId` if appropriate
- **Drag End / Drop**: Reset both `draggedId` and `hoveredId` to null

---

## Tree Manipulation Functions

### Finding a Node

```tsx
const findNode = useCallback((tree: TreeNode, id: string): TreeNode | null => {
  if (tree.id === id) return tree;
  for (const child of tree.children) {
    const found = findNode(child, id);
    if (found) return found;
  }
  return null;
}, []);
```

### Removing a Node

```tsx
const removeNode = useCallback((tree: TreeNode, id: string): TreeNode | null => {
  if (tree.id === id) return null; // Remove this node
  return {
    ...tree,
    children: tree.children
      .map(child => removeNode(child, id))
      .filter((node): node is TreeNode => node !== null)
  };
}, []);
```

### Adding a Node to Target

```tsx
const addNodeToTarget = useCallback((tree: TreeNode, targetId: string, nodeToAdd: TreeNode): TreeNode => {
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
```

### Checking Descendants (Prevent Circular Drops)

```tsx
const isDescendant = useCallback((tree: TreeNode, sourceId: string, targetId: string): boolean => {
  const sourceNode = findNode(tree, sourceId);
  if (!sourceNode) return false;
  
  const checkDescendant = (node: TreeNode): boolean => {
    if (node.id === targetId) return true;
    return node.children.some(child => checkDescendant(child));
  };
  
  return checkDescendant(sourceNode);
}, [findNode]);
```

---

## Visual Feedback Patterns

### Hover Highlighting

```tsx
const isHovered = dragState.hoveredId === node.id;
const finalBgColor = isHovered ? '#FF6B6B' : normalBgColor;
const finalBorderColor = isHovered ? '#FF0000' : normalBorderColor;
```

### Drag Feedback

```tsx
const isDragged = dragState.draggedId === node.id;
const opacity = isDragged ? 0.5 : 1;
```

### Combined Styles

```tsx
style={{
  backgroundColor: finalBgColor,
  border: `2px solid ${finalBorderColor}`,
  opacity: isDragged ? 0.5 : 1,
  transition: 'background-color 0.2s, border-color 0.2s',
  cursor: 'move',
  // ... other styles
}}
```

---

## React Hooks Rules for Drag and Drop

### ⚠️ CRITICAL: Hooks Must Be Defined Before Early Returns

**Problem**: React Hooks must be called in the exact same order on every render. If hooks are called after conditional early returns, React will throw errors.

**Example Error**:
```
React Hook "useCallback" is called conditionally. React Hooks must be called in the exact same order in every component render. Did you accidentally call a React Hook after an early return?
```

**Solution**: Define all hooks at the top of the component, before any early returns:

```tsx
const MyComponent = ({ item }) => {
  // ✅ CORRECT: All hooks defined first
  const [dragState, setDragState] = useState({ draggedId: null, hoveredId: null });
  const handleDragStart = useCallback((e) => { /* ... */ }, []);
  const handleDragEnd = useCallback(() => { /* ... */ }, []);
  const handleMouseMove = useCallback((e) => { /* ... */ }, []);
  
  // Early returns AFTER all hooks
  if (!item) return null;
  if (!item.isVisible) return null;
  
  // Rest of component...
};
```

**❌ WRONG**:
```tsx
const MyComponent = ({ item }) => {
  if (!item) return null; // ❌ Early return before hooks
  
  const handleDragStart = useCallback((e) => { /* ... */ }, []); // ❌ Hook after return
};
```

**Best Practice**:
- Define all `useState`, `useCallback`, `useMemo`, `useEffect` hooks at the very top
- Place all early returns (`if (...) return null`) after hooks
- This ensures hooks are always called in the same order

---

## Key Implementation Rules

### ✅ DO

1. **Always prevent default and stop propagation** in `onDragOver` and `onDrop`
2. **Validate Node types** before using DOM methods like `contains()`
3. **Find closest draggable element** in `onMouseMove` to determine hover target
4. **Check for child elements** in `onMouseLeave` before clearing hover
5. **Prevent circular drops** by checking if target is descendant of source
6. **Store dragged ID in both state and dataTransfer** for redundancy
7. **Reset state** in both `onDragEnd` and `onDrop` handlers
8. **Use useCallback** for all event handlers to prevent unnecessary re-renders
9. **Use `stopPropagation()`** in drag events to prevent parent interference
10. **Don't use `stopPropagation()`** in mouse events - use logic filtering instead
11. **Delay state updates in `onDragStart`** using `setTimeout` to prevent interrupting drag initialization
12. **Define all hooks before early returns** to comply with React Hooks rules
13. **Use browser's default drag image** unless custom image is absolutely necessary

### ❌ DON'T

1. **Don't use CSS `:hover`** for nested draggable elements (it bubbles to parents)
2. **Don't forget to validate** Node types before DOM operations
3. **Don't skip `e.preventDefault()`** in `onDragOver` (drop won't work)
4. **Don't allow dropping** into own descendants (creates circular references)
5. **Don't forget to clear state** when drag operation ends
6. **Don't use event bubbling** without checking if target is a child element
7. **Don't stop propagation** in `onMouseMove` or `onMouseLeave` (need bubbling for padding area detection)
8. **Don't call state updates synchronously in `onDragStart`** - this causes re-render that interrupts drag
9. **Don't define hooks after early returns** - violates React Hooks rules
10. **Don't create custom drag images unnecessarily** - use browser default for consistency

---

## Common Patterns

### Pattern 1: Simple List Drag and Drop

For flat lists without nesting:

```tsx
// Simpler - no need for closest draggable detection
const handleMouseMove = useCallback((e: React.MouseEvent, itemId: string) => {
  setDragState(prev => ({ ...prev, hoveredId: itemId }));
}, []);
```

### Pattern 2: Nested Tree Drag and Drop

For nested structures (like the reference component):

```tsx
// Complex - need to find closest draggable element
const handleMouseMove = useCallback((e: React.MouseEvent, itemId: string) => {
  // ... find closest draggable logic ...
}, []);
```

### Pattern 3: Copy vs Move

For copy operations:

```tsx
// In handleDragStart
e.dataTransfer.effectAllowed = 'copy';

// In handleDragOver
e.dataTransfer.dropEffect = 'copy';

// In handleDrop - don't remove from source, just add to target
```

---

## Troubleshooting

### Problem: Parent elements highlight when hovering children

**Solution**: Use `onMouseMove` with closest draggable detection (see implementation above)

### Problem: Drop doesn't work

**Solution**: Ensure `e.preventDefault()` is called in `onDragOver`

### Problem: Browser crashes with "contains()" error

**Solution**: Validate Node types before using DOM methods:
```tsx
if (relatedTarget instanceof Node && currentTarget instanceof Node) {
  // Safe to use contains()
}
```

### Problem: Hover state doesn't clear properly

**Solution**: Check `relatedTarget` in `onMouseLeave` to see if moving to child element

### Problem: Can drop item into its own children

**Solution**: Implement `isDescendant` check in `handleDrop`

### Problem: Parent receives drag events when dragging child

**Solution**: Use `e.stopPropagation()` in `onDragStart`, `onDragOver`, and `onDrop`

### Problem: Need to drag twice before drag starts working

**Symptoms**: 
- First drag attempt doesn't start drag operation
- Second drag attempt works correctly
- Console may show no errors

**Root Cause**: 
State updates in `onDragStart` cause React re-render, which interrupts browser's drag initialization. The drag operation must complete initialization synchronously, but re-render happens before initialization completes.

**Solution**: Delay state updates using `setTimeout`:
```tsx
const handleDragStart = useCallback((e: React.DragEvent, itemId: string) => {
  e.stopPropagation();
  e.dataTransfer.effectAllowed = 'move';
  e.dataTransfer.setData('text/plain', itemId);
  
  // Delay state update to avoid interrupting drag initialization
  setTimeout(() => {
    setDragState(prev => ({ ...prev, draggedId: itemId }));
  }, 0);
}, []);
```

**Why This Works**:
- `setTimeout(..., 0)` defers state update to next event loop tick
- Drag initialization completes synchronously before re-render occurs
- State update happens after drag is fully initialized

### Problem: React Hook called conditionally error

**Error Message**:
```
React Hook "useCallback" is called conditionally. React Hooks must be called in the exact same order in every component render.
```

**Root Cause**: 
Hooks are defined after early returns (`if (...) return null`), causing them to be called conditionally.

**Solution**: Move all hooks to the top of the component, before any early returns:
```tsx
const MyComponent = ({ item }) => {
  // ✅ All hooks first
  const [state, setState] = useState();
  const handleDragStart = useCallback(() => {}, []);
  
  // ✅ Early returns after hooks
  if (!item) return null;
  
  return <div>...</div>;
};
```

### Problem: Custom drag image causes visual inconsistencies

**Symptoms**:
- Drag image doesn't match dragged element
- Drag image appears in wrong position
- Performance issues during drag

**Solution**: Use browser's default drag image by not calling `e.dataTransfer.setDragImage()`:
```tsx
const handleDragStart = useCallback((e: React.DragEvent) => {
  // Don't call setDragImage - browser will use default
  e.dataTransfer.effectAllowed = 'move';
  e.dataTransfer.setData('text/plain', itemId);
}, []);
```

**When to Use Custom Drag Image**:
- Only when you need to show different content than the dragged element
- When dragged element is too large or complex
- When you need specific visual feedback during drag

---

## Testing Checklist

When implementing drag and drop, verify:

- [ ] Can drag any element to any other element
- [ ] Hovering child element only highlights child, not parent
- [ ] Dragging child element doesn't affect parent
- [ ] Cannot drop item into its own descendants
- [ ] Visual feedback works (hover highlight, drag opacity)
- [ ] State resets properly after drag ends
- [ ] No browser errors or crashes
- [ ] Works with deeply nested structures (3+ levels)

---

## Reference Files

- **Example Implementation**: `src/components/test/dragAndDrop/TestDomDragAndDrop.tsx`
- **Related Standards**: 
  - `PageLayoutStandards.md` - For page structure
  - `FormStandards.md` - For form-related drag and drop

---

## Summary

Implementing drag and drop with nested elements requires:

1. **Proper event handling**: All 7 event handlers must be implemented correctly
2. **State management**: Track both dragged and hovered states
3. **DOM traversal**: Find closest draggable element for accurate hover detection
4. **Validation**: Check Node types and prevent invalid drops
5. **Visual feedback**: Provide clear visual cues for drag and hover states
6. **Event propagation control**: Use `stopPropagation()` for drag events, logic filtering for mouse events
7. **React Hooks compliance**: Define all hooks before early returns
8. **Drag initialization timing**: Delay state updates in `onDragStart` to prevent interrupting drag

The key challenge is ensuring that child elements don't affect parent elements during hover or drag operations. This is achieved through:
- **DOM traversal logic** in `onMouseMove` to find the closest draggable element
- **Event propagation control** using `stopPropagation()` in drag events
- **Logic filtering** in mouse events to allow bubbling while preventing unwanted effects
- **Delayed state updates** in `onDragStart` to allow drag initialization to complete before re-render

## Common Pitfalls and Solutions

### Pitfall 1: Drag Requires Two Attempts
**Cause**: Synchronous state update in `onDragStart` interrupts drag initialization  
**Solution**: Use `setTimeout` to delay state updates

### Pitfall 2: React Hooks Error
**Cause**: Hooks defined after early returns  
**Solution**: Move all hooks to component top, before any returns

### Pitfall 3: Custom Drag Image Issues
**Cause**: Custom drag images can cause visual/performance problems  
**Solution**: Use browser default drag image unless custom is necessary

### Pitfall 4: Parent Highlighting When Hovering Child
**Cause**: CSS `:hover` or event bubbling without filtering  
**Solution**: Use `onMouseMove` with closest draggable detection logic