using System.Collections.Generic;

namespace APP.TechPack.Engine;

public interface IGradingEngine
{
    /// <summary>
    /// Forward pass: baseValue at baseSizeIndex + adjacent deltas → full size value array.
    /// </summary>
    IReadOnlyList<decimal> ComputeSizeValues(
        decimal baseValue, int baseSizeIndex, IReadOnlyList<decimal> gradingDeltas);

    /// <summary>
    /// Reverse pass: absolute size values → adjacent-step deltas.
    /// Delta at baseSizeIndex is always 0.
    /// </summary>
    IReadOnlyList<decimal> ComputeGradingDeltas(
        IReadOnlyList<decimal> sizeValues, int baseSizeIndex);

    /// <summary>
    /// Apply a named grade rule set to produce per-size deltas for one body part.
    /// Throws if no rule matches bodyPartCode.
    /// </summary>
    IReadOnlyList<decimal> ApplyGradeRuleSet(
        IReadOnlyList<GradeRuleInput> rules, string bodyPartCode,
        int sizeCount, int baseSizeIndex);

    /// <summary>
    /// Rebase: shift the base-size index while keeping all absolute measurements intact.
    /// Returns the new base value and recomputed adjacent-step deltas.
    /// </summary>
    (decimal NewBaseValue, IReadOnlyList<decimal> NewDeltas) ChangeBaseSize(
        decimal currentBaseValue, IReadOnlyList<decimal> currentDeltas,
        int currentBaseSizeIndex, int newBaseSizeIndex);
}
