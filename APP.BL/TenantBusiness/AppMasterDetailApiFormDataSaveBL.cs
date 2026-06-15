using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using APP.Framework.Communication;
using APP.Framework.Excel;
using APP.Framework.Validation;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
//using APP.Persistence.Common;
using DatabaseSchemaMrg;
using DatabaseSchemaMrg.DataSchema;
using ExchangeBL;
using ExchangeBL.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

using APP.Framework;
namespace App.BL
{
    //aChildTransactionUnitExDto.IsUsedForLoadingAvailableSource.HasValue && aChildTransactionUnitExDto.IsUsedForLoadingAvailableSource.Value
    public static class AppMasterDetailApiFormDataSaveBL
    {
        public static OperationCallResult<AppMasterDetailDto> SaveTransactionData(AppMasterDetailDto rootClientAppformDataDto, bool isCallingFromWorkFlow = false)
        {
            OperationCallResult<AppMasterDetailDto> aOperationCallResult = new OperationCallResult<AppMasterDetailDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;


            if (!rootClientAppformDataDto.IsDirty && !rootClientAppformDataDto.IsNew)
            {
                return aOperationCallResult;
            }

            aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppTransactionExDto aAppTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(rootClientAppformDataDto.TransactionId);

            if (aAppTransactionExDto.BaseApiConfigDto != null)
            {
                AppTransactionStructureDto aAppMasterDetailStructureDto = AppTransactionStructureLoadBL.GetGeneralStrcutureInfo(aAppTransactionExDto);


                AppMasterDetailFormDataSaveBL.VerifyTransDataAndResetDataType(rootClientAppformDataDto, aAppMasterDetailStructureDto);

                aAppTransactionExDto.AppMasterDetailStructureDtoInfo = aAppMasterDetailStructureDto;
                AppTransactionUnitExDto rootMasterUnit = aAppTransactionExDto.RootMasterUnit;
                rootClientAppformDataDto.RootUnitId = rootMasterUnit.Id;


                Dictionary<string, object> rootOneToOneFields = rootClientAppformDataDto.DictOneToOneFields;
                //string tableName = rootMasterUnit.DataBaseTableName;

                object rootPkValue = rootClientAppformDataDto.RootPrimaryKeyValue;

                var calendarRepeatSettingField = rootMasterUnit.AppTransactionFieldList.FirstOrDefault(o => o.MappingEmSystemTokenField.HasValue && o.MappingEmSystemTokenField.Value == (int)EmBLFiledMappingSystemTokenField.CalendarRepeatSetting);

                if (calendarRepeatSettingField != null)
                {
                    if (rootClientAppformDataDto.CalendarRepeatSetting != null)
                    {

                        rootOneToOneFields[calendarRepeatSettingField.DataBaseFieldName] = JsonConvert.SerializeObject(rootClientAppformDataDto.CalendarRepeatSetting);
                    }
                }

                //if (!rootMasterUnit.IsPrimaryKeyIdentityInsert)
                //{
                //    string validationNoneEmptyKeyMessage = ValidaNoneIdenityInsertPkValue(rootMasterUnit, rootOneToOneFields);

                //    if (!string.IsNullOrWhiteSpace(validationNoneEmptyKeyMessage))
                //    {
                //        aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_PrimaryKey_Error", ValidationItemType.Error, validationNoneEmptyKeyMessage));

                //        return aOperationCallResult;
                //    }
                //    else
                //    {

                //        rootPkValue =  GetUnitPkFiledValue(rootMasterUnit, rootOneToOneFields).First().Value;
                //    }
                //}

                try
                {
                    if (rootClientAppformDataDto.IsNew)
                    {
                        //rootPkValue = InsertMasterDetail(rootClientAppformDataDto, aAppTransactionExDto, aValidationResult);

                        CallApiToPostTransactionData(rootClientAppformDataDto, aAppTransactionExDto, aValidationResult);
                    }//// need to update !!
                    else if (rootClientAppformDataDto.IsDirty)
                    {

                        CallApiToPostTransactionData(rootClientAppformDataDto, aAppTransactionExDto, aValidationResult);
                        //rootPkValue = AppMasterDetailFormDataSaveBL.GetUnitPkFiledValue(rootMasterUnit, rootOneToOneFields).First().Value;
                    }

                }
                catch (Exception ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Exception_Error", ValidationItemType.Error, ex.ToString()));
                }

                if (!aValidationResult.HasErrors)
                {
                    if (rootClientAppformDataDto.DataTransferSettingId.HasValue && !string.IsNullOrWhiteSpace(rootClientAppformDataDto.ApiResponse))
                    {
                        if (rootClientAppformDataDto.DataTransferFromMasterDetailDto != null)
                        {
                            AppTransactionDataTransferBL.ProcessApiResponseDataTransfer_DataTransferFromMasterDetail(rootClientAppformDataDto.DataTransferSettingId.Value, rootClientAppformDataDto.ApiResponse, rootClientAppformDataDto.DataTransferFromMasterDetailDto);
                        }
                        else if (rootClientAppformDataDto.DataTransferFromListEditRowDto != null)
                        {
                            AppTransactionDataTransferBL.ProcessApiResponseDataTransfer_DataTransferFromListEditRowDto(rootClientAppformDataDto.DataTransferSettingId.Value, rootClientAppformDataDto.ApiResponse, rootClientAppformDataDto.DataTransferFromListEditRowDto);
                        }
                    }


                    aOperationCallResult.Object = rootClientAppformDataDto;


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


                    aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

                if (aOperationCallResult.Object != null)
                {
                    // aOperationCallResult.Object.RootUnitId = rootMasterUnit.Id;
                }

            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Exception_Error", ValidationItemType.Error, "Cannot Find Update Data Model API Operation."));
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<AppMasterDetailDto> PublishConsumApiTransactionDataByDataTransfer_Old(AppMasterDetailDto rootClientAppformDataDto, bool isCallingFromWorkFlow = false)
        {
            OperationCallResult<AppMasterDetailDto> aOperationCallResult = new OperationCallResult<AppMasterDetailDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppTransactionExDto aAppTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(rootClientAppformDataDto.TransactionId);

            if (aAppTransactionExDto.ConsumApiDataModelSaveSettingDto != null && aAppTransactionExDto.ConsumApiDataModelSaveSettingDto.DataTransferSettingId.HasValue)
            {
                var transformedFormData = AppTransactionDataTransferBL.PrepareDataTransferFormData_FromMasterDetailToMasterDetail(rootClientAppformDataDto, aAppTransactionExDto.ConsumApiDataModelSaveSettingDto.DataTransferSettingId);

                if (transformedFormData != null)
                {
                    //transformedFormData.RootPrimaryKeyValue = rootClientAppformDataDto.RootPrimaryKeyValue;

                    var postResult = SaveTransactionData(transformedFormData, isCallingFromWorkFlow);

                    if (postResult != null)
                    {
                        if (postResult.IsSuccessfulWithResult)
                        {
                            if (postResult.Object != null && postResult.Object.DataTransferFromMasterDetailDto != null)
                            {
                                var sourceFormApiCallBackData = postResult.Object.DataTransferFromMasterDetailDto;


                                aOperationCallResult.Object = sourceFormApiCallBackData;

                                if (aAppTransactionExDto.OtherOptions != null)
                                {
                                    if (aAppTransactionExDto.OtherOptions.NeedToSaveFormAfterPublishByApiCall && !aAppTransactionExDto.OtherOptions.IsApiIntegrationTransaction)
                                    {
                                        var callBackData_saveResult = AppMasterDetailFormDataSaveBL.SaveTransactionData(sourceFormApiCallBackData, isCallingFromWorkFlow);

                                        if (callBackData_saveResult.IsSuccessfulWithResult)
                                        {
                                            aOperationCallResult.Object = callBackData_saveResult.Object;
                                            aOperationCallResult.Object.IsNeedToRefresh = true;
                                        }
                                        else
                                        {
                                            aValidationResult.Merge(callBackData_saveResult.ValidationResult);
                                        }

                                    }
                                    else if (aAppTransactionExDto.OtherOptions.NeedToRefreshAfterSaveByApiCall)
                                    {
                                        aOperationCallResult.Object.IsNeedToRefresh = true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            aValidationResult.Merge(postResult.ValidationResult);
                        }

                    }


                }
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<AppMasterDetailDto> PublishConsumApiTransactionDataByDataTransfer(AppMasterDetailDto formDataDto, bool isCallingFromWorkFlow = false)
        {
            OperationCallResult<AppMasterDetailDto> aOperationCallResult = new OperationCallResult<AppMasterDetailDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppTransactionExDto aAppTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(formDataDto.TransactionId);

            if (aAppTransactionExDto.ConsumApiDataModelSaveSettingDto != null && aAppTransactionExDto.ConsumApiDataModelSaveSettingDto.DataTransferSettingId.HasValue)
            {
                TransactionApiSettingDto transactionApiSettingDto = aAppTransactionExDto.ConsumApiDataModelSaveSettingDto;

                try
                {
                    CallApiOperationFromMasterDetailTransactionForm(formDataDto, aValidationResult, transactionApiSettingDto.DataTransferSettingId.Value, null);

                    aOperationCallResult.Object = formDataDto;


                    if (aAppTransactionExDto.OtherOptions != null)
                    {
                        if (aAppTransactionExDto.OtherOptions.NeedToSaveFormAfterPublishByApiCall && !aAppTransactionExDto.OtherOptions.IsApiIntegrationTransaction)
                        {
                            var callBackData_saveResult = AppMasterDetailFormDataSaveBL.SaveTransactionData(formDataDto, isCallingFromWorkFlow);

                            if (callBackData_saveResult.IsSuccessfulWithResult)
                            {
                                aOperationCallResult.Object = callBackData_saveResult.Object;
                                aOperationCallResult.Object.IsNeedToRefresh = true;
                            }
                            else
                            {
                                aValidationResult.Merge(callBackData_saveResult.ValidationResult);
                            }

                        }
                        else if (aAppTransactionExDto.OtherOptions.NeedToRefreshAfterSaveByApiCall)
                        {
                            aOperationCallResult.Object.IsNeedToRefresh = true;
                        }
                    }

                    if (!aValidationResult.HasErrors)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_SaveOk", ValidationItemType.Message, "Publish Success."));
                    }
                }
                catch (Exception ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Exception_Error", ValidationItemType.Error, ex.ToString()));
                }


            }

            return aOperationCallResult;
        }

        internal static void CallApiOperationFromMasterDetailTransactionForm(AppMasterDetailDto formDataDto, ValidationResult aValidationResult, int dataTransferSettingId, AppProjectWorkFlowActionExDto commandDto)
        {

            AppIntergrationSettingParameterExDto dataExchangeSettingDTO = null;

            try
            {
                AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(formDataDto.TransactionId);
                AppTransactionDataTransferSettingExDto dataTransferDto = AppTransactionDataTransferSettingBL.RetrieveOneAppTransactionDataTransferSettingExDto(dataTransferSettingId);
                dataExchangeSettingDTO = DataExchangeSettingBL.GetSetting(dataTransferDto.TargetApiOperationId.Value);

                Dictionary<string, string> headers = new Dictionary<string, string>();
                Dictionary<string, string> queryParams = new Dictionary<string, string>();
                Dictionary<string, string> pathParams = new Dictionary<string, string>();
                Dictionary<string, string> environmentVairables = new Dictionary<string, string>();
                dynamic postDynamicData = new ExpandoObject();

                AppTransactionDataTransferBL.PrepareApiPayloadData_FromMasterDetailFormDataTransfer(formDataDto, dataTransferSettingId,
                   out headers, out queryParams, out pathParams, out environmentVairables, out postDynamicData);


                List<string> listResponse = new List<string>();


                EmAppApiPayloadDataType payloadDataType = EmAppApiPayloadDataType.JSON;

                if (dataTransferDto.TargetApiOperationDto.OtherSettingsDto != null && dataTransferDto.TargetApiOperationDto.OtherSettingsDto.PayloadDataType.HasValue)
                {
                    payloadDataType = dataTransferDto.TargetApiOperationDto.OtherSettingsDto.PayloadDataType.Value;
                }


                if (payloadDataType == EmAppApiPayloadDataType.JSON)
                {
                    string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(postDynamicData);

                    if (commandDto != null && commandDto.ActionAttribute != null && commandDto.ActionAttribute.IsLogApiPayload)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "PublishConsumApiTransactionDataByDataTransfer_ViewJsonData", ValidationItemType.Message, "Payload Data:\n" + jsonData));
                    }

                    listResponse = CallApiWithJsonDataByIntegrationSettingApiCallAsync(dataExchangeSettingDTO, headers, queryParams, pathParams, environmentVairables, jsonData).Result;
                }
                else if (payloadDataType == EmAppApiPayloadDataType.FileByteArray)
                {
                    if (formDataDto.UploadedFileDto != null)
                    {
                        listResponse = CallApiWithJsonDataByIntegrationSettingApiCallAsync(dataExchangeSettingDTO, headers, queryParams, pathParams, environmentVairables, formDataDto.UploadedFileDto).Result;
                    }
                    else
                    {

                        var transferMappingDto = dataTransferDto.AppTransactionSaveAsMappingList.FirstOrDefault(o =>
                            !(o.IsBlankTargetField.HasValue && o.IsBlankTargetField.Value) && o.SourceFiledId.HasValue && o.JsonPropertyPathName == EmBLFiledMappingSystemTokenField.ApiUploadFileId.ToString());

                        if (transferMappingDto != null)
                        {
                            var fileIdTransFieldDto = appTransactionExDto.RootMasterUnit.AppTransactionFieldList.FirstOrDefault(o => (int)o.Id == transferMappingDto.SourceFiledId.Value);

                            if (fileIdTransFieldDto != null)
                            {
                                int? fileId = ControlTypeValueConverter.ConvertValueToInt(formDataDto.DictOneToOneFields[fileIdTransFieldDto.DataBaseFieldName]);

                                if (fileId.HasValue)
                                {
                                    byte[] fileByteArray = null;
                                    string fileName = "";
                                    EmailHelper.GetAppFileContentFromId(fileId.Value, ref fileByteArray, ref fileName, false);
                                    var payloadFile = new AppFileDto() { FileCode = fileName, FileContent = fileByteArray };

                                    if (commandDto != null && commandDto.ActionAttribute != null && commandDto.ActionAttribute.IsLogApiPayload)
                                    {
                                        aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "PublishConsumApiTransactionDataByDataTransfer_ViewJsonData", ValidationItemType.Message, "Payload Data:\n" + "File Id: " + fileId.Value));
                                    }

                                    listResponse = CallApiWithJsonDataByIntegrationSettingApiCallAsync(dataExchangeSettingDTO, headers, queryParams, pathParams, environmentVairables, payloadFile).Result;
                                }
                            }
                        }
                    }
                }
                else if (payloadDataType == EmAppApiPayloadDataType.ServerFilePath)
                {
                    var transferMappingDto = dataTransferDto.AppTransactionSaveAsMappingList.FirstOrDefault(o =>
                        !(o.IsBlankTargetField.HasValue && o.IsBlankTargetField.Value) && o.SourceFiledId.HasValue && o.JsonPropertyPathName == EmBLFiledMappingSystemTokenField.ApiUploadServerFilePath.ToString());

                    if (transferMappingDto != null)
                    {
                        var serverFilePathFieldDto = appTransactionExDto.RootMasterUnit.AppTransactionFieldList.FirstOrDefault(o => (int)o.Id == transferMappingDto.SourceFiledId.Value);

                        if (serverFilePathFieldDto != null)
                        {
                            string serverFilePath = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(formDataDto.DictOneToOneFields[serverFilePathFieldDto.DataBaseFieldName]);

                            if (!string.IsNullOrWhiteSpace(serverFilePath))
                            {
                                AppFilePathDto appFilePathDto = new AppFilePathDto();
                                appFilePathDto.FilePath = serverFilePath;

                                if (serverFilePath.ToLower().StartsWith("ftp"))
                                {
                                    var ftpUserNameMappingDto = dataTransferDto.AppTransactionSaveAsMappingList.FirstOrDefault(o =>
                                        !(o.IsBlankTargetField.HasValue && o.IsBlankTargetField.Value) && o.SourceFiledId.HasValue && o.JsonPropertyPathName == EmBLFiledMappingSystemTokenField.ApiUploadFtpUserName.ToString());

                                    var ftpPasswordMappingDto = dataTransferDto.AppTransactionSaveAsMappingList.FirstOrDefault(o =>
                                        !(o.IsBlankTargetField.HasValue && o.IsBlankTargetField.Value) && o.SourceFiledId.HasValue && o.JsonPropertyPathName == EmBLFiledMappingSystemTokenField.ApiUploadFtpPassword.ToString());

                                    if (ftpUserNameMappingDto != null && ftpPasswordMappingDto != null)
                                    {
                                        var ftpUserNamehFieldDto = appTransactionExDto.RootMasterUnit.AppTransactionFieldList.FirstOrDefault(o => (int)o.Id == ftpUserNameMappingDto.SourceFiledId.Value);
                                        var ftpPasswordFieldDto = appTransactionExDto.RootMasterUnit.AppTransactionFieldList.FirstOrDefault(o => (int)o.Id == ftpPasswordMappingDto.SourceFiledId.Value);

                                        if (ftpUserNamehFieldDto != null && ftpPasswordFieldDto != null)
                                        {
                                            appFilePathDto.FtpUserName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(formDataDto.DictOneToOneFields[ftpUserNamehFieldDto.DataBaseFieldName]);
                                            appFilePathDto.FtpPassword = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(formDataDto.DictOneToOneFields[ftpPasswordFieldDto.DataBaseFieldName]);
                                        }
                                    }
                                }

                                if (commandDto != null && commandDto.ActionAttribute != null && commandDto.ActionAttribute.IsLogApiPayload)
                                {
                                    aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "PublishConsumApiTransactionDataByDataTransfer_ViewJsonData", ValidationItemType.Message, "Payload Data:\n" + "Server File Path: " + serverFilePath));
                                }

                                listResponse = CallApiWithJsonDataByIntegrationSettingApiCallAsync(dataExchangeSettingDTO, headers, queryParams, pathParams, environmentVairables, appFilePathDto).Result;
                            }
                        }
                    }


                }

                if (listResponse.Count > 0)
                {
                    formDataDto.ApiResponse = listResponse[0];

                    if (commandDto != null && commandDto.ActionAttribute != null && commandDto.ActionAttribute.IsLogApiResponse)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "PublishConsumApiTransactionDataByDataTransfer_ViewJsonData", ValidationItemType.Message, "Response Data:\n" + formDataDto.ApiResponse));
                    }

