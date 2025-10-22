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
        var result = WebhookDelivery.Create(
            DeliveryId.Create("abc"),
            WebhookEventName.Create("push"),
            DateTimeOffset.UtcNow,
            WebhookPayload.Create("{\"foo\":true}"),
            InstallationId.Create(1));

        result.IsSuccess.Should().BeTrue();
        var delivery = result.Value!;
        delivery.EventName.Value.Should().Be("push");
        delivery.Payload.RawBody.Should().Contain("foo");
        delivery.InstallationId!.Value.Should().Be(1);
    }

    [Fact]
    public void Create_WithDefaultDeliveredAt_ShouldReturnFailure()
    {
        var result = WebhookDelivery.Create(
            DeliveryId.Create("abc"),
            WebhookEventName.Create("push"),
            default,
            WebhookPayload.Create("{\"foo\":true}"),
            null);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("webhook_delivery.invalid_delivered_at");
        result.Error!.Value.Message.Should().Be("DeliveredAt must be set.");
    }
}
