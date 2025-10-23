// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Infrastructure.Configuration;

namespace ProbotSharp.Infrastructure.Tests.Configuration;

public class AdapterConfigurationTests
{
    [Fact]
    public void SectionName_ShouldReturnCorrectValue()
    {
        // Assert
        AdapterConfiguration.SectionName.Should().Be("ProbotSharp:Adapters");
    }

    [Fact]
    public void Constructor_ShouldInitializeAllAdapterOptions()
    {
        // Act
        var config = new AdapterConfiguration();

        // Assert
        config.Cache.Should().NotBeNull();
        config.Idempotency.Should().NotBeNull();
        config.Persistence.Should().NotBeNull();
        config.ReplayQueue.Should().NotBeNull();
        config.Metrics.Should().NotBeNull();
        config.Tracing.Should().NotBeNull();
    }

    [Fact]
    public void Validate_WithDefaultConfiguration_ShouldNotThrow()
    {
        // Arrange
        var config = new AdapterConfiguration();

        // Act & Assert
        config.Invoking(c => c.Validate()).Should().NotThrow();
    }

    [Fact]
    public void Validate_WithInvalidCache_ShouldThrow()
    {
        // Arrange
        var config = new AdapterConfiguration
        {
            Cache = new CacheAdapterOptions { Provider = CacheProvider.Redis },
        };

        // Act & Assert
        config.Invoking(c => c.Validate())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*requires 'ConnectionString'*");
    }

    [Fact]
    public void Validate_WithInvalidIdempotency_ShouldThrow()
    {
        // Arrange
        var config = new AdapterConfiguration
        {
            Idempotency = new IdempotencyAdapterOptions { Provider = IdempotencyProvider.Redis },
        };

        // Act & Assert
        config.Invoking(c => c.Validate())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*requires 'ConnectionString'*");
    }

    [Fact]
    public void Validate_WithInvalidPersistence_ShouldThrow()
    {
        // Arrange
        var config = new AdapterConfiguration
        {
            Persistence = new PersistenceAdapterOptions { Provider = PersistenceProvider.PostgreSQL },
        };

        // Act & Assert
        config.Invoking(c => c.Validate())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*requires 'ConnectionString'*");
    }

    [Fact]
    public void Validate_WithInvalidReplayQueue_ShouldThrow()
    {
        // Arrange
        var config = new AdapterConfiguration
        {
            ReplayQueue = new ReplayQueueAdapterOptions { Provider = ReplayQueueProvider.FileSystem },
        };

        // Act & Assert
        config.Invoking(c => c.Validate())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*requires 'Path'*");
    }
}

public class CacheAdapterOptionsTests
{
    [Fact]
    public void DefaultProvider_ShouldBeMemory()
    {
        // Arrange & Act
        var options = new CacheAdapterOptions();

        // Assert
        options.Provider.Should().Be(CacheProvider.Memory);
    }

    [Fact]
    public void Options_ShouldBeEmptyByDefault()
    {
        // Arrange & Act
        var options = new CacheAdapterOptions();

        // Assert
        options.Options.Should().BeEmpty();
    }

    [Fact]
    public void GetRedisConnectionString_WhenNotSet_ShouldReturnNull()
    {
        // Arrange
        var options = new CacheAdapterOptions();

        // Act & Assert
        options.GetRedisConnectionString().Should().BeNull();
    }

    [Fact]
    public void GetRedisConnectionString_WhenSet_ShouldReturnValue()
    {
        // Arrange
        var options = new CacheAdapterOptions();
        options.Options["ConnectionString"] = "localhost:6379";

        // Act & Assert
        options.GetRedisConnectionString().Should().Be("localhost:6379");
    }

    [Fact]
    public void GetRedisInstanceName_WhenNotSet_ShouldReturnDefault()
    {
        // Arrange
        var options = new CacheAdapterOptions();

        // Act & Assert
        options.GetRedisInstanceName().Should().Be("ProbotSharp:");
    }

    [Fact]
    public void GetRedisInstanceName_WhenSet_ShouldReturnValue()
    {
        // Arrange
        var options = new CacheAdapterOptions();
        options.Options["InstanceName"] = "MyApp:";

        // Act & Assert
        options.GetRedisInstanceName().Should().Be("MyApp:");
    }

