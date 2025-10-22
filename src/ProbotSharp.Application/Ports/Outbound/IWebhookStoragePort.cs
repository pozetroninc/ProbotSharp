// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Domain.Abstractions;

namespace ProbotSharp.Application.Ports.Outbound;

/// <summary>
/// Port for storing and retrieving webhook deliveries.
/// </summary>
public interface IWebhookStoragePort
{
    /// <summary>
    /// Saves a webhook delivery to storage.
    /// </summary>
    /// <param name="delivery">The webhook delivery to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> SaveAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a webhook delivery by ID.
    /// </summary>
    /// <param name="deliveryId">The delivery ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The webhook delivery if found; otherwise, null.</returns>
    Task<Result<WebhookDelivery?>> GetAsync(DeliveryId deliveryId, CancellationToken cancellationToken = default);
}
