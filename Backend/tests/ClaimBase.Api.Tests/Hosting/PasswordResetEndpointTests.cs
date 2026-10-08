using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClaimBase.Application.Common.Interfaces;
using ClaimBase.Domain.Academic;
using ClaimBase.Domain.Identity;
using ClaimBase.Infrastructure.Identity;
using ClaimBase.Infrastructure.Persistence;
using ClaimBase.Shared.Auth;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimBase.Api.Tests.Hosting;

[Collection(PostgresCollection.Name)]
public class PasswordResetEndpointTests
{
    private const string Password = "correct-password";
    private const string NewPassword = "replacement-1";
    private readonly PostgresApiFactory _factory;

    public PasswordResetEndpointTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UnknownEmail_ReturnsThePublicMessage_AndStoresNothing()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = "nobody@claimbase.test" });
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain(PasswordResetCopy.LinkSent);
        Captured("nobody@claimbase.test").Should().BeEmpty();
    }

    [Fact]
    public async Task EmailSharedByTwoTenants_SendsNothing()
    {
        var email = "shared-reset@claimbase.test";
        await AddUserAsync(Id("tenantc"), Id("userc"), email, UserRole.Admin, null, null);
        await AddUserAsync(Id("tenantd"), Id("userd"), email, UserRole.Admin, null, null);
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Captured(email).Should().BeEmpty();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userIds = new[] { Id("userc"), Id("userd") };
        (await db.PasswordResetTokens.IgnoreQueryFilters().CountAsync(token => userIds.Contains(token.UserId))).Should().Be(0);
    }

    [Fact]
    public async Task Reset_ChangesThePassword_EndsTheOldToken_AndRejectsTheUsedLink()
    {
        var email = "reset-me@claimbase.test";
        var userId = Id("usere");
        await AddUserAsync(Id("tenante"), userId, email, UserRole.Admin, null, null);
        using var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        var original = await login.Content.ReadFromJsonAsync<AuthResponse>();
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var forgot = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = " Reset-Me@ClaimBase.test " });
        var forgotBody = await forgot.Content.ReadAsStringAsync();
        forgot.StatusCode.Should().Be(HttpStatusCode.OK);
        forgotBody.Should().Contain(PasswordResetCopy.LinkSent);

        var link = Captured(email).Should().ContainSingle().Subject;
        var token = QueryValue(link.ResetUrl, "token");
        forgotBody.Should().NotContain(token);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stored = await db.PasswordResetTokens.IgnoreQueryFilters().SingleAsync(row => row.UserId == userId && row.UsedAt == null);
            stored.TokenHash.Should().NotBe(token);
            stored.TokenHash.Should().HaveLength(64);
        }

        var reset = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            email,
            token,
            newPassword = NewPassword,
            confirmPassword = NewPassword
        });
        (await reset.Content.ReadAsStringAsync()).Should().Contain(PasswordResetCopy.Reset);

        var oldPassword = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        oldPassword.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", original!.Token);
        var stale = await client.GetAsync("/api/auth/me");
        stale.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var again = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            email,
            token,
            newPassword = "another-password",
            confirmPassword = "another-password"
        });
        again.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        client.DefaultRequestHeaders.Authorization = null;
        var freshLogin = await client.PostAsJsonAsync("/api/auth/login", new { email, password = NewPassword });
        var fresh = await freshLogin.Content.ReadFromJsonAsync<AuthResponse>();
        freshLogin.StatusCode.Should().Be(HttpStatusCode.OK);
        Payload(fresh!.Token).GetProperty("pwd").GetString().Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fresh.Token);
        (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SecondRequest_ReplacesTheFirstLink()
    {
        var email = "replace-link@claimbase.test";
        await AddUserAsync(Id("tenantf"), Id("userf"), email, UserRole.Finance, null, null);
        using var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        var first = QueryValue(Captured(email).Single().ResetUrl, "token");
        await client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        var second = QueryValue(Captured(email).Last().ResetUrl, "token");

        var rejected = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            email,
            token = first,
            newPassword = NewPassword,
            confirmPassword = NewPassword
        });
        rejected.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var accepted = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            email,
            token = second,
            newPassword = NewPassword,
            confirmPassword = NewPassword
        });
        accepted.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Lecturer_CanRequestAResetLink()
    {
        var email = "lecturer-reset@claimbase.test";
        await AddUserAsync(Id("tenantg"), Id("userg"), email, UserRole.Lecturer, null, Id("staffg"));
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Captured(email).Should().ContainSingle();
    }

    [Fact]
    public async Task ParallelResets_AcceptTheLinkOnce()
    {
        var email = "parallel-reset@claimbase.test";
        await AddUserAsync(Id("tenanti"), Id("useri"), email, UserRole.Admin, null, null);
        using var client = _factory.CreateClient();
        (await client.PostAsJsonAsync("/api/auth/forgot-password", new { email })).StatusCode.Should().Be(HttpStatusCode.OK);
        var token = QueryValue(Captured(email).Single().ResetUrl, "token");
        var body = new { email, token, newPassword = NewPassword, confirmPassword = NewPassword };

        using var first = _factory.CreateClient();
        using var second = _factory.CreateClient();
        var results = await Task.WhenAll(
            first.PostAsJsonAsync("/api/auth/reset-password", body),
            second.PostAsJsonAsync("/api/auth/reset-password", body));

        results.Count(result => result.StatusCode == HttpStatusCode.OK).Should().Be(1);
        results.Count(result => result.StatusCode == HttpStatusCode.BadRequest).Should().Be(1);
    }

    [Fact]
    public async Task ParallelForgot_LeavesOneLiveLink()
    {
        var email = "parallel-forgot@claimbase.test";
        var userId = Id("userj");
        await AddUserAsync(Id("tenantj"), userId, email, UserRole.Admin, null, null);
        using var first = _factory.CreateClient();
        using var second = _factory.CreateClient();

        var results = await Task.WhenAll(
            first.PostAsJsonAsync("/api/auth/forgot-password", new { email }),
            second.PostAsJsonAsync("/api/auth/forgot-password", new { email }));

        results.Should().OnlyContain(result => result.StatusCode == HttpStatusCode.OK);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.PasswordResetTokens.IgnoreQueryFilters().CountAsync(token => token.UserId == userId && token.UsedAt == null))
            .Should().Be(1);
    }

    [Fact]
    public async Task WrongEmail_DoesNotSpendTheLink()
    {
        var email = "keep-link@claimbase.test";
        await AddUserAsync(Id("tenanth"), Id("userh"), email, UserRole.Admin, null, null);
        using var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        var token = QueryValue(Captured(email).Single().ResetUrl, "token");

        var wrong = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            email = "other@claimbase.test",
            token,
            newPassword = NewPassword,
            confirmPassword = NewPassword
        });
        wrong.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var right = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            email,
            token,
            newPassword = NewPassword,
            confirmPassword = NewPassword
        });
        right.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private IReadOnlyList<PasswordResetEmailCapture> Captured(string email)
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IPasswordResetEmailLog>().Snapshot()
            .Where(message => message.Email == email)
            .ToArray();
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

        if (staffId is not null && !await db.Staff.IgnoreQueryFilters().AnyAsync(staff => staff.Id == staffId))
            db.Staff.Add(Staff.Create(staffId, tenantId, staffId, "Test Lecturer", null, EmploymentType.PartTime, null));

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

    private static string QueryValue(string url, string name)
    {
        var query = new Uri(url).Query.TrimStart('?');
        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pieces = part.Split('=', 2);
            if (pieces.Length == 2 && pieces[0] == name)
                return Uri.UnescapeDataString(pieces[1]);
        }

        throw new InvalidOperationException($"Query {name} was missing.");
    }

    private static string Id(string suffix) => ("01JRESET" + suffix).PadRight(26, '0')[..26];

    private static JsonElement Payload(string token)
    {
        var segment = token.Split('.')[1];
        var padded = segment.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        using var document = JsonDocument.Parse(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(padded)));
        return document.RootElement.Clone();
    }
}
