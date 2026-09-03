using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using App.BL;

namespace App.BL.TenantBusiness
{
    public sealed record AppAgentMcpServerDto(
        int    McpServerId,
        string SkillKey,
        string ServerName,
        string ServerType,
        string ServerUrl,
        string Command,
        bool   IsActive);

    public static class AppAgentMcpServerBL
    {
        public static List<AppAgentMcpServerDto> GetAll()
        {
            var fixture = GetFixture();
            if (fixture == null) return new List<AppAgentMcpServerDto>();

            var dt = fixture.RetriveDataTable(
                "SELECT McpServerId,SkillKey,ServerName,ServerType,ServerUrl,Command,IsActive FROM dbo.AppAgentMcpServer ORDER BY McpServerId",
                new List<DbParameter>());
            return MapAll(dt);
        }

        public static List<AppAgentMcpServerDto> GetBySkillKey(string skillKey)
        {
            if (string.IsNullOrWhiteSpace(skillKey)) return new List<AppAgentMcpServerDto>();
            var fixture = GetFixture();
            if (fixture == null) return new List<AppAgentMcpServerDto>();
            return GetBySkillKey(skillKey, fixture);
        }

        public static List<AppAgentMcpServerDto> GetBySkillKey(string skillKey, int dataSourceId)
        {
            if (string.IsNullOrWhiteSpace(skillKey)) return new List<AppAgentMcpServerDto>();
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return new List<AppAgentMcpServerDto>();
            return GetBySkillKey(skillKey, fixture);
        }

        private static List<AppAgentMcpServerDto> GetBySkillKey(string skillKey, DatabaseSchemaMrg.DatabaseFixture fixture)
        {
            var dt = fixture.RetriveDataTable(
                "SELECT McpServerId,SkillKey,ServerName,ServerType,ServerUrl,Command,IsActive FROM dbo.AppAgentMcpServer WHERE SkillKey=@SkillKey AND IsActive=1 ORDER BY McpServerId",
                new List<DbParameter> { P(fixture, "@SkillKey", skillKey.Trim()) });
            return MapAll(dt);
        }

        public static AppAgentMcpServerDto GetById(int mcpServerId)
        {
            var fixture = GetFixture();
            if (fixture == null) return null;

            var dt = fixture.RetriveDataTable(
                "SELECT McpServerId,SkillKey,ServerName,ServerType,ServerUrl,Command,IsActive FROM dbo.AppAgentMcpServer WHERE McpServerId=@McpServerId",
                new List<DbParameter> { P(fixture, "@McpServerId", mcpServerId) });

            if (dt == null || dt.Rows.Count == 0) return null;
            return Map(dt.Rows[0]);
        }

        public static int Upsert(AppAgentMcpServerDto dto)
        {
            if (dto == null) return 0;
            var fixture = GetFixture();
            if (fixture == null) return 0;

            if (dto.McpServerId > 0)
            {
                fixture.ExecuteNonQueryResult(
                    @"UPDATE dbo.AppAgentMcpServer SET
                        SkillKey=@SkillKey, ServerName=@ServerName, ServerType=@ServerType,
                        ServerUrl=@ServerUrl, Command=@Command, IsActive=@IsActive
                      WHERE McpServerId=@McpServerId",
                    UpsertParams(fixture, dto));
                return dto.McpServerId;
            }

            var dt = fixture.RetriveDataTable(
                @"INSERT INTO dbo.AppAgentMcpServer (SkillKey,ServerName,ServerType,ServerUrl,Command,IsActive)
                  OUTPUT INSERTED.McpServerId
                  VALUES (@SkillKey,@ServerName,@ServerType,@ServerUrl,@Command,@IsActive)",
                UpsertParams(fixture, dto));

            if (dt != null && dt.Rows.Count > 0)
                return Convert.ToInt32(dt.Rows[0][0]);
            return 0;
        }

        public static bool Delete(int mcpServerId)
        {
            if (mcpServerId <= 0) return false;
            var fixture = GetFixture();
            if (fixture == null) return false;

            fixture.ExecuteNonQueryResult(
                "DELETE FROM dbo.AppAgentMcpServer WHERE McpServerId=@McpServerId",
                new List<DbParameter> { P(fixture, "@McpServerId", mcpServerId) });
            return true;
        }

        private static List<AppAgentMcpServerDto> MapAll(DataTable dt)
        {
            var result = new List<AppAgentMcpServerDto>();
            if (dt == null) return result;
            foreach (DataRow row in dt.Rows)
                result.Add(Map(row));
            return result;
        }

        private static AppAgentMcpServerDto Map(DataRow row)
        {
            return new AppAgentMcpServerDto(
                McpServerId: Convert.ToInt32(row["McpServerId"]),
                SkillKey:    row["SkillKey"] as string ?? "",
                ServerName:  row["ServerName"] as string ?? "",
                ServerType:  row["ServerType"] as string ?? "streamable-http",
                ServerUrl:   row["ServerUrl"] as string ?? "",
                Command:     row["Command"] as string ?? "",
                IsActive:    row["IsActive"] != DBNull.Value && Convert.ToBoolean(row["IsActive"]));
        }

        private static List<DbParameter> UpsertParams(DatabaseSchemaMrg.DatabaseFixture f, AppAgentMcpServerDto d)
        {
            return new List<DbParameter>
            {
                P(f, "@McpServerId", d.McpServerId > 0 ? (object)d.McpServerId : DBNull.Value),
                P(f, "@SkillKey",    d.SkillKey),
                P(f, "@ServerName",  d.ServerName),
                P(f, "@ServerType",  d.ServerType),
                P(f, "@ServerUrl",   d.ServerUrl),
                P(f, "@Command",     d.Command),
                P(f, "@IsActive",    d.IsActive)
            };
        }

        private static DatabaseSchemaMrg.DatabaseFixture GetFixture()
        {
            var id = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            if (!id.HasValue) return null;
            return AppCacheManagerBL.GetOneDatabaseFixture(id.Value);
        }

        private static DbParameter P(DatabaseSchemaMrg.DatabaseFixture f, string name, object value)
        {
            var p = f.CreateParameter(name);
            p.Value = value ?? DBNull.Value;
            return p;
        }
    }
}
