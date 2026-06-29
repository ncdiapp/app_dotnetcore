using System;
using System.Collections.Generic;
using System.Linq;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;

namespace App.BL
{
    /// <summary>
    /// Server-side transform for the "Child Unit Pivot Columns" projection
    /// (EmAppTransactionGridDisplayType.ChildUnitPivotColumns).
    ///
    /// Projects a GRANDCHILD unit's nested rows onto the PARENT (child) grid as dynamic pivot
    /// column groups. The column domain comes from a SOURCE grid (matrix-like) resolved from the
    /// pivot-column field's MatrixForeignKeyFieldId. Data stays in its native nested structure
    /// (childRow.DictOneToManyFields[grandchildUnitId]) so the normal master-detail save persists it.
    ///
    /// ALL conversion logic lives here (not in the UI) so it is reusable across front-ends.
    /// This is a separate route from Matrix (GenerateMatrix) and single-unit Pivot.
    /// </summary>
    public static class AppChildPivotProjectionBL
    {
        private class ProjectionContext
        {
            public AppTransactionUnitExDto HostUnit;
            public AppTransactionUnitExDto GrandchildUnit;
            public string GrandchildUnitId;
            public string ColumnKeyFieldName;     // grandchild field storing the column value
            public string ColumnSourceFieldName;  // source-grid field whose values are the columns
            public int? ColumnSourceFieldId;      // source-grid field id (for UI display-text lookup)
            public int? ColumnSourceUnitId;
            public List<AppTransactionFieldExDto> ValueFields;
        }

        public static ChildPivotProjectionModelDto ConvertGrandChildDataToPivotColumns(AppMasterDetailDto formData, int hostUnitId)
        {
            var model = new ChildPivotProjectionModelDto { IsConfigured = false };
            if (formData == null) return model;

            AppTransactionExDto tx = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(formData.TransactionId);
            ProjectionContext ctx = ResolveContext(tx, hostUnitId);
            if (ctx == null) return model;

            List<AppChildDataDto> childRows = GetRows(formData.DictOneToManyFields, ctx.HostUnit.Id.ToString());
            List<AppChildDataDto> sourceRows = ctx.ColumnSourceUnitId.HasValue
                ? GetRows(formData.DictOneToManyFields, ctx.ColumnSourceUnitId.Value.ToString())
                : new List<AppChildDataDto>();

            List<ProjColumnDto> hostColumns = BuildHostColumns(ctx.HostUnit);
            List<ProjColumnGroupDto> columnGroups = BuildColumnGroups(ctx, sourceRows);
            List<Dictionary<string, object>> wideRows = BuildWideRows(childRows, ctx, hostColumns, columnGroups);

            model.IsConfigured = true;
            model.HostColumns = hostColumns;
            model.ColumnGroups = columnGroups;
            model.WideRows = wideRows;
            model.ColumnKeyFieldName = ctx.ColumnKeyFieldName;
            model.ColumnSourceFieldName = ctx.ColumnSourceFieldName;
            model.ColumnSourceFieldId = ctx.ColumnSourceFieldId;
            model.ColumnSourceUnitId = ctx.ColumnSourceUnitId;
            model.GrandchildUnitId = ToNullableInt(ctx.GrandchildUnitId);
            model.ChildRowCount = childRows.Count;
            model.SourceRowCount = sourceRows.Count;
            return model;
        }

        public static AppMasterDetailDto ConvertBackPivotColumnsToGrandChildData(AppMasterDetailDto formData, int hostUnitId, List<Dictionary<string, object>> wideRows)
        {
            if (formData == null) return null;

            AppTransactionExDto tx = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(formData.TransactionId);
            ProjectionContext ctx = ResolveContext(tx, hostUnitId);
            if (ctx == null) return formData;

            List<AppChildDataDto> childRows = GetRows(formData.DictOneToManyFields, ctx.HostUnit.Id.ToString());
            List<AppChildDataDto> sourceRows = ctx.ColumnSourceUnitId.HasValue
                ? GetRows(formData.DictOneToManyFields, ctx.ColumnSourceUnitId.Value.ToString())
                : new List<AppChildDataDto>();

            List<ProjColumnDto> hostColumns = BuildHostColumns(ctx.HostUnit);
            List<ProjColumnGroupDto> columnGroups = BuildColumnGroups(ctx, sourceRows);

            FoldWideRows(childRows, wideRows ?? new List<Dictionary<string, object>>(), ctx, hostColumns, columnGroups);
            return formData;
        }

