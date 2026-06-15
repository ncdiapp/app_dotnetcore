using System;
using System.Text;
using System.Net;
using System.IdentityModel.Tokens.Jwt;

using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Security.Principal;
using APP.Components.Dto;
using System.Linq;
using Newtonsoft.Json;

namespace App.BL
{
    public static class ExternalAccessTokeParseBL
    {

        public static string GetEmailFromExternalToken(EmAppExternalLoginType signInType, string externalAceeeTokenOrIdToken)
        {
            string userEmail = string.Empty;
            // need to pass IDToken
            if (signInType == EmAppExternalLoginType.Google)
            {
                userEmail = GetGoogleEmailAddressFromIdToken(externalAceeeTokenOrIdToken);
            }
            // need to pass acess token
            else if (signInType == EmAppExternalLoginType.Facebook)
            {
                userEmail = GetFacebookEmailAddressFromAccessToken(externalAceeeTokenOrIdToken);
            }
            // need more test: 
            // ID token or acess token
            else if (signInType == EmAppExternalLoginType.Apple)
            {
                userEmail= GetAppleEmailAddressFromIdToken(externalAceeeTokenOrIdToken);
            }

            return userEmail;
        }

        // Client !!
        //var profile = googleUser.getBasicProfile();
        //let idToken = googleUser.getAuthResponse().id_token;
        //let email = profile.getEmail();
        //var masterPageScope = angular.element(document.getElementById('MasterNavigationPageContainer')).scope();
        //    if (masterPageScope && masterPageScope.eSitePartnerUserThirdPartAccountLoginPostProcess) {
        //        let googleLoginType = 1;
        //let externalSignInTokenDto = { };
        //externalSignInTokenDto.EmExternalSigninType = googleLoginType;
        //        externalSignInTokenDto.ExternalAcessToken = idToken;
        //        masterPageScope.eSitePartnerUserThirdPartAccountLoginPostProcess(externalSignInTokenDto);
        //    }

    private static string GetGoogleEmailAddressFromIdToken(string acessToken)
        {
            string userEmail = string.Empty;
            JwtSecurityTokenHandler jwtHandler = new JwtSecurityTokenHandler();
            string jwtInput = acessToken;
            var jwtOutput = string.Empty;

            if (!jwtHandler.CanReadToken(jwtInput)) throw new Exception("The token doesn't seem to be in a proper JWT format.");

            JwtSecurityToken token = jwtHandler.ReadJwtToken(jwtInput);

            if (token != null && token.Claims != null)
            {
                var emailClaim = token.Claims.FirstOrDefault(o => o.Type == "email");
                if (emailClaim != null)
                {
                    userEmail = emailClaim.Value;
                }
            }

            return userEmail;
        }
        //https://jwt.io/
        //https://developer.apple.com/documentation/sign_in_with_apple/generate_and_validate_tokens
        //https://developer.apple.com/forums/thread/118209
        // Must use gmail to loingin, cannot create another domaind account for personal gmail !!

//        [access_token] => a2293d83289aa41f7ad22de6844511826.0.mzuq.oCJsXOEXZQX1v8NQCQOy0g

//[token_type] => Bearer

//[expires_in] => 3600

//[refresh_token] =>

//[id_token] => REeyJraWQiOiJBSURPUEsxIiwiYWxnIjoiUlMyNTYifQ.eyJpc3MiOiJodHRwczpcL1wvYXBwbGVpZC5hcHBsZS5jb20iLCJhdWQiOiJ3d3cuZXhhbXBsZS5jb20iLCJleHAiOjE1NjE0OTA2MTUsImlhdCI6MTU2MTUzMzU4OCwic3ViIjoiMDAwMTMyLmMzTWlPaUpvZEhSd2N6b3ZMMkZ3Y0d4bGFXUWFzZHNhLjg5MTQiLCJhdF9oYXNoIjoiQnBiVmVmTm5waVBUY1BzcWt3VEppZyIsImVtYWlsX2R1bW15IjoiZXhhbXBsZUBwcml2YXRlcmVsYXkuYXBwbGVpZC5jb20ifQ==.SyCF8jT50FHALit-u9H_TyzPikirYnDq1RiDT3ennHQrNl0UcRE4bDmVM1qlG2cfHPH5OtpyQZIjGi_r9v7ZoN2EfyDGlg08yEWGwwCNlrCkcHcA9gjNN2RYmT4Yt3toRLgnwSDyzHOP6FS7I1kzwcdZmJTuGrYPThxe80F6rQABUWUBDAl2KgP7ujt1j8H3LrfV0r3RKTHA7azWWu9rVAFrx1_IeRk-ASDW0OPrqDJoF8YdZF1Da4-br-gTOt_LJhZFhuPh1WDgZj6AAcytTrSL4AhW2BrN_U0bMw47nw7k9OZbcbDNb-j3hEAkQdvZYEBHIRtEMxrzTAgs7oxbtg

//[iss] => https://appleid.apple.com

//[aud] => www.example.com[exp] => 1560776678

//[iat] => 1560776078

//[sub] => 000132.c3MiOiJodHRwczovL2FwcGxlaWQasdsa.8914

//[at_hash] => SrJXsKX1f4FpGPFmiUPzUQ

//[user] => {"name":{"firstName":"Jane","middleName":"","":"Doe"},"email":"j123easj2@privaterelay.appleid.com"} (ENCODE BASE64)
        private static string GetAppleEmailAddressFromIdToken(string idLongToken)
        {
            string userEmail = string.Empty;
            JwtSecurityTokenHandler jwtHandler = new JwtSecurityTokenHandler();
            string jwtInput = idLongToken;
            var jwtOutput = string.Empty;

            if (!jwtHandler.CanReadToken(jwtInput)) throw new Exception("The token doesn't seem to be in a proper JWT format.");

            JwtSecurityToken token = jwtHandler.ReadJwtToken(jwtInput);

            // there is no email address on id toke calims //??/
            if (token != null && token.Claims != null)
            {
                var emailClaim = token.Claims.FirstOrDefault(o => o.Type == "email");
                if (emailClaim != null)
                {
                    userEmail = emailClaim.Value;
                }
            }

            return userEmail;
        }


