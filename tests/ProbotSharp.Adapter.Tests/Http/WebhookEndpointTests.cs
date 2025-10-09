// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using NSubstitute;

using ProbotSharp.Adapters.Http.Webhooks;
using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Domain.Services;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Adapter.Tests.Http;

public class WebhookEndpointTests
{
    private const string TestSecret = "test-webhook-secret";
    private readonly IConfiguration _configuration;
    private readonly WebhookSignatureValidator _signatureValidator;
    private readonly ILogger _logger;

    public WebhookEndpointTests()
    {
        var configurationData = new Dictionary<string, string?>
        {
            { "WebhookSecret", TestSecret }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();
        _signatureValidator = new WebhookSignatureValidator();
        _logger = Substitute.For<ILogger>();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAccepted_WhenProcessingSucceeds()
    {
        var payloadBody = "{\"installation\":{\"id\":1},\"foo\":\"bar\"}";
        var context = CreateContext("delivery", "push", payloadBody);
        var processingPort = Substitute.For<IWebhookProcessingPort>();
        processingPort.ProcessAsync(Arg.Any<ProcessWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        await WebhookEndpoint.HandleAsync(context, processingPort, _configuration, _signatureValidator, _logger);

        context.Response.StatusCode.Should().Be(StatusCodes.Status202Accepted);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnError_WhenProcessingFails()
    {
        var payloadBody = "{\"installation\":{\"id\":1},\"foo\":\"bar\"}";
        var context = CreateContext("delivery", "push", payloadBody);
        var processingPort = Substitute.For<IWebhookProcessingPort>();
        processingPort.ProcessAsync(Arg.Any<ProcessWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure("error", "failed"));

        await WebhookEndpoint.HandleAsync(context, processingPort, _configuration, _signatureValidator, _logger);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnBadRequest_WhenHeadersMissing()
    {
        var context = CreateContext(null, "push", "{}", includeSignature: false);
        var processingPort = Substitute.For<IWebhookProcessingPort>();

        await WebhookEndpoint.HandleAsync(context, processingPort, _configuration, _signatureValidator, _logger);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUnauthorized_WhenSignatureIsInvalid()
    {
        var payloadBody = "{\"installation\":{\"id\":1},\"foo\":\"bar\"}";
        var context = CreateContext("delivery", "push", payloadBody, useInvalidSignature: true);
        var processingPort = Substitute.For<IWebhookProcessingPort>();

        await WebhookEndpoint.HandleAsync(context, processingPort, _configuration, _signatureValidator, _logger);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        await processingPort.DidNotReceive().ProcessAsync(Arg.Any<ProcessWebhookCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnInternalServerError_WhenWebhookSecretMissing()
    {
        var emptyConfig = new ConfigurationBuilder().Build();
        var payloadBody = "{\"installation\":{\"id\":1},\"foo\":\"bar\"}";
        var context = CreateContext("delivery", "push", payloadBody);
        var processingPort = Substitute.For<IWebhookProcessingPort>();

        await WebhookEndpoint.HandleAsync(context, processingPort, emptyConfig, _signatureValidator, _logger);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        await processingPort.DidNotReceive().ProcessAsync(Arg.Any<ProcessWebhookCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldLogWarning_WhenSignatureIsInvalid()
    {
        var payloadBody = "{\"installation\":{\"id\":1},\"foo\":\"bar\"}";
        var context = CreateContext("delivery", "push", payloadBody, useInvalidSignature: true);
        var processingPort = Substitute.For<IWebhookProcessingPort>();

        await WebhookEndpoint.HandleAsync(context, processingPort, _configuration, _signatureValidator, _logger);

        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    private static DefaultHttpContext CreateContext(string? deliveryId, string? eventName, string payloadBody, bool includeSignature = true, bool useInvalidSignature = false)
    {
        var context = new DefaultHttpContext();
        if (deliveryId is not null)
        {
            context.Request.Headers["x-github-delivery"] = deliveryId;
        }

        if (eventName is not null)
        {
            context.Request.Headers["x-github-event"] = eventName;
        }

        if (includeSignature)
        {
            context.Request.Headers["x-hub-signature-256"] = useInvalidSignature
                ? "sha256=" + new string('0', 64)
                : GenerateSignature(payloadBody, TestSecret);
        }

        var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, leaveOpen: true);
        writer.Write(payloadBody);
        writer.Flush();
        stream.Position = 0;
        context.Request.Body = stream;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static string GenerateSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}
