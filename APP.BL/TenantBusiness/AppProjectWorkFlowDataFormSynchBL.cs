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
using PlmExpressionEval;
using System.Data.SqlClient;
using System.Data.Common;

using APP.Framework;
namespace App.BL
{


    public static class AppProjectWorkFlowDataFormSynchBL
    {
        private static readonly int MaxWhileLoopCount = 1000;
        internal static bool DoSynFormAndTaskWrokFlow(AppMasterDetailDto orgAppMasterDetailDto, AppMasterDetailDto updatedMasterDetailFromDatabase,
            AppTransactionExDto aAppTransactionExDto, AppTransactionStructureDto aAppMasterDetailStructureDto, OperationCallResult<AppMasterDetailDto> aOperationCallResult)
        {

            bool neeToRefreshFormData = false;



            string rootKeyDbname = aAppTransactionExDto.AppTransactionUnitList.First().PrimaryKeyDbfieldList.First();
            object rootKeyValue = updatedMasterDetailFromDatabase.DictOneToOneFields[rootKeyDbname];

            AppProjectOrWorkFlowExDto workflowExDto = AppProjectWorkFlowProcessBL.GetTransactionRunningProjectWorkflow(updatedMasterDetailFromDatabase.TransactionId, rootKeyValue, false);

            if (workflowExDto != null && workflowExDto.RuntimeOriginalProjectId.HasValue)
            {
                List<AppTransactionExDto> workflowTransactionList = AppProjectWorkFlowStructureBL.RetrieveOneMasterWorkflowTransactionList(workflowExDto.RuntimeOriginalProjectId.Value);
                workflowExDto.DictTransIdAndTransExDto = workflowTransactionList.ToDictionary(o => (int)o.Id, o => o);


                AppTransactionChangedObjectDto transChangeDto = GetTransactionChangedObjects(orgAppMasterDetailDto, updatedMasterDetailFromDatabase, aAppTransactionExDto);

                //List<AppProjectWorkFlowConditionExDto> listWorkflowCondition = new List<AppProjectWorkFlowConditionExDto>();


                List<AppProjectWorkFlowConditionExDto> triggeredConditionList = SetupTransactionContionAction(transChangeDto, workflowExDto);

                Dictionary<int, object> dictUpdateAllTransactionIdValue = new Dictionary<int, object>();

                ProcessAllProjectTriger(aAppTransactionExDto, transChangeDto.AllFreshRootValue, triggeredConditionList, dictUpdateAllTransactionIdValue, workflowExDto, rootKeyValue, aOperationCallResult);

                if (dictUpdateAllTransactionIdValue.Count > 0)
                {
                    AppMasterDetailDto aAppMasterDetailDto = updatedMasterDetailFromDatabase;
                    aAppMasterDetailDto.IsDirty = true;

                    AppTransactionFormulaBL.UpdateRootAndSiblingDbFieldNameValueFromFiedIdValue(aAppMasterDetailDto, aAppTransactionExDto, dictUpdateAllTransactionIdValue);

                    // need to break up cscading call  AppMasterDetailFormDataSaveBL.SaveTransactionData(aAppMasterDetailDto, false);
                    AppMasterDetailFormDataSaveBL.SaveTransactionData(aAppMasterDetailDto, true);


                    neeToRefreshFormData = true;


                }

            }

            return neeToRefreshFormData;
        }

        private static void ProcessAllProjectTriger(AppTransactionExDto aAppTransactionExDto, Dictionary<int, object> allFreshRootValue,
            List<AppProjectWorkFlowConditionExDto> triggeredConditionList, Dictionary<int, object> dictUpdateAllTransactionIdValue, AppProjectOrWorkFlowExDto workflowExDto,
            object rootKeyValue, OperationCallResult<AppMasterDetailDto> aOperationCallResult)
        {
            //ProcessOneProject(aAppTransactionExDto, allFreshRootValue, listProjectCondition, workflowExDto, listProjectChangeTrigger, projectId, rootKeyValue);
            Dictionary<int, AppProjectWorkFlowTaskExDto> dictWorkFlowTask = workflowExDto.RootTreeList.ToDictionary(o => (int)o.Id, o => o);
            dictWorkFlowTask.ForAll(o =>
            {
                o.Value.ForeignAppProjectOrWorkFlowExDto = workflowExDto;
                o.Value.NewSystemConversationMessageList = new List<string>();
            });

            Dictionary<int, object> dictUpdateTransactionIdValue = new Dictionary<int, object>();

            List<AppMessageDto> messageList = new List<AppMessageDto>();

            foreach (var triggeredCondition in triggeredConditionList)
            {
                PorcessOneCondition(workflowExDto, aAppTransactionExDto, allFreshRootValue, dictWorkFlowTask, dictUpdateTransactionIdValue, triggeredCondition, messageList, rootKeyValue, aOperationCallResult);
            }

            if (workflowExDto.IsRelatedEntitiesModified())
            {
                OperationCallResult<AppProjectOrWorkFlowExDto> aResult = AppProjectWorkFlowStructureBL.SaveProjectOrWorkFlowExDto(workflowExDto);

                if (aResult.IsSuccessful)
                {
                    if (messageList.Count > 0)
                    {
                        SendWorkflowAllTaskStatusChangeMessage(messageList);
                    }
                }
            }

            foreach (int transactionFiedId in dictUpdateTransactionIdValue.Keys)
            {
                if (dictUpdateAllTransactionIdValue.ContainsKey(transactionFiedId))
                {
                    dictUpdateAllTransactionIdValue[transactionFiedId] = dictUpdateTransactionIdValue[transactionFiedId];
                }
                else
                {
                    dictUpdateAllTransactionIdValue.Add(transactionFiedId, dictUpdateTransactionIdValue[transactionFiedId]);
                }
            }
        }


        private static AppTransactionChangedObjectDto GetTransactionChangedObjects(AppMasterDetailDto orgAppMasterDetailDto, AppMasterDetailDto updatedRootClientAppformDataDto,
            AppTransactionExDto aAppTransactionExDto)
        {
            Dictionary<int, object> allFreshRootValue = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(updatedRootClientAppformDataDto, aAppTransactionExDto);
            List<int> rootLevelChangeFiedIds = new List<int>();

            AppTransactionChangedObjectDto transChangeDto = new AppTransactionChangedObjectDto();
            transChangeDto.AllFreshRootValue = allFreshRootValue;
            transChangeDto.RootLevelChangeFiedIds = rootLevelChangeFiedIds;
            transChangeDto.DictChildUnitIdAndNewRows = new Dictionary<string, List<AppChildDataDto>>();
            transChangeDto.DictChildUnitIdAndChangedRows = new Dictionary<string, List<AppChildDataDto>>();
            transChangeDto.DictChildUnitIdAndDeletedRows = new Dictionary<string, List<AppChildDataDto>>();

            // 
            if (orgAppMasterDetailDto != null) // need to get call Change Fields
            {
                Dictionary<int, object> allOriRootValue = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(orgAppMasterDetailDto, aAppTransactionExDto);
                foreach (int key in allOriRootValue.Keys)
                {
                    object orgValue = allOriRootValue[key];
                    object freshValue = allFreshRootValue[key];
                    // string only comparat content 
                    // 
                    if (CompareObjectValue(orgValue, freshValue))
                    {
                        rootLevelChangeFiedIds.Add(key);
                    }

                }

                foreach (string unitId in orgAppMasterDetailDto.DictOneToManyFields.Keys)
                {
                    transChangeDto.DictChildUnitIdAndNewRows.Add(unitId, new List<AppChildDataDto>());
                    transChangeDto.DictChildUnitIdAndDeletedRows.Add(unitId, new List<AppChildDataDto>());
                    transChangeDto.DictChildUnitIdAndChangedRows.Add(unitId, new List<AppChildDataDto>());

                    var unitDto = aAppTransactionExDto.DictAllTransactionUnitIdExDto[unitId];//.ToString());

                    if (unitDto != null && unitDto.PrimaryKeyDbfieldList != null && unitDto.PrimaryKeyDbfieldList.Count > 0)
                    {
                        List<AppChildDataDto> orgChildRowList = orgAppMasterDetailDto.DictOneToManyFields[unitId];
                        List<AppChildDataDto> updatedChildRowList = updatedRootClientAppformDataDto.DictOneToManyFields[unitId];

                        SetChildRowPKValueCombinString(unitDto, orgChildRowList);
                        SetChildRowPKValueCombinString(unitDto, updatedChildRowList);

                        foreach (var updatedRow in updatedChildRowList)
                        {
                            var orgRow = orgChildRowList.FirstOrDefault(o => o.PKValueCombinString != string.Empty && o.PKValueCombinString == updatedRow.PKValueCombinString);

                            if (orgRow != null)
                            {
                                bool isAnyRowValueChanged = false;
                                foreach (string key in orgRow.DictOneToOneFields.Keys)
                                {
                                    if (key != EmSystemDbTrackField.AppModifiedDate.ToString() && key != EmSystemDbTrackField.AppModifiedByID.ToString())
                                    {
                                        object orgValue = orgRow.DictOneToOneFields[key];
                                        object freshValue = updatedRow.DictOneToOneFields[key];
                                        // string only comparat content 
                                        // 
                                        if (CompareObjectValue(orgValue, freshValue))
                                        {
                                            isAnyRowValueChanged = true;
                                        }
                                    }
                                }
                                if (isAnyRowValueChanged)
                                {
                                    transChangeDto.DictChildUnitIdAndChangedRows[unitId].Add(updatedRow);
                                }
                            }
                            else
                            {
                                transChangeDto.DictChildUnitIdAndNewRows[unitId].Add(updatedRow);
                            }
                        }

                        foreach (var orgRow in orgChildRowList)
                        {
                            var updatedRow = updatedChildRowList.FirstOrDefault(o => o.PKValueCombinString != string.Empty && o.PKValueCombinString == orgRow.PKValueCombinString);

                            if (updatedRow == null)
                            {
                                transChangeDto.DictChildUnitIdAndDeletedRows[unitId].Add(orgRow);
                            }
                        }

                    }
                }

            }
            else // it is new  Form data
            {
                rootLevelChangeFiedIds.AddRange(allFreshRootValue.Keys);

                transChangeDto.DictChildUnitIdAndNewRows = updatedRootClientAppformDataDto.DictOneToManyFields;

            }

            return transChangeDto;
        }

        public static void SetChildRowPKValueCombinString(AppTransactionUnitExDto unitDto, List<AppChildDataDto> updatedChildRowList)
        {
            foreach (var anUpdatedRow in updatedChildRowList)
            {
                string pkValueCombinString = string.Empty;

                foreach (var aPkField in unitDto.PrimaryKeyDbfieldList)
                {
                    string onePkValue = string.Empty;
                    if (anUpdatedRow.DictOneToOneFields[aPkField] != null)
                    {
                        onePkValue = anUpdatedRow.DictOneToOneFields[aPkField].ToString();
                    }

                    pkValueCombinString += onePkValue + "_";
                }

                anUpdatedRow.PKValueCombinString = pkValueCombinString;
            }
        }

