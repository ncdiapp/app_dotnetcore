using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework.Collections;
using APP.Components.Dto;
using DatabaseSchemaMrg.DataSchema;

namespace APP.Components.EntityDto
{
    public partial class TableToUnitConverterDto
    {      
        public string TableName { get; set; }

		public string SchemaOwner { get; set; }

		public int? TransactionId { get; set; }
        
        public AppTransactionUnitExDto ParentUnit { get; set; }
   
        
        public List<DatabaseColumn> NeedToAddDbColumns { get; set; }

        public int? DataSourceRegisterId { get; set; }

		
        public Dictionary<string, int> DictColumnNameAndEntityId { get; set; }


        public Dictionary<string, string> DictColumnNameFkTableName { get; set; }
    }
}
