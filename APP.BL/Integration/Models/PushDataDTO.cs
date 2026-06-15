using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeBL.Models
{
    public class PushDataDTO
    {
        public string id { get; set; }
        public string InternalKeyId { get; set; }
        public string RequestJson { get; set; }
        public string ResponseJson { get; set; }
        public List<RowInfoDTO> RowInfos { get; set; }
        public bool IsNew => RowInfos.Any(r => r.IsNew);
    }
    public class RowInfoDTO
    {
        public string TableName { get; set; }
        public KeyValuePair<string, string> InternalKeyNameId { get; set; }
        public string InternalForeginKeyId { get; set; }
        public KeyValuePair<string, string> KeyNameId { get; set; }
        public KeyValuePair<string, string> ForeginKeyNameId { get; set; }
        public List<string> Unique { get; set; }
        public bool IsNew { get; set; }
    }
}
