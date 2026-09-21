-- TENANT / CUSTOMER seed — NOT a schema migration.
-- Do NOT run via Flyway/generic structure upgrade.
-- Apply manually on a tenant that needs PLM Integration agents (e.g. TenantDB_PLM32).
--
-- Library: integration-plm-import
-- All agent tools are ExternalDll (APP.AgentPlugins.PlmImport.dll).
-- PlmImportEngine lives in the plugin (APP.BL/DataMigration/PlmMigration removed).
-- Orchestrator: Seed_PlmIntegrationOrchestrator.sql (SkillKey plm-integration-orchestrator).
-- Progress: AppReact/ImportDoc/PlmAgentIntegration/Agent-Replace-Wizard-Progress.md
-- E2E: AppReact/ImportDoc/PlmAgentIntegration/Interactive-E2E-Checklist.md

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'integration-plm-import')
INSERT INTO dbo.AppAgentToolLibrary
    (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'integration-plm-import',
    N'platform',
    N'PLM Integration Import',
    N'PLM Data Import tools for Agent (Connect/Entity/Image/Folder/Color/POM/DW/Search). Prefer ExternalDll + sessionId. Subscribe plm-integration-orchestrator.',
    N'BuiltIn',
    1
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'test_plm_connection')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'test_plm_connection',
    N'Test a tenant AppDataSourceRegister by id (PLM/PLMDW/ERP). Never pass a connection string. Returns IsSuccess, DataSourceName, DatabaseName, ServerVersion.',
    N'{"type":"object","properties":{"dataSourceRegisterId":{"type":"integer","description":"Tenant AppDataSourceRegister id"},"targetCompanyId":{"type":"integer","description":"Optional target company id (SysAdmin)"}},"required":["dataSourceRegisterId"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.TestPlmConnectionTool"}',
    1,
    10
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'list_tenant_data_sources')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'list_tenant_data_sources',
    N'List DataSourceRegisterId + name + databaseName for the current tenant company. Use for ask_user to pick PLM / PLMDW / ERP registers. Never returns connection strings.',
    N'{"type":"object","properties":{"targetCompanyId":{"type":"integer"}}}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.ListTenantDataSourcesTool"}',
    1,
    15
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'discover_plm_data_sources')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'discover_plm_data_sources',
    N'DISABLED for security. Use list_tenant_data_sources + save_plm_import_session with register ids. Do not pass connection strings.',
    N'{"type":"object","properties":{}}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.DiscoverPlmDataSourcesTool"}',
    0,
    20
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'get_plm_import_session')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'get_plm_import_session',
    N'Get the active PLM Import session (register ids + status). Never returns connection strings.',
    N'{"type":"object","properties":{"targetCompanyId":{"type":"integer"}}}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.GetPlmImportSessionTool"}',
    1,
    30
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'save_plm_import_session')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'save_plm_import_session',
    N'Save / upsert PLM Import session. Pass saasApplicationId + plmDataSourceRegisterId (required) and optional plmDwDataSourceRegisterId / erpDataSourceRegisterId. Never pass connection strings.',
    N'{"type":"object","properties":{"saasApplicationId":{"type":"integer"},"plmDataSourceRegisterId":{"type":"integer","description":"Tenant register id for PLM"},"plmDwDataSourceRegisterId":{"type":"integer"},"erpDataSourceRegisterId":{"type":"integer"},"sessionId":{"type":"integer"},"sessionJson":{"type":"string","description":"Optional full PlmImportSessionDto JSON without connection strings"},"targetCompanyId":{"type":"integer"}},"required":["saasApplicationId","plmDataSourceRegisterId"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.SavePlmImportSessionTool"}',
    1,
    40
);
GO

