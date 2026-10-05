using ClaimBase.Domain.Academic;
using ClaimBase.Domain.Identity;
using ClaimBase.Domain.Rates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimBase.Infrastructure.Persistence.Configurations;

/// <summary>EF mapping for <see cref="TeachingRate"/>.</summary>
public sealed class TeachingRateConfiguration : IEntityTypeConfiguration<TeachingRate>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TeachingRate> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("TeachingRates", table =>
        {
            table.HasCheckConstraint("CK_TeachingRates_Range", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" > \"EffectiveFrom\"");
            table.HasCheckConstraint("CK_TeachingRates_Amount", "\"Amount\" >= 0");
        });
        builder.HasKey(rate => rate.Id);
        builder.Property(rate => rate.Id).HasMaxLength(UserConstraints.IdMaxLength);
        builder.Property(rate => rate.TenantId).HasMaxLength(UserConstraints.IdMaxLength).IsRequired();
        builder.Property(rate => rate.PositionTitleId).HasMaxLength(UserConstraints.IdMaxLength).IsRequired();
        builder.Property(rate => rate.QualificationId).HasMaxLength(UserConstraints.IdMaxLength).IsRequired();
        builder.Property(rate => rate.Amount).HasPrecision(RateConstraints.AmountPrecision, RateConstraints.AmountScale);
        builder.HasOne<PositionTitle>().WithMany().HasForeignKey(rate => rate.PositionTitleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Qualification>().WithMany().HasForeignKey(rate => rate.QualificationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(rate => new { rate.TenantId, rate.PositionTitleId, rate.QualificationId, rate.EffectiveFrom });
        builder.Ignore(rate => rate.Range);
    }
}

/// <summary>EF mapping for <see cref="TransportRate"/>.</summary>
public sealed class TransportRateConfiguration : IEntityTypeConfiguration<TransportRate>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TransportRate> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("TransportRates", table =>
        {
            table.HasCheckConstraint("CK_TransportRates_Range", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" > \"EffectiveFrom\"");
            table.HasCheckConstraint("CK_TransportRates_Amount", "\"Amount\" >= 0");
        });
        builder.HasKey(rate => rate.Id);
        builder.Property(rate => rate.Id).HasMaxLength(UserConstraints.IdMaxLength);
        builder.Property(rate => rate.TenantId).HasMaxLength(UserConstraints.IdMaxLength).IsRequired();
        builder.Property(rate => rate.Amount).HasPrecision(RateConstraints.AmountPrecision, RateConstraints.AmountScale);
        builder.HasIndex(rate => new { rate.TenantId, rate.EffectiveFrom });
        builder.Ignore(rate => rate.Range);
    }
}
