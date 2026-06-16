using System;
using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmImportJobDto
    {
        [DataMember]
        public int JobId { get; set; }

        [DataMember]
        public int SessionId { get; set; }

        [DataMember]
        public string JobType { get; set; }

        /// <summary>Queued | Running | Completed | Failed | Cancelled</summary>
        [DataMember]
        public string Status { get; set; }

        [DataMember]
        public int ProgressPercent { get; set; }

        [DataMember]
        public string ProgressMessage { get; set; }

        [DataMember]
        public string ResultJson { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public DateTime? CreatedAt { get; set; }

        [DataMember]
        public DateTime? UpdatedAt { get; set; }

        [DataMember]
        public DateTime? StartedAt { get; set; }

        [DataMember]
        public DateTime? CompletedAt { get; set; }
    }
}