     //  apple test: {"authorizationCode": "cca2da85df67d431e80813813effed861.0.rvyy.RLeMbUS-1sUyiq2kpWXE9Q", "authorizedScopes": [], "email": null, "fullName": {"familyName": null, "givenName": null, "middleName": null, "namePrefix": null, "nameSuffix": null, "nickname": null}, "identityToken": "eyJraWQiOiI4NkQ4OEtmIiwiYWxnIjoiUlMyNTYifQ.eyJpc3MiOiJodHRwczovL2FwcGxlaWQuYXBwbGUuY29tIiwiYXVkIjoiY29tLmZpdC1jb25jaWVyZ2UiLCJleHAiOjE2MTU0ODM0NDEsImlhdCI6MTYxNTM5NzA0MSwic3ViIjoiMDAwNTg4LjI2YmI4ZmJkMDZmZDQ0YWViM2Q3NWU5NzM2MjQ2MTI2LjExMjYiLCJub25jZSI6ImE1YzQ1ZWY5NWYzMTI5OGRiOTUyZDk3MmYxYWRmYTNkOTliYWMwODcyZTdhNjQzNGNhZjZhMTA0MzM0N2IwM2QiLCJjX2hhc2giOiJ3WGJlRVBjTkFqeEI1VXRjaUxkOFZ3IiwiZW1haWwiOiJrb250YWt0QGdvcnJpb24ucGwiLCJlbWFpbF92ZXJpZmllZCI6InRydWUiLCJhdXRoX3RpbWUiOjE2MTUzOTcwNDEsIm5vbmNlX3N1cHBvcnRlZCI6dHJ1ZX0.cjRdZyeMQYw3avehh6F08UhbE8LnX4zScGjHkYy-PktRkvCKeJshbxpp0zvXS_7DutsdZ3js6gL53cr26xFW_VlAEDV8ZgX3mrTMWpB-Q49RH1iNP-CPcj-lwzstWdsX8V-uNEiFP7ALtX-TsX4btQ7mi4xTIXMiUfr9Cju2ENcpI-txs6UHej0Y2cmjrFxEPnkyOCjBfy5ZLPPAmHzJ1pYY_CQxnQs3mhLYOVaktHjOeCmK-duUpG1jbFfIxF0JLGfOxztwFHlut1qa6DR5wDIxa8bWytn0HQyaCnFc-uNV4eElcg06Oi4-qzA4DTouNI3QBfSrPum4YwriL2xSSA", "nonce": "AwVl2OTOfRogGfodZ7RcPjYy9u-s3qDX", "realUserStatus": 1, "state": null, "user": "000588.26bb8fbd06fd44aeb3d75e9736246126.1126"}

