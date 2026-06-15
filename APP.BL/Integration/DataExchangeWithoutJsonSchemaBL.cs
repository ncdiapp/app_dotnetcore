using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using DatabaseSchemaMrg;
using DatabaseSchemaMrg.DataSchema;
using ExchangeBL.Models;
using Google.Protobuf.WellKnownTypes;
//
#if NETFRAMEWORK
using Microsoft.AnalysisServices.AdomdClient;
// TODO-PHASE4: Replace with .NET 10 equivalent
#endif
#if NETFRAMEWORK
using MySqlX.XDevAPI;
#endif
using Newtonsoft.Json.Linq;
using SD.LLBLGen.Pro.ORMSupportClasses;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
#if NETFRAMEWORK
using System.Management.Automation.Language;
#endif
using System.Text;
using System.Threading.Tasks;
using Twilio.TwiML.Voice;

using APP.Framework;
namespace ExchangeBL
{
    public class DataExchangeWithoutJsonSchemaBL
    {
        public async static Task<object> GetAsync(string actionCode, List<KeyValuePair<string, string>> parameters)
        {

            AppIntergrationSettingParameterExDto dataExchangeSetting = DataExchangeSettingBL.GetSetting(actionCode);

            if (dataExchangeSetting.IsSimpleQuery.HasValue && dataExchangeSetting.IsSimpleQuery.Value)
            {
                string jsonData = "";
                if (dataExchangeSetting.DataSourceId.HasValue)
                {
                    string strQuery = dataExchangeSetting.JsonQuery;

                    KeyValuePair<int?, string> dataSourceRegisterIdQuery = new KeyValuePair<int?, string>(dataExchangeSetting.DataSourceId, strQuery);
                    List<DbParameter> parameterList = new List<DbParameter>();
                    DatabaseFixture dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(dataExchangeSetting.DataSourceId.Value);

                    if (parameters != null)
                    {

                        foreach (var oneKV in parameters)
                        {
                            string parameName = oneKV.Key;

                            if (strQuery.Contains("@" + parameName))
                            {
                                DbParameter parameter = dataBaseFixture.CreateParameter(parameName);
                                parameter.Value = oneKV.Value;
                                parameterList.Add(parameter);
                            }
                        }
                    }

                    TableDataDto dtQueryResult = AppMetaDataBL.ExcuteQueryResult(dataSourceRegisterIdQuery, null, false, parameterList);

                    if (dtQueryResult != null && dtQueryResult.DataRowList != null && dtQueryResult.DataRowList.Count > 0)
                    {
                        if (dtQueryResult.DataRowList[0].ContainsKey("JSON"))
                        {
                            jsonData = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dtQueryResult.DataRowList[0]["JSON"]);

                        }
                    }
                }

                return jsonData;
            }
            else if (dataExchangeSetting.TranscationId.HasValue)
            {
                string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject("");

                AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(dataExchangeSetting.TranscationId.Value);

                if (transactionExDto.TransactionOrganizedType.HasValue)
                {
                    if (transactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.MasterDetail)
                    {
                        jsonData = PrepareJsonData_MasterDetailDataModelGetApi(parameters, dataExchangeSetting);
                    }
                    else if (transactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.List)
                    {
                        jsonData = PrepareJsonData_ListEditDataModelGetApi(parameters, dataExchangeSetting);
                    }

                }

                return jsonData;
            }
            else if (dataExchangeSetting.TranscationFieId.HasValue)
            {
                string jsonData = PrepareJsonData_SearchResultGetApi(parameters, dataExchangeSetting);
                return jsonData;
            }
            else
            {
                if (parameters != null && parameters.Count > 0)
                {
                    foreach (KeyValuePair<string, string> oneKV in parameters)
                    {
                        if (dataExchangeSetting.APIConfigParameters.QueryParams.ContainsKey(oneKV.Key))
                        {
                            dataExchangeSetting.APIConfigParameters.QueryParams[oneKV.Key] = oneKV.Value;
                        }
                    }
                }

                List<string> lstResponse = await Helper.CallAPIAsync(dataExchangeSetting.APIConfigParameters, null, dataExchangeSetting.IntergrationSettingId).ConfigureAwait(false);

                if (lstResponse.Count() > 0) // if new json, then update keys
                {
                    string jsonData = lstResponse.FirstOrDefault();

                    return jsonData;
                }
            }

