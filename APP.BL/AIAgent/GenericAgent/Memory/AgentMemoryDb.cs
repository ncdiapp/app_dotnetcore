using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using APP.Framework;

namespace App.BL.AIAgent.GenericAgent.Memory
{
    /// <summary>
    /// Shared data access for the memory tables (tenant DB). Mirrors migration V055; the schema is also
    /// ensured once per data source at runtime so tenants that have not been migrated yet keep working.
    /// </summary>
    internal static class AgentMemoryDb
    {
        private static readonly ConcurrentDictionary<int, bool> Ensured = new();

        private const string SchemaSql = @"
IF OBJECT_ID(N'dbo.AppAgentMemoryFact', N'U') IS NULL
CREATE TABLE dbo.AppAgentMemoryFact
(
    FactId           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppAgentMemoryFact PRIMARY KEY,
    UserId           INT           NOT NULL,
    SkillKey         NVARCHAR(100) NULL,
    Category         NVARCHAR(30)  NOT NULL CONSTRAINT DF_AppAgentMemoryFact_Category DEFAULT N'note',
    FactKey          NVARCHAR(100) NOT NULL,
    FactValue        NVARCHAR(1000) NOT NULL,
    PreviousValue    NVARCHAR(1000) NULL,
    Status           NVARCHAR(20)  NOT NULL CONSTRAINT DF_AppAgentMemoryFact_Status DEFAULT N'confirmed',
    Source           NVARCHAR(20)  NOT NULL CONSTRAINT DF_AppAgentMemoryFact_Source DEFAULT N'user',
    Confidence       FLOAT         NULL,
    SourceSessionKey NVARCHAR(200) NULL,
    IsActive         BIT           NOT NULL CONSTRAINT DF_AppAgentMemoryFact_IsActive DEFAULT 1,
    UpdatedAt        DATETIME2     NOT NULL CONSTRAINT DF_AppAgentMemoryFact_UpdatedAt DEFAULT GETUTCDATE()
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_AppAgentMemoryFact_UserSkillKey' AND object_id = OBJECT_ID(N'dbo.AppAgentMemoryFact'))
CREATE UNIQUE INDEX UX_AppAgentMemoryFact_UserSkillKey ON dbo.AppAgentMemoryFact (UserId, SkillKey, FactKey);

IF OBJECT_ID(N'dbo.AppAgentEpisode', N'U') IS NULL
CREATE TABLE dbo.AppAgentEpisode
(
    EpisodeId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppAgentEpisode PRIMARY KEY,
    UserId         INT           NOT NULL,
    SkillKey       NVARCHAR(100) NOT NULL,
    SessionKey     NVARCHAR(200) NULL,
    Summary        NVARCHAR(MAX) NOT NULL,
    Topics         NVARCHAR(500) NULL,
    Embedding      VARBINARY(MAX) NULL,
    EmbeddingModel NVARCHAR(80)  NULL,
    CreatedAt      DATETIME2     NOT NULL CONSTRAINT DF_AppAgentEpisode_CreatedAt DEFAULT GETUTCDATE()
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppAgentEpisode_UserSkill' AND object_id = OBJECT_ID(N'dbo.AppAgentEpisode'))
CREATE INDEX IX_AppAgentEpisode_UserSkill ON dbo.AppAgentEpisode (UserId, SkillKey);

IF OBJECT_ID(N'dbo.AppGenericAgentSession', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.AppGenericAgentSession', 'ExtractedAt') IS NULL
        ALTER TABLE dbo.AppGenericAgentSession ADD ExtractedAt DATETIME2 NULL;
    IF COL_LENGTH('dbo.AppGenericAgentSession', 'ExtractedTurnCount') IS NULL
        ALTER TABLE dbo.AppGenericAgentSession ADD ExtractedTurnCount INT NOT NULL
            CONSTRAINT DF_AppGenericAgentSession_ExtractedTurnCount DEFAULT 0;
    IF COL_LENGTH('dbo.AppGenericAgentSession', 'ExtractingAt') IS NULL
        ALTER TABLE dbo.AppGenericAgentSession ADD ExtractingAt DATETIME2 NULL;
    IF COL_LENGTH('dbo.AppGenericAgentSession', 'ExtractRetryCount') IS NULL
        ALTER TABLE dbo.AppGenericAgentSession ADD ExtractRetryCount INT NOT NULL
            CONSTRAINT DF_AppGenericAgentSession_ExtractRetryCount DEFAULT 0;
    IF COL_LENGTH('dbo.AppGenericAgentSession', 'MemoryOptOut') IS NULL
        ALTER TABLE dbo.AppGenericAgentSession ADD MemoryOptOut BIT NOT NULL
            CONSTRAINT DF_AppGenericAgentSession_MemoryOptOut DEFAULT 0;
END";

        public static DatabaseSchemaMrg.DatabaseFixture Fixture(int dataSourceId)
        {
            var id = dataSourceId > 0 ? dataSourceId : AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            if (!id.HasValue || id.Value <= 0) return null;
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(id.Value);
            if (fixture != null) EnsureSchema(fixture, id.Value);
            return fixture;
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
                Ensured[dataSourceId] = true;
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, "AgentMemoryDb.EnsureSchema ds={0}", dataSourceId);
            }
        }
    }
}
