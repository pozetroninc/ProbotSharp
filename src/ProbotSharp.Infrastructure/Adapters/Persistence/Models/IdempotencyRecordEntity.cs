// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Infrastructure.Adapters.Persistence.Models;

/// <summary>
/// Entity representing an idempotency record for preventing duplicate webhook processing.
/// </summary>
public sealed class IdempotencyRecordEntity
{
    /// <summary>
    /// Gets or sets the idempotency key (typically the webhook delivery ID).
    /// </summary>
    public required string IdempotencyKey { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this record was created (first processing attempt).
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the expiration timestamp for this idempotency record.
    /// Records can be safely deleted after this time.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets optional metadata about the request (e.g., event name, installation ID).
    /// </summary>
    public string? Metadata { get; set; }
}
