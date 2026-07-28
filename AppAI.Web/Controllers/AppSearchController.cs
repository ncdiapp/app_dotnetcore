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
public class AppSearchController : SecureBaseController
{
    [HttpGet]
    public async Task<SearchDto> RetrieveDefaultSearch(int? searchUsageType = null)
    {
        return await AppSearchBL.RetrieveDefaultSearchAsync(searchUsageType).ConfigureAwait(false);
    }

    [HttpGet]
    public ObservableSet<AppListMenuExDto> FullTextSearch(string keyword)
    {
        return new ObservableSet<AppListMenuExDto>();
    }

    [HttpGet]
    public async Task<SearchDto> RetrieveOneSearch(int searchId, bool? isSavedSearch)
    {
        return await RetrieveOneSearchMethodAsync(searchId, isSavedSearch).ConfigureAwait(false);
    }

    [HttpGet]
    public async Task<Dictionary<int, List<LookupItemDto>>> RetrieveViewDictEntityLookupItemDto(int viewId)
    {
        var dto = await AppSearchBL.RetrieveOneReferenceViewDtoAsync(viewId).ConfigureAwait(false);
        return dto.DictEntityLookupItemDto;
    }

    [HttpGet]
    public async Task<ReferenceViewDto> RetrieveOneReferenceViewDto(int viewId)
    {
        return await AppSearchBL.RetrieveOneReferenceViewDtoAsync(viewId).ConfigureAwait(false);
    }

    [HttpPost]
    public FileSimpleDto ProcessSearchResult(dynamic searchViewExternalUriDto)
    {
        if (searchViewExternalUriDto != null)
        {
            //var restuls = new { FristSearchResult = searchViewExternalUriDto.FirstSearchResult, SecondSearchResult = searchViewExternalUriDto.SecondSearchResult };

            //return _TechPackServiceFacadeServiceFacade.PublishReferenceFromSearchViewToExternalWebSerivceWithExtraSearchSetup(searchViewExternalUriDto, searchViewExternalUriDto.RestResourceUri.ToString());
            // this could be Internal code or RestService Url
            return AppPluginClient.ProcessSearchResult(searchViewExternalUriDto, searchViewExternalUriDto.RestResourceUri.ToString());
        }
        return null;
    }

    [HttpPost]
    public SearchResultDto RetrieveSearchResult(SearchDto searchDto)
    {
        return RetrieveSearchResultMethod(searchDto);
    }

    [HttpGet]
    public RetrieveSearchesDto RetrieveSearchesByUsageType(int? emSearchUsageType)
    {
        return AppSearchBL.RetrieveSearchesByUsageType(emSearchUsageType);
    }

    [HttpPost]
    public IEnumerable<ReferenceViewDefinitionDto> RetrieveUserViewsBySearchDefinition(SearchDefinitionDto searchDefinition)
    {
        if (searchDefinition != null)
        {
            return AppSearchViewConfigBL.RetrieveUserViewsBySearchDefinition(searchDefinition);
        }
        else
        {
            return null;
        }
    }

    [HttpPost]
    public async Task<OperationCallResult<SearchDefinitionDto>> SaveCriteriaPreset(SearchDto searchDto)
    {
        return await AppSearchConfigBL.SaveCriteriaPresetAsync(searchDto, false).ConfigureAwait(false);
    }

    [HttpPost]
    public async Task<OperationCallResult<SearchDefinitionDto>> SaveCriteriaPresetAs(SearchDto searchDto)
    {
        return await AppSearchConfigBL.SaveCriteriaPresetAsync(searchDto, true).ConfigureAwait(false);
    }

    [HttpPost]
    public async Task<OperationCallResult<SearchDefinitionDto>> DeleteCriteriaPreset(SearchDto searchDto)
    {
        return await AppSearchConfigBL.DeleteCriteriaPresetAsync(searchDto).ConfigureAwait(false);
    }

