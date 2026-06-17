using APP.TechPack.Engine;

namespace APP.BL.Tests.POM;

public class GradingEngineTests
{
    private readonly IGradingEngine _engine = new GradingEngine();

    // ── ComputeSizeValues ────────────────────────────────────────────────────

    [Fact]
    public void ComputeSizeValues_BaseOnly_ReturnsBaseValue()
    {
        var result = _engine.ComputeSizeValues(10m, 0, [0m]);
        Assert.Equal([10m], result);
    }

    [Fact]
    public void ComputeSizeValues_ForwardPass_ProducesCorrectValues()
    {
        // Sizes: 2T(0) 3T(1) 4T(2)  — base at index 1 (3T), baseValue=20
        // deltas: [-1.5, 0, +1.5]
        // expected: [18.5, 20, 21.5]
        var result = _engine.ComputeSizeValues(20m, 1, [-1.5m, 0m, 1.5m]);

        Assert.Equal(3, result.Count);
        Assert.Equal(18.5m, result[0]);
        Assert.Equal(20m,   result[1]);
        Assert.Equal(21.5m, result[2]);
    }

    [Fact]
    public void ComputeSizeValues_BaseAtStart_ExpandsUpward()
    {
        // base at index 0, each step +0.5
        var result = _engine.ComputeSizeValues(30m, 0, [0m, 0.5m, 0.5m, 0.5m]);

        Assert.Equal([30m, 30.5m, 31m, 31.5m], result);
    }

    [Fact]
    public void ComputeSizeValues_BaseAtEnd_ExpandsDownward()
    {
        // base at index 3, each step down -0.5 (delta[i] = value[i] - value[i+1] = -0.5)
        var result = _engine.ComputeSizeValues(30m, 3, [-0.5m, -0.5m, -0.5m, 0m]);

        Assert.Equal([28.5m, 29m, 29.5m, 30m], result);
    }

    [Fact]
    public void ComputeSizeValues_ThrowsWhenBaseValueNotPositive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _engine.ComputeSizeValues(0m, 0, [0m]));
    }

    [Fact]
    public void ComputeSizeValues_ThrowsWhenBaseSizeIndexOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _engine.ComputeSizeValues(10m, 5, [0m, 1m, 2m]));
    }

    // ── ComputeGradingDeltas ─────────────────────────────────────────────────

    [Fact]
    public void ComputeGradingDeltas_RoundTrip_ReproducesOriginalDeltas()
    {
        decimal[] originalDeltas = [-0.5m, -0.5m, 0m, 0.5m, 0.5m];
        var values = _engine.ComputeSizeValues(25m, 2, originalDeltas);
        var recovered = _engine.ComputeGradingDeltas(values, 2);

        Assert.Equal(originalDeltas.Length, recovered.Count);
        for (int i = 0; i < originalDeltas.Length; i++)
            Assert.Equal(originalDeltas[i], recovered[i]);
    }

    [Fact]
    public void ComputeGradingDeltas_DeltaAtBaseSizeIsAlwaysZero()
    {
        var result = _engine.ComputeGradingDeltas([18m, 20m, 22m, 24m], 1);
        Assert.Equal(0m, result[1]);
    }

    // ── ApplyGradeRuleSet ────────────────────────────────────────────────────

    [Fact]
    public void ApplyGradeRuleSet_ProducesCorrectDeltas()
    {
        var rules = new[] { new GradeRuleInput("BUST", PlusValue: 1.0m, MinuValue: 0.75m) };
        // 5 sizes, base at index 2
        var result = _engine.ApplyGradeRuleSet(rules, "BUST", sizeCount: 5, baseSizeIndex: 2);

        Assert.Equal(5, result.Count);
        Assert.Equal(-0.75m, result[0]);  // below base
        Assert.Equal(-0.75m, result[1]);  // below base
        Assert.Equal(0m,     result[2]);  // base
        Assert.Equal(1.0m,   result[3]);  // above base
        Assert.Equal(1.0m,   result[4]);  // above base
    }

    [Fact]
    public void ApplyGradeRuleSet_CaseInsensitiveBodyPartCode()
    {
        var rules = new[] { new GradeRuleInput("waist", 1m, 1m) };
        var result = _engine.ApplyGradeRuleSet(rules, "WAIST", sizeCount: 3, baseSizeIndex: 1);
        Assert.Equal(0m, result[1]);
    }

    [Fact]
    public void ApplyGradeRuleSet_ThrowsWhenBodyPartCodeNotFound()
    {
        var rules = new[] { new GradeRuleInput("BUST", 1m, 1m) };
        Assert.Throws<InvalidOperationException>(() =>
            _engine.ApplyGradeRuleSet(rules, "HIP", sizeCount: 3, baseSizeIndex: 1));
    }

    // ── ChangeBaseSize ───────────────────────────────────────────────────────

    [Fact]
    public void ChangeBaseSize_SameIndex_ReturnsUnchanged()
    {
        decimal[] deltas = [-0.5m, 0m, 0.5m];
        var (newBase, newDeltas) = _engine.ChangeBaseSize(20m, deltas, 1, 1);

        Assert.Equal(20m, newBase);
        Assert.Equal(deltas, newDeltas);
    }

    [Fact]
    public void ChangeBaseSize_ShiftRight_AbsoluteMeasurementsUnchanged()
    {
        // Original: base=index 1 (value 20), sizes [18.5, 20, 21.5]
        decimal[] deltas = [-1.5m, 0m, 1.5m];
        var (newBase, newDeltas) = _engine.ChangeBaseSize(20m, deltas, currentBaseSizeIndex: 1, newBaseSizeIndex: 2);

        // New base value should be 21.5 (the size at index 2)
        Assert.Equal(21.5m, newBase);

        // Recomputed values must still equal [18.5, 20, 21.5]
        var values = _engine.ComputeSizeValues(newBase, 2, newDeltas);
        Assert.Equal(18.5m, values[0]);
        Assert.Equal(20m,   values[1]);
        Assert.Equal(21.5m, values[2]);
    }

    [Fact]
    public void ChangeBaseSize_ShiftLeft_AbsoluteMeasurementsUnchanged()
    {
        decimal[] deltas = [-1.5m, 0m, 1.5m];
        var (newBase, newDeltas) = _engine.ChangeBaseSize(20m, deltas, currentBaseSizeIndex: 1, newBaseSizeIndex: 0);

        Assert.Equal(18.5m, newBase);

        var values = _engine.ComputeSizeValues(newBase, 0, newDeltas);
        Assert.Equal(18.5m, values[0]);
        Assert.Equal(20m,   values[1]);
        Assert.Equal(21.5m, values[2]);
    }

    [Fact]
    public void ChangeBaseSize_NewDeltaAtNewBaseIsZero()
    {
        decimal[] deltas = [-1m, 0m, 1m, 1m];
        var (_, newDeltas) = _engine.ChangeBaseSize(30m, deltas, currentBaseSizeIndex: 1, newBaseSizeIndex: 3);
        Assert.Equal(0m, newDeltas[3]);
    }
}
