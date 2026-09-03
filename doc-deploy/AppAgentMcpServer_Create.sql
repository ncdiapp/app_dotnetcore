-- ============================================================
-- AppAgentMcpServer_Create.sql
-- Phase 0 — Generic Agent Refactor
-- Creates AppAgentMcpServer table.
-- MCP server tools are auto-discovered at session start via McpClient.CreateAsync
-- + HttpClientTransport (streamable-http) or StdioClientTransport (stdio).
-- Run once per tenant DB. Idempotent (checks existence before CREATE/INSERT).
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AppAgentMcpServer' AND type = 'U')
BEGIN
    CREATE TABLE dbo.AppAgentMcpServer (
        McpServerId   INT           IDENTITY(1,1) PRIMARY KEY,
        SkillKey      NVARCHAR(100) NOT NULL,       -- FK to AppAgentSkillSet.SkillKey
        ServerName    NVARCHAR(200) NOT NULL,       -- display name; used as SK plugin group name prefix (mcp_<ServerName>)
        ServerType    NVARCHAR(50)  NOT NULL,       -- 'streamable-http' | 'stdio'
        ServerUrl     NVARCHAR(500) NULL,           -- streamable-http: full HTTP endpoint URL
        Command       NVARCHAR(500) NULL,           -- stdio: executable path + args (space-separated)
        IsActive      BIT           NOT NULL DEFAULT 1
    );
END
GO

-- ----------------------------------------------------------------
-- Transport notes for GenericAgentEngine (Phase 3 implementation):
--
--   streamable-http:
--     var transport = new HttpClientTransport(new HttpClientTransportOptions
--     {
--         Url           = server.ServerUrl,
--         TransportMode = HttpTransportMode.StreamableHttp
--     });
--
--   stdio:
--     var parts = server.Command.Split(' ');
--     var transport = new StdioClientTransport(new StdioClientTransportOptions
--     {
--         Command   = parts[0],
--         Arguments = parts.Skip(1).ToArray()
--     });
--
--   Both: var mcpClient = await McpClient.CreateAsync(transport);
--   Do NOT use AsKernelFunction() — wrap tools manually via KernelFunctionFactory.CreateFromMethod.
--   Reference: BC-MCP-Client\server\McpChatAgent.Api\Services\McpPluginFactory.cs
-- ----------------------------------------------------------------

-- ----------------------------------------------------------------
-- Example seed row — BlueCherry ERP MCP server for app-builder.
-- Uncomment and adjust URL when deploying.
-- ----------------------------------------------------------------
-- IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentMcpServer WHERE SkillKey='app-builder' AND ServerName='BlueCherry ERP MCP')
-- INSERT INTO dbo.AppAgentMcpServer (SkillKey, ServerName, ServerType, ServerUrl) VALUES (
--     'app-builder',
--     'BlueCherry ERP MCP',
--     'streamable-http',
--     'http://localhost:5100/mcp'
-- );
GO
