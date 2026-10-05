namespace ClaimBase.Domain.Rates;

/// <summary>
/// Money bounds for teaching and transport rates. Amounts are <c>numeric(18,2)</c> in the tenant currency and are never negative.
/// </summary>
public static class RateConstraints
{
    /// <summary>Total digits stored for an amount.</summary>
    public const int AmountPrecision = 18;

    /// <summary>Digits after the decimal point. Cedis are stored to the pesewa.</summary>
    public const int AmountScale = 2;

    /// <summary>Largest amount <c>numeric(18,2)</c> can store.</summary>
    public const decimal MaxAmount = 9_999_999_999_999_999.99m;

    /// <summary>
    /// True when <paramref name="amount"/> can be stored without rounding.
    /// </summary>
    /// <param name="amount">The cedis amount.</param>
    /// <returns>True when the amount is non-negative, within range, and has at most two decimal places.</returns>
    public static bool IsStorable(decimal amount) =>
        amount >= 0 && amount <= MaxAmount && amount == decimal.Round(amount, AmountScale);
}
