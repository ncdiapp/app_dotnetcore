-- V085: app-configurator Data Model Operation — upsert_command ActionType clarify + SystemPrompt KB index.
-- Product term: Data Model Operation (short: Operation). Tool names (upsert_command, …) unchanged.
-- 1) Tool description: actionType = EmAppTransactionCommandType (NOT Link Target 1=Edit/2=Create).
-- 2) Short SystemPrompt taxonomy index pointing at AgentStarter commands-knowledge.md (not full KB).
-- Idempotent on (2) via NOT LIKE. Formerly split as V085 + V086.

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Create/update Data Model Operation. actionType MUST be EmAppTransactionCommandType (NOT Link Target 1=Edit/2=Create). Common: 50=Refresh, 49=Save, 42=Execute SQL, 43=Data Transfer, 59=Call API, 200=Composition, 54=Close Window, 60/61/62=Send Message, 205=Validate Required Fields. optionsJson: linkToUI, isShowOnTopMenu, isAutoExecuteOnFormOpen, actionFlowOrder, … Preview → ask_user → confirmed=true → request_form_reload. Does not execute operations.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'upsert_command';
GO

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Full Data Model Operation detail (ActionAttribute / options).'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_command_detail';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Delete Data Model Operation. Preview → ask_user → confirmed=true → request_form_reload.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'delete_command';

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'List Data Model Operations on the open form/transaction.'
WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_commands';
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
'
WHERE AgentCode = N'app-configurator'
  AND SystemPrompt NOT LIKE N'%commands-knowledge.md%';
GO
