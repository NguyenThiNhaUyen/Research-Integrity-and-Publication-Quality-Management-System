using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PublicationQualitySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizePaperProcessingEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;");

            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_paper_processing_events_event_id\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_PaperProcessingEvents_EventId\";");

            migrationBuilder.AddColumn<string>(
                name: "correlation_id",
                table: "paper_processing_events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE paper_processing_trackers
                SET correlation_id = replace(gen_random_uuid()::text, '-', '')
                WHERE correlation_id IS NULL OR btrim(correlation_id) = '';
                """);

            migrationBuilder.Sql(
                """
                UPDATE paper_processing_events
                SET correlation_id = NULLIF(payload_json ->> 'correlationId', '')
                WHERE payload_json IS NOT NULL
                  AND (correlation_id IS NULL OR btrim(correlation_id) = '');
                """);

            migrationBuilder.Sql(
                """
                UPDATE paper_processing_events AS e
                SET correlation_id = t.correlation_id
                FROM paper_processing_trackers AS t
                WHERE e.tracker_id = t."Id"
                  AND t.correlation_id IS NOT NULL
                  AND btrim(t.correlation_id) <> ''
                  AND (e.correlation_id IS NULL OR btrim(e.correlation_id) = '');
                """);

            migrationBuilder.Sql(
                """
                UPDATE paper_processing_events
                SET correlation_id = replace(gen_random_uuid()::text, '-', '')
                WHERE correlation_id IS NULL OR btrim(correlation_id) = '';
                """);

            migrationBuilder.Sql(
                """
                UPDATE paper_processing_events
                SET payload_json = '{}'::jsonb
                WHERE payload_json IS NULL;
                """);

            migrationBuilder.Sql(
                """
                WITH ranked_events AS (
                    SELECT
                        "Id",
                        event_id,
                        row_number() OVER (
                            PARTITION BY event_id
                            ORDER BY "CreatedAt", "Id"
                        ) AS duplicate_rank
                    FROM paper_processing_events
                    WHERE event_id IS NOT NULL AND btrim(event_id) <> ''
                )
                UPDATE paper_processing_events AS e
                SET event_id = replace(gen_random_uuid()::text, '-', '')
                FROM ranked_events AS ranked
                WHERE e."Id" = ranked."Id"
                  AND (
                      ranked.event_id !~ '^[0-9a-fA-F]{32}$'
                      OR ranked.duplicate_rank > 1
                  );
                """);

            migrationBuilder.Sql(
                """
                UPDATE paper_processing_events
                SET event_id = replace(gen_random_uuid()::text, '-', '')
                WHERE event_id IS NULL
                   OR btrim(event_id) = ''
                   OR event_id !~ '^[0-9a-fA-F]{32}$';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "event_id",
                table: "paper_processing_events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "correlation_id",
                table: "paper_processing_events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "payload_json",
                table: "paper_processing_events",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_paper_processing_events_correlation_id",
                table: "paper_processing_events",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_PaperProcessingEvents_EventId",
                table: "paper_processing_events",
                column: "event_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_paper_processing_events_correlation_id",
                table: "paper_processing_events");

            migrationBuilder.DropIndex(
                name: "IX_PaperProcessingEvents_EventId",
                table: "paper_processing_events");

            migrationBuilder.DropColumn(
                name: "correlation_id",
                table: "paper_processing_events");

            migrationBuilder.AlterColumn<string>(
                name: "payload_json",
                table: "paper_processing_events",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValueSql: "'{}'::jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "event_id",
                table: "paper_processing_events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_paper_processing_events_event_id",
                table: "paper_processing_events",
                column: "event_id");
        }
    }
}
