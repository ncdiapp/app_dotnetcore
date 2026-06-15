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
    /// Convert Properties between  AppCalendarBaseDateEntity and  AppCalendarBaseDateDto
    /// </summary>
    public static partial class AppCalendarBaseDateConverter 
    {
         /// <summary>
        ///  Convert AppCalendarBaseDateEntity To  AppCalendarBaseDateDto
        /// </summary>
        public static AppCalendarBaseDateDto ConvertEntityToDto(AppCalendarBaseDateEntity aAppCalendarBaseDateEntity)
        {        
    		AppCalendarBaseDateDto aAppCalendarBaseDateDto = new AppCalendarBaseDateDto();
    		CopyEntityPropertyToDto( aAppCalendarBaseDateEntity, aAppCalendarBaseDateDto);          
			return aAppCalendarBaseDateDto;
        }
		 /// <summary>
        ///  Convert AppCalendarBaseDateEntity To  AppCalendarBaseDateExDto
        /// </summary>
        public static AppCalendarBaseDateExDto ConvertEntityToExDto(AppCalendarBaseDateEntity aAppCalendarBaseDateEntity)
        {        
    		AppCalendarBaseDateExDto aAppCalendarBaseDateExDto = new AppCalendarBaseDateExDto();
			CopyEntityPropertyToDto( aAppCalendarBaseDateEntity, aAppCalendarBaseDateExDto);
			
			
			
            return aAppCalendarBaseDateExDto;
        }
		
		 /// <summary>
        ///  Convert AppCalendarBaseDateEntity To  AppCalendarBaseDateDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppCalendarBaseDateEntity aAppCalendarBaseDateEntity,AppCalendarBaseDateDto aAppCalendarBaseDateDto)
        {        
    		
           // aAppCalendarBaseDateDto.StopChangeTracking();
 			aAppCalendarBaseDateDto.Id = aAppCalendarBaseDateEntity.Day;
 			aAppCalendarBaseDateDto.DayDesc = aAppCalendarBaseDateEntity.DayDesc;
 			aAppCalendarBaseDateDto.Week = aAppCalendarBaseDateEntity.Week;
 			aAppCalendarBaseDateDto.WeekDesc = aAppCalendarBaseDateEntity.WeekDesc;
 			aAppCalendarBaseDateDto.BiWeek = aAppCalendarBaseDateEntity.BiWeek;
 			aAppCalendarBaseDateDto.BiWeekDesc = aAppCalendarBaseDateEntity.BiWeekDesc;
 			aAppCalendarBaseDateDto.HlfMonth = aAppCalendarBaseDateEntity.HlfMonth;
 			aAppCalendarBaseDateDto.HlfMonthDesc = aAppCalendarBaseDateEntity.HlfMonthDesc;
 			aAppCalendarBaseDateDto.Month = aAppCalendarBaseDateEntity.Month;
 			aAppCalendarBaseDateDto.MonthDesc = aAppCalendarBaseDateEntity.MonthDesc;
 			aAppCalendarBaseDateDto.Quarter = aAppCalendarBaseDateEntity.Quarter;
 			aAppCalendarBaseDateDto.QuarterDesc = aAppCalendarBaseDateEntity.QuarterDesc;
 			aAppCalendarBaseDateDto.PlnHlfYr = aAppCalendarBaseDateEntity.PlnHlfYr;
 			aAppCalendarBaseDateDto.PlnHlfYrDesc = aAppCalendarBaseDateEntity.PlnHlfYrDesc;
 			aAppCalendarBaseDateDto.PlnYr = aAppCalendarBaseDateEntity.PlnYr;
 			aAppCalendarBaseDateDto.PlnYrDesc = aAppCalendarBaseDateEntity.PlnYrDesc;
 			aAppCalendarBaseDateDto.FiscalHlfYr = aAppCalendarBaseDateEntity.FiscalHlfYr;
 			aAppCalendarBaseDateDto.FiscalHlfYrDesc = aAppCalendarBaseDateEntity.FiscalHlfYrDesc;
 			aAppCalendarBaseDateDto.FiscalYr = aAppCalendarBaseDateEntity.FiscalYr;
 			aAppCalendarBaseDateDto.FiscalYrDesc = aAppCalendarBaseDateEntity.FiscalYrDesc;
 			aAppCalendarBaseDateDto.RangePeriod = aAppCalendarBaseDateEntity.RangePeriod;
 			aAppCalendarBaseDateDto.AppCreatedById = aAppCalendarBaseDateEntity.AppCreatedById;
 			aAppCalendarBaseDateDto.AppCreatedDate = aAppCalendarBaseDateEntity.AppCreatedDate;
 			aAppCalendarBaseDateDto.AppModifiedDate = aAppCalendarBaseDateEntity.AppModifiedDate;
 			aAppCalendarBaseDateDto.AppModifiedById = aAppCalendarBaseDateEntity.AppModifiedById;
 			aAppCalendarBaseDateDto.AppCreatedByCompanyId = aAppCalendarBaseDateEntity.AppCreatedByCompanyId;
 			aAppCalendarBaseDateDto.RangePeriodDesc = aAppCalendarBaseDateEntity.RangePeriodDesc;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppCalendarBaseDateDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCalendarBaseDateEntity.AppCreatedDate);
                aAppCalendarBaseDateDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCalendarBaseDateEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppCalendarBaseDateEntity, aAppCalendarBaseDateDto);
		}
		
		 /// <summary>
        ///  Copy AppCalendarBaseDateDto Properties to   AppCalendarBaseDateEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppCalendarBaseDateEntity aAppCalendarBaseDateEntity,AppCalendarBaseDateDto aAppCalendarBaseDateDto)
        {        
 
      			aAppCalendarBaseDateEntity.DayDesc = aAppCalendarBaseDateDto.DayDesc;
      			aAppCalendarBaseDateEntity.Week = aAppCalendarBaseDateDto.Week;
      			aAppCalendarBaseDateEntity.WeekDesc = aAppCalendarBaseDateDto.WeekDesc;
      			aAppCalendarBaseDateEntity.BiWeek = aAppCalendarBaseDateDto.BiWeek;
      			aAppCalendarBaseDateEntity.BiWeekDesc = aAppCalendarBaseDateDto.BiWeekDesc;
      			aAppCalendarBaseDateEntity.HlfMonth = aAppCalendarBaseDateDto.HlfMonth;
      			aAppCalendarBaseDateEntity.HlfMonthDesc = aAppCalendarBaseDateDto.HlfMonthDesc;
      			aAppCalendarBaseDateEntity.Month = aAppCalendarBaseDateDto.Month;
      			aAppCalendarBaseDateEntity.MonthDesc = aAppCalendarBaseDateDto.MonthDesc;
      			aAppCalendarBaseDateEntity.Quarter = aAppCalendarBaseDateDto.Quarter;
      			aAppCalendarBaseDateEntity.QuarterDesc = aAppCalendarBaseDateDto.QuarterDesc;
      			aAppCalendarBaseDateEntity.PlnHlfYr = aAppCalendarBaseDateDto.PlnHlfYr;
      			aAppCalendarBaseDateEntity.PlnHlfYrDesc = aAppCalendarBaseDateDto.PlnHlfYrDesc;
      			aAppCalendarBaseDateEntity.PlnYr = aAppCalendarBaseDateDto.PlnYr;
      			aAppCalendarBaseDateEntity.PlnYrDesc = aAppCalendarBaseDateDto.PlnYrDesc;
      			aAppCalendarBaseDateEntity.FiscalHlfYr = aAppCalendarBaseDateDto.FiscalHlfYr;
      			aAppCalendarBaseDateEntity.FiscalHlfYrDesc = aAppCalendarBaseDateDto.FiscalHlfYrDesc;
      			aAppCalendarBaseDateEntity.FiscalYr = aAppCalendarBaseDateDto.FiscalYr;
      			aAppCalendarBaseDateEntity.FiscalYrDesc = aAppCalendarBaseDateDto.FiscalYrDesc;
      			aAppCalendarBaseDateEntity.RangePeriod = aAppCalendarBaseDateDto.RangePeriod;
 
  
   
    
      			aAppCalendarBaseDateEntity.AppCreatedByCompanyId = aAppCalendarBaseDateDto.AppCreatedByCompanyId;
      			aAppCalendarBaseDateEntity.RangePeriodDesc = aAppCalendarBaseDateDto.RangePeriodDesc;
			
			if(aAppCalendarBaseDateDto.Id == null)
			{
				aAppCalendarBaseDateEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppCalendarBaseDateEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppCalendarBaseDateEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppCalendarBaseDateEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppCalendarBaseDateEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppCalendarBaseDateEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppCalendarBaseDateEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppCalendarBaseDateEntity, aAppCalendarBaseDateDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppCalendarBaseDateEntity aAppCalendarBaseDateEntity,AppCalendarBaseDateDto aAppCalendarBaseDateDto);
		
		static partial void OnCopyDtoToEntityDone(AppCalendarBaseDateEntity aAppCalendarBaseDateEntity,AppCalendarBaseDateDto aAppCalendarBaseDateDto);
		
   
       
    }
}

 