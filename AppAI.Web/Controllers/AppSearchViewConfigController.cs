using System.Collections.Generic;
using System.Threading.Tasks;
using APP.Components.Dto;
using APP.Components.EntityDto;
using App.BL;
using APP.Framework.Collections;
using APP.Framework.Communication;
using Microsoft.AspNetCore.Mvc;
using AppAI.Web.Controllers.Base;

namespace AppAI.Web.Controllers;

[Route("webapi/[controller]/[action]")]
public class AppSearchViewConfigController : SecureBaseController
{
    // Search and View Configuration

    [HttpGet]
    public ObservableSet<AppDataSetExDto> RetrieveAllAppDataSetEntityDto()
    {
        return AppDataSetBL.RetrieveAllAppDataSetEntityDto();
    }

    [HttpGet]
    public List<LookupItemDto> RetrieveQueryColumnList(int? dataSetId)
    {
        if (dataSetId.HasValue)
        {
            return AppDataSetBL.RetrieveQueryColumnList(dataSetId.Value);
        }
        else
        {
            return null;
        }
    }

    [HttpGet]
    public string GenerateQueryFromDataModel(int? dataSetId)
    {
        if (dataSetId.HasValue)
        {
            return AppDataSetBL.GenerateQueryFromDataModel(dataSetId.Value);
        }

        return string.Empty;
    }

    [HttpPost]
    public List<LookupItemDto> RetrieveDataSetQueryColumnList(AppDataSetExDto aAppDataSetExDto)
    {
        if (aAppDataSetExDto != null)
        {
            return AppDataSetBL.RetrieveDataSetQueryColumnList(aAppDataSetExDto);
        }

        return null;
    }

    [HttpGet]
    public AppDataSetExDto RetrieveOneAppDataSetExDto(int? dataSetId, bool? isGetDbDiagram)
    {
        if (!isGetDbDiagram.HasValue)
        {
            isGetDbDiagram = false;
        }
        return AppDataSetBL.RetrieveOneAppDataSetExDto(dataSetId, isGetDbDiagram.Value);
    }

    [HttpPost]
    public OperationCallResult<AppDataSetExDto> SaveOneAppDataSetEntityDto(AppDataSetExDto aAppDataSetExDto)
    {
        if (aAppDataSetExDto != null && aAppDataSetExDto.DeletedItemsIds != null)
        {
            aAppDataSetExDto.AppDataSetParameterList.DeletedItemIds = aAppDataSetExDto.DeletedItemsIds;
        }

        return AppDataSetBL.SaveOneAppDataSetEntityDto(aAppDataSetExDto);
    }

    [HttpGet]
    public OperationCallResult<object> DeleteOneAppDataSetEntityDto(int? dataSetId)
    {
        return AppDataSetBL.DeleteOneAppDataSetEntityDto(dataSetId);
    }

    [HttpGet]
    public List<AppSearchViewDto> RetrieveStatciSearchAvailableViewWithSameQueryBL(int? dataSetId)
    {
        if (dataSetId.HasValue)
        {
            return AppSearchConfigBL.RetrieveStatciSearchAvailableViewWithSameQueryBL(dataSetId.Value);
        }
        else
        {
            return null;
        }
    }

    [HttpGet]
    public async Task<AppSearchExDto> RetrieveOneAppSearchExDto(int? searchId)
    {
        return await AppSearchConfigBL.RetrieveOneAppSearchExDtoAsync(searchId).ConfigureAwait(false);
    }

    [HttpGet]
    public List<AppSearchDto> RetrieveAllAppSearchDto(int? searchUsageType)
    {
        return AppSearchConfigBL.RetrieveAllAppSearchDto(searchUsageType);
    }

