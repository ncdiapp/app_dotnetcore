using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;

namespace App.BL
{
    public static class AppOpenApiDocumentBL
    {
        private const string CreateDocumentSql = @"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = N'dbo' AND TABLE_NAME = N'AppOpenApiDocument')
BEGIN
    CREATE TABLE dbo.AppOpenApiDocument (
        Id               INT IDENTITY(1,1) NOT NULL,
        Name             NVARCHAR(200)  NOT NULL,
        Code             NVARCHAR(100)  NOT NULL,
        Version          NVARCHAR(40)   NULL,
        Description      NVARCHAR(1000) NULL,
        Status           NVARCHAR(20)   NOT NULL CONSTRAINT DF_AppOpenApiDocument_Status DEFAULT (N'Draft'),
        OpenApiJson      NVARCHAR(MAX)  NULL,
        ApiCount         INT            NOT NULL CONSTRAINT DF_AppOpenApiDocument_ApiCount DEFAULT (0),
        LastGenerated    DATETIME2      NULL,
        AppCreatedByID   INT            NULL,
        AppCreatedDate   DATETIME2      NOT NULL CONSTRAINT DF_AppOpenApiDocument_Created DEFAULT (SYSUTCDATETIME()),
        AppModifiedByID  INT            NULL,
        AppModifiedDate  DATETIME2      NOT NULL CONSTRAINT DF_AppOpenApiDocument_Modified DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AppOpenApiDocument PRIMARY KEY (Id),
        CONSTRAINT UQ_AppOpenApiDocument_Code UNIQUE (Code)
    );
END";

        private const string CreateMemberSql = @"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = N'dbo' AND TABLE_NAME = N'AppOpenApiDocumentMember')
BEGIN
    CREATE TABLE dbo.AppOpenApiDocumentMember (
        Id          INT IDENTITY(1,1) NOT NULL,
        DocumentId  INT           NOT NULL,
        ActionCode  NVARCHAR(200) NOT NULL,
        CONSTRAINT PK_AppOpenApiDocumentMember PRIMARY KEY (Id),
        CONSTRAINT UQ_AppOpenApiDocumentMember_DocCode UNIQUE (DocumentId, ActionCode)
    );
END";

        public class DocumentDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Code { get; set; }
            public string Version { get; set; }
            public string Description { get; set; }
            public string Status { get; set; }
            public int ApiCount { get; set; }
            public DateTime? LastGenerated { get; set; }
            public DateTime? AppCreatedDate { get; set; }
            public DateTime? AppModifiedDate { get; set; }
            public int? AppCreatedByID { get; set; }
            public int? AppModifiedByID { get; set; }
            public string OpenApiJson { get; set; }
            public List<MemberDto> Members { get; set; } = new List<MemberDto>();
        }

        public class MemberDto
        {
            public string ActionCode { get; set; }
            public string HttpMethod { get; set; }
            public string Source { get; set; }
            public string Description { get; set; }
        }

