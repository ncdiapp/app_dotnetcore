-- V036: Auth settings for AppAgentMcpServer (streamable-http).
--   BearerTokenEnvVar : NAME of an env var whose value is sent as "Authorization: Bearer <value>"
--   Headers           : JSON object of static headers            {"HeaderName":"value"}
--   HeadersFromEnv    : JSON object of header -> env var name    {"HeaderName":"ENV_VAR_NAME"}

IF COL_LENGTH(N'dbo.AppAgentMcpServer', N'BearerTokenEnvVar') IS NULL
    ALTER TABLE dbo.AppAgentMcpServer ADD BearerTokenEnvVar NVARCHAR(200) NULL;
GO

IF COL_LENGTH(N'dbo.AppAgentMcpServer', N'Headers') IS NULL
    ALTER TABLE dbo.AppAgentMcpServer ADD Headers NVARCHAR(MAX) NULL;
GO

IF COL_LENGTH(N'dbo.AppAgentMcpServer', N'HeadersFromEnv') IS NULL
    ALTER TABLE dbo.AppAgentMcpServer ADD HeadersFromEnv NVARCHAR(MAX) NULL;
GO
