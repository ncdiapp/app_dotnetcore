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
    /// Convert Properties between  AppCountryEntity and  AppCountryDto
    /// </summary>
    public static partial class AppCountryConverter 
    {
         /// <summary>
        ///  Convert AppCountryEntity To  AppCountryDto
        /// </summary>
        public static AppCountryDto ConvertEntityToDto(AppCountryEntity aAppCountryEntity)
        {        
    		AppCountryDto aAppCountryDto = new AppCountryDto();
    		CopyEntityPropertyToDto( aAppCountryEntity, aAppCountryDto);          
			return aAppCountryDto;
        }
		 /// <summary>
        ///  Convert AppCountryEntity To  AppCountryExDto
        /// </summary>
        public static AppCountryExDto ConvertEntityToExDto(AppCountryEntity aAppCountryEntity)
        {        
    		AppCountryExDto aAppCountryExDto = new AppCountryExDto();
			CopyEntityPropertyToDto( aAppCountryEntity, aAppCountryExDto);
			
			
			
            return aAppCountryExDto;
        }
		
		 /// <summary>
        ///  Convert AppCountryEntity To  AppCountryDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppCountryEntity aAppCountryEntity,AppCountryDto aAppCountryDto)
        {        
    		
           // aAppCountryDto.StopChangeTracking();
 			aAppCountryDto.Id = aAppCountryEntity.CountryId;
 			aAppCountryDto.Name = aAppCountryEntity.Name;
 			aAppCountryDto.Descriptoin = aAppCountryEntity.Descriptoin;
 			aAppCountryDto.Descriptoin2 = aAppCountryEntity.Descriptoin2;
 			aAppCountryDto.Alpha2Code = aAppCountryEntity.Alpha2Code;
 			aAppCountryDto.Alpha3Code = aAppCountryEntity.Alpha3Code;
 			aAppCountryDto.NumericCode = aAppCountryEntity.NumericCode;
 			aAppCountryDto.CurrencyId = aAppCountryEntity.CurrencyId;
 			aAppCountryDto.FlagImageId = aAppCountryEntity.FlagImageId;
 			aAppCountryDto.ContinentId = aAppCountryEntity.ContinentId;
 			aAppCountryDto.AppCreatedById = aAppCountryEntity.AppCreatedById;
 			aAppCountryDto.AppCreatedDate = aAppCountryEntity.AppCreatedDate;
 			aAppCountryDto.AppModifiedDate = aAppCountryEntity.AppModifiedDate;
 			aAppCountryDto.AppModifiedById = aAppCountryEntity.AppModifiedById;
 			aAppCountryDto.AppCreatedByCompanyId = aAppCountryEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppCountryDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCountryEntity.AppCreatedDate);
                aAppCountryDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCountryEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppCountryEntity, aAppCountryDto);
		}
		
		 /// <summary>
        ///  Copy AppCountryDto Properties to   AppCountryEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppCountryEntity aAppCountryEntity,AppCountryDto aAppCountryDto)
        {        
 
      			aAppCountryEntity.Name = aAppCountryDto.Name;
      			aAppCountryEntity.Descriptoin = aAppCountryDto.Descriptoin;
      			aAppCountryEntity.Descriptoin2 = aAppCountryDto.Descriptoin2;
      			aAppCountryEntity.Alpha2Code = aAppCountryDto.Alpha2Code;
      			aAppCountryEntity.Alpha3Code = aAppCountryDto.Alpha3Code;
      			aAppCountryEntity.NumericCode = aAppCountryDto.NumericCode;
      			aAppCountryEntity.CurrencyId = aAppCountryDto.CurrencyId;
      			aAppCountryEntity.FlagImageId = aAppCountryDto.FlagImageId;
      			aAppCountryEntity.ContinentId = aAppCountryDto.ContinentId;
 
  
   
    
      			aAppCountryEntity.AppCreatedByCompanyId = aAppCountryDto.AppCreatedByCompanyId;
			
			if(aAppCountryDto.Id == null)
			{
				aAppCountryEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppCountryEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppCountryEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppCountryEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppCountryEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppCountryEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppCountryEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppCountryEntity, aAppCountryDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppCountryEntity aAppCountryEntity,AppCountryDto aAppCountryDto);
		
		static partial void OnCopyDtoToEntityDone(AppCountryEntity aAppCountryEntity,AppCountryDto aAppCountryDto);
		
   
       
    }
}

 