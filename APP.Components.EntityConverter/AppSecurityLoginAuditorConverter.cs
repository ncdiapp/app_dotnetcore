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
    /// Convert Properties between  AppSecurityLoginAuditorEntity and  AppSecurityLoginAuditorDto
    /// </summary>
    public static partial class AppSecurityLoginAuditorConverter 
    {
         /// <summary>
        ///  Convert AppSecurityLoginAuditorEntity To  AppSecurityLoginAuditorDto
        /// </summary>
        public static AppSecurityLoginAuditorDto ConvertEntityToDto(AppSecurityLoginAuditorEntity aAppSecurityLoginAuditorEntity)
        {        
    		AppSecurityLoginAuditorDto aAppSecurityLoginAuditorDto = new AppSecurityLoginAuditorDto();
    		CopyEntityPropertyToDto( aAppSecurityLoginAuditorEntity, aAppSecurityLoginAuditorDto);          
			return aAppSecurityLoginAuditorDto;
        }
		 /// <summary>
        ///  Convert AppSecurityLoginAuditorEntity To  AppSecurityLoginAuditorExDto
        /// </summary>
        public static AppSecurityLoginAuditorExDto ConvertEntityToExDto(AppSecurityLoginAuditorEntity aAppSecurityLoginAuditorEntity)
        {        
    		AppSecurityLoginAuditorExDto aAppSecurityLoginAuditorExDto = new AppSecurityLoginAuditorExDto();
			CopyEntityPropertyToDto( aAppSecurityLoginAuditorEntity, aAppSecurityLoginAuditorExDto);
			
			
			
            return aAppSecurityLoginAuditorExDto;
        }
		
		 /// <summary>
        ///  Convert AppSecurityLoginAuditorEntity To  AppSecurityLoginAuditorDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSecurityLoginAuditorEntity aAppSecurityLoginAuditorEntity,AppSecurityLoginAuditorDto aAppSecurityLoginAuditorDto)
        {        
    		
           // aAppSecurityLoginAuditorDto.StopChangeTracking();
 			aAppSecurityLoginAuditorDto.Id = aAppSecurityLoginAuditorEntity.LogId;
 			aAppSecurityLoginAuditorDto.LoginName = aAppSecurityLoginAuditorEntity.LoginName;
 			aAppSecurityLoginAuditorDto.Password = aAppSecurityLoginAuditorEntity.Password;
 			aAppSecurityLoginAuditorDto.HostAddress = aAppSecurityLoginAuditorEntity.HostAddress;
 			aAppSecurityLoginAuditorDto.State = aAppSecurityLoginAuditorEntity.State;
 			aAppSecurityLoginAuditorDto.LoginTime = aAppSecurityLoginAuditorEntity.LoginTime;
 			aAppSecurityLoginAuditorDto.AppCreatedById = aAppSecurityLoginAuditorEntity.AppCreatedById;
 			aAppSecurityLoginAuditorDto.AppCreatedDate = aAppSecurityLoginAuditorEntity.AppCreatedDate;
 			aAppSecurityLoginAuditorDto.AppModifiedDate = aAppSecurityLoginAuditorEntity.AppModifiedDate;
 			aAppSecurityLoginAuditorDto.AppModifiedById = aAppSecurityLoginAuditorEntity.AppModifiedById;
 			aAppSecurityLoginAuditorDto.AppCreatedByCompanyId = aAppSecurityLoginAuditorEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSecurityLoginAuditorDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityLoginAuditorEntity.AppCreatedDate);
                aAppSecurityLoginAuditorDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityLoginAuditorEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSecurityLoginAuditorEntity, aAppSecurityLoginAuditorDto);
		}
		
		 /// <summary>
        ///  Copy AppSecurityLoginAuditorDto Properties to   AppSecurityLoginAuditorEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSecurityLoginAuditorEntity aAppSecurityLoginAuditorEntity,AppSecurityLoginAuditorDto aAppSecurityLoginAuditorDto)
        {        
 
      			aAppSecurityLoginAuditorEntity.LoginName = aAppSecurityLoginAuditorDto.LoginName;
      			aAppSecurityLoginAuditorEntity.Password = aAppSecurityLoginAuditorDto.Password;
      			aAppSecurityLoginAuditorEntity.HostAddress = aAppSecurityLoginAuditorDto.HostAddress;
      			aAppSecurityLoginAuditorEntity.State = aAppSecurityLoginAuditorDto.State;
      			aAppSecurityLoginAuditorEntity.LoginTime = aAppSecurityLoginAuditorDto.LoginTime;
 
  
   
    
      			aAppSecurityLoginAuditorEntity.AppCreatedByCompanyId = aAppSecurityLoginAuditorDto.AppCreatedByCompanyId;
			
			if(aAppSecurityLoginAuditorDto.Id == null)
			{
				aAppSecurityLoginAuditorEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityLoginAuditorEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSecurityLoginAuditorEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityLoginAuditorEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityLoginAuditorEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSecurityLoginAuditorEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityLoginAuditorEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSecurityLoginAuditorEntity, aAppSecurityLoginAuditorDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSecurityLoginAuditorEntity aAppSecurityLoginAuditorEntity,AppSecurityLoginAuditorDto aAppSecurityLoginAuditorDto);
		
		static partial void OnCopyDtoToEntityDone(AppSecurityLoginAuditorEntity aAppSecurityLoginAuditorEntity,AppSecurityLoginAuditorDto aAppSecurityLoginAuditorDto);
		
   
       
    }
}

 