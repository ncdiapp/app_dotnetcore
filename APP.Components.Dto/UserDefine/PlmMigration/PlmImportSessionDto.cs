using System;
using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmImportSessionDto
    {
        [DataMember]
        public int? SessionId { get; set; }

        [DataMember]
        public string SessionGuid { get; set; }

        [DataMember]
        public int? CompanyId { get; set; }

        [DataMember]
        public int? SaasApplicationId { get; set; }

        [DataMember]
        public string SaasApplicationName { get; set; }

        [DataMember]
        public int? CreatedByUserId { get; set; }

        [DataMember]
        public DateTime? CreatedAt { get; set; }

        [DataMember]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>InProgress | Completed</summary>
        [DataMember]
        public string SessionStatus { get; set; }

        [DataMember]
        public string CurrentStepCode { get; set; }

        /// <summary>Decrypted PLM connection string (returned to wizard admin on read/save).</summary>
        [DataMember]
        public string PlmConnectionString { get; set; }

        /// <summary>True when session has a stored PLM connection (resume).</summary>
        [DataMember]
        public bool HasPlmConnection { get; set; }

        [DataMember]
        public string StepStateJson { get; set; }

        [DataMember]
        public string DataSourceDiscoveryJson { get; set; }
    }
}
