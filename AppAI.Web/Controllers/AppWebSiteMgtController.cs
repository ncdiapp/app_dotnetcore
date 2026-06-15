using System.Collections.Generic;
using System.Threading.Tasks;
using APP.Components.Dto;
using APP.Components.EntityDto;
using App.BL;
using APP.BL;
using APP.Framework;
using APP.Framework.Communication;
using APP.Framework.Validation;
using Microsoft.AspNetCore.Mvc;
using AppAI.Web.Controllers.Base;

namespace AppAI.Web.Controllers;

[Route("webapi/[controller]/[action]")]
public class AppWebSiteMgtController : SecureBaseController
{
    [HttpGet]
    public AppEsiteCatalogueExDto RetrieveOneAppEsiteCatalogueExDto(int? eStoreId)
    {
        if (eStoreId.HasValue)
        {
            return AppEsiteConfigBL.RetrieveOneAppEsiteCatalogueExDto(eStoreId.Value);
        }
        return null;
    }

    [HttpPost]
    public OperationCallResult<AppEsiteCatalogueExDto> SaveAppEsiteCatalogueExDto(AppEsiteCatalogueExDto aAppEsiteCatalogueExDto)
    {
        if (aAppEsiteCatalogueExDto != null)
        {
            return AppEsiteConfigBL.SaveAppEsiteCatalogueExDto(aAppEsiteCatalogueExDto);
        }
        else
        {
            return null;
        }
    }

    [HttpGet]
    public OperationCallResult<object> DeleteOneAppEsiteCatalogue(int? eStoreId)
    {
        if (eStoreId.HasValue)
        {
            return AppEsiteConfigBL.DeleteOneAppEsiteCatalogue(eStoreId.Value);
        }
        return null;
    }

    [HttpGet]
    public List<AppEsiteDto> RetrieveApplicationWebsiteDtoList()
    {
        return AppEsiteConfigBL.RetrieveApplicationWebsiteDtoList();
    }

    [HttpGet]
    public List<AppEsiteDto> RetrieveNextJsApplicationList()
    {
        return AppEsiteConfigBL.RetrieveNextJsApplicationList();
    }

    [HttpGet]
    public List<AppEsiteDto> RetrieveWebsiteTemplateDtoList()
    {
        return AppEsiteConfigBL.RetrieveWebsiteTemplateDtoList();
    }

