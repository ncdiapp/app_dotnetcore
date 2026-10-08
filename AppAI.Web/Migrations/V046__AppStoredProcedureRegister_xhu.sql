-- V046: AppStoredProcedureRegister — AI-trained SP catalog for agent discovery
-- UI: Database Management → Stored Procedure AI Register (AI-ready only)
-- Agent: stored_procedure_search/detail read published register rows only (not live sys.procedures)
-- Idempotent.

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = N'dbo' AND TABLE_NAME = N'AppStoredProcedureRegister'
)
BEGIN
    CREATE TABLE dbo.AppStoredProcedureRegister (
        Id                      INT IDENTITY(1,1) NOT NULL,
        DataSourceRegisterId    INT            NOT NULL,
        SchemaName              NVARCHAR(128)  NOT NULL CONSTRAINT DF_AppSpReg_Schema DEFAULT (N'dbo'),
        SpName                  NVARCHAR(256)  NOT NULL,
        FullName                NVARCHAR(400)  NOT NULL,
        Description             NVARCHAR(1000) NULL,
        InputJson               NVARCHAR(MAX)  NULL,
        OutputColumnsJson       NVARCHAR(MAX)  NULL,
        SampleJson              NVARCHAR(MAX)  NULL,
        UsageText               NVARCHAR(1000) NULL,
        IsPublishedToAgent      BIT            NOT NULL CONSTRAINT DF_AppSpReg_Published DEFAULT (1),
        DefinitionHash          NVARCHAR(64)   NULL,
        AiAnalyzedAt            DATETIME2      NULL,
        AiModel                 NVARCHAR(100)  NULL,
        AppCreatedDate          DATETIME2      NOT NULL CONSTRAINT DF_AppSpReg_Created DEFAULT (SYSUTCDATETIME()),
        AppModifiedDate         DATETIME2      NOT NULL CONSTRAINT DF_AppSpReg_Modified DEFAULT (SYSUTCDATETIME()),
        AppCreatedById          INT            NULL,
        AppModifiedById         INT            NULL,
        CONSTRAINT PK_AppStoredProcedureRegister PRIMARY KEY (Id),
        CONSTRAINT UQ_AppStoredProcedureRegister_DsSchemaName UNIQUE (DataSourceRegisterId, SchemaName, SpName)
    );

    CREATE INDEX IX_AppStoredProcedureRegister_DsPublished
        ON dbo.AppStoredProcedureRegister (DataSourceRegisterId, IsPublishedToAgent)
        INCLUDE (SpName, SchemaName, FullName);
END
GO

-- Agent tool copy: search/detail use AI register (published only)
UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Search AI-registered stored procedures (AppStoredProcedureRegister) by keyword against name, description, input params, and output columns. Only IsPublishedToAgent rows. Required: query. Optional dataSourceId to limit to one DataSource. Then stored_procedure_detail → stored_procedure_execute. Prefer this over guessing SP names from the live database.'
WHERE LibraryKey = N'platform-database' AND ToolName = N'stored_procedure_search';
GO

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'Get one AI-registered stored procedure from AppStoredProcedureRegister: description, input parameters JSON, output columns, sample JSON, usage. Required: procedureName (optionally schema.Name). Optional dataSourceId. Only published register rows. Call before stored_procedure_execute.'
WHERE LibraryKey = N'platform-database' AND ToolName = N'stored_procedure_detail';
GO

UPDATE dbo.AppAgentLibraryTool
SET ToolDescription = N'List AI-registered stored procedures (published) for a DataSource from AppStoredProcedureRegister — not the live sys.procedures catalog. Optional dataSourceId = DataSourceRegisterId; omit for session default. Prefer stored_procedure_search for keyword discovery.'
WHERE LibraryKey = N'platform-database' AND ToolName = N'stored_procedure_list';
GO
