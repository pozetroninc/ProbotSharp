// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Events;

public sealed record class GitHubAppCreatedDomainEvent(GitHubAppId AppId, string Name);

