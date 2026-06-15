using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;
using System.Data;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class TransactionDataDto
    {


        //key:DBFieldName
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, object> DictOneToOneFields
        {
            get;
            set;
        }

        //Key:TaleName 
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<String, List<AppChildDataDto>> DictOneToManyFields
        {
            get;
            set;
        }


     
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class TransactionChildDataDto
    {

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, object> DictOneToOneFields
        {
            get;
            set;
        }

     

        // Key:TaleName or Tanscation unit ID
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<String, List<Dictionary<string, object>>> DictOneToManyFields
        {
            get;
            set;
        }
      

    }
}
