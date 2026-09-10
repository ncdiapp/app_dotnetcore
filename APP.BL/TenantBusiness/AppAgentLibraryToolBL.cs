using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using App.BL;

namespace App.BL.TenantBusiness
{
    public sealed record AppAgentLibraryToolDto(
        int    Id,
        string LibraryKey,
        string ToolName,
        string Description,
        string ParameterSchemaJson,
        string ToolType,
        string ToolConfig,
        bool   IsActive,
        int    SortOrder);

    public static class AppAgentLibraryToolBL
    {
        // ─────────────────────────────────────────────────────────────────────
        // Read
        // ─────────────────────────────────────────────────────────────────────

        public static List<AppAgentLibraryToolDto> GetByLibraryKey(string libraryKey)
        {
            if (string.IsNullOrWhiteSpace(libraryKey)) return new List<AppAgentLibraryToolDto>();
            var fixture = GetFixture();
            if (fixture == null) return new List<AppAgentLibraryToolDto>();
            return GetByLibraryKey(libraryKey, fixture);
        }

        public static List<AppAgentLibraryToolDto> GetByLibraryKey(int dataSourceId, string libraryKey)
        {
            if (string.IsNullOrWhiteSpace(libraryKey)) return new List<AppAgentLibraryToolDto>();
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return new List<AppAgentLibraryToolDto>();
            return GetByLibraryKey(libraryKey, fixture);
        }

        private static List<AppAgentLibraryToolDto> GetByLibraryKey(string libraryKey, DatabaseSchemaMrg.DatabaseFixture fixture)
        {
            var dt = fixture.RetriveDataTable(
                @"SELECT LibraryToolId, LibraryKey, ToolName, ToolDescription, ParameterSchemaJson,
                         ToolType, ToolConfig, IsActive, SortOrder
                  FROM dbo.AppAgentLibraryTool
                  WHERE LibraryKey=@LibraryKey
                  ORDER BY SortOrder, LibraryToolId",
                new List<DbParameter> { P(fixture, "@LibraryKey", libraryKey.Trim()) });
            return MapAll(dt);
        }

        // Used by the runtime engine — returns all active tools from subscribed libraries
        public static List<AppAgentLibraryToolDto> GetForRuntime(string skillKey)
        {
            if (string.IsNullOrWhiteSpace(skillKey)) return new List<AppAgentLibraryToolDto>();
            var fixture = GetFixture();
            if (fixture == null) return new List<AppAgentLibraryToolDto>();
            return GetForRuntime(skillKey, fixture);
        }

        public static List<AppAgentLibraryToolDto> GetForRuntime(int dataSourceId, string skillKey)
        {
            if (string.IsNullOrWhiteSpace(skillKey)) return new List<AppAgentLibraryToolDto>();
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return new List<AppAgentLibraryToolDto>();
            return GetForRuntime(skillKey, fixture);
        }

        private static List<AppAgentLibraryToolDto> GetForRuntime(string skillKey, DatabaseSchemaMrg.DatabaseFixture fixture)
        {
            var dt = fixture.RetriveDataTable(@"
SELECT t.LibraryToolId, t.LibraryKey, t.ToolName, t.ToolDescription, t.ParameterSchemaJson,
       t.ToolType, t.ToolConfig, t.IsActive, t.SortOrder
FROM dbo.AppAgentLibraryTool t
INNER JOIN dbo.AppAgentLibrarySubscription s ON t.LibraryKey = s.LibraryKey
WHERE s.SkillKey=@SkillKey AND t.IsActive=1
ORDER BY t.SortOrder, t.LibraryToolId",
                new List<DbParameter> { P(fixture, "@SkillKey", skillKey.Trim()) });
            return MapAll(dt);
        }

        public static List<LibraryToolPreviewDto> GetLibraryToolPreview(int dataSourceId, string libraryKey)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return new List<LibraryToolPreviewDto>();
            var dt = fixture.RetriveDataTable(
                "SELECT ToolName, ToolDescription FROM dbo.AppAgentLibraryTool WHERE LibraryKey=@LibraryKey AND IsActive=1 ORDER BY SortOrder, LibraryToolId",
                new List<DbParameter> { P(fixture, "@LibraryKey", libraryKey) });
            var result = new List<LibraryToolPreviewDto>();
            if (dt == null) return result;
            foreach (DataRow row in dt.Rows)
                result.Add(new LibraryToolPreviewDto(
                    ToolName:        row["ToolName"]        as string ?? "",
                    ToolDescription: row["ToolDescription"] as string ?? ""));
            return result;
        }

        public static List<LibraryToolPreviewDto> GetAvailableBuiltInTools(int dataSourceId)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return new List<LibraryToolPreviewDto>();
            var dt = fixture.RetriveDataTable(
                "SELECT DISTINCT ToolName, ToolDescription, ToolConfig FROM dbo.AppAgentLibraryTool WHERE ToolType='BuiltIn' AND IsActive=1 ORDER BY ToolName",
                new List<DbParameter>());
            var result = new List<LibraryToolPreviewDto>();
            if (dt == null) return result;
            foreach (DataRow row in dt.Rows)
                result.Add(new LibraryToolPreviewDto(
                    ToolName:        row["ToolName"]        as string ?? "",
                    ToolDescription: row["ToolDescription"] as string ?? "",
                    ToolConfig:      row["ToolConfig"]      as string ?? ""));
            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Write
        // ─────────────────────────────────────────────────────────────────────

