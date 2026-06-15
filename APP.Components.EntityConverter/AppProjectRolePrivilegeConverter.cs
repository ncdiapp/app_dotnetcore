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
    /// Convert Properties between  AppProjectRolePrivilegeEntity and  AppProjectRolePrivilegeDto
    /// </summary>
    public static partial class AppProjectRolePrivilegeConverter 
    {
         /// <summary>
        ///  Convert AppProjectRolePrivilegeEntity To  AppProjectRolePrivilegeDto
        /// </summary>
        public static AppProjectRolePrivilegeDto ConvertEntityToDto(AppProjectRolePrivilegeEntity aAppProjectRolePrivilegeEntity)
        {        
    		AppProjectRolePrivilegeDto aAppProjectRolePrivilegeDto = new AppProjectRolePrivilegeDto();
    		CopyEntityPropertyToDto( aAppProjectRolePrivilegeEntity, aAppProjectRolePrivilegeDto);          
			return aAppProjectRolePrivilegeDto;
        }
		 /// <summary>
        ///  Convert AppProjectRolePrivilegeEntity To  AppProjectRolePrivilegeExDto
        /// </summary>
        public static AppProjectRolePrivilegeExDto ConvertEntityToExDto(AppProjectRolePrivilegeEntity aAppProjectRolePrivilegeEntity)
        {        
    		AppProjectRolePrivilegeExDto aAppProjectRolePrivilegeExDto = new AppProjectRolePrivilegeExDto();
			CopyEntityPropertyToDto( aAppProjectRolePrivilegeEntity, aAppProjectRolePrivilegeExDto);
			
			
			
            return aAppProjectRolePrivilegeExDto;
        }
		
		 /// <summary>
        ///  Convert AppProjectRolePrivilegeEntity To  AppProjectRolePrivilegeDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppProjectRolePrivilegeEntity aAppProjectRolePrivilegeEntity,AppProjectRolePrivilegeDto aAppProjectRolePrivilegeDto)
        {        
    		
           // aAppProjectRolePrivilegeDto.StopChangeTracking();
 			aAppProjectRolePrivilegeDto.Id = aAppProjectRolePrivilegeEntity.ProjectRoleActionId;
 			aAppProjectRolePrivilegeDto.ProjectRoleId = aAppProjectRolePrivilegeEntity.ProjectRoleId;
 			aAppProjectRolePrivilegeDto.ProjectPrivilegeId = aAppProjectRolePrivilegeEntity.ProjectPrivilegeId;
 			aAppProjectRolePrivilegeDto.Description = aAppProjectRolePrivilegeEntity.Description;
 			aAppProjectRolePrivilegeDto.AppCreatedById = aAppProjectRolePrivilegeEntity.AppCreatedById;
 			aAppProjectRolePrivilegeDto.AppCreatedDate = aAppProjectRolePrivilegeEntity.AppCreatedDate;
 			aAppProjectRolePrivilegeDto.AppModifiedDate = aAppProjectRolePrivilegeEntity.AppModifiedDate;
 			aAppProjectRolePrivilegeDto.AppModifiedById = aAppProjectRolePrivilegeEntity.AppModifiedById;
 			aAppProjectRolePrivilegeDto.AppCreatedByCompanyId = aAppProjectRolePrivilegeEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppProjectRolePrivilegeDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectRolePrivilegeEntity.AppCreatedDate);
                aAppProjectRolePrivilegeDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectRolePrivilegeEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppProjectRolePrivilegeEntity, aAppProjectRolePrivilegeDto);
		}
		
		 /// <summary>
        ///  Copy AppProjectRolePrivilegeDto Properties to   AppProjectRolePrivilegeEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppProjectRolePrivilegeEntity aAppProjectRolePrivilegeEntity,AppProjectRolePrivilegeDto aAppProjectRolePrivilegeDto)
        {        
 
      			aAppProjectRolePrivilegeEntity.ProjectRoleId = aAppProjectRolePrivilegeDto.ProjectRoleId;
      			aAppProjectRolePrivilegeEntity.ProjectPrivilegeId = aAppProjectRolePrivilegeDto.ProjectPrivilegeId;
      			aAppProjectRolePrivilegeEntity.Description = aAppProjectRolePrivilegeDto.Description;
 
  
   
    
      			aAppProjectRolePrivilegeEntity.AppCreatedByCompanyId = aAppProjectRolePrivilegeDto.AppCreatedByCompanyId;
			
			if(aAppProjectRolePrivilegeDto.Id == null)
			{
				aAppProjectRolePrivilegeEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectRolePrivilegeEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppProjectRolePrivilegeEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectRolePrivilegeEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectRolePrivilegeEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppProjectRolePrivilegeEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectRolePrivilegeEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppProjectRolePrivilegeEntity, aAppProjectRolePrivilegeDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppProjectRolePrivilegeEntity aAppProjectRolePrivilegeEntity,AppProjectRolePrivilegeDto aAppProjectRolePrivilegeDto);
		
		static partial void OnCopyDtoToEntityDone(AppProjectRolePrivilegeEntity aAppProjectRolePrivilegeEntity,AppProjectRolePrivilegeDto aAppProjectRolePrivilegeDto);
		
   
       
    }
}

 