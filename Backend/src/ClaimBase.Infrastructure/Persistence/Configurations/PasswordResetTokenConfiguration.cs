using ClaimBase.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimBase.Infrastructure.Persistence.Configurations;

/// <summary>EF mapping for <see cref="PasswordResetToken"/>.</summary>
public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PasswordResetTokens", table =>
        {
            table.HasCheckConstraint(
                "CK_PasswordResetTokens_TokenHash",
                "char_length(\"TokenHash\") = 64");
            table.HasCheckConstraint(
                "CK_PasswordResetTokens_Expiry",
                "\"ExpiresAt\" > \"CreatedAt\"");
        });

        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).HasMaxLength(UserConstraints.IdMaxLength);
        builder.Property(token => token.TenantId).HasMaxLength(UserConstraints.IdMaxLength).IsRequired();
        builder.Property(token => token.UserId).HasMaxLength(UserConstraints.IdMaxLength).IsRequired();
        builder.Property(token => token.TokenHash).HasMaxLength(UserConstraints.PasswordResetTokenHashLength).IsRequired();
        builder.Property(token => token.ExpiresAt).IsRequired();
        builder.Property(token => token.CreatedAt).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(token => token.UserId).OnDelete(DeleteBehavior.Cascade);

        // Lookup is by hash, before a tenant is known. Replacing older links filters by user.
        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => new { token.TenantId, token.UserId });

        // One live link per user. The advisory lock is the usual path; this rejects a second insert.
        builder.HasIndex(token => token.UserId)
            .IsUnique()
            .HasFilter("\"UsedAt\" IS NULL")
            .HasDatabaseName("IX_PasswordResetTokens_OneOutstanding");
    }
}
