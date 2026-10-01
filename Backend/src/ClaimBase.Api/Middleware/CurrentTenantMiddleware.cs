using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using ClaimBase.Domain.Identity;
using ClaimBase.Infrastructure.Tenancy;

namespace ClaimBase.Api.Middleware;

/// <summary>
/// Copies <c>sub</c>, <c>tenantId</c>, <c>role</c>, and <c>departmentId</c> onto the scoped tenant before a database context is created.
/// </summary>
public sealed class CurrentTenantMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Creates the middleware.
    /// </summary>
    /// <param name="next">The next component.</param>
    public CurrentTenantMiddleware(RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(next);
        _next = next;
    }

    /// <summary>
    /// Resolves the tenant from a valid token. Anonymous requests leave the filter closed.
    /// </summary>
    /// <param name="context">The current request.</param>
    /// <param name="currentTenant">The scoped tenant.</param>
    public async Task InvokeAsync(HttpContext context, CurrentTenant currentTenant)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(currentTenant);

        if (context.User.Identity?.IsAuthenticated == true)
        {
            if (!TryRead(context.User, out var tenantId, out var userId, out var role, out var departmentId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { message = "Sign in again." });
                return;
            }

            currentTenant.Set(tenantId, userId, role, departmentId);
        }

        await _next(context);
    }

    private static bool TryRead(
        ClaimsPrincipal user,
        out string tenantId,
        out string userId,
        out UserRole role,
        out string? departmentId)
    {
        tenantId = "";
        userId = "";
        role = default;
        departmentId = null;

        var subject = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var tenantClaim = user.FindFirstValue("tenantId");
        var roleClaim = user.FindFirstValue("role");
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(tenantClaim) || string.IsNullOrWhiteSpace(roleClaim))
            return false;

        if (!Enum.TryParse(roleClaim, ignoreCase: false, out role) || !Enum.IsDefined(role))
            return false;

        var departmentClaim = user.FindFirstValue("departmentId");
        // A head of department token without a department would let later screens trust a body value. Reject it.
        if (role == UserRole.HeadOfDepartment)
        {
            if (string.IsNullOrWhiteSpace(departmentClaim))
                return false;

            departmentId = departmentClaim;
        }

        tenantId = tenantClaim;
        userId = subject;
        return true;
    }
}
