using ClaimBase.Application.Common;
using ClaimBase.Application.Common.Exceptions;
using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Domain.Academic;
using ClaimBase.Domain.Identity;
using ClaimBase.Shared.Academic;
using ClaimBase.Shared.Common;
using FluentValidation;
using MediatR;

namespace ClaimBase.Application.Features.Staff;

/// <summary>Lists staff.</summary>
/// <param name="Page">1-based page.</param>
/// <param name="PageSize">Page size, 1 to 100.</param>
public sealed record ListStaffQuery(int Page, int PageSize) : IRequest<PagedResult<StaffResponse>>;

/// <summary>Validates staff list paging.</summary>
public sealed class ListStaffQueryValidator : AbstractValidator<ListStaffQuery>
{
    /// <summary>Creates the paging rules.</summary>
    public ListStaffQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, PageLimits.MaxSize);
    }
}

/// <summary>Returns one page of staff.</summary>
public sealed class ListStaffQueryHandler : IRequestHandler<ListStaffQuery, PagedResult<StaffResponse>>
{
    private readonly ISetupAccess _access;
    private readonly IAcademicCatalog _catalog;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="catalog">Academic store.</param>
    public ListStaffQueryHandler(ISetupAccess access, IAcademicCatalog catalog)
    {
        _access = access;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<PagedResult<StaffResponse>> Handle(ListStaffQuery request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        return await _catalog.ListStaffAsync(request.Page, request.PageSize, cancellationToken);
    }
}

/// <summary>Creates a lecturer with at least one department.</summary>
/// <param name="StaffNumber">Staff number.</param>
/// <param name="DisplayName">Display name.</param>
/// <param name="Email">Optional email.</param>
/// <param name="EmploymentType"><c>PartTime</c> or <c>FullTime</c>.</param>
/// <param name="BiometricId">Optional device id.</param>
/// <param name="DepartmentIds">Departments. At least one, with no duplicates.</param>
public sealed record CreateStaffCommand(
    string StaffNumber,
    string DisplayName,
    string? Email,
    string EmploymentType,
    string? BiometricId,
    IReadOnlyList<string> DepartmentIds) : IRequest<StaffResponse>;

/// <summary>Validates a new lecturer.</summary>
public sealed class CreateStaffCommandValidator : AbstractValidator<CreateStaffCommand>
{
    /// <summary>Creates the staff rules.</summary>
    public CreateStaffCommandValidator()
    {
        RuleFor(command => command.StaffNumber).NotEmpty().MaximumLength(AcademicConstraints.StaffNumberMaxLength);
        RuleFor(command => command.DisplayName).NotEmpty().MaximumLength(AcademicConstraints.NameMaxLength);
        RuleFor(command => command.Email).MaximumLength(AcademicFieldLimits.Email);
        RuleFor(command => command.Email)
            .Must(email => string.IsNullOrWhiteSpace(email) || StaffEmail.IsValid(email))
            .WithMessage("'{PropertyName}' is not a valid email address.");
        RuleFor(command => command.EmploymentType).Must(value => value is "PartTime" or "FullTime");
        RuleFor(command => command.BiometricId).MaximumLength(AcademicConstraints.BiometricIdMaxLength);
        RuleFor(command => command.DepartmentIds).NotEmpty();
        RuleForEach(command => command.DepartmentIds).NotEmpty().MaximumLength(UserConstraints.IdMaxLength);
        RuleFor(command => command.DepartmentIds).Must(ids => ids.Distinct().Count() == ids.Count);
    }
}

/// <summary>Creates a lecturer and the department assignments in one save.</summary>
public sealed class CreateStaffCommandHandler : IRequestHandler<CreateStaffCommand, StaffResponse>
{
    private readonly ISetupAccess _access;
    private readonly IAcademicCatalog _catalog;
    private readonly ICurrentTenant _current;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="catalog">Academic store.</param>
    /// <param name="current">Current tenant.</param>
    public CreateStaffCommandHandler(ISetupAccess access, IAcademicCatalog catalog, ICurrentTenant current)
    {
        _access = access;
        _catalog = catalog;
        _current = current;
    }

