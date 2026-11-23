-- V086: app-configurator — Command Formula (57) storage + formulaToken / [Now] hard rules.
-- Idempotent: SystemPrompt append guarded by NOT LIKE; tool description overwritten.

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Create/update Data Model Operation. actionType=EmAppTransactionCommandType (NOT Link Target 1/2). Common: 50=Refresh, 49=Save, 42=SQL, 43=Data Transfer, 59=Call API, 57=Cmd Formula, 200=Composition, 60/61/62=Message, 205=Validate Required. Type 42 SQL → notificationMessage + sqlToken [TF_{fieldId}_{dbName}] (find_fields / rootPrimaryKeys). Never [rootid]. Type 57 → optionsJson.executionFormulaUI (ActionAttribute) with formulaToken + [Now]/[Today]; NOT notificationMessage; never Format(Now(),…) or bare Notes=. Type 200: Private children first, then optionsJson.childActionList:[{commandId,sort}]. Message→messageToken. Preview→ask_user→confirmed=true→request_form_reload.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'upsert_command';
GO

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = SystemPrompt + N'

━━━ COMMAND FORMULA (57) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Storage: expression → optionsJson.executionFormulaUI → ActionAttribute.ExecutionFormulaUI
(persisted in ActionAttribute JSON / FormulaExpression column). NEVER put the formula in notificationMessage (that column is SQL 42 / message bodies).

Syntax (same as Command Formula editor keypad):
• Left side = find_fields / get_unit_fields → formulaToken (root [DbColumn_fieldId], e.g. [Notes_3499]).
• Datetime = [Now] or [Today]. Forbidden: Format(Now(), "yyyy-MM-dd…"), Now(), Today(), bare Notes = ….
• Example: [Notes_3499] = [Now]
• Do NOT use update_unit_formula for Command Formula (that tool is Calculation Flow / unit formulas).
• Fix preview warnings about ExecutionFormulaUI / Format( / bare left side before confirmed=true.
'
-- Guard must be the section header — V085 already contains the phrase "Command Formula (57): formulaToken".
WHERE AgentCode = N'app-configurator'
  AND SystemPrompt NOT LIKE N'%━━━ COMMAND FORMULA (57)%';
GO
