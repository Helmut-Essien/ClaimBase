using ClaimBase.Domain.Academic;
using ClaimBase.Shared.Academic;
using ClaimBase.Shared.Common;

namespace ClaimBase.Application.Common.Interfaces;

/// <summary>
/// Academic persistence. Reads are filtered to the current tenant. Writes share one <c>SaveChanges</c> per use case.
/// </summary>
public interface IAcademicCatalog
{
    /// <summary>Lists faculties ordered by name.</summary>
    Task<PagedResult<FacultyResponse>> ListFacultiesAsync(int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>True when another faculty in this tenant already uses the name, ignoring case.</summary>
    Task<bool> FacultyNameTakenAsync(string name, CancellationToken cancellationToken);

    /// <summary>Stages a faculty.</summary>
    void Add(Faculty faculty);

    /// <summary>Lists departments, optionally for one faculty.</summary>
    Task<PagedResult<DepartmentResponse>> ListDepartmentsAsync(int page, int pageSize, string? facultyId, CancellationToken cancellationToken);

    /// <summary>True when the faculty is in this tenant.</summary>
    Task<bool> FacultyExistsAsync(string facultyId, CancellationToken cancellationToken);

    /// <summary>True when the department name is already used in that faculty, ignoring case.</summary>
    Task<bool> DepartmentNameTakenAsync(string facultyId, string name, CancellationToken cancellationToken);

    /// <summary>True when the department is in this tenant.</summary>
    Task<bool> DepartmentExistsAsync(string departmentId, CancellationToken cancellationToken);

    /// <summary>Loads one department with its faculty name.</summary>
    Task<DepartmentResponse?> GetDepartmentAsync(string departmentId, CancellationToken cancellationToken);

    /// <summary>Stages a department.</summary>
    void Add(Department department);

    /// <summary>Lists semesters, newest start date first.</summary>
    Task<PagedResult<SemesterResponse>> ListSemestersAsync(int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Loads a semester for update. Null when it is missing or in another tenant.</summary>
    Task<Semester?> FindSemesterAsync(string id, CancellationToken cancellationToken);

    /// <summary>True when another open semester covers any day of this inclusive range.</summary>
    Task<bool> OpenSemesterOverlapsAsync(DateOnly start, DateOnly end, string exceptId, CancellationToken cancellationToken);

    /// <summary>Stages a semester.</summary>
    void Add(Semester semester);

    /// <summary>Lists qualifications ordered by name.</summary>
    Task<PagedResult<QualificationResponse>> ListQualificationsAsync(int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>True when the qualification name is taken, ignoring case.</summary>
    Task<bool> QualificationNameTakenAsync(string name, CancellationToken cancellationToken);

    /// <summary>True when the qualification is in this tenant.</summary>
    Task<bool> QualificationExistsAsync(string id, CancellationToken cancellationToken);

    /// <summary>Stages a qualification.</summary>
    void Add(Qualification qualification);

    /// <summary>Lists position titles ordered by name.</summary>
    Task<PagedResult<PositionTitleResponse>> ListPositionTitlesAsync(int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>True when the title name is taken, ignoring case.</summary>
    Task<bool> PositionTitleNameTakenAsync(string name, CancellationToken cancellationToken);

    /// <summary>True when the title is in this tenant.</summary>
    Task<bool> PositionTitleExistsAsync(string id, CancellationToken cancellationToken);

    /// <summary>Stages a position title.</summary>
    void Add(PositionTitle title);

    /// <summary>Lists courses ordered by code.</summary>
    Task<PagedResult<CourseResponse>> ListCoursesAsync(int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>True when another course uses this uppercase code.</summary>
    Task<bool> CourseCodeTakenAsync(string code, string? exceptId, CancellationToken cancellationToken);

    /// <summary>Loads a course for update.</summary>
    Task<Course?> FindCourseAsync(string id, CancellationToken cancellationToken);

    /// <summary>Loads one course with its qualification name.</summary>
    Task<CourseResponse?> GetCourseAsync(string id, CancellationToken cancellationToken);

    /// <summary>Stages a course.</summary>
    void Add(Course course);

    /// <summary>Lists staff ordered by staff number, with departments and appointments.</summary>
    Task<PagedResult<StaffResponse>> ListStaffAsync(int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Loads one staff member with departments and appointments.</summary>
    Task<StaffResponse?> GetStaffAsync(string id, CancellationToken cancellationToken);

    /// <summary>Loads a staff member for update.</summary>
    Task<Staff?> FindStaffAsync(string id, CancellationToken cancellationToken);

    /// <summary>True when another staff member uses this staff number.</summary>
    Task<bool> StaffNumberTakenAsync(string staffNumber, string? exceptId, CancellationToken cancellationToken);

    /// <summary>True when another staff member already has this biometric device id.</summary>
    Task<bool> BiometricIdTakenAsync(string biometricId, string? exceptId, CancellationToken cancellationToken);

    /// <summary>Stages a staff member.</summary>
    void Add(Staff staff);

    /// <summary>How many departments this lecturer is assigned to.</summary>
    Task<int> CountDepartmentsAsync(string staffId, CancellationToken cancellationToken);

    /// <summary>True when this lecturer is already assigned to the department.</summary>
    Task<bool> AssignmentExistsAsync(string staffId, string departmentId, CancellationToken cancellationToken);

    /// <summary>Stages an assignment.</summary>
    void Add(StaffDepartment assignment);

    /// <summary>Removes one assignment. Returns false when it does not exist.</summary>
    Task<bool> RemoveAssignmentAsync(string staffId, string departmentId, CancellationToken cancellationToken);

    /// <summary>Lists appointments for one lecturer.</summary>
    Task<IReadOnlyList<StaffPositionResponse>> ListPositionsAsync(string staffId, CancellationToken cancellationToken);

    /// <summary>Loads existing appointment ranges for overlap checks.</summary>
    Task<IReadOnlyList<DateRange>> ListPositionRangesAsync(string staffId, CancellationToken cancellationToken);

    /// <summary>Stages an appointment.</summary>
    void Add(StaffPosition position);

    /// <summary>Saves the staged changes. A unique-index violation becomes a conflict.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