            return "";
        }


        public static string ExecuteApiOperationSaveCommand(string actionCode, string strJson, List<KeyValuePair<string, string>> parameters)
        {
            AppIntergrationSettingParameterExDto dataExchangeSetting = DataExchangeSettingBL.GetSetting(actionCode);
            DatabaseFixture dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(dataExchangeSetting.DataSourceId.Value);

            if (dataExchangeSetting.IsSimpleQuery.HasValue && dataExchangeSetting.IsSimpleQuery.Value)
            {
                //string strQuery = dataExchangeSetting.JsonQuery;

                //ICommand sqlClientCommand = new Command(dataExchangeSetting.ConnectionString).Sql(strQuery);

                //if (strQuery.Contains("@json"))
                //{
                //    sqlClientCommand.Param("@json", strJson);
                //}

                //if (parameters != null)
                //{
                //    foreach (var oneKV in parameters)
                //    {
                //        if (strQuery.Contains("@" + oneKV.Key))
                //        {
                //            sqlClientCommand.Param("@" + oneKV.Key, oneKV.Value);
                //        }
                //    }
                //}
                //object id = 1;

                //await sqlClientCommand.ExecAsync().ConfigureAwait(false);




                // Replace Belgrade Command
                string strQuery = dataExchangeSetting.JsonQuery;
                List<DbParameter> parameterList = new List<DbParameter>();

                if (strQuery.Contains("@json"))
                {
                    DbParameter parameter = dataBaseFixture.CreateParameter("json");
                    parameter.Value = strJson;
                    parameterList.Add(parameter);
                }

                if (parameters != null)
                {
                    foreach (var oneKV in parameters)
                    {
                        if (strQuery.Contains("@" + oneKV.Key))
                        {
                            DbParameter parameter = dataBaseFixture.CreateParameter(oneKV.Key);
                            parameter.Value = oneKV.Value;
                            parameterList.Add(parameter);
                        }
                    }
                }
                object id = 1;

                string errorMsg = AppMetaDataBL.ExecSQlCommand(dataBaseFixture, strQuery, true, parameterList);

                return id.ToString();
            }
            else if (dataExchangeSetting.TranscationId.HasValue)
            {
                string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(""); ;

                AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(dataExchangeSetting.TranscationId.Value);

                if (transactionExDto.TransactionOrganizedType.HasValue)
                {
                    if (transactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.MasterDetail)
                    {
                        jsonData = ExecuteMasterDetailDataModelPostApi(strJson, dataExchangeSetting);
                    }
                    else if (transactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.List)
                    {
                        jsonData = ExecuteListEditDataModelPostApi(strJson, dataExchangeSetting);
                    }

                }


                return jsonData;

            }
            else if (dataExchangeSetting.APIConfigParameters != null && dataExchangeSetting.APIConfigParameters.ExcelDataImportDataSetId.HasValue)
            {
                string jsonData = strJson;

                int importSettingDataSetId = dataExchangeSetting.APIConfigParameters.ExcelDataImportDataSetId.Value;

                DatabaseTableImportSettingDto importSettingDto = AppDatabaseTableImportBL.RetrieveOneTableImportSettingDto(importSettingDataSetId);


                string toReturn = ExecuteExcelImportUpdateByApi(strJson, dataExchangeSetting, importSettingDataSetId, importSettingDto);

                return toReturn;
            }

            return "";

        }

