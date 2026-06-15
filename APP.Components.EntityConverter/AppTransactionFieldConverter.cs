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
    /// Convert Properties between  AppTransactionFieldEntity and  AppTransactionFieldDto
    /// </summary>
    public static partial class AppTransactionFieldConverter 
    {
         /// <summary>
        ///  Convert AppTransactionFieldEntity To  AppTransactionFieldDto
        /// </summary>
        public static AppTransactionFieldDto ConvertEntityToDto(AppTransactionFieldEntity aAppTransactionFieldEntity)
        {        
    		AppTransactionFieldDto aAppTransactionFieldDto = new AppTransactionFieldDto();
    		CopyEntityPropertyToDto( aAppTransactionFieldEntity, aAppTransactionFieldDto);          
			return aAppTransactionFieldDto;
        }
		 /// <summary>
        ///  Convert AppTransactionFieldEntity To  AppTransactionFieldExDto
        /// </summary>
        public static AppTransactionFieldExDto ConvertEntityToExDto(AppTransactionFieldEntity aAppTransactionFieldEntity)
        {        
    		AppTransactionFieldExDto aAppTransactionFieldExDto = new AppTransactionFieldExDto();
			CopyEntityPropertyToDto( aAppTransactionFieldEntity, aAppTransactionFieldExDto);
			
			
			
            return aAppTransactionFieldExDto;
        }
		
		 /// <summary>
        ///  Convert AppTransactionFieldEntity To  AppTransactionFieldDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppTransactionFieldEntity aAppTransactionFieldEntity,AppTransactionFieldDto aAppTransactionFieldDto)
        {        
    		
           // aAppTransactionFieldDto.StopChangeTracking();
 			aAppTransactionFieldDto.Id = aAppTransactionFieldEntity.TransactionFieldId;
 			aAppTransactionFieldDto.TransactionUnitId = aAppTransactionFieldEntity.TransactionUnitId;
 			aAppTransactionFieldDto.DisplayName = aAppTransactionFieldEntity.DisplayName;
 			aAppTransactionFieldDto.DataBaseFieldName = aAppTransactionFieldEntity.DataBaseFieldName;
 			aAppTransactionFieldDto.ControlType = aAppTransactionFieldEntity.ControlType;
 			aAppTransactionFieldDto.DataType = aAppTransactionFieldEntity.DataType;
 			aAppTransactionFieldDto.EntityId = aAppTransactionFieldEntity.EntityId;
 			aAppTransactionFieldDto.InternalCode = aAppTransactionFieldEntity.InternalCode;
 			aAppTransactionFieldDto.NeedValidator = aAppTransactionFieldEntity.NeedValidator;
 			aAppTransactionFieldDto.ValidatorType = aAppTransactionFieldEntity.ValidatorType;
 			aAppTransactionFieldDto.Nbdecimal = aAppTransactionFieldEntity.Nbdecimal;
 			aAppTransactionFieldDto.SortOrder = aAppTransactionFieldEntity.SortOrder;
 			aAppTransactionFieldDto.MaxCharLegnth = aAppTransactionFieldEntity.MaxCharLegnth;
 			aAppTransactionFieldDto.MaxNumber = aAppTransactionFieldEntity.MaxNumber;
 			aAppTransactionFieldDto.DdlparentLevelId = aAppTransactionFieldEntity.DdlparentLevelId;
 			aAppTransactionFieldDto.AutoIncrementSeed = aAppTransactionFieldEntity.AutoIncrementSeed;
 			aAppTransactionFieldDto.AutoIncrementPrefix = aAppTransactionFieldEntity.AutoIncrementPrefix;
 			aAppTransactionFieldDto.AutoIncrementLastId = aAppTransactionFieldEntity.AutoIncrementLastId;
 			aAppTransactionFieldDto.IsNeedLog = aAppTransactionFieldEntity.IsNeedLog;
 			aAppTransactionFieldDto.IsAllowEmpty = aAppTransactionFieldEntity.IsAllowEmpty;
 			aAppTransactionFieldDto.ToolTip = aAppTransactionFieldEntity.ToolTip;
 			aAppTransactionFieldDto.IsConvertToUpperCase = aAppTransactionFieldEntity.IsConvertToUpperCase;
 			aAppTransactionFieldDto.DefaultValue = aAppTransactionFieldEntity.DefaultValue;
 			aAppTransactionFieldDto.CascadingRelationTableSchemaOwner = aAppTransactionFieldEntity.CascadingRelationTableSchemaOwner;
 			aAppTransactionFieldDto.CascadingRelationTable = aAppTransactionFieldEntity.CascadingRelationTable;
 			aAppTransactionFieldDto.CascadingRelationTableParentKeyField = aAppTransactionFieldEntity.CascadingRelationTableParentKeyField;
 			aAppTransactionFieldDto.CascadingRelationTableChildKeyField = aAppTransactionFieldEntity.CascadingRelationTableChildKeyField;
 			aAppTransactionFieldDto.MasterEntityFieldlId = aAppTransactionFieldEntity.MasterEntityFieldlId;
 			aAppTransactionFieldDto.InnerEntitySubscribeFiled = aAppTransactionFieldEntity.InnerEntitySubscribeFiled;
 			aAppTransactionFieldDto.DisplayWidth = aAppTransactionFieldEntity.DisplayWidth;
 			aAppTransactionFieldDto.IsReadonly = aAppTransactionFieldEntity.IsReadonly;
 			aAppTransactionFieldDto.ChildUnitSubscribeParentFieldId = aAppTransactionFieldEntity.ChildUnitSubscribeParentFieldId;
 			aAppTransactionFieldDto.ParentUnitSubscribeChildAggFunctionId = aAppTransactionFieldEntity.ParentUnitSubscribeChildAggFunctionId;
 			aAppTransactionFieldDto.IsGridUseAvailableEntitySource = aAppTransactionFieldEntity.IsGridUseAvailableEntitySource;
 			aAppTransactionFieldDto.IsUnique = aAppTransactionFieldEntity.IsUnique;
 			aAppTransactionFieldDto.DataRetrieveType = aAppTransactionFieldEntity.DataRetrieveType;
 			aAppTransactionFieldDto.AppExternalSourceFrom = aAppTransactionFieldEntity.AppExternalSourceFrom;
 			aAppTransactionFieldDto.IsGroupBy = aAppTransactionFieldEntity.IsGroupBy;
 			aAppTransactionFieldDto.GroupByLevel = aAppTransactionFieldEntity.GroupByLevel;
 			aAppTransactionFieldDto.MatrixKeyTransactionFieldId = aAppTransactionFieldEntity.MatrixKeyTransactionFieldId;
 			aAppTransactionFieldDto.IsPrimaryKey = aAppTransactionFieldEntity.IsPrimaryKey;
 			aAppTransactionFieldDto.IsLinkToParentPrimaryKey = aAppTransactionFieldEntity.IsLinkToParentPrimaryKey;
 			aAppTransactionFieldDto.LinkToParentPrimaryKeyFieldId = aAppTransactionFieldEntity.LinkToParentPrimaryKeyFieldId;
 			aAppTransactionFieldDto.IsVisible = aAppTransactionFieldEntity.IsVisible;
 			aAppTransactionFieldDto.IsFilterByCurrentUser = aAppTransactionFieldEntity.IsFilterByCurrentUser;
 			aAppTransactionFieldDto.SystemVariableEnumCode = aAppTransactionFieldEntity.SystemVariableEnumCode;
 			aAppTransactionFieldDto.MatrixForeignKeyFieldId = aAppTransactionFieldEntity.MatrixForeignKeyFieldId;
 			aAppTransactionFieldDto.IsPivotRow = aAppTransactionFieldEntity.IsPivotRow;
 			aAppTransactionFieldDto.IsPivotColumn = aAppTransactionFieldEntity.IsPivotColumn;
 			aAppTransactionFieldDto.DdlQueryText = aAppTransactionFieldEntity.DdlQueryText;
 			aAppTransactionFieldDto.WhereClauseExpress = aAppTransactionFieldEntity.WhereClauseExpress;
 			aAppTransactionFieldDto.DdlForeignUnitId = aAppTransactionFieldEntity.DdlForeignUnitId;
 			aAppTransactionFieldDto.DdlForeignUnitDisplayDbFieds = aAppTransactionFieldEntity.DdlForeignUnitDisplayDbFieds;
 			aAppTransactionFieldDto.FileControlTypeFolderTransactionId = aAppTransactionFieldEntity.FileControlTypeFolderTransactionId;
 			aAppTransactionFieldDto.RowIdentityGuid = aAppTransactionFieldEntity.RowIdentityGuid;
 			aAppTransactionFieldDto.MappingEmSystemTokenField = aAppTransactionFieldEntity.MappingEmSystemTokenField;
 			aAppTransactionFieldDto.IsLogicalDisplay = aAppTransactionFieldEntity.IsLogicalDisplay;
 			aAppTransactionFieldDto.IsChangeTrigerNotification = aAppTransactionFieldEntity.IsChangeTrigerNotification;
 			aAppTransactionFieldDto.SiblingUnitLogicalKeyFieldId = aAppTransactionFieldEntity.SiblingUnitLogicalKeyFieldId;
 			aAppTransactionFieldDto.AppCreatedById = aAppTransactionFieldEntity.AppCreatedById;
 			aAppTransactionFieldDto.AppCreatedDate = aAppTransactionFieldEntity.AppCreatedDate;
 			aAppTransactionFieldDto.AppModifiedDate = aAppTransactionFieldEntity.AppModifiedDate;
 			aAppTransactionFieldDto.AppModifiedById = aAppTransactionFieldEntity.AppModifiedById;
 			aAppTransactionFieldDto.AppCreatedByCompanyId = aAppTransactionFieldEntity.AppCreatedByCompanyId;
 			aAppTransactionFieldDto.IsFieldExclusiveForOwner = aAppTransactionFieldEntity.IsFieldExclusiveForOwner;
 			aAppTransactionFieldDto.IsAllowEditOnMobileRowPopup = aAppTransactionFieldEntity.IsAllowEditOnMobileRowPopup;
 			aAppTransactionFieldDto.EmInternalCodeRegistration = aAppTransactionFieldEntity.EmInternalCodeRegistration;
 			aAppTransactionFieldDto.HostFormLayoutItemId = aAppTransactionFieldEntity.HostFormLayoutItemId;
 			aAppTransactionFieldDto.IsPivotValue = aAppTransactionFieldEntity.IsPivotValue;
 			aAppTransactionFieldDto.PivotAggregationType = aAppTransactionFieldEntity.PivotAggregationType;
 			aAppTransactionFieldDto.ControlTypeParam1 = aAppTransactionFieldEntity.ControlTypeParam1;
 			aAppTransactionFieldDto.ControlTypeParam2 = aAppTransactionFieldEntity.ControlTypeParam2;
 			aAppTransactionFieldDto.ControlTypeParam3 = aAppTransactionFieldEntity.ControlTypeParam3;
 			aAppTransactionFieldDto.IsPrintVisible = aAppTransactionFieldEntity.IsPrintVisible;
 			aAppTransactionFieldDto.OnChangeTriggerToCommandId = aAppTransactionFieldEntity.OnChangeTriggerToCommandId;
 			aAppTransactionFieldDto.IsTempVariable = aAppTransactionFieldEntity.IsTempVariable;
 			aAppTransactionFieldDto.MappingToAvailableSourceUnitTransactionFieldId = aAppTransactionFieldEntity.MappingToAvailableSourceUnitTransactionFieldId;
 			aAppTransactionFieldDto.IsStoreToExtendTable = aAppTransactionFieldEntity.IsStoreToExtendTable;
 			aAppTransactionFieldDto.InnerEntityLabelSubscribeFiled = aAppTransactionFieldEntity.InnerEntityLabelSubscribeFiled;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppTransactionFieldDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionFieldEntity.AppCreatedDate);
                aAppTransactionFieldDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionFieldEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppTransactionFieldEntity, aAppTransactionFieldDto);
		}
		
		 /// <summary>
        ///  Copy AppTransactionFieldDto Properties to   AppTransactionFieldEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppTransactionFieldEntity aAppTransactionFieldEntity,AppTransactionFieldDto aAppTransactionFieldDto)
        {        
 
      			aAppTransactionFieldEntity.TransactionUnitId = aAppTransactionFieldDto.TransactionUnitId;
      			aAppTransactionFieldEntity.DisplayName = aAppTransactionFieldDto.DisplayName;
      			aAppTransactionFieldEntity.DataBaseFieldName = aAppTransactionFieldDto.DataBaseFieldName;
      			aAppTransactionFieldEntity.ControlType = aAppTransactionFieldDto.ControlType;
      			aAppTransactionFieldEntity.DataType = aAppTransactionFieldDto.DataType;
      			aAppTransactionFieldEntity.EntityId = aAppTransactionFieldDto.EntityId;
      			aAppTransactionFieldEntity.InternalCode = aAppTransactionFieldDto.InternalCode;
      			aAppTransactionFieldEntity.NeedValidator = aAppTransactionFieldDto.NeedValidator;
      			aAppTransactionFieldEntity.ValidatorType = aAppTransactionFieldDto.ValidatorType;
      			aAppTransactionFieldEntity.Nbdecimal = aAppTransactionFieldDto.Nbdecimal;
      			aAppTransactionFieldEntity.SortOrder = aAppTransactionFieldDto.SortOrder;
      			aAppTransactionFieldEntity.MaxCharLegnth = aAppTransactionFieldDto.MaxCharLegnth;
      			aAppTransactionFieldEntity.MaxNumber = aAppTransactionFieldDto.MaxNumber;
      			aAppTransactionFieldEntity.DdlparentLevelId = aAppTransactionFieldDto.DdlparentLevelId;
      			aAppTransactionFieldEntity.AutoIncrementSeed = aAppTransactionFieldDto.AutoIncrementSeed;
      			aAppTransactionFieldEntity.AutoIncrementPrefix = aAppTransactionFieldDto.AutoIncrementPrefix;
      			aAppTransactionFieldEntity.AutoIncrementLastId = aAppTransactionFieldDto.AutoIncrementLastId;
      			aAppTransactionFieldEntity.IsNeedLog = aAppTransactionFieldDto.IsNeedLog;
      			aAppTransactionFieldEntity.IsAllowEmpty = aAppTransactionFieldDto.IsAllowEmpty;
      			aAppTransactionFieldEntity.ToolTip = aAppTransactionFieldDto.ToolTip;
      			aAppTransactionFieldEntity.IsConvertToUpperCase = aAppTransactionFieldDto.IsConvertToUpperCase;
      			aAppTransactionFieldEntity.DefaultValue = aAppTransactionFieldDto.DefaultValue;
      			aAppTransactionFieldEntity.CascadingRelationTableSchemaOwner = aAppTransactionFieldDto.CascadingRelationTableSchemaOwner;
      			aAppTransactionFieldEntity.CascadingRelationTable = aAppTransactionFieldDto.CascadingRelationTable;
      			aAppTransactionFieldEntity.CascadingRelationTableParentKeyField = aAppTransactionFieldDto.CascadingRelationTableParentKeyField;
      			aAppTransactionFieldEntity.CascadingRelationTableChildKeyField = aAppTransactionFieldDto.CascadingRelationTableChildKeyField;
      			aAppTransactionFieldEntity.MasterEntityFieldlId = aAppTransactionFieldDto.MasterEntityFieldlId;
      			aAppTransactionFieldEntity.InnerEntitySubscribeFiled = aAppTransactionFieldDto.InnerEntitySubscribeFiled;
      			aAppTransactionFieldEntity.DisplayWidth = aAppTransactionFieldDto.DisplayWidth;
      			aAppTransactionFieldEntity.IsReadonly = aAppTransactionFieldDto.IsReadonly;
      			aAppTransactionFieldEntity.ChildUnitSubscribeParentFieldId = aAppTransactionFieldDto.ChildUnitSubscribeParentFieldId;
      			aAppTransactionFieldEntity.ParentUnitSubscribeChildAggFunctionId = aAppTransactionFieldDto.ParentUnitSubscribeChildAggFunctionId;
      			aAppTransactionFieldEntity.IsGridUseAvailableEntitySource = aAppTransactionFieldDto.IsGridUseAvailableEntitySource;
      			aAppTransactionFieldEntity.IsUnique = aAppTransactionFieldDto.IsUnique;
      			aAppTransactionFieldEntity.DataRetrieveType = aAppTransactionFieldDto.DataRetrieveType;
      			aAppTransactionFieldEntity.AppExternalSourceFrom = aAppTransactionFieldDto.AppExternalSourceFrom;
      			aAppTransactionFieldEntity.IsGroupBy = aAppTransactionFieldDto.IsGroupBy;
      			aAppTransactionFieldEntity.GroupByLevel = aAppTransactionFieldDto.GroupByLevel;
      			aAppTransactionFieldEntity.MatrixKeyTransactionFieldId = aAppTransactionFieldDto.MatrixKeyTransactionFieldId;
      			aAppTransactionFieldEntity.IsPrimaryKey = aAppTransactionFieldDto.IsPrimaryKey;
      			aAppTransactionFieldEntity.IsLinkToParentPrimaryKey = aAppTransactionFieldDto.IsLinkToParentPrimaryKey;
      			aAppTransactionFieldEntity.LinkToParentPrimaryKeyFieldId = aAppTransactionFieldDto.LinkToParentPrimaryKeyFieldId;
      			aAppTransactionFieldEntity.IsVisible = aAppTransactionFieldDto.IsVisible;
      			aAppTransactionFieldEntity.IsFilterByCurrentUser = aAppTransactionFieldDto.IsFilterByCurrentUser;
      			aAppTransactionFieldEntity.SystemVariableEnumCode = aAppTransactionFieldDto.SystemVariableEnumCode;
      			aAppTransactionFieldEntity.MatrixForeignKeyFieldId = aAppTransactionFieldDto.MatrixForeignKeyFieldId;
      			aAppTransactionFieldEntity.IsPivotRow = aAppTransactionFieldDto.IsPivotRow;
      			aAppTransactionFieldEntity.IsPivotColumn = aAppTransactionFieldDto.IsPivotColumn;
      			aAppTransactionFieldEntity.DdlQueryText = aAppTransactionFieldDto.DdlQueryText;
      			aAppTransactionFieldEntity.WhereClauseExpress = aAppTransactionFieldDto.WhereClauseExpress;
      			aAppTransactionFieldEntity.DdlForeignUnitId = aAppTransactionFieldDto.DdlForeignUnitId;
      			aAppTransactionFieldEntity.DdlForeignUnitDisplayDbFieds = aAppTransactionFieldDto.DdlForeignUnitDisplayDbFieds;
      			aAppTransactionFieldEntity.FileControlTypeFolderTransactionId = aAppTransactionFieldDto.FileControlTypeFolderTransactionId;
      			aAppTransactionFieldEntity.RowIdentityGuid = aAppTransactionFieldDto.RowIdentityGuid;
      			aAppTransactionFieldEntity.MappingEmSystemTokenField = aAppTransactionFieldDto.MappingEmSystemTokenField;
      			aAppTransactionFieldEntity.IsLogicalDisplay = aAppTransactionFieldDto.IsLogicalDisplay;
      			aAppTransactionFieldEntity.IsChangeTrigerNotification = aAppTransactionFieldDto.IsChangeTrigerNotification;
      			aAppTransactionFieldEntity.SiblingUnitLogicalKeyFieldId = aAppTransactionFieldDto.SiblingUnitLogicalKeyFieldId;
 
  
   
    
      			aAppTransactionFieldEntity.AppCreatedByCompanyId = aAppTransactionFieldDto.AppCreatedByCompanyId;
      			aAppTransactionFieldEntity.IsFieldExclusiveForOwner = aAppTransactionFieldDto.IsFieldExclusiveForOwner;
      			aAppTransactionFieldEntity.IsAllowEditOnMobileRowPopup = aAppTransactionFieldDto.IsAllowEditOnMobileRowPopup;
      			aAppTransactionFieldEntity.EmInternalCodeRegistration = aAppTransactionFieldDto.EmInternalCodeRegistration;
      			aAppTransactionFieldEntity.HostFormLayoutItemId = aAppTransactionFieldDto.HostFormLayoutItemId;
      			aAppTransactionFieldEntity.IsPivotValue = aAppTransactionFieldDto.IsPivotValue;
      			aAppTransactionFieldEntity.PivotAggregationType = aAppTransactionFieldDto.PivotAggregationType;
      			aAppTransactionFieldEntity.ControlTypeParam1 = aAppTransactionFieldDto.ControlTypeParam1;
      			aAppTransactionFieldEntity.ControlTypeParam2 = aAppTransactionFieldDto.ControlTypeParam2;
      			aAppTransactionFieldEntity.ControlTypeParam3 = aAppTransactionFieldDto.ControlTypeParam3;
      			aAppTransactionFieldEntity.IsPrintVisible = aAppTransactionFieldDto.IsPrintVisible;
      			aAppTransactionFieldEntity.OnChangeTriggerToCommandId = aAppTransactionFieldDto.OnChangeTriggerToCommandId;
      			aAppTransactionFieldEntity.IsTempVariable = aAppTransactionFieldDto.IsTempVariable;
      			aAppTransactionFieldEntity.MappingToAvailableSourceUnitTransactionFieldId = aAppTransactionFieldDto.MappingToAvailableSourceUnitTransactionFieldId;
      			aAppTransactionFieldEntity.IsStoreToExtendTable = aAppTransactionFieldDto.IsStoreToExtendTable;
      			aAppTransactionFieldEntity.InnerEntityLabelSubscribeFiled = aAppTransactionFieldDto.InnerEntityLabelSubscribeFiled;
			
			if(aAppTransactionFieldDto.Id == null)
			{
				aAppTransactionFieldEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionFieldEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppTransactionFieldEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionFieldEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionFieldEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppTransactionFieldEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionFieldEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppTransactionFieldEntity, aAppTransactionFieldDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppTransactionFieldEntity aAppTransactionFieldEntity,AppTransactionFieldDto aAppTransactionFieldDto);
		
		static partial void OnCopyDtoToEntityDone(AppTransactionFieldEntity aAppTransactionFieldEntity,AppTransactionFieldDto aAppTransactionFieldDto);
		
   
       
    }
}

 