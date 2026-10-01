using ClaimBase.Domain.Common;
using ClaimBase.Domain.Identity;

namespace ClaimBase.Domain.Academic;

/// <summary>A course offered by the tenant. The code is stored uppercase and is unique in the tenant.</summary>
public sealed class Course
{
    private Course()
    {
    }

    /// <summary>ULID primary key.</summary>
    public string Id { get; private set; } = null!;

    /// <summary>Owning tenant.</summary>
    public string TenantId { get; private set; } = null!;

    /// <summary>Course code, uppercase.</summary>
    public string Code { get; private set; } = null!;

    /// <summary>Course name.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Qualification this course leads to.</summary>
    public string QualificationId { get; private set; } = null!;

    /// <summary>
    /// Creates a course.
    /// </summary>
    /// <param name="id">ULID.</param>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="code">Course code.</param>
    /// <param name="name">Course name.</param>
    /// <param name="qualificationId">Qualification id.</param>
    /// <returns>The new course.</returns>
    public static Course Create(string id, string tenantId, string code, string name, string qualificationId)
    {
        var course = new Course
        {
            Id = Guard.RequiredId(id, nameof(id), UserConstraints.IdMaxLength),
            TenantId = Guard.RequiredId(tenantId, nameof(tenantId), UserConstraints.IdMaxLength),
            Code = "",
            Name = "",
            QualificationId = Guard.RequiredId(qualificationId, nameof(qualificationId), UserConstraints.IdMaxLength)
        };
        course.Update(code, name, qualificationId);
        return course;
    }

    /// <summary>
    /// Replaces the code, name, and qualification. The code is stored uppercase.
    /// </summary>
    /// <param name="code">Course code.</param>
    /// <param name="name">Course name.</param>
    /// <param name="qualificationId">Qualification id.</param>
    public void Update(string code, string name, string qualificationId)
    {
        Code = Guard.Required(code, nameof(code), AcademicConstraints.CourseCodeMaxLength).ToUpperInvariant();
        Name = Guard.Required(name, nameof(name), AcademicConstraints.NameMaxLength);
        QualificationId = Guard.RequiredId(qualificationId, nameof(qualificationId), UserConstraints.IdMaxLength);
    }
}