                    AppTransactionDataTransferBL.ProcessApiResponseDataTransfer_DataTransferFromMasterDetail(dataTransferSettingId, formDataDto.ApiResponse, formDataDto);
                }


                if (dataExchangeSettingDTO.APIConfigParameters != null && !string.IsNullOrWhiteSpace(dataExchangeSettingDTO.APIConfigParameters.RequestInfo))
                {
                    if (commandDto != null && commandDto.ActionAttribute != null && commandDto.ActionAttribute.IsLogApiRequest)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "PublishConsumApiTransactionDataByDataTransfer_ViewJsonData", ValidationItemType.Message, "Request Data:\n" + dataExchangeSettingDTO.APIConfigParameters.RequestInfo));
                    }
                }

            }
            catch (Exception ex)
            {
                if (dataExchangeSettingDTO != null && dataExchangeSettingDTO.APIConfigParameters != null && !string.IsNullOrWhiteSpace(dataExchangeSettingDTO.APIConfigParameters.RequestInfo))
                {
                    if (commandDto != null && commandDto.ActionAttribute != null && commandDto.ActionAttribute.IsLogApiRequest)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "PublishConsumApiTransactionDataByDataTransfer_ViewJsonData", ValidationItemType.Message, "Request Data:\n" + dataExchangeSettingDTO.APIConfigParameters.RequestInfo));
                    }
                }

                aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Exception_Error", ValidationItemType.Error, ex.ToString()));
            }
        }

        public static OperationCallResult<AppMasterDetailDto> DeleteComsumeApiTransactionData(AppMasterDetailDto rootClientAppformDataDto, bool isCallingFromWorkFlow = false)
        {
            OperationCallResult<AppMasterDetailDto> aOperationCallResult = new OperationCallResult<AppMasterDetailDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;


            aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppTransactionExDto aAppTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(rootClientAppformDataDto.TransactionId);

            if (aAppTransactionExDto.BaseApiConfigDto != null)
            {
                AppTransactionStructureDto aAppMasterDetailStructureDto = AppTransactionStructureLoadBL.GetGeneralStrcutureInfo(aAppTransactionExDto);


                aAppTransactionExDto.AppMasterDetailStructureDtoInfo = aAppMasterDetailStructureDto;



                object rootPkValue = rootClientAppformDataDto.RootPrimaryKeyValue;


                try
                {
                    CallApiToDeleteTransactionData(rootClientAppformDataDto, aAppTransactionExDto, aValidationResult);

                }
                catch (Exception ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Exception_Error", ValidationItemType.Error, ex.ToString()));
                }

                if (!aValidationResult.HasErrors)
                {
                    rootClientAppformDataDto.IsNeedToCloseFormWindow = true;
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Delete_OK", ValidationItemType.Message, "Delete Success"));
                }
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Exception_Error", ValidationItemType.Error, "Cannot Find Update Data Model API Operation."));
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<AppMasterDetailDto> DeleteTransactionDataByDataTransfer(AppMasterDetailDto rootClientAppformDataDto, bool isCallingFromWorkFlow = false)
        {
            OperationCallResult<AppMasterDetailDto> aOperationCallResult = new OperationCallResult<AppMasterDetailDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppTransactionExDto aAppTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(rootClientAppformDataDto.TransactionId);

            if (aAppTransactionExDto.ConsumApiDataModelDeleteSettingDto != null && aAppTransactionExDto.ConsumApiDataModelDeleteSettingDto.DataTransferSettingId.HasValue)
            {
                var transformedFormData = AppTransactionDataTransferBL.PrepareDataTransferFormData_FromMasterDetailToMasterDetail(rootClientAppformDataDto, aAppTransactionExDto.ConsumApiDataModelDeleteSettingDto.DataTransferSettingId);

                if (transformedFormData != null)
                {
                    transformedFormData.RootPrimaryKeyValue = rootClientAppformDataDto.RootPrimaryKeyValue;

                    return DeleteComsumeApiTransactionData(transformedFormData, isCallingFromWorkFlow);
                }
            }

            return aOperationCallResult;
        }

        private static void CallApiToPostTransactionData(AppMasterDetailDto formDataDto, AppTransactionExDto transactionExDto, ValidationResult aValidationResult)
        {
            if (transactionExDto.BaseApiConfigDto != null && transactionExDto.BaseApiConfigDto.Id != null)
            {
                int operationId = (int)transactionExDto.BaseApiConfigDto.Id;

                AppIntergrationSettingParameterExDto dataExchangeSettingDTO = DataExchangeSettingBL.GetSetting(operationId);

                Dictionary<string, string> headers = new Dictionary<string, string>();
                Dictionary<string, string> queryParams = new Dictionary<string, string>();
                Dictionary<string, string> pathParams = new Dictionary<string, string>();
                Dictionary<string, string> environmentVairables = new Dictionary<string, string>();

                Dictionary<string, object> rootPrimaryKeyValue = new Dictionary<string, object>();

                AppMasterDetailApiFormDataLoadBL.GetDictMasterPkAndValueFromApiRootKeyConcatString(formDataDto.RootPrimaryKeyValue, transactionExDto, rootPrimaryKeyValue);

                AppMasterDetailApiFormDataLoadBL.AssignApiParameterFromDictRootPrimaryKeyValue(rootPrimaryKeyValue, queryParams, pathParams, environmentVairables);

                var rootUnit = transactionExDto.RootMasterUnit;

                List<string> listResponse = new List<string>();
                APIConfigParameterDTO requestData = new APIConfigParameterDTO();

                EmAppApiPayloadDataType payloadDataType = EmAppApiPayloadDataType.JSON;

                if (dataExchangeSettingDTO.OtherSettingsDto != null && dataExchangeSettingDTO.OtherSettingsDto.PayloadDataType.HasValue)
                {
                    payloadDataType = dataExchangeSettingDTO.OtherSettingsDto.PayloadDataType.Value;
                }

                if (payloadDataType == EmAppApiPayloadDataType.JSON)
                {
                    dynamic postDynamicData = ConvertMasterDetailFormDataToJsonDynamicData(formDataDto, transactionExDto);
                    string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(postDynamicData);
                    listResponse = CallApiWithJsonDataByIntegrationSettingApiCallAsync(dataExchangeSettingDTO, headers, queryParams, pathParams, environmentVairables, jsonData).Result;
                }
                else if (payloadDataType == EmAppApiPayloadDataType.FileByteArray)
                {
                    var fileIdTransFieldDto = rootUnit.AppTransactionFieldList.FirstOrDefault(o => o.MappingEmSystemTokenField.HasValue && o.MappingEmSystemTokenField.Value == (int)EmBLFiledMappingSystemTokenField.ApiUploadFileId);

                    if (fileIdTransFieldDto != null)
                    {
                        int? fileId = ControlTypeValueConverter.ConvertValueToInt(formDataDto.DictOneToOneFields[fileIdTransFieldDto.DataBaseFieldName]);

                        if (fileId.HasValue)
                        {
                            byte[] fileByteArray = null;
                            string fileName = "";
                            EmailHelper.GetAppFileContentFromId(fileId.Value, ref fileByteArray, ref fileName, false);
                            var payloadFile = new AppFileDto() { FileCode = fileName, FileContent = fileByteArray };

                            listResponse = CallApiWithJsonDataByIntegrationSettingApiCallAsync(dataExchangeSettingDTO, headers, queryParams, pathParams, environmentVairables, payloadFile).Result;
                        }
                    }
                }
                else if (payloadDataType == EmAppApiPayloadDataType.ServerFilePath)
                {
                    var serverFilePathFieldDto = rootUnit.AppTransactionFieldList.FirstOrDefault(o => o.MappingEmSystemTokenField.HasValue && o.MappingEmSystemTokenField.Value == (int)EmBLFiledMappingSystemTokenField.ApiUploadServerFilePath);

                    if (serverFilePathFieldDto != null)
                    {
                        string serverFilePath = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(formDataDto.DictOneToOneFields[serverFilePathFieldDto.DataBaseFieldName]);

                        if (!string.IsNullOrWhiteSpace(serverFilePath))
                        {
                            AppFilePathDto appFilePathDto = new AppFilePathDto();
                            appFilePathDto.FilePath = serverFilePath;

                            if (serverFilePath.ToLower().StartsWith("ftp"))
                            {
                                var ftpUserNamehFieldDto = rootUnit.AppTransactionFieldList.FirstOrDefault(o => o.MappingEmSystemTokenField.HasValue && o.MappingEmSystemTokenField.Value == (int)EmBLFiledMappingSystemTokenField.ApiUploadFtpUserName);
                                var ftpPasswordFieldDto = rootUnit.AppTransactionFieldList.FirstOrDefault(o => o.MappingEmSystemTokenField.HasValue && o.MappingEmSystemTokenField.Value == (int)EmBLFiledMappingSystemTokenField.ApiUploadFtpPassword);

                                if (ftpUserNamehFieldDto != null && ftpPasswordFieldDto != null)
                                {
                                    appFilePathDto.FtpUserName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(formDataDto.DictOneToOneFields[ftpUserNamehFieldDto.DataBaseFieldName]);
                                    appFilePathDto.FtpPassword = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(formDataDto.DictOneToOneFields[ftpPasswordFieldDto.DataBaseFieldName]);
                                }
                            }



                            listResponse = CallApiWithJsonDataByIntegrationSettingApiCallAsync(dataExchangeSettingDTO, headers, queryParams, pathParams, environmentVairables, appFilePathDto).Result;
                        }
                    }
                }


                if (listResponse.Count > 0)
                {
                    formDataDto.ApiResponse = listResponse[0];
                }
            }
        }


        private static void CallApiToDeleteTransactionData(AppMasterDetailDto formDataDto, AppTransactionExDto transactionExDto, ValidationResult aValidationResult)
        {
            dynamic postDynamicData = new ExpandoObject();
            var postRootData = ((IDictionary<String, Object>)postDynamicData);

            var rootUnit = transactionExDto.RootMasterUnit;
            object formRootData = postRootData;

            if (rootUnit != null)
            {

                string rootUnitPath = rootUnit.JsonPath;

                if (string.IsNullOrWhiteSpace(rootUnitPath))
                {
                    //formRootData = postRootData;
                }
                else
                {
                    string[] path = rootUnitPath.Split(new string[] { "." }, StringSplitOptions.None);
                    CreateDictNodeFromPathLevel(postRootData, path, 0);

                    var formRootDataExpandObj = new ExpandoObject();
                    formRootData = ((IDictionary<String, Object>)formRootDataExpandObj);
                    postRootData[path[path.Length - 1]] = formRootData;


                }


                List<string> rootUnitTempFieldKeys = rootUnit.AppTransactionFieldList.Where(o => o.IsTempVariable.HasValue && o.IsTempVariable.Value).Select(o => o.DataBaseFieldName).ToList();

                ConvertOneUnitDataRowToDictObject(formDataDto.DictOneToOneFields, (IDictionary<String, Object>)formRootData, rootUnitTempFieldKeys);

                // Sibling Unit
                //foreach (var aUnit in transactionExDto.AppTransactionUnitList)
                //{
                //    if (aUnit.IsMasterSiblingUnit.HasValue && aUnit.IsMasterSiblingUnit.Value)
                //    {

                //    }
                //}

                // Child Uit
                foreach (var childUnit in rootUnit.Children)
                {
                    List<string> childUnitTempFieldKeys = childUnit.AppTransactionFieldList.Where(o => o.IsTempVariable.HasValue && o.IsTempVariable.Value).Select(o => o.DataBaseFieldName).ToList();

                    string childUnitId = childUnit.Id.ToString();
                    string childUnitPath = childUnit.JsonPath;
                    string[] childUnitPaths = childUnitPath.Split(new string[] { "." }, StringSplitOptions.None);

                    bool isChildUnitSimpleList = childUnit.IsExclusiveForOwner.HasValue && childUnit.IsExclusiveForOwner.Value;
                    var childUnitArrayNode = CreateArrayNodeFromPathLevel((IDictionary<String, Object>)formRootData, childUnitPaths, 0, isChildUnitSimpleList);

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

                            ConvertOneUnitDataRowToDictObject(childDataRow.DictOneToOneFields, (IDictionary<String, Object>)dictChildRowData, childUnitTempFieldKeys);
                            ((List<ExpandoObject>)childUnitArrayNode).Add(dictChildRowData);

                            foreach (var grandchildUnit in childUnit.Children)
                            {
                                List<string> grandchildUnitTempFieldKeys = grandchildUnit.AppTransactionFieldList.Where(o => o.IsTempVariable.HasValue && o.IsTempVariable.Value).Select(o => o.DataBaseFieldName).ToList();

                                string gcUnitId = grandchildUnit.Id.ToString();
                                string gcUnitPath = grandchildUnit.JsonPath;
                                string[] gcUnitPaths = gcUnitPath.Split(new string[] { "." }, StringSplitOptions.None);

                                bool isGcUnitSimpleList = grandchildUnit.IsExclusiveForOwner.HasValue && grandchildUnit.IsExclusiveForOwner.Value;
                                var gcUnitArrayNode = CreateArrayNodeFromPathLevel((IDictionary<String, Object>)dictChildRowData, gcUnitPaths, 0, isGcUnitSimpleList);

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
                                        ConvertOneUnitDataRowToDictObject(gcDataRow.DictOneToOneFields, (IDictionary<String, Object>)dictGcRowData, grandchildUnitTempFieldKeys);
                                        ((List<ExpandoObject>)gcUnitArrayNode).Add(dictGcRowData);
                                    }
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

            Dictionary<string, object> rootPrimaryKeyValue = new Dictionary<string, object>();

            AppMasterDetailApiFormDataLoadBL.GetDictMasterPkAndValueFromApiRootKeyConcatString(formDataDto.RootPrimaryKeyValue, transactionExDto, rootPrimaryKeyValue);

            AppMasterDetailApiFormDataLoadBL.AssignApiParameterFromDictRootPrimaryKeyValue(rootPrimaryKeyValue, queryParams, pathParams, environmentVairables);

            string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(postDynamicData);
            AppIntergrationSettingParameterExDto dataExchangeSettingDTO = DataExchangeSettingBL.GetSetting((int)transactionExDto.BaseApiConfigDto.Id);
            List<string> listResponse = CallApiWithJsonDataByIntegrationSettingApiCallAsync(dataExchangeSettingDTO, headers, queryParams, pathParams, environmentVairables, jsonData).Result;

            if (listResponse.Count > 0)
            {
                formDataDto.ApiResponse = listResponse[0];
            }
        }

        public static dynamic ConvertOneUnitDataRowToDictObject(Dictionary<string, object> dictOneToOneFields, IDictionary<String, Object> dictRootData, List<string> notConvertToJsonFieldKeys = null)
        {

            foreach (string fieldKey in dictOneToOneFields.Keys)
            {
                if (notConvertToJsonFieldKeys != null && notConvertToJsonFieldKeys.Contains(fieldKey))
                {
                    continue;
                }

                string fieldPath = fieldKey.Replace("___", ".");
                string[] path = fieldPath.Split(new string[] { "." }, StringSplitOptions.None);
                object propertyValue = dictOneToOneFields[fieldKey];

                if (path.Length == 1)
                {
                    dictRootData[path[0]] = propertyValue;
                }
                else // mutiple level
                {
                    IDictionary<string, object> dictDatalevel = CreateDictNodeFromPathLevel(dictRootData, path, 0);
                    dictDatalevel[path[path.Length - 1]] = propertyValue;

                }
            }


            return dictRootData;
        }



        internal static IDictionary<string, object> CreateDictNodeFromPathLevel(IDictionary<string, object> dictRootData, string[] levelPathList, int level)
        {
            if (!dictRootData.ContainsKey(levelPathList[level]))
            {
                dictRootData[levelPathList[level]] = new ExpandoObject();
            }

            var dictDatalevel = (IDictionary<String, Object>)dictRootData[levelPathList[level]];

            level++;

            if (level < levelPathList.Length - 1)
            {
                dictDatalevel = CreateDictNodeFromPathLevel(dictDatalevel, levelPathList, level);
            }

            return dictDatalevel;
        }

        internal static object CreateArrayNodeFromPathLevel(IDictionary<string, object> dictRootData, string[] levelPathList, int level, bool isSimpleList)
        {

            if (!dictRootData.ContainsKey(levelPathList[level]))
            {
                if (level < levelPathList.Length - 1)
                {
                    dictRootData[levelPathList[level]] = new ExpandoObject();
                }
                else
                {
                    if (isSimpleList)
                    {
                        dictRootData[levelPathList[level]] = new List<object>();
                    }
                    else
                    {
                        dictRootData[levelPathList[level]] = new List<ExpandoObject>();
                    }

                }
            }

            var nodeObj = dictRootData[levelPathList[level]];
            level++;

            if (level < levelPathList.Length - 1)
            {
                nodeObj = CreateDictNodeFromPathLevel((IDictionary<String, Object>)nodeObj, levelPathList, level);
                return nodeObj;
            }
            else
            {
                return nodeObj;
            }
        }

        internal static async Task<List<string>> CallApiWithJsonDataByIntegrationSettingApiCallAsync(AppIntergrationSettingParameterExDto dataExchangeSettingDTO, Dictionary<string, string> headers, Dictionary<string, string> queryParams, Dictionary<string, string> pathParams, Dictionary<string, string> environmentVairables, object jsonData)
        {
            //AppIntergrationSettingParameterExDto dataExchangeSettingDTO = DataExchangeSettingBL.GetSetting(actionId);

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


            //dataExchangeSettingDTO.PayloadFile

            EmAppApiPayloadDataType payloadDataType = EmAppApiPayloadDataType.JSON;

            if (dataExchangeSettingDTO.OtherSettingsDto != null && dataExchangeSettingDTO.OtherSettingsDto.PayloadDataType.HasValue)
            {
                payloadDataType = dataExchangeSettingDTO.OtherSettingsDto.PayloadDataType.Value;
            }

            bool isMultipartFormDataContent = dataExchangeSettingDTO.OtherSettingsDto != null && dataExchangeSettingDTO.OtherSettingsDto.IsMultipartFormDataContent;

            List<string> lstResponse = lstResponse = await Helper.CallAPIAsync(dataExchangeSettingDTO.APIConfigParameters, jsonData, dataExchangeSettingDTO.IntergrationSettingId, null, payloadDataType, isMultipartFormDataContent).ConfigureAwait(false);


            return lstResponse;


        }

        private static dynamic ConvertMasterDetailFormDataToJsonDynamicData(AppMasterDetailDto formDataDto, AppTransactionExDto transactionExDto)
        {
            dynamic postDynamicData = new ExpandoObject();
            var postRootData = ((IDictionary<String, Object>)postDynamicData);



            var rootUnit = transactionExDto.RootMasterUnit;

            string rootUnitPath = rootUnit.JsonPath;

            object formRootData = null;

            if (string.IsNullOrWhiteSpace(rootUnitPath))
            {
                formRootData = postRootData;
            }
            else
            {
                string[] path = rootUnitPath.Split(new string[] { "." }, StringSplitOptions.None);
                CreateDictNodeFromPathLevel(postRootData, path, 0);

                var formRootDataExpandObj = new ExpandoObject();
                formRootData = ((IDictionary<String, Object>)formRootDataExpandObj);
                postRootData[path[path.Length - 1]] = formRootData;


            }

            List<string> rootUnitTempFieldKeys = rootUnit.AppTransactionFieldList.Where(o => o.IsTempVariable.HasValue && o.IsTempVariable.Value).Select(o => o.DataBaseFieldName).ToList();

            ConvertOneUnitDataRowToDictObject(formDataDto.DictOneToOneFields, (IDictionary<String, Object>)formRootData, rootUnitTempFieldKeys);

            // Sibling Unit
            //foreach (var aUnit in transactionExDto.AppTransactionUnitList)
            //{
            //    if (aUnit.IsMasterSiblingUnit.HasValue && aUnit.IsMasterSiblingUnit.Value)
            //    {

            //    }
            //}

            // Child Uit
            foreach (var childUnit in rootUnit.Children)
            {
                List<string> childUnitTempFieldKeys = childUnit.AppTransactionFieldList.Where(o => o.IsTempVariable.HasValue && o.IsTempVariable.Value).Select(o => o.DataBaseFieldName).ToList();

                string childUnitId = childUnit.Id.ToString();
                string childUnitPath = childUnit.JsonPath;
                string[] childUnitPaths = childUnitPath.Split(new string[] { "." }, StringSplitOptions.None);

                bool isChildUnitSimpleList = childUnit.IsExclusiveForOwner.HasValue && childUnit.IsExclusiveForOwner.Value;
                var childUnitArrayNode = CreateArrayNodeFromPathLevel((IDictionary<String, Object>)formRootData, childUnitPaths, 0, isChildUnitSimpleList);

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

                        ConvertOneUnitDataRowToDictObject(childDataRow.DictOneToOneFields, (IDictionary<String, Object>)dictChildRowData, childUnitTempFieldKeys);
                        ((List<ExpandoObject>)childUnitArrayNode).Add(dictChildRowData);

                        foreach (var grandchildUnit in childUnit.Children)
                        {
                            List<string> grandchildUnitTempFieldKeys = grandchildUnit.AppTransactionFieldList.Where(o => o.IsTempVariable.HasValue && o.IsTempVariable.Value).Select(o => o.DataBaseFieldName).ToList();

                            string gcUnitId = grandchildUnit.Id.ToString();
                            string gcUnitPath = grandchildUnit.JsonPath;
                            string[] gcUnitPaths = gcUnitPath.Split(new string[] { "." }, StringSplitOptions.None);

                            bool isGcUnitSimpleList = grandchildUnit.IsExclusiveForOwner.HasValue && grandchildUnit.IsExclusiveForOwner.Value;
                            var gcUnitArrayNode = CreateArrayNodeFromPathLevel((IDictionary<String, Object>)dictChildRowData, gcUnitPaths, 0, isGcUnitSimpleList);

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
                                    ConvertOneUnitDataRowToDictObject(gcDataRow.DictOneToOneFields, (IDictionary<String, Object>)dictGcRowData, grandchildUnitTempFieldKeys);
                                    ((List<ExpandoObject>)gcUnitArrayNode).Add(dictGcRowData);
                                }
                            }
                        }
                    }
                }
            }

            return postDynamicData;
        }

    }
}