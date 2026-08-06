using System;
using System.Collections.Generic;
using System.Globalization;
using APP.Components.EntityDto;
using APP.TechPack.Engine;

namespace APP.TechPack.Services;

/// <summary>
/// In-memory display conversion between adjacent-step deltas and absolute size values.
/// DB always stores deltas in TchpGradeValue.GradingDelta; this service only mutates formData
/// for UI mode switching (GradingDisplayMode = DELTA | SIZEVALUE).
/// </summary>
public static class GradingDisplayConvertService
{
    public const string ModeFieldName = "GradingDisplayMode";
    public const string ModeDelta = "DELTA";
    public const string ModeSizeValue = "SIZEVALUE";

    private static readonly IGradingEngine Engine = new GradingEngine();

    /// <summary>
    /// Assumes current GradingDelta cells hold deltas; replaces them with absolute size values
    /// and sets GradingDisplayMode = SIZEVALUE.
    /// </summary>
    public static void ConvertDeltasToSizeValues(AppMasterDetailDto formData)
    {
        if (formData == null)
            throw new ArgumentNullException(nameof(formData));

        if (string.Equals(GetMode(formData), ModeSizeValue, StringComparison.OrdinalIgnoreCase))
            return;

        int baseSizeDetailId = RequireBaseSizeDetailId(formData);
        ConvertAllHostUnits(formData, baseSizeDetailId, toSizeValues: true);
        SetMode(formData, ModeSizeValue);
        RefreshProjectionToken(formData);
    }

    /// <summary>
    /// Assumes current GradingDelta cells hold absolute size values; replaces them with deltas,
    /// syncs PomSpecLine.BaseValue from the base-size cell, and sets GradingDisplayMode = DELTA.
    /// </summary>
    public static void ConvertSizeValuesToDeltas(AppMasterDetailDto formData)
    {
        if (formData == null)
            throw new ArgumentNullException(nameof(formData));

        string mode = GetMode(formData);
        if (string.IsNullOrWhiteSpace(mode)
            || string.Equals(mode, ModeDelta, StringComparison.OrdinalIgnoreCase))
            return;

        int baseSizeDetailId = RequireBaseSizeDetailId(formData);
        ConvertAllHostUnits(formData, baseSizeDetailId, toSizeValues: false);
        SetMode(formData, ModeDelta);
        RefreshProjectionToken(formData);
    }

    // ── Core ────────────────────────────────────────────────────────────────

    private static void ConvertAllHostUnits(
        AppMasterDetailDto formData, int baseSizeDetailId, bool toSizeValues)
    {
        var hosts = ResolveHostUnits(formData);
        if (hosts.Count == 0)
            throw new InvalidOperationException(
                "No POM / GradeValue host unit found in formData to convert.");

        foreach (var host in hosts)
        {
            var childRows = GetChildRows(formData, host.HostUnitId);
            if (childRows == null || childRows.Count == 0)
                continue;

            IReadOnlyList<string> sizeKeys = host.SizeKeysOrdered;
            int baseSizeIndex = sizeKeys.Count == 0
                ? -1
                : IndexOfSizeKey(sizeKeys, baseSizeDetailId);
            if (sizeKeys.Count > 0 && baseSizeIndex < 0)
                throw new InvalidOperationException(
                    $"BaseSizeDetailId {baseSizeDetailId} not found in active size columns.");

            for (int rowIndex = 0; rowIndex < childRows.Count; rowIndex++)
            {
                var child = childRows[rowIndex];
                if (child?.DictOneToOneFields == null)
                    continue;

                ConvertOnePomLine(
                    child, host, sizeKeys, baseSizeIndex, toSizeValues);

                SyncWideRow(formData, host.HostUnitId, rowIndex, child, host);
            }
        }
    }

