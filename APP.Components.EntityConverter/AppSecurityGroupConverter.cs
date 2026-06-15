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
    /// Convert Properties between  AppSecurityGroupEntity and  AppSecurityGroupDto
    /// </summary>
    public static partial class AppSecurityGroupConverter 
    {
         /// <summary>
        ///  Convert AppSecurityGroupEntity To  AppSecurityGroupDto
        /// </summary>
        public static AppSecurityGroupDto ConvertEntityToDto(AppSecurityGroupEntity aAppSecurityGroupEntity)
        {        
    		AppSecurityGroupDto aAppSecurityGroupDto = new AppSecurityGroupDto();
    		CopyEntityPropertyToDto( aAppSecurityGroupEntity, aAppSecurityGroupDto);          
			return aAppSecurityGroupDto;
        }
		 /// <summary>
        ///  Convert AppSecurityGroupEntity To  AppSecurityGroupExDto
        /// </summary>
        public static AppSecurityGroupExDto ConvertEntityToExDto(AppSecurityGroupEntity aAppSecurityGroupEntity)
        {        
    		AppSecurityGroupExDto aAppSecurityGroupExDto = new AppSecurityGroupExDto();
			CopyEntityPropertyToDto( aAppSecurityGroupEntity, aAppSecurityGroupExDto);
			
			
			
            return aAppSecurityGroupExDto;
        }
		
		 /// <summary>
        ///  Convert AppSecurityGroupEntity To  AppSecurityGroupDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSecurityGroupEntity aAppSecurityGroupEntity,AppSecurityGroupDto aAppSecurityGroupDto)
        {        
    		
           // aAppSecurityGroupDto.StopChangeTracking();
 			aAppSecurityGroupDto.Id = aAppSecurityGroupEntity.GroupId;
 			aAppSecurityGroupDto.GroupName = aAppSecurityGroupEntity.GroupName;
 			aAppSecurityGroupDto.Description = aAppSecurityGroupEntity.Description;
 			aAppSecurityGroupDto.LoginEvent = aAppSecurityGroupEntity.LoginEvent;
 			aAppSecurityGroupDto.InternalCode = aAppSecurityGroupEntity.InternalCode;
 			aAppSecurityGroupDto.IsBuiltIn = aAppSecurityGroupEntity.IsBuiltIn;
 			aAppSecurityGroupDto.OrganizationId = aAppSecurityGroupEntity.OrganizationId;
 			aAppSecurityGroupDto.GroupUsage = aAppSecurityGroupEntity.GroupUsage;
 			aAppSecurityGroupDto.IsSharedbyMutipleCompany = aAppSecurityGroupEntity.IsSharedbyMutipleCompany;
 			aAppSecurityGroupDto.AppCreatedById = aAppSecurityGroupEntity.AppCreatedById;
 			aAppSecurityGroupDto.AppCreatedDate = aAppSecurityGroupEntity.AppCreatedDate;
 			aAppSecurityGroupDto.AppModifiedDate = aAppSecurityGroupEntity.AppModifiedDate;
 			aAppSecurityGroupDto.AppModifiedById = aAppSecurityGroupEntity.AppModifiedById;
 			aAppSecurityGroupDto.AppCreatedByCompanyId = aAppSecurityGroupEntity.AppCreatedByCompanyId;
 			aAppSecurityGroupDto.DefaultDesktopId = aAppSecurityGroupEntity.DefaultDesktopId;
 			aAppSecurityGroupDto.RoleUserTypeId = aAppSecurityGroupEntity.RoleUserTypeId;
 			aAppSecurityGroupDto.BusinessPartnerId = aAppSecurityGroupEntity.BusinessPartnerId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSecurityGroupDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityGroupEntity.AppCreatedDate);
                aAppSecurityGroupDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityGroupEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSecurityGroupEntity, aAppSecurityGroupDto);
		}
		
		 /// <summary>
        ///  Copy AppSecurityGroupDto Properties to   AppSecurityGroupEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSecurityGroupEntity aAppSecurityGroupEntity,AppSecurityGroupDto aAppSecurityGroupDto)
        {        
 
      			aAppSecurityGroupEntity.GroupName = aAppSecurityGroupDto.GroupName;
      			aAppSecurityGroupEntity.Description = aAppSecurityGroupDto.Description;
      			aAppSecurityGroupEntity.LoginEvent = aAppSecurityGroupDto.LoginEvent;
      			aAppSecurityGroupEntity.InternalCode = aAppSecurityGroupDto.InternalCode;
      			aAppSecurityGroupEntity.IsBuiltIn = aAppSecurityGroupDto.IsBuiltIn;
      			aAppSecurityGroupEntity.OrganizationId = aAppSecurityGroupDto.OrganizationId;
      			aAppSecurityGroupEntity.GroupUsage = aAppSecurityGroupDto.GroupUsage;
      			aAppSecurityGroupEntity.IsSharedbyMutipleCompany = aAppSecurityGroupDto.IsSharedbyMutipleCompany;
 
  
   
    
      			aAppSecurityGroupEntity.AppCreatedByCompanyId = aAppSecurityGroupDto.AppCreatedByCompanyId;
      			aAppSecurityGroupEntity.DefaultDesktopId = aAppSecurityGroupDto.DefaultDesktopId;
      			aAppSecurityGroupEntity.RoleUserTypeId = aAppSecurityGroupDto.RoleUserTypeId;
      			aAppSecurityGroupEntity.BusinessPartnerId = aAppSecurityGroupDto.BusinessPartnerId;
			
			if(aAppSecurityGroupDto.Id == null)
			{
				aAppSecurityGroupEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityGroupEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSecurityGroupEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityGroupEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityGroupEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSecurityGroupEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityGroupEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSecurityGroupEntity, aAppSecurityGroupDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSecurityGroupEntity aAppSecurityGroupEntity,AppSecurityGroupDto aAppSecurityGroupDto);
		
		static partial void OnCopyDtoToEntityDone(AppSecurityGroupEntity aAppSecurityGroupEntity,AppSecurityGroupDto aAppSecurityGroupDto);
		
   
       
    }
}

 