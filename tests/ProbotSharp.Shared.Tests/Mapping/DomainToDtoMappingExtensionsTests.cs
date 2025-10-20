// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Security.Cryptography;

using FluentAssertions;

using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.Mapping;

using Xunit;

namespace ProbotSharp.Shared.Tests.Mapping;

public sealed class DomainToDtoMappingExtensionsTests
{
    [Fact]
    public void ToDto_WithValidGitHubApp_ShouldMapAllProperties()
    {
        // Arrange
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(GeneratePem());
        var app = GitHubApp.Create(appId, "Test App", privateKey, "webhook-secret");

        // Act
        var dto = app.ToDto();

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(12345);
        dto.Name.Should().Be("Test App");
        dto.HasPrivateKey.Should().BeTrue();
        dto.HasWebhookSecret.Should().BeTrue();
        dto.Installations.Should().BeEmpty();
    }

    [Fact]
    public void ToDto_WithGitHubAppWithInstallations_ShouldMapInstallations()
    {
        // Arrange
        var appId = GitHubAppId.Create(12345);
        var privateKey = PrivateKeyPem.Create(GeneratePem());
        var app = GitHubApp.Create(appId, "Test App", privateKey, "webhook-secret");
        var installationId = InstallationId.Create(67890);
        app.AddInstallation(installationId, "test-account");

        // Act
        var dto = app.ToDto();

        // Assert
        dto.Should().NotBeNull();
        dto.Installations.Should().HaveCount(1);
        dto.Installations[0].Id.Should().Be(67890);
        dto.Installations[0].AccountLogin.Should().Be("test-account");
    }

