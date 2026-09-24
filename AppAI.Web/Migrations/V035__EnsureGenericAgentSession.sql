-- V035: Repair GenericAgent session schema for tenants where V021 was skipped
-- or incorrectly recorded as applied.

IF OBJECT_ID(N'dbo.AppGenericAgentSession', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppGenericAgentSession
    (
        SessionKey   NVARCHAR(200) NOT NULL,
        SkillKey     NVARCHAR(100) NOT NULL,
        UserId       INT           NOT NULL,
        MessagesJson NVARCHAR(MAX) NOT NULL,
        UpdatedAt    DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_AppGenericAgentSession PRIMARY KEY (SessionKey)
    );
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_AppGenericAgentSession_SkillUser'
      AND object_id = OBJECT_ID(N'dbo.AppGenericAgentSession')
)
BEGIN
    CREATE INDEX IX_AppGenericAgentSession_SkillUser
        ON dbo.AppGenericAgentSession (SkillKey, UserId);
END
GO

IF COL_LENGTH(N'dbo.AppGenericAgentSession', N'DisplayTitle') IS NULL
BEGIN
    ALTER TABLE dbo.AppGenericAgentSession ADD DisplayTitle NVARCHAR(200) NULL;
END
GO
