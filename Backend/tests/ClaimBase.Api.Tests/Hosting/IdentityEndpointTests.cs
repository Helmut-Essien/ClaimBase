using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Domain.Academic;
using ClaimBase.Domain.Identity;
using ClaimBase.Infrastructure.Persistence;
using ClaimBase.Infrastructure.Tenancy;
using ClaimBase.Shared.Auth;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimBase.Api.Tests.Hosting;

[Collection(PostgresCollection.Name)]
public class IdentityEndpointTests
{
    private const string Password = "correct-password";
    private readonly PostgresApiFactory _factory;

    public IdentityEndpointTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TenantFilter_HidesTheOtherUniversity()
    {
        var tenantA = Id("tenanta");
        var tenantB = Id("tenantb");
        var userA = Id("usera");
        var userB = Id("userb");
        await AddUserAsync(tenantA, userA, "a@isolation.test", UserRole.Admin, null, null);
        await AddUserAsync(tenantB, userB, "b@isolation.test", UserRole.Admin, null, null);

        using var scope = _factory.Services.CreateScope();
        var current = scope.ServiceProvider.GetRequiredService<CurrentTenant>();
        current.Set(tenantA, userA, UserRole.Admin, null);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var visibleUsers = await db.Users.Select(user => user.Id).ToListAsync();
        var visibleTenants = await db.Tenants.Select(tenant => tenant.Id).ToListAsync();

        visibleUsers.Should().Equal(userA);
        visibleTenants.Should().Equal(tenantA);
        (await db.Users.IgnoreQueryFilters().AnyAsync(user => user.Id == userB)).Should().BeTrue();
    }

    [Fact]
    public async Task Login_ReturnsAToken_AndMeReturnsTheSameUser()
    {
        var tenantId = Id("tenantme");
        var userId = Id("userme");
        await AddUserAsync(tenantId, userId, "me@claimbase.test", UserRole.Admin, null, null);
        using var client = _factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = " ME@claimbase.test ", password = Password });
        var body = await login.Content.ReadFromJsonAsync<AuthResponse>();

        login.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotBeNull();
        body!.Role.Should().Be(nameof(UserRole.Admin));
        body.CurrencyCode.Should().Be("GHS");
        Payload(body.Token).TryGetProperty("departmentId", out _).Should().BeFalse();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.Token);
        var me = await client.GetAsync("/api/auth/me");
        var profile = await me.Content.ReadFromJsonAsync<MeResponse>();

        me.StatusCode.Should().Be(HttpStatusCode.OK);
        profile!.Email.Should().Be("me@claimbase.test");
        profile.TimeZoneId.Should().Be("Africa/Accra");
        profile.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task HeadOfDepartmentToken_CarriesDepartmentId()
    {
        var departmentId = Id("dept");
        await AddUserAsync(Id("tenanthod"), Id("userhod"), "hod@claimbase.test", UserRole.HeadOfDepartment, departmentId, null);
        using var client = _factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "hod@claimbase.test", password = Password });
        var body = await login.Content.ReadFromJsonAsync<AuthResponse>();

        login.StatusCode.Should().Be(HttpStatusCode.OK);
        Payload(body!.Token).GetProperty("departmentId").GetString().Should().Be(departmentId);
        Payload(body.Token).GetProperty("role").GetString().Should().Be(nameof(UserRole.HeadOfDepartment));
        Payload(body.Token).GetProperty("sub").GetString().Should().Be(Id("userhod"));
        Payload(body.Token).GetProperty("tenantId").GetString().Should().Be(Id("tenanthod"));
    }

    [Fact]
    public async Task Lecturer_IsForbiddenOnMe()
    {
        await AddUserAsync(Id("tenantlec"), Id("userlec"), "lecturer@claimbase.test", UserRole.Lecturer, null, Id("staff"));
        using var client = _factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "lecturer@claimbase.test", password = Password });
        var body = await login.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);

        var me = await client.GetAsync("/api/auth/me");
        var json = await me.Content.ReadFromJsonAsync<JsonElement>();

        me.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        json.GetProperty("message").GetString().Should().Be("Use the ClaimBase mobile app.");
    }

    [Fact]
    public async Task UnknownEmail_IsUnauthorized_AndStillAllowsThePortalOrigin()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email = "nobody@claimbase.test", password = Password })
        };
        request.Headers.Add("Origin", "http://localhost:4201");

        var response = await client.SendAsync(request);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain("http://localhost:4201");
        json.GetProperty("message").GetString().Should().Be("Email or password is incorrect.");
    }

    [Fact]
    public async Task ShortPassword_IsBadRequest()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "me@claimbase.test", password = "short" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MeWithoutAToken_IsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task AddUserAsync(
        string tenantId,
        string userId,
        string email,
        UserRole role,
        string? departmentId,
        string? staffId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwords = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var createdAt = DateTimeOffset.UtcNow;
        if (!await db.Tenants.IgnoreQueryFilters().AnyAsync(tenant => tenant.Id == tenantId))
            db.Tenants.Add(Tenant.Create(tenantId, "Test University", "GHS", "Africa/Accra", createdAt));

        if (departmentId is not null)
        {
            var facultyId = "01JF" + departmentId[4..];
            var campusId = "01JC" + departmentId[4..];
            if (!await db.Campuses.IgnoreQueryFilters().AnyAsync(campus => campus.Id == campusId))
                db.Campuses.Add(Campus.Create(campusId, tenantId, "Campus " + campusId[^6..]));

            if (!await db.Faculties.IgnoreQueryFilters().AnyAsync(faculty => faculty.Id == facultyId))
                db.Faculties.Add(Faculty.Create(facultyId, tenantId, campusId, "Faculty " + facultyId[^6..]));

            if (!await db.Departments.IgnoreQueryFilters().AnyAsync(department => department.Id == departmentId))
                db.Departments.Add(Department.Create(departmentId, tenantId, facultyId, "Department"));
        }

        if (staffId is not null && !await db.Staff.IgnoreQueryFilters().AnyAsync(staff => staff.Id == staffId))
        {
            db.Staff.Add(Staff.Create(staffId, tenantId, staffId, "Test Lecturer", null, EmploymentType.PartTime, null));
        }

        db.Users.Add(User.Create(
            userId,
            tenantId,
            email,
            "Test User",
            passwords.Hash(Password),
            role,
            departmentId,
            staffId,
            createdAt));
        await db.SaveChangesAsync();
    }

    private static string Id(string suffix) => ("01JTEST" + suffix).PadRight(26, '0')[..26];

    private static JsonElement Payload(string token)
    {
        var segment = token.Split('.')[1];
        var padded = segment.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        using var document = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(padded)));
        return document.RootElement.Clone();
    }
}
