// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.Tests.ValueObjects;

public class GitHubAppIdTests
{
    [Fact]
    public void Create_WithPositiveValue_ShouldReturnInstance()
    {
        var id = GitHubAppId.Create(12345);

        id.Value.Should().Be(12345);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveValue_ShouldThrow(long value)
    {
        var act = () => GitHubAppId.Create(value);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ToString_ShouldReturnValueAsString()
    {
        var id = GitHubAppId.Create(12345);

        var result = id.ToString();

        result.Should().Be("12345");
    }

    [Fact]
    public void Equals_WithSameValue_ShouldBeEqual()
    {
        var id1 = GitHubAppId.Create(12345);
        var id2 = GitHubAppId.Create(12345);

        id1.Equals(id2).Should().BeTrue();
        (id1 == id2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldNotBeEqual()
    {
        var id1 = GitHubAppId.Create(12345);
        var id2 = GitHubAppId.Create(67890);

        id1.Equals(id2).Should().BeFalse();
        (id1 != id2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_WithSameValue_ShouldReturnSameHashCode()
    {
        var id1 = GitHubAppId.Create(12345);
        var id2 = GitHubAppId.Create(12345);

        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }
}
