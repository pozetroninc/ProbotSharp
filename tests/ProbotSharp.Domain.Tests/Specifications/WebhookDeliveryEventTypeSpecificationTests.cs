// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.Specifications;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Tests.Specifications;

public class WebhookDeliveryEventTypeSpecificationTests
{
    [Fact]
    public void Constructor_WithNullEventName_ShouldThrow()
    {
        var act = () => new WebhookDeliveryEventTypeSpecification(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsSatisfiedBy_WithMatchingEventType_ShouldReturnTrue()
    {
        var eventName = WebhookEventName.Create("push");
        var result = WebhookDelivery.Create(
            DeliveryId.Create(Guid.NewGuid().ToString()),
            eventName,
            DateTimeOffset.UtcNow,
            WebhookPayload.Create("{}"),
            null);
        var delivery = result.Value!;
        var spec = new WebhookDeliveryEventTypeSpecification(eventName);

        spec.IsSatisfiedBy(delivery).Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_WithDifferentEventType_ShouldReturnFalse()
    {
        var result = WebhookDelivery.Create(
            DeliveryId.Create(Guid.NewGuid().ToString()),
            WebhookEventName.Create("push"),
            DateTimeOffset.UtcNow,
            WebhookPayload.Create("{}"),
            null);
        var delivery = result.Value!;
        var spec = new WebhookDeliveryEventTypeSpecification(WebhookEventName.Create("pull_request"));

        spec.IsSatisfiedBy(delivery).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_WithNullCandidate_ShouldThrow()
    {
        var spec = new WebhookDeliveryEventTypeSpecification(WebhookEventName.Create("push"));

        var act = () => spec.IsSatisfiedBy(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
