using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using APP.Persistence.Common;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;

using APP.Framework;
namespace App.BL
{
    public static class AppSearchViewCommandBL
    {
        public static OperationCallResult<TransactionCommandActionResultDto> ExcuteOneTransactionCommonad(AppMasterDetailDto aformData, int? chlldUnitId = null, string childRowPkCombString = null)
        {
            OperationCallResult<TransactionCommandActionResultDto> aOperationCallResult = new OperationCallResult<TransactionCommandActionResultDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (aformData.TransactionCommandId.HasValue)
            {
                var commandDto = AppTransactionCommandBL.RetrieveOneTransactionCommandExDto(aformData.TransactionCommandId.Value);
                var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aformData.TransactionId);

                if (commandDto != null && transactionExDto != null)
                {
                    bool isAllowExecuteByCondition = AppTransactionCommandBL.CheckCommandCondition(aformData, transactionExDto, commandDto, null);

                    if (isAllowExecuteByCondition)
                    {
                        if (commandDto.ActionType.HasValue)
                        {
                            Dictionary<int, object> dictOneRowFiedIdValue = AppTransactionFormulaBL.ConvertUnitOneRowDbFileNameToFileId(transactionExDto.RootMasterUnit, aformData.DictOneToOneFields);
                            Dictionary<int, object> dictChildRowFiedIdValue = PrepareChildUnitRowDictFieldIdAndValue(aformData, chlldUnitId, childRowPkCombString, transactionExDto);

                            if (commandDto.ActionType.Value == (int)EmAppTransactionCommandType.CompositionCommand)
                            {
                                aOperationCallResult = ExecuteCompositionCommand(dictOneRowFiedIdValue, commandDto, aformData, chlldUnitId, childRowPkCombString);
                            }
                            else
                            {
                                aOperationCallResult = ExecuteSingleCommandByActionType(dictOneRowFiedIdValue, commandDto, aformData, dictChildRowFiedIdValue);
                            }
                        }
                    }
                    else
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Condition pass failure", ValidationItemType.Error, "Command execute condition is not ready."));
                    }

                }
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Command_Error", ValidationItemType.Error, "Empty Command"));
            }



