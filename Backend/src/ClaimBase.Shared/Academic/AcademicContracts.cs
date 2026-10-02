using System.ComponentModel.DataAnnotations;

namespace ClaimBase.Shared.Academic;

/// <summary>Campus row.</summary>
public sealed class CampusResponse
{
    /// <summary>Campus id.</summary>
    public required string Id { get; init; }

    /// <summary>Campus name.</summary>
    public required string Name { get; init; }
}

/// <summary>Body for <c>POST /api/campuses</c>.</summary>
public sealed class CreateCampusRequest
{
    /// <summary>Campus name, unique in the tenant.</summary>
    [Required]
    [MaxLength(AcademicFieldLimits.Name)]
    public string Name { get; init; } = "";
}

/// <summary>Faculty row, including its campus.</summary>
public sealed class FacultyResponse
{
    /// <summary>Faculty id.</summary>
    public required string Id { get; init; }

    /// <summary>Campus id.</summary>
    public required string CampusId { get; init; }

    /// <summary>Campus name.</summary>
    public required string CampusName { get; init; }

    /// <summary>Faculty name.</summary>
    public required string Name { get; init; }
}

/// <summary>Body for <c>POST /api/faculties</c>.</summary>
public sealed class CreateFacultyRequest
{
    /// <summary>Parent campus.</summary>
    [Required]
    [MaxLength(26)]
    public string CampusId { get; init; } = "";

    /// <summary>Faculty name, unique on the campus.</summary>
    [Required]
    [MaxLength(AcademicFieldLimits.Name)]
    public string Name { get; init; } = "";
}

/// <summary>Department row, including its campus and faculty.</summary>
public sealed class DepartmentResponse
{
    /// <summary>Department id.</summary>
    public required string Id { get; init; }

    /// <summary>Campus id.</summary>
    public required string CampusId { get; init; }

    /// <summary>Campus name.</summary>
    public required string CampusName { get; init; }

    /// <summary>Faculty id.</summary>
    public required string FacultyId { get; init; }

    /// <summary>Faculty name.</summary>
    public required string FacultyName { get; init; }

    /// <summary>Department name.</summary>
    public required string Name { get; init; }
}

/// <summary>Body for <c>POST /api/departments</c>.</summary>
public sealed class CreateDepartmentRequest
{
    /// <summary>Parent faculty.</summary>
    [Required]
    [MaxLength(26)]
    public string FacultyId { get; init; } = "";

    /// <summary>Department name, unique inside the faculty.</summary>
    [Required]
    [MaxLength(AcademicFieldLimits.Name)]
    public string Name { get; init; } = "";
}

/// <summary>Semester row.</summary>
public sealed class SemesterResponse
{
    /// <summary>Semester id.</summary>
    public required string Id { get; init; }

    /// <summary>Semester name.</summary>
    public required string Name { get; init; }

    /// <summary>First day included.</summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>Last day included.</summary>
    public required DateOnly EndDate { get; init; }

    /// <summary><c>Draft</c>, <c>Open</c>, or <c>Closed</c>.</summary>
    public required string Status { get; init; }
}

/// <summary>Body for <c>POST /api/semesters</c>.</summary>
public sealed class CreateSemesterRequest
{
    /// <summary>Semester name.</summary>
    [Required]
    [MaxLength(AcademicFieldLimits.Name)]
    public string Name { get; init; } = "";

    /// <summary>First day included.</summary>
    [Required]
    public DateOnly StartDate { get; init; }

    /// <summary>Last day included.</summary>
    [Required]
    public DateOnly EndDate { get; init; }
}

/// <summary>Body for <c>PUT /api/semesters</c>. Status changes only through open and close.</summary>
public sealed class UpdateSemesterRequest
{
    /// <summary>Semester id.</summary>
    [Required]
    [MaxLength(26)]
    public string Id { get; init; } = "";

    /// <summary>Semester name.</summary>
    [Required]
    [MaxLength(AcademicFieldLimits.Name)]
    public string Name { get; init; } = "";

