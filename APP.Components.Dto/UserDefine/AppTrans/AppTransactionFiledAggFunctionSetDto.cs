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
    public class AppTransactionFieldAggFunctionSetDto
    {

     
        //Key:TaleName or Tanscation unit ID
        [DataMember(EmitDefaultValue = false)]
        public List<AppTransactionFieldAggFunctionDto> ListAppTransactionFieldAggFunction
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int TransactionFieldId
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int? TransactionId
        {
            get;
            set;
        }



    }
}
