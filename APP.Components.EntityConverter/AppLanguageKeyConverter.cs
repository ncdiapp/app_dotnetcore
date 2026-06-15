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
    /// Convert Properties between  AppLanguageKeyEntity and  AppLanguageKeyDto
    /// </summary>
    public static partial class AppLanguageKeyConverter 
    {
         /// <summary>
        ///  Convert AppLanguageKeyEntity To  AppLanguageKeyDto
        /// </summary>
        public static AppLanguageKeyDto ConvertEntityToDto(AppLanguageKeyEntity aAppLanguageKeyEntity)
        {        
    		AppLanguageKeyDto aAppLanguageKeyDto = new AppLanguageKeyDto();
    		CopyEntityPropertyToDto( aAppLanguageKeyEntity, aAppLanguageKeyDto);          
			return aAppLanguageKeyDto;
        }
		 /// <summary>
        ///  Convert AppLanguageKeyEntity To  AppLanguageKeyExDto
        /// </summary>
        public static AppLanguageKeyExDto ConvertEntityToExDto(AppLanguageKeyEntity aAppLanguageKeyEntity)
        {        
    		AppLanguageKeyExDto aAppLanguageKeyExDto = new AppLanguageKeyExDto();
			CopyEntityPropertyToDto( aAppLanguageKeyEntity, aAppLanguageKeyExDto);
			
			
			
            return aAppLanguageKeyExDto;
        }
		
		 /// <summary>
        ///  Convert AppLanguageKeyEntity To  AppLanguageKeyDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppLanguageKeyEntity aAppLanguageKeyEntity,AppLanguageKeyDto aAppLanguageKeyDto)
        {        
    		
           // aAppLanguageKeyDto.StopChangeTracking();
 			aAppLanguageKeyDto.Id = aAppLanguageKeyEntity.LanguageKeyId;
 			aAppLanguageKeyDto.ResourceKey = aAppLanguageKeyEntity.ResourceKey;
 			aAppLanguageKeyDto.LanguageId = aAppLanguageKeyEntity.LanguageId;
 			aAppLanguageKeyDto.ApplicationId = aAppLanguageKeyEntity.ApplicationId;
 			aAppLanguageKeyDto.Value = aAppLanguageKeyEntity.Value;
 			aAppLanguageKeyDto.SystemTimeStamp = aAppLanguageKeyEntity.SystemTimeStamp;
 			aAppLanguageKeyDto.AppCreatedById = aAppLanguageKeyEntity.AppCreatedById;
 			aAppLanguageKeyDto.AppCreatedDate = aAppLanguageKeyEntity.AppCreatedDate;
 			aAppLanguageKeyDto.AppModifiedDate = aAppLanguageKeyEntity.AppModifiedDate;
 			aAppLanguageKeyDto.AppModifiedById = aAppLanguageKeyEntity.AppModifiedById;
 			aAppLanguageKeyDto.AppCreatedByCompanyId = aAppLanguageKeyEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppLanguageKeyDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppLanguageKeyEntity.AppCreatedDate);
                aAppLanguageKeyDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppLanguageKeyEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppLanguageKeyEntity, aAppLanguageKeyDto);
		}
		
		 /// <summary>
        ///  Copy AppLanguageKeyDto Properties to   AppLanguageKeyEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppLanguageKeyEntity aAppLanguageKeyEntity,AppLanguageKeyDto aAppLanguageKeyDto)
        {        
 
      			aAppLanguageKeyEntity.ResourceKey = aAppLanguageKeyDto.ResourceKey;
      			aAppLanguageKeyEntity.LanguageId = aAppLanguageKeyDto.LanguageId;
      			aAppLanguageKeyEntity.ApplicationId = aAppLanguageKeyDto.ApplicationId;
      			aAppLanguageKeyEntity.Value = aAppLanguageKeyDto.Value;
 
 
  
   
    
      			aAppLanguageKeyEntity.AppCreatedByCompanyId = aAppLanguageKeyDto.AppCreatedByCompanyId;
			
			if(aAppLanguageKeyDto.Id == null)
			{
				aAppLanguageKeyEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppLanguageKeyEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppLanguageKeyEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppLanguageKeyEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppLanguageKeyEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppLanguageKeyEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppLanguageKeyEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppLanguageKeyEntity, aAppLanguageKeyDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppLanguageKeyEntity aAppLanguageKeyEntity,AppLanguageKeyDto aAppLanguageKeyDto);
		
		static partial void OnCopyDtoToEntityDone(AppLanguageKeyEntity aAppLanguageKeyEntity,AppLanguageKeyDto aAppLanguageKeyDto);
		
   
       
    }
}

 