        private static string ExecuteExcelImportUpdateByApi(string strJson, AppIntergrationSettingParameterExDto dataExchangeSetting, int importSettingDataSetId, DatabaseTableImportSettingDto importSettingDto)
        {
            string toReturn = "";

            if (importSettingDto != null)
            {

                if (importSettingDto != null && importSettingDto.OrgSourceColumns != null)
                {
                    if (importSettingDto.OrgSourceColumns.Count > 0)
                    {
                        List<string> srcColumnNameList = importSettingDto.OrgSourceColumns.Select(o => o.Name).ToList();

                        string tempTableName = "Temp_ImportJson";

                        // Step1. Create Temp Table From Json On Master DB
                        // Step2. Read Temp Table as DataTable to memory
                        // Step3. Use DataTable to generate target db (ie. mysql) temptable, and follow the excel import logic.


                        string query = @"IF OBJECT_ID('" + tempTableName + "', 'U') IS NOT NULL DROP TABLE " + tempTableName + ";" + Environment.NewLine
                            + @"SELECT * into " + tempTableName + " FROM OPENJSON(@json)    WITH ( " + Environment.NewLine;


                        for (int i = 0; i < srcColumnNameList.Count; i++)
                        {
                            string colName = srcColumnNameList[i];

                            query += "    [" + colName + "] nvarchar(4000) '$." + colName + "'";

                            if (i < srcColumnNameList.Count - 1)
                            {
                                query += ", " + Environment.NewLine;
                            }
                            else if (i == srcColumnNameList.Count - 1)
                            {
                                query += Environment.NewLine;
                            }
                        }

                        query += "); " + Environment.NewLine;


                        using (DataAccessAdapter adapter = new DataAccessAdapter(ServerContext.Instance.CurrentUserDbConnectionString))
                        {

                            try
                            {
                                List<SqlParameter> listParas = new List<SqlParameter>();
                                listParas.Add(new SqlParameter("@json", strJson));
                                adapter.ExecuteScalarQuery(query, listParas);

                                if (importSettingDto.IsEntityPureManyToManyRelationshipImport)
                                {
                                    importSettingDto.TempTableName = tempTableName;
                                    ValidationResult validationResult = new ValidationResult();
                                    AppDatabaseTableImportBL.ProcessUpdateEntityPureManyToManyRelationshipTable(importSettingDto, validationResult);

                                    if (validationResult.HasErrors)
                                    {
                                        string resultMsg = validationResult.LocalizedResult;
                                        toReturn = Newtonsoft.Json.JsonConvert.SerializeObject(resultMsg);
                                    }
                                    else
                                    {
                                        string resultMsg = "Import Success.";
                                        toReturn = Newtonsoft.Json.JsonConvert.SerializeObject(resultMsg);
                                    }
                                }
                                else
                                {

                                    var updateResult = AppDatabaseTableImportBL.UpdateImportedTableDataFromTempTable(importSettingDataSetId, tempTableName, "API_" + dataExchangeSetting.ActionCode);

                                    if (updateResult.IsSuccessful)
                                    {
                                        string resultMsg = "Import Success.";
                                        toReturn = Newtonsoft.Json.JsonConvert.SerializeObject(resultMsg);
                                    }
                                    else
                                    {
                                        string resultMsg = updateResult.ValidationResult.LocalizedMessages;
                                        toReturn = Newtonsoft.Json.JsonConvert.SerializeObject(resultMsg);
                                    }
                                }
                            }
                            catch (ORMQueryExecutionException ex)
                            {

                                adapter.Rollback();
                                string resultMsg = "Import Failed. " + Environment.NewLine
                                   //+ "Query: "
                                   //+ query + Environment.NewLine
                                   + " Error Details: " + Environment.NewLine + " "
                                   + ex.ToString();

                                toReturn = Newtonsoft.Json.JsonConvert.SerializeObject(resultMsg);
                            }

                        }
                    }

                }


            }



            return toReturn;
        }

        public static string GetSample(string actionCode)
        {
            AppIntergrationSettingParameterExDto dataChangeSetting = DataExchangeSettingBL.GetSetting(actionCode);

            Dictionary<string, string> apiSample = new Dictionary<string, string>();

            apiSample.Add("SampleData", dataChangeSetting.JsonSampleData);
            apiSample.Add("Parameters", dataChangeSetting.WhereClauseFormat);

            return Newtonsoft.Json.JsonConvert.SerializeObject(apiSample);
        }

        private static string PrepareJsonData_MasterDetailDataModelGetApi(List<KeyValuePair<string, string>> parameters, AppIntergrationSettingParameterExDto dataExchangeSetting)
        {
            AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(dataExchangeSetting.TranscationId.Value);
            var rootIdParam = parameters.FirstOrDefault(o => o.Key.ToLower() == "id");

            if (!string.IsNullOrWhiteSpace(rootIdParam.Value))
            {
                AppMasterDetailDto formDataDto = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(dataExchangeSetting.TranscationId.Value, rootIdParam.Value);

                DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(formDataDto);
                dynamic postDynamicData = ConvertMasterDetailFormDataToDynamicJsonObj(dataExchangeSetting, transactionExDto, formDataDto);

                string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(postDynamicData);

                return jsonData;
            }

            return "";
        }

