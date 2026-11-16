-- V084: Form Config Command/DataTransfer/LinkTarget/LinkedSearch mutate tools
--        + Search View Config Agent (app-search-view-config) + platform-search-view-config.
-- IF NOT EXISTS only. Does not overwrite existing prompts on re-run.

-- ═══════════════════════════════════════════════════════════════════════════
-- A) platform-form-config — new BuiltIns on FormConfigPlugin
-- ═══════════════════════════════════════════════════════════════════════════
DECLARE @FormPlugin nvarchar(300) = N'App.BL.AIAgent.GenericAgent.Plugins.FormConfigPlugin';

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_command_detail')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'get_command_detail',
    N'Full Operation Task command detail (ActionAttribute / options).',
    N'{"commandId":{"type":"integer","required":true},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin + N'","MethodName":"GetCommandDetail"}', 1, 20);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'upsert_command')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'upsert_command',
    N'Create/update Operation Task command. Preview → ask_user → confirmed=true → request_form_reload. Does not execute commands.',
    N'{"commandId":{"type":"integer"},"name":{"type":"string"},"actionType":{"type":"integer"},"notificationMessage":{"type":"string"},"optionsJson":{"type":"string"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin + N'","MethodName":"UpsertCommand"}', 1, 21);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'delete_command')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'delete_command',
    N'Delete Operation Task command. Preview → ask_user → confirmed=true → request_form_reload.',
    N'{"commandId":{"type":"integer","required":true},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin + N'","MethodName":"DeleteCommand"}', 1, 22);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_data_transfers')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'get_data_transfers',
    N'List Internal Data Transfer settings on the open transaction.',
    N'{"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin + N'","MethodName":"GetDataTransfers"}', 1, 23);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_data_transfer_detail')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'get_data_transfer_detail',
    N'Data Transfer header + field mappings.',
    N'{"dataTransferId":{"type":"integer","required":true},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin + N'","MethodName":"GetDataTransferDetail"}', 1, 24);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'upsert_data_transfer')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'upsert_data_transfer',
    N'Create/update Data Transfer (+ optional mappingsJson). Preview → ask_user → confirmed=true → request_form_reload.',
    N'{"dataTransferId":{"type":"integer"},"description":{"type":"string"},"internalCode":{"type":"string"},"transferTypeId":{"type":"integer"},"destinationTransactionId":{"type":"integer"},"mappingsJson":{"type":"string"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin + N'","MethodName":"UpsertDataTransfer"}', 1, 25);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'delete_data_transfer')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'delete_data_transfer',
    N'Delete Data Transfer. Preview → ask_user → confirmed=true → request_form_reload.',
    N'{"dataTransferId":{"type":"integer","required":true},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin + N'","MethodName":"DeleteDataTransfer"}', 1, 26);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'upsert_linked_search')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'upsert_linked_search',
    N'Create/update unit Linked Search binding (not Link Target menus). Preview → ask_user → confirmed=true.',
    N'{"unitId":{"type":"integer","required":true},"linkedSearchId":{"type":"integer"},"name":{"type":"string"},"searchId":{"type":"integer"},"changesJson":{"type":"string"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin + N'","MethodName":"UpsertLinkedSearch"}', 1, 27);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'delete_linked_search')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'delete_linked_search',
    N'Delete unit Linked Search binding. Preview → ask_user → confirmed=true.',
    N'{"linkedSearchId":{"type":"integer","required":true},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin + N'","MethodName":"DeleteLinkedSearch"}', 1, 28);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_link_targets')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'get_link_targets',
    N'List Transaction Unit AppFormLinkTarget menus (usageType 101=LinkToForm, 5=System page). Prefer unitId.',
    N'{"unitId":{"type":"integer","required":true},"usageType":{"type":"integer"},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin + N'","MethodName":"GetLinkTargets"}', 1, 29);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'get_link_target_detail')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'get_link_target_detail',
    N'Full AppFormLinkTarget detail for a Transaction Unit menu.',
    N'{"linkTargetId":{"type":"integer","required":true},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin + N'","MethodName":"GetLinkTargetDetail"}', 1, 30);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'upsert_link_target')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'upsert_link_target',
    N'Create/update Transaction Unit Link Target. usageType 101 needs linkTargetTransactionId. Preview → ask_user → confirmed=true → request_form_reload.',
    N'{"unitId":{"type":"integer","required":true},"linkTargetId":{"type":"integer"},"usageType":{"type":"integer"},"actionType":{"type":"integer"},"navigationActionName":{"type":"string"},"linkTargetTransactionId":{"type":"integer"},"linkTargetTransactionGroupId":{"type":"integer"},"linkTargetSearchId":{"type":"integer"},"linkTargetUrlOrRouteCode":{"type":"string"},"dataTransferSettingId":{"type":"integer"},"sort":{"type":"integer"},"isPopup":{"type":"boolean"},"popupWidth":{"type":"integer"},"popupHeight":{"type":"integer"},"iconName":{"type":"string"},"groupName":{"type":"string"},"sourceColumnType":{"type":"integer"},"sourceColumn1":{"type":"string"},"sourceColumn2":{"type":"string"},"sourceColumn3":{"type":"string"},"targetColumn1":{"type":"string"},"targetColumn2":{"type":"string"},"targetColumn3":{"type":"string"},"layoutDisplayMode":{"type":"integer"},"otherSettingsJson":{"type":"string"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin + N'","MethodName":"UpsertLinkTarget"}', 1, 31);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-form-config' AND ToolName = N'delete_link_target')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-form-config', N'delete_link_target',
    N'Delete Transaction Unit Link Target. Preview → ask_user → confirmed=true → request_form_reload.',
    N'{"unitId":{"type":"integer","required":true},"linkTargetId":{"type":"integer","required":true},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"transactionId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @FormPlugin + N'","MethodName":"DeleteLinkTarget"}', 1, 32);