-- Image Import (tblSketch → AppFile) — BuiltIn wrappers; later ExternalDll for PLM-read slice.
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'preview_plm_sketch_import')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'preview_plm_sketch_import',
    N'Preview PLM tblSketch → AppFile import. Returns counts. Requires sessionId from Connect.',
    N'{"type":"object","properties":{"sessionId":{"type":"integer","description":"AppPlmImportSession.SessionId"}},"required":["sessionId"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.PreviewSketchImportTool"}',
    1,
    50
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'execute_plm_sketch_import')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'execute_plm_sketch_import',
    N'Start background job importing tblSketch into AppFile (FileID=SketchID). Poll with get_plm_import_job.',
    N'{"type":"object","properties":{"sessionId":{"type":"integer"}},"required":["sessionId"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.ExecuteSketchImportTool"}',
    1,
    60
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'get_plm_import_job')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'get_plm_import_job',
    N'Poll PLM import job status / progress / ResultJson.',
    N'{"type":"object","properties":{"jobId":{"type":"integer"}},"required":["jobId"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.GetPlmImportJobTool"}',
    1,
    70
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'cancel_plm_import_job')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'cancel_plm_import_job',
    N'Request cancellation of a running PLM import job.',
    N'{"type":"object","properties":{"jobId":{"type":"integer"}},"required":["jobId"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.CancelPlmImportJobTool"}',
    1,
    80
);
GO

-- Phase B: ExternalDll PLM-read preview (connection strings). Host BuiltIn preview_plm_sketch_import calls this internally.
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'plm_build_sketch_import_preview')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'plm_build_sketch_import_preview',
    N'ExternalDll: build tblSketch→AppFile preview counts from raw PLM + tenant connection strings (no session). Prefer preview_plm_sketch_import for agents.',
    N'{"type":"object","properties":{"plmConnectionString":{"type":"string"},"tenantConnectionString":{"type":"string"}},"required":["plmConnectionString","tenantConnectionString"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.SketchPreviewTool"}',
    1,
    55
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'plm_export_sketches_to_staging')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'plm_export_sketches_to_staging',
    N'ExternalDll (B2): export tblSketch binaries to a staging folder. Prefer execute_plm_sketch_import (Host job) for production imports.',
    N'{"type":"object","properties":{"plmConnectionString":{"type":"string"},"stagingDirectory":{"type":"string"},"skipFileIdsCsv":{"type":"string","description":"Optional comma-separated AppFile FileIDs to skip"}},"required":["plmConnectionString","stagingDirectory"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.SketchExportTool"}',
    1,
    56
);
GO

-- Entity Import (TableExport / SystemDefine / UserDefine) — BuiltIn wrappers; TableExport plan → ExternalDll.
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'preview_plm_table_export_plan')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'preview_plm_table_export_plan',
    N'Preview System Define physical table export plan (pdmEntity → tenant prefixed tables). Requires sessionId.',
    N'{"type":"object","properties":{"sessionId":{"type":"integer","description":"AppPlmImportSession.SessionId"}},"required":["sessionId"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.PreviewTableExportPlanTool"}',
    1,
    90
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'execute_plm_table_export')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'execute_plm_table_export',
    N'Start background job copying System Define PLM tables into tenant DB. Poll with get_plm_import_job.',
    N'{"type":"object","properties":{"sessionId":{"type":"integer"}},"required":["sessionId"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.ExecuteTableExportTool"}',
    1,
    100
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'preview_system_define_entity_import')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'preview_system_define_entity_import',
    N'Preview System Define entity metadata import (pdmEntity DataSourceFrom=1). Requires sessionId.',
    N'{"type":"object","properties":{"sessionId":{"type":"integer"}},"required":["sessionId"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.PreviewSystemDefineEntityImportTool"}',
    1,
    110
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'execute_system_define_entity_import')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'execute_system_define_entity_import',
    N'Start background job importing System Define entities. Poll with get_plm_import_job.',
    N'{"type":"object","properties":{"sessionId":{"type":"integer"}},"required":["sessionId"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.ExecuteSystemDefineEntityImportTool"}',
    1,
    120
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'preview_user_define_entity_import')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'preview_user_define_entity_import',
    N'Preview User Define / entity-wide metadata import. Requires sessionId.',
    N'{"type":"object","properties":{"sessionId":{"type":"integer"}},"required":["sessionId"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.PreviewUserDefineEntityImportTool"}',
    1,
    130
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'execute_user_define_entity_import')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'execute_user_define_entity_import',
    N'Start background job importing User Define entities. Poll with get_plm_import_job.',
    N'{"type":"object","properties":{"sessionId":{"type":"integer"}},"required":["sessionId"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.ExecuteUserDefineEntityImportTool"}',
    1,
    140
);
GO

