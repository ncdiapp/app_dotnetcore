using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
#if NETFRAMEWORK
using System.Web;
#endif
using App.BL;

namespace App.BL
{
    // Applies versioned SQL migration scripts from the Migrations/ folder to a target DB.
    // Scripts must be named VNN__Description.sql (e.g. V001__InitialSchema.sql) and are applied
    // in lexicographic order.  Applied versions are tracked in _SchemaMigrations so the runner
    // is idempotent — safe to call on every deployment.
    public static class AppTenantMigrationRunnerBL
    {
        private const string CreateTrackingTable = @"
            IF OBJECT_ID('dbo._SchemaMigrations', 'U') IS NULL
            CREATE TABLE dbo._SchemaMigrations (
                Id        INT IDENTITY(1,1) PRIMARY KEY,
                Version   NVARCHAR(100) NOT NULL UNIQUE,
                Script    NVARCHAR(500) NOT NULL,
                AppliedAt DATETIME2    NOT NULL DEFAULT GETUTCDATE()
            );";

        // Run pending migrations against a single tenant connection string.
        // Returns count of newly applied scripts.
        public static int RunPendingMigrations(string tenantConnStr)
        {
            if (string.IsNullOrWhiteSpace(tenantConnStr))
                return 0;

            string folder = GetMigrationsFolder();
            if (!Directory.Exists(folder))
                return 0;

            var scripts = Directory.GetFiles(folder, "*.sql")
                                   .OrderBy(Path.GetFileName)
                                   .ToList();
            if (scripts.Count == 0)
                return 0;

            int applied = 0;

            using (var conn = new SqlConnection(tenantConnStr))
            {
                conn.Open();

                // Ensure tracking table exists
                ExecuteBatch(CreateTrackingTable, conn);

                foreach (var scriptPath in scripts)
                {
                    string version = Path.GetFileNameWithoutExtension(scriptPath);

                    // Skip already-applied versions
                    using (var check = new SqlCommand(
                        "SELECT COUNT(1) FROM dbo._SchemaMigrations WHERE Version = @v", conn))
                    {
                        check.Parameters.AddWithValue("@v", version);
                        if ((int)check.ExecuteScalar() > 0) continue;
                    }

                    // Execute every GO-separated batch in the script
                    string sql = File.ReadAllText(scriptPath);
                    foreach (var batch in SplitOnGo(sql))
                        ExecuteBatch(batch, conn);

                    // Record the applied version
                    using (var record = new SqlCommand(
                        "INSERT INTO dbo._SchemaMigrations (Version, Script) VALUES (@v, @s)", conn))
                    {
                        record.Parameters.AddWithValue("@v", version);
                        record.Parameters.AddWithValue("@s", Path.GetFileName(scriptPath));
                        record.ExecuteNonQuery();
                    }

                    applied++;
                }
            }

            return applied;
        }

        // Run pending migrations against every registered tenant DB.
        // Returns a map of DataSourceName → migrations applied (-1 on error).
        public static Dictionary<string, int> RunMigrationsOnAllTenants()
        {
            var results = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var tenants = AppDataSourceRegisterBL.RetrieveAllAppDataSourceRegisterEntity();

            foreach (var tenant in tenants)
            {
                string key = tenant.DataSourceName ?? tenant.DataSourceId.ToString();
                if (string.IsNullOrEmpty(tenant.ConnectionString))
                {
                    results[key] = 0;
                    continue;
                }

                try
                {
                    string plain = AppConnectionStringEncryptionBL.Decrypt(tenant.ConnectionString);
                    results[key] = RunPendingMigrations(plain);
                }
                catch
                {
                    results[key] = -1;
                }
            }

            return results;
        }

        private static string GetMigrationsFolder()
        {
#if NETFRAMEWORK
            // TODO-PHASE4: Replace with IHttpContextAccessor
            if (HttpContext.Current != null)
                return HttpContext.Current.Server.MapPath("~/Migrations");
#endif
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Migrations");
        }

        private static IEnumerable<string> SplitOnGo(string sql)
        {
            return Regex.Split(sql, @"^\s*GO\s*$",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);
        }

        private static void ExecuteBatch(string batch, SqlConnection conn)
        {
            if (string.IsNullOrWhiteSpace(batch)) return;
            using (var cmd = new SqlCommand(batch, conn) { CommandTimeout = 300 })
                cmd.ExecuteNonQuery();
        }
    }
}
