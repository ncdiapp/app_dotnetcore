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
    /// Convert Properties between  AppProjectTeamMemberRoleEntity and  AppProjectTeamMemberRoleDto
    /// </summary>
    public static partial class AppProjectTeamMemberRoleConverter 
    {
         /// <summary>
        ///  Convert AppProjectTeamMemberRoleEntity To  AppProjectTeamMemberRoleDto
        /// </summary>
        public static AppProjectTeamMemberRoleDto ConvertEntityToDto(AppProjectTeamMemberRoleEntity aAppProjectTeamMemberRoleEntity)
        {        
    		AppProjectTeamMemberRoleDto aAppProjectTeamMemberRoleDto = new AppProjectTeamMemberRoleDto();
    		CopyEntityPropertyToDto( aAppProjectTeamMemberRoleEntity, aAppProjectTeamMemberRoleDto);          
			return aAppProjectTeamMemberRoleDto;
        }
		 /// <summary>
        ///  Convert AppProjectTeamMemberRoleEntity To  AppProjectTeamMemberRoleExDto
        /// </summary>
        public static AppProjectTeamMemberRoleExDto ConvertEntityToExDto(AppProjectTeamMemberRoleEntity aAppProjectTeamMemberRoleEntity)
        {        
    		AppProjectTeamMemberRoleExDto aAppProjectTeamMemberRoleExDto = new AppProjectTeamMemberRoleExDto();
			CopyEntityPropertyToDto( aAppProjectTeamMemberRoleEntity, aAppProjectTeamMemberRoleExDto);
			
			
			
            return aAppProjectTeamMemberRoleExDto;
        }
		
		 /// <summary>
        ///  Convert AppProjectTeamMemberRoleEntity To  AppProjectTeamMemberRoleDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppProjectTeamMemberRoleEntity aAppProjectTeamMemberRoleEntity,AppProjectTeamMemberRoleDto aAppProjectTeamMemberRoleDto)
        {        
    		
           // aAppProjectTeamMemberRoleDto.StopChangeTracking();
 			aAppProjectTeamMemberRoleDto.Id = aAppProjectTeamMemberRoleEntity.TeamMemberRoleId;
 			aAppProjectTeamMemberRoleDto.TeamMemberId = aAppProjectTeamMemberRoleEntity.TeamMemberId;
 			aAppProjectTeamMemberRoleDto.ProjectRoleId = aAppProjectTeamMemberRoleEntity.ProjectRoleId;
 			aAppProjectTeamMemberRoleDto.RoleRate = aAppProjectTeamMemberRoleEntity.RoleRate;
 			aAppProjectTeamMemberRoleDto.AppCreatedById = aAppProjectTeamMemberRoleEntity.AppCreatedById;
 			aAppProjectTeamMemberRoleDto.AppCreatedDate = aAppProjectTeamMemberRoleEntity.AppCreatedDate;
 			aAppProjectTeamMemberRoleDto.AppModifiedDate = aAppProjectTeamMemberRoleEntity.AppModifiedDate;
 			aAppProjectTeamMemberRoleDto.AppModifiedById = aAppProjectTeamMemberRoleEntity.AppModifiedById;
 			aAppProjectTeamMemberRoleDto.AppCreatedByCompanyId = aAppProjectTeamMemberRoleEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppProjectTeamMemberRoleDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectTeamMemberRoleEntity.AppCreatedDate);
                aAppProjectTeamMemberRoleDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectTeamMemberRoleEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppProjectTeamMemberRoleEntity, aAppProjectTeamMemberRoleDto);
		}
		
		 /// <summary>
        ///  Copy AppProjectTeamMemberRoleDto Properties to   AppProjectTeamMemberRoleEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppProjectTeamMemberRoleEntity aAppProjectTeamMemberRoleEntity,AppProjectTeamMemberRoleDto aAppProjectTeamMemberRoleDto)
        {        
 
      			aAppProjectTeamMemberRoleEntity.TeamMemberId = aAppProjectTeamMemberRoleDto.TeamMemberId;
      			aAppProjectTeamMemberRoleEntity.ProjectRoleId = aAppProjectTeamMemberRoleDto.ProjectRoleId;
      			aAppProjectTeamMemberRoleEntity.RoleRate = aAppProjectTeamMemberRoleDto.RoleRate;
 
  
   
    
      			aAppProjectTeamMemberRoleEntity.AppCreatedByCompanyId = aAppProjectTeamMemberRoleDto.AppCreatedByCompanyId;
			
			if(aAppProjectTeamMemberRoleDto.Id == null)
			{
				aAppProjectTeamMemberRoleEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectTeamMemberRoleEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppProjectTeamMemberRoleEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectTeamMemberRoleEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectTeamMemberRoleEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppProjectTeamMemberRoleEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectTeamMemberRoleEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppProjectTeamMemberRoleEntity, aAppProjectTeamMemberRoleDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppProjectTeamMemberRoleEntity aAppProjectTeamMemberRoleEntity,AppProjectTeamMemberRoleDto aAppProjectTeamMemberRoleDto);
		
		static partial void OnCopyDtoToEntityDone(AppProjectTeamMemberRoleEntity aAppProjectTeamMemberRoleEntity,AppProjectTeamMemberRoleDto aAppProjectTeamMemberRoleDto);
		
   
       
    }
}

 