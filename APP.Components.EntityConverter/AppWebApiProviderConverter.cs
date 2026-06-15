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
    /// Convert Properties between  AppWebApiProviderEntity and  AppWebApiProviderDto
    /// </summary>
    public static partial class AppWebApiProviderConverter 
    {
         /// <summary>
        ///  Convert AppWebApiProviderEntity To  AppWebApiProviderDto
        /// </summary>
        public static AppWebApiProviderDto ConvertEntityToDto(AppWebApiProviderEntity aAppWebApiProviderEntity)
        {        
    		AppWebApiProviderDto aAppWebApiProviderDto = new AppWebApiProviderDto();
    		CopyEntityPropertyToDto( aAppWebApiProviderEntity, aAppWebApiProviderDto);          
			return aAppWebApiProviderDto;
        }
		 /// <summary>
        ///  Convert AppWebApiProviderEntity To  AppWebApiProviderExDto
        /// </summary>
        public static AppWebApiProviderExDto ConvertEntityToExDto(AppWebApiProviderEntity aAppWebApiProviderEntity)
        {        
    		AppWebApiProviderExDto aAppWebApiProviderExDto = new AppWebApiProviderExDto();
			CopyEntityPropertyToDto( aAppWebApiProviderEntity, aAppWebApiProviderExDto);
			
			
			
            return aAppWebApiProviderExDto;
        }
		
		 /// <summary>
        ///  Convert AppWebApiProviderEntity To  AppWebApiProviderDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppWebApiProviderEntity aAppWebApiProviderEntity,AppWebApiProviderDto aAppWebApiProviderDto)
        {        
    		
           // aAppWebApiProviderDto.StopChangeTracking();
 			aAppWebApiProviderDto.Id = aAppWebApiProviderEntity.WebApiPorviderId;
 			aAppWebApiProviderDto.ProviderName = aAppWebApiProviderEntity.ProviderName;
 			aAppWebApiProviderDto.ApiKey = aAppWebApiProviderEntity.ApiKey;
 			aAppWebApiProviderDto.ApiSecret = aAppWebApiProviderEntity.ApiSecret;
 			aAppWebApiProviderDto.AuthorizationType = aAppWebApiProviderEntity.AuthorizationType;
 			aAppWebApiProviderDto.AuthorizationTypePrefix = aAppWebApiProviderEntity.AuthorizationTypePrefix;
 			aAppWebApiProviderDto.AppCreatedById = aAppWebApiProviderEntity.AppCreatedById;
 			aAppWebApiProviderDto.AppCreatedDate = aAppWebApiProviderEntity.AppCreatedDate;
 			aAppWebApiProviderDto.AppModifiedDate = aAppWebApiProviderEntity.AppModifiedDate;
 			aAppWebApiProviderDto.AppModifiedById = aAppWebApiProviderEntity.AppModifiedById;
 			aAppWebApiProviderDto.AppCreatedByCompanyId = aAppWebApiProviderEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppWebApiProviderDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppWebApiProviderEntity.AppCreatedDate);
                aAppWebApiProviderDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppWebApiProviderEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppWebApiProviderEntity, aAppWebApiProviderDto);
		}
		
		 /// <summary>
        ///  Copy AppWebApiProviderDto Properties to   AppWebApiProviderEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppWebApiProviderEntity aAppWebApiProviderEntity,AppWebApiProviderDto aAppWebApiProviderDto)
        {        
 
      			aAppWebApiProviderEntity.ProviderName = aAppWebApiProviderDto.ProviderName;
      			aAppWebApiProviderEntity.ApiKey = aAppWebApiProviderDto.ApiKey;
      			aAppWebApiProviderEntity.ApiSecret = aAppWebApiProviderDto.ApiSecret;
      			aAppWebApiProviderEntity.AuthorizationType = aAppWebApiProviderDto.AuthorizationType;
      			aAppWebApiProviderEntity.AuthorizationTypePrefix = aAppWebApiProviderDto.AuthorizationTypePrefix;
 
  
   
    
      			aAppWebApiProviderEntity.AppCreatedByCompanyId = aAppWebApiProviderDto.AppCreatedByCompanyId;
			
			if(aAppWebApiProviderDto.Id == null)
			{
				aAppWebApiProviderEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppWebApiProviderEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppWebApiProviderEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppWebApiProviderEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppWebApiProviderEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppWebApiProviderEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppWebApiProviderEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppWebApiProviderEntity, aAppWebApiProviderDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppWebApiProviderEntity aAppWebApiProviderEntity,AppWebApiProviderDto aAppWebApiProviderDto);
		
		static partial void OnCopyDtoToEntityDone(AppWebApiProviderEntity aAppWebApiProviderEntity,AppWebApiProviderDto aAppWebApiProviderDto);
		
   
       
    }
}

 