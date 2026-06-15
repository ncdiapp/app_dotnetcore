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
    /// Convert Properties between  AppSecurityEntityActionEntity and  AppSecurityEntityActionDto
    /// </summary>
    public static partial class AppSecurityEntityActionConverter 
    {
         /// <summary>
        ///  Convert AppSecurityEntityActionEntity To  AppSecurityEntityActionDto
        /// </summary>
        public static AppSecurityEntityActionDto ConvertEntityToDto(AppSecurityEntityActionEntity aAppSecurityEntityActionEntity)
        {        
    		AppSecurityEntityActionDto aAppSecurityEntityActionDto = new AppSecurityEntityActionDto();
    		CopyEntityPropertyToDto( aAppSecurityEntityActionEntity, aAppSecurityEntityActionDto);          
			return aAppSecurityEntityActionDto;
        }
		 /// <summary>
        ///  Convert AppSecurityEntityActionEntity To  AppSecurityEntityActionExDto
        /// </summary>
        public static AppSecurityEntityActionExDto ConvertEntityToExDto(AppSecurityEntityActionEntity aAppSecurityEntityActionEntity)
        {        
    		AppSecurityEntityActionExDto aAppSecurityEntityActionExDto = new AppSecurityEntityActionExDto();
			CopyEntityPropertyToDto( aAppSecurityEntityActionEntity, aAppSecurityEntityActionExDto);
			
			
			
            return aAppSecurityEntityActionExDto;
        }
		
		 /// <summary>
        ///  Convert AppSecurityEntityActionEntity To  AppSecurityEntityActionDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSecurityEntityActionEntity aAppSecurityEntityActionEntity,AppSecurityEntityActionDto aAppSecurityEntityActionDto)
        {        
    		
           // aAppSecurityEntityActionDto.StopChangeTracking();
 			aAppSecurityEntityActionDto.Id = aAppSecurityEntityActionEntity.EntityActionId;
 			aAppSecurityEntityActionDto.ActionCode = aAppSecurityEntityActionEntity.ActionCode;
 			aAppSecurityEntityActionDto.Description = aAppSecurityEntityActionEntity.Description;
 			aAppSecurityEntityActionDto.NoSecurityControl = aAppSecurityEntityActionEntity.NoSecurityControl;
 			aAppSecurityEntityActionDto.TransactionId = aAppSecurityEntityActionEntity.TransactionId;
 			aAppSecurityEntityActionDto.RouteStateId = aAppSecurityEntityActionEntity.RouteStateId;
 			aAppSecurityEntityActionDto.IsSharedbyMutipleCompany = aAppSecurityEntityActionEntity.IsSharedbyMutipleCompany;
 			aAppSecurityEntityActionDto.AppCreatedById = aAppSecurityEntityActionEntity.AppCreatedById;
 			aAppSecurityEntityActionDto.AppCreatedDate = aAppSecurityEntityActionEntity.AppCreatedDate;
 			aAppSecurityEntityActionDto.AppModifiedDate = aAppSecurityEntityActionEntity.AppModifiedDate;
 			aAppSecurityEntityActionDto.AppModifiedById = aAppSecurityEntityActionEntity.AppModifiedById;
 			aAppSecurityEntityActionDto.AppCreatedByCompanyId = aAppSecurityEntityActionEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSecurityEntityActionDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityEntityActionEntity.AppCreatedDate);
                aAppSecurityEntityActionDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityEntityActionEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSecurityEntityActionEntity, aAppSecurityEntityActionDto);
		}
		
		 /// <summary>
        ///  Copy AppSecurityEntityActionDto Properties to   AppSecurityEntityActionEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSecurityEntityActionEntity aAppSecurityEntityActionEntity,AppSecurityEntityActionDto aAppSecurityEntityActionDto)
        {        
 
      			aAppSecurityEntityActionEntity.ActionCode = aAppSecurityEntityActionDto.ActionCode;
      			aAppSecurityEntityActionEntity.Description = aAppSecurityEntityActionDto.Description;
      			aAppSecurityEntityActionEntity.NoSecurityControl = aAppSecurityEntityActionDto.NoSecurityControl;
      			aAppSecurityEntityActionEntity.TransactionId = aAppSecurityEntityActionDto.TransactionId;
      			aAppSecurityEntityActionEntity.RouteStateId = aAppSecurityEntityActionDto.RouteStateId;
      			aAppSecurityEntityActionEntity.IsSharedbyMutipleCompany = aAppSecurityEntityActionDto.IsSharedbyMutipleCompany;
 
  
   
    
      			aAppSecurityEntityActionEntity.AppCreatedByCompanyId = aAppSecurityEntityActionDto.AppCreatedByCompanyId;
			
			if(aAppSecurityEntityActionDto.Id == null)
			{
				aAppSecurityEntityActionEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityEntityActionEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSecurityEntityActionEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityEntityActionEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityEntityActionEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSecurityEntityActionEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityEntityActionEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSecurityEntityActionEntity, aAppSecurityEntityActionDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSecurityEntityActionEntity aAppSecurityEntityActionEntity,AppSecurityEntityActionDto aAppSecurityEntityActionDto);
		
		static partial void OnCopyDtoToEntityDone(AppSecurityEntityActionEntity aAppSecurityEntityActionEntity,AppSecurityEntityActionDto aAppSecurityEntityActionDto);
		
   
       
    }
}

 