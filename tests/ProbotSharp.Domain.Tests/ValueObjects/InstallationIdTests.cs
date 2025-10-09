// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.Tests.ValueObjects;

public class InstallationIdTests
{
    [Fact]
    public void Create_WithPositiveValue_ShouldReturnInstance()
    {
        var id = InstallationId.Create(999);

        id.Value.Should().Be(999);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Create_WithNonPositiveValue_ShouldThrow(long value)
    {
        var act = () => InstallationId.Create(value);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