            return aOperationCallResult;
        }

       

        public static OperationCallResult<TransactionCommandActionResultDto> ExecuteOneTransactionCommonadById(int commandId, int transactionId, string rootPrimaryKeyValue, int? chlldUnitId = null, string childRowPkCombString = null)
        {
            AppMasterDetailDto aformData = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(transactionId, rootPrimaryKeyValue);
            aformData.TransactionCommandId = commandId;

            return ExcuteOneTransactionCommonad(aformData, chlldUnitId, childRowPkCombString);            
        }



        public static AppProjectWorkFlowActionExDto RetrieveOneTransactionCommandExDto(int? CommanfWorkFlowActionId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppProjectWorkFlowActionEntity aAppProjectWorkFlowActionEntity = new AppProjectWorkFlowActionEntity(CommanfWorkFlowActionId.Value);
                adapter.FetchEntity(aAppProjectWorkFlowActionEntity);

                AppProjectWorkFlowActionExDto dto = AppProjectWorkFlowActionConverter.ConvertEntityToExDto(aAppProjectWorkFlowActionEntity);

                return dto;
            }
        }


        public static OperationCallResult<bool> SaveOneSearchViewCommandActionList(AppTransactionExDto aAppTransactionExDto)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;



            if (aAppTransactionExDto != null && aAppTransactionExDto.Id != null && aAppTransactionExDto.CommandActionList != null)
            {
                var needToSaveCommandList = aAppTransactionExDto.CommandActionList;
                var orgTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aAppTransactionExDto.Id);

                List<int> deletActionIds = new List<int>();
                EntityCollection<AppProjectWorkFlowActionEntity> newActionEntities = new EntityCollection<AppProjectWorkFlowActionEntity>();
                EntityCollection<AppProjectWorkFlowActionEntity> dirtyActionEntities = new EntityCollection<AppProjectWorkFlowActionEntity>();

                var orgCommandActionList = AppTransactionCommandBL.RetrieveOneTransactionCommandActionList(orgTransactionExDto);


                List<int> actionIdDB = orgCommandActionList.Where(o => o.Id != null).Select(o => (int)o.Id).ToList();
                List<int> actionIdUI = needToSaveCommandList.Where(o => o.Id != null).Select(o => (int)o.Id).ToList();

                deletActionIds = actionIdDB.Except(actionIdUI).ToList();


                List<AppTransactionFieldExDto> transactionFieldList = orgTransactionExDto.DictRootLevelUnitTransactionField.Values.ToList();
                AppPorjectWorkFlowBL.InitialTransactionFieldFormularDisplayName(orgTransactionExDto, transactionFieldList);

                foreach (var actionDto in needToSaveCommandList)
                {
                    AppPorjectWorkFlowBL.InFormatActionFormulaExpress(transactionFieldList, actionDto);
                }

                foreach (var dto in needToSaveCommandList)
                {
                    if (!dto.CommandTransactionId.HasValue)
                    {
                        dto.CommandTransactionId = ControlTypeValueConverter.ConvertValueToInt(aAppTransactionExDto.Id);
                    }

                    AppProjectWorkFlowActionEntity entity = new AppProjectWorkFlowActionEntity();
                    AppProjectWorkFlowActionConverter.CopyDtoToEntity(entity, dto);
                    entity.CommandTransactionId = (int)aAppTransactionExDto.Id;
                    if (dto.Id != null)
                    {
                        entity.WorkFlowActionId = (int)dto.Id;
                        dirtyActionEntities.Add(entity);
                    }
                    else
                    {
                        newActionEntities.Add(entity);
                    }
                }

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        if (newActionEntities.Count > 0)
                        {
                            adapter.SaveEntityCollection(newActionEntities);
                        }

                        if (dirtyActionEntities.Count > 0)
                        {
                            foreach (var dirtyEntity in dirtyActionEntities)
                            {
                                adapter.UpdateEntitiesDirectly(dirtyEntity, new RelationPredicateBucket(AppProjectWorkFlowActionFields.WorkFlowActionId == dirtyEntity.WorkFlowActionId));
                            }
                        }

                        if (deletActionIds.Count > 0)
                        {
                            adapter.DeleteEntitiesDirectly(typeof(AppProjectWorkFlowActionEntity), new RelationPredicateBucket(AppProjectWorkFlowActionFields.WorkFlowActionId == deletActionIds));
                        }

                        adapter.Commit();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "App_TransactionEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                        aOperationCallResult.Object = true;

                        AppCacheManagerBL.RefreshOnetHierarchyTranscation(aAppTransactionExDto.Id);
                    }


                    // Database FK Exception .......
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "App_TransactionEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }

            }



            return aOperationCallResult;
        }


        public static List<AppProjectWorkFlowActionExDto> RetrieveOneTransactionCommandActionList(int? searchViewId)
        {
            List<AppProjectWorkFlowActionExDto> toReturn = new List<AppProjectWorkFlowActionExDto>();

       

            if (searchViewId.HasValue )
            {
                AppSearchViewExDto appSearchViewExDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(searchViewId);

                foreach ( var searchFied in appSearchViewExDto.AppSearchViewFieldList )
                {

                }

                using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
                {
                    EntityCollection<AppProjectWorkFlowActionEntity> commandActionList = new EntityCollection<AppProjectWorkFlowActionEntity>();
                    adpater.FetchEntityCollection(commandActionList, new RelationPredicateBucket(AppProjectWorkFlowActionFields.CommandSearchViewId == searchViewId.Value));


                   var transactionFieldList = new List<AppTransactionFieldExDto>();



                    foreach (var entity in commandActionList)
                    {
                        var actionDto = AppProjectWorkFlowActionConverter.ConvertEntityToExDto(entity);
                        AppPorjectWorkFlowBL.OutFormatActionFormulaExpress(transactionFieldList, actionDto);
                        toReturn.Add(actionDto);



                    }
                }
            }
            return toReturn;
        }


        //public static OperationCallResult<TransactionCommandActionResultDto> ExcuteTransactionCommonad(AppMasterDetailDto aformData)
        //{
        //    OperationCallResult<TransactionCommandActionResultDto> aOperationCallResult = new OperationCallResult<TransactionCommandActionResultDto>();
        //    ValidationResult aValidationResult = new ValidationResult();
        //    aOperationCallResult.ValidationResult = aValidationResult;

        //    int? actionType = null;

        //    if (aformData.TransactionCommandId.HasValue)
        //    {
        //        var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aformData.TransactionId);

        //        if (transactionExDto != null)
        //        {
        //            var commandDto = RetrieveOneTransactionCommandExDto(aformData.TransactionCommandId.Value);
        //            actionType = commandDto.ActionType;

        //            var dictOneRowFiedIdValue = AppTransactionFormulaBL.ConvertUnitOneRowDbFileNameToFileId(transactionExDto.RootMasterUnit, aformData.DictOneToOneFields);

        //            bool isConditionAllowExecute = false;

        //            if (commandDto.CommandConditionTransactionFieldId.HasValue)
        //            {
        //                int conditionId = commandDto.CommandConditionTransactionFieldId.Value;
        //                var dictRootFiedValie = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(aformData, transactionExDto); ;
        //                if (!dictRootFiedValie.IsEmpty() && dictRootFiedValie.ContainsKey(conditionId))
        //                {
        //                    bool? conditionResult = ControlTypeValueConverter.ConvertValueToBoolean(dictRootFiedValie[conditionId]);

        //                    if (conditionResult.HasValue && conditionResult.Value)
        //                    {
        //                        isConditionAllowExecute = true;
        //                    }
        //                }
        //                else
        //                {
        //                    aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Condition pass failure", ValidationItemType.Error, "Condition pass failure"));
        //                }
        //            }
        //            else
        //            {
        //                isConditionAllowExecute = true;
        //            }

        //            if (isConditionAllowExecute)
        //            {
        //                if (actionType.HasValue)
        //                {
        //                    if (actionType.Value == (int)EmAppTransactionCommandType.CompositionCommand)
        //                    {
        //                        aOperationCallResult = ExecuteCompositionCommand(dictOneRowFiedIdValue, commandDto, aformData);
        //                    }
        //                    else
        //                    {
        //                        aOperationCallResult = ExecuteSingleCommandByActionType(dictOneRowFiedIdValue, commandDto, aformData);
        //                    }
        //                }
        //            }

        //        }
        //    }
        //    else
        //    {
        //        aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Command_Error", ValidationItemType.Error, "Empty Command"));
        //    }



        //    return aOperationCallResult;
        //}

        //public static OperationCallResult<TransactionCommandActionResultDto> ExecuteCompositionCommand(Dictionary<int, object> dictOneRowFiedIdValue, AppProjectWorkFlowActionExDto actionDto, AppMasterDetailDto aformData)
        //{
        //    OperationCallResult<TransactionCommandActionResultDto> toReturn = new OperationCallResult<TransactionCommandActionResultDto>();
        //    toReturn.ValidationResult = new ValidationResult();


        //    toReturn.Object = new TransactionCommandActionResultDto();
        //    toReturn.Object.CommandId = ControlTypeValueConverter.ConvertValueToInt(actionDto.Id);
        //    toReturn.Object.ActionTypeId = actionDto.ActionType;
        //    toReturn.Object.FormData = aformData;


        //    if (actionDto.ActionAttribute != null && actionDto.ActionAttribute.ChildActionList != null)
        //    {                
        //        toReturn.Object.ChildCommandResultDtoList = new List<TransactionCommandActionResultDto>();

        //        foreach (int commandId in actionDto.ActionAttribute.ChildActionList.Where(o => o.CommandId.HasValue).OrderBy(o => o.Sort).Select(o => o.CommandId.Value))
        //        {
        //            toReturn.Object.FormData.TransactionCommandId = commandId;

        //            var aChlidCommandResult = ExcuteTransactionCommonad(toReturn.Object.FormData);
        //            if (aChlidCommandResult != null && aChlidCommandResult.Object != null)
        //            {
        //                if (aChlidCommandResult.IsSuccessfulWithResult)
        //                {
        //                    toReturn.Object.ChildCommandResultDtoList.Add(aChlidCommandResult.Object);
        //                    if (aChlidCommandResult.Object.FormData != null)
        //                    {
        //                        toReturn.Object.FormData = aChlidCommandResult.Object.FormData;
        //                    }
        //                }
        //                else
        //                {
        //                    toReturn.ValidationResult.Merge(aChlidCommandResult.ValidationResult);
        //                }
        //            }
        //        }
        //    }

        //    return toReturn;
        //}






        //public static OperationCallResult<TransactionCommandActionResultDto> ExecuteSingleCommandByActionType(Dictionary<int, object> dictOneRowFiedIdValue, AppProjectWorkFlowActionExDto actionDto, AppMasterDetailDto aformData)
        //{
        //    OperationCallResult<TransactionCommandActionResultDto> toReturn = new OperationCallResult<TransactionCommandActionResultDto>();
        //    toReturn.ValidationResult = new ValidationResult();

        //    if (actionDto != null && actionDto.ActionType.HasValue)
        //    {
        //        toReturn.Object = new TransactionCommandActionResultDto();
        //        toReturn.Object.CommandId = ControlTypeValueConverter.ConvertValueToInt(actionDto.Id);
        //        toReturn.Object.ActionTypeId = actionDto.ActionType;
        //        toReturn.Object.FormData = aformData;

        //        if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.ExecuteSQLStatement)
        //        {
        //            AppProjectWorkFlowDataFormSynchBL.ExcusteSqlStatmentActionWithRootValue(dictOneRowFiedIdValue, actionDto);
        //        }
        //        else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.PluginWebApiCall)
        //        {
        //            AppTransactionCommandBL.ExecuteCommand_PluginWebApiCall(actionDto, toReturn);
        //        }
        //        else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.ExecuteDataTransfer)
        //        {
        //            AppTransactionCommandBL.ExecuteCommand_DataTransfer(actionDto, toReturn);
        //        }
        //        else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.Calculation)
        //        {
        //            AppTransactionCommandBL.ExecuteCommand_Calculation(toReturn);
        //        }
        //        else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.ExecuteAllDataLoad)
        //        {
        //            AppTransactionCommandBL.ExecuteCommand_ExecuteAllDataLoad(toReturn);
        //        }
        //        else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.GenerateMatrix)
        //        {
        //            AppTransactionCommandBL.ExecuteCommand_GenerateMatrix(actionDto, toReturn);

        //        }
        //        else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.SaveAs)
        //        {
        //            AppTransactionCommandBL.ExecuteCommand_SaveAs(actionDto, toReturn);
        //        }
        //        else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.OpenFormCreationWindow)
        //        {
        //            AppTransactionCommandBL.ExecuteCommand_OpenFormCreationWindow(actionDto, toReturn);
        //        }
        //        else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.refresh)
        //        {
        //            AppTransactionCommandBL.ExecuteCommand_Refresh(toReturn);
        //        }
        //        else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.Save)
        //        {
        //            AppTransactionCommandBL.ExecuteCommand_Save(toReturn);
        //        }
        //        else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.CompositionCommand)
        //        {
        //            if (actionDto.ActionAttribute != null && actionDto.ActionAttribute.ChildActionList != null)
        //            {
        //                //toReturn.Object = new TransactionCommandActionResultDto();
        //                toReturn.Object.ChildCommandResultDtoList = new List<TransactionCommandActionResultDto>();

        //                foreach (int commandId in actionDto.ActionAttribute.ChildActionList.Where(o => o.CommandId.HasValue).OrderBy(o => o.Sort).Select(o => o.CommandId.Value))
        //                {
        //                    toReturn.Object.FormData.TransactionCommandId = commandId;

        //                    var aChlidCommandResult = ExcuteTransactionCommonad(toReturn.Object.FormData, true);
        //                    if (aChlidCommandResult != null && aChlidCommandResult.Object != null)
        //                    {
        //                        if (aChlidCommandResult.IsSuccessfulWithResult)
        //                        {
        //                            toReturn.Object.ChildCommandResultDtoList.Add(aChlidCommandResult.Object);
        //                            if (aChlidCommandResult.Object.FormData != null)
        //                            {
        //                                toReturn.Object.FormData = aChlidCommandResult.Object.FormData;
        //                            }
        //                        }
        //                        else
        //                        {
        //                            toReturn.ValidationResult.Merge(aChlidCommandResult.ValidationResult);
        //                        }
        //                    }
        //                }
        //            }
        //        }

        //        return toReturn;
        //    }

        //    return toReturn;
        //}

        public static bool CheckCommandCondition(AppMasterDetailDto aformData, AppTransactionExDto transactionExDto, AppProjectWorkFlowActionExDto commandDto)
        {
            bool isConditionAllowExecute = false;

            if (commandDto.CommandConditionTransactionFieldId.HasValue)
            {
                int conditionId = commandDto.CommandConditionTransactionFieldId.Value;
                var dictRootFiedValie = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(aformData, transactionExDto); ;
                if (!dictRootFiedValie.IsEmpty() && dictRootFiedValie.ContainsKey(conditionId))
                {
                    bool? conditionResult = ControlTypeValueConverter.ConvertValueToBoolean(dictRootFiedValie[conditionId]);

                    if (conditionResult.HasValue && conditionResult.Value)
                    {
                        isConditionAllowExecute = true;
                    }
                }
            }
            else
            {
                isConditionAllowExecute = true;
            }

            return isConditionAllowExecute;
        }

        public static void ExecuteCommand_Save(OperationCallResult<TransactionCommandActionResultDto> toReturn)
        {
            toReturn.Object.FormData.IsDirty = true;
            //AppMasterDetailFormDataSaveBL.ConvertMasterDetailFormDateTime_FromClientToUTC(toReturn.Object.FormData);
            var postSaveResult = AppMasterDetailFormDataSaveBL.SaveTransactionData(toReturn.Object.FormData);

            if (postSaveResult != null)
            {
                if (postSaveResult.IsSuccessfulWithResult)
                {
                    toReturn.Object.FormData = postSaveResult.Object;
                }
                else if (postSaveResult.ValidationResult.HasErrors)
                {
                    toReturn.ValidationResult.Merge(postSaveResult.ValidationResult);
                }
            }
        }

        public static void ExecuteCommand_Refresh(OperationCallResult<TransactionCommandActionResultDto> toReturn)
        {
            var refreshedFormData = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(toReturn.Object.FormData.TransactionId, toReturn.Object.FormData.RootPrimaryKeyValue);
            toReturn.Object.FormData = refreshedFormData;
        }

        public static void ExecuteCommand_OpenFormCreationWindow(AppProjectWorkFlowActionExDto actionDto, OperationCallResult<TransactionCommandActionResultDto> toReturn)
        {
            toReturn.Object.CommandActionExDto = actionDto;

            if (actionDto.DataTransferSettingId.HasValue)
            {
                AppTransactionDataTransferSettingExDto dataTransferDto = AppTransactionDataTransferSettingBL.RetrieveOneAppTransactionDataTransferSettingExDto(actionDto.DataTransferSettingId.Value);

                if (dataTransferDto != null)
                {
                    toReturn.Object.TransactionId = dataTransferDto.DestinationTransactionId;
                    toReturn.Object.TransactionRId = toReturn.Object.FormData.RootPrimaryKeyValue;
                }

            }
        }

        public static void ExecuteCommand_SaveAs(AppProjectWorkFlowActionExDto actionDto, OperationCallResult<TransactionCommandActionResultDto> toReturn)
        {
            OperationCallResult<AppMasterDetailDto> saveAsResult = AppMasterDetailFormDataSaveBL.SaveAsMasterDetailTransactionData(toReturn.Object.FormData.TransactionId, toReturn.Object.FormData.RootPrimaryKeyValue);

            if (saveAsResult.IsSuccessfulWithResult)
            {
                if (actionDto.ActionAttribute != null)
                {
                    toReturn.Object.IsNeedToOpenTransactionForm = true; // actionDto.ActionAttribute.IsNeedToOpenNewForm;
                }

                toReturn.Object.FormTitleDisplay = saveAsResult.Object.FormTitleDisplay;
                toReturn.Object.TransactionId = saveAsResult.Object.TransactionId;
                toReturn.Object.TransactionRId = saveAsResult.Object.RootPrimaryKeyValue;
            }
            else
            {
                toReturn.ValidationResult.Merge(saveAsResult.ValidationResult);
            }
        }

        public static void ExecuteCommand_GenerateMatrix(AppProjectWorkFlowActionExDto actionDto, OperationCallResult<TransactionCommandActionResultDto> toReturn)
        {
            int? matrixUnitId = null;
            if (actionDto.ActionAttribute != null)
            {
                matrixUnitId = actionDto.ActionAttribute.ParamId1;
            }

            AppTransactionFormulaBL.GenerateMatrix(toReturn.Object.FormData, matrixUnitId);
            toReturn.Object.FormData.IsDirty = true;
        }

        public static void ExecuteCommand_ExecuteAllDataLoad(OperationCallResult<TransactionCommandActionResultDto> toReturn)
        {
            AppTransactionTemplateDataLoadBL.LoadTransactionTemplateData(toReturn.Object.FormData);
            toReturn.Object.FormData.IsDirty = true;
        }

        public static void ExecuteCommand_Calculation(OperationCallResult<TransactionCommandActionResultDto> toReturn)
        {
            var calculationResult = AppTransactionFormulaBL.ValidateAndCalculateMasterDetailTransactionData(toReturn.Object.FormData);

            if (calculationResult.IsSuccessfulWithResult)
            {
                toReturn.Object.FormData = calculationResult.Object;
                toReturn.Object.FormData.IsDirty = true;
                if (calculationResult.ValidationResult.HasWarnings)
                {
                    toReturn.ValidationResult.Merge(calculationResult.ValidationResult);
                }
            }
            else
            {
                toReturn.ValidationResult.Merge(calculationResult.ValidationResult);
            }
        }

        public static void ExecuteCommand_DataTransfer(AppProjectWorkFlowActionExDto actionDto, OperationCallResult<TransactionCommandActionResultDto> toReturn)
        {
            OperationCallResult<AppMasterDetailDto> dataTransferResult = new OperationCallResult<AppMasterDetailDto>();
            ValidationResult aValidationResult = new ValidationResult();
            dataTransferResult.ValidationResult = aValidationResult;

            var tgtFormData = PrepareNewFormDataFromDataTransfer(toReturn.Object.FormData, actionDto.DataTransferSettingId);

            if (tgtFormData != null)
            {
                dataTransferResult = AppMasterDetailFormDataSaveBL.SaveTransactionData(tgtFormData);
            }

            if (dataTransferResult.IsSuccessfulWithResult)
            {
                if (actionDto.ActionAttribute != null)
                {
                    toReturn.Object.IsNeedToOpenTransactionForm = actionDto.ActionAttribute.IsNeedToOpenNewForm;
                }


                toReturn.Object.FormTitleDisplay = dataTransferResult.Object.FormTitleDisplay;
                toReturn.Object.TransactionId = dataTransferResult.Object.TransactionId;
                toReturn.Object.TransactionRId = dataTransferResult.Object.RootPrimaryKeyValue;
            }
            else
            {
                toReturn.ValidationResult.Merge(dataTransferResult.ValidationResult);
            }
        }

        public static void ExecuteCommand_PluginWebApiCall(AppProjectWorkFlowActionExDto actionDto, OperationCallResult<TransactionCommandActionResultDto> toReturn)
        {
            var aAppTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(toReturn.Object.FormData.TransactionId);

            if (actionDto.ActionAttribute != null && !string.IsNullOrWhiteSpace(actionDto.ActionAttribute.WebApiMethodName))
            {

                string restResourceUri = actionDto.ActionAttribute.WebApiMethodName.Trim();

                OperationCallResult<AppMasterDetailDto> result = AppPluginClient.CallTransactionFormExternalService(toReturn.Object.FormData, restResourceUri);

                if (result != null)
                {
                    toReturn.ValidationResult.Merge(result.ValidationResult);

                    if (result.ValidationResult.HasErrors)
                    {
                        //toReturn.ValidationResult.Merge(result.ValidationResult);
                    }
                    else
                    {
                        toReturn.Object.FormData = result.Object;
                    }
                }
            }
        }

        public static AppMasterDetailDto PrepareNewFormDataFromDataTransfer(AppMasterDetailDto srcFormData, int? dataTransferSettingId)
        {
            if (dataTransferSettingId.HasValue)
            {
                AppTransactionDataTransferSettingExDto dataTransferDto = AppTransactionDataTransferSettingBL.RetrieveOneAppTransactionDataTransferSettingExDto(dataTransferSettingId.Value);

                if (dataTransferDto != null
                    && dataTransferDto.DestinationTransactionId.HasValue && dataTransferDto.TransactionId == srcFormData.TransactionId
                    && dataTransferDto.AppTransactionSaveAsMappingList != null)
                {
                    AppMasterDetailDto tgtFormBlankData = AppMasterDetailFormDataLoadBL.GetNewFormData(dataTransferDto.DestinationTransactionId.Value);
                    AppMasterDetailDto tgtFormData = AppMasterDetailFormDataLoadBL.GetNewFormData(dataTransferDto.DestinationTransactionId.Value);

                    AppTransactionExDto srcTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(dataTransferDto.TransactionId);
                    AppTransactionExDto tgtTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(dataTransferDto.DestinationTransactionId.Value);

                    tgtFormData.DictCascadingFiledDataSource = new Dictionary<string, List<LookupItemDto>>();

                    tgtFormData.EditCloneDictOneToManyFields = tgtFormBlankData.DictOneToManyFields;



                    List<AppTransactionSaveAsMappingExDto> mappingList = dataTransferDto.AppTransactionSaveAsMappingList.ToList();

                    Dictionary<int, List<AppTransactionSaveAsMappingExDto>> dictUnitSaveAsMapping = mappingList.GroupBy(o => o.MappingUnitId).ToDictionary(o => o.Key.Value, o => o.ToList());

                    Dictionary<int, int?> dictSrcUnitIdAndTgtUnitId = new Dictionary<int, int?>();






                    var srcRootMasterUnit = srcTransactionExDto.RootMasterUnit;
                    var tgtRootMasterUnit = tgtTransactionExDto.RootMasterUnit;

                    Dictionary<string, object> srcRootOneToOneFields = srcFormData.DictOneToOneFields;
                    Dictionary<string, object> tgtRootOneToOneFields = tgtFormData.DictOneToOneFields;







                    TransferOneUnitOneRow(dictUnitSaveAsMapping, srcRootMasterUnit, tgtRootMasterUnit, srcRootOneToOneFields, tgtRootOneToOneFields);

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

                                                        TransferOneUnitOneRow(dictUnitSaveAsMapping, srcGradnchildUnit, tgtGrandChildUnit, srcGradnOneToOneFields, aTgtGrandChildDataDto.DictOneToOneFields);
                                                    }
                                                }
                                            }
                                        }
                                    } // end of grand child
                                } // end of child

                            }

                        }

                    }

                    AppCascadingBL.SetupIntialCscadingFieldDataSource(tgtTransactionExDto, tgtFormData, false);
                    AppCascadingBL.SetupIntialAutoCompleteFieldDataSource(tgtTransactionExDto, tgtFormData, false);

                    tgtFormData.IsNew = true;

                    return tgtFormData;
                }
            }

            return null;
        }



        private static void TransferOneUnitOneRow(Dictionary<int, List<AppTransactionSaveAsMappingExDto>> dictUnitMapping,
            AppTransactionUnitExDto srcRootMasterUnit, AppTransactionUnitExDto tgtRootMasterUnit,
            Dictionary<string, object> srcRootOneToOneFields, Dictionary<string, object> tgtRootOneToOneFields)
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
                }
            }
        }







        // Input: Client DateTime
        // Output: Client DateTime
        private static OperationCallResult<TransactionCommandActionResultDto> ExecuteCompositionCommand(Dictionary<int, object> dictOneRowFiedIdValue, AppProjectWorkFlowActionExDto actionDto, AppMasterDetailDto aformData, int? chlldUnitId = null, string childRowPkCombString = null)
        {
            OperationCallResult<TransactionCommandActionResultDto> toReturn = new OperationCallResult<TransactionCommandActionResultDto>();
            toReturn.ValidationResult = new ValidationResult();


            toReturn.Object = new TransactionCommandActionResultDto();
            toReturn.Object.CommandId = ControlTypeValueConverter.ConvertValueToInt(actionDto.Id);
            toReturn.Object.ActionTypeId = actionDto.ActionType;
            toReturn.Object.FormData = aformData;


            if (actionDto.ActionAttribute != null && actionDto.ActionAttribute.ChildActionList != null)
            {
                toReturn.Object.ChildCommandResultDtoList = new List<TransactionCommandActionResultDto>();

                foreach (int commandId in actionDto.ActionAttribute.ChildActionList.Where(o => o.CommandId.HasValue).OrderBy(o => o.Sort).Select(o => o.CommandId.Value))
                {
                    toReturn.Object.FormData.TransactionCommandId = commandId;

                    var aChlidCommandResult = ExcuteOneTransactionCommonad(toReturn.Object.FormData, chlldUnitId, childRowPkCombString);
                    if (aChlidCommandResult != null && aChlidCommandResult.Object != null)
                    {
                        toReturn.ValidationResult.Merge(aChlidCommandResult.ValidationResult);

                        if (aChlidCommandResult.IsSuccessfulWithResult)
                        {
                            toReturn.Object.ChildCommandResultDtoList.Add(aChlidCommandResult.Object);
                            if (aChlidCommandResult.Object.FormData != null)
                            {
                                toReturn.Object.FormData = aChlidCommandResult.Object.FormData;
                            }
                        }
                        else
                        {
                            //toReturn.ValidationResult.Merge(aChlidCommandResult.ValidationResult);
                            break;
                        }
                    }
                }
            }

            return toReturn;
        }


        private static void ExecuteCommand_SendMessageToTransFieldEmailAddress(AppMasterDetailDto appformDataDto, AppProjectWorkFlowActionExDto action)
        {

            if (!string.IsNullOrWhiteSpace(action.NotificationSubject)
                && !string.IsNullOrWhiteSpace(action.NotificationMessage)
                && action.ActionAttribute.NotificationDestinationEmailAddressTransactionFiledId.HasValue)
            {
                //List<int> userIdList = new List<int>();

                //if (action.NotificationDestinationUserIdtransactionFiledId.HasValue)
                //{
                //    int? toUserId = null;
                //    int userIdFieldId = action.NotificationDestinationUserIdtransactionFiledId.Value;

                //    if (allFreshRootValue.ContainsKey(userIdFieldId))
                //    {
                //        toUserId = ControlTypeValueConverter.ConvertValueToInt(allFreshRootValue[userIdFieldId]);
                //    }
                //    else
                //    {
                //        if (childRowData != null && aAppTransactionExDto != null && childRowData.ChildUnitId != null)
                //        {
                //            var unitDto = aAppTransactionExDto.DictAllTransactionUnitIdExDto.Values.FirstOrDefault(o => o.Id.ToString() == childRowData.ChildUnitId.ToString());
                //            if (unitDto != null)
                //            {
                //                var filedDto = unitDto.DicFieldIdFieldExdto.Values.FirstOrDefault(o => o.Id.ToString() == userIdFieldId.ToString());
                //                if (filedDto != null)
                //                {
                //                    if (childRowData.DictOneToOneFields.ContainsKey(filedDto.DataBaseFieldName) && childRowData.DictOneToOneFields[filedDto.DataBaseFieldName] != null)
                //                    {
                //                        toUserId = ControlTypeValueConverter.ConvertValueToInt(childRowData.DictOneToOneFields[filedDto.DataBaseFieldName]);
                //                    }
                //                }
                //            }
                //        }
                //    }

                //    if (toUserId.HasValue)
                //    {
                //        userIdList.Add(toUserId.Value);
                //    }
                //}

                //if (action.NotificationDestinationRoleIdtransactionFiledId.HasValue)
                //{
                //    int? toRoleId = null;
                //    int roleIdFieldId = action.NotificationDestinationRoleIdtransactionFiledId.Value;

                //    if (allFreshRootValue.ContainsKey(roleIdFieldId))
                //    {
                //        toRoleId = ControlTypeValueConverter.ConvertValueToInt(allFreshRootValue[roleIdFieldId]);
                //    }
                //    else
                //    {
                //        if (childRowData != null && aAppTransactionExDto != null && childRowData.ChildUnitId != null)
                //        {
                //            var unitDto = aAppTransactionExDto.DictAllTransactionUnitIdExDto.Values.FirstOrDefault(o => o.Id.ToString() == childRowData.ChildUnitId.ToString());
                //            if (unitDto != null)
                //            {
                //                var filedDto = unitDto.DicFieldIdFieldExdto.Values.FirstOrDefault(o => o.Id.ToString() == roleIdFieldId.ToString());
                //                if (filedDto != null)
                //                {
                //                    if (childRowData.DictOneToOneFields.ContainsKey(filedDto.DataBaseFieldName) && childRowData.DictOneToOneFields[filedDto.DataBaseFieldName] != null)
                //                    {
                //                        toRoleId = ControlTypeValueConverter.ConvertValueToInt(childRowData.DictOneToOneFields[filedDto.DataBaseFieldName]);
                //                    }
                //                }
                //            }
                //        }
                //    }

                //    if (toRoleId.HasValue)
                //    {
                //        userIdList.AddRange(AppSecurityGroupBL.GetUserIdsByGroupIds(new List<int> { toRoleId.Value }));
                //    }
                //}

                //userIdList = userIdList.Distinct().ToList();

                //if (userIdList.Count > 0)
                //{

                //    string messageTempalte = action.NotificationMessage;

                //    string currentDatetime = DateTime.UtcNow.ToShortDateString() + " " + DateTime.UtcNow.ToShortTimeString();
                //    string currentUserName = AppSecurityUserBL.CurrentUserEntity.UserName;
                //    string workflowName = string.Empty;
                //    string taskName = string.Empty;
                //    string taskStatus = string.Empty;


                //    string subject = ReplaceMessageTemplatePlaceHolderWithActualValue(allFreshRootValue, action.NotificationSubject, currentDatetime, currentUserName, workflowName, taskName, taskStatus, aAppTransactionExDto, childRowData);
                //    string messageBody = ReplaceMessageTemplatePlaceHolderWithActualValue(allFreshRootValue, action.NotificationMessage, currentDatetime, currentUserName, workflowName, taskName, taskStatus, aAppTransactionExDto, childRowData);

                //    var messageDto = new AppMessageDto();
                //    messageDto.ToUserIdList = userIdList;
                //    //messageDto.ToUserIdList.Add(AppSecurityUserBL.CurrentUserEntity.UserId);

                //    messageDto.MessgaeScopeType = (int)EmAppMessgaeScopeType.Task;

                //    //messageDto.TransactionId = ControlTypeValueConverter.ConvertValueToInt(aAppTransactionExDto.Id);
                //    //messageDto.TransactionRootValueId = rootKeyValue != null ? rootKeyValue.ToString() : string.Empty;

                //    messageDto.Subject = subject;
                //    messageDto.Message = messageBody;



                //    messageList.Add(messageDto);
                //}
            }
        }

        private static void ExecuteCommand_SendMessageToTransFieldPartnerId(AppMasterDetailDto appformDataDto, AppProjectWorkFlowActionExDto action)
        {
            List<AppMessageDto> messageList = new List<AppMessageDto>();

            if (appformDataDto != null
                && (
                    !string.IsNullOrWhiteSpace(action.NotificationSubject) && !string.IsNullOrWhiteSpace(action.NotificationMessage) 
                    || action.MessageTemplateId.HasValue
                )
                && action.ActionAttribute.NotificationDestinationPartnerIdTransactionFiledId.HasValue)
            {

                AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appformDataDto.TransactionId);
                int partnerIdFieldId = action.ActionAttribute.NotificationDestinationPartnerIdTransactionFiledId.Value;

                if (appTransactionExDto.DictAllTransactionField.ContainsKey(partnerIdFieldId))
                {

                    var transfieldDto = appTransactionExDto.DictAllTransactionField[partnerIdFieldId];
                    int unitId = transfieldDto.TransactionUnitId;

                    if (appformDataDto.RootUnitId.ToString() == unitId.ToString())
                    {
                        AppChildDataDto childRowData = null;

                        int? partnerId = ControlTypeValueConverter.ConvertValueToInt(appformDataDto.DictOneToOneFields[transfieldDto.DataBaseFieldName]);
                        if (partnerId.HasValue)
                        {
                            PrepareOnePartnerFieldMessage(appformDataDto, action, messageList, appTransactionExDto, childRowData, partnerId);
                        }
                    }
                    else if (appformDataDto.DictSiblingOneToOneFields.ContainsKey(unitId.ToString()))
                    {
                        AppChildDataDto childRowData = null;

                        int? partnerId = ControlTypeValueConverter.ConvertValueToInt(appformDataDto.DictSiblingOneToOneFields[unitId.ToString()][transfieldDto.DataBaseFieldName]);
                        if (partnerId.HasValue)
                        {
                            PrepareOnePartnerFieldMessage(appformDataDto, action, messageList, appTransactionExDto, childRowData, partnerId);
                        }
                    }
                    else if (appformDataDto.DictOneToManyFields.ContainsKey(unitId.ToString()))
                    {
                        var unitDto = appTransactionExDto.DictAllTransactionUnitIdExDto[unitId.ToString()];

                        AppProjectWorkFlowDataFormSynchBL.SetChildRowPKValueCombinString(unitDto, appformDataDto.DictOneToManyFields[unitId.ToString()]);

                        foreach (AppChildDataDto childRowData in appformDataDto.DictOneToManyFields[unitId.ToString()])
                        {
                            int? partnerId = ControlTypeValueConverter.ConvertValueToInt(childRowData.DictOneToOneFields[transfieldDto.DataBaseFieldName]);
                            if (partnerId.HasValue)
                            {
                                PrepareOnePartnerFieldMessage(appformDataDto, action, messageList, appTransactionExDto, childRowData, partnerId);
                            }
                        }
                    }
                }
            }

            SendCommandMessages(messageList);
        }


        private static void PrepareOnePartnerFieldMessage(AppMasterDetailDto appformDataDto, AppProjectWorkFlowActionExDto action, List<AppMessageDto> messageList, AppTransactionExDto appTransactionExDto, AppChildDataDto childRowData, int? partnerId)
        {
            List<AppSecurityUserDto> partnerUserList = AppBusinessPartnerBL.RetrieveCurrentCompanyOneBusinessPartnerAllInviteUserDtoList(partnerId.Value);
            List<int> userIdList = partnerUserList.Select(o => (int)o.Id).Distinct().ToList();


            if (userIdList.Count > 0)
            {
                string messageTempalte = action.NotificationMessage;

                string currentDatetime = DateTime.UtcNow.ToShortDateString() + " " + DateTime.UtcNow.ToShortTimeString();
                string currentUserName = AppSecurityUserBL.CurrentUserEntity.UserName;
                string workflowName = string.Empty;
                string taskName = string.Empty;
                string taskStatus = string.Empty;

                Dictionary<int, object> allFreshRootValue = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(appformDataDto, appTransactionExDto);

                if (!(action.ActionAttribute != null && action.ActionAttribute.IsUseRichTextMessageTemplate))
                {
                    action.MessageTemplateId = null;
                }

                string subject = AppProjectWorkFlowDataFormSynchBL.ReplaceMessageTemplatePlaceHolderWithActualValue(null, allFreshRootValue, action.NotificationSubject, currentDatetime, currentUserName, workflowName, taskName, taskStatus, appTransactionExDto, childRowData, action.MessageTemplateId, true);
                string messageBody = AppProjectWorkFlowDataFormSynchBL.ReplaceMessageTemplatePlaceHolderWithActualValue(null, allFreshRootValue, action.NotificationMessage, currentDatetime, currentUserName, workflowName, taskName, taskStatus, appTransactionExDto, childRowData, action.MessageTemplateId, false);

                if (action.ActionAttribute.ChildActionList != null && action.ActionAttribute.ChildActionList.Count > 0)
                {
                    foreach (ChildTransactionCommandDto childCommand in action.ActionAttribute.ChildActionList)
                    {
                        if (childCommand.CommandId.HasValue)
                        {
                            var childCommandDto = appTransactionExDto.CommandActionList.FirstOrDefault(o => (int)o.Id == childCommand.CommandId.Value);
                            if (childCommandDto != null)
                            {
                                string paramStr = EmAppGlobalServiceAction.ExecuteDataModelInternalCommand.ToString()
                                            + "|" + ServerContext.Instance.CurrentCompanyId.ToString()
                                            + "|" + childCommandDto.Id
                                            + "|" + appformDataDto.TransactionId
                                            + "|" + appformDataDto.RootPrimaryKeyValue;

                                if (childRowData != null)
                                {
                                    if (!string.IsNullOrWhiteSpace(childRowData.PKValueCombinString))
                                    {
                                        paramStr += "|" + childRowData.ChildUnitId.ToString();
                                        paramStr += "|" + childRowData.PKValueCombinString;
                                    }
                                    //var unitDto = appTransactionExDto.DictAllTransactionUnitIdExDto[childRowData.ChildUnitId.ToString()];
                                    // if (unitDto != null)
                                    // {                                    
                                    //     foreach (string pk in unitDto.PrimaryKeyDbfieldList)
                                    //     {
                                    //         var pkValue = childRowData.DictOneToOneFields[pk];
                                    //         if (pkValue != null)
                                    //         {
                                    //             paramStr += "|" + pkValue.ToString();
                                    //         }
                                    //     }                                        
                                    // }                                    
                                }

                                string paramStr_encrypted = AppSaasAccountUserBL.EncryptParamString(paramStr);
                                string applicationUrl = AppSystemSettingBL.GetStringValue(EmSystemSettings.ApplicationURL);
                                string url = applicationUrl + "/Home/As/?servicename=" + paramStr_encrypted;

                                messageBody += "<div><a target='_blank' href='" + url + "' style=''>" + childCommandDto.Name + "</a></div>";
                            }
                        }
                    }
                }



                var messageDto = new AppMessageDto();
                messageDto.ToUserIdList = userIdList;

                messageDto.MessgaeScopeType = (int)EmAppMessgaeScopeType.Transaction;
                messageDto.TransactionId = appformDataDto.TransactionId;
                messageDto.TransactionRootValueId = appformDataDto.RootPrimaryKeyValue.ToString();

                messageDto.Subject = subject;
                messageDto.Message = messageBody;

                if (action.ActionAttribute.IsAttachAllFormFilesToMessage)
                {
                    messageDto.IsAttachAllFormFiles = true;
                }


                messageList.Add(messageDto);
            }
        }


        // Input: Client DateTime
        // Output: Client DateTime
        private static OperationCallResult<TransactionCommandActionResultDto> ExecuteSingleCommandByActionType(Dictionary<int, object> dictOneRowFiedIdValue, AppProjectWorkFlowActionExDto actionDto, AppMasterDetailDto aformData, Dictionary<int, object> dictChildRowFiedIdValue = null)
        {
            OperationCallResult<TransactionCommandActionResultDto> toReturn = new OperationCallResult<TransactionCommandActionResultDto>();
            toReturn.ValidationResult = new ValidationResult();

            if (actionDto != null && actionDto.ActionType.HasValue)
            {
                toReturn.Object = new TransactionCommandActionResultDto();
                toReturn.Object.CommandId = ControlTypeValueConverter.ConvertValueToInt(actionDto.Id);
                toReturn.Object.ActionTypeId = actionDto.ActionType;
                toReturn.Object.FormData = aformData;

                if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.ExecuteSQLStatement)
                {
                    DataModelDateTimeConverterBL.ConvertMasterDetailFromClientToUtc(toReturn.Object.FormData);
                    AppProjectWorkFlowDataFormSynchBL.ExcusteSqlStatmentActionWithRootValue(dictOneRowFiedIdValue, actionDto, toReturn);
                    DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(toReturn.Object.FormData);
                }
                else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.PluginWebApiCall)
                {
                    // To Do, Convert Time at Plugin API 
                    AppTransactionCommandBL.ExecuteCommand_PluginWebApiCall(actionDto, toReturn);
                }
                else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.ExecuteFormDataTransfer)
                {
                    DataModelDateTimeConverterBL.ConvertMasterDetailFromClientToUtc(toReturn.Object.FormData);
                    ExecuteCommand_DataTransfer(actionDto, toReturn);
                    DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(toReturn.Object.FormData);
                }
                else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.CommnadFormulaCalculation)
                {
                    ExecuteCommand_Calculation(toReturn);
                }
                else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.ExecuteLoadDataSetToTranscation)
                {
                    AppTransactionCommandBL.ExecuteCommand_ExecuteAllDataLoad(toReturn);
                }
                else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.GenerateMatrix)
                {
                    AppTransactionCommandBL.ExecuteCommand_GenerateMatrix(actionDto, toReturn);

                }
                else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.SaveAs)
                {
                    AppTransactionCommandBL.ExecuteCommand_SaveAs(actionDto, toReturn);
                    DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(toReturn.Object.FormData);
                }
                else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.OpenFormCreationWindow)
                {
                    AppTransactionCommandBL.ExecuteCommand_OpenFormCreationWindow(actionDto, toReturn);
                }
                else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.refresh)
                {
                    AppTransactionCommandBL.ExecuteCommand_Refresh(toReturn);
                    DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(toReturn.Object.FormData);
                }
                else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.Save)
                {
                    DataModelDateTimeConverterBL.ConvertMasterDetailFromClientToUtc(toReturn.Object.FormData);
                    AppTransactionCommandBL.ExecuteCommand_Save(toReturn);
                    DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(toReturn.Object.FormData);
                }
                //else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.SendMessageToTransFieldEmailAddress)
                //{
                //    AppTransactionCommandBL.ExecuteCommand_SendMessageToTransFieldEmailAddress(toReturn.Object.FormData, actionDto);                    
                //}
                //else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.SendMessageToTransFieldPartnerId)
                //{
                //    AppTransactionCommandBL.ExecuteCommand_SendMessageToTransFieldPartnerId(toReturn.Object.FormData, actionDto);
                //}



                return toReturn;
            }

            return toReturn;
        }

        private static Dictionary<int, object> PrepareChildUnitRowDictFieldIdAndValue(AppMasterDetailDto aformData, int? chlldUnitId, string childRowPkCombString, AppTransactionExDto transactionExDto)
        {
            Dictionary<int, object> dictChildRowFiedIdValue = null;

            if (chlldUnitId.HasValue && !string.IsNullOrWhiteSpace(childRowPkCombString)
                && aformData.DictOneToManyFields.ContainsKey(chlldUnitId.Value.ToString()))
            {
                var childUnitDto = transactionExDto.DictAllTransactionUnitIdExDto[chlldUnitId.Value.ToString()];
                AppProjectWorkFlowDataFormSynchBL.SetChildRowPKValueCombinString(childUnitDto, aformData.DictOneToManyFields[chlldUnitId.Value.ToString()]);
                var rowList = aformData.DictOneToManyFields[chlldUnitId.Value.ToString()];
                var rowDto = rowList.FirstOrDefault(o => o.PKValueCombinString == childRowPkCombString);
                if (rowDto != null)
                {
                    dictChildRowFiedIdValue = AppTransactionFormulaBL.ConvertUnitOneRowDbFileNameToFileId(childUnitDto, rowDto.DictOneToOneFields);
                }
            }

            return dictChildRowFiedIdValue;
        }

        private static void SendCommandMessages(List<AppMessageDto> messageList)
        {
            foreach (AppMessageDto message in messageList)
            {
                List<int> mandatoryUserIdList = message.ToUserIdList;

                string subject = message.Subject;
                string messageText = message.Message;
                int? taskId = null;
                int? workflowId = null;
                int? transactionId = message.TransactionId;
                string transactionRId = message.TransactionRootValueId;

                EmAppMessgaeScopeType messageScope = EmAppMessgaeScopeType.Transaction;

                AppMessageBL.SendNewAppMessageByScope(messageScope,
                    transactionId,
                    transactionRId,
                    taskId,
                    workflowId,
                    null,
                    EmAppMessgaePostType.SystemNotification,
                    subject,
                    messageText,
                    mandatoryUserIdList);
            }
        }

    }
}