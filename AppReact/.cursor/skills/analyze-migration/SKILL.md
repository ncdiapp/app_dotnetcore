---
name: analyze-migration
description: Analyze a React component against its AngularJS counterpart to find missing functionality. Use when comparing React vs AngularJS implementations, auditing migration completeness, or generating analysis reports before fixes.
---

# Analyze Migration Skill

Analyze a React component against its AngularJS counterpart to find missing functionality.

**Read** the angularjs-to-react-migration skill (`.cursor/skills/angularjs-to-react-migration/SKILL.md`) for standards. For full UI rules see `.claude/reference/03-ui/UIMainPrompt.md`, `.claude/reference/03-ui/standards/` (PageLayoutStandards, ButtonStandards, FormStandards, ThemeUsageStandards, TailwindCSSStandards, ReferenceComponents, **WijmoFlexGridColumnVisibility**), `.claude/reference/03-ui/layout/TailwindFlexBoxRemainSpace.md`. Scenario: `.claude/reference/00-scenarios/ScenarioTemplates.md` (Scenario 2).

## Workflow

1. Read the current React implementation
2. Locate the AngularJS implementation using the RouteCode (controllers: `PlmApplication\Scripts1x\mgtCtrl\`, services: `Scripts1x\Services\`, views: `Server\Views\`, WebAPI: `Server\WebApi\`)
3. Compare functionality and identify gaps:
   - Missing features
   - Incomplete implementations
   - UI non-compliance (layout, theme, buttons, forms, flexbox, icons)
   - Code quality (debug logging, imports, error handling)
4. Generate a detailed analysis report
5. Wait for user confirmation before proceeding with fixes

## Output Format

- Functionality comparison checklist
- Missing features list
- UI standards compliance check
- Code quality check
- Suggested fixes

## Inputs

When the user invokes this skill, obtain:
- **React component path** (e.g. `src/components/admin/MyApplicationEditor.tsx`)
- **AngularJS route code** (or enough context to locate controller/view)

Arguments pattern: `<react-component-path> <angularjs-route-code>`
