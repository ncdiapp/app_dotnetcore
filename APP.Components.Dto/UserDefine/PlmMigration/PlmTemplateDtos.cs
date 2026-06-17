using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmTemplatePreviewItemDto
    {
        [DataMember]
        public int PlmTemplateId { get; set; }

        [DataMember]
        public string PlmTemplateName { get; set; }

        [DataMember]
        public int PlmTabId { get; set; }

        [DataMember]
        public string PlmTabName { get; set; }

        /// <summary>MainItem | TemplateHeader | Skipped</summary>
        [DataMember]
        public string TabType { get; set; }

        /// <summary>Ready | Skipped | Blocked</summary>
        [DataMember]
        public string ImportStatus { get; set; }

        /// <summary>Insert | Update (when Ready)</summary>
        [DataMember]
        public string ImportAction { get; set; }

        [DataMember]
        public string SiblingTableName { get; set; }

        [DataMember]
        public string ChildTableNames { get; set; }

        [DataMember]
        public int SiblingFieldCount { get; set; }

        [DataMember]
        public int GridFieldCount { get; set; }

        [DataMember]
        public int WarningCount { get; set; }

        [DataMember]
        public string SkipReason { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmTemplateWarningDto
    {
        [DataMember]
        public int? PlmTemplateId { get; set; }

        [DataMember]
        public int? PlmTabId { get; set; }

        [DataMember]
        public int? PlmSubItemId { get; set; }

        [DataMember]
        public int? PlmGridColumnId { get; set; }

        [DataMember]
        public string Issue { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmTemplateBlockerDto
    {
        [DataMember]
        public int? PlmTemplateId { get; set; }

        [DataMember]
        public int? PlmTabId { get; set; }

        [DataMember]
        public string PlmTabName { get; set; }

        [DataMember]
        public string Issue { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmTemplatePreviewDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public int TemplateCount { get; set; }

        [DataMember]
        public int ReadyCount { get; set; }

        [DataMember]
        public int SkippedCount { get; set; }

        [DataMember]
        public int BlockerCount { get; set; }

        [DataMember]
        public int WarningCount { get; set; }

        [DataMember]
        public List<PlmTemplatePreviewItemDto> Tabs { get; set; } = new List<PlmTemplatePreviewItemDto>();

        [DataMember]
        public List<PlmTemplateBlockerDto> Blockers { get; set; } = new List<PlmTemplateBlockerDto>();

        [DataMember]
        public List<PlmTemplateWarningDto> Warnings { get; set; } = new List<PlmTemplateWarningDto>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmTemplateImportResultDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public int TemplatesProcessed { get; set; }

        [DataMember]
        public int TabsInserted { get; set; }

        [DataMember]
        public int TabsUpdated { get; set; }

        [DataMember]
        public int TabsSkipped { get; set; }

        [DataMember]
        public List<PlmTemplatePreviewItemDto> SkippedTabs { get; set; } = new List<PlmTemplatePreviewItemDto>();

        [DataMember]
        public List<PlmTemplateBlockerDto> Blockers { get; set; } = new List<PlmTemplateBlockerDto>();

        [DataMember]
        public List<PlmTemplateWarningDto> Warnings { get; set; } = new List<PlmTemplateWarningDto>();
    }
}
