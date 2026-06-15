using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.LBL.EntityClasses;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.Components.EntityDto;
using System;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using AngleSharp;
using AngleSharp.Dom;
using App.BL;
using APP.Components.Dto;
using System.Text.RegularExpressions;


using Newtonsoft.Json.Linq;

using RestSharp;
using System.Net;
using AngleSharp.Html.Parser;
using AngleSharp.Html.Dom;
    
using Org.BouncyCastle.Asn1.Ocsp;
using System.ComponentModel;
using GemBox.Document;
using System.Xml.Linq;

namespace APP.BL
{
    public static class AppFigmaApiBL
    {

        //private static string _figmaApiAccessToken = "SET_VIA_CONFIG";
        public static List<AppEsitePagesDto> RetrieveAppEsiteFigmaTemplateList(int? siteId)
        {
            List<AppEsitePagesDto> figmaTemplateList = new List<AppEsitePagesDto>();

            if (siteId.HasValue)
            {
                string siteBasePath = AppEsiteFileBL.GetCompanyWebSiteBaseFolderPath(siteId.Value);

                string folderPath = siteBasePath + "\\SharedResource\\FigmaTemplate";

                if (!System.IO.Directory.Exists(folderPath))
                {
                    System.IO.Directory.CreateDirectory(folderPath);
                }

                DirectoryInfo d = new DirectoryInfo(folderPath);//Assuming Test is your Folder
                FileInfo[] files = d.GetFiles(); //Getting Text files

                foreach (var fileInfo in files.OrderBy(o => o.Name))
                {
                    string filename = fileInfo.Name;

                    var filenamePartArray = filename.Split('.')[0].Split(new[] { "___" }, StringSplitOptions.None);

                    if (filenamePartArray.Length >= 2)
                    {
                        var pageDto = new AppEsitePagesDto();
                        pageDto.EsiteId = siteId;
                        pageDto.LogicKey = filenamePartArray[0];
                        pageDto.FileCode = filename;

                        pageDto.ComponentName = filenamePartArray[1];
                        pageDto.FileFullPath = fileInfo.FullName;

                        figmaTemplateList.Add(pageDto);
                    }
                }
            }


            return figmaTemplateList;
        }


        public static List<AppEsitePagesDto> RetrieveOneAppEsiteFigmaTemplatePageList(AppEsitePagesDto inputDto)
        {
            List<AppEsitePagesDto> toReturn = new List<AppEsitePagesDto>();

            if (inputDto != null)
            {
                int? siteId = inputDto.EsiteId;
                string fileFullPath = inputDto.FileFullPath;
                dynamic figmaFileObj = GetOneImportedFigmaTemplate(siteId, fileFullPath);

                if (figmaFileObj != null)
                {
                    PrepareEsitePageDtoFromFigmaTemplateObject(toReturn, figmaFileObj, inputDto.IsAutoConvertInlineStyleToTailwind);
                }
            }

            return toReturn;
        }


        public static AppEsitePagesDto RetrieveOneAppEsiteFigmaTemplatePageContent(AppEsitePagesDto inputDto)
        {
            if (inputDto != null)
            {
                int? siteId = inputDto.EsiteId;
                string fileFullPath = inputDto.FileFullPath;
                string canvasId = inputDto.FigmaCanvasId;
                string frameId = inputDto.FigmaFrameId;

                if (siteId.HasValue && !string.IsNullOrWhiteSpace(fileFullPath) && !string.IsNullOrWhiteSpace(canvasId) && !string.IsNullOrWhiteSpace(frameId))
                {
                    dynamic figmaFileObj = GetOneImportedFigmaTemplate(siteId, fileFullPath);


                    if (figmaFileObj != null && figmaFileObj.document != null && figmaFileObj.document.children != null)
                    {
                        foreach (var canvas in figmaFileObj.document.children)
                        {
                            if (canvas.type == "CANVAS" && canvas.children != null && canvas.id == canvasId)
                            {
                                foreach (var frame in canvas.children)
                                {
                                    if (frame.type == "FRAME" && frame.id == frameId)
                                    {
                                        AppEsitePagesDto convertedHtmlPage = new AppEsitePagesDto();
                                        convertedHtmlPage.FileCode = frame.name;
                                        convertedHtmlPage.Description = canvas.name;
                                        convertedHtmlPage.SizeScaleFactor = inputDto.SizeScaleFactor;
                                        convertedHtmlPage.HtmlContent = ConvertFigmaToHtml(frame, inputDto.SizeScaleFactor);


                                        if (inputDto.IsAutoConvertAbsolutePositionToStatic || true)
                                        {
                                            convertedHtmlPage.HtmlContent = ConvertHtmlAbsolutePositionToStatic(convertedHtmlPage.HtmlContent);
                                        }

                                        if (inputDto.IsAutoConvertInlineStyleToTailwind)
                                        {
                                            convertedHtmlPage.HtmlContent = AppTailwindHelperBL.ConvertOneHtmlPageInlineStylesToTailwind(convertedHtmlPage.HtmlContent, new TailwindConvertSettingDto());
                                        }

                                        //ValidationResult aValidationResult = new ValidationResult();
                                        //List<string> fontFamilyList = new List<string>();
                                        //Dictionary<string, dynamic> dictRegularImageCodeAndNodeObj = new Dictionary<string, dynamic>();
                                        //Dictionary<string, dynamic> dictSvgImageCodeAndNodeObj = new Dictionary<string, dynamic>();

                                        //FindFigmaNodeTreeImageAndFont(aValidationResult, frame, fontFamilyList, dictRegularImageCodeAndNodeObj, dictSvgImageCodeAndNodeObj);

                                        //convertedHtmlPage.FontFamilyList = fontFamilyList;

                                        return convertedHtmlPage;
                                    }
                                }

                                break;
                            }
                        }
                    }
                }


            }

            return null;
        }

        //private static 



        public static OperationCallResult<AppEsitePagesDto> ImportAppEsiteFigmaTemplate(int? siteId, string figmaFileUrlOrId, string accessToken)
        {



            OperationCallResult<AppEsitePagesDto> aOperationCallResult = new OperationCallResult<AppEsitePagesDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                accessToken = AppTenantSettingBL.GetStringValue(EmTenantSettings.FigmaPersonalAccessToken);
            }


