using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

using APP.Framework;
using APP.Framework.Collections;
using APP.LBL.EntityClasses;
using APP.Components.EntityDto;



namespace APP.Components.EntityConverter
{
    /// <summary>
    /// Convert Properties between  AppSearchViewEntity and  AppSearchViewDto
    /// </summary>
    public static partial class AppSearchViewConverter 
    {
         /// <summary>
        ///  Convert AppSearchViewEntity To  AppSearchViewDto
        /// </summary>
        public static AppSearchViewDto ConvertEntityToDto(AppSearchViewEntity aAppSearchViewEntity)
        {        
    		AppSearchViewDto aAppSearchViewDto = new AppSearchViewDto();
    		CopyEntityPropertyToDto( aAppSearchViewEntity, aAppSearchViewDto);          
			return aAppSearchViewDto;
        }
		 /// <summary>
        ///  Convert AppSearchViewEntity To  AppSearchViewExDto
        /// </summary>
        public static AppSearchViewExDto ConvertEntityToExDto(AppSearchViewEntity aAppSearchViewEntity)
        {        
    		AppSearchViewExDto aAppSearchViewExDto = new AppSearchViewExDto();
			CopyEntityPropertyToDto( aAppSearchViewEntity, aAppSearchViewExDto);
			
			
			
            return aAppSearchViewExDto;
        }
		
