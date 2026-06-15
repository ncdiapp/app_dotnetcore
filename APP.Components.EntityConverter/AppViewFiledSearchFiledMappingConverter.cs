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
    /// Convert Properties between  AppViewFiledSearchFiledMappingEntity and  AppViewFiledSearchFiledMappingDto
    /// </summary>
    public static partial class AppViewFiledSearchFiledMappingConverter 
    {
         /// <summary>
        ///  Convert AppViewFiledSearchFiledMappingEntity To  AppViewFiledSearchFiledMappingDto
        /// </summary>
        public static AppViewFiledSearchFiledMappingDto ConvertEntityToDto(AppViewFiledSearchFiledMappingEntity aAppViewFiledSearchFiledMappingEntity)
        {        
    		AppViewFiledSearchFiledMappingDto aAppViewFiledSearchFiledMappingDto = new AppViewFiledSearchFiledMappingDto();
    		CopyEntityPropertyToDto( aAppViewFiledSearchFiledMappingEntity, aAppViewFiledSearchFiledMappingDto);          
			return aAppViewFiledSearchFiledMappingDto;
        }
		 /// <summary>
        ///  Convert AppViewFiledSearchFiledMappingEntity To  AppViewFiledSearchFiledMappingExDto
        /// </summary>
        public static AppViewFiledSearchFiledMappingExDto ConvertEntityToExDto(AppViewFiledSearchFiledMappingEntity aAppViewFiledSearchFiledMappingEntity)
        {        
    		AppViewFiledSearchFiledMappingExDto aAppViewFiledSearchFiledMappingExDto = new AppViewFiledSearchFiledMappingExDto();
			CopyEntityPropertyToDto( aAppViewFiledSearchFiledMappingEntity, aAppViewFiledSearchFiledMappingExDto);
			
			
			
            return aAppViewFiledSearchFiledMappingExDto;
        }
		
		 /// <summary>
        ///  Convert AppViewFiledSearchFiledMappingEntity To  AppViewFiledSearchFiledMappingDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppViewFiledSearchFiledMappingEntity aAppViewFiledSearchFiledMappingEntity,AppViewFiledSearchFiledMappingDto aAppViewFiledSearchFiledMappingDto)
        {        
    		
           // aAppViewFiledSearchFiledMappingDto.StopChangeTracking();
 			aAppViewFiledSearchFiledMappingDto.Id = aAppViewFiledSearchFiledMappingEntity.MappingId;
 			aAppViewFiledSearchFiledMappingDto.SearchViewId = aAppViewFiledSearchFiledMappingEntity.SearchViewId;
 			aAppViewFiledSearchFiledMappingDto.SearchViewFiledId = aAppViewFiledSearchFiledMappingEntity.SearchViewFiledId;
 			aAppViewFiledSearchFiledMappingDto.SearchId = aAppViewFiledSearchFiledMappingEntity.SearchId;
 			aAppViewFiledSearchFiledMappingDto.SearchFieldId = aAppViewFiledSearchFiledMappingEntity.SearchFieldId;
 			aAppViewFiledSearchFiledMappingDto.AppCreatedById = aAppViewFiledSearchFiledMappingEntity.AppCreatedById;
 			aAppViewFiledSearchFiledMappingDto.AppCreatedDate = aAppViewFiledSearchFiledMappingEntity.AppCreatedDate;
 			aAppViewFiledSearchFiledMappingDto.AppModifiedDate = aAppViewFiledSearchFiledMappingEntity.AppModifiedDate;
 			aAppViewFiledSearchFiledMappingDto.AppModifiedById = aAppViewFiledSearchFiledMappingEntity.AppModifiedById;
 			aAppViewFiledSearchFiledMappingDto.AppCreatedByCompanyId = aAppViewFiledSearchFiledMappingEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppViewFiledSearchFiledMappingDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppViewFiledSearchFiledMappingEntity.AppCreatedDate);
                aAppViewFiledSearchFiledMappingDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppViewFiledSearchFiledMappingEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppViewFiledSearchFiledMappingEntity, aAppViewFiledSearchFiledMappingDto);
		}
		
		 /// <summary>
        ///  Copy AppViewFiledSearchFiledMappingDto Properties to   AppViewFiledSearchFiledMappingEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppViewFiledSearchFiledMappingEntity aAppViewFiledSearchFiledMappingEntity,AppViewFiledSearchFiledMappingDto aAppViewFiledSearchFiledMappingDto)
        {        
 
      			aAppViewFiledSearchFiledMappingEntity.SearchViewId = aAppViewFiledSearchFiledMappingDto.SearchViewId;
      			aAppViewFiledSearchFiledMappingEntity.SearchViewFiledId = aAppViewFiledSearchFiledMappingDto.SearchViewFiledId;
      			aAppViewFiledSearchFiledMappingEntity.SearchId = aAppViewFiledSearchFiledMappingDto.SearchId;
      			aAppViewFiledSearchFiledMappingEntity.SearchFieldId = aAppViewFiledSearchFiledMappingDto.SearchFieldId;
 
  
   
    
      			aAppViewFiledSearchFiledMappingEntity.AppCreatedByCompanyId = aAppViewFiledSearchFiledMappingDto.AppCreatedByCompanyId;
			
			if(aAppViewFiledSearchFiledMappingDto.Id == null)
			{
				aAppViewFiledSearchFiledMappingEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppViewFiledSearchFiledMappingEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppViewFiledSearchFiledMappingEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppViewFiledSearchFiledMappingEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppViewFiledSearchFiledMappingEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppViewFiledSearchFiledMappingEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppViewFiledSearchFiledMappingEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppViewFiledSearchFiledMappingEntity, aAppViewFiledSearchFiledMappingDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppViewFiledSearchFiledMappingEntity aAppViewFiledSearchFiledMappingEntity,AppViewFiledSearchFiledMappingDto aAppViewFiledSearchFiledMappingDto);
		
		static partial void OnCopyDtoToEntityDone(AppViewFiledSearchFiledMappingEntity aAppViewFiledSearchFiledMappingEntity,AppViewFiledSearchFiledMappingDto aAppViewFiledSearchFiledMappingDto);
		
   
       
    }
}

 