        // ---------------------------------------------------------------------

        private static ProjectionContext ResolveContext(AppTransactionExDto tx, int hostUnitId)
        {
            if (tx == null || tx.DictAllTransactionUnitIdExDto == null) return null;
            if (!tx.DictAllTransactionUnitIdExDto.TryGetValue(hostUnitId.ToString(), out AppTransactionUnitExDto hostUnit) || hostUnit == null)
                return null;

            AppTransactionUnitExDto grandchild = tx.DictAllTransactionUnitIdExDto.Values.FirstOrDefault(u =>
                u != null
                && u.ParentTransactionUnitId.HasValue && u.ParentTransactionUnitId.Value == hostUnitId
                && u.EmGridViewDisplayType.HasValue
                && u.EmGridViewDisplayType.Value == (int)EmAppTransactionGridDisplayType.ChildUnitPivotColumns);
            if (grandchild == null) return null;

            AppTransactionFieldExDto columnKeyField = grandchild.AppTransactionFieldList
                .FirstOrDefault(f => f.IsPivotColumn.HasValue && f.IsPivotColumn.Value && f.MatrixForeignKeyFieldId.HasValue);

            string sourceFieldName = null;
            int? sourceUnitId = null;
            if (columnKeyField != null && columnKeyField.MatrixForeignKeyFieldId.HasValue
                && tx.DictAllTransactionField != null
                && tx.DictAllTransactionField.TryGetValue(columnKeyField.MatrixForeignKeyFieldId.Value, out AppTransactionFieldExDto sourceField)
                && sourceField != null)
            {
                sourceFieldName = sourceField.DataBaseFieldName;
                sourceUnitId = sourceField.TransactionUnitId;
            }

            List<AppTransactionFieldExDto> valueFields = grandchild.AppTransactionFieldList
                .Where(f => (f.IsPivotValue.HasValue && f.IsPivotValue.Value) || f.IsPrimaryKey || f.IsLinkToParentPrimaryKey)
                .ToList();

            return new ProjectionContext
            {
                HostUnit = hostUnit,
                GrandchildUnit = grandchild,
                GrandchildUnitId = grandchild.Id.ToString(),
                ColumnKeyFieldName = columnKeyField?.DataBaseFieldName,
                ColumnSourceFieldName = sourceFieldName,
                ColumnSourceFieldId = columnKeyField?.MatrixForeignKeyFieldId,
                ColumnSourceUnitId = sourceUnitId,
                ValueFields = valueFields,
            };
        }

        private static List<ProjColumnDto> BuildHostColumns(AppTransactionUnitExDto hostUnit)
        {
            return hostUnit.AppTransactionFieldList
                .Where(f => !string.IsNullOrWhiteSpace(f.DataBaseFieldName) && IsVisible(f.IsVisible))
                .OrderBy(f => f.SortOrder ?? 0)
                .Select(f => new ProjColumnDto
                {
                    Header = string.IsNullOrWhiteSpace(f.DisplayName) ? f.DataBaseFieldName : f.DisplayName,
                    Binding = f.DataBaseFieldName,
                    FieldId = ToNullableInt(f.Id),
                    ControlType = f.ControlType,
                    IsReadOnly = (f.IsReadonly.HasValue && f.IsReadonly.Value) || f.IsPrimaryKey,
                    Visible = true,
                })
                .ToList();
        }

