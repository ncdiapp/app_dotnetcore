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
    /// Convert Properties between  AppProjectTemplateResourceEntity and  AppProjectTemplateResourceDto
    /// </summary>
    public static partial class AppProjectTemplateResourceConverter 
    {
         /// <summary>
        ///  Convert AppProjectTemplateResourceEntity To  AppProjectTemplateResourceDto
        /// </summary>
        public static AppProjectTemplateResourceDto ConvertEntityToDto(AppProjectTemplateResourceEntity aAppProjectTemplateResourceEntity)
        {        
    		AppProjectTemplateResourceDto aAppProjectTemplateResourceDto = new AppProjectTemplateResourceDto();
    		CopyEntityPropertyToDto( aAppProjectTemplateResourceEntity, aAppProjectTemplateResourceDto);          
			return aAppProjectTemplateResourceDto;
        }
		 /// <summary>
        ///  Convert AppProjectTemplateResourceEntity To  AppProjectTemplateResourceExDto
        /// </summary>
        public static AppProjectTemplateResourceExDto ConvertEntityToExDto(AppProjectTemplateResourceEntity aAppProjectTemplateResourceEntity)
        {        
    		AppProjectTemplateResourceExDto aAppProjectTemplateResourceExDto = new AppProjectTemplateResourceExDto();
			CopyEntityPropertyToDto( aAppProjectTemplateResourceEntity, aAppProjectTemplateResourceExDto);
			
			
			
            return aAppProjectTemplateResourceExDto;
        }
		
		 /// <summary>
        ///  Convert AppProjectTemplateResourceEntity To  AppProjectTemplateResourceDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppProjectTemplateResourceEntity aAppProjectTemplateResourceEntity,AppProjectTemplateResourceDto aAppProjectTemplateResourceDto)
        {        
    		
           // aAppProjectTemplateResourceDto.StopChangeTracking();
 			aAppProjectTemplateResourceDto.Id = aAppProjectTemplateResourceEntity.TemplateResouceId;
 			aAppProjectTemplateResourceDto.ProejctId = aAppProjectTemplateResourceEntity.ProejctId;
 			aAppProjectTemplateResourceDto.ResourceName = aAppProjectTemplateResourceEntity.ResourceName;
 			aAppProjectTemplateResourceDto.Description = aAppProjectTemplateResourceEntity.Description;
 			aAppProjectTemplateResourceDto.AppCreatedById = aAppProjectTemplateResourceEntity.AppCreatedById;
 			aAppProjectTemplateResourceDto.AppCreatedDate = aAppProjectTemplateResourceEntity.AppCreatedDate;
 			aAppProjectTemplateResourceDto.AppModifiedDate = aAppProjectTemplateResourceEntity.AppModifiedDate;
 			aAppProjectTemplateResourceDto.AppModifiedById = aAppProjectTemplateResourceEntity.AppModifiedById;
 			aAppProjectTemplateResourceDto.AppCreatedByCompanyId = aAppProjectTemplateResourceEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppProjectTemplateResourceDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectTemplateResourceEntity.AppCreatedDate);
                aAppProjectTemplateResourceDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectTemplateResourceEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppProjectTemplateResourceEntity, aAppProjectTemplateResourceDto);
		}
		
		 /// <summary>
        ///  Copy AppProjectTemplateResourceDto Properties to   AppProjectTemplateResourceEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppProjectTemplateResourceEntity aAppProjectTemplateResourceEntity,AppProjectTemplateResourceDto aAppProjectTemplateResourceDto)
        {        
 
      			aAppProjectTemplateResourceEntity.ProejctId = aAppProjectTemplateResourceDto.ProejctId;
      			aAppProjectTemplateResourceEntity.ResourceName = aAppProjectTemplateResourceDto.ResourceName;
      			aAppProjectTemplateResourceEntity.Description = aAppProjectTemplateResourceDto.Description;
 
  
   
    
      			aAppProjectTemplateResourceEntity.AppCreatedByCompanyId = aAppProjectTemplateResourceDto.AppCreatedByCompanyId;
			
			if(aAppProjectTemplateResourceDto.Id == null)
			{
				aAppProjectTemplateResourceEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectTemplateResourceEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppProjectTemplateResourceEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectTemplateResourceEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectTemplateResourceEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppProjectTemplateResourceEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectTemplateResourceEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppProjectTemplateResourceEntity, aAppProjectTemplateResourceDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppProjectTemplateResourceEntity aAppProjectTemplateResourceEntity,AppProjectTemplateResourceDto aAppProjectTemplateResourceDto);
		
		static partial void OnCopyDtoToEntityDone(AppProjectTemplateResourceEntity aAppProjectTemplateResourceEntity,AppProjectTemplateResourceDto aAppProjectTemplateResourceDto);
		
   
       
    }
}

 