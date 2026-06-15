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
using APP.Framework.Excel;
using System.Dynamic;
#if NETFRAMEWORK
using Microsoft.AnalysisServices.AdomdClient;
// TODO-PHASE4: Replace with .NET 10 equivalent
#endif

using APP.Framework;
namespace App.BL
{
    public static class AppTransactionDataTransferBL
    {

        public static AppMasterDetailDto PrepareDataTransferFormData_FromMasterDetailToMasterDetail(AppMasterDetailDto srcFormData, int? dataTransferSettingId)
        {
            if (dataTransferSettingId.HasValue)
            {
                AppTransactionDataTransferSettingExDto dataTransferDto = AppTransactionDataTransferSettingBL.RetrieveOneAppTransactionDataTransferSettingExDto(dataTransferSettingId.Value);

                if (dataTransferDto != null
                    && dataTransferDto.DestinationTransactionId.HasValue && dataTransferDto.TransactionId == srcFormData.TransactionId
                    && dataTransferDto.DataTransferMappingList != null)
                {

                    AppTransactionExDto srcTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(dataTransferDto.TransactionId);
                    AppTransactionExDto tgtTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(dataTransferDto.DestinationTransactionId.Value);

                    var srcRootMasterUnit = srcTransactionExDto.RootMasterUnit;
                    var tgtRootMasterUnit = tgtTransactionExDto.RootMasterUnit;

                    AppMasterDetailDto tgtFormBlankData = AppMasterDetailFormDataLoadBL.GetNewFormData(dataTransferDto.DestinationTransactionId.Value);
                    AppMasterDetailDto tgtFormData = null;
                    string tgtPkValue = "";

                    tgtPkValue = PrepareDataTransferFormData_PrepareTargetPkValue(srcFormData, dataTransferDto, srcTransactionExDto, srcRootMasterUnit, tgtRootMasterUnit, tgtPkValue);

                    bool isTgtTransactionApiReadType = tgtTransactionExDto.BaseApiConfigDto != null
                        && tgtTransactionExDto.BaseApiConfigDto.TransactionCrudType == EmAppTransactionCrudType.Read.ToString();

                    if (!string.IsNullOrWhiteSpace(tgtPkValue) && (tgtTransactionExDto.BaseApiConfigDto == null || isTgtTransactionApiReadType))
                    {
                        tgtFormData = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(dataTransferDto.DestinationTransactionId.Value, tgtPkValue);
                    }
                    else
                    {
                        tgtFormData = AppMasterDetailFormDataLoadBL.GetNewFormData(dataTransferDto.DestinationTransactionId.Value);
                        tgtFormData.DictCascadingFiledDataSource = new Dictionary<string, List<LookupItemDto>>();
                        tgtFormData.DictCascadingFieldIdAndLabel = new Dictionary<string, string>();
                        tgtFormData.EditCloneDictOneToManyFields = tgtFormBlankData.EditCloneDictOneToManyFields;
                    }



                    List<AppTransactionSaveAsMappingExDto> mappingList = dataTransferDto.DataTransferMappingList.ToList();



                    Dictionary<int, List<AppTransactionSaveAsMappingExDto>> dictUnitSaveAsMapping = mappingList.Where(o => o.MappingUnitId.HasValue)
                        .GroupBy(o => o.MappingUnitId).ToDictionary(o => o.Key.Value, o => o.ToList());


                    Dictionary<int, int?> dictSrcUnitIdAndTgtUnitId = new Dictionary<int, int?>();
                    Dictionary<string, object> srcRootOneToOneFields = srcFormData.DictOneToOneFields;
                    Dictionary<string, object> tgtRootOneToOneFields = tgtFormData.DictOneToOneFields;
                    Dictionary<string, string> dictTargetParameterKeyAndValue = new Dictionary<string, string>();


                    TransferOneUnitOneRow(dictUnitSaveAsMapping, srcRootMasterUnit, tgtRootMasterUnit, srcRootOneToOneFields, tgtRootOneToOneFields, srcTransactionExDto, tgtTransactionExDto, dictTargetParameterKeyAndValue);

                    foreach (string srcSibUnitid in srcFormData.DictSiblingOneToOneFields.Keys)
                    {
                        int srcSiblingUnitId = int.Parse(srcSibUnitid);
                        var srcSiblingUnit = srcTransactionExDto.DictAllTransactionUnitIdExDto[srcSibUnitid];
                        Dictionary<string, object> srcSibOneToOneFields = srcFormData.DictSiblingOneToOneFields[srcSibUnitid];

                        TransferOneUnitOneRow(dictUnitSaveAsMapping, srcSiblingUnit, tgtRootMasterUnit, srcSibOneToOneFields, tgtRootOneToOneFields, srcTransactionExDto, tgtTransactionExDto, dictTargetParameterKeyAndValue);
                    }

                    TransferChildAndGrandChildData(srcFormData, srcTransactionExDto, tgtTransactionExDto, tgtRootMasterUnit, tgtFormData, mappingList, dictUnitSaveAsMapping, tgtRootOneToOneFields);

                    // TransferApiTargetTransactionParameter(tgtTransactionExDto, tgtFormData, dictTargetParameterKeyAndValue);

                    if (tgtTransactionExDto.ApiInputParameterList != null && tgtTransactionExDto.ApiInputParameterList.Count > 0)
                    {
                        TransferTargetTransactionApiParameters(srcFormData, srcTransactionExDto, tgtFormData, mappingList, dictTargetParameterKeyAndValue);
                    }

                    AppCascadingBL.SetupIntialCscadingFieldDataSource(tgtTransactionExDto, tgtFormData, false);
                    AppCascadingBL.SetupIntialAutoCompleteFieldDataSource(tgtTransactionExDto, tgtFormData, false);

                    tgtFormData.IsNew = true;
                    tgtFormData.DataTransferFromMasterDetailDto = srcFormData;
                    tgtFormData.DataTransferSettingId = dataTransferSettingId.Value;


                    //if (tgtFormData.RootPrimaryKeyValue == null)
                    //{
                    //    Dictionary<string, object> rootPrimaryKeyValue = AppMasterDetailFormDataSaveBL.GetUnitPkFiledValue(tgtTransactionExDto.RootMasterUnit, tgtFormData.DictOneToOneFields);

                    //    if (rootPrimaryKeyValue.Values.Count > 0)
                    //    {
                    //        tgtFormData.RootPrimaryKeyValue = rootPrimaryKeyValue.Values.First();
                    //    }


                    //}

                    return tgtFormData;
                }
            }
            return null;
        }


        public static void PrepareApiPayloadData_FromMasterDetailFormDataTransfer(AppMasterDetailDto srcFormData, int? dataTransferSettingId,
            out Dictionary<string, string> headers,
            out Dictionary<string, string> queryParams,
            out Dictionary<string, string> pathParams,
            out Dictionary<string, string> environmentVairables,
            out dynamic postDynamicData)
        {
            headers = new Dictionary<string, string>();
            queryParams = new Dictionary<string, string>();
            pathParams = new Dictionary<string, string>();
            environmentVairables = new Dictionary<string, string>();
            postDynamicData = new ExpandoObject();

            if (dataTransferSettingId.HasValue)
            {
                AppTransactionDataTransferSettingExDto dataTransferDto = AppTransactionDataTransferSettingBL.RetrieveOneAppTransactionDataTransferSettingExDto(dataTransferSettingId.Value);

                if (dataTransferDto.TargetApiOperationId.HasValue
                    && dataTransferDto.TransactionId == srcFormData.TransactionId
                    && dataTransferDto.TargetApiOperationDto != null
                    && dataTransferDto.DataTransferMappingList != null)
                {
                    AppTransactionExDto srcTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(dataTransferDto.TransactionId);
                    var srcRootMasterUnit = srcTransactionExDto.RootMasterUnit;

                    var targetApiOperationDto = dataTransferDto.TargetApiOperationDto;

                    PrepareApiPayloadData_FrommDataTransfer_PrepareApiInputParameters(srcFormData, queryParams, pathParams, environmentVairables, dataTransferDto, srcTransactionExDto);

                    if (dataTransferDto.TargetApiPayloadDataStructure != null)
                    {
                        if (!string.IsNullOrWhiteSpace(targetApiOperationDto.JsonSchema))
                        {
                            var rootStructureNode = AppTransactionBL.PrepareApiDataStrucureFromJsonSchema(targetApiOperationDto.JsonSchema);
                            AppIntergrationSettingBL.InitApiNodeAbolutePath(rootStructureNode, null);


                            IDictionary<String, Object> postRootData = ((IDictionary<String, Object>)postDynamicData);

                            PrepareApiPayloadData_FrommDataTransfer_PrepareApiPostData(srcFormData, dataTransferDto, srcTransactionExDto, rootStructureNode, postRootData);
                        }





                    }
                }
            }
        }


        private static void TransferApiTargetTransactionParameter(AppTransactionExDto tgtTransactionExDto, AppMasterDetailDto tgtFormData, Dictionary<string, string> dictTargetParameterKeyAndValue)
        {
            if (tgtTransactionExDto.ApiInputParameterList != null && tgtTransactionExDto.ApiInputParameterList.Count > 0)
            {
                List<string> rootKeyPart = new List<string>();

                foreach (var kvPair in dictTargetParameterKeyAndValue)
                {
                    string targetParamName = kvPair.Key;
                    string paramValue = kvPair.Value;
                    rootKeyPart.Add(targetParamName + AppMasterDetailApiFormDataLoadBL.ApiRootKeyAndValueSplit + paramValue);
                }

                if (rootKeyPart.Count > 0)
                {
                    tgtFormData.RootPrimaryKeyValue = AppMasterDetailApiFormDataLoadBL.ApiRootKeyPrefix + string.Join(AppMasterDetailApiFormDataLoadBL.ApiRootKeySplit, rootKeyPart);
                }
            }
        }

