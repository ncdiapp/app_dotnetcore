using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
////using APP.Persistence.Common;

using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using System;
using System.Data.SqlClient;

using APP.Framework;
namespace App.BL
{

    public static class AppCalendarBL
    {
        // Require Param: calendarView.ViewStartDate, calendarView.ViewEndDate
        //public static AppCalendarViewExDto RetrieveCurrentUserScheduler(AppCalendarViewExDto calendarView)
        //{
        //    calendarView.UserId = AppSecurityUserBL.CurrentUserId;            
        //    return RetrieveOneUserScheduler(calendarView);            
        //}


        //// Require Param: calendarView.UserId, calendarView.ViewStartDate, calendarView.ViewEndDate
        //public static AppCalendarViewExDto RetrieveOneUserScheduler(AppCalendarViewExDto calendarView)
        //{
        //    int? userId = calendarView.UserId;
        //    if (userId.HasValue)
        //    {
        //        int? userCalendarId = GetUserCalendarId(userId.Value);

        //        if (!userCalendarId.HasValue)
        //        {
        //            AppCalendarExDto appCalendarExDto = new AppCalendarExDto();
        //            appCalendarExDto.UserId = userId.Value;
        //            OperationCallResult<AppCalendarExDto> saveNewResult = SaveAppCalendar(appCalendarExDto);

        //            if (saveNewResult.IsSuccessfulWithResult)
        //            {                        
        //                calendarView.CalenarId = ControlTypeValueConverter.ConvertValueToInt(saveNewResult.Object.Id);
        //                if (calendarView.CalenarId.HasValue)
        //                {
        //                    return RetrieveCalenarView(calendarView);
        //                }
        //            }
        //        }
        //        else
        //        {
        //            var calendarExDto = RetrieveOneAppCalendarExDto(userCalendarId.Value);
        //            calendarView.CalenarId = ControlTypeValueConverter.ConvertValueToInt(calendarExDto.Id);
        //            if (calendarView.CalenarId.HasValue)
        //            {
        //                return RetrieveCalenarView(calendarView);
        //            }
        //        }
                
        //    }


        //    return null;

        //}

       

        public static AppCalendarExDto RetrieveOneAppCalendarExDto(object Id)
        {
            AppCalendarEntity aAppCalendarEntity = RetrieveOneAppCalendarEntity(Id);
            AppCalendarExDto aAppCalendarExDto = ConvertPdmCalendarEntityToPdmCalendarExDto(aAppCalendarEntity);

            return aAppCalendarExDto;
        }


        private static AppCalendarExDto ConvertPdmCalendarEntityToPdmCalendarExDto(AppCalendarEntity aAppCalendarEntity)
        {
            AppCalendarExDto aAppCalendarExDto = AppCalendarConverter.ConvertEntityToExDto(aAppCalendarEntity);

            foreach (var entity in aAppCalendarEntity.AppCalendarRecurringDay)
            {
                var aRotateDto = AppCalendarRecurringDayConverter.ConvertEntityToExDto(entity);
                aAppCalendarExDto.AppCalendarRecurringDayList.Add(aRotateDto);
            }
            foreach (var entity in aAppCalendarEntity.AppCalendarSpecificDay)
            {
                var aRotateDto = AppCalendarSpecificDayConverter.ConvertEntityToExDto(entity);
                aAppCalendarExDto.AppCalendarSpecificDayList.Add(aRotateDto);
            }
            return aAppCalendarExDto;
        }

        public static ObservableSet<AppCalendarDto> RetrieveAllAppCalendarDto()
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppCalendarEntity> list = new EntityCollection<AppCalendarEntity>();
                adapter.FetchEntityCollection(list, null);

                var aDtoList = new ObservableSet<AppCalendarDto>();
                foreach (var o in list)
                {
                    aDtoList.Add(AppCalendarConverter.ConvertEntityToDto(o));
                }

