// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

using NSubstitute;

using ProbotSharp.Application.Models;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Infrastructure.Adapters.Workers;

namespace ProbotSharp.Infrastructure.Tests.Adapters.Workers;

public sealed class InMemoryWebhookReplayQueueAdapterTests
{
    [Fact]
    public async Task EnqueueDequeue_ShouldRoundTrip()
    {
        var logger = Substitute.For<ILogger<InMemoryWebhookReplayQueueAdapter>>();
        var sut = new InMemoryWebhookReplayQueueAdapter(logger);

        var cmd = CreateReplayCommand();
        (await sut.EnqueueAsync(cmd)).IsSuccess.Should().BeTrue();

        var first = await sut.DequeueAsync();
        first.IsSuccess.Should().BeTrue();
        first.Value.Should().NotBeNull();

        var second = await sut.DequeueAsync();
        second.IsSuccess.Should().BeTrue();
        second.Value.Should().BeNull();
    }

    private static EnqueueReplayCommand CreateReplayCommand(int attempt = 0)
        => new(new ProcessWebhookCommand(
            DeliveryId.Create(Guid.NewGuid().ToString()),
            WebhookEventName.Create("push"),
            WebhookPayload.Create("{\"ok\":true}"),
            InstallationId.Create(1),
            WebhookSignature.Create("sha256=" + new string('a', 64)),
            "{\"ok\":true}"),
            attempt);
}