    /// <inheritdoc />
    public async Task<StaffResponse> Handle(CreateStaffCommand request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        foreach (var departmentId in request.DepartmentIds)
        {
            if (!await _catalog.DepartmentExistsAsync(departmentId, cancellationToken))
                throw new NotFoundAppException("Department was not found.");
        }

        if (await _catalog.StaffNumberTakenAsync(request.StaffNumber, null, cancellationToken))
            throw new ConflictAppException("That staff number is already in use.");

        if (!string.IsNullOrWhiteSpace(request.BiometricId)
            && await _catalog.BiometricIdTakenAsync(request.BiometricId, null, cancellationToken))
        {
            throw new ConflictAppException("That biometric id is already in use.");
        }

        var staff = Domain.Academic.Staff.Create(
            EntityIds.New(),
            _current.TenantId,
            request.StaffNumber,
            request.DisplayName,
            request.Email,
            ParseEmployment(request.EmploymentType),
            request.BiometricId);
        _catalog.Add(staff);
        foreach (var departmentId in request.DepartmentIds)
            _catalog.Add(StaffDepartment.Create(EntityIds.New(), _current.TenantId, staff.Id, departmentId));

        await _catalog.SaveChangesAsync(cancellationToken);
        return (await _catalog.GetStaffAsync(staff.Id, cancellationToken))!;
    }

    private static EmploymentType ParseEmployment(string value) =>
        value == "FullTime" ? EmploymentType.FullTime : EmploymentType.PartTime;
}

/// <summary>Updates a lecturer. Departments stay on their own routes.</summary>
/// <param name="Id">Staff id.</param>
/// <param name="StaffNumber">Staff number.</param>
/// <param name="DisplayName">Display name.</param>
/// <param name="Email">Optional email.</param>
/// <param name="EmploymentType"><c>PartTime</c> or <c>FullTime</c>.</param>
/// <param name="BiometricId">Optional device id. Blank clears it.</param>
public sealed record UpdateStaffCommand(
    string Id,
    string StaffNumber,
    string DisplayName,
    string? Email,
    string EmploymentType,
    string? BiometricId) : IRequest<StaffResponse>;

/// <summary>Validates a staff update.</summary>
public sealed class UpdateStaffCommandValidator : AbstractValidator<UpdateStaffCommand>
{
    /// <summary>Creates the staff rules.</summary>
    public UpdateStaffCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().MaximumLength(UserConstraints.IdMaxLength);
        RuleFor(command => command.StaffNumber).NotEmpty().MaximumLength(AcademicConstraints.StaffNumberMaxLength);
        RuleFor(command => command.DisplayName).NotEmpty().MaximumLength(AcademicConstraints.NameMaxLength);
        RuleFor(command => command.Email).MaximumLength(AcademicFieldLimits.Email);
        RuleFor(command => command.Email)
            .Must(email => string.IsNullOrWhiteSpace(email) || StaffEmail.IsValid(email))
            .WithMessage("'{PropertyName}' is not a valid email address.");
        RuleFor(command => command.EmploymentType).Must(value => value is "PartTime" or "FullTime");
        RuleFor(command => command.BiometricId).MaximumLength(AcademicConstraints.BiometricIdMaxLength);
    }
}

/// <summary>Updates a lecturer when the staff number and biometric id stay unique.</summary>
public sealed class UpdateStaffCommandHandler : IRequestHandler<UpdateStaffCommand, StaffResponse>
{
    private readonly ISetupAccess _access;
    private readonly IAcademicCatalog _catalog;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="catalog">Academic store.</param>
    public UpdateStaffCommandHandler(ISetupAccess access, IAcademicCatalog catalog)
    {
        _access = access;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<StaffResponse> Handle(UpdateStaffCommand request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        var staff = await _catalog.FindStaffAsync(request.Id, cancellationToken)
            ?? throw new NotFoundAppException("Staff was not found.");

        if (await _catalog.StaffNumberTakenAsync(request.StaffNumber, staff.Id, cancellationToken))
            throw new ConflictAppException("That staff number is already in use.");

        if (!string.IsNullOrWhiteSpace(request.BiometricId)
            && await _catalog.BiometricIdTakenAsync(request.BiometricId, staff.Id, cancellationToken))
        {
            throw new ConflictAppException("That biometric id is already in use.");
        }

        staff.Update(
            request.StaffNumber,
            request.DisplayName,
            request.Email,
            request.EmploymentType == "FullTime" ? EmploymentType.FullTime : EmploymentType.PartTime,
            request.BiometricId);
        await _catalog.SaveChangesAsync(cancellationToken);
        return (await _catalog.GetStaffAsync(staff.Id, cancellationToken))!;
    }
}

/// <summary>Assigns another department to a lecturer.</summary>
/// <param name="StaffId">Staff id.</param>
/// <param name="DepartmentId">Department id.</param>
public sealed record AssignDepartmentCommand(string StaffId, string DepartmentId) : IRequest<StaffResponse>;

/// <summary>Validates a department assignment.</summary>
public sealed class AssignDepartmentCommandValidator : AbstractValidator<AssignDepartmentCommand>
{
    /// <summary>Creates the id rules.</summary>
    public AssignDepartmentCommandValidator()
    {
        RuleFor(command => command.StaffId).NotEmpty().MaximumLength(UserConstraints.IdMaxLength);
        RuleFor(command => command.DepartmentId).NotEmpty().MaximumLength(UserConstraints.IdMaxLength);
    }
}

/// <summary>Assigns a department when the lecturer does not already have it.</summary>
public sealed class AssignDepartmentCommandHandler : IRequestHandler<AssignDepartmentCommand, StaffResponse>
{
    private readonly ISetupAccess _access;
    private readonly IAcademicCatalog _catalog;
    private readonly ICurrentTenant _current;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="catalog">Academic store.</param>
    /// <param name="current">Current tenant.</param>
    public AssignDepartmentCommandHandler(ISetupAccess access, IAcademicCatalog catalog, ICurrentTenant current)
    {
        _access = access;
        _catalog = catalog;
        _current = current;
    }

