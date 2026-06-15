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
    /// Convert Properties between  AppApplicationAssetsItemEntity and  AppApplicationAssetsItemDto
    /// </summary>
    public static partial class AppApplicationAssetsItemConverter 
    {
         /// <summary>
        ///  Convert AppApplicationAssetsItemEntity To  AppApplicationAssetsItemDto
        /// </summary>
        public static AppApplicationAssetsItemDto ConvertEntityToDto(AppApplicationAssetsItemEntity aAppApplicationAssetsItemEntity)
        {        
    		AppApplicationAssetsItemDto aAppApplicationAssetsItemDto = new AppApplicationAssetsItemDto();
    		CopyEntityPropertyToDto( aAppApplicationAssetsItemEntity, aAppApplicationAssetsItemDto);          
			return aAppApplicationAssetsItemDto;
        }
		 /// <summary>
        ///  Convert AppApplicationAssetsItemEntity To  AppApplicationAssetsItemExDto
        /// </summary>
        public static AppApplicationAssetsItemExDto ConvertEntityToExDto(AppApplicationAssetsItemEntity aAppApplicationAssetsItemEntity)
        {        
    		AppApplicationAssetsItemExDto aAppApplicationAssetsItemExDto = new AppApplicationAssetsItemExDto();
			CopyEntityPropertyToDto( aAppApplicationAssetsItemEntity, aAppApplicationAssetsItemExDto);
			
			
			
            return aAppApplicationAssetsItemExDto;
        }
		
		 /// <summary>
        ///  Convert AppApplicationAssetsItemEntity To  AppApplicationAssetsItemDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppApplicationAssetsItemEntity aAppApplicationAssetsItemEntity,AppApplicationAssetsItemDto aAppApplicationAssetsItemDto)
        {        
    		
           // aAppApplicationAssetsItemDto.StopChangeTracking();
 			aAppApplicationAssetsItemDto.Id = aAppApplicationAssetsItemEntity.AssetsItemId;
 			aAppApplicationAssetsItemDto.ApplicationId = aAppApplicationAssetsItemEntity.ApplicationId;
 			aAppApplicationAssetsItemDto.Name = aAppApplicationAssetsItemEntity.Name;
 			aAppApplicationAssetsItemDto.Description = aAppApplicationAssetsItemEntity.Description;
 			aAppApplicationAssetsItemDto.FormId = aAppApplicationAssetsItemEntity.FormId;
 			aAppApplicationAssetsItemDto.TransactionId = aAppApplicationAssetsItemEntity.TransactionId;
 			aAppApplicationAssetsItemDto.ProjectWorkflowId = aAppApplicationAssetsItemEntity.ProjectWorkflowId;
 			aAppApplicationAssetsItemDto.SearchId = aAppApplicationAssetsItemEntity.SearchId;
 			aAppApplicationAssetsItemDto.ReportId = aAppApplicationAssetsItemEntity.ReportId;
 			aAppApplicationAssetsItemDto.DesktopId = aAppApplicationAssetsItemEntity.DesktopId;
 			aAppApplicationAssetsItemDto.AppCreatedById = aAppApplicationAssetsItemEntity.AppCreatedById;
 			aAppApplicationAssetsItemDto.AppCreatedDate = aAppApplicationAssetsItemEntity.AppCreatedDate;
 			aAppApplicationAssetsItemDto.AppModifiedDate = aAppApplicationAssetsItemEntity.AppModifiedDate;
 			aAppApplicationAssetsItemDto.AppModifiedById = aAppApplicationAssetsItemEntity.AppModifiedById;
 			aAppApplicationAssetsItemDto.AppCreatedByCompanyId = aAppApplicationAssetsItemEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppApplicationAssetsItemDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppApplicationAssetsItemEntity.AppCreatedDate);
                aAppApplicationAssetsItemDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppApplicationAssetsItemEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppApplicationAssetsItemEntity, aAppApplicationAssetsItemDto);
		}
		
		 /// <summary>
        ///  Copy AppApplicationAssetsItemDto Properties to   AppApplicationAssetsItemEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppApplicationAssetsItemEntity aAppApplicationAssetsItemEntity,AppApplicationAssetsItemDto aAppApplicationAssetsItemDto)
        {        
 
      			aAppApplicationAssetsItemEntity.ApplicationId = aAppApplicationAssetsItemDto.ApplicationId;
      			aAppApplicationAssetsItemEntity.Name = aAppApplicationAssetsItemDto.Name;
      			aAppApplicationAssetsItemEntity.Description = aAppApplicationAssetsItemDto.Description;
      			aAppApplicationAssetsItemEntity.FormId = aAppApplicationAssetsItemDto.FormId;
      			aAppApplicationAssetsItemEntity.TransactionId = aAppApplicationAssetsItemDto.TransactionId;
      			aAppApplicationAssetsItemEntity.ProjectWorkflowId = aAppApplicationAssetsItemDto.ProjectWorkflowId;
      			aAppApplicationAssetsItemEntity.SearchId = aAppApplicationAssetsItemDto.SearchId;
      			aAppApplicationAssetsItemEntity.ReportId = aAppApplicationAssetsItemDto.ReportId;
      			aAppApplicationAssetsItemEntity.DesktopId = aAppApplicationAssetsItemDto.DesktopId;
 
  
   
    
      			aAppApplicationAssetsItemEntity.AppCreatedByCompanyId = aAppApplicationAssetsItemDto.AppCreatedByCompanyId;
			
			if(aAppApplicationAssetsItemDto.Id == null)
			{
				aAppApplicationAssetsItemEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppApplicationAssetsItemEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppApplicationAssetsItemEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppApplicationAssetsItemEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppApplicationAssetsItemEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppApplicationAssetsItemEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppApplicationAssetsItemEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppApplicationAssetsItemEntity, aAppApplicationAssetsItemDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppApplicationAssetsItemEntity aAppApplicationAssetsItemEntity,AppApplicationAssetsItemDto aAppApplicationAssetsItemDto);
		
		static partial void OnCopyDtoToEntityDone(AppApplicationAssetsItemEntity aAppApplicationAssetsItemEntity,AppApplicationAssetsItemDto aAppApplicationAssetsItemDto);
		
   
       
    }
}

 