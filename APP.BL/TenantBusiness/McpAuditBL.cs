using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace App.BL.TenantBusiness
{
    /// <summary>Tenant DB connection captured on the request thread so a background writer can use it later.</summary>
    public sealed record McpAuditTarget(string ConnectionString, string DatabaseName);

    /// <summary>One row of dbo.AppMcpAuditLog (V040__McpAuditLog.sql).</summary>
    public sealed record McpAuditRow(
        DateTime CreatedUtc,
        string EventCode,
        string Action,
        bool Success,
        int? CompanyId,
        int? UserId,
        string McpSessionId,
        string IpAddress,
        string CorrelationId,
        string AppSource,
        string HttpMethod,
        string ResourcePath,
        int? HttpStatus,
        string ErrorMessage,
        string AdditionalContext);

    // Audit trail of MCP gateway events, stored per tenant in dbo.AppMcpAuditLog.
    // The table is not an LLBLGen entity (like the other recent tenant tables), so rows are written with
    // parameterized SQL against the tenant connection captured from the registered identity.
    public static class McpAuditBL
    {
        private const string InsertSql =
            @"INSERT INTO {0}.dbo.AppMcpAuditLog
                (CreatedUtc, EventCode, Action, Success, CompanyId, UserId, McpSessionId, IpAddress, CorrelationId,
                 AppSource, HttpMethod, ResourcePath, HttpStatus, ErrorMessage, AdditionalContext)
              VALUES
                (@CreatedUtc, @EventCode, @Action, @Success, @CompanyId, @UserId, @McpSessionId, @IpAddress, @CorrelationId,
                 @AppSource, @HttpMethod, @ResourcePath, @HttpStatus, @ErrorMessage, @AdditionalContext)";

        // Call on the request thread, after the identity has been registered. Returns null when there is no
        // tenant context (the event then cannot be stored in a tenant DB).
        public static McpAuditTarget CaptureTarget()
        {
            try
            {
                var (connStr, dbName) = AppTenantAdapterBL.GetTenantConnectionInfo();
                return new McpAuditTarget(connStr, dbName);
            }
            catch (InvalidOperationException ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(ex, nameof(CaptureTarget));
                return null;
            }
        }

        // Writes the rows in one transaction. Throws on failure; the caller decides how to report it.
        public static async Task WriteAsync(McpAuditTarget target, IReadOnlyList<McpAuditRow> rows, CancellationToken cancellationToken)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (rows == null || rows.Count == 0) return;

            // Database name comes from the registered identity, not from a client; quote it anyway.
            string sql = string.Format(InsertSql, "[" + target.DatabaseName.Replace("]", "]]") + "]");

            using (var conn = new SqlConnection(target.ConnectionString))
            {
                await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
                using (var tx = conn.BeginTransaction())
                {
                    foreach (var row in rows)
                    {
                        using (var cmd = new SqlCommand(sql, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@CreatedUtc", row.CreatedUtc);
                            cmd.Parameters.AddWithValue("@EventCode", Cut(row.EventCode, 40) ?? string.Empty);
                            cmd.Parameters.AddWithValue("@Action", Cut(row.Action, 500) ?? string.Empty);
                            cmd.Parameters.AddWithValue("@Success", row.Success);
                            cmd.Parameters.AddWithValue("@CompanyId", (object)row.CompanyId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@UserId", (object)row.UserId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@McpSessionId", (object)Cut(row.McpSessionId, 100) ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@IpAddress", (object)Cut(row.IpAddress, 64) ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@CorrelationId", (object)Cut(row.CorrelationId, 100) ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@AppSource", (object)Cut(row.AppSource, 100) ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@HttpMethod", (object)Cut(row.HttpMethod, 10) ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ResourcePath", (object)Cut(row.ResourcePath, 1000) ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@HttpStatus", (object)row.HttpStatus ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ErrorMessage", (object)Cut(row.ErrorMessage, 2000) ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@AdditionalContext", (object)row.AdditionalContext ?? DBNull.Value);
                            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                        }
                    }
                    tx.Commit();
                }
            }
        }

        private static string Cut(string value, int max) =>
            value == null ? null : (value.Length <= max ? value : value.Substring(0, max));
    }
}
