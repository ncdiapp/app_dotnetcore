# Context Menu Standards (Grid / List)

## Overview

This document is the **single source of truth** for the CONTEXT MENU pattern on list/grid management pages. When using SKILL to **modify** or **generate** pages that have a grid with row actions, apply this standard so that:

- **First column** = one action button per row that opens a **floating context menu** (no per-row key/pencil/trash buttons).
- **No button column on the right** — all row actions are via the context menu.

**Reference implementations**:

| Style | Component | Path | Use when |
|-------|-----------|------|----------|
| **A. DataModelDesign** | Data Model Design | `src/components/admin/MyApplicationEditor/DataModelDesign.tsx` | General management grids (transactions, datasets, REST API import, etc.) |
| **B. UserManagement** | User Management | `src/components/admin/UserManagement.tsx` | User list pages (e.g. Company Security → Users, user management) |

Use **Style A** by default for new grid pages. Use **Style B** when the page is a **user list** (login/user CRUD) or when explicitly aligning with UserManagement.

---

## Style A: DataModelDesign (default for management grids)

### 1. Action column

- **Position**: First column of the FlexGrid.
- **Header**: `header="Actions"` (text only; no gear icon).
- **Width**: `width={60}`. Optional: `allowSorting={false}` or `isReadOnly`.
- **Trigger**: One button per row — **pencil + bars** (two icons). Do **not** use a single icon (e.g. fa-gear, fa-ellipsis).

| Attribute | Value |
|-----------|--------|
| Container | `flex items-center justify-center w-full` |
| Button | `${theme.menu_default} w-8 h-6 flex items-center justify-center` |
| Title | `title="More Options"` |
| Icon 1 | `<i className="fa-solid fa-pencil text-xs" aria-hidden />` |
| Icon 2 | `<i className="fa-solid fa-bars text-[9px] relative -left-1 top-0.5" aria-hidden />` |

**Click**: `e.stopPropagation()`; `const rect = e.currentTarget.getBoundingClientRect();`; set state `{ visible: true, x: rect.right, y: rect.top, item: cell.item }`.

