using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
//using APP.Persistence.Common;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;

namespace App.BL
{
    public static class AppProjectCostAndProgressBL
    {

        // need to add save function or loaridng
        public static void CaculationOneProjectCostAndProgress(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, List<AppProjectOrWorkFlowExDto> childPorjectColelction = null)
        {
            List<AppProjectTeamMemberDto> teamMemberList = AppProjectWorkFlowStructureBL.RetriveOneProjectAllTeamMembers(aAppProjectOrWorkFlowExDto.Id);
            Dictionary<int, double?> dictUsrIdAndRate = teamMemberList.Where(o => o.UserId.HasValue).ToDictionary(o => o.UserId.Value, o => o.PersonalRate);

            foreach (var taskDto in aAppProjectOrWorkFlowExDto.RootTreeList)
            {
                CalculateOneTaskProgresseAndCost(taskDto, aAppProjectOrWorkFlowExDto, childPorjectColelction, dictUsrIdAndRate);
            }


            // 1 Project Cost
            aAppProjectOrWorkFlowExDto.PlannedWorkHours = aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList.Where(o => o.PlannedWorkHours.HasValue && o.Children.IsEmpty()).Sum(o => o.PlannedWorkHours);
            aAppProjectOrWorkFlowExDto.PlannedResourceCost = aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList.Where(o => o.PlannedResourceCost.HasValue && o.Children.IsEmpty()).Sum(o => o.PlannedResourceCost);

            aAppProjectOrWorkFlowExDto.ActualWorkHours = aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList.Where(o => o.ActualWorkHours.HasValue && o.Children.IsEmpty()).Sum(o => o.ActualWorkHours);
            aAppProjectOrWorkFlowExDto.ActualResourceCost = aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList.Where(o => o.ActualResourceCost.HasValue && o.Children.IsEmpty()).Sum(o => o.ActualResourceCost);


            // 2 Project Complete Percentage
            if (aAppProjectOrWorkFlowExDto.RootTreeList.FirstOrDefault(o => o.Weight.HasValue && o.Weight.Value > 0) != null)
            {
                double totalWeight = aAppProjectOrWorkFlowExDto.RootTreeList.Where(o => o.Weight.HasValue && o.Weight.Value > 0).Sum(o => o.Weight.Value);
                double weightByCompletePercentage = aAppProjectOrWorkFlowExDto.RootTreeList.Where(o => o.Weight.HasValue && o.Weight.Value > 0
                    && o.CompletedPercent.HasValue).Sum(o => (o.Weight.Value * o.CompletedPercent.Value / 100.0));

                aAppProjectOrWorkFlowExDto.CompletedPercent = 100 * weightByCompletePercentage / totalWeight;
            }
            else
            {
                double totalDays = aAppProjectOrWorkFlowExDto.RootTreeList.Where(o => o.AmountOfTime.HasValue && o.AmountOfTime.Value > 0).Sum(o => o.AmountOfTime.Value);
                double totalDaysByCompletePercentage = aAppProjectOrWorkFlowExDto.RootTreeList.Where(o => o.AmountOfTime.HasValue && o.AmountOfTime.Value > 0
                         && o.CompletedPercent.HasValue).Sum(o => (o.AmountOfTime.Value * o.CompletedPercent.Value / 100.0));

                if (totalDays > 0)
                {
                    aAppProjectOrWorkFlowExDto.CompletedPercent = 100 * totalDaysByCompletePercentage / totalDays;
                }
                else
                {
                    aAppProjectOrWorkFlowExDto.CompletedPercent = 0;
                }

            }
        }


        private static void CalculateOneTaskProgresseAndCost(AppProjectWorkFlowTaskExDto taskDto, AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, List<AppProjectOrWorkFlowExDto> childPorjectColelction, Dictionary<int, double?> dictUsrIdAndRate)
        {
            int? taskId = taskDto.Id as int?;

            // ExcludeCalculationTask
            if (taskId.HasValue && aAppProjectOrWorkFlowExDto.ExcludeCalculationTaskId.HasValue && aAppProjectOrWorkFlowExDto.ExcludeCalculationTaskId.Value == taskId.Value)
            {
                return;
            }


            if (!taskDto.Children.IsEmpty())                        // 1 Summary Task
            {
                foreach (var childTaskDto in taskDto.Children)
                {
                    CalculateOneTaskProgresseAndCost(childTaskDto, aAppProjectOrWorkFlowExDto, childPorjectColelction, dictUsrIdAndRate);
                }

                CalculateSummaryTask_Progress(taskDto);
                CalculateSummaryTask_Cost(taskDto);

            }
            else if (taskDto.ProjectSectionId.HasValue)             // 2 Child Project Main Task
            {
                CalculateOneChildProjectMainTask_ProgressAndCost(taskDto, childPorjectColelction);
            }
            else                                                    // 3 Leaf Task
            {
                CalculateOneLeafTask_Progress(taskDto);
                CalculateOneLeafTask_PlannedCost(taskDto, dictUsrIdAndRate);
            }


        }