        private static dynamic ConvertMasterDetailFormDataToDynamicJsonObj(AppIntergrationSettingParameterExDto dataExchangeSetting, AppTransactionExDto transactionExDto, AppMasterDetailDto formDataDto)
        {
            dynamic postDynamicData = new ExpandoObject();
            object formRootData = ((IDictionary<String, Object>)postDynamicData);

            var rootUnit = transactionExDto.RootMasterUnit;

            // Root Unit
            AppMasterDetailApiFormDataSaveBL.ConvertOneUnitDataRowToDictObject(formDataDto.DictOneToOneFields, (IDictionary<String, Object>)formRootData);

            //Sibling Unit
            foreach (var aUnit in transactionExDto.AppTransactionUnitList)
            {
                if (aUnit.IsMasterSiblingUnit.HasValue && aUnit.IsMasterSiblingUnit.Value)
                {
                    // ExpandoObject dictSiblingUnit = new ExpandoObject();
                    string[] levelPathList = aUnit.JsonPath.Split(new string[] { "." }, StringSplitOptions.None);
                    var siblingNode = AppMasterDetailApiFormDataSaveBL.CreateDictNodeFromPathLevel((IDictionary<String, Object>)formRootData, levelPathList, 0);
                    AppMasterDetailApiFormDataSaveBL.ConvertOneUnitDataRowToDictObject(formDataDto.DictSiblingOneToOneFields[aUnit.Id.ToString()], (IDictionary<String, Object>)siblingNode);
                }
            }

            // Child Uit
            foreach (var childUnit in rootUnit.Children)
            {
                string childUnitId = childUnit.Id.ToString();
                string childUnitPath = childUnit.JsonPath;
                string[] childUnitPaths = childUnitPath.Split(new string[] { "." }, StringSplitOptions.None);

                bool isChildUnitSimpleList = childUnit.IsExclusiveForOwner.HasValue && childUnit.IsExclusiveForOwner.Value;
                var childUnitArrayNode = AppMasterDetailApiFormDataSaveBL.CreateArrayNodeFromPathLevel((IDictionary<String, Object>)formRootData, childUnitPaths, 0, isChildUnitSimpleList);

                foreach (var childDataRow in formDataDto.DictOneToManyFields[childUnitId])
                {
                    ExpandoObject dictChildRowData = new ExpandoObject();

                    if (isChildUnitSimpleList)
                    {
                        string simpleValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(childDataRow.DictOneToOneFields.Values.FirstOrDefault());
                        ((List<object>)childUnitArrayNode).Add(simpleValue);
                    }
                    else
                    {
                        AppMasterDetailApiFormDataSaveBL.ConvertOneUnitDataRowToDictObject(childDataRow.DictOneToOneFields, (IDictionary<String, Object>)dictChildRowData);
                        ((List<ExpandoObject>)childUnitArrayNode).Add(dictChildRowData);

                        // Grandchild Uit
                        foreach (var grandchildUnit in childUnit.Children)
                        {
                            string gcUnitId = grandchildUnit.Id.ToString();
                            string gcUnitPath = grandchildUnit.JsonPath;
                            string[] gcUnitPaths = gcUnitPath.Split(new string[] { "." }, StringSplitOptions.None);

                            bool isGcUnitSimpleList = grandchildUnit.IsExclusiveForOwner.HasValue && grandchildUnit.IsExclusiveForOwner.Value;
                            var gcUnitArrayNode = AppMasterDetailApiFormDataSaveBL.CreateArrayNodeFromPathLevel((IDictionary<String, Object>)dictChildRowData, gcUnitPaths, 0, isGcUnitSimpleList);

                            foreach (var gcDataRow in childDataRow.DictOneToManyFields[gcUnitId])
                            {

                                if (isGcUnitSimpleList)
                                {
                                    string simpleValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(gcDataRow.DictOneToOneFields.Values.FirstOrDefault());
                                    ((List<object>)gcUnitArrayNode).Add(simpleValue);
                                }
                                else
                                {
                                    ExpandoObject dictGcRowData = new ExpandoObject();
                                    AppMasterDetailApiFormDataSaveBL.ConvertOneUnitDataRowToDictObject(gcDataRow.DictOneToOneFields, (IDictionary<String, Object>)dictGcRowData);
                                    ((List<ExpandoObject>)gcUnitArrayNode).Add(dictGcRowData);
                                }
                            }
                        }
                    }
                }


            }


            GenerateLookupItemDataSourceNode(dataExchangeSetting, postDynamicData);
            PrepareDynamicLabelDictionary(transactionExDto, formDataDto, postDynamicData);

            return postDynamicData;
        }

