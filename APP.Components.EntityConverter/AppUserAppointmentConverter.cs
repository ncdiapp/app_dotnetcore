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
    /// Convert Properties between  AppUserAppointmentEntity and  AppUserAppointmentDto
    /// </summary>
    public static partial class AppUserAppointmentConverter 
    {
         /// <summary>
        ///  Convert AppUserAppointmentEntity To  AppUserAppointmentDto
        /// </summary>
        public static AppUserAppointmentDto ConvertEntityToDto(AppUserAppointmentEntity aAppUserAppointmentEntity)
        {        
    		AppUserAppointmentDto aAppUserAppointmentDto = new AppUserAppointmentDto();
    		CopyEntityPropertyToDto( aAppUserAppointmentEntity, aAppUserAppointmentDto);          
			return aAppUserAppointmentDto;
        }
		 /// <summary>
        ///  Convert AppUserAppointmentEntity To  AppUserAppointmentExDto
        /// </summary>
        public static AppUserAppointmentExDto ConvertEntityToExDto(AppUserAppointmentEntity aAppUserAppointmentEntity)
        {        
    		AppUserAppointmentExDto aAppUserAppointmentExDto = new AppUserAppointmentExDto();
			CopyEntityPropertyToDto( aAppUserAppointmentEntity, aAppUserAppointmentExDto);
			
			
			
            return aAppUserAppointmentExDto;
        }
		
		 /// <summary>
        ///  Convert AppUserAppointmentEntity To  AppUserAppointmentDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppUserAppointmentEntity aAppUserAppointmentEntity,AppUserAppointmentDto aAppUserAppointmentDto)
        {        
    		
           // aAppUserAppointmentDto.StopChangeTracking();
 			aAppUserAppointmentDto.Id = aAppUserAppointmentEntity.UserAppointmentId;
 			aAppUserAppointmentDto.UserId = aAppUserAppointmentEntity.UserId;
 			aAppUserAppointmentDto.Subject = aAppUserAppointmentEntity.Subject;
 			aAppUserAppointmentDto.Body = aAppUserAppointmentEntity.Body;
 			aAppUserAppointmentDto.DateStart = aAppUserAppointmentEntity.DateStart;
 			aAppUserAppointmentDto.DateEnd = aAppUserAppointmentEntity.DateEnd;
 			aAppUserAppointmentDto.IsAllDay = aAppUserAppointmentEntity.IsAllDay;
 			aAppUserAppointmentDto.Importance = aAppUserAppointmentEntity.Importance;
 			aAppUserAppointmentDto.Location = aAppUserAppointmentEntity.Location;
 			aAppUserAppointmentDto.OriginalAppointmentId = aAppUserAppointmentEntity.OriginalAppointmentId;
 			aAppUserAppointmentDto.UserCc = aAppUserAppointmentEntity.UserCc;
 			aAppUserAppointmentDto.SystemTimeStamp = aAppUserAppointmentEntity.SystemTimeStamp;
 			aAppUserAppointmentDto.AppCreatedById = aAppUserAppointmentEntity.AppCreatedById;
 			aAppUserAppointmentDto.AppCreatedDate = aAppUserAppointmentEntity.AppCreatedDate;
 			aAppUserAppointmentDto.AppModifiedDate = aAppUserAppointmentEntity.AppModifiedDate;
 			aAppUserAppointmentDto.AppModifiedById = aAppUserAppointmentEntity.AppModifiedById;
 			aAppUserAppointmentDto.AppCreatedByCompanyId = aAppUserAppointmentEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppUserAppointmentDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppUserAppointmentEntity.AppCreatedDate);
                aAppUserAppointmentDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppUserAppointmentEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppUserAppointmentEntity, aAppUserAppointmentDto);
		}
		
		 /// <summary>
        ///  Copy AppUserAppointmentDto Properties to   AppUserAppointmentEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppUserAppointmentEntity aAppUserAppointmentEntity,AppUserAppointmentDto aAppUserAppointmentDto)
        {        
 
      			aAppUserAppointmentEntity.UserId = aAppUserAppointmentDto.UserId;
      			aAppUserAppointmentEntity.Subject = aAppUserAppointmentDto.Subject;
      			aAppUserAppointmentEntity.Body = aAppUserAppointmentDto.Body;
      			aAppUserAppointmentEntity.DateStart = aAppUserAppointmentDto.DateStart;
      			aAppUserAppointmentEntity.DateEnd = aAppUserAppointmentDto.DateEnd;
      			aAppUserAppointmentEntity.IsAllDay = aAppUserAppointmentDto.IsAllDay;
      			aAppUserAppointmentEntity.Importance = aAppUserAppointmentDto.Importance;
      			aAppUserAppointmentEntity.Location = aAppUserAppointmentDto.Location;
      			aAppUserAppointmentEntity.OriginalAppointmentId = aAppUserAppointmentDto.OriginalAppointmentId;
      			aAppUserAppointmentEntity.UserCc = aAppUserAppointmentDto.UserCc;
 
 
  
   
    
      			aAppUserAppointmentEntity.AppCreatedByCompanyId = aAppUserAppointmentDto.AppCreatedByCompanyId;
			
			if(aAppUserAppointmentDto.Id == null)
			{
				aAppUserAppointmentEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppUserAppointmentEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppUserAppointmentEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppUserAppointmentEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppUserAppointmentEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppUserAppointmentEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppUserAppointmentEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppUserAppointmentEntity, aAppUserAppointmentDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppUserAppointmentEntity aAppUserAppointmentEntity,AppUserAppointmentDto aAppUserAppointmentDto);
		
		static partial void OnCopyDtoToEntityDone(AppUserAppointmentEntity aAppUserAppointmentEntity,AppUserAppointmentDto aAppUserAppointmentDto);
		
   
       
    }
}

 