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
    /// Convert Properties between  AppCalendarEntity and  AppCalendarDto
    /// </summary>
    public static partial class AppCalendarConverter 
    {
         /// <summary>
        ///  Convert AppCalendarEntity To  AppCalendarDto
        /// </summary>
        public static AppCalendarDto ConvertEntityToDto(AppCalendarEntity aAppCalendarEntity)
        {        
    		AppCalendarDto aAppCalendarDto = new AppCalendarDto();
    		CopyEntityPropertyToDto( aAppCalendarEntity, aAppCalendarDto);          
			return aAppCalendarDto;
        }
		 /// <summary>
        ///  Convert AppCalendarEntity To  AppCalendarExDto
        /// </summary>
        public static AppCalendarExDto ConvertEntityToExDto(AppCalendarEntity aAppCalendarEntity)
        {        
    		AppCalendarExDto aAppCalendarExDto = new AppCalendarExDto();
			CopyEntityPropertyToDto( aAppCalendarEntity, aAppCalendarExDto);
			
			
			
            return aAppCalendarExDto;
        }
		
		 /// <summary>
        ///  Convert AppCalendarEntity To  AppCalendarDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppCalendarEntity aAppCalendarEntity,AppCalendarDto aAppCalendarDto)
        {        
    		
           // aAppCalendarDto.StopChangeTracking();
 			aAppCalendarDto.Id = aAppCalendarEntity.CalendarId;
 			aAppCalendarDto.Name = aAppCalendarEntity.Name;
 			aAppCalendarDto.Description = aAppCalendarEntity.Description;
 			aAppCalendarDto.IsCompanyCalendar = aAppCalendarEntity.IsCompanyCalendar;
 			aAppCalendarDto.IsCompanyDefaultCalendar = aAppCalendarEntity.IsCompanyDefaultCalendar;
 			aAppCalendarDto.UserId = aAppCalendarEntity.UserId;
 			aAppCalendarDto.AppCreatedById = aAppCalendarEntity.AppCreatedById;
 			aAppCalendarDto.AppCreatedDate = aAppCalendarEntity.AppCreatedDate;
 			aAppCalendarDto.AppModifiedDate = aAppCalendarEntity.AppModifiedDate;
 			aAppCalendarDto.AppModifiedById = aAppCalendarEntity.AppModifiedById;
 			aAppCalendarDto.AppCreatedByCompanyId = aAppCalendarEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppCalendarDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCalendarEntity.AppCreatedDate);
                aAppCalendarDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCalendarEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppCalendarEntity, aAppCalendarDto);
		}
		
		 /// <summary>
        ///  Copy AppCalendarDto Properties to   AppCalendarEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppCalendarEntity aAppCalendarEntity,AppCalendarDto aAppCalendarDto)
        {        
 
      			aAppCalendarEntity.Name = aAppCalendarDto.Name;
      			aAppCalendarEntity.Description = aAppCalendarDto.Description;
      			aAppCalendarEntity.IsCompanyCalendar = aAppCalendarDto.IsCompanyCalendar;
      			aAppCalendarEntity.IsCompanyDefaultCalendar = aAppCalendarDto.IsCompanyDefaultCalendar;
      			aAppCalendarEntity.UserId = aAppCalendarDto.UserId;
 
  
   
    
      			aAppCalendarEntity.AppCreatedByCompanyId = aAppCalendarDto.AppCreatedByCompanyId;
			
			if(aAppCalendarDto.Id == null)
			{
				aAppCalendarEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppCalendarEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppCalendarEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppCalendarEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppCalendarEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppCalendarEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppCalendarEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppCalendarEntity, aAppCalendarDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppCalendarEntity aAppCalendarEntity,AppCalendarDto aAppCalendarDto);
		
		static partial void OnCopyDtoToEntityDone(AppCalendarEntity aAppCalendarEntity,AppCalendarDto aAppCalendarDto);
		
   
       
    }
}

 