    [Fact]
    public void Validate_WithMemoryProvider_ShouldNotThrow()
    {
        // Arrange
        var options = new CacheAdapterOptions { Provider = CacheProvider.Memory };

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }

    [Fact]
    public void Validate_WithRedisProviderAndNoConnectionString_ShouldThrow()
    {
        // Arrange
        var options = new CacheAdapterOptions { Provider = CacheProvider.Redis };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*Redis*requires 'ConnectionString'*");
    }

    [Fact]
    public void Validate_WithRedisProviderAndConnectionString_ShouldNotThrow()
    {
        // Arrange
        var options = new CacheAdapterOptions
        {
            Provider = CacheProvider.Redis,
            Options = new Dictionary<string, string> { ["ConnectionString"] = "localhost:6379" },
        };

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }

    [Fact]
    public void Validate_WithDistributedProviderAndNoConnectionString_ShouldThrow()
    {
        // Arrange
        var options = new CacheAdapterOptions { Provider = CacheProvider.Distributed };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*Distributed*requires 'ConnectionString'*");
    }

    [Fact]
    public void Validate_WithDistributedProviderAndConnectionString_ShouldNotThrow()
    {
        // Arrange
        var options = new CacheAdapterOptions
        {
            Provider = CacheProvider.Distributed,
            Options = new Dictionary<string, string> { ["ConnectionString"] = "localhost:6379" },
        };

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }
}

public class IdempotencyAdapterOptionsTests
{
    [Fact]
    public void DefaultProvider_ShouldBeDatabase()
    {
        // Arrange & Act
        var options = new IdempotencyAdapterOptions();

        // Assert
        options.Provider.Should().Be(IdempotencyProvider.Database);
    }

    [Fact]
    public void Options_ShouldBeEmptyByDefault()
    {
        // Arrange & Act
        var options = new IdempotencyAdapterOptions();

        // Assert
        options.Options.Should().BeEmpty();
    }

    [Fact]
    public void GetRedisConnectionString_WhenNotSet_ShouldReturnNull()
    {
        // Arrange
        var options = new IdempotencyAdapterOptions();

        // Act & Assert
        options.GetRedisConnectionString().Should().BeNull();
    }

    [Fact]
    public void GetRedisConnectionString_WhenSet_ShouldReturnValue()
    {
        // Arrange
        var options = new IdempotencyAdapterOptions();
        options.Options["ConnectionString"] = "localhost:6379";

        // Act & Assert
        options.GetRedisConnectionString().Should().Be("localhost:6379");
    }

    [Fact]
    public void GetExpirationHours_WhenNotSet_ShouldReturnDefault24()
    {
        // Arrange
        var options = new IdempotencyAdapterOptions();

        // Act & Assert
        options.GetExpirationHours().Should().Be(24);
    }

    [Fact]
    public void GetExpirationHours_WhenSet_ShouldReturnValue()
    {
        // Arrange
        var options = new IdempotencyAdapterOptions();
        options.Options["ExpirationHours"] = "48";

        // Act & Assert
        options.GetExpirationHours().Should().Be(48);
    }

    [Fact]
    public void GetExpirationHours_WhenInvalidValue_ShouldReturnDefault()
    {
        // Arrange
        var options = new IdempotencyAdapterOptions();
        options.Options["ExpirationHours"] = "invalid";

        // Act & Assert
        options.GetExpirationHours().Should().Be(24);
    }

    [Fact]
    public void Validate_WithMemoryProvider_ShouldNotThrow()
    {
        // Arrange
        var options = new IdempotencyAdapterOptions { Provider = IdempotencyProvider.Memory };

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }

    [Fact]
    public void Validate_WithDatabaseProvider_ShouldNotThrow()
    {
        // Arrange
        var options = new IdempotencyAdapterOptions { Provider = IdempotencyProvider.Database };

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }

    [Fact]
    public void Validate_WithRedisProviderAndNoConnectionString_ShouldThrow()
    {
        // Arrange
        var options = new IdempotencyAdapterOptions { Provider = IdempotencyProvider.Redis };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*Redis*requires 'ConnectionString'*");
    }

