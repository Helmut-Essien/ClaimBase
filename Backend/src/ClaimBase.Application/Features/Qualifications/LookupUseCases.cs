using ClaimBase.Application.Common;
using ClaimBase.Application.Common.Exceptions;
using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Domain.Academic;
using ClaimBase.Domain.Identity;
using ClaimBase.Shared.Academic;
using ClaimBase.Shared.Common;
using FluentValidation;
using MediatR;

namespace ClaimBase.Application.Features.Qualifications;

/// <summary>Lists qualifications.</summary>
/// <param name="Page">1-based page.</param>
/// <param name="PageSize">Page size, 1 to 100.</param>
public sealed record ListQualificationsQuery(int Page, int PageSize) : IRequest<PagedResult<QualificationResponse>>;

/// <summary>Validates qualification list paging.</summary>
public sealed class ListQualificationsQueryValidator : AbstractValidator<ListQualificationsQuery>
{
    /// <summary>Creates the paging rules.</summary>
    public ListQualificationsQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, PageLimits.MaxSize);
    }
}

/// <summary>Returns one page of qualifications.</summary>
public sealed class ListQualificationsQueryHandler : IRequestHandler<ListQualificationsQuery, PagedResult<QualificationResponse>>
{
    private readonly ISetupAccess _access;
    private readonly IAcademicCatalog _catalog;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="catalog">Academic store.</param>
    public ListQualificationsQueryHandler(ISetupAccess access, IAcademicCatalog catalog)
    {
        _access = access;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<PagedResult<QualificationResponse>> Handle(ListQualificationsQuery request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        return await _catalog.ListQualificationsAsync(request.Page, request.PageSize, cancellationToken);
    }
}

/// <summary>Creates a qualification.</summary>
/// <param name="Name">Qualification name.</param>
public sealed record CreateQualificationCommand(string Name) : IRequest<QualificationResponse>;

/// <summary>Validates a qualification name.</summary>
public sealed class CreateQualificationCommandValidator : AbstractValidator<CreateQualificationCommand>
{
    /// <summary>Creates the name rule.</summary>
    public CreateQualificationCommandValidator() =>
        RuleFor(command => command.Name).NotEmpty().MaximumLength(AcademicConstraints.QualificationNameMaxLength);
}

/// <summary>Creates a qualification when the name is free.</summary>
public sealed class CreateQualificationCommandHandler : IRequestHandler<CreateQualificationCommand, QualificationResponse>
{
    private readonly ISetupAccess _access;
    private readonly IAcademicCatalog _catalog;
    private readonly ICurrentTenant _current;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="catalog">Academic store.</param>
    /// <param name="current">Current tenant.</param>
    public CreateQualificationCommandHandler(ISetupAccess access, IAcademicCatalog catalog, ICurrentTenant current)
    {
        _access = access;
        _catalog = catalog;
        _current = current;
    }

    /// <inheritdoc />
    public async Task<QualificationResponse> Handle(CreateQualificationCommand request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        if (await _catalog.QualificationNameTakenAsync(request.Name, cancellationToken))
            throw new ConflictAppException("A qualification with that name already exists.");

        var qualification = Qualification.Create(EntityIds.New(), _current.TenantId, request.Name);
        _catalog.Add(qualification);
        await _catalog.SaveChangesAsync(cancellationToken);
        return new QualificationResponse { Id = qualification.Id, Name = qualification.Name };
    }
}

/// <summary>Lists position titles.</summary>
/// <param name="Page">1-based page.</param>
/// <param name="PageSize">Page size, 1 to 100.</param>
public sealed record ListPositionTitlesQuery(int Page, int PageSize) : IRequest<PagedResult<PositionTitleResponse>>;

/// <summary>Validates position-title list paging.</summary>
public sealed class ListPositionTitlesQueryValidator : AbstractValidator<ListPositionTitlesQuery>
{
    /// <summary>Creates the paging rules.</summary>
    public ListPositionTitlesQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, PageLimits.MaxSize);
    }
}

/// <summary>Returns one page of position titles.</summary>
public sealed class ListPositionTitlesQueryHandler : IRequestHandler<ListPositionTitlesQuery, PagedResult<PositionTitleResponse>>
{
    private readonly ISetupAccess _access;
    private readonly IAcademicCatalog _catalog;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="catalog">Academic store.</param>
    public ListPositionTitlesQueryHandler(ISetupAccess access, IAcademicCatalog catalog)
    {
        _access = access;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<PagedResult<PositionTitleResponse>> Handle(ListPositionTitlesQuery request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        return await _catalog.ListPositionTitlesAsync(request.Page, request.PageSize, cancellationToken);
    }
}

/// <summary>Creates a position title.</summary>
/// <param name="Name">Title name.</param>
public sealed record CreatePositionTitleCommand(string Name) : IRequest<PositionTitleResponse>;

/// <summary>Validates a position title.</summary>
public sealed class CreatePositionTitleCommandValidator : AbstractValidator<CreatePositionTitleCommand>
{
    /// <summary>Creates the name rule.</summary>
    public CreatePositionTitleCommandValidator() =>
        RuleFor(command => command.Name).NotEmpty().MaximumLength(AcademicConstraints.PositionTitleNameMaxLength);
}

/// <summary>Creates a position title when the name is free.</summary>
public sealed class CreatePositionTitleCommandHandler : IRequestHandler<CreatePositionTitleCommand, PositionTitleResponse>
{
    private readonly ISetupAccess _access;
    private readonly IAcademicCatalog _catalog;
    private readonly ICurrentTenant _current;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="catalog">Academic store.</param>
    /// <param name="current">Current tenant.</param>
    public CreatePositionTitleCommandHandler(ISetupAccess access, IAcademicCatalog catalog, ICurrentTenant current)
    {
        _access = access;
        _catalog = catalog;
        _current = current;
    }

    /// <inheritdoc />
    public async Task<PositionTitleResponse> Handle(CreatePositionTitleCommand request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        if (await _catalog.PositionTitleNameTakenAsync(request.Name, cancellationToken))
            throw new ConflictAppException("A position title with that name already exists.");

        var title = PositionTitle.Create(EntityIds.New(), _current.TenantId, request.Name);
        _catalog.Add(title);
        await _catalog.SaveChangesAsync(cancellationToken);
        return new PositionTitleResponse { Id = title.Id, Name = title.Name };
    }
}
