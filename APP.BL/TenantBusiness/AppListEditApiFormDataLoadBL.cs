using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
//using APP.Persistence.Common;
using DatabaseSchemaMrg;
using DatabaseSchemaMrg.DataSchema;
using ExchangeBL;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SD.LLBLGen.Pro.ORMSupportClasses;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
#if NETFRAMEWORK
using System.Management.Automation.Language;
#endif

namespace App.BL
{
    public static class AppListEditApiFormDataLoadBL
    {
        public static AppListDataDto GetApiListEditFormData(int transactionId, List<object> rootIdList = null, string transactionJsonData = null)
        {
            AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);

            AppChildDataDto cloneAppChildDataDto = AppListEditFormDataLoadBL.GetListCloneEditRow(transactionExDto);

            AppListDataDto aAppformDataDto = GetOneApiListEditTransactionAllData(transactionExDto, rootIdList, transactionJsonData);

            aAppformDataDto.EditCloneAppChildDataDto = cloneAppChildDataDto;

            AppListEditFormDataLoadBL.ProcessListEditFormFileIDCodeDictionary(transactionExDto, aAppformDataDto);

            return aAppformDataDto;
        }



        private static AppListDataDto GetOneApiListEditTransactionAllData(AppTransactionExDto transactionExDto, List<object> rootIdList, string transactionJsonData = null)
        {
            int TransactionId = (int)transactionExDto.Id;
            AppListDataDto rootAppformDataDto = new AppListDataDto();
            rootAppformDataDto.TransactionId = TransactionId;
            rootAppformDataDto.ListData = new List<AppChildDataDto>();
            AppTransactionUnitExDto rootMasterUnit = transactionExDto.RootMasterUnit;

            string jsonData = "";

            if (transactionJsonData != null)
            {
                jsonData = transactionJsonData;
            }
            else
            {
                if (transactionExDto.BaseApiConfigDto != null && transactionExDto.BaseApiConfigDto.Id != null)
                {
                    Dictionary<string, string> headers = new Dictionary<string, string>();
                    Dictionary<string, string> queryParams = new Dictionary<string, string>();
                    Dictionary<string, string> pathParams = new Dictionary<string, string>();
                    Dictionary<string, string> environmentVairables = new Dictionary<string, string>();
                    List<string> lstResponse = AppMasterDetailApiFormDataLoadBL.GetDataFromIntegrationSettingApiCallAsync((int)transactionExDto.BaseApiConfigDto.Id, headers, queryParams, pathParams, environmentVairables).Result;

                    if (lstResponse.Count > 0)
                    {


                        jsonData = lstResponse[0];
                    }
                }
            }

            

            var jToken = JToken.Parse(jsonData);
            List<AppChildDataDto> listData = null;


            if (jToken.Type == JTokenType.Array)
            {
                listData = new List<AppChildDataDto>();
                AppMasterDetailApiFormDataLoadBL.ProcessChildAndGrandchildDataRowsFromChildUnitArrayToken(rootMasterUnit, jToken, listData);
            }
            else
            {
                JObject jObj = (JObject)jToken;
                listData = AppMasterDetailApiFormDataLoadBL.SetupOneChildAndGrandChildData(rootMasterUnit, jObj, null);
            }

            if (transactionJsonData == null && rootIdList != null && rootIdList.Count > 0)
            {
                if (rootMasterUnit.PrimaryKeyDbfieldList.Count > 0)
                {
                    rootAppformDataDto.ListData = new List<AppChildDataDto>();
                    List<string> rootIdListStr = rootIdList.Select(o => o.ToString().Trim()).Distinct().Where(o => !string.IsNullOrWhiteSpace(o)).ToList();

                    string pkDbName = rootMasterUnit.PrimaryKeyDbfieldList[0];

                    if (!string.IsNullOrWhiteSpace(pkDbName))
                    {
                        foreach (AppChildDataDto aDataRow in listData)
                        {
                            string rowPkValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(aDataRow.DictOneToOneFields[pkDbName]).Trim();

                            if (!string.IsNullOrWhiteSpace(rowPkValue) && rootIdListStr.Contains(rowPkValue))
                            {
                                rootAppformDataDto.ListData.Add(aDataRow);
                            }
                        }
                    }
                }
            }
            else
            {
                rootAppformDataDto.ListData = listData;
            }


            return rootAppformDataDto;
        }

        public static OperationCallResult<AppListDataDto> SaveApiListEditFormData(AppListDataDto appListDataDto)
        {

            OperationCallResult<AppListDataDto> aOperationCallResult = new OperationCallResult<AppListDataDto>();

            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            aValidationResult.Merge(AppTransactionFormulaBL.ValidateListEditTransactionData(appListDataDto).ValidationResult);

            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = appListDataDto;
                return aOperationCallResult;
            }