        private static void CalculateOneChildProjectMainTask_ProgressAndCost(AppProjectWorkFlowTaskExDto taskDto, List<AppProjectOrWorkFlowExDto> childPorjectColelction)
        {
            AppProjectOrWorkFlowExDto childAppProjectOrWorkFlowExDto = AppProjectWorkFlowStructureBL.RetrieveOneAppProjectOrWorkFlowExDto((object)taskDto.ProjectSectionId.Value, false, false);

            CaculationOneProjectCostAndProgress(childAppProjectOrWorkFlowExDto, childPorjectColelction);

            if (childPorjectColelction != null)
            {
                childPorjectColelction.Add(childAppProjectOrWorkFlowExDto);
            }

            taskDto.PlannedWorkHours = childAppProjectOrWorkFlowExDto.PlannedWorkHours;
            taskDto.PlannedResourceCost = childAppProjectOrWorkFlowExDto.PlannedResourceCost;

            taskDto.ActualWorkHours = childAppProjectOrWorkFlowExDto.ActualWorkHours;
            taskDto.ActualResourceCost = childAppProjectOrWorkFlowExDto.ActualResourceCost;


            taskDto.CompletedPercent = childAppProjectOrWorkFlowExDto.CompletedPercent;

            if (taskDto.CompletedPercent.Value == 0)
            {
                taskDto.ProgressId = (int)EmAppProjectTaskProgress.NotStarted;
            }
            else if (taskDto.CompletedPercent.Value > 0 && taskDto.CompletedPercent.Value < 50)
            {
                taskDto.ProgressId = (int)EmAppProjectTaskProgress.Started;
            }
            else if (taskDto.CompletedPercent.Value >= 50 && taskDto.CompletedPercent.Value < 75)
            {
                taskDto.ProgressId = (int)EmAppProjectTaskProgress.HalfwayDone;
            }
            else if (taskDto.CompletedPercent.Value >= 75 && taskDto.CompletedPercent.Value < 100)
            {
                taskDto.ProgressId = (int)EmAppProjectTaskProgress.AlmostDone;
            }
            else if (taskDto.CompletedPercent.Value == 100)
            {
                taskDto.ProgressId = (int)EmAppProjectTaskProgress.Done;
            }
        }

        private static void CalculateSummaryTask_Cost(AppProjectWorkFlowTaskExDto taskDto)
        {
            taskDto.PlannedWorkHours = taskDto.Children.Where(o => o.PlannedWorkHours.HasValue).Sum(o => o.PlannedWorkHours);
            taskDto.PlannedResourceCost = taskDto.Children.Where(o => o.PlannedResourceCost.HasValue).Sum(o => o.PlannedResourceCost);
            taskDto.ActualWorkHours = taskDto.Children.Where(o => o.ActualWorkHours.HasValue).Sum(o => o.ActualWorkHours);
            taskDto.ActualResourceCost = taskDto.Children.Where(o => o.ActualResourceCost.HasValue).Sum(o => o.ActualResourceCost);
        }


        private static void CalculateSummaryTask_Progress(AppProjectWorkFlowTaskExDto taskDto)
        {

            if (taskDto.Children.FirstOrDefault(o => o.Weight.HasValue && o.Weight.Value > 0) != null)
            {
                double totalWeight = taskDto.Children.Where(o => o.Weight.HasValue && o.Weight.Value > 0).Sum(o => o.Weight.Value);
                double weightByCompletePercentage = taskDto.Children.Where(o => o.Weight.HasValue && o.Weight.Value > 0
                    && o.CompletedPercent.HasValue).Sum(o => (o.Weight.Value * o.CompletedPercent.Value / 100.0));

                taskDto.CompletedPercent = 100 * weightByCompletePercentage / totalWeight;
            }
            else
            {
                double totalDays = taskDto.Children.Where(o => o.AmountOfTime.HasValue && o.AmountOfTime.Value > 0).Sum(o => o.AmountOfTime.Value);
                double totalDaysByCompletePercentage = taskDto.Children.Where(o => o.AmountOfTime.HasValue && o.AmountOfTime.Value > 0
                         && o.CompletedPercent.HasValue).Sum(o => (o.AmountOfTime.Value * o.CompletedPercent.Value / 100.0));

                taskDto.CompletedPercent = 100 * totalDaysByCompletePercentage / totalDays;
            }



            if (taskDto.CompletedPercent.Value == 0)
            {
                bool isAnyChildStarted = taskDto.Children.Any(o => o.ProgressId.HasValue && o.ProgressId != (int)EmAppProjectTaskProgress.NotStarted);

                if (isAnyChildStarted)
                {
                    taskDto.ProgressId = (int)EmAppProjectTaskProgress.Started;
                }
                else
                {
                    taskDto.ProgressId = (int)EmAppProjectTaskProgress.NotStarted;
                }

            }
            else if (taskDto.CompletedPercent.Value > 0 && taskDto.CompletedPercent.Value < 50)
            {
                taskDto.ProgressId = (int)EmAppProjectTaskProgress.Started;
            }
            else if (taskDto.CompletedPercent.Value >= 50 && taskDto.CompletedPercent.Value < 75)
            {
                taskDto.ProgressId = (int)EmAppProjectTaskProgress.HalfwayDone;
            }
            else if (taskDto.CompletedPercent.Value >= 75 && taskDto.CompletedPercent.Value < 100)
            {
                taskDto.ProgressId = (int)EmAppProjectTaskProgress.AlmostDone;
            }
            else if (taskDto.CompletedPercent.Value == 100)
            {
                taskDto.ProgressId = (int)EmAppProjectTaskProgress.Done;
            }
        }


