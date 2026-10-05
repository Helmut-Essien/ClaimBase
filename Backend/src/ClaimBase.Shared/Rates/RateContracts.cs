using System.ComponentModel.DataAnnotations;

namespace ClaimBase.Shared.Rates;

/// <summary>
/// Rate field bounds. These match <c>RateConstraints</c>. Portal <c>RATE_FIELD_LIMITS</c> are added with the rates screen.
/// </summary>
public static class RateFieldLimits
{
    /// <summary>Digits after the decimal point for a cedis amount.</summary>
    public const int AmountScale = 2;
}

/// <summary>One hourly teaching rate, including the title and qualification names.</summary>
public sealed class TeachingRateResponse
{
    /// <summary>Rate id.</summary>
    public required string Id { get; init; }

    /// <summary>Position title id.</summary>
    public required string PositionTitleId { get; init; }

    /// <summary>Position title name.</summary>
    public required string PositionTitleName { get; init; }

    /// <summary>Qualification id.</summary>
    public required string QualificationId { get; init; }

    /// <summary>Qualification name.</summary>
    public required string QualificationName { get; init; }

    /// <summary>Cedis per hour.</summary>
    public required decimal Amount { get; init; }

    /// <summary>First day included.</summary>
    public required DateOnly EffectiveFrom { get; init; }

    /// <summary>First day excluded. Null means the amount continues until a later row replaces it.</summary>
    public DateOnly? EffectiveTo { get; init; }
}

/// <summary>Body for <c>POST /api/rates/teaching</c>.</summary>
public sealed class CreateTeachingRateRequest
{
    /// <summary>Shared position title.</summary>
    [Required]
    [MaxLength(26)]
    public string PositionTitleId { get; init; } = "";

    /// <summary>Course qualification.</summary>
    [Required]
    [MaxLength(26)]
    public string QualificationId { get; init; } = "";

    /// <summary>Cedis per hour. Zero is a real amount. A missing row is a gap and is not stored as zero.</summary>
    [Required]
    public decimal Amount { get; init; }

    /// <summary>First day included.</summary>
    [Required]
    public DateOnly EffectiveFrom { get; init; }

    /// <summary>First day excluded. Omit to keep the amount in force through later semesters.</summary>
    public DateOnly? EffectiveTo { get; init; }
}

/// <summary>One transport amount for the university.</summary>
public sealed class TransportRateResponse
{
    /// <summary>Rate id.</summary>
    public required string Id { get; init; }

    /// <summary>Cedis paid once per teaching day.</summary>
    public required decimal Amount { get; init; }

    /// <summary>First day included.</summary>
    public required DateOnly EffectiveFrom { get; init; }

    /// <summary>First day excluded. Null means the amount continues until a later row replaces it.</summary>
    public DateOnly? EffectiveTo { get; init; }
}

/// <summary>Body for <c>POST /api/rates/transport</c>.</summary>
public sealed class CreateTransportRateRequest
{
    /// <summary>Cedis per teaching day. Zero is a real amount. A missing row is a gap and is not stored as zero.</summary>
    [Required]
    public decimal Amount { get; init; }

    /// <summary>First day included.</summary>
    [Required]
    public DateOnly EffectiveFrom { get; init; }

    /// <summary>First day excluded. Omit to keep the amount in force through later semesters.</summary>
    public DateOnly? EffectiveTo { get; init; }
}
