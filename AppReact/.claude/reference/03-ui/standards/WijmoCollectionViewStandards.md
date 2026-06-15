# Wijmo CollectionView – React State Standards

## Default initial value for grid `itemsSource` (CollectionView state)

When a Wijmo **FlexGrid** (or other control) uses **itemsSource** bound to state that is later set to a **CollectionView**, initialize that state with a **safe default** so the grid never receives `null` or `undefined`. Passing `null`/`undefined` to `itemsSource` can cause runtime errors.

### Rule: Initialize with empty array

Use **empty array** as the initial state value:

```tsx
const [gridCV, setGridCV] = useState<any>([]);
```

- **Type**: `useState<any>([])` is acceptable so the same state can hold either `[]` (before load) or a `CollectionView` instance (after load).
- **Why `[]`**: FlexGrid accepts an array for `itemsSource`; an empty array renders an empty grid without errors.
- **Avoid**: Do not use `useState<CollectionView | null>(null)` or `useState(undefined)` for grid itemsSource state, unless the grid is not rendered when the value is null (e.g. conditional render). Prefer always passing a valid value.

### On load success

Set the real CollectionView:

```tsx
const cv = new CollectionView(safeList);
cv.sortDescriptions.push(new SortDescription('AppModifiedDate', false));
setGridCV(cv);
```

### On load error

Set back to empty array, **not** `null`:

```tsx
catch (error) {
  showError(error?.message || 'Failed to load');
  setGridCV([]);   // safe for itemsSource; do not use setGridCV(null)
}
```

### Summary

| Scenario        | Value to set   |
|----------------|----------------|
| Initial state  | `[]`           |
| Load success   | `new CollectionView(list)` |
| Load error     | `[]`           |

This pattern avoids "previous" errors from the grid receiving `null`/`undefined` on first render or after a failed load.
