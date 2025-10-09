// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Net;

using Microsoft.Extensions.Logging;

namespace ProbotSharp.Infrastructure.Adapters.GitHub;

/// <summary>
/// Structured log messages for GitHub OAuth client operations.
/// </summary>
internal static partial class LogMessages
{
    /// <summary>
    /// Logs when a GitHub installation token request fails with a non-success status code.
    /// </summary>
    [LoggerMessage(EventId = 2400, Level = LogLevel.Warning, Message = "GitHub installation token request failed with status {StatusCode}: {Body}")]
    public static partial void InstallationTokenRequestFailed(ILogger logger, HttpStatusCode statusCode, string body);

    /// <summary>
    /// Logs when a GitHub installation token response is empty or invalid.
    /// </summary>
    [LoggerMessage(EventId = 2401, Level = LogLevel.Warning, Message = "GitHub installation token response was empty")]
    public static partial void InstallationTokenInvalidResponse(ILogger logger);

    /// <summary>
    /// Logs when a GitHub installation token response contains invalid JSON.
    /// </summary>
    [LoggerMessage(EventId = 2402, Level = LogLevel.Error, Message = "GitHub installation token response contained invalid JSON")]
    public static partial void InstallationTokenInvalidJson(ILogger logger, Exception exception);

    /// <summary>
    /// Logs when a GitHub installation token request fails unexpectedly.
    /// </summary>
    [LoggerMessage(EventId = 2403, Level = LogLevel.Error, Message = "GitHub installation token request failed unexpectedly")]
    public static partial void InstallationTokenUnexpectedError(ILogger logger, Exception exception);
}
