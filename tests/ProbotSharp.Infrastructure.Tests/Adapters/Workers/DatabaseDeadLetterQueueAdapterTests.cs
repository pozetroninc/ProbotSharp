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

    [Fact]
    public void Constructor_WithNullDbContext_ShouldThrow()
    {
        var logger = Substitute.For<ILogger<DatabaseDeadLetterQueueAdapter>>();
        var act = () => new DatabaseDeadLetterQueueAdapter(null!, logger);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        using var db = CreateInMemoryDb();
        var act = () => new DatabaseDeadLetterQueueAdapter(db, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task MoveToDeadLetterAsync_WithNullCommand_ShouldThrow()
    {
        await using var db = CreateInMemoryDb();
        var logger = Substitute.For<ILogger<DatabaseDeadLetterQueueAdapter>>();
        var sut = new DatabaseDeadLetterQueueAdapter(db, logger);

        var act = async () => await sut.MoveToDeadLetterAsync(null!, "reason");
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task MoveToDeadLetterAsync_WithNullReason_ShouldThrow()
    {
        await using var db = CreateInMemoryDb();
        var logger = Substitute.For<ILogger<DatabaseDeadLetterQueueAdapter>>();
        var sut = new DatabaseDeadLetterQueueAdapter(db, logger);

        var cmd = CreateReplayCommand();
        var act = async () => await sut.MoveToDeadLetterAsync(cmd, null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task MoveToDeadLetterAsync_WithEmptyReason_ShouldThrow()
    {
        await using var db = CreateInMemoryDb();
        var logger = Substitute.For<ILogger<DatabaseDeadLetterQueueAdapter>>();
        var sut = new DatabaseDeadLetterQueueAdapter(db, logger);

        var cmd = CreateReplayCommand();
        var act = async () => await sut.MoveToDeadLetterAsync(cmd, string.Empty);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetAllAsync_WithEmptyQueue_ShouldReturnEmptyList()
    {
        await using var db = CreateInMemoryDb();
        var logger = Substitute.For<ILogger<DatabaseDeadLetterQueueAdapter>>();
        var sut = new DatabaseDeadLetterQueueAdapter(db, logger);

        var result = await sut.GetAllAsync();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_WithNullId_ShouldThrow()
    {
        await using var db = CreateInMemoryDb();
        var logger = Substitute.For<ILogger<DatabaseDeadLetterQueueAdapter>>();
        var sut = new DatabaseDeadLetterQueueAdapter(db, logger);

        var act = async () => await sut.GetByIdAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetByIdAsync_WithEmptyId_ShouldThrow()
    {
        await using var db = CreateInMemoryDb();
        var logger = Substitute.For<ILogger<DatabaseDeadLetterQueueAdapter>>();
        var sut = new DatabaseDeadLetterQueueAdapter(db, logger);

        var act = async () => await sut.GetByIdAsync(string.Empty);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        await using var db = CreateInMemoryDb();
        var logger = Substitute.For<ILogger<DatabaseDeadLetterQueueAdapter>>();
        var sut = new DatabaseDeadLetterQueueAdapter(db, logger);

        var result = await sut.GetByIdAsync("non-existent-id");
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task RequeueAsync_WithNullId_ShouldThrow()
    {
        await using var db = CreateInMemoryDb();
        var logger = Substitute.For<ILogger<DatabaseDeadLetterQueueAdapter>>();
        var sut = new DatabaseDeadLetterQueueAdapter(db, logger);

        var act = async () => await sut.RequeueAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RequeueAsync_WithEmptyId_ShouldThrow()
    {
        await using var db = CreateInMemoryDb();
        var logger = Substitute.For<ILogger<DatabaseDeadLetterQueueAdapter>>();
        var sut = new DatabaseDeadLetterQueueAdapter(db, logger);

        var act = async () => await sut.RequeueAsync(string.Empty);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RequeueAsync_WithNonExistentId_ShouldReturnNull()
    {
        await using var db = CreateInMemoryDb();
        var logger = Substitute.For<ILogger<DatabaseDeadLetterQueueAdapter>>();
        var sut = new DatabaseDeadLetterQueueAdapter(db, logger);

        var result = await sut.RequeueAsync("non-existent-id");
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task RequeueAsync_ShouldResetAttemptCount()
    {
        await using var db = CreateInMemoryDb();
        var logger = Substitute.For<ILogger<DatabaseDeadLetterQueueAdapter>>();
        var sut = new DatabaseDeadLetterQueueAdapter(db, logger);

        var cmd = CreateReplayCommand(attempt: 5);
        await sut.MoveToDeadLetterAsync(cmd, "max retries");

        var all = await sut.GetAllAsync();
        var id = all.Value![0].Id;

        var requeued = await sut.RequeueAsync(id);
        requeued.IsSuccess.Should().BeTrue();
        requeued.Value.Should().NotBeNull();
        requeued.Value!.Attempt.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_WithNullId_ShouldThrow()
    {
        await using var db = CreateInMemoryDb();
        var logger = Substitute.For<ILogger<DatabaseDeadLetterQueueAdapter>>();
        var sut = new DatabaseDeadLetterQueueAdapter(db, logger);

        var act = async () => await sut.DeleteAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DeleteAsync_WithEmptyId_ShouldThrow()
    {
        await using var db = CreateInMemoryDb();
        var logger = Substitute.For<ILogger<DatabaseDeadLetterQueueAdapter>>();
        var sut = new DatabaseDeadLetterQueueAdapter(db, logger);

        var act = async () => await sut.DeleteAsync(string.Empty);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ShouldReturnFailure()
    {
        await using var db = CreateInMemoryDb();
        var logger = Substitute.For<ILogger<DatabaseDeadLetterQueueAdapter>>();
        var sut = new DatabaseDeadLetterQueueAdapter(db, logger);

        var result = await sut.DeleteAsync("non-existent-id");
        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("dead_letter_not_found");
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleItems_ShouldReturnOrderedByFailedAtDescending()
    {
        await using var db = CreateInMemoryDb();
        var logger = Substitute.For<ILogger<DatabaseDeadLetterQueueAdapter>>();
        var sut = new DatabaseDeadLetterQueueAdapter(db, logger);

        var cmd1 = CreateReplayCommand();
        var cmd2 = CreateReplayCommand();
        var cmd3 = CreateReplayCommand();

        await sut.MoveToDeadLetterAsync(cmd1, "first");
        await Task.Delay(10);
        await sut.MoveToDeadLetterAsync(cmd2, "second");
        await Task.Delay(10);
        await sut.MoveToDeadLetterAsync(cmd3, "third");

        var result = await sut.GetAllAsync();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value![0].Reason.Should().Be("third");
        result.Value[1].Reason.Should().Be("second");
        result.Value[2].Reason.Should().Be("first");
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
