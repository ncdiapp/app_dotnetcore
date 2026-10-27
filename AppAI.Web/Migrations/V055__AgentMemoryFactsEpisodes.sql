-- V055: Layer 2 (facts) and Layer 3 (episodes) agent memory, plus the extraction bookkeeping on each chat.
-- AppAgentMemoryFact: per user, key -> value, upserted on (UserId, SkillKey, FactKey). SkillKey NULL = all agents.
-- AppAgentEpisode: append-only summaries of past chats with an embedding for similarity recall.
-- AppGenericAgentSession: ExtractedAt / ExtractedTurnCount / ExtractingAt / ExtractRetryCount / MemoryOptOut.
-- The same DDL runs at runtime in AgentMemoryDb.EnsureSchema; keep both in sync.

IF OBJECT_ID(N'dbo.AppAgentMemoryFact', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppAgentMemoryFact
    (
        FactId           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppAgentMemoryFact PRIMARY KEY,
        UserId           INT           NOT NULL,
        SkillKey         NVARCHAR(100) NULL,
        Category         NVARCHAR(30)  NOT NULL CONSTRAINT DF_AppAgentMemoryFact_Category DEFAULT N'note',
        FactKey          NVARCHAR(100) NOT NULL,
        FactValue        NVARCHAR(1000) NOT NULL,
        PreviousValue    NVARCHAR(1000) NULL,
        Status           NVARCHAR(20)  NOT NULL CONSTRAINT DF_AppAgentMemoryFact_Status DEFAULT N'confirmed',
        Source           NVARCHAR(20)  NOT NULL CONSTRAINT DF_AppAgentMemoryFact_Source DEFAULT N'user',
        Confidence       FLOAT         NULL,
        SourceSessionKey NVARCHAR(200) NULL,
        IsActive         BIT           NOT NULL CONSTRAINT DF_AppAgentMemoryFact_IsActive DEFAULT 1,
        UpdatedAt        DATETIME2     NOT NULL CONSTRAINT DF_AppAgentMemoryFact_UpdatedAt DEFAULT GETUTCDATE()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_AppAgentMemoryFact_UserSkillKey' AND object_id = OBJECT_ID(N'dbo.AppAgentMemoryFact'))
    CREATE UNIQUE INDEX UX_AppAgentMemoryFact_UserSkillKey ON dbo.AppAgentMemoryFact (UserId, SkillKey, FactKey);
GO

IF OBJECT_ID(N'dbo.AppAgentEpisode', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppAgentEpisode
    (
        EpisodeId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppAgentEpisode PRIMARY KEY,
        UserId         INT           NOT NULL,
        SkillKey       NVARCHAR(100) NOT NULL,
        SessionKey     NVARCHAR(200) NULL,
        Summary        NVARCHAR(MAX) NOT NULL,
        Topics         NVARCHAR(500) NULL,
        Embedding      VARBINARY(MAX) NULL,
        EmbeddingModel NVARCHAR(80)  NULL,
        CreatedAt      DATETIME2     NOT NULL CONSTRAINT DF_AppAgentEpisode_CreatedAt DEFAULT GETUTCDATE()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppAgentEpisode_UserSkill' AND object_id = OBJECT_ID(N'dbo.AppAgentEpisode'))
    CREATE INDEX IX_AppAgentEpisode_UserSkill ON dbo.AppAgentEpisode (UserId, SkillKey);
GO

IF OBJECT_ID(N'dbo.AppGenericAgentSession', N'U') IS NOT NULL AND COL_LENGTH('dbo.AppGenericAgentSession', 'ExtractedAt') IS NULL
    ALTER TABLE dbo.AppGenericAgentSession ADD ExtractedAt DATETIME2 NULL;
GO
IF OBJECT_ID(N'dbo.AppGenericAgentSession', N'U') IS NOT NULL AND COL_LENGTH('dbo.AppGenericAgentSession', 'ExtractedTurnCount') IS NULL
    ALTER TABLE dbo.AppGenericAgentSession ADD ExtractedTurnCount INT NOT NULL
        CONSTRAINT DF_AppGenericAgentSession_ExtractedTurnCount DEFAULT 0;
GO
IF OBJECT_ID(N'dbo.AppGenericAgentSession', N'U') IS NOT NULL AND COL_LENGTH('dbo.AppGenericAgentSession', 'ExtractingAt') IS NULL
    ALTER TABLE dbo.AppGenericAgentSession ADD ExtractingAt DATETIME2 NULL;
GO
IF OBJECT_ID(N'dbo.AppGenericAgentSession', N'U') IS NOT NULL AND COL_LENGTH('dbo.AppGenericAgentSession', 'ExtractRetryCount') IS NULL
    ALTER TABLE dbo.AppGenericAgentSession ADD ExtractRetryCount INT NOT NULL
        CONSTRAINT DF_AppGenericAgentSession_ExtractRetryCount DEFAULT 0;
GO
IF OBJECT_ID(N'dbo.AppGenericAgentSession', N'U') IS NOT NULL AND COL_LENGTH('dbo.AppGenericAgentSession', 'MemoryOptOut') IS NULL
    ALTER TABLE dbo.AppGenericAgentSession ADD MemoryOptOut BIT NOT NULL
        CONSTRAINT DF_AppGenericAgentSession_MemoryOptOut DEFAULT 0;
GO

-- platform-memory library: tools an agent can subscribe to. These rows do not attach themselves.
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey = N'platform-memory')
INSERT INTO dbo.AppAgentToolLibrary (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
VALUES (
    N'platform-memory',
    N'platform',
    N'Platform Memory',
    N'Remember, forget and recall things about the current user across chats (facts and past conversations).',
    N'BuiltIn',
    1
);
GO

DECLARE @type nvarchar(300) = N'App.BL.AIAgent.GenericAgent.Plugins.AgentMemoryPlugin';

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-memory' AND ToolName = N'remember_fact')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-memory', N'remember_fact',
    N'Save one stable fact about the user so it is known in future chats (a preference, a rule they stated, or a name/ID they use). Use only when the user asks you to remember something or states a lasting preference. Never store passwords, keys or tokens. A new value for the same key replaces the old one.',
    N'{"key":{"type":"string","description":"Short snake_case name, e.g. date_format","required":true},"value":{"type":"string","description":"The fact, max 500 characters","required":true},"category":{"type":"string","description":"preference, entity, rule or note"}}',
    N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"RememberFact"}', 1, 0);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-memory' AND ToolName = N'forget_fact')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-memory', N'forget_fact',
    N'Delete a remembered fact by its key when the user asks you to forget it.',
    N'{"key":{"type":"string","description":"The fact key to delete","required":true}}',
    N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"ForgetFact"}', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-memory' AND ToolName = N'recall_memory')
INSERT INTO dbo.AppAgentLibraryTool (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (N'platform-memory', N'recall_memory',
    N'Search what you remember about this user: saved facts plus summaries of earlier conversations. Use when the user refers to something from before ("like last time", "what did we decide about X").',
    N'{"query":{"type":"string","description":"What to look for","required":true}}',
    N'BuiltIn', N'{"TypeName":"' + @type + N'","MethodName":"RecallMemory"}', 1, 2);
GO
