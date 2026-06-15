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
using System.Text.RegularExpressions;
using System;
using DatabaseSchemaMrg.DataSchema;
using System.Data.SqlClient;
using DatabaseSchemaMrg;
//using APP.Persistence.Common;
using Newtonsoft.Json.Linq;
//using GrapeCity.Enterprise.Data.VisualBasicReplacement;

using APP.Framework;
namespace App.BL
{

    public static class AppTransactionTemplateDataLoadBL
    {

        public static AppMasterDetailDto LoadTransactionTemplateData(AppMasterDetailDto appformDataDto)
        {

            AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appformDataDto.TransactionId);
            var dictOneRowFiedIdValue = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(appformDataDto, appTransactionExDto);
            LanuchTransactionDataLoad(appformDataDto, appTransactionExDto, dictOneRowFiedIdValue);


            return appformDataDto;
        }

        public static AppMasterDetailDto LoadAutoExcutedTransactionTemplateData(AppMasterDetailDto appformDataDto, bool isAutoExecuteBeforeIntialCscading)
        {

            AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appformDataDto.TransactionId);
            List<AppTransactionDataLoadEntity> listLoad = new List<AppTransactionDataLoadEntity>();

            if (isAutoExecuteBeforeIntialCscading)
            {
                listLoad = AppTransactionTemplateDataLoadSetupBL.RetrieveOneTrasactioAllLoad(appformDataDto.TransactionId)
                .Where(o => o.IsAutoExcutedWhenOpenEditForm.HasValue && o.IsAutoExcutedWhenOpenEditForm.Value 
                        && o.IsAutoExecuteBeforeIntialCscading.HasValue && o.IsAutoExecuteBeforeIntialCscading.Value)
                .OrderBy(o => o.LoadOrder).ToList();
            }
            else
            {
                listLoad = AppTransactionTemplateDataLoadSetupBL.RetrieveOneTrasactioAllLoad(appformDataDto.TransactionId)
                 .Where(o => o.IsAutoExcutedWhenOpenEditForm.HasValue && o.IsAutoExcutedWhenOpenEditForm.Value 
                        && !(o.IsAutoExecuteBeforeIntialCscading.HasValue && o.IsAutoExecuteBeforeIntialCscading.Value))
                 .OrderBy(o => o.LoadOrder).ToList();
            }
           

            if (!listLoad.IsEmpty())
            {
                var dictOneRowFiedIdValue = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(appformDataDto, appTransactionExDto);
                foreach (var load in listLoad)
                {
                    PorcessOneDataLoad(load, appformDataDto, appTransactionExDto, dictOneRowFiedIdValue);
                }

                LanuchTransactionDataLoad(appformDataDto, appTransactionExDto, dictOneRowFiedIdValue);

                if (isAutoExecuteBeforeIntialCscading)
                {
                    AppMasterDetailFormDataLoadBL.SetupFormConditionLockingDictValue(appTransactionExDto, appformDataDto);
                }
            }



