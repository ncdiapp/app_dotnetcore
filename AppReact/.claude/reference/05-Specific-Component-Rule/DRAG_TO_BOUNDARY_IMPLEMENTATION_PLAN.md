# 拖拽到 BOUNDARY 区域插入功能实现计划

## 功能概述

在 Angular 版本中，除了可以将左边的 FIELD ITEM 拖拽到 PLACEHOLDER（已实现），还能拖拽到上下左右 BOUNDARY 区域进行插入。本计划详细说明如何在 React 版本中实现此功能。

## 当前状态分析

### 已实现的功能
1. ✅ **拖拽到 PLACEHOLDER**：从 `AddFieldToolbox` 拖拽 FIELD ITEM 到 `NewItemAddButton` (placeholder) 已实现
   - 处理函数：`onDropToNewItemButton` (FormDesign.tsx:2722)
   - 数据流：AddFieldToolbox → onDragStart → dataTransfer → onDropToNewItemButton → convertPlaceholderToItem

2. ✅ **BOUNDARY 区域 UI 已存在**：
   - **左右边界**：`OneLayoutRowDesign.tsx` 中已实现 `InsertBoundaryMarker` 组件
     - 左边界：`hoveredLeftBoundary` (line 54)
     - 右边界：`hoveredRightBoundary` (line 55)
     - 中间边界：`hoveredInsertIndex` (line 52)
   - **上下边界**：
     - Section 内的行边界：`OneLayoutItemDesign.tsx` 中的 `HorizontalRowBoundary` (line 484)
     - 顶层行边界：`FormLayoutDesignArea.tsx` 中的 `HorizontalRowBoundary` (line 493)

3. ✅ **拖拽处理函数已存在**：
   - `handleDropToInsertBoundary` (FormDesign.tsx:1755) - 处理行内边界插入
   - `handleDropToRowBoundary` (FormDesign.tsx:1566) - 处理行边界插入
   测试现在插入动作无效果，需要检查是否真正实现

### 需要实现的功能

**问题**：虽然 BOUNDARY 区域的 UI 和拖拽处理函数已存在，但可能只处理了从现有 layout item 的拖拽，需要确保也能正确处理从 `AddFieldToolbox` 拖拽的 FIELD ITEM。

## 实现步骤

### 步骤 1：验证当前拖拽数据流

**目标**：确认从 `AddFieldToolbox` 拖拽的数据能否被 BOUNDARY 处理函数正确识别

**检查点**：
1. `AddFieldToolbox.tsx` (line 258-289) 中的 `onDragStart` 是否正确设置了所有必要的数据：
   - ✅ `text/plain` (JSON 格式)
   - ✅ `application/drag-type`
   - ✅ `application/drag-transaction-field-id`
   - ✅ `data-drag-type` 属性
   - ✅ `data-drag-transaction-field-id` 属性

2. `handleDropToInsertBoundary` (FormDesign.tsx:1755) 是否能正确读取这些数据：
   - ✅ 从 `dataTransfer.getData` 读取
   - ✅ 从 `text/plain` JSON 解析
   - ✅ 从 `currentDragData` state 回退

3. `handleDropToRowBoundary` (FormDesign.tsx:1566) 是否也有相同的数据读取逻辑

**预期结果**：如果数据流完整，这两个函数应该已经能处理从 AddFieldToolbox 的拖拽。如果不行，需要修复数据读取逻辑。

---

### 步骤 2：确保 BOUNDARY 区域能接收拖拽事件

**目标**：确保 BOUNDARY 区域的拖拽事件处理正确配置

**检查点**：

#### 2.1 左右边界（OneLayoutRowDesign.tsx）

**文件**：`src/components/formMgt/FormDesign/OneLayoutRowDesign.tsx`

**检查**：
1. `InsertBoundaryMarker` 组件 (line 396-614) 是否正确设置了拖拽事件：
   - ✅ `onDragOver` - 应该 preventDefault 并设置 dropEffect
   - ✅ `onDrop` - 应该调用 `onDropToInsertBoundary`
   - ✅ `pointerEvents: 'auto'` - 确保可以接收事件

2. 左边界和右边界 (line 347-389) 是否也正确配置了拖拽事件

