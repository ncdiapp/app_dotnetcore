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
using System;
using APP.LBL;

using APP.Framework;
namespace App.BL
{
    public static class AppProjectWorkloadBL
    {
        private static readonly double _workHoursPerDay = 8;

        public static AppProjectWorkloadPivotDto RetrieveProjectWorkload(AppProjectWorkloadInputParameterDto pivotParameterDto)
        {
            if (pivotParameterDto != null)
            {
                AppProjectWorkloadPivotDto pivotDto = new AppProjectWorkloadPivotDto();

                pivotDto.DictWeekGroupAndDateList = PrepareDictWeekGroupAndDateList(pivotParameterDto);

                pivotDto.ProjectWorkloadPivotRowDtoList = PrepareWorkloadPivotDataRowList(pivotParameterDto); ;

                return pivotDto;
            }

            return null;
        }

        public static OperationCallResult<bool> SaveProjectWorkload(AppProjectWorkloadPivotDto workloadPivotDto)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            if (workloadPivotDto != null && workloadPivotDto.ProjectWorkloadPivotRowDtoList != null)
            {
                foreach (var pivotRow in workloadPivotDto.ProjectWorkloadPivotRowDtoList.Where(o => o.TaskResourceId.HasValue && o.TaskId.HasValue))
                {

                    AppProjectTaskResourceExDto aTaskResourceExDto = new AppProjectTaskResourceExDto();

                    aTaskResourceExDto.Id = pivotRow.TaskResourceId;
                    aTaskResourceExDto.UserId = pivotRow.UserId;
                    aTaskResourceExDto.ProjectWorkFlowTaskId = pivotRow.TaskId.Value;
                    aTaskResourceExDto.PlannedWorkHours = pivotRow.DictDateIdAndWorkhour.Values.Where(o => o.HasValue).Sum(o => o.Value);

                    aTaskResourceExDto.AppProjectTaskResourcePlannedHoursList = new ObservableSet<AppProjectTaskResourcePlannedHoursExDto>();

                    foreach (int dateId in pivotRow.DictDateIdAndWorkhour.Keys)
                    {
                        AppProjectTaskResourcePlannedHoursExDto planHourDto = new AppProjectTaskResourcePlannedHoursExDto();
                        planHourDto.DateId = dateId;
                        planHourDto.PlannedWorkHours = pivotRow.DictDateIdAndWorkhour[dateId];

                        aTaskResourceExDto.AppProjectTaskResourcePlannedHoursList.Add(planHourDto);
                    }

                    var saveResourceResult = SaveProjectWorkload_ProcessOneAppProjectTaskResourceExDto(aTaskResourceExDto);

                    if (saveResourceResult.HasErrors)
                    {
                        validationResult.Merge(SaveProjectWorkload_ProcessOneAppProjectTaskResourceExDto(aTaskResourceExDto));
                    }
                }
            }

