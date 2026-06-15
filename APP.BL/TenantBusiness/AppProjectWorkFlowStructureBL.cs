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
using System.Data.SqlClient;
using System.Text.RegularExpressions;

using APP.Framework;
namespace App.BL
{
    public static class AppProjectWorkFlowStructureBL
    {


        // Get project or workflow Folder Hierarchy
        public static List<AppProjectOrWorkFlowDto> RetrieveAppProjectOrWorkFlows(EmAppProjectWorkflowType projectWorkflowType, bool? isPredefined = null, bool isHierarchy = true)
        {
            List<AppProjectOrWorkFlowDto> allprojectList = new List<AppProjectOrWorkFlowDto>();
            EntityCollection<AppProjectOrWorkFlowEntity> entities = new EntityCollection<AppProjectOrWorkFlowEntity>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                //RelationPredicateBucket filter = new RelationPredicateBucket(AppProjectOrWorkFlowFields.IsPredefined == false & AppProjectOrWorkFlowFields.ProjectWorkflowType == projectWorkflowType);
                RelationPredicateBucket filter = new RelationPredicateBucket(AppProjectOrWorkFlowFields.ProjectWorkflowType == projectWorkflowType);

                if (isPredefined.HasValue)
                {
                    filter.PredicateExpression.AddWithAnd(AppProjectOrWorkFlowFields.IsPredefined == isPredefined.Value);
                }

                adapter.FetchEntityCollection(entities, filter);
            }

            foreach (AppProjectOrWorkFlowEntity entity in entities)
            {
                var projectDto = AppProjectOrWorkFlowConverter.ConvertEntityToDto(entity);

                if (projectWorkflowType == EmAppProjectWorkflowType.Project)
                {
                    AppProjectSettingBL.InitializedProjectStage(projectDto);
                }

                allprojectList.Add(projectDto);
            }

            if (isHierarchy)
            {
                var rootProjectList = allprojectList.Where(f => f.ParentProjectId == null).ToList();

                foreach (var rootproject in rootProjectList)
                {
                    ProcessAppProjectOrWorkFlowChildren(allprojectList, rootproject);
                }

                rootProjectList = rootProjectList.OrderBy(o => o.Name).ToList();

                return rootProjectList;
            }
            else
            {
                return allprojectList.OrderBy(o => o.Name).ToList();
            }


        }


        public static List<AppProjectOrWorkFlowDto> RetrieveSaasApplicationWorkFlowList(int? applicationId)
        {
            List<AppProjectOrWorkFlowDto> toReturn = new List<AppProjectOrWorkFlowDto>();

            if (applicationId.HasValue)
            {
                List<AppProjectOrWorkFlowDto> allWorkFlowList = RetrieveAppProjectOrWorkFlows(EmAppProjectWorkflowType.BusinessProcessWorkflow, true, false);
                

                var transactionList = AppTransactionBL.RetrieveSaasApplicationTransactionList(applicationId);

                Dictionary<int, AppTransactionDto> dictWorkFlowIdAndTransDto = new Dictionary<int, AppTransactionDto>();
                transactionList.Where(o => o.MasterWorkflowId.HasValue).ForAll(o =>
                {
                    if (!dictWorkFlowIdAndTransDto.ContainsKey(o.MasterWorkflowId.Value))
                    {
                        dictWorkFlowIdAndTransDto.Add(o.MasterWorkflowId.Value, o);
                    }                    
                });
                
                toReturn = allWorkFlowList.Where(o => dictWorkFlowIdAndTransDto.ContainsKey((int)o.Id)).ToList();

                toReturn.ForAll(o => o.ForeignTransactionDto = dictWorkFlowIdAndTransDto[(int)o.Id]);
            }

            return toReturn;
        }

        public static OperationCallResult<AppProjectOrWorkFlowExDto> CreateDefaultWorkflowFromTransaction(int transactionId)
        {
            AppTransactionEntity transactionEntity = AppTransactionBL.RetrieveOneAppTransactionSimpleEntity(transactionId);

            if (transactionEntity != null)
            {
                AppProjectOrWorkFlowExDto workflowDto = new AppProjectOrWorkFlowExDto();
                workflowDto.ProjectWorkflowType = (int)EmAppProjectWorkflowType.BusinessProcessWorkflow;
                workflowDto.Name = transactionEntity.TransactionName;
                workflowDto.IsPredefined = true;
                workflowDto.IsActive = true;

                AppProjectWorkFlowTaskExDto taskDto = new AppProjectWorkFlowTaskExDto();
                taskDto.Name = "Stage 1";
                taskDto.DatePlannedStart = ClientTimeZoneHelper.ConvertClientToUTCDateTime(ClientTimeZoneHelper.ConvertUTCToClientDateTime(DateTime.UtcNow).Date);
                taskDto.DatePlannedEnd = ClientTimeZoneHelper.ConvertClientToUTCDateTime(ClientTimeZoneHelper.ConvertUTCToClientDateTime(DateTime.UtcNow).Date);
                taskDto.Sort = 1;
                taskDto.ProjectActivityStatusId = 5;
                taskDto.RowIdentity = Guid.NewGuid();
                taskDto.DiagramShapeType = 1;
                taskDto.StageStatusFlag = 3;
                taskDto.StageUilayout = "left: 45px; top: 50px; width: 150px; height: 50px; right: auto; bottom: auto;";
                taskDto.ProgressId = 1;
                taskDto.TransactionId = transactionId;

                workflowDto.AppProjectWorkFlowTaskList.Add(taskDto);
                workflowDto.RootTreeList = workflowDto.AppProjectWorkFlowTaskList.ToArray();

                var saveWorkflowResult = SaveOnePhysicalProjectOrWorkFlowExDto(workflowDto);

                if (saveWorkflowResult.IsSuccessfulWithResult)
                {
                    transactionEntity.MasterWorkflowId = (int)saveWorkflowResult.Object.Id;

                    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                    {
                        adapter.UpdateEntitiesDirectly(transactionEntity, new RelationPredicateBucket(AppTransactionFields.TransactionId == transactionId));
                        AppCacheManagerBL.RefreshOnetHierarchyTranscation(transactionId);
                    }

                    return saveWorkflowResult;
                }

            }

            return null;
        }

        public static OperationCallResult<AppProjectOrWorkFlowExDto> ExtractMainTaskToChildPorject(AppProjectOrWorkFlowExDto mainProject)
        {
            if (!mainProject.ExtractMainTaskId.HasValue)
            {
                return null;
            }

            AppProjectOrWorkFlowExDto childProject = CreateChildProject(mainProject);
            int? childProjectId = childProject.Id as int?;

            return RemovePorjectMainTaskChildTask(mainProject, childProjectId);
        }

        public static AppProjectOrWorkFlowExDto RetrieveOneAppProjectOrWorkFlowExDto(object proejctId, bool isRunningTime = false, bool isConvertUTCToClient = true)
        {
            AppProjectOrWorkFlowEntity appProjectOrWorkFlowEntity = RetrieveOneAppProjectOrWorkFlowEntity(proejctId);

            appProjectOrWorkFlowEntity.IsConvertDBUtcToClient = isConvertUTCToClient;

            appProjectOrWorkFlowEntity.AppProjectWorkFlowTask.ForAll(o => o.IsConvertDBUtcToClient = isConvertUTCToClient);

            AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto = ConvertAppProjectOrWorkFlowEntity_AppProjectOrWorkFlowExDto(appProjectOrWorkFlowEntity);



            InitializeActivityPredecessorsAndSuccessors(aAppProjectOrWorkFlowExDto);
            SetupProjectOrWorkFlowExDtoMainSubTaskAndProdecessor(aAppProjectOrWorkFlowExDto);

            InitializeOneProjectAllTaskStageAndStatus(aAppProjectOrWorkFlowExDto, isConvertUTCToClient);

            if (!isRunningTime)
            {
                SetupFormulaExpress(aAppProjectOrWorkFlowExDto);

            }
            aAppProjectOrWorkFlowExDto.IsModified = false;





            if (aAppProjectOrWorkFlowExDto.ProjectWorkflowType.HasValue && aAppProjectOrWorkFlowExDto.ProjectWorkflowType.Value == (int)EmAppProjectWorkflowType.BusinessProcessWorkflow)
            {
                aAppProjectOrWorkFlowExDto.WorkflowTransactionLookUpList = RetrieveOneMasterWorkflowTransactionLookUpList(aAppProjectOrWorkFlowExDto.Id);
            }

            return aAppProjectOrWorkFlowExDto;
        }




        public static List<AppProjectTaskResourceDto> RetrieveWorkFlowTaskResources(object taskId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppProjectTaskResourceEntity> list = new EntityCollection<AppProjectTaskResourceEntity>();

                IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppProjectTaskResourceFields.ProjectWorkFlowTaskId == taskId);
                adapter.FetchEntityCollection(list, aFilter);

                var aDtoList = new List<AppProjectTaskResourceDto>();
                foreach (var o in list)
                {
                    AppProjectTaskResourceDto aDto = AppProjectTaskResourceConverter.ConvertEntityToDto(o);
                    aDtoList.Add(aDto);

                }