            if (string.IsNullOrWhiteSpace(accessToken))
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_ImportAppEsiteFigmaTemplate_Error", ValidationItemType.Error, "Invalid Figma Personal Access Token."));
            }
            else
            {


                if (siteId.HasValue && !string.IsNullOrWhiteSpace(figmaFileUrlOrId))
                {
                    string figmaFileId = figmaFileUrlOrId;

                    if (Regex.IsMatch(figmaFileUrlOrId, @"(?:file|share|design)/"))
                    {
                        figmaFileId = GetFigmaFileIdFromUrl(figmaFileUrlOrId);
                    }

                    try
                    {
                        string figmaFileJsonStr = GetFigmaFileJsonString(figmaFileId, accessToken);

                        var figmaFileObj = JsonConvert.DeserializeObject<dynamic>(figmaFileJsonStr);

                        if (figmaFileObj != null && figmaFileObj.name != null)
                        {

                            string siteBasePath = AppEsiteFileBL.GetCompanyWebSiteBaseFolderPath(siteId.Value);

                            string folderPath = siteBasePath + "\\SharedResource\\FigmaTemplate";

                            if (!System.IO.Directory.Exists(folderPath))
                            {
                                System.IO.Directory.CreateDirectory(folderPath);
                            }

                            string importFileName = AppEsiteFileBL.ConvertToValidFileName(figmaFileId + "___" + figmaFileObj.name + ".json");
                            string importFilePath = Path.Combine(folderPath, importFileName).Replace("\\\\","\\");

                            figmaFileObj.AppOrgFigmaPersonalAccessToken = accessToken;
                            figmaFileObj.AppOrgFigmaFileId = figmaFileId;
                            figmaFileObj.AppFileName = importFileName;
                            figmaFileObj.AppFilePath = importFilePath;

                            string fileContent = JsonConvert.SerializeObject(figmaFileObj, Formatting.Indented);

                            File.WriteAllText(importFilePath, fileContent);

                            AppEsitePagesDto toReturn = new AppEsitePagesDto();
                            toReturn.LogicKey = figmaFileId;
                            toReturn.FileCode = importFileName;
                            toReturn.ComponentName = figmaFileObj.name;
                            toReturn.FileFullPath = importFilePath;

                            aOperationCallResult.Object = toReturn;



                            //GenerateImageAndFontFromImportedFigmaTemplate_ProcessFigmaFileObject(aValidationResult, siteId.Value, figmaFileId, figmaFileObj, accessToken);



                            aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_ImportAppEsiteFigmaTemplate_Ok", ValidationItemType.Message, "Import Figma File Success."));
                        }
                        else
                        {
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_ImportAppEsiteFigmaTemplate_Error", ValidationItemType.Error, "Import Figma File Failed."));
                        }
                    }
                    catch (Exception ex)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_ImportAppEsiteFigmaTemplate_Error", ValidationItemType.Error, "Import Figma File Failed. Please verify the figma url and access token. \n"
                            //+ ex.ToString()
                            ));
                    }

                }
            }

            return aOperationCallResult;
        }



        public static OperationCallResult<bool> GenerateImageAndFontFromImportedFigmaTemplate(AppEsitePagesDto figmaTemplateDto)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;


            bool isSuccess = false;

            if (figmaTemplateDto != null)
            {
                int? siteId = figmaTemplateDto.EsiteId;
                string fileFullPath = figmaTemplateDto.FileFullPath;
                dynamic figmaFileObj = GetOneImportedFigmaTemplate(siteId, fileFullPath);

                if (figmaFileObj != null)
                {
                    string figmaFileId = figmaFileObj.AppOrgFigmaFileId;
                    string accessToken = figmaFileObj.AppOrgFigmaPersonalAccessToken;
                    if (!string.IsNullOrWhiteSpace(figmaFileId) && !string.IsNullOrWhiteSpace(accessToken))
                    {

                        GenerateImageAndFontFromImportedFigmaTemplate_ProcessFigmaFileObject(aValidationResult, siteId.Value, figmaFileId, figmaFileObj, accessToken);
                        if (!aValidationResult.HasErrors)
                        {
                            isSuccess = true;
                        }

                    }
                }
            }
            if (isSuccess)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_GenerateImageAndFontFromImportedFigmaTemplate_Ok", ValidationItemType.Message, "Generate Fonts And Images Completed."));
            }
            else
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_GenerateImageAndFontFromImportedFigmaTemplate_Error", ValidationItemType.Error, "Generate Fonts And Images Completed With Error."));
            }



            return aOperationCallResult;
        }

        private static void GenerateImageAndFontFromImportedFigmaTemplate_ProcessFigmaFileObject(ValidationResult aValidationResult, int siteId, string figmaFileId, dynamic figmaFileObj, string accessToken)
        {
            try
            {
                string siteBasePath = AppEsiteFileBL.GetCompanyWebSiteBaseFolderPath(siteId);

                string imageFolderPath = siteBasePath + "\\public\\img";

                if (!System.IO.Directory.Exists(imageFolderPath))
                {
                    System.IO.Directory.CreateDirectory(imageFolderPath);
                }

                List<string> fontFamilyList = new List<string>();
                Dictionary<string, dynamic> dictRegularImageCodeAndNodeObj = new Dictionary<string, dynamic>();
                Dictionary<string, dynamic> dictSvgImageCodeAndNodeObj = new Dictionary<string, dynamic>();

                if (figmaFileObj.document != null && figmaFileObj.document.children != null)
                {
                    foreach (var canvas in figmaFileObj.document.children)
                    {
                        if (canvas.type == "CANVAS" && canvas.children != null)
                        {
                            foreach (var frame in canvas.children)
                            {
                                if (frame.type == "FRAME")
                                {
                                    try
                                    {

                                        FindFigmaNodeTreeImageAndFont(aValidationResult, frame, fontFamilyList, dictRegularImageCodeAndNodeObj, dictSvgImageCodeAndNodeObj);


                                        //FindAllUsedFontFamilies(aValidationResult, frame, ref fontFamilyList);                                       

                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                }
                            }
                        }
                    }
                }

                //fontFamilyList.Add("abc def");

                if (fontFamilyList.Count > 0)
                {
                    UpdateFontFamiliesOnRootLayoutFileByFontList(aValidationResult, siteId, fontFamilyList);

                    var siteExDto = AppNextJsAppConfigBL.RetrieveOneNextJsAppExDto(siteId);
                    if (siteExDto != null && siteExDto.EsiteAttribute != null)
                    {
                        if (siteExDto.EsiteAttribute.FontFamilyList == null)
                        {
                            siteExDto.EsiteAttribute.FontFamilyList = new List<string>();
                        }

                        siteExDto.EsiteAttribute.FontFamilyList.AddRange(fontFamilyList);
                        siteExDto.EsiteAttribute.FontFamilyList = siteExDto.EsiteAttribute.FontFamilyList.Distinct().ToList();

                        var updateSiteRresult = AppNextJsAppConfigBL.UpdateNextJsApp(siteExDto);

                        if (updateSiteRresult.ValidationResult.HasErrors)
                        {
                            aValidationResult.Merge(updateSiteRresult.ValidationResult);
                        }
                    }

                }

                if (dictRegularImageCodeAndNodeObj.Count > 0)
                {
                    DownloadFigmaTemplateImages(aValidationResult, figmaFileId, accessToken, imageFolderPath, dictRegularImageCodeAndNodeObj, "png");
                }

                if (dictSvgImageCodeAndNodeObj.Count > 0)
                {
                    DownloadFigmaTemplateImages(aValidationResult, figmaFileId, accessToken, imageFolderPath, dictSvgImageCodeAndNodeObj, "svg");
                }
            }
            catch (Exception ex)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_GenerateImageAndFontFromImportedFigmaTemplate_ProcessFigmaFileObject_Error", ValidationItemType.Error, "Generate figma font and image error. " + ex.ToString()));
            }
        }

        private static void DownloadFigmaTemplateImages(ValidationResult aValidationResult, string figmaFileId, string accessToken, string imageFolderPath, Dictionary<string, dynamic> dictImageCodeAndNodeObj, string format)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-Figma-Token", accessToken);

                List<dynamic> imageNodeList = dictImageCodeAndNodeObj.Values.ToList();

                List<string> nodeIds = imageNodeList.Where(o => o.id != null).Select(o => (string)o.id).Distinct().ToList();
                Dictionary<string, dynamic> dictImageNodeIdAndDto = imageNodeList.Where(o => o.id != null).ToDictionary(o => (string)o.id, o => o);


                int maxNbSimulatedApiCallTasks = 20;
                List<Task> tasks = new List<Task>();

                foreach (var batch in SplitNodeIdsIntoBatches(nodeIds, figmaFileId, format))
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        string url = batch;
                        try
                        {
                            HttpResponseMessage response = await client.GetAsync(url);

                            if (response.IsSuccessStatusCode)
                            {
                                var responseBody = await response.Content.ReadAsStringAsync();
                                var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseBody);

                                DownloadFigmaTemplateImagesFromApi_SaveBatchFiles(client, format, imageFolderPath, dictImageNodeIdAndDto, jsonResponse);
                            }
                            else
                            {
                                // Handle unsuccessful response
                            }
                        }
                        catch (Exception ex)
                        {
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_DownloadFigmaTemplateImages_Error", ValidationItemType.Error,
                                "Import figma image error. " + ex.Message));
                        }
                    }));

                    if (tasks.Count == maxNbSimulatedApiCallTasks)
                    {
                        Task.WhenAll(tasks).GetAwaiter().GetResult();
                        tasks.Clear();
                    }
                }


                if (tasks.Count > 0)
                {
                    Task.WhenAll(tasks).GetAwaiter().GetResult();
                }
            }
        }



        private static void FindFigmaNodeTreeImageAndFont(ValidationResult aValidationResult, dynamic node,
            List<string> fontFamilyList, Dictionary<string, dynamic> dictRegularImageCodeAndNodeObj, Dictionary<string, dynamic> dictSvgImageCodeAndNodeObj)
        {
            if (node == null)
                return;

            try
            {
                string fontFamily = GetNodeFontFamil(node);

                if (!string.IsNullOrWhiteSpace(fontFamily))
                {
                    if (!fontFamilyList.Contains(fontFamily))
                    {
                        fontFamilyList.Add(fontFamily);
                    }
                }


                if (IsNodeSvg(node, out string svgImageCode))
                {
                    if (!string.IsNullOrWhiteSpace(svgImageCode))
                    {
                        //svgImageCode = svgImageCode.Replace(":", "_");

                        if (!dictSvgImageCodeAndNodeObj.ContainsKey(svgImageCode))
                        {
                            node.AppImageCode = svgImageCode;
                            dictSvgImageCodeAndNodeObj.Add(svgImageCode, node);
                        }

                        //if (!imgRefList.Contains(svgImageCode))
                        //{                           
                        //    using (HttpClient client = new HttpClient())
                        //    {
                        //        string imageUrl = DownloadFigmaNodeImages_GetImageUrl(client, figmaFileId, node.id.ToString(), accessToken, "svg");

                        //        byte[] imageData = DownloadImage(client, imageUrl);

                        //        string localFilePath = Path.Combine(downloadToFolderPath, svgImageCode + ".svg");
                        //        SaveImageToFile(imageData, localFilePath);

                        //    }

                        //}
                    }
                }
                else if (IsImageFill(node, out string imageRef))
                {

                    if (!string.IsNullOrWhiteSpace(imageRef))
                    {

                        if (!dictRegularImageCodeAndNodeObj.ContainsKey(imageRef))
                        {
                            node.AppImageCode = imageRef;
                            dictRegularImageCodeAndNodeObj.Add(imageRef, node);
                        }
                    }

                    //if (!imgRefList.Contains(imageRef))
                    //{
                    //    //aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_DownloadFigmaNodeImages_Start", ValidationItemType.Message, "App_DownloadFigmaNodeImages_Start:" + imageRef));


                    //    using (HttpClient client = new HttpClient())
                    //    {
                    //        string imageUrl = DownloadFigmaNodeImages_GetImageUrl(client, figmaFileId, node.id.ToString(), accessToken, "png");


                    //        byte[] imageData = DownloadImage(client, imageUrl);


                    //        string localFilePath = Path.Combine(downloadToFolderPath, imageRef + ".png");
                    //        SaveImageToFile(imageData, localFilePath);
                    //        //aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_DownloadFigmaNodeImages_Error", ValidationItemType.Message, "App_DownloadFigmaNodeImages_End:" + imageRef));

                    //    }



                    //}
                }
            }
            catch (Exception ex)
            {
                //aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_DownloadFigmaNodeImages_Error", ValidationItemType.Message, "App_DownloadFigmaNodeImages_Error:" + imageRef + ". " + ex.Message));
            }

            var children = node.children;
            if (children != null)
            {
                foreach (var child in children)
                {
                    FindFigmaNodeTreeImageAndFont(aValidationResult, child, fontFamilyList, dictRegularImageCodeAndNodeObj, dictSvgImageCodeAndNodeObj);
                }
            }
        }

        private static bool IsImageFill(JObject node, out string imageRef)
        {
            imageRef = null;

            if (node["fills"] != null)
            {
                foreach (var fill in node["fills"])
                {
                    if (fill["type"]?.ToString() == "IMAGE")
                    {
                        imageRef = fill["imageRef"]?.ToString();
                        return true;
                    }
                }
            }
            return false;
        }


        private static bool IsNodeSvg(dynamic node, out string svgImageCode)
        {
            svgImageCode = null;

            if (node.type == "VECTOR"
                || node.type == "ELLIPSE"
                || node.type == "POLYGON"
                || node.type == "LINE"
                || node.type == "STAR"
                || node.type == "BOOLEAN_OPERATION")
            {
                svgImageCode = GetSvgImageCodeFromFigmaNodeId(node);

                return true;
            }
            return false;
        }

        private static string GetSvgImageCodeFromFigmaNodeId(dynamic node)
        {
            if (node != null && node.id != null)
            {
                string nodeId = node.id;
                //var partList = nodeId.Split(';');
                //string lastPart = partList[partList.Length - 1];

                //return lastPart;

                string imageCode = nodeId.Replace(":", "_").Replace(";", "_");
                return imageCode;
            }

            return "";
        }



        private static void DownloadFigmaTemplateImagesFromApi_SaveBatchFiles(HttpClient client, string format, string imageFolderPath, Dictionary<string, dynamic> dictImageNodeIdAndDto, dynamic jsonResponse)
        {
            if (jsonResponse["images"] != null)
            {
                foreach (var image in (JObject)jsonResponse["images"])
                {
                    string nodeId = image.Key;
                    string imageUrl = image.Value.ToString();

                    if (!string.IsNullOrWhiteSpace(imageUrl)
                        && !string.IsNullOrWhiteSpace(nodeId) && !string.IsNullOrWhiteSpace(nodeId) && dictImageNodeIdAndDto.ContainsKey(nodeId))
                    {
                        var node = dictImageNodeIdAndDto[nodeId];
                        dictImageNodeIdAndDto[nodeId].AppImageDownloadUrl = imageUrl;
                        string imageCode = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(node.AppImageCode);

                        if (!string.IsNullOrWhiteSpace(imageCode))
                        {
                            byte[] imageData = DownloadImage(client, imageUrl);
                            string localFilePath = Path.Combine(imageFolderPath, imageCode + "." + format);
                            SaveImageDataToFile(imageData, localFilePath);

                        }

                    }
                }
            }
        }

        private static IEnumerable<string> SplitNodeIdsIntoBatches(List<string> nodeIds, string fileKey, string format)
        {
            int MaxUrlLength = 1900;
            var batchSize = 50;
            var baseUrl = $"https://api.figma.com/v1/images/{fileKey}?format={format}&ids=";

            for (int i = 0; i < nodeIds.Count; i += batchSize)
            {
                var batch = nodeIds.GetRange(i, Math.Min(batchSize, nodeIds.Count - i));
                string url = baseUrl + string.Join(",", batch);


                while (url.Length > MaxUrlLength && batchSize > 1)
                {
                    batchSize--;
                    batch = nodeIds.GetRange(i, Math.Min(batchSize, nodeIds.Count - i));
                    url = baseUrl + string.Join(",", batch);
                }

                yield return url;
            }
        }

        private static string ExtractImageUrl(string responseBody, string nodeId)
        {
            var jsonResponse = JObject.Parse(responseBody);
            return jsonResponse["images"]?[nodeId]?.ToString();
        }

        private static byte[] DownloadImage(HttpClient client, string imageUrl)
        {
            HttpResponseMessage response = client.GetAsync(imageUrl).Result;
            response.EnsureSuccessStatusCode();
            return response.Content.ReadAsByteArrayAsync().Result;
        }

        private static void SaveImageDataToFile(byte[] imageData, string filePath)
        {
            File.WriteAllBytes(filePath, imageData);
        }

        private static string GetFigmaFileIdFromUrl(string url)
        {
            var regex = new Regex(@"(?:file|share|design)/([a-zA-Z0-9]+)", RegexOptions.IgnoreCase);
            var match = regex.Match(url);

            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private static string GetFigmaFileJsonString(string fileId, string accessToken)
        {
            ////exception: "The request was aborted: Could not create SSL/TLS secure channel."	

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls12;

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-Figma-Token", accessToken);


                //client.DefaultRequestHeaders.Add("Content-Type", "application/json");

                string apiUrl = $"https://api.figma.com/v1/files/{fileId}";

                var response = client.GetStringAsync(apiUrl).GetAwaiter().GetResult();

                return response;

            }

            //string apiUrl = $"https://api.figma.com/v1/files/{fileId}";

            //return CallFigmaGetApi(apiUrl, accessToken);
        }



        private static string CallFigmaGetApi(string baseUrl, string accessToken)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var client = new RestClient(baseUrl);

            var request = new RestRequest();
            request.AddHeader("X-Figma-Token", accessToken);

            // Execute the request
            RestResponse response = client.ExecuteAsync(request).GetAwaiter().GetResult();

            if (!string.IsNullOrWhiteSpace(response.ErrorMessage))
            {
                throw new Exception($"Error calling API: {response.StatusCode} - {response.ErrorMessage}");
            }
            else
            {
                return response.Content;

            }
        }

        //public static string GetFigmaFileJsonString(string fileId, string accessToken = _figmaApiAccessToken)
        //{
        //    // Configure SSL/TLS settings
        //    System.Net.ServicePointManager.Expect100Continue = true;
        //    System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

        //    using (HttpClient client = new HttpClient())
        //    {
        //        // Add the authorization header
        //        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        //        // Construct the API URL
        //        string apiUrl = $"https://api.figma.com/v1/files/{fileId}";


        //        // Send the request and get the response
        //        HttpResponseMessage response = client.GetAsync(apiUrl).GetAwaiter().GetResult();

        //        // Ensure the request was successful
        //        response.EnsureSuccessStatusCode();

        //        // Read the response body
        //        string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        //        return responseBody;

        //    }
        //}



        private static string ConvertFigmaToHtml(dynamic documentNode, double? sizeScale = null)
        {
            var htmlBuilder = new StringBuilder();
            ConvertNodeToHtml(documentNode, htmlBuilder, 0, 0, 0, true, sizeScale);
            return htmlBuilder.ToString();
        }

        private static double GetScaledSizeValue(object orgSize, double? sizeScale, int nbDecimal)
        {
            double? orgSizeValue = ControlTypeValueConverter.ConvertValueToDouble(orgSize);
            double toReturn = orgSizeValue.HasValue ? orgSizeValue.Value : 0;

            if (!sizeScale.HasValue || sizeScale.Value <= 0)
            {
                sizeScale = 1;
            }

            toReturn = Math.Round((toReturn * sizeScale.Value), nbDecimal);

            return toReturn;
        }

        private static dynamic GetFigmaNodeDefaultFill(dynamic fills)
        {
            dynamic defaultFill = null;

            if (fills != null && fills.Count > 0)
            {
                for (int i = fills.Count - 1; i >= 0; i--)
                {
                    var aFill = fills[i];

                    if (aFill.visible != null && aFill.visible == false)
                    {
                        continue;
                    }
                    else
                    {
                        defaultFill = aFill;
                        break;
                    }
                }
            }

            return defaultFill;
        }

        private static void ConvertNodeToHtml(dynamic node, StringBuilder htmlBuilder, int indentLevel, double parentCanvasPositionX, double parentCanvasPositionY, bool isRootDomOnPosition0, double? sizeScale)
        {
            var indent = new string(' ', indentLevel * 2);

            if (node == null)
                return;

            var type = node.type;
            var name = node.name;
            var styles = node.styles;
            var fills = node.fills;
            var layout = node.absoluteBoundingBox;


            double canvasPositionX = layout?.x ?? 0;
            double canvasPositionY = layout?.y ?? 0;

            //canvasPositionX = GetScaledSizeValue(canvasPositionX, sizeScale, 2);
            //canvasPositionX = GetScaledSizeValue(canvasPositionX, sizeScale, 2);


            double htmlPositionX = canvasPositionX - parentCanvasPositionX;
            double htmlPositionY = canvasPositionY - parentCanvasPositionY;

            if (isRootDomOnPosition0)
            {
                htmlPositionX = 0;
                htmlPositionY = 0;
            }

            dynamic defaultFill = GetFigmaNodeDefaultFill(fills);


            if (defaultFill != null && defaultFill.type == "IMAGE")
            {
                string imageRef = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(defaultFill.imageRef);
                string imageUrl = ConvertNodeToHtml_GetImageUrl(imageRef);
                htmlBuilder.AppendLine($"{indent}<img src='{imageUrl}' style='{ConvertFigmaNodeToHtml_GetStyle(styles, layout, htmlPositionX, htmlPositionY, fills, node, isRootDomOnPosition0, sizeScale)}' alt='{name}' />");

                var children = node.children;
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        ConvertNodeToHtml(child, htmlBuilder, indentLevel + 1, canvasPositionX, canvasPositionY, false, sizeScale);
                    }
                }
            }
            else if (IsNodeSvg(node, out string svgImageCode) && !string.IsNullOrWhiteSpace(svgImageCode))
            {
                //svgImageCode = svgImageCode.Replace(":", "_");               
                string imageUrl = $"/img/{svgImageCode}.svg";
                htmlBuilder.AppendLine($"{indent}<img src='{imageUrl}' style='{ConvertFigmaNodeToHtml_GetStyle(styles, layout, htmlPositionX, htmlPositionY, fills, node, isRootDomOnPosition0, sizeScale)}' alt='{name}' />");

                var children = node.children;
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        ConvertNodeToHtml(child, htmlBuilder, indentLevel + 1, canvasPositionX, canvasPositionY, false, sizeScale);
                    }
                }
            }
            else
            {
                if (type == "FRAME" || type == "INSTANCE" || type == "RECTANGLE" || type == "Content")
                {
                    htmlBuilder.AppendLine($"{indent}<div style='{ConvertFigmaNodeToHtml_GetStyle(styles, layout, htmlPositionX, htmlPositionY, fills, node, isRootDomOnPosition0, sizeScale)}'>");
                }
                else if (type == "TEXT")
                {
                    string htmlText = node.characters;

                    //htmlText = htmlText.ReplaceAll(" ", "&nbsp;");

                    htmlText = htmlText.Replace("\n", "<br/>");

                    htmlBuilder.AppendLine($"{indent}<div style='overflow: hidden; {ConvertFigmaNodeToHtml_GetStyle(styles, layout, htmlPositionX, htmlPositionY, fills, node, isRootDomOnPosition0, sizeScale)}'>{htmlText}");
                }
                else
                {
                    htmlBuilder.AppendLine($"{indent}<div style='{ConvertFigmaNodeToHtml_GetStyle(styles, layout, htmlPositionX, htmlPositionY, fills, node, isRootDomOnPosition0, sizeScale)}'>");
                }

                // Recursively handle child nodes
                var children = node.children;
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        ConvertNodeToHtml(child, htmlBuilder, indentLevel + 1, canvasPositionX, canvasPositionY, false, sizeScale);
                    }
                }

                // Close the HTML tags
                //if (type == "FRAME" || type == "RECTANGLE" || type == "INSTANCE" || type == "Content" || type == "VECTOR")
                //{
                htmlBuilder.AppendLine($"{indent}</div>");
                //}
            }
        }

        private static string ConvertNodeToHtml_GetImageUrl(string imageRef)
        {
            string baseUrl = "/img/";


            string imageUrl = $"{baseUrl}{imageRef}.png";

            return imageUrl;
        }

        private static string ConvertFigmaNodeToHtml_GetStyle(dynamic styles, dynamic layout, double x, double y, dynamic fills, dynamic node, bool isRootDomOnPosition0, double? sizeScale)
        {
            var styleBuilder = new StringBuilder();

            try
            {
                if (node != null)
                {
                    // Handle position and size
                    ConvertFigmaNodeToHtml_GetStyle_HandlePositionAndSIze(layout, x, y, styleBuilder, isRootDomOnPosition0, sizeScale);

                    if (IsNodeSvg(node, out string svgImageCode) == false)
                    {
                        // Handle fills for background color
                        ConvertFigmaNodeToHtml_GetStyle_HandleBackground(fills, node, styleBuilder);

                        // Handle border styles from strokes
                        ConvertFigmaNodeToHtml_GetStyle_HandleBorder(node, styleBuilder, sizeScale);

                        // Handle effects (e.g., drop shadow)
                        ConvertFigmaNodeToHtml_GetStyle_HandleEffect(node, styleBuilder);

                        // Handle additional styles
                        ConvertFigmaNodeToHtml_GetStyle_HandleOtherProperties(node, styleBuilder, sizeScale);
                    }
                }

            }
            catch (Exception ex)
            {
                // Handle exception
            }

            return styleBuilder.ToString().Trim();
        }

        private static void ConvertFigmaNodeToHtml_GetStyle_HandleOtherProperties(dynamic node, StringBuilder styleBuilder, double? sizeScale)
        {
            var backgroundColor = node.backgroundColor;
            var rotation = node.rotation;
            var fontSize = node.style?.fontSize;
            var fontFamily = node.style?.fontFamily;
            var fontWeight = node.style?.fontWeight;
            var textAlignHorizontal = node.style?.textAlignHorizontal;
            var textAlignVertical = node.style?.textAlignVertical;
            var lineHeightPx = node.style?.lineHeightPx;
            var letterSpacing = node.style?.letterSpacing;
            bool? clicpContent = ControlTypeValueConverter.ConvertValueToBoolean(node.clipsContent);
            bool? isVisible = ControlTypeValueConverter.ConvertValueToBoolean(node.visible);

            if (backgroundColor != null)
            {
                if (!styleBuilder.ToString().Contains("background-color"))
                {
                    styleBuilder.Append($"background-color: rgba({(int)(backgroundColor.r * 255)},{(int)(backgroundColor.g * 255)},{(int)(backgroundColor.b * 255)},{ConvertToDecimal(backgroundColor.a, 4)}); ");
                }
            }                

            if (fontSize != null)
            {
                double fontSizeDouble = GetScaledSizeValue(fontSize, sizeScale, 0);
                if (fontSizeDouble > 0)
                {
                    styleBuilder.Append($"font-size: {fontSizeDouble}px; ");
                }
            }

            if (fontFamily != null)
                styleBuilder.Append($"font-family: {fontFamily}; ");

            if (fontWeight != null)
                styleBuilder.Append($"font-weight: {fontWeight}; ");

            if (textAlignHorizontal != null)
                styleBuilder.Append($"text-align: {textAlignHorizontal.ToString().ToLower()}; ");

            if (textAlignVertical != null)
            {
                //if (!styleBuilder.ToString().Contains("display:"))
                //{
                //    styleBuilder.Append($"display: flex; ");
                //    styleBuilder.Append($"align-items: {textAlignVertical.ToString().ToLower()}; ");
                //}
            }

            if (lineHeightPx != null)
            {
                double lineHeightPxDouble = GetScaledSizeValue(lineHeightPx, sizeScale, 0);
                if (lineHeightPxDouble > 0)
                {  
                    styleBuilder.Append($"line-height: {lineHeightPxDouble}px; min-height: {lineHeightPxDouble}px; ");
                }
            }


            if (letterSpacing != null)
            {
                double letterSpacingDouble = GetScaledSizeValue(letterSpacing, sizeScale, 0);
                if (letterSpacingDouble > 0)
                {
                    styleBuilder.Append($"letter-spacing: {letterSpacingDouble}px; ");
                }
            }

            if (clicpContent.HasValue && clicpContent.Value)
            {
                styleBuilder.Append($"overflow: hidden; ");
            }

            if (isVisible.HasValue && !isVisible.Value)
            {
                styleBuilder.Append($"display: none; ");
            }
        }

        private static void ConvertFigmaNodeToHtml_GetStyle_HandleEffect(dynamic node, StringBuilder styleBuilder)
        {
            if (node.type == "TEXT")
            {
                return;
            }

            if (node.effects != null && node.effects.Count > 0)
            {
                var cssBoxShadows = new List<string>();

                foreach (var effect in node.effects)
                {
                    if (effect.type == "DROP_SHADOW" && effect.visible == true)
                    {
                        // Extract RGBA color
                        var color = effect.color;
                        var r = (int)(color.r * 255);
                        var g = (int)(color.g * 255);
                        var b = (int)(color.b * 255);
                        var a = ConvertToDecimal(color.a, 4);

                        // Extract offset and radius
                        var offsetX = ConvertToDecimal(effect.offset.x, 2);
                        var offsetY = ConvertToDecimal(effect.offset.y, 2);
                        var blurRadius = ConvertToDecimal(effect.radius, 2);

                        // Construct box-shadow CSS
                        var boxShadow = $"{offsetX}px {offsetY}px {blurRadius}px rgba({r},{g},{b},{a})";
                        cssBoxShadows.Add(boxShadow);
                    }
                }

                if (cssBoxShadows.Count > 0)
                {
                    styleBuilder.Append($"box-shadow: {string.Join(", ", cssBoxShadows)}; ");
                }
            }
        }

        private static void ConvertFigmaNodeToHtml_GetStyle_HandleBorder(dynamic node, StringBuilder styleBuilder, double? sizeScale)
        {
            if (node.type == "TEXT")
            {
                return;
            }

            if (node.strokes != null && node.strokes.Count > 0)
            {
                var borderWidth = node.strokeWeight ?? 0;
                var borderColor = node.strokes[0].color;

                // Border width
                double borderWidthDouble = GetScaledSizeValue(borderWidth, sizeScale, 1);
                if (borderWidth > 0)
                {
                    styleBuilder.Append($"border-width: {borderWidthDouble}px; ");
                }

                // Border color
                if (borderColor != null)
                {
                    styleBuilder.Append($"border-color: rgba({(int)(borderColor.r * 255)}, {(int)(borderColor.g * 255)}, {(int)(borderColor.b * 255)}, {ConvertToDecimal(borderColor.a, 4)}); ");
                }

                // Border style (assuming solid)
                styleBuilder.Append("border-style: solid; ");
            }

            // Handle border-radius from cornerRadius
            if (node.cornerRadius != null)
            {
                double radius = node.cornerRadius;
                radius = GetScaledSizeValue(radius, sizeScale, 0);
                if (radius > 0)
                {
                    styleBuilder.Append($"border-radius: {radius}px; ");
                }
            }
            // Handle different radius for each corner using top/bottom/left/right radii
            else if (node.topLeftRadius != null || node.topRightRadius != null || node.bottomLeftRadius != null || node.bottomRightRadius != null)
            {
                double topLeftRadius = (node.topLeftRadius ?? 0);
                double topRightRadius = (node.topRightRadius ?? 0);
                double bottomLeftRadius = (node.bottomLeftRadius ?? 0);
                double bottomRightRadius = (node.bottomRightRadius ?? 0);

                topLeftRadius = GetScaledSizeValue(topLeftRadius, sizeScale, 0);
                topRightRadius = GetScaledSizeValue(topRightRadius, sizeScale, 0);
                bottomLeftRadius = GetScaledSizeValue(bottomLeftRadius, sizeScale, 0);
                bottomRightRadius = GetScaledSizeValue(bottomRightRadius, sizeScale, 0);

                styleBuilder.Append($"border-radius: {topLeftRadius}px {topRightRadius}px {bottomRightRadius}px {bottomLeftRadius}px; ");
            }
            // Handle rectangleCornerRadii property
            else if (node.rectangleCornerRadii != null && node.rectangleCornerRadii.Count == 4)
            {
                double topLeftRadius = node.rectangleCornerRadii[0];
                double topRightRadius = node.rectangleCornerRadii[1];
                double bottomRightRadius = node.rectangleCornerRadii[2];
                double bottomLeftRadius = node.rectangleCornerRadii[3];

                topLeftRadius = GetScaledSizeValue(topLeftRadius, sizeScale, 0);
                topRightRadius = GetScaledSizeValue(topRightRadius, sizeScale, 0);
                bottomLeftRadius = GetScaledSizeValue(bottomLeftRadius, sizeScale, 0);
                bottomRightRadius = GetScaledSizeValue(bottomRightRadius, sizeScale, 0);

                // Set border-radius for each corner
                styleBuilder.Append($"border-radius: {topLeftRadius}px {topRightRadius}px {bottomRightRadius}px {bottomLeftRadius}px; ");
            }
        }

        private static void ConvertFigmaNodeToHtml_GetStyle_HandleBackground(dynamic fills, dynamic node, StringBuilder styleBuilder)
        {
            dynamic defaultFill = GetFigmaNodeDefaultFill(fills);


            if (defaultFill != null && defaultFill.color != null)
            {
                

                if (node.type == "TEXT")
                {
                    var textColor = defaultFill.color;
                    styleBuilder.Append($"color: rgba({(int)(textColor.r * 255)}, {(int)(textColor.g * 255)}, {(int)(textColor.b * 255)}, {ConvertToDecimal(textColor.a, 4)}); ");
                }
                else
                {
                    var fillColor = defaultFill.color;

                    decimal opacity = ConvertToDecimal(fillColor.a, 4);

                    if (defaultFill.opacity != null)
                    {
                        opacity = ConvertToDecimal(defaultFill.opacity, 4);
                        

                    }


                    styleBuilder.Append($"background-color: rgba({(int)(fillColor.r * 255)}, {(int)(fillColor.g * 255)}, {(int)(fillColor.b * 255)}, {opacity}); ");
                }
            }





        }

        private static void ConvertFigmaNodeToHtml_GetStyle_HandlePositionAndSIze(dynamic layout, double x, double y, StringBuilder styleBuilder, bool isRootDomOnPosition0, double? sizeScale)
        {
            if (layout != null)
            {
                var width = layout.width;
                var height = layout.height;

                if (isRootDomOnPosition0)
                {
                    styleBuilder.Append($"position: relative; ");
                }
                else
                {
                    x = GetScaledSizeValue(x, sizeScale, 0);
                    y = GetScaledSizeValue(y, sizeScale, 0);
                    styleBuilder.Append($"position: absolute; left: {ConvertToDecimal(x, 2)}px; top: {ConvertToDecimal(y, 2)}px; ");
                }

                if (width != null)
                {
                    double widthValue = GetScaledSizeValue(width, sizeScale, 0);
                    if (widthValue > 0)
                    {
                        styleBuilder.Append($"width: {widthValue}px; ");
                    }

                }

                if (height != null)
                {
                    double heightValue = GetScaledSizeValue(height, sizeScale, 0);
                    if (heightValue > 0)
                    {

                        var lineHeightPx = layout.lineHeightPx;

                        if (lineHeightPx != null)
                        {
                            double lineHeightPxDouble = GetScaledSizeValue(lineHeightPx, sizeScale, 0);
                            if (lineHeightPxDouble > heightValue)
                            {
                                heightValue = lineHeightPxDouble;
                            }
                        }

                        styleBuilder.Append($"height: {heightValue}px; ");
                    }
                }

            }
        }



        //private static void FindAllUsedFontFamilies(ValidationResult aValidationResult, dynamic node, ref List<string> fontFamilyList)
        //{
        //    if (node == null)
        //        return;

        //    string fontFamily = GetNodeFontFamil(node);

        //    if (!string.IsNullOrWhiteSpace(fontFamily))
        //    {
        //        if (!fontFamilyList.Contains(fontFamily))
        //        {
        //            fontFamilyList.Add(fontFamily);
        //        }
        //    }

        //    var children = node.children;

        //    if (children != null)
        //    {
        //        foreach (var child in children)
        //        {
        //            FindAllUsedFontFamilies(aValidationResult, child, ref fontFamilyList);
        //        }
        //    }
        //}

        private static string GetNodeFontFamil(dynamic node)
        {
            string fontFamily = "";
            try
            {
                fontFamily = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(node.style?.fontFamily);
                fontFamily = fontFamily.Trim();
            }
            catch (Exception ex)
            {

            }


            return fontFamily.Trim();
        }

        private static void UpdateFontFamiliesOnRootLayoutFileByFontList(ValidationResult aValidationResult, int siteId, List<string> fontFamilyList)
        {
            try
            {
                string siteBasePath = AppEsiteFileBL.GetCompanyWebSiteBaseFolderPath(siteId);

                string filePath = Path.Combine(siteBasePath + "src\\app\\layout.tsx");

                if (File.Exists(filePath))
                {

                    var lines = File.ReadAllLines(filePath).ToList();

                    // Find the line: const fontFamilies: string[] = ...
                    var fontFamiliesPattern = @"^const\s+fontFamilies:\s+string\[\]\s+=\s+\[(.*)\];";
                    var fontFamiliesRegex = new Regex(fontFamiliesPattern);

                    for (int i = 0; i < lines.Count; i++)
                    {
                        var match = fontFamiliesRegex.Match(lines[i]);
                        if (match.Success)
                        {
                            // Extract the current font families
                            var existingFontFamilies = match.Groups[1].Value
                                .Split(new[] { "\", \"" }, StringSplitOptions.None)
                                .Select(f => f.Trim(' ', '"'))
                                .ToList();

                            // Add new font families if they don't already exist
                            foreach (var fontFamily in fontFamilyList)
                            {
                                if (!existingFontFamilies.Contains(fontFamily))
                                {
                                    existingFontFamilies.Add(fontFamily);
                                }
                            }

                            // Create the updated line
                            var updatedLine = $"const fontFamilies: string[] = [\"{string.Join("\", \"", existingFontFamilies)}\"];";
                            lines[i] = updatedLine;
                            break;
                        }
                    }

                    File.WriteAllLines(filePath, lines);
                }
            }
            catch (Exception ex)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_GenerateImageAndFontFromImportedFigmaTemplate_ProcessFigmaFileObject_Error", ValidationItemType.Error,
                    "Update font error. " + ex.Message));
            }

        }




        private static dynamic GetOneImportedFigmaTemplate(int? siteId, string fileFullPath)
        {
            if (siteId.HasValue && !string.IsNullOrWhiteSpace(fileFullPath))
            {

                string siteBasePath = AppEsiteFileBL.GetCompanyWebSiteBaseFolderPath(siteId.Value);

                string folderPath = siteBasePath + "\\SharedResource\\FigmaTemplate";

                if (!System.IO.Directory.Exists(folderPath))
                {
                    System.IO.Directory.CreateDirectory(folderPath);
                }

                DirectoryInfo d = new DirectoryInfo(folderPath);//Assuming Test is your Folder
                FileInfo[] files = d.GetFiles(); //Getting Text files

                var fileInfo = files.FirstOrDefault(o => o.FullName == fileFullPath);

                if (fileInfo != null)
                {
                    string figmaFileJsonStr = File.ReadAllText(fileInfo.FullName);

                    try
                    {


                        dynamic figmaFileObj = JsonConvert.DeserializeObject<dynamic>(figmaFileJsonStr);
                        return figmaFileObj;

                    }
                    catch (Exception ex)
                    {

                    }
                }
            }

            return null;
        }

        private static void PrepareEsitePageDtoFromFigmaTemplateObject(List<AppEsitePagesDto> toReturn, dynamic figmaFileObj, bool autoConvertInlineStyleToTailwind)
        {
            if (figmaFileObj.document != null && figmaFileObj.document.children != null)
            {
                foreach (var canvas in figmaFileObj.document.children)
                {
                    if (canvas.type == "CANVAS" && canvas.children != null)
                    {
                        foreach (var frame in canvas.children)
                        {
                            if (frame.type == "FRAME")
                            {

                                AppEsitePagesDto convertedHtmlPage = new AppEsitePagesDto();
                                convertedHtmlPage.FileCode = frame.name;
                                convertedHtmlPage.Description = canvas.name;
                                convertedHtmlPage.FigmaCanvasId = canvas.id;
                                convertedHtmlPage.FigmaFrameId = frame.id;

                                //convertedHtmlPage.HtmlContent = ConvertFigmaToHtml(frame);

                                //if (autoConvertInlineStyleToTailwind)
                                //{
                                //    convertedHtmlPage.HtmlContent = AppTailwindHelperBL.ConvertOneHtmlPageInlineStylesToTailwind(convertedHtmlPage.HtmlContent, new TailwindConvertSettingDto());
                                //}

                                toReturn.Add(convertedHtmlPage);

                            }
                        }
                    }
                }
            }
        }

        private static decimal ConvertToDecimal(dynamic obj, int nbDigits)
        {
            decimal? result = ControlTypeValueConverter.ConvertValueToDecimal(obj, nbDigits);

            if (!result.HasValue)
            {
                result = 0;
            }

            return result.Value;
        }




        private static string ConvertHtmlAbsolutePositionToStatic(string html)
        {
            var parser = new HtmlParser();
            var document = parser.ParseDocument(html);

            var bodyElement = document.Body;

            if (bodyElement != null)
            {
                Dictionary<string, PositionData> dictElemIdAndPosition = ConvertAbsoluteToStatic_InitPositionDictionary(bodyElement);


                ConvertAbsoluteToStatic_MoveNodesIntoExistingContainerNodesByPositionAndSize(bodyElement, dictElemIdAndPosition);


                ConvertAbsoluteToStatic_AddHorizontalContainers(document, bodyElement.Children[0], dictElemIdAndPosition);
                ConvertAbsoluteToStatic_AddVerticalContainers(document, bodyElement.Children[0], dictElemIdAndPosition);
                ConvertAbsoluteToStatic_InitParentId(bodyElement.Children[0], dictElemIdAndPosition);

                ConvertAbsoluteToStatic_CalculatePositionAndDispay(bodyElement, bodyElement.Children[0].Id, dictElemIdAndPosition);

                ConvertAbsoluteToStatic_UpdateFinalStylesFromPositionData(bodyElement, dictElemIdAndPosition);

                return bodyElement.InnerHtml;
            }

            return html;
        }

        private static Dictionary<string, PositionData> ConvertAbsoluteToStatic_InitPositionDictionary(IHtmlElement bodyElement)
        {
            Dictionary<string, PositionData> dictElemIdAndPosition = new Dictionary<string, PositionData>();

            foreach (var element in bodyElement.QuerySelectorAll("*"))
            {

                element.Id = ExtensionMethodhelper.RandomId();

                PositionData positionData = new PositionData();
                dictElemIdAndPosition.Add(element.Id, positionData);
                positionData.Id = element.Id;

                var style = element.GetAttribute("style");

                if (!string.IsNullOrEmpty(style))
                {
                    var originalStyles = new Dictionary<string, string>();

                    var styleProperties = style.Split(';')
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToArray();

                    foreach (var propertyValue in styleProperties)
                    {
                        var parts = propertyValue.Split(':');
                        if (parts.Length == 2)
                        {
                            var property = parts[0].Trim();
                            var value = parts[1].Trim();
                            originalStyles[property] = value;

                            if (value.EndsWith("px"))
                            {
                                value = value.Substring(0, value.Length - 2);
                            }

                            if (property == "position")
                            {
                                positionData.Position = value;
                            }
                            else if (property == "width")
                            {
                                positionData.Width = ConvertToDecimal(value, 2);
                                positionData.Width = positionData.Width.HasValue ? positionData.Width.Value : 0;
                            }
                            else if (property == "height")
                            {
                                positionData.Height = ConvertToDecimal(value, 2);
                                positionData.Height = positionData.Height.HasValue ? positionData.Height.Value : 0;
                            }
                            else if (property == "top")
                            {
                                positionData.Top = ConvertToDecimal(value, 2);
                                positionData.Top = positionData.Top.HasValue ? positionData.Top.Value : 0;
                            }
                            else if (property == "left")
                            {
                                positionData.Left = ConvertToDecimal(value, 2);
                                positionData.Left = positionData.Left.HasValue ? positionData.Left.Value : 0;
                            }
                            else if (property == "opacity")
                            {
                                positionData.Opacity = ConvertToDecimal(value, 4);
                            }
                        }
                    }

                    positionData.OriginalStyles = originalStyles;
                }
            }

            return dictElemIdAndPosition;
        }


        private static void ConvertAbsoluteToStatic_MoveNodesIntoExistingContainerNodesByPositionAndSize(IElement element, Dictionary<string, PositionData> dictElemIdAndPosition)
        {
            if (element.Children.Count() > 0)
            {
                if (element.Children.Count() > 1)
                {
                    var childList = element.Children.ToList();


                    bool noMoreChildToProcess = false;

                    while (!noMoreChildToProcess)
                    {
                        IElement needToMoveNode = null;
                        IElement needToMoveIntoNode = null;

                        foreach (var child in childList)
                        {
                            if (!string.IsNullOrEmpty(child.Id))
                            {
                                foreach (var potentialParent in childList.Where(o => o != child))
                                {
                                    if (!string.IsNullOrEmpty(potentialParent.Id))
                                    {
                                        if (ConvertAbsoluteToStatic_IsChildInsideParent(child.Id, potentialParent.Id, dictElemIdAndPosition))
                                        {
                                            var parentPosition = dictElemIdAndPosition[potentialParent.Id];

                                            if (potentialParent.TagName == "IMG")
                                            {

                                            }
                                            else if (parentPosition.Opacity.HasValue && parentPosition.Opacity < 1)
                                            {

                                            }
                                            else
                                            {
                                                needToMoveNode = child;
                                                needToMoveIntoNode = potentialParent;
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (needToMoveNode != null && needToMoveIntoNode != null)
                                {
                                    break;
                                }
                            }
                        }

                        if (needToMoveNode != null && needToMoveIntoNode != null)
                        {
                            ConvertAbsoluteToStatic_MoveChildIntoNewParent(needToMoveNode, needToMoveIntoNode, dictElemIdAndPosition);
                            childList.Remove(needToMoveNode);
                        }
                        else
                        {
                            noMoreChildToProcess = true;
                        }
                    }
                }

                foreach (var child in element.Children)
                {
                    ConvertAbsoluteToStatic_MoveNodesIntoExistingContainerNodesByPositionAndSize(child, dictElemIdAndPosition);
                }
            }
        }

        private static bool ConvertAbsoluteToStatic_IsChildInsideParent(string childId, string parentId, Dictionary<string, PositionData> dictElemIdAndPosition)
        {
            if (dictElemIdAndPosition != null && dictElemIdAndPosition.ContainsKey(childId) && dictElemIdAndPosition.ContainsKey(parentId))
            {
                var child = dictElemIdAndPosition[childId];
                var parent = dictElemIdAndPosition[parentId];

                if (child.Left.HasValue && child.Top.HasValue && child.Width.HasValue && child.Height.HasValue
                    && parent.Left.HasValue && parent.Top.HasValue && parent.Width.HasValue && parent.Height.HasValue)
                {

                    return child.Left >= parent.Left && child.Top >= parent.Top &&
                        child.Left + child.Width <= parent.Left + parent.Width &&
                        child.Top + child.Height <= parent.Top + parent.Height;
                }
            }
            return false;

        }

        private static void ConvertAbsoluteToStatic_MoveChildIntoNewParent(IElement childNode, IElement parentNode, Dictionary<string, PositionData> dictElemIdAndPosition)
        {
            parentNode.AppendChild(childNode);

            var childPosition = dictElemIdAndPosition[childNode.Id];
            var parentPosition = dictElemIdAndPosition[parentNode.Id];

            childPosition.Left = childPosition.Left - parentPosition.Left;
            childPosition.Top = childPosition.Top - parentPosition.Top;


        }



        private static void ConvertAbsoluteToStatic_AddHorizontalContainers(IHtmlDocument document, IElement element, Dictionary<string, PositionData> dictElemIdAndPosition)
        {
            var parentPosition = dictElemIdAndPosition[element.Id];

            if (element.Children.Count() > 0)
            {
                if (element.Children.Count() > 1)
                {
                    var childList = element.Children.OrderBy(o => dictElemIdAndPosition[o.Id].Top).ThenBy(o => dictElemIdAndPosition[o.Id].Left).ToList();

                    var originalOrder = element.Children
                            .Select((child, index) => new { child.Id, Index = index })
                            .ToDictionary(x => x.Id, x => x.Index);

                    Dictionary<string, List<IElement>> dictContainerIdAndChildElementList = new Dictionary<string, List<IElement>>();

                    bool noMoreChildNeedToMove = false;

                    while (childList.Count() > 0 && !noMoreChildNeedToMove)
                    {
                        PositionData containerPosition = null;
                        List<IElement> containerChildList = null;

                        foreach (var nexChildElem in childList)
                        {
                            var nextChildPosition = dictElemIdAndPosition[nexChildElem.Id];

                            if (!nextChildPosition.Top.HasValue || !nextChildPosition.Left.HasValue)
                            {
                                continue;
                            }

                            if (containerPosition == null)
                            {
                                containerPosition = nextChildPosition.DeepCopy();
                                containerPosition.Id = ExtensionMethodhelper.RandomId();
                                containerPosition.OriginalStyles = new Dictionary<string, string>();
                                containerPosition.Left = 0;
                                containerPosition.Width = parentPosition.Width;

                                dictElemIdAndPosition.Add(containerPosition.Id, containerPosition);
                            }



                            bool isNeedToPutChildIntoContainer = UpdateHorizontalContainerSizeByNewChild(containerPosition, nextChildPosition);

                            if (isNeedToPutChildIntoContainer)
                            {
                                if (!dictContainerIdAndChildElementList.ContainsKey(containerPosition.Id))
                                {
                                    containerChildList = new List<IElement>();
                                    dictContainerIdAndChildElementList.Add(containerPosition.Id, containerChildList);
                                }

                                containerChildList.Add(nexChildElem);


                            }
                            else
                            {
                                //noNeedToMoveChildList.Add(nexChildElem.Id);
                            }
                        }

                        if (containerChildList != null && containerChildList.Count > 0)
                        {
                            foreach (var childElement in containerChildList)
                            {
                                childList.Remove(childElement);
                            }
                        }
                        else
                        {
                            noMoreChildNeedToMove = true;
                        }
                    }

                    var containerIds = dictContainerIdAndChildElementList.Keys.ToList();

                    foreach (var key in containerIds)
                    {
                        var containerChildList = dictContainerIdAndChildElementList[key];
                        containerChildList = containerChildList
                                       .OrderBy(child => originalOrder[child.Id])
                                       .ToList();

                        dictContainerIdAndChildElementList[key] = containerChildList;
                    }


                    AddCalculatedContainers(document, element, dictElemIdAndPosition, dictContainerIdAndChildElementList, false);
                }

                foreach (var child in element.Children)
                {
                    ConvertAbsoluteToStatic_AddHorizontalContainers(document, child, dictElemIdAndPosition);
                }
            }
        }


        private static bool UpdateHorizontalContainerSizeByNewChild(PositionData container, PositionData child)
        {
            container.Height = container.Height.HasValue ? container.Height.Value : 0;
            child.Height = child.Height.HasValue ? child.Height.Value : 0;

            bool isNeedToPutChildIntoContainer = false;
            decimal containerY1 = container.Top.Value;
            decimal containerY2 = container.Top.Value + container.Height.Value;



            decimal childY1 = child.Top.Value;
            decimal childY2 = child.Top.Value + child.Height.Value;

            if ((containerY1 <= childY1 && containerY2 >= childY2)
                || (containerY1 >= childY1 && containerY2 <= childY2)
                || (containerY1 >= childY1 && containerY1 < childY2)
                || (containerY2 > childY1 && containerY2 <= childY2)

                )
            {

                containerY1 = Math.Min(containerY1, childY1);
                containerY2 = Math.Max(containerY2, childY2);

                container.Top = containerY1;
                container.Height = containerY2 - containerY1;


                isNeedToPutChildIntoContainer = true;
            }
            else
            {
                isNeedToPutChildIntoContainer = false;
            }

            return isNeedToPutChildIntoContainer;
        }


        private static void ConvertAbsoluteToStatic_AddVerticalContainers(IHtmlDocument document, IElement element, Dictionary<string, PositionData> dictElemIdAndPosition)
        {
            var positionData = dictElemIdAndPosition[element.Id];

            if (element.Children.Count() > 0)
            {
                if (element.Children.Count() > 1)
                {
                    var childList = element.Children.OrderBy(o => dictElemIdAndPosition[o.Id].Left).ThenBy(o => dictElemIdAndPosition[o.Id].Top).ToList();
                    var originalOrder = element.Children
                          .Select((child, index) => new { child.Id, Index = index })
                          .ToDictionary(x => x.Id, x => x.Index);

                    Dictionary<string, List<IElement>> dictContainerIdAndChildElementList = new Dictionary<string, List<IElement>>();

                    bool noMoreChildNeedToMove = false;

                    //decimal nextVerticalContainer_PositionLeft = 0;

                    while (childList.Count() > 0 && !noMoreChildNeedToMove)
                    {
                        PositionData containerPosition = null;
                        List<IElement> containerChildList = null;

                        foreach (var nexChildElem in childList)
                        {
                            var nextChildPosition = dictElemIdAndPosition[nexChildElem.Id];

                            if (!nextChildPosition.Top.HasValue || !nextChildPosition.Left.HasValue)
                            {
                                continue;
                            }

                            if (containerPosition == null)
                            {
                                containerPosition = nextChildPosition.DeepCopy();
                                containerPosition.Id = ExtensionMethodhelper.RandomId();
                                containerPosition.OriginalStyles = new Dictionary<string, string>();
                                containerPosition.Top = 0;
                                containerPosition.Height = positionData.Height;
                                //containerPosition.Left = nextVerticalContainer_PositionLeft;

                                dictElemIdAndPosition.Add(containerPosition.Id, containerPosition);
                            }

                            bool isNeedToPutChildIntoContainer = UpdateVerticalContainerSizeByNewChild(containerPosition, nextChildPosition);

                            if (isNeedToPutChildIntoContainer)
                            {
                                if (!dictContainerIdAndChildElementList.ContainsKey(containerPosition.Id))
                                {
                                    containerChildList = new List<IElement>();
                                    dictContainerIdAndChildElementList.Add(containerPosition.Id, containerChildList);
                                }

                                containerChildList.Add(nexChildElem);
                            }
                            else
                            {
                                //noNeedToMoveChildList.Add(nexChildElem.Id);
                            }
                        }

                        if (containerChildList != null && containerChildList.Count > 0)
                        {
                            foreach (var childElement in containerChildList)
                            {
                                childList.Remove(childElement);
                            }
                        }
                        else
                        {
                            noMoreChildNeedToMove = true;
                        }
                    }

                    var containerIds = dictContainerIdAndChildElementList.Keys.ToList();

                    foreach (var key in containerIds)
                    {
                        var containerChildList = dictContainerIdAndChildElementList[key];
                        containerChildList = containerChildList
                                       .OrderBy(child => originalOrder[child.Id])
                                       .ToList();

                        dictContainerIdAndChildElementList[key] = containerChildList;
                    }



                    AddCalculatedContainers(document, element, dictElemIdAndPosition, dictContainerIdAndChildElementList, true);
                }

                foreach (var child in element.Children)
                {
                    ConvertAbsoluteToStatic_AddVerticalContainers(document, child, dictElemIdAndPosition);
                }
            }
        }
        private static void ConvertAbsoluteToStatic_InitParentId(IElement element, Dictionary<string, PositionData> dictElemIdAndPosition)
        {
            if (element.Children.Count() > 0)
            {
                foreach (var child in element.Children)
                {
                    dictElemIdAndPosition[child.Id].ParentId = element.Id;

                    ConvertAbsoluteToStatic_InitParentId(child, dictElemIdAndPosition);
                }
            }
        }

        private static void ConvertAbsoluteToStatic_CalculatePositionAndDispay(IHtmlElement bodyElement, string elementId, Dictionary<string, PositionData> dictElemIdAndPosition)
        {
            var currentPosition = dictElemIdAndPosition[elementId];
            var currentElement = bodyElement.QuerySelector("#" + elementId);

            var childPositionList = dictElemIdAndPosition.Values.Where(o => o.ParentId == elementId).ToList();

            if (currentElement != null)
            {

                DetectPositionByChlidOverlap(currentPosition, childPositionList);

                if (childPositionList.Count > 0)
                {
                    if (currentPosition.Display == "flex" && childPositionList.Count >= 2)
                    {
                        ConvertAbsoluteToStatic_CalculatePositionAndDispay_ProcessStaticHorizontalContainer(currentPosition, currentElement, childPositionList);
                    }
                    else
                    {
                        ConvertAbsoluteToStatic_CalculatePositionAndDispay_ProcessStaticVerticalContainer(currentPosition, currentElement, childPositionList);
                    }

                    foreach (var child in childPositionList)
                    {
                        if (!child.IsForceAbsolute)
                        {
                            child.Position = "static";
                            child.Top = null;
                            child.Left = null;
                        }
                    }
                }

                foreach (var childPosition in childPositionList)
                {
                    ConvertAbsoluteToStatic_CalculatePositionAndDispay(bodyElement, childPosition.Id, dictElemIdAndPosition);
                }
            }
        }

        private static void ConvertAbsoluteToStatic_CalculatePositionAndDispay_ProcessStaticVerticalContainer(PositionData parentPosition, IElement parentElement, List<PositionData> childPositionList)
        {
            decimal initTop = 0;

            if (childPositionList.Where(o => !o.IsForceAbsolute && o.Left.HasValue).Count() > 0)
            {
                parentPosition.PaddingLeft = childPositionList.Where(o => !o.IsForceAbsolute && o.Left.HasValue).Min(o => o.Left.Value);
                parentPosition.PaddingLeft = parentPosition.PaddingLeft >= 0 ? parentPosition.PaddingLeft : 0;
            }

            PositionData prevChild = null;
            foreach (var child in childPositionList.OrderBy(o => o.Top))
            {
                child.Top = child.Top.HasValue ? child.Top.Value : 0;
                child.Left = child.Left.HasValue ? child.Left.Value : 0;
                child.Height = child.Height.HasValue ? child.Height.Value : 0;
                child.Width = child.Width.HasValue ? child.Width.Value : 0;

                if (!child.IsForceAbsolute)
                {
                    child.Position = "static";

                    if (prevChild == null)
                    {
                        decimal offsetY = child.Top.Value - initTop;

                        if (offsetY >= 0)
                        {
                            parentPosition.PaddingTop = offsetY;
                        }
                        else
                        {
                            child.MarginTop = offsetY;
                        }
                    }
                    else
                    {
                        prevChild.MarginBottom = child.Top.Value - initTop;
                    }

                    decimal rightValue = parentPosition.Width.Value - child.Width.Value - child.Left.Value;

                    if (rightValue > 0 && Math.Abs((child.Left.Value - rightValue) / rightValue) < (decimal)0.05)
                    {
                        child.IsMxAuto = true;
                        if (childPositionList.Count == 1)
                        {
                            parentPosition.PaddingLeft = 0;
                        }
                    }
                    else
                    {
                        child.MarginLeft = child.Left.Value - parentPosition.PaddingLeft;
                    }

                    initTop = child.Top.Value + child.Height.Value;

                    prevChild = child;

                    var childElement = parentElement.Children.FirstOrDefault(o => o.Id == child.Id);

                    if (childElement != null)
                    {
                        parentElement.AppendChild(childElement);
                    }
                }


            }

            foreach (var child in childPositionList.Where(o => o.IsForceAbsolute))
            {
                var childElement = parentElement.Children.FirstOrDefault(o => o.Id == child.Id);

                if (childElement != null)
                {
                    parentElement.AppendChild(childElement);
                }
            }
        }

        private static void ConvertAbsoluteToStatic_CalculatePositionAndDispay_ProcessStaticHorizontalContainer(PositionData parentPosition, IElement parentElement, List<PositionData> childPositionList)
        {
            decimal initLeft = 0;


            if (childPositionList.Where(o => !o.IsForceAbsolute && o.Top.HasValue).Count() > 0)
            {
                parentPosition.PaddingTop = childPositionList.Where(o => !o.IsForceAbsolute && o.Top.HasValue).Min(o => o.Top.Value);
                parentPosition.PaddingTop = parentPosition.PaddingTop >= 0 ? parentPosition.PaddingTop : 0;
            }

            PositionData prevChild = null;
            foreach (var child in childPositionList.OrderBy(o => o.Left))
            {
                child.Top = child.Top.HasValue ? child.Top.Value : 0;
                child.Left = child.Left.HasValue ? child.Left.Value : 0;
                child.Height = child.Height.HasValue ? child.Height.Value : 0;
                child.Width = child.Width.HasValue ? child.Width.Value : 0;

                if (!child.IsForceAbsolute)
                {
                    if (prevChild == null)
                    {
                        parentPosition.PaddingLeft = child.Left.Value - initLeft;
                    }
                    else
                    {
                        prevChild.MarginRight = child.Left.Value - initLeft;
                    }

                    child.MarginTop = child.Top.Value - parentPosition.PaddingTop;

                    initLeft = child.Left.Value + child.Width.Value;

                    prevChild = child;

                    var childElement = parentElement.Children.FirstOrDefault(o => o.Id == child.Id);

                    if (childElement != null)
                    {
                        parentElement.AppendChild(childElement);
                    }
                }
            }

            foreach (var child in childPositionList.Where(o => o.IsForceAbsolute))
            {
                var childElement = parentElement.Children.FirstOrDefault(o => o.Id == child.Id);

                if (childElement != null)
                {
                    parentElement.AppendChild(childElement);
                }
            }
        }

        private static void DetectPositionByChlidOverlap(PositionData currentPosition, List<PositionData> childPositionList)
        {
            if (currentPosition.IsForceAbsolute)
            {
                currentPosition.Position = "absolute";

                bool isContainerRelative, isFoundHorizontalOverlap_OnRemainChildren, isFoundVerticalOverlap_OnRemainChildren;
                DetectPositionByChlidOverlap_ProcessChildPosition(childPositionList, out isContainerRelative, out isFoundHorizontalOverlap_OnRemainChildren, out isFoundVerticalOverlap_OnRemainChildren);

                if (!isFoundHorizontalOverlap_OnRemainChildren && isFoundVerticalOverlap_OnRemainChildren)
                {
                    currentPosition.Display = "flex";
                }
            }
            else
            {
                if (childPositionList.Count >= 2)
                {
                    bool isContainerRelative, isFoundHorizontalOverlap_OnRemainChildren, isFoundVerticalOverlap_OnRemainChildren;
                    DetectPositionByChlidOverlap_ProcessChildPosition(childPositionList, out isContainerRelative, out isFoundHorizontalOverlap_OnRemainChildren, out isFoundVerticalOverlap_OnRemainChildren);


                    if (isContainerRelative)
                    {
                        currentPosition.Position = "relative";
                    }
                    else
                    {
                        currentPosition.Position = "static";
                    }

                    if (!isFoundHorizontalOverlap_OnRemainChildren && isFoundVerticalOverlap_OnRemainChildren)
                    {
                        currentPosition.Display = "flex";
                    }
                }
                else
                {
                    currentPosition.Position = "static";
                }
            }
        }

        private static void DetectPositionByChlidOverlap_ProcessChildPosition(List<PositionData> childPositionList, out bool isContainerRelative, out bool isFoundHorizontalOverlap_OnRemainChildren, out bool isFoundVerticalOverlap_OnRemainChildren)
        {
            var remianChildPositionList = new List<PositionData>();
            childPositionList.ForAll(o => remianChildPositionList.Add(o));

            isContainerRelative = false;
            isFoundHorizontalOverlap_OnRemainChildren = false;
            isFoundVerticalOverlap_OnRemainChildren = false;
            if (remianChildPositionList.Count > 1)
            {
                do
                {
                    isFoundHorizontalOverlap_OnRemainChildren = false;
                    isFoundVerticalOverlap_OnRemainChildren = false;

                    for (int i = 0; i < remianChildPositionList.Count; i++)
                    {
                        var current = remianChildPositionList[i];

                        if (!current.IsForceAbsolute)
                        {
                            for (int j = i + 1; j < remianChildPositionList.Count; j++)
                            {
                                var other = remianChildPositionList[j];

                                if (!other.IsForceAbsolute)
                                {

                                    bool horizontalOverlap = (current.Left < other.Left + other.Width) &&
                                                             (current.Left + current.Width > other.Left);

                                    bool verticalOverlap = (current.Top < other.Top + other.Height) &&
                                                           (current.Top + current.Height > other.Top);

                                    if (horizontalOverlap)
                                    {
                                        isFoundHorizontalOverlap_OnRemainChildren = true;
                                    }

                                    if (verticalOverlap)
                                    {
                                        isFoundVerticalOverlap_OnRemainChildren = true;
                                    }

                                    if (isFoundHorizontalOverlap_OnRemainChildren && verticalOverlap || isFoundVerticalOverlap_OnRemainChildren && horizontalOverlap)
                                    {
                                        if (other.Opacity.HasValue && other.Opacity.Value > 0)
                                        {
                                            other.IsForceAbsolute = true;
                                        }
                                        else if (current.Opacity.HasValue && current.Opacity.Value > 0)
                                        {
                                            current.IsForceAbsolute = true;
                                        }
                                        else
                                        {
                                            other.IsForceAbsolute = true;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (isFoundHorizontalOverlap_OnRemainChildren && isFoundVerticalOverlap_OnRemainChildren)
                    {
                        isContainerRelative = true;
                    }

                    remianChildPositionList = remianChildPositionList.Where(o => !o.IsForceAbsolute).ToList();
                }
                while (remianChildPositionList.Count > 1 && (isFoundHorizontalOverlap_OnRemainChildren && isFoundVerticalOverlap_OnRemainChildren));
            }
        }

        private static void AddCalculatedContainers(IHtmlDocument document, IElement element, Dictionary<string, PositionData> dictElemIdAndPosition, Dictionary<string, List<IElement>> dictContainerIdAndChildElementList, bool isVerticalContainer)
        {
            int containerCount = dictContainerIdAndChildElementList.Keys.Count;

            if (containerCount > 1)
            {
                var orgFirstChild = element.Children.First();

                //List<string> containerIdList = dictContainerIdAndChildElementList.Where(o => o.Value.Count > 1).Select(o => o.Key).ToList();

                List<string> containerIdList = dictContainerIdAndChildElementList.Keys.ToList();

                Dictionary<string, IElement> dictContainerIdAndContainerElement = new Dictionary<string, IElement>();

                foreach (var containerId in containerIdList)
                {
                    bool isNeedToAddContainer = false;
                    var containerPosition = dictElemIdAndPosition[containerId];
                    var childElemList = dictContainerIdAndChildElementList[containerId];

                    if (childElemList.Count > 1)
                    {
                        isNeedToAddContainer = true;
                    }
                    else if (childElemList.Count == 1)
                    {
                        var childPosition = dictElemIdAndPosition[childElemList[0].Id];

                        if (isVerticalContainer)
                        {
                            isNeedToAddContainer = containerPosition.Height > childPosition.Height;
                        }
                        else
                        {
                            isNeedToAddContainer = containerPosition.Width > childPosition.Width;
                        }
                    }

                    if (isNeedToAddContainer)
                    {
                        var containerElem = document.CreateElement("div");
                        containerElem.Id = containerId;

                        SetElementStyleFromPositionData(containerElem, containerPosition, null);

                        //containerPosition.is

                        element.InsertBefore(containerElem, orgFirstChild);
                        dictContainerIdAndContainerElement.Add(containerId, containerElem);
                    }
                }

                foreach (var containerId in dictContainerIdAndContainerElement.Keys)
                {
                    if (dictContainerIdAndContainerElement.ContainsKey(containerId))
                    {
                        var containerElem = dictContainerIdAndContainerElement[containerId];
                        var childElemList = dictContainerIdAndChildElementList[containerId];
                        childElemList.ForEach(o =>
                        {
                            ConvertAbsoluteToStatic_MoveChildIntoNewParent(o, containerElem, dictElemIdAndPosition);

                        });
                    }
                }
            }
        }

        private static bool UpdateVerticalContainerSizeByNewChild(PositionData container, PositionData child)
        {
            bool isNeedToPutChildIntoContainer = false;
            //decimal containerY1 = container.Top.Value;
            //decimal containerY2 = container.Top.Value + container.Height.Value;
            decimal containerX1 = container.Left.Value;
            container.Width = container.Width.HasValue ? container.Width.Value : 0;
            decimal containerX2 = container.Left.Value + container.Width.Value;

            child.Width = child.Width.HasValue ? child.Width.Value : 0;

            decimal childX1 = child.Left.Value;
            decimal childX2 = child.Left.Value + child.Width.Value;

            if ((containerX1 <= childX1 && containerX2 >= childX2)
                || (containerX1 >= childX1 && containerX2 <= childX2)
                || (containerX1 >= childX1 && containerX1 < childX2)
                || (containerX2 > childX1 && containerX2 <= childX2)

                )
            {
                //container.Left = Math.Min(container.Left.Value, child.Left.Value);

                //if (containerX1 > container.Left.Value)
                //{
                //    container.Width = container.Width.Value + (containerX1 - container.Left.Value);
                //}

                containerX1 = Math.Min(containerX1, childX1);
                containerX2 = Math.Max(containerX2, childX2);
                container.Left = containerX1;
                container.Width = containerX2 - containerX1;

                isNeedToPutChildIntoContainer = true;
            }
            else
            {
                isNeedToPutChildIntoContainer = false;
            }

            return isNeedToPutChildIntoContainer;
        }

        private static void ConvertAbsoluteToStatic_UpdateFinalStylesFromPositionData(IHtmlElement bodyElement, Dictionary<string, PositionData> dictElemIdAndPosition)
        {
            foreach (var element in bodyElement.QuerySelectorAll("*"))
            {
                if (dictElemIdAndPosition.ContainsKey(element.Id))
                {
                    PositionData positionData = dictElemIdAndPosition[element.Id];

                    PositionData parentPosition = null;

                    if (!string.IsNullOrWhiteSpace(positionData.ParentId) && dictElemIdAndPosition.ContainsKey(positionData.ParentId))
                    {
                        parentPosition = dictElemIdAndPosition[positionData.ParentId];
                    }

                    SetElementStyleFromPositionData(element, positionData, parentPosition);

                }
            }
        }

        private static void SetElementStyleFromPositionData(IElement element, PositionData positionData, PositionData parentPosition)
        {

            Dictionary<string, string> originalStyles = positionData.OriginalStyles;

            if (!string.IsNullOrWhiteSpace(positionData.Position) && positionData.Position != "static")
            {
                originalStyles["position"] = positionData.Position;
            }
            else
            {
                originalStyles.Remove("position");
            }

            if (!string.IsNullOrWhiteSpace(positionData.Display))
            {
                originalStyles["display"] = positionData.Display;
            }
            else
            {
                originalStyles.Remove("display");
            }

            if (positionData.Left.HasValue && !string.IsNullOrWhiteSpace(positionData.Position) && positionData.Position != "static")
            {
                originalStyles["left"] = positionData.Left.Value + "px";
            }
            else
            {
                originalStyles.Remove("left");
            }

            if (positionData.Top.HasValue && !string.IsNullOrWhiteSpace(positionData.Position) && positionData.Position != "static")
            {
                originalStyles["top"] = positionData.Top.Value + "px";
            }
            else
            {
                originalStyles.Remove("top");
            }

            if (positionData.Width.HasValue)
            {
                if (parentPosition != null && parentPosition.Width.HasValue && parentPosition.Width.Value == positionData.Width.Value)
                {
                    originalStyles["width"] = "100%";
                }
                else
                {
                    originalStyles["width"] = positionData.Width.Value + "px";
                    originalStyles["max-width"] = "100%";

                    //if (parentPosition != null && parentPosition.Display == "flex" && !positionData.IsForceAbsolute)
                    //{
                    //    originalStyles["flex"] = "11auto";
                    //}
                }
            }
            else
            {
                originalStyles.Remove("width");
            }

            if (positionData.Height.HasValue)
            {
                if (parentPosition != null && parentPosition.Height.HasValue && parentPosition.Height.Value == positionData.Height.Value)
                {
                    originalStyles["height"] = "100%";
                }
                else
                {
                    originalStyles["height"] = positionData.Height.Value + "px";
                }
            }
            else
            {
                originalStyles.Remove("height");
            }



            if (positionData.PaddingTop != 0)
            {
                originalStyles["padding-top"] = positionData.PaddingTop + "px";
            }
            else
            {
                originalStyles.Remove("padding-top");
            }

            if (positionData.PaddingRight != 0)
            {
                originalStyles["padding-right"] = positionData.PaddingRight + "px";
            }
            else
            {
                originalStyles.Remove("padding-right");
            }

            if (positionData.PaddingBottom != 0)
            {
                originalStyles["padding-bottom"] = positionData.PaddingBottom + "px";
            }
            else
            {
                originalStyles.Remove("padding-bottom");
            }

            if (positionData.PaddingLeft != 0)
            {
                originalStyles["padding-left"] = positionData.PaddingLeft + "px";
            }
            else
            {
                originalStyles.Remove("padding-left");
            }


            if (positionData.MarginTop != 0)
            {
                originalStyles["margin-top"] = positionData.MarginTop + "px";
            }
            else
            {
                originalStyles.Remove("margin-top");
            }

            if (positionData.IsMxAuto)
            {
                originalStyles["margin-left"] = "auto";
                originalStyles["margin-right"] = "auto";
            }
            else
            {
                if (positionData.MarginRight != 0)
                {
                    originalStyles["margin-right"] = positionData.MarginRight + "px";
                }
                else
                {
                    originalStyles.Remove("margin-right");
                }

                if (positionData.MarginLeft != 0)
                {
                    originalStyles["margin-left"] = positionData.MarginLeft + "px";
                }
                else
                {
                    originalStyles.Remove("margin-left");
                }
            }


            if (positionData.MarginBottom != 0)
            {
                originalStyles["margin-bottom"] = positionData.MarginBottom + "px";
            }
            else
            {
                originalStyles.Remove("margin-bottom");
            }





            if (originalStyles.Any())
            {
                var newStyle = string.Join("; ", originalStyles.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                element.SetAttribute("style", newStyle);
            }
            else
            {
                element.RemoveAttribute("style");
            }
        }



        //private static void ConvertHtmlAbsolutePositionToStatic_ProcessOneNode(IElement element)
        //{
        //    foreach (var child in element.Children)
        //    {
        //        if (child.HasAttribute("style"))
        //        {
        //            var style = child.GetAttribute("style");

        //            if (!string.IsNullOrEmpty(style))
        //            {
        //                var originalStyles = new Dictionary<string, string>();

        //                var styleProperties = style.Split(';')
        //                    .Select(s => s.Trim())
        //                    .Where(s => !string.IsNullOrEmpty(s))
        //                    .ToArray();

        //                foreach (var propertyValue in styleProperties)
        //                {
        //                    var parts = propertyValue.Split(':');
        //                    if (parts.Length == 2)
        //                    {
        //                        string property = parts[0].Trim();
        //                        string value = parts[1].Trim();
        //                        originalStyles[property] = value;
        //                    }
        //                }

        //                if (originalStyles.Any())
        //                {
        //                    var newStyle = string.Join("; ", originalStyles.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
        //                    element.SetAttribute("style", newStyle);
        //                }
        //                else
        //                {
        //                    element.RemoveAttribute("style");
        //                }
        //            }
        //        }

        //        ConvertHtmlAbsolutePositionToStatic_ProcessOneNode(child);
        //    }
        //}

    }

    public class PositionData
    {
        public string Id { get; set; }

        public string ParentId { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public bool IsForceAbsolute { get; set; }
        public string Position { get; set; }
        public string Display { get; set; }
        public decimal? Left { get; set; }
        public decimal? Top { get; set; }

        public decimal MarginTop { get; set; }
        public decimal MarginRight { get; set; }
        public decimal MarginBottom { get; set; }
        public decimal MarginLeft { get; set; }

        public bool IsMxAuto { get; set; }

        public decimal PaddingTop { get; set; }
        public decimal PaddingRight { get; set; }
        public decimal PaddingBottom { get; set; }
        public decimal PaddingLeft { get; set; }

        public decimal? Opacity { get; set; }
        public Dictionary<string, string> OriginalStyles { get; set; } = new Dictionary<string, string>();

        public bool IsHorizontalContainer { get; set; }
        //public bool IsRelativeContainer { get; set; }

    }
}
