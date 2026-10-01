using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClaimBase.Application.Common.Interfaces;
using NUlid;
using ClaimBase.Domain.Academic;
using ClaimBase.Domain.Identity;
using ClaimBase.Infrastructure.Persistence;
using ClaimBase.Shared.Academic;
using ClaimBase.Shared.Auth;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimBase.Api.Tests.Hosting;

[Collection(PostgresCollection.Name)]
public class AcademicEndpointTests
{
    private const string Password = "correct-password";
    private readonly PostgresApiFactory _factory;

    public AcademicEndpointTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_CanCreateFacultyDepartmentCourseAndStaff()
    {
        using var client = await SignInAsync(UserRole.Admin);
        var suffix = NewId()[..6];

        var faculty = await PostAsync<FacultyResponse>(client, "/api/faculties", new { name = "Science " + suffix });
        faculty.Status.Should().Be(HttpStatusCode.Created);

        var department = await PostAsync<DepartmentResponse>(client, "/api/departments", new { facultyId = faculty.Body!.Id, name = "Physics" });
        department.Status.Should().Be(HttpStatusCode.Created);
        department.Body!.FacultyName.Should().Be("Science " + suffix);

        var qualification = await PostAsync<QualificationResponse>(client, "/api/qualifications", new { name = "Degree " + suffix });
        var course = await PostAsync<CourseResponse>(client, "/api/courses", new { code = "cs " + suffix, name = "Intro", qualificationId = qualification.Body!.Id });
        course.Status.Should().Be(HttpStatusCode.Created);
        course.Body!.Code.Should().Be(("CS " + suffix).ToUpperInvariant());

        var staff = await PostAsync<StaffResponse>(client, "/api/staff", new
        {
            staffNumber = "L-" + suffix,
            displayName = "Ada",
            email = " Ada@School.test ",
            employmentType = "PartTime",
            departmentIds = new[] { department.Body.Id }
        });
        staff.Status.Should().Be(HttpStatusCode.Created);
        staff.Body!.Email.Should().Be("ada@school.test");
        staff.Body.Departments.Should().ContainSingle(item => item.DepartmentId == department.Body.Id);
    }

    [Fact]
    public async Task HeadOfDepartmentAndLecturer_CannotChangeAcademicSetup()
    {
        using var hod = await SignInAsync(UserRole.HeadOfDepartment);
        using var lecturer = await SignInAsync(UserRole.Lecturer);

        var hodResponse = await hod.PostAsJsonAsync("/api/faculties", new { name = "Blocked " + NewId() });
        var lecturerResponse = await lecturer.PostAsJsonAsync("/api/faculties", new { name = "Blocked " + NewId() });

        hodResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        lecturerResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await hodResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()
            .Should().Be("Academic setup is limited to tenant admins and admins.");
    }

    [Fact]
    public async Task OtherTenantsFaculty_IsNotFound()
    {
        using var owner = await SignInAsync(UserRole.Admin);
        using var other = await SignInAsync(UserRole.Admin);
        var faculty = await PostAsync<FacultyResponse>(owner, "/api/faculties", new { name = "Owned " + NewId() });

        var response = await other.PostAsJsonAsync("/api/departments", new { facultyId = faculty.Body!.Id, name = "Remote" });
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        json.GetProperty("message").GetString().Should().Be("Faculty was not found.");
    }

    [Fact]
    public async Task OpeningAnOverlappingSemester_IsConflict()
    {
        using var client = await SignInAsync(UserRole.Admin);
        var first = await PostAsync<SemesterResponse>(client, "/api/semesters", new
        {
            name = "First " + NewId(),
            startDate = new DateOnly(2026, 1, 1),
            endDate = new DateOnly(2026, 6, 30)
        });
        var second = await PostAsync<SemesterResponse>(client, "/api/semesters", new
        {
            name = "Second " + NewId(),
            startDate = new DateOnly(2026, 6, 30),
            endDate = new DateOnly(2026, 12, 31)
        });

        var opened = await client.PostAsync($"/api/semesters/{first.Body!.Id}/open", null);
        var overlap = await client.PostAsync($"/api/semesters/{second.Body!.Id}/open", null);
        var message = await overlap.Content.ReadFromJsonAsync<JsonElement>();

        opened.StatusCode.Should().Be(HttpStatusCode.OK);
        overlap.StatusCode.Should().Be(HttpStatusCode.Conflict);
        message.GetProperty("message").GetString().Should().Be("Another open semester already covers these dates.");
    }

