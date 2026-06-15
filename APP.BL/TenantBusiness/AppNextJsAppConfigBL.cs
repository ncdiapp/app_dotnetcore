using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Globalization;

using System;



using APP.Components.EntityDto;
using System.Data.Common;
using APP.Components.Dto;
using System.Diagnostics;
using System.IO;
using AngleSharp.Dom;
using AngleSharp;
using System.Text;
using System.Threading.Tasks;
using APP.Components.EntityConverter;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.LBL;
using System.Text.RegularExpressions;
using APP.LBL.HelperClasses;
using System.Net.Http;
using System.Net.Sockets;


using APP.Framework;
namespace App.BL
{

    public static class AppNextJsAppConfigBL
    {
        public static readonly List<string> HtmlSelfClosingTags = new List<string>
        {
            "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "source", "track", "wbr"
        };

        public static readonly int BaseSiteId = 1;

        public static OperationCallResult<string> StartNextJsAppTestServer(int siteId)
        {
            OperationCallResult<string> aOperationCallResult = new OperationCallResult<string>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            int? companyId = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentCompanyId);
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string nextJsFolderPath = string.Format(@"{0}FileRepository\Company_{1}\WebSite\Site_{2}\", baseDirectory, companyId, siteId);


            try
            {
                if (string.IsNullOrEmpty(nextJsFolderPath) || !Directory.Exists(nextJsFolderPath))
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_StartNextJsAppTestServer_Error", ValidationItemType.Error, "NextJs Application Folder Does Not Exist."));

                    return aOperationCallResult;
                }

                if (!Directory.Exists(nextJsFolderPath + "node_modules\\"))
                {
                    string npmInstallOutput = ExecuteSystemProcessCommand("npm install", nextJsFolderPath);

                    if (string.IsNullOrWhiteSpace(npmInstallOutput))
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_StartNextJsAppTestServer_Error", ValidationItemType.Error, "NextJs Application Start Failed. Failed to run npm install."));
                        return aOperationCallResult;
                    }
                }


                var existingPort = GetNextJsAppTestServerRunningPort(nextJsFolderPath);

                if (existingPort.HasValue)
                {
                    string runningUrl = $"http://localhost:{existingPort}";
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_StartNextJsAppTestServer_Error", ValidationItemType.Message, "NextJs App already running at port: " + existingPort.Value));

                    aOperationCallResult.Object = existingPort.Value.ToString();
                }

