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
    /// Convert Properties between  AppLanguageEntity and  AppLanguageDto
    /// </summary>
    public static partial class AppLanguageConverter 
    {
         /// <summary>
        ///  Convert AppLanguageEntity To  AppLanguageDto
        /// </summary>
        public static AppLanguageDto ConvertEntityToDto(AppLanguageEntity aAppLanguageEntity)
        {        
    		AppLanguageDto aAppLanguageDto = new AppLanguageDto();
    		CopyEntityPropertyToDto( aAppLanguageEntity, aAppLanguageDto);          
			return aAppLanguageDto;
        }
		 /// <summary>
        ///  Convert AppLanguageEntity To  AppLanguageExDto
        /// </summary>
        public static AppLanguageExDto ConvertEntityToExDto(AppLanguageEntity aAppLanguageEntity)
        {        
    		AppLanguageExDto aAppLanguageExDto = new AppLanguageExDto();
			CopyEntityPropertyToDto( aAppLanguageEntity, aAppLanguageExDto);
			
			
			
            return aAppLanguageExDto;
        }
		
		 /// <summary>
        ///  Convert AppLanguageEntity To  AppLanguageDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppLanguageEntity aAppLanguageEntity,AppLanguageDto aAppLanguageDto)
        {        
    		
           // aAppLanguageDto.StopChangeTracking();
 			aAppLanguageDto.Id = aAppLanguageEntity.LanguageId;
 			aAppLanguageDto.Name = aAppLanguageEntity.Name;
 			aAppLanguageDto.Description = aAppLanguageEntity.Description;
 			aAppLanguageDto.IsDefault = aAppLanguageEntity.IsDefault;
 			aAppLanguageDto.DefaultCultureInfoCode = aAppLanguageEntity.DefaultCultureInfoCode;
 			aAppLanguageDto.SystemTimeStamp = aAppLanguageEntity.SystemTimeStamp;
 			aAppLanguageDto.AppCreatedById = aAppLanguageEntity.AppCreatedById;
 			aAppLanguageDto.AppCreatedDate = aAppLanguageEntity.AppCreatedDate;
 			aAppLanguageDto.AppModifiedDate = aAppLanguageEntity.AppModifiedDate;
 			aAppLanguageDto.AppModifiedById = aAppLanguageEntity.AppModifiedById;
 			aAppLanguageDto.AppCreatedByCompanyId = aAppLanguageEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppLanguageDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppLanguageEntity.AppCreatedDate);
                aAppLanguageDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppLanguageEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppLanguageEntity, aAppLanguageDto);
		}
		
		 /// <summary>
        ///  Copy AppLanguageDto Properties to   AppLanguageEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppLanguageEntity aAppLanguageEntity,AppLanguageDto aAppLanguageDto)
        {        
 
      			aAppLanguageEntity.Name = aAppLanguageDto.Name;
      			aAppLanguageEntity.Description = aAppLanguageDto.Description;
      			aAppLanguageEntity.IsDefault = aAppLanguageDto.IsDefault;
      			aAppLanguageEntity.DefaultCultureInfoCode = aAppLanguageDto.DefaultCultureInfoCode;
 
 
  
   
    
      			aAppLanguageEntity.AppCreatedByCompanyId = aAppLanguageDto.AppCreatedByCompanyId;
			
			if(aAppLanguageDto.Id == null)
			{
				aAppLanguageEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppLanguageEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppLanguageEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppLanguageEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppLanguageEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppLanguageEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppLanguageEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppLanguageEntity, aAppLanguageDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppLanguageEntity aAppLanguageEntity,AppLanguageDto aAppLanguageDto);
		
		static partial void OnCopyDtoToEntityDone(AppLanguageEntity aAppLanguageEntity,AppLanguageDto aAppLanguageDto);
		
   
       
    }
}

 