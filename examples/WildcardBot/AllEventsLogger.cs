// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Domain.Context;

namespace WildcardBot;

/// <summary>
/// Logs all webhook events using wildcard pattern.
/// This is the equivalent of Node.js Probot's app.onAny().
/// </summary>
[EventHandler("*", null)]
public class AllEventsLogger : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        // This handler receives ALL webhook events
        context.Logger.LogInformation(
            "[AllEventsLogger] Event: {Event}.{Action} | Repository: {Repository} | Sender: {Sender}",
            context.EventName,
            context.EventAction ?? "null",
            context.GetRepositoryFullName(),
            context.Payload["sender"]?["login"]?.ToString() ?? "unknown");

        // Example: Log payload size
        var payloadSize = context.Payload.ToString().Length;
        context.Logger.LogDebug(
            "[AllEventsLogger] Payload size: {Size} bytes",
            payloadSize);

        await Task.CompletedTask;
    }
}
