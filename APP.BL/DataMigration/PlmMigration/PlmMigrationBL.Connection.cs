using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using APP.Framework.Communication;
using APP.Framework.Validation;
using DatabaseSchemaMrg;
using Newtonsoft.Json;

namespace APP.BL.DataMigration.PlmMigration
{
    public static partial class PlmMigrationBL
    {
        #region Schema DDL

        private const string EnsureIntegrationIdColumnsSql = @"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME='AppEntityInfo' AND COLUMN_NAME='IntegrationId')
    ALTER TABLE dbo.AppEntityInfo ADD IntegrationId INT NULL;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME='AppTransaction' AND COLUMN_NAME='IntegrationId')
    ALTER TABLE dbo.AppTransaction ADD IntegrationId NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME='AppTransactionUnit' AND COLUMN_NAME='IntegrationId')
    ALTER TABLE dbo.AppTransactionUnit ADD IntegrationId NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME='AppTransactionField' AND COLUMN_NAME='IntegrationId')
    ALTER TABLE dbo.AppTransactionField ADD IntegrationId NVARCHAR(100) NULL;";

        private const string EnsureSessionTableSql = @"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME='AppPlmImportSession')
BEGIN
    CREATE TABLE dbo.AppPlmImportSession (
        SessionId               INT IDENTITY(1,1) NOT NULL,
        SessionGuid             NVARCHAR(50)      NOT NULL,
        CompanyId               INT               NOT NULL,
        SaasApplicationId       INT               NULL,
        CreatedByUserId         INT               NULL,
        CreatedAt               DATETIME          NOT NULL,
        UpdatedAt               DATETIME          NOT NULL,
        SessionStatus           NVARCHAR(20)      NOT NULL,
        CurrentStepCode         NVARCHAR(50)      NULL,
        PlmConnectionEncrypted  NVARCHAR(MAX)     NULL,
        StepStateJson           NVARCHAR(MAX)     NULL,
        DataSourceDiscoveryJson NVARCHAR(MAX)     NULL,
        CONSTRAINT PK_AppPlmImportSession PRIMARY KEY (SessionId),
        CONSTRAINT UQ_AppPlmImportSession_Guid UNIQUE (SessionGuid)
    );
    CREATE INDEX IX_AppPlmImportSession_CompanyStatus ON dbo.AppPlmImportSession (CompanyId, SessionStatus);
END";

        private const string EnsureJobTableSql = @"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME='AppPlmImportJob')
BEGIN
    CREATE TABLE dbo.AppPlmImportJob (
        JobId             INT IDENTITY(1,1) NOT NULL,
        SessionId         INT               NOT NULL,
        JobType           NVARCHAR(50)      NOT NULL,
        Status            NVARCHAR(20)      NOT NULL,
        ProgressPercent   INT               NOT NULL CONSTRAINT DF_AppPlmImportJob_Progress DEFAULT(0),
        ProgressMessage   NVARCHAR(500)     NULL,
        ResultJson        NVARCHAR(MAX)     NULL,
        ErrorMessage      NVARCHAR(MAX)     NULL,
        CreatedAt         DATETIME          NOT NULL,
        UpdatedAt         DATETIME          NOT NULL,
        StartedAt         DATETIME          NULL,
        CompletedAt       DATETIME          NULL,
        CONSTRAINT PK_AppPlmImportJob PRIMARY KEY (JobId)
    );
    CREATE INDEX IX_AppPlmImportJob_SessionId ON dbo.AppPlmImportJob (SessionId);
END";

        private const string EnsureLogTableSql = @"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME='AppPlmImportLog')
BEGIN
    CREATE TABLE dbo.AppPlmImportLog (
        LogId               INT IDENTITY(1,1) NOT NULL,
        SessionId           INT               NOT NULL,
        JobId               INT               NULL,
        StepCode            NVARCHAR(50)      NULL,
        Action              NVARCHAR(100)     NULL,
        Status              NVARCHAR(20)      NULL,
        TargetKey           NVARCHAR(200)     NULL,
        PlmIntegrationKey   NVARCHAR(100)     NULL,
        RowsAffected        INT               NULL,
        DurationMs          INT               NULL,
        Message             NVARCHAR(MAX)     NULL,
        CreatedAt           DATETIME          NOT NULL,
        CONSTRAINT PK_AppPlmImportLog PRIMARY KEY (LogId)
    );
    CREATE INDEX IX_AppPlmImportLog_SessionId ON dbo.AppPlmImportLog (SessionId);