    private static void ConvertOnePomLine(
        AppChildDataDto child,
        HostUnitContext host,
        IReadOnlyList<string> sizeKeysOrdered,
        int baseSizeIndex,
        bool toSizeValues)
    {
        bool isFixed = IsTruthy(GetField(child.DictOneToOneFields, "IsFixed"));
        decimal? baseValue = ToNullableDecimal(GetField(child.DictOneToOneFields, "BaseValue"));

        var gradeRows = EnsureGradeRowList(child, host.GrandchildUnitId);
        var bySize = IndexGradeRowsBySizeKey(gradeRows, host.ColumnKeyFieldName);

        List<string> sizeKeys = sizeKeysOrdered.Count > 0
            ? new List<string>(sizeKeysOrdered)
            : new List<string>(bySize.Keys);

        if (sizeKeys.Count == 0)
            return;

        if (sizeKeysOrdered.Count == 0
            && (baseSizeIndex < 0 || baseSizeIndex >= sizeKeys.Count))
            baseSizeIndex = 0;

        // Ensure a grade cell exists for every projected size column.
        foreach (var sizeKey in sizeKeys)
        {
            if (bySize.ContainsKey(sizeKey))
                continue;
            var blank = new AppChildDataDto
            {
                DictOneToOneFields = new Dictionary<string, object>
                {
                    [host.ColumnKeyFieldName] = ParseSizeKeyValue(sizeKey),
                    ["GradingDelta"] = 0m,
                },
                IsNew = true,
                IsDirty = true,
            };
            if (child.DictOneToOneFields.TryGetValue("PomSpecLineId", out var pslId) && pslId != null)
                blank.DictOneToOneFields["PomSpecLineId"] = pslId;

            gradeRows.Add(blank);
            bySize[sizeKey] = blank;
        }

        if (baseSizeIndex < 0 || baseSizeIndex >= sizeKeys.Count)
            throw new InvalidOperationException(
                "Cannot resolve base size index for grading display conversion.");

        if (isFixed)
        {
            ConvertFixedLine(child, bySize, sizeKeys, baseSizeIndex, baseValue, toSizeValues);
            return;
        }

        if (toSizeValues)
        {
            if (!baseValue.HasValue || baseValue.Value <= 0)
                throw new InvalidOperationException(
                    "BaseValue must be a positive number before converting deltas to size values.");

            var deltas = new decimal[sizeKeys.Count];
            for (int i = 0; i < sizeKeys.Count; i++)
                deltas[i] = ToDecimal(GetField(bySize[sizeKeys[i]].DictOneToOneFields, "GradingDelta"));

            var values = Engine.ComputeSizeValues(baseValue.Value, baseSizeIndex, deltas);
            WriteGradeNumbers(bySize, sizeKeys, values);
        }
        else
        {
            var values = new decimal[sizeKeys.Count];
            for (int i = 0; i < sizeKeys.Count; i++)
                values[i] = ToDecimal(GetField(bySize[sizeKeys[i]].DictOneToOneFields, "GradingDelta"));

            // Keep BaseValue aligned with the absolute measurement at the base size column.
            child.DictOneToOneFields["BaseValue"] = values[baseSizeIndex];
            child.IsDirty = true;

            var deltas = Engine.ComputeGradingDeltas(values, baseSizeIndex);
            WriteGradeNumbers(bySize, sizeKeys, deltas);
        }
    }

    private static void ConvertFixedLine(
        AppChildDataDto child,
        Dictionary<string, AppChildDataDto> bySize,
        IReadOnlyList<string> sizeKeys,
        int baseSizeIndex,
        decimal? baseValue,
        bool toSizeValues)
    {
        if (toSizeValues)
        {
            decimal abs = baseValue ?? 0m;
            foreach (var sizeKey in sizeKeys)
            {
                var row = bySize[sizeKey];
                row.DictOneToOneFields ??= new Dictionary<string, object>();
                row.DictOneToOneFields["GradingDelta"] = abs;
                row.IsDirty = true;
            }
            return;
        }

        // Values → Delta: all sizes equal BaseValue; store deltas as 0.
        // Prefer the absolute value currently shown in the base-size column.
        decimal newBase = ToDecimal(GetField(bySize[sizeKeys[baseSizeIndex]].DictOneToOneFields, "GradingDelta"));
        if (newBase == 0m && baseValue.HasValue)
            newBase = baseValue.Value;

        child.DictOneToOneFields["BaseValue"] = newBase;
        child.IsDirty = true;

        foreach (var sizeKey in sizeKeys)
        {
            var row = bySize[sizeKey];
            row.DictOneToOneFields ??= new Dictionary<string, object>();
            row.DictOneToOneFields["GradingDelta"] = 0m;
            row.IsDirty = true;
        }
    }

    private static void WriteGradeNumbers(
        Dictionary<string, AppChildDataDto> bySize,
        IReadOnlyList<string> sizeKeys,
        IReadOnlyList<decimal> numbers)
    {
        for (int i = 0; i < sizeKeys.Count; i++)
        {
            var row = bySize[sizeKeys[i]];
            row.DictOneToOneFields ??= new Dictionary<string, object>();
            row.DictOneToOneFields["GradingDelta"] = numbers[i];
            row.IsDirty = true;
        }
    }

    // ── Host / size discovery ────────────────────────────────────────────────

    private sealed class HostUnitContext
    {
        public string HostUnitId { get; init; } = "";
        public string GrandchildUnitId { get; init; } = "";
        public string ColumnKeyFieldName { get; init; } = "SizeRunSizeId";
        public List<string> SizeKeysOrdered { get; init; } = new();
    }

