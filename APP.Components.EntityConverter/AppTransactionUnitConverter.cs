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
    /// Convert Properties between  AppTransactionUnitEntity and  AppTransactionUnitDto
    /// </summary>
    public static partial class AppTransactionUnitConverter 
    {
         /// <summary>
        ///  Convert AppTransactionUnitEntity To  AppTransactionUnitDto
        /// </summary>
        public static AppTransactionUnitDto ConvertEntityToDto(AppTransactionUnitEntity aAppTransactionUnitEntity)
        {        
    		AppTransactionUnitDto aAppTransactionUnitDto = new AppTransactionUnitDto();
    		CopyEntityPropertyToDto( aAppTransactionUnitEntity, aAppTransactionUnitDto);          
			return aAppTransactionUnitDto;
        }
		 /// <summary>
        ///  Convert AppTransactionUnitEntity To  AppTransactionUnitExDto
        /// </summary>
        public static AppTransactionUnitExDto ConvertEntityToExDto(AppTransactionUnitEntity aAppTransactionUnitEntity)
        {        
    		AppTransactionUnitExDto aAppTransactionUnitExDto = new AppTransactionUnitExDto();
			CopyEntityPropertyToDto( aAppTransactionUnitEntity, aAppTransactionUnitExDto);
			
			
			
            return aAppTransactionUnitExDto;
        }
		
		 /// <summary>
        ///  Convert AppTransactionUnitEntity To  AppTransactionUnitDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppTransactionUnitEntity aAppTransactionUnitEntity,AppTransactionUnitDto aAppTransactionUnitDto)
        {        
    		
           // aAppTransactionUnitDto.StopChangeTracking();
 			aAppTransactionUnitDto.Id = aAppTransactionUnitEntity.TransactionUnitId;
 			aAppTransactionUnitDto.TransactionId = aAppTransactionUnitEntity.TransactionId;
 			aAppTransactionUnitDto.UnitDisplayName = aAppTransactionUnitEntity.UnitDisplayName;
 			aAppTransactionUnitDto.DataBaseTableName = aAppTransactionUnitEntity.DataBaseTableName;
 			aAppTransactionUnitDto.SchemaOwner = aAppTransactionUnitEntity.SchemaOwner;
 			aAppTransactionUnitDto.TransactionFlow = aAppTransactionUnitEntity.TransactionFlow;
 			aAppTransactionUnitDto.ParentTransactionUnitId = aAppTransactionUnitEntity.ParentTransactionUnitId;
 			aAppTransactionUnitDto.IsReadOnly = aAppTransactionUnitEntity.IsReadOnly;
 			aAppTransactionUnitDto.IsMatrixUnit = aAppTransactionUnitEntity.IsMatrixUnit;
 			aAppTransactionUnitDto.IsMatrixPivotUnit = aAppTransactionUnitEntity.IsMatrixPivotUnit;
 			aAppTransactionUnitDto.IsSynchToDatabaseTable = aAppTransactionUnitEntity.IsSynchToDatabaseTable;
 			aAppTransactionUnitDto.IsMasterSiblingUnit = aAppTransactionUnitEntity.IsMasterSiblingUnit;
 			aAppTransactionUnitDto.IsPrimaryKeyIdentityInsert = aAppTransactionUnitEntity.IsPrimaryKeyIdentityInsert;
 			aAppTransactionUnitDto.AppCreatedById = aAppTransactionUnitEntity.AppCreatedById;
 			aAppTransactionUnitDto.AppCreatedDate = aAppTransactionUnitEntity.AppCreatedDate;
 			aAppTransactionUnitDto.AppModifiedDate = aAppTransactionUnitEntity.AppModifiedDate;
 			aAppTransactionUnitDto.AppModifiedById = aAppTransactionUnitEntity.AppModifiedById;
 			aAppTransactionUnitDto.AppCreatedByCompanyId = aAppTransactionUnitEntity.AppCreatedByCompanyId;
 			aAppTransactionUnitDto.IsExclusiveForOwner = aAppTransactionUnitEntity.IsExclusiveForOwner;
 			aAppTransactionUnitDto.IsDisableAddButton = aAppTransactionUnitEntity.IsDisableAddButton;
 			aAppTransactionUnitDto.IsDisableDeleteButton = aAppTransactionUnitEntity.IsDisableDeleteButton;
 			aAppTransactionUnitDto.BaseDataBaseTableName = aAppTransactionUnitEntity.BaseDataBaseTableName;
 			aAppTransactionUnitDto.TransactionUnitIentityGuid = aAppTransactionUnitEntity.TransactionUnitIentityGuid;
 			aAppTransactionUnitDto.TreeViewKeyField = aAppTransactionUnitEntity.TreeViewKeyField;
 			aAppTransactionUnitDto.TreeViewParentKeyField = aAppTransactionUnitEntity.TreeViewParentKeyField;
 			aAppTransactionUnitDto.EmGridViewDisplayType = aAppTransactionUnitEntity.EmGridViewDisplayType;
 			aAppTransactionUnitDto.ImageHeight = aAppTransactionUnitEntity.ImageHeight;
 			aAppTransactionUnitDto.AvailableSourceUnitId = aAppTransactionUnitEntity.AvailableSourceUnitId;
 			aAppTransactionUnitDto.AvailableSourceFilterByParentTransactionFieldId = aAppTransactionUnitEntity.AvailableSourceFilterByParentTransactionFieldId;
 			aAppTransactionUnitDto.AvailableSourceFilterWhereClause = aAppTransactionUnitEntity.AvailableSourceFilterWhereClause;
 			aAppTransactionUnitDto.DataSourceQuery = aAppTransactionUnitEntity.DataSourceQuery;
 			aAppTransactionUnitDto.MinRowCount = aAppTransactionUnitEntity.MinRowCount;
 			aAppTransactionUnitDto.MaxRowCount = aAppTransactionUnitEntity.MaxRowCount;
 			aAppTransactionUnitDto.IsUsedForLoadingAvailableSource = aAppTransactionUnitEntity.IsUsedForLoadingAvailableSource;
 			aAppTransactionUnitDto.AvailableSourceMatchToParentUnitTransactionFieldId = aAppTransactionUnitEntity.AvailableSourceMatchToParentUnitTransactionFieldId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppTransactionUnitDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionUnitEntity.AppCreatedDate);
                aAppTransactionUnitDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionUnitEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppTransactionUnitEntity, aAppTransactionUnitDto);
		}
		
		 /// <summary>
        ///  Copy AppTransactionUnitDto Properties to   AppTransactionUnitEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppTransactionUnitEntity aAppTransactionUnitEntity,AppTransactionUnitDto aAppTransactionUnitDto)
        {        
 
      			aAppTransactionUnitEntity.TransactionId = aAppTransactionUnitDto.TransactionId;
      			aAppTransactionUnitEntity.UnitDisplayName = aAppTransactionUnitDto.UnitDisplayName;
      			aAppTransactionUnitEntity.DataBaseTableName = aAppTransactionUnitDto.DataBaseTableName;
      			aAppTransactionUnitEntity.SchemaOwner = aAppTransactionUnitDto.SchemaOwner;
      			aAppTransactionUnitEntity.TransactionFlow = aAppTransactionUnitDto.TransactionFlow;
      			aAppTransactionUnitEntity.ParentTransactionUnitId = aAppTransactionUnitDto.ParentTransactionUnitId;
      			aAppTransactionUnitEntity.IsReadOnly = aAppTransactionUnitDto.IsReadOnly;
      			aAppTransactionUnitEntity.IsMatrixUnit = aAppTransactionUnitDto.IsMatrixUnit;
      			aAppTransactionUnitEntity.IsMatrixPivotUnit = aAppTransactionUnitDto.IsMatrixPivotUnit;
      			aAppTransactionUnitEntity.IsSynchToDatabaseTable = aAppTransactionUnitDto.IsSynchToDatabaseTable;
      			aAppTransactionUnitEntity.IsMasterSiblingUnit = aAppTransactionUnitDto.IsMasterSiblingUnit;
      			aAppTransactionUnitEntity.IsPrimaryKeyIdentityInsert = aAppTransactionUnitDto.IsPrimaryKeyIdentityInsert;
 
  
   
    
      			aAppTransactionUnitEntity.AppCreatedByCompanyId = aAppTransactionUnitDto.AppCreatedByCompanyId;
      			aAppTransactionUnitEntity.IsExclusiveForOwner = aAppTransactionUnitDto.IsExclusiveForOwner;
      			aAppTransactionUnitEntity.IsDisableAddButton = aAppTransactionUnitDto.IsDisableAddButton;
      			aAppTransactionUnitEntity.IsDisableDeleteButton = aAppTransactionUnitDto.IsDisableDeleteButton;
      			aAppTransactionUnitEntity.BaseDataBaseTableName = aAppTransactionUnitDto.BaseDataBaseTableName;
      			aAppTransactionUnitEntity.TransactionUnitIentityGuid = aAppTransactionUnitDto.TransactionUnitIentityGuid;
      			aAppTransactionUnitEntity.TreeViewKeyField = aAppTransactionUnitDto.TreeViewKeyField;
      			aAppTransactionUnitEntity.TreeViewParentKeyField = aAppTransactionUnitDto.TreeViewParentKeyField;
      			aAppTransactionUnitEntity.EmGridViewDisplayType = aAppTransactionUnitDto.EmGridViewDisplayType;
      			aAppTransactionUnitEntity.ImageHeight = aAppTransactionUnitDto.ImageHeight;
      			aAppTransactionUnitEntity.AvailableSourceUnitId = aAppTransactionUnitDto.AvailableSourceUnitId;
      			aAppTransactionUnitEntity.AvailableSourceFilterByParentTransactionFieldId = aAppTransactionUnitDto.AvailableSourceFilterByParentTransactionFieldId;
      			aAppTransactionUnitEntity.AvailableSourceFilterWhereClause = aAppTransactionUnitDto.AvailableSourceFilterWhereClause;
      			aAppTransactionUnitEntity.DataSourceQuery = aAppTransactionUnitDto.DataSourceQuery;
      			aAppTransactionUnitEntity.MinRowCount = aAppTransactionUnitDto.MinRowCount;
      			aAppTransactionUnitEntity.MaxRowCount = aAppTransactionUnitDto.MaxRowCount;
      			aAppTransactionUnitEntity.IsUsedForLoadingAvailableSource = aAppTransactionUnitDto.IsUsedForLoadingAvailableSource;
      			aAppTransactionUnitEntity.AvailableSourceMatchToParentUnitTransactionFieldId = aAppTransactionUnitDto.AvailableSourceMatchToParentUnitTransactionFieldId;
			
			if(aAppTransactionUnitDto.Id == null)
			{
				aAppTransactionUnitEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionUnitEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppTransactionUnitEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionUnitEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionUnitEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppTransactionUnitEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionUnitEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppTransactionUnitEntity, aAppTransactionUnitDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppTransactionUnitEntity aAppTransactionUnitEntity,AppTransactionUnitDto aAppTransactionUnitDto);
		
		static partial void OnCopyDtoToEntityDone(AppTransactionUnitEntity aAppTransactionUnitEntity,AppTransactionUnitDto aAppTransactionUnitDto);
		
   
       
    }
}

 