```tsx
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

### 2. Floating menu (Style A)

- **Container**: `fixed z-50`, `${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-max`.
- **Position**: `style={{ left: contextMenu.x, top: contextMenu.y }}`.
- **Click**: `onClick={(e) => e.stopPropagation()}` on container.
- **Menu items**: `w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`. Icons: **fa-solid** prefix only (e.g. `fa-solid fa-pen-to-square`, `fa-solid fa-trash`), `mr-2 flex-shrink-0`, `aria-hidden`.

---

## Style B: UserManagement (user list pages)

Use for **user list** grids (e.g. Company Security Setting → Users, User Management page).

### 1. Action column (Style B)

- **Header**: `header=""` (empty).
- **Width**: `width={100}`, `allowSorting={false}`.
- **Trigger**: One button per row — **pencil + navicon** (same idea, different icon set). Button width `30px`.

| Attribute | Value |
|-----------|--------|
| Button | `${theme.menu_default}`, `style={{ width: '30px' }}` |
| Title | `title="More Options"` |
| Icon 1 | `<i className="fa fa-pencil" aria-hidden style={{ fontSize: '12px' }} />` |
| Icon 2 | `<i className="fa fa-navicon" aria-hidden style={{ position: 'relative', left: '-1px', top: '2px', fontSize: '9px' }} />` |

**Click**: `e.preventDefault(); e.stopPropagation();`; set state using **click position**: `{ x: e.clientX, y: e.clientY, item }` (menu opens at cursor).

```tsx
<FlexGridColumn header="" width={100} allowSorting={false}>
  <FlexGridCellTemplate
    cellType="Cell"
    template={(cell: any) => {
      const item = cell.item;
      if (!item) return null;
      return (
        <button
          type="button"
          className={`${theme.menu_default}`}
          title="More Options"
          style={{ width: '30px' }}
          onClick={(e) => {
            e.preventDefault();
            e.stopPropagation();
            setContextMenu({ x: e.clientX, y: e.clientY, user: item });
          }}
        >
          <i className="fa fa-pencil" aria-hidden style={{ fontSize: '12px' }} />
          <i className="fa fa-navicon" aria-hidden style={{ position: 'relative', left: '-1px', top: '2px', fontSize: '9px' }} />
        </button>
      );
    }}
  />
</FlexGridColumn>
```

### 2. Floating menu (Style B)

- **Container**: Same as Style A but **min-width**: `min-w-[150px]` (not `min-w-max`).
- **Position**: `style={{ left: contextMenu.x, top: contextMenu.y }}` (from `e.clientX` / `e.clientY`).
- **Menu items**: Same classes: `w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`. Icons: `fa fa-edit mr-2`, `fa fa-trash mr-2` (UserManagement uses `fa fa-*`; project prefers `fa-solid` where consistent).
- **Typical labels for user list**: "Edit Login Info", "Edit User Details", "Delete User".

```tsx
{contextMenu?.user && (
  <div
    ref={contextMenuRef}
    className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[150px]`}
    style={{ left: contextMenu.x, top: contextMenu.y }}
    onClick={(e) => e.stopPropagation()}
  >
    <button type="button" className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`} onClick={...}>
      <i className="fa fa-edit mr-2" aria-hidden /> Edit Login Info
    </button>
    <button type="button" className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`} onClick={...}>
      <i className="fa fa-edit mr-2" aria-hidden /> Edit User Details
    </button>
    <button type="button" className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`} onClick={...}>
      <i className="fa fa-trash mr-2" aria-hidden /> Delete User
    </button>
  </div>
)}
```

---

## Common rules (both styles)

### No button column on the right

- Do **not** add a column with per-row Edit / Delete / Key / More buttons.
- All row actions go through the **first-column action button** and the **floating context menu**.

### Closing the menu

- Add a **document** `click` (or `mousedown`) listener when the menu is visible; set context menu to closed. Exclude the menu container (e.g. with a `ref` and `!ref.current.contains(e.target)`).

### State shape

- At least: `{ x: number; y: number; item: any }` or `{ visible: boolean; x: number; y: number; item: any }`. Use a `ref` on the popup for outside-click detection.

---

## Reference components

| Component | Path | Style |
|-----------|------|--------|
| Data Model Design | `src/components/admin/MyApplicationEditor/DataModelDesign.tsx` | A |
| User Management | `src/components/admin/UserManagement.tsx` | B |
| Company Security → Users (CompanyUsersTab) | `src/components/admin/CompanySecuritySetting/CompanyUsersTab.tsx` | B |
| CompanyOrgnizationSetup | `src/components/admin/CompanySecuritySetting/CompanyOrgnizationSetup.tsx` | B (when controlled) |
| REST API Import Management | `src/components/dbmgt/RestApiImportManagement.tsx` | A |
| Dataset Management | `src/components/dbmgt/DatasetManagement.tsx` | A |

---

## Summary checklist (for SKILL / code gen)

- [ ] First column is the **only** action column; one button per row that opens a floating menu.
- [ ] **No** per-row key/pencil/trash (or similar) buttons in the grid.
- [ ] **Style A**: header `"Actions"`, width 60, button `theme.menu_default` + `w-8 h-6`, icons `fa-solid fa-pencil` + `fa-solid fa-bars`, menu at `rect.right`/`rect.top`.
- [ ] **Style B** (user lists): header `""`, width 100, button width 30px, icons `fa fa-pencil` + `fa fa-navicon`, menu at `e.clientX`/`e.clientY`, popup `min-w-[150px]`.
- [ ] Popup: `fixed z-50`, `theme.mainContentSection`, `border rounded-[4px] shadow-lg py-1`, menu items `px-4 py-2 text-xs ${theme.contextMenu}`.
- [ ] Menu closes on outside click (document listener; exclude popup by ref).
