-- merge file
-- V038: Per-agent tool exclusions.
-- An agent that subscribes to a library gets every active tool in it, except the rows listed here.
-- ToolName is the library tool name, or the MCP server's own tool name for MCP libraries.

IF OBJECT_ID(N'dbo.AppAgentToolExclusion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppAgentToolExclusion
    (
        SkillKey   NVARCHAR(100) NOT NULL,
        LibraryKey NVARCHAR(100) NOT NULL,
        ToolName   NVARCHAR(200) NOT NULL,
        CONSTRAINT PK_AppAgentToolExclusion PRIMARY KEY (SkillKey, LibraryKey, ToolName)
    );
END
GO

-- V038: Remove ask_user from platform-multi-agent library (and stray private registers).
-- ask_user is a system built-in auto-injected by GenericAgentEngine for Interactive agents —
-- it must not appear as a library tool or private BuiltIn registration.
-- Idempotent DELETE.

DELETE FROM dbo.AppAgentLibraryTool
WHERE LibraryKey = N'platform-multi-agent' AND ToolName = N'ask_user';
GO

DELETE FROM dbo.AppAgentToolRegister
WHERE ToolName = N'ask_user';
GO
