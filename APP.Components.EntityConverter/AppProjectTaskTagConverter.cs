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
    /// Convert Properties between  AppProjectTaskTagEntity and  AppProjectTaskTagDto
    /// </summary>
    public static partial class AppProjectTaskTagConverter 
    {
         /// <summary>
        ///  Convert AppProjectTaskTagEntity To  AppProjectTaskTagDto
        /// </summary>
        public static AppProjectTaskTagDto ConvertEntityToDto(AppProjectTaskTagEntity aAppProjectTaskTagEntity)
        {        
    		AppProjectTaskTagDto aAppProjectTaskTagDto = new AppProjectTaskTagDto();
    		CopyEntityPropertyToDto( aAppProjectTaskTagEntity, aAppProjectTaskTagDto);          
			return aAppProjectTaskTagDto;
        }
		 /// <summary>
        ///  Convert AppProjectTaskTagEntity To  AppProjectTaskTagExDto
        /// </summary>
        public static AppProjectTaskTagExDto ConvertEntityToExDto(AppProjectTaskTagEntity aAppProjectTaskTagEntity)
        {        
    		AppProjectTaskTagExDto aAppProjectTaskTagExDto = new AppProjectTaskTagExDto();
			CopyEntityPropertyToDto( aAppProjectTaskTagEntity, aAppProjectTaskTagExDto);
			
			
			
            return aAppProjectTaskTagExDto;
        }
		
		 /// <summary>
        ///  Convert AppProjectTaskTagEntity To  AppProjectTaskTagDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppProjectTaskTagEntity aAppProjectTaskTagEntity,AppProjectTaskTagDto aAppProjectTaskTagDto)
        {        
    		
           // aAppProjectTaskTagDto.StopChangeTracking();
 			aAppProjectTaskTagDto.Id = aAppProjectTaskTagEntity.TaskTagId;
 			aAppProjectTaskTagDto.ProjectWorkFlowTaskId = aAppProjectTaskTagEntity.ProjectWorkFlowTaskId;
 			aAppProjectTaskTagDto.ScopeTagId = aAppProjectTaskTagEntity.ScopeTagId;
 			aAppProjectTaskTagDto.AppCreatedById = aAppProjectTaskTagEntity.AppCreatedById;
 			aAppProjectTaskTagDto.AppCreatedDate = aAppProjectTaskTagEntity.AppCreatedDate;
 			aAppProjectTaskTagDto.AppModifiedDate = aAppProjectTaskTagEntity.AppModifiedDate;
 			aAppProjectTaskTagDto.AppModifiedById = aAppProjectTaskTagEntity.AppModifiedById;
 			aAppProjectTaskTagDto.AppCreatedByCompanyId = aAppProjectTaskTagEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppProjectTaskTagDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectTaskTagEntity.AppCreatedDate);
                aAppProjectTaskTagDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectTaskTagEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppProjectTaskTagEntity, aAppProjectTaskTagDto);
		}
		
		 /// <summary>
        ///  Copy AppProjectTaskTagDto Properties to   AppProjectTaskTagEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppProjectTaskTagEntity aAppProjectTaskTagEntity,AppProjectTaskTagDto aAppProjectTaskTagDto)
        {        
 
      			aAppProjectTaskTagEntity.ProjectWorkFlowTaskId = aAppProjectTaskTagDto.ProjectWorkFlowTaskId;
      			aAppProjectTaskTagEntity.ScopeTagId = aAppProjectTaskTagDto.ScopeTagId;
 
  
   
    
      			aAppProjectTaskTagEntity.AppCreatedByCompanyId = aAppProjectTaskTagDto.AppCreatedByCompanyId;
			
			if(aAppProjectTaskTagDto.Id == null)
			{
				aAppProjectTaskTagEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectTaskTagEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppProjectTaskTagEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectTaskTagEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectTaskTagEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppProjectTaskTagEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectTaskTagEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppProjectTaskTagEntity, aAppProjectTaskTagDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppProjectTaskTagEntity aAppProjectTaskTagEntity,AppProjectTaskTagDto aAppProjectTaskTagDto);
		
		static partial void OnCopyDtoToEntityDone(AppProjectTaskTagEntity aAppProjectTaskTagEntity,AppProjectTaskTagDto aAppProjectTaskTagDto);
		
   
       
    }
}

 