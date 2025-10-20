// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;

using ProbotSharp.Infrastructure.Adapters.Configuration;

namespace ProbotSharp.Infrastructure.Tests.Adapters.Configuration;

public sealed class ConfigurationAppConfigurationAdapterTests
{
    [Fact]
    public async Task GetWebhookSecretAsync_ShouldReturnSecret_WhenConfigured()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GitHub:WebhookSecret"] = "super-secret"
            })
            .Build();

        var sut = new ConfigurationAppConfigurationAdapter(configuration);

        var result = await sut.GetWebhookSecretAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("super-secret");
    }

    [Fact]
    public async Task GetWebhookSecretAsync_ShouldFail_WhenSecretMissing()
    {
        // Store original environment variables
        var originalProbotSharpSecret = Environment.GetEnvironmentVariable("PROBOTSHARP_WEBHOOK_SECRET");
        var originalWebhookSecret = Environment.GetEnvironmentVariable("WEBHOOK_SECRET");

        try
        {
            // Clear environment variables to ensure test isolation
            Environment.SetEnvironmentVariable("PROBOTSHARP_WEBHOOK_SECRET", null);
            Environment.SetEnvironmentVariable("WEBHOOK_SECRET", null);

            var configuration = new ConfigurationBuilder().Build();
            var sut = new ConfigurationAppConfigurationAdapter(configuration);

            var result = await sut.GetWebhookSecretAsync();

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error!.Value.Code.Should().Be("webhook_secret_missing");
        }
        finally
        {
            // Restore original environment variables
            Environment.SetEnvironmentVariable("PROBOTSHARP_WEBHOOK_SECRET", originalProbotSharpSecret);
            Environment.SetEnvironmentVariable("WEBHOOK_SECRET", originalWebhookSecret);
        }
    }
}
