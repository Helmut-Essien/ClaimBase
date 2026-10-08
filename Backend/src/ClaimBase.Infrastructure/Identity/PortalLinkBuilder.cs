using ClaimBase.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace ClaimBase.Infrastructure.Identity;

/// <summary>Portal base URL used in emails. <c>Portal:BaseUrl</c>.</summary>
public sealed class PortalOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Portal";

    /// <summary>Absolute Portal origin, with no path. Development is <c>http://localhost:4201</c>.</summary>
    public string BaseUrl { get; set; } = "";
}

/// <summary>Builds the reset link from <see cref="PortalOptions.BaseUrl"/>. The request host is never used.</summary>
public sealed class PortalLinkBuilder : IPortalLinks
{
    private readonly PortalOptions _options;

    /// <summary>
    /// Creates the builder.
    /// </summary>
    /// <param name="options">Portal settings.</param>
    public PortalLinkBuilder(IOptions<PortalOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <inheritdoc />
    public string ResetPassword(string email, string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);

        if (!Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttps && baseUri.Scheme != Uri.UriSchemeHttp))
        {
            throw new InvalidOperationException("Portal:BaseUrl must be an absolute http or https URL.");
        }

        // Escape once. UriBuilder would encode the percent signs a second time.
        var root = baseUri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        return $"{root}/login/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(rawToken)}";
    }
}