            AppTransactionExDto aAppTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appListDataDto.TransactionId);

            AppTransactionStructureDto transactionStructureDto = AppTransactionStructureLoadBL.GetGeneralStrcutureInfo(aAppTransactionExDto);




            if (aAppTransactionExDto.BaseApiConfigDto != null)                
            {
                AppTransactionStructureDto aAppMasterDetailStructureDto = AppTransactionStructureLoadBL.GetGeneralStrcutureInfo(aAppTransactionExDto);



                CallApiToPostListEditData(appListDataDto, aAppTransactionExDto, aValidationResult);



                if (!aValidationResult.HasErrors)
                {

                    //if (rootClientAppformDataDto.DataTransferSettingId.HasValue && rootClientAppformDataDto.DataTransferFromMasterDetailDto != null
                    //    && !string.IsNullOrWhiteSpace(rootClientAppformDataDto.ApiResponse))
                    //{
                    //    AppTransactionDataTransferBL.ProcessApiResponseDataTransfer(rootClientAppformDataDto.DataTransferSettingId.Value, rootClientAppformDataDto.DataTransferFromMasterDetailDto, rootClientAppformDataDto.ApiResponse);
                    //}

                    //var freshDBAppformDataDto = AppMasterDetailApiFormDataLoadBL.GetApiMasterDetailFormData((int)aAppTransactionExDto.Id, rootPkValue);

                    //if (freshDBAppformDataDto != null)
                    //{
                    //    freshDBAppformDataDto.ApiResponse = rootClientAppformDataDto.ApiResponse;
                    //    freshDBAppformDataDto.DataTransferFromMasterDetailDto = rootClientAppformDataDto.DataTransferFromMasterDetailDto;

                    //    aOperationCallResult.Object = freshDBAppformDataDto;
                    //}
                    //else
                    //{
                    //    aOperationCallResult.Object = rootClientAppformDataDto;
                    //}

                    //if (!string.IsNullOrWhiteSpace(aOperationCallResult.Object.ApiResponse))
                    //{
                    //    aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Exception_Error", ValidationItemType.Message, "API Response: \n" + aOperationCallResult.Object.ApiResponse));
                    //}


                }
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Exception_Error", ValidationItemType.Error, "Cannot Find Update Data Model API Operation."));
            }



            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = GetApiListEditFormData(appListDataDto.TransactionId);
            }

            return aOperationCallResult;
        }


        private static void CallApiToPostListEditData(AppListDataDto appListDataDto, AppTransactionExDto transactionExDto, ValidationResult aValidationResult)
        {


            var rootUnit = transactionExDto.RootMasterUnit;
            bool isRootUnitimpleList = rootUnit.IsExclusiveForOwner.HasValue && rootUnit.IsExclusiveForOwner.Value;
            List<string> rootUnitTempFieldKeys = rootUnit.AppTransactionFieldList.Where(o => o.IsTempVariable.HasValue && o.IsTempVariable.Value).Select(o => o.DataBaseFieldName).ToList();

            string rootUnitPath = rootUnit.JsonPath;

            object rootUnitArrayNode = new List<ExpandoObject>();

            dynamic postDynamicData = null;

            if (string.IsNullOrWhiteSpace(rootUnitPath))
            {
                postDynamicData = new List<ExpandoObject>();
                rootUnitArrayNode = postDynamicData;
            }
            else
            {
                postDynamicData = new ExpandoObject();
                var postRootData = ((IDictionary<String, Object>)postDynamicData);

                string[] path = rootUnitPath.Split(new string[] { "." }, StringSplitOptions.None);
                AppMasterDetailApiFormDataSaveBL.CreateDictNodeFromPathLevel(postRootData, path, 0);



                rootUnitArrayNode = AppMasterDetailApiFormDataSaveBL.CreateArrayNodeFromPathLevel(postRootData, path, 0, isRootUnitimpleList);
            }

            if (rootUnitArrayNode != null)
            {


                foreach (var childDataRow in appListDataDto.ListData)
                {
                    ExpandoObject dictChildRowData = new ExpandoObject();

                    if (isRootUnitimpleList)
                    {
                        string simpleValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(childDataRow.DictOneToOneFields.Values.FirstOrDefault());
                        ((List<object>)rootUnitArrayNode).Add(simpleValue);
                    }
                    else
                    {
                        AppMasterDetailApiFormDataSaveBL.ConvertOneUnitDataRowToDictObject(childDataRow.DictOneToOneFields, (IDictionary<String, Object>)dictChildRowData, rootUnitTempFieldKeys);
                        ((List<ExpandoObject>)rootUnitArrayNode).Add(dictChildRowData);

                        foreach (var grandchildUnit in rootUnit.Children)
                        {
                            List<string> grandchildUnitTempFieldKeys = grandchildUnit.AppTransactionFieldList.Where(o => o.IsTempVariable.HasValue && o.IsTempVariable.Value).Select(o => o.DataBaseFieldName).ToList();

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
                                    AppMasterDetailApiFormDataSaveBL.ConvertOneUnitDataRowToDictObject(gcDataRow.DictOneToOneFields, (IDictionary<String, Object>)dictGcRowData, grandchildUnitTempFieldKeys);
                                    ((List<ExpandoObject>)gcUnitArrayNode).Add(dictGcRowData);
                                }
                            }
                        }
                    }
                }
            }


            Dictionary<string, string> headers = new Dictionary<string, string>();
            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            Dictionary<string, string> pathParams = new Dictionary<string, string>();
            Dictionary<string, string> environmentVairables = new Dictionary<string, string>();

            string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(postDynamicData);
            AppIntergrationSettingParameterExDto dataExchangeSettingDTO = DataExchangeSettingBL.GetSetting((int)transactionExDto.BaseApiConfigDto.Id);
            List<string> listResponse = AppMasterDetailApiFormDataSaveBL.CallApiWithJsonDataByIntegrationSettingApiCallAsync(dataExchangeSettingDTO, headers, queryParams, pathParams, environmentVairables, jsonData).Result;

            if (listResponse.Count > 0)
            {
                //formDataDto.ApiResponse = listResponse[0];
            }
        }


    }
}