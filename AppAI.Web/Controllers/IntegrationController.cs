using System.Collections.Generic;
using System.Threading.Tasks;
using APP.Components.Dto;
using APP.Components.EntityDto;
using App.BL;
using APP.Framework.Communication;
using ExchangeBL;
using Microsoft.AspNetCore.Mvc;
using AppAI.Web.Controllers.Base;

namespace AppAI.Web.Controllers;

[Route("webapi/[controller]/[action]")]
public class IntegrationController : SecureBaseController
{
    [HttpGet]
    public List<AppIntergrationSettingExDto> RetrieveAllAppIntergrationSettingDto(bool? isIncludeAppBuiltInApi)
    {
        isIncludeAppBuiltInApi = isIncludeAppBuiltInApi.HasValue && isIncludeAppBuiltInApi.Value;

        return AppIntergrationSettingBL.RetrieveAllAppIntergrationSettingDto(isIncludeAppBuiltInApi.Value);
    }

    [HttpGet]
    public AppIntergrationSettingExDto RetrieveOneAppIntergrationSettingExDto(int? IntergrationSettingId)
    {
        if (IntergrationSettingId.HasValue)
        {
            return AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingExDto(IntergrationSettingId);
        }

        return null;
    }

    [HttpGet]
    public List<AppIntergrationSettingParameterExDto> RetrieveAllJsonFileTableImportSettingDtoList()
    {
        return AppIntergrationSettingBL.RetrieveAllJsonFileTableImportSettingDtoList();
    }

    [HttpGet]
    public List<AppIntergrationSettingParameterExDto> RetrieveAllApiStagingTableImportSettingDtoList()
    {
        return AppIntergrationSettingBL.RetrieveAllApiStagingTableImportSettingDtoList();
    }

