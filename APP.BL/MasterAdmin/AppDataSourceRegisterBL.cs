using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
//using APP.Persistence.Common;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using APP.Framework;
using System.Net;
using DatabaseSchemaMrg.DataSchema;
using DatabaseSchemaMrg;
using System.Data.Common;

namespace App.BL
{

    // 
    public static class AppDataSourceRegisterBL
    {

        // Resolved once at runtime from the "AppMasterDBConnectionString" in web.config.
        // Finds the AppDataSourceRegister row whose ConnectionString matches that web.config value.
        // Falls back to int.MaxValue (legacy hardcoded sentinel) if no match is found.
        private static int? _masterDataSourceRegisterId;
        private static readonly object _masterIdLock = new object();

        public static int MasterDataSourceRegisterId
        {
            get
            {
                if (_masterDataSourceRegisterId.HasValue)
                    return _masterDataSourceRegisterId.Value;

                lock (_masterIdLock)
                {
                    if (_masterDataSourceRegisterId.HasValue)
                        return _masterDataSourceRegisterId.Value;

                    try
                    {
                        var connStr = AppConfig.GetConnectionString("AppMasterDBConnectionString");

                        if (!string.IsNullOrWhiteSpace(connStr))
                        {
                            using (var adapter = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
                            {
                                var list = new EntityCollection<AppDataSourceRegisterEntity>();
                                adapter.FetchEntityCollection(list, null);

                                // Compare after decryption to support both encrypted and legacy rows
                                var match = list.FirstOrDefault(r =>
                                    string.Equals(
                                        AppConnectionStringEncryptionBL.Decrypt(r.ConnectionString),
                                        connStr,
                                        StringComparison.OrdinalIgnoreCase));
                                if (match != null)
                                {
                                    _masterDataSourceRegisterId = match.DataSourceId;
                                    return _masterDataSourceRegisterId.Value;
                                }
                            }
                        }
                    }
                    catch { }

                    // Fallback to legacy sentinel value
                    _masterDataSourceRegisterId = int.MaxValue;
                    return _masterDataSourceRegisterId.Value;
                }
            }
        }

        public static int? GetDefaultDataSourceRegId()
        {
            return (ServerContext.Instance != null) ? ServerContext.Instance.DataSourceId as int? : null;
        }

        public static AppDataSourceRegisterEntity RetrievAppDataSourceRegisterEntityByCompanyId(int? companyId)
        {
            if (!companyId.HasValue)
            {
                return null;
            }

            EntityCollection<AppDataSourceRegisterEntity> list = new EntityCollection<AppDataSourceRegisterEntity>();

            using (DataAccessAdapter adapter = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
            {
                IRelationPredicateBucket filter = new RelationPredicateBucket(AppDataSourceRegisterFields.DataSourceOwnerCompanyId == companyId.Value);
                adapter.FetchEntityCollection(list, filter);
            }

            return list.FirstOrDefault();
        }

        public static EntityCollection<AppDataSourceRegisterEntity> RetrieveAllAppDataSourceRegisterEntity(int? ownerCompanyId = null)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
            {
                EntityCollection<AppDataSourceRegisterEntity> list = new EntityCollection<AppDataSourceRegisterEntity>();
                SortClause aSortClause = AppDataSourceRegisterFields.DataSourceName | SortOperator.Ascending;

                IRelationPredicateBucket filter = null;
                if (ownerCompanyId.HasValue)
                {
                    filter = new RelationPredicateBucket(
                        AppDataSourceRegisterFields.DataSourceOwnerCompanyId == ownerCompanyId.Value);
                }

                adapter.FetchEntityCollection(list, filter, 0, new SortExpression(aSortClause), null);

                return list;
            }
        }

        //alwsy from master DB
        public static AppDataSourceRegisterEntity RetrieveOneAppDataSourceRegisterEntity(object Id)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
            {
                AppDataSourceRegisterEntity aAppDataSourceRegisterEntity = new AppDataSourceRegisterEntity(int.Parse(Id.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppDataSourceRegisterEntity);




                adapter.FetchEntity(aAppDataSourceRegisterEntity, rootPath);
                return aAppDataSourceRegisterEntity;
            }
        }

        private static bool IsSysAdminUser()
        {
            return ServerContext.Instance?.CurrentLoginUserType == (int)EmAppUserType.SysAdmin;
        }

        private static int? GetCurrentUserCompanyId()
        {
            return ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance?.CurrentCompanyId);
        }