**需要验证的代码**：
```typescript
// Line 518-528: InsertBoundaryMarker 的 onDragOver 和 onDrop
onDragOver={(e) => {
  e.preventDefault();
  e.stopPropagation();
  e.dataTransfer.dropEffect = 'copy';  // ✅ 已设置
  onDragOver(e);
}}
onDrop={(e) => {
  e.preventDefault();
  e.stopPropagation();
  onDrop(e);  // ✅ 应该调用 onDropToInsertBoundary
}}
```

#### 2.2 上下边界（Section 内）

**文件**：`src/components/formMgt/FormDesign/OneLayoutItemDesign.tsx`

**检查**：
1. `HorizontalRowBoundary` 组件 (line 484-656) 是否正确设置了拖拽事件
2. `SectionRowsContainer` (line 245-481) 中的边界是否都连接了 `onDropToInsertBoundary`

**需要验证的代码**：
```typescript
// Line 377-386, 437-446, 464-473: HorizontalRowBoundary 的 onDrop
onDrop={(e) => {
  e.preventDefault();
  e.stopPropagation();
  // ⚠️ 需要检查：是否调用了 onDropToInsertBoundary？
}}
```

**问题发现**：在 `OneLayoutItemDesign.tsx` 的 `SectionRowsContainer` 中，`HorizontalRowBoundary` 的 `onDrop` 处理是空的（line 386, 446, 473），需要实现。

#### 2.3 顶层行边界（FormLayoutDesignArea.tsx）

**文件**：`src/components/formMgt/FormDesign/FormLayoutDesignArea.tsx`

**检查**：
1. `HorizontalRowBoundary` 组件 (line 493-678) 是否正确调用了 `onDropToRowBoundary`

**需要验证的代码**：
```typescript
// Line 330-336, 438-444, 471-477: 应该调用 onDropToRowBoundary
onDrop={(e) => {
  if (onDropToRowBoundary) {
    e.preventDefault();
    e.stopPropagation();
    onDropToRowBoundary(e, insertIndex);  // ✅ 已正确实现
  }
}}
```

---

### 步骤 3：修复 Section 内行边界的拖拽处理

**目标**：实现 `OneLayoutItemDesign.tsx` 中 `SectionRowsContainer` 内行边界的拖拽处理

**文件**：`src/components/formMgt/FormDesign/OneLayoutItemDesign.tsx`

**修改位置**：
1. `SectionRowsContainer` 组件 (line 245-481)
2. `HorizontalRowBoundary` 的 `onDrop` 处理 (line 383-386, 443-446, 470-473)

**需要修改**：

```typescript
// 当前代码（line 383-386）：
onDrop={(e) => {
  e.preventDefault();
  e.stopPropagation();
  // ⚠️ 空实现
}}

// 应该改为：
onDrop={(e) => {
  if (onDropToInsertBoundary && sectionRows.length > 0) {
    e.preventDefault();
    e.stopPropagation();
    // 计算正确的 insertIndex
    const insertIndex = 0;  // 对于第一个边界（rowIndex === -1）
    // 或者
    const insertIndex = rowIndex + 1;  // 对于中间边界
    // 或者
    const insertIndex = sortedRows.length;  // 对于最后一个边界
    
    // 需要找到对应的 parentRow（section 本身）
    onDropToInsertBoundary(e, layoutItemExDto, insertIndex);
  }
}}
```

**挑战**：
- 需要确定正确的 `parentRow`：应该是包含这些行的 Section 容器（`layoutItemExDto`）
- 需要计算正确的 `insertIndex`：基于 `rowIndex` 和 `sortedRows`

**实现细节**：
1. 在 `SectionRowsContainer` 中，`parentRow` 应该是 `layoutItemExDto`（Section 本身）
2. `insertIndex` 应该基于 `FlowOrGridLayoutSortOrder` 计算
3. 需要确保 `onDropToInsertBoundary` 被正确传递到 `SectionRowsContainer`

---

### 步骤 4：验证 handleDropToInsertBoundary 能处理新字段

**目标**：确保 `handleDropToInsertBoundary` 能正确处理从 AddFieldToolbox 拖拽的新字段

**文件**：`src/components/formMgt/FormDesign.tsx`