                else
                {

                    if (_dictFolderPathAndRunningProcess.ContainsKey(nextJsFolderPath))
                    {
                        StopLongRunningSystemProcess(_dictFolderPathAndRunningProcess[nextJsFolderPath]);
                        _dictFolderPathAndRunningProcess[nextJsFolderPath] = null;
                    }


                    int port = FindNextAvailablePort();

                    ExecuteSystemProcessCommand($"npm run dev -- --port {port}", nextJsFolderPath, true, message =>
                    {
                        //if (message.StartsWith("ERROR:"))
                        //{
                        //    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_StartNextJsAppTestServer_Error", ValidationItemType.Error, "Starting NextJs Test Server Failed. " + message));
                        //}
                        //else
                        //{
                        //    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_StartNextJsAppTestServer_Message", ValidationItemType.Message, "Starting NextJs Test Server at port " + port + ". " + message));

                        //    RegisterNextJsTestServer(nextJsFolderPath, port);

                        //    aOperationCallResult.Object = port.ToString();
                        //}

                    });


                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_StartNextJsAppTestServer_Message", ValidationItemType.Message, "Starting NextJs Test Server at port " + port + "."));

                    RegisterNextJsTestServer(nextJsFolderPath, port);

                    aOperationCallResult.Object = port.ToString();

                }

            }
            catch (Exception ex)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_StartNextJsAppTestServer_Error", ValidationItemType.Error, ex.ToString()));
            }


            return aOperationCallResult;
        }


        public static AppEsiteExDto RetrieveOneNextJsAppExDto(object AppEsiteId)
        {
            AppEsiteEntity aAppEsiteEntity = RetrieveOneNextJsAppEntity(AppEsiteId);
            AppEsiteExDto aAppEsiteDto = AppEsiteConverter.ConvertEntityToExDto(aAppEsiteEntity);

            aAppEsiteDto.RootFolderPath = AppEsiteFileBL.GetCompanyWebSiteBaseFolderPath((int)AppEsiteId);




            //foreach (var o in aAppEsiteEntity.AppEsitePages.OrderBy(o => o.LoadOrder))
            //{
            //    AppEsitePagesExDto appEsitePagesExDto = AppEsitePagesConverter.ConvertEntityToExDto(o);
            //    PrepreSitePageTypeDisplay(appEsitePagesExDto);

            //    aAppEsiteDto.AppEsitePagesList.Add(appEsitePagesExDto);
            //}



            //PrepareGlobalSiteThemeParameterList(aAppEsiteDto);

            //PrepareEsiteThirdPartControlThemeNameList(aAppEsiteDto);

            //PrepareUserDefinedJsFunctionDtoList(aAppEsiteDto);

            //PrepareEsiteComponentConfig(aAppEsiteDto);

            //PrepareEsitePartnerMapping(aAppEsiteDto);   

            return aAppEsiteDto;
        }

        public static OperationCallResult<AppEsiteExDto> GenerateNextJsApplicationFilesFromTemplate(string siteId)
        {
            OperationCallResult<AppEsiteExDto> aOperationCallResult = new OperationCallResult<AppEsiteExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            int? companyId = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentCompanyId);
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string appSitePath = string.Format(@"{0}FileRepository\Company_{1}\WebSite\Site_{2}\", baseDirectory, companyId, siteId);
            string templatePath = string.Format(@"{0}FileRepDevWebsiteTemplate\React\NextJsClient\", baseDirectory);

            if (!Directory.Exists(appSitePath))
            {
                Directory.CreateDirectory(appSitePath);
            }

            AppEsiteFileBL.CopyFolderToNewLocation(templatePath, appSitePath);

            //AppEsiteFileBL.UpdateUserCompanyDefaultSiteMainVariablesJsPage(companyId, applicationCode);

            return aOperationCallResult;
        }

        public static OperationCallResult<AppEsiteExDto> CreatNextJsApp(AppEsiteExDto aAppEsiteExDto, string requestHostServerPath)
        {
            OperationCallResult<AppEsiteExDto> aOperationCallResult = new OperationCallResult<AppEsiteExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (aAppEsiteExDto.EsiteAttribute == null)
            {
                aAppEsiteExDto.EsiteAttribute = new EsiteAttributeDto();
            }

            aAppEsiteExDto.EmApplicationType = (int)EmAppESiteApplicationType.NextJsApplication;

            if (!string.IsNullOrWhiteSpace(requestHostServerPath))
            {
                aAppEsiteExDto.EsiteAttribute.MgtSiteBaseUrl = requestHostServerPath;
            }



            AppEsiteEntity aAppEsiteEntity = new AppEsiteEntity();
            AppEsiteConverter.CopyDtoToEntity(aAppEsiteEntity, aAppEsiteExDto);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppEsiteEntity);
                    adapter.Commit();

                    aAppEsiteExDto.Id = aAppEsiteEntity.EsiteId;
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

                // Entity Logical Validation Exception
                catch (ORMEntityValidationException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                }

                // Database FK Exeption ........
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }


            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneNextJsAppExDto(aAppEsiteExDto.Id);

                GenerateNextJsApplicationFilesFromTemplate(aAppEsiteExDto.Id.ToString());


                UpdateNextJsAppEnvLocalFile(aAppEsiteExDto);
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<AppEsiteExDto> UpdateNextJsApp(AppEsiteExDto aAppEsiteExDto)
        {
            OperationCallResult<AppEsiteExDto> aOperationCallResult = new OperationCallResult<AppEsiteExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;



            if (!aAppEsiteExDto.IsNew)
            {
                var orgEsiteExDto = RetrieveOneNextJsAppExDto(aAppEsiteExDto.Id);

                AppEsiteEntity aAppEsiteEntity = RetrieveOneNextJsAppEntity(aAppEsiteExDto.Id);
                AppEsiteConverter.CopyDtoToEntity(aAppEsiteEntity, aAppEsiteExDto);


                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppEsiteEntity);


                        adapter.Commit();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }

                    // Entity Logical Validation Exception
                    catch (ORMEntityValidationException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                    }

                    // Concurrency validation ("Concurrency violation, data not updated")
                    catch (ORMConcurrencyException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_Concurrency_Error", ValidationItemType.Error, ex.ToString()));
                    }

                    // Database FK Exception .......
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }



                if (!aValidationResult.HasErrors)
                {
                    aOperationCallResult.Object = RetrieveOneNextJsAppExDto(aAppEsiteExDto.Id);

                    //UpdateNextJsAppEnvLocalFile(aAppEsiteExDto);
                }
            }

            return aOperationCallResult;
        }


        public static void UpdateNextJsAppEnvLocalFile(AppEsiteExDto aAppEsiteExDto)
        {

            int EsiteId = (int)aAppEsiteExDto.Id;
            string envLocalFilePath = ".env.local";

            string webSiteStartPath = AppEsiteFileBL.GetWebSiteBasePath(EsiteId);
            string envLocalFileFullPath = Path.Combine(webSiteStartPath, envLocalFilePath);

            if (File.Exists(envLocalFileFullPath))
            {
                string originalString = File.ReadAllText(envLocalFileFullPath);

                string[] lineList = originalString.Split(Environment.NewLine.ToArray());
                foreach (string line in lineList)
                {
                    if (line.Contains("NEXT_PUBLIC_MGT_BASE_URL"))
                    {
                        if (aAppEsiteExDto.EsiteAttribute != null && !string.IsNullOrWhiteSpace(aAppEsiteExDto.EsiteAttribute.MgtSiteBaseUrl))
                        {
                            originalString = originalString.Replace(line, string.Format("NEXT_PUBLIC_MGT_BASE_URL = {0}", aAppEsiteExDto.EsiteAttribute.MgtSiteBaseUrl));
                        }
                    }
                    else if (line.Contains("NEXT_PUBLIC_SITE_ID"))
                    {
                        originalString = originalString.Replace(line, string.Format("NEXT_PUBLIC_SITE_ID = {0}", EsiteId));
                    }
                }

                File.WriteAllText(envLocalFileFullPath, originalString);
            }
        }

        public static AppEsitePagesExDto RetrieveOneNextJsPageDto(string fileFullPath, int siteId, bool isHtmlLayoutOnly)
        {
            AppEsitePagesExDto filePageDto = new AppEsitePagesExDto();
            FileInfo fileInfo = new FileInfo(fileFullPath);

            string appSitePath = AppEsiteFileBL.GetWebSiteBasePath(siteId);

            var fromDbPageDto = AppEsiteConfigBL.RetrieveOneFileExtentPageDto(fileFullPath, siteId);

            if (fromDbPageDto != null)
            {
                filePageDto = fromDbPageDto;
            }
            else
            {
                filePageDto.Title = fileInfo.Name;
                filePageDto.EsiteId = siteId;

                if (fileFullPath.Contains("StaticPages"))
                {
                    filePageDto.IsStaticSitePage = true;
                }

            }

            AppEsiteFileBL.AddFileAttributesToAppEsitePagesDto(fileInfo, filePageDto);

            if (filePageDto.FileFullPath.HasValue())
            {
                filePageDto.Description = filePageDto.FileFullPath.Substring(appSitePath.Length).Replace("\\", "/");
            }

            string text = File.ReadAllText(fileFullPath);
            filePageDto.HtmlContent = text;

            filePageDto.IsHtmlLayoutOnly = isHtmlLayoutOnly;

            if (isHtmlLayoutOnly)
            {
                string htmlLayout = GetNextJsPageHtmlLayout(filePageDto.HtmlContent);

                filePageDto.HtmlContent = htmlLayout;

                filePageDto.DictSeoSettingKeyAndValue = GetNextJsPageDictSeoSettingKeyAndValue(fileFullPath);

                filePageDto.PathParameterList = GetNextJsPagePathParameters(fileFullPath);
            }

            return filePageDto;
        }



        public static string ConvertToValidPageName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "";
            }

            string result = Regex.Replace(input, @"[^a-zA-Z0-9]+", "", RegexOptions.Compiled);

            if (result.Length > 0 && Char.IsDigit(result[0]))
            {
                result = "Page" + result;
            }

            return result;
        }

        public static string ConvertToValidComponentName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "";
            }

            input = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input);

            string result = Regex.Replace(input, @"[^a-zA-Z0-9]+", "", RegexOptions.Compiled);

            if (result.Length > 0 && Char.IsDigit(result[0]))
            {
                result = "Component" + result;
            }

            return result;
        }

        public static OperationCallResult<AppEsitePagesExDto> CreateNewNextJsPageInSubFolder(AppEsitePagesExDto appEsitePagesDto)
        {
            OperationCallResult<AppEsitePagesExDto> aOperationCallResult = new OperationCallResult<AppEsitePagesExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            string newFolderName = appEsitePagesDto.Title.Trim();
            string parentFolderPath = appEsitePagesDto.FileFullPath;
            if (parentFolderPath.HasValue())
            {
                bool isDynamicRouteFolder = newFolderName.StartsWith("[") && newFolderName.EndsWith("]");

                string pageName = ConvertToValidPageName(newFolderName);

                if (string.IsNullOrWhiteSpace(pageName))
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_SaveAppEsitePagesExDtoWithFile_Error", ValidationItemType.Error, "Invalid page name."));
                    return aOperationCallResult;
                }

                if (isDynamicRouteFolder)
                {
                    newFolderName = "[" + pageName.ToLower() + "]";
                    pageName = GetPageNameBeforeDynamicFolders(parentFolderPath);
                }
                else
                {
                    newFolderName = pageName.ToLower();
                }

                string newPageFileName = "page.tsx";
                string newPageLayoutFileName = "pageMarkup.tsx";



                string siteBasePath = AppEsiteFileBL.GetCompanyWebSiteBaseFolderPath(appEsitePagesDto.EsiteId.Value);
                string htmlTemplateContent = appEsitePagesDto.HtmlContent;


                parentFolderPath = parentFolderPath.Replace("/", "\\");

                if (parentFolderPath.IndexOf(siteBasePath) == -1)
                {
                    parentFolderPath = siteBasePath + parentFolderPath;
                }

                string newFolderFullPath = Path.Combine(parentFolderPath, newFolderName);

                string newPageFileFullPath = Path.Combine(newFolderFullPath, newPageFileName);
                string newPageStyleFileFullPath = Path.Combine(newFolderFullPath, newPageLayoutFileName);

                try
                {
                    Directory.CreateDirectory(newFolderFullPath);

                    string htmlContent = "";

                    if (!File.Exists(newPageFileFullPath) && !File.Exists(newPageStyleFileFullPath))
                    {
                        using (FileStream fs = File.Create(newPageFileFullPath))
                        {
                            htmlContent = _newTsxPageTemplate.Replace("NewPageName", pageName);

                            int indexOfSrcFolder = newFolderFullPath.IndexOf("src\\app");
                            if (indexOfSrcFolder >= 0)
                            {
                                string subFolderPath = newFolderFullPath.Substring(indexOfSrcFolder + "src\\app".Length);
                                int level = subFolderPath.Count(c => c == '\\');

                                if (level >= 1)
                                {
                                    string pathPrefix = "'../";
                                    for (int i = 1; i <= level; i++)
                                    {
                                        pathPrefix += "../";
                                    }

                                    htmlContent = htmlContent.Replace("'../", pathPrefix);
                                }
                            }


                            Byte[] title = new UTF8Encoding(true).GetBytes(htmlContent);
                            fs.Write(title, 0, title.Length);
                        }

                        using (FileStream fs = File.Create(newPageStyleFileFullPath))
                        {

                            htmlContent = _newTsxPageLayoutTemplate;

                            if (!string.IsNullOrWhiteSpace(htmlTemplateContent))
                            {
                                htmlContent = UpdateNextJsPageHtmlLayout(htmlContent, htmlTemplateContent, appEsitePagesDto.EsiteId);
                            }

                            Byte[] title = new UTF8Encoding(true).GetBytes(htmlContent);
                            fs.Write(title, 0, title.Length);
                        }
                    }
                    else
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_SaveAppEsitePagesExDtoWithFile_Error", ValidationItemType.Error, "Page already exists."));
                        return aOperationCallResult;
                    }


                    string appSitePath = AppEsiteFileBL.GetWebSiteBasePath(appEsitePagesDto.EsiteId);

                    var toReturnPageDto = RetrieveOneNextJsPageDto(newPageStyleFileFullPath, appEsitePagesDto.EsiteId.Value, false);

                    if (toReturnPageDto.FileFullPath.HasValue())
                    {
                        toReturnPageDto.Description = toReturnPageDto.FileFullPath.Substring(appSitePath.Length).Replace("\\", "/");

                    }

                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    //toReturnPageDto.HtmlContent = htmlContent;
                    aOperationCallResult.Object = toReturnPageDto;

                }
                catch (Exception ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_SaveAppEsitePagesExDtoWithFile_Error", ValidationItemType.Error, ex.ToString()));
                }



            }


            return aOperationCallResult;

        }


        public static OperationCallResult<AppEsitePagesExDto> CreateNewNextComponent(AppEsitePagesExDto appEsitePagesDto)
        {
            OperationCallResult<AppEsitePagesExDto> aOperationCallResult = new OperationCallResult<AppEsitePagesExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            string componentName = ConvertToValidComponentName(appEsitePagesDto.Title);

            if (string.IsNullOrWhiteSpace(componentName))
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_SaveAppEsitePagesExDtoWithFile_Error", ValidationItemType.Error, "Invalid page name."));
                return aOperationCallResult;
            }


            string newPageFileName = componentName + ".tsx";

            string folderPath = appEsitePagesDto.FileFullPath;

            string siteBasePath = AppEsiteFileBL.GetCompanyWebSiteBaseFolderPath(appEsitePagesDto.EsiteId.Value);
            //string htmlTemplateContent = appEsitePagesDto.HtmlContent;

            if (folderPath.HasValue())
            {
                folderPath = folderPath.Replace("/", "\\");

                if (folderPath.IndexOf(siteBasePath) == -1)
                {
                    folderPath = siteBasePath + folderPath;
                }

                string newPageFileFullPath = Path.Combine(folderPath, newPageFileName);

                try
                {

                    string htmlContent = "";

                    if (!File.Exists(newPageFileFullPath))
                    {
                        using (FileStream fs = File.Create(newPageFileFullPath))
                        {
                            htmlContent = _newTsxComponentTemplate.Replace("NewComponentName", componentName);

                            int indexOfSrcFolder = folderPath.IndexOf("src\\components");
                            if (indexOfSrcFolder >= 0)
                            {
                                string subFolderPath = folderPath.Substring(indexOfSrcFolder + "src\\components".Length);
                                int level = subFolderPath.Count(c => c == '\\');

                                if (level >= 1)
                                {
                                    string pathPrefix = "'../";
                                    for (int i = 1; i <= level; i++)
                                    {
                                        pathPrefix += "../";
                                    }

                                    htmlContent = htmlContent.Replace("'../", pathPrefix);
                                }
                            }


                            Byte[] title = new UTF8Encoding(true).GetBytes(htmlContent);
                            fs.Write(title, 0, title.Length);
                        }
                    }
                    else
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_SaveAppEsitePagesExDtoWithFile_Error", ValidationItemType.Error, "Page already exists."));
                        return aOperationCallResult;
                    }


                    string appSitePath = AppEsiteFileBL.GetWebSiteBasePath(appEsitePagesDto.EsiteId);

                    var toReturnPageDto = RetrieveOneNextJsPageDto(newPageFileFullPath, appEsitePagesDto.EsiteId.Value, false);

                    if (toReturnPageDto.FileFullPath.HasValue())
                    {
                        toReturnPageDto.Description = toReturnPageDto.FileFullPath.Substring(appSitePath.Length).Replace("\\", "/");

                    }

                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    //toReturnPageDto.HtmlContent = htmlContent;
                    aOperationCallResult.Object = toReturnPageDto;

                }
                catch (Exception ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_SaveAppEsitePagesExDtoWithFile_Error", ValidationItemType.Error, ex.ToString()));
                }



            }


            return aOperationCallResult;

        }

        public static OperationCallResult<AppEsitePagesExDto> SaveOneNextJsPageLayout(AppEsitePagesExDto appEsitePagesDto)
        {
            OperationCallResult<AppEsitePagesExDto> aOperationCallResult = new OperationCallResult<AppEsitePagesExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            string fileNameFullPath = appEsitePagesDto.FileFullPath;

            string siteBasePath = AppEsiteFileBL.GetCompanyWebSiteBaseFolderPath(appEsitePagesDto.EsiteId.Value);

            if (appEsitePagesDto.EsiteId.HasValue)
            {
                if (string.IsNullOrWhiteSpace(fileNameFullPath))
                {
                    if (appEsitePagesDto.CreatedFromFilePath.HasValue() && appEsitePagesDto.Title.HasValue())
                    {
                        fileNameFullPath = appEsitePagesDto.CreatedFromFilePath.Substring(0, appEsitePagesDto.CreatedFromFilePath.LastIndexOf("\\") + 1) + appEsitePagesDto.Title;
                    }
                }
            }

            if (fileNameFullPath.HasValue())
            {
                if (fileNameFullPath.IndexOf(siteBasePath) == -1)
                {
                    fileNameFullPath = siteBasePath + fileNameFullPath;
                }

                appEsitePagesDto.FileFullPath = fileNameFullPath;

                // need to remove the Base
                try
                {

                    Directory.CreateDirectory(Path.GetDirectoryName(fileNameFullPath));



                    string htmlContent = appEsitePagesDto.HtmlContent;
                    string fileExtension = string.Empty;

                    if (!File.Exists(fileNameFullPath))
                    {
                        using (FileStream fs = File.Create(fileNameFullPath))
                        {
                            if (appEsitePagesDto.IsHtmlLayoutOnly)
                            {
                                if (string.IsNullOrWhiteSpace(htmlContent))
                                {
                                    htmlContent = @"
<div class='p-5'>
        
</div>";
                                }

                                string newPageFolder = AppEsiteFileBL.GetFileParentFolderName(fileNameFullPath);

                                //string originalFileContent = _newTsxPageTemplate.Replace("NewPageName", newPageFolder);
                                htmlContent = UpdateNextJsPageHtmlLayout(_newTsxPageLayoutTemplate, htmlContent, appEsitePagesDto.EsiteId);
                            }

                            Byte[] title = new UTF8Encoding(true).GetBytes(htmlContent);
                            fs.Write(title, 0, title.Length);
                        }
                    }
                    else
                    {
                        string fileContent = File.ReadAllText(fileNameFullPath);

                        if (appEsitePagesDto.IsHtmlLayoutOnly)
                        {
                            if (string.IsNullOrWhiteSpace(htmlContent))
                            {
                                htmlContent = @"
<div class='p-5'>
        
</div>";
                            }

                            htmlContent = UpdateNextJsPageHtmlLayout(fileContent, htmlContent, appEsitePagesDto.EsiteId);
                        }

                        File.WriteAllText(fileNameFullPath, htmlContent);
                    }

                    if (appEsitePagesDto.IsSeoSettingChanged)
                    {
                        UpdateNextDataModelPageSeoSetting(appEsitePagesDto, fileNameFullPath);
                    }

                    string appSitePath = AppEsiteFileBL.GetWebSiteBasePath(appEsitePagesDto.EsiteId);

                    var toReturnPageDto = RetrieveOneNextJsPageDto(appEsitePagesDto.FileFullPath, appEsitePagesDto.EsiteId.Value, false);

                    if (toReturnPageDto.FileFullPath.HasValue())
                    {
                        toReturnPageDto.Description = toReturnPageDto.FileFullPath.Substring(appSitePath.Length).Replace("\\", "/");

                    }

                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    //toReturnPageDto.HtmlContent = htmlContent;
                    aOperationCallResult.Object = toReturnPageDto;




                }
                catch (Exception ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_SaveAppEsitePagesExDtoWithFile_Error", ValidationItemType.Error, ex.ToString()));
                }



            }


            return aOperationCallResult;






        }


        public static void RunCommand(string command, string arguments)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo(command, arguments)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(processStartInfo))
            {
                process.WaitForExit();
                Console.WriteLine(process.StandardOutput.ReadToEnd());
            }
        }

        public static void ConfigureTailwind(string projectPath)
        {
            string tailwindConfigPath = Path.Combine(projectPath, "tailwind.config.js");
            string content = @"
                module.exports = {
                  content: [
                    './pages/**/*.{js,ts,jsx,tsx}',
                    './components/**/*.{js,ts,jsx,tsx}'
                  ],
                  theme: {
                    extend: {},
                  },
                  plugins: [],
                }";

            File.WriteAllText(tailwindConfigPath, content);

            string globalsPath = Path.Combine(projectPath, "styles", "globals.css");
            File.AppendAllText(globalsPath, @"
                @tailwind base;
                @tailwind components;
                @tailwind utilities;
                ");
        }

        public static void ConvertHtmlTemplatesToNextJsPages(string projectPath)
        {
            string htmlTemplatesPath = Path.Combine(Directory.GetCurrentDirectory(), "html-templates");
            string pagesPath = Path.Combine(projectPath, "pages");

            if (Directory.Exists(htmlTemplatesPath))
            {
                foreach (string file in Directory.GetFiles(htmlTemplatesPath, "*.html"))
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string nextJsPagePath = Path.Combine(pagesPath, $"{fileName}.js");

                    // Example HTML to JSX conversion (simple replacements)
                    string htmlContent = File.ReadAllText(file);
                    string jsxContent = ConvertHtmlToJsx(htmlContent);

                    File.WriteAllText(nextJsPagePath, jsxContent);
                }
            }
        }

        //public static string ConvertHtmlToJsx(string htmlContent)
        //{
        //    // Perform simple replacements to convert HTML to JSX
        //    return htmlContent
        //        .Replace("class=", "className=")
        //        .Replace("<!DOCTYPE html>", "")
        //        .Replace("<html>", "<div>")
        //        .Replace("</html>", "</div>")
        //        .Replace("<body>", "<div>")
        //        .Replace("</body>", "</div>");
        //}

        public static string ConvertHtmlToJsx(string html, int? siteId = null)
        {
            var dictTempKeyAndOrgValue = new Dictionary<string, string>();
            html = ConvertHtmlToJsx_PreProcess(html, dictTempKeyAndOrgValue, siteId);

            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            var document = context.OpenAsync(req => req.Content(html)).GetAwaiter().GetResult();
            var node = document.Body;

            var jsxBuilder = new StringBuilder();
            ConvertHtmlNodeToJsx(node, jsxBuilder, 0);

            return ConvertHtmlToJsx_PostProcess(jsxBuilder.ToString(), dictTempKeyAndOrgValue, siteId);
        }

        //private static string ConvertHtmlToJsx_PreProcess(string html)
        //{           
        //    return html.Replace("<Link", "[JSX_LINK]")
        //        .Replace("</Link>", "[/JSX_LINK]")
        //        .Replace("`", "[TEMPLATE_LITERAL]")
        //        .Replace("href=", "[HREF_START]");
        //}

        //private static string ConvertHtmlToJsx_PostProcess(string jsx)
        //{
        //    return jsx.Replace("[JSX_LINK]", "<Link")
        //        .Replace("[/JSX_LINK]", "</Link>")
        //        .Replace("[TEMPLATE_LITERAL]", "`")
        //        .Replace("[HREF_START]", "href=");
        //}

        //private static string ConvertHtmlToJsx_PreProcess(string html)
        //{
        //    return html.Replace("<Link", "<applink")
        //        .Replace("</Link>", "</applink>")
        //        .Replace("`", "[TEMPLATE_LITERAL]")
        //        .Replace("href=", "[HREF_START]");
        //}

        //private static string ConvertHtmlToJsx_PostProcess(string jsx)
        //{            
        //    return jsx.Replace("<applink", "<Link")
        //        .Replace("</applink>", "</Link>")
        //        .Replace("[TEMPLATE_LITERAL]", "`")
        //        .Replace("[HREF_START]", "href=");
        //}



        public static string ConvertHtmlToJsx_PreProcess(string inputHtml, Dictionary<string, string> dictTempKeyAndOrgValue, int? siteId)
        {


            int counter = 1;
            string updatedHtml = inputHtml
                .Replace("<Link", "<applink")
                .Replace("</Link>", "</applink>")
                .Replace("defaultValue", "appdefaultvalue")
                .Replace("formAction", "appformaction")


                .Replace("mainMenuLabel=", "appmainmenulabel=")
                .Replace("mainMenuUrl=", "appmainmenuurl=")
                .Replace("childMenuData=", "appchildmenudata=");

                //.Replace("<Header", "<appheader")
                //.Replace("</Header>", "</appheader>")
                //.Replace("<Footer", "<appfooter")
                //.Replace("</Footer>", "</appfooter>")
                //.Replace("<Footer", "<appfooter")
                //.Replace("<ExampleUseAppContext", "<exampleuseappcontext")
                //.Replace("</ExampleUseAppContext>", "</exampleuseappcontext>")

                //.Replace("<DropdownMenu", "<appdropdownmenu")
                //.Replace("</DropdownMenu>", "</appdropdownmenu>")

                //.Replace("<AddToShoppingCartButton", "<addtoshoppingcartbutton")
                //.Replace("</AddToShoppingCartButton>", "</addtoshoppingcartbutton>")
                //.Replace("<LoginForm", "<loginform")
                //.Replace("</LoginForm>", "</loginform>")
                //.Replace("<MyAccountButton", "<myaccountbutton")
                //.Replace("</MyAccountButton>", "</myaccountbutton>")
                //.Replace("<ShoppingCartButton", "<shoppingcartbutton")
                //.Replace("</ShoppingCartButton>", "</shoppingcartbutton>")
                //.Replace("<ShoppingCartPopup", "<shoppingcartpopup")
                //.Replace("</ShoppingCartPopup>", "</shoppingcartpopup>")
                //.Replace("<PlaceOrderPopup", "<placeorderpopup")
                //.Replace("</PlaceOrderPopup>", "</placeorderpopup>")
                //.Replace("<ImageViewer", "<imageviewer")
                //.Replace("</ImageViewer>", "</imageviewer>")
                //.Replace("<DistributorDropdown", "<distributordropdown")
                //.Replace("</DistributorDropdown>", "</distributordropdown>")
                //.Replace("<DistributorSelectorPopup", "<distributorselectorpopup")
                //.Replace("</DistributorSelectorPopup>", "</distributorselectorpopup>")
                //.Replace("<MultiItemSelector", "<multiitemselector")
                //.Replace("</MultiItemSelector>", "</multiitemselector>")
                //.Replace("<SendMesssageButton", "<sendmesssagebutton")
                //.Replace("</SendMesssageButton>", "</sendmesssagebutton>")
                //.Replace("<SendMessagePopup", "<sendmessagepopup")
                //.Replace("</SendMessagePopup>", "</sendmessagepopup>");

            if (siteId.HasValue)
            {
                updatedHtml=  ConvertComponentTagsToLowerCase(updatedHtml, siteId.Value);
            }


            // Regex to match only attributes with `{...}` syntax inside HTML tags (i.e., between < and >)
            string tagRegexPattern = @"<[^>]*?>";

            updatedHtml = Regex.Replace(updatedHtml, tagRegexPattern, (tagMatch) =>
            {
                string tag = tagMatch.Value;

                // Regular expression to match attributes with `{...}` syntax within the tag content
                string attrRegexPattern = @"(\w+)=\{([^{}]*?(?:\{[^{}]*?\}[^{}]*?)*?)\}";

                return Regex.Replace(tag, attrRegexPattern, (attrMatch) =>
                {
                    string attr = attrMatch.Groups[1].Value;
                    string value = attrMatch.Groups[2].Value;
                    string tempKey = $"SpecialStringPlaceholder{counter++}";

                    // Save the original value in the dictionary
                    dictTempKeyAndOrgValue[tempKey] = $"{{{value}}}";

                    // Replace the attribute value with a placeholder
                    return $"{attr}=\"{{{tempKey}}}\"";
                });
            });

            return updatedHtml;
        }


       

        public static string ConvertHtmlToJsx_PostProcess(string inputHtml, Dictionary<string, string> dictTempKeyAndOrgValue, int? siteId)
        {
            var dictKeyAndOrgValue = dictTempKeyAndOrgValue;

            //string placeholderPattern = @"\{(SpecialStringPlaceholder\d+)\}";
            string placeholderPattern = @"""\{(SpecialStringPlaceholder\d+)\}""";

            string updatedHtml = Regex.Replace(inputHtml, placeholderPattern, (placeholderMatch) =>
            {
                string key = placeholderMatch.Groups[1].Value;

                // Retrieve the original value from the dictionary
                if (dictTempKeyAndOrgValue.TryGetValue(key, out string originalValue))
                {
                    return originalValue;
                }

                return placeholderMatch.Value;  // Return the match if no original value found
            });

            // Clear the dictionary after use
            dictTempKeyAndOrgValue = null;

            updatedHtml = updatedHtml
                .Replace("<applink", "<Link")
                .Replace("</applink>", "</Link>")
                .Replace("appdefaultvalue", "defaultValue")
                .Replace("appformaction", "formAction")


                .Replace("appmainmenulabel=", "mainMenuLabel=")
                .Replace("appmainmenuurl=", "mainMenuUrl=")
                .Replace("appchildmenudata=", "childMenuData=");

                //.Replace("<appdropdownmenu", "<DropdownMenu")
                //.Replace("</appdropdownmenu>", "</DropdownMenu>")
                //.Replace("<appheader", "<Header")
                //.Replace("</appheader>", "</Header>")
                //.Replace("<appfooter", "<Footer")
                //.Replace("</appfooter>", "</Footer>")
                //.Replace("<appfooter", "<Footer")
                //.Replace("<exampleuseappcontext", "<ExampleUseAppContext")
                //.Replace("</exampleuseappcontext>", "</ExampleUseAppContext>")

                //.Replace("<addtoshoppingcartbutton", "<AddToShoppingCartButton")
                //.Replace("</addtoshoppingcartbutton>", "</AddToShoppingCartButton>")
                //.Replace("<loginform", "<LoginForm")
                //.Replace("</loginform>", "</LoginForm>")
                //.Replace("<myaccountbutton", "<MyAccountButton")
                //.Replace("</myaccountbutton>", "</MyAccountButton>")
                //.Replace("<shoppingcartbutton", "<ShoppingCartButton")
                //.Replace("</shoppingcartbutton>", "</ShoppingCartButton>")
                //.Replace("<shoppingcartpopup", "<ShoppingCartPopup")
                //.Replace("</shoppingcartpopup>", "</ShoppingCartPopup>")
                //.Replace("<placeorderpopup", "<PlaceOrderPopup")
                //.Replace("</placeorderpopup>", "</PlaceOrderPopup>")
                //.Replace("<imageviewer", "<ImageViewer")
                //.Replace("</imageviewer>", "</ImageViewer>")
                //.Replace("<distributordropdown", "<DistributorDropdown")
                //.Replace("</distributordropdown>", "</DistributorDropdown>")
                //.Replace("<distributorselectorpopup", "<DistributorSelectorPopup")
                //.Replace("</distributorselectorpopup>", "</DistributorSelectorPopup>")
                //.Replace("<multiitemselector", "<MultiItemSelector")
                //.Replace("</multiitemselector>", "</MultiItemSelector>")
                //.Replace("<sendmesssagebutton", "<SendMesssageButton")
                //.Replace("</sendmesssagebutton>", "</SendMesssageButton>")
                //.Replace("<sendmessagepopup", "<SendMessagePopup")
                //.Replace("</sendmessagepopup>", "</SendMessagePopup>");

            if (siteId.HasValue)
            {
                updatedHtml = ConvertBackComponentTagsFromLowerCaseToOriginal(updatedHtml, siteId.Value);
            }

            dictTempKeyAndOrgValue = null;
            return updatedHtml;
        }




        public static string ConvertJsxLayoutToHtml(string jsxLayout)
        {
            if (string.IsNullOrEmpty(jsxLayout))
            {
                return string.Empty;
            }

            // Replace "className" with "class"
            string htmlLayout = jsxLayout.Replace("className", "class");

            // Regex to match style={{...}} in JSX
            var styleRegex = new Regex(@"style\s*=\s*{{(.*?)}}", RegexOptions.Singleline);

            // Replace the style attributes
            htmlLayout = styleRegex.Replace(htmlLayout, match =>
            {
                string styleContent = match.Groups[1].Value;

                // Convert camelCase to kebab-case (e.g., backgroundColor -> background-color)
                var cssStyle = Regex.Replace(styleContent, @"([a-z])([A-Z])", "$1-$2").ToLower();

                // Remove \" 
                cssStyle = cssStyle.Replace("\"", "");

                // Remove single quotes around property values but preserve them inside functions like rgba()
                cssStyle = Regex.Replace(cssStyle, @":\s*'([^']*)'", ": $1");

                // Replace only the commas that separate style declarations
                cssStyle = Regex.Replace(cssStyle, @",\s*(?=[^()]*(?:\(|$))", "; ");

                return $"style=\"{cssStyle}\"";
            });
            htmlLayout = htmlLayout.Trim();
            if (htmlLayout.StartsWith("<>") && htmlLayout.EndsWith("</>"))
            {
                htmlLayout = htmlLayout.Substring(2, htmlLayout.Length - 5).Trim();
            }

            return htmlLayout;
        }


        private static void ConvertHtmlNodeToJsx(INode node, StringBuilder jsxBuilder, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 2);

            if (node is IElement element)
            {
                string tagName = element.TagName.ToLower();
                if (tagName == "body")
                {
                    tagName = "";
                }

                jsxBuilder.Append($"{indent}<{tagName}");

                foreach (var attribute in element.Attributes)
                {
                    var attributeName = ConvertHtmlNodeToJsx_ConvertAttributeName(attribute.Name);
                    var attributeValue = attribute.Value.Replace("\"", "'");

                    if (attributeName == "style")
                    {
                        attributeValue = ConvertHtmlNodeToJsx_ConvertStyleToJsx(attributeValue);
                        jsxBuilder.Append($" {attributeName}={{{attributeValue}}}");
                    }
                    else if (attributeName == "key")
                    {
                        jsxBuilder.Append($" {attributeName}=\"{attributeValue}\"");
                    }
                    else
                    {
                        if (attributeName == "class")
                        {
                            attributeName = "className";
                        }

                        jsxBuilder.Append($" {attributeName}=\"{attributeValue}\"");
                    }
                }

                if (HtmlSelfClosingTags.Contains(tagName) && !element.HasChildNodes)
                {
                    jsxBuilder.AppendLine(" />");
                }
                else
                {
                    jsxBuilder.AppendLine(">");

                    foreach (var child in element.ChildNodes)
                    {
                        ConvertHtmlNodeToJsx(child, jsxBuilder, indentLevel + 1);
                    }

                    jsxBuilder.AppendLine($"{indent}</{tagName}>");
                }
            }
            else if (node is IText textNode)
            {
                var text = textNode.TextContent.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    jsxBuilder.AppendLine($"{indent}{text}");
                }
            }
        }

        private static string ConvertHtmlNodeToJsx_ConvertAttributeName(string attributeName)
        {
            switch (attributeName)
            {
                case "class":
                    return "className";
                case "for":
                    return "htmlFor";
                default:
                    return attributeName;
            }
        }
        private static string ConvertHtmlNodeToJsx_ConvertStyleToJsx(string style)
        {
            var stylePairs = style.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var jsxStyle = new StringBuilder();

            jsxStyle.Append("{");

            for (int i = 0; i < stylePairs.Length; i++)
            {
                var stylePair = stylePairs[i];
                var keyValue = stylePair.Split(new[] { ':' }, 2);

                if (keyValue.Length == 2)
                {
                    var key = ConvertCssPropertyToJsx(keyValue[0].Trim());
                    var value = keyValue[1].Trim();

                    if (value.StartsWith("url("))
                    {
                        jsxStyle.Append($"{key}: \"{value}\"");
                    }
                    else
                    {
                        jsxStyle.Append($"{key}: '{value}'");
                    }


                    if (i < stylePairs.Length - 1)
                    {
                        jsxStyle.Append(", ");
                    }
                }
            }

            jsxStyle.Append("}");

            return jsxStyle.ToString();
        }


        private static string ConvertCssPropertyToJsx(string cssProperty)
        {
            var parts = cssProperty.Split('-');
            if (parts.Length > 1)
            {
                for (int i = 1; i < parts.Length; i++)
                {
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
                }
            }
            return string.Join(string.Empty, parts);
        }



        public static AppEsiteEntity RetrieveOneNextJsAppEntity(object AppEsiteId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppEsiteEntity AppEsiteEntity = new AppEsiteEntity(int.Parse(AppEsiteId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppEsiteEntity);

                adapter.FetchEntity(AppEsiteEntity, rootPath);
                return AppEsiteEntity;
            }
        }

        private static List<string> GetNextJsPagePathParameters(string fileFullPath)
        {
            var pathParameters = new List<string>();
            var regex = new Regex(@"\[([^\]]+)\]");

            foreach (Match match in regex.Matches(fileFullPath))
            {
                pathParameters.Add(match.Groups[1].Value);
            }

            return pathParameters;
        }

        private static Dictionary<string, string> GetNextJsPageDictSeoSettingKeyAndValue(string fileFullPath)
        {
            Dictionary<string, string> toReturn = new Dictionary<string, string>();

            try
            {
                string originalString = File.ReadAllText(fileFullPath.Replace("pageMarkup.tsx", "page.tsx"));



                PrepareOneSeoKeyAndValue(toReturn, originalString, @"/*** Start of metadata.title ***/", @"/*** End of metadata.title ***/", "metadata.title");
                PrepareOneSeoKeyAndValue(toReturn, originalString, @"/*** Start of metadata.description ***/", @"/*** End of metadata.description ***/", "metadata.description");
                PrepareOneSeoKeyAndValue(toReturn, originalString, @"/*** Start of metadata.keywords ***/", @"/*** End of metadata.keywords ***/", "metadata.keywords");
                PrepareOneSeoKeyAndValue(toReturn, originalString, @"/*** Start of metadata.openGraph.url ***/", @"/*** End of metadata.openGraph.url ***/", "metadata.openGraph.url");

            }
            catch (Exception ex)
            {

            }

            return toReturn;
        }

        private static void PrepareOneSeoKeyAndValue(Dictionary<string, string> toReturn, string originalString, string startToken, string endToken, string variable)
        {
            List<string> extractedValueList = AppEsiteFileBL.ExtractFromBody(originalString, startToken, endToken);
            if (extractedValueList.Count > 0)
            {
                string extractedValue = extractedValueList[0].Trim();
                string variablePrefix = variable + " = `";
                int indexStart = extractedValue.IndexOf(variablePrefix);
                int indexValueStart = indexStart + variablePrefix.Length;
                int indexValueEnd = extractedValue.LastIndexOf("`;");

                if (indexValueStart >= 0 && indexValueEnd >= indexValueStart)
                {
                    string value = extractedValue.Substring(indexValueStart, indexValueEnd - indexValueStart);

                    toReturn.Add(variable, value);
                }
            }
        }



        private static void UpdateNextDataModelPageSeoSetting(AppEsitePagesExDto appEsitePagesDto, string fileNameFullPath)
        {
            if (appEsitePagesDto.DictSeoSettingKeyAndValue != null)
            {
                string needToUpdateFilePath = fileNameFullPath.Replace("pageMarkup.tsx", "page.tsx");

                string page_tsx_orgContent = File.ReadAllText(needToUpdateFilePath);

                string updatedContent = page_tsx_orgContent;

                if (appEsitePagesDto.DictSeoSettingKeyAndValue.ContainsKey("metadata.title"))
                {
                    string updatedValue = @"
  metadata.title = `" + appEsitePagesDto.DictSeoSettingKeyAndValue["metadata.title"] + @"`;
";
                    updatedContent = AppEsiteFileBL.RepalceOneExtractedSection(updatedContent, @"/*** Start of metadata.title ***/", @"/*** End of metadata.title ***/", updatedValue);
                }

                if (appEsitePagesDto.DictSeoSettingKeyAndValue.ContainsKey("metadata.description"))
                {
                    string updatedValue = @"
  metadata.description = `" + appEsitePagesDto.DictSeoSettingKeyAndValue["metadata.description"] + @"`;
";
                    updatedContent = AppEsiteFileBL.RepalceOneExtractedSection(updatedContent, @"/*** Start of metadata.description ***/", @"/*** End of metadata.description ***/", updatedValue);
                }

                if (appEsitePagesDto.DictSeoSettingKeyAndValue.ContainsKey("metadata.keywords"))
                {
                    string updatedValue = @"
  metadata.keywords = `" + appEsitePagesDto.DictSeoSettingKeyAndValue["metadata.keywords"] + @"`;
";
                    updatedContent = AppEsiteFileBL.RepalceOneExtractedSection(updatedContent, @"/*** Start of metadata.keywords ***/", @"/*** End of metadata.keywords ***/", updatedValue);
                }

                if (appEsitePagesDto.DictSeoSettingKeyAndValue.ContainsKey("metadata.openGraph.url"))
                {
                    string updatedValue = @"
metadata.openGraph.url = `" + appEsitePagesDto.DictSeoSettingKeyAndValue["metadata.openGraph.url"] + @"`;
";
                    updatedContent = AppEsiteFileBL.RepalceOneExtractedSection(updatedContent, @"/*** Start of metadata.openGraph.url ***/", @"/*** End of metadata.openGraph.url ***/", updatedValue);
                }

                if (page_tsx_orgContent != updatedContent)
                {
                    try
                    {
                        File.WriteAllText(needToUpdateFilePath, updatedContent);
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }


        private static string GetNextJsPageHtmlLayout(string originalString)
        {

            string htmlLayout = "";

            List<string> htmlLayoutSectionList = AppEsiteFileBL.ExtractFromBody(originalString, @"/* Start of NextJs Page Layout */", @"/* End of NextJs Page Layout */");

            if (htmlLayoutSectionList.Count > 0)
            {
                htmlLayout = htmlLayoutSectionList[0];

                htmlLayout = ConvertJsxLayoutToHtml(htmlLayout);

            }

            return htmlLayout;
        }


        private static string UpdateNextJsPageHtmlLayout(string originalFileContent, string updatedHtmlLayout, int? siteId = null)
        {

            string newFileContent = "";


            List<string> htmlLayoutSectionList = AppEsiteFileBL.ExtractFromBody(originalFileContent, @"/* Start of NextJs Page Layout */", @"/* End of NextJs Page Layout */");

            if (htmlLayoutSectionList.Count > 0)
            {
                string orgHtmlLayout = htmlLayoutSectionList[0];

                updatedHtmlLayout = ConvertHtmlToJsx(updatedHtmlLayout, siteId);

                newFileContent = originalFileContent.Replace(orgHtmlLayout, updatedHtmlLayout);
            }



            return newFileContent;
        }



        private static readonly Dictionary<string, int> _dictNextJsFolderPathAndRunningServerPort = new Dictionary<string, int>();
        private static readonly Dictionary<string, Process> _dictFolderPathAndRunningProcess = new Dictionary<string, Process>();
        //private static Process _longRunningProcess;

        private static int? GetNextJsAppTestServerRunningPort(string nextJsAppFolderPath)
        {
            if (_dictNextJsFolderPathAndRunningServerPort.TryGetValue(nextJsAppFolderPath, out int port))
            {
                if (IsPortActive(port))
                {
                    return port;
                }
                else
                {
                    _dictNextJsFolderPathAndRunningServerPort.Remove(nextJsAppFolderPath);
                }
            }
            return null;
        }

        private static bool IsPortActive(int port)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    client.Connect("localhost", port);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static int FindNextAvailablePort(int startingPort = 3000)
        {
            int port = startingPort;
            while (IsPortActive(port))
            {
                port++;
            }
            return port;
        }

        private static void RegisterNextJsTestServer(string folderPath, int port)
        {
            _dictNextJsFolderPathAndRunningServerPort[folderPath] = port;
        }


        private static string ExecuteSystemProcessCommand(string command, string workingDirectory, bool isLongRunning = false, Action<string> outputCallback = null)
        {
            if (isLongRunning)
            {
                ExecuteLongRunningSystemProcessCommand(command, workingDirectory, outputCallback);
                return "";
            }
            else
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    // Start the process
                    process.Start();

                    // Read both streams asynchronously
                    Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                    Task<string> errorTask = process.StandardError.ReadToEndAsync();

                    // Wait for the process to exit
                    process.WaitForExit();

                    // Await tasks to ensure all output/error is captured
                    string output = outputTask.Result;
                    string error = errorTask.Result;

                    if (process.ExitCode != 0)
                        throw new Exception($"Error: {error}");

                    return output;
                }
            }
        }



        private static void ExecuteLongRunningSystemProcessCommand(string command, string workingDirectory, Action<string> outputCallback)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };



            // Start the long-running process
            var longRunningProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            _dictFolderPathAndRunningProcess[workingDirectory] = longRunningProcess;

            longRunningProcess.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null && outputCallback != null)
                {
                    outputCallback(args.Data);
                }
            };

            longRunningProcess.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null && outputCallback != null)
                {
                    outputCallback($"ERROR: {args.Data}");
                }
            };

            longRunningProcess.Start();

            // Begin asynchronous read
            longRunningProcess.BeginOutputReadLine();
            longRunningProcess.BeginErrorReadLine();
        }

        private static void StopLongRunningSystemProcess(Process longRunningProcess)
        {
            if (longRunningProcess != null && !longRunningProcess.HasExited)
            {
                longRunningProcess.Kill();
                longRunningProcess.Dispose();
                longRunningProcess = null;
            }
        }



        private static string ExtractNextJsTestServerUrl(string npmOutput)
        {
            // Extract the URL (e.g., http://localhost:3000) from the `npm run dev` output
            string url = null;
            foreach (string line in npmOutput.Split('\n'))
            {
                if (line.Contains("localhost"))
                {
                    url = line.Trim();
                    break;
                }
            }

            return url ?? "http://localhost:3000"; // Default fallback
        }

        private static string GetPageNameBeforeDynamicFolders(string path)
        {
            string normalizedPath = Path.GetFullPath(path).Replace("\\", "/");

            var parts = normalizedPath.Split('/');

            for (int i = parts.Length - 1; i >= 0; i--)
            {
                if (!parts[i].StartsWith("[") || !parts[i].EndsWith("]"))
                {
                    return parts[i];
                }
            }

            return "";
        }



        private static string ConvertComponentTagsToLowerCase(string inputHtml, int siteId)
        {
            string siteBasePath = AppEsiteFileBL.GetWebSiteBasePath(siteId);
            // 1. build the path to your components folder
            string componentFolderPath = Path.Combine(siteBasePath, "src", "components");

            if (!Directory.Exists(componentFolderPath))
                return inputHtml;  // nothing to do

            // 2. grab all component filenames (e.g. "Link.tsx", "MyButton.tsx")
            var componentNames = Directory
                .EnumerateFiles(componentFolderPath, "*.tsx", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .Distinct()
                .ToList();

            var outputHtml = inputHtml;

            // 3. for each component name, do a Regex replace
            foreach (var name in componentNames)
            {
                var lower = name.ToLowerInvariant();

                // regex to catch: <ComponentName   or <ComponentName>
                var openTagPattern = $@"<\s*{Regex.Escape(name)}\b";
                var openTagReplace = $"<app{lower}";

                // regex to catch: </ComponentName>
                var closeTagPattern = $@"</\s*{Regex.Escape(name)}\b";
                var closeTagReplace = $"</app{lower}";

                outputHtml = Regex.Replace(outputHtml, openTagPattern, openTagReplace);
                outputHtml = Regex.Replace(outputHtml, closeTagPattern, closeTagReplace);
            }

            return outputHtml;
        }

        private static string ConvertBackComponentTagsFromLowerCaseToOriginal(string inputHtml, int siteId)
        {
            // get the same base path & components folder
            string siteBasePath = AppEsiteFileBL.GetWebSiteBasePath(siteId);
            string componentFolderPath = Path.Combine(siteBasePath, "src", "components");

            if (!Directory.Exists(componentFolderPath))
                return inputHtml;  // nothing to do

            // grab all component names again
            var componentNames = Directory
                .EnumerateFiles(componentFolderPath, "*.tsx", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .Distinct()
                .ToList();

            var outputHtml = inputHtml;

            // for each component, undo the lowercase/app prefix
            foreach (var name in componentNames)
            {
                var lower = name.ToLowerInvariant();

                // opening tag: <appcomponentname  â†’  <ComponentName
                var openTagPattern = $@"<\s*app{Regex.Escape(lower)}\b";
                var openTagReplace = $"<{name}";

                // closing tag: </appcomponentname>  â†’  </ComponentName>
                var closeTagPattern = $@"</\s*app{Regex.Escape(lower)}\b";
                var closeTagReplace = $"</{name}";

                outputHtml = Regex.Replace(outputHtml, openTagPattern, openTagReplace);
                outputHtml = Regex.Replace(outputHtml, closeTagPattern, closeTagReplace);
            }

            return outputHtml;
        }



        private static readonly string _newTsxPageTemplate = @"
import PageMarkup from './pageMarkup';  
import { createDataService } from '@/services/dataservice';
import { callMgtGetApiByCode, callMgtPostApiByCode } from '@/services/mgtdataservice';
import appHelper from '@/helper/apphelper';
import { Metadata } from 'next';
import { redirect } from 'next/navigation';
import { headers } from 'next/headers';

export const metadata: Metadata = {
  title: """",
  description: """",
  keywords: """",
  robots: ""index, follow"",
  openGraph: {
    title: """",
    description: """",
    url: """",
    images: [],
  }
};

const NewPageName = async({ params, searchParams }: any) => {

  const dataService = createDataService();
  const headersList = headers();    

  const dataModel: { [key: string]: any } = appHelper.initializePageDataModel('NewPageName', params, searchParams, headersList);

  /* Start of Mgt Get Api Call */
    
  /* End of Mgt Get Api Call */



  /* Start of Mgt Post Api Call */

  /* End of Mgt Post Api Call */


  /* Metadata Setting */

  /*** Start of metadata.title ***/
  metadata.title = `NewPageName`;
  /*** End of metadata.title ***/

  /*** Start of metadata.description ***/
  metadata.description = `NewPageName`;
  /*** End of metadata.description ***/

  /*** Start of metadata.keywords ***/
  metadata.keywords = `NewPageName`;
  /*** End of metadata.keywords ***/

  if (process.env.NEXT_PUBLIC_SITE_DEFAULT_TITLE) {
    metadata.title = metadata.title + ' - ' + process.env.NEXT_PUBLIC_SITE_DEFAULT_TITLE;
  }
  
  if (process.env.NEXT_PUBLIC_SITE_DEFAULT_TITLE) {
    metadata.description = metadata.description + '. ' + process.env.NEXT_PUBLIC_SITE_DEFAULT_DESCRIPTION;
  }

  metadata.openGraph = {
    title: metadata.title,
    description: metadata.description,   
    images: [],
  };  
  
  /*** Start of metadata.openGraph.url ***/
  metadata.openGraph.url = ``;
  /*** End of metadata.openGraph.url ***/


  return (
    <PageMarkup dataModel={dataModel} />
  );
}

export default NewPageName;

    ";

        private static readonly string _newTsxPageLayoutTemplate = @"
import Link from ""next/link"";
import Image from ""next/image"";
import appHelper from ""@/helper/apphelper"";

export default function PageMarkup({ dataModel }: { dataModel: any }) { 
    return (
    /* Start of NextJs Page Layout */
        <>
            <div className='p-5'>
                {dataModel.pageName}
            </div>
        </>
    /* End of NextJs Page Layout */
    );
};

    ";



        private static readonly string _newTsxComponentTemplate = @"
export const NewComponentName = () => {
    return (
        <div className="">
            NewComponentName
        </div>
    )
}
    ";


    }
}