    private static List<HostUnitContext> ResolveHostUnits(AppMasterDetailDto formData)
    {
        var hosts = new List<HostUnitContext>();

        if (formData.DictHostUnitIdChildPivotProjection != null)
        {
            foreach (var kvp in formData.DictHostUnitIdChildPivotProjection)
            {
                var model = kvp.Value;
                if (model == null || !model.IsConfigured || model.GrandchildUnitId == null)
                    continue;

                var sizeKeys = new List<string>();
                if (model.ColumnGroups != null)
                {
                    foreach (var g in model.ColumnGroups)
                    {
                        if (!string.IsNullOrEmpty(g?.ComboId))
                            sizeKeys.Add(g.ComboId);
                    }
                }

                hosts.Add(new HostUnitContext
                {
                    HostUnitId = kvp.Key,
                    GrandchildUnitId = model.GrandchildUnitId.Value.ToString(CultureInfo.InvariantCulture),
                    ColumnKeyFieldName = string.IsNullOrWhiteSpace(model.ColumnKeyFieldName)
                        ? "SizeRunSizeId"
                        : model.ColumnKeyFieldName,
                    SizeKeysOrdered = sizeKeys,
                });
            }
        }

        if (hosts.Count > 0)
            return hosts;

        // Fallback: scan child units for BaseValue rows with nested GradingDelta grandchildren.
        if (formData.DictOneToManyFields == null)
            return hosts;

        foreach (var kvp in formData.DictOneToManyFields)
        {
            var rows = kvp.Value;
            if (rows == null || rows.Count == 0)
                continue;
            var sample = rows[0];
            if (sample?.DictOneToOneFields == null
                || !sample.DictOneToOneFields.ContainsKey("BaseValue"))
                continue;

            string? gcUnitId = null;
            string columnKey = "SizeRunSizeId";
            var sizeKeys = new List<string>();

            if (sample.DictOneToManyFields != null)
            {
                foreach (var gcKvp in sample.DictOneToManyFields)
                {
                    var gcRows = gcKvp.Value;
                    if (gcRows == null || gcRows.Count == 0)
                        continue;
                    var gc0 = gcRows[0];
                    if (gc0?.DictOneToOneFields == null
                        || !gc0.DictOneToOneFields.ContainsKey("GradingDelta"))
                        continue;

                    gcUnitId = gcKvp.Key;
                    if (gc0.DictOneToOneFields.ContainsKey("SizeRunSizeId"))
                        columnKey = "SizeRunSizeId";

                    foreach (var gc in gcRows)
                    {
                        var keyObj = GetField(gc?.DictOneToOneFields, columnKey);
                        if (keyObj == null)
                            continue;
                        string key = keyObj.ToString() ?? "";
                        if (key.Length > 0 && !sizeKeys.Contains(key))
                            sizeKeys.Add(key);
                    }
                    break;
                }
            }

            if (gcUnitId == null)
                continue;

            hosts.Add(new HostUnitContext
            {
                HostUnitId = kvp.Key,
                GrandchildUnitId = gcUnitId,
                ColumnKeyFieldName = columnKey,
                SizeKeysOrdered = sizeKeys,
            });
        }

        return hosts;
    }

    // ── Projection wide-row sync ─────────────────────────────────────────────

    private static void SyncWideRow(
        AppMasterDetailDto formData,
        string hostUnitId,
        int rowIndex,
        AppChildDataDto child,
        HostUnitContext host)
    {
        var projections = formData.DictHostUnitIdChildPivotProjection;
        if (projections == null
            || !projections.TryGetValue(hostUnitId, out var model)
            || model?.WideRows == null
            || rowIndex < 0
            || rowIndex >= model.WideRows.Count)
            return;

        var wide = model.WideRows[rowIndex];
        if (wide == null)
            return;

        // Host BaseValue may have changed (Values → Delta).
        if (child.DictOneToOneFields != null
            && child.DictOneToOneFields.TryGetValue("BaseValue", out var bv))
            wide["BaseValue"] = bv;

        if (model.ColumnGroups == null)
            return;

        var gradeRows = EnsureGradeRowList(child, host.GrandchildUnitId);
        var bySize = IndexGradeRowsBySizeKey(gradeRows, host.ColumnKeyFieldName);

        foreach (var g in model.ColumnGroups)
        {
            if (g?.Columns == null || string.IsNullOrEmpty(g.ComboId))
                continue;
            if (!bySize.TryGetValue(g.ComboId, out var gc))
                continue;

            foreach (var leaf in g.Columns)
            {
                if (leaf == null || string.IsNullOrEmpty(leaf.Binding))
                    continue;
                if (!string.Equals(leaf.DataBaseFieldName, "GradingDelta", StringComparison.OrdinalIgnoreCase))
                    continue;
                wide[leaf.Binding] = GetField(gc.DictOneToOneFields, "GradingDelta");
            }
        }
    }

    private static void RefreshProjectionToken(AppMasterDetailDto formData)
    {
        if (formData.DictHostUnitIdChildPivotProjection != null
            && formData.DictHostUnitIdChildPivotProjection.Count > 0)
        {
            formData.ChildPivotProjectionLoadToken = Guid.NewGuid().ToString("N");
        }
    }