            if (!validationResult.HasErrors)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppProjectTaskResourceEntity), "plm_AppProjectTaskResourceEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                aOperationCallResult.Object = true;
            }

            return aOperationCallResult;
        }


        public static Dictionary<int, double> RetrieveOneProjectTaskUsersAvailableHours(List<int> userIdList, AppProjectWorkFlowTaskExDto taskDto, AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto)
        {
            if (userIdList != null && userIdList.Count > 0 && taskDto != null && aAppProjectOrWorkFlowExDto != null)
            {
                userIdList = userIdList.Distinct().ToList();

                if (taskDto.DatePlannedStart.HasValue && taskDto.DatePlannedEnd.HasValue)
                {
                    if (AppSecurityUserBL.CurrentUserEntity != null && !string.IsNullOrWhiteSpace(AppSecurityUserBL.CurrentUserEntity.TimeZoneInfoToken))
                    {
                        string zoneKey = AppProjectDateCaculationBL.GetProjectCurrentUserTimeZoneKey(aAppProjectOrWorkFlowExDto);

                        if (!string.IsNullOrEmpty(zoneKey))
                        {
                            DateTime startDate = TimeZoneHelper.ConvertUTCToClientDateTime(taskDto.DatePlannedStart.Value, zoneKey);
                            DateTime endDate = TimeZoneHelper.ConvertUTCToClientDateTime(taskDto.DatePlannedEnd.Value, zoneKey);

                            int startDateId = ControlTypeValueConverter.ConvertValueToInt(startDate.ToString("yyyyMMdd")).Value;
                            int endDateId = ControlTypeValueConverter.ConvertValueToInt(endDate.ToString("yyyyMMdd")).Value;

                            List<AppProjectTaskResourcePlannedHoursExDto> resourcePlannedHoursList = RetrieveAppProjectTaskResourcePlannedHoursExDtoList(null, userIdList);
                            resourcePlannedHoursList = resourcePlannedHoursList.Where(o => o.PlannedWorkHours.HasValue && o.DateId.HasValue && o.DateId.Value >= startDateId && o.DateId.Value <= endDateId).ToList();

                            Dictionary<int, double> dictUserIdAndAvailableHours = new Dictionary<int, double>();

                            var dictUserIdUserCalendar = aAppProjectOrWorkFlowExDto.DictUserIdUserCalendar = AppCalendarBL.GetUserIdCalendarExDtoDictionary(userIdList);                           
                            var dictUserIdCopmanyCalendar =  aAppProjectOrWorkFlowExDto.DictUserIdCopmanyCalendar = AppCalendarBL.GetUserIdCompanyCalendarExDtoDictionary(userIdList);


                            foreach (int userId in userIdList)
                            {
                                double userAvailableHours = GetOneUserAvailableHours(dictUserIdUserCalendar, dictUserIdCopmanyCalendar, startDate, endDate, resourcePlannedHoursList, userId);

                                dictUserIdAndAvailableHours[userId] = userAvailableHours;
                            }

                            return dictUserIdAndAvailableHours;
                        }
                    }
                }
            }

            return null;
        }


        public static Dictionary<int, double> RetrieveOneTaskAllUsersAvailableHours(AppProjectWorkFlowTaskExDto taskDto)
        {
            if (taskDto != null)
            {
                List<LookupItemDto> userLookupItems = AppEntityInfoBL.GetLookupItemListByCode(EmAppEntityLookupInfoCode.AppSecurityUser.ToString(), "");
                Dictionary<int, string> dictUserIdName = userLookupItems.ToDictionary(o => (int)o.Id, o => o.Display);
                List<int> userIdList = dictUserIdName.Keys.ToList();

                if (taskDto.DatePlannedStart.HasValue && taskDto.DatePlannedEnd.HasValue)
                {
                    if (AppSecurityUserBL.CurrentUserEntity != null && !string.IsNullOrWhiteSpace(AppSecurityUserBL.CurrentUserEntity.TimeZoneInfoToken))
                    {
                        string zoneKey = AppSecurityUserBL.CurrentUserEntity.TimeZoneInfoToken;

                        if (!string.IsNullOrEmpty(zoneKey))
                        {
                            DateTime startDate = TimeZoneHelper.ConvertUTCToClientDateTime(taskDto.DatePlannedStart.Value, zoneKey);
                            DateTime endDate = TimeZoneHelper.ConvertUTCToClientDateTime(taskDto.DatePlannedEnd.Value, zoneKey);

                            int startDateId = ControlTypeValueConverter.ConvertValueToInt(startDate.ToString("yyyyMMdd")).Value;
                            int endDateId = ControlTypeValueConverter.ConvertValueToInt(endDate.ToString("yyyyMMdd")).Value;

                            List<AppProjectTaskResourcePlannedHoursExDto> resourcePlannedHoursList = RetrieveAppProjectTaskResourcePlannedHoursExDtoList(null, userIdList);
                            resourcePlannedHoursList = resourcePlannedHoursList.Where(o => o.PlannedWorkHours.HasValue && o.DateId.HasValue && o.DateId.Value >= startDateId && o.DateId.Value <= endDateId).ToList();

                            Dictionary<int, double> dictUserIdAndAvailableHours = new Dictionary<int, double>();

                            var dictUserIdUserCalendar = AppCalendarBL.GetUserIdCalendarExDtoDictionary(userIdList);
                            var dictUserIdCopmanyCalendar = AppCalendarBL.GetUserIdCompanyCalendarExDtoDictionary(userIdList);

                            foreach (int userId in userIdList)
                            {
                                double userAvailableHours = GetOneUserAvailableHours(dictUserIdUserCalendar, dictUserIdCopmanyCalendar, startDate, endDate, resourcePlannedHoursList, userId);

                                dictUserIdAndAvailableHours[userId] = userAvailableHours;
                            }

                            return dictUserIdAndAvailableHours;
                        }
                    }
                }
            }

            return null;
        }


        private static List<AppProjectTaskResourcePlannedHoursExDto> RetrieveAppProjectTaskResourcePlannedHoursExDtoList(List<int> projectIdList, List<int> userIdList)
        {
            EntityCollection<AppProjectTaskResourcePlannedHoursEntity> plannedHoursEntitys = RetrieveAppProjectTaskResourcePlannedHoursEntitytList(projectIdList, userIdList);

            var aDtoList = new List<AppProjectTaskResourcePlannedHoursExDto>();

            foreach (var anEntity in plannedHoursEntitys)
            {
                var aDto = AppProjectTaskResourcePlannedHoursConverter.ConvertEntityToExDto(anEntity);

                if (anEntity.AppProjectTaskResource != null)
                {
                    if (anEntity.AppProjectTaskResource.AppProjectWorkFlowTask != null)
                    {
                        aDto.UserId = anEntity.AppProjectTaskResource.UserId;
                        aDto.TaskId = anEntity.AppProjectTaskResource.ProjectWorkFlowTaskId;
                        aDto.ProjectId = anEntity.AppProjectTaskResource.AppProjectWorkFlowTask.ProjectId;
                    }
                }

                aDtoList.Add(aDto);
            }

            return aDtoList;
        }

        private static Dictionary<DateTime, List<AppProjectDateColumnDto>> PrepareDictWeekGroupAndDateList(AppProjectWorkloadInputParameterDto parameterDto)
        {
            Dictionary<DateTime, List<AppProjectDateColumnDto>> dictWeekGroupAndDateList = new Dictionary<DateTime, List<AppProjectDateColumnDto>>();

            DateTime today = DateTime.Today;

            int year = today.Year;
            int month = today.Month;
            int dayOfMonth = today.Day;

            DateTime startDate = new DateTime(year, month, 1);
            DateTime endDate = startDate.AddMonths(1).AddDays(-1);

            if (parameterDto.RangeStartDateId.HasValue && parameterDto.RangeEndDateId.HasValue)
            {
                int startDateId = parameterDto.RangeStartDateId.Value;
                int endDateId = parameterDto.RangeEndDateId.Value;

                startDate = new DateTime(startDateId / 10000, (startDateId % 10000) / 100, startDateId % 100);
                endDate = new DateTime(endDateId / 10000, (endDateId % 10000) / 100, endDateId % 100);
            }


            for (DateTime aDate = startDate; aDate <= endDate; aDate = aDate.AddDays(1))
            {
                AppProjectDateColumnDto dateColumnDto = new AppProjectDateColumnDto();
                dateColumnDto.WorkDate = aDate;
                dateColumnDto.DateId = ControlTypeValueConverter.ConvertValueToInt(aDate.ToString("yyyyMMdd"));
                dateColumnDto.DateDisplay = aDate.ToString("ddd").Substring(0, 2);

                DateTime startDateOfThisWeek = aDate.AddDays(((aDate.DayOfWeek - DayOfWeek.Monday + 7) % 7) * -1);

                if (startDateOfThisWeek < startDate)
                {
                    startDateOfThisWeek = startDate;
                }

                dateColumnDto.WeekGroupDate = startDateOfThisWeek;


                if (!dictWeekGroupAndDateList.ContainsKey(dateColumnDto.WeekGroupDate))
                {
                    dictWeekGroupAndDateList.Add(dateColumnDto.WeekGroupDate, new List<AppProjectDateColumnDto>());
                }

                dictWeekGroupAndDateList[dateColumnDto.WeekGroupDate].Add(dateColumnDto);
            }

            return dictWeekGroupAndDateList;
        }

        private static List<ProjectWorkloadPivotDataRowDto> PrepareWorkloadPivotDataRowList(AppProjectWorkloadInputParameterDto parameterDto)
        {
            var pivotRowList = new List<ProjectWorkloadPivotDataRowDto>();

            List<AppProjectTaskResourcePlannedHoursExDto> plannedHourDtoList = RetrieveAppProjectTaskResourcePlannedHoursExDtoList(parameterDto.ProjectIdList, parameterDto.UserIdList);

            foreach (var pairUserIdAndPlanHourDtoList in plannedHourDtoList.Where(o => o.UserId.HasValue && o.TaskId.HasValue && o.DateId.HasValue).GroupBy(o => o.UserId.Value).ToDictionary(g => g.Key.ToString(), g => g.ToList()))
            {
                int? userId = ControlTypeValueConverter.ConvertValueToInt(pairUserIdAndPlanHourDtoList.Key);

                foreach (var pairTaskIdAndPlanHourDtoList in pairUserIdAndPlanHourDtoList.Value.GroupBy(o => o.TaskId.Value).ToDictionary(g => g.Key.ToString(), g => g.ToList()))
                {
                    int? taskId = ControlTypeValueConverter.ConvertValueToInt(pairTaskIdAndPlanHourDtoList.Key);
                    var planHourDtoLis = pairTaskIdAndPlanHourDtoList.Value;

                    if (userId.HasValue && taskId.HasValue && planHourDtoLis.Count > 0)
                    {
                        ProjectWorkloadPivotDataRowDto pivotRow = new ProjectWorkloadPivotDataRowDto();

                        pivotRow.TaskId = taskId;
                        pivotRow.UserId = userId;
                        pivotRow.ProjectId = planHourDtoLis.First().ProjectId;
                        pivotRow.TaskResourceId = planHourDtoLis.First().TaskResourceId;
                        pivotRow.TotalHours = planHourDtoLis.Where(o => o.PlannedWorkHours.HasValue).Sum(o => o.PlannedWorkHours.Value);

                        pivotRow.DictDateIdAndWorkhour = new Dictionary<int, double?>();

                        foreach (var aPlanHourDto in planHourDtoLis)
                        {
                            pivotRow.DictDateIdAndWorkhour[aPlanHourDto.DateId.Value] = aPlanHourDto.PlannedWorkHours;
                        }

                        pivotRowList.Add(pivotRow);
                    }
                }
            }

            return pivotRowList;
        }



        private static EntityCollection<AppProjectTaskResourcePlannedHoursEntity> RetrieveAppProjectTaskResourcePlannedHoursEntitytList(List<int> projectIdList, List<int> userIdList)
        {
            using (DataAccessAdapter adapater = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppProjectTaskResourcePlannedHoursEntity> entities = new EntityCollection<AppProjectTaskResourcePlannedHoursEntity>();

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppProjectTaskResourcePlannedHoursEntity);

                rootPath.Add(AppProjectTaskResourcePlannedHoursEntity.PrefetchPathAppProjectTaskResource).SubPath.Add(AppProjectTaskResourceEntity.PrefetchPathAppProjectWorkFlowTask);

                IRelationPredicateBucket filter = new RelationPredicateBucket();
                filter.Relations.Add(AppProjectTaskResourcePlannedHoursEntity.Relations.AppProjectTaskResourceEntityUsingTaskResourceId);
                filter.Relations.Add(AppProjectTaskResourceEntity.Relations.AppProjectWorkFlowTaskEntityUsingProjectWorkFlowTaskId);

                if (projectIdList != null && projectIdList.Count > 0)
                {
                    filter.PredicateExpression.Add(AppProjectWorkFlowTaskFields.ProjectId == projectIdList.ToArray());
                }

                if (userIdList != null && userIdList.Count > 0)
                {
                    filter.PredicateExpression.Add(AppProjectTaskResourceFields.UserId == userIdList.ToArray());
                }

                adapater.FetchEntityCollection(entities, filter, rootPath);


                return entities;
            }


        }

        private static AppProjectTaskResourceEntity RetrieveOneAppProjectTaskResourceEntity(object taskResoureId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppProjectTaskResourceEntity aEntity = new AppProjectTaskResourceEntity(int.Parse(taskResoureId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppProjectTaskResourceEntity);

                rootPath.Add(AppProjectTaskResourceEntity.PrefetchPathAppProjectTaskResourcePlannedHours);

                adpater.FetchEntity(aEntity, rootPath);
                return aEntity;
            }
        }

        private static ValidationResult SaveProjectWorkload_ProcessOneAppProjectTaskResourceExDto(AppProjectTaskResourceExDto aTaskResourceExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppProjectTaskResourceEntity aAppProjectTaskResourceEntity = RetrieveOneAppProjectTaskResourceEntity(aTaskResourceExDto.Id);

            AppProjectTaskResourceConverter.CopyDtoToEntity(aAppProjectTaskResourceEntity, aTaskResourceExDto);

            foreach (var plannedHourDto in aTaskResourceExDto.AppProjectTaskResourcePlannedHoursList.Where(o => o.PlannedWorkHours.HasValue))
            {
                AppProjectTaskResourcePlannedHoursEntity plannedHoursEntity = new AppProjectTaskResourcePlannedHoursEntity();
                AppProjectTaskResourcePlannedHoursConverter.CopyDtoToEntity(plannedHoursEntity, plannedHourDto);
                aAppProjectTaskResourceEntity.AppProjectTaskResourcePlannedHours.Add(plannedHoursEntity);
            }

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.DeleteEntitiesDirectly(typeof(AppProjectTaskResourcePlannedHoursEntity),
                        new RelationPredicateBucket(AppProjectTaskResourcePlannedHoursFields.TaskResourceId == aTaskResourceExDto.Id));
                    //| AppProjectTaskResourcePlannedHoursFields.PlannedWorkHours == System.DBNull.Value));
                    adapter.SaveEntity(aAppProjectTaskResourceEntity, false, true);
                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectTaskResourceEntity), "plm_AppProjectTaskResourceEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                }


                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectTaskResourceEntity), "plm_AppProjectTaskResourceEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static double GetOneUserAvailableHours(Dictionary<int, AppCalendarExDto> dictUserIdUserCalendar, Dictionary<int, AppCalendarExDto> dictUserIdCopmanyCalendar, DateTime startDate, DateTime endDate,
            List<AppProjectTaskResourcePlannedHoursExDto> resourcePlannedHoursList, int userId)
        {
            double workHoursPerDay = _workHoursPerDay;

            AppCalendarExDto userCalendar = null;
            AppCalendarExDto companyCalendar = null;

            
                if (dictUserIdUserCalendar != null && dictUserIdUserCalendar.ContainsKey(userId))
                {
                    userCalendar = dictUserIdUserCalendar[userId];
                }

                if (dictUserIdCopmanyCalendar != null && dictUserIdCopmanyCalendar.ContainsKey(userId))
                {
                    companyCalendar = dictUserIdCopmanyCalendar[userId];
                }
            

            endDate = endDate.AddSeconds(1);

            double availableDays = AppProjectDateCaculationBL.CalculateAvailableInBetweenDays(startDate, endDate, userCalendar, companyCalendar);

            double userTotalWorkingHours = availableDays * workHoursPerDay;

            double userAvailableHours = userTotalWorkingHours;

            var userPlannedHoursList = resourcePlannedHoursList.Where(o => o.UserId.HasValue && o.UserId.Value == userId).ToList();

            if (userPlannedHoursList.Count > 0)
            {
                double userAssignedHours = userPlannedHoursList.Sum(o => o.PlannedWorkHours.Value);
                userAvailableHours = userTotalWorkingHours - userAssignedHours;

                if (userAvailableHours < 0)
                {
                    userAvailableHours = 0;
                }
            }

            return userAvailableHours;
        }



    }
}