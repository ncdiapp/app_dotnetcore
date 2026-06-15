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
    /// Convert Properties between  AppCompanyOrderModuleEntity and  AppCompanyOrderModuleDto
    /// </summary>
    public static partial class AppCompanyOrderModuleConverter 
    {
         /// <summary>
        ///  Convert AppCompanyOrderModuleEntity To  AppCompanyOrderModuleDto
        /// </summary>
        public static AppCompanyOrderModuleDto ConvertEntityToDto(AppCompanyOrderModuleEntity aAppCompanyOrderModuleEntity)
        {        
    		AppCompanyOrderModuleDto aAppCompanyOrderModuleDto = new AppCompanyOrderModuleDto();
    		CopyEntityPropertyToDto( aAppCompanyOrderModuleEntity, aAppCompanyOrderModuleDto);          
			return aAppCompanyOrderModuleDto;
        }
		 /// <summary>
        ///  Convert AppCompanyOrderModuleEntity To  AppCompanyOrderModuleExDto
        /// </summary>
        public static AppCompanyOrderModuleExDto ConvertEntityToExDto(AppCompanyOrderModuleEntity aAppCompanyOrderModuleEntity)
        {        
    		AppCompanyOrderModuleExDto aAppCompanyOrderModuleExDto = new AppCompanyOrderModuleExDto();
			CopyEntityPropertyToDto( aAppCompanyOrderModuleEntity, aAppCompanyOrderModuleExDto);
			
			
			
            return aAppCompanyOrderModuleExDto;
        }
		
		 /// <summary>
        ///  Convert AppCompanyOrderModuleEntity To  AppCompanyOrderModuleDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppCompanyOrderModuleEntity aAppCompanyOrderModuleEntity,AppCompanyOrderModuleDto aAppCompanyOrderModuleDto)
        {        
    		
           // aAppCompanyOrderModuleDto.StopChangeTracking();
 			aAppCompanyOrderModuleDto.Id = aAppCompanyOrderModuleEntity.OrderModuleId;
 			aAppCompanyOrderModuleDto.ModuleRegisterId = aAppCompanyOrderModuleEntity.ModuleRegisterId;
 			aAppCompanyOrderModuleDto.StartTrialDate = aAppCompanyOrderModuleEntity.StartTrialDate;
 			aAppCompanyOrderModuleDto.EndTrialDate = aAppCompanyOrderModuleEntity.EndTrialDate;
 			aAppCompanyOrderModuleDto.CurrentUsageStatus = aAppCompanyOrderModuleEntity.CurrentUsageStatus;
 			aAppCompanyOrderModuleDto.AppCompanyId = aAppCompanyOrderModuleEntity.AppCompanyId;
 			aAppCompanyOrderModuleDto.AppCreatedById = aAppCompanyOrderModuleEntity.AppCreatedById;
 			aAppCompanyOrderModuleDto.AppCreatedDate = aAppCompanyOrderModuleEntity.AppCreatedDate;
 			aAppCompanyOrderModuleDto.AppModifiedDate = aAppCompanyOrderModuleEntity.AppModifiedDate;
 			aAppCompanyOrderModuleDto.AppModifiedById = aAppCompanyOrderModuleEntity.AppModifiedById;
 			aAppCompanyOrderModuleDto.AppCreatedByCompanyId = aAppCompanyOrderModuleEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppCompanyOrderModuleDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCompanyOrderModuleEntity.AppCreatedDate);
                aAppCompanyOrderModuleDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCompanyOrderModuleEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppCompanyOrderModuleEntity, aAppCompanyOrderModuleDto);
		}
		
		 /// <summary>
        ///  Copy AppCompanyOrderModuleDto Properties to   AppCompanyOrderModuleEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppCompanyOrderModuleEntity aAppCompanyOrderModuleEntity,AppCompanyOrderModuleDto aAppCompanyOrderModuleDto)
        {        
 
      			aAppCompanyOrderModuleEntity.ModuleRegisterId = aAppCompanyOrderModuleDto.ModuleRegisterId;
      			aAppCompanyOrderModuleEntity.StartTrialDate = aAppCompanyOrderModuleDto.StartTrialDate;
      			aAppCompanyOrderModuleEntity.EndTrialDate = aAppCompanyOrderModuleDto.EndTrialDate;
      			aAppCompanyOrderModuleEntity.CurrentUsageStatus = aAppCompanyOrderModuleDto.CurrentUsageStatus;
      			aAppCompanyOrderModuleEntity.AppCompanyId = aAppCompanyOrderModuleDto.AppCompanyId;
 
  
   
    
      			aAppCompanyOrderModuleEntity.AppCreatedByCompanyId = aAppCompanyOrderModuleDto.AppCreatedByCompanyId;
			
			if(aAppCompanyOrderModuleDto.Id == null)
			{
				aAppCompanyOrderModuleEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppCompanyOrderModuleEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppCompanyOrderModuleEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppCompanyOrderModuleEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppCompanyOrderModuleEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppCompanyOrderModuleEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppCompanyOrderModuleEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppCompanyOrderModuleEntity, aAppCompanyOrderModuleDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppCompanyOrderModuleEntity aAppCompanyOrderModuleEntity,AppCompanyOrderModuleDto aAppCompanyOrderModuleDto);
		
		static partial void OnCopyDtoToEntityDone(AppCompanyOrderModuleEntity aAppCompanyOrderModuleEntity,AppCompanyOrderModuleDto aAppCompanyOrderModuleDto);
		
   
       
    }
}

 