// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.Specifications;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Tests.Specifications;

public class WebhookDeliveryForInstallationSpecificationTests
{
    [Fact]
    public void Constructor_WithNullInstallationId_ShouldThrow()
    {
        var act = () => new WebhookDeliveryForInstallationSpecification(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsSatisfiedBy_WithMatchingInstallation_ShouldReturnTrue()
    {
        var installationId = InstallationId.Create(123);
        var result = WebhookDelivery.Create(
            DeliveryId.Create(Guid.NewGuid().ToString()),
            WebhookEventName.Create("push"),
            DateTimeOffset.UtcNow,
            WebhookPayload.Create("{}"),
            installationId);
        var delivery = result.Value!;
        var spec = new WebhookDeliveryForInstallationSpecification(installationId);

        spec.IsSatisfiedBy(delivery).Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_WithDifferentInstallation_ShouldReturnFalse()
    {
        var result = WebhookDelivery.Create(
            DeliveryId.Create(Guid.NewGuid().ToString()),
            WebhookEventName.Create("push"),
            DateTimeOffset.UtcNow,
            WebhookPayload.Create("{}"),
            InstallationId.Create(123));
        var delivery = result.Value!;
        var spec = new WebhookDeliveryForInstallationSpecification(InstallationId.Create(456));

        spec.IsSatisfiedBy(delivery).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_WithNullInstallationId_ShouldReturnFalse()
    {
        var result = WebhookDelivery.Create(
            DeliveryId.Create(Guid.NewGuid().ToString()),
            WebhookEventName.Create("push"),
            DateTimeOffset.UtcNow,
            WebhookPayload.Create("{}"),
            null);
        var delivery = result.Value!;
        var spec = new WebhookDeliveryForInstallationSpecification(InstallationId.Create(123));

        spec.IsSatisfiedBy(delivery).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_WithNullCandidate_ShouldThrow()
    {
        var spec = new WebhookDeliveryForInstallationSpecification(InstallationId.Create(123));

        var act = () => spec.IsSatisfiedBy(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
