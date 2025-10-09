// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Events;

namespace ProbotSharp.Domain.Tests.Events;

/// <summary>
/// Tests for domain events.
/// Validates construction and record equality for all domain event types.
/// </summary>
public class DomainEventTests
{
    [Fact]
    public void ManifestCreatedDomainEvent_ShouldCreateWithProperties()
    {
        // Arrange
        var manifestJson = "{\"name\":\"test-app\"}";
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var domainEvent = new ManifestCreatedDomainEvent(manifestJson, createdAt);

        // Assert
        domainEvent.ManifestJson.Should().Be(manifestJson);
        domainEvent.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void ManifestCreatedDomainEvent_RecordEquality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var manifestJson = "{\"name\":\"test-app\"}";
        var createdAt = DateTimeOffset.UtcNow;
        var event1 = new ManifestCreatedDomainEvent(manifestJson, createdAt);
        var event2 = new ManifestCreatedDomainEvent(manifestJson, createdAt);

        // Act & Assert
        event1.Should().Be(event2);
        (event1 == event2).Should().BeTrue();
    }

    [Fact]
    public void ManifestCreatedDomainEvent_RecordEquality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var manifestJson1 = "{\"name\":\"test-app-1\"}";
        var manifestJson2 = "{\"name\":\"test-app-2\"}";
        var createdAt = DateTimeOffset.UtcNow;
        var event1 = new ManifestCreatedDomainEvent(manifestJson1, createdAt);
        var event2 = new ManifestCreatedDomainEvent(manifestJson2, createdAt);

