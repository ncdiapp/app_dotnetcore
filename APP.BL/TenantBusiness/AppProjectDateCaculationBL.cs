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


    public static class AppProjectDateCaculationBL
    {
        public static string GetProjectCurrentUserTimeZoneKey(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
        {
            int clientToUtcOffSet = aAppProjectOrWorkFlowExDto.TimeZoneOffset;
            int offsetHour = -clientToUtcOffSet / 60 % 24;

            string zoneKey = string.Empty;

            if (AppSecurityUserBL.CurrentUserEntity != null && !string.IsNullOrWhiteSpace(AppSecurityUserBL.CurrentUserEntity.TimeZoneInfoToken))
            {
                zoneKey = AppSecurityUserBL.CurrentUserEntity.TimeZoneInfoToken;
            }
            else if (offsetHour != 0)
            {
                if (TimeZoneHelper.DictHoursTimeZoneId.ContainsKey(offsetHour))
                {
                    zoneKey = TimeZoneHelper.DictHoursTimeZoneId[offsetHour];
                }
            }

            return zoneKey;
        }



        #region  ----------------- CircularReference

        // Circular reference

        public static AppProjectWorkFlowTaskExDto CheckCircularProjectPredecessor(AppProjectOrWorkFlowExDto AppProjectOrWorkFlowExDto, List<AppProjectWorkFlowTaskExDto> listPath)
        {
            AppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList = AppProjectWorkFlowStructureBL.GetFlatTaskExDtoList(AppProjectOrWorkFlowExDto);

            var dictProjectActivity = AppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList.Where(o => o.RowIdentity.HasValue).ToDictionary(o => o.RowIdentity.Value, o => o);

            List<KeyValuePair<Guid, List<Guid>>> allProjectIdWithPredecesorlist = SetupAllProjectActivityPredecessorGuid(dictProjectActivity);

            foreach (KeyValuePair<Guid, List<Guid>> pair in allProjectIdWithPredecesorlist)
            {
                List<Guid> listPathGuid = new List<Guid>();

                var projectActivityguid = CheckCircularProjectPredecessor(pair, allProjectIdWithPredecesorlist, listPathGuid);
                if (projectActivityguid.HasValue)
                {
                    foreach (var pathGuid in listPathGuid)
                    {
                        listPath.Add(dictProjectActivity[pathGuid]);
                    }
                    return dictProjectActivity[pair.Key];
                }
            }

            return null;
        }

        public static AppProjectWorkFlowTaskExDto CheckCircularOneProjectActivityPredecessor(AppProjectWorkFlowTaskExDto needToCheckAppProjectWorkFlowTaskExDto,
            AppProjectOrWorkFlowExDto AppProjectOrWorkFlowExDto, List<AppProjectWorkFlowTaskExDto> circularPath)
        {
            AppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList = AppProjectWorkFlowStructureBL.GetFlatTaskExDtoList(AppProjectOrWorkFlowExDto);

            var dictProjectActivity = AppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList.Where(o => o.RowIdentity.HasValue).ToDictionary(o => o.RowIdentity.Value, o => o);

            List<KeyValuePair<Guid, List<Guid>>> allProjectIActivityGuidWithPredecesorlist = SetupAllProjectActivityPredecessorGuid(dictProjectActivity);

            if (!allProjectIActivityGuidWithPredecesorlist.IsEmpty())
            {

                KeyValuePair<Guid, List<Guid>> checkPair = allProjectIActivityGuidWithPredecesorlist.First(o => o.Key == needToCheckAppProjectWorkFlowTaskExDto.RowIdentity);
                List<Guid> listPathGuid = new List<Guid>();

                var projectActivityguid = CheckCircularProjectPredecessor(checkPair, allProjectIActivityGuidWithPredecesorlist, listPathGuid);
                if (projectActivityguid.HasValue)
                {
                    foreach (var pathGuid in listPathGuid)
                    {
                        circularPath.Add(dictProjectActivity[pathGuid]);
                    }
                    return dictProjectActivity[checkPair.Key];
                }
            }




            return null;
        }

        private static List<KeyValuePair<Guid, List<Guid>>> SetupAllProjectActivityPredecessorGuid(Dictionary<Guid, AppProjectWorkFlowTaskExDto> dictProjectActivity)
        {
            List<KeyValuePair<Guid, List<Guid>>> allProjectIdWithPredecesorlist = new List<KeyValuePair<Guid, List<Guid>>>();
            foreach (var keyValue in dictProjectActivity)
            {
                var prodecessorList = keyValue.Value.AppProjectTaskPredecessorList;
                if (prodecessorList.Count > 0)
                {
                    KeyValuePair<Guid, List<Guid>> pair = new KeyValuePair<Guid, List<Guid>>(keyValue.Key, prodecessorList.Select(o => o.PredecessorGuid.Value).ToList());
                    allProjectIdWithPredecesorlist.Add(pair);
                }
            }
            return allProjectIdWithPredecesorlist;
        }

        private static Guid? CheckCircularProjectPredecessor(KeyValuePair<Guid, List<Guid>> onePairProjectActivityIdWihtPredecessorIds,
            List<KeyValuePair<Guid, List<Guid>>> allProjectIdWithPredecesorlist, List<Guid> listPath)
        {
            Stack<KeyValuePair<Guid, List<Guid>>> stack = new Stack<KeyValuePair<Guid, List<Guid>>>();
            stack.Push(onePairProjectActivityIdWihtPredecessorIds);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                listPath.Add(current.Key);
                List<Guid> predecessorIds = current.Value;

                foreach (Guid predecessorid in predecessorIds)
                {
                    if (predecessorid == onePairProjectActivityIdWihtPredecessorIds.Key)
                    {
                        return predecessorid;
                    }
                }

                var childList = allProjectIdWithPredecesorlist.Where(o => predecessorIds.Contains(o.Key));
                foreach (var child in childList)
                {
                    // if (child.Value != root.Key)
                    stack.Push(child);
                }
            }

            //didn’t find it.
            return null;
        }




        #endregion


        public static OperationCallResult<AppProjectOrWorkFlowExDto> ProjectTaskChanged(AppProjectWorkFlowTaskExDto aTask, AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, EmAppProjectTaskChangeType changeType, bool isNeedToRecalculateProject)
        {
            if (aTask != null && aTask.ProjectSectionId.HasValue)
            {
                return null;
            }

            OperationCallResult<AppProjectOrWorkFlowExDto> aOperationCallResult = new OperationCallResult<AppProjectOrWorkFlowExDto>();

            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;



            ConvertTaskDatesToCalculationDates(aTask);

            bool isTaskDateChanged = false;       

            string zoneKey = GetProjectCurrentUserTimeZoneKey(aAppProjectOrWorkFlowExDto);

            if (!string.IsNullOrEmpty(zoneKey))
            {
                if (aTask.CalStartDate.HasValue)
                {
                    aTask.CalStartDate = TimeZoneHelper.ConvertUTCToClientDateTime(aTask.CalStartDate.Value, zoneKey);
                }

                if (aTask.CalEndDate.HasValue)
                {
                    aTask.CalEndDate = TimeZoneHelper.ConvertUTCToClientDateTime(aTask.CalEndDate.Value, zoneKey);
                }
            }

            AppCalendarExDto userCalendar = null;
            AppCalendarExDto companyCalendar = null;
            GetTaskUserCalendar(aTask, aAppProjectOrWorkFlowExDto, out userCalendar, out companyCalendar);

            if (changeType == EmAppProjectTaskChangeType.StartDateChange)
            {
                if (aTask.CalStartDate.HasValue)
                {
                    if (aTask.CalEndDate.HasValue && aTask.CalEndDate.Value < aTask.CalStartDate)
                    {
                        aTask.CalEndDate = aTask.CalStartDate;
                    }

                    bool isStartDateHolidy = AppCalendarBL.IsHolidayOnCalendar(companyCalendar, userCalendar, aTask.CalStartDate.Value);

                    if (isStartDateHolidy)
                    {
                        DateTime nextBusinessDay = GetNextBusynessDayFromCalendar(aTask.CalStartDate.Value, 1, userCalendar, companyCalendar);
                        aTask.CalStartDate = new DateTime(nextBusinessDay.Year, nextBusinessDay.Month, nextBusinessDay.Day, 0, 0, 0, DateTimeKind.Utc);
                    }

                    if (!(aTask.EmTaskType.HasValue && aTask.EmTaskType.Value == (int)EmAppProjectTaskType.Milestone))
                    {
                        //double durationDays = CalculateCalendarDurationDays(aTask.CalStartDate, aTask.CalEndDate, userCalendar, companyCalendar);
                        //SetTaskAmountOfTimeFromDurationDays(aTask, durationDays);

                        CalculateTaskEndDate(aTask, aAppProjectOrWorkFlowExDto);
                    }
                    else
                    {
                        aTask.CalEndDate = aTask.CalStartDate.Value;
                        aTask.CalDurationDays = 0;
                    }

                    isTaskDateChanged = true;

                }
            }
            else if (changeType == EmAppProjectTaskChangeType.EndDateChange)
            {
                if (aTask.CalEndDate.HasValue)
                {
                    if (aTask.CalStartDate.HasValue && aTask.CalEndDate.Value < aTask.CalStartDate)
                    {
                        aTask.CalStartDate = aTask.CalEndDate;
                    }

                    bool isEndDateHolidy = AppCalendarBL.IsHolidayOnCalendar(companyCalendar, userCalendar, aTask.CalEndDate.Value);

                    if (isEndDateHolidy)
                    {
                        DateTime nextBusinessDay = GetNextBusynessDayFromCalendar(aTask.CalEndDate.Value, 1, userCalendar, companyCalendar);
                        aTask.CalEndDate = new DateTime(nextBusinessDay.Year, nextBusinessDay.Month, nextBusinessDay.Day, 0, 0, 0, DateTimeKind.Utc); ;
                    }

                    if (!(aTask.EmTaskType.HasValue && aTask.EmTaskType.Value == (int)EmAppProjectTaskType.Milestone))
                    {
                        double durationDays = CalculateCalendarDurationDays(aTask.CalStartDate, aTask.CalEndDate, userCalendar, companyCalendar);
                        //SetTaskAmountOfTimeFromDurationDays(aTask, durationDays);
                        aTask.CalDurationDays = durationDays;
                    }
                    else
                    {
                        aTask.CalStartDate = aTask.CalEndDate.Value;
                        aTask.CalDurationDays = 0;
                    }
                    isTaskDateChanged = true;
                }
            }
            else if (changeType == EmAppProjectTaskChangeType.BothStartDateAndEndDataChange)
            {
                if (aTask.CalStartDate.HasValue && aTask.CalEndDate.HasValue)
                {
                    if (aTask.CalEndDate.Value < aTask.CalStartDate)
                    {
                        aTask.CalEndDate = aTask.CalStartDate;
                    }

                    bool isStartDateHolidy = AppCalendarBL.IsHolidayOnCalendar(companyCalendar, userCalendar, aTask.CalStartDate.Value);

                    if (isStartDateHolidy)
                    {
                        DateTime nextBusinessDay = GetNextBusynessDayFromCalendar(aTask.CalStartDate.Value, 1, userCalendar, companyCalendar);
                        aTask.CalStartDate = new DateTime(nextBusinessDay.Year, nextBusinessDay.Month, nextBusinessDay.Day, 0, 0, 0, DateTimeKind.Utc);

                    }

                    bool isEndDateHolidy = AppCalendarBL.IsHolidayOnCalendar(companyCalendar, userCalendar, aTask.CalEndDate.Value);

                    if (isEndDateHolidy)
                    {
                        DateTime nextBusinessDay = GetNextBusynessDayFromCalendar(aTask.CalEndDate.Value, 1, userCalendar, companyCalendar);
                        aTask.CalEndDate = new DateTime(nextBusinessDay.Year, nextBusinessDay.Month, nextBusinessDay.Day, 0, 0, 0, DateTimeKind.Utc); ;
                    }

                    if (!(aTask.EmTaskType.HasValue && aTask.EmTaskType.Value == (int)EmAppProjectTaskType.Milestone))
                    {
                        double durationDays = CalculateCalendarDurationDays(aTask.CalStartDate, aTask.CalEndDate, userCalendar, companyCalendar);
                        //SetTaskAmountOfTimeFromDurationDays(aTask, durationDays);
                        aTask.CalDurationDays = durationDays;
                    }
                    else
                    {
                        aTask.CalEndDate = aTask.CalStartDate.Value;
                        aTask.CalDurationDays = 0;
                    }

                    isTaskDateChanged = true;
                }
            }
            else if (changeType == EmAppProjectTaskChangeType.AmountOfTimeChange)
            {
                if (aTask.CalStartDate.HasValue)
                {
                    if (!(aTask.EmTaskType.HasValue && aTask.EmTaskType.Value == (int)EmAppProjectTaskType.Milestone))
                    {
                        CalculateTaskEndDate(aTask, aAppProjectOrWorkFlowExDto);
                    }
                    else
                    {
                        aTask.CalEndDate = aTask.CalStartDate.Value;
                        aTask.CalDurationDays = 0;
                    }

                    isTaskDateChanged = true;
                }
            }
            else if (changeType == EmAppProjectTaskChangeType.TimeUnitChange)
            {
                if (aTask.UnitOfTime.HasValue)
                {
                    if (!(aTask.EmTaskType.HasValue && aTask.EmTaskType.Value == (int)EmAppProjectTaskType.Milestone))
                    {
                        //double durationDays = GetTaskDurationDays(aTask);
                        //SetTaskAmountOfTimeFromDurationDays(aTask, durationDays);


                        if (aTask.CalStartDate.HasValue)
                        {
                            CalculateTaskEndDate(aTask, aAppProjectOrWorkFlowExDto);
                            isTaskDateChanged = true;
                        }
                    }
                    else
                    {
                        aTask.CalDurationDays = 0;
                        isTaskDateChanged = true;
                    }
                }
            }

            if (!string.IsNullOrEmpty(zoneKey))
            {
                if (aTask.CalStartDate.HasValue)
                {
                    aTask.CalStartDate = TimeZoneHelper.ConvertClientToUTCDateTime(aTask.CalStartDate.Value, zoneKey);
                }

                if (aTask.CalEndDate.HasValue)
                {
                    aTask.CalEndDate = TimeZoneHelper.ConvertClientToUTCDateTime(aTask.CalEndDate.Value, zoneKey);
                }
            }


            ConvertBackTaskCalculationDatesToTaskDates(aTask, aAppProjectOrWorkFlowExDto.EmAppProjectStage.Value);

            if (isTaskDateChanged)
            {
                ObservableSet<AppProjectWorkFlowTaskExDto> projectWorkFlowTaskList = AppProjectWorkFlowStructureBL.GetFlatTaskExDtoList(aAppProjectOrWorkFlowExDto);
                var orgTask = projectWorkFlowTaskList.FirstOrDefault(o => object.Equals(o.Id, aTask.Id));
                if (orgTask != null)
                {
                    orgTask.CalStartDate = aTask.CalStartDate;
                    orgTask.CalEndDate = aTask.CalEndDate;
                    orgTask.CalDurationDays = aTask.CalDurationDays;
                    orgTask.UnitOfTime = aTask.UnitOfTime;


                    orgTask.AmountOfTime = aTask.AmountOfTime;

                    //orgTask.DateModelStart = aTask.DateModelStart;
                    //orgTask.DateModelEnd = aTask.DateModelEnd;
                    orgTask.DatePlannedStart = aTask.DatePlannedStart;
                    orgTask.DatePlannedEnd = aTask.DatePlannedEnd;
                    orgTask.DateActualStart = aTask.DateActualStart;
                    orgTask.DateActualEnd = aTask.DateActualEnd;
                }

                aOperationCallResult.Object = aAppProjectOrWorkFlowExDto;

                if (isNeedToRecalculateProject)
                {
                    aOperationCallResult = CalculateProjectOrWorkFlow(aAppProjectOrWorkFlowExDto);
                }

                aOperationCallResult.Object.CurrentTask = aTask;


            }


            return aOperationCallResult;
        }

        public static OperationCallResult<AppProjectOrWorkFlowExDto> CalculateProjectOrWorkFlowTaskDates(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
        {
            OperationCallResult<AppProjectOrWorkFlowExDto> aOperationCallResult = new OperationCallResult<AppProjectOrWorkFlowExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            DoOnePojectDateCaculation(aAppProjectOrWorkFlowExDto, null);

            aOperationCallResult.Object = aAppProjectOrWorkFlowExDto;
            return aOperationCallResult;
        }

        public static OperationCallResult<AppProjectOrWorkFlowExDto> CalculateProjectOrWorkFlow(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
        {
            OperationCallResult<AppProjectOrWorkFlowExDto> aOperationCallResult = new OperationCallResult<AppProjectOrWorkFlowExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            DoOnePojectDateCaculation(aAppProjectOrWorkFlowExDto, null);
            AppProjectCostAndProgressBL.CaculationOneProjectCostAndProgress(aAppProjectOrWorkFlowExDto);

            aOperationCallResult.Object = aAppProjectOrWorkFlowExDto;
            return aOperationCallResult;
        }





        public static OperationCallResult<AppProjectOrWorkFlowExDto> CalculateProjectCriticalPath(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
        {
            OperationCallResult<AppProjectOrWorkFlowExDto> aOperationCallResult = new OperationCallResult<AppProjectOrWorkFlowExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            var calculationResult = CalculateProjectOrWorkFlowTaskDates(aAppProjectOrWorkFlowExDto);
            aValidationResult.Merge(calculationResult.ValidationResult);

            if (calculationResult.IsSuccessfulWithResult)
            {
                aAppProjectOrWorkFlowExDto = calculationResult.Object;
            }
            else
            {
                return calculationResult;
            }

            if (aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList == null)
            {
                aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList = AppProjectWorkFlowStructureBL.GetFlatTaskExDtoList(aAppProjectOrWorkFlowExDto);
            }

            AppProjectWorkFlowStructureBL.InitializeActivityPredecessorsAndSuccessors(aAppProjectOrWorkFlowExDto);

            //if (aAppProjectOrWorkFlowExDto.ProjectDirectionId == (int)EmAppProjectDirection.Reverse)
            //{
            //    CalculateProjectReverseDirectionCriticalPath(aAppProjectOrWorkFlowExDto);
            //}
            //else
            //{
            //    CalculateProjectForwardDirectionCriticalPath(aAppProjectOrWorkFlowExDto);
            //}

            CalculateProjectForwardDirectionCriticalPath(aAppProjectOrWorkFlowExDto);

            aOperationCallResult.Object = aAppProjectOrWorkFlowExDto;
            return aOperationCallResult;

        }

        internal static void DoOnePojectDateCaculation(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, List<AppProjectOrWorkFlowExDto> childPorjectColelction)
        {
            AssignProjectTeamMemberserCalendar(aAppProjectOrWorkFlowExDto);

            // all Task input As UtC
            int timezoneHours = ConvertUtcToLocalTimeForCaculation(aAppProjectOrWorkFlowExDto);

            SetupMilestone(aAppProjectOrWorkFlowExDto);

            DoCaculationPlanedDate(aAppProjectOrWorkFlowExDto, childPorjectColelction);

            CalculateProject_ConvertLocalToUTC(aAppProjectOrWorkFlowExDto, timezoneHours);


            CalculateProjectOrWorkFlowTaskDates_ConvertBackCalculationDatesToProjectDates(aAppProjectOrWorkFlowExDto);

        }

        private static void DoCaculationPlanedDate(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, List<AppProjectOrWorkFlowExDto> childPorjectColelction)
        {
            if (aAppProjectOrWorkFlowExDto.ProjectDirectionId == (int)EmAppProjectDirection.Reverse)
            {
                CalculateProjectReverseDirection(aAppProjectOrWorkFlowExDto);
            }
            else
            {
                CalculateProjectForwardDirection(aAppProjectOrWorkFlowExDto, childPorjectColelction);
            }
        }

        private static void SetupMilestone(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
        {
            foreach (var aTask in aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList)
            {
                if (aTask.EmTaskType.HasValue && aTask.EmTaskType.Value == (int)EmAppProjectTaskType.Milestone)
                {
                    aTask.CalDurationDays = 0;
                }
            }
        }

        private static int ConvertUtcToLocalTimeForCaculation(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
        {
            ObservableSet<AppProjectWorkFlowTaskExDto> projectWorkFlowTaskList = AppProjectWorkFlowStructureBL.GetFlatTaskExDtoList(aAppProjectOrWorkFlowExDto);

            aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList = projectWorkFlowTaskList;

            CalculateProjectOrWorkFlowTaskDates_ConvertProjectDatesToCalculationDates(aAppProjectOrWorkFlowExDto);

            int clientToUtcOffSetInMinutes = aAppProjectOrWorkFlowExDto.TimeZoneOffset;
            //int UtcToClientOffSet = -clientToUtcOffSet;
            int timezoneHours = (-1 * clientToUtcOffSetInMinutes) / 60;
            timezoneHours = timezoneHours % 24;

            CalculateProject_ConvertUCCToToLocal(aAppProjectOrWorkFlowExDto, timezoneHours);
            return timezoneHours;
        }

        private static void AssignProjectTeamMemberserCalendar(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
        {
            List<AppProjectTeamMemberDto> teamMembers = AppProjectWorkFlowStructureBL.RetriveOneProjectAllTeamMembers(aAppProjectOrWorkFlowExDto.Id);

            if (teamMembers.Count > 0)
            {
                List<int> userIdList = teamMembers.Where(o => o.UserId.HasValue).Select(o => o.UserId.Value).ToList();
                aAppProjectOrWorkFlowExDto.DictUserIdUserCalendar = AppCalendarBL.GetUserIdCalendarExDtoDictionary(userIdList);
                aAppProjectOrWorkFlowExDto.DictUserIdCopmanyCalendar = AppCalendarBL.GetUserIdCompanyCalendarExDtoDictionary(userIdList);
            }
        }

        private static void CalculateProjectOrWorkFlowTaskDates_ConvertProjectDatesToCalculationDates(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
        {
            if (aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList == null || aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList.Count == 0)
            {
                aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList = AppProjectWorkFlowStructureBL.GetFlatTaskExDtoList(aAppProjectOrWorkFlowExDto);
            }

            foreach (var aTask in aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList)
            {
                ConvertTaskDatesToCalculationDates(aTask);
            }

            aAppProjectOrWorkFlowExDto.CalcProjectStartDate = aAppProjectOrWorkFlowExDto.DatePlannedStart;
            aAppProjectOrWorkFlowExDto.CalcProjectEndDate = aAppProjectOrWorkFlowExDto.DatePlannedEnd;

        }

        private static void CalculateProjectOrWorkFlowTaskDates_ConvertBackCalculationDatesToProjectDates(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
        {
            if (aAppProjectOrWorkFlowExDto != null && aAppProjectOrWorkFlowExDto.EmAppProjectStage.HasValue)
            {
                if (aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList == null || aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList.Count == 0)
                {
                    aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList = AppProjectWorkFlowStructureBL.GetFlatTaskExDtoList(aAppProjectOrWorkFlowExDto);
                }

                if (aAppProjectOrWorkFlowExDto.EmAppProjectStage == EmAppProjectStage.Completed)
                {
                    var earliestActualStartTask = aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList.Where(o => o.DateActualStart.HasValue).OrderBy(o => o.DateActualStart.Value).FirstOrDefault();
                    var latestActualEndTast = aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList.Where(o => o.DateActualEnd.HasValue).OrderByDescending(o => o.DateActualEnd.Value).FirstOrDefault();

                    if (earliestActualStartTask != null)
                    {
                        aAppProjectOrWorkFlowExDto.DateActualStart = earliestActualStartTask.DateActualStart;
                    }

                    if (latestActualEndTast != null)
                    {
                        aAppProjectOrWorkFlowExDto.DateActualEnd = latestActualEndTast.DateActualEnd;
                    }


                }
                else
                {
                    aAppProjectOrWorkFlowExDto.DatePlannedStart = aAppProjectOrWorkFlowExDto.CalcProjectStartDate;
                    aAppProjectOrWorkFlowExDto.DatePlannedEnd = aAppProjectOrWorkFlowExDto.CalcProjectEndDate;
                }

                foreach (var aTask in aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList)
                {
                    ConvertBackTaskCalculationDatesToTaskDates(aTask, aAppProjectOrWorkFlowExDto.EmAppProjectStage.Value);
                }

            }
        }



        private static void ConvertTaskDatesToCalculationDates(AppProjectWorkFlowTaskExDto aTask)
        {
            //if (isForGanttChart)
            //{
            //    ConvertTaskGanttUIDatesToTaskDates(aTask, emAppProjectStage);
            //}

            aTask.CalStartDate = null;
            aTask.CalEndDate = null;

            if (!aTask.AmountOfTime.HasValue)
            {
                aTask.AmountOfTime = 0;
            }


            aTask.CalDurationDays = aTask.AmountOfTime.Value;
            aTask.CalStartDate = aTask.DatePlannedStart;
            aTask.CalEndDate = aTask.DatePlannedEnd;


        }

        private static void ConvertBackTaskCalculationDatesToTaskDates(AppProjectWorkFlowTaskExDto aTask, EmAppProjectStage emAppProjectStage)
        {
            if (emAppProjectStage != EmAppProjectStage.Completed)
            {
                aTask.AmountOfTime = aTask.CalDurationDays;
                aTask.DatePlannedStart = aTask.CalStartDate;
                aTask.DatePlannedEnd = aTask.CalEndDate;
            }

        }


        public static double CalculateCalendarDurationDays(DateTime? startDate, DateTime? endDate, AppCalendarExDto userCalendar, AppCalendarExDto companyCalendar)
        {
            double durationDays = 0;

            if (startDate.HasValue && endDate.HasValue && startDate.Value < endDate.Value)
            {
                DateTime cursorDate = startDate.Value;

                while (cursorDate < endDate.Value)
                {
                    cursorDate = cursorDate.AddBusinessDaysFromCalendar(1, userCalendar, companyCalendar);

                    if (cursorDate <= endDate.Value)
                    {
                        durationDays += 1;
                    }
                    else
                    {
                        double diffDays = (cursorDate - endDate.Value).TotalDays;
                        diffDays = diffDays - (long)diffDays;
                        //durationDays += 1 - Math.Round(diffDays, 4);
                        durationDays += 1 - diffDays;
                    }
                }
            }
            return durationDays;
        }


        public static double CalculateAvailableInBetweenDays(DateTime? startDate, DateTime? endDate, AppCalendarExDto userCalendar, AppCalendarExDto companyCalendar)
        {
            double durationDays = 0;

            if (startDate.HasValue && endDate.HasValue && startDate.Value < endDate.Value)
            {
                DateTime cursorDate = startDate.Value;

                while (cursorDate < endDate.Value)
                {
                    cursorDate = cursorDate.AddBusinessDaysFromCalendar(1, userCalendar, companyCalendar);

                    if (cursorDate <= endDate.Value)
                    {
                        durationDays += 1;
                    }
                    else
                    {
                        
                    }
                }
            }
            return durationDays;
        }


        //private static void CalculateCalendarDurationDays(DateTime? startDate, DateTime? endDate, AppCalendarExDto userCalendar, AppCalendarExDto companyCalendar)
        //{
        //    if (!(aTask.CalStartDate.HasValue && aTask.CalEndDate.HasValue))
        //    {
        //        return;
        //    }

        //    AppCalendarExDto userCalendar = null;
        //    AppCalendarExDto companyCalendar = null;
        //    GetTaskUserCalendar(aTask, aAppProjectOrWorkFlowExDto, out userCalendar, out companyCalendar);

        //    double durationDays = 0;
        //    //EmAppTaskDurationUnit durationUnit = EmAppTaskDurationUnit.Day;

        //    if (aTask.CalStartDate.Value < aTask.CalEndDate.Value)
        //    {
        //        DateTime cursorDate = aTask.CalStartDate.Value;
        //        //double diffDays = (aTask.CalEndDate.Value - cursorDate).TotalDays;              


        //        while (cursorDate < aTask.CalEndDate.Value)
        //        {
        //            cursorDate = cursorDate.AddBusinessDaysFromCalendar(1, userCalendar, companyCalendar);

        //            if (cursorDate <= aTask.CalEndDate.Value)
        //            {
        //                durationDays += 1;
        //            }
        //            else
        //            {
        //                double diffDays = (cursorDate - aTask.CalEndDate.Value).TotalDays;
        //                diffDays = diffDays - (long)diffDays;
        //                durationDays += 1 - Math.Round(diffDays, 4);
        //            }
        //        }
        //    }

        //    SetTaskAmountOfTimeFromDurationDays(aTask, durationDays);
        //}

        //private static void SetStartDateAndEndDateToBusinessDays(AppProjectWorkFlowTaskExDto aTask, AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
        //{
        //    if (!(aTask.CalStartDate.HasValue && aTask.CalEndDate.HasValue))
        //    {
        //        return;
        //    }

        //    AppCalendarExDto userCalendar = null;
        //    AppCalendarExDto companyCalendar = null;
        //    GetTaskUserCalendar(aTask, aAppProjectOrWorkFlowExDto, out userCalendar, out companyCalendar);

        //    bool isStartDateHolidy = AppCalendarBL.IsHolidayOnCalendar(companyCalendar, userCalendar, aTask.CalStartDate.Value);
        //    bool isEndDateHolidy = AppCalendarBL.IsHolidayOnCalendar(companyCalendar, userCalendar, aTask.CalEndDate.Value);

        //    if (isStartDateHolidy)
        //    {
        //        DateTime nextBusinessDay = GetNextBusynessDayFromCalendar(aTask.CalStartDate.Value, 1, userCalendar, companyCalendar);
        //        aTask.CalStartDate = new DateTime(nextBusinessDay.Year, nextBusinessDay.Month, nextBusinessDay.Day, 0, 0, 0);
        //    }

        //    if (isEndDateHolidy)
        //    {
        //        DateTime nextBusinessDay = GetNextBusynessDayFromCalendar(aTask.CalEndDate.Value, 1, userCalendar, companyCalendar);
        //        aTask.CalEndDate = nextBusinessDay;
        //    }
        //}

        private static void SetTaskAmountOfTimeFromDurationDays(AppProjectWorkFlowTaskExDto aTask, double duration)
        {
            if (!aTask.UnitOfTime.HasValue)
            {
                aTask.UnitOfTime = (int)EmAppTaskDurationUnit.Day;
            }

            if (aTask.UnitOfTime == (int)EmAppTaskDurationUnit.Week)
            {
                aTask.AmountOfTime = duration / 5;
            }
            else if (aTask.UnitOfTime == (int)EmAppTaskDurationUnit.Day)
            {
                aTask.AmountOfTime = duration;
            }
            else if (aTask.UnitOfTime == (int)EmAppTaskDurationUnit.Hour)
            {
                aTask.AmountOfTime = duration * 8;
            }
            else if (aTask.UnitOfTime == (int)EmAppTaskDurationUnit.Minute)
            {
                aTask.AmountOfTime = duration * 8 * 60;
            }
        }

        private static double GetTaskDurationDays(AppProjectWorkFlowTaskExDto aTask)
        {
            double? duration = 0;
            if (aTask.AmountOfTime.HasValue)
            {
                duration = ControlTypeValueConverter.ConvertValueToDouble(aTask.AmountOfTime);
            }

            double durationDays = 0;
            EmAppTaskDurationUnit durationUnit = EmAppTaskDurationUnit.Day;

            if (aTask.UnitOfTime.HasValue)
            {
                durationUnit = (EmAppTaskDurationUnit)aTask.UnitOfTime.Value;
            }

            if (durationUnit == EmAppTaskDurationUnit.Week)
            {
                durationDays = duration.Value * 5;
            }
            else if (durationUnit == EmAppTaskDurationUnit.Day)
            {
                durationDays = duration.Value;
            }
            else if (durationUnit == EmAppTaskDurationUnit.Hour)
            {
                durationDays = duration.Value / 8;
            }
            else if (durationUnit == EmAppTaskDurationUnit.Minute)
            {
                durationDays = duration.Value / 8 / 60;
            }
            return durationDays;
        }



        public static DateTime GetNextBusynessDayFromCalendar(this DateTime current, int sign, AppCalendarExDto userCalendar, AppCalendarExDto companyCalendar)
        {

            bool isHolidy = AppCalendarBL.IsHolidayOnCalendar(companyCalendar, userCalendar, current);

            while (isHolidy)
            {
                current = current.AddDays(sign * 1);
                isHolidy = AppCalendarBL.IsHolidayOnCalendar(companyCalendar, userCalendar, current);
            }

            return current;
        }



        private static double CalculateBusinessDays(DateTime startDate, DateTime endDate)
        {
            DateTime current = startDate;
            double busynessDays = 0;

            while (current.Date < endDate.Date)
            {
                current = current.AddBusinessDays(1);
                busynessDays++;
            }

            busynessDays = busynessDays + endDate.Subtract(current).TotalDays;
            return busynessDays;
        }

        private static DateTime AddBusinessDaysFromCalendar(this DateTime current, double days, AppCalendarExDto userCalendar, AppCalendarExDto companyCalendar)
        {
            var sign = Math.Sign(days);
            var unsignedDays = Math.Abs(days);
            for (double i = unsignedDays; i > 0; i--)
            {
                if (i > 1)
                {
                    current = current.AddDays(sign * 1);
                }
                else
                {
                    current = current.AddDays(sign * i);
                }

                current = GetNextBusynessDayFromCalendar(current, sign, userCalendar, companyCalendar);
            }

            return current;
        }




        private static void CalculateOneRegularTaskDates(AppProjectWorkFlowTaskExDto aTask, AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, DateTime? defaultStartDate = null, List<AppProjectOrWorkFlowExDto> childPorjectColelction = null)
        {
            int? taskId = aTask.Id as int?;

            if (taskId.HasValue && aAppProjectOrWorkFlowExDto.ExcludeCalculationTaskId.HasValue
                && aAppProjectOrWorkFlowExDto.ExcludeCalculationTaskId.Value == taskId.Value) // ExcludeCalculationTask
            {
                aTask.CalStartDate = ConvertSingleDateByTimeZoneOffset_ConvertUTCToLocal(aTask.DatePlannedStart, aAppProjectOrWorkFlowExDto.TimeZoneOffset);
                aTask.CalEndDate = ConvertSingleDateByTimeZoneOffset_ConvertUTCToLocal(aTask.DatePlannedEnd, aAppProjectOrWorkFlowExDto.TimeZoneOffset);
            }
            else
            {
                CalculateTaskStartDate(aTask, aAppProjectOrWorkFlowExDto, defaultStartDate);

                // 	mainTaskDto.ProjectSectionId = childProjectId;
                // Main Task which links to a child Project
                if (aTask.ProjectSectionId.HasValue)
                {

                    // Get Utc time
                    AppProjectOrWorkFlowExDto childAppProjectOrWorkFlowExDto = AppProjectWorkFlowStructureBL.RetrieveOneAppProjectOrWorkFlowExDto(aTask.ProjectSectionId.Value, false, false);

                    childAppProjectOrWorkFlowExDto.DatePlannedStart = ConvertSingleDateByTimeZoneOffset_ConvertLocalToUTC(aTask.CalStartDate, aAppProjectOrWorkFlowExDto.TimeZoneOffset);
                    childAppProjectOrWorkFlowExDto.DatePlannedEnd = aTask.DatePlannedEnd;
                    childAppProjectOrWorkFlowExDto.TimeZoneOffset = aAppProjectOrWorkFlowExDto.TimeZoneOffset;

                    if (childPorjectColelction != null)
                    {
                        childPorjectColelction.Add(childAppProjectOrWorkFlowExDto);
                    }


                    DoOnePojectDateCaculation(childAppProjectOrWorkFlowExDto, childPorjectColelction);
                    // Recursive 


                    //aAppProjectOrWorkFlowExDto.CalcProjectEndDate = aAppProjectOrWorkFlowExDto.RootTreeList.Where(o => o.CalEndDate.HasValue).Max(o => o.CalEndDate.Value);

                    aTask.CalStartDate = ConvertSingleDateByTimeZoneOffset_ConvertUTCToLocal(childAppProjectOrWorkFlowExDto.CalcProjectStartDate, aAppProjectOrWorkFlowExDto.TimeZoneOffset);

                    aTask.CalEndDate = ConvertSingleDateByTimeZoneOffset_ConvertUTCToLocal(childAppProjectOrWorkFlowExDto.CalcProjectEndDate, aAppProjectOrWorkFlowExDto.TimeZoneOffset);


                    double sum_childTaskWorkingDays = childAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList.Sum(o => o.CalDurationDays);
                    double sum_childTaskLeadingDays = childAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList.Where(o => o.TimingDays.HasValue).Sum(o => o.TimingDays.Value);

                    aTask.CalDurationDays = sum_childTaskWorkingDays + sum_childTaskLeadingDays;



                    //aTask.n

                    // return;
                }
                else
                {
                    CalculateTaskEndDate(aTask, aAppProjectOrWorkFlowExDto);
                }


            }


        }

        private static void CalculateTaskStartDate(AppProjectWorkFlowTaskExDto aTask, AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, DateTime? parentTaskStartDate = null)
        {
            AppCalendarExDto userCalendar = null;
            AppCalendarExDto companyCalendar = null;
            GetTaskUserCalendar(aTask, aAppProjectOrWorkFlowExDto, out userCalendar, out companyCalendar);

            List<string> predecessorList = new List<string>();
            if (!aAppProjectOrWorkFlowExDto.DictGuidKeyPredecessorList.IsEmpty() && aAppProjectOrWorkFlowExDto.DictGuidKeyPredecessorList.ContainsKey(aTask.RowIdentity.ToString()))
            {
                predecessorList = aAppProjectOrWorkFlowExDto.DictGuidKeyPredecessorList[aTask.RowIdentity.ToString()];
            }

            if (parentTaskStartDate.HasValue)
            {
                aTask.CalStartDate = aTask.CalEndDate = parentTaskStartDate.Value;
            }
            else
            {
                aTask.CalStartDate = aTask.CalEndDate = aAppProjectOrWorkFlowExDto.CalcProjectStartDate;
            }

            aTask.CalStartDate = GetNextBusynessDayFromCalendar(aTask.CalStartDate.Value, 1, userCalendar, companyCalendar);

            if (aTask.TimingDays.HasValue)
            {
                aTask.CalStartDate = aTask.CalStartDate.Value.AddBusinessDaysFromCalendar(aTask.TimingDays.Value, userCalendar, companyCalendar);
            }

            if (!predecessorList.IsEmpty())
            {
                aTask.CalStartDate = aTask.CalEndDate = null;
                foreach (string taskGuid in predecessorList)
                {
                    var aPredecessorTask = aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList.FirstOrDefault(o => o.RowIdentity.ToString() == taskGuid);
                    if (aPredecessorTask != null)
                    {
                        CalculateOneRegularTaskDates(aPredecessorTask, aAppProjectOrWorkFlowExDto, parentTaskStartDate);

                        DateTime? newStartDate = null;

                        if (aPredecessorTask.CalEndDate.HasValue)
                        {
                            newStartDate = aPredecessorTask.CalEndDate;
                            newStartDate = MidnightAddOneSecondBackFix(newStartDate, userCalendar, companyCalendar);

                            if (aTask.TimingDays.HasValue)
                            {
                                newStartDate = newStartDate.Value.AddBusinessDaysFromCalendar(aTask.TimingDays.Value, userCalendar, companyCalendar);
                            }
                        }

                        if (!aTask.CalStartDate.HasValue || (newStartDate.HasValue && newStartDate.Value > aTask.CalStartDate))
                        {
                            aTask.CalStartDate = newStartDate;
                        }

                        aTask.CalStartDate = MidnightAddOneSecondBackFix(aTask.CalStartDate, userCalendar, companyCalendar);
                    }
                }
            }
        }


        private static void CalculateTaskEndDate(AppProjectWorkFlowTaskExDto aTask, AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
        {
            if (!aTask.CalStartDate.HasValue)
            {
                return;
            }

            if (!aTask.Children.IsEmpty())
            {
                foreach (AppProjectWorkFlowTaskExDto childTask in aTask.Children)
                {
                    CalculateOneRegularTaskDates(childTask, aAppProjectOrWorkFlowExDto, aTask.CalStartDate.Value);
                }

                aTask.CalEndDate = aTask.Children.Where(o => o.CalEndDate.HasValue).Max(o => o.CalEndDate.Value);

                double sum_childTaskWorkingDays = aTask.Children.Sum(o => o.CalDurationDays);
                double sum_childTaskLeadingDays = aTask.Children.Where(o => o.TimingDays.HasValue).Sum(o => o.TimingDays.Value);

                aTask.CalDurationDays = sum_childTaskWorkingDays + sum_childTaskLeadingDays;
            }
            else
            {
                AppCalendarExDto userCalendar = null;
                AppCalendarExDto companyCalendar = null;
                GetTaskUserCalendar(aTask, aAppProjectOrWorkFlowExDto, out userCalendar, out companyCalendar);

                aTask.CalEndDate = aTask.CalStartDate;
                //double durationDays = GetTaskDurationDays(aTask);
                double durationDays = aTask.CalDurationDays;

                if (durationDays != 0)
                {
                    //aTask.CalEndDate = aTask.CalEndDate.Value.AddBusinessDays(durationDays);
                    aTask.CalEndDate = aTask.CalEndDate.Value.AddBusinessDaysFromCalendar(durationDays, userCalendar, companyCalendar);

                    aTask.CalEndDate = MidnightMinusOneSecondFix(aTask.CalEndDate, userCalendar, companyCalendar);
                }
            }
        }



        public static DateTime? MidnightAddOneSecondBackFix(DateTime? aDateTime, AppCalendarExDto userCalendar, AppCalendarExDto companyCalendar)
        {
            if (aDateTime.HasValue)
            {
                if (aDateTime.Value.Hour == 23 && aDateTime.Value.Minute == 59 && aDateTime.Value.Second == 59)
                {
                    aDateTime = aDateTime.Value.AddSeconds(1);
                    aDateTime = GetNextBusynessDayFromCalendar(aDateTime.Value, 1, userCalendar, companyCalendar);
                }
            }

            return aDateTime;
        }

        private static DateTime? MidnightMinusOneSecondFix(DateTime? aDateTime, AppCalendarExDto userCalendar, AppCalendarExDto companyCalendar)
        {
            if (aDateTime.HasValue)
            {
                if (aDateTime.Value.Hour == 0 && aDateTime.Value.Minute == 0 && aDateTime.Value.Second == 0)
                {
                    aDateTime = aDateTime.Value.AddSeconds(-1);
                    aDateTime = GetNextBusynessDayFromCalendar(aDateTime.Value, -1, userCalendar, companyCalendar);
                }
            }

            return aDateTime;
        }

        public static void GetTaskUserCalendar(AppProjectWorkFlowTaskExDto aTask, AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, out AppCalendarExDto userCalendar, out AppCalendarExDto companyCalendar)
        {
            userCalendar = null;
            companyCalendar = null;

            if (aTask.TaskOwnerId.HasValue)
            {
                int userId = aTask.TaskOwnerId.Value;

                if (aAppProjectOrWorkFlowExDto.DictUserIdUserCalendar != null && aAppProjectOrWorkFlowExDto.DictUserIdUserCalendar.ContainsKey(userId))
                {
                    userCalendar = aAppProjectOrWorkFlowExDto.DictUserIdUserCalendar[userId];
                }

                if (aAppProjectOrWorkFlowExDto.DictUserIdCopmanyCalendar != null && aAppProjectOrWorkFlowExDto.DictUserIdCopmanyCalendar.ContainsKey(userId))
                {
                    companyCalendar = aAppProjectOrWorkFlowExDto.DictUserIdCopmanyCalendar[userId];
                }

            }
        }

        public static void GetProjectUserCalendarByUserId(int userId, AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, out AppCalendarExDto userCalendar, out AppCalendarExDto companyCalendar)
        {
            userCalendar = null;
            companyCalendar = null;

            if (aAppProjectOrWorkFlowExDto.DictUserIdUserCalendar != null && aAppProjectOrWorkFlowExDto.DictUserIdUserCalendar.ContainsKey(userId))
            {
                userCalendar = aAppProjectOrWorkFlowExDto.DictUserIdUserCalendar[userId];
            }

            if (aAppProjectOrWorkFlowExDto.DictUserIdCopmanyCalendar != null && aAppProjectOrWorkFlowExDto.DictUserIdCopmanyCalendar.ContainsKey(userId))
            {
                companyCalendar = aAppProjectOrWorkFlowExDto.DictUserIdCopmanyCalendar[userId];
            }


        }

        private static void CalculateProjectForwardDirection(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, List<AppProjectOrWorkFlowExDto> childPorjectColelction = null)
        {
            if (!aAppProjectOrWorkFlowExDto.CalcProjectStartDate.HasValue)
            {
                //???
                aAppProjectOrWorkFlowExDto.CalcProjectStartDate = DateTime.Today;

                //aAppProjectOrWorkFlowExDto.CalcProjectStartDate = DateTime.Today.Date;
            }

            aAppProjectOrWorkFlowExDto.CalcProjectStartDate = aAppProjectOrWorkFlowExDto.CalcProjectStartDate.Value.Date;



            foreach (AppProjectWorkFlowTaskExDto aTask in aAppProjectOrWorkFlowExDto.RootTreeList)
            {
                // Root task has no parent task !!
                CalculateOneRegularTaskDates(aTask, aAppProjectOrWorkFlowExDto, null, childPorjectColelction);
            }




            if (aAppProjectOrWorkFlowExDto.RootTreeList.Where(o => o.CalEndDate.HasValue).Count() > 0)
            {
                aAppProjectOrWorkFlowExDto.CalcProjectEndDate = aAppProjectOrWorkFlowExDto.RootTreeList.Where(o => o.CalEndDate.HasValue).Max(o => o.CalEndDate.Value);
            }
            else
            {
                aAppProjectOrWorkFlowExDto.CalcProjectEndDate = null;
            }

        }


        private static void CalculateProjectReverseDirection(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
        {
            DateTime startOfTheDayTime = aAppProjectOrWorkFlowExDto.CalcProjectEndDate.Value.Date;
            DateTime endOfTheDayTime = startOfTheDayTime.AddDays(1).AddSeconds(-1);

            aAppProjectOrWorkFlowExDto.CalcProjectEndDate = endOfTheDayTime;

            AppProjectOrWorkFlowExDto cloneProject = aAppProjectOrWorkFlowExDto.DeepCopy<AppProjectOrWorkFlowExDto>();

            // set fake start date to do forward calculation
            cloneProject.CalcProjectStartDate = DateTime.Today;


            CalculateProjectForwardDirection(cloneProject);


            if (cloneProject.CalcProjectEndDate.HasValue)
            {
                cloneProject.CalcProjectEndDate = MidnightAddOneSecondBackFix(cloneProject.CalcProjectEndDate, null, null);

                double busynessDays = CalculateBusinessDays(cloneProject.CalcProjectStartDate.Value, cloneProject.CalcProjectEndDate.Value);
                aAppProjectOrWorkFlowExDto.CalcProjectStartDate = aAppProjectOrWorkFlowExDto.CalcProjectEndDate.Value.AddBusinessDays(-1 * busynessDays);

                aAppProjectOrWorkFlowExDto.CalcProjectStartDate = MidnightAddOneSecondBackFix(aAppProjectOrWorkFlowExDto.CalcProjectStartDate, null, null);

                CalculateProjectForwardDirection(aAppProjectOrWorkFlowExDto);
            }
        }

        //private static void CalculateProject_SetProjectTimeByOffSet(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, int offset)
        //{
        //    if (offset != 0)
        //    {
        //        if (aAppProjectOrWorkFlowExDto.CalcProjectStartDate.HasValue)
        //        {
        //            aAppProjectOrWorkFlowExDto.CalcProjectStartDate = aAppProjectOrWorkFlowExDto.CalcProjectStartDate.Value.AddMinutes(offset);
        //        }

        //        if (aAppProjectOrWorkFlowExDto.CalcProjectEndDate.HasValue)
        //        {
        //            aAppProjectOrWorkFlowExDto.CalcProjectEndDate = aAppProjectOrWorkFlowExDto.CalcProjectEndDate.Value.AddMinutes(offset);
        //        }

        //        foreach (var aTask in aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList)
        //        {
        //            if (aTask.CalStartDate.HasValue)
        //            {
        //                aTask.CalStartDate = aTask.CalStartDate.Value.AddMinutes(offset);
        //            }

        //            if (aTask.CalEndDate.HasValue)
        //            {
        //                aTask.CalEndDate = aTask.CalEndDate.Value.AddMinutes(offset);
        //            }
        //        }


        //    }
        //}

        private static DateTime? ConvertSingleDateByTimeZoneOffset_ConvertLocalToUTC(DateTime? aDate, int timeZoneOffset)
        {
            int timezoneHours = GetTimzezoneHoursByTimezoneOffset(timeZoneOffset);

            string zoneKey = string.Empty;

            if (AppSecurityUserBL.CurrentUserEntity != null && !string.IsNullOrWhiteSpace(AppSecurityUserBL.CurrentUserEntity.TimeZoneInfoToken))
            {
                zoneKey = AppSecurityUserBL.CurrentUserEntity.TimeZoneInfoToken;
            }
            else if (timezoneHours != 0)
            {
                if (TimeZoneHelper.DictHoursTimeZoneId.ContainsKey(timezoneHours))
                {
                    zoneKey = TimeZoneHelper.DictHoursTimeZoneId[timezoneHours];
                }
            }

            if (!string.IsNullOrEmpty(zoneKey))
            {
                if (aDate.HasValue)
                {
                    return TimeZoneHelper.ConvertClientToUTCDateTime(aDate.Value, zoneKey);
                }

            }

            return null;
        }

        public static DateTime? ConvertSingleDateByTimeZoneOffset_ConvertUTCToLocal(DateTime? aDate, int timeZoneOffset)
        {
            int timezoneHours = GetTimzezoneHoursByTimezoneOffset(timeZoneOffset);

            string zoneKey = string.Empty;

            if (AppSecurityUserBL.CurrentUserEntity != null && !string.IsNullOrWhiteSpace(AppSecurityUserBL.CurrentUserEntity.TimeZoneInfoToken))
            {
                zoneKey = AppSecurityUserBL.CurrentUserEntity.TimeZoneInfoToken;
            }
            else if (timezoneHours != 0)
            {
                if (TimeZoneHelper.DictHoursTimeZoneId.ContainsKey(timezoneHours))
                {
                    zoneKey = TimeZoneHelper.DictHoursTimeZoneId[timezoneHours];
                }
            }

            if (!string.IsNullOrEmpty(zoneKey))
            {
                if (aDate.HasValue)
                {
                    return TimeZoneHelper.ConvertUTCToClientDateTime(aDate.Value, zoneKey);
                }

            }

            return null;
        }

        private static int GetTimzezoneHoursByTimezoneOffset(int timeZoneOffset)
        {
            int clientToUtcOffSetInMinutes = timeZoneOffset;
            int timezoneHours = (-1 * clientToUtcOffSetInMinutes) / 60;
            timezoneHours = timezoneHours % 24;
            return timezoneHours;
        }

        private static void CalculateProject_ConvertUCCToToLocal(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, int offsetHour)
        {
            string zoneKey = string.Empty;

            if (AppSecurityUserBL.CurrentUserEntity != null && !string.IsNullOrWhiteSpace(AppSecurityUserBL.CurrentUserEntity.TimeZoneInfoToken))
            {
                zoneKey = AppSecurityUserBL.CurrentUserEntity.TimeZoneInfoToken;
            }
            else if (offsetHour != 0)
            {
                if (TimeZoneHelper.DictHoursTimeZoneId.ContainsKey(offsetHour))
                {
                    zoneKey = TimeZoneHelper.DictHoursTimeZoneId[offsetHour];
                }
            }

            if (!string.IsNullOrEmpty(zoneKey))
            {
                if (aAppProjectOrWorkFlowExDto.CalcProjectStartDate.HasValue)
                {
                    aAppProjectOrWorkFlowExDto.CalcProjectStartDate = TimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectOrWorkFlowExDto.CalcProjectStartDate.Value, zoneKey);
                }

                if (aAppProjectOrWorkFlowExDto.CalcProjectEndDate.HasValue)
                {
                    aAppProjectOrWorkFlowExDto.CalcProjectEndDate = TimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectOrWorkFlowExDto.CalcProjectEndDate.Value, zoneKey);
                }

                foreach (var aTask in aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList)
                {
                    if (aTask.CalStartDate.HasValue)
                    {
                        aTask.CalStartDate = TimeZoneHelper.ConvertUTCToClientDateTime(aTask.CalStartDate.Value, zoneKey);
                    }

                    if (aTask.CalEndDate.HasValue)
                    {
                        aTask.CalEndDate = TimeZoneHelper.ConvertUTCToClientDateTime(aTask.CalEndDate.Value, zoneKey);
                    }
                }
            }
        }

        private static void CalculateProject_ConvertLocalToUTC(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, int offsetHour)
        {
            string zoneKey = string.Empty;

            if (AppSecurityUserBL.CurrentUserEntity != null && !string.IsNullOrWhiteSpace(AppSecurityUserBL.CurrentUserEntity.TimeZoneInfoToken))
            {
                zoneKey = AppSecurityUserBL.CurrentUserEntity.TimeZoneInfoToken;
            }
            else if (offsetHour != 0)
            {
                if (TimeZoneHelper.DictHoursTimeZoneId.ContainsKey(offsetHour))
                {
                    zoneKey = TimeZoneHelper.DictHoursTimeZoneId[offsetHour];
                }
            }

            if (!string.IsNullOrEmpty(zoneKey))
            {
                if (aAppProjectOrWorkFlowExDto.CalcProjectStartDate.HasValue)
                {
                    aAppProjectOrWorkFlowExDto.CalcProjectStartDate = TimeZoneHelper.ConvertClientToUTCDateTime(aAppProjectOrWorkFlowExDto.CalcProjectStartDate.Value, zoneKey);
                }

                if (aAppProjectOrWorkFlowExDto.CalcProjectEndDate.HasValue)
                {
                    aAppProjectOrWorkFlowExDto.CalcProjectEndDate = TimeZoneHelper.ConvertClientToUTCDateTime(aAppProjectOrWorkFlowExDto.CalcProjectEndDate.Value, zoneKey);
                }

                foreach (var aTask in aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList)
                {
                    if (aTask.CalStartDate.HasValue)
                    {
                        aTask.CalStartDate = TimeZoneHelper.ConvertClientToUTCDateTime(aTask.CalStartDate.Value, zoneKey);
                    }

                    if (aTask.CalEndDate.HasValue)
                    {
                        aTask.CalEndDate = TimeZoneHelper.ConvertClientToUTCDateTime(aTask.CalEndDate.Value, zoneKey);
                    }
                }
            }
        }


        private static DateTime AddBusinessDays(this DateTime current, double days)
        {
            var sign = Math.Sign(days);
            var unsignedDays = Math.Abs(days);
            for (double i = unsignedDays; i > 0; i--)
            {
                if (i > 1)
                {
                    current = current.AddDays(sign * 1);
                }
                else
                {
                    current = current.AddDays(sign * i);
                }

                current = GetNextBusynessDay(current, sign);
            }

            return current;
        }

        private static DateTime GetNextBusynessDay(this DateTime current, int sign)
        {
            while (current.DayOfWeek == DayOfWeek.Saturday || current.DayOfWeek == DayOfWeek.Sunday)
            {
                current = current.AddDays(sign * 1);
            }

            return current;
        }

        private static void CalculateOneGroupTaskDates(AppProjectWorkFlowTaskExDto aTask, AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
        {
            aTask.CalStartDate = aTask.CalEndDate = null;

            foreach (var aChildTask in aTask.Children)
            {
                if (!aChildTask.Children.IsEmpty())
                {
                    CalculateOneGroupTaskDates(aChildTask, aAppProjectOrWorkFlowExDto);
                }

                if (!aTask.CalStartDate.HasValue || (aChildTask.CalStartDate.HasValue && aChildTask.CalStartDate.Value < aTask.CalStartDate.Value))
                {
                    aTask.CalStartDate = aChildTask.CalStartDate;
                }
                if (!aTask.CalEndDate.HasValue || aChildTask.CalEndDate.HasValue && aChildTask.CalEndDate.Value > aTask.CalEndDate.Value)
                {
                    aTask.CalEndDate = aChildTask.CalEndDate;
                }
            }
        }


        private static void CalculateProjectReverseDirectionCriticalPath(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
        {
            throw new NotImplementedException();
        }

        private static void CalculateProjectForwardDirectionCriticalPath(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
        {
            ObservableSet<AppProjectWorkFlowTaskExDto> projectWorkFlowTaskList = aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList;

            if (projectWorkFlowTaskList != null)
            {
                projectWorkFlowTaskList.ForAll(o =>
                {
                    o.IsCriticalPathTask = false;
                    o.IsLastTask = false;
                });

                AppProjectWorkFlowTaskExDto lastTask = projectWorkFlowTaskList.Where(o => o.CalEndDate.HasValue && o.Children.IsEmpty()).OrderByDescending(o => o.CalEndDate.Value).FirstOrDefault();

                if (lastTask != null)
                {
                    lastTask.IsLastTask = true;
                    lastTask.IsCriticalPathTask = true;

                    MarkCriticalPrececessorTasks(aAppProjectOrWorkFlowExDto, lastTask);
                }
            }
        }

        private static void MarkCriticalPrececessorTasks(AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto, AppProjectWorkFlowTaskExDto lastTask)
        {
            var prececessors = aAppProjectOrWorkFlowExDto.AppProjectWorkFlowTaskList.Where(o => (
                     o.Sucessors != null && o.Sucessors.FirstOrDefault(p => p.RowIdentity != null && lastTask.RowIdentity != null && p.RowIdentity.ToString() == lastTask.RowIdentity.ToString()) != null)
                 ).ToList();

            if (!prececessors.IsEmpty())
            {
                AppProjectWorkFlowTaskExDto lastPrececessor = prececessors.Where(o => o.CalEndDate.HasValue).OrderByDescending(o => o.CalEndDate.Value).FirstOrDefault();
                if (lastPrececessor != null)
                {
                    lastPrececessor.IsCriticalPathTask = true;
                    MarkCriticalPrececessorTasks(aAppProjectOrWorkFlowExDto, lastPrececessor);
                }
            }
        }




    }


}


