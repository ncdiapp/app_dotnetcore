using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
//using APP.Persistence.Common;

using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using System;
using System.Globalization;
using System.Data.SqlClient;

using NLog.LayoutRenderers;
using System.Text.RegularExpressions;

using APP.Framework;
namespace App.BL
{
    public static class AppSearchConfigBL
    {
        public static AppSearchEntity RetrieveOneAppSearchEntity(object searchId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppSearchEntity aEntity = new AppSearchEntity(int.Parse(searchId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppSearchEntity);

                rootPath.Add(AppSearchEntity.PrefetchPathAppSearchField);
                rootPath.Add(AppSearchEntity.PrefetchPathAppSearchParameter);

                adpater.FetchEntity(aEntity, rootPath);
                return aEntity;
            }
        }


        //internal static AppSearchExDto RetrieveOneAppSearchExDto(object searchId)
        //{

        //	AppSearchEntity appSearchEntity = RetrieveOneAppSearchEntity(searchId);

        //	AppSearchExDto aAppSearchExDto =AppSearchConverter.ConvertEntityToExDto(appSearchEntity);

        //	foreach ( var appSearchFieldEntity in appSearchEntity.AppSearchField )
        //	{

        //		aAppSearchExDto.AppSearchFieldList.Add(AppSearchFieldConverter.ConvertEntityToExDto(appSearchFieldEntity));
        //	}

        //	foreach (var appSearchParameter in appSearchEntity.AppSearchParameter)
        //	{

        //		aAppSearchExDto.AppSearchParameterList.Add(AppSearchParameterConverter.ConvertEntityToExDto(appSearchParameter));
        //	}

        //	return aAppSearchExDto;
        //}


        public static EntityCollection<AppSearchEntity> RetrieveAllSearchEntity(int? searchUsageType = null)
        {
            EntityCollection<AppSearchEntity> list = new EntityCollection<AppSearchEntity>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                RelationPredicateBucket filter = searchUsageType.HasValue
                    ? new RelationPredicateBucket(AppSearchFields.Type == searchUsageType.Value)
                    : null;
                adapter.FetchEntityCollection(list, filter);
            }
            return list;
        }


        internal static CriteriaOperatorDto SetDefaultOperator(IEnumerable<CriteriaOperatorDto> supportedOperator, int operationId)
        {
            foreach (CriteriaOperatorDto aOperator in supportedOperator)
            {
                if ((int)aOperator.OperatorType == operationId)
                {
                    return aOperator;
                }
            }

            return null;
        }

        internal static string CheckIfDefaultValueIsFromSystemDefineToken(SearchCriteriaDto aDto, string defaultVaue)
        {


            if (!string.IsNullOrWhiteSpace(defaultVaue))
            {



                DateTime today = DateTime.Today;

                if (aDto.CriteriaType == EmAppCriteriaType.Date)
                {
                    defaultVaue = AssignDateCriteriaDefaultValueFromToken(aDto, defaultVaue, today);
                }
                else
                {

                    if (defaultVaue == "[" + EmAppSearchCriteriaTokenType.CurrentUser.ToString() + "]")
                    {
                        defaultVaue = AppSecurityUserBL.CurrentUserId.ToString();
                    }
                    else if (defaultVaue == "[" + EmAppSearchCriteriaTokenType.CurrentYear.ToString() + "]"
                        || defaultVaue == "[" + EmAppSearchCriteriaTokenType.CurrentMonth.ToString() + "]"
                        || defaultVaue == "[" + EmAppSearchCriteriaTokenType.CurrentBiWeek.ToString() + "]"
                        || defaultVaue == "[" + EmAppSearchCriteriaTokenType.CurrentWeek.ToString() + "]"
                        )
                    {
                        CalendarBaseDateDto calendarBaseDateDto = AppCalendarBL.FindOneCalendarBaseDate(DateTime.Today);

                        if (calendarBaseDateDto != null)
                        {
                            if (defaultVaue == "[" + EmAppSearchCriteriaTokenType.CurrentYear.ToString() + "]")
                            {
                                //defaultVaue = calendarBaseDateDto.Pln_Yr;
                                defaultVaue = DateTime.Today.Year.ToString();
                            }
                            else if (defaultVaue == "[" + EmAppSearchCriteriaTokenType.CurrentMonth.ToString() + "]")
                            {
                                //defaultVaue = calendarBaseDateDto.Month;
                                defaultVaue = DateTime.Today.Month.ToString();
                            }
                            else if (defaultVaue == "[" + EmAppSearchCriteriaTokenType.CurrentBiWeek.ToString() + "]")
                            {
                                defaultVaue = calendarBaseDateDto.Bi_Week_Desc;
                            }
                            else if (defaultVaue == "[" + EmAppSearchCriteriaTokenType.CurrentWeek.ToString() + "]")
                            {
                                defaultVaue = calendarBaseDateDto.Week;
                            }
                        }
                        else
                        {
                            if (defaultVaue == "[" + EmAppSearchCriteriaTokenType.CurrentYear.ToString() + "]")
                            {
                                defaultVaue = DateTime.Today.Year.ToString();
                            }
                            else if (defaultVaue == "[" + EmAppSearchCriteriaTokenType.CurrentMonth.ToString() + "]")
                            {
                                defaultVaue = DateTime.Today.Month.ToString();
                            }
                        }
                    }
                }
            }

            return defaultVaue;
        }

        private static string AssignDateCriteriaDefaultValueFromToken(SearchCriteriaDto aDto, string defaultVaue, DateTime today)
        {
            CalendarBaseDateDto calendarBaseDateDto = AppCalendarBL.FindOneCalendarBaseDate(DateTime.Today);


            //!!! Need to Redo below logic by CalendarBaseDate Logic

            bool isEndDate = aDto.EmInternalCodeRegistration.HasValue
                                    && aDto.EmInternalCodeRegistration.Value == (int)EmInternalCodeRegistration.CalendarNavigationEndDate;

            if (defaultVaue == "[" + EmAppSearchCriteriaTokenType.CurrentYear.ToString() + "]")
            {
                //!!! Need to Redo below logic by CalendarBaseDate Logic

                DateTime firstDayOfYear = new DateTime(today.Year, 1, 1);
                DateTime lastDayOfYear = new DateTime(today.Year, 12, 31);

                if (isEndDate)
                {
                    defaultVaue = lastDayOfYear.Date.ToString(CultureInfo.CurrentCulture);
                }
                else
                {
                    defaultVaue = firstDayOfYear.Date.ToString(CultureInfo.CurrentCulture);
                }
            }
            else if (defaultVaue == "[" + EmAppSearchCriteriaTokenType.CurrentHalfYear.ToString() + "]")
            {
                //!!! Need to Redo below logic by CalendarBaseDate Logic

                DateTime firstDayOfHalfYear = today.Month <= 6 ? new DateTime(today.Year, 1, 1) : new DateTime(today.Year, 7, 1);
                DateTime lastDayOfHalfYear = today.Month <= 6 ? new DateTime(today.Year, 6, 30) : new DateTime(today.Year, 12, 31);

                if (isEndDate)
                {
                    defaultVaue = lastDayOfHalfYear.Date.ToString(CultureInfo.CurrentCulture);
                }
                else
                {
                    defaultVaue = firstDayOfHalfYear.Date.ToString(CultureInfo.CurrentCulture);
                }
            }
            else if (defaultVaue == "[" + EmAppSearchCriteriaTokenType.CurrentQuarterYear.ToString() + "]")
            {
                //!!! Need to Redo below logic by CalendarBaseDate Logic

                int quarterNumber = (today.Month - 1) / 3 + 1;
                DateTime firstDayOfQuarter = new DateTime(today.Year, (quarterNumber - 1) * 3 + 1, 1);
                DateTime lastDayOfQuarter = firstDayOfQuarter.AddMonths(3).AddDays(-1);

                if (isEndDate)
                {
                    defaultVaue = lastDayOfQuarter.Date.ToString(CultureInfo.CurrentCulture);
                }
                else
                {
                    defaultVaue = firstDayOfQuarter.Date.ToString(CultureInfo.CurrentCulture);
                }
            }
            else if (defaultVaue == "[" + EmAppSearchCriteriaTokenType.CurrentMonth.ToString() + "]")
            {
                DateTime firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
                DateTime lastDayOfMonth = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

                if (isEndDate)
                {
                    defaultVaue = lastDayOfMonth.Date.ToString(CultureInfo.CurrentCulture);
                }
                else
                {
                    defaultVaue = firstDayOfMonth.Date.ToString(CultureInfo.CurrentCulture);
                }
            }
            else if (defaultVaue == "[" + EmAppSearchCriteriaTokenType.CurrentHalfMonth.ToString() + "]")
            {
                DateTime firstDayOfHalfMonth = today.Day <= 15 ? new DateTime(today.Year, today.Month, 1) : new DateTime(today.Year, today.Month, 16);
                DateTime lastDayOfHalfMonth = today.Day <= 15 ? new DateTime(today.Year, today.Month, 15) : new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

                if (isEndDate)
                {
                    defaultVaue = lastDayOfHalfMonth.Date.ToString(CultureInfo.CurrentCulture);
                }
                else
                {
                    defaultVaue = firstDayOfHalfMonth.Date.ToString(CultureInfo.CurrentCulture);
                }
            }
            else if (defaultVaue == "[" + EmAppSearchCriteriaTokenType.CurrentBiWeek.ToString() + "]")
            {

            }
            else if (defaultVaue == "[" + EmAppSearchCriteriaTokenType.CurrentWeek.ToString() + "]")
            {
                DayOfWeek startDayOfWeek = DayOfWeek.Sunday;
                int diff = (7 + (today.DayOfWeek - startDayOfWeek)) % 7;

                DateTime firstDayOfWeek = today.AddDays(-1 * diff).Date;
                DateTime lastDayOfWeek = firstDayOfWeek.AddDays(6);

                if (isEndDate)
                {
                    defaultVaue = lastDayOfWeek.Date.ToString(CultureInfo.CurrentCulture);
                }
                else
                {
                    defaultVaue = firstDayOfWeek.Date.ToString(CultureInfo.CurrentCulture);
                }
            }


            else if (defaultVaue == "[" + EmAppSearchCriteriaTokenType.Today.ToString() + "]")
            {
                defaultVaue = today.Date.ToString(CultureInfo.CurrentCulture);
            }
            else if (defaultVaue == "[" + EmAppSearchCriteriaTokenType.Tomorrow.ToString() + "]")
            {
                defaultVaue = today.Date.AddDays(1).ToString(CultureInfo.CurrentCulture);
            }
            else if (defaultVaue == "[" + EmAppSearchCriteriaTokenType.Yesterday.ToString() + "]")
            {
                defaultVaue = today.Date.AddDays(-1).ToString(CultureInfo.CurrentCulture);
            }
            else if (defaultVaue == "[" + EmAppSearchCriteriaTokenType.Now.ToString() + "]")
            {
                defaultVaue = DateTime.Now.ToString(CultureInfo.CurrentCulture);
            }

            return defaultVaue;
        }

        internal static void SetupDefaultValue(SearchCriteriaDto aDto, string defaultVaue)
        {
            // need to check  CriteriaType before assign default value
            aDto.OriginalDefaultValue = defaultVaue;

            string defaultSystemTokenVaue = CheckIfDefaultValueIsFromSystemDefineToken(aDto, defaultVaue);

            // delitnme token: ,
            List<string> mutipleValueList = new List<string>();

            // defaultVaue is not changed at all
            if (defaultVaue == defaultSystemTokenVaue)
            {
                mutipleValueList = defaultVaue.Split(",".ToArray()).ToList();

                if (mutipleValueList.Count == 1)
                {
                    SetSingDefaultValue(aDto, defaultVaue);
                }
                else // need to setup mutiple default value
                {
                    SetMutipleDefaultValue(aDto, mutipleValueList);
                }
            }
            else // it is change 
            {
                SetSingDefaultValue(aDto, defaultSystemTokenVaue);
            }




        }

