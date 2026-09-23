-- V034: Generic Agent chat DisplayTitle + agent-files.execute_agent_sql_file.
-- Idempotent: safe to re-run (COL_LENGTH / IF NOT EXISTS).
--
-- Tracking note (AppTenantMigrationRunnerBL): Version = full filename without .sql
-- (e.g. V034__GenericAgentDisplayTitleAndExecuteSqlFile).

-- ─────────────────────────────────────────────────────────────────────────────
-- 1) DisplayTitle — auto-title from first user message; Rename locks this column
-- ─────────────────────────────────────────────────────────────────────────────
IF COL_LENGTH('dbo.AppGenericAgentSession', 'DisplayTitle') IS NULL
BEGIN
    ALTER TABLE dbo.AppGenericAgentSession
        ADD DisplayTitle NVARCHAR(200) NULL;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 2) execute_agent_sql_file — run output/*.sql from AgentOutput (GO batches)
--    Official PLM import SQL uses three-part names and DB_ID().
--    Requires V026 (agent-files).
-- ─────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'agent-files' AND ToolName = N'execute_agent_sql_file')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'agent-files',
    N'execute_agent_sql_file',
    N'Execute a .sql file from the current chat AgentOutput area (GO-batch). Connects to APP tenant (or dataSourceId). Cross-database three-part names work only when PLM/plmDW/ERP catalogs are on the SAME SQL Server and visible via DB_ID. Pass requiredDataSourceIds (comma-separated) to preflight same-server. Returns {ok,path,batches,durationMs,targetServer,targetCatalog,catalogs,error} — never the SQL body. Admin only.',
    N'{"type":"object","properties":{"relativePath":{"type":"string","description":"Relative .sql path under AgentOutput, e.g. output/3359/3_PlmDw_ImportFromDW.sql"},"dataSourceId":{"type":"integer","description":"Optional target DataSourceRegisterId. Omit or 0 = APP tenant / session default."},"requiredDataSourceIds":{"type":"string","description":"Optional comma-separated DataSourceIds (PLM, plmDW, ERP) that the script will three-part-name. Must be the same SQL Server as the target."}},"required":["relativePath"]}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentFilePlugin","MethodName":"ExecuteSqlFile"}',
    1,
    5
);
GO
