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
    /// Convert Properties between  AppCurrentUserFavouriteFolderOrFileEntity and  AppCurrentUserFavouriteFolderOrFileDto
    /// </summary>
    public static partial class AppCurrentUserFavouriteFolderOrFileConverter 
    {
         /// <summary>
        ///  Convert AppCurrentUserFavouriteFolderOrFileEntity To  AppCurrentUserFavouriteFolderOrFileDto
        /// </summary>
        public static AppCurrentUserFavouriteFolderOrFileDto ConvertEntityToDto(AppCurrentUserFavouriteFolderOrFileEntity aAppCurrentUserFavouriteFolderOrFileEntity)
        {        
    		AppCurrentUserFavouriteFolderOrFileDto aAppCurrentUserFavouriteFolderOrFileDto = new AppCurrentUserFavouriteFolderOrFileDto();
    		CopyEntityPropertyToDto( aAppCurrentUserFavouriteFolderOrFileEntity, aAppCurrentUserFavouriteFolderOrFileDto);          
			return aAppCurrentUserFavouriteFolderOrFileDto;
        }
		 /// <summary>
        ///  Convert AppCurrentUserFavouriteFolderOrFileEntity To  AppCurrentUserFavouriteFolderOrFileExDto
        /// </summary>
        public static AppCurrentUserFavouriteFolderOrFileExDto ConvertEntityToExDto(AppCurrentUserFavouriteFolderOrFileEntity aAppCurrentUserFavouriteFolderOrFileEntity)
        {        
    		AppCurrentUserFavouriteFolderOrFileExDto aAppCurrentUserFavouriteFolderOrFileExDto = new AppCurrentUserFavouriteFolderOrFileExDto();
			CopyEntityPropertyToDto( aAppCurrentUserFavouriteFolderOrFileEntity, aAppCurrentUserFavouriteFolderOrFileExDto);
			
			
			
            return aAppCurrentUserFavouriteFolderOrFileExDto;
        }
		
		 /// <summary>
        ///  Convert AppCurrentUserFavouriteFolderOrFileEntity To  AppCurrentUserFavouriteFolderOrFileDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppCurrentUserFavouriteFolderOrFileEntity aAppCurrentUserFavouriteFolderOrFileEntity,AppCurrentUserFavouriteFolderOrFileDto aAppCurrentUserFavouriteFolderOrFileDto)
        {        
    		
           // aAppCurrentUserFavouriteFolderOrFileDto.StopChangeTracking();
 			aAppCurrentUserFavouriteFolderOrFileDto.Id = aAppCurrentUserFavouriteFolderOrFileEntity.FavouriteFileId;
 			aAppCurrentUserFavouriteFolderOrFileDto.CurrentUserId = aAppCurrentUserFavouriteFolderOrFileEntity.CurrentUserId;
 			aAppCurrentUserFavouriteFolderOrFileDto.FiledId = aAppCurrentUserFavouriteFolderOrFileEntity.FiledId;
 			aAppCurrentUserFavouriteFolderOrFileDto.FolderId = aAppCurrentUserFavouriteFolderOrFileEntity.FolderId;
 			aAppCurrentUserFavouriteFolderOrFileDto.AppCreatedById = aAppCurrentUserFavouriteFolderOrFileEntity.AppCreatedById;
 			aAppCurrentUserFavouriteFolderOrFileDto.AppCreatedDate = aAppCurrentUserFavouriteFolderOrFileEntity.AppCreatedDate;
 			aAppCurrentUserFavouriteFolderOrFileDto.AppModifiedDate = aAppCurrentUserFavouriteFolderOrFileEntity.AppModifiedDate;
 			aAppCurrentUserFavouriteFolderOrFileDto.AppModifiedById = aAppCurrentUserFavouriteFolderOrFileEntity.AppModifiedById;
 			aAppCurrentUserFavouriteFolderOrFileDto.AppCreatedByCompanyId = aAppCurrentUserFavouriteFolderOrFileEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppCurrentUserFavouriteFolderOrFileDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCurrentUserFavouriteFolderOrFileEntity.AppCreatedDate);
                aAppCurrentUserFavouriteFolderOrFileDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCurrentUserFavouriteFolderOrFileEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppCurrentUserFavouriteFolderOrFileEntity, aAppCurrentUserFavouriteFolderOrFileDto);
		}
		
		 /// <summary>
        ///  Copy AppCurrentUserFavouriteFolderOrFileDto Properties to   AppCurrentUserFavouriteFolderOrFileEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppCurrentUserFavouriteFolderOrFileEntity aAppCurrentUserFavouriteFolderOrFileEntity,AppCurrentUserFavouriteFolderOrFileDto aAppCurrentUserFavouriteFolderOrFileDto)
        {        
 
      			aAppCurrentUserFavouriteFolderOrFileEntity.CurrentUserId = aAppCurrentUserFavouriteFolderOrFileDto.CurrentUserId;
      			aAppCurrentUserFavouriteFolderOrFileEntity.FiledId = aAppCurrentUserFavouriteFolderOrFileDto.FiledId;
      			aAppCurrentUserFavouriteFolderOrFileEntity.FolderId = aAppCurrentUserFavouriteFolderOrFileDto.FolderId;
 
  
   
    
      			aAppCurrentUserFavouriteFolderOrFileEntity.AppCreatedByCompanyId = aAppCurrentUserFavouriteFolderOrFileDto.AppCreatedByCompanyId;
			
			if(aAppCurrentUserFavouriteFolderOrFileDto.Id == null)
			{
				aAppCurrentUserFavouriteFolderOrFileEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppCurrentUserFavouriteFolderOrFileEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppCurrentUserFavouriteFolderOrFileEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppCurrentUserFavouriteFolderOrFileEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppCurrentUserFavouriteFolderOrFileEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppCurrentUserFavouriteFolderOrFileEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppCurrentUserFavouriteFolderOrFileEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppCurrentUserFavouriteFolderOrFileEntity, aAppCurrentUserFavouriteFolderOrFileDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppCurrentUserFavouriteFolderOrFileEntity aAppCurrentUserFavouriteFolderOrFileEntity,AppCurrentUserFavouriteFolderOrFileDto aAppCurrentUserFavouriteFolderOrFileDto);
		
		static partial void OnCopyDtoToEntityDone(AppCurrentUserFavouriteFolderOrFileEntity aAppCurrentUserFavouriteFolderOrFileEntity,AppCurrentUserFavouriteFolderOrFileDto aAppCurrentUserFavouriteFolderOrFileDto);
		
   
       
    }
}

 