using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.LBL.DatabaseSpecific;
using DatabaseSchemaMrg;
using DatabaseSchemaMrg.DataSchema;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Dynamic;
using Newtonsoft.Json;
using ExchangeBL;
using System.Threading.Tasks;
using Twilio.Http;
using APP.Framework.Collections;

using APP.Framework;
namespace App.BL
{
    public static class AppMasterDetailApiFormDataLoadBL
    {
        public static readonly string TransactionId = "TransactionId";
        public static readonly string ApiRootKeyPrefix = "api__";
        public static readonly string ApiRootKeySplit = "|";
        public static readonly string ApiRootKeyAndValueSplit = ":";
        public static readonly string ApiPathParamPrefix = "Path Parameter - ";
        public static readonly string ApiQueryParamPrefix = "Query Parameter - ";
        public static readonly string ApiEvironmentVariablePrefix = "Evironment Variable - ";

        public static AppMasterDetailDto GetApiMasterDetailFormData(int transactionId, object rootPrimaryKeyValue)
        {
            AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);


            AppMasterDetailDto aAppformDataDto = null;

            Dictionary<string, object> dictMasterPkValues = new Dictionary<string, object>();

            if (transactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.MasterDetail)
            {

                if (transactionExDto.BaseApiConfigDto != null && transactionExDto.BaseApiConfigDto.Id != null)
                {

                    AppTransactionUnitExDto rootMasterUnit = transactionExDto.RootMasterUnit;

                    GetDictMasterPkAndValueFromApiRootKeyConcatString(rootPrimaryKeyValue, transactionExDto, dictMasterPkValues);


                 

                    aAppformDataDto = PopulateApiMasterDetailFormData(transactionExDto, dictMasterPkValues);

                    aAppformDataDto.TransactionId = transactionId;
                    aAppformDataDto.RootPrimaryKeyValue = rootPrimaryKeyValue;
                    aAppformDataDto.FormID = transactionExDto.FormId;
                    aAppformDataDto.IsApiIntegrationTransaction = true;

                    aAppformDataDto.DictCascadingFiledDataSource = new Dictionary<string, List<LookupItemDto>>();
                    //aAppformDataDto.DictAutoCompleteFieldDataSource = new Dictionary<string, List<LookupItemDto>>();
                    aAppformDataDto.DictCascadingFieldIdAndLabel = new Dictionary<string, string>();

                    AppTransactionTemplateDataLoadBL.LoadAutoExcutedTransactionTemplateData(aAppformDataDto, true);

                    AppCascadingBL.SetupIntialCscadingFieldDataSource(transactionExDto, aAppformDataDto, false);

                    foreach (var fieldDtoDto in transactionExDto.DictAllTransactionField.Values)
                    {
                        if (!fieldDtoDto.AppConditionalAction__List.IsEmpty())
                        {
                            aAppformDataDto.IsChangedNeedToCascadingFiedIds.Add(fieldDtoDto.Id.ToString());
                        }
                    }

                    AppCascadingBL.SetupIntialAutoCompleteFieldDataSource(transactionExDto, aAppformDataDto, false);
                }
                else
                {

                }
            }

            if (aAppformDataDto != null)
            {
                aAppformDataDto.DictDocumentIdFileCode = new Dictionary<int, string>();
                aAppformDataDto.DictFolderIdAndPath = new Dictionary<int, string>();


                aAppformDataDto.RootPrimaryKeyValue = rootPrimaryKeyValue;

                if (transactionExDto.RootMasterUnit != null)
                {
                    var titleDisplayField = transactionExDto.RootMasterUnit.AppTransactionFieldList.OrderBy(o => o.SortOrder).FirstOrDefault(o => o.ControlType == (int)EmAppControlType.TextBox);
                    if (titleDisplayField != null)
                    {
                        string filedDbName = titleDisplayField.DataBaseFieldName;
                        aAppformDataDto.FormTitleDisplay = aAppformDataDto.DictOneToOneFields[filedDbName];
                    }
                }
                AppTransactionTemplateDataLoadBL.LoadAutoExcutedTransactionTemplateData(aAppformDataDto, false);
            }



