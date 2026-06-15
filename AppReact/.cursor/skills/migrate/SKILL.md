---
name: migrate
description: Migrate an AngularJS feature to React following project standards. Use when the user asks to migrate a feature, port AngularJS to React, or implement a new React component from AngularJS (RouteCode or component path).
---

# Migrate Skill

Migrate an AngularJS feature to React following the project standards.

**Read** the angularjs-to-react-migration skill (`.cursor/skills/angularjs-to-react-migration/SKILL.md`) for conversion rules. Full workflow: `.claude/reference/02-migration/ConverterAngularJsPage.md` (9-step), `.claude/reference/02-migration/ConverterAngularServiceToReact.md`, `.claude/reference/02-migration/DevelopmentPrompt.md`. UI: `.claude/reference/03-ui/UIMainPrompt.md`. Scenario: `.claude/reference/00-scenarios/ScenarioTemplates.md` (Scenario 1). **Wijmo**: grid multi-select / dual-list → `.claude/reference/03-ui/standards/WijmoGridMultiSelectStandards.md`; **ComboBox cascading** (parent DDL drives child DDLs, init handler after bind, remove before refresh) → `.claude/reference/03-ui/standards/WijmoComboBoxCascadingStandards.md`; **column visibility** (show/hide columns by toggle, use `visible` prop, do not conditionally render columns) → `.claude/reference/03-ui/standards/WijmoFlexGridColumnVisibility.md`.

## Workflow

1. Locate the AngularJS implementation (controller: `Scripts1x\mgtCtrl\`, service: `Scripts1x\Services\`, view: `Server\Views\`, WebAPI: `Server\WebApi\`)
2. Understand business logic in `APP.BL\` and DTOs in `APP.Components.Dto\` (EntityExdto, UserDefine, AppEnums.cs); identify server-side model properties that may not be in API responses
3. Plan the React implementation (components, state, services, routes); for dictionary-response APIs (e.g. getMassEntitiesLookupItem) build entity code list programmatically, use constants, `|| []` fallback
4. Implement following UI standards (theme tokens, page layout, buttons, forms, flexbox, icons); reference components: UserLoginInfoEditor, UserManagement, FormMasterDetail
5. Verify functionality matches AngularJS
6. Ensure no TypeScript/linter/console errors; clean imports (no `.tsx` suffix)

## Key Rules

- Keep Angular method names (PascalCase)
- Use `${param || ''}` for query params; include all params in URL
- Never use `flex-1` — use `w-1 flex-auto` or `h-1 flex-auto`
- Always use theme tokens from `useTheme()` hook
- Use `appHelper.debugLog()` not `console.log()`
- JSON for UI: `JSON.stringify(value, null, 2)`

## Input

Provide the AngularJS RouteCode or component path to migrate.
