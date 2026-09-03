using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using App.BL;
using APP.Components.EntityDto;
using DatabaseSchemaMrg;

namespace App.BL.AIAgent.GenericAgent
{
    public static class AppAgentMcpServerBL
    {
        public static List<AppAgentMcpServerDto> GetAllMcpServers(int dataSourceId)
        {
            var list = new List<AppAgentMcpServerDto>();
            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                if (fixture == null) return list;
                var dt = fixture.RetriveDataTable(
                    "SELECT * FROM dbo.AppAgentMcpServer ORDER BY McpServerKey",
                    new List<DbParameter>());
                if (dt == null) return list;
                foreach (DataRow row in dt.Rows)
                    list.Add(MapRow(row));
            }
            catch { }
            return list;
        }

        public static bool UpsertMcpServer(int dataSourceId, AppAgentMcpServerDto dto)
        {
            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                if (fixture == null) return false;
                const string sql = @"
IF EXISTS (SELECT 1 FROM dbo.AppAgentMcpServer WHERE McpServerKey = @McpServerKey)
    UPDATE dbo.AppAgentMcpServer SET
        ServerUrl = @ServerUrl, Transport = @Transport,
        AuthType = @AuthType, AuthValue = @AuthValue, IsActive = @IsActive
    WHERE McpServerKey = @McpServerKey
ELSE
    INSERT INTO dbo.AppAgentMcpServer (McpServerKey, ServerUrl, Transport, AuthType, AuthValue, IsActive)
    VALUES (@McpServerKey, @ServerUrl, @Transport, @AuthType, @AuthValue, @IsActive)";
                fixture.ExecuteNonQueryResult(sql, BuildParams(fixture, dto));
                return true;
            }
            catch { return false; }
        }

        public static bool DeleteMcpServer(int dataSourceId, string mcpServerKey)
        {
            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                if (fixture == null) return false;
                var p = fixture.CreateParameter("@McpServerKey"); p.Value = mcpServerKey;
                fixture.ExecuteNonQueryResult(
                    "DELETE FROM dbo.AppAgentMcpServer WHERE McpServerKey = @McpServerKey",
                    new List<DbParameter> { p });
                return true;
            }
            catch { return false; }
        }

        private static AppAgentMcpServerDto MapRow(DataRow row) => new AppAgentMcpServerDto
        {
            McpServerKey = row["McpServerKey"] as string ?? "",
            ServerUrl    = row["ServerUrl"] as string ?? "",
            Transport    = row["Transport"] as string ?? "streamable-http",
            AuthType     = row["AuthType"] as string ?? "none",
            AuthValue    = row["AuthValue"] as string ?? "",
            IsActive     = row["IsActive"] is bool ia ? ia : (row["IsActive"] is int iai && iai == 1),
        };

        private static List<DbParameter> BuildParams(DatabaseFixture fixture, AppAgentMcpServerDto dto)
        {
            var p = new List<DbParameter>();
            void Add(string n, object v) { var x = fixture.CreateParameter(n); x.Value = v ?? DBNull.Value; p.Add(x); }
            Add("@McpServerKey", dto.McpServerKey);
            Add("@ServerUrl",    dto.ServerUrl);
            Add("@Transport",    string.IsNullOrEmpty(dto.Transport) ? "streamable-http" : dto.Transport);
            Add("@AuthType",     string.IsNullOrEmpty(dto.AuthType) ? "none" : dto.AuthType);
            Add("@AuthValue",    string.IsNullOrEmpty(dto.AuthValue) ? null : dto.AuthValue);
            Add("@IsActive",     dto.IsActive);
            return p;
        }
    }
}
