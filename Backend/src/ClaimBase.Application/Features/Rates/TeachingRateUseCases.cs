using ClaimBase.Application.Common;
using ClaimBase.Application.Common.Exceptions;
using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Domain.Academic;
using ClaimBase.Domain.Identity;
using ClaimBase.Domain.Rates;
using ClaimBase.Shared.Common;
using ClaimBase.Shared.Rates;
using FluentValidation;
using MediatR;

namespace ClaimBase.Application.Features.Rates;

/// <summary>Lists hourly teaching rates.</summary>
/// <param name="Page">1-based page.</param>
/// <param name="PageSize">Page size, 1 to 100.</param>
/// <param name="PositionTitleId">Optional title filter.</param>
/// <param name="QualificationId">Optional qualification filter.</param>
/// <param name="On">Optional day. Only rows in force on that day are returned.</param>
public sealed record ListTeachingRatesQuery(
    int Page,
    int PageSize,
    string? PositionTitleId,
    string? QualificationId,
    DateOnly? On) : IRequest<PagedResult<TeachingRateResponse>>;

/// <summary>Validates teaching-rate list paging and optional filters.</summary>
public sealed class ListTeachingRatesQueryValidator : AbstractValidator<ListTeachingRatesQuery>
{
    /// <summary>Creates the paging and filter rules.</summary>
    public ListTeachingRatesQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, PageLimits.MaxSize);
        RuleFor(query => query.PositionTitleId!)
            .MaximumLength(UserConstraints.IdMaxLength)
            .When(query => !string.IsNullOrWhiteSpace(query.PositionTitleId));
        RuleFor(query => query.QualificationId!)
            .MaximumLength(UserConstraints.IdMaxLength)
            .When(query => !string.IsNullOrWhiteSpace(query.QualificationId));
        // A missing query date must not bind as year 1 and hide every real row.
        RuleFor(query => query.On)
            .Must(on => on is null || on.Value != default)
            .WithMessage("On must be a calendar date.");
    }
}

/// <summary>Returns one page of teaching rates for the current tenant.</summary>
public sealed class ListTeachingRatesQueryHandler : IRequestHandler<ListTeachingRatesQuery, PagedResult<TeachingRateResponse>>
{
    private readonly ISetupAccess _access;
    private readonly IRateSchedule _schedule;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="schedule">Rate store.</param>
    public ListTeachingRatesQueryHandler(ISetupAccess access, IRateSchedule schedule)
    {
        _access = access;
        _schedule = schedule;
    }

    /// <inheritdoc />
    public async Task<PagedResult<TeachingRateResponse>> Handle(ListTeachingRatesQuery request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        return await _schedule.ListTeachingAsync(
            request.Page,
            request.PageSize,
            request.PositionTitleId,
            request.QualificationId,
            request.On,
            cancellationToken);
    }
}

/// <summary>Adds an hourly rate for one position title and one qualification.</summary>
/// <param name="PositionTitleId">Shared title.</param>
/// <param name="QualificationId">Course qualification.</param>
/// <param name="Amount">Cedis per hour.</param>
/// <param name="EffectiveFrom">First day included.</param>
/// <param name="EffectiveTo">First day excluded, or null.</param>
public sealed record CreateTeachingRateCommand(
    string PositionTitleId,
    string QualificationId,
    decimal Amount,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo) : IRequest<TeachingRateResponse>;

/// <summary>Validates a new teaching rate.</summary>
public sealed class CreateTeachingRateCommandValidator : AbstractValidator<CreateTeachingRateCommand>
{
    /// <summary>Creates the amount, id, and date rules.</summary>
    public CreateTeachingRateCommandValidator()
    {
        RuleFor(command => command.PositionTitleId).NotEmpty().MaximumLength(UserConstraints.IdMaxLength);
        RuleFor(command => command.QualificationId).NotEmpty().MaximumLength(UserConstraints.IdMaxLength);
        RuleFor(command => command.Amount)
            .Must(RateConstraints.IsStorable)
            .WithMessage("Amount must be zero or greater with at most 2 decimal places.");
        // [Required] does not reject a missing DateOnly. Model binding leaves 0001-01-01.
        RuleFor(command => command.EffectiveFrom).Must(date => date != default).WithMessage("EffectiveFrom is required.");
        RuleFor(command => command.EffectiveTo)
            .Must((command, effectiveTo) => effectiveTo is null || effectiveTo.Value > command.EffectiveFrom)
            .WithMessage("EffectiveTo must be after EffectiveFrom.");
    }
}

/// <summary>Adds a teaching rate when its dates do not overlap another row for the same title and qualification.</summary>
public sealed class CreateTeachingRateCommandHandler : IRequestHandler<CreateTeachingRateCommand, TeachingRateResponse>
{
    private readonly ISetupAccess _access;
    private readonly IRateSchedule _schedule;
    private readonly ICurrentTenant _current;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="schedule">Rate store.</param>
    /// <param name="current">Current tenant.</param>
    public CreateTeachingRateCommandHandler(ISetupAccess access, IRateSchedule schedule, ICurrentTenant current)
    {
        _access = access;
        _schedule = schedule;
        _current = current;
    }

    /// <inheritdoc />
    public async Task<TeachingRateResponse> Handle(CreateTeachingRateCommand request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        if (!await _schedule.PositionTitleExistsAsync(request.PositionTitleId, cancellationToken))
            throw new NotFoundAppException("Position title was not found.");

        if (!await _schedule.QualificationExistsAsync(request.QualificationId, cancellationToken))
            throw new NotFoundAppException("Qualification was not found.");

        // Overlapping ranges for the same position and qualification are rejected.
        // The lock is held across the read and the save so two creates cannot both pass the check.
        return await _schedule.LockTimelineAsync(
            $"{_current.TenantId}|teaching|{request.PositionTitleId}|{request.QualificationId}",
            token => SaveTeachingAsync(request, token),
            cancellationToken);
    }

    private async Task<TeachingRateResponse> SaveTeachingAsync(CreateTeachingRateCommand request, CancellationToken cancellationToken)
    {
        var rate = TeachingRate.Create(
            EntityIds.New(),
            _current.TenantId,
            request.PositionTitleId,
            request.QualificationId,
            request.Amount,
            request.EffectiveFrom,
            request.EffectiveTo);

        var existing = await _schedule.ListTeachingForUpdateAsync(request.PositionTitleId, request.QualificationId, cancellationToken);
        // A new amount closes the open-ended row it replaces. The previous end becomes this row's start.
        // A row that already has an end is left alone, so a true overlap is still rejected.
        ReplaceOpenEnded(existing, rate.Range);
        if (DateRange.OverlapsAny(rate.Range, existing.Select(item => item.Range)))
            throw new ConflictAppException("Those teaching dates overlap an existing rate for this position and qualification.");

        _schedule.Add(rate);
        await _schedule.SaveChangesAsync(cancellationToken);
        return await _schedule.GetTeachingAsync(rate.Id, cancellationToken)
            ?? throw new NotFoundAppException("Teaching rate was not found.");
    }

    private static void ReplaceOpenEnded(IEnumerable<TeachingRate> existing, DateRange candidate)
    {
        foreach (var prior in existing)
        {
            if (prior.EffectiveTo is null && candidate.Start > prior.EffectiveFrom && prior.Range.Overlaps(candidate))
                prior.EndOn(candidate.Start);
        }
    }
}
