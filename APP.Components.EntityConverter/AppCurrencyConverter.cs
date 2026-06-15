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
    /// Convert Properties between  AppCurrencyEntity and  AppCurrencyDto
    /// </summary>
    public static partial class AppCurrencyConverter 
    {
         /// <summary>
        ///  Convert AppCurrencyEntity To  AppCurrencyDto
        /// </summary>
        public static AppCurrencyDto ConvertEntityToDto(AppCurrencyEntity aAppCurrencyEntity)
        {        
    		AppCurrencyDto aAppCurrencyDto = new AppCurrencyDto();
    		CopyEntityPropertyToDto( aAppCurrencyEntity, aAppCurrencyDto);          
			return aAppCurrencyDto;
        }
		 /// <summary>
        ///  Convert AppCurrencyEntity To  AppCurrencyExDto
        /// </summary>
        public static AppCurrencyExDto ConvertEntityToExDto(AppCurrencyEntity aAppCurrencyEntity)
        {        
    		AppCurrencyExDto aAppCurrencyExDto = new AppCurrencyExDto();
			CopyEntityPropertyToDto( aAppCurrencyEntity, aAppCurrencyExDto);
			
			
			
            return aAppCurrencyExDto;
        }
		
		 /// <summary>
        ///  Convert AppCurrencyEntity To  AppCurrencyDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppCurrencyEntity aAppCurrencyEntity,AppCurrencyDto aAppCurrencyDto)
        {        
    		
           // aAppCurrencyDto.StopChangeTracking();
 			aAppCurrencyDto.Code = aAppCurrencyEntity.Code;
 			aAppCurrencyDto.Description = aAppCurrencyEntity.Description;
 			aAppCurrencyDto.Id = aAppCurrencyEntity.CurrencyId;
 			aAppCurrencyDto.AppCreatedById = aAppCurrencyEntity.AppCreatedById;
 			aAppCurrencyDto.AppCreatedDate = aAppCurrencyEntity.AppCreatedDate;
 			aAppCurrencyDto.AppModifiedDate = aAppCurrencyEntity.AppModifiedDate;
 			aAppCurrencyDto.AppModifiedById = aAppCurrencyEntity.AppModifiedById;
 			aAppCurrencyDto.AppCreatedByCompanyId = aAppCurrencyEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppCurrencyDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCurrencyEntity.AppCreatedDate);
                aAppCurrencyDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCurrencyEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppCurrencyEntity, aAppCurrencyDto);
		}
		
		 /// <summary>
        ///  Copy AppCurrencyDto Properties to   AppCurrencyEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppCurrencyEntity aAppCurrencyEntity,AppCurrencyDto aAppCurrencyDto)
        {        
      			aAppCurrencyEntity.Code = aAppCurrencyDto.Code;
      			aAppCurrencyEntity.Description = aAppCurrencyDto.Description;
 
 
  
   
    
      			aAppCurrencyEntity.AppCreatedByCompanyId = aAppCurrencyDto.AppCreatedByCompanyId;
			
			if(aAppCurrencyDto.Id == null)
			{
				aAppCurrencyEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppCurrencyEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppCurrencyEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppCurrencyEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppCurrencyEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppCurrencyEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppCurrencyEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppCurrencyEntity, aAppCurrencyDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppCurrencyEntity aAppCurrencyEntity,AppCurrencyDto aAppCurrencyDto);
		
		static partial void OnCopyDtoToEntityDone(AppCurrencyEntity aAppCurrencyEntity,AppCurrencyDto aAppCurrencyDto);
		
   
       
    }
}

 