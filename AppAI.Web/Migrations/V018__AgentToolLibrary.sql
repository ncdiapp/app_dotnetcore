-- V017: Agent Tool Library — Domain / Library / Subscription hierarchy
-- Enables shared tool libraries that agents can subscribe to.
-- AppAgentToolRegister.SkillKey has no FK constraint — LibraryKey IS a SkillKey
-- for tool rows in that library. No existing tables are modified.

-- ─────────────────────────────────────────────────────────────────────────────
-- 1. Tables
-- ─────────────────────────────────────────────────────────────────────────────

CREATE TABLE dbo.AppAgentToolDomain (
    DomainKey   NVARCHAR(100) NOT NULL,
    DomainName  NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    SortOrder   INT           NOT NULL CONSTRAINT DF_AppAgentToolDomain_SortOrder DEFAULT 0,
    IsActive    BIT           NOT NULL CONSTRAINT DF_AppAgentToolDomain_IsActive  DEFAULT 1,
    CONSTRAINT PK_AppAgentToolDomain PRIMARY KEY (DomainKey)
);

CREATE TABLE dbo.AppAgentToolLibrary (
    LibraryKey   NVARCHAR(100) NOT NULL,
    DomainKey    NVARCHAR(100) NOT NULL,
    LibraryName  NVARCHAR(200) NOT NULL,
    Description  NVARCHAR(MAX) NULL,
    ToolCategory NVARCHAR(50)  NULL,   -- UI hint: HttpRest / SqlQuery / BuiltIn / Mcp
    IsActive     BIT           NOT NULL CONSTRAINT DF_AppAgentToolLibrary_IsActive DEFAULT 1,
    CONSTRAINT PK_AppAgentToolLibrary PRIMARY KEY (LibraryKey),
    CONSTRAINT FK_AppAgentToolLibrary_Domain
        FOREIGN KEY (DomainKey) REFERENCES dbo.AppAgentToolDomain(DomainKey)
);

CREATE TABLE dbo.AppAgentLibrarySubscription (
    SkillKey   NVARCHAR(100) NOT NULL,
    LibraryKey NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_AppAgentLibrarySub PRIMARY KEY (SkillKey, LibraryKey),
    CONSTRAINT FK_AppAgentLibrarySub_Library
        FOREIGN KEY (LibraryKey) REFERENCES dbo.AppAgentToolLibrary(LibraryKey)
        ON DELETE CASCADE
);

CREATE INDEX IX_AppAgentLibrarySub_LibraryKey
    ON dbo.AppAgentLibrarySubscription (LibraryKey);

-- ─────────────────────────────────────────────────────────────────────────────
-- 2. Seed domains
-- ─────────────────────────────────────────────────────────────────────────────

INSERT INTO dbo.AppAgentToolDomain (DomainKey, DomainName, Description, SortOrder, IsActive) VALUES
    ('platform',         'Platform Built-in',  'Built-in C# tools pre-compiled into the platform',    1, 1),
    ('mcp-servers',      'MCP Servers',        'External MCP server connections (auto-discover tools)',2, 1),
    ('database-queries', 'Custom SQL Queries', 'SqlQuery tools that query the tenant database',        3, 1),
    ('external-rest',    'External REST APIs', 'HttpRest tools calling third-party REST endpoints',    4, 1);

-- ─────────────────────────────────────────────────────────────────────────────
-- 3. Seed example library + example tools
--    Non-developers can copy these rows as starting templates.
-- ─────────────────────────────────────────────────────────────────────────────

INSERT INTO dbo.AppAgentToolLibrary (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive) VALUES
    ('platform-queries', 'database-queries',
     'Platform Query Tools',
     'Common SqlQuery tools for reading AppAI platform configuration and settings.',
     'SqlQuery', 1);

-- Example SqlQuery tool: read tenant settings
INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'platform-queries',
    'get_tenant_settings',
    'Call this tool when the user asks about platform configuration, application settings, or tenant settings. Returns key-value configuration pairs for the current tenant. Filter by category keyword if the user names a specific area.',
    '{"properties":{"category":{"description":"Optional keyword to filter setting keys (e.g. ''AI'', ''Email'', ''Theme''). Leave empty to return all settings.","type":"string"}},"required":[]}',
    'SqlQuery',
    '{"SqlBody":"SELECT SettingKey, SettingValue FROM dbo.AppTenantSetting WHERE IsActive=1 AND (@category IS NULL OR @category = '''' OR SettingKey LIKE ''%'' + @category + ''%'') ORDER BY SettingKey","ReturnType":"json"}',
    1
);

-- Example HttpRest tool: call a public JSON placeholder API (safe demo endpoint)
INSERT INTO dbo.AppAgentToolLibrary (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive) VALUES
    ('demo-rest-api', 'external-rest',
     'Demo REST API Tools',
     'Example HttpRest tools showing the correct ToolConfig shape for calling external REST APIs. Replace the URL and headers with your real endpoint.',
     'HttpRest', 1);

INSERT INTO dbo.AppAgentToolRegister
    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
VALUES (
    'demo-rest-api',
    'get_post_by_id',
    'Call this tool when the user asks to retrieve a post or article by its numeric ID. Returns the post title and body from the demo API. Replace with your real API endpoint and authentication headers.',
    '{"properties":{"postId":{"description":"The numeric ID of the post to retrieve","type":"string"}},"required":["postId"]}',
    'HttpRest',
    '{"Url":"https://jsonplaceholder.typicode.com/posts/{postId}","Method":"GET","Headers":{}}',
    1
);
