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
    /// Convert Properties between  AppSetupEntity and  AppSetupDto
    /// </summary>
    public static partial class AppSetupConverter 
    {
         /// <summary>
        ///  Convert AppSetupEntity To  AppSetupDto
        /// </summary>
        public static AppSetupDto ConvertEntityToDto(AppSetupEntity aAppSetupEntity)
        {        
    		AppSetupDto aAppSetupDto = new AppSetupDto();
    		CopyEntityPropertyToDto( aAppSetupEntity, aAppSetupDto);          
			return aAppSetupDto;
        }
		 /// <summary>
        ///  Convert AppSetupEntity To  AppSetupExDto
        /// </summary>
        public static AppSetupExDto ConvertEntityToExDto(AppSetupEntity aAppSetupEntity)
        {        
    		AppSetupExDto aAppSetupExDto = new AppSetupExDto();
			CopyEntityPropertyToDto( aAppSetupEntity, aAppSetupExDto);
			
			
			
            return aAppSetupExDto;
        }
		
		 /// <summary>
        ///  Convert AppSetupEntity To  AppSetupDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSetupEntity aAppSetupEntity,AppSetupDto aAppSetupDto)
        {        
    		
           // aAppSetupDto.StopChangeTracking();
 			aAppSetupDto.Id = aAppSetupEntity.SetupId;
 			aAppSetupDto.SetupCode = aAppSetupEntity.SetupCode;
 			aAppSetupDto.SetupValue = aAppSetupEntity.SetupValue;
 			aAppSetupDto.Description = aAppSetupEntity.Description;
 			aAppSetupDto.EntityId = aAppSetupEntity.EntityId;
 			aAppSetupDto.UsageType = aAppSetupEntity.UsageType;
 			aAppSetupDto.SystemTimeStamp = aAppSetupEntity.SystemTimeStamp;
 			aAppSetupDto.AppCreatedById = aAppSetupEntity.AppCreatedById;
 			aAppSetupDto.AppCreatedDate = aAppSetupEntity.AppCreatedDate;
 			aAppSetupDto.AppModifiedDate = aAppSetupEntity.AppModifiedDate;
 			aAppSetupDto.AppModifiedById = aAppSetupEntity.AppModifiedById;
 			aAppSetupDto.AppCreatedByCompanyId = aAppSetupEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSetupDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSetupEntity.AppCreatedDate);
                aAppSetupDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSetupEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSetupEntity, aAppSetupDto);
		}
		
		 /// <summary>
        ///  Copy AppSetupDto Properties to   AppSetupEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSetupEntity aAppSetupEntity,AppSetupDto aAppSetupDto)
        {        
 
      			aAppSetupEntity.SetupCode = aAppSetupDto.SetupCode;
      			aAppSetupEntity.SetupValue = aAppSetupDto.SetupValue;
      			aAppSetupEntity.Description = aAppSetupDto.Description;
      			aAppSetupEntity.EntityId = aAppSetupDto.EntityId;
      			aAppSetupEntity.UsageType = aAppSetupDto.UsageType;
 
 
  
   
    
      			aAppSetupEntity.AppCreatedByCompanyId = aAppSetupDto.AppCreatedByCompanyId;
			
			if(aAppSetupDto.Id == null)
			{
				aAppSetupEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSetupEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSetupEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSetupEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSetupEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSetupEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSetupEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSetupEntity, aAppSetupDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSetupEntity aAppSetupEntity,AppSetupDto aAppSetupDto);
		
		static partial void OnCopyDtoToEntityDone(AppSetupEntity aAppSetupEntity,AppSetupDto aAppSetupDto);
		
   
       
    }
}

 