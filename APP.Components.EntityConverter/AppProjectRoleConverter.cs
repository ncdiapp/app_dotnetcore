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
    /// Convert Properties between  AppProjectRoleEntity and  AppProjectRoleDto
    /// </summary>
    public static partial class AppProjectRoleConverter 
    {
         /// <summary>
        ///  Convert AppProjectRoleEntity To  AppProjectRoleDto
        /// </summary>
        public static AppProjectRoleDto ConvertEntityToDto(AppProjectRoleEntity aAppProjectRoleEntity)
        {        
    		AppProjectRoleDto aAppProjectRoleDto = new AppProjectRoleDto();
    		CopyEntityPropertyToDto( aAppProjectRoleEntity, aAppProjectRoleDto);          
			return aAppProjectRoleDto;
        }
		 /// <summary>
        ///  Convert AppProjectRoleEntity To  AppProjectRoleExDto
        /// </summary>
        public static AppProjectRoleExDto ConvertEntityToExDto(AppProjectRoleEntity aAppProjectRoleEntity)
        {        
    		AppProjectRoleExDto aAppProjectRoleExDto = new AppProjectRoleExDto();
			CopyEntityPropertyToDto( aAppProjectRoleEntity, aAppProjectRoleExDto);
			
			
			
            return aAppProjectRoleExDto;
        }
		
		 /// <summary>
        ///  Convert AppProjectRoleEntity To  AppProjectRoleDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppProjectRoleEntity aAppProjectRoleEntity,AppProjectRoleDto aAppProjectRoleDto)
        {        
    		
           // aAppProjectRoleDto.StopChangeTracking();
 			aAppProjectRoleDto.Id = aAppProjectRoleEntity.ProjectRoleId;
 			aAppProjectRoleDto.RoleName = aAppProjectRoleEntity.RoleName;
 			aAppProjectRoleDto.Description = aAppProjectRoleEntity.Description;
 			aAppProjectRoleDto.RoleRate = aAppProjectRoleEntity.RoleRate;
 			aAppProjectRoleDto.CurrencyId = aAppProjectRoleEntity.CurrencyId;
 			aAppProjectRoleDto.AppCreatedById = aAppProjectRoleEntity.AppCreatedById;
 			aAppProjectRoleDto.AppCreatedDate = aAppProjectRoleEntity.AppCreatedDate;
 			aAppProjectRoleDto.AppModifiedDate = aAppProjectRoleEntity.AppModifiedDate;
 			aAppProjectRoleDto.AppModifiedById = aAppProjectRoleEntity.AppModifiedById;
 			aAppProjectRoleDto.AppCreatedByCompanyId = aAppProjectRoleEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppProjectRoleDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectRoleEntity.AppCreatedDate);
                aAppProjectRoleDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectRoleEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppProjectRoleEntity, aAppProjectRoleDto);
		}
		
		 /// <summary>
        ///  Copy AppProjectRoleDto Properties to   AppProjectRoleEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppProjectRoleEntity aAppProjectRoleEntity,AppProjectRoleDto aAppProjectRoleDto)
        {        
 
      			aAppProjectRoleEntity.RoleName = aAppProjectRoleDto.RoleName;
      			aAppProjectRoleEntity.Description = aAppProjectRoleDto.Description;
      			aAppProjectRoleEntity.RoleRate = aAppProjectRoleDto.RoleRate;
      			aAppProjectRoleEntity.CurrencyId = aAppProjectRoleDto.CurrencyId;
 
  
   
    
      			aAppProjectRoleEntity.AppCreatedByCompanyId = aAppProjectRoleDto.AppCreatedByCompanyId;
			
			if(aAppProjectRoleDto.Id == null)
			{
				aAppProjectRoleEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectRoleEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppProjectRoleEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectRoleEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectRoleEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppProjectRoleEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectRoleEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppProjectRoleEntity, aAppProjectRoleDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppProjectRoleEntity aAppProjectRoleEntity,AppProjectRoleDto aAppProjectRoleDto);
		
		static partial void OnCopyDtoToEntityDone(AppProjectRoleEntity aAppProjectRoleEntity,AppProjectRoleDto aAppProjectRoleDto);
		
   
       
    }
}

 