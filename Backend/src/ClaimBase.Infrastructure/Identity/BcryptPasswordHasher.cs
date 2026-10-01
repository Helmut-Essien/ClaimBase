using ClaimBase.Application.Common.Interfaces;

namespace ClaimBase.Infrastructure.Identity;

/// <summary>Bcrypt password hasher. Unknown emails compare against a hash created once per process.</summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    // One real hash so an unknown email pays the same bcrypt cost as a stored user. The plain value is discarded.
    private static readonly string UnknownEmailHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"));

    /// <inheritdoc />
    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    /// <inheritdoc />
    public bool Verify(string password, string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }

    /// <inheritdoc />
    public void VerifyUnknownEmail(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        BCrypt.Net.BCrypt.Verify(password, UnknownEmailHash);
    }
}
