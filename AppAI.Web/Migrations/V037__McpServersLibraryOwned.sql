-- V037: MCP servers are owned by Tool Libraries only.
-- AppAgentMcpServer.SkillKey now always holds a LibraryKey; agents get MCP servers by
-- subscribing to the library (AppAgentLibrarySubscription).
-- Rows whose SkillKey is not a library key (previously agent-owned) are moved into a new
-- library "<SkillKey>-mcp" and, if SkillKey is an agent, that agent is subscribed to it.

IF OBJECT_ID(N'dbo.AppAgentMcpServer', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolDomain WHERE DomainKey = N'mcp-servers')
        INSERT INTO dbo.AppAgentToolDomain (DomainKey, DomainName, Description, SortOrder, IsActive)
        VALUES (N'mcp-servers', N'MCP Servers', N'Libraries that hold registered MCP servers', 0, 1);

    DECLARE @Owners TABLE (OldKey NVARCHAR(100) NOT NULL, NewKey NVARCHAR(100) NOT NULL);

    INSERT INTO @Owners (OldKey, NewKey)
    SELECT DISTINCT m.SkillKey, LEFT(m.SkillKey, 95) + N'-mcp'
    FROM dbo.AppAgentMcpServer m
    WHERE NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary l WHERE l.LibraryKey = m.SkillKey);

    INSERT INTO dbo.AppAgentToolLibrary (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
    SELECT o.NewKey, N'mcp-servers', o.OldKey + N' MCP servers', N'Migrated from agent-owned MCP servers (V037)', N'Mcp', 1
    FROM @Owners o
    WHERE NOT EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary l WHERE l.LibraryKey = o.NewKey);

    INSERT INTO dbo.AppAgentLibrarySubscription (SkillKey, LibraryKey)
    SELECT o.OldKey, o.NewKey
    FROM @Owners o
    WHERE EXISTS (SELECT 1 FROM dbo.AppAgentSkillSet s WHERE s.SkillKey = o.OldKey)
      AND NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibrarySubscription x WHERE x.SkillKey = o.OldKey AND x.LibraryKey = o.NewKey);

    UPDATE m SET m.SkillKey = o.NewKey
    FROM dbo.AppAgentMcpServer m
    INNER JOIN @Owners o ON o.OldKey = m.SkillKey;
END
GO
