-- V070: App Form Agent (app-transaction-form-agent) and platform-form-host tools.
-- IF NOT EXISTS only. Re-run does not overwrite prompt or subscriptions.

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-form-host')
INSERT INTO dbo.AppAgentToolLibrary
    (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'platform-form-host',
    N'platform',
    N'BuiltIn: Transaction Form Host',
    N'Tools that read and change the open Master Detail or List Edit form, and list AppFile rows.',
    N'BuiltIn',
    1
);
GO

DECLARE @Plugin nvarchar(300) = N'App.BL.AIAgent.GenericAgent.Plugins.FormHostPlugin';

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-host' AND ToolName = N'get_form_state')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-host', N'get_form_state',
    N'Read the live open form. Optional unitId and rowKey to return one grid or one row when the catalog was truncated.',
    N'{"unitId":{"type":"string","description":"Optional grid unit id"},"rowKey":{"type":"string","description":"Optional row key"}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"GetFormState"}', 1, 0);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-host' AND ToolName = N'describe_action')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-host', N'describe_action',
    N'Detail for one linked search, link target, or command, including the next hop on a link target.',
    N'{"kind":{"type":"string","description":"linkedSearch | linkTarget | command","required":true},"id":{"type":"string","description":"Action id","required":true}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"DescribeAction"}', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-host' AND ToolName = N'set_fields')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-host', N'set_fields',
    N'Set fields on the open form immediately. Does not save. fieldsJson is an object of database field name to value. On List Edit, rowKey selects the row.',
    N'{"fieldsJson":{"type":"string","description":"JSON object of db field name to value","required":true},"rowKey":{"type":"string","description":"List Edit row key, or omit for the selected row / master record"}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"SetFields"}', 1, 2);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-host' AND ToolName = N'update_grid')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-host', N'update_grid',
    N'Add, update, or delete a grid or list row. op is add, update, or delete. Does not save.',
    N'{"unitId":{"type":"string","description":"Grid unit id. Omit on List Edit."},"op":{"type":"string","description":"add | update | delete","required":true},"rowKey":{"type":"string","description":"Required for update and delete"},"fieldsJson":{"type":"string","description":"JSON object of field values for add or update"}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"UpdateGrid"}', 1, 3);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-host' AND ToolName = N'open_linked_search')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-host', N'open_linked_search',
    N'Open the form''s existing Linked Search popup. Returns after the user confirms or cancels. Do not invent the selected rows.',
    N'{"linkedSearchId":{"type":"string","description":"Linked search id","required":true},"unitId":{"type":"string","description":"Host unit id"},"rowKey":{"type":"string","description":"Grid or list row the search starts from"}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"OpenLinkedSearch"}', 1, 4);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-host' AND ToolName = N'open_link_target')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-host', N'open_link_target',
    N'Open a configured Link Target using the existing popup or tab. Returns after the user finishes or cancels.',
    N'{"linkTargetId":{"type":"string","description":"Link target id","required":true},"unitId":{"type":"string","description":"Host unit id"},"rowKey":{"type":"string","description":"Grid or list row"}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"OpenLinkTarget"}', 1, 5);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-host' AND ToolName = N'run_command')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-host', N'run_command',
    N'Run a form command. Save, delete, and server commands require confirmed=true after ask_user. Refresh may omit confirmed.',
    N'{"commandId":{"type":"string","description":"Command id"},"commandName":{"type":"string","description":"Save, Delete, Refresh, or the command display name"},"confirmed":{"type":"string","description":"true after the user confirmed a save, delete, or server command"}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"RunCommand"}', 1, 6);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-host' AND ToolName = N'list_app_files')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-host', N'list_app_files',
    N'List AppFile rows (FileId and name) for the current user. Then render cards and, after the user picks one, set_fields the FileId onto the image field. This is not the agent workspace file library.',
    N'{"keyword":{"type":"string","description":"Optional name filter"},"take":{"type":"string","description":"Max rows, default 30, max 50"}}',
    N'BuiltIn', N'{"TypeName":"' + @Plugin + N'","MethodName":"ListAppFiles"}', 1, 7);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE AgentCode = N'app-transaction-form-agent')
