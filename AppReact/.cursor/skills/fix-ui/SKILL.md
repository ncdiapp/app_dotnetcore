---
name: fix-ui
description: Fix a React component's UI to comply with project standards. Use when the user asks to fix UI standards, align a component with theme/layout rules, or correct flex/button/form styling.
---

# Fix UI Skill

Fix a React component's UI to comply with project standards.

**Read** the angularjs-to-react-migration skill (`.cursor/skills/angularjs-to-react-migration/SKILL.md`) for UI standards. Full UI docs: `.claude/reference/03-ui/UIMainPrompt.md`, `.claude/reference/03-ui/standards/` (PageLayoutStandards, ButtonStandards, FormStandards, ThemeUsageStandards, TailwindCSSStandards, ReferenceComponents, **ContextMenuStandards**, **WijmoFlexGridColumnVisibility**), `.claude/reference/03-ui/layout/TailwindFlexBoxRemainSpace.md`. For grid/list context menu pattern use **context-menu** skill (`.cursor/skills/context-menu/SKILL.md`). Scenario: `.claude/reference/00-scenarios/ScenarioTemplates.md` (Scenario 4).

## Standards to Check

- **Page layout**: Header toolbar (`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`) + main content area below
- **Theme usage**: No hardcoded colors (no `bg-blue-400`, `bg-green-500`, etc.); use `theme.*` tokens only
- **Buttons**: Standard = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`; small icon = `w-8 h-6 ${theme.button_default} rounded-[4px] text-xs` with `title` for tooltip
- **Forms**:
  - Labels: `w-32 text-xs ${theme.label} mr-2` (include `mr-2` for horizontal layout)
  - Inputs / select: `h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`; add `autoComplete="off"` on text inputs
  - Textarea: same border/theme via `theme.inputBox`, `text-xs`, `focus:outline-none`
  - Form row wrapper: `flex items-center py-1` per row
- **Flexbox**: Never use `flex-1`. Use `w-1 flex-auto` (horizontal) or `h-1 flex-auto` (vertical). Main content area: `w-full h-1 flex-auto overflow-auto` when scrollable.
- **Icons**: Font Awesome 6 with `fa-solid` prefix (e.g. `fa-solid fa-rotate`, `fa-solid fa-floppy-disk`); no bare `fa fa-*`
- **Spacing**: `px-3 py-2` for header toolbar; `px-5 py-5` for form content area
- **Grid/list context menu**: First column = "Actions" with pencil+bars icon button; floating menu `theme.mainContentSection` + `rounded-[4px] shadow-lg`; menu items `px-4 py-2 text-xs ${theme.contextMenu}`; no button column on the right. See `.cursor/skills/context-menu/SKILL.md` and `.claude/reference/03-ui/standards/ContextMenuStandards.md`.

## Workflow

1. Read the component
2. Identify UI standard violations against the checklist above
3. Fix each issue (reference `.claude/reference/03-ui/standards/ReferenceComponents.md` for examples)
4. Verify no new errors introduced

## Input

Provide the React component path to fix (e.g. `src/components/integration/IntegrationSettingEditor.tsx`).
