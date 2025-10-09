// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace ProbotSharp.Infrastructure.Adapters.Configuration;

/// <summary>
/// Structured log messages for environment configuration adapter operations.
/// </summary>
internal static partial class LogMessages
{
    /// <summary>
    /// Logs when writing environment configuration to a file fails.
    /// </summary>
    [LoggerMessage(EventId = 2600, Level = LogLevel.Error, Message = "Failed to persist environment configuration to {EnvFile}")]
    public static partial void EnvironmentWriteFailed(ILogger logger, string envFile, Exception exception);
}
