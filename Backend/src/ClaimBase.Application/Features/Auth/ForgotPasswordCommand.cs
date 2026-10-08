using ClaimBase.Shared.Auth;
using MediatR;

namespace ClaimBase.Application.Features.Auth;

/// <summary>Asks for a reset link. The response does not say whether the email is on file.</summary>
/// <param name="Email">Sign-in email.</param>
public sealed record ForgotPasswordCommand(string Email) : IRequest<ForgotPasswordResponse>;
