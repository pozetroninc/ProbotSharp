// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.Events;

public sealed record class ManifestCreatedDomainEvent(string ManifestJson, DateTimeOffset CreatedAt);
