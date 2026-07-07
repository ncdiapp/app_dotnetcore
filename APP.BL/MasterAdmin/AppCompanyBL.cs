using System.Collections.Generic;
using System.Linq;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Components.EntityConverter;
using APP.Framework;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL;
using System;
using APP.Framework.Collections;
using System.Data;
//using APP.Persistence.Common;
using System.Data.SqlClient;
using System.IO;



namespace App.BL
{
    public static class AppCompanyBL
    {
        // public const string APPConnectionConfigName = "AppMasterDBConnectionString";

        public static readonly string AppMasterDBConnectionString = AppConfig.GetConnectionString("AppMasterDBConnectionString") ?? string.Empty;
        public static readonly string HostCompanyDbName = new SqlConnectionStringBuilder(AppMasterDBConnectionString).InitialCatalog;
        public static AppDataSourceRegisterEntity GetCompnayDataSourceRegisterEntityFromCompanyDomainIdentityToken(string companyDomainIdentityToken)
        {

            EntityCollection<AppDataSourceRegisterEntity> list = new EntityCollection<AppDataSourceRegisterEntity>();

            string query = @" SELECT top 1 AppCompanyID FROM[dbo].[AppCompany]    where CompanyDomainIdentityToken = @CompanyDomainIdentityToken";
            List<SqlParameter> paralist = new List<SqlParameter>();
            paralist.Add(new SqlParameter("@CompanyDomainIdentityToken", companyDomainIdentityToken));

            using (DataAccessAdapter adpater = new DataAccessAdapter(AppMasterDBConnectionString))
            {
                object objId = adpater.ExecuteScalarQuery(query, paralist);

                int? companyId = ControlTypeValueConverter.ConvertValueToInt(objId);

                IRelationPredicateBucket filter = new RelationPredicateBucket(AppDataSourceRegisterFields.DataSourceOwnerCompanyId == companyId.Value);
                adpater.FetchEntityCollection(list, filter);

            }
            return list.FirstOrDefault();
        }


        public static AppCompanyDto GetCustomerDbCompnayData(string userDatabaseName)
        {

            EntityCollection<AppDataSourceRegisterEntity> list = new EntityCollection<AppDataSourceRegisterEntity>();

            string query = @" SELECT top 1 * FROM [" + userDatabaseName + "].[dbo].[AppCompany]  ";
            List<SqlParameter> paralist = new List<SqlParameter>();


            using (DataAccessAdapter adapter = new DataAccessAdapter(AppMasterDBConnectionString))
            {
                DataTable result = adapter.ExecuteDataTableRetrievalQuery(query, paralist);
                if (result.Rows.Count > 0)
                {
                    AppCompanyDto toReturn = new AppCompanyDto();
                    AppCompanyEntity companyEntity = new AppCompanyEntity();

                    DataRow dataRow = result.Rows[0];

                    foreach (DataColumn col in result.Columns)
                    {
                        object value = dataRow[col];

                        if (col.ColumnName.ToLower() == "AppCompanyID".ToLower())
                        {
                            int? companyId = ControlTypeValueConverter.ConvertValueToInt(value);
                            if (companyId.HasValue)
                            {
                                companyEntity.AppCompanyId = companyId.Value;
                            }

                        }
                        else if (col.ColumnName.ToLower() == "Code".ToLower())
                        {
                            companyEntity.Code = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(value);
                        }
                        else if (col.ColumnName.ToLower() == "ShortName".ToLower())
                        {
                            companyEntity.ShortName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(value);
                        }
                        else if (col.ColumnName.ToLower() == "FullName".ToLower())
                        {
                            companyEntity.FullName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(value);
                        }
                        else if (col.ColumnName.ToLower() == "RegistrationNumber".ToLower())
                        {
                            companyEntity.RegistrationNumber = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(value);
                        }
                        else if (col.ColumnName.ToLower() == "ValueAddedTaxID".ToLower())
                        {
                            companyEntity.ValueAddedTaxId = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(value);
                        }
                        else if (col.ColumnName.ToLower() == "Adress1".ToLower())
                        {
                            companyEntity.Adress1 = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(value);
                        }
                        else if (col.ColumnName.ToLower() == "Adress2".ToLower())
                        {
                            companyEntity.Adress2 = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(value);
                        }
                        else if (col.ColumnName.ToLower() == "Adress3".ToLower())
                        {
                            companyEntity.Adress3 = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(value);
                        }
                        else if (col.ColumnName.ToLower() == "City".ToLower())
                        {
                            companyEntity.City = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(value);
                        }
                        else if (col.ColumnName.ToLower() == "Language".ToLower())
                        {
                            companyEntity.Language = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(value);
                        }
                        else if (col.ColumnName.ToLower() == "State".ToLower())
                        {
                            companyEntity.State = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(value);
                        }
                        else if (col.ColumnName.ToLower() == "PostCode".ToLower())
                        {
                            companyEntity.PostCode = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(value);
                        }
                        else if (col.ColumnName.ToLower() == "Country".ToLower())
                        {
                            companyEntity.Country = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(value);
                        }
                        else if (col.ColumnName.ToLower() == "Status".ToLower())
                        {
                            companyEntity.Status = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(value);
                        }
                        else if (col.ColumnName.ToLower() == "CurrencyCode".ToLower())
                        {
                            companyEntity.CurrencyCode = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(value);
                        }
                        else if (col.ColumnName.ToLower() == "ContactPhone".ToLower())
                        {
                            companyEntity.ContactPhone = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(value);
                        }
                        else if (col.ColumnName.ToLower() == "ContactName".ToLower())
                        {
                            companyEntity.ContactName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(value);
                        }
                        else if (col.ColumnName.ToLower() == "ContactFax".ToLower())
                        {
                            companyEntity.ContactFax = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(value);
                        }
                        else if (col.ColumnName.ToLower() == "EmApplicationVersionEdition".ToLower())
                        {
                            companyEntity.EmApplicationVersionEdition = ControlTypeValueConverter.ConvertValueToInt(value);
                        }
                        else if (col.ColumnName.ToLower() == "CompanyDomainIdentityToken".ToLower())
                        {
                            companyEntity.CompanyDomainIdentityToken = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(value);
                        }
                    }

                    toReturn = AppCompanyConverter.ConvertEntityToDto(companyEntity);

                    PrepareCompanyOtherSettingsDto(toReturn);

                    return toReturn;
                }

            }

            return new AppCompanyDto();
        }


