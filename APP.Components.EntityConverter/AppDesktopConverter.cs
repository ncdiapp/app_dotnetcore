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
    /// Convert Properties between  AppDesktopEntity and  AppDesktopDto
    /// </summary>
    public static partial class AppDesktopConverter 
    {
         /// <summary>
        ///  Convert AppDesktopEntity To  AppDesktopDto
        /// </summary>
        public static AppDesktopDto ConvertEntityToDto(AppDesktopEntity aAppDesktopEntity)
        {        
    		AppDesktopDto aAppDesktopDto = new AppDesktopDto();
    		CopyEntityPropertyToDto( aAppDesktopEntity, aAppDesktopDto);          
			return aAppDesktopDto;
        }
		 /// <summary>
        ///  Convert AppDesktopEntity To  AppDesktopExDto
        /// </summary>
        public static AppDesktopExDto ConvertEntityToExDto(AppDesktopEntity aAppDesktopEntity)
        {        
    		AppDesktopExDto aAppDesktopExDto = new AppDesktopExDto();
			CopyEntityPropertyToDto( aAppDesktopEntity, aAppDesktopExDto);
			
			
			
            return aAppDesktopExDto;
        }
		
		 /// <summary>
        ///  Convert AppDesktopEntity To  AppDesktopDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppDesktopEntity aAppDesktopEntity,AppDesktopDto aAppDesktopDto)
        {        
    		
           // aAppDesktopDto.StopChangeTracking();
 			aAppDesktopDto.Id = aAppDesktopEntity.DesktopId;
 			aAppDesktopDto.DesktopName = aAppDesktopEntity.DesktopName;
 			aAppDesktopDto.Description = aAppDesktopEntity.Description;
 			aAppDesktopDto.IsGlobalDefault = aAppDesktopEntity.IsGlobalDefault;
 			aAppDesktopDto.LayoutType = aAppDesktopEntity.LayoutType;
 			aAppDesktopDto.AppCreatedById = aAppDesktopEntity.AppCreatedById;
 			aAppDesktopDto.AppCreatedDate = aAppDesktopEntity.AppCreatedDate;
 			aAppDesktopDto.AppModifiedDate = aAppDesktopEntity.AppModifiedDate;
 			aAppDesktopDto.AppModifiedById = aAppDesktopEntity.AppModifiedById;
 			aAppDesktopDto.AppCreatedByCompanyId = aAppDesktopEntity.AppCreatedByCompanyId;
 			aAppDesktopDto.SaasApplicationId = aAppDesktopEntity.SaasApplicationId;
 			aAppDesktopDto.OtherSettings = aAppDesktopEntity.OtherSettings;
 			aAppDesktopDto.IsUserDesktop = aAppDesktopEntity.IsUserDesktop;
 			aAppDesktopDto.UserDesktopUserId = aAppDesktopEntity.UserDesktopUserId;
 			aAppDesktopDto.UserFavoriteList = aAppDesktopEntity.UserFavoriteList;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppDesktopDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppDesktopEntity.AppCreatedDate);
                aAppDesktopDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppDesktopEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppDesktopEntity, aAppDesktopDto);
		}
		
		 /// <summary>
        ///  Copy AppDesktopDto Properties to   AppDesktopEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppDesktopEntity aAppDesktopEntity,AppDesktopDto aAppDesktopDto)
        {        
 
      			aAppDesktopEntity.DesktopName = aAppDesktopDto.DesktopName;
      			aAppDesktopEntity.Description = aAppDesktopDto.Description;
      			aAppDesktopEntity.IsGlobalDefault = aAppDesktopDto.IsGlobalDefault;
      			aAppDesktopEntity.LayoutType = aAppDesktopDto.LayoutType;
 
  
   
    
      			aAppDesktopEntity.AppCreatedByCompanyId = aAppDesktopDto.AppCreatedByCompanyId;
      			aAppDesktopEntity.SaasApplicationId = aAppDesktopDto.SaasApplicationId;
      			aAppDesktopEntity.OtherSettings = aAppDesktopDto.OtherSettings;
      			aAppDesktopEntity.IsUserDesktop = aAppDesktopDto.IsUserDesktop;
      			aAppDesktopEntity.UserDesktopUserId = aAppDesktopDto.UserDesktopUserId;
      			aAppDesktopEntity.UserFavoriteList = aAppDesktopDto.UserFavoriteList;
			
			if(aAppDesktopDto.Id == null)
			{
				aAppDesktopEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppDesktopEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppDesktopEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppDesktopEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppDesktopEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppDesktopEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppDesktopEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppDesktopEntity, aAppDesktopDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppDesktopEntity aAppDesktopEntity,AppDesktopDto aAppDesktopDto);
		
		static partial void OnCopyDtoToEntityDone(AppDesktopEntity aAppDesktopEntity,AppDesktopDto aAppDesktopDto);
		
   
       
    }
}

 