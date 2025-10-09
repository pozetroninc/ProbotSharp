// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Infrastructure.Adapters.Idempotency;

using StackExchange.Redis;

namespace ProbotSharp.Infrastructure.Tests.Adapters.Idempotency;

/// <summary>
/// Tests for <see cref="RedisIdempotencyAdapter"/> covering distributed idempotency key tracking.
/// </summary>
public sealed class RedisIdempotencyAdapterTests
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisIdempotencyAdapter> _logger;

    public RedisIdempotencyAdapterTests()
    {
        this._redis = Substitute.For<IConnectionMultiplexer>();
        this._database = Substitute.For<IDatabase>();
        this._redis.GetDatabase().Returns(this._database);
        this._logger = Substitute.For<ILogger<RedisIdempotencyAdapter>>();
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldReturnTrue_WhenKeyDoesNotExist()
    {
        // Arrange
        var adapter = new RedisIdempotencyAdapter(this._redis, this._logger);
        var key = IdempotencyKey.Create("test-key");
        this._database.StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            When.NotExists).Returns(true);

        // Act
        var result = await adapter.TryAcquireAsync(key);

        // Assert
        result.Should().BeTrue();
        await this._database.Received(1).StringSetAsync(
            Arg.Is<RedisKey>(k => k.ToString().Contains(key.Value)),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            When.NotExists);
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldReturnFalse_WhenKeyAlreadyExists()
    {
        // Arrange
        var adapter = new RedisIdempotencyAdapter(this._redis, this._logger);
        var key = IdempotencyKey.Create("existing-key");
        this._database.StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            When.NotExists).Returns(false);

        // Act
        var result = await adapter.TryAcquireAsync(key);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldUseCustomTtl_WhenProvided()
    {
        // Arrange
        var adapter = new RedisIdempotencyAdapter(this._redis, this._logger);
        var key = IdempotencyKey.Create("test-key");
        var customTtl = TimeSpan.FromMinutes(30);
        this._database.StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            When.NotExists).Returns(true);

        // Act
        var result = await adapter.TryAcquireAsync(key, customTtl);

        // Assert
        result.Should().BeTrue();
        await this._database.Received(1).StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            customTtl,
            When.NotExists);
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldReturnFalse_WhenRedisExceptionOccurs()
    {
        // Arrange
        var adapter = new RedisIdempotencyAdapter(this._redis, this._logger);
        var key = IdempotencyKey.Create("test-key");
        this._database.StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            When.NotExists).Returns<bool>(_ => throw new RedisException("Connection failed"));

        // Act
        var result = await adapter.TryAcquireAsync(key);

        // Assert
        result.Should().BeFalse(); // Fail open to avoid blocking webhooks
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldThrowArgumentNullException_WhenKeyIsNull()
    {
        // Arrange
        var adapter = new RedisIdempotencyAdapter(this._redis, this._logger);

        // Act
        var act = async () => await adapter.TryAcquireAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenKeyExists()
    {
        // Arrange
        var adapter = new RedisIdempotencyAdapter(this._redis, this._logger);
        var key = IdempotencyKey.Create("existing-key");
        this._database.KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(true);

        // Act
        var result = await adapter.ExistsAsync(key);

        // Assert
        result.Should().BeTrue();
        await this._database.Received(1).KeyExistsAsync(
            Arg.Is<RedisKey>(k => k.ToString().Contains(key.Value)),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenKeyDoesNotExist()
    {
        // Arrange
        var adapter = new RedisIdempotencyAdapter(this._redis, this._logger);
        var key = IdempotencyKey.Create("nonexistent-key");
        this._database.KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(false);

        // Act
        var result = await adapter.ExistsAsync(key);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenRedisExceptionOccurs()
    {
        // Arrange
        var adapter = new RedisIdempotencyAdapter(this._redis, this._logger);
        var key = IdempotencyKey.Create("test-key");
        this._database.KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns<bool>(_ => throw new RedisException("Connection failed"));

        // Act
        var result = await adapter.ExistsAsync(key);

        // Assert
        result.Should().BeFalse(); // Fail open
    }

    [Fact]
    public async Task ExistsAsync_ShouldThrowArgumentNullException_WhenKeyIsNull()
    {
        // Arrange
        var adapter = new RedisIdempotencyAdapter(this._redis, this._logger);

        // Act
        var act = async () => await adapter.ExistsAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReleaseAsync_ShouldDeleteKey()
    {
        // Arrange
        var adapter = new RedisIdempotencyAdapter(this._redis, this._logger);
        var key = IdempotencyKey.Create("test-key");
        this._database.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(true);

        // Act
        await adapter.ReleaseAsync(key);

        // Assert
        await this._database.Received(1).KeyDeleteAsync(
            Arg.Is<RedisKey>(k => k.ToString().Contains(key.Value)),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task ReleaseAsync_ShouldNotThrow_WhenKeyDoesNotExist()
    {
        // Arrange
        var adapter = new RedisIdempotencyAdapter(this._redis, this._logger);
        var key = IdempotencyKey.Create("nonexistent-key");
        this._database.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(false);

        // Act
        var act = async () => await adapter.ReleaseAsync(key);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ReleaseAsync_ShouldNotThrow_WhenRedisExceptionOccurs()
    {
        // Arrange
        var adapter = new RedisIdempotencyAdapter(this._redis, this._logger);
        var key = IdempotencyKey.Create("test-key");
        this._database.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns<bool>(_ => throw new RedisException("Connection failed"));

        // Act
        var act = async () => await adapter.ReleaseAsync(key);

        // Assert - should not throw, just log the error
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ReleaseAsync_ShouldThrowArgumentNullException_WhenKeyIsNull()
    {
        // Arrange
        var adapter = new RedisIdempotencyAdapter(this._redis, this._logger);

        // Act
        var act = async () => await adapter.ReleaseAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CleanupExpiredAsync_ShouldReturnZero()
    {
        // Arrange
        var adapter = new RedisIdempotencyAdapter(this._redis, this._logger);

        // Act
        var result = await adapter.CleanupExpiredAsync();

        // Assert
        result.Should().Be(0); // Redis handles cleanup automatically via TTL
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenRedisIsNull()
    {
        var act = () => new RedisIdempotencyAdapter(null!, this._logger);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        var act = () => new RedisIdempotencyAdapter(this._redis, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
