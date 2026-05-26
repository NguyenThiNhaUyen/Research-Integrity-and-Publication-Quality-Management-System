using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PublicationQualitySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadedFilesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_paper_versions_PaperId",
                table: "paper_versions");

            migrationBuilder.AddColumn<string>(
                name: "owner_user_id",
                table: "papers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "research_group_id",
                table: "papers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "change_log",
                table: "paper_versions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "content_type",
                table: "paper_versions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "file_key",
                table: "paper_versions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "original_file_name",
                table: "paper_versions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "size",
                table: "paper_versions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "uploaded_by",
                table: "paper_versions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "uploaded_file_id",
                table: "paper_versions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "version_name",
                table: "paper_versions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "uploaded_files",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    original_file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_key = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    content_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    size = table.Column<long>(type: "bigint", nullable: false),
                    upload_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    uploaded_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uploaded_files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_uploaded_files_users_uploaded_by",
                        column: x => x.uploaded_by,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_papers_owner_user_id",
                table: "papers",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_papers_research_group_id",
                table: "papers",
                column: "research_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_paper_versions_PaperId_version_number",
                table: "paper_versions",
                columns: new[] { "PaperId", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_paper_versions_uploaded_by",
                table: "paper_versions",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "IX_paper_versions_uploaded_file_id",
                table: "paper_versions",
                column: "uploaded_file_id");

            migrationBuilder.CreateIndex(
                name: "IX_uploaded_files_file_key",
                table: "uploaded_files",
                column: "file_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_uploaded_files_uploaded_by",
                table: "uploaded_files",
                column: "uploaded_by");

            migrationBuilder.AddForeignKey(
                name: "FK_paper_versions_uploaded_files_uploaded_file_id",
                table: "paper_versions",
                column: "uploaded_file_id",
                principalTable: "uploaded_files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_paper_versions_users_uploaded_by",
                table: "paper_versions",
                column: "uploaded_by",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_papers_research_groups_research_group_id",
                table: "papers",
                column: "research_group_id",
                principalTable: "research_groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_papers_users_owner_user_id",
                table: "papers",
                column: "owner_user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_paper_versions_uploaded_files_uploaded_file_id",
                table: "paper_versions");

            migrationBuilder.DropForeignKey(
                name: "FK_paper_versions_users_uploaded_by",
                table: "paper_versions");

            migrationBuilder.DropForeignKey(
                name: "FK_papers_research_groups_research_group_id",
                table: "papers");

            migrationBuilder.DropForeignKey(
                name: "FK_papers_users_owner_user_id",
                table: "papers");

            migrationBuilder.DropTable(
                name: "uploaded_files");

            migrationBuilder.DropIndex(
                name: "IX_papers_owner_user_id",
                table: "papers");

            migrationBuilder.DropIndex(
                name: "IX_papers_research_group_id",
                table: "papers");

            migrationBuilder.DropIndex(
                name: "IX_paper_versions_PaperId_version_number",
                table: "paper_versions");

            migrationBuilder.DropIndex(
                name: "IX_paper_versions_uploaded_by",
                table: "paper_versions");

            migrationBuilder.DropIndex(
                name: "IX_paper_versions_uploaded_file_id",
                table: "paper_versions");

            migrationBuilder.DropColumn(
                name: "owner_user_id",
                table: "papers");

            migrationBuilder.DropColumn(
                name: "research_group_id",
                table: "papers");

            migrationBuilder.DropColumn(
                name: "change_log",
                table: "paper_versions");

            migrationBuilder.DropColumn(
                name: "content_type",
                table: "paper_versions");

            migrationBuilder.DropColumn(
                name: "file_key",
                table: "paper_versions");

            migrationBuilder.DropColumn(
                name: "original_file_name",
                table: "paper_versions");

            migrationBuilder.DropColumn(
                name: "size",
                table: "paper_versions");

            migrationBuilder.DropColumn(
                name: "uploaded_by",
                table: "paper_versions");

            migrationBuilder.DropColumn(
                name: "uploaded_file_id",
                table: "paper_versions");

            migrationBuilder.DropColumn(
                name: "version_name",
                table: "paper_versions");

            migrationBuilder.CreateIndex(
                name: "IX_paper_versions_PaperId",
                table: "paper_versions",
                column: "PaperId");
        }
    }
}
