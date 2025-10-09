// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Infrastructure.Adapters.Workers;

/// <summary>
/// Generates local development webhook proxy URLs without contacting external services.
/// </summary>
public sealed class LocalWebhookChannelAdapter : IWebhookChannelPort
{
    private readonly ILogger<LocalWebhookChannelAdapter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalWebhookChannelAdapter"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public LocalWebhookChannelAdapter(ILogger<LocalWebhookChannelAdapter> logger)
    {
        this._logger = logger;
    }

    /// <inheritdoc />
    public Task<Result<CreateWebhookChannelResponse>> CreateChannelAsync(CancellationToken cancellationToken = default)
    {
        var token = Guid.NewGuid().ToString("n");
        var url = $"https://smee.io/{token}";

#pragma warning disable CA1848 // Use LoggerMessage delegates for high-performance logging
        this._logger.LogInformation("Generated local webhook channel {WebhookProxyUrl}", url);
#pragma warning restore CA1848

        var response = new CreateWebhookChannelResponse(url, DateTimeOffset.UtcNow);
        return Task.FromResult(Result<CreateWebhookChannelResponse>.Success(response));
    }
}

