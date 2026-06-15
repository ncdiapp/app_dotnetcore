using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;
//using Newtonsoft.Json;

namespace DatabaseSchemaMrg.DataSchema
{
    public partial class DatabaseFkMappingDto
    {
        

        public string ChildTableName { get; set; }  

        public string ChildTableFkColumnName { get; set; } 

        public string ParentTableName { get; set; }

        public string ParentTablePkColumnName { get; set; }



    }
}
