// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace ProbotSharp.Infrastructure.Adapters.Persistence.Migrations;

/// <summary>
/// Database migration adding the idempotency_records table for webhook duplicate detection.
/// </summary>
public partial class AddIdempotencyRecords : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "idempotency_records",
            schema: "probot",
            columns: table => new
            {
                idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_idempotency_records", x => x.idempotency_key);
            });

        // Create index on expires_at for efficient cleanup queries
        migrationBuilder.CreateIndex(
            name: "ix_idempotency_records_expires_at",
            schema: "probot",
            table: "idempotency_records",
            column: "expires_at");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "idempotency_records",
            schema: "probot");
    }
}
