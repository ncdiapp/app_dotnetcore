using APP.Components.EntityDto;
using APP.TechPack.Services;

namespace APP.BL.Tests.POM;

public class GradingDisplayConvertServiceTests
{
    // SizeRunSizeIds: XS=234 S=235 M=237 L=238 XL=239 XXL=240
    private const int BaseSizeM = 237;
    private const string PomHostUnitId = "100";
    private const string GradeGcUnitId = "200";
    private const string SizeSourceUnitId = "300";

    [Fact]
    public void ConvertDeltasToSizeValues_WhenBaseSizeHiddenByDimensions_StillConvertsFullRun()
    {
        // UI ColumnGroups only show Extra Large (L/XL/XXL); Base Size M is under Regular and hidden.
        var form = BuildForm(
            visibleSizeIds: [238, 239, 240],
            allSizeIds: [234, 235, 237, 238, 239, 240],
            baseValue: 20m,
            // Adjacent deltas for XS S M L XL XXL with base at M
            deltasBySize: new Dictionary<int, decimal>
            {
                [234] = -1m,
                [235] = -1m,
                [237] = 0m,
                [238] = 1.5m,
                [239] = 1.5m,
                [240] = 1.5m,
            });

        GradingDisplayConvertService.ConvertDeltasToSizeValues(form);

        Assert.Equal(
            GradingDisplayConvertService.ModeSizeValue,
            form.DictOneToOneFields![GradingDisplayConvertService.ModeFieldName]?.ToString());

        var gradeBySize = IndexGradeRows(form);
        Assert.Equal(18m, ToDec(gradeBySize[234]));   // XS
        Assert.Equal(19m, ToDec(gradeBySize[235]));   // S
        Assert.Equal(20m, ToDec(gradeBySize[237]));   // M (hidden in UI, still converted)
        Assert.Equal(21.5m, ToDec(gradeBySize[238])); // L
        Assert.Equal(23m, ToDec(gradeBySize[239]));   // XL
        Assert.Equal(24.5m, ToDec(gradeBySize[240])); // XXL

        // Visible wide cells updated; hidden sizes remain in nested grade data only.
        var wide = form.DictHostUnitIdChildPivotProjection![PomHostUnitId].WideRows![0];
        Assert.Equal(21.5m, ToDec(wide["pv_238_GradingDelta"]));
        Assert.Equal(23m, ToDec(wide["pv_239_GradingDelta"]));
        Assert.Equal(24.5m, ToDec(wide["pv_240_GradingDelta"]));
        Assert.False(wide.ContainsKey("pv_237_GradingDelta"));
    }

    [Fact]
    public void ConvertDeltasToSizeValues_UsesSiblingDiffDisplayMode_AndIgnoresLegacySizeValueFlag()
    {
        // StyleSpec sibling holds DiffDisplayMode=DELTA; root still has stale GradingDisplayMode=SIZEVALUE
        // from an older build — must not early-return.
        var form = BuildForm(
            visibleSizeIds: [237, 238],
            allSizeIds: [237, 238],
            baseValue: 10m,
            deltasBySize: new Dictionary<int, decimal> { [237] = 0m, [238] = 1m });

        form.DictOneToOneFields!.Remove(GradingDisplayConvertService.ModeFieldName);
        form.DictOneToOneFields[GradingDisplayConvertService.LegacyModeFieldName] =
            GradingDisplayConvertService.ModeSizeValue;
        form.DictSiblingOneToOneFields = new Dictionary<string, Dictionary<string, object>>
        {
            ["900"] = new Dictionary<string, object>
            {
                ["BaseSizeDetailId"] = BaseSizeM,
                [GradingDisplayConvertService.ModeFieldName] = GradingDisplayConvertService.ModeDelta,
            },
        };
        form.DictOneToOneFields.Remove("BaseSizeDetailId");

        GradingDisplayConvertService.ConvertDeltasToSizeValues(form);

        Assert.Equal(
            GradingDisplayConvertService.ModeSizeValue,
            form.DictSiblingOneToOneFields["900"][GradingDisplayConvertService.ModeFieldName]?.ToString());
        Assert.False(form.DictOneToOneFields.ContainsKey(GradingDisplayConvertService.LegacyModeFieldName));

        var gradeBySize = IndexGradeRows(form);
        Assert.Equal(10m, ToDec(gradeBySize[237]));
        Assert.Equal(11m, ToDec(gradeBySize[238]));
    }

