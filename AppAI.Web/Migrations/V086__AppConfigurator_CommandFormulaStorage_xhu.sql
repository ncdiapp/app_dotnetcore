-- V086: app-configurator — Command Formula (57) + Navigate/Linked Search (53).
-- Merged former V086 + V087. Idempotent: SystemPrompt appends guarded by NOT LIKE;
-- tool descriptions overwritten; list_available_searches IF NOT EXISTS.

DECLARE @FormPlugin nvarchar(300) = N'App.BL.AIAgent.GenericAgent.Plugins.FormConfigPlugin';

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'list_available_searches')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'list_available_searches',
    N'List real AppSearch ids for this transaction SaasApplication. Use BEFORE upsert_linked_search — never invent searchId.',
    N'{"nameContains":{"type":"string"},"take":{"type":"integer"},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin + N'","MethodName":"ListAvailableSearches"}', 1, 26);
GO

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'List unit Linked Search bindings. id/linkedSearchId is for upsert_command actionType 53 — NOT AppSearch.SearchId. If empty: list_available_searches → upsert_linked_search first.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_linked_searches';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Create/update unit Linked Search. Flow: list_available_searches → real searchId → upsert_linked_search(unitId,searchId,name); Action defaults to 3=Open Search. Use returned linkedSearchId on upsert_command 53. Preview→ask_user→confirmed=true.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'upsert_linked_search';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Create/update Data Model Operation. actionType=EmAppTransactionCommandType (NOT Link Target 1/2). Common: 50=Refresh, 49=Save, 42=SQL, 43=Data Transfer, 59=Call API, 57=Cmd Formula, 53=Navigate Search, 200=Composition, 60/61/62=Message, 205=Validate Required. Type 42 SQL→notificationMessage+sqlToken [TF_{fieldId}_{dbName}] (find_fields / rootPrimaryKeys). Never [rootid]. Type 57→optionsJson.executionFormulaUI (ActionAttribute) with formulaToken+[Now]/[Today]; NOT notificationMessage; never Format(Now(),…) or bare Notes=. Type 53→optionsJson.linkedSearchId must be unit Linked Search binding id from get_linked_searches/upsert_linked_search (never invent 1 / AppSearch.SearchId; if empty list_available_searches first). Type 200→childActionList. Message→messageToken. Preview→ask_user→confirmed=true→request_form_reload.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'upsert_command';
GO

DELETE e
FROM dbo.AppAgentToolExclusion e
INNER JOIN dbo.AppAgentSkillSet s ON s.AgentID = e.AgentID
WHERE s.AgentCode = N'app-configurator'
  AND e.LibraryKey = N'platform-form-config'
  AND e.ToolName = N'list_available_searches';
GO

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = SystemPrompt + N'

=== COMMAND FORMULA (57) ===
Storage: expression → optionsJson.executionFormulaUI → ActionAttribute.ExecutionFormulaUI
(persisted in ActionAttribute JSON / FormulaExpression column). NEVER put the formula in notificationMessage (that column is SQL 42 / message bodies).

Syntax (same as Command Formula editor keypad):
- Left side = find_fields / get_unit_fields → formulaToken (root [DbColumn_fieldId], e.g. [Notes_3499]).
- Datetime = [Now] or [Today]. Forbidden: Format(Now(), "yyyy-MM-dd…"), Now(), Today(), bare Notes = ….
- Example: [Notes_3499] = [Now]
- Do NOT use update_unit_formula for Command Formula (that tool is Calculation Flow / unit formulas).
- Fix preview warnings about ExecutionFormulaUI / Format( / bare left side before confirmed=true.
'
WHERE AgentCode = N'app-configurator'
  AND SystemPrompt NOT LIKE N'%COMMAND FORMULA (57) ===%'
  AND SystemPrompt NOT LIKE N'%━━━ COMMAND FORMULA (57)%';
GO

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = SystemPrompt + N'

=== NAVIGATE TO SEARCH / LINKED SEARCH (53) ===
Dropdown options = unit Linked Search bindings (AppTransactionUnitLinkedSearch), not raw AppSearch rows.

Hard flow:
1) get_linked_searches - if empty, do NOT set linkedSearchId yet.
2) list_available_searches - pick real searchId (+ name). Never invent 1 / transactionId / unitId.
3) upsert_linked_search(unitId=rootMaster, searchId, name) confirmed=true (create at least 1; 2+ when user asks).
4) upsert_command actionType 53 with optionsJson.linkedSearchId = returned linkedSearchId (binding id).
5) request_form_reload.

Forbidden: optionsJson.linkedSearchId = AppSearch.SearchId or a made-up number (UI shows fake "Search 1").
upsert_command rejects orphan linkedSearchId that is not on this transaction units.
'
WHERE AgentCode = N'app-configurator'
  AND SystemPrompt NOT LIKE N'%NAVIGATE TO SEARCH / LINKED SEARCH (53)%';
GO
