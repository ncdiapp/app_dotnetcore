using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;

#if NETFRAMEWORK
using LibSassHost;
// TODO-PHASE4: Replace with .NET 10 equivalent
#endif
using SD.LLBLGen.Pro.ORMSupportClasses;


using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Twilio.TwiML.Voice;
using AngleSharp;
using AngleSharp.Html.Parser;
using System.Windows.Forms;
using AngleSharp.Html.Dom;
using AngleSharp.Dom;

using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;

using APP.Framework;
namespace App.BL
{
    public static class AppEsiteFileBL
    {
        //Get Folder hairachy by root folder: id foder path: descirption: file name
        //Show File List for each forlder
        //upload one file
        // Delete File
        // Save alue
        //zip all file and Folder with root foder
        // extract all zip to destnation foler
        // publih web site as zip package
        //Security ...
        // need to craete sysdefine website templaet mgt
        // name:, Description,root repository link, preview website on thlie, export as clinet web site,link to managment web ite . ispublished

        public static readonly string prefix_DesignWidthRange = ".Design_WidthRange_";
        public static readonly string cssClass_CtnCompanySitePage = ".Ctn-CompanySitePage";
        public static readonly string anonymousToken = "6601508d-e7e0-4ed6-892b-879c834676af";

        public static readonly int CompanyDefaultSiteId = 1;
        public static readonly string PublicFolderName = "Public";
        public static readonly string CustomerFolderName = "Customer";
        public static readonly string SupplierFolderName = "Supplier";

        public static void TestAngleSharp()
        {
            // Sample HTML string
            string html = "<div><h1>Hello, AngleSharp!</h1><p>This is a sample HTML string.</p></div>";

            //Create an HTML parser instance
            var parser = new HtmlParser();

            // Parse the HTML string
            var document = parser.ParseDocument(html);

            // Access elements using CSS selectors
            var h1Element = document.QuerySelector("h1");
            var pElement = document.QuerySelector("p");

            // Get the inner text of the elements
            string h1Text = h1Element.TextContent;
            string pText = pElement.TextContent;

            // Output the results
            Console.WriteLine($"h1 element text: {h1Text}");
            Console.WriteLine($"p element text: {pText}");

            h1Element.ClassName = "test-white bg-red-500";

        }

        public static AppSefolderDto RetrieveAppEsiteLocalFolderHairarchyDto(int? eSiteId, string subFolderPath = "", int? subsiteType = null)
        {
            if (eSiteId.HasValue)
            {
                //I:\DevTest\App\PlmApplication\FileRepository\Company_4007\WebSite\Site_5\
                //"I:\DevTest\App\PlmApplication\FileRepository\Company_4007\WebSite\Site_5\" + subFolderPath + "\"

                // var siteEntity = AppEsiteBL.re

                string webSiteBaseFolderPath = GetWebSiteBasePath(eSiteId);



                if (!string.IsNullOrWhiteSpace(subFolderPath))
                {
                    webSiteBaseFolderPath += subFolderPath;
                }
                else if (subsiteType.HasValue)
                {
                    if (subsiteType.Value == (int)EmAppEsitePageCategory.PublicPage)
                    {
                        webSiteBaseFolderPath += PublicFolderName + "\\Dev";
                    }
                    else if (subsiteType.Value == (int)EmAppEsitePageCategory.SupplierPage)
                    {
                        webSiteBaseFolderPath += SupplierFolderName + "\\Dev";
                    }
                    else if (subsiteType.Value == (int)EmAppEsitePageCategory.ClientPage)
                    {
                        webSiteBaseFolderPath += CustomerFolderName + "\\Dev";
                    }
                }

                List<string> ignoredFolderPathList = new List<string>();
                ignoredFolderPathList.Add("\\.next");
                ignoredFolderPathList.Add("\\.vscode");
                ignoredFolderPathList.Add("\\node_modules");
                ignoredFolderPathList.Add("\\SharedResource");



                AppSefolderDto rootAppSefolderDto = AppEsiteFileBL.RetrieveLocalFolderHairarchyDtoByFolderPath(webSiteBaseFolderPath, ignoredFolderPathList);


                DirectoryInfo parentDir = Directory.GetParent(webSiteBaseFolderPath);
                string websiteRelatPathBase = Directory.GetParent(Directory.GetParent(webSiteBaseFolderPath).FullName).FullName;



                rootAppSefolderDto.EsiteId = eSiteId;
                rootAppSefolderDto.RelativePathDisplay = rootAppSefolderDto.FolderPath.Replace(websiteRelatPathBase, "").Replace("\\", "/");


                UpdateChildSiteId(websiteRelatPathBase, rootAppSefolderDto);

                //if (subsiteType.HasValue)
                //{
                //    PrepareSubsiteFolders(subsiteType, rootAppSefolderDto);
                //}


                return rootAppSefolderDto;
            }

            return null;
        }



        //private static void PrepareSubsiteFolders(int? subsiteType, AppSefolderDto rootAppSefolderDto)
        //{
        //    AppSefolderDto pagesFolder = null;

        //    if (rootAppSefolderDto.Name.ToLower() == "pages")
        //    {
        //        pagesFolder = rootAppSefolderDto;
        //    }
        //    else
        //    {
        //        pagesFolder = rootAppSefolderDto.Children.FirstOrDefault(o => o.Name.ToLower() == "pages");
        //    }


        //    if (pagesFolder != null)
        //    {

        //        RemoveSubFolderByName(pagesFolder, "BuiltIn");

        //        if (subsiteType.Value == (int)EmAppEsitePageCategory.PublicPage)
        //        {
        //            RemoveSubFolderByName(pagesFolder, "Employee");
        //            RemoveSubFolderByName(pagesFolder, "Customer");
        //            RemoveSubFolderByName(pagesFolder, "Supplier");
        //        }
        //        else if (subsiteType.Value == (int)EmAppEsitePageCategory.SupplierPage)
        //        {
        //            RemoveSubFolderByName(pagesFolder, "Static");
        //            RemoveSubFolderByName(pagesFolder, "Employee");
        //            RemoveSubFolderByName(pagesFolder, "Customer");
        //        }
        //        else if (subsiteType.Value == (int)EmAppEsitePageCategory.ClientPage)
        //        {
        //            RemoveSubFolderByName(pagesFolder, "Static");
        //            RemoveSubFolderByName(pagesFolder, "Employee");
        //            RemoveSubFolderByName(pagesFolder, "Supplier");
        //        }
        //    }
        //}

        private static void RemoveSubFolderByName(AppSefolderDto parentFolder, string needToRemoveSubFolderName)
        {
            if (parentFolder != null && !parentFolder.Children.IsEmpty() && needToRemoveSubFolderName.HasValue())
            {
                var needToRemoveSubFolder = parentFolder.Children.FirstOrDefault(o => o.Name.ToLower() == needToRemoveSubFolderName.ToLower());
                if (needToRemoveSubFolder != null)
                {
                    var childrenFolderList = parentFolder.Children.ToList();
                    childrenFolderList.Remove(needToRemoveSubFolder);
                    parentFolder.Children = childrenFolderList.ToArray();
                }
            }
        }

        private static void UpdateChildSiteId(string websiteRelatPathBase, AppSefolderDto rootAppSefolderDto)
        {
            foreach (var child in rootAppSefolderDto.Children)
            {
                child.RelativePathDisplay = child.FolderPath.Replace(websiteRelatPathBase, "").Replace("\\", "/");
                child.EsiteId = rootAppSefolderDto.EsiteId;
                if (!child.Children.IsEmpty())
                {
                    UpdateChildSiteId(websiteRelatPathBase, child);
                }
            }
        }

        public static string GetWebSiteBasePath(int? eSiteId)
        {
            string WebSiteBaseFolderPath;
            if (eSiteId.Value >= AppEsiteConfigBL.WebSiteTemplateSiteIdStartFrom)
            {
                WebSiteBaseFolderPath = GetWebSiteTemplateBaseFolderPath(eSiteId.Value);
            }
            else
            {
                WebSiteBaseFolderPath = GetCompanyWebSiteBaseFolderPath(eSiteId.Value);
            }

            return WebSiteBaseFolderPath;
        }



        public static AppSefolderDto ImportWebSiteTemplateToApplicationSite(int? tempalteSiteId, int? appSiteId, string requestHostServerPath)
        {
            if (tempalteSiteId.HasValue && appSiteId.HasValue)
            {

                string templatePath = GetWebSiteBasePath(tempalteSiteId);
                string appSitePath = GetWebSiteBasePath(appSiteId);




                CopyFolderToNewLocation(templatePath, appSitePath);

                // need to copy WebSite Defautobject as well




            }

            AppEsiteEntity appEsiteEntity = AppEsiteConfigBL.RetrieveOneAppEsiteEntity(appSiteId);

            UpdateHostSiteMainVariablesJsPage(appEsiteEntity, requestHostServerPath);

            // Update AppWebsite
            AppEsiteExDto appEsiteExDto = AppEsiteConfigBL.RetrieveOneAppEsiteExDto(tempalteSiteId);
            appEsiteExDto.Name = appEsiteEntity.Name;
            appEsiteExDto.Description = appEsiteEntity.Description;
            appEsiteExDto.EmAppEsiteTheme = appEsiteEntity.EmAppEsiteTheme;
            appEsiteExDto.StartPage = appEsiteEntity.StartPage;

            if (appEsiteExDto.EsiteAttribute == null)
            {
                appEsiteExDto.EsiteAttribute = new EsiteAttributeDto();
            }

            appEsiteExDto.EsiteAttribute.TemplateCode = "Default";

            appEsiteExDto.Id = appSiteId.Value;
            appEsiteExDto.IsModified = true;



            appEsiteExDto.AppEsitePagesList.ForAll(o =>
            {
                if (appEsiteEntity.AppEsitePages.FirstOrDefault(p => p.MetaDesciption == o.MetaDesciption) == null)
                {
                    o.Id = null;
                }
            });

            AppEsiteConfigBL.SaveAppEsiteExDto(appEsiteExDto);
            ImportMenuTreeFromSiteTemplate(tempalteSiteId, appSiteId);

            return AppEsiteFileBL.RetrieveAppEsiteLocalFolderHairarchyDto(appSiteId);
        }


        public static void UpdateHostSiteMainVariablesJsPage(AppEsiteEntity esiteEntity, string requestHostServerPath, string appSiteBaseUrl = "", string siteMgtBaseUrl = "")
        {
            int EsiteId = esiteEntity.EsiteId;
            string variablesJsFilePath = "SharedResource/js/SiteMainVariables.js";
            if (!string.IsNullOrWhiteSpace(esiteEntity.StartPage))
            {
                //variablesJsFilePath = esiteEntity.StartPage;
            }
            string webSiteStartPath = AppEsiteFileBL.GetWebSiteBasePath(EsiteId);
            string variablesJsFileFullPath = Path.Combine(webSiteStartPath, variablesJsFilePath);
            // var applicationSiteId = 4; //NeedToRepalce
            // var domainAndApplicationpath = 'http://192.168.1.70/AppBuilder';//NeedToRepalce
            if (File.Exists(variablesJsFileFullPath))
            {

                string originalString = File.ReadAllText(variablesJsFileFullPath);
                //@"(?i)SELECT\s+(.+?)\s+FROM"
                //"(?<=http://).*?(?=\.png)"

                List<string> RepalceSectionList = ExtractFromBody(originalString, "/**BeginNeedToRepalce****/", @"/**EndNeedToRepalce****/");
                if (RepalceSectionList.Count > 0)
                {
                    string firstSection = RepalceSectionList[0];
                    string[] lineList = firstSection.Split(";".ToArray());
                    foreach (string line in lineList)
                    {
                        if (line.Contains("const applicationSiteId"))
                        {
                            originalString = originalString.Replace(line, System.Environment.NewLine + "const applicationSiteId = " + EsiteId);
                        }
                        else if (line.Contains("const siteMgtVirtualPath"))
                        {
                            if (requestHostServerPath != null)
                            {
                                string tempPath = requestHostServerPath;
                                if (tempPath.EndsWith("/"))
                                {
                                    tempPath = tempPath.Substring(0, tempPath.Length - 1);
                                }

                                int virtualPath_StartIndex = tempPath.LastIndexOf("/");

                                if (virtualPath_StartIndex >= 0)
                                {
                                    tempPath = tempPath.Substring(virtualPath_StartIndex) + "/";
                                    originalString = originalString.Replace(line, System.Environment.NewLine + string.Format("const siteMgtVirtualPath = '{0}'", tempPath));
                                }
                            }
                        }
                        else if (line.Contains("const siteMgtBaseUrl"))
                        {
                            //if (!string.IsNullOrWhiteSpace(siteMgtBaseUrl))
                            //{
                            //    originalString = originalString.Replace(line, System.Environment.NewLine + string.Format("const siteMgtBaseUrl = '{0}'", siteMgtBaseUrl));
                            //}
                        }
                        else if (line.Contains("const appSiteBaseUrl"))
                        {
                            //if (!string.IsNullOrWhiteSpace(appSiteBaseUrl))
                            //{
                            //    originalString = originalString.Replace(line, System.Environment.NewLine + string.Format("const appSiteBaseUrl = '{0}'", appSiteBaseUrl));
                            //}
                        }
                        //else if (line.Contains("staticSiteBaseUrl"))
                        //{
                        //    originalString = originalString.Replace(line, System.Environment.NewLine + string.Format("const staticSiteBaseUrl = '{0}'", requestHostServerPath));
                        //}
                        else if (line.Contains("const applicationToken"))
                        {
                            // need to site debugId
                            string applicationToken = "6601508d-e7e0-4ed6-892b-879c834676af";
                            applicationToken = "";
                            originalString = originalString.Replace(line, System.Environment.NewLine + string.Format("const applicationToken = '{0}'", applicationToken));
                        }
                        //else if (line.Contains("applicationType"))
                        //{
                        //    if (esiteEntity.EmApplicationType.HasValue && esiteEntity.EmApplicationType.Value == (int)EmAppESiteApplicationType.ECommerce)
                        //    {
                        //        originalString = originalString.Replace(line, System.Environment.NewLine + string.Format("const applicationType = '{0}'", EmAppESiteApplicationType.ECommerce.ToString()));
                        //    }
                        //    else
                        //    {
                        //        originalString = originalString.Replace(line, System.Environment.NewLine + string.Format("const applicationType = '{0}'", EmAppESiteApplicationType.NonECommerce.ToString()));
                        //    }


                        //}

                    }

                }

                // List<string> RepalceTitleSectionList = ExtractFromBody(originalString, "<title>", @"/**EndNeedToRepalce****/");


                File.WriteAllText(variablesJsFileFullPath, originalString);

            }


        }

