// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

using NSubstitute;

using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Infrastructure.Context;
using ProbotSharp.Domain.Abstractions;

namespace ProbotSharp.Infrastructure.Tests.Context;

public class ProbotSharpContextFactoryTests
{
    private readonly IInstallationAuthenticationPort _installationAuth;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ProbotSharpContextFactory _factory;

    public ProbotSharpContextFactoryTests()
    {
        _installationAuth = Substitute.For<IInstallationAuthenticationPort>();
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _logger = Substitute.For<ILogger>();
        _httpClientFactory = Substitute.For<IHttpClientFactory>();

        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(_logger);

        // Setup HttpClient factory to return a mock HttpClient
        using var httpClient = new HttpClient { BaseAddress = new Uri("https://api.github.com/") };
        _httpClientFactory.CreateClient("GitHubGraphQL").Returns(httpClient);

        _factory = new ProbotSharpContextFactory(_installationAuth, _loggerFactory, _httpClientFactory, Array.Empty<IProbotSharpContextConfigurator>());
    }

    [Fact]
    public async Task CreateAsync_WithValidDelivery_ShouldCreateContext()
    {
        // Arrange
        var installationId = InstallationId.Create(123);
        var payload = WebhookPayload.Create(@"{
            ""action"": ""opened"",
            ""repository"": {
                ""id"": 456,
                ""name"": ""test-repo"",
                ""full_name"": ""test-owner/test-repo"",
                ""owner"": {
                    ""login"": ""test-owner""
                }
            },
            ""installation"": {
                ""id"": 123,
                ""account"": {
                    ""login"": ""test-account""
                }
            }
        }");

        var deliveryResult = WebhookDelivery.Create(
            DeliveryId.Create("delivery-123"),
            WebhookEventName.Create("issues"),
            DateTimeOffset.UtcNow,
            payload,
            installationId);
        var delivery = deliveryResult.Value!;

        var token = InstallationAccessToken.Create("ghs_token123", DateTimeOffset.UtcNow.AddHours(1));
        _installationAuth
            .AuthenticateAsync(Arg.Any<AuthenticateInstallationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<InstallationAccessToken>.Success(token));

        // Act
        var context = await _factory.CreateAsync(delivery, CancellationToken.None);

        // Assert
        context.Should().NotBeNull();
        context.Id.Should().Be("delivery-123");
        context.EventName.Should().Be("issues");
        context.EventAction.Should().Be("opened");
        context.Payload.Should().NotBeNull();
        context.Logger.Should().NotBeNull();
        context.GitHub.Should().NotBeNull();
        context.Repository.Should().NotBeNull();
        context.Repository!.Id.Should().Be(456);
        context.Repository.Name.Should().Be("test-repo");
        context.Repository.Owner.Should().Be("test-owner");
        context.Installation.Should().NotBeNull();
        context.Installation!.Id.Should().Be(123);
        context.Installation.AccountLogin.Should().Be("test-account");
    }

