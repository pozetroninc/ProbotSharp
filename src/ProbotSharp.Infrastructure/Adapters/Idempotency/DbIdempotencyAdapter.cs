// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Infrastructure.Adapters.Persistence;
using ProbotSharp.Infrastructure.Adapters.Persistence.Models;

namespace ProbotSharp.Infrastructure.Adapters.Idempotency;

/// <summary>
/// Database-backed implementation of idempotency storage using Entity Framework Core.
/// Provides thread-safe idempotency key tracking with automatic expiration.
/// </summary>
public sealed partial class DbIdempotencyAdapter : IIdempotencyPort
{
    private readonly ProbotSharpDbContext _dbContext;
    private readonly ILogger<DbIdempotencyAdapter> _logger;
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);

    /// <summary>
    /// Initializes a new instance of the <see cref="DbIdempotencyAdapter"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="logger">The logger instance.</param>
    public DbIdempotencyAdapter(
        ProbotSharpDbContext dbContext,
        ILogger<DbIdempotencyAdapter> logger)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(logger);

        this._dbContext = dbContext;
        this._logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> TryAcquireAsync(
        IdempotencyKey key,
        TimeSpan? timeToLive = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        var ttl = timeToLive ?? DefaultTtl;
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.Add(ttl);

        try
        {
            // Check if the key already exists
            var exists = await this._dbContext.IdempotencyRecords
                .AnyAsync(r => r.IdempotencyKey == key.Value, cancellationToken)
                .ConfigureAwait(false);

            if (exists)
            {
                LogMessages.LogIdempotencyKeyAlreadyExists(this._logger, key.Value);
                return false;
            }

            // Try to insert the record
            var record = new IdempotencyRecordEntity
            {
                IdempotencyKey = key.Value,
                CreatedAt = now,
                ExpiresAt = expiresAt,
            };

            this._dbContext.IdempotencyRecords.Add(record);
            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            LogMessages.LogIdempotencyKeyAcquired(this._logger, key.Value, expiresAt);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Race condition: another process inserted the same key
            LogMessages.LogIdempotencyKeyRaceCondition(this._logger, key.Value, ex);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(
        IdempotencyKey key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        var exists = await this._dbContext.IdempotencyRecords
            .AnyAsync(r => r.IdempotencyKey == key.Value, cancellationToken)
            .ConfigureAwait(false);

        return exists;
    }

    /// <inheritdoc />
    public async Task ReleaseAsync(
        IdempotencyKey key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        var record = await this._dbContext.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.IdempotencyKey == key.Value, cancellationToken)
            .ConfigureAwait(false);

        if (record is not null)
        {
            this._dbContext.IdempotencyRecords.Remove(record);
            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            LogMessages.LogIdempotencyKeyReleased(this._logger, key.Value);
        }
    }

    /// <inheritdoc />
    public async Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var expiredRecords = await this._dbContext.IdempotencyRecords
            .Where(r => r.ExpiresAt < now)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (expiredRecords.Count > 0)
        {
            this._dbContext.IdempotencyRecords.RemoveRange(expiredRecords);
            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            LogMessages.LogIdempotencyCleanupCompleted(this._logger, expiredRecords.Count);
        }

        return expiredRecords.Count;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        // Check for unique constraint violation in the exception message
        // Different database providers have different error codes/messages
        var innerException = exception.InnerException;
        if (innerException is null)
        {
            return false;
        }

        var message = innerException.Message;
        return message.Contains("unique", StringComparison.OrdinalIgnoreCase)
            || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
            || message.Contains("constraint", StringComparison.OrdinalIgnoreCase)
            || message.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase);
    }
}
