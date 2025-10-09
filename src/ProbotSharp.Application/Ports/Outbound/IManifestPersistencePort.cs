// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Application.Ports.Outbound;

public interface IManifestPersistencePort
{
    Task<Result> SaveAsync(string manifestJson, CancellationToken cancellationToken = default);
    Task<Result<string?>> GetAsync(CancellationToken cancellationToken = default);
}