        public static void SetServerContextCompanyProperties(AppCompanyDto companyDto)
        {
            if (companyDto.OtherSettingsDto == null)
            {
                companyDto.OtherSettingsDto = new APP.Components.EntityDto.AppCompanyOtherSettingsDto();
            }

            var settingDto = companyDto.OtherSettingsDto;

            ServerContext.Instance.CompanySettings = new CompanySettingDto();

            ServerContext.Instance.CompanySettings.CompanyId = ControlTypeValueConverter.ConvertValueToInt(companyDto.Id);

            if (companyDto.ShortName.HasValue())
            {
                ServerContext.Instance.CompanySettings.CompanyName = companyDto.ShortName;
            }
            else if (companyDto.FullName.HasValue())
            {
                ServerContext.Instance.CompanySettings.CompanyName = companyDto.FullName;
            }
            else if (companyDto.Code.HasValue())
            {
                ServerContext.Instance.CompanySettings.CompanyName = companyDto.Code;
            }


            ServerContext.Instance.CompanySettings.IsEnableClientSelfRegistration = settingDto.IsEnableClientSelfRegistration.HasValue && settingDto.IsEnableClientSelfRegistration.Value;
            ServerContext.Instance.CompanySettings.IsEnableSupplierSelfRegistration = settingDto.IsEnableSupplierSelfRegistration.HasValue && settingDto.IsEnableSupplierSelfRegistration.Value;
            ServerContext.Instance.CompanySettings.IsEnableClientAgentSelfRegistration = settingDto.IsEnableClientAgentSelfRegistration.HasValue && settingDto.IsEnableClientAgentSelfRegistration.Value;
            ServerContext.Instance.CompanySettings.IsEnableSupplierAgentSelfRegistration = settingDto.IsEnableSupplierAgentSelfRegistration.HasValue && settingDto.IsEnableSupplierAgentSelfRegistration.Value;


            ServerContext.Instance.CompanySettings.ClientLabelName = settingDto.ClientLabelName.HasValue() ? settingDto.ClientLabelName : "Client";
            ServerContext.Instance.CompanySettings.SupplierLabelName = settingDto.SupplierLabelName.HasValue() ? settingDto.SupplierLabelName : "Supplier";
            ServerContext.Instance.CompanySettings.ClientAgentLabelName = settingDto.ClientAgentLabelName.HasValue() ? settingDto.ClientAgentLabelName : "Client Agent";
            ServerContext.Instance.CompanySettings.SupplierAgentLabelName = settingDto.SupplierAgentLabelName.HasValue() ? settingDto.SupplierAgentLabelName : "Supplier Agent";


            //ServerContext.Instance.CompanySettings.LogoImageId = settingDto.LogoImageId;
            //ServerContext.Instance.CompanySettings.LoginPageBackgroundImageIdList = settingDto.LoginPageBackgroundImageIdList;
        }


        public static AppCompanyEntity RetrieveOneAppCompanyEntityFromMasterDB(object companyId)
        {
            using (DataAccessAdapter adpater = new DataAccessAdapter(AppMasterDBConnectionString))
            {
                AppCompanyEntity companyEntity = new AppCompanyEntity(int.Parse(companyId.ToString()));
                adpater.FetchEntity(companyEntity);
                return companyEntity;
            }
        }

