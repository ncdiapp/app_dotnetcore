using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using APP.Components.EntityConverter;
using System.Data;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using System;

using APP.Framework;
namespace App.BL
{


    public static class AppUserTaskListBL
    {

        public static List<AppProjectWorkFlowTaskExDto> RetrieveUserTaskList(TaskFilterOptionDto filterOption, bool isCurrentUserTaskOnly)
        {
            List<AppProjectWorkFlowTaskExDto> userTaskList = new List<AppProjectWorkFlowTaskExDto>();

            EntityCollection<AppProjectWorkFlowTaskEntity> taskEntltyList = new EntityCollection<AppProjectWorkFlowTaskEntity>();

            DateTime clientToday = ClientTimeZoneHelper.ConvertUTCToClientDateTime(DateTime.UtcNow).Date;
            DateTime clentStartDateOfThisWeek = clientToday.AddDays(((clientToday.DayOfWeek - DayOfWeek.Monday + 7) % 7) * -1);
            DateTime clentStartDateOfNextWeek = clentStartDateOfThisWeek.AddDays(7);

            DateTime clientToday_UtcTime = ClientTimeZoneHelper.ConvertClientToUTCDateTime(clientToday);
            DateTime clientTomorrow_UtcTime = clientToday_UtcTime.AddDays(1);
            DateTime clentStartDateOfThisWeek_UtcTime = ClientTimeZoneHelper.ConvertClientToUTCDateTime(clentStartDateOfThisWeek);
            DateTime clentStartDateOfNextWeek_UtcTime = ClientTimeZoneHelper.ConvertClientToUTCDateTime(clentStartDateOfNextWeek);
            DateTime clentEndDateOfNextWeek_UtcTime = clentStartDateOfNextWeek_UtcTime.AddDays(7);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                var filter = new RelationPredicateBucket();

                filter.PredicateExpression.AddWithAnd(
                    AppProjectWorkFlowTaskFields.DiagramShapeType == System.DBNull.Value |
                    (AppProjectWorkFlowTaskFields.DiagramShapeType != (int)EmAppWorkflowDiagramShapeType.StartStep
                    & AppProjectWorkFlowTaskFields.DiagramShapeType != (int)EmAppWorkflowDiagramShapeType.EndStep));


                if (isCurrentUserTaskOnly)
                {
                    //filter.PredicateExpression.AddWithAnd(AppProjectWorkFlowTaskFields.TaskOwnerId == AppSecurityUserBL.CurrentUserId);

                    filter.Relations.Add(AppProjectWorkFlowTaskEntity.Relations.AppProjectTaskResourceEntityUsingProjectWorkFlowTaskId);
                    filter.PredicateExpression.AddWithAnd(AppProjectTaskResourceFields.UserId == AppSecurityUserBL.CurrentUserId);
                }

                if (filterOption != null)
                {
                    if (!filterOption.SelectedTaskProgressList.IsEmpty())
                    {
                        List<int> progressIdList = filterOption.SelectedTaskProgressList.Select(o => (int)o).ToList();

                        if (progressIdList.IndexOf((int)EmAppProjectTaskProgress.NotStarted) >= 0)
                        {
                            filter.PredicateExpression.AddWithAnd(AppProjectWorkFlowTaskFields.ProgressId == progressIdList | AppProjectWorkFlowTaskFields.ProgressId == System.DBNull.Value);
                        }
                        else
                        {
                            filter.PredicateExpression.AddWithAnd(AppProjectWorkFlowTaskFields.ProgressId == progressIdList);
                        }
                    }

                    if (!filterOption.SelectedTaskPriorityList.IsEmpty())
                    {
                        List<int> priorityIdList = filterOption.SelectedTaskPriorityList.Select(o => (int)o).ToList();
                        filter.PredicateExpression.AddWithAnd(AppProjectWorkFlowTaskFields.EmPriority == priorityIdList);
                    }

                    if (!filterOption.SelectedTaskDueDateTypeList.IsEmpty())
                    {
                        PredicateExpression predicateToAdd = new PredicateExpression();

                        foreach (EmAppTaskDueDateType emDueDateType in filterOption.SelectedTaskDueDateTypeList)
                        {
                            //if (emDueDateType == EmAppTaskDueDateType.Overdue)
                            //{
                            //    predicateToAdd.AddWithOr(
                            //        (AppProjectWorkFlowTaskFields.ProgressId != (int)EmAppProjectTaskProgress.Done | AppProjectWorkFlowTaskFields.ProgressId == System.DBNull.Value)
                            //        & (AppProjectWorkFlowTaskFields.DatePlannedEnd != System.DBNull.Value & AppProjectWorkFlowTaskFields.DatePlannedEnd < clientTodayUtcTime)
                            //        );
                            //}

                            if (emDueDateType == EmAppTaskDueDateType.NoDueDate)
                            {
                                predicateToAdd.AddWithOr(
                                    (AppProjectWorkFlowTaskFields.DatePlannedEnd == System.DBNull.Value)
                                    );
                            }

                            if (emDueDateType == EmAppTaskDueDateType.Earlier)
                            {
                                predicateToAdd.AddWithOr(
                                    (AppProjectWorkFlowTaskFields.DatePlannedEnd != System.DBNull.Value & AppProjectWorkFlowTaskFields.DatePlannedEnd < clientToday_UtcTime)
                                    );
                            }

                            if (emDueDateType == EmAppTaskDueDateType.Today)
                            {
                                predicateToAdd.AddWithOr(
                                     (AppProjectWorkFlowTaskFields.DatePlannedEnd != System.DBNull.Value
                                        & AppProjectWorkFlowTaskFields.DatePlannedEnd >= clientToday_UtcTime & AppProjectWorkFlowTaskFields.DatePlannedEnd < clientTomorrow_UtcTime)
                                    );
                            }

                            if (emDueDateType == EmAppTaskDueDateType.ThisWeek)
                            {
                                predicateToAdd.AddWithOr(
                                    (AppProjectWorkFlowTaskFields.DatePlannedEnd != System.DBNull.Value
                                       & AppProjectWorkFlowTaskFields.DatePlannedEnd >= clentStartDateOfThisWeek_UtcTime & AppProjectWorkFlowTaskFields.DatePlannedEnd < clentStartDateOfNextWeek_UtcTime)
                                   );
                            }

                            if (emDueDateType == EmAppTaskDueDateType.NextWeek)
                            {
                                predicateToAdd.AddWithOr(
                                    (AppProjectWorkFlowTaskFields.DatePlannedEnd != System.DBNull.Value
                                       & AppProjectWorkFlowTaskFields.DatePlannedEnd >= clentStartDateOfNextWeek_UtcTime & AppProjectWorkFlowTaskFields.DatePlannedEnd < clentEndDateOfNextWeek_UtcTime)
                                   );
                            }

                            if (emDueDateType == EmAppTaskDueDateType.Later)
                            {
                                predicateToAdd.AddWithOr(
                                    (AppProjectWorkFlowTaskFields.DatePlannedEnd != System.DBNull.Value
                                       & AppProjectWorkFlowTaskFields.DatePlannedEnd >= clentEndDateOfNextWeek_UtcTime)
                                   );
                            }
                        }

                        filter.PredicateExpression.AddWithAnd(predicateToAdd);
                    }
                }
                //else
                //{
                //    if (emAppMyTaskViewOption == EmApprTaskViewOption.Incomplete)
                //    {
                //        filter.PredicateExpression.AddWithAnd((AppProjectWorkFlowTaskFields.DateActualEnd == System.DBNull.Value
                //            & AppProjectWorkFlowTaskFields.StageStatusFlag == System.DBNull.Value)
                //            |
                //            (AppProjectWorkFlowTaskFields.StageStatusFlag == (int)EmAppWorkflowTaskStageStatus.Started
                //            | AppProjectWorkFlowTaskFields.StageStatusFlag == (int)EmAppWorkflowTaskStageStatus.Holding));
                //    }
                //    else if (emAppMyTaskViewOption == EmApprTaskViewOption.Completed)
                //    {
                //        filter.PredicateExpression.AddWithAnd((AppProjectWorkFlowTaskFields.DateActualEnd != System.DBNull.Value
                //            & AppProjectWorkFlowTaskFields.StageStatusFlag == System.DBNull.Value)
                //            | AppProjectWorkFlowTaskFields.StageStatusFlag == (int)EmAppWorkflowTaskStageStatus.Completed
                //            | AppProjectWorkFlowTaskFields.StageStatusFlag == (int)EmAppWorkflowTaskStageStatus.Rejected
                //            | AppProjectWorkFlowTaskFields.StageStatusFlag == (int)EmAppWorkflowTaskStageStatus.Ignored);
                //    }
                //}


                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppProjectWorkFlowTaskEntity);
                rootPath.Add(AppProjectWorkFlowTaskEntity.PrefetchPathAppProjectTaskResource);

