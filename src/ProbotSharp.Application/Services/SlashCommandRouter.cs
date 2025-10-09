// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProbotSharp.Application.Abstractions.Commands;
using ProbotSharp.Domain.Commands;
using ProbotSharp.Domain.Context;

namespace ProbotSharp.Application.Services;

/// <summary>
/// Routes slash commands from GitHub comments to registered command handlers.
/// This service discovers commands in comment bodies, resolves matching handlers from DI,
/// and executes them in registration order.
/// </summary>
public class SlashCommandRouter
{
    private readonly Dictionary<string, List<Type>> _handlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<SlashCommandRouter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SlashCommandRouter"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information.</param>
    public SlashCommandRouter(ILogger<SlashCommandRouter> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        this._logger = logger;
    }

    /// <summary>
    /// Registers a handler type for a specific command name.
    /// Multiple handlers can be registered for the same command.
    /// </summary>
    /// <param name="commandName">The name of the command (case-insensitive).</param>
    /// <param name="handlerType">The type of the handler (must implement <see cref="ISlashCommandHandler"/>).</param>
    /// <exception cref="ArgumentException">Thrown when commandName is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when handlerType is null.</exception>
    /// <exception cref="ArgumentException">Thrown when handlerType does not implement ISlashCommandHandler.</exception>
    public void RegisterHandler(string commandName, Type handlerType)
    {
        if (string.IsNullOrWhiteSpace(commandName))
        {
            throw new ArgumentException("Command name cannot be null or whitespace.", nameof(commandName));
        }

        ArgumentNullException.ThrowIfNull(handlerType);

        if (!typeof(ISlashCommandHandler).IsAssignableFrom(handlerType))
        {
            throw new ArgumentException(
                $"Handler type {handlerType.Name} must implement {nameof(ISlashCommandHandler)}",
                nameof(handlerType));
        }

        if (!this._handlers.TryGetValue(commandName, out var handlers))
        {
            handlers = [];
            this._handlers[commandName] = handlers;
        }

        if (!handlers.Contains(handlerType))
        {
            handlers.Add(handlerType);
            this._logger.LogDebug(
                "Registered handler {HandlerType} for command '{CommandName}'",
                handlerType.Name,
                commandName);
        }
    }

    /// <summary>
    /// Parses the comment body for slash commands and routes them to registered handlers.
    /// </summary>
    /// <param name="context">The Probot context for the webhook event.</param>
    /// <param name="commentBody">The full text of the comment to parse.</param>
    /// <param name="serviceProvider">Service provider for resolving handler instances.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task RouteAsync(
        ProbotSharpContext context,
        string commentBody,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        if (string.IsNullOrWhiteSpace(commentBody))
        {
            return;
        }

        var commands = SlashCommandParser.Parse(commentBody).ToList();

        if (commands.Count == 0)
        {
            this._logger.LogDebug("No slash commands found in comment");
            return;
        }

        this._logger.LogInformation(
            "Found {CommandCount} slash command(s) in comment",
            commands.Count);

        foreach (var command in commands)
        {
            await this.DispatchCommandAsync(command, context, serviceProvider, cancellationToken);
        }
    }

    /// <summary>
    /// Dispatches a single command to all registered handlers.
    /// </summary>
    /// <param name="command">The parsed slash command.</param>
    /// <param name="context">The Probot context for the webhook event.</param>
    /// <param name="serviceProvider">Service provider for resolving handler instances.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task DispatchCommandAsync(
        SlashCommand command,
        ProbotSharpContext context,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (!this._handlers.TryGetValue(command.Name, out var handlerTypes))
        {
            this._logger.LogDebug(
                "No handlers registered for command '{CommandName}'",
                command.Name);
            return;
        }

        this._logger.LogInformation(
            "Dispatching command '{CommandName}' with arguments '{Arguments}' to {HandlerCount} handler(s)",
            command.Name,
            command.Arguments,
            handlerTypes.Count);

        foreach (var handlerType in handlerTypes)
        {
            try
            {
                var handler = serviceProvider.GetRequiredService(handlerType) as ISlashCommandHandler;
                if (handler == null)
                {
                    this._logger.LogError(
                        "Failed to resolve handler {HandlerType} as ISlashCommandHandler",
                        handlerType.Name);
                    continue;
                }

                this._logger.LogDebug(
                    "Executing handler {HandlerType} for command '{CommandName}'",
                    handlerType.Name,
                    command.Name);

                await handler.HandleAsync(context, command, cancellationToken);

                this._logger.LogDebug(
                    "Handler {HandlerType} completed successfully",
                    handlerType.Name);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                this._logger.LogInformation(
                    "Handler {HandlerType} was cancelled",
                    handlerType.Name);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.LogError(
                    ex,
                    "Handler {HandlerType} for command '{CommandName}' threw an exception: {Message}",
                    handlerType.Name,
                    command.Name,
                    ex.Message);

                // Continue processing other handlers despite this failure
            }
        }
    }
}
