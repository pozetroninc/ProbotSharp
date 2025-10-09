// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Infrastructure.Adapters.Idempotency;
using StackExchange.Redis;
using Xunit;

namespace ProbotSharp.Infrastructure.Tests.Integration;

/// <summary>
/// Integration tests for RedisIdempotencyAdapter using real Redis via Testcontainers.
/// These tests validate actual Redis behavior for idempotency tracking including TTL and concurrency.
/// </summary>
[Collection("Redis Integration Tests")]
public sealed class RedisIdempotencyIntegrationTests : IAsyncLifetime
{
    private IContainer? _redisContainer;
    private IConnectionMultiplexer? _redis;
    private RedisIdempotencyAdapter? _sut;
    private ILogger<RedisIdempotencyAdapter>? _logger;
    private bool _available;

    public async Task InitializeAsync()
    {
        _available = true;
        try
        {
            _redisContainer = new ContainerBuilder()
                .WithImage("redis:7-alpine")
                .WithPortBinding(6379, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379))
                .WithCleanUp(true)
                .WithAutoRemove(true)
                .Build();

            await _redisContainer.StartAsync();

            var port = _redisContainer.GetMappedPublicPort(6379);
            var connectionString = $"localhost:{port}";

            _redis = await ConnectionMultiplexer.ConnectAsync(connectionString);
            _logger = Substitute.For<ILogger<RedisIdempotencyAdapter>>();
            _sut = new RedisIdempotencyAdapter(_redis, _logger);
        }
        catch (ResourceReaperException)
        {
            _available = false;
        }
        catch (RedisConnectionException)
        {
            _available = false;
        }
        catch (Exception)
        {
            _available = false;
        }
    }

    public async Task DisposeAsync()
    {
        _redis?.Dispose();
        if (_redisContainer != null)
        {
            await _redisContainer.StopAsync();
            await _redisContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyDoesNotExist_ShouldReturnFalse()
    {
        if (!_available) return;
        // Arrange
        var key = IdempotencyKey.Create(Guid.NewGuid().ToString());

        // Act
        var result = await _sut!.ExistsAsync(key);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenKeyDoesNotExist_ShouldReturnTrue()
    {
        if (!_available) return;
        // Arrange
        var key = IdempotencyKey.Create(Guid.NewGuid().ToString());

        // Act
        var acquired = await _sut!.TryAcquireAsync(key, TimeSpan.FromMinutes(5));

        // Assert
        acquired.Should().BeTrue();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenKeyExists_ShouldReturnFalse()
    {
        if (!_available) return;
        // Arrange
        var key = IdempotencyKey.Create(Guid.NewGuid().ToString());
        await _sut!.TryAcquireAsync(key, TimeSpan.FromMinutes(5));

        // Act
        var secondAttempt = await _sut.TryAcquireAsync(key, TimeSpan.FromMinutes(5));

        // Assert
        secondAttempt.Should().BeFalse();
    }

    [Fact]
    public async Task TryAcquireAndExistsAsync_ShouldWorkTogether()
    {
        if (!_available) return;
        // Arrange
        var key = IdempotencyKey.Create(Guid.NewGuid().ToString());

        // Act
        var existsBefore = await _sut!.ExistsAsync(key);
        var acquired = await _sut.TryAcquireAsync(key, TimeSpan.FromMinutes(5));
        var existsAfter = await _sut.ExistsAsync(key);

        // Assert
        existsBefore.Should().BeFalse();
        acquired.Should().BeTrue();
        existsAfter.Should().BeTrue();
    }

    [Fact]
    public async Task TryAcquireAsync_WithTtl_ShouldExpireAfterTtl()
    {
        if (!_available) return;
        // Arrange
        var key = IdempotencyKey.Create(Guid.NewGuid().ToString());

        // Act
        await _sut!.TryAcquireAsync(key, TimeSpan.FromSeconds(2));
        var immediateCheck = await _sut.ExistsAsync(key);

        await Task.Delay(TimeSpan.FromSeconds(3));

        var afterExpiry = await _sut.ExistsAsync(key);

        // Assert
        immediateCheck.Should().BeTrue();
        afterExpiry.Should().BeFalse();
    }

    [Fact]
    public async Task ReleaseAsync_ShouldDeleteKey()
    {
        if (!_available) return;
        // Arrange
        var key = IdempotencyKey.Create(Guid.NewGuid().ToString());
        await _sut!.TryAcquireAsync(key, TimeSpan.FromMinutes(5));

        // Act
        await _sut.ReleaseAsync(key);
        var exists = await _sut.ExistsAsync(key);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ReleaseAsync_AfterRelease_ShouldAllowReacquisition()
    {
        if (!_available) return;
        // Arrange
        var key = IdempotencyKey.Create(Guid.NewGuid().ToString());
        await _sut!.TryAcquireAsync(key, TimeSpan.FromMinutes(5));
        await _sut.ReleaseAsync(key);

        // Act
        var reacquired = await _sut.TryAcquireAsync(key, TimeSpan.FromMinutes(5));

        // Assert
        reacquired.Should().BeTrue();
    }

    [Fact]
    public async Task ConcurrentAcquires_ShouldOnlyAllowOneSuccess()
    {
        if (!_available) return;
        // Arrange
        var key = IdempotencyKey.Create(Guid.NewGuid().ToString());
        var tasks = new List<Task<bool>>();

        // Act - Multiple concurrent acquire attempts for same key
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_sut!.TryAcquireAsync(key, TimeSpan.FromMinutes(5)));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - Exactly one should succeed
        results.Count(r => r).Should().Be(1);
        results.Count(r => !r).Should().Be(9);
    }

    [Fact]
    public async Task MultipleDifferentKeys_ShouldStoreIndependently()
    {
        if (!_available) return;
        // Arrange
        var key1 = IdempotencyKey.Create(Guid.NewGuid().ToString());
        var key2 = IdempotencyKey.Create(Guid.NewGuid().ToString());
        var key3 = IdempotencyKey.Create(Guid.NewGuid().ToString());

        // Act
        await _sut!.TryAcquireAsync(key1, TimeSpan.FromMinutes(5));
        await _sut.TryAcquireAsync(key2, TimeSpan.FromMinutes(5));
        // Intentionally don't acquire key3

        // Assert
        var exists1 = await _sut.ExistsAsync(key1);
        var exists2 = await _sut.ExistsAsync(key2);
        var exists3 = await _sut.ExistsAsync(key3);

        exists1.Should().BeTrue();
        exists2.Should().BeTrue();
        exists3.Should().BeFalse();
    }

    [Fact]
    public async Task TryAcquireAsync_AfterRedisFlush_ShouldAllowNewAcquisition()
    {
        if (!_available) return;
        // Arrange
        var key1 = IdempotencyKey.Create(Guid.NewGuid().ToString());
        await _sut!.TryAcquireAsync(key1, TimeSpan.FromMinutes(5));

        // Act - Flush Redis
        var db = _redis!.GetDatabase();
        await db.ExecuteAsync("FLUSHALL");

        // Try to acquire after flush
        var key2 = IdempotencyKey.Create(Guid.NewGuid().ToString());
        var acquired2 = await _sut.TryAcquireAsync(key2, TimeSpan.FromMinutes(5));

        // Assert
        var exists1 = await _sut.ExistsAsync(key1);
        var exists2 = await _sut.ExistsAsync(key2);

        exists1.Should().BeFalse();
        acquired2.Should().BeTrue();
        exists2.Should().BeTrue();
    }

    [Fact]
    public async Task CleanupExpiredAsync_ShouldReturnZero()
    {
        if (!_available) return;
        // Arrange & Act
        var result = await _sut!.CleanupExpiredAsync();

        // Assert - Redis handles cleanup automatically via TTL
        result.Should().Be(0);
    }

    [Fact]
    public async Task TryAcquireAsync_WithDefaultTtl_ShouldUse24Hours()
    {
        if (!_available) return;
        // Arrange
        var key = IdempotencyKey.Create(Guid.NewGuid().ToString());

        // Act - Use default TTL (no explicit timeToLive parameter)
        var acquired = await _sut!.TryAcquireAsync(key);
        var exists = await _sut.ExistsAsync(key);

        // Verify TTL is set (should be close to 24 hours)
        var db = _redis!.GetDatabase();
        var redisKey = $"idempotency:{key.Value}";
        var ttl = await db.KeyTimeToLiveAsync(redisKey);

        // Assert
        acquired.Should().BeTrue();
        exists.Should().BeTrue();
        ttl.Should().NotBeNull();
        ttl!.Value.TotalHours.Should().BeInRange(23.9, 24.0); // Close to 24 hours
    }

    [Fact]
    public async Task TryAcquireAsync_WithMultipleKeys_ShouldHandleLargeVolume()
    {
        if (!_available) return;
        // Arrange
        var keys = Enumerable.Range(1, 100)
            .Select(i => IdempotencyKey.Create($"webhook-{Guid.NewGuid()}"))
            .ToList();

        // Act - Acquire all keys
        var acquireResults = new List<bool>();
        foreach (var key in keys)
        {
            var acquired = await _sut!.TryAcquireAsync(key, TimeSpan.FromMinutes(5));
            acquireResults.Add(acquired);
        }

        // Verify all exist
        var existsResults = new List<bool>();
        foreach (var key in keys)
        {
            var exists = await _sut!.ExistsAsync(key);
            existsResults.Add(exists);
        }

        // Assert
        acquireResults.Should().AllSatisfy(r => r.Should().BeTrue());
        existsResults.Should().AllSatisfy(r => r.Should().BeTrue());
    }

    [Fact]
    public async Task TryAcquireAsync_WithVeryShortTtl_ShouldExpireQuickly()
    {
        if (!_available) return;
        // Arrange
        var key = IdempotencyKey.Create(Guid.NewGuid().ToString());

        // Act
        await _sut!.TryAcquireAsync(key, TimeSpan.FromSeconds(1));
        var immediate = await _sut.ExistsAsync(key);

        await Task.Delay(TimeSpan.FromSeconds(2));

        var afterExpiry = await _sut.ExistsAsync(key);
        var reacquireAfterExpiry = await _sut.TryAcquireAsync(key, TimeSpan.FromMinutes(5));

        // Assert
        immediate.Should().BeTrue();
        afterExpiry.Should().BeFalse();
        reacquireAfterExpiry.Should().BeTrue(); // Should be able to reacquire after expiry
    }

    [Fact]
    public async Task ConcurrentReadsAndWrites_ShouldHandleCorrectly()
    {
        if (!_available) return;
        // Arrange
        var keys = Enumerable.Range(1, 20)
            .Select(i => IdempotencyKey.Create($"concurrent-{i}"))
            .ToList();

        // Pre-populate half the keys
        foreach (var key in keys.Take(10))
        {
            await _sut!.TryAcquireAsync(key, TimeSpan.FromMinutes(5));
        }

        // Act - Mix of reads and writes concurrently
        var tasks = new List<Task>();
        foreach (var key in keys)
        {
            tasks.Add(Task.Run(async () => await _sut!.ExistsAsync(key)));
            tasks.Add(Task.Run(async () => await _sut!.TryAcquireAsync(key, TimeSpan.FromMinutes(5))));
        }

        // Assert - Should not throw
        await FluentActions.Awaiting(async () => await Task.WhenAll(tasks))
            .Should().NotThrowAsync();
    }
}
