using APP.Components.Dto;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL.EntityClasses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace App.BL
{
//    public static class PaypalApiBL
//    {
//        private static readonly NLog.Logger PaypalApiBLLogger = NLog.LogManager.GetCurrentClassLogger();

//        private static readonly string ApiPorverConfigName = "PaypalApi";

//        private static readonly Dictionary<string, ApiTokenDto> _DictApiToken = new Dictionary<string, ApiTokenDto>();

//        private static readonly AppWebApiProviderEntity _AppWebApiProviderEntity = AppWebApiProviderBL.GetOneApiProviderEntity(ApiPorverConfigName);

//        //api-m.sandbox  (sandbox)       
//        // api-m (live)
//        private static readonly string _PaypalUrlRefix = _AppWebApiProviderEntity.AuthorizationTypePrefix.Trim();


        

//        //Onboard sellers before payment https://developer.paypal.com/docs/platforms/seller-onboarding/before-payment/
//        //Enables you to create and get information about shared customer data.
//        public static dynamic PartnerReferrals(dynamic objectOtPass)
//        {
//            RestSharp.Method reqmethd = RestSharp.Method.POST;
//            string baseurl = string.Format(@"https://{0}.paypal.com/v2/customer/partner-referrals", _PaypalUrlRefix);
//            dynamic result = CallPapalApi( reqmethd, baseurl, objectOtPass);

//            return result;

//            //            '{
//            //    "tracking_id": "<Tracking-ID>",
//            //    "operations": [
//            //      {
//            //        "operation": "API_INTEGRATION",
//            //        "api_integration_preference": {
//            //          "rest_api_integration": {
//            //            "integration_method": "PAYPAL",
//            //            "integration_type": "THIRD_PARTY",
//            //            "third_party_details": {
//            //              "features": [
//            //                "PAYMENT",
//            //                "REFUND"
//            //             ]
//            //    }
//            //}
//            //        }
//            //      }
//            //    ],
//            //    "products": [
//            //      "EXPRESS_CHECKOUT"
//            //    ],
//            //    "legal_consents": [
//            //      {
//            //        "type": "SHARE_DATA_CONSENT",
//            //        "granted": true
//            //      }
//            //    ]
//            //}'

//            //            {
//            //                "links": [
//            //                  {
//            //      "href": "https://api-m.sandbox.paypal.com/v2/customer/partner-referrals/NDZlMjQ1YTItMGQwNi00ZjlkLWJjNmYtYjcwODNiMWEzOTk0c203SWFJeU9NQ3gvcDEvbUVaS21rWFAvSWdlV1JKWktGRGxPUFA1MEZtUT12Mg==",
//            //                    "rel": "self",
//            //                    "method": "GET",
//            //                    "description": "Read Referral Data shared by the Caller."
//            //    },
//            //    {
//            //      "href": "https://www.sandbox.paypal.com/us/merchantsignup/partner/onboardingentry?token=NDZlMjQ1YTItMGQwNi00ZjlkLWJjNmYtYjcwODNiMWEzOTk0c203SWFJeU9NQ3gvcDEvbUVaS21rWFAvSWdlV1JKWktGRGxPUFA1MEZtUT12Mg==",
//            //      "rel": "action_url",
//            //      "method": "GET",
//            //      "description": "Target WEB REDIRECT URL for the next action. Customer should be redirected to this URL in the browser."
//            //    }
//            //  ]
//            //}
//        }

//        public static dynamic PartnerReferralsByTrackingId(string trackingId)
//        {
//            string jsonBody = @"{
//                ""tracking_id"": """ + trackingId + @""",
//                ""operations"": [
//                  {
//                    ""operation"": ""API_INTEGRATION"",
//                    ""api_integration_preference"": {
//                      ""rest_api_integration"": {
//                        ""integration_method"": ""PAYPAL"",
//                        ""integration_type"": ""THIRD_PARTY"",
//                        ""third_party_details"": {
//                          ""features"": [
//                            ""PAYMENT"",
//                            ""REFUND""
//                         ]
//                        }
//                      }
//                    }
//                  }
//                ],
//                ""products"": [
//                  ""EXPRESS_CHECKOUT""
//                ],
//                ""legal_consents"": [
//                  {
//                    ""type"": ""SHARE_DATA_CONSENT"",
//                    ""granted"": true
//                  }
//                ]
//            }";


//            RestSharp.Method reqmethd = RestSharp.Method.POST;
//            string baseurl = string.Format(@"https://{0}.paypal.com/v2/customer/partner-referrals", _PaypalUrlRefix);
//            dynamic result = CallPapalApi( reqmethd, baseurl, null, jsonBody);

//            return result;
//        }

//        public static OperationCallResult<string> GeneratePayPalSellerOnboardingUrlByTrackingId(string trackingId)
//        {
//            OperationCallResult<string> operationCallResult = new OperationCallResult<string>();
//            ValidationResult validationResult = new ValidationResult();
//            operationCallResult.ValidationResult = validationResult;

//            if (!string.IsNullOrWhiteSpace(trackingId))
//            {
//                dynamic objectToPass = new ExpandoObject();
//                objectToPass.tracking_id = trackingId;

//                List<ExpandoObject> operations = new List<ExpandoObject>();
//                dynamic operationItem = new ExpandoObject();
//                operationItem.operation = "API_INTEGRATION";
//                operationItem.api_integration_preference = new ExpandoObject();
//                operationItem.api_integration_preference.rest_api_integration = new ExpandoObject();
//                operationItem.api_integration_preference.rest_api_integration.integration_method = "PAYPAL";
//                operationItem.api_integration_preference.rest_api_integration.integration_type = "THIRD_PARTY";
//                operationItem.api_integration_preference.rest_api_integration.third_party_details = new ExpandoObject();
//                operationItem.api_integration_preference.rest_api_integration.third_party_details.features = new string[] { "PAYMENT", "REFUND" };
//                operations.Add(operationItem);
//                objectToPass.operations = operations;

//                objectToPass.products = new string[] { "EXPRESS_CHECKOUT" };

//                List<ExpandoObject> legal_consents = new List<ExpandoObject>();
//                dynamic legal_consent_item = new ExpandoObject();
//                legal_consent_item.type = "SHARE_DATA_CONSENT";
//                legal_consent_item.granted = true;
//                legal_consents.Add(legal_consent_item);
//                objectToPass.legal_consents = legal_consents;

//                dynamic resonseObj = PaypalApiBL.PartnerReferrals(objectToPass);

//                string linkUrl = "";

//                if (resonseObj != null && resonseObj.links != null)
//                {
//                    foreach (var aLink in resonseObj.links.Children())
//                    {
//                        if (aLink.rel.Value == "action_url")
//                        {
//                            linkUrl = aLink.href.Value;
//                            break;
//                        }
//                    }
//                }

//                if (!string.IsNullOrWhiteSpace(linkUrl))
//                {
//                    operationCallResult.Object = linkUrl;
//                    validationResult.AddItem(null, "Ex_Method_GeneratePayPalSellerOnboardingUrlByTrackingId_OnboardingSuccess", ValidationItemType.Message, "Onboarding Success.");
//                }
//                else
//                {
//                    validationResult.AddItem(null, "Ex_Method_GeneratePayPalSellerOnboardingUrlByTrackingId_OnboardingFailed", ValidationItemType.Warning, "Onboarding Failed.");
//                }
//            }
//            else
//            {
//                validationResult.AddItem(null, "Ex_Method_GeneratePayPalSellerOnboardingUrlByTrackingId_InvalidPartnerId", ValidationItemType.Warning, "Invalid PartnerId.");
//            }

//            return operationCallResult;
//        }

//        public static dynamic TrackSellerOnboardingStatusByTrackingId(string trackingId)
//        {
//            string partnerId = "";
//            RestSharp.Method reqmethd = RestSharp.Method.GET;

//            string baseurl = string.Format(@"https://{0}.paypal.com/v1/customer/partners/{1}/merchant-integrations?tracking_id={2}",
//                _PaypalUrlRefix, partnerId, trackingId);

//            //string baseurl = string.Format(@"https://{0}.paypal.com/v1/identity/oauth2/userinfo?schema=paypalv1.1",
//            //   _PaypalUrlRefix, partnerId, trackingId);
//            //// v1/identity/oauth2/userinfo?schema=paypalv1.1

//            dynamic result = CallPapalApi( reqmethd, baseurl,null);

//            return result;

//        }


//        //   string ordermessage = @"{
//        //  ""intent"": ""CAPTURE"",
//        //  ""purchase_units"": [
//        //    {
//        //      ""amount"": {
//        //        ""currency_code"": ""USD"",
//        //        ""value"": ""100.00""
//        //      }
//        //    }
//        //  ]
//        //}";
//        public static dynamic CaptureOrder(dynamic objectOtPass)
//        {
//            //  dynamic objectOtPass = new ExpandoObject();
//            //  objectOtPass.intent = "CAPTURE";
//            //  List<ExpandoObject> unitsList = new List<ExpandoObject>();
//            //  dynamic unit = new ExpandoObject();
//            //  unit.amount = new ExpandoObject();
//            //  unit.amount.currency_code = "USD";
//            //  unit.amount.value = "100.00";
//            //  unitsList.Add(unit);
//            //  objectOtPass.purchase_units = unitsList;

//            RestSharp.Method reqmethd = RestSharp.Method.POST;
//            string baseurl = string.Format(@"https://{0}.paypal.com/v2/checkout/orders", _PaypalUrlRefix);
//            dynamic result = CallPapalApi( reqmethd, baseurl, objectOtPass);

//            return result;

//        }

//        //https://developer.paypal.com/docs/api/payments/v2/#captures

//        public static dynamic CaptureAuthorization(string authorizationID)
//        {

//            RestSharp.Method reqmethd = RestSharp.Method.POST;
           

//            string baseurl = string.Format(@"https://{0}.paypal.com/v2/payments/authorizations/{1}/capture", _PaypalUrlRefix, authorizationID);

//            PaypalApiBLLogger.Warn("CaptureURL: " + baseurl);

//            dynamic result = CallPapalApi( reqmethd, baseurl, null);

            
//            return result;

//        }

//        public static dynamic CancelAuthorization(string authorizationID)
//        {

//            ///
//            //https://developer.paypal.com/docs/api/payments/v2/#authorizations_void
//            ///v2/payments/authorizations/{authorization_id}/void
//            ///
//            ///Voids, or cancels, an authorized payment, by ID. You cannot void an authorized payment that has been fully captured.
//            ///
//            //Response
//            //A successful request returns the HTTP 204 No Content status code with no JSON response body.

//            RestSharp.Method reqmethd = RestSharp.Method.POST;
//            string baseurl = string.Format(@"https://{0}.paypal.com/v2/payments/authorizations/{1}/void", _PaypalUrlRefix, authorizationID);
//            dynamic result = CallPapalApi(reqmethd, baseurl, null);

//            return result;

//        }

//        public static dynamic Payouts(dynamic objectOtPass)
//        {
//            //https://developer.paypal.com/docs/api/payments.payouts-batch/v1#definition-payout_item

//            //var senderBatchHeaderObj = new
//            //{
//            //    sender_batch_id = "Payouts_123456",
//            //    email_subject = "You have a payout!",
//            //    email_message = "You have received a payout! Thanks for using our service!",
//            //};

//            //List<ExpandoObject> payoutItemList = new List<ExpandoObject>();
//            //dynamic payoutItem = new ExpandoObject();
//            //payoutItem.recipient_type = "EMAIL";
//            //payoutItem.amount = new ExpandoObject();
//            //payoutItem.amount.currency = "USD";
//            //payoutItem.amount.value = "100.00";
//            //payoutItem.note = "Fit session payout for session# 123456";
//            //payoutItem.sender_item_id = "Item_123456";
//            //payoutItem.receiver = "TrainerA@personal.example.com";
//            //payoutItemList.Add(payoutItem);

//            //var objectToPass = new
//            //{
//            //    sender_batch_header = senderBatchHeaderObj,
//            //    items = payoutItemList
//            //};
//            //Post: create new payout API
//            RestSharp.Method reqmethd = RestSharp.Method.POST;
//            string baseurl = string.Format(@"https://{0}.paypal.com/v1/payments/payouts", _PaypalUrlRefix);
//            dynamic result = CallPapalApi( reqmethd, baseurl, objectOtPass);

//            return result;

//        }

//        //https://developer.paypal.com/docs/api/payments/v2/#authorizations_reauthorize        
//        public static dynamic Reauthorize(dynamic objectOtPass, string authorization_id)
//        {
//            RestSharp.Method reqmethd = RestSharp.Method.POST;
//            string baseurl = string.Format(@"https://{0}.paypal.com/v2/payments/authorizations/{1}/reauthorize", PaypalApiBL._PaypalUrlRefix, authorization_id);
//            dynamic result = PaypalApiBL.CallPapalApi( reqmethd, baseurl, objectOtPass);

//            return result;

//        }
//// statusenum
////The status for the authorized payment.

////The possible values are:

////CREATED.The authorized payment is created.No captured payments have been made for this authorized payment.
////CAPTURED.The authorized payment has one or more captures against it.The sum of these captured payments is greater than the amount of the original authorized payment.
////DENIED.PayPal cannot authorize funds for this authorized payment.
////EXPIRED.The authorized payment has expired.
////PARTIALLY_CAPTURED.A captured payment was made for the authorized payment for an amount that is less than the amount of the original authorized payment.
////PARTIALLY_CREATED.The payment which was authorized for an amount that is less than the originally requested amount.
////VOIDED.The authorized payment was voided.No more captured payments can be made against this authorized payment.
////PENDING.The created authorization is in pending state. For more information, see status.details.
////Read only.

//        //https://developer.paypal.com/docs/api/payments/v2/#authorizations_get
//        public static dynamic ShowsAuthorizedPaymentDetails(string authorization_id)
//        {
//            RestSharp.Method reqmethd = RestSharp.Method.GET;
//            string baseurl = string.Format(@"https://{0}.paypal.com/v2/payments/authorizations/{1}", PaypalApiBL._PaypalUrlRefix, authorization_id);
//            dynamic result = PaypalApiBL.CallPapalApi(reqmethd, baseurl, null);

//            return result;

//        }



//        //https://developer.paypal.com/docs/integration/direct/transaction-search/
//        //https://developer.paypal.com/docs/api/payments/v2/#captures_get
//        public static dynamic CheckCaptureStatus(string capture_id)
//        {
//            RestSharp.Method reqmethd = RestSharp.Method.GET;
//            string baseurl = string.Format(@"https://{0}.paypal.com/v2/payments/captures/{1}", PaypalApiBL._PaypalUrlRefix, capture_id);
//            dynamic result = PaypalApiBL.CallPapalApi( reqmethd, baseurl, null);

//            return result;

//        }

//        //https://developer.paypal.com/docs/api/payments.payouts-batch/v1/
//        public static dynamic CheckPayoutStatus(string payout_batch_id)
//        {
//            RestSharp.Method reqmethd = RestSharp.Method.GET;
//            string baseurl = string.Format(@"https://{0}.paypal.com//v1/payments/payouts/{1}", PaypalApiBL._PaypalUrlRefix, payout_batch_id);
//            dynamic result = PaypalApiBL.CallPapalApi( reqmethd, baseurl, null);

//            return result;

//        }

//        //TODO after 3 days
//        // 1)https://developer.paypal.com/docs/api/payments/v2/#authorizations_reauthorize
//        // 2) https://developer.paypal.com/docs/api/transaction-search/v1/
//        // authorizationID was obtained from web-sdk ( sdk key link to merchandise account, when capture all money  will go to merchande account account
//        // how instrcutor get capture  from client??, does client need to know trainer account? ? 
//        // split payment from authrization ?? from capture
//        public static dynamic GetOrderInfo(string orderID)
//        {

//            RestSharp.Method reqmethd = RestSharp.Method.GET;
//            string baseurl = string.Format(@"https://{0}.paypal.com/v2/checkout/orders/{1}", _PaypalUrlRefix, orderID);
//            dynamic result = CallPapalApi(reqmethd, baseurl,null);

//            return result;

//        }

//        public static dynamic GetAuthorizationsInfo(string authorizationID)
//        {

//            RestSharp.Method reqmethd = RestSharp.Method.GET;
//            string baseurl = string.Format(@"https://{0}.paypal.com/v2/payments/authorizations/{1}", _PaypalUrlRefix, authorizationID);
//            dynamic result = CallPapalApi( reqmethd, baseurl,null);

//            return result;

//        }


//        public static dynamic CallPapalApi( Method reqmethd, string baseurl, dynamic postObjectData, string postJosnText = "")
//        {

//            var client = new RestClient(baseurl);
//            var request = new RestSharp.RestRequest(reqmethd);

//            string token = GetToken();

//            request.AddHeader("content-type", "application/json");
//            request.AddHeader("authorization", "Bearer " + token);


//            string serializeedjson = "";

//            if (!string.IsNullOrWhiteSpace(postJosnText))
//            {
//                serializeedjson = postJosnText;
//            }

//            else if (postObjectData != null)
//            {
//                serializeedjson = JsonConvert.SerializeObject(postObjectData);
//                request.AddParameter("application/json; charset=utf-8", serializeedjson, ParameterType.RequestBody);
//            }

           


//            request.RequestFormat = DataFormat.Json;

//            string orgResponseContent = string.Empty;

//            try {
//                var response = client.Execute(request);
//                orgResponseContent = response.Content;

//                var result = JsonConvert.DeserializeObject<dynamic>(response.Content);
//                return result;
//            }
//            catch (Exception ex)
//            {
//                dynamic exeptionObj = new ExpandoObject();
//                exeptionObj.ExceptionMessage = ex.ToString() + " Org Response Content: " + orgResponseContent;


//                return exeptionObj;
//            }
            
//        }

//        private static string GetToken()
//        {
//            string toReturn = "";
//            toReturn = ApiTokenDto.GetValidtokeFromDict(_DictApiToken);

//            if (string.IsNullOrEmpty(toReturn))
//            {
//                var token_expire = GetFromServerToken();
//                ApiTokenDto aApiTokenDto = ApiTokenDto.ConvertTokenExpireToDto(token_expire);
//                _DictApiToken[ApiTokenDto.ApiTokenKey] = aApiTokenDto;

//                toReturn = aApiTokenDto.Token;
//            }
//            return toReturn;
//        }


//        //will return acesstoken token, and expire int double.Parse (expires_in))
//        private static Tuple<string, double> GetFromServerToken()
//        {


//            //    'ASO3vShHiZH9YVklJwyKgcgvl0UE4oda7lnnKukkJEQ1wrYnWEr1yKGwuznDthGG0ZtfGon',
//            //  'EFOzp48Qpu0f7Rpo-3gWnw0HOtc_QJs4Hy8dnSUe63jkVqTqOowdPLsRmk5tV7FlZAq-abSaedK43qjU',

//            string clientId = _AppWebApiProviderEntity.ApiKey;
//            string seceret = _AppWebApiProviderEntity.ApiSecret;

//            //string clientId = @"ASO3vShHiZH9YVklJwyKgcgvl0UE4oda7lnnKukkJEQ1wrYnWEr1yKGwuznDthGG0ZtfGon-bgPcPueC";
//            // string seceret = @"EFOzp48Qpu0f7Rpo-3gWnw0HOtc_QJs4Hy8dnSUe63jkVqTqOowdPLsRmk5tV7FlZAq-abSaedK43qjU";


//            var byteArray = Encoding.ASCII.GetBytes(string.Format("{0}:{1}", clientId, seceret));
//            string encodeString = string.Format("Basic {0}", Convert.ToBase64String(byteArray));


//            string baseurl = string.Format(@"https://{0}.paypal.com/v1/oauth2/token", _PaypalUrlRefix);

//            RestClient clientget = new RestClient(baseurl);
//            // clientget.Authenticator = new JwtAuthenticator(yourAccessToken);


//            var requestget = new RestRequest(Method.POST);

//            requestget.AddHeader("Authorization", encodeString);

//            requestget.AddHeader("Content-Type", "application/x-www-form-urlencoded");

//            // request.AddParameter("application/x-www-form-urlencoded", $"app_id={token.app_id}&secret={token.secret}&grant_type={token.grant_type}&Username={token.Username}&Password={token.Password}", ParameterType.RequestBody);

//            requestget.AddParameter("application/x-www-form-urlencoded", "grant_type=client_credentials", ParameterType.RequestBody);


//            // requestget.AddHeader("authorization", "Bearer " + jwtToken);
//            IRestResponse response1 = clientget.Execute(requestget);


//            string content = response1.Content;

//            dynamic serializeedjson = JsonConvert.DeserializeObject(content);

//            string token = serializeedjson.access_token;
//            string expires_in = serializeedjson.expires_in;

//            Tuple<string, double> returnValue = new Tuple<string, double>(token, double.Parse(expires_in));

//            //        "scope": "https://uri.paypal.com/services/invoicing https://uri.paypal.com/services/vault/payment-tokens/read https://uri.paypal.com/services/disputes/read-buyer https://uri.paypal.com/services/payments/realtimepayment https://uri.paypal.com/services/disputes/update-seller https://uri.paypal.com/services/payments/payment/authcapture openid https://uri.paypal.com/services/disputes/read-seller Braintree:Vault https://uri.paypal.com/services/payments/refund https://api.paypal.com/v1/vault/credit-card https://api.paypal.com/v1/payments/.* https://uri.paypal.com/payments/payouts https://uri.paypal.com/services/vault/payment-tokens/readwrite https://api.paypal.com/v1/vault/credit-card/.* https://uri.paypal.com/services/subscriptions https://uri.paypal.com/services/applications/webhooks",
//            //"access_token": "A21AAIOQBpMsAxGTNDs8XG9e_kIh6gQ7MuxycxkEy-HmxGV8OKGJxLJQG_a14gJtoMUBFYsrKEo1bvPya7KVzCMpx9M_niUgQ",
//            //"token_type": "Bearer",
//            //"app_id": "APP-80W284485P519543T",
//            //"expires_in": 32399,
//            //"nonce": "2020-12-12T01:24:58ZK2CBs9K9mGhEyHQZ7jyEK8X1dwQ1UZmC5c_fhizdgrY"

//            return returnValue;
//        }


//    }



}