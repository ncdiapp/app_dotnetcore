using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class TransactionCommandActionResultDto
    {
        [DataMember(EmitDefaultValue = false)]
        public int? CommandId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? ActionTypeId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? TransactionId { get; set; }
        

        [DataMember(EmitDefaultValue = false)]
        public object TransactionRId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public AppFormLinkTargetDto NeedToExecuteLinkTargetDto { get; set; }



        [DataMember(EmitDefaultValue = false)]
        public AppMasterDetailDto FormData { get; set; }
        


        [DataMember(EmitDefaultValue = false)]
        public List<TransactionCommandActionResultDto> ChildCommandResultDtoList { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool? IsNeedToOpenTransactionForm { get; set; }


        //[DataMember(EmitDefaultValue = false)]
        //public bool? IsNeedToPrintForm { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public AppProjectWorkFlowActionExDto CommandActionExDto { get; set; }

        //[DataMember(EmitDefaultValue = false)]
        //public AppMasterDetailDto NewFormDefaultData { get; set; }        


        [DataMember(EmitDefaultValue = false)]
        public object FormTitleDisplay { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public object ActionResultScalarValue { get; set; }
    }
}
