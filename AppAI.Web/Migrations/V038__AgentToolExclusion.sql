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
