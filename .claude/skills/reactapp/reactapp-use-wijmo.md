# reactapp-use-wijmo

Use when using **Wijmo** controls (FlexGrid, ComboBox, etc.) in the React app. Ensures correct data binding, refs, and patches.

**Full reference**: `.claude/react-app/reference/03-ui/standards/` — WijmoCollectionViewStandards, WijmoGridMultiSelectStandards, WijmoGridNullRefFixes, WijmoFlexGridContainerStandards, WijmoFlexGridColumnVisibility, WijmoComboBoxCascadingStandards.

## Rules (summary)

- **CollectionView / itemsSource**: Initialize state with `useState<any>([])` or `useState(() => new CollectionView<any>([]))`; never pass `null` to `itemsSource`. On load error set back to `[]` or empty CollectionView.
- **FlexGridCellTemplate**: Use `cell.item`, **not** `cell.dataItem`.
- **FlexGrid ref**: Access grid via `flexGridRef.current.control`; for row selection in `selectionChanged` use `const flex = s?.control ?? s`, then `flex.selection?.row`, `flex.rows[rowIndex]?.dataItem` (do not rely on `collectionView.currentItem`).
- **Spacer column**: Add a last column: `<FlexGridColumn header="" binding="" width="*" allowSorting={false} isReadOnly={true} />`.
- **FlexGrid fill**: Parent should allow flex growth; add `className="w-full h-full"` on `<FlexGrid>` so it fills the container.
- **ComboBox selection**: When setting selected value programmatically, set to `''` first then `setTimeout(() => { comboBox.selectedValue = value; }, 0)`.
- **Null ref fixes**: Use patches in `src/wijmoPatches.ts` for editRange/finishEditing if needed; see WijmoGridNullRefFixes.md.

See the Wijmo* standards in 03-ui/standards/ for multi-select, column visibility, and cascading ComboBox.
