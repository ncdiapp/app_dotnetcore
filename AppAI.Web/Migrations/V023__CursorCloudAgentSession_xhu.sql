-- V022: Rename Cursor Cloud Agent session table (standalone DI / CursorCloudAgent path).
-- Agent Management does NOT use per-agent RuntimeProvider; LLM comes from tenant Application Settings.

-- Physical rename (FK-free session table only)
IF OBJECT_ID(N'dbo.AppDataIntegrationAgentSession', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.CursorCloudAgentSession', N'U') IS NULL
    EXEC sp_rename N'dbo.AppDataIntegrationAgentSession', N'CursorCloudAgentSession';
GO

IF OBJECT_ID(N'dbo.PK_AppDataIntegrationAgentSession', N'PK') IS NOT NULL
   AND OBJECT_ID(N'dbo.PK_CursorCloudAgentSession', N'PK') IS NULL
    EXEC sp_rename N'dbo.PK_AppDataIntegrationAgentSession', N'PK_CursorCloudAgentSession';
GO

IF OBJECT_ID(N'dbo.DF_AppDataIntegrationAgentSession_IsArchived', N'D') IS NOT NULL
   AND OBJECT_ID(N'dbo.DF_CursorCloudAgentSession_IsArchived', N'D') IS NULL
    EXEC sp_rename N'dbo.DF_AppDataIntegrationAgentSession_IsArchived', N'DF_CursorCloudAgentSession_IsArchived';
GO

IF OBJECT_ID(N'dbo.DF_AppDataIntegrationAgentSession_SortOrder', N'D') IS NOT NULL
   AND OBJECT_ID(N'dbo.DF_CursorCloudAgentSession_SortOrder', N'D') IS NULL
    EXEC sp_rename N'dbo.DF_AppDataIntegrationAgentSession_SortOrder', N'DF_CursorCloudAgentSession_SortOrder';
GO