GO

DELETE e
FROM dbo.AppAgentToolExclusion e
INNER JOIN dbo.AppAgentSkillSet s ON s.AgentID = e.AgentID
WHERE s.AgentCode = N'app-configurator'
  AND e.LibraryKey = N'platform-form-config'
  AND e.ToolName IN (
      N'get_command_detail', N'upsert_command', N'delete_command',
      N'get_data_transfers', N'get_data_transfer_detail', N'upsert_data_transfer', N'delete_data_transfer',
      N'upsert_linked_search', N'delete_linked_search',
      N'get_link_targets', N'get_link_target_detail', N'upsert_link_target', N'delete_link_target');
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- B) platform-search-view-config + app-search-view-config
-- ═══════════════════════════════════════════════════════════════════════════
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-search-view-config')
INSERT INTO dbo.AppAgentToolLibrary
    (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'platform-search-view-config',
    N'platform',
    N'BuiltIn: Search View Configurator',
    N'Durable Search / View / Dataset / SearchView Link Target configuration. Not FormData; not create_search.',
    N'BuiltIn',
    1
);
GO

DECLARE @SearchPlugin nvarchar(300) = N'App.BL.AIAgent.GenericAgent.Plugins.SearchViewConfigPlugin';

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-search-view-config' AND ToolName = N'get_search_config')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-search-view-config', N'get_search_config',
    N'Inspect Search + View/Dataset ids + criteria/columns summary + link-target count.',
    N'{"searchId":{"type":"integer","description":"Required in Test Run without HostContext"}}',
    N'BuiltIn', N'{"TypeName":"' + @SearchPlugin + N'","MethodName":"GetSearchConfig"}', 1, 0);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-search-view-config' AND ToolName = N'update_search_basic_setting')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-search-view-config', N'update_search_basic_setting',
    N'Update AppSearch root via changesJson. Preview → ask_user → confirmed=true → request_search_reload.',
    N'{"changesJson":{"type":"string","required":true},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"searchId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @SearchPlugin + N'","MethodName":"UpdateSearchBasicSetting"}', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-search-view-config' AND ToolName = N'update_search_criteria')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-search-view-config', N'update_search_criteria',
    N'CRUD AppSearchField. op=add|update|delete. Preview → ask_user → confirmed=true → request_search_reload.',
    N'{"op":{"type":"string","required":true},"searchFieldId":{"type":"integer"},"changesJson":{"type":"string"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"searchId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @SearchPlugin + N'","MethodName":"UpdateSearchCriteria"}', 1, 2);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-search-view-config' AND ToolName = N'update_search_view_columns')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-search-view-config', N'update_search_view_columns',
    N'CRUD Search View columns. op=add|update|delete. Preview → ask_user → confirmed=true → request_search_reload.',
    N'{"op":{"type":"string","required":true},"searchViewFieldId":{"type":"integer"},"changesJson":{"type":"string"},"searchViewId":{"type":"integer"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"searchId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @SearchPlugin + N'","MethodName":"UpdateSearchViewColumns"}', 1, 3);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-search-view-config' AND ToolName = N'update_dataset')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-search-view-config', N'update_dataset',
    N'Update AppDataSet via changesJson. Warn if shared. Optional syncViewColumns. Preview → ask_user → confirmed=true.',
    N'{"changesJson":{"type":"string","required":true},"dataSetId":{"type":"integer"},"syncViewColumns":{"type":"boolean"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"searchId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @SearchPlugin + N'","MethodName":"UpdateDataset"}', 1, 4);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-search-view-config' AND ToolName = N'get_search_view_link_targets')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-search-view-config', N'get_search_view_link_targets',
    N'List Search View AppFormLinkTarget menus (1=Form 2=FormGroup 3=Search 4=Report 5=SystemPage).',
    N'{"usageType":{"type":"integer"},"searchViewId":{"type":"integer"},"searchId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @SearchPlugin + N'","MethodName":"GetSearchViewLinkTargets"}', 1, 5);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-search-view-config' AND ToolName = N'get_search_view_link_target_detail')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-search-view-config', N'get_search_view_link_target_detail',
    N'Full Search View Link Target detail.',
    N'{"linkTargetId":{"type":"integer","required":true},"searchId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @SearchPlugin + N'","MethodName":"GetSearchViewLinkTargetDetail"}', 1, 6);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-search-view-config' AND ToolName = N'upsert_search_view_link_target')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-search-view-config', N'upsert_search_view_link_target',
    N'Create/update Search View Link Target (Link To Form/Search/…). Preview → ask_user → confirmed=true → request_search_reload.',
    N'{"linkTargetId":{"type":"integer"},"usageType":{"type":"integer","required":true},"actionType":{"type":"integer"},"navigationActionName":{"type":"string"},"linkTargetTransactionId":{"type":"integer"},"linkTargetTransactionGroupId":{"type":"integer"},"linkTargetSearchId":{"type":"integer"},"linkTargetUrlOrRouteCode":{"type":"string"},"dataTransferSettingId":{"type":"integer"},"sort":{"type":"integer"},"isPopup":{"type":"boolean"},"popupWidth":{"type":"integer"},"popupHeight":{"type":"integer"},"iconName":{"type":"string"},"groupName":{"type":"string"},"sourceColumnType":{"type":"integer"},"sourceViewColumnId1":{"type":"integer"},"sourceViewColumnId2":{"type":"integer"},"sourceViewColumnId3":{"type":"integer"},"targetSearchFieldId1":{"type":"integer"},"targetSearchFieldId2":{"type":"integer"},"targetSearchFieldId3":{"type":"integer"},"layoutDisplayMode":{"type":"integer"},"otherSettingsJson":{"type":"string"},"searchViewId":{"type":"integer"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"searchId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @SearchPlugin + N'","MethodName":"UpsertSearchViewLinkTarget"}', 1, 7);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-search-view-config' AND ToolName = N'delete_search_view_link_target')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-search-view-config', N'delete_search_view_link_target',
    N'Delete Search View Link Target. Preview → ask_user → confirmed=true → request_search_reload.',
    N'{"linkTargetId":{"type":"integer","required":true},"searchViewId":{"type":"integer"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"searchId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @SearchPlugin + N'","MethodName":"DeleteSearchViewLinkTarget"}', 1, 8);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-search-view-config' AND ToolName = N'get_view_linked_search_or_url')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-search-view-config', N'get_view_linked_search_or_url',
    N'List AppViewLinkedSeaechOrUrl (inline linked Search/URL — not Link Target menus).',
    N'{"searchViewId":{"type":"integer"},"searchId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @SearchPlugin + N'","MethodName":"GetViewLinkedSearchOrUrl"}', 1, 9);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-search-view-config' AND ToolName = N'upsert_view_linked_search_or_url')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-search-view-config', N'upsert_view_linked_search_or_url',
    N'Create/update inline linked Search/URL. Preview → ask_user → confirmed=true.',
    N'{"id":{"type":"integer"},"name":{"type":"string"},"linkTargetSearchId":{"type":"integer"},"linkTargetUrlOrRouteCode":{"type":"string"},"layoutDisplayMode":{"type":"integer"},"searchViewId":{"type":"integer"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"searchId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @SearchPlugin + N'","MethodName":"UpsertViewLinkedSearchOrUrl"}', 1, 10);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-search-view-config' AND ToolName = N'delete_view_linked_search_or_url')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-search-view-config', N'delete_view_linked_search_or_url',
    N'Delete inline linked Search/URL. Preview → ask_user → confirmed=true.',
    N'{"id":{"type":"integer","required":true},"searchViewId":{"type":"integer"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"searchId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @SearchPlugin + N'","MethodName":"DeleteViewLinkedSearchOrUrl"}', 1, 11);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-search-view-config' AND ToolName = N'request_search_reload')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-search-view-config', N'request_search_reload',
    N'Ask Search host to prompt Reload after configuration writes.',
    N'{}',
    N'BuiltIn', N'{"TypeName":"' + @SearchPlugin + N'","MethodName":"RequestSearchReload"}', 1, 12);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE AgentCode = N'app-search-view-config')
