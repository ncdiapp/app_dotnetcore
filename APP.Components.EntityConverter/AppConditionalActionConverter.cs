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
    /// Convert Properties between  AppConditionalActionEntity and  AppConditionalActionDto
    /// </summary>
    public static partial class AppConditionalActionConverter 
    {
         /// <summary>
        ///  Convert AppConditionalActionEntity To  AppConditionalActionDto
        /// </summary>
        public static AppConditionalActionDto ConvertEntityToDto(AppConditionalActionEntity aAppConditionalActionEntity)
        {        
    		AppConditionalActionDto aAppConditionalActionDto = new AppConditionalActionDto();
    		CopyEntityPropertyToDto( aAppConditionalActionEntity, aAppConditionalActionDto);          
			return aAppConditionalActionDto;
        }
		 /// <summary>
        ///  Convert AppConditionalActionEntity To  AppConditionalActionExDto
        /// </summary>
        public static AppConditionalActionExDto ConvertEntityToExDto(AppConditionalActionEntity aAppConditionalActionEntity)
        {        
    		AppConditionalActionExDto aAppConditionalActionExDto = new AppConditionalActionExDto();
			CopyEntityPropertyToDto( aAppConditionalActionEntity, aAppConditionalActionExDto);
			
			
			
            return aAppConditionalActionExDto;
        }
		
		 /// <summary>
        ///  Convert AppConditionalActionEntity To  AppConditionalActionDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppConditionalActionEntity aAppConditionalActionEntity,AppConditionalActionDto aAppConditionalActionDto)
        {        
    		
           // aAppConditionalActionDto.StopChangeTracking();
 			aAppConditionalActionDto.Id = aAppConditionalActionEntity.ActionId;
 			aAppConditionalActionDto.Name = aAppConditionalActionEntity.Name;
 			aAppConditionalActionDto.TransactionId = aAppConditionalActionEntity.TransactionId;
 			aAppConditionalActionDto.ConditionUnitId = aAppConditionalActionEntity.ConditionUnitId;
 			aAppConditionalActionDto.BooleanConditionFieldId = aAppConditionalActionEntity.BooleanConditionFieldId;
 			aAppConditionalActionDto.UitriggerTransactionFieldId = aAppConditionalActionEntity.UitriggerTransactionFieldId;
 			aAppConditionalActionDto.BooleanConditionFormula = aAppConditionalActionEntity.BooleanConditionFormula;
 			aAppConditionalActionDto.LockingTransactionFieldId = aAppConditionalActionEntity.LockingTransactionFieldId;
 			aAppConditionalActionDto.LockingFieldUnitId = aAppConditionalActionEntity.LockingFieldUnitId;
 			aAppConditionalActionDto.IsLockingTransaction = aAppConditionalActionEntity.IsLockingTransaction;
 			aAppConditionalActionDto.LockingTransactionUnitId = aAppConditionalActionEntity.LockingTransactionUnitId;
 			aAppConditionalActionDto.NotificationTemplateMessgeId = aAppConditionalActionEntity.NotificationTemplateMessgeId;
 			aAppConditionalActionDto.AppCreatedById = aAppConditionalActionEntity.AppCreatedById;
 			aAppConditionalActionDto.AppCreatedDate = aAppConditionalActionEntity.AppCreatedDate;
 			aAppConditionalActionDto.AppModifiedDate = aAppConditionalActionEntity.AppModifiedDate;
 			aAppConditionalActionDto.AppModifiedById = aAppConditionalActionEntity.AppModifiedById;
 			aAppConditionalActionDto.AppCreatedByCompanyId = aAppConditionalActionEntity.AppCreatedByCompanyId;
 			aAppConditionalActionDto.IsLockForSpecailEditPrivilege = aAppConditionalActionEntity.IsLockForSpecailEditPrivilege;
 			aAppConditionalActionDto.NeedToHideTransactionFieldId = aAppConditionalActionEntity.NeedToHideTransactionFieldId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppConditionalActionDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppConditionalActionEntity.AppCreatedDate);
                aAppConditionalActionDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppConditionalActionEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppConditionalActionEntity, aAppConditionalActionDto);
		}
		
		 /// <summary>
        ///  Copy AppConditionalActionDto Properties to   AppConditionalActionEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppConditionalActionEntity aAppConditionalActionEntity,AppConditionalActionDto aAppConditionalActionDto)
        {        
 
      			aAppConditionalActionEntity.Name = aAppConditionalActionDto.Name;
      			aAppConditionalActionEntity.TransactionId = aAppConditionalActionDto.TransactionId;
      			aAppConditionalActionEntity.ConditionUnitId = aAppConditionalActionDto.ConditionUnitId;
      			aAppConditionalActionEntity.BooleanConditionFieldId = aAppConditionalActionDto.BooleanConditionFieldId;
      			aAppConditionalActionEntity.UitriggerTransactionFieldId = aAppConditionalActionDto.UitriggerTransactionFieldId;
      			aAppConditionalActionEntity.BooleanConditionFormula = aAppConditionalActionDto.BooleanConditionFormula;
      			aAppConditionalActionEntity.LockingTransactionFieldId = aAppConditionalActionDto.LockingTransactionFieldId;
      			aAppConditionalActionEntity.LockingFieldUnitId = aAppConditionalActionDto.LockingFieldUnitId;
      			aAppConditionalActionEntity.IsLockingTransaction = aAppConditionalActionDto.IsLockingTransaction;
      			aAppConditionalActionEntity.LockingTransactionUnitId = aAppConditionalActionDto.LockingTransactionUnitId;
      			aAppConditionalActionEntity.NotificationTemplateMessgeId = aAppConditionalActionDto.NotificationTemplateMessgeId;
 
  
   
    
      			aAppConditionalActionEntity.AppCreatedByCompanyId = aAppConditionalActionDto.AppCreatedByCompanyId;
      			aAppConditionalActionEntity.IsLockForSpecailEditPrivilege = aAppConditionalActionDto.IsLockForSpecailEditPrivilege;
      			aAppConditionalActionEntity.NeedToHideTransactionFieldId = aAppConditionalActionDto.NeedToHideTransactionFieldId;
			
			if(aAppConditionalActionDto.Id == null)
			{
				aAppConditionalActionEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppConditionalActionEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppConditionalActionEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppConditionalActionEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppConditionalActionEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppConditionalActionEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppConditionalActionEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppConditionalActionEntity, aAppConditionalActionDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppConditionalActionEntity aAppConditionalActionEntity,AppConditionalActionDto aAppConditionalActionDto);
		
		static partial void OnCopyDtoToEntityDone(AppConditionalActionEntity aAppConditionalActionEntity,AppConditionalActionDto aAppConditionalActionDto);
		
   
       
    }
}

 