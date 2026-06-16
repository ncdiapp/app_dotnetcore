using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmUserDefineEntityPreviewItemDto
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

        /// <summary>SimpleValueList | SystemDefineTable</summary>
        [DataMember]
        public string AppTargetType { get; set; }

        [DataMember]
        public int ColumnCount { get; set; }

        [DataMember]
        public int PlmRowCount { get; set; }

        [DataMember]
        public int ImportOrder { get; set; }

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
    public class PlmUserDefineEntityBlockerDto
    {
        [DataMember]
        public int PlmEntityId { get; set; }

        [DataMember]
        public string TargetEntityCode { get; set; }

        [DataMember]
        public string TableName { get; set; }

        [DataMember]
        public string Issue { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmUserDefineEntityPreviewDto
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
        public List<PlmUserDefineEntityPreviewItemDto> Entities { get; set; } =
            new List<PlmUserDefineEntityPreviewItemDto>();

        [DataMember]
        public List<PlmUserDefineEntityBlockerDto> Blockers { get; set; } =
            new List<PlmUserDefineEntityBlockerDto>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmUserDefineEntityImportResultDto
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
        public int RowsImported { get; set; }

        [DataMember]
        public List<PlmUserDefineEntityPreviewItemDto> SkippedEntities { get; set; } =
            new List<PlmUserDefineEntityPreviewItemDto>();

        [DataMember]
        public List<PlmUserDefineEntityBlockerDto> Blockers { get; set; } =
            new List<PlmUserDefineEntityBlockerDto>();
    }
}