        private static void SetMutipleDefaultValue(SearchCriteriaDto aDto, List<string> mutipleValueList)
        {

            if (aDto.CriteriaType == EmAppCriteriaType.Integer)
            {

                foreach (string defaultVaue in mutipleValueList)
                {
                    int? Id = ControlTypeValueConverter.ConvertValueToInt(defaultVaue);
                    if (Id.HasValue)
                    {
                        //aDto.Values.Clear();
                        aDto.Values.Add(Id.Value);
                    }
                }

            }
            else
            {

                if (aDto.CriteriaType == EmAppCriteriaType.Text)
                {
                    foreach (string defaultVaue in mutipleValueList)
                    {
                        aDto.Values.Add(defaultVaue);
                    }

                }

                else if (aDto.CriteriaType == EmAppCriteriaType.Date)
                {
                    foreach (string defaultVaue in mutipleValueList)
                    {
                        var aDateTime = ControlTypeValueConverter.ConvertValueToDate(defaultVaue);
                        if (aDateTime.HasValue)
                        {
                            //aDto.Values.Clear();
                            aDto.Values.Add(aDateTime.Value);
                        }
                    }
                }

                else if (aDto.CriteriaType == EmAppCriteriaType.Numeric)
                {
                    foreach (string defaultVaue in mutipleValueList)
                    {
                        var aDecimal = ControlTypeValueConverter.ConvertValueToDecimal(defaultVaue);
                        if (aDecimal.HasValue)
                        {
                            //aDto.Values.Clear();
                            aDto.Values.Add(aDecimal.Value);
                        }
                    }
                }
                else if (aDto.CriteriaType == EmAppCriteriaType.Boolean)
                {
                    foreach (string defaultVaue in mutipleValueList)
                    {
                        var aBoolean = ControlTypeValueConverter.ConvertValueToBoolean(defaultVaue);
                        if (aBoolean.HasValue)
                        {
                            //aDto.Values.Clear();
                            aDto.Values.Add(aBoolean.Value);
                        }
                    }
                }

                else if (aDto.CriteriaType == EmAppCriteriaType.Entity)
                {
                    foreach (string defaultVaue in mutipleValueList)
                    {
                        var id = ControlTypeValueConverter.ConvertValueToInt(defaultVaue);
                        if (id.HasValue)
                        {
                            aDto.Values.Add(id.Value);


                        }
                        else // for text DDL devaule search
                        {
                            aDto.Values.Add(defaultVaue);
                        }
                    }
                }

                else if (aDto.CriteriaType == EmAppCriteriaType.FolderTree)
                {
                    foreach (string defaultVaue in mutipleValueList)
                    {
                        var id = ControlTypeValueConverter.ConvertValueToInt(defaultVaue);
                        if (id.HasValue)
                        {
                            aDto.Values.Add(id.Value);


                        }
                    }
                }
            }

        }


        private static void SetSingDefaultValue(SearchCriteriaDto aDto, string defaultVaue)
        {
            if (aDto.CriteriaType != EmAppCriteriaType.Integer)
            {
                if (aDto.CriteriaType == EmAppCriteriaType.Text)
                {
                    //aDto.Values.Clear();
                    aDto.Values.Add(defaultVaue);
                }

                else if (aDto.CriteriaType == EmAppCriteriaType.Date)
                {
                    var aDateTime = ControlTypeValueConverter.ConvertValueToDate(defaultVaue);
                    if (aDateTime.HasValue)
                    {
                        //aDto.Values.Clear();
                        aDto.Values.Add(aDateTime.Value);
                    }
                }

                else if (aDto.CriteriaType == EmAppCriteriaType.Numeric)
                {
                    var aDecimal = ControlTypeValueConverter.ConvertValueToDecimal(defaultVaue);
                    if (aDecimal.HasValue)
                    {
                        //aDto.Values.Clear();
                        aDto.Values.Add(aDecimal.Value);
                    }
                }
                else if (aDto.CriteriaType == EmAppCriteriaType.Boolean)
                {
                    var aBoolean = ControlTypeValueConverter.ConvertValueToBoolean(defaultVaue);
                    if (aBoolean.HasValue)
                    {
                        //aDto.Values.Clear();
                        aDto.Values.Add(aBoolean.Value);
                    }
                }

                else if (aDto.CriteriaType == EmAppCriteriaType.Entity)
                {
                    var id = ControlTypeValueConverter.ConvertValueToInt(defaultVaue);
                    if (id.HasValue)
                    {
                        aDto.Values.Add(id.Value);


                    }
                    else // for text DDL devaule search
                    {
                        aDto.Values.Add(defaultVaue);
                    }
                }

                else if (aDto.CriteriaType == EmAppCriteriaType.FolderTree)
                {
                    var id = ControlTypeValueConverter.ConvertValueToInt(defaultVaue);
                    if (id.HasValue)
                    {
                        aDto.Values.Add(id.Value);


                    }
                }
            }
            else
            {
                int? Id = ControlTypeValueConverter.ConvertValueToInt(defaultVaue);
                if (Id.HasValue)
                {
                    //aDto.Values.Clear();
                    aDto.Values.Add(Id.Value);
                }
            }
        }

        public static SearchDto ConvertSearchEntitySearchDto(AppSearchEntity appSearchEntity)
        {
            SearchDto aSearchDTO = GetSearchDtoSetupDefaultSearchCretia(appSearchEntity);

            aSearchDTO.Display = AppLocalizeSystemLableBL.GetSearchLabel(aSearchDTO.Id, aSearchDTO.Display);

            if (appSearchEntity.SearchViewId.HasValue)
            {
                AppSearchViewEntity aView = AppSearchViewConfigBL.RetrieveOneAppSearchViewEntity(appSearchEntity.SearchViewId);

                aSearchDTO.DefaultView = AppSearchViewConfigBL.ConvertReverenceViewEntityToReferenceViewDto(aView);
            }

            aSearchDTO.IsForPublicAcesss = appSearchEntity.IsForPublicAcesss;


            aSearchDTO.CriteriasRowCount = aSearchDTO.Criterias.Select(o => o.RowIndex).DefaultIfEmpty(0).Max();

            AppIntergrationSettingExDto publicApiBuilderExDto = AppIntergrationSettingBL.TryRetrieveAppBuiltInProviderExDto();
            if (publicApiBuilderExDto?.AppIntergrationSettingParameterList != null)
            {
                foreach (var publicApiDto in publicApiBuilderExDto.AppIntergrationSettingParameterList)
                {
                    if (publicApiDto.TranscationFieId.HasValue && publicApiDto.TranscationFieId.Value == appSearchEntity.SearchId
                        && publicApiDto.HttpMethd == "Get")
                    {
                        aSearchDTO.DefaultAppProvideApiId = (int)publicApiDto.Id;
                    }
                }
            }

            return aSearchDTO;
        }

        public static SearchDto GetSearchDtoSetupDefaultSearchCretia(AppSearchEntity appSearchEntity)
        {
            SearchDto aSearchDTO = new SearchDto();
            aSearchDTO.Id = appSearchEntity.SearchId;
            aSearchDTO.Display = appSearchEntity.Name.ToString();
            aSearchDTO.SearchType = appSearchEntity.Type;
            aSearchDTO.TechPackTypeId = appSearchEntity.Type;
            aSearchDTO.IsAutoExcute = appSearchEntity.IsAutoExecute;
            aSearchDTO.WhereUsedSearchId = appSearchEntity.WhereUsedSearchId;
            aSearchDTO.IsSavedSearch = false;
            //aSearchDTO.FolderTransactionId = appSearchEntity.FolderTransactionId;
            aSearchDTO.FolderTransactionId = appSearchEntity.FolderTransactionId;

            aSearchDTO.IsStaticBuiltInSearch = appSearchEntity.IsBuiltIn;
            aSearchDTO.BlqueryId = appSearchEntity.DataSetId;
            aSearchDTO.IsHideAllToolsBar = appSearchEntity.IsHideAllToolsBar;
            aSearchDTO.IsShowSearchTitleLabel = false;
            // aSearchDTO.qu

            List<SearchCriteriaDto> aList = new List<SearchCriteriaDto>();
            aSearchDTO.Criterias = aList;

            // List<int> ignoreFileterSearchIds = AppSecurityManagementBL.RetrieveUserIgnoreFilterByCurrentUserSearchIds();

            foreach (var searchField in appSearchEntity.AppSearchField)
            {
                if (searchField.IsVisible)
                {
                    aSearchDTO.IsShowSearchTitleLabel = true;
                }

                SearchCriteriaDto aDto = new SearchCriteriaDto();
                aDto.RowIndex = searchField.PositionRow.HasValue ? searchField.PositionRow.Value : 0; ;
                aDto.ColumnIndex = searchField.PositionColumn.HasValue ? searchField.PositionColumn.Value : 0;
                aDto.SearcDCUID = searchField.SearchFielDid;
                aDto.IsAutoPopulate = searchField.IsAutoPopulate;
                aDto.IsReadOnly = searchField.IsReadOnly;

                aDto.IsChangedAutoExecute = searchField.IsChangedAutoExecute;
                aDto.StartValueEntityField = searchField.StartValueEntityField;
                aDto.EndValueEntityField = searchField.EndValueEntityField;
                aDto.StartValueDataSetField = searchField.StartValueDataSetField;
                aDto.EndValueDataSetField = searchField.EndValueDataSetField;
                aDto.EmInternalCodeRegistration = searchField.EmInternalCodeRegistration;
                aDto.IsVisible = searchField.IsVisible;







                if (string.IsNullOrEmpty(searchField.DisplayText))
                {
                    aDto.Display = searchField.SysTableFiledPath;
                }
                else
                {
                    aDto.Display = searchField.DisplayText;
                }

                aDto.SysTableFiledPath = searchField.SysTableFiledPath;
                aDto.IsSkipSearch = searchField.IsSkipSearch;

                //aDto.IsUseAsCandarNavigator = searchField.IsUseAsCandarNavigator ;

                aDto.IsAllowMultipleSelect = searchField.IsAllowMultipleSelect;
                aDto.IsChangedAutoExecute = searchField.IsChangedAutoExecute;
                aDto.ControlType = searchField.ControlType;

                EmAppCriteriaType aCriteriaType = EmAppCriteriaType.Text;
                if (searchField.ControlType.HasValue)
                {
                    aCriteriaType = GetCriteriaTypeByControlType(searchField.ControlType.Value);
                }

                aDto.CriteriaType = aCriteriaType;
                aDto.CriteriaSubType = searchField.SubControlType;

                if (aCriteriaType == EmAppCriteriaType.Entity && searchField.EntityId.HasValue)
                {
                    aDto.ItemsSource = AppEntityInfoBL.GetLookupItemList(searchField.EntityId.Value, "");

                    bool isAdmin = AppSecurityUserBL.IsAdminUser();

                    // Filter DDL ItemSource By Current User
                    if (!isAdmin)
                    {
                        if (!string.IsNullOrWhiteSpace(appSearchEntity.FilterByCurrentUserMappingField)
                            && appSearchEntity.FilterByCurrentUserMappingField == searchField.SysTableFiledPath)
                        {
                            //if (!ignoreFileterSearchIds.Contains(appSearchEntity.SearchId)) // need to filter by current uses
                            //{
                            aDto.ItemsSource = aDto.ItemsSource.Where(o => (int)o.Id == AppSecurityUserBL.CurrentUserEntity.UserId).ToList();
                            aDto.Value = AppSecurityUserBL.CurrentUserEntity.UserId;
                            aDto.IsReadOnly = true;
                            //}
                        }
                    }
                }



                SetupCriteraDtoSupportOperatorAndDefaultValue(searchField, aDto);

                aList.Add(aDto);
            }

            if (aSearchDTO.FolderTransactionId.HasValue)
            {
                AppSefolderDto[] folderHairarchy = AppSeFolderBL.RetrieveFolderHairarchyDto(aSearchDTO.FolderTransactionId.Value);
                List<AppSefolderDto> flatFolderList = AppSeFolderBL.ConvertFolderHairarchyToFlatList(folderHairarchy);
                aSearchDTO.DictFolderIdFolderDisplay = flatFolderList.ToDictionary(o => (int)o.Id, o => o.FolderPath);
            }

            return aSearchDTO;
        }


