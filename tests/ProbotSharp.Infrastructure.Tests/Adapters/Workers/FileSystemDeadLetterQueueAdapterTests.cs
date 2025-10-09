// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System;
using System.IO;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ProbotSharp.Application.Models;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Infrastructure.Adapters.Workers;

namespace ProbotSharp.Infrastructure.Tests.Adapters.Workers;

public sealed class FileSystemDeadLetterQueueAdapterTests : IDisposable
{
    private readonly string _dir;
    private readonly ILogger<FileSystemDeadLetterQueueAdapter> _logger;
    private readonly FileSystemDeadLetterQueueAdapter _sut;

    public FileSystemDeadLetterQueueAdapterTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), $"ps-dlq-{Guid.NewGuid():N}");
        _logger = Substitute.For<ILogger<FileSystemDeadLetterQueueAdapter>>();
        _sut = new FileSystemDeadLetterQueueAdapter(_dir, _logger);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); } catch { }
    }

    private static EnqueueReplayCommand CreateReplayCommand(int attempt = 1)
    {
        var cmd = new ProcessWebhookCommand(
            DeliveryId.Create(Guid.NewGuid().ToString("N")),
            WebhookEventName.Create("ping"),
            WebhookPayload.Create("{}"),
            null,
            WebhookSignature.Create("sha256=" + new string('a', 64)),
            "{}");
        return new EnqueueReplayCommand(cmd, attempt);
    }

    [Fact]
    public async Task MoveGetAllGetByIdRequeueDelete_ShouldRoundTrip()
    {
        var cmd = CreateReplayCommand();
        var moved = await _sut.MoveToDeadLetterAsync(cmd, "fail");
        moved.IsSuccess.Should().BeTrue();

        var all = await _sut.GetAllAsync();
        all.IsSuccess.Should().BeTrue();
        all.Value.Should().HaveCount(1);

        var id = all.Value![0].Id;
        var byId = await _sut.GetByIdAsync(id);
        byId.IsSuccess.Should().BeTrue();
        byId.Value.Should().NotBeNull();

        var requeue = await _sut.RequeueAsync(id);
        requeue.IsSuccess.Should().BeTrue();
        requeue.Value.Should().NotBeNull();

        // After requeue the file is deleted; delete should report not found now
        var delete = await _sut.DeleteAsync(id);
        delete.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenItemExists_ShouldSucceed()
    {
        var cmd = CreateReplayCommand();
        var moved = await _sut.MoveToDeadLetterAsync(cmd, "fail");
        moved.IsSuccess.Should().BeTrue();

        var all = await _sut.GetAllAsync();
        var id = all.Value![0].Id;

        var delete = await _sut.DeleteAsync(id);
        delete.IsSuccess.Should().BeTrue();

        var byId = await _sut.GetByIdAsync(id);
        byId.IsSuccess.Should().BeTrue();
        byId.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithCorruptedFile_ShouldSkipAndReturnSuccess()
    {
        Directory.CreateDirectory(_dir);
        var corrupted = Path.Combine(_dir, "dlq-corrupted.json");
        await File.WriteAllTextAsync(corrupted, "{not-json}");

        var result = await _sut.GetAllAsync();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ShouldReturnNull()
    {
        var res = await _sut.GetByIdAsync("dlq-000000000000000-ffffffffffffffffffffffffffffffff");
        res.IsSuccess.Should().BeTrue();
        res.Value.Should().BeNull();
    }

    [Fact]
    public async Task MoveToDeadLetterAsync_WhenDirectoryIsAFile_ShouldReturnFailure()
    {
        var filePath = _dir; // reuse directory field, but create a file at that path
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllTextAsync(filePath, "not a directory");

        var cmd = CreateReplayCommand();
        var result = await _sut.MoveToDeadLetterAsync(cmd, "reason");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("dead_letter_write_failed");
    }
}