    /// <summary>First day included.</summary>
    [Required]
    public DateOnly StartDate { get; init; }

    /// <summary>Last day included.</summary>
    [Required]
    public DateOnly EndDate { get; init; }
}

/// <summary>Qualification row.</summary>
public sealed class QualificationResponse
{
    /// <summary>Qualification id.</summary>
    public required string Id { get; init; }

    /// <summary>Qualification name.</summary>
    public required string Name { get; init; }
}

/// <summary>Body for <c>POST /api/qualifications</c>.</summary>
public sealed class CreateQualificationRequest
{
    /// <summary>Qualification name, unique in the tenant.</summary>
    [Required]
    [MaxLength(AcademicFieldLimits.ShortName)]
    public string Name { get; init; } = "";
}

/// <summary>Position title row. This is the shared catalog, not a lecturer appointment.</summary>
public sealed class PositionTitleResponse
{
    /// <summary>Title id.</summary>
    public required string Id { get; init; }

    /// <summary>Title name.</summary>
    public required string Name { get; init; }
}

/// <summary>Body for <c>POST /api/position-titles</c>.</summary>
public sealed class CreatePositionTitleRequest
{
    /// <summary>Title name, unique in the tenant.</summary>
    [Required]
    [MaxLength(AcademicFieldLimits.ShortName)]
    public string Name { get; init; } = "";
}

/// <summary>Course row.</summary>
public sealed class CourseResponse
{
    /// <summary>Course id.</summary>
    public required string Id { get; init; }

    /// <summary>Uppercase course code.</summary>
    public required string Code { get; init; }

    /// <summary>Course name.</summary>
    public required string Name { get; init; }

    /// <summary>Qualification id.</summary>
    public required string QualificationId { get; init; }

    /// <summary>Qualification name.</summary>
    public required string QualificationName { get; init; }
}

/// <summary>Body for <c>POST /api/courses</c>.</summary>
public sealed class CreateCourseRequest
{
    /// <summary>Course code. Stored uppercase.</summary>
    [Required]
    [MaxLength(AcademicFieldLimits.CourseCode)]
    public string Code { get; init; } = "";

    /// <summary>Course name.</summary>
    [Required]
    [MaxLength(AcademicFieldLimits.Name)]
    public string Name { get; init; } = "";

    /// <summary>Qualification id.</summary>
    [Required]
    [MaxLength(26)]
    public string QualificationId { get; init; } = "";
}

/// <summary>Body for <c>PUT /api/courses</c>.</summary>
public sealed class UpdateCourseRequest
{
    /// <summary>Course id.</summary>
    [Required]
    [MaxLength(26)]
    public string Id { get; init; } = "";

    /// <summary>Course code. Stored uppercase.</summary>
    [Required]
    [MaxLength(AcademicFieldLimits.CourseCode)]
    public string Code { get; init; } = "";

    /// <summary>Course name.</summary>
    [Required]
    [MaxLength(AcademicFieldLimits.Name)]
    public string Name { get; init; } = "";

    /// <summary>Qualification id.</summary>
    [Required]
    [MaxLength(26)]
    public string QualificationId { get; init; } = "";
}

/// <summary>A department assignment shown with its campus and faculty.</summary>
public sealed class StaffDepartmentResponse
{
    /// <summary>Department id.</summary>
    public required string DepartmentId { get; init; }

    /// <summary>Department name.</summary>
    public required string DepartmentName { get; init; }

    /// <summary>Campus id.</summary>
    public required string CampusId { get; init; }

    /// <summary>Campus name.</summary>
    public required string CampusName { get; init; }

    /// <summary>Faculty id.</summary>
    public required string FacultyId { get; init; }

    /// <summary>Faculty name.</summary>
    public required string FacultyName { get; init; }
}

/// <summary>One position appointment on a lecturer.</summary>
public sealed class StaffPositionResponse
{
    /// <summary>Appointment id.</summary>
    public required string Id { get; init; }

    /// <summary>Shared title id.</summary>
    public required string PositionTitleId { get; init; }

    /// <summary>Shared title name.</summary>
    public required string PositionTitleName { get; init; }

