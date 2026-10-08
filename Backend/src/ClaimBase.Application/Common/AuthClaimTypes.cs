namespace ClaimBase.Application.Common;

/// <summary>JWT claim names ClaimBase adds beside the registered <c>sub</c> claim.</summary>
public static class AuthClaimTypes
{
    /// <summary>
    /// Unix seconds of <c>User.PasswordChangedAt</c>. Absent until the first reset.
    /// A token whose stamp does not match the user row is rejected.
    /// </summary>
    public const string PasswordChangedAt = "pwd";
}
