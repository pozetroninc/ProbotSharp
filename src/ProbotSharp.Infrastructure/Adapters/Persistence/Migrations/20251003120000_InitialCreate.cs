// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace ProbotSharp.Infrastructure.Adapters.Persistence.Migrations;

/// <summary>
/// Initial database migration creating the webhook_deliveries table.
/// </summary>
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "probot");

        migrationBuilder.CreateTable(
            name: "webhook_deliveries",
            schema: "probot",
            columns: table => new
            {
                delivery_id = table.Column<string>(type: "text", maxLength: 128, nullable: false),
                event_name = table.Column<string>(type: "text", maxLength: 128, nullable: false),
                delivered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                payload = table.Column<string>(type: "text", nullable: false),
                installation_id = table.Column<long>(type: "bigint", nullable: true),
                payload_hash = table.Column<string>(type: "text", maxLength: 64, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_webhook_deliveries", x => x.delivery_id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_webhook_deliveries_delivered_at",
            schema: "probot",
            table: "webhook_deliveries",
            column: "delivered_at");

        migrationBuilder.CreateIndex(
            name: "ix_webhook_deliveries_event_name",
            schema: "probot",
            table: "webhook_deliveries",
            column: "event_name");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "webhook_deliveries",
            schema: "probot");
    }
}

