// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Infrastructure.Adapters.Persistence.Models;

/// <summary>
/// Entity used by the infrastructure layer to persist dead-letter queue items.
/// </summary>
public sealed class DeadLetterQueueItemEntity
{
    /// <summary>Gets or sets the unique identifier for this dead-letter item.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the webhook delivery identifier.</summary>
    public string DeliveryId { get; set; } = string.Empty;

    /// <summary>Gets or sets the GitHub event name.</summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>Gets or sets the webhook payload.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional installation identifier.</summary>
    public long? InstallationId { get; set; }

    /// <summary>Gets or sets the webhook signature.</summary>
    public string Signature { get; set; } = string.Empty;

    /// <summary>Gets or sets the raw payload.</summary>
    public string RawPayload { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of retry attempts made.</summary>
    public int Attempt { get; set; }

    /// <summary>Gets or sets the reason for moving to dead-letter queue.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the item failed.</summary>
    public DateTimeOffset FailedAt { get; set; }

    /// <summary>Gets or sets the last error message.</summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Converts the entity to a domain dead-letter item.
    /// </summary>
    internal DeadLetterItem ToDomain()
    {
        var processCommand = new ProcessWebhookCommand(
            DeliveryId: Domain.ValueObjects.DeliveryId.Create(this.DeliveryId),
            EventName: WebhookEventName.Create(this.EventName),
            Payload: WebhookPayload.Create(this.RawPayload),
            InstallationId: this.InstallationId.HasValue ? Domain.ValueObjects.InstallationId.Create(this.InstallationId.Value) : null,
            Signature: WebhookSignature.Create(this.Signature),
            RawPayload: this.RawPayload);

        var enqueueCommand = new EnqueueReplayCommand(processCommand, this.Attempt);

        return new DeadLetterItem(
            this.Id,
            enqueueCommand,
            this.Reason,
            this.FailedAt,
            this.LastError);
    }

    /// <summary>
    /// Creates an entity from a domain dead-letter item.
    /// </summary>
    internal static DeadLetterQueueItemEntity FromDomain(DeadLetterItem item)
    {
        return new DeadLetterQueueItemEntity
        {
            Id = item.Id,
            DeliveryId = item.Command.Command.DeliveryId.Value,
            EventName = item.Command.Command.EventName.Value,
            Payload = item.Command.Command.Payload.RawBody,
            InstallationId = item.Command.Command.InstallationId?.Value,
            Signature = item.Command.Command.Signature.Value,
            RawPayload = item.Command.Command.RawPayload,
            Attempt = item.Command.Attempt,
            Reason = item.Reason,
            FailedAt = item.FailedAt,
            LastError = item.LastError,
        };
    }
}
