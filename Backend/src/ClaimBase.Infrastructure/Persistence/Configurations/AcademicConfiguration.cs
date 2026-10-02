using ClaimBase.Domain.Academic;
using ClaimBase.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimBase.Infrastructure.Persistence.Configurations;

/// <summary>EF mapping for <see cref="Campus"/>.</summary>
public sealed class CampusConfiguration : IEntityTypeConfiguration<Campus>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Campus> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Campuses", table => table.HasCheckConstraint("CK_Campuses_Name", "char_length(\"Name\") > 0"));
        builder.HasKey(campus => campus.Id);
        builder.Property(campus => campus.Id).HasMaxLength(UserConstraints.IdMaxLength);
        builder.Property(campus => campus.TenantId).HasMaxLength(UserConstraints.IdMaxLength).IsRequired();
        builder.Property(campus => campus.Name).HasMaxLength(AcademicConstraints.NameMaxLength).IsRequired();
        builder.HasIndex(campus => new { campus.TenantId, campus.Name }).IsUnique();
    }
}

/// <summary>EF mapping for <see cref="Faculty"/>.</summary>
public sealed class FacultyConfiguration : IEntityTypeConfiguration<Faculty>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Faculty> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Faculties", table => table.HasCheckConstraint("CK_Faculties_Name", "char_length(\"Name\") > 0"));
        builder.HasKey(faculty => faculty.Id);
        builder.Property(faculty => faculty.Id).HasMaxLength(UserConstraints.IdMaxLength);
        builder.Property(faculty => faculty.TenantId).HasMaxLength(UserConstraints.IdMaxLength).IsRequired();
        builder.Property(faculty => faculty.CampusId).HasMaxLength(UserConstraints.IdMaxLength).IsRequired();
        builder.Property(faculty => faculty.Name).HasMaxLength(AcademicConstraints.NameMaxLength).IsRequired();
        builder.HasOne<Campus>().WithMany().HasForeignKey(faculty => faculty.CampusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(faculty => new { faculty.TenantId, faculty.CampusId, faculty.Name }).IsUnique();
    }
}

/// <summary>EF mapping for <see cref="Department"/>.</summary>
public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Departments", table => table.HasCheckConstraint("CK_Departments_Name", "char_length(\"Name\") > 0"));
        builder.HasKey(department => department.Id);
        builder.Property(department => department.Id).HasMaxLength(UserConstraints.IdMaxLength);
        builder.Property(department => department.TenantId).HasMaxLength(UserConstraints.IdMaxLength).IsRequired();
        builder.Property(department => department.FacultyId).HasMaxLength(UserConstraints.IdMaxLength).IsRequired();
        builder.Property(department => department.Name).HasMaxLength(AcademicConstraints.NameMaxLength).IsRequired();
        builder.HasOne<Faculty>().WithMany().HasForeignKey(department => department.FacultyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(department => new { department.TenantId, department.FacultyId, department.Name }).IsUnique();
    }
}

/// <summary>EF mapping for <see cref="Semester"/>.</summary>
public sealed class SemesterConfiguration : IEntityTypeConfiguration<Semester>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Semester> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Semesters", table =>
        {
            table.HasCheckConstraint("CK_Semesters_Name", "char_length(\"Name\") > 0");
            table.HasCheckConstraint("CK_Semesters_Dates", "\"EndDate\" >= \"StartDate\"");
            table.HasCheckConstraint("CK_Semesters_Status", "\"Status\" IN ('Draft', 'Open', 'Closed')");
        });
        builder.HasKey(semester => semester.Id);
        builder.Property(semester => semester.Id).HasMaxLength(UserConstraints.IdMaxLength);
        builder.Property(semester => semester.TenantId).HasMaxLength(UserConstraints.IdMaxLength).IsRequired();
        builder.Property(semester => semester.Name).HasMaxLength(AcademicConstraints.NameMaxLength).IsRequired();
        builder.Property(semester => semester.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.HasIndex(semester => new { semester.TenantId, semester.Status });
    }
}

/// <summary>EF mapping for <see cref="Qualification"/>.</summary>
public sealed class QualificationConfiguration : IEntityTypeConfiguration<Qualification>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Qualification> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Qualifications", table => table.HasCheckConstraint("CK_Qualifications_Name", "char_length(\"Name\") > 0"));
        builder.HasKey(qualification => qualification.Id);
        builder.Property(qualification => qualification.Id).HasMaxLength(UserConstraints.IdMaxLength);
        builder.Property(qualification => qualification.TenantId).HasMaxLength(UserConstraints.IdMaxLength).IsRequired();
        builder.Property(qualification => qualification.Name).HasMaxLength(AcademicConstraints.QualificationNameMaxLength).IsRequired();
        builder.HasIndex(qualification => new { qualification.TenantId, qualification.Name }).IsUnique();
    }
}

/// <summary>EF mapping for <see cref="PositionTitle"/>.</summary>
public sealed class PositionTitleConfiguration : IEntityTypeConfiguration<PositionTitle>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PositionTitle> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PositionTitles", table => table.HasCheckConstraint("CK_PositionTitles_Name", "char_length(\"Name\") > 0"));
        builder.HasKey(title => title.Id);
        builder.Property(title => title.Id).HasMaxLength(UserConstraints.IdMaxLength);
        builder.Property(title => title.TenantId).HasMaxLength(UserConstraints.IdMaxLength).IsRequired();
        builder.Property(title => title.Name).HasMaxLength(AcademicConstraints.PositionTitleNameMaxLength).IsRequired();
        builder.HasIndex(title => new { title.TenantId, title.Name }).IsUnique();
    }
}

