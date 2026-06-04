using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PublicationQualitySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaperSimilarityChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "paper_similarity_checks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paper_id = table.Column<long>(type: "bigint", nullable: false),
                    paper_metadata_id = table.Column<long>(type: "bigint", nullable: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    matched_openalex_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    matched_doi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    matched_title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    title_similarity = table.Column<double>(type: "double precision", nullable: true),
                    author_similarity = table.Column<double>(type: "double precision", nullable: true),
                    abstract_similarity = table.Column<double>(type: "double precision", nullable: true),
                    reference_similarity = table.Column<double>(type: "double precision", nullable: true),
                    overall_score = table.Column<double>(type: "double precision", nullable: true),
                    risk_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    skip_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    error_message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    raw_json = table.Column<string>(type: "jsonb", nullable: true),
                    checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paper_similarity_checks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_paper_similarity_checks_paper_metadata_paper_metadata_id",
                        column: x => x.paper_metadata_id,
                        principalTable: "paper_metadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_paper_similarity_checks_papers_paper_id",
                        column: x => x.paper_id,
                        principalTable: "papers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_paper_similarity_checks_checked_at",
                table: "paper_similarity_checks",
                column: "checked_at");

            migrationBuilder.CreateIndex(
                name: "IX_paper_similarity_checks_paper_id",
                table: "paper_similarity_checks",
                column: "paper_id");

            migrationBuilder.CreateIndex(
                name: "IX_paper_similarity_checks_paper_metadata_id",
                table: "paper_similarity_checks",
                column: "paper_metadata_id");

            migrationBuilder.CreateIndex(
                name: "IX_paper_similarity_checks_risk_level",
                table: "paper_similarity_checks",
                column: "risk_level");

            migrationBuilder.CreateIndex(
                name: "IX_paper_similarity_checks_source",
                table: "paper_similarity_checks",
                column: "source");

            migrationBuilder.CreateIndex(
                name: "IX_paper_similarity_checks_source_paper_metadata_id",
                table: "paper_similarity_checks",
                columns: new[] { "source", "paper_metadata_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_paper_similarity_checks_status",
                table: "paper_similarity_checks",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "paper_similarity_checks");
        }
    }
}
