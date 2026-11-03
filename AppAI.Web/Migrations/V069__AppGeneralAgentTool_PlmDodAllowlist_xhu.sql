-- V069: Replace General Agent allowlist with the TenantDB_PLMDOD selection.
-- Inserts only rows whose LibraryKey exists (active Tool Set) and ToolName exists
-- in AppAgentLibraryTool or AppAgentToolCatalog.

IF OBJECT_ID(N'dbo.AppGeneralAgentTool', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppGeneralAgentTool
    (
        LibraryKey NVARCHAR(100) NOT NULL,
        ToolName   NVARCHAR(200) NOT NULL,
        CONSTRAINT PK_AppGeneralAgentTool PRIMARY KEY (LibraryKey, ToolName)
    );
END
GO

IF OBJECT_ID(N'dbo.AppGeneralAgentTool', N'U') IS NOT NULL
BEGIN
    DELETE FROM dbo.AppGeneralAgentTool;

    CREATE TABLE #WantedGeneralAgentTool
    (
        LibraryKey NVARCHAR(100) NOT NULL,
        ToolName   NVARCHAR(200) NOT NULL,
        PRIMARY KEY (LibraryKey, ToolName)
    );

    INSERT INTO #WantedGeneralAgentTool (LibraryKey, ToolName) VALUES
        (N'agent-files', N'execute_agent_sql_file'),
        (N'agent-files', N'file_delete'),
        (N'agent-files', N'file_list'),
        (N'agent-files', N'file_mkdir'),
        (N'agent-files', N'file_read'),
        (N'agent-files', N'file_write'),
        (N'data-analyze', N'data_analyze'),
        (N'platform-api-provider', N'api-provider-detail'),
        (N'platform-api-provider', N'api-provider-execute'),
        (N'platform-api-provider', N'api-provider-search'),
        (N'platform-database', N'execute_sql'),
        (N'platform-database', N'get_database_schema'),
        (N'platform-document-export', N'convert_docx_to_pdf'),
        (N'platform-document-export', N'export_csv'),
        (N'platform-document-export', N'export_docx'),
        (N'platform-document-export', N'export_excel'),
        (N'platform-document-export', N'export_html'),
        (N'platform-document-export', N'export_pdf'),
        (N'platform-document-export', N'export_pptx'),
        (N'platform-document-export', N'read_csv'),
        (N'platform-document-export', N'read_docx'),
        (N'platform-document-export', N'read_excel'),
        (N'platform-document-export', N'read_html'),
        (N'platform-document-export', N'read_pdf'),
        (N'platform-document-export', N'read_pptx'),
        (N'platform-memory', N'forget_fact'),
        (N'platform-memory', N'load_memory_context'),
        (N'platform-memory', N'recall_memory'),
        (N'platform-memory', N'remember_fact'),
        (N'platform-memory', N'search_memory'),
        (N'platform-search', N'list_available_searches'),
        (N'platform-ui-render', N'cache_ui_render_polish'),
        (N'platform-ui-render', N'data_render'),
        (N'platform-ui-render', N'prepare_ui_render_polish'),
        (N'platform-ui-render', N'render_interactive'),
        (N'platform-ui-render', N'render_none_interactive'),
        (N'PLM-MCP', N'PLM-MCP-Server-Tool');

    INSERT INTO dbo.AppGeneralAgentTool (LibraryKey, ToolName)
    SELECT w.LibraryKey, w.ToolName
    FROM #WantedGeneralAgentTool w
    INNER JOIN dbo.AppAgentToolLibrary l
        ON l.LibraryKey = w.LibraryKey AND l.IsActive = 1
    INNER JOIN dbo.AppAgentLibraryTool t
        ON t.LibraryKey = w.LibraryKey AND t.ToolName = w.ToolName AND t.IsActive = 1;

    IF OBJECT_ID(N'dbo.AppAgentToolCatalog', N'U') IS NOT NULL
        EXEC(N'
        INSERT INTO dbo.AppGeneralAgentTool (LibraryKey, ToolName)
        SELECT w.LibraryKey, w.ToolName
        FROM #WantedGeneralAgentTool w
        INNER JOIN dbo.AppAgentToolLibrary l
            ON l.LibraryKey = w.LibraryKey AND l.IsActive = 1
        INNER JOIN dbo.AppAgentToolCatalog c
            ON c.LibraryKey = w.LibraryKey AND c.ToolName = w.ToolName
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.AppGeneralAgentTool g
            WHERE g.LibraryKey = w.LibraryKey AND g.ToolName = w.ToolName
        );');

    DROP TABLE #WantedGeneralAgentTool;
END
GO
