namespace ClaimBase.Application.Common.Interfaces;

/// <summary>Builds Portal URLs that emails can open. The host comes from configuration, not from the request.</summary>
public interface IPortalLinks
{
    /// <summary>
    /// Reset page for one email and one raw token.
    /// </summary>
    /// <param name="email">Lowercase email.</param>
    /// <param name="rawToken">Raw token. This is the only place it is put into a URL.</param>
    /// <returns>Absolute Portal URL.</returns>
    string ResetPassword(string email, string rawToken);
}
