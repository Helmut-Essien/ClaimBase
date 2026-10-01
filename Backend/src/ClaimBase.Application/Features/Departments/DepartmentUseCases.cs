using ClaimBase.Application.Common;
using ClaimBase.Application.Common.Exceptions;
using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Domain.Academic;
using ClaimBase.Domain.Identity;
using ClaimBase.Shared.Academic;
using ClaimBase.Shared.Common;
using FluentValidation;
using MediatR;

namespace ClaimBase.Application.Features.Departments;

/// <summary>Lists departments. <paramref name="FacultyId"/> limits the page to one faculty.</summary>
/// <param name="Page">1-based page.</param>
/// <param name="PageSize">Page size, 1 to 100.</param>
/// <param name="FacultyId">Optional faculty filter.</param>
public sealed record ListDepartmentsQuery(int Page, int PageSize, string? FacultyId) : IRequest<PagedResult<DepartmentResponse>>;

/// <summary>Validates department list paging.</summary>
public sealed class ListDepartmentsQueryValidator : AbstractValidator<ListDepartmentsQuery>
{
    /// <summary>Creates the paging rules.</summary>
    public ListDepartmentsQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, PageLimits.MaxSize);
        RuleFor(query => query.FacultyId).MaximumLength(UserConstraints.IdMaxLength);
    }
}

/// <summary>Returns one page of departments.</summary>
public sealed class ListDepartmentsQueryHandler : IRequestHandler<ListDepartmentsQuery, PagedResult<DepartmentResponse>>
{
    private readonly ISetupAccess _access;
    private readonly IAcademicCatalog _catalog;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="catalog">Academic store.</param>
    public ListDepartmentsQueryHandler(ISetupAccess access, IAcademicCatalog catalog)
    {
        _access = access;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<PagedResult<DepartmentResponse>> Handle(ListDepartmentsQuery request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        return await _catalog.ListDepartmentsAsync(request.Page, request.PageSize, request.FacultyId, cancellationToken);
    }
}

/// <summary>Creates a department under a faculty.</summary>
/// <param name="FacultyId">Parent faculty.</param>
/// <param name="Name">Department name.</param>
public sealed record CreateDepartmentCommand(string FacultyId, string Name) : IRequest<DepartmentResponse>;

/// <summary>Validates a new department.</summary>
public sealed class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    /// <summary>Creates the department rules.</summary>
    public CreateDepartmentCommandValidator()
    {
        RuleFor(command => command.FacultyId).NotEmpty().MaximumLength(UserConstraints.IdMaxLength);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(AcademicConstraints.NameMaxLength);
    }
}

/// <summary>Creates a department when the faculty exists and the name is free inside it.</summary>
public sealed class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, DepartmentResponse>
{
    private readonly ISetupAccess _access;
    private readonly IAcademicCatalog _catalog;
    private readonly ICurrentTenant _current;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="catalog">Academic store.</param>
    /// <param name="current">Current tenant.</param>
    public CreateDepartmentCommandHandler(ISetupAccess access, IAcademicCatalog catalog, ICurrentTenant current)
    {
        _access = access;
        _catalog = catalog;
        _current = current;
    }

    /// <inheritdoc />
    public async Task<DepartmentResponse> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        if (!await _catalog.FacultyExistsAsync(request.FacultyId, cancellationToken))
            throw new NotFoundAppException("Faculty was not found.");

        if (await _catalog.DepartmentNameTakenAsync(request.FacultyId, request.Name, cancellationToken))
            throw new ConflictAppException("A department with that name already exists in this faculty.");

        var department = Department.Create(EntityIds.New(), _current.TenantId, request.FacultyId, request.Name);
        _catalog.Add(department);
        await _catalog.SaveChangesAsync(cancellationToken);
        return (await _catalog.GetDepartmentAsync(department.Id, cancellationToken))!;
    }
}
