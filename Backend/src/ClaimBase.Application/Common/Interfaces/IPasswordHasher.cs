namespace ClaimBase.Application.Common.Interfaces;

/// <summary>Password hashing. The API never stores or logs the plain password.</summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a password for storage.
    /// </summary>
    /// <param name="password">Plain password.</param>
    /// <returns>A bcrypt hash.</returns>
    string Hash(string password);

    /// <summary>
    /// Compares a password with a stored hash.
    /// </summary>
    /// <param name="password">Plain password.</param>
    /// <param name="passwordHash">Stored bcrypt hash.</param>
    /// <returns>True when the password matches.</returns>
    bool Verify(string password, string passwordHash);

    /// <summary>
    /// Runs a real bcrypt compare against a fixed hash. Unknown emails and ambiguous emails call this so they are not faster than a miss.
    /// </summary>
    /// <param name="password">The submitted password. The result is discarded.</param>
    void VerifyUnknownEmail(string password);
}
