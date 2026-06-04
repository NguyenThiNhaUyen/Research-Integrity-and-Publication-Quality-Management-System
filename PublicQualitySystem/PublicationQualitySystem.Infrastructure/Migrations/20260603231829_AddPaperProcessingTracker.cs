using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PublicationQualitySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaperProcessingTracker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "paper_processing_trackers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paper_id = table.Column<long>(type: "bigint", nullable: false),
                    paper_version_id = table.Column<long>(type: "bigint", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    overall_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    current_step = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    progress_percent = table.Column<int>(type: "integer", nullable: false),
                    upload_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    markdown_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    metadata_extraction_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    metadata_quality_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    openalex_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    crossref_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ai_review_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    integrity_screening_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    paper_uploaded_event_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    markdown_requested_event_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    markdown_generated_event_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    metadata_extraction_requested_event_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    metadata_extracted_event_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    metadata_quality_scored_event_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    openalex_requested_event_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    openalex_completed_event_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    openalex_skipped_event_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    last_failed_step = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    error_details_json = table.Column<string>(type: "jsonb", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    last_retry_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_retry_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    upload_completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    markdown_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    markdown_completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    metadata_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    metadata_completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    metadata_quality_completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    openalex_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    openalex_completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    step_details_json = table.Column<string>(type: "jsonb", nullable: true),
                    warnings_json = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paper_processing_trackers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_paper_processing_trackers_paper_versions_paper_version_id",
                        column: x => x.paper_version_id,
                        principalTable: "paper_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_paper_processing_trackers_papers_paper_id",
                        column: x => x.paper_id,
                        principalTable: "papers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_paper_processing_trackers_correlation_id",
                table: "paper_processing_trackers",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_paper_processing_trackers_overall_status",
                table: "paper_processing_trackers",
                column: "overall_status");

            migrationBuilder.CreateIndex(
                name: "IX_paper_processing_trackers_paper_id",
                table: "paper_processing_trackers",
                column: "paper_id");

            migrationBuilder.CreateIndex(
                name: "IX_paper_processing_trackers_paper_version_id",
                table: "paper_processing_trackers",
                column: "paper_version_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "paper_processing_trackers");
        }
    }
}
