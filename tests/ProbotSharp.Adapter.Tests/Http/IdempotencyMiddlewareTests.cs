// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using NSubstitute;

using ProbotSharp.Adapters.Http.Middleware;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.ValueObjects;

using Xunit;

namespace ProbotSharp.Adapter.Tests.Http;

/// <summary>
/// Unit tests for <see cref="IdempotencyMiddleware"/>.
/// </summary>
public sealed class IdempotencyMiddlewareTests
{
    private readonly IIdempotencyPort _idempotencyPort;
    private readonly ILogger<IdempotencyMiddleware> _logger;
    private readonly RequestDelegate _next;
    private readonly IdempotencyMiddleware _middleware;

    public IdempotencyMiddlewareTests()
    {
        _idempotencyPort = Substitute.For<IIdempotencyPort>();
        _logger = Substitute.For<ILogger<IdempotencyMiddleware>>();
        _next = Substitute.For<RequestDelegate>();
        _middleware = new IdempotencyMiddleware(_next, _logger);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenNotWebhookRequest()
    {
        // Arrange
        var context = CreateHttpContext("/api/health");

        // Act
        await _middleware.InvokeAsync(context, _idempotencyPort);

        // Assert
        await _next.Received(1).Invoke(context);
        await _idempotencyPort.DidNotReceive().TryAcquireAsync(
            Arg.Any<IdempotencyKey>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenDeliveryHeaderMissing()
    {
        // Arrange
        var context = CreateHttpContext("/webhooks");
        // No X-GitHub-Delivery header

        // Act
        await _middleware.InvokeAsync(context, _idempotencyPort);

        // Assert
        await _next.Received(1).Invoke(context);
        await _idempotencyPort.DidNotReceive().TryAcquireAsync(
            Arg.Any<IdempotencyKey>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenIdempotencyLockAcquired()
    {
        // Arrange
        var deliveryId = "test-delivery-123";
        var context = CreateHttpContext("/webhooks", deliveryId);

        _idempotencyPort.TryAcquireAsync(
            Arg.Any<IdempotencyKey>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _middleware.InvokeAsync(context, _idempotencyPort);

        // Assert
        await _next.Received(1).Invoke(context);
        await _idempotencyPort.Received(1).TryAcquireAsync(
            Arg.Is<IdempotencyKey>(k => k.Value == deliveryId),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn202_WhenDuplicateDetected()
    {
        // Arrange
        var deliveryId = "duplicate-delivery-456";
        var context = CreateHttpContext("/webhooks", deliveryId);

        _idempotencyPort.TryAcquireAsync(
            Arg.Any<IdempotencyKey>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>())
            .Returns(false); // Lock not acquired = duplicate

        // Act
        await _middleware.InvokeAsync(context, _idempotencyPort);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status202Accepted);
        await _next.DidNotReceive().Invoke(context);
    }

    [Fact]
    public async Task InvokeAsync_ShouldWriteMessage_WhenDuplicateDetected()
    {
        // Arrange
        var deliveryId = "duplicate-delivery-789";
        var context = CreateHttpContext("/webhooks", deliveryId);
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        _idempotencyPort.TryAcquireAsync(
            Arg.Any<IdempotencyKey>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        await _middleware.InvokeAsync(context, _idempotencyPort);

        // Assert
        responseBody.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(responseBody);
        var content = await reader.ReadToEndAsync();
        content.Should().Contain(deliveryId);
        content.Should().Contain("already been processed");
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenIdempotencyCheckThrows()
    {
        // Arrange
        var deliveryId = "error-delivery-999";
        var context = CreateHttpContext("/webhooks", deliveryId);

        _idempotencyPort.TryAcquireAsync(
            Arg.Any<IdempotencyKey>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new InvalidOperationException("Database error")));

        // Act
        await _middleware.InvokeAsync(context, _idempotencyPort);

        // Assert - Middleware should fail open and allow the request
        await _next.Received(1).Invoke(context);
    }

    [Theory]
    [InlineData("/webhooks")]
    [InlineData("/api/webhooks")]
    [InlineData("/Webhooks")]
    [InlineData("/API/WEBHOOKS")]
    public async Task InvokeAsync_ShouldApplyIdempotency_ForWebhookPaths(string path)
    {
        // Arrange
        var deliveryId = "test-delivery-path";
        var context = CreateHttpContext(path, deliveryId);

        _idempotencyPort.TryAcquireAsync(
            Arg.Any<IdempotencyKey>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _middleware.InvokeAsync(context, _idempotencyPort);

        // Assert
        await _idempotencyPort.Received(1).TryAcquireAsync(
            Arg.Any<IdempotencyKey>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_ShouldUse24HourTtl()
    {
        // Arrange
        var deliveryId = "ttl-test-delivery";
        var context = CreateHttpContext("/webhooks", deliveryId);

        _idempotencyPort.TryAcquireAsync(
            Arg.Any<IdempotencyKey>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _middleware.InvokeAsync(context, _idempotencyPort);

        // Assert
        await _idempotencyPort.Received(1).TryAcquireAsync(
            Arg.Any<IdempotencyKey>(),
            Arg.Is<TimeSpan?>(ttl => ttl == TimeSpan.FromHours(24)),
            Arg.Any<CancellationToken>());
    }

    private static HttpContext CreateHttpContext(string path, string? deliveryId = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = "POST";
        context.Response.Body = new MemoryStream();

        if (deliveryId != null)
        {
            context.Request.Headers["X-GitHub-Delivery"] = deliveryId;
        }

        return context;
    }
}
