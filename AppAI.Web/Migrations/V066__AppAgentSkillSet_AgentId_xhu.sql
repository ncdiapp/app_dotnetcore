-- V066: AppAgentSkillSet PK becomes AgentID (IDENTITY).
-- SkillKey is renamed to AgentCode (UNIQUE, not PK).
-- Agent-pointing tables get AgentID FKs; leftover SkillKey columns are dropped after backfill.
-- Idempotent. Each ALTER ADD COLUMN is in its own batch (SQL Server compile rules).

-- ================================================================
-- 1. Drop FKs that currently point at AppAgentSkillSet.SkillKey
-- ================================================================
DECLARE @dropFk nvarchar(max) = N'';
SELECT @dropFk = @dropFk + N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id))
    + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
    + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';'
FROM sys.foreign_keys fk
WHERE fk.referenced_object_id = OBJECT_ID(N'dbo.AppAgentSkillSet');
IF LEN(@dropFk) > 0 EXEC sp_executesql @dropFk;
GO

-- Drop SkillSet PK so AgentID can become PK
IF OBJECT_ID(N'dbo.AppAgentSkillSet', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentSkillSet', N'AgentID') IS NULL
BEGIN
    DECLARE @pk nvarchar(256);
    SELECT @pk = kc.name
    FROM sys.key_constraints kc
    WHERE kc.parent_object_id = OBJECT_ID(N'dbo.AppAgentSkillSet') AND kc.type = N'PK';
    IF @pk IS NOT NULL
        EXEC(N'ALTER TABLE dbo.AppAgentSkillSet DROP CONSTRAINT [' + @pk + N']');
END
GO

IF OBJECT_ID(N'dbo.AppAgentSkillSet', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentSkillSet', N'AgentID') IS NULL
    ALTER TABLE dbo.AppAgentSkillSet ADD AgentID INT IDENTITY(1,1) NOT NULL;
GO

IF OBJECT_ID(N'dbo.AppAgentSkillSet', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentSkillSet', N'AgentID') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.key_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.AppAgentSkillSet') AND type = N'PK')
    ALTER TABLE dbo.AppAgentSkillSet ADD CONSTRAINT PK_AppAgentSkillSet PRIMARY KEY (AgentID);
GO

IF COL_LENGTH(N'dbo.AppAgentSkillSet', N'SkillKey') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentSkillSet', N'AgentCode') IS NULL
    EXEC sp_rename N'dbo.AppAgentSkillSet.SkillKey', N'AgentCode', N'COLUMN';
GO

IF OBJECT_ID(N'dbo.AppAgentSkillSet', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentSkillSet', N'AgentCode') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.AppAgentSkillSet')
          AND name = N'UX_AppAgentSkillSet_AgentCode')
    CREATE UNIQUE INDEX UX_AppAgentSkillSet_AgentCode ON dbo.AppAgentSkillSet (AgentCode);
GO

