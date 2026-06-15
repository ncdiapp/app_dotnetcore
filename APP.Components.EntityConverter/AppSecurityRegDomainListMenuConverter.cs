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
    /// Convert Properties between  AppSecurityRegDomainListMenuEntity and  AppSecurityRegDomainListMenuDto
    /// </summary>
    public static partial class AppSecurityRegDomainListMenuConverter 
    {
         /// <summary>
        ///  Convert AppSecurityRegDomainListMenuEntity To  AppSecurityRegDomainListMenuDto
        /// </summary>
        public static AppSecurityRegDomainListMenuDto ConvertEntityToDto(AppSecurityRegDomainListMenuEntity aAppSecurityRegDomainListMenuEntity)
        {        
    		AppSecurityRegDomainListMenuDto aAppSecurityRegDomainListMenuDto = new AppSecurityRegDomainListMenuDto();
    		CopyEntityPropertyToDto( aAppSecurityRegDomainListMenuEntity, aAppSecurityRegDomainListMenuDto);          
			return aAppSecurityRegDomainListMenuDto;
        }
		 /// <summary>
        ///  Convert AppSecurityRegDomainListMenuEntity To  AppSecurityRegDomainListMenuExDto
        /// </summary>
        public static AppSecurityRegDomainListMenuExDto ConvertEntityToExDto(AppSecurityRegDomainListMenuEntity aAppSecurityRegDomainListMenuEntity)
        {        
    		AppSecurityRegDomainListMenuExDto aAppSecurityRegDomainListMenuExDto = new AppSecurityRegDomainListMenuExDto();
			CopyEntityPropertyToDto( aAppSecurityRegDomainListMenuEntity, aAppSecurityRegDomainListMenuExDto);
			
			
			
            return aAppSecurityRegDomainListMenuExDto;
        }
		
		 /// <summary>
        ///  Convert AppSecurityRegDomainListMenuEntity To  AppSecurityRegDomainListMenuDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSecurityRegDomainListMenuEntity aAppSecurityRegDomainListMenuEntity,AppSecurityRegDomainListMenuDto aAppSecurityRegDomainListMenuDto)
        {        
    		
           // aAppSecurityRegDomainListMenuDto.StopChangeTracking();
 			aAppSecurityRegDomainListMenuDto.Id = aAppSecurityRegDomainListMenuEntity.DomainMenuId;
 			aAppSecurityRegDomainListMenuDto.MenuId = aAppSecurityRegDomainListMenuEntity.MenuId;
 			aAppSecurityRegDomainListMenuDto.DomainId = aAppSecurityRegDomainListMenuEntity.DomainId;
 			aAppSecurityRegDomainListMenuDto.OrganizationId = aAppSecurityRegDomainListMenuEntity.OrganizationId;
 			aAppSecurityRegDomainListMenuDto.SystemTimeStamp = aAppSecurityRegDomainListMenuEntity.SystemTimeStamp;
 			aAppSecurityRegDomainListMenuDto.AppCreatedById = aAppSecurityRegDomainListMenuEntity.AppCreatedById;
 			aAppSecurityRegDomainListMenuDto.AppCreatedDate = aAppSecurityRegDomainListMenuEntity.AppCreatedDate;
 			aAppSecurityRegDomainListMenuDto.AppModifiedDate = aAppSecurityRegDomainListMenuEntity.AppModifiedDate;
 			aAppSecurityRegDomainListMenuDto.AppModifiedById = aAppSecurityRegDomainListMenuEntity.AppModifiedById;
 			aAppSecurityRegDomainListMenuDto.AppCreatedByCompanyId = aAppSecurityRegDomainListMenuEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSecurityRegDomainListMenuDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityRegDomainListMenuEntity.AppCreatedDate);
                aAppSecurityRegDomainListMenuDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityRegDomainListMenuEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSecurityRegDomainListMenuEntity, aAppSecurityRegDomainListMenuDto);
		}
		
		 /// <summary>
        ///  Copy AppSecurityRegDomainListMenuDto Properties to   AppSecurityRegDomainListMenuEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSecurityRegDomainListMenuEntity aAppSecurityRegDomainListMenuEntity,AppSecurityRegDomainListMenuDto aAppSecurityRegDomainListMenuDto)
        {        
 
      			aAppSecurityRegDomainListMenuEntity.MenuId = aAppSecurityRegDomainListMenuDto.MenuId;
      			aAppSecurityRegDomainListMenuEntity.DomainId = aAppSecurityRegDomainListMenuDto.DomainId;
      			aAppSecurityRegDomainListMenuEntity.OrganizationId = aAppSecurityRegDomainListMenuDto.OrganizationId;
 
 
  
   
    
      			aAppSecurityRegDomainListMenuEntity.AppCreatedByCompanyId = aAppSecurityRegDomainListMenuDto.AppCreatedByCompanyId;
			
			if(aAppSecurityRegDomainListMenuDto.Id == null)
			{
				aAppSecurityRegDomainListMenuEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityRegDomainListMenuEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSecurityRegDomainListMenuEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityRegDomainListMenuEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityRegDomainListMenuEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSecurityRegDomainListMenuEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityRegDomainListMenuEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSecurityRegDomainListMenuEntity, aAppSecurityRegDomainListMenuDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSecurityRegDomainListMenuEntity aAppSecurityRegDomainListMenuEntity,AppSecurityRegDomainListMenuDto aAppSecurityRegDomainListMenuDto);
		
		static partial void OnCopyDtoToEntityDone(AppSecurityRegDomainListMenuEntity aAppSecurityRegDomainListMenuEntity,AppSecurityRegDomainListMenuDto aAppSecurityRegDomainListMenuDto);
		
   
       
    }
}

 