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
    /// Convert Properties between  AppSecurityUserContactEntity and  AppSecurityUserContactDto
    /// </summary>
    public static partial class AppSecurityUserContactConverter 
    {
         /// <summary>
        ///  Convert AppSecurityUserContactEntity To  AppSecurityUserContactDto
        /// </summary>
        public static AppSecurityUserContactDto ConvertEntityToDto(AppSecurityUserContactEntity aAppSecurityUserContactEntity)
        {        
    		AppSecurityUserContactDto aAppSecurityUserContactDto = new AppSecurityUserContactDto();
    		CopyEntityPropertyToDto( aAppSecurityUserContactEntity, aAppSecurityUserContactDto);          
			return aAppSecurityUserContactDto;
        }
		 /// <summary>
        ///  Convert AppSecurityUserContactEntity To  AppSecurityUserContactExDto
        /// </summary>
        public static AppSecurityUserContactExDto ConvertEntityToExDto(AppSecurityUserContactEntity aAppSecurityUserContactEntity)
        {        
    		AppSecurityUserContactExDto aAppSecurityUserContactExDto = new AppSecurityUserContactExDto();
			CopyEntityPropertyToDto( aAppSecurityUserContactEntity, aAppSecurityUserContactExDto);
			
			
			
            return aAppSecurityUserContactExDto;
        }
		
		 /// <summary>
        ///  Convert AppSecurityUserContactEntity To  AppSecurityUserContactDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSecurityUserContactEntity aAppSecurityUserContactEntity,AppSecurityUserContactDto aAppSecurityUserContactDto)
        {        
    		
           // aAppSecurityUserContactDto.StopChangeTracking();
 			aAppSecurityUserContactDto.Id = aAppSecurityUserContactEntity.ContactId;
 			aAppSecurityUserContactDto.UserId = aAppSecurityUserContactEntity.UserId;
 			aAppSecurityUserContactDto.ContactType = aAppSecurityUserContactEntity.ContactType;
 			aAppSecurityUserContactDto.ContactFormat = aAppSecurityUserContactEntity.ContactFormat;
 			aAppSecurityUserContactDto.AdditionalContactInfo = aAppSecurityUserContactEntity.AdditionalContactInfo;
 			aAppSecurityUserContactDto.Comments = aAppSecurityUserContactEntity.Comments;
 			aAppSecurityUserContactDto.IsForwardMessageToThisContact = aAppSecurityUserContactEntity.IsForwardMessageToThisContact;
 			aAppSecurityUserContactDto.AppCreatedById = aAppSecurityUserContactEntity.AppCreatedById;
 			aAppSecurityUserContactDto.AppCreatedDate = aAppSecurityUserContactEntity.AppCreatedDate;
 			aAppSecurityUserContactDto.AppModifiedDate = aAppSecurityUserContactEntity.AppModifiedDate;
 			aAppSecurityUserContactDto.AppModifiedById = aAppSecurityUserContactEntity.AppModifiedById;
 			aAppSecurityUserContactDto.AppCreatedByCompanyId = aAppSecurityUserContactEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSecurityUserContactDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityUserContactEntity.AppCreatedDate);
                aAppSecurityUserContactDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityUserContactEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSecurityUserContactEntity, aAppSecurityUserContactDto);
		}
		
		 /// <summary>
        ///  Copy AppSecurityUserContactDto Properties to   AppSecurityUserContactEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSecurityUserContactEntity aAppSecurityUserContactEntity,AppSecurityUserContactDto aAppSecurityUserContactDto)
        {        
 
      			aAppSecurityUserContactEntity.UserId = aAppSecurityUserContactDto.UserId;
      			aAppSecurityUserContactEntity.ContactType = aAppSecurityUserContactDto.ContactType;
      			aAppSecurityUserContactEntity.ContactFormat = aAppSecurityUserContactDto.ContactFormat;
      			aAppSecurityUserContactEntity.AdditionalContactInfo = aAppSecurityUserContactDto.AdditionalContactInfo;
      			aAppSecurityUserContactEntity.Comments = aAppSecurityUserContactDto.Comments;
      			aAppSecurityUserContactEntity.IsForwardMessageToThisContact = aAppSecurityUserContactDto.IsForwardMessageToThisContact;
 
  
   
    
      			aAppSecurityUserContactEntity.AppCreatedByCompanyId = aAppSecurityUserContactDto.AppCreatedByCompanyId;
			
			if(aAppSecurityUserContactDto.Id == null)
			{
				aAppSecurityUserContactEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityUserContactEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSecurityUserContactEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityUserContactEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityUserContactEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSecurityUserContactEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityUserContactEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSecurityUserContactEntity, aAppSecurityUserContactDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSecurityUserContactEntity aAppSecurityUserContactEntity,AppSecurityUserContactDto aAppSecurityUserContactDto);
		
		static partial void OnCopyDtoToEntityDone(AppSecurityUserContactEntity aAppSecurityUserContactEntity,AppSecurityUserContactDto aAppSecurityUserContactDto);
		
   
       
    }
}

 