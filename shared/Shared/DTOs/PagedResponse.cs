namespace Shared.DTOs;

/// <summary>
/// Standard paginated response wrapper for offset-based pagination.
/// </summary>
/// <typeparam name="T">The type of items in the response</typeparam>
public class PagedResponse<T>
{
    /// <summary>
    /// The list of items in the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; set; } = [];

    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Indicates if there are more pages after the current page.
    /// </summary>
    public bool HasMore => Page < TotalPages;

    /// <summary>
    /// Indicates if there are pages before the current page.
    /// </summary>
    public bool HasPrevious => Page > 1;

    /// <summary>
    /// Creates a new PagedResponse from a collection.
    /// </summary>
    public static PagedResponse<T> Create(IEnumerable<T> items, int totalCount, int page, int pageSize)
    {
        return new PagedResponse<T>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