    [HttpPost]
    public async Task<OperationCallResult<AppSearchExDto>> SaveAppSearchExDto(AppSearchExDto aAppSearchExDto)
    {
        aAppSearchExDto.AppSearchFieldList.DeletedItemIds = aAppSearchExDto.DeletedItemsIds;

        if (aAppSearchExDto.DictDeletedItemsIds != null)
        {
            if (aAppSearchExDto.DictDeletedItemsIds.ContainsKey("AppSearchParameterList"))
            {
                aAppSearchExDto.AppSearchParameterList.DeletedItemIds = aAppSearchExDto.DictDeletedItemsIds["AppSearchParameterList"];
            }
        }

        return await AppSearchConfigBL.SaveAppSearchExDtoAsync(aAppSearchExDto).ConfigureAwait(false);
    }

    [HttpPost]
    public OperationCallResult<AppSearchExDto> SaveEshopCategorySearchExDto(AppSearchExDto aAppSearchExDto)
    {
        if (aAppSearchExDto.DeletedItemsIds == null)
        {
            aAppSearchExDto.DeletedItemsIds = new List<object>();
        }

        aAppSearchExDto.AppSearchFieldList.DeletedItemIds = aAppSearchExDto.DeletedItemsIds;

        if (aAppSearchExDto.DictDeletedItemsIds != null)
        {
            if (aAppSearchExDto.DictDeletedItemsIds.ContainsKey("AppSearchParameterList"))
            {
                aAppSearchExDto.AppSearchParameterList.DeletedItemIds = aAppSearchExDto.DictDeletedItemsIds["AppSearchParameterList"];
            }
        }

        if (aAppSearchExDto.DefaultSearchViewExDto != null)
        {
            var searchViewExDto = aAppSearchExDto.DefaultSearchViewExDto;

            if (searchViewExDto.DeletedItemsIds != null)
            {
                searchViewExDto.AppSearchViewFieldList.DeletedItemIds = searchViewExDto.DeletedItemsIds;
            }
        }

        if (aAppSearchExDto.EshopCardSearchExDto != null && aAppSearchExDto.EshopCardSearchExDto.DefaultSearchViewExDto != null)
        {
            var searchViewExDto = aAppSearchExDto.EshopCardSearchExDto.DefaultSearchViewExDto;

            if (searchViewExDto.DeletedItemsIds != null)
            {
                searchViewExDto.AppSearchViewFieldList.DeletedItemIds = searchViewExDto.DeletedItemsIds;
            }
        }

        return AppSearchConfigBL.SaveEshopCategorySearchExDto(aAppSearchExDto);
    }

    [HttpGet]
    public OperationCallResult<AppSearchExDto> SaveAsSearch(int? searchId)
    {
        if (searchId.HasValue)
        {
            return AppSearchConfigBL.SaveAsSearch(searchId);
        }

        return null;
    }

    [HttpGet]
    public async Task<OperationCallResult<object>> DeleteAppSearch(int? searchId)
    {
        return await AppSearchConfigBL.DeleteAppSearchAsync(searchId).ConfigureAwait(false);
    }

    [HttpGet]
    public async Task<AppSearchViewExDto> RetrieveOneAppSearchViewExDto(int? searchViewId)
    {
        return await AppSearchViewConfigBL.RetrieveOneAppSearchViewExDtoAsync(searchViewId).ConfigureAwait(false);
    }

    [HttpGet]
    public async Task<ObservableSet<AppSearchViewDto>> RetrieveAllAppSearchViewDto()
    {
        return await AppSearchViewConfigBL.RetrieveAllAppSearchViewDtoAsync().ConfigureAwait(false);
    }

    [HttpGet]
    public async Task<ObservableSet<AppSearchViewDto>> RetrieveAllSearchViewDtoByViewType(int? viewType)
    {
        return await AppSearchViewConfigBL.RetrieveAllSearchViewDtoByViewTypeAsync(viewType).ConfigureAwait(false);
    }

    [HttpPost]
    public async Task<OperationCallResult<AppSearchViewExDto>> SaveAppSearchViewExDto(AppSearchViewExDto aAppSearchViewExDto)
    {
        aAppSearchViewExDto.AppSearchViewFieldList.DeletedItemIds = aAppSearchViewExDto.DeletedItemsIds;
        return await AppSearchViewConfigBL.SaveAppSearchViewExDtoAsync(aAppSearchViewExDto).ConfigureAwait(false);
    }