INSERT INTO dbo.AppAgentSkillSet
    (AgentCode, DisplayName, Description, SystemPrompt, CapabilityFlags, IsActive, SortOrder, Version,
     MaxHistoryTokens, SummarizeThreshold, MaxToolResultChars, RecentWindowSize,
     MaxIterations, ExecutionMode, AgentUi)
VALUES (
    N'app-transaction-form-agent',
    N'App Form Agent',
    N'Operates the open Master Detail or List Edit form. Not listed in the left menu.',
    N'You are the App Form Agent. You only help with the transaction form that is open.
Use the form context in the system message. Call get_form_state when you need a fresh or fuller grid.
Change fields with set_fields and grids with update_grid. Those changes are visible immediately and are not saved.
Call run_command for Save, Delete, or a server command only after ask_user confirms. Refresh does not need confirmation.
Open a configured Linked Search or Link Target with open_linked_search or open_link_target. Wait for the user. Do not invent the selection.
To search data that is not a linked search, use execute_sql or api-provider tools, then data_analyze and render_interactive for a grid, cards, or a chart. Do not ask whether to build a KPI dashboard.
To pick an image, call list_app_files, render cards, and after the user picks a file write its fileId with set_fields.
Do not change form design, create tables, or drop tables.',
    3, 1, 5, 1,
    80000, 60000, 8000, 10,
    40, N'Interactive', 1
);
GO

DECLARE @AgentId int;
SELECT @AgentId = AgentID FROM dbo.AppAgentSkillSet WHERE AgentCode = N'app-transaction-form-agent';

IF @AgentId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibrarySubscription WHERE AgentID = @AgentId AND LibraryKey = N'platform-form-host')
    INSERT INTO dbo.AppAgentLibrarySubscription (AgentID, LibraryKey) VALUES (@AgentId, N'platform-form-host');
IF @AgentId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibrarySubscription WHERE AgentID = @AgentId AND LibraryKey = N'platform-database')
    INSERT INTO dbo.AppAgentLibrarySubscription (AgentID, LibraryKey) VALUES (@AgentId, N'platform-database');
IF @AgentId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibrarySubscription WHERE AgentID = @AgentId AND LibraryKey = N'platform-api-provider')
    INSERT INTO dbo.AppAgentLibrarySubscription (AgentID, LibraryKey) VALUES (@AgentId, N'platform-api-provider');
IF @AgentId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibrarySubscription WHERE AgentID = @AgentId AND LibraryKey = N'data-analyze')
    INSERT INTO dbo.AppAgentLibrarySubscription (AgentID, LibraryKey) VALUES (@AgentId, N'data-analyze');
IF @AgentId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibrarySubscription WHERE AgentID = @AgentId AND LibraryKey = N'platform-ui-render')
    INSERT INTO dbo.AppAgentLibrarySubscription (AgentID, LibraryKey) VALUES (@AgentId, N'platform-ui-render');

IF @AgentId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolExclusion WHERE AgentID = @AgentId AND LibraryKey = N'platform-database' AND ToolName = N'create_database_table')
    INSERT INTO dbo.AppAgentToolExclusion (AgentID, LibraryKey, ToolName) VALUES (@AgentId, N'platform-database', N'create_database_table');
IF @AgentId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolExclusion WHERE AgentID = @AgentId AND LibraryKey = N'platform-database' AND ToolName = N'alter_table')
    INSERT INTO dbo.AppAgentToolExclusion (AgentID, LibraryKey, ToolName) VALUES (@AgentId, N'platform-database', N'alter_table');
IF @AgentId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolExclusion WHERE AgentID = @AgentId AND LibraryKey = N'platform-database' AND ToolName = N'insert_mockup_data')
    INSERT INTO dbo.AppAgentToolExclusion (AgentID, LibraryKey, ToolName) VALUES (@AgentId, N'platform-database', N'insert_mockup_data');
IF @AgentId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolExclusion WHERE AgentID = @AgentId AND LibraryKey = N'platform-database' AND ToolName = N'confirm_drop_tables')
    INSERT INTO dbo.AppAgentToolExclusion (AgentID, LibraryKey, ToolName) VALUES (@AgentId, N'platform-database', N'confirm_drop_tables');
GO