        private static void PrepareDynamicLabelDictionary(AppTransactionExDto transactionExDto, AppMasterDetailDto formDataDto, dynamic postDynamicData)
        {
            if (formDataDto.DictCascadingFieldIdAndLabel != null && formDataDto.DictCascadingFieldIdAndLabel.Count > 0)
            {
                Dictionary<string, string> dictDynamicLabel = new Dictionary<string, string>();

                foreach (string transFieldIdStr in formDataDto.DictCascadingFieldIdAndLabel.Keys)
                {
                    string labelValue = formDataDto.DictCascadingFieldIdAndLabel[transFieldIdStr];

                    int? transFieldId = ControlTypeValueConverter.ConvertValueToInt(transFieldIdStr);

                    if (transFieldId.HasValue)
                    {
                        transactionExDto.DictAllTransactionField.ContainsKey(transFieldId.Value);
                        var fieldDto = transactionExDto.DictAllTransactionField[transFieldId.Value];
                        dictDynamicLabel[fieldDto.DataBaseFieldName + "___" + transFieldIdStr] = labelValue;
                    }

                }

                postDynamicData.DynamicLabel = dictDynamicLabel;

            }
        }

        private static string PrepareJsonData_ListEditDataModelGetApi(List<KeyValuePair<string, string>> parameters, AppIntergrationSettingParameterExDto dataExchangeSetting)
        {
            AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(dataExchangeSetting.TranscationId.Value);


            AppListDataDto formDataDto = AppListEditFormDataLoadBL.GetListEditFormData(dataExchangeSetting.TranscationId.Value);

            DataModelDateTimeConverterBL.ConvertListEditFromUtcToClient(formDataDto);


            dynamic dynamicData = new ExpandoObject();

            IDictionary<String, Object> dictRootData = ((IDictionary<String, Object>)dynamicData);

            dictRootData["ListData"] = new List<ExpandoObject>();


            foreach (AppChildDataDto childDataDto in formDataDto.ListData)
            {
                dynamic dynamicRowData = new ExpandoObject();
                object dictRowData = ((IDictionary<String, Object>)dynamicRowData);
                AppMasterDetailApiFormDataSaveBL.ConvertOneUnitDataRowToDictObject(childDataDto.DictOneToOneFields, (IDictionary<String, Object>)dictRowData);

                foreach (var childUnit in transactionExDto.RootMasterUnit.Children)
                {
                    string childUnitId = childUnit.Id.ToString();
                    string childUnitPath = childUnit.JsonPath;
                    string[] childUnitPaths = childUnitPath.Split(new string[] { "." }, StringSplitOptions.None);

                    var childUnitArrayNode = AppMasterDetailApiFormDataSaveBL.CreateArrayNodeFromPathLevel((IDictionary<String, Object>)dictRowData, childUnitPaths, 0, false);

                    foreach (var childDataRow in childDataDto.DictOneToManyFields[childUnitId])
                    {
                        ExpandoObject dictChildRowData = new ExpandoObject();
                        AppMasterDetailApiFormDataSaveBL.ConvertOneUnitDataRowToDictObject(childDataRow.DictOneToOneFields, (IDictionary<String, Object>)dictChildRowData);
                        ((List<ExpandoObject>)childUnitArrayNode).Add(dictChildRowData);
                    }
                }

                ((List<ExpandoObject>)dictRootData["ListData"]).Add(dynamicRowData);
            }

            GenerateLookupItemDataSourceNode(dataExchangeSetting, dictRootData);

            string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(dynamicData);

            return jsonData;

        }

        private static void GenerateLookupItemDataSourceNode(AppIntergrationSettingParameterExDto dataExchangeSetting, IDictionary<string, object> dictRootData)
        {
            dictRootData["LookupItems"] = new ExpandoObject();

            AppTransactionStructureDto transactionStructureDto = AppTransactionStructureLoadBL.GetAppTransactionStructureDto(dataExchangeSetting.TranscationId.Value);

            Dictionary<string, string> dictEntityIdAndCode = AppEntityInfoBL.RetrieveAllAppEntityInfoDto().ToDictionary(o => o.Id.ToString(), o => o.EntityCode);

            foreach (string entityId in transactionStructureDto.DictStandAloneEntityDataSource.Keys)
            {
                string entityCode = dictEntityIdAndCode[entityId];

                ((IDictionary<String, Object>)dictRootData["LookupItems"])[entityCode] = transactionStructureDto.DictStandAloneEntityDataSource[entityId];
            }
        }