        public class SaveRequest
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Code { get; set; }
            public string Version { get; set; }
            public string Description { get; set; }
            public List<string> ActionCodes { get; set; }
        }

        public class SelectableApiDto
        {
            public int Id { get; set; }
            public string ActionCode { get; set; }
            public string Description { get; set; }
            public string ApiType { get; set; }
            public string HttpMethod { get; set; }
            public int? DataSourceId { get; set; }
            public string DataSourceName { get; set; }
            public int? TranscationId { get; set; }
            public string DataModelName { get; set; }
            public string Application { get; set; }
            public string ProviderName { get; set; }
            public string ProviderKind { get; set; }
        }

        public class RegenerateResult
        {
            public DocumentDto Document { get; set; }
            public List<AppOpenApiDocumentGenerator.Skip> Skipped { get; set; }
        }

        public static List<DocumentDto> List()
        {
            EnsureTables();
            var rows = new List<DocumentDto>();
            using (var conn = OpenTenant())
            using (var cmd = new SqlCommand(@"
SELECT Id, Name, Code, Version, Description, Status, ApiCount, LastGenerated,
       AppCreatedDate, AppModifiedDate, AppCreatedByID, AppModifiedByID
FROM dbo.AppOpenApiDocument
ORDER BY Name", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    rows.Add(ReadDocument(reader, includeJson: false));
            }
            return rows;
        }

        public static DocumentDto Get(int id)
        {
            EnsureTables();
            DocumentDto doc;
            using (var conn = OpenTenant())
                doc = ReadOne(conn, id, includeJson: true);
            if (doc == null)
                return null;
            doc.Members = LoadMembers(id);
            return doc;
        }

        public static DocumentDto Save(SaveRequest request)
        {
            if (request == null)
                throw new InvalidOperationException("Nothing to save.");
            var name = (request.Name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Name is required.");

            EnsureTables();
            var codes = (request.ActionCodes ?? new List<string>())
                .Select(c => (c ?? "").Trim())
                .Where(c => c.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var userId = AppSecurityUserBL.CurrentUserId;
            using (var conn = OpenTenant())
            {
                if (request.Id <= 0)
                {
                    var code = NormalizeCode(request.Code);
                    if (CodeExists(conn, code, 0))
                        throw new InvalidOperationException("Code is already used.");
                    var version = string.IsNullOrWhiteSpace(request.Version) ? "1.0.0" : request.Version.Trim();
                    using (var cmd = new SqlCommand(@"
INSERT INTO dbo.AppOpenApiDocument
    (Name, Code, Version, Description, Status, ApiCount, AppCreatedByID, AppModifiedByID)
VALUES (@Name, @Code, @Version, @Description, N'Draft', 0, @UserId, @UserId);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Code", code);
                        cmd.Parameters.AddWithValue("@Version", version);
                        cmd.Parameters.AddWithValue("@Description", (object)request.Description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        request.Id = (int)cmd.ExecuteScalar();
                    }
                }
                else
                {
                    using (var cmd = new SqlCommand(@"
UPDATE dbo.AppOpenApiDocument
SET Name = @Name,
    Version = @Version,
    Description = @Description,
    AppModifiedByID = @UserId,
    AppModifiedDate = SYSUTCDATETIME()
WHERE Id = @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Version", (object)(request.Version ?? "").Trim() ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Description", (object)request.Description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@Id", request.Id);
                        if (cmd.ExecuteNonQuery() == 0)
                            throw new InvalidOperationException("Document was not found.");
                    }
                }

                ReplaceMembers(conn, request.Id, codes);
            }

            return Get(request.Id);
        }

        public static void Delete(int id)
        {
            EnsureTables();
            using (var conn = OpenTenant())
            {
                using (var members = new SqlCommand("DELETE FROM dbo.AppOpenApiDocumentMember WHERE DocumentId = @Id", conn))
                {
                    members.Parameters.AddWithValue("@Id", id);
                    members.ExecuteNonQuery();
                }
                using (var doc = new SqlCommand("DELETE FROM dbo.AppOpenApiDocument WHERE Id = @Id", conn))
                {
                    doc.Parameters.AddWithValue("@Id", id);
                    doc.ExecuteNonQuery();
                }
            }
        }

        public static DocumentDto SetPublished(int id, bool published)
        {
            EnsureTables();
            using (var conn = OpenTenant())
            {
                string json = null;
                using (var read = new SqlCommand("SELECT OpenApiJson FROM dbo.AppOpenApiDocument WHERE Id = @Id", conn))
                {
                    read.Parameters.AddWithValue("@Id", id);
                    json = read.ExecuteScalar() as string;
                }
                if (published && string.IsNullOrWhiteSpace(json))
                    throw new InvalidOperationException("Regenerate the document before publishing.");

                using (var cmd = new SqlCommand(@"
UPDATE dbo.AppOpenApiDocument
SET Status = @Status, AppModifiedByID = @UserId, AppModifiedDate = SYSUTCDATETIME()
WHERE Id = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Status", published ? "Published" : "Draft");
                    cmd.Parameters.AddWithValue("@UserId", AppSecurityUserBL.CurrentUserId);
                    cmd.Parameters.AddWithValue("@Id", id);
                    if (cmd.ExecuteNonQuery() == 0)
                        throw new InvalidOperationException("Document was not found.");
                }
            }
            return Get(id);
        }

        public static RegenerateResult Regenerate(int id)
        {
            var doc = Get(id);
            if (doc == null)
                throw new InvalidOperationException("Document was not found.");

            var version = IncrementVersion(doc.Version);
            var apis = new Dictionary<string, AppIntergrationSettingParameterExDto>(StringComparer.OrdinalIgnoreCase);
            foreach (var api in LoadOperations())
            {
                if (!string.IsNullOrWhiteSpace(api.ActionCode) && !apis.ContainsKey(api.ActionCode))
                    apis[api.ActionCode] = api;
            }
            var members = new List<AppIntergrationSettingParameterExDto>();
            var missing = new List<AppOpenApiDocumentGenerator.Skip>();
            foreach (var member in doc.Members)
            {
                AppIntergrationSettingParameterExDto api;
                if (!apis.TryGetValue(member.ActionCode, out api))
                {
                    missing.Add(new AppOpenApiDocumentGenerator.Skip
                    {
                        ActionCode = member.ActionCode,
                        Reason = "No API with this ActionCode."
                    });
                    continue;
                }
                members.Add(api);
            }

            var generated = AppOpenApiDocumentGenerator.Generate(doc.Name, version, doc.Description, members);
            generated.Skipped.InsertRange(0, missing);

            using (var conn = OpenTenant())
            using (var cmd = new SqlCommand(@"
UPDATE dbo.AppOpenApiDocument
SET OpenApiJson = @Json,
    ApiCount = @ApiCount,
    Version = @Version,
    LastGenerated = SYSUTCDATETIME(),
    AppModifiedByID = @UserId,
    AppModifiedDate = SYSUTCDATETIME()
WHERE Id = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Json", generated.Json);
                cmd.Parameters.AddWithValue("@ApiCount", generated.ApiCount);
                cmd.Parameters.AddWithValue("@Version", version);
                cmd.Parameters.AddWithValue("@UserId", AppSecurityUserBL.CurrentUserId);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }

            return new RegenerateResult
            {
                Document = Get(id),
                Skipped = generated.Skipped
            };
        }

        public static string ReadForDownload(int id, string requestRoot)
        {
            var doc = Get(id);
            if (doc == null || string.IsNullOrWhiteSpace(doc.OpenApiJson))
                return null;
            return AppOpenApiDocumentGenerator.ApplyServerUrl(doc.OpenApiJson, requestRoot);
        }

        public static string ReadPublished(string code, string requestRoot)
        {
            if (!IsCodeShape(code))
                return null;

            foreach (var connStr in CompanyMasterConnectionStrings())
            {
                try
                {
                    using (var conn = new SqlConnection(connStr))
                    {
                        conn.Open();
                        if (!TableExists(conn, "AppOpenApiDocument"))
                            continue;
                        using (var cmd = new SqlCommand(@"
SELECT OpenApiJson FROM dbo.AppOpenApiDocument
WHERE Code = @Code AND Status = N'Published'", conn))
                        {
                            cmd.Parameters.AddWithValue("@Code", code);
                            var json = cmd.ExecuteScalar() as string;
                            if (!string.IsNullOrWhiteSpace(json))
                                return AppOpenApiDocumentGenerator.ApplyServerUrl(json, requestRoot);
                        }
                    }
                }
                catch
                {
                    // One tenant database being unavailable does not hide a document in another.
                }
            }
            return null;
        }

        public static List<SelectableApiDto> ListSelectableApis()
        {
            var operations = LoadOperations();
            var dataSources = SafeDataSources();
            var transactions = SafeTransactions();
            var searches = SafeSearches();
            var apps = SafeApplications();

            var rows = new List<SelectableApiDto>();
            foreach (var dto in operations)
            {
                if (AppOpenApiDocumentGenerator.IsExcelImport(dto))
                    continue;
                if (string.IsNullOrWhiteSpace(dto.ActionCode))
                    continue;

                var providerId = dto.IntergrationSettingId ?? 0;
                var isApp = providerId == AppIntergrationSettingBL.AppBuiltInProviderId;
                int? dataSourceId = dto.DataSourceId;
                int? applicationId = dto.SaasApplicationId;
                string dataModelName = null;

                if (isApp && dto.TranscationId.HasValue && transactions.ContainsKey(dto.TranscationId.Value))
                {
                    var transaction = transactions[dto.TranscationId.Value];
                    dataModelName = transaction.TransactionName;
                    dataSourceId = transaction.DataSourceFrom ?? dataSourceId;
                    applicationId = transaction.SaasApplicationId ?? applicationId;
                }
                else if (isApp && dto.TranscationFieId.HasValue && searches.ContainsKey(dto.TranscationFieId.Value))
                {
                    var search = searches[dto.TranscationFieId.Value];
                    dataSourceId = search.DataSourceFrom ?? dataSourceId;
                    applicationId = search.SaasApplicationId ?? applicationId;
                }

                string application = "Other API";
                if (applicationId.HasValue && apps.ContainsKey(applicationId.Value))
                    application = apps[applicationId.Value];
                else if (dataSourceId.HasValue && dataSources.ContainsKey(dataSourceId.Value))
                    application = "Other API on " + dataSources[dataSourceId.Value];

                rows.Add(new SelectableApiDto
                {
                    Id = dto.Id == null ? 0 : Convert.ToInt32(dto.Id),
                    ActionCode = dto.ActionCode,
                    Description = dto.ActionDescription,
                    ApiType = AppOpenApiDocumentGenerator.Classify(dto),
                    HttpMethod = string.IsNullOrWhiteSpace(dto.HttpMethd) ? "Get" : dto.HttpMethd,
                    DataSourceId = dataSourceId,
                    DataSourceName = dataSourceId.HasValue && dataSources.ContainsKey(dataSourceId.Value) ? dataSources[dataSourceId.Value] : "",
                    TranscationId = dto.TranscationId,
                    DataModelName = dataModelName ?? "",
                    Application = isApp ? application : "",
                    ProviderName = string.IsNullOrWhiteSpace(dto.ProviderName) ? (isApp ? AppIntergrationSettingBL.AppBuiltInProviderName : "") : dto.ProviderName,
                    ProviderKind = isApp ? "app" : "thirdParty"
                });
            }

            return rows.OrderBy(r => r.ActionCode, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static List<AppIntergrationSettingParameterExDto> LoadOperations()
        {
            var result = new List<AppIntergrationSettingParameterExDto>();
            var providerNames = new Dictionary<int, string>();
            using (var adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                var settings = new EntityCollection<AppIntergrationSettingEntity>();
                adapter.FetchEntityCollection(settings, null);
                foreach (var setting in settings)
                {
                    var id = Convert.ToInt32(setting.IntergrationSettingId);
                    providerNames[id] = setting.Name;
                }

                var list = new EntityCollection<AppIntergrationSettingParameterEntity>();
                adapter.FetchEntityCollection(list, null);
                foreach (var entity in list)
                {
                    if (!string.IsNullOrWhiteSpace(entity.MappingInternalCode)
                        && !entity.MappingInternalCode.Equals(EmAppIntergrationSettingParameterUsageType.ApiOperation.ToString(), StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (string.IsNullOrWhiteSpace(entity.ActionCode))
                        continue;

                    var dto = AppIntergrationSettingParameterConverter.ConvertEntityToExDto(entity);
                    var providerId = dto.IntergrationSettingId ?? 0;
                    string providerName;
                    if (!providerNames.TryGetValue(providerId, out providerName) || string.IsNullOrWhiteSpace(providerName))
                        providerName = providerId == AppIntergrationSettingBL.AppBuiltInProviderId
                            ? AppIntergrationSettingBL.AppBuiltInProviderName
                            : "Provider " + providerId;
                    dto.ProviderName = providerName;
                    result.Add(dto);
                }
            }
            return result;
        }

        private static Dictionary<int, string> SafeDataSources()
        {
            var map = new Dictionary<int, string>();
            try
            {
                foreach (var source in AppDataSourceRegisterBL.RetrieveAllAppDataSourceRegisterExDto())
                {
                    if (source?.Id == null) continue;
                    map[Convert.ToInt32(source.Id)] = source.DataSourceName ?? "";
                }
            }
            catch { }
            return map;
        }

        private static Dictionary<int, AppTransactionDto> SafeTransactions()
        {
            var map = new Dictionary<int, AppTransactionDto>();
            try
            {
                foreach (var row in AppTransactionBL.RetrieveAllAppTransactionDto(null))
                {
                    if (row?.Id == null) continue;
                    map[Convert.ToInt32(row.Id)] = row;
                }
            }
            catch { }
            return map;
        }

        private static Dictionary<int, AppSearchDto> SafeSearches()
        {
            var map = new Dictionary<int, AppSearchDto>();
            try
            {
                foreach (var row in AppSearchConfigBL.RetrieveAllAppSearchDto())
                {
                    if (row?.Id == null) continue;
                    map[Convert.ToInt32(row.Id)] = row;
                }
            }
            catch { }
            return map;
        }

        private static Dictionary<int, string> SafeApplications()
        {
            var map = new Dictionary<int, string>();
            try
            {
                foreach (var row in AppSaasUserApplicationPackageBL.GetSaasApplicationList())
                {
                    if (row?.Id == null) continue;
                    map[Convert.ToInt32(row.Id)] = row.Name;
                }
            }
            catch { }
            return map;
        }

        private static List<MemberDto> LoadMembers(int documentId)
        {
            var codes = new List<string>();
            using (var conn = OpenTenant())
            using (var cmd = new SqlCommand("SELECT ActionCode FROM dbo.AppOpenApiDocumentMember WHERE DocumentId = @Id ORDER BY ActionCode", conn))
            {
                cmd.Parameters.AddWithValue("@Id", documentId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        codes.Add(reader.GetString(0));
                }
            }

            var apis = new Dictionary<string, AppIntergrationSettingParameterExDto>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var api in LoadOperations())
                {
                    if (!string.IsNullOrWhiteSpace(api.ActionCode) && !apis.ContainsKey(api.ActionCode))
                        apis[api.ActionCode] = api;
                }
            }
            catch { }

            return codes.Select(code =>
            {
                AppIntergrationSettingParameterExDto api;
                apis.TryGetValue(code, out api);
                var isApp = api != null && (api.IntergrationSettingId ?? 0) == AppIntergrationSettingBL.AppBuiltInProviderId;
                return new MemberDto
                {
                    ActionCode = code,
                    HttpMethod = api == null || string.IsNullOrWhiteSpace(api.HttpMethd) ? "" : api.HttpMethd,
                    Source = api == null ? "" : (isApp ? AppOpenApiDocumentGenerator.Classify(api) : api.ProviderName),
                    Description = api?.ActionDescription ?? ""
                };
            }).ToList();
        }

        private static void ReplaceMembers(SqlConnection conn, int documentId, List<string> codes)
        {
            using (var delete = new SqlCommand("DELETE FROM dbo.AppOpenApiDocumentMember WHERE DocumentId = @Id", conn))
            {
                delete.Parameters.AddWithValue("@Id", documentId);
                delete.ExecuteNonQuery();
            }
            foreach (var code in codes)
            {
                using (var insert = new SqlCommand("INSERT INTO dbo.AppOpenApiDocumentMember (DocumentId, ActionCode) VALUES (@Id, @Code)", conn))
                {
                    insert.Parameters.AddWithValue("@Id", documentId);
                    insert.Parameters.AddWithValue("@Code", code);
                    insert.ExecuteNonQuery();
                }
            }
        }

        private static DocumentDto ReadOne(SqlConnection conn, int id, bool includeJson)
        {
            var sql = includeJson
                ? @"SELECT Id, Name, Code, Version, Description, Status, ApiCount, LastGenerated,
                           AppCreatedDate, AppModifiedDate, AppCreatedByID, AppModifiedByID, OpenApiJson
                    FROM dbo.AppOpenApiDocument WHERE Id = @Id"
                : @"SELECT Id, Name, Code, Version, Description, Status, ApiCount, LastGenerated,
                           AppCreatedDate, AppModifiedDate, AppCreatedByID, AppModifiedByID
                    FROM dbo.AppOpenApiDocument WHERE Id = @Id";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;
                    return ReadDocument(reader, includeJson);
                }
            }
        }

        private static DocumentDto ReadDocument(SqlDataReader reader, bool includeJson)
        {
            var doc = new DocumentDto
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Code = reader.GetString(2),
                Version = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Description = reader.IsDBNull(4) ? "" : reader.GetString(4),
                Status = reader.GetString(5),
                ApiCount = reader.GetInt32(6),
                LastGenerated = reader.IsDBNull(7) ? (DateTime?)null : reader.GetDateTime(7),
                AppCreatedDate = reader.IsDBNull(8) ? (DateTime?)null : reader.GetDateTime(8),
                AppModifiedDate = reader.IsDBNull(9) ? (DateTime?)null : reader.GetDateTime(9),
                AppCreatedByID = reader.IsDBNull(10) ? (int?)null : reader.GetInt32(10),
                AppModifiedByID = reader.IsDBNull(11) ? (int?)null : reader.GetInt32(11)
            };
            if (includeJson)
                doc.OpenApiJson = reader.IsDBNull(12) ? "" : reader.GetString(12);
            return doc;
        }

        private static bool CodeExists(SqlConnection conn, string code, int exceptId)
        {
            using (var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.AppOpenApiDocument WHERE Code = @Code AND Id <> @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Code", code);
                cmd.Parameters.AddWithValue("@Id", exceptId);
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        private static string NormalizeCode(string code)
        {
            var value = (code ?? "").Trim().ToLowerInvariant();
            if (!IsCodeShape(value))
                throw new InvalidOperationException("Code may contain only lowercase letters, digits, and hyphens.");
            return value;
        }

        private static bool IsCodeShape(string code)
        {
            return !string.IsNullOrWhiteSpace(code) && Regex.IsMatch(code, "^[a-z0-9]+(?:-[a-z0-9]+)*$");
        }

        public static string IncrementVersion(string version)
        {
            var value = (version ?? "").Trim();
            if (string.IsNullOrWhiteSpace(value))
                value = "1.0.0";
            var parts = value.Split('.');
            int last;
            if (parts.Length == 0 || !int.TryParse(parts[parts.Length - 1], out last))
                return "1.0.1";
            parts[parts.Length - 1] = (last + 1).ToString();
            return string.Join(".", parts);
        }

        private static void EnsureTables()
        {
            using (var conn = OpenTenant())
            {
                using (var cmd = new SqlCommand(CreateDocumentSql, conn))
                    cmd.ExecuteNonQuery();
                using (var cmd = new SqlCommand(CreateMemberSql, conn))
                    cmd.ExecuteNonQuery();
            }
        }

        private static SqlConnection OpenTenant()
        {
            var info = AppTenantAdapterBL.GetTenantConnectionInfo();
            var conn = new SqlConnection(info.connStr);
            conn.Open();
            return conn;
        }

        private static bool TableExists(SqlConnection conn, string tableName)
        {
            using (var cmd = new SqlCommand("SELECT OBJECT_ID(@Name, 'U')", conn))
            {
                cmd.Parameters.AddWithValue("@Name", "dbo." + tableName);
                var id = cmd.ExecuteScalar();
                return id != null && id != DBNull.Value;
            }
        }

        private static IEnumerable<string> CompanyMasterConnectionStrings()
        {
            foreach (var tenant in AppDataSourceRegisterBL.RetrieveAllAppDataSourceRegisterEntity())
            {
                if (tenant.IsCompanyMasterDb != true || string.IsNullOrEmpty(tenant.ConnectionString))
                    continue;
                string plain;
                try
                {
                    plain = AppConnectionStringEncryptionBL.Decrypt(tenant.ConnectionString);
                }
                catch
                {
                    continue;
                }
                if (!string.IsNullOrWhiteSpace(plain))
                    yield return plain;
            }
        }
    }
}