                adapter.FetchEntityCollection(taskEntltyList, filter, 0, null, rootPath);

                //adapter.FetchEntityCollection(taskEntltyList, filter);

                List<int> projectOrWorkflowIdList = taskEntltyList.Where(o => o.ProjectId.HasValue).Select(o => o.ProjectId.Value).Distinct().ToList();

                PrepareUserTaskDtoList(userTaskList, taskEntltyList, projectOrWorkflowIdList);

                if (filterOption != null && !filterOption.SelectedTaskTypeList.IsEmpty())
                {
                    List<int> taskTypeIdList = filterOption.SelectedTaskTypeList.Select(o => (int)o).ToList();
                    userTaskList = userTaskList.Where(o => o.EmAppTaskSystemDefinedCategory.HasValue && taskTypeIdList.IndexOf(o.EmAppTaskSystemDefinedCategory.Value) >= 0).ToList();
                }

            }

            List<LookupItemDto> userLookupItems = AppEntityInfoBL.GetLookupItemListByCode(EmAppEntityLookupInfoCode.AppSecurityUser.ToString(), "");
            Dictionary<int, string> dictUserIdName = userLookupItems.ToDictionary(o => (int)o.Id, o => o.Display);

            userTaskList.ForAll(aTask =>
            {
                if (aTask.DatePlannedEnd.HasValue)
                {
                    aTask.DueDateDisplay = aTask.DatePlannedEnd.Value.ToString("yyyy-MM-dd") + " ";
                }
                else
                {
                    aTask.DueDateDisplay = "Unfilled Due Date";
                }

                AppProjectWorkFlowStructureBL.InitialTaskStatus(aTask, true, clientToday);

                if (aTask.ProjectActivityStatusId.HasValue)
                {                   
                    aTask.TaskStatusDisplay = ((EmAppProjectTaskStatus)aTask.ProjectActivityStatusId.Value).ToString();
                }

                aTask.ResourceDisplay = string.Empty;

                if (!aTask.AppProjectTaskResourceList.IsEmpty())
                {
                    foreach (var resourceDto in aTask.AppProjectTaskResourceList)
                    {
                        if (resourceDto.UserId.HasValue && dictUserIdName.ContainsKey(resourceDto.UserId.Value))
                        {
                            aTask.ResourceDisplay += dictUserIdName[resourceDto.UserId.Value] + ";";
                        }
                    }
                }
            });

