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

/// <summary>Lists faculties.</summary>
/// <param name="Page">1-based page.</param>
/// <param name="PageSize">Page size, 1 to 100.</param>
public sealed record ListFacultiesQuery(int Page, int PageSize) : IRequest<PagedResult<FacultyResponse>>;

/// <summary>Validates faculty list paging.</summary>
public sealed class ListFacultiesQueryValidator : AbstractValidator<ListFacultiesQuery>
{
    /// <summary>Creates the paging rules.</summary>
    public ListFacultiesQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, PageLimits.MaxSize);
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
        return await _catalog.ListFacultiesAsync(request.Page, request.PageSize, cancellationToken);
    }
}

/// <summary>Creates a faculty.</summary>
/// <param name="Name">Faculty name.</param>
public sealed record CreateFacultyCommand(string Name) : IRequest<FacultyResponse>;

/// <summary>Validates a faculty name.</summary>
public sealed class CreateFacultyCommandValidator : AbstractValidator<CreateFacultyCommand>
{
    /// <summary>Creates the name rules.</summary>
    public CreateFacultyCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(AcademicConstraints.NameMaxLength);
    }
}

/// <summary>Creates a faculty when the name is not already used.</summary>
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
        if (await _catalog.FacultyNameTakenAsync(request.Name, cancellationToken))
            throw new ConflictAppException("A faculty with that name already exists.");

        var faculty = Faculty.Create(EntityIds.New(), _current.TenantId, request.Name);
        _catalog.Add(faculty);
        await _catalog.SaveChangesAsync(cancellationToken);
        return new FacultyResponse { Id = faculty.Id, Name = faculty.Name };
    }
}
