using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PublicationQualitySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaperReviewProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "paper_review_profiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paper_id = table.Column<long>(type: "bigint", nullable: false),
                    target_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    research_field = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    paper_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    review_goal = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paper_review_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_paper_review_profiles_papers_paper_id",
                        column: x => x.paper_id,
                        principalTable: "papers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_paper_review_profiles_paper_id",
                table: "paper_review_profiles",
                column: "paper_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "paper_review_profiles");
        }
    }
}
