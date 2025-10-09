// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.Tests.ValueObjects;

/// <summary>
/// Tests for the IdempotencyKey value object.
/// Validates creation, edge cases, and conversion from DeliveryId.
/// </summary>
public class IdempotencyKeyTests
{
    [Fact]
    public void Create_WithValidString_ShouldReturnInstance()
    {
        // Arrange & Act
        var key = IdempotencyKey.Create("abc-123-def");

        // Assert
        key.Value.Should().Be("abc-123-def");
    }

    [Fact]
    public void Create_WithWhitespace_ShouldTrimAndReturnInstance()
    {
        // Arrange & Act
        var key = IdempotencyKey.Create("  abc-123  ");

        // Assert
        key.Value.Should().Be("abc-123");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidString_ShouldThrow(string value)
    {
        // Arrange & Act
        var act = () => IdempotencyKey.Create(value!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Idempotency key cannot be null or whitespace.*")
            .And.ParamName.Should().Be("value");
    }

    [Fact]
    public void FromDeliveryId_WithValidDeliveryId_ShouldReturnInstance()
    {
        // Arrange
        var deliveryId = DeliveryId.Create("delivery-123");

        // Act
        var key = IdempotencyKey.FromDeliveryId(deliveryId);

        // Assert
        key.Value.Should().Be("delivery-123");
    }

    [Fact]
    public void FromDeliveryId_WithNullDeliveryId_ShouldThrow()
    {
        // Arrange & Act
        var act = () => IdempotencyKey.FromDeliveryId(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var key = IdempotencyKey.Create("test-key");

        // Act
        var result = key.ToString();

        // Assert
        result.Should().Be("test-key");
    }

    [Fact]
    public void RecordEquality_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var key1 = IdempotencyKey.Create("same-key");
        var key2 = IdempotencyKey.Create("same-key");

        // Act & Assert
        key1.Should().Be(key2);
        (key1 == key2).Should().BeTrue();
    }

    [Fact]
    public void RecordEquality_WithDifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var key1 = IdempotencyKey.Create("key-1");
        var key2 = IdempotencyKey.Create("key-2");

        // Act & Assert
        key1.Should().NotBe(key2);
        (key1 != key2).Should().BeTrue();
    }
}
