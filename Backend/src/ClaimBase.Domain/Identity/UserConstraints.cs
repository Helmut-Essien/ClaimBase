namespace ClaimBase.Domain.Identity;

/// <summary>
/// Column and input bounds for a user. Portal <c>AUTH_FIELD_LIMITS</c> must match the password and email bounds.
/// </summary>
public static class UserConstraints
{
    /// <summary>ULID string length used for primary keys and for ids stored before their tables exist.</summary>
    public const int IdMaxLength = 26;

    /// <summary>Email addresses are stored lowercase and unique per tenant.</summary>
    public const int EmailMaxLength = 320;

    /// <summary>Plain-text password minimum. The column stores a hash, not this value.</summary>
    public const int PasswordMinLength = 8;

    /// <summary>Plain-text password maximum accepted by login.</summary>
    public const int PasswordMaxLength = 128;

    /// <summary>Bcrypt hashes are 60 characters. The column allows a little headroom.</summary>
    public const int PasswordHashMaxLength = 100;

    /// <summary>Display name maximum.</summary>
    public const int DisplayNameMaxLength = 200;

    /// <summary>Stored role name maximum.</summary>
    public const int RoleMaxLength = 32;
}