-- Phase C-B: ExternalDll table-export plan (connection string + tablePrefix). Host BuiltIn preview_plm_table_export_plan calls this internally.
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'plm_build_table_export_plan')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'plm_build_table_export_plan',
    N'ExternalDll: build System Define table-export plan from raw PLM connection + tablePrefix (no session). Prefer preview_plm_table_export_plan for agents.',
    N'{"type":"object","properties":{"plmConnectionString":{"type":"string"},"tablePrefix":{"type":"string"}},"required":["plmConnectionString"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.TableExportPlanTool"}',
    1,
    95
);
GO

-- Phase C-C: System / User Define preview ExternalDll tools.
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'plm_build_system_define_entity_preview')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'plm_build_system_define_entity_preview',
    N'ExternalDll: System Define entity preview from PLM + tenant connections + host-resolved dataSourceMapsJson. Prefer preview_system_define_entity_import for agents.',
    N'{"type":"object","properties":{"plmConnectionString":{"type":"string"},"tenantConnectionString":{"type":"string"},"tablePrefix":{"type":"string"},"dataSourceMapsJson":{"type":"string"}},"required":["plmConnectionString","tenantConnectionString","dataSourceMapsJson"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.SystemDefineEntityPreviewTool"}',
    1,
    115
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'plm_build_user_define_entity_preview')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'plm_build_user_define_entity_preview',
    N'ExternalDll: User Define entity preview from PLM + tenant connections + tenantDatabaseName. Prefer preview_user_define_entity_import for agents.',
    N'{"type":"object","properties":{"plmConnectionString":{"type":"string"},"tenantConnectionString":{"type":"string"},"tenantDatabaseName":{"type":"string"},"entityWideTablePrefix":{"type":"string"}},"required":["plmConnectionString","tenantConnectionString","tenantDatabaseName"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.UserDefineEntityPreviewTool"}',
    1,
    135
);
GO

-- Phase C-D: TableExport physical copy ExternalDll (prefer Host job execute_plm_table_export).
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'plm_export_tables_to_tenant')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'plm_export_tables_to_tenant',
    N'ExternalDll (C-D): copy System Define PLM tables into tenant. Prefer execute_plm_table_export (Host job) for production.',
    N'{"type":"object","properties":{"plmConnectionString":{"type":"string"},"tenantConnectionString":{"type":"string"},"tablePrefix":{"type":"string"}},"required":["plmConnectionString","tenantConnectionString"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.TableExportTool"}',
    1,
    105
);
GO

