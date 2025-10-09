// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Domain.Services;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Adapters.Http.Webhooks;

public static class WebhookEndpoint
{
    public static async Task HandleAsync(
        HttpContext context,
        IWebhookProcessingPort processingPort,
        IConfiguration configuration,
        WebhookSignatureValidator signatureValidator,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(processingPort);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(signatureValidator);
        ArgumentNullException.ThrowIfNull(logger);

        if (!context.Request.Headers.TryGetValue("x-github-delivery", out var deliveryHeader) || string.IsNullOrWhiteSpace(deliveryHeader))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteErrorAsync(context, "missing_delivery", "Missing x-github-delivery header.").ConfigureAwait(false);
            return;
        }

        if (!context.Request.Headers.TryGetValue("x-github-event", out var eventHeader) || string.IsNullOrWhiteSpace(eventHeader))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteErrorAsync(context, "missing_event", "Missing x-github-event header.").ConfigureAwait(false);
            return;
        }

        if (!context.Request.Headers.TryGetValue("x-hub-signature-256", out var signatureHeader) || string.IsNullOrWhiteSpace(signatureHeader))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteErrorAsync(context, "missing_signature", "Missing x-hub-signature-256 header.").ConfigureAwait(false);
            return;
        }

        var deliveryId = DeliveryId.Create(deliveryHeader!);
        var eventName = WebhookEventName.Create(eventHeader!);
        var signature = WebhookSignature.Create(signatureHeader!);

        using var requestBody = await CloneRequestBodyAsync(context.RequestAborted, context.Request.Body).ConfigureAwait(false);
        using var reader = new StreamReader(requestBody, leaveOpen: true);
        var payloadBody = await reader.ReadToEndAsync(context.RequestAborted).ConfigureAwait(false);
        requestBody.Position = 0;

        // Validate webhook signature before processing
        var webhookSecret = configuration["GitHub:WebhookSecret"]
            ?? configuration["ProbotSharp:WebhookSecret"]
            ?? configuration["WebhookSecret"]
            ?? Environment.GetEnvironmentVariable("PROBOTSHARP_WEBHOOK_SECRET")
            ?? Environment.GetEnvironmentVariable("WEBHOOK_SECRET");

        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            logger.LogError("Webhook secret is not configured. Unable to validate signature for delivery {DeliveryId}", deliveryId.Value);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await WriteErrorAsync(context, "webhook_secret_missing", "Webhook secret is not configured.").ConfigureAwait(false);
            return;
        }

        var isSignatureValid = signatureValidator.IsSignatureValid(payloadBody, webhookSecret, signature.Value);
        if (!isSignatureValid)
        {
            logger.LogWarning("Invalid webhook signature for delivery {DeliveryId} from {RemoteIp}", deliveryId.Value, context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await WriteErrorAsync(context, "invalid_signature", "Webhook signature validation failed.").ConfigureAwait(false);
            return;
        }

        var payload = WebhookPayload.Create(payloadBody);
        InstallationId? installationId = null;
        if (payload.RootElement.TryGetProperty("installation", out var installationElement) && installationElement.TryGetProperty("id", out var idElement))
        {
            installationId = InstallationId.Create(idElement.GetInt64());
        }

        var command = new ProcessWebhookCommand(deliveryId, eventName, payload, installationId, signature, payloadBody);
        var result = await processingPort.ProcessAsync(command, context.RequestAborted).ConfigureAwait(false);

        context.Response.StatusCode = result.IsSuccess ? StatusCodes.Status202Accepted : StatusCodes.Status500InternalServerError;

        if (!result.IsSuccess && result.Error is not null)
        {
            await WriteErrorAsync(context, result.Error.Value.Code, result.Error.Value.Message, result.Error.Value.Details).ConfigureAwait(false);
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, string code, string message, string? details = null)
    {
        var payload = JsonSerializer.Serialize(new { error = code, message, details });
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(payload, context.RequestAborted).ConfigureAwait(false);
    }

    private static async Task<Stream> CloneRequestBodyAsync(CancellationToken cancellationToken, Stream body)
    {
        if (!body.CanSeek)
        {
            var memoryStream = new MemoryStream();
            await body.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            memoryStream.Position = 0;
            return memoryStream;
        }

        body.Position = 0;
        return body;
    }
}

