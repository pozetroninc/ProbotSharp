// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Infrastructure.Adapters.Persistence.Models;

/// <summary>
/// Entity representing persisted GitHub App manifest metadata for reuse across runs.
/// </summary>
public sealed class GitHubAppManifestEntity
{
    /// <summary>Gets or sets the manifest identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the serialized manifest JSON.</summary>
    public string ManifestJson { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the manifest was saved.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
