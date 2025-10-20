// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using ProbotSharp.Infrastructure.Adapters.Persistence;

namespace ProbotSharp.Adapters.Http.HealthChecks;

/// <summary>
/// Health check for database connectivity and availability.
/// </summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly ProbotSharpDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseHealthCheck"/> class.
    /// </summary>
    /// <param name="dbContext">The database context to check.</param>
    public DatabaseHealthCheck(ProbotSharpDbContext dbContext)
    {
        this._dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>
    /// Checks the health of the database connection.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The health check result.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Attempt to connect to the database and execute a simple query
            await this._dbContext.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false);

            var providerName = this._dbContext.Database.ProviderName ?? "Unknown";
            var data = new Dictionary<string, object>
            {
                { "database_provider", providerName }
            };

            if (this._dbContext.Database.IsRelational())
            {
                data.Add("connection_string", MaskConnectionString(this._dbContext.Database.GetConnectionString()));
            }
            else
            {
                data.Add("connection_string", "Not applicable for non-relational provider");
            }

            return HealthCheckResult.Healthy("Database is accessible", data);
        }
        catch (Exception ex)
        {
            // CA1031: Catching general exception is intentional here for health check reporting
            return HealthCheckResult.Unhealthy(
                "Database is not accessible",
                ex,
                new Dictionary<string, object>
                {
                    { "error", ex.Message }
                });
        }
    }

    private static string MaskConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return "Not configured";
        }

        // Mask sensitive information in connection string
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var maskedParts = parts.Select(part =>
        {
            var keyValue = part.Split('=', 2);
            if (keyValue.Length != 2)
            {
                return part;
            }

            var key = keyValue[0].Trim().ToLowerInvariant();
            if (key.Contains("password") || key.Contains("pwd") || key.Contains("secret"))
            {
                return $"{keyValue[0]}=***";
            }

            return part;
        });

        return string.Join("; ", maskedParts);
    }
}
