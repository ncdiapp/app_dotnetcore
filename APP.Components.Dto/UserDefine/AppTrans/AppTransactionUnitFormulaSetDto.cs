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
    public class AppTransactionUnitFormulaSetDto
    {

    
        [DataMember(EmitDefaultValue = false)]
        public List<AppTransactionUnitFormulaDto> ListAppTransactionUnitFormula
        {
            get;
            set;
        }

      
        public List<AppTransactionUnitFormulaExDto> OrgFormulaExDtoList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? SearchViewId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int TransactionId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int TransactionUnitId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string TransactionUnitName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsModified
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public AppTransactionExDto NeedToUpdateTransactionExDto
        {
            get;
            set;
        }


    }
}