        private static void SetupCriteraDtoSupportOperatorAndDefaultValue(AppSearchFieldEntity aDCU, SearchCriteriaDto aDto)
        {
            aDto.SupportedOperators = SetupSurportedCriteriaOperators(aDto.CriteriaType, aDto.ControlType);

            aDto.CriteriaOperator = SetupDefaultOperator(aDCU, aDto.SupportedOperators);

            if (!string.IsNullOrEmpty(aDCU.DefaultValue))
            {
                if (!string.IsNullOrEmpty(aDCU.DefaultValue.ToString()))
                {
                    SetupDefaultValue(aDto, aDCU.DefaultValue.ToString());
                }
            }
        }


        private static CriteriaOperatorDto SetupDefaultOperator(AppSearchFieldEntity aDCU, IEnumerable<CriteriaOperatorDto> supportedOperator)
        {
            foreach (CriteriaOperatorDto aOperator in supportedOperator)
            {
                if (aDCU.OperationId.HasValue)
                {
                    if ((int)aOperator.OperatorType == aDCU.OperationId.Value)
                        return aOperator;
                }
            }

            return null;
        }

        private static List<CriteriaOperatorDto> SetupSurportedCriteriaOperators(EmAppCriteriaType aCriteriaType, int? controlType)
        {
            List<CriteriaOperatorDto> list = new List<CriteriaOperatorDto>();

            switch (aCriteriaType)
            {
                case EmAppCriteriaType.Boolean:
                    list.Add(CriteriaOperators.EqualsOp);
                    list.Add(CriteriaOperators.NotNullOrEmpty);
                    list.Add(CriteriaOperators.NullOrEmpty);
                    break;
                case EmAppCriteriaType.Date:
                    //if (controlType.HasValue && controlType.Value == (int)EmAppControlType.DateTimeDetail)
                    //{                        
                    //    list.Add(CriteriaOperators.LessThan);
                    //    list.Add(CriteriaOperators.GreaterThan);
                    //    list.Add(CriteriaOperators.GreaterThanOrEquals);
                    //    list.Add(CriteriaOperators.LessThanOrEquals);
                    //    //list.Add(CriteriaOperators.NotEqual);
                    //    list.Add(CriteriaOperators.NotNullOrEmpty);
                    //    list.Add(CriteriaOperators.NullOrEmpty);
                    //    // list.Add(CriteriaOperators.Between);
                    //}
                    //else
                    //{
                    //    list.Add(CriteriaOperators.EqualsOp);
                    //    list.Add(CriteriaOperators.LessThan);
                    //    list.Add(CriteriaOperators.GreaterThan);
                    //    list.Add(CriteriaOperators.GreaterThanOrEquals);
                    //    list.Add(CriteriaOperators.LessThanOrEquals);
                    //    //list.Add(CriteriaOperators.NotEqual);
                    //    list.Add(CriteriaOperators.NotNullOrEmpty);
                    //    list.Add(CriteriaOperators.NullOrEmpty);
                    //    // list.Add(CriteriaOperators.Between);
                    //}


                    list.Add(CriteriaOperators.EqualsOp);
                    list.Add(CriteriaOperators.LessThan);
                    list.Add(CriteriaOperators.GreaterThan);
                    list.Add(CriteriaOperators.GreaterThanOrEquals);
                    list.Add(CriteriaOperators.LessThanOrEquals);
                    //list.Add(CriteriaOperators.NotEqual);
                    list.Add(CriteriaOperators.NotNullOrEmpty);
                    list.Add(CriteriaOperators.NullOrEmpty);
                    // list.Add(CriteriaOperators.Between);
                    break;
                case EmAppCriteriaType.Entity:
                    list.Add(CriteriaOperators.EqualsOp);
                    list.Add(CriteriaOperators.NotEqual);
                    list.Add(CriteriaOperators.In);
                    list.Add(CriteriaOperators.NotNull);
                    list.Add(CriteriaOperators.Null);
                    break;
                case EmAppCriteriaType.Numeric:
                    list.Add(CriteriaOperators.EqualsOp);
                    list.Add(CriteriaOperators.LessThan);
                    list.Add(CriteriaOperators.GreaterThan);
                    list.Add(CriteriaOperators.GreaterThanOrEquals);
                    list.Add(CriteriaOperators.LessThanOrEquals);
                    //list.Add(CriteriaOperators.NotEqual);
                    list.Add(CriteriaOperators.NotNullOrEmpty);
                    list.Add(CriteriaOperators.NullOrEmpty);
                    //  list.Add(CriteriaOperators.Between);
                    break;
                case EmAppCriteriaType.Integer:
                    list.Add(CriteriaOperators.EqualsOp);
                    list.Add(CriteriaOperators.LessThan);
                    list.Add(CriteriaOperators.GreaterThan);
                    list.Add(CriteriaOperators.GreaterThanOrEquals);
                    list.Add(CriteriaOperators.LessThanOrEquals);
                    //list.Add(CriteriaOperators.NotEqual);
                    //list.Add(CriteriaOperators.NotNullOrEmpty);
                    //list.Add(CriteriaOperators.NullOrEmpty);
                    break;
                case EmAppCriteriaType.Text:
                    list.Add(CriteriaOperators.Like);
                    list.Add(CriteriaOperators.EqualsOp);
                    //list.Add(CriteriaOperators.NotEqual);
                    list.Add(CriteriaOperators.NotNullOrEmpty);
                    list.Add(CriteriaOperators.NullOrEmpty);
                    break;
                case EmAppCriteriaType.FolderTree:
                    list.Add(CriteriaOperators.EqualsOp);
                    //list.Add(CriteriaOperators.NotEqual);
                    list.Add(CriteriaOperators.NotNullOrEmpty);
                    list.Add(CriteriaOperators.NullOrEmpty);
                    break;
                default:
                    list.Add(CriteriaOperators.EqualsOp);
                    list.Add(CriteriaOperators.In);
                    list.Add(CriteriaOperators.LessThan);
                    list.Add(CriteriaOperators.GreaterThan);
                    list.Add(CriteriaOperators.GreaterThanOrEquals);
                    list.Add(CriteriaOperators.LessThanOrEquals);
                    //list.Add(CriteriaOperators.NotEqual);
                    list.Add(CriteriaOperators.NotNullOrEmpty);
                    list.Add(CriteriaOperators.NullOrEmpty);
                    break;
            }

            return list;
        }


        private static EmAppCriteriaType GetCriteriaTypeByControlType(int controlType)
        {
            if (controlType == (int)EmAppControlType.Memo

               || controlType == (int)EmAppControlType.TextBox

               || controlType == (int)EmAppControlType.AutoGeneration

                )
            {
                return EmAppCriteriaType.Text;
            }

            else if (controlType == (int)EmAppControlType.DDL || controlType == (int)EmAppControlType.AutoComplete || controlType == (int)EmAppControlType.SearchAbleDDL)
            {
                return EmAppCriteriaType.Entity;
            }

            else if (controlType == (int)EmAppControlType.CheckBox)
            {
                return EmAppCriteriaType.Boolean;
            }
            else if (controlType == (int)EmAppControlType.Date

                || controlType == (int)EmAppControlType.Time

                || controlType == (int)EmAppControlType.DateTimeDetail

                )
            {
                return EmAppCriteriaType.Date;
            }
            else if (controlType == (int)EmAppControlType.Numeric)
            {
                return EmAppCriteriaType.Numeric;
            }
            else if (controlType == (int)EmAppControlType.FolderTree)
            {
                return EmAppCriteriaType.FolderTree;
            }
            else if (controlType == (int)EmAppControlType.GoogleAddress)
            {
                return EmAppCriteriaType.GoogleAddress;
            }
            return EmAppCriteriaType.Text;
        }

        public static OperationCallResult<bool> SetSearchForPublicAccess(int searchId)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppSearchEntity entity = new AppSearchEntity();
            entity.IsForPublicAcesss = true;

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.UpdateEntitiesDirectly(entity, new RelationPredicateBucket(AppSearchFields.SearchId == searchId));


                    adapter.Commit();

                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_UpdateSearch_Ok", ValidationItemType.Message, "Update Search Success."));

