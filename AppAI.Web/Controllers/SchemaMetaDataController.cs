using System;
using System.Collections.Generic;
using System.Linq;
using APP.Components.Dto;
using APP.Components.EntityDto;
using App.BL;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL.EntityClasses;
using DatabaseSchemaMrg.DataSchema;
using DatabaseSchemaMrg;
using Microsoft.AspNetCore.Mvc;
using AppAI.Web.Controllers.Base;

namespace AppAI.Web.Controllers;

[Route("webapi/[controller]/[action]")]
public class SchemaMetaDataController : SecureBaseController
{
    public static readonly string SqlProvideName = "System.Data.SqlClient";

    [HttpGet]
    public List<string> GetDataBaseSchemaOwnerList(int? dataSourceRegisterId)
    {
        return AppMetaDataBL.GetDataBaseSchemaOwnerList(dataSourceRegisterId);
    }

    [HttpGet]
    public TableDataDto GetInstalledDbDriver()
    {
        return AppMetaDataBL.GetInstalledDbDriver();
    }

    [HttpGet]
    public List<DatabaseTableDto> GetDataSourceTableAndViewList(int? dataSourceRegisterId, int? saasFilterOption, int? filterByApplicationId)
    {
        var toReturn = AppMetaDataBL.GetSaasDataSourceTableAndViewList(dataSourceRegisterId, saasFilterOption, filterByApplicationId).OrderBy(o => o.SchemaOwner).ThenBy(o => o.Name).ToList();

        return toReturn;
    }

    // need to drop this fure
    [HttpGet]
    public string RefreshAllCustomerDbRegAndFixtureCache()
    {
        try
        {
            //AppCacheManagerBL.RefreshAllCustomerDbRegAndFixtureCache();

            return "Refresh Schema Cache Successfuly  ";
        }
        catch (Exception ex)
        {
            return "Refresh Schema Cache Failed:" + ex.ToString();
        }
    }

    // this method always get from real time db scheme
    [HttpGet]
    public DatabaseTable GetOneDatabaseTableSchema(string tableName, int? dataSourceRegisterId, string schemaOwner)
    {
        DatabaseTable dbTable = AppMetaDataBL.GetOneDatabaseTableSchema(tableName, dataSourceRegisterId, schemaOwner);

        return dbTable;
    }

    [HttpPost]
    public Dictionary<string, DatabaseTable> GetDatabaseTableSchemaDictionaryBySchemaOwnerTableNames(TableNamesToSchemaConverterDto tableNamesToSchemaConverterDto)
    {
        return AppMetaDataBL.GetDatabaseTableSchemaDictionaryBySchemaOwnerTableNames(tableNamesToSchemaConverterDto.SchemaOwnerAndTableNamePairList, tableNamesToSchemaConverterDto.DataSourceRegisterId);
    }

    [HttpGet]
    public bool RenameTableName(string orgTableName, string newTableName, int? dataSourceRegisterId, string schemaOwner)
    {
        return AppMetaDataBL.RenameTableName(orgTableName, newTableName, dataSourceRegisterId, schemaOwner);
    }

