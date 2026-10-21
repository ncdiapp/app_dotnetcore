using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using APP.Framework;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>Rolling summary of the oldest turns of one chat (Layer 1 working memory).</summary>
    public sealed class RollingSummary
    {
        public string Text        { get; set; } = "";
        public int    TurnCount   { get; set; }
        public string Fingerprint { get; set; } = "";
    }

    /// <summary>
    /// Persists <see cref="RollingSummary"/> on dbo.AppGenericAgentSession (V054 columns).
    /// Failures are logged and never fail a chat run: without a stored summary the history is simply rebuilt.
    /// </summary>
    public static class AgentSessionSummaryBL
    {
        private const string EnsureColumnsSql = @"
IF COL_LENGTH('dbo.AppGenericAgentSession', 'SummaryText') IS NULL
    ALTER TABLE dbo.AppGenericAgentSession ADD SummaryText NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.AppGenericAgentSession', 'SummarizedTurnCount') IS NULL
    ALTER TABLE dbo.AppGenericAgentSession ADD SummarizedTurnCount INT NOT NULL
        CONSTRAINT DF_AppGenericAgentSession_SummarizedTurnCount DEFAULT 0;
IF COL_LENGTH('dbo.AppGenericAgentSession', 'SummaryFingerprint') IS NULL
    ALTER TABLE dbo.AppGenericAgentSession ADD SummaryFingerprint NVARCHAR(64) NULL;";

        public static RollingSummary Load(string sessionKey, string skillKey, int userId, int dataSourceId)
        {
            if (string.IsNullOrWhiteSpace(sessionKey) || string.IsNullOrWhiteSpace(skillKey) || userId <= 0)
                return null;
            try
            {
                var fixture = Fixture(dataSourceId);
                if (fixture == null) return null;
                EnsureColumns(fixture);

                var dt = fixture.RetriveDataTable(
                    @"SELECT SummaryText, SummarizedTurnCount, SummaryFingerprint
                      FROM dbo.AppGenericAgentSession
                      WHERE SessionKey=@K AND SkillKey=@S AND UserId=@U",
                    new List<DbParameter>
                    {
                        P(fixture, "@K", sessionKey.Trim()),
                        P(fixture, "@S", skillKey.Trim()),
                        P(fixture, "@U", userId)
                    });
                if (dt == null || dt.Rows.Count == 0) return null;

                var row  = dt.Rows[0];
                var text = row["SummaryText"] == DBNull.Value ? null : row["SummaryText"].ToString();
                if (string.IsNullOrWhiteSpace(text)) return null;
                return new RollingSummary
                {
                    Text        = text,
                    TurnCount   = row["SummarizedTurnCount"] == DBNull.Value ? 0 : Convert.ToInt32(row["SummarizedTurnCount"]),
                    Fingerprint = row["SummaryFingerprint"] == DBNull.Value ? "" : row["SummaryFingerprint"].ToString()
                };
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(Load));
                return null;
            }
        }

        public static void Save(string sessionKey, string skillKey, int userId, int dataSourceId, RollingSummary summary)
        {
            if (summary == null || string.IsNullOrWhiteSpace(sessionKey) || string.IsNullOrWhiteSpace(skillKey) || userId <= 0)
                return;
            Write(sessionKey, skillKey, userId, dataSourceId, summary.Text, summary.TurnCount, summary.Fingerprint);
        }

        /// <summary>Drop the summary (Clear conversation).</summary>
        public static void Reset(string sessionKey, string skillKey, int userId, int dataSourceId = 0)
        {
            if (string.IsNullOrWhiteSpace(sessionKey) || string.IsNullOrWhiteSpace(skillKey) || userId <= 0) return;
            Write(sessionKey, skillKey, userId, dataSourceId, null, 0, null);
        }

        private static void Write(string sessionKey, string skillKey, int userId, int dataSourceId,
            string text, int turnCount, string fingerprint)
        {
            try
            {
                var fixture = Fixture(dataSourceId);
                if (fixture == null) return;
                EnsureColumns(fixture);

                fixture.ExecuteNonQueryResult(
                    @"UPDATE dbo.AppGenericAgentSession
                      SET SummaryText=@T, SummarizedTurnCount=@C, SummaryFingerprint=@F
                      WHERE SessionKey=@K AND SkillKey=@S AND UserId=@U",
                    new List<DbParameter>
                    {
                        P(fixture, "@T", text),
                        P(fixture, "@C", turnCount),
                        P(fixture, "@F", fingerprint),
                        P(fixture, "@K", sessionKey.Trim()),
                        P(fixture, "@S", skillKey.Trim()),
                        P(fixture, "@U", userId)
                    });
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(Write));
            }
        }

        private static void EnsureColumns(DatabaseSchemaMrg.DatabaseFixture fixture)
        {
            try { fixture.ExecuteNonQueryResult(EnsureColumnsSql, new List<DbParameter>()); }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(EnsureColumns));
            }
        }

        private static DatabaseSchemaMrg.DatabaseFixture Fixture(int dataSourceId)
        {
            var id = dataSourceId > 0 ? dataSourceId : AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            return id.HasValue && id.Value > 0 ? AppCacheManagerBL.GetOneDatabaseFixture(id.Value) : null;
        }

        private static DbParameter P(DatabaseSchemaMrg.DatabaseFixture f, string name, object value)
        {
            var p = f.CreateParameter(name);
            p.Value = value ?? DBNull.Value;
            return p;
        }
    }
}