    /// <inheritdoc />
    public async Task<StaffResponse> Handle(AssignDepartmentCommand request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        if (await _catalog.FindStaffAsync(request.StaffId, cancellationToken) is null)
            throw new NotFoundAppException("Staff was not found.");

        if (!await _catalog.DepartmentExistsAsync(request.DepartmentId, cancellationToken))
            throw new NotFoundAppException("Department was not found.");

        if (await _catalog.AssignmentExistsAsync(request.StaffId, request.DepartmentId, cancellationToken))
            throw new ConflictAppException("That department is already assigned.");

        _catalog.Add(StaffDepartment.Create(EntityIds.New(), _current.TenantId, request.StaffId, request.DepartmentId));
        await _catalog.SaveChangesAsync(cancellationToken);
        return (await _catalog.GetStaffAsync(request.StaffId, cancellationToken))!;
    }
}

/// <summary>Removes one department assignment. The last assignment is rejected.</summary>
/// <param name="StaffId">Staff id.</param>
/// <param name="DepartmentId">Department id.</param>
public sealed record RemoveDepartmentCommand(string StaffId, string DepartmentId) : IRequest<StaffResponse>;

/// <summary>Validates a department removal.</summary>
public sealed class RemoveDepartmentCommandValidator : AbstractValidator<RemoveDepartmentCommand>
{
    /// <summary>Creates the id rules.</summary>
    public RemoveDepartmentCommandValidator()
    {
        RuleFor(command => command.StaffId).NotEmpty().MaximumLength(UserConstraints.IdMaxLength);
        RuleFor(command => command.DepartmentId).NotEmpty().MaximumLength(UserConstraints.IdMaxLength);
    }
}

/// <summary>Removes an assignment when the lecturer would still have one department.</summary>
public sealed class RemoveDepartmentCommandHandler : IRequestHandler<RemoveDepartmentCommand, StaffResponse>
{
    private readonly ISetupAccess _access;
    private readonly IAcademicCatalog _catalog;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="catalog">Academic store.</param>
    public RemoveDepartmentCommandHandler(ISetupAccess access, IAcademicCatalog catalog)
    {
        _access = access;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<StaffResponse> Handle(RemoveDepartmentCommand request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        if (await _catalog.FindStaffAsync(request.StaffId, cancellationToken) is null)
            throw new NotFoundAppException("Staff was not found.");

        if (await _catalog.CountDepartmentsAsync(request.StaffId, cancellationToken) <= 1)
            throw new ConflictAppException("The last department assignment cannot be removed.");

        if (!await _catalog.RemoveAssignmentAsync(request.StaffId, request.DepartmentId, cancellationToken))
            throw new NotFoundAppException("Department assignment was not found.");

        await _catalog.SaveChangesAsync(cancellationToken);
        return (await _catalog.GetStaffAsync(request.StaffId, cancellationToken))!;
    }
}

/// <summary>Lists appointments for one lecturer.</summary>
/// <param name="StaffId">Staff id.</param>
public sealed record ListPositionsQuery(string StaffId) : IRequest<IReadOnlyList<StaffPositionResponse>>;

/// <summary>Validates a position list.</summary>
public sealed class ListPositionsQueryValidator : AbstractValidator<ListPositionsQuery>
{
    /// <summary>Creates the id rule.</summary>
    public ListPositionsQueryValidator() =>
        RuleFor(query => query.StaffId).NotEmpty().MaximumLength(UserConstraints.IdMaxLength);
}

/// <summary>Returns appointments for one lecturer.</summary>
public sealed class ListPositionsQueryHandler : IRequestHandler<ListPositionsQuery, IReadOnlyList<StaffPositionResponse>>
{
    private readonly ISetupAccess _access;
    private readonly IAcademicCatalog _catalog;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="catalog">Academic store.</param>
    public ListPositionsQueryHandler(ISetupAccess access, IAcademicCatalog catalog)
    {
        _access = access;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StaffPositionResponse>> Handle(ListPositionsQuery request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        if (await _catalog.FindStaffAsync(request.StaffId, cancellationToken) is null)
            throw new NotFoundAppException("Staff was not found.");

        return await _catalog.ListPositionsAsync(request.StaffId, cancellationToken);
    }
}

/// <summary>Adds a position appointment.</summary>
/// <param name="StaffId">Staff id.</param>
/// <param name="PositionTitleId">Shared title.</param>
/// <param name="EffectiveFrom">First day included.</param>
/// <param name="EffectiveTo">First day excluded, or null.</param>
public sealed record CreateStaffPositionCommand(
    string StaffId,
    string PositionTitleId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo) : IRequest<StaffPositionResponse>;

/// <summary>Validates a new appointment.</summary>
public sealed class CreateStaffPositionCommandValidator : AbstractValidator<CreateStaffPositionCommand>
{
    /// <summary>Creates the appointment rules.</summary>
    public CreateStaffPositionCommandValidator()
    {
        RuleFor(command => command.StaffId).NotEmpty().MaximumLength(UserConstraints.IdMaxLength);
        RuleFor(command => command.PositionTitleId).NotEmpty().MaximumLength(UserConstraints.IdMaxLength);
        RuleFor(command => command.EffectiveTo)
            .Must((command, effectiveTo) => effectiveTo is null || effectiveTo.Value > command.EffectiveFrom);
    }
}

/// <summary>Adds an appointment when its half-open dates do not overlap an existing one.</summary>
public sealed class CreateStaffPositionCommandHandler : IRequestHandler<CreateStaffPositionCommand, StaffPositionResponse>
{
    private readonly ISetupAccess _access;
    private readonly IAcademicCatalog _catalog;
    private readonly ICurrentTenant _current;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="catalog">Academic store.</param>
    /// <param name="current">Current tenant.</param>
    public CreateStaffPositionCommandHandler(ISetupAccess access, IAcademicCatalog catalog, ICurrentTenant current)
    {
        _access = access;
        _catalog = catalog;
        _current = current;
    }

