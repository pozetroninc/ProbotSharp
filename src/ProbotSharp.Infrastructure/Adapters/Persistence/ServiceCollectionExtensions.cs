// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ProbotSharp.Application.Ports.Outbound;

namespace ProbotSharp.Infrastructure.Adapters.Persistence;

/// <summary>
/// Dependency injection extensions for registering persistence adapters.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Entity Framework persistence adapters and database context.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPersistenceAdapters(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var persistenceSection = configuration.GetSection("ProbotSharp:Persistence");
        var configuredProvider = persistenceSection["Provider"];
        var configuredConnectionString = persistenceSection["ConnectionString"];

        var connectionString = configuration.GetConnectionString("ProbotSharp")
            ?? configuration["PROBOTSHARP_STORAGE_CONNECTION"]
            ?? configuredConnectionString;

        services.AddDbContext<ProbotSharpDbContext>((serviceProvider, options) =>
        {
            var provider = configuredProvider;

            if (string.IsNullOrWhiteSpace(provider))
            {
                provider = string.IsNullOrWhiteSpace(connectionString)
                    ? "inmemory"
                    : "postgres";
            }

            if (string.Equals(provider, "sqlite", StringComparison.OrdinalIgnoreCase))
            {
                var sqliteConnectionString = string.IsNullOrWhiteSpace(connectionString)
                    ? "DataSource=probotsharp.sqlite"
                    : connectionString;

                options.UseSqlite(sqliteConnectionString);
                return;
            }

            if (string.Equals(provider, "postgres", StringComparison.OrdinalIgnoreCase)
                || string.Equals(provider, "npgsql", StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__efmigrationshistory", "probot");
                });
                return;
            }

            if (string.Equals(provider, "inmemory", StringComparison.OrdinalIgnoreCase))
            {
                options.UseInMemoryDatabase("probot-sharp");
                return;
            }

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__efmigrationshistory", "probot");
                });
                return;
            }

            options.UseInMemoryDatabase("probot-sharp");
        });

        services.AddScoped<IWebhookStoragePort, EfWebhookStorageAdapter>();
        services.AddScoped<IUnitOfWorkPort, EfUnitOfWork>();
        services.AddScoped<IMetadataPort, ProbotSharp.Infrastructure.Adapters.Metadata.PostgresMetadataAdapter>();

        return services;
    }
}