    /// <summary>First day included.</summary>
    public required DateOnly EffectiveFrom { get; init; }

    /// <summary>First day excluded. Null while the appointment is open.</summary>
    public DateOnly? EffectiveTo { get; init; }
}

/// <summary>Staff row, including departments and appointments.</summary>
public sealed class StaffResponse
{
    /// <summary>Staff id.</summary>
    public required string Id { get; init; }

    /// <summary>Staff number.</summary>
    public required string StaffNumber { get; init; }

    /// <summary>Display name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Optional email.</summary>
    public string? Email { get; init; }

    /// <summary><c>PartTime</c> or <c>FullTime</c>.</summary>
    public required string EmploymentType { get; init; }

    /// <summary>Optional biometric device id.</summary>
    public string? BiometricId { get; init; }

    /// <summary>Department assignments. At least one.</summary>
    public required IReadOnlyList<StaffDepartmentResponse> Departments { get; init; }

    /// <summary>Position appointments.</summary>
    public required IReadOnlyList<StaffPositionResponse> Positions { get; init; }
}

/// <summary>Body for <c>POST /api/staff</c>.</summary>
public sealed class CreateStaffRequest
{
    /// <summary>Staff number, unique in the tenant.</summary>
    [Required]
    [MaxLength(AcademicFieldLimits.StaffNumber)]
    public string StaffNumber { get; init; } = "";

    /// <summary>Display name.</summary>
    [Required]
    [MaxLength(AcademicFieldLimits.Name)]
    public string DisplayName { get; init; } = "";

    /// <summary>Optional email.</summary>
    [MaxLength(AcademicFieldLimits.Email)]
    public string? Email { get; init; }

    /// <summary><c>PartTime</c> or <c>FullTime</c>.</summary>
    [Required]
    public string EmploymentType { get; init; } = "";

    /// <summary>Optional biometric device id.</summary>
    [MaxLength(AcademicFieldLimits.BiometricId)]
    public string? BiometricId { get; init; }

    /// <summary>At least one department.</summary>
    [Required]
    [MinLength(1)]
    public IReadOnlyList<string> DepartmentIds { get; init; } = [];
}

/// <summary>Body for <c>PUT /api/staff</c>. Assignments are changed on their own routes.</summary>
public sealed class UpdateStaffRequest
{
    /// <summary>Staff id.</summary>
    [Required]
    [MaxLength(26)]
    public string Id { get; init; } = "";

    /// <summary>Staff number.</summary>
    [Required]
    [MaxLength(AcademicFieldLimits.StaffNumber)]
    public string StaffNumber { get; init; } = "";

    /// <summary>Display name.</summary>
    [Required]
    [MaxLength(AcademicFieldLimits.Name)]
    public string DisplayName { get; init; } = "";

    /// <summary>Optional email.</summary>
    [MaxLength(AcademicFieldLimits.Email)]
    public string? Email { get; init; }

    /// <summary><c>PartTime</c> or <c>FullTime</c>.</summary>
    [Required]
    public string EmploymentType { get; init; } = "";

    /// <summary>Optional biometric device id. Blank clears it.</summary>
    [MaxLength(AcademicFieldLimits.BiometricId)]
    public string? BiometricId { get; init; }
}

/// <summary>Body for <c>POST /api/staff/{id}/departments</c>.</summary>
public sealed class AssignDepartmentRequest
{
    /// <summary>Department to assign.</summary>
    [Required]
    [MaxLength(26)]
    public string DepartmentId { get; init; } = "";
}

/// <summary>Body for <c>POST /api/staff/{id}/positions</c>.</summary>
public sealed class CreateStaffPositionRequest
{
    /// <summary>Shared position title.</summary>
    [Required]
    [MaxLength(26)]
    public string PositionTitleId { get; init; } = "";

    /// <summary>First day included.</summary>
    [Required]
    public DateOnly EffectiveFrom { get; init; }

    /// <summary>First day excluded. Omit while the appointment is open.</summary>
    public DateOnly? EffectiveTo { get; init; }
}
