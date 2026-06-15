using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace APP.Components.EntityDto
{
    public partial class SearchApiSettingDto
    {
        [DataMember(EmitDefaultValue = false)]
        public string ConsumeOrProvideType { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string CRUDType { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? OperationId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string ActionCode { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string BaseUrl { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string Url { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string HttpMethd { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? DataTransferSettingId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string DataTransferSettingName { get; set; }        


        [DataMember(EmitDefaultValue = false)]
        public int? TargetTransactionId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string TargetTransactionName { get; set; }

    }
}