        public static List<AppMasterDetailDto> PrepareDataTransferFormData_FromListEditToMasterDetail(AppListDataDto srcListData, int? dataTransferSettingId)
        {
            if (dataTransferSettingId.HasValue)
            {
                AppTransactionDataTransferSettingExDto dataTransferDto = AppTransactionDataTransferSettingBL.RetrieveOneAppTransactionDataTransferSettingExDto(dataTransferSettingId.Value);

                if (dataTransferDto != null
                    && dataTransferDto.DestinationTransactionId.HasValue && dataTransferDto.TransactionId == srcListData.TransactionId
                    && dataTransferDto.DataTransferMappingList != null)
                {
                    List<AppMasterDetailDto> toReturn = new List<AppMasterDetailDto>();
                    foreach (var srcListDataRow in srcListData.ListData)
                    {



                        AppTransactionExDto srcTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(dataTransferDto.TransactionId);
                        AppTransactionExDto tgtTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(dataTransferDto.DestinationTransactionId.Value);

                        var srcRootMasterUnit = srcTransactionExDto.RootMasterUnit;
                        var tgtRootMasterUnit = tgtTransactionExDto.RootMasterUnit;

                        List<AppTransactionSaveAsMappingExDto> mappingList = dataTransferDto.DataTransferMappingList.ToList();


                        AppMasterDetailDto tgtFormBlankData = AppMasterDetailFormDataLoadBL.GetNewFormData(dataTransferDto.DestinationTransactionId.Value);
                        AppMasterDetailDto tgtFormData = null;
                        string tgtPkValue = "";

                        if (srcRootMasterUnit != null && tgtRootMasterUnit != null)
                        {
                            var tgtPkField = tgtRootMasterUnit.AppTransactionFieldList.FirstOrDefault(o => o.IsPrimaryKey);


                            if (tgtPkField != null)
                            {
                                var pkMapping = dataTransferDto.DataTransferMappingList
                                    .FirstOrDefault(o => o.SourceFiledId.HasValue && o.TargetFiledId.HasValue && o.TargetFiledId.Value == (int)tgtPkField.Id);

                                if (pkMapping != null)
                                {
                                    if (srcTransactionExDto.DictAllTransactionField.ContainsKey(pkMapping.SourceFiledId.Value))
                                    {
                                        var srcField = srcTransactionExDto.DictAllTransactionField[pkMapping.SourceFiledId.Value];

                                        if ((int)srcRootMasterUnit.Id == srcField.TransactionUnitId)
                                        {
                                            tgtPkValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(srcListDataRow.DictOneToOneFields[srcField.DataBaseFieldName]);
                                        }
                                    }
                                }
                            }
                        }

                        bool isTgtTransactionApiReadType = tgtTransactionExDto.BaseApiConfigDto != null
                            && tgtTransactionExDto.BaseApiConfigDto.TransactionCrudType == EmAppTransactionCrudType.Read.ToString();

                        if (!string.IsNullOrWhiteSpace(tgtPkValue) && (tgtTransactionExDto.BaseApiConfigDto == null || isTgtTransactionApiReadType))
                        {
                            tgtFormData = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(dataTransferDto.DestinationTransactionId.Value, tgtPkValue);
                        }
                        else
                        {
                            tgtFormData = AppMasterDetailFormDataLoadBL.GetNewFormData(dataTransferDto.DestinationTransactionId.Value);
                            tgtFormData.DictCascadingFiledDataSource = new Dictionary<string, List<LookupItemDto>>();
                            tgtFormData.DictCascadingFieldIdAndLabel = new Dictionary<string, string>();
                            tgtFormData.EditCloneDictOneToManyFields = tgtFormBlankData.EditCloneDictOneToManyFields;
                        }



                        Dictionary<int, List<AppTransactionSaveAsMappingExDto>> dictUnitSaveAsMapping = mappingList.GroupBy(o => o.MappingUnitId).ToDictionary(o => o.Key.Value, o => o.ToList());

                        Dictionary<int, int?> dictSrcUnitIdAndTgtUnitId = new Dictionary<int, int?>();




                        Dictionary<string, object> srcRootOneToOneFields = srcListDataRow.DictOneToOneFields;
                        Dictionary<string, object> tgtRootOneToOneFields = tgtFormData.DictOneToOneFields;



                        TransferOneUnitOneRow(dictUnitSaveAsMapping, srcRootMasterUnit, tgtRootMasterUnit, srcRootOneToOneFields, tgtRootOneToOneFields);

                        foreach (string srcChildUnitid in srcListDataRow.DictOneToManyFields.Keys)
                        {
                            int srcCUnitId = int.Parse(srcChildUnitid);
                            var srcChildUnit = srcTransactionExDto.DictAllTransactionUnitIdExDto[srcChildUnitid];

                            var aChildUnitMapping = mappingList.FirstOrDefault(o => o.MappingUnitId.HasValue && o.MappingUnitId.Value == srcCUnitId && o.TargetFiledId.HasValue);

                            if (aChildUnitMapping != null)
                            {
                                int aTargetChildUnitFieldId = aChildUnitMapping.TargetFiledId.Value;
                                if (tgtTransactionExDto.DictAllTransactionField.ContainsKey(aTargetChildUnitFieldId))
                                {
                                    int targetUnitId = tgtTransactionExDto.DictAllTransactionField[aTargetChildUnitFieldId].TransactionUnitId;
                                    var tgtChildUnit = tgtTransactionExDto.DictAllTransactionUnitIdExDto[targetUnitId.ToString()];

                                    List<AppChildDataDto> srcChildUnitData = srcListDataRow.DictOneToManyFields[srcChildUnitid];
                                    List<AppChildDataDto> tgtChildUnitData = tgtFormData.DictOneToManyFields[targetUnitId.ToString()];
                                    tgtChildUnitData.Clear();

                                    AppChildDataDto tgtDefaultAppChildDataDto = tgtFormData.EditCloneDictOneToManyFields[targetUnitId.ToString()].FirstOrDefault();

                                    foreach (AppChildDataDto aSrcChildDataDto in srcChildUnitData)
                                    {
                                        var aTgtChildDataDto = tgtDefaultAppChildDataDto.DeepCopy();
                                        tgtChildUnitData.Add(aTgtChildDataDto);

                                        Dictionary<string, object> srcChildOneToOneFields = aSrcChildDataDto.DictOneToOneFields;
                                        Dictionary<string, object> tgtChildOneToOneFields = aTgtChildDataDto.DictOneToOneFields;

                                        TransferOneUnitOneRow(dictUnitSaveAsMapping, srcChildUnit, tgtChildUnit, srcChildOneToOneFields, tgtChildOneToOneFields);

                                    } // end of child

                                }

                            }

                        }

                        AppCascadingBL.SetupIntialCscadingFieldDataSource(tgtTransactionExDto, tgtFormData, false);
                        AppCascadingBL.SetupIntialAutoCompleteFieldDataSource(tgtTransactionExDto, tgtFormData, false);

                        tgtFormData.IsNew = true;
                        tgtFormData.DataTransferFromListEditRowDto = srcListDataRow;
                        tgtFormData.DataTransferSettingId = dataTransferSettingId.Value;

                        //if (tgtFormData.RootPrimaryKeyValue == null)
                        //{
                        //    Dictionary<string, object> rootPrimaryKeyValue = AppMasterDetailFormDataSaveBL.GetUnitPkFiledValue(tgtTransactionExDto.RootMasterUnit, tgtFormData.DictOneToOneFields);

                        //    if (rootPrimaryKeyValue.Values.Count > 0)
                        //    {
                        //        tgtFormData.RootPrimaryKeyValue = rootPrimaryKeyValue.Values.First();
                        //    }
                        //}

                        toReturn.Add(tgtFormData);
                    }


                    return toReturn;
                }


            }

            return null;
        }



        public static AppMessageDto ConvertMasterDetaiFormDataToNotificationDto(int? transactionId, int? transactionRid)
        {
            if (transactionId.HasValue && transactionRid.HasValue)
            {

                AppMessageDto appMessageDto = new AppMessageDto();

                AppMasterDetailDto formData = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(transactionId.Value, transactionRid.Value);
                AppTransactionStructureDto formStructure = AppTransactionStructureLoadBL.GetAppTransactionStructureDto(transactionId.Value);
                AppTransactionExDto transactionExDto = AppTransactionBL.RetrieveOneAppTransactionExDto(transactionId.Value);

                AppTransactionDataTransferSettingExDto dataTransferSettingExDto
                    = AppTransactionDataTransferSettingBL.RetrieveDataTransferSettingByTransactionIdAndInternalCode(transactionId.Value, TransactionDataTransferRegister.AppMessageTransfer.TransferType);

                if (formData == null || formStructure == null || transactionExDto == null || dataTransferSettingExDto == null)
                {
                    return null;
                }

                appMessageDto.Subject += string.Empty;
                appMessageDto.ToList += string.Empty;
                appMessageDto.Message = string.Empty;

                string lineFormatString = "<p><strong>{0}:  </strong>{1}</p>";

                //string lineFormatString = "<p><strong>{0}: </strong><em>{1}</em></p>";

                foreach (var aUnit in transactionExDto.AppTransactionUnitList)
                {
                    if (!aUnit.ParentTransactionUnitId.HasValue)
                    {
                        ConvertMasterDetaiFormDataToNotificationDto_ProcessRootUnit(appMessageDto, formData, formStructure, lineFormatString, aUnit, dataTransferSettingExDto);
                    }
                    else
                    {
                        ConvertMasterDetaiFormDataToNotificationDto_ProcessChildUnit(appMessageDto, formData, aUnit, dataTransferSettingExDto);
                    }
                }

                //if (!string.IsNullOrWhiteSpace(appMessageDto.Message))
                //{
                //    appMessageDto.Message = "<pre>" + appMessageDto.Message + "</pre>";
                //}

                if (formData.DictDocumentIdFileCode.Count > 0)
                {
                    appMessageDto.DictAttachmentFileIdAndDisplay = formData.DictDocumentIdFileCode;
                }

                return appMessageDto;


            }

            return null;

        }

