namespace Shared.DTOs;

public class CursorPagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public string? NextCursor { get; set; }
    public int TotalCount { get; set; }
    public bool HasMore => NextCursor != null;
}