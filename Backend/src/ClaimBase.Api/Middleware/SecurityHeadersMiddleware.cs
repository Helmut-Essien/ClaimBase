namespace ClaimBase.Api.Middleware;

/// <summary>
/// Adds baseline browser security headers to every API response.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Creates the middleware.
    /// </summary>
    /// <param name="next">The next component in the pipeline.</param>
    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(next);
        _next = next;
    }

    /// <summary>
    /// Sets <c>nosniff</c>, a denying frame policy, and <c>no-store</c> before the rest of the pipeline runs.
    /// </summary>
    /// <param name="context">The current HTTP request.</param>
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Cache-Control"] = "no-store";
        headers["Referrer-Policy"] = "no-referrer";
        return _next(context);
    }
}
