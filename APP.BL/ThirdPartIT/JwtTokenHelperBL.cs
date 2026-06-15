using System;
using System.Text;
using System.Net;
using System.IdentityModel.Tokens.Jwt;

using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Security.Principal;
#if NETFRAMEWORK
using System.ServiceModel.Security.Tokens;
using Microsoft.Owin.Security.DataHandler.Encoder;
using System.IdentityModel.Claims;
#endif
using System.Configuration;

//namespace App.BL
//{
//    public static class JwtTokenHelperBL
//    {

//        static readonly char[] padding = { '=' };
//        //static readonly string apiKey = "jywYLFAgSwqd_0Uggz06fA";
//       // static readonly string apiSecret = "xOKIgOL6dNspLsFGokjaFbSeJQvTdC64PmnP";

//            // private key is kept in servide side, client nerver can acess priate
//            // there is no need to password UserId(zoom account email address)
//            // zoom
//        public static string CreateJWTToken(string apiKey, string apiSecret, double expireMinute)
//        {
//            // Token will be good for 20 minutes
//            DateTime expiry = DateTime.UtcNow.AddMinutes(expireMinute);


//            //   // string jwtToken = @"eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJhdWQiOm51bGwsImlzcyI6IjlBbG51Sk5sUW11U0dIMlNsMzVSWFEiLCJleHAiOjE1ODg2MDQ0MzcsImlhdCI6MTU4Nzk5OTQzNH0.EpOrC7eqkThlkuj6G4xrL0WBv4x2Ez8D-w9r3sYZN60";
//            JwtSecurityToken secToken = CreateJwtKey(expiry, apiKey, apiSecret);

//            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();

//            // Token to String so you can use it in your client
//            var tokenString = handler.WriteToken(secToken);

         

//            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
//            return tokenString;
//        }

//        //JWT decode vs verify - Understanding which to use for token verification
//        private static bool ValidateToken(string authToken,string samekeyAsGeneratethetoken)
//        {
//            var tokenHandler = new JwtSecurityTokenHandler();
//            var validationParameters = GetValidationParameters(samekeyAsGeneratethetoken);

//            SecurityToken validatedToken;
//            IPrincipal principal = tokenHandler.ValidateToken(authToken, validationParameters, out validatedToken);
//            return true;
//        }


//        //validate a JWT token
//        private static string GenerateToken()
//        {
//            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("secrekey"));
//            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

//            var secToken = new JwtSecurityToken(
//                signingCredentials: credentials,
//                issuer: "Sample",
//                audience: "Sample",
              
//                expires: DateTime.UtcNow.AddDays(1));


//        //secToken.Claims: new[] { new Claim(JwtRegisteredClaimNames.Sub, "meziantou") },

//            var handler = new JwtSecurityTokenHandler();
//            return handler.WriteToken(secToken);
//        }

//        private static TokenValidationParameters GetValidationParameters(string samekeyAsGeneratethetoken)
//        {
//            return new TokenValidationParameters()
//            {
//                ValidateLifetime = false, // Because there is no expiration in the generated token
//                ValidateAudience = false, // Because there is no audiance in the generated token
//                ValidateIssuer = false,   // Because there is no issuer in the generated token
//                ValidIssuer = "Sample",
//                ValidAudience = "Sample",
//                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(samekeyAsGeneratethetoken)) // The same key as the one that generate the token
//            };
//        }

//        public static string ReadToken(string jwtInput)
//        {
//            JwtSecurityTokenHandler jwtHandler = new JwtSecurityTokenHandler();
//            var jwtOutput = string.Empty;

//            // Check Token Format
//            if (!jwtHandler.CanReadToken(jwtInput)) throw new Exception("The token doesn't seem to be in a proper JWT format.");

//            JwtSecurityToken token = jwtHandler.ReadJwtToken(jwtInput);

//            // // Re-serialize the Token Headers to just Key and Values
//            // var jwtHeader = JsonConvert.SerializeObject(token.Header.Select(h => new { h.Key, h.Value }));
//            //// jwtOutput = @"{{\r\n\"Header\":\r\n{JToken.Parse(jwtHeader)},";

