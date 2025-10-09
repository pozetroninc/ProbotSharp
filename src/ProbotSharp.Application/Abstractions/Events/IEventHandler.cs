// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Context;

namespace ProbotSharp.Application.Abstractions.Events;

/// <summary>
/// Defines a handler for GitHub webhook events.
/// Implementations of this interface are automatically discovered and registered
/// when decorated with <see cref="EventHandlerAttribute"/>.
/// </summary>
public interface IEventHandler
{
    /// <summary>
    /// Handles a webhook event with the provided context.
    /// </summary>
    /// <param name="context">The Probot context containing event data and GitHub API client.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default);
}
