using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PublicationQualitySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScholarlyMetadataFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "corresponding_author",
                table: "paper_metadata",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "doi_source",
                table: "paper_metadata",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "issue",
                table: "paper_metadata",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "journal_source",
                table: "paper_metadata",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "metadata_source",
                table: "paper_metadata",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pages",
                table: "paper_metadata",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "volume",
                table: "paper_metadata",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "corresponding_author",
                table: "paper_metadata");

            migrationBuilder.DropColumn(
                name: "doi_source",
                table: "paper_metadata");

            migrationBuilder.DropColumn(
                name: "issue",
                table: "paper_metadata");

            migrationBuilder.DropColumn(
                name: "journal_source",
                table: "paper_metadata");

            migrationBuilder.DropColumn(
                name: "metadata_source",
                table: "paper_metadata");

            migrationBuilder.DropColumn(
                name: "pages",
                table: "paper_metadata");

            migrationBuilder.DropColumn(
                name: "volume",
                table: "paper_metadata");
        }
    }
}