        private static ValidationResult ValidateTenantDataSourceAccess(int? dataSourceOwnerCompanyId, string dataSourceName = null)
        {
            var result = new ValidationResult();
            if (IsSysAdminUser())
                return result;

            int? companyId = GetCurrentUserCompanyId();
            if (!companyId.HasValue)
            {
                result.Items.Add(new ValidationItem(typeof(AppDataSourceRegisterExDto), "App_AppDataSourceRegisterEntity_Forbidden", ValidationItemType.Error, "Company context is required."));
                return result;
            }

            if (dataSourceOwnerCompanyId.HasValue && dataSourceOwnerCompanyId != companyId)
            {
                var label = string.IsNullOrWhiteSpace(dataSourceName) ? "Data source" : dataSourceName;
                result.Items.Add(new ValidationItem(typeof(AppDataSourceRegisterExDto), "App_AppDataSourceRegisterEntity_Forbidden", ValidationItemType.Error, label + " does not belong to the current company."));
            }

            return result;
        }

        // need to use lookup item to show DLL list value
        // each customer only can acess his own database
        public static List<AppDataSourceRegisterExDto> GetDataSourceRegisterList()
        {
            List<AppDataSourceRegisterExDto> toReturn = RetrieveAllAppDataSourceRegisterExDto().ToList();

            foreach (var o in toReturn)
            {
                o.ConnectionString = "";
                o.DatabaseName = "";
            }

            if (!IsSysAdminUser())
            {
                toReturn = toReturn.OrderBy(o => o.Id).ToList();
            }

            return toReturn;
        }

        private static int? GetTenantScopeCompanyId()
        {
            return IsSysAdminUser() ? null : GetCurrentUserCompanyId();
        }

        public static ObservableSet<AppDataSourceRegisterExDto> RetrieveAllAppDataSourceRegisterExDto()
        {
            ObservableSet<AppDataSourceRegisterExDto> aSet = new ObservableSet<AppDataSourceRegisterExDto>();
            EntityCollection<AppDataSourceRegisterEntity> list = RetrieveAllAppDataSourceRegisterEntity(GetTenantScopeCompanyId());

            foreach (var o in list)
            {
                AppDataSourceRegisterExDto aDto = AppDataSourceRegisterConverter.ConvertEntityToExDto(o);
                aSet.Add(aDto);
            }

            return aSet;
        }

        public static AppDataSourceRegisterExDto RetrieveOneAppDataSourceRegisterExDto(object Id)
        {
            AppDataSourceRegisterEntity aAppDataSourceRegisterEntity = RetrieveOneAppDataSourceRegisterEntity(Id);
            AppDataSourceRegisterExDto aAppDataSourceRegisterExDto = AppDataSourceRegisterConverter.ConvertEntityToExDto(aAppDataSourceRegisterEntity);



            return aAppDataSourceRegisterExDto;
        }

        private static void ScopeDataSourceRegisterSetForCurrentTenant(ObservableSet<AppDataSourceRegisterExDto> aSet)
        {
            if (IsSysAdminUser() || aSet == null)
                return;

            int? companyId = GetCurrentUserCompanyId();
            if (!companyId.HasValue)
                return;

            foreach (var foreignItem in aSet.Where(o => o.DataSourceOwnerCompanyId.HasValue && o.DataSourceOwnerCompanyId != companyId).ToList())
            {
                aSet.Remove(foreignItem);
            }

            foreach (var item in aSet)
            {
                item.DataSourceOwnerCompanyId = companyId;
            }

            var scopedDeleteIds = aSet.FindDeletedItemIds()
                .Where(id =>
                {
                    var existingEntity = RetrieveOneAppDataSourceRegisterEntity(id);
                    return existingEntity.DataSourceOwnerCompanyId == companyId;
                })
                .ToList();

            aSet.DeletedItemIds = scopedDeleteIds;
        }

