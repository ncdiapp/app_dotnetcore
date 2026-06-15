using System.Collections.Generic;
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
    public SearchDto RetrieveDefaultSearch(int? searchUsageType = null)
    {
        var aSearchDto = AppSearchBL.RetrieveDefaultSearch(searchUsageType);

        return aSearchDto;
    }

    [HttpGet]
    public ObservableSet<AppListMenuExDto> FullTextSearch(string keyword)
    {
        return new ObservableSet<AppListMenuExDto>();
    }

    [HttpGet]
    public SearchDto RetrieveOneSearch(int searchId, bool? isSavedSearch)
    {
        return RetrieveOneSearchMethod(searchId, isSavedSearch);
    }

    [HttpGet]
    public Dictionary<int, List<LookupItemDto>> RetrieveViewDictEntityLookupItemDto(int viewId)
    {
        return AppSearchBL.RetrieveOneReferenceViewDto(viewId).DictEntityLookupItemDto;
    }

    [HttpGet]
    public ReferenceViewDto RetrieveOneReferenceViewDto(int viewId)
    {
        var toReturn = AppSearchBL.RetrieveOneReferenceViewDto(viewId);

        return toReturn;
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
    public OperationCallResult<SearchDefinitionDto> SaveCriteriaPreset(SearchDto searchDto)
    {
        return AppSearchConfigBL.SaveCriteriaPreset(searchDto, false);
    }

    [HttpPost]
    public OperationCallResult<SearchDefinitionDto> SaveCriteriaPresetAs(SearchDto searchDto)
    {
        return AppSearchConfigBL.SaveCriteriaPreset(searchDto, true);
    }

    [HttpPost]
    public OperationCallResult<SearchDefinitionDto> DeleteCriteriaPreset(SearchDto searchDto)
    {
        return AppSearchConfigBL.DeleteCriteriaPreset(searchDto);
    }

    [HttpPost]
    public bool SetAsDefaultCriteriaPreset(SearchDto searchDto)
    {
        return AppSearchConfigBL.SetAsDefaultCriteriaPreset(searchDto);
    }

    [HttpPost]
    public bool ChangeSearchAutoExecute(SearchDto searchDto)
    {
        return AppSearchConfigBL.ChangeSearchAutoExecute(searchDto);
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
    public List<SearchApiSettingDto> RetrieveSearchApiSettings(int? searchId)
    {
        return AppSearchConfigBL.RetrieveSearchApiSettings(searchId);
    }
}
