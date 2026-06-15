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
    /// Convert Properties between  AppProjectPerspectiveViewEntity and  AppProjectPerspectiveViewDto
    /// </summary>
    public static partial class AppProjectPerspectiveViewConverter 
    {
         /// <summary>
        ///  Convert AppProjectPerspectiveViewEntity To  AppProjectPerspectiveViewDto
        /// </summary>
        public static AppProjectPerspectiveViewDto ConvertEntityToDto(AppProjectPerspectiveViewEntity aAppProjectPerspectiveViewEntity)
        {        
    		AppProjectPerspectiveViewDto aAppProjectPerspectiveViewDto = new AppProjectPerspectiveViewDto();
    		CopyEntityPropertyToDto( aAppProjectPerspectiveViewEntity, aAppProjectPerspectiveViewDto);          
			return aAppProjectPerspectiveViewDto;
        }
		 /// <summary>
        ///  Convert AppProjectPerspectiveViewEntity To  AppProjectPerspectiveViewExDto
        /// </summary>
        public static AppProjectPerspectiveViewExDto ConvertEntityToExDto(AppProjectPerspectiveViewEntity aAppProjectPerspectiveViewEntity)
        {        
    		AppProjectPerspectiveViewExDto aAppProjectPerspectiveViewExDto = new AppProjectPerspectiveViewExDto();
			CopyEntityPropertyToDto( aAppProjectPerspectiveViewEntity, aAppProjectPerspectiveViewExDto);
			
			
			
            return aAppProjectPerspectiveViewExDto;
        }
		
		 /// <summary>
        ///  Convert AppProjectPerspectiveViewEntity To  AppProjectPerspectiveViewDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppProjectPerspectiveViewEntity aAppProjectPerspectiveViewEntity,AppProjectPerspectiveViewDto aAppProjectPerspectiveViewDto)
        {        
    		
           // aAppProjectPerspectiveViewDto.StopChangeTracking();
 			aAppProjectPerspectiveViewDto.Id = aAppProjectPerspectiveViewEntity.PerspectiveSectionId;
 			aAppProjectPerspectiveViewDto.ProjectId = aAppProjectPerspectiveViewEntity.ProjectId;
 			aAppProjectPerspectiveViewDto.ViewSectionName = aAppProjectPerspectiveViewEntity.ViewSectionName;
 			aAppProjectPerspectiveViewDto.Description = aAppProjectPerspectiveViewEntity.Description;
 			aAppProjectPerspectiveViewDto.DisplayOrder = aAppProjectPerspectiveViewEntity.DisplayOrder;
 			aAppProjectPerspectiveViewDto.AppCreatedById = aAppProjectPerspectiveViewEntity.AppCreatedById;
 			aAppProjectPerspectiveViewDto.AppCreatedDate = aAppProjectPerspectiveViewEntity.AppCreatedDate;
 			aAppProjectPerspectiveViewDto.AppModifiedDate = aAppProjectPerspectiveViewEntity.AppModifiedDate;
 			aAppProjectPerspectiveViewDto.AppModifiedById = aAppProjectPerspectiveViewEntity.AppModifiedById;
 			aAppProjectPerspectiveViewDto.AppCreatedByCompanyId = aAppProjectPerspectiveViewEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppProjectPerspectiveViewDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectPerspectiveViewEntity.AppCreatedDate);
                aAppProjectPerspectiveViewDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectPerspectiveViewEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppProjectPerspectiveViewEntity, aAppProjectPerspectiveViewDto);
		}
		
		 /// <summary>
        ///  Copy AppProjectPerspectiveViewDto Properties to   AppProjectPerspectiveViewEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppProjectPerspectiveViewEntity aAppProjectPerspectiveViewEntity,AppProjectPerspectiveViewDto aAppProjectPerspectiveViewDto)
        {        
 
      			aAppProjectPerspectiveViewEntity.ProjectId = aAppProjectPerspectiveViewDto.ProjectId;
      			aAppProjectPerspectiveViewEntity.ViewSectionName = aAppProjectPerspectiveViewDto.ViewSectionName;
      			aAppProjectPerspectiveViewEntity.Description = aAppProjectPerspectiveViewDto.Description;
      			aAppProjectPerspectiveViewEntity.DisplayOrder = aAppProjectPerspectiveViewDto.DisplayOrder;
 
  
   
    
      			aAppProjectPerspectiveViewEntity.AppCreatedByCompanyId = aAppProjectPerspectiveViewDto.AppCreatedByCompanyId;
			
			if(aAppProjectPerspectiveViewDto.Id == null)
			{
				aAppProjectPerspectiveViewEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectPerspectiveViewEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppProjectPerspectiveViewEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectPerspectiveViewEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectPerspectiveViewEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppProjectPerspectiveViewEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectPerspectiveViewEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppProjectPerspectiveViewEntity, aAppProjectPerspectiveViewDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppProjectPerspectiveViewEntity aAppProjectPerspectiveViewEntity,AppProjectPerspectiveViewDto aAppProjectPerspectiveViewDto);
		
		static partial void OnCopyDtoToEntityDone(AppProjectPerspectiveViewEntity aAppProjectPerspectiveViewEntity,AppProjectPerspectiveViewDto aAppProjectPerspectiveViewDto);
		
   
       
    }
}

 