        private static List<ProjColumnGroupDto> BuildColumnGroups(ProjectionContext ctx, List<AppChildDataDto> sourceRows)
        {
            var groups = new List<ProjColumnGroupDto>();
            if (string.IsNullOrWhiteSpace(ctx.ColumnSourceFieldName)) return groups;

            var seen = new HashSet<string>();
            foreach (var sr in sourceRows)
            {
                if (sr?.DictOneToOneFields == null) continue;
                if (!sr.DictOneToOneFields.TryGetValue(ctx.ColumnSourceFieldName, out object colValue)) continue;
                if (colValue == null) continue;
                string comboId = colValue.ToString();
                if (comboId.Length == 0 || seen.Contains(comboId)) continue;
                seen.Add(comboId);

                var columns = ctx.ValueFields.Select(vf => new ProjLeafColumnDto
                {
                    Header = string.IsNullOrWhiteSpace(vf.DisplayName) ? vf.DataBaseFieldName : vf.DisplayName,
                    Binding = "pv_" + comboId + "_" + vf.DataBaseFieldName,
                    ComboId = comboId,
                    DataBaseFieldName = vf.DataBaseFieldName,
                    FieldId = ToNullableInt(vf.Id),
                    ControlType = vf.ControlType,
                    Visible = IsVisible(vf.IsVisible),
                }).ToList();

                groups.Add(new ProjColumnGroupDto
                {
                    Header = comboId,
                    ComboId = comboId,
                    ColValue = colValue,
                    Columns = columns,
                });
            }
            return groups;
        }

        private static List<Dictionary<string, object>> BuildWideRows(
            List<AppChildDataDto> childRows,
            ProjectionContext ctx,
            List<ProjColumnDto> hostColumns,
            List<ProjColumnGroupDto> columnGroups)
        {
            var wideRows = new List<Dictionary<string, object>>();
            int rowIndex = 0;
            foreach (var cr in childRows)
            {
                var wide = new Dictionary<string, object>();
                wide["__rowIndex"] = rowIndex++;

                var cd = cr.DictOneToOneFields ?? new Dictionary<string, object>();
                foreach (var hc in hostColumns)
                    wide[hc.Binding] = cd.TryGetValue(hc.Binding, out object hv) ? hv : null;

                List<AppChildDataDto> gcRows = GetGrandchildRows(cr, ctx.GrandchildUnitId);
                var byCol = new Dictionary<string, AppChildDataDto>();
                foreach (var gc in gcRows)
                {
                    string key = GetColKey(gc, ctx.ColumnKeyFieldName);
                    if (key != null && !byCol.ContainsKey(key)) byCol[key] = gc;
                }

                foreach (var g in columnGroups)
                {
                    byCol.TryGetValue(g.ComboId, out AppChildDataDto gc);
                    var gcd = gc?.DictOneToOneFields;
                    foreach (var leaf in g.Columns)
                        wide[leaf.Binding] = (gcd != null && gcd.TryGetValue(leaf.DataBaseFieldName, out object v)) ? v : null;
                }

                wideRows.Add(wide);
            }
            return wideRows;
        }

