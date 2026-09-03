using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using App.BL;

namespace App.BL.TenantBusiness
{
    public sealed record AppAgentToolRegisterDto(
        int    Id,
        string SkillKey,
        string ToolName,
        string Description,
        string ParameterSchemaJson,
        string ToolType,
        string ToolConfig,
        bool   IsActive);

    public static class AppAgentToolRegisterBL
    {
        public static List<AppAgentToolRegisterDto> GetBySkillKey(string skillKey)
        {
            if (string.IsNullOrWhiteSpace(skillKey)) return new List<AppAgentToolRegisterDto>();
            var fixture = GetFixture();
            if (fixture == null) return new List<AppAgentToolRegisterDto>();
            return GetBySkillKey(skillKey, fixture);
        }

        public static List<AppAgentToolRegisterDto> GetBySkillKey(string skillKey, int dataSourceId)
        {
            if (string.IsNullOrWhiteSpace(skillKey)) return new List<AppAgentToolRegisterDto>();
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return new List<AppAgentToolRegisterDto>();
            return GetBySkillKey(skillKey, fixture);
        }

        private static List<AppAgentToolRegisterDto> GetBySkillKey(string skillKey, DatabaseSchemaMrg.DatabaseFixture fixture)
        {
            var dt = fixture.RetriveDataTable(
                @"SELECT ToolRegisterId, SkillKey, ToolName, ToolDescription, ParameterSchemaJson,
                         ToolType, ToolConfig, IsActive
                  FROM dbo.AppAgentToolRegister
                  WHERE SkillKey=@SkillKey AND IsActive=1
                  ORDER BY ToolRegisterId",
                new List<DbParameter> { P(fixture, "@SkillKey", skillKey.Trim()) });

            return MapAll(dt);
        }

        // Controller-facing aliases (accept explicit dsId, bypass ServerContext)
        public static List<AppAgentToolRegisterDto> GetToolsBySkillKey(int dataSourceId, string skillKey)
            => GetBySkillKey(skillKey, dataSourceId);

        public static bool UpsertTool(int dataSourceId, AppAgentToolRegisterDto dto)
        {
            if (dto == null) return false;
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return false;
            if (dto.Id > 0)
            {
                fixture.ExecuteNonQueryResult(
                    @"UPDATE dbo.AppAgentToolRegister SET
                        SkillKey=@SkillKey, ToolName=@ToolName, ToolDescription=@Description,
                        ParameterSchemaJson=@ParameterSchemaJson, ToolType=@ToolType,
                        ToolConfig=@ToolConfig, IsActive=@IsActive
                      WHERE ToolRegisterId=@Id",
                    UpsertParams(fixture, dto));
            }
            else
            {
                fixture.ExecuteNonQueryResult(
                    @"INSERT INTO dbo.AppAgentToolRegister
                        (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
                      VALUES (@SkillKey, @ToolName, @Description, @ParameterSchemaJson, @ToolType, @ToolConfig, @IsActive)",
                    UpsertParams(fixture, dto));
            }
            return true;
        }

        public static bool DeleteTool(int dataSourceId, int id)
        {
            if (id <= 0) return false;
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return false;
            fixture.ExecuteNonQueryResult(
                "DELETE FROM dbo.AppAgentToolRegister WHERE ToolRegisterId=@Id",
                new List<DbParameter> { P(fixture, "@Id", id) });
            return true;
        }

        public static int Upsert(AppAgentToolRegisterDto dto)
        {
            if (dto == null) return 0;
            var fixture = GetFixture();
            if (fixture == null) return 0;

            if (dto.Id > 0)
            {
                fixture.ExecuteNonQueryResult(
                    @"UPDATE dbo.AppAgentToolRegister SET
                        SkillKey=@SkillKey, ToolName=@ToolName, ToolDescription=@Description,
                        ParameterSchemaJson=@ParameterSchemaJson, ToolType=@ToolType,
                        ToolConfig=@ToolConfig, IsActive=@IsActive
                      WHERE ToolRegisterId=@Id",
                    UpsertParams(fixture, dto));
                return dto.Id;
            }

            var dt = fixture.RetriveDataTable(
                @"INSERT INTO dbo.AppAgentToolRegister
                    (SkillKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive)
                  OUTPUT INSERTED.ToolRegisterId
                  VALUES (@SkillKey, @ToolName, @Description, @ParameterSchemaJson, @ToolType, @ToolConfig, @IsActive)",
                UpsertParams(fixture, dto));

            if (dt != null && dt.Rows.Count > 0)
                return Convert.ToInt32(dt.Rows[0][0]);
            return 0;
        }

        public static bool Delete(int id)
        {
            if (id <= 0) return false;
            var fixture = GetFixture();
            if (fixture == null) return false;

            fixture.ExecuteNonQueryResult(
                "DELETE FROM dbo.AppAgentToolRegister WHERE ToolRegisterId=@Id",
                new List<DbParameter> { P(fixture, "@Id", id) });
            return true;
        }

        public static bool SetActive(int id, bool active)
        {
            if (id <= 0) return false;
            var fixture = GetFixture();
            if (fixture == null) return false;

            fixture.ExecuteNonQueryResult(
                "UPDATE dbo.AppAgentToolRegister SET IsActive=@IsActive WHERE ToolRegisterId=@Id",
                new List<DbParameter>
                {
                    P(fixture, "@IsActive", active),
                    P(fixture, "@Id",       id)
                });
            return true;
        }

        private static List<AppAgentToolRegisterDto> MapAll(DataTable dt)
        {
            var result = new List<AppAgentToolRegisterDto>();
            if (dt == null) return result;
            foreach (DataRow row in dt.Rows)
                result.Add(Map(row));
            return result;
        }

        private static AppAgentToolRegisterDto Map(DataRow row)
        {
            return new AppAgentToolRegisterDto(
                Id:                  Convert.ToInt32(row["ToolRegisterId"]),
                SkillKey:            row["SkillKey"] as string ?? "",
                ToolName:            row["ToolName"] as string ?? "",
                Description:         row["ToolDescription"] as string ?? "",
                ParameterSchemaJson: row["ParameterSchemaJson"] as string ?? "",
                ToolType:            row["ToolType"] as string ?? "BuiltIn",
                ToolConfig:          row["ToolConfig"] as string ?? "",
                IsActive:            row["IsActive"] != DBNull.Value && Convert.ToBoolean(row["IsActive"]));
        }

        private static List<DbParameter> UpsertParams(DatabaseSchemaMrg.DatabaseFixture f, AppAgentToolRegisterDto d)
        {
            return new List<DbParameter>
            {
                P(f, "@Id",                  d.Id > 0 ? (object)d.Id : DBNull.Value),
                P(f, "@SkillKey",            d.SkillKey),
                P(f, "@ToolName",            d.ToolName),
                P(f, "@Description",         d.Description),
                P(f, "@ParameterSchemaJson", d.ParameterSchemaJson),
                P(f, "@ToolType",            d.ToolType ?? "BuiltIn"),
                P(f, "@ToolConfig",          d.ToolConfig),
                P(f, "@IsActive",            d.IsActive)
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
