-- V025: Split AppAgentToolRegister into two tables
-- Before: AppAgentToolRegister stored BOTH agent-owned tools (SkillKey = agent key)
--         AND library-owned tools (SkillKey = library key). The dual-key pattern is confusing.
-- After:  AppAgentLibraryTool stores library-owned tools exclusively.
--         AppAgentToolRegister is now purely agent-owned tools.

-- ─────────────────────────────────────────────────────────────────────────────
-- 1. Create the new table
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AppAgentLibraryTool')
BEGIN
    CREATE TABLE dbo.AppAgentLibraryTool (
        LibraryToolId        INT IDENTITY(1,1) NOT NULL,
        LibraryKey           NVARCHAR(100)     NOT NULL,
        ToolName             NVARCHAR(200)     NOT NULL,
        ToolDescription      NVARCHAR(MAX)     NULL,
        ParameterSchemaJson  NVARCHAR(MAX)     NULL,
        ToolType             NVARCHAR(50)      NOT NULL DEFAULT 'BuiltIn',
        ToolConfig           NVARCHAR(MAX)     NULL,
        IsActive             BIT               NOT NULL DEFAULT 1,
        SortOrder            INT               NOT NULL DEFAULT 0,
        CONSTRAINT PK_AppAgentLibraryTool PRIMARY KEY (LibraryToolId),
        CONSTRAINT FK_AppAgentLibraryTool_Library
            FOREIGN KEY (LibraryKey) REFERENCES dbo.AppAgentToolLibrary(LibraryKey)
    );
END;

-- ─────────────────────────────────────────────────────────────────────────────
-- 2. Migrate existing library-owned rows from AppAgentToolRegister
--    (rows where SkillKey matches a LibraryKey in AppAgentToolLibrary)
-- ─────────────────────────────────────────────────────────────────────────────

INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
SELECT
    t.SkillKey,
    t.ToolName,
    t.ToolDescription,
    t.ParameterSchemaJson,
    t.ToolType,
    t.ToolConfig,
    t.IsActive,
    0
FROM dbo.AppAgentToolRegister t
WHERE EXISTS (
    SELECT 1 FROM dbo.AppAgentToolLibrary l WHERE l.LibraryKey = t.SkillKey
)
AND NOT EXISTS (
    -- Idempotent: skip rows already migrated
    SELECT 1 FROM dbo.AppAgentLibraryTool lt WHERE lt.LibraryKey = t.SkillKey AND lt.ToolName = t.ToolName
);

-- ─────────────────────────────────────────────────────────────────────────────
-- 3. Remove migrated rows from AppAgentToolRegister
-- ─────────────────────────────────────────────────────────────────────────────

DELETE FROM dbo.AppAgentToolRegister
WHERE SkillKey IN (SELECT LibraryKey FROM dbo.AppAgentToolLibrary);
