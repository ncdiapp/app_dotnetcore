# Wijmo ComboBox Cascading Standards (Angular Reference)

## Overview

When a parent ComboBox (e.g. Table/View Name) drives the **itemsSource** of child ComboBoxes (e.g. Identity Field, Display Field 1–3), the cascade must be wired so that:

1. Changing the parent selection loads new data and updates child DDLs.
2. **First open / initial load** does **not** fire the parent’s change handler and clear or overwrite child values that were bound from the server.

This document summarizes the **Angular pattern** and the **React implementation rules** used in this project.

---

## Angular Reference Pattern

**Source**: `appEntityInfoEditCtrl.js` / `AppEntityInfoEdit.cshtml` (Entity Info Edit).

### 1. Control ref only in `initialized`

- The Table DDL uses `control="controllerModel.flexObject.tableDdlControl"` and `initialized="tableDdlControl_initialized(s,e)"`.
- **Initialized only stores the control reference.** No handler is attached here.

### 2. Attach handler only after binding is complete

- **`initialTableNameChangeEvent()`** runs **after** the current entity is assigned (e.g. after `assignCurrentAppEntity(entityData)`).
- It uses **`$timeout(..., 1000)`** so that:
  - The ComboBox has already received its `selectedValue` (e.g. `ownerTableName`).
  - Any internal Wijmo updates from the initial bind have finished.
- Then it does:
  - `tableDdlControl.selectedIndexChanged.removeAllHandlers()`
  - `tableDdlControl.selectedIndexChanged.addHandler(OwnerTableNameChanged)`
- So **selectedIndexChanged** is attached only **after** the first bind is done, and does not run on that first bind.

### 3. Remove handler before refresh

- **`removeTableNameChangeEvent()`** clears all handlers from `selectedIndexChanged`.
- It is called **before** reassigning entity data (e.g. at the start of `assignCurrentAppEntity` or before reload).
- So during load/refresh, the control’s value may change without firing the cascade and clearing child DDLs.

### 4. Summary flow in Angular

| Step | Action |
|------|--------|
| Init | Store control ref in `initialized`; **do not** attach `selectedIndexChanged`. |
| Load / refresh start | Call `removeTableNameChangeEvent()` (remove all handlers). |
| Load data | Set entity, load table list, set table columns, restore child DDL values (e.g. in `$timeout`). |
| After bind complete | Call `initialTableNameChangeEvent()` (attach `selectedIndexChanged`). |
| User changes parent DDL | `OwnerTableNameChanged` runs → e.g. call GetOneDatabaseTableSchema, update child itemsSource and clear child selections. |

---

## React Implementation Rules

### 1. Use `initialized` only to store the control ref

- In the parent ComboBox (e.g. Table/View Name), use **`initialized={initializeTableDdl}`**.
- **Do not** attach `selectedIndexChanged` inside `initialized`. If you do, the first time the control receives `selectedValue` (e.g. after loadData), Wijmo can fire `selectedIndexChanged` and your handler will run, clearing or overwriting child DDL values that were just bound.

```tsx
const tableDdlRef = useRef<any>(null);

const initializeTableDdl = useCallback((sender: any) => {
  const ctrl = sender?.control ?? sender;
  tableDdlRef.current = ctrl;
  // Do NOT add selectedIndexChanged here
}, []);
```

### 2. Remove handler at the start of load/refresh

- Before loading data (e.g. at the very start of `loadData`), call a **remove** function that clears all handlers from the parent control’s `selectedIndexChanged`.

```tsx
const removeTableDdlChangeHandler = useCallback(() => {
  const ctrl = tableDdlRef.current;
  if (ctrl?.selectedIndexChanged) ctrl.selectedIndexChanged.removeAllHandlers();
}, []);
```

- In `loadData`: call **`removeTableDdlChangeHandler()`** as the first step (before `setIsBusy` / API calls).

### 3. Attach handler only after initial binding is complete

- After load finishes and the UI has had time to apply the new values (entity, parent DDL, child DDLs), attach the **selectedIndexChanged** handler.
- Do this in **loadData’s `finally`** (or equivalent), after state updates, using **`setTimeout(attachTableDdlChangeHandler, 200)`** so React and Wijmo have completed the first bind.

