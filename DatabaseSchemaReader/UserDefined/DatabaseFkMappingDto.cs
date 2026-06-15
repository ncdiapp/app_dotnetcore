using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;
//using Newtonsoft.Json;

namespace DatabaseSchemaMrg.DataSchema
{
    public partial class DatabaseColumn
    {
        public bool IsLogicKey { get; set; }   //Replace IsUniqueKey

        public string MapToSourceColumnName { get; set; }  // Replace ComputedDefinition

        public string LinkToParentTablePkColumnName { get; set; }  // Replace Description

        public string OverrideDefaultValue { get; set; }

        public int? SystemToken { get; set; }



    }
}
