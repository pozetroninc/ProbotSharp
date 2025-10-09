// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProbotSharp.Infrastructure.Adapters.Persistence;

namespace ProbotSharp.Infrastructure.Tests.Persistence;

public sealed class MigrationsTests
{
    [Fact]
    public void ApplyMigrations_OnSqliteInMemory_ShouldSucceed()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ProbotSharpDbContext>()
            .UseSqlite(connection)
            .Options;

        using var ctx = new ProbotSharpDbContext(options);
        ctx.Database.Migrate();

        // Smoke check a few tables exist via EF metadata
        ctx.Model.FindEntityType(typeof(ProbotSharp.Infrastructure.Adapters.Persistence.Models.WebhookDeliveryEntity)).Should().NotBeNull();
        ctx.Model.FindEntityType(typeof(ProbotSharp.Infrastructure.Adapters.Persistence.Models.DeadLetterQueueItemEntity)).Should().NotBeNull();
    }
}



