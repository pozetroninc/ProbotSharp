// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;

namespace ProbotSharp.Bootstrap.Api;

/// <summary>
/// Validates application configuration on startup to fail fast if required settings are missing.
/// </summary>
public static class ConfigurationValidation
{
    /// <summary>
    /// Validates that all required configuration values are present.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <exception cref="InvalidOperationException">Thrown when required configuration is missing.</exception>
    public static void ValidateRequiredConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // Skip validation if explicitly disabled (useful for tests)
        if (configuration.GetValue<bool>("ProbotSharp:SkipConfigurationValidation"))
        {
            return;
        }

        var errors = new List<string>();

        // GitHub configuration
        ValidateRequired(configuration, "ProbotSharp:AppId", errors);
        ValidateRequired(configuration, "ProbotSharp:WebhookSecret", errors);

        // Private key - either path or direct value
        var privateKeyPath = configuration["ProbotSharp:PrivateKeyPath"];
        var privateKey = configuration["ProbotSharp:PrivateKey"];
        if (string.IsNullOrWhiteSpace(privateKeyPath) && string.IsNullOrWhiteSpace(privateKey))
        {
            errors.Add("ProbotSharp:PrivateKeyPath or ProbotSharp:PrivateKey");
        }

        // Database configuration (if using PostgreSQL or SQLite persistence)
        var persistenceProvider = configuration["ProbotSharp:Adapters:Persistence:Provider"];
        if (persistenceProvider?.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) == true ||
            persistenceProvider?.Equals("SQLite", StringComparison.OrdinalIgnoreCase) == true)
        {
            ValidateRequired(configuration, "ConnectionStrings:ProbotSharp", errors);
        }

        // Redis configuration (if using Redis for cache or idempotency)
        var cacheProvider = configuration["ProbotSharp:Adapters:Cache:Provider"];
        if (cacheProvider?.Equals("Redis", StringComparison.OrdinalIgnoreCase) == true)
        {
            ValidateRequired(configuration, "ProbotSharp:Adapters:Cache:Options:ConnectionString", errors);
        }

        var idempotencyProvider = configuration["ProbotSharp:Adapters:Idempotency:Provider"];
        if (idempotencyProvider?.Equals("Redis", StringComparison.OrdinalIgnoreCase) == true)
        {
            ValidateRequired(configuration, "ProbotSharp:Adapters:Idempotency:Options:ConnectionString", errors);
        }

        // OpenTelemetry configuration (if enabled)
        var metricsProvider = configuration["ProbotSharp:Adapters:Metrics:Provider"];
        if (metricsProvider?.Equals("OpenTelemetry", StringComparison.OrdinalIgnoreCase) == true)
        {
            ValidateRequired(configuration, "ProbotSharp:Adapters:Metrics:Options:OtlpEndpoint", errors);
        }

        var tracingProvider = configuration["ProbotSharp:Adapters:Tracing:Provider"];
        if (tracingProvider?.Equals("OpenTelemetry", StringComparison.OrdinalIgnoreCase) == true)
        {
            ValidateRequired(configuration, "ProbotSharp:Adapters:Tracing:Options:OtlpEndpoint", errors);
        }

        // ReplayQueue configuration (if using FileSystem provider)
        var replayQueueProvider = configuration["ProbotSharp:Adapters:ReplayQueue:Provider"];
        if (replayQueueProvider?.Equals("FileSystem", StringComparison.OrdinalIgnoreCase) == true)
        {
            ValidateRequired(configuration, "ProbotSharp:Adapters:ReplayQueue:Options:Path", errors);
        }

        // DeadLetterQueue configuration (if using FileSystem provider)
        var dlqProvider = configuration["ProbotSharp:Adapters:DeadLetterQueue:Provider"];
        if (dlqProvider?.Equals("FileSystem", StringComparison.OrdinalIgnoreCase) == true)
        {
            ValidateRequired(configuration, "ProbotSharp:Adapters:DeadLetterQueue:Options:Path", errors);
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Missing required configuration values: {string.Join(", ", errors)}");
        }
    }

    private static void ValidateRequired(IConfiguration configuration, string key, List<string> errors)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(key);
        }
    }
}
