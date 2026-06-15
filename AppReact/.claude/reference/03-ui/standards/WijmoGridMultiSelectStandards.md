# Wijmo FlexGrid 多选与引用规则

本文档定义 React 项目中 **Wijmo FlexGrid** 的以下用法，供 Claude 与 Cursor 技能引用：

- **单行选中 + 另一 Panel 显示选中行**（主从 / Master-Detail）：统一用 hostElement 上监听 `click`，在点击后再读 selection，避免快速点击时错行。
- **多选**（ListBox / MultiRange）、获取选中行、以及双列表转移（dual-list transfer）。

---

## 1. selectionMode 取值

FlexGrid 的 `selectionMode` 决定用户如何选择行：

| 取值 | 说明 | 典型场景 |
|------|------|----------|
| `Row` | 单选一行 | 主从表、详情编辑、单行操作 |
| `ListBox` | 多选多行（点击、Ctrl+点击、Shift+点击） | 双列表「可用 / 已选」、批量勾选 |
| `MultiRange` | 多范围选择 | 批量删除、批量操作 |
| `CellRange` | 单元格范围 | 表格编辑、复制粘贴 |
| `None` | 不可选 | 纯展示 |

**推荐**：
- 需要**多选**且行为类似列表框时，使用 **`selectionMode="ListBox"`**。
- 需要**多范围**（连续/不连续多行）时，使用 **`selectionMode="MultiRange"`**。

```tsx
<FlexGrid
  itemsSource={itemsCV}
  selectionMode="ListBox"
  headersVisibility="Column"
  isReadOnly={true}
  style={{ height: '100%' }}
>
  <FlexGridColumn binding="Display" header="Name" width="*" />
</FlexGrid>
```

---

## 2. 通过 ref 获取网格实例与选中行

### 2.1 ref 指向谁

Wijmo React 的 FlexGrid 的 **ref** 可能指向：
- 底层 **Wijmo 控件实例**（多数情况），或
- React 包装组件，此时控件在 **`ref.current.control`**。

为兼容两种写法，读取网格时建议：

```tsx
const grid = gridRef.current?.control ?? gridRef.current;
```

### 2.2 获取选中行（selectedRows）

多选模式下，选中行在 **`grid.selectedRows`**：

- **类型**：行对象数组（每个元素为 grid 的 row 对象）。
- **行数据**：使用 **`row.dataItem`** 取当前行的数据项。

```tsx
const gridRef = useRef<any>(null);

const handleTransfer = useCallback(() => {
  const grid = gridRef.current?.control ?? gridRef.current;
  if (!grid?.selectedRows?.length) return;

  const ids: number[] = [];
  for (let i = 0; i < grid.selectedRows.length; i++) {
    const row = grid.selectedRows[i];
    const item = row?.dataItem as { Id: number } | undefined;
    if (item?.Id != null) ids.push(item.Id);
  }
  // 使用 ids 更新状态…
}, []);
```

**注意**：
- 先判断 `grid?.selectedRows?.length`，避免无选中时误操作。
- 使用 `row.dataItem` 取业务数据，不要依赖 `row.index` 做业务键（排序/过滤后 index 会变）。

### 2.3 备用：遍历 rows 按 isSelected

当 `selectedRows` 不可用或为空时，可遍历所有行并按 **`row.isSelected`** 取选中行：

```tsx
const selectedDataItems: any[] = [];
for (let i = 0; i < grid.rows.length; i++) {
  const row = grid.rows[i];
  if (row?.isSelected && row.dataItem) {
    selectedDataItems.push(row.dataItem);
  }
}
```

---

## 3. 单行选中 + 另一 Panel 显示选中行（主从 / Master-Detail）

**场景**：左侧（或上方）一个 FlexGrid 单选一行，右侧（或下方）另一个 Panel 根据当前选中行显示详情（如编辑表单、子表等）。快速连续点击不同行时，右侧必须稳定显示**当前点击行**对应的数据。

### 3.1 为何不用 selectionChanged

- `selectionChanged` 触发时，网格的 `selection` 可能尚未更新（尤其树形/层级网格、或点击很快时），用 `grid.selection.row` 或 `grid.rows[sel.row].dataItem` 容易拿到**上一行**，导致右侧始终显示第一行或错行。
- 来源：Angular 端曾用 `flex.cells.hostElement.addEventListener('click', ...)` 在**点击之后**再读 `flex.selection`，保证读到的是本次点击的行。

