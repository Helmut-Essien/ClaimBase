using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Domain.Academic;
using ClaimBase.Domain.Identity;
using ClaimBase.Infrastructure.Persistence;
using ClaimBase.Shared.Academic;
using ClaimBase.Shared.Auth;
using ClaimBase.Shared.Common;
using ClaimBase.Shared.Rates;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUlid;

namespace ClaimBase.Api.Tests.Hosting;

[Collection(PostgresCollection.Name)]
public class RateEndpointTests
{
    private const string Password = "correct-password";
    private readonly PostgresApiFactory _factory;

    public RateEndpointTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_CanSetAnOpenEndedTeachingRateAndATouchingReplacement()
    {
        using var client = await SignInAsync(UserRole.Admin);
        var suffix = NewId()[..6];
        var title = await PostAsync<PositionTitleResponse>(client, "/api/position-titles", new { name = "Senior Lecturer " + suffix });
        var diploma = await PostAsync<QualificationResponse>(client, "/api/qualifications", new { name = "Diploma " + suffix });
        var degree = await PostAsync<QualificationResponse>(client, "/api/qualifications", new { name = "Degree " + suffix });

        var open = await PostAsync<TeachingRateResponse>(client, "/api/rates/teaching", new
        {
            positionTitleId = title.Body!.Id,
            qualificationId = diploma.Body!.Id,
            amount = 10.5m,
            effectiveFrom = new DateOnly(2026, 1, 1)
        });
        var boundedOverlap = await client.PostAsJsonAsync("/api/rates/teaching", new
        {
            positionTitleId = title.Body.Id,
            qualificationId = diploma.Body.Id,
            amount = 11m,
            effectiveFrom = new DateOnly(2025, 6, 1),
            effectiveTo = new DateOnly(2026, 3, 1)
        });
        var otherQualification = await PostAsync<TeachingRateResponse>(client, "/api/rates/teaching", new
        {
            positionTitleId = title.Body.Id,
            qualificationId = degree.Body!.Id,
            amount = 5m,
            effectiveFrom = new DateOnly(2026, 1, 1)
        });
        var replacement = await PostAsync<TeachingRateResponse>(client, "/api/rates/teaching", new
        {
            positionTitleId = title.Body.Id,
            qualificationId = diploma.Body.Id,
            amount = 12m,
            effectiveFrom = new DateOnly(2027, 1, 1)
        });

        open.Status.Should().Be(HttpStatusCode.Created);
        open.Body!.Amount.Should().Be(10.5m);
        open.Body.EffectiveTo.Should().BeNull();
        open.Body.PositionTitleName.Should().Be("Senior Lecturer " + suffix);
        open.Body.QualificationName.Should().Be("Diploma " + suffix);
        boundedOverlap.StatusCode.Should().Be(HttpStatusCode.Conflict);
        otherQualification.Status.Should().Be(HttpStatusCode.Created);
        replacement.Status.Should().Be(HttpStatusCode.Created);
        replacement.Body!.EffectiveTo.Should().BeNull();

        var listed = await client.GetFromJsonAsync<PagedResult<TeachingRateResponse>>(
            $"/api/rates/teaching?positionTitleId={title.Body.Id}&qualificationId={diploma.Body.Id}");
        listed!.TotalCount.Should().Be(2);
        listed.Items.Should().ContainSingle(item => item.Id == open.Body.Id && item.EffectiveTo == new DateOnly(2027, 1, 1));
        listed.Items.Should().ContainSingle(item => item.Id == replacement.Body.Id && item.EffectiveTo == null);
    }

    [Fact]
    public async Task TeachingList_OnADate_ReturnsOnlyTheRowInForce()
    {
        using var client = await SignInAsync(UserRole.Admin);
        var suffix = NewId()[..6];
        var title = await PostAsync<PositionTitleResponse>(client, "/api/position-titles", new { name = "Lecturer " + suffix });
        var qualification = await PostAsync<QualificationResponse>(client, "/api/qualifications", new { name = "Diploma " + suffix });
        var early = await PostAsync<TeachingRateResponse>(client, "/api/rates/teaching", new
        {
            positionTitleId = title.Body!.Id,
            qualificationId = qualification.Body!.Id,
            amount = 5m,
            effectiveFrom = new DateOnly(2026, 1, 1),
            effectiveTo = new DateOnly(2026, 6, 1)
        });
        var later = await PostAsync<TeachingRateResponse>(client, "/api/rates/teaching", new
        {
            positionTitleId = title.Body.Id,
            qualificationId = qualification.Body.Id,
            amount = 10m,
            effectiveFrom = new DateOnly(2026, 6, 1)
        });

        var duringEarly = await client.GetFromJsonAsync<PagedResult<TeachingRateResponse>>(
            $"/api/rates/teaching?on=2026-03-01&positionTitleId={title.Body.Id}&qualificationId={qualification.Body.Id}");
        var onTheEndDay = await client.GetFromJsonAsync<PagedResult<TeachingRateResponse>>(
            $"/api/rates/teaching?on=2026-06-01&positionTitleId={title.Body.Id}&qualificationId={qualification.Body.Id}");

        early.Status.Should().Be(HttpStatusCode.Created);
        later.Status.Should().Be(HttpStatusCode.Created);
        duringEarly!.Items.Should().ContainSingle(item => item.Id == early.Body!.Id);
        onTheEndDay!.Items.Should().ContainSingle(item => item.Id == later.Body!.Id);
    }

