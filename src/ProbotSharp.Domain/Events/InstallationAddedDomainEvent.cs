// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Events;

/// <summary>
/// Domain event raised when an installation is added to a GitHub App.
/// </summary>
/// <param name="AppId">The GitHub App ID.</param>
/// <param name="InstallationId">The installation ID.</param>
/// <param name="AccountLogin">The account login that installed the app.</param>
public sealed record class InstallationAddedDomainEvent(GitHubAppId AppId, InstallationId InstallationId, string AccountLogin);