/// <summary>EF mapping for <see cref="Course"/>.</summary>
public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Courses", table =>
        {
            table.HasCheckConstraint("CK_Courses_Code", "char_length(\"Code\") > 0");
            table.HasCheckConstraint("CK_Courses_Name", "char_length(\"Name\") > 0");
        });
        builder.HasKey(course => course.Id);
        builder.Property(course => course.Id).HasMaxLength(UserConstraints.IdMaxLength);
        builder.Property(course => course.TenantId).HasMaxLength(UserConstraints.IdMaxLength).IsRequired();
        builder.Property(course => course.Code).HasMaxLength(AcademicConstraints.CourseCodeMaxLength).IsRequired();
        builder.Property(course => course.Name).HasMaxLength(AcademicConstraints.NameMaxLength).IsRequired();
        builder.Property(course => course.QualificationId).HasMaxLength(UserConstraints.IdMaxLength).IsRequired();
        builder.HasOne<Qualification>().WithMany().HasForeignKey(course => course.QualificationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(course => new { course.TenantId, course.Code }).IsUnique();
    }
}

/// <summary>EF mapping for <see cref="Staff"/>.</summary>
public sealed class StaffConfiguration : IEntityTypeConfiguration<Staff>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Staff> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Staff", table =>
        {
            table.HasCheckConstraint("CK_Staff_StaffNumber", "char_length(\"StaffNumber\") > 0");
            table.HasCheckConstraint("CK_Staff_DisplayName", "char_length(\"DisplayName\") > 0");
            table.HasCheckConstraint("CK_Staff_EmploymentType", "\"EmploymentType\" IN ('PartTime', 'FullTime')");
        });
        builder.HasKey(staff => staff.Id);
        builder.Property(staff => staff.Id).HasMaxLength(UserConstraints.IdMaxLength);
        builder.Property(staff => staff.TenantId).HasMaxLength(UserConstraints.IdMaxLength).IsRequired();
        builder.Property(staff => staff.StaffNumber).HasMaxLength(AcademicConstraints.StaffNumberMaxLength).IsRequired();
        builder.Property(staff => staff.DisplayName).HasMaxLength(AcademicConstraints.NameMaxLength).IsRequired();
        builder.Property(staff => staff.Email).HasMaxLength(UserConstraints.EmailMaxLength);
        builder.Property(staff => staff.EmploymentType).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(staff => staff.BiometricId).HasMaxLength(AcademicConstraints.BiometricIdMaxLength);
        builder.HasIndex(staff => new { staff.TenantId, staff.StaffNumber }).IsUnique();
        // PostgreSQL treats nulls as distinct, so several lecturers may have no biometric id.
        builder.HasIndex(staff => new { staff.TenantId, staff.BiometricId }).IsUnique();
    }
}

/// <summary>EF mapping for <see cref="StaffDepartment"/>.</summary>
public sealed class StaffDepartmentConfiguration : IEntityTypeConfiguration<StaffDepartment>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StaffDepartment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("StaffDepartments");
        builder.HasKey(assignment => assignment.Id);
        builder.Property(assignment => assignment.Id).HasMaxLength(UserConstraints.IdMaxLength);
        builder.Property(assignment => assignment.TenantId).HasMaxLength(UserConstraints.IdMaxLength).IsRequired();
        builder.Property(assignment => assignment.StaffId).HasMaxLength(UserConstraints.IdMaxLength).IsRequired();
        builder.Property(assignment => assignment.DepartmentId).HasMaxLength(UserConstraints.IdMaxLength).IsRequired();
        builder.HasOne<Staff>().WithMany().HasForeignKey(assignment => assignment.StaffId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>().WithMany().HasForeignKey(assignment => assignment.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(assignment => new { assignment.TenantId, assignment.StaffId, assignment.DepartmentId }).IsUnique();
        builder.HasIndex(assignment => new { assignment.TenantId, assignment.DepartmentId });
    }
}

/// <summary>EF mapping for <see cref="StaffPosition"/>.</summary>
public sealed class StaffPositionConfiguration : IEntityTypeConfiguration<StaffPosition>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StaffPosition> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("StaffPositions", table =>
            table.HasCheckConstraint("CK_StaffPositions_Range", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" > \"EffectiveFrom\""));
        builder.HasKey(position => position.Id);
        builder.Property(position => position.Id).HasMaxLength(UserConstraints.IdMaxLength);
        builder.Property(position => position.TenantId).HasMaxLength(UserConstraints.IdMaxLength).IsRequired();
        builder.Property(position => position.StaffId).HasMaxLength(UserConstraints.IdMaxLength).IsRequired();
        builder.Property(position => position.PositionTitleId).HasMaxLength(UserConstraints.IdMaxLength).IsRequired();
        builder.HasOne<Staff>().WithMany().HasForeignKey(position => position.StaffId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PositionTitle>().WithMany().HasForeignKey(position => position.PositionTitleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(position => new { position.TenantId, position.StaffId, position.EffectiveFrom });
        builder.Ignore(position => position.Range);
    }
}
