using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

using System.Text.RegularExpressions;
using System.Net.Http;
using System.Threading.Tasks;

using RestSharp;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using APP.Components.EntityDto;
using APP.Framework;
using APP.Components.Dto;
using APP.LBL.EntityClasses;
using APP.Framework.Communication;
using APP.Framework.Validation;

namespace App.BL
{

    public class AppPluginClient
    {




        //AppWebClient.PorcessSearchResult(searchViewExternalUriDto, searchViewExternalUriDto.RestResourceUri.ToString());

        //It turns out that the problem was because of the RESTSharp library's serialiser. When I switched to NewtonSoft it worked fine. Basically, don't use RESTSharp.
        public static FileSimpleDto ProcessSearchResult(dynamic twoSetSearchResult, string restResourceUri)
        {
            //var restuls = new { FristSearchResult = new List<SearchReferenceResultRowJsonDto>(), SecondSearchResult = new List<SearchReferenceResultRowJsonDto>() };



            //string toReturn = "";

            FileSimpleDto aFileDto = null;

            HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);

            if (!string.IsNullOrEmpty(restResourceUri))
            {
                aFileDto = RestSharCall(twoSetSearchResult, restResourceUri);

            }





            return aFileDto;
        }




        private static FileSimpleDto RestSharCall(dynamic twoSetSearchResult, string restResourceUri)
        {
            //FileDto aFileDto = new WsCall.FileDto();
            string restEndPoint = AppSystemSettingBL.GetStringValue(EmSystemSettings.InternalApiRestEndPoint);

            string json = JsonConvert.SerializeObject(twoSetSearchResult);

            var client = new RestClient(restEndPoint);

            var request = new RestSharp.RestRequest(restResourceUri, RestSharp.Method.Post);
            request.AddParameter("application/json; charset=utf-8", json, ParameterType.RequestBody);
            request.RequestFormat = DataFormat.Json;


            var response = client.Execute<FileSimpleDto>(request);

            //response.ad

            FileSimpleDto aFileDto = response.Data;//JsonConvert.DeserializeObject<FileDto>(response.Content);



            string fileGuidId = System.Guid.NewGuid().ToString();
            FileSimpleDto outPutFileDto = new FileSimpleDto();
            outPutFileDto.FileName = aFileDto.FileName;
            if (!aFileDto.OriginalImage.IsEmpty())
            {
                outPutFileDto.FileName = outPutFileDto.FileName;
                outPutFileDto.OriginalImage = aFileDto.OriginalImage;
            }



            FileSimpleDto.DictExternalByteFileGuIdFileDto[fileGuidId] = outPutFileDto;

            //Must disable OriginalImage( set null)
            aFileDto.ExternalByteFileGuId = fileGuidId;
            aFileDto.OriginalImage = null;
            return aFileDto;
        }

        public static string CallFileExternalService(int fileId, string restResourceUri)
        {

            string toReturn = "";

            var objectOtPass = new { FileId = fileId, UserId = AppSecurityUserBL.CurrentUserId };
            string serializeedjson = JsonConvert.SerializeObject(objectOtPass);
            toReturn = PostSerilazedJsonAndRetrunResoneContent(restResourceUri, serializeedjson);

            return toReturn;

        }



