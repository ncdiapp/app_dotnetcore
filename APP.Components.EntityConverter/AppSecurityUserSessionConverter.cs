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
    /// Convert Properties between  AppSecurityUserSessionEntity and  AppSecurityUserSessionDto
    /// </summary>
    public static partial class AppSecurityUserSessionConverter 
    {
         /// <summary>
        ///  Convert AppSecurityUserSessionEntity To  AppSecurityUserSessionDto
        /// </summary>
        public static AppSecurityUserSessionDto ConvertEntityToDto(AppSecurityUserSessionEntity aAppSecurityUserSessionEntity)
        {        
    		AppSecurityUserSessionDto aAppSecurityUserSessionDto = new AppSecurityUserSessionDto();
    		CopyEntityPropertyToDto( aAppSecurityUserSessionEntity, aAppSecurityUserSessionDto);          
			return aAppSecurityUserSessionDto;
        }
		 /// <summary>
        ///  Convert AppSecurityUserSessionEntity To  AppSecurityUserSessionExDto
        /// </summary>
        public static AppSecurityUserSessionExDto ConvertEntityToExDto(AppSecurityUserSessionEntity aAppSecurityUserSessionEntity)
        {        
    		AppSecurityUserSessionExDto aAppSecurityUserSessionExDto = new AppSecurityUserSessionExDto();
			CopyEntityPropertyToDto( aAppSecurityUserSessionEntity, aAppSecurityUserSessionExDto);
			
			
			
            return aAppSecurityUserSessionExDto;
        }
		
		 /// <summary>
        ///  Convert AppSecurityUserSessionEntity To  AppSecurityUserSessionDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSecurityUserSessionEntity aAppSecurityUserSessionEntity,AppSecurityUserSessionDto aAppSecurityUserSessionDto)
        {        
    		
           // aAppSecurityUserSessionDto.StopChangeTracking();
 			aAppSecurityUserSessionDto.Id = aAppSecurityUserSessionEntity.UserSessionId;
 			aAppSecurityUserSessionDto.UserId = aAppSecurityUserSessionEntity.UserId;
 			aAppSecurityUserSessionDto.ExpirationDate = aAppSecurityUserSessionEntity.ExpirationDate;
 			aAppSecurityUserSessionDto.SessionId = aAppSecurityUserSessionEntity.SessionId;
 			aAppSecurityUserSessionDto.ApplicationType = aAppSecurityUserSessionEntity.ApplicationType;
 			aAppSecurityUserSessionDto.AppCreatedById = aAppSecurityUserSessionEntity.AppCreatedById;
 			aAppSecurityUserSessionDto.AppCreatedDate = aAppSecurityUserSessionEntity.AppCreatedDate;
 			aAppSecurityUserSessionDto.AppModifiedDate = aAppSecurityUserSessionEntity.AppModifiedDate;
 			aAppSecurityUserSessionDto.AppModifiedById = aAppSecurityUserSessionEntity.AppModifiedById;
 			aAppSecurityUserSessionDto.AppCreatedByCompanyId = aAppSecurityUserSessionEntity.AppCreatedByCompanyId;
 			aAppSecurityUserSessionDto.EmExternalSigninType = aAppSecurityUserSessionEntity.EmExternalSigninType;
 			aAppSecurityUserSessionDto.ExternalAcessToken = aAppSecurityUserSessionEntity.ExternalAcessToken;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSecurityUserSessionDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityUserSessionEntity.AppCreatedDate);
                aAppSecurityUserSessionDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityUserSessionEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSecurityUserSessionEntity, aAppSecurityUserSessionDto);
		}
		
		 /// <summary>
        ///  Copy AppSecurityUserSessionDto Properties to   AppSecurityUserSessionEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSecurityUserSessionEntity aAppSecurityUserSessionEntity,AppSecurityUserSessionDto aAppSecurityUserSessionDto)
        {        
 
      			aAppSecurityUserSessionEntity.UserId = aAppSecurityUserSessionDto.UserId;
      			aAppSecurityUserSessionEntity.ExpirationDate = aAppSecurityUserSessionDto.ExpirationDate;
      			aAppSecurityUserSessionEntity.SessionId = aAppSecurityUserSessionDto.SessionId;
      			aAppSecurityUserSessionEntity.ApplicationType = aAppSecurityUserSessionDto.ApplicationType;
 
  
   
    
      			aAppSecurityUserSessionEntity.AppCreatedByCompanyId = aAppSecurityUserSessionDto.AppCreatedByCompanyId;
      			aAppSecurityUserSessionEntity.EmExternalSigninType = aAppSecurityUserSessionDto.EmExternalSigninType;
      			aAppSecurityUserSessionEntity.ExternalAcessToken = aAppSecurityUserSessionDto.ExternalAcessToken;
			
			if(aAppSecurityUserSessionDto.Id == null)
			{
				aAppSecurityUserSessionEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityUserSessionEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSecurityUserSessionEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityUserSessionEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityUserSessionEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSecurityUserSessionEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityUserSessionEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSecurityUserSessionEntity, aAppSecurityUserSessionDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSecurityUserSessionEntity aAppSecurityUserSessionEntity,AppSecurityUserSessionDto aAppSecurityUserSessionDto);
		
		static partial void OnCopyDtoToEntityDone(AppSecurityUserSessionEntity aAppSecurityUserSessionEntity,AppSecurityUserSessionDto aAppSecurityUserSessionDto);
		
   
       
    }
}

 