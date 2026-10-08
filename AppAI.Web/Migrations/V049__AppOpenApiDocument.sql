-- OpenAPI Documents. Membership is ActionCode only; no foreign keys.
-- Idempotent.

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = N'dbo' AND TABLE_NAME = N'AppOpenApiDocument'
)
BEGIN
    CREATE TABLE dbo.AppOpenApiDocument (
        Id               INT IDENTITY(1,1) NOT NULL,
        Name             NVARCHAR(200)  NOT NULL,
        Code             NVARCHAR(100)  NOT NULL,
        Version          NVARCHAR(40)   NULL,
        Description      NVARCHAR(1000) NULL,
        Status           NVARCHAR(20)   NOT NULL CONSTRAINT DF_AppOpenApiDocument_Status DEFAULT (N'Draft'),
        OpenApiJson      NVARCHAR(MAX)  NULL,
        ApiCount         INT            NOT NULL CONSTRAINT DF_AppOpenApiDocument_ApiCount DEFAULT (0),
        LastGenerated    DATETIME2      NULL,
        AppCreatedByID   INT            NULL,
        AppCreatedDate   DATETIME2      NOT NULL CONSTRAINT DF_AppOpenApiDocument_Created DEFAULT (SYSUTCDATETIME()),
        AppModifiedByID  INT            NULL,
        AppModifiedDate  DATETIME2      NOT NULL CONSTRAINT DF_AppOpenApiDocument_Modified DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AppOpenApiDocument PRIMARY KEY (Id),
        CONSTRAINT UQ_AppOpenApiDocument_Code UNIQUE (Code)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = N'dbo' AND TABLE_NAME = N'AppOpenApiDocumentMember'
)
BEGIN
    CREATE TABLE dbo.AppOpenApiDocumentMember (
        Id          INT IDENTITY(1,1) NOT NULL,
        DocumentId  INT           NOT NULL,
        ActionCode  NVARCHAR(200) NOT NULL,
        CONSTRAINT PK_AppOpenApiDocumentMember PRIMARY KEY (Id),
        CONSTRAINT UQ_AppOpenApiDocumentMember_DocCode UNIQUE (DocumentId, ActionCode)
    );
END
GO