        private static string PostSerilazedJsonAndRetrunResoneContent(string restResourceUri, string serializeedjson)
        {
            string toReturn;
            //update pdmSetup set SetupValue= 'PoPublish' where setupCode ='PublishReferenceURL'

            if (string.IsNullOrEmpty(restResourceUri))
            {
                toReturn = "Invalid Web service Url setting, please check application setting ";
            }
            else
            {

                string restEndPoint = AppSystemSettingBL.GetStringValue(EmSystemSettings.InternalApiRestEndPoint);



                var client = new RestClient(restEndPoint);

                //client.Timeout = RestApiRequestTimeout * 1000;

                var request = new RestSharp.RestRequest(restResourceUri, RestSharp.Method.Post);
                request.AddParameter("application/json; charset=utf-8", serializeedjson, ParameterType.RequestBody);
                request.RequestFormat = DataFormat.Json;
                request.AddHeader(ServerContext.CurrentUserSessionIdToken, ServerContext.Instance.CurrentSessionId.ToString());

                try
                {
                    var response = client.ExecuteAsync(request).GetAwaiter().GetResult();

                    toReturn = response.Content;

                    // it is empry message, need to  check respne error message 
                    if (string.IsNullOrWhiteSpace(toReturn))
                    {
                        if (string.IsNullOrWhiteSpace(response.ErrorMessage))
                        {
                            toReturn = " Plm Web service call failed: " + " Response is empty";

                            //SystemFramework.ApplicationLog.WriteError(toReturn);
                        }
                        else // not empty 
                        {
                            toReturn = " Plm Web service call failed: " + response.ErrorMessage;

                            //SystemFramework.ApplicationLog.WriteError(toReturn);
                        }


                    }


                }

                catch (Exception ex)
                {



                    toReturn = " Plm Web service call failed: " + ex.Message;



                    //SystemFramework.ApplicationLog.WriteError(" Plm Web service call failed: " + ex.ToString());


                }


            }

            return toReturn;
        }





        public static OperationCallResult<AppMasterDetailDto> CallTransactionFormExternalService(dynamic paramObj, string restResourceUri)
        {
            if (paramObj != null && !string.IsNullOrWhiteSpace(restResourceUri))
            {
                paramObj.CurrentUserId = AppSecurityUserBL.CurrentUserId;
                string restEndPoint = AppSystemSettingBL.GetStringValue(EmSystemSettings.InternalApiRestEndPoint);
                              

                string json = JsonConvert.SerializeObject(paramObj);

                var client = new RestClient(restEndPoint);

                var request = new RestSharp.RestRequest(restResourceUri, RestSharp.Method.Post);
                request.AddParameter("application/json; charset=utf-8", json, ParameterType.RequestBody);
                request.RequestFormat = DataFormat.Json;
                // request.AddHeader(ServerContext.CurrentUserSessionIdToken, ServerContext.Instance.CurrentSessionId.ToString());
                request.AddCookie(ServerContext.CurrentUserSessionIdToken, ServerContext.Instance.CurrentSessionId.ToString(), "/", "");
                var response = client.Execute<OperationCallResult<AppMasterDetailDto>>(request);

                if (response != null && response.Content != null)
                {
                    OperationCallResult<AppMasterDetailDto> result = JsonConvert.DeserializeObject<OperationCallResult<AppMasterDetailDto>>(response.Content);
                    return result;
                }
            }

            return null;
        }

        public static OperationCallResult<AppMasterDetailDto> CallTransactionFormIntegrationService(dynamic paramObj, string restResourceUri)
        {
            if (paramObj != null && !string.IsNullOrWhiteSpace(restResourceUri))
            {
                
                string restEndPoint = AppSystemSettingBL.GetStringValue(EmSystemSettings.InternalApiRestEndPoint).Replace("PluginApi", "DataIntegration");
               

                var operationDto = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDtoByActionCode(restResourceUri);

                if (operationDto.HttpMethd == "Get")
                {
                    var client = new RestClient(restEndPoint);

                    var request = new RestSharp.RestRequest(restResourceUri, RestSharp.Method.Get);      
                   
                    var response = client.Execute<OperationCallResult<AppMasterDetailDto>>(request);

                    if (response != null && response.Content != null)
                    {
                        OperationCallResult<AppMasterDetailDto> result = new OperationCallResult<AppMasterDetailDto>();
                        result.ValidationResult = new APP.Framework.Validation.ValidationResult();
                        result.ValidationResult.Items.Add(new ValidationItem(null, "App_CallTransactionFormIntegrationService", ValidationItemType.Message, response.Content, null));
                        return result;
                    }
                }
                else if (operationDto.HttpMethd == "Pose")
                {
                    paramObj.CurrentUserId = AppSecurityUserBL.CurrentUserId;
                    string json = JsonConvert.SerializeObject(paramObj);

                    var client = new RestClient(restEndPoint);

                    var request = new RestSharp.RestRequest(restResourceUri, RestSharp.Method.Post);
                    request.AddParameter("application/json; charset=utf-8", json, ParameterType.RequestBody);
                    request.RequestFormat = DataFormat.Json;
                    // request.AddHeader(ServerContext.CurrentUserSessionIdToken, ServerContext.Instance.CurrentSessionId.ToString());
                    request.AddCookie(ServerContext.CurrentUserSessionIdToken, ServerContext.Instance.CurrentSessionId.ToString(), "/", "");
                    var response = client.Execute<OperationCallResult<AppMasterDetailDto>>(request);

                    if (response != null && response.Content != null)
                    {
                        OperationCallResult<AppMasterDetailDto> result = new OperationCallResult<AppMasterDetailDto>();
                        result.ValidationResult = new APP.Framework.Validation.ValidationResult();
                        result.ValidationResult.Items.Add(new ValidationItem(null, "App_CallTransactionFormIntegrationService", ValidationItemType.Message, response.Content, null));
                        return result;
                    }
                }

                
            }

            return null;
        }


