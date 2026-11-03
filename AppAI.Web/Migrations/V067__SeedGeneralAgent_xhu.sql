-- V067: General Agent is built-in, not an AppAgentSkillSet row.
-- Session (and episode) AgentID is NULL for those chats. FK still applies to specialists.
-- If an earlier draft seeded __general_agent__, remap then delete that stub.

IF OBJECT_ID(N'dbo.AppGenericAgentSession', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppGenericAgentSession', N'AgentID') IS NOT NULL
BEGIN
    DECLARE @gen int;
    SELECT @gen = AgentID FROM dbo.AppAgentSkillSet WHERE AgentCode = N'__general_agent__';
    IF @gen IS NOT NULL
        UPDATE dbo.AppGenericAgentSession SET AgentID = NULL WHERE AgentID = @gen;

    ALTER TABLE dbo.AppGenericAgentSession ALTER COLUMN AgentID INT NULL;
END
GO

IF OBJECT_ID(N'dbo.AppAgentEpisode', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentEpisode', N'AgentID') IS NOT NULL
BEGIN
    DECLARE @genEp int;
    SELECT @genEp = AgentID FROM dbo.AppAgentSkillSet WHERE AgentCode = N'__general_agent__';
    IF @genEp IS NOT NULL
        UPDATE dbo.AppAgentEpisode SET AgentID = NULL WHERE AgentID = @genEp;

    ALTER TABLE dbo.AppAgentEpisode ALTER COLUMN AgentID INT NULL;
END
GO


