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
    public class AppFormWithStructureDto
    {
        [DataMember]
        public AppTransactionStructureDto AppTransactionStructureDto
        {
            get;set;
        }

        [DataMember]
       public AppFormExDto AppFormExDto
        {
            get; set;
        }

        [DataMember]
        public AppMasterDetailDto AppMasterDetailDto
        {
            get; set;

        }

        
    }

}