//            // // Re-serialize the Token Claims to just Type and Values
//            // var jwtPayload = JsonConvert.SerializeObject(token.Claims.Select(c => new { c.Type, c.Value }));
//            // // jwtOutput += $"\r\n\"Payload\":\r\n{JToken.Parse(jwtPayload)}\r\n}}";

//            // Output the whole thing to pretty Json object formatted.
//            return "";//JToken.Parse(jwtOutput).ToString(Formatting.Indented);
//        }
//        public static string GetWebUrlMeetingSignature(string apiKey, string apiSecret,string meetingNumber, string role )
//        {



//            String ts = (ToTimestamp(DateTime.UtcNow.ToUniversalTime()) - 30000).ToString();
           
//            string token = GenerateMeetingSignatureToken( apiKey,  apiSecret,meetingNumber, ts, role);
//            return token;
//            //Console.WriteLine(token);
//        }

//        public static long ToTimestamp(DateTime value)
//        {
//            long epoch = (value.Ticks - 621355968000000000) / 10000;
//            return epoch;
//        }
//        private static JwtSecurityToken CreateJwtKey(DateTime expiry, string apiKey, string apiSecretPrivateKey)
//        {
//            int ts = (int)(expiry - new DateTime(1970, 1, 1)).TotalSeconds;

//            // Create Security key  using private key above:
//            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(apiSecretPrivateKey));

//            // length should be >256b
//            var securityKeyCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

//            //Finally create a Token
//            var header = new JwtHeader(securityKeyCredentials);

//            //Zoom Required Payload
//            var payload = new JwtPayload
//            {
//                { "iss", apiKey},
//                { "exp", ts },
//            };

//            JwtSecurityToken secToken = new JwtSecurityToken(header, payload);
//            return secToken;
//        }

//        private static string GenerateMeetingSignatureToken(string apiKey, string apiSecret,string meetingNumber, string ts, string role)
//        {
//            string message = String.Format("{0}{1}{2}{3}", apiKey, meetingNumber, ts, role);

//            var encoding = new System.Text.ASCIIEncoding();
//            byte[] keyByte = encoding.GetBytes(apiSecret);
//            byte[] messageBytesTest = encoding.GetBytes(message);
//            string msgHashPreHmac = System.Convert.ToBase64String(messageBytesTest);
//            byte[] messageBytes = encoding.GetBytes(msgHashPreHmac);
//            using (var hmacsha256 = new HMACSHA256(keyByte))
//            {
//                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
//                string msgHash = System.Convert.ToBase64String(hashmessage);
//                string token = String.Format("{0}.{1}.{2}.{3}.{4}", apiKey, meetingNumber, ts, role, msgHash);
//                var tokenBytes = System.Text.Encoding.UTF8.GetBytes(token);
//                return System.Convert.ToBase64String(tokenBytes).TrimEnd(padding);
//            }
//        }

//        private static bool ValidateToken(string authToken, string appKey, string privateKey)
//        {
//            var tokenHandler = new JwtSecurityTokenHandler();
//            var validationParameters = GetValidationParameters(appKey, privateKey);

//            SecurityToken validatedToken;
//            //   //@CaseyCookston It is a bit odd, but ValidateToken throws an exception if it's not valid. Therefore, if it gets to that line, it's valid. Perhaps return void and rename it AssertValidToken(...)? – 

//            try
//            {
//                IPrincipal principal = tokenHandler.ValidateToken(authToken, validationParameters, out validatedToken);
//                return true;
//            }
//            catch
//            {
//                return false;
//            }

//        }


//        private static TokenValidationParameters GetValidationParameters(string appKey, string privateKey)
//        {

//            return new TokenValidationParameters()
//            {
//                ValidateLifetime = true, // Because there is no expiration in the generated token
//                ValidateAudience = false, // Because there is no audiance in the generated token
//                ValidateIssuer = false,   // Because there is no issuer in the generated token
//                ValidIssuer = appKey,

//                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(privateKey)) // The same key as the one that generate the token
//            };
//        }


//    }



//}