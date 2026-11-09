-- V075: Tenant vector store for Entity Lookup semantic resolve (and future corpora).
-- Authoritative vector column is VARBINARY(MAX). Native SQL vector columns are optional later (SQL 2025+).
-- Collections are classified by Kind + SourceKey (entity / help / user / rag / …).
-- Keep DDL in sync with App.BL.AIAgent.GenericAgent.VectorStore.AppVectorDb.EnsureSchema.

IF OBJECT_ID(N'dbo.AppVectorEmbedding', N'U') IS NOT NULL
    DROP TABLE dbo.AppVectorEmbedding;
GO
IF OBJECT_ID(N'dbo.AppVectorImageEmbedding', N'U') IS NOT NULL
    DROP TABLE dbo.AppVectorImageEmbedding;
GO
IF OBJECT_ID(N'dbo.AppVectorCollection', N'U') IS NOT NULL
    DROP TABLE dbo.AppVectorCollection;
GO

CREATE TABLE dbo.AppVectorCollection
(
    Id                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppVectorCollection PRIMARY KEY,
    Name                NVARCHAR(200)  NOT NULL,
    Kind                NVARCHAR(40)   NOT NULL CONSTRAINT DF_AppVectorCollection_Kind DEFAULT N'entity',
    SourceKey           NVARCHAR(200)  NULL,
    Dimension           INT            NOT NULL,
    ModelProvider       NVARCHAR(80)   NOT NULL,
    EmbeddingModel      NVARCHAR(120)  NOT NULL,
    ContentFingerprint  NVARCHAR(128)  NULL,
    UpdatedAt           DATETIME2      NOT NULL CONSTRAINT DF_AppVectorCollection_UpdatedAt DEFAULT GETUTCDATE(),
    CreatedAt           DATETIME2      NOT NULL CONSTRAINT DF_AppVectorCollection_CreatedAt DEFAULT GETUTCDATE()
);
GO

CREATE UNIQUE INDEX UX_AppVectorCollection_KindSourceProviderModel
    ON dbo.AppVectorCollection (Kind, SourceKey, ModelProvider, EmbeddingModel)
    WHERE SourceKey IS NOT NULL;
GO

CREATE TABLE dbo.AppVectorEmbedding
(
    Id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppVectorEmbedding PRIMARY KEY,
    CollectionId     INT            NOT NULL,
    Document         NVARCHAR(MAX)  NOT NULL,
    VectorData       VARBINARY(MAX) NOT NULL,
    Dimension        INT            NOT NULL,
    Metadata         NVARCHAR(MAX)  NULL,
    ModelProvider    NVARCHAR(80)   NOT NULL,
    EmbeddingModel   NVARCHAR(120)  NOT NULL,
    CreatedAt        DATETIME2      NOT NULL CONSTRAINT DF_AppVectorEmbedding_CreatedAt DEFAULT GETUTCDATE(),
    CONSTRAINT FK_AppVectorEmbedding_Collection FOREIGN KEY (CollectionId)
        REFERENCES dbo.AppVectorCollection (Id) ON DELETE CASCADE
);
GO

CREATE INDEX IX_AppVectorEmbedding_Collection ON dbo.AppVectorEmbedding (CollectionId);
GO

CREATE TABLE dbo.AppVectorImageEmbedding
(
    Id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppVectorImageEmbedding PRIMARY KEY,
    CollectionId      INT            NOT NULL,
    Document          NVARCHAR(2000) NOT NULL,
    VectorData        VARBINARY(MAX) NULL,
    Dimension         INT            NULL,
    ImageVectorData   VARBINARY(MAX) NULL,
    ImageDimension    INT            NULL,
    Metadata          NVARCHAR(MAX)  NULL,
    ModelProvider     NVARCHAR(80)   NOT NULL,
    EmbeddingModel    NVARCHAR(120)  NOT NULL,
    UpdatedAt         DATETIME2      NOT NULL CONSTRAINT DF_AppVectorImageEmbedding_UpdatedAt DEFAULT GETUTCDATE(),
    CreatedAt         DATETIME2      NOT NULL CONSTRAINT DF_AppVectorImageEmbedding_CreatedAt DEFAULT GETUTCDATE(),
    CONSTRAINT FK_AppVectorImageEmbedding_Collection FOREIGN KEY (CollectionId)
        REFERENCES dbo.AppVectorCollection (Id) ON DELETE CASCADE
);
GO

CREATE INDEX IX_AppVectorImageEmbedding_Collection ON dbo.AppVectorImageEmbedding (CollectionId);
GO
