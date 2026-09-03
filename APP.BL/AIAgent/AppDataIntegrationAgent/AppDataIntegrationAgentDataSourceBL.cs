using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using App.BL;
using APP.Components.EntityDto;
using Newtonsoft.Json.Linq;

namespace App.BL.AppDataIntegrationAgent
{
    /// <summary>
    /// Data-source access for the App Data Integration Agent:
    /// 1) User supplies connectionString → direct DB access (validated).
    /// 2) Tenant boundary: MasterDB AppDataSourceRegister where DataSourceOwnerCompanyId = CurrentCompanyId
    ///    (same list as System Settings → Database Registration for the logged-in tenant).
    /// </summary>
    public static class AppDataIntegrationAgentDataSourceBL
    {
        public static List<AppDataIntegrationAgentDataSourceItemDto> ListForSession(
            AppDataIntegrationAgentSessionStore.SessionData session)
        {
            return ListTenantCompanyDataSources();
        }

        public static List<AppDataIntegrationAgentDataSourceItemDto> ListTenantCompanyDataSources()
        {
            return MapIdsToListItems(GetTenantCompanyAccessibleIds());
        }

        public static int? NormalizeSessionDataSource(int? requestedDataSourceRegisterId)
        {
            if (!requestedDataSourceRegisterId.HasValue || requestedDataSourceRegisterId.Value <= 0)
                return null;
            if (!IsTenantCompanyAccessible(requestedDataSourceRegisterId.Value))
                throw new ArgumentException(
                    "DataSourceRegisterId " + requestedDataSourceRegisterId.Value
                    + " is not registered for the current tenant company (MasterDB DataSourceOwnerCompanyId).");
            return requestedDataSourceRegisterId.Value;
        }

        public static int ResolveForTool(
            AppDataIntegrationAgentSessionStore.SessionData session,
            int? requestedDataSourceRegisterId)
        {
            var allowed = GetTenantCompanyAccessibleIds();
            if (requestedDataSourceRegisterId.HasValue && requestedDataSourceRegisterId.Value > 0)
            {
                if (!allowed.Contains(requestedDataSourceRegisterId.Value))
                    throw new InvalidOperationException(FormatDeniedMessage(requestedDataSourceRegisterId.Value, allowed));
                return requestedDataSourceRegisterId.Value;
            }

            if (session?.DataSourceRegisterId.HasValue == true
                && allowed.Contains(session.DataSourceRegisterId.Value))
                return session.DataSourceRegisterId.Value;

            if (allowed.Count == 1)
                return allowed.First();

            if (allowed.Count == 0)
                throw new InvalidOperationException(
                    "No data sources are registered for this tenant company. Use Database Registration in System Settings,"
                    + " list_datasources, or ask the user for an explicit connection string.");

            throw new InvalidOperationException(
                "dataSourceRegisterId is required. Call list_datasources and use one of: "
                + string.Join(", ", allowed.OrderBy(i => i)));
        }

        public static AppDataIntegrationAgentSqlTarget ResolveSqlTarget(
            AppDataIntegrationAgentSessionStore.SessionData session,
            JObject args)
        {
            var connectionString = ExtractConnectionString(args);
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                ValidateDirectConnection(connectionString);
                var target = new AppDataIntegrationAgentSqlTarget { ConnectionString = connectionString };
                NoteSqlRunTarget(session, target);
                return target;
            }

            var dsId = ResolveForTool(session, ExtractDataSourceRegisterId(args));
            var regTarget = new AppDataIntegrationAgentSqlTarget { DataSourceRegisterId = dsId };
            NoteSqlRunTarget(session, regTarget);
            return regTarget;
        }

        public static bool ArgsHaveConnectionString(JObject args)
        {
            return !string.IsNullOrWhiteSpace(ExtractConnectionString(args));
        }