        private static string ExecuteMasterDetailDataModelPostApi(string jsonData, AppIntergrationSettingParameterExDto dataExchangeSetting)
        {
            AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(dataExchangeSetting.TranscationId.Value);
            AppTransactionUnitExDto rootMasterUnit = transactionExDto.RootMasterUnit;

            AppMasterDetailDto formDataDto = AppMasterDetailFormDataLoadBL.GetNewFormData(dataExchangeSetting.TranscationId.Value);
            if (formDataDto != null && !string.IsNullOrWhiteSpace(jsonData))
            {
                Dictionary<string, object> dictMasterPkValues = new Dictionary<string, object>();

                foreach (string pk in rootMasterUnit.PrimaryKeyDbfieldList)
                {
                    dictMasterPkValues[pk] = rootMasterUnit.PrimaryKeyDbfieldList;
                }

                AppTransactionFieldExDto pkField = null;

                foreach (string key in dictMasterPkValues.Keys)
                {
                    pkField = rootMasterUnit.AppTransactionFieldList.FirstOrDefault(o => o.DataBaseFieldName == key);

                    if (pkField != null)
                    {
                        break;
                    }
                }


                var jObj = JObject.Parse(jsonData);

                // Special Params
                formDataDto.TransactionCommandId = ControlTypeValueConverter.ConvertValueToInt(jObj.SelectToken("TransactionCommandId"));


                //root unit
                Dictionary<string, object> dictOneToOneFields = AppMasterDetailApiFormDataLoadBL.MappingJsonFiledToTransField(rootMasterUnit, jObj);
                formDataDto.DictOneToOneFields = dictOneToOneFields;


                //sibling units
                foreach (var siblingUnit in transactionExDto.AppTransactionUnitList.Where(o => o.IsMasterSiblingUnit.HasValue && o.IsMasterSiblingUnit.Value))
                {
                    foreach (var fieldDto in siblingUnit.AppTransactionFieldList)
                    {
                        string jsonPath = siblingUnit.JsonPath + "." + fieldDto.JsonPath;
                        var value = jObj.SelectToken(jsonPath);
                        AppMasterDetailFormDataLoadBL.AssignOneFormFieldValueByType(formDataDto.DictSiblingOneToOneFields[siblingUnit.Id.ToString()], fieldDto, value);

                    }
                }


                //child and grandchild units
                foreach (AppTransactionUnitExDto childUnitExDto in rootMasterUnit.Children)
                {
                    List<AppChildDataDto> childAppformChildDataDto = null; ;

                    childAppformChildDataDto = AppMasterDetailApiFormDataLoadBL.SetupOneChildAndGrandChildData(childUnitExDto, jObj, "");


                    AppMasterDetailFormDataLoadBL.SetupStandAloneEntityDepedentFiled(childAppformChildDataDto, childUnitExDto);


                    formDataDto.DictOneToManyFields[childUnitExDto.Id.ToString()] = childAppformChildDataDto;
                }


                if (pkField != null)
                {
                    var pkValue = formDataDto.DictOneToOneFields[pkField.DataBaseFieldName];

                    if (pkValue != null && !string.IsNullOrWhiteSpace(pkValue.ToString()))
                    {
                        formDataDto.RootPrimaryKeyValue = pkValue.ToString();

                        formDataDto.IsNew = false;
                        formDataDto.IsDirty = true;
                    }
                }

                

                if (formDataDto.TransactionCommandId.HasValue)
                {
                    string jsonResult = Newtonsoft.Json.JsonConvert.SerializeObject("");
                    

                    var commandResult = AppTransactionCommandBL.ExecuteTransactionRootCommand(formDataDto);
                    if (commandResult.IsSuccessfulWithResult && commandResult.Object.FormData != null)
                    {
                        var formData = commandResult.Object.FormData;


                        DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(formData);
                        dynamic postDynamicData = ConvertMasterDetailFormDataToDynamicJsonObj(dataExchangeSetting, transactionExDto, formData);

                        jsonResult = Newtonsoft.Json.JsonConvert.SerializeObject(postDynamicData);
                    }     

                    return jsonResult;
                }
                else
                {
                    OperationCallResult<AppMasterDetailDto> saveFormResult = MasterDeatilDataCalculationAndSave(formDataDto);                    

                    string jsonResult = "";

                    foreach (var validationItem in saveFormResult.ValidationResult.Items)
                    {
                        jsonResult += validationItem.LocalizedMessage + "\n";
                    }

                    return Newtonsoft.Json.JsonConvert.SerializeObject(jsonResult);
                }


               
            }

            return "";
        }

