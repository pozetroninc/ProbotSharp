// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Events;

/// <summary>
/// Domain event raised when a GitHub App is created.
/// </summary>
/// <param name="AppId">The GitHub App ID.</param>
/// <param name="Name">The name of the GitHub App.</param>
public sealed record class GitHubAppCreatedDomainEvent(GitHubAppId AppId, string Name);