    [Fact]
    public void Validate_WithRedisProviderAndConnectionString_ShouldNotThrow()
    {
        // Arrange
        var options = new IdempotencyAdapterOptions
        {
            Provider = IdempotencyProvider.Redis,
            Options = new Dictionary<string, string> { ["ConnectionString"] = "localhost:6379" },
        };

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }
}

public class PersistenceAdapterOptionsTests
{
    [Fact]
    public void DefaultProvider_ShouldBeInMemory()
    {
        // Arrange & Act
        var options = new PersistenceAdapterOptions();

        // Assert
        options.Provider.Should().Be(PersistenceProvider.InMemory);
    }

    [Fact]
    public void GetConnectionString_WhenNotSet_ShouldReturnNull()
    {
        // Arrange
        var options = new PersistenceAdapterOptions();

        // Act & Assert
        options.GetConnectionString().Should().BeNull();
    }

    [Fact]
    public void GetConnectionString_WhenSet_ShouldReturnValue()
    {
        // Arrange
        var options = new PersistenceAdapterOptions();
        options.Options["ConnectionString"] = "Host=localhost;Database=probot";

        // Act & Assert
        options.GetConnectionString().Should().Be("Host=localhost;Database=probot");
    }

    [Fact]
    public void Validate_WithInMemoryProvider_ShouldNotThrow()
    {
        // Arrange
        var options = new PersistenceAdapterOptions { Provider = PersistenceProvider.InMemory };

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }

    [Fact]
    public void Validate_WithPostgreSQLProviderAndNoConnectionString_ShouldThrow()
    {
        // Arrange
        var options = new PersistenceAdapterOptions { Provider = PersistenceProvider.PostgreSQL };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*PostgreSQL*requires 'ConnectionString'*");
    }

    [Fact]
    public void Validate_WithPostgreSQLProviderAndConnectionString_ShouldNotThrow()
    {
        // Arrange
        var options = new PersistenceAdapterOptions
        {
            Provider = PersistenceProvider.PostgreSQL,
            Options = new Dictionary<string, string> { ["ConnectionString"] = "Host=localhost" },
        };

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }
}

public class ReplayQueueAdapterOptionsTests
{
    [Fact]
    public void DefaultProvider_ShouldBeInMemory()
    {
        // Arrange & Act
        var options = new ReplayQueueAdapterOptions();

        // Assert
        options.Provider.Should().Be(ReplayQueueProvider.InMemory);
    }

    [Fact]
    public void GetPath_WhenNotSet_ShouldReturnNull()
    {
        // Arrange
        var options = new ReplayQueueAdapterOptions();

        // Act & Assert
        options.GetPath().Should().BeNull();
    }

    [Fact]
    public void GetPath_WhenSet_ShouldReturnValue()
    {
        // Arrange
        var options = new ReplayQueueAdapterOptions();
        options.Options["Path"] = "/var/webhooks";

        // Act & Assert
        options.GetPath().Should().Be("/var/webhooks");
    }

    [Fact]
    public void GetAzureQueueConnectionString_WhenNotSet_ShouldReturnNull()
    {
        // Arrange
        var options = new ReplayQueueAdapterOptions();

        // Act & Assert
        options.GetAzureQueueConnectionString().Should().BeNull();
    }

    [Fact]
    public void GetAzureQueueConnectionString_WhenSet_ShouldReturnValue()
    {
        // Arrange
        var options = new ReplayQueueAdapterOptions();
        options.Options["ConnectionString"] = "DefaultEndpointsProtocol=https";

        // Act & Assert
        options.GetAzureQueueConnectionString().Should().Be("DefaultEndpointsProtocol=https");
    }

    [Fact]
    public void Validate_WithInMemoryProvider_ShouldNotThrow()
    {
        // Arrange
        var options = new ReplayQueueAdapterOptions { Provider = ReplayQueueProvider.InMemory };

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }

    [Fact]
    public void Validate_WithFileSystemProviderAndNoPath_ShouldThrow()
    {
        // Arrange
        var options = new ReplayQueueAdapterOptions { Provider = ReplayQueueProvider.FileSystem };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*FileSystem*requires 'Path'*");
    }

