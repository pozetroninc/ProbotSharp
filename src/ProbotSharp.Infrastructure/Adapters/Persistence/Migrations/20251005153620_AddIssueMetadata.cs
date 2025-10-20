using System;

using Microsoft.EntityFrameworkCore.Migrations;

using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ProbotSharp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIssueMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "probot");

            migrationBuilder.CreateTable(
                name: "github_app_manifests",
                schema: "probot",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manifest_json = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_github_app_manifests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_records",
                schema: "probot",
                columns: table => new
                {
                    idempotency_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    metadata = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_records", x => x.idempotency_key);
                });

            migrationBuilder.CreateTable(
                name: "issue_metadata",
                schema: "probot",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    repository_owner = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    repository_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    issue_number = table.Column<int>(type: "integer", nullable: false),
                    key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_issue_metadata", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_deliveries",
                schema: "probot",
                columns: table => new
                {
                    delivery_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    event_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    delivered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    installation_id = table.Column<long>(type: "bigint", nullable: true),
                    payload_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webhook_deliveries", x => x.delivery_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_records_expires_at",
                schema: "probot",
                table: "idempotency_records",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_issue_metadata_composite_key",
                schema: "probot",
                table: "issue_metadata",
                columns: new[] { "repository_owner", "repository_name", "issue_number", "key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "github_app_manifests",
                schema: "probot");

            migrationBuilder.DropTable(
                name: "idempotency_records",
                schema: "probot");

            migrationBuilder.DropTable(
                name: "issue_metadata",
                schema: "probot");

            migrationBuilder.DropTable(
                name: "webhook_deliveries",
                schema: "probot");
        }
    }
}
