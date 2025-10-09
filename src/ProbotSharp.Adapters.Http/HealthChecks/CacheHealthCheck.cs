// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Diagnostics.HealthChecks;

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Adapters.Http.HealthChecks;

public sealed class CacheHealthCheck : IHealthCheck
{
    private readonly IAccessTokenCachePort _cachePort;

    public CacheHealthCheck(IAccessTokenCachePort cachePort)
    {
        _cachePort = cachePort ?? throw new ArgumentNullException(nameof(cachePort));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test cache by attempting to get a non-existent key
            var testInstallationId = InstallationId.Create(999999999);
            var result = await _cachePort.GetAsync(testInstallationId, cancellationToken).ConfigureAwait(false);

            // If we get here without exception, cache is operational
            var data = new Dictionary<string, object>
            {
                { "cache_type", _cachePort.GetType().Name },
                { "test_result", "Cache is accessible and responding" }
            };

            return HealthCheckResult.Healthy("Cache is operational", data);
        }
        catch (Exception ex)
        {
            // CA1031: Catching general exception is intentional here for health check reporting
            return HealthCheckResult.Unhealthy(
                "Cache is not operational",
                ex,
                new Dictionary<string, object>
                {
                    { "error", ex.Message },
                    { "cache_type", _cachePort.GetType().Name }
                });
        }
    }
}
