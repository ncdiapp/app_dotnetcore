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
    /// Convert Properties between  AppTransactionUnitLinkedSearchEntity and  AppTransactionUnitLinkedSearchDto
    /// </summary>
    public static partial class AppTransactionUnitLinkedSearchConverter 
    {
         /// <summary>
        ///  Convert AppTransactionUnitLinkedSearchEntity To  AppTransactionUnitLinkedSearchDto
        /// </summary>
        public static AppTransactionUnitLinkedSearchDto ConvertEntityToDto(AppTransactionUnitLinkedSearchEntity aAppTransactionUnitLinkedSearchEntity)
        {        
    		AppTransactionUnitLinkedSearchDto aAppTransactionUnitLinkedSearchDto = new AppTransactionUnitLinkedSearchDto();
    		CopyEntityPropertyToDto( aAppTransactionUnitLinkedSearchEntity, aAppTransactionUnitLinkedSearchDto);          
			return aAppTransactionUnitLinkedSearchDto;
        }
		 /// <summary>
        ///  Convert AppTransactionUnitLinkedSearchEntity To  AppTransactionUnitLinkedSearchExDto
        /// </summary>
        public static AppTransactionUnitLinkedSearchExDto ConvertEntityToExDto(AppTransactionUnitLinkedSearchEntity aAppTransactionUnitLinkedSearchEntity)
        {        
    		AppTransactionUnitLinkedSearchExDto aAppTransactionUnitLinkedSearchExDto = new AppTransactionUnitLinkedSearchExDto();
			CopyEntityPropertyToDto( aAppTransactionUnitLinkedSearchEntity, aAppTransactionUnitLinkedSearchExDto);
			
			
			
            return aAppTransactionUnitLinkedSearchExDto;
        }
		
		 /// <summary>
        ///  Convert AppTransactionUnitLinkedSearchEntity To  AppTransactionUnitLinkedSearchDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppTransactionUnitLinkedSearchEntity aAppTransactionUnitLinkedSearchEntity,AppTransactionUnitLinkedSearchDto aAppTransactionUnitLinkedSearchDto)
        {        
    		
           // aAppTransactionUnitLinkedSearchDto.StopChangeTracking();
 			aAppTransactionUnitLinkedSearchDto.Id = aAppTransactionUnitLinkedSearchEntity.TransactionUnitLinkedSearchId;
 			aAppTransactionUnitLinkedSearchDto.TransactionUnitId = aAppTransactionUnitLinkedSearchEntity.TransactionUnitId;
 			aAppTransactionUnitLinkedSearchDto.SearchId = aAppTransactionUnitLinkedSearchEntity.SearchId;
 			aAppTransactionUnitLinkedSearchDto.SearchSaveId = aAppTransactionUnitLinkedSearchEntity.SearchSaveId;
 			aAppTransactionUnitLinkedSearchDto.SearchViewId = aAppTransactionUnitLinkedSearchEntity.SearchViewId;
 			aAppTransactionUnitLinkedSearchDto.Name = aAppTransactionUnitLinkedSearchEntity.Name;
 			aAppTransactionUnitLinkedSearchDto.Action = aAppTransactionUnitLinkedSearchEntity.Action;
 			aAppTransactionUnitLinkedSearchDto.IsSingleSelectedRow = aAppTransactionUnitLinkedSearchEntity.IsSingleSelectedRow;
 			aAppTransactionUnitLinkedSearchDto.Description = aAppTransactionUnitLinkedSearchEntity.Description;
 			aAppTransactionUnitLinkedSearchDto.UsageType = aAppTransactionUnitLinkedSearchEntity.UsageType;
 			aAppTransactionUnitLinkedSearchDto.GroupName = aAppTransactionUnitLinkedSearchEntity.GroupName;
 			aAppTransactionUnitLinkedSearchDto.IsNeedPreValidation = aAppTransactionUnitLinkedSearchEntity.IsNeedPreValidation;
 			aAppTransactionUnitLinkedSearchDto.IsNeedPostValidation = aAppTransactionUnitLinkedSearchEntity.IsNeedPostValidation;
 			aAppTransactionUnitLinkedSearchDto.CallbackRestResourceUri = aAppTransactionUnitLinkedSearchEntity.CallbackRestResourceUri;
 			aAppTransactionUnitLinkedSearchDto.AppCreatedById = aAppTransactionUnitLinkedSearchEntity.AppCreatedById;
 			aAppTransactionUnitLinkedSearchDto.AppCreatedDate = aAppTransactionUnitLinkedSearchEntity.AppCreatedDate;
 			aAppTransactionUnitLinkedSearchDto.AppModifiedDate = aAppTransactionUnitLinkedSearchEntity.AppModifiedDate;
 			aAppTransactionUnitLinkedSearchDto.AppModifiedById = aAppTransactionUnitLinkedSearchEntity.AppModifiedById;
 			aAppTransactionUnitLinkedSearchDto.AppCreatedByCompanyId = aAppTransactionUnitLinkedSearchEntity.AppCreatedByCompanyId;
 			aAppTransactionUnitLinkedSearchDto.TargetTransactionId = aAppTransactionUnitLinkedSearchEntity.TargetTransactionId;
 			aAppTransactionUnitLinkedSearchDto.ConditionTransFieldId = aAppTransactionUnitLinkedSearchEntity.ConditionTransFieldId;
 			aAppTransactionUnitLinkedSearchDto.CallBackCommandId = aAppTransactionUnitLinkedSearchEntity.CallBackCommandId;
 			aAppTransactionUnitLinkedSearchDto.Sort = aAppTransactionUnitLinkedSearchEntity.Sort;
 			aAppTransactionUnitLinkedSearchDto.IsPopup = aAppTransactionUnitLinkedSearchEntity.IsPopup;
 			aAppTransactionUnitLinkedSearchDto.PopupWidth = aAppTransactionUnitLinkedSearchEntity.PopupWidth;
 			aAppTransactionUnitLinkedSearchDto.PopupHeight = aAppTransactionUnitLinkedSearchEntity.PopupHeight;
 			aAppTransactionUnitLinkedSearchDto.IconName = aAppTransactionUnitLinkedSearchEntity.IconName;
 			aAppTransactionUnitLinkedSearchDto.OtherSettings = aAppTransactionUnitLinkedSearchEntity.OtherSettings;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppTransactionUnitLinkedSearchDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionUnitLinkedSearchEntity.AppCreatedDate);
                aAppTransactionUnitLinkedSearchDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionUnitLinkedSearchEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppTransactionUnitLinkedSearchEntity, aAppTransactionUnitLinkedSearchDto);
		}
		
		 /// <summary>
        ///  Copy AppTransactionUnitLinkedSearchDto Properties to   AppTransactionUnitLinkedSearchEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppTransactionUnitLinkedSearchEntity aAppTransactionUnitLinkedSearchEntity,AppTransactionUnitLinkedSearchDto aAppTransactionUnitLinkedSearchDto)
        {        
 
      			aAppTransactionUnitLinkedSearchEntity.TransactionUnitId = aAppTransactionUnitLinkedSearchDto.TransactionUnitId;
      			aAppTransactionUnitLinkedSearchEntity.SearchId = aAppTransactionUnitLinkedSearchDto.SearchId;
      			aAppTransactionUnitLinkedSearchEntity.SearchSaveId = aAppTransactionUnitLinkedSearchDto.SearchSaveId;
      			aAppTransactionUnitLinkedSearchEntity.SearchViewId = aAppTransactionUnitLinkedSearchDto.SearchViewId;
      			aAppTransactionUnitLinkedSearchEntity.Name = aAppTransactionUnitLinkedSearchDto.Name;
      			aAppTransactionUnitLinkedSearchEntity.Action = aAppTransactionUnitLinkedSearchDto.Action;
      			aAppTransactionUnitLinkedSearchEntity.IsSingleSelectedRow = aAppTransactionUnitLinkedSearchDto.IsSingleSelectedRow;
      			aAppTransactionUnitLinkedSearchEntity.Description = aAppTransactionUnitLinkedSearchDto.Description;
      			aAppTransactionUnitLinkedSearchEntity.UsageType = aAppTransactionUnitLinkedSearchDto.UsageType;
      			aAppTransactionUnitLinkedSearchEntity.GroupName = aAppTransactionUnitLinkedSearchDto.GroupName;
      			aAppTransactionUnitLinkedSearchEntity.IsNeedPreValidation = aAppTransactionUnitLinkedSearchDto.IsNeedPreValidation;
      			aAppTransactionUnitLinkedSearchEntity.IsNeedPostValidation = aAppTransactionUnitLinkedSearchDto.IsNeedPostValidation;
      			aAppTransactionUnitLinkedSearchEntity.CallbackRestResourceUri = aAppTransactionUnitLinkedSearchDto.CallbackRestResourceUri;
 
  
   
    
      			aAppTransactionUnitLinkedSearchEntity.AppCreatedByCompanyId = aAppTransactionUnitLinkedSearchDto.AppCreatedByCompanyId;
      			aAppTransactionUnitLinkedSearchEntity.TargetTransactionId = aAppTransactionUnitLinkedSearchDto.TargetTransactionId;
      			aAppTransactionUnitLinkedSearchEntity.ConditionTransFieldId = aAppTransactionUnitLinkedSearchDto.ConditionTransFieldId;
      			aAppTransactionUnitLinkedSearchEntity.CallBackCommandId = aAppTransactionUnitLinkedSearchDto.CallBackCommandId;
      			aAppTransactionUnitLinkedSearchEntity.Sort = aAppTransactionUnitLinkedSearchDto.Sort;
      			aAppTransactionUnitLinkedSearchEntity.IsPopup = aAppTransactionUnitLinkedSearchDto.IsPopup;
      			aAppTransactionUnitLinkedSearchEntity.PopupWidth = aAppTransactionUnitLinkedSearchDto.PopupWidth;
      			aAppTransactionUnitLinkedSearchEntity.PopupHeight = aAppTransactionUnitLinkedSearchDto.PopupHeight;
      			aAppTransactionUnitLinkedSearchEntity.IconName = aAppTransactionUnitLinkedSearchDto.IconName;
      			aAppTransactionUnitLinkedSearchEntity.OtherSettings = aAppTransactionUnitLinkedSearchDto.OtherSettings;
			
			if(aAppTransactionUnitLinkedSearchDto.Id == null)
			{
				aAppTransactionUnitLinkedSearchEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionUnitLinkedSearchEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppTransactionUnitLinkedSearchEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionUnitLinkedSearchEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionUnitLinkedSearchEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppTransactionUnitLinkedSearchEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionUnitLinkedSearchEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppTransactionUnitLinkedSearchEntity, aAppTransactionUnitLinkedSearchDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppTransactionUnitLinkedSearchEntity aAppTransactionUnitLinkedSearchEntity,AppTransactionUnitLinkedSearchDto aAppTransactionUnitLinkedSearchDto);
		
		static partial void OnCopyDtoToEntityDone(AppTransactionUnitLinkedSearchEntity aAppTransactionUnitLinkedSearchEntity,AppTransactionUnitLinkedSearchDto aAppTransactionUnitLinkedSearchDto);
		
   
       
    }
}

 