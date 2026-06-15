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
    /// Convert Properties between  AppProjectWorkFlowActionEntity and  AppProjectWorkFlowActionDto
    /// </summary>
    public static partial class AppProjectWorkFlowActionConverter 
    {
         /// <summary>
        ///  Convert AppProjectWorkFlowActionEntity To  AppProjectWorkFlowActionDto
        /// </summary>
        public static AppProjectWorkFlowActionDto ConvertEntityToDto(AppProjectWorkFlowActionEntity aAppProjectWorkFlowActionEntity)
        {        
    		AppProjectWorkFlowActionDto aAppProjectWorkFlowActionDto = new AppProjectWorkFlowActionDto();
    		CopyEntityPropertyToDto( aAppProjectWorkFlowActionEntity, aAppProjectWorkFlowActionDto);          
			return aAppProjectWorkFlowActionDto;
        }
		 /// <summary>
        ///  Convert AppProjectWorkFlowActionEntity To  AppProjectWorkFlowActionExDto
        /// </summary>
        public static AppProjectWorkFlowActionExDto ConvertEntityToExDto(AppProjectWorkFlowActionEntity aAppProjectWorkFlowActionEntity)
        {        
    		AppProjectWorkFlowActionExDto aAppProjectWorkFlowActionExDto = new AppProjectWorkFlowActionExDto();
			CopyEntityPropertyToDto( aAppProjectWorkFlowActionEntity, aAppProjectWorkFlowActionExDto);
			
			
			
            return aAppProjectWorkFlowActionExDto;
        }
		
		 /// <summary>
        ///  Convert AppProjectWorkFlowActionEntity To  AppProjectWorkFlowActionDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppProjectWorkFlowActionEntity aAppProjectWorkFlowActionEntity,AppProjectWorkFlowActionDto aAppProjectWorkFlowActionDto)
        {        
    		
           // aAppProjectWorkFlowActionDto.StopChangeTracking();
 			aAppProjectWorkFlowActionDto.Id = aAppProjectWorkFlowActionEntity.WorkFlowActionId;
 			aAppProjectWorkFlowActionDto.WorkFlowConditionId = aAppProjectWorkFlowActionEntity.WorkFlowConditionId;
 			aAppProjectWorkFlowActionDto.CommandTransactionId = aAppProjectWorkFlowActionEntity.CommandTransactionId;
 			aAppProjectWorkFlowActionDto.CommandConditionTransactionFieldId = aAppProjectWorkFlowActionEntity.CommandConditionTransactionFieldId;
 			aAppProjectWorkFlowActionDto.Name = aAppProjectWorkFlowActionEntity.Name;
 			aAppProjectWorkFlowActionDto.Description = aAppProjectWorkFlowActionEntity.Description;
 			aAppProjectWorkFlowActionDto.ActionType = aAppProjectWorkFlowActionEntity.ActionType;
 			aAppProjectWorkFlowActionDto.UpdateActionTransactionFieldId = aAppProjectWorkFlowActionEntity.UpdateActionTransactionFieldId;
 			aAppProjectWorkFlowActionDto.FormulaExpression = aAppProjectWorkFlowActionEntity.FormulaExpression;
 			aAppProjectWorkFlowActionDto.NextWorkFlowId = aAppProjectWorkFlowActionEntity.NextWorkFlowId;
 			aAppProjectWorkFlowActionDto.RowIdentity = aAppProjectWorkFlowActionEntity.RowIdentity;
 			aAppProjectWorkFlowActionDto.NextTransactionRid = aAppProjectWorkFlowActionEntity.NextTransactionRid;
 			aAppProjectWorkFlowActionDto.NextTransactionId = aAppProjectWorkFlowActionEntity.NextTransactionId;
 			aAppProjectWorkFlowActionDto.NextProjectId = aAppProjectWorkFlowActionEntity.NextProjectId;
 			aAppProjectWorkFlowActionDto.ExcutionDateTime = aAppProjectWorkFlowActionEntity.ExcutionDateTime;
 			aAppProjectWorkFlowActionDto.ExcutedById = aAppProjectWorkFlowActionEntity.ExcutedById;
 			aAppProjectWorkFlowActionDto.NotificationSubject = aAppProjectWorkFlowActionEntity.NotificationSubject;
 			aAppProjectWorkFlowActionDto.NotificationMessage = aAppProjectWorkFlowActionEntity.NotificationMessage;
 			aAppProjectWorkFlowActionDto.NotificationDestination = aAppProjectWorkFlowActionEntity.NotificationDestination;
 			aAppProjectWorkFlowActionDto.NotificationDestinationUserIdtransactionFiledId = aAppProjectWorkFlowActionEntity.NotificationDestinationUserIdtransactionFiledId;
 			aAppProjectWorkFlowActionDto.PathUilayout = aAppProjectWorkFlowActionEntity.PathUilayout;
 			aAppProjectWorkFlowActionDto.ActionFlowOrder = aAppProjectWorkFlowActionEntity.ActionFlowOrder;
 			aAppProjectWorkFlowActionDto.AppCreatedById = aAppProjectWorkFlowActionEntity.AppCreatedById;
 			aAppProjectWorkFlowActionDto.AppCreatedDate = aAppProjectWorkFlowActionEntity.AppCreatedDate;
 			aAppProjectWorkFlowActionDto.AppModifiedDate = aAppProjectWorkFlowActionEntity.AppModifiedDate;
 			aAppProjectWorkFlowActionDto.AppModifiedById = aAppProjectWorkFlowActionEntity.AppModifiedById;
 			aAppProjectWorkFlowActionDto.AppCreatedByCompanyId = aAppProjectWorkFlowActionEntity.AppCreatedByCompanyId;
 			aAppProjectWorkFlowActionDto.NotificationDestinationRoleIdtransactionFiledId = aAppProjectWorkFlowActionEntity.NotificationDestinationRoleIdtransactionFiledId;
 			aAppProjectWorkFlowActionDto.MessageContentQueryDataSetId = aAppProjectWorkFlowActionEntity.MessageContentQueryDataSetId;
 			aAppProjectWorkFlowActionDto.DataSetQeuryString = aAppProjectWorkFlowActionEntity.DataSetQeuryString;
 			aAppProjectWorkFlowActionDto.TransactionId = aAppProjectWorkFlowActionEntity.TransactionId;
 			aAppProjectWorkFlowActionDto.TransactionUnitId = aAppProjectWorkFlowActionEntity.TransactionUnitId;
 			aAppProjectWorkFlowActionDto.TransactionFieldId = aAppProjectWorkFlowActionEntity.TransactionFieldId;
 			aAppProjectWorkFlowActionDto.ChangeTypeForTransactionUnitField = aAppProjectWorkFlowActionEntity.ChangeTypeForTransactionUnitField;
 			aAppProjectWorkFlowActionDto.MessageTemplateId = aAppProjectWorkFlowActionEntity.MessageTemplateId;
 			aAppProjectWorkFlowActionDto.IsNeedToAttachForm = aAppProjectWorkFlowActionEntity.IsNeedToAttachForm;
 			aAppProjectWorkFlowActionDto.IsNeedToAttachAllFormFiles = aAppProjectWorkFlowActionEntity.IsNeedToAttachAllFormFiles;
 			aAppProjectWorkFlowActionDto.DataLoadId = aAppProjectWorkFlowActionEntity.DataLoadId;
 			aAppProjectWorkFlowActionDto.ActionGuid = aAppProjectWorkFlowActionEntity.ActionGuid;
 			aAppProjectWorkFlowActionDto.DataTransferSettingId = aAppProjectWorkFlowActionEntity.DataTransferSettingId;
 			aAppProjectWorkFlowActionDto.OtherOptions = aAppProjectWorkFlowActionEntity.OtherOptions;
 			aAppProjectWorkFlowActionDto.CommandUioption = aAppProjectWorkFlowActionEntity.CommandUioption;
 			aAppProjectWorkFlowActionDto.CommandSearchViewId = aAppProjectWorkFlowActionEntity.CommandSearchViewId;
 			aAppProjectWorkFlowActionDto.CommandConditionExpression = aAppProjectWorkFlowActionEntity.CommandConditionExpression;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppProjectWorkFlowActionDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectWorkFlowActionEntity.AppCreatedDate);
                aAppProjectWorkFlowActionDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectWorkFlowActionEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppProjectWorkFlowActionEntity, aAppProjectWorkFlowActionDto);
		}
		
		 /// <summary>
        ///  Copy AppProjectWorkFlowActionDto Properties to   AppProjectWorkFlowActionEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppProjectWorkFlowActionEntity aAppProjectWorkFlowActionEntity,AppProjectWorkFlowActionDto aAppProjectWorkFlowActionDto)
        {        
 
      			aAppProjectWorkFlowActionEntity.WorkFlowConditionId = aAppProjectWorkFlowActionDto.WorkFlowConditionId;
      			aAppProjectWorkFlowActionEntity.CommandTransactionId = aAppProjectWorkFlowActionDto.CommandTransactionId;
      			aAppProjectWorkFlowActionEntity.CommandConditionTransactionFieldId = aAppProjectWorkFlowActionDto.CommandConditionTransactionFieldId;
      			aAppProjectWorkFlowActionEntity.Name = aAppProjectWorkFlowActionDto.Name;
      			aAppProjectWorkFlowActionEntity.Description = aAppProjectWorkFlowActionDto.Description;
      			aAppProjectWorkFlowActionEntity.ActionType = aAppProjectWorkFlowActionDto.ActionType;
      			aAppProjectWorkFlowActionEntity.UpdateActionTransactionFieldId = aAppProjectWorkFlowActionDto.UpdateActionTransactionFieldId;
      			aAppProjectWorkFlowActionEntity.FormulaExpression = aAppProjectWorkFlowActionDto.FormulaExpression;
      			aAppProjectWorkFlowActionEntity.NextWorkFlowId = aAppProjectWorkFlowActionDto.NextWorkFlowId;
      			aAppProjectWorkFlowActionEntity.RowIdentity = aAppProjectWorkFlowActionDto.RowIdentity;
      			aAppProjectWorkFlowActionEntity.NextTransactionRid = aAppProjectWorkFlowActionDto.NextTransactionRid;
      			aAppProjectWorkFlowActionEntity.NextTransactionId = aAppProjectWorkFlowActionDto.NextTransactionId;
      			aAppProjectWorkFlowActionEntity.NextProjectId = aAppProjectWorkFlowActionDto.NextProjectId;
      			aAppProjectWorkFlowActionEntity.ExcutionDateTime = aAppProjectWorkFlowActionDto.ExcutionDateTime;
      			aAppProjectWorkFlowActionEntity.ExcutedById = aAppProjectWorkFlowActionDto.ExcutedById;
      			aAppProjectWorkFlowActionEntity.NotificationSubject = aAppProjectWorkFlowActionDto.NotificationSubject;
      			aAppProjectWorkFlowActionEntity.NotificationMessage = aAppProjectWorkFlowActionDto.NotificationMessage;
      			aAppProjectWorkFlowActionEntity.NotificationDestination = aAppProjectWorkFlowActionDto.NotificationDestination;
      			aAppProjectWorkFlowActionEntity.NotificationDestinationUserIdtransactionFiledId = aAppProjectWorkFlowActionDto.NotificationDestinationUserIdtransactionFiledId;
      			aAppProjectWorkFlowActionEntity.PathUilayout = aAppProjectWorkFlowActionDto.PathUilayout;
      			aAppProjectWorkFlowActionEntity.ActionFlowOrder = aAppProjectWorkFlowActionDto.ActionFlowOrder;
 
  
   
    
      			aAppProjectWorkFlowActionEntity.AppCreatedByCompanyId = aAppProjectWorkFlowActionDto.AppCreatedByCompanyId;
      			aAppProjectWorkFlowActionEntity.NotificationDestinationRoleIdtransactionFiledId = aAppProjectWorkFlowActionDto.NotificationDestinationRoleIdtransactionFiledId;
      			aAppProjectWorkFlowActionEntity.MessageContentQueryDataSetId = aAppProjectWorkFlowActionDto.MessageContentQueryDataSetId;
      			aAppProjectWorkFlowActionEntity.DataSetQeuryString = aAppProjectWorkFlowActionDto.DataSetQeuryString;
      			aAppProjectWorkFlowActionEntity.TransactionId = aAppProjectWorkFlowActionDto.TransactionId;
      			aAppProjectWorkFlowActionEntity.TransactionUnitId = aAppProjectWorkFlowActionDto.TransactionUnitId;
      			aAppProjectWorkFlowActionEntity.TransactionFieldId = aAppProjectWorkFlowActionDto.TransactionFieldId;
      			aAppProjectWorkFlowActionEntity.ChangeTypeForTransactionUnitField = aAppProjectWorkFlowActionDto.ChangeTypeForTransactionUnitField;
      			aAppProjectWorkFlowActionEntity.MessageTemplateId = aAppProjectWorkFlowActionDto.MessageTemplateId;
      			aAppProjectWorkFlowActionEntity.IsNeedToAttachForm = aAppProjectWorkFlowActionDto.IsNeedToAttachForm;
      			aAppProjectWorkFlowActionEntity.IsNeedToAttachAllFormFiles = aAppProjectWorkFlowActionDto.IsNeedToAttachAllFormFiles;
      			aAppProjectWorkFlowActionEntity.DataLoadId = aAppProjectWorkFlowActionDto.DataLoadId;
      			aAppProjectWorkFlowActionEntity.ActionGuid = aAppProjectWorkFlowActionDto.ActionGuid;
      			aAppProjectWorkFlowActionEntity.DataTransferSettingId = aAppProjectWorkFlowActionDto.DataTransferSettingId;
      			aAppProjectWorkFlowActionEntity.OtherOptions = aAppProjectWorkFlowActionDto.OtherOptions;
      			aAppProjectWorkFlowActionEntity.CommandUioption = aAppProjectWorkFlowActionDto.CommandUioption;
      			aAppProjectWorkFlowActionEntity.CommandSearchViewId = aAppProjectWorkFlowActionDto.CommandSearchViewId;
      			aAppProjectWorkFlowActionEntity.CommandConditionExpression = aAppProjectWorkFlowActionDto.CommandConditionExpression;
			
			if(aAppProjectWorkFlowActionDto.Id == null)
			{
				aAppProjectWorkFlowActionEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectWorkFlowActionEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppProjectWorkFlowActionEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectWorkFlowActionEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectWorkFlowActionEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppProjectWorkFlowActionEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectWorkFlowActionEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppProjectWorkFlowActionEntity, aAppProjectWorkFlowActionDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppProjectWorkFlowActionEntity aAppProjectWorkFlowActionEntity,AppProjectWorkFlowActionDto aAppProjectWorkFlowActionDto);
		
		static partial void OnCopyDtoToEntityDone(AppProjectWorkFlowActionEntity aAppProjectWorkFlowActionEntity,AppProjectWorkFlowActionDto aAppProjectWorkFlowActionDto);
		
   
       
    }
}

 