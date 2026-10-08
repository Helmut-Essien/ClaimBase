using ClaimBase.Shared.Auth;
using MediatR;

namespace ClaimBase.Application.Features.Auth;

/// <summary>Sets a new password from a reset link.</summary>
/// <param name="Email">Email the link was sent to.</param>
/// <param name="Token">Raw token from the link.</param>
/// <param name="NewPassword">New plain password.</param>
/// <param name="ConfirmPassword">The same password typed again.</param>
public sealed record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword,
    string ConfirmPassword) : IRequest<ResetPasswordResponse>;
