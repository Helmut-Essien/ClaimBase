using ClaimBase.Application.Common.Exceptions;
using FluentValidation;

namespace ClaimBase.Api.Middleware;

/// <summary>Maps application and validation failures to status codes. Unexpected errors stay generic.</summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Creates the middleware.
    /// </summary>
    /// <param name="next">The next component.</param>
    /// <param name="logger">Logger for unexpected failures.</param>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(logger);
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the rest of the pipeline and writes a JSON error when it fails.
    /// </summary>
    /// <param name="context">The current request.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await _next(context);
        }
        catch (ValidationException exception)
        {
            await WriteAsync(context, StatusCodes.Status400BadRequest, new
            {
                errors = exception.Errors.Select(error => new { property = error.PropertyName, message = error.ErrorMessage })
            });
        }
        catch (AppException exception)
        {
            await WriteAsync(context, exception.StatusCode, new { message = exception.Message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled API exception.");
            await WriteAsync(context, StatusCodes.Status500InternalServerError, new { message = "Something went wrong." });
        }
    }

    private static Task WriteAsync(HttpContext context, int statusCode, object body)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(body);
    }
}
