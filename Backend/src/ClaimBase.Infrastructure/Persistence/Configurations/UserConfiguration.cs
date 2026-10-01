using ClaimBase.Domain.Academic;
using ClaimBase.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimBase.Infrastructure.Persistence.Configurations;

/// <summary>EF mapping for <see cref="User"/>.</summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Users", table =>
        {
            table.HasCheckConstraint(
                "CK_Users_Role",
                "\"Role\" IN ('TenantAdmin', 'Admin', 'HeadOfDepartment', 'Finance', 'Lecturer')");
            table.HasCheckConstraint("CK_Users_Email", "char_length(\"Email\") > 0");
            table.HasCheckConstraint("CK_Users_DisplayName", "char_length(\"DisplayName\") > 0");
            table.HasCheckConstraint(
                "CK_Users_HodDepartment",
                "\"Role\" <> 'HeadOfDepartment' OR \"DepartmentId\" IS NOT NULL");
            table.HasCheckConstraint(
                "CK_Users_LecturerStaff",
                "\"Role\" <> 'Lecturer' OR \"StaffId\" IS NOT NULL");
        });

        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).HasMaxLength(UserConstraints.IdMaxLength);
        builder.Property(user => user.TenantId).HasMaxLength(UserConstraints.IdMaxLength).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(UserConstraints.EmailMaxLength).IsRequired();
        builder.Property(user => user.DisplayName).HasMaxLength(UserConstraints.DisplayNameMaxLength).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(UserConstraints.PasswordHashMaxLength).IsRequired();
        builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(UserConstraints.RoleMaxLength).IsRequired();
        builder.Property(user => user.DepartmentId).HasMaxLength(UserConstraints.IdMaxLength);
        builder.Property(user => user.StaffId).HasMaxLength(UserConstraints.IdMaxLength);
        builder.Property(user => user.CreatedAt).IsRequired();
        builder.HasOne<Department>().WithMany().HasForeignKey(user => user.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Staff>().WithMany().HasForeignKey(user => user.StaffId).OnDelete(DeleteBehavior.Restrict);

        // Leading TenantId supports the global filter. Email stays unique inside one university only.
        builder.HasIndex(user => new { user.TenantId, user.Email }).IsUnique();
    }
}
