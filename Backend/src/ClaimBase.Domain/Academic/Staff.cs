using ClaimBase.Domain.Common;
using ClaimBase.Domain.Identity;

namespace ClaimBase.Domain.Academic;

/// <summary>
/// A lecturer who can be claimed, part-time or full-time. Department assignments and position appointments are child records.
/// A staff row with no department is rejected by the use case, not stored.
/// </summary>
public sealed class Staff
{
    private Staff()
    {
    }

    /// <summary>ULID primary key.</summary>
    public string Id { get; private set; } = null!;

    /// <summary>Owning tenant.</summary>
    public string TenantId { get; private set; } = null!;

    /// <summary>Staff number, unique in the tenant.</summary>
    public string StaffNumber { get; private set; } = null!;

    /// <summary>Name printed on the claim.</summary>
    public string DisplayName { get; private set; } = null!;

    /// <summary>Optional contact email, stored lowercase.</summary>
    public string? Email { get; private set; }

    /// <summary>Part-time or full-time. Both are paid.</summary>
    public EmploymentType EmploymentType { get; private set; }

    /// <summary>
    /// Optional biometric device id. When set, punches match this exact value. There is no alias table.
    /// </summary>
    public string? BiometricId { get; private set; }

    /// <summary>
    /// Creates a staff member. Email and biometric id are optional.
    /// </summary>
    /// <param name="id">ULID.</param>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="staffNumber">Staff number.</param>
    /// <param name="displayName">Display name.</param>
    /// <param name="email">Optional email.</param>
    /// <param name="employmentType">Part-time or full-time.</param>
    /// <param name="biometricId">Optional device id.</param>
    /// <returns>The new staff member.</returns>
    public static Staff Create(
        string id,
        string tenantId,
        string staffNumber,
        string displayName,
        string? email,
        EmploymentType employmentType,
        string? biometricId)
    {
        var staff = new Staff
        {
            Id = Guard.RequiredId(id, nameof(id), UserConstraints.IdMaxLength),
            TenantId = Guard.RequiredId(tenantId, nameof(tenantId), UserConstraints.IdMaxLength),
            StaffNumber = "",
            DisplayName = ""
        };
        staff.Update(staffNumber, displayName, email, employmentType, biometricId);
        return staff;
    }

    /// <summary>
    /// Replaces the editable staff fields. Department assignments are not changed here.
    /// </summary>
    /// <param name="staffNumber">Staff number.</param>
    /// <param name="displayName">Display name.</param>
    /// <param name="email">Optional email.</param>
    /// <param name="employmentType">Part-time or full-time.</param>
    /// <param name="biometricId">Optional device id. Blank clears it.</param>
    public void Update(
        string staffNumber,
        string displayName,
        string? email,
        EmploymentType employmentType,
        string? biometricId)
    {
        if (!Enum.IsDefined(employmentType))
            throw new ArgumentException("EmploymentType is not a known value.", nameof(employmentType));

        StaffNumber = Guard.Required(staffNumber, nameof(staffNumber), AcademicConstraints.StaffNumberMaxLength);
        DisplayName = Guard.Required(displayName, nameof(displayName), AcademicConstraints.NameMaxLength);
        Email = NormalizeEmail(email);
        EmploymentType = employmentType;
        BiometricId = string.IsNullOrWhiteSpace(biometricId)
            ? null
            : Guard.Required(biometricId, nameof(biometricId), AcademicConstraints.BiometricIdMaxLength);
    }

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var normalized = Guard.Required(email, nameof(email), UserConstraints.EmailMaxLength).ToLowerInvariant();
        if (!normalized.Contains('@') || normalized.StartsWith('@') || normalized.EndsWith('@'))
            throw new ArgumentException("Email is invalid.", nameof(email));

        return normalized;
    }
}
