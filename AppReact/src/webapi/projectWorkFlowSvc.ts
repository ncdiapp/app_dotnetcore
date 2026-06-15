import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';
class ProjectWorkflowService {

  async retrieveCurrentUserAppointmentList(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveCurrentUserAppointmentList`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve current user appointment list');
    return response.json();
  }

  async saveOnePdmUserAppointmentEntityDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/SaveOnePdmUserAppointmentEntityDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save user appointment');
    return response.json();
  }

  async deleteOnePdmUserAppointmentEntityDto(id: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/DeleteOnePdmUserAppointmentEntityDto?id=${id || ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete user appointment');
    return response.json();
  }

  async initializeAppointmentInfo(aPdmUserAppointmentExDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/InitializeAppointmentInfo`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(aPdmUserAppointmentExDto)
    });
    if (!response.ok) throw new Error('Failed to initialize appointment info');
    return response.json();
  }

  async RetrieveAppProjectOrWorkFlows(projectOrWorflowType: any, isPredefined: any, isHierarchy: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveAppProjectOrWorkFlows?projectOrWorflowType=${projectOrWorflowType || ''}&isPredefined=${isPredefined || ''}&isHierarchy=${isHierarchy || ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app project or workflows');
    return response.json();
  }

  async RetrieveOneAppProjectOrWorkFlowExDto(proejctId: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveOneAppProjectOrWorkFlowExDto?proejctId=${proejctId || ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve project or workflow');
    return response.json();
  }

  async CreateDefaultWorkflowFromTransaction(transactionId: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/CreateDefaultWorkflowFromTransaction?transactionId=${transactionId || ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to create default workflow from transaction');
    return response.json();
  }

  async StartOneProject(aAppProjectOrWorkFlowExDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/StartOneProject`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(aAppProjectOrWorkFlowExDto)
    });
    if (!response.ok) throw new Error('Failed to start project');
    return response.json();
  }

  async SaveProjectOrWorkFlowExDto(aAppProjectOrWorkFlowExDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/SaveProjectOrWorkFlowExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(aAppProjectOrWorkFlowExDto)
    });
    if (!response.ok) throw new Error('Failed to save project or workflow');
    return response.json();
  }

  async DeleteProjectWorkFlow(proejctId: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/DeleteProjectWorkFlow?proejctId=${proejctId || ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete project workflow');
    return response.json();
  }

  async calculateProjectOrWorkFlowTaskDates(aAppProjectOrWorkFlowExDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/CalculateProjectOrWorkFlowTaskDates`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(aAppProjectOrWorkFlowExDto)
    });
    if (!response.ok) throw new Error('Failed to calculate project or workflow task dates');
    return response.json();
  }

  async CalculateProjectOrWorkFlow(aAppProjectOrWorkFlowExDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/CalculateProjectOrWorkFlow`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(aAppProjectOrWorkFlowExDto)
    });
    if (!response.ok) throw new Error('Failed to calculate project or workflow');
    return response.json();
  }

  async CheckCircularProjectPredecessor(aAppProjectTaskCircularCheckDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/CheckCircularProjectPredecessor`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(aAppProjectTaskCircularCheckDto)
    });
    if (!response.ok) throw new Error('Failed to check circular project predecessor');
    return response.json();
  }

  async CheckCircularOneProjectActivityPredecessor(aAppProjectTaskCircularCheckDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/CheckCircularOneProjectActivityPredecessor`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(aAppProjectTaskCircularCheckDto)
    });
    if (!response.ok) throw new Error('Failed to check circular project activity predecessor');
    return response.json();
  }

  async InitializeBPMWorkFlowExDto(aAppProjectOrWorkFlowExDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/InitializeBPMWorkFlowExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(aAppProjectOrWorkFlowExDto)
    });
    if (!response.ok) throw new Error('Failed to initialize BPM workflow');
    return response.json();
  }

  async GetTransactionProjectWorkFlowTemplateList(transactionId: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/GetTransactionProjectWorkFlowTemplateList?transactionId=${transactionId || ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get transaction project workflow template list');
    return response.json();
  }

  async RetrieveProjectSettingExDto(projectId: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveProjectSettingExDto?projectId=${projectId || ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve project setting');
    return response.json();
  }

  async SaveProjectSettingExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/SaveProjectSettingExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save project setting');
    return response.json();
  }

  async RetrieveAllAppProjectTeamDto(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveAllAppProjectTeamDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve all project teams');
    return response.json();
  }

  async RetrieveProjectTeamExDto(teamId: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveProjectTeamExDto?teamId=${teamId || ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve project team');
    return response.json();
  }

  async SaveAppProjectTeamExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/SaveAppProjectTeamExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save project team');
    return response.json();
  }

  async RetrieveOneAppProjectRoleExDto(projectRoleId: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveOneAppProjectRoleExDto?projectRoleId=${projectRoleId || ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve project role');
    return response.json();
  }

  async DeleteOneAppProjectRole(projectRoleId: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/DeleteOneAppProjectRole?projectRoleId=${projectRoleId || ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete project role');
    return response.json();
  }

  async RetrieveAllAppProjectPrivilegeLibraryDto(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveAllAppProjectPrivilegeLibraryDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve all project privilege library');
    return response.json();
  }

  async RetrieveAllAppProjectRoleEntityDto(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveAllAppProjectRoleEntityDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve all project roles');
    return response.json();
  }

  async SaveAppProjectRoleExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/SaveAppProjectRoleExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save project role');
    return response.json();
  }

  async ImportProjectTeamFromLibary(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/ImportProjectTeamFromLibary`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to import project team from library');
    return response.json();
  }

  async RetrieveOneTaskTimeSheetDtoList(projectWorkFlowTaskID: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveOneTaskTimeSheetDtoList?projectWorkFlowTaskID=${projectWorkFlowTaskID || ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve task timesheet list');
    return response.json();
  }

  async SaveOnePorjectTaskAllTimeSheet(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/SaveOnePorjectTaskAllTimeSheet`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save project task timesheet');
    return response.json();
  }

  async RetrieveOneTaskCheckListDtoList(projectWorkFlowTaskID: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveOneTaskCheckListDtoList?projectWorkFlowTaskID=${projectWorkFlowTaskID || ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve task checklist list');
    return response.json();
  }

  async SaveAllAppProjectTaskCheckListEntityDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/SaveAllAppProjectTaskCheckListEntityDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save project task checklist');
    return response.json();
  }

  async ProjectTaskChanged(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/ProjectTaskChanged`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to update project task');
    return response.json();
  }

  async CalculateProjectCriticalPath(aAppProjectOrWorkFlowExDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/CalculateProjectCriticalPath`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(aAppProjectOrWorkFlowExDto)
    });
    if (!response.ok) throw new Error('Failed to calculate project critical path');
    return response.json();
  }

  async CreateProjectDefaultForm(aAppProjectOrWorkFlowExDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/CreateProjectDefaultForm`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(aAppProjectOrWorkFlowExDto)
    });
    if (!response.ok) throw new Error('Failed to create project default form');
    return response.json();
  }

  async CreateProjectTaskDefaultForm(aAppProjectOrWorkFlowExDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/CreateProjectTaskDefaultForm`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(aAppProjectOrWorkFlowExDto)
    });
    if (!response.ok) throw new Error('Failed to create project task default form');
    return response.json();
  }

  async RetrieveUserTaskList(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveUserTaskList`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to retrieve user task list');
    return response.json();
  }

  async CompleteOneTask(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/CompleteOneTask`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to complete task');
    return response.json();
  }

  async UnCompleteOneTask(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/UnCompleteOneTask`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to uncomplete task');
    return response.json();
  }

  async UpdateOneTaskOwnerDeliverPhase(taskId: any, emAppTaskOwnerDeliverPhase: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/UpdateOneTaskOwnerDeliverPhase?taskId=${taskId || ''}&emAppTaskOwnerDeliverPhase=${emAppTaskOwnerDeliverPhase || ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to update task owner deliver phase');
    return response.json();
  }

  async ReassignOneTask(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/ReassignOneTask`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to reassign task');
    return response.json();
  }

  async SaveOneAppProjectWorkFlowTaskExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/SaveOneAppProjectWorkFlowTaskExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save project workflow task');
    return response.json();
  }

  async DeleteOneStandAloneTask(taskId: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/DeleteOneStandAloneTask?taskId=${taskId || ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete standalone task');
    return response.json();
  }

  async RetrieveAllMasterWorkFlows(isPredefined: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveAllMasterWorkFlows?isPredefined=${isPredefined || ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve all master workflows');
    return response.json();
  }

  async RetrieveOneWorkflowExDtoWithAllChildWorkflowList(workflowId: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveOneWorkflowExDtoWithAllChildWorkflowList?workflowId=${workflowId || ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve workflow with child workflow list');
    return response.json();
  }

  async SaveOneWorkflowAllChildWorkflowList(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/SaveOneWorkflowAllChildWorkflowList`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save workflow child workflow list');
    return response.json();
  }

  async UpdateWorkflowParent(workflowId: any, parentId: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/UpdateWorkflowParent?workflowId=${workflowId || ''}&parentId=${parentId || ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to update workflow parent');
    return response.json();
  }

  async RetrieveOneMasterWorkflowTransactionList(masterWorkflowId: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveOneMasterWorkflowTransactionList?masterWorkflowId=${masterWorkflowId || ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve master workflow transaction list');
    return response.json();
  }

  async ExtractMainTaskToChildPorject(aAppProjectOrWorkFlowExDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/ExtractMainTaskToChildPorject`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(aAppProjectOrWorkFlowExDto)
    });
    if (!response.ok) throw new Error('Failed to extract main task to child project');
    return response.json();
  }

  async RetriveOneProjectAllTeamMembers(projectId: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetriveOneProjectAllTeamMembers?projectId=${projectId || ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve project all team members');
    return response.json();
  }

  async RetrieveAllAppProjectPortfolioDto(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveAllAppProjectPortfolioDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve all project portfolios');
    return response.json();
  }

  async RetrieveOneAppProjectPortfolioExDto(portfolioId: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveOneAppProjectPortfolioExDto?portfolioId=${portfolioId || ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve project portfolio');
    return response.json();
  }

  async SaveAppProjectPortfolioExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/SaveAppProjectPortfolioExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save project portfolio');
    return response.json();
  }

  async DeleteOneAppProjectPortfolio(portfolioId: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/DeleteOneAppProjectPortfolio?portfolioId=${portfolioId || ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete project portfolio');
    return response.json();
  }

  async RetrieveProjectWorkload(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveProjectWorkload`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to retrieve project workload');
    return response.json();
  }

  async SaveProjectWorkload(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/SaveProjectWorkload`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save project workload');
    return response.json();
  }

  async RetrieveOneProjectTaskUsersAvailableHours(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveOneProjectTaskUsersAvailableHours`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to retrieve project task users available hours');
    return response.json();
  }

  async RetrieveOneTaskAllUsersAvailableHours(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveOneTaskAllUsersAvailableHours`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to retrieve task all users available hours');
    return response.json();
  }

  async RetrieveOneProjectDashboardDto(projectId: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveOneProjectDashboardDto?projectId=${projectId || ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve project dashboard');
    return response.json();
  }

  async RetrieveCurrentUserTaskKanbanDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveCurrentUserTaskKanbanDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to retrieve current user task kanban');
    return response.json();
  }

  async SaveCurrentUserTaskKanbanDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/SaveCurrentUserTaskKanbanDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save current user task kanban');
    return response.json();
  }
}

export const projectWorkflowService = new ProjectWorkflowService();