    [HttpPost]
    public async Task<bool> SetAsDefaultCriteriaPreset(SearchDto searchDto)
    {
        return await AppSearchConfigBL.SetAsDefaultCriteriaPresetAsync(searchDto).ConfigureAwait(false);
    }

    [HttpPost]
    public async Task<bool> ChangeSearchAutoExecute(SearchDto searchDto)
    {
        return await AppSearchConfigBL.ChangeSearchAutoExecuteAsync(searchDto).ConfigureAwait(false);
    }

    [HttpPost]
    public bool AddToFavorite(SearchDto searchDto)
    {
        return false;
    }

    [HttpPost]
    public OperationCallResult<StaticSearchResultRowJsonDto> SaveMassUpdateResult(MassUpdateSaveDto massUpdateSaveDto)
    {
        if (massUpdateSaveDto.IsListEditSimpleMassUpdate)
        {
            if (massUpdateSaveDto.MassUpdateAppListDataDto != null && !massUpdateSaveDto.MassUpdateAppListDataDto.ListData.IsEmpty())
            {
                DataModelDateTimeConverterBL.ConvertListEditPostedUtcToClientForCalculation(massUpdateSaveDto.MassUpdateAppListDataDto);

                OperationCallResult<AppListDataDto> validationResult = AppTransactionFormulaBL.ValidateListEditTransactionData(massUpdateSaveDto.MassUpdateAppListDataDto);

                if (!validationResult.IsSuccessfulWithResult)
                {
                    return null;
                }
            }
        }

        // To Do, Need to verify if need time convert
        return AppTransactionDataMassUpdateBL.SaveMassUpdateResult(massUpdateSaveDto);
    }

    [HttpPost]
    public SearchCascdingDto CascadingSearchCriteriaValueChanged(SearchCascdingDto searchDto)
    {
        AppCascadingSearchBL.SetupOneSearchFiledCscadingSearchCretiaDataSource(searchDto);

        //SearchDto toReturn = searchDto;

        return searchDto;
    }

    public static async Task<SearchDto> RetrieveOneSearchMethodAsync(int searchId, bool? isSavedSearch)
    {
        var aSearchDto = await AppSearchBL.RetrieveOneSearchDtoAsync(searchId, isSavedSearch).ConfigureAwait(false);

        if (aSearchDto.DefaultView != null && !aSearchDto.DefaultView.IsMassUpdate)
        {
            aSearchDto.DefaultView.DictEntityLookupItemDto = new Dictionary<int, List<LookupItemDto>>();
        }

        return aSearchDto;
    }

    public static SearchDto RetrieveOneSearchMethod(int searchId, bool? isSavedSearch)
    {
        var aSearchDto = AppSearchBL.RetrieveOneSearchDto(searchId, isSavedSearch);

        if (aSearchDto.DefaultView != null && !aSearchDto.DefaultView.IsMassUpdate)
        {
            aSearchDto.DefaultView.DictEntityLookupItemDto = new Dictionary<int, List<LookupItemDto>>();
        }

        return aSearchDto;
    }

    public static SearchResultDto RetrieveSearchResultMethod(SearchDto searchDto)
    {
        // if criteria contorl type == datetime, auto searialized to UTC
        // if criteria contorl type == date, need to convert to client time (trunkate time: 00:00:00)

        AppSearchBL.ConvertSearchCriteriaDateFromUTCToClient(searchDto);

        //TODO
        SearchResultDto searchResult = AppSearchBL.RetrieveSearchResult(searchDto);


        if (searchResult != null && searchResult.MassUpdateAppListDataDto != null)
        {
            DataModelDateTimeConverterBL.ConvertListEditFromUtcToClient(searchResult.MassUpdateAppListDataDto);
        }


        return searchResult;
    }

    [HttpGet]
    public async Task<List<SearchApiSettingDto>> RetrieveSearchApiSettings(int? searchId)
    {
        return await AppSearchConfigBL.RetrieveSearchApiSettingsAsync(searchId).ConfigureAwait(false);
    }
}
