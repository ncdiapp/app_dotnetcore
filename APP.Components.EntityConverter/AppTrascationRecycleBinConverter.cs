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
    /// Convert Properties between  AppTrascationRecycleBinEntity and  AppTrascationRecycleBinDto
    /// </summary>
    public static partial class AppTrascationRecycleBinConverter 
    {
         /// <summary>
        ///  Convert AppTrascationRecycleBinEntity To  AppTrascationRecycleBinDto
        /// </summary>
        public static AppTrascationRecycleBinDto ConvertEntityToDto(AppTrascationRecycleBinEntity aAppTrascationRecycleBinEntity)
        {        
    		AppTrascationRecycleBinDto aAppTrascationRecycleBinDto = new AppTrascationRecycleBinDto();
    		CopyEntityPropertyToDto( aAppTrascationRecycleBinEntity, aAppTrascationRecycleBinDto);          
			return aAppTrascationRecycleBinDto;
        }
		 /// <summary>
        ///  Convert AppTrascationRecycleBinEntity To  AppTrascationRecycleBinExDto
        /// </summary>
        public static AppTrascationRecycleBinExDto ConvertEntityToExDto(AppTrascationRecycleBinEntity aAppTrascationRecycleBinEntity)
        {        
    		AppTrascationRecycleBinExDto aAppTrascationRecycleBinExDto = new AppTrascationRecycleBinExDto();
			CopyEntityPropertyToDto( aAppTrascationRecycleBinEntity, aAppTrascationRecycleBinExDto);
			
			
			
            return aAppTrascationRecycleBinExDto;
        }
		
		 /// <summary>
        ///  Convert AppTrascationRecycleBinEntity To  AppTrascationRecycleBinDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppTrascationRecycleBinEntity aAppTrascationRecycleBinEntity,AppTrascationRecycleBinDto aAppTrascationRecycleBinDto)
        {        
    		
           // aAppTrascationRecycleBinDto.StopChangeTracking();
 			aAppTrascationRecycleBinDto.Id = aAppTrascationRecycleBinEntity.RecycleBinId;
 			aAppTrascationRecycleBinDto.TranscationId = aAppTrascationRecycleBinEntity.TranscationId;
 			aAppTrascationRecycleBinDto.RootKeyValueId = aAppTrascationRecycleBinEntity.RootKeyValueId;
 			aAppTrascationRecycleBinDto.AppCreatedById = aAppTrascationRecycleBinEntity.AppCreatedById;
 			aAppTrascationRecycleBinDto.AppCreatedDate = aAppTrascationRecycleBinEntity.AppCreatedDate;
 			aAppTrascationRecycleBinDto.AppModifiedDate = aAppTrascationRecycleBinEntity.AppModifiedDate;
 			aAppTrascationRecycleBinDto.AppModifiedById = aAppTrascationRecycleBinEntity.AppModifiedById;
 			aAppTrascationRecycleBinDto.AppCreatedByCompanyId = aAppTrascationRecycleBinEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppTrascationRecycleBinDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTrascationRecycleBinEntity.AppCreatedDate);
                aAppTrascationRecycleBinDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTrascationRecycleBinEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppTrascationRecycleBinEntity, aAppTrascationRecycleBinDto);
		}
		
		 /// <summary>
        ///  Copy AppTrascationRecycleBinDto Properties to   AppTrascationRecycleBinEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppTrascationRecycleBinEntity aAppTrascationRecycleBinEntity,AppTrascationRecycleBinDto aAppTrascationRecycleBinDto)
        {        
 
      			aAppTrascationRecycleBinEntity.TranscationId = aAppTrascationRecycleBinDto.TranscationId;
      			aAppTrascationRecycleBinEntity.RootKeyValueId = aAppTrascationRecycleBinDto.RootKeyValueId;
 
  
   
    
      			aAppTrascationRecycleBinEntity.AppCreatedByCompanyId = aAppTrascationRecycleBinDto.AppCreatedByCompanyId;
			
			if(aAppTrascationRecycleBinDto.Id == null)
			{
				aAppTrascationRecycleBinEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppTrascationRecycleBinEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppTrascationRecycleBinEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTrascationRecycleBinEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppTrascationRecycleBinEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppTrascationRecycleBinEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTrascationRecycleBinEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppTrascationRecycleBinEntity, aAppTrascationRecycleBinDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppTrascationRecycleBinEntity aAppTrascationRecycleBinEntity,AppTrascationRecycleBinDto aAppTrascationRecycleBinDto);
		
		static partial void OnCopyDtoToEntityDone(AppTrascationRecycleBinEntity aAppTrascationRecycleBinEntity,AppTrascationRecycleBinDto aAppTrascationRecycleBinDto);
		
   
       
    }
}

 