        private static List<AppProjectWorkFlowConditionExDto> SetupTransactionContionAction(AppTransactionChangedObjectDto transChangeDto, AppProjectOrWorkFlowExDto appProjectOrWorkFlowExDto)
        {
            List<AppProjectWorkFlowConditionExDto> triggeredConditionList = new List<AppProjectWorkFlowConditionExDto>();

            foreach (var orgConditon in appProjectOrWorkFlowExDto.AppProjectWorkFlowConditionList.OrderBy(o => o.TriggerFlowOrder))
            {
                if (orgConditon.ConditionTypeId.HasValue)
                {
                    if (orgConditon.ConditionTypeId.Value == (int)EmAppWorkflowConditionType.FieldModified)
                    {
                        if (orgConditon.ConditionTransactionFieldId.HasValue && !transChangeDto.RootLevelChangeFiedIds.IsEmpty())
                        {
                            if (transChangeDto.RootLevelChangeFiedIds.Contains(orgConditon.ConditionTransactionFieldId.Value))
                            {
                                triggeredConditionList.Add(orgConditon.DeepCopy());
                            }
                        }
                    }
                    else if (orgConditon.ConditionTypeId.Value == (int)EmAppWorkflowConditionType.RowAdded)
                    {
                        if (orgConditon.MonitorChildUnitId.HasValue)
                        {
                            if (transChangeDto.DictChildUnitIdAndNewRows.ContainsKey(orgConditon.MonitorChildUnitId.Value.ToString()))
                            {
                                foreach (var aChildRow in transChangeDto.DictChildUnitIdAndNewRows[orgConditon.MonitorChildUnitId.Value.ToString()])
                                {
                                    var triggeredCondition = orgConditon.DeepCopy();
                                    triggeredCondition.RuntimeTriggeredChildUnitRow = aChildRow;
                                    triggeredConditionList.Add(triggeredCondition);
                                }
                            }
                        }
                    }
                    else if (orgConditon.ConditionTypeId.Value == (int)EmAppWorkflowConditionType.RowDeleted)
                    {
                        if (orgConditon.MonitorChildUnitId.HasValue)
                        {
                            if (transChangeDto.DictChildUnitIdAndDeletedRows.ContainsKey(orgConditon.MonitorChildUnitId.Value.ToString()))
                            {
                                foreach (var aChildRow in transChangeDto.DictChildUnitIdAndDeletedRows[orgConditon.MonitorChildUnitId.Value.ToString()])
                                {
                                    var triggeredCondition = orgConditon.DeepCopy();
                                    triggeredCondition.RuntimeTriggeredChildUnitRow = aChildRow;
                                    triggeredConditionList.Add(triggeredCondition);
                                }
                            }
                        }
                    }
                    else if (orgConditon.ConditionTypeId.Value == (int)EmAppWorkflowConditionType.RowModified)
                    {
                        if (orgConditon.MonitorChildUnitId.HasValue)
                        {
                            if (transChangeDto.DictChildUnitIdAndChangedRows.ContainsKey(orgConditon.MonitorChildUnitId.Value.ToString()))
                            {
                                foreach (var aChildRow in transChangeDto.DictChildUnitIdAndChangedRows[orgConditon.MonitorChildUnitId.Value.ToString()])
                                {
                                    var triggeredCondition = orgConditon.DeepCopy();
                                    triggeredCondition.RuntimeTriggeredChildUnitRow = aChildRow;
                                    triggeredConditionList.Add(triggeredCondition);
                                }
                            }
                        }
                    }
                }
            }

            return triggeredConditionList;

        }


        //private static Dictionary<int, object> ProcessOneProject(AppTransactionExDto aAppTransactionExDto, Dictionary<int, object> allFreshRootValue, List<AppProjectWorkFlowConditionExDto> projectConditionList, AppProjectOrWorkFlowExDto workFlowExDto, List<int> formTriggerFieds, int projectId, object rootKeyValue)
        //{
        //    Dictionary<int, AppProjectWorkFlowTaskExDto> dictWorkFlowTask = workFlowExDto.RootTreeList.ToDictionary(o => (int)o.Id, o => o);
        //    dictWorkFlowTask.ForAll(o =>
        //    {
        //        o.Value.ForeignAppProjectOrWorkFlowExDto = workFlowExDto;
        //        o.Value.NewSystemConversationMessageList = new List<string>();
        //    });

        //    Dictionary<int, object> dictUpdateTransactionIdValue = new Dictionary<int, object>();

        //    List<AppMessageDto> messageList = new List<AppMessageDto>();

        //    foreach (int formTrigerFiedId in formTriggerFieds)
        //    {
        //        PorcessOneFieldTrigger(workFlowExDto, aAppTransactionExDto, allFreshRootValue, dictWorkFlowTask, projectConditionList, dictUpdateTransactionIdValue, formTrigerFiedId, messageList, rootKeyValue);

        //    }
        //    // need to udpate Transaction Form Data



        //    if (workFlowExDto.IsRelatedEntitiesModified())
        //    {
        //        OperationCallResult<AppProjectOrWorkFlowExDto> aOperationCallResult = AppProjectWorkFlowStructureBL.SaveProjectOrWorkFlowExDto(workFlowExDto);

        //        if (aOperationCallResult.IsSuccessful)
        //        {
        //            if (messageList.Count > 0)
        //            {
        //                //AppMessageBL.ConvertMessageToListUserIdsToEmails(messageList);
        //                //AppMessageBL.SendEmailFromAppMessageDtoList(messageList);

        //                SendWorkflowAllTaskStatusChangeMessage(messageList);

        //            }
        //        }
        //    }

        //    return dictUpdateTransactionIdValue;

        //}



        //private static void PorcessOneFieldTrigger(AppProjectOrWorkFlowExDto projectWorkFlow, AppTransactionExDto aAppTransactionExDto, Dictionary<int, object> allFreshRootValue, 
        //    Dictionary<int, AppProjectWorkFlowTaskExDto> dictWorkFlowTask, List<AppProjectWorkFlowConditionExDto> projectConditionList, Dictionary<int, object> dictUpdateTransactionIdValue, 
        //    int formTrigerFiedId, List<AppMessageDto> messageList, object rootKeyValue)
        //{

        //    List<AppProjectWorkFlowConditionExDto> triggerConditionList = projectConditionList.Where(o => o.ConditionTransactionFieldId == formTrigerFiedId).ToList();

        //    var dictAllTransactionField = aAppTransactionExDto.DictAllTransactionField;
        //    foreach (AppProjectWorkFlowConditionExDto conditionDto in triggerConditionList.OrderBy(o => o.TriggerFlowOrder))
        //    {
        //        //	conditionDto.ProjectWorkFlowTaskId ??/ TO DO
        //        //transactionfieldid_4097== 2

        //        PorcessOneCondition(projectWorkFlow, aAppTransactionExDto, allFreshRootValue, dictWorkFlowTask, dictUpdateTransactionIdValue, conditionDto, messageList, rootKeyValue);
        //    }
        //}