    [HttpGet]
    public OperationCallResult<AppSearchViewExDto> SaveAsSearchView(int? searchViewId)
    {
        if (searchViewId.HasValue)
        {
            return AppSearchViewConfigBL.SaveAsSearchView(searchViewId);
        }

        return null;
    }

    [HttpGet]
    public async Task<OperationCallResult<object>> DeleteAppSearchView(int? searchViewId)
    {
        return await AppSearchViewConfigBL.DeleteAppSearchViewAsync(searchViewId).ConfigureAwait(false);
    }

    [HttpGet]
    public List<AppFormLinkTargetDto> RetrieveOneSearchViewLinkTargetList(string searchViewId, int? usageType)
    {
        return LinkTragetBL.RetrieveOneSearchViewLinkTargetList(searchViewId, usageType);
    }

    [HttpPost]
    public OperationCallResult<AppFormLinkTargetDto> SaveOneSearchViewLinkTargetList(AppFormLinkTargetSetDto appFormLinkTargetSetDto)
    {
        if (appFormLinkTargetSetDto != null && appFormLinkTargetSetDto.SearchViewId.HasValue)
        {
            appFormLinkTargetSetDto.AppFormLinkTargetDtoSet.DeletedItemIds = appFormLinkTargetSetDto.DeletedItemIds;

            OperationCallResult<AppFormLinkTargetDto> aOperationCallResult = LinkTragetBL.SaveOneAppFormLinkTargetList(
                (int)EmAppLinkTargetSourceType.SearchView, appFormLinkTargetSetDto.SearchViewId.Value, appFormLinkTargetSetDto.AppFormLinkTargetDtoSet);

            return aOperationCallResult;
        }

        return null;
    }

