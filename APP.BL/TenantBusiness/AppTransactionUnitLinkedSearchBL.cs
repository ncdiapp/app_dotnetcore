using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
//using APP.Persistence.Common;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;

using APP.Framework;
namespace App.BL
{
    public static class AppTransactionUnitLinkedSearchBL
    {

        public static ObservableSet<AppTransactionUnitLinkedSearchExDto> RetrieveOneAppTransactionUnitLinkedSearchList(object transactionUnitId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppTransactionUnitLinkedSearchEntity> entityList = new EntityCollection<AppTransactionUnitLinkedSearchEntity>();


                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppTransactionUnitLinkedSearchEntity);
                rootPath.Add(AppTransactionUnitLinkedSearchEntity.PrefetchPathAppTransactionUnitSearchFieldMapping)
                    .SubPath.Add(AppTransactionUnitSearchFieldMappingEntity.PrefetchPathAppTransactionField);

                rootPath.Add(AppTransactionUnitLinkedSearchEntity.PrefetchPathAppTransactionUnitSearchViewFieldMapping)
                    .SubPath.Add(AppTransactionUnitSearchViewFieldMappingEntity.PrefetchPathAppTransactionField);

                IRelationPredicateBucket filter = new RelationPredicateBucket(AppTransactionUnitLinkedSearchFields.TransactionUnitId == transactionUnitId);

                adapter.FetchEntityCollection(entityList, filter, rootPath);


