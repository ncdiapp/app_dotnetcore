---
name: context-menu
description: Apply the standard grid/list CONTEXT MENU pattern. Use when adding or aligning an action column and floating context menu in management list pages (e.g. DatasetManagement, RestApiImportManagement, DataModelDesign).
---

# Context Menu Skill (Grid / List)

Unify the CONTEXT MENU pattern for list/grid pages: **first column = Action column** (single icon button that opens a floating menu), **no button column on the right**.

**Reference implementation**: `src/components/admin/MyApplicationEditor/DataModelDesign.tsx` (Data Model Design page).

## Standard Pattern

### 1. Action column (first column of the grid)

**Reference**: `DataModelDesign.tsx` — context menu column (transactionMenuCellTemplate). Use the **exact same icons**: pencil + bars.

- **Position**: First column of the FlexGrid.
- **Header**: `header="Actions"` (text only; no gear or other icon in header).
- **Width**: `width={60}`.
- **Cell content**: One button per row that opens the context menu.

**Action button (trigger)** — must match DataModelDesign:

- Class: `${theme.menu_default} w-8 h-6 flex items-center justify-center`
- `title="More Options"`.
- **Icons (required — do not use a single edit/pen or gear icon)**:
  - First: `<i className="fa-solid fa-pencil text-xs" aria-hidden />`
  - Second: `<i className="fa-solid fa-bars text-[9px] relative -left-1 top-0.5" aria-hidden />`
- **Click handler**: `e.stopPropagation()`; get position with `e.currentTarget.getBoundingClientRect()`; set context menu state with `{ visible: true, x: rect.right, y: rect.top, item: rowData }`.

**Wijmo usage**:

```tsx
{/* Same as DataModelDesign.tsx transactionMenuCellTemplate — pencil + bars only */}
<FlexGridColumn width={60} header="Actions" isReadOnly>
  <FlexGridCellTemplate
    cellType="Cell"
    template={(cell: any) => (
      <div className="flex items-center justify-center w-full">
        <button
          type="button"
          className={`${theme.menu_default} w-8 h-6 flex items-center justify-center`}
          title="More Options"
          onClick={(e) => {
            e.stopPropagation();
            const rect = e.currentTarget.getBoundingClientRect();
            setContextMenu({ visible: true, x: rect.right, y: rect.top, item: cell.item });
          }}
        >
          <i className="fa-solid fa-pencil text-xs" aria-hidden />
          <i className="fa-solid fa-bars text-[9px] relative -left-1 top-0.5" aria-hidden />
        </button>
      </div>
    )}
  />
</FlexGridColumn>
```

### 2. Floating context menu popup

- **Container**: `fixed z-50`, theme and border.
- **Classes**: `${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-max`
- **Position**: `style={{ left: contextMenu.x, top: contextMenu.y }}`
- **Click**: `onClick={(e) => e.stopPropagation()}` so outside clicks close via document listener.

**Menu item (each action)**:

- Class: `w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`
- **Icons**: Must use **Font Awesome 6 with `fa-solid` prefix** only (no bare `fa fa-*`). Standard mapping:
  - **Edit**: `fa-solid fa-pen-to-square` (or `fa-solid fa-pencil` for pencil-only)
  - **Delete**: `fa-solid fa-trash`
  - **Copy / Save As**: `fa-solid fa-copy`
  - **Open / View**: `fa-solid fa-eye` or `fa-solid fa-folder-open`
  - **Extract View / Table**: `fa-solid fa-table-columns`
  - **Add to menu**: `fa-solid fa-bars`
- Icon element: add `mr-2 flex-shrink-0` (or use `gap-2` on the button) and `aria-hidden`.

**Divider between groups** (optional):

- `<div className={`h-px my-1 border-t ${t('border_default')}`} />` or use `theme.menu_divider` if available.

**Example popup**:

```tsx
{contextMenu.visible && contextMenu.item && (
  <div
    className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-max`}
    style={{ left: contextMenu.x, top: contextMenu.y }}
    onClick={(e) => e.stopPropagation()}
  >
    <button
      type="button"
      className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
      onClick={() => { handleEdit(contextMenu.item); setContextMenu({ visible: false, item: null }); }}
    >
      <i className="fa-solid fa-edit mr-2 flex-shrink-0" aria-hidden /> Edit
    </button>
    <button
      type="button"
      className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
      onClick={() => { handleDelete(contextMenu.item); setContextMenu({ visible: false, item: null }); }}
    >
      <i className="fa-solid fa-trash mr-2 flex-shrink-0" aria-hidden /> Delete
    </button>
  </div>
)}
```

### 3. No button column on the right

- Do **not** add a column at the end of the grid with per-row Edit/Delete/More buttons.
- All row actions are accessed via the **first-column action icon** and the floating context menu.

### 4. Closing the menu

- Add a document `click` listener when the menu is visible to set `contextMenu.visible = false` (and optionally clear `item`). Exclude clicks inside the menu container and the action button if needed (e.g. by ref or `stopPropagation` on the trigger).

## Checklist

- [ ] First column is "Actions" (header text only, no gear icon) with one button per row.
- [ ] Action trigger uses **exactly** the DataModelDesign icons: `fa-solid fa-pencil text-xs` and `fa-solid fa-bars text-[9px] relative -left-1 top-0.5` (pencil + bars; do not use a single icon such as fa-pen-to-square or fa-gear).
- [ ] Button uses `theme.menu_default`, `w-8 h-6`, and opens menu at `rect.right`/`rect.top`.
- [ ] Popup uses `fixed z-50`, `theme.mainContentSection`, `rounded-[4px]`, `shadow-lg`, `py-1 min-w-max`.
- [ ] Each menu item uses `px-4 py-2 text-xs`, `theme.contextMenu`, **icon with `fa-solid` prefix** + text (Edit = fa-pen-to-square, Delete = fa-trash, etc.).
- [ ] No extra action/button column on the right side of the grid.
- [ ] Menu closes on outside click (document listener).

## Input

Use when the user asks to add or align a context menu on a list/grid page, or to "follow Data Model Design / RestApiImportManagement context menu style".
