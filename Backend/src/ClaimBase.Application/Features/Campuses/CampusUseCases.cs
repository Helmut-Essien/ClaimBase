using ClaimBase.Application.Common;
using ClaimBase.Application.Common.Exceptions;
using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Domain.Academic;
using ClaimBase.Domain.Identity;
using ClaimBase.Shared.Academic;
using ClaimBase.Shared.Common;
using FluentValidation;
using MediatR;

namespace ClaimBase.Application.Features.Campuses;

/// <summary>Lists campuses.</summary>
/// <param name="Page">1-based page.</param>
/// <param name="PageSize">Page size, 1 to 100.</param>
public sealed record ListCampusesQuery(int Page, int PageSize) : IRequest<PagedResult<CampusResponse>>;

/// <summary>Validates campus list paging.</summary>
public sealed class ListCampusesQueryValidator : AbstractValidator<ListCampusesQuery>
{
    /// <summary>Creates the paging rules.</summary>
    public ListCampusesQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, PageLimits.MaxSize);
    }
}

/// <summary>Returns one page of campuses.</summary>
public sealed class ListCampusesQueryHandler : IRequestHandler<ListCampusesQuery, PagedResult<CampusResponse>>
{
    private readonly ISetupAccess _access;
    private readonly IAcademicCatalog _catalog;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="catalog">Academic store.</param>
    public ListCampusesQueryHandler(ISetupAccess access, IAcademicCatalog catalog)
    {
        _access = access;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<PagedResult<CampusResponse>> Handle(ListCampusesQuery request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        return await _catalog.ListCampusesAsync(request.Page, request.PageSize, cancellationToken);
    }
}

/// <summary>Creates a campus.</summary>
/// <param name="Name">Campus name.</param>
public sealed record CreateCampusCommand(string Name) : IRequest<CampusResponse>;

/// <summary>Validates a campus name.</summary>
public sealed class CreateCampusCommandValidator : AbstractValidator<CreateCampusCommand>
{
    /// <summary>Creates the name rules.</summary>
    public CreateCampusCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(AcademicConstraints.NameMaxLength);
    }
}

/// <summary>Creates a campus when the name is not already used in the tenant.</summary>
public sealed class CreateCampusCommandHandler : IRequestHandler<CreateCampusCommand, CampusResponse>
{
    private readonly ISetupAccess _access;
    private readonly IAcademicCatalog _catalog;
    private readonly ICurrentTenant _current;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="catalog">Academic store.</param>
    /// <param name="current">Current tenant.</param>
    public CreateCampusCommandHandler(ISetupAccess access, IAcademicCatalog catalog, ICurrentTenant current)
    {
        _access = access;
        _catalog = catalog;
        _current = current;
    }

    /// <inheritdoc />
    public async Task<CampusResponse> Handle(CreateCampusCommand request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        if (await _catalog.CampusNameTakenAsync(request.Name, cancellationToken))
            throw new ConflictAppException("A campus with that name already exists.");

        var campus = Campus.Create(EntityIds.New(), _current.TenantId, request.Name);
        _catalog.Add(campus);
        await _catalog.SaveChangesAsync(cancellationToken);
        return new CampusResponse { Id = campus.Id, Name = campus.Name };
    }
}
