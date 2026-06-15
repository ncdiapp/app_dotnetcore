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
    /// Convert Properties between  AppIntergrationSettingEntity and  AppIntergrationSettingDto
    /// </summary>
    public static partial class AppIntergrationSettingConverter 
    {
         /// <summary>
        ///  Convert AppIntergrationSettingEntity To  AppIntergrationSettingDto
        /// </summary>
        public static AppIntergrationSettingDto ConvertEntityToDto(AppIntergrationSettingEntity aAppIntergrationSettingEntity)
        {        
    		AppIntergrationSettingDto aAppIntergrationSettingDto = new AppIntergrationSettingDto();
    		CopyEntityPropertyToDto( aAppIntergrationSettingEntity, aAppIntergrationSettingDto);          
			return aAppIntergrationSettingDto;
        }
		 /// <summary>
        ///  Convert AppIntergrationSettingEntity To  AppIntergrationSettingExDto
        /// </summary>
        public static AppIntergrationSettingExDto ConvertEntityToExDto(AppIntergrationSettingEntity aAppIntergrationSettingEntity)
        {        
    		AppIntergrationSettingExDto aAppIntergrationSettingExDto = new AppIntergrationSettingExDto();
			CopyEntityPropertyToDto( aAppIntergrationSettingEntity, aAppIntergrationSettingExDto);
			
			
			
            return aAppIntergrationSettingExDto;
        }
		
		 /// <summary>
        ///  Convert AppIntergrationSettingEntity To  AppIntergrationSettingDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppIntergrationSettingEntity aAppIntergrationSettingEntity,AppIntergrationSettingDto aAppIntergrationSettingDto)
        {        
    		
           // aAppIntergrationSettingDto.StopChangeTracking();
 			aAppIntergrationSettingDto.Id = aAppIntergrationSettingEntity.IntergrationSettingId;
 			aAppIntergrationSettingDto.Name = aAppIntergrationSettingEntity.Name;
 			aAppIntergrationSettingDto.InternalCode = aAppIntergrationSettingEntity.InternalCode;
 			aAppIntergrationSettingDto.Description = aAppIntergrationSettingEntity.Description;
 			aAppIntergrationSettingDto.DataSourceRegisterId = aAppIntergrationSettingEntity.DataSourceRegisterId;
 			aAppIntergrationSettingDto.RestApiurl = aAppIntergrationSettingEntity.RestApiurl;
 			aAppIntergrationSettingDto.ApicredentialConfig = aAppIntergrationSettingEntity.ApicredentialConfig;
 			aAppIntergrationSettingDto.IntergrationType = aAppIntergrationSettingEntity.IntergrationType;
 			aAppIntergrationSettingDto.AppCreatedById = aAppIntergrationSettingEntity.AppCreatedById;
 			aAppIntergrationSettingDto.AppCreatedDate = aAppIntergrationSettingEntity.AppCreatedDate;
 			aAppIntergrationSettingDto.AppModifiedDate = aAppIntergrationSettingEntity.AppModifiedDate;
 			aAppIntergrationSettingDto.AppModifiedById = aAppIntergrationSettingEntity.AppModifiedById;
 			aAppIntergrationSettingDto.AppCreatedByCompanyId = aAppIntergrationSettingEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppIntergrationSettingDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppIntergrationSettingEntity.AppCreatedDate);
                aAppIntergrationSettingDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppIntergrationSettingEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppIntergrationSettingEntity, aAppIntergrationSettingDto);
		}
		
		 /// <summary>
        ///  Copy AppIntergrationSettingDto Properties to   AppIntergrationSettingEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppIntergrationSettingEntity aAppIntergrationSettingEntity,AppIntergrationSettingDto aAppIntergrationSettingDto)
        {        
 
      			aAppIntergrationSettingEntity.Name = aAppIntergrationSettingDto.Name;
      			aAppIntergrationSettingEntity.InternalCode = aAppIntergrationSettingDto.InternalCode;
      			aAppIntergrationSettingEntity.Description = aAppIntergrationSettingDto.Description;
      			aAppIntergrationSettingEntity.DataSourceRegisterId = aAppIntergrationSettingDto.DataSourceRegisterId;
      			aAppIntergrationSettingEntity.RestApiurl = aAppIntergrationSettingDto.RestApiurl;
      			aAppIntergrationSettingEntity.ApicredentialConfig = aAppIntergrationSettingDto.ApicredentialConfig;
      			aAppIntergrationSettingEntity.IntergrationType = aAppIntergrationSettingDto.IntergrationType;
 
  
   
    
      			aAppIntergrationSettingEntity.AppCreatedByCompanyId = aAppIntergrationSettingDto.AppCreatedByCompanyId;
			
			if(aAppIntergrationSettingDto.Id == null)
			{
				aAppIntergrationSettingEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppIntergrationSettingEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppIntergrationSettingEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppIntergrationSettingEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppIntergrationSettingEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppIntergrationSettingEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppIntergrationSettingEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppIntergrationSettingEntity, aAppIntergrationSettingDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppIntergrationSettingEntity aAppIntergrationSettingEntity,AppIntergrationSettingDto aAppIntergrationSettingDto);
		
		static partial void OnCopyDtoToEntityDone(AppIntergrationSettingEntity aAppIntergrationSettingEntity,AppIntergrationSettingDto aAppIntergrationSettingDto);
		
   
       
    }
}

 