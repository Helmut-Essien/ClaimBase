using ClaimBase.Shared.Auth;
using MediatR;

namespace ClaimBase.Application.Features.Auth;

/// <summary>Signs a user in with email and password.</summary>
/// <param name="Email">Email. Stored lowercase; the handler trims and lowercases.</param>
/// <param name="Password">Plain password. It is not logged.</param>
public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;
