namespace ClaimBase.Domain.Identity;

/// <summary>
/// Portal and mobile roles. The lecturer role is a mobile account and is rejected by portal routes.
/// </summary>
public enum UserRole
{
    /// <summary>Owns tenant settings.</summary>
    TenantAdmin = 1,

    /// <summary>Runs academic setup, rates, and claims for the tenant.</summary>
    Admin = 2,

    /// <summary>Reviews claims for one department.</summary>
    HeadOfDepartment = 3,

    /// <summary>Approves claims after the head of department.</summary>
    Finance = 4,

    /// <summary>Logs sessions in the mobile app. This role cannot use the portal.</summary>
    Lecturer = 5
}