                return aDtoList;
            }
        }

        public static ObservableSet<AppCalendarDto> RetrieveAllCompanyCalendarDto()
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppCalendarEntity> list = new EntityCollection<AppCalendarEntity>();
                IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppCalendarFields.IsCompanyCalendar == true);
                adapter.FetchEntityCollection(list, aFilter);

                var aDtoList = new ObservableSet<AppCalendarDto>();
                foreach (var o in list)
                {
                    aDtoList.Add(AppCalendarConverter.ConvertEntityToDto(o));
                }

                return aDtoList;
            }
        }



        public static OperationCallResult<AppCalendarExDto> SaveAppCalendar(AppCalendarExDto aAppCalendarExDto)
        {
            OperationCallResult<AppCalendarExDto> aOperationCallResult = new OperationCallResult<AppCalendarExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppCalendarEntity aAppCalendarEntity;

            // Save New
            if (aAppCalendarExDto.IsNew)
            {
                aAppCalendarEntity = new AppCalendarEntity();
                AppCalendarConverter.CopyDtoToEntity(aAppCalendarEntity, aAppCalendarExDto);

                foreach (var aAppCalendarRecurringDayExDto in aAppCalendarExDto.AppCalendarRecurringDayList)
                {
                    AppCalendarRecurringDayEntity aAppCalendarRecurringDayEntity = new AppCalendarRecurringDayEntity();
                    AppCalendarRecurringDayConverter.CopyDtoToEntity(aAppCalendarRecurringDayEntity, aAppCalendarRecurringDayExDto);
                    aAppCalendarEntity.AppCalendarRecurringDay.Add(aAppCalendarRecurringDayEntity);
                }

                foreach (var aAppCalendarSpecificDayExDto in aAppCalendarExDto.AppCalendarSpecificDayList)
                {
                    AppCalendarSpecificDayEntity aAppCalendarSpecificDayEntity = new AppCalendarSpecificDayEntity();
                    AppCalendarSpecificDayConverter.CopyDtoToEntity(aAppCalendarSpecificDayEntity, aAppCalendarSpecificDayExDto);
                    aAppCalendarEntity.AppCalendarSpecificDay.Add(aAppCalendarSpecificDayEntity);
                }

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppCalendarEntity);
                        adapter.Commit();

                        aAppCalendarExDto.Id = aAppCalendarEntity.CalendarId;
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppCalendarEntity), "plm_AppCalendarEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }

                    // Entity Logical Validation Exception
                    catch (ORMEntityValidationException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppCalendarEntity), "plm_AppCalendarEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                    }

                    // Database FK Exeption ........
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppCalendarEntity), "plm_AppCalendarEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }

            // Save Dirty
            else if (aAppCalendarExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(SaveAppCalendarExDto_ProcessDirtyAppCalendarExDto(aAppCalendarExDto));
            }

            // if no any errors, refresh from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppCalendarExDto(aAppCalendarExDto.Id);
            }

            return aOperationCallResult;
        }

        private static ValidationResult SaveAppCalendarExDto_ProcessDirtyAppCalendarExDto(AppCalendarExDto aAppCalendarExDto)
        {
            int calendarId = (int)aAppCalendarExDto.Id;

            ValidationResult aValidationResult = new ValidationResult();

            AppCalendarEntity aAppCalendarEntity = RetrieveOneAppCalendarEntity(aAppCalendarExDto.Id);
            AppCalendarConverter.CopyDtoToEntity(aAppCalendarEntity, aAppCalendarExDto);

            // Process week Item
            Dictionary<int, AppCalendarRecurringDayEntity> dictAppCalendarRecurringDayFromDbms = aAppCalendarEntity.AppCalendarRecurringDay.ToDictionary(o => o.RecurringDayId, o => o);

            // new Items
            foreach (AppCalendarRecurringDayDto aChildDto in aAppCalendarExDto.AppCalendarRecurringDayList.FindNewItems())
            {
                AppCalendarRecurringDayEntity aNewChildEntity = new AppCalendarRecurringDayEntity();
                AppCalendarRecurringDayConverter.CopyDtoToEntity(aNewChildEntity, aChildDto);
                aAppCalendarEntity.AppCalendarRecurringDay.Add(aNewChildEntity);
            }

            // Dirty items
            foreach (var modifyitem in aAppCalendarExDto.AppCalendarRecurringDayList.FindModifiedItems())
            {
                int dtoKey = int.Parse(modifyitem.Id.ToString());
                if (dictAppCalendarRecurringDayFromDbms.ContainsKey(dtoKey))
                {
                    AppCalendarRecurringDayConverter.CopyDtoToEntity(dictAppCalendarRecurringDayFromDbms[dtoKey], modifyitem);
                }
            }

            // deleted week IDs
            int[] deleteAppCalendarRecurringDayIDs = aAppCalendarExDto.AppCalendarRecurringDayList.FindDeletedItemIds().Cast<int>().ToArray();

            // process Day Item

            Dictionary<int, AppCalendarSpecificDayEntity> dictAppCalendarSpecificDayFromDbms = aAppCalendarEntity.AppCalendarSpecificDay.ToDictionary(o => o.CalendarDayId, o => o);

            // new Items
            foreach (AppCalendarSpecificDayDto aChildDto in aAppCalendarExDto.AppCalendarSpecificDayList.FindNewItems())
            {
                AppCalendarSpecificDayEntity aNewChildEntity = new AppCalendarSpecificDayEntity();
                AppCalendarSpecificDayConverter.CopyDtoToEntity(aNewChildEntity, aChildDto);
                aAppCalendarEntity.AppCalendarSpecificDay.Add(aNewChildEntity);
            }

            // Dirty items
            foreach (var modifyitem in aAppCalendarExDto.AppCalendarSpecificDayList.FindModifiedItems())
            {
                int dtoKey = int.Parse(modifyitem.Id.ToString());
                if (dictAppCalendarSpecificDayFromDbms.ContainsKey(dtoKey))
                {
                    AppCalendarSpecificDayConverter.CopyDtoToEntity(dictAppCalendarSpecificDayFromDbms[dtoKey], modifyitem);
                }
            }

            // deletedIDs
            int[] deleteAppCalendarSpecificDayIDs = aAppCalendarExDto.AppCalendarSpecificDayList.FindDeletedItemIds().Cast<int>().ToArray();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppCalendarEntity);

                    if (deleteAppCalendarRecurringDayIDs.Count() > 0)
                    {
                        adapter.DeleteEntitiesDirectly(typeof(AppCalendarRecurringDayEntity), new RelationPredicateBucket(AppCalendarRecurringDayFields.RecurringDayId == deleteAppCalendarRecurringDayIDs));
                    }

                    if (deleteAppCalendarSpecificDayIDs.Count() > 0)
                    {
                        adapter.DeleteEntitiesDirectly(typeof(AppCalendarSpecificDayEntity), new RelationPredicateBucket(AppCalendarSpecificDayFields.CalendarDayId == deleteAppCalendarSpecificDayIDs));
                    }

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppCalendarEntity), "plm_AppCalendarEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }
                // Entity Logical Validation Exception
                catch (ORMEntityValidationException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppCalendarEntity), "plm_AppCalendarEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                }

                // Concurrency validation ("Concurrency violation, data not updated")
                catch (ORMConcurrencyException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppCalendarEntity), "plm_AppCalendarEntity_Concurrency_Error", ValidationItemType.Error, ex.ToString()));
                }

                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppCalendarEntity), "plm_AppCalendarEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        public static OperationCallResult<object> DeleteAppCalendar(object calendarId)
        {
            OperationCallResult<object> aValidationResult = new OperationCallResult<object>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.DeleteEntitiesDirectly(typeof(AppCalendarEntity), new RelationPredicateBucket(AppCalendarFields.CalendarId == calendarId));
                    adapter.Commit();
                }

                // Entity Logical Validation Exception
                catch (ORMEntityValidationException ex)
                {
                    adapter.Rollback();
                    aValidationResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppCalendarEntity), "plm_AppCalendarEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                }

                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppCalendarEntity), "plm_AppCalendarEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }

                // if no any errors
                if (!aValidationResult.ValidationResult.HasErrors)
                {
                    aValidationResult.Object = calendarId;
                }
            }

            return aValidationResult;
        }


        public static List<AppCalendarDto> RetrieveUserCalendarDtoList(object userId)
        {
            var aDtoList = new List<AppCalendarDto>();

            if (userId != null)
            {
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    EntityCollection<AppCalendarEntity> list = new EntityCollection<AppCalendarEntity>();
                    IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppCalendarFields.UserId == userId);
                    adapter.FetchEntityCollection(list, aFilter);


                    foreach (var o in list)
                    {
                        aDtoList.Add(AppCalendarConverter.ConvertEntityToDto(o));
                    }
                }
            }

            return aDtoList;
        }

        public static int? GetUserCalendarId(object userId)
        {

            if (userId != null)
            {
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    EntityCollection<AppCalendarEntity> list = new EntityCollection<AppCalendarEntity>();
                    IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppCalendarFields.UserId == userId);
                    adapter.FetchEntityCollection(list, aFilter);

                    if (list.Count > 0)
                    {
                        return list.First().CalendarId;
                    }
                }
            }

            return null;
        }


        public static Dictionary<int, AppCalendarExDto> GetUserIdCalendarExDtoDictionary(List<int> userIdList)
        {
            Dictionary<int, AppCalendarExDto> dictUserIdUserCalendar = new Dictionary<int, AppCalendarExDto>();
            if (userIdList != null)
            {
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    EntityCollection<AppCalendarEntity> list = new EntityCollection<AppCalendarEntity>();
                    IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppCalendarEntity);
                    rootPath.Add(AppCalendarEntity.PrefetchPathAppCalendarRecurringDay);
                    rootPath.Add(AppCalendarEntity.PrefetchPathAppCalendarSpecificDay);

                    IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppCalendarFields.UserId == userIdList.ToArray());

                    adapter.FetchEntityCollection(list, aFilter, rootPath);

                    foreach (var aAppCalendarEntity in list)
                    {
                        if (!dictUserIdUserCalendar.ContainsKey(aAppCalendarEntity.UserId.Value))
                        {
                            AppCalendarExDto aAppCalendarExDto = AppCalendarConverter.ConvertEntityToExDto(aAppCalendarEntity);

                            foreach (var entity in aAppCalendarEntity.AppCalendarRecurringDay)
                            {
                                var aRecurringDayDto = AppCalendarRecurringDayConverter.ConvertEntityToExDto(entity);
                                aAppCalendarExDto.AppCalendarRecurringDayList.Add(aRecurringDayDto);
                            }
                            foreach (var entity in aAppCalendarEntity.AppCalendarSpecificDay)
                            {
                                var aSpecificDayDto = AppCalendarSpecificDayConverter.ConvertEntityToExDto(entity);
                                aAppCalendarExDto.AppCalendarSpecificDayList.Add(aSpecificDayDto);
                            }

                            dictUserIdUserCalendar.Add(aAppCalendarEntity.UserId.Value, aAppCalendarExDto);
                        }
                    }
                }
            }

            return dictUserIdUserCalendar;
        }

        public static Dictionary<int, AppCalendarExDto> GetUserIdCompanyCalendarExDtoDictionary(List<int> userIdList)
        {
            Dictionary<int, AppCalendarExDto> dictUserIdCopmanyCalendar = new Dictionary<int, AppCalendarExDto>();

            Dictionary<int, int> dictUserIdCompanyCalendarId = null;

            AppCalendarExDto defaultComapnyCalendar = RetriveCompanyDefaultCalendar();

            if (userIdList != null && defaultComapnyCalendar != null)
            {
                dictUserIdCompanyCalendarId = GetUserIdCompanyCalendarIdDictionary(userIdList);

                List<int> companyCalendarIds = dictUserIdCompanyCalendarId.Values.Distinct().ToList();

                Dictionary<int, AppCalendarExDto> dictComapnyCalendarIdAndExDto = GetCalendarIdAndExDtoDictionary(dictUserIdCopmanyCalendar, companyCalendarIds);

                foreach (int userId in userIdList)
                {
                    dictUserIdCopmanyCalendar.Add(userId, defaultComapnyCalendar);

                    if (dictUserIdCompanyCalendarId.ContainsKey(userId))
                    {
                        int companyCalendarId = dictUserIdCompanyCalendarId[userId];

                        if (dictComapnyCalendarIdAndExDto.ContainsKey(companyCalendarId) && dictComapnyCalendarIdAndExDto[companyCalendarId] != null)
                        {
                            dictUserIdCopmanyCalendar[userId] = dictComapnyCalendarIdAndExDto[companyCalendarId];
                        }
                    }
                }
            }

            return dictUserIdCopmanyCalendar;
        }

        private static Dictionary<int, AppCalendarExDto> GetCalendarIdAndExDtoDictionary(Dictionary<int, AppCalendarExDto> toReturn, List<int> companyCalendarIds)
        {
            Dictionary<int, AppCalendarExDto> dictComapnyCalendarIdAndExDto = new Dictionary<int, AppCalendarExDto>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppCalendarEntity> list = new EntityCollection<AppCalendarEntity>();
                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppCalendarEntity);
                rootPath.Add(AppCalendarEntity.PrefetchPathAppCalendarRecurringDay);
                rootPath.Add(AppCalendarEntity.PrefetchPathAppCalendarSpecificDay);

                IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppCalendarFields.CalendarId == companyCalendarIds.ToArray());

                adapter.FetchEntityCollection(list, aFilter, rootPath);

                foreach (var aAppCalendarEntity in list)
                {
                    if (!dictComapnyCalendarIdAndExDto.ContainsKey(aAppCalendarEntity.CalendarId))
                    {
                        AppCalendarExDto aAppCalendarExDto = AppCalendarConverter.ConvertEntityToExDto(aAppCalendarEntity);

                        foreach (var entity in aAppCalendarEntity.AppCalendarRecurringDay)
                        {
                            var aRecurringDayDto = AppCalendarRecurringDayConverter.ConvertEntityToExDto(entity);
                            aAppCalendarExDto.AppCalendarRecurringDayList.Add(aRecurringDayDto);
                        }
                        foreach (var entity in aAppCalendarEntity.AppCalendarSpecificDay)
                        {
                            var aSpecificDayDto = AppCalendarSpecificDayConverter.ConvertEntityToExDto(entity);
                            aAppCalendarExDto.AppCalendarSpecificDayList.Add(aSpecificDayDto);
                        }

                        dictComapnyCalendarIdAndExDto.Add(aAppCalendarEntity.CalendarId, aAppCalendarExDto);
                    }
                }
            }

            return dictComapnyCalendarIdAndExDto;
        }



        private static Dictionary<int, int> GetUserIdCompanyCalendarIdDictionary(List<int> userIdList)
        {
            Dictionary<int, int> dictUserIdCompanyCalendarId = new Dictionary<int, int>();
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppSecurityUserEntity> list = new EntityCollection<AppSecurityUserEntity>();
                IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppSecurityUserFields.UserId == userIdList.ToArray());
                adapter.FetchEntityCollection(list, aFilter);

                foreach (var userEntity in list)
                {
                    if (userEntity.CompanyCalendarId.HasValue)
                    {
                        dictUserIdCompanyCalendarId.Add(userEntity.UserId, userEntity.CompanyCalendarId.Value);
                    }
                }
            }

            return dictUserIdCompanyCalendarId;
        }

        public static AppCalendarExDto RetriveCompanyDefaultCalendar()
        {
            var allCompanyDto = RetrieveAllCompanyCalendarDto();
            if (allCompanyDto.Count > 0)
            {
                AppCalendarDto dto = allCompanyDto.FirstOrDefault(o => o.IsCompanyDefaultCalendar.HasValue && o.IsCompanyDefaultCalendar.Value);
                if (dto != null)
                {
                    return RetrieveOneAppCalendarExDto(dto.Id);
                }

            }
            return null;
        }



        // For UI Calendar Setting


        public static AppCalendarViewExDto GetUserCalenarViewByStartAndEndDate(int? userId, DateTime startDate, DateTime endDate)
        {
            AppCalendarViewExDto calendarView = new AppCalendarViewExDto();
            calendarView.CalenarId = AppCalendarBL.GetUserCalendarId(userId.Value);
            calendarView.UserId = userId;
            calendarView.ViewStartDate = startDate;
            calendarView.ViewEndDate = endDate;
            calendarView = AppCalendarBL.RetrieveCalenarView(calendarView);
            return calendarView;
        }

        public static AppCalendarViewExDto RetrieveCalenarView(AppCalendarViewExDto calendarView)
        {
            if (calendarView != null && calendarView.ViewStartDate.HasValue && calendarView.ViewEndDate.HasValue)
            {


                AppCalendarExDto calendarDto = null;
                AppCalendarExDto companyCalendarDto = null;

                if (calendarView.CalenarId.HasValue)
                {
                    calendarDto = RetrieveOneAppCalendarExDto(calendarView.CalenarId.Value);
                }
                else if (calendarView.UserId.HasValue) // For New User Only, Retrieve Company Calendar
                {
                    calendarDto = RetrieveUserCompanyCalendar(calendarView.UserId.Value);
                }

                if (calendarDto != null)
                {
                    calendarView.CalendarDaysList = new List<AppCalendarSpecificDayDto>();

                    if (calendarDto.UserId.HasValue)
                    {
                        int userId = calendarDto.UserId.Value;
                        companyCalendarDto = RetrieveUserCompanyCalendar(userId);
                    }

                    for (DateTime aDate = calendarView.ViewStartDate.Value; aDate <= calendarView.ViewEndDate.Value; aDate = aDate.AddDays(1))
                    {
                        AppCalendarSpecificDayDto aCalendarDay = new AppCalendarSpecificDayDto();
                        aCalendarDay.StartDate = new DateTime(aDate.Year, aDate.Month, aDate.Day, 0, 0, 0);
                        aCalendarDay.EndDate = new DateTime(aDate.Year, aDate.Month, aDate.Day, 23, 59, 59);

                        //aCalendarDay.StartDateString = aCalendarDay.StartDate.Value.ToString("o");
                        //aCalendarDay.EndDateString = aCalendarDay.EndDate.Value.ToString("o");

                        aCalendarDay.StartDateString = string.Format("{0:00}--{1:00}-{2:00}", aDate.Year, aDate.Month, aDate.Day) + "T00:00:00";
                        aCalendarDay.EndDateString = string.Format("{0:00}--{1:00}-{2:00}", aDate.Year, aDate.Month, aDate.Day) + "T23:59:59";


                        aCalendarDay.WorkStatus = null;

                        if (aDate.DayOfWeek == DayOfWeek.Saturday || aDate.DayOfWeek == DayOfWeek.Sunday)
                        {
                            aCalendarDay.WorkStatus = (int)EmAppCalendarWorkState.NotWorking;
                            aCalendarDay.Name = "Weekend";
                        }

                        CalculateOneCalendarDateWorkingStatus(calendarDto, aDate, aCalendarDay);

                        if (!aCalendarDay.WorkStatus.HasValue && companyCalendarDto != null)
                        {
                            CalculateOneCalendarDateWorkingStatus(companyCalendarDto, aDate, aCalendarDay);
                        }


                        if (aCalendarDay.WorkStatus.HasValue && aCalendarDay.WorkStatus.Value == (int)EmAppCalendarWorkState.NotWorking)
                        {
                            calendarView.CalendarDaysList.Add(aCalendarDay);
                        }
                    }
                }

                return calendarView;
            }

            return null;


        }


        private static AppCalendarExDto RetrieveUserCompanyCalendar(object userId)
        {
            var userEntity = AppSecurityUserBL.RetrieveOneAppSecurityUserEntity(userId);

            if (userEntity != null && !userEntity.IsNew)
            {
                AppCalendarExDto companyCalendar = null;

                if (userEntity.CompanyCalendarId.HasValue)
                {
                    companyCalendar = RetrieveOneAppCalendarExDto(userEntity.CompanyCalendarId.Value);
                }
                else
                {
                    companyCalendar = RetriveCompanyDefaultCalendar();
                }


                if (companyCalendar != null)
                {
                    var companyRecurringDayList = companyCalendar.AppCalendarRecurringDayList;
                    var companySpecificDayList = companyCalendar.AppCalendarSpecificDayList;

                    foreach (var companyRow in companyRecurringDayList)
                    {
                        companyRow.IsFromCompanyCalendar = true;
                    }

                    return companyCalendar;
                }
            }

            return null;

        }

        private static void CalculateOneCalendarDateWorkingStatus(AppCalendarExDto calendarDto, DateTime aDate, AppCalendarSpecificDayDto aCalendarDay)
        {
            var RecurringDays = calendarDto.AppCalendarRecurringDayList;
            var specificDays = calendarDto.AppCalendarSpecificDayList;

            foreach (var recurringDay in RecurringDays)
            {
                if (recurringDay.DateTokenType.HasValue)
                {
                    if (recurringDay.DateTokenType.Value == (int)EmAppCalendarRecurringType.ByYear)
                    {
                        if (recurringDay.Month.HasValue && recurringDay.DayOfMonth.HasValue
                            && recurringDay.Month.Value == aDate.Month && recurringDay.DayOfMonth.Value == aDate.Day)
                        {
                            aCalendarDay.WorkStatus = recurringDay.WorkStatus;
                            aCalendarDay.Name = recurringDay.Name;
                        }
                    }
                    else if (recurringDay.DateTokenType.Value == (int)EmAppCalendarRecurringType.ByMonth)
                    {
                        if (recurringDay.DayOfMonth.HasValue && recurringDay.DayOfMonth.Value == aDate.Day)
                        {
                            aCalendarDay.WorkStatus = recurringDay.WorkStatus;
                            aCalendarDay.Name = recurringDay.Name;
                        }
                    }
                    else if (recurringDay.DateTokenType.Value == (int)EmAppCalendarRecurringType.ByWeek)
                    {
                        if (recurringDay.DayOfWeek.HasValue && (recurringDay.DayOfWeek.Value % 7) == (int)aDate.DayOfWeek)
                        {
                            aCalendarDay.WorkStatus = recurringDay.WorkStatus;
                            aCalendarDay.Name = recurringDay.Name;
                        }
                    }
                }
            }

            foreach (var aSpecificDay in specificDays)
            {
                if (aSpecificDay.StartDate <= aDate && aSpecificDay.EndDate >= aDate)
                {
                    aCalendarDay.WorkStatus = aSpecificDay.WorkStatus;
                    aCalendarDay.Name = aSpecificDay.Name;
                    aCalendarDay.EmDateDefineType = aSpecificDay.EmDateDefineType;
                    aCalendarDay.EmDateRangeType = aSpecificDay.EmDateRangeType;
                }
            }
        }

        public static bool IsHolidayOnCalendar(AppCalendarExDto companyCalendar, AppCalendarExDto userCalendar, DateTime aDate)
        {
            AppCalendarSpecificDayDto aCalendarDay = new AppCalendarSpecificDayDto();

            if (aDate.DayOfWeek == DayOfWeek.Saturday || aDate.DayOfWeek == DayOfWeek.Sunday)
            {
                aCalendarDay.WorkStatus = (int)EmAppCalendarWorkState.NotWorking;
                aCalendarDay.Name = "Weekend";
            }



            if (userCalendar != null)
            {
                CalculateOneCalendarDateWorkingStatus(userCalendar, aDate, aCalendarDay);
            }

            if (companyCalendar != null && !aCalendarDay.WorkStatus.HasValue)
            {
                CalculateOneCalendarDateWorkingStatus(companyCalendar, aDate, aCalendarDay);
            }

            if (aCalendarDay.WorkStatus.HasValue && aCalendarDay.WorkStatus.Value == (int)EmAppCalendarWorkState.NotWorking)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static CalendarBaseDateDto FindOneCalendarBaseDate(DateTime aDate)
        {
            aDate = new DateTime(aDate.Year, aDate.Month, aDate.Day);

            string query = @"  SELECT Top 1 Day
                                      ,Day_Desc
                                      ,Week
                                      ,Week_Desc
                                      ,Bi_Week
                                      ,Bi_Week_Desc
                                      ,Hlf_Month
                                      ,Hlf_Month_Desc
                                      ,Month
                                      ,Month_Desc
                                      ,Quarter
                                      ,Quarter_Desc
                                      ,Pln_Hlf_Yr
                                      ,Pln_Hlf_Yr_Desc
                                      ,Pln_Yr
                                      ,Pln_Yr_Desc
                                      ,Fiscal_Hlf_Yr
                                      ,Fiscal_Hlf_Yr_Desc
                                      ,Fiscal_Yr
                                      ,Fiscal_Yr_Desc
                                      ,Range_Period
                                      ,Range_Period_Desc
                                    FROM AppCalendarBaseDate where Day_Desc = @dateValue";

            List<SqlParameter> lsitparamter = new List<SqlParameter>();
            lsitparamter.Add(new SqlParameter("@dateValue", aDate));

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                DataTable result = adapter.ExecuteDataTableRetrievalQuery(query, lsitparamter);
                if (result.Rows.Count > 0)
                {
                    CalendarBaseDateDto toReturn = new CalendarBaseDateDto();

                    DataRow dataRow = result.Rows[0];

                    foreach (DataColumn col in result.Columns)
                    {
                        object value = dataRow[col];

                        if (col.ColumnName.ToLower() == "Day".ToLower())
                        {
                            toReturn.Day = ControlTypeValueConverter.ConvertValueToInt(value);
                        }
                        else if (col.ColumnName.ToLower() == "Day_Desc".ToLower())
                        {
                            toReturn.Day_Desc = ControlTypeValueConverter.ConvertValueToDate(value);
                        }
                        else if (col.ColumnName.ToLower() == "Week".ToLower())
                        {
                            toReturn.Week = value.ToString();
                        }
                        else if (col.ColumnName.ToLower() == "Week_Desc".ToLower())
                        {
                            toReturn.Week_Desc = value.ToString();
                        }
                        else if (col.ColumnName.ToLower() == "Bi_Week".ToLower())
                        {
                            toReturn.Bi_Week = value.ToString();
                        }
                        else if (col.ColumnName.ToLower() == "Bi_Week_Desc".ToLower())
                        {
                            toReturn.Bi_Week_Desc = value.ToString();
                        }
                        else if (col.ColumnName.ToLower() == "Hlf_Month".ToLower())
                        {
                            toReturn.Hlf_Month = value.ToString();
                        }
                        else if (col.ColumnName.ToLower() == "Hlf_Month_Desc".ToLower())
                        {
                            toReturn.Hlf_Month_Desc = value.ToString();
                        }
                        else if (col.ColumnName.ToLower() == "Month".ToLower())
                        {
                            toReturn.Month = value.ToString();
                        }
                        else if (col.ColumnName.ToLower() == "Month_Desc".ToLower())
                        {
                            toReturn.Month_Desc = value.ToString();
                        }
                        else if (col.ColumnName.ToLower() == "Quarter".ToLower())
                        {
                            toReturn.Quarter = value.ToString();
                        }
                        else if (col.ColumnName.ToLower() == "Quarter_Desc".ToLower())
                        {
                            toReturn.Quarter_Desc = value.ToString();
                        }
                        else if (col.ColumnName.ToLower() == "Pln_Hlf_Yr".ToLower())
                        {
                            toReturn.Pln_Hlf_Yr = value.ToString();
                        }
                        else if (col.ColumnName.ToLower() == "Pln_Hlf_Yr_Desc".ToLower())
                        {
                            toReturn.Pln_Hlf_Yr_Desc = value.ToString();
                        }
                        else if (col.ColumnName.ToLower() == "Pln_Yr".ToLower())
                        {
                            toReturn.Pln_Yr = value.ToString();
                        }
                        else if (col.ColumnName.ToLower() == "Pln_Yr_Desc".ToLower())
                        {
                            toReturn.Pln_Yr_Desc = value.ToString();
                        }
                        else if (col.ColumnName.ToLower() == "Fiscal_Hlf_Yr".ToLower())
                        {
                            toReturn.Fiscal_Hlf_Yr = value.ToString();
                        }
                        else if (col.ColumnName.ToLower() == "Fiscal_Hlf_Yr_Desc".ToLower())
                        {
                            toReturn.Fiscal_Hlf_Yr_Desc = value.ToString();
                        }
                        else if (col.ColumnName.ToLower() == "Fiscal_Yr".ToLower())
                        {
                            toReturn.Fiscal_Yr = value.ToString();
                        }
                        else if (col.ColumnName.ToLower() == "Fiscal_Yr_Desc".ToLower())
                        {
                            toReturn.Fiscal_Yr_Desc = value.ToString();
                        }
                        else if (col.ColumnName.ToLower() == "Range_Period".ToLower())
                        {
                            toReturn.Range_Period = value.ToString();
                        }
                        else if (col.ColumnName.ToLower() == "Range_Period_Desc".ToLower())
                        {
                            toReturn.Range_Period_Desc = value.ToString();
                        }
                    }

                    if (toReturn.Day.HasValue && toReturn.Day_Desc.HasValue)
                    {
                        return toReturn;
                    }
                }
            }

            return null;
        }

        //public static double GetNumberOfHolidays(DateTime startDate, DateTime endDate, AppCalendarExDto companyCalendar, AppCalendarExDto userCalendar)
        //{
        //    double nbHolidays = 0;

        //    DateTime cursorDate = startDate;

        //    while (cursorDate < endDate)
        //    {
        //        if (IsHolidayOnCalendar(companyCalendar, userCalendar, cursorDate))
        //        {
        //            nbHolidays++;
        //        }

        //        cursorDate = cursorDate.AddDays(1);
        //    }

        //    return nbHolidays;
        //}


        private static AppCalendarEntity RetrieveOneAppCalendarEntity(object Id)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {

                AppCalendarEntity aEntity = new AppCalendarEntity(int.Parse(Id.ToString()));
                PrefetchPath2 path = new PrefetchPath2(EntityType.AppCalendarEntity);
                path.Add(AppCalendarEntity.PrefetchPathAppCalendarRecurringDay);
                path.Add(AppCalendarEntity.PrefetchPathAppCalendarSpecificDay);

                adpater.FetchEntity(aEntity, path);
                return aEntity;
            }
        }


    }
}