    [HttpPost]
    public OperationCallResult<AppIntergrationSettingExDto> SaveAppIntergrationSettingExDto(AppIntergrationSettingExDto aAppIntergrationSettingExDto)
    {
        if (aAppIntergrationSettingExDto != null)
        {
            return AppIntergrationSettingBL.SaveAppIntergrationSettingExDto(aAppIntergrationSettingExDto);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<object> DeleteOneAppIntergrationSetting(int? IntergrationSettingId)
    {
        if (IntergrationSettingId.HasValue)
        {
            return AppIntergrationSettingBL.DeleteOneAppIntergrationSetting(IntergrationSettingId);
        }

        return null;
    }

    [HttpGet]
    public AppIntergrationSettingParameterExDto RetrieveOneAppIntergrationSettingParameterExDto(int? settingParameterId, bool isInlucdeApiDataStructure)
    {
        if (settingParameterId.HasValue)
        {
            return AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(settingParameterId, isInlucdeApiDataStructure);
        }

        return null;
    }

    [HttpGet]
    public AppIntergrationSettingParameterExDto GetAppSearchDefaultProviderApi(int? searchId, bool isInlucdeApiDataStructure, string appBaseUrl)
    {
        if (searchId.HasValue)
        {
            return AppIntergrationSettingBL.GetAppSearchDefaultProviderApi(searchId.Value, isInlucdeApiDataStructure, appBaseUrl);
        }

        return null;
    }

    [HttpGet]
    public List<AppIntergrationSettingParameterExDto> GetAppTransactionDefaultProviderApi(int? transactionId, bool isInlucdeApiDataStructure, string appBaseUrl)
    {
        if (transactionId.HasValue)
        {
            return AppIntergrationSettingBL.GetAppTransactionDefaultProviderApi(transactionId.Value, isInlucdeApiDataStructure, appBaseUrl);
        }

        return null;
    }

    [HttpGet]
    public List<ApiDataStructureNodeDto> RetrieveOneApiAvailableFetchDataNodeStructure(int? settingParameterId, string rootNodeFixedName)
    {
        if (settingParameterId.HasValue)
        {
            return AppIntergrationSettingBL.RetrieveOneApiAvailableFetchDataNodeStructure(settingParameterId, rootNodeFixedName);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppIntergrationSettingParameterExDto> SaveAppIntergrationSettingParameterExDto(AppIntergrationSettingParameterExDto aAppIntergrationSettingParameterExDto)
    {
        if (aAppIntergrationSettingParameterExDto != null)
        {
            return AppIntergrationSettingBL.SaveAppIntergrationSettingParameterExDto(aAppIntergrationSettingParameterExDto);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppIntergrationSettingParameterExDto> BuildJsonImportTableDiagramFromSetting(AppIntergrationSettingParameterExDto aAppIntergrationSettingParameterExDto)
    {
        if (aAppIntergrationSettingParameterExDto != null)
        {
            return AppIntergrationSettingBL.BuildJsonImportTableDiagramFromSetting(aAppIntergrationSettingParameterExDto);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<AppIntergrationSettingParameterExDto> CreateJsonFileDatabaseTableImportSettingByFileId(int? jsonFileId, int? dataSourceRegId, bool? isImportToExistingTable)
    {
        if (jsonFileId.HasValue)
        {
            if (!isImportToExistingTable.HasValue)
            {
                isImportToExistingTable = false;
            }

            return AppIntergrationSettingBL.CreateJsonFileDatabaseTableImportSettingByFileId(jsonFileId, dataSourceRegId, isImportToExistingTable.Value);
        }

        return null;
    }

    [HttpPost]
    // jsonString = importSettingDto.JsonSampleData, datasourceId = importSettingDto.DataSourceId
    public OperationCallResult<AppIntergrationSettingParameterExDto> CreateJsonDatabaseTableImportSettingFromJsonText(AppIntergrationSettingParameterExDto importSettingDto)
    {
        if (importSettingDto != null)
        {
            return AppIntergrationSettingBL.CreateJsonDatabaseTableImportSettingFromJsonText(importSettingDto);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<AppIntergrationSettingParameterExDto> UpdateStagingTableDataFromJsonUpload(int? importSettingId, int? jsonFileId)
    {
        if (jsonFileId.HasValue && importSettingId.HasValue)
        {
            return AppIntergrationSettingBL.UpdateStagingTableDataFromJsonUpload(importSettingId, jsonFileId);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<AppIntergrationSettingParameterExDto> UpdateJsonSchemaFromJsonUpload(int? importSettingId, int? jsonFileId)
    {
        if (jsonFileId.HasValue && importSettingId.HasValue)
        {
            return AppIntergrationSettingBL.UpdateJsonSchemaFromJsonUpload(importSettingId, jsonFileId);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<AppIntergrationSettingParameterExDto> CreateStatingTableImportSettingFromApiOperation(int? apiOperationId, bool? isImportToExistingTable)
    {
        if (apiOperationId.HasValue)
        {
            if (!isImportToExistingTable.HasValue)
            {
                isImportToExistingTable = false;
            }

            return AppIntergrationSettingBL.CreateStatingTableImportSettingFromApiOperation(apiOperationId, isImportToExistingTable.Value);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<object> DeleteOneAppIntergrationSettingParameter(int? settingParameterId)
    {
        if (settingParameterId.HasValue)
        {
            return AppIntergrationSettingBL.DeleteOneAppIntergrationSettingParameter(settingParameterId);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppIntergrationSettingParameterExDto> GenerateSampleJsonDataFromApiConfig(AppIntergrationSettingParameterExDto dto)
    {
        if (dto != null)
        {
            return DataExchangeSettingBL.GenerateSampleJsonDataFromApiConfig(dto);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppIntergrationSettingParameterExDto> GenerateDefaultSchemaAndDataSetMappingFromSampleJson(AppIntergrationSettingParameterExDto dto)
    {
        if (dto != null)
        {
            return DataExchangeSettingBL.GenerateDefaultSchemaAndDataSetMappingFromSampleJson(dto);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppIntergrationSettingParameterExDto> GenerateRuntimeSchemaFromDataSetMapping(AppIntergrationSettingParameterExDto dto)
    {
        if (dto != null)
        {
            return DataExchangeSettingBL.GenerateRuntimeSchemaFromDataSetMapping(dto);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppIntergrationSettingParameterExDto> CreateOrAlterDatabaseTablesFromRuntimeSchema(AppIntergrationSettingParameterExDto dto)
    {
        if (dto != null)
        {
            return DataExchangeSettingBL.CreateOrAlterDatabaseTablesFromRuntimeSchema(dto);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppIntergrationSettingParameterExDto> GenerateScriptsFromRuntimeSchema(AppIntergrationSettingParameterExDto dto)
    {
        if (dto != null)
        {
            return DataExchangeSettingBL.GenerateScriptsFromRuntimeSchema(dto);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppIntergrationSettingParameterExDto> GenerateTableAndScriptsFromSchemaDataSetMappingDto(AppIntergrationSettingParameterExDto dto)
    {
        if (dto != null)
        {
            return DataExchangeSettingBL.GenerateTableAndScriptsFromSchemaDataSetMappingDto(dto);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<bool> ExecuteOneOperationWithTestParameters(int? settingParameterId, bool isSimulate = false)
    {
        if (settingParameterId.HasValue)
        {
            return AppIntergrationSettingBL.ExecuteOneOperationWithTestParameters(settingParameterId, isSimulate);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<bool> ExecuteDataImportOnJsonFileTableImportSetting(int? importSettingId)
    {
        if (importSettingId.HasValue)
        {
            var result = AppIntergrationSettingBL.ExecuteDataImportOnJsonFileTableImportSetting(importSettingId);

            return result;
        }
        else
        {
            return null;
        }
    }

    [HttpGet]
    public List<AppTransactionDto> RetrieveApiWhereUsedOnTransactions(int? apiId)
    {
        if (apiId.HasValue)
        {
            return AppIntergrationSettingBL.RetrieveApiWhereUsedOnTransactions(apiId);
        }

        return null;
    }

    [HttpGet]
    public List<AppSearchDto> RetrieveApiWhereUsedOnSearches(int? apiId)
    {
        if (apiId.HasValue)
        {
            return AppIntergrationSettingBL.RetrieveApiWhereUsedOnSearches(apiId);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<AppIntergrationSettingParameterExDto> GenerateTransactionProvideSimpleQueryAPI(int? transactionId)
    {
        if (transactionId.HasValue)
        {
            return AppIntergrationSettingBL.GenerateTransactionProvideSimpleQueryAPI(transactionId);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<bool> DropAllStagingTablesByImportSettingId(int? settingParameterId)
    {
        if (settingParameterId.HasValue)
        {
            return AppIntergrationSettingBL.DropAllStagingTablesByImportSettingId(settingParameterId);
        }

        return null;
    }
}
