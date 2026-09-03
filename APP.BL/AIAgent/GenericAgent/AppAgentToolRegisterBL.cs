using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using App.BL;
using APP.Components.EntityDto;
using DatabaseSchemaMrg;

namespace App.BL.AIAgent.GenericAgent
{
    public static class AppAgentToolRegisterBL
    {
        public static List<AppAgentToolRegisterDto> GetToolsBySkillKey(int dataSourceId, string skillKey)
        {
            var list = new List<AppAgentToolRegisterDto>();
            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                if (fixture == null) return list;
                var p = fixture.CreateParameter("@SkillKey"); p.Value = skillKey ?? "";
                var dt = fixture.RetriveDataTable(
                    "SELECT * FROM dbo.AppAgentToolRegister WHERE SkillKey = @SkillKey ORDER BY SortOrder, Id",
                    new List<DbParameter> { p });
                if (dt == null) return list;
                foreach (DataRow row in dt.Rows)
                    list.Add(MapRow(row));
            }
            catch { }
            return list;
        }

        public static bool UpsertTool(int dataSourceId, AppAgentToolRegisterDto dto)
        {
            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                if (fixture == null) return false;
                if (dto.Id > 0)
                {
                    const string upd = @"
UPDATE dbo.AppAgentToolRegister SET
    SkillKey = @SkillKey, ToolName = @ToolName, Description = @Description,
    ToolType = @ToolType, ToolConfig = @ToolConfig, IsActive = @IsActive, SortOrder = @SortOrder
WHERE Id = @Id";
                    fixture.ExecuteNonQueryResult(upd, BuildParams(fixture, dto, includeId: true));
                }
                else
                {
                    const string ins = @"
INSERT INTO dbo.AppAgentToolRegister (SkillKey, ToolName, Description, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (@SkillKey, @ToolName, @Description, @ToolType, @ToolConfig, @IsActive, @SortOrder)";
                    fixture.ExecuteNonQueryResult(ins, BuildParams(fixture, dto, includeId: false));
                }
                return true;
            }
            catch { return false; }
        }

        public static bool DeleteTool(int dataSourceId, int id)
        {
            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
                if (fixture == null) return false;
                var p = fixture.CreateParameter("@Id"); p.Value = id;
                fixture.ExecuteNonQueryResult(
                    "DELETE FROM dbo.AppAgentToolRegister WHERE Id = @Id",
                    new List<DbParameter> { p });
                return true;
            }
            catch { return false; }
        }

        private static AppAgentToolRegisterDto MapRow(DataRow row) => new AppAgentToolRegisterDto
        {
            Id          = row["Id"] is int id ? id : Convert.ToInt32(row["Id"]),
            SkillKey    = row["SkillKey"] as string ?? "",
            ToolName    = row["ToolName"] as string ?? "",
            Description = row["Description"] as string ?? "",
            ToolType    = row["ToolType"] as string ?? "BuiltIn",
            ToolConfig  = row["ToolConfig"] as string ?? "{}",
            IsActive    = row["IsActive"] is bool ia ? ia : (row["IsActive"] is int iai && iai == 1),
            SortOrder   = row["SortOrder"] is int so ? so : 0,
        };

        private static List<DbParameter> BuildParams(DatabaseFixture fixture, AppAgentToolRegisterDto dto, bool includeId)
        {
            var p = new List<DbParameter>();
            void Add(string n, object v) { var x = fixture.CreateParameter(n); x.Value = v ?? DBNull.Value; p.Add(x); }
            if (includeId) Add("@Id", dto.Id);
            Add("@SkillKey",    dto.SkillKey);
            Add("@ToolName",    dto.ToolName);
            Add("@Description", string.IsNullOrEmpty(dto.Description) ? null : dto.Description);
            Add("@ToolType",    string.IsNullOrEmpty(dto.ToolType) ? "BuiltIn" : dto.ToolType);
            Add("@ToolConfig",  string.IsNullOrEmpty(dto.ToolConfig) ? "{}" : dto.ToolConfig);
            Add("@IsActive",    dto.IsActive);
            Add("@SortOrder",   dto.SortOrder);
            return p;
        }
    }
}