        public static bool ShouldSkipSqlWorkbenchOpen(
            AppDataIntegrationAgentSessionStore.SessionData session,
            JObject args)
        {
            return ArgsHaveConnectionString(args)
                || (session != null && session.CurrentTurnSqlViaConnectionString);
        }

        public static void NoteSqlRunTarget(
            AppDataIntegrationAgentSessionStore.SessionData session,
            AppDataIntegrationAgentSqlTarget target)
        {
            if (session == null || target == null) return;
            if (target.UsesConnectionString)
                session.CurrentTurnSqlViaConnectionString = true;
        }

        public static void EnsureTablePreviewDataSources(
            AppDataIntegrationAgentSessionStore.SessionData session,
            List<AppDataIntegrationAgentTablePreviewItemDto> tables)
        {
            if (tables == null || tables.Count == 0) return;
            foreach (var t in tables)
            {
                if (t == null) continue;
                var ds = t.DataSourceId;
                if (!ds.HasValue || ds.Value <= 0)
                    ds = session?.DataSourceRegisterId;
                if (!ds.HasValue || ds.Value <= 0)
                    throw new InvalidOperationException("dataSourceRegisterId is required for table preview.");
                t.DataSourceId = ResolveForTool(session, ds);
            }
        }

        private static HashSet<int> GetTenantCompanyAccessibleIds()
        {
            return AppDataSourceRegisterBL.GetTenantAccessibleDataSourceIds();
        }

        private static bool IsTenantCompanyAccessible(int dataSourceRegisterId)
        {
            return AppDataSourceRegisterBL.IsTenantAccessibleDataSource(dataSourceRegisterId);
        }

        private static List<AppDataIntegrationAgentDataSourceItemDto> MapIdsToListItems(HashSet<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return new List<AppDataIntegrationAgentDataSourceItemDto>();

            return AppDataSourceRegisterBL.GetDataSourceRegisterList()
                .Select(d => new { Dto = d, Id = ParseRegisterId(d.Id) })
                .Where(x => x.Id.HasValue && ids.Contains(x.Id.Value))
                .OrderBy(x => x.Id)
                .Select(x => new AppDataIntegrationAgentDataSourceItemDto
                {
                    Id = x.Id.Value,
                    Name = x.Dto.DataSourceName,
                    DatabaseName = x.Dto.DatabaseName
                })
                .ToList();
        }

        private static string ExtractConnectionString(JObject args)
        {
            if (args == null) return null;
            var s = (string)args["connectionString"] ?? (string)args["connection_string"];
            return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        }

        private static int? ExtractDataSourceRegisterId(JObject args)
        {
            if (args == null) return null;
            var raw = args["dataSourceRegisterId"] ?? args["dataSourceId"];
            if (raw == null || raw.Type == JTokenType.Null) return null;
            if (raw.Type == JTokenType.Integer) return raw.Value<int>();
            int parsed;
            return int.TryParse(raw.ToString(), out parsed) ? parsed : null;
        }

        private static void ValidateDirectConnection(string connectionString)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "connectionString is invalid or the database is unreachable: " + ex.Message);
            }
        }

        private static int? ParseRegisterId(object id)
        {
            if (id == null) return null;
            if (id is int i) return i;
            if (int.TryParse(id.ToString(), out var parsed)) return parsed;
            return null;
        }

        private static string FormatDeniedMessage(int deniedId, HashSet<int> allowed)
        {
            var allowedText = allowed.Count == 0
                ? "(none for this tenant company)"
                : string.Join(", ", allowed.OrderBy(i => i));
            return "DataSourceRegisterId " + deniedId
                + " is not allowed for the current tenant company."
                + " MasterDB rule: AppDataSourceRegister.DataSourceOwnerCompanyId must match CurrentCompanyId"
                + " (see System Settings → Database Registration)."
                + " Allowed ids: " + allowedText
                + ". Or use connectionString when the user provided an explicit database connection string.";
        }
    }
}
