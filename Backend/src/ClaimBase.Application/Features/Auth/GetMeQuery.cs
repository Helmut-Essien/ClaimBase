using ClaimBase.Shared.Auth;
using MediatR;

namespace ClaimBase.Application.Features.Auth;

/// <summary>Loads the signed-in portal profile. Lecturers are rejected here.</summary>
public sealed record GetMeQuery : IRequest<MeResponse>;