        public static void UpdateUserCompanyDefaultSiteMainVariablesJsPage(string companyId, string applicationCode)
        {
            int EsiteId = 1;
            string variablesJsFilePath = "SharedResource/js/SiteMainVariables.js";

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string webSiteStartPath = string.Format(@"{0}FileRepository\Company_{1}\WebSite\Site_1\", baseDirectory, companyId);
            string variablesJsFileFullPath = Path.Combine(webSiteStartPath, variablesJsFilePath);
            string requestHostServerPath = "/MGT/";
            string appSiteBaseUrl = "";
            string siteMgtBaseUrl = "";

            string applicationUrl = AppSystemSettingBL.GetStringValue(EmSystemSettings.ApplicationURL);

            if (!string.IsNullOrWhiteSpace(applicationUrl) && !string.IsNullOrWhiteSpace(applicationCode))
            {
                if (!applicationUrl.EndsWith("/"))
                {
                    applicationUrl += "/";
                }

                string currentAppDomain = ServerContext.Instance.ApplicationDomain;

                if (!string.IsNullOrWhiteSpace(currentAppDomain) && applicationUrl.ToLower().EndsWith(currentAppDomain.ToLower() + "/"))
                {
                    appSiteBaseUrl = applicationUrl.ToLower().Replace(currentAppDomain.ToLower(), applicationCode);
                    siteMgtBaseUrl = appSiteBaseUrl + "MGT/";
                }
            }




            if (File.Exists(variablesJsFileFullPath))
            {

                string originalString = File.ReadAllText(variablesJsFileFullPath);


                List<string> RepalceSectionList = ExtractFromBody(originalString, "/**BeginNeedToRepalce****/", @"/**EndNeedToRepalce****/");
                if (RepalceSectionList.Count > 0)
                {
                    string firstSection = RepalceSectionList[0];
                    string[] lineList = firstSection.Split(";".ToArray());
                    foreach (string line in lineList)
                    {
                        if (line.Contains("const applicationSiteId"))
                        {
                            originalString = originalString.Replace(line, System.Environment.NewLine + "const applicationSiteId = " + EsiteId);
                        }
                        else if (line.Contains("const siteMgtVirtualPath"))
                        {
                            if (requestHostServerPath != null)
                            {
                                string tempPath = requestHostServerPath;
                                if (tempPath.EndsWith("/"))
                                {
                                    tempPath = tempPath.Substring(0, tempPath.Length - 1);
                                }

                                int virtualPath_StartIndex = tempPath.LastIndexOf("/");

                                if (virtualPath_StartIndex >= 0)
                                {
                                    tempPath = tempPath.Substring(virtualPath_StartIndex) + "/";
                                    originalString = originalString.Replace(line, System.Environment.NewLine + string.Format("const siteMgtVirtualPath = '{0}'", tempPath));
                                }


                            }
                        }
                        else if (line.Contains("const siteMgtBaseUrl"))
                        {
                            //if (!string.IsNullOrWhiteSpace(siteMgtBaseUrl))
                            //{
                            //    originalString = originalString.Replace(line, System.Environment.NewLine + string.Format("const siteMgtBaseUrl = '{0}'", siteMgtBaseUrl));
                            //}
                        }
                        else if (line.Contains("const appSiteBaseUrl"))
                        {
                            //if (!string.IsNullOrWhiteSpace(appSiteBaseUrl))
                            //{
                            //    originalString = originalString.Replace(line, System.Environment.NewLine + string.Format("const appSiteBaseUrl = '{0}'", appSiteBaseUrl));
                            //}
                        }
                        //else if (line.Contains("var domainAndApplicationpath"))
                        //{
                        //    originalString = originalString.Replace(line, System.Environment.NewLine + "var domainAndApplicationpath = siteApplicationpath + siteMgtVirtualPath;");
                        //}                        
                        else if (line.Contains("const applicationToken"))
                        {
                            // need to site debugId
                            string applicationToken = "6601508d-e7e0-4ed6-892b-879c834676af";
                            applicationToken = "";
                            originalString = originalString.Replace(line, System.Environment.NewLine + string.Format("const applicationToken = '{0}'", applicationToken));
                        }

                    }

                }

                //List<string> RepalceTitleSectionList = ExtractFromBody(originalString, "<title>", @"/**EndNeedToRepalce****/");


                File.WriteAllText(variablesJsFileFullPath, originalString);

            }


        }
        public static string RepalceOneExtractedSection(string originalContent, string sectionBeginToken, string sectionEndToken, string updatedSectionContent)
        {
            string updatedContent = originalContent;

            List<string> repalceSectionList = AppEsiteFileBL.ExtractFromBody(originalContent, sectionBeginToken, sectionEndToken);

            if (repalceSectionList.Count > 0)
            {
                string needToUpdateSectionContent = repalceSectionList[0];
                int firstIdx = originalContent.IndexOf(needToUpdateSectionContent, StringComparison.Ordinal);
                updatedContent = firstIdx >= 0
                    ? originalContent.Substring(0, firstIdx) + updatedSectionContent + originalContent.Substring(firstIdx + needToUpdateSectionContent.Length)
                    : originalContent;
            }

            return updatedContent;
        }

        public static List<string> ExtractFromBody(string body, string start, string end)
        {
            List<string> matched = new List<string>();

            int indexStart = 0;
            int indexEnd = 0;

            bool exit = false;
            while (!exit)
            {
                indexStart = body.IndexOf(start);

                if (indexStart != -1)
                {
                    indexEnd = indexStart + start.Length + body.Substring(indexStart + start.Length).IndexOf(end);

                    matched.Add(body.Substring(indexStart + start.Length, indexEnd - indexStart - start.Length));

                    body = body.Substring(indexEnd + end.Length);
                }
                else
                {
                    exit = true;
                }
            }

            return matched;
        }




        public static bool SynchronizeWebSiteRoutstatejs(int? eSiteId)
        {
            string beginToken = @"/**BeginUserDefineRouteSection**/";
            string endToken = @"/**EndUserDefineRouteSection**/";

            string appSitePath = GetWebSiteBasePath(eSiteId);
            string routestateFullath = Path.Combine(appSitePath, @"SharedResource\js\RouteState.js");

            string originalString = File.ReadAllText(routestateFullath);
            List<string> repalceSectionList = ExtractFromBody(originalString, beginToken, endToken);

            try
            {




                var pages = AppEsiteConfigBL.RetrieveRunningRouteStateJavascript(eSiteId);

                string allRoute = "";


                foreach (var page in pages)
                {
                    if (!string.IsNullOrWhiteSpace(page.FileFullPath))
                    {
                        if (page.FileFullPath.ToLower().Contains("SharedResource"))
                        {
                            continue;
                        }

                        if (page.FileFullPath.ToLower().Contains("Dev\\StaticPages\\"))
                        {
                            continue;
                        }

                        if (!page.FileFullPath.ToLower().EndsWith(".html"))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(page.Title))
                    {

                        if (page.Title.ToLower() == "publichomepage.html"
                            || page.Title.ToLower() == "homepage.html"
                            || page.Title.ToLower() == "index.html"
                            || page.Title.ToLower() == "clientsignup.html"
                            || page.Title.ToLower() == "suppliersignup.html"
                            || page.Title.ToLower() == "signin.html"
                            || page.Title.ToLower() == "forgotpassword.html"
                            || page.Title.StartsWith("_")
                            )
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }

                    string pageRelativepath = page.FileFullPath.Replace(appSitePath, "/");
                    pageRelativepath = pageRelativepath.Replace("\\", "/");

                    if (pageRelativepath.IndexOf("Public/Dev/DynamicFormPages/", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        pageRelativepath = Regex.Replace(pageRelativepath, "Public/Dev/DynamicFormPages/", "publicPageSubPath + '", RegexOptions.IgnoreCase) + "'";
                    }
                    else if (pageRelativepath.IndexOf("Customer/Dev/DynamicFormPages/", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        pageRelativepath = Regex.Replace(pageRelativepath, "Customer/Dev/DynamicFormPages/", "'/Customer/' + pageSubPath + '", RegexOptions.IgnoreCase) + "'";
                    }
                    else if (pageRelativepath.IndexOf("Supplier/Dev/DynamicFormPages/", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        pageRelativepath = Regex.Replace(pageRelativepath, "Supplier/Dev/DynamicFormPages/", "'/Supplier/' + pageSubPath + '", RegexOptions.IgnoreCase) + "'";
                    }
                    else if (pageRelativepath.IndexOf("Employee/Dev/DynamicFormPages/", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        pageRelativepath = Regex.Replace(pageRelativepath, "Employee/Dev/DynamicFormPages/", "'/Employee/' + pageSubPath + '", RegexOptions.IgnoreCase) + "'";
                    }
                    else
                    {
                        pageRelativepath = "'" + pageRelativepath + "'";
                    }

                    if (string.IsNullOrWhiteSpace(page.UrlAndHandle))
                    {
                        page.UrlAndHandle = page.MetaDesciption;
                    }

                    string oneRoute = $"    .state('main.{page.MetaDesciption}', {{ url: '/{page.UrlAndHandle}', controller: '{page.ControllerName}', templateUrl:siteApplicationpath + {pageRelativepath}}}) ";

                    // only need to keep on state MetaDesciption
                    if (allRoute.IndexOf($".state('main.{page.MetaDesciption}'") == -1)
                    {
                        allRoute = allRoute + System.Environment.NewLine + oneRoute;

                    }



                }

                allRoute = allRoute + System.Environment.NewLine;


                if (repalceSectionList.Count > 0)
                {
                    string useDefineRouteSection = repalceSectionList[0];
                    // not exist , need to insert !
                    if (string.IsNullOrWhiteSpace(useDefineRouteSection))
                    {
                        int indexOfEndToken = originalString.IndexOf(endToken);

                        originalString = originalString.Insert(indexOfEndToken, allRoute);
                    }
                    else // need to update 
                    {
                        originalString = originalString.Replace(useDefineRouteSection, allRoute);

                    }


                }


                File.WriteAllText(routestateFullath, originalString);


                return true;

            }
            catch (Exception ex)
            {
                return false;
            }




            //return AppEsiteFileBL.RetrieveAppEsiteLocalFolderHairarchyDto(tempalteSiteId);
        }

        public static List<FileInfo> GetDirectoryFileList(DirectoryInfo directoryInfo, bool includeFilesInSubfolders = false)
        {
            try
            {
                List<FileInfo> files = directoryInfo.GetFiles().OrderBy(o => o.CreationTime).ToList(); ;

                if (includeFilesInSubfolders)
                {
                    foreach (DirectoryInfo subdirectory in directoryInfo.GetDirectories())
                    {
                        files.AddRange(GetDirectoryFileList(subdirectory, true));
                    }
                }

                return files;
            }
            catch (Exception ex)
            {
                return new List<FileInfo>();
            }
        }


        public static List<AppEsitePagesDto> RetrieveAppEsiteAllFileInfoDtos(int? eSiteId, string filterByExtention)
        {
            List<AppEsitePagesDto> appFileDtoList = new List<AppEsitePagesDto>();

            string appSitePath = GetWebSiteBasePath(eSiteId);

            string filter = "*.*";

            if (!string.IsNullOrWhiteSpace(filterByExtention))
            {
                filter = "*." + filterByExtention.Trim().ToLower();
            }

            Dictionary<string, AppEsitePagesDto> dictFullNamePgeDto = GetWebSiteAllPageExtenstionPage(eSiteId);

            foreach (string orgFilePath in Directory.GetFiles(appSitePath, filter, SearchOption.AllDirectories))
            {
                FileInfo fileInfo = new FileInfo(orgFilePath);



                AppEsitePagesDto pageDto = new AppEsitePagesDto();
                if (dictFullNamePgeDto.ContainsKey(fileInfo.FullName))
                {
                    pageDto = dictFullNamePgeDto[fileInfo.FullName];
                }

                AddFileAttributesToAppEsitePagesDto(fileInfo, pageDto);
                if (pageDto.FileFullPath.HasValue())
                {
                    pageDto.Description = pageDto.FileFullPath.Substring(appSitePath.Length).Replace("\\", "/");

                }


                appFileDtoList.Add(pageDto);
            }

            return appFileDtoList;
        }

        public static OperationCallResult<bool> ExportEsiteToStaticSite(int siteId, string requestHostServerPath)
        {
            OperationCallResult<bool> toReturn = new OperationCallResult<bool>();
            ValidationResult validationResult = new ValidationResult();
            toReturn.ValidationResult = validationResult;

            AppEsiteExDto appEsiteExDto = AppEsiteConfigBL.RetrieveOneAppEsiteExDto(siteId);

            if (!string.IsNullOrWhiteSpace(requestHostServerPath))
            {
                if (requestHostServerPath.ToLower().EndsWith("/mgt/"))
                {
                    appEsiteExDto.SitePublishedBaseUrl = requestHostServerPath.Substring(0, requestHostServerPath.Length - 4);
                    appEsiteExDto.EsiteAttribute.MgtSiteBaseUrl = requestHostServerPath;

                    var saveEsiteResult = AppEsiteConfigBL.SaveAppEsiteExDto(appEsiteExDto);
                    if (saveEsiteResult.IsSuccessfulWithResult)
                    {
                        appEsiteExDto = saveEsiteResult.Object;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(appEsiteExDto.SitePublishedBaseUrl))
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_SitePublishedBaseUrl_IsEmpty.", ValidationItemType.Error, "Publish cannot be processed. Cannot find site base url."));

                toReturn.Object = false;

                return toReturn;
            }


            string appSitePath = GetWebSiteBasePath(siteId);


            List<string> subSiteNames = new List<string>() { PublicFolderName, CustomerFolderName, SupplierFolderName };

            foreach (string subsiteName in subSiteNames)
            {

                try
                {
                    string targetFolderPath_Release = string.Format("{0}{1}\\", appSitePath, subsiteName);

                    if (siteId == CompanyDefaultSiteId && subsiteName == PublicFolderName)
                    {
                        targetFolderPath_Release = appSitePath;
                    }
                    else
                    {
                        //ClearOneFileFolderByPath(targetFolderPath_Release);
                    }


                    string staticPageFolderPath = string.Format("{0}{1}\\Dev\\StaticPages\\", appSitePath, subsiteName);

                    List<AppEsitePagesDto> staticPageDtoList = RetrieveLocalFileInfoDtosByFolderPath(staticPageFolderPath, siteId);

                    foreach (AppEsitePagesDto pageDto in staticPageDtoList)
                    {
                        if (!pageDto.IsDataModelPage)
                        {
                            pageDto.EsiteId = siteId;
                            var aResult = ExportOneEsitePageToStaticPage(pageDto, null, null, null, requestHostServerPath);
                            if (aResult.Object != true)
                            {
                                validationResult.Merge(aResult.ValidationResult);
                            }
                        }
                    }

                    string srcFolderPath_DynamicPages = string.Format("{0}{1}\\Dev\\DynamicFormPages\\", appSitePath, subsiteName);



                    CopyFolderToNewLocation(srcFolderPath_DynamicPages, targetFolderPath_Release);


                }
                catch (Exception ex)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_SaveAppEsitePagesExDtoWithFile_Error", ValidationItemType.Error, ex.ToString()));

                    toReturn.Object = false;
                }
            }



            if (!validationResult.HasErrors)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_Save_OK", ValidationItemType.Message, "Publish Successful"));
                toReturn.Object = true;
            }

            return toReturn;
        }

        public static OperationCallResult<bool> GenerateComponentJsFileRerence(AppEsiteUserDefinedJsFunctionDto functionDto)
        {
            OperationCallResult<bool> toReturn = new OperationCallResult<bool>();
            ValidationResult validationResult = new ValidationResult();
            toReturn.ValidationResult = validationResult;

            int esiteId = functionDto.EsiteId.Value;


            if (functionDto.FunctionName.HasValue() && functionDto.InitalExpression.HasValue())
            {
                string jsFileName = functionDto.FunctionName.Trim();
                string initExpression = functionDto.InitalExpression.Trim();


                string webSiteStartPath = AppEsiteFileBL.GetWebSiteBasePath(esiteId);
                string indexPageFilePath = "index.html";
                string navCtrlJsFilePath = "SharedResource/js/appWebsiteGenericNavigationCtrl.js";

                string indexPageFileFullPath = Path.Combine(webSiteStartPath, indexPageFilePath);
                string navCtrlJsFileFullPath = Path.Combine(webSiteStartPath, navCtrlJsFilePath);

                try
                {
                    if (File.Exists(indexPageFileFullPath))
                    {
                        string needToAddJsReferenceCode = string.Format("<script src=\"SharedResource/Component/js/{0}\"></script>", jsFileName);
                        string originalString = File.ReadAllText(indexPageFileFullPath);

                        List<string> repalceSectionList = ExtractFromBody(originalString, @"<!-- Begin of Component Js -->", @"<!-- End of Component Js -->");
                        if (repalceSectionList.Count > 0)
                        {
                            string firstSection = repalceSectionList[0];

                            if (!firstSection.Contains(needToAddJsReferenceCode))
                            {

                                originalString = originalString.Replace(@"<!-- End of Component Js -->", needToAddJsReferenceCode + System.Environment.NewLine + @"<!-- End of Component Js -->");
                            }
                        }

                        File.WriteAllText(indexPageFileFullPath, originalString);
                    }

                    if (File.Exists(navCtrlJsFileFullPath))
                    {

                        string originalString = File.ReadAllText(navCtrlJsFileFullPath);

                        List<string> repalceSectionList = ExtractFromBody(originalString, @"/*** Begin of Component Js Init ***/", @"/*** End of Component Js Init ***/");
                        if (repalceSectionList.Count > 0)
                        {
                            string firstSection = repalceSectionList[0];

                            if (!firstSection.Contains(initExpression))
                            {
                                originalString = originalString.Replace(@"/*** End of Component Js Init ***/", initExpression + System.Environment.NewLine + @"/*** End of Component Js Init ***/");
                            }
                        }

                        File.WriteAllText(navCtrlJsFileFullPath, originalString);

                    }

                    toReturn.Object = true;
                }
                catch (Exception ex)
                {

                }
            }

            return toReturn;
        }

        public static OperationCallResult<bool> ExportOneEsitePageToStaticPage(AppEsitePagesDto srcPageDto, string distSubFolderPath = "", string distFileName = "", string newHtmlContent = "", string requestHostServerPath = "", bool needToPreSaveEsite = false)
        {
            OperationCallResult<bool> toReturn = new OperationCallResult<bool>();
            ValidationResult validationResult = new ValidationResult();
            toReturn.ValidationResult = validationResult;

            if (srcPageDto != null && !string.IsNullOrWhiteSpace(srcPageDto.FileFullPath) && !string.IsNullOrWhiteSpace(srcPageDto.FileCode) && srcPageDto.EsiteId.HasValue)
            {


                string fileFullPath = srcPageDto.FileFullPath;
                string newFileFullPath = "";

                if (string.IsNullOrWhiteSpace(distFileName))
                {
                    distFileName = srcPageDto.FileCode;
                }


                int eSiteId = srcPageDto.EsiteId.Value;

                AppEsiteExDto appEsiteExDto = AppEsiteConfigBL.RetrieveOneAppEsiteExDto(eSiteId);

                if (!string.IsNullOrWhiteSpace(requestHostServerPath))
                {
                    if (requestHostServerPath.ToLower().EndsWith("/mgt/"))
                    {
                        appEsiteExDto.SitePublishedBaseUrl = requestHostServerPath.Substring(0, requestHostServerPath.Length - 4);
                        appEsiteExDto.EsiteAttribute.MgtSiteBaseUrl = requestHostServerPath;

                        if (needToPreSaveEsite)
                        {
                            var saveEsiteResult = AppEsiteConfigBL.SaveAppEsiteExDto(appEsiteExDto);
                            if (saveEsiteResult.IsSuccessfulWithResult)
                            {
                                appEsiteExDto = saveEsiteResult.Object;
                            }
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(appEsiteExDto.SitePublishedBaseUrl))
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_SitePublishedBaseUrl_IsEmpty.", ValidationItemType.Error, "Publish cannot be processed. The Site Publish Base Url is not defined. Please set it on Global Site Setting."));

                    toReturn.Object = false;

                    return toReturn;
                }

                string appSitePath = GetWebSiteBasePath(eSiteId);

                string subsiteName = "";

                if (fileFullPath.Contains(appSitePath + PublicFolderName))
                {
                    subsiteName = PublicFolderName;
                }
                else if (fileFullPath.Contains(appSitePath + SupplierFolderName))
                {
                    subsiteName = SupplierFolderName;
                }
                else if (fileFullPath.Contains(appSitePath + CustomerFolderName))
                {
                    subsiteName = CustomerFolderName;
                }

                if (!string.IsNullOrWhiteSpace(subsiteName))
                {
                    newFileFullPath = fileFullPath.Replace("\\Dev\\StaticPages\\", "\\");

                    if (eSiteId == CompanyDefaultSiteId && subsiteName == PublicFolderName)
                    {
                        newFileFullPath = fileFullPath.Replace((PublicFolderName + "\\Dev\\StaticPages\\"), "\\");
                    }


                }


                if (string.IsNullOrWhiteSpace(newFileFullPath))
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_PublishFailed.", ValidationItemType.Error, "Publish cannot be processed. Invalid file path."));

                    toReturn.Object = false;

                    return toReturn;
                }

                if (appEsiteExDto.EsiteAttribute == null)
                {
                    appEsiteExDto.EsiteAttribute = new EsiteAttributeDto();
                }

                AppEsitePagesExDto sourcePageExDto = RetrieveOneWebSiteFile(fileFullPath, eSiteId);


                if (!string.IsNullOrWhiteSpace(newHtmlContent))
                {
                    sourcePageExDto.HtmlContent = newHtmlContent;
                }


                if (sourcePageExDto.IsSearchViewPage && sourcePageExDto.SearchId.HasValue)
                {
                    ExportOneEsitePageToStaticPage_PopulateSearchViewPageHtml(sourcePageExDto, appEsiteExDto);

                    if (sourcePageExDto.PageAttribute != null
                            && sourcePageExDto.PageAttribute.NeedToGenerateStaticSearchDetailViewPages
                            && !string.IsNullOrWhiteSpace(sourcePageExDto.PageAttribute.StaticSiteSearchDetailViewPageFileName)
                            && sourcePageExDto.PageAttribute.StaticSiteSearchDetailViewPagePkViewColumnId.HasValue)
                    {
                        ExportOneEsitePageToStaticPage_GenerateSearchViewDetailPages(sourcePageExDto, appEsiteExDto, validationResult, requestHostServerPath, subsiteName);
                    }
                }




                string filepath_staticPageRootContainer = string.Format("{0}{1}\\Dev\\StaticPages\\PagePart\\_StaticSitePageRootContainer.html", appSitePath, subsiteName);
                //string filepath_publicHeader = string.Format("{0}{1}\\Dev\\StaticPages\\PagePart\\_StaticSiteHeader.html", appSitePath, subsiteName);
                //string filepath_publicFooter = string.Format("{0}{1}\\Dev\\StaticPages\\PagePart\\_StaticSiteFooter.html", appSitePath, subsiteName);
                //string filepath_mobileTopMenu = string.Format("{0}{1}\\Dev\\StaticPages\\PagePart\\_StaticSiteMobileTopMenu.html", appSitePath, subsiteName);

                AppEsitePagesExDto rootContainerFile = RetrieveOneWebSiteFile(filepath_staticPageRootContainer, eSiteId);
                //AppEsitePagesExDto publicHeaderFile = RetrieveOneWebSiteFile(filepath_publicHeader, eSiteId);
                //AppEsitePagesExDto publicFooterFile = RetrieveOneWebSiteFile(filepath_publicFooter, eSiteId);
                //AppEsitePagesExDto mobileTopMenuFile = RetrieveOneWebSiteFile(filepath_mobileTopMenu, eSiteId);


                AppEsitePagesExDto publicHeaderFile = new AppEsitePagesExDto();
                AppEsitePagesExDto publicFooterFile = new AppEsitePagesExDto();
                AppEsitePagesExDto mobileTopMenuFile = new AppEsitePagesExDto();

                if (subsiteName == PublicFolderName)
                {

                    int emAppMenuItemCategory = (int)EmAppMenuItemCategory.PublicPage;

                    if (string.IsNullOrWhiteSpace(publicHeaderFile.HtmlContent))
                    {

                        string filepath_dynamicHeader = string.Format("{0}{1}\\Dev\\DynamicFormPages\\LandingPage\\_SiteHeader.html", appSitePath, subsiteName);
                        publicHeaderFile.HtmlContent = RetrieveOneWebSiteFile(filepath_dynamicHeader, eSiteId).HtmlContent;

                        ConvertLandingPagePartFromDynamicToStatic(eSiteId, appEsiteExDto, publicHeaderFile, emAppMenuItemCategory);

                    }

                    if (string.IsNullOrWhiteSpace(publicFooterFile.HtmlContent))
                    {
                        string filepath_dynamicFooter = string.Format("{0}{1}\\Dev\\DynamicFormPages\\LandingPage\\_SiteFooter.html", appSitePath, subsiteName);
                        publicFooterFile.HtmlContent = RetrieveOneWebSiteFile(filepath_dynamicFooter, eSiteId).HtmlContent;

                        ConvertLandingPagePartFromDynamicToStatic(eSiteId, appEsiteExDto, publicFooterFile, emAppMenuItemCategory);
                    }

                    if (string.IsNullOrWhiteSpace(mobileTopMenuFile.HtmlContent))
                    {
                        string filepath_dynamicMobileTopMenu = string.Format("{0}{1}\\Dev\\DynamicFormPages\\GlobalPopups\\_MobileTopMenuPopup.html", appSitePath, subsiteName);
                        mobileTopMenuFile.HtmlContent = RetrieveOneWebSiteFile(filepath_dynamicMobileTopMenu, eSiteId).HtmlContent;

                        ConvertLandingPagePartFromDynamicToStatic(eSiteId, appEsiteExDto, mobileTopMenuFile, emAppMenuItemCategory);
                    }
                }



                string htmlContent = rootContainerFile.HtmlContent;

                htmlContent = ReplaceBodyString(htmlContent, "<!--*****Start Of SiteHeader-->", "<!--*****End Of SiteHeader-->", publicHeaderFile.HtmlContent);
                htmlContent = ReplaceBodyString(htmlContent, "<!--*****Start Of Main UiView Container-->", "<!--*****End Of Main UiView Container-->", sourcePageExDto.HtmlContent);
                htmlContent = ReplaceBodyString(htmlContent, "<!--*****Start Of SiteFooter-->", "<!--*****End Of SiteFooter-->", publicFooterFile.HtmlContent);

                htmlContent = ReplaceBodyString(htmlContent, "<!--*****Start Of Mobile Top Menu-->", "<!--*****End Of Mobile Top Menu-->", mobileTopMenuFile.HtmlContent);


                string appSiteBaseUrl = appEsiteExDto.SitePublishedBaseUrl;

                if (!string.IsNullOrWhiteSpace(appSiteBaseUrl))
                {
                    if (!appSiteBaseUrl.EndsWith("/"))
                    {
                        appSiteBaseUrl += "/";
                    }

                    htmlContent = htmlContent.Replace(" src=\"./", " src=\"" + appSiteBaseUrl);
                    htmlContent = htmlContent.Replace(" href=\"./", " href=\"" + appSiteBaseUrl);
                    htmlContent = htmlContent.Replace("url(./", "url(" + appSiteBaseUrl);

                    htmlContent = htmlContent.Replace("\"SharedResource/", "\"" + appSiteBaseUrl + "SharedResource/");
                }
                else
                {
                    //string relativeUrlPrefix = "../../";

                    //if (!string.IsNullOrWhiteSpace(distSubFolderPath))
                    //{
                    //    relativeUrlPrefix = "../";

                    //    int index = distSubFolderPath.IndexOf("\\");

                    //    while (index >= 0 && index < distSubFolderPath.Length - 2)
                    //    {
                    //        relativeUrlPrefix += "../";
                    //        index = distSubFolderPath.IndexOf("\\", index + "\\".Length);
                    //    }

                    //    //htmlContent = htmlContent.Replace(" src=\"js/", " src=\"" + relativeUrlPrefix + "js/");
                    //    //htmlContent = htmlContent.Replace(" href=\"style/", " href=\"" + relativeUrlPrefix + "style/");

                    //    htmlContent = htmlContent.Replace(" src=\"./", " src=\"" + relativeUrlPrefix);
                    //    htmlContent = htmlContent.Replace(" href=\"./", " href=\"" + relativeUrlPrefix);
                    //    htmlContent = htmlContent.Replace("url(./", "url(" + relativeUrlPrefix);

                    //}
                }

                string pageTitle = appEsiteExDto.Name;
                string pageDescription = appEsiteExDto.Description;
                string pageSearchKeywords = appEsiteExDto.EsiteAttribute.SearchEngineKeywords;
                string pageOgImagePath = appEsiteExDto.EsiteAttribute.OgImageUrl;

                if (srcPageDto.PageAttribute != null)
                {
                    if (!string.IsNullOrWhiteSpace(srcPageDto.PageAttribute.PageTitle))
                    {
                        pageTitle = srcPageDto.PageAttribute.PageTitle;
                    }

                    if (!string.IsNullOrWhiteSpace(srcPageDto.PageAttribute.PageDescription))
                    {
                        pageDescription = srcPageDto.PageAttribute.PageDescription;
                    }

                    if (!string.IsNullOrWhiteSpace(srcPageDto.PageAttribute.SearchEngineKeywords))
                    {
                        pageSearchKeywords = srcPageDto.PageAttribute.SearchEngineKeywords + ", " + pageSearchKeywords;
                    }

                    if (!string.IsNullOrWhiteSpace(srcPageDto.PageAttribute.OgImageUrl))
                    {
                        pageOgImagePath = srcPageDto.PageAttribute.OgImageUrl;
                    }
                }


                htmlContent = htmlContent.Replace("%{expression.siteTitle}", pageTitle);
                htmlContent = htmlContent.Replace("%{expression.siteDescription}", pageDescription);
                htmlContent = htmlContent.Replace("%{expression.siteKeywords}", pageSearchKeywords);
                htmlContent = htmlContent.Replace("%{expression.siteOgImagePath}", pageOgImagePath);

                if (sourcePageExDto.PageAttribute != null)
                {
                    htmlContent = htmlContent.Replace("%{expression.PageText1}", sourcePageExDto.PageAttribute.PageText1);
                    htmlContent = htmlContent.Replace("%{expression.PageText2}", sourcePageExDto.PageAttribute.PageText2);
                    htmlContent = htmlContent.Replace("%{expression.PageText3}", sourcePageExDto.PageAttribute.PageText3);
                    htmlContent = htmlContent.Replace("%{expression.PageText4}", sourcePageExDto.PageAttribute.PageText4);
                    htmlContent = htmlContent.Replace("%{expression.PageText5}", sourcePageExDto.PageAttribute.PageText5);
                }






                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(newFileFullPath));

                    // Create a new file                                  

                    using (FileStream fs = File.Create(newFileFullPath))
                    {
                        // Add some text to file    
                        Byte[] title = new UTF8Encoding(true).GetBytes(htmlContent);
                        fs.Write(title, 0, title.Length);
                        //FileInfo fileInfo = new FileInfo(newFileFullPath);
                    }

                    validationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_Save_OK", ValidationItemType.Message, "Publish Successful"));
                    toReturn.Object = true;

                }
                catch (Exception ex)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_SaveAppEsitePagesExDtoWithFile_Error", ValidationItemType.Error, ex.ToString()));

                    toReturn.Object = false;
                }
            }
            else
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_SaveAppEsitePagesExDtoWithFile_Error", ValidationItemType.Error, "Invalid Page."));

                toReturn.Object = false;
            }



            return toReturn;
        }