                var aDtoList = new ObservableSet<AppTransactionUnitLinkedSearchExDto>();
                foreach (var appTransactionUnitLinkedSearchEntity in entityList)
                {
                    AppTransactionUnitLinkedSearchExDto aAppTransactionUnitLinkedSearchExDto = ConvertOneEntityToDto(appTransactionUnitLinkedSearchEntity);


                    aDtoList.Add(aAppTransactionUnitLinkedSearchExDto);

                }
                return aDtoList;
            }
        }

        public static AppTransactionUnitLinkedSearchExDto RetrieveOneAppTransactionUnitLinkedSearchExDto(object transactionUnitLinkedSearchId)
        {
            AppTransactionUnitLinkedSearchEntity aAppTransactionUnitLinkedSearchEntity = RetrieveOneAppTransactionUnitLinkedSearchEntity(transactionUnitLinkedSearchId);

            AppTransactionUnitLinkedSearchExDto aAppTransactionUnitLinkedSearchExDto = ConvertOneEntityToDto(aAppTransactionUnitLinkedSearchEntity);


            return aAppTransactionUnitLinkedSearchExDto;
        }

        public static AppTransactionUnitLinkedSearchEntity RetrieveOneAppTransactionUnitLinkedSearchEntity(object transactionUnitLinkedSearchId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppTransactionUnitLinkedSearchEntity aEntity = new AppTransactionUnitLinkedSearchEntity(int.Parse(transactionUnitLinkedSearchId.ToString()));

                //  IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppTransactionUnitLinkedSearchEntity);

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppTransactionUnitLinkedSearchEntity);
                rootPath.Add(AppTransactionUnitLinkedSearchEntity.PrefetchPathAppTransactionUnitSearchFieldMapping)
                    .SubPath.Add(AppTransactionUnitSearchFieldMappingEntity.PrefetchPathAppTransactionField);

                rootPath.Add(AppTransactionUnitLinkedSearchEntity.PrefetchPathAppTransactionUnitSearchViewFieldMapping)
                    .SubPath.Add(AppTransactionUnitSearchViewFieldMappingEntity.PrefetchPathAppTransactionField);



                adpater.FetchEntity(aEntity, rootPath);
                return aEntity;
            }
        }

        public static OperationCallResult<AppTransactionUnitLinkedSearchExDto> SaveAppTransactionUnitLinkedSearchExDto(AppTransactionUnitLinkedSearchExDto aAppTransactionUnitLinkedSearchExDto)
        {
            OperationCallResult<AppTransactionUnitLinkedSearchExDto> aOperationCallResult = new OperationCallResult<AppTransactionUnitLinkedSearchExDto>();
            
            var aValidationResult = aAppTransactionUnitLinkedSearchExDto.ValidateDto();
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }




            aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;



            // prepare Data
            if (aAppTransactionUnitLinkedSearchExDto.IsNew)
            {
                var aAppTransactionUnitLinkedSearchEntity = new AppTransactionUnitLinkedSearchEntity();
                AppTransactionUnitLinkedSearchConverter.CopyDtoToEntity(aAppTransactionUnitLinkedSearchEntity, aAppTransactionUnitLinkedSearchExDto);

                foreach (var searchdto in aAppTransactionUnitLinkedSearchExDto.AppTransactionUnitSearchFieldMappingList)
                {
                    AppTransactionUnitSearchFieldMappingEntity searchMppingEntity = new AppTransactionUnitSearchFieldMappingEntity();
                    AppTransactionUnitSearchFieldMappingConverter.CopyDtoToEntity(searchMppingEntity, searchdto);
                    aAppTransactionUnitLinkedSearchEntity.AppTransactionUnitSearchFieldMapping.Add(searchMppingEntity);
                }

                foreach (var searchViewdto in aAppTransactionUnitLinkedSearchExDto.AppTransactionUnitSearchViewFieldMappingList)
                {
                    AppTransactionUnitSearchViewFieldMappingEntity searchViewMppingEntity = new AppTransactionUnitSearchViewFieldMappingEntity();
                    AppTransactionUnitSearchViewFieldMappingConverter.CopyDtoToEntity(searchViewMppingEntity, searchViewdto);
                    aAppTransactionUnitLinkedSearchEntity.AppTransactionUnitSearchViewFieldMapping.Add(searchViewMppingEntity);
                }


                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppTransactionUnitLinkedSearchEntity);
                        adapter.Commit();

                        aAppTransactionUnitLinkedSearchExDto.Id = aAppTransactionUnitLinkedSearchEntity.TransactionUnitLinkedSearchId;
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionUnitLinkedSearchExDto), "app_SearchViewEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));


                        var unitEntity = AppTransactionBL.RetrieveOneAppTransactionUnitEntity(aAppTransactionUnitLinkedSearchExDto.TransactionUnitId);

                        if (unitEntity != null && unitEntity.TransactionId.HasValue)
                        {
                            var freshHierarchyAppTransactionExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(unitEntity.TransactionId);
                            AppTransactionBL.SynchronizeDatabaseTableAndUpdateCahce(freshHierarchyAppTransactionExDto);
                        }
                    }


                    // Database FK Exeption ........
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionUnitLinkedSearchExDto), "app_SearchViewEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }

            else if (aAppTransactionUnitLinkedSearchExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(ProcessDirtyAppTransactionUnitLinkedSearchExDto(aAppTransactionUnitLinkedSearchExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                var unitEntity = AppTransactionBL.RetrieveOneAppTransactionUnitEntity(aAppTransactionUnitLinkedSearchExDto.TransactionUnitId);
                if (unitEntity != null && unitEntity.TransactionId.HasValue)
                {
                    var freshHierarchyAppTransactionExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(unitEntity.TransactionId);
                    AppTransactionBL.SynchronizeDatabaseTableAndUpdateCahce(freshHierarchyAppTransactionExDto);
                }


                aOperationCallResult.Object = RetrieveOneAppTransactionUnitLinkedSearchExDto(aAppTransactionUnitLinkedSearchExDto.Id);
            }

            return aOperationCallResult;
        }

        //Delete a AppTransactionUnitLinkedSearch
        public static OperationCallResult<object> DeleteAppTransactionUnitLinkedSearch(object transactionUnitLinkedSearchId)
        {
            OperationCallResult<object> aValidationResult = new OperationCallResult<object>();

            string referMsg = string.Empty;


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.DeleteEntitiesDirectly(typeof(AppTransactionUnitSearchFieldMappingEntity), new RelationPredicateBucket(AppTransactionUnitSearchFieldMappingFields.TransactionUnitLinkedSearchId == transactionUnitLinkedSearchId));
                    adapter.DeleteEntitiesDirectly(typeof(AppTransactionUnitSearchViewFieldMappingEntity), new RelationPredicateBucket(AppTransactionUnitSearchViewFieldMappingFields.TransactionUnitLinkedSearchId == transactionUnitLinkedSearchId));

                    adapter.DeleteEntitiesDirectly(typeof(AppTransactionUnitLinkedSearchEntity), new RelationPredicateBucket(AppTransactionUnitLinkedSearchFields.TransactionUnitLinkedSearchId == transactionUnitLinkedSearchId));
                    adapter.Commit();
                }


                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionUnitLinkedSearchEntity), "app_SearchViewEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }

                // if no any errors
                if (!aValidationResult.ValidationResult.HasErrors)
                {
                    aValidationResult.Object = transactionUnitLinkedSearchId;
                }
            }
            return aValidationResult;
        }



        internal static AppTransactionUnitLinkedSearchExDto ConvertOneEntityToDto(AppTransactionUnitLinkedSearchEntity appTransactionUnitLinkedSearchEntity)
        {
            AppTransactionUnitLinkedSearchExDto aAppTransactionUnitLinkedSearchExDto = AppTransactionUnitLinkedSearchConverter.ConvertEntityToExDto(appTransactionUnitLinkedSearchEntity);

            if (appTransactionUnitLinkedSearchEntity.UsageType == 3)
            {
                var targetTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appTransactionUnitLinkedSearchEntity.SearchViewId);

                if (targetTransactionExDto.DictAllTransactionField != null)
                {
                    foreach (var searchMppingEntity in appTransactionUnitLinkedSearchEntity.AppTransactionUnitSearchFieldMapping)
                    {
                        var searchFiledMappingDto = AppTransactionUnitSearchFieldMappingConverter.ConvertEntityToExDto(searchMppingEntity);
                        searchFiledMappingDto.DataBaseFieldName = searchMppingEntity.AppTransactionField.DataBaseFieldName;

                        if (targetTransactionExDto.DictAllTransactionField.ContainsKey(searchFiledMappingDto.SearchFieldId))
                        {
                            var transFieldExDto = targetTransactionExDto.DictAllTransactionField[searchFiledMappingDto.SearchFieldId];
                            searchFiledMappingDto.TargetTransFieldDataBaseFieldName = transFieldExDto.DataBaseFieldName;
                        }

                        aAppTransactionUnitLinkedSearchExDto.AppTransactionUnitSearchFieldMappingList.Add(searchFiledMappingDto);
                    }

                    foreach (var viewhMppingEntity in appTransactionUnitLinkedSearchEntity.AppTransactionUnitSearchViewFieldMapping)
                    {
                        var viewMappingDto = AppTransactionUnitSearchViewFieldMappingConverter.ConvertEntityToExDto(viewhMppingEntity);

                        viewMappingDto.DataBaseFieldName = viewhMppingEntity.AppTransactionField.DataBaseFieldName;

                        if (targetTransactionExDto.DictAllTransactionField.ContainsKey(viewMappingDto.SearchViewFieldId.Value))
                        {
                            var transFieldExDto = targetTransactionExDto.DictAllTransactionField[viewMappingDto.SearchViewFieldId.Value];
                            viewMappingDto.TargetTransFieldDataBaseFieldName = transFieldExDto.DataBaseFieldName;
                        }

                        aAppTransactionUnitLinkedSearchExDto.AppTransactionUnitSearchViewFieldMappingList.Add(viewMappingDto);
                    }
                }
            }
            else
            {
                foreach (var searchMppingEntity in appTransactionUnitLinkedSearchEntity.AppTransactionUnitSearchFieldMapping)
                {
                    var searchFiledMappingDto = AppTransactionUnitSearchFieldMappingConverter.ConvertEntityToExDto(searchMppingEntity);
                    searchFiledMappingDto.DataBaseFieldName = searchMppingEntity.AppTransactionField.DataBaseFieldName;
                    searchFiledMappingDto.SourceTransactionUnitId = searchMppingEntity.AppTransactionField.TransactionUnitId.ToString();
                    
                    aAppTransactionUnitLinkedSearchExDto.AppTransactionUnitSearchFieldMappingList.Add(searchFiledMappingDto);
                }

                foreach (var viewhMppingEntity in appTransactionUnitLinkedSearchEntity.AppTransactionUnitSearchViewFieldMapping)
                {
                    var viewMappingDto = AppTransactionUnitSearchViewFieldMappingConverter.ConvertEntityToExDto(viewhMppingEntity);

                    viewMappingDto.DataBaseFieldName = viewhMppingEntity.AppTransactionField.DataBaseFieldName;

                    aAppTransactionUnitLinkedSearchExDto.AppTransactionUnitSearchViewFieldMappingList.Add(viewMappingDto);
                }
            }

            return aAppTransactionUnitLinkedSearchExDto;
        }



        private static ValidationResult ProcessDirtyAppTransactionUnitLinkedSearchExDto(AppTransactionUnitLinkedSearchExDto aAppTransactionUnitLinkedSearchExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            // int[] deleteAppTransactionUnitLinkedSearchFieldIDs = aAppTransactionUnitLinkedSearchExDto.AppTransactionUnitLinkedSearchFieldList.FindModifiedItems().Select(o => o.Id).Cast<int>().ToArray();

            AppTransactionUnitLinkedSearchEntity aAppTransactionUnitLinkedSearchEntity = RetrieveOneAppTransactionUnitLinkedSearchEntity(aAppTransactionUnitLinkedSearchExDto.Id);
            AppTransactionUnitLinkedSearchConverter.CopyDtoToEntity(aAppTransactionUnitLinkedSearchEntity, aAppTransactionUnitLinkedSearchExDto);

            aAppTransactionUnitLinkedSearchEntity.AppTransactionUnitSearchFieldMapping.Clear();
            aAppTransactionUnitLinkedSearchEntity.AppTransactionUnitSearchViewFieldMapping.Clear();

            foreach (var searchdto in aAppTransactionUnitLinkedSearchExDto.AppTransactionUnitSearchFieldMappingList)
            {
                AppTransactionUnitSearchFieldMappingEntity searchMppingEntity = new AppTransactionUnitSearchFieldMappingEntity();
                AppTransactionUnitSearchFieldMappingConverter.CopyDtoToEntity(searchMppingEntity, searchdto);
                aAppTransactionUnitLinkedSearchEntity.AppTransactionUnitSearchFieldMapping.Add(searchMppingEntity);
            }

            foreach (var searchViewdto in aAppTransactionUnitLinkedSearchExDto.AppTransactionUnitSearchViewFieldMappingList)
            {
                AppTransactionUnitSearchViewFieldMappingEntity searchViewMppingEntity = new AppTransactionUnitSearchViewFieldMappingEntity();
                AppTransactionUnitSearchViewFieldMappingConverter.CopyDtoToEntity(searchViewMppingEntity, searchViewdto);
                aAppTransactionUnitLinkedSearchEntity.AppTransactionUnitSearchViewFieldMapping.Add(searchViewMppingEntity);
            }

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");

                    adapter.DeleteEntitiesDirectly(typeof(AppTransactionUnitSearchFieldMappingEntity), new RelationPredicateBucket(AppTransactionUnitSearchFieldMappingFields.TransactionUnitLinkedSearchId == aAppTransactionUnitLinkedSearchExDto.Id));
                    adapter.DeleteEntitiesDirectly(typeof(AppTransactionUnitSearchViewFieldMappingEntity), new RelationPredicateBucket(AppTransactionUnitSearchViewFieldMappingFields.TransactionUnitLinkedSearchId == aAppTransactionUnitLinkedSearchExDto.Id));


                    adapter.SaveEntity(aAppTransactionUnitLinkedSearchEntity);



                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionUnitLinkedSearchEntity), "app_SearchViewEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionUnitLinkedSearchEntity), "app_SearchViewEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }













    }
}