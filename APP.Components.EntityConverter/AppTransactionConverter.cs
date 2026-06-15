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
    /// Convert Properties between  AppTransactionEntity and  AppTransactionDto
    /// </summary>
    public static partial class AppTransactionConverter 
    {
         /// <summary>
        ///  Convert AppTransactionEntity To  AppTransactionDto
        /// </summary>
        public static AppTransactionDto ConvertEntityToDto(AppTransactionEntity aAppTransactionEntity)
        {        
    		AppTransactionDto aAppTransactionDto = new AppTransactionDto();
    		CopyEntityPropertyToDto( aAppTransactionEntity, aAppTransactionDto);          
			return aAppTransactionDto;
        }
		 /// <summary>
        ///  Convert AppTransactionEntity To  AppTransactionExDto
        /// </summary>
        public static AppTransactionExDto ConvertEntityToExDto(AppTransactionEntity aAppTransactionEntity)
        {        
    		AppTransactionExDto aAppTransactionExDto = new AppTransactionExDto();
			CopyEntityPropertyToDto( aAppTransactionEntity, aAppTransactionExDto);
			
			
			
            return aAppTransactionExDto;
        }
		
		 /// <summary>
        ///  Convert AppTransactionEntity To  AppTransactionDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppTransactionEntity aAppTransactionEntity,AppTransactionDto aAppTransactionDto)
        {        
    		
           // aAppTransactionDto.StopChangeTracking();
 			aAppTransactionDto.Id = aAppTransactionEntity.TransactionId;
 			aAppTransactionDto.DataSourceFrom = aAppTransactionEntity.DataSourceFrom;
 			aAppTransactionDto.TransactionName = aAppTransactionEntity.TransactionName;
 			aAppTransactionDto.Description = aAppTransactionEntity.Description;
 			aAppTransactionDto.NeedToCheckRowVersion = aAppTransactionEntity.NeedToCheckRowVersion;
 			aAppTransactionDto.TransactionOrganizedType = aAppTransactionEntity.TransactionOrganizedType;
 			aAppTransactionDto.FolderUsageType = aAppTransactionEntity.FolderUsageType;
 			aAppTransactionDto.PreSaveValidationMethod = aAppTransactionEntity.PreSaveValidationMethod;
 			aAppTransactionDto.PostProcessStoreProcedure = aAppTransactionEntity.PostProcessStoreProcedure;
 			aAppTransactionDto.ListFilterWhereClause = aAppTransactionEntity.ListFilterWhereClause;
 			aAppTransactionDto.IsReadOnly = aAppTransactionEntity.IsReadOnly;
 			aAppTransactionDto.FormId = aAppTransactionEntity.FormId;
 			aAppTransactionDto.BusinessScopeId = aAppTransactionEntity.BusinessScopeId;
 			aAppTransactionDto.PrintFormId = aAppTransactionEntity.PrintFormId;
 			aAppTransactionDto.IsEnableFolderSecurity = aAppTransactionEntity.IsEnableFolderSecurity;
 			aAppTransactionDto.IsSystemBuitIn = aAppTransactionEntity.IsSystemBuitIn;
 			aAppTransactionDto.IsNeedToSetCriticalPathTrackFlow = aAppTransactionEntity.IsNeedToSetCriticalPathTrackFlow;
 			aAppTransactionDto.IsNeedToSetComunication = aAppTransactionEntity.IsNeedToSetComunication;
 			aAppTransactionDto.ConversationBoxDockPosition = aAppTransactionEntity.ConversationBoxDockPosition;
 			aAppTransactionDto.FolderTransactionId = aAppTransactionEntity.FolderTransactionId;
 			aAppTransactionDto.EmAppTransBusinessType = aAppTransactionEntity.EmAppTransBusinessType;
 			aAppTransactionDto.LogicalDisplayEntityId = aAppTransactionEntity.LogicalDisplayEntityId;
 			aAppTransactionDto.MgtRootFolderId = aAppTransactionEntity.MgtRootFolderId;
 			aAppTransactionDto.TransactionFileStorageRootFolderId = aAppTransactionEntity.TransactionFileStorageRootFolderId;
 			aAppTransactionDto.AppCreatedById = aAppTransactionEntity.AppCreatedById;
 			aAppTransactionDto.AppCreatedDate = aAppTransactionEntity.AppCreatedDate;
 			aAppTransactionDto.AppModifiedDate = aAppTransactionEntity.AppModifiedDate;
 			aAppTransactionDto.AppModifiedById = aAppTransactionEntity.AppModifiedById;
 			aAppTransactionDto.AppCreatedByCompanyId = aAppTransactionEntity.AppCreatedByCompanyId;
 			aAppTransactionDto.IsExclusiveForOwner = aAppTransactionEntity.IsExclusiveForOwner;
 			aAppTransactionDto.MasterWorkflowId = aAppTransactionEntity.MasterWorkflowId;
 			aAppTransactionDto.MasterTransactionId = aAppTransactionEntity.MasterTransactionId;
 			aAppTransactionDto.EmGrandChildEditMode = aAppTransactionEntity.EmGrandChildEditMode;
 			aAppTransactionDto.IsPhysicalModelTableCreated = aAppTransactionEntity.IsPhysicalModelTableCreated;
 			aAppTransactionDto.IsAllowSaveAs = aAppTransactionEntity.IsAllowSaveAs;
 			aAppTransactionDto.FormTitleDisplayFieldId = aAppTransactionEntity.FormTitleDisplayFieldId;
 			aAppTransactionDto.IsShowSaveButton = aAppTransactionEntity.IsShowSaveButton;
 			aAppTransactionDto.IsShowCalculateButton = aAppTransactionEntity.IsShowCalculateButton;
 			aAppTransactionDto.IsShowPrintButton = aAppTransactionEntity.IsShowPrintButton;
 			aAppTransactionDto.SaasApplicationId = aAppTransactionEntity.SaasApplicationId;
 			aAppTransactionDto.IsForPublicAcesss = aAppTransactionEntity.IsForPublicAcesss;
 			aAppTransactionDto.EmNotificaionMethod = aAppTransactionEntity.EmNotificaionMethod;
 			aAppTransactionDto.NotificationSetting = aAppTransactionEntity.NotificationSetting;
 			aAppTransactionDto.WebApiConfigId = aAppTransactionEntity.WebApiConfigId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppTransactionDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionEntity.AppCreatedDate);
                aAppTransactionDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppTransactionEntity, aAppTransactionDto);
		}
		
		 /// <summary>
        ///  Copy AppTransactionDto Properties to   AppTransactionEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppTransactionEntity aAppTransactionEntity,AppTransactionDto aAppTransactionDto)
        {        
 
      			aAppTransactionEntity.DataSourceFrom = aAppTransactionDto.DataSourceFrom;
      			aAppTransactionEntity.TransactionName = aAppTransactionDto.TransactionName;
      			aAppTransactionEntity.Description = aAppTransactionDto.Description;
      			aAppTransactionEntity.NeedToCheckRowVersion = aAppTransactionDto.NeedToCheckRowVersion;
      			aAppTransactionEntity.TransactionOrganizedType = aAppTransactionDto.TransactionOrganizedType;
      			aAppTransactionEntity.FolderUsageType = aAppTransactionDto.FolderUsageType;
      			aAppTransactionEntity.PreSaveValidationMethod = aAppTransactionDto.PreSaveValidationMethod;
      			aAppTransactionEntity.PostProcessStoreProcedure = aAppTransactionDto.PostProcessStoreProcedure;
      			aAppTransactionEntity.ListFilterWhereClause = aAppTransactionDto.ListFilterWhereClause;
      			aAppTransactionEntity.IsReadOnly = aAppTransactionDto.IsReadOnly;
      			aAppTransactionEntity.FormId = aAppTransactionDto.FormId;
      			aAppTransactionEntity.BusinessScopeId = aAppTransactionDto.BusinessScopeId;
      			aAppTransactionEntity.PrintFormId = aAppTransactionDto.PrintFormId;
      			aAppTransactionEntity.IsEnableFolderSecurity = aAppTransactionDto.IsEnableFolderSecurity;
      			aAppTransactionEntity.IsSystemBuitIn = aAppTransactionDto.IsSystemBuitIn;
      			aAppTransactionEntity.IsNeedToSetCriticalPathTrackFlow = aAppTransactionDto.IsNeedToSetCriticalPathTrackFlow;
      			aAppTransactionEntity.IsNeedToSetComunication = aAppTransactionDto.IsNeedToSetComunication;
      			aAppTransactionEntity.ConversationBoxDockPosition = aAppTransactionDto.ConversationBoxDockPosition;
      			aAppTransactionEntity.FolderTransactionId = aAppTransactionDto.FolderTransactionId;
      			aAppTransactionEntity.EmAppTransBusinessType = aAppTransactionDto.EmAppTransBusinessType;
      			aAppTransactionEntity.LogicalDisplayEntityId = aAppTransactionDto.LogicalDisplayEntityId;
      			aAppTransactionEntity.MgtRootFolderId = aAppTransactionDto.MgtRootFolderId;
      			aAppTransactionEntity.TransactionFileStorageRootFolderId = aAppTransactionDto.TransactionFileStorageRootFolderId;
 
  
   
    
      			aAppTransactionEntity.AppCreatedByCompanyId = aAppTransactionDto.AppCreatedByCompanyId;
      			aAppTransactionEntity.IsExclusiveForOwner = aAppTransactionDto.IsExclusiveForOwner;
      			aAppTransactionEntity.MasterWorkflowId = aAppTransactionDto.MasterWorkflowId;
      			aAppTransactionEntity.MasterTransactionId = aAppTransactionDto.MasterTransactionId;
      			aAppTransactionEntity.EmGrandChildEditMode = aAppTransactionDto.EmGrandChildEditMode;
      			aAppTransactionEntity.IsPhysicalModelTableCreated = aAppTransactionDto.IsPhysicalModelTableCreated;
      			aAppTransactionEntity.IsAllowSaveAs = aAppTransactionDto.IsAllowSaveAs;
      			aAppTransactionEntity.FormTitleDisplayFieldId = aAppTransactionDto.FormTitleDisplayFieldId;
      			aAppTransactionEntity.IsShowSaveButton = aAppTransactionDto.IsShowSaveButton;
      			aAppTransactionEntity.IsShowCalculateButton = aAppTransactionDto.IsShowCalculateButton;
      			aAppTransactionEntity.IsShowPrintButton = aAppTransactionDto.IsShowPrintButton;
      			aAppTransactionEntity.SaasApplicationId = aAppTransactionDto.SaasApplicationId;
      			aAppTransactionEntity.IsForPublicAcesss = aAppTransactionDto.IsForPublicAcesss;
      			aAppTransactionEntity.EmNotificaionMethod = aAppTransactionDto.EmNotificaionMethod;
      			aAppTransactionEntity.NotificationSetting = aAppTransactionDto.NotificationSetting;
      			aAppTransactionEntity.WebApiConfigId = aAppTransactionDto.WebApiConfigId;
			
			if(aAppTransactionDto.Id == null)
			{
				aAppTransactionEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppTransactionEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppTransactionEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppTransactionEntity, aAppTransactionDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppTransactionEntity aAppTransactionEntity,AppTransactionDto aAppTransactionDto);
		
		static partial void OnCopyDtoToEntityDone(AppTransactionEntity aAppTransactionEntity,AppTransactionDto aAppTransactionDto);
		
   
       
    }
}

 