// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;

namespace ProbotSharp.Application.Tests.Models;

public class WebhookReplayOptionsTests
{
    #region Default Values Tests

    [Fact]
    public void DefaultConstructor_ShouldSetDefaultValues()
    {
        // Act
        var options = new WebhookReplayOptions();

        // Assert
        options.MaxRetryAttempts.Should().Be(5);
        options.InitialBackoffSeconds.Should().Be(2);
        options.MaxBackoffSeconds.Should().Be(300);
        options.BackoffMultiplier.Should().Be(2.0);
        options.JitterFactor.Should().Be(0.1);
        options.PollIntervalSeconds.Should().Be(1);
    }

    #endregion

    #region Validation Tests - MaxRetryAttempts

    [Fact]
    public void Validate_WithValidMaxRetryAttempts_ShouldNotThrow()
    {
        // Arrange
        var options = new WebhookReplayOptions { MaxRetryAttempts = 10 };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithMaxRetryAttemptsLessThanOne_ShouldThrow()
    {
        // Arrange
        var options = new WebhookReplayOptions { MaxRetryAttempts = 0 };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("MaxRetryAttempts")
            .WithMessage("*Must be at least 1*");
    }

    [Fact]
    public void Validate_WithNegativeMaxRetryAttempts_ShouldThrow()
    {
        // Arrange
        var options = new WebhookReplayOptions { MaxRetryAttempts = -1 };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("MaxRetryAttempts");
    }

    #endregion

    #region Validation Tests - InitialBackoffSeconds

    [Fact]
    public void Validate_WithValidInitialBackoffSeconds_ShouldNotThrow()
    {
        // Arrange
        var options = new WebhookReplayOptions { InitialBackoffSeconds = 5 };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithZeroInitialBackoffSeconds_ShouldNotThrow()
    {
        // Arrange
        var options = new WebhookReplayOptions { InitialBackoffSeconds = 0 };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithNegativeInitialBackoffSeconds_ShouldThrow()
    {
        // Arrange
        var options = new WebhookReplayOptions { InitialBackoffSeconds = -1 };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("InitialBackoffSeconds")
            .WithMessage("*Cannot be negative*");
    }

    #endregion

    #region Validation Tests - MaxBackoffSeconds

    [Fact]
    public void Validate_WithValidMaxBackoffSeconds_ShouldNotThrow()
    {
        // Arrange
        var options = new WebhookReplayOptions
        {
            InitialBackoffSeconds = 2,
            MaxBackoffSeconds = 600,
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithMaxBackoffSecondsEqualToInitial_ShouldNotThrow()
    {
        // Arrange
        var options = new WebhookReplayOptions
        {
            InitialBackoffSeconds = 10,
            MaxBackoffSeconds = 10,
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithMaxBackoffSecondsLessThanInitial_ShouldThrow()
    {
        // Arrange
        var options = new WebhookReplayOptions
        {
            InitialBackoffSeconds = 10,
            MaxBackoffSeconds = 5,
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("MaxBackoffSeconds")
            .WithMessage("*Cannot be less than InitialBackoffSeconds*");
    }

    #endregion

    #region Validation Tests - BackoffMultiplier

    [Fact]
    public void Validate_WithValidBackoffMultiplier_ShouldNotThrow()
    {
        // Arrange
        var options = new WebhookReplayOptions { BackoffMultiplier = 3.0 };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithBackoffMultiplierEqualToOne_ShouldThrow()
    {
        // Arrange
        var options = new WebhookReplayOptions { BackoffMultiplier = 1.0 };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("BackoffMultiplier")
            .WithMessage("*Must be greater than 1.0*");
    }

    [Fact]
    public void Validate_WithBackoffMultiplierLessThanOne_ShouldThrow()
    {
        // Arrange
        var options = new WebhookReplayOptions { BackoffMultiplier = 0.5 };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("BackoffMultiplier");
    }

    #endregion

    #region Validation Tests - JitterFactor

    [Fact]
    public void Validate_WithValidJitterFactor_ShouldNotThrow()
    {
        // Arrange
        var options = new WebhookReplayOptions { JitterFactor = 0.2 };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithJitterFactorZero_ShouldNotThrow()
    {
        // Arrange
        var options = new WebhookReplayOptions { JitterFactor = 0.0 };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithJitterFactorOne_ShouldNotThrow()
    {
        // Arrange
        var options = new WebhookReplayOptions { JitterFactor = 1.0 };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithJitterFactorLessThanZero_ShouldThrow()
    {
        // Arrange
        var options = new WebhookReplayOptions { JitterFactor = -0.1 };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("JitterFactor")
            .WithMessage("*Must be between 0 and 1*");
    }

    [Fact]
    public void Validate_WithJitterFactorGreaterThanOne_ShouldThrow()
    {
        // Arrange
        var options = new WebhookReplayOptions { JitterFactor = 1.1 };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("JitterFactor")
            .WithMessage("*Must be between 0 and 1*");
    }

    #endregion

    #region Validation Tests - PollIntervalSeconds

    [Fact]
    public void Validate_WithValidPollIntervalSeconds_ShouldNotThrow()
    {
        // Arrange
        var options = new WebhookReplayOptions { PollIntervalSeconds = 5 };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithZeroPollIntervalSeconds_ShouldNotThrow()
    {
        // Arrange
        var options = new WebhookReplayOptions { PollIntervalSeconds = 0 };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithNegativePollIntervalSeconds_ShouldThrow()
    {
        // Arrange
        var options = new WebhookReplayOptions { PollIntervalSeconds = -1 };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("PollIntervalSeconds")
            .WithMessage("*Cannot be negative*");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Validate_WithAllValidCustomValues_ShouldNotThrow()
    {
        // Arrange
        var options = new WebhookReplayOptions
        {
            MaxRetryAttempts = 10,
            InitialBackoffSeconds = 5,
            MaxBackoffSeconds = 600,
            BackoffMultiplier = 1.5,
            JitterFactor = 0.25,
            PollIntervalSeconds = 2,
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var options = new WebhookReplayOptions();

        // Act
        options.MaxRetryAttempts = 10;
        options.InitialBackoffSeconds = 5;
        options.MaxBackoffSeconds = 600;
        options.BackoffMultiplier = 3.0;
        options.JitterFactor = 0.2;
        options.PollIntervalSeconds = 2;

        // Assert
        options.MaxRetryAttempts.Should().Be(10);
        options.InitialBackoffSeconds.Should().Be(5);
        options.MaxBackoffSeconds.Should().Be(600);
        options.BackoffMultiplier.Should().Be(3.0);
        options.JitterFactor.Should().Be(0.2);
        options.PollIntervalSeconds.Should().Be(2);
    }

    [Fact]
    public void Validate_WithVeryLargeBackoffMultiplier_ShouldNotThrow()
    {
        // Arrange
        var options = new WebhookReplayOptions { BackoffMultiplier = 10.0 };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithVeryLargeMaxBackoffSeconds_ShouldNotThrow()
    {
        // Arrange
        var options = new WebhookReplayOptions
        {
            InitialBackoffSeconds = 1,
            MaxBackoffSeconds = 86400, // 24 hours
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    #endregion
}
