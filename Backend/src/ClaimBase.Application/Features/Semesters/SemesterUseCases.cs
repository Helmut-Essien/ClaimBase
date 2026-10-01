using ClaimBase.Application.Common;
using ClaimBase.Application.Common.Exceptions;
using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Domain.Academic;
using ClaimBase.Domain.Identity;
using ClaimBase.Shared.Academic;
using ClaimBase.Shared.Common;
using FluentValidation;
using MediatR;

namespace ClaimBase.Application.Features.Semesters;

/// <summary>Lists semesters, newest start date first.</summary>
/// <param name="Page">1-based page.</param>
/// <param name="PageSize">Page size, 1 to 100.</param>
public sealed record ListSemestersQuery(int Page, int PageSize) : IRequest<PagedResult<SemesterResponse>>;

/// <summary>Validates semester list paging.</summary>
public sealed class ListSemestersQueryValidator : AbstractValidator<ListSemestersQuery>
{
    /// <summary>Creates the paging rules.</summary>
    public ListSemestersQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, PageLimits.MaxSize);
    }
}

/// <summary>Returns one page of semesters.</summary>
public sealed class ListSemestersQueryHandler : IRequestHandler<ListSemestersQuery, PagedResult<SemesterResponse>>
{
    private readonly ISetupAccess _access;
    private readonly IAcademicCatalog _catalog;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="catalog">Academic store.</param>
    public ListSemestersQueryHandler(ISetupAccess access, IAcademicCatalog catalog)
    {
        _access = access;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<PagedResult<SemesterResponse>> Handle(ListSemestersQuery request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        return await _catalog.ListSemestersAsync(request.Page, request.PageSize, cancellationToken);
    }
}

/// <summary>Creates a draft semester.</summary>
/// <param name="Name">Semester name.</param>
/// <param name="StartDate">Inclusive first day.</param>
/// <param name="EndDate">Inclusive last day.</param>
public sealed record CreateSemesterCommand(string Name, DateOnly StartDate, DateOnly EndDate) : IRequest<SemesterResponse>;

/// <summary>Validates a new semester.</summary>
public sealed class CreateSemesterCommandValidator : AbstractValidator<CreateSemesterCommand>
{
    /// <summary>Creates the date and name rules.</summary>
    public CreateSemesterCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(AcademicConstraints.NameMaxLength);
        RuleFor(command => command.EndDate).GreaterThanOrEqualTo(command => command.StartDate);
    }
}

/// <summary>Creates a draft semester. Drafts may overlap.</summary>
public sealed class CreateSemesterCommandHandler : IRequestHandler<CreateSemesterCommand, SemesterResponse>
{
    private readonly ISetupAccess _access;
    private readonly IAcademicCatalog _catalog;
    private readonly ICurrentTenant _current;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="catalog">Academic store.</param>
    /// <param name="current">Current tenant.</param>
    public CreateSemesterCommandHandler(ISetupAccess access, IAcademicCatalog catalog, ICurrentTenant current)
    {
        _access = access;
        _catalog = catalog;
        _current = current;
    }

    /// <inheritdoc />
    public async Task<SemesterResponse> Handle(CreateSemesterCommand request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        var semester = Semester.Create(EntityIds.New(), _current.TenantId, request.Name, request.StartDate, request.EndDate);
        _catalog.Add(semester);
        await _catalog.SaveChangesAsync(cancellationToken);
        return Map(semester);
    }

    private static SemesterResponse Map(Semester semester) => new()
    {
        Id = semester.Id,
        Name = semester.Name,
        StartDate = semester.StartDate,
        EndDate = semester.EndDate,
        Status = semester.Status.ToString()
    };
}

/// <summary>Updates a semester that is not closed.</summary>
/// <param name="Id">Semester id.</param>
/// <param name="Name">Semester name.</param>
/// <param name="StartDate">Inclusive first day.</param>
/// <param name="EndDate">Inclusive last day.</param>
public sealed record UpdateSemesterCommand(string Id, string Name, DateOnly StartDate, DateOnly EndDate) : IRequest<SemesterResponse>;

/// <summary>Validates a semester update.</summary>
public sealed class UpdateSemesterCommandValidator : AbstractValidator<UpdateSemesterCommand>
{
    /// <summary>Creates the date and name rules.</summary>
    public UpdateSemesterCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().MaximumLength(UserConstraints.IdMaxLength);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(AcademicConstraints.NameMaxLength);
        RuleFor(command => command.EndDate).GreaterThanOrEqualTo(command => command.StartDate);
    }
}

