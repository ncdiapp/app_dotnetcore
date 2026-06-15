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
    /// Convert Properties between  AppSearchEntity and  AppSearchDto
    /// </summary>
    public static partial class AppSearchConverter 
    {
         /// <summary>
        ///  Convert AppSearchEntity To  AppSearchDto
        /// </summary>
        public static AppSearchDto ConvertEntityToDto(AppSearchEntity aAppSearchEntity)
        {        
    		AppSearchDto aAppSearchDto = new AppSearchDto();
    		CopyEntityPropertyToDto( aAppSearchEntity, aAppSearchDto);          
			return aAppSearchDto;
        }
		 /// <summary>
        ///  Convert AppSearchEntity To  AppSearchExDto
        /// </summary>
        public static AppSearchExDto ConvertEntityToExDto(AppSearchEntity aAppSearchEntity)
        {        
    		AppSearchExDto aAppSearchExDto = new AppSearchExDto();
			CopyEntityPropertyToDto( aAppSearchEntity, aAppSearchExDto);
			
			
			
            return aAppSearchExDto;
        }
		
		 /// <summary>
        ///  Convert AppSearchEntity To  AppSearchDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSearchEntity aAppSearchEntity,AppSearchDto aAppSearchDto)
        {        
    		
           // aAppSearchDto.StopChangeTracking();
 			aAppSearchDto.Id = aAppSearchEntity.SearchId;
 			aAppSearchDto.Name = aAppSearchEntity.Name;
 			aAppSearchDto.Description = aAppSearchEntity.Description;
 			aAppSearchDto.Type = aAppSearchEntity.Type;
 			aAppSearchDto.IsBuiltIn = aAppSearchEntity.IsBuiltIn;
 			aAppSearchDto.WhereUsedSearchId = aAppSearchEntity.WhereUsedSearchId;
 			aAppSearchDto.SearchViewId = aAppSearchEntity.SearchViewId;
 			aAppSearchDto.IsAutoExecute = aAppSearchEntity.IsAutoExecute;
 			aAppSearchDto.DataSetId = aAppSearchEntity.DataSetId;
 			aAppSearchDto.FilterByCurrentUserMappingField = aAppSearchEntity.FilterByCurrentUserMappingField;
 			aAppSearchDto.FilterByCurrentUserDomainTypeMappingField = aAppSearchEntity.FilterByCurrentUserDomainTypeMappingField;
 			aAppSearchDto.BusinessScopeId = aAppSearchEntity.BusinessScopeId;
 			aAppSearchDto.FilterByCurrentUserRoleMappingField = aAppSearchEntity.FilterByCurrentUserRoleMappingField;
 			aAppSearchDto.AppCreatedById = aAppSearchEntity.AppCreatedById;
 			aAppSearchDto.AppCreatedDate = aAppSearchEntity.AppCreatedDate;
 			aAppSearchDto.AppModifiedDate = aAppSearchEntity.AppModifiedDate;
 			aAppSearchDto.AppModifiedById = aAppSearchEntity.AppModifiedById;
 			aAppSearchDto.FolderTransactionId = aAppSearchEntity.FolderTransactionId;
 			aAppSearchDto.AppCreatedByCompanyId = aAppSearchEntity.AppCreatedByCompanyId;
 			aAppSearchDto.IsHideAllToolsBar = aAppSearchEntity.IsHideAllToolsBar;
 			aAppSearchDto.SaasApplicationId = aAppSearchEntity.SaasApplicationId;
 			aAppSearchDto.IsForPublicAcesss = aAppSearchEntity.IsForPublicAcesss;
 			aAppSearchDto.IsFilterByUserTypeEntity = aAppSearchEntity.IsFilterByUserTypeEntity;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSearchDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSearchEntity.AppCreatedDate);
                aAppSearchDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSearchEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSearchEntity, aAppSearchDto);
		}
		
		 /// <summary>
        ///  Copy AppSearchDto Properties to   AppSearchEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSearchEntity aAppSearchEntity,AppSearchDto aAppSearchDto)
        {        
 
      			aAppSearchEntity.Name = aAppSearchDto.Name;
      			aAppSearchEntity.Description = aAppSearchDto.Description;
      			aAppSearchEntity.Type = aAppSearchDto.Type;
      			aAppSearchEntity.IsBuiltIn = aAppSearchDto.IsBuiltIn;
      			aAppSearchEntity.WhereUsedSearchId = aAppSearchDto.WhereUsedSearchId;
      			aAppSearchEntity.SearchViewId = aAppSearchDto.SearchViewId;
      			aAppSearchEntity.IsAutoExecute = aAppSearchDto.IsAutoExecute;
      			aAppSearchEntity.DataSetId = aAppSearchDto.DataSetId;
      			aAppSearchEntity.FilterByCurrentUserMappingField = aAppSearchDto.FilterByCurrentUserMappingField;
      			aAppSearchEntity.FilterByCurrentUserDomainTypeMappingField = aAppSearchDto.FilterByCurrentUserDomainTypeMappingField;
      			aAppSearchEntity.BusinessScopeId = aAppSearchDto.BusinessScopeId;
      			aAppSearchEntity.FilterByCurrentUserRoleMappingField = aAppSearchDto.FilterByCurrentUserRoleMappingField;
 
  
   
    
      			aAppSearchEntity.FolderTransactionId = aAppSearchDto.FolderTransactionId;
      			aAppSearchEntity.AppCreatedByCompanyId = aAppSearchDto.AppCreatedByCompanyId;
      			aAppSearchEntity.IsHideAllToolsBar = aAppSearchDto.IsHideAllToolsBar;
      			aAppSearchEntity.SaasApplicationId = aAppSearchDto.SaasApplicationId;
      			aAppSearchEntity.IsForPublicAcesss = aAppSearchDto.IsForPublicAcesss;
      			aAppSearchEntity.IsFilterByUserTypeEntity = aAppSearchDto.IsFilterByUserTypeEntity;
			
			if(aAppSearchDto.Id == null)
			{
				aAppSearchEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSearchEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSearchEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSearchEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSearchEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSearchEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSearchEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSearchEntity, aAppSearchDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSearchEntity aAppSearchEntity,AppSearchDto aAppSearchDto);
		
		static partial void OnCopyDtoToEntityDone(AppSearchEntity aAppSearchEntity,AppSearchDto aAppSearchDto);
		
   
       
    }
}

 