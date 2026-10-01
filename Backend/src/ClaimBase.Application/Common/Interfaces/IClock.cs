namespace ClaimBase.Application.Common.Interfaces;

/// <summary>UTC clock so tests can freeze token expiry.</summary>
public interface IClock
{
    /// <summary>Current UTC time.</summary>
    DateTimeOffset UtcNow { get; }
}
