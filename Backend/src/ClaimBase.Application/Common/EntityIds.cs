using NUlid;

namespace ClaimBase.Application.Common;

/// <summary>
/// Creates ULID primary keys. The value is a 26-character string from NUlid, not a <see cref="Guid"/> and not an <c>Ulid</c> column.
/// </summary>
public static class EntityIds
{
    /// <summary>Returns a new 26-character ULID string.</summary>
    /// <returns>The identifier.</returns>
    public static string New() => Ulid.NewUlid().ToString();
}
