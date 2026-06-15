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
    /// Convert Properties between  AppmessageNotificationSettingEntity and  AppmessageNotificationSettingDto
    /// </summary>
    public static partial class AppmessageNotificationSettingConverter 
    {
         /// <summary>
        ///  Convert AppmessageNotificationSettingEntity To  AppmessageNotificationSettingDto
        /// </summary>
        public static AppmessageNotificationSettingDto ConvertEntityToDto(AppmessageNotificationSettingEntity aAppmessageNotificationSettingEntity)
        {        
    		AppmessageNotificationSettingDto aAppmessageNotificationSettingDto = new AppmessageNotificationSettingDto();
    		CopyEntityPropertyToDto( aAppmessageNotificationSettingEntity, aAppmessageNotificationSettingDto);          
			return aAppmessageNotificationSettingDto;
        }
		 /// <summary>
        ///  Convert AppmessageNotificationSettingEntity To  AppmessageNotificationSettingExDto
        /// </summary>
        public static AppmessageNotificationSettingExDto ConvertEntityToExDto(AppmessageNotificationSettingEntity aAppmessageNotificationSettingEntity)
        {        
    		AppmessageNotificationSettingExDto aAppmessageNotificationSettingExDto = new AppmessageNotificationSettingExDto();
			CopyEntityPropertyToDto( aAppmessageNotificationSettingEntity, aAppmessageNotificationSettingExDto);
			
			
			
            return aAppmessageNotificationSettingExDto;
        }
		
		 /// <summary>
        ///  Convert AppmessageNotificationSettingEntity To  AppmessageNotificationSettingDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppmessageNotificationSettingEntity aAppmessageNotificationSettingEntity,AppmessageNotificationSettingDto aAppmessageNotificationSettingDto)
        {        
    		
           // aAppmessageNotificationSettingDto.StopChangeTracking();
 			aAppmessageNotificationSettingDto.Id = aAppmessageNotificationSettingEntity.NotificationSettingId;
 			aAppmessageNotificationSettingDto.TranscationId = aAppmessageNotificationSettingEntity.TranscationId;
 			aAppmessageNotificationSettingDto.ProejctId = aAppmessageNotificationSettingEntity.ProejctId;
 			aAppmessageNotificationSettingDto.SettingName = aAppmessageNotificationSettingEntity.SettingName;
 			aAppmessageNotificationSettingDto.NotificationQuery = aAppmessageNotificationSettingEntity.NotificationQuery;
 			aAppmessageNotificationSettingDto.MessageUsageType = aAppmessageNotificationSettingEntity.MessageUsageType;
 			aAppmessageNotificationSettingDto.MessageTemplateId = aAppmessageNotificationSettingEntity.MessageTemplateId;
 			aAppmessageNotificationSettingDto.MessageTemplate = aAppmessageNotificationSettingEntity.MessageTemplate;
 			aAppmessageNotificationSettingDto.EmScanPeriod = aAppmessageNotificationSettingEntity.EmScanPeriod;
 			aAppmessageNotificationSettingDto.NotificationQueryContentSettingId = aAppmessageNotificationSettingEntity.NotificationQueryContentSettingId;
 			aAppmessageNotificationSettingDto.AlertSpanTime = aAppmessageNotificationSettingEntity.AlertSpanTime;
 			aAppmessageNotificationSettingDto.AppCreatedById = aAppmessageNotificationSettingEntity.AppCreatedById;
 			aAppmessageNotificationSettingDto.AppCreatedDate = aAppmessageNotificationSettingEntity.AppCreatedDate;
 			aAppmessageNotificationSettingDto.AppModifiedDate = aAppmessageNotificationSettingEntity.AppModifiedDate;
 			aAppmessageNotificationSettingDto.AppModifiedById = aAppmessageNotificationSettingEntity.AppModifiedById;
 			aAppmessageNotificationSettingDto.AppCreatedByCompanyId = aAppmessageNotificationSettingEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppmessageNotificationSettingDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppmessageNotificationSettingEntity.AppCreatedDate);
                aAppmessageNotificationSettingDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppmessageNotificationSettingEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppmessageNotificationSettingEntity, aAppmessageNotificationSettingDto);
		}
		
		 /// <summary>
        ///  Copy AppmessageNotificationSettingDto Properties to   AppmessageNotificationSettingEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppmessageNotificationSettingEntity aAppmessageNotificationSettingEntity,AppmessageNotificationSettingDto aAppmessageNotificationSettingDto)
        {        
 
      			aAppmessageNotificationSettingEntity.TranscationId = aAppmessageNotificationSettingDto.TranscationId;
      			aAppmessageNotificationSettingEntity.ProejctId = aAppmessageNotificationSettingDto.ProejctId;
      			aAppmessageNotificationSettingEntity.SettingName = aAppmessageNotificationSettingDto.SettingName;
      			aAppmessageNotificationSettingEntity.NotificationQuery = aAppmessageNotificationSettingDto.NotificationQuery;
      			aAppmessageNotificationSettingEntity.MessageUsageType = aAppmessageNotificationSettingDto.MessageUsageType;
      			aAppmessageNotificationSettingEntity.MessageTemplateId = aAppmessageNotificationSettingDto.MessageTemplateId;
      			aAppmessageNotificationSettingEntity.MessageTemplate = aAppmessageNotificationSettingDto.MessageTemplate;
      			aAppmessageNotificationSettingEntity.EmScanPeriod = aAppmessageNotificationSettingDto.EmScanPeriod;
      			aAppmessageNotificationSettingEntity.NotificationQueryContentSettingId = aAppmessageNotificationSettingDto.NotificationQueryContentSettingId;
      			aAppmessageNotificationSettingEntity.AlertSpanTime = aAppmessageNotificationSettingDto.AlertSpanTime;
 
  
   
    
      			aAppmessageNotificationSettingEntity.AppCreatedByCompanyId = aAppmessageNotificationSettingDto.AppCreatedByCompanyId;
			
			if(aAppmessageNotificationSettingDto.Id == null)
			{
				aAppmessageNotificationSettingEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppmessageNotificationSettingEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppmessageNotificationSettingEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppmessageNotificationSettingEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppmessageNotificationSettingEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppmessageNotificationSettingEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppmessageNotificationSettingEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppmessageNotificationSettingEntity, aAppmessageNotificationSettingDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppmessageNotificationSettingEntity aAppmessageNotificationSettingEntity,AppmessageNotificationSettingDto aAppmessageNotificationSettingDto);
		
		static partial void OnCopyDtoToEntityDone(AppmessageNotificationSettingEntity aAppmessageNotificationSettingEntity,AppmessageNotificationSettingDto aAppmessageNotificationSettingDto);
		
   
       
    }
}

 