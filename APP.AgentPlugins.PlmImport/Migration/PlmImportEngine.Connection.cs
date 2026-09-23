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
using Newtonsoft.Json.Linq;

namespace APP.AgentPlugins.PlmImport
{
    public static partial class PlmImportEngine
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
    ALTER TABLE dbo.AppTransactionField ADD IntegrationId NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME='AppSearch' AND COLUMN_NAME='IntegrationId')
    ALTER TABLE dbo.AppSearch ADD IntegrationId NVARCHAR(100) NULL;";

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
        ChatSessionKey          NVARCHAR(200)     NULL,
        PlmConnectionEncrypted  NVARCHAR(MAX)     NULL,
        StepStateJson           NVARCHAR(MAX)     NULL,
        DataSourceDiscoveryJson NVARCHAR(MAX)     NULL,
        CONSTRAINT PK_AppPlmImportSession PRIMARY KEY (SessionId),
        CONSTRAINT UQ_AppPlmImportSession_Guid UNIQUE (SessionGuid)
    );
    CREATE INDEX IX_AppPlmImportSession_CompanyStatus ON dbo.AppPlmImportSession (CompanyId, SessionStatus);
    CREATE INDEX IX_AppPlmImportSession_CompanyChat ON dbo.AppPlmImportSession (CompanyId, ChatSessionKey);
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

        private const string EnsureFolderMapTableSql = @"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME='AppPlmFolderMap')
BEGIN
    CREATE TABLE dbo.AppPlmFolderMap (
        MapId             INT IDENTITY(1,1) NOT NULL,
        SessionId         INT               NOT NULL,
        CompanyId         INT               NOT NULL,
        PlmFolderId       INT               NOT NULL,
        AppFolderId       INT               NOT NULL,
        PlmFolderType     INT               NOT NULL,
        AppTransactionId  INT               NULL,
        AppFolderType     INT               NOT NULL,
        PlmParentId       INT               NULL,
        PlmName           NVARCHAR(200)     NULL,
        AppName           NVARCHAR(200)     NULL,
        LastSyncAt        DATETIME          NOT NULL,
        CONSTRAINT PK_AppPlmFolderMap PRIMARY KEY (MapId),
        CONSTRAINT UQ_AppPlmFolderMap_Scope UNIQUE (SessionId, PlmFolderId)
    );
    CREATE INDEX IX_AppPlmFolderMap_App ON dbo.AppPlmFolderMap (AppFolderId);
    CREATE INDEX IX_AppPlmFolderMap_Plm ON dbo.AppPlmFolderMap (PlmFolderId, PlmFolderType);
END";

        private const string EnsureColorGroupDetailTableSql = @"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME='Plm_pdmColorGroupDetail')
BEGIN
    CREATE TABLE dbo.Plm_pdmColorGroupDetail (
        ColorGroupDetailID INT           NOT NULL,
        ColorGroupID       INT           NULL,
        RGBColorID         INT           NOT NULL,
        PlmFolderId        INT           NOT NULL,
        AppFolderId        INT           NULL,
        CONSTRAINT PK_Plm_pdmColorGroupDetail PRIMARY KEY (ColorGroupDetailID)
    );
    CREATE INDEX IX_Plm_pdmColorGroupDetail_RGBColorID ON dbo.Plm_pdmColorGroupDetail (RGBColorID);
    CREATE INDEX IX_Plm_pdmColorGroupDetail_AppFolderId ON dbo.Plm_pdmColorGroupDetail (AppFolderId);
END";

        private const string EnsureSessionRegisterColumnsSql = @"
IF COL_LENGTH('dbo.AppPlmImportSession', 'PlmDataSourceRegisterId') IS NULL
    ALTER TABLE dbo.AppPlmImportSession ADD PlmDataSourceRegisterId INT NULL;
IF COL_LENGTH('dbo.AppPlmImportSession', 'PlmDwDataSourceRegisterId') IS NULL
    ALTER TABLE dbo.AppPlmImportSession ADD PlmDwDataSourceRegisterId INT NULL;
IF COL_LENGTH('dbo.AppPlmImportSession', 'ErpDataSourceRegisterId') IS NULL
    ALTER TABLE dbo.AppPlmImportSession ADD ErpDataSourceRegisterId INT NULL;
IF COL_LENGTH('dbo.AppPlmImportSession', 'PlmExDbDataSourceRegisterId') IS NULL
    ALTER TABLE dbo.AppPlmImportSession ADD PlmExDbDataSourceRegisterId INT NULL;
