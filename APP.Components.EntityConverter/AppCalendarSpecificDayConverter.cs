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
    /// Convert Properties between  AppCalendarSpecificDayEntity and  AppCalendarSpecificDayDto
    /// </summary>
    public static partial class AppCalendarSpecificDayConverter 
    {
         /// <summary>
        ///  Convert AppCalendarSpecificDayEntity To  AppCalendarSpecificDayDto
        /// </summary>
        public static AppCalendarSpecificDayDto ConvertEntityToDto(AppCalendarSpecificDayEntity aAppCalendarSpecificDayEntity)
        {        
    		AppCalendarSpecificDayDto aAppCalendarSpecificDayDto = new AppCalendarSpecificDayDto();
    		CopyEntityPropertyToDto( aAppCalendarSpecificDayEntity, aAppCalendarSpecificDayDto);          
			return aAppCalendarSpecificDayDto;
        }
		 /// <summary>
        ///  Convert AppCalendarSpecificDayEntity To  AppCalendarSpecificDayExDto
        /// </summary>
        public static AppCalendarSpecificDayExDto ConvertEntityToExDto(AppCalendarSpecificDayEntity aAppCalendarSpecificDayEntity)
        {        
    		AppCalendarSpecificDayExDto aAppCalendarSpecificDayExDto = new AppCalendarSpecificDayExDto();
			CopyEntityPropertyToDto( aAppCalendarSpecificDayEntity, aAppCalendarSpecificDayExDto);
			
			
			
            return aAppCalendarSpecificDayExDto;
        }
		
		 /// <summary>
        ///  Convert AppCalendarSpecificDayEntity To  AppCalendarSpecificDayDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppCalendarSpecificDayEntity aAppCalendarSpecificDayEntity,AppCalendarSpecificDayDto aAppCalendarSpecificDayDto)
        {        
    		
           // aAppCalendarSpecificDayDto.StopChangeTracking();
 			aAppCalendarSpecificDayDto.Id = aAppCalendarSpecificDayEntity.CalendarDayId;
 			aAppCalendarSpecificDayDto.CalendarId = aAppCalendarSpecificDayEntity.CalendarId;
 			aAppCalendarSpecificDayDto.Name = aAppCalendarSpecificDayEntity.Name;
 			aAppCalendarSpecificDayDto.Description = aAppCalendarSpecificDayEntity.Description;
 			aAppCalendarSpecificDayDto.StartDate = aAppCalendarSpecificDayEntity.StartDate;
 			aAppCalendarSpecificDayDto.EndDate = aAppCalendarSpecificDayEntity.EndDate;
 			aAppCalendarSpecificDayDto.WorkStatus = aAppCalendarSpecificDayEntity.WorkStatus;
 			aAppCalendarSpecificDayDto.EmDateDefineType = aAppCalendarSpecificDayEntity.EmDateDefineType;
 			aAppCalendarSpecificDayDto.EmDateRangeType = aAppCalendarSpecificDayEntity.EmDateRangeType;
 			aAppCalendarSpecificDayDto.AppCreatedById = aAppCalendarSpecificDayEntity.AppCreatedById;
 			aAppCalendarSpecificDayDto.AppCreatedDate = aAppCalendarSpecificDayEntity.AppCreatedDate;
 			aAppCalendarSpecificDayDto.AppModifiedDate = aAppCalendarSpecificDayEntity.AppModifiedDate;
 			aAppCalendarSpecificDayDto.AppModifiedById = aAppCalendarSpecificDayEntity.AppModifiedById;
 			aAppCalendarSpecificDayDto.AppCreatedByCompanyId = aAppCalendarSpecificDayEntity.AppCreatedByCompanyId;
 			aAppCalendarSpecificDayDto.UserDefined1 = aAppCalendarSpecificDayEntity.UserDefined1;
 			aAppCalendarSpecificDayDto.UserDefined2 = aAppCalendarSpecificDayEntity.UserDefined2;
 			aAppCalendarSpecificDayDto.UserDefined3 = aAppCalendarSpecificDayEntity.UserDefined3;
 			aAppCalendarSpecificDayDto.UserDefined4 = aAppCalendarSpecificDayEntity.UserDefined4;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppCalendarSpecificDayDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCalendarSpecificDayEntity.AppCreatedDate);
                aAppCalendarSpecificDayDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCalendarSpecificDayEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppCalendarSpecificDayEntity, aAppCalendarSpecificDayDto);
		}
		
		 /// <summary>
        ///  Copy AppCalendarSpecificDayDto Properties to   AppCalendarSpecificDayEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppCalendarSpecificDayEntity aAppCalendarSpecificDayEntity,AppCalendarSpecificDayDto aAppCalendarSpecificDayDto)
        {        
 
      			aAppCalendarSpecificDayEntity.CalendarId = aAppCalendarSpecificDayDto.CalendarId;
      			aAppCalendarSpecificDayEntity.Name = aAppCalendarSpecificDayDto.Name;
      			aAppCalendarSpecificDayEntity.Description = aAppCalendarSpecificDayDto.Description;
      			aAppCalendarSpecificDayEntity.StartDate = aAppCalendarSpecificDayDto.StartDate;
      			aAppCalendarSpecificDayEntity.EndDate = aAppCalendarSpecificDayDto.EndDate;
      			aAppCalendarSpecificDayEntity.WorkStatus = aAppCalendarSpecificDayDto.WorkStatus;
      			aAppCalendarSpecificDayEntity.EmDateDefineType = aAppCalendarSpecificDayDto.EmDateDefineType;
      			aAppCalendarSpecificDayEntity.EmDateRangeType = aAppCalendarSpecificDayDto.EmDateRangeType;
 
  
   
    
      			aAppCalendarSpecificDayEntity.AppCreatedByCompanyId = aAppCalendarSpecificDayDto.AppCreatedByCompanyId;
      			aAppCalendarSpecificDayEntity.UserDefined1 = aAppCalendarSpecificDayDto.UserDefined1;
      			aAppCalendarSpecificDayEntity.UserDefined2 = aAppCalendarSpecificDayDto.UserDefined2;
      			aAppCalendarSpecificDayEntity.UserDefined3 = aAppCalendarSpecificDayDto.UserDefined3;
      			aAppCalendarSpecificDayEntity.UserDefined4 = aAppCalendarSpecificDayDto.UserDefined4;
			
			if(aAppCalendarSpecificDayDto.Id == null)
			{
				aAppCalendarSpecificDayEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppCalendarSpecificDayEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppCalendarSpecificDayEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppCalendarSpecificDayEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppCalendarSpecificDayEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppCalendarSpecificDayEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppCalendarSpecificDayEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppCalendarSpecificDayEntity, aAppCalendarSpecificDayDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppCalendarSpecificDayEntity aAppCalendarSpecificDayEntity,AppCalendarSpecificDayDto aAppCalendarSpecificDayDto);
		
		static partial void OnCopyDtoToEntityDone(AppCalendarSpecificDayEntity aAppCalendarSpecificDayEntity,AppCalendarSpecificDayDto aAppCalendarSpecificDayDto);
		
   
       
    }
}

 