**检查点**：
1. `handleDropToInsertBoundary` (line 1755-1854) 是否能识别新字段拖拽：
   - ✅ 检查 `layoutItemUiId` - 如果存在，是移动现有项
   - ✅ 检查 `itemType` - 如果存在且没有 `layoutItemUiId`，是新字段

2. 新字段插入逻辑 (line 1842-1853)：
   ```typescript
   } else if (itemType !== undefined) {
     // Dragging a new item from toolbox - insert at index
     handleAddLayoutItem(
       itemType,
       transactionFieldId,
       gridTransactionUnitId,
       commandActionId,
       linkedSearchId,
       undefined,
       insertAtIndex  // ✅ 已传递 insertAtIndex
     );
   }
   ```

3. `handleAddLayoutItem` 是否支持 `insertAtIndex` 参数

**需要验证**：
- `handleAddLayoutItem` 函数签名是否包含 `insertAtIndex` 参数
- 如果包含，是否正确使用该参数插入到指定位置

---

### 步骤 5：验证 handleDropToRowBoundary 能处理新字段

**目标**：确保 `handleDropToRowBoundary` 能正确处理从 AddFieldToolbox 拖拽的新字段

**文件**：`src/components/formMgt/FormDesign.tsx`

**检查点**：
1. `handleDropToRowBoundary` (line 1566-1722) 是否能识别新字段拖拽
2. 新字段插入逻辑是否正确创建新行并插入字段

**需要验证**：
- 函数是否能正确创建新行
- 是否能将字段插入到新行的正确位置

---

### 步骤 6：测试所有边界场景

**目标**：确保所有边界都能正确接收和处理拖拽

**测试场景**：

#### 6.1 左右边界测试
- [ ] 拖拽到行的最左边界（第一个 item 之前）
- [ ] 拖拽到行的最右边界（最后一个 item 之后）
- [ ] 拖拽到两个 item 之间的边界

#### 6.2 上下边界测试（顶层）
- [ ] 拖拽到第一行之前
- [ ] 拖拽到两行之间
- [ ] 拖拽到最后一行之后

#### 6.3 上下边界测试（Section 内）
- [ ] 拖拽到 Section 内第一行之前
- [ ] 拖拽到 Section 内两行之间
- [ ] 拖拽到 Section 内最后一行之后

#### 6.4 边界条件测试
- [ ] 空行（只有 placeholder）的边界
- [ ] 只有一个 item 的行的边界
- [ ] 嵌套 Section 内的边界

---

### 步骤 7：优化用户体验

**目标**：确保拖拽到边界时的视觉反馈清晰

**检查点**：
1. **边界高亮**：
   - ✅ `InsertBoundaryMarker` 的 `opacity` 在 hover 时变为 1 (line 502)
   - ✅ `HorizontalRowBoundary` 的 `opacity` 在 hover 时变为 1 (line 562, 579)

2. **拖拽反馈**：
   - ✅ `dropEffect` 设置为 'copy' (line 96, 361, 380, 522, 581)
   - ✅ 边界背景色在 hover 时显示 (line 508, 568)

3. **边界可见性**：
   - ✅ 只在 hover 时显示（`opacity: isHovered ? 1 : 0`）
   - ✅ 使用 `BOUNDARY_THRESHOLD = 4px` 确保精确触发

**可能需要优化**：
- 考虑在拖拽过程中（不仅仅是 hover）也显示边界
- 确保边界按钮（+ 号）在拖拽时也可见

---

## 实现优先级

### 高优先级（必须实现）
1. ✅ **步骤 3**：修复 Section 内行边界的拖拽处理
2. ✅ **步骤 4**：验证 `handleDropToInsertBoundary` 能处理新字段
3. ✅ **步骤 5**：验证 `handleDropToRowBoundary` 能处理新字段

### 中优先级（应该实现）
4. ✅ **步骤 1**：验证当前拖拽数据流
5. ✅ **步骤 2**：确保 BOUNDARY 区域能接收拖拽事件

### 低优先级（优化）
6. ✅ **步骤 6**：全面测试所有边界场景
7. ✅ **步骤 7**：优化用户体验

---

## 潜在问题和解决方案

