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
    /// Convert Properties between  AppEsiteCatalogueEntity and  AppEsiteCatalogueDto
    /// </summary>
    public static partial class AppEsiteCatalogueConverter 
    {
         /// <summary>
        ///  Convert AppEsiteCatalogueEntity To  AppEsiteCatalogueDto
        /// </summary>
        public static AppEsiteCatalogueDto ConvertEntityToDto(AppEsiteCatalogueEntity aAppEsiteCatalogueEntity)
        {        
    		AppEsiteCatalogueDto aAppEsiteCatalogueDto = new AppEsiteCatalogueDto();
    		CopyEntityPropertyToDto( aAppEsiteCatalogueEntity, aAppEsiteCatalogueDto);          
			return aAppEsiteCatalogueDto;
        }
		 /// <summary>
        ///  Convert AppEsiteCatalogueEntity To  AppEsiteCatalogueExDto
        /// </summary>
        public static AppEsiteCatalogueExDto ConvertEntityToExDto(AppEsiteCatalogueEntity aAppEsiteCatalogueEntity)
        {        
    		AppEsiteCatalogueExDto aAppEsiteCatalogueExDto = new AppEsiteCatalogueExDto();
			CopyEntityPropertyToDto( aAppEsiteCatalogueEntity, aAppEsiteCatalogueExDto);
			
			
			
            return aAppEsiteCatalogueExDto;
        }
		
		 /// <summary>
        ///  Convert AppEsiteCatalogueEntity To  AppEsiteCatalogueDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppEsiteCatalogueEntity aAppEsiteCatalogueEntity,AppEsiteCatalogueDto aAppEsiteCatalogueDto)
        {        
    		
           // aAppEsiteCatalogueDto.StopChangeTracking();
 			aAppEsiteCatalogueDto.Id = aAppEsiteCatalogueEntity.EsiteCatalogueId;
 			aAppEsiteCatalogueDto.EsiteId = aAppEsiteCatalogueEntity.EsiteId;
 			aAppEsiteCatalogueDto.Sort = aAppEsiteCatalogueEntity.Sort;
 			aAppEsiteCatalogueDto.Name = aAppEsiteCatalogueEntity.Name;
 			aAppEsiteCatalogueDto.Description = aAppEsiteCatalogueEntity.Description;
 			aAppEsiteCatalogueDto.EmAppEstoreLayout = aAppEsiteCatalogueEntity.EmAppEstoreLayout;
 			aAppEsiteCatalogueDto.EmAppEstoreTheme = aAppEsiteCatalogueEntity.EmAppEstoreTheme;
 			aAppEsiteCatalogueDto.TreeNavigationViewId = aAppEsiteCatalogueEntity.TreeNavigationViewId;
 			aAppEsiteCatalogueDto.CatalogCardViewId = aAppEsiteCatalogueEntity.CatalogCardViewId;
 			aAppEsiteCatalogueDto.CatalogCardDetailId = aAppEsiteCatalogueEntity.CatalogCardDetailId;
 			aAppEsiteCatalogueDto.SaasApplicationId = aAppEsiteCatalogueEntity.SaasApplicationId;
 			aAppEsiteCatalogueDto.IsActive = aAppEsiteCatalogueEntity.IsActive;
 			aAppEsiteCatalogueDto.IsDefault = aAppEsiteCatalogueEntity.IsDefault;
 			aAppEsiteCatalogueDto.AppCreatedByCompanyId = aAppEsiteCatalogueEntity.AppCreatedByCompanyId;
 			aAppEsiteCatalogueDto.AppCreatedById = aAppEsiteCatalogueEntity.AppCreatedById;
 			aAppEsiteCatalogueDto.AppCreatedDate = aAppEsiteCatalogueEntity.AppCreatedDate;
 			aAppEsiteCatalogueDto.AppModifiedDate = aAppEsiteCatalogueEntity.AppModifiedDate;
 			aAppEsiteCatalogueDto.AppModifiedById = aAppEsiteCatalogueEntity.AppModifiedById;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppEsiteCatalogueDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppEsiteCatalogueEntity.AppCreatedDate);
                aAppEsiteCatalogueDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppEsiteCatalogueEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppEsiteCatalogueEntity, aAppEsiteCatalogueDto);
		}
		
		 /// <summary>
        ///  Copy AppEsiteCatalogueDto Properties to   AppEsiteCatalogueEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppEsiteCatalogueEntity aAppEsiteCatalogueEntity,AppEsiteCatalogueDto aAppEsiteCatalogueDto)
        {        
 
      			aAppEsiteCatalogueEntity.EsiteId = aAppEsiteCatalogueDto.EsiteId;
      			aAppEsiteCatalogueEntity.Sort = aAppEsiteCatalogueDto.Sort;
      			aAppEsiteCatalogueEntity.Name = aAppEsiteCatalogueDto.Name;
      			aAppEsiteCatalogueEntity.Description = aAppEsiteCatalogueDto.Description;
      			aAppEsiteCatalogueEntity.EmAppEstoreLayout = aAppEsiteCatalogueDto.EmAppEstoreLayout;
      			aAppEsiteCatalogueEntity.EmAppEstoreTheme = aAppEsiteCatalogueDto.EmAppEstoreTheme;
      			aAppEsiteCatalogueEntity.TreeNavigationViewId = aAppEsiteCatalogueDto.TreeNavigationViewId;
      			aAppEsiteCatalogueEntity.CatalogCardViewId = aAppEsiteCatalogueDto.CatalogCardViewId;
      			aAppEsiteCatalogueEntity.CatalogCardDetailId = aAppEsiteCatalogueDto.CatalogCardDetailId;
      			aAppEsiteCatalogueEntity.SaasApplicationId = aAppEsiteCatalogueDto.SaasApplicationId;
      			aAppEsiteCatalogueEntity.IsActive = aAppEsiteCatalogueDto.IsActive;
      			aAppEsiteCatalogueEntity.IsDefault = aAppEsiteCatalogueDto.IsDefault;
      			aAppEsiteCatalogueEntity.AppCreatedByCompanyId = aAppEsiteCatalogueDto.AppCreatedByCompanyId;
 
  
   
    
			
			if(aAppEsiteCatalogueDto.Id == null)
			{
				aAppEsiteCatalogueEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppEsiteCatalogueEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppEsiteCatalogueEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppEsiteCatalogueEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppEsiteCatalogueEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppEsiteCatalogueEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppEsiteCatalogueEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppEsiteCatalogueEntity, aAppEsiteCatalogueDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppEsiteCatalogueEntity aAppEsiteCatalogueEntity,AppEsiteCatalogueDto aAppEsiteCatalogueDto);
		
		static partial void OnCopyDtoToEntityDone(AppEsiteCatalogueEntity aAppEsiteCatalogueEntity,AppEsiteCatalogueDto aAppEsiteCatalogueDto);
		
   
       
    }
}

 