            return appformDataDto;
        }

        private static void LanuchTransactionDataLoad(AppMasterDetailDto appformDataDto, AppTransactionExDto appTransactionExDto, Dictionary<int, object> dictOneRowFiedIdValue)
        {
            List<AppTransactionDataLoadEntity> listLoad = AppTransactionTemplateDataLoadSetupBL.RetrieveOneTrasactioAllLoad(appformDataDto.TransactionId).OrderBy(o => o.LoadOrder).ToList();

            foreach (var load in listLoad)
            {
                PorcessOneDataLoad(load, appformDataDto, appTransactionExDto, dictOneRowFiedIdValue);

            }
        }

        //
        internal static void PorcessOneDataLoad(AppTransactionDataLoadEntity load, AppMasterDetailDto appformDataDto, AppTransactionExDto appTransactionExDto, Dictionary<int, object> dictRootAndSiblingFiedIdValue)
        {
            AppDataSetEntity dataSet = load.AppDataSet;

            if (dataSet.QueryType.HasValue && dataSet.QueryType.Value == (int)EmAppDataServiceType.IntegrationWebApiCall)
            {
                PorcessOneApiDataLoad(load, appformDataDto, appTransactionExDto, dictRootAndSiblingFiedIdValue, dataSet);
            }
            else
            {
                if (load.TransactionUnitId.ToString() == appTransactionExDto.RootMasterUnit.Id.ToString())
                {
                    string whereclause = GetFullQuey(load, dictRootAndSiblingFiedIdValue);

                    DataTable result = AppDataSetBL.FilterDataSet(load.AppDataSet, whereclause);

                    if (result.Rows.Count > 0)
                    {
                        var valueFileds = load.AppTranscationDataLoadFieldMapping.Where(o => o.IsConditionMapping != true).ToList();

                        foreach (var valueFied in valueFileds)
                        {
                            int transActiofiledId = valueFied.TransactionFieldId.Value;

                            if (dictRootAndSiblingFiedIdValue.ContainsKey(transActiofiledId))
                            {
                                dictRootAndSiblingFiedIdValue[transActiofiledId] = result.Rows[0][valueFied.DbcolumnName];
                            }
                            else
                            {
                                dictRootAndSiblingFiedIdValue.Add(transActiofiledId, result.Rows[0][valueFied.DbcolumnName]);
                            }
                        }

                        if (appformDataDto != null)
                        {
                            AppTransactionFormulaBL.UpdateRootAndSiblingDbFieldNameValueFromFiedIdValue(appformDataDto, appTransactionExDto, dictRootAndSiblingFiedIdValue);
                        }
                    }
                }
                else // it is child unit or grand child unit 
                {
                    // Child Unit
                    if (appformDataDto != null && appformDataDto.DictOneToManyFields.ContainsKey(load.TransactionUnitId.ToString()))
                    {
                        if (appTransactionExDto.DictAllTransactionUnitIdExDto != null && appTransactionExDto.DictAllTransactionUnitIdExDto.ContainsKey(load.TransactionUnitId.ToString()))
                        {
                            var childUnitExDto = appTransactionExDto.DictAllTransactionUnitIdExDto[load.TransactionUnitId.ToString()];
                            Dictionary<string, int> dictTransFieldDbNameAndId = childUnitExDto.AppTransactionFieldList.ToDictionary(o => o.DataBaseFieldName, o => (int)o.Id);

                            List<AppChildDataDto> childUnitRowList = appformDataDto.DictOneToManyFields[load.TransactionUnitId.ToString()];

                            foreach (AppChildDataDto childDataDto in childUnitRowList)
                            {
                                Dictionary<int, object> dictOneChildRowFiedIdAndValue = new Dictionary<int, object>();

                                foreach (var dataBaseFieldName in childDataDto.DictOneToOneFields.Keys)
                                {
                                    if (dictTransFieldDbNameAndId.ContainsKey(dataBaseFieldName))
                                    {
                                        dictOneChildRowFiedIdAndValue.Add(dictTransFieldDbNameAndId[dataBaseFieldName], childDataDto.DictOneToOneFields[dataBaseFieldName]);
                                    }
                                }

                                string whereclause = GetFullQuey(load, dictOneChildRowFiedIdAndValue);

                                DataTable result = AppDataSetBL.FilterDataSet(load.AppDataSet, whereclause);

                                if (result.Rows.Count > 0)
                                {
                                    var valueFileds = load.AppTranscationDataLoadFieldMapping.Where(o => o.IsConditionMapping != true).ToList();

                                    foreach (var valueFied in valueFileds)
                                    {
                                        int transActiofiledId = valueFied.TransactionFieldId.Value;

                                        if (dictOneChildRowFiedIdAndValue.ContainsKey(transActiofiledId))
                                        {
                                            dictOneChildRowFiedIdAndValue[transActiofiledId] = result.Rows[0][valueFied.DbcolumnName];
                                        }
                                    }

                                    AppTransactionFormulaBL.UpdateOneUnitDbFieldNameValueFromFiedIdValue(childUnitExDto, dictOneChildRowFiedIdAndValue, childDataDto.DictOneToOneFields);

                                }
                            }
                        }
                    }

                    // Grandchild Unit
                    // To Do
                }
            }





        }

        private static void PorcessOneApiDataLoad(AppTransactionDataLoadEntity load, AppMasterDetailDto appformDataDto, AppTransactionExDto appTransactionExDto, Dictionary<int, object> dictRootAndSiblingFiedIdValue, AppDataSetEntity dataSet)
        {
            int? apiOperationId = ControlTypeValueConverter.ConvertValueToInt(dataSet.QueryText);

            if (apiOperationId.HasValue)
            {
                var apiConfigDto = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(apiOperationId.Value);

                List<LookupItemDto> datasetQueryColumnList = AppDataSetBL.RetrieveQueryColumnList(load.DataSetId.Value);
                Dictionary<string, string> dictInputParamKeyAndValue = datasetQueryColumnList.Where(o => o.ItemType == 1).ToDictionary(o => o.Id.ToString(), o => "");


                var condiftionFileds = load.AppTranscationDataLoadFieldMapping.Where(o => o.IsConditionMapping == true).ToList();

                if (load.TransactionUnitId.ToString() == appTransactionExDto.RootMasterUnit.Id.ToString()) // root or sibling units
                {

                    Dictionary<string, string> dictOutputParamKeyAndValue = new Dictionary<string, string>();

                    Dictionary<string, List<string>> dictApiParamValuesAndResponse = new Dictionary<string, List<string>>();

                    PorcessOneApiDataLoad_ProcessOneUnitRow(load, dictRootAndSiblingFiedIdValue, dataSet, apiOperationId, apiConfigDto, dictInputParamKeyAndValue, dictOutputParamKeyAndValue, condiftionFileds, dictApiParamValuesAndResponse);

                    if (appformDataDto != null)
                    {
                        AppTransactionFormulaBL.UpdateRootAndSiblingDbFieldNameValueFromFiedIdValue(appformDataDto, appTransactionExDto, dictRootAndSiblingFiedIdValue);
                    }
                }
                else // it is child unit or grand child unit 
                {
                    // Child Unit

                    Dictionary<string, List<string>> dictApiParamValuesAndResponse = new Dictionary<string, List<string>>();

                    if (appformDataDto != null && appformDataDto.DictOneToManyFields.ContainsKey(load.TransactionUnitId.ToString()))
                    {
                        if (appTransactionExDto.DictAllTransactionUnitIdExDto != null && appTransactionExDto.DictAllTransactionUnitIdExDto.ContainsKey(load.TransactionUnitId.ToString()))
                        {
                            var childUnitExDto = appTransactionExDto.DictAllTransactionUnitIdExDto[load.TransactionUnitId.ToString()];
                            Dictionary<string, int> dictTransFieldDbNameAndId = childUnitExDto.AppTransactionFieldList.ToDictionary(o => o.DataBaseFieldName, o => (int)o.Id);

                            List<AppChildDataDto> childUnitRowList = appformDataDto.DictOneToManyFields[load.TransactionUnitId.ToString()];

                            foreach (AppChildDataDto childDataDto in childUnitRowList)
                            {
                                Dictionary<int, object> dictOneChildRowFiedIdAndValue = new Dictionary<int, object>();

                                foreach (var dataBaseFieldName in childDataDto.DictOneToOneFields.Keys)
                                {
                                    dictOneChildRowFiedIdAndValue.Add(dictTransFieldDbNameAndId[dataBaseFieldName], childDataDto.DictOneToOneFields[dataBaseFieldName]);
                                }


                                Dictionary<string, string> dictOutputParamKeyAndValue = new Dictionary<string, string>();

                                PorcessOneApiDataLoad_ProcessOneUnitRow(load, dictOneChildRowFiedIdAndValue, dataSet, apiOperationId, apiConfigDto, dictInputParamKeyAndValue, dictOutputParamKeyAndValue, condiftionFileds, dictApiParamValuesAndResponse);

                                AppTransactionFormulaBL.UpdateOneUnitDbFieldNameValueFromFiedIdValue(childUnitExDto, dictOneChildRowFiedIdAndValue, childDataDto.DictOneToOneFields);


                            }
                        }
                    }

                    // Grandchild Unit
                    // To Do
                }



            }
        }

        private static void PorcessOneApiDataLoad_ProcessOneUnitRow(AppTransactionDataLoadEntity load, Dictionary<int, object> dictRowFiedIdAndValue, AppDataSetEntity dataSet, int? apiOperationId, AppIntergrationSettingParameterExDto apiConfigDto, Dictionary<string, string> dictInputParamKeyAndValue, Dictionary<string, string> dictOutputParamKeyAndValue, List<AppTranscationDataLoadFieldMappingEntity> condiftionFileds, Dictionary<string, List<string>> dictApiParamValuesAndResponse)
        {
            foreach (var condition in condiftionFileds)
            {
                if (!string.IsNullOrWhiteSpace(condition.DbcolumnName) && condition.TransactionFieldId.HasValue)
                {
                    if (dictRowFiedIdAndValue.ContainsKey(condition.TransactionFieldId.Value))
                    {
                        string paramKey = condition.DbcolumnName;
                        string paramValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dictRowFiedIdAndValue[condition.TransactionFieldId.Value]);

                        if (dictInputParamKeyAndValue.ContainsKey(paramKey))
                        {
                            dictInputParamKeyAndValue[paramKey] = paramValue;
                        }
                        else
                        {
                            if (!dictOutputParamKeyAndValue.ContainsKey(paramKey))
                            {
                                dictOutputParamKeyAndValue.Add(paramKey, paramValue);
                            }
                            else
                            {
                                dictOutputParamKeyAndValue[paramKey] = paramValue;
                            }
                        }
                    }
                }
            }

            Dictionary<string, string> headers = new Dictionary<string, string>();
            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            Dictionary<string, string> pathParams = new Dictionary<string, string>();
            Dictionary<string, string> environmentVairables = new Dictionary<string, string>();
            
            string apiParamValues_ConcatStr = "";

            foreach (string paramKey in dictInputParamKeyAndValue.Keys)
            {
                if (apiConfigDto.APIConfigParameters.PathParams.ContainsKey(paramKey)
                    || apiConfigDto.APIConfigParameters.QueryParams.ContainsKey(paramKey))
                {
                    string paramValue = dictInputParamKeyAndValue[paramKey];

                    apiParamValues_ConcatStr += paramValue + "|";

                    if (!string.IsNullOrWhiteSpace(paramValue))
                    {
                        pathParams[paramKey] = paramValue;
                        queryParams[paramKey] = paramValue;
                    }
                }
            }

            List<string> lstResponse = null;

            if (dictApiParamValuesAndResponse.ContainsKey(apiParamValues_ConcatStr))
            {
                lstResponse = dictApiParamValuesAndResponse[apiParamValues_ConcatStr];
            }
            else
            {
                lstResponse = AppMasterDetailApiFormDataLoadBL.GetDataFromIntegrationSettingApiCallAsync(apiOperationId.Value, headers, queryParams, pathParams, environmentVairables).Result;
                dictApiParamValuesAndResponse.Add(apiParamValues_ConcatStr, lstResponse);
            }


            if (lstResponse != null && lstResponse.Count > 0)
            {
                string jsonData = lstResponse[0];
                var jObj = JObject.Parse(jsonData);

                JToken arrayJtokenArray = null;

                string rootArrayJsonNodePath = dataSet.BaseTableName;

                if (!string.IsNullOrWhiteSpace(rootArrayJsonNodePath))
                {
                    arrayJtokenArray = jObj.SelectToken(rootArrayJsonNodePath);
                }
                else
                {
                    arrayJtokenArray = (JToken)jObj;
                }


                if (dataSet.UsageTypeId.HasValue && dataSet.UsageTypeId.Value == (int)EmAppDataSetUsageType.ConvertSimpleObjectToList)
                {
                    if (arrayJtokenArray.Type == JTokenType.Object)
                    {
                        foreach (JToken childNode in arrayJtokenArray.Children())
                        {
                            if (childNode.Type == JTokenType.Property)
                            {
                                string key = ((JProperty)childNode).Name;
                                string value = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(((JProperty)childNode).Value);

                                JObject keyValueJObj = new JObject();
                                keyValueJObj.Add("Key", key);
                                keyValueJObj.Add("Value", value);

                                PorcessOneApiDataLoad_PrepareResult(load, dictOutputParamKeyAndValue, (JToken)keyValueJObj, dictRowFiedIdAndValue);
                            }
                        }
                    }
                }
                else
                {
                    if (arrayJtokenArray.Type == JTokenType.Array)
                    {
                        foreach (JToken rowJtoken in arrayJtokenArray.Children())
                        {
                            PorcessOneApiDataLoad_PrepareResult(load, dictOutputParamKeyAndValue, rowJtoken, dictRowFiedIdAndValue);

                        }
                    }
                    else if (arrayJtokenArray.Type == JTokenType.Object)
                    {
                        JToken rowJtoken = arrayJtokenArray;
                        PorcessOneApiDataLoad_PrepareResult(load, dictOutputParamKeyAndValue, rowJtoken, dictRowFiedIdAndValue);

                    }
                }
            }
        }

        private static void PorcessOneApiDataLoad_PrepareResult(AppTransactionDataLoadEntity load, Dictionary<string, string> dictOutputParamKeyAndValue, JToken rowJtoken, Dictionary<int, object> dictRowFiedIdAndValue)
        {
            Dictionary<string, string> dictMappingKeyAndValue = new Dictionary<string, string>();

            foreach (string paramKey in dictOutputParamKeyAndValue.Keys)
            {
                string paramValue = dictOutputParamKeyAndValue[paramKey];
                string nodevalue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(rowJtoken.SelectToken(paramKey));

                if (paramValue != nodevalue)
                {
                    return;
                }
            }

            var valueFileds = load.AppTranscationDataLoadFieldMapping.Where(o => o.IsConditionMapping != true).ToList();

            foreach (var mappingDto in valueFileds)
            {
                if (!dictMappingKeyAndValue.ContainsKey(mappingDto.DbcolumnName) && mappingDto.TransactionFieldId.HasValue)
                {
                    string nodevalue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(rowJtoken.SelectToken(mappingDto.DbcolumnName));

                    dictRowFiedIdAndValue[mappingDto.TransactionFieldId.Value] = nodevalue;
                }
            }
        }

        private static string GetFullQuey(AppTransactionDataLoadEntity load, Dictionary<int, object> dictOneRowFiedIdValue)
        {
            AppDataSetEntity dataSet = load.AppDataSet;

            var dbFixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSet.DataSourceFrom.Value);

            var condiftionFileds = load.AppTranscationDataLoadFieldMapping.Where(o => o.IsConditionMapping == true).ToList();



            string whereConditionField = string.Empty;


            foreach (var condition in condiftionFileds)
            {

                if (!string.IsNullOrWhiteSpace(condition.DbcolumnName) && condition.TransactionFieldId.HasValue)
                {
                    if (dictOneRowFiedIdValue.ContainsKey(condition.TransactionFieldId.Value))
                    {
                        string fullbcolumnName = AppMetaDataBL.GetQulifiedTableFiledName(condition.DbcolumnName, dbFixture.SqlServerType.Value);

                        string dbFieldCondtion = string.Format("{0}='{1}' AND", fullbcolumnName, dictOneRowFiedIdValue[condition.TransactionFieldId.Value]);
                        whereConditionField = whereConditionField + dbFieldCondtion;
                    }


                }

            }

            if (whereConditionField != string.Empty)
            {
                whereConditionField = whereConditionField.Substring(0, whereConditionField.Length - 3);
            }


            string whereFreeTextClause = string.Empty;
            foreach (var condition in condiftionFileds)
            {



                if (!string.IsNullOrWhiteSpace(condition.WhereClause))
                {

                    foreach (int transactionFieldId in dictOneRowFiedIdValue.Keys)
                    {
                        //\b{0}\b

                        string tranActionFieldTokey = string.Format("{0}{1}", AppTransactionTemplateDataLoadSetupBL.TransactionFieldFormulaPrefix, transactionFieldId.ToString());
                        string matchSubItem = string.Format(@"\b{0}\b", tranActionFieldTokey);

                        if (Regex.IsMatch(condition.WhereClause, matchSubItem)) //; conditionWhereClause.Contains(matchSubItem))
                        {

                            //string sqlTextExpress = ControlTypeValueConverter.ConvertDataTableSelectFiledString(dictBlockSubitemValue[subItmeId], dictSubItemControType[subItmeId]);

                            condition.WhereClause = condition.WhereClause.Replace(tranActionFieldTokey, string.Format("'{0}'", dictOneRowFiedIdValue[transactionFieldId]));


                        }


                    }

                    whereFreeTextClause = whereFreeTextClause + condition.WhereClause + " AND";

                }

            }

            if (whereFreeTextClause != string.Empty)
            {
                whereFreeTextClause = whereFreeTextClause.Substring(0, whereFreeTextClause.Length - 3);
            }




            if (string.IsNullOrWhiteSpace(whereConditionField))
            {
                return whereFreeTextClause;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(whereFreeTextClause))
                {
                    return whereConditionField;
                }
                else
                {
                    return string.Format("{0} AND {1}", whereConditionField, whereFreeTextClause);

                }


            }


        }
    }


}