        private static void ConvertMasterDetaiFormDataToNotificationDto_ProcessChildUnit(AppMessageDto appMessageDto, AppMasterDetailDto formData, AppTransactionUnitExDto aUnit, AppTransactionDataTransferSettingExDto dataTransferSettingExDto)
        {
            List<int> toEmailMappingTransFiledIds = dataTransferSettingExDto.DataTransferMappingList
                    .Where(o => o.DestinationInternalCode == TransactionDataTransferRegister.AppMessageTransfer.ToList && o.TransactionFieldId.HasValue).Select(o => o.TransactionFieldId.Value).ToList();

            if (toEmailMappingTransFiledIds.Count > 0)
            {
                if (formData.DictOneToManyFields.ContainsKey(aUnit.Id.ToString()))  // Child Unit
                {
                    var toEmailTransField = aUnit.AppTransactionFieldList.FirstOrDefault(o => toEmailMappingTransFiledIds.Contains((int)o.Id));
                    if (toEmailTransField != null)
                    {
                        foreach (AppChildDataDto childDataDto in formData.DictOneToManyFields[aUnit.Id.ToString()])
                        {
                            if (childDataDto.DictOneToOneFields.ContainsKey(toEmailTransField.DataBaseFieldName))
                            {
                                object fieldValue = childDataDto.DictOneToOneFields[toEmailTransField.DataBaseFieldName];
                                string emailAddress = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(fieldValue).Trim();

                                if (!string.IsNullOrWhiteSpace(emailAddress))
                                {
                                    appMessageDto.ToList += emailAddress + ";";
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void ConvertMasterDetaiFormDataToNotificationDto_ProcessRootUnit(AppMessageDto appMessageDto, AppMasterDetailDto formData,
            AppTransactionStructureDto formStructure, string lineFormatString, AppTransactionUnitExDto aUnit, AppTransactionDataTransferSettingExDto dataTransferSettingExDto)
        {

            foreach (var transField in aUnit.AppTransactionFieldList.OrderBy(o => o.SortOrder))
            {
                if (formData.DictOneToOneFields.ContainsKey(transField.DataBaseFieldName))
                {
                    object fieldValue = formData.DictOneToOneFields[transField.DataBaseFieldName];

                    bool isMappingToSubject = dataTransferSettingExDto.DataTransferMappingList.FirstOrDefault(o => o.DestinationInternalCode == TransactionDataTransferRegister.AppMessageTransfer.Subject && o.TransactionFieldId.HasValue && o.TransactionFieldId.Value == (int)transField.Id) != null;
                    bool isMapptingToMessage = dataTransferSettingExDto.DataTransferMappingList.FirstOrDefault(o => o.DestinationInternalCode == TransactionDataTransferRegister.AppMessageTransfer.Message && o.TransactionFieldId.HasValue && o.TransactionFieldId.Value == (int)transField.Id) != null;
                    bool isMapptingToToList = dataTransferSettingExDto.DataTransferMappingList.FirstOrDefault(o => o.DestinationInternalCode == TransactionDataTransferRegister.AppMessageTransfer.ToList && o.TransactionFieldId.HasValue && o.TransactionFieldId.Value == (int)transField.Id) != null;
                    bool isMapptingToReminderTargetDate = dataTransferSettingExDto.DataTransferMappingList.FirstOrDefault(o => o.DestinationInternalCode == TransactionDataTransferRegister.AppMessageTransfer.ReminderTargetDate && o.TransactionFieldId.HasValue && o.TransactionFieldId.Value == (int)transField.Id) != null;
                    bool isMapptingToIsEnableReminder = dataTransferSettingExDto.DataTransferMappingList.FirstOrDefault(o => o.DestinationInternalCode == TransactionDataTransferRegister.AppMessageTransfer.IsEnableReminder && o.TransactionFieldId.HasValue && o.TransactionFieldId.Value == (int)transField.Id) != null;
                    bool isMapptingToReminderMinutes = dataTransferSettingExDto.DataTransferMappingList.FirstOrDefault(o => o.DestinationInternalCode == TransactionDataTransferRegister.AppMessageTransfer.ReminderMinutes && o.TransactionFieldId.HasValue && o.TransactionFieldId.Value == (int)transField.Id) != null;


                    if (isMappingToSubject || isMapptingToMessage || isMapptingToToList || isMapptingToReminderTargetDate || isMapptingToIsEnableReminder || isMapptingToReminderMinutes)
                    {
                        if (isMappingToSubject)
                        {
                            if (transField.ControlType == (int)EmAppControlType.Memo || transField.ControlType == (int)EmAppControlType.TextBox)
                            {
                                string textValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(fieldValue);
                                appMessageDto.Subject = textValue;
                            }
                        }
                        if (isMapptingToMessage)
                        {
                            if (transField.ControlType == (int)EmAppControlType.AutoGeneration
                                    || transField.ControlType == (int)EmAppControlType.Memo
                                    || transField.ControlType == (int)EmAppControlType.Numeric
                                    || transField.ControlType == (int)EmAppControlType.TextBox)
                            {
                                string textValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(fieldValue);
                                string strFormat = lineFormatString;
                                appMessageDto.Message += string.Format(strFormat, transField.DisplayName, textValue);
                            }
                            else if (transField.ControlType == (int)EmAppControlType.Date || transField.ControlType == (int)EmAppControlType.DateTimeDetail)
                            {
                                string textValue = string.Empty;
                                DateTime? dateValue = ControlTypeValueConverter.ConvertValueToDate(fieldValue);

                                if (dateValue.HasValue)
                                {
                                    textValue = dateValue.Value.ToShortDateString() + " " + dateValue.Value.ToShortTimeString();
                                }

                                appMessageDto.Message += string.Format(lineFormatString, transField.DisplayName, textValue);
                            }

                            else if ((transField.ControlType == (int)EmAppControlType.DDL
                                || transField.ControlType == (int)EmAppControlType.AutoComplete
                                || transField.ControlType == (int)EmAppControlType.SearchAbleDDL) && transField.EntityId.HasValue
                                )
                            {
                                string textValue = string.Empty;

                                if (fieldValue != null)
                                {
                                    if (formStructure.DictStandAloneEntityDataSource.ContainsKey(transField.EntityId.Value.ToString()))
                                    {
                                        List<LookupItemDto> lookupItems = formStructure.DictStandAloneEntityDataSource[transField.EntityId.Value.ToString()];
                                        var lookupDto = lookupItems.FirstOrDefault(o => o.Id.ToString() == fieldValue.ToString());
                                        if (lookupDto != null)
                                        {
                                            textValue = lookupDto.Display;
                                        }
                                    }
                                }

                                appMessageDto.Message += string.Format(lineFormatString, transField.DisplayName, textValue);
                            }
                            else if (transField.ControlType == (int)EmAppControlType.CheckBox)
                            {
                                bool? booleanValue = ControlTypeValueConverter.ConvertValueToBoolean(fieldValue);

                                string textValue = "No";

                                if (booleanValue.HasValue)
                                {
                                    textValue = booleanValue.Value ? "Yes" : "No";
                                }

                                appMessageDto.Message += string.Format(lineFormatString, transField.DisplayName, textValue);
                            }
                        }
                        if (isMapptingToToList)
                        {
                            if (transField.ControlType == (int)EmAppControlType.Memo || transField.ControlType == (int)EmAppControlType.TextBox)
                            {
                                string textValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(fieldValue);

                                if (!string.IsNullOrWhiteSpace(textValue))
                                {
                                    appMessageDto.ToList += textValue.Trim() + ";";
                                }
                            }
                        }
                        if (isMapptingToReminderTargetDate)
                        {
                            if (transField.ControlType == (int)EmAppControlType.Date || transField.ControlType == (int)EmAppControlType.DateTimeDetail)
                            {
                                DateTime? dateValue = ControlTypeValueConverter.ConvertValueToDate(fieldValue);
                                appMessageDto.ReminderTargetDate = dateValue;
                            }
                        }
                        if (isMapptingToIsEnableReminder)
                        {
                            if (transField.ControlType == (int)EmAppControlType.CheckBox)
                            {
                                bool? booleanValue = ControlTypeValueConverter.ConvertValueToBoolean(fieldValue);
                                appMessageDto.IsEnableReminder = booleanValue;
                            }
                        }
                        if (isMapptingToReminderMinutes)
                        {
                            if (transField.ControlType == (int)EmAppControlType.Memo
                            || transField.ControlType == (int)EmAppControlType.Numeric
                            || transField.ControlType == (int)EmAppControlType.TextBox)
                            {
                                appMessageDto.ReminderMinutes = ControlTypeValueConverter.ConvertValueToInt(fieldValue);
                            }
                        }

                    }
                }
            }
        }


        // API Response Data Transfer
        internal static void ProcessApiResponseDataTransfer_DataTransferFromMasterDetail(int dataTransferSettingId, string jsonData, AppMasterDetailDto tgtFormData)
        {
            if (tgtFormData != null && !string.IsNullOrWhiteSpace(jsonData))
            {
                AppTransactionDataTransferSettingExDto dataTransferDto = AppTransactionDataTransferSettingBL.RetrieveOneAppTransactionDataTransferSettingExDto(dataTransferSettingId);


                if (dataTransferDto != null && dataTransferDto.TransactionId.HasValue && dataTransferDto.ApiResponseMappingList != null)
                {
                    //AppTransactionExDto srcTansactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(dataTransferDto.DestinationTransactionId.Value);

                    AppTransactionExDto tgtTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(tgtFormData.TransactionId);

                    //var apiStructure = srcTansactionExDto.ApiDataStructure;

                    var jObj = JObject.Parse(jsonData);



                    Dictionary<string, string> dictMappingPkNameAndJsonPath = dataTransferDto.ApiResponseMappingList
                        .Where(o => !string.IsNullOrWhiteSpace(o.Name) && !string.IsNullOrWhiteSpace(o.JsonPropertyPathName))
                        .ToDictionary(o => o.Name, o => o.JsonPropertyPathName);

                    if (dictMappingPkNameAndJsonPath.Count > 0)
                    {


                        List<string> rootKeyPart = new List<string>();

                        foreach (string inputParamName in dictMappingPkNameAndJsonPath.Keys)
                        {
                            string jsonPath = dictMappingPkNameAndJsonPath[inputParamName];
                            string paramValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(jObj.SelectToken(jsonPath));
                            rootKeyPart.Add(inputParamName + AppMasterDetailApiFormDataLoadBL.ApiRootKeyAndValueSplit + paramValue);
                        }

                        tgtFormData.RootPrimaryKeyValue = AppMasterDetailApiFormDataLoadBL.ApiRootKeyPrefix + string.Join(AppMasterDetailApiFormDataLoadBL.ApiRootKeySplit, rootKeyPart);


                    }



                    Dictionary<int, string> dictAssignToFieldIdAndJsonPath = dataTransferDto.ApiResponseMappingList
                        .Where(o => o.TargetFiledId.HasValue && !string.IsNullOrWhiteSpace(o.JsonPropertyPathName) && !(o.SourceFiledId.HasValue && o.SourceFiledId.Value == 1))
                        .ToDictionary(o => o.TargetFiledId.Value, o => o.JsonPropertyPathName);

                    Dictionary<int, string> dictArrayLogicKeyFieldIdAndJsonPath = dataTransferDto.ApiResponseMappingList
                        .Where(o => o.TargetFiledId.HasValue && !string.IsNullOrWhiteSpace(o.JsonPropertyPathName) && (o.SourceFiledId.HasValue && o.SourceFiledId.Value == 1))
                        .ToDictionary(o => o.TargetFiledId.Value, o => o.JsonPropertyPathName);

                    // rootUnit
                    if (tgtTransactionExDto.RootMasterUnit != null)
                    {
                        foreach (var targetFieldExDto in tgtTransactionExDto.RootMasterUnit.AppTransactionFieldList)
                        {
                            if (dictAssignToFieldIdAndJsonPath.ContainsKey((int)targetFieldExDto.Id))
                            {
                                string jsonPath = dictAssignToFieldIdAndJsonPath[(int)targetFieldExDto.Id];
                                var value = jObj.SelectToken(jsonPath);
                                AppMasterDetailFormDataLoadBL.AssignOneFormFieldValueByType(tgtFormData.DictOneToOneFields, targetFieldExDto, value);
                            }
                        }

                        // Sibling Unit
                        foreach (string tgtSibUnitid in tgtFormData.DictSiblingOneToOneFields.Keys)
                        {
                            AppTransactionUnitExDto tgtSibUnit = tgtTransactionExDto.DictAllTransactionUnitIdExDto[tgtSibUnitid];

                            foreach (var targetFieldExDto in tgtSibUnit.AppTransactionFieldList)
                            {
                                if (dictAssignToFieldIdAndJsonPath.ContainsKey((int)targetFieldExDto.Id))
                                {
                                    string jsonPath = dictAssignToFieldIdAndJsonPath[(int)targetFieldExDto.Id];
                                    var value = jObj.SelectToken(jsonPath);
                                    AppMasterDetailFormDataLoadBL.AssignOneFormFieldValueByType(tgtFormData.DictSiblingOneToOneFields[tgtSibUnitid], targetFieldExDto, value);
                                }
                            }

                        }

                        //AppMasterDetailDto newFormDataDto = AppMasterDetailFormDataLoadBL.GetNewFormData(tgtTransactionExDto);

                        foreach (string tgtChildUnitid in tgtFormData.DictOneToManyFields.Keys)
                        {
                            ProcessApiResponseDataTransfer_ProcessChildUnit(tgtFormData, tgtTransactionExDto, jObj, dictAssignToFieldIdAndJsonPath, dictArrayLogicKeyFieldIdAndJsonPath, tgtChildUnitid);
                        }
                    }

                    tgtFormData.IsDirty = true;
                }
            }

        }

        internal static void ProcessApiResponseDataTransfer_DataTransferFromListEditRowDto(int dataTransferSettingId, string jsonData, AppChildDataDto tgtListEditRow)
        {
            //if (tgtListEditRow != null)
            //{
            //    AppTransactionDataTransferSettingExDto dataTransferDto = AppTransactionDataTransferSettingBL.RetrieveOneAppTransactionDataTransferSettingExDto(dataTransferSettingId);


            //    if (dataTransferDto != null && dataTransferDto.TransactionId.HasValue && dataTransferDto.ApiResponseMappingList != null
            //        && dataTransferDto.DestinationTransactionId.HasValue)
            //    {
            //        //AppTransactionExDto srcTansactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(dataTransferDto.DestinationTransactionId.Value);

            //        AppTransactionExDto tgtTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(tgtFormData.TransactionId);

            //        //var apiStructure = srcTansactionExDto.ApiDataStructure;

            //        var jObj = JObject.Parse(jsonData);

            //        Dictionary<int, string> dictMappingFieldIdAndJsonPath = dataTransferDto.ApiResponseMappingList
            //            .Where(o => o.TargetFiledId.HasValue && !string.IsNullOrWhiteSpace(o.JsonPropertyPathName))
            //            .ToDictionary(o => o.TargetFiledId.Value, o => o.JsonPropertyPathName);

            //        // rootUnit
            //        foreach (var targetFieldExDto in tgtTransactionExDto.RootMasterUnit.AppTransactionFieldList)
            //        {
            //            if (dictMappingFieldIdAndJsonPath.ContainsKey((int)targetFieldExDto.Id))
            //            {
            //                string jsonPath = dictMappingFieldIdAndJsonPath[(int)targetFieldExDto.Id];
            //                var value = jObj.SelectToken(jsonPath);
            //                AppMasterDetailFormDataLoadBL.AssignOneFormFieldValueByType(tgtFormData.DictOneToOneFields, targetFieldExDto, value);
            //            }
            //        }

            //        // Sibling Unit
            //        foreach (string tgtSibUnitid in tgtFormData.DictSiblingOneToOneFields.Keys)
            //        {
            //            AppTransactionUnitExDto tgtSibUnit = tgtTransactionExDto.DictAllTransactionUnitIdExDto[tgtSibUnitid];

            //            foreach (var targetFieldExDto in tgtSibUnit.AppTransactionFieldList)
            //            {
            //                if (dictMappingFieldIdAndJsonPath.ContainsKey((int)targetFieldExDto.Id))
            //                {
            //                    string jsonPath = dictMappingFieldIdAndJsonPath[(int)targetFieldExDto.Id];
            //                    var value = jObj.SelectToken(jsonPath);
            //                    AppMasterDetailFormDataLoadBL.AssignOneFormFieldValueByType(tgtFormData.DictSiblingOneToOneFields[tgtSibUnitid], targetFieldExDto, value);
            //                }
            //            }

            //        }

            //        //AppMasterDetailDto newFormDataDto = AppMasterDetailFormDataLoadBL.GetNewFormData(tgtTransactionExDto);

            //        foreach (string tgtChildUnitid in tgtFormData.DictOneToManyFields.Keys)
            //        {
            //            ProcessApiResponseDataTransfer_ProcessChildUnit(tgtFormData, tgtTransactionExDto, jObj, dictMappingFieldIdAndJsonPath, tgtChildUnitid);
            //        }

            //        tgtFormData.IsDirty = true;
            //    }
            //}

        }

        //private static void ProcessApiResponseDataTransfer_ProcessChildUnit(AppMasterDetailDto tgtFormData, AppTransactionExDto tgtTransactionExDto, JObject jObj, Dictionary<int, string> dictMappingFieldIdAndJsonPath, string tgtChildUnitid)
        //{
        //    AppTransactionUnitExDto tgtChildUnit = tgtTransactionExDto.DictAllTransactionUnitIdExDto[tgtChildUnitid];
        //    AppChildDataDto defaultChildDataDto;
        //    InitChlidOrGrandChildUnitBlankData(tgtFormData, tgtTransactionExDto, tgtChildUnit, out defaultChildDataDto);

        //    List<AppTransactionFieldExDto> childUnit_logicKeyFieldList = tgtChildUnit.AppTransactionFieldList.Where(o => o.IsUnique.HasValue && o.IsUnique.Value).ToList();
        //    List<string> childUnit_logicKeyList = childUnit_logicKeyFieldList.Select(o => o.JsonPath).ToList();
        //    Dictionary<string, AppChildDataDto> dictChildRowLogicKeyAndRowDto = SetupDictLogicKeyAndRowDtoForOneChlidUnit(tgtFormData, tgtChildUnit);

        //    foreach (var childTransField in tgtChildUnit.AppTransactionFieldList)
        //    {
        //        if (!dictMappingFieldIdAndJsonPath.ContainsKey((int)childTransField.Id))
        //        {
        //            continue;
        //        }

        //        string mapping_absoluteJsonPath = dictMappingFieldIdAndJsonPath[(int)childTransField.Id];
        //        JToken arrayNode = FindJObjFirstArrayNodeByAbsolutePath(jObj, mapping_absoluteJsonPath);

        //        if (arrayNode != null && arrayNode.Type == JTokenType.Array && arrayNode.Path.Length < mapping_absoluteJsonPath.Length)
        //        {
        //            string nodeValueJsonPath = mapping_absoluteJsonPath.Substring(arrayNode.Path.Length + 1);

        //            foreach (JToken jToken_arrayItem in (JArray)arrayNode)
        //            {
        //                JToken jToken_Nodevalue = jToken_arrayItem.SelectToken(nodeValueJsonPath);

        //                if (jToken_Nodevalue != null)
        //                {
        //                    var unitRowList = tgtFormData.DictOneToManyFields[tgtChildUnitid];

        //                    if (childUnit_logicKeyList.Count > 0) // Update Existing Row
        //                    {
        //                        string jArrayItem_logicKeyValue = GetJArrayItemLogicKeyValue(childUnit_logicKeyList, jToken_arrayItem);

        //                        if (!string.IsNullOrWhiteSpace(jArrayItem_logicKeyValue) && dictChildRowLogicKeyAndRowDto.ContainsKey(jArrayItem_logicKeyValue))
        //                        {
        //                            var existRow_AppChildDataDto = dictChildRowLogicKeyAndRowDto[jArrayItem_logicKeyValue];
        //                            AppMasterDetailFormDataLoadBL.AssignOneFormFieldValueByType(existRow_AppChildDataDto.DictOneToOneFields, childTransField, jToken_Nodevalue);
        //                        }
        //                        else
        //                        {
        //                            var aTgtChildDataDto = ProcessApiResponseDataTransfer_ProcessAddNewUnitRow(unitRowList, defaultChildDataDto, childUnit_logicKeyFieldList, dictChildRowLogicKeyAndRowDto, jToken_arrayItem);
        //                            AppMasterDetailFormDataLoadBL.AssignOneFormFieldValueByType(aTgtChildDataDto.DictOneToOneFields, childTransField, jToken_Nodevalue);
        //                        }
        //                    }
        //                    else
        //                    {
        //                        var aTgtChildDataDto = ProcessApiResponseDataTransfer_ProcessAddNewUnitRow(unitRowList, defaultChildDataDto, childUnit_logicKeyFieldList, dictChildRowLogicKeyAndRowDto, jToken_arrayItem);
        //                        AppMasterDetailFormDataLoadBL.AssignOneFormFieldValueByType(aTgtChildDataDto.DictOneToOneFields, childTransField, jToken_Nodevalue);
        //                    }
        //                }
        //            }

        //        }
        //    }

        //    foreach (var tgtGrandChildUnit in tgtChildUnit.Children)
        //    {
        //        ProcessApiResponseDataTransfer_ProcessGrandChildUnit(tgtFormData, tgtTransactionExDto, jObj, dictMappingFieldIdAndJsonPath, tgtChildUnit, childUnit_logicKeyList, dictChildRowLogicKeyAndRowDto, tgtGrandChildUnit);

        //    }
        //}

        //private static void ProcessApiResponseDataTransfer_ProcessGrandChildUnit(AppMasterDetailDto tgtFormData, AppTransactionExDto tgtTransactionExDto, JObject jObj, Dictionary<int, string> dictMappingFieldIdAndJsonPath, AppTransactionUnitExDto tgtChildUnit, List<string> childUnit_logicKeyList, Dictionary<string, AppChildDataDto> dictChildRowLogicKeyAndRowDto, AppTransactionUnitExDto tgtGrandChildUnit)
        //{
        //    AppChildDataDto defaultGrandchildDataDto;
        //    InitChlidOrGrandChildUnitBlankData(tgtFormData, tgtTransactionExDto, tgtGrandChildUnit, out defaultGrandchildDataDto);

        //    List<AppTransactionFieldExDto> grandchildUnit_logicKeyFieldList = tgtGrandChildUnit.AppTransactionFieldList.Where(o => o.IsUnique.HasValue && o.IsUnique.Value).ToList();
        //    List<string> grandchildUnit_logicKeyList = grandchildUnit_logicKeyFieldList.Select(o => o.JsonPath).ToList();
        //    Dictionary<string, AppChildDataDto> dictGrandchildRowLogicKeyAndRowDto = SetupDictLogicKeyAndRowDtoForOneGrandchlidUnit(tgtFormData, tgtChildUnit, tgtGrandChildUnit);

        //    foreach (var grandchildTransField in tgtGrandChildUnit.AppTransactionFieldList)
        //    {
        //        if (!dictMappingFieldIdAndJsonPath.ContainsKey((int)grandchildTransField.Id))
        //        {
        //            continue;
        //        }

        //        string mapping_absoluteJsonPath = dictMappingFieldIdAndJsonPath[(int)grandchildTransField.Id];
        //        JToken childUnit_arrayNode = FindJObjFirstArrayNodeByAbsolutePath(jObj, mapping_absoluteJsonPath);

        //        if (childUnit_arrayNode != null && childUnit_arrayNode.Type == JTokenType.Array && childUnit_arrayNode.Path.Length < mapping_absoluteJsonPath.Length)
        //        {
        //            string mapping_JsonAbsoluteSubPath = mapping_absoluteJsonPath.Substring(childUnit_arrayNode.Path.Length + 1);

        //            foreach (JToken jToken_childArrayItem in (JArray)childUnit_arrayNode)
        //            {
        //                string childArrayItem_logicKeyValue = GetJArrayItemLogicKeyValue(childUnit_logicKeyList, jToken_childArrayItem);

        //                if (!dictChildRowLogicKeyAndRowDto.ContainsKey(childArrayItem_logicKeyValue))
        //                {
        //                    break;
        //                }

        //                var childRowDto = dictChildRowLogicKeyAndRowDto[childArrayItem_logicKeyValue];

        //                JToken grandchildUnit_arrayNode = FindJObjFirstArrayNodeByAbsolutePath((JObject)jToken_childArrayItem, mapping_JsonAbsoluteSubPath);

        //                if (grandchildUnit_arrayNode != null && grandchildUnit_arrayNode.Type == JTokenType.Array && grandchildUnit_arrayNode.Path.Length < mapping_absoluteJsonPath.Length)
        //                {
        //                    string nodeValueJsonPath = mapping_absoluteJsonPath.Substring(grandchildUnit_arrayNode.Path.Length + 1);

        //                    foreach (JToken jToken_grandchildArrayItem in (JArray)grandchildUnit_arrayNode)
        //                    {
        //                        JToken jToken_grandchildNodevalue = jToken_grandchildArrayItem.SelectToken(nodeValueJsonPath);

        //                        if (jToken_grandchildNodevalue != null)
        //                        {
        //                            var grandchildUnitRowList = childRowDto.DictOneToManyFields[tgtGrandChildUnit.Id.ToString()];

        //                            if (grandchildUnit_logicKeyList.Count > 0) // Update Existing Row
        //                            {
        //                                string grandchildArrayItem_logicKeyValue = childArrayItem_logicKeyValue + "|" + GetJArrayItemLogicKeyValue(grandchildUnit_logicKeyList, jToken_grandchildArrayItem);

        //                                if (!string.IsNullOrWhiteSpace(grandchildArrayItem_logicKeyValue) && dictGrandchildRowLogicKeyAndRowDto.ContainsKey(grandchildArrayItem_logicKeyValue))
        //                                {
        //                                    var existRow_AppChildDataDto = dictGrandchildRowLogicKeyAndRowDto[grandchildArrayItem_logicKeyValue];
        //                                    AppMasterDetailFormDataLoadBL.AssignOneFormFieldValueByType(existRow_AppChildDataDto.DictOneToOneFields, grandchildTransField, jToken_grandchildNodevalue);
        //                                }
        //                                else
        //                                {
        //                                    var aTgtChildDataDto = ProcessApiResponseDataTransfer_ProcessAddNewUnitRow(grandchildUnitRowList, defaultGrandchildDataDto, grandchildUnit_logicKeyFieldList, dictGrandchildRowLogicKeyAndRowDto, jToken_grandchildArrayItem);
        //                                    AppMasterDetailFormDataLoadBL.AssignOneFormFieldValueByType(aTgtChildDataDto.DictOneToOneFields, grandchildTransField, jToken_grandchildNodevalue);
        //                                }
        //                            }
        //                            else
        //                            {
        //                                var aTgtChildDataDto = ProcessApiResponseDataTransfer_ProcessAddNewUnitRow(grandchildUnitRowList, defaultGrandchildDataDto, grandchildUnit_logicKeyFieldList, dictGrandchildRowLogicKeyAndRowDto, jToken_grandchildArrayItem);
        //                                AppMasterDetailFormDataLoadBL.AssignOneFormFieldValueByType(aTgtChildDataDto.DictOneToOneFields, grandchildTransField, jToken_grandchildNodevalue);
        //                            }
        //                        }
        //                    }

        //                }
        //            }
        //        }
        //    }
        //}



        private static void ProcessApiResponseDataTransfer_ProcessChildUnit(AppMasterDetailDto tgtFormData, AppTransactionExDto tgtTransactionExDto, JObject jObj,
                                    Dictionary<int, string> dictAssignToFieldIdAndJsonPath, Dictionary<int, string> dictArrayLogicKeyFieldIdAndJsonPath, string tgtChildUnitid)
        {
            AppTransactionUnitExDto tgtChildUnit = tgtTransactionExDto.DictAllTransactionUnitIdExDto[tgtChildUnitid];
            AppChildDataDto defaultChildDataDto;
            InitChlidOrGrandChildUnitBlankData(tgtFormData, tgtTransactionExDto, tgtChildUnit, out defaultChildDataDto);
            Dictionary<string, AppTransactionFieldExDto> dictLogicKeyJsonPathAndFieldDto = ProcessApiResponseDataTransfer_ChildUnit_PrepareDictLogicKeyJsonPathAndFieldDto(jObj, dictArrayLogicKeyFieldIdAndJsonPath, tgtChildUnit);

            List<AppTransactionFieldExDto> childUnit_logicKeyFieldList = dictLogicKeyJsonPathAndFieldDto.Values.ToList();
            List<string> childUnit_logicKeyList = dictLogicKeyJsonPathAndFieldDto.Keys.ToList();

            Dictionary<string, AppChildDataDto> dictChildRowLogicKeyAndRowDto = SetupDictLogicKeyAndRowDtoForOneChlidUnit(tgtFormData, tgtChildUnit, childUnit_logicKeyFieldList);

            foreach (var childTransField in tgtChildUnit.AppTransactionFieldList)
            {
                if (!dictAssignToFieldIdAndJsonPath.ContainsKey((int)childTransField.Id))
                {
                    continue;
                }

                string mapping_absoluteJsonPath = dictAssignToFieldIdAndJsonPath[(int)childTransField.Id];
                JToken arrayNode = FindJObjFirstArrayNodeByAbsolutePath(jObj, mapping_absoluteJsonPath);

                if (arrayNode != null && arrayNode.Type == JTokenType.Array && arrayNode.Path.Length < mapping_absoluteJsonPath.Length)
                {
                    string nodeValueJsonPath = mapping_absoluteJsonPath.Substring(arrayNode.Path.Length + 1);

                    foreach (JToken jToken_arrayItem in (JArray)arrayNode)
                    {
                        JToken jToken_Nodevalue = jToken_arrayItem.SelectToken(nodeValueJsonPath);

                        if (jToken_Nodevalue != null)
                        {
                            var unitRowList = tgtFormData.DictOneToManyFields[tgtChildUnitid];

                            if (childUnit_logicKeyList.Count > 0) // Update Existing Row
                            {
                                string jArrayItem_logicKeyValue = GetJArrayItemLogicKeyValue(childUnit_logicKeyList, jToken_arrayItem);

                                if (!string.IsNullOrWhiteSpace(jArrayItem_logicKeyValue) && dictChildRowLogicKeyAndRowDto.ContainsKey(jArrayItem_logicKeyValue))
                                {
                                    var existRow_AppChildDataDto = dictChildRowLogicKeyAndRowDto[jArrayItem_logicKeyValue];
                                    AppMasterDetailFormDataLoadBL.AssignOneFormFieldValueByType(existRow_AppChildDataDto.DictOneToOneFields, childTransField, jToken_Nodevalue);
                                }
                                else
                                {
                                    var aTgtChildDataDto = ProcessApiResponseDataTransfer_ProcessAddNewUnitRow(unitRowList, defaultChildDataDto, dictLogicKeyJsonPathAndFieldDto, dictChildRowLogicKeyAndRowDto, jToken_arrayItem);
                                    if (aTgtChildDataDto != null)
                                    {
                                        AppMasterDetailFormDataLoadBL.AssignOneFormFieldValueByType(aTgtChildDataDto.DictOneToOneFields, childTransField, jToken_Nodevalue);
                                    }
                                }
                            }
                        }
                    }

                }
            }

            foreach (var tgtGrandChildUnit in tgtChildUnit.Children)
            {
                ProcessApiResponseDataTransfer_ProcessGrandChildUnit(tgtFormData, tgtTransactionExDto, jObj, dictAssignToFieldIdAndJsonPath, dictArrayLogicKeyFieldIdAndJsonPath, tgtChildUnit, dictLogicKeyJsonPathAndFieldDto, dictChildRowLogicKeyAndRowDto, tgtGrandChildUnit);

            }
        }


        private static void ProcessApiResponseDataTransfer_ProcessGrandChildUnit(AppMasterDetailDto tgtFormData, AppTransactionExDto tgtTransactionExDto, JObject jObj,
            Dictionary<int, string> dictAssignToFieldIdAndJsonPath, Dictionary<int, string> dictArrayLogicKeyFieldIdAndJsonPath, AppTransactionUnitExDto tgtChildUnit,
            Dictionary<string, AppTransactionFieldExDto> dictChildLogicKeyJsonPathAndFieldDto, Dictionary<string, AppChildDataDto> dictChildRowLogicKeyAndRowDto, AppTransactionUnitExDto tgtGrandChildUnit)
        {
            AppChildDataDto defaultGrandchildDataDto;
            InitChlidOrGrandChildUnitBlankData(tgtFormData, tgtTransactionExDto, tgtGrandChildUnit, out defaultGrandchildDataDto);

            Dictionary<string, AppTransactionFieldExDto> dictGrandChildLogicKeyJsonPathAndFieldDto = ProcessApiResponseDataTransfer_GrandChildUnit_PrepareDictLogicKeyJsonPathAndFieldDto(jObj, dictArrayLogicKeyFieldIdAndJsonPath, tgtGrandChildUnit);
            List<AppTransactionFieldExDto> grandchildUnit_logicKeyFieldList = dictGrandChildLogicKeyJsonPathAndFieldDto.Values.ToList();
            List<string> grandchildUnit_logicKeyList = dictGrandChildLogicKeyJsonPathAndFieldDto.Keys.ToList();

            Dictionary<string, AppChildDataDto> dictGrandchildRowLogicKeyAndRowDto = SetupDictLogicKeyAndRowDtoForOneGrandchlidUnit(tgtFormData, tgtChildUnit, tgtGrandChildUnit, dictChildLogicKeyJsonPathAndFieldDto.Values.ToList(), grandchildUnit_logicKeyFieldList);

            foreach (var grandchildTransField in tgtGrandChildUnit.AppTransactionFieldList)
            {
                if (!dictAssignToFieldIdAndJsonPath.ContainsKey((int)grandchildTransField.Id))
                {
                    continue;
                }

                string mapping_absoluteJsonPath = dictAssignToFieldIdAndJsonPath[(int)grandchildTransField.Id];
                JToken childUnit_arrayNode = FindJObjFirstArrayNodeByAbsolutePath(jObj, mapping_absoluteJsonPath);

                if (childUnit_arrayNode != null && childUnit_arrayNode.Type == JTokenType.Array && childUnit_arrayNode.Path.Length < mapping_absoluteJsonPath.Length)
                {
                    string mapping_JsonAbsoluteSubPath = mapping_absoluteJsonPath.Substring(childUnit_arrayNode.Path.Length + 1);

                    foreach (JToken jToken_childArrayItem in (JArray)childUnit_arrayNode)
                    {
                        string childArrayItem_logicKeyValue = GetJArrayItemLogicKeyValue(dictChildLogicKeyJsonPathAndFieldDto.Keys.ToList(), jToken_childArrayItem);

                        if (!dictChildRowLogicKeyAndRowDto.ContainsKey(childArrayItem_logicKeyValue))
                        {
                            break;
                        }

                        var childRowDto = dictChildRowLogicKeyAndRowDto[childArrayItem_logicKeyValue];

                        JToken grandchildUnit_arrayNode = FindJObjFirstArrayNodeByAbsolutePath((JObject)jToken_childArrayItem, mapping_JsonAbsoluteSubPath);
                        if (grandchildUnit_arrayNode != null && grandchildUnit_arrayNode.Type == JTokenType.Array)
                        {
                            int lastArrayIndex = grandchildUnit_arrayNode.Path.LastIndexOf("]");
                            string arrayRelativePath = grandchildUnit_arrayNode.Path.Substring(lastArrayIndex + 1);
                            int arrayAbsolutePathLength = (childUnit_arrayNode.Path + arrayRelativePath).Length;

                            if (mapping_absoluteJsonPath.Length > arrayAbsolutePathLength)
                            {
                                string nodeValueJsonPath = mapping_absoluteJsonPath.Substring(arrayAbsolutePathLength + 1);

                                foreach (JToken jToken_grandchildArrayItem in (JArray)grandchildUnit_arrayNode)
                                {
                                    JToken jToken_grandchildNodevalue = jToken_grandchildArrayItem.SelectToken(nodeValueJsonPath);

                                    if (jToken_grandchildNodevalue != null)
                                    {
                                        var grandchildUnitRowList = childRowDto.DictOneToManyFields[tgtGrandChildUnit.Id.ToString()];

                                        if (grandchildUnit_logicKeyList.Count > 0) // Update Existing Row
                                        {
                                            string grandchildArrayItem_logicKeyValue = childArrayItem_logicKeyValue + "|" + GetJArrayItemLogicKeyValue(grandchildUnit_logicKeyList, jToken_grandchildArrayItem);

                                            if (!string.IsNullOrWhiteSpace(grandchildArrayItem_logicKeyValue) && dictGrandchildRowLogicKeyAndRowDto.ContainsKey(grandchildArrayItem_logicKeyValue))
                                            {
                                                var existRow_AppChildDataDto = dictGrandchildRowLogicKeyAndRowDto[grandchildArrayItem_logicKeyValue];
                                                AppMasterDetailFormDataLoadBL.AssignOneFormFieldValueByType(existRow_AppChildDataDto.DictOneToOneFields, grandchildTransField, jToken_grandchildNodevalue);
                                            }
                                            else
                                            {
                                                var aTgtChildDataDto = ProcessApiResponseDataTransfer_ProcessAddNewUnitRow(grandchildUnitRowList, defaultGrandchildDataDto, dictGrandChildLogicKeyJsonPathAndFieldDto, dictGrandchildRowLogicKeyAndRowDto, jToken_grandchildArrayItem);
                                                if (aTgtChildDataDto != null)
                                                {
                                                    AppMasterDetailFormDataLoadBL.AssignOneFormFieldValueByType(aTgtChildDataDto.DictOneToOneFields, grandchildTransField, jToken_grandchildNodevalue);
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
        }

        private static Dictionary<string, AppTransactionFieldExDto> ProcessApiResponseDataTransfer_ChildUnit_PrepareDictLogicKeyJsonPathAndFieldDto(
           JObject jObj, Dictionary<int, string> dictArrayLogicKeyFieldIdAndJsonPath, AppTransactionUnitExDto tgtGridUnit)
        {
            Dictionary<string, AppTransactionFieldExDto> dictLogicKeyJsonPathAndFieldDto = new Dictionary<string, AppTransactionFieldExDto>();

            foreach (var fieldDto in tgtGridUnit.AppTransactionFieldList)
            {
                if (dictArrayLogicKeyFieldIdAndJsonPath.Keys.Contains((int)fieldDto.Id))
                {
                    string mapping_absoluteJsonPath = dictArrayLogicKeyFieldIdAndJsonPath[(int)fieldDto.Id];
                    JToken arrayNode = FindJObjFirstArrayNodeByAbsolutePath(jObj, mapping_absoluteJsonPath);

                    if (arrayNode != null && arrayNode.Type == JTokenType.Array && arrayNode.Path.Length < mapping_absoluteJsonPath.Length)
                    {
                        string nodeValueJsonPath = mapping_absoluteJsonPath.Substring(arrayNode.Path.Length + 1);
                        dictLogicKeyJsonPathAndFieldDto.Add(nodeValueJsonPath, fieldDto);
                    }
                }
            }

            return dictLogicKeyJsonPathAndFieldDto;
        }


        private static Dictionary<string, AppTransactionFieldExDto> ProcessApiResponseDataTransfer_GrandChildUnit_PrepareDictLogicKeyJsonPathAndFieldDto(
           JObject jObj, Dictionary<int, string> dictArrayLogicKeyFieldIdAndJsonPath, AppTransactionUnitExDto tgtGridUnit)
        {
            Dictionary<string, AppTransactionFieldExDto> dictLogicKeyJsonPathAndFieldDto = new Dictionary<string, AppTransactionFieldExDto>();

            foreach (var fieldDto in tgtGridUnit.AppTransactionFieldList)
            {
                if (dictArrayLogicKeyFieldIdAndJsonPath.Keys.Contains((int)fieldDto.Id))
                {
                    string mapping_absoluteJsonPath = dictArrayLogicKeyFieldIdAndJsonPath[(int)fieldDto.Id];
                    JToken childArrayNode = FindJObjFirstArrayNodeByAbsolutePath(jObj, mapping_absoluteJsonPath);

                    if (childArrayNode != null && childArrayNode.Type == JTokenType.Array && childArrayNode.Path.Length < mapping_absoluteJsonPath.Length)
                    {
                        string nodeValueJsonPath = mapping_absoluteJsonPath.Substring(childArrayNode.Path.Length + 1);

                        
                        if (((JArray)childArrayNode).Count > 0)
                        {
                            JToken firstChild = ((JArray)childArrayNode)[0];

                            if (firstChild != null && firstChild.Type == JTokenType.Object)
                            {
                                JToken grandChildArrayNode = FindJObjFirstArrayNodeByAbsolutePath((JObject)firstChild, nodeValueJsonPath);

                                if (grandChildArrayNode != null && grandChildArrayNode.Type == JTokenType.Array)
                                {
                                    // grandChildArrayNode.Path: "order.line_items[0].tax_lines"

                                    int lastArrayIndex = grandChildArrayNode.Path.LastIndexOf("]");
                                    string arrayRelativePath = grandChildArrayNode.Path.Substring(lastArrayIndex + 1);
                                    int arrayAbsolutePathLength = (childArrayNode.Path + arrayRelativePath).Length;

                                    if (mapping_absoluteJsonPath.Length > arrayAbsolutePathLength) {
                                        string gchildNodeValueJsonPath = mapping_absoluteJsonPath.Substring(arrayAbsolutePathLength + 1);
                                        dictLogicKeyJsonPathAndFieldDto.Add(gchildNodeValueJsonPath, fieldDto);
                                    }
                                }
                            }      
                        }
                    }
                }
            }

            return dictLogicKeyJsonPathAndFieldDto;
        }


        private static void InitChlidOrGrandChildUnitBlankData(AppMasterDetailDto tgtFormData, AppTransactionExDto tgtTransactionExDto,
            AppTransactionUnitExDto tgtChildUnit, out AppChildDataDto tgtDefaultAppChildDataDto)
        {
            tgtDefaultAppChildDataDto = new AppChildDataDto();
            tgtDefaultAppChildDataDto.DictOneToOneFields = new Dictionary<string, object>();
            tgtDefaultAppChildDataDto.DictOneToManyFields = new Dictionary<string, List<AppChildDataDto>>();

            foreach (var grandChildUnitDto in tgtChildUnit.Children)
            {
                tgtDefaultAppChildDataDto.DictOneToManyFields.Add(grandChildUnitDto.Id.ToString(), new List<AppChildDataDto>());
            }

            foreach (var fieldDto in tgtChildUnit.AppTransactionFieldList)
            {
                tgtDefaultAppChildDataDto.DictOneToOneFields.Add(fieldDto.DataBaseFieldName, null);
            }
        }


        private static AppChildDataDto ProcessApiResponseDataTransfer_ProcessAddNewUnitRow(List<AppChildDataDto> unitRowList, AppChildDataDto defaultRowDataDto, Dictionary<string, AppTransactionFieldExDto> dictLogicKeyJsonPathAndFieldDto, Dictionary<string, AppChildDataDto> dictRowLogicKeyAndRowDto, JToken jToken_arrayItem)
        {

            if (dictLogicKeyJsonPathAndFieldDto.Count > 0)
            {
                var aTgtChildDataDto = defaultRowDataDto.DeepCopy();
                aTgtChildDataDto.IsNew = true;

                string logicKeyValue = "";

                foreach (var kvPair in dictLogicKeyJsonPathAndFieldDto)
                {
                    string jsonPath = kvPair.Key;
                    AppTransactionFieldExDto fieldDto = kvPair.Value;

                    var jtoken_logicKeyValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(jToken_arrayItem.SelectToken(jsonPath));
                    AppMasterDetailFormDataLoadBL.AssignOneFormFieldValueByType(aTgtChildDataDto.DictOneToOneFields, fieldDto, jtoken_logicKeyValue);

                    logicKeyValue += jtoken_logicKeyValue + "|";
                }

                logicKeyValue = logicKeyValue.Substring(0, logicKeyValue.Length - 1);

                if (!dictRowLogicKeyAndRowDto.ContainsKey(logicKeyValue))
                {
                    dictRowLogicKeyAndRowDto.Add(logicKeyValue, aTgtChildDataDto);
                }

                unitRowList.Add(aTgtChildDataDto);

                return aTgtChildDataDto;
            }

            return null;
        }

        private static string GetJArrayItemLogicKeyValue(List<string> logicKeyList, JToken jToken_arrayItem)
        {
            string jArrayItem_logicKeyValue = "";

            if (logicKeyList.Count > 0)
            {

                foreach (string logicKey in logicKeyList)
                {
                    string logicKeyValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(jToken_arrayItem.SelectToken(logicKey));
                    jArrayItem_logicKeyValue += logicKeyValue + "|";
                }

                jArrayItem_logicKeyValue = jArrayItem_logicKeyValue.Substring(0, jArrayItem_logicKeyValue.Length - 1);
            }

            return jArrayItem_logicKeyValue;
        }

        private static JToken FindJObjFirstArrayNodeByAbsolutePath(JObject jObj, string absoluteJsonPath)
        {
            string[] pathArray = absoluteJsonPath.Split(new string[] { "." }, StringSplitOptions.None);

            int? arrayTokenLevel = FindJsonAbsolutePath_FirstArrayTokenPathLevel((JToken)jObj, pathArray, 0);

            if (arrayTokenLevel.HasValue)
            {
                string path_arrayToken = GetTokenSubPathByLevel(pathArray, arrayTokenLevel.Value);

                if (!string.IsNullOrWhiteSpace(path_arrayToken))
                {

                    JToken arrayNode = jObj.SelectToken(path_arrayToken);

                    if (arrayNode != null)
                    {
                        return arrayNode;
                    }
                }
            }

            return null;
        }

        private static Dictionary<string, AppChildDataDto> SetupDictLogicKeyAndRowDtoForOneChlidUnit(AppMasterDetailDto formData, AppTransactionUnitExDto childUnit, List<AppTransactionFieldExDto> logicKeyTransFields)
        {
            Dictionary<string, AppChildDataDto> dictRowLogicKeyAndRowDto = new Dictionary<string, AppChildDataDto>();

            string tgtChildUnitid = childUnit.Id.ToString();

            if (logicKeyTransFields.Count > 0)
            {

                List<AppChildDataDto> existChildRows = formData.DictOneToManyFields[tgtChildUnitid];

                foreach (var rowDto in existChildRows)
                {
                    string logicKeyValue = "";

                    foreach (var keyFieldDto in logicKeyTransFields)
                    {
                        logicKeyValue += ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(rowDto.DictOneToOneFields[keyFieldDto.DataBaseFieldName]) + "|";
                    }
                    logicKeyValue = logicKeyValue.Substring(0, logicKeyValue.Length - 1);

                    if (!dictRowLogicKeyAndRowDto.ContainsKey(logicKeyValue))
                    {
                        dictRowLogicKeyAndRowDto.Add(logicKeyValue, rowDto);
                    }
                }
            }

            return dictRowLogicKeyAndRowDto;
        }

        private static Dictionary<string, AppChildDataDto> SetupDictLogicKeyAndRowDtoForOneGrandchlidUnit(AppMasterDetailDto formData, AppTransactionUnitExDto childUnit, AppTransactionUnitExDto grandchildUnit,
            List<AppTransactionFieldExDto> childLogicKeyTransFields, List<AppTransactionFieldExDto> grandchildLogicKeyTransFields)
        {
            Dictionary<string, AppChildDataDto> dictRowLogicKeyAndRowDto = new Dictionary<string, AppChildDataDto>();

            string childUnitid = childUnit.Id.ToString();
            string grandchildUnitid = grandchildUnit.Id.ToString();          

            if (childLogicKeyTransFields.Count > 0 && grandchildLogicKeyTransFields.Count > 0)
            {
                List<AppChildDataDto> existChildRows = formData.DictOneToManyFields[childUnitid];

                foreach (var rowDto in existChildRows)
                {
                    string childLogicKeyValue = "";

                    foreach (var keyFieldDto in childLogicKeyTransFields)
                    {
                        childLogicKeyValue += ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(rowDto.DictOneToOneFields[keyFieldDto.DataBaseFieldName]) + "|";
                    }

                    foreach (var grandChildRowDto in rowDto.DictOneToManyFields[grandchildUnitid])
                    {
                        string grandchildLogicKeyValue = childLogicKeyValue;

                        foreach (var keyFieldDto in grandchildLogicKeyTransFields)
                        {
                            grandchildLogicKeyValue += ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(grandChildRowDto.DictOneToOneFields[keyFieldDto.DataBaseFieldName]) + "|";
                        }

                        grandchildLogicKeyValue = grandchildLogicKeyValue.Substring(0, grandchildLogicKeyValue.Length - 1);

                        if (!dictRowLogicKeyAndRowDto.ContainsKey(grandchildLogicKeyValue))
                        {
                            dictRowLogicKeyAndRowDto.Add(grandchildLogicKeyValue, grandChildRowDto);
                        }
                    }

                }
            }

            return dictRowLogicKeyAndRowDto;
        }


        internal static int? FindJsonAbsolutePath_FirstArrayTokenPathLevel(JToken aJToken, string[] levelPathList, int level)
        {
            string nodeName = levelPathList[level];
            JToken childJToken = aJToken.SelectToken(nodeName);

            if (childJToken != null)
            {
                if (level == levelPathList.Length - 1)
                {
                    return null;
                }
                else
                {
                    if (childJToken.Type == JTokenType.Array)
                    {
                        return level;
                    }
                    else
                    {
                        level++;
                        return FindJsonAbsolutePath_FirstArrayTokenPathLevel(childJToken, levelPathList, level);
                    }
                }
            }

            return null;
        }

        private static string GetTokenSubPathByLevel(string[] levelPathList, int level)
        {
            string tokenSubPath = "";

            for (int i = 0; i <= level && i < levelPathList.Length; i++)
            {
                if (tokenSubPath.Length > 0)
                {
                    tokenSubPath += "." + levelPathList[i];
                }
                else
                {
                    tokenSubPath += levelPathList[i];
                }
            }

            return tokenSubPath;
        }

        private static string PrepareDataTransferFormData_PrepareTargetPkValue(AppMasterDetailDto srcFormData, AppTransactionDataTransferSettingExDto dataTransferDto, AppTransactionExDto srcTransactionExDto, AppTransactionUnitExDto srcRootMasterUnit, AppTransactionUnitExDto tgtRootMasterUnit, string tgtPkValue)
        {
            if (srcRootMasterUnit != null && tgtRootMasterUnit != null)
            {
                var tgtPkField = tgtRootMasterUnit.AppTransactionFieldList.FirstOrDefault(o => o.IsPrimaryKey);


                if (tgtPkField != null)
                {
                    var pkMapping = dataTransferDto.DataTransferMappingList
                        .FirstOrDefault(o => o.SourceFiledId.HasValue && o.TargetFiledId.HasValue && o.TargetFiledId.Value == (int)tgtPkField.Id);

                    if (pkMapping != null)
                    {
                        if (srcTransactionExDto.DictAllTransactionField.ContainsKey(pkMapping.SourceFiledId.Value))
                        {
                            var srcField = srcTransactionExDto.DictAllTransactionField[pkMapping.SourceFiledId.Value];

                            if ((int)srcRootMasterUnit.Id == srcField.TransactionUnitId)
                            {
                                tgtPkValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(srcFormData.DictOneToOneFields[srcField.DataBaseFieldName]);
                            }
                            else
                            {
                                if (srcTransactionExDto.DictAllTransactionUnitIdExDto.ContainsKey(srcField.TransactionUnitId.ToString()))
                                {
                                    var unitDto = srcTransactionExDto.DictAllTransactionUnitIdExDto[srcField.TransactionUnitId.ToString()];

                                    if (unitDto.IsMasterSiblingUnit.HasValue && unitDto.IsMasterSiblingUnit.Value)
                                    {
                                        string siblingUnitId = srcField.TransactionUnitId.ToString();
                                        tgtPkValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(srcFormData.DictSiblingOneToOneFields[siblingUnitId][srcField.DataBaseFieldName]);
                                    }
                                }

                            }
                        }
                    }
                }
            }

            return tgtPkValue;
        }

        private static void TransferOneUnitOneRow(Dictionary<int, List<AppTransactionSaveAsMappingExDto>> dictUnitMapping,
          AppTransactionUnitExDto srcRootMasterUnit, AppTransactionUnitExDto tgtRootMasterUnit,
          Dictionary<string, object> srcRootOneToOneFields, Dictionary<string, object> tgtRootOneToOneFields,
          AppTransactionExDto srcTransactionExDto = null, AppTransactionExDto tgtTransactionExDto = null, Dictionary<string, string> dictTargetParameterKeyAndValue = null)
        {
            if (srcRootMasterUnit != null && tgtRootMasterUnit != null)
            {

                Dictionary<int, string> dictSrcIdDbfieldname = srcRootMasterUnit.AppTransactionFieldList.ToDictionary(o => (int)o.Id, o => o.DataBaseFieldName);
                Dictionary<int, string> dictTgtIdDbfieldname = tgtRootMasterUnit.AppTransactionFieldList.ToDictionary(o => (int)o.Id, o => o.DataBaseFieldName);

                if (dictUnitMapping.ContainsKey((int)srcRootMasterUnit.Id))
                {
                    List<AppTransactionSaveAsMappingExDto> unitListmapping = dictUnitMapping[(int)srcRootMasterUnit.Id];

                    foreach (var mapping in unitListmapping)
                    {
                        if (mapping.SourceFiledId.HasValue && mapping.TargetFiledId.HasValue)
                        {
                            // needto blank this field
                            if (mapping.IsBlankTargetField.HasValue && mapping.IsBlankTargetField.Value)
                            {
                                string blankFieldName = dictTgtIdDbfieldname[mapping.TargetFiledId.Value];

                                tgtRootOneToOneFields[blankFieldName] = null;
                            }
                            else
                            {
                                if (dictSrcIdDbfieldname.ContainsKey(mapping.SourceFiledId.Value) && dictTgtIdDbfieldname.ContainsKey(mapping.TargetFiledId.Value))
                                {
                                    string sourceFieldName = dictSrcIdDbfieldname[mapping.SourceFiledId.Value];
                                    string targetFieldName = dictTgtIdDbfieldname[mapping.TargetFiledId.Value];

                                    tgtRootOneToOneFields[targetFieldName] = srcRootOneToOneFields[sourceFieldName];
                                }

                            }
                        }

                        if (dictTargetParameterKeyAndValue != null && tgtTransactionExDto != null)
                        {
                            if (mapping.SourceFiledId.HasValue && !string.IsNullOrWhiteSpace(mapping.JsonPropertyPathName)
                                && tgtTransactionExDto.ApiInputParameterList != null && tgtTransactionExDto.ApiInputParameterList.Count > 0)
                            {
                                if (dictSrcIdDbfieldname.ContainsKey(mapping.SourceFiledId.Value))
                                {
                                    string sourceFieldName = dictSrcIdDbfieldname[mapping.SourceFiledId.Value];

                                    dictTargetParameterKeyAndValue[mapping.JsonPropertyPathName] = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(srcRootOneToOneFields[sourceFieldName]);
                                }


                            }
                        }
                    }
                }
            }
        }

        private static void TransferChildAndGrandChildData(AppMasterDetailDto srcFormData, AppTransactionExDto srcTransactionExDto, AppTransactionExDto tgtTransactionExDto, AppTransactionUnitExDto tgtRootMasterUnit, AppMasterDetailDto tgtFormData, List<AppTransactionSaveAsMappingExDto> mappingList, Dictionary<int, List<AppTransactionSaveAsMappingExDto>> dictUnitSaveAsMapping, Dictionary<string, object> tgtRootOneToOneFields)
        {
            foreach (string srcChildUnitid in srcFormData.DictOneToManyFields.Keys)
            {
                int srcCUnitId = int.Parse(srcChildUnitid);
                var srcChildUnit = srcTransactionExDto.DictAllTransactionUnitIdExDto[srcChildUnitid];

                var aChildUnitMapping = mappingList.FirstOrDefault(o => o.MappingUnitId.HasValue && o.MappingUnitId.Value == srcCUnitId && o.TargetFiledId.HasValue);

                if (aChildUnitMapping != null)
                {
                    int aTargetChildUnitFieldId = aChildUnitMapping.TargetFiledId.Value;
                    if (tgtTransactionExDto.DictAllTransactionField.ContainsKey(aTargetChildUnitFieldId))
                    {
                        int targetUnitId = tgtTransactionExDto.DictAllTransactionField[aTargetChildUnitFieldId].TransactionUnitId;
                        var tgtChildUnit = tgtTransactionExDto.DictAllTransactionUnitIdExDto[targetUnitId.ToString()];

                        List<AppChildDataDto> srcChildUnitData = srcFormData.DictOneToManyFields[srcChildUnitid];

                        if (srcFormData.CommandForeachLoopChildUnitId.HasValue && srcFormData.CommandForeachLoopChildUnitId.Value == srcCUnitId)
                        {
                            if (srcFormData.CommandForeachLoopToRowData != null)
                            {
                                TransferOneUnitOneRow(dictUnitSaveAsMapping, srcChildUnit, tgtRootMasterUnit, srcFormData.CommandForeachLoopToRowData.DictOneToOneFields, tgtRootOneToOneFields, srcTransactionExDto, tgtTransactionExDto);

                            }
                        }
                        else
                        {
                            if (tgtFormData.DictOneToManyFields.ContainsKey(targetUnitId.ToString()))
                            {
                                List<AppChildDataDto> tgtChildUnitData = tgtFormData.DictOneToManyFields[targetUnitId.ToString()];
                                tgtChildUnitData.Clear();

                                AppChildDataDto tgtDefaultAppChildDataDto = tgtFormData.EditCloneDictOneToManyFields[targetUnitId.ToString()].FirstOrDefault();

                                foreach (AppChildDataDto aSrcChildDataDto in srcChildUnitData)
                                {
                                    var aTgtChildDataDto = tgtDefaultAppChildDataDto.DeepCopy();
                                    tgtChildUnitData.Add(aTgtChildDataDto);

                                    Dictionary<string, object> srcChildOneToOneFields = aSrcChildDataDto.DictOneToOneFields;
                                    Dictionary<string, object> tgtChildOneToOneFields = aTgtChildDataDto.DictOneToOneFields;

                                    TransferOneUnitOneRow(dictUnitSaveAsMapping, srcChildUnit, tgtChildUnit, srcChildOneToOneFields, tgtChildOneToOneFields, srcTransactionExDto, tgtTransactionExDto);

                                    //grand child
                                    foreach (string srcGrandUnitId in aSrcChildDataDto.DictOneToManyFields.Keys)
                                    {
                                        int srcGUnitId = int.Parse(srcGrandUnitId);
                                        var srcGradnchildUnit = srcTransactionExDto.DictAllTransactionUnitIdExDto[srcGrandUnitId];

                                        var aGChildUnitMapping = mappingList.FirstOrDefault(o => o.MappingUnitId.HasValue && o.MappingUnitId.Value == srcGUnitId && o.TargetFiledId.HasValue);
                                        if (aGChildUnitMapping != null)
                                        {
                                            int aTargetGChildUnitFieldId = aGChildUnitMapping.TargetFiledId.Value;

                                            if (tgtTransactionExDto.DictAllTransactionField.ContainsKey(aTargetGChildUnitFieldId))
                                            {
                                                int targetGrandUnitId = tgtTransactionExDto.DictAllTransactionField[aTargetGChildUnitFieldId].TransactionUnitId;
                                                var tgtGrandChildUnit = tgtTransactionExDto.DictAllTransactionUnitIdExDto[targetGrandUnitId.ToString()];

                                                if (aTgtChildDataDto.DictOneToManyFields.ContainsKey(targetGrandUnitId.ToString()))
                                                {
                                                    List<AppChildDataDto> srcGrandchildOneToOneFieldsList = aSrcChildDataDto.DictOneToManyFields[srcGrandUnitId];
                                                    List<AppChildDataDto> tgtGrandchildOneToOneFieldsList = aTgtChildDataDto.DictOneToManyFields[targetGrandUnitId.ToString()];

                                                    AppChildDataDto tgtCloneAppGrandChildDataDto = tgtFormData.EditCloneDictOneToManyFields[targetGrandUnitId.ToString()].FirstOrDefault();

                                                    foreach (var aAppChildDataDto in srcGrandchildOneToOneFieldsList)
                                                    {
                                                        Dictionary<string, object> srcGradnOneToOneFields = aAppChildDataDto.DictOneToOneFields;
                                                        var aTgtGrandChildDataDto = tgtCloneAppGrandChildDataDto.DeepCopy();
                                                        aTgtGrandChildDataDto.DictOneToOneFields = srcGradnOneToOneFields;
                                                        tgtGrandchildOneToOneFieldsList.Add(aTgtGrandChildDataDto);

                                                        TransferOneUnitOneRow(dictUnitSaveAsMapping, srcGradnchildUnit, tgtGrandChildUnit, srcGradnOneToOneFields, aTgtGrandChildDataDto.DictOneToOneFields, srcTransactionExDto, tgtTransactionExDto);
                                                    }
                                                }
                                            }
                                        }
                                    } // end of grand child
                                } // end of child
                            }
                        }
                    }

                }

            }
        }

        private static void TransferTargetTransactionApiParameters(AppMasterDetailDto srcFormData, AppTransactionExDto srcTransactionExDto, AppMasterDetailDto tgtFormData, List<AppTransactionSaveAsMappingExDto> mappingList, Dictionary<string, string> dictTargetParameterKeyAndValue)
        {
            if (srcTransactionExDto.ApiInputParameterList != null && srcTransactionExDto.ApiInputParameterList.Count > 0)
            {
                if (srcFormData.RootPrimaryKeyValue != null)
                {
                    Dictionary<string, object> dictSrcMasterPkValues = new Dictionary<string, object>();

                    AppMasterDetailApiFormDataLoadBL.GetDictMasterPkAndValueFromApiRootKeyConcatString(srcFormData.RootPrimaryKeyValue, srcTransactionExDto, dictSrcMasterPkValues);

                    var apiInputParamMappings = mappingList.Where(o => !string.IsNullOrWhiteSpace(o.Name) && !string.IsNullOrWhiteSpace(o.JsonPropertyPathName)).ToList();

                    foreach (var mappingDto in apiInputParamMappings)
                    {
                        string srcParamName = mappingDto.Name;
                        string targetParamName = mappingDto.JsonPropertyPathName;

                        if (dictSrcMasterPkValues.ContainsKey(srcParamName))
                        {
                            dictTargetParameterKeyAndValue[targetParamName] = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dictSrcMasterPkValues[srcParamName]);
                        }
                    }

                }
            }

            List<string> rootKeyPart = new List<string>();

            foreach (var kvPair in dictTargetParameterKeyAndValue)
            {
                string targetParamName = kvPair.Key;
                string paramValue = kvPair.Value;
                rootKeyPart.Add(targetParamName + AppMasterDetailApiFormDataLoadBL.ApiRootKeyAndValueSplit + paramValue);
            }

            if (rootKeyPart.Count > 0)
            {
                tgtFormData.RootPrimaryKeyValue = AppMasterDetailApiFormDataLoadBL.ApiRootKeyPrefix + string.Join(AppMasterDetailApiFormDataLoadBL.ApiRootKeySplit, rootKeyPart);
            }
        }

        //private static void TransferSrcTransactionConsumeAPIInputParameterToTarget(AppMasterDetailDto srcFormData, AppTransactionExDto srcTransactionExDto, AppTransactionExDto tgtTransactionExDto, AppMasterDetailDto tgtFormData, List<AppTransactionSaveAsMappingExDto> mappingList)
        //{
        //    if (srcTransactionExDto.ApiInputParameterList != null && srcTransactionExDto.ApiInputParameterList.Count > 0
        //       && tgtTransactionExDto.ApiInputParameterList != null && tgtTransactionExDto.ApiInputParameterList.Count > 0)
        //    {
        //        if (srcFormData.RootPrimaryKeyValue != null)
        //        {
        //            Dictionary<string, object> dictSrcMasterPkValues = new Dictionary<string, object>();

        //            AppMasterDetailApiFormDataLoadBL.GetDictMasterPkAndValueFromApiRootKeyConcatString(srcFormData.RootPrimaryKeyValue, srcTransactionExDto, dictSrcMasterPkValues);

        //            var apiInputParamMappings = mappingList.Where(o => !string.IsNullOrWhiteSpace(o.Name) && !string.IsNullOrWhiteSpace(o.JsonPropertyPathName)).ToList();

        //            List<string> rootKeyPart = new List<string>();

        //            foreach (var mappingDto in apiInputParamMappings)
        //            {
        //                string srcParamName = mappingDto.Name;
        //                string targetParamName = mappingDto.JsonPropertyPathName;

        //                if (dictSrcMasterPkValues.ContainsKey(srcParamName))
        //                {
        //                    string paramValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dictSrcMasterPkValues[srcParamName]);
        //                    rootKeyPart.Add(targetParamName + AppMasterDetailApiFormDataLoadBL.ApiRootKeyAndValueSplit + paramValue);
        //                }
        //            }


        //            if (rootKeyPart.Count > 0)
        //            {
        //                tgtFormData.RootPrimaryKeyValue = AppMasterDetailApiFormDataLoadBL.ApiRootKeyPrefix + string.Join(AppMasterDetailApiFormDataLoadBL.ApiRootKeySplit, rootKeyPart);
        //            }

        //        }
        //    }
        //}

        private static void PrepareApiPayloadData_FrommDataTransfer_PrepareApiInputParameters(AppMasterDetailDto srcFormData, Dictionary<string, string> queryParams, Dictionary<string, string> pathParams,
            Dictionary<string, string> environmentVairables, AppTransactionDataTransferSettingExDto dataTransferDto, AppTransactionExDto srcTransactionExDto)
        {
            if (dataTransferDto.TargetApiInputParameterList != null)
            {


                foreach (string key in dataTransferDto.TargetApiInputParameterList)
                {
                    var mappingDto = dataTransferDto.DataTransferMappingList.FirstOrDefault(o => o.Name == key);
                    if (mappingDto != null && mappingDto.SourceFiledId.HasValue && srcTransactionExDto.DictAllTransactionField.ContainsKey(mappingDto.SourceFiledId.Value))
                    {
                        var transFieldDto = srcTransactionExDto.DictAllTransactionField[mappingDto.SourceFiledId.Value];

                        string paramValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(srcFormData.DictOneToOneFields[transFieldDto.DataBaseFieldName]);

                        if (key.StartsWith(AppMasterDetailApiFormDataLoadBL.ApiPathParamPrefix))
                        {
                            string paramName = key.Substring(AppMasterDetailApiFormDataLoadBL.ApiPathParamPrefix.Length);
                            if (!string.IsNullOrWhiteSpace(paramName))
                            {
                                pathParams[paramName] = paramValue;
                            }
                        }
                        else if (key.StartsWith(AppMasterDetailApiFormDataLoadBL.ApiQueryParamPrefix))
                        {
                            string paramName = key.Substring(AppMasterDetailApiFormDataLoadBL.ApiQueryParamPrefix.Length);
                            if (!string.IsNullOrWhiteSpace(paramName))
                            {
                                queryParams[paramName] = paramValue;
                            }
                        }
                        else if (key.StartsWith(AppMasterDetailApiFormDataLoadBL.ApiEvironmentVariablePrefix))
                        {
                            string paramName = key.Substring(AppMasterDetailApiFormDataLoadBL.ApiEvironmentVariablePrefix.Length);
                            if (!string.IsNullOrWhiteSpace(paramName) && !string.IsNullOrWhiteSpace(paramValue))
                            {
                                environmentVairables[paramName] = paramValue;
                            }
                        }
                    }
                }
            }
        }

        internal static dynamic PrepareApiPayloadData_FrommDataTransfer_PrepareApiPostData(AppMasterDetailDto srcFormData, AppTransactionDataTransferSettingExDto dataTransferDto,
            AppTransactionExDto srcTransactionExDto, ApiDataStructureNodeDto rootStructureNode, dynamic postDynamicData)
        {
            var postRootData = ((IDictionary<String, Object>)postDynamicData);

            PrepareApiPayloadData_ProcessOneObjOrArrayNodeChildNodes(1, srcFormData, dataTransferDto, srcTransactionExDto, rootStructureNode, postRootData, null, null);

            return postDynamicData;
        }

        private static void PrepareApiPayloadData_ProcessOneObjOrArrayNodeChildNodes(int unitLevel, AppMasterDetailDto srcFormData, AppTransactionDataTransferSettingExDto dataTransferDto,
            AppTransactionExDto srcTransactionExDto, ApiDataStructureNodeDto structureNode, IDictionary<string, object> postDataNode,
            AppTransactionUnitExDto mapToGridUnitDto, AppChildDataDto mapToGridUnitRow)
        {
            if (structureNode.Children != null)
            {
                foreach (var childStructureNode in structureNode.Children)
                {
                    if (childStructureNode.IsObject)
                    {
                        postDataNode[childStructureNode.Name] = new ExpandoObject();
                        var postDataChildNode = (IDictionary<string, object>)postDataNode[childStructureNode.Name];

                        PrepareApiPayloadData_ProcessOneObjOrArrayNodeChildNodes(unitLevel, srcFormData, dataTransferDto, srcTransactionExDto, childStructureNode, postDataChildNode, mapToGridUnitDto, mapToGridUnitRow);
                    }
                    else if (childStructureNode.IsArray)
                    {
                        PrepareApiPayloadData_ProcessOneArrayNode(unitLevel + 1, srcFormData, dataTransferDto, srcTransactionExDto, postDataNode, childStructureNode, mapToGridUnitDto, mapToGridUnitRow);
                    }
                    else
                    {
                        postDataNode[childStructureNode.Name] = null;

                        string jsonPropertyPath = childStructureNode.AbsolutePath;

                        if (!string.IsNullOrWhiteSpace(jsonPropertyPath))
                        {
                            var transferMappingDto = dataTransferDto.AppTransactionSaveAsMappingList.FirstOrDefault(o => !(o.IsBlankTargetField.HasValue && o.IsBlankTargetField.Value) && o.SourceFiledId.HasValue && o.JsonPropertyPathName == jsonPropertyPath);

                            if (transferMappingDto != null)
                            {
                                if (unitLevel == 1)
                                {
                                    var rootTransFieldDto = srcTransactionExDto.RootMasterUnit.AppTransactionFieldList.FirstOrDefault(o => (int)o.Id == transferMappingDto.SourceFiledId.Value);

                                    if (rootTransFieldDto != null)
                                    {
                                        var fieldValue = srcFormData.DictOneToOneFields[rootTransFieldDto.DataBaseFieldName];
                                        postDataNode[childStructureNode.Name] = fieldValue;
                                    }
                                    else
                                    {
                                        foreach (var siblingUnitDto in srcTransactionExDto.AppTransactionUnitList.Where(o => o.IsMasterSiblingUnit.HasValue && o.IsMasterSiblingUnit.Value))
                                        {
                                            var siblingTransFieldDto = siblingUnitDto.AppTransactionFieldList.FirstOrDefault(o => (int)o.Id == transferMappingDto.SourceFiledId.Value);

                                            if (siblingTransFieldDto != null)
                                            {
                                                var fieldValue = srcFormData.DictSiblingOneToOneFields[siblingUnitDto.Id.ToString()][siblingTransFieldDto.DataBaseFieldName];
                                                postDataNode[childStructureNode.Name] = fieldValue;
                                                break;
                                            }
                                        }
                                    }
                                }
                                else if (unitLevel >= 2)
                                {
                                    if (mapToGridUnitDto != null && mapToGridUnitRow != null)
                                    {
                                        var transFieldDto = mapToGridUnitDto.AppTransactionFieldList.FirstOrDefault(o => (int)o.Id == transferMappingDto.SourceFiledId.Value);

                                        if (transFieldDto != null)
                                        {
                                            var fieldValue = mapToGridUnitRow.DictOneToOneFields[transFieldDto.DataBaseFieldName];
                                            postDataNode[childStructureNode.Name] = fieldValue;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void PrepareApiPayloadData_ProcessOneArrayNode(int unitLevel, AppMasterDetailDto srcFormData, AppTransactionDataTransferSettingExDto dataTransferDto, AppTransactionExDto srcTransactionExDto,
            IDictionary<string, object> postDataNode, ApiDataStructureNodeDto structureNode,
            AppTransactionUnitExDto parentGridUnit, AppChildDataDto parentGridUnitRow)
        {
            if (structureNode.IsSimpleList)
            {
                postDataNode[structureNode.Name] = new List<object>();
            }
            else
            {
                List<ExpandoObject> postChildDataRowList = new List<ExpandoObject>();
                postDataNode[structureNode.Name] = postChildDataRowList;

                AppTransactionUnitExDto mapToGridUnitDto = null;

                mapToGridUnitDto = FindMapToChildOrGrandChildUnit(unitLevel, srcFormData, dataTransferDto, srcTransactionExDto, postDataNode, structureNode);

                if (mapToGridUnitDto != null)
                {
                    if (unitLevel == 2)
                    {
                        if (srcFormData.DictOneToManyFields.ContainsKey(mapToGridUnitDto.Id.ToString()))
                        {
                            var childRowList = srcFormData.DictOneToManyFields[mapToGridUnitDto.Id.ToString()];

                            foreach (var childRow in childRowList)
                            {
                                var postChildDynamicDataRow = new ExpandoObject();
                                var postChildDataRow = ((IDictionary<String, Object>)postChildDynamicDataRow);

                                postChildDataRowList.Add(postChildDynamicDataRow);

                                PrepareApiPayloadData_ProcessOneObjOrArrayNodeChildNodes(unitLevel, srcFormData, dataTransferDto, srcTransactionExDto, structureNode, postChildDataRow, mapToGridUnitDto, childRow);
                            }
                        }
                    }
                    else if (unitLevel == 3)
                    {
                        if (parentGridUnit != null && parentGridUnitRow != null)
                        {
                            if (parentGridUnitRow.DictOneToManyFields.ContainsKey(mapToGridUnitDto.Id.ToString()))
                            {
                                var grandchildRowList = parentGridUnitRow.DictOneToManyFields[mapToGridUnitDto.Id.ToString()];

                                foreach (var grandchildRow in grandchildRowList)
                                {
                                    var postChildDynamicDataRow = new ExpandoObject();
                                    var postChildDataRow = ((IDictionary<String, Object>)postChildDynamicDataRow);

                                    postChildDataRowList.Add(postChildDynamicDataRow);

                                    PrepareApiPayloadData_ProcessOneObjOrArrayNodeChildNodes(unitLevel, srcFormData, dataTransferDto, srcTransactionExDto, structureNode, postChildDataRow, mapToGridUnitDto, grandchildRow);
                                }
                            }
                        }
                    }
                }

            }
        }

        private static AppTransactionUnitExDto FindMapToChildOrGrandChildUnit(int unitLevel, AppMasterDetailDto srcFormData, AppTransactionDataTransferSettingExDto dataTransferDto, AppTransactionExDto srcTransactionExDto, IDictionary<string, object> postDataNode, ApiDataStructureNodeDto structureNode)
        {
            AppTransactionUnitExDto mapToGridUnitDto = null;

            if (structureNode.Children != null)
            {
                foreach (var propertyNode in structureNode.Children.Where(o => !o.IsArray && !o.IsObject))
                {
                    var transferMappingDto = dataTransferDto.AppTransactionSaveAsMappingList.FirstOrDefault(o => !(o.IsBlankTargetField.HasValue && o.IsBlankTargetField.Value) && o.SourceFiledId.HasValue && o.JsonPropertyPathName == propertyNode.AbsolutePath);
                    if (transferMappingDto != null)
                    {
                        if (srcTransactionExDto.DictAllTransactionField.ContainsKey(transferMappingDto.SourceFiledId.Value))
                        {
                            var transFieldDto = srcTransactionExDto.DictAllTransactionField[transferMappingDto.SourceFiledId.Value];
                            int unitId = transFieldDto.TransactionUnitId;

                            if (unitLevel == 2)
                            {
                                mapToGridUnitDto = srcTransactionExDto.RootMasterUnit.Children.FirstOrDefault(o => (int)o.Id == unitId);

                                if (mapToGridUnitDto != null)
                                {
                                    break;
                                }
                            }
                            else if (unitLevel == 3)
                            {
                                foreach (var childUnit in srcTransactionExDto.RootMasterUnit.Children)
                                {
                                    mapToGridUnitDto = childUnit.Children.FirstOrDefault(o => (int)o.Id == unitId);

                                    if (mapToGridUnitDto != null)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                if (mapToGridUnitDto == null)
                {
                    foreach (var childObjNode in structureNode.Children.Where(o => o.IsObject))
                    {
                        postDataNode[childObjNode.Name] = new ExpandoObject();
                        var postDataChildNode = (IDictionary<string, object>)postDataNode[childObjNode.Name];

                        mapToGridUnitDto = FindMapToChildOrGrandChildUnit(unitLevel, srcFormData, dataTransferDto, srcTransactionExDto, postDataChildNode, childObjNode);

                        if (mapToGridUnitDto != null)
                        {
                            break;
                        }
                    }
                }
            }

            return mapToGridUnitDto;
        }
    }
}