    //        {
    //    "access_token": "a8a553508e53e48b19592886f08f9a6b0.0.mwvx.eRuGAbf8uDOD0ZeOrhHE3w",

    //    "token_type": "Bearer",

    //    "expires_in": 3600,

    //    "refresh_token": "r9e03ba3c70ef4b2b9bf67281b0914ea1.0.mwvx.MUIwF6uk5OsIYIJNY3zanw",

    //    "id_token": "eyJraWQiOiJBSURPUEsxIiwiYWxnIjoiUlMyNTYifQ.eyJpc3MiOiJodHRwczovL2FwcGxlaWQuYXBwbGUuY29tIiwiYXVkIjoiY29tLnRlbmNlbnQucXFtdXNpY2JhYnkuZCIsImV4cCI6MTU3MDYxNTU3NywiaWF0IjoxNTcwNjE0OTc3LCJzdWIiOiIwMDA2NTcuNjY1NjJkM2IxMWJjNDAzMDk5YjFjZGI0OTQ3YzFkM2MuMTE0MiIsImF0X2hhc2giOiJibFQ3UTFNMDF1NW12Y0ZVZ1JIZGR3IiwiYXV0aF90aW1lIjoxNTcwNjE0OTI0fQ.TXpunnl6hlJs8C9_W7k-LeJ3Lm_otBeLoJxwe1C2oufKmMWxlANu0KI2-LnTcHYx23npMY3swk4fM46W5Vt9ursllz27P4zR8H1eoZ2Tj-3O3rTz8lqC1uI-mMo_a6VxqXvNmqenre5S0CyaUHAI1_Um9798b4ehduJqDtYVYIbftYIpiXBAW-vGjEbBnjWkHw_7HmjEWrsc0vfPhHGXyUMFmon4VhMBzzY2Nq0LIF4NP9Aa_9dyTzdEaqNfPjdSbFCVaJcTI_rxrIbooh18UbdowsFJtnLKsTZ7ePYtz3uBIaWUaiwJI1oU6ZeAb6uAzHl7TV2DdB9UkHDJe960hg"

    //}

    //        if (response.authResponse && response.authResponse.accessToken) {
    //console.log(response.authResponse);
    //loginToAppWithFacebookAccessToken(response.authResponse.accessToken, response.authResponse.userID);
    // }

    // user can use different email account to sign in facebook, 
    private static string GetFacebookEmailAddressFromAccessToken(string onlyAccessToken)
        {
            // https://graph.facebook.com/v6.0/me?access_token=<REDACTED>&fields=name%2Cemail&method=get&pretty=0&sdk=joey&suppress_http_code=1

            // https://graph.facebook.com/{your-user-id}?fields = birthday,email,hometown& access_token ={ your - user - access - token}

            string userEmail = string.Empty;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            //string baseurl = @"https://graph.facebook.com/v6.0/me?access_token=" + accessToken + " & fields=name%2Cemail&method=get&pretty=0&sdk=joey&suppress_http_code=1";

            string baseurl = @"https://graph.facebook.com/me?access_token=" + onlyAccessToken + "&fields=id,name,email,picture";

            RestSharp.RestClient clientget = new RestSharp.RestClient(baseurl);

            var requestget = new RestSharp.RestRequest();


            RestSharp.RestResponse response1 = clientget.ExecuteAsync(requestget).GetAwaiter().GetResult();

            if (response1 != null && !string.IsNullOrWhiteSpace(response1.Content))
            {
                dynamic contentData = JsonConvert.DeserializeObject(response1.Content);
                userEmail = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(contentData.email);
            }

            return userEmail;
        }


    }


}