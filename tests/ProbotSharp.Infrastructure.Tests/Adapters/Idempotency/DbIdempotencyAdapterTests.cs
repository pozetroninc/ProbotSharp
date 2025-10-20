// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NSubstitute;

using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Infrastructure.Adapters.Idempotency;
using ProbotSharp.Infrastructure.Adapters.Persistence;
using ProbotSharp.Infrastructure.Adapters.Persistence.Models;

using Xunit;

namespace ProbotSharp.Infrastructure.Tests.Adapters.Idempotency;

/// <summary>
/// Unit tests for <see cref="DbIdempotencyAdapter"/>.
/// </summary>
public sealed class DbIdempotencyAdapterTests : IDisposable
{
    private readonly ProbotSharpDbContext _dbContext;
    private readonly ILogger<DbIdempotencyAdapter> _logger;
    private readonly DbIdempotencyAdapter _sut;

    public DbIdempotencyAdapterTests()
    {
        var options = new DbContextOptionsBuilder<ProbotSharpDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ProbotSharpDbContext(options);
        _logger = Substitute.For<ILogger<DbIdempotencyAdapter>>();
        _sut = new DbIdempotencyAdapter(_dbContext, _logger);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldReturnTrue_WhenKeyDoesNotExist()
    {
        // Arrange
        var key = IdempotencyKey.Create("test-delivery-id");

        // Act
        var result = await _sut.TryAcquireAsync(key);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldReturnFalse_WhenKeyAlreadyExists()
    {
        // Arrange
        var key = IdempotencyKey.Create("duplicate-delivery-id");
        await _sut.TryAcquireAsync(key);

        // Act
        var result = await _sut.TryAcquireAsync(key);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldStoreRecordWithExpiration()
    {
        // Arrange
        var key = IdempotencyKey.Create("expiring-delivery-id");
        var ttl = TimeSpan.FromHours(2);

        // Act
        await _sut.TryAcquireAsync(key, ttl);

        // Assert
        var record = await _dbContext.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.IdempotencyKey == key.Value);

        record.Should().NotBeNull();
        record!.ExpiresAt.Should().BeCloseTo(
            DateTimeOffset.UtcNow.Add(ttl),
            precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldUseDefaultTtl_WhenNotSpecified()
    {
        // Arrange
        var key = IdempotencyKey.Create("default-ttl-delivery-id");

        // Act
        await _sut.TryAcquireAsync(key);

        // Assert
        var record = await _dbContext.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.IdempotencyKey == key.Value);

        record.Should().NotBeNull();
        record!.ExpiresAt.Should().BeCloseTo(
            DateTimeOffset.UtcNow.AddHours(24),
            precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldThrowArgumentNullException_WhenKeyIsNull()
    {
        // Act
        var act = () => _sut.TryAcquireAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenKeyExists()
    {
        // Arrange
        var key = IdempotencyKey.Create("existing-delivery-id");
        await _sut.TryAcquireAsync(key);

        // Act
        var exists = await _sut.ExistsAsync(key);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenKeyDoesNotExist()
    {
        // Arrange
        var key = IdempotencyKey.Create("non-existing-delivery-id");

        // Act
        var exists = await _sut.ExistsAsync(key);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ShouldThrowArgumentNullException_WhenKeyIsNull()
    {
        // Act
        var act = () => _sut.ExistsAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReleaseAsync_ShouldRemoveRecord_WhenKeyExists()
    {
        // Arrange
        var key = IdempotencyKey.Create("release-delivery-id");
        await _sut.TryAcquireAsync(key);

        // Act
        await _sut.ReleaseAsync(key);

        // Assert
        var exists = await _sut.ExistsAsync(key);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ReleaseAsync_ShouldNotThrow_WhenKeyDoesNotExist()
    {
        // Arrange
        var key = IdempotencyKey.Create("non-existing-release-id");

        // Act
        var act = () => _sut.ReleaseAsync(key);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ReleaseAsync_ShouldThrowArgumentNullException_WhenKeyIsNull()
    {
        // Act
        var act = () => _sut.ReleaseAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CleanupExpiredAsync_ShouldRemoveExpiredRecords()
    {
        // Arrange
        var key1 = IdempotencyKey.Create("expired-1");
        var key2 = IdempotencyKey.Create("expired-2");
        var key3 = IdempotencyKey.Create("not-expired");

        // Insert expired records directly
        _dbContext.IdempotencyRecords.Add(new IdempotencyRecordEntity
        {
            IdempotencyKey = key1.Value,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-25),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1),
        });
        _dbContext.IdempotencyRecords.Add(new IdempotencyRecordEntity
        {
            IdempotencyKey = key2.Value,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-26),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-30),
        });
        await _dbContext.SaveChangesAsync();

        // Insert a non-expired record
        await _sut.TryAcquireAsync(key3, TimeSpan.FromHours(24));

        // Act
        var removedCount = await _sut.CleanupExpiredAsync();

        // Assert
        removedCount.Should().Be(2);
        (await _sut.ExistsAsync(key1)).Should().BeFalse();
        (await _sut.ExistsAsync(key2)).Should().BeFalse();
        (await _sut.ExistsAsync(key3)).Should().BeTrue();
    }

    [Fact]
    public async Task CleanupExpiredAsync_ShouldReturnZero_WhenNoExpiredRecords()
    {
        // Arrange
        var key = IdempotencyKey.Create("not-expired-2");
        await _sut.TryAcquireAsync(key, TimeSpan.FromHours(24));

        // Act
        var removedCount = await _sut.CleanupExpiredAsync();

        // Assert
        removedCount.Should().Be(0);
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldHandleRaceCondition()
    {
        // Arrange
        var key = IdempotencyKey.Create("race-condition-id");

        // Act - Simulate concurrent attempts
        var task1 = _sut.TryAcquireAsync(key);
        var task2 = _sut.TryAcquireAsync(key);

        var results = await Task.WhenAll(task1, task2);

        // Assert - Only one should succeed
        results.Count(r => r).Should().Be(1);
        results.Count(r => !r).Should().Be(1);
    }
}
