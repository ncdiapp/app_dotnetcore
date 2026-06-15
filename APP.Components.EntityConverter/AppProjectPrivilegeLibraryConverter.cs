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
    /// Convert Properties between  AppProjectPrivilegeLibraryEntity and  AppProjectPrivilegeLibraryDto
    /// </summary>
    public static partial class AppProjectPrivilegeLibraryConverter 
    {
         /// <summary>
        ///  Convert AppProjectPrivilegeLibraryEntity To  AppProjectPrivilegeLibraryDto
        /// </summary>
        public static AppProjectPrivilegeLibraryDto ConvertEntityToDto(AppProjectPrivilegeLibraryEntity aAppProjectPrivilegeLibraryEntity)
        {        
    		AppProjectPrivilegeLibraryDto aAppProjectPrivilegeLibraryDto = new AppProjectPrivilegeLibraryDto();
    		CopyEntityPropertyToDto( aAppProjectPrivilegeLibraryEntity, aAppProjectPrivilegeLibraryDto);          
			return aAppProjectPrivilegeLibraryDto;
        }
		 /// <summary>
        ///  Convert AppProjectPrivilegeLibraryEntity To  AppProjectPrivilegeLibraryExDto
        /// </summary>
        public static AppProjectPrivilegeLibraryExDto ConvertEntityToExDto(AppProjectPrivilegeLibraryEntity aAppProjectPrivilegeLibraryEntity)
        {        
    		AppProjectPrivilegeLibraryExDto aAppProjectPrivilegeLibraryExDto = new AppProjectPrivilegeLibraryExDto();
			CopyEntityPropertyToDto( aAppProjectPrivilegeLibraryEntity, aAppProjectPrivilegeLibraryExDto);
			
			
			
            return aAppProjectPrivilegeLibraryExDto;
        }
		
		 /// <summary>
        ///  Convert AppProjectPrivilegeLibraryEntity To  AppProjectPrivilegeLibraryDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppProjectPrivilegeLibraryEntity aAppProjectPrivilegeLibraryEntity,AppProjectPrivilegeLibraryDto aAppProjectPrivilegeLibraryDto)
        {        
    		
           // aAppProjectPrivilegeLibraryDto.StopChangeTracking();
 			aAppProjectPrivilegeLibraryDto.Id = aAppProjectPrivilegeLibraryEntity.ProjectPrivilegeId;
 			aAppProjectPrivilegeLibraryDto.EmAppProjectPrivilegeCode = aAppProjectPrivilegeLibraryEntity.EmAppProjectPrivilegeCode;
 			aAppProjectPrivilegeLibraryDto.Description = aAppProjectPrivilegeLibraryEntity.Description;
 			aAppProjectPrivilegeLibraryDto.AppCreatedById = aAppProjectPrivilegeLibraryEntity.AppCreatedById;
 			aAppProjectPrivilegeLibraryDto.AppCreatedDate = aAppProjectPrivilegeLibraryEntity.AppCreatedDate;
 			aAppProjectPrivilegeLibraryDto.AppModifiedDate = aAppProjectPrivilegeLibraryEntity.AppModifiedDate;
 			aAppProjectPrivilegeLibraryDto.AppModifiedById = aAppProjectPrivilegeLibraryEntity.AppModifiedById;
 			aAppProjectPrivilegeLibraryDto.AppCreatedByCompanyId = aAppProjectPrivilegeLibraryEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppProjectPrivilegeLibraryDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectPrivilegeLibraryEntity.AppCreatedDate);
                aAppProjectPrivilegeLibraryDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectPrivilegeLibraryEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppProjectPrivilegeLibraryEntity, aAppProjectPrivilegeLibraryDto);
		}
		
		 /// <summary>
        ///  Copy AppProjectPrivilegeLibraryDto Properties to   AppProjectPrivilegeLibraryEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppProjectPrivilegeLibraryEntity aAppProjectPrivilegeLibraryEntity,AppProjectPrivilegeLibraryDto aAppProjectPrivilegeLibraryDto)
        {        
 
      			aAppProjectPrivilegeLibraryEntity.EmAppProjectPrivilegeCode = aAppProjectPrivilegeLibraryDto.EmAppProjectPrivilegeCode;
      			aAppProjectPrivilegeLibraryEntity.Description = aAppProjectPrivilegeLibraryDto.Description;
 
  
   
    
      			aAppProjectPrivilegeLibraryEntity.AppCreatedByCompanyId = aAppProjectPrivilegeLibraryDto.AppCreatedByCompanyId;
			
			if(aAppProjectPrivilegeLibraryDto.Id == null)
			{
				aAppProjectPrivilegeLibraryEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectPrivilegeLibraryEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppProjectPrivilegeLibraryEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectPrivilegeLibraryEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectPrivilegeLibraryEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppProjectPrivilegeLibraryEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectPrivilegeLibraryEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppProjectPrivilegeLibraryEntity, aAppProjectPrivilegeLibraryDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppProjectPrivilegeLibraryEntity aAppProjectPrivilegeLibraryEntity,AppProjectPrivilegeLibraryDto aAppProjectPrivilegeLibraryDto);
		
		static partial void OnCopyDtoToEntityDone(AppProjectPrivilegeLibraryEntity aAppProjectPrivilegeLibraryEntity,AppProjectPrivilegeLibraryDto aAppProjectPrivilegeLibraryDto);
		
   
       
    }
}

 