/// <summary>Updates a semester. An open semester is checked again for date overlap.</summary>
public sealed class UpdateSemesterCommandHandler : IRequestHandler<UpdateSemesterCommand, SemesterResponse>
{
    private readonly ISetupAccess _access;
    private readonly IAcademicCatalog _catalog;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="catalog">Academic store.</param>
    public UpdateSemesterCommandHandler(ISetupAccess access, IAcademicCatalog catalog)
    {
        _access = access;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<SemesterResponse> Handle(UpdateSemesterCommand request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        var semester = await _catalog.FindSemesterAsync(request.Id, cancellationToken)
            ?? throw new NotFoundAppException("Semester was not found.");

        StatusChange.Run(() => semester.UpdateDetails(request.Name, request.StartDate, request.EndDate));
        if (semester.Status == SemesterStatus.Open
            && await _catalog.OpenSemesterOverlapsAsync(semester.StartDate, semester.EndDate, semester.Id, cancellationToken))
        {
            throw new ConflictAppException("Another open semester already covers these dates.");
        }

        await _catalog.SaveChangesAsync(cancellationToken);
        return CreateSemesterCommandHandlerMap(semester);
    }

    private static SemesterResponse CreateSemesterCommandHandlerMap(Semester semester) => new()
    {
        Id = semester.Id,
        Name = semester.Name,
        StartDate = semester.StartDate,
        EndDate = semester.EndDate,
        Status = semester.Status.ToString()
    };
}

/// <summary>Opens a draft semester.</summary>
/// <param name="Id">Semester id.</param>
public sealed record OpenSemesterCommand(string Id) : IRequest<SemesterResponse>;

/// <summary>Validates an open request.</summary>
public sealed class OpenSemesterCommandValidator : AbstractValidator<OpenSemesterCommand>
{
    /// <summary>Creates the id rule.</summary>
    public OpenSemesterCommandValidator() =>
        RuleFor(command => command.Id).NotEmpty().MaximumLength(UserConstraints.IdMaxLength);
}

/// <summary>Opens a draft when no other open semester covers the same dates.</summary>
public sealed class OpenSemesterCommandHandler : IRequestHandler<OpenSemesterCommand, SemesterResponse>
{
    private readonly ISetupAccess _access;
    private readonly IAcademicCatalog _catalog;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="catalog">Academic store.</param>
    public OpenSemesterCommandHandler(ISetupAccess access, IAcademicCatalog catalog)
    {
        _access = access;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<SemesterResponse> Handle(OpenSemesterCommand request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        var semester = await _catalog.FindSemesterAsync(request.Id, cancellationToken)
            ?? throw new NotFoundAppException("Semester was not found.");

        if (await _catalog.OpenSemesterOverlapsAsync(semester.StartDate, semester.EndDate, semester.Id, cancellationToken))
            throw new ConflictAppException("Another open semester already covers these dates.");

        StatusChange.Run(semester.Open);
        await _catalog.SaveChangesAsync(cancellationToken);
        return new SemesterResponse
        {
            Id = semester.Id,
            Name = semester.Name,
            StartDate = semester.StartDate,
            EndDate = semester.EndDate,
            Status = semester.Status.ToString()
        };
    }
}

/// <summary>Closes an open semester.</summary>
/// <param name="Id">Semester id.</param>
public sealed record CloseSemesterCommand(string Id) : IRequest<SemesterResponse>;

/// <summary>Validates a close request.</summary>
public sealed class CloseSemesterCommandValidator : AbstractValidator<CloseSemesterCommand>
{
    /// <summary>Creates the id rule.</summary>
    public CloseSemesterCommandValidator() =>
        RuleFor(command => command.Id).NotEmpty().MaximumLength(UserConstraints.IdMaxLength);
}

/// <summary>Closes an open semester. Closed semesters stay closed.</summary>
public sealed class CloseSemesterCommandHandler : IRequestHandler<CloseSemesterCommand, SemesterResponse>
{
    private readonly ISetupAccess _access;
    private readonly IAcademicCatalog _catalog;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="catalog">Academic store.</param>
    public CloseSemesterCommandHandler(ISetupAccess access, IAcademicCatalog catalog)
    {
        _access = access;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<SemesterResponse> Handle(CloseSemesterCommand request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        var semester = await _catalog.FindSemesterAsync(request.Id, cancellationToken)
            ?? throw new NotFoundAppException("Semester was not found.");

        StatusChange.Run(semester.Close);
        await _catalog.SaveChangesAsync(cancellationToken);
        return new SemesterResponse
        {
            Id = semester.Id,
            Name = semester.Name,
            StartDate = semester.StartDate,
            EndDate = semester.EndDate,
            Status = semester.Status.ToString()
        };
    }
}
