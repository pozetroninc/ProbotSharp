// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using NSubstitute;
using ProbotSharp.Infrastructure.Adapters.Workers;

namespace ProbotSharp.Infrastructure.Tests.Adapters.Workers;

public sealed class LocalWebhookChannelAdapterTests
{
    [Fact]
    public async Task CreateChannelAsync_ShouldReturnUrl()
    {
        var logger = Substitute.For<ILogger<LocalWebhookChannelAdapter>>();
        var sut = new LocalWebhookChannelAdapter(logger);

        var result = await sut.CreateChannelAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.WebhookProxyUrl.Should().StartWith("https://smee.io/");
    }
}



