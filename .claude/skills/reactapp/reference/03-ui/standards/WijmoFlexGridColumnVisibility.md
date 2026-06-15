# Wijmo FlexGrid – 列可见性控制（Column Visibility）

当需要根据“显示高级列”等开关动态显示/隐藏部分列时，**必须使用 FlexGridColumn 的 `visible` 属性**，不要用条件渲染（如 `{showAdvanced && <FlexGridColumn ... />}`）增减列。

---

## 1. 规则

**用 `visible` 控制列显隐，所有列始终作为 FlexGrid 的子组件渲染。**

- 始终渲染所有列（包括“高级”列）。
- 需要随开关显隐的列：`visible={showAdvancedColumns}`（或对应的 state 变量）。
- 始终显示的列：不设置 `visible`，或 `visible={true}`。

这样与 Angular 中通过 `ng-if="controllerModel.IsShowSearchFieldAdvancedColumns"` 控制列显隐等价：Wijmo 会根据列的 `visible` 更新显示，无需重新挂载网格。

---

## 2. 正确写法

```tsx
const [showAdvancedColumns, setShowAdvancedColumns] = useState(false);

<FlexGrid ref={gridRef} itemsSource={cv} selectionMode="Row">
  {/* 高级列：用 visible 绑定 */}
  <FlexGridColumn binding="Sort" header="Sort" width={50} visible={showAdvancedColumns} />
  <FlexGridColumn binding="FieldName" header="Field Name" width={180} />
  <FlexGridColumn binding="DisplayText" header="Display Name" width={160} />
  {/* 另一列仅在高级模式显示 */}
  <FlexGridColumn binding="EntityId" header="EntityId" width={100} visible={showAdvancedColumns} />
  <FlexGridColumn binding="SubControlType" header="Sub Control Type" width={130} dataMap={subControlTypeDataMap} visible={showAdvancedColumns} />
  {/* 始终显示的列不写 visible */}
  <FlexGridColumn header="" binding="" width="*" allowSorting={false} isReadOnly />
</FlexGrid>
```

---

## 3. 错误写法（勿用）

**不要**通过条件渲染增减列——Wijmo FlexGrid 可能不会在 children 变化时刷新列集合，导致“高级”列不出现：

```tsx
{/* 错误：条件渲染列 */}
{showAdvancedColumns && <FlexGridColumn binding="Sort" header="Sort" width={50} />}
<FlexGridColumn binding="FieldName" header="Field Name" width={180} />
```

**不要**用依赖“显示高级列”的 key 去重挂载整表——会导致列集合被重建，切换时可能看不到高级列或状态错乱：

```tsx
{/* 错误：key 随 showAdvancedColumns 变化导致整表 remount */}
<FlexGrid key={`filter-grid-${showAdvancedColumns}`} itemsSource={cv} ...>
```

**不要**用 Fragment + 条件包住一批“高级列”再作为子节点——等同于条件渲染列，违反“所有列始终作为子组件存在”：

```tsx
{/* 错误：用条件包住多列 */}
<FlexGrid ...>
  <FlexGridColumn binding="FieldName" ... />
  {showAdvancedColumns && (
    <>
      <FlexGridColumn binding="Sort" ... />
      <FlexGridColumn binding="EntityId" ... />
    </>
  )}
</FlexGrid>
```

列显隐**只**用列自带的 `visible`（或 Wijmo 文档中的 `isVisible`，以实际 API 为准），不要用 React 条件渲染或 key 来“换一批列”。

---

## 4. 小结

| 项目     | 说明 |
|----------|------|
| 控制方式 | FlexGridColumn 的 `visible` 属性（布尔或 state 变量） |
| 列定义   | 所有列始终作为 FlexGrid 子组件存在，不随条件挂载/卸载 |
| 参考     | Angular 中对应 `ng-if` 控制列显隐的实现 |

以上为 Reference Rule，供 `.cursor/skills/` 与 `.claude/react-app/reference/` 引用。
