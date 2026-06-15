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

using APP.Framework;
namespace App.BL
{


    public static class AppProjectWorkFlowProcessBL
    {


        public static List<AppProjectOrWorkFlowDto> GetTransactionProjectWorkFlowTemplateList(int transactionId)
        {       

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppProjectOrWorkFlowEntity> listWorkFlow = new EntityCollection<AppProjectOrWorkFlowEntity>();
                SortExpression expression = new SortExpression(AppProjectOrWorkFlowFields.Name | SortOperator.Ascending);
                adapter.FetchEntityCollection(listWorkFlow, new RelationPredicateBucket(AppProjectOrWorkFlowFields.TransactionId == transactionId 
                    & (AppProjectOrWorkFlowFields.TransactionRid == DBNull.Value | AppProjectOrWorkFlowFields.TransactionRid == string.Empty)
                    & AppProjectOrWorkFlowFields.IsPredefined == true), 0, expression);

                var aDtoList = new List<AppProjectOrWorkFlowDto>();

                foreach (var porjectEntity in listWorkFlow)
                {
                    aDtoList.Add(AppProjectOrWorkFlowConverter.ConvertEntityToDto(porjectEntity));
                }

                return aDtoList;
            }


        }

        public static OperationCallResult<AppProjectOrWorkFlowExDto> SaveAsTransactionWorkFlow(int projectOrWorkflowId, object trascationRootValueId)
        {
            AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto = AppProjectWorkFlowStructureBL.RetrieveOneAppProjectOrWorkFlowExDto(projectOrWorkflowId);

            aAppProjectOrWorkFlowExDto.Id = null;
			aAppProjectOrWorkFlowExDto.TransactionRid = trascationRootValueId.ToString();
            aAppProjectOrWorkFlowExDto.IsPredefined = false;
            aAppProjectOrWorkFlowExDto.RuntimeOriginalProjectId = projectOrWorkflowId;
            aAppProjectOrWorkFlowExDto.Name = aAppProjectOrWorkFlowExDto.Name + " #"+ trascationRootValueId.ToString();

            Dictionary<Guid, Guid> dictOldmapNewGuid = new Dictionary<Guid, Guid>();

            foreach (AppProjectWorkFlowTaskExDto appProjectWorkFlowTask in aAppProjectOrWorkFlowExDto.RootTreeList)
            {
                Guid oldGuid = appProjectWorkFlowTask.RowIdentity.Value ;
                Guid newGuid = Guid.NewGuid();

                dictOldmapNewGuid.Add(oldGuid, newGuid);
                appProjectWorkFlowTask.RowIdentity = newGuid;


            }

            foreach (AppProjectWorkFlowTaskExDto appProjectWorkFlowTask in aAppProjectOrWorkFlowExDto.RootTreeList)
            {


                // MainTas task Dto

                if (appProjectWorkFlowTask.MainTaskGuId.HasValue)
                {
                    appProjectWorkFlowTask.MainTaskGuId = dictOldmapNewGuid[appProjectWorkFlowTask.MainTaskGuId.Value];
                }

                // prodecessor
                foreach (var processDtoDto in appProjectWorkFlowTask.AppProjectTaskPredecessorList)
                {
                    processDtoDto.PredecessorGuid = dictOldmapNewGuid[processDtoDto.PredecessorGuid.Value];

                }




            }


            // condition and Actiom..

            foreach (var conditionDto in aAppProjectOrWorkFlowExDto.AppProjectWorkFlowConditionList)
            {
                foreach (var actionDto in conditionDto.AppProjectWorkFlowActionList)
                {
                    //AddActionDtoToConditionEntity(conditionEntity, actionDto);

                    if (actionDto.NextWorkFlowGuId.HasValue)
                    {
                        actionDto.NextWorkFlowGuId = dictOldmapNewGuid[actionDto.NextWorkFlowGuId.Value];
                    }

                }

            }

            //var startTaskDto = aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList.FirstOrDefault(o => o.StageType == (int)EmAppWorkflowTaskStageType.Start);

            //if (startTaskDto != null)
            //{
            //    StartOneTask(aAppProjectOrWorkFlowExDto, startTaskDto);
            //}

            return AppProjectWorkFlowStructureBL.SaveProjectOrWorkFlowExDto(aAppProjectOrWorkFlowExDto);

        }



        public static OperationCallResult<object> CreateFormRunningTimeTaskWorkflows(AppTransactionExDto transactionExDto, object rootPkValue)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (transactionExDto.MasterWorkflowId.HasValue && rootPkValue != null)
            {
                var taskWorkFlowPorcessREsult = AppProjectWorkFlowProcessBL.SaveAsTransactionWorkFlow(transactionExDto.MasterWorkflowId.Value, rootPkValue);
           
                aValidationResult.Merge(taskWorkFlowPorcessREsult.ValidationResult);
            }