IF COL_LENGTH('dbo.AppPlmImportSession', 'ChatSessionKey') IS NULL
    ALTER TABLE dbo.AppPlmImportSession ADD ChatSessionKey NVARCHAR(200) NULL;";

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
            fixture.ExecuteNonQueryResult(EnsureSessionRegisterColumnsSql, new List<DbParameter>());
            fixture.ExecuteNonQueryResult(EnsureJobTableSql, new List<DbParameter>());
            fixture.ExecuteNonQueryResult(EnsureLogTableSql, new List<DbParameter>());
            fixture.ExecuteNonQueryResult(EnsureFolderMapTableSql, new List<DbParameter>());
            fixture.ExecuteNonQueryResult(EnsureColorGroupDetailTableSql, new List<DbParameter>());
        }

        /// <summary>
        /// Resolve a tenant AppDataSourceRegister connection string. Never expose to Agent/ask_user.
        /// </summary>
        internal static string ResolveConnectionStringFromRegisterId(int dataSourceRegisterId)
        {
            if (dataSourceRegisterId <= 0)
                throw new ArgumentException("DataSourceRegisterId is required.");

            var reg = AppDataSourceRegisterBL.RetrieveOneAppDataSourceRegisterEntity(dataSourceRegisterId);
            if (reg == null)
                throw new InvalidOperationException($"DataSourceRegisterId {dataSourceRegisterId} was not found on this tenant.");
            if (string.IsNullOrWhiteSpace(reg.ConnectionString))
                throw new InvalidOperationException($"DataSourceRegisterId {dataSourceRegisterId} has no connection string configured.");

            return AppConnectionStringEncryptionBL.Decrypt(reg.ConnectionString);
        }

        /// <summary>
        /// Resolve PLM connection for engine use from session register id (preferred) or legacy encrypted field.
        /// </summary>
        internal static string RequireResolvedPlmConnection(PlmImportSessionDto session)
        {
            if (session == null)
                throw new InvalidOperationException("Import session not found.");

            if (session.PlmDataSourceRegisterId.HasValue && session.PlmDataSourceRegisterId.Value > 0)
                return ResolveConnectionStringFromRegisterId(session.PlmDataSourceRegisterId.Value);

            if (!string.IsNullOrWhiteSpace(session.PlmConnectionString))
                return session.PlmConnectionString.Trim();

            throw new InvalidOperationException(
                "PLM DataSourceRegisterId is required on this session. Use list_tenant_data_sources and save_plm_import_session with plmDataSourceRegisterId.");
        }

        #endregion

        #region Connection test & list

        public static OperationCallResult<PlmConnectionTestResultDto> TestPlmConnection(PlmConnectionTestRequestDto request)
        {
            var result = new OperationCallResult<PlmConnectionTestResultDto> { Object = new PlmConnectionTestResultDto() };
            try
            {
                RequirePlmMigrationAdmin();
                if (request != null)
                    ResolveCompanyId(request.TargetCompanyId);

                if (request?.DataSourceRegisterId == null || request.DataSourceRegisterId.Value <= 0)
                {
                    result.ValidationResult.Items.Add(new ValidationItem(
                        typeof(PlmConnectionTestRequestDto), "Plm_Connection_RegisterRequired", ValidationItemType.Error,
                        "DataSourceRegisterId is required. Do not pass a connection string."));
                    return result;
                }

                int registerId = request.DataSourceRegisterId.Value;
                result.Object.DataSourceRegisterId = registerId;

                var reg = AppDataSourceRegisterBL.RetrieveOneAppDataSourceRegisterExDto(registerId);
                if (reg == null)
                {
                    result.Object.IsSuccess = false;
                    result.Object.ErrorMessage = $"DataSourceRegisterId {registerId} was not found on this tenant.";
                    result.ValidationResult.Items.Add(new ValidationItem(
                        typeof(PlmConnectionTestRequestDto), "Plm_Connection_RegisterNotFound", ValidationItemType.Error,
                        result.Object.ErrorMessage));
                    return result;
                }

                result.Object.DataSourceName = reg.DataSourceName;
                result.Object.DatabaseName = reg.DatabaseName;

                string conn = ResolveConnectionStringFromRegisterId(registerId);
                using (var sqlConn = new SqlConnection(conn))
                {
                    sqlConn.Open();
                    result.Object.IsSuccess = true;
                    result.Object.ServerVersion = sqlConn.ServerVersion;
                    if (string.IsNullOrWhiteSpace(result.Object.DatabaseName))
                        result.Object.DatabaseName = sqlConn.Database;
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

        /// <summary>
        /// List tenant-visible DataSource registers for ask_user DDL (Id + Name only).
        /// Same source as SQL Workbench / Database Management dropdown:
        /// <see cref="AppDataSourceRegisterBL.GetDataSourceRegisterList"/> (no Admin gate; no connection strings).
        /// </summary>
        public static OperationCallResult<PlmListTenantDataSourcesResultDto> ListTenantDataSources(PlmListTenantDataSourcesRequestDto request)
        {
            var result = new OperationCallResult<PlmListTenantDataSourcesResultDto>
            {
                Object = new PlmListTenantDataSourcesResultDto()
            };
            try
            {
                // Align with UI pages (Workbench etc.): company-scoped list, connection strings cleared.
                // Do not require SaasCompanyAdmin — listing Ids/Names is not a PLM import write.
                _ = request;

                foreach (var reg in AppDataSourceRegisterBL.GetDataSourceRegisterList())
                {
                    if (reg == null || reg.Id == null)
                        continue;
                    int id = Convert.ToInt32(reg.Id);
                    if (id <= 0)
                        continue;

                    result.Object.DataSources.Add(new PlmTenantDataSourceItemDto
                    {
                        DataSourceRegisterId = id,
                        DataSourceName = reg.DataSourceName,
                        DatabaseName = reg.DatabaseName
                    });
                }

                result.Object.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmListTenantDataSourcesRequestDto), "Plm_ListDataSources_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        /// <summary>
        /// List tenant SaaS Application packages for ask_user DDL (Id + Name only).
        /// Same ids as save_plm_import_session.saasApplicationId (root AppListMenu LinkType=ApplicationPackage).
        /// Prefer this over platform list_applications (full TX/Search tree) for Gate-0.
        /// </summary>
        public static OperationCallResult<PlmListTenantSaasApplicationsResultDto> ListTenantSaasApplications(
            PlmListTenantSaasApplicationsRequestDto request)
        {
            var result = new OperationCallResult<PlmListTenantSaasApplicationsResultDto>
            {
                Object = new PlmListTenantSaasApplicationsResultDto()
            };
            try
            {
                _ = request;

                // Canonical package list (MenuId = SaasApplicationId).
                foreach (var app in AppSaasUserApplicationPackageBL.GetSaasApplicationList(excludeChildMenu: true))
                {
                    if (app?.Id == null)
                        continue;
                    int id = Convert.ToInt32(app.Id);
                    if (id <= 0)
                        continue;
                    result.Object.Applications.Add(new PlmTenantSaasApplicationItemDto
                    {
                        SaasApplicationId = id,
                        ApplicationName = app.Name
                    });
                }

                // Fallback: root ApplicationPackage menus (LinkType=10) if BL returned empty.
                if (result.Object.Applications.Count == 0)
                {
                    var fixture = GetTenantFixture();
                    var dt = fixture.RetriveDataTable(@"
SELECT MenuID, Name
FROM dbo.AppListMenu
WHERE (ParentID IS NULL OR ParentID = 0)
  AND LinkType = 10
ORDER BY MenuID",
                        new List<DbParameter>());
                    if (dt != null)
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            int id = Convert.ToInt32(row["MenuID"]);
                            if (id <= 0)
                                continue;
                            result.Object.Applications.Add(new PlmTenantSaasApplicationItemDto
                            {
                                SaasApplicationId = id,
                                ApplicationName = row["Name"]?.ToString()
                            });
                        }
                    }
                }

                result.Object.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmListTenantSaasApplicationsRequestDto), "Plm_ListSaasApplications_Error",
                    ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        /// <summary>Obsolete: connection-string discover from PLM pdmDataSource is disabled for security.</summary>
        public static OperationCallResult<PlmDiscoverDataSourcesResultDto> DiscoverPlmDataSources(PlmDiscoverDataSourcesRequestDto request)
        {
            var result = new OperationCallResult<PlmDiscoverDataSourcesResultDto>
            {
                Object = new PlmDiscoverDataSourcesResultDto
                {
                    IsSuccess = false,
                    ErrorMessage =
                        "discover_plm_data_sources is removed. Use list_tenant_data_sources and bind Plm/PlmDw/Erp/ExDb DataSourceRegisterIds from ask_user. Do not pass connection strings; never create new registers from PLM."
                }
            };
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(PlmDiscoverDataSourcesRequestDto), "Plm_Discover_Disabled", ValidationItemType.Error,
                result.Object.ErrorMessage));
            return result;
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
       PlmConnectionEncrypted, ChatSessionKey,
       PlmDataSourceRegisterId, PlmDwDataSourceRegisterId, ErpDataSourceRegisterId, PlmExDbDataSourceRegisterId,
       CASE WHEN PlmDataSourceRegisterId IS NOT NULL AND PlmDataSourceRegisterId > 0 THEN 1
            WHEN PlmConnectionEncrypted IS NULL OR LEN(PlmConnectionEncrypted)=0 THEN 0 ELSE 1 END AS HasPlmConnection
FROM dbo.AppPlmImportSession
WHERE CompanyId = @CompanyId AND SessionStatus = @Status
ORDER BY UpdatedAt DESC",
                    new List<DbParameter>
                    {
                        pCompany,
                        CreateParam(fixture, "@Status", SessionStatusInProgress)
                    });

                if (dt != null && dt.Rows.Count > 0)
                    result.Object = MapSessionRow(dt.Rows[0], includeConnection: false);
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmImportSessionDto), "Plm_Session_GetActive_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        /// <summary>InProgress job for this Chat. Does not fall back to another Chat's company session.</summary>
        public static OperationCallResult<PlmImportSessionDto> GetImportSessionForChat(string chatSessionKey, int? targetCompanyId)
        {
            var result = new OperationCallResult<PlmImportSessionDto>();
            try
            {
                RequirePlmMigrationAdmin();
                int companyId = ResolveCompanyId(targetCompanyId);
                var fixture = GetTenantFixture();
                result.Object = LoadInProgressByChat(fixture, companyId, chatSessionKey);
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmImportSessionDto), "Plm_Session_GetByChat_Error", ValidationItemType.Error, ex.Message));
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

                if (!string.IsNullOrWhiteSpace(dto.PlmConnectionString))
                {
                    result.ValidationResult.Items.Add(new ValidationItem(
                        typeof(PlmImportSessionDto), "Plm_Session_ConnectionStringRejected", ValidationItemType.Error,
                        "Do not pass PlmConnectionString. Bind plmDataSourceRegisterId (and optional plmDw/erp/exDb register ids) from list_tenant_data_sources."));
                    return result;
                }

                if (dto.PlmDataSourceRegisterId.HasValue && dto.PlmDataSourceRegisterId.Value > 0)
                    ResolveConnectionStringFromRegisterId(dto.PlmDataSourceRegisterId.Value);
                if (dto.PlmDwDataSourceRegisterId.HasValue && dto.PlmDwDataSourceRegisterId.Value > 0)
                    ResolveConnectionStringFromRegisterId(dto.PlmDwDataSourceRegisterId.Value);
                if (dto.ErpDataSourceRegisterId.HasValue && dto.ErpDataSourceRegisterId.Value > 0)
                    ResolveConnectionStringFromRegisterId(dto.ErpDataSourceRegisterId.Value);
                if (dto.PlmExDbDataSourceRegisterId.HasValue && dto.PlmExDbDataSourceRegisterId.Value > 0)
                    ResolveConnectionStringFromRegisterId(dto.PlmExDbDataSourceRegisterId.Value);

                var fixture = GetTenantFixture();
                var now = DateTime.UtcNow;

                if ((!dto.SessionId.HasValue || dto.SessionId.Value <= 0)
                    && !string.IsNullOrWhiteSpace(dto.ChatSessionKey))
                {
                    var existingForChat = LoadInProgressByChat(fixture, companyId, dto.ChatSessionKey);
                    if (existingForChat?.SessionId != null && existingForChat.SessionId.Value > 0)
                        dto.SessionId = existingForChat.SessionId;
                }

                if (dto.SessionId.HasValue && dto.SessionId.Value > 0)
                {
                    var existing = LoadSessionById(fixture, dto.SessionId.Value, includeConnection: false);
                    MergeSessionRegisterIds(dto, existing);
                }

                dto.DataSourceDiscoveryJson = BuildDataSourceDiscoveryJson(dto);

                if (dto.SessionId.HasValue && dto.SessionId.Value > 0)
                {
                    var pId = fixture.CreateParameter("@SessionId");
                    pId.Value = dto.SessionId.Value;

                    string updateSql = @"
UPDATE dbo.AppPlmImportSession SET
    UpdatedAt = @UpdatedAt,
    SaasApplicationId = @SaasApplicationId,
    CurrentStepCode = @CurrentStepCode,
    StepStateJson = @StepStateJson"
                        + (!string.IsNullOrWhiteSpace(dto.ChatSessionKey)
                            ? ", ChatSessionKey = @ChatSessionKey"
                            : "")
                        + ", DataSourceDiscoveryJson = @DataSourceDiscoveryJson"
                        + (dto.PlmDataSourceRegisterId.HasValue
                            ? ", PlmDataSourceRegisterId = @PlmDataSourceRegisterId"
                            : "")
                        + (dto.PlmDwDataSourceRegisterId.HasValue
                            ? ", PlmDwDataSourceRegisterId = @PlmDwDataSourceRegisterId"
                            : "")
                        + (dto.ErpDataSourceRegisterId.HasValue
                            ? ", ErpDataSourceRegisterId = @ErpDataSourceRegisterId"
                            : "")
                        + (dto.PlmExDbDataSourceRegisterId.HasValue
                            ? ", PlmExDbDataSourceRegisterId = @PlmExDbDataSourceRegisterId"
                            : "")
                        + " WHERE SessionId = @SessionId AND CompanyId = @CompanyId AND SessionStatus = @Status";

                    var parms = new List<DbParameter>
                    {
                        CreateParam(fixture, "@UpdatedAt", now),
                        CreateParam(fixture, "@SaasApplicationId", dto.SaasApplicationId),
                        CreateParam(fixture, "@CurrentStepCode", dto.CurrentStepCode ?? StepConnect),
                        CreateParam(fixture, "@StepStateJson", (object)dto.StepStateJson ?? DBNull.Value),
                        pId,
                        CreateParam(fixture, "@CompanyId", companyId),
                        CreateParam(fixture, "@Status", SessionStatusInProgress)
                    };
                    parms.Add(CreateParam(fixture, "@DataSourceDiscoveryJson",
                        (object)dto.DataSourceDiscoveryJson ?? DBNull.Value));
                    if (dto.PlmDataSourceRegisterId.HasValue)
                        parms.Add(CreateParam(fixture, "@PlmDataSourceRegisterId", dto.PlmDataSourceRegisterId));
                    if (dto.PlmDwDataSourceRegisterId.HasValue)
                        parms.Add(CreateParam(fixture, "@PlmDwDataSourceRegisterId", dto.PlmDwDataSourceRegisterId));
                    if (dto.ErpDataSourceRegisterId.HasValue)
                        parms.Add(CreateParam(fixture, "@ErpDataSourceRegisterId", dto.ErpDataSourceRegisterId));
                    if (dto.PlmExDbDataSourceRegisterId.HasValue)
                        parms.Add(CreateParam(fixture, "@PlmExDbDataSourceRegisterId", dto.PlmExDbDataSourceRegisterId));
                    if (!string.IsNullOrWhiteSpace(dto.ChatSessionKey))
                        parms.Add(CreateParam(fixture, "@ChatSessionKey", dto.ChatSessionKey.Trim()));

                    fixture.ExecuteNonQueryResult(updateSql, parms);
                    result.Object = LoadSessionById(fixture, dto.SessionId.Value, includeConnection: false);
                }
                else
                {
                    string sessionGuid = string.IsNullOrWhiteSpace(dto.SessionGuid)
                        ? Guid.NewGuid().ToString("N")
                        : dto.SessionGuid;

                    const string insertSql = @"
INSERT INTO dbo.AppPlmImportSession
    (SessionGuid, CompanyId, SaasApplicationId, CreatedByUserId, CreatedAt, UpdatedAt,
     SessionStatus, CurrentStepCode, ChatSessionKey, PlmConnectionEncrypted, StepStateJson, DataSourceDiscoveryJson,
     PlmDataSourceRegisterId, PlmDwDataSourceRegisterId, ErpDataSourceRegisterId, PlmExDbDataSourceRegisterId)
VALUES
    (@SessionGuid, @CompanyId, @SaasApplicationId, @CreatedByUserId, @CreatedAt, @UpdatedAt,
     @Status, @CurrentStepCode, @ChatSessionKey, NULL, @StepStateJson, @DataSourceDiscoveryJson,
     @PlmDataSourceRegisterId, @PlmDwDataSourceRegisterId, @ErpDataSourceRegisterId, @PlmExDbDataSourceRegisterId);
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
                        CreateParam(fixture, "@ChatSessionKey",
                            string.IsNullOrWhiteSpace(dto.ChatSessionKey) ? (object)DBNull.Value : dto.ChatSessionKey.Trim()),
                        CreateParam(fixture, "@StepStateJson", (object)dto.StepStateJson ?? DBNull.Value),
                        CreateParam(fixture, "@DataSourceDiscoveryJson", (object)dto.DataSourceDiscoveryJson ?? DBNull.Value),
                        CreateParam(fixture, "@PlmDataSourceRegisterId", (object)dto.PlmDataSourceRegisterId ?? DBNull.Value),
                        CreateParam(fixture, "@PlmDwDataSourceRegisterId", (object)dto.PlmDwDataSourceRegisterId ?? DBNull.Value),
                        CreateParam(fixture, "@ErpDataSourceRegisterId", (object)dto.ErpDataSourceRegisterId ?? DBNull.Value),
                        CreateParam(fixture, "@PlmExDbDataSourceRegisterId", (object)dto.PlmExDbDataSourceRegisterId ?? DBNull.Value)
                    };

                    var newIdObj = fixture.RetriveScalar(insertSql, parms);
                    int newId = Convert.ToInt32(newIdObj);
                    WriteImportLog(fixture, newId, null, dto.CurrentStepCode ?? StepConnect, "SessionCreated", "Success", null, null, null, null, "Import session created.");
                    result.Object = LoadSessionById(fixture, newId, includeConnection: false);
                }
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmImportSessionDto), "Plm_Session_Save_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<bool> DiscardImportSession(
            int? sessionId,
            int? targetCompanyId,
            string chatSessionKey = null)
        {
            var result = new OperationCallResult<bool> { Object = false };
            try
            {
                RequirePlmMigrationAdmin();
                int companyId = ResolveCompanyId(targetCompanyId);
                var fixture = GetTenantFixture();

                if (!sessionId.HasValue || sessionId.Value <= 0)
                {
                    var forChat = LoadInProgressByChat(fixture, companyId, chatSessionKey);
                    if (forChat?.SessionId == null)
                    {
                        if (!string.IsNullOrWhiteSpace(chatSessionKey))
                            DeleteChatSharedContext(fixture, chatSessionKey.Trim(), WizardSharedContextKey);
                        result.Object = true;
                        return result;
                    }
                    sessionId = forChat.SessionId;
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
                if (!string.IsNullOrWhiteSpace(chatSessionKey))
                    DeleteChatSharedContext(fixture, chatSessionKey.Trim(), WizardSharedContextKey);
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
                    return result;

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
            public bool templatesComplete { get; set; }
            public string tablePrefix { get; set; }
            public string entityWideTablePrefix { get; set; }
            public string templateImportSettingJson { get; set; }

            /// <summary>Legacy only. Wizard SoT is AppAgentSharedContext ScopeId=ChatSessionKey.</summary>
            public string agentWizardJson { get; set; }
        }

        public const string WizardSharedContextKey = "plm.integration.wizard";

        /// <summary>
        /// Persist wizard onto AppAgentSharedContext with ScopeId = ChatSessionKey
        /// (survives restart; not WorkflowId). Does not require AppPlmImportSession.
        /// </summary>
        public static OperationCallResult<object> UpdateWizardProgress(
            int? sessionId,
            string wizardJson,
            string currentStepCode,
            int? targetCompanyId,
            string chatSessionKey = null)
        {
            var result = new OperationCallResult<object>();
            try
            {
                RequirePlmMigrationAdmin();
                if (string.IsNullOrWhiteSpace(wizardJson))
                    throw new ArgumentException("wizardJson is required.");
                if (string.IsNullOrWhiteSpace(chatSessionKey))
                    throw new ArgumentException("ChatSessionKey is required (current Chat).");

                try { JToken.Parse(wizardJson); }
                catch (Exception ex)
                {
                    throw new ArgumentException("wizardJson must be valid JSON: " + ex.Message);
                }

                var fixture = GetTenantFixture();
                UpsertChatSharedContext(fixture, chatSessionKey.Trim(), WizardSharedContextKey, wizardJson);

                string cursor = currentStepCode;
                if (string.IsNullOrWhiteSpace(cursor))
                {
                    try { cursor = JObject.Parse(wizardJson).Value<string>("cursor"); }
                    catch { /* ignore */ }
                }

                int? jobSessionId = null;
                try
                {
                    int companyId = ResolveCompanyId(targetCompanyId);
                    var job = ResolveImportSession(fixture, companyId, sessionId, chatSessionKey);
                    jobSessionId = job?.SessionId;
                    if (job?.SessionId != null && job.SessionId.Value > 0 && !string.IsNullOrWhiteSpace(cursor))
                    {
                        fixture.ExecuteNonQueryResult(@"
UPDATE dbo.AppPlmImportSession SET UpdatedAt = @UpdatedAt, CurrentStepCode = @CurrentStepCode
WHERE SessionId = @SessionId AND CompanyId = @CompanyId AND SessionStatus = @Status",
                            new List<DbParameter>
                            {
                                CreateParam(fixture, "@UpdatedAt", DateTime.UtcNow),
                                CreateParam(fixture, "@CurrentStepCode", cursor.Trim()),
                                CreateParam(fixture, "@SessionId", job.SessionId.Value),
                                CreateParam(fixture, "@CompanyId", companyId),
                                CreateParam(fixture, "@Status", SessionStatusInProgress)
                            });
                    }
                }
                catch { /* Job table is PLM-only; wizard persist must not depend on it. */ }

                result.Object = new
                {
                    ok = true,
                    sessionId = jobSessionId,
                    chatSessionKey = chatSessionKey.Trim(),
                    currentStepCode = cursor,
                    wizardPersisted = true,
                    scope = "chat"
                };
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmImportSessionDto), "Plm_Wizard_UpdateFailed", ValidationItemType.Error, ex.Message));
            }
            return result;
        }

        /// <summary>
        /// Read wizard for this Chat from AppAgentSharedContext (ScopeId=ChatSessionKey).
        /// Legacy fallback: AppPlmImportSession.StepStateJson.agentWizardJson if Chat scope is empty.
        /// </summary>
        public static OperationCallResult<object> GetWizardProgress(
            int? sessionId,
            int? targetCompanyId,
            string chatSessionKey = null)
        {
            var result = new OperationCallResult<object>();
            try
            {
                RequirePlmMigrationAdmin();
                if (string.IsNullOrWhiteSpace(chatSessionKey))
                {
                    result.Object = new { ok = true, found = false, sessionId = (int?)null, wizardJson = (string)null };
                    return result;
                }

                var fixture = GetTenantFixture();
                string wizardJson = ReadChatSharedContext(fixture, chatSessionKey.Trim(), WizardSharedContextKey);

                int? jobSessionId = null;
                int? saasApplicationId = null;
                string sessionStatus = null;
                string cursor = null;
                try
                {
                    int companyId = ResolveCompanyId(targetCompanyId);
                    var job = ResolveImportSession(fixture, companyId, sessionId, chatSessionKey);
                    jobSessionId = job?.SessionId;
                    saasApplicationId = job?.SaasApplicationId;
                    sessionStatus = job?.SessionStatus;
                    cursor = job?.CurrentStepCode;
                    if (string.IsNullOrWhiteSpace(wizardJson) && job != null)
                    {
                        wizardJson = ExtractAgentWizardJson(job.StepStateJson);
                        if (!string.IsNullOrWhiteSpace(wizardJson))
                            UpsertChatSharedContext(fixture, chatSessionKey.Trim(), WizardSharedContextKey, wizardJson);
                    }
                }
                catch { /* Job table is PLM-only. */ }

                if (string.IsNullOrWhiteSpace(cursor) && !string.IsNullOrWhiteSpace(wizardJson))
                {
                    try { cursor = JObject.Parse(wizardJson).Value<string>("cursor"); }
                    catch { /* ignore */ }
                }

                result.Object = new
                {
                    ok = true,
                    found = !string.IsNullOrWhiteSpace(wizardJson),
                    sessionId = jobSessionId,
                    chatSessionKey = chatSessionKey.Trim(),
                    saasApplicationId,
                    currentStepCode = cursor,
                    sessionStatus,
                    wizardJson,
                    scope = "chat"
                };
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmImportSessionDto), "Plm_Wizard_GetFailed", ValidationItemType.Error, ex.Message));
            }
            return result;
        }

        private static string ReadChatSharedContext(DatabaseFixture fixture, string chatSessionKey, string contextKey)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey) || string.IsNullOrWhiteSpace(contextKey))
                return null;
            var dt = fixture.RetriveDataTable(
                "SELECT DataJson FROM dbo.AppAgentSharedContext WHERE ScopeId=@S AND ContextKey=@K",
                new List<DbParameter>
                {
                    CreateParam(fixture, "@S", chatSessionKey),
                    CreateParam(fixture, "@K", contextKey)
                });
            if (dt == null || dt.Rows.Count == 0) return null;
            return dt.Rows[0]["DataJson"]?.ToString();
        }

        private static void UpsertChatSharedContext(
            DatabaseFixture fixture, string chatSessionKey, string contextKey, string dataJson)
        {
            fixture.ExecuteNonQueryResult(@"
IF EXISTS (SELECT 1 FROM dbo.AppAgentSharedContext WHERE ScopeId=@S AND ContextKey=@K)
    UPDATE dbo.AppAgentSharedContext SET DataJson=@J, UpdatedAt=GETUTCDATE() WHERE ScopeId=@S AND ContextKey=@K
ELSE
    INSERT INTO dbo.AppAgentSharedContext (ScopeId, ContextKey, DataJson, UpdatedAt)
    VALUES (@S, @K, @J, GETUTCDATE())",
                new List<DbParameter>
                {
                    CreateParam(fixture, "@S", chatSessionKey),
                    CreateParam(fixture, "@K", contextKey),
                    CreateParam(fixture, "@J", dataJson ?? "")
                });
        }

        private static void DeleteChatSharedContext(DatabaseFixture fixture, string chatSessionKey, string contextKey)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey) || string.IsNullOrWhiteSpace(contextKey))
                return;
            fixture.ExecuteNonQueryResult(
                "DELETE FROM dbo.AppAgentSharedContext WHERE ScopeId=@S AND ContextKey=@K",
                new List<DbParameter>
                {
                    CreateParam(fixture, "@S", chatSessionKey),
                    CreateParam(fixture, "@K", contextKey)
                });
        }

        internal static string ExtractAgentWizardJson(string stepStateJson)
        {
            if (string.IsNullOrWhiteSpace(stepStateJson)) return null;
            try
            {
                var state = JsonConvert.DeserializeObject<PlmImportStepStateJson>(stepStateJson);
                return string.IsNullOrWhiteSpace(state?.agentWizardJson) ? null : state.agentWizardJson;
            }
            catch
            {
                return null;
            }
        }

        internal static string MergeAgentWizardIntoStepState(string stepStateJson, string wizardJson)
        {
            PlmImportStepStateJson state;
            try
            {
                state = string.IsNullOrWhiteSpace(stepStateJson)
                    ? new PlmImportStepStateJson()
                    : JsonConvert.DeserializeObject<PlmImportStepStateJson>(stepStateJson) ?? new PlmImportStepStateJson();
            }
            catch
            {
                state = new PlmImportStepStateJson();
            }

            state.agentWizardJson = wizardJson;
            return JsonConvert.SerializeObject(state);
        }

        internal static PlmTemplateImportSettingDto LoadTemplateImportSetting(string stepStateJson)
        {
            if (string.IsNullOrWhiteSpace(stepStateJson))
                return null;

            try
            {
                var state = JsonConvert.DeserializeObject<PlmImportStepStateJson>(stepStateJson);
                if (state == null || string.IsNullOrWhiteSpace(state.templateImportSettingJson))
                    return null;

                return JsonConvert.DeserializeObject<PlmTemplateImportSettingDto>(state.templateImportSettingJson);
            }
            catch
            {
                return null;
            }
        }

        internal static string MergeTemplateImportSettingIntoStepState(string stepStateJson, PlmTemplateImportSettingDto setting)
        {
            PlmImportStepStateJson state;
            try
            {
                state = string.IsNullOrWhiteSpace(stepStateJson)
                    ? new PlmImportStepStateJson()
                    : JsonConvert.DeserializeObject<PlmImportStepStateJson>(stepStateJson) ?? new PlmImportStepStateJson();
            }
            catch
            {
                state = new PlmImportStepStateJson();
            }

            state.templateImportSettingJson = setting == null
                ? null
                : JsonConvert.SerializeObject(setting);
            return JsonConvert.SerializeObject(state);
        }

        internal static void PersistTemplateImportSetting(DatabaseFixture fixture, int sessionId, int companyId, PlmTemplateImportSettingDto setting)
        {
            var session = LoadSessionById(fixture, sessionId, includeConnection: false);
            if (session == null)
                throw new InvalidOperationException("Import session not found.");

            string merged = MergeTemplateImportSettingIntoStepState(session.StepStateJson, setting);
            var pId = fixture.CreateParameter("@SessionId");
            pId.Value = sessionId;
            fixture.ExecuteNonQueryResult(@"
UPDATE dbo.AppPlmImportSession SET
    UpdatedAt = @UpdatedAt,
    StepStateJson = @StepStateJson
WHERE SessionId = @SessionId AND CompanyId = @CompanyId AND SessionStatus = @Status",
                new List<DbParameter>
                {
                    CreateParam(fixture, "@UpdatedAt", DateTime.UtcNow),
                    CreateParam(fixture, "@StepStateJson", merged),
                    pId,
                    CreateParam(fixture, "@CompanyId", companyId),
                    CreateParam(fixture, "@Status", SessionStatusInProgress)
                });
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
                settings.EntityWideTablePrefix = ResolveEntityWideTablePrefix(settings.TablePrefix);
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

        internal static string ResolveEntityWideTablePrefix(string tablePrefix)
        {
            return SanitizeImportTablePrefix(tablePrefix, DefaultTablePrefix) + EntityWideTableSuffix;
        }

        internal static string ResolveSystemDefineTargetTableName(string sourceTableName, string tablePrefix)
        {
            if (string.IsNullOrWhiteSpace(sourceTableName))
                return null;

            string prefix = SanitizeImportTablePrefix(tablePrefix, DefaultTablePrefix);
            string source = sourceTableName.Trim();
            string target = source.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? source
                : prefix + source;
            return target.Length <= 100 ? target : target.Substring(0, 100);
        }

        #endregion

        #region Mapping helpers

        private static PlmImportSessionDto ResolveImportSession(
            DatabaseFixture fixture,
            int companyId,
            int? sessionId,
            string chatSessionKey)
        {
            if (sessionId.HasValue && sessionId.Value > 0)
            {
                var byId = LoadSessionById(fixture, sessionId.Value, includeConnection: false);
                if (byId != null) return byId;
            }
            return LoadInProgressByChat(fixture, companyId, chatSessionKey);
        }

        private static PlmImportSessionDto LoadInProgressByChat(
            DatabaseFixture fixture,
            int companyId,
            string chatSessionKey)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey)) return null;

            var dt = fixture.RetriveDataTable(@"
SELECT TOP 1 SessionId, SessionGuid, CompanyId, SaasApplicationId, CreatedByUserId,
       CreatedAt, UpdatedAt, SessionStatus, CurrentStepCode, StepStateJson, DataSourceDiscoveryJson,
       PlmConnectionEncrypted, ChatSessionKey,
       PlmDataSourceRegisterId, PlmDwDataSourceRegisterId, ErpDataSourceRegisterId, PlmExDbDataSourceRegisterId,
       CASE WHEN PlmDataSourceRegisterId IS NOT NULL AND PlmDataSourceRegisterId > 0 THEN 1
            WHEN PlmConnectionEncrypted IS NULL OR LEN(PlmConnectionEncrypted)=0 THEN 0 ELSE 1 END AS HasPlmConnection
FROM dbo.AppPlmImportSession
WHERE CompanyId = @CompanyId AND ChatSessionKey = @ChatSessionKey AND SessionStatus = @Status
ORDER BY UpdatedAt DESC",
                new List<DbParameter>
                {
                    CreateParam(fixture, "@CompanyId", companyId),
                    CreateParam(fixture, "@ChatSessionKey", chatSessionKey.Trim()),
                    CreateParam(fixture, "@Status", SessionStatusInProgress)
                });
            if (dt == null || dt.Rows.Count == 0) return null;
            return MapSessionRow(dt.Rows[0], includeConnection: false);
        }

        private static PlmImportSessionDto LoadSessionById(DatabaseFixture fixture, int sessionId, bool includeConnection)
        {
            var p = fixture.CreateParameter("@SessionId");
            p.Value = sessionId;
            var dt = fixture.RetriveDataTable(@"
SELECT SessionId, SessionGuid, CompanyId, SaasApplicationId, CreatedByUserId,
       CreatedAt, UpdatedAt, SessionStatus, CurrentStepCode, StepStateJson, DataSourceDiscoveryJson,
       PlmConnectionEncrypted, ChatSessionKey,
       PlmDataSourceRegisterId, PlmDwDataSourceRegisterId, ErpDataSourceRegisterId, PlmExDbDataSourceRegisterId,
       CASE WHEN PlmDataSourceRegisterId IS NOT NULL AND PlmDataSourceRegisterId > 0 THEN 1
            WHEN PlmConnectionEncrypted IS NULL OR LEN(PlmConnectionEncrypted)=0 THEN 0 ELSE 1 END AS HasPlmConnection
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
                ChatSessionKey = row.Table.Columns.Contains("ChatSessionKey") && row["ChatSessionKey"] != DBNull.Value
                    ? row["ChatSessionKey"] as string
                    : null,
                StepStateJson = row["StepStateJson"] as string,
                DataSourceDiscoveryJson = row["DataSourceDiscoveryJson"] as string,
                HasPlmConnection = Convert.ToInt32(row["HasPlmConnection"]) == 1,
                PlmConnectionString = null
            };

            if (row.Table.Columns.Contains("PlmDataSourceRegisterId") && row["PlmDataSourceRegisterId"] != DBNull.Value)
                dto.PlmDataSourceRegisterId = Convert.ToInt32(row["PlmDataSourceRegisterId"]);
            if (row.Table.Columns.Contains("PlmDwDataSourceRegisterId") && row["PlmDwDataSourceRegisterId"] != DBNull.Value)
                dto.PlmDwDataSourceRegisterId = Convert.ToInt32(row["PlmDwDataSourceRegisterId"]);
            if (row.Table.Columns.Contains("ErpDataSourceRegisterId") && row["ErpDataSourceRegisterId"] != DBNull.Value)
                dto.ErpDataSourceRegisterId = Convert.ToInt32(row["ErpDataSourceRegisterId"]);
            if (row.Table.Columns.Contains("PlmExDbDataSourceRegisterId") && row["PlmExDbDataSourceRegisterId"] != DBNull.Value)
                dto.PlmExDbDataSourceRegisterId = Convert.ToInt32(row["PlmExDbDataSourceRegisterId"]);

            // Engine-only hydration: never returned by GetActive/Save (those use includeConnection:false).
            if (includeConnection)
            {
                try
                {
                    if (dto.PlmDataSourceRegisterId.HasValue && dto.PlmDataSourceRegisterId.Value > 0)
                    {
                        dto.PlmConnectionString = ResolveConnectionStringFromRegisterId(dto.PlmDataSourceRegisterId.Value);
                        dto.HasPlmConnection = true;
                    }
                    else if (row.Table.Columns.Contains("PlmConnectionEncrypted"))
                    {
                        var enc = row["PlmConnectionEncrypted"] as string;
                        if (!string.IsNullOrWhiteSpace(enc))
                        {
                            try { dto.PlmConnectionString = AppConnectionStringEncryptionBL.Decrypt(enc); }
                            catch { dto.HasPlmConnection = true; }
                        }
                    }
                }
                catch
                {
                    dto.PlmConnectionString = null;
                    throw;
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
