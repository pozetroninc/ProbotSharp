using ProbotSharp.Shared.DTOs;

namespace ProbotSharp.Shared.Mapping;

/// <summary>
/// Helper methods for common collection transformations such as pagination and filtering.
/// </summary>
public static class CollectionMappingHelpers
{
    /// <summary>
    /// Creates a paged result from a collection of items.
    /// </summary>
    /// <typeparam name="T">The type of items.</typeparam>
    /// <param name="items">The items in the current page.</param>
    /// <param name="totalCount">The total number of items across all pages.</param>
    /// <param name="pageNumber">The current page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A PagedResult containing the items and pagination metadata.</returns>
    public static PagedResult<T> ToPagedResult<T>(
        this IEnumerable<T> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        ArgumentNullException.ThrowIfNull(items);

        return new PagedResult<T>
        {
            Items = items.ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Applies pagination to a queryable collection.
    /// </summary>
    /// <typeparam name="T">The type of items.</typeparam>
    /// <param name="source">The source queryable collection.</param>
    /// <param name="pageNumber">The page number (1-based). Must be at least 1.</param>
    /// <param name="pageSize">The page size. Must be at least 1.</param>
    /// <returns>A paginated subset of the source collection.</returns>
    public static IQueryable<T> Paginate<T>(
        this IQueryable<T> source,
        int pageNumber,
        int pageSize)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be at least 1.");
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be at least 1.");
        }

        return source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
    }

    /// <summary>
    /// Applies pagination to an enumerable collection.
    /// </summary>
    /// <typeparam name="T">The type of items.</typeparam>
    /// <param name="source">The source enumerable collection.</param>
    /// <param name="pageNumber">The page number (1-based). Must be at least 1.</param>
    /// <param name="pageSize">The page size. Must be at least 1.</param>
    /// <returns>A paginated subset of the source collection.</returns>
    public static IEnumerable<T> Paginate<T>(
        this IEnumerable<T> source,
        int pageNumber,
        int pageSize)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be at least 1.");
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be at least 1.");
        }

        return source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
    }

    /// <summary>
    /// Converts a PagedResult from one type to another using a mapping function.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <param name="source">The source paged result.</param>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A new PagedResult with mapped items.</returns>
    public static PagedResult<TDestination> Map<TSource, TDestination>(
        this PagedResult<TSource> source,
        Func<TSource, TDestination> mapper)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(mapper);

        return new PagedResult<TDestination>
        {
            Items = source.Items.Select(mapper).ToList(),
            PageNumber = source.PageNumber,
            PageSize = source.PageSize,
            TotalCount = source.TotalCount
        };
    }

    /// <summary>
    /// Filters a collection based on a predicate if the condition is true.
    /// This is useful for building dynamic queries.
    /// </summary>
    /// <typeparam name="T">The type of items.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="condition">The condition to check before applying the filter.</param>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The filtered collection if condition is true, otherwise the original collection.</returns>
    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> source,
        bool condition,
        Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        return condition ? source.Where(predicate).AsQueryable() : source;
    }

    /// <summary>
    /// Filters an enumerable collection based on a predicate if the condition is true.
    /// This is useful for building dynamic queries.
    /// </summary>
    /// <typeparam name="T">The type of items.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="condition">The condition to check before applying the filter.</param>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The filtered collection if condition is true, otherwise the original collection.</returns>
    public static IEnumerable<T> WhereIf<T>(
        this IEnumerable<T> source,
        bool condition,
        Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        return condition ? source.Where(predicate) : source;
    }

    /// <summary>
    /// Batches an enumerable collection into fixed-size batches.
    /// </summary>
    /// <typeparam name="T">The type of items.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="batchSize">The size of each batch.</param>
    /// <returns>An enumerable of batches.</returns>
    public static IEnumerable<IEnumerable<T>> Batch<T>(
        this IEnumerable<T> source,
        int batchSize)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (batchSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be at least 1.");
        }

        var batch = new List<T>(batchSize);

        foreach (var item in source)
        {
            batch.Add(item);

            if (batch.Count == batchSize)
            {
                yield return batch;
                batch = new List<T>(batchSize);
            }
        }

        if (batch.Count > 0)
        {
            yield return batch;
        }
    }

    /// <summary>
    /// Converts an async enumerable to a list asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of items.</typeparam>
    /// <param name="source">The async enumerable source.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list containing all items from the async enumerable.</returns>
    public static async Task<List<T>> ToListAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        var list = new List<T>();

        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            list.Add(item);
        }

        return list;
    }
}
