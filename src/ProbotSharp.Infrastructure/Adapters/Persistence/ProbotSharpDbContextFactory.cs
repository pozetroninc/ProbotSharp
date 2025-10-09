// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProbotSharp.Infrastructure.Adapters.Persistence;

/// <summary>
/// Design-time factory for creating database context instances during migrations.
/// </summary>
internal sealed class ProbotSharpDbContextFactory : IDesignTimeDbContextFactory<ProbotSharpDbContext>
{
    /// <inheritdoc />
    public ProbotSharpDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProbotSharpDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=probot-sharp;Username=probot;Password=probot",
            npgsql => npgsql.MigrationsHistoryTable("__efmigrationshistory", "probot"));

        return new ProbotSharpDbContext(optionsBuilder.Options);
    }
}