		 /// <summary>
        ///  Convert AppSearchViewEntity To  AppSearchViewDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSearchViewEntity aAppSearchViewEntity,AppSearchViewDto aAppSearchViewDto)
        {        
    		
           // aAppSearchViewDto.StopChangeTracking();
 			aAppSearchViewDto.Id = aAppSearchViewEntity.SearchViewId;
 			aAppSearchViewDto.Name = aAppSearchViewEntity.Name;
 			aAppSearchViewDto.Description = aAppSearchViewEntity.Description;
 			aAppSearchViewDto.NoSecurity = aAppSearchViewEntity.NoSecurity;
 			aAppSearchViewDto.GridOutputMode = aAppSearchViewEntity.GridOutputMode;
 			aAppSearchViewDto.Options = aAppSearchViewEntity.Options;
 			aAppSearchViewDto.ViewType = aAppSearchViewEntity.ViewType;
 			aAppSearchViewDto.WhereUsedDefaultViewId = aAppSearchViewEntity.WhereUsedDefaultViewId;
 			aAppSearchViewDto.PivotOrChartSetting = aAppSearchViewEntity.PivotOrChartSetting;
 			aAppSearchViewDto.ColumnCount = aAppSearchViewEntity.ColumnCount;
 			aAppSearchViewDto.RowPerPage = aAppSearchViewEntity.RowPerPage;
 			aAppSearchViewDto.IsFilterByCurrentUser = aAppSearchViewEntity.IsFilterByCurrentUser;
 			aAppSearchViewDto.DataSetId = aAppSearchViewEntity.DataSetId;
 			aAppSearchViewDto.ChartInnerRadius = aAppSearchViewEntity.ChartInnerRadius;
 			aAppSearchViewDto.ChartType = aAppSearchViewEntity.ChartType;
 			aAppSearchViewDto.CatalogueSearchId = aAppSearchViewEntity.CatalogueSearchId;
 			aAppSearchViewDto.FilterSearchId = aAppSearchViewEntity.FilterSearchId;
 			aAppSearchViewDto.EntityInternalCode = aAppSearchViewEntity.EntityInternalCode;
 			aAppSearchViewDto.TransactionId = aAppSearchViewEntity.TransactionId;
 			aAppSearchViewDto.ProductDetaiViewMapUnitId = aAppSearchViewEntity.ProductDetaiViewMapUnitId;
 			aAppSearchViewDto.IsMasterEditInSamePage = aAppSearchViewEntity.IsMasterEditInSamePage;
 			aAppSearchViewDto.UpdateTransctionId = aAppSearchViewEntity.UpdateTransctionId;
 			aAppSearchViewDto.UpdateTransctionRootFieldName = aAppSearchViewEntity.UpdateTransctionRootFieldName;
 			aAppSearchViewDto.UpdateChildParentFkfieldName = aAppSearchViewEntity.UpdateChildParentFkfieldName;
 			aAppSearchViewDto.UpdateBaseTranscationUnitId = aAppSearchViewEntity.UpdateBaseTranscationUnitId;
 			aAppSearchViewDto.AppRestResourceUri = aAppSearchViewEntity.AppRestResourceUri;
 			aAppSearchViewDto.AppRestResourceUriDisplay = aAppSearchViewEntity.AppRestResourceUriDisplay;
 			aAppSearchViewDto.NbFrozenColumn = aAppSearchViewEntity.NbFrozenColumn;
 			aAppSearchViewDto.IsMassUpdateView = aAppSearchViewEntity.IsMassUpdateView;
 			aAppSearchViewDto.IsAllowAddRow = aAppSearchViewEntity.IsAllowAddRow;
 			aAppSearchViewDto.IsAllowDeleteRow = aAppSearchViewEntity.IsAllowDeleteRow;
 			aAppSearchViewDto.IsAllowUpdateRow = aAppSearchViewEntity.IsAllowUpdateRow;
 			aAppSearchViewDto.AppCreatedById = aAppSearchViewEntity.AppCreatedById;
 			aAppSearchViewDto.AppCreatedDate = aAppSearchViewEntity.AppCreatedDate;
 			aAppSearchViewDto.AppModifiedDate = aAppSearchViewEntity.AppModifiedDate;
 			aAppSearchViewDto.AppModifiedById = aAppSearchViewEntity.AppModifiedById;
 			aAppSearchViewDto.AppCreatedByCompanyId = aAppSearchViewEntity.AppCreatedByCompanyId;
 			aAppSearchViewDto.CanlendarDefaultViewMode = aAppSearchViewEntity.CanlendarDefaultViewMode;
 			aAppSearchViewDto.IsEnableCalendarMonthView = aAppSearchViewEntity.IsEnableCalendarMonthView;
 			aAppSearchViewDto.IsEnableCalendarWeekView = aAppSearchViewEntity.IsEnableCalendarWeekView;
 			aAppSearchViewDto.IsEnableCalendarDayView = aAppSearchViewEntity.IsEnableCalendarDayView;
 			aAppSearchViewDto.IsEnableCalendarNavigator = aAppSearchViewEntity.IsEnableCalendarNavigator;
 			aAppSearchViewDto.IsDisableClientTimeConvert = aAppSearchViewEntity.IsDisableClientTimeConvert;
 			aAppSearchViewDto.SaasApplicationId = aAppSearchViewEntity.SaasApplicationId;
 			aAppSearchViewDto.IsForPublicAcesss = aAppSearchViewEntity.IsForPublicAcesss;
 			aAppSearchViewDto.IsFilterByUserTypeEntity = aAppSearchViewEntity.IsFilterByUserTypeEntity;
 			aAppSearchViewDto.CalendarStartHour = aAppSearchViewEntity.CalendarStartHour;
 			aAppSearchViewDto.CalendarEndHour = aAppSearchViewEntity.CalendarEndHour;
 			aAppSearchViewDto.HierachyParentViewId = aAppSearchViewEntity.HierachyParentViewId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSearchViewDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSearchViewEntity.AppCreatedDate);
                aAppSearchViewDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSearchViewEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSearchViewEntity, aAppSearchViewDto);
		}
		
		 /// <summary>
        ///  Copy AppSearchViewDto Properties to   AppSearchViewEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSearchViewEntity aAppSearchViewEntity,AppSearchViewDto aAppSearchViewDto)
        {        
 
      			aAppSearchViewEntity.Name = aAppSearchViewDto.Name;
      			aAppSearchViewEntity.Description = aAppSearchViewDto.Description;
      			aAppSearchViewEntity.NoSecurity = aAppSearchViewDto.NoSecurity;
      			aAppSearchViewEntity.GridOutputMode = aAppSearchViewDto.GridOutputMode;
      			aAppSearchViewEntity.Options = aAppSearchViewDto.Options;
      			aAppSearchViewEntity.ViewType = aAppSearchViewDto.ViewType;
      			aAppSearchViewEntity.WhereUsedDefaultViewId = aAppSearchViewDto.WhereUsedDefaultViewId;
      			aAppSearchViewEntity.PivotOrChartSetting = aAppSearchViewDto.PivotOrChartSetting;
      			aAppSearchViewEntity.ColumnCount = aAppSearchViewDto.ColumnCount;
      			aAppSearchViewEntity.RowPerPage = aAppSearchViewDto.RowPerPage;
      			aAppSearchViewEntity.IsFilterByCurrentUser = aAppSearchViewDto.IsFilterByCurrentUser;
      			aAppSearchViewEntity.DataSetId = aAppSearchViewDto.DataSetId;
      			aAppSearchViewEntity.ChartInnerRadius = aAppSearchViewDto.ChartInnerRadius;
      			aAppSearchViewEntity.ChartType = aAppSearchViewDto.ChartType;
      			aAppSearchViewEntity.CatalogueSearchId = aAppSearchViewDto.CatalogueSearchId;
      			aAppSearchViewEntity.FilterSearchId = aAppSearchViewDto.FilterSearchId;
      			aAppSearchViewEntity.EntityInternalCode = aAppSearchViewDto.EntityInternalCode;
      			aAppSearchViewEntity.TransactionId = aAppSearchViewDto.TransactionId;
      			aAppSearchViewEntity.ProductDetaiViewMapUnitId = aAppSearchViewDto.ProductDetaiViewMapUnitId;
      			aAppSearchViewEntity.IsMasterEditInSamePage = aAppSearchViewDto.IsMasterEditInSamePage;
      			aAppSearchViewEntity.UpdateTransctionId = aAppSearchViewDto.UpdateTransctionId;
      			aAppSearchViewEntity.UpdateTransctionRootFieldName = aAppSearchViewDto.UpdateTransctionRootFieldName;
      			aAppSearchViewEntity.UpdateChildParentFkfieldName = aAppSearchViewDto.UpdateChildParentFkfieldName;
      			aAppSearchViewEntity.UpdateBaseTranscationUnitId = aAppSearchViewDto.UpdateBaseTranscationUnitId;
      			aAppSearchViewEntity.AppRestResourceUri = aAppSearchViewDto.AppRestResourceUri;
      			aAppSearchViewEntity.AppRestResourceUriDisplay = aAppSearchViewDto.AppRestResourceUriDisplay;
      			aAppSearchViewEntity.NbFrozenColumn = aAppSearchViewDto.NbFrozenColumn;
      			aAppSearchViewEntity.IsMassUpdateView = aAppSearchViewDto.IsMassUpdateView;
      			aAppSearchViewEntity.IsAllowAddRow = aAppSearchViewDto.IsAllowAddRow;
      			aAppSearchViewEntity.IsAllowDeleteRow = aAppSearchViewDto.IsAllowDeleteRow;
      			aAppSearchViewEntity.IsAllowUpdateRow = aAppSearchViewDto.IsAllowUpdateRow;
 
  
   
    
      			aAppSearchViewEntity.AppCreatedByCompanyId = aAppSearchViewDto.AppCreatedByCompanyId;
      			aAppSearchViewEntity.CanlendarDefaultViewMode = aAppSearchViewDto.CanlendarDefaultViewMode;
      			aAppSearchViewEntity.IsEnableCalendarMonthView = aAppSearchViewDto.IsEnableCalendarMonthView;
      			aAppSearchViewEntity.IsEnableCalendarWeekView = aAppSearchViewDto.IsEnableCalendarWeekView;
      			aAppSearchViewEntity.IsEnableCalendarDayView = aAppSearchViewDto.IsEnableCalendarDayView;
      			aAppSearchViewEntity.IsEnableCalendarNavigator = aAppSearchViewDto.IsEnableCalendarNavigator;
      			aAppSearchViewEntity.IsDisableClientTimeConvert = aAppSearchViewDto.IsDisableClientTimeConvert;
      			aAppSearchViewEntity.SaasApplicationId = aAppSearchViewDto.SaasApplicationId;
      			aAppSearchViewEntity.IsForPublicAcesss = aAppSearchViewDto.IsForPublicAcesss;
      			aAppSearchViewEntity.IsFilterByUserTypeEntity = aAppSearchViewDto.IsFilterByUserTypeEntity;
      			aAppSearchViewEntity.CalendarStartHour = aAppSearchViewDto.CalendarStartHour;
      			aAppSearchViewEntity.CalendarEndHour = aAppSearchViewDto.CalendarEndHour;
      			aAppSearchViewEntity.HierachyParentViewId = aAppSearchViewDto.HierachyParentViewId;
			
			if(aAppSearchViewDto.Id == null)
			{
				aAppSearchViewEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSearchViewEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSearchViewEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSearchViewEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSearchViewEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSearchViewEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSearchViewEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSearchViewEntity, aAppSearchViewDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSearchViewEntity aAppSearchViewEntity,AppSearchViewDto aAppSearchViewDto);
		
		static partial void OnCopyDtoToEntityDone(AppSearchViewEntity aAppSearchViewEntity,AppSearchViewDto aAppSearchViewDto);
		
   
       
    }
}

 