namespace ClaimBase.Infrastructure.Identity;

/// <summary>
/// Writes the reset link to <c>logs/password-resets.log</c> when Development has no SMTP host.
/// That folder is gitignored. Production never uses this path, and the URL is not written to Serilog.
/// </summary>
internal static class DevelopmentResetMailbox
{
    public static void Append(string contentRoot, string email, string resetUrl)
    {
        var directory = Path.Combine(contentRoot, "logs");
        Directory.CreateDirectory(directory);
        var line = $"{DateTimeOffset.UtcNow:O}\t{email}\t{resetUrl}{Environment.NewLine}";
        File.AppendAllText(Path.Combine(directory, "password-resets.log"), line);
    }
}
