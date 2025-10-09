// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Abstractions;

namespace ProbotSharp.Domain.Tests.Abstractions;

/// <summary>
/// Tests for the ValueObject base class.
/// Validates equality semantics, hash code generation, and operator overloads.
/// </summary>
public class ValueObjectTests
{
    private class TestValueObject : ValueObject
    {
        public string Property1 { get; init; } = string.Empty;
        public int Property2 { get; init; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return this.Property1;
            yield return this.Property2;
        }
    }

    private class DifferentTestValueObject : ValueObject
    {
        public string Property1 { get; init; } = string.Empty;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return this.Property1;
        }
    }

    private class ValueObjectWithNullProperty : ValueObject
    {
        public string? NullableProperty { get; init; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return this.NullableProperty;
        }
    }

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var obj1 = new TestValueObject { Property1 = "test", Property2 = 42 };
        var obj2 = new TestValueObject { Property1 = "test", Property2 = 42 };

        // Act & Assert
        obj1.Equals(obj2).Should().BeTrue();
        obj1.Equals((object)obj2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var obj1 = new TestValueObject { Property1 = "test", Property2 = 42 };
        var obj2 = new TestValueObject { Property1 = "test", Property2 = 43 };

        // Act & Assert
        obj1.Equals(obj2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var obj1 = new TestValueObject { Property1 = "test", Property2 = 42 };

        // Act & Assert
        obj1.Equals(null).Should().BeFalse();
        obj1.Equals((object?)null).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithSameReference_ShouldReturnTrue()
    {
        // Arrange
        var obj1 = new TestValueObject { Property1 = "test", Property2 = 42 };

        // Act & Assert
        obj1.Equals(obj1).Should().BeTrue();
        obj1.Equals((object)obj1).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        var obj1 = new TestValueObject { Property1 = "test" };
        var obj2 = new DifferentTestValueObject { Property1 = "test" };

        // Act & Assert
        obj1.Equals(obj2).Should().BeFalse();
        obj1.Equals((object)obj2).Should().BeFalse();
    }

    [Fact]
    public void EqualityOperator_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var obj1 = new TestValueObject { Property1 = "test", Property2 = 42 };
        var obj2 = new TestValueObject { Property1 = "test", Property2 = 42 };

        // Act & Assert
        (obj1 == obj2).Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var obj1 = new TestValueObject { Property1 = "test", Property2 = 42 };
        var obj2 = new TestValueObject { Property1 = "test", Property2 = 43 };

        // Act & Assert
        (obj1 == obj2).Should().BeFalse();
    }

    [Fact]
    public void EqualityOperator_WithBothNull_ShouldReturnTrue()
    {
        // Arrange
        TestValueObject? obj1 = null;
        TestValueObject? obj2 = null;

        // Act & Assert
        (obj1 == obj2).Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_WithOneNull_ShouldReturnFalse()
    {
        // Arrange
        var obj1 = new TestValueObject { Property1 = "test", Property2 = 42 };
        TestValueObject? obj2 = null;

        // Act & Assert
        (obj1 == obj2).Should().BeFalse();
        (obj2 == obj1).Should().BeFalse();
    }

    [Fact]
    public void InequalityOperator_WithSameValues_ShouldReturnFalse()
    {
        // Arrange
        var obj1 = new TestValueObject { Property1 = "test", Property2 = 42 };
        var obj2 = new TestValueObject { Property1 = "test", Property2 = 42 };

        // Act & Assert
        (obj1 != obj2).Should().BeFalse();
    }

    [Fact]
    public void InequalityOperator_WithDifferentValues_ShouldReturnTrue()
    {
        // Arrange
        var obj1 = new TestValueObject { Property1 = "test", Property2 = 42 };
        var obj2 = new TestValueObject { Property1 = "test", Property2 = 43 };

        // Act & Assert
        (obj1 != obj2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldReturnSameHashCode()
    {
        // Arrange
        var obj1 = new TestValueObject { Property1 = "test", Property2 = 42 };
        var obj2 = new TestValueObject { Property1 = "test", Property2 = 42 };

        // Act
        var hash1 = obj1.GetHashCode();
        var hash2 = obj2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void GetHashCode_WithDifferentValues_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var obj1 = new TestValueObject { Property1 = "test", Property2 = 42 };
        var obj2 = new TestValueObject { Property1 = "test", Property2 = 43 };

        // Act
        var hash1 = obj1.GetHashCode();
        var hash2 = obj2.GetHashCode();

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void GetHashCode_WithNullProperty_ShouldNotThrow()
    {
        // Arrange
        var obj = new ValueObjectWithNullProperty { NullableProperty = null };

        // Act
        var act = () => obj.GetHashCode();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Equals_WithNullProperties_ShouldBeEqual()
    {
        // Arrange
        var obj1 = new ValueObjectWithNullProperty { NullableProperty = null };
        var obj2 = new ValueObjectWithNullProperty { NullableProperty = null };

        // Act & Assert
        obj1.Equals(obj2).Should().BeTrue();
        (obj1 == obj2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithOneNullProperty_ShouldNotBeEqual()
    {
        // Arrange
        var obj1 = new ValueObjectWithNullProperty { NullableProperty = "test" };
        var obj2 = new ValueObjectWithNullProperty { NullableProperty = null };

        // Act & Assert
        obj1.Equals(obj2).Should().BeFalse();
        (obj1 == obj2).Should().BeFalse();
    }
}
