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
    /// Convert Properties between  AppSefolderResourceEntity and  AppSefolderResourceDto
    /// </summary>
    public static partial class AppSefolderResourceConverter 
    {
         /// <summary>
        ///  Convert AppSefolderResourceEntity To  AppSefolderResourceDto
        /// </summary>
        public static AppSefolderResourceDto ConvertEntityToDto(AppSefolderResourceEntity aAppSefolderResourceEntity)
        {        
    		AppSefolderResourceDto aAppSefolderResourceDto = new AppSefolderResourceDto();
    		CopyEntityPropertyToDto( aAppSefolderResourceEntity, aAppSefolderResourceDto);          
			return aAppSefolderResourceDto;
        }
		 /// <summary>
        ///  Convert AppSefolderResourceEntity To  AppSefolderResourceExDto
        /// </summary>
        public static AppSefolderResourceExDto ConvertEntityToExDto(AppSefolderResourceEntity aAppSefolderResourceEntity)
        {        
    		AppSefolderResourceExDto aAppSefolderResourceExDto = new AppSefolderResourceExDto();
			CopyEntityPropertyToDto( aAppSefolderResourceEntity, aAppSefolderResourceExDto);
			
			
			
            return aAppSefolderResourceExDto;
        }
		
		 /// <summary>
        ///  Convert AppSefolderResourceEntity To  AppSefolderResourceDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSefolderResourceEntity aAppSefolderResourceEntity,AppSefolderResourceDto aAppSefolderResourceDto)
        {        
    		
           // aAppSefolderResourceDto.StopChangeTracking();
 			aAppSefolderResourceDto.Id = aAppSefolderResourceEntity.FolderResourceId;
 			aAppSefolderResourceDto.UserId = aAppSefolderResourceEntity.UserId;
 			aAppSefolderResourceDto.RoleId = aAppSefolderResourceEntity.RoleId;
 			aAppSefolderResourceDto.FolderId = aAppSefolderResourceEntity.FolderId;
 			aAppSefolderResourceDto.IsReadOnly = aAppSefolderResourceEntity.IsReadOnly;
 			aAppSefolderResourceDto.IsAllowedToEditSecurity = aAppSefolderResourceEntity.IsAllowedToEditSecurity;
 			aAppSefolderResourceDto.SystemTimeStamp = aAppSefolderResourceEntity.SystemTimeStamp;
 			aAppSefolderResourceDto.AppCreatedById = aAppSefolderResourceEntity.AppCreatedById;
 			aAppSefolderResourceDto.AppCreatedDate = aAppSefolderResourceEntity.AppCreatedDate;
 			aAppSefolderResourceDto.AppModifiedDate = aAppSefolderResourceEntity.AppModifiedDate;
 			aAppSefolderResourceDto.AppModifiedById = aAppSefolderResourceEntity.AppModifiedById;
 			aAppSefolderResourceDto.AppCreatedByCompanyId = aAppSefolderResourceEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSefolderResourceDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSefolderResourceEntity.AppCreatedDate);
                aAppSefolderResourceDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSefolderResourceEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSefolderResourceEntity, aAppSefolderResourceDto);
		}
		
		 /// <summary>
        ///  Copy AppSefolderResourceDto Properties to   AppSefolderResourceEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSefolderResourceEntity aAppSefolderResourceEntity,AppSefolderResourceDto aAppSefolderResourceDto)
        {        
 
      			aAppSefolderResourceEntity.UserId = aAppSefolderResourceDto.UserId;
      			aAppSefolderResourceEntity.RoleId = aAppSefolderResourceDto.RoleId;
      			aAppSefolderResourceEntity.FolderId = aAppSefolderResourceDto.FolderId;
      			aAppSefolderResourceEntity.IsReadOnly = aAppSefolderResourceDto.IsReadOnly;
      			aAppSefolderResourceEntity.IsAllowedToEditSecurity = aAppSefolderResourceDto.IsAllowedToEditSecurity;
 
 
  
   
    
      			aAppSefolderResourceEntity.AppCreatedByCompanyId = aAppSefolderResourceDto.AppCreatedByCompanyId;
			
			if(aAppSefolderResourceDto.Id == null)
			{
				aAppSefolderResourceEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSefolderResourceEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSefolderResourceEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSefolderResourceEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSefolderResourceEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSefolderResourceEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSefolderResourceEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSefolderResourceEntity, aAppSefolderResourceDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSefolderResourceEntity aAppSefolderResourceEntity,AppSefolderResourceDto aAppSefolderResourceDto);
		
		static partial void OnCopyDtoToEntityDone(AppSefolderResourceEntity aAppSefolderResourceEntity,AppSefolderResourceDto aAppSefolderResourceDto);
		
   
       
    }
}

 