using System;
using System.Collections.Generic;

namespace APP.TechPack.Engine;

public sealed class GradingEngine : IGradingEngine
{
    public IReadOnlyList<decimal> ComputeSizeValues(
        decimal baseValue, int baseSizeIndex, IReadOnlyList<decimal> gradingDeltas)
    {
        if (baseValue <= 0)
            throw new ArgumentOutOfRangeException(nameof(baseValue), "Base value must be positive.");
        ValidateIndex(baseSizeIndex, gradingDeltas.Count, nameof(baseSizeIndex));

        var values = new decimal[gradingDeltas.Count];
        values[baseSizeIndex] = baseValue;

        // Expand upward: delta[i] = value[i] - value[i-1]
        for (int i = baseSizeIndex + 1; i < values.Length; i++)
            values[i] = values[i - 1] + gradingDeltas[i];

        // Expand downward: delta[i] = value[i] - value[i+1], so value[i] = value[i+1] + delta[i]
        for (int i = baseSizeIndex - 1; i >= 0; i--)
            values[i] = values[i + 1] + gradingDeltas[i];

        return values;
    }

    public IReadOnlyList<decimal> ComputeGradingDeltas(
        IReadOnlyList<decimal> sizeValues, int baseSizeIndex)
    {
        ValidateIndex(baseSizeIndex, sizeValues.Count, nameof(baseSizeIndex));

        var deltas = new decimal[sizeValues.Count];
        deltas[baseSizeIndex] = 0m;

        for (int i = baseSizeIndex + 1; i < deltas.Length; i++)
            deltas[i] = sizeValues[i] - sizeValues[i - 1];

        for (int i = baseSizeIndex - 1; i >= 0; i--)
            deltas[i] = sizeValues[i] - sizeValues[i + 1];

        return deltas;
    }

    public IReadOnlyList<decimal> ApplyGradeRuleSet(
        IReadOnlyList<GradeRuleInput> rules, string bodyPartCode,
        int sizeCount, int baseSizeIndex)
    {
        ValidateIndex(baseSizeIndex, sizeCount, nameof(baseSizeIndex));

        var rule = FindRule(rules, bodyPartCode);

        var deltas = new decimal[sizeCount];
        deltas[baseSizeIndex] = 0m;

        // Above base: each step up adds PlusValue
        for (int i = baseSizeIndex + 1; i < sizeCount; i++)
            deltas[i] = rule.PlusValue;

        // Below base: each step down subtracts MinuValue (delta convention: value[i] - value[i+1])
        for (int i = baseSizeIndex - 1; i >= 0; i--)
            deltas[i] = -rule.MinuValue;

        return deltas;
    }

    public (decimal NewBaseValue, IReadOnlyList<decimal> NewDeltas) ChangeBaseSize(
        decimal currentBaseValue, IReadOnlyList<decimal> currentDeltas,
        int currentBaseSizeIndex, int newBaseSizeIndex)
    {
        ValidateIndex(currentBaseSizeIndex, currentDeltas.Count, nameof(currentBaseSizeIndex));
        ValidateIndex(newBaseSizeIndex, currentDeltas.Count, nameof(newBaseSizeIndex));

        if (currentBaseSizeIndex == newBaseSizeIndex)
            return (currentBaseValue, currentDeltas);

        var values = ComputeSizeValues(currentBaseValue, currentBaseSizeIndex, currentDeltas);
        var newBaseValue = values[newBaseSizeIndex];
        var newDeltas = ComputeGradingDeltas(values, newBaseSizeIndex);

        return (newBaseValue, newDeltas);
    }

    private static GradeRuleInput FindRule(IReadOnlyList<GradeRuleInput> rules, string bodyPartCode)
    {
        foreach (var rule in rules)
        {
            if (string.Equals(rule.BodyPartCode, bodyPartCode, StringComparison.OrdinalIgnoreCase))
                return rule;
        }
        throw new InvalidOperationException(
            $"No grade rule found for body part code '{bodyPartCode}'.");
    }

    private static void ValidateIndex(int index, int count, string paramName)
    {
        if (index < 0 || index >= count)
            throw new ArgumentOutOfRangeException(paramName,
                $"Index {index} is out of range [0, {count - 1}].");
    }
}
