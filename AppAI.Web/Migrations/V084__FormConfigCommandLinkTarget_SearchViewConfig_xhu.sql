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
    N'CRUD AppSearchField. op=add|update|delete. DDL needs ControlType=1 + EntityId from search_lookup_entities. Preview → ask_user → confirmed=true → request_search_reload.',
    N'{"op":{"type":"string","required":true},"searchFieldId":{"type":"integer"},"changesJson":{"type":"string"},"confirmed":{"type":"boolean"},"dryRun":{"type":"boolean"},"searchId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @SearchPlugin + N'","MethodName":"UpdateSearchCriteria"}', 1, 2);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-search-view-config' AND ToolName = N'search_lookup_entities')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-search-view-config', N'search_lookup_entities',
    N'Search AppEntityInfo for DDL EntityId (EntityCode/TableName/Description). Prefer host SaasApplicationId. NEVER use SearchId as EntityId. ask_user with id=EntityId.',
    N'{"query":{"type":"string","required":true},"saasApplicationId":{"type":"integer"},"take":{"type":"integer"},"searchId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @SearchPlugin + N'","MethodName":"SearchLookupEntities"}', 1, 13);

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
C2) Criteria DDL Entity (ONE confirm only): search_lookup_entities(query) → ask_user pick EntityId → immediately update_search_criteria(ControlType=1, EntityId, confirmed=true) → request_search_reload. Entity pick IS the confirmation — do NOT ask_user Yes/No again / do NOT dryRun-preview after the pick.
D) View columns: update_search_view_columns(op=…) → ask_user → confirmed=true → request_search_reload
E) Dataset SQL: update_dataset → warn if shared → ask_user → confirmed=true → request_search_reload
F) Link To Form/Search/…: get_search_view_link_targets → upsert_search_view_link_target → ask_user → confirmed=true → request_search_reload

━━━ 3. HARD RULES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✓ Always ask_user before durable writes (except: after DDL Entity pick, call confirmed=true immediately — that ask_user already confirmed).
✓ Never ask the user to confirm the same write 2–3 times.
✓ After successful writes call request_search_reload.
✓ Stay on HostContext searchId / matching view/dataset.
✓ DDL / SearchAbleDDL / AutoComplete MUST use AppEntityInfo EntityId from search_lookup_entities (ControlType DDL=1, not 6/Grid).
✗ NEVER offer SearchId / searchFieldId / list_available_searches as EntityId candidates.
✗ Do not create_search / greenfield apps.
✗ Do not edit Form TX fields / formulas / commands (send user to Form Configurator).
✗ Do not invent ids — read from get_search_config / search_lookup_entities / list tools.

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

-- Exclude create/delete / broken report get_search_criteria from platform-search if subscribed
IF @SearchAgentId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolExclusion WHERE AgentID=@SearchAgentId AND LibraryKey=N'platform-search' AND ToolName=N'create_search')
        AND EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey=N'platform-search' AND ToolName=N'create_search')
        INSERT INTO dbo.AppAgentToolExclusion (AgentID, LibraryKey, ToolName) VALUES (@SearchAgentId, N'platform-search', N'create_search');

    -- Report-agent get_search_criteria throws LLBL "out of sync" in this host; use get_search_config instead.
    IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolExclusion WHERE AgentID=@SearchAgentId AND LibraryKey=N'platform-search' AND ToolName=N'get_search_criteria')
        AND EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey=N'platform-search' AND ToolName=N'get_search_criteria')
        INSERT INTO dbo.AppAgentToolExclusion (AgentID, LibraryKey, ToolName) VALUES (@SearchAgentId, N'platform-search', N'get_search_criteria');
END
GO

-- ========== App Builder left-sidebar menu list / update / delete (by menuId) ==========
-- One delete + one update tool — NOT per Search/Transaction type.

-- Drop superseded type-specific remove tools if ever seeded
DELETE FROM dbo.AppAgentLibraryTool
WHERE LibraryKey = N'platform-application'
  AND ToolName IN (N'remove_search_from_menu', N'remove_transaction_from_menu', N'remove_nav_menu_item');