### 问题 1：Section 内边界没有连接拖拽处理
**位置**：`OneLayoutItemDesign.tsx` line 383-386, 443-446, 470-473
**解决方案**：实现 `onDrop` 处理，调用 `onDropToInsertBoundary`

### 问题 2：insertIndex 计算可能不正确
**位置**：`OneLayoutItemDesign.tsx` SectionRowsContainer
**解决方案**：基于 `FlowOrGridLayoutSortOrder` 正确计算插入位置

### 问题 3：parentRow 传递可能不正确
**位置**：`OneLayoutItemDesign.tsx` SectionRowsContainer
**解决方案**：对于 Section 内的行，parentRow 应该是 Section 本身（`layoutItemExDto`）

### 问题 4：handleAddLayoutItem 可能不支持 insertAtIndex
**位置**：`FormDesign.tsx` handleAddLayoutItem
**解决方案**：检查函数签名，如果不支持，需要添加该参数

---

## 代码修改清单

### 文件 1：`src/components/formMgt/FormDesign/OneLayoutItemDesign.tsx`

**修改位置 1**：`SectionRowsContainer` 组件中的第一个 `HorizontalRowBoundary` (line 369-391)
- 修改 `onDrop` 处理，调用 `onDropToInsertBoundary`

**修改位置 2**：`SectionRowsContainer` 组件中的中间 `HorizontalRowBoundary` (line 429-451)
- 修改 `onDrop` 处理，调用 `onDropToInsertBoundary`
- 计算正确的 `insertIndex`

**修改位置 3**：`SectionRowsContainer` 组件中的最后一个 `HorizontalRowBoundary` (line 456-478)
- 修改 `onDrop` 处理，调用 `onDropToInsertBoundary`
- 计算正确的 `insertIndex`

### 文件 2：`src/components/formMgt/FormDesign.tsx`

**检查位置 1**：`handleAddLayoutItem` 函数签名
- 确认是否支持 `insertAtIndex` 参数
- 如果不支持，添加该参数并实现插入逻辑

**检查位置 2**：`handleDropToInsertBoundary` 函数
- 确认能正确处理新字段拖拽（已有，line 1842-1853）

**检查位置 3**：`handleDropToRowBoundary` 函数
- 确认能正确处理新字段拖拽

---

## 测试计划

### 单元测试
- [ ] 测试 `handleDropToInsertBoundary` 处理新字段
- [ ] 测试 `handleDropToRowBoundary` 处理新字段
- [ ] 测试 `insertIndex` 计算逻辑

### 集成测试
- [ ] 测试从 AddFieldToolbox 拖拽到左右边界
- [ ] 测试从 AddFieldToolbox 拖拽到上下边界（顶层）
- [ ] 测试从 AddFieldToolbox 拖拽到上下边界（Section 内）

### 手动测试
- [ ] 测试所有边界场景
- [ ] 测试边界条件（空行、单 item 行等）
- [ ] 测试嵌套结构

---

## 验收标准

1. ✅ 可以从 `AddFieldToolbox` 拖拽 FIELD ITEM 到任意 BOUNDARY 区域
2. ✅ 字段能正确插入到指定位置
3. ✅ 插入后，其他 item 的 `FlowOrGridLayoutSortOrder` 正确更新
4. ✅ 视觉反馈清晰（边界高亮、拖拽效果）
5. ✅ 不影响现有的拖拽到 PLACEHOLDER 功能
6. ✅ 不影响现有的拖拽移动 layout item 功能

---

## 注意事项

1. **数据兼容性**：确保拖拽数据格式与现有代码兼容
2. **性能**：边界检测使用 `BOUNDARY_THRESHOLD = 4px`，避免过于敏感
3. **事件冒泡**：确保 `stopPropagation` 正确使用，避免事件冲突
4. **状态管理**：确保 `currentDragData` 状态正确清理
5. **边界情况**：处理空行、单 item 行等特殊情况

---

## 后续优化建议

1. **拖拽预览**：在拖拽过程中显示插入位置的预览
2. **键盘快捷键**：支持键盘操作插入
3. **批量操作**：支持批量拖拽多个字段
4. **撤销/重做**：支持拖拽操作的撤销和重做
