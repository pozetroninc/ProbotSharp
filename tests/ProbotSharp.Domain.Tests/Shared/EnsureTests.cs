// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Domain.Tests.Shared;

public class EnsureTests
{
    [Fact]
    public void NotNull_WithNonNullValue_ShouldReturnValue()
    {
        var value = new object();

        Ensure.NotNull(value, nameof(value)).Should().BeSameAs(value);
    }

    [Fact]
    public void NotNull_WithNullValue_ShouldThrow()
    {
        var act = () => Ensure.NotNull<object>(null, "value");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NotNullOrWhiteSpace_WithValidValue_ShouldReturnValue()
    {
        Ensure.NotNullOrWhiteSpace("value", "value").Should().Be("value");
    }

    [Fact]
    public void NotNullOrWhiteSpace_WithInvalidValue_ShouldThrow()
    {
        var act = () => Ensure.NotNullOrWhiteSpace("   ", "value");

        act.Should().Throw<ArgumentException>();
    }
}
