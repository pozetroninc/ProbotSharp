// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Ports.Outbound;

public interface IWebhookStoragePort
{
    Task<Result> SaveAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default);
    Task<Result<WebhookDelivery?>> GetAsync(DeliveryId deliveryId, CancellationToken cancellationToken = default);
}