        public static int? GetCompnayIdFromCompanyDomainIdentityToken(string companyDomainIdentityToken)
        {
            string query = @" SELECT top 1 AppCompanyID FROM [dbo].[AppCompany]    where CompanyDomainIdentityToken = @CompanyDomainIdentityToken";
            List<SqlParameter> paralist = new List<SqlParameter>();
            paralist.Add(new SqlParameter("@CompanyDomainIdentityToken", companyDomainIdentityToken));

            using (DataAccessAdapter adpater = new DataAccessAdapter(AppMasterDBConnectionString))
            {
                object objId = adpater.ExecuteScalarQuery(query, paralist);

                return ControlTypeValueConverter.ConvertValueToInt(objId);

            }
        }




        // Must login 
        public static string GetCompnayDomainIdentityToken(object compmnayId)
        {
            string query = @" SELECT top 1  CompanyDomainIdentityToken  FROM [dbo].[AppCompany]    where AppCompanyID = @AppCompanyID";
            List<SqlParameter> paralist = new List<SqlParameter>();
            paralist.Add(new SqlParameter("@AppCompanyID", compmnayId));

            using (DataAccessAdapter adpater = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
            {
                object objId = adpater.ExecuteScalarQuery(query, paralist);

                return objId.ToString();

            }
        }

        public static UserContext GetAnonymousContext(int? companyId)
        {
            // string Anonymousagent = "anonymousagent";

            UserContext aUserContext = null;
            int? sysagentuserId = AppTenantSettingBL.GetIntValue(EmTenantSettings.SystemAgentUser);

            if (sysagentuserId.HasValue)
            {
                aUserContext = new UserContext();


                aUserContext.UserId = sysagentuserId;
                //aUserContext.SessionId = System.Guid.NewGuid().ToString();

                aUserContext.SessionId = "6601508d-e7e0-4ed6-892b-879c834676af";
                aUserContext.ServerSideCurrentCompnayId = companyId;
                AppSecurityUserSessionBL.CreateNewAppSecurityUserSession(aUserContext);
            }

            return aUserContext;
        }

        public static AppCompanyEntity RetrieveOneAppCompanyEntity(object companyId)
        {
            using (DataAccessAdapter adpater = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
            {
                AppCompanyEntity companyEntity = new AppCompanyEntity(int.Parse(companyId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppCompanyEntity);
                //rootPath.Add(AppCompanyEntity.PrefetchPathAppComOrgLevel);

                adpater.FetchEntity(companyEntity, rootPath);
                return companyEntity;
            }
        }


        public static AppCompanyEntity RetrieveOneSaasCompanyEntity(object companyId)
        {
            using (DataAccessAdapter adpater = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
            {
                AppCompanyEntity companyEntity = new AppCompanyEntity(int.Parse(companyId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppCompanyEntity);
                rootPath.Add(AppCompanyEntity.PrefetchPathAppDataSourceRegister);

                adpater.FetchEntity(companyEntity, rootPath);
                return companyEntity;
            }
        }

        public static AppCompanyExDto RetrieveOneAppCompanyExDto(object companyId)
        {
            AppCompanyEntity companyEntity = RetrieveOneAppCompanyEntity(companyId);
            AppCompanyExDto aAppCompanyExDto = AppCompanyConverter.ConvertEntityToExDto(companyEntity);

            //foreach (AppComOrgLevelEntity appComOrgLevelEntity in companyEntity.AppComOrgLevel.OrderBy(o => o.ClassificationLevel))
            //{
            //    AppComOrgLevelExDto aAppComOrgLevelExDto = AppComOrgLevelConverter.ConvertEntityToExDto(appComOrgLevelEntity);
            //    aAppCompanyExDto.AppComOrgLevelList.Add(aAppComOrgLevelExDto);

            //}

            AppDataSourceRegisterEntity companyDataSource = AppDataSourceRegisterBL.RetrievAppDataSourceRegisterEntityByCompanyId(companyId as int?);

            if (companyDataSource != null)
            {
                if (companyDataSource.DataSourceId != int.MaxValue)
                {
                    aAppCompanyExDto.IsSaasUserCompany = true;
                }
            }

            int? companyIdInt = companyId as int? ?? (int.TryParse(companyId?.ToString(), out int parsed) ? parsed : (int?)null);
            aAppCompanyExDto.LogoImageUrl = GetCurrentCompanyLogoImageUrl(companyIdInt);
            aAppCompanyExDto.BackgroundImageUrlList = GetCurrentCompanyBackgroundImageUrlList(companyIdInt);



            PrepareCompanyOtherSettingsDto(aAppCompanyExDto);

            return aAppCompanyExDto;
        }


        public static AppCompanyExDto RetrieveCurrentUserCompanyExDto()
        {
            int? companyId = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentCompanyId);
            return companyId.HasValue ? RetrieveOneAppCompanyExDto(companyId.Value) : null;
        }


        public static List<AppCompanyDto> RetrieveAllRootCompanyDtoList()
        {
            var folderEntities = new EntityCollection<AppCompanyEntity>();

            using (DataAccessAdapter adapater = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
            {

                IRelationPredicateBucket filter = new RelationPredicateBucket(AppCompanyFields.ParentCompayId == DBNull.Value);

                adapater.FetchEntityCollection(folderEntities, filter);
            }

            var aDtoList = new List<AppCompanyDto>();
            foreach (var folderEntity in folderEntities)
            {
                aDtoList.Add(AppCompanyConverter.ConvertEntityToDto(folderEntity));
            }

            return aDtoList;
        }

        public static List<AppCompanyDto> RetrieveAllSaasCompanyDtoList()
        {
            var entities = new EntityCollection<AppCompanyEntity>();

            using (DataAccessAdapter adapater = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
            {
                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppCompanyEntity);
                rootPath.Add(AppCompanyEntity.PrefetchPathAppDataSourceRegister);

                IRelationPredicateBucket filter = new RelationPredicateBucket(AppCompanyFields.ParentCompayId == DBNull.Value
                    & AppCompanyFields.AppCompanyId != AppCacheManagerBL.HostCompanyReserveId);

                if (ServerContext.Instance?.CurrentLoginUserType != (int)EmAppUserType.SysAdmin)
                {
                    int? companyId = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance?.CurrentCompanyId);
                    if (companyId.HasValue)
                    {
                        filter.PredicateExpression.Add(AppCompanyFields.AppCompanyId == companyId.Value);
                    }
                    else
                    {
                        return new List<AppCompanyDto>();
                    }
                }

                adapater.FetchEntityCollection(entities, filter, rootPath);
            }

            var aDtoList = new List<AppCompanyDto>();
            foreach (var anEntity in entities)
            {
                var companyDto = AppCompanyConverter.ConvertEntityToDto(anEntity);
                companyDto.DataSourceRegisterInfo = new AppDataSourceRegisterDto();

                if (anEntity.AppDataSourceRegister != null && anEntity.AppDataSourceRegister.Count > 0)
                {
                    companyDto.DataSourceRegisterInfo = AppDataSourceRegisterConverter.ConvertEntityToDto(anEntity.AppDataSourceRegister[0]);

                    if (companyDto.DataSourceRegisterInfo.DatabaseName == HostCompanyDbName)
                    {
                        companyDto.IsLinkToHostCompanyDb = true;
                    }
                }



                aDtoList.Add(companyDto);

            }

            return aDtoList;
        }

        public static AppCompanyDto RetrieveOneSaasCompanyDto(int companyId)
        {
            var entity = RetrieveOneSaasCompanyEntity(companyId);
            var companyDto = AppCompanyConverter.ConvertEntityToDto(entity);

            companyDto.DataSourceRegisterInfo = new AppDataSourceRegisterDto();

            if (entity.AppDataSourceRegister != null && entity.AppDataSourceRegister.Count > 0)
            {
                companyDto.DataSourceRegisterInfo = AppDataSourceRegisterConverter.ConvertEntityToDto(entity.AppDataSourceRegister[0]);
            }

            return companyDto;
        }


        internal static List<AppCompanyEntity> RetrieveCompanyEntityListByIdsFromMasterDB(List<int> commpanyIds)
        {
            var entities = new EntityCollection<AppCompanyEntity>();

            IRelationPredicateBucket filter = null;

            if (commpanyIds != null && commpanyIds.Count > 0)
            {
                filter = new RelationPredicateBucket(AppCompanyFields.AppCompanyId == commpanyIds.ToArray());


                using (DataAccessAdapter adapater = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
                {
                    adapater.FetchEntityCollection(entities, filter);
                }
            }

            return entities.ToList();
        }


        public static OperationCallResult<AppCompanyExDto> SaveOneAppCompanyExDto(AppCompanyExDto aAppCompanyExDto)
        {
            OperationCallResult<AppCompanyExDto> aOperationCallResult = new OperationCallResult<AppCompanyExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppCompanyEntity aAppCompanyEntity;

            ConvertCompanyOtherSettingsDtoToDbValue(aAppCompanyExDto);


            //if (aAppCompanyExDto.AppComOrgLevelList.Count == 0)
            //{
            //    AppComOrgLevelExDto defaultLevel = new AppComOrgLevelExDto();
            //    defaultLevel.ClassificationLevel = 1;
            //    defaultLevel.CodeNum = "01";
            //    defaultLevel.LevelName = "Company";
            //    defaultLevel.FullName = "Company";
            //    aAppCompanyExDto.AppComOrgLevelList.Add(defaultLevel);
            //}
            //else
            //{
            //    int level = 1;
            //    foreach (var levelDto in aAppCompanyExDto.AppComOrgLevelList.OrderBy(o => o.ClassificationLevel))
            //    {
            //        levelDto.ClassificationLevel = level;
            //        level++;
            //    }
            //}


            // prepare Data
            if (aAppCompanyExDto.IsNew)
            {
                aAppCompanyEntity = new AppCompanyEntity();
                AppCompanyConverter.CopyDtoToEntity(aAppCompanyEntity, aAppCompanyExDto);



                //foreach (var appComOrgLevelExDto in aAppCompanyExDto.AppComOrgLevelList)
                //{
                //    ProcessNewComOrgLeveExDto(aAppCompanyEntity, appComOrgLevelExDto);
                //}

                using (DataAccessAdapter adapter = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
                {

                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppCompanyEntity);
                        adapter.Commit();

                        // need to root fodler new createion compnay 
                        AppCompanyBL.CreateMyCompanyFolder(aAppCompanyEntity.AppCompanyId);



                        AppComOrganizationExDto aAppComOrganizationExDto = new AppComOrganizationExDto();
                        aAppComOrganizationExDto.AppCompanyId = aAppCompanyEntity.AppCompanyId;
                        aAppComOrganizationExDto.Code = aAppCompanyExDto.Code;
                        aAppComOrganizationExDto.ShortName = aAppCompanyExDto.ShortName;
                        aAppComOrganizationExDto.FullName = aAppCompanyExDto.FullName;

                        if (string.IsNullOrWhiteSpace(aAppComOrganizationExDto.Code))
                        {
                            if (!string.IsNullOrWhiteSpace(aAppComOrganizationExDto.ShortName))
                            {
                                aAppComOrganizationExDto.Code = aAppComOrganizationExDto.ShortName;
                            }
                            else if (!string.IsNullOrWhiteSpace(aAppComOrganizationExDto.FullName))
                            {
                                aAppComOrganizationExDto.Code = aAppComOrganizationExDto.FullName;
                            }
                            else
                            {
                                aAppComOrganizationExDto.Code = "Company";
                            }
                        }

                        aAppComOrganizationExDto.ClassificationLevel = 1;
                        aAppComOrganizationExDto.UserTypeEm = (int)EmAppUserType.Employee;
                        var organizationSaveResult = AppComOrgBL.SaveAppComOrganization(aAppComOrganizationExDto);

                        if (organizationSaveResult.ValidationResult.HasErrors)
                        {
                            adapter.Rollback();
                            aValidationResult.Merge(organizationSaveResult.ValidationResult);
                        }
                        else
                        {
                            aAppCompanyExDto.Id = aAppCompanyEntity.AppCompanyId;
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppCompanyExDto), "App_SecurityGroupEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                            AppCompanyBL.CreateMyCompanyFolder(aAppCompanyEntity.AppCompanyId);
                        }
                    }


                    catch (ORMQueryExecutionException ex)
                    {

                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppCompanyExDto), "App_SecurityGroupEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }


            }

            else if (aAppCompanyExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(ProcessDirtyAppCompanyExDto(aAppCompanyExDto));
            }


            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppCompanyExDto(aAppCompanyExDto.Id as int?);

                if (!aAppCompanyExDto.IsNew)
                {
                    SetServerContextCompanyProperties(aOperationCallResult.Object);


                }
                // need to create company folder
            }

            return aOperationCallResult;


        }


        public static string GetCompanyBackgroundImageFolderPath(int companyId)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return string.Format(@"{0}FileRepository\Company_{1}\Image\Background\", baseDirectory, companyId);
        }

        public static string GetCurrentCompanyBackgroundImageFolderPath()
        {
            if (ServerContext.Instance != null && ServerContext.Instance.CompanySettings != null && ServerContext.Instance.CompanySettings.CompanyId.HasValue)
                return GetCompanyBackgroundImageFolderPath(ServerContext.Instance.CompanySettings.CompanyId.Value);

            return "";
        }

        public static string GetCompanyLogoImageFolderPath(int companyId)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return string.Format(@"{0}FileRepository\Company_{1}\Image\Logo\", baseDirectory, companyId);
        }

        public static string GetCurrentCompanyLogoImageFolderPath()
        {
            if (ServerContext.Instance != null && ServerContext.Instance.CompanySettings != null && ServerContext.Instance.CompanySettings.CompanyId.HasValue)
                return GetCompanyLogoImageFolderPath(ServerContext.Instance.CompanySettings.CompanyId.Value);


            return "";

        }

        public static string GetCurrentCompanyLogoImageUrl(int? companyId = null)
        {
            string url = "";

            int? resolvedId = companyId
                ?? (ServerContext.Instance?.CompanySettings?.CompanyId);

            if (!resolvedId.HasValue) return url;

            string folderPath = AppCompanyBL.GetCompanyLogoImageFolderPath(resolvedId.Value);

            if (Directory.Exists(folderPath))
            {
                try
                {
                    DirectoryInfo dir = new DirectoryInfo(folderPath);
                    var files = dir.GetFiles();

                    if (files.Count() > 0)
                    {
                        var file = files.First();
                        url = "/FileRepository/Company_" + resolvedId.Value + "/Image/Logo/" + file.Name;
                    }
                }
                catch (Exception ex)
                {

                }
            }

            return url;
        }


        public static List<string> GetCurrentCompanyBackgroundImageUrlList(int? companyId = null)
        {
            List<string> urlList = new List<string>();

            int? resolvedId = companyId
                ?? (ServerContext.Instance?.CompanySettings?.CompanyId);

            if (!resolvedId.HasValue) return urlList;

            string folderPath = AppCompanyBL.GetCompanyBackgroundImageFolderPath(resolvedId.Value);
            string companyIdStr = resolvedId.Value.ToString();

            if (Directory.Exists(folderPath))
            {
                try
                {
                    DirectoryInfo dir = new DirectoryInfo(folderPath);
                    var files = dir.GetFiles();

                    List<string> fileNameList = new List<string>();

                    foreach (var file in dir.GetFiles())
                    {
                        string fileName = file.Name.ToLower();

                        if (!fileNameList.Contains(fileName))
                        {
                            fileNameList.Add(fileName);

                            string url = "/FileRepository/Company_" + companyIdStr + "/Image/Background/" + file.Name;

                            urlList.Add(url);
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }

            return urlList;
        }



        public static OperationCallResult<bool> DeleteOneAppCompany(int companyId)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            try
            {
                using (DataAccessAdapter adapter = new DataAccessAdapter(AppMasterDBConnectionString))
                {
                    var users = new EntityCollection<AppSecurityUserEntity>();
                    adapter.FetchEntityCollection(users, new RelationPredicateBucket(AppSecurityUserFields.AppCreatedByCompanyId == companyId));
                    if (users.Count > 0)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppCompanyEntity),
                            "App_Company_HasUsers_Error", ValidationItemType.Error,
                            string.Format("Cannot delete: {0} user(s) are assigned to this company. Remove users first.", users.Count)));
                        return aOperationCallResult;
                    }

                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "DeleteCompany");
                        adapter.DeleteEntitiesDirectly(typeof(AppCompanyEntity),
                            new RelationPredicateBucket(AppCompanyFields.AppCompanyId == companyId));
                        adapter.Commit();
                    }
                    catch (Exception ex)
                    {
                        try { adapter.Rollback(); } catch { }
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppCompanyEntity),
                            "App_Company_Delete_Error", ValidationItemType.Error, ex.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppCompanyEntity),
                    "App_Company_Delete_Error", ValidationItemType.Error, ex.Message));
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<bool> DeleteCompanyLogoImage()
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            string folderPath = AppCompanyBL.GetCurrentCompanyLogoImageFolderPath();

