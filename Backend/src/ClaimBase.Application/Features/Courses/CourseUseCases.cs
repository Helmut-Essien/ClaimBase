using ClaimBase.Application.Common;
using ClaimBase.Application.Common.Exceptions;
using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Domain.Academic;
using ClaimBase.Domain.Identity;
using ClaimBase.Shared.Academic;
using ClaimBase.Shared.Common;
using FluentValidation;
using MediatR;

namespace ClaimBase.Application.Features.Courses;

/// <summary>Lists courses.</summary>
/// <param name="Page">1-based page.</param>
/// <param name="PageSize">Page size, 1 to 100.</param>
public sealed record ListCoursesQuery(int Page, int PageSize) : IRequest<PagedResult<CourseResponse>>;

/// <summary>Validates course list paging.</summary>
public sealed class ListCoursesQueryValidator : AbstractValidator<ListCoursesQuery>
{
    /// <summary>Creates the paging rules.</summary>
    public ListCoursesQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, PageLimits.MaxSize);
    }
}

/// <summary>Returns one page of courses.</summary>
public sealed class ListCoursesQueryHandler : IRequestHandler<ListCoursesQuery, PagedResult<CourseResponse>>
{
    private readonly ISetupAccess _access;
    private readonly IAcademicCatalog _catalog;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="catalog">Academic store.</param>
    public ListCoursesQueryHandler(ISetupAccess access, IAcademicCatalog catalog)
    {
        _access = access;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<PagedResult<CourseResponse>> Handle(ListCoursesQuery request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        return await _catalog.ListCoursesAsync(request.Page, request.PageSize, cancellationToken);
    }
}

/// <summary>Creates a course.</summary>
/// <param name="Code">Course code. Stored uppercase.</param>
/// <param name="Name">Course name.</param>
/// <param name="QualificationId">Qualification used by the rate matrix.</param>
public sealed record CreateCourseCommand(string Code, string Name, string QualificationId) : IRequest<CourseResponse>;

/// <summary>Validates a new course.</summary>
public sealed class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    /// <summary>Creates the course rules.</summary>
    public CreateCourseCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(AcademicConstraints.CourseCodeMaxLength);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(AcademicConstraints.NameMaxLength);
        RuleFor(command => command.QualificationId).NotEmpty().MaximumLength(UserConstraints.IdMaxLength);
    }
}

/// <summary>Creates a course when the qualification exists and the code is free.</summary>
public sealed class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, CourseResponse>
{
    private readonly ISetupAccess _access;
    private readonly IAcademicCatalog _catalog;
    private readonly ICurrentTenant _current;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="catalog">Academic store.</param>
    /// <param name="current">Current tenant.</param>
    public CreateCourseCommandHandler(ISetupAccess access, IAcademicCatalog catalog, ICurrentTenant current)
    {
        _access = access;
        _catalog = catalog;
        _current = current;
    }

    /// <inheritdoc />
    public async Task<CourseResponse> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        if (!await _catalog.QualificationExistsAsync(request.QualificationId, cancellationToken))
            throw new NotFoundAppException("Qualification was not found.");

        if (await _catalog.CourseCodeTakenAsync(request.Code, null, cancellationToken))
            throw new ConflictAppException("A course with that code already exists.");

        var course = Course.Create(EntityIds.New(), _current.TenantId, request.Code, request.Name, request.QualificationId);
        _catalog.Add(course);
        await _catalog.SaveChangesAsync(cancellationToken);
        return (await _catalog.GetCourseAsync(course.Id, cancellationToken))!;
    }
}

/// <summary>Updates a course.</summary>
/// <param name="Id">Course id.</param>
/// <param name="Code">Course code. Stored uppercase.</param>
/// <param name="Name">Course name.</param>
/// <param name="QualificationId">Qualification used by the rate matrix.</param>
public sealed record UpdateCourseCommand(string Id, string Code, string Name, string QualificationId) : IRequest<CourseResponse>;

/// <summary>Validates a course update.</summary>
public sealed class UpdateCourseCommandValidator : AbstractValidator<UpdateCourseCommand>
{
    /// <summary>Creates the course rules.</summary>
    public UpdateCourseCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().MaximumLength(UserConstraints.IdMaxLength);
        RuleFor(command => command.Code).NotEmpty().MaximumLength(AcademicConstraints.CourseCodeMaxLength);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(AcademicConstraints.NameMaxLength);
        RuleFor(command => command.QualificationId).NotEmpty().MaximumLength(UserConstraints.IdMaxLength);
    }
}

/// <summary>Updates a course when the qualification exists and the code stays unique.</summary>
public sealed class UpdateCourseCommandHandler : IRequestHandler<UpdateCourseCommand, CourseResponse>
{
    private readonly ISetupAccess _access;
    private readonly IAcademicCatalog _catalog;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="catalog">Academic store.</param>
    public UpdateCourseCommandHandler(ISetupAccess access, IAcademicCatalog catalog)
    {
        _access = access;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<CourseResponse> Handle(UpdateCourseCommand request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        var course = await _catalog.FindCourseAsync(request.Id, cancellationToken)
            ?? throw new NotFoundAppException("Course was not found.");

        if (!await _catalog.QualificationExistsAsync(request.QualificationId, cancellationToken))
            throw new NotFoundAppException("Qualification was not found.");

        if (await _catalog.CourseCodeTakenAsync(request.Code, course.Id, cancellationToken))
            throw new ConflictAppException("A course with that code already exists.");

        course.Update(request.Code, request.Name, request.QualificationId);
        await _catalog.SaveChangesAsync(cancellationToken);
        return (await _catalog.GetCourseAsync(course.Id, cancellationToken))!;
    }
}
