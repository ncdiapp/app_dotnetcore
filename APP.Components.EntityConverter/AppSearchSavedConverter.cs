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
    /// Convert Properties between  AppSearchSavedEntity and  AppSearchSavedDto
    /// </summary>
    public static partial class AppSearchSavedConverter 
    {
         /// <summary>
        ///  Convert AppSearchSavedEntity To  AppSearchSavedDto
        /// </summary>
        public static AppSearchSavedDto ConvertEntityToDto(AppSearchSavedEntity aAppSearchSavedEntity)
        {        
    		AppSearchSavedDto aAppSearchSavedDto = new AppSearchSavedDto();
    		CopyEntityPropertyToDto( aAppSearchSavedEntity, aAppSearchSavedDto);          
			return aAppSearchSavedDto;
        }
		 /// <summary>
        ///  Convert AppSearchSavedEntity To  AppSearchSavedExDto
        /// </summary>
        public static AppSearchSavedExDto ConvertEntityToExDto(AppSearchSavedEntity aAppSearchSavedEntity)
        {        
    		AppSearchSavedExDto aAppSearchSavedExDto = new AppSearchSavedExDto();
			CopyEntityPropertyToDto( aAppSearchSavedEntity, aAppSearchSavedExDto);
			
			
			
            return aAppSearchSavedExDto;
        }
		
		 /// <summary>
        ///  Convert AppSearchSavedEntity To  AppSearchSavedDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSearchSavedEntity aAppSearchSavedEntity,AppSearchSavedDto aAppSearchSavedDto)
        {        
    		
           // aAppSearchSavedDto.StopChangeTracking();
 			aAppSearchSavedDto.Id = aAppSearchSavedEntity.SearchSavedId;
 			aAppSearchSavedDto.SearchId = aAppSearchSavedEntity.SearchId;
 			aAppSearchSavedDto.SavedSearchName = aAppSearchSavedEntity.SavedSearchName;
 			aAppSearchSavedDto.UserId = aAppSearchSavedEntity.UserId;
 			aAppSearchSavedDto.SystemTimeStamp = aAppSearchSavedEntity.SystemTimeStamp;
 			aAppSearchSavedDto.IsDefault = aAppSearchSavedEntity.IsDefault;
 			aAppSearchSavedDto.IsAutoExecute = aAppSearchSavedEntity.IsAutoExecute;
 			aAppSearchSavedDto.GroupId = aAppSearchSavedEntity.GroupId;
 			aAppSearchSavedDto.DefaultSearchViewId = aAppSearchSavedEntity.DefaultSearchViewId;
 			aAppSearchSavedDto.AppCreatedById = aAppSearchSavedEntity.AppCreatedById;
 			aAppSearchSavedDto.AppCreatedDate = aAppSearchSavedEntity.AppCreatedDate;
 			aAppSearchSavedDto.AppModifiedDate = aAppSearchSavedEntity.AppModifiedDate;
 			aAppSearchSavedDto.AppModifiedById = aAppSearchSavedEntity.AppModifiedById;
 			aAppSearchSavedDto.AppCreatedByCompanyId = aAppSearchSavedEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSearchSavedDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSearchSavedEntity.AppCreatedDate);
                aAppSearchSavedDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSearchSavedEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSearchSavedEntity, aAppSearchSavedDto);
		}
		
		 /// <summary>
        ///  Copy AppSearchSavedDto Properties to   AppSearchSavedEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSearchSavedEntity aAppSearchSavedEntity,AppSearchSavedDto aAppSearchSavedDto)
        {        
 
      			aAppSearchSavedEntity.SearchId = aAppSearchSavedDto.SearchId;
      			aAppSearchSavedEntity.SavedSearchName = aAppSearchSavedDto.SavedSearchName;
      			aAppSearchSavedEntity.UserId = aAppSearchSavedDto.UserId;
 
      			aAppSearchSavedEntity.IsDefault = aAppSearchSavedDto.IsDefault;
      			aAppSearchSavedEntity.IsAutoExecute = aAppSearchSavedDto.IsAutoExecute;
      			aAppSearchSavedEntity.GroupId = aAppSearchSavedDto.GroupId;
      			aAppSearchSavedEntity.DefaultSearchViewId = aAppSearchSavedDto.DefaultSearchViewId;
 
  
   
    
      			aAppSearchSavedEntity.AppCreatedByCompanyId = aAppSearchSavedDto.AppCreatedByCompanyId;
			
			if(aAppSearchSavedDto.Id == null)
			{
				aAppSearchSavedEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSearchSavedEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSearchSavedEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSearchSavedEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSearchSavedEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSearchSavedEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSearchSavedEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSearchSavedEntity, aAppSearchSavedDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSearchSavedEntity aAppSearchSavedEntity,AppSearchSavedDto aAppSearchSavedDto);
		
		static partial void OnCopyDtoToEntityDone(AppSearchSavedEntity aAppSearchSavedEntity,AppSearchSavedDto aAppSearchSavedDto);
		
   
       
    }
}

 