using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace APP.Components.EntityDto
{
    public partial class APIConfigParameterDTO
    {      

        public string BaseUrl { get; set; }
        public string Url { get; set; }
        public EnumHttpMethod Method { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public Dictionary<string, string> QueryParams { get; set; }
        public Dictionary<string, string> PathParams { get; set; }
        public string PostProcessMethodName { get; set; }
        public AuthenticationType AuthenticationType { get; set; }
        public Dictionary<string, string> AuthenticationParameters { get; set;}        
        public int? ExcelDataImportDataSetId { get; set; }

        public Dictionary<string, string> DictOverrideEnvionmentVariable { get; set; }

        public Dictionary<string, string> ResponseObjectMapToEnvionmentVariable { get; set; }


        public string RequestInfo { get; set; }

        public List<string> ResponseHeaderNeedToSetCookieNames { get; set; }

        /// <summary>True when this App API Provider operation invokes a stored procedure.</summary>
        public bool IsStoredProcedureApi { get; set; }

        public string SpName { get; set; }

        public string SpSchema { get; set; }

        public string SpEngine { get; set; }

        /// <summary>SP parameters with configured default values for invoke / sample capture.</summary>
        public List<StoredProcedureApiParameterDTO> SpParameters { get; set; }

        public APIConfigParameterDTO()
        {
            this.BaseUrl = string.Empty;
            this.Url = string.Empty;
            this.Headers = new Dictionary<string, string>();
            this.QueryParams = new Dictionary<string, string>();
            this.PathParams = new Dictionary<string, string>();
            this.SpParameters = new List<StoredProcedureApiParameterDTO>();
        }
    }

    public partial class StoredProcedureApiParameterDTO
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Direction { get; set; }
        public int? MaxLength { get; set; }
        public int Ordinal { get; set; }
        public bool HasDefault { get; set; }
        /// <summary>Configured default as string (empty / 0 / null token). Null means omit and let SP default apply when HasDefault.</summary>
        public string DefaultValue { get; set; }
    }
}
