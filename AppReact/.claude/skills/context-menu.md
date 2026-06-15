# Context Menu Skill (Grid / List)

Use this skill when adding or aligning the **CONTEXT MENU** pattern on list/grid management pages (e.g. DatasetManagement, RestApiImportManagement). The standard is taken from the **Data Model Design** page.

**Full standard**: `.claude/reference/03-ui/standards/ContextMenuStandards.md`

**Reference components**:
- `src/components/admin/MyApplicationEditor/DataModelDesign.tsx` (primary)
- `src/components/dbmgt/RestApiImportManagement.tsx`

## When to Use

- Implementing or refactoring a management list/grid that has row actions (Edit, Delete, etc.).
- User asks to "follow Data Model Design context menu" or "统一 CONTEXT MENU 标准".

## Rules (Summary)

1. **First column = "Actions"** (match DataModelDesign.tsx context menu column)
   - Header: text `"Actions"` only (no gear icon). One button per row: `${theme.menu_default} w-8 h-6`, `title="More Options"`.
   - **Icons (required)**: `fa-solid fa-pencil text-xs` and `fa-solid fa-bars text-[9px] relative -left-1 top-0.5` (pencil + bars; do not use single icon like fa-pen-to-square or fa-gear).
   - Click: `getBoundingClientRect()`, set menu state `{ visible: true, x: rect.right, y: rect.top, item: rowData }`.

2. **Floating menu**
   - Container: `fixed z-50`, `${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-max`, position from state.
   - Each item: `w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`, icon `mr-2 flex-shrink-0`.
   - **Icons**: Use **Font Awesome 6 with `fa-solid` prefix only** (no `fa fa-*`). Edit = `fa-solid fa-pen-to-square`, Delete = `fa-solid fa-trash`, Copy = `fa-solid fa-copy`, View = `fa-solid fa-eye`, Extract View = `fa-solid fa-table-columns`.

3. **No button column on the right**
   - Do not add a column at the end with Edit/Delete/More buttons; all actions go through the context menu.

4. **Close on outside click**
   - Document click listener when menu is visible to set `visible: false`.

For full code snippets and checklist, read `.claude/reference/03-ui/standards/ContextMenuStandards.md`.
