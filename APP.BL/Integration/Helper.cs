using App.BL;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
//using CustomProcessBL;
//using CustomProcessBL.Models;
using ExchangeBL.Models;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using APP.Framework;
namespace ExchangeBL
{
    public class Helper
    {
        private static readonly WindowClientIdentityProvider windowClientIdentityProvider = new WindowClientIdentityProvider();
        private static readonly int ApiKeyUserId = 3;

        private static readonly string EnvironmentVariableKeyPrefix = "Env.";
        private static readonly string PathParameterKeyPrefix = "PathParams.";

        public static async Task<List<string>> CallAPIAsync(APIConfigParameterDTO apiParameterDTO, object payloadData, int? apiProviderId = null,
            List<KeyValuePair<string, string>> responseHeaderKeyAndValueList = null, EmAppApiPayloadDataType? emAppApiPayloadDataType = null, bool isMultipartFormDataContent = false)
        {
            HttpResponseMessage httpResponse = null;
            List<string> lstResponse = new List<string>();
            string url = apiParameterDTO.Url;

            Dictionary<string, string> dictEnvironmentVariable = null;
            Dictionary<string, string> dictCookieSetting = null;

            if (apiProviderId.HasValue)
            {
                // Get
                var intergrationSettingEntity = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingEntity(apiProviderId);
                AppIntergrationSettingExDto aIntergrationSettingDto = AppIntergrationSettingConverter.ConvertEntityToExDto(intergrationSettingEntity);

                dictEnvironmentVariable = aIntergrationSettingDto.OtherSettingsDto.DictEnvironmentVariable
                    .DeepCopy();

                if (apiParameterDTO.DictOverrideEnvionmentVariable != null)
                {
                    foreach (var kv in apiParameterDTO.DictOverrideEnvionmentVariable)
                    {
                        if (dictEnvironmentVariable.ContainsKey(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
                        {
                            dictEnvironmentVariable[kv.Key] = kv.Value;
                        }
                    }
                }

                dictCookieSetting = aIntergrationSettingDto.OtherSettingsDto.DictCookieSetting
                    .DeepCopy();
            }

            //TODO !!!!
            //Need to overide dictEnvironmentVariable from  apiParameterDTO

            var handler = new HttpClientHandler();

            if (dictCookieSetting != null && dictCookieSetting.Keys.Count > 0)
            {
                var cookieContainer = new CookieContainer();

                foreach (var kv in dictCookieSetting)
                {
                    if (!string.IsNullOrWhiteSpace(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
                    {
                        cookieContainer.Add(new Uri(apiParameterDTO.BaseUrl), new Cookie(kv.Key, kv.Value));
                    }
                }

                handler.CookieContainer = cookieContainer;
            }

            using (HttpClient httpClient = new HttpClient(handler))
            {
                apiParameterDTO.BaseUrl = ReplaceAllEnvionmentVariableKeyWithValues(dictEnvironmentVariable, apiParameterDTO.BaseUrl);
                apiParameterDTO.RequestInfo = "BaseUrl: \"" + apiParameterDTO.BaseUrl + "\"";
                httpClient.BaseAddress = new Uri(apiParameterDTO.BaseUrl);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.Timeout = new TimeSpan(0, 15, 0);

                foreach (KeyValuePair<string, string> oneKV in apiParameterDTO.Headers)
                {
                    string key = oneKV.Key;
                    string value = oneKV.Value;

                    value = ReplaceAllEnvionmentVariableKeyWithValues(dictEnvironmentVariable, value);

                    httpClient.DefaultRequestHeaders.Add(key, value);
                }

                foreach (KeyValuePair<string, string> oneKV in apiParameterDTO.PathParams)
                {
                    string key = "{{" + PathParameterKeyPrefix + oneKV.Key + "}}";
                    string value = oneKV.Value;

                    value = ReplaceAllEnvionmentVariableKeyWithValues(dictEnvironmentVariable, value);

                    url = url.Replace(key, value);
                }

                url = ReplaceAllEnvionmentVariableKeyWithValues(dictEnvironmentVariable, url);

                if (apiParameterDTO.QueryParams.Count > 0)
                {
                    if (url.IndexOf("?") >= 0)
                    {
                        url = url + "&" + string.Join("&", apiParameterDTO.QueryParams.Select(kv => $"{kv.Key}={ReplaceAllEnvionmentVariableKeyWithValues(dictEnvironmentVariable, kv.Value)}").ToList());
                    }
                    else
                    {
                        url = url + "?" + string.Join("&", apiParameterDTO.QueryParams.Select(kv => $"{kv.Key}={ReplaceAllEnvionmentVariableKeyWithValues(dictEnvironmentVariable, kv.Value)}").ToList());
                    }
                }

                url = Regex.Replace(url, @"\/{2,}", "/");

                apiParameterDTO.RequestInfo += "\nUrl: \"" + url + "\"";

                await GetTokenAsync(httpClient, apiParameterDTO).ConfigureAwait(false);

                switch (apiParameterDTO.Method)
                {
                    case EnumHttpMethod.Post:
                    case EnumHttpMethod.Put:
                        httpResponse = await CallAPIAsync_ProcessPostOrPutMethod(apiParameterDTO, payloadData, emAppApiPayloadDataType, isMultipartFormDataContent, httpResponse, url, httpClient).ConfigureAwait(false);
                        break;

                    case EnumHttpMethod.Delete:
                        httpResponse = await httpClient.DeleteAsync(url).ConfigureAwait(false);
                        break;

                    default:
                        httpResponse = await httpClient.GetAsync(url).ConfigureAwait(false);
                        break;
                }

                string strResponse = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (httpResponse.RequestMessage != null)
                {
                    apiParameterDTO.RequestInfo = Newtonsoft.Json.JsonConvert.SerializeObject(httpResponse.RequestMessage);
                }

                

                if (responseHeaderKeyAndValueList != null)
                {
                    foreach (var header in httpResponse.Headers)
                    {
                        responseHeaderKeyAndValueList.Add(new KeyValuePair<string, string>(header.Key, string.Join(",", header.Value)));
                    }
                }

                if (httpResponse.IsSuccessStatusCode)
                {
                    lstResponse.Add(strResponse);
                }
                else
                {
                    throw new Exception(strResponse);
                }

              

                UpdateProviderEnvionmentVariables(apiParameterDTO, apiProviderId, strResponse);

                UpdateProviderCookieValues(apiParameterDTO, apiProviderId, responseHeaderKeyAndValueList);
            }

            return lstResponse;
        }

        private static async Task<HttpResponseMessage> CallAPIAsync_ProcessPostOrPutMethod(APIConfigParameterDTO apiParameterDTO, object payloadData, EmAppApiPayloadDataType? emAppApiPayloadDataType, bool isMultipartFormDataContent, HttpResponseMessage httpResponse, string url, HttpClient httpClient)
        {
            if (payloadData != null)
            {
                if (emAppApiPayloadDataType.HasValue && emAppApiPayloadDataType.Value != EmAppApiPayloadDataType.JSON)
                {
                    if (emAppApiPayloadDataType.Value == EmAppApiPayloadDataType.FileByteArray)
                    {
                        httpResponse = await ProcessFileByteArrayUpload(apiParameterDTO, payloadData, isMultipartFormDataContent, httpResponse, url, httpClient).ConfigureAwait(false);
                    }
                    else if (emAppApiPayloadDataType.Value == EmAppApiPayloadDataType.ServerFilePath)
                    {
                        httpResponse = await ProcessFileServerPathUpload(apiParameterDTO, payloadData, isMultipartFormDataContent, httpResponse, url, httpClient).ConfigureAwait(false);
                    }
                }
                else
                {
                    string payloadDataStr = payloadData.ToString();

                    if (apiParameterDTO.Method == EnumHttpMethod.Post)
                    {
                        httpResponse = await httpClient.PostAsync(url, new StringContent(payloadDataStr, UnicodeEncoding.UTF8, "application/json")).ConfigureAwait(false);
                    }
                    else if (apiParameterDTO.Method == EnumHttpMethod.Put)
                    {
                        httpResponse = await httpClient.PutAsync(url, new StringContent(payloadDataStr, UnicodeEncoding.UTF8, "application/json")).ConfigureAwait(false);
                    }
                }
            }

            return httpResponse;
        }

        private static async Task<HttpResponseMessage> ProcessFileServerPathUpload(APIConfigParameterDTO apiParameterDTO, object payloadData, bool isMultipartFormDataContent, HttpResponseMessage httpResponse, string url, HttpClient httpClient)
        {
            if (payloadData is AppFilePathDto)
            {
                AppFilePathDto filePathDto = payloadData as AppFilePathDto;

                if (filePathDto != null)
                {
                    //string filePath = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(payloadData);

                    string filePath = filePathDto.FilePath;

                    if (!string.IsNullOrWhiteSpace(filePath))
                    {
                        byte[] buffer = null;
                        string fileName = "";

                        if (filePath.ToLower().StartsWith("http"))
                        {
                            string errorMsg = "";
                            buffer = AppEsiteFileBL.DownloadFileByUrl(filePath, out errorMsg);
                            fileName = Path.GetFileName(filePath);

                            if (fileName.Length > 50)
                            {
                                fileName = fileName.Substring(0, 40) + "_" + Guid.NewGuid().ToString();
                            }

                            if (!string.IsNullOrWhiteSpace(errorMsg))
                            {
                                errorMsg = "Invalid file url: " + filePath;

                                throw new Exception(errorMsg);
                            }
                        }
                        else if (filePath.ToLower().StartsWith("ftp"))
                        {
                            FtpTools ftpInstance = new FtpTools("", filePathDto.FtpUserName, filePathDto.FtpPassword);
                            string errorMsg = "";
                            buffer = ftpInstance.GetFtpFileBinaryData(filePathDto.FilePath, out errorMsg);
                            fileName = Path.GetFileName(filePath);

                            if (!string.IsNullOrWhiteSpace(errorMsg))
                            {
                                errorMsg = "Cannot Read File Content: " + filePath + "\n" + errorMsg;
                                throw new Exception(errorMsg);
                            }
                        }
                        else
                        {
                            buffer = StreamHelper.FileToByteArray(filePath);
                            fileName = Path.GetFileName(filePath);
                        }

                        if (buffer != null)
                        {
                            var byteContent = new ByteArrayContent(buffer);

                            if (isMultipartFormDataContent)
                            {
                                using (var content = new MultipartFormDataContent())
                                {
                                    content.Add(byteContent, "file", fileName);

                                    if (apiParameterDTO.Method == EnumHttpMethod.Post)
                                    {
                                        httpResponse = await httpClient.PostAsync(url, content).ConfigureAwait(false);
                                    }
                                    else if (apiParameterDTO.Method == EnumHttpMethod.Put)
                                    {
                                        httpResponse = await httpClient.PutAsync(url, content).ConfigureAwait(false);
                                    }
                                }
                            }
                            else
                            {
                                //byteContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                                string extension = System.IO.Path.GetExtension(fileName);
                                string contentType = DocumentHelper.GetSpecifiedMimeContentType(extension);
                                byteContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

                                if (apiParameterDTO.Method == EnumHttpMethod.Post)
                                {
                                    httpResponse = await httpClient.PostAsync(url, byteContent).ConfigureAwait(false);
                                }
                                else if (apiParameterDTO.Method == EnumHttpMethod.Put)
                                {
                                    httpResponse = await httpClient.PutAsync(url, byteContent).ConfigureAwait(false);
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("Cannot Read File Content: " + filePath);
                        }
                    }
                }
            }

            return httpResponse;
        }

        private static async Task<HttpResponseMessage> ProcessFileByteArrayUpload(APIConfigParameterDTO apiParameterDTO, object payloadData, bool isMultipartFormDataContent, HttpResponseMessage httpResponse, string url, HttpClient httpClient)
        {
            AppFileDto fileDto = payloadData as AppFileDto;

            if (fileDto != null && fileDto.FileContent != null)
            {
                var byteContent = new ByteArrayContent(fileDto.FileContent);

                if (isMultipartFormDataContent)
                {
                    using (var content = new MultipartFormDataContent())
                    {
                        content.Add(byteContent, "file", fileDto.FileCode);

                        httpResponse = await httpClient.PostAsync(url, content).ConfigureAwait(false);

                        if (apiParameterDTO.Method == EnumHttpMethod.Post)
                        {
                            httpResponse = await httpClient.PostAsync(url, content).ConfigureAwait(false);
                        }
                        else if (apiParameterDTO.Method == EnumHttpMethod.Put)
                        {
                            httpResponse = await httpClient.PutAsync(url, content).ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    //byteContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                    string extension = System.IO.Path.GetExtension(fileDto.FileCode);
                    string contentType = DocumentHelper.GetSpecifiedMimeContentType(extension);
                    byteContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

                    if (apiParameterDTO.Method == EnumHttpMethod.Post)
                    {
                        httpResponse = await httpClient.PostAsync(url, byteContent).ConfigureAwait(false);
                    }
                    else if (apiParameterDTO.Method == EnumHttpMethod.Put)
                    {
                        httpResponse = await httpClient.PutAsync(url, byteContent).ConfigureAwait(false);
                    }
                }
            }

            return httpResponse;
        }

        private static async Task GetTokenAsync(HttpClient httpClient, APIConfigParameterDTO configParameterDTO)
        {
            if (configParameterDTO.AuthenticationParameters != null)
            {
                switch (configParameterDTO.AuthenticationType)
                {
                    case AuthenticationType.None:
                        break;

                    case AuthenticationType.OAuth2:
                        await GetOAuth2TokenAsync(httpClient, configParameterDTO.AuthenticationParameters);
                        break;

                    case AuthenticationType.Basic64:
                        GetBasic64Token(httpClient, configParameterDTO.AuthenticationParameters);
                        break;

                    default:
                        foreach (KeyValuePair<string, string> oneKV in configParameterDTO.AuthenticationParameters)
                        {
                            httpClient.DefaultRequestHeaders.Add(oneKV.Key, oneKV.Value);
                        }
                        break;
                }
            }
        }

        private static async Task GetOAuth2TokenAsync(HttpClient httpClient, Dictionary<string, string> dictParameter)
        {
            Dictionary<string, string> reqToken = new Dictionary<string, string>(dictParameter);

            reqToken.Add("grant_type", "password");

            HttpResponseMessage httpResponse = await httpClient.PostAsync("Token", new FormUrlEncodedContent(reqToken)).ConfigureAwait(false);

            string strResponse = await httpResponse.Content.ReadAsStringAsync();

            if (httpResponse.IsSuccessStatusCode)
            {
                OAuth2TokenDTO oAuth2TokenDTO = Newtonsoft.Json.JsonConvert.DeserializeObject<OAuth2TokenDTO>(strResponse);

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(oAuth2TokenDTO.token_type, oAuth2TokenDTO.access_token);
            }
            else
            {
                throw new Exception(strResponse);
            }
        }

        private static void UserRestSharptoUpload(APIConfigParameterDTO apiParameterDTO, object payloadData, string url)
        {
            var restClient = new RestClient(apiParameterDTO.BaseUrl);

            var request = new RestRequest(url, Method.Put);

            // this cause alwasy download iamge, cannot display in browser
            // only for download !!! application/octet-stream
            request.AddHeader("Content-Type", "application/octet-stream");

            AppFileDto fileDto = payloadData as AppFileDto;
            byte[] imageBytes = fileDto.FileContent;
            request.AddParameter("application/octet-stream", imageBytes, ParameterType.RequestBody);
            var response = restClient.ExecuteAsync(request).GetAwaiter().GetResult();
        }

        private static void GetBasic64Token(HttpClient httpClient, Dictionary<string, string> dictParameter)
        {
            string userName = string.Empty;
            string password = string.Empty;
            foreach (string key in dictParameter.Keys)
            {
                switch (key.ToLower())
                {
                    case "username":
                        userName = dictParameter[key];
                        break;

                    case "password":
                        password = dictParameter[key];
                        break;

                    default:
                        throw new Exception("not know parameter" + key);
                }
            }
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.Default.GetBytes($"{userName}:{password}")));
        }

        public static string ReplaceAllEnvionmentVariableKeyWithValues(Dictionary<string, string> dictEnvironmentVariable, string value)
        {
            if (dictEnvironmentVariable != null && dictEnvironmentVariable.Keys.Count > 0)
            {
                dictEnvironmentVariable.ForAll(environmentVariable =>
                {
                    value = value.Replace("{{" + EnvironmentVariableKeyPrefix + environmentVariable.Key + "}}", environmentVariable.Value);
                });
            }

            return value;
        }

        private static void UpdateProviderEnvionmentVariables(APIConfigParameterDTO apiParameterDTO, int? apiProviderId, string strResponse)
        {
            if (!string.IsNullOrWhiteSpace(strResponse))
            {
                if (apiProviderId.HasValue && apiParameterDTO.ResponseObjectMapToEnvionmentVariable != null && apiParameterDTO.ResponseObjectMapToEnvionmentVariable.Count > 0)
                {
                    var intergrationSettingEntity = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingEntity(apiProviderId);
                    AppIntergrationSettingExDto aIntergrationSettingDto = AppIntergrationSettingConverter.ConvertEntityToExDto(intergrationSettingEntity);

                    var dictVariableKeyAndValue = aIntergrationSettingDto.OtherSettingsDto.DictEnvironmentVariable;

                    bool needToUpdateVariable = false;

                    if (dictVariableKeyAndValue != null && dictVariableKeyAndValue.Count > 0)
                    {
                        try
                        {
                            var jObj = JObject.Parse(strResponse);

                            foreach (KeyValuePair<string, string> oneKV in apiParameterDTO.ResponseObjectMapToEnvionmentVariable)
                            {
                                if (!string.IsNullOrWhiteSpace(oneKV.Key) && !string.IsNullOrWhiteSpace(oneKV.Value))
                                {
                                    string aEnvironmentVariableName = oneKV.Key;
                                    string aResponseObjectPath = oneKV.Value;

                                    if (dictVariableKeyAndValue.ContainsKey(aEnvironmentVariableName))
                                    {
                                        var nodeValue = jObj.SelectToken(aResponseObjectPath);

                                        var nodeStringValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(nodeValue);

                                        if (!string.IsNullOrWhiteSpace(nodeStringValue))
                                        {
                                            dictVariableKeyAndValue[aEnvironmentVariableName] = nodeStringValue;
                                            needToUpdateVariable = true;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                    if (needToUpdateVariable)
                    {
                        ServerContext.Instance.InitializeWindowsClient(windowClientIdentityProvider);

                        ServerContext.Instance.WindowsIdentityProvider.RegisterIdentity(new AppClientIdentity() { UserId = ApiKeyUserId, SessionId = System.Guid.NewGuid().ToString(), });

                        AppIntergrationSettingBL.SaveAppIntergrationSettingExDto(aIntergrationSettingDto);
                    }
                }
            }
        }

        private static void UpdateProviderCookieValues(APIConfigParameterDTO apiParameterDTO, int? apiProviderId, List<KeyValuePair<string, string>> responseHeaderKeyAndValueList)
        {
            if (responseHeaderKeyAndValueList != null)
            {
                if (apiProviderId.HasValue && apiParameterDTO.ResponseHeaderNeedToSetCookieNames != null && apiParameterDTO.ResponseHeaderNeedToSetCookieNames.Count > 0)
                {
                    Dictionary<string, string> dictNeedToUpdateCookieKeyAndValue = new Dictionary<string, string>();

                    foreach (KeyValuePair<string, string> responseKV in responseHeaderKeyAndValueList)
                    {
                        if (!string.IsNullOrWhiteSpace(responseKV.Key) && !string.IsNullOrWhiteSpace(responseKV.Value)
                            && responseKV.Key.ToLower() == "Set-Cookie".ToLower())
                        {
                            List<string> cookieSettingStringParts = responseKV.Value.Split(';').ToList();

                            if (cookieSettingStringParts.Count > 0)
                            {
                                foreach (string cookieStringPart in cookieSettingStringParts)
                                {
                                    int indexOfEqual = cookieStringPart.IndexOf("=");

                                    if (indexOfEqual > 0 && indexOfEqual < cookieStringPart.Length - 1)
                                    {
                                        string cookieName = cookieStringPart.Substring(0, indexOfEqual).Trim();
                                        string cookieValue = cookieStringPart.Substring(indexOfEqual + 1).Trim();

                                        if (!string.IsNullOrWhiteSpace(cookieName) && apiParameterDTO.ResponseHeaderNeedToSetCookieNames.Contains(cookieName))
                                        {
                                            dictNeedToUpdateCookieKeyAndValue[cookieName] = cookieValue;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (dictNeedToUpdateCookieKeyAndValue.Count > 0)
                    {
                        var intergrationSettingEntity = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingEntity(apiProviderId);
                        AppIntergrationSettingExDto aIntergrationSettingDto = AppIntergrationSettingConverter.ConvertEntityToExDto(intergrationSettingEntity);

                        if (aIntergrationSettingDto.OtherSettingsDto.DictCookieSetting == null)
                        {
                            aIntergrationSettingDto.OtherSettingsDto.DictCookieSetting = new Dictionary<string, string>();
                        }

                        var dictOrgCookieKeyAndValue = aIntergrationSettingDto.OtherSettingsDto.DictCookieSetting;

                        foreach (var kv in dictNeedToUpdateCookieKeyAndValue)
                        {
                            dictOrgCookieKeyAndValue[kv.Key] = kv.Value;
                        }

                        ServerContext.Instance.InitializeWindowsClient(windowClientIdentityProvider);

                        ServerContext.Instance.WindowsIdentityProvider.RegisterIdentity(new AppClientIdentity() { UserId = ApiKeyUserId, SessionId = System.Guid.NewGuid().ToString(), });

                        AppIntergrationSettingBL.SaveAppIntergrationSettingExDto(aIntergrationSettingDto);
                    }
                }
            }
        }
    }
}