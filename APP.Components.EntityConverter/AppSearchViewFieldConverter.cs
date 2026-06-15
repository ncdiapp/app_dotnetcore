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
    /// Convert Properties between  AppSearchViewFieldEntity and  AppSearchViewFieldDto
    /// </summary>
    public static partial class AppSearchViewFieldConverter 
    {
         /// <summary>
        ///  Convert AppSearchViewFieldEntity To  AppSearchViewFieldDto
        /// </summary>
        public static AppSearchViewFieldDto ConvertEntityToDto(AppSearchViewFieldEntity aAppSearchViewFieldEntity)
        {        
    		AppSearchViewFieldDto aAppSearchViewFieldDto = new AppSearchViewFieldDto();
    		CopyEntityPropertyToDto( aAppSearchViewFieldEntity, aAppSearchViewFieldDto);          
			return aAppSearchViewFieldDto;
        }
		 /// <summary>
        ///  Convert AppSearchViewFieldEntity To  AppSearchViewFieldExDto
        /// </summary>
        public static AppSearchViewFieldExDto ConvertEntityToExDto(AppSearchViewFieldEntity aAppSearchViewFieldEntity)
        {        
    		AppSearchViewFieldExDto aAppSearchViewFieldExDto = new AppSearchViewFieldExDto();
			CopyEntityPropertyToDto( aAppSearchViewFieldEntity, aAppSearchViewFieldExDto);
			
			
			
            return aAppSearchViewFieldExDto;
        }
		
		 /// <summary>
        ///  Convert AppSearchViewFieldEntity To  AppSearchViewFieldDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSearchViewFieldEntity aAppSearchViewFieldEntity,AppSearchViewFieldDto aAppSearchViewFieldDto)
        {        
    		
           // aAppSearchViewFieldDto.StopChangeTracking();
 			aAppSearchViewFieldDto.Id = aAppSearchViewFieldEntity.SearchViewFieldId;
 			aAppSearchViewFieldDto.SearchViewId = aAppSearchViewFieldEntity.SearchViewId;
 			aAppSearchViewFieldDto.IsVisible = aAppSearchViewFieldEntity.IsVisible;
 			aAppSearchViewFieldDto.DisplayText = aAppSearchViewFieldEntity.DisplayText;
 			aAppSearchViewFieldDto.Sort = aAppSearchViewFieldEntity.Sort;
 			aAppSearchViewFieldDto.Width = aAppSearchViewFieldEntity.Width;
 			aAppSearchViewFieldDto.SysTableFiledPath = aAppSearchViewFieldEntity.SysTableFiledPath;
 			aAppSearchViewFieldDto.MassUpdateTransactionFieldId = aAppSearchViewFieldEntity.MassUpdateTransactionFieldId;
 			aAppSearchViewFieldDto.ControlType = aAppSearchViewFieldEntity.ControlType;
 			aAppSearchViewFieldDto.EntityId = aAppSearchViewFieldEntity.EntityId;
 			aAppSearchViewFieldDto.DataType = aAppSearchViewFieldEntity.DataType;
 			aAppSearchViewFieldDto.IsGroupBy = aAppSearchViewFieldEntity.IsGroupBy;
 			aAppSearchViewFieldDto.GroupByLevel = aAppSearchViewFieldEntity.GroupByLevel;
 			aAppSearchViewFieldDto.AggregationFunctionType = aAppSearchViewFieldEntity.AggregationFunctionType;
 			aAppSearchViewFieldDto.IsFilterByCurrentUser = aAppSearchViewFieldEntity.IsFilterByCurrentUser;
 			aAppSearchViewFieldDto.IsMapToChartX = aAppSearchViewFieldEntity.IsMapToChartX;
 			aAppSearchViewFieldDto.IsMapToChartY = aAppSearchViewFieldEntity.IsMapToChartY;
 			aAppSearchViewFieldDto.ChartYmappingOrder = aAppSearchViewFieldEntity.ChartYmappingOrder;
 			aAppSearchViewFieldDto.TreeLevel = aAppSearchViewFieldEntity.TreeLevel;
 			aAppSearchViewFieldDto.IsTreeNodeId = aAppSearchViewFieldEntity.IsTreeNodeId;
 			aAppSearchViewFieldDto.IsTreeNodeDisplay = aAppSearchViewFieldEntity.IsTreeNodeDisplay;
 			aAppSearchViewFieldDto.IsTreeNodeDesc = aAppSearchViewFieldEntity.IsTreeNodeDesc;
 			aAppSearchViewFieldDto.IsTreeNodeImageUrl = aAppSearchViewFieldEntity.IsTreeNodeImageUrl;
 			aAppSearchViewFieldDto.MappingSearchFieldId = aAppSearchViewFieldEntity.MappingSearchFieldId;
 			aAppSearchViewFieldDto.ProductDetaiMapTransFiledId = aAppSearchViewFieldEntity.ProductDetaiMapTransFiledId;
 			aAppSearchViewFieldDto.IsUserDefined1 = aAppSearchViewFieldEntity.IsUserDefined1;
 			aAppSearchViewFieldDto.IsUserDefined2 = aAppSearchViewFieldEntity.IsUserDefined2;
 			aAppSearchViewFieldDto.IsUserDefined3 = aAppSearchViewFieldEntity.IsUserDefined3;
 			aAppSearchViewFieldDto.IsUserDefined4 = aAppSearchViewFieldEntity.IsUserDefined4;
 			aAppSearchViewFieldDto.IsFileFoderId = aAppSearchViewFieldEntity.IsFileFoderId;
 			aAppSearchViewFieldDto.IsTransRootId = aAppSearchViewFieldEntity.IsTransRootId;
 			aAppSearchViewFieldDto.AppCreatedById = aAppSearchViewFieldEntity.AppCreatedById;
 			aAppSearchViewFieldDto.AppCreatedDate = aAppSearchViewFieldEntity.AppCreatedDate;
 			aAppSearchViewFieldDto.AppModifiedDate = aAppSearchViewFieldEntity.AppModifiedDate;
 			aAppSearchViewFieldDto.AppModifiedById = aAppSearchViewFieldEntity.AppModifiedById;
 			aAppSearchViewFieldDto.AppCreatedByCompanyId = aAppSearchViewFieldEntity.AppCreatedByCompanyId;
 			aAppSearchViewFieldDto.RowNumber = aAppSearchViewFieldEntity.RowNumber;
 			aAppSearchViewFieldDto.ColumnNumber = aAppSearchViewFieldEntity.ColumnNumber;
 			aAppSearchViewFieldDto.OrderByLevel = aAppSearchViewFieldEntity.OrderByLevel;
 			aAppSearchViewFieldDto.IsDescOrder = aAppSearchViewFieldEntity.IsDescOrder;
 			aAppSearchViewFieldDto.IsReadOnly = aAppSearchViewFieldEntity.IsReadOnly;
 			aAppSearchViewFieldDto.PullCriteriaAsDefaultValueSearchFieldId = aAppSearchViewFieldEntity.PullCriteriaAsDefaultValueSearchFieldId;
 			aAppSearchViewFieldDto.EmInternalCodeRegistration = aAppSearchViewFieldEntity.EmInternalCodeRegistration;
 			aAppSearchViewFieldDto.IsPartnerFilterFiled = aAppSearchViewFieldEntity.IsPartnerFilterFiled;
 			aAppSearchViewFieldDto.JoinToParentViewFieldId = aAppSearchViewFieldEntity.JoinToParentViewFieldId;
 			aAppSearchViewFieldDto.IsCalulationField = aAppSearchViewFieldEntity.IsCalulationField;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSearchViewFieldDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSearchViewFieldEntity.AppCreatedDate);
                aAppSearchViewFieldDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSearchViewFieldEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSearchViewFieldEntity, aAppSearchViewFieldDto);
		}
		
		 /// <summary>
        ///  Copy AppSearchViewFieldDto Properties to   AppSearchViewFieldEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSearchViewFieldEntity aAppSearchViewFieldEntity,AppSearchViewFieldDto aAppSearchViewFieldDto)
        {        
 
      			aAppSearchViewFieldEntity.SearchViewId = aAppSearchViewFieldDto.SearchViewId;
      			aAppSearchViewFieldEntity.IsVisible = aAppSearchViewFieldDto.IsVisible;
      			aAppSearchViewFieldEntity.DisplayText = aAppSearchViewFieldDto.DisplayText;
      			aAppSearchViewFieldEntity.Sort = aAppSearchViewFieldDto.Sort;
      			aAppSearchViewFieldEntity.Width = aAppSearchViewFieldDto.Width;
      			aAppSearchViewFieldEntity.SysTableFiledPath = aAppSearchViewFieldDto.SysTableFiledPath;
      			aAppSearchViewFieldEntity.MassUpdateTransactionFieldId = aAppSearchViewFieldDto.MassUpdateTransactionFieldId;
      			aAppSearchViewFieldEntity.ControlType = aAppSearchViewFieldDto.ControlType;
      			aAppSearchViewFieldEntity.EntityId = aAppSearchViewFieldDto.EntityId;
      			aAppSearchViewFieldEntity.DataType = aAppSearchViewFieldDto.DataType;
      			aAppSearchViewFieldEntity.IsGroupBy = aAppSearchViewFieldDto.IsGroupBy;
      			aAppSearchViewFieldEntity.GroupByLevel = aAppSearchViewFieldDto.GroupByLevel;
      			aAppSearchViewFieldEntity.AggregationFunctionType = aAppSearchViewFieldDto.AggregationFunctionType;
      			aAppSearchViewFieldEntity.IsFilterByCurrentUser = aAppSearchViewFieldDto.IsFilterByCurrentUser;
      			aAppSearchViewFieldEntity.IsMapToChartX = aAppSearchViewFieldDto.IsMapToChartX;
      			aAppSearchViewFieldEntity.IsMapToChartY = aAppSearchViewFieldDto.IsMapToChartY;
      			aAppSearchViewFieldEntity.ChartYmappingOrder = aAppSearchViewFieldDto.ChartYmappingOrder;
      			aAppSearchViewFieldEntity.TreeLevel = aAppSearchViewFieldDto.TreeLevel;
      			aAppSearchViewFieldEntity.IsTreeNodeId = aAppSearchViewFieldDto.IsTreeNodeId;
      			aAppSearchViewFieldEntity.IsTreeNodeDisplay = aAppSearchViewFieldDto.IsTreeNodeDisplay;
      			aAppSearchViewFieldEntity.IsTreeNodeDesc = aAppSearchViewFieldDto.IsTreeNodeDesc;
      			aAppSearchViewFieldEntity.IsTreeNodeImageUrl = aAppSearchViewFieldDto.IsTreeNodeImageUrl;
      			aAppSearchViewFieldEntity.MappingSearchFieldId = aAppSearchViewFieldDto.MappingSearchFieldId;
      			aAppSearchViewFieldEntity.ProductDetaiMapTransFiledId = aAppSearchViewFieldDto.ProductDetaiMapTransFiledId;
      			aAppSearchViewFieldEntity.IsUserDefined1 = aAppSearchViewFieldDto.IsUserDefined1;
      			aAppSearchViewFieldEntity.IsUserDefined2 = aAppSearchViewFieldDto.IsUserDefined2;
      			aAppSearchViewFieldEntity.IsUserDefined3 = aAppSearchViewFieldDto.IsUserDefined3;
      			aAppSearchViewFieldEntity.IsUserDefined4 = aAppSearchViewFieldDto.IsUserDefined4;
      			aAppSearchViewFieldEntity.IsFileFoderId = aAppSearchViewFieldDto.IsFileFoderId;
      			aAppSearchViewFieldEntity.IsTransRootId = aAppSearchViewFieldDto.IsTransRootId;
 
  
   
    
      			aAppSearchViewFieldEntity.AppCreatedByCompanyId = aAppSearchViewFieldDto.AppCreatedByCompanyId;
      			aAppSearchViewFieldEntity.RowNumber = aAppSearchViewFieldDto.RowNumber;
      			aAppSearchViewFieldEntity.ColumnNumber = aAppSearchViewFieldDto.ColumnNumber;
      			aAppSearchViewFieldEntity.OrderByLevel = aAppSearchViewFieldDto.OrderByLevel;
      			aAppSearchViewFieldEntity.IsDescOrder = aAppSearchViewFieldDto.IsDescOrder;
      			aAppSearchViewFieldEntity.IsReadOnly = aAppSearchViewFieldDto.IsReadOnly;
      			aAppSearchViewFieldEntity.PullCriteriaAsDefaultValueSearchFieldId = aAppSearchViewFieldDto.PullCriteriaAsDefaultValueSearchFieldId;
      			aAppSearchViewFieldEntity.EmInternalCodeRegistration = aAppSearchViewFieldDto.EmInternalCodeRegistration;
      			aAppSearchViewFieldEntity.IsPartnerFilterFiled = aAppSearchViewFieldDto.IsPartnerFilterFiled;
      			aAppSearchViewFieldEntity.JoinToParentViewFieldId = aAppSearchViewFieldDto.JoinToParentViewFieldId;
      			aAppSearchViewFieldEntity.IsCalulationField = aAppSearchViewFieldDto.IsCalulationField;
			
			if(aAppSearchViewFieldDto.Id == null)
			{
				aAppSearchViewFieldEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSearchViewFieldEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSearchViewFieldEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSearchViewFieldEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSearchViewFieldEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSearchViewFieldEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSearchViewFieldEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSearchViewFieldEntity, aAppSearchViewFieldDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSearchViewFieldEntity aAppSearchViewFieldEntity,AppSearchViewFieldDto aAppSearchViewFieldDto);
		
		static partial void OnCopyDtoToEntityDone(AppSearchViewFieldEntity aAppSearchViewFieldEntity,AppSearchViewFieldDto aAppSearchViewFieldDto);
		
   
       
    }
}

 