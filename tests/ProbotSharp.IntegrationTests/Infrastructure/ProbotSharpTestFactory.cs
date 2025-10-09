// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using ProbotSharp.Adapters.Workers;
using ProbotSharp.Infrastructure.Adapters.Persistence;

namespace ProbotSharp.IntegrationTests.Infrastructure;

/// <summary>
/// Test factory for creating test instances of the ProbotSharp web application.
/// Configures in-memory database and test-specific configuration.
/// </summary>
public class ProbotSharpTestFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName;
    private readonly string _databaseFilePath;

    public ProbotSharpTestFactory()
    {
        _databaseName = $"IntegrationTestDb_{Guid.NewGuid()}";
        _databaseFilePath = Path.Combine(Path.GetTempPath(), $"{_databaseName}.db");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment variable to skip configuration validation in tests
        Environment.SetEnvironmentVariable("SKIP_CONFIG_VALIDATION", "true");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test configuration
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GitHub:WebhookSecret"] = "test-webhook-secret",
                ["ProbotSharp:WebhookSecret"] = "test-webhook-secret",
                ["ProbotSharp:GitHub:AppId"] = "123456",
                ["ProbotSharp:GitHub:WebhookSecret"] = "test-webhook-secret",
                ["ProbotSharp:GitHub:PrivateKey"] = "-----BEGIN RSA PRIVATE KEY-----\ntest-key\n-----END RSA PRIVATE KEY-----",
                ["ProbotSharp:Adapters:ReplayQueue:Provider"] = "InMemory",
                ["ProbotSharp:Persistence:Provider"] = "inmemory",
                ["ProbotSharp:SkipConfigurationValidation"] = "true"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the hosted service (WebhookReplayWorker) to prevent background processing during tests
            services.RemoveAll<Microsoft.Extensions.Hosting.IHostedService>();

            // Replace the DbContext registration with in-memory database for tests
            services.RemoveAll<DbContextOptions<ProbotSharpDbContext>>();
            services.RemoveAll<ProbotSharpDbContext>();

            services.AddDbContext<ProbotSharpDbContext>(options =>
            {
                options.UseInMemoryDatabase($"IntegrationTestDb_{Guid.NewGuid()}");
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // For in-memory database, EnsureCreated is not needed but we do it anyway for consistency
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProbotSharpDbContext>();
        db.Database.EnsureCreated();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                // For in-memory database, no file cleanup is needed
                using var scope = Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ProbotSharpDbContext>();
                db.Database.EnsureDeleted();
            }
            catch (ObjectDisposedException)
            {
                // The service provider has already been disposed; no additional cleanup required.
            }
        }

        base.Dispose(disposing);
    }
}
