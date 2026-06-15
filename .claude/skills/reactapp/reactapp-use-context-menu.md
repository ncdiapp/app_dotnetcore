# reactapp-use-context-menu

Use when implementing or aligning **context menu** on list/grid management pages (e.g. datasets, REST API import). Standard: first column = "Actions" with one button per row that opens a **floating context menu**; no Edit/Delete button column on the right.

**Full standard**: `.claude/react-app/reference/03-ui/standards/ContextMenuStandards.md`

**Reference components**:
- `PlmApplication/AppReact/src/components/admin/MyApplicationEditor/DataModelDesign.tsx` (default for management grids)
- `PlmApplication/AppReact/src/components/dbmgt/RestApiImportManagement.tsx`
- `PlmApplication/AppReact/src/components/admin/UserManagement.tsx` (user list style)

## Rules (summary)

1. **First column**: Header `"Actions"`, one button per row: `${theme.menu_default} w-8 h-6`, icons `fa-solid fa-pencil` + `fa-solid fa-bars`, `title="More Options"`. On click: `getBoundingClientRect()`, set menu state `{ visible: true, x: rect.right, y: rect.top, item: rowData }`.
2. **Floating menu**: `fixed z-50`, `${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-max`. Items: `px-4 py-2 text-xs ${theme.contextMenu}`. Icons: Font Awesome 6 `fa-solid` only (e.g. `fa-solid fa-pen-to-square`, `fa-solid fa-trash`).
3. No button column on the right; close menu on outside click.

Use theme from `useTheme()`; see ContextMenuStandards.md for full snippets.
