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
    /// Convert Properties between  AppSearchFieldEntity and  AppSearchFieldDto
    /// </summary>
    public static partial class AppSearchFieldConverter 
    {
         /// <summary>
        ///  Convert AppSearchFieldEntity To  AppSearchFieldDto
        /// </summary>
        public static AppSearchFieldDto ConvertEntityToDto(AppSearchFieldEntity aAppSearchFieldEntity)
        {        
    		AppSearchFieldDto aAppSearchFieldDto = new AppSearchFieldDto();
    		CopyEntityPropertyToDto( aAppSearchFieldEntity, aAppSearchFieldDto);          
			return aAppSearchFieldDto;
        }
		 /// <summary>
        ///  Convert AppSearchFieldEntity To  AppSearchFieldExDto
        /// </summary>
        public static AppSearchFieldExDto ConvertEntityToExDto(AppSearchFieldEntity aAppSearchFieldEntity)
        {        
    		AppSearchFieldExDto aAppSearchFieldExDto = new AppSearchFieldExDto();
			CopyEntityPropertyToDto( aAppSearchFieldEntity, aAppSearchFieldExDto);
			
			
			
            return aAppSearchFieldExDto;
        }
		
		 /// <summary>
        ///  Convert AppSearchFieldEntity To  AppSearchFieldDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSearchFieldEntity aAppSearchFieldEntity,AppSearchFieldDto aAppSearchFieldDto)
        {        
    		
           // aAppSearchFieldDto.StopChangeTracking();
 			aAppSearchFieldDto.Id = aAppSearchFieldEntity.SearchFielDid;
 			aAppSearchFieldDto.SearchId = aAppSearchFieldEntity.SearchId;
 			aAppSearchFieldDto.Sort = aAppSearchFieldEntity.Sort;
 			aAppSearchFieldDto.PositionRow = aAppSearchFieldEntity.PositionRow;
 			aAppSearchFieldDto.PositionColumn = aAppSearchFieldEntity.PositionColumn;
 			aAppSearchFieldDto.OperationId = aAppSearchFieldEntity.OperationId;
 			aAppSearchFieldDto.DisplayText = aAppSearchFieldEntity.DisplayText;
 			aAppSearchFieldDto.IsVisible = aAppSearchFieldEntity.IsVisible;
 			aAppSearchFieldDto.DefaultValue = aAppSearchFieldEntity.DefaultValue;
 			aAppSearchFieldDto.IsReadOnly = aAppSearchFieldEntity.IsReadOnly;
 			aAppSearchFieldDto.IsAutoPopulate = aAppSearchFieldEntity.IsAutoPopulate;
 			aAppSearchFieldDto.ParentFieldId = aAppSearchFieldEntity.ParentFieldId;
 			aAppSearchFieldDto.IsLoadOnDemand = aAppSearchFieldEntity.IsLoadOnDemand;
 			aAppSearchFieldDto.SysTableFiledPath = aAppSearchFieldEntity.SysTableFiledPath;
 			aAppSearchFieldDto.SysTableFiledFullPath = aAppSearchFieldEntity.SysTableFiledFullPath;
 			aAppSearchFieldDto.ControlType = aAppSearchFieldEntity.ControlType;
 			aAppSearchFieldDto.EntityId = aAppSearchFieldEntity.EntityId;
 			aAppSearchFieldDto.DataType = aAppSearchFieldEntity.DataType;
 			aAppSearchFieldDto.IsFilterByCurrentUser = aAppSearchFieldEntity.IsFilterByCurrentUser;
 			aAppSearchFieldDto.AppCreatedById = aAppSearchFieldEntity.AppCreatedById;
 			aAppSearchFieldDto.AppCreatedDate = aAppSearchFieldEntity.AppCreatedDate;
 			aAppSearchFieldDto.AppModifiedDate = aAppSearchFieldEntity.AppModifiedDate;
 			aAppSearchFieldDto.AppModifiedById = aAppSearchFieldEntity.AppModifiedById;
 			aAppSearchFieldDto.AppCreatedByCompanyId = aAppSearchFieldEntity.AppCreatedByCompanyId;
 			aAppSearchFieldDto.IsChangedAutoExecute = aAppSearchFieldEntity.IsChangedAutoExecute;
 			aAppSearchFieldDto.StartValueEntityField = aAppSearchFieldEntity.StartValueEntityField;
 			aAppSearchFieldDto.EndValueEntityField = aAppSearchFieldEntity.EndValueEntityField;
 			aAppSearchFieldDto.StartValueDataSetField = aAppSearchFieldEntity.StartValueDataSetField;
 			aAppSearchFieldDto.EndValueDataSetField = aAppSearchFieldEntity.EndValueDataSetField;
 			aAppSearchFieldDto.SubControlType = aAppSearchFieldEntity.SubControlType;
 			aAppSearchFieldDto.CascadingRelationTable = aAppSearchFieldEntity.CascadingRelationTable;
 			aAppSearchFieldDto.CascadingRelationTableParentKeyField = aAppSearchFieldEntity.CascadingRelationTableParentKeyField;
 			aAppSearchFieldDto.CascadingRelationTableChildKeyField = aAppSearchFieldEntity.CascadingRelationTableChildKeyField;
 			aAppSearchFieldDto.IsAllowMultipleSelect = aAppSearchFieldEntity.IsAllowMultipleSelect;
 			aAppSearchFieldDto.MasterEntityFieldlId = aAppSearchFieldEntity.MasterEntityFieldlId;
 			aAppSearchFieldDto.InnerEntitySubscribeFiled = aAppSearchFieldEntity.InnerEntitySubscribeFiled;
 			aAppSearchFieldDto.IsSkipSearch = aAppSearchFieldEntity.IsSkipSearch;
 			aAppSearchFieldDto.DataRetrieveType = aAppSearchFieldEntity.DataRetrieveType;
 			aAppSearchFieldDto.CascadingRelationTableSchemaOwner = aAppSearchFieldEntity.CascadingRelationTableSchemaOwner;
 			aAppSearchFieldDto.AppExternalSourceFrom = aAppSearchFieldEntity.AppExternalSourceFrom;
 			aAppSearchFieldDto.DdlQueryText = aAppSearchFieldEntity.DdlQueryText;
 			aAppSearchFieldDto.WhereClauseExpress = aAppSearchFieldEntity.WhereClauseExpress;
 			aAppSearchFieldDto.EmInternalCodeRegistration = aAppSearchFieldEntity.EmInternalCodeRegistration;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSearchFieldDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSearchFieldEntity.AppCreatedDate);
                aAppSearchFieldDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSearchFieldEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSearchFieldEntity, aAppSearchFieldDto);
		}
		
		 /// <summary>
        ///  Copy AppSearchFieldDto Properties to   AppSearchFieldEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSearchFieldEntity aAppSearchFieldEntity,AppSearchFieldDto aAppSearchFieldDto)
        {        
 
      			aAppSearchFieldEntity.SearchId = aAppSearchFieldDto.SearchId;
      			aAppSearchFieldEntity.Sort = aAppSearchFieldDto.Sort;
      			aAppSearchFieldEntity.PositionRow = aAppSearchFieldDto.PositionRow;
      			aAppSearchFieldEntity.PositionColumn = aAppSearchFieldDto.PositionColumn;
      			aAppSearchFieldEntity.OperationId = aAppSearchFieldDto.OperationId;
      			aAppSearchFieldEntity.DisplayText = aAppSearchFieldDto.DisplayText;
      			aAppSearchFieldEntity.IsVisible = aAppSearchFieldDto.IsVisible;
      			aAppSearchFieldEntity.DefaultValue = aAppSearchFieldDto.DefaultValue;
      			aAppSearchFieldEntity.IsReadOnly = aAppSearchFieldDto.IsReadOnly;
      			aAppSearchFieldEntity.IsAutoPopulate = aAppSearchFieldDto.IsAutoPopulate;
      			aAppSearchFieldEntity.ParentFieldId = aAppSearchFieldDto.ParentFieldId;
      			aAppSearchFieldEntity.IsLoadOnDemand = aAppSearchFieldDto.IsLoadOnDemand;
      			aAppSearchFieldEntity.SysTableFiledPath = aAppSearchFieldDto.SysTableFiledPath;
      			aAppSearchFieldEntity.SysTableFiledFullPath = aAppSearchFieldDto.SysTableFiledFullPath;
      			aAppSearchFieldEntity.ControlType = aAppSearchFieldDto.ControlType;
      			aAppSearchFieldEntity.EntityId = aAppSearchFieldDto.EntityId;
      			aAppSearchFieldEntity.DataType = aAppSearchFieldDto.DataType;
      			aAppSearchFieldEntity.IsFilterByCurrentUser = aAppSearchFieldDto.IsFilterByCurrentUser;
 
  
   
    
      			aAppSearchFieldEntity.AppCreatedByCompanyId = aAppSearchFieldDto.AppCreatedByCompanyId;
      			aAppSearchFieldEntity.IsChangedAutoExecute = aAppSearchFieldDto.IsChangedAutoExecute;
      			aAppSearchFieldEntity.StartValueEntityField = aAppSearchFieldDto.StartValueEntityField;
      			aAppSearchFieldEntity.EndValueEntityField = aAppSearchFieldDto.EndValueEntityField;
      			aAppSearchFieldEntity.StartValueDataSetField = aAppSearchFieldDto.StartValueDataSetField;
      			aAppSearchFieldEntity.EndValueDataSetField = aAppSearchFieldDto.EndValueDataSetField;
      			aAppSearchFieldEntity.SubControlType = aAppSearchFieldDto.SubControlType;
      			aAppSearchFieldEntity.CascadingRelationTable = aAppSearchFieldDto.CascadingRelationTable;
      			aAppSearchFieldEntity.CascadingRelationTableParentKeyField = aAppSearchFieldDto.CascadingRelationTableParentKeyField;
      			aAppSearchFieldEntity.CascadingRelationTableChildKeyField = aAppSearchFieldDto.CascadingRelationTableChildKeyField;
      			aAppSearchFieldEntity.IsAllowMultipleSelect = aAppSearchFieldDto.IsAllowMultipleSelect;
      			aAppSearchFieldEntity.MasterEntityFieldlId = aAppSearchFieldDto.MasterEntityFieldlId;
      			aAppSearchFieldEntity.InnerEntitySubscribeFiled = aAppSearchFieldDto.InnerEntitySubscribeFiled;
      			aAppSearchFieldEntity.IsSkipSearch = aAppSearchFieldDto.IsSkipSearch;
      			aAppSearchFieldEntity.DataRetrieveType = aAppSearchFieldDto.DataRetrieveType;
      			aAppSearchFieldEntity.CascadingRelationTableSchemaOwner = aAppSearchFieldDto.CascadingRelationTableSchemaOwner;
      			aAppSearchFieldEntity.AppExternalSourceFrom = aAppSearchFieldDto.AppExternalSourceFrom;
      			aAppSearchFieldEntity.DdlQueryText = aAppSearchFieldDto.DdlQueryText;
      			aAppSearchFieldEntity.WhereClauseExpress = aAppSearchFieldDto.WhereClauseExpress;
      			aAppSearchFieldEntity.EmInternalCodeRegistration = aAppSearchFieldDto.EmInternalCodeRegistration;
			
			if(aAppSearchFieldDto.Id == null)
			{
				aAppSearchFieldEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSearchFieldEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSearchFieldEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSearchFieldEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSearchFieldEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSearchFieldEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSearchFieldEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSearchFieldEntity, aAppSearchFieldDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSearchFieldEntity aAppSearchFieldEntity,AppSearchFieldDto aAppSearchFieldDto);
		
		static partial void OnCopyDtoToEntityDone(AppSearchFieldEntity aAppSearchFieldEntity,AppSearchFieldDto aAppSearchFieldDto);
		
   
       
    }
}

 