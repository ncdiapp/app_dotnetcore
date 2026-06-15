using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using APP.Components.Dto;
using APP.Components.EntityDto;
using App.BL;
using APP.Framework.Collections;
using APP.Framework.Communication;
using Microsoft.AspNetCore.Mvc;
using AppAI.Web.Controllers.Base;

namespace AppAI.Web.Controllers;

[Route("webapi/[controller]/[action]")]
public class ProjectWorkFlowController : SecureBaseController
{
    [HttpGet]
    public List<AppProjectOrWorkFlowDto> RetrieveAppProjectOrWorkFlows(int? projectOrWorflowType, bool? isPredefined, bool? isHierarchy)
    {
        if (projectOrWorflowType.HasValue)
        {
            isHierarchy = isHierarchy.HasValue ? isHierarchy.Value : false;
            return AppProjectWorkFlowStructureBL.RetrieveAppProjectOrWorkFlows((EmAppProjectWorkflowType)projectOrWorflowType.Value, isPredefined, isHierarchy.Value);
        }
        return null;
    }

    [HttpGet]
    public List<AppProjectOrWorkFlowDto> RetrieveAppProjectList(bool? isPredefined)
    {
        return AppProjectSettingBL.RetrieveAppProjectList(isPredefined);
    }

    [HttpGet]
    public List<AppProjectOrWorkFlowDto> RetrieveWorkFlowList(int? projectOrWorflowType, bool? isPredefined)
    {
        if (projectOrWorflowType.HasValue)
        {
            return AppProjectWorkFlowStructureBL.RetrieveAppProjectOrWorkFlows((EmAppProjectWorkflowType)projectOrWorflowType.Value, isPredefined, false);
        }
        return null;
    }

    [HttpPost]
    public OperationCallResult<AppProjectOrWorkFlowExDto> ExtractMainTaskToChildPorject(AppProjectOrWorkFlowExDto mainProject)
    {
        return AppProjectWorkFlowStructureBL.ExtractMainTaskToChildPorject(mainProject);
    }

    [HttpGet]
    public AppProjectOrWorkFlowExDto RetrieveOneAppProjectOrWorkFlowExDto(int? proejctId)
    {
        var aAppProjectOrWorkFlowExDto = AppProjectWorkFlowStructureBL.RetrieveOneAppProjectOrWorkFlowExDto(proejctId);

        aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList = null;

        return aAppProjectOrWorkFlowExDto;
    }

