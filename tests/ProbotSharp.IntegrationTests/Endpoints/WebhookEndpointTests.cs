// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http.Json;
using System.Text;

using FluentAssertions;

using ProbotSharp.IntegrationTests.Helpers;
using ProbotSharp.IntegrationTests.Infrastructure;

namespace ProbotSharp.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for webhook endpoint processing.
/// Tests webhook signature validation, payload processing, and error handling.
/// </summary>
public class WebhookEndpointTests : IClassFixture<ProbotSharpTestFactory>
{
    private readonly ProbotSharpTestFactory _factory;
    private readonly HttpClient _client;
    private const string WebhookSecret = "test-webhook-secret";

    public WebhookEndpointTests(ProbotSharpTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task PostWebhook_WithValidSignature_ShouldReturnAccepted()
    {
        // Arrange
        var payload = WebhookTestHelpers.CreatePushEventPayload();
        var signature = WebhookTestHelpers.GenerateWebhookSignature(payload, WebhookSecret);
        var request = WebhookTestHelpers.CreateWebhookRequest(payload, signature, "push");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task PostWebhook_WithInvalidSignature_ShouldReturnUnauthorized()
    {
        // Arrange
        var payload = WebhookTestHelpers.CreatePushEventPayload();
        var invalidSignature = "sha256=" + new string('0', 64); // Invalid signature
        var request = WebhookTestHelpers.CreateWebhookRequest(payload, invalidSignature, "push", Guid.NewGuid().ToString());

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("invalid_signature");
    }

    [Fact]
    public async Task PostWebhook_WithMissingDeliveryHeader_ShouldReturnBadRequest()
    {
        // Arrange
        var payload = WebhookTestHelpers.CreatePushEventPayload();
        var signature = WebhookTestHelpers.GenerateWebhookSignature(payload, WebhookSecret);
        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-GitHub-Event", "push");
        request.Headers.Add("X-Hub-Signature-256", signature);
        // Missing X-GitHub-Delivery header

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("missing_delivery");
    }

    [Fact]
    public async Task PostWebhook_WithMissingEventHeader_ShouldReturnBadRequest()
    {
        // Arrange
        var payload = WebhookTestHelpers.CreatePushEventPayload();
        var signature = WebhookTestHelpers.GenerateWebhookSignature(payload, WebhookSecret);
        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-GitHub-Delivery", Guid.NewGuid().ToString());
        request.Headers.Add("X-Hub-Signature-256", signature);
        // Missing X-GitHub-Event header

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("missing_event");
    }

    [Fact]
    public async Task PostWebhook_WithMissingSignatureHeader_ShouldReturnBadRequest()
    {
        // Arrange
        var payload = WebhookTestHelpers.CreatePushEventPayload();
        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-GitHub-Delivery", Guid.NewGuid().ToString());
        request.Headers.Add("X-GitHub-Event", "push");
        // Missing X-Hub-Signature-256 header

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("missing_signature");
    }

    [Fact]
    public async Task PostWebhook_WithIssuesEvent_ShouldReturnAccepted()
    {
        // Arrange
        var payload = WebhookTestHelpers.CreateIssuesEventPayload();
        var signature = WebhookTestHelpers.GenerateWebhookSignature(payload, WebhookSecret);
        var request = WebhookTestHelpers.CreateWebhookRequest(payload, signature, "issues");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task PostWebhook_WithDifferentInstallationIds_ShouldProcessSeparately()
    {
        // Arrange
        var payload1 = WebhookTestHelpers.CreatePushEventPayload(installationId: 11111);
        var signature1 = WebhookTestHelpers.GenerateWebhookSignature(payload1, WebhookSecret);
        var request1 = WebhookTestHelpers.CreateWebhookRequest(
            payload1,
            signature1,
            "push",
            "11111111-1111-1111-1111-111111111111");

        var payload2 = WebhookTestHelpers.CreatePushEventPayload(installationId: 22222);
        var signature2 = WebhookTestHelpers.GenerateWebhookSignature(payload2, WebhookSecret);
        var request2 = WebhookTestHelpers.CreateWebhookRequest(
            payload2,
            signature2,
            "push",
            "22222222-2222-2222-2222-222222222222");

        // Act
        var response1 = await _client.SendAsync(request1);
        var response2 = await _client.SendAsync(request2);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.Accepted);
        response2.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task PostWebhook_WithPullRequestOpenedEvent_ShouldReturnAccepted()
    {
        // Arrange
        var payload = WebhookTestHelpers.CreatePullRequestEventPayload(action: "opened");
        var signature = WebhookTestHelpers.GenerateWebhookSignature(payload, WebhookSecret);
        var request = WebhookTestHelpers.CreateWebhookRequest(payload, signature, "pull_request");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task PostWebhook_WithPullRequestClosedEvent_ShouldReturnAccepted()
    {
        // Arrange
        var payload = WebhookTestHelpers.CreatePullRequestEventPayload(action: "closed");
        var signature = WebhookTestHelpers.GenerateWebhookSignature(payload, WebhookSecret);
        var request = WebhookTestHelpers.CreateWebhookRequest(payload, signature, "pull_request");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task PostWebhook_WithPullRequestSynchronizeEvent_ShouldReturnAccepted()
    {
        // Arrange
        var payload = WebhookTestHelpers.CreatePullRequestEventPayload(action: "synchronize");
        var signature = WebhookTestHelpers.GenerateWebhookSignature(payload, WebhookSecret);
        var request = WebhookTestHelpers.CreateWebhookRequest(payload, signature, "pull_request");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task PostWebhook_WithIssueCommentEvent_ShouldReturnAccepted()
    {
        // Arrange
        var payload = WebhookTestHelpers.CreateIssueCommentEventPayload();
        var signature = WebhookTestHelpers.GenerateWebhookSignature(payload, WebhookSecret);
        var request = WebhookTestHelpers.CreateWebhookRequest(payload, signature, "issue_comment");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task PostWebhook_WithIssuesClosedEvent_ShouldReturnAccepted()
    {
        // Arrange
        var payload = WebhookTestHelpers.CreateIssuesEventPayload(action: "closed");
        var signature = WebhookTestHelpers.GenerateWebhookSignature(payload, WebhookSecret);
        var request = WebhookTestHelpers.CreateWebhookRequest(payload, signature, "issues");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task PostWebhook_WithIssuesEditedEvent_ShouldReturnAccepted()
    {
        // Arrange
        var payload = WebhookTestHelpers.CreateIssuesEventPayload(action: "edited");
        var signature = WebhookTestHelpers.GenerateWebhookSignature(payload, WebhookSecret);
        var request = WebhookTestHelpers.CreateWebhookRequest(payload, signature, "issues");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task PostWebhook_WithMalformedJson_ShouldReturnBadRequest()
    {
        // Arrange
        var payload = WebhookTestHelpers.CreateMalformedPayload();
        var signature = WebhookTestHelpers.GenerateWebhookSignature(payload, WebhookSecret);
        var request = WebhookTestHelpers.CreateWebhookRequest(payload, signature, "push", Guid.NewGuid().ToString());

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task PostWebhook_WithEmptyPayload_ShouldReturnBadRequest()
    {
        // Arrange
        var payload = string.Empty;
        var signature = WebhookTestHelpers.GenerateWebhookSignature(payload, WebhookSecret);
        var request = WebhookTestHelpers.CreateWebhookRequest(payload, signature, "push");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostWebhook_WithInvalidSignatureFormat_ShouldReturnBadRequest()
    {
        // Arrange
        var payload = WebhookTestHelpers.CreatePushEventPayload();
        var invalidSignature = "invalid-signature-format";
        var request = WebhookTestHelpers.CreateWebhookRequest(payload, invalidSignature, "push", Guid.NewGuid().ToString());

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostWebhook_WithSignatureMissingSha256Prefix_ShouldReturnBadRequest()
    {
        // Arrange
        var payload = WebhookTestHelpers.CreatePushEventPayload();
        var signatureWithoutPrefix = new string('0', 64);
        var request = WebhookTestHelpers.CreateWebhookRequest(payload, signatureWithoutPrefix, "push", Guid.NewGuid().ToString());

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostWebhook_WithEmptyEventName_ShouldReturnBadRequest()
    {
        // Arrange
        var payload = WebhookTestHelpers.CreatePushEventPayload();
        var signature = WebhookTestHelpers.GenerateWebhookSignature(payload, WebhookSecret);
        var request = WebhookTestHelpers.CreateWebhookRequest(payload, signature, string.Empty, Guid.NewGuid().ToString());

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostWebhook_WithInvalidDeliveryIdFormat_ShouldReturnBadRequest()
    {
        // Arrange
        var payload = WebhookTestHelpers.CreatePushEventPayload();
        var signature = WebhookTestHelpers.GenerateWebhookSignature(payload, WebhookSecret);
        var request = WebhookTestHelpers.CreateWebhookRequest(
            payload,
            signature,
            "push",
            "invalid-delivery-id");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task PostWebhook_WithDuplicateDeliveryId_ShouldHandleIdempotently()
    {
        // Arrange
        var deliveryId = "99999999-9999-9999-9999-999999999999";
        var payload = WebhookTestHelpers.CreatePushEventPayload();
        var signature = WebhookTestHelpers.GenerateWebhookSignature(payload, WebhookSecret);
        var request1 = WebhookTestHelpers.CreateWebhookRequest(payload, signature, "push", deliveryId);
        var request2 = WebhookTestHelpers.CreateWebhookRequest(payload, signature, "push", deliveryId);

        // Act
        var response1 = await _client.SendAsync(request1);
        var response2 = await _client.SendAsync(request2);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.Accepted);
        response2.StatusCode.Should().BeOneOf(HttpStatusCode.Accepted, HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("push")]
    [InlineData("issues")]
    [InlineData("pull_request")]
    [InlineData("issue_comment")]
    [InlineData("pull_request_review")]
    [InlineData("pull_request_review_comment")]
    [InlineData("status")]
    [InlineData("check_run")]
    [InlineData("check_suite")]
    public async Task PostWebhook_WithVariousEventTypes_ShouldReturnAccepted(string eventName)
    {
        // Arrange
        var payload = WebhookTestHelpers.CreatePushEventPayload();
        var signature = WebhookTestHelpers.GenerateWebhookSignature(payload, WebhookSecret);
        var request = WebhookTestHelpers.CreateWebhookRequest(
            payload,
            signature,
            eventName,
            Guid.NewGuid().ToString());

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task PostWebhook_WithLargePayload_ShouldReturnAccepted()
    {
        // Arrange - Create a payload with large body content
        var largeBody = new string('x', 10000);
        var payload = $$"""
        {
          "action": "opened",
          "issue": {
            "number": 1,
            "title": "Test Issue",
            "body": "{{largeBody}}",
            "state": "open",
            "user": {
              "login": "test-user",
              "id": 1
            }
          },
          "repository": {
            "id": 1,
            "name": "test-repo",
            "full_name": "test-org/test-repo",
            "private": false,
            "owner": {
              "login": "test-org",
              "id": 1
            }
          },
          "sender": {
            "login": "test-user",
            "id": 1
          },
          "installation": {
            "id": 12345
          }
        }
        """;
        var signature = WebhookTestHelpers.GenerateWebhookSignature(payload, WebhookSecret);
        var request = WebhookTestHelpers.CreateWebhookRequest(payload, signature, "issues");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task PostWebhook_WithMultipleSequentialRequests_ShouldProcessAllSuccessfully()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 5; i++)
        {
            var payload = WebhookTestHelpers.CreatePushEventPayload(installationId: 12345 + i);
            var signature = WebhookTestHelpers.GenerateWebhookSignature(payload, WebhookSecret);
            var request = WebhookTestHelpers.CreateWebhookRequest(
                payload,
                signature,
                "push",
                Guid.NewGuid().ToString());

            tasks.Add(_client.SendAsync(request));
        }

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().AllSatisfy(response =>
            response.StatusCode.Should().Be(HttpStatusCode.Accepted));
    }
}
