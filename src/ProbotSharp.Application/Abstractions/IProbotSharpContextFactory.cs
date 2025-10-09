// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Context;
using ProbotSharp.Domain.Entities;

namespace ProbotSharp.Application.Abstractions;

/// <summary>
/// Factory for creating ProbotSharpContext instances from webhook deliveries.
/// </summary>
public interface IProbotSharpContextFactory
{
    /// <summary>
    /// Creates a ProbotSharpContext from a webhook delivery.
    /// </summary>
    /// <param name="delivery">The webhook delivery to create context from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that resolves to a ProbotSharpContext.</returns>
    Task<ProbotSharpContext> CreateAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default);
}
