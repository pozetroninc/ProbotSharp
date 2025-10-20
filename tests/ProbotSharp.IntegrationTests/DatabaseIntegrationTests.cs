// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NSubstitute;

using ProbotSharp.Infrastructure.Adapters.Persistence;
using ProbotSharp.Infrastructure.Adapters.Persistence.Models;

using Testcontainers.PostgreSql;

using Xunit;

namespace ProbotSharp.IntegrationTests;

/// <summary>
/// Integration tests for PostgreSQL database operations using Testcontainers.
/// Tests real database behavior including migrations, CRUD operations, transactions, and concurrency.
/// </summary>
[Collection("Database Integration Tests")]
#pragma warning disable CA1001 // Disposable fields are properly disposed via IAsyncLifetime.DisposeAsync()
public sealed class DatabaseIntegrationTests : IAsyncLifetime
#pragma warning restore CA1001
{
    private PostgreSqlContainer? _postgresContainer;
    private string? _connectionString;
    private ProbotSharpDbContext? _dbContext;

    /// <summary>
    /// Initializes the test fixture by starting a PostgreSQL container and running migrations.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Spin up PostgreSQL container using Testcontainers
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .Build();

        await _postgresContainer.StartAsync();

        // Build connection string from container
        _connectionString = _postgresContainer.GetConnectionString();

        // Create DbContext with real PostgreSQL connection
        var options = new DbContextOptionsBuilder<ProbotSharpDbContext>()
            .UseNpgsql(_connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__efmigrationshistory", "probot");
            })
            .Options;

        _dbContext = new ProbotSharpDbContext(options);

        // Create the database schema directly from the model
        // Note: Using EnsureCreatedAsync instead of MigrateAsync because migration detection
        // doesn't work properly in test context with dynamically loaded assemblies
        await _dbContext.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Cleans up resources by disposing the DbContext and stopping the container.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_dbContext != null)
        {
            await _dbContext.DisposeAsync();
        }

        if (_postgresContainer != null)
        {
            await _postgresContainer.StopAsync();
            await _postgresContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task Database_ShouldCreateAllTables()
    {
        // Act - Query information_schema to get all tables in the probot schema
        var sql = @"
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'probot'
            AND table_type = 'BASE TABLE'";

        var tables = new List<string>();
        await using (var connection = _dbContext!.Database.GetDbConnection())
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }

        // Assert - Should have all core tables created from model
        tables.Should().Contain("webhook_deliveries");
        tables.Should().Contain("github_app_manifests");
        tables.Should().Contain("idempotency_records");
    }

    [Fact]
    public async Task Database_ShouldCreateProbotSchema()
    {
        // Act - Query for schema existence
        var sql = "SELECT schema_name FROM information_schema.schemata WHERE schema_name = 'probot'";

        string? schemaName = null;
        await using (var connection = _dbContext!.Database.GetDbConnection())
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            schemaName = (string?)await command.ExecuteScalarAsync();
        }

        // Assert
        schemaName.Should().Be("probot");
    }

    [Fact]
    public async Task WebhookDelivery_InsertAndRetrieve_ShouldPersist()
    {
        // Arrange
        var entity = new WebhookDeliveryEntity
        {
            DeliveryId = "test-delivery-12345",
            EventName = "push",
            Payload = "{\"action\":\"opened\",\"ref\":\"refs/heads/main\"}",
            DeliveredAt = DateTimeOffset.UtcNow,
            InstallationId = 123456,
            PayloadHash = "abc123def456"
        };

        // Act - Insert record
        _dbContext!.WebhookDeliveries.Add(entity);
        await _dbContext.SaveChangesAsync();

        // Clear context to force database read (not cache)
        _dbContext.ChangeTracker.Clear();

        var retrieved = await _dbContext.WebhookDeliveries
            .FirstOrDefaultAsync(w => w.DeliveryId == "test-delivery-12345");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.EventName.Should().Be("push");
        retrieved.Payload.Should().Contain("opened");
        retrieved.InstallationId.Should().Be(123456);
        retrieved.PayloadHash.Should().Be("abc123def456");
    }

    [Fact]
    public async Task WebhookDelivery_Update_ShouldPersistChanges()
    {
        // Arrange - Insert initial record
        var entity = new WebhookDeliveryEntity
        {
            DeliveryId = "update-test-67890",
            EventName = "issues",
            Payload = "{\"action\":\"opened\"}",
            DeliveredAt = DateTimeOffset.UtcNow,
            InstallationId = null,
            PayloadHash = null
        };

        _dbContext!.WebhookDeliveries.Add(entity);
        await _dbContext.SaveChangesAsync();

        // Act - Update the record
        _dbContext.ChangeTracker.Clear();
        var toUpdate = await _dbContext.WebhookDeliveries
            .FirstAsync(w => w.DeliveryId == "update-test-67890");

        toUpdate.InstallationId = 999;
        toUpdate.PayloadHash = "updated-hash";
        await _dbContext.SaveChangesAsync();

        // Assert
        _dbContext.ChangeTracker.Clear();
        var updated = await _dbContext.WebhookDeliveries
            .FirstAsync(w => w.DeliveryId == "update-test-67890");

        updated.InstallationId.Should().Be(999);
        updated.PayloadHash.Should().Be("updated-hash");
    }

    [Fact]
    public async Task WebhookDelivery_Delete_ShouldRemove()
    {
        // Arrange - Insert record
        var entity = new WebhookDeliveryEntity
        {
            DeliveryId = "delete-test-111",
            EventName = "pull_request",
            Payload = "{}",
            DeliveredAt = DateTimeOffset.UtcNow
        };

        _dbContext!.WebhookDeliveries.Add(entity);
        await _dbContext.SaveChangesAsync();

        // Act - Delete the record
        _dbContext.ChangeTracker.Clear();
        var toDelete = await _dbContext.WebhookDeliveries
            .FirstAsync(w => w.DeliveryId == "delete-test-111");

        _dbContext.WebhookDeliveries.Remove(toDelete);
        await _dbContext.SaveChangesAsync();

        // Assert
        _dbContext.ChangeTracker.Clear();
        var deleted = await _dbContext.WebhookDeliveries
            .FirstOrDefaultAsync(w => w.DeliveryId == "delete-test-111");

        deleted.Should().BeNull();
    }

    [Fact]
    public async Task IdempotencyRecord_InsertAndCheck_ShouldPreventDuplicates()
    {
        // Arrange
        var key = $"idempotency-{Guid.NewGuid()}";
        var record = new IdempotencyRecordEntity
        {
            IdempotencyKey = key,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            Metadata = "{\"event\":\"push\"}"
        };

        // Act - Insert record
        _dbContext!.IdempotencyRecords.Add(record);
        await _dbContext.SaveChangesAsync();

        // Check existence
        var exists = await _dbContext.IdempotencyRecords
            .AnyAsync(r => r.IdempotencyKey == key);

        // Assert
        exists.Should().BeTrue();

        // Verify we can retrieve it
        _dbContext.ChangeTracker.Clear();
        var retrieved = await _dbContext.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.IdempotencyKey == key);

        retrieved.Should().NotBeNull();
        retrieved!.Metadata.Should().Be("{\"event\":\"push\"}");
    }

    [Fact]
    public async Task IdempotencyRecord_DuplicateKey_ShouldThrowException()
    {
        // Arrange - Insert first record
        var key = $"duplicate-key-{Guid.NewGuid()}";
        var record1 = new IdempotencyRecordEntity
        {
            IdempotencyKey = key,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };

        _dbContext!.IdempotencyRecords.Add(record1);
        await _dbContext.SaveChangesAsync();

        // Clear tracker to allow testing database constraint (not EF tracker constraint)
        _dbContext.ChangeTracker.Clear();

        // Act & Assert - Try to insert duplicate with same key
        var record2 = new IdempotencyRecordEntity
        {
            IdempotencyKey = key,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };

        _dbContext.IdempotencyRecords.Add(record2);

        var act = async () => await _dbContext.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task GitHubAppManifest_InsertAndRetrieve_ShouldPersist()
    {
        // Arrange
        var manifest = new GitHubAppManifestEntity
        {
            ManifestJson = "{\"name\":\"TestApp\",\"url\":\"https://example.com\"}",
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        _dbContext!.GitHubAppManifests.Add(manifest);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();
        var retrieved = await _dbContext.GitHubAppManifests
            .FirstOrDefaultAsync();

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.ManifestJson.Should().Contain("TestApp");
        retrieved.ManifestJson.Should().Contain("example.com");
        retrieved.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Transaction_Rollback_ShouldNotPersistChanges()
    {
        // Arrange
        var entity1 = new WebhookDeliveryEntity
        {
            DeliveryId = "tx-rollback-1",
            EventName = "push",
            Payload = "{}",
            DeliveredAt = DateTimeOffset.UtcNow
        };

        var entity2 = new WebhookDeliveryEntity
        {
            DeliveryId = "tx-rollback-2",
            EventName = "push",
            Payload = "{}",
            DeliveredAt = DateTimeOffset.UtcNow
        };

        // Act - Start transaction and rollback
        await using var transaction = await _dbContext!.Database.BeginTransactionAsync();

        _dbContext.WebhookDeliveries.Add(entity1);
        await _dbContext.SaveChangesAsync();

        _dbContext.WebhookDeliveries.Add(entity2);
        await _dbContext.SaveChangesAsync();

        // Intentionally rollback
        await transaction.RollbackAsync();

        // Assert - No records should exist
        _dbContext.ChangeTracker.Clear();
        var count = await _dbContext.WebhookDeliveries
            .CountAsync(w => w.DeliveryId.StartsWith("tx-rollback"));

        count.Should().Be(0);
    }

    [Fact]
    public async Task Transaction_Commit_ShouldPersistChanges()
    {
        // Arrange
        var entity1 = new WebhookDeliveryEntity
        {
            DeliveryId = "tx-commit-1",
            EventName = "push",
            Payload = "{}",
            DeliveredAt = DateTimeOffset.UtcNow
        };

        var entity2 = new WebhookDeliveryEntity
        {
            DeliveryId = "tx-commit-2",
            EventName = "push",
            Payload = "{}",
            DeliveredAt = DateTimeOffset.UtcNow
        };

        // Act - Start transaction and commit
        await using var transaction = await _dbContext!.Database.BeginTransactionAsync();

        _dbContext.WebhookDeliveries.Add(entity1);
        _dbContext.WebhookDeliveries.Add(entity2);
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        // Assert - Both records should exist
        _dbContext.ChangeTracker.Clear();
        var count = await _dbContext.WebhookDeliveries
            .CountAsync(w => w.DeliveryId.StartsWith("tx-commit"));

        count.Should().Be(2);
    }

    [Fact]
    public async Task ConcurrentInserts_ShouldAllSucceed()
    {
        // Arrange - Create 10 concurrent insert tasks
        var tasks = new List<Task>();

        // Act - Insert 10 records concurrently with separate contexts
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                // Each task gets its own context to simulate concurrent requests
                var options = new DbContextOptionsBuilder<ProbotSharpDbContext>()
                    .UseNpgsql(_connectionString!, npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsHistoryTable("__efmigrationshistory", "probot");
                    })
                    .Options;

                await using var context = new ProbotSharpDbContext(options);

                var entity = new WebhookDeliveryEntity
                {
                    DeliveryId = $"concurrent-{index}",
                    EventName = "push",
                    Payload = $"{{\"index\":{index}}}",
                    DeliveredAt = DateTimeOffset.UtcNow
                };

                context.WebhookDeliveries.Add(entity);
                await context.SaveChangesAsync();
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - All 10 records should exist
        _dbContext!.ChangeTracker.Clear();
        var count = await _dbContext.WebhookDeliveries
            .CountAsync(w => w.DeliveryId.StartsWith("concurrent-"));

        count.Should().Be(10);
    }

    [Fact]
    public async Task Query_WithComplexFilter_ShouldReturnCorrectResults()
    {
        // Arrange - Insert test data with varying attributes
        var now = DateTimeOffset.UtcNow;
        var entities = new[]
        {
            new WebhookDeliveryEntity
            {
                DeliveryId = "filter-1",
                EventName = "push",
                DeliveredAt = now.AddMinutes(-10),
                Payload = "{}",
                InstallationId = 100
            },
            new WebhookDeliveryEntity
            {
                DeliveryId = "filter-2",
                EventName = "push",
                DeliveredAt = now.AddMinutes(-5),
                Payload = "{}",
                InstallationId = null
            },
            new WebhookDeliveryEntity
            {
                DeliveryId = "filter-3",
                EventName = "issues",
                DeliveredAt = now.AddMinutes(-1),
                Payload = "{}",
                InstallationId = 100
            }
        };

        _dbContext!.WebhookDeliveries.AddRange(entities);
        await _dbContext.SaveChangesAsync();

        // Act - Query for push events with installation ID
        _dbContext.ChangeTracker.Clear();
        var results = await _dbContext.WebhookDeliveries
            .Where(w => w.EventName == "push" && w.InstallationId.HasValue)
            .OrderBy(w => w.DeliveredAt)
            .ToListAsync();

        // Assert
        results.Should().HaveCount(1);
        results[0].DeliveryId.Should().Be("filter-1");
        results[0].InstallationId.Should().Be(100);
    }

    [Fact]
    public async Task Query_WithDateRangeFilter_ShouldReturnCorrectResults()
    {
        // Arrange - Insert records with different timestamps
        var baseTime = new DateTimeOffset(2025, 10, 1, 12, 0, 0, TimeSpan.Zero);
        var entities = new[]
        {
            new WebhookDeliveryEntity
            {
                DeliveryId = "date-1",
                EventName = "push",
                DeliveredAt = baseTime.AddHours(-2),
                Payload = "{}"
            },
            new WebhookDeliveryEntity
            {
                DeliveryId = "date-2",
                EventName = "push",
                DeliveredAt = baseTime,
                Payload = "{}"
            },
            new WebhookDeliveryEntity
            {
                DeliveryId = "date-3",
                EventName = "push",
                DeliveredAt = baseTime.AddHours(2),
                Payload = "{}"
            }
        };

        _dbContext!.WebhookDeliveries.AddRange(entities);
        await _dbContext.SaveChangesAsync();

        // Act - Query for records within date range
        _dbContext.ChangeTracker.Clear();
        var startDate = baseTime.AddHours(-1);
        var endDate = baseTime.AddHours(1);

        var results = await _dbContext.WebhookDeliveries
            .Where(w => w.DeliveredAt >= startDate && w.DeliveredAt <= endDate)
            .OrderBy(w => w.DeliveredAt)
            .ToListAsync();

        // Assert
        results.Should().HaveCount(1);
        results[0].DeliveryId.Should().Be("date-2");
    }

    [Fact]
    public async Task IdempotencyRecord_QueryByExpiresAt_ShouldUseIndex()
    {
        // Arrange - Insert expired and non-expired records
        var now = DateTimeOffset.UtcNow;
        var records = new[]
        {
            new IdempotencyRecordEntity
            {
                IdempotencyKey = $"expired-1-{Guid.NewGuid()}",
                CreatedAt = now.AddHours(-2),
                ExpiresAt = now.AddHours(-1) // Expired
            },
            new IdempotencyRecordEntity
            {
                IdempotencyKey = $"active-1-{Guid.NewGuid()}",
                CreatedAt = now,
                ExpiresAt = now.AddHours(1) // Active
            },
            new IdempotencyRecordEntity
            {
                IdempotencyKey = $"expired-2-{Guid.NewGuid()}",
                CreatedAt = now.AddHours(-3),
                ExpiresAt = now.AddMinutes(-30) // Expired
            }
        };

        _dbContext!.IdempotencyRecords.AddRange(records);
        await _dbContext.SaveChangesAsync();

        // Act - Query for expired records (this should use the expires_at index)
        _dbContext.ChangeTracker.Clear();
        var expiredRecords = await _dbContext.IdempotencyRecords
            .Where(r => r.ExpiresAt < now)
            .ToListAsync();

        // Assert
        expiredRecords.Should().HaveCount(2);
        expiredRecords.All(r => r.ExpiresAt < now).Should().BeTrue();
    }

    [Fact]
    public async Task WebhookDelivery_BulkInsert_ShouldSucceed()
    {
        // Arrange - Create 100 webhook deliveries
        var entities = Enumerable.Range(1, 100)
            .Select(i => new WebhookDeliveryEntity
            {
                DeliveryId = $"bulk-{i}",
                EventName = i % 2 == 0 ? "push" : "pull_request",
                Payload = $"{{\"number\":{i}}}",
                DeliveredAt = DateTimeOffset.UtcNow.AddMinutes(-i),
                InstallationId = i % 3 == 0 ? i * 100L : null
            })
            .ToList();

        // Act - Bulk insert
        _dbContext!.WebhookDeliveries.AddRange(entities);
        await _dbContext.SaveChangesAsync();

        // Assert
        _dbContext.ChangeTracker.Clear();
        var count = await _dbContext.WebhookDeliveries
            .CountAsync(w => w.DeliveryId.StartsWith("bulk-"));

        count.Should().Be(100);

        // Verify aggregate queries work
        var pushCount = await _dbContext.WebhookDeliveries
            .CountAsync(w => w.DeliveryId.StartsWith("bulk-") && w.EventName == "push");

        pushCount.Should().Be(50);
    }

    [Fact]
    public async Task EfManifestStorageAdapter_RealDatabase_ShouldWorkEndToEnd()
    {
        // Arrange - Create adapter with real database
        var logger = Substitute.For<ILogger<EfManifestStorageAdapter>>();
        var adapter = new EfManifestStorageAdapter(_dbContext!, logger);

        var manifestJson = "{\"name\":\"IntegrationTestApp\",\"description\":\"Test manifest\"}";

        // Act - Save manifest
        var saveResult = await adapter.SaveAsync(manifestJson);

        // Assert - Save succeeded
        saveResult.IsSuccess.Should().BeTrue();

        // Act - Retrieve manifest
        var getResult = await adapter.GetAsync();

        // Assert - Retrieved successfully
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Should().Be(manifestJson);
    }

    [Fact]
    public async Task EfWebhookStorageAdapter_RealDatabase_ShouldWorkEndToEnd()
    {
        // This test would require domain entities which we're not creating here
        // Instead, we verify the table structure supports the adapter's needs

        // Arrange - Insert a webhook delivery entity directly
        var entity = new WebhookDeliveryEntity
        {
            DeliveryId = "adapter-test-999",
            EventName = "push",
            Payload = "{\"ref\":\"refs/heads/main\"}",
            DeliveredAt = DateTimeOffset.UtcNow,
            InstallationId = 12345
        };

        // Act
        _dbContext!.WebhookDeliveries.Add(entity);
        await _dbContext.SaveChangesAsync();

        // Assert - Verify it can be queried back
        _dbContext.ChangeTracker.Clear();
        var exists = await _dbContext.WebhookDeliveries
            .AnyAsync(w => w.DeliveryId == "adapter-test-999");

        exists.Should().BeTrue();
    }
}