        private static void PorcessOneCondition(AppProjectOrWorkFlowExDto projectWorkFlow, AppTransactionExDto aAppTransactionExDto, Dictionary<int, object> allFreshRootValue,
            Dictionary<int, AppProjectWorkFlowTaskExDto> dictWorkFlowTask, Dictionary<int, object> dictUpdateTransactionIdValue, AppProjectWorkFlowConditionExDto conditionDto,
            List<AppMessageDto> messageList, object rootKeyValue, OperationCallResult<AppMasterDetailDto> aOperationCallResult)
        {
            var dictAllTransactionField = aAppTransactionExDto.DictAllTransactionField;

            string formula = conditionDto.FormulaExpression;


            if (string.IsNullOrWhiteSpace(formula))
            {
                if (conditionDto.MonitorChildUnitId.HasValue)
                {
                    formula = "true";
                }
                else
                {
                    return;
                }

            }

            //if (!string.IsNullOrWhiteSpace(formula))
            //{
            formula = AppTransactionFormulaBL.RightSideAssignmentEXpressWithRealValue(dictAllTransactionField, formula, allFreshRootValue);

            if (!formula.StartsWith("\"\""))
            {
                Boolean? exResult = ControlTypeValueConverter.ConvertValueToBoolean(Evaluator.EvaluateToObject(formula));
                if (exResult.HasValue && exResult.Value)
                {

                    //AppProjectWorkFlowTaskExDto currenctWorkFlow = dictWorkFlowTask[conditionDto.ProjectWorkFlowTaskId.Value];

                    foreach (var action in conditionDto.AppProjectWorkFlowActionList.OrderBy(o => o.ActionFlowOrder))
                    {
                        //Test one case ??? clear

                        if (action.ActionType.HasValue && action.ActionType.Value == (int)EmAppWorkFlowActionType.StartProject)
                        {

                            SetStartProject(projectWorkFlow, dictWorkFlowTask, action, messageList);
                            projectWorkFlow.IsModified = true;

                        }

                        else if (action.ActionType.HasValue && action.ActionType.Value == (int)EmAppWorkFlowActionType.Complete)
                        {

                            SetComplete(dictWorkFlowTask, action, messageList);
                            projectWorkFlow.IsModified = true;

                        }


                        else if (action.ActionType.HasValue && action.ActionType.Value == (int)EmAppWorkFlowActionType.Hold)
                        {

                            SetHolderTask(dictWorkFlowTask, action, messageList);
                            projectWorkFlow.IsModified = true;

                        }

                        else if (action.ActionType.HasValue && action.ActionType.Value == (int)EmAppWorkFlowActionType.Reject)
                        {

                            SetRejectTask(dictWorkFlowTask, action, messageList);
                            projectWorkFlow.IsModified = true;

                        }

                        else if (action.ActionType.HasValue && action.ActionType.Value == (int)EmAppWorkFlowActionType.Start)
                        {

                            SetStartTask(dictWorkFlowTask, action, messageList);
                            projectWorkFlow.IsModified = true;

                        }

                        else if (action.ActionType.HasValue && action.ActionType.Value == (int)EmAppWorkFlowActionType.Ignore)
                        {

                            SetIgnoreTask(dictWorkFlowTask, action, messageList);
                            projectWorkFlow.IsModified = true;

                        }

                        else if (action.ActionType.HasValue && action.ActionType.Value == (int)EmAppWorkFlowActionType.UpdateTransactionField)
                        {
                            //UpdateTransactionField(dictAllTransactionField, allFreshRootValue, dictUpdateTransactionIdValue, action);
                            //projectWorkFlow.IsModified = true;
                        }


                        else if (action.ActionType.HasValue && action.ActionType.Value == (int)EmAppWorkFlowActionType.UpdateNextTransactionForm)
                        {
                            //UpdateNextTransactionForm(aAppTransactionExDto, allFreshRootValue, dictUpdateTransactionIdValue, action, rootKeyValue);
                            //projectWorkFlow.IsModified = true;
                        }
                        else if (action.ActionType.HasValue && action.ActionType.Value == (int)EmAppWorkFlowActionType.DataLoad)
                        {
                            ExecuteTransactionDataLoad(aAppTransactionExDto, allFreshRootValue, dictUpdateTransactionIdValue, action);
                            projectWorkFlow.IsModified = true;
                        }
                        else if (action.ActionType.HasValue && action.ActionType.Value == (int)EmAppWorkFlowActionType.ExecuteNextTransactionFormDataLoad)
                        {
                            ExecuteNextTransactionDataLoad(aAppTransactionExDto, allFreshRootValue, dictUpdateTransactionIdValue, action, rootKeyValue);
                            projectWorkFlow.IsModified = true;
                        }
                        else if (action.ActionType.HasValue && action.ActionType.Value == (int)EmAppWorkFlowActionType.SetCurrentUser)
                        {
                            SetCurrentUser(dictWorkFlowTask, action);
                            projectWorkFlow.IsModified = true;
                        }
                        else if (action.ActionType.HasValue && action.ActionType.Value == (int)EmAppWorkFlowActionType.AppendCurrentUser)
                        {
                            AppendCurrenctUser(dictWorkFlowTask, action);
                            projectWorkFlow.IsModified = true;
                        }

                        else if (action.ActionType.HasValue && action.ActionType.Value == (int)EmAppWorkFlowActionType.SetUser)
                        {
                            SetUser(dictWorkFlowTask, allFreshRootValue, action);
                            projectWorkFlow.IsModified = true;
                        }

                        else if (action.ActionType.HasValue && action.ActionType.Value == (int)EmAppWorkFlowActionType.AppendUser)
                        {
                            AppendUser(dictWorkFlowTask, allFreshRootValue, action);
                            projectWorkFlow.IsModified = true;
                        }

                        else if (action.ActionType.HasValue && action.ActionType.Value == (int)EmAppWorkFlowActionType.NotifyTaskUser)
                        {
                            NotifyTaskUser(dictWorkFlowTask, allFreshRootValue, action, messageList, aAppTransactionExDto, rootKeyValue);
                            projectWorkFlow.IsModified = true;
                        }
                        else if (action.ActionType.HasValue && action.ActionType.Value == (int)EmAppWorkFlowActionType.SendFormMessageToFollowUpUsers)
                        {
                            SendFormMessageToFollowUpUsers(dictWorkFlowTask, allFreshRootValue, action, messageList, aAppTransactionExDto, rootKeyValue);
                            projectWorkFlow.IsModified = true;
                        }
                        else if (action.ActionType.HasValue && action.ActionType.Value == (int)EmAppWorkFlowActionType.SendMessageToTransFieldUserOrRole)
                        {
                            SendMessageToTransFieldUserOrRole(dictWorkFlowTask, allFreshRootValue, action, messageList, aAppTransactionExDto, rootKeyValue, conditionDto.RuntimeTriggeredChildUnitRow);
                            projectWorkFlow.IsModified = true;
                        }
                        else if (action.ActionType.HasValue && action.ActionType.Value == (int)EmAppWorkFlowActionType.PluginWebApiCall)
                        {
                            ExecutePluginWebApiCall(action, aAppTransactionExDto, rootKeyValue, aOperationCallResult);
                            projectWorkFlow.IsModified = true;
                        }
                        else if (action.ActionType.HasValue && action.ActionType.Value == (int)EmAppWorkFlowActionType.ExecuteSQLStatement)
                        {
                            ExcusteSqlStatmentActionWithRootValue(allFreshRootValue, action, null);
                            projectWorkFlow.IsModified = true;
                        }

                        //           ExcusteSqlStatment(Dictionary < int, AppProjectWorkFlowTaskExDto > dictWorkFlowTask, Dictionary < int, object > allFreshRootValue, AppProjectWorkFlowActionExDto action,
                        //List < AppMessageDto > messageList, AppTransactionExDto aAppTransactionExDto, object rootKeyValue)
                    }

                }
            }
            //}
            //else
            //{


            //}
        }


        //private static void UpdateTransactionField(Dictionary<int, AppTransactionFieldExDto> dictAllTransactionField, Dictionary<int, object> allFreshRootValue, Dictionary<int, object> dictUpdateTransactionIdValue, AppProjectWorkFlowActionExDto action)
        //{



        //    int leftSideTransActionFieldId = AppTransactionUnitFormulaExDto.GetAssignmentLeftSideFieldId(action.FormulaExpression);

        //    if (leftSideTransActionFieldId != -1)
        //    {
        //        AppTransactionUnitFormulaExDto aAppTransactionUnitFormulaExDto = new AppTransactionUnitFormulaExDto();
        //        aAppTransactionUnitFormulaExDto.FormulaExpression = action.FormulaExpression;
        //        aAppTransactionUnitFormulaExDto.OperationType = (int)EmAppFormularType.Assignment;
        //        AppTransactionFormulaBL.DoOneFormulaCalculation(dictAllTransactionField, aAppTransactionUnitFormulaExDto, allFreshRootValue, null, null);

        //        if (allFreshRootValue.ContainsKey(leftSideTransActionFieldId))
        //        {
        //            if (dictUpdateTransactionIdValue.ContainsKey(leftSideTransActionFieldId))
        //            {
        //                dictUpdateTransactionIdValue[leftSideTransActionFieldId] = allFreshRootValue[leftSideTransActionFieldId];
        //            }
        //            else
        //            {
        //                dictUpdateTransactionIdValue.Add(leftSideTransActionFieldId, allFreshRootValue[leftSideTransActionFieldId]);

        //            }


        //        }


        //    }



        //}

        public static void ExcusteSqlStatmentActionWithRootValue(Dictionary<int, object> allFreshRootValue, AppProjectWorkFlowActionExDto action, OperationCallResult<TransactionCommandActionResultDto> toReturn)
        {


            ////  
            //if (!string.IsNullOrWhiteSpace(action.NotificationMessage))
            //{

            //    string messageTempalte = action.NotificationMessage;


            //    List<SqlParameter> sqlParamterList = new List<SqlParameter>();

            //    string sqlStateMent = ReplaceSQlParamterWithActualValue(allFreshRootValue, action.NotificationMessage, sqlParamterList);

            //    if (!string.IsNullOrWhiteSpace(sqlStateMent))
            //    {
            //        using (DataAccessAdapter adpter = AppTenantAdapterBL.GetTenantAdapter())
            //        {
            //            object result = adpter.ExecuteScalarQuery(sqlStateMent, sqlParamterList);
            //            string resultErrorMsg = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(result);

            //            if (toReturn != null)
            //            {
            //                if (!string.IsNullOrWhiteSpace(resultErrorMsg))
            //                {
            //                    toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityRegDomainExDto), "App_ExcusteSqlStatmentActionWithRootValue_Error", ValidationItemType.Error, resultErrorMsg));
            //                }
            //            }
            //        }
            //    }



            //}
        }

        internal static string ReplaceSQlParamterWithActualValue(Dictionary<int, object> allFreshRootValue, string messageTempalte,
             List<DbParameter> sqlParamterList)
        {
            if (string.IsNullOrWhiteSpace(messageTempalte))
            {
                return messageTempalte;
            }

            var currentUserEntitty = AppSecurityUserBL.CurrentUserEntity;

            string currentUserId = "";
            string currentPartnerId = "";
            string currentUserName = "";

            if (AppSecurityUserBL.CurrentUserEntity != null)
            {
                currentUserId = AppSecurityUserBL.CurrentUserEntity.UserId.ToString();
                currentUserName = AppSecurityUserBL.CurrentUserEntity.UserName;
            }


            if (ServerContext.Instance.CurrnetClientIdentity != null && ServerContext.Instance.CurrnetClientIdentity.CurrentPartnerId != null)
            {
                currentPartnerId = ServerContext.Instance.CurrnetClientIdentity.CurrentPartnerId.ToString();
            }


            if (currentUserEntitty != null && currentUserEntitty.UserName != null)
            {
                messageTempalte = messageTempalte.Replace("[" + EmAppMessagePlaceHolderToken.CurrentUserId.ToString() + "]", currentUserId);
                messageTempalte = messageTempalte.Replace("[" + EmAppMessagePlaceHolderToken.CurrentPartnerId.ToString() + "]", currentPartnerId);
                messageTempalte = messageTempalte.Replace("[" + EmAppMessagePlaceHolderToken.CurrentUserName.ToString() + "]", currentUserName);

            }


            int count = 0;

            while (messageTempalte.Contains("[" + EmAppTransactionCommandSystemParameterToken.TF.ToString()))
            {
                count++;

                if (count > MaxWhileLoopCount)
                {
                    throw new Exception("Infinit Loop While Replacing SQl Transaction Field Token With Actual Value.\n"
                        + "Query With Error: " + messageTempalte);
                }

                int startIndex = messageTempalte.IndexOf("[" + EmAppTransactionCommandSystemParameterToken.TF.ToString());
                int endIndex = messageTempalte.IndexOf("]", startIndex);

                if (startIndex >= 0 && endIndex >= 0)
                {
                    int tokenLength = EmAppTransactionCommandSystemParameterToken.TF.ToString().Length + 2;

                    int transFieldStringLength = endIndex - startIndex - tokenLength;
                    if (transFieldStringLength > 0)
                    {
                        string needToReplaceString = messageTempalte.Substring(startIndex, endIndex - startIndex + 1);

                        string transFieldIdString = messageTempalte.Substring(startIndex + tokenLength, transFieldStringLength);

                        if (transFieldIdString.IndexOf("_") > 0)
                        {
                            transFieldIdString = transFieldIdString.Substring(0, transFieldIdString.IndexOf("_"));
                        }

                        int? transFiledId = ControlTypeValueConverter.ConvertValueToInt(transFieldIdString);

                        if (transFiledId.HasValue)
                        {
                            if (allFreshRootValue.ContainsKey(transFiledId.Value))
                            {
                                object fiedValue = allFreshRootValue[transFiledId.Value];
                                //string oBjectStringValeu = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(fiedValue);

                                if (sqlParamterList != null)
                                {

                                    string paraterName = "@Parameter" + count;

                                    SqlParameter parater;

                                    if (fiedValue == null)
                                    {
                                        parater = new SqlParameter(paraterName, DBNull.Value);
                                    }
                                    else
                                    {
                                        parater = new SqlParameter(paraterName, fiedValue);
                                    }


                                    sqlParamterList.Add(parater);

                                    messageTempalte = messageTempalte.Replace(needToReplaceString, paraterName);
                                }
                                else
                                {
                                    messageTempalte = messageTempalte.Replace(needToReplaceString,  ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(fiedValue));
                                }

                                //  BUG , if the onject is 
                                //if (! string.IsNullOrWhiteSpace(oBjectStringValeu))
                                //{

                                //    string paraterName = "@Parameter" + count;

                                //    SqlParameter parater = new SqlParameter(paraterName, fiedValue);

                                //    sqlParamterList.Add(parater);
                                //    messageTempalte = messageTempalte.Replace(needToReplaceString, paraterName);
                                //}
                                //else
                                //{
                                //    return string.Empty;
                                //}
                            }

                        }

                    }
                }
            }


            count = 0;

            while (messageTempalte.Contains("[" + EmAppTransactionCommandSystemParameterToken.GlobalTF.ToString()))
            {
                count++;

                if (count > MaxWhileLoopCount)
                {
                    throw new Exception("Infinit Loop While Replacing SQl Transaction Field Token With Actual Value.\n"
                        + "Query With Error: " + messageTempalte);
                }

                int startIndex = messageTempalte.IndexOf("[" + EmAppTransactionCommandSystemParameterToken.GlobalTF.ToString());
                int endIndex = messageTempalte.IndexOf("]", startIndex);

                if (startIndex >= 0 && endIndex >= 0)
                {
                    int tokenLength = EmAppTransactionCommandSystemParameterToken.GlobalTF.ToString().Length + 2;

                    int transFieldStringLength = endIndex - startIndex - tokenLength;
                    if (transFieldStringLength > 0)
                    {
                        string needToReplaceString = messageTempalte.Substring(startIndex, endIndex - startIndex + 1);

                        string transFieldIdString = messageTempalte.Substring(startIndex + tokenLength, transFieldStringLength);

                        if (transFieldIdString.IndexOf("_") > 0)
                        {
                            transFieldIdString = transFieldIdString.Substring(0, transFieldIdString.IndexOf("_"));
                        }

                        int? transFiledId = ControlTypeValueConverter.ConvertValueToInt(transFieldIdString);

                        if (transFiledId.HasValue)
                        {
                            if (allFreshRootValue.ContainsKey(transFiledId.Value))
                            {
                                object fiedValue = allFreshRootValue[transFiledId.Value];
                                //string oBjectStringValeu = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(fiedValue);

                                

                                if (sqlParamterList != null)
                                {
                                    string paraterName = "@Parameter" + count;
                                    SqlParameter parater;

                                    if (fiedValue == null)
                                    {
                                        parater = new SqlParameter(paraterName, DBNull.Value);
                                    }
                                    else
                                    {
                                        parater = new SqlParameter(paraterName, fiedValue);
                                    }


                                    sqlParamterList.Add(parater);

                                    messageTempalte = messageTempalte.Replace(needToReplaceString, paraterName);

                                }
                                else
                                {
                                    messageTempalte = messageTempalte.Replace(needToReplaceString, ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(fiedValue));
                                }
                            }

                        }

                    }
                }
            }
            return messageTempalte;
        }


