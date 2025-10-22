// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using FluentAssertions;

using ProbotSharp.Shared.DTOs;
using ProbotSharp.Shared.Mapping;

using Xunit;

namespace ProbotSharp.Shared.Tests.Mapping;

public sealed class DtoToDomainMappingExtensionsTests
{
    [Fact]
    public void ToDomain_WithValidRepositoryDto_ShouldMapAllProperties()
    {
        // Arrange
        var dto = new RepositoryDto
        {
            Id = 12345,
            Name = "test-repo",
            FullName = "test-account/test-repo"
        };

        // Act
        var repository = dto.ToDomain();

        // Assert
        repository.Should().NotBeNull();
        repository.Id.Should().Be(12345);
        repository.Name.Should().Be("test-repo");
        repository.FullName.Should().Be("test-account/test-repo");
    }

    [Fact]
    public void ToDomain_WithNullRepositoryDto_ShouldThrowArgumentNullException()
    {
        // Arrange
        RepositoryDto? dto = null;

        // Act
        var act = () => dto!.ToDomain();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDomain_WithValidInstallationDto_ShouldMapAllProperties()
    {
        // Arrange
        var dto = new InstallationDto
        {
            Id = 67890,
            AccountLogin = "test-account"
        };

        // Act
        var installation = dto.ToDomain();

        // Assert
        installation.Should().NotBeNull();
        installation.Id.Value.Should().Be(67890);
        installation.AccountLogin.Should().Be("test-account");
        installation.Repositories.Should().BeEmpty();
    }

    [Fact]
    public void ToDomain_WithInstallationDtoWithRepositories_ShouldIgnoreRepositories()
    {
        // Arrange
        var dto = new InstallationDto
        {
            Id = 67890,
            AccountLogin = "test-account",
            Repositories = new List<RepositoryDto>
            {
                new() { Id = 12345, Name = "repo-1", FullName = "account/repo-1" }
            }
        };

        // Act
        var installation = dto.ToDomain();

        // Assert
        installation.Should().NotBeNull();
        installation.Repositories.Should().BeEmpty(); // Repositories are not mapped by design
    }

    [Fact]
    public void ToDomain_WithNullInstallationDto_ShouldThrowArgumentNullException()
    {
        // Arrange
        InstallationDto? dto = null;

        // Act
        var act = () => dto!.ToDomain();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDomain_WithValidWebhookDeliveryDto_ShouldMapAllProperties()
    {
        // Arrange
        var deliveredAt = DateTimeOffset.UtcNow;
        var dto = new WebhookDeliveryDto
        {
            DeliveryId = "abc-123-def",
            EventName = "push",
            DeliveredAt = deliveredAt,
            Payload = "{\"action\":\"opened\"}",
            InstallationId = 67890
        };

        // Act
        var result = dto.ToDomain();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var delivery = result.Value!;
        delivery.Should().NotBeNull();
        delivery.Id.Value.Should().Be("abc-123-def");
        delivery.EventName.Value.Should().Be("push");
        delivery.DeliveredAt.Should().Be(deliveredAt);
        delivery.Payload.RawBody.Should().Be("{\"action\":\"opened\"}");
        delivery.InstallationId.Should().NotBeNull();
        delivery.InstallationId!.Value.Should().Be(67890);
    }

    [Fact]
    public void ToDomain_WithWebhookDeliveryDtoWithoutInstallationId_ShouldMapInstallationIdAsNull()
    {
        // Arrange
        var deliveredAt = DateTimeOffset.UtcNow;
        var dto = new WebhookDeliveryDto
        {
            DeliveryId = "abc-123-def",
            EventName = "push",
            DeliveredAt = deliveredAt,
            Payload = "{\"action\":\"opened\"}",
            InstallationId = null
        };

        // Act
        var result = dto.ToDomain();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var delivery = result.Value!;
        delivery.Should().NotBeNull();
        delivery.InstallationId.Should().BeNull();
    }

    [Fact]
    public void ToDomain_WithNullWebhookDeliveryDto_ShouldThrowArgumentNullException()
    {
        // Arrange
        WebhookDeliveryDto? dto = null;

        // Act
        var act = () => dto!.ToDomain();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDomainList_WithValidRepositoryDtos_ShouldMapAllRepositories()
    {
        // Arrange
        var dtos = new[]
        {
            new RepositoryDto { Id = 12345, Name = "repo-1", FullName = "account/repo-1" },
            new RepositoryDto { Id = 67890, Name = "repo-2", FullName = "account/repo-2" }
        };

        // Act
        var repositories = dtos.ToDomainList();

        // Assert
        repositories.Should().HaveCount(2);
        repositories[0].Id.Should().Be(12345);
        repositories[0].Name.Should().Be("repo-1");
        repositories[0].FullName.Should().Be("account/repo-1");
        repositories[1].Id.Should().Be(67890);
        repositories[1].Name.Should().Be("repo-2");
        repositories[1].FullName.Should().Be("account/repo-2");
    }

    [Fact]
    public void ToDomainList_WithEmptyRepositoryDtos_ShouldReturnEmptyList()
    {
        // Arrange
        var dtos = Enumerable.Empty<RepositoryDto>();

        // Act
        var repositories = dtos.ToDomainList();

        // Assert
        repositories.Should().BeEmpty();
    }

    [Fact]
    public void ToDomainList_WithNullRepositoryDtos_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<RepositoryDto>? dtos = null;

        // Act
        var act = () => dtos!.ToDomainList();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDomainList_WithValidInstallationDtos_ShouldMapAllInstallations()
    {
        // Arrange
        var dtos = new[]
        {
            new InstallationDto { Id = 12345, AccountLogin = "account-1" },
            new InstallationDto { Id = 67890, AccountLogin = "account-2" }
        };

        // Act
        var installations = dtos.ToDomainList();

        // Assert
        installations.Should().HaveCount(2);
        installations[0].Id.Value.Should().Be(12345);
        installations[0].AccountLogin.Should().Be("account-1");
        installations[1].Id.Value.Should().Be(67890);
        installations[1].AccountLogin.Should().Be("account-2");
    }

    [Fact]
    public void ToDomainList_WithEmptyInstallationDtos_ShouldReturnEmptyList()
    {
        // Arrange
        var dtos = Enumerable.Empty<InstallationDto>();

        // Act
        var installations = dtos.ToDomainList();

        // Assert
        installations.Should().BeEmpty();
    }

    [Fact]
    public void ToDomainList_WithNullInstallationDtos_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<InstallationDto>? dtos = null;

        // Act
        var act = () => dtos!.ToDomainList();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDomainList_WithValidWebhookDeliveryDtos_ShouldMapAllDeliveries()
    {
        // Arrange
        var deliveredAt = DateTimeOffset.UtcNow;
        var dtos = new[]
        {
            new WebhookDeliveryDto
            {
                DeliveryId = "abc-123",
                EventName = "push",
                DeliveredAt = deliveredAt,
                Payload = "{\"action\":\"opened\"}",
                InstallationId = 12345
            },
            new WebhookDeliveryDto
            {
                DeliveryId = "def-456",
                EventName = "pull_request",
                DeliveredAt = deliveredAt,
                Payload = "{\"action\":\"closed\"}",
                InstallationId = null
            }
        };

        // Act
        var result = dtos.ToDomainList();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var deliveries = result.Value!;
        deliveries.Should().HaveCount(2);
        deliveries[0].Id.Value.Should().Be("abc-123");
        deliveries[0].EventName.Value.Should().Be("push");
        deliveries[0].InstallationId.Should().NotBeNull();
        deliveries[0].InstallationId!.Value.Should().Be(12345);
        deliveries[1].Id.Value.Should().Be("def-456");
        deliveries[1].EventName.Value.Should().Be("pull_request");
        deliveries[1].InstallationId.Should().BeNull();
    }

    [Fact]
    public void ToDomainList_WithEmptyWebhookDeliveryDtos_ShouldReturnEmptyList()
    {
        // Arrange
        var dtos = Enumerable.Empty<WebhookDeliveryDto>();

        // Act
        var result = dtos.ToDomainList();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public void ToDomainList_WithNullWebhookDeliveryDtos_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<WebhookDeliveryDto>? dtos = null;

        // Act
        var act = () => dtos!.ToDomainList();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
