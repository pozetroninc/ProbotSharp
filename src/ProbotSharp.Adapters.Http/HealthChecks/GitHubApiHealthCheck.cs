// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Diagnostics.HealthChecks;

using ProbotSharp.Application.Ports.Outbound;

namespace ProbotSharp.Adapters.Http.HealthChecks;

public sealed class GitHubApiHealthCheck : IHealthCheck
{
    private readonly IGitHubRestClientPort _gitHubRestClient;

    public GitHubApiHealthCheck(IGitHubRestClientPort gitHubRestClient)
    {
        _gitHubRestClient = gitHubRestClient ?? throw new ArgumentNullException(nameof(gitHubRestClient));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Attempt to call GitHub API meta endpoint (doesn't require authentication)
            var result = await _gitHubRestClient.SendAsync(
                async client =>
                {
                    var response = await client.GetAsync("/meta", cancellationToken).ConfigureAwait(false);
                    return response;
                },
                cancellationToken).ConfigureAwait(false);

            if (result.IsSuccess && result.Value != null)
            {
                var statusCode = (int)result.Value.StatusCode;
                var data = new Dictionary<string, object>
                {
                    { "status_code", statusCode },
                    { "endpoint", "https://api.github.com/meta" },
                    { "response_time_ms", "< 5000" }
                };

                if (result.Value.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy("GitHub API is accessible", data);
                }
                else
                {
                    data["error"] = $"Unexpected status code: {statusCode}";
                    return HealthCheckResult.Degraded("GitHub API returned non-success status", null, data);
                }
            }
            else
            {
                return HealthCheckResult.Unhealthy(
                    "GitHub API is not accessible",
                    null,
                    new Dictionary<string, object>
                    {
                        { "error", result.Error?.Message ?? "Unknown error" },
                        { "endpoint", "https://api.github.com/meta" }
                    });
            }
        }
        catch (Exception ex)
        {
            // CA1031: Catching general exception is intentional here for health check reporting
            return HealthCheckResult.Unhealthy(
                "GitHub API connectivity check failed",
                ex,
                new Dictionary<string, object>
                {
                    { "error", ex.Message },
                    { "endpoint", "https://api.github.com/meta" }
                });
        }
    }
}
