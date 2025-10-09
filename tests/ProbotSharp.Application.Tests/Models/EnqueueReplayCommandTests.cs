// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Models;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Application.Tests.Models;

/// <summary>
/// Tests for <see cref="EnqueueReplayCommand"/>.
/// Verifies command validation, attempt tracking, and immutability.
/// </summary>
public sealed class EnqueueReplayCommandTests
{
    [Fact]
    public void Constructor_WhenCommandIsNull_ShouldThrowArgumentNullException()
    {
        var act = () => new EnqueueReplayCommand(null!, 0);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("command");
    }

    [Fact]
    public void Constructor_WhenAttemptIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var command = CreateProcessCommand();

        var act = () => new EnqueueReplayCommand(command, -1);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("attempt")
            .WithMessage("*Attempt count cannot be negative*");
    }

    [Fact]
    public void Constructor_WhenValidParameters_ShouldCreateInstance()
    {
        var command = CreateProcessCommand();

        var result = new EnqueueReplayCommand(command, 2);

        result.Command.Should().Be(command);
        result.Attempt.Should().Be(2);
    }

    [Fact]
    public void Constructor_WhenAttemptIsZero_ShouldCreateInstance()
    {
        var command = CreateProcessCommand();

        var result = new EnqueueReplayCommand(command, 0);

        result.Command.Should().Be(command);
        result.Attempt.Should().Be(0);
    }

    [Fact]
    public void NextAttempt_WhenCalled_ShouldIncrementAttempt()
    {
        var command = CreateProcessCommand();
        var original = new EnqueueReplayCommand(command, 2);

        var next = original.NextAttempt();

        next.Command.Should().Be(original.Command);
        next.Attempt.Should().Be(3);
        original.Attempt.Should().Be(2);
    }

    [Fact]
    public void NextAttempt_WhenCalledMultipleTimes_ShouldIncrementEachTime()
    {
        var command = CreateProcessCommand();
        var original = new EnqueueReplayCommand(command, 0);

        var first = original.NextAttempt();
        var second = first.NextAttempt();
        var third = second.NextAttempt();

        original.Attempt.Should().Be(0);
        first.Attempt.Should().Be(1);
        second.Attempt.Should().Be(2);
        third.Attempt.Should().Be(3);
    }

    [Fact]
    public void Command_ShouldBeImmutable()
    {
        var command = CreateProcessCommand();
        var enqueueCommand = new EnqueueReplayCommand(command, 1);

        var retrievedCommand = enqueueCommand.Command;

        retrievedCommand.Should().BeSameAs(command);
    }

    [Fact]
    public void Attempt_ShouldBeReadOnly()
    {
        var command = CreateProcessCommand();
        var enqueueCommand = new EnqueueReplayCommand(command, 5);

        var attempt = enqueueCommand.Attempt;

        attempt.Should().Be(5);
    }

    private static ProcessWebhookCommand CreateProcessCommand()
        => new(
            DeliveryId.Create(Guid.NewGuid().ToString()),
            WebhookEventName.Create("push"),
            WebhookPayload.Create("{\"test\":true}"),
            InstallationId.Create(123),
            WebhookSignature.Create("sha256=" + new string('a', 64)),
            "{\"test\":true}");
}
