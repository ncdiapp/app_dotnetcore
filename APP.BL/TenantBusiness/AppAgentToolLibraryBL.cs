using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using App.BL;

namespace App.BL.TenantBusiness
{
    public sealed record AppAgentToolDomainDto(
        string DomainKey,
        string DomainName,
        string Description,
        int    SortOrder,
        bool   IsActive);

    public sealed record AppAgentToolLibraryDto(
        string LibraryKey,
        string DomainKey,
        string LibraryName,
        string Description,
        string ToolCategory,
        bool   IsActive,
        int    ToolCount);

    public sealed record AppAgentLibrarySubscriptionDto(
        string SkillKey,
        string LibraryKey);

    public sealed record LibraryToolPreviewDto(
        string ToolName,
        string ToolDescription,
        string ToolConfig = "");

    public static class AppAgentToolLibraryBL
    {
        // ─────────────────────────────────────────────────────────────────────
        // Domains
        // ─────────────────────────────────────────────────────────────────────

        public static List<AppAgentToolDomainDto> GetAllDomains()
        {
            var fixture = GetFixture();
            if (fixture == null) return new List<AppAgentToolDomainDto>();
            return GetAllDomains(fixture);
        }

        public static List<AppAgentToolDomainDto> GetAllDomains(int dataSourceId)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return new List<AppAgentToolDomainDto>();
            return GetAllDomains(fixture);
        }

        private static List<AppAgentToolDomainDto> GetAllDomains(DatabaseSchemaMrg.DatabaseFixture fixture)
        {
            var dt = fixture.RetriveDataTable(
                "SELECT DomainKey, DomainName, Description, SortOrder, IsActive FROM dbo.AppAgentToolDomain ORDER BY SortOrder, DomainName",
                new List<DbParameter>());
            return MapDomains(dt);
        }

        public static bool UpsertDomain(int dataSourceId, AppAgentToolDomainDto dto)
        {
            if (dto == null) return false;
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return false;

            fixture.ExecuteNonQueryResult(@"
IF EXISTS (SELECT 1 FROM dbo.AppAgentToolDomain WHERE DomainKey=@DomainKey)
    UPDATE dbo.AppAgentToolDomain SET
        DomainName=@DomainName, Description=@Description, SortOrder=@SortOrder, IsActive=@IsActive
    WHERE DomainKey=@DomainKey
ELSE
    INSERT INTO dbo.AppAgentToolDomain (DomainKey, DomainName, Description, SortOrder, IsActive)
    VALUES (@DomainKey, @DomainName, @Description, @SortOrder, @IsActive)",
                new List<DbParameter>
                {
                    P(fixture, "@DomainKey",   dto.DomainKey),
                    P(fixture, "@DomainName",  dto.DomainName),
                    P(fixture, "@Description", (object)dto.Description ?? DBNull.Value),
                    P(fixture, "@SortOrder",   dto.SortOrder),
                    P(fixture, "@IsActive",    dto.IsActive)
                });
            return true;
        }

        public static bool DeleteDomain(int dataSourceId, string domainKey)
        {
            if (string.IsNullOrWhiteSpace(domainKey)) return false;
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return false;
            fixture.ExecuteNonQueryResult(
                "DELETE FROM dbo.AppAgentToolDomain WHERE DomainKey=@DomainKey",
                new List<DbParameter> { P(fixture, "@DomainKey", domainKey) });
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Libraries
        // ─────────────────────────────────────────────────────────────────────

        public static List<AppAgentToolLibraryDto> GetAllLibraries()
        {
            var fixture = GetFixture();
            if (fixture == null) return new List<AppAgentToolLibraryDto>();
            return GetAllLibraries(fixture);
        }

        public static List<AppAgentToolLibraryDto> GetAllLibraries(int dataSourceId)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return new List<AppAgentToolLibraryDto>();
            return GetAllLibraries(fixture);
        }

        private static List<AppAgentToolLibraryDto> GetAllLibraries(DatabaseSchemaMrg.DatabaseFixture fixture)
        {
            var dt = fixture.RetriveDataTable(@"
SELECT l.LibraryKey, l.DomainKey, l.LibraryName, l.Description, l.ToolCategory, l.IsActive,
       (SELECT COUNT(*) FROM dbo.AppAgentToolRegister t WHERE t.SkillKey=l.LibraryKey AND t.IsActive=1) AS ToolCount
FROM dbo.AppAgentToolLibrary l
ORDER BY l.DomainKey, l.LibraryName",
                new List<DbParameter>());
            return MapLibraries(dt);
        }