        public static OperationCallResult<AppDataSourceRegisterExDto> SaveAllAppDataSourceRegisterExDto(ObservableSet<AppDataSourceRegisterExDto> aSet)
        {
            OperationCallResult<AppDataSourceRegisterExDto> aOperationCallResult = new OperationCallResult<AppDataSourceRegisterExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            if (!IsSysAdminUser())
            {
                int? companyId = GetCurrentUserCompanyId();
                if (!companyId.HasValue)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppDataSourceRegisterExDto), "App_AppDataSourceRegisterEntity_Forbidden", ValidationItemType.Error, "Company context is required."));
                    return aOperationCallResult;
                }
            }

            ScopeDataSourceRegisterSetForCurrentTenant(aSet);

            List<AppDataSourceRegisterExDto> needToValidateDatabaseRegisterDto = aSet.Where(o => o.DataSourceType.HasValue && o.DataSourceType.Value == (int)EmAppDataServerType.SqlServer).ToList();

            foreach (var aRegisterDto in needToValidateDatabaseRegisterDto)
            {
                if (string.IsNullOrWhiteSpace(aRegisterDto.ConnectionString))
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppDataSourceRegisterExDto), "App_AppDataSourceRegisterExDto_connectionstringisempty", ValidationItemType.Error, "ConnectionString is invalid on " + aRegisterDto.DataSourceName));
                    return aOperationCallResult;
                }
                else
                {
                    try
                    {
                        string plainConn = AppConnectionStringEncryptionBL.Decrypt(aRegisterDto.ConnectionString);
                        using (SqlConnection conn = new SqlConnection(plainConn))
                        {
                            conn.Open();
                        }
                    }
                    catch
                    {
                        validationResult.Items.Add(new ValidationItem(typeof(AppDataSourceRegisterExDto), "plm_PdmDataSourceEntity_Connection_Error", ValidationItemType.Error, "ConnectionString is invalid on " + aRegisterDto.DataSourceName));
                        return aOperationCallResult;
                    }
                }
            }

            foreach (var newItemDto in aSet.FindNewItems())
            {
                var result = ProcessNewDto(newItemDto);
                validationResult.Merge(result);
            }

            aSet.FindModifiedItems().Where(o => o.IsNew == false).ForAll(o => validationResult.Merge(ProcessDirtyDto(o)));
            aSet.FindDeletedItemIds().ForAll(Id => validationResult.Merge(DeleteOneAppDataSourceRegisterEntityDto(Id).ValidationResult));

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppDataSourceRegisterExDto), "App_AppDataSourceRegisterEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                aOperationCallResult.ObjectList = RetrieveAllAppDataSourceRegisterExDto();
                // no need to refresh  all cache , dot change connection info unless changed 
                // DB load on demand
                //AppCacheManagerBL.RefreshAllCustomerDbRegAndFixtureCache();
                //

            }

            return aOperationCallResult;
        }

        public static OperationCallResult<AppDataSourceRegisterExDto> SaveOneAppDataSourceRegisterExDto(AppDataSourceRegisterExDto aAppDataSourceRegisterExDto)
        {
            OperationCallResult<AppDataSourceRegisterExDto> aOperationCallResult = new OperationCallResult<AppDataSourceRegisterExDto>();

            var aValidationResult = aAppDataSourceRegisterExDto.ValidateDto();
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }


            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;







            if (aAppDataSourceRegisterExDto.IsNew)
            {
                validationResult.Merge(ProcessNewDto(aAppDataSourceRegisterExDto));
            }
            else if (aAppDataSourceRegisterExDto.IsModified)
            {
                validationResult.Merge(ProcessDirtyDto(aAppDataSourceRegisterExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                // var entity = AppDataSourceRegisterBL.RetrieveOneAppDataSourceRegisterEntity(aAppDataSourceRegisterExDto.Id);
                aOperationCallResult.Object = RetrieveOneAppDataSourceRegisterExDto(aAppDataSourceRegisterExDto.Id);
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<object> DeleteOneAppDataSourceRegisterEntityDto(object Id)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            var existingEntity = RetrieveOneAppDataSourceRegisterEntity(Id);
            aValidationResult.Merge(ValidateTenantDataSourceAccess(existingEntity.DataSourceOwnerCompanyId, existingEntity.DataSourceName));
            if (aValidationResult.HasErrors)
                return aOperationCallResult;

            using (DataAccessAdapter adapter = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppDataSourceRegisterEntity");
                    adapter.DeleteEntitiesDirectly(typeof(AppDataSourceRegisterEntity), new RelationPredicateBucket(AppDataSourceRegisterFields.DataSourceId == Id));
                    adapter.Commit();
                }


                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_AppDataSourceRegisterEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    adapter.Rollback();
                }
            }

            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = true;
            }

            return aOperationCallResult;
        }



        private static ValidationResult ProcessNewDto(AppDataSourceRegisterExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            if (!IsSysAdminUser())
            {
                int? companyId = GetCurrentUserCompanyId();
                aValidationResult.Merge(ValidateTenantDataSourceAccess(aDto.DataSourceOwnerCompanyId, aDto.DataSourceName));
                if (aValidationResult.HasErrors)
                    return aValidationResult;

                aDto.DataSourceOwnerCompanyId = companyId;
            }
            else if (!aDto.DataSourceOwnerCompanyId.HasValue)
            {
                aDto.DataSourceOwnerCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
            }

            // Encrypt the connection string before persisting
            if (!string.IsNullOrEmpty(aDto.ConnectionString))
                aDto.ConnectionString = AppConnectionStringEncryptionBL.Encrypt(aDto.ConnectionString);

            AppDataSourceRegisterEntity aParentAppDataSourceRegisterEntity = new AppDataSourceRegisterEntity();
            AppDataSourceRegisterConverter.CopyDtoToEntity(aParentAppDataSourceRegisterEntity, aDto);




            using (DataAccessAdapter adapter = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aParentAppDataSourceRegisterEntity);

                    adapter.Commit();
                    aDto.Id = aParentAppDataSourceRegisterEntity.DataSourceId;

                    if (!(aDto.IsCompanyMasterDb.HasValue && aDto.IsCompanyMasterDb.Value))
                    { 
                        aValidationResult.Merge(ExecuteStructureUpdateScript(aDto));
                    }
                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSourceRegisterExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }



            return aValidationResult;
        }

        private static ValidationResult ProcessDirtyDto(AppDataSourceRegisterExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppDataSourceRegisterEntity aAppDataSourceRegisterEntity = RetrieveOneAppDataSourceRegisterEntity(aDto.Id);

            if (!IsSysAdminUser())
            {
                int? companyId = GetCurrentUserCompanyId();
                aValidationResult.Merge(ValidateTenantDataSourceAccess(aAppDataSourceRegisterEntity.DataSourceOwnerCompanyId, aDto.DataSourceName));
                if (aValidationResult.HasErrors)
                    return aValidationResult;

                aDto.DataSourceOwnerCompanyId = companyId;
            }

            // Encrypt the connection string before persisting
            if (!string.IsNullOrEmpty(aDto.ConnectionString))
                aDto.ConnectionString = AppConnectionStringEncryptionBL.Encrypt(aDto.ConnectionString);

            AppDataSourceRegisterConverter.CopyDtoToEntity(aAppDataSourceRegisterEntity, aDto);



            using (DataAccessAdapter adapter = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppDataSourceRegisterEntity, false, true);

                    adapter.Commit();

                    if (!(aDto.IsCompanyMasterDb.HasValue && aDto.IsCompanyMasterDb.Value))
                    {
                        aValidationResult.Merge(ExecuteStructureUpdateScript(aDto));
                    }
                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSourceRegisterExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }



            return aValidationResult;
        }

        private static ValidationResult ExecuteStructureUpdateScript(AppDataSourceRegisterExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();


            if (aDto.DataSourceType.HasValue && !string.IsNullOrWhiteSpace(aDto.ConnectionString) && !(aDto.IsCompanyMasterDb.HasValue && aDto.IsCompanyMasterDb.Value))
            {
                string query = "";

                if (aDto.DataSourceType.Value == (int)EmAppDataServerType.SqlServer)
                {
                    query = @"
                            IF  NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AppTransactionUnitExtendFieldValue]') AND type in (N'U'))
                            BEGIN
	                            CREATE TABLE [dbo].[AppTransactionUnitExtendFieldValue](
		                            [UnitExtendFieldValueID] [int] IDENTITY(1,1) NOT NULL,
		                            [TransactionUnitID] [int] NULL,
		                            [UnitExtendFiledID] [int] NULL,
		                            [UnitPKValue] [nvarchar](100) NULL,
		                            [ValueText] [nvarchar](max) NULL,
	                             CONSTRAINT [PK_AppTransactionUnitExtendFieldValue] PRIMARY KEY CLUSTERED 
	                            (
		                            [UnitExtendFieldValueID] ASC
	                            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	                            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]	                           
                            END
                        ";
                }
                else if (aDto.DataSourceType.Value == (int)EmAppDataServerType.MySql)
                {
                    query = @" 
                            CREATE TABLE IF NOT EXISTS `AppTransactionUnitExtendFieldValue` (
                                `UnitExtendFieldValueID` INT AUTO_INCREMENT PRIMARY KEY,
                                `TransactionUnitID` INT,
                                `UnitExtendFiledID` INT,
                                `UnitPKValue` VARCHAR(100),
                                `ValueText` TEXT
                            ) ENGINE=InnoDB;
                        ";
                }

                if (!string.IsNullOrWhiteSpace(query))
                {
                    try
                    {
                        string plainConn = AppConnectionStringEncryptionBL.Decrypt(aDto.ConnectionString);
                        var dbFixtureInstance = new DatabaseFixture(plainConn, (EmSqlType)aDto.DataSourceType);
                        dbFixtureInstance.ExecuteNonQueryResult(query, new List<DbParameter>());
                    }
                    catch (WebException ex)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSourceRegisterExDto), "ExecuteStructureUpdateScript_Error", ValidationItemType.Warning,
                            ex.Message));
                    }

                }
            }

            return aValidationResult;
        }


        // Resolves tenant branding info from the HTTP Host header.
        // Matches on DomainToken (subdomain) or CustomDomain (exact host).
        // Uses raw ADO.NET so no LLBLGen entity regeneration is required for the new columns.
        public static AppTenantInfoDto GetTenantInfoByHost(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
                return new AppTenantInfoDto { IsFound = false };

            var hostOnly = host.Split(':')[0].ToLowerInvariant();
            var parts = hostOnly.Split('.');
            var subdomainToken = parts.Length >= 3 ? parts[0] : null;

            const string sql = @"
                SELECT TOP 1 c.ShortName, ds.DomainToken, ds.CustomDomain
                FROM   AppDataSourceRegister ds
                INNER JOIN AppCompany c ON c.AppCompanyId = ds.DataSourceOwnerCompanyId
                WHERE  (ds.DomainToken IS NOT NULL AND LOWER(ds.DomainToken) = @Token)
                    OR (ds.CustomDomain IS NOT NULL AND LOWER(ds.CustomDomain) = @Host)";

            var masterConnStr = AppConfig.GetConnectionString("AppMasterDBConnectionString");

            if (string.IsNullOrEmpty(masterConnStr))
                return new AppTenantInfoDto { IsFound = false };

            try
            {
                using (var conn = new SqlConnection(masterConnStr))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Token", (object)subdomainToken ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Host", hostOnly);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new AppTenantInfoDto
                                {
                                    IsFound = true,
                                    CompanyName = reader["ShortName"] as string,
                                    DomainToken = reader["DomainToken"] as string,
                                    CustomDomain = reader["CustomDomain"] as string,
                                };
                            }
                        }
                    }
                }
            }
            catch { }

            return new AppTenantInfoDto { IsFound = false };
        }
    }

}