### 3.2 标准做法：hostElement 上监听 click

**统一采用**：在 FlexGrid **initialized** 回调里，给 **`grid.hostElement`** 绑定 **`click`**，在 click 回调里再读 `grid.selection` 和 `grid.rows[sel.row].dataItem`，用该 dataItem 驱动另一 Panel 的状态（如 `setCurrentItem(rowDataItem)`）。

要点：

1. **initialized 回调**：拿到 Wijmo 的 grid 后，`flex.hostElement.addEventListener('click', handler)`。
2. **click 内读选中行**：`const sel = flex.selection;` → `const rowDataItem = flex.rows[sel.row].dataItem`（或 `(flex.rows[sel.row] as any).item`），再根据 rowDataItem 更新 state（如 setCurrentEditAction）。
3. **用 ref 持有最新数据**：若 handler 里需要依赖「当前 props/state 算出的字典」（如 `dictIdToDto`），应把该字典写入 **ref**（如 `dictRef.current = dictIdToDto`），在 click 里读 `dictRef.current`，避免闭包陈旧。
4. **卸载时移除监听**：在 `useEffect` 的 cleanup 里对保存的 `{ grid, handler }` 做 `grid.hostElement.removeEventListener('click', handler)`，防止重复绑定与泄漏。
5. **树形网格**（`childItemsPath`）同样适用；可选保留 `selectionChanged` 作为补充，但**以 hostElement click 为准**。

### 3.3 示例代码骨架

```tsx
const gridRef = useRef<any>(null);
const dictIdToDtoRef = useRef<Record<number, any>>({});
const gridClickRef = useRef<{ grid: any; handler: () => void } | null>(null);

dictIdToDtoRef.current = dictIdToDto; // 每轮渲染同步，供 click 内使用

const handleGridInitialized = useCallback((grid: any) => {
  const flex = grid?.control ?? grid;
  if (!flex?.hostElement) return;
  const handler = () => {
    const sel = flex.selection;
    if (!sel || sel.row < 0 || !flex.rows?.length) return;
    const rowDataItem = flex.rows[sel.row].dataItem ?? (flex.rows[sel.row] as any)?.item;
    if (!rowDataItem) return;
    const dict = dictIdToDtoRef.current;
    const id = rowDataItem.Id ?? rowDataItem.CommandId;
    if (id == null) return;
    const dto = dict?.[id] ?? dict?.[Number(id)];
    if (dto) setCurrentItem(dto);
  };
  flex.hostElement.addEventListener('click', handler);
  if (gridClickRef.current?.grid?.hostElement && gridClickRef.current?.handler) {
    gridClickRef.current.grid.hostElement.removeEventListener('click', gridClickRef.current.handler);
  }
  gridClickRef.current = { grid: flex, handler };
}, []);

useEffect(() => {
  return () => {
    const r = gridClickRef.current;
    if (r?.grid?.hostElement && r?.handler) {
      r.grid.hostElement.removeEventListener('click', r.handler);
    }
    gridClickRef.current = null;
  };
}, []);

<FlexGrid
  ref={gridRef}
  itemsSource={itemsCV}
  childItemsPath="Children"
  selectionMode="Row"
  initialized={handleGridInitialized}
  style={{ height: '100%' }}
/>
```

### 3.4 参考实现

| 功能 | 文件路径 |
|------|----------|
| 单行选中 + 右侧 Panel 显示当前行详情（含树形 grid + hostElement click） | `src/components/workflow/WorkflowAutomationEditor.tsx` |

---

## 4. 双列表转移（Available / Selected）标准模式

典型场景：左侧「可用项」、右侧「已选项」，中间箭头按钮只负责按**当前网格多选**转移。

### 4.1 结构

- **左侧**：FlexGrid，`itemsSource={availableCV}`，`selectionMode="ListBox"`。
- **中间**：两个按钮 —— 右箭头（→ 加入已选）、左箭头（← 移回可用）。
- **右侧**：FlexGrid，`itemsSource={selectedCV}`，`selectionMode="ListBox"`。

**数据**：
- 用 **`selectedIds`**（如 `number[]`）表示「已选 ID」。
- `availableItems = allItems.filter(x => !selectedIds.includes(x.Id))`。
- `selectedItems = allItems.filter(x => selectedIds.includes(x.Id))`。
- 用 **CollectionView** 包装 `availableItems` / `selectedItems` 再赋给网格的 `itemsSource`。

