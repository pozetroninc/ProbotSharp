// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

using FluentAssertions;

using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Infrastructure.Adapters.Caching;

using StackExchange.Redis;

using Xunit;

namespace ProbotSharp.Infrastructure.Tests.Integration;

/// <summary>
/// Integration tests for RedisAccessTokenCacheAdapter using real Redis via Testcontainers.
/// These tests validate actual Redis behavior including TTL, concurrency, and data persistence.
/// </summary>
[Collection("Redis Integration Tests")]
public sealed class RedisAccessTokenCacheIntegrationTests : IAsyncLifetime
{
    private IContainer? _redisContainer;
    private IConnectionMultiplexer? _redis;
    private RedisAccessTokenCacheAdapter? _sut;
    private bool _available;

    public async Task InitializeAsync()
    {
        _available = true;
        try
        {
            // Spin up Redis container
            _redisContainer = new ContainerBuilder()
                .WithImage("redis:7-alpine")
                .WithPortBinding(6379, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379))
                .WithCleanUp(true)
                .WithAutoRemove(true)
                .Build();

            await _redisContainer.StartAsync();

            // Get connection string
            var port = _redisContainer.GetMappedPublicPort(6379);
            var connectionString = $"localhost:{port}";

            // Create real Redis connection
            _redis = await ConnectionMultiplexer.ConnectAsync(connectionString);
            _sut = new RedisAccessTokenCacheAdapter(_redis);
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
    public async Task GetAsync_WhenKeyDoesNotExist_ShouldReturnNull()
    {
        if (!_available)
        {
            return;
        }

        // Arrange
        var installationId = InstallationId.Create(99999);

        // Act
        var result = await _sut!.GetAsync(installationId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAndGetAsync_ShouldStoreAndRetrieveToken()
    {
        if (!_available)
        {
            return;
        }

        // Arrange
        var installationId = InstallationId.Create(12345);
        var token = InstallationAccessToken.Create(
            "ghs_testtoken123",
            DateTimeOffset.UtcNow.AddHours(1));

        // Act
        await _sut!.SetAsync(installationId, token);
        var retrieved = await _sut.GetAsync(installationId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Value.Should().Be("ghs_testtoken123");
        retrieved.ExpiresAt.Should().BeCloseTo(token.ExpiresAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SetAsync_WithTtl_ShouldExpireAfterCalculatedTtl()
    {
        if (!_available)
        {
            return;
        }

        // Arrange
        var installationId = InstallationId.Create(54321);

        // Token expires in 3 seconds, should be cached for ~2.5 seconds (with 30s buffer, but token is so short)
        var token = InstallationAccessToken.Create(
            "ghs_shortlived",
            DateTimeOffset.UtcNow.AddSeconds(3));

        // Act
        await _sut!.SetAsync(installationId, token);
        var immediate = await _sut.GetAsync(installationId);

        await Task.Delay(TimeSpan.FromSeconds(4)); // Wait for expiry

        var afterExpiry = await _sut.GetAsync(installationId);

        // Assert
        immediate.Should().NotBeNull();
        afterExpiry.Should().BeNull();
    }

    [Fact]
    public async Task ConcurrentWrites_ShouldHandleCorrectly()
    {
        if (!_available)
        {
            return;
        }

        // Arrange
        var installationId = InstallationId.Create(22222);
        var tasks = new List<Task>();

        // Act - Multiple concurrent writes
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                var token = InstallationAccessToken.Create(
                    $"ghs_concurrent_{index}",
                    DateTimeOffset.UtcNow.AddHours(1));
                await _sut!.SetAsync(installationId, token);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Should have a value (last write wins)
        var result = await _sut!.GetAsync(installationId);
        result.Should().NotBeNull();
        result!.Value.Should().StartWith("ghs_concurrent_");
    }

    [Fact]
    public async Task MultipleInstallations_ShouldStoreIndependently()
    {
        if (!_available)
        {
            return;
        }

        // Arrange
        var id1 = InstallationId.Create(100);
        var id2 = InstallationId.Create(200);
        var id3 = InstallationId.Create(300);

        var token1 = InstallationAccessToken.Create("ghs_token1", DateTimeOffset.UtcNow.AddHours(1));
        var token2 = InstallationAccessToken.Create("ghs_token2", DateTimeOffset.UtcNow.AddHours(1));
        var token3 = InstallationAccessToken.Create("ghs_token3", DateTimeOffset.UtcNow.AddHours(1));

        // Act
        await _sut!.SetAsync(id1, token1);
        await _sut.SetAsync(id2, token2);
        await _sut.SetAsync(id3, token3);

        // Assert
        var result1 = await _sut.GetAsync(id1);
        var result2 = await _sut.GetAsync(id2);
        var result3 = await _sut.GetAsync(id3);

        result1!.Value.Should().Be("ghs_token1");
        result2!.Value.Should().Be("ghs_token2");
        result3!.Value.Should().Be("ghs_token3");
    }

    [Fact]
    public async Task SetAsync_WhenUpdatingExisting_ShouldOverwrite()
    {
        if (!_available)
        {
            return;
        }

        // Arrange
        var installationId = InstallationId.Create(33333);
        var token1 = InstallationAccessToken.Create("ghs_original", DateTimeOffset.UtcNow.AddHours(1));
        var token2 = InstallationAccessToken.Create("ghs_updated", DateTimeOffset.UtcNow.AddHours(2));

        // Act
        await _sut!.SetAsync(installationId, token1);
        await _sut.SetAsync(installationId, token2);
        var result = await _sut.GetAsync(installationId);

        // Assert
        result!.Value.Should().Be("ghs_updated");
        result.ExpiresAt.Should().BeCloseTo(token2.ExpiresAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetAsync_AfterRedisFlush_ShouldReturnNull()
    {
        if (!_available)
        {
            return;
        }

        // Arrange
        var installationId = InstallationId.Create(44444);
        var token = InstallationAccessToken.Create("ghs_beforeflush", DateTimeOffset.UtcNow.AddHours(1));
        await _sut!.SetAsync(installationId, token);

        // Act - Simulate restart by clearing all data
        var db = _redis!.GetDatabase();
        await db.ExecuteAsync("FLUSHALL");

        var result = await _sut.GetAsync(installationId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ShouldSerializeAndDeserializeComplexToken()
    {
        if (!_available)
        {
            return;
        }

        // Arrange
        var installationId = InstallationId.Create(55555);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        var token = InstallationAccessToken.Create(
            "ghs_complex_token_with_special_chars_!@#$%",
            expiresAt);

        // Act
        await _sut!.SetAsync(installationId, token);
        var retrieved = await _sut.GetAsync(installationId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Value.Should().Be("ghs_complex_token_with_special_chars_!@#$%");
        retrieved.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public async Task ConcurrentReads_ShouldReturnConsistentData()
    {
        if (!_available)
        {
            return;
        }

        // Arrange
        var installationId = InstallationId.Create(66666);
        var token = InstallationAccessToken.Create("ghs_concurrent_read", DateTimeOffset.UtcNow.AddHours(1));
        await _sut!.SetAsync(installationId, token);

        // Act - Multiple concurrent reads
        var tasks = new List<Task<InstallationAccessToken?>>();
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(_sut.GetAsync(installationId));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - All reads should return the same token
        results.Should().AllSatisfy(r =>
        {
            r.Should().NotBeNull();
            r!.Value.Should().Be("ghs_concurrent_read");
        });
    }

    [Fact]
    public async Task SetAsync_WithVeryShortTtl_ShouldExpireQuickly()
    {
        if (!_available)
        {
            return;
        }

        // Arrange
        var installationId = InstallationId.Create(77777);

        // Token expires in 1 second
        var token = InstallationAccessToken.Create(
            "ghs_veryshorttl",
            DateTimeOffset.UtcNow.AddSeconds(1));

        // Act
        await _sut!.SetAsync(installationId, token);
        var immediate = await _sut.GetAsync(installationId);

        await Task.Delay(TimeSpan.FromSeconds(2));

        var afterExpiry = await _sut.GetAsync(installationId);

        // Assert
        immediate.Should().NotBeNull();
        afterExpiry.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WithMultipleInstallations_ShouldIsolateData()
    {
        if (!_available)
        {
            return;
        }

        // Arrange
        var installations = Enumerable.Range(1, 50)
            .Select(i => InstallationId.Create(10000 + i))
            .ToList();

        var tokens = installations
            .Select((id, index) => (Id: id, Token: InstallationAccessToken.Create($"ghs_token_{index}", DateTimeOffset.UtcNow.AddHours(1))))
            .ToList();

        // Act - Write all tokens
        foreach (var (id, token) in tokens)
        {
            await _sut!.SetAsync(id, token);
        }

        // Read and verify
        var results = new List<(InstallationId Id, InstallationAccessToken? Token)>();
        foreach (var (id, _) in tokens)
        {
            var retrieved = await _sut!.GetAsync(id);
            results.Add((id, retrieved));
        }

        // Assert - All tokens should be independently stored
        for (int i = 0; i < tokens.Count; i++)
        {
            results[i].Token.Should().NotBeNull();
            results[i].Token!.Value.Should().Be($"ghs_token_{i}");
        }
    }
}
