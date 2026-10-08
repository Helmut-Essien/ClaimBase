using ClaimBase.Api.Hosting;
using ClaimBase.Application.Features.Auth;
using ClaimBase.Shared.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClaimBase.Api.Controllers;

/// <summary>Sign-in, password reset, and the current user. Login and reset are anonymous. The current-user route requires a JWT.</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Creates the controller.
    /// </summary>
    /// <param name="sender">MediatR sender.</param>
    public AuthController(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);
        _sender = sender;
    }

    /// <summary>Signs in with email and password.</summary>
    /// <param name="request">Email and password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The access token and tenant profile.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(LoginRateLimiter.PolicyName)]
    [ServiceFilter(typeof(LoginEmailRateLimitFilter))]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new LoginCommand(request.Email, request.Password), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Sends a reset link when exactly one account uses the email.
    /// The body is the same when the address is unknown or shared by two tenants.
    /// </summary>
    /// <param name="request">Email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The public confirmation. It never includes the token.</returns>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(LoginRateLimiter.PolicyName)]
    [ServiceFilter(typeof(LoginEmailRateLimitFilter))]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new ForgotPasswordCommand(request.Email), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Sets a new password from a reset link. A bad link is HTTP 400 with the same message for every failure.
    /// </summary>
    /// <param name="request">Email, token, and the new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The success message.</returns>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting(LoginRateLimiter.PolicyName)]
    [ServiceFilter(typeof(LoginEmailRateLimitFilter))]
    public async Task<ActionResult<ResetPasswordResponse>> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new ResetPasswordCommand(request.Email, request.Token, request.NewPassword, request.ConfirmPassword),
            cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Returns the signed-in portal user. Lecturers receive 403 because the portal is not their app.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The profile, including the tenant time zone.</returns>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeResponse>> Me(CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetMeQuery(), cancellationToken));
    }
}
