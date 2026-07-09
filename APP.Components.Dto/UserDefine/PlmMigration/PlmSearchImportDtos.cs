using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchImportBlueprintDto
    {
        [DataMember]
        public int SchemaVersion { get; set; } = 1;

        [DataMember]
        public string GeneratedAt { get; set; }

        [DataMember]
        public PlmSearchImportSourceDto Source { get; set; }

        [DataMember]
        public PlmSearchImportSearchDto Search { get; set; }

        [DataMember]
        public PlmSearchImportDataSetDto DataSet { get; set; }

        [DataMember]
        public PlmSearchImportJoinPlanDto JoinPlan { get; set; }

        [DataMember]
        public PlmSearchImportTransactionGroupDto TransactionGroup { get; set; }

        [DataMember]
        public List<PlmSearchImportCriteriaFieldDto> CriteriaFields { get; set; } =
            new List<PlmSearchImportCriteriaFieldDto>();

        [DataMember]
        public PlmSearchImportSearchViewDto SearchView { get; set; }

        [DataMember]
        public List<PlmSearchImportLinkTargetDto> LinkTargets { get; set; } =
            new List<PlmSearchImportLinkTargetDto>();

        [DataMember]
        public PlmSearchImportMenuDto Menu { get; set; }

        [DataMember]
        public PlmSearchImportCoverageDto Coverage { get; set; }

        [DataMember]
        public List<PlmSearchImportUnmappedFieldDto> UnmappedPlmFields { get; set; } =
            new List<PlmSearchImportUnmappedFieldDto>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchImportSourceDto
    {
        [DataMember]
        public int? PlmSearchTemplateId { get; set; }

        [DataMember]
        public string PlmSearchName { get; set; }

        [DataMember]
        public int? PlmReferenceViewId { get; set; }

        [DataMember]
        public string PlmReferenceViewName { get; set; }

        [DataMember]
        public int? PlmBlQueryId { get; set; }

        [DataMember]
        public string TablePrefix { get; set; }

        [DataMember]
        public string SelectedJoinPlanId { get; set; }

        [DataMember]
        public string GridColumnStrategy { get; set; }

        [DataMember]
        public string PrimaryTableName { get; set; }

        [DataMember]
        public PlmSearchImportReferenceScopeDto ReferenceScope { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchImportReferenceScopeDto
    {
        [DataMember]
        public string Mode { get; set; }

        [DataMember]
        public string Notes { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchImportSearchDto
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string IntegrationId { get; set; }

        /// <summary>Management | DataModelTemplate</summary>
        [DataMember]
        public string UsageType { get; set; } = "Management";

        [DataMember]
        public bool AutoExecute { get; set; } = true;

        [DataMember]
        public int? SaasApplicationId { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchImportDataSetDto
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string QueryMode { get; set; }

        [DataMember]
        public string PrimaryTableName { get; set; }

        [DataMember]
        public string RootTableName { get; set; }

        [DataMember]
        public string QueryText { get; set; }

        [DataMember]
        public int? TenantDataSourceRegisterId { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchImportJoinPlanDto
    {
        [DataMember]
        public string PlanId { get; set; }

        [DataMember]
        public int? Score { get; set; }

        [DataMember]
        public string Label { get; set; }

        [DataMember]
        public string SemanticSummary { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchImportTransactionGroupDto
    {
        [DataMember]
        public int? TransactionGroupId { get; set; }

        [DataMember]
        public string GroupName { get; set; }

        [DataMember]
        public string PrimaryTransactionIntegrationId { get; set; }

        [DataMember]
        public string Notes { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchImportCriteriaFieldDto
    {
        [DataMember]
        public string IntegrationKey { get; set; }

        [DataMember]
        public string DisplayText { get; set; }

        [DataMember]
        public string SysTableFiledPath { get; set; }

        [DataMember]
        public int? ControlType { get; set; }

        [DataMember]
        public string EntityIntegrationId { get; set; }

        [DataMember]
        public int? OperationId { get; set; }

        [DataMember]
        public int? PositionRow { get; set; }

        [DataMember]
        public int? PositionColumn { get; set; }

        [DataMember]
        public bool IsVisible { get; set; } = true;

        [DataMember]
        public int? Sort { get; set; }

        [DataMember]
        public string DefaultValue { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchImportSearchViewDto
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string IntegrationId { get; set; }

        [DataMember]
        public string ViewType { get; set; } = "GridView";

        [DataMember]
        public int GridOutputMode { get; set; } = 1;

        [DataMember]
        public List<PlmSearchImportSearchViewFieldDto> Fields { get; set; } =
            new List<PlmSearchImportSearchViewFieldDto>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchImportSearchViewFieldDto
    {
        [DataMember]
        public string DisplayText { get; set; }

        [DataMember]
        public string SysTableFiledPath { get; set; }

        [DataMember]
        public int? ControlType { get; set; }

        [DataMember]
        public string EntityIntegrationId { get; set; }

        [DataMember]
        public bool IsTransRootId { get; set; }

        [DataMember]
        public bool IsVisible { get; set; } = true;

        [DataMember]
        public int? Sort { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchImportLinkTargetDto
    {
        [DataMember]
        public string Name { get; set; }

        /// <summary>Create | Edit | Delete</summary>
        [DataMember]
        public string ActionType { get; set; }

        [DataMember]
        public string TransactionIntegrationId { get; set; }

        [DataMember]
        public int? LinkTargetTransactionGroupId { get; set; }

        [DataMember]
        public string LinkTargetTransactionGroupName { get; set; }

        [DataMember]
        public string SourceColumn { get; set; }

        [DataMember]
        public int? Sort { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchImportMenuDto
    {
        [DataMember]
        public bool RegisterInMainMenu { get; set; }

        [DataMember]
        public string MenuTitle { get; set; }

        [DataMember]
        public string MenuGroup { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchImportCoverageDto
    {
        [DataMember]
        public PlmSearchImportCoverageSectionDto Criteria { get; set; }

        [DataMember]
        public PlmSearchImportCoverageSectionDto View { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchImportCoverageSectionDto
    {
        [DataMember]
        public int Total { get; set; }

        [DataMember]
        public int Mapped { get; set; }

        [DataMember]
        public int Ignored { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchImportUnmappedFieldDto
    {
        [DataMember]
        public string Role { get; set; }

        [DataMember]
        public string DisplayLabel { get; set; }

        [DataMember]
        public int? PlmSubItemId { get; set; }

        [DataMember]
        public string Reason { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchImportLoadRequestDto
    {
        [DataMember]
        public string BlueprintJson { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchImportValidationDto
    {
        [DataMember]
        public bool IsValid { get; set; }

        [DataMember]
        public List<string> Errors { get; set; } = new List<string>();

        [DataMember]
        public List<string> Warnings { get; set; } = new List<string>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchImportPreviewDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public List<PlmSearchImportPreviewItemDto> Items { get; set; } =
            new List<PlmSearchImportPreviewItemDto>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchImportPreviewItemDto
    {
        [DataMember]
        public string ObjectType { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string IntegrationId { get; set; }

        /// <summary>Insert | Update</summary>
        [DataMember]
        public string Action { get; set; }

        [DataMember]
        public int? ExistingId { get; set; }

        [DataMember]
        public string Detail { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchImportExecuteRequestDto
    {
        [DataMember]
        public PlmSearchImportBlueprintDto Blueprint { get; set; }

        [DataMember]
        public int? SaasApplicationId { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchImportExecuteResultDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public int? SearchId { get; set; }

        [DataMember]
        public int? SearchViewId { get; set; }

        [DataMember]
        public int? DataSetId { get; set; }

        [DataMember]
        public List<string> Messages { get; set; } = new List<string>();
    }
}
