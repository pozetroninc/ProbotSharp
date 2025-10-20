using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProbotSharp.Infrastructure.Adapters.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeadLetterQueueTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dead_letter_queue_items",
                schema: "probot",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    delivery_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    event_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    installation_id = table.Column<long>(type: "bigint", nullable: true),
                    signature = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    raw_payload = table.Column<string>(type: "text", nullable: false),
                    attempt = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    failed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dead_letter_queue_items", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dead_letter_queue_items_delivery_id",
                schema: "probot",
                table: "dead_letter_queue_items",
                column: "delivery_id");

            migrationBuilder.CreateIndex(
                name: "ix_dead_letter_queue_items_failed_at",
                schema: "probot",
                table: "dead_letter_queue_items",
                column: "failed_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dead_letter_queue_items",
                schema: "probot");
        }
    }
}
