using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSystemDefineDataSourceMapDto
    {
        [DataMember]
        public int PlmDataSourceFrom { get; set; }

        [DataMember]
        public int DataSourceRegisterId { get; set; }

        [DataMember]
        public string DatabaseName { get; set; }

        [DataMember]
        public bool IsRegisterResolved { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSystemDefineEntityPreviewItemDto
    {
        [DataMember]
        public int PlmEntityId { get; set; }

        [DataMember]
        public string PlmEntityCode { get; set; }

        [DataMember]
        public string TargetEntityCode { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string TableName { get; set; }

        [DataMember]
        public string SchemaOwner { get; set; }

        [DataMember]
        public int? PlmDataSourceFrom { get; set; }

        [DataMember]
        public int? AppDataSourceFrom { get; set; }

        [DataMember]
        public string TargetDatabaseName { get; set; }

        [DataMember]
        public string IdentityField { get; set; }

        [DataMember]
        public string DisplayFiled1 { get; set; }

        [DataMember]
        public string DisplayFiled2 { get; set; }

        [DataMember]
        public string DisplayFiled3 { get; set; }

        /// <summary>Ready | Skipped | Blocked</summary>
        [DataMember]
        public string ImportStatus { get; set; }

        /// <summary>Insert | Update (only when ImportStatus = Ready)</summary>
        [DataMember]
        public string ImportAction { get; set; }

        [DataMember]
        public string SkipReason { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSystemDefineEntityBlockerDto
    {
        [DataMember]
        public int PlmEntityId { get; set; }

        [DataMember]
        public string TargetEntityCode { get; set; }

        [DataMember]
        public string TableName { get; set; }

        [DataMember]
        public string TargetDatabaseName { get; set; }

        [DataMember]
        public string Issue { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSystemDefineEntityPreviewDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public int ReadyCount { get; set; }

        [DataMember]
        public int SkippedCount { get; set; }

        [DataMember]
        public int BlockerCount { get; set; }

        [DataMember]
        public List<PlmSystemDefineDataSourceMapDto> DataSourceMaps { get; set; } =
            new List<PlmSystemDefineDataSourceMapDto>();

        [DataMember]
        public List<PlmSystemDefineEntityPreviewItemDto> Entities { get; set; } =
            new List<PlmSystemDefineEntityPreviewItemDto>();

        [DataMember]
        public List<PlmSystemDefineEntityBlockerDto> Blockers { get; set; } =
            new List<PlmSystemDefineEntityBlockerDto>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSystemDefineEntityImportResultDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public int InsertedCount { get; set; }

        [DataMember]
        public int UpdatedCount { get; set; }

        [DataMember]
        public int SkippedCount { get; set; }

        [DataMember]
        public List<PlmSystemDefineEntityPreviewItemDto> SkippedEntities { get; set; } =
            new List<PlmSystemDefineEntityPreviewItemDto>();

        [DataMember]
        public List<PlmSystemDefineEntityBlockerDto> Blockers { get; set; } =
            new List<PlmSystemDefineEntityBlockerDto>();
    }
}