            return userTaskList;
        }

        private static void PrepareUserTaskDtoList(List<AppProjectWorkFlowTaskExDto> userTaskList, EntityCollection<AppProjectWorkFlowTaskEntity> taskEntltyList, List<int> projectOrWorkflowIdList)
        {
            Dictionary<int, AppProjectOrWorkFlowDto> dictProjectIdAndProjectDto = new Dictionary<int, AppProjectOrWorkFlowDto>();

            if (projectOrWorkflowIdList.Count > 0)
            {
                dictProjectIdAndProjectDto = AppProjectSettingBL.RetrieveAppProjectAndWorkflowListByIds(projectOrWorkflowIdList)
                    .ToDictionary(o => (int)o.Id, o => o);
            }

            Dictionary<int, AppTransactionDto> dictTransIdTransDto = AppTransactionBL.RetrieveAllAppTransactionDto(null, (int)EmTransactionOrganizedType.MasterDetail).ToDictionary(o => (int)o.Id, o => o);

            foreach (var entity in taskEntltyList)
            {
                var taskDto = AppProjectWorkFlowTaskConverter.ConvertEntityToExDto(entity);

                foreach (var resourceEntity in entity.AppProjectTaskResource)
                {
                    taskDto.AppProjectTaskResourceList.Add(AppProjectTaskResourceConverter.ConvertEntityToExDto(resourceEntity));
                }

                bool includeThisTask = false;

                if (taskDto.ProjectId.HasValue && dictProjectIdAndProjectDto.ContainsKey(taskDto.ProjectId.Value))
                {
                    includeThisTask = PrepareMyTaskProjectOrWorkflowData(dictProjectIdAndProjectDto, taskDto);
                }
                else if (taskDto.TransactionId.HasValue)
                {
                    includeThisTask = PrepareMyTaskSimpleFormData(dictTransIdTransDto, taskDto);
                }
                else
                {
                    taskDto.EmAppTaskSystemDefinedCategory = (int)EmAppTaskSystemDefinedCategory.UserDefinedFreeTask;
                    taskDto.CategoryDetailDisplay = StringLocalizer.Localize("script_TaskManagement_StandaloneTasks", "Standalone Tasks");
                    includeThisTask = true;
                }

                if (includeThisTask)
                {
                    AssignMyTaskPhaseAndStage(taskDto);
                    userTaskList.Add(taskDto);
                }
            }

            userTaskList.Add(new AppProjectWorkFlowTaskExDto() { EmAppTaskOwnerDeliverPhase = (int)EmAppTaskOwnerDeliverPhase.New });
            userTaskList.Add(new AppProjectWorkFlowTaskExDto() { EmAppTaskOwnerDeliverPhase = (int)EmAppTaskOwnerDeliverPhase.Today });
            userTaskList.Add(new AppProjectWorkFlowTaskExDto() { EmAppTaskOwnerDeliverPhase = (int)EmAppTaskOwnerDeliverPhase.Upcoming });
            userTaskList.Add(new AppProjectWorkFlowTaskExDto() { EmAppTaskOwnerDeliverPhase = (int)EmAppTaskOwnerDeliverPhase.Later });
        }





        private static bool PrepareMyTaskProjectOrWorkflowData(Dictionary<int, AppProjectOrWorkFlowDto> dictProjectIdAndProjectDto, AppProjectWorkFlowTaskExDto taskDto)
        {
            var projectDto = dictProjectIdAndProjectDto[taskDto.ProjectId.Value];
            bool isMyTask = false;

            if (projectDto.ProjectWorkflowType.HasValue && projectDto.ProjectWorkflowType.Value == (int)EmAppProjectWorkflowType.BusinessProcessWorkflow)
            {
                if (!string.IsNullOrWhiteSpace(projectDto.TransactionRid))
                {
                    taskDto.EmAppTaskSystemDefinedCategory = (int)EmAppTaskSystemDefinedCategory.WorkflowTask;
                    taskDto.CategoryDetailDisplay = StringLocalizer.Localize("script_TaskManagement_Workflow", "Workflow") + ": " + projectDto.Name;
                    //taskDto.WorkflowTransactionId = projectDto.TransactionId;
                    taskDto.WorkflowTransactionRId = projectDto.TransactionRid;
                    isMyTask = true;
                }
            }
            else
            {
                //if (projectDto.DateActualStart.HasValue)
                //{
                taskDto.EmAppTaskSystemDefinedCategory = (int)EmAppTaskSystemDefinedCategory.ProjectTask;
                taskDto.CategoryDetailDisplay = StringLocalizer.Localize("script_TaskManagement_Project", "Project") + ": " + projectDto.Name;
                isMyTask = true;
                //}
            }

            return isMyTask;
        }


        private static bool PrepareMyTaskSimpleFormData(Dictionary<int, AppTransactionDto> dictTransIdTransDto, AppProjectWorkFlowTaskExDto taskDto)
        {
            bool isMyTask = false;

            if (!string.IsNullOrEmpty(taskDto.TransactionRid))
            {
                taskDto.EmAppTaskSystemDefinedCategory = (int)EmAppTaskSystemDefinedCategory.SimpleFormTask;

                if (dictTransIdTransDto.ContainsKey(taskDto.TransactionId.Value))
                {
                    var transDto = dictTransIdTransDto[taskDto.TransactionId.Value];
                    taskDto.CategoryDetailDisplay = StringLocalizer.Localize("script_TaskManagement_Form", "Form") + ": " + transDto.TransactionName + " (#" + taskDto.TransactionRid.ToString() + ")";
                }

                isMyTask = true;
            }

            return isMyTask;
        }


        private static void AssignMyTaskPhaseAndStage(AppProjectWorkFlowTaskExDto taskDto)
        {
            if (!taskDto.EmAppTaskOwnerDeliverPhase.HasValue)
            {
                taskDto.EmAppTaskOwnerDeliverPhase = (int)EmAppTaskOwnerDeliverPhase.New;
            }

            if (taskDto.ProgressId.HasValue)
            {
                if (taskDto.ProgressId.Value == (int)EmAppProjectTaskProgress.Done)
                {
                    taskDto.EmAppProjectTaskStage = EmAppProjectTaskStage.Completed;
                }
                else if (taskDto.ProgressId.Value == (int)EmAppProjectTaskProgress.Started
                    || taskDto.ProgressId.Value == (int)EmAppProjectTaskProgress.HalfwayDone
                    || taskDto.ProgressId.Value == (int)EmAppProjectTaskProgress.AlmostDone)
                {
                    taskDto.EmAppProjectTaskStage = EmAppProjectTaskStage.Started;
                }
                else
                {
                    taskDto.EmAppProjectTaskStage = EmAppProjectTaskStage.NotStarted;
                }
            }
            else
            {
                taskDto.EmAppProjectTaskStage = EmAppProjectTaskStage.NotStarted;
            }
        }


        private static OperationCallResult<bool> UpdateOneTaskEntity(int taskId, AppProjectWorkFlowTaskEntity updateEntity)
        {
            if (updateEntity != null)
            {
                OperationCallResult<bool> operationCallResult = new OperationCallResult<bool>();
                ValidationResult validationResult = new ValidationResult();
                operationCallResult.ValidationResult = validationResult;

                // To Do: Need to check Task complete dependency. Give warning message if the task is not allowed to complete now.

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.UpdateEntitiesDirectly(updateEntity, new RelationPredicateBucket(AppProjectWorkFlowTaskFields.ProjectWorkFlowTaskId == taskId));
                        adapter.Commit();
                        operationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppProjectWorkFlowTaskEntity), "App_AppProjectWorkFlowTaskEntity_Update_OK", ValidationItemType.Message, "Task Update Successfully"));
                        operationCallResult.Object = true;
                    }
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        operationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppProjectWorkFlowTaskEntity), "App_AppProjectWorkFlowTaskEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }

                return operationCallResult;
            }

            return null;
        }

        public static OperationCallResult<bool> CompleteOneTask(AppProjectWorkFlowTaskDto taskDto)
        {
            OperationCallResult<bool> operationCallResult = new OperationCallResult<bool>();

            int taskId = (int)taskDto.Id;
            // To Do: Need to check Task complete dependency. Give warning message if the task is not allowed to complete.


            // step1: Check predesceiir
            //step2: check check List
            // step

            string validationMessage = VerifyProjectTaskCompleteCondition(taskDto);

            if (!string.IsNullOrEmpty(validationMessage))
            {
                operationCallResult.ValidationResult = new ValidationResult();

                operationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppProjectWorkFlowTaskEntity), "App_AppProjectWorkFlowTaskEntity_ValidationError", ValidationItemType.Error, validationMessage));

                operationCallResult.Object = false;

                return operationCallResult;
            }
            else
            {
                AppProjectWorkFlowTaskEntity updateEntity = new AppProjectWorkFlowTaskEntity();

                updateEntity.ProgressId = (int)EmAppProjectTaskProgress.Done;

                var utcNow = System.DateTime.UtcNow;

                if (!taskDto.DateActualStart.HasValue)
                {
                    if (taskDto.DatePlannedStart.HasValue)
                    {
                        updateEntity.DateActualStart = taskDto.DatePlannedStart;
                    }
                    else
                    {
                        updateEntity.DateActualStart = utcNow;
                    }
                }

                updateEntity.DateActualEnd = utcNow;

                operationCallResult = UpdateOneTaskEntity(taskId, updateEntity);

                if (operationCallResult.IsSuccessfulWithResult)
                {

                    string messageText = string.Empty;
                    var userEntity = AppSecurityUserBL.CurrentUserEntity;

                    string subject = "Task \"" + taskDto.Name + "\" Completed";
                    messageText = "Task Is Completed By " + userEntity.UserName;

                    List<int> mandatoryUserIdList = new List<int>();

                    mandatoryUserIdList.Add(userEntity.UserId);

                    if (taskDto.TaskOwnerId.HasValue && !mandatoryUserIdList.Contains(taskDto.TaskOwnerId.Value))
                    {
                        mandatoryUserIdList.Add(taskDto.TaskOwnerId.Value);
                    }

                    AppMessageBL.SendNewAppMessageByScope(EmAppMessgaeScopeType.Task, null, string.Empty, (int)taskDto.Id, taskDto.ProjectId, null, EmAppMessgaePostType.SystemNotification, subject, messageText, mandatoryUserIdList);

                }

                return operationCallResult;
            }

        }

        public static OperationCallResult<bool> UnCompleteOneTask(AppProjectWorkFlowTaskDto taskDto)
        {
            OperationCallResult<bool> operationCallResult = new OperationCallResult<bool>();

            int taskId = (int)taskDto.Id;
            // To Do: Need to check Task uncomplete dependency. Give warning message if the task is not allowed to uncomplete.

            string validationMessage = VerifyProjectTaskSetIncompleteCondition(taskDto);

            if (!string.IsNullOrEmpty(validationMessage))
            {
                operationCallResult.ValidationResult = new ValidationResult();

                operationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppProjectWorkFlowTaskEntity), "App_AppProjectWorkFlowTaskEntity_ValidationError", ValidationItemType.Error, validationMessage));

                operationCallResult.Object = false;

                return operationCallResult;
            }
            else
            {
                AppProjectWorkFlowTaskEntity updateEntity = new AppProjectWorkFlowTaskEntity();
                updateEntity.ProgressId = (int)EmAppProjectTaskProgress.Started;
                updateEntity.DateActualEnd = null;
                operationCallResult = UpdateOneTaskEntity(taskId, updateEntity);

                if (operationCallResult.IsSuccessfulWithResult)
                {

                    string messageText = string.Empty;
                    var userEntity = AppSecurityUserBL.CurrentUserEntity;
                    string subject = "Task \"" + taskDto.Name + "\" Is Set To Uncomplete";
                    messageText = "Task Is Set To Uncomplete By " + userEntity.UserName;

                    List<int> mandatoryUserIdList = new List<int>();

                    mandatoryUserIdList.Add(userEntity.UserId);

                    if (taskDto.TaskOwnerId.HasValue && !mandatoryUserIdList.Contains(taskDto.TaskOwnerId.Value))
                    {
                        mandatoryUserIdList.Add(taskDto.TaskOwnerId.Value);
                    }

                    AppMessageBL.SendNewAppMessageByScope(EmAppMessgaeScopeType.Task, null, string.Empty, (int)taskDto.Id, taskDto.ProjectId, null, EmAppMessgaePostType.SystemNotification, subject, messageText, mandatoryUserIdList);

                }

                return operationCallResult;
            }
        }

        public static string VerifyProjectTaskCompleteCondition(AppProjectWorkFlowTaskDto taskDto)
        {
            string toRetrun = "";
            if (taskDto.EmAppTaskSystemDefinedCategory.Value == (int)EmAppTaskSystemDefinedCategory.ProjectTask && taskDto.ProjectId.HasValue)
            {

                Dictionary<int, string> taskNotPassCheckList = GetOneTaskNotPassCheckList((int)taskDto.Id);

                if (taskNotPassCheckList.Count > 0)
                {
                    return "The task " + taskDto.Name.ToUpper() + " cannot be completed. Its' checklist item " + taskNotPassCheckList.First().Value + " has not been passed yet.";
                }


                AppProjectOrWorkFlowExDto projectExDto = AppProjectWorkFlowStructureBL.RetrieveOneAppProjectOrWorkFlowExDto(taskDto.ProjectId.Value, false);

                if (projectExDto != null && projectExDto.ProjectWorkflowType.HasValue)
                {
                    if (projectExDto.DictGuidKeyPredecessorList != null && projectExDto.DictGuidKeyPredecessorList.Count > 0
                       && projectExDto.AppProjectWorkFlowTaskList != null && projectExDto.AppProjectWorkFlowTaskList.Count > 0
                       && taskDto.RowIdentity.HasValue)
                    {
                        if (projectExDto.DictGuidKeyPredecessorList.ContainsKey(taskDto.RowIdentity.Value.ToString()))
                        {
                            var predecessorGuidList = projectExDto.DictGuidKeyPredecessorList[taskDto.RowIdentity.Value.ToString()];

                            foreach (string predecessorGuid in predecessorGuidList)
                            {
                                var predecessorTask = projectExDto.AppProjectWorkFlowTaskList.FirstOrDefault(o => o.RowIdentity.HasValue && o.RowIdentity.Value.ToString() == predecessorGuid);
                                if (predecessorTask != null && !predecessorTask.DateActualEnd.HasValue)
                                {
                                    //isFoundUncompletedPredecessor = true;
                                    return "The task " + taskDto.Name.ToUpper() + " cannot be completed. Its' predecessor task \"" + predecessorTask.Name + "\" has not been completed yet.";
                                }
                            }
                        }
                    }
                }
            }
            else if (taskDto.EmAppTaskSystemDefinedCategory.Value == (int)EmAppTaskSystemDefinedCategory.WorkflowTask && taskDto.ProjectId.HasValue)
            {
                return "The task " + taskDto.Name.ToUpper() + " cannot be completed. Workflow tasks can only be completed on the form.";
            }

            return toRetrun;


        }


        public static string VerifyProjectTaskSetIncompleteCondition(AppProjectWorkFlowTaskDto taskDto)
        {
            string toRetrun = "";
            if (taskDto.EmAppTaskSystemDefinedCategory.Value == (int)EmAppTaskSystemDefinedCategory.ProjectTask && taskDto.ProjectId.HasValue)
            {
                //bool isFoundStartedSuccessor = false;

                AppProjectOrWorkFlowExDto projectExDto = AppProjectWorkFlowStructureBL.RetrieveOneAppProjectOrWorkFlowExDto(taskDto.ProjectId.Value, false);

                if (projectExDto != null && projectExDto.PredecessorList != null && projectExDto.PredecessorList.Count > 0
                    && projectExDto.AppProjectWorkFlowTaskList != null && projectExDto.AppProjectWorkFlowTaskList.Count > 0
                    && taskDto.RowIdentity.HasValue)
                {
                    List<string> successorGuidList = projectExDto.PredecessorList.Where(o => o.CurrentTaskGuid.HasValue && o.PredecessorGuid.HasValue && o.PredecessorGuid.Value.ToString() == taskDto.RowIdentity.Value.ToString())
                        .Select(o => o.CurrentTaskGuid.Value.ToString()).ToList();

                    foreach (string successorGuid in successorGuidList)
                    {
                        var successorTask = projectExDto.AppProjectWorkFlowTaskList.FirstOrDefault(o => o.RowIdentity.HasValue && o.RowIdentity.Value.ToString() == successorGuid);
                        if (successorTask != null && successorTask.DateActualStart.HasValue)
                        {
                            //isFoundStartedAccessorr = true;
                            return "Cannot set this task incomplete. Its' successor task \"" + successorTask.Name + "\" has already been started.";
                        }
                    }
                }

            }

            return toRetrun;


        }



        // check List
        private static Dictionary<int, string> GetOneTaskNotPassCheckList(int projectWorkFlowTaskID)
        {
            EntityCollection<AppProjectTaskCheckListEntity> folderEntities = AppProjectTaskCheckListBL.RetrieveOneTaskCheckListEntityList(projectWorkFlowTaskID);

            var toReturn = folderEntities.Where(o => !(o.IsPass.HasValue && o.IsPass.Value)).ToDictionary(o => o.TaskCheckListId, o => o.CheckItemDesc);

            return toReturn;
        }

        // Project task or workflow task
        private static Dictionary<int, string> GetOneTaskForPredessorNotDone(int projectWorkFlowTaskID)
        {

            var process = AppProjectWorkFlowStructureBL.RetrievOneTaskPredecesor(projectWorkFlowTaskID);

            var toretrun = process.Where(o => !o.DateActualEnd.HasValue).ToDictionary(o => o.ProjectWorkFlowTaskId, o => o.Name);

            // if it is work flow need to check check box more detail

            return toretrun;
        }



        public static OperationCallResult<bool> UpdateOneTaskOwnerDeliverPhase(int taskId, int? emAppTaskOwnerDeliverPhase)
        {
            // To Do: Need to check Task uncomplete dependency. Give warning message if the task is not allowed to uncomplete.

            AppProjectWorkFlowTaskEntity updateEntity = new AppProjectWorkFlowTaskEntity();
            updateEntity.EmAppTaskOwnerDeliverPhase = emAppTaskOwnerDeliverPhase;
            OperationCallResult<bool> operationCallResult = UpdateOneTaskEntity(taskId, updateEntity);

            var taskDto = AppProjectWorkFlowStructureBL.RetrieveOneAppProjectWorkFlowTask(taskId);


            if (operationCallResult.IsSuccessfulWithResult & taskDto != null)
            {

                string messageText = string.Empty;
                var userEntity = AppSecurityUserBL.CurrentUserEntity;

                string subject = "Task \"" + taskDto.Name + "\" Delivery Phase Is Set To "
                    + ((EmAppTaskOwnerDeliverPhase)emAppTaskOwnerDeliverPhase.Value).ToString();
                messageText = "Task Delivery Phase Is Set To "
                    + ((EmAppTaskOwnerDeliverPhase)emAppTaskOwnerDeliverPhase.Value).ToString()
                    + " By " + userEntity.UserName;


                List<int> mandatoryUserIdList = new List<int>();

                mandatoryUserIdList.Add(userEntity.UserId);

                if (taskDto.TaskOwnerId.HasValue && !mandatoryUserIdList.Contains(taskDto.TaskOwnerId.Value))
                {
                    mandatoryUserIdList.Add(taskDto.TaskOwnerId.Value);
                }

                AppMessageBL.SendNewAppMessageByScope(EmAppMessgaeScopeType.Task, null, string.Empty, (int)taskDto.Id, taskDto.ProjectId, null, EmAppMessgaePostType.SystemNotification, subject, messageText, mandatoryUserIdList);



            }

            return operationCallResult;
        }








        public static OperationCallResult<bool> ReassignOneTask(AppProjectWorkFlowTaskDto aTaskExDto)
        {
            int taskId = (int)aTaskExDto.Id;


            AppProjectWorkFlowTaskEntity updateEntity = new AppProjectWorkFlowTaskEntity();
            updateEntity.TaskOwnerId = aTaskExDto.TaskOwnerId;

            OperationCallResult<bool> saveResult = UpdateOneTaskEntity(taskId, updateEntity);

            if (saveResult.IsSuccessfulWithResult)
            {


                var byUserEntity = AppSecurityUserBL.CurrentUserEntity;
                var toUserEntity = AppSecurityUserBL.RetrieveOneAppSecurityUserEntity(updateEntity.TaskOwnerId.Value);

                if (byUserEntity != null && toUserEntity != null)
                {
                    string messageText = "Task Is Reassigned To "
                               + toUserEntity.UserName
                               + " By " + byUserEntity.UserName;

                    string subject = "Task " + aTaskExDto.Name + " Is Reassigned To " + toUserEntity.UserName;

                    List<int> mandatoryUserIdList = new List<int>();

                    mandatoryUserIdList.Add(byUserEntity.UserId);
                    mandatoryUserIdList.Add(toUserEntity.UserId);

                    if (aTaskExDto.TaskOwnerId.HasValue && !mandatoryUserIdList.Contains(aTaskExDto.TaskOwnerId.Value))
                    {
                        mandatoryUserIdList.Add(aTaskExDto.TaskOwnerId.Value);
                    }

                    AppMessageBL.SendNewAppMessageByScope(EmAppMessgaeScopeType.Task, null, string.Empty, (int)aTaskExDto.Id, aTaskExDto.ProjectId, null, EmAppMessgaePostType.SystemNotification, subject, messageText, mandatoryUserIdList);

                }

            }

            return saveResult;
        }




        public static OperationCallResult<bool> DeleteOneStandAloneTask(int? taskId)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (taskId.HasValue)
            {

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");

                        string deleteProjectWorkFlow =
                            @"  
                            delete [dbo].[AppProjectTaskResource] where ProjectWorkFlowTaskID = @TaskId

                            delete [dbo].[AppMessageDeleted]  where MessageID in (select MessageID from [dbo].[AppMessage] where ProjectActivityID = @TaskId)

                            delete [dbo].[AppMessageUserReceived]  where MessageID in (select MessageID from [dbo].[AppMessage] where ProjectActivityID = @TaskId)

                            delete [dbo].[AppMessage] where ProjectActivityID = @TaskId            

                            delete [dbo].[AppProjectPerspectiveTask] where  ProjectWorkFlowTaskID = @TaskId           

                            delete [dbo].[AppProjectWorkFlowTask] where  ProjectWorkFlowTaskID = @TaskId
                        ";


                        List<SqlParameter> paramters = new List<SqlParameter>();
                        paramters.Add(new SqlParameter("@TaskId", taskId.Value));


                        adapter.ExecuteScalarQuery(deleteProjectWorkFlow, paramters);



                        adapter.Commit();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppFormEntity), "App_AppProjectWorkFlowTaskEntity_Delete_OK", ValidationItemType.Message, "Delete Successfully"));
                        aOperationCallResult.Object = true;

                    }
                    // Database FK Exeption ........
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectOrWorkFlowExDto), "plm_AppProjectWorkFlowTaskEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectOrWorkFlowExDto), "plm_AppProjectWorkFlowTaskEntity_ProjectIdRequired_Error", ValidationItemType.Error, "TaskId Required".ToString()));
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<AppProjectWorkFlowTaskExDto> SaveOneAppProjectWorkFlowTaskExDto(AppProjectWorkFlowTaskExDto aTaskExDto)
        {
            if (aTaskExDto != null)
            {
                OperationCallResult<AppProjectWorkFlowTaskExDto> aOperationCallResult = new OperationCallResult<AppProjectWorkFlowTaskExDto>();
                ValidationResult aValidationResult = new ValidationResult();
                aOperationCallResult.ValidationResult = aValidationResult;

                AppProjectCostAndProgressBL.CalculateOneLeafTask_Progress(aTaskExDto);

                bool isNewTask = false;

                AppProjectWorkFlowTaskEntity orgTaskEnity = null;

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        AppProjectWorkFlowTaskEntity taskEntity = new AppProjectWorkFlowTaskEntity();

                        List<int> deleteReousceIds = new List<int>();

                        if (aTaskExDto.IsNew)
                        {
                            isNewTask = true;

                            AppProjectWorkFlowTaskConverter.CopyDtoToEntity(taskEntity, aTaskExDto);

                            if (aTaskExDto.AppProjectTaskResourceList.IsEmpty())
                            {
                                aTaskExDto.AppProjectTaskResourceList.Add(new AppProjectTaskResourceExDto()
                                {
                                    UserId = AppSecurityUserBL.CurrentUserId,
                                    PlannedWorkHours = 0,
                                });
                            }

                            foreach (var resourceDto in aTaskExDto.AppProjectTaskResourceList)
                            {
                                AppProjectTaskResourceEntity resourceEntitty = new AppProjectTaskResourceEntity();
                                AppProjectTaskResourceConverter.CopyDtoToEntity(resourceEntitty, resourceDto);
                                taskEntity.AppProjectTaskResource.Add(resourceEntitty);
                            }

                        }
                        else
                        {
                            taskEntity = AppProjectWorkFlowStructureBL.RetrieveOneAppProjectWorkFlowTaskEntity(aTaskExDto.Id);
                            orgTaskEnity = taskEntity.DeepCopy();
                            AppProjectWorkFlowTaskConverter.CopyDtoToEntity(taskEntity, aTaskExDto);

                            if (aTaskExDto.AppProjectTaskResourceList != null)
                            {
                                Dictionary<int, AppProjectTaskResourceEntity> dictResourceIdAndEntity = taskEntity.AppProjectTaskResource.ToDictionary(o => o.TaskResourceId, o => o);
                                List<int> resourceIdDbms = dictResourceIdAndEntity.Keys.ToList();
                                List<int> resourceIdUi = aTaskExDto.AppProjectTaskResourceList.Where(o => o.Id != null).Select(o => (int)o.Id).Distinct().ToList();
                                deleteReousceIds = resourceIdDbms.Except(resourceIdUi).ToList();

                                foreach (var resourceDto in aTaskExDto.AppProjectTaskResourceList)
                                {
                                    if (resourceDto.IsNew)
                                    {
                                        AppProjectTaskResourceEntity resourceEntitty = new AppProjectTaskResourceEntity();
                                        AppProjectTaskResourceConverter.CopyDtoToEntity(resourceEntitty, resourceDto);
                                        taskEntity.AppProjectTaskResource.Add(resourceEntitty);
                                    }
                                    else
                                    {
                                        if (dictResourceIdAndEntity.ContainsKey((int)resourceDto.Id))
                                        {
                                            AppProjectTaskResourceEntity resourceEntitty = dictResourceIdAndEntity[(int)resourceDto.Id];
                                            AppProjectTaskResourceConverter.CopyDtoToEntity(resourceEntitty, resourceDto);
                                        }
                                    }
                                }
                            }
                        }

                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(taskEntity);

                        adapter.DeleteEntitiesDirectly(typeof(AppProjectTaskResourcePlannedHoursEntity), new RelationPredicateBucket(AppProjectTaskResourcePlannedHoursFields.TaskResourceId == deleteReousceIds));
                        adapter.DeleteEntitiesDirectly(typeof(AppProjectTaskResourceEntity), new RelationPredicateBucket(AppProjectTaskResourceFields.TaskResourceId == deleteReousceIds));

                        adapter.Commit();

                        aTaskExDto.Id = taskEntity.ProjectWorkFlowTaskId;
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppFormEntity), "App_AppProjectWorkFlowTaskEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }
                    // Database FK Exeption ........
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectOrWorkFlowExDto), "plm_AppProjectWorkFlowTaskEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }



                if (!aValidationResult.HasErrors)
                {
                    var updatedTaskEntity = AppProjectWorkFlowStructureBL.RetrieveOneAppProjectWorkFlowTaskEntity(aTaskExDto.Id);
                    var updateHoursResult = AppProjectWorkFlowStructureBL.UpdateOneTaskResourcePlannedHours(updatedTaskEntity, orgTaskEnity);

                    if (updateHoursResult.HasErrors)
                    {
                        aValidationResult.Merge(updateHoursResult);
                    }


                    aOperationCallResult.Object = AppProjectWorkFlowStructureBL.RetrieveOneAppProjectWorkFlowTask(aTaskExDto.Id);
                    
                    if (!aOperationCallResult.Object.AppProjectTaskResourceList.IsEmpty())
                    {
                        List<LookupItemDto> userLookupItems = AppEntityInfoBL.GetLookupItemListByCode(EmAppEntityLookupInfoCode.AppSecurityUser.ToString(), "");
                        Dictionary<int, string> dictUserIdName = userLookupItems.ToDictionary(o => (int)o.Id, o => o.Display);

                        foreach (var resourceDto in aOperationCallResult.Object.AppProjectTaskResourceList)
                        {
                            if (resourceDto.UserId.HasValue && dictUserIdName.ContainsKey(resourceDto.UserId.Value))
                            {
                                aOperationCallResult.Object.ResourceDisplay += dictUserIdName[resourceDto.UserId.Value] + ";";
                            }
                        }
                    }

                    string subject = string.Empty;
                    string messageText = string.Empty;
                    var userEntity = AppSecurityUserBL.CurrentUserEntity;


                    if (isNewTask)
                    {
                        subject = "Task " + aTaskExDto.Name + " Is Created";
                        messageText = "Task Is Created By " + userEntity.UserName;

                    }
                    else
                    {
                        subject = "Task " + aTaskExDto.Name + " Is Updated";
                        messageText = "Task Is Updated By " + userEntity.UserName;
                    }

                    List<int> mandatoryUserIdList = new List<int>();

                    mandatoryUserIdList.Add(userEntity.UserId);

                    if (aTaskExDto.TaskOwnerId.HasValue && !mandatoryUserIdList.Contains(aTaskExDto.TaskOwnerId.Value))
                    {
                        mandatoryUserIdList.Add(aTaskExDto.TaskOwnerId.Value);
                    }

                    AppMessageBL.SendNewAppMessageByScope(EmAppMessgaeScopeType.Task, null, string.Empty, (int)aTaskExDto.Id, aTaskExDto.ProjectId, null, EmAppMessgaePostType.SystemNotification, subject, messageText, mandatoryUserIdList);

                }

                return aOperationCallResult;
            }
            return null;

        }








    }
}