            try
            {
                if (Directory.Exists(folderPath))
                {
                    DirectoryInfo dir = new DirectoryInfo(folderPath);
                    foreach (FileInfo aFile in dir.GetFiles())
                    {
                        aFile.Delete();
                    }
                }
            }
            catch (Exception ex)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectTeamMemberExDto), "App_ProjectTeamMember_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<bool> DeleteOneCompanyBackgroundImage(string fileName)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            string folderPath = AppCompanyBL.GetCurrentCompanyBackgroundImageFolderPath();

            try
            {
                if (Directory.Exists(folderPath))
                {
                    DirectoryInfo dir = new DirectoryInfo(folderPath);
                    foreach (FileInfo aFile in dir.GetFiles())
                    {
                        if (aFile.Name.ToLower() == fileName.ToLower())
                        {
                            aFile.Delete();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectTeamMemberExDto), "App_ProjectTeamMember_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
            }

            return aOperationCallResult;
        }

        //private static void ProcessNewComOrgLeveExDto(AppCompanyEntity appCompanyEntity, AppComOrgLevelExDto appComOrgLevelExDto)
        //{
        //    AppComOrgLevelEntity aAppComOrgLevelEntity = new AppComOrgLevelEntity();
        //    AppComOrgLevelConverter.CopyDtoToEntity(aAppComOrgLevelEntity, appComOrgLevelExDto);
        //    appCompanyEntity.AppComOrgLevel.Add(aAppComOrgLevelEntity);

        //}

        private static ValidationResult ProcessDirtyAppCompanyExDto(AppCompanyExDto appCompanyExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();


            AppCompanyEntity appCompanyEntity = RetrieveOneAppCompanyEntity(appCompanyExDto.Id);
            AppCompanyConverter.CopyDtoToEntity(appCompanyEntity, appCompanyExDto);


            //Dictionary<int, AppComOrgLevelEntity> dictDbAppComOrgLevel = appCompanyEntity.AppComOrgLevel.ToDictionary(o => o.OrgLevelId, o => o);
            //Dictionary<int, AppComOrgLevelExDto> dictDbAppComOrgLevelExdto = appCompanyExDto.AppComOrgLevelList.Where(o => !o.IsNew).ToDictionary(o => int.Parse(o.Id.ToString()), o => o);

            //List<int> childIdDbms = dictDbAppComOrgLevel.Keys.ToList();

            //List<int> childIdUi = dictDbAppComOrgLevelExdto.Keys.ToList();


            //Delete Id
            //List<int> deletAppComOrgLevelIDs = childIdDbms.Except(childIdUi).ToList();


            //appCompanyExDto.AppComOrgLevelList.Where(o => o.IsNew)
            //  .ForAll(o => ProcessNewComOrgLeveExDto(appCompanyEntity, o));



            //dirty
            //List<int> dirtyAppComOrgLevelIds = childIdDbms.Intersect(childIdUi).ToList();



            //foreach (int updateGroupId in dirtyAppComOrgLevelIds)
            //{
            //    //  dirty 
            //    AppComOrgLevelEntity AppComOrgLevelEntity = dictDbAppComOrgLevel[updateGroupId];
            //    AppComOrgLevelExDto AppComOrgLevelExdto = dictDbAppComOrgLevelExdto[updateGroupId];
            //    AppComOrgLevelConverter.CopyDtoToEntity(AppComOrgLevelEntity, AppComOrgLevelExdto);





            //}

            using (DataAccessAdapter adapter = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
            {

                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(appCompanyEntity);

                    //adapter.DeleteEntitiesDirectly(typeof(AppComOrgLevelEntity), new RelationPredicateBucket(AppComOrgLevelFields.OrgLevelId == deletAppComOrgLevelIDs));


                    adapter.Commit();
                    //aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectTeamMemberExDto), "App_ProjectTeamMember_Save_OK", ValidationItemType.Message, "Saved Successfully"));



                }


                catch (ORMQueryExecutionException ex)
                {

                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectTeamMemberExDto), "App_ProjectTeamMember_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));


                }


            }

            if (!aValidationResult.HasErrors)
            {
                // UpdateUserCompanyCodeNameOnHostDB was a double-write: SaveEntity above already
                // persists Code/ShortName/FullName to master DB via AppMasterDBConnectionString.
            }

            if (!aValidationResult.HasErrors)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectTeamMemberExDto), "App_ProjectTeamMember_Save_OK", ValidationItemType.Message, "Saved Successfully"));
            }

            return aValidationResult;
        }

        private static void UpdateUserCompanyCodeNameOnHostDB(AppCompanyExDto appCompanyExDto, ValidationResult aValidationResult, AppCompanyEntity appCompanyEntity)
        {
            using (DataAccessAdapter adpater = new DataAccessAdapter(AppMasterDBConnectionString))
            {
                try
                {
                    string query = "";

                    query += string.Format(@"  update [AppCompany] set Code = '{0}', ShortName = '{1}', FullName = '{2}' WHERE AppCompanyID = {3}"
                        , appCompanyExDto.Code, appCompanyExDto.ShortName, appCompanyExDto.FullName, appCompanyEntity.AppCompanyId
                        );

                    adpater.ExecuteExecuteNonQuery(query, new List<SqlParameter>());
                }
                catch (Exception ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectTeamMemberExDto), "App_ProjectTeamMember_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));

                }
            }
        }

        private static void PrepareCompanyOtherSettingsDto(AppCompanyDto aAppCompanyExDto)
        {
            aAppCompanyExDto.OtherSettingsDto = new AppCompanyOtherSettingsDto();
            aAppCompanyExDto.OtherSettingsDto.IsEnableClientSelfRegistration = ControlTypeValueConverter.ConvertValueToBoolean(aAppCompanyExDto.CurrencyCode);
            aAppCompanyExDto.OtherSettingsDto.IsEnableSupplierSelfRegistration = ControlTypeValueConverter.ConvertValueToBoolean(aAppCompanyExDto.ContactPhone);
            aAppCompanyExDto.OtherSettingsDto.IsEnableClientAgentSelfRegistration = ControlTypeValueConverter.ConvertValueToBoolean(aAppCompanyExDto.ContactName);
            aAppCompanyExDto.OtherSettingsDto.IsEnableSupplierAgentSelfRegistration = ControlTypeValueConverter.ConvertValueToBoolean(aAppCompanyExDto.ContactFax);

            aAppCompanyExDto.OtherSettingsDto.ClientLabelName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(aAppCompanyExDto.Adress1);
            aAppCompanyExDto.OtherSettingsDto.SupplierLabelName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(aAppCompanyExDto.City);
            aAppCompanyExDto.OtherSettingsDto.ClientAgentLabelName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(aAppCompanyExDto.State);
            aAppCompanyExDto.OtherSettingsDto.SupplierAgentLabelName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(aAppCompanyExDto.Adress2);

            //aAppCompanyExDto.OtherSettingsDto.LogoImageId = aAppCompanyExDto.EmApplicationVersionEdition;
            //aAppCompanyExDto.OtherSettingsDto.LoginPageBackgroundImageIdList = new List<int>();

            //if (!string.IsNullOrWhiteSpace(aAppCompanyExDto.Adress3))
            //{
            //    string[] imageIdStrList = aAppCompanyExDto.Adress3.Split('|');

            //    var imageIdList = aAppCompanyExDto.OtherSettingsDto.LoginPageBackgroundImageIdList;

            //    foreach (var imageIdStr in imageIdStrList)
            //    {
            //        int? imageId = ControlTypeValueConverter.ConvertValueToInt(imageIdStr);

            //        if (imageId.HasValue && !imageIdList.Contains(imageId.Value))
            //        {
            //            imageIdList.Add(imageId.Value);
            //        }
            //    }
            //}
        }



        private static void ConvertCompanyOtherSettingsDtoToDbValue(AppCompanyExDto aAppCompanyExDto)
        {
            if (aAppCompanyExDto.OtherSettingsDto == null)
            {
                aAppCompanyExDto.OtherSettingsDto = new AppCompanyOtherSettingsDto();
            }

            aAppCompanyExDto.CurrencyCode = "";
            aAppCompanyExDto.ContactPhone = "";
            aAppCompanyExDto.ContactName = "";
            aAppCompanyExDto.ContactFax = "";

            aAppCompanyExDto.Adress1 = "";
            aAppCompanyExDto.City = "";
            aAppCompanyExDto.State = "";
            aAppCompanyExDto.Adress2 = "";

            aAppCompanyExDto.EmApplicationVersionEdition = null;
            aAppCompanyExDto.Adress3 = "";

            if (aAppCompanyExDto.OtherSettingsDto.IsEnableClientSelfRegistration.HasValue)
            {
                aAppCompanyExDto.CurrencyCode = aAppCompanyExDto.OtherSettingsDto.IsEnableClientSelfRegistration.Value.ToString();
            }

            if (aAppCompanyExDto.OtherSettingsDto.IsEnableSupplierSelfRegistration.HasValue)
            {
                aAppCompanyExDto.ContactPhone = aAppCompanyExDto.OtherSettingsDto.IsEnableSupplierSelfRegistration.Value.ToString();
            }

            if (aAppCompanyExDto.OtherSettingsDto.IsEnableClientAgentSelfRegistration.HasValue)
            {
                aAppCompanyExDto.ContactName = aAppCompanyExDto.OtherSettingsDto.IsEnableClientAgentSelfRegistration.Value.ToString();
            }

            if (aAppCompanyExDto.OtherSettingsDto.IsEnableSupplierAgentSelfRegistration.HasValue)
            {
                aAppCompanyExDto.ContactFax = aAppCompanyExDto.OtherSettingsDto.IsEnableSupplierAgentSelfRegistration.Value.ToString();
            }

            aAppCompanyExDto.Adress1 = aAppCompanyExDto.OtherSettingsDto.ClientLabelName;
            aAppCompanyExDto.City = aAppCompanyExDto.OtherSettingsDto.SupplierLabelName;
            aAppCompanyExDto.State = aAppCompanyExDto.OtherSettingsDto.ClientAgentLabelName;
            aAppCompanyExDto.Adress2 = aAppCompanyExDto.OtherSettingsDto.SupplierAgentLabelName;

            //aAppCompanyExDto.EmApplicationVersionEdition = aAppCompanyExDto.OtherSettingsDto.LogoImageId;

            //if (aAppCompanyExDto.OtherSettingsDto.LoginPageBackgroundImageIdList != null && aAppCompanyExDto.OtherSettingsDto.LoginPageBackgroundImageIdList.Count > 0)
            //{
            //    aAppCompanyExDto.Adress3 = string.Join("|", aAppCompanyExDto.OtherSettingsDto.LoginPageBackgroundImageIdList.Select(o => o.ToString()));
            //}


        }

        // ── Company file-system helpers ──────────────────────────────────────

        private static readonly string FileRepositoryPath =
            AppDomain.CurrentDomain.BaseDirectory + @"FileRepository\";

        public static string GetMyCompanyImagePath()
        {
            string companyIdPath = string.Format("Company_{0}", APP.Framework.ServerContext.Instance.CurrentCompanyId);
            return string.Format(@"{0}\{1}\Image\", FileRepositoryPath, companyIdPath);
        }

        public static string GetMyCompanyWebSitePath()
        {
            string companyIdPath = string.Format("Company_{0}", APP.Framework.ServerContext.Instance.CurrentCompanyId);
            return string.Format(@"{0}\{1}\WebSite\", FileRepositoryPath, companyIdPath);
        }

        public static string GetMyCompanyReportPath()
        {
            string companyIdPath = string.Format("Company_{0}", APP.Framework.ServerContext.Instance.CurrentCompanyId);
            return string.Format(@"{0}\{1}\Report\", FileRepositoryPath, companyIdPath);
        }

        public static string GetMyCompanyTempPath()
        {
            string companyIdPath = string.Format("Company_{0}", APP.Framework.ServerContext.Instance.CurrentCompanyId);
            return string.Format(@"{0}\{1}\temp\", FileRepositoryPath, companyIdPath);
        }

        public static void CreateMyCompanyFolder(int companyId)
        {
            string root = string.Format(@"{0}Company_{1}", FileRepositoryPath, companyId);
            System.IO.Directory.CreateDirectory(root + @"\Image\");
            System.IO.Directory.CreateDirectory(root + @"\Image\original\");
            System.IO.Directory.CreateDirectory(root + @"\Image\regular\");
            System.IO.Directory.CreateDirectory(root + @"\Image\temp\");
            System.IO.Directory.CreateDirectory(root + @"\Image\tessdata\");
            System.IO.Directory.CreateDirectory(root + @"\Image\thumbnail\");
            System.IO.Directory.CreateDirectory(root + @"\Report\");
            System.IO.Directory.CreateDirectory(root + @"\temp\");
        }


    }
}