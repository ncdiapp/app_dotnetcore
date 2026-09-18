-- TENANT / CUSTOMER seed — NOT a schema migration.
-- Do NOT run via Flyway/generic structure upgrade.
-- Apply manually on a tenant that needs PLM Integration agents (e.g. TenantDB_PLM32).
--
-- Library: integration-plm-import (Phase 1 BuiltIn wrappers of PlmMigrationBL).
-- Wizard Connect/Discover and GenericAgent share AppAgentToolEngine.Dispatch.

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'integration-plm-import')
INSERT INTO dbo.AppAgentToolLibrary
    (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'integration-plm-import',
    N'platform',
    N'BuiltIn: PLM Data Import',
    N'PLM Data Import Connect & Discover (and later Entity/DW/Search/Folder/Color/POM/Image). Subscribe orchestrator or import workers. Phase 1 = BuiltIn wrappers of PlmMigrationBL; Phase 2+ = ExternalDll.',
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
    N'Test a legacy PLM SQL Server connection string. Returns IsSuccess, ServerVersion, DatabaseName.',
    N'{"type":"object","properties":{"connectionString":{"type":"string","description":"PLM SQL Server connection string"},"targetCompanyId":{"type":"integer","description":"Optional target company id"}},"required":["connectionString"]}',
    N'BuiltIn',
    N'{"TypeName":"APP.BL.DataMigration.PlmMigration.PlmImportConnectPlugin","MethodName":"TestPlmConnection"}',
    1,
    10
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'discover_plm_data_sources')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'discover_plm_data_sources',
    N'Connect to PLM and discover pdmDataSource rows (PLM/ERP/DW/…). May register tenant data sources. Requires admin.',
    N'{"type":"object","properties":{"plmConnectionString":{"type":"string"},"saasApplicationId":{"type":"integer"},"targetCompanyId":{"type":"integer"},"sessionId":{"type":"integer"}},"required":["plmConnectionString"]}',
    N'BuiltIn',
    N'{"TypeName":"APP.BL.DataMigration.PlmMigration.PlmImportConnectPlugin","MethodName":"DiscoverPlmDataSources"}',
    1,
    20
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'integration-plm-import' AND ToolName = N'get_plm_import_session')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'integration-plm-import',
    N'get_plm_import_session',
    N'Get the active PLM Data Import session for the current (or target) company.',
    N'{"type":"object","properties":{"targetCompanyId":{"type":"integer"}}}',
    N'BuiltIn',
    N'{"TypeName":"APP.BL.DataMigration.PlmMigration.PlmImportConnectPlugin","MethodName":"GetPlmImportSession"}',
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
    N'Save / upsert the PLM Data Import wizard session. Pass sessionJson as PlmImportSessionDto JSON.',
    N'{"type":"object","properties":{"sessionJson":{"type":"string","description":"JSON of PlmImportSessionDto"}},"required":["sessionJson"]}',
    N'BuiltIn',
    N'{"TypeName":"APP.BL.DataMigration.PlmMigration.PlmImportConnectPlugin","MethodName":"SavePlmImportSession"}',
    1,
    40
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
