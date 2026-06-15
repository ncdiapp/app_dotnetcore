Analyze a React component against its AngularJS counterpart to find missing functionality.

**Read**: `.claude/skills/angularjs-to-react-migration.md` for standards. For full UI rules see `.claude/reference/03-ui/UIMainPrompt.md`, `.claude/reference/03-ui/standards/` (PageLayoutStandards, ButtonStandards, FormStandards, ThemeUsageStandards, TailwindCSSStandards, ReferenceComponents), `.claude/reference/03-ui/layout/TailwindFlexBoxRemainSpace.md`. Scenario: `.claude/reference/00-scenarios/ScenarioTemplates.md` (Scenario 2).

**Workflow:**
1. Read the current React implementation
2. Locate the AngularJS implementation using the RouteCode (controllers: `PlmApplication\Scripts1x\mgtCtrl\`, services: `Scripts1x\Services\`, views: `Server\Views\`, WebAPI: `Server\WebApi\`)
3. Compare functionality and identify gaps:
   - Missing features
   - Incomplete implementations
   - UI non-compliance with standards (layout, theme, buttons, forms, flexbox, icons)
   - Code quality issues (debug logging, imports, error handling)
4. Generate a detailed analysis report
5. Wait for user confirmation before proceeding with fixes

**Output Format:**
- Functionality comparison checklist
- Missing features list
- UI standards compliance check
- Code quality check
- Suggested fixes

Arguments: `<react-component-path> <angularjs-route-code>`
