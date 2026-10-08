namespace ClaimBase.Infrastructure.Identity;

/// <summary>SMTP settings from the <c>Email</c> section. An empty host means no SMTP in Development and tests.</summary>
public sealed class EmailOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Email";

    /// <summary>SMTP host. Required in Production. Empty keeps the link out of SMTP.</summary>
    public string Host { get; set; } = "";

    /// <summary>SMTP port. Defaults to 587.</summary>
    public int Port { get; set; } = 587;

    /// <summary>SMTP username. Empty skips authentication.</summary>
    public string Username { get; set; } = "";

    /// <summary>SMTP password. Never committed for Production.</summary>
    public string Password { get; set; } = "";

    /// <summary>From address. Required in Production.</summary>
    public string FromAddress { get; set; } = "";

    /// <summary>From display name.</summary>
    public string FromName { get; set; } = "ClaimBase";

    /// <summary>Use STARTTLS when connecting.</summary>
    public bool UseStartTls { get; set; } = true;
}
