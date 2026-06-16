using System;
using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmImportLogDto
    {
        [DataMember]
        public int LogId { get; set; }

        [DataMember]
        public int SessionId { get; set; }

        [DataMember]
        public int? JobId { get; set; }

        [DataMember]
        public string StepCode { get; set; }

        [DataMember]
        public string Action { get; set; }

        [DataMember]
        public string Status { get; set; }

        [DataMember]
        public string TargetKey { get; set; }

        [DataMember]
        public string PlmIntegrationKey { get; set; }

        [DataMember]
        public int? RowsAffected { get; set; }

        [DataMember]
        public int? DurationMs { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public DateTime? CreatedAt { get; set; }
    }
}
