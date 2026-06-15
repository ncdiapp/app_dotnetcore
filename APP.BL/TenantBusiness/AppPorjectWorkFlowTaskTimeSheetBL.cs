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
using System;

using APP.Framework;
namespace App.BL
{


    public static class AppPorjectWorkFlowTaskTimeSheetBL
    {




        public static AppProjectWorkFlowTaskExDto RetrieveOneTaskTimeSheetDtoList(int projectWorkFlowTaskID, bool isConvertDateToClientTimezone = true)
        {

            AppProjectWorkFlowTaskExDto taskExDto = AppProjectWorkFlowStructureBL.RetrieveOneAppProjectWorkFlowTask(projectWorkFlowTaskID);


            if (taskExDto != null)
            {
                var aDtoList = taskExDto.AppPorjectWorkFlowTaskTimeSheetList = new ObservableSet<AppPorjectWorkFlowTaskTimeSheetExDto>();

                Dictionary<int, double?> dictUserIdAndDefaultRate = AppSecurityUserBL.RetrieveAllAppSecurityUserEntity().ToDictionary(o => o.UserId, o => o.PersonalRate);

                List<AppProjectTeamMemberDto> teamMemberList = new List<AppProjectTeamMemberDto>();

                if (taskExDto.ProjectId.HasValue)
                {
                    teamMemberList = AppProjectWorkFlowStructureBL.RetriveOneProjectAllTeamMembers(taskExDto.ProjectId);
                }

                Dictionary<int, double?> dictTeamMemberUserIdAndRate = teamMemberList.Where(o => o.UserId.HasValue).ToDictionary(o => o.UserId.Value, o => o.PersonalRate);

                EntityCollection<AppPorjectWorkFlowTaskTimeSheetEntity> timeSheetEntities = RetrieveOneTaskTimeSheetEntityList(projectWorkFlowTaskID);

                List<LookupItemDto> userLookupItems = AppEntityInfoBL.GetLookupItemListByCode(EmAppEntityLookupInfoCode.AppSecurityUser.ToString(), "");
                Dictionary<int, string> dictUserIdName = userLookupItems.ToDictionary(o => (int)o.Id, o => o.Display);

                foreach (var aTimeSheetEntity in timeSheetEntities)
                {
                    AppPorjectWorkFlowTaskTimeSheetExDto timeSheetExDto = AppPorjectWorkFlowTaskTimeSheetConverter.ConvertEntityToExDto(aTimeSheetEntity);


                    timeSheetExDto.ResourceUserId = timeSheetExDto.ApprovedById;
                    timeSheetExDto.WorkDate = timeSheetExDto.ApprovedByDate;

                    if (timeSheetExDto.WorkDate.HasValue)
                    {
                        timeSheetExDto.DateId = ControlTypeValueConverter.ConvertValueToInt(timeSheetExDto.WorkDate.Value.Date.ToString("yyyyMMdd"));
                    }

                    if (isConvertDateToClientTimezone)
                    {
                        timeSheetExDto.WorkDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(timeSheetExDto.WorkDate);
                        //timeSheetExDto.StartTime = ClientTimeZoneHelper.ConvertUTCToClientDateTime(timeSheetExDto.StartTime);
                        //timeSheetExDto.EndTime = ClientTimeZoneHelper.ConvertUTCToClientDateTime(timeSheetExDto.EndTime);
                    }

                    if (dictUserIdName.ContainsKey(timeSheetExDto.ResourceUserId.Value))
                    {
                        timeSheetExDto.ResourceUserDisplay = dictUserIdName[timeSheetExDto.ResourceUserId.Value];
                    }

                    double rate = 0;
                    if (dictTeamMemberUserIdAndRate.ContainsKey(timeSheetExDto.ResourceUserId.Value) && dictTeamMemberUserIdAndRate[timeSheetExDto.ResourceUserId.Value].HasValue)
                    {
                        double? teamMemberRate = dictTeamMemberUserIdAndRate[timeSheetExDto.ResourceUserId.Value];
                        rate = teamMemberRate.Value;
                    }
                    else if (dictUserIdAndDefaultRate.ContainsKey(timeSheetExDto.ResourceUserId.Value) && dictUserIdAndDefaultRate[timeSheetExDto.ResourceUserId.Value].HasValue)
                    {
                        double? userDefaultRate = dictUserIdAndDefaultRate[timeSheetExDto.ResourceUserId.Value];
                        rate = userDefaultRate.Value;
                    }           

                    timeSheetExDto.HourByRate = rate;

                    aDtoList.Add(timeSheetExDto);
                }

            }

            return taskExDto;
        }

