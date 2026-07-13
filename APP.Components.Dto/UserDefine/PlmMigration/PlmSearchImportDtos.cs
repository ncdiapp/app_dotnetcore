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
        public bool? IsMassUpdateView { get; set; }

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

        /// <summary>Mass Update: map this view column to a transaction field (by id).</summary>
        [DataMember]
        public int? MassUpdateTransactionFieldId { get; set; }

        /// <summary>Mass Update: resolve TransactionField by DataBaseFieldName on the update unit.</summary>
        [DataMember]
        public string MassUpdateDatabaseFieldName { get; set; }

        /// <summary>Mass Update: when false, do not set MassUpdateTransactionFieldId.</summary>
        [DataMember]
        public bool? IsUpdatable { get; set; }
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

    // -------------------------------------------------------------------------
    // Sibling View (Option A) — enrich existing DataSet + add AppSearchView
    // -------------------------------------------------------------------------

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchSiblingViewBlueprintDto
    {
        [DataMember]
        public int SchemaVersion { get; set; } = 1;

        /// <summary>Must be SiblingViewEnrichDataSet for Option A execute.</summary>
        [DataMember]
        public string Mode { get; set; } = "SiblingViewEnrichDataSet";

        [DataMember]
        public string GeneratedAt { get; set; }

        [DataMember]
        public PlmSearchSiblingViewSourceDto Source { get; set; }

        [DataMember]
        public PlmSearchSiblingViewTargetDto Target { get; set; }

        [DataMember]
        public PlmSearchSiblingViewDataSetPatchDto DataSetPatch { get; set; }

        [DataMember]
        public PlmSearchImportSearchViewDto SearchView { get; set; }

        [DataMember]
        public PlmSearchSiblingViewLinkTargetsDto LinkTargets { get; set; }

        [DataMember]
        public PlmSearchSiblingViewCoverageDto Coverage { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchSiblingViewSourceDto
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
        public string TablePrefix { get; set; }

        [DataMember]
        public string RowGrain { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchSiblingViewTargetDto
    {
        [DataMember]
        public string AppSearchIntegrationId { get; set; }

        [DataMember]
        public int? AppSearchId { get; set; }

        [DataMember]
        public int? AppDataSetId { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchSiblingViewDataSetPatchDto
    {
        /// <summary>Preferred: full QueryText after enrich. If set, used as-is.</summary>
        [DataMember]
        public string ResultingQueryText { get; set; }

        [DataMember]
        public List<PlmSearchSiblingViewAddColumnDto> AddColumns { get; set; } =
            new List<PlmSearchSiblingViewAddColumnDto>();

        [DataMember]
        public List<PlmSearchSiblingViewAddJoinDto> AddLeftJoins { get; set; } =
            new List<PlmSearchSiblingViewAddJoinDto>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchSiblingViewAddColumnDto
    {
        [DataMember]
        public string SysTableFiledPath { get; set; }

        [DataMember]
        public string AppTableName { get; set; }

        [DataMember]
        public string Alias { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchSiblingViewAddJoinDto
    {
        [DataMember]
        public string Alias { get; set; }

        [DataMember]
        public string AppTableName { get; set; }

        [DataMember]
        public string JoinType { get; set; } = "LEFT";

        /// <summary>Must be 1:1 for Option A.</summary>
        [DataMember]
        public string Cardinality { get; set; } = "1:1";

        [DataMember]
        public string LeftTable { get; set; }

        [DataMember]
        public string LeftColumn { get; set; } = "ReferenceId";

        [DataMember]
        public string RightColumn { get; set; } = "ReferenceId";
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchSiblingViewLinkTargetsDto
    {
        [DataMember]
        public bool CopyFromDefaultSearchView { get; set; } = true;

        [DataMember]
        public List<PlmSearchImportLinkTargetDto> Items { get; set; } =
            new List<PlmSearchImportLinkTargetDto>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchSiblingViewCoverageDto
    {
        [DataMember]
        public int Covered { get; set; }

        [DataMember]
        public int AddColumn { get; set; }

        [DataMember]
        public int AddOneToOneLeftJoin { get; set; }

        [DataMember]
        public int RequiresOneToN { get; set; }

        [DataMember]
        public int Unmapped { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchSiblingViewLoadRequestDto
    {
        [DataMember]
        public string BlueprintJson { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchSiblingViewExecuteRequestDto
    {
        [DataMember]
        public PlmSearchSiblingViewBlueprintDto Blueprint { get; set; }

        [DataMember]
        public int? SaasApplicationId { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchSiblingViewExecuteResultDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public int? SearchId { get; set; }

        [DataMember]
        public int? DataSetId { get; set; }

        [DataMember]
        public int? SiblingSearchViewId { get; set; }

        [DataMember]
        public int? DefaultSearchViewId { get; set; }

        [DataMember]
        public List<string> Messages { get; set; } = new List<string>();
    }

    // -------------------------------------------------------------------------
    // Mass Update View — attach IsMassUpdateView SearchView (+ optional ListEdit create)
    // -------------------------------------------------------------------------

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchMassUpdateViewBlueprintDto
    {
        [DataMember]
        public int SchemaVersion { get; set; } = 1;

        /// <summary>Must be MassUpdateViewAttach.</summary>
        [DataMember]
        public string Mode { get; set; } = "MassUpdateViewAttach";

        [DataMember]
        public string GeneratedAt { get; set; }

        [DataMember]
        public PlmSearchMassUpdateViewSourceDto Source { get; set; }

        [DataMember]
        public PlmSearchMassUpdateViewTargetDto Target { get; set; }

        [DataMember]
        public PlmSearchSiblingViewDataSetPatchDto DataSetPatch { get; set; }

        [DataMember]
        public PlmSearchMassUpdateListEditCreateDto ListEditCreate { get; set; }

        [DataMember]
        public PlmSearchMassUpdateSettingsDto MassUpdate { get; set; }

        [DataMember]
        public PlmSearchImportSearchViewDto SearchView { get; set; }

        [DataMember]
        public PlmSearchMassUpdateViewCoverageDto Coverage { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchMassUpdateViewSourceDto
    {
        [DataMember]
        public int? PlmSearchTemplateId { get; set; }

        [DataMember]
        public string PlmSearchName { get; set; }

        [DataMember]
        public int? PlmMassUpdateViewId { get; set; }

        [DataMember]
        public string PlmMassUpdateViewName { get; set; }

        [DataMember]
        public string PlmUpdateType { get; set; }

        [DataMember]
        public int? PlmUpdateTypeId { get; set; }

        [DataMember]
        public int? PlmMainTabId { get; set; }

        [DataMember]
        public int? PlmGridBlockId { get; set; }

        [DataMember]
        public string TablePrefix { get; set; }

        [DataMember]
        public bool? IsPlmDefaultMassUpdateView { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchMassUpdateViewTargetDto
    {
        [DataMember]
        public string AppSearchIntegrationId { get; set; }

        [DataMember]
        public int? AppSearchId { get; set; }

        [DataMember]
        public int? AppDataSetId { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchMassUpdateSettingsDto
    {
        /// <summary>SingleTableUpdate | HierarchicalTableUpdate</summary>
        [DataMember]
        public string AppMode { get; set; }

        [DataMember]
        public string UpdateTransactionIntegrationId { get; set; }

        [DataMember]
        public int? UpdateTransactionId { get; set; }

        [DataMember]
        public int? UpdateBaseTransactionUnitId { get; set; }

        [DataMember]
        public string UpdateUnitTableName { get; set; }

        [DataMember]
        public string PkDatabaseFieldName { get; set; }

        [DataMember]
        public bool? IsAllowAddRow { get; set; } = true;

        [DataMember]
        public bool? IsAllowDeleteRow { get; set; } = true;

        [DataMember]
        public bool? IsAllowAdvancedUpdate { get; set; } = true;

        [DataMember]
        public bool? SetAsDefaultMassUpdateView { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchMassUpdateListEditCreateDto
    {
        /// <summary>UseExisting | CreateNew</summary>
        [DataMember]
        public string Action { get; set; }

        [DataMember]
        public int? ExistingTransactionId { get; set; }

        [DataMember]
        public string ExistingIntegrationId { get; set; }

        [DataMember]
        public PlmSearchMassUpdateListEditCreateSpecDto Create { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchMassUpdateListEditCreateSpecDto
    {
        [DataMember]
        public string IntegrationId { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string TransactionOrganizedType { get; set; } = "List";

        [DataMember]
        public int? TransactionOrganizedTypeId { get; set; } = 3;

        [DataMember]
        public int? TenantDataSourceRegisterId { get; set; }

        [DataMember]
        public int? SaasApplicationId { get; set; }

        [DataMember]
        public PlmSearchMassUpdateListEditUnitStructureDto UnitStructure { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchMassUpdateListEditUnitStructureDto
    {
        [DataMember]
        public string Mode { get; set; } = "RootPlusChild";

        [DataMember]
        public PlmSearchMassUpdateListEditUnitDto Root { get; set; }

        [DataMember]
        public List<PlmSearchMassUpdateListEditUnitDto> Children { get; set; } =
            new List<PlmSearchMassUpdateListEditUnitDto>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchMassUpdateListEditUnitDto
    {
        [DataMember]
        public string AppTableName { get; set; }

        [DataMember]
        public string UnitDisplayName { get; set; }

        [DataMember]
        public string PkColumn { get; set; }

        [DataMember]
        public string FkColumn { get; set; }

        [DataMember]
        public string ParentPkColumn { get; set; }

        [DataMember]
        public List<PlmSearchMassUpdateListEditFieldDto> Fields { get; set; } =
            new List<PlmSearchMassUpdateListEditFieldDto>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchMassUpdateListEditFieldDto
    {
        [DataMember]
        public string AppColumnName { get; set; }

        [DataMember]
        public string DisplayLabel { get; set; }

        [DataMember]
        public int? ControlType { get; set; }

        [DataMember]
        public bool IsVisible { get; set; } = true;

        [DataMember]
        public int? Sort { get; set; }

        [DataMember]
        public bool? IsPrimaryKey { get; set; }

        [DataMember]
        public bool? IsForeignKey { get; set; }

        [DataMember]
        public int? PlmSubItemId { get; set; }

        [DataMember]
        public int? PlmMetaColumnId { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchMassUpdateViewCoverageDto
    {
        [DataMember]
        public int MappedToTxnField { get; set; }

        [DataMember]
        public int CoveredInDataSet { get; set; }

        [DataMember]
        public int AddColumn { get; set; }

        [DataMember]
        public int AddOneToOneLeftJoin { get; set; }

        [DataMember]
        public int RequiresOneToN { get; set; }

        [DataMember]
        public int Unmapped { get; set; }

        [DataMember]
        public int ReadonlySkip { get; set; }

        [DataMember]
        public int ListEditChildFieldsProposed { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchMassUpdateViewLoadRequestDto
    {
        [DataMember]
        public string BlueprintJson { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchMassUpdateViewExecuteRequestDto
    {
        [DataMember]
        public PlmSearchMassUpdateViewBlueprintDto Blueprint { get; set; }

        [DataMember]
        public int? SaasApplicationId { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSearchMassUpdateViewExecuteResultDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public int? SearchId { get; set; }

        [DataMember]
        public int? DataSetId { get; set; }

        [DataMember]
        public int? MassUpdateSearchViewId { get; set; }

        [DataMember]
        public int? DefaultSearchViewId { get; set; }

        [DataMember]
        public int? ListEditTransactionId { get; set; }

        [DataMember]
        public string ListEditIntegrationId { get; set; }

        [DataMember]
        public List<string> Messages { get; set; } = new List<string>();
    }
}
