using System;
using System.IO;
using APP.Framework;

namespace App.BL.CursorAgent
{
    public static class CursorAgentConfig
    {
        public static string ApiKey => AppConfig.Get("Cursor.ApiKey")?.Trim() ?? "";
        public static string ApiBaseUrl => (AppConfig.Get("Cursor.ApiBaseUrl") ?? "https://api.cursor.com").TrimEnd('/');
        public static string ModelId => string.IsNullOrWhiteSpace(AppConfig.Get("Cursor.ModelId")) ? "auto" : AppConfig.Get("Cursor.ModelId").Trim();
        public static string RepoUrl => AppConfig.Get("Cursor.RepoUrl")?.Trim() ?? "";
        public static string RepoRef => string.IsNullOrWhiteSpace(AppConfig.Get("Cursor.RepoRef")) ? "main" : AppConfig.Get("Cursor.RepoRef").Trim();
        public static bool AttachRepo
        {
            get
            {
                var raw = AppConfig.Get("Cursor.AttachRepo");
                if (string.IsNullOrWhiteSpace(raw)) return true;
                return !string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase);
            }
        }
        public static bool AutoCreatePr => string.Equals(AppConfig.Get("Cursor.AutoCreatePr"), "true", StringComparison.OrdinalIgnoreCase);
        public static bool AdminOnly => !string.Equals(AppConfig.Get("Cursor.AdminOnly"), "false", StringComparison.OrdinalIgnoreCase);
        public static int SqlPreviewRowLimit => ParseInt(AppConfig.Get("Cursor.SqlPreviewRowLimit"), 200);
        public static int MaxWorkspaceFileMb => ParseInt(AppConfig.Get("Cursor.MaxWorkspaceFileMb"), 5);

        public static string McpPublicBaseUrl => AppConfig.Get("Cursor.McpPublicBaseUrl")?.Trim().TrimEnd('/') ?? "";

        public static string WorkspaceRootAbsolute
        {
            get
            {
                var root = AppConfig.Get("Cursor.WorkspaceRoot") ?? "App_Data/CursorWorkspace";
                if (!Path.IsPathRooted(root))
                    root = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), root));
                return root;
            }
        }

        private static int ParseInt(string value, int fallback)
        {
            int parsed;
            return int.TryParse(value, out parsed) && parsed > 0 ? parsed : fallback;
        }
    }
}