DELETE e
FROM dbo.AppAgentToolExclusion e
INNER JOIN dbo.AppAgentSkillSet s ON s.AgentID = e.AgentID
WHERE s.AgentCode = N'app-builder'
  AND e.LibraryKey = N'platform-application'
  AND e.ToolName IN (
      N'remove_search_from_menu',
      N'remove_transaction_from_menu',
      N'remove_nav_menu_item');
GO

DECLARE @AppMenuPlugin nvarchar(300) = N'App.BL.AppBuilderAgent.Plugins.ApplicationManagerPlugin';

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-application' AND ToolName = N'list_app_nav_menu')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-application', N'list_app_nav_menu',
    N'List left-sidebar menu items under one SaasApplicationId (menuId, name, kind=search|listTransaction, linkedSearchId/transactionId). Use before update_nav_menu_item / delete_nav_menu_item.',
    N'{"saasApplicationId":{"type":"integer","description":"Application package id","required":true}}',
    N'BuiltIn', N'{"TypeName":"' + @AppMenuPlugin + N'","MethodName":"ListAppNavMenu"}', 1, 8);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-application' AND ToolName = N'update_nav_menu_item')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-application', N'update_nav_menu_item',
    N'Update one left-sidebar menu node by menuId. Pass name and/or sort and/or iconName. Does not change route/link.',
    N'{"menuId":{"type":"integer","required":true},"name":{"type":"string"},"sort":{"type":"integer"},"iconName":{"type":"string"}}',
    N'BuiltIn', N'{"TypeName":"' + @AppMenuPlugin + N'","MethodName":"UpdateNavMenuItem"}', 1, 9);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-application' AND ToolName = N'delete_nav_menu_item')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-application', N'delete_nav_menu_item',
    N'Delete one left-sidebar menu node by menuId (from list_app_nav_menu). Works for Search or List shortcuts. Does NOT delete Search/transaction. ask_user before calling.',
    N'{"menuId":{"type":"integer","required":true},"saasApplicationId":{"type":"integer","description":"Optional safety check — must be direct child of this app"}}',
    N'BuiltIn', N'{"TypeName":"' + @AppMenuPlugin + N'","MethodName":"DeleteNavMenuItem"}', 1, 10);
GO

INSERT INTO dbo.AppAgentLibrarySubscription (LibraryKey, AgentID)
SELECT N'platform-application', s.AgentID
FROM dbo.AppAgentSkillSet s
WHERE s.AgentCode = N'app-builder'
  AND EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary l WHERE l.LibraryKey = N'platform-application')
  AND NOT EXISTS (
      SELECT 1 FROM dbo.AppAgentLibrarySubscription x
      WHERE x.AgentID = s.AgentID AND x.LibraryKey = N'platform-application');
GO

DELETE e
FROM dbo.AppAgentToolExclusion e
INNER JOIN dbo.AppAgentSkillSet s ON s.AgentID = e.AgentID
WHERE s.AgentCode = N'app-builder'
  AND e.LibraryKey = N'platform-application'
  AND e.ToolName IN (N'list_app_nav_menu', N'update_nav_menu_item', N'delete_nav_menu_item');
GO

-- create_search: optional criteriaFields + viewType at creation time
UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Create Dataset + SearchView (all SQL columns) + Search. Optional criteriaFields (comma-separated column/alias names) creates Filters. Optional viewType (GridView|CardView|… or int) sets representation. Always pass saasApplicationId. Report only CriteriaCreated / ViewType from the tool result.',
    ParameterSchemaJson = N'{
  "name": {"type":"string","description":"Search display name.","required":true},
  "sqlQuery": {"type":"string","description":"SQL SELECT for the dataset.","required":true},
  "saasApplicationId": {"type":"integer","description":"SaasApplicationId from create_app_package.","required":true},
  "criteriaFields": {"type":"string","description":"Optional comma-separated dataset column/alias names to create as Filters/Criteria, e.g. Customer,Title."},
  "viewType": {"type":"string","description":"Optional view type: GridView (default), CardView, PivotView, CalendarView, ChartView, or EmAppViewType int."}
}'
WHERE LibraryKey = N'platform-search' AND ToolName = N'create_search';
GO

