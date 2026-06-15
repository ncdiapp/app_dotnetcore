# reactapp-create-component

Use when **creating** new React components or pages. Applies all UI rules: theme, Tailwind layout, forms, and Wijmo when used.

**Related skills**: `.claude/skills/reactapp/reactapp-use-theme.md`, `.claude/skills/reactapp/reactapp-use-wijmo.md` (if using grids/ComboBox). **Full reference**: `.claude/react-app/reference/ReactAppReferenceIndex.md`, `.claude/react-app/reference/03-ui/UIMainPrompt.md`, `.claude/react-app/reference/03-ui/standards/` (PageLayoutStandards, ButtonStandards, FormStandards, ThemeUsageStandards, TailwindCSSStandards, ReferenceComponents), `.claude/react-app/reference/03-ui/layout/TailwindFlexBoxRemainSpace.md`.

## Theme

- Use `useTheme()`; apply `theme.mainContentSection`, `theme.button_default`, `theme.inputBox`, `theme.label`, `theme.title`. Never hardcode colors (no `bg-blue-400`). See `.claude/skills/reactapp/reactapp-use-theme.md`.

## Tailwind / layout

- **Never use `flex-1`**. Use `w-1 flex-auto` (horizontal) or `h-1 flex-auto` (vertical).
- **Page**: Root `w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden`; header `flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`; main content `w-full h-1 flex-auto overflow-hidden` or `overflow-auto` for scrollable; spacing header `px-3 py-2`, form area `px-5 py-5`.
- **Scrollable content**: `w-full h-1 flex-auto overflow-auto px-5 py-5`.

## Forms

- **Form row**: `flex items-center py-1`. **Label**: `w-32 text-xs ${theme.label} mr-2`. **Input**: `flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`; `autoComplete="off"` on text inputs.
- **Buttons**: `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`; icon button `w-8 h-6 ${theme.button_default} rounded-[4px] text-xs` with `title`.
- **Icons**: Font Awesome 6 `fa-solid` only (e.g. `fa-solid fa-floppy-disk`, `fa-solid fa-rotate`).

## Wijmo (when used)

- CollectionView/itemsSource: never `null`; init with `[]` or `new CollectionView([])`; on error set back to `[]`.
- FlexGridCellTemplate: use `cell.item`, not `cell.dataItem`. Ref: `flexGridRef.current.control`. Add spacer column `width="*"`. ComboBox: set `selectedValue = ''` then `setTimeout(..., 0)` when setting programmatically. See `.claude/skills/reactapp/reactapp-use-wijmo.md`.

## Conventions

- Debug: `appHelper.debugLog()`, not `console.log()`. Enums: `useEnumValues('EmAppEnumName')`. JSON in UI: `JSON.stringify(value, null, 2)`. Imports: no `.tsx`/`.ts` extension. See `.claude/react-app/reference/ReactAppConventions.md`.

Reference components: UserLoginInfoEditor, UserManagement, FormMasterDetail, DataModelDesign (see ReferenceComponents.md).
