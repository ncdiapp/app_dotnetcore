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
    /// Convert Properties between  AppFileOrFolderShareToOtherEntity and  AppFileOrFolderShareToOtherDto
    /// </summary>
    public static partial class AppFileOrFolderShareToOtherConverter 
    {
         /// <summary>
        ///  Convert AppFileOrFolderShareToOtherEntity To  AppFileOrFolderShareToOtherDto
        /// </summary>
        public static AppFileOrFolderShareToOtherDto ConvertEntityToDto(AppFileOrFolderShareToOtherEntity aAppFileOrFolderShareToOtherEntity)
        {        
    		AppFileOrFolderShareToOtherDto aAppFileOrFolderShareToOtherDto = new AppFileOrFolderShareToOtherDto();
    		CopyEntityPropertyToDto( aAppFileOrFolderShareToOtherEntity, aAppFileOrFolderShareToOtherDto);          
			return aAppFileOrFolderShareToOtherDto;
        }
		 /// <summary>
        ///  Convert AppFileOrFolderShareToOtherEntity To  AppFileOrFolderShareToOtherExDto
        /// </summary>
        public static AppFileOrFolderShareToOtherExDto ConvertEntityToExDto(AppFileOrFolderShareToOtherEntity aAppFileOrFolderShareToOtherEntity)
        {        
    		AppFileOrFolderShareToOtherExDto aAppFileOrFolderShareToOtherExDto = new AppFileOrFolderShareToOtherExDto();
			CopyEntityPropertyToDto( aAppFileOrFolderShareToOtherEntity, aAppFileOrFolderShareToOtherExDto);
			
			
			
            return aAppFileOrFolderShareToOtherExDto;
        }
		
		 /// <summary>
        ///  Convert AppFileOrFolderShareToOtherEntity To  AppFileOrFolderShareToOtherDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppFileOrFolderShareToOtherEntity aAppFileOrFolderShareToOtherEntity,AppFileOrFolderShareToOtherDto aAppFileOrFolderShareToOtherDto)
        {        
    		
           // aAppFileOrFolderShareToOtherDto.StopChangeTracking();
 			aAppFileOrFolderShareToOtherDto.Id = aAppFileOrFolderShareToOtherEntity.SharingId;
 			aAppFileOrFolderShareToOtherDto.FolderId = aAppFileOrFolderShareToOtherEntity.FolderId;
 			aAppFileOrFolderShareToOtherDto.FileId = aAppFileOrFolderShareToOtherEntity.FileId;
 			aAppFileOrFolderShareToOtherDto.ShareToOtherUserId = aAppFileOrFolderShareToOtherEntity.ShareToOtherUserId;
 			aAppFileOrFolderShareToOtherDto.ShareToOtherRoleId = aAppFileOrFolderShareToOtherEntity.ShareToOtherRoleId;
 			aAppFileOrFolderShareToOtherDto.IsCanWrite = aAppFileOrFolderShareToOtherEntity.IsCanWrite;
 			aAppFileOrFolderShareToOtherDto.AppCreatedById = aAppFileOrFolderShareToOtherEntity.AppCreatedById;
 			aAppFileOrFolderShareToOtherDto.AppCreatedDate = aAppFileOrFolderShareToOtherEntity.AppCreatedDate;
 			aAppFileOrFolderShareToOtherDto.AppModifiedDate = aAppFileOrFolderShareToOtherEntity.AppModifiedDate;
 			aAppFileOrFolderShareToOtherDto.AppModifiedById = aAppFileOrFolderShareToOtherEntity.AppModifiedById;
 			aAppFileOrFolderShareToOtherDto.AppCreatedByCompanyId = aAppFileOrFolderShareToOtherEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppFileOrFolderShareToOtherDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppFileOrFolderShareToOtherEntity.AppCreatedDate);
                aAppFileOrFolderShareToOtherDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppFileOrFolderShareToOtherEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppFileOrFolderShareToOtherEntity, aAppFileOrFolderShareToOtherDto);
		}
		
		 /// <summary>
        ///  Copy AppFileOrFolderShareToOtherDto Properties to   AppFileOrFolderShareToOtherEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppFileOrFolderShareToOtherEntity aAppFileOrFolderShareToOtherEntity,AppFileOrFolderShareToOtherDto aAppFileOrFolderShareToOtherDto)
        {        
 
      			aAppFileOrFolderShareToOtherEntity.FolderId = aAppFileOrFolderShareToOtherDto.FolderId;
      			aAppFileOrFolderShareToOtherEntity.FileId = aAppFileOrFolderShareToOtherDto.FileId;
      			aAppFileOrFolderShareToOtherEntity.ShareToOtherUserId = aAppFileOrFolderShareToOtherDto.ShareToOtherUserId;
      			aAppFileOrFolderShareToOtherEntity.ShareToOtherRoleId = aAppFileOrFolderShareToOtherDto.ShareToOtherRoleId;
      			aAppFileOrFolderShareToOtherEntity.IsCanWrite = aAppFileOrFolderShareToOtherDto.IsCanWrite;
 
  
   
    
      			aAppFileOrFolderShareToOtherEntity.AppCreatedByCompanyId = aAppFileOrFolderShareToOtherDto.AppCreatedByCompanyId;
			
			if(aAppFileOrFolderShareToOtherDto.Id == null)
			{
				aAppFileOrFolderShareToOtherEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppFileOrFolderShareToOtherEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppFileOrFolderShareToOtherEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppFileOrFolderShareToOtherEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppFileOrFolderShareToOtherEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppFileOrFolderShareToOtherEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppFileOrFolderShareToOtherEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppFileOrFolderShareToOtherEntity, aAppFileOrFolderShareToOtherDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppFileOrFolderShareToOtherEntity aAppFileOrFolderShareToOtherEntity,AppFileOrFolderShareToOtherDto aAppFileOrFolderShareToOtherDto);
		
		static partial void OnCopyDtoToEntityDone(AppFileOrFolderShareToOtherEntity aAppFileOrFolderShareToOtherEntity,AppFileOrFolderShareToOtherDto aAppFileOrFolderShareToOtherDto);
		
   
       
    }
}

 