INSERT INTO dbo.AppAgentSkillSet
    (AgentCode, DisplayName, Description, SystemPrompt, CapabilityFlags, IsActive, SortOrder, Version,
     MaxHistoryTokens, SummarizeThreshold, MaxToolResultChars, RecentWindowSize,
     MaxIterations, ExecutionMode, AgentUi)
VALUES (
    N'app-search-view-config',
    N'Search View Config Agent',
    N'Configures the open Search / View / Dataset / SearchView Link Targets. Not listed in the left menu.',
    N'You are the Search View Config Agent for the open Search (HostContextJson searchId / searchViewId / dataSetId).
You change durable Search metadata only — not FormData, not Form/TX field config (that is app-configurator).

━━━ 1. CONCEPTS ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
- HostContext forces searchId; Test Run without host requires explicit searchId.
- Link Target (AppFormLinkTarget on SearchView): usageType 1=Form 2=FormGroup 3=Search 4=Report 5=SystemPage.
- Inline AppViewLinkedSeaechOrUrl is separate from Link Target menus.
- Prefer ids (searchFieldId, searchViewFieldId, linkTargetId, dataSetId).

━━━ 2. DEFAULT SEQUENCES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
A) Inspect: get_search_config
B) Rename / rebind View or Dataset: update_search_basic_setting → ask_user → confirmed=true → request_search_reload
C) Criteria CRUD: update_search_criteria(op=…) → ask_user → confirmed=true → request_search_reload
D) View columns: update_search_view_columns(op=…) → ask_user → confirmed=true → request_search_reload
E) Dataset SQL: update_dataset → warn if shared → ask_user → confirmed=true → request_search_reload
F) Link To Form/Search/…: get_search_view_link_targets → upsert_search_view_link_target → ask_user → confirmed=true → request_search_reload