                    aOperationCallResult.Object = true;


                }
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchEntity), "App_SearchEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<AppSearchExDto> SaveAsSearch(object searchId)
        {
            AppSearchExDto aAppSearchExDto = RetrieveOneAppSearchExDto(searchId);

            aAppSearchExDto.Id = null;

            aAppSearchExDto.Name = "Copy from" + aAppSearchExDto.Name;

            return SaveAppSearchExDto(aAppSearchExDto);



        }

        public static AppSearchExDto RetrieveOneAppSearchExDto(object searchId)
        {
            AppSearchEntity aAppSearchEntity = RetrieveOneAppSearchEntity(searchId);

            AppSearchExDto aAppSearchDto = AppSearchConverter.ConvertEntityToExDto(aAppSearchEntity);


            foreach (var o in aAppSearchEntity.AppSearchField)
            {
                AppSearchFieldExDto aAppSearchFieldExDto = AppSearchFieldConverter.ConvertEntityToExDto(o);
                aAppSearchDto.AppSearchFieldList.Add(aAppSearchFieldExDto);
            }

            foreach (var o in aAppSearchEntity.AppSearchParameter)
            {
                AppSearchParameterExDto aAppSearchFieldExDto = AppSearchParameterConverter.ConvertEntityToExDto(o);
                aAppSearchDto.AppSearchParameterList.Add(aAppSearchFieldExDto);
            }


            if (aAppSearchDto.Type == (int)EmAppSearchUsageType.EshopCategorySearch)
            {
                if (aAppSearchDto.SearchViewId.HasValue)
                {
                    int viewId = aAppSearchDto.SearchViewId.Value;
                    aAppSearchDto.DefaultSearchViewExDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(viewId);


                    int? targetSearchId = aAppSearchDto.DefaultSearchViewExDto.EshopCardViewSearchId;

                    if (targetSearchId.HasValue)
                    {
                        aAppSearchDto.EshopCardSearchExDto = RetrieveOneAppSearchExDto(targetSearchId.Value);

                        if (aAppSearchDto.EshopCardSearchExDto.SearchViewId.HasValue)
                        {
                            aAppSearchDto.EshopCardSearchExDto.DefaultSearchViewExDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(aAppSearchDto.EshopCardSearchExDto.SearchViewId.Value);
                        }
                    }

                }

            }
            if (aAppSearchDto.DataSetId.HasValue)
            {
                try
                {
                    AppDataSetEntity aAppDataSetEntity = AppDataSetBL.RetrieveOneAppDataSetEntity(aAppSearchDto.DataSetId.Value);

                    if (aAppDataSetEntity.QueryType.HasValue)
                    {
                        int queryType = aAppDataSetEntity.QueryType.Value;
                        aAppSearchDto.DataServiceType = queryType;
                        aAppSearchDto.DataServiceTypeDisplay = ((EmAppDataServiceType)queryType).ToString();
                        aAppSearchDto.DataServiceTypeDisplay = Regex.Replace(aAppSearchDto.DataServiceTypeDisplay, "([a-z])([A-Z])", "$1 $2");

                    }
                }
                catch (Exception ex)
                {

                }
            }

            return aAppSearchDto;
        }



        public static List<AppSearchDto> RetrieveAllAppSearchDto(int? searchUsageType = null)
        {
            EntityCollection<AppSearchEntity> list = RetrieveAllSearchEntity(searchUsageType);

            Dictionary<int, AppDataSetExDto> dictDataSetIdAndDto = AppDataSetBL.RetrieveAllAppDataSetEntityDto().ToDictionary(o => (int)o.Id, o => o);

            var aDtoList = new List<AppSearchDto>();

            foreach (var o in list)
            {
                var searchDto = AppSearchConverter.ConvertEntityToDto(o);

                if (searchDto.DataSetId.HasValue && dictDataSetIdAndDto.ContainsKey(searchDto.DataSetId.Value))
                {
                    var datasetDto = dictDataSetIdAndDto[searchDto.DataSetId.Value];
                    searchDto.DataSourceFrom = datasetDto.DataSourceFrom;

                    if (datasetDto.QueryType.HasValue)
                    {
                        int queryType = datasetDto.QueryType.Value;
                        searchDto.DataServiceType = datasetDto.QueryType;

                        try
                        {
                            searchDto.DataServiceTypeDisplay = ((EmAppDataServiceType)queryType).ToString();
                            searchDto.DataServiceTypeDisplay = Regex.Replace(searchDto.DataServiceTypeDisplay, "([a-z])([A-Z])", "$1 $2");
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }

                aDtoList.Add(searchDto);
            }

            return aDtoList;
        }


        public static List<AppSearchDto> RetrieveSaasApplicationSearchList(int? applicationId, int? usageType = null)
        {
            List<AppSearchDto> toReturn = new List<AppSearchDto>();

            if (applicationId.HasValue)
            {
                List<AppSearchDto> allSearchList = RetrieveAllAppSearchDto();
                EntityCollection<AppApplicationAssetsItemEntity> list = AppSaasUserApplicationPackageBL.RetrieveAppApplicationAssetsItemEntityListByType(applicationId.Value, (int)EmAppApplicationAssetsType.Search);

                List<int> importedSearchIdList = list.Where(o => o.SearchId.HasValue).Select(o => o.SearchId.Value).Distinct().ToList();

                toReturn = allSearchList.Where(o => (o.SaasApplicationId.HasValue && o.SaasApplicationId.Value == applicationId.Value) || importedSearchIdList.Contains((int)o.Id)).ToList();

                if (usageType.HasValue)
                {
                    toReturn = toReturn.Where(o => (o.Type == usageType.Value)).ToList();
                }

            }

            return toReturn;
        }



        public static OperationCallResult<AppSearchExDto> SaveAppSearchExDto(AppSearchExDto aAppSearchExDto)
        {
            OperationCallResult<AppSearchExDto> aOperationCallResult = new OperationCallResult<AppSearchExDto>();
            var aValidationResult = aAppSearchExDto.ValidateDto();
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }


            aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppSearchEntity aAppSearchEntity;

            foreach (var searchFieldDto in aAppSearchExDto.AppSearchFieldList)
            {
                if (string.IsNullOrEmpty(searchFieldDto.DisplayText))
                {
                    searchFieldDto.DisplayText = searchFieldDto.SysTableFiledPath;
                }

                if (searchFieldDto.ControlType.HasValue
                    && searchFieldDto.ControlType.Value != (int)EmAppControlType.DDL
                    && searchFieldDto.ControlType.Value != (int)EmAppControlType.AutoComplete
                    && searchFieldDto.ControlType.Value != (int)EmAppControlType.SearchAbleDDL)
                {
                    searchFieldDto.EntityId = null;
                }
            }

            // prepare Data
            if (aAppSearchExDto.IsNew)
            {
                if (aAppSearchExDto.DataSetId.HasValue)
                {
                    if (aAppSearchExDto.NeedToCreateDefaultViewWithAllDataSetColumns)
                    {
                        AppSearchViewExDto searchViewExDto = new AppSearchViewExDto();
                        searchViewExDto.DataSetId = aAppSearchExDto.DataSetId;
                        searchViewExDto.Name = aAppSearchExDto.Name;
                        searchViewExDto.NoSecurity = false;
                        searchViewExDto.GridOutputMode = 1;
                        searchViewExDto.Options = 0;
                        searchViewExDto.ViewType = (int)EmAppViewType.GridView;
                        searchViewExDto.ColumnCount = 0;
                        searchViewExDto.RowPerPage = 0;
                        searchViewExDto.IsEnableCalendarMonthView = true;
                        searchViewExDto.IsEnableCalendarWeekView = true;
                        searchViewExDto.IsEnableCalendarDayView = true;
                        searchViewExDto.IsEnableCalendarNavigator = true;
                        searchViewExDto.IsDisableClientTimeConvert = false;

                        searchViewExDto.AppSearchViewFieldList = new ObservableSet<AppSearchViewFieldExDto>();

                        AppSearchViewConfigBL.AddAllDataSetColumnToSearchViewField(searchViewExDto, true);

                        OperationCallResult<AppSearchViewExDto> saveSearchViewResult = AppSearchViewConfigBL.SaveAppSearchViewExDto(searchViewExDto);

                        if (saveSearchViewResult.IsSuccessfulWithResult)
                        {
                            aAppSearchExDto.SearchViewId = (int)saveSearchViewResult.Object.Id;
                        }
                        else
                        {
                            aValidationResult.Merge(saveSearchViewResult.ValidationResult);
                        }
                    }
                    else if (aAppSearchExDto.SearchViewId == 0)
                    {
                        OperationCallResult<AppSearchViewExDto> saveSearchViewResult = CreateDefaultBlankView(aAppSearchExDto);

                        if (saveSearchViewResult.IsSuccessfulWithResult)
                        {
                            aAppSearchExDto.SearchViewId = (int)saveSearchViewResult.Object.Id;
                        }
                        else
                        {
                            aValidationResult.Merge(saveSearchViewResult.ValidationResult);
                        }
                    }

                }

                aAppSearchEntity = new AppSearchEntity();
                AppSearchConverter.CopyDtoToEntity(aAppSearchEntity, aAppSearchExDto);

                foreach (var searchTemplateSubitemDto in aAppSearchExDto.AppSearchFieldList)
                {
                    AppSearchFieldEntity aAppSearchFieldEntity = new AppSearchFieldEntity();
                    AppSearchFieldConverter.CopyDtoToEntity(aAppSearchFieldEntity, searchTemplateSubitemDto);
                    aAppSearchEntity.AppSearchField.Add(aAppSearchFieldEntity);
                }


                foreach (var searchParamterDto in aAppSearchExDto.AppSearchParameterList)
                {
                    AppSearchParameterEntity aAppSearchFieldEntity = new AppSearchParameterEntity();
                    AppSearchParameterConverter.CopyDtoToEntity(aAppSearchFieldEntity, searchParamterDto);
                    aAppSearchEntity.AppSearchParameter.Add(aAppSearchFieldEntity);
                }



                if (!aValidationResult.HasErrors)
                {
                    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                    {
                        try
                        {
                            adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                            adapter.SaveEntity(aAppSearchEntity);
                            adapter.Commit();

                            aAppSearchExDto.Id = aAppSearchEntity.SearchId;
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "App_SearchEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                        }


                        // Database FK Exeption ........
                        catch (ORMQueryExecutionException ex)
                        {
                            adapter.Rollback();
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "App_SearchEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                        }
                    }
                }
            }

            else if (aAppSearchExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(ProcessDirtyAppSearchExDto(aAppSearchExDto));

                if (!aValidationResult.HasErrors)
                {
                    if (aAppSearchExDto.IsNeedToSynchronizeDefaultViewDataSetId)
                    {
                        if (aAppSearchExDto.SearchViewId.HasValue)
                        {
                            var searchViewExDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(aAppSearchExDto.SearchViewId.Value);
                            searchViewExDto.DataSetId = aAppSearchExDto.DataSetId;

                            var updateSearchViewResult = AppSearchViewConfigBL.SaveAppSearchViewExDto(searchViewExDto);                            
                        }
                    }
                }
            }

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppSearchExDto(aAppSearchExDto.Id);
            }

            return aOperationCallResult;
        }


        public static AppSearchFieldEntity RetrieveOneAppSearchFieldEntity(object searchFieldId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppSearchFieldEntity aEntity = new AppSearchFieldEntity(int.Parse(searchFieldId.ToString()));


                adpater.FetchEntity(aEntity);
                return aEntity;
            }
        }


        public static AppSearchFieldExDto RetrieveOneAppSearchFieldExDto(object searchFieldId)
        {
            AppSearchFieldEntity aAppSearchFieldEntity = RetrieveOneAppSearchFieldEntity(searchFieldId);

            AppSearchFieldExDto aAppSearchFieldDto = AppSearchFieldConverter.ConvertEntityToExDto(aAppSearchFieldEntity);


            return aAppSearchFieldDto;
        }


        public static OperationCallResult<AppSearchFieldExDto> SaveAppSearchFieldExDto(AppSearchFieldExDto aAppSearchFieldExDto)
        {
            OperationCallResult<AppSearchFieldExDto> aOperationCallResult = new OperationCallResult<AppSearchFieldExDto>();

            var aValidationResult = aAppSearchFieldExDto.ValidateDto();
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }


            aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppSearchFieldEntity aAppSearchFieldEntity;


            if (string.IsNullOrEmpty(aAppSearchFieldExDto.DisplayText))
            {
                aAppSearchFieldExDto.DisplayText = aAppSearchFieldExDto.SysTableFiledPath;
            }

            if (aAppSearchFieldExDto.ControlType.HasValue
                && aAppSearchFieldExDto.ControlType.Value != (int)EmAppControlType.DDL
                && aAppSearchFieldExDto.ControlType.Value != (int)EmAppControlType.AutoComplete
                && aAppSearchFieldExDto.ControlType.Value != (int)EmAppControlType.SearchAbleDDL)
            {
                aAppSearchFieldExDto.EntityId = null;
            }


            // prepare Data
            if (aAppSearchFieldExDto.IsNew)
            {
                aAppSearchFieldEntity = new AppSearchFieldEntity();
                AppSearchFieldConverter.CopyDtoToEntity(aAppSearchFieldEntity, aAppSearchFieldExDto);


                if (!aValidationResult.HasErrors)
                {
                    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                    {
                        try
                        {
                            adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                            adapter.SaveEntity(aAppSearchFieldEntity);
                            adapter.Commit();

                            aAppSearchFieldExDto.Id = aAppSearchFieldEntity.SearchFielDid;
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchFieldExDto), "App_SearchEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                        }


                        // Database FK Exeption ........
                        catch (ORMQueryExecutionException ex)
                        {
                            adapter.Rollback();
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchFieldExDto), "App_SearchEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                        }
                    }
                }
            }

            else if (aAppSearchFieldExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(ProcessDirtyAppSearchFieldExDto(aAppSearchFieldExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppSearchFieldExDto(aAppSearchFieldExDto.Id);
            }

            return aOperationCallResult;
        }




        public static OperationCallResult<AppSearchExDto> SaveEshopCardSearchExDto(AppSearchExDto aAppSearchExDto)
        {
            OperationCallResult<AppSearchExDto> aOperationCallResult = new OperationCallResult<AppSearchExDto>();
            var aValidationResult = aAppSearchExDto.ValidateDto();
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }


            aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppSearchEntity aAppSearchEntity;

            foreach (var searchFieldDto in aAppSearchExDto.AppSearchFieldList)
            {
                if (string.IsNullOrEmpty(searchFieldDto.DisplayText))
                {
                    searchFieldDto.DisplayText = searchFieldDto.SysTableFiledPath;
                }

                if (searchFieldDto.ControlType.HasValue
                    && searchFieldDto.ControlType.Value != (int)EmAppControlType.DDL
                    && searchFieldDto.ControlType.Value != (int)EmAppControlType.AutoComplete
                    && searchFieldDto.ControlType.Value != (int)EmAppControlType.SearchAbleDDL)
                {
                    searchFieldDto.EntityId = null;
                }
            }

            // prepare Data
            if (aAppSearchExDto.IsNew)
            {
                if (aAppSearchExDto.DataSetId.HasValue)
                {

                    AppSearchViewExDto searchViewExDto = new AppSearchViewExDto();
                    searchViewExDto.DataSetId = aAppSearchExDto.DataSetId;
                    searchViewExDto.Name = aAppSearchExDto.Name;
                    searchViewExDto.NoSecurity = false;
                    searchViewExDto.GridOutputMode = 1;
                    searchViewExDto.Options = 0;
                    searchViewExDto.ViewType = (int)EmAppViewType.EShopCardView;
                    searchViewExDto.ColumnCount = 0;
                    searchViewExDto.RowPerPage = 0;
                    searchViewExDto.IsEnableCalendarMonthView = true;
                    searchViewExDto.IsEnableCalendarWeekView = true;
                    searchViewExDto.IsEnableCalendarDayView = true;
                    searchViewExDto.IsEnableCalendarNavigator = true;
                    searchViewExDto.IsDisableClientTimeConvert = false;
                    searchViewExDto.OtherSettingsDto = new AppSearchViewOtherSettingsDto();


                    searchViewExDto.AppSearchViewFieldList = new ObservableSet<AppSearchViewFieldExDto>();

                    //AppSearchViewConfigBL.AddAllDataSetColumnToSearchViewField(searchViewExDto, true);

                    OperationCallResult<AppSearchViewExDto> saveSearchViewResult = AppSearchViewConfigBL.SaveAppSearchViewExDto(searchViewExDto);

                    if (saveSearchViewResult.IsSuccessfulWithResult)
                    {
                        aAppSearchExDto.SearchViewId = (int)saveSearchViewResult.Object.Id;
                    }
                    else
                    {
                        aValidationResult.Merge(saveSearchViewResult.ValidationResult);
                    }


                }

                aAppSearchEntity = new AppSearchEntity();
                AppSearchConverter.CopyDtoToEntity(aAppSearchEntity, aAppSearchExDto);

                foreach (var searchTemplateSubitemDto in aAppSearchExDto.AppSearchFieldList)
                {
                    AppSearchFieldEntity aAppSearchFieldEntity = new AppSearchFieldEntity();
                    AppSearchFieldConverter.CopyDtoToEntity(aAppSearchFieldEntity, searchTemplateSubitemDto);
                    aAppSearchEntity.AppSearchField.Add(aAppSearchFieldEntity);
                }


                foreach (var searchParamterDto in aAppSearchExDto.AppSearchParameterList)
                {
                    AppSearchParameterEntity aAppSearchFieldEntity = new AppSearchParameterEntity();
                    AppSearchParameterConverter.CopyDtoToEntity(aAppSearchFieldEntity, searchParamterDto);
                    aAppSearchEntity.AppSearchParameter.Add(aAppSearchFieldEntity);
                }



                if (!aValidationResult.HasErrors)
                {
                    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                    {
                        try
                        {
                            adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                            adapter.SaveEntity(aAppSearchEntity);
                            adapter.Commit();

                            aAppSearchExDto.Id = aAppSearchEntity.SearchId;
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "App_SearchEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                        }


                        // Database FK Exeption ........
                        catch (ORMQueryExecutionException ex)
                        {
                            adapter.Rollback();
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "App_SearchEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                        }
                    }
                }
            }

            else if (aAppSearchExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(ProcessDirtyAppSearchExDto(aAppSearchExDto));

                if (!aValidationResult.HasErrors)
                {
                    if (aAppSearchExDto.DefaultSearchViewExDto != null)
                    {
                        if (aAppSearchExDto.DefaultSearchViewExDto.NeedToSaveAsFromEshopProductBaseDataModelId.HasValue)
                        {
                            var saveAsResult = AppTransactionSaveAsBL.SaveAsAppTransaction(aAppSearchExDto.DefaultSearchViewExDto.NeedToSaveAsFromEshopProductBaseDataModelId.Value, true);

                            if (saveAsResult.IsSuccessfulWithResult)
                            {
                                if (aAppSearchExDto.DefaultSearchViewExDto.OtherSettingsDto == null)
                                {
                                    aAppSearchExDto.DefaultSearchViewExDto.OtherSettingsDto = new AppSearchViewOtherSettingsDto();
                                }


                                aAppSearchExDto.DefaultSearchViewExDto.OtherSettingsDto.EshopCardViewItemDetailTransactionId = (int)saveAsResult.Object.Id;
                            }
                        }


                        var saveSearchViewResult = AppSearchViewConfigBL.SaveAppSearchViewExDto(aAppSearchExDto.DefaultSearchViewExDto);

                        if (!saveSearchViewResult.IsSuccessfulWithResult)
                        {
                            aValidationResult.Merge(saveSearchViewResult.ValidationResult);
                        }
                    }
                }

            }

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppSearchExDto(aAppSearchExDto.Id);
            }

            return aOperationCallResult;
        }



        public static OperationCallResult<AppSearchExDto> SaveEshopCategorySearchExDto(AppSearchExDto aAppSearchExDto)
        {
            OperationCallResult<AppSearchExDto> aOperationCallResult = new OperationCallResult<AppSearchExDto>();
            var aValidationResult = aAppSearchExDto.ValidateDto();
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }


            aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppSearchEntity aAppSearchEntity;

            foreach (var searchFieldDto in aAppSearchExDto.AppSearchFieldList)
            {
                if (string.IsNullOrEmpty(searchFieldDto.DisplayText))
                {
                    searchFieldDto.DisplayText = searchFieldDto.SysTableFiledPath;
                }

                if (searchFieldDto.ControlType.HasValue
                    && searchFieldDto.ControlType.Value != (int)EmAppControlType.DDL
                    && searchFieldDto.ControlType.Value != (int)EmAppControlType.AutoComplete
                    && searchFieldDto.ControlType.Value != (int)EmAppControlType.SearchAbleDDL)
                {
                    searchFieldDto.EntityId = null;
                }
            }

            // prepare Data
            if (aAppSearchExDto.IsNew)
            {
                if (aAppSearchExDto.DataSetId.HasValue)
                {
                    AppSearchViewExDto searchViewExDto = new AppSearchViewExDto();
                    searchViewExDto.DataSetId = aAppSearchExDto.DataSetId;
                    searchViewExDto.Name = aAppSearchExDto.Name;
                    searchViewExDto.NoSecurity = false;
                    searchViewExDto.GridOutputMode = 1;
                    searchViewExDto.Options = 0;
                    searchViewExDto.ViewType = (int)EmAppViewType.FlatDataSetTreeView;
                    searchViewExDto.ColumnCount = 0;
                    searchViewExDto.RowPerPage = 0;
                    searchViewExDto.IsEnableCalendarMonthView = true;
                    searchViewExDto.IsEnableCalendarWeekView = true;
                    searchViewExDto.IsEnableCalendarDayView = true;
                    searchViewExDto.IsEnableCalendarNavigator = true;
                    searchViewExDto.IsDisableClientTimeConvert = false;

                    searchViewExDto.AppSearchViewFieldList = new ObservableSet<AppSearchViewFieldExDto>();

                    OperationCallResult<AppSearchViewExDto> saveSearchViewResult = AppSearchViewConfigBL.SaveAppSearchViewExDto(searchViewExDto);

                    if (saveSearchViewResult.IsSuccessfulWithResult)
                    {
                        aAppSearchExDto.SearchViewId = (int)saveSearchViewResult.Object.Id;
                    }
                    else
                    {
                        aValidationResult.Merge(saveSearchViewResult.ValidationResult);
                    }
                }

                aAppSearchEntity = new AppSearchEntity();
                AppSearchConverter.CopyDtoToEntity(aAppSearchEntity, aAppSearchExDto);

                foreach (var searchTemplateSubitemDto in aAppSearchExDto.AppSearchFieldList)
                {
                    AppSearchFieldEntity aAppSearchFieldEntity = new AppSearchFieldEntity();
                    AppSearchFieldConverter.CopyDtoToEntity(aAppSearchFieldEntity, searchTemplateSubitemDto);
                    aAppSearchEntity.AppSearchField.Add(aAppSearchFieldEntity);
                }


                foreach (var searchParamterDto in aAppSearchExDto.AppSearchParameterList)
                {
                    AppSearchParameterEntity aAppSearchFieldEntity = new AppSearchParameterEntity();
                    AppSearchParameterConverter.CopyDtoToEntity(aAppSearchFieldEntity, searchParamterDto);
                    aAppSearchEntity.AppSearchParameter.Add(aAppSearchFieldEntity);
                }



                if (!aValidationResult.HasErrors)
                {
                    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                    {
                        try
                        {
                            adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                            adapter.SaveEntity(aAppSearchEntity);
                            adapter.Commit();

                            aAppSearchExDto.Id = aAppSearchEntity.SearchId;
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "App_SearchEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                        }


                        // Database FK Exeption ........
                        catch (ORMQueryExecutionException ex)
                        {
                            adapter.Rollback();
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchExDto), "App_SearchEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                        }
                    }
                }
            }

            else if (aAppSearchExDto.IsRelatedEntitiesModified())
            {
                if (!aValidationResult.HasErrors)
                {
                    if (aAppSearchExDto.EshopCardSearchExDto != null)
                    {
                        BuildEshopCategorySearchMapping(aAppSearchExDto, aValidationResult);

                        var saveEshopCardSearchResult = SaveEshopCardSearchExDto(aAppSearchExDto.EshopCardSearchExDto);

                        if (saveEshopCardSearchResult.IsSuccessfulWithResult)
                        {
                            aAppSearchExDto.EshopCardSearchExDto = saveEshopCardSearchResult.Object;
                            BuildEshopCategorySearchMapping(aAppSearchExDto, aValidationResult);
                        }
                        else
                        {
                            aValidationResult.Merge(saveEshopCardSearchResult.ValidationResult);
                        }
                    }
                }

                if (!aValidationResult.HasErrors)
                {
                    aValidationResult.Merge(ProcessDirtyAppSearchExDto(aAppSearchExDto));
                }

                if (!aValidationResult.HasErrors)
                {
                    if (aAppSearchExDto.DefaultSearchViewExDto != null)
                    {
                        var saveSearchViewResult = AppSearchViewConfigBL.SaveAppSearchViewExDto(aAppSearchExDto.DefaultSearchViewExDto);

                        if (!saveSearchViewResult.IsSuccessfulWithResult)
                        {
                            aValidationResult.Merge(saveSearchViewResult.ValidationResult);
                        }
                    }
                }


            }

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppSearchExDto(aAppSearchExDto.Id);
            }

            return aOperationCallResult;
        }

        private static void BuildEshopCategorySearchMapping(AppSearchExDto appSearchExDto, ValidationResult validationResult)
        {
            if (appSearchExDto != null && appSearchExDto.DefaultSearchViewExDto != null && appSearchExDto.DefaultSearchViewExDto.Id != null
                && appSearchExDto.EshopCardSearchExDto != null && appSearchExDto.EshopCardSearchExDto.Id != null)
            {
                int viewId = (int)appSearchExDto.DefaultSearchViewExDto.Id;
                var viewDto = appSearchExDto.DefaultSearchViewExDto;

                int targetSearchId = (int)appSearchExDto.EshopCardSearchExDto.Id;
                var targetSearchDto = appSearchExDto.EshopCardSearchExDto;

                if (viewDto.OtherSettingsDto != null && viewDto.OtherSettingsDto.EshopCategorySearchMapping != null)
                {
                    var mappingDto = viewDto.OtherSettingsDto.EshopCategorySearchMapping;

                    if (!string.IsNullOrWhiteSpace(mappingDto.TargetSearchFieldName1))
                    {
                        var existField = targetSearchDto.AppSearchFieldList.FirstOrDefault(o => o.SysTableFiledPath == mappingDto.TargetSearchFieldName1);

                        if (existField != null)
                        {
                            if (existField.Id != null)
                            {
                                mappingDto.TargetSearchFieldId1 = (int)existField.Id;
                            }
                        }
                        else
                        {
                            AppSearchFieldExDto newField = new AppSearchFieldExDto();
                            newField.SysTableFiledPath = mappingDto.TargetSearchFieldName1;
                            newField.DisplayText = mappingDto.TargetSearchFieldName1;
                            newField.ControlType = (int)EmAppControlType.TextBox;
                            newField.IsModified = true;
                            newField.IsVisible = true;
                            newField.OperationId = 0;
                            newField.Sort = 1;
                            newField.PositionRow = 1;
                            newField.PositionColumn = targetSearchDto.AppSearchFieldList.Count + 1;

                            targetSearchDto.AppSearchFieldList.Add(newField);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(mappingDto.TargetSearchFieldName2))
                    {
                        var existField = targetSearchDto.AppSearchFieldList.FirstOrDefault(o => o.SysTableFiledPath == mappingDto.TargetSearchFieldName2);

                        if (existField != null)
                        {
                            if (existField.Id != null)
                            {
                                mappingDto.TargetSearchFieldId2 = (int)existField.Id;
                            }
                        }
                        else
                        {
                            AppSearchFieldExDto newField = new AppSearchFieldExDto();
                            newField.SysTableFiledPath = mappingDto.TargetSearchFieldName2;
                            newField.DisplayText = mappingDto.TargetSearchFieldName2;
                            newField.ControlType = (int)EmAppControlType.TextBox;
                            newField.IsModified = true;
                            newField.IsVisible = true;
                            newField.OperationId = 0;
                            newField.Sort = 1;
                            newField.PositionRow = 1;
                            newField.PositionColumn = targetSearchDto.AppSearchFieldList.Count + 1;

                            targetSearchDto.AppSearchFieldList.Add(newField);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(mappingDto.TargetSearchFieldName3))
                    {
                        var existField = targetSearchDto.AppSearchFieldList.FirstOrDefault(o => o.SysTableFiledPath == mappingDto.TargetSearchFieldName3);

                        if (existField != null)
                        {
                            if (existField.Id != null)
                            {
                                mappingDto.TargetSearchFieldId3 = (int)existField.Id;
                            }
                        }
                        else
                        {
                            AppSearchFieldExDto newField = new AppSearchFieldExDto();
                            newField.SysTableFiledPath = mappingDto.TargetSearchFieldName3;
                            newField.DisplayText = mappingDto.TargetSearchFieldName3;
                            newField.ControlType = (int)EmAppControlType.TextBox;
                            newField.IsModified = true;
                            newField.IsVisible = true;
                            newField.OperationId = 0;
                            newField.Sort = 1;
                            newField.PositionRow = 1;
                            newField.PositionColumn = targetSearchDto.AppSearchFieldList.Count + 1;

                            targetSearchDto.AppSearchFieldList.Add(newField);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(mappingDto.TargetSearchFieldName4))
                    {
                        var existField = targetSearchDto.AppSearchFieldList.FirstOrDefault(o => o.SysTableFiledPath == mappingDto.TargetSearchFieldName4);

                        if (existField != null)
                        {
                            if (existField.Id != null)
                            {
                                mappingDto.TargetSearchFieldId4 = (int)existField.Id;
                            }
                        }
                        else
                        {
                            AppSearchFieldExDto newField = new AppSearchFieldExDto();
                            newField.SysTableFiledPath = mappingDto.TargetSearchFieldName4;
                            newField.DisplayText = mappingDto.TargetSearchFieldName4;
                            newField.ControlType = (int)EmAppControlType.TextBox;
                            newField.IsModified = true;
                            newField.IsVisible = true;
                            newField.OperationId = 0;
                            newField.Sort = 1;
                            newField.PositionRow = 1;
                            newField.PositionColumn = targetSearchDto.AppSearchFieldList.Count + 1;

                            targetSearchDto.AppSearchFieldList.Add(newField);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(mappingDto.TargetSearchFieldName5))
                    {
                        var existField = targetSearchDto.AppSearchFieldList.FirstOrDefault(o => o.SysTableFiledPath == mappingDto.TargetSearchFieldName5);

                        if (existField != null)
                        {
                            if (existField.Id != null)
                            {
                                mappingDto.TargetSearchFieldId5 = (int)existField.Id;
                            }
                        }
                        else
                        {
                            AppSearchFieldExDto newField = new AppSearchFieldExDto();
                            newField.SysTableFiledPath = mappingDto.TargetSearchFieldName5;
                            newField.DisplayText = mappingDto.TargetSearchFieldName5;
                            newField.ControlType = (int)EmAppControlType.TextBox;
                            newField.IsModified = true;
                            newField.IsVisible = true;
                            newField.OperationId = 0;
                            newField.Sort = 1;
                            newField.PositionRow = 1;
                            newField.PositionColumn = targetSearchDto.AppSearchFieldList.Count + 1;

                            targetSearchDto.AppSearchFieldList.Add(newField);
                        }
                    }
                }

                AppViewLinkedSeaechOrUrlExDto newLinkedSearchDto = new AppViewLinkedSeaechOrUrlExDto();
                newLinkedSearchDto.SearchViewId = viewId;
                newLinkedSearchDto.LinkTargetSearchId = targetSearchId;

                ObservableSet<AppViewLinkedSeaechOrUrlExDto> aSet = new ObservableSet<AppViewLinkedSeaechOrUrlExDto>();
                aSet.Add(newLinkedSearchDto);

                OperationCallResult<AppViewLinkedSeaechOrUrlExDto> result = AppViewLinkedSeaechOrUrlBL.SaveAllAppViewLinkedSeaechOrUrlEntityDto(aSet, viewId);
                if (!result.IsSuccessful)
                {
                    validationResult.Merge(result.ValidationResult);
                }

            }
        }

        private static OperationCallResult<AppSearchViewExDto> CreateDefaultBlankView(AppSearchExDto aAppSearchExDto)
        {
            AppSearchViewExDto searchViewExDto = new AppSearchViewExDto();
            searchViewExDto.DataSetId = aAppSearchExDto.DataSetId;
            searchViewExDto.Name = aAppSearchExDto.Name;
            searchViewExDto.NoSecurity = false;
            searchViewExDto.GridOutputMode = 1;
            searchViewExDto.Options = 0;
            searchViewExDto.ViewType = (int)EmAppViewType.GridView;
            searchViewExDto.ColumnCount = 0;
            searchViewExDto.RowPerPage = 0;
            searchViewExDto.IsEnableCalendarMonthView = true;
            searchViewExDto.IsEnableCalendarWeekView = true;
            searchViewExDto.IsEnableCalendarDayView = true;
            searchViewExDto.IsEnableCalendarNavigator = true;
            searchViewExDto.IsDisableClientTimeConvert = false;

            var saveSearchViewResult = AppSearchViewConfigBL.SaveAppSearchViewExDto(searchViewExDto);
            return saveSearchViewResult;
        }

        private static ValidationResult ProcessDirtyAppSearchExDto(AppSearchExDto aAppSearchExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            // int[] deleteAppSearchFieldIDs = aAppSearchExDto.AppSearchFieldList.FindModifiedItems().Select(o => o.Id).Cast<int>().ToArray();

            AppSearchEntity aAppSearchEntity = RetrieveOneAppSearchEntity(aAppSearchExDto.Id);

            Dictionary<int, AppSearchFieldEntity> dictAppSearchFieldFromDbms = aAppSearchEntity.AppSearchField.ToDictionary(o => o.SearchFielDid, o => o);

            AppSearchConverter.CopyDtoToEntity(aAppSearchEntity, aAppSearchExDto);

            // new Items
            foreach (AppSearchFieldDto aChildDto in aAppSearchExDto.AppSearchFieldList.FindNewItems())
            {
                AppSearchFieldEntity aNewChildEntity = new AppSearchFieldEntity();
                AppSearchFieldConverter.CopyDtoToEntity(aNewChildEntity, aChildDto);
                aAppSearchEntity.AppSearchField.Add(aNewChildEntity);
            }

            // Dirty items
            foreach (var modifyitem in aAppSearchExDto.AppSearchFieldList.FindModifiedItems())
            {
                int dtoKey = int.Parse(modifyitem.Id.ToString());
                if (dictAppSearchFieldFromDbms.ContainsKey(dtoKey))
                {
                    AppSearchFieldConverter.CopyDtoToEntity(dictAppSearchFieldFromDbms[dtoKey], modifyitem);
                }
            }

            // deletedIDs
            int[] deleteAppSearchFieldIDs = aAppSearchExDto.AppSearchFieldList.FindDeletedItemIds().Cast<int>().ToArray();


            // AppSearchParameter  Dto

            Dictionary<int, AppSearchParameterEntity> dictAppSearchParameterFromDbms = aAppSearchEntity.AppSearchParameter.ToDictionary(o => o.SearchparameterId, o => o);
            foreach (AppSearchParameterDto aChildDto in aAppSearchExDto.AppSearchParameterList.FindNewItems())
            {
                AppSearchParameterEntity aNewChildEntity = new AppSearchParameterEntity();
                AppSearchParameterConverter.CopyDtoToEntity(aNewChildEntity, aChildDto);
                aAppSearchEntity.AppSearchParameter.Add(aNewChildEntity);
            }

            // Dirty items
            foreach (var modifyitem in aAppSearchExDto.AppSearchParameterList.FindModifiedItems())
            {
                int dtoKey = int.Parse(modifyitem.Id.ToString());
                if (dictAppSearchParameterFromDbms.ContainsKey(dtoKey))
                {
                    AppSearchParameterConverter.CopyDtoToEntity(dictAppSearchParameterFromDbms[dtoKey], modifyitem);
                }
            }

            // deletedIDs
            int[] deleteAppSearchParameterIDs = aAppSearchExDto.AppSearchParameterList.FindDeletedItemIds().Cast<int>().ToArray();





            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppSearchEntity);

                    // Need to delete SearchTemplate subitems
                    if (deleteAppSearchFieldIDs.Count() > 0)
                    {
                        adapter.DeleteEntitiesDirectly(typeof(AppSearchFieldEntity), new RelationPredicateBucket(AppSearchFieldFields.SearchFielDid == deleteAppSearchFieldIDs));

                    }

                    if (deleteAppSearchParameterIDs.Count() > 0)
                    {
                        adapter.DeleteEntitiesDirectly(typeof(AppSearchParameterEntity), new RelationPredicateBucket(AppSearchParameterFields.SearchparameterId == deleteAppSearchParameterIDs));
                    }

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchEntity), "App_SearchEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchEntity), "App_SearchEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }


        private static ValidationResult ProcessDirtyAppSearchFieldExDto(AppSearchFieldExDto aAppSearchFieldExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();


            AppSearchFieldEntity aAppSearchFieldEntity = RetrieveOneAppSearchFieldEntity(aAppSearchFieldExDto.Id);

            AppSearchFieldConverter.CopyDtoToEntity(aAppSearchFieldEntity, aAppSearchFieldExDto);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppSearchFieldEntity);

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchFieldEntity), "App_SearchEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchFieldEntity), "App_SearchEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }


        //Delete a AppSearch
        public static OperationCallResult<object> DeleteAppSearch(object SearchId, bool isDeleteDefaultView = false, bool isDeleteDataSet = false)
        {
            OperationCallResult<object> aValidationResult = new OperationCallResult<object>();

            string referMsg = string.Empty;

            AppSearchEntity searchEntity = RetrieveOneAppSearchEntity(SearchId);


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");



                    adapter.DeleteEntitiesDirectly(typeof(AppTransactionNavigationEntity), new RelationPredicateBucket(AppTransactionNavigationFields.QuickSearchId == SearchId));
                    adapter.DeleteEntitiesDirectly(typeof(AppSearchFieldEntity), new RelationPredicateBucket(AppSearchFieldFields.SearchId == SearchId));
                    adapter.DeleteEntitiesDirectly(typeof(AppSearchEntity), new RelationPredicateBucket(AppSearchFields.SearchId == SearchId));

                    try
                    {
                        if (isDeleteDefaultView)
                        {
                            if (searchEntity.SearchViewId.HasValue)
                            {

                                int searchViewId = searchEntity.SearchViewId.Value;

                                adapter.DeleteEntitiesDirectly(typeof(AppViewLinkedSeaechOrUrlEntity), new RelationPredicateBucket(AppViewLinkedSeaechOrUrlFields.SearchViewId == searchViewId));
                                adapter.DeleteEntitiesDirectly(typeof(AppFormLinkTargetEntity), new RelationPredicateBucket(AppFormLinkTargetFields.SearchViewId == searchViewId));
                                adapter.DeleteEntitiesDirectly(typeof(AppSearchViewFieldEntity), new RelationPredicateBucket(AppSearchViewFieldFields.SearchViewId == searchViewId));
                                adapter.DeleteEntitiesDirectly(typeof(AppFormLinkTargetEntity), new RelationPredicateBucket(AppFormLinkTargetFields.SearchViewId == searchViewId));
                                adapter.DeleteEntitiesDirectly(typeof(AppSearchViewEntity), new RelationPredicateBucket(AppSearchViewFields.SearchViewId == searchViewId));
                            }
                        }

                        if (isDeleteDataSet && searchEntity.DataSetId.HasValue)
                        {

                            adapter.DeleteEntitiesDirectly(typeof(AppDataSetParameterEntity), new RelationPredicateBucket(AppDataSetParameterFields.DataSetId == searchEntity.DataSetId.Value));
                            adapter.DeleteEntitiesDirectly(typeof(AppDataSetEntity), new RelationPredicateBucket(AppDataSetFields.DataSetId == searchEntity.DataSetId.Value));
                        }
                    }
                    catch (Exception ex)
                    {
                        aValidationResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSearchEntity), "App_SearchEntity_QueryExecution_Error", ValidationItemType.Warning, ex.ToString()));
                    }


                    //adapter.DeleteEntitiesDirectly(typeof(AppListMenuEntity), new RelationPredicateBucket(AppListMenuFields.RouteCode == "MasterDataManagement" & AppListMenuFields.Link == SearchId.ToString()));

                    adapter.Commit();
                }



                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppSearchEntity), "App_SearchEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }

                // if no any errors
                if (!aValidationResult.ValidationResult.HasErrors)
                {
                    aValidationResult.Object = SearchId;
                }
            }
            return aValidationResult;
        }

        public static List<LookupItemDto> RetrieveBLQueryColumnList(int dataSetId)
        {
            List<LookupItemDto> list = new List<LookupItemDto>();
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppDataSetEntity aEntity = new AppDataSetEntity(dataSetId);
                adpater.FetchEntity(aEntity);


                //  var dictColumNameDataType = AppDataSetBL.GetQuerySchemeColumnNameDataType(aEntity.QueryText, aEntity.DataSourceFrom.Value );


                var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(aEntity.DataSourceFrom.Value);
                var dictColumNameDataType = dataBaseFixture.GetQuerySchemeColumnNameDataType(aEntity.QueryText);



                foreach (var pair in dictColumNameDataType)
                {
                    LookupItemDto aLookupItemDto = new LookupItemDto();
                    aLookupItemDto.Id = pair.Key;
                    aLookupItemDto.Display = pair.Value;
                    list.Add(aLookupItemDto);
                }
                return list;
            }

            // throw new NotImplementedException();
        }

        public static List<AppSearchViewDto> RetrieveStatciSearchAvailableViewWithSameQueryBL(int dataSetId)
        {
            EntityCollection<AppSearchViewEntity> aListEntity = AppDataSetBL.GetAllSearchViewForOneDataSet(dataSetId);

            List<AppSearchViewDto> listDto = new List<AppSearchViewDto>();
            foreach (var entity in aListEntity)
            {
                listDto.Add(AppSearchViewConverter.ConvertEntityToDto(entity));
            }

            return listDto;
            // throw new NotImplementedException();
        }






        // Saved Search

        internal static EntityCollection<AppSearchSavedEntity> RetrieveAllCurrentUserSavedSearchEntity(int? searchUsageType = null)
        {
            EntityCollection<AppSearchSavedEntity> userSaveedSearchlist = new EntityCollection<AppSearchSavedEntity>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                RelationPredicateBucket filter = new RelationPredicateBucket(AppSearchSavedFields.UserId == ServerContext.Instance.CurrentUid);

                if (searchUsageType.HasValue)
                {
                    filter.Relations.Add(AppSearchSavedEntity.Relations.AppSearchEntityUsingSearchId);
                    filter.PredicateExpression.AddWithAnd(AppSearchFields.Type == searchUsageType.Value);
                }

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppSearchSavedEntity);
                rootPath.Add(AppSearchSavedEntity.PrefetchPathAppSearchSavedValue);

                adapter.FetchEntityCollection(userSaveedSearchlist, filter, rootPath);

            }
            return userSaveedSearchlist;
        }

        internal static List<SearchDefinitionDto> GetCurrentUserSavedSearchList(int? searchUsageType = null)
        {

            List<SearchDefinitionDto> aList = new List<SearchDefinitionDto>();

            foreach (var aSaveSearch in RetrieveAllCurrentUserSavedSearchEntity(searchUsageType))
            {
                SearchDefinitionDto aSearchDto = new SearchDefinitionDto();
                //Bugs should keep saveSearchId not SearchId
                // aSearchDto.Id = aSaveSearch.SearchId ;

                aSearchDto.Id = aSaveSearch.SearchSavedId;

                aSearchDto.Display = aSaveSearch.SavedSearchName.ToString();
                aSearchDto.IsSavedSearch = true;
                aList.Add(aSearchDto);
            }

            return aList;

        }


        internal static AppSearchSavedEntity RetrieveOneUserSavedSearchEntity(int saveSearchId)
        {


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                AppSearchSavedEntity appSearchSavedEntity = new AppSearchSavedEntity(saveSearchId);


                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppSearchSavedEntity);
                rootPath.Add(AppSearchSavedEntity.PrefetchPathAppSearchSavedValue);

                adapter.FetchEntity(appSearchSavedEntity, rootPath);

                return appSearchSavedEntity;

            }

        }


        internal static SearchDto ConvertSavedSearchEntitySearchDto(AppSearchSavedEntity aSavedSearchEntiy)
        {
            //  SearchTemplate aSearchTempalte = new SearchTemplate(aSaveSearch.SearchTemplateID.Value);
            int searchId = aSavedSearchEntiy.SearchId;
            AppSearchEntity aSearchTempalte = RetrieveOneAppSearchEntity(searchId);

            aSearchTempalte.IsAutoExecute = !aSavedSearchEntiy.IsAutoExecute.HasValue ? false : aSavedSearchEntiy.IsAutoExecute.Value;


            SearchDto aSearchDto = AppSearchConfigBL.ConvertSearchEntitySearchDto(aSearchTempalte);

            // important !
            aSearchDto.IsSavedSearch = true;
            aSearchDto.SearchTemplateSavedID = aSavedSearchEntiy.SearchSavedId;

            if (aSavedSearchEntiy.SavedSearchName != string.Empty)
            {
                aSearchDto.Display = aSavedSearchEntiy.SavedSearchName.ToString();
            }

            //???
            foreach (SearchCriteriaDto aCriteriaDto in aSearchDto.Criterias)
            {
                aCriteriaDto.Value = null;
                aCriteriaDto.Values.Clear();

                foreach (var aSearchSavedValue in aSavedSearchEntiy.AppSearchSavedValue)
                {
                    if (!string.IsNullOrEmpty(aSearchSavedValue.SearchValue.ToString()))      // not empty !
                    {
                        int aSearchDCUID = aSearchSavedValue.SearchFieldId;

                        if (aCriteriaDto.SearcDCUID == aSearchDCUID)
                        {
                            SetupDefaultValue(aCriteriaDto, aSearchSavedValue.SearchValue.ToString());

                            if (aCriteriaDto.Values.Count > 0)
                            {
                                if (aSearchSavedValue.OperationId.HasValue)
                                {
                                    int operationId = aSearchSavedValue.OperationId.Value;

                                    aCriteriaDto.CriteriaOperator = SetDefaultOperator(aCriteriaDto.SupportedOperators, operationId);
                                }
                            }
                        }
                    }
                }
            }
            aSearchDto.CriteriasRowCount = aSearchDto.Criterias.Max(o => o.RowIndex);
            return aSearchDto;
        }




        //--------



        public static ValidationResult DeleteAppSearchSavedEntity(object searchSavedId)
        {
            var aValidationResult = new ValidationResult();
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppSearchSavedEntity aEntity = new AppSearchSavedEntity(int.Parse(searchSavedId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppSearchSavedEntity);
                rootPath.Add(AppSearchSavedEntity.PrefetchPathAppSearchSavedValue);
                adpater.FetchEntity(aEntity, rootPath);
                int[] saveValueIds = aEntity.AppSearchSavedValue.Select(o => o.SearchSavedValueId).ToArray();

                try
                {
                    adpater.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adpater.DeleteEntitiesDirectly(typeof(AppSearchSavedValueEntity), new RelationPredicateBucket(AppSearchSavedValueFields.SearchSavedValueId == saveValueIds));
                    adpater.DeleteEntitiesDirectly(typeof(AppSearchSavedEntity), new RelationPredicateBucket(AppSearchSavedFields.SearchSavedId == searchSavedId));

                    adpater.Commit();

                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchSavedEntity), "App_AppSearchSavedEntity_Delete_OK", ValidationItemType.Message, "Delete Successful"));
                }


                catch (Exception ex)
                {
                    adpater.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchSavedEntity), "App_AppSearchSavedEntity_Delete_Error", ValidationItemType.Error, ex.ToString()));

                }
            }

            return aValidationResult;
        }

        internal static ValidationResult SaveExistingAppSearchSavedEntity(SearchDto search)
        {
            var aValidationResult = new ValidationResult();

            if (!(search.IsSavedSearch && search.SearchTemplateSavedID.HasValue))
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchSavedEntity), "App_AppSearchSavedEntity_SavedSearchDoesNotExist", ValidationItemType.Error, "Saved Search Does Not Exist."));
                return aValidationResult;
            }

            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppSearchSavedEntity aEntity = RetrieveOneUserSavedSearchEntity(search.SearchTemplateSavedID.Value);
                if (aEntity == null)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchSavedEntity), "App_AppSearchSavedEntity_SavedSearchDoesNotExist", ValidationItemType.Error, "Saved Search Does Not Exist."));
                    return aValidationResult;
                }

                aEntity.SavedSearchName = search.Display;
                aEntity.UserId = int.Parse(ServerContext.Instance.CurrentUid.ToString());
                aEntity.IsDefault = search.IsDefault;
                aEntity.IsAutoExecute = search.IsAutoExcute;
                //aEntity.FolderTransactionId = search.FolderTransactionId;

                foreach (SearchCriteriaDto aSearchCriteriaDto in search.Criterias)
                {
                    foreach (var value in aSearchCriteriaDto.Values)
                    {
                        if (value != null)
                        {
                            AppSearchSavedValueEntity aSearchTemplateSavedValue = new AppSearchSavedValueEntity();
                            aSearchTemplateSavedValue.SearchFieldId = aSearchCriteriaDto.SearcDCUID;
                            if (search.IsStaticBuiltInSearch.HasValue && search.IsStaticBuiltInSearch.Value)
                            {
                                aSearchTemplateSavedValue.OperationId = aSearchCriteriaDto.OperationId;
                            }
                            else if (aSearchCriteriaDto.CriteriaOperator != null)
                            {
                                aSearchTemplateSavedValue.OperationId = (int)aSearchCriteriaDto.CriteriaOperator.OperatorType;
                            }

                            aSearchTemplateSavedValue.SearchValue = value.ToString();
                            aEntity.AppSearchSavedValue.Add(aSearchTemplateSavedValue);
                        }
                    }
                }

                try
                {
                    adpater.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adpater.DeleteEntitiesDirectly(typeof(AppSearchSavedValueEntity), new RelationPredicateBucket(AppSearchSavedValueFields.SearchSavedId == search.SearchTemplateSavedID.Value));
                    adpater.SaveEntity(aEntity);

                    adpater.Commit();

                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchSavedEntity), "App_AppSearchSavedEntity_Save_OK", ValidationItemType.Message, "Save Successfully"));
                }


                catch (Exception ex)
                {
                    adpater.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchSavedEntity), "App_AppSearchSavedEntity_Save_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        internal static ValidationResult SaveNewAppSearchSavedEntity(SearchDto search)
        {
            var aValidationResult = new ValidationResult();
            if (!search.Id.HasValue)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchSavedEntity), "App_AppSearchSavedEntity_Save_Error", ValidationItemType.Error, "Search Id cannot be empty"));
                return aValidationResult;
            }

            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppSearchSavedEntity aEntity = new AppSearchSavedEntity();
                aEntity.SearchId = search.Id.Value;
                aEntity.SavedSearchName = search.Display;
                aEntity.UserId = int.Parse(ServerContext.Instance.CurrentUid.ToString());
                aEntity.IsDefault = search.IsDefault;
                aEntity.IsAutoExecute = search.IsAutoExcute;
                //aEntity.FolderTransactionId = search.FolderTransactionId;

                foreach (SearchCriteriaDto aSearchCriteriaDto in search.Criterias)
                {
                    foreach (var value in aSearchCriteriaDto.Values)
                    {
                        if (value != null)
                        {
                            AppSearchSavedValueEntity aSearchTemplateSavedValue = new AppSearchSavedValueEntity();
                            aSearchTemplateSavedValue.SearchFieldId = aSearchCriteriaDto.SearcDCUID;
                            if (search.IsStaticBuiltInSearch.HasValue && search.IsStaticBuiltInSearch.Value)
                            {
                                aSearchTemplateSavedValue.OperationId = aSearchCriteriaDto.OperationId;
                            }
                            else if (aSearchCriteriaDto.CriteriaOperator != null)
                            {
                                aSearchTemplateSavedValue.OperationId = (int)aSearchCriteriaDto.CriteriaOperator.OperatorType;
                            }

                            aSearchTemplateSavedValue.SearchValue = value.ToString();
                            aEntity.AppSearchSavedValue.Add(aSearchTemplateSavedValue);
                        }
                    }
                }

                try
                {
                    adpater.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adpater.SaveEntity(aEntity);
                    adpater.Commit();

                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchSavedEntity), "App_AppSearchSavedEntity_Save_OK", ValidationItemType.Message, "Save Successfully"));
                }


                catch (Exception ex)
                {
                    adpater.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppSearchSavedEntity), "App_AppSearchSavedEntity_Save_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }





        public static bool SetAsDefaultCriteriaPreset(SearchDto search)
        {
            if (!search.IsSavedSearch)
                return false;

            bool toReturn = false;
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "UpdateUserDefineSearch");
                    RelationPredicateBucket aPredicated = new RelationPredicateBucket(AppSearchSavedFields.SearchSavedId == search.SearchTemplateSavedID);
                    aPredicated.PredicateExpression.AddWithAnd(AppSearchSavedFields.UserId == ServerContext.Instance.CurrentUid);

                    AppSearchSavedEntity aEntity = new AppSearchSavedEntity();
                    aEntity.IsDefault = true;
                    adapter.UpdateEntitiesDirectly(aEntity, aPredicated);

                    RelationPredicateBucket aPredicated2 = new RelationPredicateBucket(AppSearchSavedFields.SearchSavedId != search.SearchTemplateSavedID);
                    aPredicated.PredicateExpression.AddWithAnd(AppSearchSavedFields.UserId == ServerContext.Instance.CurrentUid);

                    aEntity.IsDefault = false;
                    adapter.UpdateEntitiesDirectly(aEntity, aPredicated2);
                    adapter.Commit();
                    toReturn = true;
                }
                catch
                {
                    adapter.Rollback();
                    toReturn = false;
                }
            }

            return toReturn;
        }


        public static OperationCallResult<SearchDefinitionDto> DeleteCriteriaPreset(SearchDto search)
        {
            OperationCallResult<SearchDefinitionDto> aOperationCallResult = new OperationCallResult<SearchDefinitionDto>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (search.SearchTemplateSavedID.HasValue)
            {
                aValidationResult.Merge(AppSearchConfigBL.DeleteAppSearchSavedEntity(search.SearchTemplateSavedID.Value));
                if (!aValidationResult.HasErrors)
                {
                    aOperationCallResult.ObjectList = AppSearchConfigBL.GetCurrentUserSavedSearchList(search.SearchType);
                }
            }


            return aOperationCallResult;
        }



        public static OperationCallResult<SearchDefinitionDto> SaveCriteriaPreset(SearchDto search, bool isSaveAs)
        {
            OperationCallResult<SearchDefinitionDto> aOperationCallResult = new OperationCallResult<SearchDefinitionDto>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (search.IsSavedSearch)
            {
                if (isSaveAs)
                {
                    aValidationResult.Merge(SaveNewAppSearchSavedEntity(search));
                }
                else
                {
                    aValidationResult.Merge(SaveExistingAppSearchSavedEntity(search));
                }
            }
            else
            {
                aValidationResult.Merge(SaveNewAppSearchSavedEntity(search));
            }


            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.ObjectList = AppSearchConfigBL.GetCurrentUserSavedSearchList(search.SearchType);
            }

            return aOperationCallResult;

        }


        public static bool ChangeSearchAutoExecute(SearchDto search)
        {
            if (!search.IsSavedSearch)
                return false;

            bool toReturn = false;
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "UpdateUserDefineSearch");
                    RelationPredicateBucket aPredicated = new RelationPredicateBucket(AppSearchSavedFields.SearchSavedId == search.SearchTemplateSavedID);
                    aPredicated.PredicateExpression.AddWithAnd(AppSearchSavedFields.UserId == ServerContext.Instance.CurrentUid);

                    AppSearchSavedEntity aEntity = new AppSearchSavedEntity();
                    aEntity.IsAutoExecute = true;
                    adapter.UpdateEntitiesDirectly(aEntity, aPredicated);

                    RelationPredicateBucket aPredicated2 = new RelationPredicateBucket(AppSearchSavedFields.SearchSavedId != search.SearchTemplateSavedID);
                    aPredicated.PredicateExpression.AddWithAnd(AppSearchSavedFields.UserId == ServerContext.Instance.CurrentUid);

                    aEntity.IsAutoExecute = false;
                    adapter.UpdateEntitiesDirectly(aEntity, aPredicated2);
                    adapter.Commit();
                    toReturn = true;
                }
                catch
                {
                    adapter.Rollback();
                    toReturn = false;
                }
            }

            return toReturn;
        }



        internal static EntityCollection<AppSearchEntity> RetrieveSearchEntityListByFolderTransactionId(object folderTransactionId)
        {
            EntityCollection<AppSearchEntity> list = new EntityCollection<AppSearchEntity>();

            if (folderTransactionId != null)
            {

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    RelationPredicateBucket filter = new RelationPredicateBucket(AppSearchFields.FolderTransactionId == folderTransactionId);
                    adapter.FetchEntityCollection(list, filter);

                }
            }
            return list;
        }

        public static List<AppSearchFieldDto> RetrieveAllAppSearchFieldDtoList()
        {
            List<AppSearchFieldDto> toReturn = new List<AppSearchFieldDto>();

            EntityCollection<AppSearchFieldEntity> list = new EntityCollection<AppSearchFieldEntity>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                adapter.FetchEntityCollection(list, null);
            }

            list.ForAll(o => toReturn.Add(AppSearchFieldConverter.ConvertEntityToDto(o)));

            return toReturn;
        }

        public static List<SearchApiSettingDto> RetrieveSearchApiSettings(int? searchId)
        {
            List<SearchApiSettingDto> toReturn = new List<SearchApiSettingDto>();

            if (searchId.HasValue)
            {

                // Consume APIs
                var searchEntity = RetrieveOneAppSearchEntity(searchId.Value);
                if (searchEntity.DataSetId.HasValue)
                {
                    var datasetExDto = AppDataSetBL.RetrieveOneAppDataSetExDto(searchEntity.DataSetId.Value);

                    if (datasetExDto.QueryType.HasValue && datasetExDto.QueryType.Value == (int)EmAppDataServiceType.IntegrationWebApiCall)
                    {
                        int? apiId = ControlTypeValueConverter.ConvertValueToInt(datasetExDto.QueryText);

                        if (apiId.HasValue)
                        {
                            var apiOperationDto = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(apiId.Value);

                            SearchApiSettingDto apiSettingDto = InitSearchApiSettingDto(apiOperationDto);
                            apiSettingDto.ConsumeOrProvideType = "Consume";
                            toReturn.Add(apiSettingDto);
                        }

                    }


                }


                // Provide APIs: 

                AppIntergrationSettingExDto publicApiBuilderExDto = AppIntergrationSettingBL.TryRetrieveAppBuiltInProviderExDto();
                if (publicApiBuilderExDto?.AppIntergrationSettingParameterList != null)
                {
                    foreach (var publicApiDto in publicApiBuilderExDto.AppIntergrationSettingParameterList)
                    {
                        if (publicApiDto.TranscationFieId.HasValue && publicApiDto.TranscationFieId.Value == searchId.Value)
                        {
                            SearchApiSettingDto apiSettingDto = InitSearchApiSettingDto(publicApiDto);
                            apiSettingDto.ConsumeOrProvideType = "Provide";
                            toReturn.Add(apiSettingDto);
                        }
                    }
                }

            }


            return toReturn;
        }

        private static SearchApiSettingDto InitSearchApiSettingDto(AppIntergrationSettingParameterExDto apiDto)
        {
            SearchApiSettingDto apiSettingDto = new SearchApiSettingDto();

            apiSettingDto.OperationId = (int)apiDto.Id;
            apiSettingDto.ActionCode = apiDto.ActionCode;
            apiSettingDto.HttpMethd = apiDto.HttpMethd;
            //apiSettingDto.BaseUrl = apiDto.APIConfigParameters.BaseUrl;
            //apiSettingDto.Url = apiDto.APIConfigParameters.Url;

            return apiSettingDto;
        }





        //public static List<AppSearchDto> RetrieveAllAppSearchByLinkTargetTransactionId(int? transactionId)
        //{
        //    List<AppSearchDto> toReturn = new List<AppSearchDto>();

        //    if (transactionId.HasValue)
        //    {
        //        List<int> searchIdList = new List<int>();

        //        using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
        //        {
        //            string queryGetSearchId = @"SELECT SearchID FROM AppSearch WHERE DataSetID IN (
        //             select DISTINCT DataSetID from AppSearchView where SearchViewID in (
        //              select DISTINCT SearchViewID from AppFormLinkTarget where SearchViewID is not null and LinkTargetTransactionID = @TransactionID
        //             )
        //            )";

        //            List<SqlParameter> listPars = new List<SqlParameter>();
        //            listPars.Add(new SqlParameter("@TransactionID", transactionId));

        //            var tableData = adapter.ExecuteDataTableRetrievalQuery(queryGetSearchId, listPars).AsEnumerable();



        //            foreach (var rowData in tableData)
        //            {
        //                int? searchId = ControlTypeValueConverter.ConvertValueToInt(rowData["SearchID"]);
        //                if (searchId.HasValue)
        //                {
        //                    searchIdList.Add(searchId.Value);
        //                }                        
        //            }
        //        }

        //        if (searchIdList.Count > 0)
        //        {
        //            List<AppSearchDto> allSearchDtoList = RetrieveAllAppSearchDto();
        //            toReturn = allSearchDtoList.Where(o => searchIdList.Contains((int)o.Id)).ToList();
        //        }
        //    }



        //    return toReturn;
        //}
    }
}