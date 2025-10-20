// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.Events;

/// <summary>
/// Domain event raised when a GitHub App manifest is created.
/// </summary>
/// <param name="ManifestJson">The JSON representation of the manifest.</param>
/// <param name="CreatedAt">The timestamp when the manifest was created.</param>
public sealed record class ManifestCreatedDomainEvent(string ManifestJson, DateTimeOffset CreatedAt);