    [Fact]
    public async Task CreateAsync_WithoutInstallationId_ShouldCreateUnauthenticatedContext()
    {
        // Arrange
        var payload = WebhookPayload.Create(@"{
            ""action"": ""opened"",
            ""repository"": {
                ""id"": 456,
                ""name"": ""test-repo"",
                ""full_name"": ""test-owner/test-repo"",
                ""owner"": {
                    ""login"": ""test-owner""
                }
            }
        }");

        var deliveryResult = WebhookDelivery.Create(
            DeliveryId.Create("delivery-123"),
            WebhookEventName.Create("push"),
            DateTimeOffset.UtcNow,
            payload,
            null);
        var delivery = deliveryResult.Value!;

        // Act
        var context = await _factory.CreateAsync(delivery, CancellationToken.None);

        // Assert
        context.Should().NotBeNull();
        context.Id.Should().Be("delivery-123");
        context.EventName.Should().Be("push");
        context.GitHub.Should().NotBeNull();
        context.Installation.Should().BeNull();

        // Should not have called authentication
        await _installationAuth.DidNotReceive()
            .AuthenticateAsync(Arg.Any<AuthenticateInstallationCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithoutRepository_ShouldCreateContextWithNullRepository()
    {
        // Arrange
        var installationId = InstallationId.Create(123);
        var payload = WebhookPayload.Create(@"{
            ""action"": ""created"",
            ""installation"": {
                ""id"": 123,
                ""account"": {
                    ""login"": ""test-account""
                }
            }
        }");

        var deliveryResult = WebhookDelivery.Create(
            DeliveryId.Create("delivery-123"),
            WebhookEventName.Create("installation"),
            DateTimeOffset.UtcNow,
            payload,
            installationId);
        var delivery = deliveryResult.Value!;

        var token = InstallationAccessToken.Create("ghs_token123", DateTimeOffset.UtcNow.AddHours(1));
        _installationAuth
            .AuthenticateAsync(Arg.Any<AuthenticateInstallationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<InstallationAccessToken>.Success(token));

        // Act
        var context = await _factory.CreateAsync(delivery, CancellationToken.None);

        // Assert
        context.Should().NotBeNull();
        context.Repository.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WhenAuthenticationFails_ShouldThrowException()
    {
        // Arrange
        var installationId = InstallationId.Create(123);
        var payload = WebhookPayload.Create(@"{
            ""action"": ""opened"",
            ""installation"": {
                ""id"": 123,
                ""account"": {
                    ""login"": ""test-account""
                }
            }
        }");

        var deliveryResult = WebhookDelivery.Create(
            DeliveryId.Create("delivery-123"),
            WebhookEventName.Create("issues"),
            DateTimeOffset.UtcNow,
            payload,
            installationId);
        var delivery = deliveryResult.Value!;

        _installationAuth
            .AuthenticateAsync(Arg.Any<AuthenticateInstallationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<InstallationAccessToken>.Failure("auth_failed", "Authentication failed"));

        // Act
        var act = async () => await _factory.CreateAsync(delivery, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_ShouldCallInstallationAuthWithCorrectId()
    {
        // Arrange
        var installationId = InstallationId.Create(123);
        var payload = WebhookPayload.Create(@"{
            ""installation"": {
                ""id"": 123,
                ""account"": {
                    ""login"": ""test-account""
                }
            }
        }");

        var deliveryResult = WebhookDelivery.Create(
            DeliveryId.Create("delivery-123"),
            WebhookEventName.Create("issues"),
            DateTimeOffset.UtcNow,
            payload,
            installationId);
        var delivery = deliveryResult.Value!;

        var token = InstallationAccessToken.Create("ghs_token123", DateTimeOffset.UtcNow.AddHours(1));
        _installationAuth
            .AuthenticateAsync(Arg.Any<AuthenticateInstallationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<InstallationAccessToken>.Success(token));

        // Act
        await _factory.CreateAsync(delivery, CancellationToken.None);

        // Assert
        await _installationAuth.Received(1).AuthenticateAsync(
            Arg.Is<AuthenticateInstallationCommand>(cmd => cmd.InstallationId.Value == 123),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateLoggerWithEventName()
    {
        // Arrange
        var payload = WebhookPayload.Create("{}");
        var deliveryResult = WebhookDelivery.Create(
            DeliveryId.Create("delivery-123"),
            WebhookEventName.Create("pull_request"),
            DateTimeOffset.UtcNow,
            payload,
            null);
        var delivery = deliveryResult.Value!;

        // Act
        await _factory.CreateAsync(delivery, CancellationToken.None);

        // Assert
        _loggerFactory.Received(1).CreateLogger("ProbotSharp.Event.pull_request");
    }

    [Fact]
    public async Task CreateAsync_WithNullDelivery_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _factory.CreateAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
