// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Entities;

namespace ProbotSharp.Domain.Specifications;

/// <summary>
/// Specification to determine if a WebhookDelivery was delivered recently.
/// Useful for filtering old webhook deliveries or implementing replay/retry logic.
/// </summary>
public sealed class RecentWebhookDeliverySpecification : Specification<WebhookDelivery>
{
    private readonly TimeSpan _recencyThreshold;

    /// <summary>
    /// Initializes a new instance with the specified recency threshold.
    /// </summary>
    /// <param name="recencyThreshold">The maximum age for a delivery to be considered recent</param>
    public RecentWebhookDeliverySpecification(TimeSpan recencyThreshold)
    {
        if (recencyThreshold <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(recencyThreshold), "Recency threshold must be positive.");
        }

        _recencyThreshold = recencyThreshold;
    }

    /// <summary>
    /// Creates a specification for deliveries within the last hour.
    /// </summary>
    public static RecentWebhookDeliverySpecification WithinLastHour()
        => new(TimeSpan.FromHours(1));

    /// <summary>
    /// Creates a specification for deliveries within the last 24 hours.
    /// </summary>
    public static RecentWebhookDeliverySpecification WithinLastDay()
        => new(TimeSpan.FromDays(1));

    /// <summary>
    /// Creates a specification for deliveries within the last 7 days.
    /// </summary>
    public static RecentWebhookDeliverySpecification WithinLastWeek()
        => new(TimeSpan.FromDays(7));

    public override bool IsSatisfiedBy(WebhookDelivery candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        var age = DateTimeOffset.UtcNow - candidate.DeliveredAt;
        return age <= _recencyThreshold;
    }
}