-- delete_search: Search + View + Dataset (+ sidebar)
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-search' AND ToolName = N'delete_search')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-search', N'delete_search',
    N'Permanently delete a standalone Search. deleteView/deleteDataSet default true mean delete IF unshared — View/Dataset are kept when other Searches or SearchViews still reference them. Removes sidebar shortcuts when possible. Does NOT drop DB tables. ask_user / propose_plan first.',
    N'{
  "searchId": {"type":"integer","description":"Search id to delete.","required":true},
  "deleteView": {"type":"boolean","description":"Delete default SearchView only if unshared. Default true."},
  "deleteDataSet": {"type":"boolean","description":"Delete Dataset only if unshared by other Searches/Views. Default true."},
  "removeFromMenu": {"type":"boolean","description":"Remove sidebar shortcuts. Default true."}
}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AppBuilderAgent.Plugins.SearchBuilderPlugin","MethodName":"DeleteSearch"}',
    1, 5);
ELSE
UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Permanently delete a standalone Search. deleteView/deleteDataSet default true mean delete IF unshared — View/Dataset are kept when other Searches or SearchViews still reference them. Removes sidebar shortcuts when possible. Does NOT drop DB tables. ask_user / propose_plan first.',
    ParameterSchemaJson = N'{
  "searchId": {"type":"integer","description":"Search id to delete.","required":true},
  "deleteView": {"type":"boolean","description":"Delete default SearchView only if unshared. Default true."},
  "deleteDataSet": {"type":"boolean","description":"Delete Dataset only if unshared by other Searches/Views. Default true."},
  "removeFromMenu": {"type":"boolean","description":"Remove sidebar shortcuts. Default true."}
}'
WHERE LibraryKey = N'platform-search' AND ToolName = N'delete_search';
GO

-- Ensure app-builder can use delete_search (clear exclusion if any)
DELETE e
FROM dbo.AppAgentToolExclusion e
INNER JOIN dbo.AppAgentSkillSet s ON s.AgentID = e.AgentID
WHERE s.AgentCode = N'app-builder'
  AND e.LibraryKey = N'platform-search'
  AND e.ToolName = N'delete_search';
GO

-- search-view-config: exclude delete_search (configurator must not greenfield-delete)
DECLARE @SearchAgentId2 int;
SELECT @SearchAgentId2 = AgentID FROM dbo.AppAgentSkillSet WHERE AgentCode = N'app-search-view-config';
IF @SearchAgentId2 IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolExclusion WHERE AgentID=@SearchAgentId2 AND LibraryKey=N'platform-search' AND ToolName=N'delete_search')
AND EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey=N'platform-search' AND ToolName=N'delete_search')
    INSERT INTO dbo.AppAgentToolExclusion (AgentID, LibraryKey, ToolName) VALUES (@SearchAgentId2, N'platform-search', N'delete_search');
GO

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = SystemPrompt + N'

━━━ LEFT SIDEBAR MENU ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Add: add_search_to_menu / add_transaction_to_menu (or auto from create_search_view).
List: list_app_nav_menu(saasApplicationId) → menuId.
Update: update_nav_menu_item(menuId, name/sort/iconName).
Delete: delete_nav_menu_item(menuId) — one tool for all kinds; ask_user first.

━━━ create_search / delete_search ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Pass criteriaFields and viewType ON create_search when the user asks for filters / Card View.
NEVER claim Criteria or Card View unless tool result CriteriaCreated / ViewType match.
Fine-tune later via app-search-view-config (update_search_criteria, etc.).
Delete standalone Search (+ View + Dataset + menu by default): delete_search(searchId).
ask_user / propose_plan first. NEVER claim deleted unless IsSuccess=true.
'
WHERE AgentCode = N'app-builder'
  AND SystemPrompt NOT LIKE N'%delete_nav_menu_item%';
GO

