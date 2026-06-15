using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;
using System.Data;
using DatabaseSchemaMrg.DataSchema;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class SchemaMetaDataDto //: DatabaseTable
    {

        //[DataMember(EmitDefaultValue = false)]
        //public DatabaseTable DatabaseTable
        //{
        //    get;
        //    set;
        //}

        [DataMember(EmitDefaultValue = false)]
        public int? DataSourceRegisterId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string OriginalTableName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string SchemaOwner
        {
            get;
            set;
        }


 


        [DataMember(EmitDefaultValue = false)]
        public string NewTableName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<DatabaseColumn> ListNewDatabaseColumn
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, DatabaseColumn> DictOrgnameNewDataBasecolumn
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<KeyValuePair<DatabaseColumn, DatabaseColumn>> ListPairAlterDatabaseColumn
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public List<DatabaseColumn> ListDropDatabaseColumn
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public DatabaseConstraint DatabaseConstraint
        {
            get;
            set;
        }

      

    }
}
