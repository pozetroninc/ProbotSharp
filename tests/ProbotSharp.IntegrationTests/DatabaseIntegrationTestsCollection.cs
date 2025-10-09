// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Xunit;

namespace ProbotSharp.IntegrationTests;

/// <summary>
/// Defines a test collection for Database integration tests to ensure they run sequentially.
/// This prevents Docker container conflicts when using Testcontainers.
/// </summary>
[CollectionDefinition("Database Integration Tests", DisableParallelization = true)]
public class DatabaseIntegrationTestsCollection
{
}