        public static OperationCallResult<AppProjectWorkFlowTaskExDto> SaveOnePorjectTaskAllTimeSheet(AppProjectWorkFlowTaskExDto taskExDto)
        {
            OperationCallResult<AppProjectWorkFlowTaskExDto> aOperationCallResult = new OperationCallResult<AppProjectWorkFlowTaskExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;



            Dictionary<int, double?> dictUserIdAndDefaultRate = AppSecurityUserBL.RetrieveAllAppSecurityUserEntity().ToDictionary(o => o.UserId, o => o.PersonalRate);

            List<AppProjectTeamMemberDto> teamMemberList = AppProjectWorkFlowStructureBL.RetriveOneProjectAllTeamMembers(taskExDto.ProjectId);

            Dictionary<int, double?> dictTeamMemberUserIdAndRate = teamMemberList.Where(o => o.UserId.HasValue).ToDictionary(o => o.UserId.Value, o => o.PersonalRate);


            //var allRoleEntity = RetrieveOneTaskTimeSheetEntityList((int)taskExDto.Id);
            var taskEntity = RetrieveOneAppProjectWorkFlowTaskEntity(taskExDto.Id);


            Dictionary<int, AppProjectTaskResourceEntity> dictDbAppPorjectWorkFlowTaskResource = taskEntity.AppProjectTaskResource.ToDictionary(o => o.TaskResourceId, o => o);

            double totalTaskPlannedWorkHours = 0;
            double totalTaskPlannedResourceCost = 0;

            foreach (var dto in taskExDto.AppProjectTaskResourceList)
            {
                int resourceId = (int)dto.Id;
                if (dictDbAppPorjectWorkFlowTaskResource.ContainsKey(resourceId))
                {
                    AppProjectTaskResourceEntity entity = dictDbAppPorjectWorkFlowTaskResource[resourceId];
                    AppProjectTaskResourceConverter.CopyDtoToEntity(entity, dto);


                    dto.PlannedWorkHours = dto.PlannedWorkHours.HasValue ? dto.PlannedWorkHours.Value : 0;

                    if (dto.PlannedWorkHours.HasValue)
                    {
                        totalTaskPlannedWorkHours += dto.PlannedWorkHours.Value;
                        double rate = 0;
                        if (dictTeamMemberUserIdAndRate.ContainsKey(dto.UserId.Value) && dictTeamMemberUserIdAndRate[dto.UserId.Value].HasValue)
                        {
                            double? teamMemberRate = dictTeamMemberUserIdAndRate[dto.UserId.Value];
                            rate = teamMemberRate.Value;
                        }
                        else if (dictUserIdAndDefaultRate.ContainsKey(dto.UserId.Value) && dictUserIdAndDefaultRate[dto.UserId.Value].HasValue)
                        {
                            double? userDefaultRate = dictUserIdAndDefaultRate[dto.UserId.Value];
                            rate = userDefaultRate.Value;
                        }


                        totalTaskPlannedResourceCost += rate * dto.PlannedWorkHours.Value;
                    }


                }
            }



            var allTaskTimeSheetEntity = taskEntity.AppPorjectWorkFlowTaskTimeSheet;

            Dictionary<int, AppPorjectWorkFlowTaskTimeSheetEntity> dictDbAppPorjectWorkFlowTaskTimeSheet = allTaskTimeSheetEntity.ToDictionary(o => o.FlowTaskTimeSheetId, o => o);
            Dictionary<int, AppPorjectWorkFlowTaskTimeSheetExDto> dictDbAppPorjectWorkFlowTaskTimeSheetExdto = taskExDto.AppPorjectWorkFlowTaskTimeSheetList.Where(o => !o.IsNew).ToDictionary(o => int.Parse(o.Id.ToString()), o => o);

            List<int> groupIdDbms = dictDbAppPorjectWorkFlowTaskTimeSheet.Keys.ToList();

            List<int> groupIdUi = dictDbAppPorjectWorkFlowTaskTimeSheetExdto.Keys.ToList();


            //Delete Id
            List<int> deletAppPorjectWorkFlowTaskTimeSheetIDs = groupIdDbms.Except(groupIdUi).ToList();

            double totalTaskActualWorkHours = 0;
            double totalTaskActualResourceCost = 0;
            //new Entity
            foreach (var timeSheetExDto in taskExDto.AppPorjectWorkFlowTaskTimeSheetList)
            {

                timeSheetExDto.ApprovedById = timeSheetExDto.ResourceUserId;
                timeSheetExDto.ApprovedByDate = timeSheetExDto.WorkDate;

                if (timeSheetExDto.IsNew)
                {

                    AppPorjectWorkFlowTaskTimeSheetEntity aParentAppPorjectWorkFlowTaskTimeSheetEntity = new AppPorjectWorkFlowTaskTimeSheetEntity();

                    ConvertTaskTimeSheetExDtoToEntity(timeSheetExDto, aParentAppPorjectWorkFlowTaskTimeSheetEntity, taskExDto.TimeSheetEntryMethod);
                    allTaskTimeSheetEntity.Add(aParentAppPorjectWorkFlowTaskTimeSheetEntity);

                }
                else // update 
                {
                    if (dictDbAppPorjectWorkFlowTaskTimeSheet.ContainsKey((int)timeSheetExDto.Id))
                    {
                        var entity = dictDbAppPorjectWorkFlowTaskTimeSheet[(int)timeSheetExDto.Id];
                        ConvertTaskTimeSheetExDtoToEntity(timeSheetExDto, entity, taskExDto.TimeSheetEntryMethod);

                    }

                }

                if (!timeSheetExDto.HourByRate.HasValue)
                {
                    if (dictTeamMemberUserIdAndRate.ContainsKey(timeSheetExDto.ResourceUserId.Value))
                    {
                        timeSheetExDto.HourByRate = dictTeamMemberUserIdAndRate[timeSheetExDto.ResourceUserId.Value];
                    }
                }

                timeSheetExDto.HourByRate = timeSheetExDto.HourByRate.HasValue ? timeSheetExDto.HourByRate.Value : 0;

                totalTaskActualWorkHours += timeSheetExDto.TimeSpan.Value;

                totalTaskActualResourceCost += timeSheetExDto.HourByRate.Value * timeSheetExDto.TimeSpan.Value;

            }




            taskEntity.PlannedWorkHours = totalTaskPlannedWorkHours;
            taskEntity.PlannedResourceCost = totalTaskPlannedResourceCost;

            taskEntity.ActualWorkHours = totalTaskActualWorkHours;
            taskEntity.ActualResourceCost = totalTaskActualResourceCost;





            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                try
                {

                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(taskEntity);


                    adapter.DeleteEntitiesDirectly(typeof(AppPorjectWorkFlowTaskTimeSheetEntity), new RelationPredicateBucket(AppPorjectWorkFlowTaskTimeSheetFields.FlowTaskTimeSheetId == deletAppPorjectWorkFlowTaskTimeSheetIDs));
                    //}

                    adapter.Commit();
                    validationResult.Items.Add(new ValidationItem(typeof(AppProjectWorkFlowTaskExDto), "App_ProjectWorkFlowTask_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                }


                catch (ORMQueryExecutionException ex)
                {

                    adapter.Rollback();
                    validationResult.Items.Add(new ValidationItem(typeof(AppProjectWorkFlowTaskExDto), "App_ProjectWorkFlowTask__QueryExecution_Error", ValidationItemType.Error, ex.ToString()));


                }


            }



            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                validationResult.Items.Clear();
                validationResult.Items.Add(new ValidationItem(typeof(AppPorjectWorkFlowTaskTimeSheetExDto), "App_AppPorjectWorkFlowTaskTimeSheetEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                aOperationCallResult.Object = RetrieveOneTaskTimeSheetDtoList((int)taskExDto.Id); ;

            }

            return aOperationCallResult;
        }


        private static EntityCollection<AppPorjectWorkFlowTaskTimeSheetEntity> RetrieveOneTaskTimeSheetEntityList(int projectWorkFlowTaskID)
        {
            var timeSheetEntities = new EntityCollection<AppPorjectWorkFlowTaskTimeSheetEntity>();

            using (DataAccessAdapter adapater = AppTenantAdapterBL.GetTenantAdapter())
            {

                IRelationPredicateBucket filter = new RelationPredicateBucket(AppPorjectWorkFlowTaskTimeSheetFields.ProjectWorkFlowTaskId == projectWorkFlowTaskID);


                adapater.FetchEntityCollection(timeSheetEntities, filter);
            }

            return timeSheetEntities;
        }

        private static void ConvertTaskTimeSheetExDtoToEntity(AppPorjectWorkFlowTaskTimeSheetExDto dto, AppPorjectWorkFlowTaskTimeSheetEntity entity, int? timeSheetEntryMethod)
        {
            if (timeSheetEntryMethod.HasValue && timeSheetEntryMethod.Value == (int)EmApprProjectTaskTimeSheetEntryMethod.CalendarTimeRange)
            {
                if (dto.StartTime.HasValue && dto.EndTime.HasValue)
                {
                    if (dto.StartTime.Value > dto.EndTime.Value)
                    {
                        //DateTime tempStartTime = dto.StartTime.Value;
                        //dto.StartTime = dto.EndTime.Value;
                        //dto.EndTime = tempStartTime;
                    }

                    double hours = (dto.EndTime.Value - dto.StartTime.Value).TotalHours;

                    dto.TimeSpan = (int)hours;
                }
                else
                {
                    if (dto.WorkDate.HasValue)
                    {
                        DateTime WorkDate_ClientTimeZone = ClientTimeZoneHelper.ConvertUTCToClientDateTime(dto.WorkDate).Value.Date;

                        //dto.StartTime = WorkDate_ClientTimeZone.AddHours(9);
                        //dto.EndTime = dto.StartTime.Value.AddHours(dto.TimeSpan);

                        //dto.StartTime = ClientTimeZoneHelper.ConvertClientToUTCDateTime(dto.StartTime);
                        //dto.EndTime = ClientTimeZoneHelper.ConvertClientToUTCDateTime(dto.EndTime);
                    }
                }
            }
            else // Method: NumberOfHours
            {
                if (dto.WorkDate.HasValue)
                {
                    DateTime WorkDate_ClientTimeZone = ClientTimeZoneHelper.ConvertUTCToClientDateTime(dto.WorkDate).Value.Date;

                    //dto.StartTime = WorkDate_ClientTimeZone.AddHours(9);
                    //dto.EndTime = dto.StartTime.Value.AddHours(dto.TimeSpan);

                    //dto.StartTime = ClientTimeZoneHelper.ConvertClientToUTCDateTime(dto.StartTime);
                    //dto.EndTime = ClientTimeZoneHelper.ConvertClientToUTCDateTime(dto.EndTime);
                }
            }

            AppPorjectWorkFlowTaskTimeSheetConverter.CopyDtoToEntity(entity, dto);
        }



        //public static OperationCallResult<AppPorjectWorkFlowTaskTimeSheetExDto> SynchronizePorjectTaskActuralHoursFromTimeSheet(int projectWorkFlowTaskID)
        //{
        //    OperationCallResult<AppPorjectWorkFlowTaskTimeSheetExDto> aOperationCallResult = new OperationCallResult<AppPorjectWorkFlowTaskTimeSheetExDto>();
        //    ValidationResult validationResult = new ValidationResult();
        //    aOperationCallResult.ValidationResult = validationResult;


        //    return aOperationCallResult;
        //}


        internal static AppProjectWorkFlowTaskEntity RetrieveOneAppProjectWorkFlowTaskEntity(object taskId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppProjectWorkFlowTaskEntity aEntity = new AppProjectWorkFlowTaskEntity(int.Parse(taskId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppProjectWorkFlowTaskEntity);

                rootPath.Add(AppProjectWorkFlowTaskEntity.PrefetchPathAppPorjectWorkFlowTaskTimeSheet);
                rootPath.Add(AppProjectWorkFlowTaskEntity.PrefetchPathAppProjectTaskResource);

                adpater.FetchEntity(aEntity, rootPath);
                return aEntity;
            }
        }
    }


}