        private static void ConvertLandingPagePartFromDynamicToStatic(int eSiteId, AppEsiteExDto appEsiteExDto, AppEsitePagesExDto srcFile, int emAppMenuItemCategory)
        {
            srcFile.HtmlContent = srcFile.HtmlContent.Replace("ng-click", "onclick").Replace("goToPageByRouteCode", "gotoAppSiteRoutePage");

            if (!appEsiteExDto.IsAllowCustomerRegister)
            {
                for (int i = 0; i < 10; i++)
                {
                    srcFile.HtmlContent = ReplaceBodyString(srcFile.HtmlContent, "<!--*****Start Of Client Sign Up Button-->", "<!--*****End Of Client Sign Up Button-->", "");
                }
            }

            if (!appEsiteExDto.IsAllowSupplierRegister)
            {
                for (int i = 0; i < 10; i++)
                {
                    srcFile.HtmlContent = ReplaceBodyString(srcFile.HtmlContent, "<!--*****Start Of Supplier Sign Up Button-->", "<!--*****End Of Supplier Sign Up Button-->", "");
                }
            }

            srcFile.HtmlContent = ConvertNavigatoinMenuFromDynamicToStatic(appEsiteExDto, srcFile.HtmlContent, emAppMenuItemCategory);
        }

        private static string ConvertNavigatoinMenuFromDynamicToStatic(AppEsiteExDto appEsiteExDto, string srcHtmlContent, int emAppMenuItemCategory)
        {
            int eSiteId = (int)appEsiteExDto.Id;
            Dictionary<string, AppEsitePagesExDto> dictRouteCodeAndPageDto = new Dictionary<string, AppEsitePagesExDto>();
            appEsiteExDto.AppEsitePagesList.Where(o => !string.IsNullOrWhiteSpace(o.MetaDesciption)).ForAll(o =>
            {
                if (!dictRouteCodeAndPageDto.ContainsKey(o.MetaDesciption))
                {
                    dictRouteCodeAndPageDto.Add(o.MetaDesciption, o);
                }
            });


            string body = srcHtmlContent;
            string start = "<!--*****Start Of Navigation Menus-->";
            string end = "<!--*****End Of Navigation Menus-->";


            int indexStart = body.IndexOf(start); ;

            int count = 0;

            while (indexStart != -1)
            {

                count++;

                int indexEnd = indexStart + body.Substring(indexStart).IndexOf(end);

                int needToReplace_IndexStart = indexStart + start.Length;
                int needToReplace_Length = indexEnd - needToReplace_IndexStart;

                string needToReplaceString = body.Substring(needToReplace_IndexStart, needToReplace_Length);
                string replaceWithString = "";

                if (!string.IsNullOrWhiteSpace(needToReplaceString))
                {


                    needToReplaceString = needToReplaceString.Replace("ng-repeat=\"menuDto_L1 in navDataModel.publicMenus | orderBy:'Sort'\"", "")
                        .Replace("ng-if=\"menuDto_L1.EmDeviceMenuShowMode!=4\"", "");

                    List<AppListMenuExDto> siteMenus = AppTreeListMenuBL.RetrieveNoneMgtUserTreeMenu(eSiteId, emAppMenuItemCategory)
                        .Where(o => !(o.EmDeviceMenuShowMode.HasValue && o.EmDeviceMenuShowMode.Value == 4)).OrderBy(o => o.Sort).ToList();



                    foreach (var menuDto in siteMenus)
                    {
                        string menuDomString = needToReplaceString.Replace("{{menuDto_L1.Name}}", menuDto.Name);
                        string linkUrl = menuDto.Link;
                        string replaceMenuClickToString = "";

                        if (menuDto.LinkType == (int)EmAppListMenuLinkType.SystemPage)
                        {
                            replaceMenuClickToString = "gotoAppSiteRoutePage('" + menuDto.RouteCode + "', '" + linkUrl + "')";

                            if (!string.IsNullOrWhiteSpace(menuDto.RouteCode) && dictRouteCodeAndPageDto.ContainsKey(menuDto.RouteCode))
                            {
                                var pageDto = dictRouteCodeAndPageDto[menuDto.RouteCode];

                                if (!string.IsNullOrWhiteSpace(pageDto.FileFullPath))
                                {
                                    if (pageDto.FileFullPath.Contains("Dev\\StaticPages\\"))
                                    {
                                        string subfolderName = "";

                                        if (emAppMenuItemCategory == (int)EmAppMenuItemCategory.PublicPage)
                                        {
                                            subfolderName = "Public";
                                        }
                                        if (emAppMenuItemCategory == (int)EmAppMenuItemCategory.ClientPage)
                                        {
                                            subfolderName = "Customer";
                                        }
                                        if (emAppMenuItemCategory == (int)EmAppMenuItemCategory.SupplierPage)
                                        {
                                            subfolderName = "Supplier";
                                        }


                                        replaceMenuClickToString = "goToStaticSitePage('" + pageDto.Title + "', '" + subfolderName + "')";
                                    }
                                }
                            }
                        }
                        else if (menuDto.LinkType == (int)EmAppListMenuLinkType.CallBuiltInFunction)
                        {
                            replaceMenuClickToString = linkUrl;


                        }
                        else if (menuDto.LinkType == (int)EmAppListMenuLinkType.WebPopup)
                        {
                            if (string.IsNullOrWhiteSpace(menuDto.Link))
                            {
                                replaceMenuClickToString = "openUrlOnNewTab('" + linkUrl + "')";
                            }
                        }

                        menuDomString = menuDomString.Replace("menuClicked(menuDto_L1)", replaceMenuClickToString);

                        replaceWithString += menuDomString;
                    }
                }

                string newString = body.Substring(0, indexStart)
                        + replaceWithString
                        + body.Substring(indexEnd + end.Length);

                body = newString;

                indexStart = body.IndexOf(start);


            }

            if (count > 10)
            {

                throw new System.Exception("Process Navigation Menu Error.");
            }


            return body;
        }

        public static string ReplaceBodyString(string body, string start, string end, string replaceWithString)
        {
            string newString = body;

            int indexStart = 0;
            int indexEnd = 0;

            indexStart = body.IndexOf(start);

            if (indexStart != -1)
            {
                indexEnd = indexStart + body.Substring(indexStart).IndexOf(end) + end.Length;
                newString = body.Substring(0, indexStart)
                    + replaceWithString
                    + body.Substring(indexEnd);
            }

            return newString;
        }


        public static OperationCallResult<bool> SaveAppEsiteMediaQueryChangesToStyleSheet(AppEsiteStyleSheetUpdateDto styleSheetUpdateDto)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            int? eSiteId = styleSheetUpdateDto.EsiteId;

            if (eSiteId.HasValue)
            {
                string appSitePath = GetWebSiteBasePath(eSiteId);

                string locationType = styleSheetUpdateDto.ScssFileComanyOrAppType;

                string loclationTypeForder = string.Format(@"SharedResource\style\PartialScss\MediaQuerySubStyle\{0}\", locationType);


                loclationTypeForder = Path.Combine(appSitePath, loclationTypeForder);

                var dictMedaiQuery = styleSheetUpdateDto.DictMeidaSizeCodeAndDomIdAndStyleObj;


                if (!dictMedaiQuery.IsEmpty())
                {
                    Process6MediaQueryWidth(locationType, loclationTypeForder, dictMedaiQuery);
                    CompileEsiteScssToCss(appSitePath);

                }



            }




            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = true;
            }

            return aOperationCallResult;
        }

        //public static AppEsiteThemParameterUpdateDto RetrieveWebsiteThemParameters(int eSiteId, string filePath)
        //{
        //    AppEsiteThemParameterUpdateDto toReturn = new AppEsiteThemParameterUpdateDto();
        //    toReturn.EsiteId = eSiteId;
        //    toReturn.FilePath = filePath;
        //    toReturn.ParameterDtoList = new List<AppEsiteThemParameterDto>();




        //    string websiteBAseFoder = GetCompanyWebSiteBaseFolderPath(eSiteId);

        //    //string navigationCtrlJsFullPath = Path.Combine(websiteBAseFoder, @"js\appWebsiteGenericNavigationCtrl.js");
        //    string fullFullPath = Path.Combine(websiteBAseFoder, filePath);



        //    string originalString = File.ReadAllText(fullFullPath);

        //    //List<string> repalceSectionList = ExtractFromBody(originalString, beginToken, endToken);

        //    List<string> codeLineLies = originalString.Split('\n').ToList();

        //    string commentText = "";
        //    int sort = 0;
        //    foreach (string codeLine in codeLineLies)
        //    {
        //        string lineText = codeLine.Trim();

        //        if (lineText.StartsWith("/*") && lineText.EndsWith("*/"))
        //        {
        //            AppEsiteThemParameterDto parameter = new AppEsiteThemParameterDto();
        //            parameter.ParameterType = "Comment";
        //            parameter.ParameterValue = lineText;

        //            commentText = lineText.Substring(2, lineText.Length - 4);
        //            //toReturn.Add(parameter);
        //        }
        //        else
        //        {
        //            List<string> parameterStringList = lineText.Split(';').ToList();
        //            foreach (string parameterString in parameterStringList)
        //            {
        //                List<string> parameterParts = parameterString.Split(':').ToList();

        //                if (parameterParts.Count == 2)
        //                {
        //                    string parameterName = parameterParts[0].Trim();
        //                    string parameterValue = parameterParts[1].Trim();

        //                    if (parameterName.Contains('$') && !string.IsNullOrWhiteSpace(parameterValue))
        //                    {
        //                        AppEsiteThemParameterDto parameter = new AppEsiteThemParameterDto();
        //                        parameter.ParameterType = "RegularText";
        //                        parameter.ParameterName = parameterName;
        //                        parameter.ParameterValue = parameterValue;

        //                        if (parameterName.EndsWith("BgColor"))
        //                        {
        //                            parameter.ParameterType = "BgColor";
        //                        }
        //                        else if (parameterName.EndsWith("FontColor"))
        //                        {
        //                            parameter.ParameterType = "FontColor";
        //                        }
        //                        else if (parameterName.EndsWith("FontFamily") || parameterName.EndsWith("Font"))
        //                        {
        //                            parameter.ParameterType = "FontFamily";
        //                        }
        //                        else if (parameterName.EndsWith("FontSize"))
        //                        {
        //                            parameter.ParameterType = "FontSize";
        //                        }


        //                        parameter.Description = commentText;

        //                        sort++;
        //                        parameter.Sort = sort;
        //                        toReturn.ParameterDtoList.Add(parameter);
        //                    }
        //                }
        //            }
        //        }

        //    }

        //    string previewFileFullPath = string.Empty;

        //    if (filePath.Contains("MainSite"))
        //    {
        //        previewFileFullPath = Path.Combine(websiteBAseFoder, @"pages\BuiltIn\StyleParameterPreview\_MainSiteStyleParameterPreview.html");
        //    }
        //    else if (filePath.Contains("UserSubsite"))
        //    {
        //        previewFileFullPath = Path.Combine(websiteBAseFoder, @"pages\BuiltIn\StyleParameterPreview\_UserSubsiteStyleParameterPreview.html");
        //    }

        //    if (!string.IsNullOrWhiteSpace(previewFileFullPath))
        //    {
        //        toReturn.PreviewFileHtmlContent = File.ReadAllText(previewFileFullPath);
        //    }


        //    return toReturn;
        //}

        //public static OperationCallResult<bool> SaveWebsiteThemParameters(AppEsiteThemParameterUpdateDto themParameterUpdateDto)
        //{
        //    OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
        //    ValidationResult aValidationResult = new ValidationResult();
        //    aOperationCallResult.ValidationResult = aValidationResult;

        //    //AppEsitePagesExDto appEsitePagesDto = new AppEsitePagesExDto();
        //    //OperationCallResult<AppEsitePagesExDto> saveResult = SaveOneWebSiteFileWithHtmlPageAtributeFile(appEsitePagesDto);

        //    if (themParameterUpdateDto.EsiteId.HasValue
        //        && !string.IsNullOrWhiteSpace(themParameterUpdateDto.FilePath)
        //        && themParameterUpdateDto.ParameterDtoList != null)
        //    {
        //        int eSiteId = themParameterUpdateDto.EsiteId.Value;
        //        string websiteBAseFoder = GetCompanyWebSiteBaseFolderPath(eSiteId);
        //        string fullFullPath = Path.Combine(websiteBAseFoder, themParameterUpdateDto.FilePath);


        //        string htmlContent = string.Empty;

        //        string commentText = string.Empty;

        //        foreach (AppEsiteThemParameterDto parameterDto in themParameterUpdateDto.ParameterDtoList)
        //        {
        //            if (!string.IsNullOrWhiteSpace(parameterDto.Description))
        //            {
        //                if (parameterDto.Description != commentText)
        //                {
        //                    htmlContent += "\n" + "/* " + parameterDto.Description + " */" + "\n";
        //                    commentText = parameterDto.Description;
        //                }
        //            }

        //            if (!string.IsNullOrWhiteSpace(parameterDto.ParameterName) && !string.IsNullOrWhiteSpace(parameterDto.ParameterValue))
        //            {
        //                htmlContent += parameterDto.ParameterName.Trim() + ": " + parameterDto.ParameterValue.Trim() + ";" + "\n";
        //            }
        //        }

        //        try
        //        {


        //            using (FileStream fs = File.Create(fullFullPath))
        //            {
        //                Byte[] title = new UTF8Encoding(true).GetBytes(htmlContent);
        //                fs.Write(title, 0, title.Length);
        //            }

        //            if (themParameterUpdateDto.EsiteId.HasValue)
        //            {
        //                string appSitePath = GetWebSiteBasePath(eSiteId);
        //                CompileEsiteScssToCss(appSitePath);
        //            }

        //            aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
        //            aOperationCallResult.Object = true;
        //        }
        //        catch (Exception ex)
        //        {
        //            aOperationCallResult.Object = false;
        //            aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_SaveWebsiteThemParameters_Error", ValidationItemType.Error, ex.ToString()));
        //        }
        //    }
        //    else
        //    {
        //        aOperationCallResult.Object = false;
        //        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_SaveWebsiteThemParameters_Error", ValidationItemType.Error, "Save Failed."));
        //    }

        //    return aOperationCallResult;
        //}


        public static OperationCallResult<bool> SaveGlobalWebsiteThemParameters(AppEsiteExDto appEsiteExDto)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            var saveEsiteResult = AppEsiteConfigBL.SaveAppEsiteExDto(appEsiteExDto);

