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
    /// Convert Properties between  AppSecurityRegDomainEntity and  AppSecurityRegDomainDto
    /// </summary>
    public static partial class AppSecurityRegDomainConverter 
    {
         /// <summary>
        ///  Convert AppSecurityRegDomainEntity To  AppSecurityRegDomainDto
        /// </summary>
        public static AppSecurityRegDomainDto ConvertEntityToDto(AppSecurityRegDomainEntity aAppSecurityRegDomainEntity)
        {        
    		AppSecurityRegDomainDto aAppSecurityRegDomainDto = new AppSecurityRegDomainDto();
    		CopyEntityPropertyToDto( aAppSecurityRegDomainEntity, aAppSecurityRegDomainDto);          
			return aAppSecurityRegDomainDto;
        }
		 /// <summary>
        ///  Convert AppSecurityRegDomainEntity To  AppSecurityRegDomainExDto
        /// </summary>
        public static AppSecurityRegDomainExDto ConvertEntityToExDto(AppSecurityRegDomainEntity aAppSecurityRegDomainEntity)
        {        
    		AppSecurityRegDomainExDto aAppSecurityRegDomainExDto = new AppSecurityRegDomainExDto();
			CopyEntityPropertyToDto( aAppSecurityRegDomainEntity, aAppSecurityRegDomainExDto);
			
			
			
            return aAppSecurityRegDomainExDto;
        }
		
		 /// <summary>
        ///  Convert AppSecurityRegDomainEntity To  AppSecurityRegDomainDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSecurityRegDomainEntity aAppSecurityRegDomainEntity,AppSecurityRegDomainDto aAppSecurityRegDomainDto)
        {        
    		
           // aAppSecurityRegDomainDto.StopChangeTracking();
 			aAppSecurityRegDomainDto.Id = aAppSecurityRegDomainEntity.DomainId;
 			aAppSecurityRegDomainDto.DomainCode = aAppSecurityRegDomainEntity.DomainCode;
 			aAppSecurityRegDomainDto.DomainType = aAppSecurityRegDomainEntity.DomainType;
 			aAppSecurityRegDomainDto.Description = aAppSecurityRegDomainEntity.Description;
 			aAppSecurityRegDomainDto.DefaultPage = aAppSecurityRegDomainEntity.DefaultPage;
 			aAppSecurityRegDomainDto.SystemTimeStamp = aAppSecurityRegDomainEntity.SystemTimeStamp;
 			aAppSecurityRegDomainDto.AppCreatedById = aAppSecurityRegDomainEntity.AppCreatedById;
 			aAppSecurityRegDomainDto.AppCreatedDate = aAppSecurityRegDomainEntity.AppCreatedDate;
 			aAppSecurityRegDomainDto.AppModifiedDate = aAppSecurityRegDomainEntity.AppModifiedDate;
 			aAppSecurityRegDomainDto.AppModifiedById = aAppSecurityRegDomainEntity.AppModifiedById;
 			aAppSecurityRegDomainDto.AppCreatedByCompanyId = aAppSecurityRegDomainEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSecurityRegDomainDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityRegDomainEntity.AppCreatedDate);
                aAppSecurityRegDomainDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityRegDomainEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSecurityRegDomainEntity, aAppSecurityRegDomainDto);
		}
		
		 /// <summary>
        ///  Copy AppSecurityRegDomainDto Properties to   AppSecurityRegDomainEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSecurityRegDomainEntity aAppSecurityRegDomainEntity,AppSecurityRegDomainDto aAppSecurityRegDomainDto)
        {        
 
      			aAppSecurityRegDomainEntity.DomainCode = aAppSecurityRegDomainDto.DomainCode;
      			aAppSecurityRegDomainEntity.DomainType = aAppSecurityRegDomainDto.DomainType;
      			aAppSecurityRegDomainEntity.Description = aAppSecurityRegDomainDto.Description;
      			aAppSecurityRegDomainEntity.DefaultPage = aAppSecurityRegDomainDto.DefaultPage;
 
 
  
   
    
      			aAppSecurityRegDomainEntity.AppCreatedByCompanyId = aAppSecurityRegDomainDto.AppCreatedByCompanyId;
			
			if(aAppSecurityRegDomainDto.Id == null)
			{
				aAppSecurityRegDomainEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityRegDomainEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSecurityRegDomainEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityRegDomainEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityRegDomainEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSecurityRegDomainEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityRegDomainEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSecurityRegDomainEntity, aAppSecurityRegDomainDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSecurityRegDomainEntity aAppSecurityRegDomainEntity,AppSecurityRegDomainDto aAppSecurityRegDomainDto);
		
		static partial void OnCopyDtoToEntityDone(AppSecurityRegDomainEntity aAppSecurityRegDomainEntity,AppSecurityRegDomainDto aAppSecurityRegDomainDto);
		
   
       
    }
}

 