        private static void ExecuteTransactionDataLoad(AppTransactionExDto aAppTransactionExDto, Dictionary<int, object> allFreshRootValue, Dictionary<int, object> dictUpdateTransactionIdValue, AppProjectWorkFlowActionExDto action)
        {

            if (action.DataLoadId.HasValue)
            {
                var dataLaodEntity = AppTransactionTemplateDataLoadSetupBL.RetrieveOneAppTransactionDataLoadEntity(action.DataLoadId.Value);
                foreach (var udpateFiled in dataLaodEntity.AppTranscationDataLoadFieldMapping)
                {
                    if (!udpateFiled.IsConditionMapping.HasValue || udpateFiled.IsConditionMapping.Value == false)
                    {
                        if (udpateFiled.TransactionFieldId.HasValue)
                        {
                            allFreshRootValue[udpateFiled.TransactionFieldId.Value] = null;
                        }

                    }

                }
                AppTransactionTemplateDataLoadBL.PorcessOneDataLoad(dataLaodEntity, null, aAppTransactionExDto, allFreshRootValue);

                foreach (var udpateFiled in dataLaodEntity.AppTranscationDataLoadFieldMapping)
                {
                    if (!udpateFiled.IsConditionMapping.HasValue || udpateFiled.IsConditionMapping.Value == false)
                    {
                        if (dictUpdateTransactionIdValue.ContainsKey(udpateFiled.TransactionFieldId.Value))
                        {
                            dictUpdateTransactionIdValue[udpateFiled.TransactionFieldId.Value] = allFreshRootValue[udpateFiled.TransactionFieldId.Value];
                        }
                        else
                        {
                            dictUpdateTransactionIdValue.Add(udpateFiled.TransactionFieldId.Value, allFreshRootValue[udpateFiled.TransactionFieldId.Value]);
                        }
                    }
                }
            }


        }

        private static void ExecuteNextTransactionDataLoad(AppTransactionExDto aAppTransactionExDto, Dictionary<int, object> allFreshRootValue, Dictionary<int, object> dictUpdateTransactionIdValue, AppProjectWorkFlowActionExDto action, object rootKeyValue)
        {
            if (action.NextTransactionId.HasValue && (int)aAppTransactionExDto.Id != action.NextTransactionId.Value)
            {
                AppTransactionExDto nextTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(action.NextTransactionId.Value);
                AppMasterDetailDto nextTransactionFormData = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(action.NextTransactionId.Value, rootKeyValue);
                Dictionary<int, object> dictNextTransactionFieldIdValue = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(nextTransactionFormData, nextTransactionExDto);
                Dictionary<int, object> dictnextTransactionUpdateTransactionIdValue = new Dictionary<int, object>();

                ExecuteTransactionDataLoad(nextTransactionExDto, dictNextTransactionFieldIdValue, dictnextTransactionUpdateTransactionIdValue, action);

                AppTransactionFormulaBL.UpdateRootAndSiblingDbFieldNameValueFromFiedIdValue(nextTransactionFormData, nextTransactionExDto, dictnextTransactionUpdateTransactionIdValue);
                nextTransactionFormData.IsDirty = true;
                OperationCallResult<AppMasterDetailDto> saveResult = AppMasterDetailFormDataSaveBL.SaveTransactionData(nextTransactionFormData, false);
            }
        }




        private static void UpdateNextTransactionForm(AppTransactionExDto aAppTransactionExDto, Dictionary<int, object> allFreshRootValue, Dictionary<int, object> dictUpdateTransactionIdValue, AppProjectWorkFlowActionExDto action, object rootKeyValue)
        {
            //if (action.NextTransactionId.HasValue && (int)aAppTransactionExDto.Id != action.NextTransactionId.Value)
            //{

            //    AppTransactionExDto nextTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(action.NextTransactionId.Value);
            //    AppMasterDetailDto nextTransactionFormData = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(action.NextTransactionId.Value, rootKeyValue);
            //    Dictionary<int, object> dictNextTransactionFieldIdValue = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(nextTransactionFormData, nextTransactionExDto);

            //    if (nextTransactionFormData != null)
            //    {
            //        if (!string.IsNullOrWhiteSpace(action.FormulaExpression))
            //        {

            //            int nextFromLeftSideTransActionFieldId = AppTransactionUnitFormulaExDto.GetAssignmentLeftSideFieldId(action.FormulaExpression);

            //            if (nextFromLeftSideTransActionFieldId != -1)
            //            {

            //                Dictionary<int, AppTransactionFieldExDto> dictAllTransactionField = new Dictionary<int, AppTransactionFieldExDto>();

            //                AppTransactionUnitFormulaExDto formulaExDto = new AppTransactionUnitFormulaExDto() { FormulaExpression = action.FormulaExpression, OperationType = (int)EmAppFormularType.Assignment };
            //                Dictionary<int, object> mergedDictTransFieldIdValue = new Dictionary<int, object>();

            //                List<AppTransactionFieldExDto> allTransFiedList = new List<AppTransactionFieldExDto>();
            //                aAppTransactionExDto.DictAllTransactionUnitIdExDto.Values.ForAll(o => allTransFiedList.AddRange(o.AppTransactionFieldList.ToList()));
            //                nextTransactionExDto.DictAllTransactionUnitIdExDto.Values.ForAll(o => allTransFiedList.AddRange(o.AppTransactionFieldList.ToList()));

            //                allTransFiedList.ForAll(o =>
            //                {
            //                    if (!dictAllTransactionField.ContainsKey((int)o.Id))
            //                        dictAllTransactionField.Add((int)o.Id, o);
            //                });

            //                allFreshRootValue.Keys.ForAll(o =>
            //                {
            //                    if (!mergedDictTransFieldIdValue.ContainsKey(o))
            //                        mergedDictTransFieldIdValue.Add(o, allFreshRootValue[o]);
            //                });

            //                dictNextTransactionFieldIdValue.Keys.ForAll(o =>
            //                {
            //                    if (!mergedDictTransFieldIdValue.ContainsKey(o))
            //                        mergedDictTransFieldIdValue.Add(o, dictNextTransactionFieldIdValue[o]);
            //                });

            //                AppTransactionFormulaBL.DoOneFormulaCalculation(dictAllTransactionField, formulaExDto, mergedDictTransFieldIdValue, null, null);

            //                AppTransactionFormulaBL.UpdateRootAndSiblingDbFieldNameValueFromFiedIdValue(nextTransactionFormData, nextTransactionExDto, mergedDictTransFieldIdValue);
            //                nextTransactionFormData.IsDirty = true;
            //                OperationCallResult<AppMasterDetailDto> saveResult = AppMasterDetailFormDataSaveBL.SaveTransactionData(nextTransactionFormData, false);
            //            }
            //        }
            //    }
            //}
        }


        private static void SetStartTask(Dictionary<int, AppProjectWorkFlowTaskExDto> dictWorkFlowTask, AppProjectWorkFlowActionExDto action, List<AppMessageDto> messageList)
        {
            int? targetTaskId = action.NextWorkFlowId;


            if (targetTaskId.HasValue)
            {
                AppProjectWorkFlowTaskExDto targetTask = dictWorkFlowTask[targetTaskId.Value];
                StartOneTask(targetTask, messageList);

            }

        }

        private static void SetRejectTask(Dictionary<int, AppProjectWorkFlowTaskExDto> dictWorkFlowTask, AppProjectWorkFlowActionExDto action, List<AppMessageDto> messageList)
        {
            int? actionSucessId = action.NextWorkFlowId;


            if (actionSucessId.HasValue)
            {
                AppProjectWorkFlowTaskExDto targetTask = dictWorkFlowTask[actionSucessId.Value];
                RejectOneTask(targetTask, messageList);

            }

        }

        private static void SetHolderTask(Dictionary<int, AppProjectWorkFlowTaskExDto> dictWorkFlowTask, AppProjectWorkFlowActionExDto action, List<AppMessageDto> messageList)
        {
            int? actionSucessId = action.NextWorkFlowId;

            //if(currenctWorkFlow.StageStatusFlag == (int)EmAppWorkflowTaskStageStatus.Completed)
            //{
            if (actionSucessId.HasValue)
            {
                AppProjectWorkFlowTaskExDto targetTask = dictWorkFlowTask[actionSucessId.Value];
                HoldOneTask(targetTask, messageList);
            }
        }

