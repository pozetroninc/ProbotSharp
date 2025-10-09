// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Infrastructure.Adapters.Idempotency;

namespace ProbotSharp.Infrastructure.Tests.Adapters.Idempotency;

/// <summary>
/// Tests for <see cref="InMemoryIdempotencyAdapter"/> covering in-memory idempotency key tracking.
/// </summary>
public sealed class InMemoryIdempotencyAdapterTests : IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<InMemoryIdempotencyAdapter> _logger;
    private readonly InMemoryIdempotencyAdapter _adapter;

    public InMemoryIdempotencyAdapterTests()
    {
        this._memoryCache = new MemoryCache(new MemoryCacheOptions());
        this._logger = Substitute.For<ILogger<InMemoryIdempotencyAdapter>>();
        this._adapter = new InMemoryIdempotencyAdapter(this._memoryCache, this._logger);
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldReturnTrue_WhenKeyDoesNotExist()
    {
        // Arrange
        var key = IdempotencyKey.Create("test-key");

        // Act
        var result = await this._adapter.TryAcquireAsync(key);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldReturnFalse_WhenKeyAlreadyExists()
    {
        // Arrange
        var key = IdempotencyKey.Create("existing-key");
        await this._adapter.TryAcquireAsync(key);

        // Act
        var result = await this._adapter.TryAcquireAsync(key);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldUseCustomTtl_WhenProvided()
    {
        // Arrange
        var key = IdempotencyKey.Create("test-key");
        var customTtl = TimeSpan.FromMilliseconds(100);

        // Act
        var acquired = await this._adapter.TryAcquireAsync(key, customTtl);
        acquired.Should().BeTrue();

        // Wait for expiration
        await Task.Delay(150);

        // Assert - should be able to acquire again after expiration
        var result = await this._adapter.TryAcquireAsync(key);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TryAcquireAsync_Concurrent_ShouldBeThreadSafe()
    {
        // Arrange
        var key = IdempotencyKey.Create("concurrent-key");
        var successCount = 0;

        // Act - multiple concurrent attempts to acquire the same key
        var tasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            var result = await this._adapter.TryAcquireAsync(key);
            if (result)
            {
                Interlocked.Increment(ref successCount);
            }

            return result;
        });

        var results = await Task.WhenAll(tasks);

        // Assert - exactly one should succeed
        successCount.Should().Be(1);
        results.Count(r => r).Should().Be(1);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenKeyExists()
    {
        // Arrange
        var key = IdempotencyKey.Create("test-key");
        await this._adapter.TryAcquireAsync(key);

        // Act
        var result = await this._adapter.ExistsAsync(key);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenKeyDoesNotExist()
    {
        // Arrange
        var key = IdempotencyKey.Create("nonexistent-key");

        // Act
        var result = await this._adapter.ExistsAsync(key);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_AfterExpiration()
    {
        // Arrange
        var key = IdempotencyKey.Create("expiring-key");
        var shortTtl = TimeSpan.FromMilliseconds(100);
        await this._adapter.TryAcquireAsync(key, shortTtl);

        // Act - check before expiration
        var existsBefore = await this._adapter.ExistsAsync(key);

        // Wait for expiration
        await Task.Delay(150);

        // Act - check after expiration
        var existsAfter = await this._adapter.ExistsAsync(key);

        // Assert
        existsBefore.Should().BeTrue();
        existsAfter.Should().BeFalse();
    }

    [Fact]
    public async Task ReleaseAsync_ShouldRemoveKey()
    {
        // Arrange
        var key = IdempotencyKey.Create("test-key");
        await this._adapter.TryAcquireAsync(key);

        // Act
        await this._adapter.ReleaseAsync(key);

        // Assert
        var exists = await this._adapter.ExistsAsync(key);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ReleaseAsync_ThenAcquire_ShouldSucceed()
    {
        // Arrange
        var key = IdempotencyKey.Create("test-key");
        await this._adapter.TryAcquireAsync(key);
        await this._adapter.ReleaseAsync(key);

        // Act
        var result = await this._adapter.TryAcquireAsync(key);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ReleaseAsync_WithNonExistentKey_ShouldNotThrow()
    {
        // Arrange
        var key = IdempotencyKey.Create("nonexistent-key");

        // Act
        var act = async () => await this._adapter.ReleaseAsync(key);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CleanupExpiredAsync_ShouldReturnZero()
    {
        // Arrange & Act
        var result = await this._adapter.CleanupExpiredAsync();

        // Assert - IMemoryCache handles cleanup automatically
        result.Should().Be(0);
    }

    [Fact]
    public async Task TryAcquireAsync_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await this._adapter.TryAcquireAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExistsAsync_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await this._adapter.ExistsAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReleaseAsync_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await this._adapter.ReleaseAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task TryAcquireAsync_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var key = IdempotencyKey.Create("test-key");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await this._adapter.TryAcquireAsync(key, cancellationToken: cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task MultipleKeys_ShouldBeIndependent()
    {
        // Arrange
        var key1 = IdempotencyKey.Create("key-1");
        var key2 = IdempotencyKey.Create("key-2");

        // Act
        var acquired1 = await this._adapter.TryAcquireAsync(key1);
        var acquired2 = await this._adapter.TryAcquireAsync(key2);

        // Assert
        acquired1.Should().BeTrue();
        acquired2.Should().BeTrue();

        var exists1 = await this._adapter.ExistsAsync(key1);
        var exists2 = await this._adapter.ExistsAsync(key2);
        exists1.Should().BeTrue();
        exists2.Should().BeTrue();
    }

    [Fact]
    public async Task Dispose_ShouldCleanupLocks()
    {
        // Arrange
        var key = IdempotencyKey.Create("test-key");
        await this._adapter.TryAcquireAsync(key);

        // Act
        this._adapter.Dispose();

        // Assert - should not throw
        var act = () => this._adapter.Dispose();
        act.Should().NotThrow();
    }

    public void Dispose()
    {
        this._adapter.Dispose();
        this._memoryCache.Dispose();
    }
}
