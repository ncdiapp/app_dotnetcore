using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmTableExportEntityRefDto
    {
        [DataMember]
        public int EntityId { get; set; }

        [DataMember]
        public string EntityCode { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmTableExportIssueDto
    {
        [DataMember]
        public int EntityId { get; set; }

        [DataMember]
        public string EntityCode { get; set; }

        [DataMember]
        public string SchemaOwner { get; set; }

        [DataMember]
        public string TableName { get; set; }

        /// <summary>MissingSourceTable | ExportFailed</summary>
        [DataMember]
        public string IssueType { get; set; }

        [DataMember]
        public string Message { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmTableExportPlanItemDto
    {
        [DataMember]
        public string SchemaOwner { get; set; }

        [DataMember]
        public string TableName { get; set; }

        [DataMember]
        public int PlmEntityCount { get; set; }

        [DataMember]
        public bool SourceTableExists { get; set; }

        [DataMember]
        public List<PlmTableExportEntityRefDto> Entities { get; set; } = new List<PlmTableExportEntityRefDto>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmTableExportPlanDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        /// <summary>Tables referenced in pdmEntity but missing from the PLM source database.</summary>
        [DataMember]
        public int MissingSourceTableCount { get; set; }

        [DataMember]
        public List<PlmTableExportIssueDto> Issues { get; set; } = new List<PlmTableExportIssueDto>();

        [DataMember]
        public List<PlmTableExportPlanItemDto> Tables { get; set; } = new List<PlmTableExportPlanItemDto>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmTableExportResultItemDto
    {
        [DataMember]
        public string SchemaOwner { get; set; }

        [DataMember]
        public string TableName { get; set; }

        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public int RowsCopied { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public List<PlmTableExportEntityRefDto> Entities { get; set; } = new List<PlmTableExportEntityRefDto>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmTableExportResultDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public List<PlmTableExportIssueDto> Issues { get; set; } = new List<PlmTableExportIssueDto>();

        [DataMember]
        public List<PlmTableExportResultItemDto> Tables { get; set; } = new List<PlmTableExportResultItemDto>();
    }
}