        // Act & Assert
        event1.Should().NotBe(event2);
        (event1 != event2).Should().BeTrue();
    }

    [Fact]
    public void WebhookHandledDomainEvent_ShouldCreateWithProperties()
    {
        // Arrange
        var deliveryId = DeliveryId.Create("delivery-123");
        var eventName = WebhookEventName.Create("push");
        var handledAt = DateTimeOffset.UtcNow;

        // Act
        var domainEvent = new WebhookHandledDomainEvent(deliveryId, eventName, handledAt);

        // Assert
        domainEvent.DeliveryId.Should().Be(deliveryId);
        domainEvent.EventName.Should().Be(eventName);
        domainEvent.HandledAt.Should().Be(handledAt);
    }

    [Fact]
    public void WebhookHandledDomainEvent_RecordEquality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var deliveryId = DeliveryId.Create("delivery-123");
        var eventName = WebhookEventName.Create("push");
        var handledAt = DateTimeOffset.UtcNow;
        var event1 = new WebhookHandledDomainEvent(deliveryId, eventName, handledAt);
        var event2 = new WebhookHandledDomainEvent(deliveryId, eventName, handledAt);

        // Act & Assert
        event1.Should().Be(event2);
        (event1 == event2).Should().BeTrue();
    }

    [Fact]
    public void WebhookHandledDomainEvent_RecordEquality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var deliveryId1 = DeliveryId.Create("delivery-123");
        var deliveryId2 = DeliveryId.Create("delivery-456");
        var eventName = WebhookEventName.Create("push");
        var handledAt = DateTimeOffset.UtcNow;
        var event1 = new WebhookHandledDomainEvent(deliveryId1, eventName, handledAt);
        var event2 = new WebhookHandledDomainEvent(deliveryId2, eventName, handledAt);

        // Act & Assert
        event1.Should().NotBe(event2);
        (event1 != event2).Should().BeTrue();
    }

    [Fact]
    public void WebhookReceivedDomainEvent_ShouldCreateWithProperties()
    {
        // Arrange
        var deliveryId = DeliveryId.Create("delivery-123");
        var eventName = WebhookEventName.Create("issues");
        var receivedAt = DateTimeOffset.UtcNow;
        var installationId = InstallationId.Create(12345);

        // Act
        var domainEvent = new WebhookReceivedDomainEvent(deliveryId, eventName, receivedAt, installationId);

        // Assert
        domainEvent.DeliveryId.Should().Be(deliveryId);
        domainEvent.EventName.Should().Be(eventName);
        domainEvent.ReceivedAt.Should().Be(receivedAt);
        domainEvent.InstallationId.Should().Be(installationId);
    }

    [Fact]
    public void WebhookReceivedDomainEvent_WithNullInstallationId_ShouldBeAllowed()
    {
        // Arrange
        var deliveryId = DeliveryId.Create("delivery-123");
        var eventName = WebhookEventName.Create("ping");
        var receivedAt = DateTimeOffset.UtcNow;

        // Act
        var domainEvent = new WebhookReceivedDomainEvent(deliveryId, eventName, receivedAt, null);

        // Assert
        domainEvent.DeliveryId.Should().Be(deliveryId);
        domainEvent.EventName.Should().Be(eventName);
        domainEvent.ReceivedAt.Should().Be(receivedAt);
        domainEvent.InstallationId.Should().BeNull();
    }

    [Fact]
    public void WebhookReceivedDomainEvent_RecordEquality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var deliveryId = DeliveryId.Create("delivery-123");
        var eventName = WebhookEventName.Create("push");
        var receivedAt = DateTimeOffset.UtcNow;
        var installationId = InstallationId.Create(12345);
        var event1 = new WebhookReceivedDomainEvent(deliveryId, eventName, receivedAt, installationId);
        var event2 = new WebhookReceivedDomainEvent(deliveryId, eventName, receivedAt, installationId);

        // Act & Assert
        event1.Should().Be(event2);
        (event1 == event2).Should().BeTrue();
    }

    [Fact]
    public void WebhookReceivedDomainEvent_RecordEquality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var deliveryId1 = DeliveryId.Create("delivery-123");
        var deliveryId2 = DeliveryId.Create("delivery-456");
        var eventName = WebhookEventName.Create("push");
        var receivedAt = DateTimeOffset.UtcNow;
        var installationId = InstallationId.Create(12345);
        var event1 = new WebhookReceivedDomainEvent(deliveryId1, eventName, receivedAt, installationId);
        var event2 = new WebhookReceivedDomainEvent(deliveryId2, eventName, receivedAt, installationId);

        // Act & Assert
        event1.Should().NotBe(event2);
        (event1 != event2).Should().BeTrue();
    }

    [Fact]
    public void WebhookReceivedDomainEvent_RecordEquality_WithDifferentInstallationIds_ShouldNotBeEqual()
    {
        // Arrange
        var deliveryId = DeliveryId.Create("delivery-123");
        var eventName = WebhookEventName.Create("push");
        var receivedAt = DateTimeOffset.UtcNow;
        var installationId1 = InstallationId.Create(12345);
        var installationId2 = InstallationId.Create(67890);
        var event1 = new WebhookReceivedDomainEvent(deliveryId, eventName, receivedAt, installationId1);
        var event2 = new WebhookReceivedDomainEvent(deliveryId, eventName, receivedAt, installationId2);

        // Act & Assert
        event1.Should().NotBe(event2);
        (event1 != event2).Should().BeTrue();
    }

    [Fact]
    public void WebhookReceivedDomainEvent_RecordEquality_OneWithNullInstallationId_ShouldNotBeEqual()
    {
        // Arrange
        var deliveryId = DeliveryId.Create("delivery-123");
        var eventName = WebhookEventName.Create("push");
        var receivedAt = DateTimeOffset.UtcNow;
        var installationId = InstallationId.Create(12345);
        var event1 = new WebhookReceivedDomainEvent(deliveryId, eventName, receivedAt, installationId);
        var event2 = new WebhookReceivedDomainEvent(deliveryId, eventName, receivedAt, null);

        // Act & Assert
        event1.Should().NotBe(event2);
        (event1 != event2).Should().BeTrue();
    }
}
