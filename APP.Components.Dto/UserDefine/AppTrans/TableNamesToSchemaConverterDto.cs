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
    public partial class TableNamesToSchemaConverterDto
    {
        // firstkey:schemaOwner, value tableName
        public List<KeyValuePair<string, string>> SchemaOwnerAndTableNamePairList { get; set; }


        public int? DataSourceRegisterId { get; set; }


    }
}
