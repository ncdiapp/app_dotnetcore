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
    /// Convert Properties between  AppProjectPerspectiveTaskEntity and  AppProjectPerspectiveTaskDto
    /// </summary>
    public static partial class AppProjectPerspectiveTaskConverter 
    {
         /// <summary>
        ///  Convert AppProjectPerspectiveTaskEntity To  AppProjectPerspectiveTaskDto
        /// </summary>
        public static AppProjectPerspectiveTaskDto ConvertEntityToDto(AppProjectPerspectiveTaskEntity aAppProjectPerspectiveTaskEntity)
        {        
    		AppProjectPerspectiveTaskDto aAppProjectPerspectiveTaskDto = new AppProjectPerspectiveTaskDto();
    		CopyEntityPropertyToDto( aAppProjectPerspectiveTaskEntity, aAppProjectPerspectiveTaskDto);          
			return aAppProjectPerspectiveTaskDto;
        }
		 /// <summary>
        ///  Convert AppProjectPerspectiveTaskEntity To  AppProjectPerspectiveTaskExDto
        /// </summary>
        public static AppProjectPerspectiveTaskExDto ConvertEntityToExDto(AppProjectPerspectiveTaskEntity aAppProjectPerspectiveTaskEntity)
        {        
    		AppProjectPerspectiveTaskExDto aAppProjectPerspectiveTaskExDto = new AppProjectPerspectiveTaskExDto();
			CopyEntityPropertyToDto( aAppProjectPerspectiveTaskEntity, aAppProjectPerspectiveTaskExDto);
			
			
			
            return aAppProjectPerspectiveTaskExDto;
        }
		
		 /// <summary>
        ///  Convert AppProjectPerspectiveTaskEntity To  AppProjectPerspectiveTaskDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppProjectPerspectiveTaskEntity aAppProjectPerspectiveTaskEntity,AppProjectPerspectiveTaskDto aAppProjectPerspectiveTaskDto)
        {        
    		
           // aAppProjectPerspectiveTaskDto.StopChangeTracking();
 			aAppProjectPerspectiveTaskDto.Id = aAppProjectPerspectiveTaskEntity.PerspectiveTaskId;
 			aAppProjectPerspectiveTaskDto.PerspectiveSectionId = aAppProjectPerspectiveTaskEntity.PerspectiveSectionId;
 			aAppProjectPerspectiveTaskDto.ProjectWorkFlowTaskId = aAppProjectPerspectiveTaskEntity.ProjectWorkFlowTaskId;
 			aAppProjectPerspectiveTaskDto.DisplayOrder = aAppProjectPerspectiveTaskEntity.DisplayOrder;
 			aAppProjectPerspectiveTaskDto.AddtionTaskNotes = aAppProjectPerspectiveTaskEntity.AddtionTaskNotes;
 			aAppProjectPerspectiveTaskDto.Description = aAppProjectPerspectiveTaskEntity.Description;
 			aAppProjectPerspectiveTaskDto.AppCreatedById = aAppProjectPerspectiveTaskEntity.AppCreatedById;
 			aAppProjectPerspectiveTaskDto.AppCreatedDate = aAppProjectPerspectiveTaskEntity.AppCreatedDate;
 			aAppProjectPerspectiveTaskDto.AppModifiedDate = aAppProjectPerspectiveTaskEntity.AppModifiedDate;
 			aAppProjectPerspectiveTaskDto.AppModifiedById = aAppProjectPerspectiveTaskEntity.AppModifiedById;
 			aAppProjectPerspectiveTaskDto.AppCreatedByCompanyId = aAppProjectPerspectiveTaskEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppProjectPerspectiveTaskDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectPerspectiveTaskEntity.AppCreatedDate);
                aAppProjectPerspectiveTaskDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectPerspectiveTaskEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppProjectPerspectiveTaskEntity, aAppProjectPerspectiveTaskDto);
		}
		
		 /// <summary>
        ///  Copy AppProjectPerspectiveTaskDto Properties to   AppProjectPerspectiveTaskEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppProjectPerspectiveTaskEntity aAppProjectPerspectiveTaskEntity,AppProjectPerspectiveTaskDto aAppProjectPerspectiveTaskDto)
        {        
 
      			aAppProjectPerspectiveTaskEntity.PerspectiveSectionId = aAppProjectPerspectiveTaskDto.PerspectiveSectionId;
      			aAppProjectPerspectiveTaskEntity.ProjectWorkFlowTaskId = aAppProjectPerspectiveTaskDto.ProjectWorkFlowTaskId;
      			aAppProjectPerspectiveTaskEntity.DisplayOrder = aAppProjectPerspectiveTaskDto.DisplayOrder;
      			aAppProjectPerspectiveTaskEntity.AddtionTaskNotes = aAppProjectPerspectiveTaskDto.AddtionTaskNotes;
      			aAppProjectPerspectiveTaskEntity.Description = aAppProjectPerspectiveTaskDto.Description;
 
  
   
    
      			aAppProjectPerspectiveTaskEntity.AppCreatedByCompanyId = aAppProjectPerspectiveTaskDto.AppCreatedByCompanyId;
			
			if(aAppProjectPerspectiveTaskDto.Id == null)
			{
				aAppProjectPerspectiveTaskEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectPerspectiveTaskEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppProjectPerspectiveTaskEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectPerspectiveTaskEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectPerspectiveTaskEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppProjectPerspectiveTaskEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectPerspectiveTaskEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppProjectPerspectiveTaskEntity, aAppProjectPerspectiveTaskDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppProjectPerspectiveTaskEntity aAppProjectPerspectiveTaskEntity,AppProjectPerspectiveTaskDto aAppProjectPerspectiveTaskDto);
		
		static partial void OnCopyDtoToEntityDone(AppProjectPerspectiveTaskEntity aAppProjectPerspectiveTaskEntity,AppProjectPerspectiveTaskDto aAppProjectPerspectiveTaskDto);
		
   
       
    }
}

 