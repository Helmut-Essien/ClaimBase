using ClaimBase.Application.Features.Rates;
using ClaimBase.Shared.Common;
using ClaimBase.Shared.Rates;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaimBase.Api.Controllers;

/// <summary>Hourly teaching rates. Tenant admin and admin only. The role is checked from the database.</summary>
[ApiController]
[Authorize]
[Route("api/rates/teaching")]
public sealed class TeachingRatesController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Creates the controller.</summary>
    /// <param name="sender">MediatR sender.</param>
    public TeachingRatesController(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);
        _sender = sender;
    }

    /// <summary>Lists teaching rates. An open-ended row is included. A date with no row is not filled in as zero.</summary>
    /// <param name="page">1-based page.</param>
    /// <param name="pageSize">Page size, 1 to 100.</param>
    /// <param name="positionTitleId">Optional title filter.</param>
    /// <param name="qualificationId">Optional qualification filter.</param>
    /// <param name="on">Optional day. Only rows in force on that day are returned.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One page of teaching rates.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TeachingRateResponse>>> List(
        [FromQuery] int page = PageLimits.DefaultPage,
        [FromQuery] int pageSize = PageLimits.DefaultSize,
        [FromQuery] string? positionTitleId = null,
        [FromQuery] string? qualificationId = null,
        [FromQuery] DateOnly? on = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _sender.Send(new ListTeachingRatesQuery(page, pageSize, positionTitleId, qualificationId, on), cancellationToken));
    }

    /// <summary>Creates a teaching rate. Overlapping dates for the same title and qualification return 409.</summary>
    /// <param name="request">Title, qualification, hourly amount, and dates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new rate.</returns>
    [HttpPost]
    public async Task<ActionResult<TeachingRateResponse>> Create(
        [FromBody] CreateTeachingRateRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _sender.Send(
            new CreateTeachingRateCommand(
                request.PositionTitleId,
                request.QualificationId,
                request.Amount,
                request.EffectiveFrom,
                request.EffectiveTo),
            cancellationToken);
        return Created("/api/rates/teaching", created);
    }
}

/// <summary>Tenant transport timeline. Tenant admin and admin only. Paid once per teaching day, not per hour.</summary>
[ApiController]
[Authorize]
[Route("api/rates/transport")]
public sealed class TransportRatesController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Creates the controller.</summary>
    /// <param name="sender">MediatR sender.</param>
    public TransportRatesController(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);
        _sender = sender;
    }

    /// <summary>Lists transport rates. An open-ended row is included. A date with no row is not filled in as zero.</summary>
    /// <param name="page">1-based page.</param>
    /// <param name="pageSize">Page size, 1 to 100.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One page of transport rates.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TransportRateResponse>>> List(
        [FromQuery] int page = PageLimits.DefaultPage,
        [FromQuery] int pageSize = PageLimits.DefaultSize,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _sender.Send(new ListTransportRatesQuery(page, pageSize), cancellationToken));
    }

    /// <summary>Creates a transport rate. Overlapping dates on the tenant timeline return 409.</summary>
    /// <param name="request">Daily amount and dates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new rate.</returns>
    [HttpPost]
    public async Task<ActionResult<TransportRateResponse>> Create(
        [FromBody] CreateTransportRateRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _sender.Send(
            new CreateTransportRateCommand(request.Amount, request.EffectiveFrom, request.EffectiveTo),
            cancellationToken);
        return Created("/api/rates/transport", created);
    }
}
