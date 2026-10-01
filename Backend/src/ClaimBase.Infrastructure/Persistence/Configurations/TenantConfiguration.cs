using ClaimBase.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimBase.Infrastructure.Persistence.Configurations;

/// <summary>EF mapping for <see cref="Tenant"/>.</summary>
public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Tenants", table =>
        {
            table.HasCheckConstraint("CK_Tenants_Name", "char_length(\"Name\") > 0");
            table.HasCheckConstraint("CK_Tenants_CurrencyCode", "char_length(\"CurrencyCode\") = 3");
            table.HasCheckConstraint("CK_Tenants_TimeZoneId", "char_length(\"TimeZoneId\") > 0");
        });

        builder.HasKey(tenant => tenant.Id);
        builder.Property(tenant => tenant.Id).HasMaxLength(UserConstraints.IdMaxLength);
        builder.Property(tenant => tenant.Name).HasMaxLength(TenantConstraints.NameMaxLength).IsRequired();
        builder.Property(tenant => tenant.CurrencyCode).HasMaxLength(TenantConstraints.CurrencyCodeLength).IsRequired();
        builder.Property(tenant => tenant.TimeZoneId).HasMaxLength(TenantConstraints.TimeZoneIdMaxLength).IsRequired();
        builder.Property(tenant => tenant.CreatedAt).IsRequired();
    }
}
