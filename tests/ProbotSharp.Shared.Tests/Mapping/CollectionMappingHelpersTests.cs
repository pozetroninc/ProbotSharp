// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using FluentAssertions;

using ProbotSharp.Shared.DTOs;
using ProbotSharp.Shared.Mapping;

using Xunit;

namespace ProbotSharp.Shared.Tests.Mapping;

public sealed class CollectionMappingHelpersTests
{
    #region ToPagedResult Tests

    [Fact]
    public void ToPagedResult_WithValidData_ShouldCreatePagedResult()
    {
        // Arrange
        var items = new List<string> { "item1", "item2", "item3" };
        var totalCount = 10;
        var pageNumber = 1;
        var pageSize = 3;

        // Act
        var result = items.ToPagedResult(totalCount, pageNumber, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.Items.Should().BeEquivalentTo(items);
        result.TotalCount.Should().Be(totalCount);
        result.PageNumber.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
        result.TotalPages.Should().Be(4); // 10 total / 3 per page = 4 pages
    }

    [Fact]
    public void ToPagedResult_WithEmptyList_ShouldReturnEmptyPagedResult()
    {
        // Arrange
        var items = new List<int>();

        // Act
        var result = items.ToPagedResult(0, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public void ToPagedResult_WithNullItems_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<string>? items = null;

        // Act
        var act = () => items!.ToPagedResult(10, 1, 5);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Paginate (IQueryable) Tests

    [Fact]
    public void Paginate_Queryable_WithValidParameters_ShouldReturnCorrectPage()
    {
        // Arrange
        var source = Enumerable.Range(1, 20).AsQueryable();
        var pageNumber = 2;
        var pageSize = 5;

        // Act
        var result = source.Paginate(pageNumber, pageSize).ToList();

        // Assert
        result.Should().HaveCount(5);
        result.Should().BeEquivalentTo(new[] { 6, 7, 8, 9, 10 });
    }

    [Fact]
    public void Paginate_Queryable_WithFirstPage_ShouldReturnFirstItems()
    {
        // Arrange
        var source = Enumerable.Range(1, 10).AsQueryable();

        // Act
        var result = source.Paginate(1, 3).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Paginate_Queryable_WithLastPage_ShouldReturnRemainingItems()
    {
        // Arrange
        var source = Enumerable.Range(1, 7).AsQueryable();

        // Act
        var result = source.Paginate(3, 3).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.Should().BeEquivalentTo(new[] { 7 });
    }

    [Fact]
    public void Paginate_Queryable_WithNullSource_ShouldThrowArgumentNullException()
    {
        // Arrange
        IQueryable<int>? source = null;

        // Act
        var act = () => source!.Paginate(1, 10);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Paginate_Queryable_WithInvalidPageNumber_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var source = Enumerable.Range(1, 10).AsQueryable();

        // Act
        var act = () => source.Paginate(0, 5);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("pageNumber");
    }

    [Fact]
    public void Paginate_Queryable_WithInvalidPageSize_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var source = Enumerable.Range(1, 10).AsQueryable();

        // Act
        var act = () => source.Paginate(1, 0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("pageSize");
    }

    #endregion

    #region Paginate (IEnumerable) Tests

    [Fact]
    public void Paginate_Enumerable_WithValidParameters_ShouldReturnCorrectPage()
    {
        // Arrange
        var source = Enumerable.Range(1, 15);
        var pageNumber = 3;
        var pageSize = 4;

        // Act
        var result = source.Paginate(pageNumber, pageSize).ToList();

        // Assert
        result.Should().HaveCount(4);
        result.Should().BeEquivalentTo(new[] { 9, 10, 11, 12 });
    }

    [Fact]
    public void Paginate_Enumerable_WithEmptySource_ShouldReturnEmpty()
    {
        // Arrange
        var source = Enumerable.Empty<int>();

        // Act
        var result = source.Paginate(1, 10).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Paginate_Enumerable_WithNullSource_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<int>? source = null;

        // Act
        var act = () => source!.Paginate(1, 10);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Paginate_Enumerable_WithNegativePageNumber_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var source = Enumerable.Range(1, 10);

        // Act
        var act = () => source.Paginate(-1, 5);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("pageNumber");
    }

    #endregion

    #region Map Tests

    [Fact]
    public void Map_WithValidMapper_ShouldMapAllItems()
    {
        // Arrange
        var sourceItems = new List<int> { 1, 2, 3 };
        var sourcePagedResult = new PagedResult<int>
        {
            Items = sourceItems,
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 3
        };

        // Act
        var result = sourcePagedResult.Map(x => x.ToString());

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEquivalentTo(new[] { "1", "2", "3" });
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public void Map_WithEmptyPagedResult_ShouldReturnEmptyMappedResult()
    {
        // Arrange
        var sourcePagedResult = new PagedResult<int>
        {
            Items = new List<int>(),
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 0
        };

        // Act
        var result = sourcePagedResult.Map(x => x * 2);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public void Map_WithNullSource_ShouldThrowArgumentNullException()
    {
        // Arrange
        PagedResult<int>? source = null;

        // Act
        var act = () => source!.Map(x => x.ToString());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Map_WithNullMapper_ShouldThrowArgumentNullException()
    {
        // Arrange
        var source = new PagedResult<int>
        {
            Items = new List<int> { 1, 2, 3 },
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 3
        };

        // Act
        var act = () => source.Map<int, string>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region WhereIf (IQueryable) Tests

    [Fact]
    public void WhereIf_Queryable_WithTrueCondition_ShouldApplyFilter()
    {
        // Arrange
        var source = Enumerable.Range(1, 10).AsQueryable();

        // Act
        var result = source.WhereIf(true, x => x > 5).ToList();

        // Assert
        result.Should().HaveCount(5);
        result.Should().BeEquivalentTo(new[] { 6, 7, 8, 9, 10 });
    }

    [Fact]
    public void WhereIf_Queryable_WithFalseCondition_ShouldNotApplyFilter()
    {
        // Arrange
        var source = Enumerable.Range(1, 10).AsQueryable();

        // Act
        var result = source.WhereIf(false, x => x > 5).ToList();

        // Assert
        result.Should().HaveCount(10);
        result.Should().BeEquivalentTo(Enumerable.Range(1, 10));
    }

    [Fact]
    public void WhereIf_Queryable_WithNullSource_ShouldThrowArgumentNullException()
    {
        // Arrange
        IQueryable<int>? source = null;

        // Act
        var act = () => source!.WhereIf(true, x => x > 0);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WhereIf_Queryable_WithNullPredicate_ShouldThrowArgumentNullException()
    {
        // Arrange
        var source = Enumerable.Range(1, 10).AsQueryable();

        // Act
        var act = () => source.WhereIf(true, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region WhereIf (IEnumerable) Tests

    [Fact]
    public void WhereIf_Enumerable_WithTrueCondition_ShouldApplyFilter()
    {
        // Arrange
        var source = Enumerable.Range(1, 10);

        // Act
        var result = source.WhereIf(true, x => x % 2 == 0).ToList();

        // Assert
        result.Should().HaveCount(5);
        result.Should().BeEquivalentTo(new[] { 2, 4, 6, 8, 10 });
    }

    [Fact]
    public void WhereIf_Enumerable_WithFalseCondition_ShouldNotApplyFilter()
    {
        // Arrange
        var source = Enumerable.Range(1, 5);

        // Act
        var result = source.WhereIf(false, x => x < 3).ToList();

        // Assert
        result.Should().HaveCount(5);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5 });
    }

    [Fact]
    public void WhereIf_Enumerable_WithNullSource_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<int>? source = null;

        // Act
        var act = () => source!.WhereIf(true, x => x > 0);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WhereIf_Enumerable_WithNullPredicate_ShouldThrowArgumentNullException()
    {
        // Arrange
        var source = Enumerable.Range(1, 10);

        // Act
        var act = () => source.WhereIf(true, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Batch Tests

    [Fact]
    public void Batch_WithValidData_ShouldCreateCorrectBatches()
    {
        // Arrange
        var source = Enumerable.Range(1, 10);
        var batchSize = 3;

        // Act
        var result = source.Batch(batchSize).ToList();

        // Assert
        result.Should().HaveCount(4);
        result[0].Should().BeEquivalentTo(new[] { 1, 2, 3 });
        result[1].Should().BeEquivalentTo(new[] { 4, 5, 6 });
        result[2].Should().BeEquivalentTo(new[] { 7, 8, 9 });
        result[3].Should().BeEquivalentTo(new[] { 10 }); // Partial batch
    }

    [Fact]
    public void Batch_WithEvenDivision_ShouldCreateEqualBatches()
    {
        // Arrange
        var source = Enumerable.Range(1, 9);
        var batchSize = 3;

        // Act
        var result = source.Batch(batchSize).ToList();

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(batch => batch.Should().HaveCount(3));
    }

    [Fact]
    public void Batch_WithEmptySource_ShouldReturnEmpty()
    {
        // Arrange
        var source = Enumerable.Empty<int>();

        // Act
        var result = source.Batch(5).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Batch_WithBatchSizeLargerThanSource_ShouldReturnSingleBatch()
    {
        // Arrange
        var source = Enumerable.Range(1, 3);

        // Act
        var result = source.Batch(10).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Batch_WithNullSource_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<int>? source = null;

        // Act - Force evaluation with ToList() to trigger the exception
        var act = () => source!.Batch(5).ToList();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Batch_WithInvalidBatchSize_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var source = Enumerable.Range(1, 10);

        // Act - Force evaluation with ToList() to trigger the exception
        var act = () => source.Batch(0).ToList();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("batchSize");
    }

    [Fact]
    public void Batch_WithNegativeBatchSize_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var source = Enumerable.Range(1, 10);

        // Act - Force evaluation with ToList() to trigger the exception
        var act = () => source.Batch(-1).ToList();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("batchSize");
    }

    #endregion

    #region ToListAsync Tests

    [Fact]
    public async Task ToListAsync_WithValidAsyncEnumerable_ShouldReturnList()
    {
        // Arrange
        var source = CreateAsyncEnumerable(new[] { 1, 2, 3, 4, 5 });

        // Act
        var result = await source.ToListAsync();

        // Assert
        result.Should().HaveCount(5);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5 });
    }

    [Fact]
    public async Task ToListAsync_WithEmptyAsyncEnumerable_ShouldReturnEmptyList()
    {
        // Arrange
        var source = CreateAsyncEnumerable(Array.Empty<int>());

        // Act
        var result = await source.ToListAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ToListAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var source = CreateAsyncEnumerableWithDelay(new[] { 1, 2, 3 });

        await cts.CancelAsync();

        // Act & Assert
        var act = async () => await source.ToListAsync(cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ToListAsync_WithNullSource_ShouldThrowArgumentNullException()
    {
        // Arrange
        IAsyncEnumerable<int>? source = null;

        // Act
        var act = async () => await source!.ToListAsync();

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region Helper Methods

    private static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }

    private static async IAsyncEnumerable<T> CreateAsyncEnumerableWithDelay<T>(
        IEnumerable<T> items,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(100, cancellationToken);
            yield return item;
        }
    }

    #endregion
}
