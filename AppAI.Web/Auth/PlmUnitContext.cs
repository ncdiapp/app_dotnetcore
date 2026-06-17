namespace AppAI.Web.Auth;

public enum PlmUnit { Cm, Inch }

/// <summary>
/// Scoped per-request unit context. Populated by PlmUnitConversionFilter from X-PLM-Unit header.
/// Storage is always CM; use these helpers at POM API boundaries only.
/// Precision: 3 decimal places for measurements, 4 for percentages (per business rule #14).
/// </summary>
public class PlmUnitContext
{
    public PlmUnit RequestUnit { get; set; } = PlmUnit.Cm;

    public bool IsInch => RequestUnit == PlmUnit.Inch;

    /// <summary>Converts an incoming value to CM for storage (pass-through if already CM).</summary>
    public decimal ToCm(decimal value) =>
        IsInch ? Math.Round(value * 2.54m, 3) : value;

    /// <summary>Converts a CM stored value to the request unit for the response.</summary>
    public decimal FromCm(decimal value) =>
        IsInch ? Math.Round(value / 2.54m, 3) : value;

    /// <summary>Converts a collection of CM values to request unit (e.g. size value arrays).</summary>
    public IReadOnlyList<decimal> FromCmAll(IReadOnlyList<decimal> values) =>
        IsInch ? values.Select(v => Math.Round(v / 2.54m, 3)).ToList() : values;

    /// <summary>Converts a collection of incoming values to CM (e.g. grade deltas on write).</summary>
    public IReadOnlyList<decimal> ToCmAll(IReadOnlyList<decimal> values) =>
        IsInch ? values.Select(v => Math.Round(v * 2.54m, 3)).ToList() : values;
}
