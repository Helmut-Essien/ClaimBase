using ClaimBase.Application.Common;
using ClaimBase.Application.Common.Exceptions;
using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Domain.Academic;
using ClaimBase.Domain.Identity;
using ClaimBase.Shared.Academic;
using ClaimBase.Shared.Common;
using FluentValidation;
using MediatR;

namespace ClaimBase.Application.Features.Faculties;

/// <summary>Lists faculties. <paramref name="CampusId"/> limits the page to one campus.</summary>
/// <param name="Page">1-based page.</param>
/// <param name="PageSize">Page size, 1 to 100.</param>
/// <param name="CampusId">Optional campus filter.</param>
public sealed record ListFacultiesQuery(int Page, int PageSize, string? CampusId) : IRequest<PagedResult<FacultyResponse>>;

/// <summary>Validates faculty list paging.</summary>
public sealed class ListFacultiesQueryValidator : AbstractValidator<ListFacultiesQuery>
{
    /// <summary>Creates the paging rules.</summary>
    public ListFacultiesQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, PageLimits.MaxSize);
        RuleFor(query => query.CampusId).MaximumLength(UserConstraints.IdMaxLength);
    }
}

/// <summary>Returns one page of faculties.</summary>
public sealed class ListFacultiesQueryHandler : IRequestHandler<ListFacultiesQuery, PagedResult<FacultyResponse>>
{
    private readonly ISetupAccess _access;
    private readonly IAcademicCatalog _catalog;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="catalog">Academic store.</param>
    public ListFacultiesQueryHandler(ISetupAccess access, IAcademicCatalog catalog)
    {
        _access = access;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<PagedResult<FacultyResponse>> Handle(ListFacultiesQuery request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        return await _catalog.ListFacultiesAsync(request.Page, request.PageSize, request.CampusId, cancellationToken);
    }
}

/// <summary>Creates a faculty on a campus.</summary>
/// <param name="CampusId">Parent campus.</param>
/// <param name="Name">Faculty name.</param>
public sealed record CreateFacultyCommand(string CampusId, string Name) : IRequest<FacultyResponse>;

/// <summary>Validates a faculty and its campus.</summary>
public sealed class CreateFacultyCommandValidator : AbstractValidator<CreateFacultyCommand>
{
    /// <summary>Creates the name rules.</summary>
    public CreateFacultyCommandValidator()
    {
        RuleFor(command => command.CampusId).NotEmpty().MaximumLength(UserConstraints.IdMaxLength);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(AcademicConstraints.NameMaxLength);
    }
}

/// <summary>Creates a faculty when the name is free on that campus.</summary>
public sealed class CreateFacultyCommandHandler : IRequestHandler<CreateFacultyCommand, FacultyResponse>
{
    private readonly ISetupAccess _access;
    private readonly IAcademicCatalog _catalog;
    private readonly ICurrentTenant _current;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="catalog">Academic store.</param>
    /// <param name="current">Current tenant.</param>
    public CreateFacultyCommandHandler(ISetupAccess access, IAcademicCatalog catalog, ICurrentTenant current)
    {
        _access = access;
        _catalog = catalog;
        _current = current;
    }

    /// <inheritdoc />
    public async Task<FacultyResponse> Handle(CreateFacultyCommand request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        var campus = await _catalog.GetCampusAsync(request.CampusId, cancellationToken);
        if (campus is null)
            throw new NotFoundAppException("Campus was not found.");

        // The same faculty name may exist on another campus. Uniqueness is per campus.
        if (await _catalog.FacultyNameTakenAsync(request.CampusId, request.Name, cancellationToken))
            throw new ConflictAppException("A faculty with that name already exists on this campus.");

        var faculty = Faculty.Create(EntityIds.New(), _current.TenantId, campus.Id, request.Name);
        _catalog.Add(faculty);
        await _catalog.SaveChangesAsync(cancellationToken);
        return new FacultyResponse
        {
            Id = faculty.Id,
            CampusId = campus.Id,
            CampusName = campus.Name,
            Name = faculty.Name
        };
    }
}
