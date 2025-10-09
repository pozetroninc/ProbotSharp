// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Tests.Entities;

public class WebhookDeliveryTests
{
    [Fact]
    public void Create_WithValidArguments_ShouldSetProperties()
    {
        var delivery = WebhookDelivery.Create(
            DeliveryId.Create("abc"),
            WebhookEventName.Create("push"),
            DateTimeOffset.UtcNow,
            WebhookPayload.Create("{\"foo\":true}"),
            InstallationId.Create(1));

        delivery.EventName.Value.Should().Be("push");
        delivery.Payload.RawBody.Should().Contain("foo");
        delivery.InstallationId!.Value.Should().Be(1);
    }

    [Fact]
    public void Create_WithDefaultDeliveredAt_ShouldThrow()
    {
        var act = () => WebhookDelivery.Create(
            DeliveryId.Create("abc"),
            WebhookEventName.Create("push"),
            default,
            WebhookPayload.Create("{\"foo\":true}"),
            null);

        act.Should().Throw<ArgumentException>();
    }
}
