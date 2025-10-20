// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Infrastructure.Adapters.Configuration;

/// <summary>
/// Retrieves application configuration values (e.g., webhook secrets) from <see cref="IConfiguration"/>.
/// </summary>
public sealed class ConfigurationAppConfigurationAdapter : IAppConfigurationPort
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationAppConfigurationAdapter"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    public ConfigurationAppConfigurationAdapter(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        this._configuration = configuration;
    }

    /// <inheritdoc />
    public Task<Result<string>> GetWebhookSecretAsync(CancellationToken cancellationToken = default)
    {
        var secret = this._configuration["GitHub:WebhookSecret"]
            ?? this._configuration["ProbotSharp:WebhookSecret"]
            ?? this._configuration["WebhookSecret"]
            ?? Environment.GetEnvironmentVariable("PROBOTSHARP_WEBHOOK_SECRET")
            ?? Environment.GetEnvironmentVariable("WEBHOOK_SECRET");

        if (string.IsNullOrWhiteSpace(secret))
        {
            return Task.FromResult(Result<string>.Failure(
                "webhook_secret_missing",
                "Webhook secret configuration is missing."));
        }

        return Task.FromResult(Result<string>.Success(secret));
    }
}