            if (!aValidationResult.HasErrors)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_MasterDetailFormDataLoad_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                aOperationCallResult.Object = true;
            }

            return aOperationCallResult;
        }

        public static AppProjectOrWorkFlowExDto GetTransactionRunningProjectWorkflow(int transactionId, object rootPrimaryKeyValue, bool isCallingFromBrowser = true)
        {
            AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);

            if (transactionExDto != null)
            {
                if (transactionExDto.MasterWorkflowId.HasValue)
                {

                    EntityCollection<AppProjectOrWorkFlowEntity> listWorkFlow = new EntityCollection<AppProjectOrWorkFlowEntity>();

                    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                    {
                        IncludeFieldsList includeFieldsList = new IncludeFieldsList();
                        includeFieldsList.Add(AppProjectOrWorkFlowFields.ProjectId);
                        includeFieldsList.Add(AppProjectOrWorkFlowFields.RuntimeOriginalProjectId);

                        var predict = new RelationPredicateBucket(AppProjectOrWorkFlowFields.RuntimeOriginalProjectId == transactionExDto.MasterWorkflowId.Value & AppProjectOrWorkFlowFields.TransactionRid == rootPrimaryKeyValue);
                        adapter.FetchEntityCollection(listWorkFlow, includeFieldsList, predict);

                    }

                    if (listWorkFlow.Count > 0)
                    {
                        var workflowExDto = AppProjectWorkFlowStructureBL.RetrieveOneAppProjectOrWorkFlowExDto(listWorkFlow[0].ProjectId);

                        if (workflowExDto != null)
                        {
                            foreach (var taskDto in workflowExDto.RootTreeList)
                            {
                                taskDto.ProjectName = workflowExDto.Name;

                                if (!isCallingFromBrowser)
                                {
                                    taskDto.DateActualEnd = ClientTimeZoneHelper.ConvertClientToUTCDateTime(taskDto.DateActualEnd);
                                    taskDto.DateActualStart = ClientTimeZoneHelper.ConvertClientToUTCDateTime(taskDto.DateActualStart);

                                   

                                    taskDto.DatePlannedEnd = ClientTimeZoneHelper.ConvertClientToUTCDateTime(taskDto.DatePlannedEnd);
                                    taskDto.DatePlannedStart = ClientTimeZoneHelper.ConvertClientToUTCDateTime(taskDto.DatePlannedStart);

                                }
                            }
                        }
                        return workflowExDto;
                    }

                    
                }
                else if (transactionExDto.MasterTransactionId.HasValue)
                {
                    return GetTransactionRunningProjectWorkflow(transactionExDto.MasterTransactionId.Value, rootPrimaryKeyValue);
                }
            }

            return null;
        }
        public static List<AppProjectOrWorkFlowExDto> GetTransactionRunningProjectWorkflowList(int transactionId, object rootPrimaryKeyValue)
        {
            var aDtoList = new List<AppProjectOrWorkFlowExDto>();
            AppProjectOrWorkFlowExDto workflowExDto = GetTransactionRunningProjectWorkflow(transactionId, rootPrimaryKeyValue);

            if (workflowExDto != null)
            {
                aDtoList.Add(workflowExDto);
            }

            return aDtoList;

            //EntityCollection<AppProjectOrWorkFlowEntity> listWorkFlow = new EntityCollection<AppProjectOrWorkFlowEntity>();

            //using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            //{
            //    IncludeFieldsList includeFieldsList = new IncludeFieldsList();
            //    includeFieldsList.Add(AppProjectOrWorkFlowFields.TransactionId);

            //    var predict = new RelationPredicateBucket(AppProjectOrWorkFlowFields.TransactionId == transactionId & AppProjectOrWorkFlowFields.TransactionRid == rootPrimaryKeyValue);
            //    adapter.FetchEntityCollection(listWorkFlow, includeFieldsList, predict);

            //}

            //var aDtoList = new List<AppProjectOrWorkFlowExDto>();

            //foreach (var porjectEntity in listWorkFlow)
            //{
            //    var workflowExDto = AppProjectWorkFlowStructureBL.RetrieveOneAppProjectOrWorkFlowExDto(porjectEntity.ProjectId);
            //    if (workflowExDto != null)
            //    {
            //        foreach (var taskDto in workflowExDto.RootTreeList)
            //        {
            //            taskDto.ProjectName = workflowExDto.Name;
            //        }

            //        aDtoList.Add(workflowExDto);
            //    }               
            //}

            //return aDtoList;

        }



        private static List<AppProjectWorkFlowTaskExDto> GetTaskSuccessorList(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, AppProjectWorkFlowTaskExDto aTaskExDto)
        {
            List<AppProjectWorkFlowTaskExDto> successorList = new List<AppProjectWorkFlowTaskExDto>();

            foreach (var aPredecessor in aAppProjectOrWorkFlowExDto.PredecessorList.Where(o => o.PredecessorGuid == aTaskExDto.RowIdentity))
            {
                if (aPredecessor.CurrentTaskGuid.HasValue)
                {
                    var needToStartTaskDto = aAppProjectOrWorkFlowExDto.RootTreeList.FirstOrDefault(o => o.RowIdentity == aPredecessor.CurrentTaskGuid.Value);
                    if (needToStartTaskDto != null)
                    {
                        successorList.Add(needToStartTaskDto);
                    }
                }
            }

            return successorList;
        }


        public static void UpdateProcessProjectsWorkflow(Dictionary<int, Dictionary<int, object>> dictProjectConditionSubitemValue, Dictionary<int, Dictionary<int, object>> dictProjectActionSubitemValue, Dictionary<int, int> dictSubItemControlType)
        {
            // need a control type here

            // ControlTypeValueConverter.ConvertValueToObject(

            foreach (int projectId in dictProjectConditionSubitemValue.Keys)
            {
                Dictionary<int, object> modifiedConditionsSubItemsDictionary = dictProjectConditionSubitemValue[projectId];
                Dictionary<int, object> modifiedActionsSubItemsDictionary = dictProjectActionSubitemValue[projectId];

              
            }
        }

        

    } 


}


