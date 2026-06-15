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
    /// Convert Properties between  AppSecurityUserEntity and  AppSecurityUserDto
    /// </summary>
    public static partial class AppSecurityUserConverter 
    {
         /// <summary>
        ///  Convert AppSecurityUserEntity To  AppSecurityUserDto
        /// </summary>
        public static AppSecurityUserDto ConvertEntityToDto(AppSecurityUserEntity aAppSecurityUserEntity)
        {        
    		AppSecurityUserDto aAppSecurityUserDto = new AppSecurityUserDto();
    		CopyEntityPropertyToDto( aAppSecurityUserEntity, aAppSecurityUserDto);          
			return aAppSecurityUserDto;
        }
		 /// <summary>
        ///  Convert AppSecurityUserEntity To  AppSecurityUserExDto
        /// </summary>
        public static AppSecurityUserExDto ConvertEntityToExDto(AppSecurityUserEntity aAppSecurityUserEntity)
        {        
    		AppSecurityUserExDto aAppSecurityUserExDto = new AppSecurityUserExDto();
			CopyEntityPropertyToDto( aAppSecurityUserEntity, aAppSecurityUserExDto);
			
			
			
            return aAppSecurityUserExDto;
        }
		
		 /// <summary>
        ///  Convert AppSecurityUserEntity To  AppSecurityUserDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSecurityUserEntity aAppSecurityUserEntity,AppSecurityUserDto aAppSecurityUserDto)
        {        
    		
           // aAppSecurityUserDto.StopChangeTracking();
 			aAppSecurityUserDto.Id = aAppSecurityUserEntity.UserId;
 			aAppSecurityUserDto.LoginName = aAppSecurityUserEntity.LoginName;
 			aAppSecurityUserDto.UserName = aAppSecurityUserEntity.UserName;
 			aAppSecurityUserDto.Email = aAppSecurityUserEntity.Email;
 			aAppSecurityUserDto.DomainId = aAppSecurityUserEntity.DomainId;
 			aAppSecurityUserDto.OrganizationId = aAppSecurityUserEntity.OrganizationId;
 			aAppSecurityUserDto.ExchangeServiceUrl = aAppSecurityUserEntity.ExchangeServiceUrl;
 			aAppSecurityUserDto.MenuSetting = aAppSecurityUserEntity.MenuSetting;
 			aAppSecurityUserDto.UserLanguage = aAppSecurityUserEntity.UserLanguage;
 			aAppSecurityUserDto.Password = aAppSecurityUserEntity.Password;
 			aAppSecurityUserDto.CultureInfoCode = aAppSecurityUserEntity.CultureInfoCode;
 			aAppSecurityUserDto.IsBuiltIntUser = aAppSecurityUserEntity.IsBuiltIntUser;
 			aAppSecurityUserDto.IsActive = aAppSecurityUserEntity.IsActive;
 			aAppSecurityUserDto.IsDeleted = aAppSecurityUserEntity.IsDeleted;
 			aAppSecurityUserDto.DefaultVendorRequestFolderId = aAppSecurityUserEntity.DefaultVendorRequestFolderId;
 			aAppSecurityUserDto.LanguageId = aAppSecurityUserEntity.LanguageId;
 			aAppSecurityUserDto.AdloginName = aAppSecurityUserEntity.AdloginName;
 			aAppSecurityUserDto.DocumentId = aAppSecurityUserEntity.DocumentId;
 			aAppSecurityUserDto.TimeZoneInfoToken = aAppSecurityUserEntity.TimeZoneInfoToken;
 			aAppSecurityUserDto.CurrentContactEmail = aAppSecurityUserEntity.CurrentContactEmail;
 			aAppSecurityUserDto.Adpassword = aAppSecurityUserEntity.Adpassword;
 			aAppSecurityUserDto.Addomain = aAppSecurityUserEntity.Addomain;
 			aAppSecurityUserDto.RefreshToken = aAppSecurityUserEntity.RefreshToken;
 			aAppSecurityUserDto.CompanyCalendarId = aAppSecurityUserEntity.CompanyCalendarId;
 			aAppSecurityUserDto.PersonalRate = aAppSecurityUserEntity.PersonalRate;
 			aAppSecurityUserDto.CurrencyId = aAppSecurityUserEntity.CurrencyId;
 			aAppSecurityUserDto.IsPersonalEmailAccount = aAppSecurityUserEntity.IsPersonalEmailAccount;
 			aAppSecurityUserDto.EmailNotificationReceiveMode = aAppSecurityUserEntity.EmailNotificationReceiveMode;
 			aAppSecurityUserDto.AppCreatedById = aAppSecurityUserEntity.AppCreatedById;
 			aAppSecurityUserDto.AppCreatedDate = aAppSecurityUserEntity.AppCreatedDate;
 			aAppSecurityUserDto.AppModifiedDate = aAppSecurityUserEntity.AppModifiedDate;
 			aAppSecurityUserDto.AppCreatedByCompanyId = aAppSecurityUserEntity.AppCreatedByCompanyId;
 			aAppSecurityUserDto.AppModifiedById = aAppSecurityUserEntity.AppModifiedById;
 			aAppSecurityUserDto.DefaultDesktopId = aAppSecurityUserEntity.DefaultDesktopId;
 			aAppSecurityUserDto.GlobalGuid = aAppSecurityUserEntity.GlobalGuid;
 			aAppSecurityUserDto.IsRegisterCompleted = aAppSecurityUserEntity.IsRegisterCompleted;
 			aAppSecurityUserDto.MyOwnCompnanyId = aAppSecurityUserEntity.MyOwnCompnanyId;
 			aAppSecurityUserDto.MappingExternalEmployeeAccountId = aAppSecurityUserEntity.MappingExternalEmployeeAccountId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSecurityUserDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityUserEntity.AppCreatedDate);
                aAppSecurityUserDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityUserEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSecurityUserEntity, aAppSecurityUserDto);
		}
		
		 /// <summary>
        ///  Copy AppSecurityUserDto Properties to   AppSecurityUserEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSecurityUserEntity aAppSecurityUserEntity,AppSecurityUserDto aAppSecurityUserDto)
        {        
 
      			aAppSecurityUserEntity.LoginName = aAppSecurityUserDto.LoginName;
      			aAppSecurityUserEntity.UserName = aAppSecurityUserDto.UserName;
      			aAppSecurityUserEntity.Email = aAppSecurityUserDto.Email;
      			aAppSecurityUserEntity.DomainId = aAppSecurityUserDto.DomainId;
      			aAppSecurityUserEntity.OrganizationId = aAppSecurityUserDto.OrganizationId;
      			aAppSecurityUserEntity.ExchangeServiceUrl = aAppSecurityUserDto.ExchangeServiceUrl;
      			aAppSecurityUserEntity.MenuSetting = aAppSecurityUserDto.MenuSetting;
      			aAppSecurityUserEntity.UserLanguage = aAppSecurityUserDto.UserLanguage;
      			aAppSecurityUserEntity.Password = aAppSecurityUserDto.Password;
      			aAppSecurityUserEntity.CultureInfoCode = aAppSecurityUserDto.CultureInfoCode;
      			aAppSecurityUserEntity.IsBuiltIntUser = aAppSecurityUserDto.IsBuiltIntUser;
      			aAppSecurityUserEntity.IsActive = aAppSecurityUserDto.IsActive;
      			aAppSecurityUserEntity.IsDeleted = aAppSecurityUserDto.IsDeleted;
      			aAppSecurityUserEntity.DefaultVendorRequestFolderId = aAppSecurityUserDto.DefaultVendorRequestFolderId;
      			aAppSecurityUserEntity.LanguageId = aAppSecurityUserDto.LanguageId;
      			aAppSecurityUserEntity.AdloginName = aAppSecurityUserDto.AdloginName;
      			aAppSecurityUserEntity.DocumentId = aAppSecurityUserDto.DocumentId;
      			aAppSecurityUserEntity.TimeZoneInfoToken = aAppSecurityUserDto.TimeZoneInfoToken;
      			aAppSecurityUserEntity.CurrentContactEmail = aAppSecurityUserDto.CurrentContactEmail;
      			aAppSecurityUserEntity.Adpassword = aAppSecurityUserDto.Adpassword;
      			aAppSecurityUserEntity.Addomain = aAppSecurityUserDto.Addomain;
      			aAppSecurityUserEntity.RefreshToken = aAppSecurityUserDto.RefreshToken;
      			aAppSecurityUserEntity.CompanyCalendarId = aAppSecurityUserDto.CompanyCalendarId;
      			aAppSecurityUserEntity.PersonalRate = aAppSecurityUserDto.PersonalRate;
      			aAppSecurityUserEntity.CurrencyId = aAppSecurityUserDto.CurrencyId;
      			aAppSecurityUserEntity.IsPersonalEmailAccount = aAppSecurityUserDto.IsPersonalEmailAccount;
      			aAppSecurityUserEntity.EmailNotificationReceiveMode = aAppSecurityUserDto.EmailNotificationReceiveMode;
 
  
   
      			aAppSecurityUserEntity.AppCreatedByCompanyId = aAppSecurityUserDto.AppCreatedByCompanyId;
    
      			aAppSecurityUserEntity.DefaultDesktopId = aAppSecurityUserDto.DefaultDesktopId;
      			aAppSecurityUserEntity.GlobalGuid = aAppSecurityUserDto.GlobalGuid;
      			aAppSecurityUserEntity.IsRegisterCompleted = aAppSecurityUserDto.IsRegisterCompleted;
      			aAppSecurityUserEntity.MyOwnCompnanyId = aAppSecurityUserDto.MyOwnCompnanyId;
      			aAppSecurityUserEntity.MappingExternalEmployeeAccountId = aAppSecurityUserDto.MappingExternalEmployeeAccountId;
			
			if(aAppSecurityUserDto.Id == null)
			{
				aAppSecurityUserEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityUserEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSecurityUserEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityUserEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityUserEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSecurityUserEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityUserEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSecurityUserEntity, aAppSecurityUserDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSecurityUserEntity aAppSecurityUserEntity,AppSecurityUserDto aAppSecurityUserDto);
		
		static partial void OnCopyDtoToEntityDone(AppSecurityUserEntity aAppSecurityUserEntity,AppSecurityUserDto aAppSecurityUserDto);
		
   
       
    }
}

 