        private static string ExecuteListEditDataModelPostApi(string jsonData, AppIntergrationSettingParameterExDto dataExchangeSetting)
        {
            AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(dataExchangeSetting.TranscationId.Value);
            AppTransactionUnitExDto rootMasterUnit = transactionExDto.RootMasterUnit;


            AppListDataDto formDataDto = AppListEditFormDataLoadBL.GetListEditFormData(dataExchangeSetting.TranscationId.Value);
            formDataDto.ListData.Clear();
            formDataDto.IsDirty = true;

            if (formDataDto != null && !string.IsNullOrWhiteSpace(jsonData))
            {
                var jObj = JObject.Parse(jsonData);

                JToken rootDataArrayNode = jObj.SelectToken("ListData");

                AppMasterDetailApiFormDataLoadBL.ProcessChildAndGrandchildDataRowsFromChildUnitArrayToken(rootMasterUnit, rootDataArrayNode, formDataDto.ListData);



                var saveFormResult = ListEditDataCalculationAndSave(formDataDto);


                string jsonResult = "";

                foreach (var validationItem in saveFormResult.ValidationResult.Items)
                {
                    jsonResult += validationItem.LocalizedMessage + "\n";
                }

                return Newtonsoft.Json.JsonConvert.SerializeObject(jsonResult);
            }

            return "";
        }

        private static OperationCallResult<AppMasterDetailDto> MasterDeatilDataCalculationAndSave(AppMasterDetailDto formDataDto)
        {
            DataModelDateTimeConverterBL.ConvertMasterDetailPostedUtcToClientForCalculation(formDataDto);

            var calculationResult = AppTransactionFormulaBL.ValidateAndCalculateMasterDetailTransactionData(formDataDto);

            if (!calculationResult.IsSuccessfulWithResult)
            {
                return calculationResult;
            }
            else
            {


                if (formDataDto != calculationResult.Object)
                {
                    formDataDto = calculationResult.Object;
                }

                formDataDto = DataModelDateTimeConverterBL.ConvertMasterDetailFromClientToUtc(formDataDto);

                var saveResult = AppMasterDetailFormDataSaveBL.SaveTransactionData(formDataDto);




                //if (saveResult.Object != null)
                //{
                //    DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(saveResult.Object);
                //}


                //if (calculationResult.ValidationResult.Items.FirstOrDefault(o => o.ItemType == ValidationItemType.Warning) != null)
                //{
                //    saveResult.ValidationResult.Merge(calculationResult.ValidationResult);

                //}



                return saveResult;
            }
        }


       


        private static OperationCallResult<AppListDataDto> ListEditDataCalculationAndSave(AppListDataDto formDataDto)
        {
            DataModelDateTimeConverterBL.ConvertListEditPostedUtcToClientForCalculation(formDataDto);

            OperationCallResult<AppListDataDto> validationResult = AppTransactionFormulaBL.ValidateListEditTransactionData(formDataDto);

            if (!validationResult.IsSuccessfulWithResult)
            {
                return validationResult;
            }
            else
            {

                OperationCallResult<AppListDataDto> saveResult = AppListEditFormDataLoadBL.SaveListEditFormData(formDataDto);

                //if (saveResult.Object != null)
                //{
                //    DataModelDateTimeConverterBL.ConvertListEditFromUtcToClient(saveResult.Object);
                //}

                return saveResult;
            }

        }


