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
    /// Convert Properties between  AppMessageUserReceivedEntity and  AppMessageUserReceivedDto
    /// </summary>
    public static partial class AppMessageUserReceivedConverter 
    {
         /// <summary>
        ///  Convert AppMessageUserReceivedEntity To  AppMessageUserReceivedDto
        /// </summary>
        public static AppMessageUserReceivedDto ConvertEntityToDto(AppMessageUserReceivedEntity aAppMessageUserReceivedEntity)
        {        
    		AppMessageUserReceivedDto aAppMessageUserReceivedDto = new AppMessageUserReceivedDto();
    		CopyEntityPropertyToDto( aAppMessageUserReceivedEntity, aAppMessageUserReceivedDto);          
			return aAppMessageUserReceivedDto;
        }
		 /// <summary>
        ///  Convert AppMessageUserReceivedEntity To  AppMessageUserReceivedExDto
        /// </summary>
        public static AppMessageUserReceivedExDto ConvertEntityToExDto(AppMessageUserReceivedEntity aAppMessageUserReceivedEntity)
        {        
    		AppMessageUserReceivedExDto aAppMessageUserReceivedExDto = new AppMessageUserReceivedExDto();
			CopyEntityPropertyToDto( aAppMessageUserReceivedEntity, aAppMessageUserReceivedExDto);
			
			
			
            return aAppMessageUserReceivedExDto;
        }
		
		 /// <summary>
        ///  Convert AppMessageUserReceivedEntity To  AppMessageUserReceivedDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppMessageUserReceivedEntity aAppMessageUserReceivedEntity,AppMessageUserReceivedDto aAppMessageUserReceivedDto)
        {        
    		
           // aAppMessageUserReceivedDto.StopChangeTracking();
 			aAppMessageUserReceivedDto.Id = aAppMessageUserReceivedEntity.MessageUserReceivedId;
 			aAppMessageUserReceivedDto.MessageId = aAppMessageUserReceivedEntity.MessageId;
 			aAppMessageUserReceivedDto.ReadDate = aAppMessageUserReceivedEntity.ReadDate;
 			aAppMessageUserReceivedDto.ReceivedEmail = aAppMessageUserReceivedEntity.ReceivedEmail;
 			aAppMessageUserReceivedDto.IsSentByEmailServer = aAppMessageUserReceivedEntity.IsSentByEmailServer;
 			aAppMessageUserReceivedDto.ReceivedById = aAppMessageUserReceivedEntity.ReceivedById;
 			aAppMessageUserReceivedDto.AppCreatedById = aAppMessageUserReceivedEntity.AppCreatedById;
 			aAppMessageUserReceivedDto.AppCreatedDate = aAppMessageUserReceivedEntity.AppCreatedDate;
 			aAppMessageUserReceivedDto.AppModifiedDate = aAppMessageUserReceivedEntity.AppModifiedDate;
 			aAppMessageUserReceivedDto.AppModifiedById = aAppMessageUserReceivedEntity.AppModifiedById;
 			aAppMessageUserReceivedDto.AppCreatedByCompanyId = aAppMessageUserReceivedEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppMessageUserReceivedDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppMessageUserReceivedEntity.AppCreatedDate);
                aAppMessageUserReceivedDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppMessageUserReceivedEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppMessageUserReceivedEntity, aAppMessageUserReceivedDto);
		}
		
		 /// <summary>
        ///  Copy AppMessageUserReceivedDto Properties to   AppMessageUserReceivedEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppMessageUserReceivedEntity aAppMessageUserReceivedEntity,AppMessageUserReceivedDto aAppMessageUserReceivedDto)
        {        
 
      			aAppMessageUserReceivedEntity.MessageId = aAppMessageUserReceivedDto.MessageId;
      			aAppMessageUserReceivedEntity.ReadDate = aAppMessageUserReceivedDto.ReadDate;
      			aAppMessageUserReceivedEntity.ReceivedEmail = aAppMessageUserReceivedDto.ReceivedEmail;
      			aAppMessageUserReceivedEntity.IsSentByEmailServer = aAppMessageUserReceivedDto.IsSentByEmailServer;
      			aAppMessageUserReceivedEntity.ReceivedById = aAppMessageUserReceivedDto.ReceivedById;
 
  
   
    
      			aAppMessageUserReceivedEntity.AppCreatedByCompanyId = aAppMessageUserReceivedDto.AppCreatedByCompanyId;
			
			if(aAppMessageUserReceivedDto.Id == null)
			{
				aAppMessageUserReceivedEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppMessageUserReceivedEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppMessageUserReceivedEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppMessageUserReceivedEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppMessageUserReceivedEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppMessageUserReceivedEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppMessageUserReceivedEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppMessageUserReceivedEntity, aAppMessageUserReceivedDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppMessageUserReceivedEntity aAppMessageUserReceivedEntity,AppMessageUserReceivedDto aAppMessageUserReceivedDto);
		
		static partial void OnCopyDtoToEntityDone(AppMessageUserReceivedEntity aAppMessageUserReceivedEntity,AppMessageUserReceivedDto aAppMessageUserReceivedDto);
		
   
       
    }
}

 