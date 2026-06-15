using System.Data;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using System;
using System.Linq;
using System.Collections.Generic;
using APP.Components.Dto;
using DatabaseSchemaMrg.DataSchema;
using APP.LBL;
using DatabaseSchemaMrg;


using APP.Framework;
namespace App.BL
{
    public static partial class AppEntityInfoBL
    {
        public static ObservableSet<AppEntityInfoDto> RetrieveAllAppEntityInfoDto(List<int> entityIdList = null)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppEntityInfoEntity> list = new EntityCollection<AppEntityInfoEntity>();

                RelationPredicateBucket filter = null;

                if (entityIdList != null && entityIdList.Count > 0)
                {
                    filter = new RelationPredicateBucket(AppEntityInfoFields.EntityInfoId == entityIdList.ToArray());
                }


                adapter.FetchEntityCollection(list, filter);



                var aDtoList = new ObservableSet<AppEntityInfoDto>();
                foreach (var aAppEntityInfoEntity in list)
                {
                    if (!aAppEntityInfoEntity.DataSourceFrom.HasValue)
                    {
                        aAppEntityInfoEntity.DataSourceFrom = ServerContext.Instance.DataSourceId;
                    }

                    aDtoList.Add(AppEntityInfoConverter.ConvertEntityToDto(aAppEntityInfoEntity));
                }

