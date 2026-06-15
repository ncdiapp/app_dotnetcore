using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using System;
using ExchangeBL;
#if NETFRAMEWORK
using ActiveUp.Net.Mail;
#endif
using System.Net;
using System.Text.RegularExpressions;
using NJsonSchema;
using AngleSharp.Common;
#if NETFRAMEWORK
using System.Management.Automation.Language;
#endif
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Threading.Tasks;
//using Microsoft.Office.Interop.Word;
#if NETFRAMEWORK
using static SpreadsheetGear.Windows.Forms.CommandRange;
#endif

using Newtonsoft.Json;
using System.Net.Http;
using DatabaseSchemaMrg;
using DatabaseSchemaMrg.DataSchema;
using APP.Framework.Excel;
using Stripe;


using APP.Framework;
namespace App.BL
{
    public static class AppIntergrationSettingBL
    {
        public static readonly int AppBuiltInProviderId = 1;

        public static readonly string App_IntergrationSettingEntity_Save_OK = "App_IntergrationSettingEntity_Save_OK";
        public static readonly string App_IntergrationSettingEntity_Save_Failed = "App_IntergrationSettingEntity_Save_Failed";
        public static readonly string App_IntergrationSettingEntityUILayout_Save_OK = "App_IntergrationSettingEntityUILayout_Save_OK";
        public static readonly string App_IntergrationSettingEntityUILayout_Save_Failed = "App_IntergrationSettingEntityUILayout_Save_Failed";
        public static readonly string App_IntergrationSettingEntity_Delete_Ok = "App_IntergrationSettingEntity_Delete_Ok";
        public static readonly string App_IntergrationSettingEntity_Delete_Failed = "App_IntergrationSettingEntity_Delete_Failed";


        public static readonly string EmbedTranscationToFormCode = "EmbedTranscationToForm";

        public static readonly Dictionary<string, int> DictEmbedTrnscationID = new Dictionary<string, int>();

        public static readonly string MappingNameSpliter = "___";


        public static List<AppTransactionDto> RetrieveApiWhereUsedOnTransactions(int? apiId)
        {
            List<AppTransactionDto> toReturn = new List<AppTransactionDto>();

            if (apiId.HasValue)
            {
                List<AppTransactionDto> transactionDtoList = AppTransactionBL.RetrieveAllAppTransactionDto(null, null);

                foreach (var transactionDto in transactionDtoList.OrderBy(o => o.TransactionName))
                {
                    if (transactionDto.FolderUsageType.HasValue && transactionDto.FolderUsageType.Value == apiId.Value)
                    {
                        toReturn.Add(transactionDto);
                    }
                }
            }

            return toReturn;
        }

        public static List<AppSearchDto> RetrieveApiWhereUsedOnSearches(int? apiId)
        {
            List<AppSearchDto> toReturn = new List<AppSearchDto>();

            if (apiId.HasValue)
            {
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    EntityCollection<AppDataSetEntity> dataSetList = new EntityCollection<AppDataSetEntity>();


                    RelationPredicateBucket datasetFilter = new RelationPredicateBucket(AppDataSetFields.QueryType == (int)EmAppDataServiceType.IntegrationWebApiCall);
                    datasetFilter.PredicateExpression.AddWithAnd(AppDataSetFields.QueryText == apiId.Value.ToString());

                    adapter.FetchEntityCollection(dataSetList, datasetFilter, 0);

                    if (dataSetList.Count > 0)
                    {
                        var dataSetIds = dataSetList.Select(o => o.DataSetId).ToArray();

                        EntityCollection<AppSearchEntity> searchEntityList = new EntityCollection<AppSearchEntity>();

                        RelationPredicateBucket searchFilter = new RelationPredicateBucket(AppSearchFields.DataSetId == dataSetIds);
                        adapter.FetchEntityCollection(searchEntityList, searchFilter);

                        foreach (var searchEntity in searchEntityList.OrderBy(o => o.Name))
                        {
                            AppSearchDto searchDto = AppSearchConverter.ConvertEntityToDto(searchEntity);
                            toReturn.Add(searchDto);
                        }
                    }
                }
            }


            return toReturn;
        }


        public static int? GetFormEmbedTranscationId(string formName)
        {
            if (DictEmbedTrnscationID.ContainsKey(formName))
            {
                return DictEmbedTrnscationID[formName];
            }
            else
            {
                PopulateDictEmbedFormTrnscationId();

                if (DictEmbedTrnscationID.ContainsKey(formName))
                {
                    return DictEmbedTrnscationID[formName];
                }
                else
                {
                    return null;
                }

            }


        }


        /// <summary>
        ///  Kye: Form Name, value TrenscatinId 
        /// </summary>
        /// <returns></returns>

        private static void PopulateDictEmbedFormTrnscationId()
        {
            Dictionary<string, int> toReturn = new Dictionary<string, int>();
            EntityCollection<AppIntergrationSettingEntity> listEntity = new EntityCollection<AppIntergrationSettingEntity>();
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {

                RelationPredicateBucket predisct = new RelationPredicateBucket(AppIntergrationSettingFields.InternalCode == EmbedTranscationToFormCode);

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppIntergrationSettingEntity);




                rootPath.Add(AppIntergrationSettingEntity.PrefetchPathAppIntergrationSettingParameter);

                adpater.FetchEntityCollection(listEntity, predisct, rootPath);

            }

            if (!listEntity.IsEmpty())
            {
                toReturn = listEntity.First().AppIntergrationSettingParameter.ToDictionary(o => o.MappingInternalCode, o => o.TranscationId.Value);
            }

