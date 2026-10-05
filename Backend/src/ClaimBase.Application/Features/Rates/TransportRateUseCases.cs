using ClaimBase.Application.Common;
using ClaimBase.Application.Common.Exceptions;
using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Domain.Academic;
using ClaimBase.Domain.Rates;
using ClaimBase.Shared.Common;
using ClaimBase.Shared.Rates;
using FluentValidation;
using MediatR;

namespace ClaimBase.Application.Features.Rates;

/// <summary>Lists the tenant transport timeline.</summary>
/// <param name="Page">1-based page.</param>
/// <param name="PageSize">Page size, 1 to 100.</param>
public sealed record ListTransportRatesQuery(int Page, int PageSize) : IRequest<PagedResult<TransportRateResponse>>;

/// <summary>Validates transport-rate list paging.</summary>
public sealed class ListTransportRatesQueryValidator : AbstractValidator<ListTransportRatesQuery>
{
    /// <summary>Creates the paging rules.</summary>
    public ListTransportRatesQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, PageLimits.MaxSize);
    }
}

/// <summary>Returns one page of transport rates for the current tenant.</summary>
public sealed class ListTransportRatesQueryHandler : IRequestHandler<ListTransportRatesQuery, PagedResult<TransportRateResponse>>
{
    private readonly ISetupAccess _access;
    private readonly IRateSchedule _schedule;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="schedule">Rate store.</param>
    public ListTransportRatesQueryHandler(ISetupAccess access, IRateSchedule schedule)
    {
        _access = access;
        _schedule = schedule;
    }

    /// <inheritdoc />
    public async Task<PagedResult<TransportRateResponse>> Handle(ListTransportRatesQuery request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        return await _schedule.ListTransportAsync(request.Page, request.PageSize, cancellationToken);
    }
}

/// <summary>Adds one transport amount to the tenant timeline.</summary>
/// <param name="Amount">Cedis per teaching day.</param>
/// <param name="EffectiveFrom">First day included.</param>
/// <param name="EffectiveTo">First day excluded, or null.</param>
public sealed record CreateTransportRateCommand(
    decimal Amount,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo) : IRequest<TransportRateResponse>;

/// <summary>Validates a new transport rate.</summary>
public sealed class CreateTransportRateCommandValidator : AbstractValidator<CreateTransportRateCommand>
{
    /// <summary>Creates the amount and date rules.</summary>
    public CreateTransportRateCommandValidator()
    {
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

/// <summary>Adds a transport rate when its dates do not overlap the tenant timeline.</summary>
public sealed class CreateTransportRateCommandHandler : IRequestHandler<CreateTransportRateCommand, TransportRateResponse>
{
    private readonly ISetupAccess _access;
    private readonly IRateSchedule _schedule;
    private readonly ICurrentTenant _current;

    /// <summary>Creates the handler.</summary>
    /// <param name="access">Role guard.</param>
    /// <param name="schedule">Rate store.</param>
    /// <param name="current">Current tenant.</param>
    public CreateTransportRateCommandHandler(ISetupAccess access, IRateSchedule schedule, ICurrentTenant current)
    {
        _access = access;
        _schedule = schedule;
        _current = current;
    }

    /// <inheritdoc />
    public async Task<TransportRateResponse> Handle(CreateTransportRateCommand request, CancellationToken cancellationToken)
    {
        await _access.EnsureAsync(cancellationToken);
        // Transport is one timeline per tenant. The lock is held across the read and the save so two creates cannot both pass.
        return await _schedule.LockTimelineAsync(
            $"{_current.TenantId}|transport",
            token => SaveTransportAsync(request, token),
            cancellationToken);
    }

    private async Task<TransportRateResponse> SaveTransportAsync(CreateTransportRateCommand request, CancellationToken cancellationToken)
    {
        var rate = TransportRate.Create(
            EntityIds.New(),
            _current.TenantId,
            request.Amount,
            request.EffectiveFrom,
            request.EffectiveTo);

        var existing = await _schedule.ListTransportForUpdateAsync(cancellationToken);
        // A new amount closes the open-ended row it replaces. It is paid once per teaching day, not per hour.
        // A date with no row is a gap, not zero.
        foreach (var prior in existing)
        {
            if (prior.EffectiveTo is null && rate.EffectiveFrom > prior.EffectiveFrom && prior.Range.Overlaps(rate.Range))
                prior.EndOn(rate.EffectiveFrom);
        }

        if (DateRange.OverlapsAny(rate.Range, existing.Select(item => item.Range)))
            throw new ConflictAppException("Those transport dates overlap an existing rate.");

        _schedule.Add(rate);
        await _schedule.SaveChangesAsync(cancellationToken);
        return new TransportRateResponse
        {
            Id = rate.Id,
            Amount = rate.Amount,
            EffectiveFrom = rate.EffectiveFrom,
            EffectiveTo = rate.EffectiveTo
        };
    }
}
