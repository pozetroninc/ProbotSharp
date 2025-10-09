// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ProbotSharp.Domain.Context;

/// <summary>
/// Extension methods for working with dry-run mode in ProbotSharpContext.
/// These helpers make it easier to implement safe testing of bulk operations.
/// </summary>
public static class ProbotSharpContextDryRunExtensions
{
    /// <summary>
    /// Logs what would be executed in dry-run mode.
    /// This method only logs if the context is in dry-run mode.
    /// </summary>
    /// <param name="context">The Probot context.</param>
    /// <param name="action">Description of the action that would be performed.</param>
    /// <param name="parameters">Optional parameters for the action (will be serialized as JSON).</param>
    /// <example>
    /// <code>
    /// context.LogDryRun("Create issue", new { title = "Test Issue", body = "Test body" });
    /// </code>
    /// </example>
    public static void LogDryRun(this ProbotSharpContext context, string action, object? parameters = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        if (context.IsDryRun)
        {
            if (parameters != null)
            {
                try
                {
                    var paramJson = JsonSerializer.Serialize(parameters, new JsonSerializerOptions
                    {
                        WriteIndented = false,
                    });
                    context.Logger.LogInformation("[DRY-RUN] Would execute: {Action} with parameters: {Parameters}",
                        action, paramJson);
                }
                catch (Exception ex)
                {
                    context.Logger.LogInformation("[DRY-RUN] Would execute: {Action} with parameters: {Parameters}",
                        action, parameters.ToString());
                    context.Logger.LogDebug(ex, "Failed to serialize dry-run parameters as JSON");
                }
            }
            else
            {
                context.Logger.LogInformation("[DRY-RUN] Would execute: {Action}", action);
            }
        }
    }

    /// <summary>
    /// Throws an exception if the context is NOT in dry-run mode.
    /// This is useful for operations that should only be executed manually, not automatically.
    /// </summary>
    /// <param name="context">The Probot context.</param>
    /// <param name="message">Error message to throw if not in dry-run mode.</param>
    /// <exception cref="InvalidOperationException">Thrown if the context is not in dry-run mode.</exception>
    /// <example>
    /// <code>
    /// // This ensures dangerous operations are only logged, never executed
    /// context.ThrowIfNotDryRun("This operation is too dangerous to run automatically");
    /// </code>
    /// </example>
    public static void ThrowIfNotDryRun(this ProbotSharpContext context, string message)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (!context.IsDryRun)
        {
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// Executes an action only if NOT in dry-run mode, otherwise logs what would be done.
    /// This provides a convenient pattern for conditional execution based on dry-run mode.
    /// </summary>
    /// <param name="context">The Probot context.</param>
    /// <param name="actionDescription">Description of the action for logging.</param>
    /// <param name="action">The action to execute if not in dry-run mode.</param>
    /// <param name="parameters">Optional parameters for logging in dry-run mode.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <example>
    /// <code>
    /// await context.ExecuteOrLog(
    ///     "Create issue comment",
    ///     async () => await context.GitHub.Issue.Comment.Create(owner, repo, number, "Hello!"),
    ///     new { owner, repo, number, body = "Hello!" }
    /// );
    /// </code>
    /// </example>
    public static async Task ExecuteOrLogAsync(
        this ProbotSharpContext context,
        string actionDescription,
        Func<Task> action,
        object? parameters = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionDescription);
        ArgumentNullException.ThrowIfNull(action);

        if (context.IsDryRun)
        {
            context.LogDryRun(actionDescription, parameters);
        }
        else
        {
            await action().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes an action only if NOT in dry-run mode, otherwise logs what would be done.
    /// This overload returns a result from the action.
    /// </summary>
    /// <typeparam name="T">The type of result returned by the action.</typeparam>
    /// <param name="context">The Probot context.</param>
    /// <param name="actionDescription">Description of the action for logging.</param>
    /// <param name="action">The action to execute if not in dry-run mode.</param>
    /// <param name="dryRunResult">The result to return in dry-run mode (default value if not specified).</param>
    /// <param name="parameters">Optional parameters for logging in dry-run mode.</param>
    /// <returns>The result of the action, or the dry-run result if in dry-run mode.</returns>
    /// <example>
    /// <code>
    /// var comment = await context.ExecuteOrLog(
    ///     "Create issue comment",
    ///     async () => await context.GitHub.Issue.Comment.Create(owner, repo, number, "Hello!"),
    ///     dryRunResult: null,
    ///     parameters: new { owner, repo, number, body = "Hello!" }
    /// );
    /// </code>
    /// </example>
    public static async Task<T?> ExecuteOrLogAsync<T>(
        this ProbotSharpContext context,
        string actionDescription,
        Func<Task<T>> action,
        T? dryRunResult = default,
        object? parameters = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionDescription);
        ArgumentNullException.ThrowIfNull(action);

        if (context.IsDryRun)
        {
            context.LogDryRun(actionDescription, parameters);
            return dryRunResult;
        }
        else
        {
            return await action().ConfigureAwait(false);
        }
    }
}