                return aDtoList;
            }
        }

        public static List<AppProjectWorkFlowTaskEntity> RetrievOneTaskPredecesor(object taskId)
        {

            List<AppProjectWorkFlowTaskEntity> toRetrun = new List<AppProjectWorkFlowTaskEntity>();
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppProjectTaskPredecessorEntity> list = new EntityCollection<AppProjectTaskPredecessorEntity>();

                IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppProjectTaskPredecessorFields.ProjectWorkFlowTaskId == taskId);

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppProjectTaskPredecessorEntity);

                rootPath.Add(AppProjectTaskPredecessorEntity.PrefetchPathAppProjectWorkFlowTask);

                adapter.FetchEntityCollection(list, aFilter, rootPath);



                foreach (var o in list)
                {
                    toRetrun.Add(o.AppProjectWorkFlowTask);

                }

                return toRetrun;
            }
        }
        public static List<AppSecurityUserDto> RetrieveOneWorkFlowAllTaskResourceUsers(object workflowId)
        {
            List<AppSecurityUserDto> userList = new List<AppSecurityUserDto>();

            List<int> userIdList = new List<int>();
            var dictAllUserDto = AppSecurityUserBL.DictAllUserDto;

            AppProjectOrWorkFlowEntity workFlowEntity = RetrieveOneAppProjectOrWorkFlowEntity(workflowId);
            foreach (var taskEntity in workFlowEntity.AppProjectWorkFlowTask)
            {
                foreach (var resourceEntity in taskEntity.AppProjectTaskResource)
                {
                    if (resourceEntity.UserId.HasValue && !userIdList.Contains(resourceEntity.UserId.Value))
                    {
                        userIdList.Add(resourceEntity.UserId.Value);
                    }
                }
            }



            foreach (int userid in userIdList)
            {
                if (dictAllUserDto.ContainsKey(userid))
                {
                    userList.Add(dictAllUserDto[userid]);
                }
            }

            return userList;

        }

        public static List<AppProjectTeamMemberDto> RetriveOneProjectAllTeamMembers(object projectId)
        {
            List<AppProjectTeamMemberDto> toReturn = new List<AppProjectTeamMemberDto>();

            if (projectId != null)
            {
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    EntityCollection<AppProjectTeamMemberEntity> list = new EntityCollection<AppProjectTeamMemberEntity>();
                    IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppProjectTeamMemberEntity);

                    // need to add Permission entity code the the entity management
                    RelationPredicateBucket filter = new RelationPredicateBucket(AppProjectTeamMemberFields.ProjectId == projectId);

                    adapter.FetchEntityCollection(list, filter, rootPath);

                    List<LookupItemDto> users = AppEntityInfoBL.GetLookupItemListByCode(EmAppEntityLookupInfoCode.AppSecurityUser.ToString(), "");
                    Dictionary<int, string> dictUserIdAndDisplay = users.ToDictionary(o => (int)o.Id, o => o.Display);

                    foreach (var o in list)
                    {
                        AppProjectTeamMemberDto aDto = AppProjectTeamMemberConverter.ConvertEntityToDto(o);

                        if (aDto.UserId.HasValue && dictUserIdAndDisplay.ContainsKey(aDto.UserId.Value))
                        {
                            aDto.UserName = dictUserIdAndDisplay[aDto.UserId.Value];
                        }

                        toReturn.Add(aDto);
                    }
                }
            }

            return toReturn;
        }

        private static OperationCallResult<object> SendProjectTaskStartedMessage(AppProjectWorkFlowTaskExDto taskDto)
        {
            string subject = "Project Task \"" + taskDto.Name + "\" Started";
            string messageText = "Project Task Started";
            return AppMessageBL.SendNewAppMessageByScope(EmAppMessgaeScopeType.Task, null, string.Empty, (int)taskDto.Id, taskDto.ProjectId, null, EmAppMessgaePostType.SystemNotification, subject, messageText, null);
        }

        public static OperationCallResult<AppProjectOrWorkFlowExDto> SaveProjectOrWorkFlowExDto(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
        {



            //Root porject
            if (!aAppProjectOrWorkFlowExDto.ParentProjectId.HasValue)
            {
                OperationCallResult<AppProjectOrWorkFlowExDto> returnResult = SaveOneProjectAndChildProjects(aAppProjectOrWorkFlowExDto);

                return returnResult;
            }
            else // it is child proejct, need to get all MainPorject to do Caculation 
            {
                if (aAppProjectOrWorkFlowExDto.IsChildProjectAllowChildBubbleUpParent.HasValue && aAppProjectOrWorkFlowExDto.IsChildProjectAllowChildBubbleUpParent.Value)
                {
                    List<AppProjectOrWorkFlowExDto> needToSaveProjectColelction_UpdateDates = new List<AppProjectOrWorkFlowExDto>();
                    PrepareDateChangedParentProjectList(aAppProjectOrWorkFlowExDto, needToSaveProjectColelction_UpdateDates);

                    foreach (var parentProejct in needToSaveProjectColelction_UpdateDates)
                    {
                        SaveOneProjectAndChildProjects(parentProejct);
                    }

                    List<AppProjectOrWorkFlowExDto> needToSaveProjectColelction_UpdateCost = new List<AppProjectOrWorkFlowExDto>();
                    PrepareCostChangedParentProjectList(aAppProjectOrWorkFlowExDto, needToSaveProjectColelction_UpdateCost);

                    foreach (var parentProejct in needToSaveProjectColelction_UpdateCost)
                    {
                        SaveOneProjectAndChildProjects(parentProejct);
                    }
                }

                if (aAppProjectOrWorkFlowExDto.IsChildProjectAllowParentTtrickleDown.HasValue && aAppProjectOrWorkFlowExDto.IsChildProjectAllowParentTtrickleDown.Value)
                {
                    return SaveOneProjectAndChildProjects(aAppProjectOrWorkFlowExDto);
                }
                else
                {

                    AppProjectDateCaculationBL.DoOnePojectDateCaculation(aAppProjectOrWorkFlowExDto, null);
                    AppProjectCostAndProgressBL.CaculationOneProjectCostAndProgress(aAppProjectOrWorkFlowExDto);

                    return SaveOnePhysicalProjectOrWorkFlowExDto(aAppProjectOrWorkFlowExDto);
                }
            }
        }

        private static void PrepareDateChangedParentProjectList(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, List<AppProjectOrWorkFlowExDto> parentPorjectColelction)
        {

            AppProjectDateCaculationBL.DoOnePojectDateCaculation(aAppProjectOrWorkFlowExDto, null);


            int? parentProjectId = aAppProjectOrWorkFlowExDto.ParentProjectId;
            int? parentMainTaskId = aAppProjectOrWorkFlowExDto.ProjectSumaryTaskId;

            if (parentProjectId.HasValue && parentMainTaskId.HasValue)
            {
                AppProjectOrWorkFlowExDto parentProjectExDto = RetrieveOneAppProjectOrWorkFlowExDto(parentProjectId.Value, false, false);

                parentProjectExDto.AppProjectWorkFlowTaskList = AppProjectWorkFlowStructureBL.GetFlatTaskExDtoList(parentProjectExDto);

                var mainTaskExDto = parentProjectExDto.AppProjectWorkFlowTaskList.FirstOrDefault(o => (int)o.Id == parentMainTaskId.Value);

                if (mainTaskExDto != null)
                {
                    bool isChangedDate = !(mainTaskExDto.DatePlannedStart == aAppProjectOrWorkFlowExDto.DatePlannedStart && mainTaskExDto.DatePlannedEnd == aAppProjectOrWorkFlowExDto.DatePlannedEnd);


                    if (isChangedDate)
                    {
                        InitializeMainTaskDatesFromChildProject(aAppProjectOrWorkFlowExDto, mainTaskExDto);

                        parentProjectExDto.IsModified = true;
                        parentProjectExDto.ExcludeCalculationTaskId = mainTaskExDto.Id as int?;
                        parentPorjectColelction.Add(parentProjectExDto);

                        PrepareDateChangedParentProjectList(parentProjectExDto, parentPorjectColelction);
                    }
                }
            }
        }


        private static void PrepareCostChangedParentProjectList(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, List<AppProjectOrWorkFlowExDto> parentPorjectColelction)
        {
            AppProjectCostAndProgressBL.CaculationOneProjectCostAndProgress(aAppProjectOrWorkFlowExDto);

            int? parentProjectId = aAppProjectOrWorkFlowExDto.ParentProjectId;
            int? parentMainTaskId = aAppProjectOrWorkFlowExDto.ProjectSumaryTaskId;

            if (parentProjectId.HasValue && parentMainTaskId.HasValue)
            {
                AppProjectOrWorkFlowExDto parentProjectExDto = RetrieveOneAppProjectOrWorkFlowExDto(parentProjectId.Value, false, false);

                parentProjectExDto.AppProjectWorkFlowTaskList = AppProjectWorkFlowStructureBL.GetFlatTaskExDtoList(parentProjectExDto);

                var mainTaskExDto = parentProjectExDto.AppProjectWorkFlowTaskList.FirstOrDefault(o => (int)o.Id == parentMainTaskId.Value);

                if (mainTaskExDto != null)
                {
                    //mainTaskExDto.TaskPlannedCost = aAppProjectOrWorkFlowExDto.ProjectPlannedCost;
                    mainTaskExDto.PlannedWorkHours = aAppProjectOrWorkFlowExDto.PlannedWorkHours;
                    mainTaskExDto.PlannedResourceCost = aAppProjectOrWorkFlowExDto.PlannedResourceCost;

                    mainTaskExDto.ActualWorkHours = aAppProjectOrWorkFlowExDto.ActualWorkHours;
                    mainTaskExDto.ActualResourceCost = aAppProjectOrWorkFlowExDto.ActualResourceCost;


                    mainTaskExDto.IsModified = true;

                    parentProjectExDto.IsModified = true;
                    parentProjectExDto.ExcludeCalculationTaskId = mainTaskExDto.Id as int?;
                    parentPorjectColelction.Add(parentProjectExDto);

                    PrepareCostChangedParentProjectList(parentProjectExDto, parentPorjectColelction);
                }
            }
        }


        private static OperationCallResult<AppProjectOrWorkFlowExDto> SaveOneProjectAndChildProjects(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
        {
            List<AppProjectOrWorkFlowExDto> childPorjectColelction_UpdateDates = new List<AppProjectOrWorkFlowExDto>();
            AppProjectDateCaculationBL.DoOnePojectDateCaculation(aAppProjectOrWorkFlowExDto, childPorjectColelction_UpdateDates);

            List<AppProjectOrWorkFlowExDto> childPorjectColelction_UpdateCost = new List<AppProjectOrWorkFlowExDto>();
            AppProjectCostAndProgressBL.CaculationOneProjectCostAndProgress(aAppProjectOrWorkFlowExDto, childPorjectColelction_UpdateCost);

            var returnResult = SaveOnePhysicalProjectOrWorkFlowExDto(aAppProjectOrWorkFlowExDto);

            foreach (var childProejct in childPorjectColelction_UpdateDates)
            {
                SaveOnePhysicalProjectOrWorkFlowExDto(childProejct);
            }


            // Don't need to update child project cost. Parent Cost change will not cascading child cost change.


            return returnResult;
        }

        private static void InitializeMainTaskDatesFromChildProject(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, AppProjectWorkFlowTaskExDto mainTaskExDto)
        {
            //mainTaskExDto.DatePlannedStart = aAppProjectOrWorkFlowExDto.DatePlannedStart;
            mainTaskExDto.DatePlannedEnd = aAppProjectOrWorkFlowExDto.DatePlannedEnd;
            mainTaskExDto.DateActualStart = aAppProjectOrWorkFlowExDto.DateActualStart;
            mainTaskExDto.DateActualEnd = aAppProjectOrWorkFlowExDto.DateActualEnd;

            //AppCalendarExDto userCalendar = null;
            //AppCalendarExDto companyCalendar = null;
            //AppProjectTaskDateCaculationBL.GetTaskUserCalendar(mainTaskExDto, aAppProjectOrWorkFlowExDto, out userCalendar, out companyCalendar);

            //DateTime? calStartDate = AppProjectTaskDateCaculationBL.ConvertSingleDateByTimeZoneOffset_ConvertUTCToLocal(aAppProjectOrWorkFlowExDto.DatePlannedStart, aAppProjectOrWorkFlowExDto.TimeZoneOffset);
            //DateTime? calEndDate = AppProjectTaskDateCaculationBL.ConvertSingleDateByTimeZoneOffset_ConvertUTCToLocal(aAppProjectOrWorkFlowExDto.DatePlannedEnd, aAppProjectOrWorkFlowExDto.TimeZoneOffset);
            //calEndDate = AppProjectTaskDateCaculationBL.MidnightAddOneSecondBackFix(calEndDate, null, null);

            //double durationDays = AppProjectTaskDateCaculationBL.CalculateCalendarDurationDays(calStartDate, calEndDate, userCalendar, companyCalendar);

            double sum_childTaskWorkingDays = aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList.Where(o => o.AmountOfTime.HasValue).Sum(o => o.AmountOfTime.Value);
            double sum_childTaskLeadingDays = aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList.Where(o => o.TimingDays.HasValue).Sum(o => o.TimingDays.Value);

            mainTaskExDto.AmountOfTime = sum_childTaskWorkingDays + sum_childTaskLeadingDays;

            mainTaskExDto.IsModified = true;



        }

        private static OperationCallResult<AppProjectOrWorkFlowExDto> SaveOnePhysicalProjectOrWorkFlowExDto(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
        {
            OperationCallResult<AppProjectOrWorkFlowExDto> aOperationCallResult;

            if (aAppProjectOrWorkFlowExDto.DeletedItemsIds == null)
            {
                aAppProjectOrWorkFlowExDto.DeletedItemsIds = new List<object>();
            }

            var deletedTaskIds = aAppProjectOrWorkFlowExDto.DeletedItemsIds;

            //CalculateOneProjectAllTaskCompletePercentage(aAppProjectOrWorkFlowExDto);

            if (aAppProjectOrWorkFlowExDto.ProjectWorkflowType.Value == (int)EmAppProjectWorkflowType.Project)
            {

            }
            else
            {
                ObservableSet<AppProjectWorkFlowTaskExDto> projectWorkFlowTaskList = GetFlatTaskExDtoList(aAppProjectOrWorkFlowExDto);
                aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList = projectWorkFlowTaskList;
            }

            aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList.DeletedItemIds = deletedTaskIds;
            aOperationCallResult = new OperationCallResult<AppProjectOrWorkFlowExDto>();

            List<AppTransactionExDto> transactionList = RetrieveOneMasterWorkflowTransactionList(aAppProjectOrWorkFlowExDto.Id);

            if (transactionList.Count > 0)
            {
                List<AppTransactionFieldExDto> allTransactionFieldList = new List<AppTransactionFieldExDto>();

                foreach (AppTransactionExDto transactionExDto in transactionList)
                {
                    List<AppTransactionFieldExDto> transactionFieldList = transactionExDto.DictRootLevelUnitTransactionField.Values.ToList();
                    AppPorjectWorkFlowBL.InitialTransactionFieldFormularDisplayName(transactionExDto, transactionFieldList);
                    allTransactionFieldList.AddRange(transactionFieldList);
                }

                AppPorjectWorkFlowBL.InFormatWorkflowExDtoConditionAndActionFormularExpression(aAppProjectOrWorkFlowExDto, allTransactionFieldList);
            }

            if (aAppProjectOrWorkFlowExDto.IsNew)
            {
                return SaveNewProjectOrWorkFlowExDto(aAppProjectOrWorkFlowExDto, aOperationCallResult);
            }
            else
            {
                OperationCallResult<AppProjectOrWorkFlowExDto> toReturn = UpdateProjectOrWorkFlowExDto(aAppProjectOrWorkFlowExDto, aOperationCallResult, true);

                return toReturn;
            }
        }


        public static List<AppProjectOrWorkFlowDto> GetTransactionFormProjectOrWorkflowDtoList(List<int> transactionIds, object rootPrimaryKeyValue, EmAppProjectWorkflowType emAppProjectWorkflowType)
        {
            List<AppProjectOrWorkFlowDto> aDtoList = new List<AppProjectOrWorkFlowDto>();

            EntityCollection<AppProjectOrWorkFlowEntity> projectEntltyList = new EntityCollection<AppProjectOrWorkFlowEntity>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                IncludeFieldsList includeFieldsList = new IncludeFieldsList();
                includeFieldsList.Add(AppProjectOrWorkFlowFields.TransactionId);
                includeFieldsList.Add(AppProjectOrWorkFlowFields.ProjectId);
                includeFieldsList.Add(AppProjectOrWorkFlowFields.Name);
                includeFieldsList.Add(AppProjectOrWorkFlowFields.Description);
                includeFieldsList.Add(AppProjectOrWorkFlowFields.ProjectWorkflowType);

                var predict = new RelationPredicateBucket(AppProjectOrWorkFlowFields.TransactionId == transactionIds.ToArray()
                    & AppProjectOrWorkFlowFields.TransactionRid == rootPrimaryKeyValue
                    & AppProjectOrWorkFlowFields.ProjectWorkflowType == (int)emAppProjectWorkflowType);
                adapter.FetchEntityCollection(projectEntltyList, includeFieldsList, predict);


                foreach (var entity in projectEntltyList)
                {
                    var dto = AppProjectOrWorkFlowConverter.ConvertEntityToDto(entity);
                    aDtoList.Add(dto);
                }

            }

            return aDtoList;
        }


        private static void ProcessAppProjectOrWorkFlowChildren(IEnumerable<AppProjectOrWorkFlowDto> allprojectList, AppProjectOrWorkFlowDto projectOrWorkFlowDto)
        {
            var children = GetAppProjectOrWorkFlowChildren(allprojectList, projectOrWorkFlowDto).OrderBy(f => f.Name).ToList();

            if (!children.IsEmpty())
            {
                projectOrWorkFlowDto.Children = children;
                projectOrWorkFlowDto.Children.ForAll(c => ProcessAppProjectOrWorkFlowChildren(allprojectList, c));

            }
        }

        private static IEnumerable<AppProjectOrWorkFlowDto> GetAppProjectOrWorkFlowChildren(IEnumerable<AppProjectOrWorkFlowDto> allprojectList, AppProjectOrWorkFlowDto folderTreeItemDto)
        {
            return allprojectList.Where(f => f.ParentProjectId.HasValue && f.ParentProjectId.Value == (int)folderTreeItemDto.Id).ToList();
        }

        private static void SetupFormulaExpress(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
        {
            List<AppTransactionExDto> transactionList = RetrieveOneMasterWorkflowTransactionList(aAppProjectOrWorkFlowExDto.Id);

            if (transactionList.Count > 0)
            {
                List<AppTransactionFieldExDto> allTransactionFieldList = new List<AppTransactionFieldExDto>();

                foreach (AppTransactionExDto transactionExDto in transactionList)
                {
                    List<AppTransactionFieldExDto> transactionFieldList = transactionExDto.DictRootLevelUnitTransactionField.Values.ToList();
                    AppPorjectWorkFlowBL.InitialTransactionFieldFormularDisplayName(transactionExDto, transactionFieldList);
                    allTransactionFieldList.AddRange(transactionFieldList);
                }

                AppPorjectWorkFlowBL.OutFormatWorkflowEntityConditionAndActionFormularExpression(aAppProjectOrWorkFlowExDto, allTransactionFieldList);

            }
        }



        private static AppProjectOrWorkFlowExDto SetupProjectOrWorkFlowExDtoMainSubTaskAndProdecessor(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
        {


            List<AppProjectWorkFlowTaskExDto> allTaskDtoList = aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList.OrderBy(o => o.Sort).ToList();
            var rootDtoList = allTaskDtoList.Where(o => !o.MainTaskId.HasValue).ToArray();

            foreach (var rootFolder in rootDtoList)
            {
                ProcessChilds(allTaskDtoList, rootFolder);
            }
            var dictGuidKeyPredecessorList = new Dictionary<string, List<string>>();

            foreach (var taskDto in aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList)
            {
                if (!taskDto.AppProjectTaskPredecessorList.IsEmpty())
                {
                    dictGuidKeyPredecessorList.Add(taskDto.RowIdentity.ToString(), taskDto.AppProjectTaskPredecessorList.Select(o => o.PredecessorGuid.ToString()).ToList());

                }

            }

            // need to remove  aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList from WebApi


            aAppProjectOrWorkFlowExDto.DictGuidKeyPredecessorList = dictGuidKeyPredecessorList;
            aAppProjectOrWorkFlowExDto.RootTreeList = rootDtoList;
            return aAppProjectOrWorkFlowExDto;
        }



        private static OperationCallResult<AppProjectOrWorkFlowExDto> UpdateProjectOrWorkFlowExDto(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto,
         OperationCallResult<AppProjectOrWorkFlowExDto> aOperationCallResult,
          bool needToRetrievePorjectExdto = true)
        {

            ValidationResult aValidationResult = aOperationCallResult.ValidationResult;

            // clear the delete taksId
            //  DeleteflowTask(aAppProjectOrWorkFlowExDto, aValidationResult);

            int? projectId = aAppProjectOrWorkFlowExDto.Id as int?;
            //if (aAppProjectOrWorkFlowExDto.IsModified)
            //{

            ObservableSet<AppProjectWorkFlowTaskExDto> projectTaskDtoList = GetFlatTaskExDtoList(aAppProjectOrWorkFlowExDto);

            AppProjectOrWorkFlowEntity projectEntity = RetrieveOneAppProjectOrWorkFlowEntity(projectId);
            var orgProjectEntity = projectEntity.DeepCopy();

            AppProjectOrWorkFlowConverter.CopyDtoToEntity(projectEntity, aAppProjectOrWorkFlowExDto);


            List<object> deleteAppProjectWorkFlowTaskIDs = aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList.FindDeletedItemIds().ToList();




            List<int> deletdProjectPredecessorIDs = new List<int>();
            //List<int> deletdProjectSubActivityIDs = new List<int>();
            List<int> deletdProjectActivityResourceIDs = new List<int>();

            List<int> deletdProjectConditionIDs = new List<int>();
            List<int> deletdProjectActionsIDs = new List<int>();

            ProcessFlowTaskList(projectTaskDtoList, projectEntity, deletdProjectPredecessorIDs, deletdProjectActivityResourceIDs);

            ProcessProjectConditionList(deletdProjectConditionIDs, deletdProjectActionsIDs, aAppProjectOrWorkFlowExDto, projectEntity);

            Dictionary<Guid, int> dictExternalGuidTaskId = new Dictionary<Guid, int>();
            foreach (var taskDto in projectTaskDtoList)
            {
                if (taskDto.IsExternalChildSumaryTask)
                {
                    dictExternalGuidTaskId.Add(taskDto.RowIdentity.Value, (int)taskDto.Id);
                }
            }

            ProcessProjectEntityPredesessor(projectEntity, dictExternalGuidTaskId);
            ProcessProjectEntityNextActionList(projectEntity);






            // DeleteIDS
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(projectEntity);

                    //List<int> deletdProjectConditionIDs = new List<int>();
                    //	List<int> deletdProjectActionsIDs = new List<int>();/


                    adapter.DeleteEntitiesDirectly(typeof(AppProjectWorkFlowActionEntity), new RelationPredicateBucket(AppProjectWorkFlowActionFields.WorkFlowActionId == deletdProjectActionsIDs));

                    // need to delete action for all delete condition ids
                    adapter.DeleteEntitiesDirectly(typeof(AppProjectWorkFlowActionEntity), new RelationPredicateBucket(AppProjectWorkFlowActionFields.WorkFlowConditionId == deletdProjectConditionIDs));

                    adapter.DeleteEntitiesDirectly(typeof(AppProjectWorkFlowConditionEntity), new RelationPredicateBucket(AppProjectWorkFlowConditionFields.WorkFlowConditionId == deletdProjectConditionIDs));

                    adapter.DeleteEntitiesDirectly(typeof(AppProjectTaskPredecessorEntity), new RelationPredicateBucket(AppProjectTaskPredecessorFields.ProjectActivityPredecessorId == deletdProjectPredecessorIDs));

                    adapter.DeleteEntitiesDirectly(typeof(AppProjectTaskResourceEntity), new RelationPredicateBucket(AppProjectTaskResourceFields.TaskResourceId == deletdProjectActivityResourceIDs));


                    if (deleteAppProjectWorkFlowTaskIDs.Count > 0)
                    {
                        DeleteFlowTaskWithDepedency(deleteAppProjectWorkFlowTaskIDs, adapter);

                    }



                    UpdateTaskMainTaskId(projectTaskDtoList, projectEntity.ProjectId, adapter);

                    adapter.Commit();
                    projectId = projectEntity.ProjectId;





                    if (!aValidationResult.HasErrors)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppFormEntity), "App_AppProjectOrWorkFlowEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }
                }

                // Database FK Exeption ........
                catch (Exception ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectOrWorkFlowExDto), "plm_AppProjectOrWorkFlowEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            AppProjectOrWorkFlowEntity updatedProjectEntity = RetrieveOneAppProjectOrWorkFlowEntity(projectId);
            var updateHoursResult = UpdateTaskResourcePlannedHours(aAppProjectOrWorkFlowExDto, orgProjectEntity, updatedProjectEntity);

            if (updateHoursResult.HasErrors)
            {
                aValidationResult.Merge(updateHoursResult);
            }


            if (needToRetrievePorjectExdto)
            {
                aOperationCallResult.Object = RetrieveOneAppProjectOrWorkFlowExDto(projectId);
            }





            return aOperationCallResult;
        }


        public static OperationCallResult<object> CreateProjectDefaultForm(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
        {
            OperationCallResult<object> operationCallResult = new OperationCallResult<object>();
            operationCallResult.ValidationResult = new ValidationResult();

            if (aAppProjectOrWorkFlowExDto != null && aAppProjectOrWorkFlowExDto.TransactionId.HasValue)
            {
                int transactionId = aAppProjectOrWorkFlowExDto.TransactionId.Value;

                AppMasterDetailDto aAppformDataDto = AppMasterDetailFormDataLoadBL.GetNewFormData(transactionId);

                if (aAppformDataDto != null)
                {
                    var saveFormResult = AppMasterDetailFormDataSaveBL.SaveTransactionData(aAppformDataDto);
                    operationCallResult.ValidationResult.Merge(saveFormResult.ValidationResult);

                    if (saveFormResult.IsSuccessfulWithResult && saveFormResult.Object != null && saveFormResult.Object.RootPrimaryKeyValue != null)
                    {
                        aAppProjectOrWorkFlowExDto.TransactionRid = saveFormResult.Object.RootPrimaryKeyValue.ToString();
                        var saveProjectResult = AppProjectSettingBL.SaveProjectSettingExDto(aAppProjectOrWorkFlowExDto);
                        operationCallResult.ValidationResult.Merge(saveProjectResult.ValidationResult);

                        if (saveProjectResult.IsSuccessfulWithResult)
                        {
                            operationCallResult.Object = saveProjectResult.Object.TransactionRid;
                        }
                    }
                }
            }
            return operationCallResult;
        }

        public static OperationCallResult<object> CreateProjectTaskDefaultForm(AppProjectWorkFlowTaskExDto aTaskExDto)
        {
            OperationCallResult<object> operationCallResult = new OperationCallResult<object>();
            operationCallResult.ValidationResult = new ValidationResult();

            if (aTaskExDto != null && aTaskExDto.Id != null && aTaskExDto.TransactionId.HasValue && aTaskExDto.Id != null)
            {
                int transactionId = aTaskExDto.TransactionId.Value;
                int taskId = (int)aTaskExDto.Id;

                AppMasterDetailDto aAppformDataDto = AppMasterDetailFormDataLoadBL.GetNewFormData(transactionId);

                if (aAppformDataDto != null)
                {
                    var saveFormResult = AppMasterDetailFormDataSaveBL.SaveTransactionData(aAppformDataDto);
                    operationCallResult.ValidationResult.Merge(saveFormResult.ValidationResult);

                    if (saveFormResult.IsSuccessfulWithResult && saveFormResult.Object != null && saveFormResult.Object.RootPrimaryKeyValue != null)
                    {
                        aTaskExDto.TransactionRid = saveFormResult.Object.RootPrimaryKeyValue.ToString();

                        using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                        {
                            try
                            {
                                AppProjectWorkFlowTaskEntity updateEntity = new AppProjectWorkFlowTaskEntity();
                                updateEntity.TransactionRid = aTaskExDto.TransactionRid;
                                adapter.UpdateEntitiesDirectly(updateEntity, new RelationPredicateBucket(AppProjectWorkFlowTaskFields.ProjectWorkFlowTaskId == taskId));
                                adapter.Commit();
                                operationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppProjectWorkFlowTaskEntity), "App_AppProjectWorkFlowTaskEntity_Update_OK", ValidationItemType.Message, "Task Update Successfully"));

                                operationCallResult.Object = updateEntity.TransactionRid;
                            }
                            catch (ORMQueryExecutionException ex)
                            {
                                adapter.Rollback();
                                operationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppProjectWorkFlowTaskEntity), "App_AppProjectWorkFlowTaskEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                            }
                        }

                    }
                }
            }
            return operationCallResult;
        }


        private static void ProcessFlowTaskList(ObservableSet<AppProjectWorkFlowTaskExDto> projectWorkFlowTaskList, AppProjectOrWorkFlowEntity projectEntity,
            List<int> deletdProjectPredecessorIDs, List<int> deletdProjectActivityResourceIDs)
        {

            var dictTaskEntityFromDB = projectEntity.AppProjectWorkFlowTask.ToDictionary(o => o.ProjectWorkFlowTaskId, o => o);

            //double projectPlannedWorkHours = 0;
            //double projectPlannedResourceCost = 0;


            //List<AppProjectTeamMemberDto> teamMemberList = AppProjectWorkFlowStructureBL.RetriveOneProjectAllTeamMembers(projectEntity.ProjectId);
            //Dictionary<int, double?> dictUsrIdAndRate = teamMemberList.Where(o => o.UserId.HasValue).ToDictionary(o => o.UserId.Value, o => o.PersonalRate);


            foreach (var taskDto in projectWorkFlowTaskList)
            {
                //CalculateOneTaskPlanedResourceHourAndCost(dictUsrIdAndRate, taskDto);

                //double taskPlannedResourceHour = taskDto.PlannedWorkHours.HasValue ? taskDto.PlannedWorkHours.Value : 0;
                //double taskPlannedResourceCost = taskDto.PlannedResourceCost.HasValue ? taskDto.PlannedResourceCost.Value : 0;

                //projectPlannedWorkHours += taskPlannedResourceHour;
                //projectPlannedResourceCost += taskPlannedResourceCost;

                if (taskDto.IsNew)
                {
                    if (!taskDto.IsExternalChildSumaryTask)
                    {
                        AddNewFlowOrTaskToProjectEntity(projectEntity, taskDto);
                    }
                }
                else if (taskDto.IsRelatedEntitiesModified())
                {

                    if (!taskDto.IsExternalChildSumaryTask)
                    {
                        if (dictTaskEntityFromDB.ContainsKey((int)taskDto.Id))
                        {
                            var dirtyTaskEntity = dictTaskEntityFromDB[(int)taskDto.Id];
                            AppProjectWorkFlowTaskConverter.CopyDtoToEntity(dirtyTaskEntity, taskDto);

                            // process Predecessor
                            ProcessAppProjectTaskPredecessor(deletdProjectPredecessorIDs, taskDto, dirtyTaskEntity);
                            ProcessAppProjectTaskResource(deletdProjectActivityResourceIDs, taskDto, dirtyTaskEntity);
                        }
                        else // if not exsting  add as a new activity 
                        {
                            AddNewFlowOrTaskToProjectEntity(projectEntity, taskDto);
                        }
                    }
                }
            }

            //projectEntity.PlannedWorkHours = projectPlannedWorkHours;
            //projectEntity.PlannedResourceCost = projectPlannedResourceCost;
        }

        //private static void CalculateOneTaskPlanedResourceHourAndCost(Dictionary<int, double?> dictUsrIdAndRate, AppProjectWorkFlowTaskExDto taskDto)
        //{
        //    if (taskDto.AppProjectTaskResourceList != null)
        //    {
        //        double totalTaskPlannedWorkHours = 0;
        //        double totalTaskPlannedResourceCost = 0;

        //        foreach (var resourceDto in taskDto.AppProjectTaskResourceList)
        //        {
        //            resourceDto.PlannedWorkHours = resourceDto.PlannedWorkHours.HasValue ? resourceDto.PlannedWorkHours.Value : 0;

        //            if (resourceDto.PlannedWorkHours.HasValue)
        //            {
        //                totalTaskPlannedWorkHours += resourceDto.PlannedWorkHours.Value;

        //                double? rate = dictUsrIdAndRate[resourceDto.UserId.Value];
        //                rate = rate.HasValue ? rate.Value : 0;

        //                totalTaskPlannedResourceCost += rate.Value * resourceDto.PlannedWorkHours.Value;
        //            }

        //        }

        //        taskDto.PlannedWorkHours = totalTaskPlannedWorkHours;
        //        taskDto.PlannedResourceCost = totalTaskPlannedResourceCost;

        //    }
        //}

        public static void InitializeActivityPredecessorsAndSuccessors(AppProjectOrWorkFlowExDto project)
        {
            project.PredecessorList = new List<AppProjectTaskPredecessorExDto>();

            foreach (AppProjectWorkFlowTaskExDto activity in project.AppProjectWorkFlowTaskList)
            {
                foreach (AppProjectTaskPredecessorExDto predecessor in activity.AppProjectTaskPredecessorList)
                {
                    predecessor.CurrentTaskGuid = activity.RowIdentity;
                    project.PredecessorList.Add(predecessor);

                    predecessor.AppProjectWorkFlowTaskPredecessorExDto = project.AppProjectWorkFlowTaskList.First(a => a.RowIdentity == predecessor.PredecessorGuid);
                    predecessor.AppProjectWorkFlowTaskExDto = activity;

                    if (predecessor.AppProjectWorkFlowTaskPredecessorExDto.Sucessors == null)
                    {
                        predecessor.AppProjectWorkFlowTaskPredecessorExDto.Sucessors = new List<AppProjectWorkFlowTaskExDto>();
                    }

                    if (!predecessor.AppProjectWorkFlowTaskPredecessorExDto.Sucessors.Any(s => s.RowIdentity.Equals(predecessor.AppProjectWorkFlowTaskExDto.RowIdentity)))
                    {
                        predecessor.AppProjectWorkFlowTaskPredecessorExDto.Sucessors.Add(predecessor.AppProjectWorkFlowTaskExDto);
                    }

                }
            }
        }

        private static void ProcessProjectConditionList(List<int> deleteConditionIds, List<int> deletdActionIDs, AppProjectOrWorkFlowExDto modifyAppProjectWorkFlowTaskDto, AppProjectOrWorkFlowEntity dirtyAppProjectWorkFlowTask)
        {   // new entity

            var dictConditionEntityFromDB = dirtyAppProjectWorkFlowTask.AppProjectWorkFlowCondition.ToDictionary(o => o.WorkFlowConditionId, o => o);

            foreach (var conditionDto in modifyAppProjectWorkFlowTaskDto.AppProjectWorkFlowConditionList)
            {
                if (conditionDto.IsNew)
                {
                    AddNewConditionDtoToWorkFlowTaskEntity(dirtyAppProjectWorkFlowTask, conditionDto);

                }

                //	// updated Condition
                else if (conditionDto.IsRelatedEntitiesModified())
                {

                    if (dictConditionEntityFromDB.ContainsKey((int)conditionDto.Id))
                    {

                        var dirtyCondioEntity = dictConditionEntityFromDB[(int)conditionDto.Id];
                        AppProjectWorkFlowConditionConverter.CopyDtoToEntity(dirtyCondioEntity, conditionDto);

                        //

                        var dictActionEntityFromDB = dirtyCondioEntity.AppProjectWorkFlowAction.ToDictionary(o => o.WorkFlowActionId, o => o);

                        foreach (var actionDto in conditionDto.AppProjectWorkFlowActionList)
                        {
                            if (!actionDto.IsNew && dictActionEntityFromDB.ContainsKey((int)actionDto.Id))
                            {

                                var dirtyActionEntity = dictActionEntityFromDB[(int)actionDto.Id];
                                AppProjectWorkFlowActionConverter.CopyDtoToEntity(dirtyActionEntity, actionDto);

                                //if (actionDto.NextWorkFlowGuId.HasValue)
                                //{
                                //    dirtyActionEntity.NextWorkFlowGuid = actionDto.NextWorkFlowGuId;
                                //}

                            }
                            else
                            {
                                AddActionDtoToConditionEntity(dirtyCondioEntity, actionDto);

                            }


                        }

                        var delActionids = conditionDto.AppProjectWorkFlowActionList.FindDeletedItemIds().Cast<int>().ToList();
                        deletdActionIDs.AddRange(delActionids);


                    }
                    else // it is a new condition
                    {
                        AddNewConditionDtoToWorkFlowTaskEntity(dirtyAppProjectWorkFlowTask, conditionDto);

                    }

                }


            }


            var delidps = modifyAppProjectWorkFlowTaskDto.AppProjectWorkFlowConditionList.FindDeletedItemIds().Cast<int>().ToList();
            deleteConditionIds.AddRange(delidps);
        }

        private static void ProcessAppProjectTaskResource(List<int> deleteProjectTaskResourceIds, AppProjectWorkFlowTaskExDto modifyAppProjectWorkFlowTaskDto, AppProjectWorkFlowTaskEntity dirtyAppProjectWorkFlowTask)
        {
            // No  Modify Dto Entity
            foreach (var aChildDto in modifyAppProjectWorkFlowTaskDto.AppProjectTaskResourceList.FindNewItems())
            {
                AppProjectTaskResourceEntity aNewChildEntity = new AppProjectTaskResourceEntity();
                AppProjectTaskResourceConverter.CopyDtoToEntity(aNewChildEntity, aChildDto);
                dirtyAppProjectWorkFlowTask.AppProjectTaskResource.Add(aNewChildEntity);
            }

            Dictionary<int, AppProjectTaskResourceEntity> dictTaskResourceFromDbms = dirtyAppProjectWorkFlowTask.AppProjectTaskResource.Where(o => !o.IsNew).ToDictionary(o => o.TaskResourceId, o => o);

            foreach (var modifyitem in modifyAppProjectWorkFlowTaskDto.AppProjectTaskResourceList.FindModifiedItems())
            {
                int dtoKey = int.Parse(modifyitem.Id.ToString());
                if (dictTaskResourceFromDbms.ContainsKey(dtoKey))
                {
                    AppProjectTaskResourceConverter.CopyDtoToEntity(dictTaskResourceFromDbms[dtoKey], modifyitem);
                }
            }

            var delidps = modifyAppProjectWorkFlowTaskDto.AppProjectTaskResourceList.FindDeletedItemIds().Cast<int>().ToList();

            deleteProjectTaskResourceIds.AddRange(delidps);
        }

        // process activity
        private static void ProcessAppProjectTaskPredecessor(List<int> deletdProjectPredecessorIDs, AppProjectWorkFlowTaskExDto modifyAppProjectWorkFlowTaskDto, AppProjectWorkFlowTaskEntity dirtyAppProjectWorkFlowTask)
        {
            // new added entity
            foreach (var AppProjectTaskPredecessorDto in modifyAppProjectWorkFlowTaskDto.AppProjectTaskPredecessorList.FindNewItems())
            {
                AppProjectTaskPredecessorEntity aNewChildEntity = new AppProjectTaskPredecessorEntity();
                AppProjectTaskPredecessorConverter.CopyDtoToEntity(aNewChildEntity, AppProjectTaskPredecessorDto);
                aNewChildEntity.PredecessorGuid = AppProjectTaskPredecessorDto.PredecessorGuid;
                dirtyAppProjectWorkFlowTask.AppProjectTaskPredecessor.Add(aNewChildEntity);
            }
            var dictProjectTaskPredecessor = dirtyAppProjectWorkFlowTask.AppProjectTaskPredecessor.Where(o => o.ProjectActivityPredecessorId != 0).ToDictionary(o => o.ProjectActivityPredecessorId, o => o);
            foreach (int id in dictProjectTaskPredecessor.Keys)
            {

                var dto = modifyAppProjectWorkFlowTaskDto.AppProjectTaskPredecessorList.FindModifiedItems().FirstOrDefault(o => (int)o.Id == id);
                if (dto != null)
                {
                    AppProjectTaskPredecessorConverter.CopyDtoToEntity(dictProjectTaskPredecessor[id], dto);
                }



            }




            // delete ids
            var delidps = modifyAppProjectWorkFlowTaskDto.AppProjectTaskPredecessorList.FindDeletedItemIds().Cast<int>().ToList();

            // need to remove from exsting enity list
            foreach (int deletePredesessorId in delidps)
            {
                var deleteEntity = dirtyAppProjectWorkFlowTask.AppProjectTaskPredecessor.Where(o => o.ProjectActivityPredecessorId == deletePredesessorId).FirstOrDefault();
                if (deleteEntity != null)
                {
                    dirtyAppProjectWorkFlowTask.AppProjectTaskPredecessor.Remove(deleteEntity);
                }
            }

            deletdProjectPredecessorIDs.AddRange(delidps);
        }


        private static OperationCallResult<AppProjectOrWorkFlowExDto> SaveNewProjectOrWorkFlowExDto(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, OperationCallResult<AppProjectOrWorkFlowExDto> aOperationCallResult)
        {

            ValidationResult aValidationResult = aOperationCallResult.ValidationResult;

            int? projectId = null;


            if (aAppProjectOrWorkFlowExDto.IsNew)
            {

                ObservableSet<AppProjectWorkFlowTaskExDto> projectWorkFlowTaskList = GetFlatTaskExDtoList(aAppProjectOrWorkFlowExDto);

                AppProjectOrWorkFlowEntity aAppProjectOrWorkFlowEntity = new AppProjectOrWorkFlowEntity();
                AppProjectOrWorkFlowConverter.CopyDtoToEntity(aAppProjectOrWorkFlowEntity, aAppProjectOrWorkFlowExDto);



                Dictionary<Guid, int> dictExternalTaskGuidId = new Dictionary<Guid, int>();

                foreach (var workFlowOrTaskDto in projectWorkFlowTaskList)
                {
                    if (workFlowOrTaskDto.IsExternalChildSumaryTask)
                    {
                        dictExternalTaskGuidId.Add(workFlowOrTaskDto.RowIdentity.Value, (int)workFlowOrTaskDto.Id);
                    }
                    else
                    {
                        AddNewFlowOrTaskToProjectEntity(aAppProjectOrWorkFlowEntity, workFlowOrTaskDto);

                    }



                }

                foreach (var conditionDto in aAppProjectOrWorkFlowExDto.AppProjectWorkFlowConditionList)
                {

                    AddNewConditionDtoToWorkFlowTaskEntity(aAppProjectOrWorkFlowEntity, conditionDto);


                }


                ProcessProjectEntityPredesessor(aAppProjectOrWorkFlowEntity, dictExternalTaskGuidId);
                ProcessProjectEntityNextActionList(aAppProjectOrWorkFlowEntity);

                EntityCollection<AppProjectWorkFlowTaskEntity> allAppProjectWorkFlowTask = new EntityCollection<AppProjectWorkFlowTaskEntity>();

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppProjectOrWorkFlowEntity);


                        UpdateTaskMainTaskId(projectWorkFlowTaskList, aAppProjectOrWorkFlowEntity.ProjectId, adapter);

                        // need to update SubTask main task ID

                        projectId = aAppProjectOrWorkFlowEntity.ProjectId;

                        if (aAppProjectOrWorkFlowExDto.NewWorkflowTransactionId.HasValue)
                        {
                            AppTransactionEntity transactionEntity = new AppTransactionEntity() { MasterWorkflowId = projectId };
                            adapter.UpdateEntitiesDirectly(transactionEntity, new RelationPredicateBucket(AppTransactionFields.TransactionId == aAppProjectOrWorkFlowExDto.NewWorkflowTransactionId.Value));

                        }

                        adapter.Commit();

                        //if (aAppProjectOrWorkFlowExDto.SaasApplicationId.HasValue)
                        //{
                        //    AppApplicationAssetsItemExDto transactionAssetsItemDto = new AppApplicationAssetsItemExDto();
                        //    transactionAssetsItemDto.ApplicationId = aAppProjectOrWorkFlowExDto.SaasApplicationId;
                        //    transactionAssetsItemDto.ProjectWorkflowId = projectId;
                        //    AppSaasUserApplicationPackageBL.SaveNewApplicationAssetsItemExDto(transactionAssetsItemDto);
                        //}

                        aValidationResult.Items.Add(new ValidationItem(typeof(AppFormEntity), "App_AppProjectOrWorkFlowEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                    }



                    // Database FK Exeption ........
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectOrWorkFlowExDto), "plm_AppProjectOrWorkFlowEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }





            }

            if (projectId.HasValue)
            {
                aOperationCallResult.Object = RetrieveOneAppProjectOrWorkFlowExDto(projectId);
                if (aAppProjectOrWorkFlowExDto.NewWorkflowTransactionId.HasValue)
                {
                    AppCacheManagerBL.RefreshOnetHierarchyTranscation(aAppProjectOrWorkFlowExDto.NewWorkflowTransactionId.Value);
                }
            }

            return aOperationCallResult;
        }

        private static void UpdateTaskMainTaskId(ObservableSet<AppProjectWorkFlowTaskExDto> projectWorkFlowTaskList, int proejctId, DataAccessAdapter adapter)
        {

            EntityCollection<AppProjectWorkFlowTaskEntity> allAppProjectWorkFlowTask = new EntityCollection<AppProjectWorkFlowTaskEntity>();
            adapter.FetchEntityCollection(allAppProjectWorkFlowTask, new RelationPredicateBucket(AppProjectWorkFlowTaskFields.ProjectId == proejctId));

            List<AppProjectWorkFlowTaskEntity> UpdateAppProjectWorkFlowTaskEntityList = GetTaskMainTaskIdUpdateProdecessorAndSubTaskMainTaskId(allAppProjectWorkFlowTask, projectWorkFlowTaskList);

            foreach (AppProjectWorkFlowTaskEntity aAppProjectWorkFlowTaskEntity in UpdateAppProjectWorkFlowTaskEntityList)
            {
                adapter.SaveEntity(aAppProjectWorkFlowTaskEntity);

            }
        }


        private static List<AppProjectWorkFlowTaskEntity> GetTaskMainTaskIdUpdateProdecessorAndSubTaskMainTaskId(EntityCollection<AppProjectWorkFlowTaskEntity> existinWrokFlowCollection, ObservableSet<AppProjectWorkFlowTaskExDto> projectWorkFlowTaskList)
        {

            List<AppProjectWorkFlowTaskEntity> UpdateAppProjectWorkFlowTaskEntity = new List<AppProjectWorkFlowTaskEntity>();

            Dictionary<Guid, int> dictGuiIdProjectTaskorWorkFlowId = existinWrokFlowCollection.ToDictionary(o => o.RowIdentity.Value, o => o.ProjectWorkFlowTaskId);
            var dictGuiIdWorkFlowTask = existinWrokFlowCollection.ToDictionary(o => o.RowIdentity, o => o);

            foreach (var taskDto in projectWorkFlowTaskList)
            {
                if (taskDto.MainTaskGuId.HasValue)
                {
                    AppProjectWorkFlowTaskEntity aAppProjectWorkFlowTaskEntity = dictGuiIdWorkFlowTask[taskDto.RowIdentity];
                    aAppProjectWorkFlowTaskEntity.MainTaskId = dictGuiIdProjectTaskorWorkFlowId[taskDto.MainTaskGuId.Value];
                    UpdateAppProjectWorkFlowTaskEntity.Add(aAppProjectWorkFlowTaskEntity);

                }
            }

            return UpdateAppProjectWorkFlowTaskEntity;

        }


        private static void AddNewFlowOrTaskToProjectEntity(AppProjectOrWorkFlowEntity aAppProjectOrWorkFlowEntity, AppProjectWorkFlowTaskExDto workFlowOrTaskDto)
        {
            AppProjectWorkFlowTaskEntity workFlowOrTaskEntity = ConvertNewTaskDtoToEntity(workFlowOrTaskDto);
            aAppProjectOrWorkFlowEntity.AppProjectWorkFlowTask.Add(workFlowOrTaskEntity);

            // add prodecessport to WorkFlow task
            foreach (var processDtoDto in workFlowOrTaskDto.AppProjectTaskPredecessorList)
            {
                AddNewProcessDtoToWorkFlowTaskEntity(workFlowOrTaskEntity, processDtoDto);

            }



            // need to add resoruce as weel
            foreach (var resourceDto in workFlowOrTaskDto.AppProjectTaskResourceList)
            {

                AppProjectTaskResourceEntity resourceEntity = new AppProjectTaskResourceEntity();
                AppProjectTaskResourceConverter.CopyDtoToEntity(resourceEntity, resourceDto);
                workFlowOrTaskEntity.AppProjectTaskResource.Add(resourceEntity);




            }

        }


        private static void AddNewProcessDtoToWorkFlowTaskEntity(AppProjectWorkFlowTaskEntity workFlowOrTaskEntity, AppProjectTaskPredecessorExDto processerDto)
        {
            AppProjectTaskPredecessorEntity appProjectTaskPredecessorEntity = new AppProjectTaskPredecessorEntity();
            AppProjectTaskPredecessorConverter.CopyDtoToEntity(appProjectTaskPredecessorEntity, processerDto);
            workFlowOrTaskEntity.AppProjectTaskPredecessor.Add(appProjectTaskPredecessorEntity);
            appProjectTaskPredecessorEntity.PredecessorGuid = processerDto.PredecessorGuid;



        }

        private static void ProcessProjectEntityPredesessor(AppProjectOrWorkFlowEntity aPdmTaprojectEntity, Dictionary<Guid, int> dictExternalGuidFlowTaskId)
        {
            var dictGuidProjectEntity = aPdmTaprojectEntity.AppProjectWorkFlowTask.ToDictionary(o => o.RowIdentity, o => o);


            foreach (var pdmTaprojectActivityEntity in aPdmTaprojectEntity.AppProjectWorkFlowTask)
            {
                foreach (var pdmTaprojectActivityPredecessorEntity in pdmTaprojectActivityEntity.AppProjectTaskPredecessor)
                {
                    if (pdmTaprojectActivityPredecessorEntity.PredecessorGuid.HasValue)
                    {
                        var key = pdmTaprojectActivityPredecessorEntity.PredecessorGuid.Value;
                        if (dictGuidProjectEntity.ContainsKey(key))
                        {
                            pdmTaprojectActivityPredecessorEntity.AppProjectWorkFlowTask_ = dictGuidProjectEntity[key];

                        }
                        else
                        {
                            if (dictExternalGuidFlowTaskId.ContainsKey(key))
                            {
                                pdmTaprojectActivityPredecessorEntity.PredecessorId = dictExternalGuidFlowTaskId[key];
                            }



                        }

                    }
                }
            }
        }

        private static void ProcessProjectEntityNextActionList(AppProjectOrWorkFlowEntity aAppProjectOrWorkFlowEntity)
        {
            var dictGuidProjectEntity = aAppProjectOrWorkFlowEntity.AppProjectWorkFlowTask.ToDictionary(o => o.RowIdentity, o => o);
            foreach (var appProjectWorkFlowCondition in aAppProjectOrWorkFlowEntity.AppProjectWorkFlowCondition)
            {

                foreach (var actionEntity in appProjectWorkFlowCondition.AppProjectWorkFlowAction)
                    if (actionEntity.NextWorkFlowGuid.HasValue)
                    {
                        var key = actionEntity.NextWorkFlowGuid.Value;
                        actionEntity.AppProjectWorkFlowTask = dictGuidProjectEntity[key];
                    }
            }

        }


        private static void AddNewConditionDtoToWorkFlowTaskEntity(AppProjectOrWorkFlowEntity workFlowOrTaskEntity, AppProjectWorkFlowConditionExDto conditionDto)
        {
            AppProjectWorkFlowConditionEntity conditionEntity = new AppProjectWorkFlowConditionEntity();
            AppProjectWorkFlowConditionConverter.CopyDtoToEntity(conditionEntity, conditionDto);
            workFlowOrTaskEntity.AppProjectWorkFlowCondition.Add(conditionEntity);



            foreach (var actionDto in conditionDto.AppProjectWorkFlowActionList)
            {
                AddActionDtoToConditionEntity(conditionEntity, actionDto);

            }
        }

        private static void AddActionDtoToConditionEntity(AppProjectWorkFlowConditionEntity conditionEntity, AppProjectWorkFlowActionExDto actionDto)
        {
            AppProjectWorkFlowActionEntity actionEntity = new AppProjectWorkFlowActionEntity();
            AppProjectWorkFlowActionConverter.CopyDtoToEntity(actionEntity, actionDto);

            //if (actionDto.NextWorkFlowGuId.HasValue)
            //{
            //    actionEntity.NextWorkFlowGuid = actionDto.NextWorkFlowGuId;
            //}


            conditionEntity.AppProjectWorkFlowAction.Add(actionEntity);
        }

        internal static ObservableSet<AppProjectWorkFlowTaskExDto> GetFlatTaskExDtoList(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
        {
            ObservableSet<AppProjectWorkFlowTaskExDto> projectWorkFlowTaskList = new ObservableSet<AppProjectWorkFlowTaskExDto>();
            // aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList = projectWorkFlowTaskList;

            foreach (AppProjectWorkFlowTaskExDto taskDto in aAppProjectOrWorkFlowExDto.RootTreeList)
            {
                projectWorkFlowTaskList.Add(taskDto);
                AddChildTaskDto(projectWorkFlowTaskList, taskDto);

            }
            return projectWorkFlowTaskList;
        }

        private static AppProjectWorkFlowTaskEntity ConvertNewTaskDtoToEntity(AppProjectWorkFlowTaskExDto taskDto)
        {
            AppProjectWorkFlowTaskEntity taskEntity = new AppProjectWorkFlowTaskEntity();
            AppProjectWorkFlowTaskConverter.CopyDtoToEntity(taskEntity, taskDto);

            return taskEntity;
        }




        public static OperationCallResult<bool> DeleteProjectWorkFlow(int? projectId)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (projectId.HasValue)
            {

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");

                        string deleteProjectWorkFlow =
                            @"  delete [dbo].[AppProjectWorkFlowAction]  where [WorkFlowConditionID] in (
	                            select  [WorkFlowConditionID]  from  [AppProjectWorkFlowCondition] where ProjectID = @ProjectID
                            )

                            delete [dbo].[AppProjectTaskResource] where ProjectWorkFlowTaskID in (select ProjectWorkFlowTaskID from [dbo].[AppProjectWorkFlowTask] where ProjectID = @ProjectID)

                            delete [dbo].[AppProjectTeamMember] where  ProjectID = @ProjectID

                            delete [dbo].[AppProjectWorkFlowAction]  where  [AppProjectWorkFlowAction].[NextWorkFlowID] in (select ProjectWorkFlowTaskID from [dbo].[AppProjectWorkFlowTask] where ProjectID = @ProjectID)
                        

                            delete  from  [AppProjectWorkFlowCondition] where  ProjectID = @ProjectID

                            delete [dbo].[AppProjectTaskPredecessor] where  ProjectWorkFlowTaskID in (select ProjectWorkFlowTaskID from [dbo].[AppProjectWorkFlowTask] where ProjectID = @ProjectID)

                            delete [dbo].[AppProjectTaskPredecessor] where  [PredecessorID] in (select ProjectWorkFlowTaskID from [dbo].[AppProjectWorkFlowTask] where ProjectID = @ProjectID)


                            delete [dbo].[AppProjectTaskCheckList] where ProjectTaskID in (select ProjectWorkFlowTaskID from [dbo].[AppProjectWorkFlowTask] where ProjectID = @ProjectID)

                            delete [dbo].[AppPorjectWorkFlowTaskTimeSheet] where ProjectWorkFlowTaskID in (select ProjectWorkFlowTaskID from [dbo].[AppProjectWorkFlowTask] where ProjectID = @ProjectID)


                            delete [dbo].[AppMessageDeleted]  where MessageID in (select MessageID from [dbo].[AppMessage] where ProjectActivityID in (select ProjectWorkFlowTaskID from [dbo].[AppProjectWorkFlowTask] where ProjectID = @ProjectID))

                            delete [dbo].[AppMessageUserReceived]  where MessageID in (select MessageID from [dbo].[AppMessage] where ProjectActivityID in (select ProjectWorkFlowTaskID from [dbo].[AppProjectWorkFlowTask] where ProjectID = @ProjectID))

                            delete [dbo].[AppMessage] where ProjectActivityID in (select ProjectWorkFlowTaskID from [dbo].[AppProjectWorkFlowTask] where ProjectID = @ProjectID)



                            update [dbo].[AppProjectWorkFlowTask] set MainTaskID = null where  MainTaskID in (select ProjectWorkFlowTaskID from [dbo].[AppProjectWorkFlowTask] where ProjectID = @ProjectID)

                            delete [dbo].[AppProjectWorkFlowTask] where  ProjectID = @ProjectID

                            delete [dbo].[AppProjectOrWorkFlow] where ProjectID = @ProjectID
                        ";


                        List<SqlParameter> paramters = new List<SqlParameter>();
                        paramters.Add(new SqlParameter("@ProjectID", projectId.Value));



                        adapter.ExecuteScalarQuery(deleteProjectWorkFlow, paramters);



                        adapter.Commit();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppFormEntity), "App_AppProjectOrWorkFlowEntity_Delete_OK", ValidationItemType.Message, "Delete Successfully"));
                        aOperationCallResult.Object = true;

                    }
                    // Database FK Exeption ........
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectOrWorkFlowExDto), "plm_AppProjectOrWorkFlowEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectOrWorkFlowExDto), "plm_AppProjectOrWorkFlowEntity_ProjectIdRequired_Error", ValidationItemType.Error, "ProjectId Required".ToString()));
            }

            return aOperationCallResult;
        }


        private static void DeleteflowTask(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, ValidationResult aValidationResult)
        {
            List<object> deleteTaksIds = aAppProjectOrWorkFlowExDto.DeletedItemsIds;
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");

                    DeleteFlowTaskWithDepedency(deleteTaksIds, adapter);

                    adapter.Commit();



                }



                // Database FK Exeption ........
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectOrWorkFlowExDto), "plm_AppProjectOrWorkFlowEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }
        }




        private static void DeleteFlowTaskWithDepedency(List<object> deleteTaksIds, DataAccessAdapter adapter)
        {
            string deletallChild = @"delete [dbo].[AppProjectWorkFlowAction]  where [WorkFlowConditionID] in (

				 	    select  [WorkFlowConditionID]  from  [AppProjectWorkFlowCondition] where  [ProjectWorkFlowTaskID] =@ProjectWorkFlowTaskID

					)

                    delete [dbo].[AppProjectTaskResource] where ProjectWorkFlowTaskID = @ProjectWorkFlowTaskID

					delete [dbo].[AppProjectWorkFlowAction]  where  [AppProjectWorkFlowAction].[NextWorkFlowID] = @ProjectWorkFlowTaskID


					delete  from  [AppProjectWorkFlowCondition] where  [ProjectWorkFlowTaskID] =@ProjectWorkFlowTaskID


					delete [dbo].[AppProjectTaskPredecessor] where  ProjectWorkFlowTaskID = @ProjectWorkFlowTaskID

					delete [dbo].[AppProjectTaskPredecessor] where  [PredecessorID] = @ProjectWorkFlowTaskID


					update [dbo].[AppProjectWorkFlowTask] set MainTaskID = null where  MainTaskID =@ProjectWorkFlowTaskID

			  	delete  [dbo].[AppProjectWorkFlowTask] where [ProjectWorkFlowTaskID]  =@ProjectWorkFlowTaskID


					";

            foreach (object projectWorkFlowTaskID in deleteTaksIds)
            {
                List<SqlParameter> paramters = new List<SqlParameter>();
                paramters.Add(new SqlParameter("@ProjectWorkFlowTaskID", projectWorkFlowTaskID));


                adapter.ExecuteScalarQuery(deletallChild, paramters);

            }

            ////adapter.DeleteEntitiesDirectly(typeof(AppProjectTaskPredecessorEntity), new RelationPredicateBucket(AppProjectTaskPredecessorFields.ProjectActivityPredecessorId == deleteTaksIds));
            ////         adapter.DeleteEntitiesDirectly(typeof(AppProjectWorkFlowTaskEntity), new RelationPredicateBucket(AppProjectWorkFlowTaskFields.MainTaskId == deleteTaksIds));
            //   adapter.DeleteEntitiesDirectly(typeof(AppProjectWorkFlowTaskEntity), new RelationPredicateBucket(AppProjectWorkFlowTaskFields.ProjectWorkFlowTaskId == deleteTaksIds));

        }

        private static void AddChildTaskDto(ObservableSet<AppProjectWorkFlowTaskExDto> projectWorkFlowTaskList, AppProjectWorkFlowTaskExDto taskDto)
        {
            if (!taskDto.Children.IsEmpty())
            {
                foreach (var childDto in taskDto.Children)
                {

                    childDto.MainTaskGuId = taskDto.RowIdentity;
                    projectWorkFlowTaskList.Add(childDto);

                    AddChildTaskDto(projectWorkFlowTaskList, childDto);

                }
            }

        }

        internal static AppProjectWorkFlowTaskEntity RetrieveOneAppProjectWorkFlowTaskEntity(object taskId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppProjectWorkFlowTaskEntity aTaskEntity = new AppProjectWorkFlowTaskEntity(int.Parse(taskId.ToString()));
                IPrefetchPath2 root = new PrefetchPath2(EntityType.AppProjectWorkFlowTaskEntity);
                var projectPath = root.Add(AppProjectWorkFlowTaskEntity.PrefetchPathAppProjectOrWorkFlow);
                root.Add(AppProjectWorkFlowTaskEntity.PrefetchPathAppProjectTaskResource);

                adapter.FetchEntity(aTaskEntity, root);

                return aTaskEntity;
            }
        }


        internal static AppProjectWorkFlowTaskExDto RetrieveOneAppProjectWorkFlowTask(object taskId)
        {
            AppProjectWorkFlowTaskEntity aTaskEntity = RetrieveOneAppProjectWorkFlowTaskEntity(taskId);

            if (aTaskEntity != null)
            {
                AppProjectWorkFlowTaskExDto taskDto = AppProjectWorkFlowTaskConverter.ConvertEntityToExDto(aTaskEntity);



                foreach (AppProjectTaskResourceEntity appProjectTaskResourceEntity in aTaskEntity.AppProjectTaskResource)
                {
                    taskDto.AppProjectTaskResourceList.Add(AppProjectTaskResourceConverter.ConvertEntityToExDto(appProjectTaskResourceEntity));
                }

                if (taskDto.ProjectId.HasValue && aTaskEntity.AppProjectOrWorkFlow != null)
                {
                    var projectEntity = aTaskEntity.AppProjectOrWorkFlow;

                    if (projectEntity.ProjectWorkflowType.HasValue && projectEntity.ProjectWorkflowType.Value == (int)EmAppProjectWorkflowType.BusinessProcessWorkflow)
                    {
                        taskDto.EmAppTaskSystemDefinedCategory = (int)EmAppTaskSystemDefinedCategory.WorkflowTask;
                        taskDto.CategoryDetailDisplay = StringLocalizer.Localize("script_TaskManagement_Workflow", "Workflow") + ": " + projectEntity.Name;
                        taskDto.ProjectName = aTaskEntity.AppProjectOrWorkFlow.Name;
                    }
                    else
                    {
                        taskDto.EmAppTaskSystemDefinedCategory = (int)EmAppTaskSystemDefinedCategory.ProjectTask;
                        taskDto.CategoryDetailDisplay = StringLocalizer.Localize("script_TaskManagement_Project", "Project") + ": " + projectEntity.Name;
                    }
                }
                else if (taskDto.TransactionId.HasValue && !string.IsNullOrEmpty(taskDto.TransactionRid))
                {
                    taskDto.EmAppTaskSystemDefinedCategory = (int)EmAppTaskSystemDefinedCategory.SimpleFormTask;
                    var transactionentity = AppTransactionBL.RetrieveOneAppTransactionSimpleEntity(taskDto.TransactionId.Value);

                    string transactionName = "Unknonw Transaction";
                    if (transactionentity != null)
                    {
                        transactionName = transactionentity.TransactionName;

                    }

                    taskDto.CategoryDetailDisplay = StringLocalizer.Localize("script_TaskManagement_Form", "Form") + ": " + transactionName + " (#" + taskDto.TransactionRid.ToString() + ")";
                }
                else
                {
                    taskDto.EmAppTaskSystemDefinedCategory = (int)EmAppTaskSystemDefinedCategory.UserDefinedFreeTask;
                    taskDto.CategoryDetailDisplay = StringLocalizer.Localize("script_TaskManagement_StandaloneTasks", "Standalone Tasks");
                }

                // CalculateOneTaskCompletePercentage(taskDto);

                if (!taskDto.EmAppTaskOwnerDeliverPhase.HasValue)
                {
                    taskDto.EmAppTaskOwnerDeliverPhase = (int)EmAppTaskOwnerDeliverPhase.New;
                }



                return taskDto;
            }

            return null;
        }




        private static AppProjectOrWorkFlowEntity RetrieveOneAppProjectOrWorkFlowEntity(object proejctId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppProjectOrWorkFlowEntity projectEntity = RetrieveOneAppProjectOrWorkFlowEntity(proejctId, adpater);

                return projectEntity;
            }
        }

        private static void ProcessChilds(IEnumerable<AppProjectWorkFlowTaskExDto> allfoldersTreeItems, AppProjectWorkFlowTaskExDto folderTreeItemDto)
        {
            AppProjectWorkFlowTaskExDto[] children = GetChilds(allfoldersTreeItems, folderTreeItemDto).OrderBy(f => f.Sort).ToArray();

            if (!children.IsEmpty())
            {
                folderTreeItemDto.Children = children;
                folderTreeItemDto.Children.ForAll(c => ProcessChilds(allfoldersTreeItems, c));

            }
        }

        private static AppProjectWorkFlowTaskExDto[] GetChilds(IEnumerable<AppProjectWorkFlowTaskExDto> allfoldersTreeItems, AppProjectWorkFlowTaskExDto folderTreeItemDto)
        {
            return allfoldersTreeItems.Where(f => f.MainTaskId == (int)folderTreeItemDto.Id).ToArray();
        }

        internal static AppProjectOrWorkFlowEntity RetrieveOneAppProjectOrWorkFlowSimpleEntity(object proejctId)
        {
            AppProjectOrWorkFlowEntity projectEntity = new AppProjectOrWorkFlowEntity((int)proejctId);

            IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppProjectOrWorkFlowEntity);
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                adpater.FetchEntity(projectEntity, rootPath);
            }


            return projectEntity;
        }


        private static AppProjectOrWorkFlowEntity RetrieveOneAppProjectOrWorkFlowEntityOnlyWith(object proejctId)
        {
            AppProjectOrWorkFlowEntity projectEntity = new AppProjectOrWorkFlowEntity((int)proejctId);

            IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppProjectOrWorkFlowEntity);
            var projectActivityPath = rootPath.Add(AppProjectOrWorkFlowEntity.PrefetchPathAppProjectWorkFlowTask);
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                adpater.FetchEntity(projectEntity, rootPath);
            }


            return projectEntity;
        }
        internal static AppProjectOrWorkFlowEntity RetrieveOneAppProjectOrWorkFlowEntity(object proejctId, DataAccessAdapter adpater)
        {
            //AppProjectOrWorkFlowEntity
            AppProjectOrWorkFlowEntity projectEntity = new AppProjectOrWorkFlowEntity((int)proejctId);

            IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppProjectOrWorkFlowEntity);



            //Task child level
            var projectActivityPath = rootPath.Add(AppProjectOrWorkFlowEntity.PrefetchPathAppProjectWorkFlowTask);
            projectActivityPath.SubPath.Add(AppProjectWorkFlowTaskEntity.PrefetchPathAppProjectTaskPredecessor);
            projectActivityPath.SubPath.Add(AppProjectWorkFlowTaskEntity.PrefetchPathAppProjectTaskResource).SubPath.Add(AppProjectTaskResourceEntity.PrefetchPathAppProjectTaskResourcePlannedHours);

            //Condition child level
            var conditionPath = rootPath.Add(AppProjectOrWorkFlowEntity.PrefetchPathAppProjectWorkFlowCondition);
            conditionPath.SubPath.Add(AppProjectWorkFlowConditionEntity.PrefetchPathAppProjectWorkFlowAction);

            adpater.FetchEntity(projectEntity, rootPath);


            // Need to fetch child project Summary task as well


            var dictAppProjectWorkFlowTask = projectEntity.AppProjectWorkFlowTask.ToDictionary(o => o.ProjectWorkFlowTaskId, o => o);


            //EntityCollection<AppProjectWorkFlowTaskEntity> childProjectExternalSumaryTask = RetrieveAppChildProjectExternalSumaryTask(proejctId, adpater);

            //foreach (AppProjectWorkFlowTaskEntity appProjectWorkFlowTaskEntity in childProjectExternalSumaryTask)
            //{
            //    dictAppProjectWorkFlowTask.Add(appProjectWorkFlowTaskEntity.ProjectWorkFlowTaskId, appProjectWorkFlowTaskEntity);

            //}


            ProcessPredecessorGuid(projectEntity, dictAppProjectWorkFlowTask);

            //projectEntity.ChildProjectExternalSumaryTask = childProjectExternalSumaryTask;


            return projectEntity;
        }

        internal static EntityCollection<AppProjectWorkFlowTaskEntity> RetrieveAppChildProjectExternalSumaryTask(object proejctId, DataAccessAdapter adpater)
        {

            EntityCollection<AppProjectWorkFlowTaskEntity> toReturn = new EntityCollection<AppProjectWorkFlowTaskEntity>();

            IPrefetchPath2 projectActivityPath = new PrefetchPath2(EntityType.AppProjectWorkFlowTaskEntity);

            projectActivityPath.Add(AppProjectWorkFlowTaskEntity.PrefetchPathAppProjectTaskResource);

            RelationPredicateBucket filter = new RelationPredicateBucket();

            filter.PredicateExpression.Add(AppProjectWorkFlowTaskFields.IsProjectSumaryTask == true);
            filter.Relations.Add(AppProjectWorkFlowTaskEntity.Relations.AppProjectOrWorkFlowEntityUsingProjectId);

            filter.PredicateExpression.Add(AppProjectOrWorkFlowFields.ParentProjectId == proejctId);
            adpater.FetchEntityCollection(toReturn, filter, projectActivityPath);


            return toReturn;
        }
        private static void ProcessPredecessorGuid(AppProjectOrWorkFlowEntity projectEntity, Dictionary<int, AppProjectWorkFlowTaskEntity> dictAppProjectWorkFlowTask)
        {
            foreach (AppProjectWorkFlowTaskEntity workFlowTaskEntity in projectEntity.AppProjectWorkFlowTask)
            {

                foreach (var predesessorEntity in workFlowTaskEntity.AppProjectTaskPredecessor)
                {
                    int PredecessorId = predesessorEntity.PredecessorId;
                    int taskId = predesessorEntity.ProjectWorkFlowTaskId;


                    predesessorEntity.PredecessorGuid = dictAppProjectWorkFlowTask[predesessorEntity.PredecessorId].RowIdentity;
                }
            }
        }



        private static AppProjectOrWorkFlowExDto ConvertAppProjectOrWorkFlowEntity_AppProjectOrWorkFlowExDto(AppProjectOrWorkFlowEntity projectOrWorkFlowEntity)
        {
            //Dictionary<int, LookupItemDto> allUsersDictionary = AppEntityInfoBL.GetLookupItemListByCode(EmEntityCode.PDMUser.ToString (),"").ToDictionary(i => (int)i.Id);

            AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto = AppProjectOrWorkFlowConverter.ConvertEntityToExDto(projectOrWorkFlowEntity);
            Dictionary<int, Guid> dictWorkFlowIdGuId = projectOrWorkFlowEntity.AppProjectWorkFlowTask.ToDictionary(o => o.ProjectWorkFlowTaskId, o => o.RowIdentity.Value);



            AppProjectSettingBL.InitializedProjectStage(aAppProjectOrWorkFlowExDto);

            foreach (var projectActivity in projectOrWorkFlowEntity.AppProjectWorkFlowTask)
            {
                AppProjectWorkFlowTaskExDto aAppProjectWorkFlowTaskExDto = AppProjectWorkFlowTaskConverter.ConvertEntityToExDto(projectActivity);
                aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList.Add(aAppProjectWorkFlowTaskExDto);

                //Predecessor
                foreach (var AppProjectTaskPredecessorEntity in projectActivity.AppProjectTaskPredecessor)
                {
                    var AppProjectTaskPredecessorExDto = AppProjectTaskPredecessorConverter.ConvertEntityToExDto(AppProjectTaskPredecessorEntity);
                    AppProjectTaskPredecessorExDto.PredecessorGuid = AppProjectTaskPredecessorEntity.PredecessorGuid.Value;

                    aAppProjectWorkFlowTaskExDto.AppProjectTaskPredecessorList.Add(AppProjectTaskPredecessorExDto);
                }

                foreach (AppProjectTaskResourceEntity appProjectTaskResourceEntity in projectActivity.AppProjectTaskResource)
                {
                    aAppProjectWorkFlowTaskExDto.AppProjectTaskResourceList.Add(AppProjectTaskResourceConverter.ConvertEntityToExDto(appProjectTaskResourceEntity));

                }

                if (!aAppProjectWorkFlowTaskExDto.AmountOfTime.HasValue)
                {
                    aAppProjectWorkFlowTaskExDto.AmountOfTime = 0;
                }

                aAppProjectWorkFlowTaskExDto.CalStartDate = aAppProjectWorkFlowTaskExDto.DatePlannedStart;
                aAppProjectWorkFlowTaskExDto.CalEndDate = aAppProjectWorkFlowTaskExDto.DatePlannedEnd;

                if (aAppProjectWorkFlowTaskExDto.AmountOfTime.HasValue)
                {
                    aAppProjectWorkFlowTaskExDto.CalDurationDays = aAppProjectWorkFlowTaskExDto.AmountOfTime.Value;
                }
                else
                {
                    aAppProjectWorkFlowTaskExDto.CalDurationDays = 0;
                }

            }

            // need to add summary Task as well
            //foreach (var projectActivity in projectOrWorkFlowEntity.ChildProjectExternalSumaryTask)
            //{
            //    AppProjectWorkFlowTaskExDto aAppProjectWorkFlowTaskExDto = AppProjectWorkFlowTaskConverter.ConvertEntityToExDto(projectActivity);
            //    aAppProjectWorkFlowTaskExDto.IsExternalChildSumaryTask = true;

            //    aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList.Add(aAppProjectWorkFlowTaskExDto);

            //    //no need to process child summary Predecessor

            //    //
            //    dictWorkFlowIdGuId.Add(projectActivity.ProjectWorkFlowTaskId, projectActivity.RowIdentity.Value);


            //    foreach (AppProjectTaskResourceEntity appProjectTaskResourceEntity in projectActivity.AppProjectTaskResource)
            //    {
            //        aAppProjectWorkFlowTaskExDto.AppProjectTaskResourceList.Add(AppProjectTaskResourceConverter.ConvertEntityToExDto(appProjectTaskResourceEntity));

            //    }


            //}


            foreach (AppProjectWorkFlowConditionEntity appProjectWorkFlowConditionEntityEntity in projectOrWorkFlowEntity.AppProjectWorkFlowCondition)
            {
                var conditionDto = AppProjectWorkFlowConditionConverter.ConvertEntityToExDto(appProjectWorkFlowConditionEntityEntity);
                aAppProjectOrWorkFlowExDto.AppProjectWorkFlowConditionList.Add(conditionDto);

                foreach (AppProjectWorkFlowActionEntity appProjectWorkFlowActionEntity in appProjectWorkFlowConditionEntityEntity.AppProjectWorkFlowAction)
                {
                    var actionDto = AppProjectWorkFlowActionConverter.ConvertEntityToExDto(appProjectWorkFlowActionEntity);

                    if (actionDto.NextWorkFlowId.HasValue)
                    {
                        actionDto.NextWorkFlowGuId = dictWorkFlowIdGuId[actionDto.NextWorkFlowId.Value];

                    }

                    conditionDto.AppProjectWorkFlowActionList.Add(actionDto);

                }
            }

            return aAppProjectOrWorkFlowExDto;
        }


        public static List<LookupItemDto> RetrieveOneMasterWorkflowTransactionLookUpList(object masterWorkflowId)
        {
            List<LookupItemDto> toReturn = new List<LookupItemDto>();
            List<int> workflowTransIdList = new List<int>();

            if (masterWorkflowId != null)
            {
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    EntityCollection<AppTransactionEntity> list = new EntityCollection<AppTransactionEntity>();


                    RelationPredicateBucket filter = new RelationPredicateBucket(AppTransactionFields.TransactionOrganizedType == (int)EmTransactionOrganizedType.MasterDetail);
                    //filter.PredicateExpression.AddWithAnd(AppTransactionFields.MasterWorkflowId == masterWorkflowId);

                    IncludeFieldsList includeFied = new IncludeFieldsList();
                    includeFied.Add(AppTransactionFields.TransactionId);
                    includeFied.Add(AppTransactionFields.TransactionName);
                    includeFied.Add(AppTransactionFields.MasterTransactionId);
                    includeFied.Add(AppTransactionFields.MasterWorkflowId);

                    adapter.FetchEntityCollection(list, includeFied, filter);

                    workflowTransIdList = list.Where(o => o.MasterWorkflowId.HasValue && o.MasterWorkflowId.Value == int.Parse(masterWorkflowId.ToString())).Select(o => o.TransactionId).ToList();
                    AddChildTransactionIds(workflowTransIdList, list);
                    workflowTransIdList = workflowTransIdList.Distinct().ToList();


                    list.Where(o => workflowTransIdList.Contains(o.TransactionId)).ForAll(o => toReturn.Add(new LookupItemDto() { Id = o.TransactionId, Display = o.TransactionName }));
                    toReturn = toReturn.OrderBy(o => o.Display).ToList();
                }

            }


            return toReturn;
        }

        public static List<AppTransactionExDto> RetrieveOneMasterWorkflowTransactionList(object masterWorkflowId)
        {
            List<AppTransactionExDto> transactionList = new List<AppTransactionExDto>();

            if (masterWorkflowId != null)
            {
                List<int> workflowTransIdList = RetrieveOneMasterWorkflowTransactionLookUpList(masterWorkflowId).Select(o => (int)o.Id).ToList();

                foreach (int transId in workflowTransIdList)
                {
                    var aTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transId);
                    transactionList.Add(aTransactionExDto);
                }
            }

            return transactionList;
        }

        internal static OperationCallResult<bool> UpdateOneProjectTask(int taskId, AppProjectWorkFlowTaskEntity updateEntity)
        {
            if (updateEntity != null)
            {
                OperationCallResult<bool> operationCallResult = new OperationCallResult<bool>();
                ValidationResult validationResult = new ValidationResult();
                operationCallResult.ValidationResult = validationResult;

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


        private static void AddChildTransactionIds(List<int> transIdList, EntityCollection<AppTransactionEntity> list)
        {
            List<int> childTransIdList = list.Where(o => o.MasterTransactionId.HasValue && transIdList.Contains(o.MasterTransactionId.Value)).Select(o => o.TransactionId).ToList();

            if (childTransIdList.Count > 0)
            {
                AddChildTransactionIds(childTransIdList, list);
                transIdList.AddRange(childTransIdList);
            }


        }



        private static OperationCallResult<AppProjectOrWorkFlowExDto> RemovePorjectMainTaskChildTask(AppProjectOrWorkFlowExDto mainProject, int? childProjectId)
        {
            if (mainProject.DeletedItemsIds.IsEmpty())
            {
                mainProject.DeletedItemsIds = new List<object>();

            }

            AppProjectWorkFlowTaskExDto mainTaskDto = null;
            GetMainTaskDtoNode(mainProject.RootTreeList, mainProject.ExtractMainTaskId, ref mainTaskDto);

            if (mainTaskDto != null)
            {
                mainTaskDto.TimingDays = 0;
                mainTaskDto.AmountOfTime = 0;

                var childDto = mainTaskDto.Children;

                List<AppProjectWorkFlowTaskExDto> childTaksList = new List<AppProjectWorkFlowTaskExDto>();

                GetChildTaskDto(mainTaskDto, childTaksList);

                mainProject.DeletedItemsIds.AddRange(childTaksList.Select(o => o.Id).ToList());

                mainTaskDto.Children = new List<AppProjectWorkFlowTaskExDto>().ToArray();

                mainTaskDto.ProjectSectionId = childProjectId;
                var saveMainPorject = SaveProjectOrWorkFlowExDto(mainProject);

                return saveMainPorject;

            }

            else
            {
                return null;

            }
        }

        private static AppProjectOrWorkFlowExDto CreateChildProject(AppProjectOrWorkFlowExDto mainProject)
        {
            var extractCopyProject = mainProject.DeepCopy();
            AppProjectWorkFlowTaskExDto copyMainTaskDto = null;
            GetMainTaskDtoNode(extractCopyProject.RootTreeList, extractCopyProject.ExtractMainTaskId, ref copyMainTaskDto);
            AppProjectOrWorkFlowExDto extractProject = null;

            if (copyMainTaskDto != null)
            {

                extractCopyProject.Id = null;
                extractCopyProject.Name = copyMainTaskDto.Name;

                extractCopyProject.RootTreeList = copyMainTaskDto.Children;


                List<AppProjectWorkFlowTaskExDto> childTaksList = new List<AppProjectWorkFlowTaskExDto>();

                GetChildTaskDto(copyMainTaskDto, childTaksList);


                foreach (var childTask in childTaksList)
                {
                    childTask.MainTaskId = null;
                }

                //TODO !!!

                extractCopyProject.ProjectSumaryTaskId = copyMainTaskDto.Id as int?;

                extractCopyProject.ParentProjectId = mainProject.Id as int?;

                extractProject = AppProjectWorkFlowStructureBL.SaveProjectOrWorkFlowExDto(extractCopyProject).Object; ;

            }

            return extractProject;
        }

        private static void GetChildTaskDto(AppProjectWorkFlowTaskExDto taskDto, List<AppProjectWorkFlowTaskExDto> childTaksList)
        {
            if (taskDto.Children != null && taskDto.Children.Count() > 0)
            {
                foreach (var childTaskDto in taskDto.Children)
                {

                    childTaksList.Add(childTaskDto);

                    GetChildTaskDto(childTaskDto, childTaksList);
                }
            }
        }
        private static void GetMainTaskDtoNode(AppProjectWorkFlowTaskExDto[] rootTreeList, object taksId, ref AppProjectWorkFlowTaskExDto branchTaskDto)
        {


            if (rootTreeList.IsEmpty())
            {
                return;
            }
            else
            {
                foreach (var rootTakDto in rootTreeList)
                {

                    if ((int)rootTakDto.Id == (int)taksId)
                    {

                        branchTaskDto = rootTakDto;
                        return;

                    }
                    else
                    {
                        GetMainTaskDtoNode(rootTakDto.Children, taksId, ref branchTaskDto);


                    }

                }

            }

        }


        private static ValidationResult UpdateTaskResourcePlannedHours(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, AppProjectOrWorkFlowEntity orgProjectEntity, AppProjectOrWorkFlowEntity updatedProjectEntity)
        {
            ValidationResult aValidationResult = new ValidationResult();

            EntityCollection<AppProjectTaskResourcePlannedHoursEntity> needToSaveEntities = new EntityCollection<AppProjectTaskResourcePlannedHoursEntity>();

            List<int> deletHourResourceIds = new List<int>();



            foreach (var updatedTaskEntity in updatedProjectEntity.AppProjectWorkFlowTask)
            {
                var orgTaskEnity = orgProjectEntity.AppProjectWorkFlowTask.FirstOrDefault(o => o.ProjectWorkFlowTaskId == updatedTaskEntity.ProjectWorkFlowTaskId);
                bool isNewTask = orgTaskEnity == null;

                bool isPlannedDateNotChanged = !isNewTask && updatedTaskEntity.DatePlannedStart.HasValue
                                                && updatedTaskEntity.DatePlannedEnd.HasValue
                                                && orgTaskEnity.DatePlannedStart.HasValue
                                                && orgTaskEnity.DatePlannedEnd.HasValue
                                                && updatedTaskEntity.DatePlannedStart.Value == orgTaskEnity.DatePlannedStart.Value
                                                && updatedTaskEntity.DatePlannedEnd.Value == orgTaskEnity.DatePlannedEnd.Value;

                foreach (var updatedResourceEntity in updatedTaskEntity.AppProjectTaskResource)
                {
                    double totalHours = updatedResourceEntity.PlannedWorkHours.HasValue ? updatedResourceEntity.PlannedWorkHours.Value : 0;

                    AppProjectTaskResourceEntity orgResourceEntity = null;

                    if (!isNewTask)
                    {
                        orgResourceEntity = orgTaskEnity.AppProjectTaskResource.FirstOrDefault(o => o.TaskResourceId == updatedResourceEntity.TaskResourceId);
                    }

                    var resourceEntities = PrecessOneResourceHours(aAppProjectOrWorkFlowExDto, updatedTaskEntity, updatedResourceEntity, orgResourceEntity, isPlannedDateNotChanged, deletHourResourceIds);

                    needToSaveEntities.AddRange(resourceEntities);
                }
            }


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.DeleteEntitiesDirectly(typeof(AppProjectTaskResourcePlannedHoursEntity), new RelationPredicateBucket(AppProjectTaskResourcePlannedHoursFields.TaskResourceId == deletHourResourceIds.ToArray()));
                    adapter.SaveEntityCollection(needToSaveEntities);


                    adapter.Commit();




                    aValidationResult.Items.Add(new ValidationItem(typeof(AppFormEntity), "App_AppProjectOrWorkFlowEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                }

                // Database FK Exeption ........
                catch (Exception ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectOrWorkFlowExDto), "plm_AppProjectOrWorkFlowEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        internal static ValidationResult UpdateOneTaskResourcePlannedHours(AppProjectWorkFlowTaskEntity updatedTaskEntity, AppProjectWorkFlowTaskEntity orgTaskEnity)
        {
            ValidationResult aValidationResult = new ValidationResult();

            EntityCollection<AppProjectTaskResourcePlannedHoursEntity> needToSaveEntities = new EntityCollection<AppProjectTaskResourcePlannedHoursEntity>();

            List<int> deletHourResourceIds = new List<int>();

            bool isNewTask = orgTaskEnity == null;

            bool isPlannedDateNotChanged = !isNewTask && updatedTaskEntity.DatePlannedStart.HasValue
                                            && updatedTaskEntity.DatePlannedEnd.HasValue
                                            && orgTaskEnity.DatePlannedStart.HasValue
                                            && orgTaskEnity.DatePlannedEnd.HasValue
                                            && updatedTaskEntity.DatePlannedStart.Value == orgTaskEnity.DatePlannedStart.Value
                                            && updatedTaskEntity.DatePlannedEnd.Value == orgTaskEnity.DatePlannedEnd.Value;

            foreach (var updatedResourceEntity in updatedTaskEntity.AppProjectTaskResource)
            {
                double totalHours = updatedResourceEntity.PlannedWorkHours.HasValue ? updatedResourceEntity.PlannedWorkHours.Value : 0;

                AppProjectTaskResourceEntity orgResourceEntity = null;

                if (!isNewTask)
                {
                    orgResourceEntity = orgTaskEnity.AppProjectTaskResource.FirstOrDefault(o => o.TaskResourceId == updatedResourceEntity.TaskResourceId);
                }

                var resourceEntities = PrecessOneResourceHours(null, updatedTaskEntity, updatedResourceEntity, orgResourceEntity, isPlannedDateNotChanged, deletHourResourceIds);

                needToSaveEntities.AddRange(resourceEntities);
            }



            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.DeleteEntitiesDirectly(typeof(AppProjectTaskResourcePlannedHoursEntity), new RelationPredicateBucket(AppProjectTaskResourcePlannedHoursFields.TaskResourceId == deletHourResourceIds.ToArray()));
                    adapter.SaveEntityCollection(needToSaveEntities);


                    adapter.Commit();




                    aValidationResult.Items.Add(new ValidationItem(typeof(AppFormEntity), "App_AppProjectOrWorkFlowEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                }

                // Database FK Exeption ........
                catch (Exception ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectOrWorkFlowExDto), "plm_AppProjectOrWorkFlowEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }


        private static EntityCollection<AppProjectTaskResourcePlannedHoursEntity> PrecessOneResourceHours(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, AppProjectWorkFlowTaskEntity updatedTaskEntity,
                AppProjectTaskResourceEntity updatedResourceEntity, AppProjectTaskResourceEntity orgResourceEntity, bool isPlannedDateNotChanged, List<int> deletHourResourceIds)
        {
            EntityCollection<AppProjectTaskResourcePlannedHoursEntity> toReturnEntities = new EntityCollection<AppProjectTaskResourcePlannedHoursEntity>();

            bool isUpdateExistingHours = false;

            if (orgResourceEntity != null)
            {
                if (isPlannedDateNotChanged)
                {
                    isUpdateExistingHours = true;
                }
                else
                {
                    isUpdateExistingHours = false;

                    deletHourResourceIds.Add(orgResourceEntity.TaskResourceId);
                }
            }



            DateTime startDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(updatedTaskEntity.DatePlannedStart.Value).Date;
            DateTime endDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(updatedTaskEntity.DatePlannedEnd.Value).Date;

            AppCalendarExDto userCalendar = null;
            AppCalendarExDto companyCalendar = null;

            if (aAppProjectOrWorkFlowExDto != null)
            {
                AppProjectDateCaculationBL.GetProjectUserCalendarByUserId(updatedResourceEntity.UserId.Value, aAppProjectOrWorkFlowExDto, out userCalendar, out companyCalendar);
            }
            else
            {
                companyCalendar = AppCalendarBL.RetriveCompanyDefaultCalendar();
            }

            List<DateTime> userWorkDayList = new List<DateTime>();

            for (DateTime aDate = startDate; aDate <= endDate; aDate = aDate.AddDays(1))
            {
                bool isHoliday = AppCalendarBL.IsHolidayOnCalendar(companyCalendar, userCalendar, aDate);

                if (!isHoliday)
                {
                    userWorkDayList.Add(aDate);
                }
            }

            if (userWorkDayList.Count > 0)
            {
                double totalWorkHour = updatedResourceEntity.PlannedWorkHours.HasValue ? updatedResourceEntity.PlannedWorkHours.Value : 0;
                double workHourBrDay = totalWorkHour / userWorkDayList.Count;



                foreach (DateTime aWorkDay in userWorkDayList)
                {
                    int? dateId = ControlTypeValueConverter.ConvertValueToInt(aWorkDay.ToString("yyyyMMdd"));

                    if (dateId.HasValue)
                    {
                        AppProjectTaskResourcePlannedHoursEntity plannedHourentity = new AppProjectTaskResourcePlannedHoursEntity();

                        if (isUpdateExistingHours)
                        {
                            var orgResourcePlannedHourentity = orgResourceEntity.AppProjectTaskResourcePlannedHours.FirstOrDefault(o => o.DateId.HasValue && dateId.Value == o.DateId.Value);
                            if (orgResourcePlannedHourentity != null)
                            {
                                plannedHourentity = orgResourcePlannedHourentity;
                            }
                            else
                            {
                                plannedHourentity.DateId = dateId;
                                plannedHourentity.TaskResourceId = updatedResourceEntity.TaskResourceId;
                            }
                        }
                        else
                        {
                            plannedHourentity.DateId = dateId;
                            plannedHourentity.TaskResourceId = updatedResourceEntity.TaskResourceId;
                        }

                        plannedHourentity.PlannedWorkHours = workHourBrDay;

                        toReturnEntities.Add(plannedHourentity);
                    }


                }
            }

            return toReturnEntities;
        }

        private static void InitializeOneProjectAllTaskStageAndStatus(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, bool isTaskOnClientTime)
        {
            //List<AppProjectWorkFlowTaskExDto> workFlowTaskDtoList = aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList.ToList();

            DateTime clientToday = ClientTimeZoneHelper.ConvertUTCToClientDateTime(DateTime.UtcNow).Date;

            foreach (var taskDto in aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList)
            {
                if (taskDto.ProgressId.HasValue)
                {
                    if (taskDto.ProgressId.Value == (int)EmAppProjectTaskProgress.NotStarted)
                    {
                        taskDto.EmAppProjectTaskStage = EmAppProjectTaskStage.NotStarted;
                    }
                    else if (taskDto.ProgressId.Value == (int)EmAppProjectTaskProgress.Started)
                    {
                        taskDto.EmAppProjectTaskStage = EmAppProjectTaskStage.Started;
                    }
                    else if (taskDto.ProgressId.Value == (int)EmAppProjectTaskProgress.HalfwayDone)
                    {
                        taskDto.EmAppProjectTaskStage = EmAppProjectTaskStage.Started;
                    }
                    else if (taskDto.ProgressId.Value == (int)EmAppProjectTaskProgress.AlmostDone)
                    {
                        taskDto.EmAppProjectTaskStage = EmAppProjectTaskStage.Started;
                    }
                    else if (taskDto.ProgressId.Value == (int)EmAppProjectTaskProgress.Done)
                    {
                        taskDto.EmAppProjectTaskStage = EmAppProjectTaskStage.Completed;
                    }

                    taskDto.TaskProgressDisplay = ((EmAppProjectTaskProgress)taskDto.ProgressId.Value).ToString();
                    taskDto.TaskStageDisplay = taskDto.EmAppProjectTaskStage.Value.ToString();
                }


                InitialTaskStatus(taskDto, isTaskOnClientTime, clientToday);
            }
        }



        internal static void InitialTaskStatus(AppProjectWorkFlowTaskExDto taskDto, bool isTaskOnClientTime, DateTime clientToday)
        {
            taskDto.ProjectActivityStatusId = (int)EmAppProjectTaskStatus.NotAvailable;

            if (taskDto != null && taskDto.DatePlannedStart.HasValue && taskDto.DatePlannedEnd.HasValue && taskDto.AmountOfTime.HasValue && taskDto.CompletedPercent.HasValue)
            {
                if (taskDto.ProgressId.Value == (int)EmAppProjectTaskProgress.Done)
                {
                    taskDto.ProjectActivityStatusId = (int)EmAppProjectTaskStatus.Completed;
                }
                else
                {
                    DateTime client_DatePlannedStart = taskDto.DatePlannedStart.Value;
                    DateTime client_DatePlannedEnd = taskDto.DatePlannedEnd.Value;

                    if (!isTaskOnClientTime)
                    {
                        client_DatePlannedStart = ClientTimeZoneHelper.ConvertUTCToClientDateTime(taskDto.DatePlannedStart.Value);
                        client_DatePlannedEnd = ClientTimeZoneHelper.ConvertUTCToClientDateTime(taskDto.DatePlannedEnd.Value);
                    }

                    if (client_DatePlannedEnd < clientToday)
                    {
                        taskDto.ProjectActivityStatusId = (int)EmAppProjectTaskStatus.Late;
                    }
                    else if (taskDto.DatePlannedStart.Value < clientToday && taskDto.DatePlannedEnd.Value >= clientToday)
                    {

                        AppCalendarExDto userCalendar = null;
                        AppCalendarExDto companyCalendar = AppCalendarBL.RetriveCompanyDefaultCalendar();

                        double nbPlannedProgressDays = AppProjectDateCaculationBL.CalculateCalendarDurationDays(client_DatePlannedStart, clientToday, userCalendar, companyCalendar);
                        double plannedCompletePercentage = 0;

                        if (taskDto.AmountOfTime.Value > 0)
                        {
                            plannedCompletePercentage = nbPlannedProgressDays / taskDto.AmountOfTime.Value * 100;
                        }

                        if (taskDto.CompletedPercent.Value >= plannedCompletePercentage)
                        {
                            taskDto.ProjectActivityStatusId = (int)EmAppProjectTaskStatus.OnSchedule;
                        }
                        else
                        {
                            taskDto.ProjectActivityStatusId = (int)EmAppProjectTaskStatus.AtRisk;
                        }

                    }
                }
            }

            taskDto.TaskStatusDisplay = ((EmAppProjectTaskStatus)taskDto.ProjectActivityStatusId.Value).ToString();
        }

    }


}


