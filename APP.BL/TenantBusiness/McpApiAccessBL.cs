using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace App.BL.TenantBusiness
{
    public sealed record McpApiKey(string AppSource, string OperationId);

    // One catalogued API and the security groups (AppSecurityGroup.GroupID) allowed to call it through MCP.
    public sealed class McpExposedApiDto
    {
        public int ExposedApiId { get; set; }
        public string AppSource { get; set; }
        public string OperationId { get; set; }
        public string HttpMethod { get; set; }
        public string ApiPath { get; set; }
        public string Summary { get; set; }
        public bool IsEnabled { get; set; }
        public List<int> GroupIds { get; set; } = new List<int>();
    }

    // Which APIs an external MCP user may use: catalogued + enabled + the user is in a granted security group.
    // Tables: dbo.AppMcpExposedApi, dbo.AppMcpExposedApiGroup (V047__McpExposedApi.sql); membership comes from
    // dbo.AppSecurityGroupMember (V001). Everything runs in the registered identity's tenant DB.
    public static class McpApiAccessBL
    {
        // The set the user may call. Deny by default: no catalog row, disabled, or no shared group => not included.
        public static async Task<List<McpApiKey>> GetAllowedForUserAsync(int userId, CancellationToken ct)
        {
            var result = new List<McpApiKey>();
            var (conn, db) = await OpenAsync(ct).ConfigureAwait(false);
            using (conn)
            using (var cmd = new SqlCommand($@"
SELECT e.AppSource, e.OperationId
FROM {db}.dbo.AppMcpExposedApi e
WHERE e.IsEnabled = 1
  AND EXISTS (SELECT 1
              FROM {db}.dbo.AppMcpExposedApiGroup g
              JOIN {db}.dbo.AppSecurityGroupMember m ON m.GroupID = g.GroupID
              WHERE g.ExposedApiId = e.ExposedApiId AND m.UserID = @UserId)", conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                using (var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false))
                    while (await reader.ReadAsync(ct).ConfigureAwait(false))
                        result.Add(new McpApiKey(reader.GetString(0), reader.GetString(1)));
            }
            return result;
        }

        public static async Task<List<McpExposedApiDto>> GetExposedApisAsync(CancellationToken ct)
        {
            var byId = new Dictionary<int, McpExposedApiDto>();
            var (conn, db) = await OpenAsync(ct).ConfigureAwait(false);
            using (conn)
            {
                using (var cmd = new SqlCommand($@"
SELECT ExposedApiId, AppSource, OperationId, HttpMethod, ApiPath, Summary, IsEnabled
FROM {db}.dbo.AppMcpExposedApi ORDER BY AppSource, OperationId", conn))
                using (var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false))
                    while (await reader.ReadAsync(ct).ConfigureAwait(false))
                        byId[reader.GetInt32(0)] = new McpExposedApiDto
                        {
                            ExposedApiId = reader.GetInt32(0),
                            AppSource = reader.GetString(1),
                            OperationId = reader.GetString(2),
                            HttpMethod = reader.IsDBNull(3) ? null : reader.GetString(3),
                            ApiPath = reader.IsDBNull(4) ? null : reader.GetString(4),
                            Summary = reader.IsDBNull(5) ? null : reader.GetString(5),
                            IsEnabled = reader.GetBoolean(6)
                        };

                using (var cmd = new SqlCommand($"SELECT ExposedApiId, GroupID FROM {db}.dbo.AppMcpExposedApiGroup", conn))
                using (var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false))
                    while (await reader.ReadAsync(ct).ConfigureAwait(false))
                        if (byId.TryGetValue(reader.GetInt32(0), out var api))
                            api.GroupIds.Add(reader.GetInt32(1));
            }
            return byId.Values.ToList();
        }

        // Creates or updates the catalog row for (AppSource, OperationId) and replaces its granted groups.
        // Returns the ExposedApiId. Throws ArgumentException for an unknown group id (nothing is saved).
        public static async Task<int> SaveExposedApiAsync(McpExposedApiDto dto, int modifiedById, CancellationToken ct)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.AppSource) || string.IsNullOrWhiteSpace(dto.OperationId))
                throw new ArgumentException("AppSource and OperationId are required.");

            var (conn, db) = await OpenAsync(ct).ConfigureAwait(false);

            using (conn)
            using (var tx = conn.BeginTransaction())
            {
                int id;
                using (var cmd = new SqlCommand($@"
SELECT ExposedApiId FROM {db}.dbo.AppMcpExposedApi WHERE AppSource = @AppSource AND OperationId = @OperationId", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@AppSource", dto.AppSource);
                    cmd.Parameters.AddWithValue("@OperationId", dto.OperationId);
                    var existing = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                    id = existing == null ? 0 : (int)existing;
                }

                if (id == 0)
                {
                    using (var cmd = new SqlCommand($@"
INSERT INTO {db}.dbo.AppMcpExposedApi (AppSource, OperationId, HttpMethod, ApiPath, Summary, IsEnabled, ModifiedById)
OUTPUT INSERTED.ExposedApiId
VALUES (@AppSource, @OperationId, @HttpMethod, @ApiPath, @Summary, @IsEnabled, @ModifiedById)", conn, tx))
                    {
                        AddApiParameters(cmd, dto, modifiedById);
                        id = (int)await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                    }
                }
                else
                {
                    using (var cmd = new SqlCommand($@"
UPDATE {db}.dbo.AppMcpExposedApi
SET HttpMethod = @HttpMethod, ApiPath = @ApiPath, Summary = @Summary, IsEnabled = @IsEnabled,
    ModifiedUtc = SYSUTCDATETIME(), ModifiedById = @ModifiedById
WHERE ExposedApiId = @ExposedApiId", conn, tx))
                    {
                        AddApiParameters(cmd, dto, modifiedById);
                        cmd.Parameters.AddWithValue("@ExposedApiId", id);
                        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                    }
                }

                using (var cmd = new SqlCommand($"DELETE FROM {db}.dbo.AppMcpExposedApiGroup WHERE ExposedApiId = @ExposedApiId", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@ExposedApiId", id);
                    await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                foreach (var groupId in (dto.GroupIds ?? new List<int>()).Distinct())
                {
                    // Insert only when the group exists, so an unknown id is an error and not a silent skip.
                    using (var cmd = new SqlCommand($@"
INSERT INTO {db}.dbo.AppMcpExposedApiGroup (ExposedApiId, GroupID)
SELECT @ExposedApiId, GroupID FROM {db}.dbo.AppSecurityGroup WHERE GroupID = @GroupID", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@ExposedApiId", id);
                        cmd.Parameters.AddWithValue("@GroupID", groupId);
                        if (await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false) == 0)
                            throw new ArgumentException("Unknown security group " + groupId);
                    }
                }

                tx.Commit();
                return id;
            }
        }

        public static async Task DeleteExposedApiAsync(int exposedApiId, CancellationToken ct)
        {
            var (conn, db) = await OpenAsync(ct).ConfigureAwait(false);
            using (conn)
            using (var cmd = new SqlCommand($"DELETE FROM {db}.dbo.AppMcpExposedApi WHERE ExposedApiId = @ExposedApiId", conn))
            {
                cmd.Parameters.AddWithValue("@ExposedApiId", exposedApiId);
                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
        }

        private static void AddApiParameters(SqlCommand cmd, McpExposedApiDto dto, int modifiedById)
        {
            cmd.Parameters.AddWithValue("@AppSource", dto.AppSource);
            cmd.Parameters.AddWithValue("@OperationId", dto.OperationId);
            cmd.Parameters.AddWithValue("@HttpMethod", (object)Cut(dto.HttpMethod, 10) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ApiPath", (object)Cut(dto.ApiPath, 500) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Summary", (object)Cut(dto.Summary, 500) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsEnabled", dto.IsEnabled);
            cmd.Parameters.AddWithValue("@ModifiedById", modifiedById);
        }

        // Tenant connection from the registered identity; the database name is quoted into every statement.
        private static async Task<(SqlConnection Conn, string Db)> OpenAsync(CancellationToken ct)
        {
            var (connStr, dbName) = AppTenantAdapterBL.GetTenantConnectionInfo();
            string quotedDb = "[" + dbName.Replace("]", "]]") + "]";
            var conn = new SqlConnection(connStr);
            try
            {
                await conn.OpenAsync(ct).ConfigureAwait(false);
                return (conn, quotedDb);
            }
            catch (Exception ex)
            {
                conn.Dispose();
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(OpenAsync));
                throw;
            }
        }

        private static string Cut(string value, int max) =>
            value == null ? null : (value.Length <= max ? value : value.Substring(0, max));
    }
}

