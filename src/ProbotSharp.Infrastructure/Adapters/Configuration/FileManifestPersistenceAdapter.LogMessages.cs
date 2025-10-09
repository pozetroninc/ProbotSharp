// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace ProbotSharp.Infrastructure.Adapters.Configuration;

/// <summary>
/// Structured log messages for file manifest persistence adapter operations.
/// </summary>
internal static partial class LogMessages
{
    /// <summary>
    /// Logs when reading a manifest file fails.
    /// </summary>
    [LoggerMessage(EventId = 2610, Level = LogLevel.Error, Message = "Failed to read manifest from {ManifestPath}")]
    public static partial void ManifestReadFailed(ILogger logger, string manifestPath, Exception exception);

    /// <summary>
    /// Logs when writing a manifest file fails.
    /// </summary>
    [LoggerMessage(EventId = 2611, Level = LogLevel.Error, Message = "Failed to write manifest to {ManifestPath}")]
    public static partial void ManifestWriteFailed(ILogger logger, string manifestPath, Exception exception);
}
