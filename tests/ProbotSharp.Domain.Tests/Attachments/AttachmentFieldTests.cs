// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Attachments;

namespace ProbotSharp.Domain.Tests.Attachments;

public class AttachmentFieldTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var field = new AttachmentField();

        // Assert
        field.Title.Should().BeEmpty();
        field.Value.Should().BeEmpty();
        field.Short.Should().BeFalse();
    }

    [Fact]
    public void Title_ShouldBeSettable()
    {
        // Arrange
        var field = new AttachmentField();
        var expectedTitle = "Status";

        // Act
        field.Title = expectedTitle;

        // Assert
        field.Title.Should().Be(expectedTitle);
    }

    [Fact]
    public void Value_ShouldBeSettable()
    {
        // Arrange
        var field = new AttachmentField();
        var expectedValue = "In Progress";

        // Act
        field.Value = expectedValue;

        // Assert
        field.Value.Should().Be(expectedValue);
    }

    [Fact]
    public void Short_ShouldBeSettableToTrue()
    {
        // Arrange
        var field = new AttachmentField();

        // Act
        field.Short = true;

        // Assert
        field.Short.Should().BeTrue();
    }

    [Fact]
    public void Short_ShouldBeSettableToFalse()
    {
        // Arrange
        var field = new AttachmentField { Short = true };

        // Act
        field.Short = false;

        // Assert
        field.Short.Should().BeFalse();
    }

    [Theory]
    [InlineData("Priority", "High", true)]
    [InlineData("Status", "Completed", false)]
    [InlineData("", "", false)]
    [InlineData("Key", "", true)]
    [InlineData("", "Value", false)]
    public void Properties_ShouldSupportVariousCombinations(string title, string value, bool isShort)
    {
        // Arrange & Act
        var field = new AttachmentField
        {
            Title = title,
            Value = value,
            Short = isShort
        };

        // Assert
        field.Title.Should().Be(title);
        field.Value.Should().Be(value);
        field.Short.Should().Be(isShort);
    }

    [Fact]
    public void Title_ShouldHandleSpecialCharacters()
    {
        // Arrange
        var field = new AttachmentField();
        var specialTitle = "Status: ⚡ with emoji & symbols!";

        // Act
        field.Title = specialTitle;

        // Assert
        field.Title.Should().Be(specialTitle);
    }

    [Fact]
    public void Value_ShouldHandleMultilineText()
    {
        // Arrange
        var field = new AttachmentField();
        var multilineValue = "Line 1\nLine 2\nLine 3";

        // Act
        field.Value = multilineValue;

        // Assert
        field.Value.Should().Be(multilineValue);
        field.Value.Should().Contain("\n");
    }

    [Fact]
    public void Value_ShouldHandleLongText()
    {
        // Arrange
        var field = new AttachmentField();
        var longValue = new string('a', 1000);

        // Act
        field.Value = longValue;

        // Assert
        field.Value.Should().HaveLength(1000);
        field.Value.Should().Be(longValue);
    }

    [Fact]
    public void ObjectInitializer_ShouldSetAllProperties()
    {
        // Arrange & Act
        var field = new AttachmentField
        {
            Title = "Environment",
            Value = "Production",
            Short = true
        };

        // Assert
        field.Title.Should().Be("Environment");
        field.Value.Should().Be("Production");
        field.Short.Should().BeTrue();
    }
}