### 4.2 右箭头（Available → Selected）

- 从**左侧**网格取 **`selectedRows`**，收集 `row.dataItem.Id`。
- 将这批 ID 并入 `selectedIds`（去重），再 `setSelectedIds`。
- **仅**在点击右箭头时转移，不在行点击时转移。

### 4.3 左箭头（Selected → Available）

- 从**右侧**网格取 **`selectedRows`**，收集 `row.dataItem.Id`。
- 从 `selectedIds` 中剔除这批 ID，再 `setSelectedIds`。

### 4.4 示例代码骨架

```tsx
const availableGridRef = useRef<any>(null);
const selectedGridRef = useRef<any>(null);

const availableItems = allItems.filter((u) => !selectedIds.includes(u.Id));
const selectedItems = allItems.filter((u) => selectedIds.includes(u.Id));
const availableCV = useMemo(() => new CollectionView(availableItems), [availableItems]);
const selectedCV = useMemo(() => new CollectionView(selectedItems), [selectedItems]);

const moveToSelected = useCallback(() => {
  const grid = availableGridRef.current?.control ?? availableGridRef.current;
  if (!grid?.selectedRows?.length) return;
  const idsToAdd: number[] = [];
  for (let i = 0; i < grid.selectedRows.length; i++) {
    const item = grid.selectedRows[i]?.dataItem;
    if (item?.Id != null) idsToAdd.push(item.Id);
  }
  setSelectedIds((prev) => {
    const combined = prev.concat(idsToAdd);
    return combined.filter((id, i) => combined.indexOf(id) === i);
  });
}, []);

const moveToAvailable = useCallback(() => {
  const grid = selectedGridRef.current?.control ?? selectedGridRef.current;
  if (!grid?.selectedRows?.length) return;
  const idsToRemove: number[] = [];
  for (let i = 0; i < grid.selectedRows.length; i++) {
    const item = grid.selectedRows[i]?.dataItem;
    if (item?.Id != null) idsToRemove.push(item.Id);
  }
  setSelectedIds((prev) => prev.filter((id) => !idsToRemove.includes(id)));
}, []);

// JSX
<FlexGrid ref={availableGridRef} itemsSource={availableCV} selectionMode="ListBox" ... />
<button onClick={moveToSelected}>→</button>
<button onClick={moveToAvailable}>←</button>
<FlexGrid ref={selectedGridRef} itemsSource={selectedCV} selectionMode="ListBox" ... />
```

---

## 5. 参考实现位置（本仓库）

| 功能 | 文件路径 |
|------|----------|
| **单行选中 + 另一 Panel 显示行详情**（hostElement click） | `src/components/workflow/WorkflowAutomationEditor.tsx` |
| 双列表 + ListBox 多选 + 箭头转移 | `src/components/admin/CompanySecuritySetting/RoleEditorModal.tsx` |
| 双列表 + ListBox（Report 选择） | `src/components/transaction/ApplicationFormBuilder/TransactionReportEditor.tsx` |
| MultiRange 多选 + selectedRows 批量操作 | `src/components/formMgt/FormMasterDetail/MasterDetailFlexLayoutForm/DataGridLayout.tsx` |
| ListBox 多选列选择 | `src/components/transaction/ApplicationFormBuilder/TableColumnSelectorDialog.tsx` |

---

## 6. 小结

- **单行选中 + 另一 Panel 显示**：统一用 **hostElement 上监听 click**，在 click 内再读 `grid.selection` 与 `grid.rows[sel.row].dataItem`，用 ref 持有最新字典，卸载时 removeEventListener。参考：`WorkflowAutomationEditor.tsx`。
- **多选**：`selectionMode="ListBox"` 或 `"MultiRange"`。
- **取选中行**：`grid.selectedRows`，逐行用 `row.dataItem` 取数据；兼容 ref 用 `ref.current?.control ?? ref.current`。
- **双列表转移**：仅通过中间箭头按钮根据**当前网格的 selectedRows** 更新 `selectedIds`，不在行点击时自动转移。
- **数据源**：用 CollectionView 包装 available/selected 数组，再赋给 FlexGrid 的 `itemsSource`。

以上规则可供 `.cursor/skills/` 下的技能或 `.claude/reference/` 下其他 RULE 直接引用。
