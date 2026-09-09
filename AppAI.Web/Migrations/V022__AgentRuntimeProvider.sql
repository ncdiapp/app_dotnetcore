-- V022: Per-agent RuntimeProvider + rename Cursor Cloud Agent session table
-- RuntimeProvider: OpenAI | Gemini | Anthropic | CursorCloudAgents

IF COL_LENGTH(N'dbo.AppAgentSkillSet', N'RuntimeProvider') IS NULL
BEGIN
    ALTER TABLE dbo.AppAgentSkillSet
        ADD RuntimeProvider NVARCHAR(40) NULL;
END
GO

-- Empty / NULL means: follow tenant Application Settings default provider at runtime.
IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.AppAgentSkillSet')
      AND name = N'RuntimeProvider'
      AND is_nullable = 0
)
BEGIN
    ALTER TABLE dbo.AppAgentSkillSet ALTER COLUMN RuntimeProvider NVARCHAR(40) NULL;
END
GO

IF OBJECT_ID(N'dbo.DF_AppAgentSkillSet_RuntimeProvider', N'D') IS NOT NULL
BEGIN
    ALTER TABLE dbo.AppAgentSkillSet DROP CONSTRAINT DF_AppAgentSkillSet_RuntimeProvider;
END
GO

-- Agents that used CapabilityFlags ExternalBackend (64) → Cursor Cloud Agents runtime
UPDATE dbo.AppAgentSkillSet
SET RuntimeProvider = N'CursorCloudAgents'
WHERE (CapabilityFlags & 64) = 64
  AND (RuntimeProvider IS NULL OR LTRIM(RTRIM(RuntimeProvider)) = N'' OR RuntimeProvider = N'Gemini');
GO

-- Physical rename (user-approved); keep FK-free session table only
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