    [HttpGet]
    public OperationCallResult<AppProjectOrWorkFlowExDto> CreateDefaultWorkflowFromTransaction(int? transactionId)
    {
        if (transactionId.HasValue)
        {
            return AppProjectWorkFlowStructureBL.CreateDefaultWorkflowFromTransaction(transactionId.Value);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppProjectOrWorkFlowExDto> StartOneProject(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
    {
        if (aAppProjectOrWorkFlowExDto != null)
        {
            //  return AppProjectWorkFlowStructureBL.StartOneProject(aAppProjectOrWorkFlowExDto);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppProjectOrWorkFlowExDto> SaveProjectOrWorkFlowExDto(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
    {
        if (aAppProjectOrWorkFlowExDto.DeletedItemsIds != null)
        {
            aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList.DeletedItemIds = aAppProjectOrWorkFlowExDto.DeletedItemsIds;
        }

        if (aAppProjectOrWorkFlowExDto.DictDeletedItemsIds != null)
        {
            foreach (var taskDto in aAppProjectOrWorkFlowExDto.RootTreeList)
            {
                ProcessProjectTaskDeletedItems(aAppProjectOrWorkFlowExDto, taskDto);
            }

            if (aAppProjectOrWorkFlowExDto.DictDeletedItemsIds.ContainsKey("AppProjectWorkFlowConditionList"))
            {
                aAppProjectOrWorkFlowExDto.AppProjectWorkFlowConditionList.DeletedItemIds = aAppProjectOrWorkFlowExDto.DictDeletedItemsIds["AppProjectWorkFlowConditionList"];
                aAppProjectOrWorkFlowExDto.IsModified = true;
            }

            foreach (var conditionDto in aAppProjectOrWorkFlowExDto.AppProjectWorkFlowConditionList)
            {
                if (conditionDto.Id != null)
                {
                    if (aAppProjectOrWorkFlowExDto.DictDeletedItemsIds.ContainsKey("AppProjectWorkFlowActionList" + "_" + conditionDto.Id.ToString()))
                    {
                        conditionDto.AppProjectWorkFlowActionList.DeletedItemIds = aAppProjectOrWorkFlowExDto.DictDeletedItemsIds["AppProjectWorkFlowActionList" + "_" + conditionDto.Id.ToString()];
                        conditionDto.IsModified = true;
                        aAppProjectOrWorkFlowExDto.IsModified = true;
                    }
                }
            }
        }

        return AppProjectWorkFlowStructureBL.SaveProjectOrWorkFlowExDto(aAppProjectOrWorkFlowExDto);
    }

    private static void ProcessProjectTaskDeletedItems(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, AppProjectWorkFlowTaskExDto taskDto)
    {
        if (taskDto.Id != null)
        {
            if (aAppProjectOrWorkFlowExDto.DictDeletedItemsIds.ContainsKey("PredecessorIdList_" + taskDto.Id.ToString()))
            {
                taskDto.AppProjectTaskPredecessorList.DeletedItemIds = aAppProjectOrWorkFlowExDto.DictDeletedItemsIds["PredecessorIdList_" + taskDto.Id.ToString()];
                taskDto.IsModified = true;
            }

            if (aAppProjectOrWorkFlowExDto.DictDeletedItemsIds.ContainsKey("AppProjectTaskResourceList_" + taskDto.Id.ToString()))
            {
                taskDto.AppProjectTaskResourceList.DeletedItemIds = aAppProjectOrWorkFlowExDto.DictDeletedItemsIds["AppProjectTaskResourceList_" + taskDto.Id.ToString()];
                taskDto.IsModified = true;
            }

            ProcessChildTaskDelteItems(aAppProjectOrWorkFlowExDto, taskDto);
        }
    }

    private static void ProcessChildTaskDelteItems(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, AppProjectWorkFlowTaskExDto taskDto)
    {
        if (taskDto.Children != null && taskDto.Children.Count() > 0)
        {
            foreach (var childTaskDto in taskDto.Children)
            {
                ProcessProjectTaskDeletedItems(aAppProjectOrWorkFlowExDto, childTaskDto);
            }
        }
    }

    [HttpGet]
    public OperationCallResult<bool> DeleteProjectWorkFlow(int? proejctId)
    {
        return AppProjectWorkFlowStructureBL.DeleteProjectWorkFlow(proejctId);
    }

    [HttpPost]
    public OperationCallResult<AppProjectOrWorkFlowExDto> CalculateProjectOrWorkFlowTaskDates(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
    {
        return AppProjectDateCaculationBL.CalculateProjectOrWorkFlowTaskDates(aAppProjectOrWorkFlowExDto);
    }

    [HttpPost]
    public OperationCallResult<AppProjectOrWorkFlowExDto> CalculateProjectOrWorkFlow(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
    {
        return AppProjectDateCaculationBL.CalculateProjectOrWorkFlow(aAppProjectOrWorkFlowExDto);
    }

    [HttpPost]
    public AppProjectTaskCircularCheckDto CheckCircularProjectPredecessor(AppProjectTaskCircularCheckDto aAppProjectTaskCircularCheckDto)
    {
        aAppProjectTaskCircularCheckDto.LockingTask = AppProjectDateCaculationBL.CheckCircularProjectPredecessor(aAppProjectTaskCircularCheckDto.AppProjectOrWorkFlowExDto, aAppProjectTaskCircularCheckDto.CircularPath);

        aAppProjectTaskCircularCheckDto.AppProjectOrWorkFlowExDto = null;

        return aAppProjectTaskCircularCheckDto;
    }

    [HttpPost]
    public AppProjectTaskCircularCheckDto CheckCircularOneProjectActivityPredecessor(object jsonObj)
    {
        AppProjectTaskCircularCheckDto aAppProjectTaskCircularCheckDto = JsonConvert.DeserializeObject<AppProjectTaskCircularCheckDto>(jsonObj.ToString());

        aAppProjectTaskCircularCheckDto.LockingTask = AppProjectDateCaculationBL.CheckCircularOneProjectActivityPredecessor(aAppProjectTaskCircularCheckDto.NeedToCheckTask,
                aAppProjectTaskCircularCheckDto.AppProjectOrWorkFlowExDto, aAppProjectTaskCircularCheckDto.CircularPath);

        aAppProjectTaskCircularCheckDto.AppProjectOrWorkFlowExDto = null;

        return aAppProjectTaskCircularCheckDto;
    }

    [HttpPost]
    public AppProjectOrWorkFlowExDto InitializeBPMWorkFlowExDto(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
    {
        if (aAppProjectOrWorkFlowExDto != null && aAppProjectOrWorkFlowExDto.ProjectWorkflowType.HasValue && aAppProjectOrWorkFlowExDto.ProjectWorkflowType.Value == (int)EmAppProjectWorkflowType.BusinessProcessWorkflow)
        {
            AppProjectWorkFlowStructureBL.InitializeActivityPredecessorsAndSuccessors(aAppProjectOrWorkFlowExDto);
        }

        return aAppProjectOrWorkFlowExDto;
    }

    [HttpGet]
    public List<AppProjectOrWorkFlowDto> GetTransactionProjectWorkFlowTemplateList(int? transactionId)
    {
        if (transactionId.HasValue)
        {
            return AppProjectWorkFlowProcessBL.GetTransactionProjectWorkFlowTemplateList(transactionId.Value);
        }

        return new List<AppProjectOrWorkFlowDto>();
    }

    [HttpGet]
    public AppProjectOrWorkFlowExDto RetrieveProjectSettingExDto(int? projectId)
    {
        if (projectId.HasValue)
        {
            return AppProjectSettingBL.RetrieveProjectSettingExDto(projectId.Value);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppProjectOrWorkFlowExDto> SaveProjectSettingExDto(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
    {
        if (aAppProjectOrWorkFlowExDto != null)
        {
            return AppProjectSettingBL.SaveProjectSettingExDto(aAppProjectOrWorkFlowExDto);
        }

        return null;
    }

    [HttpGet]
    public List<AppProjectTeamDto> RetrieveAllAppProjectTeamDto()
    {
        return AppProjectTeamLibBL.RetrieveAllAppProjectTeamDto();
    }

    [HttpGet]
    public AppProjectTeamExDto RetrieveProjectTeamExDto(int? teamId)
    {
        if (teamId.HasValue)
        {
            return AppProjectTeamLibBL.RetrieveProjectTeamExDto(teamId.Value);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppProjectTeamExDto> SaveAppProjectTeamExDto(AppProjectTeamExDto aAppProjectTeamExDto)
    {
        if (aAppProjectTeamExDto != null)
        {
            return AppProjectTeamLibBL.SaveAppProjectTeamExDto(aAppProjectTeamExDto);
        }

        return null;
    }

    [HttpGet]
    public ObservableSet<AppProjectPrivilegeLibraryDto> RetrieveAllAppProjectPrivilegeLibraryDto()
    {
        return AppProjectRoleBL.RetrieveAllAppProjectPrivilegeLibraryDto();
    }

    [HttpGet]
    public ObservableSet<AppProjectRoleDto> RetrieveAllAppProjectRoleEntityDto()
    {
        return AppProjectRoleBL.RetrieveAllAppProjectRoleDto();
    }

    [HttpPost]
    public OperationCallResult<AppProjectRoleExDto> SaveAppProjectRoleExDto(AppProjectRoleExDto projectRoleSetDto)
    {
        return AppProjectRoleBL.SaveAppProjectRoleExDto(projectRoleSetDto);
    }

    [HttpGet]
    public AppProjectRoleExDto RetrieveOneAppProjectRoleExDto(int? projectRoleId)
    {
        if (projectRoleId.HasValue)
        {
            return AppProjectRoleBL.RetrieveOneAppProjectRoleExDto(projectRoleId.Value);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<object> DeleteOneAppProjectRole(int? projectRoleId)
    {
        if (projectRoleId.HasValue)
        {
            return AppProjectRoleBL.DeleteOneAppProjectRole(projectRoleId.Value);
        }

        return null;
    }

    [HttpPost]
    public AppProjectOrWorkFlowExDto ImportProjectTeamFromLibary(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
    {
        if (aAppProjectOrWorkFlowExDto != null && aAppProjectOrWorkFlowExDto.ImportProjectTeamLibaryId.HasValue)
        {
            int? projectTeamId = aAppProjectOrWorkFlowExDto.ImportProjectTeamLibaryId;
            aAppProjectOrWorkFlowExDto.ImportProjectTeamLibaryId = null;

            return AppProjectSettingBL.ImportProjectTeamFromLibary(aAppProjectOrWorkFlowExDto, projectTeamId);
        }

        return null;
    }

    [HttpGet]
    public AppProjectWorkFlowTaskExDto RetrieveOneTaskTimeSheetDtoList(int? projectWorkFlowTaskID)
    {
        if (projectWorkFlowTaskID.HasValue)
        {
            return AppPorjectWorkFlowTaskTimeSheetBL.RetrieveOneTaskTimeSheetDtoList(projectWorkFlowTaskID.Value);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppProjectWorkFlowTaskExDto> SaveOnePorjectTaskAllTimeSheet(AppProjectWorkFlowTaskExDto taskDto)
    {
        if (taskDto != null && taskDto.Id != null && taskDto.AppPorjectWorkFlowTaskTimeSheetList != null)
        {
            return AppPorjectWorkFlowTaskTimeSheetBL.SaveOnePorjectTaskAllTimeSheet(taskDto);
        }

        return null;
    }

    [HttpGet]
    public List<AppProjectTaskCheckListExDto> RetrieveOneTaskCheckListDtoList(int? projectWorkFlowTaskID)
    {
        if (projectWorkFlowTaskID.HasValue)
        {
            return AppProjectTaskCheckListBL.RetrieveOneTaskCheckListDtoList(projectWorkFlowTaskID.Value);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppProjectTaskCheckListExDto> SaveAllAppProjectTaskCheckListEntityDto(AppProjectWorkFlowTaskExDto taskDto)
    {
        if (taskDto != null && taskDto.Id != null && taskDto.AppProjectTaskCheckListList != null)
        {
            return AppProjectTaskCheckListBL.SaveAllAppProjectTaskCheckListEntityDto(taskDto.AppProjectTaskCheckListList, (int)taskDto.Id);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppProjectOrWorkFlowExDto> ProjectTaskChanged(ProjectTaskChangeDto taskChangeDto)
    {
        if (taskChangeDto != null && taskChangeDto.Project != null && taskChangeDto.Task != null)
        {
            return AppProjectDateCaculationBL.ProjectTaskChanged(taskChangeDto.Task, taskChangeDto.Project, taskChangeDto.ChangeType, taskChangeDto.IsNeedToRecalculateProject);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppProjectOrWorkFlowExDto> CalculateProjectCriticalPath(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
    {
        return AppProjectDateCaculationBL.CalculateProjectCriticalPath(aAppProjectOrWorkFlowExDto);
    }

    [HttpPost]
    public OperationCallResult<object> CreateProjectDefaultForm(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
    {
        return AppProjectWorkFlowStructureBL.CreateProjectDefaultForm(aAppProjectOrWorkFlowExDto);
    }

    [HttpPost]
    public OperationCallResult<object> CreateProjectTaskDefaultForm(AppProjectWorkFlowTaskExDto aTaskExDto)
    {
        return AppProjectWorkFlowStructureBL.CreateProjectTaskDefaultForm(aTaskExDto);
    }

    [HttpPost]
    public List<AppProjectWorkFlowTaskExDto> RetrieveUserTaskList(TaskFilterOptionDto filterOption)
    {
        if (filterOption != null)
        {
            return AppUserTaskListBL.RetrieveUserTaskList(filterOption, filterOption.IsCurrentUserTaskOnly);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<bool> CompleteOneTask(AppProjectWorkFlowTaskDto taskDto)
    {
        if (taskDto != null)
        {
            OperationCallResult<bool> operationCallResult = AppUserTaskListBL.CompleteOneTask(taskDto);

            return operationCallResult;
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<bool> UnCompleteOneTask(AppProjectWorkFlowTaskDto taskDto)
    {
        if (taskDto != null)
        {
            OperationCallResult<bool> operationCallResult = AppUserTaskListBL.UnCompleteOneTask(taskDto);

            return operationCallResult;
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<bool> UpdateOneTaskOwnerDeliverPhase(int? taskId, int? emAppTaskOwnerDeliverPhase)
    {
        if (taskId.HasValue)
        {
            if (!emAppTaskOwnerDeliverPhase.HasValue)
            {
                emAppTaskOwnerDeliverPhase = (int)EmAppTaskOwnerDeliverPhase.New;
            }

            OperationCallResult<bool> operationCallResult = AppUserTaskListBL.UpdateOneTaskOwnerDeliverPhase(taskId.Value, emAppTaskOwnerDeliverPhase);

            return operationCallResult;
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<bool> ReassignOneTask(AppProjectWorkFlowTaskDto taskDto)
    {
        if (taskDto != null)
        {
            return AppUserTaskListBL.ReassignOneTask(taskDto);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppProjectWorkFlowTaskExDto> SaveOneAppProjectWorkFlowTaskExDto(AppProjectWorkFlowTaskExDto aTaskExDto)
    {
        if (aTaskExDto != null)
        {
            return AppUserTaskListBL.SaveOneAppProjectWorkFlowTaskExDto(aTaskExDto);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<bool> DeleteOneStandAloneTask(int? taskId)
    {
        if (taskId.HasValue)
        {
            return AppUserTaskListBL.DeleteOneStandAloneTask(taskId.Value);
        }

        return null;
    }

    [HttpGet]
    public List<AppProjectOrWorkFlowDto> RetrieveAllMasterWorkFlows(bool? isPredefined)
    {
        return AppWorkFlowMasterBL.RetrieveAllMasterWorkFlows(isPredefined);
    }

    [HttpGet]
    public AppProjectOrWorkFlowExDto RetrieveOneWorkflowExDtoWithAllChildWorkflowList(int? workflowId)
    {
        if (workflowId.HasValue)
        {
            return AppWorkFlowMasterBL.RetrieveOneWorkflowExDtoWithAllChildWorkflowList(workflowId.Value);
        }
        return null;
    }

    [HttpPost]
    public OperationCallResult<AppProjectOrWorkFlowExDto> SaveOneWorkflowAllChildWorkflowList(AppProjectOrWorkFlowExDto masterWorkflowExDto)
    {
        if (masterWorkflowExDto != null)
        {
            return AppWorkFlowMasterBL.SaveOneWorkflowAllChildWorkflowList(masterWorkflowExDto);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<bool> UpdateWorkflowParent(int? workflowId, int? parentId)
    {
        if (workflowId.HasValue)
        {
            return AppWorkFlowMasterBL.UpdateWorkflowParent(workflowId.Value, parentId);
        }

        return null;
    }

    [HttpGet]
    public List<AppTransactionExDto> RetrieveOneMasterWorkflowTransactionList(int? masterWorkflowId)
    {
        if (masterWorkflowId.HasValue)
        {
            return AppProjectWorkFlowStructureBL.RetrieveOneMasterWorkflowTransactionList(masterWorkflowId.Value);
        }

        return null;
    }

    [HttpGet]
    public List<AppProjectTeamMemberDto> RetriveOneProjectAllTeamMembers(int? projectId)
    {
        return AppProjectWorkFlowStructureBL.RetriveOneProjectAllTeamMembers(projectId);
    }

    [HttpGet]
    public ObservableSet<AppProjectPortfolioDto> RetrieveAllAppProjectPortfolioDto()
    {
        return AppProjectPortfolioBL.RetrieveAllAppProjectPortfolioDto();
    }

    [HttpGet]
    public AppProjectPortfolioExDto RetrieveOneAppProjectPortfolioExDto(int? portfolioId)
    {
        if (portfolioId.HasValue)
        {
            return AppProjectPortfolioBL.RetrieveOneAppProjectPortfolioExDto(portfolioId.Value);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppProjectPortfolioExDto> SaveAppProjectPortfolioExDto(AppProjectPortfolioExDto aAppProjectPortfolioExDto)
    {
        if (aAppProjectPortfolioExDto != null)
        {
            return AppProjectPortfolioBL.SaveAppProjectPortfolioExDto(aAppProjectPortfolioExDto);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<object> DeleteOneAppProjectPortfolio(int? portfolioId)
    {
        if (portfolioId.HasValue)
        {
            return AppProjectPortfolioBL.DeleteOneAppProjectPortfolio(portfolioId.Value);
        }

        return null;
    }

    [HttpPost]
    public AppProjectWorkloadPivotDto RetrieveProjectWorkload(AppProjectWorkloadInputParameterDto parameterDto)
    {
        if (parameterDto != null)
        {
            return AppProjectWorkloadBL.RetrieveProjectWorkload(parameterDto);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<bool> SaveProjectWorkload(AppProjectWorkloadPivotDto workloadPivotDto)
    {
        if (workloadPivotDto != null)
        {
            return AppProjectWorkloadBL.SaveProjectWorkload(workloadPivotDto);
        }

        return null;
    }

    [HttpPost]
    public Dictionary<int, double> RetrieveOneProjectTaskUsersAvailableHours(AppProjectWorkFlowTaskExDto taskDto)
    {
        if (taskDto != null && taskDto.ForeignAppProjectOrWorkFlowExDto != null)
        {
            if (taskDto.ForeignAppProjectOrWorkFlowExDto.AppProjectTeamMemberList != null)
            {
                var userIdList = taskDto.ForeignAppProjectOrWorkFlowExDto.AppProjectTeamMemberList.Where(o => o.UserId.HasValue).Select(o => o.UserId.Value).Distinct().ToList();

                var toReturn = AppProjectWorkloadBL.RetrieveOneProjectTaskUsersAvailableHours(userIdList, taskDto, taskDto.ForeignAppProjectOrWorkFlowExDto);

                return toReturn;
            }
        }

        return null;
    }

    [HttpPost]
    public Dictionary<int, double> RetrieveOneTaskAllUsersAvailableHours(AppProjectWorkFlowTaskExDto taskDto)
    {
        if (taskDto != null)
        {
            var toReturn = AppProjectWorkloadBL.RetrieveOneTaskAllUsersAvailableHours(taskDto);

            return toReturn;
        }

        return null;
    }

    [HttpGet]
    public ProjectDashboardDto RetrieveOneProjectDashboardDto(int? projectId)
    {
        if (projectId.HasValue)
        {
            return AppProjectDashboardBL.RetrieveOneProjectDashboardDto(projectId.Value);
        }

        return null;
    }

    [HttpPost]
    public UserTaskKanbanDto RetrieveCurrentUserTaskKanbanDto(TaskFilterOptionDto filterOption)
    {
        return AppProjectPerspectiveViewBL.RetrieveCurrentUserTaskKanbanDto(filterOption);
    }

    [HttpPost]
    public OperationCallResult<bool> SaveCurrentUserTaskKanbanDto(UserTaskKanbanDto kanbanDto)
    {
        if (kanbanDto != null)
        {
            return AppProjectPerspectiveViewBL.SaveCurrentUserTaskKanbanDto(kanbanDto);
        }

        return null;
    }
}
