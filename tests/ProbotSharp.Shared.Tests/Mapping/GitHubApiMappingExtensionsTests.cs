// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Text.Json;
using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.DTOs;
using ProbotSharp.Shared.Mapping;

namespace ProbotSharp.Shared.Tests.Mapping;

public class GitHubApiMappingExtensionsTests
{
    #region ToRepository Tests

    [Fact]
    public void ToRepository_WithValidDto_ShouldMapCorrectly()
    {
        // Arrange
        var dto = new GitHubApiRepositoryDto
        {
            Id = 12345,
            Name = "test-repo",
            FullName = "owner/test-repo",
        };

        // Act
        var result = dto.ToRepository();

        // Assert
        result.Id.Should().Be(12345);
        result.Name.Should().Be("test-repo");
        result.FullName.Should().Be("owner/test-repo");
    }

    [Fact]
    public void ToRepository_WithNullDto_ShouldThrowArgumentNullException()
    {
        // Arrange
        GitHubApiRepositoryDto? dto = null;

        // Act
        var act = () => dto!.ToRepository();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ToInstallation Tests

    [Fact]
    public void ToInstallation_WithValidDto_ShouldMapCorrectly()
    {
        // Arrange
        var dto = new GitHubApiInstallationDto
        {
            Id = 67890,
            Account = new GitHubApiAccountDto { Login = "testuser" },
        };

        // Act
        var result = dto.ToInstallation();

        // Assert
        result.Id.Value.Should().Be(67890);
        result.AccountLogin.Should().Be("testuser");
    }

    [Fact]
    public void ToInstallation_WithNullAccount_ShouldUseUnknown()
    {
        // Arrange
        var dto = new GitHubApiInstallationDto
        {
            Id = 67890,
            Account = null,
        };

        // Act
        var result = dto.ToInstallation();

        // Assert
        result.AccountLogin.Should().Be("unknown");
    }

    [Fact]
    public void ToInstallation_WithAccountButNullLogin_ShouldUseUnknown()
    {
        // Arrange
        var dto = new GitHubApiInstallationDto
        {
            Id = 67890,
            Account = new GitHubApiAccountDto { Login = null! },
        };

        // Act
        var result = dto.ToInstallation();

        // Assert
        result.AccountLogin.Should().Be("unknown");
    }

    [Fact]
    public void ToInstallation_WithNullDto_ShouldThrowArgumentNullException()
    {
        // Arrange
        GitHubApiInstallationDto? dto = null;

        // Act
        var act = () => dto!.ToInstallation();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ToInstallationAccessToken Tests

    [Fact]
    public void ToInstallationAccessToken_WithValidDto_ShouldMapCorrectly()
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        var dto = new GitHubApiAccessTokenDto
        {
            Token = "ghs_test_token_12345",
            ExpiresAt = expiresAt,
        };

        // Act
        var result = dto.ToInstallationAccessToken();

        // Assert
        result.Value.Should().Be("ghs_test_token_12345");
        result.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public void ToInstallationAccessToken_WithNullDto_ShouldThrowArgumentNullException()
    {
        // Arrange
        GitHubApiAccessTokenDto? dto = null;

        // Act
        var act = () => dto!.ToInstallationAccessToken();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ToRepositories Tests

    [Fact]
    public void ToRepositories_WithValidCollection_ShouldMapAll()
    {
        // Arrange
        var dtos = new List<GitHubApiRepositoryDto>
        {
            new() { Id = 1, Name = "repo1", FullName = "owner/repo1" },
            new() { Id = 2, Name = "repo2", FullName = "owner/repo2" },
            new() { Id = 3, Name = "repo3", FullName = "owner/repo3" },
        };

        // Act
        var result = dtos.ToRepositories();

        // Assert
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("repo1");
        result[1].Name.Should().Be("repo2");
        result[2].Name.Should().Be("repo3");
    }

    [Fact]
    public void ToRepositories_WithEmptyCollection_ShouldReturnEmptyList()
    {
        // Arrange
        var dtos = new List<GitHubApiRepositoryDto>();

        // Act
        var result = dtos.ToRepositories();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToRepositories_WithNullCollection_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<GitHubApiRepositoryDto>? dtos = null;

        // Act
        var act = () => dtos!.ToRepositories();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ToInstallations Tests

    [Fact]
    public void ToInstallations_WithValidCollection_ShouldMapAll()
    {
        // Arrange
        var dtos = new List<GitHubApiInstallationDto>
        {
            new() { Id = 1, Account = new GitHubApiAccountDto { Login = "user1" } },
            new() { Id = 2, Account = new GitHubApiAccountDto { Login = "user2" } },
            new() { Id = 3, Account = new GitHubApiAccountDto { Login = "user3" } },
        };

        // Act
        var result = dtos.ToInstallations();

        // Assert
        result.Should().HaveCount(3);
        result[0].AccountLogin.Should().Be("user1");
        result[1].AccountLogin.Should().Be("user2");
        result[2].AccountLogin.Should().Be("user3");
    }

    [Fact]
    public void ToInstallations_WithEmptyCollection_ShouldReturnEmptyList()
    {
        // Arrange
        var dtos = new List<GitHubApiInstallationDto>();

        // Act
        var result = dtos.ToInstallations();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToInstallations_WithNullCollection_ShouldThrowArgumentNullException()
    {
        // Arrange
        IEnumerable<GitHubApiInstallationDto>? dtos = null;

        // Act
        var act = () => dtos!.ToInstallations();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ExtractInstallationId Tests

    [Fact]
    public void ExtractInstallationId_WithValidInstallation_ShouldReturnId()
    {
        // Arrange
        var json = @"{""installation"":{""id"":12345}}";
        var payload = WebhookPayload.Create(json);

        // Act
        var result = payload.ExtractInstallationId();

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be(12345);
    }

    [Fact]
    public void ExtractInstallationId_WithoutInstallation_ShouldReturnNull()
    {
        // Arrange
        var json = @"{""other"":""data""}";
        var payload = WebhookPayload.Create(json);

        // Act
        var result = payload.ExtractInstallationId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractInstallationId_WithInstallationButNoId_ShouldReturnNull()
    {
        // Arrange
        var json = @"{""installation"":{""other"":""field""}}";
        var payload = WebhookPayload.Create(json);

        // Act
        var result = payload.ExtractInstallationId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractInstallationId_WithNullPayload_ShouldThrowArgumentNullException()
    {
        // Arrange
        WebhookPayload? payload = null;

        // Act
        var act = () => payload!.ExtractInstallationId();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ExtractRepository Tests

    [Fact]
    public void ExtractRepository_WithValidRepository_ShouldReturnRepository()
    {
        // Arrange
        var json = @"{""repository"":{""id"":123,""name"":""test-repo"",""full_name"":""owner/test-repo""}}";
        var payload = WebhookPayload.Create(json);

        // Act
        var result = payload.ExtractRepository();

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(123);
        result.Name.Should().Be("test-repo");
        result.FullName.Should().Be("owner/test-repo");
    }

    [Fact]
    public void ExtractRepository_WithoutRepository_ShouldReturnNull()
    {
        // Arrange
        var json = @"{""other"":""data""}";
        var payload = WebhookPayload.Create(json);

        // Act
        var result = payload.ExtractRepository();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractRepository_WithRepositoryButMissingId_ShouldReturnNull()
    {
        // Arrange
        var json = @"{""repository"":{""name"":""test-repo"",""full_name"":""owner/test-repo""}}";
        var payload = WebhookPayload.Create(json);

        // Act
        var result = payload.ExtractRepository();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractRepository_WithRepositoryButMissingName_ShouldReturnNull()
    {
        // Arrange
        var json = @"{""repository"":{""id"":123,""full_name"":""owner/test-repo""}}";
        var payload = WebhookPayload.Create(json);

        // Act
        var result = payload.ExtractRepository();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractRepository_WithRepositoryButMissingFullName_ShouldReturnNull()
    {
        // Arrange
        var json = @"{""repository"":{""id"":123,""name"":""test-repo""}}";
        var payload = WebhookPayload.Create(json);

        // Act
        var result = payload.ExtractRepository();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractRepository_WithNullName_ShouldReturnNull()
    {
        // Arrange - Repository.Create() validates that name cannot be null/empty
        var json = @"{""repository"":{""id"":123,""name"":null,""full_name"":""owner/test-repo""}}";
        var payload = WebhookPayload.Create(json);

        // Act
        var result = payload.ExtractRepository();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractRepository_WithNullFullName_ShouldReturnNull()
    {
        // Arrange - Repository.Create() validates that fullName cannot be null/empty
        var json = @"{""repository"":{""id"":123,""name"":""test-repo"",""full_name"":null}}";
        var payload = WebhookPayload.Create(json);

        // Act
        var result = payload.ExtractRepository();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractRepository_WithNullPayload_ShouldThrowArgumentNullException()
    {
        // Arrange
        WebhookPayload? payload = null;

        // Act
        var act = () => payload!.ExtractRepository();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ExtractSenderLogin Tests

    [Fact]
    public void ExtractSenderLogin_WithValidSender_ShouldReturnLogin()
    {
        // Arrange
        var json = @"{""sender"":{""login"":""testuser""}}";
        var payload = WebhookPayload.Create(json);

        // Act
        var result = payload.ExtractSenderLogin();

        // Assert
        result.Should().Be("testuser");
    }

    [Fact]
    public void ExtractSenderLogin_WithoutSender_ShouldReturnNull()
    {
        // Arrange
        var json = @"{""other"":""data""}";
        var payload = WebhookPayload.Create(json);

        // Act
        var result = payload.ExtractSenderLogin();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractSenderLogin_WithSenderButNoLogin_ShouldReturnNull()
    {
        // Arrange
        var json = @"{""sender"":{""id"":12345}}";
        var payload = WebhookPayload.Create(json);

        // Act
        var result = payload.ExtractSenderLogin();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractSenderLogin_WithNullPayload_ShouldThrowArgumentNullException()
    {
        // Arrange
        WebhookPayload? payload = null;

        // Act
        var act = () => payload!.ExtractSenderLogin();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ExtractAction Tests

    [Fact]
    public void ExtractAction_WithValidAction_ShouldReturnAction()
    {
        // Arrange
        var json = @"{""action"":""opened""}";
        var payload = WebhookPayload.Create(json);

        // Act
        var result = payload.ExtractAction();

        // Assert
        result.Should().Be("opened");
    }

    [Fact]
    public void ExtractAction_WithoutAction_ShouldReturnNull()
    {
        // Arrange
        var json = @"{""other"":""data""}";
        var payload = WebhookPayload.Create(json);

        // Act
        var result = payload.ExtractAction();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractAction_WithNullPayload_ShouldThrowArgumentNullException()
    {
        // Arrange
        WebhookPayload? payload = null;

        // Act
        var act = () => payload!.ExtractAction();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExtractAction_WithVariousActions_ShouldReturnCorrectValue()
    {
        // Arrange
        var actions = new[] { "opened", "closed", "edited", "deleted", "reopened" };

        foreach (var action in actions)
        {
            var json = $@"{{""action"":""{action}""}}";
            var payload = WebhookPayload.Create(json);

            // Act
            var result = payload.ExtractAction();

            // Assert
            result.Should().Be(action);
        }
    }

    #endregion
}
