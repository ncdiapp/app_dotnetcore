using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using APP.Framework;
using NLog;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Executes a .sql file from AgentOutput/{sessionKey}/ against a registered DataSource.
    /// Official PLM import scripts use three-part names (APP insert + PLM/plmDW/ERP select)
    /// and DB_ID() — they require every catalog on the same SQL Server instance as the target.
    /// </summary>
    public static class GenericAgentSqlFileBL
    {
        public const int DefaultBatchTimeoutSeconds = 600;

        private static readonly Regex GoBatchSplitter = new Regex(
            @"^\s*GO\s*(?:--.*)?\s*$",
            RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex DeclaredCatalogRegex = new Regex(
            @"DECLARE\s+@(Dw|Plm|Erp)Database\s+NVARCHAR\s*\(\s*\d+\s*\)\s*=\s*N'([^']+)'",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static AgentSqlFileExecuteResult Execute(
            string sessionKey,
            int companyId,
            string relativePath,
            int? dataSourceId,
            string requiredDataSourceIds)
        {
            var result = new AgentSqlFileExecuteResult { Path = NormalizeRel(relativePath) };
            var sw = Stopwatch.StartNew();
            var log = LogManager.GetCurrentClassLogger();
            try
            {
                if (!App.BL.AppSecurityUserBL.IsAdminUser())
                    throw new UnauthorizedAccessException("Executing agent SQL files requires SaasCompanyAdmin or SysAdmin.");
                if (string.IsNullOrWhiteSpace(sessionKey))
                    throw new InvalidOperationException("ChatSessionKey is required.");
                if (companyId <= 0)
                    throw new InvalidOperationException("CompanyId is required.");

                var path = result.Path;
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("relativePath is required (e.g. output/3359/3_PlmDw_ImportFromDW.sql).");
                if (path.IndexOf("..", StringComparison.Ordinal) >= 0)
                    throw new UnauthorizedAccessException("Path must stay under the chat AgentOutput folder.");
                if (!path.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Only .sql files can be executed.");

                var file = GenericAgentFileBL.ReadText(sessionKey, path, companyId);
                if (file == null || string.IsNullOrWhiteSpace(file.Content))
                    throw new InvalidOperationException("SQL file is empty or missing.");
                if (file.Truncated)
                    throw new InvalidOperationException("SQL file exceeds the 20 MB AgentOutput limit.");

                var targetId = ResolveTargetDataSourceId(dataSourceId);
                var targetFixture = App.BL.AppCacheManagerBL.GetOneDatabaseFixture(targetId);
                if (targetFixture == null || string.IsNullOrWhiteSpace(targetFixture.ConnectionString))
                    throw new InvalidOperationException("Could not resolve the target DataSource connection.");

                var targetBuilder = new SqlConnectionStringBuilder(targetFixture.ConnectionString);
                result.TargetServer = targetBuilder.DataSource;
                result.TargetCatalog = targetBuilder.InitialCatalog;

                var extraSources = ResolveRequiredSources(requiredDataSourceIds);
                AssertSameSqlServer(targetBuilder, extraSources);

                var declaredCatalogs = ParseDeclaredCatalogs(file.Content);
                foreach (var extra in extraSources)
                {
                    if (!string.IsNullOrWhiteSpace(extra.Catalog)
                        && !declaredCatalogs.Any(c => string.Equals(c, extra.Catalog, StringComparison.OrdinalIgnoreCase)))
                        declaredCatalogs.Add(extra.Catalog);
                }

                log.Info("execute_agent_sql_file start path={0} target={1}/{2} extraIds={3}",
                    path, result.TargetServer, result.TargetCatalog, requiredDataSourceIds);

                using (var conn = new SqlConnection(targetFixture.ConnectionString))
                {
                    conn.Open();
                    var visible = AssertCatalogsVisible(conn, declaredCatalogs);
                    result.Catalogs = visible;

                    var batches = SplitGoBatches(file.Content);
                    var executed = 0;
                    for (var i = 0; i < batches.Count; i++)
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = batches[i];
                        cmd.CommandTimeout = DefaultBatchTimeoutSeconds;
                        cmd.ExecuteNonQuery();
                        executed++;
                    }

                    result.Batches = executed;
                    result.Ok = true;
                    log.Info("execute_agent_sql_file ok path={0} batches={1} ms={2}", path, executed, sw.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.Error = ex.Message;
                log.Error(ex, "execute_agent_sql_file failed path={0}", result.Path);
            }

            sw.Stop();
            result.DurationMs = (int)sw.ElapsedMilliseconds;
            return result;
        }

        private static int ResolveTargetDataSourceId(int? dataSourceId)
        {
            if (dataSourceId.HasValue && dataSourceId.Value > 0)
                return dataSourceId.Value;

            if (ServerContext.Instance?.DataSourceId is int sessionDs && sessionDs > 0)
                return sessionDs;

            var fallback = App.BL.AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            if (fallback.HasValue && fallback.Value > 0)
                return fallback.Value;

            throw new InvalidOperationException("No APP tenant DataSourceId. Pass dataSourceId or complete Gate-0.");
        }

        private static List<RequiredSource> ResolveRequiredSources(string requiredDataSourceIds)
        {
            var list = new List<RequiredSource>();
            if (string.IsNullOrWhiteSpace(requiredDataSourceIds))
                return list;

            foreach (var part in requiredDataSourceIds.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!int.TryParse(part.Trim(), out var id) || id <= 0)
                    continue;
                var fixture = App.BL.AppCacheManagerBL.GetOneDatabaseFixture(id);
                if (fixture == null || string.IsNullOrWhiteSpace(fixture.ConnectionString))
                    throw new InvalidOperationException("Could not resolve required DataSourceId " + id + ".");
                var builder = new SqlConnectionStringBuilder(fixture.ConnectionString);
                list.Add(new RequiredSource
                {
                    DataSourceId = id,
                    Server = builder.DataSource,
                    Catalog = builder.InitialCatalog
                });
            }

            return list;
        }

        private static void AssertSameSqlServer(SqlConnectionStringBuilder target, List<RequiredSource> extras)
        {
            var targetServer = NormalizeServer(target.DataSource);
            foreach (var extra in extras)
            {
                var extraServer = NormalizeServer(extra.Server);
                if (string.IsNullOrWhiteSpace(extraServer) || string.Equals(extraServer, targetServer, StringComparison.OrdinalIgnoreCase))
                    continue;

                throw new InvalidOperationException(
                    "Cross-server SQL is not supported. Target APP is on '" + target.DataSource +
                    "' (catalog " + target.InitialCatalog + "), dataSourceId=" + extra.DataSourceId +
                    " is on '" + extra.Server + "' (catalog " + extra.Catalog + "). " +
                    "Official import scripts use three-part names and DB_ID(), which require APP / PLM / plmDW / ERP " +
                    "on the same SQL Server instance.");
            }
        }

        private static List<AgentSqlFileCatalogResult> AssertCatalogsVisible(SqlConnection conn, List<string> catalogs)
        {
            var visible = new List<AgentSqlFileCatalogResult>();
            foreach (var catalog in catalogs
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT DB_ID(@n)";
                cmd.CommandTimeout = 30;
                var p = cmd.CreateParameter();
                p.ParameterName = "@n";
                p.Value = catalog;
                cmd.Parameters.Add(p);
                var id = cmd.ExecuteScalar();
                var ok = id != null && id != DBNull.Value;
                visible.Add(new AgentSqlFileCatalogResult { Name = catalog, Visible = ok });
                if (!ok)
                {
                    throw new InvalidOperationException(
                        "Database '" + catalog + "' is not visible from the APP connection (DB_ID is NULL). " +
                        "Three-part names like [" + catalog + "].dbo.Table require the APP login to see that catalog " +
                        "on the same SQL Server. Grant access or keep PLM / plmDW / ERP on the same instance.");
                }
            }

            return visible;
        }

        private static List<string> ParseDeclaredCatalogs(string sql)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(sql))
                return list;

            foreach (Match match in DeclaredCatalogRegex.Matches(sql))
            {
                if (match.Groups.Count >= 3)
                {
                    var name = match.Groups[2].Value.Trim();
                    if (!string.IsNullOrWhiteSpace(name))
                        list.Add(name);
                }
            }

            return list;
        }

        private static List<string> SplitGoBatches(string script)
        {
            var batches = new List<string>();
            foreach (var raw in GoBatchSplitter.Split(script ?? ""))
            {
                var batch = raw.Trim();
                if (!string.IsNullOrWhiteSpace(batch))
                    batches.Add(batch);
            }

            if (batches.Count == 0)
                throw new InvalidOperationException("SQL file has no executable batches.");
            return batches;
        }

        private static string NormalizeRel(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return "";
            return relativePath.Trim().Replace('\\', '/').TrimStart('/');
        }

        private static string NormalizeServer(string dataSource)
        {
            if (string.IsNullOrWhiteSpace(dataSource))
                return "";

            var s = dataSource.Trim();
            if (s.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase))
                s = s.Substring(4);
            var comma = s.IndexOf(',');
            if (comma > 0)
                s = s.Substring(0, comma);
            if (s.Equals("(local)", StringComparison.OrdinalIgnoreCase)
                || s.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || s.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || s == ".")
                return "localhost";
            return s;
        }

        private sealed class RequiredSource
        {
            public int DataSourceId { get; set; }
            public string Server { get; set; }
            public string Catalog { get; set; }
        }
    }

    public sealed class AgentSqlFileExecuteResult
    {
        public bool Ok { get; set; }
        public string Path { get; set; }
        public int Batches { get; set; }
        public int DurationMs { get; set; }
        public string Error { get; set; }
        public string TargetServer { get; set; }
        public string TargetCatalog { get; set; }
        public List<AgentSqlFileCatalogResult> Catalogs { get; set; }
    }

    public sealed class AgentSqlFileCatalogResult
    {
        public string Name { get; set; }
        public bool Visible { get; set; }
    }
}
