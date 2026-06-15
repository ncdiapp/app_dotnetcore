Migrate an AngularJS feature to React following the project standards.

**Read**: `.claude/skills/angularjs-to-react-migration.md` for conversion rules. Full workflow: `.claude/reference/02-migration/ConverterAngularJsPage.md` (9-step), `.claude/reference/02-migration/ConverterAngularServiceToReact.md` (service rules), `.claude/reference/02-migration/DevelopmentPrompt.md` (reference implementation). UI: `.claude/reference/03-ui/UIMainPrompt.md`. Scenario: `.claude/reference/00-scenarios/ScenarioTemplates.md` (Scenario 1).

**Workflow:**
1. Locate the AngularJS implementation (controller: `Scripts1x\mgtCtrl\`, service: `Scripts1x\Services\`, view: `Server\Views\`, WebAPI: `Server\WebApi\`)
2. Understand business logic in `APP.BL\` and DTOs in `APP.Components.Dto\` (EntityExdto, UserDefine, AppEnums.cs); identify server-side model properties that may not be in API responses
3. Plan the React implementation (components, state, services, routes); for dictionary-response APIs (e.g. getMassEntitiesLookupItem) build entity code list programmatically, use constants, `|| []` fallback
4. Implement following UI standards (theme tokens, page layout, buttons, forms, flexbox, icons); reference components: UserLoginInfoEditor, UserManagement, FormMasterDetail
5. Verify functionality matches AngularJS
6. Ensure no TypeScript/linter/console errors; clean imports (no `.tsx` suffix)

**Key Rules:**
- Keep Angular method names (PascalCase)
- Use `${param || ''}` for query params; include all params in URL
- Never use `flex-1` — use `w-1 flex-auto` or `h-1 flex-auto`
- Always use theme tokens from `useTheme()` hook
- Use `appHelper.debugLog()` not `console.log()`
- JSON for UI: `JSON.stringify(value, null, 2)`

Argument: Provide the AngularJS RouteCode or component path to migrate.