        public static object CallDynamicExternalDynamicService(dynamic paramObj, string restResourceUri)
        {
            if (paramObj != null && !string.IsNullOrWhiteSpace(restResourceUri))
            {
                paramObj.CurrentUserId = AppSecurityUserBL.CurrentUserId;
                string restEndPoint = AppSystemSettingBL.GetStringValue(EmSystemSettings.InternalApiRestEndPoint);

                string json = JsonConvert.SerializeObject(paramObj);

                var client = new RestClient(restEndPoint);

                var request = new RestSharp.RestRequest(restResourceUri, RestSharp.Method.Post);
                request.AddParameter("application/json; charset=utf-8", json, ParameterType.RequestBody);
                request.RequestFormat = DataFormat.Json;
                // request.AddHeader(ServerContext.CurrentUserSessionIdToken, ServerContext.Instance.CurrentSessionId.ToString());
                request.AddCookie(ServerContext.CurrentUserSessionIdToken, ServerContext.Instance.CurrentSessionId.ToString(), "/", "");
                var response = client.Execute<object>(request);

                if (response != null && response.Content != null)
                {
                    return response.Content;
                }
            }

            return null;
        }



        private static readonly string PluginApiMethod_RetrieveUserProjectTasks = "RetrieveUserProjectTasks";
        private static readonly string PluginApiMethod_RetrieveUserProjectTaskCountByStatus = "RetrieveUserProjectTaskCountByStatus";
        private static readonly string PluginApiMethod_RetrieveUserProjectTaskCountByStage = "RetrieveUserProjectTaskCountByStage";
        private static readonly string Fit_RetrieveTrainerAvailableCalendarDateTimes = "Fit_RetrieveTrainerAvailableCalendarDateTimes";
        private static readonly string Fit_RetrieveTrainerAvailableCalendarTimeSlotBySessionDuration = "Fit_RetrieveTrainerAvailableCalendarTimeSlotBySessionDuration";
        private static readonly string Fit_SearchAvailableTrainersByLocation = "Fit_SearchAvailableTrainersByLocation";

