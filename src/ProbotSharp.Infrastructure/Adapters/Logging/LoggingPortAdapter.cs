// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Ports.Outbound;

namespace ProbotSharp.Infrastructure.Adapters.Logging;

/// <summary>
/// Bridges the application logging port to <see cref="Microsoft.Extensions.Logging"/>.
/// </summary>
public sealed class LoggingPortAdapter : ILoggingPort
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingPortAdapter"/> class.
    /// </summary>
    /// <param name="loggerFactory">The factory used to create the backing logger.</param>
    public LoggingPortAdapter(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        this._logger = loggerFactory.CreateLogger("ProbotSharp.Application")
                      ?? throw new InvalidOperationException("Unable to create application logger instance.");
    }

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        ArgumentNullException.ThrowIfNull(state);
        return this._logger.BeginScope(state) ?? NullScope.Instance;
    }

    /// <inheritdoc />
    public void LogCritical(Exception? exception, string message, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(message);
        this._logger.LogCritical(exception, message, args);
    }

    /// <inheritdoc />
    public void LogDebug(string message, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(message);
        this._logger.LogDebug(message, args);
    }

    /// <inheritdoc />
    public void LogError(Exception? exception, string message, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(message);
        this._logger.LogError(exception, message, args);
    }

    /// <inheritdoc />
    public void LogInformation(string message, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(message);
        this._logger.LogInformation(message, args);
    }

    /// <inheritdoc />
    public void LogTrace(string message, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(message);
        this._logger.LogTrace(message, args);
    }

    /// <inheritdoc />
    public void LogWarning(string message, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(message);
        this._logger.LogWarning(message, args);
    }

    /// <summary>
    /// A no-op scope used when BeginScope is called but no scope can be created.
    /// </summary>
    private sealed class NullScope : IDisposable
    {
        /// <summary>
        /// Gets the singleton instance of the null scope.
        /// </summary>
        public static readonly NullScope Instance = new();

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