        public static List<AppAgentToolLibraryDto> GetLibrariesByDomain(int dataSourceId, string domainKey)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return new List<AppAgentToolLibraryDto>();

            var dt = fixture.RetriveDataTable(@"
SELECT l.LibraryKey, l.DomainKey, l.LibraryName, l.Description, l.ToolCategory, l.IsActive,
       (SELECT COUNT(*) FROM dbo.AppAgentToolRegister t WHERE t.SkillKey=l.LibraryKey AND t.IsActive=1) AS ToolCount
FROM dbo.AppAgentToolLibrary l
WHERE l.DomainKey=@DomainKey
ORDER BY l.LibraryName",
                new List<DbParameter> { P(fixture, "@DomainKey", domainKey) });
            return MapLibraries(dt);
        }

        public static List<AppAgentToolLibraryDto> SearchLibraries(int dataSourceId, string query)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return new List<AppAgentToolLibraryDto>();

            var dt = fixture.RetriveDataTable(@"
SELECT l.LibraryKey, l.DomainKey, l.LibraryName, l.Description, l.ToolCategory, l.IsActive,
       (SELECT COUNT(*) FROM dbo.AppAgentToolRegister t WHERE t.SkillKey=l.LibraryKey AND t.IsActive=1) AS ToolCount
FROM dbo.AppAgentToolLibrary l
WHERE l.IsActive=1
  AND (@query IS NULL OR @query=''
       OR l.LibraryName  LIKE '%'+@query+'%'
       OR l.Description  LIKE '%'+@query+'%'
       OR l.DomainKey    LIKE '%'+@query+'%')
ORDER BY l.DomainKey, l.LibraryName",
                new List<DbParameter> { P(fixture, "@query", (object)query ?? DBNull.Value) });
            return MapLibraries(dt);
        }

        public static List<LibraryToolPreviewDto> GetLibraryToolPreview(int dataSourceId, string libraryKey)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return new List<LibraryToolPreviewDto>();

            // Returns tool name + description only — ToolConfig is intentionally excluded
            var dt = fixture.RetriveDataTable(
                "SELECT ToolName, ToolDescription FROM dbo.AppAgentToolRegister WHERE SkillKey=@LibraryKey AND IsActive=1 ORDER BY ToolRegisterId",
                new List<DbParameter> { P(fixture, "@LibraryKey", libraryKey) });

            var result = new List<LibraryToolPreviewDto>();
            if (dt == null) return result;
            foreach (DataRow row in dt.Rows)
                result.Add(new LibraryToolPreviewDto(
                    ToolName:        row["ToolName"] as string ?? "",
                    ToolDescription: row["ToolDescription"] as string ?? ""));
            return result;
        }

        public static bool UpsertLibrary(int dataSourceId, AppAgentToolLibraryDto dto)
        {
            if (dto == null) return false;
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return false;

            fixture.ExecuteNonQueryResult(@"
IF EXISTS (SELECT 1 FROM dbo.AppAgentToolLibrary WHERE LibraryKey=@LibraryKey)
    UPDATE dbo.AppAgentToolLibrary SET
        DomainKey=@DomainKey, LibraryName=@LibraryName, Description=@Description,
        ToolCategory=@ToolCategory, IsActive=@IsActive
    WHERE LibraryKey=@LibraryKey
ELSE
    INSERT INTO dbo.AppAgentToolLibrary (LibraryKey, DomainKey, LibraryName, Description, ToolCategory, IsActive)
    VALUES (@LibraryKey, @DomainKey, @LibraryName, @Description, @ToolCategory, @IsActive)",
                new List<DbParameter>
                {
                    P(fixture, "@LibraryKey",   dto.LibraryKey),
                    P(fixture, "@DomainKey",    dto.DomainKey),
                    P(fixture, "@LibraryName",  dto.LibraryName),
                    P(fixture, "@Description",  (object)dto.Description  ?? DBNull.Value),
                    P(fixture, "@ToolCategory", (object)dto.ToolCategory ?? DBNull.Value),
                    P(fixture, "@IsActive",     dto.IsActive)
                });
            return true;
        }

        public static bool DeleteLibrary(int dataSourceId, string libraryKey)
        {
            if (string.IsNullOrWhiteSpace(libraryKey)) return false;
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return false;

            // Order matters: remove tool rows, then MCP rows, then the library row.
            // Subscription rows are removed automatically by ON DELETE CASCADE.
            fixture.ExecuteNonQueryResult(
                "DELETE FROM dbo.AppAgentMcpServer    WHERE SkillKey=@LibraryKey",
                new List<DbParameter> { P(fixture, "@LibraryKey", libraryKey) });
            fixture.ExecuteNonQueryResult(
                "DELETE FROM dbo.AppAgentToolRegister WHERE SkillKey=@LibraryKey",
                new List<DbParameter> { P(fixture, "@LibraryKey", libraryKey) });
            fixture.ExecuteNonQueryResult(
                "DELETE FROM dbo.AppAgentToolLibrary  WHERE LibraryKey=@LibraryKey",
                new List<DbParameter> { P(fixture, "@LibraryKey", libraryKey) });
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Subscriptions
        // ─────────────────────────────────────────────────────────────────────

        public static List<AppAgentLibrarySubscriptionDto> GetSubscriptions(int dataSourceId, string skillKey)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return new List<AppAgentLibrarySubscriptionDto>();

            var dt = fixture.RetriveDataTable(
                "SELECT SkillKey, LibraryKey FROM dbo.AppAgentLibrarySubscription WHERE SkillKey=@SkillKey",
                new List<DbParameter> { P(fixture, "@SkillKey", skillKey) });

            var result = new List<AppAgentLibrarySubscriptionDto>();
            if (dt == null) return result;
            foreach (DataRow row in dt.Rows)
                result.Add(new AppAgentLibrarySubscriptionDto(
                    SkillKey:   row["SkillKey"]   as string ?? "",
                    LibraryKey: row["LibraryKey"] as string ?? ""));
            return result;
        }

        public static bool SetSubscriptions(int dataSourceId, string skillKey, IEnumerable<string> libraryKeys)
        {
            if (string.IsNullOrWhiteSpace(skillKey)) return false;
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return false;

            // Delete existing, then insert new (simple and safe for small sets)
            fixture.ExecuteNonQueryResult(
                "DELETE FROM dbo.AppAgentLibrarySubscription WHERE SkillKey=@SkillKey",
                new List<DbParameter> { P(fixture, "@SkillKey", skillKey) });

            foreach (var key in libraryKeys ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                fixture.ExecuteNonQueryResult(
                    "INSERT INTO dbo.AppAgentLibrarySubscription (SkillKey, LibraryKey) VALUES (@SkillKey, @LibraryKey)",
                    new List<DbParameter>
                    {
                        P(fixture, "@SkillKey",   skillKey),
                        P(fixture, "@LibraryKey", key)
                    });
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // BuiltIn tool browser (for tool registration UX)
        // ─────────────────────────────────────────────────────────────────────

        public static List<LibraryToolPreviewDto> GetAvailableBuiltInTools(int dataSourceId)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            if (fixture == null) return new List<LibraryToolPreviewDto>();

            var dt = fixture.RetriveDataTable(
                "SELECT DISTINCT ToolName, ToolDescription, ToolConfig FROM dbo.AppAgentToolRegister WHERE ToolType='BuiltIn' AND IsActive=1 ORDER BY ToolName",
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
        // Mapping helpers
        // ─────────────────────────────────────────────────────────────────────

        private static List<AppAgentToolDomainDto> MapDomains(DataTable dt)
        {
            var result = new List<AppAgentToolDomainDto>();
            if (dt == null) return result;
            foreach (DataRow row in dt.Rows)
                result.Add(new AppAgentToolDomainDto(
                    DomainKey:   row["DomainKey"]   as string ?? "",
                    DomainName:  row["DomainName"]  as string ?? "",
                    Description: row["Description"] as string ?? "",
                    SortOrder:   row["SortOrder"]   != DBNull.Value ? Convert.ToInt32(row["SortOrder"]) : 0,
                    IsActive:    row["IsActive"]    != DBNull.Value && Convert.ToBoolean(row["IsActive"])));
            return result;
        }

        private static List<AppAgentToolLibraryDto> MapLibraries(DataTable dt)
        {
            var result = new List<AppAgentToolLibraryDto>();
            if (dt == null) return result;
            foreach (DataRow row in dt.Rows)
                result.Add(new AppAgentToolLibraryDto(
                    LibraryKey:   row["LibraryKey"]   as string ?? "",
                    DomainKey:    row["DomainKey"]    as string ?? "",
                    LibraryName:  row["LibraryName"]  as string ?? "",
                    Description:  row["Description"]  as string ?? "",
                    ToolCategory: row["ToolCategory"] as string ?? "",
                    IsActive:     row["IsActive"]     != DBNull.Value && Convert.ToBoolean(row["IsActive"]),
                    ToolCount:    row["ToolCount"]     != DBNull.Value ? Convert.ToInt32(row["ToolCount"]) : 0));
            return result;
        }

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