        //private static void SetStartSucessor(Dictionary<int, AppProjectWorkFlowTaskExDto> dictWorkFlowTask, AppProjectWorkFlowActionExDto action, AppProjectWorkFlowTaskExDto currenctWorkFlow)
        //{
        //    int? actionSucessId = action.NextWorkFlowId;

        //    //if(currenctWorkFlow.StageStatusFlag == (int)EmAppWorkflowTaskStageStatus.Completed)
        //    //{
        //    if (actionSucessId.HasValue)
        //    {
        //        AppProjectWorkFlowTaskExDto targetTask = dictWorkFlowTask[actionSucessId.Value];               
        //        StartOneTask(targetTask);
        //    }

        //    //}
        //}


        private static void SetStartProject(AppProjectOrWorkFlowExDto workflowExDto, Dictionary<int, AppProjectWorkFlowTaskExDto> dictWorkFlowTask, AppProjectWorkFlowActionExDto action, List<AppMessageDto> messageList)
        {
            if (workflowExDto != null)
            {
                //OperationCallResult<AppProjectOrWorkFlowExDto> startProjectResult = AppProjectWorkFlowStructureBL.StartOneProject(workflowExDto);

                //if (startProjectResult != null && startProjectResult.IsSuccessful) {
                //    foreach (var aStartStepTask in dictWorkFlowTask.Values.Where(o => o.DiagramShapeType.HasValue && o.DiagramShapeType.Value == (int)EmAppWorkflowDiagramShapeType.StartStep))
                //    {
                //        if (!aStartStepTask.DateActualEnd.HasValue)
                //        {
                //            CompleOneTask(aStartStepTask, messageList);
                //        }
                //    }
                //}

            }
        }

        private static void SetComplete(Dictionary<int, AppProjectWorkFlowTaskExDto> dictWorkFlowTask, AppProjectWorkFlowActionExDto action, List<AppMessageDto> messageList)
        {
            int? actionSucessId = action.NextWorkFlowId;

            if (actionSucessId.HasValue)
            {
                AppProjectWorkFlowTaskExDto targetTast = dictWorkFlowTask[actionSucessId.Value];
                CompleOneTask(targetTast, messageList);
            }
        }

        private static void SetIgnoreTask(Dictionary<int, AppProjectWorkFlowTaskExDto> dictWorkFlowTask, AppProjectWorkFlowActionExDto action, List<AppMessageDto> messageList)
        {
            int? actionSucessId = action.NextWorkFlowId;

            if (actionSucessId.HasValue)
            {
                AppProjectWorkFlowTaskExDto targetTask = dictWorkFlowTask[actionSucessId.Value];
                IgnoreOneTask(targetTask, messageList);
            }
        }



        private static void StartOneTask(AppProjectWorkFlowTaskExDto targetTask, List<AppMessageDto> messageList)
        {
            targetTask.IsModified = true;
            targetTask.StageStatusFlag = (int)EmAppWorkflowTaskStageStatus.Started;

            targetTask.DatePlannedStart = DateTime.UtcNow;
            targetTask.DatePlannedEnd = targetTask.DatePlannedStart.Value.AddHours(1);

            targetTask.DateActualStart = DateTime.UtcNow;
            targetTask.DateActualEnd = null;

            targetTask.ProgressId = (int)EmAppProjectTaskProgress.Started;
            targetTask.CompletedPercent = 25;



            //GenerateWorkflowTaskStatusChangeMessage(targetTask, messageList, EmAppWorkflowTaskStageStatus.Started.ToString());
        }

        private static void CompleOneTask(AppProjectWorkFlowTaskExDto targetTask, List<AppMessageDto> messageList)
        {
            targetTask.IsModified = true;
            targetTask.StageStatusFlag = (int)EmAppWorkflowTaskStageStatus.Completed;
            targetTask.DateActualEnd = DateTime.UtcNow;
            targetTask.ProgressId = (int)EmAppProjectTaskProgress.Done;
            targetTask.CompletedPercent = 100;
            if (!targetTask.DateActualStart.HasValue)
            {
                targetTask.DateActualStart = targetTask.DateActualEnd;
            }

            //GenerateWorkflowTaskStatusChangeMessage(targetTask, messageList, EmAppWorkflowTaskStageStatus.Completed.ToString());

            // need to do recuesive??
            if (targetTask.Sucessors != null)
            {
                foreach (AppProjectWorkFlowTaskExDto successor in targetTask.Sucessors)
                {
                    if (successor.IsAutoStart.HasValue && successor.IsAutoStart.Value)
                    {
                        StartOneTask(successor, messageList);
                    }
                }
            }
        }

        private static void RejectOneTask(AppProjectWorkFlowTaskExDto targetTask, List<AppMessageDto> messageList)
        {
            targetTask.IsModified = true;
            targetTask.StageStatusFlag = (int)EmAppWorkflowTaskStageStatus.Rejected;
            targetTask.DateActualEnd = DateTime.UtcNow;

            targetTask.ProgressId = (int)EmAppProjectTaskProgress.Done;
            targetTask.CompletedPercent = 100;

            if (!targetTask.DateActualStart.HasValue)
            {
                targetTask.DateActualStart = targetTask.DateActualEnd;
            }
            //GenerateWorkflowTaskStatusChangeMessage(targetTask, messageList, EmAppWorkflowTaskStageStatus.Rejected.ToString());
        }

        private static void HoldOneTask(AppProjectWorkFlowTaskExDto targetTask, List<AppMessageDto> messageList)
        {
            targetTask.IsModified = true;
            targetTask.StageStatusFlag = (int)EmAppWorkflowTaskStageStatus.Holding;
            targetTask.DateActualEnd = DateTime.UtcNow;

            targetTask.ProgressId = (int)EmAppProjectTaskProgress.Started;
            targetTask.CompletedPercent = 25;

            if (!targetTask.DateActualStart.HasValue)
            {
                targetTask.DateActualStart = targetTask.DateActualEnd;
            }
            //GenerateWorkflowTaskStatusChangeMessage(targetTask, messageList, EmAppWorkflowTaskStageStatus.Holding.ToString());
        }

        private static void IgnoreOneTask(AppProjectWorkFlowTaskExDto targetTask, List<AppMessageDto> messageList)
        {
            targetTask.IsModified = true;
            targetTask.StageStatusFlag = (int)EmAppWorkflowTaskStageStatus.Ignored;

            targetTask.ProgressId = (int)EmAppProjectTaskProgress.NotStarted;
            targetTask.CompletedPercent = 0;

            //GenerateWorkflowTaskStatusChangeMessage(targetTask, messageList, EmAppWorkflowTaskStageStatus.Ignored.ToString());

            // need to do recuesive??
            if (targetTask.Sucessors != null)
            {
                foreach (AppProjectWorkFlowTaskExDto successor in targetTask.Sucessors)
                {
                    if (successor.IsAutoStart.HasValue && successor.IsAutoStart.Value)
                    {
                        StartOneTask(successor, messageList);
                    }
                }
            }
        }

        private static bool CompareObjectValue(object orgValue, object freshValue)
        {
            if (orgValue != null && freshValue != null)
            {
                if (orgValue.ToString() != freshValue.ToString())
                {
                    return true;
                }

            }
            else
            {
                if ((orgValue != null && freshValue == null) || (orgValue == null && freshValue != null))
                {
                    return true;
                }


            }

            return false;
        }


        private static void SetCurrentUser(Dictionary<int, AppProjectWorkFlowTaskExDto> dictWorkFlowTask, AppProjectWorkFlowActionExDto action)
        {
            int? targetWorkFlowId = action.NextWorkFlowId;

            if (targetWorkFlowId.HasValue)
            {
                AppProjectWorkFlowTaskExDto targetWorkFlow = dictWorkFlowTask[targetWorkFlowId.Value];

                targetWorkFlow.IsModified = true;

                //targetWorkFlow.AppProjectTaskResourceList.Clear();

                //targetWorkFlow.AppProjectTaskResourceList.Add(new AppProjectTaskResourceExDto() { UserId = AppSecurityUserBL.CurrentUserId });

                targetWorkFlow.TaskOwnerId = AppSecurityUserBL.CurrentUserId;


            }
        }

        private static void AppendCurrenctUser(Dictionary<int, AppProjectWorkFlowTaskExDto> dictWorkFlowTask, AppProjectWorkFlowActionExDto action)
        {
            int? targetWorkFlowId = action.NextWorkFlowId;

            if (targetWorkFlowId.HasValue)
            {
                AppProjectWorkFlowTaskExDto targetWorkFlow = dictWorkFlowTask[targetWorkFlowId.Value];

                targetWorkFlow.IsModified = true;

                bool isCurrentUserNotAssigned = targetWorkFlow.AppProjectTaskResourceList.FirstOrDefault(o => o.UserId.HasValue && o.UserId.Value == AppSecurityUserBL.CurrentUserId) == null;
                if (isCurrentUserNotAssigned)
                {
                    targetWorkFlow.AppProjectTaskResourceList.Add(new AppProjectTaskResourceExDto() { UserId = AppSecurityUserBL.CurrentUserId });
                }


            }
        }


        private static void SetUser(Dictionary<int, AppProjectWorkFlowTaskExDto> dictWorkFlowTask, Dictionary<int, object> allFreshRootValue, AppProjectWorkFlowActionExDto action)
        {
            int? targetWorkFlowId = action.NextWorkFlowId;

            if (targetWorkFlowId.HasValue)
            {
                AppProjectWorkFlowTaskExDto targetWorkFlow = dictWorkFlowTask[targetWorkFlowId.Value];

                int? transFieldId = action.NotificationDestinationUserIdtransactionFiledId;

                if (!transFieldId.HasValue)
                {
                    transFieldId = AppTransactionUnitFormulaExDto.ConvertTransactionFieldExpressionToId(action.FormulaExpression);
                }

                if (transFieldId.HasValue && transFieldId.Value != -1)
                {

                    if (allFreshRootValue.ContainsKey(transFieldId.Value))
                    {

                        int? userId = ControlTypeValueConverter.ConvertValueToInt(allFreshRootValue[transFieldId.Value]);
                        targetWorkFlow.IsModified = true;

                        //targetWorkFlow.AppProjectTaskResourceList.Clear();

                        //targetWorkFlow.AppProjectTaskResourceList.Add(new AppProjectTaskResourceExDto() { UserId = userId });


                        targetWorkFlow.TaskOwnerId = userId;

                    }
                }
            }
        }

