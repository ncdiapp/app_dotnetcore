using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
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
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmTableExportPlanDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

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
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmTableExportResultDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public List<PlmTableExportResultItemDto> Tables { get; set; } = new List<PlmTableExportResultItemDto>();
    }
}