            if (saveEsiteResult.IsSuccessful)
            {
                string siteDefaultClassString = "class=\"AppSite";
                string siteHeaderClassString = "class=\"App_SiteHeader w-full";
                string siteFooterClassString = "class=\"App_SiteFooter w-full";

                var parameterList = appEsiteExDto.EsiteAttribute.GlobalSiteThemeParameterList;

                foreach (var parameter in parameterList)
                {
                    if (!string.IsNullOrWhiteSpace(parameter.ParameterValue))
                    {
                        if (parameter.ParameterCategory == "Site Default")
                        {
                            siteDefaultClassString += " " + parameter.ParameterValue.Trim();
                        }
                        else if (parameter.ParameterCategory == "Site Header")
                        {
                            siteHeaderClassString += " " + parameter.ParameterValue.Trim();
                        }
                        else if (parameter.ParameterCategory == "Site Footer")
                        {
                            siteFooterClassString += " " + parameter.ParameterValue.Trim();
                        }

                    }
                }

                string websiteBAseFoder = appEsiteExDto.RootFolderPath;

                List<string> needToUpdateFilePathList_SiteDefulat = new List<string>();
                needToUpdateFilePathList_SiteDefulat.Add(Path.Combine(websiteBAseFoder, @"index.html"));
                needToUpdateFilePathList_SiteDefulat.Add(Path.Combine(websiteBAseFoder, @"DesignPreview.html"));
                needToUpdateFilePathList_SiteDefulat.Add(Path.Combine(websiteBAseFoder, @"DesignPreviewDp.html"));
                needToUpdateFilePathList_SiteDefulat.Add(Path.Combine(websiteBAseFoder, @"DesignPreviewWj.html"));
                needToUpdateFilePathList_SiteDefulat.Add(Path.Combine(websiteBAseFoder, @"Public\Dev\StaticPages\PagePart\_StaticSitePageRootContainer.html"));
                needToUpdateFilePathList_SiteDefulat.Add(Path.Combine(websiteBAseFoder, @"Customer\Dev\StaticPages\PagePart\_StaticSitePageRootContainer.html"));
                needToUpdateFilePathList_SiteDefulat.Add(Path.Combine(websiteBAseFoder, @"Supplier\Dev\StaticPages\PagePart\_StaticSitePageRootContainer.html"));

                List<string> needToUpdateFilePathList_SiteHeader = new List<string>();
                needToUpdateFilePathList_SiteHeader.Add(Path.Combine(websiteBAseFoder, @"Public\Dev\DynamicFormPages\LandingPage\_SiteHeader.html"));
                needToUpdateFilePathList_SiteHeader.Add(Path.Combine(websiteBAseFoder, @"Public\Dev\StaticPages\PagePart\_StaticSiteHeader.html"));
                needToUpdateFilePathList_SiteHeader.Add(Path.Combine(websiteBAseFoder, @"Customer\Dev\DynamicFormPages\LandingPage\_Header.html"));
                needToUpdateFilePathList_SiteHeader.Add(Path.Combine(websiteBAseFoder, @"Customer\Dev\StaticPages\PagePart\_StaticSiteHeader.html"));
                needToUpdateFilePathList_SiteHeader.Add(Path.Combine(websiteBAseFoder, @"Supplier\Dev\DynamicFormPages\LandingPage\_Header.html"));
                needToUpdateFilePathList_SiteHeader.Add(Path.Combine(websiteBAseFoder, @"Supplier\Dev\StaticPages\PagePart\_StaticSiteHeader.html"));

                List<string> needToUpdateFilePathList_SiteFooter = new List<string>();
                needToUpdateFilePathList_SiteFooter.Add(Path.Combine(websiteBAseFoder, @"Public\Dev\DynamicFormPages\LandingPage\_SiteFooter.html"));
                needToUpdateFilePathList_SiteFooter.Add(Path.Combine(websiteBAseFoder, @"Public\Dev\StaticPages\PagePart\_StaticSiteFooter.html"));
                needToUpdateFilePathList_SiteFooter.Add(Path.Combine(websiteBAseFoder, @"Customer\Dev\DynamicFormPages\LandingPage\_Footer.html"));
                needToUpdateFilePathList_SiteFooter.Add(Path.Combine(websiteBAseFoder, @"Customer\Dev\StaticPages\PagePart\_StaticSiteFooter.html"));
                needToUpdateFilePathList_SiteFooter.Add(Path.Combine(websiteBAseFoder, @"Supplier\Dev\DynamicFormPages\LandingPage\_Footer.html"));
                needToUpdateFilePathList_SiteFooter.Add(Path.Combine(websiteBAseFoder, @"Supplier\Dev\StaticPages\PagePart\_StaticSiteFooter.html"));

                try
                {
                    foreach (string fileLoclation in needToUpdateFilePathList_SiteDefulat)
                    {
                        string originalString = File.ReadAllText(fileLoclation);

                        List<string> repalceSectionList = ExtractFromBody(originalString, "class=\"AppSite", "\"");


                        if (repalceSectionList.Count > 0)
                        {
                            string firstSection = repalceSectionList[0];
                            firstSection = "class=\"AppSite" + firstSection;

                            originalString = originalString.Replace(firstSection, siteDefaultClassString);

                            File.WriteAllText(fileLoclation, originalString);
                        }
                    }

                    foreach (string fileLoclation in needToUpdateFilePathList_SiteHeader)
                    {
                        string originalString = File.ReadAllText(fileLoclation);

                        List<string> repalceSectionList = ExtractFromBody(originalString, "class=\"App_SiteHeader", "\"");


                        if (repalceSectionList.Count > 0)
                        {
                            string firstSection = repalceSectionList[0];
                            firstSection = "class=\"App_SiteHeader" + firstSection;

                            originalString = originalString.Replace(firstSection, siteHeaderClassString);

                            File.WriteAllText(fileLoclation, originalString);
                        }
                    }

                    foreach (string fileLoclation in needToUpdateFilePathList_SiteFooter)
                    {
                        string originalString = File.ReadAllText(fileLoclation);

                        List<string> repalceSectionList = ExtractFromBody(originalString, "class=\"App_SiteFooter", "\"");


                        if (repalceSectionList.Count > 0)
                        {
                            string firstSection = repalceSectionList[0];
                            firstSection = "class=\"App_SiteFooter" + firstSection;

                            originalString = originalString.Replace(firstSection, siteFooterClassString);

                            File.WriteAllText(fileLoclation, originalString);
                        }
                    }
                }
                catch (Exception ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_SaveGlobalWebsiteThemParameters_Error", ValidationItemType.Error, "Save Site File Failed. " + ex.Message));
                }

            }
            else
            {
                aValidationResult.Merge(saveEsiteResult.ValidationResult);
            }

