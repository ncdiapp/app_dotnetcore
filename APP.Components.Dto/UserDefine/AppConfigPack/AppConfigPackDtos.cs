using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackDto
    {
        [DataMember]
        public int SchemaVersion { get; set; } = 1;

        [DataMember]
        public string GeneratedAt { get; set; }

        [DataMember]
        public AppConfigPackSourceDto Source { get; set; }

        [DataMember]
        public List<AppConfigPackTableDto> Tables { get; set; } = new List<AppConfigPackTableDto>();

        [DataMember]
        public List<AppConfigPackViewDto> Views { get; set; } = new List<AppConfigPackViewDto>();

        [DataMember]
        public List<AppConfigPackTransactionDto> Transactions { get; set; } = new List<AppConfigPackTransactionDto>();

        [DataMember]
        public AppConfigPackTransactionGroupDto TransactionGroup { get; set; }

        [DataMember]
        public List<AppConfigPackSearchDto> Searches { get; set; } = new List<AppConfigPackSearchDto>();

        /// <summary>Simple Value List (EmAppEntityType.SimpleValueList = 4) entities, upserted by entityCode before transactions.</summary>
        [DataMember]
        public List<AppConfigPackSimpleListEntityDto> SimpleListEntities { get; set; } = new List<AppConfigPackSimpleListEntityDto>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackSourceDto
    {
        /// <summary>export | ai | manual</summary>
        [DataMember]
        public string GeneratedBy { get; set; }

        [DataMember]
        public string ApplicationName { get; set; }

        [DataMember]
        public int? SaasApplicationId { get; set; }

        [DataMember]
        public string Notes { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackSimpleListEntityDto
    {
        [DataMember]
        public string EntityCode { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public List<AppConfigPackSimpleListValueDto> Values { get; set; } = new List<AppConfigPackSimpleListValueDto>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackSimpleListValueDto
    {
        [DataMember]
        public int? InternalKey { get; set; }

        [DataMember]
        public string Code { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public int? Sort { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackTableDto
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string SchemaOwner { get; set; } = "dbo";

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public List<AppConfigPackColumnDto> Columns { get; set; } = new List<AppConfigPackColumnDto>();

        [DataMember]
        public List<AppConfigPackRelationshipDto> Relationships { get; set; } = new List<AppConfigPackRelationshipDto>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackColumnDto
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string DataType { get; set; }

        [DataMember]
        public int? Length { get; set; }

        [DataMember]
        public int? Precision { get; set; }

        [DataMember]
        public int? Scale { get; set; }

        [DataMember]
        public bool IsPrimaryKey { get; set; }

        [DataMember]
        public bool IsNullable { get; set; } = true;

        [DataMember]
        public bool IsAutoIncrement { get; set; }

        [DataMember]
        public string DefaultValue { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackRelationshipDto
    {
        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public string TargetTable { get; set; }

        [DataMember]
        public string ForeignKeyColumn { get; set; }

        [DataMember]
        public string ReferencedColumn { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackViewDto
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string SchemaOwner { get; set; } = "dbo";

        [DataMember]
        public string CreateOrAlterSql { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackTransactionDto
    {
        [DataMember]
        public string IntegrationId { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        /// <summary>
        /// Screen pattern: MasterDetail (default) | List (ListEdit grid).
        /// List aliases: ListEdit. Maps to EmTransactionOrganizedType (1 / 3).
        /// Null/omit on insert = MasterDetail; on update = leave existing.
        /// </summary>
        [DataMember]
        public string OrganizedType { get; set; }

        [DataMember]
        public AppConfigPackUnitStructureDto UnitStructure { get; set; }

        [DataMember]
        public List<AppConfigPackFieldDto> Fields { get; set; } = new List<AppConfigPackFieldDto>();

        /// <summary>Transaction commands (Execute SQL, Refresh, Composition). Matched by Name within the transaction.</summary>
        [DataMember]
        public List<AppConfigPackCommandDto> Commands { get; set; } = new List<AppConfigPackCommandDto>();

        /// <summary>
        /// Portable Flex form tree. When Items is non-empty, import deletes the existing layout and rebuilds it.
        /// When omitted, import still creates the default Flex form (form-if-missing) and applies layoutTab / layoutHostTable.
        /// </summary>
        [DataMember]
        public AppConfigPackFormLayoutDto FormLayout { get; set; }

        /// <summary>Default | Flex. Flex is set on export when formLayout is present.</summary>
        [DataMember]
        public string FormMode { get; set; } = "Default";

        /// <summary>AppTransaction.IsShowSaveButton. Null/omit = keep current default.</summary>
        [DataMember]
        public bool? IsShowSaveButton { get; set; }

        /// <summary>AppTransaction.IsShowPrintButton. Null/omit = keep current default.</summary>
        [DataMember]
        public bool? IsShowPrintButton { get; set; }

        /// <summary>AppTransaction.IsShowCalculateButton. Null/omit = keep current default.</summary>
        [DataMember]
        public bool? IsShowCalculateButton { get; set; }

        /// <summary>Whole-transaction read-only (AppTransaction.IsReadOnly). Null/omit = keep current default.</summary>
        [DataMember]
        public bool? IsReadOnly { get; set; }

        /// <summary>
        /// Register this transaction on the Application main menu (FormListEdit route).
        /// Requires organizedType List / ListEdit. Prefer this over a Search menu for simple grid CRUD.
        /// </summary>
        [DataMember]
        public AppConfigPackMenuDto Menu { get; set; }

        /// <summary>Null/omit = leave existing. [] = clear. Otherwise replace all data loads on this transaction.</summary>
        [DataMember]
        public List<AppConfigPackDataLoadDto> DataLoads { get; set; }

        /// <summary>Null/omit = leave existing. [] = clear. Otherwise replace all unit formulas on this transaction.</summary>
        [DataMember]
        public List<AppConfigPackUnitFormulaDto> UnitFormulas { get; set; }

        /// <summary>Null/omit = leave existing. [] = clear. Otherwise replace all conditional actions on this transaction.</summary>
        [DataMember]
        public List<AppConfigPackConditionalActionDto> ConditionalActions { get; set; }

        /// <summary>Null/omit = leave existing. [] = clear. Otherwise replace all unit linked-search mappings on this transaction.</summary>
        [DataMember]
        public List<AppConfigPackLinkedSearchDto> LinkedSearches { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackFormLayoutDto
    {
        [DataMember]
        public int? DefaultNbColumns { get; set; }

        [DataMember]
        public string DefaultWidth { get; set; }

        [DataMember]
        public List<AppConfigPackFormLayoutItemDto> Items { get; set; } = new List<AppConfigPackFormLayoutItemDto>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackFormLayoutItemDto
    {
        /// <summary>
        /// row | stack | tabContainer | tab | field | grid | commandButton | content | space |
        /// addButton | linkedSearch | tableContainer | htmlContentContainer | widget
        /// </summary>
        [DataMember]
        public string Type { get; set; }

        /// <summary>EmAppFormLayoutItemType. Required when Type is field or an uncommon widget.</summary>
        [DataMember]
        public int? WidgetDisplayType { get; set; }

        [DataMember]
        public string DisplayName { get; set; }

        [DataMember]
        public int? Sort { get; set; }

        [DataMember]
        public int? DefaultNbColumns { get; set; }

        [DataMember]
        public int? ColSpan { get; set; }

        [DataMember]
        public int? Height { get; set; }

        [DataMember]
        public bool? IsUnlimitedHeight { get; set; }

        [DataMember]
        public string BackgroundColor { get; set; }

        [DataMember]
        public string TextColor { get; set; }

        [DataMember]
        public bool? IsHideLabel { get; set; }

        [DataMember]
        public int? LabelWidth { get; set; }

        [DataMember]
        public int? EmUnitLabelPosition { get; set; }

        [DataMember]
        public bool? IsCollapsible { get; set; }

        [DataMember]
        public bool? IsDefaultCollapsed { get; set; }

        [DataMember]
        public bool? IsTab { get; set; }

        [DataMember]
        public bool? IsBindingToDataField { get; set; }

        [DataMember]
        public int? TranscationUnitLevel { get; set; }

        [DataMember]
        public int? ColumnWidth { get; set; }

        [DataMember]
        public string HtmlContent { get; set; }

        [DataMember]
        public string VisibleExpression { get; set; }

        [DataMember]
        public string InlineStyle { get; set; }

        [DataMember]
        public bool? IsShowSearchCriterias { get; set; }

        [DataMember]
        public bool? IsDisplayGridAsCardList { get; set; }

        [DataMember]
        public bool? IsDisplayAsSlider { get; set; }

        [DataMember]
        public int? NbDecimal { get; set; }

        [DataMember]
        public string TableName { get; set; }

        [DataMember]
        public string ColumnName { get; set; }

        [DataMember]
        public string CommandName { get; set; }

        [DataMember]
        public string SearchIntegrationId { get; set; }

        [DataMember]
        public string EntityCode { get; set; }

        [DataMember]
        public List<AppConfigPackFormLayoutItemDto> Children { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackCommandDto
    {
        /// <summary>Pack-local id for composition childCommandIntegrationIds. Not stored on AppProjectWorkFlowAction.</summary>
        [DataMember]
        public string IntegrationId { get; set; }

        [DataMember]
        public string Name { get; set; }

        /// <summary>EmAppTransactionCommandType: 42 ExecuteSQLStatement, 50 refresh, 200 CompositionCommand.</summary>
        [DataMember]
        public int ActionType { get; set; }

        /// <summary>
        /// SQL for actionType 42. Use portable tokens [TF:Table.Column] (rewritten to [TF_{FieldId}_{Column}] on import).
        /// [CurrentUserId] is left as-is.
        /// </summary>
        [DataMember]
        public string SqlStatement { get; set; }

        [DataMember]
        public List<string> ChildCommandIntegrationIds { get; set; }

        [DataMember]
        public bool? IsShowOnTopMenu { get; set; }

        [DataMember]
        public bool? LinkToUI { get; set; }

        /// <summary>Place a CommandActionButton on the default Flex form, above this unit's grid.</summary>
        [DataMember]
        public string LayoutHostTable { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackUnitStructureDto
    {
        [DataMember]
        public string RootTableName { get; set; }

        [DataMember]
        public string RootDisplayName { get; set; }

        [DataMember]
        public List<string> SiblingTableNames { get; set; } = new List<string>();

        [DataMember]
        public List<AppConfigPackSiblingUnitDto> SiblingUnits { get; set; } = new List<AppConfigPackSiblingUnitDto>();

        [DataMember]
        public List<AppConfigPackChildUnitDto> ChildUnits { get; set; } = new List<AppConfigPackChildUnitDto>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackSiblingUnitDto
    {
        [DataMember]
        public string TableName { get; set; }

        [DataMember]
        public string DisplayName { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackChildUnitDto
    {
        [DataMember]
        public string TableName { get; set; }

        [DataMember]
        public string DisplayName { get; set; }

        [DataMember]
        public List<string> GrandChildTableNames { get; set; } = new List<string>();

        [DataMember]
        public List<AppConfigPackChildUnitDto> GrandChildUnits { get; set; } = new List<AppConfigPackChildUnitDto>();

        [DataMember]
        public int? GridDisplayType { get; set; }

        [DataMember]
        public bool? IsReadOnly { get; set; }

        [DataMember]
        public bool? IsSynchToDatabaseTable { get; set; }

        /// <summary>Hide the child grid Add row button (AppTransactionUnit.IsDisableAddButton).</summary>
        [DataMember]
        public bool? IsDisableAddButton { get; set; }

        /// <summary>Hide the child grid Delete row button (AppTransactionUnit.IsDisableDeleteButton).</summary>
        [DataMember]
        public bool? IsDisableDeleteButton { get; set; }

        /// <summary>Available Select source unit table/view name (same transaction).</summary>
        [DataMember]
        public string AvailableSourceTableName { get; set; }

        /// <summary>Column on this (selected) unit mapped to the available source.</summary>
        [DataMember]
        public string AvailableSelectSelectedColumn { get; set; }

        /// <summary>Column on the available source unit. Defaults to AvailableSelectSelectedColumn.</summary>
        [DataMember]
        public string AvailableSelectSourceColumn { get; set; }

        [DataMember]
        public List<AppConfigPackLinkTargetDto> LinkTargets { get; set; }

        /// <summary>Form tab title for this child grid when the default layout uses a Tab Container.</summary>
        [DataMember]
        public string LayoutTab { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackFieldDto
    {
        [DataMember]
        public string TableName { get; set; }

        [DataMember]
        public string ColumnName { get; set; }

        [DataMember]
        public string DisplayName { get; set; }

        [DataMember]
        public int? ControlType { get; set; }

        [DataMember]
        public string EntityCode { get; set; }

        [DataMember]
        public bool? IsVisible { get; set; }

        [DataMember]
        public bool? IsReadOnly { get; set; }

        [DataMember]
        public bool? IsPrimaryKey { get; set; }

        [DataMember]
        public bool? IsLinkToParentPrimaryKey { get; set; }

        [DataMember]
        public bool? IsPivotRow { get; set; }

        [DataMember]
        public bool? IsPivotColumn { get; set; }

        [DataMember]
        public bool? IsPivotValue { get; set; }

        [DataMember]
        public string MatrixSourceTable { get; set; }

        [DataMember]
        public string MatrixSourceColumn { get; set; }

        /// <summary>DDL parent field table (resolves DDLParentLevelID).</summary>
        [DataMember]
        public string DependsOnTable { get; set; }

        /// <summary>DDL parent field column (resolves DDLParentLevelID).</summary>
        [DataMember]
        public string DependsOnColumn { get; set; }

        [DataMember]
        public string CascadingRelationTable { get; set; }

        [DataMember]
        public string CascadingRelationSchemaOwner { get; set; }

        [DataMember]
        public string CascadingParentKey { get; set; }

        [DataMember]
        public string CascadingChildKey { get; set; }

        [DataMember]
        public int? SortOrder { get; set; }

        /// <summary>Decimal places for numeric fields (AppTransactionField.NBDecimal).</summary>
        [DataMember]
        public int? NbDecimal { get; set; }

        /// <summary>Grid/form field width (AppTransactionField.DisplayWidth).</summary>
        [DataMember]
        public string DisplayWidth { get; set; }

        /// <summary>
        /// Query Datasource SQL (AppTransactionField.DdlQueryText). First column = id, second = display.
        /// When set, EntityId is cleared. Use @p0, @p1... with ddlQueryParameterColumns.
        /// </summary>
        [DataMember]
        public string DdlQueryText { get; set; }

        /// <summary>Table.Column (or Column) for each @pN in ddlQueryText. Stored as pipe-separated field ids in WhereClauseExpress.</summary>
        [DataMember]
        public List<string> DdlQueryParameterColumns { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackDataLoadDto
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string TableName { get; set; }

        [DataMember]
        public int? LoadOrder { get; set; }

        [DataMember]
        public bool? IsAutoExecutedWhenOpenEditForm { get; set; }

        [DataMember]
        public bool? IsAutoExecuteBeforeInitialCascading { get; set; }

        [DataMember]
        public AppConfigPackDataSetDto DataSet { get; set; }

        [DataMember]
        public List<AppConfigPackDataLoadMappingDto> Mappings { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackDataLoadMappingDto
    {
        [DataMember]
        public string TableName { get; set; }

        [DataMember]
        public string ColumnName { get; set; }

        [DataMember]
        public string DataSetColumn { get; set; }

        [DataMember]
        public bool? IsConditionMapping { get; set; }

        [DataMember]
        public string WhereClause { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackUnitFormulaDto
    {
        [DataMember]
        public string TableName { get; set; }

        [DataMember]
        public string FormulaName { get; set; }

        /// <summary>Use [TF:Table.Column] tokens. Rewritten to transactionfieldid_{FieldId} on import.</summary>
        [DataMember]
        public string FormulaExpression { get; set; }

        [DataMember]
        public string WarningMessage { get; set; }

        [DataMember]
        public int? CalculationFlowSort { get; set; }

        [DataMember]
        public int? FunctionType { get; set; }

        [DataMember]
        public int? OperationType { get; set; }

        [DataMember]
        public int? ApplyToScope { get; set; }

        [DataMember]
        public string ConditionTableName { get; set; }

        [DataMember]
        public string ConditionColumnName { get; set; }

        [DataMember]
        public bool? SwitchTrueFalseType { get; set; }

        [DataMember]
        public string ChildTableName { get; set; }

        [DataMember]
        public string HighlightTableName { get; set; }

        [DataMember]
        public string HighlightColumnName { get; set; }

        [DataMember]
        public int? WarningHighlightStyleId { get; set; }

        [DataMember]
        public string SearchIntegrationId { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackConditionalActionDto
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string ConditionTableName { get; set; }

        [DataMember]
        public string BooleanConditionTableName { get; set; }

        [DataMember]
        public string BooleanConditionColumnName { get; set; }

        [DataMember]
        public string UiTriggerTableName { get; set; }

        [DataMember]
        public string UiTriggerColumnName { get; set; }

        /// <summary>Use [TF:Table.Column] tokens.</summary>
        [DataMember]
        public string BooleanConditionFormula { get; set; }

        [DataMember]
        public string LockingTableName { get; set; }

        [DataMember]
        public string LockingColumnName { get; set; }

        [DataMember]
        public string LockingFieldUnitTableName { get; set; }

        [DataMember]
        public bool? IsLockingTransaction { get; set; }

        [DataMember]
        public string LockingTransactionUnitTableName { get; set; }

        [DataMember]
        public bool? IsLockForSpecialEditPrivilege { get; set; }

        [DataMember]
        public string HideTableName { get; set; }

        [DataMember]
        public string HideColumnName { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackLinkedSearchDto
    {
        [DataMember]
        public string TableName { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string SearchIntegrationId { get; set; }

        [DataMember]
        public int? Action { get; set; }

        [DataMember]
        public int? UsageType { get; set; }

        [DataMember]
        public string GroupName { get; set; }

        [DataMember]
        public bool? IsSingleSelectedRow { get; set; }

        [DataMember]
        public bool? IsNeedPreValidation { get; set; }

        [DataMember]
        public bool? IsNeedPostValidation { get; set; }

        [DataMember]
        public string CallbackRestResourceUri { get; set; }

        [DataMember]
        public string TargetTransactionIntegrationId { get; set; }

        [DataMember]
        public string ConditionTableName { get; set; }

        [DataMember]
        public string ConditionColumnName { get; set; }

        [DataMember]
        public string CallbackCommandName { get; set; }

        [DataMember]
        public int? Sort { get; set; }

        [DataMember]
        public bool? IsPopup { get; set; }

        [DataMember]
        public int? PopupWidth { get; set; }

        [DataMember]
        public int? PopupHeight { get; set; }

        [DataMember]
        public string IconName { get; set; }

        [DataMember]
        public string OtherSettings { get; set; }

        /// <summary>Criteria / search-field mappings (AppTransactionUnitSearchFieldMapping).</summary>
        [DataMember]
        public List<AppConfigPackLinkedSearchFieldMappingDto> FieldMappings { get; set; }

        /// <summary>Result-column mappings (AppTransactionUnitSearchViewFieldMapping).</summary>
        [DataMember]
        public List<AppConfigPackLinkedSearchViewMappingDto> ViewFieldMappings { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackLinkedSearchFieldMappingDto
    {
        [DataMember]
        public string TableName { get; set; }

        [DataMember]
        public string ColumnName { get; set; }

        [DataMember]
        public string SearchFieldColumn { get; set; }

        [DataMember]
        public string TargetTableName { get; set; }

        [DataMember]
        public string TargetColumnName { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackLinkedSearchViewMappingDto
    {
        [DataMember]
        public string TableName { get; set; }

        [DataMember]
        public string ColumnName { get; set; }

        [DataMember]
        public string SearchViewFieldColumn { get; set; }

        [DataMember]
        public string TargetTableName { get; set; }

        [DataMember]
        public string TargetColumnName { get; set; }

        [DataMember]
        public string ExternalAppFieldMappingCode { get; set; }

        [DataMember]
        public bool? IsUnique { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackTransactionGroupDto
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string IntegrationId { get; set; }

        [DataMember]
        public string PrimaryTransactionIntegrationId { get; set; }

        [DataMember]
        public List<string> MemberTransactionIntegrationIds { get; set; } = new List<string>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackSearchDto
    {
        [DataMember]
        public string IntegrationId { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        /// <summary>Management | DataModelTemplate</summary>
        [DataMember]
        public string UsageType { get; set; } = "Management";

        [DataMember]
        public bool AutoExecute { get; set; } = true;

        [DataMember]
        public AppConfigPackDataSetDto DataSet { get; set; }

        [DataMember]
        public List<AppConfigPackCriteriaFieldDto> CriteriaFields { get; set; } =
            new List<AppConfigPackCriteriaFieldDto>();

        [DataMember]
        public AppConfigPackSearchViewDto SearchView { get; set; }

        [DataMember]
        public List<AppConfigPackLinkTargetDto> LinkTargets { get; set; } =
            new List<AppConfigPackLinkTargetDto>();

        [DataMember]
        public AppConfigPackMenuDto Menu { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackDataSetDto
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string PrimaryTableName { get; set; }

        [DataMember]
        public string QueryText { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackCriteriaFieldDto
    {
        [DataMember]
        public string DisplayText { get; set; }

        [DataMember]
        public string SysTableFiledPath { get; set; }

        [DataMember]
        public int? ControlType { get; set; }

        [DataMember]
        public string EntityCode { get; set; }

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
    public class AppConfigPackSearchViewDto
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string IntegrationId { get; set; }

        [DataMember]
        public int GridOutputMode { get; set; } = 1;

        [DataMember]
        public List<AppConfigPackSearchViewFieldDto> Fields { get; set; } =
            new List<AppConfigPackSearchViewFieldDto>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackSearchViewFieldDto
    {
        [DataMember]
        public string DisplayText { get; set; }

        [DataMember]
        public string SysTableFiledPath { get; set; }

        [DataMember]
        public int? ControlType { get; set; }

        [DataMember]
        public string EntityCode { get; set; }

        [DataMember]
        public bool IsTransRootId { get; set; }

        [DataMember]
        public bool IsVisible { get; set; } = true;

        [DataMember]
        public int? Sort { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackLinkTargetDto
    {
        [DataMember]
        public string Name { get; set; }

        /// <summary>Create | Edit | Delete</summary>
        [DataMember]
        public string ActionType { get; set; } = "Edit";

        [DataMember]
        public string TransactionIntegrationId { get; set; }

        [DataMember]
        public string SourceColumn { get; set; }

        [DataMember]
        public string TargetColumn { get; set; }

        [DataMember]
        public bool? IsPopup { get; set; }

        [DataMember]
        public int? PopupWidth { get; set; }

        [DataMember]
        public int? PopupHeight { get; set; }

        [DataMember]
        public int? Sort { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackMenuDto
    {
        [DataMember]
        public bool RegisterInMainMenu { get; set; }

        [DataMember]
        public string MenuTitle { get; set; }

        [DataMember]
        public int? MenuOrder { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackLoadRequestDto
    {
        [DataMember]
        public string PackJson { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackExecuteRequestDto
    {
        [DataMember]
        public AppConfigPackDto Pack { get; set; }

        [DataMember]
        public int? SaasApplicationId { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackExportRequestDto
    {
        [DataMember]
        public int? SaasApplicationId { get; set; }

        /// <summary>
        /// When true, export the whole application. Empty TransactionIds/SearchIds then mean "all", not "none".
        /// When false, each id list is an inclusive filter; an empty list exports none of that kind.
        /// </summary>
        [DataMember]
        public bool ExportAll { get; set; }

        [DataMember]
        public List<int> TransactionIds { get; set; } = new List<int>();

        [DataMember]
        public List<int> SearchIds { get; set; } = new List<int>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackExportResultDto
    {
        [DataMember]
        public AppConfigPackDto Pack { get; set; }

        [DataMember]
        public string JsonText { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackValidationDto
    {
        [DataMember]
        public bool IsValid { get; set; }

        [DataMember]
        public List<string> Errors { get; set; } = new List<string>();

        [DataMember]
        public List<string> Warnings { get; set; } = new List<string>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackPreviewDto
    {
        [DataMember]
        public bool IsSuccess { get; set; } = true;

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public List<AppConfigPackPreviewItemDto> Items { get; set; } = new List<AppConfigPackPreviewItemDto>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackPreviewItemDto
    {
        [DataMember]
        public string ObjectType { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string IntegrationId { get; set; }

        [DataMember]
        public string Action { get; set; }

        [DataMember]
        public int? ExistingId { get; set; }

        [DataMember]
        public string Detail { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppConfigPackExecuteResultDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public List<string> Messages { get; set; } = new List<string>();

        [DataMember]
        public int TablesCreated { get; set; }

        [DataMember]
        public int ColumnsAdded { get; set; }

        [DataMember]
        public int ViewsApplied { get; set; }

        [DataMember]
        public int TransactionsInserted { get; set; }

        [DataMember]
        public int TransactionsUpdated { get; set; }

        [DataMember]
        public int SearchesInserted { get; set; }

        [DataMember]
        public int SearchesUpdated { get; set; }

        [DataMember]
        public int? TransactionGroupId { get; set; }
    }
}
