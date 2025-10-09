// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Ports.Outbound;

/// <summary>
/// Port for structured logging operations.
/// Abstracts the underlying logging implementation (e.g., Serilog, Microsoft.Extensions.Logging)
/// to maintain hexagonal architecture and enable testability.
/// </summary>
public interface ILoggingPort
{
    /// <summary>
    /// Logs a trace-level message. Used for detailed diagnostic information during development.
    /// </summary>
    /// <param name="message">The log message template with optional placeholders.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    void LogTrace(string message, params object?[] args);

    /// <summary>
    /// Logs a debug-level message. Used for internal debugging information.
    /// </summary>
    /// <param name="message">The log message template with optional placeholders.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    void LogDebug(string message, params object?[] args);

    /// <summary>
    /// Logs an informational message. Used to track general application flow.
    /// </summary>
    /// <param name="message">The log message template with optional placeholders.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    void LogInformation(string message, params object?[] args);

    /// <summary>
    /// Logs a warning message. Used to highlight abnormal or unexpected events that don't prevent the application from functioning.
    /// </summary>
    /// <param name="message">The log message template with optional placeholders.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    void LogWarning(string message, params object?[] args);

    /// <summary>
    /// Logs an error message, optionally with an exception. Used when operations fail but the application can recover.
    /// </summary>
    /// <param name="exception">The exception associated with the error, if any.</param>
    /// <param name="message">The log message template with optional placeholders.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    void LogError(Exception? exception, string message, params object?[] args);

    /// <summary>
    /// Logs a critical error message, optionally with an exception. Used for catastrophic failures requiring immediate attention.
    /// </summary>
    /// <param name="exception">The exception associated with the critical error, if any.</param>
    /// <param name="message">The log message template with optional placeholders.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    void LogCritical(Exception? exception, string message, params object?[] args);

    /// <summary>
    /// Begins a logical operation scope for correlation and grouping of log entries.
    /// </summary>
    /// <typeparam name="TState">The type of the state to associate with the scope.</typeparam>
    /// <param name="state">The state object to associate with the scope (e.g., correlation ID, request context).</param>
    /// <returns>A disposable object that ends the scope when disposed.</returns>
    /// <example>
    /// <code>
    /// using (logger.BeginScope(new { DeliveryId = deliveryId.Value, EventName = eventName.Value }))
    /// {
    ///     // All logs within this scope will be correlated
    ///     logger.LogInformation("Processing webhook delivery");
    /// }
    /// </code>
    /// </example>
    IDisposable BeginScope<TState>(TState state) where TState : notnull;
}
