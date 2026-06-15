using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
//using APP.Persistence.Common;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using System.Text.RegularExpressions;
using System.Data.Common;
using DatabaseSchemaMrg.DataSchema;
using GemBox.Document.Tables;


using APP.Framework;
namespace App.BL
{
    public static class AppDatabaseErDiagramBL
    {
        public static List<AppDataSetExDto> RetrieveSaasApplicationErDiagramList(int? applicationId)
        {
            List<AppDataSetExDto> toReturn = new List<AppDataSetExDto>();

            if (applicationId.HasValue)
            {
                List<AppDataSetExDto> allDataSetList = RetrieveAllErDiagramDto().ToList();
                toReturn = allDataSetList.Where(o => (o.SaasApplicationId.HasValue && o.SaasApplicationId.Value == applicationId.Value)).ToList();
            }

            return toReturn;
        }

        public static ObservableSet<AppDataSetExDto> RetrieveAllErDiagramDto()
        {
            ObservableSet<AppDataSetExDto> aSet = new ObservableSet<AppDataSetExDto>();
            EntityCollection<AppDataSetEntity> list = RetrieveAllErDiagramEntity();
            foreach (var o in list)
            {
                AppDataSetExDto aDto = AppDataSetConverter.ConvertEntityToExDto(o);
                aSet.Add(aDto);
            }

            return aSet;
        }

        public static AppDataSetExDto RetrieveOneErDiagramExDto(object Id)
        {
            AppDataSetEntity aAppDataSetEntity = RetrieveOneErDiagramEntity(Id);
            AppDataSetExDto aAppDataSetExDto = AppDataSetConverter.ConvertEntityToExDto(aAppDataSetEntity);
            aAppDataSetExDto.OtherSettings = "";

            if (aAppDataSetExDto.OtherSettingsDto != null && aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo != null)
            {
                DatabaseViewDto viewDto = aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo;
                viewDto.DataSourceRegisterId = aAppDataSetExDto.DataSourceFrom;

                viewDto.DictAllColumns = new Dictionary<string, Dictionary<string, bool>>();

                PrepareFkReferenceTableNameList(aAppDataSetExDto.DataSourceFrom.Value, viewDto);

                if (viewDto.DictTables != null && viewDto.DictTables.Values.Count > 0)
                {
                    List<KeyValuePair<string, string>> ownertableNameList = new List<KeyValuePair<string, string>>();

                    foreach (var tableDto in viewDto.DictTables.Values)
                    {
                        if (tableDto != null && !string.IsNullOrWhiteSpace(tableDto.TableName))
                        {
                            KeyValuePair<string, string> pair = new KeyValuePair<string, string>(tableDto.SchemaOwner, tableDto.TableName);
                            ownertableNameList.Add(pair);
                        }
                    }


                    Dictionary<string, DatabaseTable> dictDBTables = AppMetaDataBL.GetDatabaseTableSchemaDictionaryBySchemaOwnerTableNames(ownertableNameList, aAppDataSetExDto.DataSourceFrom);

                    foreach (KeyValuePair<string, string> ownerTablePair in ownertableNameList)
                    {
                        string ownerTableKey = AppMetaDataBL.GetOwnerTableKey(ownerTablePair.Key, ownerTablePair.Value);

                        if (dictDBTables.ContainsKey(ownerTableKey))
                        {
                            var dbTable = dictDBTables[ownerTableKey];
                            string tableName = dbTable.Name;

                            string uniqTableOrAliasName = dbTable.Name;

                            string keyFound = viewDto.DictTables.Keys.FirstOrDefault(o => o.ToLower() == uniqTableOrAliasName.ToLower());

                            if (!string.IsNullOrWhiteSpace(keyFound))
                            {
                                var viewTable = viewDto.DictTables[keyFound];
                                if (viewTable != null && !string.IsNullOrWhiteSpace(viewTable.UniqTableOrAliasName))
                                {

                                    viewTable.PkNames = new List<string>();
                                    if (dbTable.PrimaryKeyColumnList != null)
                                    {
                                        viewTable.PkNames = dbTable.PrimaryKeyColumnList.Select(o => o.Name).ToList();
                                    }

                                    Dictionary<string, bool> dictTableColumn = new Dictionary<string, bool>();

                                    if (viewDto.IsErDiagram)
                                    {
                                        dictTableColumn = dbTable.Columns.ToDictionary(
                                            o => o.Name,
                                            o => viewTable.PkNames.Contains(o.Name) ? true : false);
                                    }

                                    if (!viewDto.DictAllColumns.ContainsKey(viewTable.UniqTableOrAliasName.ToLower()))
                                    {

                                        viewDto.DictAllColumns.Add(viewTable.UniqTableOrAliasName.ToLower(), dictTableColumn);
                                    }
                                }
                            }
                        }
                    }

                    AppDatabaseViewBL.RebuildErDiagramFkLinks(viewDto);



                }




            }





            return aAppDataSetExDto;
        }

        internal static void PrepareFkReferenceTableNameList(int dataSourceRegId, DatabaseViewDto viewDto)
        {
            if (viewDto != null && viewDto.DictTables != null)
            {
                List<string> allTablesList = viewDto.DictTables.Values.Select(o => o.TableName).ToList();

                var dbfixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegId);


                var dictTableNameRefParentFkList = dbfixture.GetMutipleTableReferenceParentTables(allTablesList);

                var dictTableNameRefedChildList = dbfixture.GetMutipleTableReferencedChildTables(allTablesList);


                foreach (DatabaseViewTableDto databaseViewTableDto in viewDto.DictTables.Values)
                {
                    if (!string.IsNullOrWhiteSpace(databaseViewTableDto.TableName))
                    {
                        if (dictTableNameRefParentFkList.ContainsKey(databaseViewTableDto.TableName))
                        {
                            databaseViewTableDto.FKRefTables = dictTableNameRefParentFkList[databaseViewTableDto.TableName].Distinct().ToList();
                        }
                        if (dictTableNameRefedChildList.ContainsKey(databaseViewTableDto.TableName))
                        {
                            databaseViewTableDto.FKRefedTables = dictTableNameRefedChildList[databaseViewTableDto.TableName].Distinct().ToList();
                        }
                    }
                }
            }


        }

