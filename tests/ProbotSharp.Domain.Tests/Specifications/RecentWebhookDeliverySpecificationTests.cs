// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.Specifications;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Tests.Specifications;

public class RecentWebhookDeliverySpecificationTests
{
    [Fact]
    public void Constructor_WithZeroThreshold_ShouldThrow()
    {
        var act = () => new RecentWebhookDeliverySpecification(TimeSpan.Zero);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithNegativeThreshold_ShouldThrow()
    {
        var act = () => new RecentWebhookDeliverySpecification(TimeSpan.FromHours(-1));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void IsSatisfiedBy_WithRecentDelivery_ShouldReturnTrue()
    {
        var delivery = WebhookDelivery.Create(
            DeliveryId.Create(Guid.NewGuid().ToString()),
            WebhookEventName.Create("push"),
            DateTimeOffset.UtcNow.AddMinutes(-5),
            WebhookPayload.Create("{}"),
            null);
        var spec = new RecentWebhookDeliverySpecification(TimeSpan.FromHours(1));

        spec.IsSatisfiedBy(delivery).Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_WithOldDelivery_ShouldReturnFalse()
    {
        var delivery = WebhookDelivery.Create(
            DeliveryId.Create(Guid.NewGuid().ToString()),
            WebhookEventName.Create("push"),
            DateTimeOffset.UtcNow.AddHours(-2),
            WebhookPayload.Create("{}"),
            null);
        var spec = new RecentWebhookDeliverySpecification(TimeSpan.FromHours(1));

        spec.IsSatisfiedBy(delivery).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_WithDeliveryAtBoundary_ShouldReturnTrue()
    {
        var threshold = TimeSpan.FromHours(1);
        var delivery = WebhookDelivery.Create(
            DeliveryId.Create(Guid.NewGuid().ToString()),
            WebhookEventName.Create("push"),
            DateTimeOffset.UtcNow.Subtract(threshold).AddMilliseconds(10),
            WebhookPayload.Create("{}"),
            null);
        var spec = new RecentWebhookDeliverySpecification(threshold);

        spec.IsSatisfiedBy(delivery).Should().BeTrue();
    }

    [Fact]
    public void WithinLastHour_ShouldCreateSpecWithOneHourThreshold()
    {
        var spec = RecentWebhookDeliverySpecification.WithinLastHour();
        var delivery = WebhookDelivery.Create(
            DeliveryId.Create(Guid.NewGuid().ToString()),
            WebhookEventName.Create("push"),
            DateTimeOffset.UtcNow.AddMinutes(-30),
            WebhookPayload.Create("{}"),
            null);

        spec.IsSatisfiedBy(delivery).Should().BeTrue();
    }

    [Fact]
    public void WithinLastDay_ShouldCreateSpecWith24HourThreshold()
    {
        var spec = RecentWebhookDeliverySpecification.WithinLastDay();
        var delivery = WebhookDelivery.Create(
            DeliveryId.Create(Guid.NewGuid().ToString()),
            WebhookEventName.Create("push"),
            DateTimeOffset.UtcNow.AddHours(-12),
            WebhookPayload.Create("{}"),
            null);

        spec.IsSatisfiedBy(delivery).Should().BeTrue();
    }

    [Fact]
    public void WithinLastWeek_ShouldCreateSpecWith7DayThreshold()
    {
        var spec = RecentWebhookDeliverySpecification.WithinLastWeek();
        var delivery = WebhookDelivery.Create(
            DeliveryId.Create(Guid.NewGuid().ToString()),
            WebhookEventName.Create("push"),
            DateTimeOffset.UtcNow.AddDays(-3),
            WebhookPayload.Create("{}"),
            null);

        spec.IsSatisfiedBy(delivery).Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_WithNullCandidate_ShouldThrow()
    {
        var spec = new RecentWebhookDeliverySpecification(TimeSpan.FromHours(1));

        var act = () => spec.IsSatisfiedBy(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