            return aAppformDataDto;
        }

        public static void GetDictMasterPkAndValueFromApiRootKeyConcatString(object rootKeyConcatStringObj, AppTransactionExDto transactionExDto, Dictionary<string, object> dictMasterPkValues)
        {
            string rootKeyConcatString = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(rootKeyConcatStringObj);

            if (!string.IsNullOrWhiteSpace(rootKeyConcatString))
            {
                if (transactionExDto.ApiInputParameterList != null && transactionExDto.ApiInputParameterList.Count > 0)
                {
                    if (rootKeyConcatString.StartsWith(ApiRootKeyPrefix))
                    {
                        List<string> rootKeyPartList = rootKeyConcatString.Substring(ApiRootKeyPrefix.Length).Split(ApiRootKeySplit.ToArray()).ToList();

                        foreach (string rootKeyPart in rootKeyPartList)
                        {
                            if (!string.IsNullOrWhiteSpace(rootKeyPart))
                            {
                                List<string> rootKeyNameAndValue = rootKeyPart.Split(ApiRootKeyAndValueSplit.ToArray()).ToList();

                                if (rootKeyNameAndValue.Count == 2)
                                {
                                    string rootKeyName = rootKeyNameAndValue[0];
                                    string rootKeyValue = rootKeyNameAndValue[1];

                                    if (!string.IsNullOrWhiteSpace(rootKeyName))
                                    {
                                        dictMasterPkValues[rootKeyName] = rootKeyValue;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static async Task<List<string>> GetDataFromIntegrationSettingApiCallAsync(int actionId, Dictionary<string, string> headers, Dictionary<string, string> queryParams, Dictionary<string, string> pathParams, Dictionary<string, string> environmentVairables)
        {
            AppIntergrationSettingParameterExDto dataExchangeSettingDTO = DataExchangeSettingBL.GetSetting(actionId);

            string strSchema = dataExchangeSettingDTO.SchemaFromDataSetMapping;

            if (headers != null && headers.Count > 0)
            {
                foreach (KeyValuePair<string, string> oneKV in headers)
                {
                    dataExchangeSettingDTO.APIConfigParameters.Headers.Add(oneKV.Key, oneKV.Value);
                }
            }

            if (pathParams != null && pathParams.Count > 0)
            {
                foreach (KeyValuePair<string, string> oneKV in pathParams)
                {
                    if (dataExchangeSettingDTO.APIConfigParameters.PathParams.ContainsKey(oneKV.Key))
                    {
                        dataExchangeSettingDTO.APIConfigParameters.PathParams[oneKV.Key] = oneKV.Value;
                    }
                    else
                    {
                        //dataExchangeSettingDTO.APIConfigParameters.PathParams.Add(oneKV.Key, oneKV.Value);
                    }

                }
            }

            if (queryParams != null && queryParams.Count > 0)
            {
                foreach (KeyValuePair<string, string> oneKV in queryParams)
                {
                    if (dataExchangeSettingDTO.APIConfigParameters.QueryParams.ContainsKey(oneKV.Key))
                    {
                        dataExchangeSettingDTO.APIConfigParameters.QueryParams[oneKV.Key] = oneKV.Value;
                    }
                    else
                    {
                        //dataExchangeSettingDTO.APIConfigParameters.QueryParams.Add(oneKV.Key, oneKV.Value);
                    }
                }
            }

            dataExchangeSettingDTO.APIConfigParameters.DictOverrideEnvionmentVariable = environmentVairables;

            List<string> lstResponse = await Helper.CallAPIAsync(dataExchangeSettingDTO.APIConfigParameters, null, dataExchangeSettingDTO.IntergrationSettingId).ConfigureAwait(false);

            return lstResponse;
        }


        internal static AppMasterDetailDto PopulateApiMasterDetailFormData(AppTransactionExDto appTransactionExDto, Dictionary<string, object> rootPrimaryKeyValue)
        {
            int TransactionId = (int)appTransactionExDto.Id;
            AppMasterDetailDto masterDetailDto = new AppMasterDetailDto();
            masterDetailDto.TransactionId = TransactionId;
            masterDetailDto.IsShowSaveButton = appTransactionExDto.IsShowSaveButton;

            // only one root, and root only pass one root key value
            var rootMasterUnit = appTransactionExDto.AppTransactionUnitList.FirstOrDefault();

            if (rootMasterUnit != null)
            {
                masterDetailDto.RootUnitId = rootMasterUnit.Id;
            }

            masterDetailDto.DictOneToManyFields = new Dictionary<string, List<AppChildDataDto>>();
            masterDetailDto.DictSiblingOneToOneFields = new Dictionary<string, Dictionary<string, object>>();

            //// to add new collection view
            AppMasterDetailDto newEmptyAppformDataDto = GetNewFormData(appTransactionExDto);
            masterDetailDto.EditCloneDictOneToManyFields = newEmptyAppformDataDto.EditCloneDictOneToManyFields;



            Dictionary<string, string> headers = new Dictionary<string, string>();
            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            Dictionary<string, string> pathParams = new Dictionary<string, string>();
            Dictionary<string, string> environmentVairables = new Dictionary<string, string>();

            AssignApiParameterFromDictRootPrimaryKeyValue(rootPrimaryKeyValue, queryParams, pathParams, environmentVairables);

            List<string> lstResponse = GetDataFromIntegrationSettingApiCallAsync((int)appTransactionExDto.BaseApiConfigDto.Id, headers, queryParams, pathParams, environmentVairables).Result;

            if (lstResponse.Count > 0)
            {
                string jsonData = lstResponse[0];
                //string strSchema = appTransactionExDto.ApiConfigRead.SchemaFromDataSetMapping;

                var jObj = JObject.Parse(jsonData);

                if (rootMasterUnit != null)
                {
                    SetupRootUnitDictValueFromJObject(masterDetailDto, rootMasterUnit, jObj);





                    foreach (AppTransactionUnitExDto aChildTransactionUnitExDto in rootMasterUnit.Children)
                    {
                        List<AppChildDataDto> childAppformChildDataDto = null; ;

                        childAppformChildDataDto = SetupOneChildAndGrandChildData(aChildTransactionUnitExDto, jObj, rootMasterUnit.JsonPath);

                        if (childAppformChildDataDto.Count == 0 && aChildTransactionUnitExDto.IsVirtualUnit)
                        {
                            childAppformChildDataDto = newEmptyAppformDataDto.DictOneToManyFields[aChildTransactionUnitExDto.Id.ToString()].DeepCopy();
                        }



                        AppMasterDetailFormDataLoadBL.SetupStandAloneEntityDepedentFiled(childAppformChildDataDto, aChildTransactionUnitExDto);


                        masterDetailDto.DictOneToManyFields.Add(aChildTransactionUnitExDto.Id.ToString(), childAppformChildDataDto);
                    }
                }
            }







            return masterDetailDto;
        }

        public static void AssignApiParameterFromDictRootPrimaryKeyValue(Dictionary<string, object> rootPrimaryKeyValue, Dictionary<string, string> queryParams, Dictionary<string, string> pathParams, Dictionary<string, string> environmentVairables)
        {
            foreach (string key in rootPrimaryKeyValue.Keys)
            {
                string paramValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(rootPrimaryKeyValue[key]);

                if (key.StartsWith(ApiPathParamPrefix))
                {
                    string paramName = key.Substring(ApiPathParamPrefix.Length);
                    if (!string.IsNullOrWhiteSpace(paramName))
                    {
                        pathParams[paramName] = paramValue;
                    }
                }
                else if (key.StartsWith(ApiQueryParamPrefix))
                {
                    string paramName = key.Substring(ApiQueryParamPrefix.Length);
                    if (!string.IsNullOrWhiteSpace(paramName))
                    {
                        queryParams[paramName] = paramValue;
                    }
                }
                else if (key.StartsWith(ApiEvironmentVariablePrefix))
                {
                    string paramName = key.Substring(ApiEvironmentVariablePrefix.Length);
                    if (!string.IsNullOrWhiteSpace(paramName) && !string.IsNullOrWhiteSpace(paramValue))
                    {
                        environmentVairables[paramName] = paramValue;
                    }
                }
            }
        }


        //private static void SetupRootUnitDictValue(Dictionary<string, object> rootPrimaryKeyValue, AppMasterDetailDto rootAppformDataDto,
        //   AppTransactionUnitExDto rootMasterUnit, List<TableNameParameterDTO> sqlParameters)
        //{
        //    foreach (TableNameParameterDTO sqlParam in sqlParameters)
        //    { 

        //    }

        //    //Dictionary<string, object> dictOneToOneFields = MappingDbFiledToTransField(rootMasterUnit, sqlParameters);

        //    //rootAppformDataDto.DictOneToOneFields = dictOneToOneFields;

        //}


        internal static void SetupRootUnitDictValueFromJObject(AppMasterDetailDto rootAppformDataDto, AppTransactionUnitExDto rootMasterUnit, JObject jObj)
        {
            if (rootMasterUnit != null)
            {
                if (!string.IsNullOrWhiteSpace(rootMasterUnit.JsonPath))
                {
                    jObj = (JObject)jObj.SelectToken(rootMasterUnit.JsonPath);
                }

                Dictionary<string, object> dictOneToOneFields = MappingJsonFiledToTransField(rootMasterUnit, jObj);
                rootAppformDataDto.DictOneToOneFields = dictOneToOneFields;
            }
        }


        //private static void SetupRootUnitDictValueFromSqlParameters(AppMasterDetailDto rootAppformDataDto, AppTransactionUnitExDto rootMasterUnit, List<TableNameParameterDTO> sqlParameters)
        //{
        //    //Dictionary<string, object> dictOneToOneFields = MappingJsonFiledToTransField(rootMasterUnit, jObj);
        //    //rootAppformDataDto.DictOneToOneFields = dictOneToOneFields;
        //}

        internal static List<AppChildDataDto> SetupOneChildAndGrandChildData(AppTransactionUnitExDto aChildUnitExDto, JObject jObj, string parentUnitPath)
        {
            string childunitJsonNotePath = aChildUnitExDto.JsonPath;

            if (!string.IsNullOrWhiteSpace(parentUnitPath))
            {
                childunitJsonNotePath = parentUnitPath + "." + childunitJsonNotePath;
            }

            JToken childUnit_jToken = jObj.SelectToken(childunitJsonNotePath);

            List<AppChildDataDto> childDataRowList = new List<AppChildDataDto>();
            ProcessChildAndGrandchildDataRowsFromChildUnitArrayToken(aChildUnitExDto, childUnit_jToken, childDataRowList);

            return childDataRowList;
        }

        internal static void ProcessChildAndGrandchildDataRowsFromChildUnitArrayToken(AppTransactionUnitExDto aChildUnitExDto, JToken childUnit_jToken, List<AppChildDataDto> childDataRowList)
        {
            foreach (JToken childRow_JToken in childUnit_jToken.Children())
            {
                AppChildDataDto aAppformChildDataDto = new AppChildDataDto();
                aAppformChildDataDto.ChildUnitId = (int)aChildUnitExDto.Id;

                childDataRowList.Add(aAppformChildDataDto);

                Dictionary<string, object> dictOneToOneFields = MappingJsonFiledToTransField(aChildUnitExDto, childRow_JToken);

                aAppformChildDataDto.DictOneToOneFields = dictOneToOneFields;



                aAppformChildDataDto.DictOneToManyFields = new Dictionary<string, List<AppChildDataDto>>();

                foreach (var grandChildUnitExDto in aChildUnitExDto.Children)
                {
                    List<AppChildDataDto> grandchildDataRowList = new List<AppChildDataDto>();
                    aAppformChildDataDto.DictOneToManyFields.Add(grandChildUnitExDto.Id.ToString(), grandchildDataRowList);

                    string grancChildUnitJsonNotePath = grandChildUnitExDto.JsonPath;

                    JToken grandchildUnit_jToken = childRow_JToken.SelectToken(grancChildUnitJsonNotePath);

                    foreach (JToken grandchildRow_JToken in grandchildUnit_jToken.Children())
                    {
                        AppChildDataDto grandchlidDataRow = new AppChildDataDto();
                        grandchlidDataRow.ChildUnitId = (int)aChildUnitExDto.Id;

                        Dictionary<string, object> gc_dictOneToOneFields = MappingJsonFiledToTransField(grandChildUnitExDto, grandchildRow_JToken);

                        grandchlidDataRow.DictOneToOneFields = gc_dictOneToOneFields;

                        grandchildDataRowList.Add(grandchlidDataRow);
                    }


                }


            }
        }

        internal static Dictionary<string, object> MappingJsonFiledToTransField(AppTransactionUnitExDto aUnit, JObject jObj)
        {
            string unitJsonNotePath = aUnit.JsonPath;



            var dictOneToOneFields = ConvertOneJObjectToDictRow(jObj, unitJsonNotePath, aUnit.AppTransactionFieldList.ToList());

            var transFieldDbNameList = aUnit.AppTransactionFieldList.Select(o => o.DataBaseFieldName).Distinct().ToList();

            var notInDbFileds = transFieldDbNameList.Except(dictOneToOneFields.Keys);

            foreach (string tempFiledname in notInDbFileds)
            {

                dictOneToOneFields[tempFiledname] = null;


            }

            return dictOneToOneFields;
        }

        private static Dictionary<string, object> MappingJsonFiledToTransField(AppTransactionUnitExDto aUnit, JToken jToken)
        {
            string unitJsonNotePath = aUnit.JsonPath;

            var transFieldDbNameList = aUnit.AppTransactionFieldList.Select(o => o.DataBaseFieldName).Distinct().ToList();

            Dictionary<string, object> dictOneToOneFields = null;

            if (aUnit.IsExclusiveForOwner.HasValue && aUnit.IsExclusiveForOwner.Value) //if unit is for json simple field list
            {
                dictOneToOneFields = ConvertOneSimpleListJTokenToDictRow(jToken, unitJsonNotePath, transFieldDbNameList);
            }
            else
            {
                dictOneToOneFields = ConvertOneJTokenToDictRow(jToken, unitJsonNotePath, aUnit.AppTransactionFieldList.ToList());
            }


            var notInDbFileds = transFieldDbNameList.Except(dictOneToOneFields.Keys);

            foreach (string tempFiledname in notInDbFileds)
            {

                dictOneToOneFields[tempFiledname] = null;


            }

            return dictOneToOneFields;
        }



        private static Dictionary<string, object> ConvertOneJObjectToDictRow(JObject jObj, string unitJsonNotePath, List<AppTransactionFieldExDto> transactionFieldList)
        {

            Dictionary<string, object> row = new Dictionary<string, object>();

            foreach (var fieldDto in transactionFieldList)
            {

                string jsonPath = fieldDto.JsonPath;

                var value = jObj.SelectToken(jsonPath);

                AppMasterDetailFormDataLoadBL.AssignOneFormFieldValueByType(row, fieldDto, value);
            }


            return row;
        }


        private static Dictionary<string, object> ConvertOneJTokenToDictRow(JToken jToken, string unitJsonNotePath, List<AppTransactionFieldExDto> transactionFieldList)
        {

            Dictionary<string, object> row = new Dictionary<string, object>();

            foreach (var fieldDto in transactionFieldList)
            {
                string jsonPath = fieldDto.JsonPath;

                var value = jToken.SelectToken(jsonPath);

                AppMasterDetailFormDataLoadBL.AssignOneFormFieldValueByType(row, fieldDto, value);
            }


            return row;
        }

        private static Dictionary<string, object> ConvertOneSimpleListJTokenToDictRow(JToken jToken, string unitJsonNotePath, List<string> transFiledJsonPathList)
        {
            Dictionary<string, object> row = new Dictionary<string, object>();
            string fieldKey = transFiledJsonPathList.FirstOrDefault();

            row.Add(fieldKey, jToken);
            return row;
        }

        public static AppMasterDetailDto GetNewFormData(AppTransactionExDto hierachyTransactionExDto)
        {

            AppMasterDetailDto masterDetailDto = new AppMasterDetailDto();

            masterDetailDto.TransactionId = (int)hierachyTransactionExDto.Id;
            masterDetailDto.IsShowSaveButton = hierachyTransactionExDto.IsShowSaveButton;
            AppTransactionStructureDto aAppMasterDetailStructureDto = AppTransactionStructureLoadBL.GetGeneralStrcutureInfo(hierachyTransactionExDto);

            Dictionary<string, List<AppChildDataDto>> DictOneToManyFields = new Dictionary<string, List<AppChildDataDto>>();
            masterDetailDto.DictOneToManyFields = DictOneToManyFields;

            var dictOneToOneFields = new Dictionary<string, object>();

            masterDetailDto.DictOneToOneFields = dictOneToOneFields;
            Dictionary<string, Dictionary<string, object>> dictSiblingValue = masterDetailDto.DictSiblingOneToOneFields;

            // only one root
            AppTransactionUnitExDto rootTransactionUnit = hierachyTransactionExDto.AppTransactionUnitList.FirstOrDefault();
            if (rootTransactionUnit != null)
            {
                masterDetailDto.RootUnitId = rootTransactionUnit.Id;


                SetupRootUnit(rootTransactionUnit, dictOneToOneFields);

                //  need to add sibling default



                //foreach (var sibLing in hierachyTransactionExDto.SibLineTransactionUnitIdExDtoList)
                //{
                //    Dictionary<string, object> dictSilValue = new Dictionary<string, object>();

                //    dictSiblingValue.Add(sibLing.Id.ToString(), dictSilValue);
                //    string sibTableName = sibLing.DataBaseTableName;

                //    DatabaseTable siblDatabaseTable = AppCacheManagerBL.GetDatabaseTable(sibTableName, sibLing.DataSourceFrom, sibLing.SchemaOwner); ;

                //    SetupRootUnit(sibLing, dictSilValue);
                //}

                foreach (AppTransactionUnitExDto aChildTransactionUnitExDto in rootTransactionUnit.Children)
                {
                    SetChildDefaultValuePlaceholder(DictOneToManyFields, aChildTransactionUnitExDto);
                }
            }


            masterDetailDto.EditCloneDictOneToManyFields = masterDetailDto.DictOneToManyFields.DeepCopy();


            foreach (AppTransactionUnitExDto aChildTransactionUnitExDto in rootTransactionUnit.Children)
            {
                if (!aChildTransactionUnitExDto.IsVirtualUnit)
                {
                    masterDetailDto.DictOneToManyFields[aChildTransactionUnitExDto.Id.ToString()].Clear();
                }
            }

            masterDetailDto.DictCascadingFiledDataSource = new Dictionary<string, List<LookupItemDto>>();
            masterDetailDto.DictCascadingFieldIdAndLabel = new Dictionary<string, string>();
            AppCascadingBL.SetupIntialCscadingFieldDataSource(hierachyTransactionExDto, masterDetailDto, true);
            AppCascadingBL.SetupIntialAutoCompleteFieldDataSource(hierachyTransactionExDto, masterDetailDto, true);
            AppMasterDetailFormDataLoadBL.SetupFormConditionLockingDictValue(hierachyTransactionExDto, masterDetailDto);

            masterDetailDto.IsNew = true;

           

            // need to udpate
            return masterDetailDto;
        }

        private static void SetupRootUnit(AppTransactionUnitExDto rootTransactionUnit, Dictionary<string, object> dictOneToOneFields)
        {
            Dictionary<string, object> dictRootTransactionUnitSecurityDDLFieldValue = AppTransactionStructureLoadBL.GetTransactionUnitSecurityDDLFieldValue(rootTransactionUnit);

            Dictionary<string, object> dictDbFieldDefaultValue = rootTransactionUnit.AppTransactionFieldList.Where(o => !string.IsNullOrWhiteSpace(o.DefaultValue))
                .ToDictionary(o => o.DataBaseFieldName, o =>
                {
                    if (o.ControlType == (int)EmAppControlType.CheckBox)
                    {
                        return (object)ControlTypeValueConverter.ConvertValueToBoolean(o.DefaultValue);
                    }
                    else if (o.ControlType == (int)EmAppControlType.DDL && o.DataType.HasValue && o.DataType.Value == (int)EmAppDataType.Integer)
                    {
                        return (object)ControlTypeValueConverter.ConvertValueToInt(o.DefaultValue);
                    }
                    else if (o.ControlType == (int)EmAppControlType.Numeric)
                    {
                        if ((o.DataType.HasValue && o.DataType.Value == (int)EmAppDataType.Integer) || (o.Nbdecimal.HasValue && o.Nbdecimal.Value == 0))
                        {
                            return (object)ControlTypeValueConverter.ConvertValueToInt(o.DefaultValue);
                        }
                        else
                        {
                            return (object)ControlTypeValueConverter.ConvertValueToDecimal(o.DefaultValue);
                        }
                    }
                    else
                    {
                        return (object)o.DefaultValue;
                    }
                });



            rootTransactionUnit.AppTransactionFieldList.Where(o => o.ControlType == (int)EmAppControlType.Numeric && !dictDbFieldDefaultValue.ContainsKey(o.DataBaseFieldName) && !o.IsPrimaryKey && !o.IsLinkToParentPrimaryKey)
                .ForAll(o => dictDbFieldDefaultValue.Add(o.DataBaseFieldName, "0"));

            foreach (var column in rootTransactionUnit.AppTransactionFieldList)
            {
                // need to get value from database column ?

                if (dictDbFieldDefaultValue.ContainsKey(column.DataBaseFieldName))
                {
                    dictOneToOneFields.Add(column.DataBaseFieldName, dictDbFieldDefaultValue[column.DataBaseFieldName]);
                }
                else
                {
                    dictOneToOneFields.Add(column.DataBaseFieldName, null);
                }

                if (dictRootTransactionUnitSecurityDDLFieldValue.ContainsKey(column.DataBaseFieldName))
                {
                    dictOneToOneFields[column.DataBaseFieldName] = dictRootTransactionUnitSecurityDDLFieldValue[column.DataBaseFieldName];
                }
            }

            if (rootTransactionUnit.DataBaseTableName == "AppBusinessPartner")
            {
                dictOneToOneFields["AppCompanyID"] = ServerContext.Instance.CurrentCompanyId;
            }


            AppTransDataSystemTokenBL.AssignAuntoGenerationCodeToUnitField(dictOneToOneFields, rootTransactionUnit, false);
        }


        private static void SetChildDefaultValuePlaceholder(Dictionary<string, List<AppChildDataDto>> DictOneToManyFields, AppTransactionUnitExDto aChildTransactionUnitExDto)
        {
            // only need to add AppChildDataDto in child level !!
            List<AppChildDataDto> childAppformChildDataDto = new List<AppChildDataDto>();
            DictOneToManyFields.Add(aChildTransactionUnitExDto.Id.ToString(), childAppformChildDataDto);

            // one need to one Row as place holder value
            AppChildDataDto aAppformChildDataDto = new AppChildDataDto() { IsNew = true };
            childAppformChildDataDto.Add(aAppformChildDataDto);

            Dictionary<string, string> dictChildDbFieldDefaultValue = aChildTransactionUnitExDto.AppTransactionFieldList.Where(o => !string.IsNullOrWhiteSpace(o.DefaultValue)).ToDictionary(o => o.DataBaseFieldName, o => o.DefaultValue);

            aChildTransactionUnitExDto.AppTransactionFieldList.Where(o => o.ControlType == (int)EmAppControlType.Numeric && !dictChildDbFieldDefaultValue.ContainsKey(o.DataBaseFieldName) && !o.IsPrimaryKey && !o.IsLinkToParentPrimaryKey)
                .ForAll(o => dictChildDbFieldDefaultValue.Add(o.DataBaseFieldName, "0"));


            aAppformChildDataDto.DictOneToOneFields = new Dictionary<string, object>();

            Dictionary<string, object> dictChildTransactionUnitSecurityDDLFieldValue = AppTransactionStructureLoadBL.GetTransactionUnitSecurityDDLFieldValue(aChildTransactionUnitExDto);

            foreach (var column in aChildTransactionUnitExDto.AppTransactionFieldList)
            {
                if (dictChildDbFieldDefaultValue.ContainsKey(column.DataBaseFieldName))
                {
                    aAppformChildDataDto.DictOneToOneFields.Add(column.DataBaseFieldName, dictChildDbFieldDefaultValue[column.DataBaseFieldName]);
                }
                else
                {
                    aAppformChildDataDto.DictOneToOneFields.Add(column.DataBaseFieldName, null);
                }

                if (dictChildTransactionUnitSecurityDDLFieldValue.ContainsKey(column.DataBaseFieldName))
                {
                    aAppformChildDataDto.DictOneToOneFields[column.DataBaseFieldName] = dictChildTransactionUnitSecurityDDLFieldValue[column.DataBaseFieldName];
                }
            }

            aAppformChildDataDto.DictOneToManyFields = new Dictionary<string, List<AppChildDataDto>>();
            if (aChildTransactionUnitExDto.Children != null && aChildTransactionUnitExDto.Children.Count > 0)
            {



                foreach (AppTransactionUnitExDto aGrandchildAppTransactionUnitExDto in aChildTransactionUnitExDto.Children)
                {

                    List<AppChildDataDto> listGridnChildRow = new System.Collections.Generic.List<AppChildDataDto>();
                    AppChildDataDto aAppChildDataDto = new AppChildDataDto();
                    Dictionary<string, object> dictGrandChildRowKeyValue = new Dictionary<string, object>();
                    aAppChildDataDto.DictOneToOneFields = dictGrandChildRowKeyValue;
                    listGridnChildRow.Add(aAppChildDataDto);

                    Dictionary<string, string> dictGrandChildDbFieldDefaultValue = aGrandchildAppTransactionUnitExDto.AppTransactionFieldList.Where(o => !string.IsNullOrWhiteSpace(o.DefaultValue)).ToDictionary(o => o.DataBaseFieldName, o => o.DefaultValue);
                    aGrandchildAppTransactionUnitExDto.AppTransactionFieldList.Where(o => o.ControlType == (int)EmAppControlType.Numeric && !dictGrandChildDbFieldDefaultValue.ContainsKey(o.DataBaseFieldName) && !o.IsPrimaryKey && !o.IsLinkToParentPrimaryKey)
                        .ForAll(o => dictGrandChildDbFieldDefaultValue.Add(o.DataBaseFieldName, "0"));



                    Dictionary<string, object> dictGrandChildTransactionUnitSecurityDDLFieldValue = AppTransactionStructureLoadBL.GetTransactionUnitSecurityDDLFieldValue(aGrandchildAppTransactionUnitExDto);

                    foreach (var column in aGrandchildAppTransactionUnitExDto.AppTransactionFieldList)
                    {
                        if (dictGrandChildDbFieldDefaultValue.ContainsKey(column.DataBaseFieldName))
                        {
                            dictGrandChildRowKeyValue.Add(column.DataBaseFieldName, dictGrandChildDbFieldDefaultValue[column.DataBaseFieldName]);
                        }
                        else
                        {
                            dictGrandChildRowKeyValue.Add(column.DataBaseFieldName, null);
                        }

                        if (dictGrandChildTransactionUnitSecurityDDLFieldValue.ContainsKey(column.DataBaseFieldName))
                        {
                            dictGrandChildRowKeyValue[column.DataBaseFieldName] = dictGrandChildTransactionUnitSecurityDDLFieldValue[column.DataBaseFieldName];
                        }
                    }

                    aAppformChildDataDto.DictOneToManyFields.Add(aGrandchildAppTransactionUnitExDto.Id.ToString(), listGridnChildRow);
                }
            }
        }
    }
}