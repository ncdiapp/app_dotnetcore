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
    /// Convert Properties between  AppFileEntity and  AppFileDto
    /// </summary>
    public static partial class AppFileConverter 
    {
         /// <summary>
        ///  Convert AppFileEntity To  AppFileDto
        /// </summary>
        public static AppFileDto ConvertEntityToDto(AppFileEntity aAppFileEntity)
        {        
    		AppFileDto aAppFileDto = new AppFileDto();
    		CopyEntityPropertyToDto( aAppFileEntity, aAppFileDto);          
			return aAppFileDto;
        }
		 /// <summary>
        ///  Convert AppFileEntity To  AppFileExDto
        /// </summary>
        public static AppFileExDto ConvertEntityToExDto(AppFileEntity aAppFileEntity)
        {        
    		AppFileExDto aAppFileExDto = new AppFileExDto();
			CopyEntityPropertyToDto( aAppFileEntity, aAppFileExDto);
			
			
			
            return aAppFileExDto;
        }
		
		 /// <summary>
        ///  Convert AppFileEntity To  AppFileDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppFileEntity aAppFileEntity,AppFileDto aAppFileDto)
        {        
    		
           // aAppFileDto.StopChangeTracking();
 			aAppFileDto.Id = aAppFileEntity.FileId;
 			aAppFileDto.FileCode = aAppFileEntity.FileCode;
 			aAppFileDto.Description = aAppFileEntity.Description;
 			aAppFileDto.FolderId = aAppFileEntity.FolderId;
 			aAppFileDto.FileType = aAppFileEntity.FileType;
 			aAppFileDto.Extension = aAppFileEntity.Extension;
 			aAppFileDto.OriginalFilePath = aAppFileEntity.OriginalFilePath;
 			aAppFileDto.ThumbnailFilePath = aAppFileEntity.ThumbnailFilePath;
 			aAppFileDto.RegularImageFilepath = aAppFileEntity.RegularImageFilepath;
 			aAppFileDto.FileContent = aAppFileEntity.FileContent;
 			aAppFileDto.Comments = aAppFileEntity.Comments;
 			aAppFileDto.InitialFileId = aAppFileEntity.InitialFileId;
 			aAppFileDto.CheckoutById = aAppFileEntity.CheckoutById;
 			aAppFileDto.CheckoutDate = aAppFileEntity.CheckoutDate;
 			aAppFileDto.ClientLastWriteTick = aAppFileEntity.ClientLastWriteTick;
 			aAppFileDto.AppCreatedById = aAppFileEntity.AppCreatedById;
 			aAppFileDto.AppCreatedDate = aAppFileEntity.AppCreatedDate;
 			aAppFileDto.AppModifiedDate = aAppFileEntity.AppModifiedDate;
 			aAppFileDto.AppModifiedById = aAppFileEntity.AppModifiedById;
 			aAppFileDto.AppCreatedByCompanyId = aAppFileEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppFileDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppFileEntity.AppCreatedDate);
                aAppFileDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppFileEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppFileEntity, aAppFileDto);
		}
		
		 /// <summary>
        ///  Copy AppFileDto Properties to   AppFileEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppFileEntity aAppFileEntity,AppFileDto aAppFileDto)
        {        
 
      			aAppFileEntity.FileCode = aAppFileDto.FileCode;
      			aAppFileEntity.Description = aAppFileDto.Description;
      			aAppFileEntity.FolderId = aAppFileDto.FolderId;
      			aAppFileEntity.FileType = aAppFileDto.FileType;
      			aAppFileEntity.Extension = aAppFileDto.Extension;
      			aAppFileEntity.OriginalFilePath = aAppFileDto.OriginalFilePath;
      			aAppFileEntity.ThumbnailFilePath = aAppFileDto.ThumbnailFilePath;
      			aAppFileEntity.RegularImageFilepath = aAppFileDto.RegularImageFilepath;
      			aAppFileEntity.FileContent = aAppFileDto.FileContent;
      			aAppFileEntity.Comments = aAppFileDto.Comments;
      			aAppFileEntity.InitialFileId = aAppFileDto.InitialFileId;
      			aAppFileEntity.CheckoutById = aAppFileDto.CheckoutById;
      			aAppFileEntity.CheckoutDate = aAppFileDto.CheckoutDate;
      			aAppFileEntity.ClientLastWriteTick = aAppFileDto.ClientLastWriteTick;
 
  
   
    
      			aAppFileEntity.AppCreatedByCompanyId = aAppFileDto.AppCreatedByCompanyId;
			
			if(aAppFileDto.Id == null)
			{
				aAppFileEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppFileEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppFileEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppFileEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppFileEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppFileEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppFileEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppFileEntity, aAppFileDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppFileEntity aAppFileEntity,AppFileDto aAppFileDto);
		
		static partial void OnCopyDtoToEntityDone(AppFileEntity aAppFileEntity,AppFileDto aAppFileDto);
		
   
       
    }
}

 