-- Idempotent: DDL entity picker tool + prompt for already-seeded tenants
DECLARE @SearchPlugin2 nvarchar(300) = N'App.BL.AIAgent.GenericAgent.Plugins.SearchViewConfigPlugin';
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-search-view-config' AND ToolName = N'search_lookup_entities')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-search-view-config', N'search_lookup_entities',
    N'Search AppEntityInfo for DDL EntityId (EntityCode/TableName/Description). Prefer host SaasApplicationId. NEVER use SearchId as EntityId. ask_user with id=EntityId.',
    N'{"query":{"type":"string","required":true},"saasApplicationId":{"type":"integer"},"take":{"type":"integer"},"searchId":{"type":"integer"}}',
    N'BuiltIn', N'{"TypeName":"' + @SearchPlugin2 + N'","MethodName":"SearchLookupEntities"}', 1, 13);

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = SystemPrompt + N'

━━━ DDL EntityId (criteria) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
For DDL / SearchAbleDDL / AutoComplete criteria — ONE user confirmation only:
1) search_lookup_entities(query=Customer|Client|tableName)
2) ask_user optionsJson from entities[].askUserOption — id MUST be EntityId
3) On selection: immediately update_search_criteria(op=update, searchFieldId, changesJson={ControlType:1,EntityId:<chosen>}, confirmed=true) → request_search_reload
Entity pick IS the confirmation. Do NOT ask_user Yes/No again. Do NOT call update_search_criteria preview/dryRun after the pick.
NEVER offer SearchId / searchFieldId / list_available_searches as EntityId.
ControlType 6 is Grid, not DDL.

━━━ Criteria modify vs add ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
To change an existing filter: get_search_config → op=update + searchFieldId of that row.
NEVER op=add when SysTableFiledPath already exists (tool will reject duplicates).
'
WHERE AgentCode = N'app-search-view-config'
  AND SystemPrompt NOT LIKE N'%search_lookup_entities%';
GO

-- Idempotent: tighten DDL flow to single confirmation (already-seeded tenants)
UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = REPLACE(SystemPrompt,
N'C2) Criteria DDL Entity: search_lookup_entities(query) → ask_user options id=EntityId → update_search_criteria ControlType=1 + EntityId',
N'C2) Criteria DDL Entity (ONE confirm): search_lookup_entities → ask_user pick EntityId → immediately update_search_criteria(ControlType=1,EntityId,confirmed=true) → request_search_reload. Entity pick IS confirmation — no second Yes/No.')
WHERE AgentCode = N'app-search-view-config'
  AND SystemPrompt LIKE N'%C2) Criteria DDL Entity: search_lookup_entities(query)%';

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = REPLACE(SystemPrompt,
N'1) search_lookup_entities(query=Customer|Client|tableName)
2) ask_user optionsJson from entities[].askUserOption — id MUST be EntityId
3) update_search_criteria with ControlType=1 and EntityId=<chosen>
NEVER offer SearchId / searchFieldId / list_available_searches as EntityId.
ControlType 6 is Grid, not DDL.',
N'1) search_lookup_entities(query=Customer|Client|tableName)
2) ask_user optionsJson from entities[].askUserOption — id MUST be EntityId
3) On selection: immediately update_search_criteria(..., confirmed=true) → request_search_reload. Entity pick IS confirmation — no second Yes/No / no preview after pick.
NEVER offer SearchId / searchFieldId / list_available_searches as EntityId.
ControlType 6 is Grid, not DDL.')
WHERE AgentCode = N'app-search-view-config'
  AND SystemPrompt LIKE N'%3) update_search_criteria with ControlType=1 and EntityId=<chosen>%';

UPDATE dbo.AppAgentSkillSet
SET SystemPrompt = REPLACE(SystemPrompt,
N'✓ Always ask_user before durable writes.',
N'✓ Always ask_user before durable writes (DDL Entity pick counts — then confirmed=true immediately; never double-confirm).')
WHERE AgentCode = N'app-search-view-config'
  AND SystemPrompt LIKE N'%✓ Always ask_user before durable writes.%'
  AND SystemPrompt NOT LIKE N'%never double-confirm%';
GO
