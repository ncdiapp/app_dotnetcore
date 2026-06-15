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
    /// Convert Properties between  AppCompanyEntity and  AppCompanyDto
    /// </summary>
    public static partial class AppCompanyConverter 
    {
         /// <summary>
        ///  Convert AppCompanyEntity To  AppCompanyDto
        /// </summary>
        public static AppCompanyDto ConvertEntityToDto(AppCompanyEntity aAppCompanyEntity)
        {        
    		AppCompanyDto aAppCompanyDto = new AppCompanyDto();
    		CopyEntityPropertyToDto( aAppCompanyEntity, aAppCompanyDto);          
			return aAppCompanyDto;
        }
		 /// <summary>
        ///  Convert AppCompanyEntity To  AppCompanyExDto
        /// </summary>
        public static AppCompanyExDto ConvertEntityToExDto(AppCompanyEntity aAppCompanyEntity)
        {        
    		AppCompanyExDto aAppCompanyExDto = new AppCompanyExDto();
			CopyEntityPropertyToDto( aAppCompanyEntity, aAppCompanyExDto);
			
			
			
            return aAppCompanyExDto;
        }
		
		 /// <summary>
        ///  Convert AppCompanyEntity To  AppCompanyDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppCompanyEntity aAppCompanyEntity,AppCompanyDto aAppCompanyDto)
        {        
    		
           // aAppCompanyDto.StopChangeTracking();
 			aAppCompanyDto.Id = aAppCompanyEntity.AppCompanyId;
 			aAppCompanyDto.Code = aAppCompanyEntity.Code;
 			aAppCompanyDto.ShortName = aAppCompanyEntity.ShortName;
 			aAppCompanyDto.FullName = aAppCompanyEntity.FullName;
 			aAppCompanyDto.RegistrationNumber = aAppCompanyEntity.RegistrationNumber;
 			aAppCompanyDto.ValueAddedTaxId = aAppCompanyEntity.ValueAddedTaxId;
 			aAppCompanyDto.Adress1 = aAppCompanyEntity.Adress1;
 			aAppCompanyDto.Adress2 = aAppCompanyEntity.Adress2;
 			aAppCompanyDto.Adress3 = aAppCompanyEntity.Adress3;
 			aAppCompanyDto.City = aAppCompanyEntity.City;
 			aAppCompanyDto.Language = aAppCompanyEntity.Language;
 			aAppCompanyDto.State = aAppCompanyEntity.State;
 			aAppCompanyDto.PostCode = aAppCompanyEntity.PostCode;
 			aAppCompanyDto.Country = aAppCompanyEntity.Country;
 			aAppCompanyDto.Status = aAppCompanyEntity.Status;
 			aAppCompanyDto.CurrencyCode = aAppCompanyEntity.CurrencyCode;
 			aAppCompanyDto.ContactPhone = aAppCompanyEntity.ContactPhone;
 			aAppCompanyDto.ContactName = aAppCompanyEntity.ContactName;
 			aAppCompanyDto.ContactFax = aAppCompanyEntity.ContactFax;
 			aAppCompanyDto.ParentCompayId = aAppCompanyEntity.ParentCompayId;
 			aAppCompanyDto.EmApplicationVersionEdition = aAppCompanyEntity.EmApplicationVersionEdition;
 			aAppCompanyDto.AppCreatedById = aAppCompanyEntity.AppCreatedById;
 			aAppCompanyDto.AppCreatedDate = aAppCompanyEntity.AppCreatedDate;
 			aAppCompanyDto.AppModifiedDate = aAppCompanyEntity.AppModifiedDate;
 			aAppCompanyDto.AppModifiedById = aAppCompanyEntity.AppModifiedById;
 			aAppCompanyDto.AppCreatedByCompanyId = aAppCompanyEntity.AppCreatedByCompanyId;
 			aAppCompanyDto.GlobalGuid = aAppCompanyEntity.GlobalGuid;
 			aAppCompanyDto.CompanyDomainIdentityToken = aAppCompanyEntity.CompanyDomainIdentityToken;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppCompanyDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCompanyEntity.AppCreatedDate);
                aAppCompanyDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCompanyEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppCompanyEntity, aAppCompanyDto);
		}
		
		 /// <summary>
        ///  Copy AppCompanyDto Properties to   AppCompanyEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppCompanyEntity aAppCompanyEntity,AppCompanyDto aAppCompanyDto)
        {        
 
      			aAppCompanyEntity.Code = aAppCompanyDto.Code;
      			aAppCompanyEntity.ShortName = aAppCompanyDto.ShortName;
      			aAppCompanyEntity.FullName = aAppCompanyDto.FullName;
      			aAppCompanyEntity.RegistrationNumber = aAppCompanyDto.RegistrationNumber;
      			aAppCompanyEntity.ValueAddedTaxId = aAppCompanyDto.ValueAddedTaxId;
      			aAppCompanyEntity.Adress1 = aAppCompanyDto.Adress1;
      			aAppCompanyEntity.Adress2 = aAppCompanyDto.Adress2;
      			aAppCompanyEntity.Adress3 = aAppCompanyDto.Adress3;
      			aAppCompanyEntity.City = aAppCompanyDto.City;
      			aAppCompanyEntity.Language = aAppCompanyDto.Language;
      			aAppCompanyEntity.State = aAppCompanyDto.State;
      			aAppCompanyEntity.PostCode = aAppCompanyDto.PostCode;
      			aAppCompanyEntity.Country = aAppCompanyDto.Country;
      			aAppCompanyEntity.Status = aAppCompanyDto.Status;
      			aAppCompanyEntity.CurrencyCode = aAppCompanyDto.CurrencyCode;
      			aAppCompanyEntity.ContactPhone = aAppCompanyDto.ContactPhone;
      			aAppCompanyEntity.ContactName = aAppCompanyDto.ContactName;
      			aAppCompanyEntity.ContactFax = aAppCompanyDto.ContactFax;
      			aAppCompanyEntity.ParentCompayId = aAppCompanyDto.ParentCompayId;
      			aAppCompanyEntity.EmApplicationVersionEdition = aAppCompanyDto.EmApplicationVersionEdition;
 
  
   
    
      			aAppCompanyEntity.AppCreatedByCompanyId = aAppCompanyDto.AppCreatedByCompanyId;
      			aAppCompanyEntity.GlobalGuid = aAppCompanyDto.GlobalGuid;
      			aAppCompanyEntity.CompanyDomainIdentityToken = aAppCompanyDto.CompanyDomainIdentityToken;
			
			if(aAppCompanyDto.Id == null)
			{
				aAppCompanyEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppCompanyEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppCompanyEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppCompanyEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppCompanyEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppCompanyEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppCompanyEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppCompanyEntity, aAppCompanyDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppCompanyEntity aAppCompanyEntity,AppCompanyDto aAppCompanyDto);
		
		static partial void OnCopyDtoToEntityDone(AppCompanyEntity aAppCompanyEntity,AppCompanyDto aAppCompanyDto);
		
   
       
    }
}

 