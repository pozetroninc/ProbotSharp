// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Net;

using Microsoft.Extensions.Logging;

namespace ProbotSharp.Infrastructure.Adapters.GitHub;

/// <summary>
/// Structured log messages for GitHub GraphQL client operations.
/// </summary>
internal static partial class LogMessages
{
    /// <summary>
    /// Logs when a GitHub GraphQL request fails with a non-success status code.
    /// </summary>
    [LoggerMessage(EventId = 2500, Level = LogLevel.Error, Message = "GitHub GraphQL request failed with status {StatusCode}: {ErrorContent}")]
    public static partial void GraphQlRequestFailed(ILogger logger, HttpStatusCode statusCode, string errorContent);

    /// <summary>
    /// Logs when a GitHub GraphQL query returns errors.
    /// </summary>
    [LoggerMessage(EventId = 2501, Level = LogLevel.Error, Message = "GitHub GraphQL returned errors: {Errors}")]
    public static partial void GraphQlErrorsReturned(ILogger logger, string errors);

    /// <summary>
    /// Logs when a GitHub GraphQL response contains no data.
    /// </summary>
    [LoggerMessage(EventId = 2502, Level = LogLevel.Error, Message = "GitHub GraphQL response contains no data")]
    public static partial void GraphQlNoData(ILogger logger);

    /// <summary>
    /// Logs when deserialization of a GraphQL response fails.
    /// </summary>
    [LoggerMessage(EventId = 2503, Level = LogLevel.Error, Message = "Failed to deserialize GraphQL response")]
    public static partial void GraphQlDeserializationFailed(ILogger logger, Exception exception);

    /// <summary>
    /// Logs when a GitHub GraphQL call fails unexpectedly.
    /// </summary>
    [LoggerMessage(EventId = 2504, Level = LogLevel.Error, Message = "GitHub GraphQL call failed")]
    public static partial void GraphQlRequestFailedUnexpected(ILogger logger, Exception exception);
}