        private static string PrepareJsonData_SearchResultGetApi(List<KeyValuePair<string, string>> parameters, AppIntergrationSettingParameterExDto dataExchangeSetting)
        {
            int searchId = dataExchangeSetting.TranscationFieId.Value;
            string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject("");

            //AppSearchExDto searchExDto = AppSearchConfigBL.RetrieveOneAppSearchExDto(searchId);
            SearchDto searchDto = AppSearchBL.RetrieveOneSearchDto(searchId, false, false);
            ReferenceViewDto referenceViewDto = searchDto.DefaultView;

            foreach (var parameter in parameters)
            {
                if (!string.IsNullOrWhiteSpace(parameter.Value))
                {
                    var matchCriteria = searchDto.Criterias.FirstOrDefault(o => o.SysTableFiledPath.ToLower() == parameter.Key.ToLower());
                    if (matchCriteria != null)
                    {
                        matchCriteria.Value = parameter.Value;
                    }
                }
            }

            AppSearchBL.ConvertSearchCriteriaDateFromUTCToClient(searchDto);
            searchDto.ReferenceViewDefinitionDto = searchDto.DefaultView;
            SearchResultDto searchResultDto = AppSearchBL.RetrieveSearchResult(searchDto);

            //dynamic dynamicData = new ExpandoObject();
            List<ExpandoObject> rootListData = new List<ExpandoObject>();
            ConvertSearchResultRowsToJsonArray(referenceViewDto, searchResultDto.SearchResultRowList.ToList(), rootListData);

            jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(rootListData);

            return jsonData;
        }

        private static void ConvertSearchResultRowsToJsonArray(ReferenceViewDto referenceViewDto, List<StaticSearchResultRowJsonDto> searchResultRowList, List<ExpandoObject> rootListData)
        {
            foreach (StaticSearchResultRowJsonDto resultRowDto in searchResultRowList)
            {
                dynamic dynamicRowData = new ExpandoObject();

                IDictionary<String, Object> dictRowData = ((IDictionary<String, Object>)dynamicRowData);

                Dictionary<string, int> dictViewColumnPathAndCount = new Dictionary<string, int>();

                foreach (int viewColumnId in resultRowDto.DictViewColumnIDKeyValue.Keys)
                {
                    var viewColumnDto = referenceViewDto.Columns.FirstOrDefault(o => (int)o.Id == viewColumnId);

                    if (viewColumnDto != null)
                    {
                        string path = viewColumnDto.SysTableFiledPath;

                        if (!dictViewColumnPathAndCount.ContainsKey(path))
                        {
                            dictViewColumnPathAndCount.Add(path, 1);
                        }
                        else
                        {
                            dictViewColumnPathAndCount[path] = dictViewColumnPathAndCount[path] + 1;
                        }

                        int count = dictViewColumnPathAndCount[path];

                        if (count > 1)
                        {
                            path = path + "_" + count.ToString();
                        }

                        dictRowData[path] = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(resultRowDto.DictViewColumnIDKeyValue[viewColumnId]);

                        if (referenceViewDto.ViewType == (int)EmAppViewType.FlatDataSetTreeView)
                        {
                            if (viewColumnDto.IsTreeNodeId)
                            {
                                dictRowData["TreeNodeId"] = dictRowData[path];
                            }
                            if (viewColumnDto.IsTreeNodeDisplay)
                            {
                                dictRowData["TreeNodeDisplay"] = dictRowData[path];
                            }
                        }
                    }
                }

                if (resultRowDto.DictViewIdAndChildRowList != null)
                {
                    foreach (int childViewId in resultRowDto.DictViewIdAndChildRowList.Keys)
                    {
                        AppSearchViewEntity childViewEntity = AppSearchViewConfigBL.RetrieveOneAppSearchViewEntity(childViewId);
                        var childViewDto = AppSearchViewConfigBL.ConvertReverenceViewEntityToReferenceViewDto(childViewEntity);
                        string path = childViewDto.Display.Replace(" ", "") + "_" + childViewDto.Id;
                        dictRowData[path] = new List<ExpandoObject>();

                        var chlidSearchResultRows = resultRowDto.DictViewIdAndChildRowList[childViewId];

                        ConvertSearchResultRowsToJsonArray(childViewDto, chlidSearchResultRows, (List<ExpandoObject>)dictRowData[path]);

                    }
                }

                if (resultRowDto.Children != null)
                {
                    List<ExpandoObject> childListData = new List<ExpandoObject>();
                    ConvertSearchResultRowsToJsonArray(referenceViewDto, resultRowDto.Children.ToList(), childListData);

                    dynamicRowData.Children = childListData;
                }

                rootListData.Add(dynamicRowData);
            }
        }
    }
}
