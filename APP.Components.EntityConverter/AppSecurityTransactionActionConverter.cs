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
    /// Convert Properties between  AppSecurityTransactionActionEntity and  AppSecurityTransactionActionDto
    /// </summary>
    public static partial class AppSecurityTransactionActionConverter 
    {
         /// <summary>
        ///  Convert AppSecurityTransactionActionEntity To  AppSecurityTransactionActionDto
        /// </summary>
        public static AppSecurityTransactionActionDto ConvertEntityToDto(AppSecurityTransactionActionEntity aAppSecurityTransactionActionEntity)
        {        
    		AppSecurityTransactionActionDto aAppSecurityTransactionActionDto = new AppSecurityTransactionActionDto();
    		CopyEntityPropertyToDto( aAppSecurityTransactionActionEntity, aAppSecurityTransactionActionDto);          
			return aAppSecurityTransactionActionDto;
        }
		 /// <summary>
        ///  Convert AppSecurityTransactionActionEntity To  AppSecurityTransactionActionExDto
        /// </summary>
        public static AppSecurityTransactionActionExDto ConvertEntityToExDto(AppSecurityTransactionActionEntity aAppSecurityTransactionActionEntity)
        {        
    		AppSecurityTransactionActionExDto aAppSecurityTransactionActionExDto = new AppSecurityTransactionActionExDto();
			CopyEntityPropertyToDto( aAppSecurityTransactionActionEntity, aAppSecurityTransactionActionExDto);
			
			
			
            return aAppSecurityTransactionActionExDto;
        }
		
		 /// <summary>
        ///  Convert AppSecurityTransactionActionEntity To  AppSecurityTransactionActionDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSecurityTransactionActionEntity aAppSecurityTransactionActionEntity,AppSecurityTransactionActionDto aAppSecurityTransactionActionDto)
        {        
    		
           // aAppSecurityTransactionActionDto.StopChangeTracking();
 			aAppSecurityTransactionActionDto.Id = aAppSecurityTransactionActionEntity.TransactionActionId;
 			aAppSecurityTransactionActionDto.Name = aAppSecurityTransactionActionEntity.Name;
 			aAppSecurityTransactionActionDto.Description = aAppSecurityTransactionActionEntity.Description;
 			aAppSecurityTransactionActionDto.TransactionId = aAppSecurityTransactionActionEntity.TransactionId;
 			aAppSecurityTransactionActionDto.ActionType = aAppSecurityTransactionActionEntity.ActionType;
 			aAppSecurityTransactionActionDto.IsNeedSecurityControl = aAppSecurityTransactionActionEntity.IsNeedSecurityControl;
 			aAppSecurityTransactionActionDto.AppCreatedById = aAppSecurityTransactionActionEntity.AppCreatedById;
 			aAppSecurityTransactionActionDto.AppCreatedDate = aAppSecurityTransactionActionEntity.AppCreatedDate;
 			aAppSecurityTransactionActionDto.AppModifiedDate = aAppSecurityTransactionActionEntity.AppModifiedDate;
 			aAppSecurityTransactionActionDto.AppModifiedById = aAppSecurityTransactionActionEntity.AppModifiedById;
 			aAppSecurityTransactionActionDto.AppCreatedByCompanyId = aAppSecurityTransactionActionEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSecurityTransactionActionDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityTransactionActionEntity.AppCreatedDate);
                aAppSecurityTransactionActionDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityTransactionActionEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSecurityTransactionActionEntity, aAppSecurityTransactionActionDto);
		}
		
		 /// <summary>
        ///  Copy AppSecurityTransactionActionDto Properties to   AppSecurityTransactionActionEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSecurityTransactionActionEntity aAppSecurityTransactionActionEntity,AppSecurityTransactionActionDto aAppSecurityTransactionActionDto)
        {        
 
      			aAppSecurityTransactionActionEntity.Name = aAppSecurityTransactionActionDto.Name;
      			aAppSecurityTransactionActionEntity.Description = aAppSecurityTransactionActionDto.Description;
      			aAppSecurityTransactionActionEntity.TransactionId = aAppSecurityTransactionActionDto.TransactionId;
      			aAppSecurityTransactionActionEntity.ActionType = aAppSecurityTransactionActionDto.ActionType;
      			aAppSecurityTransactionActionEntity.IsNeedSecurityControl = aAppSecurityTransactionActionDto.IsNeedSecurityControl;
 
  
   
    
      			aAppSecurityTransactionActionEntity.AppCreatedByCompanyId = aAppSecurityTransactionActionDto.AppCreatedByCompanyId;
			
			if(aAppSecurityTransactionActionDto.Id == null)
			{
				aAppSecurityTransactionActionEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityTransactionActionEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSecurityTransactionActionEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityTransactionActionEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityTransactionActionEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSecurityTransactionActionEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityTransactionActionEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSecurityTransactionActionEntity, aAppSecurityTransactionActionDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSecurityTransactionActionEntity aAppSecurityTransactionActionEntity,AppSecurityTransactionActionDto aAppSecurityTransactionActionDto);
		
		static partial void OnCopyDtoToEntityDone(AppSecurityTransactionActionEntity aAppSecurityTransactionActionEntity,AppSecurityTransactionActionDto aAppSecurityTransactionActionDto);
		
   
       
    }
}

 