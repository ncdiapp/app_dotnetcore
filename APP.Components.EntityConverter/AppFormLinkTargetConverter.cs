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
    /// Convert Properties between  AppFormLinkTargetEntity and  AppFormLinkTargetDto
    /// </summary>
    public static partial class AppFormLinkTargetConverter 
    {
         /// <summary>
        ///  Convert AppFormLinkTargetEntity To  AppFormLinkTargetDto
        /// </summary>
        public static AppFormLinkTargetDto ConvertEntityToDto(AppFormLinkTargetEntity aAppFormLinkTargetEntity)
        {        
    		AppFormLinkTargetDto aAppFormLinkTargetDto = new AppFormLinkTargetDto();
    		CopyEntityPropertyToDto( aAppFormLinkTargetEntity, aAppFormLinkTargetDto);          
			return aAppFormLinkTargetDto;
        }
		 /// <summary>
        ///  Convert AppFormLinkTargetEntity To  AppFormLinkTargetExDto
        /// </summary>
        public static AppFormLinkTargetExDto ConvertEntityToExDto(AppFormLinkTargetEntity aAppFormLinkTargetEntity)
        {        
    		AppFormLinkTargetExDto aAppFormLinkTargetExDto = new AppFormLinkTargetExDto();
			CopyEntityPropertyToDto( aAppFormLinkTargetEntity, aAppFormLinkTargetExDto);
			
			
			
            return aAppFormLinkTargetExDto;
        }
		
		 /// <summary>
        ///  Convert AppFormLinkTargetEntity To  AppFormLinkTargetDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppFormLinkTargetEntity aAppFormLinkTargetEntity,AppFormLinkTargetDto aAppFormLinkTargetDto)
        {        
    		
           // aAppFormLinkTargetDto.StopChangeTracking();
 			aAppFormLinkTargetDto.Id = aAppFormLinkTargetEntity.LinkTargetId;
 			aAppFormLinkTargetDto.SearchViewId = aAppFormLinkTargetEntity.SearchViewId;
 			aAppFormLinkTargetDto.TransactionUnitId = aAppFormLinkTargetEntity.TransactionUnitId;
 			aAppFormLinkTargetDto.SourceColumn1 = aAppFormLinkTargetEntity.SourceColumn1;
 			aAppFormLinkTargetDto.SourceColumn2 = aAppFormLinkTargetEntity.SourceColumn2;
 			aAppFormLinkTargetDto.SourceColumn3 = aAppFormLinkTargetEntity.SourceColumn3;
 			aAppFormLinkTargetDto.TargetColumn1 = aAppFormLinkTargetEntity.TargetColumn1;
 			aAppFormLinkTargetDto.TargetColumn2 = aAppFormLinkTargetEntity.TargetColumn2;
 			aAppFormLinkTargetDto.TargetColumn3 = aAppFormLinkTargetEntity.TargetColumn3;
 			aAppFormLinkTargetDto.NavigationActionName = aAppFormLinkTargetEntity.NavigationActionName;
 			aAppFormLinkTargetDto.ActionType = aAppFormLinkTargetEntity.ActionType;
 			aAppFormLinkTargetDto.IsReadonly = aAppFormLinkTargetEntity.IsReadonly;
 			aAppFormLinkTargetDto.RowDisplayDbField = aAppFormLinkTargetEntity.RowDisplayDbField;
 			aAppFormLinkTargetDto.SourceConditionColumn = aAppFormLinkTargetEntity.SourceConditionColumn;
 			aAppFormLinkTargetDto.ConditionWarningMessage = aAppFormLinkTargetEntity.ConditionWarningMessage;
 			aAppFormLinkTargetDto.GroupName = aAppFormLinkTargetEntity.GroupName;
 			aAppFormLinkTargetDto.LinkTargetTransactionId = aAppFormLinkTargetEntity.LinkTargetTransactionId;
 			aAppFormLinkTargetDto.LinkTargetTransactionGroupId = aAppFormLinkTargetEntity.LinkTargetTransactionGroupId;
 			aAppFormLinkTargetDto.AppCreatedById = aAppFormLinkTargetEntity.AppCreatedById;
 			aAppFormLinkTargetDto.AppCreatedDate = aAppFormLinkTargetEntity.AppCreatedDate;
 			aAppFormLinkTargetDto.AppModifiedDate = aAppFormLinkTargetEntity.AppModifiedDate;
 			aAppFormLinkTargetDto.AppModifiedById = aAppFormLinkTargetEntity.AppModifiedById;
 			aAppFormLinkTargetDto.AppCreatedByCompanyId = aAppFormLinkTargetEntity.AppCreatedByCompanyId;
 			aAppFormLinkTargetDto.LinkTargetSearchId = aAppFormLinkTargetEntity.LinkTargetSearchId;
 			aAppFormLinkTargetDto.LinkTargetUsageType = aAppFormLinkTargetEntity.LinkTargetUsageType;
 			aAppFormLinkTargetDto.SourceColumnType = aAppFormLinkTargetEntity.SourceColumnType;
 			aAppFormLinkTargetDto.LinkTargetUrlOrRouteCode = aAppFormLinkTargetEntity.LinkTargetUrlOrRouteCode;
 			aAppFormLinkTargetDto.LayoutDisplayMode = aAppFormLinkTargetEntity.LayoutDisplayMode;
 			aAppFormLinkTargetDto.SourceViewColumnId1 = aAppFormLinkTargetEntity.SourceViewColumnId1;
 			aAppFormLinkTargetDto.SourceViewColumnId2 = aAppFormLinkTargetEntity.SourceViewColumnId2;
 			aAppFormLinkTargetDto.SourceViewColumnId3 = aAppFormLinkTargetEntity.SourceViewColumnId3;
 			aAppFormLinkTargetDto.TargetSearchFieldId1 = aAppFormLinkTargetEntity.TargetSearchFieldId1;
 			aAppFormLinkTargetDto.TargetSearchFieldId2 = aAppFormLinkTargetEntity.TargetSearchFieldId2;
 			aAppFormLinkTargetDto.TargetSearchFieldId3 = aAppFormLinkTargetEntity.TargetSearchFieldId3;
 			aAppFormLinkTargetDto.RowDisplayViewColumnId = aAppFormLinkTargetEntity.RowDisplayViewColumnId;
 			aAppFormLinkTargetDto.SourceConditionViewColumnId = aAppFormLinkTargetEntity.SourceConditionViewColumnId;
 			aAppFormLinkTargetDto.DataTransferSettingId = aAppFormLinkTargetEntity.DataTransferSettingId;
 			aAppFormLinkTargetDto.ConditionTransFieldId = aAppFormLinkTargetEntity.ConditionTransFieldId;
 			aAppFormLinkTargetDto.Sort = aAppFormLinkTargetEntity.Sort;
 			aAppFormLinkTargetDto.OpennedFormAutoExecuteCommandId = aAppFormLinkTargetEntity.OpennedFormAutoExecuteCommandId;
 			aAppFormLinkTargetDto.IsPopup = aAppFormLinkTargetEntity.IsPopup;
 			aAppFormLinkTargetDto.PopupWidth = aAppFormLinkTargetEntity.PopupWidth;
 			aAppFormLinkTargetDto.PopupHeight = aAppFormLinkTargetEntity.PopupHeight;
 			aAppFormLinkTargetDto.IconName = aAppFormLinkTargetEntity.IconName;
 			aAppFormLinkTargetDto.OtherSettings = aAppFormLinkTargetEntity.OtherSettings;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppFormLinkTargetDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppFormLinkTargetEntity.AppCreatedDate);
                aAppFormLinkTargetDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppFormLinkTargetEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppFormLinkTargetEntity, aAppFormLinkTargetDto);
		}
		
		 /// <summary>
        ///  Copy AppFormLinkTargetDto Properties to   AppFormLinkTargetEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppFormLinkTargetEntity aAppFormLinkTargetEntity,AppFormLinkTargetDto aAppFormLinkTargetDto)
        {        
 
      			aAppFormLinkTargetEntity.SearchViewId = aAppFormLinkTargetDto.SearchViewId;
      			aAppFormLinkTargetEntity.TransactionUnitId = aAppFormLinkTargetDto.TransactionUnitId;
      			aAppFormLinkTargetEntity.SourceColumn1 = aAppFormLinkTargetDto.SourceColumn1;
      			aAppFormLinkTargetEntity.SourceColumn2 = aAppFormLinkTargetDto.SourceColumn2;
      			aAppFormLinkTargetEntity.SourceColumn3 = aAppFormLinkTargetDto.SourceColumn3;
      			aAppFormLinkTargetEntity.TargetColumn1 = aAppFormLinkTargetDto.TargetColumn1;
      			aAppFormLinkTargetEntity.TargetColumn2 = aAppFormLinkTargetDto.TargetColumn2;
      			aAppFormLinkTargetEntity.TargetColumn3 = aAppFormLinkTargetDto.TargetColumn3;
      			aAppFormLinkTargetEntity.NavigationActionName = aAppFormLinkTargetDto.NavigationActionName;
      			aAppFormLinkTargetEntity.ActionType = aAppFormLinkTargetDto.ActionType;
      			aAppFormLinkTargetEntity.IsReadonly = aAppFormLinkTargetDto.IsReadonly;
      			aAppFormLinkTargetEntity.RowDisplayDbField = aAppFormLinkTargetDto.RowDisplayDbField;
      			aAppFormLinkTargetEntity.SourceConditionColumn = aAppFormLinkTargetDto.SourceConditionColumn;
      			aAppFormLinkTargetEntity.ConditionWarningMessage = aAppFormLinkTargetDto.ConditionWarningMessage;
      			aAppFormLinkTargetEntity.GroupName = aAppFormLinkTargetDto.GroupName;
      			aAppFormLinkTargetEntity.LinkTargetTransactionId = aAppFormLinkTargetDto.LinkTargetTransactionId;
      			aAppFormLinkTargetEntity.LinkTargetTransactionGroupId = aAppFormLinkTargetDto.LinkTargetTransactionGroupId;
 
  
   
    
      			aAppFormLinkTargetEntity.AppCreatedByCompanyId = aAppFormLinkTargetDto.AppCreatedByCompanyId;
      			aAppFormLinkTargetEntity.LinkTargetSearchId = aAppFormLinkTargetDto.LinkTargetSearchId;
      			aAppFormLinkTargetEntity.LinkTargetUsageType = aAppFormLinkTargetDto.LinkTargetUsageType;
      			aAppFormLinkTargetEntity.SourceColumnType = aAppFormLinkTargetDto.SourceColumnType;
      			aAppFormLinkTargetEntity.LinkTargetUrlOrRouteCode = aAppFormLinkTargetDto.LinkTargetUrlOrRouteCode;
      			aAppFormLinkTargetEntity.LayoutDisplayMode = aAppFormLinkTargetDto.LayoutDisplayMode;
      			aAppFormLinkTargetEntity.SourceViewColumnId1 = aAppFormLinkTargetDto.SourceViewColumnId1;
      			aAppFormLinkTargetEntity.SourceViewColumnId2 = aAppFormLinkTargetDto.SourceViewColumnId2;
      			aAppFormLinkTargetEntity.SourceViewColumnId3 = aAppFormLinkTargetDto.SourceViewColumnId3;
      			aAppFormLinkTargetEntity.TargetSearchFieldId1 = aAppFormLinkTargetDto.TargetSearchFieldId1;
      			aAppFormLinkTargetEntity.TargetSearchFieldId2 = aAppFormLinkTargetDto.TargetSearchFieldId2;
      			aAppFormLinkTargetEntity.TargetSearchFieldId3 = aAppFormLinkTargetDto.TargetSearchFieldId3;
      			aAppFormLinkTargetEntity.RowDisplayViewColumnId = aAppFormLinkTargetDto.RowDisplayViewColumnId;
      			aAppFormLinkTargetEntity.SourceConditionViewColumnId = aAppFormLinkTargetDto.SourceConditionViewColumnId;
      			aAppFormLinkTargetEntity.DataTransferSettingId = aAppFormLinkTargetDto.DataTransferSettingId;
      			aAppFormLinkTargetEntity.ConditionTransFieldId = aAppFormLinkTargetDto.ConditionTransFieldId;
      			aAppFormLinkTargetEntity.Sort = aAppFormLinkTargetDto.Sort;
      			aAppFormLinkTargetEntity.OpennedFormAutoExecuteCommandId = aAppFormLinkTargetDto.OpennedFormAutoExecuteCommandId;
      			aAppFormLinkTargetEntity.IsPopup = aAppFormLinkTargetDto.IsPopup;
      			aAppFormLinkTargetEntity.PopupWidth = aAppFormLinkTargetDto.PopupWidth;
      			aAppFormLinkTargetEntity.PopupHeight = aAppFormLinkTargetDto.PopupHeight;
      			aAppFormLinkTargetEntity.IconName = aAppFormLinkTargetDto.IconName;
      			aAppFormLinkTargetEntity.OtherSettings = aAppFormLinkTargetDto.OtherSettings;
			
			if(aAppFormLinkTargetDto.Id == null)
			{
				aAppFormLinkTargetEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppFormLinkTargetEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppFormLinkTargetEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppFormLinkTargetEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppFormLinkTargetEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppFormLinkTargetEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppFormLinkTargetEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppFormLinkTargetEntity, aAppFormLinkTargetDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppFormLinkTargetEntity aAppFormLinkTargetEntity,AppFormLinkTargetDto aAppFormLinkTargetDto);
		
		static partial void OnCopyDtoToEntityDone(AppFormLinkTargetEntity aAppFormLinkTargetEntity,AppFormLinkTargetDto aAppFormLinkTargetDto);
		
   
       
    }
}

 