// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Ports.Outbound;

using Serilog;
using Serilog.Context;

namespace ProbotSharp.Infrastructure.Adapters.Observability;

/// <summary>
/// Serilog-based implementation of the logging port.
/// Provides structured logging with support for scopes, enrichment, and correlation.
/// </summary>
public sealed class SerilogLoggingAdapter : ILoggingPort
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerilogLoggingAdapter"/> class.
    /// </summary>
    /// <param name="logger">The Serilog logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public SerilogLoggingAdapter(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        this._logger = logger.ForContext("SourceContext", "ProbotSharp.Application");
    }

    /// <inheritdoc />
    public void LogTrace(string message, params object?[] args)
    {
        this._logger.Verbose(message, args);
    }

    /// <inheritdoc />
    public void LogDebug(string message, params object?[] args)
    {
        this._logger.Debug(message, args);
    }

    /// <inheritdoc />
    public void LogInformation(string message, params object?[] args)
    {
        this._logger.Information(message, args);
    }

    /// <inheritdoc />
    public void LogWarning(string message, params object?[] args)
    {
        this._logger.Warning(message, args);
    }

    /// <inheritdoc />
    public void LogError(Exception? exception, string message, params object?[] args)
    {
        if (exception is not null)
        {
            this._logger.Error(exception, message, args);
        }
        else
        {
            this._logger.Error(message, args);
        }
    }

    /// <inheritdoc />
    public void LogCritical(Exception? exception, string message, params object?[] args)
    {
        if (exception is not null)
        {
            this._logger.Fatal(exception, message, args);
        }
        else
        {
            this._logger.Fatal(message, args);
        }
    }

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state is IEnumerable<KeyValuePair<string, object>> properties)
        {
            return new SerilogScope(properties);
        }

        var propertyInfos = state.GetType().GetProperties();
        var propertyDictionary = new Dictionary<string, object>();

        foreach (var prop in propertyInfos)
        {
            var value = prop.GetValue(state);
            if (value is not null)
            {
                propertyDictionary[prop.Name] = value;
            }
        }

        return new SerilogScope(propertyDictionary);
    }

    /// <summary>
    /// Represents a Serilog logging scope that enriches log events with contextual properties.
    /// </summary>
    private sealed class SerilogScope : IDisposable
    {
        private readonly List<IDisposable> _disposables = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogScope"/> class.
        /// </summary>
        /// <param name="properties">The properties to add to the logging context.</param>
        public SerilogScope(IEnumerable<KeyValuePair<string, object>> properties)
        {
            foreach (var property in properties)
            {
                this._disposables.Add(LogContext.PushProperty(property.Key, property.Value));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var disposable in this._disposables)
            {
                disposable.Dispose();
            }

            this._disposables.Clear();
        }
    }
}
