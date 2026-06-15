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
    public static class AppProjectDashboardBL
    {


        public static ProjectDashboardDto RetrieveOneProjectDashboardDto(object projectId)
        {
            AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto = AppProjectWorkFlowStructureBL.RetrieveOneAppProjectOrWorkFlowExDto(projectId, false, true);

            if (aAppProjectOrWorkFlowExDto != null)
            {
                ProjectDashboardDto projectDashboardDto = new ProjectDashboardDto();

                DateTime clientToday = ClientTimeZoneHelper.ConvertUTCToClientDateTime(DateTime.UtcNow).Date;

                int totalNbTasks = aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList.Count;
                int nbNotStartedTasks = 0;
                int nbCompletedTasks = 0;
                int nbInProgressTasks = 0;
                int nbOverdueTasks = 0;


                double nbTotalTaskPlannedCompletedDays = 0;
                double nbTotalTaskActualCompletedDays = 0;
                double nbTotalTaskDays = 0;

                projectDashboardDto.ProjectTaskList = new List<ProjectDashboardTaskDto>();
                projectDashboardDto.ProjectUserList = new List<ProjectDashboardUserDto>();

                List<LookupItemDto> userLookupItems = AppEntityInfoBL.GetLookupItemListByCode(EmAppEntityLookupInfoCode.AppSecurityUser.ToString(), "");
                Dictionary<int, string> dictUserIdName = userLookupItems.ToDictionary(o => (int)o.Id, o => o.Display);

                Dictionary<int, ProjectDashboardUserDto> dictUserIdAndDashboardUserDto = new Dictionary<int, ProjectDashboardUserDto>();

                foreach (var taskDto in aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList)
                {


                    bool isTaskOverDue = false;
                    bool isTaskCompleted = false;

                    double nbCurrentTaskPlannedCompletedDays = 0;
                    double nbCurrentTaskActualCompletedDays = 0;

                    if (taskDto.DatePlannedStart.HasValue && taskDto.DatePlannedEnd.HasValue && taskDto.AmountOfTime.HasValue)
                    {
                        nbTotalTaskDays += taskDto.AmountOfTime.Value;

                        if (taskDto.DatePlannedEnd.Value < clientToday)
                        {
                            nbCurrentTaskPlannedCompletedDays = taskDto.AmountOfTime.Value;

                            if (!(taskDto.ProgressId.HasValue && taskDto.ProgressId.Value == (int)EmAppProjectTaskProgress.Done))
                            {
                                nbOverdueTasks++;
                                isTaskOverDue = true;
                                //currentTaskUserIdList.ForAll(o => { dictUserIdAndDashboardUserDto[o].NbOverdueTasks++; });

                            }
                        }
                        else if (taskDto.DatePlannedStart.Value < clientToday && taskDto.DatePlannedEnd.Value >= clientToday)
                        {
                            AppCalendarExDto userCalendar = null;
                            AppCalendarExDto companyCalendar = AppCalendarBL.RetriveCompanyDefaultCalendar();

                            nbCurrentTaskPlannedCompletedDays = AppProjectDateCaculationBL.CalculateCalendarDurationDays(taskDto.DatePlannedStart, clientToday, userCalendar, companyCalendar);

                        }

                        nbTotalTaskPlannedCompletedDays += nbCurrentTaskPlannedCompletedDays;
                    }


                    if ((taskDto.ProgressId.HasValue && taskDto.ProgressId.Value == (int)EmAppProjectTaskProgress.NotStarted)
                        || !taskDto.ProgressId.HasValue)
                    {
                        nbNotStartedTasks++;
                    }
                    else if (taskDto.ProgressId.HasValue && taskDto.ProgressId.Value == (int)EmAppProjectTaskProgress.Done)
                    {
                        nbCompletedTasks++;
                        isTaskCompleted = true;

                        if (taskDto.AmountOfTime.HasValue)
                        {
                            nbCurrentTaskActualCompletedDays = taskDto.AmountOfTime.Value * 1;

                        }
                    }
                    else
                    {
                        nbInProgressTasks++;

                        if (taskDto.AmountOfTime.HasValue)
                        {
                            if (taskDto.ProgressId.HasValue && taskDto.ProgressId.Value == (int)EmAppProjectTaskProgress.Started)
                            {

                                nbCurrentTaskActualCompletedDays = taskDto.AmountOfTime.Value * 0.25;


                            }
                            else if (taskDto.ProgressId.HasValue && taskDto.ProgressId.Value == (int)EmAppProjectTaskProgress.HalfwayDone)
                            {

                                nbCurrentTaskActualCompletedDays = taskDto.AmountOfTime.Value * 0.5;


                            }
                            else if (taskDto.ProgressId.HasValue && taskDto.ProgressId.Value == (int)EmAppProjectTaskProgress.AlmostDone)
                            {

                                nbCurrentTaskActualCompletedDays = taskDto.AmountOfTime.Value * 0.75;


                            }
                        }
                    }

                    nbTotalTaskActualCompletedDays += nbCurrentTaskActualCompletedDays;

                    if (!taskDto.MainTaskId.HasValue)
                    {
                        ProjectDashboardTaskDto pjDashboardTaskDto = new ProjectDashboardTaskDto();
                        pjDashboardTaskDto.TaskId = (int)taskDto.Id;
                        pjDashboardTaskDto.Display = taskDto.Name;
                        pjDashboardTaskDto.Sort = taskDto.Sort;
                        pjDashboardTaskDto.PlannedCompletePercentage = nbCurrentTaskPlannedCompletedDays / taskDto.AmountOfTime.Value;
                        pjDashboardTaskDto.ActualCompletePercentage = taskDto.CompletedPercent.HasValue ? (taskDto.CompletedPercent.Value / 100) : 0;
                        projectDashboardDto.ProjectTaskList.Add(pjDashboardTaskDto);
                    }

                    List<int> currentTaskUserIdList = taskDto.AppProjectTaskResourceList.Where(o => o.UserId.HasValue).Select(o => o.UserId.Value).Distinct().ToList();

                    foreach (int userId in currentTaskUserIdList)
                    {
                        if (!dictUserIdAndDashboardUserDto.ContainsKey(userId))
                        {
                            ProjectDashboardUserDto dto = new ProjectDashboardUserDto();

                            if (dictUserIdName.ContainsKey(userId))
                            {
                                dto.Display = dictUserIdName[userId];
                            }

                            dictUserIdAndDashboardUserDto[userId] = dto;
                        }

                        var dashboardUserDto = dictUserIdAndDashboardUserDto[userId];

                        if (isTaskCompleted)
                        {
                            dashboardUserDto.NbCompletedTasks++;
                        }
                        else if (isTaskOverDue)
                        {
                            dashboardUserDto.NbOverdueTasks++;
                        }
                        else
                        {
                            dashboardUserDto.NbRemainingTasks++;
                        }
                    }
                }

                projectDashboardDto.TotalNbTasks = totalNbTasks;
                projectDashboardDto.NbNotStartedTasks = nbNotStartedTasks;
                projectDashboardDto.NbCompletedTasks = nbCompletedTasks;
                projectDashboardDto.NbInProgressTasks = nbInProgressTasks;

                projectDashboardDto.NbTotalTaskPlannedCompletedDays = nbTotalTaskPlannedCompletedDays;
                projectDashboardDto.NbTotalTaskActualCompletedDays = nbTotalTaskActualCompletedDays;
                projectDashboardDto.NbTotalTaskDays = nbTotalTaskDays;

                if (nbTotalTaskDays > 0)
                {
                    projectDashboardDto.PlannedCompletion = nbTotalTaskPlannedCompletedDays / nbTotalTaskDays;
                    projectDashboardDto.ActualCompletion = nbTotalTaskActualCompletedDays / nbTotalTaskDays;
                }

                projectDashboardDto.Slippage = projectDashboardDto.ActualCompletion - projectDashboardDto.PlannedCompletion;

                projectDashboardDto.Slippage = projectDashboardDto.Slippage;

                if (projectDashboardDto.Slippage == 0)
                {
                    projectDashboardDto.TimeHealthDisplay = "On schedule.";
                }
                else if (projectDashboardDto.Slippage < 0)
                {
                    projectDashboardDto.TimeHealthDisplay = Math.Round((projectDashboardDto.Slippage * -100), 0) + "% behind schedule.";
                }
                else if (projectDashboardDto.Slippage > 0)
                {
                    projectDashboardDto.TimeHealthDisplay = Math.Round((projectDashboardDto.Slippage * 100), 0) + "% ahead of schedule.";
                }

                int nbToBeCompletedTasks = nbInProgressTasks + nbNotStartedTasks;

                projectDashboardDto.TasksHealthDisplay = nbToBeCompletedTasks + " task"
                    + (nbToBeCompletedTasks > 1 ? "s" : "")
                    + " to be completed.";


                projectDashboardDto.NbOverdueTasks = nbOverdueTasks;

                projectDashboardDto.WorkloadHealthDisplay = nbOverdueTasks
                    + " task"
                    + (nbOverdueTasks > 1 ? "s" : "")
                    + " overdue.";

                projectDashboardDto.ProgressHealthDisplay = Math.Round((projectDashboardDto.ActualCompletion * 100), 0) + "% complete.";


                projectDashboardDto.PlannedProjectCost = aAppProjectOrWorkFlowExDto.PlannedResourceCost.HasValue ? aAppProjectOrWorkFlowExDto.PlannedResourceCost.Value : 0;
                projectDashboardDto.ActualProjectCost = aAppProjectOrWorkFlowExDto.ActualResourceCost.HasValue ? aAppProjectOrWorkFlowExDto.ActualResourceCost.Value : 0;
                projectDashboardDto.ProjectBudget = aAppProjectOrWorkFlowExDto.ProjectModelBugestCost.HasValue ? aAppProjectOrWorkFlowExDto.ProjectModelBugestCost.Value : 0;

                if (projectDashboardDto.ProjectBudget == 0)
                {
                    projectDashboardDto.CostHealthDisplay = "No budget specified.";
                }
                else
                {
                    projectDashboardDto.CostHealth = (projectDashboardDto.ActualProjectCost - projectDashboardDto.ProjectBudget) / projectDashboardDto.ProjectBudget;

                    if (projectDashboardDto.CostHealth == 0)
                    {
                        projectDashboardDto.CostHealthDisplay = "Equal to budget.";
                    }
                    else if (projectDashboardDto.CostHealth < 0)
                    {
                        projectDashboardDto.CostHealthDisplay = Math.Round((projectDashboardDto.CostHealth * -100), 0) + "% under budget.";
                    }
                    else if (projectDashboardDto.CostHealth > 0)
                    {
                        projectDashboardDto.CostHealthDisplay = Math.Round((projectDashboardDto.CostHealth * 100), 0) + "% over budget.";
                    }
                }



                projectDashboardDto.ProjectUserList = dictUserIdAndDashboardUserDto.Values.ToList();




                return projectDashboardDto;

            }


            return null;
        }


    }


}


