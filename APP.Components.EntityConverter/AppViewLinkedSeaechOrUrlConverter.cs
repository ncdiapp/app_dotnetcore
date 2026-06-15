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
    /// Convert Properties between  AppViewLinkedSeaechOrUrlEntity and  AppViewLinkedSeaechOrUrlDto
    /// </summary>
    public static partial class AppViewLinkedSeaechOrUrlConverter 
    {
         /// <summary>
        ///  Convert AppViewLinkedSeaechOrUrlEntity To  AppViewLinkedSeaechOrUrlDto
        /// </summary>
        public static AppViewLinkedSeaechOrUrlDto ConvertEntityToDto(AppViewLinkedSeaechOrUrlEntity aAppViewLinkedSeaechOrUrlEntity)
        {        
    		AppViewLinkedSeaechOrUrlDto aAppViewLinkedSeaechOrUrlDto = new AppViewLinkedSeaechOrUrlDto();
    		CopyEntityPropertyToDto( aAppViewLinkedSeaechOrUrlEntity, aAppViewLinkedSeaechOrUrlDto);          
			return aAppViewLinkedSeaechOrUrlDto;
        }
		 /// <summary>
        ///  Convert AppViewLinkedSeaechOrUrlEntity To  AppViewLinkedSeaechOrUrlExDto
        /// </summary>
        public static AppViewLinkedSeaechOrUrlExDto ConvertEntityToExDto(AppViewLinkedSeaechOrUrlEntity aAppViewLinkedSeaechOrUrlEntity)
        {        
    		AppViewLinkedSeaechOrUrlExDto aAppViewLinkedSeaechOrUrlExDto = new AppViewLinkedSeaechOrUrlExDto();
			CopyEntityPropertyToDto( aAppViewLinkedSeaechOrUrlEntity, aAppViewLinkedSeaechOrUrlExDto);
			
			
			
            return aAppViewLinkedSeaechOrUrlExDto;
        }
		
		 /// <summary>
        ///  Convert AppViewLinkedSeaechOrUrlEntity To  AppViewLinkedSeaechOrUrlDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppViewLinkedSeaechOrUrlEntity aAppViewLinkedSeaechOrUrlEntity,AppViewLinkedSeaechOrUrlDto aAppViewLinkedSeaechOrUrlDto)
        {        
    		
           // aAppViewLinkedSeaechOrUrlDto.StopChangeTracking();
 			aAppViewLinkedSeaechOrUrlDto.Id = aAppViewLinkedSeaechOrUrlEntity.SearchViewLinkSearchId;
 			aAppViewLinkedSeaechOrUrlDto.SearchViewId = aAppViewLinkedSeaechOrUrlEntity.SearchViewId;
 			aAppViewLinkedSeaechOrUrlDto.LinkTargetUrlOrRouteCode = aAppViewLinkedSeaechOrUrlEntity.LinkTargetUrlOrRouteCode;
 			aAppViewLinkedSeaechOrUrlDto.AppCreatedById = aAppViewLinkedSeaechOrUrlEntity.AppCreatedById;
 			aAppViewLinkedSeaechOrUrlDto.AppCreatedDate = aAppViewLinkedSeaechOrUrlEntity.AppCreatedDate;
 			aAppViewLinkedSeaechOrUrlDto.AppModifiedDate = aAppViewLinkedSeaechOrUrlEntity.AppModifiedDate;
 			aAppViewLinkedSeaechOrUrlDto.AppModifiedById = aAppViewLinkedSeaechOrUrlEntity.AppModifiedById;
 			aAppViewLinkedSeaechOrUrlDto.AppCreatedByCompanyId = aAppViewLinkedSeaechOrUrlEntity.AppCreatedByCompanyId;
 			aAppViewLinkedSeaechOrUrlDto.LayoutDisplayMode = aAppViewLinkedSeaechOrUrlEntity.LayoutDisplayMode;
 			aAppViewLinkedSeaechOrUrlDto.SourceViewColumnId1 = aAppViewLinkedSeaechOrUrlEntity.SourceViewColumnId1;
 			aAppViewLinkedSeaechOrUrlDto.SourceViewColumnId2 = aAppViewLinkedSeaechOrUrlEntity.SourceViewColumnId2;
 			aAppViewLinkedSeaechOrUrlDto.SourceViewColumnId3 = aAppViewLinkedSeaechOrUrlEntity.SourceViewColumnId3;
 			aAppViewLinkedSeaechOrUrlDto.TargetSearchFieldId1 = aAppViewLinkedSeaechOrUrlEntity.TargetSearchFieldId1;
 			aAppViewLinkedSeaechOrUrlDto.TargetSearchFieldId2 = aAppViewLinkedSeaechOrUrlEntity.TargetSearchFieldId2;
 			aAppViewLinkedSeaechOrUrlDto.TargetSearchFieldId3 = aAppViewLinkedSeaechOrUrlEntity.TargetSearchFieldId3;
 			aAppViewLinkedSeaechOrUrlDto.DisplayText = aAppViewLinkedSeaechOrUrlEntity.DisplayText;
 			aAppViewLinkedSeaechOrUrlDto.Sort = aAppViewLinkedSeaechOrUrlEntity.Sort;
 			aAppViewLinkedSeaechOrUrlDto.LinkTargetSearchId = aAppViewLinkedSeaechOrUrlEntity.LinkTargetSearchId;
 			aAppViewLinkedSeaechOrUrlDto.IsPopup = aAppViewLinkedSeaechOrUrlEntity.IsPopup;
 			aAppViewLinkedSeaechOrUrlDto.PopupWidth = aAppViewLinkedSeaechOrUrlEntity.PopupWidth;
 			aAppViewLinkedSeaechOrUrlDto.PopupHeight = aAppViewLinkedSeaechOrUrlEntity.PopupHeight;
 			aAppViewLinkedSeaechOrUrlDto.IconName = aAppViewLinkedSeaechOrUrlEntity.IconName;
 			aAppViewLinkedSeaechOrUrlDto.RowDisplayViewColumnId = aAppViewLinkedSeaechOrUrlEntity.RowDisplayViewColumnId;
 			aAppViewLinkedSeaechOrUrlDto.SourceConditionViewColumnId = aAppViewLinkedSeaechOrUrlEntity.SourceConditionViewColumnId;
 			aAppViewLinkedSeaechOrUrlDto.OtherSettings = aAppViewLinkedSeaechOrUrlEntity.OtherSettings;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppViewLinkedSeaechOrUrlDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppViewLinkedSeaechOrUrlEntity.AppCreatedDate);
                aAppViewLinkedSeaechOrUrlDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppViewLinkedSeaechOrUrlEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppViewLinkedSeaechOrUrlEntity, aAppViewLinkedSeaechOrUrlDto);
		}
		
		 /// <summary>
        ///  Copy AppViewLinkedSeaechOrUrlDto Properties to   AppViewLinkedSeaechOrUrlEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppViewLinkedSeaechOrUrlEntity aAppViewLinkedSeaechOrUrlEntity,AppViewLinkedSeaechOrUrlDto aAppViewLinkedSeaechOrUrlDto)
        {        
 
      			aAppViewLinkedSeaechOrUrlEntity.SearchViewId = aAppViewLinkedSeaechOrUrlDto.SearchViewId;
      			aAppViewLinkedSeaechOrUrlEntity.LinkTargetUrlOrRouteCode = aAppViewLinkedSeaechOrUrlDto.LinkTargetUrlOrRouteCode;
 
  
   
    
      			aAppViewLinkedSeaechOrUrlEntity.AppCreatedByCompanyId = aAppViewLinkedSeaechOrUrlDto.AppCreatedByCompanyId;
      			aAppViewLinkedSeaechOrUrlEntity.LayoutDisplayMode = aAppViewLinkedSeaechOrUrlDto.LayoutDisplayMode;
      			aAppViewLinkedSeaechOrUrlEntity.SourceViewColumnId1 = aAppViewLinkedSeaechOrUrlDto.SourceViewColumnId1;
      			aAppViewLinkedSeaechOrUrlEntity.SourceViewColumnId2 = aAppViewLinkedSeaechOrUrlDto.SourceViewColumnId2;
      			aAppViewLinkedSeaechOrUrlEntity.SourceViewColumnId3 = aAppViewLinkedSeaechOrUrlDto.SourceViewColumnId3;
      			aAppViewLinkedSeaechOrUrlEntity.TargetSearchFieldId1 = aAppViewLinkedSeaechOrUrlDto.TargetSearchFieldId1;
      			aAppViewLinkedSeaechOrUrlEntity.TargetSearchFieldId2 = aAppViewLinkedSeaechOrUrlDto.TargetSearchFieldId2;
      			aAppViewLinkedSeaechOrUrlEntity.TargetSearchFieldId3 = aAppViewLinkedSeaechOrUrlDto.TargetSearchFieldId3;
      			aAppViewLinkedSeaechOrUrlEntity.DisplayText = aAppViewLinkedSeaechOrUrlDto.DisplayText;
      			aAppViewLinkedSeaechOrUrlEntity.Sort = aAppViewLinkedSeaechOrUrlDto.Sort;
      			aAppViewLinkedSeaechOrUrlEntity.LinkTargetSearchId = aAppViewLinkedSeaechOrUrlDto.LinkTargetSearchId;
      			aAppViewLinkedSeaechOrUrlEntity.IsPopup = aAppViewLinkedSeaechOrUrlDto.IsPopup;
      			aAppViewLinkedSeaechOrUrlEntity.PopupWidth = aAppViewLinkedSeaechOrUrlDto.PopupWidth;
      			aAppViewLinkedSeaechOrUrlEntity.PopupHeight = aAppViewLinkedSeaechOrUrlDto.PopupHeight;
      			aAppViewLinkedSeaechOrUrlEntity.IconName = aAppViewLinkedSeaechOrUrlDto.IconName;
      			aAppViewLinkedSeaechOrUrlEntity.RowDisplayViewColumnId = aAppViewLinkedSeaechOrUrlDto.RowDisplayViewColumnId;
      			aAppViewLinkedSeaechOrUrlEntity.SourceConditionViewColumnId = aAppViewLinkedSeaechOrUrlDto.SourceConditionViewColumnId;
      			aAppViewLinkedSeaechOrUrlEntity.OtherSettings = aAppViewLinkedSeaechOrUrlDto.OtherSettings;
			
			if(aAppViewLinkedSeaechOrUrlDto.Id == null)
			{
				aAppViewLinkedSeaechOrUrlEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppViewLinkedSeaechOrUrlEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppViewLinkedSeaechOrUrlEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppViewLinkedSeaechOrUrlEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppViewLinkedSeaechOrUrlEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppViewLinkedSeaechOrUrlEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppViewLinkedSeaechOrUrlEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppViewLinkedSeaechOrUrlEntity, aAppViewLinkedSeaechOrUrlDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppViewLinkedSeaechOrUrlEntity aAppViewLinkedSeaechOrUrlEntity,AppViewLinkedSeaechOrUrlDto aAppViewLinkedSeaechOrUrlDto);
		
		static partial void OnCopyDtoToEntityDone(AppViewLinkedSeaechOrUrlEntity aAppViewLinkedSeaechOrUrlEntity,AppViewLinkedSeaechOrUrlDto aAppViewLinkedSeaechOrUrlDto);
		
   
       
    }
}

 