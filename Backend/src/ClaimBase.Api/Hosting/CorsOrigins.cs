namespace ClaimBase.Api.Hosting;

/// <summary>
/// Resolves CORS origins from <c>Cors:Origins</c> as a JSON array or a comma-separated <c>CORS__ORIGINS</c> value.
/// </summary>
public static class CorsOrigins
{
    /// <summary>
    /// Returns trimmed origin URLs. An empty result means same-origin only.
    /// </summary>
    /// <param name="configuration">The host configuration.</param>
    /// <returns>Distinct origin URLs.</returns>
    public static string[] Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var fromArray = configuration.GetSection("Cors:Origins").Get<string[]>();
        if (fromArray is { Length: > 0 })
            return Normalize(fromArray);

        // Environment variables arrive as one string (`CORS__ORIGINS=https://a,https://b`).
        var raw = configuration["Cors:Origins"];
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        return Normalize(raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static string[] Normalize(IEnumerable<string> origins) =>
        origins
            .Select(origin => origin.Trim())
            .Where(origin => origin.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
