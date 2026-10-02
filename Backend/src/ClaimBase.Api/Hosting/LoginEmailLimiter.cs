using System.Threading.RateLimiting;
using ClaimBase.Shared.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ClaimBase.Api.Hosting;

/// <summary>
/// Fixed window of login attempts for one normalized email.
/// This sits beside the per-IP limit so a rotated client address cannot keep guessing one password.
/// </summary>
public sealed class LoginEmailLimiter : IDisposable
{
    private readonly PartitionedRateLimiter<string> _limiter;

    /// <summary>
    /// Creates the limiter.
    /// </summary>
    /// <param name="permitLimit">Attempts allowed per email in one minute.</param>
    public LoginEmailLimiter(int permitLimit)
    {
        if (permitLimit < 1)
            throw new ArgumentOutOfRangeException(nameof(permitLimit));

        _limiter = PartitionedRateLimiter.Create<string, string>(email =>
            RateLimitPartition.GetFixedWindowLimiter(
                email,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
    }

    /// <summary>
    /// Consumes one attempt for the email, when the address is short enough to be a login.
    /// </summary>
    /// <param name="email">Raw email from the login body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when this attempt is inside the window.</returns>
    public async ValueTask<bool> TryConsumeAsync(string? email, CancellationToken cancellationToken)
    {
        var key = Normalize(email);
        if (key is null)
            return true;

        using var lease = await _limiter.AcquireAsync(key, 1, cancellationToken);
        return lease.IsAcquired;
    }

    /// <inheritdoc />
    public void Dispose() => _limiter.Dispose();

    /// <summary>Trims and lowercases the same way login compares addresses. Over-long values are not a bucket.</summary>
    /// <param name="email">Raw email.</param>
    /// <returns>The bucket key, or null when the value cannot be a stored email.</returns>
    public static string? Normalize(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var key = email.Trim().ToLowerInvariant();
        if (key.Length > AuthFieldLimits.Email)
            return null;

        return key;
    }
}

/// <summary>Rejects a login that has used its email window. The IP window is a separate policy.</summary>
public sealed class LoginEmailRateLimitFilter : IAsyncActionFilter
{
    private readonly LoginEmailLimiter _limiter;

    /// <summary>
    /// Creates the filter.
    /// </summary>
    /// <param name="limiter">Per-email login window.</param>
    public LoginEmailRateLimitFilter(LoginEmailLimiter limiter)
    {
        ArgumentNullException.ThrowIfNull(limiter);
        _limiter = limiter;
    }

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var email = context.ActionArguments.Values.OfType<LoginRequest>().FirstOrDefault()?.Email;
        if (!await _limiter.TryConsumeAsync(email, context.HttpContext.RequestAborted))
        {
            context.Result = new StatusCodeResult(StatusCodes.Status429TooManyRequests);
            return;
        }

        await next();
    }
}
