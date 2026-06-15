# reactapp-fix-ui

Use when **fixing** existing React components to comply with project UI standards. Applies all UI rules: theme, Tailwind, forms, and Wijmo.

**Related skills**: `.claude/skills/reactapp/reactapp-use-theme.md`, `.claude/skills/reactapp/reactapp-use-wijmo.md`. **Full reference**: `.claude/react-app/reference/03-ui/UIMainPrompt.md`, `.claude/react-app/reference/03-ui/standards/` (PageLayoutStandards, ButtonStandards, FormStandards, ThemeUsageStandards, TailwindCSSStandards, ReferenceComponents), `.claude/react-app/reference/03-ui/layout/TailwindFlexBoxRemainSpace.md`.

## Standards to check

- **Page layout**: Header toolbar `flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`; main content `w-full h-1 flex-auto overflow-auto px-5 py-5` for scrollable area.
- **Theme**: No hardcoded colors; use `theme.*` from `useTheme()` only.
- **Buttons**: `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`; icon button `w-8 h-6 ${theme.button_default} rounded-[4px] text-xs` with `title`.
- **Forms**: Label `w-32 text-xs ${theme.label} mr-2`; input/select `h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`; `autoComplete="off"` on text inputs; row wrapper `flex items-center py-1`.
- **Flexbox**: Never `flex-1` — use `w-1 flex-auto` or `h-1 flex-auto`.
- **Icons**: Font Awesome 6 `fa-solid` prefix (e.g. `fa-solid fa-rotate`, `fa-solid fa-floppy-disk`).
- **Spacing**: Header `px-3 py-2`; form content `px-5 py-5`.
- **Wijmo**: itemsSource never null; `cell.item` not `cell.dataItem`; spacer column; ref via `.control`. See `.claude/skills/reactapp/reactapp-use-wijmo.md`.

## Workflow

1. Read the component.
2. Identify violations against the checklist above.
3. Fix each issue (reference `.claude/react-app/reference/03-ui/standards/ReferenceComponents.md` for examples).
4. Verify no new errors.

Argument: React component path to fix.
