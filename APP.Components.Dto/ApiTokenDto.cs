using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace APP.Components.Dto
{
   public class ApiTokenDto
    {
        public static readonly string ApiTokenKey = "ApiTokenKey";
      

        public string Token
        {
            get;set;
        }

        public double TokenExpireInSeconds
        {
            get;set;
        }

        public DateTime LastTimeGetToekn
        {
            get; set;
        }


        public static string GetValidtokeFromDict(Dictionary<string, ApiTokenDto> dictApiToken)
        {
            if (dictApiToken.ContainsKey(ApiTokenDto.ApiTokenKey))
            {
                var tokenDto = dictApiToken[ApiTokenDto.ApiTokenKey];
                string token = tokenDto.Token;
                double expireInt = tokenDto.TokenExpireInSeconds - 100;
                double diffInSeconds = (System.DateTime.UtcNow - tokenDto.LastTimeGetToekn).TotalSeconds;
                if (diffInSeconds < expireInt)
                {
                    return token;
                }


            }

            return string.Empty;
        }

        public static ApiTokenDto ConvertTokenExpireToDto(Tuple<string, double> token_expire)
        {
            ApiTokenDto aApiTokenDto = new ApiTokenDto();
            aApiTokenDto.Token = token_expire.Item1;
            aApiTokenDto.TokenExpireInSeconds = token_expire.Item2;
            aApiTokenDto.LastTimeGetToekn = System.DateTime.UtcNow;
            return aApiTokenDto;
        }
    }


}
