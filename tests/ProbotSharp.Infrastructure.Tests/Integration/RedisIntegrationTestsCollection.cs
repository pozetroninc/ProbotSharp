// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Xunit;

namespace ProbotSharp.Infrastructure.Tests.Integration;

/// <summary>
/// Defines a test collection for Redis integration tests to ensure they run sequentially.
/// This prevents Docker container conflicts when using Testcontainers.
/// </summary>
[CollectionDefinition("Redis Integration Tests", DisableParallelization = true)]
public class RedisIntegrationTestsCollection
{
}
