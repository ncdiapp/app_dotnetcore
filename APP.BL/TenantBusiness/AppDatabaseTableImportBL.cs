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


using DatabaseSchemaMrg;

using System.Drawing;


using Newtonsoft.Json;

using System.Dynamic;
using AngleSharp.Common;
using System.Xml;
#if NETFRAMEWORK
using Microsoft.Exchange.WebServices.Data;
// TODO-PHASE4: Replace with .NET 10 equivalent
#endif
using ExchangeBL;
using System.IO;


using APP.Framework;
namespace App.BL
{
    public static class AppDatabaseTableImportBL
    {

        public static readonly string SystemDefinedColumnName_OriginalFile = "OriginalFile";

        public static List<AppDataSetExDto> RetrieveSaasApplicationExcelTableImportSettingList(int? applicationId, bool isFlatSingleTableImport)
        {
            List<AppDataSetExDto> toReturn = new List<AppDataSetExDto>();

            if (applicationId.HasValue)
            {
                List<AppDataSetExDto> allDataSetList = RetrieveAllExcelTableImportSettingDto(isFlatSingleTableImport).ToList();
                toReturn = allDataSetList.Where(o => (o.SaasApplicationId.HasValue && o.SaasApplicationId.Value == applicationId.Value)).ToList();
            }

            return toReturn;
        }

        public static List<AppDataSetExDto> RetrieveSaasApplicationDbToDbTableImportSettingList(int? applicationId)
        {
            List<AppDataSetExDto> toReturn = new List<AppDataSetExDto>();

            if (applicationId.HasValue)
            {
                List<AppDataSetExDto> allDataSetList = RetrieveAllDbToDbTableImportSettingDto().ToList();
                toReturn = allDataSetList.Where(o => (o.SaasApplicationId.HasValue && o.SaasApplicationId.Value == applicationId.Value)).ToList();
            }

            return toReturn;
        }

        public static List<AppDataSetExDto> RetrieveSaasApplicationEntityImportSettingList(int? applicationId)
        {
            List<AppDataSetExDto> toReturn = new List<AppDataSetExDto>();

            if (applicationId.HasValue)
            {
                List<AppDataSetExDto> allDataSetList = RetrieveAllEntityImportSettingDto().ToList();
                toReturn = allDataSetList.Where(o => (o.SaasApplicationId.HasValue && o.SaasApplicationId.Value == applicationId.Value)).ToList();
            }

            return toReturn;
        }

        public static ObservableSet<AppDataSetExDto> RetrieveAllExcelTableImportSettingDto(bool isFlatSingleTableImport)
        {
            ObservableSet<AppDataSetExDto> aSet = new ObservableSet<AppDataSetExDto>();
            EntityCollection<AppDataSetEntity> list = RetrieveAllExcelTableImportSettingEntity();
            foreach (var o in list)
            {
                AppDataSetExDto aDto = AppDataSetConverter.ConvertEntityToExDto(o);

                if (aDto.OtherSettingsDto != null)
                {
                    aDto.OtherSettingsDto.DatabaseDiagramInfo = null;

                    if (aDto.OtherSettingsDto.TableImportSettingDto != null)
                    {
                        aDto.OtherSettings = "";
                        var settingDto = aDto.OtherSettingsDto.TableImportSettingDto;


                        settingDto.OrgSourceColumns = new List<DatabaseColumnExDto>();
                        settingDto.SourceColumns = new List<DatabaseColumnExDto>();

                        List<DatabaseTableInfoDto> simpleTables = new List<DatabaseTableInfoDto>();
                        settingDto.Tables.ForAll(t =>
                            simpleTables.Add(
                                new DatabaseTableInfoDto()
                                {
                                    Name = t.Name,
                                    DataSourceRegisterId = t.DataSourceRegisterId,
                                    SchemaOwner = t.SchemaOwner
                                }));

                        settingDto.Tables = simpleTables;
                        settingDto.DictTableNameAndEntityLookUpIdColumnName = new Dictionary<string, string>();
                        settingDto.DictTableNameAndEntityLookUpDisplayColumnNameList = new Dictionary<string, List<string>>();
                        settingDto.DictTableNameColumnNameAndCascadingDto = new Dictionary<string, Dictionary<string, DatabaseColumnCascadingDto>>();


                        if (isFlatSingleTableImport)
                        {
                            if (aDto.OtherSettingsDto.TableImportSettingDto.IsFlatSingleTableImport)
                            {
                                aSet.Add(aDto);
                            }
                        }
                        else
                        {
                            aSet.Add(aDto);
                        }
                    }
                }



            }

            return aSet;
        }

        public static ObservableSet<AppDataSetExDto> RetrieveAllDbToDbTableImportSettingDto()
        {
            ObservableSet<AppDataSetExDto> aSet = new ObservableSet<AppDataSetExDto>();
            EntityCollection<AppDataSetEntity> list = RetrieveAllDbToDbTableImportSettingEntity();
            foreach (var o in list)
            {
                AppDataSetExDto aDto = AppDataSetConverter.ConvertEntityToExDto(o);

                if (aDto.OtherSettingsDto != null)
                {
                    aDto.OtherSettingsDto.DatabaseDiagramInfo = null;

                    if (aDto.OtherSettingsDto.TableImportSettingDto != null)
                    {
                        aDto.OtherSettings = "";
                        var settingDto = aDto.OtherSettingsDto.TableImportSettingDto;


                        settingDto.OrgSourceColumns = new List<DatabaseColumnExDto>();
                        settingDto.SourceColumns = new List<DatabaseColumnExDto>();

                        List<DatabaseTableInfoDto> simpleTables = new List<DatabaseTableInfoDto>();
                        settingDto.Tables.ForAll(t =>
                            simpleTables.Add(
                                new DatabaseTableInfoDto()
                                {
                                    Name = t.Name,
                                    DataSourceRegisterId = t.DataSourceRegisterId,
                                    SchemaOwner = t.SchemaOwner
                                }));

                        settingDto.Tables = simpleTables;
                        settingDto.DictTableNameAndEntityLookUpIdColumnName = new Dictionary<string, string>();
                        settingDto.DictTableNameAndEntityLookUpDisplayColumnNameList = new Dictionary<string, List<string>>();
                        settingDto.DictTableNameColumnNameAndCascadingDto = new Dictionary<string, Dictionary<string, DatabaseColumnCascadingDto>>();

                        aSet.Add(aDto);

                    }
                }



            }

            return aSet;
        }




        public static ObservableSet<AppDataSetExDto> RetrieveAllEntityImportSettingDto()
        {
            ObservableSet<AppDataSetExDto> aSet = new ObservableSet<AppDataSetExDto>();
            EntityCollection<AppDataSetEntity> list = RetrieveAllEntityImportSettingEntity();
            foreach (var o in list)
            {
                AppDataSetExDto aDto = AppDataSetConverter.ConvertEntityToExDto(o);
                aSet.Add(aDto);
            }

            return aSet;
        }



        public static OperationCallResult<AppDataSetExDto> ProcessMetaDataTablesImport(AppDataSetExDto aAppDataSetExDto)
        {
            if (aAppDataSetExDto.UsageTypeId.HasValue && aAppDataSetExDto.UsageTypeId.Value == (int)EmAppDataSetUsageType.DbToDbTableImportSetting)
            {
                return ProcessDbToDbTableImport(aAppDataSetExDto);
            }


            OperationCallResult<AppDataSetExDto> aOperationCallResult = new OperationCallResult<AppDataSetExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            ProcessMetaDataTablesImport_InitializeImportSetting(aAppDataSetExDto);

            aValidationResult.Merge(validateTableImportSettingDto(aAppDataSetExDto));

            if (aValidationResult.HasErrors)
            {
                return aOperationCallResult;
            }

            if (aAppDataSetExDto.OtherSettingsDto != null && aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto != null)
            {

                try
                {
                    var importSettingDto = aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto;

                    if (importSettingDto.IsHaveSimulateImportedData && aAppDataSetExDto.Id != null)
                    {
                        var result = DeleteSimulateImportedData((int)aAppDataSetExDto.Id);

                        if (!result.IsSuccessful)
                        {
                            aValidationResult.Merge(result.ValidationResult);
                            return aOperationCallResult;
                        }
                    }

                    // need to drop !!!!
                    CreateNewTempTableFromOrgTempTable(importSettingDto, aValidationResult);

                    if (aValidationResult.HasErrors)
                    {
                        return aOperationCallResult;
                    }

                    importSettingDto.Tables.ForAll(o => o.IsNewTable = true);
                    importSettingDto.CurrentImportFileName = importSettingDto.ImportFileName;

                    // is it normalize the big table to mutiple small tables
                    aValidationResult.Merge(ProcessTempTableToTargetTableImport(importSettingDto));
                    aAppDataSetExDto = PostImportProcess(aAppDataSetExDto, aOperationCallResult, aValidationResult, importSettingDto);

                }
                catch (Exception ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_CreateTableImportSettingAndProcessImport_Error", ValidationItemType.Error, "Import Failed.\n" + ex.ToString()));
                }


            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_CreateTableImportSettingAndProcessImport_Error", ValidationItemType.Error, "Import Failed."));
            }



            if (!aValidationResult.HasErrors)
            {
                aValidationResult.Items.Clear();
                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_UpdateImportedTableDataFromTempTable_Ok", ValidationItemType.Message, "Import Success."));




            }

            return aOperationCallResult;
        }


        public static OperationCallResult<AppDataSetExDto> ProcessDbToDbTableImport(AppDataSetExDto aAppDataSetExDto)
        {

            OperationCallResult<AppDataSetExDto> aOperationCallResult = new OperationCallResult<AppDataSetExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (aAppDataSetExDto.OtherSettingsDto != null && aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto != null)
            {
                var importSettingDto = aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto;


                if (importSettingDto.Tables != null
                    && importSettingDto.SourceDataSourceFrom.HasValue
                   && aAppDataSetExDto.DataSourceFrom.HasValue
                   && importSettingDto.SourceDataSourceType.HasValue)
                {
                    try
                    {
                        var srcFixture = AppCacheManagerBL.GetOneDatabaseFixture(importSettingDto.SourceDataSourceFrom.Value);
                        var targetFixture = AppCacheManagerBL.GetOneDatabaseFixture(aAppDataSetExDto.DataSourceFrom.Value);

                        // Step 1. Prepare Source DataTable
                        DataTable srcDataTable = ProcessDbToDbTableImport_PopulateSourceDataTable(aValidationResult, importSettingDto, srcFixture);


                        // Step 2. Run Query on Target Table: Insert and Update
                        if (!aValidationResult.HasErrors && srcDataTable != null)
                        {
                            foreach (DatabaseTableInfoDto targerTableDto in importSettingDto.LevelOneTables)
                            {
                                ProcessDbToDbTableImport_ProcessOneTable(aAppDataSetExDto, aValidationResult, targetFixture, srcDataTable, targerTableDto, null);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_CreateTableImportSettingAndProcessImport_Error", ValidationItemType.Error, "Import Failed.\n" + ex.ToString()));
                    }

                }
                else
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_CreateDbToDbTableImportSetting_Error", ValidationItemType.Error, "Invalid Import Setting."));
                }



                if (!aValidationResult.HasErrors)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Message, "Import Success."));

                    AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(importSettingDto.DataSourceRegisterId);

                    aAppDataSetExDto = PostImportProcess(aAppDataSetExDto, aOperationCallResult, aValidationResult, importSettingDto);

                }


            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_ProcessDbToDbTableImport_Error", ValidationItemType.Error, "Import Failed."));
            }



