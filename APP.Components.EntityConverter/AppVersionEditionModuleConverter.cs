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
    /// Convert Properties between  AppVersionEditionModuleEntity and  AppVersionEditionModuleDto
    /// </summary>
    public static partial class AppVersionEditionModuleConverter 
    {
         /// <summary>
        ///  Convert AppVersionEditionModuleEntity To  AppVersionEditionModuleDto
        /// </summary>
        public static AppVersionEditionModuleDto ConvertEntityToDto(AppVersionEditionModuleEntity aAppVersionEditionModuleEntity)
        {        
    		AppVersionEditionModuleDto aAppVersionEditionModuleDto = new AppVersionEditionModuleDto();
    		CopyEntityPropertyToDto( aAppVersionEditionModuleEntity, aAppVersionEditionModuleDto);          
			return aAppVersionEditionModuleDto;
        }
		 /// <summary>
        ///  Convert AppVersionEditionModuleEntity To  AppVersionEditionModuleExDto
        /// </summary>
        public static AppVersionEditionModuleExDto ConvertEntityToExDto(AppVersionEditionModuleEntity aAppVersionEditionModuleEntity)
        {        
    		AppVersionEditionModuleExDto aAppVersionEditionModuleExDto = new AppVersionEditionModuleExDto();
			CopyEntityPropertyToDto( aAppVersionEditionModuleEntity, aAppVersionEditionModuleExDto);
			
			
			
            return aAppVersionEditionModuleExDto;
        }
		
		 /// <summary>
        ///  Convert AppVersionEditionModuleEntity To  AppVersionEditionModuleDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppVersionEditionModuleEntity aAppVersionEditionModuleEntity,AppVersionEditionModuleDto aAppVersionEditionModuleDto)
        {        
    		
           // aAppVersionEditionModuleDto.StopChangeTracking();
 			aAppVersionEditionModuleDto.Id = aAppVersionEditionModuleEntity.EditionModuleItemId;
 			aAppVersionEditionModuleDto.MenuId = aAppVersionEditionModuleEntity.MenuId;
 			aAppVersionEditionModuleDto.EmApplicationVersionEdition = aAppVersionEditionModuleEntity.EmApplicationVersionEdition;
 			aAppVersionEditionModuleDto.AppCreatedById = aAppVersionEditionModuleEntity.AppCreatedById;
 			aAppVersionEditionModuleDto.AppCreatedDate = aAppVersionEditionModuleEntity.AppCreatedDate;
 			aAppVersionEditionModuleDto.AppModifiedDate = aAppVersionEditionModuleEntity.AppModifiedDate;
 			aAppVersionEditionModuleDto.AppModifiedById = aAppVersionEditionModuleEntity.AppModifiedById;
 			aAppVersionEditionModuleDto.AppCreatedByCompanyId = aAppVersionEditionModuleEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppVersionEditionModuleDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppVersionEditionModuleEntity.AppCreatedDate);
                aAppVersionEditionModuleDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppVersionEditionModuleEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppVersionEditionModuleEntity, aAppVersionEditionModuleDto);
		}
		
		 /// <summary>
        ///  Copy AppVersionEditionModuleDto Properties to   AppVersionEditionModuleEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppVersionEditionModuleEntity aAppVersionEditionModuleEntity,AppVersionEditionModuleDto aAppVersionEditionModuleDto)
        {        
 
      			aAppVersionEditionModuleEntity.MenuId = aAppVersionEditionModuleDto.MenuId;
      			aAppVersionEditionModuleEntity.EmApplicationVersionEdition = aAppVersionEditionModuleDto.EmApplicationVersionEdition;
 
  
   
    
      			aAppVersionEditionModuleEntity.AppCreatedByCompanyId = aAppVersionEditionModuleDto.AppCreatedByCompanyId;
			
			if(aAppVersionEditionModuleDto.Id == null)
			{
				aAppVersionEditionModuleEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppVersionEditionModuleEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppVersionEditionModuleEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppVersionEditionModuleEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppVersionEditionModuleEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppVersionEditionModuleEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppVersionEditionModuleEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppVersionEditionModuleEntity, aAppVersionEditionModuleDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppVersionEditionModuleEntity aAppVersionEditionModuleEntity,AppVersionEditionModuleDto aAppVersionEditionModuleDto);
		
		static partial void OnCopyDtoToEntityDone(AppVersionEditionModuleEntity aAppVersionEditionModuleEntity,AppVersionEditionModuleDto aAppVersionEditionModuleDto);
		
   
       
    }
}

 