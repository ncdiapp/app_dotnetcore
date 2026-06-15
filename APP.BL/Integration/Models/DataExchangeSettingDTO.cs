using APP.Components.EntityDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeBL.Models
{
    public class DataExchangeSettingDTO
    {
        public int ActionId { get; set; }
        public string ActionCode { get; set; }
        public string ActionDescription { get; set; }
        public string JsonQuery { get; set; }
        public string WhereClauseFormat { get; set; }
        public bool IsSimpleQuery { get; set; }
        public string JsonSampleData { get; set; }
        public string JsonSchema { get; set; }
        public string SchemaDataSetMapping { get; set; }
        public string SchemaFromDataSetMapping { get; set; }
        public EnumHttpMethod HttpMethd { get; set; }
        public int DataSourceID { get; set; }
        public string ConnectionString { get; set; }
        public string PostProcessScript { get; set; } // after post a json , update ids with those script
        public APIConfigParameterDTO APIConfigParameters { get; set; } // API call parameters
        public string TablePrefix { get; set; } // database table prefix: Shopify, Quickbooks..
    }
}
