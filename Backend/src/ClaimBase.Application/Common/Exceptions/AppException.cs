namespace ClaimBase.Application.Common.Exceptions;

/// <summary>Application error that maps to an HTTP status. The message is safe to return to the client.</summary>
public abstract class AppException : Exception
{
    /// <summary>
    /// Creates an application error.
    /// </summary>
    /// <param name="statusCode">HTTP status code.</param>
    /// <param name="message">Client-safe message.</param>
    protected AppException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    /// <summary>HTTP status code.</summary>
    public int StatusCode { get; }
}

/// <summary>Sign-in failed or the caller is not authenticated. Maps to 401.</summary>
public sealed class UnauthorizedAppException : AppException
{
    /// <summary>Creates a generic sign-in failure so callers cannot tell which field was wrong.</summary>
    public UnauthorizedAppException()
        : base(401, "Email or password is incorrect.")
    {
    }
}

/// <summary>The caller is signed in but cannot use this route. Maps to 403.</summary>
public sealed class ForbiddenAppException : AppException
{
    /// <summary>
    /// Creates a forbidden response.
    /// </summary>
    /// <param name="message">Client-safe reason.</param>
    public ForbiddenAppException(string message)
        : base(403, message)
    {
    }
}

/// <summary>The row is missing, including when it belongs to another tenant. Maps to 404.</summary>
public sealed class NotFoundAppException : AppException
{
    /// <summary>
    /// Creates a not-found response.
    /// </summary>
    /// <param name="message">Client-safe reason.</param>
    public NotFoundAppException(string message)
        : base(404, message)
    {
    }
}

/// <summary>The write conflicts with an existing row. Maps to 409.</summary>
public sealed class ConflictAppException : AppException
{
    /// <summary>
    /// Creates a conflict response.
    /// </summary>
    /// <param name="message">Client-safe reason.</param>
    public ConflictAppException(string message)
        : base(409, message)
    {
    }
}
