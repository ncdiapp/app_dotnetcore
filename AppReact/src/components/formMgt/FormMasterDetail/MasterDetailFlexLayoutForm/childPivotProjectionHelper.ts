// Type definitions for the "Child Unit Pivot Columns" projection
// (EmAppTransactionGridDisplayType.ChildUnitPivotColumns).
//
// IMPORTANT: All transform LOGIC lives on the C# server (AppChildPivotProjectionBL):
//   - POST /webapi/AppTransaction/BuildChildPivotProjection → ChildPivotProjectionModel
//   - POST /webapi/AppTransaction/FoldChildPivotProjection   → updated AppMasterDetailDto
// This file only mirrors the server DTO shapes so the UI can render the model. Keeping the logic
// server-side means the projection works unchanged across front-ends (Angular / React / other).

export interface ProjColumn {
  Header: string;
  Binding: string; // host field DataBaseFieldName
  FieldId?: number | null;
  ControlType?: number | null;
  IsReadOnly?: boolean;
  Visible?: boolean;
}

export interface ProjLeafColumn {
  Header: string;
  Binding: string; // `pv_${comboId}_${grandchildFieldName}`
  ComboId: string;
  DataBaseFieldName: string;
  FieldId?: number | null;
  ControlType?: number | null;
  Visible?: boolean;
}

export interface ProjColumnGroup {
  Header: string;
  ComboId: string;
  ColValue?: any;
  Columns: ProjLeafColumn[];
}

export interface ChildPivotProjectionModel {
  HostColumns?: ProjColumn[];
  ColumnGroups?: ProjColumnGroup[];
  WideRows?: any[];
  ColumnKeyFieldName?: string;
  ColumnSourceFieldName?: string;
  ColumnSourceFieldId?: number | null;
  ColumnSourceUnitId?: number | null;
  GrandchildUnitId?: number | null;
  IsConfigured?: boolean;
  ChildRowCount?: number;
  SourceRowCount?: number;
}