        public static List<LookupItemDto> GetAppPluginClientMethodResultColumns(string methodName)
        {
            if (!string.IsNullOrWhiteSpace(methodName))
            {
                List<LookupItemDto> toRetrun = new List<LookupItemDto>();

                methodName = methodName.Trim();

                if (methodName == PluginApiMethod_RetrieveUserProjectTasks)
                {
                    //var properties = typeof(AppProjectWorkFlowTaskExDto).GetProperties();
                    //foreach (var prop in properties)
                    //{
                    //    toRetrun.Add(new LookupItemDto() { Id = prop.Name, Display = prop.Name, ItemType = 2 });
                    //}


                    toRetrun.Add(new LookupItemDto() { Id = AppProjectWorkFlowTaskDto.IdProperty, Display = AppProjectWorkFlowTaskDto.IdProperty, ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = AppProjectWorkFlowTaskDto.NameProperty, Display = AppProjectWorkFlowTaskDto.NameProperty, ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = AppProjectWorkFlowTaskDto.DescriptionProperty, Display = AppProjectWorkFlowTaskDto.DescriptionProperty, ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = AppProjectWorkFlowTaskDto.ProjectIdProperty, Display = AppProjectWorkFlowTaskDto.ProjectIdProperty, ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = AppProjectWorkFlowTaskDto.DatePlannedStartProperty, Display = AppProjectWorkFlowTaskDto.DatePlannedStartProperty, ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = AppProjectWorkFlowTaskDto.DatePlannedEndProperty, Display = AppProjectWorkFlowTaskDto.DatePlannedEndProperty, ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = AppProjectWorkFlowTaskDto.DateActualStartProperty, Display = AppProjectWorkFlowTaskDto.DateActualStartProperty, ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = AppProjectWorkFlowTaskDto.DateActualEndProperty, Display = AppProjectWorkFlowTaskDto.DateActualEndProperty, ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = AppProjectWorkFlowTaskDto.AmountOfTimeProperty, Display = AppProjectWorkFlowTaskDto.AmountOfTimeProperty, ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = AppProjectWorkFlowTaskDto.CompletedPercentProperty, Display = AppProjectWorkFlowTaskDto.CompletedPercentProperty, ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = AppProjectWorkFlowTaskDto.TaskOwnerIdProperty, Display = AppProjectWorkFlowTaskDto.TaskOwnerIdProperty, ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = AppProjectWorkFlowTaskDto.ProjectActivityStatusIdProperty, Display = AppProjectWorkFlowTaskDto.ProjectActivityStatusIdProperty, ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = AppProjectWorkFlowTaskDto.TransactionIdProperty, Display = AppProjectWorkFlowTaskDto.TransactionIdProperty, ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = AppProjectWorkFlowTaskDto.TransactionRidProperty, Display = AppProjectWorkFlowTaskDto.TransactionRidProperty, ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "EmAppProjectTaskStage", Display = "EmAppProjectTaskStage", ItemType = 2 });
                }

                else if (methodName == PluginApiMethod_RetrieveUserProjectTaskCountByStatus)
                {
                    toRetrun.Add(new LookupItemDto() { Id = AppProjectWorkFlowTaskDto.ProjectActivityStatusIdProperty, Display = AppProjectWorkFlowTaskDto.ProjectActivityStatusIdProperty, ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "TaskCount", Display = "TaskCount", ItemType = 2 });

                }

                else if (methodName == PluginApiMethod_RetrieveUserProjectTaskCountByStage)
                {
                    toRetrun.Add(new LookupItemDto() { Id = "EmAppProjectTaskStage", Display = "EmAppProjectTaskStage", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "TaskCount", Display = "TaskCount", ItemType = 2 });

                }

                else if (methodName == Fit_RetrieveTrainerAvailableCalendarDateTimes)
                {
                    toRetrun.Add(new LookupItemDto() { Id = "TimeSlotId", Display = "TimeSlotId", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "TrainerID", Display = "TrainerID", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "WeekdayID", Display = "WeekdayID", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "CalendarDate", Display = "CalendarDate", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "StartTime", Display = "StartTime", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "EndTime", Display = "EndTime", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "StartDateTime", Display = "StartDateTime", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "EndDateTime", Display = "EndDateTime", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "IsOccupied", Display = "IsOccupied", ItemType = 2 });

                    toRetrun.Add(new LookupItemDto() { Id = "DisplayText", Display = "DisplayText", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "ColorCode", Display = "ColorCode", ItemType = 2 });



                }
                else if (methodName == Fit_RetrieveTrainerAvailableCalendarTimeSlotBySessionDuration)
                {
                    toRetrun.Add(new LookupItemDto() { Id = "SessionDuration", Display = "SessionDuration", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "TimeSlotId", Display = "TimeSlotId", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "TrainerID", Display = "TrainerID", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "WeekdayID", Display = "WeekdayID", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "CalendarDate", Display = "CalendarDate", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "StartTime", Display = "StartTime", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "EndTime", Display = "EndTime", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "StartDateTime", Display = "StartDateTime", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "EndDateTime", Display = "EndDateTime", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "IsOccupied", Display = "IsOccupied", ItemType = 2 });

                    toRetrun.Add(new LookupItemDto() { Id = "DisplayText", Display = "DisplayText", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "ColorCode", Display = "ColorCode", ItemType = 2 });



                }


                else if (methodName == Fit_SearchAvailableTrainersByLocation)
                {
                    toRetrun.Add(new LookupItemDto() { Id = "TrainerID", Display = "TrainerID", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "Distance", Display = "Distance", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "TrainerName", Display = "TrainerName", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "FullAddress", Display = "FullAddress", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "MapData", Display = "MapData", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "TrainerProfileImageID", Display = "TrainerProfileImageID", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "TrainerProfileImageID2", Display = "TrainerProfileImageID2", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "TrainerProfileImageID3", Display = "TrainerProfileImageID3", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "TrainerRating", Display = "TrainerRating", ItemType = 2 });

                    toRetrun.Add(new LookupItemDto() { Id = "RatingUD1", Display = "RatingUD1", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "RatingUD2", Display = "RatingUD2", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "RatingUD3", Display = "RatingUD3", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "RatingUD4", Display = "RatingUD4", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "RatingUD5", Display = "RatingUD5", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "RatingUD6", Display = "RatingUD6", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "RatingUD7", Display = "RatingUD7", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "RatingUD8", Display = "RatingUD8", ItemType = 2 });

                    toRetrun.Add(new LookupItemDto() { Id = "SkillDisplay", Display = "SkillDisplay", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "CertificateDisplay", Display = "CertificateDisplay", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "AgeRangeDisplay", Display = "AgeRangeDisplay", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "LocationDisplay", Display = "LocationDisplay", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "AboutMe", Display = "AboutMe", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "IsReqLastSessionID", Display = "IsReqLastSessionID", ItemType = 2 });
                    toRetrun.Add(new LookupItemDto() { Id = "TrainerNameLike", Display = "TrainerNameLike", ItemType = 2 });






                }
                return toRetrun;
            }

            return null;
        }


    }






}

