using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    /// <summary>Import Blueprint schema version for PLM DW → APP transaction configuration.</summary>
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmDwImportBlueprintDto
    {
        [DataMember]
        public int SchemaVersion { get; set; } = 1;

        [DataMember]
        public string GeneratedAt { get; set; }

        [DataMember]
        public PlmDwBlueprintSourceDto Source { get; set; }

        [DataMember]
        public PlmDwBlueprintTransactionGroupDto TransactionGroup { get; set; }

        [DataMember]
        public PlmDwBlueprintRootUnitDto RootUnit { get; set; }

        [DataMember]
        public List<PlmDwBlueprintTabSharedTableGroupDto> TabSharedTableGroups { get; set; } =
            new List<PlmDwBlueprintTabSharedTableGroupDto>();

        [DataMember]
        public List<PlmDwBlueprintTransactionDto> Transactions { get; set; } =
            new List<PlmDwBlueprintTransactionDto>();

        [DataMember]
        public List<PlmDwBlueprintGridBindingDto> GridBindings { get; set; } =
            new List<PlmDwBlueprintGridBindingDto>();

        [DataMember]
        public List<PlmDwBlueprintFieldDto> BlueprintFields { get; set; } =
            new List<PlmDwBlueprintFieldDto>();

        [DataMember]
        public PlmDwBlueprintSearchViewDto SearchView { get; set; }

        [DataMember]
        public PlmDwBlueprintNavigationDto Navigation { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmDwBlueprintSourceDto
    {
        [DataMember]
        public string DwDatabase { get; set; }

        [DataMember]
        public List<int> ImportTabIds { get; set; } = new List<int>();

        [DataMember]
        public string TablePrefix { get; set; }

        [DataMember]
        public string ConfigFile { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmDwBlueprintTransactionGroupDto
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string IntegrationId { get; set; }

        [DataMember]
        public int? SaasApplicationId { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmDwBlueprintReferenceScopeDto
    {
        [DataMember]
        public string DwTable { get; set; }

        [DataMember]
        public string DwColumn { get; set; }

        [DataMember]
        public int PlmTabId { get; set; }

        [DataMember]
        public int PlmSubItemId { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmDwBlueprintRootUnitDto
    {
        [DataMember]
        public string AppTableName { get; set; }

        [DataMember]
        public string IntegrationId { get; set; }

        [DataMember]
        public PlmDwBlueprintReferenceScopeDto ReferenceScope { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmDwBlueprintTabSharedTableGroupDto
    {
        [DataMember]
        public string GroupId { get; set; }

        [DataMember]
        public string SharedAppTableName { get; set; }

        [DataMember]
        public int PrimaryPlmTabId { get; set; }

        [DataMember]
        public List<int> SecondaryPlmTabIds { get; set; } = new List<int>();

        /// <summary>SharedSubItemsOnPrimaryOnly</summary>
        [DataMember]
        public string Rule { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmDwBlueprintTransactionDto
    {
        [DataMember]
        public int PlmTabId { get; set; }

        [DataMember]
        public string PlmTabName { get; set; }

        [DataMember]
        public string IntegrationId { get; set; }

        [DataMember]
        public string TransactionName { get; set; }

        /// <summary>Ready | Skipped</summary>
        [DataMember]
        public string ImportStatus { get; set; } = "Ready";

        [DataMember]
        public PlmDwBlueprintUnitStructureDto UnitStructure { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmDwBlueprintUnitStructureDto
    {
        /// <summary>RootPlusMasterSibling</summary>
        [DataMember]
        public string Mode { get; set; }

        [DataMember]
        public string RootTableName { get; set; }

        [DataMember]
        public List<PlmDwBlueprintSiblingUnitDto> SiblingUnits { get; set; } =
            new List<PlmDwBlueprintSiblingUnitDto>();

        [DataMember]
        public List<PlmDwBlueprintChildUnitDto> ChildUnits { get; set; } =
            new List<PlmDwBlueprintChildUnitDto>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmDwBlueprintSiblingUnitDto
    {
        [DataMember]
        public string AppTableName { get; set; }

        [DataMember]
        public bool IsMasterSibling { get; set; } = true;

        /// <summary>AllMappedColumns | ExclusiveSubItemsOnly</summary>
        [DataMember]
        public string FieldPolicy { get; set; }

        [DataMember]
        public string ExcludeSubItemsFromDwTable { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmDwBlueprintChildUnitDto
    {
        [DataMember]
        public string AppTableName { get; set; }

        [DataMember]
        public bool AttachToRoot { get; set; } = true;
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmDwBlueprintGridBindingDto
    {
        [DataMember]
        public int PlmGridId { get; set; }

        [DataMember]
        public string AppTableName { get; set; }

        [DataMember]
        public int? ParentPlmTabId { get; set; }

        [DataMember]
        public bool AttachToRoot { get; set; } = true;

        [DataMember]
        public string IntegrationId { get; set; }

        [DataMember]
        public string TransactionIntegrationId { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmDwBlueprintFieldDto
    {
        [DataMember]
        public string AppTableName { get; set; }

        [DataMember]
        public string AppColumnName { get; set; }

        [DataMember]
        public List<int> PlmTabIds { get; set; } = new List<int>();

        [DataMember]
        public int? AppControlType { get; set; }

        [DataMember]
        public int? PlmControlType { get; set; }

        [DataMember]
        public int? PlmEntityId { get; set; }

        [DataMember]
        public string EntityIntegrationId { get; set; }

        [DataMember]
        public string DisplayLabel { get; set; }

        [DataMember]
        public int? DisplayOrder { get; set; }

        [DataMember]
        public string FormSection { get; set; }

        [DataMember]
        public bool IncludeInSearch { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmDwBlueprintSearchViewDto
    {
        [DataMember]
        public PlmDwBlueprintSearchDto Search { get; set; }

        [DataMember]
        public PlmDwBlueprintSearchViewSpecDto SearchView { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmDwBlueprintSearchDto
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string IntegrationId { get; set; }

        [DataMember]
        public string UsageType { get; set; } = "DataModelTemplate";

        [DataMember]
        public string RootTableName { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmDwBlueprintSearchViewSpecDto
    {
        [DataMember]
        public string IntegrationId { get; set; }

        /// <summary>DefaultReferenceBasicInfo or explicit field list key</summary>
        [DataMember]
        public string Fields { get; set; } = "DefaultReferenceBasicInfo";
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmDwBlueprintNavigationDto
    {
        [DataMember]
        public string FolderName { get; set; }

        [DataMember]
        public string ParentFolderIntegrationId { get; set; }

        [DataMember]
        public int MenuOrder { get; set; } = 100;
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmDwBlueprintLoadRequestDto
    {
        [DataMember]
        public string TablePrefix { get; set; }

        [DataMember]
        public string BlueprintJson { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmDwBlueprintExecuteRequestDto
    {
        [DataMember]
        public PlmDwImportBlueprintDto Blueprint { get; set; }

        [DataMember]
        public int? SaasApplicationId { get; set; }

        /// <summary>Insert | Update | Repair</summary>
        [DataMember]
        public string Mode { get; set; } = "Insert";

        [DataMember]
        public bool IncludeSearchView { get; set; } = true;

        [DataMember]
        public bool IncludeNavigation { get; set; } = true;

        [DataMember]
        public bool IncludeTransactionGroup { get; set; } = true;
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmDwBlueprintValidationDto
    {
        [DataMember]
        public bool IsValid { get; set; }

        [DataMember]
        public List<string> Errors { get; set; } = new List<string>();

        [DataMember]
        public List<string> Warnings { get; set; } = new List<string>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmDwBlueprintPreviewDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public List<PlmDwBlueprintPreviewItemDto> Items { get; set; } =
            new List<PlmDwBlueprintPreviewItemDto>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmDwBlueprintPreviewItemDto
    {
        [DataMember]
        public string ObjectType { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string IntegrationId { get; set; }

        /// <summary>Insert | Update | Skip</summary>
        [DataMember]
        public string Action { get; set; }

        [DataMember]
        public int? ExistingId { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmDwBlueprintExecuteResultDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public int TransactionsInserted { get; set; }

        [DataMember]
        public int TransactionsUpdated { get; set; }

        [DataMember]
        public int? TransactionGroupId { get; set; }

        [DataMember]
        public int? SearchId { get; set; }

        [DataMember]
        public List<int> TransactionIds { get; set; } = new List<int>();
    }
}
