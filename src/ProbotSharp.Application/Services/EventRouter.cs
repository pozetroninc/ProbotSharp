// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Domain.Context;

#pragma warning disable CA1848 // Performance: LoggerMessage delegates - not performance-critical for this codebase

namespace ProbotSharp.Application.Services;

/// <summary>
/// Routes webhook events to registered handlers based on event name and action patterns.
/// Supports wildcard matching and multiple handlers per event.
/// </summary>
public sealed class EventRouter
{
    private readonly ILogger<EventRouter> _logger;
    private readonly List<HandlerRegistration> _handlers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EventRouter"/> class.
    /// </summary>
    /// <param name="logger">Logger for event routing operations.</param>
    public EventRouter(ILogger<EventRouter> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        this._logger = logger;
    }

    /// <summary>
    /// Registers a handler type for a specific event and action pattern.
    /// </summary>
    /// <param name="eventName">The event name to match (e.g., "issues", "*", "issues.*").</param>
    /// <param name="action">The action to match (e.g., "opened", "*"), or null to match all actions.</param>
    /// <param name="handlerType">The handler type that implements <see cref="IEventHandler"/>.</param>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public void RegisterHandler(string eventName, string? action, Type handlerType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(handlerType);

        if (!typeof(IEventHandler).IsAssignableFrom(handlerType))
        {
            throw new ArgumentException(
                $"Handler type {handlerType.Name} must implement {nameof(IEventHandler)}",
                nameof(handlerType));
        }

        var registration = new HandlerRegistration(eventName, action, handlerType);
        this._handlers.Add(registration);

        this._logger.LogDebug(
            "Registered handler {HandlerType} for event {EventName} action {Action}",
            handlerType.Name,
            eventName,
            action ?? "*");
    }

    /// <summary>
    /// Routes an event to all matching handlers.
    /// </summary>
    /// <param name="context">The Probot context containing event information.</param>
    /// <param name="serviceProvider">Service provider for resolving handler instances.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous routing operation.</returns>
    public async Task RouteAsync(
        ProbotSharpContext context,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var matchingHandlers = this.FindMatchingHandlers(context.EventName, context.EventAction);

        if (matchingHandlers.Count == 0)
        {
            this._logger.LogDebug(
                "No handlers found for event {EventName} action {Action}",
                context.EventName,
                context.EventAction ?? "<none>");
            return;
        }

        this._logger.LogInformation(
            "Routing event {EventName}.{Action} to {HandlerCount} handler(s)",
            context.EventName,
            context.EventAction ?? "*",
            matchingHandlers.Count);

        foreach (var registration in matchingHandlers)
        {
            await this.ExecuteHandlerAsync(registration, context, serviceProvider, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Finds all handlers that match the given event name and action.
    /// </summary>
    private List<HandlerRegistration> FindMatchingHandlers(string eventName, string? action)
    {
        var matches = new List<HandlerRegistration>();

        foreach (var handler in this._handlers)
        {
            if (IsEventMatch(handler.EventName, eventName) &&
                IsActionMatch(handler.Action, action))
            {
                matches.Add(handler);
            }
        }

        return matches;
    }

    /// <summary>
    /// Determines if an event pattern matches an actual event name.
    /// Supports wildcards: "*" matches all events, "issues.*" is treated as "issues".
    /// </summary>
    private static bool IsEventMatch(string pattern, string eventName)
    {
        if (pattern == "*")
        {
            return true;
        }

        // Handle "event.*" pattern - strip the ".*" suffix
        if (pattern.EndsWith(".*", StringComparison.Ordinal))
        {
            var basePattern = pattern[..^2];
            return string.Equals(basePattern, eventName, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(pattern, eventName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if an action pattern matches an actual action.
    /// Supports wildcards: "*" or null matches all actions.
    /// </summary>
    private static bool IsActionMatch(string? pattern, string? action)
    {
        // Null or "*" pattern matches everything
        if (pattern == null || pattern == "*")
        {
            return true;
        }

        // If pattern is specific but action is null, no match
        if (action == null)
        {
            return false;
        }

        return string.Equals(pattern, action, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Executes a single handler, catching and logging any exceptions.
    /// </summary>
    private async Task ExecuteHandlerAsync(
        HandlerRegistration registration,
        ProbotSharpContext context,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var handlerName = registration.HandlerType.Name;

        try
        {
            this._logger.LogDebug(
                "Executing handler {HandlerName} for {EventName}.{Action}",
                handlerName,
                context.EventName,
                context.EventAction ?? "*");

            // Resolve handler from DI container (creates a new scope for this handler)
            using var scope = serviceProvider.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService(registration.HandlerType) as IEventHandler;

            if (handler == null)
            {
                this._logger.LogError(
                    "Failed to resolve handler {HandlerName} from service provider",
                    handlerName);
                return;
            }

            await handler.HandleAsync(context, cancellationToken).ConfigureAwait(false);

            this._logger.LogDebug(
                "Handler {HandlerName} completed successfully",
                handlerName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            this._logger.LogWarning(
                "Handler {HandlerName} was cancelled",
                handlerName);
            throw;
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Handler {HandlerName} threw an exception while processing {EventName}.{Action}: {ErrorMessage}",
                handlerName,
                context.EventName,
                context.EventAction ?? "*",
                ex.Message);

            // Don't rethrow - we want to continue processing other handlers
        }
    }

    /// <summary>
    /// Gets the count of registered handlers.
    /// </summary>
    public int HandlerCount => this._handlers.Count;

    /// <summary>
    /// Represents a registered handler with its event pattern.
    /// </summary>
    private sealed class HandlerRegistration
    {
        public HandlerRegistration(string eventName, string? action, Type handlerType)
        {
            this.EventName = eventName;
            this.Action = action;
            this.HandlerType = handlerType;
        }

        public string EventName { get; }

        public string? Action { get; }

        public Type HandlerType { get; }
    }
}

#pragma warning restore CA1848
