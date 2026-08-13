using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    // View-model DTOs for the "Child Unit Pivot Columns" projection
    // (EmAppTransactionGridDisplayType.ChildUnitPivotColumns).
    //
    // All transform logic lives server-side (AppChildPivotProjectionBL). The UI only renders the
    // returned model and sends edited wide rows back to be folded. This keeps the projection logic
    // independent of any specific front-end (Angular / React / other).

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class ChildPivotProjectionBuildRequestDto
    {
        [DataMember(EmitDefaultValue = false)]
        public AppMasterDetailDto FormData { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int HostUnitId { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class ChildPivotProjectionFoldRequestDto
    {
        [DataMember(EmitDefaultValue = false)]
        public AppMasterDetailDto FormData { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int HostUnitId { get; set; }

        // Edited wide rows (one per child row, in child-row order). Keyed by host DataBaseFieldName
        // and leaf binding `pv_{comboId}_{grandchildFieldName}`. May carry `__rowIndex`.
        [DataMember(EmitDefaultValue = false)]
        public List<Dictionary<string, object>> WideRows { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class ProjColumnDto
    {
        [DataMember(EmitDefaultValue = false)]
        public string Header { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Binding { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? FieldId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? ControlType { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? Nbdecimal { get; set; }

        [DataMember]
        public bool IsReadOnly { get; set; }

        [DataMember]
        public bool Visible { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class ProjLeafColumnDto
    {
        [DataMember(EmitDefaultValue = false)]
        public string Header { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Binding { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string ComboId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string DataBaseFieldName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? FieldId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? ControlType { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? Nbdecimal { get; set; }

        [DataMember]
        public bool Visible { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class ProjColumnGroupDto
    {
        [DataMember(EmitDefaultValue = false)]
        public string Header { get; set; }

        /// <summary>Optional: IsPivotRow Column Group field id (value-based parent headers).</summary>
        [DataMember(EmitDefaultValue = false)]
        public int? FieldId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string ComboId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public object ColValue { get; set; }

        /// <summary>Leaf value columns (data-bearing groups). Null/empty for static parent groups.</summary>
        [DataMember(EmitDefaultValue = false)]
        public List<ProjLeafColumnDto> Columns { get; set; }

        /// <summary>Nested child groups (IsPivotRow parents and/or per-comboId groups).</summary>
        [DataMember(EmitDefaultValue = false)]
        public List<ProjColumnGroupDto> ChildGroups { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class ChildPivotProjectionModelDto
    {
        [DataMember(EmitDefaultValue = false)]
        public List<ProjColumnDto> HostColumns { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public List<ProjColumnGroupDto> ColumnGroups { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public List<Dictionary<string, object>> WideRows { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string ColumnKeyFieldName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string ColumnSourceFieldName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? ColumnSourceFieldId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? ColumnSourceUnitId { get; set; }

        /// <summary>
        /// Optional source-grid boolean field (DB name) that gates which key rows become pivot columns.
        /// From pivot-column field MatrixKeyTransactionFieldId. Null = project all keys.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string ColumnSourceVisibleFieldName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? ColumnSourceVisibleFieldId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? GrandchildUnitId { get; set; }

        // True when the host unit actually has a ChildUnitPivotColumns grandchild.
        [DataMember]
        public bool IsConfigured { get; set; }

        /// <summary>
        /// True when grandchild has at least one IsPivotRow field (not the IsPivotColumn field).
        /// UI should render FlexGridColumnGroup hierarchy; otherwise flat FlexGridColumn.
        /// </summary>
        [DataMember]
        public bool IsNeedPivotColumnGroup { get; set; }

        // Diagnostics for the empty-state UI.
        [DataMember(EmitDefaultValue = false)]
        public int ChildRowCount { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int SourceRowCount { get; set; }
    }
}
