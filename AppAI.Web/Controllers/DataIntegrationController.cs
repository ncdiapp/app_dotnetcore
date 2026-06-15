using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using APP.Framework.Communication;
using App.BL;
using AppAI.Web.Controllers.Base;
using ExchangeBL;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace AppAI.Web.Controllers;

[Route("webapi/DataIntegration/[action]")]
public class DataIntegrationController : SecureBaseController
{
    ////http://localhost/AppBuilder/webapi/DataIntegration/Test
    [HttpGet]
    public string Test()
    {
        return " Echo from App DataExchangeApi 111";
    }


    private HttpResponseMessage _resposneMessage = null;


    // !!!!!!!!!!!! For Simple Integration
    /// <summary>
    /// Get data from database
    /// </summary>
    /// <param name="actionName"></param>
    /// <returns></returns>
    public async Task<HttpResponseMessage> GetAsync(string actionName)
    {
        _resposneMessage = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, };

        try
        {
            if (string.IsNullOrWhiteSpace(actionName))
            {
                throw new Exception("There is no action name");
            }

            var queryParameters = Request.Query.Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value.ToString())).ToList();

            if (queryParameters.Any(kv => kv.Key.Equals("wsdl", StringComparison.InvariantCultureIgnoreCase)))
            {
                var exampleData = DataExchangeWithoutJsonSchemaBL.GetSample(actionName);

                if (!string.IsNullOrWhiteSpace(exampleData))
                {
                    _resposneMessage.Content = new StringContent(exampleData.ToString());

                    _resposneMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                }
            }
            else
            {
                var responseStream = await DataExchangeWithoutJsonSchemaBL.GetAsync(actionName, queryParameters);

                if (responseStream.GetType() == typeof(MemoryStream))
                {
                    _resposneMessage.Content = new StreamContent((MemoryStream)responseStream);
                }
                else
                {
                    _resposneMessage.Content = new StringContent(responseStream.ToString());
                }


                _resposneMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }
        }
        catch (Exception ex)
        {
            _resposneMessage.Content = new StringContent(ex.Message);
            _resposneMessage.StatusCode = HttpStatusCode.InternalServerError;
        }

        return _resposneMessage;
    }

    /// <summary>
    /// Save json data
    /// </summary>
    /// <param name="actionName"></param>
    /// <returns></returns>
    public async Task<HttpResponseMessage> PostAsync(string actionName)
    {
        _resposneMessage = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK };

        try
        {
            if (string.IsNullOrWhiteSpace(actionName))
            {
                throw new Exception("THere is no action name");
            }

            var requestBody = await new System.IO.StreamReader(Request.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                throw new Exception("THere is post data");
            }

            var queryParameters = Request.Query.Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value.ToString())).ToList();

            var responseJson = DataExchangeWithoutJsonSchemaBL.ExecuteApiOperationSaveCommand(actionName, requestBody, queryParameters);

            if (!string.IsNullOrWhiteSpace(responseJson))
            {
                _resposneMessage.Content = new StringContent(responseJson);

                _resposneMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }
        }
        catch (Exception ex)
        {
            _resposneMessage.Content = new StringContent(ex.Message);
            _resposneMessage.StatusCode = HttpStatusCode.InternalServerError;
        }

        return _resposneMessage;
    }
}
