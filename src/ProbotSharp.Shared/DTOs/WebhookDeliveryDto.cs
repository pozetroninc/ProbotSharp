namespace ProbotSharp.Shared.DTOs;

/// <summary>
/// Data transfer object representing a webhook delivery.
/// </summary>
public sealed class WebhookDeliveryDto
{
    /// <summary>
    /// The unique delivery identifier.
    /// </summary>
    public string DeliveryId { get; set; } = string.Empty;

    /// <summary>
    /// The name of the webhook event (e.g., "push", "pull_request").
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp when the webhook was delivered.
    /// </summary>
    public DateTimeOffset DeliveredAt { get; set; }

    /// <summary>
    /// The raw JSON payload of the webhook.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// The installation ID associated with this webhook, if applicable.
    /// </summary>
    public long? InstallationId { get; set; }
}
