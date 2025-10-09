// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.Tests.ValueObjects;

public class DeliveryIdTests
{
    [Fact]
    public void Create_WithValidString_ShouldReturnInstance()
    {
        var id = DeliveryId.Create("abc-123");

        id.Value.Should().Be("abc-123");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidString_ShouldThrow(string value)
    {
        var act = () => DeliveryId.Create(value!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithWhitespace_ShouldTrimAndReturnInstance()
    {
        var id = DeliveryId.Create("  abc-123  ");

        id.Value.Should().Be("abc-123");
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        var id = DeliveryId.Create("delivery-abc-123");

        var result = id.ToString();

        result.Should().Be("delivery-abc-123");
    }

    [Fact]
    public void RecordEquality_WithSameValue_ShouldBeEqual()
    {
        var id1 = DeliveryId.Create("abc-123");
        var id2 = DeliveryId.Create("abc-123");

        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
    }

    [Fact]
    public void RecordEquality_WithDifferentValue_ShouldNotBeEqual()
    {
        var id1 = DeliveryId.Create("abc-123");
        var id2 = DeliveryId.Create("def-456");

        id1.Should().NotBe(id2);
        (id1 != id2).Should().BeTrue();
    }
}
