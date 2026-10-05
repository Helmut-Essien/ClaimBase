using ClaimBase.Domain.Rates;
using ClaimBase.Shared.Rates;
using FluentAssertions;

namespace ClaimBase.Api.Tests.Domain;

public class RateRulesTests
{
    private const string TenantId = "01JB0000000000000000000TNT";
    private const string EntityId = "01JB0000000000000000000ENT";

    [Fact]
    public void TeachingRateStoresTheHourlyAmountForOneTitleAndQualification()
    {
        var rate = TeachingRate.Create(
            EntityId,
            TenantId,
            EntityId,
            EntityId,
            10.5m,
            new DateOnly(2026, 1, 1),
            null);

        rate.Amount.Should().Be(10.5m);
        rate.EffectiveTo.Should().BeNull();
    }

    [Fact]
    public void TransportRateIsADailyAmountWithNoRank()
    {
        var rate = TransportRate.Create(EntityId, TenantId, 25m, new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 1));

        rate.Amount.Should().Be(25m);
        rate.Range.End.Should().Be(new DateOnly(2026, 6, 1));
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("10.555")]
    public void AmountMustBeNonNegativeWithTwoDecimalPlaces(string amount)
    {
        var value = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);
        var teaching = () => TeachingRate.Create(EntityId, TenantId, EntityId, EntityId, value, new DateOnly(2026, 1, 1), null);
        var transport = () => TransportRate.Create(EntityId, TenantId, value, new DateOnly(2026, 1, 1), null);

        teaching.Should().Throw<ArgumentOutOfRangeException>();
        transport.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ZeroIsAStoredAmount()
    {
        var rate = TeachingRate.Create(EntityId, TenantId, EntityId, EntityId, 0m, new DateOnly(2026, 1, 1), null);

        rate.Amount.Should().Be(0m);
    }

    [Fact]
    public void RateEndMustBeAfterTheStart()
    {
        var act = () => TransportRate.Create(EntityId, TenantId, 1m, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 1));

        act.Should().Throw<ArgumentException>().WithMessage("EffectiveTo must be after EffectiveFrom.*");
    }

    [Fact]
    public void RateRejectsTheUnsetStartDate()
    {
        var act = () => TeachingRate.Create(EntityId, TenantId, EntityId, EntityId, 1m, default, null);

        act.Should().Throw<ArgumentException>().WithMessage("EffectiveFrom is required.*");
    }

    [Fact]
    public void ReplacementEndsAnOpenRateAndLeavesAClosedRateAlone()
    {
        var rate = TeachingRate.Create(EntityId, TenantId, EntityId, EntityId, 10m, new DateOnly(2026, 1, 1), null);
        rate.EndOn(new DateOnly(2027, 1, 1));
        rate.EffectiveTo.Should().Be(new DateOnly(2027, 1, 1));

        var again = () => rate.EndOn(new DateOnly(2027, 6, 1));
        again.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SharedAmountScaleMatchesTheDomain()
    {
        RateFieldLimits.AmountScale.Should().Be(RateConstraints.AmountScale);
    }
}
