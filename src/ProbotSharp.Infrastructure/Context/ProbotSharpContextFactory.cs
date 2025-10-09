// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Octokit;
using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Domain.Context;
using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.Models;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Infrastructure.Context;

/// <summary>
/// Factory implementation for creating ProbotSharpContext instances from webhook deliveries.
/// </summary>
public sealed class ProbotSharpContextFactory : IProbotSharpContextFactory
{
    private readonly IInstallationAuthenticationPort _installationAuth;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEnumerable<IProbotSharpContextConfigurator> _configurators;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProbotSharpContextFactory"/> class.
    /// </summary>
    /// <param name="installationAuth">Installation authentication port.</param>
    /// <param name="loggerFactory">Logger factory for creating scoped loggers.</param>
    /// <param name="httpClientFactory">HTTP client factory for creating GitHub API clients.</param>
    /// <param name="configurators">Context configurators for attaching services to contexts.</param>
    public ProbotSharpContextFactory(
        IInstallationAuthenticationPort installationAuth,
        ILoggerFactory loggerFactory,
        IHttpClientFactory httpClientFactory,
        IEnumerable<IProbotSharpContextConfigurator> configurators)
    {
        _installationAuth = installationAuth ?? throw new ArgumentNullException(nameof(installationAuth));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _configurators = configurators ?? throw new ArgumentNullException(nameof(configurators));
    }

    /// <inheritdoc/>
    public async Task<ProbotSharpContext> CreateAsync(
        WebhookDelivery delivery,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(delivery);

        // Parse the payload as JObject
        var payloadJson = delivery.Payload.RawBody;
        var payload = JObject.Parse(payloadJson);

        // Extract event action if present
        var eventAction = payload["action"]?.Value<string>();

        // Extract repository information
        var repository = ExtractRepositoryInfo(payload);

        // Extract installation information
        var installation = ExtractInstallationInfo(payload);

        // Create scoped logger with event context
        var logger = _loggerFactory.CreateLogger($"ProbotSharp.Event.{delivery.EventName.Value}");
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["DeliveryId"] = delivery.Id.Value,
            ["EventName"] = delivery.EventName.Value,
            ["EventAction"] = eventAction ?? "none",
            ["InstallationId"] = installation?.Id ?? 0,
        }))
        {
            // Check if dry-run mode is enabled via environment variable
            var isDryRun = Environment.GetEnvironmentVariable("PROBOT_DRY_RUN")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
            if (isDryRun)
            {
                logger.LogInformation("Dry-run mode enabled - no changes will be made to GitHub");
            }

            // Create authenticated GitHub client and GraphQL client for the installation
            IGitHubClient gitHubClient;
            Domain.Contracts.IGitHubGraphQlClient graphQLClient;

            if (delivery.InstallationId != null)
            {
                // Authenticate and create installation-scoped client
                var authCommand = new AuthenticateInstallationCommand(delivery.InstallationId);
                var authResult = await _installationAuth.AuthenticateAsync(authCommand, cancellationToken)
                    .ConfigureAwait(false);

                if (!authResult.IsSuccess || authResult.Value == null)
                {
                    var errorMessage = authResult.Error?.Message ?? "Failed to authenticate installation";
                    logger.LogError("Failed to authenticate installation {InstallationId}: {Error}",
                        delivery.InstallationId.Value, errorMessage);
                    throw new InvalidOperationException($"Failed to authenticate installation: {errorMessage}");
                }

                var token = authResult.Value;

                // Create Octokit client with the installation access token
                gitHubClient = new GitHubClient(new ProductHeaderValue("ProbotSharp"))
                {
                    Credentials = new Credentials(token.Value),
                };

                // Create GraphQL client with the installation access token
                graphQLClient = new InstallationAuthenticatedGraphQLClient(_httpClientFactory, token.Value);
            }
            else
            {
                // No installation ID - create an unauthenticated client (limited use)
                logger.LogWarning("No installation ID found in webhook delivery {DeliveryId}, creating unauthenticated client",
                    delivery.Id.Value);
                gitHubClient = new GitHubClient(new ProductHeaderValue("ProbotSharp"));

                // Create unauthenticated GraphQL client (limited use)
                graphQLClient = new InstallationAuthenticatedGraphQLClient(_httpClientFactory, string.Empty);
            }

            // Create the context
            var context = new ProbotSharpContext(
                id: delivery.Id.Value,
                eventName: delivery.EventName.Value,
                eventAction: eventAction,
                payload: payload,
                logger: logger,
                gitHub: gitHubClient,
                graphQL: graphQLClient,
                repository: repository,
                installation: installation,
                isDryRun: isDryRun);

            // Run all configurators to attach services to the context
            foreach (var configurator in _configurators)
            {
                configurator.Configure(context);
            }

            return context;
        }
    }

    private static RepositoryInfo? ExtractRepositoryInfo(JObject payload)
    {
        var repo = payload["repository"];
        if (repo == null)
        {
            return null;
        }

        var id = repo["id"]?.Value<long>();
        var name = repo["name"]?.Value<string>();
        var fullName = repo["full_name"]?.Value<string>();
        var owner = repo["owner"]?["login"]?.Value<string>();

        if (id == null || string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(owner))
        {
            return null;
        }

        return new RepositoryInfo(id.Value, name, owner, fullName);
    }

    private static InstallationInfo? ExtractInstallationInfo(JObject payload)
    {
        var installation = payload["installation"];
        if (installation == null)
        {
            return null;
        }

        var id = installation["id"]?.Value<long>();
        var account = installation["account"]?["login"]?.Value<string>();

        if (id == null || string.IsNullOrWhiteSpace(account))
        {
            return null;
        }

        return new InstallationInfo(id.Value, account);
    }
}
