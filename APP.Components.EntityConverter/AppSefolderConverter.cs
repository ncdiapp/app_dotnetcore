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
    /// Convert Properties between  AppSefolderEntity and  AppSefolderDto
    /// </summary>
    public static partial class AppSefolderConverter 
    {
         /// <summary>
        ///  Convert AppSefolderEntity To  AppSefolderDto
        /// </summary>
        public static AppSefolderDto ConvertEntityToDto(AppSefolderEntity aAppSefolderEntity)
        {        
    		AppSefolderDto aAppSefolderDto = new AppSefolderDto();
    		CopyEntityPropertyToDto( aAppSefolderEntity, aAppSefolderDto);          
			return aAppSefolderDto;
        }
		 /// <summary>
        ///  Convert AppSefolderEntity To  AppSefolderExDto
        /// </summary>
        public static AppSefolderExDto ConvertEntityToExDto(AppSefolderEntity aAppSefolderEntity)
        {        
    		AppSefolderExDto aAppSefolderExDto = new AppSefolderExDto();
			CopyEntityPropertyToDto( aAppSefolderEntity, aAppSefolderExDto);
			
			
			
            return aAppSefolderExDto;
        }
		
		 /// <summary>
        ///  Convert AppSefolderEntity To  AppSefolderDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSefolderEntity aAppSefolderEntity,AppSefolderDto aAppSefolderDto)
        {        
    		
           // aAppSefolderDto.StopChangeTracking();
 			aAppSefolderDto.Id = aAppSefolderEntity.FolderId;
 			aAppSefolderDto.FolderType = aAppSefolderEntity.FolderType;
 			aAppSefolderDto.Name = aAppSefolderEntity.Name;
 			aAppSefolderDto.Description = aAppSefolderEntity.Description;
 			aAppSefolderDto.ParentId = aAppSefolderEntity.ParentId;
 			aAppSefolderDto.IsSystemFolder = aAppSefolderEntity.IsSystemFolder;
 			aAppSefolderDto.DefaultViewId = aAppSefolderEntity.DefaultViewId;
 			aAppSefolderDto.TransactionId = aAppSefolderEntity.TransactionId;
 			aAppSefolderDto.AppCreatedById = aAppSefolderEntity.AppCreatedById;
 			aAppSefolderDto.AppCreatedDate = aAppSefolderEntity.AppCreatedDate;
 			aAppSefolderDto.AppModifiedDate = aAppSefolderEntity.AppModifiedDate;
 			aAppSefolderDto.AppModifiedById = aAppSefolderEntity.AppModifiedById;
 			aAppSefolderDto.AppCreatedByCompanyId = aAppSefolderEntity.AppCreatedByCompanyId;
 			aAppSefolderDto.OtherSettings = aAppSefolderEntity.OtherSettings;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSefolderDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSefolderEntity.AppCreatedDate);
                aAppSefolderDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSefolderEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSefolderEntity, aAppSefolderDto);
		}
		
		 /// <summary>
        ///  Copy AppSefolderDto Properties to   AppSefolderEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSefolderEntity aAppSefolderEntity,AppSefolderDto aAppSefolderDto)
        {        
 
      			aAppSefolderEntity.FolderType = aAppSefolderDto.FolderType;
      			aAppSefolderEntity.Name = aAppSefolderDto.Name;
      			aAppSefolderEntity.Description = aAppSefolderDto.Description;
      			aAppSefolderEntity.ParentId = aAppSefolderDto.ParentId;
      			aAppSefolderEntity.IsSystemFolder = aAppSefolderDto.IsSystemFolder;
      			aAppSefolderEntity.DefaultViewId = aAppSefolderDto.DefaultViewId;
      			aAppSefolderEntity.TransactionId = aAppSefolderDto.TransactionId;
 
  
   
    
      			aAppSefolderEntity.AppCreatedByCompanyId = aAppSefolderDto.AppCreatedByCompanyId;
      			aAppSefolderEntity.OtherSettings = aAppSefolderDto.OtherSettings;
			
			if(aAppSefolderDto.Id == null)
			{
				aAppSefolderEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSefolderEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSefolderEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSefolderEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSefolderEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSefolderEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSefolderEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSefolderEntity, aAppSefolderDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSefolderEntity aAppSefolderEntity,AppSefolderDto aAppSefolderDto);
		
		static partial void OnCopyDtoToEntityDone(AppSefolderEntity aAppSefolderEntity,AppSefolderDto aAppSefolderDto);
		
   
       
    }
}

 