-- ---------------------------------------------------------------------------
-- Phase 0/1: ExternalDll session ops + Folder / Color / Pom / Dw / Search
-- (wrappers call PlmMigrationBL during transition; agents prefer these over Wizard)
-- ---------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'discard_plm_import_session')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'discard_plm_import_session',
    N'Discard (complete) the PLM import session. Optional sessionId; else active session for company.',
    N'{"type":"object","properties":{"sessionId":{"type":"integer"},"targetCompanyId":{"type":"integer"}}}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.DiscardSessionTool"}',
    1,
    85
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'get_plm_import_log')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'get_plm_import_log',
    N'Read AppPlmImportLog rows for a session (newest first).',
    N'{"type":"object","properties":{"sessionId":{"type":"integer"},"targetCompanyId":{"type":"integer"}}}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.GetImportLogTool"}',
    1,
    86
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'preview_plm_folder_import')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'preview_plm_folder_import',
    N'Preview PLM folder tree + color-folder link import. Requires sessionId from Connect.',
    N'{"type":"object","properties":{"sessionId":{"type":"integer"}},"required":["sessionId"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.PreviewFolderImportTool"}',
    1,
    150
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'execute_plm_folder_import')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'execute_plm_folder_import',
    N'Start background job importing PLM folders. Poll with get_plm_import_job.',
    N'{"type":"object","properties":{"sessionId":{"type":"integer"}},"required":["sessionId"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.ExecuteFolderImportTool"}',
    1,
    151
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'preview_plm_folder_placement')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'preview_plm_folder_placement',
    N'Preview PLM folder placement / anchor mapping. Requires sessionId.',
    N'{"type":"object","properties":{"sessionId":{"type":"integer"}},"required":["sessionId"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.PreviewFolderPlacementTool"}',
    1,
    152
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'execute_plm_folder_placement')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'execute_plm_folder_placement',
    N'Start background job for folder placement. Poll with get_plm_import_job.',
    N'{"type":"object","properties":{"sessionId":{"type":"integer"}},"required":["sessionId"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.ExecuteFolderPlacementTool"}',
    1,
    153
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'preview_plm_color_import')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'preview_plm_color_import',
    N'Preview PLM RGB color transaction / search import. Requires sessionId.',
    N'{"type":"object","properties":{"sessionId":{"type":"integer"}},"required":["sessionId"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.PreviewColorImportTool"}',
    1,
    160
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'execute_plm_color_import')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'execute_plm_color_import',
    N'Execute PLM color import (sync). Pass sessionId; optional saasApplicationId.',
    N'{"type":"object","properties":{"sessionId":{"type":"integer"},"saasApplicationId":{"type":"integer"}},"required":["sessionId"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.ExecuteColorImportTool"}',
    1,
    161
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'preview_plm_pom_import')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'preview_plm_pom_import',
    N'Preview PLM POM / body-part import. Requires sessionId.',
    N'{"type":"object","properties":{"sessionId":{"type":"integer"}},"required":["sessionId"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.PreviewPomImportTool"}',
    1,
    170
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'execute_plm_pom_import')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'execute_plm_pom_import',
    N'Execute PLM POM import (sync). Optional importJunctionTables / importFoldersIfMissing (default true).',
    N'{"type":"object","properties":{"sessionId":{"type":"integer"},"saasApplicationId":{"type":"integer"},"importJunctionTables":{"type":"boolean"},"importFoldersIfMissing":{"type":"boolean"}},"required":["sessionId"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.ExecutePomImportTool"}',
    1,
    171
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'load_dw_import_blueprint')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'load_dw_import_blueprint',
    N'Parse / validate DW Import blueprintJson string into structured blueprint.',
    N'{"type":"object","properties":{"blueprintJson":{"type":"string"},"tablePrefix":{"type":"string"}},"required":["blueprintJson"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.LoadDwBlueprintTool"}',
    1,
    180
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'load_dw_blueprint_from_table')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'load_dw_blueprint_from_table',
    N'Load DW blueprint from tenant {tablePrefix}ImportBlueprint table by blueprintKey (default default).',
    N'{"type":"object","properties":{"tablePrefix":{"type":"string"},"blueprintKey":{"type":"string"}}}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.LoadDwBlueprintFromTableTool"}',
    1,
    181
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'preview_dw_blueprint_config')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'preview_dw_blueprint_config',
    N'Preview DW blueprint apply plan. Pass full blueprintJson.',
    N'{"type":"object","properties":{"blueprintJson":{"type":"string"}},"required":["blueprintJson"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.PreviewDwBlueprintTool"}',
    1,
    182
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'execute_dw_blueprint_config')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'execute_dw_blueprint_config',
    N'Execute DW blueprint. mode=Insert|Update|Repair. Pass blueprintJson + optional saasApplicationId.',
    N'{"type":"object","properties":{"blueprintJson":{"type":"string"},"saasApplicationId":{"type":"integer"},"mode":{"type":"string"},"includeSearchView":{"type":"boolean"},"includeNavigation":{"type":"boolean"},"includeTransactionGroup":{"type":"boolean"}},"required":["blueprintJson"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.ExecuteDwBlueprintTool"}',
    1,
    183
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'load_search_import_blueprint')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'load_search_import_blueprint',
    N'Parse Search Import blueprintJson.',
    N'{"type":"object","properties":{"blueprintJson":{"type":"string"}},"required":["blueprintJson"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.LoadSearchBlueprintTool"}',
    1,
    190
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'preview_search_blueprint_config')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'preview_search_blueprint_config',
    N'Preview Search Import blueprint apply plan. Pass blueprintJson.',
    N'{"type":"object","properties":{"blueprintJson":{"type":"string"}},"required":["blueprintJson"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.PreviewSearchBlueprintTool"}',
    1,
    191
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'execute_search_blueprint_config')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'execute_search_blueprint_config',
    N'Execute Search Import blueprint. Pass blueprintJson + optional saasApplicationId.',
    N'{"type":"object","properties":{"blueprintJson":{"type":"string"},"saasApplicationId":{"type":"integer"}},"required":["blueprintJson"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.ExecuteSearchBlueprintTool"}',
    1,
    192
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'preview_search_sibling_view')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'preview_search_sibling_view',
    N'Preview Search Sibling View blueprint (Option A enrich dataset). Pass blueprintJson.',
    N'{"type":"object","properties":{"blueprintJson":{"type":"string"}},"required":["blueprintJson"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.PreviewSearchSiblingViewTool"}',
    1,
    193
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'execute_search_sibling_view')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'execute_search_sibling_view',
    N'Execute Search Sibling View blueprint. Pass blueprintJson + optional saasApplicationId.',
    N'{"type":"object","properties":{"blueprintJson":{"type":"string"},"saasApplicationId":{"type":"integer"}},"required":["blueprintJson"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.ExecuteSearchSiblingViewTool"}',
    1,
    194
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'preview_search_massupdate_view')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'preview_search_massupdate_view',
    N'Preview Search MassUpdate View blueprint. Pass blueprintJson.',
    N'{"type":"object","properties":{"blueprintJson":{"type":"string"}},"required":["blueprintJson"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.PreviewSearchMassUpdateViewTool"}',
    1,
    195
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'execute_search_massupdate_view')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'execute_search_massupdate_view',
    N'Execute Search MassUpdate View blueprint. Pass blueprintJson + optional saasApplicationId.',
    N'{"type":"object","properties":{"blueprintJson":{"type":"string"},"saasApplicationId":{"type":"integer"}},"required":["blueprintJson"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.ExecuteSearchMassUpdateViewTool"}',
    1,
    196
);
GO

