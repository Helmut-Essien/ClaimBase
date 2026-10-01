using ClaimBase.Application.Common.Exceptions;

namespace ClaimBase.Application.Features;

/// <summary>Turns a semester state-machine failure into HTTP 409.</summary>
internal static class StatusChange
{
    public static void Run(Action change)
    {
        try
        {
            change();
        }
        catch (InvalidOperationException exception)
        {
            throw new ConflictAppException(exception.Message);
        }
    }
}
