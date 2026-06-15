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
   public class DatabaseColumnDto
    {

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string TableName { get; set; }

        [DataMember]
        public string ComputedDefinition { get; set; }

        [DataMember]
        public String DataType { get; set; }

         [DataMember]
        public int? DateTimePrecision { get; set; }

         [DataMember]
        public string DbDataType { get; set; }

         [DataMember]
        public string DefaultValue { get; set; }

         [DataMember]
        public string Description { get; set; }

         [DataMember]
        public long IdentityIncrement { get; set; }

         [DataMember]
        public long IdentitySeed { get; set; }

         [DataMember]
        public bool IsAutoNumber { get; set; }

         [DataMember]
        public bool IsComputed { get; set; }

         [DataMember]
        public bool IsForeignKey { get; set; }

         [DataMember]
        public bool IsIndexed { get; set; }
         [DataMember]
        public bool IsPrimaryKey { get; set; }
         [DataMember]
        public bool IsUniqueKey { get; set; }
         [DataMember]
        public int? Length { get; set; }
         [DataMember]
        public string NetName { get; set; }
         [DataMember]
        public bool Nullable { get; set; }
         [DataMember]
        public int Ordinal { get; set; }
         [DataMember]
        public int? Precision { get; set; }
         [DataMember]
        public int? Scale { get; set; }
      
    }
}
