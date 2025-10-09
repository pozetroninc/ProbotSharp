// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using NSubstitute;
using ProbotSharp.Application.Models;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Infrastructure.Adapters.Workers;

namespace ProbotSharp.Infrastructure.Tests.Adapters.Workers;

public sealed class InMemoryDeadLetterQueueAdapterTests
{
    private readonly ILogger<InMemoryDeadLetterQueueAdapter> _logger = Substitute.For<ILogger<InMemoryDeadLetterQueueAdapter>>();

    [Fact]
    public async Task MoveGetRequeueDelete_ShouldCoverHappyPaths()
    {
        var sut = new InMemoryDeadLetterQueueAdapter(_logger);

        var command = CreateReplayCommand();

        var write = await sut.MoveToDeadLetterAsync(command, "because");
        write.IsSuccess.Should().BeTrue();

        var all = await sut.GetAllAsync();
        all.IsSuccess.Should().BeTrue();
        all.Value.Should().HaveCount(1);
        var id = all.Value![0].Id;

        var byId = await sut.GetByIdAsync(id);
        byId.IsSuccess.Should().BeTrue();
        byId.Value.Should().NotBeNull();

        var requeued = await sut.RequeueAsync(id);
        requeued.IsSuccess.Should().BeTrue();
        requeued.Value.Should().NotBeNull();

        // Requeue removes the item; delete should report not found now
        var deleted = await sut.DeleteAsync(id);
        deleted.IsSuccess.Should().BeFalse();

        var missingDelete = await sut.DeleteAsync(id);
        missingDelete.IsSuccess.Should().BeFalse();
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


