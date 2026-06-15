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
    /// Convert Properties between  AppWebApiConfigEntity and  AppWebApiConfigDto
    /// </summary>
    public static partial class AppWebApiConfigConverter 
    {
         /// <summary>
        ///  Convert AppWebApiConfigEntity To  AppWebApiConfigDto
        /// </summary>
        public static AppWebApiConfigDto ConvertEntityToDto(AppWebApiConfigEntity aAppWebApiConfigEntity)
        {        
    		AppWebApiConfigDto aAppWebApiConfigDto = new AppWebApiConfigDto();
    		CopyEntityPropertyToDto( aAppWebApiConfigEntity, aAppWebApiConfigDto);          
			return aAppWebApiConfigDto;
        }
		 /// <summary>
        ///  Convert AppWebApiConfigEntity To  AppWebApiConfigExDto
        /// </summary>
        public static AppWebApiConfigExDto ConvertEntityToExDto(AppWebApiConfigEntity aAppWebApiConfigEntity)
        {        
    		AppWebApiConfigExDto aAppWebApiConfigExDto = new AppWebApiConfigExDto();
			CopyEntityPropertyToDto( aAppWebApiConfigEntity, aAppWebApiConfigExDto);
			
			
			
            return aAppWebApiConfigExDto;
        }
		
		 /// <summary>
        ///  Convert AppWebApiConfigEntity To  AppWebApiConfigDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppWebApiConfigEntity aAppWebApiConfigEntity,AppWebApiConfigDto aAppWebApiConfigDto)
        {        
    		
           // aAppWebApiConfigDto.StopChangeTracking();
 			aAppWebApiConfigDto.Id = aAppWebApiConfigEntity.WebApiConfigId;
 			aAppWebApiConfigDto.WebApiName = aAppWebApiConfigEntity.WebApiName;
 			aAppWebApiConfigDto.Description = aAppWebApiConfigEntity.Description;
 			aAppWebApiConfigDto.WebApiProviderId = aAppWebApiConfigEntity.WebApiProviderId;
 			aAppWebApiConfigDto.WebApiBaseUrl = aAppWebApiConfigEntity.WebApiBaseUrl;
 			aAppWebApiConfigDto.PathParameterName = aAppWebApiConfigEntity.PathParameterName;
 			aAppWebApiConfigDto.WebMethod = aAppWebApiConfigEntity.WebMethod;
 			aAppWebApiConfigDto.WebApiFullUrlFormat = aAppWebApiConfigEntity.WebApiFullUrlFormat;
 			aAppWebApiConfigDto.AppCreatedById = aAppWebApiConfigEntity.AppCreatedById;
 			aAppWebApiConfigDto.AppCreatedDate = aAppWebApiConfigEntity.AppCreatedDate;
 			aAppWebApiConfigDto.AppModifiedDate = aAppWebApiConfigEntity.AppModifiedDate;
 			aAppWebApiConfigDto.AppModifiedById = aAppWebApiConfigEntity.AppModifiedById;
 			aAppWebApiConfigDto.AppCreatedByCompanyId = aAppWebApiConfigEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppWebApiConfigDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppWebApiConfigEntity.AppCreatedDate);
                aAppWebApiConfigDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppWebApiConfigEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppWebApiConfigEntity, aAppWebApiConfigDto);
		}
		
		 /// <summary>
        ///  Copy AppWebApiConfigDto Properties to   AppWebApiConfigEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppWebApiConfigEntity aAppWebApiConfigEntity,AppWebApiConfigDto aAppWebApiConfigDto)
        {        
 
      			aAppWebApiConfigEntity.WebApiName = aAppWebApiConfigDto.WebApiName;
      			aAppWebApiConfigEntity.Description = aAppWebApiConfigDto.Description;
      			aAppWebApiConfigEntity.WebApiProviderId = aAppWebApiConfigDto.WebApiProviderId;
      			aAppWebApiConfigEntity.WebApiBaseUrl = aAppWebApiConfigDto.WebApiBaseUrl;
      			aAppWebApiConfigEntity.PathParameterName = aAppWebApiConfigDto.PathParameterName;
      			aAppWebApiConfigEntity.WebMethod = aAppWebApiConfigDto.WebMethod;
      			aAppWebApiConfigEntity.WebApiFullUrlFormat = aAppWebApiConfigDto.WebApiFullUrlFormat;
 
  
   
    
      			aAppWebApiConfigEntity.AppCreatedByCompanyId = aAppWebApiConfigDto.AppCreatedByCompanyId;
			
			if(aAppWebApiConfigDto.Id == null)
			{
				aAppWebApiConfigEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppWebApiConfigEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppWebApiConfigEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppWebApiConfigEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppWebApiConfigEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppWebApiConfigEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppWebApiConfigEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppWebApiConfigEntity, aAppWebApiConfigDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppWebApiConfigEntity aAppWebApiConfigEntity,AppWebApiConfigDto aAppWebApiConfigDto);
		
		static partial void OnCopyDtoToEntityDone(AppWebApiConfigEntity aAppWebApiConfigEntity,AppWebApiConfigDto aAppWebApiConfigDto);
		
   
       
    }
}

 