        private static void CalculateOneLeafTask_PlannedCost(AppProjectWorkFlowTaskExDto taskDto, Dictionary<int, double?> dictUsrIdAndRate)
        {
            if (taskDto.AppProjectTaskResourceList != null)
            {
                double totalTaskPlannedWorkHours = 0;
                double totalTaskPlannedResourceCost = 0;

                foreach (var resourceDto in taskDto.AppProjectTaskResourceList)
                {
                    resourceDto.PlannedWorkHours = resourceDto.PlannedWorkHours.HasValue ? resourceDto.PlannedWorkHours.Value : 0;

                    if (resourceDto.PlannedWorkHours.HasValue)
                    {
                        totalTaskPlannedWorkHours += resourceDto.PlannedWorkHours.Value;
                        if (dictUsrIdAndRate != null && resourceDto.UserId.HasValue && dictUsrIdAndRate.ContainsKey(resourceDto.UserId.Value))
                        {
                            double? rate = dictUsrIdAndRate[resourceDto.UserId.Value];
                            rate = rate.HasValue ? rate.Value : 0;

                            totalTaskPlannedResourceCost += rate.Value * resourceDto.PlannedWorkHours.Value;
                        }
                    }

                }

                taskDto.PlannedWorkHours = totalTaskPlannedWorkHours;
                taskDto.PlannedResourceCost = totalTaskPlannedResourceCost;

            }
        }


        internal static void CalculateOneLeafTask_Progress(AppProjectWorkFlowTaskExDto taskDto)
        {
            if (!taskDto.ProgressId.HasValue)
            {
                if (taskDto.DateActualEnd.HasValue)
                {
                    taskDto.ProgressId = (int)EmAppProjectTaskProgress.Done;
                }
                else if (taskDto.DateActualStart.HasValue)
                {
                    taskDto.ProgressId = (int)EmAppProjectTaskProgress.Started;
                }
                else
                {
                    taskDto.ProgressId = (int)EmAppProjectTaskProgress.NotStarted;
                }
            }


            if (taskDto.ProgressId.Value == (int)EmAppProjectTaskProgress.NotStarted)
            {
                taskDto.CompletedPercent = 0;

                taskDto.DateActualStart = null;
                taskDto.DateActualEnd = null;
            }
            else if (taskDto.ProgressId.Value == (int)EmAppProjectTaskProgress.Started)
            {
                taskDto.CompletedPercent = 25;

                if (!taskDto.DateActualStart.HasValue)
                {
                    taskDto.DateActualStart = System.DateTime.UtcNow;
                }

                taskDto.DateActualEnd = null;
            }
            else if (taskDto.ProgressId.Value == (int)EmAppProjectTaskProgress.HalfwayDone)
            {
                taskDto.CompletedPercent = 50;

                if (!taskDto.DateActualStart.HasValue)
                {
                    taskDto.DateActualStart = System.DateTime.UtcNow;
                }

                taskDto.DateActualEnd = null;
            }
            else if (taskDto.ProgressId.Value == (int)EmAppProjectTaskProgress.AlmostDone)
            {
                taskDto.CompletedPercent = 75;

                if (!taskDto.DateActualStart.HasValue)
                {
                    taskDto.DateActualStart = System.DateTime.UtcNow;
                }

                taskDto.DateActualEnd = null;
            }
            else if (taskDto.ProgressId.Value == (int)EmAppProjectTaskProgress.Done)
            {
                taskDto.CompletedPercent = 100;

                if (!taskDto.DateActualStart.HasValue)
                {
                    taskDto.DateActualStart = System.DateTime.UtcNow;
                }

                if (!taskDto.DateActualEnd.HasValue)
                {
                    taskDto.DateActualEnd = System.DateTime.UtcNow;
                }
            }

        }

    }
}