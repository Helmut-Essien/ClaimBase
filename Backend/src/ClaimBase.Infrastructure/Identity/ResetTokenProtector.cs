using System.Security.Cryptography;
using System.Text;
using ClaimBase.Application.Common.Interfaces;

namespace ClaimBase.Infrastructure.Identity;

/// <summary>Random 32-byte reset tokens. Only the SHA-256 hex is stored.</summary>
public sealed class ResetTokenProtector : IResetTokenProtector
{
    /// <inheritdoc />
    public ResetToken Create()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var raw = ToBase64Url(bytes);
        return new ResetToken(raw, Hash(raw));
    }

    /// <inheritdoc />
    public string Hash(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
