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
    /// Convert Properties between  AppComOrganizationEntity and  AppComOrganizationDto
    /// </summary>
    public static partial class AppComOrganizationConverter 
    {
         /// <summary>
        ///  Convert AppComOrganizationEntity To  AppComOrganizationDto
        /// </summary>
        public static AppComOrganizationDto ConvertEntityToDto(AppComOrganizationEntity aAppComOrganizationEntity)
        {        
    		AppComOrganizationDto aAppComOrganizationDto = new AppComOrganizationDto();
    		CopyEntityPropertyToDto( aAppComOrganizationEntity, aAppComOrganizationDto);          
			return aAppComOrganizationDto;
        }
		 /// <summary>
        ///  Convert AppComOrganizationEntity To  AppComOrganizationExDto
        /// </summary>
        public static AppComOrganizationExDto ConvertEntityToExDto(AppComOrganizationEntity aAppComOrganizationEntity)
        {        
    		AppComOrganizationExDto aAppComOrganizationExDto = new AppComOrganizationExDto();
			CopyEntityPropertyToDto( aAppComOrganizationEntity, aAppComOrganizationExDto);
			
			
			
            return aAppComOrganizationExDto;
        }
		
		 /// <summary>
        ///  Convert AppComOrganizationEntity To  AppComOrganizationDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppComOrganizationEntity aAppComOrganizationEntity,AppComOrganizationDto aAppComOrganizationDto)
        {        
    		
           // aAppComOrganizationDto.StopChangeTracking();
 			aAppComOrganizationDto.Id = aAppComOrganizationEntity.OrganizationId;
 			aAppComOrganizationDto.AppCompanyId = aAppComOrganizationEntity.AppCompanyId;
 			aAppComOrganizationDto.Code = aAppComOrganizationEntity.Code;
 			aAppComOrganizationDto.ShortName = aAppComOrganizationEntity.ShortName;
 			aAppComOrganizationDto.FullName = aAppComOrganizationEntity.FullName;
 			aAppComOrganizationDto.ClassificationLevel = aAppComOrganizationEntity.ClassificationLevel;
 			aAppComOrganizationDto.ContactPerson = aAppComOrganizationEntity.ContactPerson;
 			aAppComOrganizationDto.Telphone = aAppComOrganizationEntity.Telphone;
 			aAppComOrganizationDto.IsActive = aAppComOrganizationEntity.IsActive;
 			aAppComOrganizationDto.Memo = aAppComOrganizationEntity.Memo;
 			aAppComOrganizationDto.BelongToId = aAppComOrganizationEntity.BelongToId;
 			aAppComOrganizationDto.Adress = aAppComOrganizationEntity.Adress;
 			aAppComOrganizationDto.LeaderId = aAppComOrganizationEntity.LeaderId;
 			aAppComOrganizationDto.UserTypeEm = aAppComOrganizationEntity.UserTypeEm;
 			aAppComOrganizationDto.AppCreatedById = aAppComOrganizationEntity.AppCreatedById;
 			aAppComOrganizationDto.AppCreatedDate = aAppComOrganizationEntity.AppCreatedDate;
 			aAppComOrganizationDto.AppModifiedDate = aAppComOrganizationEntity.AppModifiedDate;
 			aAppComOrganizationDto.AppModifiedById = aAppComOrganizationEntity.AppModifiedById;
 			aAppComOrganizationDto.AppCreatedByCompanyId = aAppComOrganizationEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppComOrganizationDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppComOrganizationEntity.AppCreatedDate);
                aAppComOrganizationDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppComOrganizationEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppComOrganizationEntity, aAppComOrganizationDto);
		}
		
		 /// <summary>
        ///  Copy AppComOrganizationDto Properties to   AppComOrganizationEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppComOrganizationEntity aAppComOrganizationEntity,AppComOrganizationDto aAppComOrganizationDto)
        {        
 
      			aAppComOrganizationEntity.AppCompanyId = aAppComOrganizationDto.AppCompanyId;
      			aAppComOrganizationEntity.Code = aAppComOrganizationDto.Code;
      			aAppComOrganizationEntity.ShortName = aAppComOrganizationDto.ShortName;
      			aAppComOrganizationEntity.FullName = aAppComOrganizationDto.FullName;
      			aAppComOrganizationEntity.ClassificationLevel = aAppComOrganizationDto.ClassificationLevel;
      			aAppComOrganizationEntity.ContactPerson = aAppComOrganizationDto.ContactPerson;
      			aAppComOrganizationEntity.Telphone = aAppComOrganizationDto.Telphone;
      			aAppComOrganizationEntity.IsActive = aAppComOrganizationDto.IsActive;
      			aAppComOrganizationEntity.Memo = aAppComOrganizationDto.Memo;
      			aAppComOrganizationEntity.BelongToId = aAppComOrganizationDto.BelongToId;
      			aAppComOrganizationEntity.Adress = aAppComOrganizationDto.Adress;
      			aAppComOrganizationEntity.LeaderId = aAppComOrganizationDto.LeaderId;
      			aAppComOrganizationEntity.UserTypeEm = aAppComOrganizationDto.UserTypeEm;
 
  
   
    
      			aAppComOrganizationEntity.AppCreatedByCompanyId = aAppComOrganizationDto.AppCreatedByCompanyId;
			
			if(aAppComOrganizationDto.Id == null)
			{
				aAppComOrganizationEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppComOrganizationEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppComOrganizationEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppComOrganizationEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppComOrganizationEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppComOrganizationEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppComOrganizationEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppComOrganizationEntity, aAppComOrganizationDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppComOrganizationEntity aAppComOrganizationEntity,AppComOrganizationDto aAppComOrganizationDto);
		
		static partial void OnCopyDtoToEntityDone(AppComOrganizationEntity aAppComOrganizationEntity,AppComOrganizationDto aAppComOrganizationDto);
		
   
       
    }
}

 