END";

        #endregion

        #region Auth & fixture

        public static void RequirePlmMigrationAdmin()
        {
            if (!AppSecurityUserBL.IsAdminUser())
                throw new UnauthorizedAccessException("PLM Data Import requires SaasCompanyAdmin or SysAdmin.");
        }

        private static int ResolveCompanyId(int? targetCompanyId)
        {
            var identity = ServerContext.Instance?.CurrnetClientIdentity;
            if (identity == null)
                throw new InvalidOperationException("No active user session.");

            bool isSysAdmin = identity.CurrentLoginUserType == (int)EmAppUserType.SysAdmin;
            if (isSysAdmin)
            {
                if (!targetCompanyId.HasValue || targetCompanyId.Value <= 0)
                    throw new ArgumentException("SysAdmin must specify target CompanyId.");
                return targetCompanyId.Value;
            }

            if (identity.CurrentWorkingCompanyId is int companyId && companyId > 0)
                return companyId;

            throw new InvalidOperationException("Current company is not set.");
        }

        private static int GetTenantDataSourceId()
        {
            var dataSourceId = ServerContext.Instance?.DataSourceId as int?;
            if (!dataSourceId.HasValue || dataSourceId.Value <= 0)
                throw new InvalidOperationException("Tenant data source is not available.");
            return dataSourceId.Value;
        }

        private static DatabaseFixture GetTenantFixture()
        {
            EnsurePlmImportSchema();
            return AppCacheManagerBL.GetOneDatabaseFixture(GetTenantDataSourceId());
        }

        public static void EnsurePlmImportSchema()
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(GetTenantDataSourceId());
            fixture.ExecuteNonQueryResult(EnsureIntegrationIdColumnsSql, new List<DbParameter>());
            fixture.ExecuteNonQueryResult(EnsureSessionTableSql, new List<DbParameter>());
            fixture.ExecuteNonQueryResult(EnsureJobTableSql, new List<DbParameter>());
            fixture.ExecuteNonQueryResult(EnsureLogTableSql, new List<DbParameter>());
        }

        #endregion

        #region Connection test

        public static OperationCallResult<PlmConnectionTestResultDto> TestPlmConnection(PlmConnectionTestRequestDto request)
        {
            var result = new OperationCallResult<PlmConnectionTestResultDto> { Object = new PlmConnectionTestResultDto() };
            try
            {
                RequirePlmMigrationAdmin();
                if (request != null)
                    ResolveCompanyId(request.TargetCompanyId);

                if (string.IsNullOrWhiteSpace(request?.ConnectionString))
                {
                    result.ValidationResult.Items.Add(new ValidationItem(
                        typeof(PlmConnectionTestRequestDto), "Plm_Connection_Empty", ValidationItemType.Error,
                        "PLM connection string is required."));
                    return result;
                }

                using (var conn = new SqlConnection(request.ConnectionString.Trim()))
                {
                    conn.Open();
                    result.Object.IsSuccess = true;
                    result.Object.ServerVersion = conn.ServerVersion;
                    result.Object.DatabaseName = conn.Database;
                }
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmConnectionTestRequestDto), "Plm_Connection_Test_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<PlmDiscoverDataSourcesResultDto> DiscoverPlmDataSources(PlmDiscoverDataSourcesRequestDto request)
        {
            var result = new OperationCallResult<PlmDiscoverDataSourcesResultDto>
            {
                Object = new PlmDiscoverDataSourcesResultDto()
            };
            try
            {
                RequirePlmMigrationAdmin();
                int companyId = ResolveCompanyId(request?.TargetCompanyId);
                EnsurePlmImportSchema();
                return DiscoverPlmDataSourcesCore(request, companyId);
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmDiscoverDataSourcesRequestDto), "Plm_Discover_Error", ValidationItemType.Error, ex.Message));
                return result;
            }
        }

        #endregion

        #region Session

        public static OperationCallResult<PlmImportSessionDto> GetActiveImportSession(int? targetCompanyId)
        {
            var result = new OperationCallResult<PlmImportSessionDto>();
            try
            {
                RequirePlmMigrationAdmin();
                int companyId = ResolveCompanyId(targetCompanyId);
                var fixture = GetTenantFixture();

                var pCompany = fixture.CreateParameter("@CompanyId");
                pCompany.Value = companyId;

                var dt = fixture.RetriveDataTable(@"
SELECT TOP 1 SessionId, SessionGuid, CompanyId, SaasApplicationId, CreatedByUserId,
       CreatedAt, UpdatedAt, SessionStatus, CurrentStepCode, StepStateJson, DataSourceDiscoveryJson,
       PlmConnectionEncrypted,
       CASE WHEN PlmConnectionEncrypted IS NULL OR LEN(PlmConnectionEncrypted)=0 THEN 0 ELSE 1 END AS HasPlmConnection
FROM dbo.AppPlmImportSession
WHERE CompanyId = @CompanyId AND SessionStatus = @Status
ORDER BY UpdatedAt DESC",
                    new List<DbParameter>
                    {
                        pCompany,
                        CreateParam(fixture, "@Status", SessionStatusInProgress)
                    });

                if (dt != null && dt.Rows.Count > 0)
                    result.Object = MapSessionRow(dt.Rows[0], includeConnection: true);
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmImportSessionDto), "Plm_Session_GetActive_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<PlmImportSessionDto> SaveImportSession(PlmImportSessionDto dto)
        {
            var result = new OperationCallResult<PlmImportSessionDto>();
            try
            {
                RequirePlmMigrationAdmin();
                if (dto == null)
                    throw new ArgumentNullException(nameof(dto));

                int companyId = ResolveCompanyId(dto.CompanyId);
                dto.CompanyId = companyId;

                if (!dto.SaasApplicationId.HasValue || dto.SaasApplicationId.Value <= 0)
                {
                    result.ValidationResult.Items.Add(new ValidationItem(
                        typeof(PlmImportSessionDto), "Plm_Session_AppRequired", ValidationItemType.Error,
                        "SaasApplicationId is required."));
                    return result;
                }

                var fixture = GetTenantFixture();
                var now = DateTime.UtcNow;
                string encryptedConn = null;
                if (!string.IsNullOrWhiteSpace(dto.PlmConnectionString))
                    encryptedConn = AppConnectionStringEncryptionBL.Encrypt(dto.PlmConnectionString.Trim());

                if (dto.SessionId.HasValue && dto.SessionId.Value > 0)
                {
                    var pId = fixture.CreateParameter("@SessionId");
                    pId.Value = dto.SessionId.Value;

                    string updateSql = @"
UPDATE dbo.AppPlmImportSession SET
    UpdatedAt = @UpdatedAt,
    SaasApplicationId = @SaasApplicationId,
    CurrentStepCode = @CurrentStepCode,
    StepStateJson = @StepStateJson,
    DataSourceDiscoveryJson = @DataSourceDiscoveryJson"
                        + (encryptedConn != null ? ", PlmConnectionEncrypted = @PlmConnectionEncrypted" : "")
                        + " WHERE SessionId = @SessionId AND CompanyId = @CompanyId AND SessionStatus = @Status";

                    var parms = new List<DbParameter>
                    {
                        CreateParam(fixture, "@UpdatedAt", now),
                        CreateParam(fixture, "@SaasApplicationId", dto.SaasApplicationId),
                        CreateParam(fixture, "@CurrentStepCode", dto.CurrentStepCode ?? StepConnect),
                        CreateParam(fixture, "@StepStateJson", (object)dto.StepStateJson ?? DBNull.Value),
                        CreateParam(fixture, "@DataSourceDiscoveryJson", (object)dto.DataSourceDiscoveryJson ?? DBNull.Value),
                        pId,
                        CreateParam(fixture, "@CompanyId", companyId),
                        CreateParam(fixture, "@Status", SessionStatusInProgress)
                    };
                    if (encryptedConn != null)
                        parms.Add(CreateParam(fixture, "@PlmConnectionEncrypted", encryptedConn));

                    fixture.ExecuteNonQueryResult(updateSql, parms);
                    result.Object = LoadSessionById(fixture, dto.SessionId.Value, includeConnection: true);
                    if (result.Object != null && !string.IsNullOrWhiteSpace(dto.PlmConnectionString))
                        result.Object.PlmConnectionString = dto.PlmConnectionString.Trim();
                }
                else
                {
                    string sessionGuid = string.IsNullOrWhiteSpace(dto.SessionGuid)
                        ? Guid.NewGuid().ToString("N")
                        : dto.SessionGuid;

                    const string insertSql = @"
INSERT INTO dbo.AppPlmImportSession
    (SessionGuid, CompanyId, SaasApplicationId, CreatedByUserId, CreatedAt, UpdatedAt,
     SessionStatus, CurrentStepCode, PlmConnectionEncrypted, StepStateJson, DataSourceDiscoveryJson)
VALUES
    (@SessionGuid, @CompanyId, @SaasApplicationId, @CreatedByUserId, @CreatedAt, @UpdatedAt,
     @Status, @CurrentStepCode, @PlmConnectionEncrypted, @StepStateJson, @DataSourceDiscoveryJson);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    var parms = new List<DbParameter>
                    {
                        CreateParam(fixture, "@SessionGuid", sessionGuid),
                        CreateParam(fixture, "@CompanyId", companyId),
                        CreateParam(fixture, "@SaasApplicationId", dto.SaasApplicationId),
                        CreateParam(fixture, "@CreatedByUserId", AppSecurityUserBL.CurrentUserId),
                        CreateParam(fixture, "@CreatedAt", now),
                        CreateParam(fixture, "@UpdatedAt", now),
                        CreateParam(fixture, "@Status", SessionStatusInProgress),
                        CreateParam(fixture, "@CurrentStepCode", dto.CurrentStepCode ?? StepConnect),
                        CreateParam(fixture, "@PlmConnectionEncrypted", (object)encryptedConn ?? DBNull.Value),
                        CreateParam(fixture, "@StepStateJson", (object)dto.StepStateJson ?? DBNull.Value),
                        CreateParam(fixture, "@DataSourceDiscoveryJson", (object)dto.DataSourceDiscoveryJson ?? DBNull.Value)
                    };

                    var newIdObj = fixture.RetriveScalar(insertSql, parms);
                    int newId = Convert.ToInt32(newIdObj);
                    WriteImportLog(fixture, newId, null, dto.CurrentStepCode ?? StepConnect, "SessionCreated", "Success", null, null, null, null, "Import session created.");
                    result.Object = LoadSessionById(fixture, newId, includeConnection: true);
                    if (result.Object != null && !string.IsNullOrWhiteSpace(dto.PlmConnectionString))
                        result.Object.PlmConnectionString = dto.PlmConnectionString.Trim();
                }
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmImportSessionDto), "Plm_Session_Save_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<bool> DiscardImportSession(int? sessionId, int? targetCompanyId)
        {
            var result = new OperationCallResult<bool> { Object = false };
            try
            {
                RequirePlmMigrationAdmin();
                int companyId = ResolveCompanyId(targetCompanyId);
                var fixture = GetTenantFixture();

                if (!sessionId.HasValue || sessionId.Value <= 0)
                {
                    var active = GetActiveImportSession(targetCompanyId);
                    if (active.Object == null)
                    {
                        result.Object = true;
                        return result;
                    }
                    sessionId = active.Object.SessionId;
                }

                fixture.ExecuteNonQueryResult(@"
UPDATE dbo.AppPlmImportSession
SET SessionStatus = @CompletedStatus, UpdatedAt = @UpdatedAt
WHERE SessionId = @SessionId AND CompanyId = @CompanyId AND SessionStatus = @InProgress",
                    new List<DbParameter>
                    {
                        CreateParam(fixture, "@CompletedStatus", SessionStatusCompleted),
                        CreateParam(fixture, "@UpdatedAt", DateTime.UtcNow),
                        CreateParam(fixture, "@SessionId", sessionId.Value),
                        CreateParam(fixture, "@CompanyId", companyId),
                        CreateParam(fixture, "@InProgress", SessionStatusInProgress)
                    });

                WriteImportLog(fixture, sessionId.Value, null, StepConnect, "SessionDiscarded", "Success", null, null, null, null, "Import session discarded by user.");
                result.Object = true;
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmImportSessionDto), "Plm_Session_Discard_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        #endregion

        #region Jobs & logs

        public static OperationCallResult<PlmImportJobDto> GetImportJob(int jobId)
        {
            var result = new OperationCallResult<PlmImportJobDto>();
            try
            {
                RequirePlmMigrationAdmin();
                var fixture = GetTenantFixture();

                var p = fixture.CreateParameter("@JobId");
                p.Value = jobId;

                var dt = fixture.RetriveDataTable(@"
SELECT JobId, SessionId, JobType, Status, ProgressPercent, ProgressMessage,
       ResultJson, ErrorMessage, CreatedAt, UpdatedAt, StartedAt, CompletedAt
FROM dbo.AppPlmImportJob WHERE JobId = @JobId",
                    new List<DbParameter> { p });

                if (dt != null && dt.Rows.Count > 0)
                    result.Object = MapJobRow(dt.Rows[0]);
                else
                    result.ValidationResult.Items.Add(new ValidationItem(
                        typeof(PlmImportJobDto), "Plm_Job_NotFound", ValidationItemType.Error, "Job not found."));
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmImportJobDto), "Plm_Job_Get_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<bool> CancelImportJob(int jobId)
        {
            var result = new OperationCallResult<bool> { Object = false };
            try
            {
                RequirePlmMigrationAdmin();
                RequestJobCancellation(jobId);
                var fixture = GetTenantFixture();
                fixture.ExecuteNonQueryResult(@"
UPDATE dbo.AppPlmImportJob
SET Status = @Status, UpdatedAt = @UpdatedAt, CompletedAt = @UpdatedAt,
    ProgressMessage = COALESCE(ProgressMessage, '') + ' (cancel requested)'
WHERE JobId = @JobId AND Status IN (@Queued, @Running)",
                    new List<DbParameter>
                    {
                        CreateParam(fixture, "@Status", JobStatusCancelled),
                        CreateParam(fixture, "@UpdatedAt", DateTime.UtcNow),
                        CreateParam(fixture, "@JobId", jobId),
                        CreateParam(fixture, "@Queued", JobStatusQueued),
                        CreateParam(fixture, "@Running", JobStatusRunning)
                    });
                result.Object = true;
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmImportJobDto), "Plm_Job_Cancel_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<List<PlmImportLogDto>> GetImportLog(int? sessionId, int? targetCompanyId)
        {
            var result = new OperationCallResult<List<PlmImportLogDto>> { Object = new List<PlmImportLogDto>() };
            try
            {
                RequirePlmMigrationAdmin();
                ResolveCompanyId(targetCompanyId);
                var fixture = GetTenantFixture();

                if (!sessionId.HasValue || sessionId.Value <= 0)
                {
                    var active = GetActiveImportSession(targetCompanyId);
                    if (active.Object?.SessionId == null)
                        return result;
                    sessionId = active.Object.SessionId;
                }

                var p = fixture.CreateParameter("@SessionId");
                p.Value = sessionId.Value;

                var dt = fixture.RetriveDataTable(@"
SELECT LogId, SessionId, JobId, StepCode, Action, Status, TargetKey, PlmIntegrationKey,
       RowsAffected, DurationMs, Message, CreatedAt
FROM dbo.AppPlmImportLog
WHERE SessionId = @SessionId
ORDER BY LogId DESC",
                    new List<DbParameter> { p });

                if (dt == null) return result;
                foreach (DataRow row in dt.Rows)
                    result.Object.Add(MapLogRow(row));
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmImportLogDto), "Plm_Log_Get_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        internal static int CreateStubJob(DatabaseFixture fixture, int sessionId, string jobType, string message)
        {
            var now = DateTime.UtcNow;
            const string sql = @"
INSERT INTO dbo.AppPlmImportJob
    (SessionId, JobType, Status, ProgressPercent, ProgressMessage, CreatedAt, UpdatedAt, CompletedAt, ErrorMessage)
VALUES
    (@SessionId, @JobType, @Status, 0, @ProgressMessage, @CreatedAt, @UpdatedAt, @CompletedAt, @ErrorMessage);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var jobIdObj = fixture.RetriveScalar(sql, new List<DbParameter>
            {
                CreateParam(fixture, "@SessionId", sessionId),
                CreateParam(fixture, "@JobType", jobType),
                CreateParam(fixture, "@Status", JobStatusFailed),
                CreateParam(fixture, "@ProgressMessage", message),
                CreateParam(fixture, "@CreatedAt", now),
                CreateParam(fixture, "@UpdatedAt", now),
                CreateParam(fixture, "@CompletedAt", now),
                CreateParam(fixture, "@ErrorMessage", message)
            });
            return Convert.ToInt32(jobIdObj);
        }

        internal static void WriteImportLog(
            DatabaseFixture fixture, int sessionId, int? jobId, string stepCode, string action,
            string status, string targetKey, string plmIntegrationKey, int? rowsAffected, int? durationMs, string message)
        {
            fixture.ExecuteNonQueryResult(@"
INSERT INTO dbo.AppPlmImportLog
    (SessionId, JobId, StepCode, Action, Status, TargetKey, PlmIntegrationKey, RowsAffected, DurationMs, Message, CreatedAt)
VALUES
    (@SessionId, @JobId, @StepCode, @Action, @Status, @TargetKey, @PlmIntegrationKey, @RowsAffected, @DurationMs, @Message, @CreatedAt)",
                new List<DbParameter>
                {
                    CreateParam(fixture, "@SessionId", sessionId),
                    CreateParam(fixture, "@JobId", (object)jobId ?? DBNull.Value),
                    CreateParam(fixture, "@StepCode", (object)stepCode ?? DBNull.Value),
                    CreateParam(fixture, "@Action", (object)action ?? DBNull.Value),
                    CreateParam(fixture, "@Status", (object)status ?? DBNull.Value),
                    CreateParam(fixture, "@TargetKey", (object)targetKey ?? DBNull.Value),
                    CreateParam(fixture, "@PlmIntegrationKey", (object)plmIntegrationKey ?? DBNull.Value),
                    CreateParam(fixture, "@RowsAffected", (object)rowsAffected ?? DBNull.Value),
                    CreateParam(fixture, "@DurationMs", (object)durationMs ?? DBNull.Value),
                    CreateParam(fixture, "@Message", (object)message ?? DBNull.Value),
                    CreateParam(fixture, "@CreatedAt", DateTime.UtcNow)
                });
        }

        #endregion

        #region Step state & table prefixes

        internal sealed class PlmImportPrefixSettings
        {
            public string TablePrefix { get; set; } = DefaultTablePrefix;
            public string EntityWideTablePrefix { get; set; } = DefaultEntityWideTablePrefix;
        }

        private sealed class PlmImportStepStateJson
        {
            public bool connectionTested { get; set; }
            public bool systemDefineTablesComplete { get; set; }
            public bool systemDefineEntitiesComplete { get; set; }
            public bool userDefineEntitiesComplete { get; set; }
            public string tablePrefix { get; set; }
            public string entityWideTablePrefix { get; set; }
        }

        internal static PlmImportPrefixSettings ResolveImportPrefixes(string stepStateJson)
        {
            var settings = new PlmImportPrefixSettings();
            if (string.IsNullOrWhiteSpace(stepStateJson))
                return settings;

            try
            {
                var state = JsonConvert.DeserializeObject<PlmImportStepStateJson>(stepStateJson);
                if (state == null)
                    return settings;

                settings.TablePrefix = SanitizeImportTablePrefix(state.tablePrefix, DefaultTablePrefix);
                settings.EntityWideTablePrefix = SanitizeImportTablePrefix(
                    state.entityWideTablePrefix, DefaultEntityWideTablePrefix);
            }
            catch
            {
                // keep defaults
            }

            return settings;
        }

        internal static string SanitizeImportTablePrefix(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(fallback))
                fallback = DefaultTablePrefix;

            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            var sb = new System.Text.StringBuilder();
            foreach (char ch in value.Trim())
            {
                if (char.IsLetterOrDigit(ch) || ch == '_')
                    sb.Append(ch);
            }

            string result = sb.ToString();
            if (result.Length == 0)
                return fallback;

            return result.Length <= 30 ? result : result.Substring(0, 30);
        }

        #endregion

        #region Mapping helpers

        private static PlmImportSessionDto LoadSessionById(DatabaseFixture fixture, int sessionId, bool includeConnection)
        {
            var p = fixture.CreateParameter("@SessionId");
            p.Value = sessionId;
            var dt = fixture.RetriveDataTable(@"
SELECT SessionId, SessionGuid, CompanyId, SaasApplicationId, CreatedByUserId,
       CreatedAt, UpdatedAt, SessionStatus, CurrentStepCode, StepStateJson, DataSourceDiscoveryJson,
       PlmConnectionEncrypted,
       CASE WHEN PlmConnectionEncrypted IS NULL OR LEN(PlmConnectionEncrypted)=0 THEN 0 ELSE 1 END AS HasPlmConnection
FROM dbo.AppPlmImportSession WHERE SessionId = @SessionId",
                new List<DbParameter> { p });
            if (dt == null || dt.Rows.Count == 0) return null;
            return MapSessionRow(dt.Rows[0], includeConnection);
        }

        private static PlmImportSessionDto MapSessionRow(DataRow row, bool includeConnection)
        {
            var dto = new PlmImportSessionDto
            {
                SessionId = row["SessionId"] as int? ?? Convert.ToInt32(row["SessionId"]),
                SessionGuid = row["SessionGuid"] as string,
                CompanyId = row["CompanyId"] as int? ?? Convert.ToInt32(row["CompanyId"]),
                SaasApplicationId = row["SaasApplicationId"] as int?,
                CreatedByUserId = row["CreatedByUserId"] as int?,
                CreatedAt = row["CreatedAt"] as DateTime?,
                UpdatedAt = row["UpdatedAt"] as DateTime?,
                SessionStatus = row["SessionStatus"] as string,
                CurrentStepCode = row["CurrentStepCode"] as string,
                StepStateJson = row["StepStateJson"] as string,
                DataSourceDiscoveryJson = row["DataSourceDiscoveryJson"] as string,
                HasPlmConnection = Convert.ToInt32(row["HasPlmConnection"]) == 1
            };
            if (includeConnection && row.Table.Columns.Contains("PlmConnectionEncrypted"))
            {
                var enc = row["PlmConnectionEncrypted"] as string;
                if (!string.IsNullOrWhiteSpace(enc))
                {
                    try { dto.PlmConnectionString = AppConnectionStringEncryptionBL.Decrypt(enc); }
                    catch { dto.HasPlmConnection = true; }
                }
            }
            return dto;
        }

        private static PlmImportJobDto MapJobRow(DataRow row)
        {
            return new PlmImportJobDto
            {
                JobId = Convert.ToInt32(row["JobId"]),
                SessionId = Convert.ToInt32(row["SessionId"]),
                JobType = row["JobType"] as string,
                Status = row["Status"] as string,
                ProgressPercent = row["ProgressPercent"] as int? ?? Convert.ToInt32(row["ProgressPercent"]),
                ProgressMessage = row["ProgressMessage"] as string,
                ResultJson = row["ResultJson"] as string,
                ErrorMessage = row["ErrorMessage"] as string,
                CreatedAt = row["CreatedAt"] as DateTime?,
                UpdatedAt = row["UpdatedAt"] as DateTime?,
                StartedAt = row["StartedAt"] as DateTime?,
                CompletedAt = row["CompletedAt"] as DateTime?
            };
        }

        private static PlmImportLogDto MapLogRow(DataRow row)
        {
            return new PlmImportLogDto
            {
                LogId = Convert.ToInt32(row["LogId"]),
                SessionId = Convert.ToInt32(row["SessionId"]),
                JobId = row["JobId"] as int?,
                StepCode = row["StepCode"] as string,
                Action = row["Action"] as string,
                Status = row["Status"] as string,
                TargetKey = row["TargetKey"] as string,
                PlmIntegrationKey = row["PlmIntegrationKey"] as string,
                RowsAffected = row["RowsAffected"] as int?,
                DurationMs = row["DurationMs"] as int?,
                Message = row["Message"] as string,
                CreatedAt = row["CreatedAt"] as DateTime?
            };
        }

        private static DbParameter CreateParam(DatabaseFixture fixture, string name, object value)
        {
            var p = fixture.CreateParameter(name);
            p.Value = value ?? DBNull.Value;
            return p;
        }

        #endregion
    }
}
