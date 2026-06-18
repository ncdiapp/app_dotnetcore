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

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmTemplateMappingGridRowDto
    {
        [DataMember]
        public int PlmTemplateId { get; set; }

        [DataMember]
        public string PlmTemplateName { get; set; }

        [DataMember]
        public int PlmTabId { get; set; }

        [DataMember]
        public string PlmTabName { get; set; }

        [DataMember]
        public string TabType { get; set; }

        [DataMember]
        public string ImportStatus { get; set; }

        [DataMember]
        public string ImportAction { get; set; }

        [DataMember]
        public string TransactionGroupName { get; set; }

        [DataMember]
        public string TransactionName { get; set; }

        [DataMember]
        public string IntegrationId { get; set; }

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

        [DataMember]
        public bool ShowTabWarning { get; set; }

        [DataMember]
        public string SimilarTabGroupId { get; set; }

        [DataMember]
        public double? SimilarTabJaccard { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmTemplateBlockAnalysisDto
    {
        [DataMember]
        public int BlockId { get; set; }

        [DataMember]
        public string BlockName { get; set; }

        [DataMember]
        public int ReferencedTabCount { get; set; }

        [DataMember]
        public List<string> ReferencedTabLabels { get; set; } = new List<string>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmTemplateSimilarTabGroupDto
    {
        [DataMember]
        public string GroupId { get; set; }

        [DataMember]
        public int PlmTemplateId { get; set; }

        [DataMember]
        public string SuggestedSharedTableName { get; set; }

        [DataMember]
        public double JaccardScore { get; set; }

        [DataMember]
        public List<int> TabIds { get; set; } = new List<int>();

        [DataMember]
        public List<string> TabLabels { get; set; } = new List<string>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmTemplateMappingGridDto
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
        public List<PlmTemplateMappingGridRowDto> Rows { get; set; } = new List<PlmTemplateMappingGridRowDto>();

        [DataMember]
        public List<PlmTemplateBlockAnalysisDto> Blocks { get; set; } = new List<PlmTemplateBlockAnalysisDto>();

        [DataMember]
        public List<PlmTemplateSimilarTabGroupDto> SimilarTabGroups { get; set; } = new List<PlmTemplateSimilarTabGroupDto>();

        [DataMember]
        public List<PlmTemplateBlockerDto> Blockers { get; set; } = new List<PlmTemplateBlockerDto>();

        [DataMember]
        public List<PlmTemplateWarningDto> Warnings { get; set; } = new List<PlmTemplateWarningDto>();

        [DataMember]
        public PlmTemplateImportSettingDto SavedSetting { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmTemplateImportSettingRowDto
    {
        [DataMember]
        public int PlmTemplateId { get; set; }

        [DataMember]
        public int PlmTabId { get; set; }

        [DataMember]
        public string TransactionGroupName { get; set; }

        [DataMember]
        public string TransactionName { get; set; }

        [DataMember]
        public string IntegrationId { get; set; }

        [DataMember]
        public string SiblingTableName { get; set; }

        [DataMember]
        public string ImportStatus { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmTemplateTabSharedTableGroupDto
    {
        [DataMember]
        public string GroupId { get; set; }

        [DataMember]
        public string SharedTableName { get; set; }

        [DataMember]
        public List<int> TabIds { get; set; } = new List<int>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmTemplateBlockStorageOverrideDto
    {
        [DataMember]
        public int BlockId { get; set; }

        /// <summary>Root | SharedSibling</summary>
        [DataMember]
        public string StorageTarget { get; set; }

        [DataMember]
        public string SharedTableName { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmTemplateImportSettingDto
    {
        [DataMember]
        public List<PlmTemplateImportSettingRowDto> Rows { get; set; } = new List<PlmTemplateImportSettingRowDto>();

        [DataMember]
        public List<PlmTemplateTabSharedTableGroupDto> TabSharedTableGroups { get; set; } = new List<PlmTemplateTabSharedTableGroupDto>();

        [DataMember]
        public List<PlmTemplateBlockStorageOverrideDto> BlockStorageOverrides { get; set; } = new List<PlmTemplateBlockStorageOverrideDto>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmTemplateMappingValidationDto
    {
        [DataMember]
        public bool IsValid { get; set; }

        [DataMember]
        public List<string> Errors { get; set; } = new List<string>();

        [DataMember]
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
