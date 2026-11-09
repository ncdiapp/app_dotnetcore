using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using APP.Framework;

namespace App.BL.AIAgent.GenericAgent.VectorStore
{
    /// <summary>Tenant vector tables. Mirrors migration V075 (includes Kind/SourceKey); also ensured at runtime.</summary>
    internal static class AppVectorDb
    {
        private static readonly ConcurrentDictionary<int, bool> Ensured = new();

        private const string SchemaSql = @"
IF OBJECT_ID(N'dbo.AppVectorCollection', N'U') IS NULL
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
IF OBJECT_ID(N'dbo.AppVectorCollection', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.AppVectorCollection', N'Kind') IS NULL
    ALTER TABLE dbo.AppVectorCollection ADD Kind NVARCHAR(40) NOT NULL CONSTRAINT DF_AppVectorCollection_Kind DEFAULT N'entity';
IF OBJECT_ID(N'dbo.AppVectorCollection', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.AppVectorCollection', N'SourceKey') IS NULL
    ALTER TABLE dbo.AppVectorCollection ADD SourceKey NVARCHAR(200) NULL;

IF OBJECT_ID(N'dbo.AppVectorEmbedding', N'U') IS NULL
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
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppVectorEmbedding_Collection' AND object_id = OBJECT_ID(N'dbo.AppVectorEmbedding'))
CREATE INDEX IX_AppVectorEmbedding_Collection ON dbo.AppVectorEmbedding (CollectionId);

IF OBJECT_ID(N'dbo.AppVectorImageEmbedding', N'U') IS NULL
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
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppVectorImageEmbedding_Collection' AND object_id = OBJECT_ID(N'dbo.AppVectorImageEmbedding'))
CREATE INDEX IX_AppVectorImageEmbedding_Collection ON dbo.AppVectorImageEmbedding (CollectionId);
";

        private const string KindPatchSql = @"
IF OBJECT_ID(N'dbo.AppVectorCollection', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.AppVectorCollection
    SET Kind = ISNULL(NULLIF(LTRIM(RTRIM(Kind)), N''), N'entity')
    WHERE Kind IS NULL OR LTRIM(RTRIM(Kind)) = N'';
    UPDATE dbo.AppVectorCollection
    SET SourceKey = CASE
        WHEN SourceKey IS NOT NULL AND LTRIM(RTRIM(SourceKey)) <> N'' THEN SourceKey
        WHEN Name LIKE N'entity-%' AND LEN(Name) > 7 THEN SUBSTRING(Name, 8, 200)
        ELSE Name
    END
    WHERE SourceKey IS NULL OR LTRIM(RTRIM(SourceKey)) = N'';
END
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_AppVectorCollection_NameProviderModel' AND object_id = OBJECT_ID(N'dbo.AppVectorCollection'))
    DROP INDEX UX_AppVectorCollection_NameProviderModel ON dbo.AppVectorCollection;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_AppVectorCollection_KindSourceProviderModel' AND object_id = OBJECT_ID(N'dbo.AppVectorCollection'))
CREATE UNIQUE INDEX UX_AppVectorCollection_KindSourceProviderModel
    ON dbo.AppVectorCollection (Kind, SourceKey, ModelProvider, EmbeddingModel)
    WHERE SourceKey IS NOT NULL;";

        public static DatabaseSchemaMrg.DatabaseFixture Fixture(int dataSourceId)
        {
            var id = dataSourceId > 0 ? dataSourceId : AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            if (!id.HasValue || id.Value <= 0) return null;
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(id.Value);
            if (fixture != null) EnsureSchema(fixture, id.Value);
            return fixture;
        }

        public static int ResolveDataSourceId(int dataSourceId)
        {
            if (dataSourceId > 0) return dataSourceId;
            return AppDataSourceRegisterBL.GetDefaultDataSourceRegId() ?? 0;
        }

        public static DbParameter P(DatabaseSchemaMrg.DatabaseFixture f, string name, object value)
        {
            var p = f.CreateParameter(name);
            p.Value = value ?? DBNull.Value;
            return p;
        }

        public static List<DbParameter> Params(DatabaseSchemaMrg.DatabaseFixture f, params (string Name, object Value)[] items)
        {
            var list = new List<DbParameter>(items.Length);
            foreach (var (name, value) in items) list.Add(P(f, name, value));
            return list;
        }

        private static void EnsureSchema(DatabaseSchemaMrg.DatabaseFixture fixture, int dataSourceId)
        {
            if (Ensured.ContainsKey(dataSourceId)) return;
            try
            {
                fixture.ExecuteNonQueryResult(SchemaSql, new List<DbParameter>());
                fixture.ExecuteNonQueryResult(KindPatchSql, new List<DbParameter>());
                Ensured[dataSourceId] = true;
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, "AppVectorDb.EnsureSchema ds={0}", dataSourceId);
            }
        }
    }
}
