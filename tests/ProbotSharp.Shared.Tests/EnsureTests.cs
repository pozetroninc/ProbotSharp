using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Shared.Tests;

public class EnsureTests
{
    [Fact]
    public void NotNull_ShouldReturnValue_WhenNotNull()
    {
        var instance = new object();

        Ensure.NotNull(instance, nameof(instance)).Should().Be(instance);
    }

    [Fact]
    public void NotNull_ShouldThrow_WhenNull()
    {
        Action act = () => Ensure.NotNull<object>(null, "value");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NotNullOrWhiteSpace_ShouldReturnValue_WhenValid()
    {
        Ensure.NotNullOrWhiteSpace("value", "value").Should().Be("value");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void NotNullOrWhiteSpace_ShouldThrow_WhenInvalid(string input)
    {
        Action act = () => Ensure.NotNullOrWhiteSpace(input, "value");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GreaterThanInt_ShouldReturnValue_WhenValid()
    {
        Ensure.GreaterThan(5, 1, "value").Should().Be(5);
    }

    [Fact]
    public void GreaterThanInt_ShouldThrow_WhenInvalid()
    {
        Action act = () => Ensure.GreaterThan(1, 5, "value");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GreaterThanLong_ShouldReturnValue_WhenValid()
    {
        Ensure.GreaterThan(10L, 1L, "value").Should().Be(10L);
    }

    [Fact]
    public void GreaterThanLong_ShouldThrow_WhenInvalid()
    {
        Action act = () => Ensure.GreaterThan(1L, 5L, "value");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Positive_ShouldReturnValue_WhenPositive()
    {
        var span = TimeSpan.FromSeconds(5);

        Ensure.Positive(span, "value").Should().Be(span);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Positive_ShouldThrow_WhenNonPositive(int seconds)
    {
        Action act = () => Ensure.Positive(TimeSpan.FromSeconds(seconds), "value");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
