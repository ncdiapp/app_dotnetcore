using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeBL.Models
{
    class OAuth2TokenDTO
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public string userName { get; set; }
        [JsonProperty(".issued")]
        public DateTime issued { get; set; }
        [JsonProperty(".expires")]
        public DateTime expires { get; set; }
    }
}