    [Fact]
    public void ToDto_WithNullGitHubApp_ShouldThrowArgumentNullException()
    {
        // Arrange
        GitHubApp? app = null;

        // Act
        var act = () => app!.ToDto();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDto_WithValidInstallation_ShouldMapAllProperties()
    {
        // Arrange
        var installationId = InstallationId.Create(67890);
        var installation = Installation.Create(installationId, "test-account");

        // Act
        var dto = installation.ToDto();

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(67890);
        dto.AccountLogin.Should().Be("test-account");
        dto.Repositories.Should().BeEmpty();
    }

    [Fact]
    public void ToDto_WithInstallationWithRepositories_ShouldMapRepositories()
    {
        // Arrange
        var installationId = InstallationId.Create(67890);
        var installation = Installation.Create(installationId, "test-account");
        installation.AddRepository(12345, "test-repo", "test-account/test-repo");

        // Act
        var dto = installation.ToDto();

        // Assert
        dto.Should().NotBeNull();
        dto.Repositories.Should().HaveCount(1);
        dto.Repositories[0].Id.Should().Be(12345);
        dto.Repositories[0].Name.Should().Be("test-repo");
        dto.Repositories[0].FullName.Should().Be("test-account/test-repo");
    }

    [Fact]
    public void ToDto_WithNullInstallation_ShouldThrowArgumentNullException()
    {
        // Arrange
        Installation? installation = null;

        // Act
        var act = () => installation!.ToDto();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDto_WithValidRepository_ShouldMapAllProperties()
    {
        // Arrange
        var repository = Repository.Create(12345, "test-repo", "test-account/test-repo");

        // Act
        var dto = repository.ToDto();

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(12345);
        dto.Name.Should().Be("test-repo");
        dto.FullName.Should().Be("test-account/test-repo");
    }

    [Fact]
    public void ToDto_WithNullRepository_ShouldThrowArgumentNullException()
    {
        // Arrange
        Repository? repository = null;

        // Act
        var act = () => repository!.ToDto();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDto_WithValidWebhookDelivery_ShouldMapAllProperties()
    {
        // Arrange
        var deliveryId = DeliveryId.Create("abc-123-def");
        var eventName = WebhookEventName.Create("push");
        var deliveredAt = DateTimeOffset.UtcNow;
        var payload = WebhookPayload.Create("{\"action\":\"opened\"}");
        var installationId = InstallationId.Create(67890);
        var delivery = WebhookDelivery.Create(deliveryId, eventName, deliveredAt, payload, installationId);

        // Act
        var dto = delivery.ToDto();

        // Assert
        dto.Should().NotBeNull();
        dto.DeliveryId.Should().Be("abc-123-def");
        dto.EventName.Should().Be("push");
        dto.DeliveredAt.Should().Be(deliveredAt);
        dto.Payload.Should().Be("{\"action\":\"opened\"}");
        dto.InstallationId.Should().Be(67890);
    }

    [Fact]
    public void ToDto_WithWebhookDeliveryWithoutInstallation_ShouldMapInstallationIdAsNull()
    {
        // Arrange
        var deliveryId = DeliveryId.Create("abc-123-def");
        var eventName = WebhookEventName.Create("push");
        var deliveredAt = DateTimeOffset.UtcNow;
        var payload = WebhookPayload.Create("{\"action\":\"opened\"}");
        var delivery = WebhookDelivery.Create(deliveryId, eventName, deliveredAt, payload, null);

        // Act
        var dto = delivery.ToDto();

        // Assert
        dto.Should().NotBeNull();
        dto.InstallationId.Should().BeNull();
    }

    [Fact]
    public void ToDto_WithNullWebhookDelivery_ShouldThrowArgumentNullException()
    {
        // Arrange
        WebhookDelivery? delivery = null;

        // Act
        var act = () => delivery!.ToDto();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDtoList_WithValidGitHubApps_ShouldMapAllApps()
    {
        // Arrange
        var appId1 = GitHubAppId.Create(12345);
        var appId2 = GitHubAppId.Create(67890);
        var privateKey = PrivateKeyPem.Create(GeneratePem());
        var app1 = GitHubApp.Create(appId1, "Test App 1", privateKey, "webhook-secret-1");
        var app2 = GitHubApp.Create(appId2, "Test App 2", privateKey, "webhook-secret-2");
        var apps = new[] { app1, app2 };

        // Act
        var dtos = apps.ToDtoList();

        // Assert
        dtos.Should().HaveCount(2);
        dtos[0].Id.Should().Be(12345);
        dtos[0].Name.Should().Be("Test App 1");
        dtos[1].Id.Should().Be(67890);
        dtos[1].Name.Should().Be("Test App 2");
    }

    [Fact]
    public void ToDtoList_WithEmptyGitHubApps_ShouldReturnEmptyList()
    {
        // Arrange
        var apps = Enumerable.Empty<GitHubApp>();

        // Act
        var dtos = apps.ToDtoList();

        // Assert
        dtos.Should().BeEmpty();
    }

    [Fact]
    public void ToDtoList_WithNullGitHubApps_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<GitHubApp>? apps = null;

        // Act
        var act = () => apps!.ToDtoList();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDtoList_WithValidInstallations_ShouldMapAllInstallations()
    {
        // Arrange
        var installationId1 = InstallationId.Create(12345);
        var installationId2 = InstallationId.Create(67890);
        var installation1 = Installation.Create(installationId1, "account-1");
        var installation2 = Installation.Create(installationId2, "account-2");
        var installations = new[] { installation1, installation2 };

        // Act
        var dtos = installations.ToDtoList();

        // Assert
        dtos.Should().HaveCount(2);
        dtos[0].Id.Should().Be(12345);
        dtos[0].AccountLogin.Should().Be("account-1");
        dtos[1].Id.Should().Be(67890);
        dtos[1].AccountLogin.Should().Be("account-2");
    }

    [Fact]
    public void ToDtoList_WithEmptyInstallations_ShouldReturnEmptyList()
    {
        // Arrange
        var installations = Enumerable.Empty<Installation>();

        // Act
        var dtos = installations.ToDtoList();

        // Assert
        dtos.Should().BeEmpty();
    }

    [Fact]
    public void ToDtoList_WithNullInstallations_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<Installation>? installations = null;

        // Act
        var act = () => installations!.ToDtoList();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDtoList_WithValidRepositories_ShouldMapAllRepositories()
    {
        // Arrange
        var repository1 = Repository.Create(12345, "repo-1", "account/repo-1");
        var repository2 = Repository.Create(67890, "repo-2", "account/repo-2");
        var repositories = new[] { repository1, repository2 };

        // Act
        var dtos = repositories.ToDtoList();

        // Assert
        dtos.Should().HaveCount(2);
        dtos[0].Id.Should().Be(12345);
        dtos[0].Name.Should().Be("repo-1");
        dtos[0].FullName.Should().Be("account/repo-1");
        dtos[1].Id.Should().Be(67890);
        dtos[1].Name.Should().Be("repo-2");
        dtos[1].FullName.Should().Be("account/repo-2");
    }

    [Fact]
    public void ToDtoList_WithEmptyRepositories_ShouldReturnEmptyList()
    {
        // Arrange
        var repositories = Enumerable.Empty<Repository>();

        // Act
        var dtos = repositories.ToDtoList();

        // Assert
        dtos.Should().BeEmpty();
    }

    [Fact]
    public void ToDtoList_WithNullRepositories_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<Repository>? repositories = null;

        // Act
        var act = () => repositories!.ToDtoList();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDtoList_WithValidWebhookDeliveries_ShouldMapAllDeliveries()
    {
        // Arrange
        var deliveryId1 = DeliveryId.Create("abc-123");
        var deliveryId2 = DeliveryId.Create("def-456");
        var eventName = WebhookEventName.Create("push");
        var deliveredAt = DateTimeOffset.UtcNow;
        var payload = WebhookPayload.Create("{\"action\":\"opened\"}");
        var delivery1 = WebhookDelivery.Create(deliveryId1, eventName, deliveredAt, payload, null);
        var delivery2 = WebhookDelivery.Create(deliveryId2, eventName, deliveredAt, payload, null);
        var deliveries = new[] { delivery1, delivery2 };

        // Act
        var dtos = deliveries.ToDtoList();

        // Assert
        dtos.Should().HaveCount(2);
        dtos[0].DeliveryId.Should().Be("abc-123");
        dtos[1].DeliveryId.Should().Be("def-456");
    }

    [Fact]
    public void ToDtoList_WithEmptyWebhookDeliveries_ShouldReturnEmptyList()
    {
        // Arrange
        var deliveries = Enumerable.Empty<WebhookDelivery>();

        // Act
        var dtos = deliveries.ToDtoList();

        // Assert
        dtos.Should().BeEmpty();
    }

    [Fact]
    public void ToDtoList_WithNullWebhookDeliveries_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<WebhookDelivery>? deliveries = null;

        // Act
        var act = () => deliveries!.ToDtoList();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    private static string GeneratePem()
    {
        using var rsa = RSA.Create(2048);
        var pkcs8 = rsa.ExportPkcs8PrivateKey();
        var base64 = Convert.ToBase64String(pkcs8, Base64FormattingOptions.InsertLineBreaks);
        return $@"-----BEGIN PRIVATE KEY-----
{base64}
-----END PRIVATE KEY-----";
    }
}