    [HttpGet]
    public AppSefolderDto ImportWebSiteTemplateToApplicationSite(int? tempalteSiteId, int? appSiteId)
    {
        if (tempalteSiteId.HasValue && appSiteId.HasValue)
        {
            string OriginalString = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}{Request.QueryString}";

            int serverPathEndIndexPosition = OriginalString.IndexOf("webapi");
            string requestHostServerPath = OriginalString.Substring(0, serverPathEndIndexPosition);

            return AppEsiteFileBL.ImportWebSiteTemplateToApplicationSite(tempalteSiteId, appSiteId, requestHostServerPath);
        }
        return null;
    }

    [HttpGet]
    public bool SynchronizeWebSiteRoutstatejs(int? appSiteId)
    {
        if (appSiteId.HasValue)
        {
            return AppEsiteFileBL.SynchronizeWebSiteRoutstatejs(appSiteId);
        }
        return false;
    }

    [HttpGet]
    public AppEsiteExDto RetrieveOneEsiteExDto(int? esiteId)
    {
        if (esiteId.HasValue)
        {
            return AppEsiteConfigBL.RetrieveOneAppEsiteExDto(esiteId.Value);
        }
        return null;
    }

    [HttpPost]
    public OperationCallResult<AppEsiteExDto> SaveOneAppEsiteExDto(AppEsiteExDto appEsiteExDto)
    {
        if (appEsiteExDto != null)
        {
            string OriginalString = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}{Request.QueryString}";

            int serverPathEndIndexPosition = OriginalString.IndexOf("webapi");
            string requestHostServerPath = OriginalString.Substring(0, serverPathEndIndexPosition);

            if (appEsiteExDto.EsiteAttribute == null)
            {
                appEsiteExDto.EsiteAttribute = new EsiteAttributeDto();
            }

            appEsiteExDto.EsiteAttribute.MgtSiteBaseUrl = requestHostServerPath;

            return AppEsiteConfigBL.SaveAppEsiteExDto(appEsiteExDto);
        }
        else
        {
            return null;
        }
    }

    [HttpGet]
    public OperationCallResult<object> DeleteOneAppEsite(int? esiteId)
    {
        if (esiteId.HasValue)
        {
            return AppEsiteConfigBL.DeleteOneAppEsite(esiteId.Value);
        }
        return null;
    }

    [HttpGet]
    public OperationCallResult<AppEsiteExDto> SaveAsAppWebsite(int? esiteId)
    {
        if (esiteId.HasValue)
        {
            return AppEsiteConfigBL.SaveAsAppWebsite(esiteId.Value);
        }
        return null;
    }

    [HttpPost]
    public AppEsitePagesExDto RetrieveOneAppEsitePagesExDto(AppEsitePagesDto pageDto)
    {
        if (pageDto != null)
        {
            if (!string.IsNullOrWhiteSpace(pageDto.FileFullPath) && pageDto.EsiteId.HasValue)
            {
                return RetrieveOneWebSiteFile(pageDto.FileFullPath, pageDto.EsiteId.Value);
            }
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppEsitePagesExDto> SaveAppEsitePagesExDto(AppEsitePagesExDto aAppEsitePagesExDto)
    {
        if (aAppEsitePagesExDto != null)
        {
            return AppEsiteFileBL.SaveOneWebSiteFileWithHtmlPageAtributeFile(aAppEsitePagesExDto);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<bool> SaveAppEsitePageExDtoList(List<AppEsitePagesExDto> pageList)
    {
        if (pageList != null)
        {
            return AppEsiteFileBL.SaveAppEsitePageExDtoList(pageList);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<object> DeleteOneAppEsitePages(AppEsitePagesDto appEsitePagesDto)
    {
        if (appEsitePagesDto != null)
        {
            if (!string.IsNullOrWhiteSpace(appEsitePagesDto.FileFullPath))
            {
                return AppEsiteFileBL.DeleteOneWebSiteFile(appEsitePagesDto);
            }
            else if (appEsitePagesDto.Id != null)
            {
                return AppEsiteConfigBL.DeleteOneAppEsitePages(appEsitePagesDto.Id);
            }
        }
        return null;
    }

    [HttpPost]
    public OperationCallResult<bool> MoveOneEsiteFileToNewLocation(AppEsitePagesDto appEsitePagesDto)
    {
        if (appEsitePagesDto != null)
        {
            return AppEsiteFileBL.MoveOneEsiteFileToNewLocation(appEsitePagesDto);
        }
        return null;
    }

    [HttpGet]
    public AppSefolderDto RetrieveLocalFolderHairarchyDtoByFolderPath(string rootFolderPath)
    {
        if (!string.IsNullOrWhiteSpace(rootFolderPath))
        {
            return AppEsiteFileBL.RetrieveLocalFolderHairarchyDtoByFolderPath(rootFolderPath);
        }
        return null;
    }

    [HttpGet]
    public AppSefolderDto RetrieveAppEsiteLocalFolderHairarchyDto(int? eSiteId, string subFolderPath, int? subsiteType)
    {
        return AppEsiteFileBL.RetrieveAppEsiteLocalFolderHairarchyDto(eSiteId, subFolderPath, subsiteType);
    }

    [HttpPost]
    public List<AppEsitePagesDto> RetrieveLocalFileInfoDtosByFolderDto(AppSefolderDto folderDto)
    {
        if (folderDto != null && !string.IsNullOrWhiteSpace(folderDto.FolderPath))
        {
            string folderPath = folderDto.FolderPath;

            // using parentId as SiteId
            int? siteId = folderDto.EsiteId;

            return AppEsiteFileBL.RetrieveLocalFileInfoDtosByFolderPath(folderPath, siteId);
        }

        return null;
    }

    [HttpGet]
    public List<AppEsitePagesDto> RetrieveAppEsiteComponentList(int? siteId)
    {
        return AppEsiteFileBL.RetrieveAppEsiteComponentList(siteId);
    }

    [HttpGet]
    public List<AppSefolderDto> RetrieveAppEsiteComponetFolders(int? siteId)
    {
        return AppEsiteFileBL.RetrieveAppEsiteComponetFolders(siteId);
    }

    [HttpGet]
    public List<AppEsitePagesDto> RetrieveAppEsiteThirdPartControlThemeList(int? siteId, EmAppEsiteThirdPartControl? thirdPartControlType)
    {
        return AppEsiteFileBL.RetrieveAppEsiteThirdPartControlThemeList(siteId, thirdPartControlType);
    }

    [HttpPost]
    public OperationCallResult<AppEsitePagesDto> CreateNewAppEsiteThirdPartControlTheme(AppEsitePagesDto themePageDto)
    {
        if (themePageDto != null)
        {
            return AppEsiteFileBL.CreateNewAppEsiteThirdPartControlTheme(themePageDto);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<bool> SaveOneAppEsiteThirdPartControlTheme(AppEsitePagesDto themePageDto)
    {
        if (themePageDto != null)
        {
            return AppEsiteFileBL.SaveOneAppEsiteThirdPartControlTheme(themePageDto);
        }

        return null;
    }

    [HttpGet]
    public List<AppEsitePagesDto> RetrieveAppEsiteFigmaTemplateList(int? siteId)
    {
        return AppFigmaApiBL.RetrieveAppEsiteFigmaTemplateList(siteId);
    }

    [HttpPost]
    public List<AppEsitePagesDto> RetrieveOneAppEsiteFigmaTemplatePageList(AppEsitePagesDto inputDto)
    {
        return AppFigmaApiBL.RetrieveOneAppEsiteFigmaTemplatePageList(inputDto);
    }

    [HttpPost]
    public AppEsitePagesDto RetrieveOneAppEsiteFigmaTemplatePageContent(AppEsitePagesDto inputDto)
    {
        return AppFigmaApiBL.RetrieveOneAppEsiteFigmaTemplatePageContent(inputDto);
    }

    [HttpPost]
    public OperationCallResult<AppEsitePagesDto> ImportAppEsiteFigmaTemplate(AppEsitePagesDto figmaTemplateImportDto)
    {
        return AppFigmaApiBL.ImportAppEsiteFigmaTemplate(figmaTemplateImportDto.EsiteId, figmaTemplateImportDto.FigmaFileUrlOrId, figmaTemplateImportDto.FigmaPersonalAccessToken);
    }

    [HttpPost]
    public OperationCallResult<bool> GenerateImageAndFontFromImportedFigmaTemplate(AppEsitePagesDto figmaTemplateDto)
    {
        return AppFigmaApiBL.GenerateImageAndFontFromImportedFigmaTemplate(figmaTemplateDto);
    }

    [HttpGet]
    public string CreateOneFileFolder(string fileFullPath)
    {
        if (!string.IsNullOrWhiteSpace(fileFullPath))
        {
            return AppEsiteFileBL.CreateOneFileFolder(fileFullPath);
        }

        return null;
    }

    [HttpPost]
    public bool DeleteOneFileFolder(AppSefolderDto folderDto)
    {
        if (folderDto != null)
        {
            return AppEsiteFileBL.DeleteOneFileFolder(folderDto);
        }

        return false;
    }

    [HttpGet]
    public AppEsitePagesExDto RetrieveOneWebSiteFile(string fileFullPath, int siteId)
    {
        if (!string.IsNullOrWhiteSpace(fileFullPath))
        {
            return AppEsiteFileBL.RetrieveOneWebSiteFile(fileFullPath, siteId);
        }

        return null;
    }

    //
    //
    [HttpPost]
    public string UpdateAppWebsiteGenericNavigationCtrlJs(AppEsitePageNavigationDto aAppEsitePageNavigationDto)
    {
        int? siteId = ControlTypeValueConverter.ConvertValueToInt(aAppEsitePageNavigationDto.SiteId);

        if (siteId.HasValue)
        {
            var callfuncEXpress = AppEsiteFileBL.UpdateAppWebsiteGenericNavigationCtrlJs(aAppEsitePageNavigationDto);
            return callfuncEXpress;
        }
        else
        {
            return " wrong new Link functiom"; ; ;
        }
    }

    [HttpPost]
    public OperationCallResult<AppEsiteExDto> GenerateWebSiteFromWizardSetting(AppEsiteExDto wizardObj)
    {
        if (wizardObj != null)
        {
            string OriginalString = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}{Request.QueryString}";
            int serverPathEndIndexPosition = OriginalString.IndexOf("webapi");
            wizardObj.RequestHostServerPath = OriginalString.Substring(0, serverPathEndIndexPosition);

            return AppEsiteConfigBL.GenerateWebSiteFromWizardSetting(wizardObj);
        }
        else
        {
            return null;
        }
    }

    [HttpPost]
    public OperationCallResult<bool> SaveAppEsiteMediaQueryChangesToStyleSheet(AppEsiteStyleSheetUpdateDto styleSheetUpdateDto)
    {
        if (styleSheetUpdateDto != null)
        {
            return AppEsiteFileBL.SaveAppEsiteMediaQueryChangesToStyleSheet(styleSheetUpdateDto);
        }
        else
        {
            return null;
        }
    }

    public OperationCallResult<bool> UpdateOneAppWebsiteStyleRule(AppEsiteStyleSheetUpdateDto styleSheetUpdateDto)
    {
        if (styleSheetUpdateDto != null)
        {
            return AppEsiteFileBL.UpdateOneAppWebsiteStyleRule(styleSheetUpdateDto);
        }
        else
        {
            return null;
        }
    }

    [HttpPost]
    public OperationCallResult<bool> SaveGlobalWebsiteThemParameters(AppEsiteExDto appEsiteExDto)
    {
        if (appEsiteExDto != null && appEsiteExDto.EsiteAttribute != null && appEsiteExDto.EsiteAttribute.GlobalSiteThemeParameterList != null)
        {
            return AppEsiteFileBL.SaveGlobalWebsiteThemParameters(appEsiteExDto);
        }
        else
        {
            return null;
        }
    }

    [HttpGet]
    public List<AppEsitePagesDto> RetrieveAppEsiteAllFileInfoDtos(int? eSiteId, string filterByExtention)
    {
        if (eSiteId.HasValue)
        {
            return AppEsiteFileBL.RetrieveAppEsiteAllFileInfoDtos(eSiteId, filterByExtention);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<bool> ExportOneEsitePageToStaticPage(AppEsitePagesDto pageDto)
    {
        if (pageDto != null)
        {
            string OriginalString = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}{Request.QueryString}";

            int serverPathEndIndexPosition = OriginalString.IndexOf("webapi");
            string requestHostServerPath = OriginalString.Substring(0, serverPathEndIndexPosition);

            return AppEsiteFileBL.ExportOneEsitePageToStaticPage(pageDto, null, null, null, requestHostServerPath, true);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<bool> ExportEsiteToStaticSite(int? siteId)
    {
        if (siteId.HasValue)
        {
            string OriginalString = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}{Request.QueryString}";

            int serverPathEndIndexPosition = OriginalString.IndexOf("webapi");
            string requestHostServerPath = OriginalString.Substring(0, serverPathEndIndexPosition);

            return AppEsiteFileBL.ExportEsiteToStaticSite(siteId.Value, requestHostServerPath);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<bool> CreateEsiteWebApplication(int? siteId)
    {
        if (siteId.HasValue)
        {
            return AppEsiteConfigBL.CreateEsiteWebApplication(siteId.Value);
        }

        return null;
    }

    [HttpGet]
    public AppEsiteUserDefinedJsFunctionDto GetAppWebsiteAllUserDefinedJsCode(int? esiteId)
    {
        if (esiteId.HasValue)
        {
            return AppEsiteFileBL.GetAppWebsiteAllUserDefinedJsCode(esiteId.Value);
        }

        return null;
    }

    [HttpGet]
    public AppEsiteUserDefinedJsFunctionDto GetAppWebsiteOneUserDefinedJsFunction(int? esiteId, string functionName)
    {
        if (esiteId.HasValue && !string.IsNullOrWhiteSpace(functionName))
        {
            return AppEsiteFileBL.GetAppWebsiteOneUserDefinedJsFunction(esiteId.Value, functionName);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<bool> UpdateAppWebsiteAllUserDefinedJsCode(AppEsiteUserDefinedJsFunctionDto udFunctionDto)
    {
        if (udFunctionDto != null && udFunctionDto.EsiteId.HasValue)
        {
            var result = AppEsiteFileBL.UpdateAppWebsiteAllUserDefinedJsCode(udFunctionDto);
            return result;
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<bool> UpdateAppWebsiteOneUserDefinedJsFunctionCode(AppEsiteUserDefinedJsFunctionDto udFunctionDto)
    {
        if (udFunctionDto != null && udFunctionDto.EsiteId.HasValue && !string.IsNullOrWhiteSpace(udFunctionDto.FunctionName))
        {
            var result = AppEsiteFileBL.UpdateAppWebsiteOneUserDefinedJsFunctionCode(udFunctionDto);
            return result;
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppEsiteUserDefinedJsFunctionDto> SaveOneUserDefinedJsFunctionDto(AppEsiteUserDefinedJsFunctionDto functionDto)
    {
        if (functionDto != null && functionDto.EsiteId.HasValue)
        {
            var result = AppEsiteFileBL.SaveOneUserDefinedJsFunctionDto(functionDto);
            return result;
        }

        return null;
    }

    [HttpGet]
    public List<AppEsiteUserDefinedJsFunctionDto> GetAlUserDefinedJsFunctionDtoList(int? esiteId)
    {
        if (esiteId.HasValue)
        {
            return AppEsiteFileBL.GetAlUserDefinedJsFunctionDtoList(esiteId.Value);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<bool> GenerateComponentJsFileRerence(AppEsiteUserDefinedJsFunctionDto functionDto)
    {
        if (functionDto != null && functionDto.EsiteId.HasValue && functionDto.FunctionName.HasValue() && functionDto.InitalExpression.HasValue())
        {
            return AppEsiteFileBL.GenerateComponentJsFileRerence(functionDto);
        }

        return null;
    }

    [HttpGet]
    public Dictionary<string, AppEsiteTemplateRegisterDto> RetrieveAllEsiteLayoutTemplates()
    {
        return AppEsiteConfigBL.RetrieveAllEsiteLayoutTemplates();
    }

    [HttpGet]
    public OperationCallResult<bool> SetEsiteLayoutTemplate(int? esiteId, string templateCode)
    {
        if (esiteId.HasValue && !string.IsNullOrWhiteSpace(templateCode))
        {
            return AppEsiteConfigBL.SetEsiteLayoutTemplate(esiteId.Value, templateCode);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<bool> ResetAppWebsite(int? esiteId)
    {
        if (esiteId.HasValue)
        {
            return AppEsiteConfigBL.ResetAppWebsite(esiteId.Value);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppEsiteExDto> CreatNextJsApp(AppEsiteExDto appEsiteExDto)
    {
        if (appEsiteExDto != null)
        {
            string OriginalString = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}{Request.QueryString}";

            int serverPathEndIndexPosition = OriginalString.IndexOf("webapi");
            string requestHostServerPath = OriginalString.Substring(0, serverPathEndIndexPosition);

            return AppNextJsAppConfigBL.CreatNextJsApp(appEsiteExDto, requestHostServerPath);
        }
        else
        {
            return null;
        }
    }

    [HttpPost]
    public OperationCallResult<AppEsiteExDto> UpdateNextJsApp(AppEsiteExDto appEsiteExDto)
    {
        if (appEsiteExDto != null)
        {
            return AppNextJsAppConfigBL.UpdateNextJsApp(appEsiteExDto);
        }
        else
        {
            return null;
        }
    }

    [HttpGet]
    public AppEsiteExDto RetrieveOneNextJsAppExDto(int? esiteId)
    {
        if (esiteId.HasValue)
        {
            return AppNextJsAppConfigBL.RetrieveOneNextJsAppExDto(esiteId.Value);
        }
        return null;
    }

    [HttpPost]
    public AppEsitePagesExDto RetrieveOneNextJsPageDto(AppEsitePagesDto pageDto)
    {
        if (pageDto != null)
        {
            if (!string.IsNullOrWhiteSpace(pageDto.FileFullPath) && pageDto.EsiteId.HasValue)
            {
                string fileFullPath = pageDto.FileFullPath;
                int siteId = pageDto.EsiteId.Value;
                bool isReactPageHtmlLayoutOnly = pageDto.IsHtmlLayoutOnly;

                return AppNextJsAppConfigBL.RetrieveOneNextJsPageDto(fileFullPath, siteId, isReactPageHtmlLayoutOnly);
            }
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppEsitePagesExDto> SaveOneNextJsPageLayout(AppEsitePagesExDto pageDto)
    {
        if (pageDto != null)
        {
            return AppNextJsAppConfigBL.SaveOneNextJsPageLayout(pageDto);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppEsitePagesExDto> CreateNewNextJsPageInSubFolder(AppEsitePagesExDto pageDto)
    {
        if (pageDto != null)
        {
            return AppNextJsAppConfigBL.CreateNewNextJsPageInSubFolder(pageDto);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppEsitePagesExDto> CreateNewNextComponent(AppEsitePagesExDto pageDto)
    {
        if (pageDto != null)
        {
            return AppNextJsAppConfigBL.CreateNewNextComponent(pageDto);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<string> StartNextJsAppTestServer(int? esiteId)
    {
        if (esiteId.HasValue)
        {
            return AppNextJsAppConfigBL.StartNextJsAppTestServer(esiteId.Value);
        }
        return null;
    }

    [HttpPost]
    public async Task<OperationCallResult<AppGitHubConfigDto>> CreateGitHubRepository(AppGitHubConfigDto gitDto)
    {
        if (gitDto != null)
        {
            var response = await AppGitHubBL.CreateGitHubRepository(gitDto);

            return response;
        }

        return null;
    }

    [HttpPost]
    public async Task<OperationCallResult<AppGitHubConfigDto>> CreateNextJsAppGitHubRepository(AppGitHubConfigDto gitDto)
    {
        if (gitDto != null)
        {
            var response = await AppGitHubBL.CreateNextJsAppGitHubRepository(gitDto);
            return response;
        }

        return null;
    }

    [HttpPost]
    public async Task<OperationCallResult<AppGitHubConfigDto>> PushAllFilesToGitFromOneFolder(AppGitHubConfigDto gitDto)
    {
        if (gitDto != null)
        {
            var response = await AppGitHubBL.PushAllFilesToGitFromOneFolder(gitDto);
            return response;
        }

        return null;
    }

    [HttpPost]
    public async Task<OperationCallResult<AppGitHubConfigDto>> PushAllFilesToGitFromOneNextJsApp(AppGitHubConfigDto gitDto)
    {
        if (gitDto != null)
        {
            var response = await AppGitHubBL.PushAllFilesToGitFromOneNextJsApp(gitDto);
            return response;
        }

        return null;
    }

    [HttpPost]
    public async Task<OperationCallResult<AppGitHubConfigDto>> PushOneNextJsAppFileToGit(AppGitHubConfigDto gitDto)
    {
        if (gitDto != null)
        {
            var response = await AppGitHubBL.PushOneNextJsAppFileToGit(gitDto);
            return response;
        }

        return null;
    }

    [HttpPost]
    public async Task<OperationCallResult<AppGitHubConfigDto>> PullAllFilesFromGitToNextJsApp(AppGitHubConfigDto gitDto)
    {
        if (gitDto != null)
        {
            var response = await AppGitHubBL.PullAllFilesFromGitToNextJsApp(gitDto);
            return response;
        }

        return null;
    }

    [HttpPost]
    public async Task<OperationCallResult<AppGitHubConfigDto>> PullOneNextJsAppFileFromGit(AppGitHubConfigDto gitDto)
    {
        if (gitDto != null)
        {
            var response = await AppGitHubBL.PullOneNextJsAppFileFromGit(gitDto);
            return response;
        }

        return null;
    }

    [HttpPost]
    public async Task<OperationCallResult<string>> TestRepositoryConnection(AppGitHubConfigDto gitDto)
    {
        if (gitDto != null)
        {
            var response = await AppGitHubBL.TestRepositoryConnection(gitDto);
            return response;
        }

        return null;
    }
}