    [HttpPost]
    public OperationCallResult<bool> CreateNewTable(DatabaseTableInfoDto databaseTable)
    {
        OperationCallResult<bool> toReturn = new OperationCallResult<bool>();
        string resultMsg = "";
        toReturn.Object = AppMetaDataBL.CreateNewTable(databaseTable, databaseTable.DataSourceRegisterId, databaseTable.ApplicationId, out resultMsg);
        if (!string.IsNullOrWhiteSpace(resultMsg))
        {
            if (toReturn.Object)
            {
                toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "App_CreateNewTable_Success", ValidationItemType.Message, resultMsg));
            }
            else
            {
                toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "App_CreateNewTable_Error", ValidationItemType.Error, resultMsg));
            }
        }

        return toReturn;
    }

    [HttpPost]
    public OperationCallResult<DatabaseTable> SaveModifiedTableSchema(SchemaMetaDataDto schemaMetaDataDto)
    {
        // schemaMetaDataDto.DataSourceRegisterId = schemaMetaDataDto.DataSourceRegId;
        return AppMetaDataBL.SaveModifiedTableSchema(schemaMetaDataDto, schemaMetaDataDto.DataSourceRegisterId);
    }

    [HttpPost]
    public bool DropDatabaseTable(DatabaseTable databaseTable)
    {
        // need to redo. use a new Dto to handle 2 parameters.
        if (databaseTable != null)
        {
            AppMetaDataBL.DropDatabaseTable(databaseTable);
            return true;
        }

        return false;
    }

    [HttpGet]
    public List<string> GetSysStoredProcedureNameList(string prefix, int? dataSourceRegisterId)
    {
        if (dataSourceRegisterId.HasValue)
        {
            return AppMetaDataBL.GetSysStoredProcedureNameList(prefix, dataSourceRegisterId);
        }

        return null;
    }

    [HttpGet]
    public List<AppDataSetParameterDto> GetStoredProcedureParamterList(string storeProcName, int? externalSourceFrom, int? dataSourceRegisterId)
    {
        if (externalSourceFrom.HasValue && externalSourceFrom.Value == (int)EmAppExternalSourceFrom.ExternalMethod)
        {
            List<AppDataSetParameterDto> toReturn = new List<AppDataSetParameterDto>();
            AppExternalMethodRegisterExDto aAppExternalMethodRegisterExDto = AppExternalMethodRegisterBL.RetrieveOneAppExternalMethodRegisterExDto(storeProcName);

            foreach (var parameterName in aAppExternalMethodRegisterExDto.InputParameterList.Split("|".ToCharArray()))
            {
                AppDataSetParameterDto aAppDataSetParameterDto = new AppDataSetParameterDto();
                aAppDataSetParameterDto.ParameterName = parameterName as string;
                toReturn.Add(aAppDataSetParameterDto);
            }

            return toReturn;
        }
        else
        {
            return AppMetaDataBL.GetStoredProcedureParamterList(storeProcName, dataSourceRegisterId);
        }
    }

    [HttpGet]
    public string GetSelectStatement(string tableName, int? dataSourceRegisterId, string schemaOwner)
    {
        return AppMetaDataBL.GetSelectStatement(tableName, dataSourceRegisterId, schemaOwner);
    }

    [HttpGet]
    public List<LookupItemDto> GetStoredProcedureResultFields(string storeProcName, int? externalSourceFrom, int? dataSourceRegisterId, string schemaOwner)
    {
        if (externalSourceFrom.HasValue && externalSourceFrom.Value == (int)EmAppExternalSourceFrom.ExternalMethod)
        {
            List<LookupItemDto> toReturn = new List<LookupItemDto>();
            List<string> outputFieldList = AppExternalMethodRegisterBL.GetExternalMethodOutputFieldList(storeProcName);

            foreach (var outputFieldName in outputFieldList)
            {
                LookupItemDto aLookupItemDto = new LookupItemDto();
                aLookupItemDto.Id = outputFieldName;
                aLookupItemDto.Display = outputFieldName;
                toReturn.Add(aLookupItemDto);
            }

            return toReturn;
        }
        else
        {
            return AppMetaDataBL.GetStoredProcedureResultFields(storeProcName, dataSourceRegisterId);
        }
    }

    [HttpGet]
    public List<String> GetReadonlyTableTypes(int? dataSourceRegisterId)
    {
        return new List<string>();
        //  return AppMetaDataBL.GetReadonlyTableTypes( dataSourceRegisterId);
    }

    [HttpGet]
    public List<LookupItemDto> GetReadonlyTableTypeColumns(string tableTypeName, int? dataSourceRegisterId)
    {
        return AppMetaDataBL.GetReadonlyTableTypeColumns(tableTypeName, dataSourceRegisterId);
    }

    [HttpPost]
    public DatabaseViewDto UpdateDatabaseViewDtoFromQuery(DatabaseViewDto databaseViewDto)
    {
        return AppDatabaseViewBL.UpdateDatabaseViewDtoFromQuery(databaseViewDto);
    }

    [HttpPost]
    public DatabaseViewDto UpdateDatabaseViewSelectedColumns(DatabaseViewDto databaseViewDto)
    {
        return AppDatabaseViewBL.UpdateDatabaseViewSelectedColumns(databaseViewDto);
    }

    [HttpPost]
    public DatabaseViewDto AddTablesToDatabaseView(ViewTableAddRemoveDto viewTableAddRemoveDto)
    {
        return AppDatabaseViewBL.AddTablesToDatabaseView(viewTableAddRemoveDto);
    }

    [HttpPost]
    public DatabaseViewDto RemoveTablesFromDatabaseView(ViewTableAddRemoveDto viewTableAddRemoveDto)
    {
        return AppDatabaseViewBL.RemoveTablesFromDatabaseView(viewTableAddRemoveDto);
    }

    [HttpPost]
    public DatabaseViewDto AddOneJoinConditionLineToDatabaseView(ViewJoinUpdateDto viewJoinUpdateDto)
    {
        return AppDatabaseViewBL.AddOneJoinConditionLineToDatabaseView(viewJoinUpdateDto);
    }

    [HttpPost]
    public DatabaseViewDto RemoveJoinConditionLinesFromDatabaseView(ViewJoinUpdateDto viewJoinUpdateDto)
    {
        return AppDatabaseViewBL.RemoveJoinConditionLinesFromDatabaseView(viewJoinUpdateDto);
    }

    [HttpPost]
    public DatabaseViewDto AddOneFkLineToErDiagram(ViewJoinUpdateDto viewJoinUpdateDto)
    {
        return AppDatabaseViewBL.AddOneFkLineToErDiagram(viewJoinUpdateDto);
    }

    [HttpPost]
    public DatabaseViewDto RemoveFkLinesFromErDiagram(ViewJoinUpdateDto viewJoinUpdateDto)
    {
        return AppDatabaseViewBL.RemoveFkLinesFromErDiagram(viewJoinUpdateDto);
    }

    [HttpPost]
    public DatabaseViewDto UpdateDatabaseViewJoinMethod(ViewJoinUpdateDto viewJoinUpdateDto)
    {
        return AppDatabaseViewBL.UpdateDatabaseViewJoinMethod(viewJoinUpdateDto);
    }

    [HttpPost]
    public OperationCallResult<DatabaseViewUpdateDto> SaveDataSetAndCreateSearchView(DatabaseViewUpdateDto databaseViewUpdateDto)
    {
        return AppDatabaseViewBL.SaveDataSetAndCreateSearchView(databaseViewUpdateDto);
    }

    [HttpPost]
    public OperationCallResult<DatabaseViewUpdateDto> SaveDataSetAndCreateFolderViewNavigation(DatabaseViewUpdateDto databaseViewUpdateDto)
    {
        return AppDatabaseViewBL.SaveDataSetAndCreateFolderViewNavigation(databaseViewUpdateDto);
    }

    //http://localhost/AppWeb/webapi/Meta/GetTableData
    [HttpGet]
    public TableDataDto GetTableData(string tableName, int? dataSourceRegisterId, string schemaOwner, int? recordLimit = null)
    {
        //base.InitializeSecurity();

        return AppMetaDataBL.GetTableData(tableName, dataSourceRegisterId, schemaOwner, recordLimit);
    }

    //http://localhost/AppWeb/webapi/Meta/GetTableData
    [HttpPost]
    public TableDataDto ExcuteQueryCheckSqlSynTaxResult(KeyValuePair<int?, string> dataSourceRegisterIdQuery)
    {
        //base.InitializeSecurity();

        return AppMetaDataBL.ExcuteQueryCheckSqlSynTaxResult(dataSourceRegisterIdQuery);
    }

    //http://localhost/AppWeb/webapi/Meta/GetTableData
    [HttpPost]
    public TableDataDto ExcuteQueryResult(KeyValuePair<int?, string> dataSourceRegisterIdQuery)
    {
        //base.InitializeSecurity();

        return AppMetaDataBL.ExcuteQueryResult(dataSourceRegisterIdQuery);
    }

    [HttpPost]
    public TableDataDto PreviewQueryResult(KeyValuePair<int?, string> dataSourceRegisterIdQuery)
    {
        //base.InitializeSecurity();

        return AppMetaDataBL.ExcuteQueryResult(dataSourceRegisterIdQuery, 100);
    }

    [HttpGet]
    public string GetViewQueryText(string viewName)
    {
        if (!string.IsNullOrWhiteSpace(viewName))
        {
            return AppMetaDataBL.GetViewQueryText(viewName);
        }

        return null;
    }

    [HttpPost]
    public TableDataDto SaveDatabaseViewFromDesignQuery(DatabaseViewDto databaseViewDto)
    {
        if (databaseViewDto != null)
        {
            return AppMetaDataBL.SaveDatabaseViewFromDesignQuery(databaseViewDto.ViewName, databaseViewDto.QueryString, databaseViewDto.IsNewView, databaseViewDto.ApplicationId);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<DatabaseViewUpdateDto> QuickGenerateTransactionDefaultSeachNavigation(int? transactionId)
    {
        if (transactionId.HasValue)
        {
            return AppDatabaseViewBL.QuickGenerateTransactionDefaultSeachNavigation(transactionId.Value);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<AppDataSetExDto> ResetTransactionDefaultNavigationSearchDataSet(int? transactionId)
    {
        if (transactionId.HasValue)
        {
            return AppDatabaseViewBL.ResetTransactionDefaultNavigationSearchDataSet(transactionId.Value);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppDataSetExDto> CreateTableImportSettingAndProcessImport(AppDataSetExDto aAppDataSetExDto)
    {
        return AppDatabaseTableImportBL.ProcessMetaDataTablesImport(aAppDataSetExDto);
    }

    [HttpPost]
    public OperationCallResult<AppDataSetExDto> SaveDraftTableImportSetting(AppDataSetExDto aAppDataSetExDto)
    {
        return AppDatabaseTableImportBL.SaveDraftTableImportSetting(aAppDataSetExDto);
    }

    [HttpPost]
    public OperationCallResult<AppDataSetExDto> ResetDbToDbImportSourceColumns(AppDataSetExDto aAppDataSetExDto)
    {
        return AppDatabaseTableImportBL.ResetDbToDbImportSourceColumns(aAppDataSetExDto);
    }

    [HttpGet]
    public OperationCallResult<bool> UpdateImportedTableDataFromTempTable(int? importSettingDataSetId, string uploadedFileTempTableName, string fileName)
    {
        if (importSettingDataSetId.HasValue && !string.IsNullOrWhiteSpace(uploadedFileTempTableName))
        {
            return AppDatabaseTableImportBL.UpdateImportedTableDataFromTempTable(importSettingDataSetId.Value, uploadedFileTempTableName, fileName);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppDataSetExDto> CreateEntityImportSettingAndProcessImport(AppDataSetExDto aAppDataSetExDto)
    {
        return AppDatabaseTableImportBL.CreateEntityImportSettingAndProcessImport(aAppDataSetExDto);
    }

    [HttpPost]
    public OperationCallResult<bool> SetupEntityManyToManyRelationshipTableImport(DatabaseTableImportSettingDto importSettingDto)
    {
        return AppDatabaseTableImportBL.SetupEntityManyToManyRelationshipTableImport(importSettingDto);
    }

    [HttpGet]
    public OperationCallResult<int?> CreateDataUpdateApiFromImportDataSetId(int? importDataSetId)
    {
        if (importDataSetId.HasValue)
        {
            return AppDatabaseTableImportBL.CreateDataUpdateApiFromImportDataSetId(importDataSetId.Value);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppDataSetExDto> CreateDbToDbTableImportSetting(AppDataSetExDto aAppDataSetExDto)
    {
        return AppDatabaseTableImportBL.CreateDbToDbTableImportSetting(aAppDataSetExDto);
    }

    // this method always get from real time db scheme
    [HttpGet]
    public string GetDatabaseTableBuiltInQuery(string tableName, int? dataSourceRegisterId, string schemaOwner, EmAppBuiltInQueryType? emBuiltInQueryType)
    {
        return AppMetaDataBL.GetDatabaseTableBuiltInQuery(tableName, dataSourceRegisterId, schemaOwner, emBuiltInQueryType);
    }
}
