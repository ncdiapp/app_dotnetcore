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
    /// Convert Properties between  AppCalendarRecurringDayEntity and  AppCalendarRecurringDayDto
    /// </summary>
    public static partial class AppCalendarRecurringDayConverter 
    {
         /// <summary>
        ///  Convert AppCalendarRecurringDayEntity To  AppCalendarRecurringDayDto
        /// </summary>
        public static AppCalendarRecurringDayDto ConvertEntityToDto(AppCalendarRecurringDayEntity aAppCalendarRecurringDayEntity)
        {        
    		AppCalendarRecurringDayDto aAppCalendarRecurringDayDto = new AppCalendarRecurringDayDto();
    		CopyEntityPropertyToDto( aAppCalendarRecurringDayEntity, aAppCalendarRecurringDayDto);          
			return aAppCalendarRecurringDayDto;
        }
		 /// <summary>
        ///  Convert AppCalendarRecurringDayEntity To  AppCalendarRecurringDayExDto
        /// </summary>
        public static AppCalendarRecurringDayExDto ConvertEntityToExDto(AppCalendarRecurringDayEntity aAppCalendarRecurringDayEntity)
        {        
    		AppCalendarRecurringDayExDto aAppCalendarRecurringDayExDto = new AppCalendarRecurringDayExDto();
			CopyEntityPropertyToDto( aAppCalendarRecurringDayEntity, aAppCalendarRecurringDayExDto);
			
			
			
            return aAppCalendarRecurringDayExDto;
        }
		
		 /// <summary>
        ///  Convert AppCalendarRecurringDayEntity To  AppCalendarRecurringDayDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppCalendarRecurringDayEntity aAppCalendarRecurringDayEntity,AppCalendarRecurringDayDto aAppCalendarRecurringDayDto)
        {        
    		
           // aAppCalendarRecurringDayDto.StopChangeTracking();
 			aAppCalendarRecurringDayDto.Id = aAppCalendarRecurringDayEntity.RecurringDayId;
 			aAppCalendarRecurringDayDto.CalendarId = aAppCalendarRecurringDayEntity.CalendarId;
 			aAppCalendarRecurringDayDto.Name = aAppCalendarRecurringDayEntity.Name;
 			aAppCalendarRecurringDayDto.Description = aAppCalendarRecurringDayEntity.Description;
 			aAppCalendarRecurringDayDto.DateTokenType = aAppCalendarRecurringDayEntity.DateTokenType;
 			aAppCalendarRecurringDayDto.Month = aAppCalendarRecurringDayEntity.Month;
 			aAppCalendarRecurringDayDto.DayOfMonth = aAppCalendarRecurringDayEntity.DayOfMonth;
 			aAppCalendarRecurringDayDto.DayOfWeek = aAppCalendarRecurringDayEntity.DayOfWeek;
 			aAppCalendarRecurringDayDto.WorkStatus = aAppCalendarRecurringDayEntity.WorkStatus;
 			aAppCalendarRecurringDayDto.RecurringStartDate = aAppCalendarRecurringDayEntity.RecurringStartDate;
 			aAppCalendarRecurringDayDto.RecurringEndDate = aAppCalendarRecurringDayEntity.RecurringEndDate;
 			aAppCalendarRecurringDayDto.RecurringType = aAppCalendarRecurringDayEntity.RecurringType;
 			aAppCalendarRecurringDayDto.AppCreatedById = aAppCalendarRecurringDayEntity.AppCreatedById;
 			aAppCalendarRecurringDayDto.AppCreatedDate = aAppCalendarRecurringDayEntity.AppCreatedDate;
 			aAppCalendarRecurringDayDto.AppModifiedDate = aAppCalendarRecurringDayEntity.AppModifiedDate;
 			aAppCalendarRecurringDayDto.AppModifiedById = aAppCalendarRecurringDayEntity.AppModifiedById;
 			aAppCalendarRecurringDayDto.AppCreatedByCompanyId = aAppCalendarRecurringDayEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppCalendarRecurringDayDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCalendarRecurringDayEntity.AppCreatedDate);
                aAppCalendarRecurringDayDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCalendarRecurringDayEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppCalendarRecurringDayEntity, aAppCalendarRecurringDayDto);
		}
		
		 /// <summary>
        ///  Copy AppCalendarRecurringDayDto Properties to   AppCalendarRecurringDayEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppCalendarRecurringDayEntity aAppCalendarRecurringDayEntity,AppCalendarRecurringDayDto aAppCalendarRecurringDayDto)
        {        
 
      			aAppCalendarRecurringDayEntity.CalendarId = aAppCalendarRecurringDayDto.CalendarId;
      			aAppCalendarRecurringDayEntity.Name = aAppCalendarRecurringDayDto.Name;
      			aAppCalendarRecurringDayEntity.Description = aAppCalendarRecurringDayDto.Description;
      			aAppCalendarRecurringDayEntity.DateTokenType = aAppCalendarRecurringDayDto.DateTokenType;
      			aAppCalendarRecurringDayEntity.Month = aAppCalendarRecurringDayDto.Month;
      			aAppCalendarRecurringDayEntity.DayOfMonth = aAppCalendarRecurringDayDto.DayOfMonth;
      			aAppCalendarRecurringDayEntity.DayOfWeek = aAppCalendarRecurringDayDto.DayOfWeek;
      			aAppCalendarRecurringDayEntity.WorkStatus = aAppCalendarRecurringDayDto.WorkStatus;
      			aAppCalendarRecurringDayEntity.RecurringStartDate = aAppCalendarRecurringDayDto.RecurringStartDate;
      			aAppCalendarRecurringDayEntity.RecurringEndDate = aAppCalendarRecurringDayDto.RecurringEndDate;
      			aAppCalendarRecurringDayEntity.RecurringType = aAppCalendarRecurringDayDto.RecurringType;
 
  
   
    
      			aAppCalendarRecurringDayEntity.AppCreatedByCompanyId = aAppCalendarRecurringDayDto.AppCreatedByCompanyId;
			
			if(aAppCalendarRecurringDayDto.Id == null)
			{
				aAppCalendarRecurringDayEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppCalendarRecurringDayEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppCalendarRecurringDayEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppCalendarRecurringDayEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppCalendarRecurringDayEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppCalendarRecurringDayEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppCalendarRecurringDayEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppCalendarRecurringDayEntity, aAppCalendarRecurringDayDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppCalendarRecurringDayEntity aAppCalendarRecurringDayEntity,AppCalendarRecurringDayDto aAppCalendarRecurringDayDto);
		
		static partial void OnCopyDtoToEntityDone(AppCalendarRecurringDayEntity aAppCalendarRecurringDayEntity,AppCalendarRecurringDayDto aAppCalendarRecurringDayDto);
		
   
       
    }
}

 