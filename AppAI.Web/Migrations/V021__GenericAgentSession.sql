-- V021: Generic agent session persistence — stores per-user, per-skill conversation history
--       so conversations survive browser refresh and tab switches.
--       SessionKey = '{SkillKey}:{UserId}' — one active session per user per skill.

CREATE TABLE dbo.AppGenericAgentSession (
    SessionKey    NVARCHAR(200) NOT NULL,
    SkillKey      NVARCHAR(100) NOT NULL,
    UserId        INT           NOT NULL,
    MessagesJson  NVARCHAR(MAX) NOT NULL,
    UpdatedAt     DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_AppGenericAgentSession PRIMARY KEY (SessionKey)
);
GO

CREATE INDEX IX_AppGenericAgentSession_SkillUser
    ON dbo.AppGenericAgentSession (SkillKey, UserId);
GO
