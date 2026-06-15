using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;

using APP.Framework;
namespace App.BL
{
    public static class LinkTragetBL
    {



        // Link Target
        public static ObservableSet<AppFormLinkTargetDto> RetrieveOneAppFormLinkTargetList(int targetSourceType, object targetSourceId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppFormLinkTargetEntity> entityList = new EntityCollection<AppFormLinkTargetEntity>();

                if (targetSourceType == (int)EmAppLinkTargetSourceType.SearchView)
                {
                    IRelationPredicateBucket filter = new RelationPredicateBucket(AppFormLinkTargetFields.SearchViewId == targetSourceId);
                    adapter.FetchEntityCollection(entityList, filter);
                }
                //else if (targetSourceType == (int)EmAppLinkTargetSourceType.Form)
                //{
                //    IRelationPredicateBucket filter = new RelationPredicateBucket(AppFormLinkTargetFields.SourceFormId == targetSourceId);
                //    adapter.FetchEntityCollection(entityList, filter);
                //}
                else if (targetSourceType == (int)EmAppLinkTargetSourceType.TransactionUnit)
                {
                    IRelationPredicateBucket filter = new RelationPredicateBucket(AppFormLinkTargetFields.TransactionUnitId == targetSourceId);
                    adapter.FetchEntityCollection(entityList, filter);
                }

                var aDtoList = new ObservableSet<AppFormLinkTargetDto>();
                foreach (var appFormLinkTargetEntity in entityList)
                {
                    aDtoList.Add(AppFormLinkTargetConverter.ConvertEntityToDto(appFormLinkTargetEntity));
                }
                return aDtoList;
            }
        }


        public static List<AppFormLinkTargetDto> RetrieveOneSearchViewLinkTargetList(object searchViewId, int? usageType)
        {
            List<AppFormLinkTargetDto> aDtoList = RetrieveOneAppFormLinkTargetList((int)EmAppLinkTargetSourceType.SearchView, searchViewId).ToList();

            if (usageType.HasValue)
            {
                aDtoList = aDtoList.Where(o => o.LinkTargetUsageType.HasValue && o.LinkTargetUsageType.Value == usageType.Value).ToList();
            }

            return aDtoList;
        }

