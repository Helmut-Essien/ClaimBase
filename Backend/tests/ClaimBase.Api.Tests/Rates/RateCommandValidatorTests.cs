using ClaimBase.Application.Features.Rates;
using FluentAssertions;

namespace ClaimBase.Api.Tests.Rates;

public class RateCommandValidatorTests
{
    private readonly CreateTeachingRateCommandValidator _teaching = new();
    private readonly CreateTransportRateCommandValidator _transport = new();

    [Fact]
    public void AcceptsAnOpenEndedTeachingRate()
    {
        var result = _teaching.Validate(new CreateTeachingRateCommand(
            "01JB0000000000000000000TTL",
            "01JB0000000000000000000QLF",
            10m,
            new DateOnly(2026, 1, 1),
            null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RejectsATeachingAmountWithThreeDecimalPlaces()
    {
        var result = _teaching.Validate(new CreateTeachingRateCommand(
            "01JB0000000000000000000TTL",
            "01JB0000000000000000000QLF",
            10.555m,
            new DateOnly(2026, 1, 1),
            null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RejectsATransportRateThatEndsOnItsStart()
    {
        var result = _transport.Validate(new CreateTransportRateCommand(20m, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1)));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RejectsAMissingTransportStart()
    {
        var result = _transport.Validate(new CreateTransportRateCommand(20m, default, null));

        result.IsValid.Should().BeFalse();
    }
}
