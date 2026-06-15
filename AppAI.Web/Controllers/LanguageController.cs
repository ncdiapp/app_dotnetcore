using System.Collections.Generic;
using APP.Components.Dto;
using APP.Components.EntityDto;
using App.BL;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using Microsoft.AspNetCore.Mvc;
using AppAI.Web.Controllers.Base;

namespace AppAI.Web.Controllers;

[Route("webapi/[controller]/[action]")]
public class LanguageController : SecureBaseController
{
    [HttpGet]
    public ObservableSet<AppLanguageDto> RetrieveAllAppLanguageDto()
    {
        return AppLanguageBL.RetrieveAllAppLanguageDto();
    }

    [HttpGet]
    public AppLanguageExDto RetrieveOneAppLanguageExDto(int languageId)
    {
        AppLanguageExDto aAppLanguageExDto = AppLanguageBL.RetrieveOneAppLanguageExDto(languageId);
        return aAppLanguageExDto;
    }

    [HttpGet]//  GetClientScriptLangKeyValue()
    public Dictionary<string, string> GetClientScriptLangKeyValue()
    {
        return StringLocalizer.GetClientScriptLangKeyValue();
    }

    [HttpPost]
    public bool SaveAppLanguageDtoList(AppLanguageExDto jsonDtoData)
    {
        jsonDtoData.AppLanguageKeyList.DeletedItemIds = new System.Collections.Generic.List<object>(jsonDtoData.DeletedItemsIds);
        AppLanguageBL.SaveAppLanguageExDto(jsonDtoData);

        return true;
    }

    [HttpGet]
    public List<LanguageKeyByTypeDto> RetrieveOneLanguageAllLanguageKeys(int languageId, int? languageKeyType)
    {
        return AppLanguageBL.RetrieveOneLanguageAllLanguageKeys(languageId, languageKeyType);
    }

    [HttpPost]
    public OperationCallResult<LanguageKeyByTypeDto> SaveOneLanguageAllLanguageKeys(LanguageKeySetDto languageKeySetDto)
    {
        if (languageKeySetDto != null)
        {
            List<LanguageKeyByTypeDto> languageKeyList = languageKeySetDto.LanguageKeyByTypeDtoList;
            int langagueId = languageKeySetDto.LangaugeId;
            int? languageKeyType = languageKeySetDto.LanguageKeyType;

            if (languageKeyList != null)
            {
                return AppLanguageBL.SaveOneLanguageAllLanguageKeys(languageKeyList, langagueId, languageKeyType);
            }
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<bool> GenerateAppSysLabelDefaultLanguageKeys()
    {
        return AppLanguageBL.GenerateAppSysLabelDefaultLanguageKeys();
    }

    [HttpGet]
    public bool RefreshAllLanguageKeyDictionaries()
    {
        AppLocalizeSystemLableBL.RefreshAppSystemLableLanguageKeyDictionaries();
        return true;
    }

    [HttpPost]
    public OperationCallResult<bool> ImportLanguageKeyFromExcel(List<string> filePathList)
    {
        if (filePathList != null)
        {
            return AppLanguageBL.ImportLanguageKeyFromExcel(filePathList);
        }

        return null;
    }
}