    [Fact]
    public async Task TransportTimeline_RejectsOverlapAndAllowsATouchingNextAmount()
    {
        using var client = await SignInAsync(UserRole.Admin);
        var first = await PostAsync<TransportRateResponse>(client, "/api/rates/transport", new
        {
            amount = 20m,
            effectiveFrom = new DateOnly(2026, 1, 1),
            effectiveTo = new DateOnly(2026, 6, 1)
        });
        var overlap = await client.PostAsJsonAsync("/api/rates/transport", new
        {
            amount = 25m,
            effectiveFrom = new DateOnly(2026, 5, 1),
            effectiveTo = new DateOnly(2026, 8, 1)
        });
        var next = await PostAsync<TransportRateResponse>(client, "/api/rates/transport", new
        {
            amount = 25m,
            effectiveFrom = new DateOnly(2026, 6, 1)
        });

        first.Status.Should().Be(HttpStatusCode.Created);
        overlap.StatusCode.Should().Be(HttpStatusCode.Conflict);
        next.Status.Should().Be(HttpStatusCode.Created);
        next.Body!.EffectiveTo.Should().BeNull();

        var listed = await client.GetFromJsonAsync<PagedResult<TransportRateResponse>>("/api/rates/transport?pageSize=100");
        listed!.Items.Select(item => item.Id).Should().Contain([first.Body!.Id, next.Body.Id]);
    }

    [Fact]
    public async Task MissingCatalogAndOtherTenant_AreNotFound()
    {
        using var owner = await SignInAsync(UserRole.Admin);
        using var other = await SignInAsync(UserRole.Admin);
        var title = await PostAsync<PositionTitleResponse>(owner, "/api/position-titles", new { name = "Lecturer " + NewId()[..6] });
        var qualification = await PostAsync<QualificationResponse>(owner, "/api/qualifications", new { name = "Diploma " + NewId()[..6] });

        var missingTitle = await other.PostAsJsonAsync("/api/rates/teaching", new
        {
            positionTitleId = title.Body!.Id,
            qualificationId = qualification.Body!.Id,
            amount = 10m,
            effectiveFrom = new DateOnly(2026, 1, 1)
        });
        var missingQualification = await owner.PostAsJsonAsync("/api/rates/teaching", new
        {
            positionTitleId = title.Body.Id,
            qualificationId = NewId(),
            amount = 10m,
            effectiveFrom = new DateOnly(2026, 1, 1)
        });

        missingTitle.StatusCode.Should().Be(HttpStatusCode.NotFound);
        missingQualification.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task HeadOfDepartmentFinanceAndLecturer_CannotSetRates()
    {
        using var hod = await SignInAsync(UserRole.HeadOfDepartment);
        using var finance = await SignInAsync(UserRole.Finance);
        using var lecturer = await SignInAsync(UserRole.Lecturer);

        var hodResponse = await hod.PostAsJsonAsync("/api/rates/transport", new { amount = 10m, effectiveFrom = new DateOnly(2026, 1, 1) });
        var financeResponse = await finance.GetAsync("/api/rates/transport");
        var lecturerResponse = await lecturer.GetAsync("/api/rates/teaching");

        hodResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        financeResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        lecturerResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ConcurrentCreates_KeepASingleOpenTeachingRateAndASingleOpenTransportRate()
    {
        using var client = await SignInAsync(UserRole.Admin);
        var suffix = NewId()[..6];
        var title = await PostAsync<PositionTitleResponse>(client, "/api/position-titles", new { name = "Lecturer " + suffix });
        var qualification = await PostAsync<QualificationResponse>(client, "/api/qualifications", new { name = "Diploma " + suffix });

        var teaching = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => client.PostAsJsonAsync("/api/rates/teaching", new
        {
            positionTitleId = title.Body!.Id,
            qualificationId = qualification.Body!.Id,
            amount = 10m,
            effectiveFrom = new DateOnly(2026, 1, 1)
        })));
        var transport = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => client.PostAsJsonAsync("/api/rates/transport", new
        {
            amount = 20m,
            effectiveFrom = new DateOnly(2026, 1, 1)
        })));

        teaching.Count(response => response.StatusCode == HttpStatusCode.Created).Should().Be(1);
        teaching.Count(response => response.StatusCode == HttpStatusCode.Conflict).Should().Be(5);
        transport.Count(response => response.StatusCode == HttpStatusCode.Created).Should().Be(1);
        transport.Count(response => response.StatusCode == HttpStatusCode.Conflict).Should().Be(5);
    }

    [Fact]
    public async Task NegativeAmountAndOmittedDate_AreBadRequest()
    {
        using var client = await SignInAsync(UserRole.Admin);

        var negative = await client.PostAsJsonAsync("/api/rates/transport", new { amount = -1m, effectiveFrom = new DateOnly(2026, 1, 1) });
        var omitted = await client.PostAsJsonAsync("/api/rates/transport", new { amount = 10m });

        negative.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        omitted.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
            var campusId = "01JC" + departmentId[4..];
            db.Campuses.Add(Campus.Create(campusId, tenantId, "Campus " + campusId[^6..]));
            db.Faculties.Add(Faculty.Create(facultyId, tenantId, campusId, "Faculty " + facultyId[^6..]));
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