        private static void AppendUser(Dictionary<int, AppProjectWorkFlowTaskExDto> dictWorkFlowTask, Dictionary<int, object> allFreshRootValue, AppProjectWorkFlowActionExDto action)
        {
            int? targetWorkFlowId = action.NextWorkFlowId;

            if (targetWorkFlowId.HasValue)
            {
                AppProjectWorkFlowTaskExDto targetWorkFlow = dictWorkFlowTask[targetWorkFlowId.Value];

                int? transFieldId = action.NotificationDestinationUserIdtransactionFiledId;

                if (!transFieldId.HasValue)
                {
                    transFieldId = AppTransactionUnitFormulaExDto.ConvertTransactionFieldExpressionToId(action.FormulaExpression);
                }

                if (transFieldId.HasValue && transFieldId != -1)
                {

                    if (allFreshRootValue.ContainsKey(transFieldId.Value))
                    {

                        int? userId = ControlTypeValueConverter.ConvertValueToInt(allFreshRootValue[transFieldId.Value]);

                        if (userId.HasValue)
                        {

                            targetWorkFlow.IsModified = true;
                            bool isUserNotAssigned = targetWorkFlow.AppProjectTaskResourceList.FirstOrDefault(o => o.UserId.HasValue && o.UserId.Value == userId.Value) == null;
                            if (isUserNotAssigned)
                            {
                                targetWorkFlow.AppProjectTaskResourceList.Add(new AppProjectTaskResourceExDto() { UserId = userId.Value });
                            }
                        }
                    }
                }

            }
        }


        private static void NotifyTaskUser(Dictionary<int, AppProjectWorkFlowTaskExDto> dictWorkFlowTask, Dictionary<int, object> allFreshRootValue, AppProjectWorkFlowActionExDto action,
            List<AppMessageDto> messageList, AppTransactionExDto aAppTransactionExDto, object rootKeyValue)
        {
            int? targetTaskId = action.NextWorkFlowId;

            if (targetTaskId.HasValue && !string.IsNullOrWhiteSpace(action.NotificationSubject))
            {
                if (string.IsNullOrWhiteSpace(action.NotificationMessage))
                {
                    action.NotificationMessage = action.NotificationSubject;
                }

                AppProjectWorkFlowTaskExDto targetTask = dictWorkFlowTask[targetTaskId.Value];

                if (targetTask.TaskOwnerId.HasValue)
                {

                    string messageTempalte = action.NotificationMessage;

                    string currentDatetime = DateTime.UtcNow.ToShortDateString() + " " + DateTime.UtcNow.ToShortTimeString();
                    string currentUserName = AppSecurityUserBL.CurrentUserEntity.UserName;
                    string workflowName = targetTask.ProjectName;
                    string taskName = targetTask.Name;
                    string taskStatus = string.Empty;





                    if (targetTask.StageStatusFlag.HasValue)
                    {
                        taskStatus = ((EmAppWorkflowTaskStageStatus)targetTask.StageStatusFlag.Value).ToString();
                    }

                    //"[CurrentUserName] just sent you an expense claim request at [CurrentDatetime]."

                    //"[CurrentUserName] just sent you an expense claim request at [CurrentDatetime]. RequestID# [TransactionFieldValue_8678]. Description: [TransactionFieldValue_8680]. Please approve. "

                    if (!(action.ActionAttribute != null && action.ActionAttribute.IsUseRichTextMessageTemplate))
                    {
                        action.MessageTemplateId = null;
                    }


                    string subject = ReplaceMessageTemplatePlaceHolderWithActualValue(rootKeyValue, allFreshRootValue, action.NotificationSubject, currentDatetime, currentUserName, workflowName, taskName, taskStatus, aAppTransactionExDto, null, action.MessageTemplateId, true);
                    string messageBody = ReplaceMessageTemplatePlaceHolderWithActualValue(rootKeyValue, allFreshRootValue, action.NotificationMessage, currentDatetime, currentUserName, workflowName, taskName, taskStatus, aAppTransactionExDto, null, action.MessageTemplateId, false);



                    var messageDto = new AppMessageDto();
                    messageDto.ToUserIdList = new List<int>();
                    //messageDto.ToUserIdList.Add(AppSecurityUserBL.CurrentUserEntity.UserId);
                    messageDto.ProjectId = targetTask.ProjectId;
                    messageDto.ProjectActivityId = (int)targetTask.Id;
                    messageDto.TransactionId = ControlTypeValueConverter.ConvertValueToInt(aAppTransactionExDto.Id);
                    messageDto.TransactionRootValueId = rootKeyValue != null ? rootKeyValue.ToString() : string.Empty;

                    messageDto.Subject = subject;
                    messageDto.Message = messageBody;

                    if (!messageDto.ToUserIdList.Contains(targetTask.TaskOwnerId.Value))
                    {
                        messageDto.ToUserIdList.Add(targetTask.TaskOwnerId.Value);
                    }

                    //foreach (var resource in targetTask.AppProjectTaskResourceList)
                    //{
                    //    if (resource.UserId.HasValue && !messageDto.ToUserIdList.Contains(resource.UserId.Value))
                    //    {
                    //        messageDto.ToUserIdList.Add(resource.UserId.Value);
                    //    }
                    //}

                    messageList.Add(messageDto);
                }
            }
        }
        private static void SendFormMessageToFollowUpUsers(Dictionary<int, AppProjectWorkFlowTaskExDto> dictWorkFlowTask, Dictionary<int, object> allFreshRootValue, AppProjectWorkFlowActionExDto action,
            List<AppMessageDto> messageList, AppTransactionExDto aAppTransactionExDto, object rootKeyValue)
        {


            if (!string.IsNullOrWhiteSpace(action.NotificationSubject) && !string.IsNullOrWhiteSpace(action.NotificationMessage))
            {

                string messageTempalte = action.NotificationMessage;

                string currentDatetime = DateTime.UtcNow.ToShortDateString() + " " + DateTime.UtcNow.ToShortTimeString();
                string currentUserName = AppSecurityUserBL.CurrentUserEntity.UserName;
                string workflowName = string.Empty;
                string taskName = string.Empty;
                string taskStatus = string.Empty;

                //"[CurrentUserName] just sent you an expense claim request at [CurrentDatetime]."

                //"[CurrentUserName] just sent you an expense claim request at [CurrentDatetime]. RequestID# [TransactionFieldValue_8678]. Description: [TransactionFieldValue_8680]. Please approve. "

                if (!(action.ActionAttribute != null && action.ActionAttribute.IsUseRichTextMessageTemplate))
                {
                    action.MessageTemplateId = null;
                }

                string subject = ReplaceMessageTemplatePlaceHolderWithActualValue(rootKeyValue, allFreshRootValue, action.NotificationSubject, currentDatetime, currentUserName, workflowName, taskName, taskStatus, aAppTransactionExDto, null, action.MessageTemplateId, true);
                string messageBody = ReplaceMessageTemplatePlaceHolderWithActualValue(rootKeyValue, allFreshRootValue, action.NotificationMessage, currentDatetime, currentUserName, workflowName, taskName, taskStatus, aAppTransactionExDto, null, action.MessageTemplateId, false);

                var messageDto = new AppMessageDto();
                messageDto.ToUserIdList = new List<int>();
                //messageDto.ToUserIdList.Add(AppSecurityUserBL.CurrentUserEntity.UserId);
                messageDto.MessgaeScopeType = (int)EmAppMessgaeScopeType.Transaction;
                messageDto.TransactionId = ControlTypeValueConverter.ConvertValueToInt(aAppTransactionExDto.Id);
                messageDto.TransactionRootValueId = rootKeyValue != null ? rootKeyValue.ToString() : string.Empty;

                messageDto.Subject = subject;
                messageDto.Message = messageBody;

                messageList.Add(messageDto);

            }
        }

        private static void SendMessageToTransFieldUserOrRole(Dictionary<int, AppProjectWorkFlowTaskExDto> dictWorkFlowTask, Dictionary<int, object> allFreshRootValue, AppProjectWorkFlowActionExDto action,
            List<AppMessageDto> messageList, AppTransactionExDto aAppTransactionExDto, object rootKeyValue, AppChildDataDto childRowData)
        {

            if (!string.IsNullOrWhiteSpace(action.NotificationSubject) && !string.IsNullOrWhiteSpace(action.NotificationMessage)
                && (action.NotificationDestinationUserIdtransactionFiledId.HasValue || action.NotificationDestinationRoleIdtransactionFiledId.HasValue))
            {
                List<int> userIdList = new List<int>();

                if (action.NotificationDestinationUserIdtransactionFiledId.HasValue)
                {
                    int? toUserId = null;
                    int userIdFieldId = action.NotificationDestinationUserIdtransactionFiledId.Value;

                    if (allFreshRootValue.ContainsKey(userIdFieldId))
                    {
                        toUserId = ControlTypeValueConverter.ConvertValueToInt(allFreshRootValue[userIdFieldId]);
                    }
                    else
                    {
                        if (childRowData != null && aAppTransactionExDto != null && childRowData.ChildUnitId != null)
                        {
                            var unitDto = aAppTransactionExDto.DictAllTransactionUnitIdExDto.Values.FirstOrDefault(o => o.Id.ToString() == childRowData.ChildUnitId.ToString());
                            if (unitDto != null)
                            {
                                var filedDto = unitDto.DicFieldIdFieldExdto.Values.FirstOrDefault(o => o.Id.ToString() == userIdFieldId.ToString());
                                if (filedDto != null)
                                {
                                    if (childRowData.DictOneToOneFields.ContainsKey(filedDto.DataBaseFieldName) && childRowData.DictOneToOneFields[filedDto.DataBaseFieldName] != null)
                                    {
                                        toUserId = ControlTypeValueConverter.ConvertValueToInt(childRowData.DictOneToOneFields[filedDto.DataBaseFieldName]);
                                    }
                                }
                            }
                        }
                    }

                    if (toUserId.HasValue)
                    {
                        userIdList.Add(toUserId.Value);
                    }
                }

                if (action.NotificationDestinationRoleIdtransactionFiledId.HasValue)
                {
                    int? toRoleId = null;
                    int roleIdFieldId = action.NotificationDestinationRoleIdtransactionFiledId.Value;

                    if (allFreshRootValue.ContainsKey(roleIdFieldId))
                    {
                        toRoleId = ControlTypeValueConverter.ConvertValueToInt(allFreshRootValue[roleIdFieldId]);
                    }
                    else
                    {
                        if (childRowData != null && aAppTransactionExDto != null && childRowData.ChildUnitId != null)
                        {
                            var unitDto = aAppTransactionExDto.DictAllTransactionUnitIdExDto.Values.FirstOrDefault(o => o.Id.ToString() == childRowData.ChildUnitId.ToString());
                            if (unitDto != null)
                            {
                                var filedDto = unitDto.DicFieldIdFieldExdto.Values.FirstOrDefault(o => o.Id.ToString() == roleIdFieldId.ToString());
                                if (filedDto != null)
                                {
                                    if (childRowData.DictOneToOneFields.ContainsKey(filedDto.DataBaseFieldName) && childRowData.DictOneToOneFields[filedDto.DataBaseFieldName] != null)
                                    {
                                        toRoleId = ControlTypeValueConverter.ConvertValueToInt(childRowData.DictOneToOneFields[filedDto.DataBaseFieldName]);
                                    }
                                }
                            }
                        }
                    }

                    if (toRoleId.HasValue)
                    {
                        userIdList.AddRange(AppSecurityGroupBL.GetUserIdsByGroupIds(new List<int> { toRoleId.Value }));
                    }
                }

                userIdList = userIdList.Distinct().ToList();

                if (userIdList.Count > 0)
                {

                    string messageTempalte = action.NotificationMessage;

                    string currentDatetime = DateTime.UtcNow.ToShortDateString() + " " + DateTime.UtcNow.ToShortTimeString();
                    string currentUserName = AppSecurityUserBL.CurrentUserEntity.UserName;
                    string workflowName = string.Empty;
                    string taskName = string.Empty;
                    string taskStatus = string.Empty;

                    if (!(action.ActionAttribute != null && action.ActionAttribute.IsUseRichTextMessageTemplate))
                    {
                        action.MessageTemplateId = null;
                    }

                    string subject = ReplaceMessageTemplatePlaceHolderWithActualValue(rootKeyValue, allFreshRootValue, action.NotificationSubject, currentDatetime, currentUserName, workflowName, taskName, taskStatus, aAppTransactionExDto, childRowData, action.MessageTemplateId, true);
                    string messageBody = ReplaceMessageTemplatePlaceHolderWithActualValue(rootKeyValue, allFreshRootValue, action.NotificationMessage, currentDatetime, currentUserName, workflowName, taskName, taskStatus, aAppTransactionExDto, childRowData, action.MessageTemplateId, false);

                    var messageDto = new AppMessageDto();
                    messageDto.ToUserIdList = userIdList;
                    //messageDto.ToUserIdList.Add(AppSecurityUserBL.CurrentUserEntity.UserId);

                    messageDto.MessgaeScopeType = (int)EmAppMessgaeScopeType.Task;

                    //messageDto.TransactionId = ControlTypeValueConverter.ConvertValueToInt(aAppTransactionExDto.Id);
                    //messageDto.TransactionRootValueId = rootKeyValue != null ? rootKeyValue.ToString() : string.Empty;

                    messageDto.Subject = subject;
                    messageDto.Message = messageBody;



                    messageList.Add(messageDto);
                }
            }
        }

