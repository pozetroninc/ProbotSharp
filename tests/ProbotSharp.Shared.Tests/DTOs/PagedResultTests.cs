// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Shared.DTOs;

namespace ProbotSharp.Shared.Tests.DTOs;

public class PagedResultTests
{
    #region Constructor and Default Values

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var result = new PagedResult<string>();

        // Assert
        result.Items.Should().BeEmpty();
        result.PageNumber.Should().Be(0);
        result.PageSize.Should().Be(0);
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
        result.HasPrevious.Should().BeFalse();
        result.HasNext.Should().BeFalse();
    }

    #endregion

    #region TotalPages Calculation

    [Fact]
    public void TotalPages_WithZeroPageSize_ShouldReturnZero()
    {
        // Arrange
        var result = new PagedResult<int>
        {
            PageSize = 0,
            TotalCount = 100,
        };

        // Act & Assert
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public void TotalPages_WithZeroTotalCount_ShouldReturnZero()
    {
        // Arrange
        var result = new PagedResult<int>
        {
            PageSize = 10,
            TotalCount = 0,
        };

        // Act & Assert
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public void TotalPages_WithExactDivision_ShouldCalculateCorrectly()
    {
        // Arrange
        var result = new PagedResult<int>
        {
            PageSize = 10,
            TotalCount = 100,
        };

        // Act & Assert
        result.TotalPages.Should().Be(10);
    }

    [Fact]
    public void TotalPages_WithRemainder_ShouldRoundUp()
    {
        // Arrange
        var result = new PagedResult<int>
        {
            PageSize = 10,
            TotalCount = 95,
        };

        // Act & Assert
        result.TotalPages.Should().Be(10); // 95 / 10 = 9.5, rounds up to 10
    }

    [Fact]
    public void TotalPages_WithOneItem_ShouldReturnOne()
    {
        // Arrange
        var result = new PagedResult<int>
        {
            PageSize = 10,
            TotalCount = 1,
        };

        // Act & Assert
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public void TotalPages_WithItemsLessThanPageSize_ShouldReturnOne()
    {
        // Arrange
        var result = new PagedResult<int>
        {
            PageSize = 10,
            TotalCount = 5,
        };

        // Act & Assert
        result.TotalPages.Should().Be(1);
    }

    #endregion

    #region HasPrevious Tests

    [Fact]
    public void HasPrevious_WhenPageNumberIsOne_ShouldBeFalse()
    {
        // Arrange
        var result = new PagedResult<int> { PageNumber = 1 };

        // Act & Assert
        result.HasPrevious.Should().BeFalse();
    }

    [Fact]
    public void HasPrevious_WhenPageNumberIsZero_ShouldBeFalse()
    {
        // Arrange
        var result = new PagedResult<int> { PageNumber = 0 };

        // Act & Assert
        result.HasPrevious.Should().BeFalse();
    }

    [Fact]
    public void HasPrevious_WhenPageNumberIsTwo_ShouldBeTrue()
    {
        // Arrange
        var result = new PagedResult<int> { PageNumber = 2 };

        // Act & Assert
        result.HasPrevious.Should().BeTrue();
    }

    [Fact]
    public void HasPrevious_WhenPageNumberIsGreaterThanOne_ShouldBeTrue()
    {
        // Arrange
        var result = new PagedResult<int> { PageNumber = 5 };

        // Act & Assert
        result.HasPrevious.Should().BeTrue();
    }

    #endregion

    #region HasNext Tests

    [Fact]
    public void HasNext_WhenOnLastPage_ShouldBeFalse()
    {
        // Arrange
        var result = new PagedResult<int>
        {
            PageNumber = 10,
            PageSize = 10,
            TotalCount = 100,
        };

        // Act & Assert
        result.HasNext.Should().BeFalse();
    }

    [Fact]
    public void HasNext_WhenOnFirstPage_ShouldBeTrue()
    {
        // Arrange
        var result = new PagedResult<int>
        {
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 100,
        };

        // Act & Assert
        result.HasNext.Should().BeTrue();
    }

    [Fact]
    public void HasNext_WhenOnMiddlePage_ShouldBeTrue()
    {
        // Arrange
        var result = new PagedResult<int>
        {
            PageNumber = 5,
            PageSize = 10,
            TotalCount = 100,
        };

        // Act & Assert
        result.HasNext.Should().BeTrue();
    }

    [Fact]
    public void HasNext_WhenPageNumberExceedsTotalPages_ShouldBeFalse()
    {
        // Arrange
        var result = new PagedResult<int>
        {
            PageNumber = 11,
            PageSize = 10,
            TotalCount = 100,
        };

        // Act & Assert
        result.HasNext.Should().BeFalse();
    }

    [Fact]
    public void HasNext_WhenNoData_ShouldBeFalse()
    {
        // Arrange
        var result = new PagedResult<int>
        {
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 0,
        };

        // Act & Assert
        result.HasNext.Should().BeFalse();
    }

    #endregion

    #region Items Collection Tests

    [Fact]
    public void Items_CanBePopulated()
    {
        // Arrange
        var result = new PagedResult<int>
        {
            Items = new List<int> { 1, 2, 3, 4, 5 },
        };

        // Act & Assert
        result.Items.Should().HaveCount(5);
        result.Items.Should().ContainInOrder(1, 2, 3, 4, 5);
    }

    [Fact]
    public void Items_CanBeModified()
    {
        // Arrange
        var result = new PagedResult<string>();

        // Act
        result.Items.Add("item1");
        result.Items.Add("item2");

        // Assert
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public void Items_WithComplexTypes_ShouldWork()
    {
        // Arrange
        var items = new List<TestData>
        {
            new() { Id = 1, Name = "Test1" },
            new() { Id = 2, Name = "Test2" },
        };

        // Act
        var result = new PagedResult<TestData> { Items = items };

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items[0].Name.Should().Be("Test1");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void PagedResult_FirstPage_ShouldHaveCorrectState()
    {
        // Arrange & Act
        var result = new PagedResult<int>
        {
            Items = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 95,
        };

        // Assert
        result.TotalPages.Should().Be(10);
        result.HasPrevious.Should().BeFalse();
        result.HasNext.Should().BeTrue();
    }

    [Fact]
    public void PagedResult_MiddlePage_ShouldHaveCorrectState()
    {
        // Arrange & Act
        var result = new PagedResult<int>
        {
            Items = new List<int> { 41, 42, 43, 44, 45, 46, 47, 48, 49, 50 },
            PageNumber = 5,
            PageSize = 10,
            TotalCount = 95,
        };

        // Assert
        result.TotalPages.Should().Be(10);
        result.HasPrevious.Should().BeTrue();
        result.HasNext.Should().BeTrue();
    }

    [Fact]
    public void PagedResult_LastPage_ShouldHaveCorrectState()
    {
        // Arrange & Act
        var result = new PagedResult<int>
        {
            Items = new List<int> { 91, 92, 93, 94, 95 },
            PageNumber = 10,
            PageSize = 10,
            TotalCount = 95,
        };

        // Assert
        result.TotalPages.Should().Be(10);
        result.HasPrevious.Should().BeTrue();
        result.HasNext.Should().BeFalse();
    }

    [Fact]
    public void PagedResult_SinglePage_ShouldHaveCorrectState()
    {
        // Arrange & Act
        var result = new PagedResult<int>
        {
            Items = new List<int> { 1, 2, 3 },
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 3,
        };

        // Assert
        result.TotalPages.Should().Be(1);
        result.HasPrevious.Should().BeFalse();
        result.HasNext.Should().BeFalse();
    }

    [Fact]
    public void PagedResult_EmptyResult_ShouldHaveCorrectState()
    {
        // Arrange & Act
        var result = new PagedResult<int>
        {
            Items = new List<int>(),
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 0,
        };

        // Assert
        result.TotalPages.Should().Be(0);
        result.HasPrevious.Should().BeFalse();
        result.HasNext.Should().BeFalse();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void TotalPages_WithLargeNumbers_ShouldCalculateCorrectly()
    {
        // Arrange
        var result = new PagedResult<int>
        {
            PageSize = 100,
            TotalCount = 10000,
        };

        // Act & Assert
        result.TotalPages.Should().Be(100);
    }

    [Fact]
    public void TotalPages_WithVeryLargePageSize_ShouldReturnOne()
    {
        // Arrange
        var result = new PagedResult<int>
        {
            PageSize = 1000,
            TotalCount = 100,
        };

        // Act & Assert
        result.TotalPages.Should().Be(1);
    }

    #endregion

    private sealed class TestData
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
