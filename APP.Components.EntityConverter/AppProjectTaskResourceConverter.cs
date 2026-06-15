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
    /// Convert Properties between  AppProjectTaskResourceEntity and  AppProjectTaskResourceDto
    /// </summary>
    public static partial class AppProjectTaskResourceConverter 
    {
         /// <summary>
        ///  Convert AppProjectTaskResourceEntity To  AppProjectTaskResourceDto
        /// </summary>
        public static AppProjectTaskResourceDto ConvertEntityToDto(AppProjectTaskResourceEntity aAppProjectTaskResourceEntity)
        {        
    		AppProjectTaskResourceDto aAppProjectTaskResourceDto = new AppProjectTaskResourceDto();
    		CopyEntityPropertyToDto( aAppProjectTaskResourceEntity, aAppProjectTaskResourceDto);          
			return aAppProjectTaskResourceDto;
        }
		 /// <summary>
        ///  Convert AppProjectTaskResourceEntity To  AppProjectTaskResourceExDto
        /// </summary>
        public static AppProjectTaskResourceExDto ConvertEntityToExDto(AppProjectTaskResourceEntity aAppProjectTaskResourceEntity)
        {        
    		AppProjectTaskResourceExDto aAppProjectTaskResourceExDto = new AppProjectTaskResourceExDto();
			CopyEntityPropertyToDto( aAppProjectTaskResourceEntity, aAppProjectTaskResourceExDto);
			
			
			
            return aAppProjectTaskResourceExDto;
        }
		
		 /// <summary>
        ///  Convert AppProjectTaskResourceEntity To  AppProjectTaskResourceDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppProjectTaskResourceEntity aAppProjectTaskResourceEntity,AppProjectTaskResourceDto aAppProjectTaskResourceDto)
        {        
    		
           // aAppProjectTaskResourceDto.StopChangeTracking();
 			aAppProjectTaskResourceDto.Id = aAppProjectTaskResourceEntity.TaskResourceId;
 			aAppProjectTaskResourceDto.ProjectWorkFlowTaskId = aAppProjectTaskResourceEntity.ProjectWorkFlowTaskId;
 			aAppProjectTaskResourceDto.UserId = aAppProjectTaskResourceEntity.UserId;
 			aAppProjectTaskResourceDto.RoleId = aAppProjectTaskResourceEntity.RoleId;
 			aAppProjectTaskResourceDto.AppCreatedById = aAppProjectTaskResourceEntity.AppCreatedById;
 			aAppProjectTaskResourceDto.AppCreatedDate = aAppProjectTaskResourceEntity.AppCreatedDate;
 			aAppProjectTaskResourceDto.AppModifiedDate = aAppProjectTaskResourceEntity.AppModifiedDate;
 			aAppProjectTaskResourceDto.AppModifiedById = aAppProjectTaskResourceEntity.AppModifiedById;
 			aAppProjectTaskResourceDto.AppCreatedByCompanyId = aAppProjectTaskResourceEntity.AppCreatedByCompanyId;
 			aAppProjectTaskResourceDto.PlannedWorkHours = aAppProjectTaskResourceEntity.PlannedWorkHours;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppProjectTaskResourceDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectTaskResourceEntity.AppCreatedDate);
                aAppProjectTaskResourceDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectTaskResourceEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppProjectTaskResourceEntity, aAppProjectTaskResourceDto);
		}
		
		 /// <summary>
        ///  Copy AppProjectTaskResourceDto Properties to   AppProjectTaskResourceEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppProjectTaskResourceEntity aAppProjectTaskResourceEntity,AppProjectTaskResourceDto aAppProjectTaskResourceDto)
        {        
 
      			aAppProjectTaskResourceEntity.ProjectWorkFlowTaskId = aAppProjectTaskResourceDto.ProjectWorkFlowTaskId;
      			aAppProjectTaskResourceEntity.UserId = aAppProjectTaskResourceDto.UserId;
      			aAppProjectTaskResourceEntity.RoleId = aAppProjectTaskResourceDto.RoleId;
 
  
   
    
      			aAppProjectTaskResourceEntity.AppCreatedByCompanyId = aAppProjectTaskResourceDto.AppCreatedByCompanyId;
      			aAppProjectTaskResourceEntity.PlannedWorkHours = aAppProjectTaskResourceDto.PlannedWorkHours;
			
			if(aAppProjectTaskResourceDto.Id == null)
			{
				aAppProjectTaskResourceEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectTaskResourceEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppProjectTaskResourceEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectTaskResourceEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectTaskResourceEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppProjectTaskResourceEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectTaskResourceEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppProjectTaskResourceEntity, aAppProjectTaskResourceDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppProjectTaskResourceEntity aAppProjectTaskResourceEntity,AppProjectTaskResourceDto aAppProjectTaskResourceDto);
		
		static partial void OnCopyDtoToEntityDone(AppProjectTaskResourceEntity aAppProjectTaskResourceEntity,AppProjectTaskResourceDto aAppProjectTaskResourceDto);
		
   
       
    }
}

 