        private static void ExecutePluginWebApiCall(AppProjectWorkFlowActionExDto action, AppTransactionExDto aAppTransactionExDto, object rootPkValue, OperationCallResult<AppMasterDetailDto> aOperationCallResult)
        {
            if (!string.IsNullOrWhiteSpace(action.FormulaExpression))
            {

                string restResourceUri = action.FormulaExpression.Trim();

                var formData = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData((int)aAppTransactionExDto.Id, rootPkValue);

                OperationCallResult<AppMasterDetailDto> result = AppPluginClient.CallTransactionFormExternalService(formData, restResourceUri);

                if (result != null)
                {
                    if (result.ValidationResult.HasErrors)
                    {
                        aOperationCallResult.ValidationResult.Merge(result.ValidationResult);
                    }
                }
            }
        }


        public static string ReplaceMessageTemplatePlaceHolderWithActualValue(object rootKeyValue, Dictionary<int, object> allFreshRootValue, string messageTempalte, string currentDatetime,
            string currentUserName, string workflowName, string taskName, string taskStatus, AppTransactionExDto aAppTransactionExDto, AppChildDataDto childRowData, int? messageTemplateId, bool isSubject)
        {
            if (messageTemplateId.HasValue)
            {
                var messageTemplateEntity = AppMessageBL.RetrieveOneAppMessageEntity(messageTemplateId.Value);

                if (isSubject)
                {
                    messageTempalte = messageTemplateEntity.Subject;
                }
                else
                {
                    messageTempalte = messageTemplateEntity.Message;
                }
            }

            string currentUserId = "";
            string currentPartnerId = "";


            if (AppSecurityUserBL.CurrentUserEntity != null)
            {
                currentUserId = AppSecurityUserBL.CurrentUserEntity.UserId.ToString();
            }


            if (ServerContext.Instance.CurrnetClientIdentity != null && ServerContext.Instance.CurrnetClientIdentity.CurrentPartnerId != null)
            {
                currentPartnerId = ServerContext.Instance.CurrnetClientIdentity.CurrentPartnerId.ToString();
            }





            //messageTempalte = messageTempalte.Replace("[" + EmAppMessagePlaceHolderToken.CurrentDatetime.ToString() + "]", currentDatetime);
            string utcNowToken = string.Format(AppMessageBL.UtcDateTimeTicks_Token_StringFormat, DateTime.UtcNow.Ticks.ToString());
            string utcTodayToken = string.Format(AppMessageBL.UtcDateTimeTicks_Token_StringFormat, DateTime.UtcNow.Date.Ticks.ToString());

            messageTempalte = messageTempalte.Replace("[" + EmAppMessagePlaceHolderToken.CurrentDatetime.ToString() + "]", utcNowToken);
            messageTempalte = messageTempalte.Replace("[" + EmAppMessagePlaceHolderToken.Today.ToString() + "]", utcTodayToken);


            messageTempalte = messageTempalte.Replace("[" + EmAppMessagePlaceHolderToken.CurrentUserName.ToString() + "]", currentUserName);
            messageTempalte = messageTempalte.Replace("[" + EmAppMessagePlaceHolderToken.WorkflowtName.ToString() + "]", workflowName);
            messageTempalte = messageTempalte.Replace("[" + EmAppMessagePlaceHolderToken.TaskName.ToString() + "]", taskName);
            messageTempalte = messageTempalte.Replace("[" + EmAppMessagePlaceHolderToken.TaskStatus.ToString() + "]", taskStatus);
            messageTempalte = messageTempalte.Replace("[" + EmAppMessagePlaceHolderToken.CurrentUserId.ToString() + "]", currentUserId);
            messageTempalte = messageTempalte.Replace("[" + EmAppMessagePlaceHolderToken.CurrentPartnerId.ToString() + "]", currentPartnerId);

            if (rootKeyValue != null)
            {
                messageTempalte = messageTempalte.Replace("[" + EmAppMessagePlaceHolderToken.CurrentPkValue.ToString() + "]", rootKeyValue.ToString());
            }

            string applicationUrl = AppSystemSettingBL.GetStringValue(EmSystemSettings.ApplicationURL);
            messageTempalte = messageTempalte.Replace("[" + EmAppMessagePlaceHolderToken.ApplicationURL.ToString() + "]", applicationUrl);


            messageTempalte = ProcessTransactionFieldToken(allFreshRootValue, messageTempalte, aAppTransactionExDto, childRowData);
            messageTempalte = ProcessEncrypToken(messageTempalte);
            messageTempalte = ProcessQrLinkToken(messageTempalte);


            return messageTempalte;
        }

