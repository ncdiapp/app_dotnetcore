# Wijmo FlexGrid – 填满容器（Reference Rule）

FlexGrid 一般情况下需要填满外边 CONTAINER DIV，否则在 flex 或固定高度容器内可能无法正确撑满，导致留白或滚动异常。

---

## 1. 规则

**FlexGrid 必须显式设置 className 以填满父容器。**

在 `<FlexGrid>` 上添加 `className="w-full h-full"`（或 `className="h-full w-full"`），使网格在宽度和高度上都填满外层容器。

```tsx
<div className="min-h-0 h-1 flex-auto overflow-hidden">
  <FlexGrid
    className="w-full h-full"
    itemsSource={gridCV}
    selectionMode="Row"
    isReadOnly
  >
    <FlexGridFilter />
    <FlexGridColumn ... />
  </FlexGrid>
</div>
```

---

## 2. 说明

| 项目 | 说明 |
|------|------|
| 目的 | 让 FlexGrid 在 flex / 固定高度容器内正确占满可用空间 |
| 写法 | `className="w-full h-full"` 或 `className="h-full w-full"` |
| 父容器 | 父 div 需有明确高度（如 `h-1 flex-auto`、`h-[240px]`、`min-h-0 h-1 flex-auto` 等），否则 `h-full` 无效 |

---

## 3. 小结

- 新增或修改 FlexGrid 时，**务必**为 `<FlexGrid>` 加上填满容器的 className（`w-full h-full`）。
- 未加时，网格可能不随容器伸缩，出现空白或布局错误。

以上为 Reference Rule，供 `.cursor/skills/` 与 `.claude/reference/` 引用。
