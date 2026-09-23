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

        /// <summary>AppGenericAgentSession.SessionKey that owns this import job. One InProgress job per Chat.</summary>
        [DataMember]
        public string ChatSessionKey { get; set; }

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

        /// <summary>
        /// Obsolete for Agent/API input. Never ask the user for a connection string.
        /// Engine may hydrate this in-memory from <see cref="PlmDataSourceRegisterId"/> for internal jobs only;
        /// Get/Save session responses must leave it null.
        /// </summary>
        [DataMember]
        public string PlmConnectionString { get; set; }

        /// <summary>True when session has a bound PLM DataSourceRegisterId (or legacy encrypted connection).</summary>
        [DataMember]
        public bool HasPlmConnection { get; set; }

        /// <summary>Required for Connect: tenant AppDataSourceRegister id for the PLM database.</summary>
        [DataMember]
        public int? PlmDataSourceRegisterId { get; set; }

        /// <summary>Optional: tenant register id for PLM DW (data warehouse) database.</summary>
        [DataMember]
        public int? PlmDwDataSourceRegisterId { get; set; }

        /// <summary>Optional: tenant register id for ERP (or other) database used by import.</summary>
        [DataMember]
        public int? ErpDataSourceRegisterId { get; set; }

        /// <summary>
        /// Optional: tenant register id for PLM External DB (ExDb) — extra data feeding PLM datasource entities.
        /// Same usage pattern as ERP; never pass connection strings.
        /// </summary>
        [DataMember]
        public int? PlmExDbDataSourceRegisterId { get; set; }

        [DataMember]
        public string StepStateJson { get; set; }

        /// <summary>Optional JSON metadata (no connection strings). Prefer register id fields above.</summary>
        [DataMember]
        public string DataSourceDiscoveryJson { get; set; }
    }
}