        private static string ProcessTransactionFieldToken(Dictionary<int, object> allFreshRootValue, string messageTempalte, AppTransactionExDto aAppTransactionExDto, AppChildDataDto childRowData)
        {
            int loopCount = 0;
            while (messageTempalte.Contains("[" + EmAppMessagePlaceHolderToken.TF.ToString()) && loopCount < 1000)
            {
                loopCount++;
                int startIndex = messageTempalte.IndexOf("[" + EmAppMessagePlaceHolderToken.TF.ToString());
                int endIndex = messageTempalte.IndexOf("]", startIndex);

                if (startIndex >= 0 && endIndex >= 0)
                {


                    int transFieldStringLength = endIndex - startIndex - 4;
                    if (transFieldStringLength > 0)
                    {
                        string needToReplaceString = messageTempalte.Substring(startIndex, endIndex - startIndex + 1);

                        string transFieldIdString = messageTempalte.Substring(startIndex + 4, transFieldStringLength);

                        if (transFieldIdString.IndexOf("_") > 0)
                        {
                            transFieldIdString = transFieldIdString.Substring(0, transFieldIdString.IndexOf("_"));
                        }

                        int? transFiledId = ControlTypeValueConverter.ConvertValueToInt(transFieldIdString);

                        string replaceToString = string.Empty;

                        if (transFiledId.HasValue && allFreshRootValue.ContainsKey(transFiledId.Value))
                        {
                            AppTransactionFieldExDto fieldDto = null;
                            bool isDDL = false;
                            bool isDateTime = false;
                            int? entityId = null;

                            if (aAppTransactionExDto.DictAllTransactionField.ContainsKey(transFiledId.Value))
                            {
                                fieldDto = aAppTransactionExDto.DictAllTransactionField[transFiledId.Value];
                                isDDL = fieldDto.ControlType == (int)EmAppControlType.DDL;
                                isDateTime = fieldDto.ControlType == (int)EmAppControlType.DateTimeDetail;
                                entityId = fieldDto.EntityId;
                            }

                            if (allFreshRootValue[transFiledId.Value] != null)
                            {
                                object transFieldValue = allFreshRootValue[transFiledId.Value];

                                replaceToString = transFieldValue.ToString();

                                if (isDDL && entityId.HasValue)
                                {
                                    List<LookupItemDto> lookupList = AppEntityInfoBL.GetLookupItemList(entityId.Value, string.Empty);
                                    LookupItemDto lookupItemFound = lookupList.FirstOrDefault(o => o.Id.ToString() == transFieldValue.ToString());

                                    if (lookupItemFound != null)
                                    {
                                        replaceToString = lookupItemFound.Display;
                                    }
                                }
                                else if (isDateTime && transFieldValue.GetType() == typeof(DateTime))
                                {
                                    DateTime? clientTime = ControlTypeValueConverter.ConvertValueToDate(transFieldValue);

                                    if (clientTime.HasValue)
                                    {
                                        DateTime utcTime = ClientTimeZoneHelper.ConvertClientToUTCDateTime(clientTime.Value);
                                        replaceToString = string.Format(AppMessageBL.UtcDateTimeTicks_Token_StringFormat, utcTime.Ticks.ToString());
                                    }

                                }
                                else if (fieldDto != null && fieldDto.ControlType == (int)EmAppControlType.Numeric)
                                {
                                    if (fieldDto.Nbdecimal.HasValue)
                                    {
                                        replaceToString = System.Math.Round(double.Parse(transFieldValue.ToString()), fieldDto.Nbdecimal.Value).ToString();
                                    }
                                    else
                                    {
                                        replaceToString = System.Math.Round(double.Parse(transFieldValue.ToString())).ToString();
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (childRowData != null && aAppTransactionExDto != null && childRowData.ChildUnitId != null)
                            {
                                var unitDto = aAppTransactionExDto.DictAllTransactionUnitIdExDto.Values.FirstOrDefault(o => o.Id.ToString() == childRowData.ChildUnitId.ToString());
                                if (unitDto != null)
                                {
                                    var fieldDto = unitDto.DicFieldIdFieldExdto.Values.FirstOrDefault(o => o.Id.ToString() == transFiledId.Value.ToString());
                                    if (fieldDto != null)
                                    {
                                        if (childRowData.DictOneToOneFields.ContainsKey(fieldDto.DataBaseFieldName) && childRowData.DictOneToOneFields[fieldDto.DataBaseFieldName] != null)
                                        {
                                            object transFieldValue = childRowData.DictOneToOneFields[fieldDto.DataBaseFieldName];
                                            replaceToString = transFieldValue.ToString();

                                            bool isDDL = fieldDto.ControlType == (int)EmAppControlType.DDL;
                                            bool isDateTime = fieldDto.ControlType == (int)EmAppControlType.DateTimeDetail; ;
                                            int? entityId = fieldDto.EntityId;

                                            if (isDDL && entityId.HasValue)
                                            {
                                                List<LookupItemDto> lookupList = AppEntityInfoBL.GetLookupItemList(entityId.Value, string.Empty);
                                                LookupItemDto lookupItemFound = lookupList.FirstOrDefault(o => o.Id.ToString() == transFieldValue.ToString());

                                                if (lookupItemFound != null)
                                                {
                                                    replaceToString = lookupItemFound.Display;
                                                }
                                            }
                                            else if (isDateTime && transFieldValue.GetType() == typeof(DateTime))
                                            {
                                                DateTime? clientTime = ControlTypeValueConverter.ConvertValueToDate(transFieldValue);

                                                if (clientTime.HasValue)
                                                {
                                                    DateTime utcTime = ClientTimeZoneHelper.ConvertClientToUTCDateTime(clientTime.Value);
                                                    replaceToString = string.Format(AppMessageBL.UtcDateTimeTicks_Token_StringFormat, utcTime.Ticks.ToString());
                                                }

                                            }
                                            else if (fieldDto != null && fieldDto.ControlType == (int)EmAppControlType.Numeric)
                                            {
                                                if (fieldDto.Nbdecimal.HasValue)
                                                {
                                                    replaceToString = System.Math.Round(double.Parse(transFieldValue.ToString()), fieldDto.Nbdecimal.Value).ToString();
                                                }
                                                else
                                                {
                                                    replaceToString = System.Math.Round(double.Parse(transFieldValue.ToString())).ToString();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        messageTempalte = messageTempalte.Replace(needToReplaceString, replaceToString);
                    }
                }
            }

            return messageTempalte;
        }


        private static string ProcessEncrypToken(string messageTempalte)
        {
            while (messageTempalte.Contains("[" + EmAppMessagePlaceHolderToken.Encrypt.ToString()))
            {
                int startIndex = messageTempalte.IndexOf("[" + EmAppMessagePlaceHolderToken.Encrypt.ToString());
                int endIndex = messageTempalte.IndexOf("_" + EmAppMessagePlaceHolderToken.Encrypt.ToString() + "]", startIndex);

                if (startIndex >= 0 && endIndex >= 0)
                {
                    int prefixLength = ("[" + EmAppMessagePlaceHolderToken.Encrypt.ToString() + "_").Length;
                    int postfixLength = ("_" + EmAppMessagePlaceHolderToken.Encrypt.ToString() + "]").Length;

                    int needToEncryptStringLength = endIndex - startIndex - prefixLength;
                    if (needToEncryptStringLength > 0)
                    {
                        string needToReplaceString = messageTempalte.Substring(startIndex, endIndex - startIndex + postfixLength);

                        string needToEncryptString = messageTempalte.Substring(startIndex + prefixLength, needToEncryptStringLength);

                        string replaceToString = AppSaasAccountUserBL.EncryptParamString(needToEncryptString);

                        messageTempalte = messageTempalte.Replace(needToReplaceString, replaceToString);
                    }
                }
            }

            return messageTempalte;
        }

        private static string ProcessQrLinkToken(string messageTempalte)
        {
            while (messageTempalte.Contains("[" + EmAppMessagePlaceHolderToken.QrLink.ToString()))
            {
                int startIndex = messageTempalte.IndexOf("[" + EmAppMessagePlaceHolderToken.QrLink.ToString());
                int endIndex = messageTempalte.IndexOf("_" + EmAppMessagePlaceHolderToken.QrLink.ToString() + "]", startIndex);

                if (startIndex >= 0 && endIndex >= 0)
                {
                    int prefixLength = ("[" + EmAppMessagePlaceHolderToken.QrLink.ToString() + "_").Length;
                    int postfixLength = ("_" + EmAppMessagePlaceHolderToken.QrLink.ToString() + "]").Length;

                    int needToEncryptStringLength = endIndex - startIndex - prefixLength;
                    if (needToEncryptStringLength > 0)
                    {
                        string applicationUrl = AppSystemSettingBL.GetStringValue(EmSystemSettings.ApplicationURL);
                        string needToReplaceString = messageTempalte.Substring(startIndex, endIndex - startIndex + postfixLength);

                        string needToEncryptString = messageTempalte.Substring(startIndex + prefixLength, needToEncryptStringLength);

                        string encryptedString = AppSaasAccountUserBL.EncryptParamString(needToEncryptString);
                        string replaceToString = replaceToString = applicationUrl + "QRDisplay.aspx?parameter=" + encryptedString;

                        messageTempalte = messageTempalte.Replace(needToReplaceString, replaceToString);
                    }
                }
            }

            return messageTempalte;
        }



        //private static void GenerateTaskStatusChangeNotes(AppProjectWorkFlowTaskExDto targetTask, string changeToStatus)
        //{
        //    if (!string.IsNullOrEmpty(targetTask.Notes) && targetTask.Notes.Length > 3000)
        //    {
        //        targetTask.Notes = "..." + System.Environment.NewLine;
        //    }

        //    targetTask.Notes += "Task Is " + changeToStatus + " By " + AppSecurityUserBL.CurrentUserEntity.UserName
        //                    + " At " + DateTime.UtcNow.ToString() + " (UTC)." + System.Environment.NewLine;
        //}


        //private static void GenerateTaskStatusChangeEmail(AppProjectWorkFlowTaskExDto targetTask, List<AppMessageDto> messageList, string changeToStatus)
        //{
        //    foreach (var resource in targetTask.AppProjectTaskResourceList)
        //    {
        //        if (resource.UserId.HasValue)
        //        {
        //            string currentUserName = AppSecurityUserBL.CurrentUserEntity.UserName;


        //            var messageDto = new AppMessageDto();
        //            messageDto.MessgaeScopeType = (int)EmAppMessgaeScopeType.Workflow;

        //            if (targetTask.ForeignAppProjectOrWorkFlowExDto != null)
        //            {
        //                messageDto.TransactionId = targetTask.ForeignAppProjectOrWorkFlowExDto.TransactionId;
        //                messageDto.TransactionRootValueId = targetTask.ForeignAppProjectOrWorkFlowExDto.TransactionRid;
        //                messageDto.ProjectId = (int)targetTask.ForeignAppProjectOrWorkFlowExDto.Id;

        //            }

        //            messageDto.ProjectActivityId = (int)targetTask.Id;

        //            messageDto.Subject = "Task " + targetTask.Name + " Is " + changeToStatus;
        //            messageDto.Message = "Task " + targetTask.Name + " On Workflow " + targetTask.ProjectName + " Is " + changeToStatus
        //                + " At " + DateTime.UtcNow.ToString() + " (UTC)"
        //                + " By " + currentUserName + ".";                                       


        //            //  {0}:Task Name,  {1}:Task Description, {2}:Task Notes,  {3}:Resources String, {4}:Workflow Name,
        //            messageDto.Message += string.Format(AppMessageBL.EmailTemplate_WorkFlowTaskDetail,
        //                targetTask.Name,
        //                targetTask.Description,
        //                //targetTask.Notes,
        //                currentUserName,
        //                targetTask.ProjectName);



        //            //messageDto.Message



        //            messageDto.ToList = resource.UserId.Value.ToString() + "|";
        //            messageList.Add(messageDto);
        //        }
        //    }
        //}


        //private static void GenerateTaskStatusChangeSystemConversation(AppProjectWorkFlowTaskExDto targetTask, string changeToStatus)
        //{
        //    string newMessage = "Task Is " + changeToStatus + " By " + AppSecurityUserBL.CurrentUserEntity.UserName;
        //    targetTask.NewSystemConversationMessageList.Add(newMessage);
        //}


        private static void GenerateWorkflowTaskStatusChangeMessage(AppProjectWorkFlowTaskExDto targetTask, List<AppMessageDto> messageList, string changeToStatus)
        {
            string currentUserName = AppSecurityUserBL.CurrentUserEntity.UserName;

            var messageDto = new AppMessageDto();
            messageDto.ToUserIdList = new List<int>();
            messageDto.ToUserIdList.Add(AppSecurityUserBL.CurrentUserEntity.UserId);
            messageDto.ProjectId = targetTask.ProjectId;
            messageDto.ProjectActivityId = (int)targetTask.Id;
            messageDto.Subject = "Task " + targetTask.Name + " Is " + changeToStatus;
            messageDto.Message = "Task " + targetTask.Name + " On Workflow " + targetTask.ProjectName + " Is " + changeToStatus
                //+ " At " + DateTime.UtcNow.ToString() + " (UTC)"
                + " By " + currentUserName + ".";


            ////  {0}:Task Name,  {1}:Task Description, {2}:Task Notes,  {3}:Resources String, {4}:Workflow Name,
            //messageDto.Message += string.Format(AppMessageBL.EmailTemplate_WorkFlowTaskDetail,
            //    targetTask.Name,
            //    targetTask.Description,
            //    string.Empty,
            //    currentUserName,
            //    targetTask.ProjectName);



            if (targetTask.TaskOwnerId.HasValue && !messageDto.ToUserIdList.Contains(targetTask.TaskOwnerId.Value))
            {
                messageDto.ToUserIdList.Add(targetTask.TaskOwnerId.Value);
            }

            foreach (var resource in targetTask.AppProjectTaskResourceList)
            {
                if (resource.UserId.HasValue && !messageDto.ToUserIdList.Contains(resource.UserId.Value))
                {
                    messageDto.ToUserIdList.Add(resource.UserId.Value);
                }
            }

            messageList.Add(messageDto);
        }

        private static void SendWorkflowAllTaskStatusChangeMessage(List<AppMessageDto> messageList)
        {
            foreach (AppMessageDto message in messageList)
            {
                List<int> mandatoryUserIdList = message.ToUserIdList;

                string subject = message.Subject;
                string messageText = message.Message;
                int? taskId = message.ProjectActivityId;
                int? workflowId = message.ProjectId;
                int? transactionId = message.TransactionId;
                string transactionRId = message.TransactionRootValueId;

                EmAppMessgaeScopeType messageScope = EmAppMessgaeScopeType.Task;

                if (message.MessgaeScopeType.HasValue)
                {
                    messageScope = (EmAppMessgaeScopeType)message.MessgaeScopeType.Value;
                }


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
                    message.IsAttachAllFormFiles);
            }
        }

    }




}


