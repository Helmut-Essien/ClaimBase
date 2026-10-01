using ClaimBase.Application.Common.Exceptions;
using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Domain.Academic;
using ClaimBase.Infrastructure.Persistence;
using ClaimBase.Shared.Academic;
using ClaimBase.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ClaimBase.Infrastructure.Academic;

/// <summary>EF Core academic store. Lists project in the database and page there.</summary>
public sealed class EfAcademicCatalog : IAcademicCatalog
{
    private readonly AppDbContext _db;

    /// <summary>
    /// Creates the store.
    /// </summary>
    /// <param name="db">Request database context. The tenant filter is already applied.</param>
    public EfAcademicCatalog(AppDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    /// <inheritdoc />
    public Task<PagedResult<FacultyResponse>> ListFacultiesAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        PageAsync(
            _db.Faculties.AsNoTracking().OrderBy(faculty => faculty.Name).Select(faculty => new FacultyResponse
            {
                Id = faculty.Id,
                Name = faculty.Name
            }),
            page,
            pageSize,
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> FacultyNameTakenAsync(string name, CancellationToken cancellationToken)
    {
        var key = name.Trim().ToLowerInvariant();
        return _db.Faculties.AnyAsync(faculty => faculty.Name.ToLower() == key, cancellationToken);
    }

    /// <inheritdoc />
    public void Add(Faculty faculty) => _db.Faculties.Add(faculty);

    /// <inheritdoc />
    public Task<PagedResult<DepartmentResponse>> ListDepartmentsAsync(
        int page,
        int pageSize,
        string? facultyId,
        CancellationToken cancellationToken)
    {
        var departments = _db.Departments.AsNoTracking().AsQueryable();
        if (facultyId is not null)
            departments = departments.Where(department => department.FacultyId == facultyId);

        var query =
            from department in departments
            join faculty in _db.Faculties.AsNoTracking() on department.FacultyId equals faculty.Id
            orderby faculty.Name, department.Name
            select new DepartmentResponse
            {
                Id = department.Id,
                FacultyId = faculty.Id,
                FacultyName = faculty.Name,
                Name = department.Name
            };

        return PageAsync(query, page, pageSize, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> FacultyExistsAsync(string facultyId, CancellationToken cancellationToken) =>
        _db.Faculties.AnyAsync(faculty => faculty.Id == facultyId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> DepartmentNameTakenAsync(string facultyId, string name, CancellationToken cancellationToken)
    {
        var key = name.Trim().ToLowerInvariant();
        return _db.Departments.AnyAsync(
            department => department.FacultyId == facultyId && department.Name.ToLower() == key,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DepartmentExistsAsync(string departmentId, CancellationToken cancellationToken) =>
        _db.Departments.AnyAsync(department => department.Id == departmentId, cancellationToken);

    /// <inheritdoc />
    public Task<DepartmentResponse?> GetDepartmentAsync(string departmentId, CancellationToken cancellationToken)
    {
        var query =
            from department in _db.Departments.AsNoTracking()
            join faculty in _db.Faculties.AsNoTracking() on department.FacultyId equals faculty.Id
            where department.Id == departmentId
            select new DepartmentResponse
            {
                Id = department.Id,
                FacultyId = faculty.Id,
                FacultyName = faculty.Name,
                Name = department.Name
            };

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Add(Department department) => _db.Departments.Add(department);

    /// <inheritdoc />
    public async Task<PagedResult<SemesterResponse>> ListSemestersAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _db.Semesters.AsNoTracking().OrderByDescending(semester => semester.StartDate);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<SemesterResponse>
        {
            Items = rows.Select(semester => new SemesterResponse
            {
                Id = semester.Id,
                Name = semester.Name,
                StartDate = semester.StartDate,
                EndDate = semester.EndDate,
                Status = semester.Status.ToString()
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    /// <inheritdoc />
    public Task<Semester?> FindSemesterAsync(string id, CancellationToken cancellationToken) =>
        _db.Semesters.FirstOrDefaultAsync(semester => semester.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> OpenSemesterOverlapsAsync(DateOnly start, DateOnly end, string exceptId, CancellationToken cancellationToken) =>
        _db.Semesters.AnyAsync(
            semester => semester.Status == SemesterStatus.Open
                && semester.Id != exceptId
                && semester.StartDate <= end
                && start <= semester.EndDate,
            cancellationToken);

    /// <inheritdoc />
    public void Add(Semester semester) => _db.Semesters.Add(semester);

    /// <inheritdoc />
    public Task<PagedResult<QualificationResponse>> ListQualificationsAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        PageAsync(
            _db.Qualifications.AsNoTracking().OrderBy(qualification => qualification.Name).Select(qualification => new QualificationResponse
            {
                Id = qualification.Id,
                Name = qualification.Name
            }),
            page,
            pageSize,
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> QualificationNameTakenAsync(string name, CancellationToken cancellationToken)
    {
        var key = name.Trim().ToLowerInvariant();
        return _db.Qualifications.AnyAsync(qualification => qualification.Name.ToLower() == key, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> QualificationExistsAsync(string id, CancellationToken cancellationToken) =>
        _db.Qualifications.AnyAsync(qualification => qualification.Id == id, cancellationToken);

    /// <inheritdoc />
    public void Add(Qualification qualification) => _db.Qualifications.Add(qualification);

    /// <inheritdoc />
    public Task<PagedResult<PositionTitleResponse>> ListPositionTitlesAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        PageAsync(
            _db.PositionTitles.AsNoTracking().OrderBy(title => title.Name).Select(title => new PositionTitleResponse
            {
                Id = title.Id,
                Name = title.Name
            }),
            page,
            pageSize,
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> PositionTitleNameTakenAsync(string name, CancellationToken cancellationToken)
    {
        var key = name.Trim().ToLowerInvariant();
        return _db.PositionTitles.AnyAsync(title => title.Name.ToLower() == key, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> PositionTitleExistsAsync(string id, CancellationToken cancellationToken) =>
        _db.PositionTitles.AnyAsync(title => title.Id == id, cancellationToken);

    /// <inheritdoc />
    public void Add(PositionTitle title) => _db.PositionTitles.Add(title);

    /// <inheritdoc />
    public Task<PagedResult<CourseResponse>> ListCoursesAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query =
            from course in _db.Courses.AsNoTracking()
            join qualification in _db.Qualifications.AsNoTracking() on course.QualificationId equals qualification.Id
            orderby course.Code
            select new CourseResponse
            {
                Id = course.Id,
                Code = course.Code,
                Name = course.Name,
                QualificationId = qualification.Id,
                QualificationName = qualification.Name
            };

        return PageAsync(query, page, pageSize, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> CourseCodeTakenAsync(string code, string? exceptId, CancellationToken cancellationToken)
    {
        var key = code.Trim().ToUpperInvariant();
        return _db.Courses.AnyAsync(course => course.Code == key && course.Id != exceptId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Course?> FindCourseAsync(string id, CancellationToken cancellationToken) =>
        _db.Courses.FirstOrDefaultAsync(course => course.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<CourseResponse?> GetCourseAsync(string id, CancellationToken cancellationToken)
    {
        var query =
            from course in _db.Courses.AsNoTracking()
            join qualification in _db.Qualifications.AsNoTracking() on course.QualificationId equals qualification.Id
            where course.Id == id
            select new CourseResponse
            {
                Id = course.Id,
                Code = course.Code,
                Name = course.Name,
                QualificationId = qualification.Id,
                QualificationName = qualification.Name
            };

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Add(Course course) => _db.Courses.Add(course);

    /// <inheritdoc />
    public async Task<PagedResult<StaffResponse>> ListStaffAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _db.Staff.AsNoTracking().OrderBy(staff => staff.StaffNumber);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<StaffResponse>
        {
            Items = await MapStaffAsync(rows, cancellationToken),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    /// <inheritdoc />
    public async Task<StaffResponse?> GetStaffAsync(string id, CancellationToken cancellationToken)
    {
        var staff = await _db.Staff.AsNoTracking().FirstOrDefaultAsync(row => row.Id == id, cancellationToken);
        if (staff is null)
            return null;

        var mapped = await MapStaffAsync([staff], cancellationToken);
        return mapped[0];
    }

    /// <inheritdoc />
    public Task<Staff?> FindStaffAsync(string id, CancellationToken cancellationToken) =>
        _db.Staff.FirstOrDefaultAsync(staff => staff.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> StaffNumberTakenAsync(string staffNumber, string? exceptId, CancellationToken cancellationToken)
    {
        var key = staffNumber.Trim();
        return _db.Staff.AnyAsync(staff => staff.StaffNumber == key && staff.Id != exceptId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> BiometricIdTakenAsync(string biometricId, string? exceptId, CancellationToken cancellationToken)
    {
        var key = biometricId.Trim();
        return _db.Staff.AnyAsync(staff => staff.BiometricId == key && staff.Id != exceptId, cancellationToken);
    }

    /// <inheritdoc />
    public void Add(Staff staff) => _db.Staff.Add(staff);

    /// <inheritdoc />
    public Task<int> CountDepartmentsAsync(string staffId, CancellationToken cancellationToken) =>
        _db.StaffDepartments.CountAsync(assignment => assignment.StaffId == staffId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> AssignmentExistsAsync(string staffId, string departmentId, CancellationToken cancellationToken) =>
        _db.StaffDepartments.AnyAsync(
            assignment => assignment.StaffId == staffId && assignment.DepartmentId == departmentId,
            cancellationToken);

    /// <inheritdoc />
    public void Add(StaffDepartment assignment) => _db.StaffDepartments.Add(assignment);

    /// <inheritdoc />
    public async Task<bool> RemoveAssignmentAsync(string staffId, string departmentId, CancellationToken cancellationToken)
    {
        var assignment = await _db.StaffDepartments.FirstOrDefaultAsync(
            row => row.StaffId == staffId && row.DepartmentId == departmentId,
            cancellationToken);
        if (assignment is null)
            return false;

        _db.StaffDepartments.Remove(assignment);
        return true;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StaffPositionResponse>> ListPositionsAsync(string staffId, CancellationToken cancellationToken)
    {
        var rows = await PositionQuery([staffId]).ToListAsync(cancellationToken);
        return rows.Select(row => row.Response).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DateRange>> ListPositionRangesAsync(string staffId, CancellationToken cancellationToken)
    {
        var rows = await _db.StaffPositions.AsNoTracking()
            .Where(position => position.StaffId == staffId)
            .Select(position => new { position.EffectiveFrom, position.EffectiveTo })
            .ToListAsync(cancellationToken);
        return rows.Select(row => new DateRange(row.EffectiveFrom, row.EffectiveTo)).ToList();
    }

    /// <inheritdoc />
    public void Add(StaffPosition position) => _db.StaffPositions.Add(position);

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new ConflictAppException("That value is already in use.");
        }
    }

    private async Task<IReadOnlyList<StaffResponse>> MapStaffAsync(IReadOnlyList<Staff> staff, CancellationToken cancellationToken)
    {
        if (staff.Count == 0)
            return [];

        var ids = staff.Select(row => row.Id).ToArray();
        var departments = await (
            from assignment in _db.StaffDepartments.AsNoTracking()
            join department in _db.Departments.AsNoTracking() on assignment.DepartmentId equals department.Id
            join faculty in _db.Faculties.AsNoTracking() on department.FacultyId equals faculty.Id
            where ids.Contains(assignment.StaffId)
            select new DepartmentRow(
                assignment.StaffId,
                department.Id,
                department.Name,
                faculty.Id,
                faculty.Name)).ToListAsync(cancellationToken);

        var positions = await PositionQuery(ids).ToListAsync(cancellationToken);

        return staff.Select(row => new StaffResponse
        {
            Id = row.Id,
            StaffNumber = row.StaffNumber,
            DisplayName = row.DisplayName,
            Email = row.Email,
            EmploymentType = row.EmploymentType.ToString(),
            BiometricId = row.BiometricId,
            Departments = departments.Where(item => item.StaffId == row.Id).Select(item => item.Response).ToList(),
            Positions = positions.Where(item => item.StaffId == row.Id).Select(item => item.Response).ToList()
        }).ToList();
    }

    private IQueryable<PositionRow> PositionQuery(IReadOnlyCollection<string> staffIds) =>
        from position in _db.StaffPositions.AsNoTracking()
        join title in _db.PositionTitles.AsNoTracking() on position.PositionTitleId equals title.Id
        where staffIds.Contains(position.StaffId)
        orderby position.EffectiveFrom
        select new PositionRow(
            position.StaffId,
            position.Id,
            title.Id,
            title.Name,
            position.EffectiveFrom,
            position.EffectiveTo);

    private static async Task<PagedResult<T>> PageAsync<T>(IQueryable<T> query, int page, int pageSize, CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    private sealed record DepartmentRow(string StaffId, string DepartmentId, string DepartmentName, string FacultyId, string FacultyName)
    {
        public StaffDepartmentResponse Response => new()
        {
            DepartmentId = DepartmentId,
            DepartmentName = DepartmentName,
            FacultyId = FacultyId,
            FacultyName = FacultyName
        };
    }

    private sealed record PositionRow(
        string StaffId,
        string Id,
        string PositionTitleId,
        string PositionTitleName,
        DateOnly EffectiveFrom,
        DateOnly? EffectiveTo)
    {
        public StaffPositionResponse Response => new()
        {
            Id = Id,
            PositionTitleId = PositionTitleId,
            PositionTitleName = PositionTitleName,
            EffectiveFrom = EffectiveFrom,
            EffectiveTo = EffectiveTo
        };
    }
}
