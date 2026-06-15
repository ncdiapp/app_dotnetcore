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
    /// Convert Properties between  AppSecurityUserRolePrevilegeEntity and  AppSecurityUserRolePrevilegeDto
    /// </summary>
    public static partial class AppSecurityUserRolePrevilegeConverter 
    {
         /// <summary>
        ///  Convert AppSecurityUserRolePrevilegeEntity To  AppSecurityUserRolePrevilegeDto
        /// </summary>
        public static AppSecurityUserRolePrevilegeDto ConvertEntityToDto(AppSecurityUserRolePrevilegeEntity aAppSecurityUserRolePrevilegeEntity)
        {        
    		AppSecurityUserRolePrevilegeDto aAppSecurityUserRolePrevilegeDto = new AppSecurityUserRolePrevilegeDto();
    		CopyEntityPropertyToDto( aAppSecurityUserRolePrevilegeEntity, aAppSecurityUserRolePrevilegeDto);          
			return aAppSecurityUserRolePrevilegeDto;
        }
		 /// <summary>
        ///  Convert AppSecurityUserRolePrevilegeEntity To  AppSecurityUserRolePrevilegeExDto
        /// </summary>
        public static AppSecurityUserRolePrevilegeExDto ConvertEntityToExDto(AppSecurityUserRolePrevilegeEntity aAppSecurityUserRolePrevilegeEntity)
        {        
    		AppSecurityUserRolePrevilegeExDto aAppSecurityUserRolePrevilegeExDto = new AppSecurityUserRolePrevilegeExDto();
			CopyEntityPropertyToDto( aAppSecurityUserRolePrevilegeEntity, aAppSecurityUserRolePrevilegeExDto);
			
			
			
            return aAppSecurityUserRolePrevilegeExDto;
        }
		
		 /// <summary>
        ///  Convert AppSecurityUserRolePrevilegeEntity To  AppSecurityUserRolePrevilegeDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSecurityUserRolePrevilegeEntity aAppSecurityUserRolePrevilegeEntity,AppSecurityUserRolePrevilegeDto aAppSecurityUserRolePrevilegeDto)
        {        
    		
           // aAppSecurityUserRolePrevilegeDto.StopChangeTracking();
 			aAppSecurityUserRolePrevilegeDto.Id = aAppSecurityUserRolePrevilegeEntity.UserPrevilegeId;
 			aAppSecurityUserRolePrevilegeDto.UserId = aAppSecurityUserRolePrevilegeEntity.UserId;
 			aAppSecurityUserRolePrevilegeDto.RoleId = aAppSecurityUserRolePrevilegeEntity.RoleId;
 			aAppSecurityUserRolePrevilegeDto.EntityActionId = aAppSecurityUserRolePrevilegeEntity.EntityActionId;
 			aAppSecurityUserRolePrevilegeDto.AppCreatedById = aAppSecurityUserRolePrevilegeEntity.AppCreatedById;
 			aAppSecurityUserRolePrevilegeDto.AppCreatedDate = aAppSecurityUserRolePrevilegeEntity.AppCreatedDate;
 			aAppSecurityUserRolePrevilegeDto.AppModifiedDate = aAppSecurityUserRolePrevilegeEntity.AppModifiedDate;
 			aAppSecurityUserRolePrevilegeDto.AppModifiedById = aAppSecurityUserRolePrevilegeEntity.AppModifiedById;
 			aAppSecurityUserRolePrevilegeDto.AppCreatedByCompanyId = aAppSecurityUserRolePrevilegeEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSecurityUserRolePrevilegeDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityUserRolePrevilegeEntity.AppCreatedDate);
                aAppSecurityUserRolePrevilegeDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityUserRolePrevilegeEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSecurityUserRolePrevilegeEntity, aAppSecurityUserRolePrevilegeDto);
		}
		
		 /// <summary>
        ///  Copy AppSecurityUserRolePrevilegeDto Properties to   AppSecurityUserRolePrevilegeEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSecurityUserRolePrevilegeEntity aAppSecurityUserRolePrevilegeEntity,AppSecurityUserRolePrevilegeDto aAppSecurityUserRolePrevilegeDto)
        {        
 
      			aAppSecurityUserRolePrevilegeEntity.UserId = aAppSecurityUserRolePrevilegeDto.UserId;
      			aAppSecurityUserRolePrevilegeEntity.RoleId = aAppSecurityUserRolePrevilegeDto.RoleId;
      			aAppSecurityUserRolePrevilegeEntity.EntityActionId = aAppSecurityUserRolePrevilegeDto.EntityActionId;
 
  
   
    
      			aAppSecurityUserRolePrevilegeEntity.AppCreatedByCompanyId = aAppSecurityUserRolePrevilegeDto.AppCreatedByCompanyId;
			
			if(aAppSecurityUserRolePrevilegeDto.Id == null)
			{
				aAppSecurityUserRolePrevilegeEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityUserRolePrevilegeEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSecurityUserRolePrevilegeEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityUserRolePrevilegeEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityUserRolePrevilegeEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSecurityUserRolePrevilegeEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityUserRolePrevilegeEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSecurityUserRolePrevilegeEntity, aAppSecurityUserRolePrevilegeDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSecurityUserRolePrevilegeEntity aAppSecurityUserRolePrevilegeEntity,AppSecurityUserRolePrevilegeDto aAppSecurityUserRolePrevilegeDto);
		
		static partial void OnCopyDtoToEntityDone(AppSecurityUserRolePrevilegeEntity aAppSecurityUserRolePrevilegeEntity,AppSecurityUserRolePrevilegeDto aAppSecurityUserRolePrevilegeDto);
		
   
       
    }
}

 