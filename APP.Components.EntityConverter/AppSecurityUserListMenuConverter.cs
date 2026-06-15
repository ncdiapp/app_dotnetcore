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
    /// Convert Properties between  AppSecurityUserListMenuEntity and  AppSecurityUserListMenuDto
    /// </summary>
    public static partial class AppSecurityUserListMenuConverter 
    {
         /// <summary>
        ///  Convert AppSecurityUserListMenuEntity To  AppSecurityUserListMenuDto
        /// </summary>
        public static AppSecurityUserListMenuDto ConvertEntityToDto(AppSecurityUserListMenuEntity aAppSecurityUserListMenuEntity)
        {        
    		AppSecurityUserListMenuDto aAppSecurityUserListMenuDto = new AppSecurityUserListMenuDto();
    		CopyEntityPropertyToDto( aAppSecurityUserListMenuEntity, aAppSecurityUserListMenuDto);          
			return aAppSecurityUserListMenuDto;
        }
		 /// <summary>
        ///  Convert AppSecurityUserListMenuEntity To  AppSecurityUserListMenuExDto
        /// </summary>
        public static AppSecurityUserListMenuExDto ConvertEntityToExDto(AppSecurityUserListMenuEntity aAppSecurityUserListMenuEntity)
        {        
    		AppSecurityUserListMenuExDto aAppSecurityUserListMenuExDto = new AppSecurityUserListMenuExDto();
			CopyEntityPropertyToDto( aAppSecurityUserListMenuEntity, aAppSecurityUserListMenuExDto);
			
			
			
            return aAppSecurityUserListMenuExDto;
        }
		
		 /// <summary>
        ///  Convert AppSecurityUserListMenuEntity To  AppSecurityUserListMenuDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSecurityUserListMenuEntity aAppSecurityUserListMenuEntity,AppSecurityUserListMenuDto aAppSecurityUserListMenuDto)
        {        
    		
           // aAppSecurityUserListMenuDto.StopChangeTracking();
 			aAppSecurityUserListMenuDto.Id = aAppSecurityUserListMenuEntity.UserMenuId;
 			aAppSecurityUserListMenuDto.UserId = aAppSecurityUserListMenuEntity.UserId;
 			aAppSecurityUserListMenuDto.GroupId = aAppSecurityUserListMenuEntity.GroupId;
 			aAppSecurityUserListMenuDto.MenuId = aAppSecurityUserListMenuEntity.MenuId;
 			aAppSecurityUserListMenuDto.AppCreatedById = aAppSecurityUserListMenuEntity.AppCreatedById;
 			aAppSecurityUserListMenuDto.AppCreatedDate = aAppSecurityUserListMenuEntity.AppCreatedDate;
 			aAppSecurityUserListMenuDto.AppModifiedDate = aAppSecurityUserListMenuEntity.AppModifiedDate;
 			aAppSecurityUserListMenuDto.AppModifiedById = aAppSecurityUserListMenuEntity.AppModifiedById;
 			aAppSecurityUserListMenuDto.AppCreatedByCompanyId = aAppSecurityUserListMenuEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSecurityUserListMenuDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityUserListMenuEntity.AppCreatedDate);
                aAppSecurityUserListMenuDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityUserListMenuEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSecurityUserListMenuEntity, aAppSecurityUserListMenuDto);
		}
		
		 /// <summary>
        ///  Copy AppSecurityUserListMenuDto Properties to   AppSecurityUserListMenuEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSecurityUserListMenuEntity aAppSecurityUserListMenuEntity,AppSecurityUserListMenuDto aAppSecurityUserListMenuDto)
        {        
 
      			aAppSecurityUserListMenuEntity.UserId = aAppSecurityUserListMenuDto.UserId;
      			aAppSecurityUserListMenuEntity.GroupId = aAppSecurityUserListMenuDto.GroupId;
      			aAppSecurityUserListMenuEntity.MenuId = aAppSecurityUserListMenuDto.MenuId;
 
  
   
    
      			aAppSecurityUserListMenuEntity.AppCreatedByCompanyId = aAppSecurityUserListMenuDto.AppCreatedByCompanyId;
			
			if(aAppSecurityUserListMenuDto.Id == null)
			{
				aAppSecurityUserListMenuEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityUserListMenuEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSecurityUserListMenuEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityUserListMenuEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityUserListMenuEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSecurityUserListMenuEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityUserListMenuEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSecurityUserListMenuEntity, aAppSecurityUserListMenuDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSecurityUserListMenuEntity aAppSecurityUserListMenuEntity,AppSecurityUserListMenuDto aAppSecurityUserListMenuDto);
		
		static partial void OnCopyDtoToEntityDone(AppSecurityUserListMenuEntity aAppSecurityUserListMenuEntity,AppSecurityUserListMenuDto aAppSecurityUserListMenuDto);
		
   
       
    }
}

 