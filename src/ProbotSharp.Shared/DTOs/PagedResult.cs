namespace ProbotSharp.Shared.DTOs;

/// <summary>
/// Represents a paginated collection of items.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>
    /// The items in the current page.
    /// </summary>
#pragma warning disable CA2227 // Required for JSON deserialization
    public List<T> Items { get; set; } = new();
#pragma warning restore CA2227

    /// <summary>
    /// The current page number (1-based).
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// The total number of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// The total number of pages.
    /// </summary>
    public int TotalPages => this.PageSize > 0 ? (int)Math.Ceiling((double)this.TotalCount / this.PageSize) : 0;

    /// <summary>
    /// Indicates whether there is a previous page.
    /// </summary>
    public bool HasPrevious => this.PageNumber > 1;

    /// <summary>
    /// Indicates whether there is a next page.
    /// </summary>
    public bool HasNext => this.PageNumber < this.TotalPages;
}
