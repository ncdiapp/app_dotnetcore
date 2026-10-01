-- V040: Platform BuiltIn tenant catalog tools (APP-wide).
-- Idempotent. PLM domain / library moves live in TenantAgentSeeds, not here.

-- platform-database: list_tenant_data_sources
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-database' AND ToolName = N'list_tenant_data_sources')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-database',
    N'list_tenant_data_sources',
    N'List DataSourceRegisterId + name + databaseName for the current tenant company. Use for ask_user DDL to pick registers. Never returns connection strings.',
    N'{"type":"object","properties":{"targetCompanyId":{"type":"integer","description":"Optional target company id"}}}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.TenantCatalogPlugin","MethodName":"ListTenantDataSources"}',
    1,
    50
);
GO

-- platform-database: test_data_source_connection
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-database' AND ToolName = N'test_data_source_connection')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-database',
    N'test_data_source_connection',
    N'Test a tenant AppDataSourceRegister by id (open SQL connection). Never pass a connection string. Returns isSuccess, dataSourceName, databaseName, serverVersion.',
    N'{"type":"object","properties":{"dataSourceRegisterId":{"type":"integer","description":"Tenant AppDataSourceRegister id"},"targetCompanyId":{"type":"integer","description":"Optional target company id"}},"required":["dataSourceRegisterId"]}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.TenantCatalogPlugin","MethodName":"TestDataSourceConnection"}',
    1,
    51
);
GO

-- platform-application: list_tenant_saas_applications
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-application' AND ToolName = N'list_tenant_saas_applications')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-application',
    N'list_tenant_saas_applications',
    N'List SaasApplicationId + ApplicationName (slim). Prefer over list_applications when the agent only needs package pick (no TX/Search tree).',
    N'{"type":"object","properties":{"targetCompanyId":{"type":"integer","description":"Optional target company id"}}}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.TenantCatalogPlugin","MethodName":"ListTenantSaasApplications"}',
    1,
    50
);
GO