    [Fact]
    public void ConvertDeltasToSizeValues_WhenSourceMissing_FallsBackToGradeRowsIncludingHiddenBase()
    {
        var form = BuildForm(
            visibleSizeIds: [238, 239, 240],
            allSizeIds: [234, 235, 237, 238, 239, 240],
            baseValue: 20m,
            deltasBySize: new Dictionary<int, decimal>
            {
                [234] = -1m,
                [235] = -1m,
                [237] = 0m,
                [238] = 1.5m,
                [239] = 1.5m,
                [240] = 1.5m,
            });

        // Simulate an older projection payload without ColumnSourceUnitId.
        var model = form.DictHostUnitIdChildPivotProjection![PomHostUnitId];
        model.ColumnSourceUnitId = null;
        model.ColumnSourceFieldName = null;

        GradingDisplayConvertService.ConvertDeltasToSizeValues(form);

        var gradeBySize = IndexGradeRows(form);
        Assert.Equal(20m, ToDec(gradeBySize[237]));
        Assert.Equal(21.5m, ToDec(gradeBySize[238]));
    }

    private static AppMasterDetailDto BuildForm(
        IReadOnlyList<int> visibleSizeIds,
        IReadOnlyList<int> allSizeIds,
        decimal baseValue,
        Dictionary<int, decimal> deltasBySize)
    {
        var gradeRows = allSizeIds.Select(id => new AppChildDataDto
        {
            DictOneToOneFields = new Dictionary<string, object>
            {
                ["SizeRunSizeId"] = id,
                ["GradingDelta"] = deltasBySize[id],
            },
        }).ToList();

        var pomRow = new AppChildDataDto
        {
            DictOneToOneFields = new Dictionary<string, object>
            {
                ["PomSpecLineId"] = 1,
                ["BaseValue"] = baseValue,
                ["IsFixed"] = false,
            },
            DictOneToManyFields = new Dictionary<string, List<AppChildDataDto>>
            {
                [GradeGcUnitId] = gradeRows,
            },
        };

        var sizeSourceRows = allSizeIds.Select(id => new AppChildDataDto
        {
            DictOneToOneFields = new Dictionary<string, object>
            {
                ["SizeRunSizeId"] = id,
                // Dimensions visibility flag — only Extra Large sizes checked in the UI scenario.
                ["IsVisibleInSpec"] = visibleSizeIds.Contains(id),
            },
        }).ToList();

        var columnGroups = visibleSizeIds.Select(id => new ProjColumnGroupDto
        {
            Header = id.ToString(),
            ComboId = id.ToString(),
            ColValue = id,
            Columns =
            [
                new ProjLeafColumnDto
                {
                    Header = "GradingDelta",
                    Binding = $"pv_{id}_GradingDelta",
                    ComboId = id.ToString(),
                    DataBaseFieldName = "GradingDelta",
                    Visible = true,
                },
            ],
        }).ToList();

        var wide = new Dictionary<string, object>
        {
            ["__rowIndex"] = 0,
            ["BaseValue"] = baseValue,
        };
        foreach (var id in visibleSizeIds)
            wide[$"pv_{id}_GradingDelta"] = deltasBySize[id];

        return new AppMasterDetailDto
        {
            DictOneToOneFields = new Dictionary<string, object>
            {
                ["BaseSizeDetailId"] = BaseSizeM,
                [GradingDisplayConvertService.ModeFieldName] = GradingDisplayConvertService.ModeDelta,
            },
            DictOneToManyFields = new Dictionary<string, List<AppChildDataDto>>
            {
                [PomHostUnitId] = [pomRow],
                [SizeSourceUnitId] = sizeSourceRows,
            },
            DictHostUnitIdChildPivotProjection = new Dictionary<string, ChildPivotProjectionModelDto>
            {
                [PomHostUnitId] = new ChildPivotProjectionModelDto
                {
                    IsConfigured = true,
                    GrandchildUnitId = int.Parse(GradeGcUnitId),
                    ColumnKeyFieldName = "SizeRunSizeId",
                    ColumnSourceFieldName = "SizeRunSizeId",
                    ColumnSourceUnitId = int.Parse(SizeSourceUnitId),
                    ColumnSourceVisibleFieldName = "IsVisibleInSpec",
                    ColumnGroups = columnGroups,
                    WideRows = [wide],
                },
            },
        };
    }

    private static Dictionary<int, object?> IndexGradeRows(AppMasterDetailDto form)
    {
        var pom = form.DictOneToManyFields![PomHostUnitId][0];
        var grades = pom.DictOneToManyFields![GradeGcUnitId];
        var map = new Dictionary<int, object?>();
        foreach (var g in grades)
        {
            int id = Convert.ToInt32(g.DictOneToOneFields!["SizeRunSizeId"]);
            map[id] = g.DictOneToOneFields["GradingDelta"];
        }
        return map;
    }

    private static decimal ToDec(object? value) => Convert.ToDecimal(value);
}
