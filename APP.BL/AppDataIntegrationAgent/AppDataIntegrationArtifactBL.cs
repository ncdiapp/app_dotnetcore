using System;
using System.Text.RegularExpressions;

namespace App.BL.AppDataIntegrationAgent
{
    public static class AppDataIntegrationArtifactBL
    {
        /// <summary>
        /// Paths that must not be synced from Cursor artifacts into the user workspace.
        /// </summary>
        public static bool ShouldSyncArtifactPath(string relativePath)
        {
            var p = Normalize(relativePath);
            if (string.IsNullOrEmpty(p)) return false;

            if (p.StartsWith("source/mcp_results/", StringComparison.OrdinalIgnoreCase)
                || p.Equals("source/mcp_results", StringComparison.OrdinalIgnoreCase))
                return false;

            if (p.Equals("source/sql_cache.json", StringComparison.OrdinalIgnoreCase)
                || p.Equals("source/build_sql_cache.py", StringComparison.OrdinalIgnoreCase))
                return false;

            if (Regex.IsMatch(p, @"^source/gen_plmdw_\d+\.py$", RegexOptions.IgnoreCase)
                || Regex.IsMatch(p, @"^source/.*_cache\.py$", RegexOptions.IgnoreCase))
                return false;

            if (p.StartsWith("artifacts/bin/", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        private static string Normalize(string path)
        {
            return (path ?? "").Replace('\\', '/').Trim().TrimStart('/');
        }
    }
}
