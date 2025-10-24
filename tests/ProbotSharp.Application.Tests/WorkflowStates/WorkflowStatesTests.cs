// Copyright (c) ProbotSharp Contributors.

namespace ProbotSharp.Application.Tests.WorkflowStates;

using FluentAssertions;
using ProbotSharp.Application.Models;
using ProbotSharp.Application.WorkflowStates;
using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.ValueObjects;
using Xunit;

public class WorkflowStatesTests
{
    [Fact]
    public void UntrustedWebhook_ShouldStoreCommand()
    {
        // Arrange
        var command = CreateTestCommand();

        // Act
        var untrusted = new UntrustedWebhook(command);

        // Assert
        untrusted.Command.Should().Be(command);
    }

    [Fact]
    public void UntrustedWebhook_WithNullCommand_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new UntrustedWebhook(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("command");
    }

    [Fact]
    public void ValidatedWebhook_ShouldTransitionFromUntrusted()
    {
        // Arrange
        var command = CreateTestCommand();
        var untrusted = new UntrustedWebhook(command);

        // Act
        var validated = new ValidatedWebhook(untrusted);

        // Assert
        validated.Command.Should().Be(command);
    }

    [Fact]
    public void ValidatedWebhook_WithNullUntrusted_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new ValidatedWebhook(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("untrusted");
    }

    [Fact]
    public void VerifiedUniqueWebhook_ShouldTransitionFromValidated()
    {
        // Arrange
        var command = CreateTestCommand();
        var untrusted = new UntrustedWebhook(command);
        var validated = new ValidatedWebhook(untrusted);

        // Act
        var unique = new VerifiedUniqueWebhook(validated);

        // Assert
        unique.Command.Should().Be(command);
    }

    [Fact]
    public void VerifiedUniqueWebhook_WithNullValidated_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new VerifiedUniqueWebhook(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("validated");
    }

    [Fact]
    public void PersistedWebhook_ShouldTransitionFromUniqueAndStoreDelivery()
    {
        // Arrange
        var command = CreateTestCommand();
        var untrusted = new UntrustedWebhook(command);
        var validated = new ValidatedWebhook(untrusted);
        var unique = new VerifiedUniqueWebhook(validated);
        var delivery = CreateTestDelivery();

        // Act
        var persisted = new PersistedWebhook(unique, delivery);

        // Assert
        persisted.Command.Should().Be(command);
        persisted.Delivery.Should().Be(delivery);
    }

    [Fact]
    public void PersistedWebhook_WithNullUnique_ShouldThrowArgumentNullException()
    {
        // Arrange
        var delivery = CreateTestDelivery();

        // Act
        Action act = () => new PersistedWebhook(null!, delivery);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("unique");
    }

    [Fact]
    public void PersistedWebhook_WithNullDelivery_ShouldThrowArgumentNullException()
    {
        // Arrange
        var command = CreateTestCommand();
        var untrusted = new UntrustedWebhook(command);
        var validated = new ValidatedWebhook(untrusted);
        var unique = new VerifiedUniqueWebhook(validated);

        // Act
        Action act = () => new PersistedWebhook(unique, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("delivery");
    }

    private static ProcessWebhookCommand CreateTestCommand()
    {
        var payloadJson = "{\"action\":\"opened\"}";
        return new ProcessWebhookCommand(
            DeliveryId.Create("test-delivery-123"),
            WebhookEventName.Create("issues"),
            WebhookPayload.Create(payloadJson),
            InstallationId.Create(12345L),
            WebhookSignature.Create("sha256=" + new string('a', 64)),
            payloadJson);
    }

    private static WebhookDelivery CreateTestDelivery()
    {
        var result = WebhookDelivery.Create(
            DeliveryId.Create("test-delivery-123"),
            WebhookEventName.Create("issues"),
            DateTimeOffset.UtcNow,
            WebhookPayload.Create("{\"action\":\"opened\"}"),
            InstallationId.Create(12345L));
        return result.Value!;
    }
}
