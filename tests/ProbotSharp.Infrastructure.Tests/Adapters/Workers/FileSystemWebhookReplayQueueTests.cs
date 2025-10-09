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

public sealed class FileSystemWebhookReplayQueueTests : IDisposable
{
    private readonly string _queueDirectory = Path.Combine(Path.GetTempPath(), "probot-sharp-tests", Guid.NewGuid().ToString("N"));
    private readonly ILogger<FileSystemWebhookReplayQueueAdapter> _logger = Substitute.For<ILogger<FileSystemWebhookReplayQueueAdapter>>();
    private readonly FileSystemWebhookReplayQueueAdapter _sut;

    public FileSystemWebhookReplayQueueTests()
    {
        _sut = new FileSystemWebhookReplayQueueAdapter(_queueDirectory, _logger);
    }

    [Fact]
    public async Task EnqueueAndDequeue_ShouldRoundTripCommand()
    {
        var command = CreateReplayCommand();

        var enqueueResult = await _sut.EnqueueAsync(command);
        enqueueResult.IsSuccess.Should().BeTrue();

        var dequeueResult = await _sut.DequeueAsync();
        dequeueResult.IsSuccess.Should().BeTrue();
        dequeueResult.Value.Should().NotBeNull();
        dequeueResult.Value!.Command.DeliveryId.Should().Be(command.Command.DeliveryId);
        dequeueResult.Value.Attempt.Should().Be(command.Attempt);

        var secondDequeue = await _sut.DequeueAsync();
        secondDequeue.IsSuccess.Should().BeTrue();
        secondDequeue.Value.Should().BeNull();
    }

    [Fact]
    public async Task Dequeue_WhenDirectoryEmpty_ShouldReturnNull()
    {
        var result = await _sut.DequeueAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Enqueue_WhenDirectoryMissing_ShouldCreateDirectory()
    {
        Directory.Exists(_queueDirectory).Should().BeFalse();

        var enqueueResult = await _sut.EnqueueAsync(CreateReplayCommand());

        enqueueResult.IsSuccess.Should().BeTrue();
        Directory.Exists(_queueDirectory).Should().BeTrue();
    }

    [Fact]
    public async Task Dequeue_WhenFileCorrupted_ShouldReturnFailure()
    {
        Directory.CreateDirectory(_queueDirectory);
        var corruptedPath = Path.Combine(_queueDirectory, "corrupted.json");
        await File.WriteAllTextAsync(corruptedPath, "{not-json}");

        var result = await _sut.DequeueAsync();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("replay_queue_read_failed");
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

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_queueDirectory))
            {
                Directory.Delete(_queueDirectory, recursive: true);
            }
        }
        catch
        {
            // ignore cleanup failures
        }
    }
}