-- Phase 3b: upgrade any previously seeded BuiltIn Connect/Entity/Image tools to ExternalDll
-- (IF NOT EXISTS inserts above skip when rows already exist).
UPDATE t
SET ToolType = N'ExternalDll',
    ToolConfig = v.ToolConfig
FROM dbo.AppAgentLibraryTool t
INNER JOIN (VALUES
    (N'test_plm_connection', N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.TestPlmConnectionTool"}'),
    (N'discover_plm_data_sources', N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.DiscoverPlmDataSourcesTool"}'),
    (N'get_plm_import_session', N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.GetPlmImportSessionTool"}'),
    (N'save_plm_import_session', N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.SavePlmImportSessionTool"}'),
    (N'preview_plm_sketch_import', N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.PreviewSketchImportTool"}'),
    (N'execute_plm_sketch_import', N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.ExecuteSketchImportTool"}'),
    (N'get_plm_import_job', N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.GetPlmImportJobTool"}'),
    (N'cancel_plm_import_job', N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.CancelPlmImportJobTool"}'),
    (N'preview_plm_table_export_plan', N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.PreviewTableExportPlanTool"}'),
    (N'execute_plm_table_export', N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.ExecuteTableExportTool"}'),
    (N'preview_system_define_entity_import', N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.PreviewSystemDefineEntityImportTool"}'),
    (N'execute_system_define_entity_import', N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.ExecuteSystemDefineEntityImportTool"}'),
    (N'preview_user_define_entity_import', N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.PreviewUserDefineEntityImportTool"}'),
    (N'execute_user_define_entity_import', N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.ExecuteUserDefineEntityImportTool"}')
) v(ToolName, ToolConfig) ON t.ToolName = v.ToolName
WHERE t.LibraryKey = N'integration-plm-import'
  AND (t.ToolType <> N'ExternalDll' OR ISNULL(t.ToolConfig, N'') <> v.ToolConfig);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'build_dw_app_config_pack')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'build_dw_app_config_pack',
    N'Convert PLM DW Import Blueprint JSON into platform AppConfigPack JSON (Transaction/Search/Menu). Review then execute via preview/execute_dw_blueprint_config or AppConfigPack Import Config.',
    N'{"type":"object","properties":{"blueprintJson":{"type":"string"},"mode":{"type":"string"},"saasApplicationId":{"type":"integer"},"includeSearchView":{"type":"boolean"},"includeNavigation":{"type":"boolean"},"includeTransactionGroup":{"type":"boolean"}},"required":["blueprintJson"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.BuildDwAppConfigPackTool"}',
    1,
    184
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'build_search_app_config_pack')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'build_search_app_config_pack',
    N'Convert PLM Search Import Blueprint JSON into platform AppConfigPack JSON. Prefer execute_search_blueprint_config which applies via AppConfigPackBL.',
    N'{"type":"object","properties":{"blueprintJson":{"type":"string"},"saasApplicationId":{"type":"integer"}},"required":["blueprintJson"]}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.BuildSearchAppConfigPackTool"}',
    1,
    197
);
GO

-- Optional: subscribe PLM Integration Orchestrator when present (idempotent).
IF EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet WHERE SkillKey = N'plm-integration-orchestrator')
AND NOT EXISTS (
    SELECT 1 FROM dbo.AppAgentLibrarySubscription
    WHERE SkillKey = N'plm-integration-orchestrator' AND LibraryKey = N'integration-plm-import')
INSERT INTO dbo.AppAgentLibrarySubscription (SkillKey, LibraryKey)
VALUES (N'plm-integration-orchestrator', N'integration-plm-import');
GO

-- Security: Connect via DataSourceRegisterId only (upgrade existing tenant rows).
UPDATE dbo.AppAgentLibraryTool SET
    ToolDescription = N'Test a tenant AppDataSourceRegister by id (PLM/PLMDW/ERP). Never pass a connection string. Returns IsSuccess, DataSourceName, DatabaseName, ServerVersion.',
    ParameterSchemaJson = N'{"type":"object","properties":{"dataSourceRegisterId":{"type":"integer","description":"Tenant AppDataSourceRegister id"},"targetCompanyId":{"type":"integer"}},"required":["dataSourceRegisterId"]}',
    ToolType = N'ExternalDll',
    ToolConfig = N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.TestPlmConnectionTool"}',
    IsActive = 1
WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'test_plm_connection';
GO

UPDATE dbo.AppAgentLibraryTool SET
    ToolDescription = N'DISABLED for security. Use list_tenant_data_sources + save_plm_import_session with register ids. Do not pass connection strings.',
    ParameterSchemaJson = N'{"type":"object","properties":{}}',
    IsActive = 0
WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'discover_plm_data_sources';
GO

UPDATE dbo.AppAgentLibraryTool SET
    ToolDescription = N'Save / upsert PLM Import session. Pass saasApplicationId + plmDataSourceRegisterId (required) and optional plmDwDataSourceRegisterId / erpDataSourceRegisterId. Never pass connection strings.',
    ParameterSchemaJson = N'{"type":"object","properties":{"saasApplicationId":{"type":"integer"},"plmDataSourceRegisterId":{"type":"integer"},"plmDwDataSourceRegisterId":{"type":"integer"},"erpDataSourceRegisterId":{"type":"integer"},"sessionId":{"type":"integer"},"sessionJson":{"type":"string"},"targetCompanyId":{"type":"integer"}},"required":["saasApplicationId","plmDataSourceRegisterId"]}',
    ToolType = N'ExternalDll',
    ToolConfig = N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.SavePlmImportSessionTool"}',
    IsActive = 1
WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'save_plm_import_session';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'list_tenant_data_sources')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'list_tenant_data_sources',
    N'List DataSourceRegisterId + name + databaseName for the current tenant company. Use for ask_user to pick PLM / PLMDW / ERP registers. Never returns connection strings.',
    N'{"type":"object","properties":{"targetCompanyId":{"type":"integer"}}}',
    N'ExternalDll',
    N'{"AssemblyName":"APP.AgentPlugins.PlmImport.dll","TypeName":"APP.AgentPlugins.PlmImport.ListTenantDataSourcesTool"}',
    1,
    15
);
GO