    [Fact]
    public void Validate_WithFileSystemProviderAndPath_ShouldNotThrow()
    {
        // Arrange
        var options = new ReplayQueueAdapterOptions
        {
            Provider = ReplayQueueProvider.FileSystem,
            Options = new Dictionary<string, string> { ["Path"] = "/var/webhooks" },
        };

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }

    [Fact]
    public void Validate_WithAzureQueueProviderAndNoConnectionString_ShouldThrow()
    {
        // Arrange
        var options = new ReplayQueueAdapterOptions { Provider = ReplayQueueProvider.AzureQueue };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*AzureQueue*requires 'ConnectionString'*");
    }

    [Fact]
    public void Validate_WithAzureQueueProviderAndConnectionString_ShouldNotThrow()
    {
        // Arrange
        var options = new ReplayQueueAdapterOptions
        {
            Provider = ReplayQueueProvider.AzureQueue,
            Options = new Dictionary<string, string> { ["ConnectionString"] = "DefaultEndpointsProtocol=https" },
        };

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }
}

public class MetricsAdapterOptionsTests
{
    [Fact]
    public void DefaultProvider_ShouldBeNoOp()
    {
        // Arrange & Act
        var options = new MetricsAdapterOptions();

        // Assert
        options.Provider.Should().Be(MetricsProvider.NoOp);
    }

    [Fact]
    public void GetMeterName_WhenNotSet_ShouldReturnDefault()
    {
        // Arrange
        var options = new MetricsAdapterOptions();

        // Act & Assert
        options.GetMeterName().Should().Be("ProbotSharp");
    }

    [Fact]
    public void GetMeterName_WhenSet_ShouldReturnValue()
    {
        // Arrange
        var options = new MetricsAdapterOptions();
        options.Options["MeterName"] = "MyApp";

        // Act & Assert
        options.GetMeterName().Should().Be("MyApp");
    }

    [Fact]
    public void GetVersion_WhenNotSet_ShouldReturnNull()
    {
        // Arrange
        var options = new MetricsAdapterOptions();

        // Act & Assert
        options.GetVersion().Should().BeNull();
    }

    [Fact]
    public void GetVersion_WhenSet_ShouldReturnValue()
    {
        // Arrange
        var options = new MetricsAdapterOptions();
        options.Options["Version"] = "1.0.0";

        // Act & Assert
        options.GetVersion().Should().Be("1.0.0");
    }

    [Fact]
    public void Validate_ShouldAlwaysSucceed()
    {
        // Arrange
        var options = new MetricsAdapterOptions();

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }
}

public class TracingAdapterOptionsTests
{
    [Fact]
    public void DefaultProvider_ShouldBeNoOp()
    {
        // Arrange & Act
        var options = new TracingAdapterOptions();

        // Assert
        options.Provider.Should().Be(TracingProvider.NoOp);
    }

    [Fact]
    public void GetSourceName_WhenNotSet_ShouldReturnDefault()
    {
        // Arrange
        var options = new TracingAdapterOptions();

        // Act & Assert
        options.GetSourceName().Should().Be("ProbotSharp");
    }

    [Fact]
    public void GetSourceName_WhenSet_ShouldReturnValue()
    {
        // Arrange
        var options = new TracingAdapterOptions();
        options.Options["SourceName"] = "MyApp";

        // Act & Assert
        options.GetSourceName().Should().Be("MyApp");
    }

    [Fact]
    public void GetVersion_WhenNotSet_ShouldReturnNull()
    {
        // Arrange
        var options = new TracingAdapterOptions();

        // Act & Assert
        options.GetVersion().Should().BeNull();
    }

    [Fact]
    public void GetVersion_WhenSet_ShouldReturnValue()
    {
        // Arrange
        var options = new TracingAdapterOptions();
        options.Options["Version"] = "1.0.0";

        // Act & Assert
        options.GetVersion().Should().Be("1.0.0");
    }

    [Fact]
    public void Validate_ShouldAlwaysSucceed()
    {
        // Arrange
        var options = new TracingAdapterOptions();

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }
}