    [HttpGet]
    public List<AppViewLinkedSeaechOrUrlExDto> RetrieveOneAppViewLinkedSeaechOrUrlExDto(int? searchViewId)
    {
        if (searchViewId.HasValue)
        {
            return AppViewLinkedSeaechOrUrlBL.RetrieveOneAppViewLinkedSeaechOrUrlExDto(searchViewId.Value);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppViewLinkedSeaechOrUrlExDto> SaveAllAppViewLinkedSeaechOrUrlEntityDto(AppFormLinkTargetSetDto appFormLinkTargetSetDto)
    {
        if (appFormLinkTargetSetDto != null && appFormLinkTargetSetDto.SearchViewId.HasValue)
        {
            appFormLinkTargetSetDto.AppViewLinkedSeaechOrUrlDtoSet.DeletedItemIds = appFormLinkTargetSetDto.DeletedItemIds;

            OperationCallResult<AppViewLinkedSeaechOrUrlExDto> aOperationCallResult = AppViewLinkedSeaechOrUrlBL.SaveAllAppViewLinkedSeaechOrUrlEntityDto(
                appFormLinkTargetSetDto.AppViewLinkedSeaechOrUrlDtoSet, appFormLinkTargetSetDto.SearchViewId.Value);

            return aOperationCallResult;
        }

        return null;
    }

    [HttpGet]
    public async Task<ObservableSet<AppViewFiledSearchFiledMappingExDto>> RetrieveAppViewFiledSearchFiledMappingBySearchViewId(int? searchViewId)
    {
        return await AppSearchViewConfigBL.RetrieveAppViewFiledSearchFiledMappingBySearchViewIdAsync(searchViewId).ConfigureAwait(false);
    }

    [HttpPost]
    public async Task<OperationCallResult<AppViewFiledSearchFiledMappingExDto>> SaveAllAppViewFiledSearchFiledMappingExDto(AppViewFiledSearchFiledMappingSetDto setDto)
    {
        ObservableSet<AppViewFiledSearchFiledMappingExDto> appViewFiledSearchFiledMappingSet = setDto.AppViewFiledSearchFiledMappingSet;
        appViewFiledSearchFiledMappingSet.DeletedItemIds = new List<object>();
        int? searchViewId = setDto.SearchViewId;
        if (appViewFiledSearchFiledMappingSet != null && searchViewId.HasValue)
        {
            return await AppSearchViewConfigBL.SaveAllAppViewFiledSearchFiledMappingExDtoAsync(appViewFiledSearchFiledMappingSet, searchViewId.Value).ConfigureAwait(false);
        }
        else
        {
            return null;
        }
    }

    [HttpGet]
    public List<AppDataSetDto> RetrieveExtractDataSetList()
    {
        return AppDataSetExtractViewConfigBL.RetrieveExtractDataSetList();
    }

    [HttpGet]
    public AppDataSetExDto RetrieveOneExtractAppDataSetExDto(int? extractDataSetId)
    {
        if (extractDataSetId.HasValue)
        {
            return AppDataSetExtractViewConfigBL.RetrieveOneAppDataSetExDto(extractDataSetId.Value);
        }
        return null;
    }

    [HttpPost]
    public OperationCallResult<AppDataSetExDto> SaveOneExtractAppDataSetExDto(AppDataSetExDto aAppDataSetExDto)
    {
        if (aAppDataSetExDto != null)
        {
            aAppDataSetExDto.AppDateSetDataExtractViewList.DeletedItemIds = aAppDataSetExDto.DeletedItemsIds;
            return AppDataSetExtractViewConfigBL.SaveAppDataSetExDto(aAppDataSetExDto);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<object> DeleteOneExtractAppDataSetExDto(int? extractDataSetId)
    {
        if (extractDataSetId.HasValue)
        {
            return AppDataSetExtractViewConfigBL.DeleteAppDataSet(extractDataSetId.Value);
        }
        return null;
    }

    [HttpGet]
    public List<AppSearchFieldDto> RetrieveAllAppSearchFieldDtoList()
    {
        return AppSearchConfigBL.RetrieveAllAppSearchFieldDtoList();
    }

    [HttpGet]
    public async Task<OperationCallResult<bool>> SetSearchForPublicAccess(int? searchId)
    {
        if (searchId.HasValue)
        {
            return await AppSearchConfigBL.SetSearchForPublicAccessAsync(searchId.Value).ConfigureAwait(false);
        }

        return null;
    }

    [HttpGet]
    public async Task<AppSearchFieldExDto> RetrieveOneAppSearchFieldExDto(int? searchFieldId)
    {
        return await AppSearchConfigBL.RetrieveOneAppSearchFieldExDtoAsync(searchFieldId).ConfigureAwait(false);
    }

    [HttpPost]
    public async Task<OperationCallResult<AppSearchFieldExDto>> SaveAppSearchFieldExDto(AppSearchFieldExDto aAppSearchFieldExDto)
    {
        return await AppSearchConfigBL.SaveAppSearchFieldExDtoAsync(aAppSearchFieldExDto).ConfigureAwait(false);
    }

    [HttpGet]
    public async Task<AppSearchViewFieldExDto> RetrieveOneAppSearchViewFieldExDto(int? searchViewFieldId)
    {
        return await AppSearchViewConfigBL.RetrieveOneAppSearchViewFieldExDtoAsync(searchViewFieldId).ConfigureAwait(false);
    }

    [HttpPost]
    public async Task<OperationCallResult<AppSearchViewFieldExDto>> SaveAppSearchViewFieldExDto(AppSearchViewFieldExDto aAppSearchViewFieldExDto)
    {
        return await AppSearchViewConfigBL.SaveAppSearchViewFieldExDtoAsync(aAppSearchViewFieldExDto).ConfigureAwait(false);
    }

    [HttpGet]
    public OperationCallResult<AppEntityInfoExDto> GenerateQueryEntityFromDataSetField(int? datasetId, string datasetFieldName)
    {
        return AppEntityInfoBL.GenerateQueryEntityFromDataSetField(datasetId, datasetFieldName);
    }
}
