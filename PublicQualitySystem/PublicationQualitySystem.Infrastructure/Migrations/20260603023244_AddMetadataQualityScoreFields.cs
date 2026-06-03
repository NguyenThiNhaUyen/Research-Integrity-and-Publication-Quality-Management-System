using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PublicationQualitySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMetadataQualityScoreFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "metadata_quality_can_proceed",
                table: "paper_metadata",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "metadata_quality_core_score",
                table: "paper_metadata",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "metadata_quality_enrichment_score",
                table: "paper_metadata",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "metadata_quality_extended_score",
                table: "paper_metadata",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "metadata_quality_field_scores_json",
                table: "paper_metadata",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "metadata_quality_grade",
                table: "paper_metadata",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "metadata_quality_missing_fields_json",
                table: "paper_metadata",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "metadata_quality_scored_at",
                table: "paper_metadata",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "metadata_quality_total_score",
                table: "paper_metadata",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "metadata_quality_warnings_json",
                table: "paper_metadata",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "metadata_quality_can_proceed",
                table: "paper_metadata");

            migrationBuilder.DropColumn(
                name: "metadata_quality_core_score",
                table: "paper_metadata");

            migrationBuilder.DropColumn(
                name: "metadata_quality_enrichment_score",
                table: "paper_metadata");

            migrationBuilder.DropColumn(
                name: "metadata_quality_extended_score",
                table: "paper_metadata");

            migrationBuilder.DropColumn(
                name: "metadata_quality_field_scores_json",
                table: "paper_metadata");

            migrationBuilder.DropColumn(
                name: "metadata_quality_grade",
                table: "paper_metadata");

            migrationBuilder.DropColumn(
                name: "metadata_quality_missing_fields_json",
                table: "paper_metadata");

            migrationBuilder.DropColumn(
                name: "metadata_quality_scored_at",
                table: "paper_metadata");

            migrationBuilder.DropColumn(
                name: "metadata_quality_total_score",
                table: "paper_metadata");

            migrationBuilder.DropColumn(
                name: "metadata_quality_warnings_json",
                table: "paper_metadata");
        }
    }
}