            if (!aValidationResult.HasErrors)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                aOperationCallResult.Object = true;
            }


            return aOperationCallResult;
        }









        public static OperationCallResult<bool> UpdateOneAppWebsiteStyleRule(AppEsiteStyleSheetUpdateDto styleSheetUpdateDto)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            int? eSiteId = styleSheetUpdateDto.EsiteId;

            if (eSiteId.HasValue)
            {
                string appSitePath = GetWebSiteBasePath(eSiteId);

                if (!styleSheetUpdateDto.DictNeedToUpdateStyleRuleSelectorAndStyleText.IsEmpty())
                {
                    try
                    {
                        foreach (string selectorText in styleSheetUpdateDto.DictNeedToUpdateStyleRuleSelectorAndStyleText.Keys)
                        {
                            string cssRuleText = styleSheetUpdateDto.DictNeedToUpdateStyleRuleSelectorAndStyleText[selectorText];
                            string cssFilePath = @"SharedResource\style\PartialScss\_8_Public_UserDefinedStyle.scss";

                            string update_selectorText = selectorText.Replace("&nbsp;", " ");
                            string update_cssRuleText = cssRuleText.Replace("&nbsp;", " ");

                            if (update_cssRuleText.StartsWith(update_selectorText))
                            {
                                update_cssRuleText = update_cssRuleText.Substring(update_selectorText.Length).Replace("{", "").Replace("}", "").Trim();
                            }

                            if (selectorText.StartsWith(prefix_DesignWidthRange))
                            {
                                update_selectorText = update_selectorText.Substring(prefix_DesignWidthRange.Length + 1);

                                string widthRangeIndex = selectorText.Substring(prefix_DesignWidthRange.Length, 1);

                                if (selectorText.Contains(cssClass_CtnCompanySitePage))
                                {
                                    cssFilePath = @"SharedResource\style\PartialScss\MediaQuerySubStyle\CompanySite\_CompanySite_Media_WidthRange" + widthRangeIndex + ".scss";
                                }
                                else
                                {
                                    cssFilePath = @"SharedResource\style\PartialScss\MediaQuerySubStyle\AppMain\_AppMain_Media_WidthRange" + widthRangeIndex + ".scss";
                                }
                            }


                            string scssFileLoclation = Path.Combine(appSitePath, cssFilePath);

                            Dictionary<string, AppEsiteStyleObjDto> dictStyle = new Dictionary<string, AppEsiteStyleObjDto>();
                            dictStyle.Add(update_selectorText, new AppEsiteStyleObjDto() { CssText = update_cssRuleText });
                            PoressOneSccsFile(scssFileLoclation, dictStyle);
                        }

                        CompileEsiteScssToCss(appSitePath);
                    }
                    catch (Exception ex)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_UpdateOneAppWebsiteStyleRule_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }

            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = true;
            }

            return aOperationCallResult;
        }

        private static void CompileEsiteScssToCss(string appSitePath, bool isCompileEsiteStyle = true, bool isCompileEsiteDesign = true, bool isCompileTwDesign = false)
        {
            // Compile Scsss to Css

#if NETFRAMEWORK
            // TODO-PHASE4: Replace with .NET 10 equivalent
            string baseDir = Path.Combine(appSitePath, @"SharedResource\style\");

            string inputFilePath = Path.Combine(baseDir, "EsiteStyle.scss");
            string outputFilePath = Path.Combine(baseDir, "EsiteStyle.css");

            string inputFilePath_Design = Path.Combine(baseDir, "DesignStyle\\EsiteStyle_Design.scss");
            string outputFilePath_Design = Path.Combine(baseDir, "DesignStyle\\EsiteStyle_Design.css");

            string inputFilePath_tailwindDesignMode = Path.Combine(baseDir, "DesignStyle\\tailwindDesignMode.scss");
            string outputFilePath_tailwindDesignMode = Path.Combine(baseDir, "DesignStyle\\tailwindDesignMode.css");

            try
            {
                if (isCompileEsiteStyle)
                {
                    CompilationResult result = SassCompiler.CompileFile(inputFilePath);
                    File.WriteAllText(outputFilePath, result.CompiledContent);
                }

                if (isCompileEsiteDesign)
                {
                    CompilationResult result_Design = SassCompiler.CompileFile(inputFilePath_Design);
                    File.WriteAllText(outputFilePath_Design, result_Design.CompiledContent);
                }

                if (isCompileTwDesign)
                {
                    CompilationResult result_tailwindDesign = SassCompiler.CompileFile(inputFilePath_tailwindDesignMode);
                    File.WriteAllText(outputFilePath_tailwindDesignMode, result_tailwindDesign.CompiledContent);
                }
            }
            catch (SassException e)
            {
                //WriteError("During compilation of SCSS file an error occurred.", e);
            }
#else
            throw new PlatformNotSupportedException("TODO-PHASE4: Migrate to .NET 10 equivalent");
#endif
        }

        private static void Process6MediaQueryWidth(string locationType, string loclationTypeForder, Dictionary<string, Dictionary<string, AppEsiteStyleObjDto>> dictMedaiQuery)
        {
            for (int i = 1; i <= 6; i++)
            {
                string indexS = i.ToString();
                if (dictMedaiQuery.ContainsKey(indexS))
                {
                    string scssFileLoclation = "";
                    if (locationType == "AppMain")
                    {
                        string fileFormat = string.Format(@"_AppMain_Media_WidthRange{0}.scss", indexS);
                        scssFileLoclation = Path.Combine(loclationTypeForder, fileFormat);
                    }
                    else if (locationType == "CompanySite")
                    {
                        string fileFormat = string.Format(@"_CompanySite_Media_WidthRange{0}.scss", indexS);
                        scssFileLoclation = Path.Combine(loclationTypeForder, fileFormat);

                    }

                    if (!string.IsNullOrWhiteSpace(scssFileLoclation))
                    {
                        Dictionary<string, AppEsiteStyleObjDto> dictStyle = dictMedaiQuery[indexS];
                        PoressOneSccsFile(scssFileLoclation, dictStyle);

                    }
                }
            }
        }

        private static void PoressOneSccsFile(string fileLoclation, Dictionary<string, AppEsiteStyleObjDto> dictStyle)
        {
            string originalString = File.ReadAllText(fileLoclation);

            string endToken = "}";

            //.rating li {
            //    font - size: 20px;
            //}
            foreach (string beginToken in dictStyle.Keys)
            {
                List<string> repalceSectionList = ExtractFromBody(originalString, beginToken, endToken);


                if (repalceSectionList.Count > 0)
                {
                    string firstSection = repalceSectionList[0];
                    originalString = originalString.Replace(firstSection,
                        " {" + System.Environment.NewLine +
                              dictStyle[beginToken].CssText + System.Environment.NewLine

                        );


                }
                else // cannod find it is new added node 
                {
                    originalString = originalString + System.Environment.NewLine +

                          beginToken + " {"
                          + System.Environment.NewLine +
                               dictStyle[beginToken].CssText + System.Environment.NewLine +
                         "}";

                }

            }

            File.WriteAllText(fileLoclation, originalString);
        }

        public static AppSefolderDto RenameFolder(string orgFolderPath, string renameToFolderName, int? eSiteId)
        {

            //renameToFolderName full path

            string lastDirecoty = GetLastDirectoryName(orgFolderPath);
            string renameToFolderFullPath = orgFolderPath.Replace(lastDirecoty, renameToFolderName);

            try
            {
                AppSefolderDto aAppSefolderDto = MoveOrRenameFolder(orgFolderPath, renameToFolderFullPath, eSiteId);

                return aAppSefolderDto;


            }
            catch (Exception ex)
            {
                return null;
            }

            //  throw new NotImplementedException();


        }

        private static AppSefolderDto MoveOrRenameFolder(string orgFolderPath, string renameToFolderFullPath, int? eSiteId)
        {
            System.IO.Directory.Move(orgFolderPath, renameToFolderFullPath);

            UpdateFolderPageSitepath(orgFolderPath, eSiteId, renameToFolderFullPath);

            var aAppSefolderDto = RetrieveLocalFolderHairarchyDtoByFolderPath(renameToFolderFullPath);
            return aAppSefolderDto;
        }

        private static void UpdateFolderPageSitepath(string orgFolderPath, int? eSiteId, string renameToFolderFullPath)
        {
            Dictionary<string, AppEsitePagesDto> dictFullNamePgeDto = GetWebSiteAllPageExtenstionPage(eSiteId);

            List<AppEsitePagesDto> updatePageLit = dictFullNamePgeDto.Where(o => o.Key.Contains(orgFolderPath)).Select(o => o.Value).ToList();
            updatePageLit.ForAll(o => o.FileFullPath = o.FileFullPath.Replace(orgFolderPath, renameToFolderFullPath));

            List<AppEsitePagesEntity> updateEntityList = new List<AppEsitePagesEntity>();

            updatePageLit.ForAll(o =>
            {
                AppEsitePagesEntity updateEntity = new AppEsitePagesEntity();
                AppEsitePagesConverter.CopyDtoToEntity(updateEntity, o);
                updateEntityList.Add(updateEntity);
            }
            );
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                foreach (var updateEntity in updateEntityList)
                {
                    adapter.UpdateEntitiesDirectly(updateEntity, new RelationPredicateBucket(AppEsitePagesFields.PageId == updateEntity.PageId));
                }


            }
        }


        // Cut Paste Folder
        public static AppSefolderDto MoveFolder(string orgFolderPath, string moveToParentFolderPath, int? eSiteId)
        {
            try
            {
                return MoveOrRenameFolder(orgFolderPath, moveToParentFolderPath, eSiteId);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        // Copy Pase Folder: Copy folder strucutre to new location, with all files in this folder and sub folders
        public static bool CopyFolderToNewLocation(string orgFolderPath, string targetFolderPath)
        {
            try
            {
                foreach (string orgPath in Directory.GetDirectories(orgFolderPath, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(orgPath.Replace(orgFolderPath, targetFolderPath));
                }


                foreach (string orgFilePath in Directory.GetFiles(orgFolderPath, "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(orgFilePath, orgFilePath.Replace(orgFolderPath, targetFolderPath), true);
                }

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // ??? Not sure the parameters, List<FileInfo> fileList
        // need to use File Upload DataImage.aspx
        //public static bool UploadFilesToFolder(string folderPath, List<FileInfo> fileList)
        //{
        //    throw new NotImplementedException();
        //}

        public static bool CopyFileToNewLocation(string orgFileFullPath, string copyToParentFolderPath, int? eSiteId)
        {
            string filename = Path.GetFileName(orgFileFullPath);
            string sourcePath = Path.GetDirectoryName(orgFileFullPath);

            string detinationFilePath = Path.Combine(copyToParentFolderPath, filename);
            if (File.Exists(orgFileFullPath))
            {
                File.Copy(orgFileFullPath, detinationFilePath, true);

                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool MoveFileToNewLocation(string orgFileFullPath, string moveToParentFolderPath, int? eSiteId)
        {
            string filename = Path.GetFileName(orgFileFullPath);
            string sourcePath = Path.GetDirectoryName(orgFileFullPath);

            string detinationFilePath = Path.Combine(moveToParentFolderPath, filename);
            if (File.Exists(orgFileFullPath))
            {
                File.Move(orgFileFullPath, detinationFilePath);

                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool RenameFile(string orgFileFullPath, string newFileName, int? eSiteId)
        {
            string filename = Path.GetFileName(orgFileFullPath);
            string sourcePath = Path.GetDirectoryName(orgFileFullPath);

            string detinationFilePath = Path.Combine(sourcePath, newFileName);
            if (File.Exists(orgFileFullPath))
            {
                File.Move(orgFileFullPath, detinationFilePath);

                return true;
            }
            else
            {
                return false;
            }
        }
        public static string GetFileUrl(string fileFullPath)
        {
            throw new NotImplementedException();
        }



        public static AppSefolderDto RetrieveLocalFolderHairarchyDtoByFolderPath(string rootFolderPath, List<string> ignoredFolderPathList = null)
        {
            AppSefolderDto aAppSefolderDto = ConvertOneNodePathToFolderDto(rootFolderPath, true);

            List<AppSefolderDto> children = GetOneFolderChildrenFolder(rootFolderPath, aAppSefolderDto.UiId.ToString(), ignoredFolderPathList);

            aAppSefolderDto.Children = children.ToArray();


            return aAppSefolderDto;


            // return AppProductTreeViewBL.RetrieveFolderHairarchyDto();
        }

        public static string CreateOneFileFolder(string fileFullPath)
        {

            if (!Directory.Exists(fileFullPath))
            {
                var directory = Directory.CreateDirectory(fileFullPath);

                return fileFullPath;
            }
            else
            {
                return null;
            }


            // return appEsitePagesDto;
        }

        public static bool DeleteOneFileFolder(AppSefolderDto appSefolderDto, bool needToDeleteRelatedEsitePages = true)
        {
            string folderFullPath = appSefolderDto.FolderPath;

            try
            {
                System.IO.DirectoryInfo di = new DirectoryInfo(folderFullPath);

                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }

                if (needToDeleteRelatedEsitePages)
                {

                    var allfiles = Directory.GetFiles(folderFullPath, "*.*", SearchOption.AllDirectories);

                    AppEsiteConfigBL.DeletePageWithPathList(appSefolderDto.EsiteId, allfiles);
                }

                di.Delete(true);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }





            // return appEsitePagesDto;
        }


        public static bool ClearOneFileFolderByPath(string folderFullPath)
        {
            try
            {
                System.IO.DirectoryInfo di = new DirectoryInfo(folderFullPath);

                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }



                //var allfiles = Directory.GetFiles(folderFullPath, "*.*", SearchOption.AllDirectories);

                //AppEsiteConfigBL.DeletePageWithPathList(appSefolderDto.EsiteId, allfiles);

                //di.Delete(true);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static bool DeleteOneFileFolderByPath(string folderFullPath)
        {
            try
            {
                System.IO.DirectoryInfo di = new DirectoryInfo(folderFullPath);

                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }

                di.Delete(true);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static OperationCallResult<bool> MoveOneEsiteFileToNewLocation(AppEsitePagesDto appEsitePagesDto)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (appEsitePagesDto != null)
            {
                if (!string.IsNullOrWhiteSpace(appEsitePagesDto.FileFullPath) && !string.IsNullOrWhiteSpace(appEsitePagesDto.MoveToParentFolderPath) && appEsitePagesDto.EsiteId.HasValue)
                {




                    string orgFullPath = appEsitePagesDto.FileFullPath;

                    if (!appEsitePagesDto.MoveToParentFolderPath.EndsWith("\\"))
                    {
                        appEsitePagesDto.MoveToParentFolderPath = appEsitePagesDto.MoveToParentFolderPath + "\\";
                    }

                    string moveToParentFolderFullPath = appEsitePagesDto.MoveToParentFolderPath;

                    string newFilePath = moveToParentFolderFullPath + appEsitePagesDto.FileCode;

                    string siteBasePath = GetCompanyWebSiteBaseFolderPath(appEsitePagesDto.EsiteId.Value);
                    if (newFilePath.Contains(siteBasePath))
                    {
                        newFilePath = newFilePath.Substring(siteBasePath.Length);
                    }


                    try
                    {
                        bool isMoveFileSuccess = MoveFileToNewLocation(orgFullPath, moveToParentFolderFullPath, null);

                        if (isMoveFileSuccess)
                        {
                            if (appEsitePagesDto.Id != null)
                            {

                                AppEsitePagesEntity updateEntity = new AppEsitePagesEntity();
                                updateEntity.FileFullPath = newFilePath;

                                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                                {
                                    adapter.UpdateEntitiesDirectly(updateEntity, new RelationPredicateBucket(AppEsitePagesFields.PageId == (int)appEsitePagesDto.Id));
                                }

                                SynchronizeWebSiteRoutstatejs((int)appEsitePagesDto.EsiteId.Value);
                            }
                        }
                        else
                        {
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_movefilefailed", ValidationItemType.Error, "Move file failed."));
                        }
                    }
                    catch (Exception ex)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_movefilefailed", ValidationItemType.Error, "Move file failed. " + ex.ToString()));
                    }
                }
                else
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_movefilefailed", ValidationItemType.Error, "Move file failed."));
                }
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<object> DeleteOneWebSiteFile(AppEsitePagesDto appEsitePagesDto)
        {

            string fileNameFullPath = appEsitePagesDto.FileFullPath;




            try
            {


                if (File.Exists(fileNameFullPath))
                {
                    File.Delete(fileNameFullPath);
                }



            }
            catch (Exception Ex)
            {
                // Console.WriteLine(Ex.ToString());
            }

            return AppEsiteConfigBL.DeleteOneAppEsitePages(appEsitePagesDto.Id);


            // return appEsitePagesDto;
        }

        // if one exsitng 
        public static OperationCallResult<AppEsitePagesExDto> SaveOneWebSiteFileWithHtmlPageAtributeFile(AppEsitePagesExDto appEsitePagesDto)
        {
            OperationCallResult<AppEsitePagesExDto> aOperationCallResult = new OperationCallResult<AppEsitePagesExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            string fileNameFullPath = appEsitePagesDto.FileFullPath;

            string siteBasePath = GetCompanyWebSiteBaseFolderPath(appEsitePagesDto.EsiteId.Value);

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


                    // Check if file already exists. If yes, delete it.     
                    //if (File.Exists(fileNameFullPath))
                    //{
                    //    if (!appEsitePagesDto.IsNewFile)
                    //    {
                    //        File.Delete(fileNameFullPath);
                    //    }
                    //    else // it is a new file
                    //    {
                    //        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_FileAlreadyExists", ValidationItemType.Error, "File aready exists."));
                    //        return aOperationCallResult;
                    //    }
                    //}

                    //using (StreamWriter writer = new StreamWriter(fileNameFullPath, false))
                    //{
                    //    writer.Write(appEsitePagesDto.HtmlContent);
                    //}
                    string appSitePath = GetWebSiteBasePath(appEsitePagesDto.EsiteId);

                    // Create a new file     

                    string htmlContent = appEsitePagesDto.HtmlContent;
                    string fileExtension = string.Empty;

                    using (FileStream fs = File.Create(fileNameFullPath))
                    {
                        // Add some text to file    
                        Byte[] title = new UTF8Encoding(true).GetBytes(appEsitePagesDto.HtmlContent);
                        fs.Write(title, 0, title.Length);


                        FileInfo fileInfo = new FileInfo(fileNameFullPath);
                        fileExtension = fileInfo.Extension;

                        if (!string.IsNullOrEmpty(fileInfo.Extension) && (fileInfo.Extension.ToLower() == ".html" || fileInfo.Extension.ToLower() == ".htm"))
                        {
                            if (!appEsitePagesDto.IsComponent)
                            {
                                appEsitePagesDto.HtmlContent = "";
                                appEsitePagesDto.EmresourceContentType = (int)EmAppWebsitePageType.HTMLPage;

                                // need to remove site absolu path:
                                if (appEsitePagesDto.EsiteId.HasValue)
                                {
                                    appEsitePagesDto.FileFullPath = appEsitePagesDto.FileFullPath.Replace(siteBasePath, "");

                                }



                                OperationCallResult<AppEsitePagesExDto> result = AppEsiteConfigBL.SaveAppEsitePagesExDto(appEsitePagesDto);
                                appEsitePagesDto = result.Object;
                            }
                            else
                            {
                                SynchronizeComponentOnAllPages(appEsitePagesDto);
                            }
                        }




                        AddFileAttributesToAppEsitePagesDto(fileInfo, appEsitePagesDto);

                        if (appEsitePagesDto.FileFullPath.HasValue())
                        {
                            appEsitePagesDto.Description = appEsitePagesDto.FileFullPath.Substring(appSitePath.Length).Replace("\\", "/");

                        }

                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                        appEsitePagesDto.HtmlContent = htmlContent;
                        aOperationCallResult.Object = appEsitePagesDto;
                    }


                    if (!string.IsNullOrEmpty(fileExtension) && (fileExtension.ToLower() == ".scss"))
                    {
                        if (appEsitePagesDto.EsiteId.HasValue)
                        {

                            CompileEsiteScssToCss(appSitePath, true, true, true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_SaveAppEsitePagesExDtoWithFile_Error", ValidationItemType.Error, ex.ToString()));
                }



            }


            return aOperationCallResult;






        }


        public static OperationCallResult<bool> SaveAppEsitePageExDtoList(List<AppEsitePagesExDto> pageList)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (!pageList.IsEmpty())
            {
                foreach (AppEsitePagesExDto pageDto in pageList)
                {
                    var saveResult = SaveOneWebSiteFileWithHtmlPageAtributeFile(pageDto);

                    if (!saveResult.IsSuccessful)
                    {
                        aValidationResult.Merge(saveResult.ValidationResult);
                    }
                }

                if (!aValidationResult.HasErrors)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    aOperationCallResult.Object = true;
                }
            }

            return aOperationCallResult;
        }




        //public static OperationCallResult<AppEsitePagesExDto> SaveOneWebSitePageWithFile(AppEsitePagesExDto appEsitePagesDto)
        //{
        //    OperationCallResult<AppEsitePagesExDto> aOperationCallResult = new OperationCallResult<AppEsitePagesExDto>();
        //    ValidationResult aValidationResult = new ValidationResult();
        //    aOperationCallResult.ValidationResult = aValidationResult;


        //    return AppEsiteFileBL.SaveOneWebSiteFileWithHtmlPageAtributeFile(appEsitePagesDto);
        //}




        public static AppEsitePagesExDto GetOneWebSiteFileContent(string fileFullPath, int siteId)
        {
            AppEsitePagesExDto filePageDto = new AppEsitePagesExDto();
            FileInfo fileInfo = new FileInfo(fileFullPath);


            filePageDto.Title = fileInfo.Name;
            filePageDto.EsiteId = siteId;

            AddFileAttributesToAppEsitePagesDto(fileInfo, filePageDto);

            string text = File.ReadAllText(fileFullPath);
            filePageDto.HtmlContent = text;

            return filePageDto;
        }

        // need to remove !!!
        public static AppEsitePagesExDto RetrieveOneWebSiteFile(string fileFullPath, int siteId)
        {
            AppEsitePagesExDto filePageDto = new AppEsitePagesExDto();
            FileInfo fileInfo = new FileInfo(fileFullPath);

            string appSitePath = GetWebSiteBasePath(siteId);

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

            AddFileAttributesToAppEsitePagesDto(fileInfo, filePageDto);

            if (filePageDto.FileFullPath.HasValue())
            {
                filePageDto.Description = filePageDto.FileFullPath.Substring(appSitePath.Length).Replace("\\", "/");

            }




            string text = File.ReadAllText(fileFullPath);
            filePageDto.HtmlContent = text;

            if (fileFullPath.Contains("style\\PartialScss\\ThirdPart"))
            {
                filePageDto.ComponentName = filePageDto.Title.Split('.')[0];


                if (fileFullPath.Contains("\\Wijmo"))
                {
                    filePageDto.ComponentType = EmAppEsiteThirdPartControl.Wijmo.ToString();

                    InitThirdPartControlThemeFileParameterDictionary(filePageDto);

                }
                else if (fileFullPath.Contains("\\DayPilot"))
                {
                    filePageDto.ComponentType = EmAppEsiteThirdPartControl.DayPilot.ToString();

                    InitThirdPartControlThemeFileParameterDictionary(filePageDto);
                }
            }

            return filePageDto;
        }




        public static List<AppEsitePagesDto> RetrieveAppEsiteComponentList(int? siteId)
        {
            List<AppEsitePagesDto> componentList = new List<AppEsitePagesDto>();

            if (siteId.HasValue)
            {
                string siteBasePath = GetCompanyWebSiteBaseFolderPath(siteId.Value);

                string folderPath = siteBasePath + "\\SharedResource\\Component";

                List<AppEsitePagesDto> pageDtoList = RetrieveLocalFileInfoDtosByFolderPath(folderPath, siteId.Value, true);

                foreach (var pageDto in pageDtoList.OrderBy(o => o.FileCode))
                {
                    string filename = pageDto.FileCode;

                    var filenamePartArray = filename.Split('.')[0].Split('_');

                    if (filenamePartArray.Length >= 3)
                    {
                        pageDto.ComponentType = filenamePartArray[1];

                        pageDto.ComponentSubType = filenamePartArray[2];

                        pageDto.ComponentSubTypeId = "_" + pageDto.ComponentType + "_" + pageDto.ComponentSubType;

                        pageDto.ComponentName = "";


                        for (int i = 3; i < filenamePartArray.Length; i++)
                        {
                            pageDto.ComponentName += filenamePartArray[i] + " ";
                        }

                        pageDto.ComponentName = pageDto.ComponentName.Trim();

                        componentList.Add(pageDto);
                    }
                }
            }


            return componentList;
        }

        public static List<AppSefolderDto> RetrieveAppEsiteComponetFolders(int? siteId)
        {
            List<AppSefolderDto> allFolders = new List<AppSefolderDto>();
            var uiFolder = new AppSefolderDto() { Id = 1, Name = "Regular UI Component", IsFolderReadonly = true, Description = "UI", };
            var dataModelFolder = new AppSefolderDto() { Id = 2, Name = "Data Model Component", IsFolderReadonly = true, Description = "DataModel", };
            var searchViewFolder = new AppSefolderDto() { Id = 3, Name = "Search View Component", IsFolderReadonly = true, Description = "SearchView", };

            allFolders.Add(uiFolder);
            allFolders.Add(dataModelFolder);
            allFolders.Add(searchViewFolder);


            Dictionary<string, AppSefolderDto> dictFolderKeyAndDto = allFolders.ToDictionary(o => o.Description, o => o);

            List<AppEsitePagesDto> componentList = RetrieveAppEsiteComponentList(siteId);

            int newFolderId = allFolders.Count;

            foreach (var aComponent in componentList)
            {
                AppSefolderDto compTypeFolderDto = null;

                if (!dictFolderKeyAndDto.ContainsKey(aComponent.ComponentType))
                {

                    newFolderId++;

                    compTypeFolderDto = new AppSefolderDto();
                    compTypeFolderDto.Id = newFolderId;
                    compTypeFolderDto.Name = aComponent.ComponentType;
                    compTypeFolderDto.Description = aComponent.ComponentType;
                    compTypeFolderDto.IsFolderReadonly = true;

                    if (aComponent.ComponentType.StartsWith("FormField") || aComponent.ComponentType.StartsWith("FormGrid"))
                    {
                        compTypeFolderDto.ParentId = (int)dataModelFolder.Id;
                    }
                    else if (aComponent.ComponentType.StartsWith("Search"))
                    {
                        compTypeFolderDto.ParentId = (int)searchViewFolder.Id;
                    }

                    allFolders.Add(compTypeFolderDto);
                    dictFolderKeyAndDto.Add(aComponent.ComponentType, compTypeFolderDto);


                }
                else
                {
                    compTypeFolderDto = dictFolderKeyAndDto[aComponent.ComponentType];
                }

                AppSefolderDto subtypeFolderDto = null;
                if (!dictFolderKeyAndDto.ContainsKey(aComponent.ComponentSubTypeId))
                {
                    newFolderId++;
                    subtypeFolderDto = new AppSefolderDto();
                    subtypeFolderDto.Id = newFolderId;
                    subtypeFolderDto.Name = aComponent.ComponentSubType;
                    subtypeFolderDto.Description = aComponent.ComponentSubTypeId;
                    subtypeFolderDto.ParentId = (int)compTypeFolderDto.Id;
                    allFolders.Add(subtypeFolderDto);
                    dictFolderKeyAndDto.Add(aComponent.ComponentSubTypeId, subtypeFolderDto);
                }
                else
                {
                    subtypeFolderDto = dictFolderKeyAndDto[aComponent.ComponentSubTypeId];
                }

                newFolderId++;
                AppSefolderDto componentFolderItem = new AppSefolderDto();
                componentFolderItem = new AppSefolderDto();
                componentFolderItem.Id = newFolderId;
                componentFolderItem.Name = aComponent.ComponentName;
                componentFolderItem.Description = aComponent.FileCode;
                componentFolderItem.ParentId = (int)subtypeFolderDto.Id;
                componentFolderItem.IsFolderReadonly = true;
                componentFolderItem.FolderType = 2;
                allFolders.Add(componentFolderItem);
            }

            List<AppSefolderDto> rootFolders = allFolders.Where(o => !o.ParentId.HasValue).ToList();

            foreach (var rootFolder in rootFolders)
            {
                rootFolder.FolderPath = rootFolder.Name + "/";
                AppSeFolderBL.ProcessChilds(allFolders, rootFolder);
            }

            return rootFolders;
        }


        public static List<AppEsitePagesDto> RetrieveAppEsiteThirdPartControlThemeList(int? siteId, EmAppEsiteThirdPartControl? thirdPartControlType)
        {
            List<AppEsitePagesDto> themeList = new List<AppEsitePagesDto>();

            if (siteId.HasValue && thirdPartControlType.HasValue)
            {
                string siteBasePath = GetCompanyWebSiteBaseFolderPath(siteId.Value);

                string folderPath = "";
                string themeType = "";

                if (thirdPartControlType.Value == EmAppEsiteThirdPartControl.Wijmo)
                {
                    themeType = EmAppEsiteThirdPartControl.Wijmo.ToString();
                    folderPath = siteBasePath + "\\SharedResource\\style\\PartialScss\\ThirdPart\\Wijmo";
                }
                else if (thirdPartControlType.Value == EmAppEsiteThirdPartControl.DayPilot)
                {
                    folderPath = siteBasePath + "\\SharedResource\\style\\PartialScss\\ThirdPart\\DayPilot";
                    themeType = EmAppEsiteThirdPartControl.DayPilot.ToString();
                }

                if (!string.IsNullOrWhiteSpace(folderPath))
                {
                    List<AppEsitePagesDto> pageDtoList = RetrieveLocalFileInfoDtosByFolderPath(folderPath, siteId.Value, true);

                    foreach (var pageDto in pageDtoList.OrderBy(o => o.FileCode))
                    {
                        string filename = pageDto.FileCode;

                        pageDto.ComponentName = filename.Split('.')[0];
                        pageDto.ComponentType = themeType;


                        themeList.Add(pageDto);
                    }
                }
            }


            return themeList;
        }


        public static OperationCallResult<AppEsitePagesDto> CreateNewAppEsiteThirdPartControlTheme(AppEsitePagesDto themePageDto)
        {
            OperationCallResult<AppEsitePagesDto> aOperationCallResult = new OperationCallResult<AppEsitePagesDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            int? eSiteId = themePageDto.EsiteId;
            string themeType = themePageDto.ComponentType;
            string themeName = themePageDto.Title;

            if (eSiteId.HasValue && !string.IsNullOrWhiteSpace(themeType) && !string.IsNullOrWhiteSpace(themeName))
            {
                string srcFileCode = "";
                string newFileCode = "";


                if (themeType == EmAppEsiteThirdPartControl.Wijmo.ToString())
                {
                    srcFileCode = "_WJ_Theme_Default.scss";
                    newFileCode = "_WJ_Theme_" + themeName + ".scss";
                }
                else if (themeType == EmAppEsiteThirdPartControl.DayPilot.ToString())
                {
                    srcFileCode = "_DP_Theme_Default.scss";
                    newFileCode = "_DP_Theme_" + themeName + ".scss";
                }

                if (!string.IsNullOrWhiteSpace(newFileCode))
                {
                    string appSitePath = GetWebSiteBasePath(eSiteId);

                    string srcFilePath = string.Format(@"SharedResource\style\PartialScss\ThirdPart\{0}\{1}", themeType, srcFileCode);
                    string newFilePath = string.Format(@"SharedResource\style\PartialScss\ThirdPart\{0}\{1}", themeType, newFileCode);

                    string srcFileFullPath = Path.Combine(appSitePath, srcFilePath);
                    string newFileFullPath = Path.Combine(appSitePath, newFilePath);

                    try
                    {
                        System.IO.File.Copy(srcFileFullPath, newFileFullPath);
                        GenerateThirdPartControlThemeIndexFile(eSiteId, themeType);
                        CompileEsiteScssToCss(appSitePath, true, true, false);

                        aOperationCallResult.Object = RetrieveOneWebSiteFile(newFileFullPath, eSiteId.Value);

                    }
                    catch (Exception ex)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_GenerateScssFile_Error", ValidationItemType.Warning, "Generate Scss File Error: " + ex.ToString()));
                    }

                }

            }

            if (aOperationCallResult.Object == null)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_GenerateScssFile_Error", ValidationItemType.Warning, "Create Theme Failed."));
            }


            return aOperationCallResult;
        }

        private static void GenerateThirdPartControlThemeIndexFile(int? eSiteId, string themeType)
        {
            if (eSiteId.HasValue && !string.IsNullOrWhiteSpace(themeType))
            {
                string appSitePath = GetWebSiteBasePath(eSiteId);

                string filePath = string.Format(@"SharedResource\style\PartialScss\ThirdPart\_{0}Themes.scss", themeType);
                string fileFullPath = Path.Combine(appSitePath, filePath);

                string childFolderPath = string.Format(@"SharedResource\style\PartialScss\ThirdPart\{0}\", themeType);
                string childFoldeFullPath = Path.Combine(appSitePath, childFolderPath);

                string[] files = System.IO.Directory.GetFiles(childFoldeFullPath);

                string defaultThemeName = "";

                if (themeType == EmAppEsiteThirdPartControl.Wijmo.ToString())
                {
                    defaultThemeName = "WJ_Theme_Default";
                }
                else if (themeType == EmAppEsiteThirdPartControl.DayPilot.ToString())
                {
                    defaultThemeName = "DP_Theme_Default";
                }

                //string fileContent = "@import \"" + themeType + "/" + defaultThemeName + "\";" + System.Environment.NewLine;

                string fileContent = ".AppSite" + " {" + System.Environment.NewLine
                                    + "    @import \"" + themeType + "/" + defaultThemeName + "\";" + System.Environment.NewLine
                                    + "}" + System.Environment.NewLine;

                foreach (string s in files)
                {
                    string extension = Path.GetExtension(s);
                    string fileName = Path.GetFileNameWithoutExtension(s);

                    if (extension.ToLower() == ".scss" && fileName.StartsWith("_"))
                    {
                        string themeName = fileName.Substring(1);
                        if (themeName != defaultThemeName)
                        {
                            fileContent += "." + themeName + " {" + System.Environment.NewLine
                                    + "    @import \"" + themeType + "/" + themeName + "\";" + System.Environment.NewLine
                                    + "}" + System.Environment.NewLine;
                        }
                    }

                }

                File.WriteAllText(fileFullPath, fileContent);
            }
        }


        public static OperationCallResult<bool> SaveOneAppEsiteThirdPartControlTheme(AppEsitePagesDto themePageDto)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            int? eSiteId = themePageDto.EsiteId;
            string themeType = themePageDto.ComponentType;

            if (eSiteId.HasValue && !string.IsNullOrWhiteSpace(themeType)
                && themePageDto.PageAttribute != null && themePageDto.PageAttribute.DictThemeParameterAndValue != null)
            {
                string appSitePath = GetWebSiteBasePath(eSiteId);

                string filePath = string.Format(@"SharedResource\style\PartialScss\ThirdPart\{0}\{1}", themeType, themePageDto.FileCode);

                string fileFullPath = Path.Combine(appSitePath, filePath);


                string originalString = File.ReadAllText(fileFullPath);

                List<string> repalceSectionList = ExtractFromBody(originalString, @"/* Start Of Parameters */", @"/* End Of Parameters */");

                try
                {
                    if (repalceSectionList.Count > 0)
                    {
                        string firstSection = repalceSectionList[0];

                        string parameterSectionText = "";
                        Dictionary<string, string> dictThemeParameterAndValue = themePageDto.PageAttribute.DictThemeParameterAndValue;

                        foreach (string key in dictThemeParameterAndValue.Keys)
                        {
                            string value = dictThemeParameterAndValue[key];

                            if (string.IsNullOrWhiteSpace(value))
                            {
                                value = "inherit";
                            }

                            parameterSectionText += string.Format(@"${0}: {1};", key, value) + System.Environment.NewLine;
                        }

                        parameterSectionText = System.Environment.NewLine + parameterSectionText + System.Environment.NewLine;

                        originalString = originalString.Replace(firstSection, parameterSectionText);

                        File.WriteAllText(fileFullPath, originalString);
                        CompileEsiteScssToCss(appSitePath, true, true, false);

                        aOperationCallResult.Object = true;
                    }
                    else
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_GenerateScssFile_Error", ValidationItemType.Warning, "Generate Scss File Error"));
                    }

                }
                catch (Exception ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_GenerateScssFile_Error", ValidationItemType.Warning, "Generate Scss File Error: " + ex.ToString()));
                }



            }


            return aOperationCallResult;
        }



        public static List<AppEsitePagesDto> RetrieveLocalFileInfoDtosByFolderPath(string folderPath, int? siteId, bool isRetrieveFileContent = false)
        {
            List<AppEsitePagesDto> appFileDtoList = new List<AppEsitePagesDto>();

            DirectoryInfo d = new DirectoryInfo(folderPath);//Assuming Test is your Folder
            FileInfo[] files = d.GetFiles(); //Getting Text files

            // Dictionary<string,FileInfo> dictFullNameFileInfo = files.ToDictionary(o => o.FullName, o => o);

            Dictionary<string, AppEsitePagesDto> dictFullNamePgeDto = GetWebSiteAllPageExtenstionPage(siteId);

            foreach (FileInfo fileInfo in files)
            {
                AppEsitePagesDto pageDto = new AppEsitePagesDto();
                if (dictFullNamePgeDto.ContainsKey(fileInfo.FullName))
                {
                    pageDto = dictFullNamePgeDto[fileInfo.FullName];
                }

                AddFileAttributesToAppEsitePagesDto(fileInfo, pageDto);

                if (isRetrieveFileContent)
                {
                    string text = File.ReadAllText(pageDto.FileFullPath);
                    pageDto.HtmlContent = text;
                }


                appFileDtoList.Add(pageDto);
            }

            return appFileDtoList;
        }

        private static Dictionary<string, AppEsitePagesDto> GetWebSiteAllPageExtenstionPage(int? siteId)
        {
            // RetrieveLocalFileInfoDtosByFolderDto
            if (!siteId.HasValue)
            {



            }
            string siteBasePath = GetCompanyWebSiteBaseFolderPath(siteId.Value);

            Dictionary<string, AppEsitePagesDto> dictFullNamePgeDto = new Dictionary<string, AppEsitePagesDto>();
            if (siteId.HasValue)
            {
                List<AppEsitePagesDto> pageDtoList = AppEsiteConfigBL.RetrieveFileExtentPageDtoList(siteId);

                foreach (var o in pageDtoList)
                {
                    o.EsiteId = siteId;
                    // not include the path, need to add the path to match file full path
                    if (o.FileFullPath.IndexOf(siteBasePath) == -1)
                    {
                        o.FileFullPath = siteBasePath + o.FileFullPath;
                    }

                    dictFullNamePgeDto[o.FileFullPath] = o;

                }


            }

            return dictFullNamePgeDto;
        }



        internal static string GetCompanyWebSiteBaseFolderPath(int siteId)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string companyId = ServerContext.Instance.CurrentCompanyId.ToString();
            string rootFolderPath = string.Format(@"{0}FileRepository\Company_{1}\WebSite\Site_{2}\", baseDirectory, companyId, siteId);

            return rootFolderPath;
        }

        public static string GetCompanyTempPath()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string companyId = ServerContext.Instance.CurrentCompanyId.ToString();
            string rootFolderPath = string.Format(@"{0}FileRepository\Company_{1}\temp\", baseDirectory, companyId);

            return rootFolderPath;
        }

        private static string GetWebSiteTemplateBaseFolderPath(int eSiteId)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string rootFolderPath = string.Format(@"{0}FileRepDevWebsiteTemplate\Site_{1}\", baseDirectory, eSiteId);

            return rootFolderPath;
        }
        internal static AppEsitePagesDto AddFileAttributesToAppEsitePagesDto(FileInfo fileInfo, AppEsitePagesDto pageDto)
        {

            pageDto.FileCode = fileInfo.Name;
            pageDto.Description = fileInfo.FullName;
            pageDto.FileFullPath = fileInfo.FullName;
            pageDto.Extension = fileInfo.Extension;
            pageDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(fileInfo.CreationTimeUtc);
            pageDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(fileInfo.LastWriteTimeUtc);



            return pageDto;
        }

        private static List<AppSefolderDto> GetOneFolderChildrenFolder(string rootFolderPath, string parentUiId, List<string> ignoredFolderPathList = null)
        {
            List<AppSefolderDto> children = new List<AppSefolderDto>();
            // var directories = Directory.GetDirectories(rootFolderPath);

            if (Directory.Exists(rootFolderPath))
            {
                var directorieInfoList = new DirectoryInfo(rootFolderPath).GetDirectories().Where(x => (x.Attributes & FileAttributes.Hidden) == 0);

                foreach (var directoryInfo in directorieInfoList)
                {
                    bool isIgnored = ignoredFolderPathList != null && ignoredFolderPathList.Count > 0 
                        && ignoredFolderPathList.FirstOrDefault(o => directoryInfo.FullName.Contains(o)) != null;

                    if (!isIgnored)
                    {
                        AppSefolderDto achildSefolderDto = ConvertOneNodePathToFolderDto(directoryInfo.FullName, true);
                        achildSefolderDto.ParentUiId = parentUiId;
                        children.Add(achildSefolderDto);

                        List<AppSefolderDto> granchildren = GetOneFolderChildrenFolder(directoryInfo.FullName, achildSefolderDto.UiId.ToString());

                        achildSefolderDto.Children = granchildren.ToArray();
                    }

                }
            }



            return children;
        }

        private static AppSefolderDto ConvertOneNodePathToFolderDto(string rootFolderPath, bool needToCountContent = false)
        {
            AppSefolderDto aAppSefolderDto = new AppSefolderDto();
            aAppSefolderDto.FolderPath = rootFolderPath;
            aAppSefolderDto.FolderType = (int)EmAppTransBusinessType.LocalFile;

            string lastDriecNameName = GetLastDirectoryName(rootFolderPath);
            aAppSefolderDto.Name = lastDriecNameName;
            aAppSefolderDto.UiId = ExtensionMethodhelper.RandomId();

            if (needToCountContent)
            {

                DirectoryInfo d = new DirectoryInfo(aAppSefolderDto.FolderPath);
                aAppSefolderDto.CountContent = d.GetFiles().Length;
            }

            return aAppSefolderDto;
        }



        private static string GetLastDirectoryName(string rootFolderPath)
        {
            string afterTrim = rootFolderPath.Trim();
            if (afterTrim[afterTrim.Length - 1] == '\\')
            {
                afterTrim = afterTrim.Remove(afterTrim.Length - 1);
            }
            string rootFoderName = afterTrim.Substring(afterTrim.LastIndexOf('\\') + 1);
            return rootFoderName;
        }



        internal static bool UpdaePageRouteStateJsAndHomePageLink(AppEsitePagesExDto aAppEsitePagesExDto)
        {
            int eSiteId = aAppEsitePagesExDto.EsiteId.Value;
            try
            {

                SynchronizeWebSiteRoutstatejs(eSiteId);

                SynchronizeSupplierOrCustomerHomePage(aAppEsitePagesExDto);

                return true;

            }
            catch
            {
                return false;
            }


        }

        public static bool SynchronizeSupplierOrCustomerHomePage(AppEsitePagesExDto aAppEsitePagesExDto)
        {
            // eSiteId = aAppEsitePagesExDto.EsiteId.Value;
            string pageFullpath = aAppEsitePagesExDto.FileFullPath;

            try
            {


                string appSitePath = GetWebSiteBasePath(aAppEsitePagesExDto.EsiteId);
                if (pageFullpath.IndexOf(appSitePath) == -1)
                {
                    pageFullpath = appSitePath + pageFullpath;

                }

                var fileInfo = new FileInfo(pageFullpath);

                string customerOrSupplierhomePageFullPath = Path.Combine(fileInfo.Directory.FullName, @"HomePage.html");


                int? paramterId = (aAppEsitePagesExDto.TransactionId.HasValue ? aAppEsitePagesExDto.TransactionId : aAppEsitePagesExDto.SearchId);

                if (paramterId.HasValue)
                {
                    AppendOneLinkPage(aAppEsitePagesExDto, customerOrSupplierhomePageFullPath);
                }


                return true;

            }
            catch (Exception ex)
            {
                return false;
            }




            //return AppEsiteFileBL.RetrieveAppEsiteLocalFolderHairarchyDto(tempalteSiteId);
        }

        public static List<AppEsiteUserDefinedJsFunctionDto> GetAlUserDefinedJsFunctionDtoList(int? esiteId)
        {
            List<AppEsiteUserDefinedJsFunctionDto> toReturn = new List<AppEsiteUserDefinedJsFunctionDto>();

            AppEsiteEntity aAppEsiteEntity = AppEsiteConfigBL.RetrieveOneAppEsiteEntity(esiteId);
            AppEsiteExDto aAppEsiteDto = AppEsiteConverter.ConvertEntityToExDto(aAppEsiteEntity);

            if (aAppEsiteDto != null && aAppEsiteDto.EsiteAttribute != null && aAppEsiteDto.EsiteAttribute.UserDefinedJsFunctionList != null)
            {
                toReturn = aAppEsiteDto.EsiteAttribute.UserDefinedJsFunctionList;
            }

            return toReturn;
        }

        public static AppEsiteUserDefinedJsFunctionDto GetOneUserDefinedJsFunctionDto(int? esiteId, string functionName)
        {
            AppEsiteEntity aAppEsiteEntity = AppEsiteConfigBL.RetrieveOneAppEsiteEntity(esiteId);
            AppEsiteExDto aAppEsiteDto = AppEsiteConverter.ConvertEntityToExDto(aAppEsiteEntity);

            if (aAppEsiteDto != null && aAppEsiteDto.EsiteAttribute != null && aAppEsiteDto.EsiteAttribute.UserDefinedJsFunctionList != null)
            {
                AppEsiteUserDefinedJsFunctionDto functionDto = aAppEsiteDto.EsiteAttribute.UserDefinedJsFunctionList.FirstOrDefault(o => o.FunctionName == functionName);

                return functionDto;
            }

            return null;
        }

        public static OperationCallResult<AppEsiteUserDefinedJsFunctionDto> SaveOneUserDefinedJsFunctionDto(AppEsiteUserDefinedJsFunctionDto functionDto)
        {
            OperationCallResult<AppEsiteUserDefinedJsFunctionDto> toReturn = new OperationCallResult<AppEsiteUserDefinedJsFunctionDto>();
            ValidationResult validationResult = new ValidationResult();
            toReturn.ValidationResult = validationResult;

            if (string.IsNullOrWhiteSpace(functionDto.FunctionName))
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_SaveAppEsitePagesExDtoWithFile_Error", ValidationItemType.Error, "Function name is empty."));
            }

            if (validationResult.IsValid)
            {
                AppEsiteExDto siteDto = AppEsiteConfigBL.RetrieveOneAppEsiteExDto(functionDto.EsiteId);

                var orgFunctionDto = siteDto.EsiteAttribute.UserDefinedJsFunctionList.FirstOrDefault(o => o.FunctionName == functionDto.FunctionName);

                if (functionDto.IsNewFunction)
                {
                    if (orgFunctionDto == null)
                    {
                        siteDto.EsiteAttribute.UserDefinedJsFunctionList.Add(functionDto);
                        var saveSiteReslt = AppEsiteConfigBL.SaveAppEsiteExDto(siteDto);
                        validationResult.Merge(saveSiteReslt.ValidationResult);
                    }
                    else
                    {
                        validationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_SaveAppEsitePagesExDtoWithFile_Error", ValidationItemType.Error, "Function name already exists."));
                    }
                }
                else
                {
                    if (orgFunctionDto != null)
                    {
                        siteDto.EsiteAttribute.UserDefinedJsFunctionList.Remove(orgFunctionDto);
                    }

                    siteDto.EsiteAttribute.UserDefinedJsFunctionList.Add(functionDto);
                    var saveSiteReslt = AppEsiteConfigBL.SaveAppEsiteExDto(siteDto);
                    validationResult.Merge(saveSiteReslt.ValidationResult);

                }

                if (validationResult.IsValid)
                {


                    UpdateAppWebsiteOneUserDefinedJsFunctionCode(functionDto);

                    toReturn.Object = GetOneUserDefinedJsFunctionDto(functionDto.EsiteId, functionDto.FunctionName);
                }
            }

            return toReturn;
        }

        public static AppEsiteUserDefinedJsFunctionDto GetAppWebsiteAllUserDefinedJsCode(int? esiteId)
        {

            string websiteBAseFoder = GetCompanyWebSiteBaseFolderPath(esiteId.Value);

            string navigationCtrlJsFullPath = Path.Combine(websiteBAseFoder, @"SharedResource\js\appWebsiteGenericNavigationCtrl.js");


            string beginToken = @"/***Begin Of User Defined Code***/";
            string endToken = @"/***End Of User Defined Code***/";

            string originalString = File.ReadAllText(navigationCtrlJsFullPath);
            List<string> repalceSectionList = ExtractFromBody(originalString, beginToken, endToken);
            if (repalceSectionList.Count > 0)
            {
                string firstSection = repalceSectionList[0];

                AppEsiteUserDefinedJsFunctionDto toReturn = new AppEsiteUserDefinedJsFunctionDto();
                toReturn.EsiteId = esiteId;
                toReturn.HtmlContent = firstSection;
                return toReturn;
            }

            return null;

        }






        public static AppEsiteUserDefinedJsFunctionDto GetAppWebsiteOneUserDefinedJsFunction(int? esiteId, string functionName)
        {

            string websiteBAseFoder = GetCompanyWebSiteBaseFolderPath(esiteId.Value);

            string navigationCtrlJsFullPath = Path.Combine(websiteBAseFoder, @"SharedResource\js\appWebsiteGenericNavigationCtrl.js");


            string beginToken = @"/***Begin Of User Defined Function: " + functionName + " ***/";
            string endToken = @"/***End Of User Defined Function: " + functionName + " ***/";

            string originalString = File.ReadAllText(navigationCtrlJsFullPath);
            List<string> repalceSectionList = ExtractFromBody(originalString, beginToken, endToken);
            if (repalceSectionList.Count > 0)
            {
                string jsText = repalceSectionList[0];

                //string paramEndToken = @"Parameters***/";

                //int indexParameterEnd = jsText.IndexOf(paramEndToken);

                //if (indexParameterEnd >= 0)
                //{
                //    jsText = jsText.Substring(indexParameterEnd + paramEndToken.Length);
                //}

                jsText = jsText.TrimStart();

                AppEsiteUserDefinedJsFunctionDto toReturn = new AppEsiteUserDefinedJsFunctionDto();
                toReturn.EsiteId = esiteId;
                toReturn.FunctionName = functionName;
                toReturn.HtmlContent = jsText;
                return toReturn;
            }

            return null;

        }

        public static OperationCallResult<bool> UpdateAppWebsiteAllUserDefinedJsCode(AppEsiteUserDefinedJsFunctionDto udFunctionDto)
        {
            OperationCallResult<bool> toReturn = new OperationCallResult<bool>();
            ValidationResult validationResult = new ValidationResult();
            toReturn.ValidationResult = validationResult;

            try
            {

                string websiteBAseFoder = GetCompanyWebSiteBaseFolderPath(udFunctionDto.EsiteId.Value);

                string navigationCtrlJsFullPath = Path.Combine(websiteBAseFoder, @"SharedResource\js\appWebsiteGenericNavigationCtrl.js");



                string beginToken = @"/***Begin Of User Defined Code***/";
                string endToken = @"/***End Of User Defined Code***/";

                string originalString = File.ReadAllText(navigationCtrlJsFullPath);
                List<string> repalceSectionList = ExtractFromBody(originalString, beginToken, endToken);
                if (repalceSectionList.Count > 0)
                {
                    string firstSection = repalceSectionList[0];

                    originalString = originalString.Replace(firstSection, udFunctionDto.HtmlContent);
                    File.WriteAllText(navigationCtrlJsFullPath, originalString);

                    validationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_Save_OK", ValidationItemType.Message, "Save Successful"));
                    toReturn.Object = true;
                }


            }
            catch (Exception ex)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_SaveAppEsitePagesExDtoWithFile_Error", ValidationItemType.Error, ex.ToString()));
                toReturn.Object = false;
            }

            return toReturn;
        }

        public static OperationCallResult<bool> UpdateAppWebsiteOneUserDefinedJsFunctionCode(AppEsiteUserDefinedJsFunctionDto udFunctionDto)
        {
            OperationCallResult<bool> toReturn = new OperationCallResult<bool>();
            ValidationResult validationResult = new ValidationResult();
            toReturn.ValidationResult = validationResult;

            try
            {

                string websiteBAseFoder = GetCompanyWebSiteBaseFolderPath(udFunctionDto.EsiteId.Value);

                string navigationCtrlJsFullPath = Path.Combine(websiteBAseFoder, @"SharedResource\js\appWebsiteGenericNavigationCtrl.js");
                string originalString = File.ReadAllText(navigationCtrlJsFullPath);


                string beginToken = @"/***Begin Of User Defined Function: " + udFunctionDto.FunctionName + " ***/";
                string endToken = @"/***End Of User Defined Function: " + udFunctionDto.FunctionName + " ***/";
                List<string> repalceSectionList = ExtractFromBody(originalString, beginToken, endToken);

                if (string.IsNullOrWhiteSpace(udFunctionDto.HtmlContent))
                {
                    string paramString = "";

                    foreach (string paramName in udFunctionDto.DictParameterNameAndType.Keys)
                    {
                        paramString += ", " + paramName;
                    }

                    if (paramString.StartsWith(", "))
                    {
                        paramString = paramString.Substring(2);
                    }

                    udFunctionDto.HtmlContent = "$scope." + udFunctionDto.FunctionName + " = function(" + paramString + ") {" + Environment.NewLine
                         + Environment.NewLine
                         + "}" + Environment.NewLine;

                }

                string functionCode = udFunctionDto.HtmlContent;

                if (repalceSectionList.Count > 0)
                {
                    string jsText = repalceSectionList[0];

                    //string paramEndToken = @"Parameters***/";

                    //int indexParameterEnd = jsText.IndexOf(paramEndToken);

                    //if (indexParameterEnd >= 0)
                    //{
                    //    jsText = jsText.Substring(indexParameterEnd + paramEndToken.Length);
                    //}

                    jsText = jsText.Trim();
                    udFunctionDto.HtmlContent = udFunctionDto.HtmlContent.Trim();

                    originalString = originalString.Replace(jsText, udFunctionDto.HtmlContent);
                    File.WriteAllText(navigationCtrlJsFullPath, originalString);

                    validationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_Save_OK", ValidationItemType.Message, "Save Successful"));
                    toReturn.Object = true;
                }
                else
                {

                    string beginToken_AllUdCode = @"/***Begin Of User Defined Code***/";
                    string endToken_AllUdCode = @"/***End Of User Defined Code***/";
                    List<string> repalceSectionList_AllUdCode = ExtractFromBody(originalString, beginToken_AllUdCode, endToken_AllUdCode);

                    if (repalceSectionList_AllUdCode.Count > 0)
                    {
                        string firstSection = repalceSectionList_AllUdCode[0];

                        int endTokeIndex = originalString.IndexOf(endToken_AllUdCode);

                        functionCode = beginToken + Environment.NewLine
                            + functionCode + Environment.NewLine
                            + endToken + Environment.NewLine;

                        originalString = originalString.Insert(endTokeIndex, functionCode);

                        File.WriteAllText(navigationCtrlJsFullPath, originalString);

                        validationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_Save_OK", ValidationItemType.Message, "Publish Successful"));
                        toReturn.Object = true;
                    }
                    else
                    {
                        validationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_SaveAppEsitePagesExDtoWithFile_Error", ValidationItemType.Error, "Cannot find user defined javascript code placeholder area."));
                    }


                }




            }
            catch (Exception ex)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppEsitePagesEntity_SaveAppEsitePagesExDtoWithFile_Error", ValidationItemType.Error, ex.ToString()));
                toReturn.Object = false;
            }

            return toReturn;
        }















        public static string UpdateAppWebsiteGenericNavigationCtrlJs(AppEsitePageNavigationDto aAppEsitePageNavigationDto)
        {
            string returncallfunctionEXpress = "";

            string websiteBAseFoder = GetCompanyWebSiteBaseFolderPath(aAppEsitePageNavigationDto.SiteId.Value);

            string navigationCtrlJsFullPath = Path.Combine(websiteBAseFoder, @"SharedResource\js\appWebsiteGenericNavigationCtrl.js");



            string beginToken = "/***BeginCustomerizedAutogenUserCode***/";
            string endToken = @"/***EndCustomerizedAutogenUserCode***/";

            string originalString = File.ReadAllText(navigationCtrlJsFullPath);
            List<string> repalceSectionList = ExtractFromBody(originalString, beginToken, endToken);
            if (repalceSectionList.Count > 0)
            {
                string firstSection = repalceSectionList[0];


                var fromPage = AppEsiteConfigBL.RetrieveOneAppEsitePagesExDto(aAppEsitePageNavigationDto.FromPageId);
                var toPage = AppEsiteConfigBL.RetrieveOneAppEsitePagesExDto(aAppEsitePageNavigationDto.ToPageId);

                string param = "";
                if (!aAppEsitePageNavigationDto.ParamList.IsEmpty())
                {
                    param = aAppEsitePageNavigationDto.ParamList.Aggregate((i, j) => i + "," + j);
                }

                string functionName = string.Format("navigationScope.linkFrom{0}To{1}", fromPage.MetaDesciption, toPage.MetaDesciption);

                returncallfunctionEXpress = string.Format(@"{0}({1})", functionName, param);

                string linkFromToFunction = "";

                string functionAssignRightSide = "";
                if (aAppEsitePageNavigationDto.isPopup)
                {
                    functionAssignRightSide = string.Format(@"=function(id, param1, param2, callbackFunc){{
	               $scope.openPageByRouteCodeOnPopup('{0}', id, param1, param2, callbackFunc);",
                                      toPage.MetaDesciption
                                );
                }
                else
                {
                    functionAssignRightSide = string.Format(@"=function(id, param1, param2){{
	                $scope.goToPageByRouteCode('{0}', id, param1, param2);",
                                      toPage.MetaDesciption
                                );
                }

                linkFromToFunction = string.Format(@"            $scope.{0}{1} }}", functionName, functionAssignRightSide);


                if (!firstSection.Contains(functionName))
                {
                    string appendString = System.Environment.NewLine + linkFromToFunction + System.Environment.NewLine;

                    int endTokeInde = originalString.IndexOf(endToken);
                    //originalString = originalString.Replace(firstSection, firstSection + appendString);

                    originalString = originalString.Insert(endTokeInde, appendString);

                    File.WriteAllText(navigationCtrlJsFullPath, originalString);
                }
                else // need to update function
                {
                    string beginTokenFunc = functionName;
                    string endTokenFunc = @"}";


                    List<string> repalceFuncSectionList = ExtractFromBody(firstSection, beginTokenFunc, endTokenFunc);
                    if (repalceFuncSectionList.Count > 0)
                    {
                        string oldfunctionpart = repalceFuncSectionList[0];
                        string newFunctionPart = functionAssignRightSide;


                        originalString = originalString.Replace(oldfunctionpart, newFunctionPart);
                        File.WriteAllText(navigationCtrlJsFullPath, originalString);

                    }



                }




            }

            return returncallfunctionEXpress;

            // throw new NotImplementedException();
        }



        public static ValidationResult GenerateMediaQueryFiles(AppEsiteExDto aAppEsiteExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            if (aAppEsiteExDto.LayoutSetting != null && aAppEsiteExDto.LayoutSetting.IsModified)
            {
                List<int> breakWidthList = new List<int>();

                if (aAppEsiteExDto.LayoutSetting.BreakWidth1.HasValue && aAppEsiteExDto.LayoutSetting.BreakWidth1.Value > 0)
                {
                    breakWidthList.Add(aAppEsiteExDto.LayoutSetting.BreakWidth1.Value);
                }

                if (aAppEsiteExDto.LayoutSetting.BreakWidth2.HasValue && aAppEsiteExDto.LayoutSetting.BreakWidth2.Value > 0)
                {
                    breakWidthList.Add(aAppEsiteExDto.LayoutSetting.BreakWidth2.Value);
                }

                if (aAppEsiteExDto.LayoutSetting.BreakWidth3.HasValue && aAppEsiteExDto.LayoutSetting.BreakWidth3.Value > 0)
                {
                    breakWidthList.Add(aAppEsiteExDto.LayoutSetting.BreakWidth3.Value);
                }

                if (aAppEsiteExDto.LayoutSetting.BreakWidth4.HasValue && aAppEsiteExDto.LayoutSetting.BreakWidth4.Value > 0)
                {
                    breakWidthList.Add(aAppEsiteExDto.LayoutSetting.BreakWidth4.Value);
                }

                if (aAppEsiteExDto.LayoutSetting.BreakWidth5.HasValue && aAppEsiteExDto.LayoutSetting.BreakWidth5.Value > 0)
                {
                    breakWidthList.Add(aAppEsiteExDto.LayoutSetting.BreakWidth5.Value);
                }

                breakWidthList = breakWidthList.Distinct().OrderBy(o => o).ToList();

                int totalWidthRanges = aAppEsiteExDto.LayoutSetting.TotalWidthRanges = Math.Min(breakWidthList.Count + 1, aAppEsiteExDto.LayoutSetting.TotalWidthRanges);




                string websiteBAseFoder = AppEsiteFileBL.GetWebSiteBasePath((int)aAppEsiteExDto.Id);

                string mediaQueryScssFileFullPath = Path.Combine(websiteBAseFoder, @"SharedResource\style\PartialScss\_100_Private_MediaQuery.scss");
                string mediaQueryDesignScssFileFullPath = Path.Combine(websiteBAseFoder, @"SharedResource\style\PartialScss\_100_Private_MediaQuery_Design.scss");

                string mediaQueryText = "";
                string mediaQueryDesignText = "";

                if (totalWidthRanges >= 2)
                {
                    int breakWidthIndex = 1;
                    mediaQueryText += "/*** Start Of Break Width Parameters ***/"
                                    + System.Environment.NewLine + System.Environment.NewLine;

                    mediaQueryDesignText += "/*** Start Of Break Width Parameters ***/"
                                    + System.Environment.NewLine + System.Environment.NewLine;

                    foreach (int breakWidth in breakWidthList)
                    {
                        mediaQueryText += "$site_Width_" + breakWidthIndex.ToString() + ": " + breakWidth.ToString() + "px;" + System.Environment.NewLine;
                        mediaQueryDesignText += "$site_Width_" + breakWidthIndex.ToString() + ": " + breakWidth.ToString() + "px;" + System.Environment.NewLine;
                        breakWidthIndex++;

                        if (breakWidthIndex >= totalWidthRanges)
                        {
                            break;
                        }
                    }

                    mediaQueryText += System.Environment.NewLine + "/*** End Of Break Width Parameters ***/"
                                    + System.Environment.NewLine + System.Environment.NewLine;

                    mediaQueryDesignText += System.Environment.NewLine + "/*** End Of Break Width Parameters ***/"
                                    + System.Environment.NewLine + System.Environment.NewLine;

                    for (int i = 1; i <= totalWidthRanges; i++)
                    {
                        if (i == 1)
                        {
                            mediaQueryText += "@media all and (max-width: $site_Width_" + i.ToString() + ") {" + System.Environment.NewLine
                                          + "    @import \"MediaQuerySubStyle/AppMain/_AppMain_Media_WidthRange" + i.ToString() + "\";" + System.Environment.NewLine
                                          + "    @import \"MediaQuerySubStyle/CompanySite/_CompanySite_Media_WidthRange" + i.ToString() + "\";" + System.Environment.NewLine
                                          + "}" + System.Environment.NewLine + System.Environment.NewLine;
                        }
                        if (i > 1 && i < totalWidthRanges)
                        {
                            mediaQueryText += "@media all and (min-width: $site_Width_" + (i - 1).ToString() + ") and (max-width: $site_Width_" + i.ToString() + ") {" + System.Environment.NewLine
                                           + "    @import \"MediaQuerySubStyle/AppMain/_AppMain_Media_WidthRange" + i.ToString() + "\";" + System.Environment.NewLine
                                           + "    @import \"MediaQuerySubStyle/CompanySite/_CompanySite_Media_WidthRange" + i.ToString() + "\";" + System.Environment.NewLine
                                           + "}" + System.Environment.NewLine + System.Environment.NewLine;
                        }
                        else if (i == totalWidthRanges)
                        {
                            mediaQueryText += "@media all and (min-width: $site_Width_" + (i - 1).ToString() + ") {" + System.Environment.NewLine
                                         + "    @import \"MediaQuerySubStyle/AppMain/_AppMain_Media_WidthRange" + i.ToString() + "\";" + System.Environment.NewLine
                                         + "    @import \"MediaQuerySubStyle/CompanySite/_CompanySite_Media_WidthRange" + i.ToString() + "\";" + System.Environment.NewLine
                                         + "}" + System.Environment.NewLine + System.Environment.NewLine;
                        }

                        mediaQueryDesignText += ".Design_WidthRange_" + i.ToString() + " {" + System.Environment.NewLine
                                         + "    @import \"MediaQuerySubStyle/AppMain/_AppMain_Media_WidthRange" + i.ToString() + "\";" + System.Environment.NewLine
                                         + "    @import \"MediaQuerySubStyle/CompanySite/_CompanySite_Media_WidthRange" + i.ToString() + "\";" + System.Environment.NewLine
                                         + "}" + System.Environment.NewLine + System.Environment.NewLine;
                    }
                }



                try
                {

                    File.WriteAllText(mediaQueryScssFileFullPath, mediaQueryText);
                    File.WriteAllText(mediaQueryDesignScssFileFullPath, mediaQueryDesignText);

                    CompileEsiteScssToCss(websiteBAseFoder);
                }
                catch (Exception ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_GenerateScssFile_Error", ValidationItemType.Warning, "Generate Scss File Error: " + ex.ToString()));
                }

            }


            return aValidationResult;
        }

        public static void LoadBreakWidthDataFromMediaQueryScssFiles(AppEsiteExDto aAppEsiteExDto)
        {

            aAppEsiteExDto.LayoutSetting = new EsiteLayoutSettingDto();
            aAppEsiteExDto.MediaWidthList = new List<int>();

            string websiteBAseFoder = AppEsiteFileBL.GetWebSiteBasePath((int)aAppEsiteExDto.Id);
            string mediaQueryScssFileFullPath = Path.Combine(websiteBAseFoder, @"SharedResource\style\PartialScss\_100_Private_MediaQuery.scss");

            try
            {
                if (File.Exists(mediaQueryScssFileFullPath))
                {
                    string originalString = File.ReadAllText(mediaQueryScssFileFullPath);
                    //@"(?i)SELECT\s+(.+?)\s+FROM"
                    //"(?<=http://).*?(?=\.png)"

                    List<string> RepalceSectionList = AppEsiteFileBL.ExtractFromBody(originalString, "/*** Start Of Break Width Parameters ***/", @"/*** End Of Break Width Parameters ***/");
                    if (RepalceSectionList.Count > 0)
                    {
                        string firstSection = RepalceSectionList[0];
                        string[] lineList = firstSection.Split(";".ToArray());
                        foreach (string line in lineList)
                        {
                            for (int i = 1; i <= 5; i++)
                            {
                                string paramName = "$site_Width_" + i.ToString();
                                if (line.Contains(paramName))
                                {
                                    string breakWidthStr = line.Replace(paramName, "").Replace(":", "").Replace("px", "").Replace(";", "").Trim();
                                    int? breakWidth = ControlTypeValueConverter.ConvertValueToInt(breakWidthStr);
                                    if (breakWidth.HasValue && breakWidth.Value > 0)
                                    {
                                        var prop = aAppEsiteExDto.LayoutSetting.GetType().GetProperty("BreakWidth" + i.ToString());
                                        if (prop != null && prop.CanWrite)
                                        {
                                            prop.SetValue(aAppEsiteExDto.LayoutSetting, breakWidth.Value);
                                            aAppEsiteExDto.LayoutSetting.TotalWidthRanges = i + 1;
                                            aAppEsiteExDto.MediaWidthList.Add(breakWidth.Value);
                                        }
                                    }
                                }
                            }


                        }

                    }
                }
            }
            catch (Exception ex)
            {

            }

        }


        public static byte[] DownloadFileByUrl(string url, out string errorMsg)
        {
            errorMsg = "";

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    // Download the binary data from the URL synchronously
                    HttpResponseMessage response = httpClient.GetAsync(url).Result;

                    // Check if the request was successful
                    response.EnsureSuccessStatusCode();

                    // Read the binary data from the response stream
                    byte[] fileData = response.Content.ReadAsByteArrayAsync().Result;

                    return fileData;
                }

            }
            catch (WebException ex)
            {
                errorMsg = ex.Message;
                return null;
            }

        }


        public static async Task<byte[]> DownloadFileByUrlAsync(string url)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                // Download the binary data from the URL
                HttpResponseMessage response = await httpClient.GetAsync(url);

                // Check if the request was successful
                response.EnsureSuccessStatusCode();

                // Read the binary data from the response stream
                byte[] fileData = await response.Content.ReadAsByteArrayAsync();

                return fileData;
            }
        }

        public static void DownloadFileToServer(string srcUrl, string destinationFilePath, out string errorMsg)
        {
            errorMsg = "";


            string srcFileName = Path.GetFileName(srcUrl);

            if (string.IsNullOrWhiteSpace(srcFileName))
            {
                errorMsg = "Cannot get file name from file url: " + srcUrl;
            }
            else
            {
                try
                {
                    string targetFileName = Path.GetFileName(destinationFilePath);
                    string destinationFolderPath = "";

                    if (string.IsNullOrWhiteSpace(targetFileName))
                    {
                        targetFileName = srcFileName;
                        destinationFolderPath = destinationFilePath;
                    }
                    else
                    {
                        destinationFolderPath = destinationFilePath.Substring(0, destinationFilePath.Length - targetFileName.Length);

                        string srcExtension = Path.GetExtension(srcFileName);
                        string targetExtension = Path.GetExtension(targetFileName);

                        if (srcExtension.ToLower() != targetExtension.ToLower())
                        {
                            targetFileName = targetFileName.Substring(0, targetFileName.Length - targetExtension.Length) + srcExtension;
                        }
                    }

                    // Set TLS version and security protocol
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    using (WebClient webClient = new WebClient())
                    {
                        if (!System.IO.Directory.Exists(destinationFolderPath))
                        {
                            System.IO.Directory.CreateDirectory(destinationFolderPath);
                        }

                        if (string.IsNullOrWhiteSpace(targetFileName))
                        {
                            if (targetFileName.Length > 50)
                            {
                                targetFileName = targetFileName.Substring(0, 40) + "_" + Guid.NewGuid().ToString();
                            }
                        }

                        string filePath = System.IO.Path.Combine(destinationFolderPath, targetFileName);

                        webClient.DownloadFile(srcUrl, filePath);
                    }


                }
                catch (Exception ex)
                {
                    errorMsg = ex.Message;
                }
            }

        }

        public static string ConvertToValidFileName(string input)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();

            string validFileName = new string(input.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());

            return validFileName.Trim();
        }

        public static string GetFileParentFolderName(string fileFullPath)
        {

            string directoryPath = Path.GetDirectoryName(fileFullPath);


            return new DirectoryInfo(directoryPath).Name;
        }

        private static void AppendOneLinkPage(AppEsitePagesExDto aAppEsitePagesExDto, string customerOrSupplierhomePageFullPath)
        {
            if (File.Exists(customerOrSupplierhomePageFullPath))
            {



                string beginToken = "<!--BeginLoggedUserNavigationSectionAutoGenerated-->";
                string endToken = @"<!--EndLoggedUserNavigationSectionAutoGenerated-->";
                UpdateSupplierOrCustomerHomePage(aAppEsitePagesExDto, customerOrSupplierhomePageFullPath, beginToken, endToken);

                DirectoryInfo parentDir = Directory.GetParent(customerOrSupplierhomePageFullPath);
                if (parentDir.FullName.IndexOf("Customer") != -1)
                {
                    string employeeHomeFullPath = Directory.GetParent(parentDir.FullName).FullName + "\\Employee\\HomePage.html";
                    beginToken = "<!--BeginLoggedCustomerUserNavigationSectionAutoGenerated-->";
                    endToken = @"<!--EndLoggedCustomerUserNavigationSectionAutoGenerated-->";
                    UpdateSupplierOrCustomerHomePage(aAppEsitePagesExDto, employeeHomeFullPath, beginToken, endToken);


                }
                else if (parentDir.FullName.IndexOf("Supplier") != -1)
                {
                    string employeeHomeFullPath = Directory.GetParent(parentDir.FullName).FullName + "\\Employee\\HomePage.html";
                    beginToken = "<!--BeginLoggedSupplierUserNavigationSectionAutoGenerated-->";
                    endToken = @"<!--EndLoggedSupplierUserNavigationSectionAutoGenerated-->";
                    UpdateSupplierOrCustomerHomePage(aAppEsitePagesExDto, employeeHomeFullPath, beginToken, endToken);


                }
                else if (parentDir.FullName.IndexOf("Employee") != -1)
                {

                }

                else if (parentDir.FullName.IndexOf("Shared") != -1)
                {

                }




            }
        }

        private static void UpdateSupplierOrCustomerHomePage(AppEsitePagesExDto aAppEsitePagesExDto, string customerOrSupplierhomePageFullPath, string beginToken, string endToken)
        {
            string originalString = File.ReadAllText(customerOrSupplierhomePageFullPath);
            List<string> repalceSectionList = ExtractFromBody(originalString, beginToken, endToken);
            if (repalceSectionList.Count > 0)
            {
                string firstSection = repalceSectionList[0];

                string linkState = $"<button class=\" \" ng-click=\"goToPageByRouteCode('{aAppEsitePagesExDto.MetaDesciption}', {(aAppEsitePagesExDto.TransactionId.HasValue ? aAppEsitePagesExDto.TransactionId : aAppEsitePagesExDto.SearchId)})\">" + System.Environment.NewLine;

                if (!firstSection.Contains(linkState))
                {
                    string appendString = System.Environment.NewLine +
                      $"<div class=\"MainIconContainer\">" + System.Environment.NewLine +
                             linkState +
                            $"<img src=\"SharedResource/images/Group28_w.svg\" class=\"\">" + System.Environment.NewLine +
                        $"</button>" + System.Environment.NewLine +
                        $"<div class=\"btnLabel\">{aAppEsitePagesExDto.Title}</div>" + System.Environment.NewLine +
                       $"</div>" + System.Environment.NewLine;


                    originalString = originalString.Replace(firstSection, firstSection + appendString);
                    File.WriteAllText(customerOrSupplierhomePageFullPath, originalString);
                }




            }
        }


        private static void ImportMenuTreeFromSiteTemplate(int? tempalteSiteId, int? importToSiteId)
        {
            if (tempalteSiteId.HasValue && importToSiteId.HasValue)
            {
                ObservableSet<AppListMenuExDto> importToSiteMenuTree_PublicPage = AppTreeListMenuBL.RetrieveNoneMgtUserTreeMenu(importToSiteId, (int)EmAppMenuItemCategory.PublicPage);

                if (importToSiteMenuTree_PublicPage.IsEmpty())
                {
                    ObservableSet<AppListMenuExDto> tempMenuTree_PublicPage = AppTreeListMenuBL.RetrieveNoneMgtUserTreeMenu(tempalteSiteId, (int)EmAppMenuItemCategory.PublicPage);

                    foreach (var menuDto in tempMenuTree_PublicPage)
                    {
                        ImportOneMenuFromSiteTemplate(importToSiteId, menuDto, null);
                    }
                }




                ObservableSet<AppListMenuExDto> importToSiteMenuTree_ClientPage = AppTreeListMenuBL.RetrieveNoneMgtUserTreeMenu(importToSiteId, (int)EmAppMenuItemCategory.ClientPage);

                if (importToSiteMenuTree_ClientPage.IsEmpty())
                {
                    ObservableSet<AppListMenuExDto> tempMenuTree_ClientPage = AppTreeListMenuBL.RetrieveNoneMgtUserTreeMenu(tempalteSiteId, (int)EmAppMenuItemCategory.ClientPage);

                    foreach (var menuDto in tempMenuTree_ClientPage)
                    {
                        ImportOneMenuFromSiteTemplate(importToSiteId, menuDto, null);
                    }
                }


                ObservableSet<AppListMenuExDto> importToSiteMenuTree_SupplierPage = AppTreeListMenuBL.RetrieveNoneMgtUserTreeMenu(importToSiteId, (int)EmAppMenuItemCategory.SupplierPage);

                if (importToSiteMenuTree_SupplierPage.IsEmpty())
                {
                    ObservableSet<AppListMenuExDto> tempMenuTree_SupplierPage = AppTreeListMenuBL.RetrieveNoneMgtUserTreeMenu(tempalteSiteId, (int)EmAppMenuItemCategory.SupplierPage);

                    foreach (var menuDto in tempMenuTree_SupplierPage)
                    {
                        ImportOneMenuFromSiteTemplate(importToSiteId, menuDto, null);
                    }
                }
            }
        }

        private static void ImportOneMenuFromSiteTemplate(int? importToSiteId, AppListMenuExDto siteTemplateMenuDto, int? parentMenuId)
        {
            siteTemplateMenuDto.Id = null;
            siteTemplateMenuDto.ParentId = null;
            siteTemplateMenuDto.EsiteId = importToSiteId;
            siteTemplateMenuDto.ParentId = parentMenuId;
            var saveResult = AppTreeListMenuBL.SaveOneAppListMenuTreeNode(siteTemplateMenuDto);

            if (!siteTemplateMenuDto.AppListMenu_List.IsEmpty())
            {
                if (saveResult.IsSuccessfulWithResult)
                {
                    AppListMenuExDto savedMenu = saveResult.Object;

                    foreach (var subMenuDto in siteTemplateMenuDto.AppListMenu_List)
                    {
                        ImportOneMenuFromSiteTemplate(importToSiteId, subMenuDto, (int)savedMenu.Id);
                    }
                }
            }
        }

        private static void CopyOneFileToNewLocation(string srcFoldrePath, string distFoldrePath, string fileName)
        {
            string sourceFile = System.IO.Path.Combine(srcFoldrePath, fileName);
            string destFile = System.IO.Path.Combine(distFoldrePath, fileName);

            System.IO.Directory.CreateDirectory(distFoldrePath);
            System.IO.File.Copy(sourceFile, destFile, true);
        }


        private static void CopyAllFilesFromOneFolder(string srcFoldrePath, string distFoldrePath)
        {
            if (System.IO.Directory.Exists(srcFoldrePath))
            {
                System.IO.Directory.CreateDirectory(distFoldrePath);
                string[] files = System.IO.Directory.GetFiles(srcFoldrePath);

                foreach (string s in files)
                {
                    string fileName = System.IO.Path.GetFileName(s);
                    var destFile = System.IO.Path.Combine(distFoldrePath, fileName);
                    System.IO.File.Copy(s, destFile, true);
                }
            }
        }

        private static void ExportOneEsitePageToStaticPage_PopulateSearchViewPageHtml(AppEsitePagesExDto sourcePageExDto, AppEsiteExDto appEsiteExDto)
        {
            var aSearchDto = AppSearchBL.RetrieveOneSearchDto(sourcePageExDto.SearchId.Value, false, false);
            AppSearchViewExDto defaultViewDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(aSearchDto.DefaultView.Id);

            if (defaultViewDto != null)
            {
                ReferenceViewDefinitionDto referenceViewDefinitionDto = new ReferenceViewDefinitionDto();
                referenceViewDefinitionDto.IsMassUpdate = false;
                referenceViewDefinitionDto.Id = aSearchDto.DefaultView.Id;
                aSearchDto.ReferenceViewDefinitionDto = referenceViewDefinitionDto;
                SearchResultDto searchResult = AppSearchBL.RetrieveSearchResult(aSearchDto);

                if (searchResult != null && !searchResult.SearchResultRowList.IsEmpty())
                {
                    var resultRowList = searchResult.SearchResultRowList;

                    List<string> resultItemTemplateList = ExtractFromBody(sourcePageExDto.HtmlContent, "<!--  Start Of Search Result List -->", "<!--  End Of Search Result List -->");

                    if (resultItemTemplateList.Count > 0)
                    {
                        string resultItemTemplate = resultItemTemplateList[0];

                        string allResultItemsHtml = "";



                        foreach (var searchResultRow in resultRowList)
                        {
                            string oneResultItemHtml = ExportOneEsitePageToStaticPage_GenerateOneSearchResultRowHtml(appEsiteExDto, resultItemTemplate, searchResultRow);

                            allResultItemsHtml += oneResultItemHtml + System.Environment.NewLine;
                        }

                        sourcePageExDto.HtmlContent = ReplaceBodyString(sourcePageExDto.HtmlContent, "<!--  Start Of Search Result List -->", "<!--  End Of Search Result List -->", allResultItemsHtml);



                    }



                }
            }
        }

        private static string ExportOneEsitePageToStaticPage_GenerateOneSearchResultRowHtml(AppEsiteExDto appEsiteExDto, string resultItemTemplate, StaticSearchResultRowJsonDto searchResultRow)
        {
            string oneResultItemHtml = resultItemTemplate;

            List<string> needToReplaceTokens = ExtractFromBody(oneResultItemHtml, "%{", "}");

            foreach (string needToReplaceToken in needToReplaceTokens)
            {
                string tokenReplaceToHtml = "";

                if (needToReplaceToken == "AppImageBaseUrl")
                {
                    if (!string.IsNullOrWhiteSpace(appEsiteExDto.EsiteAttribute.MgtSiteBaseUrl))
                    {
                        tokenReplaceToHtml = appEsiteExDto.EsiteAttribute.MgtSiteBaseUrl + "GetRegularImage.aspx?CurrentUserSessionId=" + anonymousToken + "&FileId=";
                    }
                }
                else if (needToReplaceToken.StartsWith("getDate("))
                {
                    int startIndex = "getDate(".Length;

                    string cellValueToken = needToReplaceToken.Substring(startIndex, needToReplaceToken.Length - startIndex - 1);
                    object value = GetOneSearchReslutCellValueFromToken(searchResultRow, cellValueToken);

                    DateTime? dateValue = ControlTypeValueConverter.ConvertValueToDate(value);

                    if (dateValue.HasValue)
                    {
                        tokenReplaceToHtml = dateValue.Value.ToString("dd");
                    }
                }
                else if (needToReplaceToken.StartsWith("getMonth("))
                {
                    int startIndex = "getMonth(".Length;

                    string cellValueToken = needToReplaceToken.Substring(startIndex, needToReplaceToken.Length - startIndex - 1);
                    object value = GetOneSearchReslutCellValueFromToken(searchResultRow, cellValueToken);

                    DateTime? dateValue = ControlTypeValueConverter.ConvertValueToDate(value);

                    if (dateValue.HasValue)
                    {
                        tokenReplaceToHtml = dateValue.Value.ToString("MMM", CultureInfo.CreateSpecificCulture("en"));
                    }
                }
                else if (needToReplaceToken.StartsWith("SearchResultValue."))
                {
                    object value = GetOneSearchReslutCellValueFromToken(searchResultRow, needToReplaceToken);

                    if (value != null)
                    {
                        tokenReplaceToHtml = value.ToString();
                    }
                }

                oneResultItemHtml = ReplaceBodyString(oneResultItemHtml, "%{", "}", tokenReplaceToHtml);
            }

            return oneResultItemHtml;
        }

        private static object GetOneSearchReslutCellValueFromToken(StaticSearchResultRowJsonDto searchResultRow, string needToReplaceToken)
        {
            object value = null;

            int? viewColumnId = ControlTypeValueConverter.ConvertValueToInt(needToReplaceToken.Substring(("SearchResultValue.").Length));

            if (viewColumnId.HasValue)
            {
                if (searchResultRow.DictViewColumnIDKeyValue.ContainsKey(viewColumnId.Value))
                {
                    value = searchResultRow.DictViewColumnIDKeyValue[viewColumnId.Value];
                }
            }

            return value;
        }

        private static void ExportOneEsitePageToStaticPage_GenerateSearchViewDetailPages(AppEsitePagesExDto searchPageExDto, AppEsiteExDto appEsiteExDto, ValidationResult validationResult, string requestHostServerPath, string subsiteName)
        {
            var aSearchDto = AppSearchBL.RetrieveOneSearchDto(searchPageExDto.SearchId.Value, false, false);
            AppSearchViewExDto defaultViewDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(aSearchDto.DefaultView.Id);

            if (defaultViewDto != null)
            {
                ReferenceViewDefinitionDto referenceViewDefinitionDto = new ReferenceViewDefinitionDto();
                referenceViewDefinitionDto.IsMassUpdate = false;
                referenceViewDefinitionDto.Id = aSearchDto.DefaultView.Id;
                aSearchDto.ReferenceViewDefinitionDto = referenceViewDefinitionDto;
                SearchResultDto searchResult = AppSearchBL.RetrieveSearchResult(aSearchDto);

                if (searchResult != null && !searchResult.SearchResultRowList.IsEmpty())
                {
                    var resultRowList = searchResult.SearchResultRowList;

                    int eSiteId = searchPageExDto.EsiteId.Value;
                    string templatePageFileName = searchPageExDto.PageAttribute.StaticSiteSearchDetailViewPageFileName;
                    int pkViewColumnId = searchPageExDto.PageAttribute.StaticSiteSearchDetailViewPagePkViewColumnId.Value;

                    string appSitePath = GetWebSiteBasePath(eSiteId);
                    string staticPageFolderPath = string.Format("{0}{1}\\Dev\\StaticPages\\", appSitePath, subsiteName);

                    string templatePageFullPath = Path.Combine(staticPageFolderPath, templatePageFileName);
                    AppEsitePagesExDto templatePageDto = RetrieveOneWebSiteFile(templatePageFullPath, eSiteId);

                    if (templatePageDto.TransactionId.HasValue)
                    {
                        int transactionId = templatePageDto.TransactionId.Value;

                        foreach (var searchResultRow in resultRowList)
                        {
                            if (searchResultRow.DictViewColumnIDKeyValue.ContainsKey(pkViewColumnId))
                            {
                                object pkValue = searchResultRow.DictViewColumnIDKeyValue[pkViewColumnId];

                                AppMasterDetailDto formData = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(transactionId, pkValue);
                                AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);
                                AppTransactionStructureDto aAppTransactionStructureDto = AppTransactionStructureLoadBL.GetAppTransactionStructureDto(transactionId);

                                if (formData != null)
                                {
                                    string newHtmlContent = templatePageDto.HtmlContent;

                                    List<string> needToReplaceTokens = ExtractFromBody(newHtmlContent, "%{", "}");

                                    foreach (string needToReplaceToken in needToReplaceTokens)
                                    {
                                        string tokenReplaceToHtml = "";

                                        object value = GetOneFormRootFieldValueFromToken(transactionExDto, aAppTransactionStructureDto, formData, needToReplaceToken);

                                        if (value != null)
                                        {
                                            tokenReplaceToHtml = value.ToString();
                                        }

                                        newHtmlContent = ReplaceBodyString(newHtmlContent, "%{", "}", tokenReplaceToHtml);
                                    }


                                    AppEsitePagesExDto newPageExDto = templatePageDto.DeepCopy();
                                    newPageExDto.HtmlContent = newHtmlContent;
                                    newPageExDto.FileCode = templatePageDto.MetaDesciption + "_" + pkValue + ".html";
                                    string distSubFolderPath = searchPageExDto.MetaDesciption;

                                    if (templatePageDto.PageAttribute != null)
                                    {
                                        if (templatePageDto.PageAttribute.PageTitleTransFieldId.HasValue)
                                        {
                                            int transFieldId = templatePageDto.PageAttribute.PageTitleTransFieldId.Value;
                                            object value = AppMasterDetailFormPrintBL.GetRootLevelTransactionFieldValue(formData, transactionExDto, transFieldId);
                                            string valueStr = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(value);

                                            if (!string.IsNullOrWhiteSpace(valueStr))
                                            {
                                                newPageExDto.PageAttribute.PageTitle = valueStr;

                                                //newPageExDto.FileCode = templatePageDto.MetaDesciption + "_" + pkValue + valueStr.Replace(" ", "_") + ".html";
                                            }
                                        }

                                        if (templatePageDto.PageAttribute.PageDescriptionTransFieldId.HasValue)
                                        {
                                            int transFieldId = templatePageDto.PageAttribute.PageDescriptionTransFieldId.Value;
                                            object value = AppMasterDetailFormPrintBL.GetRootLevelTransactionFieldValue(formData, transactionExDto, transFieldId);
                                            string valueStr = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(value);

                                            if (!string.IsNullOrWhiteSpace(valueStr))
                                            {
                                                newPageExDto.PageAttribute.PageDescription = valueStr;
                                            }
                                        }

                                        //if (templatePageDto.PageAttribute.OgImageUrlTransFieldId.HasValue)
                                        //{
                                        //    int transFieldId = templatePageDto.PageAttribute.OgImageUrlTransFieldId.Value;
                                        //    object value = AppMasterDetailFormPrintBL.GetRootLevelTransactionFieldValue(formData, transactionExDto, transFieldId);

                                        //    string valueStr = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(value);

                                        //    if (!string.IsNullOrWhiteSpace(valueStr))
                                        //    {
                                        //        newPageExDto.Description = valueStr;
                                        //    }
                                        //}
                                    }



                                    var result = ExportOneEsitePageToStaticPage(newPageExDto, distSubFolderPath, newPageExDto.FileCode, newHtmlContent, requestHostServerPath);

                                    if (!result.IsSuccessful)
                                    {
                                        validationResult.Merge(result.ValidationResult);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }



        private static object GetOneFormRootFieldValueFromToken(AppTransactionExDto transactionExDto, AppTransactionStructureDto aAppTransactionStructureDto, AppMasterDetailDto formData, string needToReplaceToken)
        {
            object value = null;

            //FormData.DictOneToOneFields['Title']

            if (needToReplaceToken.StartsWith("FormData.DictOneToOneFields['"))
            {
                int startIndex = "FormData.DictOneToOneFields['".Length;

                string fieldDbName = needToReplaceToken.Substring(startIndex, needToReplaceToken.Length - startIndex - 2);

                if (!string.IsNullOrWhiteSpace(fieldDbName))
                {
                    if (formData.DictOneToOneFields.ContainsKey(fieldDbName))
                    {
                        value = formData.DictOneToOneFields[fieldDbName];

                        var transField = transactionExDto.RootMasterUnit.AppTransactionFieldList.FirstOrDefault(o => o.DataBaseFieldName == fieldDbName);

                        if (transField != null)
                        {
                            value = AppMasterDetailFormPrintBL.GetTransFieldPrintValue(transField, value, aAppTransactionStructureDto);
                        }

                    }
                }
            }
            else if (needToReplaceToken.StartsWith("FormData.DictSiblingOneToOneFields["))
            {
                int prefixLength = "FormData.DictSiblingOneToOneFields[".Length;
                int unitId_EndIndex = needToReplaceToken.IndexOf("]", prefixLength);

                if (unitId_EndIndex >= 0)
                {
                    int unitId_Length = unitId_EndIndex - prefixLength;

                    int? sibUnitId = ControlTypeValueConverter.ConvertValueToInt(needToReplaceToken.Substring(prefixLength, unitId_Length));

                    if (sibUnitId.HasValue && transactionExDto.DictAllTransactionUnitIdExDto.ContainsKey(sibUnitId.Value.ToString()))
                    {
                        var unitExDto = transactionExDto.DictAllTransactionUnitIdExDto[sibUnitId.Value.ToString()];

                        int fieldDbName_StartIndex = unitId_EndIndex + 2;
                        int fieldDbName_EndIndex = needToReplaceToken.IndexOf("']", fieldDbName_StartIndex);
                        int fieldDbName_Length = fieldDbName_EndIndex - fieldDbName_StartIndex;
                        string fieldDbName = needToReplaceToken.Substring(fieldDbName_StartIndex, fieldDbName_Length);

                        var transField = unitExDto.AppTransactionFieldList.FirstOrDefault(o => o.DataBaseFieldName == fieldDbName);

                        if (transField != null)
                        {
                            value = AppMasterDetailFormPrintBL.GetTransFieldPrintValue(transField, value, aAppTransactionStructureDto);
                        }
                    }
                }
            }


            return value;
        }

        internal static void InitThirdPartControlThemeFileParameterDictionary(AppEsitePagesExDto filePageDto)
        {
            if (filePageDto != null && !string.IsNullOrWhiteSpace(filePageDto.HtmlContent))
            {
                if (filePageDto.PageAttribute == null)
                {
                    filePageDto.PageAttribute = new AppEsitePageAttributeDto();
                }

                filePageDto.PageAttribute.DictThemeParameterAndValue = new Dictionary<string, string>();

                List<string> parameterSectionList = ExtractFromBody(filePageDto.HtmlContent, @"/* Start Of Parameters */", @"/* End Of Parameters */");


                if (parameterSectionList.Count > 0)
                {
                    string parameterSection = parameterSectionList[0];

                    List<string> codeLineLies = parameterSection.Split('\n').ToList();

                    foreach (string codeLine in codeLineLies)
                    {
                        string lineText = codeLine.Trim();

                        List<string> parameterStringList = lineText.Split(';').ToList();
                        foreach (string parameterString in parameterStringList)
                        {
                            List<string> parameterParts = parameterString.Split(':').ToList();
                            if (parameterParts.Count == 2)
                            {
                                string parameterName = parameterParts[0].Trim();
                                string parameterValue = parameterParts[1].Trim();

                                if (parameterName.Contains('$') && !string.IsNullOrWhiteSpace(parameterValue))
                                {
                                    parameterName = parameterName.Substring(1);
                                    filePageDto.PageAttribute.DictThemeParameterAndValue.Add(parameterName, parameterValue);
                                }
                            }
                        }


                    }

                }

            }

        }

        private static void SynchronizeComponentOnAllPages(AppEsitePagesExDto appEsitePagesDto)
        {
            if (!string.IsNullOrWhiteSpace(appEsitePagesDto.HtmlContent))
            {
                var parser = new HtmlParser();
                var documentComponent = parser.ParseDocument(appEsitePagesDto.HtmlContent);

                var componentElement = documentComponent.Body.FirstElementChild;
                string componentId = componentElement.GetAttribute("data-component-element-id");

                bool isDynamicComponent = !appEsitePagesDto.FileCode.ToLower().StartsWith("_UI_".ToLower());

                if (!string.IsNullOrWhiteSpace(componentId))
                {
                    Dictionary<string, AppEsitePagesDto> dictFullNamePgeDto = GetWebSiteAllPageExtenstionPage(appEsitePagesDto.EsiteId);

                    List<AppEsitePagesDto> pageDtoList = AppEsiteConfigBL.RetrieveFileExtentPageDtoList(appEsitePagesDto.EsiteId);

                    foreach (var fileFullPath in dictFullNamePgeDto.Keys)
                    {
                        try
                        {
                            AppEsitePagesExDto pageDto = RetrieveOneWebSiteFile(fileFullPath, appEsitePagesDto.EsiteId.Value);

                            if (!string.IsNullOrWhiteSpace(pageDto.HtmlContent))
                            {

                                var pageDocument = parser.ParseDocument(pageDto.HtmlContent);


                                var componentInstanceList = pageDocument.QuerySelectorAll("[data-component-element-id='" + componentId + "']");

                                bool isPageModified = false;

                                // Iterate through the found elements
                                foreach (var componentInstance in componentInstanceList)
                                {
                                    var updatedComponentInstance = parser.ParseDocument(componentInstance.OuterHtml);
                                    //var newElement = parser.ParseDocument(componentElement.OuterHtml).Body.FirstElementChild;

                                    List<IElement> needToDeleteElementList = new List<IElement>();

                                    SynchronizeComponent_ProcessOneChildElement(updatedComponentInstance, componentElement, null, needToDeleteElementList);

                                    if (!isDynamicComponent)
                                    {
                                        foreach (var needToDeleteElement in needToDeleteElementList)
                                        {
                                            string needToDeleteElemId = needToDeleteElement.GetAttribute("data-component-element-id");
                                            var toRemoveElement = updatedComponentInstance.QuerySelector("[data-component-element-id='" + needToDeleteElemId + "']");

                                            if (toRemoveElement != null && toRemoveElement.ParentElement != null)
                                            {
                                                toRemoveElement.ParentElement.RemoveChild(toRemoveElement);
                                            }
                                        }
                                    }


                                    componentInstance.OuterHtml = updatedComponentInstance.Body.FirstElementChild.OuterHtml;

                                    isPageModified = true;
                                }

                                if (isPageModified)
                                {
                                    pageDto.HtmlContent = pageDocument.Body.InnerHtml;

                                    SaveOneWebSiteFileWithHtmlPageAtributeFile(pageDto);
                                }

                            }



                        }
                        catch (Exception ex)
                        {

                        }

                    }

                }


            }
        }

        //private static void SynchronizeComponent_ProcessOneChildElement_ToDelete(IHtmlDocument backupDocument, IElement element)
        //{
        //    string componentElemId = element.GetAttribute("data-component-element-id");

        //    if (!string.IsNullOrWhiteSpace(componentElemId))
        //    {
        //        var orgElement = backupDocument.QuerySelector("[data-component-element-id='" + componentElemId + "']");
        //        if (orgElement != null)
        //        {
        //            if (orgElement.ChildElementCount == 0 && !string.IsNullOrWhiteSpace(orgElement.InnerHtml))
        //            {
        //                if (element.ChildElementCount == 0)
        //                {
        //                    element.InnerHtml = orgElement.InnerHtml;
        //                }
        //            }

        //            string orgValue_src = orgElement.GetAttribute("src");
        //            string orgValue_ngModel = orgElement.GetAttribute("ng-model");
        //            string orgValue_ngIf = orgElement.GetAttribute("ng-if");
        //            string orgValue_ngShow = orgElement.GetAttribute("ng-show");
        //            string orgValue_ngHide = orgElement.GetAttribute("ng-hide");
        //            string orgValue_ngStyle = orgElement.GetAttribute("ng-style");
        //            string orgValue_ngClass = orgElement.GetAttribute("ng-class");
        //            string orgValue_ngClick = orgElement.GetAttribute("ng-click");

        //            if (!string.IsNullOrWhiteSpace(orgValue_src))
        //            {
        //                element.SetAttribute("src", orgValue_src);
        //            }

        //            if (!string.IsNullOrWhiteSpace(orgValue_ngModel))
        //            {
        //                element.SetAttribute("ng-model", orgValue_ngModel);
        //            }

        //            if (!string.IsNullOrWhiteSpace(orgValue_ngIf))
        //            {
        //                element.SetAttribute("ng-if", orgValue_ngIf);
        //            }

        //            if (!string.IsNullOrWhiteSpace(orgValue_ngShow))
        //            {
        //                element.SetAttribute("ng-show", orgValue_ngShow);
        //            }

        //            if (!string.IsNullOrWhiteSpace(orgValue_ngHide))
        //            {
        //                element.SetAttribute("ng-hide", orgValue_ngHide);
        //            }

        //            if (!string.IsNullOrWhiteSpace(orgValue_ngStyle))
        //            {
        //                element.SetAttribute("ng-style", orgValue_ngStyle);
        //            }

        //            if (!string.IsNullOrWhiteSpace(orgValue_ngClass))
        //            {
        //                element.SetAttribute("ng-class", orgValue_ngClass);
        //            }

        //            if (!string.IsNullOrWhiteSpace(orgValue_ngClick))
        //            {
        //                element.SetAttribute("ng-click", orgValue_ngClick);
        //            }
        //        }
        //    }

        //    if (element.ChildElementCount > 0)
        //    {
        //        foreach (var childElement in element.Children)
        //        {
        //            SynchronizeComponent_ProcessOneChildElement_ToDelete(backupDocument, childElement);
        //        }
        //    }

        //}



        private static void SynchronizeComponent_ProcessOneChildElement(IHtmlDocument updatedInstance, IElement componentElement, IElement instanceParentElement, List<IElement> needToDeleteElementList)
        {
            string componentElemId = componentElement.GetAttribute("data-component-element-id");

            if (!string.IsNullOrWhiteSpace(componentElemId))
            {
                IElement currentElement = null;

                var orgElement = updatedInstance.QuerySelector("[data-component-element-id='" + componentElemId + "']");
                if (orgElement != null)
                {
                    currentElement = orgElement;
                    currentElement.SetAttribute("class", componentElement.GetAttribute("class"));
                    currentElement.SetAttribute("style", componentElement.GetAttribute("style"));


                    if (instanceParentElement != null)
                    {
                        instanceParentElement.AppendChild(currentElement);

                        if (needToDeleteElementList.Contains(instanceParentElement))
                        {
                            needToDeleteElementList.Remove(instanceParentElement);
                        }
                    }
                }
                else
                {
                    currentElement = componentElement;

                    if (instanceParentElement != null)
                    {
                        instanceParentElement.AppendChild(currentElement);

                        needToDeleteElementList.Add(currentElement);
                    }
                }


                List<string> componentChildElementIdList = new List<string>();

                foreach (var componentChildElement in componentElement.Children)
                {
                    SynchronizeComponent_ProcessOneChildElement(updatedInstance, componentChildElement, currentElement, needToDeleteElementList);

                    string childElemId = componentChildElement.GetAttribute("data-component-element-id");

                    if (!string.IsNullOrWhiteSpace(childElemId))
                    {
                        componentChildElementIdList.Add(childElemId);
                    }
                }

                if (!needToDeleteElementList.Contains(currentElement))
                {
                    if (instanceParentElement != null && needToDeleteElementList.Contains(instanceParentElement))
                    {
                        needToDeleteElementList.Remove(instanceParentElement);
                    }
                }

                componentChildElementIdList = componentChildElementIdList.Distinct().ToList();

                var instanceChildElementNotBelongToComponent = currentElement.Children.Where(o => !componentChildElementIdList.Contains(o.GetAttribute("data-component-element-id"))).ToList();

                foreach (var childElement in instanceChildElementNotBelongToComponent)
                {
                    currentElement.AppendChild(childElement);
                }
            }
        }
    }
}