namespace ClaimBase.Shared.Common;

/// <summary>Paging bounds for list routes. Page size is 1 to 100 and defaults to 20.</summary>
public static class PageLimits
{
    /// <summary>First page.</summary>
    public const int DefaultPage = 1;

    /// <summary>Default page size.</summary>
    public const int DefaultSize = 20;

    /// <summary>Largest page size.</summary>
    public const int MaxSize = 100;
}

/// <summary>One page of results.</summary>
/// <typeparam name="T">Row type.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>Rows for this page.</summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>1-based page number.</summary>
    public required int Page { get; init; }

    /// <summary>Requested page size.</summary>
    public required int PageSize { get; init; }

    /// <summary>Total rows matching the filter, before paging.</summary>
    public required int TotalCount { get; init; }
}
