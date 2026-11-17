-- V085: app-configurator Data Model Operation — tool descriptions + SystemPrompt
-- (taxonomy index + field-token hard rules). Product term: Data Model Operation.
-- Tool names (upsert_command, …) unchanged. Idempotent SystemPrompt via NOT LIKE.
-- Merged former V085 + V086 (field tokens / Query Builder alignment).

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Create/update Data Model Operation. actionType=EmAppTransactionCommandType (NOT Link Target 1/2). Common: 50=Refresh, 49=Save, 42=SQL, 43=Data Transfer, 59=Call API, 57=Cmd Formula, 200=Composition, 60/61/62=Message, 205=Validate Required. Type 42 SQL → notificationMessage + sqlToken [TF_{fieldId}_{dbName}] (find_fields / rootPrimaryKeys). Never [rootid]. Type 200: create Private children first, then optionsJson.childActionList:[{commandId,sort}] — empty ChildActionList = empty Child Operations grid; do not put SQL on composition parent. Message→messageToken; Formula→formulaToken. Preview→ask_user→confirmed=true→request_form_reload.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'upsert_command';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Full Data Model Operation detail (ActionAttribute / options).'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_command_detail';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Delete Data Model Operation. Preview → ask_user → confirmed=true → request_form_reload.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'delete_command';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'List Data Model Operations on the open form/transaction.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_commands';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Inspect open form config: units, fields (sqlToken/messageToken/formulaToken/isPrimaryKey/unitKind), rootPrimaryKeys, commands. Any named field → units[].fields[].sqlToken; current-row WHERE → rootPrimaryKeys[].sqlToken.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_current_transaction_config';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'List fields for one unit. Each field: sqlToken/messageToken [TF_{fieldId}_{dbName}], formulaToken, isPrimaryKey, unitKind (rootMaster|masterSibling|childGrid). Same catalog as Query Builder token buttons.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_unit_fields';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Search fields by display name or DB column across units. Returns sqlToken/messageToken/formulaToken/isPrimaryKey/unitKind for each hit — use when the user names a field (Notes, Title, OpportunityId, …). Current-row SQL WHERE: prefer isPrimaryKey on unitKind=rootMaster (or rootPrimaryKeys).'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'find_fields';
GO

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = SystemPrompt + N'

━━━ DATA MODEL OPERATION ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Product name: Data Model Operation (short: Operation). Legacy docs may say Command / Operation Task.
KB file (AgentStarter / chat source): commands-knowledge.md — read via list_agent_files / read_agent_file before upsert_command.
actionType = EmAppTransactionCommandType ONLY (never Link Target 1=Edit / 2=Create).

Taxonomy (L1 → L2 → Id):
• UI Ops: Close 54, Navigate Search 53, Print 48, Print From MsgTpl 74, Save As 47
• Backend → Form Refresh, Validate & Save: Save 49, Refresh 50, Validate Required 205
• Backend → Form Data Processing: All Formula 44, Cmd Formula 57, All DataLoad 45, Matrix 46, SQL 42, Data Transfer 43
• Backend → Import & File Generation: Json/Excel/Rest/DB 66/67/71/72, Multi 76/77, Download 73, Xml↔Json 81/82
• API / Plugin / External: Call API 59, Plugin WebApi 40, Ext Method 38, Ext Exe 68
• Messaging: UserId 60, Email 61, Partner 62, SMS 63
• Composition: 200 (ordered children)
Do not offer 51 / 55 / 70 for new operations.
SQL body for type 42 → notificationMessage. Public flags → optionsJson (linkToUI, isShowOnTopMenu, isAutoExecuteOnFormOpen, actionFlowOrder).
Preview → ask_user → confirmed=true → request_form_reload. NEVER execute runtime operations.

━━━ FIELD TOKENS (Query Builder / Message / Formula) ━━━━━━━━━━━━━━━━━━━
UI: Operation editor → Query Builder with Built-in Tokens shows unit-layered field buttons
(Form Root Level Fields, Grid {unit} Columns). Clicking inserts a live token — agents must use the same strings.

Resolve tokens BEFORE writing SQL/message/formula:
• User names a field (e.g. Notes, Title) → find_fields(name) or get_unit_fields → that hit’s sqlToken / messageToken / formulaToken
• Current form row / root master id → get_current_transaction_config.rootPrimaryKeys[].sqlToken (or isPrimaryKey on unitKind=rootMaster)
• Full catalog → get_current_transaction_config units[].fields[] (every field has sqlToken)

Token formats (do not invent):
• SQL (actionType 42) & Message templates: [TF_{fieldId}_{dbName}] = sqlToken / messageToken
  Example root PK: [TF_3499_OpportunityId]
• Command Formula (57): formulaToken — root [DbColumn_fieldId]; grid/sibling [Table.DbColumn_fieldId]
• Built-ins only as listed in tokenGuide (SQL: [CurrentUserId], GETDATE(), …). [CurrentPkValue] is message-only.

Hard rules for Execute SQL (42):
1) Put SQL in notificationMessage.
2) UPDATE/DELETE MUST include WHERE scoped to the intended row(s).
3) For “this form / current record”, WHERE on root master PK using rootPrimaryKeys[].sqlToken — NOT [rootid], NOT a bare column name.
4) Other named fields also use their own sqlToken when substituting form values (find_fields → sqlToken).
5) Preview warnings about missing WHERE / [TF_ / [rootid] must be fixed before confirmed=true.

Composition (200):
• Create Private child operations first (linkToUI:false), then set optionsJson.childActionList:[{commandId,sort},…].
• Empty ChildActionList ⇒ empty Child Operations grid. Do not put SQL on the composition parent (use child actionType 42).
'
WHERE AgentCode = N'app-configurator'
  AND SystemPrompt NOT LIKE N'%FIELD TOKENS (Query Builder / Message / Formula)%';
GO