            foreach (string mappingKey in toReturn.Keys)
            {
                DictEmbedTrnscationID[mappingKey] = toReturn[mappingKey];
            }
            //return toReturn;
        }

        /// <summary>
        ///  for Exttrrnal 
        /// </summary>
        /// <returns></returns>
        public static List<AppIntergrationSettingExDto> RetrieveAllAppIntergrationSettingDto(bool isIncludeAppBuiltInApi = false)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppIntergrationSettingEntity> list = new EntityCollection<AppIntergrationSettingEntity>();


                IPrefetchPath2 root = new PrefetchPath2(EntityType.AppIntergrationSettingEntity);


                ExcludeFieldsList excludeFieldsList = new ExcludeFieldsList();
                excludeFieldsList.Add(AppIntergrationSettingParameterFields.ApiconfigParameters);
                excludeFieldsList.Add(AppIntergrationSettingParameterFields.JsonQuery);
                excludeFieldsList.Add(AppIntergrationSettingParameterFields.JsonSampleData);
                excludeFieldsList.Add(AppIntergrationSettingParameterFields.JsonSchema);
                excludeFieldsList.Add(AppIntergrationSettingParameterFields.SchemaDataSetMapping);
                excludeFieldsList.Add(AppIntergrationSettingParameterFields.SchemaFromDataSetMapping);
                excludeFieldsList.Add(AppIntergrationSettingParameterFields.PostProcessScript);

                root.Add(AppIntergrationSettingEntity.PrefetchPathAppIntergrationSettingParameter, excludeFieldsList);

                RelationPredicateBucket filter = null;
                if (!isIncludeAppBuiltInApi)
                {
                    filter = new RelationPredicateBucket(AppIntergrationSettingFields.IntergrationSettingId != 1);
                }




                SortExpression expression = new SortExpression(AppIntergrationSettingFields.Name | SortOperator.Ascending);
                adapter.FetchEntityCollection(list, filter, 0, expression, root);

                var aDtoList = new List<AppIntergrationSettingExDto>();

                foreach (var settingEntity in list)
                {
                    var dto = AppIntergrationSettingConverter.ConvertEntityToExDto(settingEntity);

                    foreach (var paramEntity in settingEntity.AppIntergrationSettingParameter.OrderBy(o => o.ActionCode))
                    {
                        if (string.IsNullOrWhiteSpace(paramEntity.MappingInternalCode) || paramEntity.MappingInternalCode == EmAppIntergrationSettingParameterUsageType.ApiOperation.ToString())
                        {
                            var operationDto = AppIntergrationSettingParameterConverter.ConvertEntityToExDto(paramEntity);
                            operationDto.ProviderName = dto.Name;
                            dto.AppIntergrationSettingParameterList.Add(operationDto);
                        }
                    }

                    aDtoList.Add(dto);
                }

                return aDtoList;
            }
        }



        /// <summary>
        /// Loads the built-in App API Provider integration (id = <see cref="AppBuiltInProviderId"/>).
        /// Returns null when that row does not exist in the tenant database.
        /// </summary>
        public static AppIntergrationSettingExDto TryRetrieveAppBuiltInProviderExDto()
        {
            try
            {
                AppIntergrationSettingEntity entity = RetrieveOneAppIntergrationSettingEntity(AppBuiltInProviderId);
                if (entity.IsNew)
                {
                    return null;
                }

                return RetrieveOneAppIntergrationSettingExDto(AppBuiltInProviderId);
            }
            catch
            {
                return null;
            }
        }

        public static AppIntergrationSettingExDto RetrieveOneAppIntergrationSettingExDto(object IntergrationSettingId)
        {
            AppIntergrationSettingEntity aAppIntergrationSettingEntity = RetrieveOneAppIntergrationSettingEntity(IntergrationSettingId);
            if (aAppIntergrationSettingEntity.IsNew)
            {
                return null;
            }

            AppIntergrationSettingExDto aIntergrationSettingDto = AppIntergrationSettingConverter.ConvertEntityToExDto(aAppIntergrationSettingEntity);

            if (aAppIntergrationSettingEntity.AppIntergrationSettingParameter != null)
            {
                foreach (var o in aAppIntergrationSettingEntity.AppIntergrationSettingParameter)
                {
                AppIntergrationSettingParameterExDto aExDto = AppIntergrationSettingParameterConverter.ConvertEntityToExDto(o);

                if (string.IsNullOrWhiteSpace(aExDto.MappingInternalCode) || aExDto.MappingInternalCode == EmAppIntergrationSettingParameterUsageType.ApiOperation.ToString())
                {

                    if (aExDto.IsSimpleQuery.HasValue && aExDto.IsSimpleQuery.Value)
                    {
                        aExDto.APIType = "SQL JSON Query API";
                    }
                    else if (aExDto.TranscationId.HasValue)
                    {
                        aExDto.APIType = "APP Data Model API";
                    }
                    else if (aExDto.TranscationFieId.HasValue)
                    {
                        aExDto.APIType = "APP Data Presentation API";
                    }
                    else if (aExDto.APIConfigParameters != null && aExDto.APIConfigParameters.ExcelDataImportDataSetId.HasValue)
                    {
                        aExDto.APIType = "Excel Import Update Data API";

                        //aExDto.ExcelImportSettingDto = AppDatabaseTableImportBL.RetrieveOneTableImportSettingDto(aExDto.APIConfigParameters.ExcelDataImportDataSetId.Value);
                    }
                    else
                    {
                        aExDto.APIType = "Standard API";
                    }

                    aIntergrationSettingDto.AppIntergrationSettingParameterList.Add(aExDto);
                }
            }
            }


            if ((int)IntergrationSettingId == AppBuiltInProviderId) // App API Provider
            {
                try
                {
                    var allTransactions = AppTransactionBL.RetrieveAllAppTransactionDto(null);
                    var allSearchs = AppSearchConfigBL.RetrieveAllAppSearchDto();
                    var allDataSet = AppDataSetBL.RetrieveAllAppDataSetEntityDto();
                    var allApplicatoins = AppSaasUserApplicationPackageBL.GetSaasApplicationList();
                    var allDataSource = AppDataSourceRegisterBL.RetrieveAllAppDataSourceRegisterExDto();

                    var dictTransIdAndDto = allTransactions.ToDictionary(o => (int)o.Id, o => o);
                    var dictSearchIdIdAndDto = allSearchs.ToDictionary(o => (int)o.Id, o => o);
                    var dictDataSetIdIdAndDto = allDataSet.ToDictionary(o => (int)o.Id, o => o);
                    var dictAppIdIdAndDto = allApplicatoins.ToDictionary(o => (int)o.Id, o => o);
                    var dictDataSourceIdAndDto = allDataSource.ToDictionary(o => (int)o.Id, o => o);

                    foreach (var aExDto in aIntergrationSettingDto.AppIntergrationSettingParameterList)
                    {
                        if (aExDto.IsSimpleQuery.HasValue && aExDto.IsSimpleQuery.Value)
                        {

                        }
                        else if (aExDto.TranscationId.HasValue)
                        {
                            if (dictTransIdAndDto.ContainsKey(aExDto.TranscationId.Value))
                            {
                                aExDto.SaasApplicationId = dictTransIdAndDto[aExDto.TranscationId.Value].SaasApplicationId;
                                aExDto.DataSourceId = dictTransIdAndDto[aExDto.TranscationId.Value].DataSourceFrom;
                            }
                        }
                        else if (aExDto.TranscationFieId.HasValue)
                        {
                            if (dictSearchIdIdAndDto.ContainsKey(aExDto.TranscationFieId.Value))
                            {
                                aExDto.SaasApplicationId = dictSearchIdIdAndDto[aExDto.TranscationFieId.Value].SaasApplicationId;
                                aExDto.DataSourceId = dictSearchIdIdAndDto[aExDto.TranscationFieId.Value].DataSourceFrom;
                            }
                        }
                        else if (aExDto.APIConfigParameters != null && aExDto.APIConfigParameters.ExcelDataImportDataSetId.HasValue)
                        {
                            int dataSetId = aExDto.APIConfigParameters.ExcelDataImportDataSetId.Value;

                            if (dictDataSetIdIdAndDto.ContainsKey(dataSetId))
                            {
                                aExDto.SaasApplicationId = dictDataSetIdIdAndDto[dataSetId].SaasApplicationId;
                                aExDto.DataSourceId = dictDataSetIdIdAndDto[dataSetId].DataSourceFrom;
                            }
                        }

                        if (aExDto.SaasApplicationId.HasValue && dictAppIdIdAndDto.ContainsKey(aExDto.SaasApplicationId.Value))
                        {
                            aExDto.ApplicatoinDisplay = dictAppIdIdAndDto[aExDto.SaasApplicationId.Value].Name;
                        }
                        else
                        {
                            aExDto.ApplicatoinDisplay = "Other API";

                            if (aExDto.DataSourceId.HasValue && dictDataSourceIdAndDto.ContainsKey(aExDto.DataSourceId.Value))
                            {
                                aExDto.ApplicatoinDisplay = "Other API on " + dictDataSourceIdAndDto[aExDto.DataSourceId.Value].DataSourceName;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                }
                
            }


            return aIntergrationSettingDto;
        }



        public static List<AppIntergrationSettingParameterExDto> RetrieveAllJsonFileTableImportSettingDtoList()
        {
            List<AppIntergrationSettingParameterExDto> toReturn = new List<AppIntergrationSettingParameterExDto>();

            var settingEntlityList = RetrieveAllAppIntergrationSettingParameterSimpleEntityList(EmAppIntergrationSettingParameterUsageType.JsonFileTableImportSetting.ToString());

            foreach (var o in settingEntlityList)
            {
                AppIntergrationSettingParameterExDto aExDto = AppIntergrationSettingParameterConverter.ConvertEntityToExDto(o);
                aExDto.SchemaDataSetMapping = "";


                var orgMappingDto = aExDto.SchemaDataSetMappingDto;
                aExDto.SchemaDataSetMappingDto = new AppIntergrationSchemaDataSetMappingDto()
                {
                    GeneratedTableNameList = orgMappingDto.GeneratedTableNameList
                };

                toReturn.Add(aExDto);
            }

            return toReturn;
        }


        public static List<AppIntergrationSettingParameterExDto> RetrieveAllApiStagingTableImportSettingDtoList()
        {
            List<AppIntergrationSettingParameterExDto> toReturn = new List<AppIntergrationSettingParameterExDto>();

            var settingEntlityList = RetrieveAllAppIntergrationSettingParameterSimpleEntityList(EmAppIntergrationSettingParameterUsageType.ApiTableImportSetting.ToString());

            List<AppIntergrationSettingExDto> providerList = RetrieveAllAppIntergrationSettingDto();
            Dictionary<int, AppIntergrationSettingExDto> dictProviderIdAndDto = providerList.ToDictionary(o => (int)o.Id, o => o);
            Dictionary<int, AppIntergrationSettingParameterExDto> dictOperationIdAndDto = new Dictionary<int, AppIntergrationSettingParameterExDto>();

            foreach (var providerExDto in providerList)
            {
                foreach (var parameterDto in providerExDto.AppIntergrationSettingParameterList)
                {
                    dictOperationIdAndDto.Add((int)parameterDto.Id, parameterDto);
                }
            }

            foreach (var o in settingEntlityList)
            {
                AppIntergrationSettingParameterExDto settingDto = AppIntergrationSettingParameterConverter.ConvertEntityToExDto(o);
                if (settingDto.OtherSettingsDto != null && settingDto.OtherSettingsDto.ParentOperationId.HasValue && dictOperationIdAndDto.ContainsKey(settingDto.OtherSettingsDto.ParentOperationId.Value))
                {
                    var operationDto = dictOperationIdAndDto[settingDto.OtherSettingsDto.ParentOperationId.Value];
                    settingDto.ParentOperationName = operationDto.ActionCode;
                    settingDto.ProviderName = operationDto.ProviderName;
                }

                settingDto.SchemaDataSetMapping = "";
                var orgMappingDto = settingDto.SchemaDataSetMappingDto;
                settingDto.SchemaDataSetMappingDto = new AppIntergrationSchemaDataSetMappingDto()
                {
                    GeneratedTableNameList = orgMappingDto.GeneratedTableNameList
                };


                toReturn.Add(settingDto);
            }

            toReturn = toReturn.OrderBy(o => o.ProviderName).ThenBy(o => o.ParentOperationName).ThenBy(o => o.ActionCode).ToList();

            return toReturn;
        }

        public static AppIntergrationSettingEntity RetrieveOneAppIntergrationSettingEntity(object IntergrationSettingId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppIntergrationSettingEntity IntergrationSettingEntity = new AppIntergrationSettingEntity(int.Parse(IntergrationSettingId.ToString()));




                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppIntergrationSettingEntity);


                ExcludeFieldsList excludeFieldsList = new ExcludeFieldsList();
                // excludeFieldsList.Add(AppIntergrationSettingParameterFields.ApiconfigParameters);
                excludeFieldsList.Add(AppIntergrationSettingParameterFields.JsonQuery);
                excludeFieldsList.Add(AppIntergrationSettingParameterFields.JsonSampleData);
                excludeFieldsList.Add(AppIntergrationSettingParameterFields.JsonSchema);
                excludeFieldsList.Add(AppIntergrationSettingParameterFields.SchemaDataSetMapping);
                excludeFieldsList.Add(AppIntergrationSettingParameterFields.SchemaFromDataSetMapping);
                excludeFieldsList.Add(AppIntergrationSettingParameterFields.PostProcessScript);

                rootPath.Add(AppIntergrationSettingEntity.PrefetchPathAppIntergrationSettingParameter, excludeFieldsList);

                adpater.FetchEntity(IntergrationSettingEntity, rootPath);
                return IntergrationSettingEntity;
            }
        }

        public static OperationCallResult<AppIntergrationSettingParameterExDto> CreateJsonFileDatabaseTableImportSettingByFileId(int? jsonFileId, int? dataSourceRegId, bool isImportToExistingTable)
        {
            OperationCallResult<AppIntergrationSettingParameterExDto> aOperationCallResult = new OperationCallResult<AppIntergrationSettingParameterExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (jsonFileId.HasValue)
            {
                var fileExDto = AppFileBL.RetrieveOneLatestAppFileExDto(jsonFileId.Value);
                if (fileExDto != null)
                {
                    string jsonString = "";

                    if (!string.IsNullOrEmpty(fileExDto.OriginalFilePath))
                    {
                        string fileFullPathName = AppCompanyBL.GetMyCompanyImagePath() + fileExDto.OriginalFilePath;

                        byte[] buffer = StreamHelper.FileToByteArray(fileFullPathName);

                        if (buffer != null)
                        {
                            jsonString = System.Text.Encoding.UTF8.GetString(buffer);
                        }


                    }

                    if (!string.IsNullOrWhiteSpace(jsonString))
                    {

                        if (!dataSourceRegId.HasValue)
                        {
                            dataSourceRegId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
                        }

                        AppIntergrationSettingParameterExDto newImportSettingDto = new AppIntergrationSettingParameterExDto();
                        newImportSettingDto.IntergrationSettingId = AppBuiltInProviderId;
                        newImportSettingDto.OtherSettingsDto = new AppIntergrationParameterOtherSettingsDto();
                        newImportSettingDto.OtherSettingsDto.IsNeedToGenerateStagingTables = !isImportToExistingTable;
                        newImportSettingDto.MappingInternalCode = EmAppIntergrationSettingParameterUsageType.JsonFileTableImportSetting.ToString();
                        newImportSettingDto.HttpMethd = EnumHttpMethod.Get.ToString();
                        newImportSettingDto.DataSourceId = dataSourceRegId;
                        newImportSettingDto.ActionCode = fileExDto.FileCode + "_" + jsonFileId.Value.ToString();
                        newImportSettingDto.JsonSampleData = jsonString;
                        newImportSettingDto.ExternalFieldName = fileExDto.FileCode;

                        var generateResult = DataExchangeSettingBL.GenerateDefaultSchemaAndDataSetMappingFromSampleJson(newImportSettingDto);

                        if (generateResult.IsSuccessfulWithResult)
                        {
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_CreateJsonFileDatabaseTableImportSettingByFileId_saveOk", ValidationItemType.Message,
                                "Create Import Setting Success."));
                            aOperationCallResult.Object = generateResult.Object;
                        }
                        else
                        {
                            aValidationResult.Merge(generateResult.ValidationResult);
                        }

                    }
                    else
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_CreateJsonFileDatabaseTableImportSettingByFileId_Error", ValidationItemType.Error,
                           "Cannot get JSON structure. The input file is not a valid JSON file with data."));
                    }

                }
                else
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_CreateJsonFileDatabaseTableImportSettingByFileId_Error", ValidationItemType.Error,
                        "File does not exist."));
                }
            }


            return aOperationCallResult;
        }

        public static OperationCallResult<AppIntergrationSettingParameterExDto> CreateJsonDatabaseTableImportSettingFromJsonText(AppIntergrationSettingParameterExDto newImportSettingDto)
        {
            OperationCallResult<AppIntergrationSettingParameterExDto> aOperationCallResult = new OperationCallResult<AppIntergrationSettingParameterExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (newImportSettingDto != null)
            {
                int? dataSourceRegId = newImportSettingDto.DataSourceId;

                if (!string.IsNullOrWhiteSpace(newImportSettingDto.JsonSampleData))
                {
                    try
                    {

                        JToken jsonToken = JToken.Parse(newImportSettingDto.JsonSampleData);


                        if (jsonToken.Type == JTokenType.Array)
                        {
                            JArray jsonArray = (JArray)jsonToken;
                            JObject jsonObject = new JObject();
                            jsonObject["RootObject"] = jsonArray;

                            newImportSettingDto.JsonSampleData = jsonObject.ToString();
                        }
                        else if (jsonToken.Type == JTokenType.Object)
                        {
                            JObject orgJsonObject = (JObject)jsonToken;

                            bool containsNonArrayOrObjectProperty = orgJsonObject.Properties()
                                    .Any(prop => prop.Value.Type != JTokenType.Array && prop.Value.Type != JTokenType.Object);

                            if (containsNonArrayOrObjectProperty)
                            {
                                JObject newJObject = new JObject();
                                newJObject["RootObject"] = orgJsonObject;

                                newImportSettingDto.JsonSampleData = newJObject.ToString();
                            }
                        }

                        if (!newImportSettingDto.DataSourceId.HasValue)
                        {
                            newImportSettingDto.DataSourceId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
                        }

                        newImportSettingDto.IntergrationSettingId = AppBuiltInProviderId;
                        newImportSettingDto.OtherSettingsDto = new AppIntergrationParameterOtherSettingsDto();
                        newImportSettingDto.OtherSettingsDto.IsNeedToGenerateStagingTables = true;
                        newImportSettingDto.MappingInternalCode = EmAppIntergrationSettingParameterUsageType.JsonFileTableImportSetting.ToString();
                        newImportSettingDto.HttpMethd = EnumHttpMethod.Get.ToString();
                        newImportSettingDto.ExternalFieldName = "";

                        var generateResult = DataExchangeSettingBL.GenerateDefaultSchemaAndDataSetMappingFromSampleJson(newImportSettingDto);

                        if (generateResult.IsSuccessfulWithResult)
                        {
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_CreateJsonFileDatabaseTableImportSettingByFileId_saveOk", ValidationItemType.Message,
                                "Create Import Setting Success."));
                            aOperationCallResult.Object = generateResult.Object;
                        }
                        else
                        {
                            aValidationResult.Merge(generateResult.ValidationResult);
                        }
                    }
                    catch (Exception ex)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_CreateJsonFileDatabaseTableImportSettingByFileId_Error", ValidationItemType.Error,
                          "Cannot get JSON structure. The input file is not a valid JSON file with data."));
                    }
                }
                else
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_CreateJsonFileDatabaseTableImportSettingByFileId_Error", ValidationItemType.Error,
                       "Cannot get JSON structure. The input file is not a valid JSON file with data."));
                }
            }


            return aOperationCallResult;
        }
        public static OperationCallResult<AppIntergrationSettingParameterExDto> UpdateStagingTableDataFromJsonUpload(int? importSettingId, int? jsonFileId)
        {
            OperationCallResult<AppIntergrationSettingParameterExDto> aOperationCallResult = new OperationCallResult<AppIntergrationSettingParameterExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (jsonFileId.HasValue && importSettingId.HasValue)
            {
                var fileExDto = AppFileBL.RetrieveOneLatestAppFileExDto(jsonFileId.Value);
                if (fileExDto != null)
                {
                    string jsonString = "";

                    if (!string.IsNullOrEmpty(fileExDto.OriginalFilePath))
                    {
                        string fileFullPathName = AppCompanyBL.GetMyCompanyImagePath() + fileExDto.OriginalFilePath;

                        byte[] buffer = StreamHelper.FileToByteArray(fileFullPathName);

                        if (buffer != null)
                        {
                            jsonString = System.Text.Encoding.UTF8.GetString(buffer);
                        }


                    }

                    if (!string.IsNullOrWhiteSpace(jsonString))
                    {
                        try
                        {
                            AppIntergrationSettingParameterExDto importSettingDto = DataExchangeSettingBL.GetSetting(importSettingId.Value);

                            JsonSchemaBL.ImportJsonToStagingTable(importSettingDto, jsonString);

                            aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_IntergrationSettingEntity_Save_OK", ValidationItemType.Message, "Execution Completed"));
                        }
                        catch (Exception ex)
                        {

                            string errorMsg = GenerateStagingTableDataUpdateExceptionUserErrorMessage(ex);

                            aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Condition pass failure", ValidationItemType.Error, errorMsg, true, ex.ToString()));
                        }

                    }
                    else
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_UpdateStagingTableDataFromJsonUpload_Error", ValidationItemType.Error,
                           "Cannot read correct JSON data."));
                    }

                }
                else
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_UpdateStagingTableDataFromJsonUpload_Error", ValidationItemType.Error,
                        "File does not exist."));
                }
            }


            return aOperationCallResult;
        }


        public static OperationCallResult<AppIntergrationSettingParameterExDto> UpdateStagingTableDataFromJsonFilePath(int? importSettingId, string jsonFilePath, string ftpUserName = "", string ftpPassword = "")
        {
            OperationCallResult<AppIntergrationSettingParameterExDto> aOperationCallResult = new OperationCallResult<AppIntergrationSettingParameterExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (!string.IsNullOrWhiteSpace(jsonFilePath) && importSettingId.HasValue)
            {
                string jsonString = "";

                byte[] buffer = null;

                if (jsonFilePath.ToLower().StartsWith("http"))
                {
                    string errorMsg = "";
                    buffer = AppEsiteFileBL.DownloadFileByUrl(jsonFilePath, out errorMsg);

                    if (!string.IsNullOrWhiteSpace(errorMsg))
                    {
                        errorMsg = "Invalid import file url. " + jsonFilePath;
                        aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Condition pass failure", ValidationItemType.Error, errorMsg));
                    }
                }
                else if (jsonFilePath.ToLower().StartsWith("ftp"))
                {
                    FtpTools ftpInstance = new FtpTools("", ftpUserName, ftpPassword);
                    string errorMsg = "";
                    buffer = ftpInstance.GetFtpFileBinaryData(jsonFilePath, out errorMsg);

                    if (!string.IsNullOrWhiteSpace(errorMsg))
                    {
                        errorMsg = "Cannot Read File Content: " + jsonFilePath + "\n" + errorMsg;
                        aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Condition pass failure", ValidationItemType.Error, errorMsg));
                    }
                }
                else
                {
                    buffer = StreamHelper.FileToByteArray(jsonFilePath);

                }

                if (buffer != null)
                {
                    jsonString = System.Text.Encoding.UTF8.GetString(buffer);
                }

                if (!string.IsNullOrWhiteSpace(jsonString))
                {
                    try
                    {
                        AppIntergrationSettingParameterExDto importSettingDto = DataExchangeSettingBL.GetSetting(importSettingId.Value);

                        JsonSchemaBL.ImportJsonToStagingTable(importSettingDto, jsonString);

                        aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_IntergrationSettingEntity_Save_OK", ValidationItemType.Message, "Execution Completed"));
                    }
                    catch (Exception ex)
                    {

                        string errorMsg = GenerateStagingTableDataUpdateExceptionUserErrorMessage(ex);

                        aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Condition pass failure", ValidationItemType.Error, errorMsg, true, ex.ToString()));
                    }
                }
                else
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_UpdateStagingTableDataFromJsonUpload_Error", ValidationItemType.Error,
                       "Cannot read correct JSON data."));
                }
            }


            return aOperationCallResult;
        }



        public static OperationCallResult<AppIntergrationSettingParameterExDto> UpdateJsonSchemaFromJsonUpload(int? importSettingId, int? jsonFileId)
        {
            OperationCallResult<AppIntergrationSettingParameterExDto> aOperationCallResult = new OperationCallResult<AppIntergrationSettingParameterExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (jsonFileId.HasValue && importSettingId.HasValue)
            {
                AppIntergrationSettingParameterExDto orgImportSettingDto = DataExchangeSettingBL.GetSetting(importSettingId.Value);

                var fileExDto = AppFileBL.RetrieveOneLatestAppFileExDto(jsonFileId.Value);
                if (fileExDto != null)
                {
                    string newSampleJsonString = "";

                    if (!string.IsNullOrEmpty(fileExDto.OriginalFilePath))
                    {
                        string fileFullPathName = AppCompanyBL.GetMyCompanyImagePath() + fileExDto.OriginalFilePath;

                        byte[] buffer = StreamHelper.FileToByteArray(fileFullPathName);

                        if (buffer != null)
                        {
                            newSampleJsonString = System.Text.Encoding.UTF8.GetString(buffer);
                        }


                    }

                    if (!string.IsNullOrWhiteSpace(newSampleJsonString))
                    {
                        try
                        {




                            string orgJsonSchema = orgImportSettingDto.JsonSchema;
                            string newJsonSchema = JsonSchemaBL.GenerateSchemaFromSampleJson(newSampleJsonString);

                            JsonSchema orgSchema = JsonSchema.FromJsonAsync(orgJsonSchema).Result;
                            JsonSchema newSchema = JsonSchema.FromJsonAsync(newJsonSchema).Result;
                            MergeJsonSchema(orgSchema, newSchema);

                            string mergedJsonSchema = orgSchema.ToJson();

                            if (!string.IsNullOrWhiteSpace(mergedJsonSchema))
                            {
                                JsonSchema mergedSchemaDataSetMapping = JsonSchema.FromJsonAsync(mergedJsonSchema).Result;

                                TSqlGeneratorSettings settings = new TSqlGeneratorSettings();

                                settings.CreateUserDefinesTemplate(mergedSchemaDataSetMapping);

                                AppIntergrationSchemaDataSetMappingDto newMappingDto = AppIntergrationSettingBL.ConvertOriginalSchemaDataSetMappingJsonStringToMappingDto(mergedSchemaDataSetMapping.ToUserDefineJson());

                                if (newMappingDto != null)
                                {
                                    orgImportSettingDto.JsonSampleData = newSampleJsonString;
                                    orgImportSettingDto.JsonSchema = mergedJsonSchema;

                                    AppIntergrationSchemaDataSetMappingDto orgMappingDto = orgImportSettingDto.SchemaDataSetMappingDto;

                                    orgMappingDto.OriginalSchemaDataSetMappingJsonString = newMappingDto.OriginalSchemaDataSetMappingJsonString;

                                    orgMappingDto.NodeTypeNameList.AddRange(newMappingDto.NodeTypeNameList);
                                    orgMappingDto.NodeTypeNameList = orgMappingDto.NodeTypeNameList.Distinct().ToList();


                                    var orgHierachyNodeNameList = orgMappingDto.HierachyNodeNameList;
                                    var newHierachyNodeNameList = newMappingDto.HierachyNodeNameList;
                                    MergeNewAndOrgHierachyNodeNameList(orgHierachyNodeNameList, newHierachyNodeNameList);
                                    MergeNewAndOrgNodeSettingDtoList(newMappingDto, orgMappingDto);

                                    string mappindgDtoJsonString = Newtonsoft.Json.JsonConvert.SerializeObject(orgMappingDto);

                                    aValidationResult.Merge(SaveAppIntergrationSettingParameterExDto(orgImportSettingDto).ValidationResult);
                                }
                                else
                                {
                                    aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_UpdateStagingTableDataFromJsonUpload_Error", ValidationItemType.Error,
                                   "Cannot read correct JSON data."));
                                }
                            }
                            else
                            {
                                aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_UpdateStagingTableDataFromJsonUpload_Error", ValidationItemType.Error,
                               "Cannot read correct JSON data."));
                            }


                        }
                        catch (Exception ex)
                        {
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity), "App_AppIntergrationSettingParameterEntity_BLValidation_Error", ValidationItemType.Error, "Update failed. " + ex.ToString()));
                        }



                        if (!aValidationResult.HasErrors)
                        {
                            aOperationCallResult.Object = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(importSettingId.Value);
                        }

                    }
                    else
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_UpdateStagingTableDataFromJsonUpload_Error", ValidationItemType.Error,
                           "Cannot read correct JSON data."));
                    }

                }
                else
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_UpdateStagingTableDataFromJsonUpload_Error", ValidationItemType.Error,
                        "File does not exist."));
                }
            }


            return aOperationCallResult;
        }


        private static void MergeNewAndOrgNodeSettingDtoList(AppIntergrationSchemaDataSetMappingDto newMappingDto, AppIntergrationSchemaDataSetMappingDto orgMappingDto)
        {
            foreach (var newNodeSettingDto in newMappingDto.NodeSettingDtoList)
            {
                var orgNodeSettingDto = orgMappingDto.NodeSettingDtoList.FirstOrDefault(o => o.NodeName == newNodeSettingDto.NodeName);

                if (orgNodeSettingDto != null)
                {
                    foreach (var newProperty in newNodeSettingDto.Properties)
                    {
                        var orgProperty = orgNodeSettingDto.Properties.FirstOrDefault(o => o.PropertyName == newProperty.PropertyName);

                        if (orgProperty != null)
                        {
                            if (!string.IsNullOrWhiteSpace(newProperty.Type) && newProperty.Type != "None")
                            {
                                orgProperty.Type = newProperty.Type;

                                if (!string.IsNullOrWhiteSpace(newProperty.OverwirtType))
                                {
                                    orgProperty.OverwirtType = newProperty.OverwirtType;
                                }

                            }
                        }
                        else
                        {
                            orgNodeSettingDto.Properties.Add(newProperty);
                        }
                    }
                }
                else
                {
                    orgMappingDto.NodeSettingDtoList.Add(newNodeSettingDto);
                }
            }
        }


        public static OperationCallResult<AppIntergrationSettingParameterExDto> CreateStatingTableImportSettingFromApiOperation(int? apiOperationId, bool isImportToExistingTable)
        {
            OperationCallResult<AppIntergrationSettingParameterExDto> aOperationCallResult = new OperationCallResult<AppIntergrationSettingParameterExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (apiOperationId.HasValue)
            {
                var parentApiDto = RetrieveOneAppIntergrationSettingParameterExDto(apiOperationId.Value);




                AppIntergrationSettingParameterExDto newImportSettingDto = new AppIntergrationSettingParameterExDto();
                newImportSettingDto.IntergrationSettingId = parentApiDto.IntergrationSettingId;
                newImportSettingDto.MappingInternalCode = EmAppIntergrationSettingParameterUsageType.ApiTableImportSetting.ToString();
                newImportSettingDto.HttpMethd = parentApiDto.HttpMethd;
                newImportSettingDto.DataSourceId = parentApiDto.DataSourceId;
                newImportSettingDto.ActionCode = parentApiDto.ActionCode + " ImportSetting";
                newImportSettingDto.APIConfigParameters = null;
                newImportSettingDto.ApiconfigParameters = "";
                newImportSettingDto.JsonSampleData = parentApiDto.JsonSampleData;
                newImportSettingDto.PostResponseDto = parentApiDto.PostResponseDto;
                newImportSettingDto.OtherSettingsDto = new AppIntergrationParameterOtherSettingsDto();
                newImportSettingDto.OtherSettingsDto.IsNeedToGenerateStagingTables = !isImportToExistingTable;
                newImportSettingDto.OtherSettingsDto.ParentOperationId = apiOperationId.Value;

                var generateResult = DataExchangeSettingBL.GenerateDefaultSchemaAndDataSetMappingFromSampleJson(newImportSettingDto);

                if (generateResult.IsSuccessfulWithResult)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_CreateJsonFileDatabaseTableImportSettingByFileId_saveOk", ValidationItemType.Message,
                        "Create Import Setting Success."));
                    aOperationCallResult.Object = generateResult.Object;
                }
                else
                {
                    aValidationResult.Merge(generateResult.ValidationResult);
                }




            }


            return aOperationCallResult;
        }

        public static string GetBaseUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                return url;
            }

            // Construct base URL with scheme and authority
            string baseUrl = uri.GetLeftPart(UriPartial.Authority);
            return baseUrl;
        }

        public static OperationCallResult<AppIntergrationSettingExDto> SaveAppIntergrationSettingExDto(AppIntergrationSettingExDto aAppIntergrationSettingExDto)
        {
            OperationCallResult<AppIntergrationSettingExDto> aOperationCallResult = new OperationCallResult<AppIntergrationSettingExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppIntergrationSettingEntity aAppIntergrationSettingEntity;

            if (aAppIntergrationSettingExDto.OtherSettingsDto != null && aAppIntergrationSettingExDto.OtherSettingsDto.DictEnvironmentVariable != null)
            {
                if (aAppIntergrationSettingExDto.OtherSettingsDto.DictEnvironmentVariable.ContainsKey(EmAppApiSystemEnvironmentVariable.BaseUrl.ToString()))
                {
                    string value = aAppIntergrationSettingExDto.OtherSettingsDto.DictEnvironmentVariable[EmAppApiSystemEnvironmentVariable.BaseUrl.ToString()];

                    string baseUrl = GetBaseUrl(value);

                    if (value != baseUrl)
                    {
                        aAppIntergrationSettingExDto.OtherSettingsDto.DictEnvironmentVariable[EmAppApiSystemEnvironmentVariable.BaseUrl.ToString()] = baseUrl;
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_IntergrationSettingEntity_Warning", ValidationItemType.Warning, "Base URL has been fixed."));
                    }
                }
            }


            // prepare Data
            if (aAppIntergrationSettingExDto.IsNew)
            {
                aAppIntergrationSettingEntity = new AppIntergrationSettingEntity();
                AppIntergrationSettingConverter.CopyDtoToEntity(aAppIntergrationSettingEntity, aAppIntergrationSettingExDto);



                //foreach (var templatefieldDto in aAppIntergrationSettingExDto.AppIntergrationSettingParameterList)
                //{
                //    AppIntergrationSettingParameterEntity aAppIntergrationSettingParameterEntity = new AppIntergrationSettingParameterEntity();
                //    AppIntergrationSettingParameterConverter.CopyDtoToEntity(aAppIntergrationSettingParameterEntity, templatefieldDto);
                //    aAppIntergrationSettingEntity.AppIntergrationSettingParameter.Add(aAppIntergrationSettingParameterEntity);
                //}
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppIntergrationSettingEntity);
                        adapter.Commit();

                        aAppIntergrationSettingExDto.Id = aAppIntergrationSettingEntity.IntergrationSettingId;
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_IntergrationSettingEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }

                    // Entity Logical Validation Exception
                    catch (ORMEntityValidationException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_IntergrationSettingEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                    }

                    // Database FK Exeption ........
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_IntergrationSettingEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }

            else if (aAppIntergrationSettingExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(ProcessDirtyAppIntergrationSettingExDto(aAppIntergrationSettingExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppIntergrationSettingExDto(aAppIntergrationSettingExDto.Id);
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<object> DeleteOneAppIntergrationSetting(object IntergrationSettingId)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.DeleteEntitiesDirectly(typeof(AppIntergrationSettingParameterEntity), new RelationPredicateBucket(AppIntergrationSettingParameterFields.IntergrationSettingId == IntergrationSettingId));
                    adapter.DeleteEntitiesDirectly(typeof(AppIntergrationSettingEntity), new RelationPredicateBucket(AppIntergrationSettingFields.IntergrationSettingId == IntergrationSettingId));
                    string message = StringLocalizer.Localize(App_IntergrationSettingEntity_Delete_Ok, "IntergrationSetting Delete Successful");
                    aValidationResult.Items.Add(new ValidationItem(null, App_IntergrationSettingEntity_Delete_Ok, ValidationItemType.Message, message));

                    adapter.Commit();
                }

                // FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    string message = StringLocalizer.Localize(App_IntergrationSettingEntity_Delete_Failed, "IntergrationSetting Delete Failed" + ex.ToString());
                    aValidationResult.Items.Add(new ValidationItem(null, App_IntergrationSettingEntity_Delete_Failed, ValidationItemType.Error, message));
                }
            }

            // if no any errors
            if (!aOperationCallResult.ValidationResult.HasErrors)
            {
                aOperationCallResult.Object = IntergrationSettingId;
            }

            return aOperationCallResult;
        }

        public static EntityCollection<AppIntergrationSettingParameterEntity> RetrieveAllAppIntergrationSettingParameterSimpleEntityList(string usageType = null)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppIntergrationSettingParameterEntity> list = new EntityCollection<AppIntergrationSettingParameterEntity>();

                ExcludeFieldsList excludeFieldsList = new ExcludeFieldsList();
                excludeFieldsList.Add(AppIntergrationSettingParameterFields.ApiconfigParameters);
                excludeFieldsList.Add(AppIntergrationSettingParameterFields.JsonQuery);
                excludeFieldsList.Add(AppIntergrationSettingParameterFields.JsonSampleData);
                excludeFieldsList.Add(AppIntergrationSettingParameterFields.JsonSchema);
                //excludeFieldsList.Add(AppIntergrationSettingParameterFields.SchemaDataSetMapping);
                excludeFieldsList.Add(AppIntergrationSettingParameterFields.SchemaFromDataSetMapping);
                excludeFieldsList.Add(AppIntergrationSettingParameterFields.PostProcessScript);

                RelationPredicateBucket filter = new RelationPredicateBucket();

                if (!string.IsNullOrWhiteSpace(usageType))
                {
                    if (usageType == EmAppIntergrationSettingParameterUsageType.ApiOperation.ToString())
                    {
                        filter = new RelationPredicateBucket(AppIntergrationSettingParameterFields.MappingInternalCode == DBNull.Value
                            | AppIntergrationSettingParameterFields.MappingInternalCode == ""
                            | AppIntergrationSettingParameterFields.MappingInternalCode == usageType);
                    }
                    else
                    {
                        filter = new RelationPredicateBucket(AppIntergrationSettingParameterFields.MappingInternalCode == usageType);
                    }

                }



                adpater.FetchEntityCollection(list, excludeFieldsList, filter);

                return list;
            }
        }

        public static AppIntergrationSettingParameterEntity RetrieveOneAppIntergrationSettingParameterEntity(object settingParameterId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppIntergrationSettingParameterEntity IntergrationSettingEntity = new AppIntergrationSettingParameterEntity(int.Parse(settingParameterId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppIntergrationSettingParameterEntity);

                adpater.FetchEntity(IntergrationSettingEntity, rootPath);
                return IntergrationSettingEntity;
            }
        }


        public static List<ApiDataStructureNodeDto> RetrieveOneApiAvailableFetchDataNodeStructure(object settingParameterId, string rootNodeFixedName)
        {
            var apiOperationDto = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(settingParameterId);

            var rootNodeDto = AppTransactionBL.PrepareApiDataStrucureFromJsonSchema(apiOperationDto.JsonSchema, false, false);

            //if (rootNodeDto.Children.Count == 1 && rootNodeDto.Children[0].IsArray && !rootNodeDto.HasSimpleProperties)
            //{
            //    rootNodeDto = rootNodeDto.Children[0];
            //    rootNodeDto.Display = "[ ]";
            //}
            ApiDataStructureNodeDto parentNode = null;

            if (!string.IsNullOrWhiteSpace(rootNodeFixedName))
            {
                parentNode = new ApiDataStructureNodeDto();
                rootNodeDto.Name = rootNodeFixedName;
                rootNodeDto.Display = rootNodeFixedName;
            }

            AppIntergrationSettingBL.InitApiNodeAbolutePath(rootNodeDto, parentNode);

            var apiAvailableFetcheDataJsonNodeStructure = new List<ApiDataStructureNodeDto>() { rootNodeDto };

            return apiAvailableFetcheDataJsonNodeStructure;
        }



        public static AppIntergrationSettingParameterExDto RetrieveOneAppIntergrationSettingParameterExDto(object settingParameterId, bool isInlucdeApiDataStructure = false)
        {
            AppIntergrationSettingParameterEntity settingParameterEntity = RetrieveOneAppIntergrationSettingParameterEntity(settingParameterId);
            AppIntergrationSettingParameterExDto dto = AppIntergrationSettingParameterConverter.ConvertEntityToExDto(settingParameterEntity);



            if (dto.APIConfigParameters != null && dto.APIConfigParameters.ExcelDataImportDataSetId.HasValue)
            {

                dto.ImportDataSetDto = AppDatabaseErDiagramBL.RetrieveOneErDiagramExDto(dto.APIConfigParameters.ExcelDataImportDataSetId.Value);
            }

            if (dto.MappingInternalCode == EmAppIntergrationSettingParameterUsageType.ApiTableImportSetting.ToString())
            {
                dto.ApiconfigParameters = "";
                dto.APIConfigParameters = null;
                dto.HttpMethd = "";
                dto.JsonSampleData = "";

                if (dto.OtherSettingsDto != null && dto.OtherSettingsDto.ParentOperationId.HasValue)
                {
                    var parentDto = RetrieveOneAppIntergrationSettingParameterExDto(dto.OtherSettingsDto.ParentOperationId.Value);

                    dto.ApiconfigParameters = parentDto.ApiconfigParameters;
                    dto.APIConfigParameters = parentDto.APIConfigParameters;
                    dto.HttpMethd = parentDto.HttpMethd;
                    dto.JsonSampleData = parentDto.JsonSampleData;

                    dto.ParentOperationName = parentDto.ActionCode;
                }

            }


            PrepareAppIntergrationSettingParameterInheriedProperties(dto);


            if (dto.DataSourceId.HasValue)
            {
                var datasourceRegDto = AppDataSourceRegisterBL.RetrieveOneAppDataSourceRegisterExDto(dto.DataSourceId.Value);
                dto.DataSourceType = datasourceRegDto.DataSourceType;
            }


            if (isInlucdeApiDataStructure)
            {

                if (!string.IsNullOrWhiteSpace(dto.JsonSchema))
                {
                    var rootNodeDto = AppTransactionBL.PrepareApiDataStrucureFromJsonSchema(dto.JsonSchema);

                    if (rootNodeDto.Children.Count == 1 && rootNodeDto.Children[0].IsObject)
                    {
                        rootNodeDto = rootNodeDto.Children[0];
                        rootNodeDto.Display = "{ }";
                    }
                    if (rootNodeDto.Children.Count == 1 && rootNodeDto.Children[0].IsArray)
                    {
                        rootNodeDto = rootNodeDto.Children[0];
                        rootNodeDto.Display = "[ ]";
                    }

                    AppIntergrationSettingBL.InitApiNodeAbolutePath(rootNodeDto, null);
                    dto.ApiDataStructure = new List<ApiDataStructureNodeDto>() { rootNodeDto };

                }
            }


            return dto;
        }


        public static AppIntergrationSettingParameterExDto GetAppSearchDefaultProviderApi(int searchId, bool isInlucdeApiDataStructure, string appBaseUrl)
        {
            AppIntergrationSettingParameterExDto toReturn = null;


            List<SearchApiSettingDto> searchApiSettingDtoList = AppSearchConfigBL.RetrieveSearchApiSettings(searchId);
            SearchApiSettingDto apiSetting = searchApiSettingDtoList.FirstOrDefault(o => o.OperationId.HasValue);

            if (apiSetting != null)
            {
                toReturn = PrepareExistingGetApiWithDataSchema(isInlucdeApiDataStructure, appBaseUrl, searchApiSettingDtoList[0].OperationId.Value);
            }
            else
            {
                OperationCallResult<AppIntergrationSettingParameterExDto> result = GenerateAppSearchProviderApi(searchId, appBaseUrl);

                if (result.IsSuccessfulWithResult)
                {
                    toReturn = RetrieveOneAppIntergrationSettingParameterExDto(result.Object.Id, isInlucdeApiDataStructure);
                }
            }

            return toReturn;
        }


        public static List<AppIntergrationSettingParameterExDto> GetAppTransactionDefaultProviderApi(int transactionId, bool isInlucdeApiDataStructure, string appBaseUrl)
        {
            List<AppIntergrationSettingParameterExDto> toReturn = new List<AppIntergrationSettingParameterExDto>();
            
            List<TransactionApiSettingDto> transactionApiSettingDtoList = AppTransactionBL.RetrieveTransactionApiSettings(transactionId);

            TransactionApiSettingDto apiGetSetting = transactionApiSettingDtoList.FirstOrDefault(o => !o.IsSimpleQuery && o.HttpMethd == "Get" && o.OperationId.HasValue);
            TransactionApiSettingDto apiPostSetting = transactionApiSettingDtoList.FirstOrDefault(o => !o.IsSimpleQuery && o.HttpMethd == "Post" && o.OperationId.HasValue);

            AppIntergrationSettingParameterExDto getApiDto = null;
            AppIntergrationSettingParameterExDto postApiDto = null;

            if (apiGetSetting != null)
            {
                getApiDto = PrepareExistingGetApiWithDataSchema(isInlucdeApiDataStructure, appBaseUrl, apiGetSetting.OperationId.Value);                
            }
            else
            {
                OperationCallResult<AppIntergrationSettingParameterExDto> result = GenerateAppFormProviderGetApi(transactionId, appBaseUrl);

                if (result.IsSuccessfulWithResult)
                {
                    getApiDto = RetrieveOneAppIntergrationSettingParameterExDto(result.Object.Id, isInlucdeApiDataStructure);
                }
            }

            if (apiPostSetting != null)
            {
                postApiDto = RetrieveOneAppIntergrationSettingParameterExDto(apiPostSetting.OperationId.Value, isInlucdeApiDataStructure);
            }
            else
            {
                OperationCallResult<AppIntergrationSettingParameterExDto> result = GenerateAppFormProviderPostApi(transactionId, appBaseUrl);

                if (result.IsSuccessfulWithResult)
                {
                    postApiDto = RetrieveOneAppIntergrationSettingParameterExDto(result.Object.Id, isInlucdeApiDataStructure);
                }
                
            }

            toReturn.Add(getApiDto);
            toReturn.Add(postApiDto);


            return toReturn;
        }

       
        public static string CallGetApiByUrl(string apiUrl, Dictionary<string, string> dictHeaderKeyAndValue)
        {
            using (var client = new HttpClient())
            {
                foreach (var header in dictHeaderKeyAndValue)
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }

                try
                {
                    var response = client.GetStringAsync(apiUrl).GetAwaiter().GetResult(); ;
                  

                    return response;
                }
                catch (HttpRequestException ex)
                {
                    throw new Exception("An error occurred while calling the API.", ex);
                }
            }
        }


        public static AppIntergrationSettingParameterExDto RetrieveOneAppIntergrationSettingParameterExDtoByActionCode(string actionCode)
        {

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppIntergrationSettingParameterEntity> list = new EntityCollection<AppIntergrationSettingParameterEntity>();

                RelationPredicateBucket filter = new RelationPredicateBucket(AppIntergrationSettingParameterFields.ActionCode == actionCode);

                adapter.FetchEntityCollection(list, filter, 0);

                if (list.Count > 0)
                {
                    AppIntergrationSettingParameterExDto dto = AppIntergrationSettingParameterConverter.ConvertEntityToExDto(list.First());

                    PrepareAppIntergrationSettingParameterInheriedProperties(dto);
                    return dto;
                }
                else
                {
                    return null;
                }

            }
        }

        internal static AppIntergrationSettingParameterExDto GetTransactionConsumeBaseApi_ToDelete(int transactionId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppIntergrationSettingParameterEntity> list = new EntityCollection<AppIntergrationSettingParameterEntity>();

                RelationPredicateBucket filter = new RelationPredicateBucket(AppIntergrationSettingParameterFields.IntergrationSettingId != 1
                    & AppIntergrationSettingParameterFields.TranscationId == transactionId);


                adapter.FetchEntityCollection(list, filter);

                if (list.Count > 0)
                {
                    AppIntergrationSettingParameterExDto operationExDto = AppIntergrationSettingParameterConverter.ConvertEntityToExDto(list.First());
                    PrepareAppIntergrationSettingParameterInheriedProperties(operationExDto);
                    return operationExDto;
                }
            }

            return null;
        }




        public static OperationCallResult<AppIntergrationSettingParameterExDto> BuildJsonImportTableDiagramFromSetting(AppIntergrationSettingParameterExDto aAppIntergrationSettingParameterExDto)
        {
            OperationCallResult<AppIntergrationSettingParameterExDto> aOperationCallResult = new OperationCallResult<AppIntergrationSettingParameterExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (aAppIntergrationSettingParameterExDto != null && aAppIntergrationSettingParameterExDto.SchemaDataSetMappingDto != null)
            {
                var mappingDto = aAppIntergrationSettingParameterExDto.SchemaDataSetMappingDto;

                mappingDto.ImportTableDiagram = new DatabaseViewDto()
                {
                    //IsErDiagram = true,
                    DictAllColumns = new Dictionary<string, Dictionary<string, bool>>(),
                    DictTables = new Dictionary<string, DatabaseViewTableDto>(StringComparer.OrdinalIgnoreCase),
                    Joins = new List<DatabaseViewJoinDto>(),
                    SelectedColumnsList = new List<DatabaseViewColumnDto>(),
                    WhereConditionFilterColumns = new List<DatabaseViewColumnDto>(),
                    QueryString = "",
                    DataSourceRegisterId = aAppIntergrationSettingParameterExDto.DataSourceId
                };

                DatabaseViewDto diagramDto = mappingDto.ImportTableDiagram;


                if (mappingDto.NodeSettingDtoList != null && mappingDto.HierachyNodeNameList != null)
                {
                    var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(aAppIntergrationSettingParameterExDto.DataSourceId.Value);
                    string schemaOwner = dataBaseFixture.CurrentOwner;

                    Dictionary<string, SchemaDataSetMappingDefinitionDto> dictNodeNameAndDto = mappingDto.NodeSettingDtoList.ToDictionary(o => o.NodeName, o => o);

                    int nodeLevel = 1;

                    foreach (var simpleHierachyNode in mappingDto.HierachyNodeNameList)
                    {
                        ConvertOneNodeToDiagramTable(aAppIntergrationSettingParameterExDto, diagramDto, schemaOwner, dictNodeNameAndDto, nodeLevel, simpleHierachyNode);
                    }
                }
            }


            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = aAppIntergrationSettingParameterExDto;
            }

            return aOperationCallResult;
        }



        public static OperationCallResult<AppIntergrationSettingParameterExDto> SaveAppIntergrationSettingParameterExDto(AppIntergrationSettingParameterExDto aAppIntergrationSettingParameterExDto)
        {
            OperationCallResult<AppIntergrationSettingParameterExDto> aOperationCallResult = new OperationCallResult<AppIntergrationSettingParameterExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;


            //if (aAppIntergrationSettingParameterExDto.IntergrationSettingId == 1 
            //    && aAppIntergrationSettingParameterExDto.TranscationId.HasValue
            //    && (aAppIntergrationSettingParameterExDto.HttpMethd == "Post" || aAppIntergrationSettingParameterExDto.HttpMethd == "Put"))
            //{ 

            //}




            AppIntergrationSettingParameterEntity aAppIntergrationSettingParameterEntity;

            // prepare Data
            if (aAppIntergrationSettingParameterExDto.IsNew)
            {
                string actionCode = aAppIntergrationSettingParameterExDto.ActionCode;

                actionCode = Regex.Replace(actionCode, @"[^0-9a-zA-Z]+", "_");

                List<string> existActionCodeList = RetrieveAllAppIntergrationSettingParameterSimpleEntityList(EmAppIntergrationSettingParameterUsageType.ApiOperation.ToString())
                    .Select(o => o.ActionCode.ToLower()).ToList();

                int renameCount = 0;

                while (existActionCodeList.Contains(actionCode.ToLower()))
                {
                    renameCount++;
                    actionCode = aAppIntergrationSettingParameterExDto.ActionCode + renameCount;
                }

                aAppIntergrationSettingParameterExDto.ActionCode = actionCode;

                aAppIntergrationSettingParameterEntity = new AppIntergrationSettingParameterEntity();
                AppIntergrationSettingParameterConverter.CopyDtoToEntity(aAppIntergrationSettingParameterEntity, aAppIntergrationSettingParameterExDto);


                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppIntergrationSettingParameterEntity);
                        adapter.Commit();

                        aAppIntergrationSettingParameterExDto.Id = aAppIntergrationSettingParameterEntity.SettingParameterId;
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity), "App_IntergrationSettingEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }

                    // Entity Logical Validation Exception
                    catch (ORMEntityValidationException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity), "App_IntergrationSettingEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                    }

                    // Database FK Exeption ........
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity), "App_IntergrationSettingEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }

            else if (aAppIntergrationSettingParameterExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(ProcessDirtyAppIntergrationSettingParameterExDto(aAppIntergrationSettingParameterExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppIntergrationSettingParameterExDto(aAppIntergrationSettingParameterExDto.Id);
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<object> DeleteOneAppIntergrationSettingParameter(object settingParameterId)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");

                    adapter.DeleteEntitiesDirectly(typeof(AppIntergrationSettingParameterEntity), new RelationPredicateBucket(AppIntergrationSettingParameterFields.SettingParameterId == (int)settingParameterId));
                    string message = StringLocalizer.Localize(App_IntergrationSettingEntity_Delete_Ok, "Intergration Setting Parameter Delete Successful");
                    aValidationResult.Items.Add(new ValidationItem(null, App_IntergrationSettingEntity_Delete_Ok, ValidationItemType.Message, message));

                    adapter.Commit();
                }

                // FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    string message = StringLocalizer.Localize(App_IntergrationSettingEntity_Delete_Failed, "Intergration Setting Parameter Delete Failed" + ex.ToString());
                    aValidationResult.Items.Add(new ValidationItem(null, App_IntergrationSettingEntity_Delete_Failed, ValidationItemType.Error, message));
                }
            }

            // if no any errors
            if (!aOperationCallResult.ValidationResult.HasErrors)
            {
                aOperationCallResult.Object = settingParameterId;
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<bool> ExecuteOneOperationWithTestParameters(int? settingParameterId, bool isSimulate = false)
        {
            OperationCallResult<bool> toReturn = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            toReturn.ValidationResult = aValidationResult;

            if (settingParameterId.HasValue)
            {
                AppIntergrationSettingParameterExDto operationDto = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(settingParameterId.Value);
                if (operationDto.HttpMethd == "Get")
                {
                    try
                    {
                        JsonSchemaBL.FromAPIToDatabaseByParametersAsync(settingParameterId.Value, null, null, null, isSimulate).Wait();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_IntergrationSettingEntity_Save_OK", ValidationItemType.Message, "Execution Completed"));
                    }
                    catch (Exception ex)
                    {
                        string errorMsg = GenerateStagingTableDataUpdateExceptionUserErrorMessage(ex);

                        toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Condition pass failure", ValidationItemType.Error, errorMsg, true, ex.ToString()));
                    }

                }
                else if (operationDto.HttpMethd == "Post" || operationDto.HttpMethd == "Put")
                {

                    int? internalKeyId = null;

                    if (internalKeyId.HasValue)
                    {
                        Dictionary<string, string> queryParams = new Dictionary<string, string>();
                        Dictionary<string, string> pathParams = new Dictionary<string, string>();

                        try
                        {
                            JsonSchemaBL.FromDatabaseToAPIAsync(settingParameterId.Value, null, queryParams, pathParams, internalKeyId.Value, isSimulate).Wait();
                        }
                        catch (Exception ex)
                        {
                            toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Condition pass failure", ValidationItemType.Error, ex.ToString()));
                        }

                    }
                }
            }

            return toReturn;

        }



        public static OperationCallResult<bool> ExecuteDataImportOnJsonFileTableImportSetting(int? settingParameterId)
        {
            OperationCallResult<bool> toReturn = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            toReturn.ValidationResult = aValidationResult;

            if (settingParameterId.HasValue)
            {
                AppIntergrationSettingParameterExDto operationDto = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(settingParameterId.Value);

                try
                {
                    JsonSchemaBL.FromJsonToDatabaseByParameters(settingParameterId.Value);
                    //  actionResult.Wait();
                    // bool boolResult = actionResult.Result;
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_IntergrationSettingEntity_Save_OK", ValidationItemType.Message, "Execution Completed"));
                }
                catch (Exception ex)
                {

                    string errorMsg = GenerateStagingTableDataUpdateExceptionUserErrorMessage(ex);

                    toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Condition pass failure", ValidationItemType.Error, errorMsg, true, ex.ToString()));
                }


            }

            return toReturn;

        }

        public static AppIntergrationSchemaDataSetMappingDto ConvertOriginalSchemaDataSetMappingJsonStringToMappingDto(string dataSetMappingJsonString)
        {
            if (!string.IsNullOrWhiteSpace(dataSetMappingJsonString))
            {

                ApiDataStructureNodeDto rootNodeDto = AppTransactionBL.PrepareApiDataStrucureFromJsonSchema(dataSetMappingJsonString, false);

                AppIntergrationSchemaDataSetMappingDto schemaDataSetMappingDto = new AppIntergrationSchemaDataSetMappingDto();
                schemaDataSetMappingDto.OriginalSchemaDataSetMappingJsonString = dataSetMappingJsonString;

                schemaDataSetMappingDto.NodeSettingDtoList = new List<SchemaDataSetMappingDefinitionDto>();

                JsonSchema mappingSchema = JsonSchema.FromJsonAsync(dataSetMappingJsonString).Result;
                TSqlGeneratorSettings tSqlSetting = new TSqlGeneratorSettings();

                Dictionary<string, AppSefolderDto> dictNodeNameAndDto = new Dictionary<string, AppSefolderDto>();

                schemaDataSetMappingDto.NodeTypeNameList = mappingSchema.Definitions.Keys.ToList();
                schemaDataSetMappingDto.NodeTypeNameList.Add("anonymous");



                int nodeCount = 0;
                foreach (string definitionName in mappingSchema.Definitions.Keys)
                {
                    nodeCount++;
                    JsonSchema definitionSchema = mappingSchema.Definitions[definitionName];


                    SchemaDataSetMappingDefinitionDto nodeSettingDto = new SchemaDataSetMappingDefinitionDto()
                    {
                        NodeName = definitionName,
                        Type = definitionSchema.Type.ToString(),
                        Properties = new List<SchemaDataSetMappingDefinitionPropertyDto>(),
                        ProcessMode = (int)EmAppSchemaDataSetMappingNodeProcessMode.Ignore,
                        //IsCreateTable = true,
                    };

                    foreach (string propertyName in definitionSchema.Properties.Keys)
                    {
                        JsonSchemaProperty schemaProperty = definitionSchema.Properties[propertyName];
                        SchemaDataSetMappingDefinitionPropertyDto propertyDto = new SchemaDataSetMappingDefinitionPropertyDto();
                        propertyDto.PropertyName = propertyName;
                        propertyDto.Type = schemaProperty.Type.ToString();
                        propertyDto.OverwirtType = schemaProperty.Format;
                        propertyDto.IsCreateColumn = true;

                        if (schemaProperty.Type == JsonObjectType.Array || schemaProperty.Type == JsonObjectType.Object || schemaProperty.Type == JsonObjectType.None)
                        {
                            string refToDefinitionName = definitionName + tSqlSetting.MappingSplit + schemaProperty.Name;

                            if (mappingSchema.Definitions.ContainsKey(refToDefinitionName))
                            {
                                propertyDto.RefToDefinitionName = refToDefinitionName;
                            }
                        }
                        else if (schemaProperty.Type == JsonObjectType.String)
                        {
                            propertyDto.OverwirtType = "max";
                        }

                        nodeSettingDto.Properties.Add(propertyDto);
                    }


                    schemaDataSetMappingDto.NodeSettingDtoList.Add(nodeSettingDto);
                }

                if (mappingSchema.UserDefines != null)
                {
                    if (mappingSchema.UserDefines.ExtensionData.ContainsKey("root_type_name"))
                    {
                        schemaDataSetMappingDto.RootNodeName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(mappingSchema.UserDefines.ExtensionData["root_type_name"]);
                    }

                    //if (mappingSchema.UserDefines.ExtensionData.ContainsKey("rollup_child_to_parent_for_one_table_parent_node_list"))
                    //{
                    //    string stringValue_rollupChild = Newtonsoft.Json.JsonConvert.SerializeObject(mappingSchema.UserDefines.ExtensionData["rollup_child_to_parent_for_one_table_parent_node_list"]);
                    //    List<string> rollupChildNodeNameList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(stringValue_rollupChild);

                    //    foreach (string nodeName in rollupChildNodeNameList)
                    //    {
                    //        var nodeDto = schemaDataSetMappingDto.NodeSettingDtoList.FirstOrDefault(o => o.NodeName == nodeName);
                    //        if (nodeDto != null)
                    //        {
                    //            nodeDto.IsNeedToRollUpAllChild = true;
                    //        }
                    //    }
                    //}

                    if (mappingSchema.UserDefines.ExtensionData.ContainsKey("object_type_map_to_table_option"))
                    {
                        var tableOptoins = mappingSchema.UserDefines.ExtensionData["object_type_map_to_table_option"];



                        string stringValue_tableOptoin = Newtonsoft.Json.JsonConvert.SerializeObject(tableOptoins);
                        List<Dictionary<string, object>> lstMapping = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(stringValue_tableOptoin);

                        foreach (Dictionary<string, object> tableMapping in lstMapping)
                        {
                            if (tableMapping.ContainsKey("object_type_definition_name"))
                            {
                                string definitionName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(tableMapping["object_type_definition_name"]);

                                var nodeDto = schemaDataSetMappingDto.NodeSettingDtoList.FirstOrDefault(o => o.NodeName == definitionName);

                                if (nodeDto != null)
                                {
                                    if (tableMapping.ContainsKey("mapping_db_table_name"))
                                    {
                                        nodeDto.MappingToTableName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(tableMapping["mapping_db_table_name"]);
                                    }

                                    if (tableMapping.ContainsKey("is_single_field"))
                                    {
                                        bool? isSingleField = ControlTypeValueConverter.ConvertValueToBoolean(tableMapping["is_single_field"]);
                                        nodeDto.IsSingleField = isSingleField.HasValue && isSingleField.Value;
                                    }


                                    if (tableMapping.ContainsKey("key"))
                                    {
                                        string stringValue_key = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(tableMapping["key"]);

                                        if (!string.IsNullOrWhiteSpace(stringValue_key))
                                        {
                                            var propertyDto = nodeDto.Properties.FirstOrDefault(o => o.PropertyName == stringValue_key);

                                            if (propertyDto != null)
                                            {
                                                propertyDto.IsLogicalKey = true;
                                            }
                                        }
                                    }
                                    else if (tableMapping.ContainsKey("unique"))
                                    {
                                        string stringValue_unique = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(tableMapping["unique"]);

                                        if (!string.IsNullOrWhiteSpace(stringValue_unique))
                                        {
                                            List<string> uniqKeys = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(stringValue_unique);

                                            foreach (string uniqKey in uniqKeys)
                                            {
                                                var propertyDto = nodeDto.Properties.FirstOrDefault(o => o.PropertyName == uniqKey);

                                                if (propertyDto != null)
                                                {
                                                    propertyDto.IsLogicalKey = true;
                                                }
                                            }
                                        }
                                    }
                                    else if (tableMapping.ContainsKey("rollup_path"))
                                    {
                                        string stringValue_rollup_path = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(tableMapping["rollup_path"]);

                                        if (!string.IsNullOrWhiteSpace(stringValue_rollup_path))
                                        {
                                            List<string> rollUpChildTypeNames = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(stringValue_rollup_path);

                                            if (rollUpChildTypeNames.Count > 0)
                                            {
                                                nodeDto.IsNeedToRollUpAllChild = true;

                                                foreach (string rollupChildNodeTypeName in rollUpChildTypeNames)
                                                {
                                                    var childNodeDto = schemaDataSetMappingDto.NodeSettingDtoList.FirstOrDefault(o => o.NodeName == rollupChildNodeTypeName);
                                                    if (childNodeDto != null)
                                                    {
                                                        childNodeDto.ProcessMode = (int)EmAppSchemaDataSetMappingNodeProcessMode.RollUpToParent;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }


                //foreach (var rootNode in rootNodes)
                //{
                //    if (string.IsNullOrWhiteSpace(schemaDataSetMappingDto.RootNodeName))
                //    {
                //        schemaDataSetMappingDto.RootNodeName = rootNode.Name;
                //    }

                //    rootNode.FolderPath = rootNode.Name + "/";
                //    AppSeFolderBL.ProcessChilds(dictNodeNameAndDto.Values, rootNode);
                //}



                schemaDataSetMappingDto.HierachyNodeNameList = rootNodeDto.Children;

                var firstRootNode = schemaDataSetMappingDto.HierachyNodeNameList.FirstOrDefault();

                if (firstRootNode != null)
                {
                    schemaDataSetMappingDto.RootNodeName = firstRootNode.SchemaTypeName;
                }

                if (string.IsNullOrWhiteSpace(schemaDataSetMappingDto.RootNodeName))
                {
                    schemaDataSetMappingDto.RootNodeName = "anonymous";
                }

                return schemaDataSetMappingDto;
            }


            return null;
        }

        public static string GenerateSchemaDataSetMappingJsonStringFromMappingDto(AppIntergrationSchemaDataSetMappingDto schemaDataSetMappingDto)
        {
            string dataSetMappingJsonString = "";

            if (schemaDataSetMappingDto != null && !string.IsNullOrWhiteSpace(schemaDataSetMappingDto.OriginalSchemaDataSetMappingJsonString))
            {
                TSqlGeneratorSettings tSqlSetting = new TSqlGeneratorSettings();

                JsonSchema mappingSchema = JsonSchema.FromJsonAsync(schemaDataSetMappingDto.OriginalSchemaDataSetMappingJsonString).Result;

                if (mappingSchema.UserDefines != null && mappingSchema.Definitions != null)
                {

                    //Dictionary<string, SchemaDataSetMappingDefinitionDto> dictMappingNodeNameAndDto = schemaDataSetMappingDto.NodeSettingDtoList.ToDictionary(o => o.NodeName, o => o);



                    List<string> needToRemoveDefinitionNames = schemaDataSetMappingDto.NodeSettingDtoList.Where(o => o.IsRemoved || o.NodeName.StartsWith("Anonymous")).Select(o => o.NodeName).ToList();


                    // 1. Update Definitions

                    schemaDataSetMappingDto.NodeSettingDtoList.ForAll(o =>
                    {
                        o.Properties.ForAll(p =>
                        {
                            p.IsRefToSerializedObj = false;
                        });
                    });


                    foreach (SchemaDataSetMappingDefinitionDto nodeSettingDto in schemaDataSetMappingDto.NodeSettingDtoList)
                    {
                        if (nodeSettingDto.IsNeedToserialize)
                        {

                            //nodeSettingDto.IsCreateTable = false;

                            //schemaDataSetMappingDto.NodeSettingDtoList.Where(o => o.NodeName.StartsWith(nodeSettingDto.NodeName + tSqlSetting.MappingSplit)).ForAll(o => o.IsCreateTable = false);

                            int lastIndexOfSplit = nodeSettingDto.NodeName.LastIndexOf(tSqlSetting.MappingSplit);

                            if (lastIndexOfSplit > 0)
                            {
                                string parentNodeName = nodeSettingDto.NodeName.Substring(0, lastIndexOfSplit);
                                string parentRefPropertyName = nodeSettingDto.NodeName.Substring(lastIndexOfSplit + tSqlSetting.MappingSplit.Length);

                                if (!string.IsNullOrWhiteSpace(parentNodeName) && !string.IsNullOrWhiteSpace(parentRefPropertyName))
                                {
                                    var parentNode = schemaDataSetMappingDto.NodeSettingDtoList.FirstOrDefault(o => o.NodeName == parentNodeName);

                                    if (parentNode != null)
                                    {
                                        var referenceProperty = parentNode.Properties.FirstOrDefault(o => o.RefToDefinitionName == nodeSettingDto.NodeName || o.PropertyName == parentRefPropertyName);

                                        if (referenceProperty != null)
                                        {
                                            referenceProperty.IsRefToSerializedObj = true;
                                            referenceProperty.OverwirtType = "max";
                                        }
                                    }
                                }
                            }
                        }
                    }

                    needToRemoveDefinitionNames = schemaDataSetMappingDto.NodeSettingDtoList.Where(o => o.IsRemoved || o.NodeName.StartsWith("Anonymous")).Select(o => o.NodeName).ToList();


                    foreach (SchemaDataSetMappingDefinitionDto nodeSettingDto in schemaDataSetMappingDto.NodeSettingDtoList)
                    {
                        if (mappingSchema.Definitions.ContainsKey(nodeSettingDto.NodeName))
                        {

                            if (nodeSettingDto.IsRemoved)
                            {
                                //mappingSchema.Definitions.Remove(nodeSettingDto.NodeName);
                                foreach (var propertyDto in nodeSettingDto.Properties)
                                {

                                    if (mappingSchema.Definitions[nodeSettingDto.NodeName].Properties.ContainsKey(propertyDto.PropertyName))
                                    {
                                        if (propertyDto.IsRemoved)
                                        {
                                            mappingSchema.Definitions[nodeSettingDto.NodeName].Properties.Remove(propertyDto.PropertyName);
                                        }
                                        else
                                        {
                                            if (!string.IsNullOrWhiteSpace(propertyDto.OverwirtType))
                                            {
                                                mappingSchema.Definitions[nodeSettingDto.NodeName].Properties[propertyDto.PropertyName].Format = propertyDto.OverwirtType;
                                            }
                                            else
                                            {
                                                mappingSchema.Definitions[nodeSettingDto.NodeName].Properties[propertyDto.PropertyName].Format = "";
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                foreach (var propertyDto in nodeSettingDto.Properties)
                                {
                                    if (propertyDto.Type == "Array")
                                    {
                                        propertyDto.OverwirtType = "max";
                                    }


                                    if (!string.IsNullOrWhiteSpace(propertyDto.RefToDefinitionName))
                                    {
                                        if (propertyDto.Type == "Array" && needToRemoveDefinitionNames.Contains(propertyDto.RefToDefinitionName) && !propertyDto.IsRefToSerializedObj)
                                        {
                                            propertyDto.IsCreateColumn = false;
                                        }
                                        else
                                        {
                                            propertyDto.IsCreateColumn = true;
                                        }
                                    }
                                    else
                                    {
                                        if (propertyDto.Type == "Array")
                                        {
                                            propertyDto.IsCreateColumn = false;
                                        }
                                        //else if (propertyDto.Type == "None")
                                        //{
                                        //    propertyDto.IsCreateColumn = false;
                                        //}
                                    }

                                    if (mappingSchema.Definitions[nodeSettingDto.NodeName].Properties.ContainsKey(propertyDto.PropertyName))
                                    {
                                        if (propertyDto.IsRemoved)
                                        {
                                            mappingSchema.Definitions[nodeSettingDto.NodeName].Properties.Remove(propertyDto.PropertyName);
                                        }
                                        else
                                        {
                                            if (!string.IsNullOrWhiteSpace(propertyDto.OverwirtType))
                                            {
                                                mappingSchema.Definitions[nodeSettingDto.NodeName].Properties[propertyDto.PropertyName].Format = propertyDto.OverwirtType;
                                            }
                                            else
                                            {
                                                mappingSchema.Definitions[nodeSettingDto.NodeName].Properties[propertyDto.PropertyName].Format = "";
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        //    mappingSchema.UserDefines.ExtensionData.TryGetValue(TSqlGeneratorSettings.UserDefineKeyOptions, out object userDefineOptions);

                        //    if (userDefineOptions != null)
                        //    {
                        //        string strTypeSetup = Newtonsoft.Json.JsonConvert.SerializeObject(userDefineOptions);

                        //        List<ObjectTypeMappingDTO> lstMapping = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ObjectTypeMappingDTO>>(strTypeSetup);

                        //        ObjectTypeMappingDTO mapping = lstMapping.FirstOrDefault(m => m.object_type_definition_name.Equals(typeName, StringComparison.InvariantCultureIgnoreCase));

                        //        if (mapping != null && mapping.unique != null)
                        //        {
                        //            return mapping.unique;
                        //        }

                        //        if (mapping == null && typeName == MappingSingleField)
                        //        {
                        //            return new string[] { MappingSingleFieldItemName, MappingPath }; // single field 
                        //        }
                        //    }


                        //    if (mappingSchema.UserDefines.ExtensionData["object_type_map_to_table_option"])
                        //    {
                        //        mappingSchema.UserDefines.Remove(nodeSettingDto.NodeName);
                        //    }

                    }


                    // 2. Update UserDefines
                    if (mappingSchema.UserDefines.ExtensionData.ContainsKey("root_type_name"))
                    {
                        mappingSchema.UserDefines.ExtensionData["root_type_name"] = schemaDataSetMappingDto.RootNodeName;
                    }

                    //if (mappingSchema.UserDefines.ExtensionData.ContainsKey("rollup_child_to_parent_for_one_table_parent_node_list"))
                    //{
                    //    List<string> rollUpNodeNameList = schemaDataSetMappingDto.NodeSettingDtoList.Where(o => o.IsNeedToRollUpAllChild).Select(o => o.NodeName).ToList();
                    //    mappingSchema.UserDefines.ExtensionData["rollup_child_to_parent_for_one_table_parent_node_list"] = rollUpNodeNameList;
                    //}

                    if (mappingSchema.UserDefines.ExtensionData.ContainsKey("object_type_map_to_table_option"))
                    {
                        var tableOptoins = mappingSchema.UserDefines.ExtensionData["object_type_map_to_table_option"];

                        string stringValue_tableOptoin = Newtonsoft.Json.JsonConvert.SerializeObject(tableOptoins);
                        List<Dictionary<string, object>> lstMapping = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(stringValue_tableOptoin);

                        foreach (Dictionary<string, object> tableMapping in lstMapping)
                        {
                            if (tableMapping.ContainsKey("object_type_definition_name"))
                            {
                                string definitionName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(tableMapping["object_type_definition_name"]);

                                var nodeDto = schemaDataSetMappingDto.NodeSettingDtoList.FirstOrDefault(o => o.NodeName == definitionName);

                                if (nodeDto != null)
                                {
                                    if (tableMapping.ContainsKey("mapping_db_table_name"))
                                    {
                                        tableMapping["mapping_db_table_name"] = nodeDto.MappingToTableName;
                                    }

                                    List<string> logicalKeyNames = nodeDto.Properties.Where(o => o.IsLogicalKey).Select(o => o.PropertyName).ToList();

                                    if (logicalKeyNames.Count == 1)
                                    {
                                        if (tableMapping.ContainsKey("key"))
                                        {
                                            tableMapping["key"] = logicalKeyNames[0];
                                        }
                                    }
                                    else if (logicalKeyNames.Count > 1)
                                    {
                                        if (tableMapping.ContainsKey("unique"))
                                        {
                                            tableMapping["unique"] = logicalKeyNames;
                                        }
                                    }

                                    if (tableMapping.ContainsKey("serialization"))
                                    {
                                        tableMapping["serialization"] = nodeDto.Properties.Where(o => o.IsRefToSerializedObj).Select(o => o.PropertyName).ToList();
                                    }

                                    if (tableMapping.ContainsKey("rollup_path"))
                                    {
                                        List<string> rollUpPathList = schemaDataSetMappingDto.NodeSettingDtoList
                                           .Where(o => o.RollUpToParentNodeName == definitionName && o.ProcessMode == (int)EmAppSchemaDataSetMappingNodeProcessMode.RollUpToParent).Select(o => o.NodeName).ToList();

                                        tableMapping["rollup_path"] = rollUpPathList;
                                    }
                                }
                            }
                        }

                        foreach (string removeDefinitionName in needToRemoveDefinitionNames)
                        {
                            var needToRemoveNode = lstMapping.FirstOrDefault(o => o.ContainsKey("object_type_definition_name") && ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(o["object_type_definition_name"]) == removeDefinitionName);

                            if (needToRemoveNode != null)
                            {
                                lstMapping.Remove(needToRemoveNode);
                            }
                        }

                        mappingSchema.UserDefines.ExtensionData["object_type_map_to_table_option"] = lstMapping;
                    }
                }


                dataSetMappingJsonString = mappingSchema.ToJson();
            }


            return dataSetMappingJsonString;
        }

        public static OperationCallResult<AppIntergrationSettingParameterExDto> GenerateTransactionProvideSimpleQueryAPI(int? transactionId)
        {
            OperationCallResult<AppIntergrationSettingParameterExDto> aOperationCallResult = new OperationCallResult<AppIntergrationSettingParameterExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);

            string query = "";

            if (transactionExDto != null && transactionExDto.IsPhysicalModelTableCreated.HasValue && transactionExDto.IsPhysicalModelTableCreated.Value && transactionExDto.DataSourceFrom.HasValue)
            {
                var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(transactionExDto.DataSourceFrom.Value);

                if (dataBaseFixture.SqlServerType.HasValue)
                {
                    if (dataBaseFixture.SqlServerType.Value == EmSqlType.SqlServer)
                    {
                        //Root Unit
                        if (transactionExDto.RootMasterUnit != null && !string.IsNullOrWhiteSpace(transactionExDto.RootMasterUnit.DataBaseTableName))
                        {
                            query = GenerateTransactionQueryAPI_ProcessOneUnit_SqlServer(transactionExDto.RootMasterUnit, null);

                            ////Sibling Unit
                            //foreach (var unitExDto in transactionExDto.AppTransactionUnitList)
                            //{
                            //    if (!string.IsNullOrWhiteSpace(unitExDto.DataBaseTableName))
                            //    {
                            //        if (unitExDto.IsMasterSiblingUnit.HasValue && unitExDto.IsMasterSiblingUnit.Value)
                            //        {
                            //            //Sibling Unit
                            //        }
                            //    }
                            //}
                        }
                    }
                    else if (dataBaseFixture.SqlServerType.Value == EmSqlType.MySql)
                    {
                        query = GenerateTransactionQueryAPI_ProcessOneUnit_MySql(transactionExDto.RootMasterUnit, null);
                    }





                    AppIntergrationSettingParameterExDto operationDto = new AppIntergrationSettingParameterExDto();
                    operationDto.TranscationId = transactionId.Value;
                    operationDto.IsSimpleQuery = true;
                    operationDto.ActionCode = "Get_" + transactionExDto.TransactionName;
                    operationDto.JsonQuery = query;
                    operationDto.HttpMethd = "Get";
                    operationDto.DataSourceId = transactionExDto.DataSourceFrom;
                    operationDto.IntergrationSettingId = 1;
                    operationDto.SimpleQueryParameterNameList = new List<string>();

                    if (transactionExDto.RootMasterUnit != null && !string.IsNullOrWhiteSpace(transactionExDto.RootMasterUnit.DataBaseTableName))
                    {
                        transactionExDto.RootMasterUnit.AppTransactionFieldList.Where(o => o.IsPrimaryKey && !string.IsNullOrWhiteSpace(o.DataBaseFieldName)).ForAll(pkField =>
                        {
                            operationDto.SimpleQueryParameterNameList.Add("@" + pkField.DataBaseFieldName);
                        });
                    }

                    var saveResult = SaveAppIntergrationSettingParameterExDto(operationDto);

                    aValidationResult.Merge(saveResult.ValidationResult);

                    if (saveResult.IsSuccessfulWithResult)
                    {
                        aOperationCallResult.Object = saveResult.Object;
                    }
                }
            }


            return aOperationCallResult;
        }

        public static OperationCallResult<bool> DropAllStagingTablesByImportSettingId(int? settingParameterId)
        {
            OperationCallResult<bool> toReturn = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            toReturn.ValidationResult = aValidationResult;

            if (settingParameterId.HasValue)
            {
                AppIntergrationSettingParameterExDto operationDto = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(settingParameterId.Value);

                //List<string> tableNames = JsonSchemaBL.GetTableNamesFromIntegrationSetting(operationDto);

                List<string> tableNames = operationDto.GeneratedTableNameList;

                if (tableNames.Count > 0)
                {
                    //aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity), "App_DropAllStagingTablesByImportSettingId_OK", ValidationItemType.Message, ""));

                    string errorMsg = JsonSchemaBL.DropDatabaseTableFromSchema(settingParameterId.Value);

                    if (string.IsNullOrWhiteSpace(errorMsg))
                    {

                        aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity), "App_DropAllStagingTablesByImportSettingId_OK", ValidationItemType.Message,
                            "The following tables have been dropped: \n  " + string.Join(", \n  ", tableNames)));
                    }
                    else
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity), "App_DropAllStagingTablesByImportSettingId_OK", ValidationItemType.Error, errorMsg));
                    }
                }
                else
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity), "App_DropAllStagingTablesByImportSettingId_OK", ValidationItemType.Warning, "No table found from current import setting."));
                }

            }

            return toReturn;

        }



        private static string GenerateTransactionQueryAPI_ProcessOneUnit_SqlServer(AppTransactionUnitExDto unitExDto, AppTransactionUnitExDto parentUnit, string prefixSpace = "")
        {
            string query = "";

            string tableName = unitExDto.DataBaseTableName;
            List<string> dbColumnNameList = new List<string>();
            List<string> pkColumnNameList = new List<string>();
            Dictionary<string, string> dictFkColumnNameAndTargetPkColumnName = new Dictionary<string, string>();

            foreach (var transFieldDto in unitExDto.AppTransactionFieldList.OrderBy(o => o.SortOrder))
            {

                if (!string.IsNullOrWhiteSpace(transFieldDto.DataBaseFieldName)
                    && !(transFieldDto.IsTempVariable.HasValue && transFieldDto.IsTempVariable.Value || transFieldDto.IsStoreToExtendTable.HasValue && transFieldDto.IsStoreToExtendTable.Value))
                {
                    dbColumnNameList.Add(transFieldDto.DataBaseFieldName);

                    if (transFieldDto.IsPrimaryKey)
                    {
                        pkColumnNameList.Add(transFieldDto.DataBaseFieldName);
                    }

                    if (parentUnit != null && transFieldDto.IsLinkToParentPrimaryKey && transFieldDto.LinkToParentPrimaryKeyFieldId.HasValue)
                    {
                        var targetField = parentUnit.AppTransactionFieldList.FirstOrDefault(o => o.Id != null && (int)o.Id == transFieldDto.LinkToParentPrimaryKeyFieldId.Value);

                        if (targetField != null && !string.IsNullOrWhiteSpace(targetField.DataBaseFieldName) && !(targetField.IsTempVariable.HasValue && targetField.IsTempVariable.Value || targetField.IsStoreToExtendTable.HasValue && targetField.IsStoreToExtendTable.Value))
                        {
                            dictFkColumnNameAndTargetPkColumnName.Add(transFieldDto.DataBaseFieldName, targetField.DataBaseFieldName);
                        }
                    }
                }
            }

            List<string> childTableQueryList = new List<string>();

            foreach (var childUnitExDto in unitExDto.Children)
            {
                string query_ChildTable = GenerateTransactionQueryAPI_ProcessOneUnit_SqlServer(childUnitExDto, unitExDto, prefixSpace + "    ");

                if (!string.IsNullOrWhiteSpace(query_ChildTable))
                {
                    //query_ChildTable = "(" + query_ChildTable + ") AS " + childUnitExDto.DataBaseTableName;

                    childTableQueryList.Add(query_ChildTable.Trim());
                }
            }

            if (dbColumnNameList.Count > 0 || childTableQueryList.Count > 0)
            {
                string strColumnNames = "";
                string strChildTableQuery = "";

                if (dbColumnNameList.Count > 0)
                {
                    strColumnNames = string.Join(",\n    ", dbColumnNameList.Select(o => tableName + "." + o));

                    if (childTableQueryList.Count > 0)
                    {
                        strColumnNames += ",\n    ";
                    }
                }

                if (childTableQueryList.Count > 0)
                {
                    strChildTableQuery = string.Join(",\n    ", childTableQueryList);
                }

                string queryPart_Select = "SELECT \n    " + strColumnNames + strChildTableQuery + " \n";

                string queryPart_From = "FROM    " + tableName + " \n";
                string queryPart_Where = "";
                string queryPart_ForJson = "FOR JSON PATH";

                if (parentUnit != null)
                {
                    if (dictFkColumnNameAndTargetPkColumnName.Keys.Count > 0)
                    {
                        queryPart_Where = "WHERE    ";

                        int fkCount = 0;
                        foreach (var kv in dictFkColumnNameAndTargetPkColumnName)
                        {
                            fkCount++;

                            if (fkCount > 1)
                            {
                                queryPart_Where += "    and ";
                            }

                            queryPart_Where += tableName + "." + kv.Key + " = " + parentUnit.DataBaseTableName + "." + kv.Value + " \n";
                        }
                    }
                }
                else
                {
                    if (pkColumnNameList.Count > 0)
                    {
                        queryPart_Where = "WHERE    ";

                        int pkCount = 0;
                        foreach (string pkName in pkColumnNameList)
                        {
                            pkCount++;

                            if (pkCount > 1)
                            {
                                queryPart_Where += "    and ";
                            }

                            queryPart_Where += tableName + "." + pkName + " = @" + pkName + " \n";
                        }
                    }
                }

                query = queryPart_Select
                    + queryPart_From
                    + queryPart_Where
                    + queryPart_ForJson;

                if (!string.IsNullOrWhiteSpace(query))
                {


                    if (parentUnit != null)
                    {
                        query = "(" + query + ") AS " + unitExDto.DataBaseTableName;
                        query = AddPrefixIntoStringOnEachLine(prefixSpace, query);
                    }


                }
            }

            return query;
        }


        private static string GenerateTransactionQueryAPI_ProcessOneUnit_MySql(AppTransactionUnitExDto unitExDto, AppTransactionUnitExDto parentUnit, string prefixSpace = "")
        {
            string query = "";

            string tableName = unitExDto.DataBaseTableName;
            List<string> dbColumnNameList = new List<string>();
            List<string> pkColumnNameList = new List<string>();
            Dictionary<string, string> dictFkColumnNameAndTargetPkColumnName = new Dictionary<string, string>();

            foreach (var transFieldDto in unitExDto.AppTransactionFieldList.OrderBy(o => o.SortOrder))
            {

                if (!string.IsNullOrWhiteSpace(transFieldDto.DataBaseFieldName)
                    && !(transFieldDto.IsTempVariable.HasValue && transFieldDto.IsTempVariable.Value || transFieldDto.IsStoreToExtendTable.HasValue && transFieldDto.IsStoreToExtendTable.Value))
                {
                    dbColumnNameList.Add(transFieldDto.DataBaseFieldName);

                    if (transFieldDto.IsPrimaryKey)
                    {
                        pkColumnNameList.Add(transFieldDto.DataBaseFieldName);
                    }

                    if (parentUnit != null && transFieldDto.IsLinkToParentPrimaryKey && transFieldDto.LinkToParentPrimaryKeyFieldId.HasValue)
                    {
                        var targetField = parentUnit.AppTransactionFieldList.FirstOrDefault(o => o.Id != null && (int)o.Id == transFieldDto.LinkToParentPrimaryKeyFieldId.Value);

                        if (targetField != null && !string.IsNullOrWhiteSpace(targetField.DataBaseFieldName) && !(targetField.IsTempVariable.HasValue && targetField.IsTempVariable.Value || targetField.IsStoreToExtendTable.HasValue && targetField.IsStoreToExtendTable.Value))
                        {
                            dictFkColumnNameAndTargetPkColumnName.Add(transFieldDto.DataBaseFieldName, targetField.DataBaseFieldName);
                        }
                    }
                }
            }

            List<string> childTableQueryList = new List<string>();

            foreach (var childUnitExDto in unitExDto.Children)
            {
                string query_ChildTable = GenerateTransactionQueryAPI_ProcessOneUnit_MySql(childUnitExDto, unitExDto, prefixSpace + "    ");

                if (!string.IsNullOrWhiteSpace(query_ChildTable))
                {
                    //query_ChildTable = "(" + query_ChildTable + ") AS " + childUnitExDto.DataBaseTableName;

                    childTableQueryList.Add(query_ChildTable.Trim());
                }
            }

            if (dbColumnNameList.Count > 0 || childTableQueryList.Count > 0)
            {
                string strColumnNames = "";
                string strChildTableQuery = "";

                if (dbColumnNameList.Count > 0)
                {
                    strColumnNames = string.Join(",\n    ", dbColumnNameList.Select(o => "'" + o + "', `" + o + "`"));

                    if (childTableQueryList.Count > 0)
                    {
                        strColumnNames += ",\n    ";
                    }
                }

                if (childTableQueryList.Count > 0)
                {
                    strChildTableQuery = string.Join(",\n    ", childTableQueryList);
                }

                string queryPart_Select = "";

                if (parentUnit == null)
                {
                    queryPart_Select = "SELECT JSON_OBJECT( \n    " + strColumnNames + strChildTableQuery + " \n ) AS JSON \n ";
                }
                else
                {
                    queryPart_Select = "SELECT JSON_ARRAYAGG( \n    JSON_OBJECT( \n        " + strColumnNames + strChildTableQuery + " \n    )  \n)  \n  ";
                }

                string queryPart_From = "FROM    " + tableName + " \n";
                string queryPart_Where = "";


                if (parentUnit != null)
                {
                    if (dictFkColumnNameAndTargetPkColumnName.Keys.Count > 0)
                    {
                        queryPart_Where = "WHERE    ";

                        int fkCount = 0;
                        foreach (var kv in dictFkColumnNameAndTargetPkColumnName)
                        {
                            fkCount++;

                            if (fkCount > 1)
                            {
                                queryPart_Where += "    and ";
                            }

                            queryPart_Where += tableName + "." + kv.Key + " = " + parentUnit.DataBaseTableName + "." + kv.Value + " \n";
                        }
                    }
                }
                else
                {
                    if (pkColumnNameList.Count > 0)
                    {
                        queryPart_Where = "WHERE    ";

                        int pkCount = 0;
                        foreach (string pkName in pkColumnNameList)
                        {
                            pkCount++;

                            if (pkCount > 1)
                            {
                                queryPart_Where += "    and ";
                            }

                            queryPart_Where += tableName + "." + pkName + " = @" + pkName + " \n";
                        }
                    }
                }

                query = queryPart_Select
                    + queryPart_From
                    + queryPart_Where;

                if (!string.IsNullOrWhiteSpace(query))
                {
                    if (parentUnit != null)
                    {
                        query = "'" + unitExDto.DataBaseTableName + "', (" + query + ")  ";
                        query = AddPrefixIntoStringOnEachLine(prefixSpace, query);
                    }


                }
            }

            return query;
        }



        private static string AddPrefixIntoStringOnEachLine(string prefixSpace, string query)
        {
            if (prefixSpace.Length > 0)
            {
                string[] lines = query.Split('\n');

                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i] = prefixSpace + lines[i];
                }

                query = string.Join("\n", lines);
            }

            return query;
        }

        private static ValidationResult ProcessDirtyAppIntergrationSettingExDto(AppIntergrationSettingExDto aAppIntergrationSettingExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            int[] dirtyFieldIds = aAppIntergrationSettingExDto.AppIntergrationSettingParameterList.FindModifiedItems().Select(o => o.Id).Cast<int>().ToArray();

            AppIntergrationSettingEntity aAppIntergrationSettingEntity = RetrieveOneAppIntergrationSettingEntity(aAppIntergrationSettingExDto.Id);

            //Dictionary<int, AppIntergrationSettingParameterEntity> dictAppIntergrationSettingParameterFromDbms = aAppIntergrationSettingEntity.AppIntergrationSettingParameter.ToDictionary(o => o.SettingParameterId, o => o);

            AppIntergrationSettingConverter.CopyDtoToEntity(aAppIntergrationSettingEntity, aAppIntergrationSettingExDto);


            //------- check  AppIntergrationSettingParameter

            //// new Items
            //foreach (var aChildDto in aAppIntergrationSettingExDto.AppIntergrationSettingParameterList.FindNewItems())
            //{
            //    AppIntergrationSettingParameterEntity aNewChildEntity = new AppIntergrationSettingParameterEntity();
            //    AppIntergrationSettingParameterConverter.CopyDtoToEntity(aNewChildEntity, aChildDto);
            //    aAppIntergrationSettingEntity.AppIntergrationSettingParameter.Add(aNewChildEntity);
            //}

            //// Dirty items
            //foreach (var modifyitem in aAppIntergrationSettingExDto.AppIntergrationSettingParameterList.FindModifiedItems())
            //{
            //    int dtoKey = int.Parse(modifyitem.Id.ToString());
            //    if (dictAppIntergrationSettingParameterFromDbms.ContainsKey(dtoKey))
            //    {
            //        AppIntergrationSettingParameterConverter.CopyDtoToEntity(dictAppIntergrationSettingParameterFromDbms[dtoKey], modifyitem);
            //    }
            //}

            //// deletedIDs
            //int[] deleteFieldIDs = aAppIntergrationSettingExDto.AppIntergrationSettingParameterList.FindDeletedItemIds().Cast<int>().ToArray();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppIntergrationSettingEntity);

                    //// Need to delete AppIntergrationSettingParameterFields
                    //if (deleteFieldIDs.Count() > 0)
                    //{
                    //    adapter.DeleteEntitiesDirectly(typeof(AppIntergrationSettingParameterEntity), new RelationPredicateBucket(AppIntergrationSettingParameterFields.IntergrationSettingId == deleteFieldIDs));
                    //}

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_IntergrationSettingEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

                // Entity Logical Validation Exception
                catch (ORMEntityValidationException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_IntergrationSettingEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                }

                // Concurrency validation ("Concurrency violation, data not updated")
                catch (ORMConcurrencyException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_IntergrationSettingEntity_Concurrency_Error", ValidationItemType.Error, ex.ToString()));
                }

                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_IntergrationSettingEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }


        private static ValidationResult ProcessDirtyAppIntergrationSettingParameterExDto(AppIntergrationSettingParameterExDto aAppIntergrationSettingParameterExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();


            string actionCode = aAppIntergrationSettingParameterExDto.ActionCode;

            actionCode = Regex.Replace(actionCode, @"[^0-9a-zA-Z]+", "_");

            List<string> existActionCodeList = RetrieveAllAppIntergrationSettingParameterSimpleEntityList(EmAppIntergrationSettingParameterUsageType.ApiOperation.ToString())
                .Where(o => o.SettingParameterId != (int)aAppIntergrationSettingParameterExDto.Id).Select(o => o.ActionCode.ToLower()).ToList();

            int renameCount = 0;

            while (existActionCodeList.Contains(actionCode.ToLower()))
            {
                renameCount++;
                actionCode = aAppIntergrationSettingParameterExDto.ActionCode + renameCount;
            }

            aAppIntergrationSettingParameterExDto.ActionCode = actionCode;


            AppIntergrationSettingParameterEntity aAppIntergrationSettingParameterEntity = RetrieveOneAppIntergrationSettingParameterEntity(aAppIntergrationSettingParameterExDto.Id);

            AppIntergrationSettingParameterConverter.CopyDtoToEntity(aAppIntergrationSettingParameterEntity, aAppIntergrationSettingParameterExDto);


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppIntergrationSettingParameterEntity);



                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity), "App_IntergrationSettingEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

                // Entity Logical Validation Exception
                catch (ORMEntityValidationException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity), "App_IntergrationSettingEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                }

                // Concurrency validation ("Concurrency violation, data not updated")
                catch (ORMConcurrencyException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity), "App_IntergrationSettingEntity_Concurrency_Error", ValidationItemType.Error, ex.ToString()));
                }

                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity), "App_IntergrationSettingEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static void PrepareAppIntergrationSettingParameterInheriedProperties(AppIntergrationSettingParameterExDto dto)
        {
            dto.ActionId = (int)dto.Id;

            if (dto.IntergrationSettingId.HasValue)
            {
                var intergrationSettingEntity = RetrieveOneAppIntergrationSettingEntity(dto.IntergrationSettingId);
                AppIntergrationSettingExDto aIntergrationSettingDto = AppIntergrationSettingConverter.ConvertEntityToExDto(intergrationSettingEntity);

                if (!dto.DataSourceId.HasValue)
                {
                    dto.DataSourceId = intergrationSettingEntity.DataSourceRegisterId;
                }

                if (dto.MappingInternalCode == EmAppIntergrationSettingParameterUsageType.JsonFileTableImportSetting.ToString())
                {
                    dto.TablePrefix = "JsonImport";
                }
                else
                {
                    dto.TablePrefix = aIntergrationSettingDto.OtherSettingsDto.DatabaseTablePrefix;
                }


                //if (dto.APIConfigParameters != null && dto.APIConfigParameters.Headers.ContainsKey("Authorization"))
                //{
                //    if (dto.APIConfigParameters.Headers["Authorization"] == "{{DefaultAuthorization}}")
                //    {
                //        dto.APIConfigParameters.Headers["Authorization"] = intergrationSettingEntity.ApicredentialConfig;
                //    }
                //}

                if (dto.DataSourceId.HasValue)
                {
                    dto.ConnectionString = AppDataSourceRegisterBL.RetrieveOneAppDataSourceRegisterEntity(dto.DataSourceId.Value).ConnectionString;
                }

                dto.DictProviderEnvironmentVariable = aIntergrationSettingDto.OtherSettingsDto.DictEnvironmentVariable;

                dto.DictUsedEnvironmentVariable = new Dictionary<string, string>();

                if (!string.IsNullOrWhiteSpace(dto.ApiconfigParameters))
                {
                    if (aIntergrationSettingDto.OtherSettingsDto.DictEnvironmentVariable != null)
                    {
                        foreach (var kvPari in aIntergrationSettingDto.OtherSettingsDto.DictEnvironmentVariable)
                        {
                            if (dto.ApiconfigParameters.Contains("{{" + kvPari.Key + "}}"))
                            {
                                dto.DictUsedEnvironmentVariable.Add(kvPari.Key, kvPari.Value);
                            }
                        }
                    }
                }


            }


        }


        internal static void InitApiNodeAbolutePath(ApiDataStructureNodeDto nodeDto, ApiDataStructureNodeDto parentNode)
        {
            if (parentNode == null)
            {
                nodeDto.AbsolutePath = "";
                nodeDto.DataBindingPath = "";
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(parentNode.AbsolutePath))
                {
                    nodeDto.AbsolutePath = parentNode.AbsolutePath + "." + nodeDto.Name;
                }
                else
                {
                    nodeDto.AbsolutePath = nodeDto.Name;
                }


                if (!string.IsNullOrWhiteSpace(parentNode.DataBindingPath) && !parentNode.IsArray)
                {
                    nodeDto.DataBindingPath = parentNode.DataBindingPath + "." + nodeDto.Name;
                }
                else
                {
                    nodeDto.DataBindingPath = nodeDto.Name;
                }
            }


            if (nodeDto.Children != null)
            {
                foreach (var childNodeDto in nodeDto.Children)
                {
                    InitApiNodeAbolutePath(childNodeDto, nodeDto);
                }
            }

        }

        private static string GenerateStagingTableDataUpdateExceptionUserErrorMessage(Exception ex)
        {
            string orgErrorMsg = ex.ToString();


            if (orgErrorMsg.StartsWith("System.AggregateException: One or more errors occurred. ---> System.Exception:"))
            {
                orgErrorMsg = orgErrorMsg.Substring("System.AggregateException: One or more errors occurred. ---> System.Exception:".Length);
            }

            string errorMsg = orgErrorMsg;
            string tableName = "";
            string queryTextKey = "IF Exists(SELECT * From [dbo].[";


            if (orgErrorMsg.IndexOf(queryTextKey) > 0)
            {
                int index = orgErrorMsg.IndexOf(queryTextKey);
                tableName = orgErrorMsg.Substring(index + queryTextKey.Length);
                tableName = tableName.Substring(0, tableName.IndexOf("]"));
            }

            if (orgErrorMsg.Contains(" WHERE )"))
            {
                errorMsg = "Logical Key is not defined on table";
                if (!string.IsNullOrWhiteSpace(tableName))
                {
                    errorMsg += " [" + tableName + "]. ";
                }
                else
                {
                    errorMsg += ". ";
                }

                errorMsg += "\nPlease check your table column setting, and select at least one logical keys.\n";
            }
            else if (orgErrorMsg.Contains("overflowed an int column") || orgErrorMsg.Contains("expression to data type int"))
            {
                errorMsg = "The import data overflowed an int column on database table";
                if (!string.IsNullOrWhiteSpace(tableName))
                {
                    errorMsg += " [" + tableName + "]. ";
                }
                else
                {
                    errorMsg += ". ";
                }

                errorMsg += "\nPlease check your table column setting, and set the overwrite type on columns.\n";
            }
            else if (orgErrorMsg.Contains("data would be truncated"))
            {
                errorMsg = "The import string or binary data overflowed an column on database table";
                if (!string.IsNullOrWhiteSpace(tableName))
                {
                    errorMsg += " [" + tableName + "]. ";
                }
                else
                {
                    errorMsg += ". ";
                }

                errorMsg += "\nPlease check your table column setting, and set the overwrite type on columns.\n";
            }
            else if (orgErrorMsg.Contains("Out of range value for column"))
            {
                errorMsg = "The import data overflowed an column on database table";
                if (!string.IsNullOrWhiteSpace(tableName))
                {
                    errorMsg += " [" + tableName + "]. ";
                }
                else
                {
                    errorMsg += ". ";
                }

                errorMsg += "\nPlease check your table column setting, and set the overwrite type on columns.\n";

                errorMsg += "\n" + orgErrorMsg;
            }




            return errorMsg;
        }

        private static void ConvertOneNodeToDiagramTable(AppIntergrationSettingParameterExDto aAppIntergrationSettingParameterExDto, DatabaseViewDto diagramDto, string schemaOwner, Dictionary<string, SchemaDataSetMappingDefinitionDto> dictNodeNameAndDto, int nodeLevel, ApiDataStructureNodeDto simpleHierachyNode)
        {
            if (dictNodeNameAndDto.ContainsKey(simpleHierachyNode.SchemaTypeName))
            {
                var nodeDto = dictNodeNameAndDto[simpleHierachyNode.SchemaTypeName];

                if (nodeDto.IsCreateTable)
                {
                    AddNodeTableToDiagram(aAppIntergrationSettingParameterExDto, diagramDto, schemaOwner, nodeLevel, nodeDto);
                }
                else if (nodeDto.IsMapToExistingTable)
                {
                    AddNodeTableToDiagram(aAppIntergrationSettingParameterExDto, diagramDto, schemaOwner, nodeLevel, nodeDto);
                }
            }

            if (simpleHierachyNode.Children != null && simpleHierachyNode.Children.Count > 0)
            {
                foreach (var childHierachyNode in simpleHierachyNode.Children)
                {
                    ConvertOneNodeToDiagramTable(aAppIntergrationSettingParameterExDto, diagramDto, schemaOwner, dictNodeNameAndDto, nodeLevel + 1, childHierachyNode);
                }
            }
        }
        private static void AddNodeTableToDiagram(AppIntergrationSettingParameterExDto aAppIntergrationSettingParameterExDto, DatabaseViewDto diagramDto, string schemaOwner, int nodeLevel, SchemaDataSetMappingDefinitionDto nodeDto)
        {
            if (string.IsNullOrWhiteSpace(nodeDto.MappingToTableName))
            {
                string tableName = aAppIntergrationSettingParameterExDto.TablePrefix + MappingNameSpliter + nodeDto.MappingToTableName;

                int positionX = AppDatabaseViewBL.startX + nodeLevel * 300;
                int indexY = diagramDto.DictTables.Values.Where(o => o.Level.HasValue && o.Level.Value == nodeLevel).Count();
                int positionY = AppDatabaseViewBL.startY + indexY * 250;

                DatabaseViewTableDto viewTable = new DatabaseViewTableDto()
                {
                    Width = AppDatabaseViewBL.tableWidth,
                    Height = AppDatabaseViewBL.tableHeight,
                    PositionX = positionX,
                    PositionY = positionY
                };

                viewTable.SortOrder = diagramDto.DictTables.Count + 1;
                viewTable.SchemaOwner = schemaOwner;
                viewTable.TableName = tableName;
                viewTable.UniqTableOrAliasName = tableName;
                viewTable.Level = nodeLevel;

                //if (dictTableNameRefParentFkList.ContainsKey(tableName))
                //{
                //    viewTable.FKRefTables = dictTableNameRefParentFkList[tableName].Distinct().ToList();
                //}


                //if (dictTableNameRefedChildList.ContainsKey(tableName))
                //{
                //    viewTable.FKRefedTables = dictTableNameRefedChildList[tableName].Distinct().ToList();
                //}


                viewTable.PkNames = new List<string>();

                //if (dbTable.PrimaryKeyColumnList != null)
                //{
                //    viewTable.PkNames = dbTable.PrimaryKeyColumnList.Select(o => o.Name).ToList();
                //}

                //if (uniqTableOrAliasName != tableName)
                //{
                //    viewTable.TableAlias = uniqTableOrAliasName;
                //}
                //else
                //{
                //    viewTable.TableAlias = string.Empty;
                //}                               


                diagramDto.DictTables.Add(tableName, viewTable);
            }

        }



        private static void MergeNewAndOrgHierachyNodeNameList(List<ApiDataStructureNodeDto> orgNodeList, List<ApiDataStructureNodeDto> newNodeList)
        {
            foreach (ApiDataStructureNodeDto newNodeDto in newNodeList)
            {
                var orgNodeDto = orgNodeList.FirstOrDefault(o => o.SchemaTypeName == newNodeDto.SchemaTypeName);

                if (orgNodeDto != null)
                {
                    if (newNodeDto.Children != null)
                    {
                        if (orgNodeDto.Children == null)
                        {
                            orgNodeDto.Children = new List<ApiDataStructureNodeDto>();
                        }

                        MergeNewAndOrgHierachyNodeNameList(orgNodeDto.Children, newNodeDto.Children);
                    }
                }
                else
                {
                    orgNodeList.Add(newNodeDto);
                }
            }
        }

        //private static void MergeJsonObjects(JObject target, JObject source)
        //{
        //    foreach (var property in source.Properties())
        //    {
        //        if (target[property.Name] is JArray targetArray && property.Value is JArray sourceArray)
        //        {                    
        //            target[property.Name] = MergeArrays(targetArray, sourceArray);
        //        }               
        //        else if (target[property.Name] is JObject targetObject && property.Value is JObject sourceObject)
        //        {
        //            // If the property is an object, merge recursively
        //            MergeJsonObjects(targetObject, sourceObject);
        //        }
        //        else
        //        {
        //            // Otherwise, add or overwrite the property in the target
        //            target[property.Name] = property.Value;
        //        }
        //    }
        //}


        //private static JArray MergeArrays(JArray targetArray, JArray sourceArray)
        //{
        //    // Merge the elements of the arrays
        //    foreach (var item in sourceArray)
        //    {
        //        targetArray.Add(item);
        //    }

        //    return targetArray;
        //}

        private static void MergeJsonSchema(JsonSchema targetSchema, JsonSchema srcSchema)
        {
            if (targetSchema.Definitions != null && srcSchema.Definitions != null)
            {

                foreach (string definitionName in srcSchema.Definitions.Keys)
                {
                    JsonSchema newDefinitionSchema = srcSchema.Definitions[definitionName];

                    if (targetSchema.Definitions.ContainsKey(definitionName))
                    {
                        JsonSchema orgDefinitionSchema = targetSchema.Definitions[definitionName];

                        foreach (string propertyName in newDefinitionSchema.Properties.Keys)
                        {
                            JsonSchemaProperty newProperty = newDefinitionSchema.Properties[propertyName];

                            if (orgDefinitionSchema.Properties.ContainsKey(propertyName))
                            {
                                JsonSchemaProperty orgProperty = orgDefinitionSchema.Properties[propertyName];

                                if (newProperty.Type != JsonObjectType.Null)
                                {
                                    orgProperty.Type = newProperty.Type;
                                }

                                if (string.IsNullOrWhiteSpace(newProperty.Format) && !string.IsNullOrWhiteSpace(newProperty.Format))
                                {
                                    orgProperty.Format = newProperty.Format;
                                }

                                if (newProperty.Reference != null && orgProperty.Reference == null)
                                {
                                    orgProperty.Reference = newProperty.Reference;
                                }

                                if (newProperty.Item != null && newProperty.Item.Reference != null)
                                {

                                    if (!(orgProperty.Item != null && newProperty.Item.Reference != null))
                                    {
                                        orgProperty.Item = newProperty.Item;
                                    }
                                }
                            }
                            else
                            {
                                orgDefinitionSchema.Properties.Add(propertyName, newProperty);
                            }
                        }

                    }
                    else
                    {
                        targetSchema.Definitions.Add(definitionName, newDefinitionSchema);
                    }
                }
            }
        }


        private static string PrepareApiGetUrl(string baseUrl, string actionCode, Dictionary<string, string> queryParams)
        {

            string apiUrl = baseUrl + "/webapi/DataIntegration/" + actionCode;

            int paramCount = 0;

            foreach (var param in queryParams)
            {
                if (!string.IsNullOrEmpty(param.Value) && !string.IsNullOrEmpty(param.Key))
                {
                    if (paramCount == 0)
                    {
                        apiUrl += $"?{param.Key}={param.Value}";
                    }
                    else
                    {
                        apiUrl += $"&{param.Key}={param.Value}";
                    }
                    paramCount++;
                }
            }

            return apiUrl;
        }

        private static AppIntergrationSettingParameterExDto PrepareExistingGetApiWithDataSchema(bool isInlucdeApiDataStructure, string appBaseUrl, int operationId)
        {
            AppIntergrationSettingParameterExDto toReturn = null;

            var existingApiDto = RetrieveOneAppIntergrationSettingParameterExDto(operationId, isInlucdeApiDataStructure);

            if (!string.IsNullOrWhiteSpace(existingApiDto.JsonSchema))
            {
                toReturn = existingApiDto;
            }
            else
            {
                string apiUrl = PrepareApiGetUrl(appBaseUrl, existingApiDto.ActionCode, existingApiDto.APIConfigParameters.QueryParams);
                var apiResult = CallGetApiByUrl(apiUrl, existingApiDto.APIConfigParameters.Headers);
                existingApiDto.JsonSampleData = apiResult;

                var generateResult = DataExchangeSettingBL.GenerateDefaultSchemaAndDataSetMappingFromSampleJson(existingApiDto);

                if (generateResult.IsSuccessfulWithResult)
                {
                    toReturn = generateResult.Object;
                }
            }

            return toReturn;
        }


        //private static AppIntergrationSettingParameterExDto PrepareExistingPostApiWithDataSchema(bool isInlucdeApiDataStructure, string appBaseUrl, int operationId)
        //{
        //    AppIntergrationSettingParameterExDto toReturn = null;

        //    var existingApiDto = RetrieveOneAppIntergrationSettingParameterExDto(operationId, isInlucdeApiDataStructure);

        //    if (!string.IsNullOrWhiteSpace(existingApiDto.JsonSchema))
        //    {
        //        toReturn = existingApiDto;
        //    }
        //    else
        //    {
        //        string apiUrl = PrepareApiGetUrl(appBaseUrl, existingApiDto.ActionCode, existingApiDto.APIConfigParameters.QueryParams);
        //        var apiResult = CallGetApiByUrl(apiUrl, existingApiDto.APIConfigParameters.Headers);
        //        existingApiDto.JsonSampleData = apiResult;

        //        var generateResult = DataExchangeSettingBL.GenerateDefaultSchemaAndDataSetMappingFromSampleJson(existingApiDto);

        //        if (generateResult.IsSuccessfulWithResult)
        //        {
        //            toReturn = generateResult.Object;
        //        }
        //    }

        //    return toReturn;
        //}



        private static OperationCallResult<AppIntergrationSettingParameterExDto> GenerateAppSearchProviderApi(int searchId, string appBaseUrl)
        {
            var searchDto = AppSearchConfigBL.RetrieveOneAppSearchExDto(searchId);

            AppIntergrationSettingParameterExDto newDto = new AppIntergrationSettingParameterExDto();
            newDto.TranscationFieId = searchId;
            newDto.HttpMethd = "Get";
            newDto.IntergrationSettingId = 1;
            newDto.DataSourceId = searchDto.DataSourceFrom;
            newDto.ActionCode = "AppSearch" + searchDto.Id + "_" + searchDto.Name.Replace(" ", "").Trim();


            newDto.JsonSampleData = "";
            newDto.JsonSchema = "";


            string queryParams = "";

            if (searchDto?.AppSearchFieldList != null)
            {
                var searchFieldList = searchDto.AppSearchFieldList.ToList();

                for (int i = 0; i < searchFieldList.Count; i++)
                {
                    var searchFieldDto = searchFieldList[i];
                    queryParams += $"\"{searchFieldDto.SysTableFiledPath}\": \"{searchFieldDto.DefaultValue ?? ""}\"";

                    if (i < searchFieldList.Count - 1)
                    {
                        queryParams += ", ";
                    }
                }
            }


            newDto.ApiconfigParameters = $@"
{{
    ""BaseUrl"": """",
    ""Url"": """",
    ""Headers"": {{
        ""CurrentUserSessionId"": ""6601508d-e7e0-4ed6-892b-879c834676af""
    }},
    ""QueryParams"": {{
        {queryParams}
    }},
    ""PathParams"": {{}},
    ""PostProcessMethodName"": null,
    ""ResponseObjectMapToEnvionmentVariable"": {{}},
    ""ResponseHeaderNeedToSetCookieNames"": []
}}";
            var saveResult = SaveAppIntergrationSettingParameterExDto(newDto);


            if (saveResult.IsSuccessfulWithResult)
            {
                var apiDto = saveResult.Object;
                string apiUrl = PrepareApiGetUrl(appBaseUrl, apiDto.ActionCode, apiDto.APIConfigParameters.QueryParams);

                //var apiResult = CallGetApiByUrlAsync(apiUrl, apiDto.APIConfigParameters.Headers).GetAwaiter().GetResult();
                var apiResult = CallGetApiByUrl(apiUrl, apiDto.APIConfigParameters.Headers);
                apiDto.JsonSampleData = apiResult;

                var generateResult = DataExchangeSettingBL.GenerateDefaultSchemaAndDataSetMappingFromSampleJson(apiDto);
                return generateResult;
            }
            else
            {
                return saveResult;
            }
        }




        private static OperationCallResult<AppIntergrationSettingParameterExDto> GenerateAppFormProviderGetApi(int transactionId, string appBaseUrl)
        {
            var transactionDto = AppTransactionBL.RetrieveOneAppTransactionExDto(transactionId);

            AppIntergrationSettingParameterExDto newDto = new AppIntergrationSettingParameterExDto();
            newDto.TranscationId = transactionId;
            newDto.HttpMethd = "Get";
            newDto.IntergrationSettingId = 1;
            newDto.DataSourceId = transactionDto.DataSourceFrom;
            newDto.ActionCode = "GetAppForm" + transactionDto.Id + "_" + transactionDto.TransactionName.Replace(" ", "").Trim();

            newDto.JsonSampleData = "";
            newDto.JsonSchema = "";            


            newDto.ApiconfigParameters = $@"
{{
    ""BaseUrl"": """",
    ""Url"": """",
    ""Headers"": {{
        ""CurrentUserSessionId"": ""6601508d-e7e0-4ed6-892b-879c834676af""
    }},
    ""QueryParams"": {{
        ""id"":1
    }},
    ""PathParams"": {{}},
    ""PostProcessMethodName"": null,
    ""ResponseObjectMapToEnvionmentVariable"": {{}},
    ""ResponseHeaderNeedToSetCookieNames"": []
}}";
            var saveResult = SaveAppIntergrationSettingParameterExDto(newDto);


            if (saveResult.IsSuccessfulWithResult)
            {
                var apiDto = saveResult.Object;
                string apiUrl = PrepareApiGetUrl(appBaseUrl, apiDto.ActionCode, apiDto.APIConfigParameters.QueryParams);

                //var apiResult = CallGetApiByUrlAsync(apiUrl, apiDto.APIConfigParameters.Headers).GetAwaiter().GetResult();
                var apiResult = CallGetApiByUrl(apiUrl, apiDto.APIConfigParameters.Headers);
                apiDto.JsonSampleData = apiResult;

                var generateResult = DataExchangeSettingBL.GenerateDefaultSchemaAndDataSetMappingFromSampleJson(apiDto);
                return generateResult;
            }
            else
            {
                return saveResult;
            }
        }



        private static OperationCallResult<AppIntergrationSettingParameterExDto> GenerateAppFormProviderPostApi(int transactionId, string appBaseUrl)
        {
            var transactionDto = AppTransactionBL.RetrieveOneAppTransactionExDto(transactionId);

            AppIntergrationSettingParameterExDto newDto = new AppIntergrationSettingParameterExDto();
            newDto.TranscationId = transactionId;
            newDto.HttpMethd = "Post";
            newDto.IntergrationSettingId = 1;
            newDto.DataSourceId = transactionDto.DataSourceFrom;
            newDto.ActionCode = "SaveAppForm" + transactionDto.Id + "_" + transactionDto.TransactionName.Replace(" ", "").Trim();

            newDto.JsonSampleData = "";
            newDto.JsonSchema = "";


            newDto.ApiconfigParameters = $@"
{{
    ""BaseUrl"": """",
    ""Url"": """",
    ""Headers"": {{
        ""CurrentUserSessionId"": ""6601508d-e7e0-4ed6-892b-879c834676af""
    }},
    ""QueryParams"": {{
        ""id"":1
    }},
    ""PathParams"": {{}},
    ""PostProcessMethodName"": null,
    ""ResponseObjectMapToEnvionmentVariable"": {{}},
    ""ResponseHeaderNeedToSetCookieNames"": []
}}";
            var saveResult = SaveAppIntergrationSettingParameterExDto(newDto);

            return saveResult;
        }



       
    }
}