```tsx
const attachTableDdlChangeHandler = useCallback(() => {
  const ctrl = tableDdlRef.current;
  if (ctrl?.selectedIndexChanged) {
    ctrl.selectedIndexChanged.removeAllHandlers();
    ctrl.selectedIndexChanged.addHandler((s: any) => {
      const val = s?.selectedValue;
      if (val != null && val !== '') onOwnerTableNameChangeRef.current(val);
    });
  }
}, []);

// In loadData:
// 1. removeTableDdlChangeHandler();
// 2. ... load and set entity, table list, table columns, child DDL values ...
// 3. finally { setIsLoading(false); ...; setTimeout(attachTableDdlChangeHandler, 200); }
```

### 4. Use a ref for the cascade callback

- The handler is attached once but must always call the latest logic (e.g. that reads current `tableList`, `currentEntity.DataSourceFrom`, and calls `getOneDatabaseTableSchema`). Keep the callback in a **ref** and assign it on each render.

```tsx
const onOwnerTableNameChangeRef = useRef<(ownerTableName: string) => void>(() => {});
// In render (after onOwnerTableNameChange is defined):
onOwnerTableNameChangeRef.current = onOwnerTableNameChange;
```

- In `attachTableDdlChangeHandler`, call **`onOwnerTableNameChangeRef.current(val)`** so the handler never holds a stale closure.

### 5. Child DDLs when parent’s itemsSource changes

- When the parent selection changes, update the **itemsSource** for child ComboBoxes (e.g. set new table columns) and clear their selected values as needed.
- If Wijmo does not refresh the child ComboBox list when **itemsSource** is replaced (e.g. new `CollectionView`), give each child ComboBox a **`key`** that changes when the parent or source data changes (e.g. `key={\`identity-${ownerTableName}-${tableColumns.length}\`}`) so React remounts them and they bind to the new list.
- For **multiple child ComboBoxes** sharing the same list, use **separate CollectionView instances** per ComboBox (same source array) so Wijmo does not share a single `currentItem` and show the same row in every dropdown.

---

## Notes and Cautions

1. **Do not attach `selectedIndexChanged` in `initialized`**  
   If you attach it there, the first time the control gets its initial `selectedValue` (e.g. after loadData), Wijmo may fire the event and your handler will run, clearing or overwriting child DDL values that were just bound from the server.

2. **Always remove before refresh**  
   On any load/refresh that reassigns the parent or child data, call the remove handler first so that programmatic value changes during load do not trigger the cascade.

3. **Attach only after bind**  
   Use a deferred attach (e.g. `setTimeout(attach, 200)` in load’s `finally`) so that the first paint with the new entity and DDL values has completed before the user can trigger the cascade by changing the parent.

4. **React prop `onSelectedValueChanged`**  
   In this project, the Wijmo React ComboBox’s `onSelectedValueChanged` is not relied on for this cascade; it may not fire consistently when the control is bound. Use the **control ref + `selectedIndexChanged.addHandler`** pattern (as in Angular) and attach only after initial binding.

5. **Child list refresh**  
   When the parent changes, clear the child list (e.g. `setTableColumns([])`) then set the new list after the API returns, so the UI clearly switches to the new options. Optionally re-apply empty selection in `setTimeout(..., 0)` to satisfy the [Wijmo ComboBox Selection Rule](../../02-migration/ConverterAngularJsPage.md) (empty first, then value).

---

## Reference Implementation

- **Full example**: `src/components/admin/EntityListOfValue/AppEntityInfoEdit.tsx`  
  - Table/View Name ComboBox: `initialized={initializeTableDdl}` only stores ref.  
  - `loadData`: starts with `removeTableDdlChangeHandler()`; in `finally` calls `setTimeout(attachTableDdlChangeHandler, 200)`.  
  - Parent change handler loads schema and updates child column DDLs; child ComboBoxes use `key={columnDdlKey}` to remount when columns change.

- **Related rules**:  
  - `../../02-migration/ConverterAngularJsPage.md` — Wijmo ComboBox Selection Rule (empty then setTimeout), FlexGrid control access.  
  - Same folder: `WijmoCollectionViewStandards.md`, `FormStandards.md` for DDL styling and layout.
