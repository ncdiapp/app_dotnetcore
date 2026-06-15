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
    /// Convert Properties between  AppBusinessPartnerInviteUserEntity and  AppBusinessPartnerInviteUserDto
    /// </summary>
    public static partial class AppBusinessPartnerInviteUserConverter 
    {
         /// <summary>
        ///  Convert AppBusinessPartnerInviteUserEntity To  AppBusinessPartnerInviteUserDto
        /// </summary>
        public static AppBusinessPartnerInviteUserDto ConvertEntityToDto(AppBusinessPartnerInviteUserEntity aAppBusinessPartnerInviteUserEntity)
        {        
    		AppBusinessPartnerInviteUserDto aAppBusinessPartnerInviteUserDto = new AppBusinessPartnerInviteUserDto();
    		CopyEntityPropertyToDto( aAppBusinessPartnerInviteUserEntity, aAppBusinessPartnerInviteUserDto);          
			return aAppBusinessPartnerInviteUserDto;
        }
		 /// <summary>
        ///  Convert AppBusinessPartnerInviteUserEntity To  AppBusinessPartnerInviteUserExDto
        /// </summary>
        public static AppBusinessPartnerInviteUserExDto ConvertEntityToExDto(AppBusinessPartnerInviteUserEntity aAppBusinessPartnerInviteUserEntity)
        {        
    		AppBusinessPartnerInviteUserExDto aAppBusinessPartnerInviteUserExDto = new AppBusinessPartnerInviteUserExDto();
			CopyEntityPropertyToDto( aAppBusinessPartnerInviteUserEntity, aAppBusinessPartnerInviteUserExDto);
			
			
			
            return aAppBusinessPartnerInviteUserExDto;
        }
		
		 /// <summary>
        ///  Convert AppBusinessPartnerInviteUserEntity To  AppBusinessPartnerInviteUserDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppBusinessPartnerInviteUserEntity aAppBusinessPartnerInviteUserEntity,AppBusinessPartnerInviteUserDto aAppBusinessPartnerInviteUserDto)
        {        
    		
           // aAppBusinessPartnerInviteUserDto.StopChangeTracking();
 			aAppBusinessPartnerInviteUserDto.Id = aAppBusinessPartnerInviteUserEntity.ParternerInvitedUserId;
 			aAppBusinessPartnerInviteUserDto.AppBusinessPartnerId = aAppBusinessPartnerInviteUserEntity.AppBusinessPartnerId;
 			aAppBusinessPartnerInviteUserDto.UserId = aAppBusinessPartnerInviteUserEntity.UserId;
 			aAppBusinessPartnerInviteUserDto.AppCreatedById = aAppBusinessPartnerInviteUserEntity.AppCreatedById;
 			aAppBusinessPartnerInviteUserDto.AppCreatedDate = aAppBusinessPartnerInviteUserEntity.AppCreatedDate;
 			aAppBusinessPartnerInviteUserDto.AppModifiedDate = aAppBusinessPartnerInviteUserEntity.AppModifiedDate;
 			aAppBusinessPartnerInviteUserDto.AppModifiedById = aAppBusinessPartnerInviteUserEntity.AppModifiedById;
 			aAppBusinessPartnerInviteUserDto.AppCompanyId = aAppBusinessPartnerInviteUserEntity.AppCompanyId;
 			aAppBusinessPartnerInviteUserDto.AppCreatedByCompanyId = aAppBusinessPartnerInviteUserEntity.AppCreatedByCompanyId;
 			aAppBusinessPartnerInviteUserDto.EmInvitedUserType = aAppBusinessPartnerInviteUserEntity.EmInvitedUserType;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppBusinessPartnerInviteUserDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppBusinessPartnerInviteUserEntity.AppCreatedDate);
                aAppBusinessPartnerInviteUserDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppBusinessPartnerInviteUserEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppBusinessPartnerInviteUserEntity, aAppBusinessPartnerInviteUserDto);
		}
		
		 /// <summary>
        ///  Copy AppBusinessPartnerInviteUserDto Properties to   AppBusinessPartnerInviteUserEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppBusinessPartnerInviteUserEntity aAppBusinessPartnerInviteUserEntity,AppBusinessPartnerInviteUserDto aAppBusinessPartnerInviteUserDto)
        {        
 
      			aAppBusinessPartnerInviteUserEntity.AppBusinessPartnerId = aAppBusinessPartnerInviteUserDto.AppBusinessPartnerId;
      			aAppBusinessPartnerInviteUserEntity.UserId = aAppBusinessPartnerInviteUserDto.UserId;
 
  
   
    
      			aAppBusinessPartnerInviteUserEntity.AppCompanyId = aAppBusinessPartnerInviteUserDto.AppCompanyId;
      			aAppBusinessPartnerInviteUserEntity.AppCreatedByCompanyId = aAppBusinessPartnerInviteUserDto.AppCreatedByCompanyId;
      			aAppBusinessPartnerInviteUserEntity.EmInvitedUserType = aAppBusinessPartnerInviteUserDto.EmInvitedUserType;
			
			if(aAppBusinessPartnerInviteUserDto.Id == null)
			{
				aAppBusinessPartnerInviteUserEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppBusinessPartnerInviteUserEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppBusinessPartnerInviteUserEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppBusinessPartnerInviteUserEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppBusinessPartnerInviteUserEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppBusinessPartnerInviteUserEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppBusinessPartnerInviteUserEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppBusinessPartnerInviteUserEntity, aAppBusinessPartnerInviteUserDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppBusinessPartnerInviteUserEntity aAppBusinessPartnerInviteUserEntity,AppBusinessPartnerInviteUserDto aAppBusinessPartnerInviteUserDto);
		
		static partial void OnCopyDtoToEntityDone(AppBusinessPartnerInviteUserEntity aAppBusinessPartnerInviteUserEntity,AppBusinessPartnerInviteUserDto aAppBusinessPartnerInviteUserDto);
		
   
       
    }
}

 