        private static void FoldWideRows(
            List<AppChildDataDto> childRows,
            List<Dictionary<string, object>> wideRows,
            ProjectionContext ctx,
            List<ProjColumnDto> hostColumns,
            List<ProjColumnGroupDto> columnGroups)
        {
            for (int i = 0; i < wideRows.Count; i++)
            {
                var wide = wideRows[i];
                int childIndex = ResolveRowIndex(wide, i);
                if (childIndex < 0 || childIndex >= childRows.Count) continue;
                var cr = childRows[childIndex];

                cr.DictOneToOneFields = cr.DictOneToOneFields ?? new Dictionary<string, object>();
                foreach (var hc in hostColumns)
                {
                    if (hc.IsReadOnly) continue;
                    cr.DictOneToOneFields[hc.Binding] = wide.TryGetValue(hc.Binding, out object hv) ? hv : null;
                }

                List<AppChildDataDto> gcRows = GetGrandchildRows(cr, ctx.GrandchildUnitId);
                var byCol = new Dictionary<string, AppChildDataDto>();
                foreach (var gc in gcRows)
                {
                    string key = GetColKey(gc, ctx.ColumnKeyFieldName);
                    if (key != null && !byCol.ContainsKey(key)) byCol[key] = gc;
                }

                foreach (var g in columnGroups)
                {
                    var vals = new Dictionary<string, object>();
                    bool hasValue = false;
                    foreach (var leaf in g.Columns)
                    {
                        object v = wide.TryGetValue(leaf.Binding, out object lv) ? lv : null;
                        vals[leaf.DataBaseFieldName] = v;
                        if (v != null && !(v is string s && s.Length == 0)) hasValue = true;
                    }

                    if (byCol.TryGetValue(g.ComboId, out AppChildDataDto existing))
                    {
                        existing.DictOneToOneFields = existing.DictOneToOneFields ?? new Dictionary<string, object>();
                        foreach (var kv in vals) existing.DictOneToOneFields[kv.Key] = kv.Value;
                        existing.DictOneToOneFields[ctx.ColumnKeyFieldName] = g.ColValue;
                        existing.IsDirty = true;
                    }
                    else if (hasValue)
                    {
                        var blank = BuildBlankGrandchildRow(ctx.GrandchildUnit);
                        foreach (var kv in vals) blank.DictOneToOneFields[kv.Key] = kv.Value;
                        blank.DictOneToOneFields[ctx.ColumnKeyFieldName] = g.ColValue;
                        blank.IsDirty = true;
                        blank.IsNew = true;
                        gcRows.Add(blank);
                        byCol[g.ComboId] = blank;
                    }
                }

                EnsureGrandchildCollection(cr, ctx.GrandchildUnitId, gcRows);
                cr.IsDirty = true;
            }
        }

        // ---------------------------------------------------------------------

        private static AppChildDataDto BuildBlankGrandchildRow(AppTransactionUnitExDto grandchildUnit)
        {
            var row = new AppChildDataDto
            {
                DictOneToOneFields = new Dictionary<string, object>(),
                DictOneToManyFields = new Dictionary<string, List<AppChildDataDto>>(),
            };
            foreach (var f in grandchildUnit.AppTransactionFieldList)
            {
                if (string.IsNullOrWhiteSpace(f.DataBaseFieldName)) continue;
                object def = string.IsNullOrEmpty(f.DefaultValue) ? null : f.DefaultValue;
                row.DictOneToOneFields[f.DataBaseFieldName] = def;
            }
            return row;
        }

        private static List<AppChildDataDto> GetRows(Dictionary<string, List<AppChildDataDto>> dict, string unitId)
        {
            if (dict != null && dict.TryGetValue(unitId, out List<AppChildDataDto> rows) && rows != null) return rows;
            return new List<AppChildDataDto>();
        }

        private static List<AppChildDataDto> GetGrandchildRows(AppChildDataDto childRow, string grandchildUnitId)
        {
            if (childRow.DictOneToManyFields != null
                && childRow.DictOneToManyFields.TryGetValue(grandchildUnitId, out List<AppChildDataDto> rows)
                && rows != null)
                return rows;
            return new List<AppChildDataDto>();
        }

        private static void EnsureGrandchildCollection(AppChildDataDto childRow, string grandchildUnitId, List<AppChildDataDto> rows)
        {
            childRow.DictOneToManyFields = childRow.DictOneToManyFields ?? new Dictionary<string, List<AppChildDataDto>>();
            childRow.DictOneToManyFields[grandchildUnitId] = rows;
        }

        private static string GetColKey(AppChildDataDto gc, string columnKeyFieldName)
        {
            if (gc?.DictOneToOneFields == null || string.IsNullOrEmpty(columnKeyFieldName)) return null;
            if (!gc.DictOneToOneFields.TryGetValue(columnKeyFieldName, out object v) || v == null) return null;
            return v.ToString();
        }

        private static int ResolveRowIndex(Dictionary<string, object> wide, int fallback)
        {
            if (wide != null && wide.TryGetValue("__rowIndex", out object idx) && idx != null)
            {
                try { return Convert.ToInt32(idx); }
                catch { return fallback; }
            }
            return fallback;
        }

        private static bool IsVisible(bool? isVisible)
        {
            return !isVisible.HasValue || isVisible.Value;
        }

        private static int? ToNullableInt(object value)
        {
            if (value == null) return null;
            try { return Convert.ToInt32(value); }
            catch { return null; }
        }
    }
}