            return aOperationCallResult;
        }



        public static OperationCallResult<AppDataSetExDto> SaveDraftTableImportSetting(AppDataSetExDto aAppDataSetExDto)
        {
            bool isNewTransactionDataUpdateImportSetting = false;
            int? transactionId = null;

            if (aAppDataSetExDto.OtherSettingsDto != null && aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto != null)
            {
                DatabaseTableImportSettingDto importSettingDto = aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto;

                if (string.IsNullOrWhiteSpace(importSettingDto.OrgTempTableName))
                {
                    importSettingDto.OrgTempTableName = importSettingDto.TempTableName;
                }

                if (importSettingDto.NeedToUpdateTransactionId.HasValue)
                {
                    //if (importSettingDto.Status.HasValue && importSettingDto.Status.Value == (int)EmAppImportStatus.Draft)
                    //{
                    if (importSettingDto.Tables == null || importSettingDto.Tables.Count == 0)
                    {
                        isNewTransactionDataUpdateImportSetting = true;
                        transactionId = importSettingDto.NeedToUpdateTransactionId;

                        PrepareNewImportSettingTablesFromTransaction(aAppDataSetExDto);


                    }
                    //}
                }
            }

            OperationCallResult<AppDataSetExDto> aOperationCallResult = AppDatabaseErDiagramBL.SaveOneErDiagramExDto(aAppDataSetExDto);

            if (aOperationCallResult.IsSuccessfulWithResult)
            {
                if (isNewTransactionDataUpdateImportSetting && transactionId.HasValue)
                {
                    int? importSettingId = ControlTypeValueConverter.ConvertValueToInt(aOperationCallResult.Object.Id);

                    if (importSettingId.HasValue)
                    {
                        var transactionExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId.Value);

                        if (transactionExDto.OtherOptions == null)
                        {
                            transactionExDto.OtherOptions = new TransactionOptionDto();
                        }

                        transactionExDto.OtherOptions.TransactionDataUpdateImportSettingId = importSettingId.Value;

                        var saveTransactionResult = AppTransactionBL.SaveAppTransactionExDto(transactionExDto);

                        if (saveTransactionResult.ValidationResult.HasErrors)
                        {
                            aOperationCallResult.ValidationResult.Merge(saveTransactionResult.ValidationResult);
                        }
                    }
                }
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<AppDataSetExDto> ResetDbToDbImportSourceColumns(AppDataSetExDto aAppDataSetExDto)
        {

            OperationCallResult<AppDataSetExDto> aOperationCallResult = new OperationCallResult<AppDataSetExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (aAppDataSetExDto.UsageTypeId == (int)EmAppDataSetUsageType.DbToDbTableImportSetting
                && aAppDataSetExDto.OtherSettingsDto != null && aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto != null)
            {

                var importSettingDto = aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto;

                if (importSettingDto.SourceDataSourceFrom.HasValue
                    && aAppDataSetExDto.DataSourceFrom.HasValue
                    && importSettingDto.SourceDataSourceType.HasValue)
                {
                    var srcFixture = AppCacheManagerBL.GetOneDatabaseFixture(importSettingDto.SourceDataSourceFrom.Value);
                    var targetFixture = AppCacheManagerBL.GetOneDatabaseFixture(aAppDataSetExDto.DataSourceFrom.Value);


                    if (importSettingDto.SourceDataSourceType.Value == (int)EmAppDbToDbImportSourceType.DatabaseTable && !string.IsNullOrWhiteSpace(importSettingDto.SourceTableName))
                    {
                        PrepareImportSourceColumnsFromSourceTable(aValidationResult, importSettingDto, srcFixture);

                    }
                    else if (importSettingDto.SourceDataSourceType.Value == (int)EmAppDbToDbImportSourceType.DataSet && importSettingDto.SourceDataSetId.HasValue)
                    {
                        PrepareImportSourceColumnsFromSourceDataSet(aValidationResult, importSettingDto, srcFixture);
                    }

                    if (importSettingDto.SourceColumns != null && importSettingDto.SourceColumns.Count > 0)
                    {
                        aOperationCallResult = SaveDraftTableImportSetting(aAppDataSetExDto);
                    }
                    else
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_CreateDbToDbTableImportSetting_Error", ValidationItemType.Error, "Generate Source Columns Failed."));
                    }
                }
                else
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_CreateDbToDbTableImportSetting_Error", ValidationItemType.Error, "Invalid Import Setting."));
                }
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_CreateDbToDbTableImportSetting_Error", ValidationItemType.Error, "Invalid Import Setting."));
            }

            return aOperationCallResult;
        }

        //public static List<DatabaseColumn> GetDbToDbImportSettingOrgSourceColumns(AppDataSetExDto aAppDataSetExDto) 
        //{
        //    OperationCallResult<AppDataSetExDto> aOperationCallResult = new OperationCallResult<AppDataSetExDto>();
        //    ValidationResult aValidationResult = new ValidationResult();
        //    aOperationCallResult.ValidationResult = aValidationResult;

        //    if (aAppDataSetExDto.UsageTypeId == (int)EmAppDataSetUsageType.DbToDbTableImportSetting
        //        && aAppDataSetExDto.OtherSettingsDto != null && aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto != null)
        //    {

        //        var importSettingDto = aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto;

        //        if (importSettingDto.SourceDataSourceFrom.HasValue
        //            && aAppDataSetExDto.DataSourceFrom.HasValue
        //            && importSettingDto.SourceDataSourceType.HasValue)
        //        {
        //            var srcFixture = AppCacheManagerBL.GetOneDatabaseFixture(importSettingDto.SourceDataSourceFrom.Value);
        //            var targetFixture = AppCacheManagerBL.GetOneDatabaseFixture(aAppDataSetExDto.DataSourceFrom.Value);


        //            if (importSettingDto.SourceDataSourceType.Value == (int)EmAppDbToDbImportSourceType.DatabaseTable && !string.IsNullOrWhiteSpace(importSettingDto.SourceTableName))
        //            {
        //                string queryDataSet = $@"SELECT * FROM [{importSettingDto.SourceTableName}]";

        //                //List<DbParameter> sqlParamterList = new List<DbParameter>();
        //                //srcDataTable = srcFixture.RetriveDataTable(queryDataSet, sqlParamterList);

        //                PrepareImportSourceColumnsFromQuery(importSettingDto, srcFixture, queryDataSet);

        //            }
        //            else if (importSettingDto.SourceDataSourceType.Value == (int)EmAppDbToDbImportSourceType.DataSet && importSettingDto.SourceDataSetId.HasValue)
        //            {
        //                var sourceDataSetDto = AppDataSetBL.RetrieveOneAppDataSetExDto(importSettingDto.SourceDataSetId.Value);


        //                if (sourceDataSetDto.QueryType.HasValue && sourceDataSetDto.QueryType.Value == (int)EmAppDataServiceType.QueryText
        //                    && !string.IsNullOrWhiteSpace(sourceDataSetDto.QueryText))
        //                {
        //                    string queryDataSet = sourceDataSetDto.QueryText;

        //                    //List<DbParameter> sqlParamterList = new List<DbParameter>();
        //                    //srcDataTable = srcFixture.RetriveDataTable(queryDataSet, sqlParamterList);

        //                    PrepareImportSourceColumnsFromQuery(importSettingDto, srcFixture, queryDataSet);
        //                }
        //                else
        //                {
        //                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_CreateDbToDbTableImportSetting_Error", ValidationItemType.Error, "Dataset Query Text Is Empty."));
        //                }
        //            }

        //            if (importSettingDto.SourceColumns != null && importSettingDto.SourceColumns.Count > 0)
        //            {
        //                aOperationCallResult = SaveDraftTableImportSetting(aAppDataSetExDto);
        //            }
        //            else
        //            {
        //                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_CreateDbToDbTableImportSetting_Error", ValidationItemType.Error, "Generate Source Columns Failed."));
        //            }
        //        }
        //        else
        //        {
        //            aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_CreateDbToDbTableImportSetting_Error", ValidationItemType.Error, "Invalid Import Setting."));
        //        }
        //    }
        //    else
        //    {
        //        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_CreateDbToDbTableImportSetting_Error", ValidationItemType.Error, "Invalid Import Setting."));
        //    }

        public static OperationCallResult<AppDataSetExDto> UpdateTransactionDataFromImport(AppDataSetExDto aAppDataSetExDto)
        {
            OperationCallResult<AppDataSetExDto> aOperationCallResult = new OperationCallResult<AppDataSetExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;


            aValidationResult.Merge(validateTableImportSettingDto(aAppDataSetExDto));

            if (aValidationResult.HasErrors)
            {
                return aOperationCallResult;
            }

            if (aAppDataSetExDto.OtherSettingsDto != null && aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto != null)
            {
                var importSettingDto = aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto;






                aValidationResult.Merge(ProcessTransactionTableDataImport(importSettingDto));

                if (importSettingDto.NeedToDropTempTableNames != null && importSettingDto.NeedToDropTempTableNames.Count > 0)
                {
                    importSettingDto.NeedToDropTempTableNames.ForAll(o => DeleteTempTable(importSettingDto.DataSourceRegisterId, o, aValidationResult));
                }

                if (!aValidationResult.HasErrors)
                {
                    if (aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo == null)
                    {
                        aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo = new DatabaseViewDto();
                        aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo.IsErDiagram = true;
                        aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo.DataSourceRegisterId = aAppDataSetExDto.DataSourceFrom;

                        aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo.DictTables = new Dictionary<string, DatabaseViewTableDto>(StringComparer.OrdinalIgnoreCase);
                    }

                    var diagramDto = aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo;

                    ViewTableAddRemoveDto viewTableAddDto = new ViewTableAddRemoveDto(diagramDto);

                    viewTableAddDto.NeedToAddOwnerTablePairList = new List<KeyValuePair<string, string>>();

                    foreach (var tableDto in importSettingDto.Tables)
                    {
                        AppCacheManagerBL.RefreshOneTableCache(tableDto.Name, aAppDataSetExDto.DataSourceFrom, tableDto.SchemaOwner);
                        viewTableAddDto.NeedToAddOwnerTablePairList.Add(new KeyValuePair<string, string>(tableDto.SchemaOwner, tableDto.Name));
                    }

                    diagramDto = aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo = AppDatabaseViewBL.AddTablesToDatabaseView(viewTableAddDto);

                    ResetDiagramTablePositionByLevel(importSettingDto, diagramDto);


                    var saveSettingResult = AppDatabaseErDiagramBL.SaveOneErDiagramExDto(aAppDataSetExDto);

                    if (saveSettingResult.IsSuccessfulWithResult)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_CreateTableImportSettingAndProcessImport_Ok", ValidationItemType.Message, "Save Import Setting Success."));

                        aAppDataSetExDto = saveSettingResult.Object;

                        importSettingDto.IsDataImported = true;
                        aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto = importSettingDto;
                        aAppDataSetExDto.IsModified = true;

                        foreach (var tableDto in importSettingDto.Tables)
                        {
                            tableDto.IsNewTable = false;
                            tableDto.DictNewColumnNameAndDto = null;
                        }

                        AppDatabaseErDiagramBL.SaveOneErDiagramExDto(aAppDataSetExDto);

                    }
                    else
                    {
                        aValidationResult.Merge(saveSettingResult.ValidationResult);
                    }
                }
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_CreateTableImportSettingAndProcessImport_Error", ValidationItemType.Error, "Import Failed."));
            }



            return aOperationCallResult;

        }



        public static OperationCallResult<AppDataSetExDto> UpdateTransactionDataFromImport_ReloadTransaction(int importSettingDataSetId)
        {
            OperationCallResult<AppDataSetExDto> aOperationCallResult = new OperationCallResult<AppDataSetExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppDataSetExDto dataSetExDto = AppDatabaseErDiagramBL.RetrieveOneErDiagramExDto(importSettingDataSetId);

            if (dataSetExDto.OtherSettingsDto != null && dataSetExDto.OtherSettingsDto.TableImportSettingDto != null)
            {
                DatabaseTableImportSettingDto importSettingDto = dataSetExDto.OtherSettingsDto.TableImportSettingDto;

                if (importSettingDto.NeedToUpdateTransactionId.HasValue)
                {
                    //int transactionId = importSettingDto.NeedToUpdateTransactionId.Value;

                    RebuildImportSettingTablesFromTransaction(importSettingDto);


                    if (!importSettingDto.IsDataImported)
                    {
                        aOperationCallResult = SaveDraftTableImportSetting(dataSetExDto);
                    }
                    else
                    {
                        foreach (string tableName in importSettingDto.DictTableNameColumnNameAndSourceColumnNameMapping.Keys)
                        {
                            var tableDto = importSettingDto.DictTableNameAndDto[tableName];

                            if (tableDto != null)
                            {
                                foreach (string transFieldColumnName in importSettingDto.DictTableNameColumnNameAndSourceColumnNameMapping[tableName].Keys)
                                {
                                    string srcColumnName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(importSettingDto.DictTableNameColumnNameAndSourceColumnNameMapping[tableName][transFieldColumnName]);

                                    if (!string.IsNullOrWhiteSpace(srcColumnName))
                                    {
                                        var transFiledColumnDto = tableDto.Columns.FirstOrDefault(o => o.Name.ToLower() == transFieldColumnName.ToLower());
                                        var sourceColumnDto = importSettingDto.SourceColumns.FirstOrDefault(o => o.Name.ToLower() == srcColumnName.ToLower());

                                        if (transFiledColumnDto != null && sourceColumnDto != null)
                                        {
                                            sourceColumnDto.Tag = transFiledColumnDto.Tag.ToString();
                                        }
                                    }
                                }
                            }
                        }

                        string query = ChangeTempTableColumnDataType(importSettingDto, aValidationResult);

                        if (importSettingDto.NeedToDropTempTableNames != null && importSettingDto.NeedToDropTempTableNames.Count > 0)
                        {
                            importSettingDto.NeedToDropTempTableNames.ForAll(o => DeleteTempTable(importSettingDto.DataSourceRegisterId, o, aValidationResult));
                        }

                        if (aValidationResult.HasErrors)
                        {
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Query", ValidationItemType.Message, "\nQuery: \n" + query + "\n"));

                        }
                    }
                }



                //aValidationResult.Merge(ProcessTransactionTableDataImport(importSettingDto));

                if (!aValidationResult.HasErrors)
                {

                }
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_CreateTableImportSettingAndProcessImport_Error", ValidationItemType.Error, "Change Import Setting Failed."));
            }


            return aOperationCallResult;

        }

        public static OperationCallResult<bool> UpdateImportedTableDataFromExcelFilePath(int importSettingDataSetId, string filePath, string ftpUserName = "", string ftpPassword = "")
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;



            if (!string.IsNullOrWhiteSpace(filePath))
            {
                try
                {
                    byte[] buffer = null;
                    string fileName = "";

                    if (filePath.ToLower().StartsWith("http"))
                    {
                        string errorMsg = "";
                        buffer = AppEsiteFileBL.DownloadFileByUrl(filePath, out errorMsg);
                        fileName = Path.GetFileName(filePath);

                        if (fileName.Length > 50)
                        {
                            fileName = fileName.Substring(0, 40) + "_" + Guid.NewGuid().ToString();
                        }

                        if (!string.IsNullOrWhiteSpace(errorMsg))
                        {
                            errorMsg = "Invalid import file url. " + filePath;
                            aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Condition pass failure", ValidationItemType.Error, errorMsg));
                        }
                    }
                    else if (filePath.ToLower().StartsWith("ftp"))
                    {
                        FtpTools ftpInstance = new FtpTools("", ftpUserName, ftpPassword);
                        string errorMsg = "";
                        buffer = ftpInstance.GetFtpFileBinaryData(filePath, out errorMsg);
                        fileName = Path.GetFileName(filePath);

                        if (!string.IsNullOrWhiteSpace(errorMsg))
                        {
                            errorMsg = "Cannot Read File Content: " + filePath + "\n" + errorMsg;
                            aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Condition pass failure", ValidationItemType.Error, errorMsg));
                        }
                    }
                    else
                    {
                        buffer = StreamHelper.FileToByteArray(filePath);
                        fileName = Path.GetFileName(filePath);
                    }

                    if (buffer != null)
                    {

                        DatabaseTableImportSettingDto importSettingDto = RetrieveOneTableImportSettingDto(importSettingDataSetId);

                        if (importSettingDto.Status.HasValue && importSettingDto.Status == (int)EmAppImportStatus.Released)
                        {
                            string uploadedFileTempTableName = ExcelImportExportBL.ImportExcelContentToDbTable(buffer, fileName, importSettingDataSetId, importSettingDto.DataSourceRegisterId);

                            if (!string.IsNullOrWhiteSpace(uploadedFileTempTableName))
                            {
                                return UpdateImportedTableDataFromTempTable(importSettingDataSetId, uploadedFileTempTableName, filePath);
                            }
                        }
                        else
                        {
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_UpdateStagingTableDataFromExcelUpload_Error", ValidationItemType.Error,
                                "The import setting (Id:" + importSettingDataSetId + ") has not been released yet."));
                        }

                        aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_IntergrationSettingEntity_Save_OK", ValidationItemType.Message, "Execution Completed"));
                    }
                }
                catch (Exception ex)
                {
                    aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Condition pass failure", ValidationItemType.Error, ex.ToString()));
                }
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingEntity), "App_UpdateStagingTableDataFromExcelUpload_Error", ValidationItemType.Error,
                   "Cannot read correct Excel data."));
            }


            return aOperationCallResult;
        }


        public static OperationCallResult<bool> UpdateImportedTableDataFromTempTable(int importSettingDataSetId, string uploadedFileTempTableName, string fileName)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            DatabaseTableImportSettingDto importSettingDto = RetrieveOneTableImportSettingDto(importSettingDataSetId);
            importSettingDto.IsUpdateImportedTableDataFromTempTable = true;
            importSettingDto.NeedToDropTempTableNames = new List<string>();
            importSettingDto.CurrentImportFileName = fileName;

            //importSettingDto.NeedToDropTempTableNames.Add(uploadedFileTempTableName);

            if (importSettingDto != null)
            {
                string tempTableName = importSettingDto.TempTableName = importSettingDto.OrgTempTableName = uploadedFileTempTableName;


                if (importSettingDto.IsEntityPureManyToManyRelationshipImport)
                {
                    ProcessUpdateEntityPureManyToManyRelationshipTable(importSettingDto, aValidationResult);

                }
                else
                {
                    //ChangeTempTableColumnDataType(importSettingDto, aValidationResult);

                    if (aValidationResult.HasErrors)
                    {
                        //importSettingDto.NeedToDropTempTableNames.ForAll(o => DeleteTempTable(importSettingDto.DataSourceRegisterId, o, aValidationResult));
                        return aOperationCallResult;
                    }


                    string query = "";

                    if (importSettingDto.IsEntityImport)
                    {
                        query += ProcessEntityImport_GenerateTableDataUpdateScript(importSettingDto, aValidationResult) + Environment.NewLine;
                    }
                    else
                    {
                        query += ProcessTableImport_GenerateTableDataUpdateScript(importSettingDto, aValidationResult) + Environment.NewLine;
                    }




                    DatabaseFixture databaseFixtureInstance = AppMetaDataBL.GetNewInstanceDatbaseFixtureWtihSchmaOwner(importSettingDto.DataSourceRegisterId, null);

                    string errorMsg = AppMetaDataBL.ExecSQlCommand(databaseFixtureInstance, query);

                    if (!string.IsNullOrWhiteSpace(errorMsg))
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error, "Import Failed. " + errorMsg));
                    }
                    else
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_UpdateImportedTableDataFromTempTable_Ok", ValidationItemType.Message, "Update Table(s) From Excel Success."));

                        //aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Query", ValidationItemType.Message, "\nQuery: \n" + query + "\n"));
                    }


                }
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_UpdateImportedTableDataFromTempTable_Error", ValidationItemType.Error, "Cannot find import setting."));
            }



            importSettingDto.NeedToDropTempTableNames.ForAll(o => DeleteTempTable(importSettingDto.DataSourceRegisterId, o, aValidationResult));

            return aOperationCallResult;
        }

        public static OperationCallResult<AppDataSetExDto> CreateEntityImportSettingAndProcessImport(AppDataSetExDto aAppDataSetExDto)
        {


            OperationCallResult<AppDataSetExDto> aOperationCallResult = new OperationCallResult<AppDataSetExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            aValidationResult.Merge(validateNewEntityImportSettingDto(aAppDataSetExDto));

            if (aValidationResult.HasErrors)
            {
                return aOperationCallResult;
            }

            if (aAppDataSetExDto.OtherSettingsDto != null && aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto != null)
            {
                var importSettingDto = aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto;

                importSettingDto.IsEntityImport = true;

                if (string.IsNullOrWhiteSpace(importSettingDto.OrgTempTableName))
                {
                    importSettingDto.OrgTempTableName = importSettingDto.TempTableName;
                }

                aValidationResult.Merge(ProcessTempTableToTargetTableImport(importSettingDto));

                if (importSettingDto.NeedToDropTempTableNames != null && importSettingDto.NeedToDropTempTableNames.Count > 0)
                {
                    importSettingDto.NeedToDropTempTableNames.ForAll(o => DeleteTempTable(importSettingDto.DataSourceRegisterId, o, aValidationResult));
                }

                if (!aValidationResult.HasErrors)
                {
                    if (aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo == null)
                    {
                        aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo = new DatabaseViewDto();
                        aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo.IsErDiagram = true;
                        aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo.DataSourceRegisterId = aAppDataSetExDto.DataSourceFrom;

                        aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo.DictTables = new Dictionary<string, DatabaseViewTableDto>(StringComparer.OrdinalIgnoreCase);
                    }

                    var diagramDto = aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo;

                    ViewTableAddRemoveDto viewTableAddDto = new ViewTableAddRemoveDto(diagramDto);

                    viewTableAddDto.NeedToAddOwnerTablePairList = new List<KeyValuePair<string, string>>();

                    foreach (var tableDto in importSettingDto.Tables)
                    {
                        AppCacheManagerBL.RefreshOneTableCache(tableDto.Name, aAppDataSetExDto.DataSourceFrom, tableDto.SchemaOwner);
                        viewTableAddDto.NeedToAddOwnerTablePairList.Add(new KeyValuePair<string, string>(tableDto.SchemaOwner, tableDto.Name));
                    }

                    diagramDto = aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo = AppDatabaseViewBL.AddTablesToDatabaseView(viewTableAddDto);

                    //ResetDiagramTablePositionByLevel(importSettingDto, diagramDto);

                    var saveSettingResult = AppDatabaseErDiagramBL.SaveOneErDiagramExDto(aAppDataSetExDto);

                    if (saveSettingResult.IsSuccessfulWithResult)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_CreateTableImportSettingAndProcessImport_Ok", ValidationItemType.Message, "Save Import Setting Success."));




                        // Create Entities
                        aAppDataSetExDto = saveSettingResult.Object;
                        GenerateEntitiesFromImportSetting(aAppDataSetExDto, aValidationResult, importSettingDto);

                        if (!aValidationResult.HasErrors)
                        {
                            if (importSettingDto.IsNeedToCreateImportApi)
                            {
                                CreateImportApiFromSetting(aAppDataSetExDto, aValidationResult, importSettingDto);



                            }


                            aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto = importSettingDto;
                            aAppDataSetExDto.IsModified = true;

                            foreach (var tableDto in importSettingDto.Tables)
                            {
                                tableDto.IsNewTable = false;
                                tableDto.DictNewColumnNameAndDto = null;
                            }

                            AppDatabaseErDiagramBL.SaveOneErDiagramExDto(aAppDataSetExDto);

                        }

                    }
                    else
                    {
                        aValidationResult.Merge(saveSettingResult.ValidationResult);
                    }
                }
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_CreateTableImportSettingAndProcessImport_Error", ValidationItemType.Error, "Import Failed."));
            }



            return aOperationCallResult;
        }



        private static void PorcessOneDataSourceRow(ValidationResult aValidationResult,
            DatabaseFixture targetFixture,
            DatabaseTable targetDatabaseTable,
            Dictionary<string, string> dictTargetColumnNameAndSourceColumnName,
            List<string> srcLogicalkeyColumn,
            Dictionary<string, DataRow> dictTargetLogicalKeyDataRow,
            DbConnection connection,
            DataRow srcDataRow,
            List<string> processedSourceRowKeyList,
            Dictionary<string, Dictionary<string, string>> dictTargetColumnName_DictOrgKeyAndNewKey
            //,Dictionary<string, object> dictPkAndValue
            )
        {

            List<DatabaseTable> childTables = new List<DatabaseTable>();



            string sourcKeyValue = "";
            foreach (string keyColumn in srcLogicalkeyColumn)
            {
                sourcKeyValue = sourcKeyValue + srcDataRow[keyColumn].ToString().ToLower() + "_";
            }

            if (!processedSourceRowKeyList.Contains(sourcKeyValue))
            {
                if (!string.IsNullOrEmpty(sourcKeyValue))
                {
                    processedSourceRowKeyList.Add(sourcKeyValue);
                }


                AppSqlCmdDto sqlCmDto = new AppSqlCmdDto();
                SqlWriter sqlWriter = new SqlWriter(targetDatabaseTable, targetFixture.SqlServerType.Value);
                List<DbParameter> sqlParamters = new List<DbParameter>();

                string sqlText = "";

                if (dictTargetLogicalKeyDataRow.ContainsKey(sourcKeyValue))
                {
                    sqlText = sqlWriter.UpdateWithConcurrencySql();

                    var pkDataRow = dictTargetLogicalKeyDataRow[sourcKeyValue];

                    foreach (string column in sqlWriter.PrimaryKeys)
                    {
                        object value = (object)(pkDataRow[column]);

                        DbParameter parameter = targetFixture.CreateParameter(column.Replace(" ", ""));
                        AppDbHelerBL.SetDbParamterValueAndType(targetDatabaseTable, column, value, parameter, targetFixture);

                        sqlParamters.Add(parameter);

                        // dictPkAndValue.Add(column, value);
                    }
                }
                else // it is new row
                {
                    sqlText = sqlWriter.InsertSqlWithoutOutputParameter();

                    //// To do: need to assign dictPkAndValue
                }

                AssignSourValueToTargetColumn(targetFixture, targetDatabaseTable, dictTargetColumnNameAndSourceColumnName, srcDataRow, sqlParamters, dictTargetColumnName_DictOrgKeyAndNewKey);

                sqlCmDto.CmdText = sqlText;
                sqlCmDto.ListParamters = sqlParamters;

                object queryResult;

                DbTransaction trans = null;
                try
                {
                    using (trans = connection.BeginTransaction())
                    {
                        queryResult = targetFixture.ExecuteTransScalar(sqlCmDto.CmdText, sqlCmDto.ListParamters, trans);
                        trans.Commit();

                    }

                }
                catch (Exception ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Save_Failed", ValidationItemType.Error, ex.Message));
                    trans.Rollback();
                }
            }


        }

        private static void AssignSourValueToTargetColumn(DatabaseFixture targetFixture, DatabaseTable targetDatabaseTable, Dictionary<string, string> dictTargetColumnNameAndSourceColumnName,
            DataRow dataRow, List<DbParameter> sqlParamters, Dictionary<string, Dictionary<string, string>> dictTargetColumnName_DictOrgKeyAndNewKey)
        {
            string[] needToinsetColumns = targetDatabaseTable.GetColumnsExcludeAutoTimeStampAndComputed();
            foreach (var columnName in needToinsetColumns)
            {
                // need to get default value from transaction units
                object value = DBNull.Value;

                if (dictTargetColumnNameAndSourceColumnName.ContainsKey(columnName))
                {
                    string srcColumnName = dictTargetColumnNameAndSourceColumnName[columnName];
                    if (!string.IsNullOrWhiteSpace(srcColumnName))
                    {
                        value = (object)(dataRow[srcColumnName]) ?? DBNull.Value;
                    }
                }

                // need to set default vale
                if (value == DBNull.Value)
                {

                }
                else
                {
                    value = GetUpdatedValueFromFkTableMapping(dictTargetColumnName_DictOrgKeyAndNewKey, columnName, value);
                }



                DbParameter parameter = targetFixture.CreateParameter(columnName.Replace(" ", ""));

                if (parameter.DbType == DbType.String)
                {
                    //if (value != DBNull.Value)
                    //{
                    //    value = value.ToString();
                    //}

                    if (value is System.UInt16)
                    {
                        value = value.ToString();
                    }
                    else if (value is System.UInt32)
                    {

                        value = value.ToString();
                    }
                    else if (value is System.UInt64)
                    {

                        value = value.ToString();
                    }
                }






                AppDbHelerBL.SetDbParamterValueAndType(targetDatabaseTable, columnName, value, parameter, targetFixture);

                sqlParamters.Add(parameter);
            }
        }

        private static object GetUpdatedValueFromFkTableMapping(Dictionary<string, Dictionary<string, string>> dictTargetColumnName_DictOrgKeyAndNewKey, string columnName, object value)
        {
            if (dictTargetColumnName_DictOrgKeyAndNewKey != null)
            {
                if (dictTargetColumnName_DictOrgKeyAndNewKey.ContainsKey(columnName))
                {
                    Dictionary<string, string> dictOrgKeyAndNewKey = dictTargetColumnName_DictOrgKeyAndNewKey[columnName];

                    string orgKey = value.ToString();

                    if (dictOrgKeyAndNewKey != null && dictOrgKeyAndNewKey.ContainsKey(orgKey))
                    {
                        value = dictOrgKeyAndNewKey[orgKey];
                    }
                }
            }

            return value;
        }

        private static void PrepareUpdateByFkTableMappingDictionary(DatabaseFixture targetFixture, DatabaseTableInfoDto targerTableInfoDto, out Dictionary<string, Dictionary<string, string>> dictTargetColumnName_DictOrgKeyAndNewKey)
        {
            dictTargetColumnName_DictOrgKeyAndNewKey = new Dictionary<string, Dictionary<string, string>>();

            if (targerTableInfoDto.DictColumnNameAndUpdateMappingDto != null)
            {
                foreach (DatabaseColumn targetColumn in targerTableInfoDto.Columns)
                {
                    if (targerTableInfoDto.DictColumnNameAndUpdateMappingDto.ContainsKey(targetColumn.Name))
                    {
                        UpdateByFkTableMappingDto fkMappingDto = targerTableInfoDto.DictColumnNameAndUpdateMappingDto[targetColumn.Name];

                        DataTable fkMappingDataTable = ProcessDbToDbTableImport_PopulateOneFkMappingDataTable(targetFixture, fkMappingDto);

                        if (fkMappingDataTable != null && fkMappingDataTable.Rows.Count > 0)
                        {
                            Dictionary<string, string> dictOrgKeyAndNewKey = new Dictionary<string, string>();
                            dictTargetColumnName_DictOrgKeyAndNewKey.Add(targetColumn.Name, dictOrgKeyAndNewKey);

                            foreach (DataRow dataRow in fkMappingDataTable.Rows)
                            {
                                string orgKey = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dataRow["OrgKey"]);
                                string newKey = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dataRow["NewKey"]);

                                if (!string.IsNullOrWhiteSpace(orgKey) && !string.IsNullOrWhiteSpace(newKey))
                                {
                                    if (!dictOrgKeyAndNewKey.ContainsKey(orgKey))
                                    {
                                        dictOrgKeyAndNewKey.Add(orgKey, newKey);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void PrepareMappingColumn(
            DatabaseFixture targetFixture,
            DatabaseTableInfoDto targerTableInfoDto,
            out Dictionary<string, string> dictTargetColumnNameAndSourceColumnName,
            out List<string> srcLogicalkeyColumn,
            out Dictionary<string, DataRow> dictTargetLogicalKeyDataRow,
            DatabaseTableInfoDto targerParentTableInfoDto)
        {


            List<string> targetlogicalKeys;

            DataTable targetKeyTable = PrepareTargetKeyDataTable(targetFixture, targerTableInfoDto, out dictTargetColumnNameAndSourceColumnName, out srcLogicalkeyColumn, out targetlogicalKeys, targerParentTableInfoDto);



            dictTargetLogicalKeyDataRow = new Dictionary<string, DataRow>();
            foreach (DataRow targetKeyRow in targetKeyTable.Rows)
            {
                string keyValue = "";
                foreach (string keyColumn in targetlogicalKeys)
                {
                    keyValue = keyValue + targetKeyRow[keyColumn].ToString().ToLower() + "_";
                }

                if (!string.IsNullOrEmpty(keyValue))
                {
                    dictTargetLogicalKeyDataRow[keyValue] = targetKeyRow;
                }

            }
        }



        private static void SyncPkToSrcDataTableMappingToPkColumns(
            DataTable srcDataTable,
            DatabaseFixture targetFixture,
            DatabaseTableInfoDto targerTableInfoDto,
            out Dictionary<string, string> dictTargetColumnNameAndSourceColumnName,
            out List<string> srcLogicalkeyColumn,
            out Dictionary<string, DataRow> dictTargetLogicalKeyDataRow,
            DatabaseTableInfoDto targerParentTableInfoDto)
        {

            PrepareMappingColumn(targetFixture, targerTableInfoDto, out dictTargetColumnNameAndSourceColumnName, out srcLogicalkeyColumn, out dictTargetLogicalKeyDataRow, targerParentTableInfoDto);

            Dictionary<string, string> dictPkNameAndSrcDataTableColumnName = GetDictDatabaseTablePkNameAndSourceDataTableColumnName(targerTableInfoDto, targetFixture);

            // Add Datatable Column Mapping to DB Table Pk Column
            foreach (var kvPair in dictPkNameAndSrcDataTableColumnName)
            {
                srcDataTable.Columns.Add(kvPair.Value, typeof(string));
            }

            foreach (System.Data.DataRow srcDataRow in srcDataTable.Rows)
            {
                string sourcKeyValue = "";
                foreach (string keyColumn in srcLogicalkeyColumn)
                {
                    sourcKeyValue = sourcKeyValue + srcDataRow[keyColumn].ToString().ToLower() + "_";
                }

                if (dictTargetLogicalKeyDataRow.ContainsKey(sourcKeyValue))
                {
                    var pkDataRow = dictTargetLogicalKeyDataRow[sourcKeyValue];

                    foreach (var kvPair in dictPkNameAndSrcDataTableColumnName)
                    {
                        object pkValue = (object)(pkDataRow[kvPair.Key]);
                        srcDataRow[kvPair.Value] = pkValue;
                    }
                }
            }
        }

        private static Dictionary<string, string> GetDictDatabaseTablePkNameAndSourceDataTableColumnName(DatabaseTableInfoDto targerTableInfoDto, DatabaseFixture targetFixture)
        {
            Dictionary<string, string> dictPkNameAndSrcDataTableColumnName = new Dictionary<string, string>();
            SqlWriter sqlWriter = new SqlWriter(targerTableInfoDto, targetFixture.SqlServerType.Value);
            List<string> targetPkKeys = sqlWriter.PrimaryKeys.ToList();

            foreach (string pkColumnName in targetPkKeys)
            {
                dictPkNameAndSrcDataTableColumnName.Add(pkColumnName, "PK_" + targerTableInfoDto.Name + "_" + pkColumnName);
            }

            return dictPkNameAndSrcDataTableColumnName;
        }

        private static DataTable PrepareTargetKeyDataTable(
            DatabaseFixture targetFixture,
            DatabaseTableInfoDto targerTableInfoDto,
            out Dictionary<string, string> dictTargetColumnNameAndSourceColumnName,
            out List<string> srcLogicalkeyColumn,
            out List<string> targetlogicalKeys,
            DatabaseTableInfoDto targerParentTableInfoDto)
        {
            SqlWriter sqlWriter = new SqlWriter(targerTableInfoDto, targetFixture.SqlServerType.Value);

            dictTargetColumnNameAndSourceColumnName = new Dictionary<string, string>();


            Dictionary<string, string> dictTargetParentPkAndSrcFkColumnName = new Dictionary<string, string>();

            if (targerParentTableInfoDto != null)
            {
                dictTargetParentPkAndSrcFkColumnName = GetDictDatabaseTablePkNameAndSourceDataTableColumnName(targerParentTableInfoDto, targetFixture);
            }

            foreach (DatabaseColumn targetColumn in targerTableInfoDto.Columns)
            {
                string parentPkName = targetColumn.LinkToParentTablePkColumnName;
                if (string.IsNullOrWhiteSpace(parentPkName))
                {
                    // 1. Regular Mapping: Target Column Mapping To Source DataTable Column

                    if (!string.IsNullOrWhiteSpace(targetColumn.MapToSourceColumnName))
                    {
                        dictTargetColumnNameAndSourceColumnName.Add(targetColumn.Name, targetColumn.MapToSourceColumnName);
                    }

                }
                else
                {
                    // 2. FK Mapping: Target Column Mapping To Parent Table PK

                    if (dictTargetParentPkAndSrcFkColumnName.ContainsKey(parentPkName))
                    {
                        string srcFkColumnName = dictTargetParentPkAndSrcFkColumnName[parentPkName];
                        dictTargetColumnNameAndSourceColumnName.Add(targetColumn.Name, srcFkColumnName);
                    }
                }
            }


            targetlogicalKeys = targerTableInfoDto.Columns.Where(O => O.IsLogicKey).Select(o => o.Name).ToList();



            List<string> targetPkKeys = sqlWriter.PrimaryKeys.ToList();

            Dictionary<string, string> dcitTargetColumnAndFkColumn =
                targerTableInfoDto
                .Columns.Where(O => !string.IsNullOrWhiteSpace(O.LinkToParentTablePkColumnName))
                .ToDictionary(o => o.Name, o => o.LinkToParentTablePkColumnName);

            srcLogicalkeyColumn = new List<string>();

            foreach (string targetKey in targetlogicalKeys)
            {
                string sourceMappingkeyname = dictTargetColumnNameAndSourceColumnName[targetKey];
                srcLogicalkeyColumn.Add(sourceMappingkeyname);
            }

            // get targtKey Valeu 
            List<string> selectCos = new List<string>();
            selectCos.AddRange(targetPkKeys);
            selectCos.AddRange(targetlogicalKeys);
            selectCos.AddRange(dcitTargetColumnAndFkColumn.Keys);

            string selectTartetSelectColumn = selectCos.Distinct().Select(o => "[" + o + "]").Aggregate((i, j) => i + "," + j);

            string selectTargetkeyValeu = $@" select {selectTartetSelectColumn} from  {targerTableInfoDto.Name}";

            DataTable targetKeyTable = targetFixture.RetriveDataTable(selectTargetkeyValeu, new List<DbParameter>());

            return targetKeyTable;
        }

        public static OperationCallResult<int?> CreateDataUpdateApiFromImportDataSetId(int? importDataSetId)
        {
            OperationCallResult<int?> aOperationCallResult = new OperationCallResult<int?>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppDataSetExDto dataSetExDto = AppDatabaseErDiagramBL.RetrieveOneErDiagramExDto(importDataSetId);

            if (dataSetExDto.OtherSettingsDto != null && dataSetExDto.OtherSettingsDto.TableImportSettingDto != null)
            {
                DatabaseTableImportSettingDto importSettingDto = dataSetExDto.OtherSettingsDto.TableImportSettingDto;

                if (!importSettingDto.DefaultUpdateApiId.HasValue)
                {
                    CreateImportApiFromSetting(dataSetExDto, aValidationResult, importSettingDto);

                    if (importSettingDto.DefaultUpdateApiId.HasValue)
                    {
                        dataSetExDto.OtherSettingsDto.TableImportSettingDto = importSettingDto;
                        dataSetExDto.IsModified = true;
                        var saveResult = AppDatabaseErDiagramBL.SaveOneErDiagramExDto(dataSetExDto);

                        if (saveResult.IsSuccessfulWithResult)
                        {
                            aOperationCallResult.Object = importSettingDto.DefaultUpdateApiId.Value;

                            aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_CreateImportApi_Ok", ValidationItemType.Message, "Create Data Update API Success."));

                        }
                        else
                        {
                            aValidationResult.Merge(saveResult.ValidationResult);
                        }
                    }
                    else
                    {

                    }

                }
                else
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_CreateImportApi_Error", ValidationItemType.Error, "Creating API failed. API already exists."));
                }
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<bool> SetupEntityManyToManyRelationshipTableImport(DatabaseTableImportSettingDto importSettingDto)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (importSettingDto != null && importSettingDto.IsEntityPureManyToManyRelationshipImport
                // && !string.IsNullOrEmpty(importSettingDto.TempTableName)
                && importSettingDto.ParentEntityId.HasValue && importSettingDto.ChildEntityId.HasValue)
            {
                if (string.IsNullOrWhiteSpace(importSettingDto.OrgTempTableName))
                {
                    importSettingDto.OrgTempTableName = importSettingDto.TempTableName;
                }

                ProcessUpdateEntityPureManyToManyRelationshipTable(importSettingDto, aValidationResult);

                if (!aValidationResult.HasErrors)
                {
                    AppDataSetExDto dataSetDto = new AppDataSetExDto();
                    dataSetDto.Name = "Import: " + importSettingDto.RelationShipTableName;
                    dataSetDto.UsageTypeId = (int)EmAppDataSetUsageType.ExcelEntityImportSetting;
                    dataSetDto.OtherSettingsDto = new AppDataSetOtherSettingsDto();



                    dataSetDto.OtherSettingsDto.DatabaseDiagramInfo = new DatabaseViewDto()
                    {
                        IsErDiagram = true,
                        DictAllColumns = new Dictionary<string, Dictionary<string, bool>>(),
                        DictTables = new Dictionary<string, DatabaseViewTableDto>(StringComparer.OrdinalIgnoreCase),
                        Joins = new List<DatabaseViewJoinDto>(),
                        SelectedColumnsList = new List<DatabaseViewColumnDto>(),
                        WhereConditionFilterColumns = new List<DatabaseViewColumnDto>(),
                        QueryString = "",
                        DataSourceRegisterId = importSettingDto.DataSourceRegisterId
                    };

                    dataSetDto.DataSourceFrom = importSettingDto.DataSourceRegisterId;
                    dataSetDto.SaasApplicationId = importSettingDto.SaasApplicationId;

                    dataSetDto.OtherSettingsDto.TableImportSettingDto = importSettingDto;

                    var saveSettingResult = AppDatabaseErDiagramBL.SaveOneErDiagramExDto(dataSetDto);

                    if (saveSettingResult.IsSuccessfulWithResult)
                    {
                        dataSetDto = saveSettingResult.Object;
                        importSettingDto = dataSetDto.OtherSettingsDto.TableImportSettingDto;

                        if (importSettingDto.IsNeedToCreateImportApi)
                        {
                            CreateImportApiFromSetting(dataSetDto, aValidationResult, importSettingDto);

                            if (importSettingDto.DefaultTransactionId.HasValue || importSettingDto.DefaultUpdateApiId.HasValue)
                            {
                                dataSetDto.IsModified = true;
                                AppDatabaseErDiagramBL.SaveOneErDiagramExDto(dataSetDto);
                            }
                        }
                    }


                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_UpdateImportedTableDataFromTempTable_Ok", ValidationItemType.Message, "Setup Entity Pure Many-To-Many Relationship Success."));

                    //aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Query", ValidationItemType.Message, "\nQuery: \n" + query + "\n"));
                }
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_UpdateImportedTableDataFromTempTable_Error", ValidationItemType.Error, "Invalid import data."));
            }

            return aOperationCallResult;
        }

        public static void ProcessUpdateEntityPureManyToManyRelationshipTable(DatabaseTableImportSettingDto importSettingDto, ValidationResult aValidationResult)
        {
            importSettingDto.IsEntityPureManyToManyRelationshipImport = true;
            importSettingDto.IsEntityImport = true;
            //importSettingDto.OrgSourceColumns = importSettingDto.SourceColumns;

            string tempTableName = importSettingDto.TempTableName;


            AppEntityInfoExDto parentEntityDto = AppEntityInfoBL.RetrieveOneAppEntityInfoExDto(importSettingDto.ParentEntityId.Value);
            var childEntityDto = AppEntityInfoBL.RetrieveOneAppEntityInfoExDto(importSettingDto.ChildEntityId.Value);

            string parentTableName = parentEntityDto.TableName;
            string parentTableIdColumn = parentEntityDto.IdentityField;

            List<string> parentTableLogicKeyColumnNames = parentEntityDto.OtherSettingsDto.LogicKeyColumnNameList;

            if (parentTableLogicKeyColumnNames == null || parentTableLogicKeyColumnNames.Count == 0)
            {
                parentTableLogicKeyColumnNames = new List<string>() { parentEntityDto.DisplayFiled1 };
            }


            string childTableName = childEntityDto.TableName;
            string childTableIdColumn = childEntityDto.IdentityField;


            List<string> childTableLogicKeyColumnNames = childEntityDto.OtherSettingsDto.LogicKeyColumnNameList;

            if (childTableLogicKeyColumnNames == null || childTableLogicKeyColumnNames.Count == 0)
            {
                childTableLogicKeyColumnNames = new List<string>() { childEntityDto.DisplayFiled1 };
            }

            string relationTableName = "Relation_" + parentTableName + "_And_" + childTableName;

            importSettingDto.RelationShipTableName = relationTableName;
            importSettingDto.DataSourceRegisterId = parentEntityDto.DataSourceFrom;
            importSettingDto.SaasApplicationId = parentEntityDto.SaasApplicationId;

            if (importSettingDto.Tables == null || importSettingDto.Tables.Count == 0)
            {
                InitManyToManyRelationshipTableDto(importSettingDto);
            }

            if (importSettingDto.OrgSourceColumns == null || importSettingDto.OrgSourceColumns.Count == 0)
            {
                InitManyToManyRelationshipSourceColumns(importSettingDto);
            }

            bool isCreatingNewImportSetting = string.IsNullOrWhiteSpace(importSettingDto.TempTableName);

            string query = "";
            var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(importSettingDto.DataSourceRegisterId.Value);

            if (isCreatingNewImportSetting)
            {
                string droptableStatement = new SqlWriter(relationTableName, dataBaseFixture.SqlServerType.Value).DropTableIfExist();

                query += droptableStatement + "; \n";

                query += string.Format(@"
                    CREATE TABLE [{0}] (
	                    [RelationId] [int] IDENTITY(1,1) NOT NULL,
	                    [{1}] nvarchar(4000) NULL,
	                    [{2}] nvarchar(4000) NULL,
                     CONSTRAINT [PK_Relation_{3}_And_a0009_{4}] PRIMARY KEY CLUSTERED 
                    (
	                    [RelationId] ASC
                    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                    ) ON [PRIMARY];
                    ",
                    relationTableName,
                    parentTableIdColumn,
                    childTableIdColumn,
                    parentTableName,
                    childTableName
                    );

            }
            else
            {

                string droptableStatement = new SqlWriter(relationTableName, dataBaseFixture.SqlServerType.Value).DropTableIfExist();

                string createTableStatement = string.Format(@"
                    CREATE TABLE [{0}] (
	                    [RelationId] [int] IDENTITY(1,1) NOT NULL,
	                    [{1}] nvarchar(4000) NULL,
	                    [{2}] nvarchar(4000) NULL,
                     CONSTRAINT [PK_Relation_{3}_And_a0009_{4}] PRIMARY KEY CLUSTERED 
                    (
	                    [RelationId] ASC
                    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                    ) ON [PRIMARY]
                    ",
                    relationTableName,
                    parentTableIdColumn,
                    childTableIdColumn,
                    parentTableName,
                    childTableName
                    );


                query += droptableStatement + "; \n "; ;

                query += createTableStatement + "; \n ";


                bool isParentTableIdColumnExist = dataBaseFixture.IsColumnExist(importSettingDto.TempTableName, parentTableIdColumn);

                if (!isParentTableIdColumnExist)
                {
                    query += "ALTER TABLE [" + importSettingDto.TempTableName + "] ADD [" + parentTableIdColumn + "] nvarchar(4000) null; \n ";
                }

                bool isChildTableIdColumnExist = dataBaseFixture.IsColumnExist(importSettingDto.TempTableName, childTableIdColumn);

                if (!isChildTableIdColumnExist)
                {
                    query += "ALTER TABLE [" + importSettingDto.TempTableName + "] ADD [" + childTableIdColumn + "] nvarchar(4000) null; \n ";
                }

                query += "ALTER TABLE [" + importSettingDto.TempTableName + "] ADD [RelationId] int null ; \n "; ;

                DatabaseTableInfoDto parentTableDto = new DatabaseTableInfoDto()
                {
                    Name = parentTableName
                };

                DatabaseTableInfoDto childTableDto = new DatabaseTableInfoDto()
                {
                    Name = childTableName
                };

                query += PrepareQuery_UpdateTemplateTablePkForOneTable(importSettingDto, parentTableDto, tempTableName, parentTableIdColumn, parentTableLogicKeyColumnNames, "");
                query += PrepareQuery_UpdateTemplateTablePkForOneTable(importSettingDto, childTableDto, tempTableName, childTableIdColumn, childTableLogicKeyColumnNames, "");


                foreach (var tableDto in importSettingDto.Tables)
                {
                    query += Environment.NewLine + GenerateUpdateTableDataScript_ProcessOneTable(importSettingDto, tableDto, false);
                }


                //query += string.Format(@"
                //    INSERT INTO [{0}]
                //       ([{1}]
                //       ,[{2}])
                //    ",
                //   relationTableName,
                //   parentTableIdColumn,
                //   childTableIdColumn
                //   );


                //string joinParentTable_conditionExpression = "";
                //foreach (string columnName in parentTableLogicKeyColumnNames)
                //{
                //    if (joinParentTable_conditionExpression.Length > 0)
                //    {
                //        joinParentTable_conditionExpression += " AND ";
                //    }

                //    joinParentTable_conditionExpression += parentTableName + ".[" + columnName + "] = tempTable.[" + columnName + "] ";
                //}

                //string joinChildTable_conditionExpression = "";
                //foreach (string columnName in childTableLogicKeyColumnNames)
                //{
                //    if (joinChildTable_conditionExpression.Length > 0)
                //    {
                //        joinChildTable_conditionExpression += " AND ";
                //    }

                //    joinChildTable_conditionExpression += childTableName + ".[" + columnName + "] = tempTable.[" + columnName + "] ";
                //}

                //query += string.Format(@"
                //    select distinct  {0}.{1}, {2}.{3}
                //    from {4} as tempTable
                //     inner join {5} on ({6})
                //     inner join {7} on ({8});
                //    ",
                //   parentTableName,parentTableIdColumn,childTableName,childTableIdColumn,
                //   tempTableName,
                //   parentTableName, joinParentTable_conditionExpression,
                //   childTableName, joinChildTable_conditionExpression

                //   );


            }

            //DatabaseFixture databaseFixtureInstance = AppMetaDataBL.GetNewInstanceDatbaseFixtureWtihSchmaOwner(importSettingDto.DataSourceRegisterId, null);

            string errorMsg = AppMetaDataBL.ExecSQlCommand(dataBaseFixture, query);

            if (!string.IsNullOrWhiteSpace(errorMsg))
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error, "Import Failed. " + errorMsg));
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_UpdateImportedTableDataFromTempTable_Ok", ValidationItemType.Message, "Import Success."));

                //aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Query", ValidationItemType.Message, "\nQuery: \n" + query + "\n"));
            }
        }


        public static OperationCallResult<AppDataSetExDto> CreateDbToDbTableImportSetting(AppDataSetExDto aAppDataSetExDto)
        {

            OperationCallResult<AppDataSetExDto> aOperationCallResult = new OperationCallResult<AppDataSetExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (aAppDataSetExDto.OtherSettingsDto != null && aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto != null)
            {
                aAppDataSetExDto.Name = "New Db To Db Import";
                aAppDataSetExDto.UsageTypeId = (int)EmAppDataSetUsageType.DbToDbTableImportSetting;


                var importSettingDto = aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto;
                importSettingDto.NeedToDropTempTableNames = new List<string>();

                importSettingDto.Status = (int)EmAppImportStatus.Draft;
                importSettingDto.DataSourceRegisterId = aAppDataSetExDto.DataSourceFrom;
                importSettingDto.IsSpilitToMultipleTables = false;
                importSettingDto.IsNeedToCreateImportApi = false;
                importSettingDto.IsFlatSingleTableImport = false;
                importSettingDto.Tables = new List<DatabaseTableInfoDto>();

                //importSettingDto.TempTableName = $scope.controllerModel.tempTableName;
                //importSettingDto.SourceColumns = $scope.dataModel.tempTable.Columns;
                //importSettingDto.OrgSourceColumns = angular.copy(importSettingDto.SourceColumns);

                aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo = new DatabaseViewDto();
                aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo.IsErDiagram = true;
                aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo.DataSourceRegisterId = aAppDataSetExDto.DataSourceFrom;
                aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo.DictTables = new Dictionary<string, DatabaseViewTableDto>(StringComparer.OrdinalIgnoreCase);

                if (importSettingDto.SourceDataSourceFrom.HasValue
                    && aAppDataSetExDto.DataSourceFrom.HasValue
                    && importSettingDto.SourceDataSourceType.HasValue)
                {
                    var srcFixture = AppCacheManagerBL.GetOneDatabaseFixture(importSettingDto.SourceDataSourceFrom.Value);
                    var targetFixture = AppCacheManagerBL.GetOneDatabaseFixture(aAppDataSetExDto.DataSourceFrom.Value);

                    //DataTable srcDataTable = null;

                    if (importSettingDto.SourceDataSourceType.Value == (int)EmAppDbToDbImportSourceType.DatabaseTable && !string.IsNullOrWhiteSpace(importSettingDto.SourceTableName))
                    {
                        PrepareImportSourceColumnsFromSourceTable(aValidationResult, importSettingDto, srcFixture);
                    }
                    else if (importSettingDto.SourceDataSourceType.Value == (int)EmAppDbToDbImportSourceType.DataSet && importSettingDto.SourceDataSetId.HasValue)
                    {
                        PrepareImportSourceColumnsFromSourceDataSet(aValidationResult, importSettingDto, srcFixture);
                    }

                    if (importSettingDto.SourceColumns != null && importSettingDto.SourceColumns.Count > 0)
                    {
                        aOperationCallResult = SaveDraftTableImportSetting(aAppDataSetExDto);
                    }
                    else
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_CreateDbToDbTableImportSetting_Error", ValidationItemType.Error, "Generate Source Columns Failed."));
                    }
                }
                else
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_CreateDbToDbTableImportSetting_Error", ValidationItemType.Error, "Invalid Import Setting."));
                }
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_CreateDbToDbTableImportSetting_Error", ValidationItemType.Error, "Invalid Import Setting."));
            }


            return aOperationCallResult;
        }

        private static void PrepareImportSourceColumnsFromSourceTable(ValidationResult aValidationResult, DatabaseTableImportSettingDto importSettingDto, DatabaseFixture srcFixture)
        {
            importSettingDto.SourceColumns = new List<DatabaseColumnExDto>();

            if (importSettingDto.SourceDataSourceType.Value == (int)EmAppDbToDbImportSourceType.DatabaseTable && !string.IsNullOrWhiteSpace(importSettingDto.SourceTableName))
            {
                DatabaseTable sourceTableDto = AppMetaDataBL.GetOneDatabaseTableSchema(importSettingDto.SourceTableName, importSettingDto.SourceDataSourceFrom, "");

                foreach (var ortColumn in sourceTableDto.Columns)
                {
                    DatabaseColumnExDto columnExDto = new DatabaseColumnExDto(ortColumn);

                    importSettingDto.SourceColumns.Add(columnExDto);
                }
            }


            importSettingDto.OrgSourceColumns = importSettingDto.SourceColumns.DeepCopy();
        }

        private static void PrepareImportSourceColumnsFromSourceDataSet(ValidationResult aValidationResult, DatabaseTableImportSettingDto importSettingDto, DatabaseFixture srcFixture)
        {
            importSettingDto.SourceColumns = new List<DatabaseColumnExDto>();

            var sourceDataSetDto = AppDataSetBL.RetrieveOneAppDataSetExDto(importSettingDto.SourceDataSetId.Value, true);

            if (sourceDataSetDto.QueryType.HasValue && sourceDataSetDto.QueryType.Value == (int)EmAppDataServiceType.QueryText
               && !string.IsNullOrWhiteSpace(sourceDataSetDto.QueryText))
            {
                DatabaseViewDto diagramDto = AppDatabaseViewBL.ConvertQueryToViewDto(sourceDataSetDto.QueryText, sourceDataSetDto.DataSourceFrom, null);

                if (diagramDto != null)
                {
                    if (diagramDto.DictAllColumns != null && diagramDto.DictTables != null && diagramDto.SelectedColumnsList != null)
                    {
                        Dictionary<string, Dictionary<string, DatabaseColumn>> dictTableAliasAndColumnNameAndColumnDto = new Dictionary<string, Dictionary<string, DatabaseColumn>>();

                        foreach (var kvPair in diagramDto.DictTables)
                        {
                            string tableAliasName = kvPair.Key.ToLower();
                            var simpleTableDto = kvPair.Value;

                            if (!dictTableAliasAndColumnNameAndColumnDto.ContainsKey(tableAliasName))
                            {
                                DatabaseTable tableDto = AppMetaDataBL.GetOneDatabaseTableSchema(simpleTableDto.TableName, importSettingDto.SourceDataSourceFrom, simpleTableDto.SchemaOwner);

                                dictTableAliasAndColumnNameAndColumnDto.Add(tableAliasName, tableDto.Columns.ToDictionary(o => o.Name.ToLower(), o => o));
                            }
                        }

                        foreach (var dataSetColumn in diagramDto.SelectedColumnsList)
                        {
                            if (!string.IsNullOrWhiteSpace(dataSetColumn.UniqTableOrAliasName)
                                && !string.IsNullOrWhiteSpace(dataSetColumn.ColumnName)
                                && !string.IsNullOrWhiteSpace(dataSetColumn.ColumnDisplayName))
                            {
                                if (dictTableAliasAndColumnNameAndColumnDto.ContainsKey(dataSetColumn.UniqTableOrAliasName.ToLower()))
                                {
                                    Dictionary<string, DatabaseColumn> dictColumnNameAndDto = dictTableAliasAndColumnNameAndColumnDto[dataSetColumn.UniqTableOrAliasName.ToLower()];

                                    if (dictColumnNameAndDto.ContainsKey(dataSetColumn.ColumnName.ToLower()))
                                    {
                                        var columnDto = dictColumnNameAndDto[dataSetColumn.ColumnName.ToLower()];


                                        DatabaseColumnExDto columnExDto = new DatabaseColumnExDto(columnDto);
                                        columnExDto.Name = dataSetColumn.ColumnDisplayName;

                                        importSettingDto.SourceColumns.Add(columnExDto);
                                    }
                                }

                            }
                        }
                    }
                }
                else
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_CreateDbToDbTableImportSetting_Error", ValidationItemType.Error, "Invalid Dataset Query."));
                }

            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_CreateDbToDbTableImportSetting_Error", ValidationItemType.Error, "Dataset Query Text Is Empty."));
            }

        }



        //private static void PrepareImportSourceColumnsFromQuery(DatabaseTableImportSettingDto importSettingDto, DatabaseFixture srcFixture, string queryDataSet)
        //{
        //    Dictionary<string, string> dictColumnNameDataType = srcFixture.GetQuerySchemeColumnNameDataType(queryDataSet);

        //    importSettingDto.SourceColumns = new List<DatabaseColumnExDto>();



        //    foreach (var kvPair in dictColumnNameDataType)
        //    {
        //        DatabaseColumnExDto columnExDto = new DatabaseColumnExDto();
        //        columnExDto.Name = kvPair.Key;
        //        columnExDto.Tag = kvPair.Value;

        //        importSettingDto.SourceColumns.Add(columnExDto);

        //    }       


        //    importSettingDto.OrgSourceColumns = importSettingDto.SourceColumns.DeepCopy();
        //}

        private static void InitManyToManyRelationshipTableDto(DatabaseTableImportSettingDto importSettingDto)
        {
            AppEntityInfoExDto parentEntityDto = AppEntityInfoBL.RetrieveOneAppEntityInfoExDto(importSettingDto.ParentEntityId.Value);
            var childEntityDto = AppEntityInfoBL.RetrieveOneAppEntityInfoExDto(importSettingDto.ChildEntityId.Value);

            string parentTableName = parentEntityDto.TableName;
            string parentTableIdColumn = parentEntityDto.IdentityField;
            string parentTableDisplayColumn = parentEntityDto.DisplayFiled1;

            string childTableName = childEntityDto.TableName;
            string childTableIdColumn = childEntityDto.IdentityField;
            string childTableDisplayColumn = childEntityDto.DisplayFiled1;

            string relationTableName = "Relation_" + parentTableName + "_And_" + childTableName;

            var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(importSettingDto.DataSourceRegisterId.Value);
            string schemaOwner = dataBaseFixture.CurrentOwner;

            importSettingDto.Tables = new List<DatabaseTableInfoDto>();

            DatabaseTableInfoDto relationTableDto = new DatabaseTableInfoDto()
            {
                Name = relationTableName,
                SchemaOwner = schemaOwner,
                Tag = relationTableName,
            };

            relationTableDto.Columns.Add(new DatabaseColumn()
            {
                Name = "RelationId",
                DbDataType = "int",
                Tag = "Integer",
                Nullable = false,
                IsPrimaryKey = true,
                IsAutoNumber = true,
                TableName = relationTableName,
            });

            relationTableDto.Columns.Add(new DatabaseColumn()
            {
                Name = parentTableIdColumn,
                DbDataType = "int",
                Tag = "Integer",
                Nullable = true,
                IsAutoNumber = false,
                TableName = relationTableName,
                IsLogicKey = true,
            });

            relationTableDto.Columns.Add(new DatabaseColumn()
            {
                Name = childTableIdColumn,
                DbDataType = "int",
                Tag = "Integer",
                Nullable = true,
                IsAutoNumber = false,
                TableName = relationTableName,
                IsLogicKey = true,
            });

            importSettingDto.Tables.Add(relationTableDto);
        }

        private static void InitManyToManyRelationshipSourceColumns(DatabaseTableImportSettingDto importSettingDto)
        {
            AppEntityInfoExDto parentEntityDto = AppEntityInfoBL.RetrieveOneAppEntityInfoExDto(importSettingDto.ParentEntityId.Value);
            var childEntityDto = AppEntityInfoBL.RetrieveOneAppEntityInfoExDto(importSettingDto.ChildEntityId.Value);

            List<string> parentTableLogicKeyColumnNames = parentEntityDto.OtherSettingsDto.LogicKeyColumnNameList;

            if (parentTableLogicKeyColumnNames == null || parentTableLogicKeyColumnNames.Count == 0)
            {
                parentTableLogicKeyColumnNames = new List<string>() { parentEntityDto.DisplayFiled1 };
            }


            List<string> childTableLogicKeyColumnNames = childEntityDto.OtherSettingsDto.LogicKeyColumnNameList;

            if (childTableLogicKeyColumnNames == null || childTableLogicKeyColumnNames.Count == 0)
            {
                childTableLogicKeyColumnNames = new List<string>() { childEntityDto.DisplayFiled1 };
            }


            importSettingDto.OrgSourceColumns = new List<DatabaseColumnExDto>();

            foreach (string logicKeyColName in parentTableLogicKeyColumnNames)
            {
                importSettingDto.OrgSourceColumns.Add(new DatabaseColumnExDto()
                {
                    Name = logicKeyColName,
                    DbDataType = "nvarchar",
                    Tag = "String",
                    Nullable = true,
                });
            }


            foreach (string logicKeyColName in childTableLogicKeyColumnNames)
            {
                importSettingDto.OrgSourceColumns.Add(new DatabaseColumnExDto()
                {
                    Name = logicKeyColName,
                    DbDataType = "nvarchar",
                    Tag = "String",
                    Nullable = true,
                });
            }

            importSettingDto.SourceColumns = importSettingDto.OrgSourceColumns.DeepCopy();

        }



        //public static OperationCallResult<bool> UpdateImportedEntityTableDataFromTempTable(int importSettingDataSetId, string uploadedFileTempTableName)
        //{

        //    OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
        //    ValidationResult aValidationResult = new ValidationResult();
        //    aOperationCallResult.ValidationResult = aValidationResult;

        //    DatabaseTableImportSettingDto importSettingDto = RetrieveOneTableImportSettingDto(importSettingDataSetId);
        //    importSettingDto.IsEntityImport = true;

        //    if (importSettingDto != null)
        //    {
        //        string tempTableName = importSettingDto.TempTableName = uploadedFileTempTableName;

        //        ChangeTempTableColumnDataType(importSettingDto, aValidationResult);

        //        if (aValidationResult.HasErrors)
        //        {
        //            return aOperationCallResult;
        //        }


        //        string query = "";

        //        if (importSettingDto.IsEntityImport)
        //        {
        //            query += ProcessEntityImport_GenerateTableDataUpdateScript(importSettingDto, aValidationResult) + Environment.NewLine;
        //        }
        //        else
        //        {
        //            query += ProcessTableImport_GenerateTableDataUpdateScript(importSettingDto, aValidationResult) + Environment.NewLine;
        //        }




        //        DatabaseFixture databaseFixtureInstance = AppMetaDataBL.GetNewInstanceDatbaseFixtureWtihSchmaOwner(importSettingDto.DataSourceRegisterId, null);

        //        string errorMsg = AppMetaDataBL.ExecSQlCommand(databaseFixtureInstance, query);

        //        if (!string.IsNullOrWhiteSpace(errorMsg))
        //        {
        //            aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error, "Import Failed. " + errorMsg));
        //        }
        //        else
        //        {
        //            aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_UpdateImportedTableDataFromTempTable_Ok", ValidationItemType.Message, "Update Table(s) From Excel Success."));

        //            aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Query", ValidationItemType.Message, "\nQuery: \n" + query + "\n"));
        //        }
        //    }
        //    else
        //    {
        //        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_UpdateImportedTableDataFromTempTable_Error", ValidationItemType.Error, "Cannot find import setting."));
        //    }

        //    return aOperationCallResult;
        //}



        public static DatabaseTableImportSettingDto RetrieveOneTableImportSettingDto(int importSettingDataSetId)
        {
            AppDataSetExDto dataSetExDto = AppDatabaseErDiagramBL.RetrieveOneErDiagramExDto(importSettingDataSetId);

            if (dataSetExDto.OtherSettingsDto != null && dataSetExDto.OtherSettingsDto.TableImportSettingDto != null)
            {
                return dataSetExDto.OtherSettingsDto.TableImportSettingDto;
            }

            return null;
        }

        private static EntityCollection<AppDataSetEntity> RetrieveAllExcelTableImportSettingEntity()
        {

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppDataSetEntity> list = new EntityCollection<AppDataSetEntity>();

                RelationPredicateBucket filter = new RelationPredicateBucket(AppDataSetFields.UsageTypeId == (int)EmAppDataSetUsageType.ExcelTableImportSetting);

                adapter.FetchEntityCollection(list, filter, 0);
                return list;
            }
        }

        private static EntityCollection<AppDataSetEntity> RetrieveAllDbToDbTableImportSettingEntity()
        {

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppDataSetEntity> list = new EntityCollection<AppDataSetEntity>();

                RelationPredicateBucket filter = new RelationPredicateBucket(AppDataSetFields.UsageTypeId == (int)EmAppDataSetUsageType.DbToDbTableImportSetting);

                adapter.FetchEntityCollection(list, filter, 0);
                return list;
            }
        }

        private static EntityCollection<AppDataSetEntity> RetrieveAllEntityImportSettingEntity()
        {

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppDataSetEntity> list = new EntityCollection<AppDataSetEntity>();

                RelationPredicateBucket filter = new RelationPredicateBucket(AppDataSetFields.UsageTypeId == (int)EmAppDataSetUsageType.ExcelEntityImportSetting);

                adapter.FetchEntityCollection(list, filter, 0);
                return list;
            }
        }


        private static ValidationResult validateTableImportSettingDto(AppDataSetExDto aAppDataSetExDto)
        {
            ValidationResult aValidationResult = aAppDataSetExDto.ValidateDto();

            if (!aValidationResult.HasErrors)
            {
                if (aAppDataSetExDto.OtherSettingsDto != null && aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto != null)
                {
                    var importSettingDto = aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto;

                    if (importSettingDto.Tables.Count == 0)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error,
                                    "Import Failed: Cannot find any table configured to import."));

                        return aValidationResult;
                    }

                    var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(aAppDataSetExDto.DataSourceFrom.Value);
                    string schemaOwner = dataBaseFixture.CurrentOwner;

                    foreach (var tableDto in importSettingDto.Tables)
                    {
                        if (string.IsNullOrWhiteSpace(tableDto.Name))
                        {
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error,
                                "Import Failed: A level " + tableDto.Tag + " table does not have table name."));

                            return aValidationResult;

                        }

                        if (importSettingDto.Tables.FirstOrDefault(o => o != tableDto && o.Name.ToLower() == tableDto.Name.ToLower()) != null)
                        {
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error,
                                   "Import Failed: Duplicated table names [" + tableDto.Name + "] detected."));

                            return aValidationResult;
                        }

                        if (!importSettingDto.NeedToUpdateTransactionId.HasValue)
                        {
                            DatabaseTable existTable = AppCacheManagerBL.GetDatabaseTable(tableDto.Name, aAppDataSetExDto.DataSourceFrom, schemaOwner);

                            if (existTable != null)
                            {
                                if (!importSettingDto.IsDataImported)
                                {
                                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error,
                                        "Import Failed: Table name [" + tableDto.Name + "] already exists in the database."));

                                    return aValidationResult;
                                }

                            }
                            else
                            {
                                tableDto.IsNewTable = true;
                            }
                        }

                        if (tableDto.IsMatrixTable)
                        {
                            int fkMatrixKeyCoumnt = 0;

                            foreach (string parentTableName in tableDto.ForeignMatrixKeyTableNameList)
                            {
                                if (importSettingDto.DictTableNameAndDto.ContainsKey(parentTableName))
                                {
                                    var parentTableDto = importSettingDto.DictTableNameAndDto[parentTableName];

                                    var parentLogicKeyColumns = parentTableDto.Columns.Where(o => o.IsLogicKey).ToList();

                                    if (parentLogicKeyColumns.Count == 1)
                                    {
                                        var parentLogicKeyColumn = parentLogicKeyColumns[0];

                                        int? entityId = ControlTypeValueConverter.ConvertValueToInt(parentLogicKeyColumn.NetName);
                                        if (entityId.HasValue)
                                        {
                                            var matrixFkColumn = parentLogicKeyColumn.DeepCopy();
                                            matrixFkColumn.TableName = tableDto.Name;

                                            var existColumn = tableDto.Columns.FirstOrDefault(o => o.Name == matrixFkColumn.Name);
                                            if (existColumn != null)
                                            {
                                                tableDto.Columns.Remove(existColumn);
                                            }

                                            tableDto.Columns.Add(matrixFkColumn);

                                            fkMatrixKeyCoumnt++;
                                        }
                                        else
                                        {
                                            aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error,
                                            "Import Failed: Matrix Foreign Key Column [" + parentTableDto.Name + "].[" + parentLogicKeyColumn.Name + "] must has an entity."));
                                            return aValidationResult;
                                        }
                                    }
                                    else
                                    {
                                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error,
                                       "Import Failed: Matrix Foreign Key Table [" + parentTableDto.Name + "] must have 1 logic key column."));
                                        return aValidationResult;
                                    }
                                }
                            }

                            if (fkMatrixKeyCoumnt == 0)
                            {
                                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error,
                                    "Import Failed: Matrix Table [" + tableDto.Name + "] does not have any foreign matrix key column."));

                                return aValidationResult;
                            }

                        }
                        else
                        {
                            if (tableDto.Columns.Where(o => !o.IsPrimaryKey && !o.IsForeignKey).Count() == 0)
                            {

                                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error,
                                    "Import Failed: Table [" + tableDto.Name + "] does not have any column."));

                                return aValidationResult;
                            }
                            if (tableDto.Columns.Where(o => o.IsLogicKey).Count() == 0)
                            {
                                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error,
                                    "Import Failed: Table [" + tableDto.Name + "] does not have any logic key column."));

                                return aValidationResult;
                            }

                        }


                    }


                }
                else
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error,
                                "Import Failed: Invalid Import Setting Data."));
                }

            }

            return aValidationResult;
        }

        private static ValidationResult validateNewEntityImportSettingDto(AppDataSetExDto aAppDataSetExDto)
        {
            ValidationResult aValidationResult = aAppDataSetExDto.ValidateDto();

            if (!aValidationResult.HasErrors)
            {
                if (aAppDataSetExDto.OtherSettingsDto != null && aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto != null)
                {
                    var importSettingDto = aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto;

                    if (importSettingDto.Tables.Count == 0)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error,
                                    "Import Failed: Cannot find any table configured to import."));

                        return aValidationResult;
                    }

                    Dictionary<string, AppEntityInfoDto> dictEntityCodeAndDto = new Dictionary<string, AppEntityInfoDto>();
                    AppEntityInfoBL.RetrieveAllAppEntityInfoDto().ForAll(o =>
                    {
                        if (!dictEntityCodeAndDto.ContainsKey(o.EntityCode.ToLower()))
                        {
                            dictEntityCodeAndDto.Add(o.EntityCode.ToLower(), o);
                        }
                    });


                    var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(aAppDataSetExDto.DataSourceFrom.Value);
                    string schemaOwner = dataBaseFixture.CurrentOwner;

                    foreach (var tableDto in importSettingDto.Tables)
                    {
                        if (string.IsNullOrWhiteSpace(tableDto.Name))
                        {
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error,
                                "Import Failed: A level " + tableDto.Tag + " table does not have table name."));

                            return aValidationResult;

                        }

                        if (importSettingDto.Tables.FirstOrDefault(o => o != tableDto && o.Name == tableDto.Name) != null)
                        {
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error,
                                   "Import Failed: Duplicated table names [" + tableDto.Name + "] detected."));

                            return aValidationResult;
                        }

                        DatabaseTable existTable = AppCacheManagerBL.GetDatabaseTable(tableDto.Name, aAppDataSetExDto.DataSourceFrom, schemaOwner);
                        if (existTable != null)
                        {
                            if (!importSettingDto.IsDataImported)
                            {
                                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error,
                                "Import Failed: Entity with table name [" + tableDto.Name + "] already exists in the database."));

                                return aValidationResult;
                            }

                        }
                        else
                        {
                            tableDto.IsNewTable = true;
                        }



                        if (dictEntityCodeAndDto.ContainsKey(tableDto.Tag.ToString().ToLower()))
                        {
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error,
                                    "Import Failed: Entity code [" + tableDto.Tag + "] already exists."));

                            return aValidationResult;
                        }


                        if (tableDto.IsEntityRelationTable)
                        {
                            int fkMatrixKeyCoumnt = 0;

                            foreach (string parentTableName in tableDto.ForeignMatrixKeyTableNameList)
                            {
                                if (importSettingDto.DictTableNameAndDto.ContainsKey(parentTableName))
                                {
                                    var parentTableDto = importSettingDto.DictTableNameAndDto[parentTableName];


                                    string fkName = parentTableDto.Columns.FirstOrDefault(o => o.IsPrimaryKey).Name;

                                    tableDto.AddColumn(new DatabaseColumn()
                                    {
                                        Name = fkName,
                                        DbDataType = "int",
                                        Tag = "Integer",
                                        Nullable = true,
                                        TableName = tableDto.Name,
                                        IsLogicKey = true,
                                        //IsForeignKey = true,
                                        //ForeignKeyTableName = parentTableDto.Name,
                                    });


                                    fkMatrixKeyCoumnt++;
                                }
                            }

                            if (fkMatrixKeyCoumnt == 0)
                            {
                                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error,
                                    "Import Failed: Relationship Table [" + tableDto.Name + "] does not have any foreign key table."));

                                return aValidationResult;
                            }

                        }
                        else
                        {
                            if (tableDto.Columns.Where(o => !o.IsPrimaryKey && !o.IsForeignKey).Count() == 0)
                            {
                                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error,
                                    "Import Failed: Table [" + tableDto.Name + "] does not have any column."));

                                return aValidationResult;

                            }

                            if (tableDto.Columns.Where(o => o.IsLogicKey).Count() == 0)
                            {
                                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error,
                                    "Import Failed: Table [" + tableDto.Name + "] does not have any logic key column."));

                                return aValidationResult;
                            }

                            var lookUpDisplayColumnNames = importSettingDto.DictTableNameAndEntityLookUpDisplayColumnNameList[tableDto.Name];
                            if (!(lookUpDisplayColumnNames != null && lookUpDisplayColumnNames.Count > 0))
                            {
                                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error,
                                    "Import Failed: Table [" + tableDto.Name + "] does not have any look up item display column."));

                                return aValidationResult;
                            }

                        }




                    }


                }
                else
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error,
                                "Import Failed: Invalid Import Setting Data."));
                }

            }

            return aValidationResult;
        }

        //public static Dictionary<string, List<string>> GetImportSettingDictTableNameAndLogicKeyColumnNameList(DatabaseTableImportSettingDto importSettingDto)
        //{
        //    if (importSettingDto.DictTableNameAndLogicKeyColumnNameList == null)
        //    {
        //        Dictionary<string, List<string>> dictTableNameAndLogicKeyColumnNameList = new Dictionary<string, List<string>>();

        //        foreach (var tableDto in importSettingDto.Tables)
        //        {
        //            List<string> uniqueKeyColumnNames = tableDto.Columns.Where(o => o.IsLogicKey).Select(o => o.Name).ToList();
        //            dictTableNameAndLogicKeyColumnNameList.Add(tableDto.Name, uniqueKeyColumnNames);
        //        }

        //        importSettingDto.DictTableNameAndLogicKeyColumnNameList = dictTableNameAndLogicKeyColumnNameList;
        //    }

        //    return importSettingDto.DictTableNameAndLogicKeyColumnNameList;

        //}


        private static void GenerateEntitiesFromImportSetting(AppDataSetExDto aAppDataSetExDto, ValidationResult aValidationResult, DatabaseTableImportSettingDto importSettingDto)
        {
            List<string> saveFailedEntityCodeList = new List<string>(); ;

            foreach (var tableDto in importSettingDto.Tables.Where(o => !o.IsEntityRelationTable && o.Tag != null && !string.IsNullOrWhiteSpace(o.Tag.ToString())))
            {
                AppEntityInfoExDto newEntityInfoDto = new AppEntityInfoExDto();
                newEntityInfoDto.EntityCode = tableDto.Tag.ToString();
                newEntityInfoDto.EntityType = (int)EmAppEntityType.SystemDefineTable;
                newEntityInfoDto.DataSourceFrom = (int)aAppDataSetExDto.DataSourceFrom; ;
                newEntityInfoDto.TableName = tableDto.Name;
                newEntityInfoDto.SchemaOwner = tableDto.SchemaOwner;
                newEntityInfoDto.IdentityField = importSettingDto.DictTableNameAndEntityLookUpIdColumnName[tableDto.Name];
                newEntityInfoDto.SaasApplicationId = aAppDataSetExDto.SaasApplicationId;
                newEntityInfoDto.OtherSettingsDto = new AppEntityInfoOtherSettingsDto();
                newEntityInfoDto.OtherSettingsDto.IdentityColumnDataType = EmAppDataType.Integer.ToString();
                newEntityInfoDto.OtherSettingsDto.LogicKeyColumnNameList = importSettingDto.DictTableNameAndLogicKeyColumnNameList[tableDto.Name];


                var idColumnDto = importSettingDto.DictTableNameAndDto[tableDto.Name].DictDataBaseColumn[newEntityInfoDto.IdentityField];

                newEntityInfoDto.OtherSettingsDto.IdentityColumnDataType = idColumnDto.Tag.ToString();

                int countDisplayField = 0;

                foreach (string columnName in importSettingDto.DictTableNameAndEntityLookUpDisplayColumnNameList[tableDto.Name])
                {
                    countDisplayField++;

                    if (countDisplayField == 1)
                    {
                        newEntityInfoDto.DisplayFiled1 = columnName;
                    }
                    else if (countDisplayField == 2)
                    {
                        newEntityInfoDto.DisplayFiled2 = columnName;
                    }
                    else if (countDisplayField == 3)
                    {
                        newEntityInfoDto.DisplayFiled3 = columnName;
                    }
                    else if (countDisplayField > 3)
                    {
                        break;
                    }
                }

                var saveEntiyResult = AppEntityInfoBL.SaveOneAppEntityInfoDto(newEntityInfoDto);
                if (!saveEntiyResult.IsSuccessful)
                {
                    aValidationResult.Merge(saveEntiyResult.ValidationResult);
                    saveFailedEntityCodeList.Add(tableDto.Tag.ToString());
                }
            }

            if (!aValidationResult.HasErrors)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_CreateTableImportSettingAndProcessImport_Ok", ValidationItemType.Message, "All entities have been created successfully."));

            }
            else
            {
                string failedEntityCodes = string.Join(", ", saveFailedEntityCodeList);
                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_CreateTableImportSettingAndProcessImport_error", ValidationItemType.Error, "Import Failed On The Following Entities: " + failedEntityCodes + "."));
            }
        }




        private static void GenerateDataModelAndFormWithNavigationSearchMenu(AppDataSetExDto aAppDataSetExDto, ValidationResult aValidationResult, DatabaseTableImportSettingDto importSettingDto)
        {
            AppTransactionExDto aAppTransactionExDto = new AppTransactionExDto();
            aAppTransactionExDto.TransactionName = importSettingDto.LevelOneTables[0].Name;
            aAppTransactionExDto.AppTransactionUnitList = new ObservableSet<AppTransactionUnitExDto>();
            aAppTransactionExDto.TransactionOrganizedType = 1;
            aAppTransactionExDto.IsEnableFolderSecurity = true;
            aAppTransactionExDto.EmAppTransBusinessType = (int)EmAppTransBusinessType.FormData;
            aAppTransactionExDto.DataSourceFrom = aAppDataSetExDto.DataSourceFrom;
            aAppTransactionExDto.SaasApplicationId = aAppDataSetExDto.SaasApplicationId;

            aAppTransactionExDto.EmGrandChildEditMode = 1;
            aAppTransactionExDto.IsPhysicalModelTableCreated = true;
            aAppTransactionExDto.IsShowCalculateButton = true;
            aAppTransactionExDto.IsShowSaveButton = true;
            aAppTransactionExDto.OtherOptions = new TransactionOptionDto();
            aAppTransactionExDto.OtherOptions.ErDiagramId = ControlTypeValueConverter.ConvertValueToInt(aAppDataSetExDto.Id);
            aAppTransactionExDto.OtherOptions.ImportSettingId = ControlTypeValueConverter.ConvertValueToInt(aAppDataSetExDto.Id);

            if (importSettingDto.IsDraft)
            {
                aAppTransactionExDto.OtherOptions.IsDraft = true;
            }

            aAppTransactionExDto.DictCurrentPKOrFKLinkToParentKeyGuidMap = new Dictionary<Guid, Guid>();

            AppTransactionUnitExDto rootUnitDto = ConvertOneTableDtoToUnit(aAppTransactionExDto, importSettingDto.LevelOneTables[0], null);
            rootUnitDto.EmGridViewDisplayType = 1;
            rootUnitDto.TreeViewParentKeyField = "1";
            aAppTransactionExDto.AppTransactionUnitList.Add(rootUnitDto);
            rootUnitDto.Children = new List<AppTransactionUnitExDto>();

            foreach (var l2TableDto in importSettingDto.LevelTwoTables)
            {
                AppTransactionUnitExDto childUnitDto = ConvertOneTableDtoToUnit(aAppTransactionExDto, l2TableDto, rootUnitDto);

                foreach (var fkField in childUnitDto.AppTransactionFieldList.Where(o => o.IsLinkToParentPrimaryKey))
                {
                    if (fkField.ParentPKFieldGuid.HasValue && fkField.RowIdentityGuid.HasValue)
                    {
                        aAppTransactionExDto.DictCurrentPKOrFKLinkToParentKeyGuidMap.Add(fkField.RowIdentityGuid.Value, fkField.ParentPKFieldGuid.Value);
                    }
                }


                rootUnitDto.Children.Add(childUnitDto);
                childUnitDto.EmGridViewDisplayType = 1;
                childUnitDto.TreeViewParentKeyField = "1";

                childUnitDto.Children = new List<AppTransactionUnitExDto>();

                foreach (var l3TableDto in importSettingDto.LevelThreeTables.Where(o => o.NetName == l2TableDto.Name))
                {
                    AppTransactionUnitExDto grandchildUnitDto = ConvertOneTableDtoToUnit(aAppTransactionExDto, l3TableDto, childUnitDto);

                    foreach (var fkField in grandchildUnitDto.AppTransactionFieldList.Where(o => o.IsLinkToParentPrimaryKey))
                    {
                        if (fkField.ParentPKFieldGuid.HasValue && fkField.RowIdentityGuid.HasValue)
                        {
                            aAppTransactionExDto.DictCurrentPKOrFKLinkToParentKeyGuidMap.Add(fkField.RowIdentityGuid.Value, fkField.ParentPKFieldGuid.Value);
                        }
                    }

                    grandchildUnitDto.EmGridViewDisplayType = 1;
                    grandchildUnitDto.TreeViewParentKeyField = "1";
                    childUnitDto.Children.Add(grandchildUnitDto);
                }
            }





            var saveTransactionResult = AppTransactionBL.SaveAppTransactionExDto(aAppTransactionExDto);

            if (saveTransactionResult.IsSuccessfulWithResult)
            {


                var savedTransactionExDto = saveTransactionResult.Object;
                int transactionId = (int)savedTransactionExDto.Id;

                importSettingDto.DefaultTransactionId = transactionId;

                List<DatabaseTableInfoDto> matrixTables = importSettingDto.LevelTwoTables.Where(o => o.IsMatrixTable).ToList();

                if (matrixTables.Count > 0)
                {
                    OperationCallResult<AppTransactionExDto> saveTransactionMatrixResult = UpdateMatrixUnitFromImportSetting(importSettingDto, savedTransactionExDto, matrixTables);

                    if (saveTransactionMatrixResult.IsSuccessfulWithResult)
                    {
                        savedTransactionExDto = saveTransactionMatrixResult.Object;

                        if (savedTransactionExDto.CommandActionList == null)
                        {
                            savedTransactionExDto.CommandActionList = new List<AppProjectWorkFlowActionExDto>();
                        }

                        AppProjectWorkFlowActionExDto commandDto = new AppProjectWorkFlowActionExDto();

                        commandDto.ActionFlowOrder = 1;
                        commandDto.ActionGuid = Guid.NewGuid();
                        commandDto.ActionType = (int)EmAppTransactionCommandType.GenerateMatrix;
                        commandDto.Name = "Generate Matrix";
                        commandDto.ActionAttribute = new AppActionAttributeDto();
                        commandDto.ActionAttribute.ChildActionList = new List<ChildTransactionCommandDto>();
                        commandDto.ActionAttribute.LinkToUI = true;
                        commandDto.ActionAttribute.IsShowOnTopMenu = true;

                        savedTransactionExDto.CommandActionList.Add(commandDto);


                        var saveMatrixCommandResult = AppTransactionCommandBL.SaveOneTransactionCommandActionList(savedTransactionExDto);

                        if (!saveMatrixCommandResult.IsSuccessfulWithResult)
                        {
                            aValidationResult.Merge(saveMatrixCommandResult.ValidationResult);
                            return;
                        }
                    }
                    else
                    {
                        aValidationResult.Merge(saveTransactionMatrixResult.ValidationResult);
                        return;
                    }
                }

                var generateFormSearchMenuResult = AppDatabaseViewBL.QuickGenerateTransactionDefaultSeachNavigation(transactionId, importSettingDto, (int)aAppDataSetExDto.Id);

                if (generateFormSearchMenuResult.IsSuccessful)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_CreateTableImportSettingAndProcessImport_Ok", ValidationItemType.Message, "Generate Form Success."));
                }
                else
                {
                    aValidationResult.Merge(saveTransactionResult.ValidationResult);
                }
            }
            else
            {
                aValidationResult.Merge(saveTransactionResult.ValidationResult);
            }
        }


        //private static void GenerateDraftImportSettingPlaceHolderDataModel(AppDataSetExDto aAppDataSetExDto, ValidationResult aValidationResult, DatabaseTableImportSettingDto importSettingDto)
        //{
        //    AppTransactionExDto aAppTransactionExDto = new AppTransactionExDto();
        //    aAppTransactionExDto.TransactionName = importSettingDto.LevelOneTables[0].Name;
        //    aAppTransactionExDto.AppTransactionUnitList = new ObservableSet<AppTransactionUnitExDto>();
        //    aAppTransactionExDto.TransactionOrganizedType = 1;
        //    aAppTransactionExDto.IsEnableFolderSecurity = true;
        //    aAppTransactionExDto.EmAppTransBusinessType = (int)EmAppTransBusinessType.FormData;
        //    aAppTransactionExDto.DataSourceFrom = aAppDataSetExDto.DataSourceFrom;
        //    aAppTransactionExDto.SaasApplicationId = aAppDataSetExDto.SaasApplicationId;

        //    aAppTransactionExDto.EmGrandChildEditMode = 1;
        //    aAppTransactionExDto.IsPhysicalModelTableCreated = false;
        //    aAppTransactionExDto.IsShowCalculateButton = false;
        //    aAppTransactionExDto.IsShowSaveButton = true;
        //    aAppTransactionExDto.OtherOptions = new TransactionOptionDto();
        //    aAppTransactionExDto.OtherOptions.ErDiagramId = ControlTypeValueConverter.ConvertValueToInt(aAppDataSetExDto.Id);
        //    aAppTransactionExDto.OtherOptions.ImportSettingId = ControlTypeValueConverter.ConvertValueToInt(aAppDataSetExDto.Id);

        //    if (importSettingDto.IsDraft)
        //    {
        //        aAppTransactionExDto.OtherOptions.IsDraft = true;
        //    }

        //    aAppTransactionExDto.DictCurrentPKOrFKLinkToParentKeyGuidMap = new Dictionary<Guid, Guid>();

        //    AppTransactionUnitExDto rootUnitDto = ConvertOneTableDtoToUnit(aAppTransactionExDto, importSettingDto.LevelOneTables[0], null);
        //    rootUnitDto.EmGridViewDisplayType = 1;
        //    rootUnitDto.TreeViewParentKeyField = "1";
        //    aAppTransactionExDto.AppTransactionUnitList.Add(rootUnitDto);
        //    rootUnitDto.Children = new List<AppTransactionUnitExDto>();

        //    foreach (var l2TableDto in importSettingDto.LevelTwoTables)
        //    {
        //        AppTransactionUnitExDto childUnitDto = ConvertOneTableDtoToUnit(aAppTransactionExDto, l2TableDto, rootUnitDto);

        //        foreach (var fkField in childUnitDto.AppTransactionFieldList.Where(o => o.IsLinkToParentPrimaryKey))
        //        {
        //            if (fkField.ParentPKFieldGuid.HasValue && fkField.RowIdentityGuid.HasValue)
        //            {
        //                aAppTransactionExDto.DictCurrentPKOrFKLinkToParentKeyGuidMap.Add(fkField.RowIdentityGuid.Value, fkField.ParentPKFieldGuid.Value);
        //            }
        //        }


        //        rootUnitDto.Children.Add(childUnitDto);
        //        childUnitDto.EmGridViewDisplayType = 1;
        //        childUnitDto.TreeViewParentKeyField = "1";

        //        childUnitDto.Children = new List<AppTransactionUnitExDto>();

        //        foreach (var l3TableDto in importSettingDto.LevelThreeTables.Where(o => o.NetName == l2TableDto.Name))
        //        {
        //            AppTransactionUnitExDto grandchildUnitDto = ConvertOneTableDtoToUnit(aAppTransactionExDto, l3TableDto, childUnitDto);

        //            foreach (var fkField in grandchildUnitDto.AppTransactionFieldList.Where(o => o.IsLinkToParentPrimaryKey))
        //            {
        //                if (fkField.ParentPKFieldGuid.HasValue && fkField.RowIdentityGuid.HasValue)
        //                {
        //                    aAppTransactionExDto.DictCurrentPKOrFKLinkToParentKeyGuidMap.Add(fkField.RowIdentityGuid.Value, fkField.ParentPKFieldGuid.Value);
        //                }
        //            }

        //            grandchildUnitDto.EmGridViewDisplayType = 1;
        //            grandchildUnitDto.TreeViewParentKeyField = "1";
        //            childUnitDto.Children.Add(grandchildUnitDto);
        //        }
        //    }





        //    var saveTransactionResult = AppTransactionBL.SaveAppTransactionExDto(aAppTransactionExDto);

        //    if (saveTransactionResult.IsSuccessfulWithResult)
        //    {


        //        var savedTransactionExDto = saveTransactionResult.Object;
        //        int transactionId = (int)savedTransactionExDto.Id;

        //        importSettingDto.DefaultTransactionId = transactionId;

        //        List<DatabaseTableInfoDto> matrixTables = importSettingDto.LevelTwoTables.Where(o => o.IsMatrixTable).ToList();

        //        if (matrixTables.Count > 0)
        //        {
        //            OperationCallResult<AppTransactionExDto> saveTransactionMatrixResult = UpdateMatrixUnitFromImportSetting(importSettingDto, savedTransactionExDto, matrixTables);

        //            if (saveTransactionMatrixResult.IsSuccessfulWithResult)
        //            {
        //                savedTransactionExDto = saveTransactionMatrixResult.Object;

        //                if (savedTransactionExDto.CommandActionList == null)
        //                {
        //                    savedTransactionExDto.CommandActionList = new List<AppProjectWorkFlowActionExDto>();
        //                }

        //                AppProjectWorkFlowActionExDto commandDto = new AppProjectWorkFlowActionExDto();

        //                commandDto.ActionFlowOrder = 1;
        //                commandDto.ActionGuid = Guid.NewGuid();
        //                commandDto.ActionType = (int)EmAppTransactionCommandType.GenerateMatrix;
        //                commandDto.Name = "Generate Matrix";
        //                commandDto.ActionAttribute = new AppActionAttributeDto();
        //                commandDto.ActionAttribute.ChildActionList = new List<ChildTransactionCommandDto>();
        //                commandDto.ActionAttribute.LinkToUI = true;
        //                commandDto.ActionAttribute.IsShowOnTopMenu = true;

        //                savedTransactionExDto.CommandActionList.Add(commandDto);


        //                var saveMatrixCommandResult = AppTransactionCommandBL.SaveOneTransactionCommandActionList(savedTransactionExDto);

        //                if (!saveMatrixCommandResult.IsSuccessfulWithResult)
        //                {
        //                    aValidationResult.Merge(saveMatrixCommandResult.ValidationResult);
        //                    return;
        //                }
        //            }
        //            else
        //            {
        //                aValidationResult.Merge(saveTransactionMatrixResult.ValidationResult);
        //                return;
        //            }
        //        }

        //        var generateFormSearchMenuResult = AppDatabaseViewBL.QuickGenerateTransactionDefaultSeachNavigation(transactionId, importSettingDto);

        //        if (generateFormSearchMenuResult.IsSuccessful)
        //        {
        //            aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_CreateTableImportSettingAndProcessImport_Ok", ValidationItemType.Message, "Generate Form Success."));
        //        }
        //        else
        //        {
        //            aValidationResult.Merge(saveTransactionResult.ValidationResult);
        //        }
        //    }
        //    else
        //    {
        //        aValidationResult.Merge(saveTransactionResult.ValidationResult);
        //    }
        //}


        private static void UpdateImportedDataModel(AppDataSetExDto aAppDataSetExDto, ValidationResult aValidationResult, DatabaseTableImportSettingDto importSettingDto)
        {
            if (importSettingDto.DefaultTransactionId.HasValue)
            {
                AppTransactionExDto aAppTransactionExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(importSettingDto.DefaultTransactionId.Value);


                //aAppTransactionExDto.DictCurrentPKOrFKLinkToParentKeyGuidMap = new Dictionary<Guid, Guid>();
                AppTransactionUnitExDto rootUnitDto = aAppTransactionExDto.RootMasterUnit;

                UpdateOneUnitFromTableDto(aAppTransactionExDto, rootUnitDto, importSettingDto.LevelOneTables[0], null);

                foreach (var l2TableDto in importSettingDto.LevelTwoTables)
                {
                    AppTransactionUnitExDto childUnitDto = null;
                    if (!l2TableDto.IsNewTable)
                    {
                        childUnitDto = rootUnitDto.Children.FirstOrDefault(o => o.DataBaseTableName == l2TableDto.Name);
                    }



                    if (childUnitDto == null)
                    {
                        childUnitDto = ConvertOneTableDtoToUnit(aAppTransactionExDto, l2TableDto, rootUnitDto);

                        foreach (var fkField in childUnitDto.AppTransactionFieldList.Where(o => o.IsLinkToParentPrimaryKey))
                        {
                            if (fkField.ParentPKFieldGuid.HasValue && fkField.RowIdentityGuid.HasValue)
                            {
                                aAppTransactionExDto.DictCurrentPKOrFKLinkToParentKeyGuidMap.Add(fkField.RowIdentityGuid.Value, fkField.ParentPKFieldGuid.Value);
                            }
                        }


                        rootUnitDto.Children.Add(childUnitDto);
                        childUnitDto.EmGridViewDisplayType = 1;
                        childUnitDto.TreeViewParentKeyField = "1";

                        childUnitDto.Children = new List<AppTransactionUnitExDto>();

                        foreach (var l3TableDto in importSettingDto.LevelThreeTables.Where(o => o.NetName == l2TableDto.Name))
                        {
                            AppTransactionUnitExDto grandchildUnitDto = ConvertOneTableDtoToUnit(aAppTransactionExDto, l3TableDto, childUnitDto);

                            foreach (var fkField in grandchildUnitDto.AppTransactionFieldList.Where(o => o.IsLinkToParentPrimaryKey))
                            {
                                if (fkField.ParentPKFieldGuid.HasValue && fkField.RowIdentityGuid.HasValue)
                                {
                                    aAppTransactionExDto.DictCurrentPKOrFKLinkToParentKeyGuidMap.Add(fkField.RowIdentityGuid.Value, fkField.ParentPKFieldGuid.Value);
                                }
                            }

                            grandchildUnitDto.EmGridViewDisplayType = 1;
                            grandchildUnitDto.TreeViewParentKeyField = "1";
                            childUnitDto.Children.Add(grandchildUnitDto);
                        }

                    }
                    else
                    {
                        UpdateOneUnitFromTableDto(aAppTransactionExDto, childUnitDto, l2TableDto, rootUnitDto);

                        //foreach (var fkField in childUnitDto.AppTransactionFieldList.Where(o => o.IsLinkToParentPrimaryKey))
                        //{
                        //    if (fkField.ParentPKFieldGuid.HasValue && fkField.RowIdentityGuid.HasValue)
                        //    {
                        //        aAppTransactionExDto.DictCurrentPKOrFKLinkToParentKeyGuidMap.Add(fkField.RowIdentityGuid.Value, fkField.ParentPKFieldGuid.Value);
                        //    }
                        //}

                        if (childUnitDto.Children == null)
                        {
                            childUnitDto.Children = new List<AppTransactionUnitExDto>();
                        }

                        foreach (var l3TableDto in importSettingDto.LevelThreeTables.Where(o => o.NetName == l2TableDto.Name))
                        {
                            AppTransactionUnitExDto grandchildUnitDto = null;

                            if (!l3TableDto.IsNewTable)
                            {
                                grandchildUnitDto = childUnitDto.Children.FirstOrDefault(o => o.DataBaseTableName == l3TableDto.Name);
                            }

                            if (grandchildUnitDto == null)
                            {
                                grandchildUnitDto = ConvertOneTableDtoToUnit(aAppTransactionExDto, l3TableDto, childUnitDto);

                                foreach (var fkField in grandchildUnitDto.AppTransactionFieldList.Where(o => o.IsLinkToParentPrimaryKey))
                                {
                                    if (fkField.ParentPKFieldGuid.HasValue && fkField.RowIdentityGuid.HasValue)
                                    {
                                        aAppTransactionExDto.DictCurrentPKOrFKLinkToParentKeyGuidMap.Add(fkField.RowIdentityGuid.Value, fkField.ParentPKFieldGuid.Value);
                                    }
                                }

                                grandchildUnitDto.EmGridViewDisplayType = 1;
                                grandchildUnitDto.TreeViewParentKeyField = "1";
                                childUnitDto.Children.Add(grandchildUnitDto);
                            }
                            else
                            {
                                UpdateOneUnitFromTableDto(aAppTransactionExDto, grandchildUnitDto, l3TableDto, childUnitDto);

                                //foreach (var fkField in grandchildUnitDto.AppTransactionFieldList.Where(o => o.IsLinkToParentPrimaryKey))
                                //{
                                //    if (fkField.ParentPKFieldGuid.HasValue && fkField.RowIdentityGuid.HasValue)
                                //    {
                                //        aAppTransactionExDto.DictCurrentPKOrFKLinkToParentKeyGuidMap.Add(fkField.RowIdentityGuid.Value, fkField.ParentPKFieldGuid.Value);
                                //    }
                                //}
                            }

                        }


                    }

                }





                var saveTransactionResult = AppTransactionBL.SaveAppTransactionExDto(aAppTransactionExDto);

                if (saveTransactionResult.IsSuccessfulWithResult)
                {

                    var savedTransactionExDto = saveTransactionResult.Object;
                    int transactionId = (int)savedTransactionExDto.Id;

                    //importSettingDto.DefaultTransactionId = transactionId;

                    //List<DatabaseTableInfoDto> matrixTables = importSettingDto.LevelTwoTables.Where(o => o.IsMatrixTable).ToList();

                    //if (matrixTables.Count > 0)
                    //{
                    //    OperationCallResult<AppTransactionExDto> saveTransactionMatrixResult = UpdateMatrixUnitFromImportSetting(importSettingDto, savedTransactionExDto, matrixTables);

                    //    if (saveTransactionMatrixResult.IsSuccessfulWithResult)
                    //    {
                    //        savedTransactionExDto = saveTransactionMatrixResult.Object;

                    //        if (savedTransactionExDto.CommandActionList == null)
                    //        {
                    //            savedTransactionExDto.CommandActionList = new List<AppProjectWorkFlowActionExDto>();
                    //        }

                    //        AppProjectWorkFlowActionExDto commandDto = new AppProjectWorkFlowActionExDto();

                    //        commandDto.ActionFlowOrder = 1;
                    //        commandDto.ActionGuid = Guid.NewGuid();
                    //        commandDto.ActionType = (int)EmAppTransactionCommandType.GenerateMatrix;
                    //        commandDto.Name = "Generate Matrix";
                    //        commandDto.ActionAttribute = new AppActionAttributeDto();
                    //        commandDto.ActionAttribute.ChildActionList = new List<ChildTransactionCommandDto>();
                    //        commandDto.ActionAttribute.LinkToUI = true;
                    //        commandDto.ActionAttribute.IsShowOnTopMenu = true;

                    //        savedTransactionExDto.CommandActionList.Add(commandDto);


                    //        var saveMatrixCommandResult = AppTransactionCommandBL.SaveOneTransactionCommandActionList(savedTransactionExDto);

                    //        if (!saveMatrixCommandResult.IsSuccessfulWithResult)
                    //        {
                    //            aValidationResult.Merge(saveMatrixCommandResult.ValidationResult);
                    //            return;
                    //        }
                    //    }
                    //    else
                    //    {
                    //        aValidationResult.Merge(saveTransactionMatrixResult.ValidationResult);
                    //        return;
                    //    }
                    //}

                    //var generateFormSearchMenuResult = AppDatabaseViewBL.QuickGenerateTransactionDefaultSeachNavigation(transactionId, importSettingDto);

                    //if (generateFormSearchMenuResult.IsSuccessful)
                    //{
                    //    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_CreateTableImportSettingAndProcessImport_Ok", ValidationItemType.Message, "Generate Form Success."));
                    //}
                    //else
                    //{
                    //    aValidationResult.Merge(saveTransactionResult.ValidationResult);
                    //}
                }
                else
                {
                    aValidationResult.Merge(saveTransactionResult.ValidationResult);
                }
            }

        }


        private static void CreateImportApiFromSetting(AppDataSetExDto aAppDataSetExDto, ValidationResult aValidationResult, DatabaseTableImportSettingDto importSettingDto)
        {
            string apiName = "";

            if (importSettingDto.LevelOneTables.Count > 0)
            {
                apiName = "Import_" + importSettingDto.LevelOneTables[0].Name + "_" + aAppDataSetExDto.Id;
            }
            else if (importSettingDto.Tables.Count > 0)
            {
                apiName = "Import_" + importSettingDto.Tables[0].Name + "_" + aAppDataSetExDto.Id;
            }

            if (!string.IsNullOrWhiteSpace(apiName))
            {
                AppIntergrationSettingParameterExDto apiDto = new AppIntergrationSettingParameterExDto();
                apiDto.IntergrationSettingId = 1; // App Bilder API
                apiDto.ActionCode = apiName;

                apiDto.HttpMethd = EnumHttpMethod.Post.ToString();
                apiDto.DataSourceId = aAppDataSetExDto.DataSourceFrom;

                apiDto.APIConfigParameters = new APIConfigParameterDTO();
                apiDto.APIConfigParameters.ExcelDataImportDataSetId = (int)aAppDataSetExDto.Id;
                apiDto.ApiconfigParameters = JsonConvert.SerializeObject(apiDto.APIConfigParameters);



                List<ExpandoObject> objList = new List<ExpandoObject>();

                var dynamicObj = new ExpandoObject() as IDictionary<string, Object>; ;

                for (int i = 0; i < importSettingDto.OrgSourceColumns.Count; i++)
                {
                    string colName = importSettingDto.OrgSourceColumns[i].Name;


                    dynamicObj.Add(colName, "");
                }

                objList.Add(dynamicObj as ExpandoObject);

                apiDto.JsonSampleData = Newtonsoft.Json.JsonConvert.SerializeObject(objList);

                var saveResult = AppIntergrationSettingBL.SaveAppIntergrationSettingParameterExDto(apiDto);

                if (saveResult.IsSuccessfulWithResult)
                {
                    importSettingDto.DefaultUpdateApiId = (int)saveResult.Object.Id;
                }
                else
                {
                    aValidationResult.Merge(saveResult.ValidationResult);
                }
            }




        }



        private static OperationCallResult<AppTransactionExDto> UpdateMatrixUnitFromImportSetting(DatabaseTableImportSettingDto importSettingDto, AppTransactionExDto transactionExDto, List<DatabaseTableInfoDto> matrixTables)
        {
            foreach (var matrixTableInfoDto in matrixTables)
            {
                foreach (var matrixUnitDto in transactionExDto.RootMasterUnit.Children)
                {
                    if (matrixUnitDto.DataBaseTableName == matrixTableInfoDto.Name)
                    {
                        matrixUnitDto.IsModified = true;
                        matrixUnitDto.IsMatrixUnit = true;

                        foreach (string matrixFkTableName in matrixTableInfoDto.ForeignMatrixKeyTableNameList)
                        {
                            string matrixFkColumnName = importSettingDto.DictTableNameAndLogicKeyColumnNameList[matrixFkTableName].FirstOrDefault();

                            if (!string.IsNullOrWhiteSpace(matrixFkColumnName))
                            {
                                var matrixFkkUnit = transactionExDto.RootMasterUnit.Children.FirstOrDefault(o => o.DataBaseTableName == matrixFkTableName);
                                if (matrixFkkUnit != null)
                                {
                                    var matrixFkUnitKeyFiled = matrixFkkUnit.AppTransactionFieldList.FirstOrDefault(o => o.DataBaseFieldName == matrixFkColumnName);
                                    var matrixFkFiled = matrixUnitDto.AppTransactionFieldList.FirstOrDefault(o => o.DataBaseFieldName == matrixFkColumnName);

                                    if (matrixFkUnitKeyFiled != null && matrixFkFiled != null)
                                    {
                                        matrixFkFiled.IsModified = true;
                                        matrixFkFiled.MatrixForeignKeyFieldId = (int)matrixFkUnitKeyFiled.Id;
                                    }
                                }
                            }
                        }
                    }

                }
            }

            transactionExDto.IsModified = true;

            var saveTransactionMatrixResult = AppTransactionBL.SaveAppTransactionExDto(transactionExDto);
            return saveTransactionMatrixResult;
        }

        private static AppTransactionUnitExDto ConvertOneTableDtoToUnit(AppTransactionExDto aAppTransactionExDto, DatabaseTableInfoDto tableDto, AppTransactionUnitExDto parentUnit)
        {
            TableToUnitConverterDto converterDto = new TableToUnitConverterDto()
            {
                TableName = tableDto.Name,
                SchemaOwner = tableDto.SchemaOwner,
                ParentUnit = parentUnit,
                TransactionId = null,
                DataSourceRegisterId = aAppTransactionExDto.DataSourceFrom,
                DictColumnNameAndEntityId = new Dictionary<string, int>(),
                DictColumnNameFkTableName = new Dictionary<string, string>(),
            };

            tableDto.Columns.ForAll(o =>
            {
                int? entityId = ControlTypeValueConverter.ConvertValueToInt(o.NetName);
                if (entityId.HasValue)
                {
                    converterDto.DictColumnNameAndEntityId.Add(o.Name, entityId.Value);
                }

                if (o.IsForeignKey)
                {
                    converterDto.DictColumnNameFkTableName.Add(o.Name, o.ForeignKeyTableName);
                }
            });


            AppTransactionUnitExDto newUnit = ConvertDbSchemaOwnerTableNameToTransactionUnitExDto(converterDto);

            return newUnit;
        }

        private static void UpdateOneUnitFromTableDto(AppTransactionExDto aAppTransactionExDto, AppTransactionUnitExDto unitExDto, DatabaseTableInfoDto tableDto, AppTransactionUnitExDto parentUnit)
        {
            if (tableDto.DictNewColumnNameAndDto != null)
            {
                foreach (var aColumn in tableDto.DictNewColumnNameAndDto.Values)
                {

                    int? entityId = ControlTypeValueConverter.ConvertValueToInt(aColumn.NetName);

                    var newTransactionField = ConvertTableColumnToTransactionFieldExDto(aColumn, entityId);

                    unitExDto.AppTransactionFieldList.Add(newTransactionField);


                }
            }

        }

        private static AppTransactionUnitExDto ConvertDbSchemaOwnerTableNameToTransactionUnitExDto(TableToUnitConverterDto converterDto)
        {
            if (converterDto != null && !string.IsNullOrWhiteSpace(converterDto.TableName))
            {
                DatabaseTable dbTable = AppMetaDataBL.GetOneDatabaseTableSchema(converterDto.TableName, converterDto.DataSourceRegisterId, converterDto.SchemaOwner);
                DatabaseTable parentUnitDbTable = null;


                if (converterDto.ParentUnit != null && !string.IsNullOrWhiteSpace(converterDto.ParentUnit.DataBaseTableName))
                {
                    parentUnitDbTable = AppMetaDataBL.GetOneDatabaseTableSchema(converterDto.ParentUnit.DataBaseTableName, converterDto.DataSourceRegisterId, converterDto.ParentUnit.SchemaOwner);
                }

                if (dbTable != null)
                {
                    AppTransactionUnitExDto parentUnit = converterDto.ParentUnit;

                    AppTransactionUnitExDto newUnit = AppTransactionBL.CreateTransactionUnitFromDatabaseTable(dbTable);

                    newUnit.IsSynchToDatabaseTable = false;
                    newUnit.IsReadOnly = false;


                    //	newUnit.IsPrimaryKeyIdentityInsert = dbTable.IsPrimaryKeyAutoNumber;


                    newUnit.AppTransactionFieldList = new ObservableSet<AppTransactionFieldExDto>();
                    newUnit.Children = new List<AppTransactionUnitExDto>();

                    foreach (var aColumn in dbTable.Columns)
                    {
                        int? entityId = null;

                        if (converterDto.DictColumnNameAndEntityId != null)
                        {
                            if (converterDto.DictColumnNameAndEntityId.ContainsKey(aColumn.Name))
                            {
                                entityId = converterDto.DictColumnNameAndEntityId[aColumn.Name];
                            }
                        }

                        var newTransactionField = ConvertTableColumnToTransactionFieldExDto(aColumn, entityId);



                        newUnit.AppTransactionFieldList.Add(newTransactionField);
                        if (converterDto.DictColumnNameFkTableName.ContainsKey(aColumn.Name))
                        {
                            aColumn.IsForeignKey = true;
                            aColumn.ForeignKeyTableName = converterDto.DictColumnNameFkTableName[aColumn.Name];
                        }

                        SetNewTransactionFieldForeignkey(dbTable, parentUnitDbTable, parentUnit, aColumn, newTransactionField);
                    }

                    return newUnit;
                }

            }

            return null;

        }


        //private static List<AppTransactionFieldExDto> ConvertTableColumnsToTransactionFieldExDtoList(TableToUnitConverterDto converterDto)
        //{
        //    if (converterDto != null && !string.IsNullOrWhiteSpace(converterDto.TableName) && converterDto.NeedToAddDbColumns != null && converterDto.NeedToAddDbColumns.Count > 0)
        //    {
        //        DatabaseTable dbTable = AppMetaDataBL.GetOneDatabaseTableSchema(converterDto.TableName, converterDto.DataSourceRegisterId, converterDto.SchemaOwner);
        //        DatabaseTable parentUnitDbTable = null;


        //        if (converterDto.ParentUnit != null && !string.IsNullOrWhiteSpace(converterDto.ParentUnit.DataBaseTableName))
        //        {
        //            parentUnitDbTable = AppMetaDataBL.GetOneDatabaseTableSchema(converterDto.ParentUnit.DataBaseTableName, converterDto.DataSourceRegisterId, converterDto.SchemaOwner);
        //        }

        //        if (dbTable != null)
        //        {
        //            AppTransactionUnitExDto parentUnit = converterDto.ParentUnit;

        //            List<AppTransactionFieldExDto> toReturn = new List<AppTransactionFieldExDto>();

        //            foreach (DatabaseColumn aColumn in converterDto.NeedToAddDbColumns)
        //            {
        //                AppTransactionFieldExDto newTransactionField = ConvertTableColumnToTransactionFieldExDto(aColumn);

        //                if (newTransactionField != null)
        //                {
        //                    toReturn.Add(newTransactionField);
        //                    SetNewTransactionFieldForeignkey(dbTable, parentUnitDbTable, parentUnit, aColumn, newTransactionField);
        //                }
        //            }

        //            return toReturn;
        //        }
        //    }

        //    return null;
        //}

        private static AppTransactionFieldExDto ConvertTableColumnToTransactionFieldExDto(DatabaseColumn aTableColumn, int? entityId = null)
        {
            if (aTableColumn != null)
            {
                AppTransactionFieldExDto aTransactionField = new AppTransactionFieldExDto();

                aTransactionField.DataBaseFieldName = aTableColumn.Name;
                aTransactionField.DisplayName = AppTransactionBL.ConvertDbNameToDisplayName(aTableColumn.Name);


                if (aTableColumn.Tag != null)
                {
                    try
                    {
                        aTransactionField.DataType = (int)Enum.Parse(typeof(EmAppDataType), aTableColumn.Tag.ToString());
                    }
                    catch (Exception ex)
                    {

                    }
                }


                aTransactionField.DataRetrieveType = (int)EmAppCascadingSourceType.RelationalTable;
                aTransactionField.DisplayWidth = "100";
                aTransactionField.Nbdecimal = 0;
                aTransactionField.IsPrimaryKey = aTableColumn.IsPrimaryKey;
                aTransactionField.IsVisible = true;
                aTransactionField.IsReadonly = false;
                aTransactionField.IsGroupBy = false;
                aTransactionField.IsGridUseAvailableEntitySource = false;
                aTransactionField.IsNeedLog = false;
                aTransactionField.IsAllowEmpty = true;
                aTransactionField.IsConvertToUpperCase = false;
                aTransactionField.IsLinkToParentPrimaryKey = false;
                aTransactionField.RowIdentityGuid = Guid.NewGuid();
                aTransactionField.ControlType = (int)EmAppControlType.TextBox;

                //if (!aTableColumn.IsForeignKey)
                //{
                //    aTransactionField.ControlType = (int)ConvertDataTypeToDefaultControlType(aTransactionField.DataType);
                //}



                if (entityId.HasValue)
                {
                    aTransactionField.ControlType = (int)EmAppControlType.DDL;
                    aTransactionField.EntityId = entityId;
                }
                else
                {
                    aTransactionField.ControlType = (int)ControlTypeValueConverter.ConvertDataTypeToDefaultControlType(aTransactionField.DataType);
                }

                if (aTableColumn.Length.HasValue && aTableColumn.Length.Value > 0)
                {
                    aTransactionField.MaxCharLegnth = aTableColumn.Length;
                    aTransactionField.NeedValidator = true;
                }

                if ((aTableColumn.IsPrimaryKey || !aTableColumn.Nullable) && !aTableColumn.IsAutoNumber)
                {
                    aTransactionField.IsAllowEmpty = false;
                    aTransactionField.NeedValidator = true;
                }

                if (aTableColumn.IsUniqueKey && !aTableColumn.IsAutoNumber)
                {
                    aTransactionField.IsUnique = true;
                    aTransactionField.NeedValidator = true;
                }

                if (aTableColumn.Scale.HasValue)
                {
                    aTransactionField.Nbdecimal = aTableColumn.Scale.Value;
                }


                //aTransactionField.SortOrder = Math.ceil(transactionConfigHelper.findMaxTransactionUnitFieldSortOrder(aUnit) / 10.0) * 10 + 10;
                //aTransactionField.uiId = angular.guid();
                //aTransactionField.isColumnSelected = true;
                //aTransactionField.EntityIdSelector = null;
                //aTransactionField.DataRetrieveMappingEditor = null;
                //aTransactionField.DdlparentLevelIdSelector = null;
                //aTransactionField.MatrixForeignKeyFieldIdSelector = null;
                //aTransactionField.MasterEntityFieldlIdSelector = null;
                //aTransactionField.MasterEntityFieldlIdSelector = String.format(cellHtmlStringFormat, aTransactionField.uiId, 'MasterEntityFieldlIdSelector', '');




                return aTransactionField;
            }

            return null;
        }

        private static void SetNewTransactionFieldForeignkey(DatabaseTable dbTable, DatabaseTable parentUnitDbTable, AppTransactionUnitExDto parentUnit, DatabaseColumn aColumn, AppTransactionFieldExDto newTransactionField)
        {
            if (parentUnitDbTable != null && parentUnit != null && aColumn.IsForeignKey && !string.IsNullOrWhiteSpace(aColumn.ForeignKeyTableName))
            {
                if (parentUnitDbTable.Name.ToLower() == aColumn.ForeignKeyTableName.ToLower())
                {
                    newTransactionField.IsLinkToParentPrimaryKey = true;
                    //newTransactionField.IsAllowEmpty = true;                                         

                    if (parentUnitDbTable != null && parentUnitDbTable.PrimaryKey != null && parentUnitDbTable.PrimaryKey.Columns != null && parentUnitDbTable.PrimaryKey.Columns.Count > 0)
                    {
                        var defaultParentPkField = parentUnit.AppTransactionFieldList.FirstOrDefault(o => o.IsPrimaryKey);

                        if (defaultParentPkField != null)
                        {
                            newTransactionField.ParentPKFieldGuid = defaultParentPkField.RowIdentityGuid;
                        }

                        if (dbTable.ForeignKeys != null && dbTable.ForeignKeys.Count > 0)
                        {
                            foreach (var fkobj in dbTable.ForeignKeys)
                            {
                                if (fkobj.RefersToTable.ToLower() == parentUnitDbTable.Name.ToLower())
                                {
                                    var fkColumnIndex = fkobj.Columns.IndexOf(aColumn.Name);

                                    if (fkColumnIndex >= 0 && parentUnitDbTable.PrimaryKey.Columns.Count > fkColumnIndex)
                                    {
                                        var linkToParentTbPKColumnName = parentUnitDbTable.PrimaryKey.Columns[fkColumnIndex];

                                        var parentPkField = parentUnit.AppTransactionFieldList.FirstOrDefault(o => o.DataBaseFieldName.ToLower() == linkToParentTbPKColumnName.ToLower());
                                        if (parentPkField != null)
                                        {
                                            newTransactionField.ParentPKFieldGuid = parentPkField.RowIdentityGuid;
                                        }
                                    }
                                    break;
                                }
                            }


                        }
                    }
                }
            }
        }



        private static void ResetDiagramTablePositionByLevel(DatabaseTableImportSettingDto importSettingDto, DatabaseViewDto diagramDto)
        {
            if (diagramDto.DictTables.ContainsKey(importSettingDto.LevelOneTables[0].Name))
            {
                var diagramTableDto = diagramDto.DictTables[importSettingDto.LevelOneTables[0].Name];

                diagramTableDto.PositionX = 20;
                diagramTableDto.PositionY = 20;
            }

            for (int i = 0; i < importSettingDto.LevelTwoTables.Count; i++)
            {
                var tableDto = importSettingDto.LevelTwoTables[i];

                if (diagramDto.DictTables.ContainsKey(tableDto.Name))
                {
                    var diagramTableDto = diagramDto.DictTables[tableDto.Name];

                    diagramTableDto.PositionX = 320;
                    diagramTableDto.PositionY = 300 * (i) + 20;
                }
            }

            for (int i = 0; i < importSettingDto.LevelThreeTables.Count; i++)
            {
                var tableDto = importSettingDto.LevelThreeTables[i];

                if (diagramDto.DictTables.ContainsKey(tableDto.Name))
                {
                    var diagramTableDto = diagramDto.DictTables[tableDto.Name];

                    diagramTableDto.PositionX = 640;
                    diagramTableDto.PositionY = 300 * (i) + 20;
                }
            }
        }

        private static ValidationResult ProcessTempTableToTargetTableImport(DatabaseTableImportSettingDto importSettingDto)
        {
            ValidationResult aValidationResult = new ValidationResult();


            string query = "";

            if (importSettingDto.Tables != null)
            {
                if (importSettingDto.IsDraft)
                {
                    importSettingDto.SimulateImportedTableNames = importSettingDto.Tables.Select(o => o.Name).ToList();
                }

                try
                {
                    //step 1. Change Column Data Types                  

                    ChangeTempTableColumnDataType(importSettingDto, aValidationResult);

                    if (aValidationResult.HasErrors)
                    {
                        return aValidationResult;
                    }

                    // step 2. Create and Alter Tables Script
                    query += ProcessTableImport_GenerateCreateTablesScript(importSettingDto, aValidationResult) + " ;" + Environment.NewLine;

                    if (aValidationResult.HasErrors)
                    {
                        return aValidationResult;
                    }

                    query += ProcessTableImport_GenerateAlterTablesScript(importSettingDto, aValidationResult) + " ;" + Environment.NewLine;

                    if (aValidationResult.HasErrors)
                    {
                        return aValidationResult;
                    }


                    // step 3. Create FK Script
                    query += ProcessTableImport_GenerateCreateFKsScript(importSettingDto, aValidationResult) + " ;" + Environment.NewLine;

                    if (aValidationResult.HasErrors)
                    {
                        return aValidationResult;
                    }


                    // step 4. Insert Data Script 
                    if (importSettingDto.IsEntityImport)
                    {
                        query += ProcessEntityImport_GenerateTableDataUpdateScript(importSettingDto, aValidationResult) + Environment.NewLine;
                    }
                    else
                    {
                        query += ProcessTableImport_GenerateTableDataUpdateScript(importSettingDto, aValidationResult) + Environment.NewLine;
                    }


                    if (aValidationResult.HasErrors)
                    {
                        return aValidationResult;
                    }
                    else
                    {
                        DatabaseFixture databaseFixtureInstance = AppMetaDataBL.GetNewInstanceDatbaseFixtureWtihSchmaOwner(importSettingDto.DataSourceRegisterId, null);

                        //string errorMsg = query;

                        string errorMsg = AppMetaDataBL.ExecSQlCommand(databaseFixtureInstance, query);

                        if (!string.IsNullOrWhiteSpace(errorMsg))
                        {


                            aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Error", ValidationItemType.Error, "Import Failed. " + errorMsg));
                        }
                    }

                }
                catch (Exception ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error, "Import Failed. " + ex.ToString()));
                }
            }

            if (!aValidationResult.HasErrors)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Message, "Import Success."));

                AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(importSettingDto.DataSourceRegisterId);
                //aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Query", ValidationItemType.Message, "\nQuery: \n" + query + "\n"));
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessTransactionTableDataImport(DatabaseTableImportSettingDto importSettingDto)
        {
            ValidationResult aValidationResult = new ValidationResult();


            string query = "";

            if (importSettingDto.Tables != null)
            {
                try
                {
                    //var transactionExDto = AppTransactionBL.RetrieveOneAppTransactionExDto(importSettingDto.NeedToUpdateTransactionId.Value);

                    SynchronizeSourceColumnDataTypeFromsFromImportTables(importSettingDto);

                    query += ChangeTempTableColumnDataType(importSettingDto, aValidationResult);

                    if (aValidationResult.HasErrors)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Query", ValidationItemType.Message, "\nQuery: \n" + query + "\n"));
                        return aValidationResult;
                    }



                    //// step 2. Create and Alter Tables Script
                    //query += ProcessTableImport_GenerateCreateTablesScript(importSettingDto, aValidationResult) + " ;" + Environment.NewLine;

                    //if (aValidationResult.HasErrors)
                    //{
                    //    return aValidationResult;
                    //}

                    //query += ProcessTableImport_GenerateAlterTablesScript(importSettingDto, aValidationResult) + " ;" + Environment.NewLine;

                    //if (aValidationResult.HasErrors)
                    //{
                    //    return aValidationResult;
                    //}


                    //// step 3. Create FK Script
                    //query += ProcessTableImport_GenerateCreateFKsScript(importSettingDto, aValidationResult) + " ;" + Environment.NewLine;

                    //if (aValidationResult.HasErrors)
                    //{
                    //    return aValidationResult;
                    //}




                    query += ProcessTableImport_GenerateTableDataUpdateScript(importSettingDto, aValidationResult) + Environment.NewLine;


                    DatabaseFixture databaseFixtureInstance = AppMetaDataBL.GetNewInstanceDatbaseFixtureWtihSchmaOwner(importSettingDto.DataSourceRegisterId, null);

                    string errorMsg = AppMetaDataBL.ExecSQlCommand(databaseFixtureInstance, query);

                    if (!string.IsNullOrWhiteSpace(errorMsg))
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error, "Import Failed. " + errorMsg));
                    }
                    else
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_UpdateImportedTableDataFromTempTable_Ok", ValidationItemType.Message, "Update Table(s) From Excel Success."));

                        //aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Query", ValidationItemType.Message, "\nQuery: \n" + query + "\n"));
                    }
                }
                catch (Exception ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error, "Import Failed. " + ex.ToString()));
                }
            }

            if (!aValidationResult.HasErrors)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Message, "Import Success."));

                //aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Query", ValidationItemType.Message, "\nQuery: \n" + query + "\n"));
            }

            return aValidationResult;
        }

        private static void SynchronizeSourceColumnDataTypeFromsFromImportTables(DatabaseTableImportSettingDto importSettingDto)
        {
            foreach (string tableName in importSettingDto.DictTableNameColumnNameAndSourceColumnNameMapping.Keys)
            {
                var tableDto = importSettingDto.DictTableNameAndDto[tableName];

                if (tableDto != null)
                {
                    foreach (string transFieldColumnName in importSettingDto.DictTableNameColumnNameAndSourceColumnNameMapping[tableName].Keys)
                    {
                        string srcColumnName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(importSettingDto.DictTableNameColumnNameAndSourceColumnNameMapping[tableName][transFieldColumnName]);

                        if (!string.IsNullOrWhiteSpace(srcColumnName))
                        {
                            var transFiledColumnDto = tableDto.Columns.FirstOrDefault(o => o.Name.ToLower() == transFieldColumnName.ToLower());
                            var sourceColumnDto = importSettingDto.SourceColumns.FirstOrDefault(o => o.Name.ToLower() == srcColumnName.ToLower());

                            if (transFiledColumnDto != null && sourceColumnDto != null)
                            {
                                sourceColumnDto.Tag = transFiledColumnDto.Tag.ToString();
                            }
                        }
                    }
                }
            }
        }

        private static string ChangeTempTableColumnDataType(DatabaseTableImportSettingDto importSettingDto, ValidationResult aValidationResult, List<DatabaseColumnExDto> needToChangeSrcColumnList = null)
        {
            string query = "";

            //DatabaseTable tempTableDto = AppCacheManagerBL.GetDatabaseTable(importSettingDto.TempTableName, importSettingDto.DataSourceRegisterId, "dbo");

            //string tempTableName = importSettingDto.OrgTempTableName;

            //if (string.IsNullOrWhiteSpace(tempTableName))
            //{
            //    tempTableName = importSettingDto.TempTableName;
            //}


            DatabaseTable tempTableDto = AppMetaDataBL.GetOneDatabaseTableSchema(importSettingDto.OrgTempTableName, importSettingDto.DataSourceRegisterId, "");
            var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(importSettingDto.DataSourceRegisterId.Value);

            if (needToChangeSrcColumnList == null)
            {
                needToChangeSrcColumnList = new List<DatabaseColumnExDto>();
                if (importSettingDto.SourceColumns != null)
                {
                    needToChangeSrcColumnList = importSettingDto.SourceColumns.Where(o => o.Tag != null && o.Tag.ToString() != EmAppDataType.String.ToString()).ToList();
                }
            }


            if (needToChangeSrcColumnList.Count > 0)
            {

                List<string> orgTempTableColumnNames = tempTableDto.Columns.Select(o => o.Name).ToList();
                List<string> alreadyExistOrgColumnNames = new List<string>();

                foreach (var changedColumnDto in needToChangeSrcColumnList)
                {
                    foreach (var columnDto in tempTableDto.Columns)
                    {
                        if (columnDto.Name == changedColumnDto.Name)
                        {
                            columnDto.Tag = changedColumnDto.Tag;
                        }
                    }

                    foreach (var tableDto in importSettingDto.Tables)
                    {
                        foreach (var columnDto in tableDto.Columns)
                        {


                            if (columnDto.Name == changedColumnDto.Name)
                            {
                                if (orgTempTableColumnNames.Contains(columnDto.Name + "__Org"))
                                {
                                    alreadyExistOrgColumnNames.Add(columnDto.Name + "__Org");
                                }

                                columnDto.Tag = changedColumnDto.Tag;

                                int? entityId = ControlTypeValueConverter.ConvertValueToInt(columnDto.NetName);

                                if (entityId.HasValue)
                                {
                                    changedColumnDto.NetName = columnDto.NetName;

                                    if (tempTableDto.Columns.FirstOrDefault(o => o.Name == columnDto.Name + "__Org") == null)
                                    {
                                        tempTableDto.Columns.Add(new DatabaseColumn()
                                        {
                                            Name = columnDto.Name + "__Org",
                                            NetName = columnDto.Name,
                                            DbDataType = "nvarchar",
                                            Tag = "String",
                                            Nullable = true,
                                            TableName = tempTableDto.Name,
                                            IsAutoNumber = false,
                                        });
                                    }

                                    if (importSettingDto.SourceColumns.FirstOrDefault(o => o.Name == columnDto.Name + "__Org") == null)
                                    {
                                        importSettingDto.SourceColumns.Add(new DatabaseColumnExDto()
                                        {
                                            Name = columnDto.Name + "__Org",
                                            NetName = columnDto.Name,
                                            DbDataType = "nvarchar",
                                            Tag = "String",
                                            Nullable = true,
                                            TableName = tempTableDto.Name,
                                            IsAutoNumber = false,
                                        });
                                    }

                                }
                            }
                        }
                    }
                }

                string orgTempTableName = tempTableDto.Name;
                string newTempTableName = tempTableDto.Name + ExtensionMethodhelper.RandomId();
                tempTableDto.Name = newTempTableName;
                importSettingDto.TempTableName = newTempTableName;

                //bool isRenameSuccuess = AppMetaDataBL.RenameTableName(tempTableDto.Name, backupTempTableName, importSettingDto.DataSourceRegisterId, "dbo");
                string createTempTableResultMsg = "";
                bool isRecreateTempTableSuccess = AppMetaDataBL.CreateNewTable(tempTableDto, importSettingDto.DataSourceRegisterId, null, out createTempTableResultMsg);

                if (isRecreateTempTableSuccess)
                {
                    if (importSettingDto.NeedToDropTempTableNames == null)
                    {
                        importSettingDto.NeedToDropTempTableNames = new List<string>();
                    }

                    importSettingDto.NeedToDropTempTableNames.Add(newTempTableName);
                    //tempTableDto = AppCacheManagerBL.GetDatabaseTable(importSettingDto.TempTableName, importSettingDto.DataSourceRegisterId, "dbo");
                    tempTableDto = AppMetaDataBL.GetOneDatabaseTableSchema(importSettingDto.TempTableName, importSettingDto.DataSourceRegisterId, "");

                    List<DatabaseColumnExDto> ddlColumnList = new List<DatabaseColumnExDto>();

                    string insertIntoColumns = string.Join(", ", importSettingDto.SourceColumns.Select(o => o.Name));
                    string columnNamesWithSqure = string.Join(", ", importSettingDto.SourceColumns.Select(o => "[" + o.Name + "]"));

                    string selectColumns = "";

                    foreach (var columnDto in importSettingDto.SourceColumns)
                    {
                        if (selectColumns.Length > 0)
                        {
                            selectColumns += ", ";
                        }

                        if (columnDto.Name.EndsWith("__Org") && !string.IsNullOrWhiteSpace(columnDto.NetName) && !alreadyExistOrgColumnNames.Contains(columnDto.Name))
                        {
                            selectColumns += "[" + columnDto.NetName + "]";
                        }
                        else
                        {
                            int? entityId = ControlTypeValueConverter.ConvertValueToInt(columnDto.NetName);

                            if (entityId.HasValue && !alreadyExistOrgColumnNames.Contains(columnDto.Name + "__Org"))
                            {
                                selectColumns += "NULL";

                                ddlColumnList.Add(columnDto);
                            }
                            else
                            {


                                selectColumns += "[" + columnDto.Name + "]";
                            }
                        }
                    }



                    query = string.Format(@"
                            INSERT INTO [{0}] ({1})                               
                            SELECT {2} FROM [{3}];

                            ",
                       newTempTableName, columnNamesWithSqure,
                       selectColumns, orgTempTableName);


                    foreach (DatabaseColumnExDto columnDto in ddlColumnList)
                    {
                        int? entityId = ControlTypeValueConverter.ConvertValueToInt(columnDto.NetName);

                        if (entityId.HasValue)
                        {
                            var entityDto = AppEntityInfoBL.RetrieveOneAppEntityInfoExDto(entityId.Value);

                            if (entityDto.EntityType.HasValue)
                            {
                                if (entityDto.EntityType.Value == (int)EmAppEntityType.SystemDefineTable)
                                {
                                    string columnName = columnDto.Name;
                                    string orgTextColumnName = columnName + "__Org";
                                    string mappingToEntityColumnName = !string.IsNullOrWhiteSpace(columnDto.EntityColumnName) ? columnDto.EntityColumnName : entityDto.DisplayFiled1;

                                    //update ImportExcel_axis_ProductDetails3_xlsxLCEX41 set Category = entityTable.a0009_cat10Id
                                    //from ImportExcel_axis_ProductDetails3_xlsxLCEX41 as tempTable
                                    //inner join a0009_cat10 as entityTable 
                                    //        on(tempTable.Category__Org = entityTable.Category)


                                    var sqlWriter = new SqlWriter(newTempTableName, dataBaseFixture.SqlServerType.Value);

                                    Dictionary<string, string> dictUpdateTableNameAndAliasName = new Dictionary<string, string>();
                                    dictUpdateTableNameAndAliasName.Add(newTempTableName, "tempTable");

                                    string setValues = string.Format(@"[{0}].[{1}] = entityTable.[{2}]", newTempTableName, columnName, entityDto.IdentityField);
                                    string fromTables = string.Format(@"{0} as tempTable inner join [{1}] as entityTable 
                                            on (tempTable.[{2}] = entityTable.[{3}])", newTempTableName, entityDto.TableName, orgTextColumnName, mappingToEntityColumnName);
                                    string whereConditions = "";

                                    string updateStatement = sqlWriter.BuildUpdateStatement(dictUpdateTableNameAndAliasName, setValues, fromTables, whereConditions);



                                    query += System.Environment.NewLine + updateStatement + ";";


                                }
                                else if (entityDto.EntityType.Value == (int)EmAppEntityType.SimpleValueList)
                                {
                                    string columnName = columnDto.Name;
                                    string orgTextColumnName = columnName + "__Org";
                                    string mappingToEntityColumnName = !string.IsNullOrWhiteSpace(columnDto.EntityColumnName) ? columnDto.EntityColumnName : "Code";

                                    //update[ImportExcel_PCTZ03_testCsv_csvOGNF23] set Category = entityTable.a0009_cat10Id
                                    //from ImportExcel_axis_ProductDetails3_xlsxLCEX41 as tempTable
                                    //inner join
                                    //(
                                    //    select* from AppEntitySimpleListValue where EntityInfoID = 3454
                                    //)
                                    //as entityTable
                                    //        on(tempTable.Category__Org = entityTable.Category)


                                    var sqlWriter = new SqlWriter(newTempTableName, dataBaseFixture.SqlServerType.Value);

                                    Dictionary<string, string> dictUpdateTableNameAndAliasName = new Dictionary<string, string>();
                                    dictUpdateTableNameAndAliasName.Add(newTempTableName, "tempTable");

                                    string setValues = string.Format(@"[{0}].[{1}] = entityTable.[{2}]", newTempTableName, columnName, "InternalKey");
                                    string fromTables = string.Format(@"from {0} as tempTable
                                        inner join (select * from AppEntitySimpleListValue where EntityInfoID = {1}) as entityTable 
                                            on (tempTable.[{2}] = entityTable.[{3}]",
                                             newTempTableName,
                                         entityId.Value,
                                         orgTextColumnName, mappingToEntityColumnName);
                                    string whereConditions = "";

                                    string updateStatement = sqlWriter.BuildUpdateStatement(dictUpdateTableNameAndAliasName, setValues, fromTables, whereConditions);



                                    query += System.Environment.NewLine + updateStatement + ";";

                                }
                                else
                                {

                                    // To Do Import
                                }
                            }



                        }


                    }




                    DatabaseFixture databaseFixtureInstance = AppMetaDataBL.GetNewInstanceDatbaseFixtureWtihSchmaOwner(importSettingDto.DataSourceRegisterId, null);

                    string errorMsg = AppMetaDataBL.ExecSQlCommand(databaseFixtureInstance, query);


                    if (!string.IsNullOrWhiteSpace(errorMsg))
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Error", ValidationItemType.Error, "Import Failed. " + errorMsg));
                    }
                    else
                    {

                    }


                }
                else
                {
                    string errorMsg1 = "Faild to recreate temp table with modified data type.";
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Error", ValidationItemType.Error, "Import Failed. " + errorMsg1));
                }

            }

            return query;
        }

        private static string ProcessTableImport_GenerateCreateTablesScript(DatabaseTableImportSettingDto importSettingDto, ValidationResult aValidationResult)
        {
            string query = "";

            AutoAddForeignLogicKeyColumns(importSettingDto);

            if (importSettingDto.IsEntityImport)
            {
                PrepareEntityImportCascadingDto(importSettingDto);
            }

            //else
            //{
            //    if (importSettingDto.IsHaveConditionFilter)
            //    {
            //        foreach (var tableDto in importSettingDto.Tables)
            //        {
            //            string fkTableName = tableDto.NetName;

            //            if (!string.IsNullOrWhiteSpace(fkTableName) && importSettingDto.DictTableNameAndDto.ContainsKey(fkTableName))
            //            {
            //                var fkTableDto = importSettingDto.DictTableNameAndDto[fkTableName];

            //                foreach (var logicKeyColumnDto in fkTableDto.Columns.Where(o => o.IsUniqueKey))
            //                {
            //                    bool isHaveFKLogicKeyAsColumn = tableDto.Columns.FirstOrDefault(o => o.Name == logicKeyColumnDto.Name) != null;

            //                    if (!isHaveFKLogicKeyAsColumn)
            //                    {
            //                        var foreignLogicKeyColumn = logicKeyColumnDto.DeepCopy();
            //                        foreignLogicKeyColumn.TableName = tableDto.Name;
            //                        foreignLogicKeyColumn.IsUniqueKey = false;

            //                        tableDto.Columns.Add(foreignLogicKeyColumn);                                    
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}



            foreach (var tableDto in importSettingDto.Tables)
            {
                //AppMetaDataBL.CreateNewTable(tableDto, tableDto.DataSourceRegisterId);
                if (tableDto.IsNewTable)
                {
                    var query_createTable = AppMetaDataBL.PrepareCreateNewTableScript(tableDto, importSettingDto.DataSourceRegisterId);

                    if (string.IsNullOrWhiteSpace(query_createTable))
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error,
                            "Import Failed When Generating Table Creation Script For Table: " + tableDto.Name + "."));

                        return "";
                    }
                    else
                    {
                        query += query_createTable + Environment.NewLine;
                    }
                }
            }

            //if (string.IsNullOrWhiteSpace(query))
            //{
            //    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error,
            //                "Import Failed When Generating Table Creation Script."));
            //}


            return query;
        }


        private static string ProcessTableImport_GenerateAlterTablesScript(DatabaseTableImportSettingDto importSettingDto, ValidationResult aValidationResult)
        {
            string query = "";

            foreach (var tableDto in importSettingDto.Tables)
            {
                if (!tableDto.IsNewTable)
                {
                    var query_alterTable = AppMetaDataBL.PrepareAlterTableScript(tableDto, importSettingDto.DataSourceRegisterId);

                    if (string.IsNullOrWhiteSpace(query_alterTable))
                    {
                        //aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error,
                        //    "Import Failed When Generating Alter Table Script For Table: " + tableDto.Name + "."));

                        //return "";
                    }
                    else
                    {
                        query += query_alterTable + Environment.NewLine;
                    }
                }
            }

            return query;
        }


        private static void AutoAddForeignLogicKeyColumns(DatabaseTableImportSettingDto importSettingDto)
        {
            foreach (var tableDto in importSettingDto.Tables)
            {
                if (tableDto.IsNewTable)
                {
                    string fkTableName = tableDto.NetName;

                    if (!string.IsNullOrWhiteSpace(fkTableName) && importSettingDto.DictTableNameAndDto.ContainsKey(fkTableName))
                    {
                        var fkTableDto = importSettingDto.DictTableNameAndDto[fkTableName];

                        foreach (var logicKeyColumnDto in fkTableDto.Columns.Where(o => o.IsLogicKey))
                        {
                            bool isHaveFKLogicKeyAsColumn = tableDto.Columns.FirstOrDefault(o => o.Name == logicKeyColumnDto.Name) != null;

                            if (!isHaveFKLogicKeyAsColumn)
                            {
                                var foreignLogicKeyColumn = logicKeyColumnDto.DeepCopy();
                                foreignLogicKeyColumn.TableName = tableDto.Name;
                                foreignLogicKeyColumn.IsLogicKey = false;

                                tableDto.Columns.Add(foreignLogicKeyColumn);
                            }
                        }
                    }
                }
            }
        }

        private static void PrepareEntityImportCascadingDto(DatabaseTableImportSettingDto importSettingDto)
        {
            List<DatabaseTableInfoDto> needToAddRelationTables = new List<DatabaseTableInfoDto>();

            foreach (var tableDto in importSettingDto.Tables)
            {
                if (!tableDto.IsNewTable)
                {
                    continue;
                }

                bool isCascadingChildTable = !string.IsNullOrWhiteSpace(tableDto.NetName);

                if (isCascadingChildTable)
                {
                    string tableName = tableDto.Name;
                    string parentTableName = tableDto.NetName;

                    var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(importSettingDto.DataSourceRegisterId.Value);
                    string schemaOwner = dataBaseFixture.CurrentOwner;

                    if (importSettingDto.DictTableNameAndDto.ContainsKey(parentTableName))
                    {
                        var parentTableDto = importSettingDto.DictTableNameAndDto[parentTableName];

                        if (tableDto.IsManyToManyCascadingChild)
                        {
                            DatabaseTableInfoDto relationTable = new DatabaseTableInfoDto();
                            relationTable.Name = "Relation_" + parentTableName + "_And_" + tableName;
                            relationTable.Tag = relationTable.Name;
                            relationTable.SchemaOwner = schemaOwner;
                            relationTable.IsEntityRelationTable = true;

                            string fkName1 = tableDto.Columns.FirstOrDefault(o => o.IsPrimaryKey).Name;
                            string fkName2 = parentTableDto.Columns.FirstOrDefault(o => o.IsPrimaryKey).Name;

                            relationTable.AddColumn(new DatabaseColumn()
                            {
                                Name = "RelationId",
                                DbDataType = "int",
                                Tag = "Integer",
                                Nullable = false,
                                IsPrimaryKey = true,
                                TableName = relationTable.Name,
                                IsAutoNumber = true,

                            });

                            relationTable.AddColumn(new DatabaseColumn()
                            {
                                Name = fkName1,
                                DbDataType = "int",
                                Tag = "Integer",
                                Nullable = true,
                                TableName = tableName,
                                IsForeignKey = true,
                                ForeignKeyTableName = tableDto.Name,
                            });

                            relationTable.AddColumn(new DatabaseColumn()
                            {
                                Name = fkName2,
                                DbDataType = "int",
                                Tag = "Integer",
                                Nullable = true,
                                TableName = tableName,
                                IsForeignKey = true,
                                ForeignKeyTableName = parentTableDto.Name,
                            });


                            needToAddRelationTables.Add(relationTable);

                        }
                        else
                        {
                            string fkName = parentTableDto.Columns.FirstOrDefault(o => o.IsPrimaryKey).Name;

                            tableDto.AddColumn(new DatabaseColumn()
                            {
                                Name = fkName,
                                DbDataType = "int",
                                Tag = "Integer",
                                Nullable = true,
                                TableName = tableName,
                                IsForeignKey = true,
                                ForeignKeyTableName = parentTableDto.Name,
                            });
                        }
                    }
                }
            }

            foreach (var newTalbleDto in needToAddRelationTables)
            {
                importSettingDto.Tables.Add(newTalbleDto);
            }

        }

        private static string ProcessTableImport_GenerateCreateFKsScript(DatabaseTableImportSettingDto importSettingDto, ValidationResult aValidationResult)
        {
            string query = "";

            foreach (var tableDto in importSettingDto.Tables)
            {
                if (!tableDto.IsNewTable)
                {
                    continue;
                }

                //foreach (DatabaseConstraint fkDto in tableDto.ForeignKeys)
                //{
                //    bool isOneFkSuccess = false;

                //    if (fkDto.Columns.Count > 0
                //        && !string.IsNullOrWhiteSpace(fkDto.RefersToTable)
                //        && importSettingDto.DictTableNameAndDto.ContainsKey(fkDto.RefersToTable)
                //        )
                //    {
                //        string fkColumnName = fkDto.Columns[0];
                //        DatabaseTable pkTableDto = importSettingDto.DictTableNameAndDto[fkDto.RefersToTable];
                //        DatabaseColumn pkColumn = pkTableDto.Columns.FirstOrDefault(o => o.IsPrimaryKey);

                //        if (pkColumn != null)
                //        {
                //            string query_CreateFk = @"ALTER TABLE " + tableDto.Name +
                //                 " ADD CONSTRAINT FK_" + tableDto.Name + "_" + pkTableDto.Name + "_" + ExtensionMethodhelper.RandomId()
                //                 + " FOREIGN KEY ([" + fkColumnName + "]) REFERENCES "
                //                 + pkTableDto.Name + " ([" + pkColumn.Name + "])";

                //            query += query_CreateFk + Environment.NewLine;

                //            isOneFkSuccess = true;
                //        }
                //    }

                //    if (!isOneFkSuccess)
                //    {
                //        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Message,
                //            "Import Failed When Generating FK Creation Scrip For Table: " + tableDto.Name + "."));

                //        return "";
                //    }
                //}

                foreach (DatabaseColumn fkColumnDto in tableDto.Columns.Where(o => o.IsForeignKey))
                {
                    bool isOneFkSuccess = false;

                    if (!string.IsNullOrWhiteSpace(fkColumnDto.ForeignKeyTableName)
                        && importSettingDto.DictTableNameAndDto.ContainsKey(fkColumnDto.ForeignKeyTableName)
                        )
                    {
                        string fkColumnName = fkColumnDto.Name;
                        DatabaseTable pkTableDto = importSettingDto.DictTableNameAndDto[fkColumnDto.ForeignKeyTableName];
                        DatabaseColumn pkColumn = pkTableDto.Columns.FirstOrDefault(o => o.IsPrimaryKey);

                        if (pkColumn != null)
                        {
                            string query_CreateFk = @"ALTER TABLE [" + tableDto.Name +
                                 "] ADD CONSTRAINT [FK_" + tableDto.Name + "_" + pkTableDto.Name + "_" + ExtensionMethodhelper.RandomId()
                                 + "] FOREIGN KEY ([" + fkColumnName + "]) REFERENCES ["
                                 + pkTableDto.Name + "] ([" + pkColumn.Name + "]);";

                            query += query_CreateFk + Environment.NewLine;

                            isOneFkSuccess = true;
                        }
                    }

                    if (!isOneFkSuccess)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Message,
                            "Import Failed When Generating FK Creation Scrip For Table: " + tableDto.Name + "."));

                        return "";
                    }
                }
            }

            return query;
        }

        private static string ProcessTableImport_GenerateAddKeyColumnsToTempTableScript(DatabaseTableImportSettingDto importSettingDto, List<string> addedKeyColumnNameList)
        {

            string query_addKeyColumns = "";

            var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(importSettingDto.DataSourceRegisterId.Value);

            foreach (var tableDto in importSettingDto.Tables)
            {
                if (tableDto.IsNewTable || importSettingDto.IsUpdateImportedTableDataFromTempTable)
                {
                    var pkColumn = tableDto.Columns.FirstOrDefault(o => o.IsPrimaryKey);

                    if (pkColumn != null)
                    {
                        string keyColumnName = pkColumn.Name;

                        bool isColumnExist = dataBaseFixture.IsColumnExist(importSettingDto.TempTableName, keyColumnName);

                        if (!isColumnExist)
                        {
                            query_addKeyColumns += "ALTER TABLE [" + importSettingDto.TempTableName + "] ADD [" + keyColumnName + "] int null;  \n ";

                        }

                        addedKeyColumnNameList.Add(keyColumnName);
                    }
                }
            }

            return query_addKeyColumns;
        }

        private static string ProcessTableImport_GenerateDropKeyColumnsFromTempTableScript(DatabaseTableImportSettingDto importSettingDto, List<string> addedKeyColumnNameList)
        {

            string query = "";


            foreach (string keyColumnName in addedKeyColumnNameList)
            {
                query += "ALTER TABLE [" + importSettingDto.TempTableName + "] DROP COLUMN [" + keyColumnName + "];  \n ";
            }

            return query;
        }

        private static string ProcessTableImport_GenerateTableDataUpdateScript(DatabaseTableImportSettingDto importSettingDto, ValidationResult aValidationResult)
        {

            string query = "";

            List<string> addedKeyColumnNameList = new List<string>();
            string query_addKeyColumns = ProcessTableImport_GenerateAddKeyColumnsToTempTableScript(importSettingDto, addedKeyColumnNameList);

            query += query_addKeyColumns;


            if (importSettingDto.LevelOneTables.Count > 0)
            {
                var l1_tableDto = importSettingDto.LevelOneTables[0];
                bool isSingleTableImport = importSettingDto.LevelTwoTables.Count == 0;

                query += GenerateUpdateTableDataScript_ProcessOneTable(importSettingDto, l1_tableDto, isSingleTableImport) + Environment.NewLine;

                foreach (var l2_tableDto in importSettingDto.LevelTwoTables)
                {

                    query += Environment.NewLine + GenerateUpdateTableDataScript_ProcessOneTable(importSettingDto, l2_tableDto, isSingleTableImport);

                    foreach (var l3_tableDto in importSettingDto.LevelThreeTables.Where(o => o.NetName == l2_tableDto.Name))
                    {
                        query += Environment.NewLine + GenerateUpdateTableDataScript_ProcessOneTable(importSettingDto, l3_tableDto, isSingleTableImport);
                    }

                }
            }


            string query_dropKeyColumns = ProcessTableImport_GenerateDropKeyColumnsFromTempTableScript(importSettingDto, addedKeyColumnNameList);

            query += query_dropKeyColumns;


            return query;
        }


        private static string ProcessEntityImport_GenerateTableDataUpdateScript(DatabaseTableImportSettingDto importSettingDto, ValidationResult aValidationResult)
        {

            string query = "";

            string query_addKeyColumns = "";

            foreach (var tableDto in importSettingDto.Tables)
            {
                var pkColumn = tableDto.Columns.FirstOrDefault(o => o.IsPrimaryKey);

                if (pkColumn != null)
                {
                    string keyColumnName = pkColumn.Name;
                    query_addKeyColumns += "ALTER TABLE [" + importSettingDto.TempTableName + "] ADD [" + keyColumnName + "] int null ; \n ";
                }
            }

            query += query_addKeyColumns;



            foreach (var tableDto in importSettingDto.Tables.Where(o => !o.IsEntityRelationTable))
            {

                query += Environment.NewLine + GenerateUpdateTableDataScript_ProcessOneTable(importSettingDto, tableDto, false);

            }

            foreach (var tableDto in importSettingDto.Tables.Where(o => o.IsEntityRelationTable))
            {
                query += Environment.NewLine + GenerateUpdateTableDataScript_ProcessOneTable(importSettingDto, tableDto, false);
                //query += Environment.NewLine + GenerateUpdateTableDataScript_ProcessOneEntityRelationTable(importSettingDto, tableDto);
            }

            return query;
        }


        //private static string GenerateInsertDataScript_ProcessOneTable(DatabaseTableImportSettingDto importSettingDto, DatabaseTable tableDto)
        //{
        //    string tempTableName = importSettingDto.TempTableName;

        //    List<string> orgColumnNameList = importSettingDto.DictTableNameAndOrgColumnNameList[tableDto.Name];
        //    //List<string> uniqueKeyColumnNameList = importSettingDto.DictTableNameAndLogicKeyColumnNameList[tableDto.Name];

        //    string table_orgColumnNames = string.Join(", ", orgColumnNameList);


        //    string columnNames_withTempTablePrefix = string.Join(", ", orgColumnNameList.Select(o => "[" + tempTableName + "].[" + o + "]"));

        //    DatabaseTable l1_tableDto = importSettingDto.LevelOneTables[0];

        //    string insertStatement = "";
        //    string selectStatement = "";


        //    string level = tableDto.Tag.ToString();

        //    if (level == "1")
        //    {
        //        insertStatement = "INSERT INTO [" + tableDto.Name + "] \r\n "
        //            + " (" + table_orgColumnNames + ")  \r\n";

        //        selectStatement = "select distinct " + columnNames_withTempTablePrefix + " from [" + tempTableName + "] \r\n";

        //    }
        //    else if (level == "2")
        //    {

        //        string l1_pkName = l1_tableDto.Columns.FirstOrDefault(o => o.IsPrimaryKey).Name;
        //        string l1_pkFullName = "[" + l1_tableDto.Name + "].[" + l1_pkName + "]";

        //        List<string> l1_orgColumnNameList = importSettingDto.DictTableNameAndOrgColumnNameList[l1_tableDto.Name];

        //        insertStatement = "INSERT INTO [" + tableDto.Name + "] \r\n "
        //            + " (" + table_orgColumnNames + ", " + l1_pkName + ")  \r\n";

        //        string join_l1_Expression = string.Join(" and ", l1_orgColumnNameList
        //            .Select(o => "[" + tempTableName + "].[" + o + "] = [" + l1_tableDto.Name + "].[" + o + "]"));

        //        selectStatement = "select " + table_orgColumnNames + ", " + l1_pkName + " from \r\n"
        //            + "("
        //            + "    select distinct " + columnNames_withTempTablePrefix + ", " + l1_pkFullName + " \r\n"
        //            + "    from [" + tempTableName + "] \r\n"
        //            + "        inner join [" + l1_tableDto.Name + "] on (" + join_l1_Expression + ") \r\n"
        //            + ") as temp ";


        //    }
        //    else if (level == "3")
        //    {
        //        string l2_tableName = tableDto.NetName;
        //        DatabaseTable l2_tableDto = importSettingDto.DictTableNameAndDto[l2_tableName];

        //        string l1_pkName = l1_tableDto.Columns.FirstOrDefault(o => o.IsPrimaryKey).Name;
        //        string l1_pkFullName = "[" + l1_tableDto.Name + "].[" + l1_pkName + "]";

        //        string l2_pkName = l2_tableDto.Columns.FirstOrDefault(o => o.IsPrimaryKey).Name;
        //        string l2_pkFullName = "[" + l2_tableDto.Name + "].[" + l2_pkName + "]";

        //        List<string> l1_orgColumnNameList = importSettingDto.DictTableNameAndOrgColumnNameList[l1_tableDto.Name];
        //        List<string> l2_orgColumnNameList = importSettingDto.DictTableNameAndOrgColumnNameList[l2_tableDto.Name];

        //        string join_l1_Expression = string.Join(" and ", l1_orgColumnNameList
        //            .Select(o => "[" + tempTableName + "].[" + o + "] = [" + l1_tableDto.Name + "].[" + o + "]"));

        //        string join_l2_Expression = string.Join(" and ", l2_orgColumnNameList
        //           .Select(o => "[" + tempTableName + "].[" + o + "] = [" + l2_tableDto.Name + "].[" + o + "]"));
        //        join_l2_Expression += " and " + l1_pkFullName + " = " + "[" + l2_tableDto.Name + "].[" + l1_pkName + "]";


        //        insertStatement = "INSERT INTO [" + tableDto.Name + "] \r\n "
        //            + " (" + table_orgColumnNames + ", " + l2_pkName + ")  \r\n";



        //        selectStatement = "select " + table_orgColumnNames + ", " + l2_pkName + " from \r\n"
        //            + "("
        //            + "    select distinct " + columnNames_withTempTablePrefix + ", " + l1_pkFullName + ", " + l2_pkFullName + " \r\n"
        //            + "    from [" + tempTableName + "] \r\n"
        //            + "        inner join [" + l1_tableDto.Name + "] on (" + join_l1_Expression + ") \r\n"
        //            + "        inner join [" + l2_tableDto.Name + "] on (" + join_l2_Expression + ") \r\n"
        //            + ") as temp ";
        //    }



        //    string query_insertOneTable = insertStatement + Environment.NewLine + selectStatement + Environment.NewLine;

        //    return query_insertOneTable;
        //}

        //private static List<DatabaseColumn> GetTransactionDataUpdateOneTableSelectedColumnList(DatabaseTableImportSettingDto importSettingDto, DatabaseTableInfoDto tableDto)
        //{
        //    List<DatabaseColumn> toReturn = new List<DatabaseColumn>();

        //    if (importSettingDto.DictTableNameColumnNameAndSourceColumnNameMapping.ContainsKey(tableDto.Name))
        //    {
        //        foreach (var columnDto in tableDto.Columns)
        //        {
        //            string srcColumnNmae = importSettingDto.DictTableNameColumnNameAndSourceColumnNameMapping[tableDto.Name][columnDto.Name];                                     

        //            if (!string.IsNullOrWhiteSpace(srcColumnNmae))
        //            {
        //                toReturn.Add(columnDto);
        //            }
        //        }



        //    }

        //    return toReturn;
        //}


        private static string GenerateUpdateTableDataScript_ProcessOneTable(DatabaseTableImportSettingDto importSettingDto, DatabaseTableInfoDto tableDto, bool isSingleTableImport)
        {

            if (string.IsNullOrEmpty(importSettingDto.CurrentImportFileName))
            {
                importSettingDto.CurrentImportFileName = "";
            }

            var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(importSettingDto.DataSourceRegisterId.Value);
            string tempTableName = importSettingDto.TempTableName;

            string pkName = importSettingDto.DictTableNameAndPkColumnName[tableDto.Name];

            List<string> targetTable_nonePkColumnNameList = new List<string>();
            List<string> tempTable_nonePkColumnNameList = new List<string>();
            List<string> targetTable_logicKeyColumnNames = new List<string>();
            List<string> tempTable_logicKeyColumnNames = new List<string>();

            if (importSettingDto.NeedToUpdateTransactionId.HasValue)
            {
                var dictColumnMapping = importSettingDto.DictTableNameColumnNameAndSourceColumnNameMapping[tableDto.Name];

                foreach (var columnDto in tableDto.Columns.Where(o => !o.IsPrimaryKey))
                {
                    if (!columnDto.IsPrimaryKey)
                    {
                        if (dictColumnMapping.ContainsKey(columnDto.Name) && !string.IsNullOrWhiteSpace(dictColumnMapping[columnDto.Name]))
                        {
                            targetTable_nonePkColumnNameList.Add(columnDto.Name);
                            tempTable_nonePkColumnNameList.Add(dictColumnMapping[columnDto.Name]);
                        }
                    }

                    if (columnDto.IsLogicKey)
                    {
                        if (dictColumnMapping.ContainsKey(columnDto.Name) && !string.IsNullOrWhiteSpace(dictColumnMapping[columnDto.Name]))
                        {
                            targetTable_logicKeyColumnNames.Add(columnDto.Name);
                            tempTable_logicKeyColumnNames.Add(dictColumnMapping[columnDto.Name]);
                        }
                    }
                }
            }
            else
            {
                targetTable_nonePkColumnNameList = tableDto.Columns.Where(o => !o.IsPrimaryKey).Select(o => o.Name).ToList();
                tempTable_nonePkColumnNameList = targetTable_nonePkColumnNameList;

                targetTable_logicKeyColumnNames = importSettingDto.DictTableNameAndLogicKeyColumnNameList[tableDto.Name];
                tempTable_logicKeyColumnNames = targetTable_logicKeyColumnNames;
            }

            if (targetTable_nonePkColumnNameList.Count == 0)
            {
                return "";
            }


            string targetTable_columnNamesWithSqure = string.Join(", ", targetTable_nonePkColumnNameList.Select(o => "[" + o + "]").ToList());

            string tempTable_nonePkColumnsMaxExpressoinForGroupBy = "";

            foreach (string columnName in tempTable_nonePkColumnNameList)
            {

                if (tempTable_nonePkColumnsMaxExpressoinForGroupBy.Length > 0)
                {
                    tempTable_nonePkColumnsMaxExpressoinForGroupBy += ", ";
                }

                tempTable_nonePkColumnsMaxExpressoinForGroupBy += "max([" + columnName + "]) as [" + columnName + "] ";
            }


            string nonePkColumnsSetExpression = " " + SystemDefinedColumnName_OriginalFile + " = '" + (Regex.Replace(importSettingDto.CurrentImportFileName, @"[^0-9a-zA-Z.:\/\\ -]+(?![.:/\\ -])", "_")) + "' ";

            for (int i = 0; i < targetTable_nonePkColumnNameList.Count; i++)
            {
                string targetTable_columnName = targetTable_nonePkColumnNameList[i];
                string tempTable_columnName = tempTable_nonePkColumnNameList[i];

                if (nonePkColumnsSetExpression.Length > 0)
                {
                    nonePkColumnsSetExpression += ", ";
                }

                nonePkColumnsSetExpression += "[" + tableDto.Name + "].[" + targetTable_columnName + "] = tempTable.[" + tempTable_columnName + "] ";
            }

            if (importSettingDto.IsEntityImport)
            {
                string parentTableName = tableDto.NetName;
                string parentTablePkName = "";
                //string groupByColumns = string.Join(", ", tempTable_logicKeyColumnNames);
                string groupByColumns = string.Join(", ", tempTable_logicKeyColumnNames.Select(o => "[" + o + "]").ToList());


                if (!string.IsNullOrWhiteSpace(parentTableName))
                {
                    parentTablePkName = importSettingDto.DictTableNameAndPkColumnName[parentTableName];

                    if (!importSettingDto.IsHaveConditionFilter)
                    {
                        if (!string.IsNullOrWhiteSpace(groupByColumns))
                        {
                            groupByColumns = "[" + parentTablePkName + "], " + groupByColumns;
                        }
                        else
                        {
                            groupByColumns = "[" + parentTablePkName + "] ";
                        }
                    }

                }

                string updateExitPkStatement = PrepareQuery_UpdateTemplateTablePkForOneTable(importSettingDto, tableDto, tempTableName, pkName, targetTable_logicKeyColumnNames, parentTablePkName, tempTable_logicKeyColumnNames);

                string transformFilterConditionStatement = PrepareQuery_TransformFilterConditionStatement(tableDto);

                string insertNewRowStatement = "INSERT INTO [" + tableDto.Name + "] \r\n "
                    + " (" + targetTable_columnNamesWithSqure + ", " + SystemDefinedColumnName_OriginalFile + ")  \r\n";

                insertNewRowStatement += "select " + tempTable_nonePkColumnsMaxExpressoinForGroupBy
                    + ", '" + (Regex.Replace(importSettingDto.CurrentImportFileName, @"[^0-9a-zA-Z.:\/\\ -]+(?![.:/\\ -])", "_")) + "'"
                    + " from [" + tempTableName + "] as tempTable \r\n"
                    + " where [" + pkName + "] is null \r\n";

                if (!string.IsNullOrWhiteSpace(transformFilterConditionStatement))
                {
                    insertNewRowStatement += " AND " + transformFilterConditionStatement;
                }

                insertNewRowStatement += " group by " + groupByColumns + "; \r\n";






                Dictionary<string, string> dictUpdateTableNameAndAliasName = new Dictionary<string, string>();
                dictUpdateTableNameAndAliasName.Add(tableDto.Name, "targetTable");

                string setValues = nonePkColumnsSetExpression;
                string fromTables = string.Format(@"[{0}] as tempTable inner join [{1}] as targetTable 
	                on (tempTable.[{2}] = targetTable.[{3}])",
                    tempTableName, tableDto.Name, pkName, pkName);
                string whereConditions = "";

                if (!string.IsNullOrWhiteSpace(transformFilterConditionStatement))
                {
                    whereConditions = transformFilterConditionStatement.Trim();
                }

                var sqlWriter = new SqlWriter(tempTableName, dataBaseFixture.SqlServerType.Value);

                string updateExistRowStatement = sqlWriter.BuildUpdateStatement(dictUpdateTableNameAndAliasName, setValues, fromTables, whereConditions);


                updateExistRowStatement += ";\r\n";


                if (importSettingDto.IsHaveConditionFilter && !string.IsNullOrWhiteSpace(parentTableName))
                {
                    string updateFkStatement = PreparetConditionFilterTableScript_UpdateFkStatement(importSettingDto, tableDto, parentTableName, parentTablePkName);
                    updateExistRowStatement += updateFkStatement;
                }


                string query_AddSystemDefinedColumnsToTargetTable = GenerateScript_ProcessOneTable_AddSystemDefinedColumnsToTargetTable(importSettingDto, tableDto);


                string query_updateTableData = query_AddSystemDefinedColumnsToTargetTable + Environment.NewLine;
                query_updateTableData += updateExitPkStatement + Environment.NewLine;
                query_updateTableData += insertNewRowStatement + Environment.NewLine;
                query_updateTableData += updateExistRowStatement + Environment.NewLine;
                query_updateTableData += updateExitPkStatement + Environment.NewLine;

                return query_updateTableData;
            }
            else
            {
                string level = tableDto.Tag.ToString();
                string query_AddSystemDefinedColumnsToTargetTable = GenerateScript_ProcessOneTable_AddSystemDefinedColumnsToTargetTable(importSettingDto, tableDto);

                if (level == "1")
                {


                    //string selectStatement = "";

                    string updateExitPkStatement = PrepareQuery_UpdateTemplateTablePkForOneTable(importSettingDto, tableDto, tempTableName, pkName, targetTable_logicKeyColumnNames, "", tempTable_logicKeyColumnNames);
                    string transformFilterConditionStatement = PrepareQuery_TransformFilterConditionStatement(tableDto);

                    //string groupByColumns = string.Join(", ", tempTable_logicKeyColumnNames);
                    string groupByColumns = string.Join(", ", tempTable_logicKeyColumnNames.Select(o => "[" + o + "]").ToList());

                    string insertNewRowStatement = "INSERT INTO [" + tableDto.Name + "] \r\n "
                        + " (" + targetTable_columnNamesWithSqure + ", " + SystemDefinedColumnName_OriginalFile + ")  \r\n";

                    insertNewRowStatement += "select " + tempTable_nonePkColumnsMaxExpressoinForGroupBy
                        + ", '" + (Regex.Replace(importSettingDto.CurrentImportFileName, @"[^0-9a-zA-Z.:\/\\ -]+(?![.:/\\ -])", "_")) + "'"
                        + " from [" + tempTableName + "] as tempTable  \r\n"
                        + " where [" + pkName + "] is null \r\n";

                    if (!string.IsNullOrWhiteSpace(transformFilterConditionStatement))
                    {
                        insertNewRowStatement += " AND " + transformFilterConditionStatement;
                    }

                    insertNewRowStatement += " group by " + groupByColumns + "; \r\n";




                    Dictionary<string, string> dictUpdateTableNameAndAliasName = new Dictionary<string, string>();
                    dictUpdateTableNameAndAliasName.Add(tableDto.Name, "targetTable");

                    string setValues = nonePkColumnsSetExpression;
                    string fromTables = string.Format(@"[{0}] as tempTable inner join [{1}] as targetTable 
	                on (tempTable.[{2}] = targetTable.[{3}])",
                        tempTableName, tableDto.Name, pkName, pkName);
                    string whereConditions = "";

                    if (!string.IsNullOrWhiteSpace(transformFilterConditionStatement))
                    {
                        whereConditions = transformFilterConditionStatement.Trim();
                    }

                    var sqlWriter = new SqlWriter(tempTableName, dataBaseFixture.SqlServerType.Value);

                    string updateExistRowStatement = sqlWriter.BuildUpdateStatement(dictUpdateTableNameAndAliasName, setValues, fromTables, whereConditions);


                    updateExistRowStatement += ";";



                    string query_updateTableData = query_AddSystemDefinedColumnsToTargetTable + Environment.NewLine;
                    query_updateTableData += updateExitPkStatement + Environment.NewLine;
                    query_updateTableData += insertNewRowStatement + Environment.NewLine;
                    query_updateTableData += updateExistRowStatement + Environment.NewLine;

                    if (!isSingleTableImport)
                    {
                        query_updateTableData += updateExitPkStatement + Environment.NewLine;
                    }


                    return query_updateTableData;
                }
                else if (level == "2")
                {
                    var parentTableDto = importSettingDto.LevelOneTables[0];
                    string parentTableName = parentTableDto.Name;
                    string parentTablePkName = importSettingDto.DictTableNameAndPkColumnName[parentTableName];

                    string updateExitPkStatement = PrepareQuery_UpdateTemplateTablePkForOneTable(importSettingDto, tableDto, tempTableName, pkName, targetTable_logicKeyColumnNames, parentTablePkName, tempTable_logicKeyColumnNames);
                    string transformFilterConditionStatement = PrepareQuery_TransformFilterConditionStatement(tableDto);
                    //string groupByColumns = string.Join(", ", tempTable_logicKeyColumnNames);
                    string groupByColumns = string.Join(", ", tempTable_logicKeyColumnNames.Select(o => "[" + o + "]").ToList());

                    if (!importSettingDto.IsHaveConditionFilter)
                    {
                        if (!string.IsNullOrWhiteSpace(groupByColumns))
                        {
                            groupByColumns = "[" + parentTablePkName + "], " + groupByColumns;
                        }
                        else
                        {
                            groupByColumns = "[" + parentTablePkName + "] ";
                        }
                    }

                    string insertNewRowStatement = "INSERT INTO [" + tableDto.Name + "] \r\n "
                    + " (" + targetTable_columnNamesWithSqure + ", " + SystemDefinedColumnName_OriginalFile + ")  \r\n";

                    insertNewRowStatement += "select " + tempTable_nonePkColumnsMaxExpressoinForGroupBy
                        + ", '" + (Regex.Replace(importSettingDto.CurrentImportFileName, @"[^0-9a-zA-Z.:\/\\ -]+(?![.:/\\ -])", "_")) + "'"
                        + " from [" + tempTableName + "] as tempTable  \r\n"
                        + " where [" + pkName + "] is null \r\n";

                    if (!string.IsNullOrWhiteSpace(transformFilterConditionStatement))
                    {
                        insertNewRowStatement += " AND " + transformFilterConditionStatement;
                    }

                    insertNewRowStatement += " group by " + groupByColumns + "; \r\n";


                    Dictionary<string, string> dictUpdateTableNameAndAliasName = new Dictionary<string, string>();
                    dictUpdateTableNameAndAliasName.Add(tableDto.Name, "targetTable");

                    string setValues = nonePkColumnsSetExpression;
                    string fromTables = string.Format(@"[{0}] as tempTable inner join [{1}] as targetTable 
	                on (tempTable.[{2}] = targetTable.[{3}])",
                        tempTableName, tableDto.Name, pkName, pkName);
                    string whereConditions = "";

                    if (!string.IsNullOrWhiteSpace(transformFilterConditionStatement))
                    {
                        whereConditions = transformFilterConditionStatement.Trim();
                    }

                    var sqlWriter = new SqlWriter(tempTableName, dataBaseFixture.SqlServerType.Value);

                    string updateExistRowStatement = sqlWriter.BuildUpdateStatement(dictUpdateTableNameAndAliasName, setValues, fromTables, whereConditions);


                    updateExistRowStatement += ";\r\n";


                    if (importSettingDto.IsHaveConditionFilter)
                    {
                        string updateFkStatement = PreparetConditionFilterTableScript_UpdateFkStatement(importSettingDto, tableDto, parentTableName, parentTablePkName);
                        updateExistRowStatement += updateFkStatement;
                    }


                    string query_updateTableData = query_AddSystemDefinedColumnsToTargetTable + Environment.NewLine;
                    query_updateTableData += updateExitPkStatement + Environment.NewLine;
                    query_updateTableData += insertNewRowStatement + Environment.NewLine;
                    query_updateTableData += updateExistRowStatement + Environment.NewLine;
                    query_updateTableData += updateExitPkStatement + Environment.NewLine;


                    return query_updateTableData;
                }
                else if (level == "3")
                {

                    string parentTableName = tableDto.NetName;
                    string parentTablePkName = importSettingDto.DictTableNameAndPkColumnName[parentTableName];

                    string updateExitPkStatement = PrepareQuery_UpdateTemplateTablePkForOneTable(importSettingDto, tableDto, tempTableName, pkName, targetTable_logicKeyColumnNames, parentTablePkName, tempTable_logicKeyColumnNames);
                    string transformFilterConditionStatement = PrepareQuery_TransformFilterConditionStatement(tableDto);

                    //string groupByColumns = string.Join(", ", tempTable_logicKeyColumnNames);
                    string groupByColumns = string.Join(", ", tempTable_logicKeyColumnNames.Select(o => "[" + o + "]").ToList());

                    if (!importSettingDto.IsHaveConditionFilter)
                    {
                        if (!string.IsNullOrWhiteSpace(groupByColumns))
                        {
                            groupByColumns = "[" + parentTablePkName + "], " + groupByColumns;
                        }
                        else
                        {
                            groupByColumns = "[" + parentTablePkName + "] ";
                        }
                    }

                    string insertNewRowStatement = "INSERT INTO [" + tableDto.Name + "] \r\n "
                        + " (" + targetTable_columnNamesWithSqure + ", " + SystemDefinedColumnName_OriginalFile + ")  \r\n";

                    insertNewRowStatement += "select " + tempTable_nonePkColumnsMaxExpressoinForGroupBy
                        + ", '" + (Regex.Replace(importSettingDto.CurrentImportFileName, @"[^0-9a-zA-Z.:\/\\ -]+(?![.:/\\ -])", "_")) + "'"
                        + " from [" + tempTableName + "]  as tempTable \r\n"
                        + " where [" + pkName + "] is null \r\n";

                    if (!string.IsNullOrWhiteSpace(transformFilterConditionStatement))
                    {
                        insertNewRowStatement += " AND " + transformFilterConditionStatement;
                    }

                    insertNewRowStatement += " group by " + groupByColumns + "; \r\n";


                    Dictionary<string, string> dictUpdateTableNameAndAliasName = new Dictionary<string, string>();
                    dictUpdateTableNameAndAliasName.Add(tableDto.Name, "targetTable");

                    string setValues = nonePkColumnsSetExpression;
                    string fromTables = string.Format(@"[{0}] as tempTable inner join [{1}] as targetTable 
	                on (tempTable.[{2}] = targetTable.[{3}])",
                        tempTableName, tableDto.Name, pkName, pkName);
                    string whereConditions = "";

                    if (!string.IsNullOrWhiteSpace(transformFilterConditionStatement))
                    {
                        whereConditions = transformFilterConditionStatement.Trim();
                    }

                    var sqlWriter = new SqlWriter(tempTableName, dataBaseFixture.SqlServerType.Value);

                    string updateExistRowStatement = sqlWriter.BuildUpdateStatement(dictUpdateTableNameAndAliasName, setValues, fromTables, whereConditions);


                    updateExistRowStatement += ";\r\n";


                    if (importSettingDto.IsHaveConditionFilter)
                    {
                        string updateFkStatement = PreparetConditionFilterTableScript_UpdateFkStatement(importSettingDto, tableDto, parentTableName, parentTablePkName);
                        updateExistRowStatement += updateFkStatement;
                    }

                    string query_updateTableData = query_AddSystemDefinedColumnsToTargetTable + Environment.NewLine;
                    query_updateTableData += updateExitPkStatement + Environment.NewLine;
                    query_updateTableData += insertNewRowStatement + Environment.NewLine;
                    query_updateTableData += updateExistRowStatement + Environment.NewLine;
                    query_updateTableData += updateExitPkStatement + Environment.NewLine;


                    return query_updateTableData;
                }

            }



            return "";

        }


        private static string GenerateScript_ProcessOneTable_AddSystemDefinedColumnsToTargetTable(DatabaseTableImportSettingDto importSettingDto, DatabaseTableInfoDto tableDto)
        {
            string query = "";

            var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(importSettingDto.DataSourceRegisterId.Value);

            bool isSysColumnExist_OriginalFile = dataBaseFixture.IsColumnExist(tableDto.Name, SystemDefinedColumnName_OriginalFile);

            if (!isSysColumnExist_OriginalFile)
            {
                query += "ALTER TABLE [" + tableDto.Name + "] ADD [" + SystemDefinedColumnName_OriginalFile + "] nvarchar(4000) null; \n ";
            }

            return query;
        }

        private static string PreparetConditionFilterTableScript_UpdateFkStatement(DatabaseTableImportSettingDto importSettingDto, DatabaseTableInfoDto tableDto, string parentTableName, string parentTablePkName)
        {
            var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(importSettingDto.DataSourceRegisterId.Value);

            List<string> parentLogicKeyColumnNames = importSettingDto.DictTableNameAndLogicKeyColumnNameList[parentTableName];
            string updateFk_joinCondition = "";

            foreach (string parentLogicKey in parentLogicKeyColumnNames)
            {
                if (updateFk_joinCondition.Length > 0)
                {
                    updateFk_joinCondition += " AND ";
                }

                updateFk_joinCondition += "updateTable." + parentLogicKey + " = fkTable." + parentLogicKey;
            }


            Dictionary<string, string> dictUpdateTableNameAndAliasName = new Dictionary<string, string>();
            dictUpdateTableNameAndAliasName.Add(tableDto.Name, "updateTable");

            string setValues = string.Format(@"[{0}].[{1}] = fkTable.[{2}]",
                tableDto.Name, parentTablePkName, parentTablePkName);
            string fromTables = string.Format(@"[{0}] as updateTable inner join [{1}] as fkTable on ({2})",
                tableDto.Name, parentTableName, updateFk_joinCondition);
            string whereConditions = "";


            var sqlWriter = new SqlWriter(tableDto.Name, dataBaseFixture.SqlServerType.Value);

            string updateFkStatement = sqlWriter.BuildUpdateStatement(dictUpdateTableNameAndAliasName, setValues, fromTables, whereConditions);


            return updateFkStatement;
        }

        private static string GenerateUpdateTableDataScript_ProcessOneEntityRelationTable(DatabaseTableImportSettingDto importSettingDto, DatabaseTableInfoDto tableDto)
        {
            string tempTableName = importSettingDto.TempTableName;

            string pkName = importSettingDto.DictTableNameAndPkColumnName[tableDto.Name];

            List<string> nonePkColumnNameList = tableDto.Columns.Where(o => !o.IsPrimaryKey).Select(o => o.Name).ToList();

            string nonePkColumnNames = string.Join(", ", nonePkColumnNameList);
            string columnNamesWithSqure = string.Join(", ", nonePkColumnNameList.Select(o => "[" + o + "]").ToList());

            List<string> tempTable_KeyColumnNameList = nonePkColumnNameList.Select(o => "[" + tempTableName + "].[" + o + "]").ToList();
            string tempTable_KeyColumnNames = string.Join(", ", tempTable_KeyColumnNameList);


            string conditionExpression = "";
            foreach (string columnName in nonePkColumnNameList)
            {
                if (conditionExpression.Length > 0)
                {
                    conditionExpression += " AND ";
                }

                conditionExpression += tableDto.Name + ".[" + columnName + "] = " + tempTableName + ".[" + columnName + "] ";
            }





            string insertNewRowStatement = "INSERT INTO [" + tableDto.Name + "] \r\n "
                + " (" + columnNamesWithSqure + ")  \r\n";



            insertNewRowStatement += "select distinct " + tempTable_KeyColumnNames
                + " from [" + tempTableName + "] left join [" + tableDto.Name + "] on (" + conditionExpression + ") \r\n"
                + " where [" + tableDto.Name + "].[" + pkName + "] is null \r\n";

            string query_updateTableData = insertNewRowStatement + Environment.NewLine;

            return query_updateTableData;

        }

        private static string PrepareQuery_UpdateTemplateTablePkForOneTable(DatabaseTableImportSettingDto importSettingDto, DatabaseTableInfoDto tableDto, string tempTableName, string pkName, List<string> targetTable_logicKeyColumnNames, string parentTablePkName, List<string> tempTable_logicKeyColumnNames = null)
        {
            string updateExitPkStatement;
            string joinConditionString = "";
            var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(importSettingDto.DataSourceRegisterId.Value);

            if (tempTable_logicKeyColumnNames == null)
            {
                tempTable_logicKeyColumnNames = targetTable_logicKeyColumnNames;
            }

            if (!string.IsNullOrWhiteSpace(parentTablePkName))
            {
                if (!tableDto.IsManyToManyCascadingChild)
                {
                    joinConditionString += string.Format("tempTable.[{0}] = targetTable.[{1}]",
                            parentTablePkName, parentTablePkName);
                }
            }

            for (int i = 0; i < targetTable_logicKeyColumnNames.Count; i++)
            {
                string targetTable_logicKeyColumnName = targetTable_logicKeyColumnNames[i];
                string tempTable_logicKeyColumnName = tempTable_logicKeyColumnNames[i];

                if (!string.IsNullOrWhiteSpace(joinConditionString))
                {
                    joinConditionString += " and ";
                }

                joinConditionString += string.Format("tempTable.[{0}] = targetTable.[{1}]",
                    tempTable_logicKeyColumnName, targetTable_logicKeyColumnName);
            }

            var sqlWriter = new SqlWriter(tempTableName, dataBaseFixture.SqlServerType.Value);


            Dictionary<string, string> dictUpdateTableNameAndAliasName = new Dictionary<string, string>();
            dictUpdateTableNameAndAliasName.Add(tempTableName, "tempTable");

            string setValues = string.Format(@"[{0}].[{1}] = targetTable.[{2}]", tempTableName, pkName, pkName);
            string fromTables = string.Format(@"[{0}] as tempTable inner join [{1}] as targetTable on ({2})", tempTableName, tableDto.Name, joinConditionString);
            string whereConditions = string.Format(@"targetTable.[{0}]  is not null", pkName);

            string transformFilterConditionStatement = PrepareQuery_TransformFilterConditionStatement(tableDto);
            if (!string.IsNullOrWhiteSpace(transformFilterConditionStatement))
            {
                whereConditions += " AND " + transformFilterConditionStatement.Trim();
            }

            updateExitPkStatement = sqlWriter.BuildUpdateStatement(dictUpdateTableNameAndAliasName, setValues, fromTables, whereConditions);

            updateExitPkStatement += ";";

            return updateExitPkStatement;
        }


        private static string PrepareQuery_TransformFilterConditionStatement(DatabaseTableInfoDto tableDto)
        {
            string toReturn = "";

            if (!string.IsNullOrWhiteSpace(tableDto.TransformCondition))
            {
                List<string> conditionList = tableDto.TransformCondition.ToLower().Split(new string[] { " and " }, StringSplitOptions.None).ToList();

                foreach (string condition in conditionList)
                {
                    if (toReturn.Length > 0)
                    {
                        toReturn += " AND ";
                    }

                    toReturn += "tempTable." + condition;
                }
            }

            return toReturn;
        }

        private static void PrepareNewImportSettingTablesFromTransaction(AppDataSetExDto aAppDataSetExDto)
        {
            DatabaseTableImportSettingDto importSettingDto = aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto;

            var transactionExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(importSettingDto.NeedToUpdateTransactionId.Value);

            aAppDataSetExDto.Name = "Update Data Model: " + transactionExDto.TransactionName + " (" + transactionExDto.Id + ")";

            importSettingDto.Tables = new List<DatabaseTableInfoDto>();

            importSettingDto.DictTableNameColumnNameAndSourceColumnNameMapping = new Dictionary<string, Dictionary<string, string>>();

            if (importSettingDto.Tables.Count == 0)
            {
                if (transactionExDto.RootMasterUnit != null)
                {
                    var rootUnit = transactionExDto.RootMasterUnit;

                    if (importSettingDto.LevelOneTables.Count == 0)
                    {
                        DatabaseTableInfoDto tableDto = PrepareOneTableFromTransactionUnit(importSettingDto, rootUnit, null, 1);
                        importSettingDto.Tables.Add(tableDto);

                    }

                    if (rootUnit.Children != null)
                    {
                        foreach (var childUnit in rootUnit.Children)
                        {
                            DatabaseTableInfoDto childTableDto = PrepareOneTableFromTransactionUnit(importSettingDto, childUnit, rootUnit, 2);
                            importSettingDto.Tables.Add(childTableDto);

                            if (childUnit.Children != null)
                            {
                                foreach (var grandchildUnit in childUnit.Children)
                                {
                                    DatabaseTableInfoDto grandchildTableDto = PrepareOneTableFromTransactionUnit(importSettingDto, grandchildUnit, childUnit, 3);
                                    importSettingDto.Tables.Add(grandchildTableDto);
                                }
                            }
                        }
                    }
                }

                if (importSettingDto.Tables.Count > 1)
                {
                    importSettingDto.IsSpilitToMultipleTables = true;
                }
                //To Do Sibling Unit
            }
        }


        private static void RebuildImportSettingTablesFromTransaction(DatabaseTableImportSettingDto importSettingDto)
        {

            var transactionExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(importSettingDto.NeedToUpdateTransactionId.Value);

            importSettingDto.Tables = new List<DatabaseTableInfoDto>();

            Dictionary<string, Dictionary<string, string>> dictOrgMapping = importSettingDto.DictTableNameColumnNameAndSourceColumnNameMapping;

            importSettingDto.DictTableNameColumnNameAndSourceColumnNameMapping = new Dictionary<string, Dictionary<string, string>>();


            if (importSettingDto.Tables.Count == 0)
            {
                if (transactionExDto.RootMasterUnit != null)
                {
                    var rootUnit = transactionExDto.RootMasterUnit;

                    if (importSettingDto.LevelOneTables.Count == 0)
                    {
                        DatabaseTableInfoDto tableDto = PrepareOneTableFromTransactionUnit(importSettingDto, rootUnit, null, 1, dictOrgMapping);
                        importSettingDto.Tables.Add(tableDto);
                    }

                    if (rootUnit.Children != null)
                    {
                        foreach (var childUnit in rootUnit.Children)
                        {
                            DatabaseTableInfoDto childTableDto = PrepareOneTableFromTransactionUnit(importSettingDto, childUnit, rootUnit, 2, dictOrgMapping);
                            importSettingDto.Tables.Add(childTableDto);

                            if (childUnit.Children != null)
                            {
                                foreach (var grandchildUnit in childUnit.Children)
                                {
                                    DatabaseTableInfoDto grandchildTableDto = PrepareOneTableFromTransactionUnit(importSettingDto, grandchildUnit, childUnit, 3, dictOrgMapping);
                                    importSettingDto.Tables.Add(grandchildTableDto);
                                }
                            }
                        }
                    }
                }

                if (importSettingDto.Tables.Count > 1)
                {
                    importSettingDto.IsSpilitToMultipleTables = true;
                }
                //To Do Sibling Unit
            }
        }

        private static DatabaseTableInfoDto PrepareOneTableFromTransactionUnit(DatabaseTableImportSettingDto importSettingDto, AppTransactionUnitExDto unitDto, AppTransactionUnitExDto parentUnitDto, int level, Dictionary<string, Dictionary<string, string>> dictOrgMapping = null)
        {
            DatabaseTableInfoDto tableDto = new DatabaseTableInfoDto();
            tableDto.Name = unitDto.DataBaseTableName;
            tableDto.SchemaOwner = unitDto.SchemaOwner;
            tableDto.Tag = level.ToString();
            tableDto.IsNewTable = true;

            Dictionary<string, string> dictColumnNameAndSourceColumnName = new Dictionary<string, string>();
            importSettingDto.DictTableNameColumnNameAndSourceColumnNameMapping.Add(tableDto.Name, dictColumnNameAndSourceColumnName);

            foreach (var transFieldDto in unitDto.AppTransactionFieldList)
            {
                if (!(transFieldDto.IsTempVariable.HasValue && transFieldDto.IsTempVariable.Value || transFieldDto.IsStoreToExtendTable.HasValue && transFieldDto.IsStoreToExtendTable.Value))
                {
                    DatabaseColumn columnDto = new DatabaseColumn()
                    {
                        Name = transFieldDto.DataBaseFieldName,
                        Tag = transFieldDto.DataType.HasValue ? ((EmAppDataType)transFieldDto.DataType.Value).ToString() : "String",
                        Nullable = transFieldDto.IsAllowEmpty.HasValue && transFieldDto.IsAllowEmpty.Value,
                        IsPrimaryKey = transFieldDto.IsPrimaryKey,
                        IsAutoNumber = unitDto.IsPrimaryKeyIdentityInsert,
                        IsLogicKey = false,
                        NetName = transFieldDto.EntityId.HasValue ? transFieldDto.EntityId.Value.ToString() : ""
                    };

                    if (parentUnitDto != null && transFieldDto.IsLinkToParentPrimaryKey)
                    {
                        columnDto.IsForeignKey = true;
                        columnDto.IsLogicKey = false;
                        columnDto.ForeignKeyTableName = parentUnitDto.DataBaseTableName;
                    }


                    tableDto.Columns.Add(columnDto);

                    dictColumnNameAndSourceColumnName.Add(transFieldDto.DataBaseFieldName, "");

                    if (dictOrgMapping != null && dictOrgMapping.ContainsKey(tableDto.Name) && dictOrgMapping[tableDto.Name].ContainsKey(transFieldDto.DataBaseFieldName))
                    {
                        dictColumnNameAndSourceColumnName[transFieldDto.DataBaseFieldName] = dictOrgMapping[tableDto.Name][transFieldDto.DataBaseFieldName];
                    }
                }
            }

            return tableDto;
        }


        private static OperationCallResult<bool> DeleteSimulateImportedData(int importSettingDataSetId)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppDataSetExDto dataSetExDto = AppDatabaseErDiagramBL.RetrieveOneErDiagramExDto(importSettingDataSetId);

            if (dataSetExDto.OtherSettingsDto != null && dataSetExDto.OtherSettingsDto.TableImportSettingDto != null)
            {
                DatabaseTableImportSettingDto importSettingDto = dataSetExDto.OtherSettingsDto.TableImportSettingDto;
                DatabaseFixture databaseFixtureInstance = AppMetaDataBL.GetNewInstanceDatbaseFixtureWtihSchmaOwner(importSettingDto.DataSourceRegisterId, null);
                string queryRootDb = "";
                string queryTargetDb = "";

                if (importSettingDto.DefaultSearchId.HasValue)
                {

                    var result = AppSearchConfigBL.DeleteAppSearch(importSettingDto.DefaultSearchId.Value, true, true);

                    //if (!result.IsSuccessful)
                    //{
                    //    aOperationCallResult.ValidationResult.Merge(result.ValidationResult);
                    //    return aOperationCallResult;
                    //}

                    if (!result.IsSuccessful)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "DeleteSearchWarning", ValidationItemType.Warning, "Cannot delete search of previous simulate import."));

                    }

                    queryRootDb += string.Format(@"
                                delete from AppSecurityUserListMenu where MenuID in (select MenuID from AppListMenu where RouteCode = 'MasterDataManagement' and Link = '{0}');
                                delete from AppListMenu where MenuID in (select MenuID from AppListMenu where RouteCode = 'MasterDataManagement' and Link = '{1}');
                            ",
                              importSettingDto.DefaultSearchId.Value,
                              importSettingDto.DefaultSearchId.Value
                              );
                }

                if (importSettingDto.DefaultTransactionId.HasValue)
                {
                    var result = AppTransactionBL.DeleteOneAppTransaction(importSettingDto.DefaultTransactionId.Value, true, false);

                    //if (!result.IsSuccessful)
                    //{
                    //    aOperationCallResult.ValidationResult.Merge(result.ValidationResult);
                    //    return aOperationCallResult;
                    //}

                    if (!result.IsSuccessful)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "DeleteSearchWarning", ValidationItemType.Warning, "Cannot delete data model of previous simulate import."));

                    }
                }





                if (importSettingDto.SimulateImportedTableNames != null && importSettingDto.SimulateImportedTableNames.Count > 0)
                {

                    for (int i = importSettingDto.SimulateImportedTableNames.Count - 1; i >= 0; i--)
                    {
                        string tableName = importSettingDto.SimulateImportedTableNames[i];

                        string droptableStatement = new SqlWriter(tableName, databaseFixtureInstance.SqlServerType.Value).DropTableIfExist();

                        queryTargetDb += droptableStatement + "; \n";


                    }
                }

                if (importSettingDto.TempTableName.HasValue() && importSettingDto.OrgTempTableName.HasValue() && importSettingDto.TempTableName != importSettingDto.OrgTempTableName)
                {

                    string droptableStatement = new SqlWriter(importSettingDto.TempTableName, databaseFixtureInstance.SqlServerType.Value).DropTableIfExist();

                    queryTargetDb += droptableStatement + "; \n";
                }

                if (!string.IsNullOrWhiteSpace(queryRootDb))
                {
                    int? defaultDataSourceRegId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
                    DatabaseFixture databaseFixtureMasterDb = AppMetaDataBL.GetNewInstanceDatbaseFixtureWtihSchmaOwner(defaultDataSourceRegId, null);

                    string errorMsg = AppMetaDataBL.ExecSQlCommand(databaseFixtureMasterDb, queryRootDb);

                    if (!string.IsNullOrWhiteSpace(errorMsg))
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error, "Import Failed. " + errorMsg));
                    }
                }

                if (!string.IsNullOrWhiteSpace(queryTargetDb))
                {

                    string errorMsg = AppMetaDataBL.ExecSQlCommand(databaseFixtureInstance, queryTargetDb);

                    if (!string.IsNullOrWhiteSpace(errorMsg))
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Ok", ValidationItemType.Error, "Import Failed. " + errorMsg));
                    }
                }

                if (!aValidationResult.HasErrors)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_UpdateImportedTableDataFromTempTable_Ok", ValidationItemType.Message, "Import Success."));
                }
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_CreateTableImportSettingAndProcessImport_Error", ValidationItemType.Error, "Change Import Setting Failed."));
            }


            return aOperationCallResult;
        }



        private static void CreateNewTempTableFromOrgTempTable(DatabaseTableImportSettingDto importSettingDto, ValidationResult aValidationResult)
        {
            string query = "";

            //DatabaseTable tempTableDto = AppCacheManagerBL.GetDatabaseTable(importSettingDto.TempTableName, importSettingDto.DataSourceRegisterId, "dbo");

            DatabaseTable orgTempTableDto = AppMetaDataBL.GetOneDatabaseTableSchema(importSettingDto.OrgTempTableName, importSettingDto.DataSourceRegisterId, "");


            List<string> orgTempTableColumnNames = orgTempTableDto.Columns.Select(o => o.Name).ToList();

            string orgTempTableName = importSettingDto.OrgTempTableName;
            string newTempTableName = importSettingDto.OrgTempTableName + ExtensionMethodhelper.RandomId();

            importSettingDto.TempTableName = newTempTableName;

            orgTempTableDto.Name = newTempTableName;
            string createTempTableResultMsg = "";
            bool isRecreateTempTableSuccess = AppMetaDataBL.CreateNewTable(orgTempTableDto, importSettingDto.DataSourceRegisterId, null, out createTempTableResultMsg);

            if (isRecreateTempTableSuccess)
            {
                importSettingDto.NeedToDropTempTableNames.Add(newTempTableName);

                //tempTableDto = AppCacheManagerBL.GetDatabaseTable(importSettingDto.TempTableName, importSettingDto.DataSourceRegisterId, "dbo");
                var tempTableDto = AppMetaDataBL.GetOneDatabaseTableSchema(importSettingDto.TempTableName, importSettingDto.DataSourceRegisterId, "");

                List<DatabaseColumnExDto> ddlColumnList = new List<DatabaseColumnExDto>();

                string insertIntoColumns = string.Join(", ", importSettingDto.OrgSourceColumns.Select(o => o.Name));
                string columnNamesWithSqure = string.Join(", ", importSettingDto.OrgSourceColumns.Select(o => "[" + o.Name + "]"));
                string selectTopValue = "TOP 10";

                if (importSettingDto.IsFinalized)
                {
                    selectTopValue = "";
                }

                query = string.Format(@"
                            INSERT INTO [{0}] ({1})                               
                            SELECT {2} {3} FROM [{4}];

                            ",
                   newTempTableName, columnNamesWithSqure,
                   selectTopValue, columnNamesWithSqure, orgTempTableName);


                DatabaseFixture databaseFixtureInstance = AppMetaDataBL.GetNewInstanceDatbaseFixtureWtihSchmaOwner(importSettingDto.DataSourceRegisterId, null);

                if (databaseFixtureInstance.SqlServerType == EmSqlType.MySql)
                {
                    if (!string.IsNullOrWhiteSpace(selectTopValue))
                    {
                        query = query.Replace("TOP 10", " ").Replace(";", " ") + "  limit 0,10 ;";
                    }


                }

                string errorMsg = AppMetaDataBL.ExecSQlCommand(databaseFixtureInstance, query);


                if (!string.IsNullOrWhiteSpace(errorMsg))
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Error", ValidationItemType.Error, "Import Failed. " + errorMsg));
                }
                else
                {

                }
            }
            else
            {
                string errorMsg1 = "Faild to recreate temp table with modified data type.";
                aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Error", ValidationItemType.Error, "Import Failed. " + errorMsg1));
            }
        }


        internal static void DeleteTempTable(int? dataSourceRegisterId, string tempTalbeName, ValidationResult aValidationResult)
        {


            if (!string.IsNullOrWhiteSpace(tempTalbeName) && dataSourceRegisterId.HasValue)
            {
                DatabaseFixture databaseFixture = AppMetaDataBL.GetNewInstanceDatbaseFixtureWtihSchmaOwner(dataSourceRegisterId, null);
                SqlWriter sqlWriter = new SqlWriter(tempTalbeName, databaseFixture.SqlServerType.Value);
                string query_droptable = sqlWriter.DropTableIfExist();

                string errorMsg = AppMetaDataBL.ExecSQlCommand(databaseFixture, query_droptable);


                if (!string.IsNullOrWhiteSpace(errorMsg))
                {
                    //aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_DataSetEntity_ProcessTableImportFromSetting_Error", ValidationItemType.Error, "Drop Temptable Failed. " + errorMsg));
                }
                else
                {

                }
            }
        }



        private static void ConvertImportSettingToTransactionDataUpdateSetting(DatabaseTableImportSettingDto importSettingDto)
        {
            if (importSettingDto.IsFinalized && importSettingDto.DefaultTransactionId.HasValue)
            {
                importSettingDto.DictTableNameColumnNameAndSourceColumnNameMapping = new Dictionary<string, Dictionary<string, string>>();

                importSettingDto.NeedToUpdateTransactionId = importSettingDto.DefaultTransactionId;

                foreach (var tableDto in importSettingDto.Tables)
                {
                    Dictionary<string, string> dictColumnMapping = new Dictionary<string, string>();
                    importSettingDto.DictTableNameColumnNameAndSourceColumnNameMapping.Add(tableDto.Name, dictColumnMapping);

                    foreach (var columnDto in tableDto.Columns)
                    {
                        if (!columnDto.IsPrimaryKey && !columnDto.IsForeignKey)
                        {
                            dictColumnMapping.Add(columnDto.Name, columnDto.Name);
                        }
                    }
                }
            }
        }

        private static void ProcessMetaDataTablesImport_InitializeImportSetting(AppDataSetExDto aAppDataSetExDto)
        {
            if (aAppDataSetExDto.OtherSettingsDto != null && aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto != null)
            {
                var importSettingDto = aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto;
                importSettingDto.NeedToDropTempTableNames = new List<string>();

                int dataSourceFrom = aAppDataSetExDto.DataSourceFrom.Value;

                if (aAppDataSetExDto.DataSourceFrom.HasValue)
                {
                    var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(aAppDataSetExDto.DataSourceFrom.Value);

                    foreach (var table in importSettingDto.Tables)
                    {
                        table.SchemaOwner = dataBaseFixture.CurrentOwner;
                    }
                }

                if (string.IsNullOrWhiteSpace(importSettingDto.OrgTempTableName))
                {
                    importSettingDto.OrgTempTableName = importSettingDto.TempTableName;
                }

            }
        }


        private static DataTable ProcessDbToDbTableImport_PopulateSourceDataTable(ValidationResult aValidationResult, DatabaseTableImportSettingDto importSettingDto, DatabaseFixture srcFixture)
        {
            DataTable srcDataTable = null;
            string srcQuery = "";

            if (importSettingDto.SourceDataSourceType.Value == (int)EmAppDbToDbImportSourceType.DatabaseTable && !string.IsNullOrWhiteSpace(importSettingDto.SourceTableName))
            {
                srcQuery = $@"SELECT * FROM [{importSettingDto.SourceTableName}]";
            }
            else if (importSettingDto.SourceDataSourceType.Value == (int)EmAppDbToDbImportSourceType.DataSet && importSettingDto.SourceDataSetId.HasValue)
            {
                var sourceDataSetDto = AppDataSetBL.RetrieveOneAppDataSetExDto(importSettingDto.SourceDataSetId.Value);


                if (sourceDataSetDto.QueryType.HasValue && sourceDataSetDto.QueryType.Value == (int)EmAppDataServiceType.QueryText
                    && !string.IsNullOrWhiteSpace(sourceDataSetDto.QueryText))
                {
                    srcQuery = sourceDataSetDto.QueryText;
                }
                else
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_CreateDbToDbTableImportSetting_Error", ValidationItemType.Error, "Dataset Query Text Is Empty."));
                }
            }

            if (!string.IsNullOrWhiteSpace(srcQuery))
            {
                List<DbParameter> sqlParamterList = new List<DbParameter>();
                srcDataTable = srcFixture.RetriveDataTable(srcQuery, sqlParamterList);
            }

            return srcDataTable;
        }


        private static DataTable ProcessDbToDbTableImport_PopulateOneFkMappingDataTable(DatabaseFixture targetFixture, UpdateByFkTableMappingDto fkMappingDto)
        {
            DataTable dataTable = null;


            if (fkMappingDto != null && !string.IsNullOrWhiteSpace(fkMappingDto.FkTableName)
                && !string.IsNullOrWhiteSpace(fkMappingDto.OrgValueColumnName)
                && !string.IsNullOrWhiteSpace(fkMappingDto.NewValueColumnName))
            {
                string query = $@"SELECT DISTINCT [{fkMappingDto.OrgValueColumnName}] as OrgKey, [{fkMappingDto.NewValueColumnName}] as NewKey FROM [{fkMappingDto.FkTableName}]";

                List<DbParameter> sqlParamterList = new List<DbParameter>();
                dataTable = targetFixture.RetriveDataTable(query, sqlParamterList);
            }

            return dataTable;
        }

        private static AppDataSetExDto PostImportProcess(AppDataSetExDto aAppDataSetExDto, OperationCallResult<AppDataSetExDto> aOperationCallResult, ValidationResult aValidationResult, DatabaseTableImportSettingDto importSettingDto)
        {
            if (!aValidationResult.HasErrors)
            {
                OperationCallResult<AppDataSetExDto> saveSettingResult = PostImportProcess_BuildErDiagram(aAppDataSetExDto, importSettingDto);

                if (saveSettingResult.IsSuccessfulWithResult)
                {
                    aAppDataSetExDto = saveSettingResult.Object;

                    if (importSettingDto.IsFlatSingleTableImport
                        || (aAppDataSetExDto.UsageTypeId.HasValue && aAppDataSetExDto.UsageTypeId.Value == (int)EmAppDataSetUsageType.DbToDbTableImportSetting))
                    {
                        PostImportProcess_AddTableToApplication(aAppDataSetExDto, importSettingDto);
                    }
                    else
                    {

                        // Create Data Model and Form
                        GenerateDataModelAndFormWithNavigationSearchMenu(aAppDataSetExDto, aValidationResult, importSettingDto);

                        if (!aValidationResult.HasErrors && importSettingDto.IsNeedToCreateImportApi)
                        {
                            CreateImportApiFromSetting(aAppDataSetExDto, aValidationResult, importSettingDto);
                        }
                    }

                    OperationCallResult<AppDataSetExDto> result = PostImportProcess_SaveImportSetting(aAppDataSetExDto, importSettingDto);

                    if (result.IsSuccessfulWithResult)
                    {
                        aAppDataSetExDto = PostImportProcess_UpdateTransactionImportSettingId(aOperationCallResult, importSettingDto, result);
                    }


                }
                else
                {
                    aValidationResult.Merge(saveSettingResult.ValidationResult);
                }
            }

            importSettingDto.NeedToDropTempTableNames.ForAll(o => DeleteTempTable(importSettingDto.DataSourceRegisterId, o, aValidationResult));
            return aAppDataSetExDto;
        }

        private static AppDataSetExDto PostImportProcess_UpdateTransactionImportSettingId(OperationCallResult<AppDataSetExDto> aOperationCallResult, DatabaseTableImportSettingDto importSettingDto, OperationCallResult<AppDataSetExDto> result)
        {
            AppDataSetExDto aAppDataSetExDto;
            aOperationCallResult.Object = aAppDataSetExDto = result.Object;

            if (importSettingDto.NeedToUpdateTransactionId.HasValue)
            {
                int? importSettingId = ControlTypeValueConverter.ConvertValueToInt(aAppDataSetExDto.Id);

                if (importSettingId.HasValue)
                {
                    var transactionExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(importSettingDto.NeedToUpdateTransactionId.Value);

                    if (transactionExDto.OtherOptions == null)
                    {
                        transactionExDto.OtherOptions = new TransactionOptionDto();
                    }

                    transactionExDto.OtherOptions.TransactionDataUpdateImportSettingId = importSettingId.Value;

                    var saveTransactionResult = AppTransactionBL.SaveAppTransactionExDto(transactionExDto);

                    if (saveTransactionResult.ValidationResult.HasErrors)
                    {
                        aOperationCallResult.ValidationResult.Merge(saveTransactionResult.ValidationResult);
                    }
                }
            }

            return aAppDataSetExDto;
        }

        private static OperationCallResult<AppDataSetExDto> PostImportProcess_SaveImportSetting(AppDataSetExDto aAppDataSetExDto, DatabaseTableImportSettingDto importSettingDto)
        {
            importSettingDto.IsDataImported = true;
            aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto = importSettingDto;
            aAppDataSetExDto.IsModified = true;

            foreach (var tableDto in importSettingDto.Tables)
            {
                tableDto.IsNewTable = false;
                tableDto.DictNewColumnNameAndDto = null;
            }

            if (importSettingDto.IsFinalized && importSettingDto.DefaultTransactionId.HasValue)
            {
                ConvertImportSettingToTransactionDataUpdateSetting(importSettingDto);
            }

            var result = AppDatabaseErDiagramBL.SaveOneErDiagramExDto(aAppDataSetExDto);
            return result;
        }

        private static void PostImportProcess_AddTableToApplication(AppDataSetExDto aAppDataSetExDto, DatabaseTableImportSettingDto importSettingDto)
        {
            if (importSettingDto.Tables.Count > 0 && aAppDataSetExDto.SaasApplicationId.HasValue)
            {
                Dictionary<string, int> dictDbObjNameAndUsageType = new Dictionary<string, int>();

                foreach (var tableObj in importSettingDto.Tables)
                {
                    string tableName = tableObj.Name.ToLower().Trim();
                    dictDbObjNameAndUsageType.Add(tableName, aAppDataSetExDto.SaasApplicationId.Value);

                }

                AppDataSetBL.AddDatabaseObjectsToApplication(dictDbObjNameAndUsageType, aAppDataSetExDto.SaasApplicationId.Value);
            }
        }

        private static OperationCallResult<AppDataSetExDto> PostImportProcess_BuildErDiagram(AppDataSetExDto aAppDataSetExDto, DatabaseTableImportSettingDto importSettingDto)
        {
            if (aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo == null)
            {
                aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo = new DatabaseViewDto();
                aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo.IsErDiagram = true;
                aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo.DataSourceRegisterId = aAppDataSetExDto.DataSourceFrom;

                aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo.DictTables = new Dictionary<string, DatabaseViewTableDto>(StringComparer.OrdinalIgnoreCase);
            }

            var diagramDto = aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo;

            ViewTableAddRemoveDto viewTableAddDto = new ViewTableAddRemoveDto(diagramDto);

            viewTableAddDto.NeedToAddOwnerTablePairList = new List<KeyValuePair<string, string>>();

            foreach (var tableDto in importSettingDto.Tables)
            {
                AppCacheManagerBL.RefreshOneTableCache(tableDto.Name, aAppDataSetExDto.DataSourceFrom, tableDto.SchemaOwner);
                viewTableAddDto.NeedToAddOwnerTablePairList.Add(new KeyValuePair<string, string>(tableDto.SchemaOwner, tableDto.Name));
            }

            diagramDto = aAppDataSetExDto.OtherSettingsDto.DatabaseDiagramInfo = AppDatabaseViewBL.AddTablesToDatabaseView(viewTableAddDto);

            ResetDiagramTablePositionByLevel(importSettingDto, diagramDto);

            var saveSettingResult = AppDatabaseErDiagramBL.SaveOneErDiagramExDto(aAppDataSetExDto);
            return saveSettingResult;
        }


        private static void ProcessDbToDbTableImport_ProcessOneTable(
            AppDataSetExDto aAppDataSetExDto,
            ValidationResult aValidationResult,
            DatabaseFixture targetFixture,
            DataTable srcDataTable,
            DatabaseTableInfoDto targerTableInfoDto,
            DatabaseTableInfoDto targerParentTableInfoDto)
        {
            //  DatabaseTable targetDatabaseTable;

            DatabaseTableImportSettingDto importSettingDto = aAppDataSetExDto.OtherSettingsDto.TableImportSettingDto;




            Dictionary<string, string> dictTargetColumnNameAndSourceColumnName;
            List<string> srcLogicalkeyColumn;
            Dictionary<string, DataRow> dictTargetLogicalKeyDataRow;

            Dictionary<string, Dictionary<string, string>> dictTargetColumnName_DictOrgKeyAndNewKey;

            PrepareMappingColumn(targetFixture, targerTableInfoDto, out dictTargetColumnNameAndSourceColumnName, out srcLogicalkeyColumn, out dictTargetLogicalKeyDataRow, targerParentTableInfoDto);

            PrepareUpdateByFkTableMappingDictionary(targetFixture, targerTableInfoDto, out dictTargetColumnName_DictOrgKeyAndNewKey);

            DbProviderFactory factory = targetFixture.DbProviderFactory;

            using (var connection = factory.CreateConnection())
            {
                connection.ConnectionString = targetFixture.ConnectionString;
                connection.Open();

                List<string> processedSourceRowKeyList = new List<string>();

                foreach (System.Data.DataRow srcDataRow in srcDataTable.Rows)
                {
                    PorcessOneDataSourceRow(aValidationResult, targetFixture, targerTableInfoDto, dictTargetColumnNameAndSourceColumnName, srcLogicalkeyColumn, dictTargetLogicalKeyDataRow, connection, srcDataRow, processedSourceRowKeyList, dictTargetColumnName_DictOrgKeyAndNewKey);
                }
            }

            var chlidTableInfoDtoList = importSettingDto.Tables.Where(o => !string.IsNullOrWhiteSpace(o.NetName) && o.NetName.ToLower() == targerTableInfoDto.Name.ToLower()).ToList();

            if (chlidTableInfoDtoList.Count > 0)
            {
                SyncPkToSrcDataTableMappingToPkColumns(srcDataTable, targetFixture, targerTableInfoDto, out dictTargetColumnNameAndSourceColumnName, out srcLogicalkeyColumn, out dictTargetLogicalKeyDataRow, targerParentTableInfoDto);

                foreach (var childTableInfoDto in chlidTableInfoDtoList)
                {
                    ProcessDbToDbTableImport_ProcessOneTable(aAppDataSetExDto, aValidationResult, targetFixture, srcDataTable, childTableInfoDto, targerTableInfoDto);

                }
            }
        }



        //private static void ProcessOneDataSourceRowChlidTargetTable(ValidationResult aValidationResult, DatabaseFixture targetFixture, DatabaseTableInfoDto targerTableDto, int targetDataSourceId, DbConnection connection, List<string> processedSourceRowKeyList, DataRow srcDataRow, Dictionary<string, object> dictPkAndValue, DatabaseTableInfoDto childTableInfoDto)
        //{
        //    var targetDatabaseTable = AppCacheManagerBL.GetDatabaseTable(childTableInfoDto.Name, targetDataSourceId, childTableInfoDto.SchemaOwner);

        //    Dictionary<string, string> dictTargetColumnNameAndSourceColumnName;
        //    List<string> srcLogicalkeyColumn;
        //    Dictionary<string, DataRow> dicttargetLogicalKeyDataRow;

        //    PrepareMappingColumn(targetFixture, targerTableDto, out dictTargetColumnNameAndSourceColumnName, out srcLogicalkeyColumn, out dicttargetLogicalKeyDataRow);


        //    PorcessOneDataSourceRow(aValidationResult, targetFixture, targetDatabaseTable, dictTargetColumnNameAndSourceColumnName, srcLogicalkeyColumn,
        //        dicttargetLogicalKeyDataRow, connection, srcDataRow, processedSourceRowKeyList, dictPkAndValue);
        //}
    }
}