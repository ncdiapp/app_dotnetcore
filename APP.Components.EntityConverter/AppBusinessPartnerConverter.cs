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
    /// Convert Properties between  AppBusinessPartnerEntity and  AppBusinessPartnerDto
    /// </summary>
    public static partial class AppBusinessPartnerConverter 
    {
         /// <summary>
        ///  Convert AppBusinessPartnerEntity To  AppBusinessPartnerDto
        /// </summary>
        public static AppBusinessPartnerDto ConvertEntityToDto(AppBusinessPartnerEntity aAppBusinessPartnerEntity)
        {        
    		AppBusinessPartnerDto aAppBusinessPartnerDto = new AppBusinessPartnerDto();
    		CopyEntityPropertyToDto( aAppBusinessPartnerEntity, aAppBusinessPartnerDto);          
			return aAppBusinessPartnerDto;
        }
		 /// <summary>
        ///  Convert AppBusinessPartnerEntity To  AppBusinessPartnerExDto
        /// </summary>
        public static AppBusinessPartnerExDto ConvertEntityToExDto(AppBusinessPartnerEntity aAppBusinessPartnerEntity)
        {        
    		AppBusinessPartnerExDto aAppBusinessPartnerExDto = new AppBusinessPartnerExDto();
			CopyEntityPropertyToDto( aAppBusinessPartnerEntity, aAppBusinessPartnerExDto);
			
			
			
            return aAppBusinessPartnerExDto;
        }
		
		 /// <summary>
        ///  Convert AppBusinessPartnerEntity To  AppBusinessPartnerDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppBusinessPartnerEntity aAppBusinessPartnerEntity,AppBusinessPartnerDto aAppBusinessPartnerDto)
        {        
    		
           // aAppBusinessPartnerDto.StopChangeTracking();
 			aAppBusinessPartnerDto.Id = aAppBusinessPartnerEntity.AppBusinessPartnerId;
 			aAppBusinessPartnerDto.AppCompanyId = aAppBusinessPartnerEntity.AppCompanyId;
 			aAppBusinessPartnerDto.Code = aAppBusinessPartnerEntity.Code;
 			aAppBusinessPartnerDto.ShortName = aAppBusinessPartnerEntity.ShortName;
 			aAppBusinessPartnerDto.FullName = aAppBusinessPartnerEntity.FullName;
 			aAppBusinessPartnerDto.Adress1 = aAppBusinessPartnerEntity.Adress1;
 			aAppBusinessPartnerDto.Adress2 = aAppBusinessPartnerEntity.Adress2;
 			aAppBusinessPartnerDto.Adress3 = aAppBusinessPartnerEntity.Adress3;
 			aAppBusinessPartnerDto.City = aAppBusinessPartnerEntity.City;
 			aAppBusinessPartnerDto.Language = aAppBusinessPartnerEntity.Language;
 			aAppBusinessPartnerDto.State = aAppBusinessPartnerEntity.State;
 			aAppBusinessPartnerDto.PostCode = aAppBusinessPartnerEntity.PostCode;
 			aAppBusinessPartnerDto.Country = aAppBusinessPartnerEntity.Country;
 			aAppBusinessPartnerDto.Status = aAppBusinessPartnerEntity.Status;
 			aAppBusinessPartnerDto.CurrencyCode = aAppBusinessPartnerEntity.CurrencyCode;
 			aAppBusinessPartnerDto.ContactPhone = aAppBusinessPartnerEntity.ContactPhone;
 			aAppBusinessPartnerDto.ContactName = aAppBusinessPartnerEntity.ContactName;
 			aAppBusinessPartnerDto.ContactFax = aAppBusinessPartnerEntity.ContactFax;
 			aAppBusinessPartnerDto.PartnerType = aAppBusinessPartnerEntity.PartnerType;
 			aAppBusinessPartnerDto.ShipToId = aAppBusinessPartnerEntity.ShipToId;
 			aAppBusinessPartnerDto.BillToId = aAppBusinessPartnerEntity.BillToId;
 			aAppBusinessPartnerDto.IsBillToSameShipTo = aAppBusinessPartnerEntity.IsBillToSameShipTo;
 			aAppBusinessPartnerDto.AppCreatedById = aAppBusinessPartnerEntity.AppCreatedById;
 			aAppBusinessPartnerDto.AppCreatedDate = aAppBusinessPartnerEntity.AppCreatedDate;
 			aAppBusinessPartnerDto.AppModifiedDate = aAppBusinessPartnerEntity.AppModifiedDate;
 			aAppBusinessPartnerDto.AppModifiedById = aAppBusinessPartnerEntity.AppModifiedById;
 			aAppBusinessPartnerDto.AppCreatedByCompanyId = aAppBusinessPartnerEntity.AppCreatedByCompanyId;
 			aAppBusinessPartnerDto.MappingExternalBusinessPartnerAccountId = aAppBusinessPartnerEntity.MappingExternalBusinessPartnerAccountId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppBusinessPartnerDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppBusinessPartnerEntity.AppCreatedDate);
                aAppBusinessPartnerDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppBusinessPartnerEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppBusinessPartnerEntity, aAppBusinessPartnerDto);
		}
		
		 /// <summary>
        ///  Copy AppBusinessPartnerDto Properties to   AppBusinessPartnerEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppBusinessPartnerEntity aAppBusinessPartnerEntity,AppBusinessPartnerDto aAppBusinessPartnerDto)
        {        
 
      			aAppBusinessPartnerEntity.AppCompanyId = aAppBusinessPartnerDto.AppCompanyId;
      			aAppBusinessPartnerEntity.Code = aAppBusinessPartnerDto.Code;
      			aAppBusinessPartnerEntity.ShortName = aAppBusinessPartnerDto.ShortName;
      			aAppBusinessPartnerEntity.FullName = aAppBusinessPartnerDto.FullName;
      			aAppBusinessPartnerEntity.Adress1 = aAppBusinessPartnerDto.Adress1;
      			aAppBusinessPartnerEntity.Adress2 = aAppBusinessPartnerDto.Adress2;
      			aAppBusinessPartnerEntity.Adress3 = aAppBusinessPartnerDto.Adress3;
      			aAppBusinessPartnerEntity.City = aAppBusinessPartnerDto.City;
      			aAppBusinessPartnerEntity.Language = aAppBusinessPartnerDto.Language;
      			aAppBusinessPartnerEntity.State = aAppBusinessPartnerDto.State;
      			aAppBusinessPartnerEntity.PostCode = aAppBusinessPartnerDto.PostCode;
      			aAppBusinessPartnerEntity.Country = aAppBusinessPartnerDto.Country;
      			aAppBusinessPartnerEntity.Status = aAppBusinessPartnerDto.Status;
      			aAppBusinessPartnerEntity.CurrencyCode = aAppBusinessPartnerDto.CurrencyCode;
      			aAppBusinessPartnerEntity.ContactPhone = aAppBusinessPartnerDto.ContactPhone;
      			aAppBusinessPartnerEntity.ContactName = aAppBusinessPartnerDto.ContactName;
      			aAppBusinessPartnerEntity.ContactFax = aAppBusinessPartnerDto.ContactFax;
      			aAppBusinessPartnerEntity.PartnerType = aAppBusinessPartnerDto.PartnerType;
      			aAppBusinessPartnerEntity.ShipToId = aAppBusinessPartnerDto.ShipToId;
      			aAppBusinessPartnerEntity.BillToId = aAppBusinessPartnerDto.BillToId;
      			aAppBusinessPartnerEntity.IsBillToSameShipTo = aAppBusinessPartnerDto.IsBillToSameShipTo;
 
  
   
    
      			aAppBusinessPartnerEntity.AppCreatedByCompanyId = aAppBusinessPartnerDto.AppCreatedByCompanyId;
      			aAppBusinessPartnerEntity.MappingExternalBusinessPartnerAccountId = aAppBusinessPartnerDto.MappingExternalBusinessPartnerAccountId;
			
			if(aAppBusinessPartnerDto.Id == null)
			{
				aAppBusinessPartnerEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppBusinessPartnerEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppBusinessPartnerEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppBusinessPartnerEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppBusinessPartnerEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppBusinessPartnerEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppBusinessPartnerEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppBusinessPartnerEntity, aAppBusinessPartnerDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppBusinessPartnerEntity aAppBusinessPartnerEntity,AppBusinessPartnerDto aAppBusinessPartnerDto);
		
		static partial void OnCopyDtoToEntityDone(AppBusinessPartnerEntity aAppBusinessPartnerEntity,AppBusinessPartnerDto aAppBusinessPartnerDto);
		
   
       
    }
}

 