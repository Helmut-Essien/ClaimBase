using ClaimBase.Domain.Common;

namespace ClaimBase.Domain.Identity;

/// <summary>
/// A login. Lecturers also have a staff record; heads of department are tied to one department.
/// Department and staff ids are stored before those tables exist so the token and the role rules can be enforced.
/// </summary>
public sealed class User
{
    private User()
    {
    }

    /// <summary>ULID primary key. This is the JWT <c>sub</c> claim.</summary>
    public string Id { get; private set; } = null!;

    /// <summary>Tenant that owns this login.</summary>
    public string TenantId { get; private set; } = null!;

    /// <summary>Sign-in email, lowercase, unique inside the tenant.</summary>
    public string Email { get; private set; } = null!;

    /// <summary>Name shown in the portal shell.</summary>
    public string DisplayName { get; private set; } = null!;

    /// <summary>Bcrypt password hash. Never returned by the API.</summary>
    public string PasswordHash { get; private set; } = null!;

    /// <summary>What this account is allowed to do.</summary>
    public UserRole Role { get; private set; }

    /// <summary>
    /// Department this head of department reviews. Required for <see cref="UserRole.HeadOfDepartment"/> and empty for every other role.
    /// There is no department foreign key until that table is added.
    /// </summary>
    public string? DepartmentId { get; private set; }

    /// <summary>
    /// Staff record this lecturer logs sessions for. Required for <see cref="UserRole.Lecturer"/> and empty for every other role.
    /// There is no staff foreign key until that table is added.
    /// </summary>
    public string? StaffId { get; private set; }

    /// <summary>UTC creation time.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Creates a user. Email is stored lowercase. A head of department must carry a department id, and a lecturer must carry a staff id.
    /// Other roles must not, so the JWT only gains <c>departmentId</c> for a head of department.
    /// </summary>
    /// <param name="id">ULID.</param>
    /// <param name="tenantId">Owning tenant id.</param>
    /// <param name="email">Sign-in email.</param>
    /// <param name="displayName">Name shown in the shell.</param>
    /// <param name="passwordHash">Bcrypt hash.</param>
    /// <param name="role">Account role.</param>
    /// <param name="departmentId">Required for a head of department.</param>
    /// <param name="staffId">Required for a lecturer.</param>
    /// <param name="createdAt">UTC timestamp.</param>
    /// <returns>The new user.</returns>
    public static User Create(
        string id,
        string tenantId,
        string email,
        string displayName,
        string passwordHash,
        UserRole role,
        string? departmentId,
        string? staffId,
        DateTimeOffset createdAt)
    {
        if (!Enum.IsDefined(role))
            throw new ArgumentException("Role is not a known ClaimBase role.", nameof(role));

        var department = OptionalId(departmentId, nameof(departmentId));
        var staff = OptionalId(staffId, nameof(staffId));

        // The token carries departmentId only for a head of department, and a lecturer login is useless without a staff row.
        if (role == UserRole.HeadOfDepartment && department is null)
            throw new ArgumentException("A head of department must be tied to one department.", nameof(departmentId));

        if (role != UserRole.HeadOfDepartment && department is not null)
            throw new ArgumentException("Only a head of department has a department id.", nameof(departmentId));

        if (role == UserRole.Lecturer && staff is null)
            throw new ArgumentException("A lecturer login must be tied to a staff record.", nameof(staffId));

        if (role != UserRole.Lecturer && staff is not null)
            throw new ArgumentException("Only a lecturer has a staff id.", nameof(staffId));

        var normalizedEmail = Guard.Required(email, nameof(email), UserConstraints.EmailMaxLength).ToLowerInvariant();
        if (!normalizedEmail.Contains('@') || normalizedEmail.StartsWith('@') || normalizedEmail.EndsWith('@'))
            throw new ArgumentException("Email is invalid.", nameof(email));

        return new User
        {
            Id = Guard.RequiredId(id, nameof(id), UserConstraints.IdMaxLength),
            TenantId = Guard.RequiredId(tenantId, nameof(tenantId), UserConstraints.IdMaxLength),
            Email = normalizedEmail,
            DisplayName = Guard.Required(displayName, nameof(displayName), UserConstraints.DisplayNameMaxLength),
            PasswordHash = Guard.Required(passwordHash, nameof(passwordHash), UserConstraints.PasswordHashMaxLength),
            Role = role,
            DepartmentId = department,
            StaffId = staff,
            CreatedAt = createdAt.Offset == TimeSpan.Zero ? createdAt : createdAt.ToUniversalTime()
        };
    }

    private static string? OptionalId(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Guard.RequiredId(value, name, UserConstraints.IdMaxLength);
    }
}
