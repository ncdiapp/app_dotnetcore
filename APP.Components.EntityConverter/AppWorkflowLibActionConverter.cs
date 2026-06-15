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
    /// Convert Properties between  AppWorkflowLibActionEntity and  AppWorkflowLibActionDto
    /// </summary>
    public static partial class AppWorkflowLibActionConverter 
    {
         /// <summary>
        ///  Convert AppWorkflowLibActionEntity To  AppWorkflowLibActionDto
        /// </summary>
        public static AppWorkflowLibActionDto ConvertEntityToDto(AppWorkflowLibActionEntity aAppWorkflowLibActionEntity)
        {        
    		AppWorkflowLibActionDto aAppWorkflowLibActionDto = new AppWorkflowLibActionDto();
    		CopyEntityPropertyToDto( aAppWorkflowLibActionEntity, aAppWorkflowLibActionDto);          
			return aAppWorkflowLibActionDto;
        }
		 /// <summary>
        ///  Convert AppWorkflowLibActionEntity To  AppWorkflowLibActionExDto
        /// </summary>
        public static AppWorkflowLibActionExDto ConvertEntityToExDto(AppWorkflowLibActionEntity aAppWorkflowLibActionEntity)
        {        
    		AppWorkflowLibActionExDto aAppWorkflowLibActionExDto = new AppWorkflowLibActionExDto();
			CopyEntityPropertyToDto( aAppWorkflowLibActionEntity, aAppWorkflowLibActionExDto);
			
			
			
            return aAppWorkflowLibActionExDto;
        }
		
		 /// <summary>
        ///  Convert AppWorkflowLibActionEntity To  AppWorkflowLibActionDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppWorkflowLibActionEntity aAppWorkflowLibActionEntity,AppWorkflowLibActionDto aAppWorkflowLibActionDto)
        {        
    		
           // aAppWorkflowLibActionDto.StopChangeTracking();
 			aAppWorkflowLibActionDto.Id = aAppWorkflowLibActionEntity.WorkflowAtionId;
 			aAppWorkflowLibActionDto.Name = aAppWorkflowLibActionEntity.Name;
 			aAppWorkflowLibActionDto.Description = aAppWorkflowLibActionEntity.Description;
 			aAppWorkflowLibActionDto.ActionType = aAppWorkflowLibActionEntity.ActionType;
 			aAppWorkflowLibActionDto.RowIdentity = aAppWorkflowLibActionEntity.RowIdentity;
 			aAppWorkflowLibActionDto.NotificationSubject = aAppWorkflowLibActionEntity.NotificationSubject;
 			aAppWorkflowLibActionDto.NotificationMessage = aAppWorkflowLibActionEntity.NotificationMessage;
 			aAppWorkflowLibActionDto.NotificationDestination = aAppWorkflowLibActionEntity.NotificationDestination;
 			aAppWorkflowLibActionDto.NotificationDestinationUserIdtransactionFiledId = aAppWorkflowLibActionEntity.NotificationDestinationUserIdtransactionFiledId;
 			aAppWorkflowLibActionDto.NotificationDestinationRoleIdtransactionFiledId = aAppWorkflowLibActionEntity.NotificationDestinationRoleIdtransactionFiledId;
 			aAppWorkflowLibActionDto.MessageContentQueryDataSetId = aAppWorkflowLibActionEntity.MessageContentQueryDataSetId;
 			aAppWorkflowLibActionDto.DataSetQeuryString = aAppWorkflowLibActionEntity.DataSetQeuryString;
 			aAppWorkflowLibActionDto.TransactionId = aAppWorkflowLibActionEntity.TransactionId;
 			aAppWorkflowLibActionDto.TransactionUnitId = aAppWorkflowLibActionEntity.TransactionUnitId;
 			aAppWorkflowLibActionDto.TransactionFieldId = aAppWorkflowLibActionEntity.TransactionFieldId;
 			aAppWorkflowLibActionDto.ChangeTypeForTransactionUnitField = aAppWorkflowLibActionEntity.ChangeTypeForTransactionUnitField;
 			aAppWorkflowLibActionDto.MessageTemplateId = aAppWorkflowLibActionEntity.MessageTemplateId;
 			aAppWorkflowLibActionDto.IsNeedToAttachForm = aAppWorkflowLibActionEntity.IsNeedToAttachForm;
 			aAppWorkflowLibActionDto.IsNeedToAttachAllFormFiles = aAppWorkflowLibActionEntity.IsNeedToAttachAllFormFiles;
 			aAppWorkflowLibActionDto.AppCreatedById = aAppWorkflowLibActionEntity.AppCreatedById;
 			aAppWorkflowLibActionDto.AppCreatedDate = aAppWorkflowLibActionEntity.AppCreatedDate;
 			aAppWorkflowLibActionDto.AppModifiedDate = aAppWorkflowLibActionEntity.AppModifiedDate;
 			aAppWorkflowLibActionDto.AppModifiedById = aAppWorkflowLibActionEntity.AppModifiedById;
 			aAppWorkflowLibActionDto.AppCreatedByCompanyId = aAppWorkflowLibActionEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppWorkflowLibActionDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppWorkflowLibActionEntity.AppCreatedDate);
                aAppWorkflowLibActionDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppWorkflowLibActionEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppWorkflowLibActionEntity, aAppWorkflowLibActionDto);
		}
		
		 /// <summary>
        ///  Copy AppWorkflowLibActionDto Properties to   AppWorkflowLibActionEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppWorkflowLibActionEntity aAppWorkflowLibActionEntity,AppWorkflowLibActionDto aAppWorkflowLibActionDto)
        {        
 
      			aAppWorkflowLibActionEntity.Name = aAppWorkflowLibActionDto.Name;
      			aAppWorkflowLibActionEntity.Description = aAppWorkflowLibActionDto.Description;
      			aAppWorkflowLibActionEntity.ActionType = aAppWorkflowLibActionDto.ActionType;
      			aAppWorkflowLibActionEntity.RowIdentity = aAppWorkflowLibActionDto.RowIdentity;
      			aAppWorkflowLibActionEntity.NotificationSubject = aAppWorkflowLibActionDto.NotificationSubject;
      			aAppWorkflowLibActionEntity.NotificationMessage = aAppWorkflowLibActionDto.NotificationMessage;
      			aAppWorkflowLibActionEntity.NotificationDestination = aAppWorkflowLibActionDto.NotificationDestination;
      			aAppWorkflowLibActionEntity.NotificationDestinationUserIdtransactionFiledId = aAppWorkflowLibActionDto.NotificationDestinationUserIdtransactionFiledId;
      			aAppWorkflowLibActionEntity.NotificationDestinationRoleIdtransactionFiledId = aAppWorkflowLibActionDto.NotificationDestinationRoleIdtransactionFiledId;
      			aAppWorkflowLibActionEntity.MessageContentQueryDataSetId = aAppWorkflowLibActionDto.MessageContentQueryDataSetId;
      			aAppWorkflowLibActionEntity.DataSetQeuryString = aAppWorkflowLibActionDto.DataSetQeuryString;
      			aAppWorkflowLibActionEntity.TransactionId = aAppWorkflowLibActionDto.TransactionId;
      			aAppWorkflowLibActionEntity.TransactionUnitId = aAppWorkflowLibActionDto.TransactionUnitId;
      			aAppWorkflowLibActionEntity.TransactionFieldId = aAppWorkflowLibActionDto.TransactionFieldId;
      			aAppWorkflowLibActionEntity.ChangeTypeForTransactionUnitField = aAppWorkflowLibActionDto.ChangeTypeForTransactionUnitField;
      			aAppWorkflowLibActionEntity.MessageTemplateId = aAppWorkflowLibActionDto.MessageTemplateId;
      			aAppWorkflowLibActionEntity.IsNeedToAttachForm = aAppWorkflowLibActionDto.IsNeedToAttachForm;
      			aAppWorkflowLibActionEntity.IsNeedToAttachAllFormFiles = aAppWorkflowLibActionDto.IsNeedToAttachAllFormFiles;
 
  
   
    
      			aAppWorkflowLibActionEntity.AppCreatedByCompanyId = aAppWorkflowLibActionDto.AppCreatedByCompanyId;
			
			if(aAppWorkflowLibActionDto.Id == null)
			{
				aAppWorkflowLibActionEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppWorkflowLibActionEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppWorkflowLibActionEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppWorkflowLibActionEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppWorkflowLibActionEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppWorkflowLibActionEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppWorkflowLibActionEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppWorkflowLibActionEntity, aAppWorkflowLibActionDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppWorkflowLibActionEntity aAppWorkflowLibActionEntity,AppWorkflowLibActionDto aAppWorkflowLibActionDto);
		
		static partial void OnCopyDtoToEntityDone(AppWorkflowLibActionEntity aAppWorkflowLibActionEntity,AppWorkflowLibActionDto aAppWorkflowLibActionDto);
		
   
       
    }
}

 