//		byte[] data = new byte[] { 1, 2, 3, 4, 5 };
//		ByteArrayContent byteContent = new ByteArrayContent(data);
//		HttpResponseMessage reponse = await client.PostAsync(uri, byteContent);
//		For text only with an specific text encoding use:

//// Convert string into System.Net.Http.HttpContent using UTF-8 encoding.
//StringContent stringContent = new StringContent(
//	"blah blah",
//	System.Text.Encoding.UTF8);
//		HttpResponseMessage reponse = await client.PostAsync(uri, stringContent)

//private static FileDto HttpClientCall(dynamic twoSetSearchResult, string restResourceUri)
//{
//	FileDto aFileDto =new FileDto ();
//	string restEndPoint = AppSystemSettingBL.GetStringValue(EmSystemSettings.InternalApiRestEndPoint);

//	string json = JsonConvert.SerializeObject(twoSetSearchResult);

//	HttpClient client  = new HttpClient();

//	client.BaseAddress = new Uri("restResourceUri");
//	client.DefaultRequestHeaders.Accept.Clear();
//	client.DefaultRequestHeaders.Accept.Add(
//		new MediaTypeWithQualityHeaderValue("application/json"));


//	//HttpContent aHttpContent = new HttpContent();

//	//string json = JsonConvert.SerializeObject(dicti, Formatting.Indented);
//	HttpContent httpContent = new StringContent(json);


//	var response =  client.PostAsync("restResourceUri", httpContent).Result ;










//	//aFileDto = response.Content.ReadAsStreamAsync<>()       //  JsonConvert.DeserializeObject<FileDto>(response.Content. );



//	//string fileGuidId = System.Guid.NewGuid().ToString();
//	//TblSketchDto sketchDto = new TblSketchDto();
//	//sketchDto.SketchCode = aFileDto.FileName;
//	//sketchDto.OriginalImage = aFileDto.Content.ToArray();


//	//ServerContext.DictExternalByteFileGuIdSketchDto[fileGuidId] = sketchDto;

//	//aFileDto.ExternalByteFileGuId = fileGuidId;
//	//aFileDto.Content = null;
//	return aFileDto;
//}