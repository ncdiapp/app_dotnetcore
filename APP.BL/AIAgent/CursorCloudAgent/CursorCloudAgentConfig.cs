using System;
using System.IO;
using APP.Components.Dto;
using APP.Framework;
using App.BL;

namespace App.BL.CursorCloudAgent
{
    public static class CursorCloudAgentConfig
    {
        /// <summary>Tenant AIConfigCursorApiKey only (removed from appsettings.json).</summary>
        public static string ApiKey
        {
            get
            {
                var fromTenant = AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigCursorApiKey);
                return string.IsNullOrWhiteSpace(fromTenant) ? "" : fromTenant.Trim();
            }
        }

        public static string ApiBaseUrl => (AppConfig.Get("Cursor.ApiBaseUrl") ?? "https://api.cursor.com").TrimEnd('/');

        /// <summary>
        /// Tenant AIConfigCursorModel when the row exists (blank → auto).
        /// If the key is absent from tenant DB, fall back to appsettings, then auto.
        /// </summary>
        public static string ModelId
        {
            get
            {
                var fromTenant = AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigCursorModel);
                if (fromTenant != null)
                    return string.IsNullOrWhiteSpace(fromTenant) ? "auto" : fromTenant.Trim();
                var fromJson = AppConfig.Get("Cursor.ModelId");
                return string.IsNullOrWhiteSpace(fromJson) ? "auto" : fromJson.Trim();
            }
        }

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
        public static int SqlPreviewRowLimit => ParseInt(AppConfig.Get("Cursor.SqlPreviewRowLimit"), 50);
        public static int SqlProbeSampleRows => ParseInt(AppConfig.Get("Cursor.SqlProbeSampleRows"), 5);
        public static int MaxWorkspaceFileMb => ParseInt(AppConfig.Get("Cursor.MaxWorkspaceFileMb"), 20);
        /// <summary>
        /// Max UTF-8 KB per write_workspace_file / append_workspace_file call.
        /// Prefer single MCP write only for files at or under this size; larger files should use Cursor artifacts.
        /// </summary>
        public static int MaxWorkspaceChunkKb => ParseInt(AppConfig.Get("Cursor.MaxWorkspaceChunkKb"), 100);

        /// <summary>Preferred max size for a single stable MCP write (same default as MaxWorkspaceChunkKb).</summary>
        public static int PreferredMcpWriteKb => ParseInt(AppConfig.Get("Cursor.PreferredMcpWriteKb"), MaxWorkspaceChunkKb);
        /// <summary>How long to poll Cursor GetRun after SSE stream ends (long MCP/tool runs).</summary>
        public static int RunRecoveryMaxMinutes => ParseInt(AppConfig.Get("Cursor.RunRecoveryMaxMinutes"), 30);
        public static int RunRecoveryPollSeconds => ParseInt(AppConfig.Get("Cursor.RunRecoveryPollSeconds"), 3);
        public static int HttpClientTimeoutMinutes => RunRecoveryMaxMinutes + 3;

        /// <summary>Tenant AIConfigCursorMcpPublicBaseUrl first; fallback appsettings Cursor:McpPublicBaseUrl.</summary>
        public static string McpPublicBaseUrl
        {
            get
            {
                var fromTenant = AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigCursorMcpPublicBaseUrl);
                if (!string.IsNullOrWhiteSpace(fromTenant))
                    return fromTenant.Trim().TrimEnd('/');
                return AppConfig.Get("Cursor.McpPublicBaseUrl")?.Trim().TrimEnd('/') ?? "";
            }
        }

        public static string WorkspaceRootAbsolute
        {
            get
            {
                var root = AppConfig.Get("Cursor.WorkspaceRoot") ?? "App_Data/AiAgentWorkspace";
                if (!Path.IsPathRooted(root))
                    root = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), root));
                TryMigrateLegacyWorkspaceRoot(root);
                return root;
            }
        }

        /// <summary>
        /// Renames App_Data/CursorWorkspace → AiAgentWorkspace when the configured path
        /// is the new default and the legacy folder still exists beside it.
        /// </summary>
        private static void TryMigrateLegacyWorkspaceRoot(string configuredRoot)
        {
            try
            {
                if (Directory.Exists(configuredRoot)) return;
                var parent = Path.GetDirectoryName(configuredRoot);
                if (string.IsNullOrEmpty(parent)) return;
                var legacy = Path.Combine(parent, "CursorWorkspace");
                if (!Directory.Exists(legacy)) return;
                if (!string.Equals(Path.GetFileName(configuredRoot), "AiAgentWorkspace", StringComparison.OrdinalIgnoreCase))
                    return;
                Directory.Move(legacy, configuredRoot);
            }
            catch
            {
                // Best-effort; callers create the folder if missing.
            }
        }

        private static int ParseInt(string value, int fallback)
        {
            int parsed;
            return int.TryParse(value, out parsed) && parsed > 0 ? parsed : fallback;
        }
    }
}