                return aDtoList;
            }
        }

        public static AppEntityInfoExDto RetrieveOneAppEntityInfoExDto(object id, bool? includeLookUpItems = null)
        {
            AppEntityInfoEntity aEntity = RetrieveOneAppEntityInfoEntity(id);
            AppEntityInfoExDto aDto = AppEntityInfoConverter.ConvertEntityToExDto(aEntity);
            foreach (var simpleEntity in aEntity.AppEntitySimpleListValue)
            {
                AppEntitySimpleListValueExDto simpleEntityExDto = AppEntitySimpleListValueConverter.ConvertEntityToExDto(simpleEntity);
                aDto.AppEntitySimpleListValueList.Add(simpleEntityExDto);
            }

            if (includeLookUpItems.HasValue && includeLookUpItems.Value)
            {
                aDto.EntityDataList = AppEntityInfoBL.GetLookupItemList((int)id, string.Empty);
            }
            return aDto;
        }



        // need to add delete validation result
        public static ValidationResult DeleteOneAppEntityInfo(object Id)
        {
            ValidationResult aValidationResult = new ValidationResult();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppEntityInfoEntity");

                    adapter.DeleteEntitiesDirectly(typeof(AppEntityInfoEntity), new RelationPredicateBucket(AppEntityInfoFields.EntityInfoId == Id));

                    adapter.Commit();
                }



                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEntityInfoEntity), "plm_AppEntityInfoEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    adapter.Rollback();
                }
            }

            return aValidationResult;
        }

        public static OperationCallResult<AppEntityInfoExDto> SaveOneAppEntityInfoDto(AppEntityInfoExDto appEntityInfoDto)
        {



            var fxiture = AppCacheManagerBL.GetOneDatabaseFixture(appEntityInfoDto.DataSourceFrom.Value);

            if (fxiture.SqlServerType == DatabaseSchemaMrg.DataSchema.EmSqlType.MySql)
            {
                if (!string.IsNullOrWhiteSpace(appEntityInfoDto.QueryText))
                {
                    appEntityInfoDto.QueryText = appEntityInfoDto.QueryText.Replace($@"[{fxiture.CurrentOwner}].", " ");
                    appEntityInfoDto.QueryText = appEntityInfoDto.QueryText.Replace($@"`{fxiture.CurrentOwner}`.", " ");
                }

            }

            OperationCallResult<AppEntityInfoExDto> aOperationCallResult = new OperationCallResult<AppEntityInfoExDto>();

            var aValidationResult = appEntityInfoDto.ValidateDto();
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }

            if (appEntityInfoDto.IsNew)
            {
                aOperationCallResult.ValidationResult = ProcessNewDto(appEntityInfoDto);
            }
            else
            {
                aOperationCallResult.ValidationResult = ProcessDirtyDto(appEntityInfoDto);

            }

            if (!aOperationCallResult.ValidationResult.HasErrors)
            {
                var savedDto = RetrieveOneAppEntityInfoExDto(appEntityInfoDto.Id);
                aOperationCallResult.Object = savedDto;

                if (savedDto.SaasApplicationId.HasValue && savedDto.DataSourceFrom.HasValue)
                {
                    if (!string.IsNullOrWhiteSpace(savedDto.TableName))
                    {
                        string tableName = savedDto.TableName.Trim().ToLower();
                        Dictionary<string, int> dictDbObjNameAndUsageType = new Dictionary<string, int>();

                        var dbFixture = AppCacheManagerBL.GetOneDatabaseFixture(savedDto.DataSourceFrom.Value);


                        if (dbFixture.AllViews().FirstOrDefault(o => o.Name.ToLower() == tableName) != null)
                        {
                            dictDbObjNameAndUsageType.Add(tableName, (int)EmAppDataSetUsageType.DatabaseView);
                        }
                        else
                        {
                            var existTable = dbFixture.Table(tableName);
                            if (existTable != null)
                            {
                                dictDbObjNameAndUsageType.Add(tableName, (int)EmAppDataSetUsageType.DatabaseTable);
                            }
                        }

                        if (dictDbObjNameAndUsageType.Count > 0)
                        {
                            AppDataSetBL.AddDatabaseObjectsToApplication(dictDbObjNameAndUsageType, savedDto.SaasApplicationId.Value);
                        }

                    }
                }
            }


            return aOperationCallResult;

        }





        private static ValidationResult ProcessNewDto(AppEntityInfoExDto aExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppEntityInfoEntity aAppEntityInfoEntity = new AppEntityInfoEntity();
            AppEntityInfoConverter.CopyDtoToEntity(aAppEntityInfoEntity, aExDto);


            foreach (var appsimpDto in aExDto.AppEntitySimpleListValueList)
            {
                AppEntitySimpleListValueEntity appEntitySimpleListValuentity = new AppEntitySimpleListValueEntity();
                AppEntitySimpleListValueConverter.CopyDtoToEntity(appEntitySimpleListValuentity, appsimpDto);
                aAppEntityInfoEntity.AppEntitySimpleListValue.Add(appEntitySimpleListValuentity);
            }


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppEntityInfoEntity, false, true);

                    adapter.Commit();

                    aExDto.Id = aAppEntityInfoEntity.EntityInfoId;
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEntityInfoEntity), "plm_AppEntityInfoEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEntityInfoEntity), "plm_AppEntityInfoEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        public static AppEntityInfoEntity RetrieveOneAppEntityInfoEntity(object Id)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppEntityInfoEntity aAppEntityInfoEntity = new AppEntityInfoEntity(int.Parse(Id.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppEntityInfoEntity);

                rootPath.Add(AppEntityInfoEntity.PrefetchPathAppEntitySimpleListValue);
                //  rootPath.Add(AppFormEntity.PrefetchPathAppFormLinkTarget);

                adapter.FetchEntity(aAppEntityInfoEntity, rootPath);


                if (string.IsNullOrWhiteSpace(aAppEntityInfoEntity.SchemaOwner))
                {
                    if (aAppEntityInfoEntity.DataSourceFrom.HasValue)
                    {
                        DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(aAppEntityInfoEntity.DataSourceFrom.Value);

                        aAppEntityInfoEntity.SchemaOwner = databaseFixtureInstance.CurrentOwner;
                    }
                }

                return aAppEntityInfoEntity;
            }

        }


        public static AppEntityInfoEntity RetrieveOneAppEntityInfoEntityWithCode(string entiycode)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                EntityCollection<AppEntityInfoEntity> list = new EntityCollection<AppEntityInfoEntity>();
                adapter.FetchEntityCollection(list, new RelationPredicateBucket(AppEntityInfoFields.EntityCode == entiycode));

                if (list.Count > 0)
                {
                    var aAppEntityInfoEntity = list[0];

                    if (!aAppEntityInfoEntity.DataSourceFrom.HasValue)
                    {
                        aAppEntityInfoEntity.DataSourceFrom = ServerContext.Instance.DataSourceId;
                    }

                    return aAppEntityInfoEntity;
                }
                else
                {
                    return null;

                }




            }
        }

        public static OperationCallResult<AppEntityInfoExDto> GenerateNewEntity(string entityCode, int? saasApplicationId)
        {
            if (!string.IsNullOrWhiteSpace(entityCode))
            {
                OperationCallResult<AppEntityInfoExDto> aOperationCallResult = new OperationCallResult<AppEntityInfoExDto>();
                var aValidationResult = new ValidationResult();
                aOperationCallResult.ValidationResult = aValidationResult;


                try
                {
                    string schemaOwner = AppMetaDataBL.GetSchemaOwnerByDataSourceRegId(ServerContext.Instance.CurrnetClientIdentity.DataSourceId);


                    Dictionary<string, DatabaseTableDto> dictTableNameAndDto = AppMetaDataBL.GetDataSourceTableAndViewList(ServerContext.Instance.CurrnetClientIdentity.DataSourceId).Where(o => !o.IsDbView).ToDictionary(o => o.Name.ToLower(), o => o);
                    Dictionary<string, AppEntityInfoDto> dictEntityCodeAndDto = RetrieveAllAppEntityInfoDto().ToDictionary(o => o.EntityCode.ToLower(), o => o);

                    string newEntityCode = entityCode;
                    int prefix = 0;

                    while (dictTableNameAndDto.ContainsKey(newEntityCode.ToLower()) || dictEntityCodeAndDto.ContainsKey(newEntityCode.ToLower()))
                    {
                        prefix++;
                        newEntityCode = entityCode + "_" + prefix.ToString();
                    }

                    DatabaseTable databaseTableDto = new DatabaseTable();
                    databaseTableDto.Name = newEntityCode;
                    databaseTableDto.DataSourceRegisterId = ServerContext.Instance.CurrnetClientIdentity.DataSourceId;
                    databaseTableDto.SchemaOwner = schemaOwner;

                    DatabaseColumn primaryColumn = new DatabaseColumn();
                    primaryColumn.Name = "ID";
                    primaryColumn.DbDataType = "int";
                    primaryColumn.IsAutoNumber = true;
                    primaryColumn.IsPrimaryKey = true;
                    primaryColumn.Nullable = false;
                    primaryColumn.Tag = (EmAppDataType.Integer).ToString();
                    databaseTableDto.Columns.Add(primaryColumn);

                    DatabaseColumn aDatabaseColumn2 = new DatabaseColumn();
                    aDatabaseColumn2.Name = "Name";
                    aDatabaseColumn2.Tag = (EmAppDataType.String).ToString();
                    databaseTableDto.Columns.Add(aDatabaseColumn2);

                    DatabaseColumn aDatabaseColumn3 = new DatabaseColumn();
                    aDatabaseColumn3.Name = "Description";
                    aDatabaseColumn3.Tag = (EmAppDataType.String).ToString();
                    databaseTableDto.Columns.Add(aDatabaseColumn3);

                    AppFromDataModelBL.AddSystemCreatedAndModifiedByColumns(databaseTableDto);
                    string createTableResultMsg = "";
                    bool tableCreated = AppMetaDataBL.CreateNewTable(databaseTableDto, databaseTableDto.DataSourceRegisterId, saasApplicationId, out createTableResultMsg);

                    if (tableCreated)
                    {
                        AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(databaseTableDto.DataSourceRegisterId);

                        AppEntityInfoExDto newEntityInfoDto = new AppEntityInfoExDto();
                        newEntityInfoDto.EntityCode = databaseTableDto.Name;
                        newEntityInfoDto.EntityType = (int)EmAppEntityType.SystemDefineTable;
                        newEntityInfoDto.DataSourceFrom = databaseTableDto.DataSourceRegisterId;
                        newEntityInfoDto.TableName = databaseTableDto.Name;
                        newEntityInfoDto.IdentityField = "ID";
                        newEntityInfoDto.DisplayFiled1 = "Name";
                        newEntityInfoDto.SchemaOwner = schemaOwner;
                        newEntityInfoDto.SaasApplicationId = saasApplicationId;


                        var saveEntiyResult = SaveOneAppEntityInfoDto(newEntityInfoDto);

                        if (saveEntiyResult.IsSuccessfulWithResult)
                        {

                            AppEntityInfoBL.AddOneLookupItemList((int)saveEntiyResult.Object.Id, "Item 1");
                            AppEntityInfoBL.AddOneLookupItemList((int)saveEntiyResult.Object.Id, "Item 2");

                            OperationCallResult<AppTransactionExDto> saveTransactionResult = AppTransactionBL.CreateDefaultListTransactionFromTableName(databaseTableDto.Name, databaseTableDto.DataSourceRegisterId, schemaOwner, saasApplicationId);

                            if (saveTransactionResult.IsSuccessfulWithResult)
                            {
                                aValidationResult.Items.Add(new ValidationItem(typeof(AppEntityInfoEntity), AppEntityInfoDto.EntityCodeProperty, ValidationItemType.Message, "Saved Successfully"));
                                aOperationCallResult.Object = saveEntiyResult.Object;
                            }
                            else
                            {
                                aValidationResult.Merge(saveTransactionResult.ValidationResult);
                            }
                        }
                        else
                        {
                            aValidationResult.Merge(saveEntiyResult.ValidationResult);
                        }
                    }
                    else
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEntityInfoEntity), "plm_AppEntityInfoEntity_TableCreation_Error", ValidationItemType.Error, "Table Creation Failed"));
                    }
                }
                catch (Exception ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEntityInfoEntity), "plm_AppEntityInfoEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }

                return aOperationCallResult;
            }

            return null;
        }



        public static OperationCallResult<AppEntityInfoExDto> GenerateQueryEntityFromDataSetField(int? datasetId, string datasetFieldName)
        {

            OperationCallResult<AppEntityInfoExDto> aOperationCallResult = new OperationCallResult<AppEntityInfoExDto>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppDataSetExDto dataSetExDto = GenerateQueryEnitty_ValidateDataSet(datasetId, datasetFieldName, aValidationResult);

            if (!aValidationResult.HasErrors && dataSetExDto != null)
            {
                Dictionary<string, AppEntityInfoDto> dictEntityCodeAndDto = RetrieveAllAppEntityInfoDto().GroupBy(o => o.EntityCode)
                    .ToDictionary(o => o.Key.ToLower(), o => o.ToList().First());

                string newEntityCode = datasetFieldName;
                int prefix = 0;

                while (dictEntityCodeAndDto.ContainsKey(newEntityCode.ToLower()))
                {
                    prefix++;
                    newEntityCode = datasetFieldName + "_" + prefix.ToString();
                }

                AppEntityInfoExDto newEntityInfoDto = new AppEntityInfoExDto();
                newEntityInfoDto.EntityCode = newEntityCode;
                newEntityInfoDto.EntityType = (int)EmAppEntityType.SimpleQuery;
                newEntityInfoDto.DataSourceFrom = dataSetExDto.DataSourceFrom;
                newEntityInfoDto.SaasApplicationId = dataSetExDto.SaasApplicationId;

                newEntityInfoDto.QueryText = $@"SELECT DISTINCT [{datasetFieldName}] 
                    FROM (
                        {dataSetExDto.QueryText}
                    ) as DataSetQueryResult ";

                var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(newEntityInfoDto.DataSourceFrom.Value);


                newEntityInfoDto.QueryText = dataBaseFixture.ReformatQuery(newEntityInfoDto.QueryText);






                string entityQueryErrorMessage = dataBaseFixture.ValidateQueryText(newEntityInfoDto.QueryText);

                if (!string.IsNullOrWhiteSpace(entityQueryErrorMessage))
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEntityInfoEntity), "plm_AppEntityInfoEntity_GenerateEntity_Error", ValidationItemType.Error, "Generate entity failed. Invalid entity query: \n" + entityQueryErrorMessage + "\n" + newEntityInfoDto.QueryText));
                }
                else
                {
                    var saveEntiyResult = SaveOneAppEntityInfoDto(newEntityInfoDto);

                    aValidationResult.Merge(saveEntiyResult.ValidationResult);
                    aOperationCallResult.Object = saveEntiyResult.Object;

                }
            }

            return aOperationCallResult;
        }

        private static AppDataSetExDto GenerateQueryEnitty_ValidateDataSet(int? datasetId, string datasetFieldName, ValidationResult aValidationResult)
        {

            if (!datasetId.HasValue)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEntityInfoEntity), "plm_AppEntityInfoEntity_GenerateEntity_Error", ValidationItemType.Error, "Generate entity failed. Invalid dataset Id."));
            }
            else if (string.IsNullOrWhiteSpace(datasetFieldName))
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEntityInfoEntity), "plm_AppEntityInfoEntity_GenerateEntity_Error", ValidationItemType.Error, "Generate entity failed. Invalid dataset field name."));
            }
            else
            {
                try
                {
                    AppDataSetExDto dataSetExDto = AppDataSetBL.RetrieveOneAppDataSetExDto(datasetId);


                    if (!aValidationResult.HasErrors)
                    {
                        if (!(dataSetExDto.QueryType.HasValue && dataSetExDto.QueryType.Value == (int)EmAppDataServiceType.QueryText))
                        {
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppEntityInfoEntity), "plm_AppEntityInfoEntity_GenerateEntity_Error", ValidationItemType.Error, "Generate entity failed. The DataSet is not the type of Query Text."));
                        }
                        else if (!dataSetExDto.DataSourceFrom.HasValue)
                        {
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppEntityInfoEntity), "plm_AppEntityInfoEntity_GenerateEntity_Error", ValidationItemType.Error, "Generate entity failed. Unknown DataSet DataSourceFrom Id."));
                        }
                        else
                        {

                            if (string.IsNullOrWhiteSpace(dataSetExDto.QueryText))
                            {
                                aValidationResult.Items.Add(new ValidationItem(typeof(AppEntityInfoEntity), "plm_AppEntityInfoEntity_GenerateEntity_Error", ValidationItemType.Error, "Generate entity failed. DataSet query text is empty."));
                            }
                            else
                            {
                                var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSetExDto.DataSourceFrom.Value);
                                string datasetQueryErrorMessage = dataBaseFixture.ValidateQueryText(dataSetExDto.QueryText);

                                if (!string.IsNullOrWhiteSpace(datasetQueryErrorMessage))
                                {
                                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEntityInfoEntity), "plm_AppEntityInfoEntity_GenerateEntity_Error", ValidationItemType.Error, "Generate entity failed. Invalid DataSet query: \n" + datasetQueryErrorMessage + "\n" + dataSetExDto.QueryText));
                                }
                                else
                                {
                                    return dataSetExDto;
                                }
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEntityInfoEntity), "plm_AppEntityInfoEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return null;
        }

        private static ValidationResult ProcessDirtyDto(AppEntityInfoExDto appEntityInfoExDto)
        {


            ValidationResult aValidationResult = new ValidationResult();

            int[] dirtyFieldIds = appEntityInfoExDto.AppEntitySimpleListValueList.FindModifiedItems().Select(o => o.Id).Cast<int>().ToArray();

            AppEntityInfoEntity aAppEntityInfoEntity = RetrieveOneAppEntityInfoEntity(appEntityInfoExDto.Id);

            Dictionary<int, AppEntitySimpleListValueEntity> dictAppEntitySimpleListValueFromDbms = aAppEntityInfoEntity.AppEntitySimpleListValue.ToDictionary(o => o.SimpleListValueId, o => o);


            AppEntityInfoConverter.CopyDtoToEntity(aAppEntityInfoEntity, appEntityInfoExDto);

            foreach (var aChildDto in appEntityInfoExDto.AppEntitySimpleListValueList.FindNewItems())
            {
                AppEntitySimpleListValueEntity aNewChildEntity = new AppEntitySimpleListValueEntity();
                AppEntitySimpleListValueConverter.CopyDtoToEntity(aNewChildEntity, aChildDto);
                aAppEntityInfoEntity.AppEntitySimpleListValue.Add(aNewChildEntity);
            }

            // Dirty items
            foreach (var modifyitem in appEntityInfoExDto.AppEntitySimpleListValueList.FindModifiedItems())
            {
                int dtoKey = int.Parse(modifyitem.Id.ToString());
                if (dictAppEntitySimpleListValueFromDbms.ContainsKey(dtoKey))
                {
                    AppEntitySimpleListValueConverter.CopyDtoToEntity(dictAppEntitySimpleListValueFromDbms[dtoKey], modifyitem);
                }
            }

            List<int> existItemIds = dictAppEntitySimpleListValueFromDbms.Keys.ToList();
            List<int> currentItemIds = appEntityInfoExDto.AppEntitySimpleListValueList.Where(o => o.Id != null).Select(o => (int)o.Id).ToList();
            List<int> deleteFieldIDs = existItemIds.Except(currentItemIds).ToList();

            // deletedIDs
            //int[] deleteFieldIDs = appEntityInfoExDto.AppEntitySimpleListValueList.FindDeletedItemIds().Cast<int>().ToArray();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppEntityInfoEntity, false, true);

                    if (deleteFieldIDs.Count() > 0)
                    {
                        adapter.DeleteEntitiesDirectly(typeof(AppEntitySimpleListValueEntity), new RelationPredicateBucket(AppEntitySimpleListValueFields.SimpleListValueId == deleteFieldIDs.ToArray()));
                    }

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEntityInfoEntity), AppEntityInfoDto.EntityCodeProperty, ValidationItemType.Message, "Saved Successfully"));

                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEntityInfoEntity), "plm_AppEntityInfoEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }


    }
}