// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;

using ProbotSharp.Bootstrap.Api;

namespace ProbotSharp.Bootstrap.Api.Tests;

public sealed class ConfigurationValidationTests
{
    [Fact]
    public void ValidateRequiredConfiguration_ShouldThrow_WhenConfigurationIsNull()
    {
        // Act
        var act = () => ConfigurationValidation.ValidateRequiredConfiguration(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidateRequiredConfiguration_ShouldSkip_WhenSkipValidationIsTrue()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProbotSharp:SkipConfigurationValidation"] = "true",
            })
            .Build();

        // Act & Assert - should not throw
        ConfigurationValidation.ValidateRequiredConfiguration(config);
    }

    [Fact]
    public void ValidateRequiredConfiguration_ShouldThrow_WhenAppIdMissing()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProbotSharp:WebhookSecret"] = "secret",
                ["ProbotSharp:PrivateKey"] = "key",
            })
            .Build();

        // Act
        var act = () => ConfigurationValidation.ValidateRequiredConfiguration(config);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ProbotSharp:AppId*");
    }

    [Fact]
    public void ValidateRequiredConfiguration_ShouldThrow_WhenWebhookSecretMissing()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProbotSharp:AppId"] = "12345",
                ["ProbotSharp:PrivateKey"] = "key",
            })
            .Build();

        // Act
        var act = () => ConfigurationValidation.ValidateRequiredConfiguration(config);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ProbotSharp:WebhookSecret*");
    }

    [Fact]
    public void ValidateRequiredConfiguration_ShouldThrow_WhenPrivateKeyAndPathMissing()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProbotSharp:AppId"] = "12345",
                ["ProbotSharp:WebhookSecret"] = "secret",
            })
            .Build();

        // Act
        var act = () => ConfigurationValidation.ValidateRequiredConfiguration(config);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PrivateKeyPath*PrivateKey*");
    }

    [Fact]
    public void ValidateRequiredConfiguration_ShouldSucceed_WhenPrivateKeyProvided()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProbotSharp:AppId"] = "12345",
                ["ProbotSharp:WebhookSecret"] = "secret",
                ["ProbotSharp:PrivateKey"] = "-----BEGIN RSA PRIVATE KEY-----\ntest\n-----END RSA PRIVATE KEY-----",
            })
            .Build();

        // Act & Assert - should not throw
        ConfigurationValidation.ValidateRequiredConfiguration(config);
    }

    [Fact]
    public void ValidateRequiredConfiguration_ShouldSucceed_WhenPrivateKeyPathProvided()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProbotSharp:AppId"] = "12345",
                ["ProbotSharp:WebhookSecret"] = "secret",
                ["ProbotSharp:PrivateKeyPath"] = "/path/to/key.pem",
            })
            .Build();

        // Act & Assert - should not throw
        ConfigurationValidation.ValidateRequiredConfiguration(config);
    }

    [Fact]
    public void ValidateRequiredConfiguration_ShouldThrow_WhenPostgreSQLProviderWithoutConnectionString()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProbotSharp:AppId"] = "12345",
                ["ProbotSharp:WebhookSecret"] = "secret",
                ["ProbotSharp:PrivateKey"] = "key",
                ["ProbotSharp:Adapters:Persistence:Provider"] = "PostgreSQL",
            })
            .Build();

        // Act
        var act = () => ConfigurationValidation.ValidateRequiredConfiguration(config);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionStrings:ProbotSharp*");
    }

    [Fact]
    public void ValidateRequiredConfiguration_ShouldThrow_WhenSQLiteProviderWithoutConnectionString()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProbotSharp:AppId"] = "12345",
                ["ProbotSharp:WebhookSecret"] = "secret",
                ["ProbotSharp:PrivateKey"] = "key",
                ["ProbotSharp:Adapters:Persistence:Provider"] = "SQLite",
            })
            .Build();

        // Act
        var act = () => ConfigurationValidation.ValidateRequiredConfiguration(config);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionStrings:ProbotSharp*");
    }

    [Fact]
    public void ValidateRequiredConfiguration_ShouldThrow_WhenRedisCacheWithoutConnectionString()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProbotSharp:AppId"] = "12345",
                ["ProbotSharp:WebhookSecret"] = "secret",
                ["ProbotSharp:PrivateKey"] = "key",
                ["ProbotSharp:Adapters:Cache:Provider"] = "Redis",
            })
            .Build();

        // Act
        var act = () => ConfigurationValidation.ValidateRequiredConfiguration(config);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ProbotSharp:Adapters:Cache:Options:ConnectionString*");
    }

    [Fact]
    public void ValidateRequiredConfiguration_ShouldThrow_WhenRedisIdempotencyWithoutConnectionString()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProbotSharp:AppId"] = "12345",
                ["ProbotSharp:WebhookSecret"] = "secret",
                ["ProbotSharp:PrivateKey"] = "key",
                ["ProbotSharp:Adapters:Idempotency:Provider"] = "Redis",
            })
            .Build();

        // Act
        var act = () => ConfigurationValidation.ValidateRequiredConfiguration(config);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ProbotSharp:Adapters:Idempotency:Options:ConnectionString*");
    }

    [Fact]
    public void ValidateRequiredConfiguration_ShouldThrow_WhenOpenTelemetryMetricsWithoutEndpoint()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProbotSharp:AppId"] = "12345",
                ["ProbotSharp:WebhookSecret"] = "secret",
                ["ProbotSharp:PrivateKey"] = "key",
                ["ProbotSharp:Adapters:Metrics:Provider"] = "OpenTelemetry",
            })
            .Build();

        // Act
        var act = () => ConfigurationValidation.ValidateRequiredConfiguration(config);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ProbotSharp:Adapters:Metrics:Options:OtlpEndpoint*");
    }

    [Fact]
    public void ValidateRequiredConfiguration_ShouldThrow_WhenOpenTelemetryTracingWithoutEndpoint()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProbotSharp:AppId"] = "12345",
                ["ProbotSharp:WebhookSecret"] = "secret",
                ["ProbotSharp:PrivateKey"] = "key",
                ["ProbotSharp:Adapters:Tracing:Provider"] = "OpenTelemetry",
            })
            .Build();

        // Act
        var act = () => ConfigurationValidation.ValidateRequiredConfiguration(config);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ProbotSharp:Adapters:Tracing:Options:OtlpEndpoint*");
    }

    [Fact]
    public void ValidateRequiredConfiguration_ShouldThrow_WhenFileSystemReplayQueueWithoutPath()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProbotSharp:AppId"] = "12345",
                ["ProbotSharp:WebhookSecret"] = "secret",
                ["ProbotSharp:PrivateKey"] = "key",
                ["ProbotSharp:Adapters:ReplayQueue:Provider"] = "FileSystem",
            })
            .Build();

        // Act
        var act = () => ConfigurationValidation.ValidateRequiredConfiguration(config);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ProbotSharp:Adapters:ReplayQueue:Options:Path*");
    }

    [Fact]
    public void ValidateRequiredConfiguration_ShouldThrow_WhenFileSystemDeadLetterQueueWithoutPath()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProbotSharp:AppId"] = "12345",
                ["ProbotSharp:WebhookSecret"] = "secret",
                ["ProbotSharp:PrivateKey"] = "key",
                ["ProbotSharp:Adapters:DeadLetterQueue:Provider"] = "FileSystem",
            })
            .Build();

        // Act
        var act = () => ConfigurationValidation.ValidateRequiredConfiguration(config);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ProbotSharp:Adapters:DeadLetterQueue:Options:Path*");
    }

    [Fact]
    public void ValidateRequiredConfiguration_ShouldSucceed_WhenAllRequiredConfigProvided()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProbotSharp:AppId"] = "12345",
                ["ProbotSharp:WebhookSecret"] = "secret",
                ["ProbotSharp:PrivateKey"] = "key",
                ["ProbotSharp:Adapters:Persistence:Provider"] = "PostgreSQL",
                ["ConnectionStrings:ProbotSharp"] = "Host=localhost;Database=probot;",
                ["ProbotSharp:Adapters:Cache:Provider"] = "Redis",
                ["ProbotSharp:Adapters:Cache:Options:ConnectionString"] = "localhost:6379",
                ["ProbotSharp:Adapters:Idempotency:Provider"] = "Redis",
                ["ProbotSharp:Adapters:Idempotency:Options:ConnectionString"] = "localhost:6379",
                ["ProbotSharp:Adapters:Metrics:Provider"] = "OpenTelemetry",
                ["ProbotSharp:Adapters:Metrics:Options:OtlpEndpoint"] = "http://localhost:4317",
                ["ProbotSharp:Adapters:Tracing:Provider"] = "OpenTelemetry",
                ["ProbotSharp:Adapters:Tracing:Options:OtlpEndpoint"] = "http://localhost:4317",
                ["ProbotSharp:Adapters:ReplayQueue:Provider"] = "FileSystem",
                ["ProbotSharp:Adapters:ReplayQueue:Options:Path"] = "/tmp/replay",
                ["ProbotSharp:Adapters:DeadLetterQueue:Provider"] = "FileSystem",
                ["ProbotSharp:Adapters:DeadLetterQueue:Options:Path"] = "/tmp/dlq",
            })
            .Build();

        // Act & Assert - should not throw
        ConfigurationValidation.ValidateRequiredConfiguration(config);
    }

    [Fact]
    public void ValidateRequiredConfiguration_ShouldCollectMultipleErrors()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        var act = () => ConfigurationValidation.ValidateRequiredConfiguration(config);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ProbotSharp:AppId*")
            .WithMessage("*ProbotSharp:WebhookSecret*")
            .WithMessage("*PrivateKey*");
    }
}