    [Fact]
    public async Task LastDepartmentAndOverlappingPosition_AreConflicts()
    {
        using var client = await SignInAsync(UserRole.Admin);
        var suffix = NewId()[..6];
        var faculty = await PostAsync<FacultyResponse>(client, "/api/faculties", new { name = "Arts " + suffix });
        var firstDepartment = await PostAsync<DepartmentResponse>(client, "/api/departments", new { facultyId = faculty.Body!.Id, name = "History" });
        var secondDepartment = await PostAsync<DepartmentResponse>(client, "/api/departments", new { facultyId = faculty.Body.Id, name = "Music" });
        var staff = await PostAsync<StaffResponse>(client, "/api/staff", new
        {
            staffNumber = "P-" + suffix,
            displayName = "Ben",
            employmentType = "FullTime",
            departmentIds = new[] { firstDepartment.Body!.Id }
        });
        var title = await PostAsync<PositionTitleResponse>(client, "/api/position-titles", new { name = "Lecturer " + suffix });

        var last = await client.DeleteAsync($"/api/staff/{staff.Body!.Id}/departments/{firstDepartment.Body.Id}");
        last.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var assigned = await client.PostAsJsonAsync($"/api/staff/{staff.Body.Id}/departments", new { departmentId = secondDepartment.Body!.Id });
        assigned.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstPosition = await client.PostAsJsonAsync($"/api/staff/{staff.Body.Id}/positions", new
        {
            positionTitleId = title.Body!.Id,
            effectiveFrom = new DateOnly(2026, 1, 1),
            effectiveTo = new DateOnly(2026, 6, 1)
        });
        var overlap = await client.PostAsJsonAsync($"/api/staff/{staff.Body.Id}/positions", new
        {
            positionTitleId = title.Body.Id,
            effectiveFrom = new DateOnly(2026, 5, 1),
            effectiveTo = new DateOnly(2026, 8, 1)
        });
        var touching = await client.PostAsJsonAsync($"/api/staff/{staff.Body.Id}/positions", new
        {
            positionTitleId = title.Body.Id,
            effectiveFrom = new DateOnly(2026, 6, 1)
        });

        firstPosition.StatusCode.Should().Be(HttpStatusCode.Created);
        overlap.StatusCode.Should().Be(HttpStatusCode.Conflict);
        touching.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task<HttpClient> SignInAsync(UserRole role)
    {
        var tenantId = NewId();
        var userId = NewId();
        var email = $"{role}-{userId}@claimbase.test".ToLowerInvariant();
        string? departmentId = role == UserRole.HeadOfDepartment ? NewId() : null;
        string? staffId = role == UserRole.Lecturer ? NewId() : null;
        await AddUserAsync(tenantId, userId, email, role, departmentId, staffId);

        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await login.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private async Task AddUserAsync(string tenantId, string userId, string email, UserRole role, string? departmentId, string? staffId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwords = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var createdAt = DateTimeOffset.UtcNow;
        db.Tenants.Add(Tenant.Create(tenantId, "Test University " + tenantId[..6], "GHS", "Africa/Accra", createdAt));

        if (departmentId is not null)
        {
            var facultyId = "01JF" + departmentId[4..];
            db.Faculties.Add(Faculty.Create(facultyId, tenantId, "Faculty " + facultyId[^6..]));
            db.Departments.Add(Department.Create(departmentId, tenantId, facultyId, "Department"));
        }

        if (staffId is not null)
            db.Staff.Add(Staff.Create(staffId, tenantId, staffId, "Test Lecturer", null, EmploymentType.PartTime, null));

        db.Users.Add(User.Create(userId, tenantId, email, "Test User", passwords.Hash(Password), role, departmentId, staffId, createdAt));
        await db.SaveChangesAsync();
    }

    private static async Task<(HttpStatusCode Status, T? Body)> PostAsync<T>(HttpClient client, string path, object body)
    {
        var response = await client.PostAsJsonAsync(path, body);
        if (!response.IsSuccessStatusCode)
            return (response.StatusCode, default);

        return (response.StatusCode, await response.Content.ReadFromJsonAsync<T>());
    }

    private static string NewId() => Ulid.NewUlid().ToString();
}
