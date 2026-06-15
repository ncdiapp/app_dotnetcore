using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using APP.BL.AppMgr;
using APP.Components.EntityDto;
using APP.Framework;
namespace App.BL.AppMgr.AiSkill
{
    public static class AppAISkillBL
    {
        #region Default DataSource
        /// <summary>
        ///  get defailt datasource register id for AI skill management.
        /// </summary>
        /// <returns></returns>
        public static int? GetDefaultDataSourceId()
        {
            return AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
        }

        #endregion

        #region Skill CRUD

        public static List<AppAISkillDto> GetAllSkills(int dataSourceRegisterId)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId);
            var sql = @"SELECT SkillId, Name, Description, IsActive, CreatedDate, UpdatedDate
                        FROM AppAISkill
                        ORDER BY Name";

            var dt = fixture.RetriveDataTable(sql, new List<DbParameter>());
            var list = new List<AppAISkillDto>();
            if (dt == null) return list;

            foreach (DataRow row in dt.Rows)
                list.Add(MapSkillRow(row, includeContent: false));

            return list;
        }

        public static AppAISkillDto GetSkillByName(int dataSourceRegisterId, string name)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId);

            var sql = @"SELECT SkillId, Name, Description, SkillContent, IsActive, CreatedDate, UpdatedDate
                        FROM AppAISkill WHERE Name = @Name AND IsActive = 1";

            var p = fixture.CreateParameter("@Name");
            p.Value = name;

            var dt = fixture.RetriveDataTable(sql, new List<DbParameter> { p });
            if (dt == null || dt.Rows.Count == 0) return null;

            var skill = MapSkillRow(dt.Rows[0], includeContent: true);
            skill.References = GetRefsBySkillId(dataSourceRegisterId, skill.SkillId);
            return skill;
        }

        public static AppAISkillDto GetSkillById(int dataSourceRegisterId, int skillId)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId);

            var sql = @"SELECT SkillId, Name, Description, SkillContent, IsActive, CreatedDate, UpdatedDate
                        FROM AppAISkill WHERE SkillId = @SkillId";

            var p = fixture.CreateParameter("@SkillId");
            p.Value = skillId;

            var dt = fixture.RetriveDataTable(sql, new List<DbParameter> { p });
            if (dt == null || dt.Rows.Count == 0) return null;

            var skill = MapSkillRow(dt.Rows[0], includeContent: true);
            skill.References = GetRefsBySkillId(dataSourceRegisterId, skillId);
            return skill;
        }

        public static int CreateSkill(int dataSourceRegisterId, AppAISkillDto dto)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId);

            var sql = @"INSERT INTO AppAISkill (Name, Description, SkillContent, IsActive, CreatedDate)
                        VALUES (@Name, @Description, @SkillContent, @IsActive, GETDATE());
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var pName = fixture.CreateParameter("@Name"); pName.Value = dto.Name ?? "";
            var pDesc = fixture.CreateParameter("@Description"); pDesc.Value = (object)dto.Description ?? DBNull.Value;
            var pContent = fixture.CreateParameter("@SkillContent"); pContent.Value = (object)dto.SkillContent ?? DBNull.Value;
            var pActive = fixture.CreateParameter("@IsActive"); pActive.Value = dto.IsActive;

            var dt = fixture.RetriveDataTable(sql, new List<DbParameter> { pName, pDesc, pContent, pActive });
            if (dt != null && dt.Rows.Count > 0)
                return Convert.ToInt32(dt.Rows[0][0]);

            return 0;
        }

        public static void UpdateSkill(int dataSourceRegisterId, AppAISkillDto dto)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId);

            var sql = @"UPDATE AppAISkill
                        SET Name = @Name,
                            Description = @Description,
                            SkillContent = @SkillContent,
                            IsActive = @IsActive,
                            UpdatedDate = GETDATE()
                        WHERE SkillId = @SkillId";

            var pId = fixture.CreateParameter("@SkillId"); pId.Value = dto.SkillId;
            var pName = fixture.CreateParameter("@Name"); pName.Value = dto.Name ?? "";
            var pDesc = fixture.CreateParameter("@Description"); pDesc.Value = (object)dto.Description ?? DBNull.Value;
            var pContent = fixture.CreateParameter("@SkillContent"); pContent.Value = (object)dto.SkillContent ?? DBNull.Value;
            var pActive = fixture.CreateParameter("@IsActive"); pActive.Value = dto.IsActive;

            fixture.ExecuteNonQueryResult(sql, new List<DbParameter> { pId, pName, pDesc, pContent, pActive });
        }

        public static void DeleteSkill(int dataSourceRegisterId, int skillId)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId);

            var pId = fixture.CreateParameter("@SkillId"); pId.Value = skillId;

            fixture.ExecuteNonQueryResult(
                "DELETE FROM AppAISkillRef WHERE SkillId = @SkillId",
                new List<DbParameter> { pId });

            var pId2 = fixture.CreateParameter("@SkillId"); pId2.Value = skillId;
            fixture.ExecuteNonQueryResult(
                "DELETE FROM AppAISkill WHERE SkillId = @SkillId",
                new List<DbParameter> { pId2 });
        }

        #endregion

        #region Reference CRUD

        public static List<AppAISkillRefDto> GetRefsBySkillId(int dataSourceRegisterId, int skillId)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId);

            var sql = @"SELECT RefId, SkillId, FileName, FileContent, SortOrder, CreatedDate
                        FROM AppAISkillRef
                        WHERE SkillId = @SkillId
                        ORDER BY SortOrder, RefId";

            var p = fixture.CreateParameter("@SkillId"); p.Value = skillId;
            var dt = fixture.RetriveDataTable(sql, new List<DbParameter> { p });

            var list = new List<AppAISkillRefDto>();
            if (dt == null) return list;

            foreach (DataRow row in dt.Rows)
                list.Add(MapRefRow(row));

            return list;
        }

        public static int CreateRef(int dataSourceRegisterId, AppAISkillRefDto dto)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId);

            var sql = @"INSERT INTO AppAISkillRef (SkillId, FileName, FileContent, SortOrder, CreatedDate)
                        VALUES (@SkillId, @FileName, @FileContent, @SortOrder, GETDATE());
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var pSkillId = fixture.CreateParameter("@SkillId"); pSkillId.Value = dto.SkillId;
            var pFileName = fixture.CreateParameter("@FileName"); pFileName.Value = dto.FileName ?? "";
            var pContent = fixture.CreateParameter("@FileContent"); pContent.Value = (object)dto.FileContent ?? DBNull.Value;
            var pSort = fixture.CreateParameter("@SortOrder"); pSort.Value = dto.SortOrder;

            var dt = fixture.RetriveDataTable(sql, new List<DbParameter> { pSkillId, pFileName, pContent, pSort });
            if (dt != null && dt.Rows.Count > 0)
                return Convert.ToInt32(dt.Rows[0][0]);

            return 0;
        }

        public static void UpdateRef(int dataSourceRegisterId, AppAISkillRefDto dto)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId);

            var sql = @"UPDATE AppAISkillRef
                        SET FileName = @FileName,
                            FileContent = @FileContent,
                            SortOrder = @SortOrder
                        WHERE RefId = @RefId";

            var pRefId = fixture.CreateParameter("@RefId"); pRefId.Value = dto.RefId;
            var pFileName = fixture.CreateParameter("@FileName"); pFileName.Value = dto.FileName ?? "";
            var pContent = fixture.CreateParameter("@FileContent"); pContent.Value = (object)dto.FileContent ?? DBNull.Value;
            var pSort = fixture.CreateParameter("@SortOrder"); pSort.Value = dto.SortOrder;

            fixture.ExecuteNonQueryResult(sql, new List<DbParameter> { pRefId, pFileName, pContent, pSort });
        }

        public static void DeleteRef(int dataSourceRegisterId, int refId)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId);

            var p = fixture.CreateParameter("@RefId"); p.Value = refId;
            fixture.ExecuteNonQueryResult(
                "DELETE FROM AppAISkillRef WHERE RefId = @RefId",
                new List<DbParameter> { p });
        }

        #endregion

        #region Agent Use

        /// <summary>
        /// Returns the fully composed skill prompt: SkillContent + all reference FileContents (ordered by SortOrder).
        /// Used by AI agents to load a skill's full system prompt.
        /// </summary>
        public static string GetComposedSkillPrompt(int dataSourceRegisterId, int skillId)
        {
            var skill = GetSkillById(dataSourceRegisterId, skillId);
            if (skill == null) return null;

            var sb = new StringBuilder();
            sb.Append(skill.SkillContent ?? "");

            foreach (var r in skill.References)
            {
                if (!string.IsNullOrWhiteSpace(r.FileContent))
                {
                    sb.AppendLine();
                    sb.AppendLine();
                    sb.AppendLine("---");
                    sb.AppendLine();
                    sb.Append(r.FileContent);
                }
            }

            return sb.ToString();
        }

        #endregion

        #region Private Helpers

        private static AppAISkillDto MapSkillRow(DataRow row, bool includeContent)
        {
            return new AppAISkillDto
            {
                SkillId = Convert.ToInt32(row["SkillId"]),
                Name = row["Name"]?.ToString(),
                Description = row["Description"] == DBNull.Value ? null : row["Description"].ToString(),
                SkillContent = includeContent && row.Table.Columns.Contains("SkillContent") && row["SkillContent"] != DBNull.Value
                    ? row["SkillContent"].ToString() : null,
                IsActive = row["IsActive"] != DBNull.Value && Convert.ToBoolean(row["IsActive"]),
                CreatedDate = row["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(row["CreatedDate"]) : DateTime.UtcNow,
                UpdatedDate = row["UpdatedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["UpdatedDate"]),
                References = new List<AppAISkillRefDto>()
            };
        }

        private static AppAISkillRefDto MapRefRow(DataRow row)
        {
            return new AppAISkillRefDto
            {
                RefId = Convert.ToInt32(row["RefId"]),
                SkillId = Convert.ToInt32(row["SkillId"]),
                FileName = row["FileName"]?.ToString(),
                FileContent = row["FileContent"] == DBNull.Value ? null : row["FileContent"].ToString(),
                SortOrder = Convert.ToInt32(row["SortOrder"]),
                CreatedDate = row["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(row["CreatedDate"]) : DateTime.UtcNow
            };
        }

        #endregion
    }
}