    // ── Mode / root fields ───────────────────────────────────────────────────

    private static string? GetMode(AppMasterDetailDto formData)
    {
        formData.DictOneToOneFields ??= new Dictionary<string, object>();
        if (formData.DictOneToOneFields.TryGetValue(ModeFieldName, out var v) && v != null)
            return v.ToString();
        return null;
    }

    private static void SetMode(AppMasterDetailDto formData, string mode)
    {
        formData.DictOneToOneFields ??= new Dictionary<string, object>();
        formData.DictOneToOneFields[ModeFieldName] = mode;
    }

    private static int RequireBaseSizeDetailId(AppMasterDetailDto formData)
    {
        object? raw = null;
        if (formData.DictOneToOneFields != null
            && formData.DictOneToOneFields.TryGetValue("BaseSizeDetailId", out var root)
            && root != null)
            raw = root;

        if (raw == null && formData.DictSiblingOneToOneFields != null)
        {
            foreach (var sibling in formData.DictSiblingOneToOneFields.Values)
            {
                if (sibling != null
                    && sibling.TryGetValue("BaseSizeDetailId", out var sv)
                    && sv != null)
                {
                    raw = sv;
                    break;
                }
            }
        }

        if (raw == null)
            throw new InvalidOperationException(
                "BaseSizeDetailId is required on the form root/sibling fields for grading display conversion.");

        return Convert.ToInt32(raw, CultureInfo.InvariantCulture);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static List<AppChildDataDto>? GetChildRows(AppMasterDetailDto formData, string hostUnitId)
    {
        if (formData.DictOneToManyFields == null)
            return null;
        return formData.DictOneToManyFields.TryGetValue(hostUnitId, out var rows) ? rows : null;
    }

    private static List<AppChildDataDto> EnsureGradeRowList(AppChildDataDto child, string grandchildUnitId)
    {
        child.DictOneToManyFields ??= new Dictionary<string, List<AppChildDataDto>>();
        if (!child.DictOneToManyFields.TryGetValue(grandchildUnitId, out var list) || list == null)
        {
            list = new List<AppChildDataDto>();
            child.DictOneToManyFields[grandchildUnitId] = list;
        }
        return list;
    }

    private static Dictionary<string, AppChildDataDto> IndexGradeRowsBySizeKey(
        List<AppChildDataDto> gradeRows, string columnKeyFieldName)
    {
        var bySize = new Dictionary<string, AppChildDataDto>(StringComparer.Ordinal);
        foreach (var gc in gradeRows)
        {
            if (gc?.DictOneToOneFields == null)
                continue;
            var keyObj = GetField(gc.DictOneToOneFields, columnKeyFieldName);
            if (keyObj == null)
                continue;
            string key = keyObj.ToString() ?? "";
            if (key.Length == 0 || bySize.ContainsKey(key))
                continue;
            bySize[key] = gc;
        }
        return bySize;
    }

    private static int IndexOfSizeKey(IReadOnlyList<string> sizeKeys, int sizeRunSizeId)
    {
        string target = sizeRunSizeId.ToString(CultureInfo.InvariantCulture);
        for (int i = 0; i < sizeKeys.Count; i++)
        {
            if (string.Equals(sizeKeys[i], target, StringComparison.Ordinal))
                return i;
        }
        return -1;
    }

    private static object ParseSizeKeyValue(string sizeKey)
    {
        if (int.TryParse(sizeKey, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
            return n;
        return sizeKey;
    }

    private static object? GetField(Dictionary<string, object>? dict, string name)
    {
        if (dict == null)
            return null;
        return dict.TryGetValue(name, out var v) ? v : null;
    }

    private static bool IsTruthy(object? value)
    {
        if (value == null || value is DBNull)
            return false;
        if (value is bool b)
            return b;
        if (value is byte bt)
            return bt != 0;
        if (value is short s)
            return s != 0;
        if (value is int i)
            return i != 0;
        if (value is long l)
            return l != 0;
        if (value is decimal d)
            return d != 0;
        string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        return text == "1"
            || text.Equals("true", StringComparison.OrdinalIgnoreCase)
            || text.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    private static decimal ToDecimal(object? value)
    {
        if (value == null || value is DBNull)
            return 0m;
        if (value is decimal d)
            return d;
        if (value is double dbl)
            return (decimal)dbl;
        if (value is float f)
            return (decimal)f;
        if (value is int i)
            return i;
        if (value is long l)
            return l;
        return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
    }

    private static decimal? ToNullableDecimal(object? value)
    {
        if (value == null || value is DBNull)
            return null;
        string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        if (string.IsNullOrWhiteSpace(text))
            return null;
        return ToDecimal(value);
    }
}
