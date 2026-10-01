namespace ClaimBase.Application.Common.Interfaces;

/// <summary>Confirms the caller may maintain academic setup. The database role wins over the token.</summary>
public interface ISetupAccess
{
    /// <summary>
    /// Throws when the caller is anonymous, missing, or not a tenant admin or admin.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnsureAsync(CancellationToken cancellationToken);
}