        public static AppFormLinkTargetEntity RetrieveOneAppFormLinkTargetEntity(object id)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppFormLinkTargetEntity aAppFormLinkTargetEntity = new AppFormLinkTargetEntity(int.Parse(id.ToString()));
                adpater.FetchEntity(aAppFormLinkTargetEntity);
                return aAppFormLinkTargetEntity;
            }
        }

        public static AppFormLinkTargetDto RetrieveOneAppFormLinkTargetDto(object id)
        {
            if (id != null)
            {
                AppFormLinkTargetEntity aAppFormLinkTargetEntity = RetrieveOneAppFormLinkTargetEntity(id);

                if (aAppFormLinkTargetEntity != null)
                {
                    return AppFormLinkTargetConverter.ConvertEntityToDto(aAppFormLinkTargetEntity);
                }
            }

            return null;
        }



        public static OperationCallResult<AppFormLinkTargetDto> SaveOneAppFormLinkTargetList(int targetSourceType, object targetSourceId, ObservableSet<AppFormLinkTargetDto> aAppFormLinkTargetDtoSet)
        {
            OperationCallResult<AppFormLinkTargetDto> aOperationCallResult = new OperationCallResult<AppFormLinkTargetDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            foreach (AppFormLinkTargetDto aAppFormLinkTargetDto in aAppFormLinkTargetDtoSet)
            {
                if (!(aAppFormLinkTargetDto.ActionType.HasValue && aAppFormLinkTargetDto.ActionType.Value == (int)EmAppLinkTargetActionType.ExecuteTransactionCommand))
                {
                    if (aAppFormLinkTargetDto.OtherSettingsDto != null)
                    {
                        aAppFormLinkTargetDto.OtherSettingsDto.LinkTargetApplyToRowRangeType = null;
                    }
                }

                aValidationResult.Merge(aAppFormLinkTargetDto.ValidateDto());
            }

            if (aValidationResult.HasErrors)
            {
                return aOperationCallResult;
            }


            // New
            aAppFormLinkTargetDtoSet.FindNewItems().ForAll(o =>
            {
                if (targetSourceType == (int)EmAppLinkTargetSourceType.SearchView)
                {
                    o.SearchViewId = int.Parse(targetSourceId.ToString());
                }
                //else if (targetSourceType == (int)EmAppLinkTargetSourceType.Form)
                //{
                //    o.SourceFormId = int.Parse(targetSourceId.ToString());
                //}
                else if (targetSourceType == (int)EmAppLinkTargetSourceType.TransactionUnit)
                {
                    o.TransactionUnitId = int.Parse(targetSourceId.ToString());
                }


                aValidationResult.Merge(SaveOneAppFormLinkTargetList_ProcessNewDto(o));
            }
            );

            // Modified
            aAppFormLinkTargetDtoSet.FindModifiedItems().ForAll(o => aValidationResult.Merge(SaveOneAppFormLinkTargetList_ProcessDirtyDto(o)));

            // Deleted
            int[] needToDeletePdmSecurityGroupUserRightId = aAppFormLinkTargetDtoSet.FindDeletedItemIds().Cast<int>().ToArray();
            aValidationResult.Merge(SaveOneAppFormLinkTargetList_ProcessDeleteDto(needToDeletePdmSecurityGroupUserRightId));

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {

                if (targetSourceType == (int)EmAppLinkTargetSourceType.TransactionUnit)
                {
                    int transactionUnitId = int.Parse(targetSourceId.ToString());

                    var unitEntity = AppTransactionBL.RetrieveOneAppTransactionUnitEntity(transactionUnitId);
                    if (unitEntity != null && unitEntity.TransactionId.HasValue)
                    {
                        var freshHierarchyAppTransactionExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(unitEntity.TransactionId);
                        AppTransactionBL.SynchronizeDatabaseTableAndUpdateCahce(freshHierarchyAppTransactionExDto);
                    }
                }

                aOperationCallResult.ObjectList = RetrieveOneAppFormLinkTargetList(targetSourceType, targetSourceId);
                if (aValidationResult.Items.Count > 1)
                {
                    var firstItem = aValidationResult.Items.FirstOrDefault();
                    aValidationResult.Items.Clear();
                    aValidationResult.Items.Add(firstItem);
                }
            }

            return aOperationCallResult;
        }

        private static ValidationResult SaveOneAppFormLinkTargetList_ProcessNewDto(AppFormLinkTargetDto aAppFormLinkTargetDto)
        {
            SetIsLinkToComsumeApiTransaction(aAppFormLinkTargetDto);
            ValidationResult aValidationResult = new ValidationResult();

            AppFormLinkTargetEntity aAppFormLinkTargetEntity = new AppFormLinkTargetEntity();
            AppFormLinkTargetConverter.CopyDtoToEntity(aAppFormLinkTargetEntity, aAppFormLinkTargetDto);
            

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppFormLinkTargetEntity, false, true);

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppFormLinkTargetEntity), "App_FormLinkTargetEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }


                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppFormLinkTargetEntity), "App_FormLinkTargetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static ValidationResult SaveOneAppFormLinkTargetList_ProcessDirtyDto(AppFormLinkTargetDto aAppFormLinkTargetDto)
        {
            SetIsLinkToComsumeApiTransaction(aAppFormLinkTargetDto);
            ValidationResult aValidationResult = new ValidationResult();

            AppFormLinkTargetEntity aAppFormLinkTargetEntity = RetrieveOneAppFormLinkTargetEntity(aAppFormLinkTargetDto.Id);

            AppFormLinkTargetConverter.CopyDtoToEntity(aAppFormLinkTargetEntity, aAppFormLinkTargetDto);
            

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppFormLinkTargetEntity, false, true);
                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppFormLinkTargetEntity), "App_FormLinkTargetEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }



                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppFormLinkTargetEntity), "App_FormLinkTargetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static void SetIsLinkToComsumeApiTransaction(AppFormLinkTargetDto aAppFormLinkTargetDto)
        {
            if (aAppFormLinkTargetDto.OtherSettingsDto == null)
            {
                aAppFormLinkTargetDto.OtherSettingsDto = new AppFormLinkTargetOtherSettingsDto();
            }

            aAppFormLinkTargetDto.OtherSettingsDto.IsLinkToComsumeApiTransaction = false;

            if (aAppFormLinkTargetDto.LinkTargetTransactionId.HasValue)
            {
                AppTransactionExDto tgtTransactionDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aAppFormLinkTargetDto.LinkTargetTransactionId.Value);
                if (tgtTransactionDto != null && tgtTransactionDto.OtherOptions != null)
                {
                    if (tgtTransactionDto.OtherOptions.IsApiIntegrationTransaction && tgtTransactionDto.FolderUsageType.HasValue)
                    {
                        aAppFormLinkTargetDto.OtherSettingsDto.IsLinkToComsumeApiTransaction = true;
                    }
                }
            }
        }

        private static ValidationResult SaveOneAppFormLinkTargetList_ProcessDeleteDto(int[] appFormLinkTargetId)
        {
            ValidationResult aValidationResult = new ValidationResult();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppFormLinkTargetEntity");
                    adapter.DeleteEntitiesDirectly(typeof(AppFormLinkTargetEntity), new RelationPredicateBucket(AppFormLinkTargetFields.LinkTargetId == appFormLinkTargetId));
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppFormLinkTargetEntity), "App_FormLinkTargetEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    adapter.Commit();
                }



                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppFormLinkTargetEntity), "App_FormLinkTargetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));

                    adapter.Rollback();
                }
            }

            return aValidationResult;
        }

        //public static Dictionary<int, string> GetTransactionDataSourceList(int transactionId)
        //{
        //    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
        //    {

        //        string queryDatasourceTransId = @"SELECT    distinct   
        //                             AppTransaction.TransactionID AS DataSourceTransactionID,AppTransaction.TransactionName
        //    FROM           [AppTransactionUnit] INNER JOIN
        //                             AppTransactionField ON AppTransactionUnit.TransactionUnitID = AppTransactionField.TransactionUnitID INNER JOIN
        //                             AppEntityInfo ON AppTransactionField.EntityID = AppEntityInfo.EntityInfoID INNER JOIN
        //                             AppTransactionUnit AS AppTransactionUnit_1 ON AppEntityInfo.TableName = AppTransactionUnit_1.DataBaseTableName INNER JOIN
        //                             AppTransaction ON AppTransactionUnit_1.TransactionID = AppTransaction.TransactionID
        //    WHERE        (AppTransactionField.ControlType = 1) AND (AppTransaction.TransactionOrganizedType = 3)
        //    and  AppTransactionUnit.TransactionID =@TransactionID";
        //        List<SqlParameter> listPars = new List<SqlParameter>();
        //        listPars.Add(new SqlParameter("@TransactionID", transactionId));
        //        return adapter.ExecuteDataTableRetrievalQuery(queryDatasourceTransId, listPars).AsEnumerable().ToDictionary(o => (int)o["DataSourceTransactionID"], o => o["TransactionName"] as string);

        //    }

        //}


        public static List<EntityMappingToListEditTransactionDto> GetTransactionDataSourceList_Old(int transactionId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                List<EntityMappingToListEditTransactionDto> toReturn = new List<EntityMappingToListEditTransactionDto>();

                string queryDatasourceTransId = @"SELECT    distinct   
                                      AppEntityInfo.EntityInfoID, AppEntityInfo.EntityCode, AppTransaction.TransactionID AS DataSourceTransactionID,AppTransaction.TransactionName
            FROM           [AppTransactionUnit] INNER JOIN
                                     AppTransactionField ON AppTransactionUnit.TransactionUnitID = AppTransactionField.TransactionUnitID INNER JOIN
                                     AppEntityInfo ON AppTransactionField.EntityID = AppEntityInfo.EntityInfoID INNER JOIN
                                     AppTransactionUnit AS AppTransactionUnit_1 ON AppEntityInfo.TableName = AppTransactionUnit_1.DataBaseTableName INNER JOIN
                                     AppTransaction ON AppTransactionUnit_1.TransactionID = AppTransaction.TransactionID
            WHERE        (AppTransactionField.ControlType = 1 or AppTransactionField.ControlType = 38 or AppTransactionField.ControlType = 48) AND (AppTransaction.TransactionOrganizedType = 3)
            and  AppTransactionUnit.TransactionID =@TransactionID";
                List<SqlParameter> listPars = new List<SqlParameter>();
                listPars.Add(new SqlParameter("@TransactionID", transactionId));

                var tableData = adapter.ExecuteDataTableRetrievalQuery(queryDatasourceTransId, listPars).AsEnumerable();

                foreach (var rowData in tableData)
                {
                    EntityMappingToListEditTransactionDto aMappingDto = new EntityMappingToListEditTransactionDto();
                    aMappingDto.EntityId = ControlTypeValueConverter.ConvertValueToInt(rowData["EntityInfoID"]);
                    aMappingDto.EntityCode = rowData["EntityCode"] as string;
                    aMappingDto.MappingToListEditTransactionId = ControlTypeValueConverter.ConvertValueToInt(rowData["DataSourceTransactionID"]);
                    aMappingDto.MappingToListEditTransactionName = rowData["TransactionName"] as string;

                    toReturn.Add(aMappingDto);
                }


                return toReturn;

            }

        }

        public static List<EntityMappingToListEditTransactionDto> GetTransactionDataSourceList(int transactionId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                List<EntityMappingToListEditTransactionDto> toReturn = new List<EntityMappingToListEditTransactionDto>();

                string queryDatasourceTransId = @"SELECT    distinct   
                                      AppEntityInfo.EntityInfoID, AppEntityInfo.EntityCode, AppTransaction.TransactionID AS DataSourceTransactionID,AppTransaction.TransactionName, AppEntityInfo.EntityType
            FROM           [AppTransactionUnit] INNER JOIN
                                     AppTransactionField ON AppTransactionUnit.TransactionUnitID = AppTransactionField.TransactionUnitID INNER JOIN
                                     AppEntityInfo ON AppTransactionField.EntityID = AppEntityInfo.EntityInfoID INNER JOIN
                                     AppTransactionUnit AS AppTransactionUnit_1 ON AppEntityInfo.TableName = AppTransactionUnit_1.DataBaseTableName INNER JOIN
                                     AppTransaction ON AppTransactionUnit_1.TransactionID = AppTransaction.TransactionID
            WHERE        (AppTransactionField.ControlType = 1 or AppTransactionField.ControlType = 38 or AppTransactionField.ControlType = 48) AND (AppTransaction.TransactionOrganizedType = 3)
            and  AppTransactionUnit.TransactionID =@TransactionID";
                List<SqlParameter> listPars = new List<SqlParameter>();
                listPars.Add(new SqlParameter("@TransactionID", transactionId));

                var tableData = adapter.ExecuteDataTableRetrievalQuery(queryDatasourceTransId, listPars).AsEnumerable();

                foreach (var rowData in tableData)
                {
                    EntityMappingToListEditTransactionDto aMappingDto = new EntityMappingToListEditTransactionDto();
                    aMappingDto.EntityId = ControlTypeValueConverter.ConvertValueToInt(rowData["EntityInfoID"]);

                    if (aMappingDto.EntityId.HasValue 
                        && toReturn.FirstOrDefault(o => o.EntityId.HasValue && aMappingDto.EntityId.HasValue && o.EntityId.Value == aMappingDto.EntityId.Value) == null)
                    {
                        aMappingDto.EntityCode = rowData["EntityCode"] as string;
                        aMappingDto.MappingToListEditTransactionId = ControlTypeValueConverter.ConvertValueToInt(rowData["DataSourceTransactionID"]);
                        aMappingDto.MappingToListEditTransactionName = rowData["TransactionName"] as string;
                        aMappingDto.EntityType = ControlTypeValueConverter.ConvertValueToInt(rowData["EntityType"]);

                        toReturn.Add(aMappingDto);
                    }                
                   
                }


                return toReturn;

            }

        }


        public static List<int> GetTransactionDataSourceIdList(int transactionId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                List<int> toReturn = new List<int>();

                string queryDatasourceTransId = @"SELECT    distinct   
                                      AppEntityInfo.EntityInfoID
            FROM           [AppTransactionUnit] INNER JOIN
                                     AppTransactionField ON AppTransactionUnit.TransactionUnitID = AppTransactionField.TransactionUnitID INNER JOIN
                                     AppEntityInfo ON AppTransactionField.EntityID = AppEntityInfo.EntityInfoID INNER JOIN
                                     AppTransactionUnit AS AppTransactionUnit_1 ON AppEntityInfo.TableName = AppTransactionUnit_1.DataBaseTableName INNER JOIN
                                     AppTransaction ON AppTransactionUnit_1.TransactionID = AppTransaction.TransactionID
            WHERE        (AppTransactionField.ControlType = 1 or AppTransactionField.ControlType = 38 or AppTransactionField.ControlType = 48) AND (AppTransaction.TransactionOrganizedType = 3)
            and  AppTransactionUnit.TransactionID =@TransactionID";
                List<SqlParameter> listPars = new List<SqlParameter>();
                listPars.Add(new SqlParameter("@TransactionID", transactionId));

                var tableData = adapter.ExecuteDataTableRetrievalQuery(queryDatasourceTransId, listPars).AsEnumerable();

                foreach (var rowData in tableData)
                {

                    int? entityId = ControlTypeValueConverter.ConvertValueToInt(rowData["EntityInfoID"]);

                    if (entityId.HasValue)
                    {
                        toReturn.Add(entityId.Value);
                    }


                }


                return toReturn;

            }

        }
    }
}