using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PublicationQualitySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaperMetadataArxivVenueConference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "arxiv_id",
                table: "paper_metadata",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "conference_name",
                table: "paper_metadata",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "venue",
                table: "paper_metadata",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "arxiv_id",
                table: "paper_metadata");

            migrationBuilder.DropColumn(
                name: "conference_name",
                table: "paper_metadata");

            migrationBuilder.DropColumn(
                name: "venue",
                table: "paper_metadata");
        }
    }
}
