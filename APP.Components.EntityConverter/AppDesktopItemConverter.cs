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
    /// Convert Properties between  AppDesktopItemEntity and  AppDesktopItemDto
    /// </summary>
    public static partial class AppDesktopItemConverter 
    {
         /// <summary>
        ///  Convert AppDesktopItemEntity To  AppDesktopItemDto
        /// </summary>
        public static AppDesktopItemDto ConvertEntityToDto(AppDesktopItemEntity aAppDesktopItemEntity)
        {        
    		AppDesktopItemDto aAppDesktopItemDto = new AppDesktopItemDto();
    		CopyEntityPropertyToDto( aAppDesktopItemEntity, aAppDesktopItemDto);          
			return aAppDesktopItemDto;
        }
		 /// <summary>
        ///  Convert AppDesktopItemEntity To  AppDesktopItemExDto
        /// </summary>
        public static AppDesktopItemExDto ConvertEntityToExDto(AppDesktopItemEntity aAppDesktopItemEntity)
        {        
    		AppDesktopItemExDto aAppDesktopItemExDto = new AppDesktopItemExDto();
			CopyEntityPropertyToDto( aAppDesktopItemEntity, aAppDesktopItemExDto);
			
			
			
            return aAppDesktopItemExDto;
        }
		
		 /// <summary>
        ///  Convert AppDesktopItemEntity To  AppDesktopItemDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppDesktopItemEntity aAppDesktopItemEntity,AppDesktopItemDto aAppDesktopItemDto)
        {        
    		
           // aAppDesktopItemDto.StopChangeTracking();
 			aAppDesktopItemDto.Id = aAppDesktopItemEntity.DesktopItemId;
 			aAppDesktopItemDto.DesktopId = aAppDesktopItemEntity.DesktopId;
 			aAppDesktopItemDto.WidgetItemType = aAppDesktopItemEntity.WidgetItemType;
 			aAppDesktopItemDto.FlowOrGridLayoutSortOrder = aAppDesktopItemEntity.FlowOrGridLayoutSortOrder;
 			aAppDesktopItemDto.StyleLayoutInfo = aAppDesktopItemEntity.StyleLayoutInfo;
 			aAppDesktopItemDto.DomElementTag = aAppDesktopItemEntity.DomElementTag;
 			aAppDesktopItemDto.ParameterKeyValue = aAppDesktopItemEntity.ParameterKeyValue;
 			aAppDesktopItemDto.DisplayTitle = aAppDesktopItemEntity.DisplayTitle;
 			aAppDesktopItemDto.RowIndex = aAppDesktopItemEntity.RowIndex;
 			aAppDesktopItemDto.ColumnIndex = aAppDesktopItemEntity.ColumnIndex;
 			aAppDesktopItemDto.RowSpan = aAppDesktopItemEntity.RowSpan;
 			aAppDesktopItemDto.ColumnSpan = aAppDesktopItemEntity.ColumnSpan;
 			aAppDesktopItemDto.GridLayoutParentId = aAppDesktopItemEntity.GridLayoutParentId;
 			aAppDesktopItemDto.AppCreatedById = aAppDesktopItemEntity.AppCreatedById;
 			aAppDesktopItemDto.AppCreatedDate = aAppDesktopItemEntity.AppCreatedDate;
 			aAppDesktopItemDto.AppModifiedDate = aAppDesktopItemEntity.AppModifiedDate;
 			aAppDesktopItemDto.AppModifiedById = aAppDesktopItemEntity.AppModifiedById;
 			aAppDesktopItemDto.AppCreatedByCompanyId = aAppDesktopItemEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppDesktopItemDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppDesktopItemEntity.AppCreatedDate);
                aAppDesktopItemDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppDesktopItemEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppDesktopItemEntity, aAppDesktopItemDto);
		}
		
		 /// <summary>
        ///  Copy AppDesktopItemDto Properties to   AppDesktopItemEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppDesktopItemEntity aAppDesktopItemEntity,AppDesktopItemDto aAppDesktopItemDto)
        {        
 
      			aAppDesktopItemEntity.DesktopId = aAppDesktopItemDto.DesktopId;
      			aAppDesktopItemEntity.WidgetItemType = aAppDesktopItemDto.WidgetItemType;
      			aAppDesktopItemEntity.FlowOrGridLayoutSortOrder = aAppDesktopItemDto.FlowOrGridLayoutSortOrder;
      			aAppDesktopItemEntity.StyleLayoutInfo = aAppDesktopItemDto.StyleLayoutInfo;
      			aAppDesktopItemEntity.DomElementTag = aAppDesktopItemDto.DomElementTag;
      			aAppDesktopItemEntity.ParameterKeyValue = aAppDesktopItemDto.ParameterKeyValue;
      			aAppDesktopItemEntity.DisplayTitle = aAppDesktopItemDto.DisplayTitle;
      			aAppDesktopItemEntity.RowIndex = aAppDesktopItemDto.RowIndex;
      			aAppDesktopItemEntity.ColumnIndex = aAppDesktopItemDto.ColumnIndex;
      			aAppDesktopItemEntity.RowSpan = aAppDesktopItemDto.RowSpan;
      			aAppDesktopItemEntity.ColumnSpan = aAppDesktopItemDto.ColumnSpan;
      			aAppDesktopItemEntity.GridLayoutParentId = aAppDesktopItemDto.GridLayoutParentId;
 
  
   
    
      			aAppDesktopItemEntity.AppCreatedByCompanyId = aAppDesktopItemDto.AppCreatedByCompanyId;
			
			if(aAppDesktopItemDto.Id == null)
			{
				aAppDesktopItemEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppDesktopItemEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppDesktopItemEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppDesktopItemEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppDesktopItemEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppDesktopItemEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppDesktopItemEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppDesktopItemEntity, aAppDesktopItemDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppDesktopItemEntity aAppDesktopItemEntity,AppDesktopItemDto aAppDesktopItemDto);
		
		static partial void OnCopyDtoToEntityDone(AppDesktopItemEntity aAppDesktopItemEntity,AppDesktopItemDto aAppDesktopItemDto);
		
   
       
    }
}

 