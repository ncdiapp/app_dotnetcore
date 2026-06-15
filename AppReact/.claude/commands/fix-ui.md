Fix a React component's UI to comply with project standards.

**Read**: `.claude/skills/angularjs-to-react-migration.md` for UI standards. Full UI docs: `.claude/reference/03-ui/UIMainPrompt.md`, `.claude/reference/03-ui/standards/` (PageLayoutStandards, ButtonStandards, FormStandards, ThemeUsageStandards, TailwindCSSStandards, ReferenceComponents), `.claude/reference/03-ui/layout/TailwindFlexBoxRemainSpace.md`. Scenario: `.claude/reference/00-scenarios/ScenarioTemplates.md` (Scenario 4).

**Standards to Check:**
- Page layout: Header toolbar (`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`) + main content; scrollable content: `w-full h-1 flex-auto overflow-auto px-5 py-5`
- Theme usage: No hardcoded colors (no `bg-blue-400`, etc.); use `theme.*` only
- Buttons: Standard `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`; icon `w-8 h-6 ${theme.button_default} rounded-[4px] text-xs` with `title`
- Forms: Labels `w-32 text-xs ${theme.label} mr-2`; inputs/select `h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`; `autoComplete="off"` on text inputs; textarea same theme; row wrapper `flex items-center py-1`
- Flexbox: Never `flex-1` — use `w-1 flex-auto` or `h-1 flex-auto`; main content scrollable: `w-full h-1 flex-auto overflow-auto`
- Icons: Font Awesome 6 `fa-solid` prefix (e.g. `fa-solid fa-rotate`, `fa-solid fa-floppy-disk`); no bare `fa fa-*`
- Spacing: Header `px-3 py-2`; form content area `px-5 py-5`

**Workflow:**
1. Read the component
2. Identify UI standard violations against the checklist above
3. Fix each issue (reference `.claude/reference/03-ui/standards/ReferenceComponents.md` for examples)
4. Verify no new errors introduced

Argument: Provide the React component path to fix.
