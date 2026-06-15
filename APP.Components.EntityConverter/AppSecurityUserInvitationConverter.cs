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
    /// Convert Properties between  AppSecurityUserInvitationEntity and  AppSecurityUserInvitationDto
    /// </summary>
    public static partial class AppSecurityUserInvitationConverter 
    {
         /// <summary>
        ///  Convert AppSecurityUserInvitationEntity To  AppSecurityUserInvitationDto
        /// </summary>
        public static AppSecurityUserInvitationDto ConvertEntityToDto(AppSecurityUserInvitationEntity aAppSecurityUserInvitationEntity)
        {        
    		AppSecurityUserInvitationDto aAppSecurityUserInvitationDto = new AppSecurityUserInvitationDto();
    		CopyEntityPropertyToDto( aAppSecurityUserInvitationEntity, aAppSecurityUserInvitationDto);          
			return aAppSecurityUserInvitationDto;
        }
		 /// <summary>
        ///  Convert AppSecurityUserInvitationEntity To  AppSecurityUserInvitationExDto
        /// </summary>
        public static AppSecurityUserInvitationExDto ConvertEntityToExDto(AppSecurityUserInvitationEntity aAppSecurityUserInvitationEntity)
        {        
    		AppSecurityUserInvitationExDto aAppSecurityUserInvitationExDto = new AppSecurityUserInvitationExDto();
			CopyEntityPropertyToDto( aAppSecurityUserInvitationEntity, aAppSecurityUserInvitationExDto);
			
			
			
            return aAppSecurityUserInvitationExDto;
        }
		
		 /// <summary>
        ///  Convert AppSecurityUserInvitationEntity To  AppSecurityUserInvitationDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSecurityUserInvitationEntity aAppSecurityUserInvitationEntity,AppSecurityUserInvitationDto aAppSecurityUserInvitationDto)
        {        
    		
           // aAppSecurityUserInvitationDto.StopChangeTracking();
 			aAppSecurityUserInvitationDto.Id = aAppSecurityUserInvitationEntity.InvitationId;
 			aAppSecurityUserInvitationDto.InvitingUserId = aAppSecurityUserInvitationEntity.InvitingUserId;
 			aAppSecurityUserInvitationDto.InvitingOrgId = aAppSecurityUserInvitationEntity.InvitingOrgId;
 			aAppSecurityUserInvitationDto.InvitingBusinessPartnerId = aAppSecurityUserInvitationEntity.InvitingBusinessPartnerId;
 			aAppSecurityUserInvitationDto.InvitedUserId = aAppSecurityUserInvitationEntity.InvitedUserId;
 			aAppSecurityUserInvitationDto.InvitedUserEmail = aAppSecurityUserInvitationEntity.InvitedUserEmail;
 			aAppSecurityUserInvitationDto.EmUserType = aAppSecurityUserInvitationEntity.EmUserType;
 			aAppSecurityUserInvitationDto.AppCreatedById = aAppSecurityUserInvitationEntity.AppCreatedById;
 			aAppSecurityUserInvitationDto.AppCreatedDate = aAppSecurityUserInvitationEntity.AppCreatedDate;
 			aAppSecurityUserInvitationDto.AppModifiedDate = aAppSecurityUserInvitationEntity.AppModifiedDate;
 			aAppSecurityUserInvitationDto.AppModifiedById = aAppSecurityUserInvitationEntity.AppModifiedById;
 			aAppSecurityUserInvitationDto.AppCreatedByCompanyId = aAppSecurityUserInvitationEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSecurityUserInvitationDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityUserInvitationEntity.AppCreatedDate);
                aAppSecurityUserInvitationDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityUserInvitationEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSecurityUserInvitationEntity, aAppSecurityUserInvitationDto);
		}
		
		 /// <summary>
        ///  Copy AppSecurityUserInvitationDto Properties to   AppSecurityUserInvitationEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSecurityUserInvitationEntity aAppSecurityUserInvitationEntity,AppSecurityUserInvitationDto aAppSecurityUserInvitationDto)
        {        
 
      			aAppSecurityUserInvitationEntity.InvitingUserId = aAppSecurityUserInvitationDto.InvitingUserId;
      			aAppSecurityUserInvitationEntity.InvitingOrgId = aAppSecurityUserInvitationDto.InvitingOrgId;
      			aAppSecurityUserInvitationEntity.InvitingBusinessPartnerId = aAppSecurityUserInvitationDto.InvitingBusinessPartnerId;
      			aAppSecurityUserInvitationEntity.InvitedUserId = aAppSecurityUserInvitationDto.InvitedUserId;
      			aAppSecurityUserInvitationEntity.InvitedUserEmail = aAppSecurityUserInvitationDto.InvitedUserEmail;
      			aAppSecurityUserInvitationEntity.EmUserType = aAppSecurityUserInvitationDto.EmUserType;
 
  
   
    
      			aAppSecurityUserInvitationEntity.AppCreatedByCompanyId = aAppSecurityUserInvitationDto.AppCreatedByCompanyId;
			
			if(aAppSecurityUserInvitationDto.Id == null)
			{
				aAppSecurityUserInvitationEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityUserInvitationEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSecurityUserInvitationEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityUserInvitationEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityUserInvitationEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSecurityUserInvitationEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityUserInvitationEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSecurityUserInvitationEntity, aAppSecurityUserInvitationDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSecurityUserInvitationEntity aAppSecurityUserInvitationEntity,AppSecurityUserInvitationDto aAppSecurityUserInvitationDto);
		
		static partial void OnCopyDtoToEntityDone(AppSecurityUserInvitationEntity aAppSecurityUserInvitationEntity,AppSecurityUserInvitationDto aAppSecurityUserInvitationDto);
		
   
       
    }
}

 