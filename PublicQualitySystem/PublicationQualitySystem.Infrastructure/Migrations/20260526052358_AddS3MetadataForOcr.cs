using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PublicationQualitySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddS3MetadataForOcr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "s3_bucket",
                table: "uploaded_files",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "s3_key",
                table: "uploaded_files",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "s3_bucket",
                table: "papers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "s3_key",
                table: "papers",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE uploaded_files
                SET s3_key = file_key
                WHERE s3_key = '' AND file_key IS NOT NULL AND file_key <> '';
                """);

            migrationBuilder.Sql("""
                UPDATE papers
                SET s3_key = file_url
                WHERE s3_key IS NULL
                  AND file_url IS NOT NULL
                  AND file_url <> ''
                  AND file_url NOT LIKE 'http%://%';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_uploaded_files_s3_key",
                table: "uploaded_files",
                column: "s3_key");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_uploaded_files_s3_key",
                table: "uploaded_files");

            migrationBuilder.DropColumn(
                name: "s3_bucket",
                table: "uploaded_files");

            migrationBuilder.DropColumn(
                name: "s3_key",
                table: "uploaded_files");

            migrationBuilder.DropColumn(
                name: "s3_bucket",
                table: "papers");

            migrationBuilder.DropColumn(
                name: "s3_key",
                table: "papers");
        }
    }
}