    /// <inheritdoc />
    public async Task<StaffPositionResponse> Handle(CreateStaffPositionCommand request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        if (await _catalog.FindStaffAsync(request.StaffId, cancellationToken) is null)
            throw new NotFoundAppException("Staff was not found.");

        if (!await _catalog.PositionTitleExistsAsync(request.PositionTitleId, cancellationToken))
            throw new NotFoundAppException("Position title was not found.");

        var position = StaffPosition.Create(
            EntityIds.New(),
            _current.TenantId,
            request.StaffId,
            request.PositionTitleId,
            request.EffectiveFrom,
            request.EffectiveTo);
        var existing = await _catalog.ListPositionRangesAsync(request.StaffId, cancellationToken);
        if (DateRange.OverlapsAny(position.Range, existing))
            throw new ConflictAppException("Those position dates overlap another appointment.");

        _catalog.Add(position);
        await _catalog.SaveChangesAsync(cancellationToken);
        var positions = await _catalog.ListPositionsAsync(request.StaffId, cancellationToken);
        return positions.Single(item => item.Id == position.Id);
    }
}

/// <summary>Optional staff email check. Blank is allowed. Surrounding spaces are ignored.</summary>
internal static class StaffEmail
{
    public static bool IsValid(string? email)
    {
        var trimmed = email?.Trim() ?? "";
        return trimmed.Contains('@') && !trimmed.StartsWith('@') && !trimmed.EndsWith('@');
    }
}
