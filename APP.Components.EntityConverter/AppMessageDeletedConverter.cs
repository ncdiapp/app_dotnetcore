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
    /// Convert Properties between  AppMessageDeletedEntity and  AppMessageDeletedDto
    /// </summary>
    public static partial class AppMessageDeletedConverter 
    {
         /// <summary>
        ///  Convert AppMessageDeletedEntity To  AppMessageDeletedDto
        /// </summary>
        public static AppMessageDeletedDto ConvertEntityToDto(AppMessageDeletedEntity aAppMessageDeletedEntity)
        {        
    		AppMessageDeletedDto aAppMessageDeletedDto = new AppMessageDeletedDto();
    		CopyEntityPropertyToDto( aAppMessageDeletedEntity, aAppMessageDeletedDto);          
			return aAppMessageDeletedDto;
        }
		 /// <summary>
        ///  Convert AppMessageDeletedEntity To  AppMessageDeletedExDto
        /// </summary>
        public static AppMessageDeletedExDto ConvertEntityToExDto(AppMessageDeletedEntity aAppMessageDeletedEntity)
        {        
    		AppMessageDeletedExDto aAppMessageDeletedExDto = new AppMessageDeletedExDto();
			CopyEntityPropertyToDto( aAppMessageDeletedEntity, aAppMessageDeletedExDto);
			
			
			
            return aAppMessageDeletedExDto;
        }
		
		 /// <summary>
        ///  Convert AppMessageDeletedEntity To  AppMessageDeletedDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppMessageDeletedEntity aAppMessageDeletedEntity,AppMessageDeletedDto aAppMessageDeletedDto)
        {        
    		
           // aAppMessageDeletedDto.StopChangeTracking();
 			aAppMessageDeletedDto.Id = aAppMessageDeletedEntity.DeleteMessageId;
 			aAppMessageDeletedDto.MessageId = aAppMessageDeletedEntity.MessageId;
 			aAppMessageDeletedDto.SenderEmail = aAppMessageDeletedEntity.SenderEmail;
 			aAppMessageDeletedDto.ReceivedEmail = aAppMessageDeletedEntity.ReceivedEmail;
 			aAppMessageDeletedDto.AppCreatedById = aAppMessageDeletedEntity.AppCreatedById;
 			aAppMessageDeletedDto.AppCreatedDate = aAppMessageDeletedEntity.AppCreatedDate;
 			aAppMessageDeletedDto.AppModifiedDate = aAppMessageDeletedEntity.AppModifiedDate;
 			aAppMessageDeletedDto.AppModifiedById = aAppMessageDeletedEntity.AppModifiedById;
 			aAppMessageDeletedDto.AppCreatedByCompanyId = aAppMessageDeletedEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppMessageDeletedDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppMessageDeletedEntity.AppCreatedDate);
                aAppMessageDeletedDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppMessageDeletedEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppMessageDeletedEntity, aAppMessageDeletedDto);
		}
		
		 /// <summary>
        ///  Copy AppMessageDeletedDto Properties to   AppMessageDeletedEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppMessageDeletedEntity aAppMessageDeletedEntity,AppMessageDeletedDto aAppMessageDeletedDto)
        {        
 
      			aAppMessageDeletedEntity.MessageId = aAppMessageDeletedDto.MessageId;
      			aAppMessageDeletedEntity.SenderEmail = aAppMessageDeletedDto.SenderEmail;
      			aAppMessageDeletedEntity.ReceivedEmail = aAppMessageDeletedDto.ReceivedEmail;
 
  
   
    
      			aAppMessageDeletedEntity.AppCreatedByCompanyId = aAppMessageDeletedDto.AppCreatedByCompanyId;
			
			if(aAppMessageDeletedDto.Id == null)
			{
				aAppMessageDeletedEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppMessageDeletedEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppMessageDeletedEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppMessageDeletedEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppMessageDeletedEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppMessageDeletedEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppMessageDeletedEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppMessageDeletedEntity, aAppMessageDeletedDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppMessageDeletedEntity aAppMessageDeletedEntity,AppMessageDeletedDto aAppMessageDeletedDto);
		
		static partial void OnCopyDtoToEntityDone(AppMessageDeletedEntity aAppMessageDeletedEntity,AppMessageDeletedDto aAppMessageDeletedDto);
		
   
       
    }
}

 