-- ================================================================
-- 2. AppAgentChildMapping
-- ================================================================
IF OBJECT_ID(N'dbo.AppAgentChildMapping', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentChildMapping', N'ParentAgentId') IS NULL
    ALTER TABLE dbo.AppAgentChildMapping ADD ParentAgentId INT NULL;
GO
IF OBJECT_ID(N'dbo.AppAgentChildMapping', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentChildMapping', N'ChildAgentId') IS NULL
    ALTER TABLE dbo.AppAgentChildMapping ADD ChildAgentId INT NULL;
GO

-- Dynamic SQL: SQL Server compiles UPDATE/DROP COLUMN even when the IF is false.
IF OBJECT_ID(N'dbo.AppAgentChildMapping', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentChildMapping', N'ParentSkillKey') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentChildMapping', N'ChildSkillKey') IS NOT NULL
BEGIN
    EXEC(N'
    UPDATE m
    SET m.ParentAgentId = p.AgentID, m.ChildAgentId = c.AgentID
    FROM dbo.AppAgentChildMapping m
    INNER JOIN dbo.AppAgentSkillSet p ON p.AgentCode = m.ParentSkillKey
    INNER JOIN dbo.AppAgentSkillSet c ON c.AgentCode = m.ChildSkillKey;

    DELETE FROM dbo.AppAgentChildMapping WHERE ParentAgentId IS NULL OR ChildAgentId IS NULL;
    ');
END
GO

IF OBJECT_ID(N'dbo.AppAgentChildMapping', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentChildMapping', N'ParentSkillKey') IS NOT NULL
BEGIN
    IF OBJECT_ID(N'dbo.CK_AppAgentChildMapping_NotSelf', N'C') IS NOT NULL
        ALTER TABLE dbo.AppAgentChildMapping DROP CONSTRAINT CK_AppAgentChildMapping_NotSelf;

    DECLARE @pkMap nvarchar(256);
    SELECT @pkMap = kc.name
    FROM sys.key_constraints kc
    WHERE kc.parent_object_id = OBJECT_ID(N'dbo.AppAgentChildMapping') AND kc.type = N'PK';
    IF @pkMap IS NOT NULL
        EXEC(N'ALTER TABLE dbo.AppAgentChildMapping DROP CONSTRAINT [' + @pkMap + N']');

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppAgentChildMapping_ChildSkillKey'
               AND object_id = OBJECT_ID(N'dbo.AppAgentChildMapping'))
        DROP INDEX IX_AppAgentChildMapping_ChildSkillKey ON dbo.AppAgentChildMapping;

    EXEC(N'ALTER TABLE dbo.AppAgentChildMapping DROP COLUMN ParentSkillKey');
    IF COL_LENGTH(N'dbo.AppAgentChildMapping', N'ChildSkillKey') IS NOT NULL
        EXEC(N'ALTER TABLE dbo.AppAgentChildMapping DROP COLUMN ChildSkillKey');
END
GO

IF OBJECT_ID(N'dbo.AppAgentChildMapping', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentChildMapping', N'ParentAgentId') IS NOT NULL
BEGIN
    DELETE FROM dbo.AppAgentChildMapping WHERE ParentAgentId IS NULL OR ChildAgentId IS NULL;
    ALTER TABLE dbo.AppAgentChildMapping ALTER COLUMN ParentAgentId INT NOT NULL;
    ALTER TABLE dbo.AppAgentChildMapping ALTER COLUMN ChildAgentId INT NOT NULL;
END
GO

IF OBJECT_ID(N'dbo.AppAgentChildMapping', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentChildMapping', N'ParentAgentId') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.key_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.AppAgentChildMapping') AND type = N'PK')
BEGIN
    ALTER TABLE dbo.AppAgentChildMapping
        ADD CONSTRAINT PK_AppAgentChildMapping PRIMARY KEY (ParentAgentId, ChildAgentId),
            CONSTRAINT CK_AppAgentChildMapping_NotSelf CHECK (ParentAgentId <> ChildAgentId),
            -- Both FKs NO ACTION: SQL Server rejects CASCADE here (two FKs to the same table).
            CONSTRAINT FK_AppAgentChildMapping_Parent
                FOREIGN KEY (ParentAgentId) REFERENCES dbo.AppAgentSkillSet (AgentID),
            CONSTRAINT FK_AppAgentChildMapping_Child
                FOREIGN KEY (ChildAgentId) REFERENCES dbo.AppAgentSkillSet (AgentID);
END
GO

IF OBJECT_ID(N'dbo.AppAgentChildMapping', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentChildMapping', N'ChildAgentId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppAgentChildMapping_ChildAgentId'
                   AND object_id = OBJECT_ID(N'dbo.AppAgentChildMapping'))
    CREATE INDEX IX_AppAgentChildMapping_ChildAgentId ON dbo.AppAgentChildMapping (ChildAgentId);
GO

-- ================================================================
-- 3. AppAgentLibrarySubscription
-- ================================================================
IF OBJECT_ID(N'dbo.AppAgentLibrarySubscription', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentLibrarySubscription', N'AgentID') IS NULL
    ALTER TABLE dbo.AppAgentLibrarySubscription ADD AgentID INT NULL;
GO

IF OBJECT_ID(N'dbo.AppAgentLibrarySubscription', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentLibrarySubscription', N'SkillKey') IS NOT NULL
BEGIN
    EXEC(N'
    UPDATE s SET s.AgentID = k.AgentID
    FROM dbo.AppAgentLibrarySubscription s
    INNER JOIN dbo.AppAgentSkillSet k ON k.AgentCode = s.SkillKey;

    DELETE FROM dbo.AppAgentLibrarySubscription WHERE AgentID IS NULL;
    ');
END
GO

IF OBJECT_ID(N'dbo.AppAgentLibrarySubscription', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentLibrarySubscription', N'SkillKey') IS NOT NULL
BEGIN
    DECLARE @pkSub nvarchar(256);
    SELECT @pkSub = kc.name
    FROM sys.key_constraints kc
    WHERE kc.parent_object_id = OBJECT_ID(N'dbo.AppAgentLibrarySubscription') AND kc.type = N'PK';
    IF @pkSub IS NOT NULL
        EXEC(N'ALTER TABLE dbo.AppAgentLibrarySubscription DROP CONSTRAINT [' + @pkSub + N']');

    EXEC(N'ALTER TABLE dbo.AppAgentLibrarySubscription DROP COLUMN SkillKey');
END
GO

IF OBJECT_ID(N'dbo.AppAgentLibrarySubscription', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentLibrarySubscription', N'AgentID') IS NOT NULL
BEGIN
    DELETE FROM dbo.AppAgentLibrarySubscription WHERE AgentID IS NULL;
    ALTER TABLE dbo.AppAgentLibrarySubscription ALTER COLUMN AgentID INT NOT NULL;
END
GO

IF OBJECT_ID(N'dbo.AppAgentLibrarySubscription', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentLibrarySubscription', N'AgentID') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentLibrarySubscription', N'SkillKey') IS NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.key_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.AppAgentLibrarySubscription') AND type = N'PK')
BEGIN
    ALTER TABLE dbo.AppAgentLibrarySubscription
        ADD CONSTRAINT PK_AppAgentLibrarySub PRIMARY KEY (AgentID, LibraryKey),
            CONSTRAINT FK_AppAgentLibrarySub_Agent
                FOREIGN KEY (AgentID) REFERENCES dbo.AppAgentSkillSet (AgentID) ON DELETE CASCADE;
END
GO

IF OBJECT_ID(N'dbo.AppAgentLibrarySubscription', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentLibrarySubscription', N'LibraryKey') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppAgentLibrarySub_LibraryKey'
                   AND object_id = OBJECT_ID(N'dbo.AppAgentLibrarySubscription'))
    CREATE INDEX IX_AppAgentLibrarySub_LibraryKey ON dbo.AppAgentLibrarySubscription (LibraryKey);
GO

-- ================================================================
-- 4. AppAgentToolExclusion
-- ================================================================
IF OBJECT_ID(N'dbo.AppAgentToolExclusion', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentToolExclusion', N'AgentID') IS NULL
    ALTER TABLE dbo.AppAgentToolExclusion ADD AgentID INT NULL;
GO

IF OBJECT_ID(N'dbo.AppAgentToolExclusion', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentToolExclusion', N'SkillKey') IS NOT NULL
BEGIN
    EXEC(N'
    UPDATE e SET e.AgentID = k.AgentID
    FROM dbo.AppAgentToolExclusion e
    INNER JOIN dbo.AppAgentSkillSet k ON k.AgentCode = e.SkillKey;

    DELETE FROM dbo.AppAgentToolExclusion WHERE AgentID IS NULL;
    ');
END
GO

IF OBJECT_ID(N'dbo.AppAgentToolExclusion', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentToolExclusion', N'SkillKey') IS NOT NULL
BEGIN
    DECLARE @pkEx nvarchar(256);
    SELECT @pkEx = kc.name
    FROM sys.key_constraints kc
    WHERE kc.parent_object_id = OBJECT_ID(N'dbo.AppAgentToolExclusion') AND kc.type = N'PK';
    IF @pkEx IS NOT NULL
        EXEC(N'ALTER TABLE dbo.AppAgentToolExclusion DROP CONSTRAINT [' + @pkEx + N']');

    EXEC(N'ALTER TABLE dbo.AppAgentToolExclusion DROP COLUMN SkillKey');
END
GO

IF OBJECT_ID(N'dbo.AppAgentToolExclusion', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentToolExclusion', N'AgentID') IS NOT NULL
BEGIN
    DELETE FROM dbo.AppAgentToolExclusion WHERE AgentID IS NULL;
    ALTER TABLE dbo.AppAgentToolExclusion ALTER COLUMN AgentID INT NOT NULL;
END
GO

IF OBJECT_ID(N'dbo.AppAgentToolExclusion', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentToolExclusion', N'AgentID') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentToolExclusion', N'SkillKey') IS NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.key_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.AppAgentToolExclusion') AND type = N'PK')
BEGIN
    ALTER TABLE dbo.AppAgentToolExclusion
        ADD CONSTRAINT PK_AppAgentToolExclusion PRIMARY KEY (AgentID, LibraryKey, ToolName),
            CONSTRAINT FK_AppAgentToolExclusion_Agent
                FOREIGN KEY (AgentID) REFERENCES dbo.AppAgentSkillSet (AgentID) ON DELETE CASCADE;
END
GO

-- ================================================================
-- 5. AppAgentSkillSetHistory
-- ================================================================
IF OBJECT_ID(N'dbo.AppAgentSkillSetHistory', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentSkillSetHistory', N'AgentID') IS NULL
    ALTER TABLE dbo.AppAgentSkillSetHistory ADD AgentID INT NULL;
GO

IF OBJECT_ID(N'dbo.AppAgentSkillSetHistory', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentSkillSetHistory', N'SkillKey') IS NOT NULL
BEGIN
    EXEC(N'
    UPDATE h SET h.AgentID = k.AgentID
    FROM dbo.AppAgentSkillSetHistory h
    INNER JOIN dbo.AppAgentSkillSet k ON k.AgentCode = h.SkillKey;

    DELETE FROM dbo.AppAgentSkillSetHistory WHERE AgentID IS NULL;
    ');
END
GO

IF OBJECT_ID(N'dbo.AppAgentSkillSetHistory', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentSkillSetHistory', N'SkillKey') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppAgentSkillSetHistory_SkillKey'
               AND object_id = OBJECT_ID(N'dbo.AppAgentSkillSetHistory'))
        DROP INDEX IX_AppAgentSkillSetHistory_SkillKey ON dbo.AppAgentSkillSetHistory;

    EXEC(N'ALTER TABLE dbo.AppAgentSkillSetHistory DROP COLUMN SkillKey');
END
GO

IF OBJECT_ID(N'dbo.AppAgentSkillSetHistory', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentSkillSetHistory', N'AgentID') IS NOT NULL
BEGIN
    DELETE FROM dbo.AppAgentSkillSetHistory WHERE AgentID IS NULL;
    ALTER TABLE dbo.AppAgentSkillSetHistory ALTER COLUMN AgentID INT NOT NULL;
END
GO

IF OBJECT_ID(N'dbo.AppAgentSkillSetHistory', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentSkillSetHistory', N'AgentID') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentSkillSetHistory', N'SkillKey') IS NULL
   AND OBJECT_ID(N'dbo.FK_AppAgentSkillSetHistory_Agent', N'F') IS NULL
    ALTER TABLE dbo.AppAgentSkillSetHistory
        ADD CONSTRAINT FK_AppAgentSkillSetHistory_Agent
            FOREIGN KEY (AgentID) REFERENCES dbo.AppAgentSkillSet (AgentID) ON DELETE CASCADE;
GO

IF OBJECT_ID(N'dbo.AppAgentSkillSetHistory', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentSkillSetHistory', N'AgentID') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppAgentSkillSetHistory_AgentID'
                   AND object_id = OBJECT_ID(N'dbo.AppAgentSkillSetHistory'))
    CREATE INDEX IX_AppAgentSkillSetHistory_AgentID
        ON dbo.AppAgentSkillSetHistory (AgentID, SavedAt DESC);
GO

-- ================================================================
-- 6. AppGenericAgentSession
-- ================================================================
IF OBJECT_ID(N'dbo.AppGenericAgentSession', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppGenericAgentSession', N'AgentID') IS NULL
    ALTER TABLE dbo.AppGenericAgentSession ADD AgentID INT NULL;
GO

IF OBJECT_ID(N'dbo.AppGenericAgentSession', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppGenericAgentSession', N'SkillKey') IS NOT NULL
BEGIN
    EXEC(N'
    UPDATE s SET s.AgentID = k.AgentID
    FROM dbo.AppGenericAgentSession s
    INNER JOIN dbo.AppAgentSkillSet k ON k.AgentCode = s.SkillKey;

    DELETE FROM dbo.AppGenericAgentSession WHERE AgentID IS NULL;
    ');
END
GO

IF OBJECT_ID(N'dbo.AppGenericAgentSession', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppGenericAgentSession', N'SkillKey') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppGenericAgentSession_SkillUser'
               AND object_id = OBJECT_ID(N'dbo.AppGenericAgentSession'))
        DROP INDEX IX_AppGenericAgentSession_SkillUser ON dbo.AppGenericAgentSession;

    EXEC(N'ALTER TABLE dbo.AppGenericAgentSession DROP COLUMN SkillKey');
END
GO

IF OBJECT_ID(N'dbo.AppGenericAgentSession', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppGenericAgentSession', N'AgentID') IS NOT NULL
BEGIN
    DELETE FROM dbo.AppGenericAgentSession WHERE AgentID IS NULL;
    ALTER TABLE dbo.AppGenericAgentSession ALTER COLUMN AgentID INT NOT NULL;
END
GO

IF OBJECT_ID(N'dbo.AppGenericAgentSession', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppGenericAgentSession', N'AgentID') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppGenericAgentSession', N'SkillKey') IS NULL
   AND OBJECT_ID(N'dbo.FK_AppGenericAgentSession_Agent', N'F') IS NULL
    ALTER TABLE dbo.AppGenericAgentSession
        ADD CONSTRAINT FK_AppGenericAgentSession_Agent
            FOREIGN KEY (AgentID) REFERENCES dbo.AppAgentSkillSet (AgentID) ON DELETE CASCADE;
GO

IF OBJECT_ID(N'dbo.AppGenericAgentSession', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppGenericAgentSession', N'AgentID') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppGenericAgentSession_AgentUser'
                   AND object_id = OBJECT_ID(N'dbo.AppGenericAgentSession'))
    CREATE INDEX IX_AppGenericAgentSession_AgentUser
        ON dbo.AppGenericAgentSession (AgentID, UserId);
GO

-- ================================================================
-- 7. AppAgentMemoryFact  (NULL AgentID = all agents)
-- ================================================================
IF OBJECT_ID(N'dbo.AppAgentMemoryFact', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentMemoryFact', N'AgentID') IS NULL
    ALTER TABLE dbo.AppAgentMemoryFact ADD AgentID INT NULL;
GO

IF OBJECT_ID(N'dbo.AppAgentMemoryFact', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentMemoryFact', N'SkillKey') IS NOT NULL
BEGIN
    EXEC(N'
    UPDATE f SET f.AgentID = k.AgentID
    FROM dbo.AppAgentMemoryFact f
    INNER JOIN dbo.AppAgentSkillSet k ON k.AgentCode = f.SkillKey
    WHERE f.SkillKey IS NOT NULL;

    DELETE FROM dbo.AppAgentMemoryFact
    WHERE SkillKey IS NOT NULL AND AgentID IS NULL;
    ');
END
GO

IF OBJECT_ID(N'dbo.AppAgentMemoryFact', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentMemoryFact', N'SkillKey') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_AppAgentMemoryFact_UserSkillKey'
               AND object_id = OBJECT_ID(N'dbo.AppAgentMemoryFact'))
        DROP INDEX UX_AppAgentMemoryFact_UserSkillKey ON dbo.AppAgentMemoryFact;

    EXEC(N'ALTER TABLE dbo.AppAgentMemoryFact DROP COLUMN SkillKey');
END
GO

IF OBJECT_ID(N'dbo.AppAgentMemoryFact', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentMemoryFact', N'AgentID') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentMemoryFact', N'SkillKey') IS NULL
   AND OBJECT_ID(N'dbo.FK_AppAgentMemoryFact_Agent', N'F') IS NULL
BEGIN
    ALTER TABLE dbo.AppAgentMemoryFact
        ADD CONSTRAINT FK_AppAgentMemoryFact_Agent
            FOREIGN KEY (AgentID) REFERENCES dbo.AppAgentSkillSet (AgentID) ON DELETE CASCADE;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_AppAgentMemoryFact_UserAgentKey'
                   AND object_id = OBJECT_ID(N'dbo.AppAgentMemoryFact'))
        CREATE UNIQUE INDEX UX_AppAgentMemoryFact_UserAgentKey
            ON dbo.AppAgentMemoryFact (UserId, AgentID, FactKey);
END
GO

-- ================================================================
-- 8. AppAgentEpisode
-- ================================================================
IF OBJECT_ID(N'dbo.AppAgentEpisode', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentEpisode', N'AgentID') IS NULL
    ALTER TABLE dbo.AppAgentEpisode ADD AgentID INT NULL;
GO

IF OBJECT_ID(N'dbo.AppAgentEpisode', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentEpisode', N'SkillKey') IS NOT NULL
BEGIN
    EXEC(N'
    UPDATE e SET e.AgentID = k.AgentID
    FROM dbo.AppAgentEpisode e
    INNER JOIN dbo.AppAgentSkillSet k ON k.AgentCode = e.SkillKey;

    DELETE FROM dbo.AppAgentEpisode WHERE AgentID IS NULL;
    ');
END
GO

IF OBJECT_ID(N'dbo.AppAgentEpisode', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentEpisode', N'SkillKey') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppAgentEpisode_UserSkill'
               AND object_id = OBJECT_ID(N'dbo.AppAgentEpisode'))
        DROP INDEX IX_AppAgentEpisode_UserSkill ON dbo.AppAgentEpisode;

    EXEC(N'ALTER TABLE dbo.AppAgentEpisode DROP COLUMN SkillKey');
END
GO

IF OBJECT_ID(N'dbo.AppAgentEpisode', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentEpisode', N'AgentID') IS NOT NULL
BEGIN
    DELETE FROM dbo.AppAgentEpisode WHERE AgentID IS NULL;
    ALTER TABLE dbo.AppAgentEpisode ALTER COLUMN AgentID INT NOT NULL;
END
GO

IF OBJECT_ID(N'dbo.AppAgentEpisode', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentEpisode', N'AgentID') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentEpisode', N'SkillKey') IS NULL
   AND OBJECT_ID(N'dbo.FK_AppAgentEpisode_Agent', N'F') IS NULL
    ALTER TABLE dbo.AppAgentEpisode
        ADD CONSTRAINT FK_AppAgentEpisode_Agent
            FOREIGN KEY (AgentID) REFERENCES dbo.AppAgentSkillSet (AgentID) ON DELETE CASCADE;
GO

IF OBJECT_ID(N'dbo.AppAgentEpisode', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppAgentEpisode', N'AgentID') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppAgentEpisode_UserAgent'
                   AND object_id = OBJECT_ID(N'dbo.AppAgentEpisode'))
    CREATE INDEX IX_AppAgentEpisode_UserAgent ON dbo.AppAgentEpisode (UserId, AgentID);
GO
