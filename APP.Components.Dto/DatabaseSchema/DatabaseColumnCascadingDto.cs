using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.EntityDto;
using APP.Components.Dto;

namespace APP.Components.Dto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
   public class DatabaseColumnCascadingDto
    {
        [DataMember]
        public string TableName { get; set; }

        [DataMember]
        public string ColumnName { get; set; }
             
        [DataMember]
        public string CascadingParentTableName { get; set; }


        [DataMember]
        public string CascadingParentColumnName { get; set; }


        [DataMember]
        public string CascadingRelationTable { get; set; }


        [DataMember]
        public string CascadingRelationTableParentKeyField { get; set; }


        [DataMember]
        public string CascadingRelationTableChildKeyField { get; set; }
    }
}
