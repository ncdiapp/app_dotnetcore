using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
//using APP.Persistence.Common;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using System.Data.SqlClient;
using ExchangeBL;
using System.Text;

#if NETFRAMEWORK
using Microsoft.Exchange.WebServices.Data;
// TODO-PHASE4: Replace with .NET 10 equivalent
#endif
using Google.Protobuf.WellKnownTypes;
using DatabaseSchemaMrg;
using System.Data.Common;
using System.ComponentModel.Design;
using AngleSharp.Dom;

using DatabaseSchemaMrg.DataSchema;
using System.IO;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using System.Threading;
using System.CodeDom.Compiler;
using System.Net;
using System.Runtime.Serialization;
#if NETFRAMEWORK
using SpreadsheetGear.Charts;
// TODO-PHASE4: Replace with .NET 10 equivalent
#endif
using System.Xml;


using APP.Framework;
namespace App.BL
{
    public static class AppTransactionCommandBL
    {
        // Input: Client DateTime
        // Output: Client DateTime

        public static readonly string GlobalTFPrefix = "[GlobalTF_";



        public static OperationCallResult<TransactionCommandActionResultDto> ExecuteTransactionRootCommand(AppMasterDetailDto aformData, int? chlldUnitId = null, string childRowPkCombString = null)
        {
            OperationCallResult<TransactionCommandActionResultDto> aOperationCallResult = new OperationCallResult<TransactionCommandActionResultDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            try
            {
                if (aformData.TransactionCommandId.HasValue)
                {
                    string transactionRId = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(aformData.RootPrimaryKeyValue);

                    AppProjectWorkFlowActionExDto commandDto = RetrieveOneTransactionCommandExDto(aformData.TransactionCommandId.Value);
                    AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aformData.TransactionId);

                    if (commandDto != null && transactionExDto != null)
                    {
                        commandDto.RootTransactionFormData = aformData;

                        bool isAllowExecuteByCondition = AppTransactionCommandBL.CheckCommandCondition(aformData, transactionExDto, commandDto, null);

                        if (isAllowExecuteByCondition)
                        {
                            if (commandDto.ActionAttribute.IsBatchCommand
                                && !(commandDto.ActionAttribute.BatchCommandSourceFromType.HasValue && commandDto.ActionAttribute.BatchCommandSourceFromType.Value == (int)EmAppBatchCommandSourceFrom.ChildUnit))
                            {
                                var batchCommandValidationResult = ExecuteBatchCommand_LoopOnDataSetOrSearch(aformData, new ValidationResult(), commandDto, commandDto);
                                batchCommandValidationResult.Items.ForAll(o => o.IsChildCommandValidationItem = true);
                                aOperationCallResult.ValidationResult.Merge(batchCommandValidationResult);
                            }
                            else
                            {
                                if (commandDto.ActionType.HasValue)
                                {
                                    if (commandDto.ActionType.Value == (int)EmAppTransactionCommandType.CompositionCommand)
                                    {
                                        aOperationCallResult = ExecuteCompositionCommand(commandDto, aformData, chlldUnitId, childRowPkCombString, commandDto);
                                    }
                                    else
                                    {
                                        aOperationCallResult = ExecuteSingleCommandByActionType(commandDto, aformData, transactionExDto, commandDto);
                                    }
                                }
                            }
                        }
                        else
                        {
                            aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Condition pass failure", ValidationItemType.Error, "Command execute condition is not ready."));
                        }

                        RebuildToSimpleValidationResultBySetting(aformData, aOperationCallResult.ValidationResult, commandDto, transactionExDto, transactionRId);
                    }
                    else
                    {
                        aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Command_Error", ValidationItemType.Error, "Cannot Load Command with Id " + aformData.TransactionCommandId.Value));
                    }
                }
                else
                {
                    aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Command_Error", ValidationItemType.Error, "Empty Command"));
                }
            }
            catch (Exception ex)
            {
                aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Command_Error", ValidationItemType.Error, "Command Error: " + ex.ToString()));
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<TransactionCommandActionResultDto> ExecuteWorkflowRootCommand(AppMasterDetailDto aformData)
        {
            OperationCallResult<TransactionCommandActionResultDto> aOperationCallResult = new OperationCallResult<TransactionCommandActionResultDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            //aformData.DebugWorkflowRootChildCommandId = 216806;

            //if (aformData.DebugWorkflowRootChildCommandId.HasValue)
            //{
            //    aformData.RootPrimaryKeyValue = "Debug_" + aformData.DebugWorkflowRootChildCommandId.Value + "_" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");                
            //}

            string transactionRId = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(aformData.RootPrimaryKeyValue);
            bool isAlreadyExecuted = CheckIsWorkflowInstanceExist(aformData.TransactionId, transactionRId);

            if (isAlreadyExecuted)
            {
                aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Command_Error", ValidationItemType.Error, "This workflow has already been executed."));
            }
            else
            {
                AppProjectWorkFlowActionExDto rootCommandDto = null;
                var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aformData.TransactionId);

                if (aformData.TransactionCommandId.HasValue)
                {

                    rootCommandDto = RetrieveOneTransactionCommandExDto(aformData.TransactionCommandId.Value);

                    if (rootCommandDto != null && transactionExDto != null)
                    {
                        if (aformData.DebugWorkflowRootChildCommandId.HasValue)
                        {
                            PrepareWorkflowDebugTestData(aformData, rootCommandDto);
                        }

                        int? workflowAutomationId = ControlTypeValueConverter.ConvertValueToInt(aformData.RootPrimaryKeyValue);


                        try
                        {
                            rootCommandDto.RootTransactionFormData = aformData;

                            rootCommandDto.IsWorkflowAutomationRootCommand = true;

                            SyncronizeWorkflowCommandNodeTreeFromActionList(transactionExDto);

                            rootCommandDto.WorkflowCommandNodeTree = transactionExDto.WorkflowCommandNodeTree;

                            rootCommandDto.ProgressStatus = EmAppWorkflowProgressStatus.Started.ToString();

                            InsertOneAppBatchLogToDb(rootCommandDto, aformData.TransactionId, transactionRId);




                            aOperationCallResult = ExecuteCompositionCommand(rootCommandDto, aformData, null, null, rootCommandDto);

                            RebuildToSimpleValidationResultBySetting(aformData, aOperationCallResult.ValidationResult, rootCommandDto, transactionExDto, transactionRId);

                            SetAppBatchLogEndTime(rootCommandDto.BatchNumber);

                            if (aOperationCallResult.ValidationResult.HasErrors)
                            {
                                rootCommandDto.ProgressStatus = EmAppWorkflowProgressStatus.StoppedWithError.ToString();
                            }
                            else
                            {
                                rootCommandDto.ProgressStatus = EmAppWorkflowProgressStatus.Completed.ToString();
                            }

                            SetAppBatchLogProgressStatus(rootCommandDto.BatchNumber, rootCommandDto.ProgressStatus, rootCommandDto.ProgressStatus.ToString(), workflowAutomationId);

                        }
                        catch (Exception ex)
                        {


                            if (ex is IntegrationPauseException)
                            {
                                aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Command_Error", ValidationItemType.Error, "Force Aborted By User."));

                                SetAppBatchLogProgressStatus(rootCommandDto.BatchNumber, EmAppWorkflowProgressStatus.ForceAbortedByUser.ToString(), "Force Aborted By User.", workflowAutomationId);
                            }
                            else
                            {
                                aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Command_Error", ValidationItemType.Error, "Command Error: " + ex.ToString()));

                                SetAppBatchLogProgressStatus(rootCommandDto.BatchNumber, EmAppWorkflowProgressStatus.StoppedWithError.ToString(), ex.ToString(), workflowAutomationId);
                            }

                        }




                    }
                    else
                    {
                        aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Command_Error", ValidationItemType.Error, "Cannot Load Command with Id " + aformData.TransactionCommandId.Value));
                    }
                }
                else
                {
                    aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Command_Error", ValidationItemType.Error, "Empty Command"));
                }
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<TransactionCommandActionResultDto> CreateWorkflowInstanceByIdAndExecute(int workflowTransactionId)
        {
            OperationCallResult<TransactionCommandActionResultDto> aOperationCallResult = new OperationCallResult<TransactionCommandActionResultDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppMasterDetailDto newFormDataDto = AppMasterDetailFormDataLoadBL.GetNewFormData(workflowTransactionId);

            if (newFormDataDto != null)
            {
                //DataModelDateTimeConverterBL.ConvertMasterDetailFromClientToUtc(newFormDataDto);                
                var saveFormResult = AppMasterDetailFormDataSaveBL.SaveTransactionData(newFormDataDto);

                if (saveFormResult.IsSuccessfulWithResult)
                {
                    var workflowFormData = saveFormResult.Object;

                    if (workflowFormData.RootPrimaryKeyValue != null)
                    {

                        return ExecuteWorkflowById(workflowTransactionId, workflowFormData.RootPrimaryKeyValue.ToString());
                    }


                }
                else
                {
                    aValidationResult.Merge(saveFormResult.ValidationResult);
                }
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<TransactionCommandActionResultDto> DeubgWorkflowOneRootChildCommand(int workflowTransactionId, int debugWorkflowRootChildCommandId, string debugKey)
        {
            OperationCallResult<TransactionCommandActionResultDto> aOperationCallResult = new OperationCallResult<TransactionCommandActionResultDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;


            try
            {
                var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(workflowTransactionId);

                var aformData = AppMasterDetailFormDataLoadBL.GetNewFormData(workflowTransactionId);

                aformData.RootPrimaryKeyValue = debugKey;
                aformData.DictOneToOneFields["Name"] = debugKey;
                aformData.TransactionCommandId = (int)transactionExDto.CommandActionList.FirstOrDefault(o => o.ActionAttribute.IsWorkflowRootCommand).Id;
                aformData.DebugWorkflowRootChildCommandId = debugWorkflowRootChildCommandId;

                return ExecuteWorkflowRootCommand(aformData);
            }
            catch (Exception ex)
            {
                aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Command_Error", ValidationItemType.Error, ex.ToString()));
            }


            return aOperationCallResult;
        }



        public static OperationCallResult<TransactionCommandActionResultDto> ExecuteOneRootCommonadById(int commandId, int transactionId, string rootPrimaryKeyValue, int? chlldUnitId = null, string childRowPkCombString = null, StaticSearchResultRowJsonDto searchResultRowDto = null)
        {
            var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);
            bool isWorkflow = transactionExDto.BusinessScopeId.HasValue && transactionExDto.BusinessScopeId.Value == (int)EmAppTransactionScopeUsage.WorkflowAutomation;

            if (transactionExDto.TransactionOrganizedType.HasValue)
            {
                AppProjectWorkFlowActionExDto commandDto = null;
                try
                {
                    commandDto = RetrieveOneTransactionCommandExDto(commandId);

                    if (transactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.MasterDetail)
                    {

                        AppMasterDetailDto aformData = null;

                        if (!string.IsNullOrWhiteSpace(rootPrimaryKeyValue) && rootPrimaryKeyValue != "null" && rootPrimaryKeyValue != "undefined")
                        {
                            aformData = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(transactionId, rootPrimaryKeyValue, null, searchResultRowDto);
                        }
                        else
                        {
                            aformData = AppMasterDetailFormDataLoadBL.GetNewFormData(transactionId);
                        }

                        DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(aformData);

                        aformData.TransactionCommandId = commandId;

                        // Input: Client DateTime
                        // Output: Client DateTime

                        if (isWorkflow)
                        {
                            return ExecuteWorkflowRootCommand(aformData);
                        }
                        else
                        {
                            return ExecuteTransactionRootCommand(aformData, chlldUnitId, childRowPkCombString);
                        }


                    }
                    else if (transactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.List)
                    {

                        var listEditFormData = AppListEditApiFormDataLoadBL.GetApiListEditFormData(transactionId);

                        DataModelDateTimeConverterBL.ConvertListEditFromUtcToClient(listEditFormData);

                        listEditFormData.TransactionCommandId = commandId;

                        // Input: Client DateTime
                        // Output: Client DateTime
                        return ExcuteListEditTransactionCommonad(listEditFormData);
                    }
                }
                catch (Exception ex)
                {
                    OperationCallResult<TransactionCommandActionResultDto> toReturn = new OperationCallResult<TransactionCommandActionResultDto>();
                    toReturn.ValidationResult = new ValidationResult();
                    toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "ExecuteOneTransactionCommonadById_Error", ValidationItemType.Error, ex.ToString()));

                    return toReturn;
                }
            }

            return null;
        }


        public static OperationCallResult<TransactionCommandActionResultDto> ExcuteListEditTransactionCommonad(AppListDataDto listEditFormData)
        {
            AppMasterDetailDto aformData = AppListEditFormDataLoadBL.ConvertAppListDataDtoToMasterDetailDto(listEditFormData);

            if (aformData != null)
            {
                var commandResult = ExecuteTransactionRootCommand(aformData);

                if (commandResult.IsSuccessfulWithResult)
                {

                }

                return commandResult;
            }

            return null;
        }


        public static OperationCallResult<bool> ExecuteCommandOnSelectedSearchResultRows(SearchResultDto searchResultDto)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (searchResultDto != null && searchResultDto.LinkTargetId.HasValue && searchResultDto.SearchResultRowList != null)
            {
                AppFormLinkTargetDto linkTargetDto = LinkTragetBL.RetrieveOneAppFormLinkTargetDto(searchResultDto.LinkTargetId.Value);

                if (linkTargetDto.ActionType == (int)EmAppLinkTargetActionType.ExecuteTransactionCommand &&
                    linkTargetDto.OtherSettingsDto != null && linkTargetDto.OtherSettingsDto.LinkTargetApplyToRowRangeType.HasValue && linkTargetDto.OtherSettingsDto.LinkTargetApplyToRowRangeType.Value == EmAppLinkTargetApplyToRowRangeType.SelectedRows
                    && linkTargetDto.LinkTargetTransactionId.HasValue
                    && linkTargetDto.OpennedFormAutoExecuteCommandId.HasValue
                    && linkTargetDto.SourceViewColumnId1.HasValue)
                {
                    if (searchResultDto != null && searchResultDto.SearchResultRowList != null)
                    {
                        foreach (StaticSearchResultRowJsonDto resultRowDto in searchResultDto.SearchResultRowList)
                        {
                            resultRowDto.SelectedRowLinkTargetId = (int)linkTargetDto.Id;

                            if (linkTargetDto.SourceConditionViewColumnId.HasValue)
                            {
                                if (resultRowDto.DictViewColumnIDKeyValue[linkTargetDto.SourceConditionViewColumnId.Value] != null)
                                {
                                    bool? conditionValue = ControlTypeValueConverter.ConvertValueToBoolean(resultRowDto.DictViewColumnIDKeyValue[linkTargetDto.SourceConditionViewColumnId.Value]);

                                    if (conditionValue.HasValue && conditionValue.Value == false)
                                    {
                                        continue;
                                    }
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            string targetPkValue = ExecuteLinkTargetCommand_GetOnResultRowTargetPkValue(linkTargetDto, resultRowDto);

                            if (!string.IsNullOrWhiteSpace(targetPkValue))
                            {
                                OperationCallResult<TransactionCommandActionResultDto> oneResult = AppTransactionCommandBL.ExecuteOneRootCommonadById(
                                    linkTargetDto.OpennedFormAutoExecuteCommandId.Value,
                                    linkTargetDto.LinkTargetTransactionId.Value,
                                    targetPkValue, null, null, resultRowDto);

                                if (oneResult.ValidationResult.HasErrors)
                                {
                                    string errorMsg = "Execution failed on item key: " + targetPkValue + ".\n";
                                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "ExecuteSearchViewLinkTargetCommand_Error", ValidationItemType.Error, errorMsg));
                                    //aValidationResult.Merge(oneResult.ValidationResult);
                                }
                            }
                        }

                        if (!aValidationResult.HasErrors)
                        {
                            aOperationCallResult.Object = true;
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "ExecuteSearchViewLinkTargetCommand_Success", ValidationItemType.Message, "Command Execution Success."));
                        }
                        //aOperationCallResult.Object = true;
                    }
                    else
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "ExecuteSearchViewLinkTargetCommand_Error", ValidationItemType.Error, "Invalid search result."));
                    }
                }
                else
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "ExecuteSearchViewLinkTargetCommand_Error", ValidationItemType.Error, "Invalid LinkTarget."));
                }
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "ExecuteSearchViewLinkTargetCommand_Error", ValidationItemType.Error, "Invalid LinkTarget."));
            }

            return aOperationCallResult;

        }


        public static OperationCallResult<TransactionCommandActionResultDto> ExecuteWorkflowById(int workflowDataModelId, string rootPrimaryKeyValue = "")
        {
            OperationCallResult<TransactionCommandActionResultDto> aOperationCallResult = new OperationCallResult<TransactionCommandActionResultDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(workflowDataModelId);

            if (transactionExDto.TransactionOrganizedType.HasValue && transactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.MasterDetail
                && transactionExDto.BusinessScopeId.HasValue && transactionExDto.BusinessScopeId.Value == (int)EmAppTransactionScopeUsage.WorkflowAutomation)
            {
                var workflowCommandList = transactionExDto.CommandActionList;

                var rootCommand = workflowCommandList.FirstOrDefault(o => o.ActionAttribute != null && o.ActionAttribute.IsWorkflowRootCommand);

                if (rootCommand != null)
                {
                    int? rootCommnadId = ControlTypeValueConverter.ConvertValueToInt(rootCommand.Id);

                    if (rootCommnadId.HasValue)
                    {

                        AppMasterDetailDto aformData = null;

                        if (!string.IsNullOrWhiteSpace(rootPrimaryKeyValue) && rootPrimaryKeyValue != "null" && rootPrimaryKeyValue != "undefined")
                        {
                            aformData = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(workflowDataModelId, rootPrimaryKeyValue, null);
                        }
                        else
                        {
                            aformData = AppMasterDetailFormDataLoadBL.GetNewFormData(workflowDataModelId);
                        }

                        DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(aformData);

                        aformData.TransactionCommandId = rootCommnadId.Value;

                        var executionResult = ExecuteWorkflowRootCommand(aformData);
                        aOperationCallResult = executionResult;

                        if (aOperationCallResult.IsSuccessful)
                        {
                            if (!aOperationCallResult.ValidationResult.HasItems)
                            {
                                aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "ExecuteWorkflowById_Success", ValidationItemType.Message, "The execution of workflow \"" + transactionExDto.TransactionName + "\" has been completed."));
                            }
                        }

                    }
                }
            }
            else
            {
                aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "ExecuteWorkflowById_Error", ValidationItemType.Error, "Invalid workflow: " + transactionExDto.TransactionName));
            }



            return aOperationCallResult;
        }



        public static OperationCallResult<TransactionCommandActionResultDto> ExecuteCommandFromTransFieldIdValueParameters(int? commandId, object transactionRId, List<string> transFieldIdValueParameterStrList)
        {
            if (commandId.HasValue && transactionRId != null)
            {
                AppProjectWorkFlowActionExDto commandDto = AppTransactionCommandBL.RetrieveOneTransactionCommandExDto(commandId.Value);

                if (commandDto != null && commandDto.CommandTransactionId.HasValue)
                {
                    int transactionId = commandDto.CommandTransactionId.Value;

                    AppMasterDetailDto aformData = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(transactionId, transactionRId);
                    aformData.TransactionCommandId = commandId;

                    DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(aformData);
                    PrepareFormDataFromTransFieldIdValueParameters(aformData, transFieldIdValueParameterStrList);

                    // Input: Client DateTime
                    // Output: Client DateTime
                    return AppTransactionCommandBL.ExecuteTransactionRootCommand(aformData);
                }

            }

            return null;
        }


        private static OperationCallResult<TransactionCommandActionResultDto> ExcuteOneChildCommonad(AppMasterDetailDto aformData, int? chlldUnitId,
           string childRowPkCombString, bool isForceNotRunAsBatch, AppProjectWorkFlowActionExDto rootCommandDto, bool? goToNextWithError = null, string batchLoopCountDispay = "")
        {

            bool isCallingFromWorkflow = rootCommandDto != null && rootCommandDto.IsWorkflowAutomationRootCommand;

            if (isCallingFromWorkflow)
            {
                return ExecuteOneWorkflowChildCommonad(aformData, chlldUnitId, childRowPkCombString, isForceNotRunAsBatch, rootCommandDto, goToNextWithError, batchLoopCountDispay);
            }
            else
            {
                return ExecuteOneTransactionChlidCommand(aformData, chlldUnitId, childRowPkCombString, isForceNotRunAsBatch, rootCommandDto);
            }
        }


        public static OperationCallResult<TransactionCommandActionResultDto> ExecuteOneTransactionChlidCommand(AppMasterDetailDto aformData, int? chlldUnitId = null,
            string childRowPkCombString = null, bool isForceNotRunAsBatch = false, AppProjectWorkFlowActionExDto rootCommandDto = null)
        {
            OperationCallResult<TransactionCommandActionResultDto> aOperationCallResult = new OperationCallResult<TransactionCommandActionResultDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppProjectWorkFlowActionExDto commandDto = null;
            AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aformData.TransactionId);
            bool isRootCommand = rootCommandDto == null;

            try
            {
                if (aformData.TransactionCommandId.HasValue)
                {
                    if (transactionExDto.CommandActionList.FirstOrDefault(o => (int)o.Id == aformData.TransactionCommandId.Value) != null)
                    {
                        string transactionRId = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(aformData.RootPrimaryKeyValue);

                        commandDto = RetrieveOneTransactionCommandExDto(aformData.TransactionCommandId.Value);

                        if (commandDto != null && transactionExDto != null)
                        {
                            if (isRootCommand)
                            {
                                rootCommandDto = commandDto;
                                rootCommandDto.RootTransactionFormData = aformData;
                            }

                            bool isAllowExecuteByCondition = AppTransactionCommandBL.CheckCommandCondition(aformData, transactionExDto, commandDto, rootCommandDto);

                            if (isAllowExecuteByCondition)
                            {
                                if (commandDto.ActionAttribute.IsBatchCommand && !isForceNotRunAsBatch
                                    && !(commandDto.ActionAttribute.BatchCommandSourceFromType.HasValue && commandDto.ActionAttribute.BatchCommandSourceFromType.Value == (int)EmAppBatchCommandSourceFrom.ChildUnit))
                                {
                                    var batchCommandValidationResult = ExecuteBatchCommand_LoopOnDataSetOrSearch(aformData, new ValidationResult(), commandDto, rootCommandDto);
                                    batchCommandValidationResult.Items.ForAll(o => o.IsChildCommandValidationItem = true);
                                    aOperationCallResult.ValidationResult.Merge(batchCommandValidationResult);
                                }
                                else
                                {
                                    if (commandDto.ActionType.HasValue)
                                    {
                                        if (commandDto.ActionType.Value == (int)EmAppTransactionCommandType.CompositionCommand)
                                        {
                                            aOperationCallResult = ExecuteCompositionCommand(commandDto, aformData, chlldUnitId, childRowPkCombString, rootCommandDto);
                                        }
                                        else
                                        {
                                            aOperationCallResult = ExecuteSingleCommandByActionType(commandDto, aformData, transactionExDto, rootCommandDto);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Condition pass failure", ValidationItemType.Error, "Command execute condition is not ready."));
                            }

                            RebuildToSimpleValidationResultBySetting(aformData, aOperationCallResult.ValidationResult, commandDto, transactionExDto, transactionRId);
                        }
                        else
                        {
                            aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Command_Error", ValidationItemType.Error, "Cannot Load Command with Id " + aformData.TransactionCommandId.Value));
                        }
                    }
                }
                else
                {
                    aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Command_Error", ValidationItemType.Error, "Empty Command"));
                }
            }
            catch (Exception ex)
            {
                aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Command_Error", ValidationItemType.Error, "Command Error: " + ex.ToString()));
            }

            return aOperationCallResult;
        }

        private static OperationCallResult<TransactionCommandActionResultDto> ExecuteOneWorkflowChildCommonad(AppMasterDetailDto aformData, int? chlldUnitId = null,
           string childRowPkCombString = null, bool isForceNotRunAsBatch = false, AppProjectWorkFlowActionExDto rootCommandDto = null, bool? goToNextWithError = null, string batchLoopCountDispay = "")
        {
            OperationCallResult<TransactionCommandActionResultDto> aOperationCallResult = new OperationCallResult<TransactionCommandActionResultDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppProjectWorkFlowActionExDto commandDto = null;
            bool isRootCommand = rootCommandDto == null;
            var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aformData.TransactionId);

            try
            {
                if (aformData.TransactionCommandId.HasValue)
                {
                    if (transactionExDto.CommandActionList.FirstOrDefault(o => (int)o.Id == aformData.TransactionCommandId.Value) != null)
                    {
                        string transactionRId = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(aformData.RootPrimaryKeyValue);

                        commandDto = RetrieveOneTransactionCommandExDto(aformData.TransactionCommandId.Value);

                        if (commandDto != null && transactionExDto != null)
                        {
                            InitialOneWorkflowChildCommand(aformData, rootCommandDto, aOperationCallResult, commandDto, transactionRId, batchLoopCountDispay);

                            bool isAllowExecuteByCondition = AppTransactionCommandBL.CheckCommandCondition(aformData, transactionExDto, commandDto, rootCommandDto);

                            if (isAllowExecuteByCondition)
                            {
                                if (commandDto.ActionAttribute.IsBatchCommand && !isForceNotRunAsBatch
                                    && !(commandDto.ActionAttribute.BatchCommandSourceFromType.HasValue && commandDto.ActionAttribute.BatchCommandSourceFromType.Value == (int)EmAppBatchCommandSourceFrom.ChildUnit))
                                {
                                    var batchCommandValidationResult = ExecuteBatchCommand_LoopOnDataSetOrSearch(aformData, new ValidationResult(), commandDto, rootCommandDto);
                                    batchCommandValidationResult.Items.ForAll(o => o.IsChildCommandValidationItem = true);
                                    aOperationCallResult.ValidationResult.Merge(batchCommandValidationResult);
                                }
                                else
                                {
                                    if (commandDto.ActionType.HasValue)
                                    {
                                        if (commandDto.ActionType.Value == (int)EmAppTransactionCommandType.CompositionCommand)
                                        {
                                            aOperationCallResult = ExecuteCompositionCommand(commandDto, aformData, chlldUnitId, childRowPkCombString, rootCommandDto);
                                        }
                                        else
                                        {
                                            aOperationCallResult = ExecuteSingleCommandByActionType(commandDto, aformData, transactionExDto, rootCommandDto);
                                        }
                                    }

                                    WorkflowChildCommand_LogExecutionEnd(aformData, rootCommandDto, goToNextWithError, aOperationCallResult, commandDto, (int)transactionExDto.Id);

                                }
                            }
                            else
                            {
                                aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Condition pass failure", ValidationItemType.Error, "Command execute condition is not ready."));

                                WorkflowChildCommand_LogIgnoredByCodition(aformData, rootCommandDto, aOperationCallResult, commandDto, transactionExDto);
                            }

                            RebuildToSimpleValidationResultBySetting(aformData, aOperationCallResult.ValidationResult, commandDto, transactionExDto, transactionRId);

                            if (!aOperationCallResult.ValidationResult.HasErrors)
                            {
                                commandDto.ProgressStatus = EmAppCommandProgressStatus.Completed.ToString();
                                UpdateCurrentWorkflowTreeNodeProgressStatus(rootCommandDto, commandDto);
                            }

                            LogWorkflowCommandEnd(commandDto, aformData.TransactionId, transactionRId, rootCommandDto);
                        }
                        else
                        {
                            aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Command_Error", ValidationItemType.Error, "Cannot Load Command with Id " + aformData.TransactionCommandId.Value));
                        }
                    }

                }
                else
                {
                    aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Command_Error", ValidationItemType.Error, "Empty Command"));
                }
            }
            catch (Exception ex)
            {
                if (ex is IntegrationPauseException)
                {
                    if (commandDto != null)
                    {
                        commandDto.ProgressStatus = EmAppCommandProgressStatus.ForceAbortedByUser.ToString();
                        UpdateCurrentWorkflowTreeNodeProgressStatus(rootCommandDto, commandDto);
                        UpdateCurrentExecutingCommandAppBatchLogRuntimeData(commandDto.TreeNodeLogicId, rootCommandDto);
                    }

                    throw new IntegrationPauseException(ex.Message);
                }
                else
                {
                    aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Command_Error", ValidationItemType.Error, "Command Error: " + ex.ToString()));

                    commandDto.ErrorMessage = "Command Error: " + ex.ToString();
                    UpdateCurrentWorkflowTreeNodeProgressStatus(rootCommandDto, commandDto);
                    LogWorkflowChildCommandErrorDetails(aformData, aOperationCallResult.ValidationResult, (int)transactionExDto.Id, commandDto, rootCommandDto);

                }
            }

            return aOperationCallResult;
        }


        private static OperationCallResult<TransactionCommandActionResultDto> ExecuteOneChildCommonadById(int commandId, AppProjectWorkFlowActionExDto rootCommandDto, int transactionId, string rootPrimaryKeyValue, int? chlldUnitId = null, string childRowPkCombString = null, StaticSearchResultRowJsonDto searchResultRowDto = null, bool isForceNotRunAsBatch = false, bool? goToNextWithError = null, string batchLoopCountDispay = "")
        {
            var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);
            bool isWorkflowChild = rootCommandDto != null && rootCommandDto.IsWorkflowAutomationRootCommand;

            if (transactionExDto.TransactionOrganizedType.HasValue)
            {
                AppProjectWorkFlowActionExDto commandDto = null;
                try
                {
                    commandDto = RetrieveOneTransactionCommandExDto(commandId);

                    if (transactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.MasterDetail)
                    {

                        AppMasterDetailDto aformData = null;

                        if (!string.IsNullOrWhiteSpace(rootPrimaryKeyValue) && rootPrimaryKeyValue != "null" && rootPrimaryKeyValue != "undefined")
                        {
                            aformData = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(transactionId, rootPrimaryKeyValue, null, searchResultRowDto);
                        }
                        else
                        {
                            aformData = AppMasterDetailFormDataLoadBL.GetNewFormData(transactionId);
                        }

                        DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(aformData);

                        aformData.TransactionCommandId = commandId;

                        // Input: Client DateTime
                        // Output: Client DateTime                                               

                        return ExcuteOneChildCommonad(aformData, chlldUnitId, childRowPkCombString, isForceNotRunAsBatch, rootCommandDto, goToNextWithError, batchLoopCountDispay);
                    }

                }
                catch (Exception ex)
                {
                    OperationCallResult<TransactionCommandActionResultDto> toReturn = new OperationCallResult<TransactionCommandActionResultDto>();
                    toReturn.ValidationResult = new ValidationResult();


                    if (rootCommandDto != null && rootCommandDto.IsWorkflowAutomationRootCommand)
                    {
                        if (ex is IntegrationPauseException)
                        {
                            toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "ExecuteOneTransactionCommonadById_Error", ValidationItemType.Error, ex.Message));
                        }
                        else
                        {
                            toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "ExecuteOneTransactionCommonadById_Error", ValidationItemType.Error, ex.ToString()));
                        }


                        if (commandDto != null)
                        {
                            commandDto.ErrorMessage = "Command Error: " + ex.ToString();
                            AppMasterDetailDto placeHolderForm = new AppMasterDetailDto();
                            placeHolderForm.RootPrimaryKeyValue = rootPrimaryKeyValue;
                            UpdateCurrentWorkflowTreeNodeProgressStatus(rootCommandDto, commandDto);
                            LogWorkflowChildCommandErrorDetails(placeHolderForm, toReturn.ValidationResult, (int)transactionExDto.Id, commandDto, rootCommandDto);
                        }

                        if (ex is IntegrationPauseException)
                        {
                            throw new IntegrationPauseException(ex.Message);
                        }
                    }
                    else
                    {
                        toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "ExecuteOneTransactionCommonadById_Error", ValidationItemType.Error, ex.ToString()));
                    }


                    return toReturn;
                }
            }

            return null;
        }








        private static void WorkflowChildCommand_LogIgnoredByCodition(AppMasterDetailDto aformData, AppProjectWorkFlowActionExDto rootCommandDto, OperationCallResult<TransactionCommandActionResultDto> aOperationCallResult, AppProjectWorkFlowActionExDto commandDto, AppTransactionExDto transactionExDto)
        {
            commandDto.ProgressStatus = EmAppCommandProgressStatus.Ignored.ToString();
            commandDto.ErrorMessage = "Command execute condition is not ready.";
            UpdateCurrentWorkflowTreeNodeProgressStatus(rootCommandDto, commandDto);
            LogWorkflowChildCommandErrorDetails(aformData, aOperationCallResult.ValidationResult, (int)transactionExDto.Id, commandDto, rootCommandDto);
        }

        private static void WorkflowChildCommand_LogExecutionEnd(AppMasterDetailDto aformData, AppProjectWorkFlowActionExDto rootCommandDto, bool? goToNextWithError, OperationCallResult<TransactionCommandActionResultDto> aOperationCallResult, AppProjectWorkFlowActionExDto commandDto, int mainTransactionId)
        {
            if (aOperationCallResult.ValidationResult.HasErrors)
            {
                commandDto.ProgressStatus = EmAppCommandProgressStatus.StoppedWithError.ToString();

                if (rootCommandDto.ProgressStatus == EmAppWorkflowProgressStatus.ForceAbortedByUser.ToString())
                {
                    commandDto.ProgressStatus = EmAppCommandProgressStatus.ForceAbortedByUser.ToString();
                }
                else if (goToNextWithError.HasValue && goToNextWithError.Value)
                {
                    commandDto.ProgressStatus = EmAppCommandProgressStatus.CompletedWithError.ToString();
                }

                commandDto.ErrorMessage = "Please check error log for details.";

                UpdateCurrentWorkflowTreeNodeProgressStatus(rootCommandDto, commandDto);
            }

            LogWorkflowChildCommandErrorDetails(aformData, aOperationCallResult.ValidationResult, mainTransactionId, commandDto, rootCommandDto);
        }

        private static void InitialOneWorkflowChildCommand(AppMasterDetailDto aformData, AppProjectWorkFlowActionExDto rootCommandDto, OperationCallResult<TransactionCommandActionResultDto> aOperationCallResult, AppProjectWorkFlowActionExDto commandDto, string transactionRId, string batchLoopCountDispay)
        {
            commandDto.RootTransactionFormData = rootCommandDto.RootTransactionFormData;
            commandDto.TreeNodeLogicId = rootCommandDto.CurrentExecutingTreeNodeLogicId;

            if (!string.IsNullOrWhiteSpace(rootCommandDto.BatchNumber))
            {
                bool isWorkflowForceStopped = AppCacheManagerBL.GetIsWorkflowForceStoppedByBatchIdFromCache(rootCommandDto.BatchNumber);

                if (isWorkflowForceStopped)
                {
                    commandDto.ErrorMessage = "Force Aborted By User";
                    commandDto.ProgressStatus = EmAppCommandProgressStatus.ForceAbortedByUser.ToString();
                    //rootCommandDto.ProgressStatus = EmAppWorkflowProgressStatus.ForceAbortedByUser.ToString();
                    //UpdateCurrentWorkflowTreeNodeProgressStatus(rootCommandDto, commandDto);
                    //UpdateCurrentExecutingCommandAppBatchLogRuntimeData(commandDto.TreeNodeLogicId, rootCommandDto);

                    aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Command_Error", ValidationItemType.Error, "Force Aborted By User."));

                    //Thread.CurrentThread.Abort();


                    WorkflowChildCommand_LogExecutionEnd(aformData, rootCommandDto, false, aOperationCallResult, commandDto, aformData.TransactionId);

                    throw new IntegrationPauseException("Force Aborted By User");



                }
            }

            if (commandDto.ActionAttribute != null)
            {
                commandDto.ProgressStatus = EmAppCommandProgressStatus.Running.ToString();

                if (!string.IsNullOrWhiteSpace(batchLoopCountDispay))
                {
                    commandDto.ProgressStatus += " " + batchLoopCountDispay;
                }

                UpdateCurrentWorkflowTreeNodeProgressStatus(rootCommandDto, commandDto);

                LogWorkflowCommandStart(commandDto, aformData.TransactionId, transactionRId, rootCommandDto, batchLoopCountDispay);
            }
        }

        private static void UpdateCurrentWorkflowTreeNodeProgressStatus(AppProjectWorkFlowActionExDto rootCommandDto, AppProjectWorkFlowActionExDto commandDto)
        {
            var treeNodeDto = FindOneWorkflowCommandTreeNodeByLogicId(rootCommandDto.WorkflowCommandNodeTree, commandDto.TreeNodeLogicId);

            if (treeNodeDto != null)
            {
                treeNodeDto.ProgressStatus = commandDto.ProgressStatus;
                treeNodeDto.ErrorMessage = commandDto.ErrorMessage;
            }
        }

        private static void PrepareFormDataFromTransFieldIdValueParameters(AppMasterDetailDto aformData, List<string> transFieldIdValueParameterStrList)
        {

            if (transFieldIdValueParameterStrList != null && transFieldIdValueParameterStrList.Count > 0)
            {
                AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aformData.TransactionId);

                Dictionary<int, AppTransactionFieldExDto> dictTransFieldIdAndDto = transactionExDto.DictAllTransactionField;

                for (int i = 0; i < transFieldIdValueParameterStrList.Count; i++)
                {
                    string transFieldIdAndValuePairStr = transFieldIdValueParameterStrList[i];

                    string[] transFieldIdAndValuePair = transFieldIdAndValuePairStr.Split('_');

                    if (transFieldIdAndValuePair.Length >= 2)
                    {
                        int? transFieldId = ControlTypeValueConverter.ConvertValueToInt(transFieldIdAndValuePair[0]);
                        string fieldValueStr = transFieldIdAndValuePair[1];

                        if (transFieldId.HasValue && dictTransFieldIdAndDto.ContainsKey(transFieldId.Value))
                        {
                            var transFieldExDto = dictTransFieldIdAndDto[transFieldId.Value];
                            object fieldValue = null;

                            if (transFieldExDto.ControlType == (int)EmAppControlType.CheckBox)
                            {
                                fieldValue = (object)ControlTypeValueConverter.ConvertValueToBoolean(fieldValueStr);
                            }
                            else if (transFieldExDto.ControlType == (int)EmAppControlType.DDL && transFieldExDto.DataType.HasValue && transFieldExDto.DataType.Value == (int)EmAppDataType.Integer)
                            {
                                fieldValue = (object)ControlTypeValueConverter.ConvertValueToInt(fieldValueStr);
                            }
                            else if (transFieldExDto.ControlType == (int)EmAppControlType.Numeric)
                            {
                                if ((transFieldExDto.DataType.HasValue && transFieldExDto.DataType.Value == (int)EmAppDataType.Integer) || (transFieldExDto.Nbdecimal.HasValue && transFieldExDto.Nbdecimal.Value == 0))
                                {
                                    fieldValue = (object)ControlTypeValueConverter.ConvertValueToInt(fieldValueStr);
                                }
                                else
                                {
                                    fieldValue = (object)ControlTypeValueConverter.ConvertValueToDecimal(fieldValueStr);
                                }
                            }
                            else
                            {
                                fieldValue = (object)fieldValueStr;
                            }

                            if (transFieldExDto.IsSiblingField)
                            {
                                aformData.DictSiblingOneToOneFields[transFieldExDto.TransactionUnitId.ToString()][transFieldExDto.DataBaseFieldName] = fieldValue;
                            }
                            else if (transFieldExDto.TransactionUnitId == (int)transactionExDto.RootMasterUnit.Id)
                            {
                                aformData.DictOneToOneFields[transFieldExDto.DataBaseFieldName] = fieldValue;
                            }
                        }
                    }
                }
            }
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



        public static OperationCallResult<bool> SaveOneTransactionCommandActionList(AppTransactionExDto aAppTransactionExDto)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;



            if (aAppTransactionExDto != null && aAppTransactionExDto.Id != null && aAppTransactionExDto.CommandActionList != null)
            {
                try
                {

                    var needToSaveCommandList = aAppTransactionExDto.CommandActionList;

                    //needToSaveCommandList.ForAll(o => {
                    //    if (!(o.ActionType.HasValue && o.ActionType.Value == (int)EmAppTransactionCommandType.CompositionCommand) && o.ActionAttribute != null)
                    //    {
                    //        o.ActionAttribute.IsSearchResultBatchCommand = false;
                    //    }

                    //    if (o.ActionAttribute.IsSearchResultBatchCommand)
                    //    {
                    //        o.ActionAttribute.IsShowOnSearchViewEventOptionMenu = false;
                    //        //o.ActionAttribute.IsShowOnTopMenu = false;
                    //        o.ActionAttribute.IsAutoExecuteOnFormOpen = false;
                    //        o.ActionAttribute.IsEnableCommandExecuteResultMessage = false;                        
                    //    }
                    //});

                    var orgTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aAppTransactionExDto.Id);

                    List<int> deletActionIds = new List<int>();
                    EntityCollection<AppProjectWorkFlowActionEntity> newActionEntities = new EntityCollection<AppProjectWorkFlowActionEntity>();
                    EntityCollection<AppProjectWorkFlowActionEntity> dirtyActionEntities = new EntityCollection<AppProjectWorkFlowActionEntity>();

                    var orgCommandActionList = AppTransactionCommandBL.RetrieveOneTransactionCommandActionList(orgTransactionExDto);
                    List<int> actionIdDB = orgCommandActionList.Where(o => o.Id != null).Select(o => (int)o.Id).ToList();
                    List<int> actionIdUI = needToSaveCommandList.Where(o => o.Id != null).Select(o => (int)o.Id).ToList();

                    deletActionIds = actionIdDB.Except(actionIdUI).ToList();





                    List<AppTransactionFieldExDto> transactionFieldList = orgTransactionExDto.DictAllTransactionField.Values.ToList();
                    AppTransactionFormulaSetupBL.InitialTransactionFieldFormularDisplayName(orgTransactionExDto, transactionFieldList);

                    if (aAppTransactionExDto.RootWorkflowTransactionId.HasValue && aAppTransactionExDto.RootWorkflowTransactionId.Value != (int)aAppTransactionExDto.Id)
                    {
                        MergeWorkflowGlobalFieldWithFormularDisplayName(aAppTransactionExDto.RootWorkflowTransactionId.Value, transactionFieldList);
                    }

                    foreach (var actionDto in needToSaveCommandList)
                    {
                        if (actionDto.ActionType.HasValue && actionDto.ActionType.Value == (int)EmAppTransactionCommandType.CommnadFormulaCalculation)
                        {
                            InFormatExecutionFormula(transactionFieldList, actionDto);
                        }
                    }

                    foreach (var dto in needToSaveCommandList)
                    {
                        if (!dto.CommandTransactionId.HasValue)
                        {
                            dto.CommandTransactionId = ControlTypeValueConverter.ConvertValueToInt(aAppTransactionExDto.Id);
                        }

                        if (dto.ActionAttribute == null)
                        {
                            dto.ActionAttribute = new AppActionAttributeDto() { ChildActionList = new List<ChildTransactionCommandDto>() };
                        }

                        if (dto.ActionAttribute.ChildActionList != null)
                        {
                            List<ChildTransactionCommandDto> needToRemoveChildActionList = new List<ChildTransactionCommandDto>();
                            foreach (var childActionDto in dto.ActionAttribute.ChildActionList)
                            {
                                if (childActionDto.CommandId.HasValue)
                                {

                                    if (childActionDto.ExternalTransactionId.HasValue)
                                    {
                                        var exTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(childActionDto.ExternalTransactionId.Value);

                                        bool externalCommandExist = exTransactionExDto.CommandActionList != null && exTransactionExDto.CommandActionList.FirstOrDefault(o => (int)o.Id == childActionDto.CommandId.Value) != null;

                                        if (!externalCommandExist)
                                        {
                                            needToRemoveChildActionList.Add(childActionDto);
                                        }
                                    }
                                    else
                                    {
                                        if (deletActionIds.Contains(childActionDto.CommandId.Value))
                                        {
                                            needToRemoveChildActionList.Add(childActionDto);
                                        }
                                    }
                                }
                            }

                            foreach (var childActionDto in needToRemoveChildActionList)
                            {
                                dto.ActionAttribute.ChildActionList.Remove(childActionDto);
                            }
                        }


                        if (!dto.ActionAttribute.EmAppValidationResultPreference.HasValue)
                        {
                            dto.ActionAttribute.EmAppValidationResultPreference = EmAppValidationResultPreference.ShowResultDetails;
                        }

                        //if (!dto.ActionAttribute.EmAppCommandLoggingPreference.HasValue)
                        //{
                        //    dto.ActionAttribute.EmAppCommandLoggingPreference = EmAppCommandLoggingPreference.DoNotLog;
                        //}

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
                catch (Exception ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "App_TransactionEntity_Error", ValidationItemType.Error, ex.ToString()));
                }
            }



            return aOperationCallResult;
        }

        public static OperationCallResult<AppTransactionExDto> SaveOneWorkflowAutomation(AppTransactionExDto aAppTransactionExDto)
        {
            OperationCallResult<AppTransactionExDto> aOperationCallResult = new OperationCallResult<AppTransactionExDto>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (aAppTransactionExDto != null)
            {
                List<AppProjectWorkFlowActionExDto> commandList = aAppTransactionExDto.CommandActionList;

                if (aAppTransactionExDto.OtherOptions == null)
                {
                    aAppTransactionExDto.OtherOptions = new TransactionOptionDto();
                }

                //if (aAppTransactionExDto.OtherOptions.WorkflowAutomationCommandList == null)
                //{
                //    aAppTransactionExDto.OtherOptions.WorkflowAutomationCommandList = new List<AppProjectWorkFlowActionExDto>();
                //}

                aAppTransactionExDto.IsShowSaveButton = true;

                var saveResult1 = AppTransactionBL.SaveAppTransactionExDto(aAppTransactionExDto);

                if (saveResult1.IsSuccessfulWithResult)
                {
                    aAppTransactionExDto = saveResult1.Object;
                }
                else
                {
                    aValidationResult.Merge(saveResult1.ValidationResult);
                }

                if (!aAppTransactionExDto.FormId.HasValue)
                {
                    AppFormExDto formDto = AppFormBL.CreateNewTranactionForm((int)aAppTransactionExDto.Id, (int)EmAppFormLayoutType.Flex, aAppTransactionExDto.SaasApplicationId, false);
                    aAppTransactionExDto.FormId = (int)formDto.Id;
                }

                if (aAppTransactionExDto.RootMasterUnit != null)
                {
                    var rebultFormDto = AppFormFlexLayoutBL.BuildAppFormDefaultLayout(aAppTransactionExDto.FormId.Value);

                    var saveResult = AppFormFlexLayoutBL.SaveAppFormFlexLayoutExDto(rebultFormDto);

                    if (saveResult.ValidationResult.HasErrors)
                    {
                        aValidationResult.Merge(saveResult.ValidationResult);
                    }
                }

                if (!aValidationResult.HasErrors)
                {
                    if (commandList != null)
                    {
                        var rootCommand = commandList.FirstOrDefault(o => o.ActionAttribute != null && o.ActionAttribute.IsWorkflowRootCommand);
                        if (rootCommand != null)
                        {
                            rootCommand.Name = aAppTransactionExDto.TransactionName;
                        }

                        commandList.ForAll(o => o.CommandTransactionId = (int)aAppTransactionExDto.Id);
                        aAppTransactionExDto.CommandActionList = commandList;

                        var saveCommandResult = SaveOneTransactionCommandActionList(aAppTransactionExDto);
                        aValidationResult.Merge(saveCommandResult.ValidationResult);

                        if (saveCommandResult.IsSuccessful)
                        {
                            aOperationCallResult.Object = AppTransactionBL.GetHierarchyTranscationFromDatabase(aAppTransactionExDto.Id);

                            //SyncronizeWorkflowCommandNodeTreeFromActionList(aOperationCallResult.Object);
                        }
                    }
                }
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<AppProjectWorkFlowActionExDto> CreateOneTransactionCommand(AppProjectWorkFlowActionExDto commandExDto)
        {
            OperationCallResult<AppProjectWorkFlowActionExDto> aOperationCallResult = new OperationCallResult<AppProjectWorkFlowActionExDto>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppProjectWorkFlowActionEntity entity = new AppProjectWorkFlowActionEntity();
            AppProjectWorkFlowActionConverter.CopyDtoToEntity(entity, commandExDto);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(entity, false, true);

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectWorkFlowActionEntity), "plm_AppProjectWorkFlowActionEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }


                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionNavigationEntity), "plm_AppProjectWorkFlowActionEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneTransactionCommand(entity.WorkFlowActionId);
            }

            return aOperationCallResult;
        }

        public static AppProjectWorkFlowActionExDto RetrieveOneTransactionCommand(object Id)
        {
            if (Id != null)
            {
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    AppProjectWorkFlowActionEntity entity = new AppProjectWorkFlowActionEntity(int.Parse(Id.ToString()));
                    adapter.FetchEntity(entity);

                    AppProjectWorkFlowActionExDto appProjectWorkFlowActionExDto = AppProjectWorkFlowActionConverter.ConvertEntityToExDto(entity);

                    return appProjectWorkFlowActionExDto;
                }
            }

            return null;
        }


        public static List<AppProjectWorkFlowActionExDto> RetrieveCommandListByIds(List<int> IdList)
        {
            List<AppProjectWorkFlowActionExDto> toReturn = new List<AppProjectWorkFlowActionExDto>();

            if (IdList != null && IdList.Count > 0)
            {
                using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
                {
                    EntityCollection<AppProjectWorkFlowActionEntity> commandActionList = new EntityCollection<AppProjectWorkFlowActionEntity>();
                    adpater.FetchEntityCollection(commandActionList, new RelationPredicateBucket(AppProjectWorkFlowActionFields.WorkFlowActionId == IdList));


                    foreach (var entity in commandActionList)
                    {
                        var actionDto = AppProjectWorkFlowActionConverter.ConvertEntityToExDto(entity);


                        if (!actionDto.ActionAttribute.EmAppValidationResultPreference.HasValue)
                        {
                            actionDto.ActionAttribute.EmAppValidationResultPreference = EmAppValidationResultPreference.ShowResultDetails;
                        }

                        //if (!actionDto.ActionAttribute.EmAppCommandLoggingPreference.HasValue)
                        //{
                        //    actionDto.ActionAttribute.EmAppCommandLoggingPreference = EmAppCommandLoggingPreference.DoNotLog;
                        //}

                        //InitChildCommandProperties(commandActionList, actionDto);

                        toReturn.Add(actionDto);
                    }
                }
            }

            return toReturn;
        }


        public static List<AppProjectWorkFlowActionExDto> RetrieveOneTransactionCommandActionList(AppTransactionExDto aAppTransactionExDto, int? rootWorkflowTransactionId = null)
        {
            List<AppProjectWorkFlowActionExDto> toReturn = new List<AppProjectWorkFlowActionExDto>();

            if (aAppTransactionExDto != null && aAppTransactionExDto.Id != null)
            {
                var transactionFieldList = aAppTransactionExDto.DictAllTransactionField.Values.ToList();

                AppTransactionFormulaSetupBL.InitialTransactionFieldFormularDisplayName(aAppTransactionExDto, transactionFieldList);

                if (rootWorkflowTransactionId.HasValue && rootWorkflowTransactionId.Value != (int)aAppTransactionExDto.Id)
                {
                    MergeWorkflowGlobalFieldWithFormularDisplayName(rootWorkflowTransactionId.Value, transactionFieldList);
                }

                using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
                {
                    EntityCollection<AppProjectWorkFlowActionEntity> commandActionList = new EntityCollection<AppProjectWorkFlowActionEntity>();
                    adpater.FetchEntityCollection(commandActionList, new RelationPredicateBucket(AppProjectWorkFlowActionFields.CommandTransactionId == (int)aAppTransactionExDto.Id));


                    foreach (var entity in commandActionList)
                    {
                        var actionDto = AppProjectWorkFlowActionConverter.ConvertEntityToExDto(entity);

                        //AppPorjectWorkFlowBL.OutFormatActionFormulaExpress(transactionFieldList, actionDto);

                        if (actionDto.ActionType.HasValue && actionDto.ActionType.Value == (int)EmAppTransactionCommandType.CommnadFormulaCalculation)
                        {
                            OutFormatExecutionFormula(transactionFieldList, actionDto);
                        }


                        if (!actionDto.ActionAttribute.EmAppValidationResultPreference.HasValue)
                        {
                            actionDto.ActionAttribute.EmAppValidationResultPreference = EmAppValidationResultPreference.ShowResultDetails;
                        }

                        //if (!actionDto.ActionAttribute.EmAppCommandLoggingPreference.HasValue)
                        //{
                        //    actionDto.ActionAttribute.EmAppCommandLoggingPreference = EmAppCommandLoggingPreference.DoNotLog;
                        //}


                        InitChildCommandProperties(commandActionList, actionDto);

                        toReturn.Add(actionDto);
                    }
                }
                //}
            }

            aAppTransactionExDto.CommandActionList = toReturn;



            return toReturn;
        }

        private static void MergeWorkflowGlobalFieldWithFormularDisplayName(int? rootWorkflowTransactionId, List<AppTransactionFieldExDto> transactionFieldList)
        {
            if (rootWorkflowTransactionId.HasValue)
            {
                var rootWorkflowTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(rootWorkflowTransactionId.Value);
                if (rootWorkflowTransactionExDto.DictRootLevelUnitTransactionField != null)
                {
                    List<AppTransactionFieldExDto> rootWorkflowTransactionFieldList = rootWorkflowTransactionExDto.DictRootLevelUnitTransactionField.Values.ToList().DeepCopy();
                    AppTransactionFormulaSetupBL.InitialTransactionFieldFormularDisplayName(rootWorkflowTransactionExDto, rootWorkflowTransactionFieldList, true);
                    transactionFieldList.AddRange(rootWorkflowTransactionFieldList);
                }
            }
        }

        public static List<AppProjectWorkFlowActionDto> SyncronizeWorkflowCommandNodeTreeFromActionList(AppTransactionExDto aAppTransactionExDto)
        {

            if (aAppTransactionExDto.BusinessScopeId.HasValue && aAppTransactionExDto.BusinessScopeId.Value == (int)EmAppTransactionScopeUsage.WorkflowAutomation)
            {
                var workflowCommandList = aAppTransactionExDto.CommandActionList;

                var rootCommand = workflowCommandList.FirstOrDefault(o => o.ActionAttribute != null && o.ActionAttribute.IsWorkflowRootCommand);

                if (rootCommand != null)
                {
                    rootCommand.ProgressStatus = "";


                    var dictTransactionIdAndName = AppTransactionBL.RetrieveAllAppTransactionDto(false).ToDictionary(o => (int)o.Id, o => o.TransactionName);

                    PrepareOneWorkflowCommandTreeNode(workflowCommandList, rootCommand, dictTransactionIdAndName, aAppTransactionExDto.TransactionName);

                    aAppTransactionExDto.WorkflowCommandNodeTree = rootCommand.WorkflowChildTreeNodes;
                }
                else
                {
                    aAppTransactionExDto.WorkflowCommandNodeTree = new List<AppProjectWorkFlowActionDto>();
                }
            }

            return aAppTransactionExDto.WorkflowCommandNodeTree;
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

        public static bool CheckCommandCondition(AppMasterDetailDto aformData, AppTransactionExDto transactionExDto, AppProjectWorkFlowActionExDto commandDto, AppProjectWorkFlowActionExDto rootCommandDto)
        {
            if (transactionExDto.TransactionOrganizedType.HasValue && transactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.List)
            {
                return true;
            }

            bool isConditionAllowExecute = false;

            if (commandDto.CommandConditionTransactionFieldId.HasValue)
            {
                int conditionFieldId = commandDto.CommandConditionTransactionFieldId.Value;
                var dictRootFiedValie = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(aformData, transactionExDto); ;

                MergeRootCommandFieldValueToDictFieldIdAndValue(rootCommandDto, dictRootFiedValie);




                if (!dictRootFiedValie.IsEmpty() && dictRootFiedValie.ContainsKey(conditionFieldId))
                {
                    bool? conditionResult = ControlTypeValueConverter.ConvertValueToBoolean(dictRootFiedValie[conditionFieldId]);

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

        private static bool CheckIsAllowExecuteChildCommand(AppMasterDetailDto formData, AppProjectWorkFlowActionExDto commandDto, ChildTransactionCommandDto childTransactionCommandDto, AppProjectWorkFlowActionExDto rootCommandDto)
        {
            bool isAllowExecute = false;

            bool isHaveSwitchCondition = commandDto.ActionAttribute != null && commandDto.ActionAttribute.ChildCommandsSwitchConditionFieldId.HasValue && childTransactionCommandDto.PredictValue.HasValue;
            bool isHaveRootLevelFieldChangeTrigger = childTransactionCommandDto.ChangeTriggerRootLevelFieldId.HasValue;
            bool isHaveChildGridUnitChangeTrigger = childTransactionCommandDto.ChangeTriggerChildGridUnitId.HasValue;

            bool isSwitchCondtionMatch = false;
            bool isTriggerFieldValueChanged = false;
            bool isTriggerGridValueChanged = false;

            if (isHaveSwitchCondition)
            {
                isSwitchCondtionMatch = CheckChildCommandSwitchCondition(formData, commandDto, childTransactionCommandDto, rootCommandDto);
            }

            if (isHaveRootLevelFieldChangeTrigger)
            {
                isTriggerFieldValueChanged = CheckChildCommandTrigerFiledValueChanged(formData, commandDto, childTransactionCommandDto);
            }

            if (isHaveChildGridUnitChangeTrigger)
            {
                isTriggerGridValueChanged = CheckChildCommandTrigerGridValueChanged(formData, commandDto, childTransactionCommandDto);
            }



            if (!isHaveSwitchCondition && !isHaveRootLevelFieldChangeTrigger && !isHaveChildGridUnitChangeTrigger)
            {
                isAllowExecute = true;
            }
            else
            {
                if (isSwitchCondtionMatch || isTriggerFieldValueChanged || isTriggerGridValueChanged)
                {
                    isAllowExecute = true;
                }
            }

            return isAllowExecute;
        }



        public static bool CheckChildCommandSwitchCondition(AppMasterDetailDto aformData, AppProjectWorkFlowActionExDto commandDto, ChildTransactionCommandDto childTransactionCommandDto, AppProjectWorkFlowActionExDto rootCommandDto)
        {
            bool isAllowExecute = true;

            if (commandDto.ActionAttribute != null && commandDto.ActionAttribute.ChildCommandsSwitchConditionFieldId.HasValue && childTransactionCommandDto.PredictValue.HasValue)
            {
                isAllowExecute = false;

                var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aformData.TransactionId);

                int conditionFieldId = commandDto.ActionAttribute.ChildCommandsSwitchConditionFieldId.Value;
                var dictRootFiedValie = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(aformData, transactionExDto);

                MergeRootCommandFieldValueToDictFieldIdAndValue(rootCommandDto, dictRootFiedValie);

                if (!dictRootFiedValie.IsEmpty() && dictRootFiedValie.ContainsKey(conditionFieldId))
                {
                    int? formConditonValue = ControlTypeValueConverter.ConvertValueToInt(dictRootFiedValie[conditionFieldId]);

                    if (formConditonValue.HasValue && formConditonValue.Value == childTransactionCommandDto.PredictValue.Value)
                    {
                        isAllowExecute = true;
                    }
                }
            }

            return isAllowExecute;
        }

        public static bool CheckChildCommandTrigerFiledValueChanged(AppMasterDetailDto aformData, AppProjectWorkFlowActionExDto commandDto, ChildTransactionCommandDto childTransactionCommandDto)
        {
            bool isFieldValueChanged = false;

            if (commandDto.ActionAttribute != null && childTransactionCommandDto.ChangeTriggerRootLevelFieldId.HasValue)
            {



                int trgerFieldId = childTransactionCommandDto.ChangeTriggerRootLevelFieldId.Value;

                if (!aformData.DictRootAndSiblingChangedField.IsEmpty() && aformData.DictRootAndSiblingChangedField.ContainsKey(trgerFieldId))
                {
                    isFieldValueChanged = true;
                }

            }

            return isFieldValueChanged;
        }

        public static bool CheckChildCommandTrigerGridValueChanged(AppMasterDetailDto aformData, AppProjectWorkFlowActionExDto commandDto, ChildTransactionCommandDto childTransactionCommandDto)
        {
            bool iGridValueChanged = false;

            if (commandDto.ActionAttribute != null && childTransactionCommandDto.ChangeTriggerChildGridUnitId.HasValue)
            {

                int trgerUnitId = childTransactionCommandDto.ChangeTriggerChildGridUnitId.Value;

                if (!aformData.ChildChangedUnitIds.IsEmpty() && aformData.ChildChangedUnitIds.Contains(trgerUnitId))
                {
                    iGridValueChanged = true;
                }
            }

            return iGridValueChanged;
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

        public static void ExecuteCommand_CallApiDefaultPublish(OperationCallResult<TransactionCommandActionResultDto> toReturn)
        {
            toReturn.Object.FormData.IsDirty = true;

            var postSaveResult = AppMasterDetailApiFormDataSaveBL.PublishConsumApiTransactionDataByDataTransfer(toReturn.Object.FormData);

            if (postSaveResult != null)
            {
                if (postSaveResult.IsSuccessfulWithResult)
                {
                    if (postSaveResult.Object != null)
                    {
                        toReturn.Object.FormData = postSaveResult.Object;
                    }

                }
                else if (postSaveResult.ValidationResult.HasErrors)
                {
                    toReturn.ValidationResult.Merge(postSaveResult.ValidationResult);
                }
            }
        }

        public static void ExecuteCommand_CallApiOperation(OperationCallResult<TransactionCommandActionResultDto> toReturn, AppProjectWorkFlowActionExDto actionDto)
        {
            if (actionDto.DataTransferSettingId.HasValue)
            {

                toReturn.Object.FormData.IsDirty = true;
                AppMasterDetailApiFormDataSaveBL.CallApiOperationFromMasterDetailTransactionForm(toReturn.Object.FormData, toReturn.ValidationResult, actionDto.DataTransferSettingId.Value, actionDto);

            }
        }



        public static void ExecuteCommand_SaveListEdit(OperationCallResult<TransactionCommandActionResultDto> toReturn)
        {
            toReturn.Object.FormData.OrgAppListDataDto.IsDirty = true;

            var postSaveResult = AppListEditFormDataLoadBL.SaveListEditFormData(toReturn.Object.FormData.OrgAppListDataDto);

            if (postSaveResult != null)
            {
                if (postSaveResult.IsSuccessfulWithResult)
                {
                    toReturn.Object.FormData.OrgAppListDataDto = postSaveResult.Object;
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

        public static void ExecuteCommand_RefreshListEdit(OperationCallResult<TransactionCommandActionResultDto> toReturn, List<object> rootIdList = null)
        {
            var refreshedFormData = AppListEditFormDataLoadBL.GetListEditFormData(toReturn.Object.FormData.TransactionId, rootIdList);
            toReturn.Object.FormData.OrgAppListDataDto = refreshedFormData;
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

        public static void ExecuteCommand_OpenFormEditWindow(AppProjectWorkFlowActionExDto actionDto, OperationCallResult<TransactionCommandActionResultDto> toReturn)
        {
            toReturn.Object.CommandActionExDto = actionDto;

            if (actionDto.ActionAttribute.LinkTargetId.HasValue)
            {
                toReturn.Object.NeedToExecuteLinkTargetDto = LinkTragetBL.RetrieveOneAppFormLinkTargetDto(actionDto.ActionAttribute.LinkTargetId.Value);
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

        public static void ExecuteCommand_ValidateAndCalculateMasterDetailTransactionData(OperationCallResult<TransactionCommandActionResultDto> toReturn)
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

        public static void ExecuteCommand_MasterDetailDataFirstLevelValidation(OperationCallResult<TransactionCommandActionResultDto> toReturn)
        {
            var calculationResult = AppTransactionFormulaBL.MasterDetailDataFirstLevelValidation(toReturn.Object.FormData);

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



        public static void ExecuteCommand_DataTransfer(AppProjectWorkFlowActionExDto actionDto, OperationCallResult<TransactionCommandActionResultDto> toReturn, AppProjectWorkFlowActionExDto rootCommandDto, bool isNeedToSaveAfterTransfer = false)
        {
            OperationCallResult<AppMasterDetailDto> dataTransferResult = new OperationCallResult<AppMasterDetailDto>();
            ValidationResult aValidationResult = new ValidationResult();
            dataTransferResult.ValidationResult = aValidationResult;

            DataModelDateTimeConverterBL.ConvertMasterDetailFromClientToUtc(toReturn.Object.FormData);
            var tgtFormData = AppTransactionDataTransferBL.PrepareDataTransferFormData_FromMasterDetailToMasterDetail(toReturn.Object.FormData, actionDto.DataTransferSettingId);
            DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(toReturn.Object.FormData);

            if (isNeedToSaveAfterTransfer)
            {
                if (tgtFormData != null)
                {
                    dataTransferResult = AppMasterDetailFormDataSaveBL.SaveTransactionData(tgtFormData);
                }

                if (dataTransferResult.IsSuccessfulWithResult)
                {
                    var newTgtFormData = dataTransferResult.Object;
                    newTgtFormData.DataTransferFromMasterDetailDto = tgtFormData.DataTransferFromMasterDetailDto;
                    newTgtFormData.DataTransferSettingId = tgtFormData.DataTransferSettingId;

                    DataTransferCommand_ExecuteTargetTransactionCommand(actionDto, toReturn, newTgtFormData, rootCommandDto);



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
            else
            {
                if (tgtFormData != null)
                {
                    DataTransferCommand_ExecuteTargetTransactionCommand(actionDto, toReturn, tgtFormData, rootCommandDto);
                }
                else
                {
                    toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Error", ValidationItemType.Error, "Data Transfer Command Execution Failed."));
                }
            }

        }


        public static void ExecuteCommand_DataTransferFromListEdit(AppProjectWorkFlowActionExDto actionDto, OperationCallResult<TransactionCommandActionResultDto> toReturn, bool isNeedToSaveAfterTransfer = false)
        {
            OperationCallResult<AppMasterDetailDto> dataTransferResult = new OperationCallResult<AppMasterDetailDto>();
            ValidationResult aValidationResult = new ValidationResult();
            dataTransferResult.ValidationResult = aValidationResult;

            DataModelDateTimeConverterBL.ConvertMasterDetailFromClientToUtc(toReturn.Object.FormData);
            List<AppMasterDetailDto> tgtFormDataList = AppTransactionDataTransferBL.PrepareDataTransferFormData_FromListEditToMasterDetail(toReturn.Object.FormData.OrgAppListDataDto, actionDto.DataTransferSettingId);
            DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(toReturn.Object.FormData);

            if (isNeedToSaveAfterTransfer)
            {
                foreach (var tgtFormData in tgtFormDataList)
                {
                    if (tgtFormData != null)
                    {
                        dataTransferResult = AppMasterDetailFormDataSaveBL.SaveTransactionData(tgtFormData);
                    }

                    if (dataTransferResult.IsSuccessfulWithResult)
                    {
                        var newTgtFormData = dataTransferResult.Object;
                        newTgtFormData.DataTransferFromMasterDetailDto = tgtFormData.DataTransferFromMasterDetailDto;
                        newTgtFormData.DataTransferSettingId = tgtFormData.DataTransferSettingId;

                        DataTransferCommand_ExecuteTargetTransactionCommand(actionDto, toReturn, newTgtFormData, null);

                        if (actionDto.ActionAttribute != null)
                        {
                            toReturn.Object.IsNeedToOpenTransactionForm = actionDto.ActionAttribute.IsNeedToOpenNewForm;
                        }

                        //toReturn.Object.FormTitleDisplay = dataTransferResult.Object.FormTitleDisplay;
                        //toReturn.Object.TransactionId = dataTransferResult.Object.TransactionId;
                        //toReturn.Object.TransactionRId = dataTransferResult.Object.RootPrimaryKeyValue;
                    }
                    else
                    {
                        toReturn.ValidationResult.Merge(dataTransferResult.ValidationResult);
                    }
                }
            }
            else
            {
                foreach (var tgtFormData in tgtFormDataList)
                {
                    if (tgtFormData != null)
                    {
                        DataTransferCommand_ExecuteTargetTransactionCommand(actionDto, toReturn, tgtFormData, null);
                    }
                    else
                    {
                        toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Error", ValidationItemType.Error, "Data Transfer Command Execution Failed."));
                    }
                }
            }

        }



        private static void DataTransferCommand_ExecuteTargetTransactionCommand(AppProjectWorkFlowActionExDto actionDto, OperationCallResult<TransactionCommandActionResultDto> toReturn, AppMasterDetailDto tgtFormData, AppProjectWorkFlowActionExDto rootCommandDto)
        {
            if (actionDto.ActionAttribute != null && actionDto.ActionAttribute.TargetTransactionCommandId.HasValue)
            {
                tgtFormData.TransactionCommandId = actionDto.ActionAttribute.TargetTransactionCommandId.Value;
                DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(tgtFormData);
                var targetForm_commandExeResult = ExcuteOneChildCommonad(tgtFormData, null, null, false, rootCommandDto, null);

                if (targetForm_commandExeResult.IsSuccessfulWithResult & targetForm_commandExeResult.Object.FormData != null)
                {
                    var newTargetFormData = targetForm_commandExeResult.Object.FormData;
                    newTargetFormData.DataTransferFromMasterDetailDto = tgtFormData.DataTransferFromMasterDetailDto;


                    if (newTargetFormData.RootPrimaryKeyValue != null)
                    {
                        toReturn.Object.TransactionId = newTargetFormData.TransactionId;
                        toReturn.Object.TransactionRId = newTargetFormData.RootPrimaryKeyValue;
                    }

                    if (newTargetFormData.DataTransferFromMasterDetailDto != null)
                    {
                        toReturn.Object.FormData = newTargetFormData.DataTransferFromMasterDetailDto;
                        DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(toReturn.Object.FormData);

                        if (actionDto.ActionAttribute.CallBackCommandID.HasValue)
                        {
                            toReturn.Object.FormData.TransactionCommandId = actionDto.ActionAttribute.CallBackCommandID;

                            var callback_commandExeResult = ExcuteOneChildCommonad(toReturn.Object.FormData, null, null, false, null, null);

                            if (callback_commandExeResult.IsSuccessfulWithResult && callback_commandExeResult.Object.FormData != null)
                            {
                                callback_commandExeResult.Object.FormData.DataTransferFromMasterDetailDto = toReturn.Object.FormData.DataTransferFromMasterDetailDto;
                                callback_commandExeResult.Object.FormData.DataTransferFromListEditRowDto = toReturn.Object.FormData.DataTransferFromListEditRowDto;
                                toReturn.Object.FormData = callback_commandExeResult.Object.FormData;


                            }

                        }
                    }
                    else if (newTargetFormData.DataTransferFromListEditRowDto != null)
                    {
                        if (actionDto.ActionAttribute.CallBackCommandID.HasValue)
                        {
                            toReturn.Object.FormData.TransactionCommandId = actionDto.ActionAttribute.CallBackCommandID;

                            var callback_commandExeResult = ExcuteOneChildCommonad(toReturn.Object.FormData, null, null, false, null, null);

                            //if (callback_commandExeResult.IsSuccessfulWithResult && callback_commandExeResult.Object.FormData != null)
                            //{
                            //    toReturn.Object.FormData = callback_commandExeResult.Object.FormData;
                            //}

                        }
                    }
                    else
                    {
                        toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Error", ValidationItemType.Error, "Refresh Data Transfer Source Form Failed."));
                    }
                }
                else
                {
                    toReturn.ValidationResult.Merge(targetForm_commandExeResult.ValidationResult);
                }
            }
        }

        public static void ExecuteCommand_PluginWebApiCall(AppProjectWorkFlowActionExDto actionDto, OperationCallResult<TransactionCommandActionResultDto> toReturn)
        {
            var aAppTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(toReturn.Object.FormData.TransactionId);

            if (actionDto.ActionAttribute != null && !string.IsNullOrWhiteSpace(actionDto.ActionAttribute.WebApiMethodName))
            {

                string restResourceUri = actionDto.ActionAttribute.WebApiMethodName.Trim();

                OperationCallResult<AppMasterDetailDto> result = AppPluginClient.CallTransactionFormExternalService(toReturn.Object.FormData, restResourceUri);
                toReturn.IsForcedToUpdateUI = result.IsForcedToUpdateUI;

                if (result != null)
                {
                    toReturn.ValidationResult.Merge(result.ValidationResult);

                    if (result.ValidationResult.HasErrors)
                    {
                        if (result.IsForcedToUpdateUI)
                        {
                            toReturn.Object.FormData = result.Object;

                        }
                        //toReturn.ValidationResult.Merge(result.ValidationResult);
                    }
                    else
                    {
                        toReturn.Object.FormData = result.Object;
                    }
                }
            }
        }

        public static void ExecuteCommand_IntegrationWebApiCall(AppProjectWorkFlowActionExDto actionDto, OperationCallResult<TransactionCommandActionResultDto> toReturn, bool isIntegrationApi = false)
        {
            var aAppTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(toReturn.Object.FormData.TransactionId);

            if (actionDto.ActionAttribute != null && !string.IsNullOrWhiteSpace(actionDto.ActionAttribute.WebApiMethodName))
            {
                int? actionId = ControlTypeValueConverter.ConvertValueToInt(actionDto.ActionAttribute.WebApiMethodName);
                if (actionId.HasValue)
                {
                    //string baseUrl = @"https://cgsquebec-test.myshopify.com/admin";
                    //string userName = "f85860ebaef2960525b3863bcb7dd92e";
                    //string password = "<SHOPIFY_API_SECRET_REDACTED>";

                    //string base64 = Convert.ToBase64String(Encoding.Default.GetBytes($"{userName}:{password}"));
                    //APIConfigParameterDTO apiDTO = new APIConfigParameterDTO() { BaseUrl = baseUrl, Method = EnumHttpMethod.Get, PostProcessMethodName = "Shopify" };

                    //apiDTO.Headers.Add("Authorization", $"Basic {base64}");

                    if (toReturn.Object != null && toReturn.Object.FormData != null && toReturn.Object.FormData.DictRootAndSiblingFieldValue == null)
                    {
                        var dictOneRowFiedIdValue = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(toReturn.Object.FormData, aAppTransactionExDto);
                        toReturn.Object.FormData.DictRootAndSiblingFieldValue = dictOneRowFiedIdValue;
                    }



                    AppIntergrationSettingParameterExDto operationDto = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(actionId.Value);
                    if (operationDto.HttpMethd == "Get")
                    {
                        JsonSchemaBL.FromAPIToDatabaseByParametersAsync(actionId.Value, null, null, null).Wait();
                    }
                    else if (operationDto.HttpMethd == "Post" || operationDto.HttpMethd == "Put")
                    {

                        int? internalKeyId = ControlTypeValueConverter.ConvertValueToInt(toReturn.Object.FormData.RootPrimaryKeyValue);


                        if (internalKeyId.HasValue)
                        {
                            Dictionary<string, string> queryParams = new Dictionary<string, string>();
                            Dictionary<string, string> pathParams = new Dictionary<string, string>();

                            if (actionDto.ActionAttribute.PathParamMappingList != null)
                            {
                                foreach (var keyValuePair in actionDto.ActionAttribute.PathParamMappingList)
                                {
                                    int? transFieldId = keyValuePair.Value;
                                    if (transFieldId.HasValue)
                                    {
                                        string fieldValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(toReturn.Object.FormData.DictRootAndSiblingFieldValue[transFieldId.Value]);

                                        pathParams.Add(keyValuePair.Key, fieldValue);
                                    }

                                }
                            }

                            if (actionDto.ActionAttribute.QueryParamMappingList != null)
                            {
                                foreach (var keyValuePair in actionDto.ActionAttribute.QueryParamMappingList)
                                {
                                    int? transFieldId = keyValuePair.Value;
                                    if (transFieldId.HasValue)
                                    {
                                        string fieldValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(toReturn.Object.FormData.DictRootAndSiblingFieldValue[transFieldId.Value]);

                                        queryParams.Add(keyValuePair.Key, fieldValue);
                                    }

                                }
                            }

                            try
                            {
                                JsonSchemaBL.FromDatabaseToAPIAsync(actionId.Value, null, queryParams, pathParams, internalKeyId.Value).Wait();
                            }
                            catch (Exception ex)
                            {
                                toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Condition pass failure", ValidationItemType.Error, ex.ToString()));
                            }

                        }

                    }



                }

            }
            //if (actionDto.ActionAttribute != null 
            //    && actionDto.ActionAttribute.IntegrationActionMappingDtoList != null
            //    && actionDto.ActionAttribute.IntegrationActionMappingDtoList.Count > 0)
            //{
            //    foreach (IntegrationActionMappingDto mappingDto in actionDto.ActionAttribute.IntegrationActionMappingDtoList)
            //    {
            //        JsonSchemaBL.FromAPIToDatabaseByParametersAsync(mappingDto.ActionId.Value, null, null, null).Wait();
            //    }                
            //}
        }



        //public static void ExecuteCommand_DetectOneTransactionFieldIsValueChangedBeforeSaveDB(AppProjectWorkFlowActionExDto actionDto, OperationCallResult<TransactionCommandActionResultDto> toReturn)
        //{
        //    if (actionDto.ActionAttribute != null
        //        && actionDto.ActionAttribute.NeedToDetectChangeTransactionFieldId.HasValue
        //        && actionDto.ActionAttribute.DetectedChangeSaveToTransactionFieldId.HasValue)
        //    {
        //        var formData = toReturn.Object.FormData;
        //        var aAppTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(formData.TransactionId);


        //        int needToDetectFieldId = actionDto.ActionAttribute.NeedToDetectChangeTransactionFieldId.Value;
        //        int resultTransFieldId = actionDto.ActionAttribute.DetectedChangeSaveToTransactionFieldId.Value;


        //        if (aAppTransactionExDto.DictAllTransactionField.ContainsKey(resultTransFieldId))
        //        {
        //            int isValueChanged = 2; //Yes = 1, no = 2

        //            var formDataDB = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(formData.TransactionId, formData.RootPrimaryKeyValue);

        //            var dictFieldIdAndValue = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(formData, aAppTransactionExDto);
        //            var dictFieldIdAndValue_DB = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(formDataDB, aAppTransactionExDto);

        //            if (dictFieldIdAndValue.ContainsKey(needToDetectFieldId)
        //               && dictFieldIdAndValue_DB.ContainsKey(resultTransFieldId))
        //            {
        //                string valueNow = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dictFieldIdAndValue[needToDetectFieldId]);
        //                string valueDB = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dictFieldIdAndValue_DB[needToDetectFieldId]);

        //                if (valueNow != valueDB)
        //                {
        //                    isValueChanged = 1; //Yes = 1, no = 2
        //                }
        //            }

        //            var resultTransFieldExDto = aAppTransactionExDto.DictAllTransactionField[resultTransFieldId];

        //            if (resultTransFieldExDto.IsSiblingField)
        //            {
        //                formData.DictSiblingOneToOneFields[resultTransFieldExDto.TransactionUnitId.ToString()][resultTransFieldExDto.DataBaseFieldName] = isValueChanged;
        //            }
        //            else
        //            {
        //                formData.DictOneToOneFields[resultTransFieldExDto.DataBaseFieldName] = isValueChanged;
        //            }

        //        }
        //    }

        //}










        // Input: Client DateTime
        // Output: Client DateTime
        private static OperationCallResult<TransactionCommandActionResultDto> ExecuteCompositionCommand(AppProjectWorkFlowActionExDto actionDto, AppMasterDetailDto aformData, int? chlldUnitId = null, string childRowPkCombString = null, AppProjectWorkFlowActionExDto rootCommandDto = null)
        {
            OperationCallResult<TransactionCommandActionResultDto> toReturn = new OperationCallResult<TransactionCommandActionResultDto>();
            toReturn.ValidationResult = new ValidationResult();

            var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aformData.TransactionId);

            toReturn.Object = new TransactionCommandActionResultDto();
            toReturn.Object.CommandId = ControlTypeValueConverter.ConvertValueToInt(actionDto.Id);
            toReturn.Object.ActionTypeId = actionDto.ActionType;
            toReturn.Object.FormData = aformData;


            if (actionDto.ActionAttribute != null && actionDto.ActionAttribute.ChildActionList != null)
            {
                bool isBatchCommand = actionDto.ActionAttribute != null && actionDto.ActionAttribute.IsBatchCommand && actionDto.ActionAttribute.BatchCommandSourceFromType.HasValue
                    && actionDto.ActionAttribute.BatchCommandSourceFromType.Value == (int)EmAppBatchCommandSourceFrom.ChildUnit
                    && actionDto.ActionAttribute.ForeachLoopSourceUnitId.HasValue
                    && (transactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.MasterDetail);

                if (isBatchCommand)
                {
                    ExecuteCompositionBatchCommand_LoopOnChildUnit(actionDto, aformData, chlldUnitId, childRowPkCombString, rootCommandDto, toReturn);
                }
                else
                {
                    ExecuteCompositoinCommandChildCommands(actionDto, chlldUnitId, childRowPkCombString, toReturn, rootCommandDto);
                }
            }

            if (toReturn.IsSuccessful)
            {
                BuildCommandSuccessMessage(actionDto, toReturn);
            }

            return toReturn;
        }


        private static void ExecuteCompositoinCommandChildCommands(AppProjectWorkFlowActionExDto compositoinCommand, int? chlldUnitId, string childRowPkCombString, OperationCallResult<TransactionCommandActionResultDto> toReturn, AppProjectWorkFlowActionExDto rootCommandDto)
        {
            toReturn.Object.ChildCommandResultDtoList = new List<TransactionCommandActionResultDto>();

            int childCount = 0;
            var childComds = compositoinCommand.ActionAttribute.ChildActionList.Where(o => o.CommandId.HasValue && (!string.IsNullOrEmpty(o.CommandDisplay)))
                .OrderBy(o => o.Sort).ToList();

            foreach (ChildTransactionCommandDto childTransactionCommandDto in childComds)
            {
                childCount++;
                rootCommandDto.CurrentExecutingTreeNodeLogicId = compositoinCommand.TreeNodeLogicId + "|" + (childCount + "_" + childTransactionCommandDto.CommandId.Value);

                if (childTransactionCommandDto.IsSkip)
                {
                    if (rootCommandDto != null && rootCommandDto.IsWorkflowAutomationRootCommand)
                    {
                        var treeNodeDto = FindOneWorkflowCommandTreeNodeByLogicId(rootCommandDto.WorkflowCommandNodeTree, rootCommandDto.CurrentExecutingTreeNodeLogicId);

                        if (treeNodeDto != null)
                        {
                            treeNodeDto.ProgressStatus = EmAppCommandProgressStatus.Ignored.ToString();
                            treeNodeDto.ErrorMessage = "Command is set to skip.";

                            UpdateCurrentExecutingCommandAppBatchLogRuntimeData(rootCommandDto.CurrentExecutingTreeNodeLogicId, rootCommandDto);
                        }
                    }

                    continue;
                }
                else
                {

                    bool isAllowExecuteChildCommand = CheckIsAllowExecuteChildCommand(toReturn.Object.FormData, compositoinCommand, childTransactionCommandDto, rootCommandDto);

                    if (isAllowExecuteChildCommand)
                    {
                        if (childTransactionCommandDto.ExternalTransactionId.HasValue)
                        {
                            var externalCommandResult = AppTransactionCommandBL.ExecuteOneChildCommonadById(
                                childTransactionCommandDto.CommandId.Value, rootCommandDto,
                                childTransactionCommandDto.ExternalTransactionId.Value,
                                "", null, null, null, false, childTransactionCommandDto.IsGoToNextCommandWithError);

                            if (externalCommandResult != null)
                            {
                                if (externalCommandResult.ValidationResult.HasErrors)
                                {
                                    externalCommandResult.ValidationResult.Items.ForAll(o =>
                                    {
                                        if (!o.CommandId.HasValue)
                                        {
                                            o.CommandId = childTransactionCommandDto.CommandId.Value;
                                        }

                                        if (!o.TransactionId.HasValue)
                                        {
                                            o.TransactionId = childTransactionCommandDto.ExternalTransactionId.Value;
                                        }
                                    });

                                    externalCommandResult.ValidationResult.Items.ForAll(o => o.IsChildCommandValidationItem = true);

                                    toReturn.ValidationResult.Merge(externalCommandResult.ValidationResult);

                                    if (childTransactionCommandDto.IsGoToNextCommandWithError.HasValue
                                        && childTransactionCommandDto.IsGoToNextCommandWithError.Value)
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            toReturn.Object.FormData.TransactionCommandId = childTransactionCommandDto.CommandId.Value;


                            var aChlidCommandResult = ExcuteOneChildCommonad(toReturn.Object.FormData, chlldUnitId, childRowPkCombString, false, rootCommandDto, childTransactionCommandDto.IsGoToNextCommandWithError);
                            if (aChlidCommandResult != null)
                            {
                                aChlidCommandResult.ValidationResult.Items.ForAll(o => o.IsChildCommandValidationItem = true);
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
                                    //if (aChlidCommandResult.ValidationResult.HasErrors)
                                    //{
                                    if (childTransactionCommandDto.IsGoToNextCommandWithError.HasValue
                                        && childTransactionCommandDto.IsGoToNextCommandWithError.Value)
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                    //}                            

                                }
                            }
                        }
                    }
                    else
                    {
                        if (rootCommandDto != null && rootCommandDto.IsWorkflowAutomationRootCommand)
                        {
                            var treeNodeDto = FindOneWorkflowCommandTreeNodeByLogicId(rootCommandDto.WorkflowCommandNodeTree, rootCommandDto.CurrentExecutingTreeNodeLogicId);

                            if (treeNodeDto != null)
                            {
                                treeNodeDto.ProgressStatus = EmAppCommandProgressStatus.Ignored.ToString();
                                treeNodeDto.ErrorMessage = "Command execute condition is not ready.";

                                UpdateCurrentExecutingCommandAppBatchLogRuntimeData(rootCommandDto.CurrentExecutingTreeNodeLogicId, rootCommandDto);
                            }
                        }
                    }
                }
            }
        }

        private static void ExecuteCompositionBatchCommand_LoopOnChildUnit(AppProjectWorkFlowActionExDto actionDto, AppMasterDetailDto aformData, int? chlldUnitId, string childRowPkCombString, AppProjectWorkFlowActionExDto rootCommandDto, OperationCallResult<TransactionCommandActionResultDto> toReturn)
        {
            int loopUnitId = actionDto.ActionAttribute.ForeachLoopSourceUnitId.Value;
            aformData.CommandForeachLoopChildUnitId = loopUnitId;

            if (aformData.DictOneToManyFields.ContainsKey(loopUnitId.ToString()))
            {
                var loopRowDataList = aformData.DictOneToManyFields[loopUnitId.ToString()];

                int loopCount = 0;
                foreach (AppChildDataDto rowData in loopRowDataList)
                {
                    loopCount++;
                    string batchLoopCountDispay = loopCount.ToString() + "/" + loopRowDataList.Count.ToString();
                    if (rowData != null)
                    {
                        aformData.CommandForeachLoopToRowData = rowData;

                        ExecuteCompositoinCommandChildCommands(actionDto, chlldUnitId, childRowPkCombString, toReturn, rootCommandDto);
                    }
                }
            }
        }

        private static void ExecuteSingleBatchCommand_LoopOnChildUnit(AppProjectWorkFlowActionExDto actionDto, AppMasterDetailDto aInputFormData, AppTransactionExDto transactionExDto, OperationCallResult<TransactionCommandActionResultDto> toReturn, AppProjectWorkFlowActionExDto rootCommandDto)
        {
            int loopUnitId = actionDto.ActionAttribute.ForeachLoopSourceUnitId.Value;
            aInputFormData.CommandForeachLoopChildUnitId = loopUnitId;

            if (aInputFormData.DictOneToManyFields.ContainsKey(loopUnitId.ToString()))
            {
                var loopRowDataList = aInputFormData.DictOneToManyFields[loopUnitId.ToString()];

                foreach (AppChildDataDto rowData in loopRowDataList)
                {
                    if (rowData != null)
                    {
                        aInputFormData.CommandForeachLoopToRowData = rowData;

                        ExecuteSingleCommandByActionType_MasterDetailTransaction(actionDto, aInputFormData, transactionExDto, toReturn, rootCommandDto);
                    }
                }
            }
        }


        private static void BuildCommandSuccessMessage(AppProjectWorkFlowActionExDto actionDto, OperationCallResult<TransactionCommandActionResultDto> toReturn)
        {
            if (!string.IsNullOrWhiteSpace(actionDto.ActionAttribute.CommandSuccessMessage))
            {
                string messageTempalte = actionDto.ActionAttribute.CommandSuccessMessage;

                string currentDatetime = DateTime.UtcNow.ToShortDateString() + " " + DateTime.UtcNow.ToShortTimeString();
                string currentUserName = AppSecurityUserBL.CurrentUserEntity.UserName;
                string workflowName = string.Empty;
                string taskName = string.Empty;
                string taskStatus = string.Empty;
                string toList = string.Empty;

                AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(toReturn.Object.FormData.TransactionId);
                Dictionary<int, object> allFreshRootValue = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(toReturn.Object.FormData, transactionExDto);


                object rootKeyValue = toReturn.Object.FormData.RootPrimaryKeyValue;
                string message = AppProjectWorkFlowDataFormSynchBL.ReplaceMessageTemplatePlaceHolderWithActualValue(rootKeyValue, allFreshRootValue, messageTempalte, currentDatetime, currentUserName, workflowName, taskName, taskStatus, transactionExDto, null, null, false);

                toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "CommandMessage", ValidationItemType.Message, message));

            }
        }

        private static void ExecuteCommand_SendMessageToTransFieldEmailAddress(AppMasterDetailDto appformDataDto, AppProjectWorkFlowActionExDto action)
        {
            List<AppMessageDto> messageList = new List<AppMessageDto>();



            if (appformDataDto != null
                && (
                    !string.IsNullOrWhiteSpace(action.NotificationSubject) && !string.IsNullOrWhiteSpace(action.NotificationMessage)
                    || action.MessageTemplateId.HasValue
                ))
            {

                AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appformDataDto.TransactionId);

                int? emailFieldId = action.ActionAttribute.NotificationDestinationEmailAddressTransactionFiledId;

                if (emailFieldId.HasValue && appTransactionExDto.DictAllTransactionField.ContainsKey(emailFieldId.Value))
                {
                    var transfieldDto = appTransactionExDto.DictAllTransactionField[emailFieldId.Value];
                    int unitId = transfieldDto.TransactionUnitId;

                    Dictionary<string, string> dictEmailAndSmsText = new Dictionary<string, string>();

                    List<string> emailList = new List<string>();

                    if (appformDataDto.RootUnitId.ToString() == unitId.ToString())
                    {
                        string email = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(appformDataDto.DictOneToOneFields[transfieldDto.DataBaseFieldName]);

                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            if (!emailList.Contains(email.ToLower()))
                            {
                                emailList.Add(email);
                            }
                        }

                    }
                    else if (appformDataDto.DictSiblingOneToOneFields.ContainsKey(unitId.ToString()))
                    {
                        string email = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(appformDataDto.DictSiblingOneToOneFields[unitId.ToString()][transfieldDto.DataBaseFieldName]);

                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            if (!emailList.Contains(email.ToLower()))
                            {
                                emailList.Add(email);
                            }
                        }
                    }
                    else if (appformDataDto.DictOneToManyFields.ContainsKey(unitId.ToString()))
                    {
                        var unitDto = appTransactionExDto.DictAllTransactionUnitIdExDto[unitId.ToString()];

                        AppProjectWorkFlowDataFormSynchBL.SetChildRowPKValueCombinString(unitDto, appformDataDto.DictOneToManyFields[unitId.ToString()]);

                        foreach (AppChildDataDto childRowData in appformDataDto.DictOneToManyFields[unitId.ToString()])
                        {
                            string email = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(childRowData.DictOneToOneFields[transfieldDto.DataBaseFieldName]);

                            if (!string.IsNullOrWhiteSpace(email))
                            {
                                if (!emailList.Contains(email.ToLower()))
                                {
                                    emailList.Add(email);
                                }
                            }
                        }
                    }

                    PrepareEmailListMessage(appformDataDto, action, messageList, appTransactionExDto, emailList);
                }
            }

            SendCommandMessages(messageList);

        }

        private static void ExecuteCommand_SendMessageToTransFieldUserId(AppMasterDetailDto appformDataDto, AppProjectWorkFlowActionExDto action)
        {
            List<AppMessageDto> messageList = new List<AppMessageDto>();

            if (appformDataDto != null
                && (
                    !string.IsNullOrWhiteSpace(action.NotificationSubject) && !string.IsNullOrWhiteSpace(action.NotificationMessage)
                    || action.MessageTemplateId.HasValue
                ))
            {
                AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appformDataDto.TransactionId);

                if (action.ActionAttribute.NotificationDestinationPartnerIdTransactionFiledId.HasValue)
                {
                    int? userIdFieldId = action.ActionAttribute.NotificationDestinationPartnerIdTransactionFiledId;

                    if (userIdFieldId.HasValue && appTransactionExDto.DictAllTransactionField.ContainsKey(userIdFieldId.Value))
                    {
                        var transfieldDto = appTransactionExDto.DictAllTransactionField[userIdFieldId.Value];
                        int unitId = transfieldDto.TransactionUnitId;

                        List<int> userIds = new List<int>();

                        if (appformDataDto.RootUnitId.ToString() == unitId.ToString())
                        {
                            AppChildDataDto childRowData = null;

                            int? userId = ControlTypeValueConverter.ConvertValueToInt(appformDataDto.DictOneToOneFields[transfieldDto.DataBaseFieldName]);
                            if (userId.HasValue)
                            {
                                userIds.Add(userId.Value);

                            }
                        }
                        else if (appformDataDto.DictSiblingOneToOneFields.ContainsKey(unitId.ToString()))
                        {
                            AppChildDataDto childRowData = null;

                            int? userId = ControlTypeValueConverter.ConvertValueToInt(appformDataDto.DictSiblingOneToOneFields[unitId.ToString()][transfieldDto.DataBaseFieldName]);
                            if (userId.HasValue)
                            {
                                userIds.Add(userId.Value);

                            }
                        }
                        else if (appformDataDto.DictOneToManyFields.ContainsKey(unitId.ToString()))
                        {
                            var unitDto = appTransactionExDto.DictAllTransactionUnitIdExDto[unitId.ToString()];

                            AppProjectWorkFlowDataFormSynchBL.SetChildRowPKValueCombinString(unitDto, appformDataDto.DictOneToManyFields[unitId.ToString()]);

                            foreach (AppChildDataDto childRowData in appformDataDto.DictOneToManyFields[unitId.ToString()])
                            {
                                int? userId = ControlTypeValueConverter.ConvertValueToInt(childRowData.DictOneToOneFields[transfieldDto.DataBaseFieldName]);
                                if (userId.HasValue)
                                {
                                    userIds.Add(userId.Value);

                                }
                            }
                        }

                        userIds = userIds.Distinct().ToList();
                        PrepareOneUserIdFieldMessage(appformDataDto, action, messageList, appTransactionExDto, userIds);
                    }
                }

            }

            SendCommandMessages(messageList);
        }

        private static void ExecuteCommand_SendMessageToTransFieldPartnerId(AppMasterDetailDto appformDataDto, AppProjectWorkFlowActionExDto action)
        {
            List<AppMessageDto> messageList = new List<AppMessageDto>();

            if (appformDataDto != null
                && (
                    !string.IsNullOrWhiteSpace(action.NotificationSubject) && !string.IsNullOrWhiteSpace(action.NotificationMessage)
                    || action.MessageTemplateId.HasValue
                ))
            {
                AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appformDataDto.TransactionId);

                if (action.ActionAttribute.NotificationDestinationPartnerIdTransactionFiledId.HasValue)
                {
                    int? partnerIdFieldId = action.ActionAttribute.NotificationDestinationPartnerIdTransactionFiledId;

                    if (partnerIdFieldId.HasValue && appTransactionExDto.DictAllTransactionField.ContainsKey(partnerIdFieldId.Value))
                    {
                        var transfieldDto = appTransactionExDto.DictAllTransactionField[partnerIdFieldId.Value];
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
                else if (action.ActionAttribute.ToEmailAddress.HasValue())
                {
                    PrepareOnePartnerFieldMessage(appformDataDto, action, messageList, appTransactionExDto, null, null);
                }
            }

            SendCommandMessages(messageList);
        }



        private static void PrepareEmailListMessage(AppMasterDetailDto appformDataDto, AppProjectWorkFlowActionExDto action, List<AppMessageDto> messageList, AppTransactionExDto appTransactionExDto, List<string> emailList)
        {
            if (emailList != null && emailList.Count > 0)
            {


                string messageTempalte = action.NotificationMessage;

                string currentDatetime = DateTime.UtcNow.ToShortDateString() + " " + DateTime.UtcNow.ToShortTimeString();
                string currentUserName = AppSecurityUserBL.CurrentUserEntity.UserName;
                string workflowName = string.Empty;
                string taskName = string.Empty;
                string taskStatus = string.Empty;
                string toList = string.Empty;

                foreach (string email in emailList)
                {
                    toList += email.Trim() + ";";

                }
                Dictionary<int, object> allFreshRootValue = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(appformDataDto, appTransactionExDto);

                if (!(action.ActionAttribute != null && action.ActionAttribute.IsUseRichTextMessageTemplate))
                {
                    action.MessageTemplateId = null;
                }


                object rootKeyValue = appformDataDto.RootPrimaryKeyValue;
                string subject = AppProjectWorkFlowDataFormSynchBL.ReplaceMessageTemplatePlaceHolderWithActualValue(rootKeyValue, allFreshRootValue, action.NotificationSubject, currentDatetime, currentUserName, workflowName, taskName, taskStatus, appTransactionExDto, null, action.MessageTemplateId, true);
                string messageBody = AppProjectWorkFlowDataFormSynchBL.ReplaceMessageTemplatePlaceHolderWithActualValue(rootKeyValue, allFreshRootValue, action.NotificationMessage, currentDatetime, currentUserName, workflowName, taskName, taskStatus, appTransactionExDto, null, action.MessageTemplateId, false);





                var messageDto = new AppMessageDto();
                messageDto.ToList = toList;

                messageDto.ToUserIdList = new List<int>();

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

        private static void PrepareOneUserIdFieldMessage(AppMasterDetailDto appformDataDto, AppProjectWorkFlowActionExDto action, List<AppMessageDto> messageList, AppTransactionExDto appTransactionExDto, List<int> userIds)
        {
            if (userIds != null && userIds.Count > 0)
            {
                List<int> userIdList = userIds;

                string messageTempalte = action.NotificationMessage;

                string currentDatetime = DateTime.UtcNow.ToShortDateString() + " " + DateTime.UtcNow.ToShortTimeString();
                string currentUserName = AppSecurityUserBL.CurrentUserEntity.UserName;
                string workflowName = string.Empty;
                string taskName = string.Empty;
                string taskStatus = string.Empty;
                string toList = string.Empty;

                Dictionary<int, object> allFreshRootValue = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(appformDataDto, appTransactionExDto);

                if (!(action.ActionAttribute != null && action.ActionAttribute.IsUseRichTextMessageTemplate))
                {
                    action.MessageTemplateId = null;
                }


                object rootKeyValue = appformDataDto.RootPrimaryKeyValue;
                string subject = AppProjectWorkFlowDataFormSynchBL.ReplaceMessageTemplatePlaceHolderWithActualValue(rootKeyValue, allFreshRootValue, action.NotificationSubject, currentDatetime, currentUserName, workflowName, taskName, taskStatus, appTransactionExDto, null, action.MessageTemplateId, true);
                string messageBody = AppProjectWorkFlowDataFormSynchBL.ReplaceMessageTemplatePlaceHolderWithActualValue(rootKeyValue, allFreshRootValue, action.NotificationMessage, currentDatetime, currentUserName, workflowName, taskName, taskStatus, appTransactionExDto, null, action.MessageTemplateId, false);





                var messageDto = new AppMessageDto();
                messageDto.ToList = toList;
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


        private static void PrepareOnePartnerFieldMessage(AppMasterDetailDto appformDataDto, AppProjectWorkFlowActionExDto action, List<AppMessageDto> messageList, AppTransactionExDto appTransactionExDto, AppChildDataDto childRowData, int? partnerId)
        {
            List<AppSecurityUserDto> partnerUserList = partnerId.HasValue ? AppBusinessPartnerBL.RetrieveCurrentCompanyOneBusinessPartnerAllInviteUserDtoList(partnerId.Value) : new List<AppSecurityUserDto>();
            List<int> userIdList = partnerUserList.Select(o => (int)o.Id).Distinct().ToList();

            bool hasToEmailAddress = action.ActionAttribute != null && !string.IsNullOrWhiteSpace(action.ActionAttribute.ToEmailAddress);

            if (userIdList.Count > 0 || hasToEmailAddress)
            {
                string messageTempalte = action.NotificationMessage;

                string currentDatetime = DateTime.UtcNow.ToShortDateString() + " " + DateTime.UtcNow.ToShortTimeString();
                string currentUserName = AppSecurityUserBL.CurrentUserEntity.UserName;
                string workflowName = string.Empty;
                string taskName = string.Empty;
                string taskStatus = string.Empty;
                string toList = string.Empty;

                Dictionary<int, object> allFreshRootValue = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(appformDataDto, appTransactionExDto);

                if (!(action.ActionAttribute != null && action.ActionAttribute.IsUseRichTextMessageTemplate))
                {
                    action.MessageTemplateId = null;
                }

                if (hasToEmailAddress)
                {
                    toList = action.ActionAttribute.ToEmailAddress;
                }

                object rootKeyValue = appformDataDto.RootPrimaryKeyValue;
                string subject = AppProjectWorkFlowDataFormSynchBL.ReplaceMessageTemplatePlaceHolderWithActualValue(rootKeyValue, allFreshRootValue, action.NotificationSubject, currentDatetime, currentUserName, workflowName, taskName, taskStatus, appTransactionExDto, childRowData, action.MessageTemplateId, true);
                string messageBody = AppProjectWorkFlowDataFormSynchBL.ReplaceMessageTemplatePlaceHolderWithActualValue(rootKeyValue, allFreshRootValue, action.NotificationMessage, currentDatetime, currentUserName, workflowName, taskName, taskStatus, appTransactionExDto, childRowData, action.MessageTemplateId, false);

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
                messageDto.ToList = toList;
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


        private static void ExecuteCommand_SendSmsToTransFieldPhoneNumber(AppMasterDetailDto appformDataDto, AppProjectWorkFlowActionExDto action)
        {
            List<AppMessageDto> smsMessageList = new List<AppMessageDto>();

            if (!string.IsNullOrWhiteSpace((string)action.NotificationMessage)
                 && action.ActionAttribute.SmsMessageToPhoneNumberFiledId.HasValue)
            {
                AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appformDataDto.TransactionId);


                int? phoneNumberFieldId = action.ActionAttribute.SmsMessageToPhoneNumberFiledId;

                if (phoneNumberFieldId.HasValue && appTransactionExDto.DictAllTransactionField.ContainsKey(phoneNumberFieldId.Value))
                {
                    var transfieldDto = appTransactionExDto.DictAllTransactionField[phoneNumberFieldId.Value];
                    int unitId = transfieldDto.TransactionUnitId;

                    Dictionary<string, string> dictPhoneNumberAndSmsText = new Dictionary<string, string>();

                    if (appformDataDto.RootUnitId.ToString() == unitId.ToString())
                    {
                        string phoneNumber = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(appformDataDto.DictOneToOneFields[transfieldDto.DataBaseFieldName]);

                        if (!string.IsNullOrWhiteSpace(phoneNumber))
                        {
                            PrepareOnePhoneNumberTransFieldSmsMessage(appformDataDto, action, smsMessageList, appTransactionExDto, null, phoneNumber);
                        }

                    }
                    else if (appformDataDto.DictSiblingOneToOneFields.ContainsKey(unitId.ToString()))
                    {
                        AppChildDataDto childRowData = null;
                        string phoneNumber = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(appformDataDto.DictSiblingOneToOneFields[unitId.ToString()][transfieldDto.DataBaseFieldName]);

                        if (!string.IsNullOrWhiteSpace(phoneNumber))
                        {
                            PrepareOnePhoneNumberTransFieldSmsMessage(appformDataDto, action, smsMessageList, appTransactionExDto, childRowData, phoneNumber);
                        }
                    }
                    else if (appformDataDto.DictOneToManyFields.ContainsKey(unitId.ToString()))
                    {
                        var unitDto = appTransactionExDto.DictAllTransactionUnitIdExDto[unitId.ToString()];

                        AppProjectWorkFlowDataFormSynchBL.SetChildRowPKValueCombinString(unitDto, appformDataDto.DictOneToManyFields[unitId.ToString()]);

                        foreach (AppChildDataDto childRowData in appformDataDto.DictOneToManyFields[unitId.ToString()])
                        {
                            string phoneNumber = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(childRowData.DictOneToOneFields[transfieldDto.DataBaseFieldName]);

                            if (!string.IsNullOrWhiteSpace(phoneNumber))
                            {
                                PrepareOnePhoneNumberTransFieldSmsMessage(appformDataDto, action, smsMessageList, appTransactionExDto, childRowData, phoneNumber);
                            }
                        }
                    }
                }
            }

            SendSmsMessages(smsMessageList);
        }

        private static void ExecuteCommand_QuickCreateUser(AppMasterDetailDto appformDataDto, AppProjectWorkFlowActionExDto action, OperationCallResult<TransactionCommandActionResultDto> toReturn)
        {
            List<AppMessageDto> messageList = new List<AppMessageDto>();

            if (appformDataDto != null && action.ActionAttribute != null
                && action.ActionAttribute.UserNameTransFieldId.HasValue && action.ActionAttribute.UserPasswordTransFieldId.HasValue
                && action.ActionAttribute.UserEmailTransFieldId.HasValue && action.ActionAttribute.UserTypeTransFieldId.HasValue)
            {
                AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appformDataDto.TransactionId);

                int userTypeTransFieldId = action.ActionAttribute.UserTypeTransFieldId.Value;
                int userNameTransFieldId = action.ActionAttribute.UserNameTransFieldId.Value;
                int userEmailTransFieldId = action.ActionAttribute.UserEmailTransFieldId.Value;
                int userPasswordTransFieldId = action.ActionAttribute.UserPasswordTransFieldId.Value;

                bool isSuccess = false;

                if (appTransactionExDto.DictAllTransactionField.ContainsKey(userTypeTransFieldId)
                    && appTransactionExDto.DictAllTransactionField.ContainsKey(userNameTransFieldId)
                    && appTransactionExDto.DictAllTransactionField.ContainsKey(userEmailTransFieldId)
                    && appTransactionExDto.DictAllTransactionField.ContainsKey(userPasswordTransFieldId)
                    )
                {
                    int? userType = ControlTypeValueConverter.ConvertValueToInt(GetFormRootFieldValueByTransactionFieldId(appTransactionExDto, appformDataDto, userTypeTransFieldId));
                    string userName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(GetFormRootFieldValueByTransactionFieldId(appTransactionExDto, appformDataDto, userNameTransFieldId)); ;
                    string userEmail = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(GetFormRootFieldValueByTransactionFieldId(appTransactionExDto, appformDataDto, userEmailTransFieldId)); ;
                    string userPassword = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(GetFormRootFieldValueByTransactionFieldId(appTransactionExDto, appformDataDto, userPasswordTransFieldId)); ;

                    if (userType.HasValue && !string.IsNullOrWhiteSpace(userName)
                        && !string.IsNullOrWhiteSpace(userEmail) && !string.IsNullOrWhiteSpace(userPassword))
                    {
                        AppSecurityUserExDto newUserExDto = new AppSecurityUserExDto();
                        newUserExDto.DomainId = userType.Value;
                        newUserExDto.UserName = userName;
                        newUserExDto.LoginName = userName;
                        newUserExDto.Email = userEmail;
                        newUserExDto.Password = userPassword;
                        newUserExDto.ConfirmPassword = userPassword;
                        newUserExDto.IsActive = true;

                        if (userType.Value == (int)EmAppUserType.Customer
                            || userType.Value == (int)EmAppUserType.Supplier
                            || userType.Value == (int)EmAppUserType.ClientAgent
                            || userType.Value == (int)EmAppUserType.SupplierAgent)
                        {
                            newUserExDto.NewUserPartnerType = userType;
                            newUserExDto.NewUserPartnerName = userName;

                            var saveResult = AppSaasAccountUserBL.CreateNewPartnerAndDefaultUser(newUserExDto);

                            if (saveResult.IsSuccessfulWithResult)
                            {
                                toReturn.Object.FormData.RootPrimaryKeyValue = (int)saveResult.Object.Id;
                                toReturn.Object.FormData.DictOneToOneFields[appTransactionExDto.RootMasterUnit.PrimaryKeyDbfieldList.First()] = (int)saveResult.Object.Id;
                                isSuccess = true;
                            }
                            else
                            {
                                toReturn.ValidationResult.Merge(saveResult.ValidationResult);
                            }
                        }
                        else if (userType.Value == (int)EmAppUserType.Employee)
                        {
                            newUserExDto.IsInvitingCompanyUser = true;
                            newUserExDto.MyOwnCompnanyId = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentCompanyId);

                            var saveResult = AppSaasAccountUserBL.QuickCreateSaasUser(newUserExDto);

                            if (saveResult.IsSuccessfulWithResult)
                            {
                                toReturn.Object.FormData.RootPrimaryKeyValue = (int)saveResult.Object.Id;
                                toReturn.Object.FormData.DictOneToOneFields[appTransactionExDto.RootMasterUnit.PrimaryKeyDbfieldList.First()] = (int)saveResult.Object.Id;
                                isSuccess = true;
                            }
                            else
                            {
                                toReturn.ValidationResult.Merge(saveResult.ValidationResult);
                            }
                        }
                        else
                        {
                            toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "ExecuteCommand_QuickCreateUser_Error", ValidationItemType.Error, "Invalid User Type."));
                        }
                    }
                }


                if (!isSuccess)
                {
                    toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "ExecuteCommand_QuickCreateUser_Error", ValidationItemType.Error, "Creating new user failed."));
                }
            }
        }


        private static object GetFormRootFieldValueByTransactionFieldId(AppTransactionExDto appTransactionExDto, AppMasterDetailDto appformDataDto, int transFieldId)
        {
            var transfieldDto = appTransactionExDto.DictAllTransactionField[transFieldId];
            int unitId = transfieldDto.TransactionUnitId;

            if (appformDataDto.RootUnitId.ToString() == unitId.ToString())
            {
                return appformDataDto.DictOneToOneFields[transfieldDto.DataBaseFieldName];
            }
            else if (appformDataDto.DictSiblingOneToOneFields.ContainsKey(unitId.ToString()))
            {
                return appformDataDto.DictSiblingOneToOneFields[unitId.ToString()][transfieldDto.DataBaseFieldName];
            }

            return null;
        }

        private static void PrepareOnePhoneNumberTransFieldSmsMessage(AppMasterDetailDto appformDataDto, AppProjectWorkFlowActionExDto action, List<AppMessageDto> smsMessageList, AppTransactionExDto appTransactionExDto, AppChildDataDto childRowData, string phoneNumber)
        {
            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                string messageTempalte = action.NotificationMessage;

                string currentDatetime = DateTime.UtcNow.ToShortDateString() + " " + DateTime.UtcNow.ToShortTimeString();
                string currentUserName = AppSecurityUserBL.CurrentUserEntity.UserName;
                string workflowName = string.Empty;
                string taskName = string.Empty;
                string taskStatus = string.Empty;

                Dictionary<int, object> allFreshRootValue = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(appformDataDto, appTransactionExDto);

                object rootKeyValue = appformDataDto.RootPrimaryKeyValue;
                string subject = "SMS Message";
                string messageBody = AppProjectWorkFlowDataFormSynchBL.ReplaceMessageTemplatePlaceHolderWithActualValue(rootKeyValue, allFreshRootValue, action.NotificationMessage, currentDatetime, currentUserName, workflowName, taskName, taskStatus, appTransactionExDto, childRowData, action.MessageTemplateId, false);

                var messageDto = new AppMessageDto();
                messageDto.ToList = phoneNumber;
                //messageDto.MessgaeScopeType = (int)EmAppMessgaeScopeType.Transaction;
                //messageDto.TransactionId = appformDataDto.TransactionId;
                //messageDto.TransactionRootValueId = appformDataDto.RootPrimaryKeyValue.ToString();

                messageDto.Subject = subject;
                messageDto.Message = messageBody;

                //if (action.ActionAttribute.IsAttachAllFormFilesToMessage)
                //{
                //    messageDto.IsAttachAllFormFiles = true;
                //}


                smsMessageList.Add(messageDto);
            }
        }




        private static void ExecuteCommand_ExecutionFormula(OperationCallResult<TransactionCommandActionResultDto> toReturn, AppProjectWorkFlowActionExDto action, AppProjectWorkFlowActionExDto rootCommandDto)
        {

            var appformDataDto = toReturn.Object.FormData;

            if (appformDataDto != null && action.ActionAttribute != null && action.ActionAttribute.ExecutionFormula.HasValue())
            {
                AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appformDataDto.TransactionId);

                if (appTransactionExDto.TransactionOrganizedType.HasValue)
                {
                    if (appTransactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.MasterDetail)
                    {
                        var dictOneRowFiedIdValue = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(appformDataDto, appTransactionExDto);
                        MergeRootCommandFieldValueToDictFieldIdAndValue(rootCommandDto, dictOneRowFiedIdValue);


                        AppTransactionUnitFormulaExDto formula = new AppTransactionUnitFormulaExDto();
                        formula.FormulaExpression = action.ActionAttribute.ExecutionFormula;
                        formula.OperationType = (int)EmAppFormularType.Assignment;
                        AppTransactionExDto rootWorkflowTransaction = null;
                        AppMasterDetailDto rootWorkflowFrmData = null;

                        if (rootCommandDto != null && rootCommandDto.CommandTransactionId.HasValue)
                        {
                            rootWorkflowTransaction = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(rootCommandDto.CommandTransactionId.Value);
                            rootWorkflowFrmData = rootCommandDto.RootTransactionFormData;
                        }

                        AppTransactionFormulaBL.DoTransactionOneFormulaCalculation(appTransactionExDto, formula, dictOneRowFiedIdValue, null, null, rootWorkflowTransaction);

                        AppTransactionFormulaBL.UpdateRootAndSiblingDbFieldNameValueFromFiedIdValue(appformDataDto, appTransactionExDto, dictOneRowFiedIdValue, rootWorkflowFrmData);

                        appformDataDto.DictRootAndSiblingFieldValue = dictOneRowFiedIdValue;
                    }
                    else if (appTransactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.List)
                    {

                    }
                }


            }
        }

        private static void ExecuteCommand_ImportToDatabaseTableFromJson(OperationCallResult<TransactionCommandActionResultDto> toReturn, AppProjectWorkFlowActionExDto action)
        {
            if (action.ActionAttribute != null && action.ActionAttribute.JsonImportSettingId.HasValue && !string.IsNullOrWhiteSpace(action.ActionAttribute.FilePath))
            {
                int importSettingId = action.ActionAttribute.JsonImportSettingId.Value;


                if (action.ActionAttribute.FilePath.ToLower().StartsWith("http"))
                {
                    string url = action.ActionAttribute.FilePath;

                    var oneFileResult = AppIntergrationSettingBL.UpdateStagingTableDataFromJsonFilePath(importSettingId, url);

                    if (!oneFileResult.IsSuccessful)
                    {
                        toReturn.ValidationResult.Merge(oneFileResult.ValidationResult);
                    }

                }
                else if (action.ActionAttribute.FilePath.ToLower().StartsWith("ftp"))
                {
                    string ftpFilePath = action.ActionAttribute.FilePath;
                    string ftpUserName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(action.ActionAttribute.FtpUserName);
                    string ftpPassword = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(action.ActionAttribute.FtpPassword);

                    var oneFileResult = AppIntergrationSettingBL.UpdateStagingTableDataFromJsonFilePath(importSettingId, ftpFilePath, ftpUserName, ftpPassword);

                    if (!oneFileResult.IsSuccessful)
                    {
                        toReturn.ValidationResult.Merge(oneFileResult.ValidationResult);
                    }
                }
                else
                {
                    string folderPath = action.ActionAttribute.FilePath;


                    if (folderPath.ToLowerInvariant().IndexOf(".json") != -1

                        )
                    {
                        var oneFileResult = AppIntergrationSettingBL.UpdateStagingTableDataFromJsonFilePath(importSettingId, folderPath);

                        if (!oneFileResult.IsSuccessful)
                        {
                            toReturn.ValidationResult.Merge(oneFileResult.ValidationResult);
                        }
                    }
                    else
                    {
                        DirectoryInfo d = new DirectoryInfo(folderPath);
                        List<FileInfo> files = d.GetFiles().OrderBy(o => o.CreationTime).ToList(); ;

                        foreach (FileInfo fileInfo in files)
                        {
                            if (fileInfo.Extension.ToLowerInvariant().IndexOf(".json") != -1)
                            {
                                var oneFileResult = AppIntergrationSettingBL.UpdateStagingTableDataFromJsonFilePath(importSettingId, fileInfo.FullName);

                                if (!oneFileResult.IsSuccessful)
                                {
                                    toReturn.ValidationResult.Merge(oneFileResult.ValidationResult);
                                }
                            }

                        }

                    }


                }


            }
        }

        private static void ExecuteCommand_ImportToDatabaseTableFromExcel(OperationCallResult<TransactionCommandActionResultDto> toReturn, AppProjectWorkFlowActionExDto action)
        {
            if (action.ActionAttribute != null && action.ActionAttribute.ExcelImportSettingId.HasValue && !string.IsNullOrWhiteSpace(action.ActionAttribute.FilePath))
            {
                int importSettingId = action.ActionAttribute.ExcelImportSettingId.Value;

                if (action.ActionAttribute.FilePath.ToLower().StartsWith("http"))
                {
                    string url = action.ActionAttribute.FilePath;
                    var oneFileResult = AppDatabaseTableImportBL.UpdateImportedTableDataFromExcelFilePath(importSettingId, url);

                    if (!oneFileResult.IsSuccessful)
                    {
                        toReturn.ValidationResult.Merge(oneFileResult.ValidationResult);
                    }

                }
                else if (action.ActionAttribute.FilePath.ToLower().StartsWith("ftp"))
                {
                    string ftpFilePath = action.ActionAttribute.FilePath;
                    string ftpUserName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(action.ActionAttribute.FtpUserName);
                    string ftpPassword = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(action.ActionAttribute.FtpPassword);

                    var oneFileResult = AppDatabaseTableImportBL.UpdateImportedTableDataFromExcelFilePath(importSettingId, ftpFilePath, ftpUserName, ftpPassword);

                    if (!oneFileResult.IsSuccessful)
                    {
                        toReturn.ValidationResult.Merge(oneFileResult.ValidationResult);
                    }
                }
                else
                {
                    string folderPath = action.ActionAttribute.FilePath;

                    if (folderPath.ToLowerInvariant().IndexOf(".csv") != -1
                           || folderPath.ToLowerInvariant().IndexOf(".xls") != -1
                           || folderPath.ToLowerInvariant().IndexOf(".xlsx") != -1
                        )
                    {
                        var oneFileResult = AppDatabaseTableImportBL.UpdateImportedTableDataFromExcelFilePath(importSettingId, folderPath);

                        if (!oneFileResult.IsSuccessful)
                        {
                            toReturn.ValidationResult.Merge(oneFileResult.ValidationResult);
                        }
                    }

                    else // it is pure folder path
                    {
                        // need to check  folderPath is pure folder or pure  excelfile path 

                        DirectoryInfo d = new DirectoryInfo(folderPath);
                        List<FileInfo> files = d.GetFiles().OrderBy(o => o.CreationTime).ToList(); ;

                        foreach (FileInfo fileInfo in files)
                        {
                            if (fileInfo.Extension.ToLowerInvariant().IndexOf(".csv") != -1
                                || fileInfo.Extension.ToLowerInvariant().IndexOf(".xls") != -1
                                || fileInfo.Extension.ToLowerInvariant().IndexOf(".xlsx") != -1


                                )
                            {
                                var oneFileResult = AppDatabaseTableImportBL.UpdateImportedTableDataFromExcelFilePath(importSettingId, fileInfo.FullName);

                                if (!oneFileResult.IsSuccessful)
                                {
                                    toReturn.ValidationResult.Merge(oneFileResult.ValidationResult);
                                }

                            }
                        }
                    }


                }

            }

        }

        private static void ExecuteCommand_ImportToDatabaseTablesFromMultipleJsonFiles(OperationCallResult<TransactionCommandActionResultDto> toReturn, AppProjectWorkFlowActionExDto action)
        {
            if (action.ActionAttribute != null && !string.IsNullOrWhiteSpace(action.ActionAttribute.FilePath))
            {
                string folderPath = action.ActionAttribute.FilePath;

                DirectoryInfo d = new DirectoryInfo(folderPath);

                List<FileInfo> files = AppEsiteFileBL.GetDirectoryFileList(d, true);

                if (files.Count > 0)
                {
                    List<AppIntergrationSettingParameterExDto> importSettingList = AppIntergrationSettingBL.RetrieveAllJsonFileTableImportSettingDtoList();

                    Dictionary<string, int> dictOrgFileNameAndImportSettingId = new Dictionary<string, int>();

                    foreach (var importSettingDto in importSettingList)
                    {
                        if (!string.IsNullOrWhiteSpace(importSettingDto.ExternalFieldName))
                        {
                            string orgFileName = importSettingDto.ExternalFieldName.Trim().ToLower();

                            if (!dictOrgFileNameAndImportSettingId.ContainsKey(orgFileName))
                            {
                                dictOrgFileNameAndImportSettingId.Add(orgFileName, (int)importSettingDto.Id);
                            }
                        }
                    }

                    foreach (FileInfo fileInfo in files)
                    {
                        if (fileInfo.Extension.ToLowerInvariant().IndexOf(".json") != -1 && !string.IsNullOrWhiteSpace(fileInfo.Name))
                        {
                            string fileName = fileInfo.Name.Trim().ToLower();

                            if (dictOrgFileNameAndImportSettingId.ContainsKey(fileName))
                            {
                                int importSettingId = dictOrgFileNameAndImportSettingId[fileName];
                                var oneFileResult = AppIntergrationSettingBL.UpdateStagingTableDataFromJsonFilePath(importSettingId, fileInfo.FullName);

                                if (!oneFileResult.IsSuccessful)
                                {
                                    toReturn.ValidationResult.Merge(oneFileResult.ValidationResult);
                                    break;
                                }
                            }
                        }
                    }
                }

            }
        }

        private static void ExecuteCommand_ImportToDatabaseTablesFromMultipleExcelFiles(OperationCallResult<TransactionCommandActionResultDto> toReturn, AppProjectWorkFlowActionExDto action)
        {
            if (action.ActionAttribute != null && !string.IsNullOrWhiteSpace(action.ActionAttribute.FilePath))
            {
                string folderPath = action.ActionAttribute.FilePath;

                DirectoryInfo d = new DirectoryInfo(folderPath);

                List<FileInfo> files = AppEsiteFileBL.GetDirectoryFileList(d, true);

                if (files.Count > 0)
                {
                    List<AppDataSetExDto> importSettingList = AppDatabaseTableImportBL.RetrieveAllExcelTableImportSettingDto(false).ToList();

                    Dictionary<string, int> dictOrgFileNameAndImportSettingId = new Dictionary<string, int>();

                    foreach (var importDataSetDto in importSettingList)
                    {
                        if (importDataSetDto.OtherSettingsDto != null
                            && importDataSetDto.OtherSettingsDto.TableImportSettingDto != null)
                        {
                            var importSettingDto = importDataSetDto.OtherSettingsDto.TableImportSettingDto;

                            if (importSettingDto.IsFinalized)
                            {
                                if (!string.IsNullOrWhiteSpace(importSettingDto.ImportFileName))
                                {
                                    string orgFileName = importSettingDto.ImportFileName.Trim().ToLower();

                                    if (!dictOrgFileNameAndImportSettingId.ContainsKey(orgFileName))
                                    {
                                        dictOrgFileNameAndImportSettingId.Add(orgFileName, (int)importDataSetDto.Id);
                                    }
                                }
                            }
                        }
                    }

                    foreach (FileInfo fileInfo in files)
                    {
                        if (fileInfo.Extension.ToLowerInvariant().IndexOf(".csv") != -1
                                || fileInfo.Extension.ToLowerInvariant().IndexOf(".xls") != -1
                                || fileInfo.Extension.ToLowerInvariant().IndexOf(".xlsx") != -1
                                )
                        {
                            string fileName = fileInfo.Name.Trim().ToLower();

                            if (!string.IsNullOrWhiteSpace(fileName) && dictOrgFileNameAndImportSettingId.ContainsKey(fileName))
                            {
                                int importSettingId = dictOrgFileNameAndImportSettingId[fileName];
                                var oneFileResult = AppDatabaseTableImportBL.UpdateImportedTableDataFromExcelFilePath(importSettingId, fileInfo.FullName);

                                if (!oneFileResult.IsSuccessful)
                                {
                                    toReturn.ValidationResult.Merge(oneFileResult.ValidationResult);
                                    break;
                                }
                            }

                        }
                    }
                }

            }

            if (action.ActionAttribute != null && action.ActionAttribute.ExcelImportSettingId.HasValue && !string.IsNullOrWhiteSpace(action.ActionAttribute.FilePath))
            {
                int importSettingId = action.ActionAttribute.ExcelImportSettingId.Value;

                if (action.ActionAttribute.FilePath.ToLower().StartsWith("http"))
                {
                    string url = action.ActionAttribute.FilePath;
                    var oneFileResult = AppDatabaseTableImportBL.UpdateImportedTableDataFromExcelFilePath(importSettingId, url);

                    if (!oneFileResult.IsSuccessful)
                    {
                        toReturn.ValidationResult.Merge(oneFileResult.ValidationResult);
                    }

                }
                else if (action.ActionAttribute.FilePath.ToLower().StartsWith("ftp"))
                {
                    string ftpFilePath = action.ActionAttribute.FilePath;
                    string ftpUserName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(action.ActionAttribute.FtpUserName);
                    string ftpPassword = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(action.ActionAttribute.FtpPassword);

                    var oneFileResult = AppDatabaseTableImportBL.UpdateImportedTableDataFromExcelFilePath(importSettingId, ftpFilePath, ftpUserName, ftpPassword);

                    if (!oneFileResult.IsSuccessful)
                    {
                        toReturn.ValidationResult.Merge(oneFileResult.ValidationResult);
                    }
                }
                else
                {
                    string folderPath = action.ActionAttribute.FilePath;

                    if (folderPath.ToLowerInvariant().IndexOf(".csv") != -1
                           || folderPath.ToLowerInvariant().IndexOf(".xls") != -1
                           || folderPath.ToLowerInvariant().IndexOf(".xlsx") != -1
                        )
                    {
                        var oneFileResult = AppDatabaseTableImportBL.UpdateImportedTableDataFromExcelFilePath(importSettingId, folderPath);

                        if (!oneFileResult.IsSuccessful)
                        {
                            toReturn.ValidationResult.Merge(oneFileResult.ValidationResult);
                        }
                    }

                    else // it is pure folder path
                    {
                        // need to check  folderPath is pure folder or pure  excelfile path 

                        DirectoryInfo d = new DirectoryInfo(folderPath);
                        List<FileInfo> files = d.GetFiles().OrderBy(o => o.CreationTime).ToList(); ;

                        foreach (FileInfo fileInfo in files)
                        {
                            if (fileInfo.Extension.ToLowerInvariant().IndexOf(".csv") != -1
                                || fileInfo.Extension.ToLowerInvariant().IndexOf(".xls") != -1
                                || fileInfo.Extension.ToLowerInvariant().IndexOf(".xlsx") != -1


                                )
                            {
                                var oneFileResult = AppDatabaseTableImportBL.UpdateImportedTableDataFromExcelFilePath(importSettingId, fileInfo.FullName);

                                if (!oneFileResult.IsSuccessful)
                                {
                                    toReturn.ValidationResult.Merge(oneFileResult.ValidationResult);
                                }

                            }
                        }
                    }


                }

            }

        }



        private static void ExecuteCommand_ImportToDatabaseTableFromDbToDbImportSetting(OperationCallResult<TransactionCommandActionResultDto> toReturn, AppProjectWorkFlowActionExDto action)
        {
            if (action.ActionAttribute != null && action.ActionAttribute.DbToDbImportSettingId.HasValue)
            {
                int importSettingId = action.ActionAttribute.DbToDbImportSettingId.Value;

                AppDataSetExDto importSettingDataSetExDto = AppDatabaseErDiagramBL.RetrieveOneErDiagramExDto(importSettingId);

                var importResult = AppDatabaseTableImportBL.ProcessMetaDataTablesImport(importSettingDataSetExDto);

                if (!importResult.IsSuccessful)
                {
                    toReturn.ValidationResult.Merge(importResult.ValidationResult);
                }
            }

        }

        private static void ExecuteCommand_ImportToDatabaseTableFromRestApiImportSetting(OperationCallResult<TransactionCommandActionResultDto> toReturn, AppProjectWorkFlowActionExDto action)
        {
            if (action.ActionAttribute != null && action.ActionAttribute.RestApiDbImportSettingId.HasValue)
            {
                int importSettingId = action.ActionAttribute.RestApiDbImportSettingId.Value;


                var importResult = AppIntergrationSettingBL.ExecuteDataImportOnJsonFileTableImportSetting(importSettingId);

                if (!importResult.IsSuccessful)
                {
                    toReturn.ValidationResult.Merge(importResult.ValidationResult);
                }
            }

        }

        private static void ExecuteCommand_ExecuteExternalExeProcess(OperationCallResult<TransactionCommandActionResultDto> toReturn, AppProjectWorkFlowActionExDto action)
        {
            if (action.ActionAttribute != null && !string.IsNullOrWhiteSpace(action.ActionAttribute.FilePath))
            {
                try
                {
                    string exeFilePath = action.ActionAttribute.FilePath;
                    string jsonString = action.ActionAttribute.Arguments;

                    ProcessHelper.CreateProcess(exeFilePath, jsonString);
                }
                catch (Exception ex)
                {
                    toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_ExcusteSqlStatmentActionWithRootValue_Error", ValidationItemType.Error, ex.ToString()));

                }
            }

        }

        //private static void ExecuteCommand_CreateWindowsSchdulerTask(OperationCallResult<TransactionCommandActionResultDto> toReturn, AppProjectWorkFlowActionExDto action)
        //{
        //    if (action.ActionAttribute != null)
        //    {
        //        try
        //        {
        //            string taskFolder = "\\Hu\\TestFolder1";
        //            string taskName = "MyScheduledTask";
        //            //string taskDescription = "This is a test scheduled task.";
        //            string fullTaskPath = $"{taskFolder}\\{taskName}";

        //            string programPath = @"C:\temp\test.exe";
        //            string startTime = "09:00"; // Format: HH:mm                     
        //            string scheduleType = "daily";
        //            string user = "System";

        //            // Command to create the scheduled task
        //            // string createTaskCmd = $"/create /tn \"{fullTaskPath}\" /tr \"{programPath}\" /sc daily /st {scheduleTime} /f /rl highest /ru System";

        //            /// create: Creates a new scheduled task.
        //            /// tn: Specifies the task name.
        //            /// tr: Specifies the task's action (the path to the executable).
        //            /// sc: Specifies the schedule type(daily, in this case).
        //            /// st: Specifies the start time.
        //            /// f: Forces the creation of the task if it already exists.
        //            /// rl: Specifies the run level(e.g., highest).
        //            /// ru: Specifies the user account under which the task should run(e.g., System).
        //            /// 
        //            string createTaskCmd = $"/create /tn \"{fullTaskPath}\" /tr \"{programPath}\" /sc {scheduleType} /st {startTime} /ru {user} /rl highest /f";


        //            if (ProcessHelper.ExecuteCommand("schtasks", createTaskCmd))
        //            {
        //                //Console.WriteLine($"Scheduled task '{taskName}' created successfully.");
        //            }
        //            else
        //            {
        //                //Console.WriteLine("Failed to create the scheduled task.");
        //            }




        //            //Example Commands

        //            //Here are some example schtasks commands for different scenarios:

        //            //1.Create a Task That Runs Once:                        
        //            //schtasks / create / tn "OneTimeTask" / tr "C:\Path\To\Your\Application.exe" / sc once / st 09:00 / sd 07 / 30 / 2024 / f

        //            //2. Create a Task That Runs Weekly:
        //            //schtasks / create / tn "WeeklyTask" / tr "C:\Path\To\Your\Application.exe" / sc weekly / d MON / st 09:00 / f

        //            //3. Create a Task That Runs at System Startup:
        //            //schtasks / create / tn "StartupTask" / tr "C:\Path\To\Your\Application.exe" / sc onstart / f

        //            //4. Delete a Task:
        //            //schtasks / delete / tn "MyScheduledTask" / f


        //        }
        //        catch (Exception ex)
        //        {
        //            toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_ExcusteSqlStatmentActionWithRootValue_Error", ValidationItemType.Error, ex.ToString()));

        //        }
        //    }

        //}



        private static void ExecuteCommand_DownloadFileToServerFolder(OperationCallResult<TransactionCommandActionResultDto> toReturn, AppProjectWorkFlowActionExDto action)
        {
            if (action.ActionAttribute != null && !string.IsNullOrWhiteSpace(action.ActionAttribute.FilePath) && !string.IsNullOrWhiteSpace(action.ActionAttribute.DistinationFilePath))
            {
                try
                {
                    string url = action.ActionAttribute.FilePath;
                    string distinationFilePath = action.ActionAttribute.DistinationFilePath;

                    if (url.ToLower().StartsWith("ftp"))
                    {
                        FtpTools ftpInstance = new FtpTools("", action.ActionAttribute.FtpUserName, action.ActionAttribute.FtpPassword);
                        string errorMsg = "";

                        ftpInstance.download(url, distinationFilePath, out errorMsg);

                        if (!string.IsNullOrWhiteSpace(errorMsg))
                        {
                            errorMsg = "Failed to download file from url: " + url + "\n" + errorMsg;
                            toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Condition pass failure", ValidationItemType.Error, errorMsg));
                        }
                    }
                    else
                    {
                        string errorMsg = "";

                        AppEsiteFileBL.DownloadFileToServer(url, distinationFilePath, out errorMsg);


                        if (!string.IsNullOrWhiteSpace(errorMsg))
                        {
                            errorMsg = "Failed to download file from url: " + url + "\n" + errorMsg;
                            toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Condition pass failure", ValidationItemType.Error, errorMsg));
                        }
                    }
                }
                catch (Exception ex)
                {
                    toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_ExcusteSqlStatmentActionWithRootValue_Error", ValidationItemType.Error, ex.ToString()));

                }
            }
            else
            {
                toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_ExcusteSqlStatmentActionWithRootValue_Error", ValidationItemType.Error, "Invalid command setting: " + action.Name));
            }

        }

        private static void ExecuteCommand_ConvertFromXmlToJson(OperationCallResult<TransactionCommandActionResultDto> toReturn, AppProjectWorkFlowActionExDto action)
        {
            if (action.ActionAttribute != null && !string.IsNullOrWhiteSpace(action.ActionAttribute.FilePath) && !string.IsNullOrWhiteSpace(action.ActionAttribute.DistinationFilePath))
            {
                string downloadedTempFilePath = ExecuteCommand_ConvertFromXmlToJson_DownloadTempFile(toReturn, action);


                if (!string.IsNullOrWhiteSpace(downloadedTempFilePath))
                {
                    try
                    {
                        if (File.Exists(downloadedTempFilePath))
                        {
                            string fileContent = File.ReadAllText(downloadedTempFilePath);

                            XmlDocument doc = new XmlDocument();
                            doc.LoadXml(fileContent);
                            string jsonText = JsonConvert.SerializeXmlNode(doc);

                            string distinationFilePath = action.ActionAttribute.DistinationFilePath;
                            File.WriteAllText(distinationFilePath, jsonText);

                            try
                            {
                                File.Delete(downloadedTempFilePath);
                            }
                            catch (Exception deleteFileEx)
                            {

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_ExcusteSqlStatmentActionWithRootValue_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }
            else
            {
                toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_ExcusteSqlStatmentActionWithRootValue_Error", ValidationItemType.Error, "Invalid command setting: " + action.Name));
            }



        }


        private static void ExecuteCommand_ConvertBackFromJsonToXml(OperationCallResult<TransactionCommandActionResultDto> toReturn, AppProjectWorkFlowActionExDto action)
        {
            if (action.ActionAttribute != null && !string.IsNullOrWhiteSpace(action.ActionAttribute.FilePath) && !string.IsNullOrWhiteSpace(action.ActionAttribute.DistinationFilePath))
            {
                string downloadedTempFilePath = ExecuteCommand_ConvertFromXmlToJson_DownloadTempFile(toReturn, action);


                if (!string.IsNullOrWhiteSpace(downloadedTempFilePath))
                {
                    try
                    {
                        if (File.Exists(downloadedTempFilePath))
                        {
                            string fileContent = File.ReadAllText(downloadedTempFilePath);

                            XmlDocument doc = JsonConvert.DeserializeXmlNode(fileContent);

                            string distinationFilePath = action.ActionAttribute.DistinationFilePath;
                            doc.Save(distinationFilePath);

                            try
                            {
                                File.Delete(downloadedTempFilePath);
                            }
                            catch (Exception deleteFileEx)
                            {

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_ExcusteSqlStatmentActionWithRootValue_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }
            else
            {
                toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_ExcusteSqlStatmentActionWithRootValue_Error", ValidationItemType.Error, "Invalid command setting: " + action.Name));
            }



        }



        private static string ExecuteCommand_ConvertFromXmlToJson_DownloadTempFile(OperationCallResult<TransactionCommandActionResultDto> toReturn, AppProjectWorkFlowActionExDto action)
        {
            string downloadedTempFilePath = "";

            try
            {
                string url = action.ActionAttribute.FilePath;
                string srcFileName = Path.GetFileName(url);
                string srcExtension = Path.GetExtension(srcFileName);

                string distinationFilePath = action.ActionAttribute.DistinationFilePath;
                string destinationFolderPath = "";
                string targetFileName = Path.GetFileName(action.ActionAttribute.DistinationFilePath);

                if (string.IsNullOrWhiteSpace(targetFileName))
                {
                    destinationFolderPath = action.ActionAttribute.DistinationFilePath;
                }
                else
                {
                    destinationFolderPath = distinationFilePath.Substring(0, distinationFilePath.Length - targetFileName.Length);
                }

                string tempDownloadFileName = "temp" + ExtensionMethodhelper.RandomId() + srcExtension;
                string needToDownloadTempFilePath = Path.Combine(destinationFolderPath, tempDownloadFileName);


                if (url.ToLower().StartsWith("ftp"))
                {
                    FtpTools ftpInstance = new FtpTools("", action.ActionAttribute.FtpUserName, action.ActionAttribute.FtpPassword);
                    string errorMsg = "";

                    ftpInstance.download(url, needToDownloadTempFilePath, out errorMsg);

                    if (!string.IsNullOrWhiteSpace(errorMsg))
                    {
                        errorMsg = "Failed to download file from url: " + url + "\n" + errorMsg;
                        toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Condition pass failure", ValidationItemType.Error, errorMsg));
                    }
                    else
                    {
                        downloadedTempFilePath = needToDownloadTempFilePath;
                    }
                }
                else
                {
                    string errorMsg = "";

                    AppEsiteFileBL.DownloadFileToServer(url, needToDownloadTempFilePath, out errorMsg);


                    if (!string.IsNullOrWhiteSpace(errorMsg))
                    {
                        errorMsg = "Failed to download file from url: " + url + "\n" + errorMsg;
                        toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Condition pass failure", ValidationItemType.Error, errorMsg));
                    }
                    else
                    {
                        downloadedTempFilePath = needToDownloadTempFilePath;
                    }
                }
            }
            catch (Exception ex)
            {
                toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_ExcusteSqlStatmentActionWithRootValue_Error", ValidationItemType.Error, ex.ToString()));

            }

            return downloadedTempFilePath;
        }



        public static AppProjectWorkFlowActionExDto InFormatExecutionFormula(List<AppTransactionFieldExDto> transactionFieldList, AppProjectWorkFlowActionExDto action)
        {
            if (transactionFieldList != null && action.ActionAttribute != null)
            {
                string expression = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(action.ActionAttribute.ExecutionFormulaUI);
                expression = AppPorjectWorkFlowBL.InFormatExpressionString(transactionFieldList, expression);
                action.ActionAttribute.ExecutionFormula = expression;
            }
            return action;
        }

        public static AppProjectWorkFlowActionExDto OutFormatExecutionFormula(List<AppTransactionFieldExDto> transactionFieldList, AppProjectWorkFlowActionExDto action)
        {
            if (transactionFieldList != null && action.ActionAttribute != null && action.ActionAttribute.ExecutionFormula != null)
            {
                if (action.ActionAttribute.ExecutionFormula.Contains(AppTransactionFormulaBL.FormulaLineEnd))
                {
                    action.ActionAttribute.ExecutionFormulaUI = string.Empty;
                    string[] formulaExpressionList = action.ActionAttribute.ExecutionFormula.Split(AppTransactionFormulaBL.FormulaLineEnd.ToArray());
                    foreach (string aFormulaExpression in formulaExpressionList)
                    {
                        if (!string.IsNullOrWhiteSpace(aFormulaExpression))
                        {
                            string expression = AppPorjectWorkFlowBL.OutFormatExpressionString(transactionFieldList, aFormulaExpression);
                            action.ActionAttribute.ExecutionFormulaUI += expression + AppTransactionFormulaBL.FormulaLineEnd + "\n";
                        }
                    }
                }
                else
                {
                    string expression = action.ActionAttribute.ExecutionFormula;
                    expression = AppPorjectWorkFlowBL.OutFormatExpressionString(transactionFieldList, expression);
                    action.ActionAttribute.ExecutionFormulaUI = expression;
                }
            }
            return action;
        }


        public static WorkflowAutomationRuntimeDto GetWorkflowAutomationRuntimeProgressData(int workflowTransactionId, string transactionRId)
        {
            if (string.IsNullOrWhiteSpace(transactionRId) || transactionRId == "null" || transactionRId == "undefined")
            {
                transactionRId = "";
            }

            DataTable dtAppBatchLog = GetOneWorkflowLatestAppBatchLogDataTableFromDb(workflowTransactionId, transactionRId);

            if (dtAppBatchLog != null && dtAppBatchLog.Rows.Count > 0)
            {
                DataRow dataRow = dtAppBatchLog.Rows[0];
                WorkflowAutomationRuntimeDto toReturn = ConvertAppBatchLogDataRowToWorkflowRuntimeDto(dataRow);
                toReturn.TransactionId = workflowTransactionId;
                toReturn.TransactionRId = transactionRId;

                return toReturn;
            }

            return null;
        }

        private static WorkflowAutomationRuntimeDto ConvertAppBatchLogDataRowToWorkflowRuntimeDto(DataRow dataRow)
        {
            WorkflowAutomationRuntimeDto toReturn = new WorkflowAutomationRuntimeDto();

            try
            {
                //toReturn.TransactionId = workflowTransactionId;
                //toReturn.TransactionRId = transactionRId;
                toReturn.DictOneToOneFields = new Dictionary<string, object>();


                string formDataString = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dataRow["RuntimeFormData"]);
                string nodeTreeDataString = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dataRow["RuntimeWorkflowNodeTreeData"]);
                string executingCommandLogicTreeNodeId = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dataRow["ExecutingCommandLogicTreeNodeId"]);


                toReturn.BatchNumber = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dataRow["BatchNumber"]);
                toReturn.WorkflowProgressStatus = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dataRow["WorkflowProgressStatus"]);

                toReturn.StartTime = ControlTypeValueConverter.ConvertValueToDate(dataRow["StartTime"]);
                toReturn.StartTime = ClientTimeZoneHelper.ConvertUTCToClientDateTime(toReturn.StartTime);

                toReturn.EndTime = ControlTypeValueConverter.ConvertValueToDate(dataRow["EndTime"]);
                toReturn.EndTime = ClientTimeZoneHelper.ConvertUTCToClientDateTime(toReturn.EndTime);




                AppMasterDetailDto formData = JsonConvert.DeserializeObject<AppMasterDetailDto>(formDataString);
                toReturn.DictOneToOneFields = formData.DictOneToOneFields;
                toReturn.WorkflowCommandNodeTree = JsonConvert.DeserializeObject<List<AppProjectWorkFlowActionDto>>(nodeTreeDataString);

                toReturn.ExecutingNodeLogicId = executingCommandLogicTreeNodeId;
            }
            catch (Exception ex)
            {

            }

            return toReturn;
        }

        public static OperationCallResult<bool> ForceStopWorkflowByBatchNumber(string batchNumber, int? workflowAutomationId)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (!string.IsNullOrWhiteSpace(batchNumber))
            {
                try
                {
                    DataTable dtAppBatchLog = GetOneAppBatchLogDataTableFromDbByBatchNumber(batchNumber);

                    if (dtAppBatchLog.Rows != null && dtAppBatchLog.Rows.Count > 0)
                    {
                        DataRow dataRow = dtAppBatchLog.Rows[0];
                        WorkflowAutomationRuntimeDto runtimeDto = ConvertAppBatchLogDataRowToWorkflowRuntimeDto(dataRow);

                        if (runtimeDto.WorkflowProgressStatus == EmAppWorkflowProgressStatus.Started.ToString())
                        {
                            AppCacheManagerBL.SetIsWorkflowForceStoppedByBatchIdFromCache(batchNumber, true);
                            SetAppBatchLogProgressStatus(batchNumber, EmAppWorkflowProgressStatus.ForceAbortedByUser.ToString(), "", workflowAutomationId);

                            aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "ForceStopWorkflowByBatchNumber_Success", ValidationItemType.Message, "Workflow has been force stopped."));
                            aOperationCallResult.Object = true;
                        }
                        else
                        {
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "ForceStopWorkflowByBatchNumber_Error", ValidationItemType.Error, "Workflow is not running. Abort failed."));
                        }
                    }
                    else
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "ForceStopWorkflowByBatchNumber_Error", ValidationItemType.Error, "Cannot find workflow execution data. Abort failed."));
                    }

                }
                catch (Exception ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "ForceStopWorkflowByBatchNumber_Error", ValidationItemType.Error, "Abort failed.\n" + ex.Message));
                }

            }



            return aOperationCallResult;
        }

        // Input: Client DateTime
        // Output: Client DateTime http://localhost/gather_1/webapi/SchemaMetaData/SaveModifiedTableSchema
        private static OperationCallResult<TransactionCommandActionResultDto> ExecuteSingleCommandByActionType(AppProjectWorkFlowActionExDto actionDto, AppMasterDetailDto aInputFormData, AppTransactionExDto transactionExDto, AppProjectWorkFlowActionExDto rootCommandDto)
        {
            OperationCallResult<TransactionCommandActionResultDto> toReturn = new OperationCallResult<TransactionCommandActionResultDto>();
            toReturn.ValidationResult = new ValidationResult();

            if (actionDto != null && actionDto.ActionType.HasValue)
            {
                if (transactionExDto.TransactionOrganizedType.HasValue)
                {
                    if (transactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.MasterDetail)
                    {
                        if (actionDto.ActionAttribute != null && actionDto.ActionAttribute.IsBatchCommand && actionDto.ActionAttribute.BatchCommandSourceFromType.HasValue
                            && actionDto.ActionAttribute.BatchCommandSourceFromType.Value == (int)EmAppBatchCommandSourceFrom.ChildUnit
                            && actionDto.ActionAttribute.ForeachLoopSourceUnitId.HasValue)
                        {
                            ExecuteSingleBatchCommand_LoopOnChildUnit(actionDto, aInputFormData, transactionExDto, toReturn, rootCommandDto);
                        }
                        else
                        {
                            ExecuteSingleCommandByActionType_MasterDetailTransaction(actionDto, aInputFormData, transactionExDto, toReturn, rootCommandDto);
                        }




                    }
                    else if (transactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.List)
                    {
                        ExecuteSingleCommandByActionType_ListEditTransaction(actionDto, aInputFormData, transactionExDto, toReturn);

                    }
                }





            }

            return toReturn;
        }



        private static void ExecuteSingleCommandByActionType_MasterDetailTransaction(AppProjectWorkFlowActionExDto actionDto, AppMasterDetailDto aInputFormData, AppTransactionExDto transactionExDto, OperationCallResult<TransactionCommandActionResultDto> toReturn, AppProjectWorkFlowActionExDto rootCommandDto)
        {
            Dictionary<int, object> dictOneRowFiedIdValue = AppTransactionFormulaBL.ConvertUnitOneRowDbFileNameToFileId(transactionExDto.RootMasterUnit, aInputFormData.DictOneToOneFields);

            if (aInputFormData.CommandForeachLoopToRowData != null && aInputFormData.CommandForeachLoopToRowData.DictOneToOneFields != null)
            {
                if (transactionExDto.DictAllTransactionUnitIdExDto != null && transactionExDto.DictAllTransactionUnitIdExDto.ContainsKey(aInputFormData.CommandForeachLoopToRowData.ChildUnitId.ToString()))
                {
                    var childUnitDto = transactionExDto.DictAllTransactionUnitIdExDto[aInputFormData.CommandForeachLoopToRowData.ChildUnitId.ToString()];
                    Dictionary<int, object> dictChildUnitOneRowFiedIdValue = AppTransactionFormulaBL.ConvertUnitOneRowDbFileNameToFileId(childUnitDto, aInputFormData.CommandForeachLoopToRowData.DictOneToOneFields);

                    foreach (var kvPair in dictChildUnitOneRowFiedIdValue)
                    {
                        dictOneRowFiedIdValue.Add(kvPair.Key, kvPair.Value);
                    }
                }
            }

            MergeRootCommandFieldValueToDictFieldIdAndValue(rootCommandDto, dictOneRowFiedIdValue);

            toReturn.Object = new TransactionCommandActionResultDto();
            toReturn.Object.CommandId = ControlTypeValueConverter.ConvertValueToInt(actionDto.Id);
            toReturn.Object.ActionTypeId = actionDto.ActionType;
            toReturn.Object.FormData = aInputFormData;


            actionDto.ActionAttribute.FilePath = AppProjectWorkFlowDataFormSynchBL.ReplaceSQlParamterWithActualValue(dictOneRowFiedIdValue, actionDto.ActionAttribute.FilePath, null); ;
            actionDto.ActionAttribute.DistinationFilePath = AppProjectWorkFlowDataFormSynchBL.ReplaceSQlParamterWithActualValue(dictOneRowFiedIdValue, actionDto.ActionAttribute.DistinationFilePath, null);
            actionDto.ActionAttribute.FtpUserName = AppProjectWorkFlowDataFormSynchBL.ReplaceSQlParamterWithActualValue(dictOneRowFiedIdValue, actionDto.ActionAttribute.FtpUserName, null);
            actionDto.ActionAttribute.FtpPassword = AppProjectWorkFlowDataFormSynchBL.ReplaceSQlParamterWithActualValue(dictOneRowFiedIdValue, actionDto.ActionAttribute.FtpPassword, null);
            actionDto.ActionAttribute.Arguments = AppProjectWorkFlowDataFormSynchBL.ReplaceSQlParamterWithActualValue(dictOneRowFiedIdValue, actionDto.ActionAttribute.Arguments, null);


            if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.ExecuteSQLStatement)
            {
                DataModelDateTimeConverterBL.ConvertMasterDetailFromClientToUtc(toReturn.Object.FormData);
                Dictionary<int, object> fromDBFiledValue = ExcusteSqlStatmentActionWithRootValue(dictOneRowFiedIdValue, actionDto, toReturn, transactionExDto);


                DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(toReturn.Object.FormData);
            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.PluginWebApiCall)
            {
                // To Do, Convert Time at Plugin API 
                AppTransactionCommandBL.ExecuteCommand_PluginWebApiCall(actionDto, toReturn);
            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.IntegrationWebApiCall)
            {
                // To Do, Convert Time at Plugin API 
                AppTransactionCommandBL.ExecuteCommand_IntegrationWebApiCall(actionDto, toReturn);
            }


            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.ExternalMethodMasterDetail)
            {

                // 
                object externalMethodRegisterId = actionDto.ActionAttribute.ExternalMethodRegisterId;

                OperationCallResult<AppMasterDetailDto> callExternResult = AppExternalMethodRegisterBL.CallExternalMethodMasterDetail(externalMethodRegisterId, new object[] { aInputFormData });
                if (callExternResult != null && callExternResult.Object != null)
                {
                    toReturn.Object.FormData = callExternResult.Object;
                    toReturn.ValidationResult.Merge(callExternResult.ValidationResult);
                }




            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.ExecuteFormDataTransfer)
            {
                //DataModelDateTimeConverterBL.ConvertMasterDetailFromClientToUtc(toReturn.Object.FormData);
                AppTransactionCommandBL.ExecuteCommand_DataTransfer(actionDto, toReturn, rootCommandDto);

                //DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(toReturn.Object.FormData);
            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.DataModelAllFormulaCalculation)
            {
                AppTransactionCommandBL.ExecuteCommand_ValidateAndCalculateMasterDetailTransactionData(toReturn);
            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.MasterDetailDataNotNullValidation)
            {
                AppTransactionCommandBL.ExecuteCommand_MasterDetailDataFirstLevelValidation(toReturn);
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
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.OpenFormEditWindow)
            {
                AppTransactionCommandBL.ExecuteCommand_OpenFormEditWindow(actionDto, toReturn);
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
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.CallApiDefaultPublish)
            {
                DataModelDateTimeConverterBL.ConvertMasterDetailFromClientToUtc(toReturn.Object.FormData);
                AppTransactionCommandBL.ExecuteCommand_CallApiDefaultPublish(toReturn);
                DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(toReturn.Object.FormData);
            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.CallApiOperation)
            {
                DataModelDateTimeConverterBL.ConvertMasterDetailFromClientToUtc(toReturn.Object.FormData);
                AppTransactionCommandBL.ExecuteCommand_CallApiOperation(toReturn, actionDto);
                DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(toReturn.Object.FormData);
            }


            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.SendMessageToTransFieldEmailAddress)
            {
                AppTransactionCommandBL.ExecuteCommand_SendMessageToTransFieldEmailAddress(toReturn.Object.FormData, actionDto);
            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.SendMessageToTransFieldUserId)
            {
                AppTransactionCommandBL.ExecuteCommand_SendMessageToTransFieldUserId(toReturn.Object.FormData, actionDto);
            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.SendMessageToTransFieldPartnerId)
            {
                AppTransactionCommandBL.ExecuteCommand_SendMessageToTransFieldPartnerId(toReturn.Object.FormData, actionDto);
            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.SendSmsToTransFieldPhoneNumber)
            {
                AppTransactionCommandBL.ExecuteCommand_SendSmsToTransFieldPhoneNumber(toReturn.Object.FormData, actionDto);
            }
            //else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.DetectTransactionFieldValueChangeBeforeSaveDB)
            //{
            //    AppTransactionCommandBL.ExecuteCommand_DetectOneTransactionFieldIsValueChangedBeforeSaveDB(actionDto, toReturn);
            //}
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.CommnadFormulaCalculation)
            {
                AppTransactionCommandBL.ExecuteCommand_ExecutionFormula(toReturn, actionDto, rootCommandDto);
            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.QuickCreateUser)
            {
                AppTransactionCommandBL.ExecuteCommand_QuickCreateUser(toReturn.Object.FormData, actionDto, toReturn);
            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.ImportToDatabaseTableFromJson)
            {
                AppTransactionCommandBL.ExecuteCommand_ImportToDatabaseTableFromJson(toReturn, actionDto);
            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.ImportToDatabaseTableFromExcel)
            {
                AppTransactionCommandBL.ExecuteCommand_ImportToDatabaseTableFromExcel(toReturn, actionDto);
            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.ImportToDatabaseTablesFromMultipleJsonFiles)
            {
                AppTransactionCommandBL.ExecuteCommand_ImportToDatabaseTablesFromMultipleJsonFiles(toReturn, actionDto);
            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.ImportToDatabaseTablesFromMultipleExcelFiles)
            {
                AppTransactionCommandBL.ExecuteCommand_ImportToDatabaseTablesFromMultipleExcelFiles(toReturn, actionDto);
            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.ImportToDatabaseTableFromDbToDbImportSetting)
            {
                AppTransactionCommandBL.ExecuteCommand_ImportToDatabaseTableFromDbToDbImportSetting(toReturn, actionDto);
            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.ImportToDatabaseTableFromRestApiImportSetting)
            {
                AppTransactionCommandBL.ExecuteCommand_ImportToDatabaseTableFromRestApiImportSetting(toReturn, actionDto);
            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.ExecuteExternalExeProcess)
            {
                AppTransactionCommandBL.ExecuteCommand_ExecuteExternalExeProcess(toReturn, actionDto);
            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.DownloadFileToServerFolder)
            {
                AppTransactionCommandBL.ExecuteCommand_DownloadFileToServerFolder(toReturn, actionDto);
            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.ConvertFromXmlToJson)
            {
                AppTransactionCommandBL.ExecuteCommand_ConvertFromXmlToJson(toReturn, actionDto);
            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.ConvertBackFromJsonToXml)
            {
                AppTransactionCommandBL.ExecuteCommand_ConvertBackFromJsonToXml(toReturn, actionDto);
            }
            //else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.CreateWindowsSchedulerTask)
            //{
            //    AppTransactionCommandBL.ExecuteCommand_CreateWindowsSchdulerTask(toReturn, actionDto);
            //}


            if (toReturn.IsSuccessful)
            {
                BuildCommandSuccessMessage(actionDto, toReturn);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(actionDto.ActionAttribute.CommandFailedMessage))
                {
                    string messageTempalte = actionDto.ActionAttribute.CommandFailedMessage;

                    string currentDatetime = DateTime.UtcNow.ToShortDateString() + " " + DateTime.UtcNow.ToShortTimeString();
                    string currentUserName = AppSecurityUserBL.CurrentUserEntity.UserName;
                    string workflowName = string.Empty;
                    string taskName = string.Empty;
                    string taskStatus = string.Empty;
                    string toList = string.Empty;

                    Dictionary<int, object> allFreshRootValue = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(toReturn.Object.FormData, transactionExDto);


                    object rootKeyValue = toReturn.Object.FormData.RootPrimaryKeyValue;
                    string message = AppProjectWorkFlowDataFormSynchBL.ReplaceMessageTemplatePlaceHolderWithActualValue(rootKeyValue, allFreshRootValue, messageTempalte, currentDatetime, currentUserName, workflowName, taskName, taskStatus, transactionExDto, null, null, false);
                    toReturn.ValidationResult.Items.Clear();
                    toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "CommandMessage", ValidationItemType.Error, message));

                }
            }
        }

        private static void MergeRootCommandFieldValueToDictFieldIdAndValue(AppProjectWorkFlowActionExDto rootCommandDto, Dictionary<int, object> dictOneRowFiedIdValue)
        {
            if (rootCommandDto != null && rootCommandDto.IsWorkflowAutomationRootCommand && rootCommandDto.RootTransactionFormData != null)
            {
                AppTransactionExDto rootTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(rootCommandDto.RootTransactionFormData.TransactionId);

                Dictionary<int, object> dictRootTransactionFiedIdAndValue = AppTransactionFormulaBL.ConvertUnitOneRowDbFileNameToFileId(rootTransactionExDto.RootMasterUnit, rootCommandDto.RootTransactionFormData.DictOneToOneFields);

                foreach (var kvPair in dictRootTransactionFiedIdAndValue)
                {
                    if (!dictOneRowFiedIdValue.ContainsKey(kvPair.Key))
                    {
                        dictOneRowFiedIdValue.Add(kvPair.Key, kvPair.Value);
                    }
                }
            }
        }

        private static void ExecuteSingleCommandByActionType_ListEditTransaction(AppProjectWorkFlowActionExDto actionDto, AppMasterDetailDto aInputFormData, AppTransactionExDto transactionExDto, OperationCallResult<TransactionCommandActionResultDto> toReturn)
        {
            toReturn.Object = new TransactionCommandActionResultDto();
            toReturn.Object.CommandId = ControlTypeValueConverter.ConvertValueToInt(actionDto.Id);
            toReturn.Object.ActionTypeId = actionDto.ActionType;
            toReturn.Object.FormData = aInputFormData;

            if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.ExecuteSQLStatement)
            {

            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.PluginWebApiCall)
            {

            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.IntegrationWebApiCall)
            {

            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.ExternalMethodMasterDetail)
            {

            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.ExecuteFormDataTransfer)
            {
                AppTransactionCommandBL.ExecuteCommand_DataTransferFromListEdit(actionDto, toReturn);
            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.DataModelAllFormulaCalculation)
            {

            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.ExecuteLoadDataSetToTranscation)
            {

            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.GenerateMatrix)
            {

            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.SaveAs)
            {

            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.OpenFormCreationWindow)
            {

            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.refresh)
            {
                AppTransactionCommandBL.ExecuteCommand_RefreshListEdit(toReturn);
                DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(toReturn.Object.FormData);
            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.Save)
            {
                DataModelDateTimeConverterBL.ConvertMasterDetailFromClientToUtc(toReturn.Object.FormData);
                AppTransactionCommandBL.ExecuteCommand_SaveListEdit(toReturn);
                DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(toReturn.Object.FormData);
            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.SendMessageToTransFieldPartnerId)
            {

            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.SendSmsToTransFieldPhoneNumber)
            {

            }
            else if (actionDto.ActionType.Value == (int)EmAppTransactionCommandType.CommnadFormulaCalculation)
            {

            }

            if (toReturn.IsSuccessful)
            {
                BuildCommandSuccessMessage(actionDto, toReturn);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(actionDto.ActionAttribute.CommandFailedMessage))
                {
                    string messageTempalte = actionDto.ActionAttribute.CommandFailedMessage;

                    string currentDatetime = DateTime.UtcNow.ToShortDateString() + " " + DateTime.UtcNow.ToShortTimeString();
                    string currentUserName = AppSecurityUserBL.CurrentUserEntity.UserName;
                    string workflowName = string.Empty;
                    string taskName = string.Empty;
                    string taskStatus = string.Empty;
                    string toList = string.Empty;

                    Dictionary<int, object> allFreshRootValue = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(toReturn.Object.FormData, transactionExDto);


                    object rootKeyValue = toReturn.Object.FormData.RootPrimaryKeyValue;
                    string message = AppProjectWorkFlowDataFormSynchBL.ReplaceMessageTemplatePlaceHolderWithActualValue(rootKeyValue, allFreshRootValue, messageTempalte, currentDatetime, currentUserName, workflowName, taskName, taskStatus, transactionExDto, null, null, false);
                    toReturn.ValidationResult.Items.Clear();
                    toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "CommandMessage", ValidationItemType.Error, message));

                }
            }
        }


        private static Dictionary<int, object> ExcusteSqlStatmentActionWithRootValue(Dictionary<int, object> allFreshRootValue, AppProjectWorkFlowActionExDto action,
            OperationCallResult<TransactionCommandActionResultDto> toReturn, AppTransactionExDto transactionExDto)
        {



            var dictUpdaetDataFiledIdValue = new Dictionary<int, object>();

            //  
            if (!string.IsNullOrWhiteSpace(action.NotificationMessage))
            {

                string sqlStatementTempalte = action.NotificationMessage;


                List<DbParameter> sqlParamterList = new List<DbParameter>();

                string sqlStateMent = AppProjectWorkFlowDataFormSynchBL.ReplaceSQlParamterWithActualValue(allFreshRootValue, action.NotificationMessage, sqlParamterList);

                if (string.IsNullOrWhiteSpace(sqlStateMent))
                {
                    toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_ExcusteSqlStatmentActionWithRootValue_Error", ValidationItemType.Error, "Invalid Paramter"));

                }
                else
                {
                    DatabaseFixture databaseFixtureInstance = AppMetaDataBL.GetNewInstanceDatbaseFixtureWtihSchmaOwner(transactionExDto.DataSourceFrom, null);

                    //string errorMsg = query;

                    List<object> resultSet = new List<object>();
                    string errorMsg = AppMetaDataBL.ExecSQlCommand(databaseFixtureInstance, sqlStateMent, false, sqlParamterList, resultSet);

                    if (!string.IsNullOrWhiteSpace(errorMsg))
                    {


                        toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_ExcusteSqlStatmentActionWithRootValue_Error", ValidationItemType.Error, errorMsg));

                    }
                    else
                    {
                        if (action.ActionAttribute.AssignSqlResultToFiledId.HasValue && toReturn.Object.FormData != null)
                        {
                            if (resultSet.Count == 1)
                            {
                                var appformDataDto = toReturn.Object.FormData;
                                AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appformDataDto.TransactionId);

                                if (action.ActionAttribute.AssignSqlResultToFiledId.HasValue)
                                {
                                    int? fieldId = action.ActionAttribute.AssignSqlResultToFiledId;

                                    if (fieldId.HasValue && appTransactionExDto.DictAllTransactionField.ContainsKey(fieldId.Value))
                                    {
                                        var transfieldDto = appTransactionExDto.DictAllTransactionField[fieldId.Value];
                                        int unitId = transfieldDto.TransactionUnitId;

                                        if (appformDataDto.RootUnitId.ToString() == unitId.ToString())
                                        {
                                            AppMasterDetailFormDataLoadBL.AssignOneFormFieldValueByType(appformDataDto.DictOneToOneFields, transfieldDto, resultSet[0]);
                                        }
                                        else if (appformDataDto.DictSiblingOneToOneFields.ContainsKey(unitId.ToString()))
                                        {
                                            AppMasterDetailFormDataLoadBL.AssignOneFormFieldValueByType(appformDataDto.DictSiblingOneToOneFields[unitId.ToString()], transfieldDto, resultSet[0]); ;
                                        }
                                    }
                                }
                            }

                        }

                    }

                    //using (DataAccessAdapter adpter = AppTenantAdapterBL.GetTenantAdapter())
                    //{
                    //    try
                    //    {

                    //        // for what?
                    //        // why need  DictDataFiledIdColumnName , get data from Database, upload form data , then save the form data
                    //        if (action.ActionAttribute != null && !action.ActionAttribute.DictDataFiledIdColumnName.IsEmpty())
                    //        {
                    //            var dictFiedMapToDataTAbleColumnName = action.ActionAttribute.DictDataFiledIdColumnName;


                    //            DataTable dataTableResult = adpter.ExecuteDataTableRetrievalQuery(sqlStateMent, sqlParamterList);
                    //            if (dataTableResult.Rows.Count > 0)
                    //            {
                    //                DataRow oneRow = dataTableResult.Rows[0];
                    //                foreach (int rootSiblingFiedId in dictFiedMapToDataTAbleColumnName.Keys)
                    //                {
                    //                    string dtColumnName = dictFiedMapToDataTAbleColumnName[rootSiblingFiedId];
                    //                    var cellValue = oneRow[dtColumnName];
                    //                    if (DBNull.Value.Equals(cellValue))
                    //                    {
                    //                        dictUpdaetDataFiledIdValue[rootSiblingFiedId] = null;

                    //                    }
                    //                    else
                    //                    {

                    //                        dictUpdaetDataFiledIdValue[rootSiblingFiedId] = cellValue;
                    //                    }


                    //                }

                    //            }

                    //        }
                    //        else
                    //        {
                    //            object callSingResult = adpter.ExecuteScalarQuery(sqlStateMent, sqlParamterList);

                    //            if (action.ActionAttribute.AssignSqlResultToFiledId.HasValue && toReturn.Object.FormData != null)
                    //            {
                    //                var appformDataDto = toReturn.Object.FormData;
                    //                AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appformDataDto.TransactionId);

                    //                if (action.ActionAttribute.AssignSqlResultToFiledId.HasValue)
                    //                {
                    //                    int? fieldId = action.ActionAttribute.AssignSqlResultToFiledId;

                    //                    if (fieldId.HasValue && appTransactionExDto.DictAllTransactionField.ContainsKey(fieldId.Value))
                    //                    {
                    //                        var transfieldDto = appTransactionExDto.DictAllTransactionField[fieldId.Value];
                    //                        int unitId = transfieldDto.TransactionUnitId;


                    //                        if (appformDataDto.RootUnitId.ToString() == unitId.ToString())
                    //                        {
                    //                            AppMasterDetailFormDataLoadBL.AssignOneFormFieldValueByType(appformDataDto.DictOneToOneFields, transfieldDto, callSingResult);
                    //                        }
                    //                        else if (appformDataDto.DictSiblingOneToOneFields.ContainsKey(unitId.ToString()))
                    //                        {
                    //                            AppMasterDetailFormDataLoadBL.AssignOneFormFieldValueByType(appformDataDto.DictSiblingOneToOneFields[unitId.ToString()], transfieldDto, callSingResult); ;
                    //                        }
                    //                    }
                    //                }
                    //            }
                    //        }

                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_ExcusteSqlStatmentActionWithRootValue_Error", ValidationItemType.Error, ex.Message));

                    //    }


                    //}
                }

            }

            if (!dictUpdaetDataFiledIdValue.IsEmpty())
            {
                var aInputFormData = toReturn.Object.FormData;

                AppTransactionFormulaBL.UpdateRootAndSiblingDbFieldNameValueFromFiedIdValue(aInputFormData, null, dictUpdaetDataFiledIdValue);
            }
            return dictUpdaetDataFiledIdValue;
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
                    mandatoryUserIdList,
                    false,
                    message.ToList);
            }
        }

        private static void SendSmsMessages(List<AppMessageDto> smsMessageList)
        {
            foreach (AppMessageDto smsMessage in smsMessageList)
            {
                string phoneNumber = smsMessage.ToList;
                string messageText = smsMessage.Message;

                if (!string.IsNullOrWhiteSpace(phoneNumber) && !string.IsNullOrWhiteSpace(messageText))
                {
                    EmailHelper.PushToClientSMS(phoneNumber, messageText);
                }
            }
        }

        private static void InitChildCommandProperties(EntityCollection<AppProjectWorkFlowActionEntity> commandActionList, AppProjectWorkFlowActionExDto actionDto)
        {
            try
            {
                if (actionDto.ActionAttribute != null && actionDto.ActionAttribute.ChildActionList != null)
                {
                    List<ChildTransactionCommandDto> needToRemoveChildCommandList = new List<ChildTransactionCommandDto>();

                    foreach (ChildTransactionCommandDto childActionDto in actionDto.ActionAttribute.ChildActionList)
                    {
                        childActionDto.CommandDisplay = "";
                        childActionDto.IsBatchCommand = false;


                        if (childActionDto.CommandId.HasValue)
                        {
                            if (childActionDto.ExternalTransactionId.HasValue)
                            {
                                var exTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(childActionDto.ExternalTransactionId.Value);

                                if (exTransactionExDto != null && exTransactionExDto.CommandActionList != null)
                                {
                                    var exCommand = exTransactionExDto.CommandActionList.FirstOrDefault(o => (int)o.Id == childActionDto.CommandId.Value);
                                    if (exCommand != null)
                                    {
                                        childActionDto.CommandDisplay = "Extrenal: " + exCommand.Name;

                                        childActionDto.IsBatchCommand = exCommand.ActionAttribute != null && exCommand.ActionAttribute.IsBatchCommand;
                                    }
                                    else
                                    {
                                        childActionDto.CommandId = null;
                                        needToRemoveChildCommandList.Add(childActionDto);
                                    }
                                }
                            }
                            else
                            {
                                var commandEntity = commandActionList.FirstOrDefault(o => o.WorkFlowActionId == childActionDto.CommandId.Value);
                                if (commandEntity != null)
                                {
                                    string typeString = "User Defined";

                                    if (commandEntity.ActionType.HasValue &&
                                        (commandEntity.ActionType.Value == (int)EmAppTransactionCommandType.DataModelAllFormulaCalculation
                                        || commandEntity.ActionType.Value == (int)EmAppTransactionCommandType.GenerateMatrix
                                        || commandEntity.ActionType.Value == (int)EmAppTransactionCommandType.SaveAs
                                        || commandEntity.ActionType.Value == (int)EmAppTransactionCommandType.Print
                                        || commandEntity.ActionType.Value == (int)EmAppTransactionCommandType.Save
                                        || commandEntity.ActionType.Value == (int)EmAppTransactionCommandType.refresh
                                        || commandEntity.ActionType.Value == (int)EmAppTransactionCommandType.CloseFormWindow
                                        ))
                                    {
                                        typeString = "Sys Built-in";
                                    }

                                    var commandDto = AppProjectWorkFlowActionConverter.ConvertEntityToExDto(commandEntity);
                                    childActionDto.CommandDisplay = typeString + ": " + commandDto.Name;


                                    childActionDto.IsBatchCommand = commandDto.ActionAttribute != null && commandDto.ActionAttribute.IsBatchCommand;
                                }
                                else
                                {
                                    childActionDto.CommandId = null;
                                    needToRemoveChildCommandList.Add(childActionDto);
                                }
                            }
                        }
                    }


                    foreach (var needToRemoveChildCommand in needToRemoveChildCommandList)
                    {
                        actionDto.ActionAttribute.ChildActionList.Remove(needToRemoveChildCommand);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }


        private static ValidationResult ExecuteBatchCommand_LoopOnDataSetOrSearch(AppMasterDetailDto aInputFormData, ValidationResult aValidationResult, AppProjectWorkFlowActionExDto commandDto, AppProjectWorkFlowActionExDto rootCommandDto)
        {
            if (commandDto.ActionAttribute.BatchCommandSourceFromType.HasValue)
            {
                int transactionId = aInputFormData.TransactionId;
                var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);

                Dictionary<int, object> dictOneRowFiedIdValue = AppTransactionFormulaBL.ConvertUnitOneRowDbFileNameToFileId(transactionExDto.RootMasterUnit, aInputFormData.DictOneToOneFields);

                MergeRootCommandFieldValueToDictFieldIdAndValue(rootCommandDto, dictOneRowFiedIdValue);



                if (commandDto.ActionAttribute.BatchCommandSourceFromType.Value == (int)EmAppBatchCommandSourceFrom.DataSet)
                {
                    if (commandDto.ActionAttribute.BatchCommandDataSetId.HasValue && !string.IsNullOrWhiteSpace(commandDto.ActionAttribute.BatchCommandSourceDataSetFieldName))
                    {
                        int datasetId = commandDto.ActionAttribute.BatchCommandDataSetId.Value;
                        string datasetColumnName = commandDto.ActionAttribute.BatchCommandSourceDataSetFieldName;

                        AppDataSetEntity aAppDataSetEntity = AppDataSetBL.RetrieveOneAppDataSetEntity(datasetId);

                        if (aAppDataSetEntity.QueryType.Value == (int)EmAppDataServiceType.QueryText)
                        {
                            DatabaseFixture databaseFixtureInstance = AppMetaDataBL.GetNewInstanceDatbaseFixtureWtihSchmaOwner(aAppDataSetEntity.DataSourceFrom, null);
                            DataTable dataTable = databaseFixtureInstance.RetriveDataTable(aAppDataSetEntity.QueryText, new List<System.Data.Common.DbParameter>());

                            if (dataTable != null)
                            {
                                bool isHaveError = false;
                                int loopCount = 0;
                                foreach (DataRow dataRow in dataTable.Rows)
                                {
                                    loopCount++;
                                    string batchLoopCountDispay = loopCount.ToString() + dataTable.Rows.Count.ToString() + "/";

                                    string targetPkValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dataRow[datasetColumnName]);
                                    var result_oneBatchInstance = ExecuteBatchCommandOneInstance(transactionId, commandDto, targetPkValue, rootCommandDto, batchLoopCountDispay);

                                    if (rootCommandDto != null && rootCommandDto.ActionAttribute.EmAppValidationResultPreference.HasValue
                                        && rootCommandDto.ActionAttribute.EmAppValidationResultPreference.Value == EmAppValidationResultPreference.ShowResultStatusOnly)
                                    {
                                        isHaveError = true;
                                    }
                                    else
                                    {
                                        aValidationResult.Merge(result_oneBatchInstance);
                                    }

                                }

                                if (isHaveError)
                                {
                                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "ExecuteSearchViewLinkTargetBatchCommand_Error", ValidationItemType.Error, "Batch command error on " + commandDto.Id + " - " + commandDto.Name));
                                }

                                //aOperationCallResult.Object = true;
                            }
                            else
                            {
                                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "ExecuteSearchViewLinkTargetBatchCommand_Error", ValidationItemType.Error, "Invalid batch command dataset query result."));
                            }

                        }

                    }
                }
                else if (commandDto.ActionAttribute.BatchCommandSourceFromType.Value == (int)EmAppBatchCommandSourceFrom.Search)
                {
                    if (commandDto.ActionAttribute.BatchCommandSearchId.HasValue && commandDto.ActionAttribute.BatchCommandSourceViewFieldId.HasValue)
                    {
                        int searchId = commandDto.ActionAttribute.BatchCommandSearchId.Value;
                        int searchViewFieldId = commandDto.ActionAttribute.BatchCommandSourceViewFieldId.Value;

                        var searchDto = AppSearchBL.RetrieveOneSearchDto(searchId, false, false);
                        searchDto.ReferenceViewDefinitionDto = searchDto.DefaultView;

                        if (commandDto.ActionAttribute.DictBatchSearchCrietraIdAndTransFieldId != null && commandDto.ActionAttribute.DictBatchSearchCrietraIdAndTransFieldId.Keys.Count > 0)
                        {
                            var dictCrietraIdAndTransFieldId = commandDto.ActionAttribute.DictBatchSearchCrietraIdAndTransFieldId;
                            foreach (SearchCriteriaDto criteriaDto in searchDto.Criterias)
                            {
                                if (dictCrietraIdAndTransFieldId.ContainsKey(criteriaDto.SearcDCUID))
                                {
                                    int? transFiledId = dictCrietraIdAndTransFieldId[criteriaDto.SearcDCUID];

                                    if (transFiledId.HasValue && dictOneRowFiedIdValue.ContainsKey(transFiledId.Value))
                                    {
                                        object newValue = dictOneRowFiedIdValue[transFiledId.Value];
                                        criteriaDto.Values.Clear();
                                        criteriaDto.Value = newValue;
                                    }

                                }
                            }
                        }

                        SearchResultDto searchResult = AppSearchBL.RetrieveSearchResult(searchDto);

                        if (searchResult != null && searchResult.SearchResultRowList != null)
                        {
                            bool isHaveError = false;
                            int loopCount = 0;
                            foreach (StaticSearchResultRowJsonDto resultRowDto in searchResult.SearchResultRowList)
                            {
                                loopCount++;
                                string batchLoopCountDispay = loopCount.ToString() + "/" + searchResult.SearchResultRowList.Count().ToString();

                                string targetPkValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(resultRowDto.DictViewColumnIDKeyValue[searchViewFieldId]);

                                var result_oneBatchInstance = ExecuteBatchCommandOneInstance(transactionId, commandDto, targetPkValue, rootCommandDto, batchLoopCountDispay);

                                if (rootCommandDto != null && rootCommandDto.ActionAttribute.EmAppValidationResultPreference.HasValue
                                       && rootCommandDto.ActionAttribute.EmAppValidationResultPreference.Value == EmAppValidationResultPreference.ShowResultStatusOnly)
                                {
                                    isHaveError = true;
                                }
                                else
                                {
                                    aValidationResult.Merge(result_oneBatchInstance);
                                }
                            }

                            if (isHaveError)
                            {
                                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "ExecuteSearchViewLinkTargetBatchCommand_Error", ValidationItemType.Error, "Batch command error on " + commandDto.Id + " - " + commandDto.Name));
                            }

                            //aOperationCallResult.Object = true;
                        }
                        else
                        {
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "ExecuteSearchViewLinkTargetBatchCommand_Error", ValidationItemType.Error, "Invalid batch command search result."));
                        }

                    }
                }
            }

            return aValidationResult;

        }

        private static ValidationResult ExecuteBatchCommandOneInstance(int transactionId, AppProjectWorkFlowActionExDto commandDto, string targetPkValue, AppProjectWorkFlowActionExDto rootCommandDto, string batchLoopCountDispay)
        {
            ValidationResult aValidationResult = new ValidationResult();

            if (!string.IsNullOrWhiteSpace(targetPkValue))
            {
                rootCommandDto.CurrentExecutingTreeNodeLogicId = commandDto.TreeNodeLogicId;
                OperationCallResult<TransactionCommandActionResultDto> oneResult = AppTransactionCommandBL.ExecuteOneChildCommonadById(
                    (int)commandDto.Id, rootCommandDto,
                   transactionId, targetPkValue, null, null, null, true, null, batchLoopCountDispay);

                if (oneResult.ValidationResult.HasErrors)
                {
                    oneResult.ValidationResult.Items.ForAll(o =>
                    {
                        if (!o.CommandId.HasValue)
                        {
                            o.CommandId = (int)commandDto.Id;
                        }

                        if (!o.TransactionId.HasValue)
                        {
                            o.TransactionId = transactionId;
                        }

                        if (string.IsNullOrWhiteSpace(o.TransactionRId))
                        {
                            o.TransactionRId = targetPkValue;
                        }
                    });

                    aValidationResult.Merge(oneResult.ValidationResult);
                }
            }

            return aValidationResult;
        }





        private static DatabaseFixture _appLogTrackDbFixture;
        private static DatabaseFixture GetAppLogTrackDbFixtureFromCache()
        {
            if (_appLogTrackDbFixture != null)
            {
                return _appLogTrackDbFixture;
            }
            else
            {
                int? defaultDataSourceRegId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
                DatabaseFixture _appLogTrackDbFixture = AppMetaDataBL.GetNewInstanceDatbaseFixtureWtihSchmaOwner(defaultDataSourceRegId, null);

                return _appLogTrackDbFixture;
            }
        }



        private static string _appLogTrackInsertQuery;
        private static string GetAppLogTrackInsertQueryFromCache()
        {
            if (!string.IsNullOrWhiteSpace(_appLogTrackInsertQuery))
            {
                return _appLogTrackInsertQuery;
            }
            else
            {
                int? defaultDataSourceRegId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();

                DatabaseTable table_AppLogTrack = AppMetaDataBL.GetOneDatabaseTableSchema("AppLogTrack", defaultDataSourceRegId, "");

                SqlWriter sqlWriter = new SqlWriter(table_AppLogTrack, EmSqlType.SqlServer);

                _appLogTrackInsertQuery = sqlWriter.InsertSqlWithoutOutputParameter();

                return _appLogTrackInsertQuery;
            }
        }


        private static string _appBatchLogInsertQuery;
        private static string GetAppBatchLogQueryFromCache()
        {
            if (!string.IsNullOrWhiteSpace(_appBatchLogInsertQuery))
            {
                return _appBatchLogInsertQuery;
            }
            else
            {
                int? defaultDataSourceRegId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();

                DatabaseTable table_AppBatchLog = AppMetaDataBL.GetOneDatabaseTableSchema("AppBatchLog", defaultDataSourceRegId, "");

                SqlWriter sqlWriter = new SqlWriter(table_AppBatchLog, EmSqlType.SqlServer);

                _appBatchLogInsertQuery = sqlWriter.InsertSqlWithoutOutputParameter();

                return _appBatchLogInsertQuery;
            }
        }

        private static void RebuildToSimpleValidationResultBySetting(AppMasterDetailDto aformData, ValidationResult aValidationResult, AppProjectWorkFlowActionExDto commandDto, AppTransactionExDto transactionExDto, string transactionRId)
        {
            if (aValidationResult.Items.Count > 0)
            {
                if (commandDto.ActionAttribute.EmAppValidationResultPreference.HasValue
                    && commandDto.ActionAttribute.EmAppValidationResultPreference.Value == EmAppValidationResultPreference.ShowResultStatusOnly)
                {
                    if (aValidationResult.HasErrors)
                    {
                        aValidationResult.Items.Clear();
                        var item = new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Error", ValidationItemType.Error, "Failed to Execute Command.");
                        item.CommandId = (int)commandDto.Id;
                        item.TransactionId = (int)transactionExDto.Id;
                        item.TransactionRId = transactionRId;
                        aValidationResult.Items.Add(item);
                    }
                    else
                    {
                        //aValidationResult.Items.Clear();
                        //aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Success", ValidationItemType.Message, "Command Execution Successful."));

                    }
                }
            }
        }

        private static int GetAppBatchLogNextSequenceNumber(int transactionId, string transactionRId)
        {
            int sequenceNumber = 1;

            int? maxSequenceNumber = GetAppBatchLogMaxSequenceNumber(transactionId, transactionRId);
            if (maxSequenceNumber.HasValue)
            {
                sequenceNumber = maxSequenceNumber.Value + 1;
            }

            return sequenceNumber;
        }

        private static int? GetAppBatchLogMaxSequenceNumber(int transactionId, string transactionRId)
        {
            int? sequenceNumber = null;

            DatabaseFixture databaseFixtureInstance = GetAppLogTrackDbFixtureFromCache();

            string queryToExecute = $@"select max(SequenceNumber) as [SequenceNumber] from AppBatchLog where transactionId = {transactionId} and transactionRId = '{transactionRId}'";

            var resultDatatable = databaseFixtureInstance.RetriveDataTable(queryToExecute, new List<DbParameter>());

            if (resultDatatable != null && resultDatatable.Rows.Count > 0)
            {
                int? maxSequenceNumber = ControlTypeValueConverter.ConvertValueToInt(resultDatatable.Rows[0]["SequenceNumber"]);
                if (maxSequenceNumber.HasValue)
                {
                    sequenceNumber = maxSequenceNumber.Value;
                }
            }


            return sequenceNumber;
        }


        private static bool CheckIsWorkflowInstanceExist(int transactionId, string transactionRId)
        {
            DatabaseFixture databaseFixtureInstance = GetAppLogTrackDbFixtureFromCache();

            string queryToExecute = $@"select top 1 * from AppBatchLog where transactionId = {transactionId} and transactionRId = '{transactionRId}'";

            var resultDatatable = databaseFixtureInstance.RetriveDataTable(queryToExecute, new List<DbParameter>());

            if (resultDatatable != null && resultDatatable.Rows.Count > 0)
            {
                return true;
            }


            return false;
        }




        private static DataTable GetOneWorkflowLatestAppBatchLogDataTableFromDb(int transactionId, string transactionRId)
        {
            DatabaseFixture databaseFixtureInstance = GetAppLogTrackDbFixtureFromCache();

            string queryToExecute = $@"SELECT TOP 1 [BatchLogId]
                      ,[BatchNumber]
                      ,[Description]
                      ,[TransactionId]
                      ,[TransactionRId]
                      ,[SequenceNumber]
                      ,[StartTime]
                      ,[EndTime]
                      ,[RuntimeFormData]
                      ,[RuntimeWorkflowNodeTreeData]
                      ,[ExecutingCommandLogicTreeNodeId]
                      ,[WorkflowProgressStatus]
                  FROM [AppBatchLog] 
                  where transactionId = {transactionId} and transactionRId = '{transactionRId}'
                  order by SequenceNumber desc
    ";

            DataTable resultDatatable = databaseFixtureInstance.RetriveDataTable(queryToExecute, new List<DbParameter>());

            return resultDatatable;
        }

        private static DataTable GetOneAppBatchLogDataTableFromDbByBatchNumber(string batchNumber)
        {
            DatabaseFixture databaseFixtureInstance = GetAppLogTrackDbFixtureFromCache();

            string queryToExecute = $@"SELECT TOP 1 [BatchLogId]
                      ,[BatchNumber]
                      ,[Description]
                      ,[TransactionId]
                      ,[TransactionRId]
                      ,[SequenceNumber]
                      ,[StartTime]
                      ,[EndTime]
                      ,[RuntimeFormData]
                      ,[RuntimeWorkflowNodeTreeData]
                      ,[ExecutingCommandLogicTreeNodeId]
                      ,[WorkflowProgressStatus]
                  FROM [AppBatchLog] 
                  where BatchNumber = '{batchNumber}'
            ";

            DataTable resultDatatable = databaseFixtureInstance.RetriveDataTable(queryToExecute, new List<DbParameter>());

            return resultDatatable;
        }

        private static void InsertOneAppBatchLogToDb(AppProjectWorkFlowActionExDto rootCommandDto, int transactionId, string transactionRId)
        {
            if (string.IsNullOrWhiteSpace(transactionRId) || transactionRId == "null" || transactionRId == "undefined")
            {
                transactionRId = "";
            }

            DatabaseFixture databaseFixtureInstance = GetAppLogTrackDbFixtureFromCache();
            transactionRId = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(transactionRId);

            string batchNumber = transactionId + "_" + transactionRId;

            var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);
            int sequenceNumber = 1;

            string description = transactionExDto.TransactionName + ": " + rootCommandDto.Name + "_" + transactionRId + "_" + sequenceNumber;

            List<AppTransactionFieldExDto> mappingToBatchLogDescriptionFieldList = transactionExDto.RootMasterUnit.AppTransactionFieldList
                .Where(o => o.IsLogicalDisplay.HasValue && o.IsLogicalDisplay.Value && !string.IsNullOrWhiteSpace(o.DataBaseFieldName)).OrderBy(o => o.SortOrder).ToList();

            if (mappingToBatchLogDescriptionFieldList.Count > 0)
            {

                List<string> fieldValues = new List<string>();
                foreach (var fieldDto in mappingToBatchLogDescriptionFieldList)
                {
                    string fieldValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(rootCommandDto.RootTransactionFormData.DictOneToOneFields[fieldDto.DataBaseFieldName]);
                    if (!string.IsNullOrWhiteSpace(fieldValue))
                    {
                        fieldValues.Add(fieldValue);
                    }
                }

                string newDescription = string.Join(" ", fieldValues);

                if (!string.IsNullOrWhiteSpace(newDescription))
                {
                    description = newDescription;
                }
            }


            string formDataString = "";
            string nodeTreeDataString = "";

            if (rootCommandDto.RootTransactionFormData != null)
            {
                formDataString = JsonConvert.SerializeObject(rootCommandDto.RootTransactionFormData);
            }

            if (rootCommandDto.WorkflowCommandNodeTree != null)
            {
                nodeTreeDataString = JsonConvert.SerializeObject(rootCommandDto.WorkflowCommandNodeTree);
            }



            List<DbParameter> paraList = new List<DbParameter>();

            string insertQuery = GetAppBatchLogQueryFromCache();
            DbParameter pSequenceNumber = databaseFixtureInstance.CreateParameter("@SequenceNumber");
            pSequenceNumber.Value = sequenceNumber;
            paraList.Add(pSequenceNumber);

            DbParameter pBatchNumber = databaseFixtureInstance.CreateParameter("@BatchNumber");
            pBatchNumber.Value = batchNumber;
            paraList.Add(pBatchNumber);

            DbParameter pDescription = databaseFixtureInstance.CreateParameter("@Description");
            pDescription.Value = description;
            paraList.Add(pDescription);

            DbParameter pTransactionId = databaseFixtureInstance.CreateParameter("@TransactionId");
            pTransactionId.Value = transactionId;
            paraList.Add(pTransactionId);

            DbParameter pTransactionRId = databaseFixtureInstance.CreateParameter("@TransactionRId");
            pTransactionRId.Value = transactionRId;
            paraList.Add(pTransactionRId);

            DbParameter pStartTime = databaseFixtureInstance.CreateParameter("@StartTime");
            pStartTime.Value = DateTime.UtcNow;
            paraList.Add(pStartTime);

            DbParameter pEndTime = databaseFixtureInstance.CreateParameter("@EndTime");
            pEndTime.Value = DBNull.Value;
            paraList.Add(pEndTime);

            DbParameter pRuntimeFormData = databaseFixtureInstance.CreateParameter("@RuntimeFormData");
            pRuntimeFormData.Value = formDataString;
            paraList.Add(pRuntimeFormData);

            DbParameter pRuntimeWorkflowNodeTreeData = databaseFixtureInstance.CreateParameter("@RuntimeWorkflowNodeTreeData");
            pRuntimeWorkflowNodeTreeData.Value = nodeTreeDataString;
            paraList.Add(pRuntimeWorkflowNodeTreeData);

            DbParameter pExecutingCommandLogicTreeNodeId = databaseFixtureInstance.CreateParameter("@ExecutingCommandLogicTreeNodeId");
            pExecutingCommandLogicTreeNodeId.Value = DBNull.Value;
            paraList.Add(pExecutingCommandLogicTreeNodeId);

            DbParameter pWorkflowProgressStatus = databaseFixtureInstance.CreateParameter("@WorkflowProgressStatus");
            pWorkflowProgressStatus.Value = rootCommandDto.ProgressStatus;
            paraList.Add(pWorkflowProgressStatus);

            DbParameter pNotes = databaseFixtureInstance.CreateParameter("@Notes");
            pNotes.Value = "";
            paraList.Add(pNotes);



            DbParameter pAppCreatedById = databaseFixtureInstance.CreateParameter("@AppCreatedById");
            pAppCreatedById.Value = ServerContext.Instance.CurrentUid;
            paraList.Add(pAppCreatedById);

            DbParameter pAppModifiedByIda = databaseFixtureInstance.CreateParameter("@AppModifiedById");
            pAppModifiedByIda.Value = ServerContext.Instance.CurrentUid;
            paraList.Add(pAppModifiedByIda);

            DbParameter pAppCreatedDate = databaseFixtureInstance.CreateParameter("@AppCreatedDate");
            pAppCreatedDate.Value = System.DateTime.UtcNow;
            paraList.Add(pAppCreatedDate);

            DbParameter pAppModifiedDate = databaseFixtureInstance.CreateParameter("@AppModifiedDate");
            pAppModifiedDate.Value = System.DateTime.UtcNow;
            paraList.Add(pAppModifiedDate);

            DbParameter pAppCreatedByCompanyId = databaseFixtureInstance.CreateParameter("@AppCreatedByCompanyId");
            pAppCreatedByCompanyId.Value = ServerContext.Instance.CurrentCompanyId;
            paraList.Add(pAppCreatedByCompanyId);











            databaseFixtureInstance.ExecuteNonQueryResult(insertQuery, paraList);

            rootCommandDto.BatchNumber = batchNumber;

            SynchonizeAppWorkflowAutomationWithAppBatchLog(ControlTypeValueConverter.ConvertValueToInt(transactionRId));
        }

        private static void UpdateCurrentExecutingCommandAppBatchLogRuntimeData(string treeNodeLogicId, AppProjectWorkFlowActionExDto rootCommandDto)
        {
            string formDataString = "";
            string nodeTreeDataString = "";

            if (rootCommandDto.RootTransactionFormData != null)
            {
                formDataString = JsonConvert.SerializeObject(rootCommandDto.RootTransactionFormData);
            }

            if (rootCommandDto.WorkflowCommandNodeTree != null)
            {
                nodeTreeDataString = JsonConvert.SerializeObject(rootCommandDto.WorkflowCommandNodeTree);
            }

            UpdateAppBatchLogRuntimeData(rootCommandDto.BatchNumber, formDataString, nodeTreeDataString, treeNodeLogicId);
        }

        private static void UpdateAppBatchLogRuntimeData(string batchNumber, string formDataString, string nodeTreeDataString, string executingCommandLogicTreeNodeId)
        {
            if (!string.IsNullOrWhiteSpace(batchNumber))
            {
                DatabaseFixture databaseFixtureInstance = GetAppLogTrackDbFixtureFromCache();

                List<DbParameter> paraList = new List<DbParameter>();

                DbParameter pRuntimeFormData = databaseFixtureInstance.CreateParameter("@RuntimeFormData");
                pRuntimeFormData.Value = formDataString;
                paraList.Add(pRuntimeFormData);

                DbParameter pRuntimeWorkflowNodeTreeData = databaseFixtureInstance.CreateParameter("@RuntimeWorkflowNodeTreeData");
                pRuntimeWorkflowNodeTreeData.Value = nodeTreeDataString;
                paraList.Add(pRuntimeWorkflowNodeTreeData);

                DbParameter pExecutingCommandLogicTreeNodeId = databaseFixtureInstance.CreateParameter("@ExecutingCommandLogicTreeNodeId");

                // Cause Excetpyion !!! toDO
                pExecutingCommandLogicTreeNodeId.Value = "";
                paraList.Add(pExecutingCommandLogicTreeNodeId);

                //DbParameter pWorkflowProgressStatus = databaseFixtureInstance.CreateParameter("@WorkflowProgressStatus");
                //pWorkflowProgressStatus.Value = EmAppWorkflowProgressStatus.Started.ToString();
                //paraList.Add(pWorkflowProgressStatus);


                string queryToExecute = $@"update AppBatchLog set 
                    [RuntimeFormData] = @RuntimeFormData 
                    ,[RuntimeWorkflowNodeTreeData] = @RuntimeWorkflowNodeTreeData 
                    ,[ExecutingCommandLogicTreeNodeId] = @ExecutingCommandLogicTreeNodeId 
                    where BatchNumber = '{batchNumber}'";

                databaseFixtureInstance.ExecuteNonQueryResult(queryToExecute, paraList);
            }

        }


        private static void SetAppBatchLogEndTime(string batchNumber)
        {
            if (!string.IsNullOrWhiteSpace(batchNumber))
            {
                DatabaseFixture databaseFixtureInstance = GetAppLogTrackDbFixtureFromCache();

                List<DbParameter> paraList = new List<DbParameter>();

                DbParameter pEndTime = databaseFixtureInstance.CreateParameter("@EndTime");
                pEndTime.Value = DateTime.UtcNow;
                paraList.Add(pEndTime);

                string queryToExecute = $@"update AppBatchLog set [EndTime] = @EndTime where BatchNumber = '{batchNumber}'";

                databaseFixtureInstance.ExecuteNonQueryResult(queryToExecute, paraList);
            }

        }


        private static void SetAppBatchLogProgressStatus(string batchNumber, string workflowProgressStatus, string notes, int? workflowAutomationId)
        {
            if (!string.IsNullOrWhiteSpace(batchNumber))
            {
                DatabaseFixture databaseFixtureInstance = GetAppLogTrackDbFixtureFromCache();

                List<DbParameter> paraList = new List<DbParameter>();

                DbParameter pWorkflowProgressStatus = databaseFixtureInstance.CreateParameter("@WorkflowProgressStatus");
                pWorkflowProgressStatus.Value = workflowProgressStatus;
                paraList.Add(pWorkflowProgressStatus);

                DbParameter pNotes = databaseFixtureInstance.CreateParameter("@Notes");
                pNotes.Value = notes;
                paraList.Add(pNotes);

                string queryToExecute = $@"update AppBatchLog set [WorkflowProgressStatus] = @WorkflowProgressStatus, [Notes] = @Notes where BatchNumber = '{batchNumber}'";

                databaseFixtureInstance.ExecuteNonQueryResult(queryToExecute, paraList);

                SynchonizeAppWorkflowAutomationWithAppBatchLog(workflowAutomationId);
            }

        }


        private static void SynchonizeAppWorkflowAutomationWithAppBatchLog(int? WorkflowAutomationId)
        {
            if (WorkflowAutomationId.HasValue)
            {
                DatabaseFixture databaseFixtureInstance = GetAppLogTrackDbFixtureFromCache();

                List<DbParameter> paraList = new List<DbParameter>();

                string queryToExecute = $@"update AppWorkflowAutomation 
	                    set Notes = AppBatchLog.Notes,
		                    StartTime = AppBatchLog.StartTime,
		                    EndTime = AppBatchLog.EndTime,
		                    WorkflowProgressStatus = AppBatchLog.WorkflowProgressStatus
                    FROM AppWorkflowAutomation inner join AppBatchLog 
	                    on (CAST(AppWorkflowAutomation.WorkflowAutomationId as nvarchar(4000)) = AppBatchLog.TransactionRId and AppWorkflowAutomation.WorkflowTransactionId = AppBatchLog.TransactionId)
                    WHERE WorkflowAutomationId = '{WorkflowAutomationId.Value}'";

                databaseFixtureInstance.ExecuteNonQueryResult(queryToExecute, paraList);
            }

        }



        private static void InsertOneAppLogTrackToDb(string description, int commandId, int transactionId, string transactionRId, string statusCode, DateTime logDate, string message, string batchNumber, string commandLogicTreeNodeId)
        {
            DatabaseFixture databaseFixtureInstance = GetAppLogTrackDbFixtureFromCache();
            string insertQuery = GetAppLogTrackInsertQueryFromCache();

            var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);

            List<DbParameter> paraList = new List<DbParameter>();

            DbParameter pBatchNumber = databaseFixtureInstance.CreateParameter("@BatchNumber");
            pBatchNumber.Value = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(batchNumber);
            paraList.Add(pBatchNumber);

            DbParameter pCatalogue = databaseFixtureInstance.CreateParameter("@Catalogue");
            pCatalogue.Value = "Command";
            paraList.Add(pCatalogue);

            DbParameter pDescription = databaseFixtureInstance.CreateParameter("@Description");
            pDescription.Value = description;
            paraList.Add(pDescription);

            DbParameter pStatusCode = databaseFixtureInstance.CreateParameter("@StatusCode");
            pStatusCode.Value = statusCode;
            paraList.Add(pStatusCode);

            DbParameter pMessage = databaseFixtureInstance.CreateParameter("@Message");
            pMessage.Value = message;
            paraList.Add(pMessage);

            DbParameter pOtherInfo = databaseFixtureInstance.CreateParameter("@OtherInfo");
            pOtherInfo.Value = "";
            paraList.Add(pOtherInfo);

            DbParameter pLogDate = databaseFixtureInstance.CreateParameter("@LogDate");
            pLogDate.Value = logDate;
            paraList.Add(pLogDate);

            DbParameter pCommandId = databaseFixtureInstance.CreateParameter("@CommandId");
            pCommandId.Value = commandId;
            paraList.Add(pCommandId);

            DbParameter pTransactionId = databaseFixtureInstance.CreateParameter("@TransactionId");
            pTransactionId.Value = transactionId;
            paraList.Add(pTransactionId);

            DbParameter pTransactionName = databaseFixtureInstance.CreateParameter("@TransactionName");
            pTransactionName.Value = transactionExDto.TransactionName;
            paraList.Add(pTransactionName);

            DbParameter pTransactionRId = databaseFixtureInstance.CreateParameter("@TransactionRId");
            pTransactionRId.Value = transactionRId;
            paraList.Add(pTransactionRId);

            //Wrong value !!!
            DbParameter pCommandLogicTreeNodeId = databaseFixtureInstance.CreateParameter("@CommandLogicTreeNodeId");
            pCommandLogicTreeNodeId.Value = "";
            paraList.Add(pCommandLogicTreeNodeId);



            DbParameter pAppCreatedById = databaseFixtureInstance.CreateParameter("@AppCreatedById");
            pAppCreatedById.Value = ServerContext.Instance.CurrentUid;
            paraList.Add(pAppCreatedById);

            DbParameter pAppModifiedByIda = databaseFixtureInstance.CreateParameter("@AppModifiedById");
            pAppModifiedByIda.Value = ServerContext.Instance.CurrentUid;
            paraList.Add(pAppModifiedByIda);

            DbParameter pAppCreatedDate = databaseFixtureInstance.CreateParameter("@AppCreatedDate");
            pAppCreatedDate.Value = System.DateTime.UtcNow;
            paraList.Add(pAppCreatedDate);

            DbParameter pAppModifiedDate = databaseFixtureInstance.CreateParameter("@AppModifiedDate");
            pAppModifiedDate.Value = System.DateTime.UtcNow;
            paraList.Add(pAppModifiedDate);

            DbParameter pAppCreatedByCompanyId = databaseFixtureInstance.CreateParameter("@AppCreatedByCompanyId");
            pAppCreatedByCompanyId.Value = ServerContext.Instance.CurrentCompanyId;
            paraList.Add(pAppCreatedByCompanyId);





            databaseFixtureInstance.ExecuteNonQueryResult(insertQuery, paraList);
        }

        private static void LogWorkflowChildCommandErrorDetails(AppMasterDetailDto aformData, ValidationResult aValidationResult, int mainTransactionId, AppProjectWorkFlowActionExDto commandDto, AppProjectWorkFlowActionExDto rootCommandDto)
        {
            if (rootCommandDto.IsWorkflowAutomationRootCommand)
            {
                if (aValidationResult.Items.Count > 0)
                {
                    if (commandDto.ActionAttribute.IsLogErrorDetails)
                    {
                        //string description = "Root Command: " + rootCommandDto.Id + " - " + rootCommandDto.Name;

                        //if (commandDto.Id.ToString() != rootCommandDto.Id.ToString())
                        //{
                        //    description += " | " + "Child Command: " + commandDto.Id + " - " + commandDto.Name;
                        //}

                        string description = commandDto.Id + " - " + commandDto.Name;

                        int mainCommandId = (int)commandDto.Id;
                        //int mainTransactionId = (int)transactionExDto.Id;
                        string mainTransactionRId = "";

                        if (aformData != null)
                        {
                            mainTransactionRId = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(aformData.RootPrimaryKeyValue);
                        }


                        foreach (var validationItem in aValidationResult.Items.Where(o => !o.IsChildCommandValidationItem))
                        {
                            int commandId = mainCommandId;
                            int transactionId = mainTransactionId;
                            string transactionRId = mainTransactionRId;

                            if (validationItem.CommandId.HasValue)
                            {
                                commandId = validationItem.CommandId.Value;
                            }

                            if (validationItem.TransactionId.HasValue)
                            {
                                transactionId = validationItem.TransactionId.Value;
                                transactionRId = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(validationItem.TransactionRId);
                            }


                            string statusCode = validationItem.ItemType.ToString();
                            DateTime logDate = validationItem.DateCreated;
                            string message = validationItem.LocalizedMessage;
                            InsertOneAppLogTrackToDb(description, commandId, transactionId, transactionRId, statusCode, logDate, message, rootCommandDto.BatchNumber, commandDto.TreeNodeLogicId);
                        }
                    }

                    UpdateCurrentExecutingCommandAppBatchLogRuntimeData(commandDto.TreeNodeLogicId, rootCommandDto);
                }
            }

        }

        private static void LogWorkflowCommandStart(AppProjectWorkFlowActionExDto commandDto, int transactionId, string transactionRId, AppProjectWorkFlowActionExDto rootCommandDto, string batchLoopCountDispay = "")
        {
            if (rootCommandDto.IsWorkflowAutomationRootCommand)
            {
                if (commandDto.ActionAttribute.IsLogCommandStartEnd)
                {
                    string description = commandDto.Id + " - " + commandDto.Name;

                    string statusCode = ValidationItemType.Message.ToString();
                    DateTime logDate = DateTime.UtcNow;
                    string message = "Command Started";

                    if (!string.IsNullOrWhiteSpace(batchLoopCountDispay))
                    {
                        message += " " + batchLoopCountDispay;
                    }

                    InsertOneAppLogTrackToDb(description, (int)commandDto.Id, transactionId, transactionRId, statusCode, logDate, message, rootCommandDto.BatchNumber, commandDto.TreeNodeLogicId);
                }



                UpdateCurrentExecutingCommandAppBatchLogRuntimeData(commandDto.TreeNodeLogicId, rootCommandDto);
            }
        }



        private static void LogWorkflowCommandEnd(AppProjectWorkFlowActionExDto commandDto, int transactionId, string transactionRId, AppProjectWorkFlowActionExDto rootCommandDto)
        {
            if (rootCommandDto.IsWorkflowAutomationRootCommand)
            {
                if (commandDto.ActionAttribute.IsLogCommandStartEnd)
                {

                    string description = commandDto.Id + " - " + commandDto.Name;

                    string statusCode = ValidationItemType.Message.ToString();
                    DateTime logDate = DateTime.UtcNow;
                    string message = "Command Ended";
                    InsertOneAppLogTrackToDb(description, (int)commandDto.Id, transactionId, transactionRId, statusCode, logDate, message, rootCommandDto.BatchNumber, commandDto.TreeNodeLogicId);
                }

                UpdateCurrentExecutingCommandAppBatchLogRuntimeData(commandDto.TreeNodeLogicId, rootCommandDto);
            }
        }


        private static void PrepareOneWorkflowCommandTreeNode(List<AppProjectWorkFlowActionExDto> workflowCommandList, AppProjectWorkFlowActionExDto aCommandExDto, Dictionary<int, string> dictTransactionIdAndName, string rootTransactionName)
        {
            aCommandExDto.WorkflowChildTreeNodes = new List<AppProjectWorkFlowActionDto>();

            aCommandExDto.TransactionName = rootTransactionName;

            if (aCommandExDto.ExternalTransactionId.HasValue)
            {
                aCommandExDto.TransactionName = "";

                if (dictTransactionIdAndName.ContainsKey(aCommandExDto.ExternalTransactionId.Value))
                {
                    aCommandExDto.TransactionName = dictTransactionIdAndName[aCommandExDto.ExternalTransactionId.Value];
                }
            }


            if (aCommandExDto.ActionType.HasValue && aCommandExDto.ActionType.Value == (int)EmAppTransactionCommandType.CompositionCommand
                && aCommandExDto.ActionAttribute != null && aCommandExDto.ActionAttribute.ChildActionList != null)
            {

                aCommandExDto.DisplayName = aCommandExDto.Name + " [ ]";


                int childCount = 0;
                foreach (var childTransactionCommandDto in aCommandExDto.ActionAttribute.ChildActionList.Where(o => o.CommandId.HasValue).OrderBy(o => o.Sort))
                {
                    childCount++;

                    if (childTransactionCommandDto.ExternalTransactionId.HasValue)
                    {
                        var externalTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(childTransactionCommandDto.ExternalTransactionId.Value);
                        AppProjectWorkFlowActionExDto childCommandDto = AppTransactionCommandBL.RetrieveOneTransactionCommandExDto(childTransactionCommandDto.CommandId.Value);

                        if (childCommandDto != null)
                        {
                            childCommandDto = childCommandDto.DeepCopy();
                            childCommandDto.ExternalTransactionId = childTransactionCommandDto.ExternalTransactionId;
                            childCommandDto.ActionFlowOrder = childTransactionCommandDto.Sort;
                            childCommandDto.ParentTreeNodeCommandId = ControlTypeValueConverter.ConvertValueToInt(aCommandExDto.Id);
                            childCommandDto.DisplayName = childCommandDto.Name;
                            childCommandDto.ProgressStatus = "";
                            childCommandDto.ErrorMessage = "";
                            childCommandDto.TreeNodeLogicId = aCommandExDto.TreeNodeLogicId + "|" + (childCount + "_" + childCommandDto.Id);

                            aCommandExDto.WorkflowChildTreeNodes.Add(childCommandDto);

                            PrepareOneWorkflowCommandTreeNode(externalTransactionExDto.CommandActionList, childCommandDto, dictTransactionIdAndName, rootTransactionName);
                        }
                    }
                    else
                    {
                        AppProjectWorkFlowActionExDto childCommandDto = workflowCommandList.FirstOrDefault(o => (int)o.Id == childTransactionCommandDto.CommandId.Value);

                        if (childCommandDto != null)
                        {
                            childCommandDto = childCommandDto.DeepCopy();
                            if (aCommandExDto.ExternalTransactionId.HasValue)
                            {
                                childCommandDto.ExternalTransactionId = aCommandExDto.ExternalTransactionId;
                            }

                            childCommandDto.ActionFlowOrder = childTransactionCommandDto.Sort;
                            childCommandDto.ParentTreeNodeCommandId = ControlTypeValueConverter.ConvertValueToInt(aCommandExDto.Id);
                            childCommandDto.DisplayName = childCommandDto.Name;
                            childCommandDto.TreeNodeLogicId = aCommandExDto.TreeNodeLogicId + "|" + (childCount + "_" + childCommandDto.Id);

                            aCommandExDto.WorkflowChildTreeNodes.Add(childCommandDto);
                            PrepareOneWorkflowCommandTreeNode(workflowCommandList, childCommandDto, dictTransactionIdAndName, rootTransactionName);
                        }
                    }
                }
            }
        }

        private static string ExecuteLinkTargetCommand_GetOnResultRowTargetPkValue(AppFormLinkTargetDto linkTargetDto, StaticSearchResultRowJsonDto resultRowDto)
        {
            string targetPkValue = "";

            string srcValue1 = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(resultRowDto.DictViewColumnIDKeyValue[linkTargetDto.SourceViewColumnId1.Value]);

            if (!string.IsNullOrWhiteSpace(srcValue1))
            {
                if (linkTargetDto.OtherSettingsDto.IsLinkToComsumeApiTransaction)
                {
                    if (!string.IsNullOrWhiteSpace(linkTargetDto.TargetColumn1))
                    {
                        targetPkValue = AppMasterDetailApiFormDataLoadBL.ApiRootKeyPrefix + linkTargetDto.TargetColumn1 + ':' + srcValue1;
                    }
                }
                else
                {
                    targetPkValue = srcValue1;
                }
            }

            if (linkTargetDto.SourceViewColumnId2.HasValue && !string.IsNullOrWhiteSpace(linkTargetDto.TargetColumn2))
            {
                if (linkTargetDto.OtherSettingsDto.IsLinkToComsumeApiTransaction)
                {
                    string srcValue2 = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(resultRowDto.DictViewColumnIDKeyValue[linkTargetDto.SourceViewColumnId2.Value]);

                    if (!string.IsNullOrWhiteSpace(srcValue2) && !string.IsNullOrWhiteSpace(linkTargetDto.TargetColumn2))
                    {
                        if (string.IsNullOrWhiteSpace(targetPkValue))
                        {
                            targetPkValue = AppMasterDetailApiFormDataLoadBL.ApiRootKeyPrefix + linkTargetDto.TargetColumn2 + ':' + srcValue2;
                        }
                        else
                        {
                            targetPkValue += AppMasterDetailApiFormDataLoadBL.ApiRootKeySplit + linkTargetDto.TargetColumn2 + ':' + srcValue2;
                        }

                    }
                }
            }

            if (linkTargetDto.SourceViewColumnId3.HasValue && !string.IsNullOrWhiteSpace(linkTargetDto.TargetColumn3))
            {
                if (linkTargetDto.OtherSettingsDto.IsLinkToComsumeApiTransaction)
                {
                    string srcValue3 = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(resultRowDto.DictViewColumnIDKeyValue[linkTargetDto.SourceViewColumnId3.Value]);

                    if (!string.IsNullOrWhiteSpace(srcValue3) && !string.IsNullOrWhiteSpace(linkTargetDto.TargetColumn3))
                    {
                        if (string.IsNullOrWhiteSpace(targetPkValue))
                        {
                            targetPkValue = AppMasterDetailApiFormDataLoadBL.ApiRootKeyPrefix + linkTargetDto.TargetColumn3 + ':' + srcValue3;
                        }
                        else
                        {
                            targetPkValue += AppMasterDetailApiFormDataLoadBL.ApiRootKeySplit + linkTargetDto.TargetColumn3 + ':' + srcValue3;
                        }

                    }
                }
            }

            return targetPkValue;
        }


        private static AppProjectWorkFlowActionDto FindOneWorkflowCommandTreeNodeByLogicId(List<AppProjectWorkFlowActionDto> workflowCommandList, string logicId)
        {
            if (workflowCommandList != null && !string.IsNullOrWhiteSpace(logicId))
            {
                foreach (var commandDto in workflowCommandList)
                {
                    if (commandDto.TreeNodeLogicId == logicId)
                    {
                        return commandDto;
                    }
                    else
                    {
                        if (commandDto.WorkflowChildTreeNodes != null && commandDto.WorkflowChildTreeNodes.Count > 0)
                        {
                            var childNodeFound = FindOneWorkflowCommandTreeNodeByLogicId(commandDto.WorkflowChildTreeNodes, logicId);

                            if (childNodeFound != null)
                            {
                                return childNodeFound;
                            }
                        }
                    }
                }
            }

            return null;
        }


        private static void PrepareWorkflowDebugTestData(AppMasterDetailDto aformData, AppProjectWorkFlowActionExDto rootCommandDto)
        {
            if (rootCommandDto.ActionAttribute != null && rootCommandDto.ActionAttribute.ChildActionList != null)
            {
                foreach (var rootChildAction in rootCommandDto.ActionAttribute.ChildActionList)
                {
                    rootChildAction.IsSkip = true;
                }

                var needToExecuteChildAction = rootCommandDto.ActionAttribute.ChildActionList.FirstOrDefault(rootChildAction => rootChildAction.CommandId.HasValue && rootChildAction.CommandId.Value == aformData.DebugWorkflowRootChildCommandId.Value);

                if (needToExecuteChildAction != null)
                {
                    needToExecuteChildAction.IsSkip = false;
                }
            }
        }

        public static OperationCallResult<bool> CreateWorkflowWindowsSchedulerTask(AppWinSchedulerTaskDto appWinSchedulerTaskDto)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (appWinSchedulerTaskDto.TransactionId.HasValue)
            {
                var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appWinSchedulerTaskDto.TransactionId);

                if (transactionExDto.TransactionOrganizedType.HasValue && transactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.MasterDetail
                    && transactionExDto.BusinessScopeId.HasValue && transactionExDto.BusinessScopeId.Value == (int)EmAppTransactionScopeUsage.WorkflowAutomation)
                {
                    //appWinSchedulerTaskDto.TaskName = transactionExDto.TransactionName;
                    appWinSchedulerTaskDto.TaskToRun = @"C:\temp\AppScheduler\AppWinSchedule.exe -workflow " + appWinSchedulerTaskDto.TransactionId.Value;
                    appWinSchedulerTaskDto.TaskFolder = "\\AppSchedulerTask";
                    appWinSchedulerTaskDto.RunAsUser = "System";

                    //appWinSchedulerTaskDto.StartTime = "09:00"; // Format: HH:mm                     
                    //appWinSchedulerTaskDto.ScheduleType = "daily";

                    //appWinSchedulerTaskDto.FullPath = $"{appWinSchedulerTaskDto.TaskFolder}\\{appWinSchedulerTaskDto.TaskName}";

                    return CreateWindowsSchedulerTask(appWinSchedulerTaskDto);
                }
                else
                {
                    aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_CreateWorkflowWindowsSchedulerTask_Error", ValidationItemType.Error, "Invalid workflow transaction."));
                }
            }
            else
            {
                aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_CreateWorkflowWindowsSchedulerTask_Error", ValidationItemType.Error, "Invalid workflow transaction."));
            }

            return aOperationCallResult;
        }




        // Command to create the scheduled task

        /// create: Creates a new scheduled task.
        /// tn: Specifies the task name.
        /// tr: Specifies the task's action (the path to the executable).
        /// sc: Specifies the schedule type(daily, in this case).
        /// st: Specifies the start time.
        /// f: Forces the creation of the task if it already exists.
        /// rl: Specifies the run level(e.g., highest).
        /// ru: Specifies the user account under which the task should run(e.g., System).
        /// 


        /*               

         Available /sc Schedule Types

            MINUTE
                Description: Runs the task every specified number of minutes.
                Example: /sc MINUTE /mo 5 (runs every 5 minutes)

            HOURLY
                Description: Runs the task every specified number of hours.
                Example: /sc HOURLY /mo 1 (runs every hour)

            DAILY
                Description: Runs the task every specified number of days.
                Example: /sc DAILY /mo 1 (runs every day)

            WEEKLY
                Description: Runs the task on specified days of the week.
                Example: /sc WEEKLY /mo 1 /d MON,WED,FRI (runs every week on Monday, Wednesday, and Friday)

            MONTHLY
                Description: Runs the task on specified days of the month.
                Example: /sc MONTHLY /mo 1 /d 1,15 (runs on the 1st and 15th of every month)

            ONCE
                Description: Runs the task once at a specified time.
                Example: /sc ONCE /st 12:00 (runs once at 12:00 PM)

            ONSTART
                Description: Runs the task every time the system starts.
                Example: /sc ONSTART (runs at every system start)

            ONLOGON
                Description: Runs the task every time the user logs on.
                Example: /sc ONLOGON (runs at every user logon)

            ONIDLE
                Description: Runs the task when the system is idle for a specified time.
                Example: /sc ONIDLE (runs when the system is idle)

            ONEVENT
                Description: Runs the task in response to a specific event.
                Example: /sc ONEVENT /ec Application /mo *[System/EventID=123] (runs on a specific event ID in the Application log)

        Additional Options and Modifiers

            Modifier (/mo): This parameter is used with some schedule types to specify the frequency or repetition interval.
                Example: /mo 10 means every 10 units (e.g., minutes, days).

            Days (/d): Used with WEEKLY and MONTHLY schedules to specify the exact days.
                Example for Weekly: /d MON,TUE,FRI (runs on Monday, Tuesday, and Friday)
                Example for Monthly: /d 1,15 (runs on the 1st and 15th)

            Start Time (/st): Specifies the time of day the task starts. Required for some schedule types.
                Format: HH:mm (24-hour format)
                Example: /st 08:00 (8:00 AM)

            End Date/Time (/et): Specifies when the task should stop running. This is less commonly used but available for fine-tuning task schedules.

            End Date (/ed): Sets an end date for the task to stop running. 

         * 
         */


        //Example Commands

        //Here are some example schtasks commands for different scenarios:

        //1.Create a Task That Runs Once:                        
        //schtasks / create / tn "OneTimeTask" / tr "C:\Path\To\Your\Application.exe" / sc once / st 09:00 / sd 07 / 30 / 2024 / f

        //2. Create a Task That Runs Weekly:
        //schtasks / create / tn "WeeklyTask" / tr "C:\Path\To\Your\Application.exe" / sc weekly / d MON / st 09:00 / f

        //3. Create a Task That Runs at System Startup:
        //schtasks / create / tn "StartupTask" / tr "C:\Path\To\Your\Application.exe" / sc onstart / f

        //4. Delete a Task:
        //schtasks / delete / tn "MyScheduledTask" / f

        public static OperationCallResult<bool> CreateWindowsSchedulerTask(AppWinSchedulerTaskDto appWinSchedulerTaskDto)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;


            try
            {
               


                if (string.IsNullOrWhiteSpace(appWinSchedulerTaskDto.TaskName))
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_CreateWindowsSchedulerTask_Error", ValidationItemType.Error, "Task Name is Missing."));
                }
                if (string.IsNullOrWhiteSpace(appWinSchedulerTaskDto.ScheduleType))
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_CreateWindowsSchedulerTask_Error", ValidationItemType.Error, "Schedule Type is Missing."));
                }
                if (string.IsNullOrWhiteSpace(appWinSchedulerTaskDto.StartTime))
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_CreateWindowsSchedulerTask_Error", ValidationItemType.Error, "Start Time is Missing."));
                }

                if (aValidationResult.HasErrors)
                {
                    return aOperationCallResult;
                }

                //appWinSchedulerTaskDto.TaskToRun = @"C:\temp\AppScheduler\AppWinSchedule.exe -workflow 11050";
                //appWinSchedulerTaskDto.TaskFolder = "\\Hu\\TestFolder1";
                //appWinSchedulerTaskDto.TaskName = "MyScheduledTask";
                //


                //appWinSchedulerTaskDto.StartTime = "09:00"; // Format: HH:mm                     
                //appWinSchedulerTaskDto.ScheduleType = "daily";
                //appWinSchedulerTaskDto.RunAsUser = "System";


                appWinSchedulerTaskDto.FullPath = $"{appWinSchedulerTaskDto.TaskFolder}\\{appWinSchedulerTaskDto.TaskName}";

                string param_repeatOption = "";                

                if (!string.IsNullOrWhiteSpace(appWinSchedulerTaskDto.RepeatEvery))
                {
                    param_repeatOption = " /mo " + appWinSchedulerTaskDto.RepeatEvery;

                    if (!string.IsNullOrWhiteSpace(appWinSchedulerTaskDto.Days))
                    {
                        param_repeatOption += " /d " + appWinSchedulerTaskDto.Days;
                    }
                }

               

                

                

                string createTaskCmd = $"/create /tn \"{appWinSchedulerTaskDto.FullPath}\" /tr \"{appWinSchedulerTaskDto.TaskToRun}\" /sc {appWinSchedulerTaskDto.ScheduleType}{param_repeatOption} /st {appWinSchedulerTaskDto.StartTime} /ru {appWinSchedulerTaskDto.RunAsUser} /rl highest /f";

                string errorMessage = "";

                if (ProcessHelper.ExecuteCommand("schtasks", createTaskCmd, ref errorMessage))
                {
                    string message = $"Windows Scheduler Task '{appWinSchedulerTaskDto.FullPath}' Created Successfully.";

                    aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_CreateWindowsSchedulerTask_ok", ValidationItemType.Message, message));
                }
                else
                {
                    aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_CreateWindowsSchedulerTask_Error", ValidationItemType.Error, errorMessage));
                }

            }
            catch (Exception ex)
            {
                aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_CreateWindowsSchedulerTask_Error", ValidationItemType.Error, ex.ToString()));

            }


            return aOperationCallResult;



        }
    }


    internal class IntegrationPauseException : ApplicationException
    {
        public IntegrationPauseException(string message)
            : base(message)
        {

        }
    }
}