        public static OperationCallResult<AppDataSetExDto> SaveOneErDiagramExDto(AppDataSetExDto aAppDataSetExDto)
        {
            OperationCallResult<AppDataSetExDto> aOperationCallResult = new OperationCallResult<AppDataSetExDto>();

            Dictionary<string, int> dictDbObjNameAndUsageType = new Dictionary<string, int>();

            if (aAppDataSetExDto.OtherSettingsDto != null && aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo != null)
            {
                var diagramDto = aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo;

                if (diagramDto.DictTables != null)
                {
                    //var orgDictTables = diagramDto.DictTables;

                    List<string> needToRemoveKeys = diagramDto.DictTables.Where(o => o.Value == null).Select(o => o.Key).ToList();

                    foreach (string key in needToRemoveKeys)
                    {
                        diagramDto.DictTables.Remove(key);
                    }

                    dictDbObjNameAndUsageType = diagramDto.DictTables.Keys.Select(o => o.Trim().ToLower()).Distinct().ToDictionary(o => o, o => (int)EmAppDataSetUsageType.DatabaseTable);
                }

            }



            var aValidationResult = aAppDataSetExDto.ValidateDto();
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }


            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            if (aAppDataSetExDto.IsNew)
            {
                validationResult.Merge(ProcessNewDto(aAppDataSetExDto));
            }
            else if (aAppDataSetExDto.IsModified)
            {
                validationResult.Merge(ProcessDirtyDto(aAppDataSetExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                // var entity = AppDataSetBL.RetrieveOneAppDataSetEntity(aAppDataSetExDto.Id);

                var savedExDto = RetrieveOneErDiagramExDto(aAppDataSetExDto.Id);
                aOperationCallResult.Object = savedExDto;

                if (dictDbObjNameAndUsageType.Count > 0 && aAppDataSetExDto.SaasApplicationId.HasValue)
                {
                    AppDataSetBL.AddDatabaseObjectsToApplication(dictDbObjNameAndUsageType, aAppDataSetExDto.SaasApplicationId.Value);
                }
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<object> DeleteOneErDiagram(object Id)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();

            int? needToUpdateTransactionId = null;

            AppDataSetExDto dataSetExDto = AppDatabaseErDiagramBL.RetrieveOneErDiagramExDto(Id);

            if (dataSetExDto.OtherSettingsDto != null && dataSetExDto.OtherSettingsDto.TableImportSettingDto != null)
            {
                var importSettingDto = dataSetExDto.OtherSettingsDto.TableImportSettingDto;
                needToUpdateTransactionId = importSettingDto.NeedToUpdateTransactionId;
            }


            ValidationResult avalidationResult = DeleteOneErDiagramEntity(Id);
            aOperationCallResult.ValidationResult = avalidationResult;
            if (!aOperationCallResult.ValidationResult.HasErrors)
            {
                aOperationCallResult.Object = Id;

                if (needToUpdateTransactionId.HasValue)
                {
                    var transactionExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(needToUpdateTransactionId.Value);

                    if (transactionExDto.OtherOptions != null && transactionExDto.OtherOptions.TransactionDataUpdateImportSettingId.HasValue)
                    {
                        transactionExDto.OtherOptions.TransactionDataUpdateImportSettingId = null;

                        var saveTransactionResult = AppTransactionBL.SaveAppTransactionExDto(transactionExDto);
                    }
                }
            }

            return aOperationCallResult;
        }

        private static AppDataSetEntity RetrieveOneErDiagramEntity(object Id)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppDataSetEntity aAppDataSetEntity = new AppDataSetEntity(int.Parse(Id.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppDataSetEntity);


                adapter.FetchEntity(aAppDataSetEntity, rootPath);
                return aAppDataSetEntity;
            }
        }

        private static EntityCollection<AppDataSetEntity> RetrieveAllErDiagramEntity()
        {

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppDataSetEntity> list = new EntityCollection<AppDataSetEntity>();

                RelationPredicateBucket filter = new RelationPredicateBucket(AppDataSetFields.UsageTypeId == (int)EmAppDataSetUsageType.ErDiagram);

                adapter.FetchEntityCollection(list, filter, 0);
                return list;
            }
        }



        private static ValidationResult DeleteOneErDiagramEntity(object Id)
        {
            ValidationResult aValidationResult = new ValidationResult();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppDataSetEntity");

                    adapter.DeleteEntitiesDirectly(typeof(AppDataSetEntity), new RelationPredicateBucket(AppDataSetFields.DataSetId == Id));

                    adapter.Commit();
                }


                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));

                    adapter.Rollback();
                }
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessNewDto(AppDataSetExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppDataSetEntity aParentAppDataSetEntity = new AppDataSetEntity();
            AppDataSetConverter.CopyDtoToEntity(aParentAppDataSetEntity, aDto);


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aParentAppDataSetEntity);

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    aDto.Id = aParentAppDataSetEntity.DataSetId;

                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }



        private static ValidationResult ProcessDirtyDto(AppDataSetExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppDataSetEntity aAppDataSetEntity = RetrieveOneErDiagramEntity(aDto.Id);

            AppDataSetConverter.CopyDtoToEntity(aAppDataSetEntity, aDto);


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppDataSetEntity, false, true);


                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

    }
}