━━━ 3. HARD RULES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✓ Always ask_user before durable writes.
✓ After successful writes call request_search_reload.
✓ Stay on HostContext searchId / matching view/dataset.
✗ Do not create_search / greenfield apps.
✗ Do not edit Form TX fields / formulas / commands (send user to Form Configurator).
✗ Do not invent ids — read from get_search_config / list tools.

When unsure, ask_user with concrete options.',
    3, 1, 7, 1,
    80000, 60000, 8000, 10,
    40, N'Interactive', 1
);
GO

DECLARE @SearchAgentId int;
SELECT @SearchAgentId = AgentID FROM dbo.AppAgentSkillSet WHERE AgentCode = N'app-search-view-config';

IF @SearchAgentId IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM dbo.AppAgentLibrarySubscription WHERE AgentID = @SearchAgentId AND LibraryKey = N'platform-search-view-config')
    INSERT INTO dbo.AppAgentLibrarySubscription (AgentID, LibraryKey) VALUES (@SearchAgentId, N'platform-search-view-config');

-- Optional read discovery for Test Run
IF @SearchAgentId IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-search')
AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibrarySubscription WHERE AgentID = @SearchAgentId AND LibraryKey = N'platform-search')
    INSERT INTO dbo.AppAgentLibrarySubscription (AgentID, LibraryKey) VALUES (@SearchAgentId, N'platform-search');

-- Exclude create/mutate from platform-search if subscribed (keep list_* only when those tools exist)
IF @SearchAgentId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolExclusion WHERE AgentID=@SearchAgentId AND LibraryKey=N'platform-search' AND ToolName=N'create_search')
        AND EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey=N'platform-search' AND ToolName=N'create_search')
        INSERT INTO dbo.AppAgentToolExclusion (AgentID, LibraryKey, ToolName) VALUES (@SearchAgentId, N'platform-search', N'create_search');
END
GO
