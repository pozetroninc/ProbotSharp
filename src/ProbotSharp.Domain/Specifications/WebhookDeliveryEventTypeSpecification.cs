// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Specifications;

/// <summary>
/// Specification to determine if a WebhookDelivery matches a specific event type.
/// Useful for filtering webhook deliveries by event name (e.g., "push", "pull_request", "issues").
/// </summary>
public sealed class WebhookDeliveryEventTypeSpecification : Specification<WebhookDelivery>
{
    private readonly WebhookEventName _eventName;

    /// <summary>
    /// Initializes a new instance for the specified event type.
    /// </summary>
    /// <param name="eventName">The event name to match</param>
    public WebhookDeliveryEventTypeSpecification(WebhookEventName eventName)
    {
        _eventName = eventName ?? throw new ArgumentNullException(nameof(eventName));
    }

    public override bool IsSatisfiedBy(WebhookDelivery candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return candidate.EventName.Equals(_eventName);
    }
}
