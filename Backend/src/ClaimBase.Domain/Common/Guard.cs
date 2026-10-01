namespace ClaimBase.Domain.Common;

/// <summary>
/// Shared argument checks for domain factories.
/// </summary>
public static class Guard
{
    /// <summary>
    /// Trims a required string and rejects values outside <paramref name="maxLength"/>.
    /// </summary>
    /// <param name="value">The raw value.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="maxLength">The maximum trimmed length.</param>
    /// <returns>The trimmed value.</returns>
    public static string Required(string? value, string name, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (maxLength < 1)
            throw new ArgumentOutOfRangeException(nameof(maxLength));

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{name} is required.", name);

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new ArgumentException($"{name} must be at most {maxLength} characters.", name);

        return trimmed;
    }

    /// <summary>
    /// Trims an identifier and rejects blanks and values longer than a ULID.
    /// </summary>
    /// <param name="value">The raw identifier.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="maxLength">The maximum length. ClaimBase identifiers are 26-character ULIDs.</param>
    /// <returns>The trimmed identifier.</returns>
    public static string RequiredId(string? value, string name, int maxLength)
    {
        var trimmed = Required(value, name, maxLength);
        if (trimmed.Contains(' '))
            throw new ArgumentException($"{name} must not contain spaces.", name);

        return trimmed;
    }
}
