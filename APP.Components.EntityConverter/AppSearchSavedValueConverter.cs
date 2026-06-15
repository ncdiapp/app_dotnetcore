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
    /// Convert Properties between  AppSearchSavedValueEntity and  AppSearchSavedValueDto
    /// </summary>
    public static partial class AppSearchSavedValueConverter 
    {
         /// <summary>
        ///  Convert AppSearchSavedValueEntity To  AppSearchSavedValueDto
        /// </summary>
        public static AppSearchSavedValueDto ConvertEntityToDto(AppSearchSavedValueEntity aAppSearchSavedValueEntity)
        {        
    		AppSearchSavedValueDto aAppSearchSavedValueDto = new AppSearchSavedValueDto();
    		CopyEntityPropertyToDto( aAppSearchSavedValueEntity, aAppSearchSavedValueDto);          
			return aAppSearchSavedValueDto;
        }
		 /// <summary>
        ///  Convert AppSearchSavedValueEntity To  AppSearchSavedValueExDto
        /// </summary>
        public static AppSearchSavedValueExDto ConvertEntityToExDto(AppSearchSavedValueEntity aAppSearchSavedValueEntity)
        {        
    		AppSearchSavedValueExDto aAppSearchSavedValueExDto = new AppSearchSavedValueExDto();
			CopyEntityPropertyToDto( aAppSearchSavedValueEntity, aAppSearchSavedValueExDto);
			
			
			
            return aAppSearchSavedValueExDto;
        }
		
		 /// <summary>
        ///  Convert AppSearchSavedValueEntity To  AppSearchSavedValueDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSearchSavedValueEntity aAppSearchSavedValueEntity,AppSearchSavedValueDto aAppSearchSavedValueDto)
        {        
    		
           // aAppSearchSavedValueDto.StopChangeTracking();
 			aAppSearchSavedValueDto.Id = aAppSearchSavedValueEntity.SearchSavedValueId;
 			aAppSearchSavedValueDto.SearchSavedId = aAppSearchSavedValueEntity.SearchSavedId;
 			aAppSearchSavedValueDto.SearchFieldId = aAppSearchSavedValueEntity.SearchFieldId;
 			aAppSearchSavedValueDto.SearchValue = aAppSearchSavedValueEntity.SearchValue;
 			aAppSearchSavedValueDto.OperationId = aAppSearchSavedValueEntity.OperationId;
 			aAppSearchSavedValueDto.AppCreatedById = aAppSearchSavedValueEntity.AppCreatedById;
 			aAppSearchSavedValueDto.AppCreatedDate = aAppSearchSavedValueEntity.AppCreatedDate;
 			aAppSearchSavedValueDto.AppModifiedDate = aAppSearchSavedValueEntity.AppModifiedDate;
 			aAppSearchSavedValueDto.AppModifiedById = aAppSearchSavedValueEntity.AppModifiedById;
 			aAppSearchSavedValueDto.AppCreatedByCompanyId = aAppSearchSavedValueEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSearchSavedValueDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSearchSavedValueEntity.AppCreatedDate);
                aAppSearchSavedValueDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSearchSavedValueEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSearchSavedValueEntity, aAppSearchSavedValueDto);
		}
		
		 /// <summary>
        ///  Copy AppSearchSavedValueDto Properties to   AppSearchSavedValueEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSearchSavedValueEntity aAppSearchSavedValueEntity,AppSearchSavedValueDto aAppSearchSavedValueDto)
        {        
 
      			aAppSearchSavedValueEntity.SearchSavedId = aAppSearchSavedValueDto.SearchSavedId;
      			aAppSearchSavedValueEntity.SearchFieldId = aAppSearchSavedValueDto.SearchFieldId;
      			aAppSearchSavedValueEntity.SearchValue = aAppSearchSavedValueDto.SearchValue;
      			aAppSearchSavedValueEntity.OperationId = aAppSearchSavedValueDto.OperationId;
 
  
   
    
      			aAppSearchSavedValueEntity.AppCreatedByCompanyId = aAppSearchSavedValueDto.AppCreatedByCompanyId;
			
			if(aAppSearchSavedValueDto.Id == null)
			{
				aAppSearchSavedValueEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSearchSavedValueEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSearchSavedValueEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSearchSavedValueEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSearchSavedValueEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSearchSavedValueEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSearchSavedValueEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSearchSavedValueEntity, aAppSearchSavedValueDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSearchSavedValueEntity aAppSearchSavedValueEntity,AppSearchSavedValueDto aAppSearchSavedValueDto);
		
		static partial void OnCopyDtoToEntityDone(AppSearchSavedValueEntity aAppSearchSavedValueEntity,AppSearchSavedValueDto aAppSearchSavedValueDto);
		
   
       
    }
}

 