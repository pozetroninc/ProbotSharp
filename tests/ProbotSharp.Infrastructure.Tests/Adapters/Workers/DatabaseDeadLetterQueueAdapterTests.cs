// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NSubstitute;

using ProbotSharp.Application.Models;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Infrastructure.Adapters.Persistence;
using ProbotSharp.Infrastructure.Adapters.Workers;

namespace ProbotSharp.Infrastructure.Tests.Adapters.Workers;

public sealed class DatabaseDeadLetterQueueAdapterTests
{
    private static ProbotSharpDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<ProbotSharpDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var ctx = new ProbotSharpDbContext(options);
        return ctx;
    }

    [Fact]
    public async Task MoveGetRequeueDelete_ShouldCoverHappyPaths()
    {
        await using var db = CreateInMemoryDb();
        var logger = Substitute.For<ILogger<DatabaseDeadLetterQueueAdapter>>();
        var sut = new DatabaseDeadLetterQueueAdapter(db, logger);

        var cmd = CreateReplayCommand();
        (await sut.MoveToDeadLetterAsync(cmd, "failure")).IsSuccess.Should().BeTrue();

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

        // After requeue, item should be removed
        (await sut.GetByIdAsync(id)).Value.Should().BeNull();

        // Create another then delete
        (await sut.MoveToDeadLetterAsync(cmd, "again")).IsSuccess.Should().BeTrue();
        var id2 = (await sut.GetAllAsync()).Value![0].Id;
        (await sut.DeleteAsync(id2)).IsSuccess.Should().BeTrue();
        (await sut.DeleteAsync(id2)).IsSuccess.Should().BeFalse();
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
