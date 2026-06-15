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
    /// Convert Properties between  AppFormLayoutItemEntity and  AppFormLayoutItemDto
    /// </summary>
    public static partial class AppFormLayoutItemConverter 
    {
         /// <summary>
        ///  Convert AppFormLayoutItemEntity To  AppFormLayoutItemDto
        /// </summary>
        public static AppFormLayoutItemDto ConvertEntityToDto(AppFormLayoutItemEntity aAppFormLayoutItemEntity)
        {        
    		AppFormLayoutItemDto aAppFormLayoutItemDto = new AppFormLayoutItemDto();
    		CopyEntityPropertyToDto( aAppFormLayoutItemEntity, aAppFormLayoutItemDto);          
			return aAppFormLayoutItemDto;
        }
		 /// <summary>
        ///  Convert AppFormLayoutItemEntity To  AppFormLayoutItemExDto
        /// </summary>
        public static AppFormLayoutItemExDto ConvertEntityToExDto(AppFormLayoutItemEntity aAppFormLayoutItemEntity)
        {        
    		AppFormLayoutItemExDto aAppFormLayoutItemExDto = new AppFormLayoutItemExDto();
			CopyEntityPropertyToDto( aAppFormLayoutItemEntity, aAppFormLayoutItemExDto);
			
			
			
            return aAppFormLayoutItemExDto;
        }
		
		 /// <summary>
        ///  Convert AppFormLayoutItemEntity To  AppFormLayoutItemDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppFormLayoutItemEntity aAppFormLayoutItemEntity,AppFormLayoutItemDto aAppFormLayoutItemDto)
        {        
    		
           // aAppFormLayoutItemDto.StopChangeTracking();
 			aAppFormLayoutItemDto.Id = aAppFormLayoutItemEntity.FormLayoutItemId;
 			aAppFormLayoutItemDto.FormId = aAppFormLayoutItemEntity.FormId;
 			aAppFormLayoutItemDto.WidgetItemType = aAppFormLayoutItemEntity.WidgetItemType;
 			aAppFormLayoutItemDto.FlowOrGridLayoutSortOrder = aAppFormLayoutItemEntity.FlowOrGridLayoutSortOrder;
 			aAppFormLayoutItemDto.StyleLayoutInfo = aAppFormLayoutItemEntity.StyleLayoutInfo;
 			aAppFormLayoutItemDto.DomElementTag = aAppFormLayoutItemEntity.DomElementTag;
 			aAppFormLayoutItemDto.ParameterKeyValue = aAppFormLayoutItemEntity.ParameterKeyValue;
 			aAppFormLayoutItemDto.DisplayTitle = aAppFormLayoutItemEntity.DisplayTitle;
 			aAppFormLayoutItemDto.RowIndex = aAppFormLayoutItemEntity.RowIndex;
 			aAppFormLayoutItemDto.ColumnIndex = aAppFormLayoutItemEntity.ColumnIndex;
 			aAppFormLayoutItemDto.RowSpan = aAppFormLayoutItemEntity.RowSpan;
 			aAppFormLayoutItemDto.ColumnSpan = aAppFormLayoutItemEntity.ColumnSpan;
 			aAppFormLayoutItemDto.UigridLayoutParentId = aAppFormLayoutItemEntity.UigridLayoutParentId;
 			aAppFormLayoutItemDto.TransactionFieldId = aAppFormLayoutItemEntity.TransactionFieldId;
 			aAppFormLayoutItemDto.GridTransactionUnitId = aAppFormLayoutItemEntity.GridTransactionUnitId;
 			aAppFormLayoutItemDto.SearchViewFieldId = aAppFormLayoutItemEntity.SearchViewFieldId;
 			aAppFormLayoutItemDto.AutoExcuteSearchId = aAppFormLayoutItemEntity.AutoExcuteSearchId;
 			aAppFormLayoutItemDto.AppCreatedById = aAppFormLayoutItemEntity.AppCreatedById;
 			aAppFormLayoutItemDto.AppCreatedDate = aAppFormLayoutItemEntity.AppCreatedDate;
 			aAppFormLayoutItemDto.AppModifiedDate = aAppFormLayoutItemEntity.AppModifiedDate;
 			aAppFormLayoutItemDto.AppModifiedById = aAppFormLayoutItemEntity.AppModifiedById;
 			aAppFormLayoutItemDto.AppCreatedByCompanyId = aAppFormLayoutItemEntity.AppCreatedByCompanyId;
 			aAppFormLayoutItemDto.CurrentHostId = aAppFormLayoutItemEntity.CurrentHostId;
 			aAppFormLayoutItemDto.ParentHostId = aAppFormLayoutItemEntity.ParentHostId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppFormLayoutItemDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppFormLayoutItemEntity.AppCreatedDate);
                aAppFormLayoutItemDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppFormLayoutItemEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppFormLayoutItemEntity, aAppFormLayoutItemDto);
		}
		
		 /// <summary>
        ///  Copy AppFormLayoutItemDto Properties to   AppFormLayoutItemEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppFormLayoutItemEntity aAppFormLayoutItemEntity,AppFormLayoutItemDto aAppFormLayoutItemDto)
        {        
 
      			aAppFormLayoutItemEntity.FormId = aAppFormLayoutItemDto.FormId;
      			aAppFormLayoutItemEntity.WidgetItemType = aAppFormLayoutItemDto.WidgetItemType;
      			aAppFormLayoutItemEntity.FlowOrGridLayoutSortOrder = aAppFormLayoutItemDto.FlowOrGridLayoutSortOrder;
      			aAppFormLayoutItemEntity.StyleLayoutInfo = aAppFormLayoutItemDto.StyleLayoutInfo;
      			aAppFormLayoutItemEntity.DomElementTag = aAppFormLayoutItemDto.DomElementTag;
      			aAppFormLayoutItemEntity.ParameterKeyValue = aAppFormLayoutItemDto.ParameterKeyValue;
      			aAppFormLayoutItemEntity.DisplayTitle = aAppFormLayoutItemDto.DisplayTitle;
      			aAppFormLayoutItemEntity.RowIndex = aAppFormLayoutItemDto.RowIndex;
      			aAppFormLayoutItemEntity.ColumnIndex = aAppFormLayoutItemDto.ColumnIndex;
      			aAppFormLayoutItemEntity.RowSpan = aAppFormLayoutItemDto.RowSpan;
      			aAppFormLayoutItemEntity.ColumnSpan = aAppFormLayoutItemDto.ColumnSpan;
      			aAppFormLayoutItemEntity.UigridLayoutParentId = aAppFormLayoutItemDto.UigridLayoutParentId;
      			aAppFormLayoutItemEntity.TransactionFieldId = aAppFormLayoutItemDto.TransactionFieldId;
      			aAppFormLayoutItemEntity.GridTransactionUnitId = aAppFormLayoutItemDto.GridTransactionUnitId;
      			aAppFormLayoutItemEntity.SearchViewFieldId = aAppFormLayoutItemDto.SearchViewFieldId;
      			aAppFormLayoutItemEntity.AutoExcuteSearchId = aAppFormLayoutItemDto.AutoExcuteSearchId;
 
  
   
    
      			aAppFormLayoutItemEntity.AppCreatedByCompanyId = aAppFormLayoutItemDto.AppCreatedByCompanyId;
      			aAppFormLayoutItemEntity.CurrentHostId = aAppFormLayoutItemDto.CurrentHostId;
      			aAppFormLayoutItemEntity.ParentHostId = aAppFormLayoutItemDto.ParentHostId;
			
			if(aAppFormLayoutItemDto.Id == null)
			{
				aAppFormLayoutItemEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppFormLayoutItemEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppFormLayoutItemEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppFormLayoutItemEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppFormLayoutItemEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppFormLayoutItemEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppFormLayoutItemEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppFormLayoutItemEntity, aAppFormLayoutItemDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppFormLayoutItemEntity aAppFormLayoutItemEntity,AppFormLayoutItemDto aAppFormLayoutItemDto);
		
		static partial void OnCopyDtoToEntityDone(AppFormLayoutItemEntity aAppFormLayoutItemEntity,AppFormLayoutItemDto aAppFormLayoutItemDto);
		
   
       
    }
}

 