        public static int Upsert(AppAgentLibraryToolDto dto)
        {
            if (dto == null) return 0;
            var fixture = GetFixture();
            if (fixture == null) return 0;
            return UpsertCore(fixture, dto);
        }

        public static int Upsert(int dataSourceId, AppAgentLibraryToolDto dto)
        {
            if (dto == null) return 0;
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return 0;
            return UpsertCore(fixture, dto);
        }

        private static int UpsertCore(DatabaseSchemaMrg.DatabaseFixture fixture, AppAgentLibraryToolDto dto)
        {
            if (dto.Id > 0)
            {
                fixture.ExecuteNonQueryResult(
                    @"UPDATE dbo.AppAgentLibraryTool SET
                        LibraryKey=@LibraryKey, ToolName=@ToolName, ToolDescription=@Description,
                        ParameterSchemaJson=@ParameterSchemaJson, ToolType=@ToolType,
                        ToolConfig=@ToolConfig, IsActive=@IsActive, SortOrder=@SortOrder
                      WHERE LibraryToolId=@Id",
                    UpsertParams(fixture, dto));
                return dto.Id;
            }
            var dt = fixture.RetriveDataTable(
                @"INSERT INTO dbo.AppAgentLibraryTool
                    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
                  OUTPUT INSERTED.LibraryToolId
                  VALUES (@LibraryKey, @ToolName, @Description, @ParameterSchemaJson, @ToolType, @ToolConfig, @IsActive, @SortOrder)",
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
                "DELETE FROM dbo.AppAgentLibraryTool WHERE LibraryToolId=@Id",
                new List<DbParameter> { P(fixture, "@Id", id) });
            return true;
        }

        public static bool Delete(int dataSourceId, int id)
        {
            if (id <= 0) return false;
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return false;
            fixture.ExecuteNonQueryResult(
                "DELETE FROM dbo.AppAgentLibraryTool WHERE LibraryToolId=@Id",
                new List<DbParameter> { P(fixture, "@Id", id) });
            return true;
        }

        // Called by AppAgentToolLibraryBL.DeleteLibrary which already holds the fixture
        internal static void DeleteByLibraryKey(DatabaseSchemaMrg.DatabaseFixture fixture, string libraryKey)
        {
            fixture.ExecuteNonQueryResult(
                "DELETE FROM dbo.AppAgentLibraryTool WHERE LibraryKey=@LibraryKey",
                new List<DbParameter> { P(fixture, "@LibraryKey", libraryKey) });
        }

        // ─────────────────────────────────────────────────────────────────────
        // Mapping
        // ─────────────────────────────────────────────────────────────────────

        private static List<AppAgentLibraryToolDto> MapAll(DataTable dt)
        {
            var result = new List<AppAgentLibraryToolDto>();
            if (dt == null) return result;
            foreach (DataRow row in dt.Rows)
                result.Add(Map(row));
            return result;
        }

        private static AppAgentLibraryToolDto Map(DataRow row) =>
            new AppAgentLibraryToolDto(
                Id:                  Convert.ToInt32(row["LibraryToolId"]),
                LibraryKey:          row["LibraryKey"]          as string ?? "",
                ToolName:            row["ToolName"]            as string ?? "",
                Description:         row["ToolDescription"]     as string ?? "",
                ParameterSchemaJson: row["ParameterSchemaJson"] as string ?? "",
                ToolType:            row["ToolType"]            as string ?? "BuiltIn",
                ToolConfig:          row["ToolConfig"]          as string ?? "",
                IsActive:            row["IsActive"]            != DBNull.Value && Convert.ToBoolean(row["IsActive"]),
                SortOrder:           row["SortOrder"]           != DBNull.Value ? Convert.ToInt32(row["SortOrder"]) : 0);

        private static List<DbParameter> UpsertParams(DatabaseSchemaMrg.DatabaseFixture f, AppAgentLibraryToolDto d) =>
            new List<DbParameter>
            {
                P(f, "@Id",                  d.Id > 0 ? (object)d.Id : DBNull.Value),
                P(f, "@LibraryKey",          d.LibraryKey),
                P(f, "@ToolName",            d.ToolName),
                P(f, "@Description",         (object)d.Description         ?? DBNull.Value),
                P(f, "@ParameterSchemaJson", (object)d.ParameterSchemaJson ?? DBNull.Value),
                P(f, "@ToolType",            d.ToolType ?? "BuiltIn"),
                P(f, "@ToolConfig",          (object)d.ToolConfig          ?? DBNull.Value),
                P(f, "@IsActive",            d.IsActive),
                P(f, "@SortOrder",           d.SortOrder)
            };

        // ─────────────────────────────────────────────────────────────────────
        // Infrastructure
        // ─────────────────────────────────────────────────────────────────────

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
