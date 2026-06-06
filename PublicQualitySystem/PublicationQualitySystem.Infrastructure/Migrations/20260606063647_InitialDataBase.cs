using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PublicationQualitySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialDataBase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    paper_id = table.Column<long>(type: "bigint", nullable: true),
                    paper_version_id = table.Column<long>(type: "bigint", nullable: true),
                    uploaded_file_id = table.Column<long>(type: "bigint", nullable: true),
                    user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    action = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    step = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    error_message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    topic = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "papers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    current_version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_papers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "paper_metadata",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paper_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    @abstract = table.Column<string>(name: "abstract", type: "text", nullable: true),
                    doi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    arxiv_id = table.Column<string>(type: "text", nullable: true),
                    journal = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    publisher = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    venue = table.Column<string>(type: "text", nullable: true),
                    conference_name = table.Column<string>(type: "text", nullable: true),
                    publication_year = table.Column<int>(type: "integer", nullable: true),
                    volume = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    issue = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    pages = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    corresponding_author = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    received_date = table.Column<DateOnly>(type: "date", nullable: true),
                    revised_date = table.Column<DateOnly>(type: "date", nullable: true),
                    accepted_date = table.Column<DateOnly>(type: "date", nullable: true),
                    published_date = table.Column<DateOnly>(type: "date", nullable: true),
                    open_access_license = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    metadata_source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    doi_source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    journal_source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    keywords_json = table.Column<string>(type: "jsonb", nullable: false),
                    funding_organizations_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    authors_json = table.Column<string>(type: "jsonb", nullable: false),
                    references_json = table.Column<string>(type: "jsonb", nullable: false),
                    raw_metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    normalized_metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    metadata_cleanliness_score = table.Column<int>(type: "integer", nullable: true),
                    reference_cleanliness_score = table.Column<int>(type: "integer", nullable: true),
                    dirty_field_count = table.Column<int>(type: "integer", nullable: true),
                    metadata_issue_codes_json = table.Column<string>(type: "jsonb", nullable: true),
                    metadata_warnings_json = table.Column<string>(type: "jsonb", nullable: true),
                    raw_grobid_xml = table.Column<string>(type: "text", nullable: true),
                    extraction_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    extraction_error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    extracted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    metadata_quality_total_score = table.Column<int>(type: "integer", nullable: true),
                    metadata_quality_core_score = table.Column<int>(type: "integer", nullable: true),
                    metadata_quality_extended_score = table.Column<int>(type: "integer", nullable: true),
                    metadata_quality_enrichment_score = table.Column<int>(type: "integer", nullable: true),
                    metadata_quality_grade = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    metadata_quality_can_proceed = table.Column<bool>(type: "boolean", nullable: true),
                    metadata_quality_missing_fields_json = table.Column<string>(type: "jsonb", nullable: true),
                    metadata_quality_warnings_json = table.Column<string>(type: "jsonb", nullable: true),
                    metadata_quality_field_scores_json = table.Column<string>(type: "jsonb", nullable: true),
                    metadata_quality_scored_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paper_metadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_paper_metadata_papers_paper_id",
                        column: x => x.paper_id,
                        principalTable: "papers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "paper_versions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paper_id = table.Column<long>(type: "bigint", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    pdf_s3_key = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    markdown_s3_key = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    conversion_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    converted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    conversion_error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paper_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_paper_versions_papers_paper_id",
                        column: x => x.paper_id,
                        principalTable: "papers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    permission_id = table.Column<long>(type: "bigint", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => new { x.permission_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.role_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "paper_doi_checks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paper_id = table.Column<long>(type: "bigint", nullable: false),
                    paper_metadata_id = table.Column<long>(type: "bigint", nullable: false),
                    paper_version_id = table.Column<long>(type: "bigint", nullable: true),
                    main_doi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    main_doi_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    main_doi_title_similarity = table.Column<double>(type: "double precision", nullable: true),
                    main_doi_year_matched = table.Column<bool>(type: "boolean", nullable: true),
                    main_doi_matched_title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    main_doi_matched_publisher = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    main_doi_issue_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    main_doi_issue_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    main_doi_raw_json = table.Column<string>(type: "jsonb", nullable: true),
                    total_references = table.Column<int>(type: "integer", nullable: false),
                    references_with_doi = table.Column<int>(type: "integer", nullable: false),
                    references_missing_doi = table.Column<int>(type: "integer", nullable: false),
                    valid_reference_dois = table.Column<int>(type: "integer", nullable: false),
                    invalid_reference_dois = table.Column<int>(type: "integer", nullable: false),
                    reference_doi_title_mismatches = table.Column<int>(type: "integer", nullable: false),
                    reference_doi_coverage_percent = table.Column<double>(type: "double precision", nullable: false),
                    missing_reference_authors = table.Column<int>(type: "integer", nullable: false),
                    missing_reference_venues = table.Column<int>(type: "integer", nullable: false),
                    low_confidence_references = table.Column<int>(type: "integer", nullable: false),
                    duplicate_references = table.Column<int>(type: "integer", nullable: false),
                    reference_cleanliness_issues = table.Column<int>(type: "integer", nullable: false),
                    reference_quality_score = table.Column<int>(type: "integer", nullable: false),
                    overall_score = table.Column<int>(type: "integer", nullable: false),
                    risk_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    error_message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    raw_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paper_doi_checks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_paper_doi_checks_paper_metadata_paper_metadata_id",
                        column: x => x.paper_metadata_id,
                        principalTable: "paper_metadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_paper_doi_checks_paper_versions_paper_version_id",
                        column: x => x.paper_version_id,
                        principalTable: "paper_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_paper_doi_checks_papers_paper_id",
                        column: x => x.paper_id,
                        principalTable: "papers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "paper_processing_trackers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paper_id = table.Column<long>(type: "bigint", nullable: false),
                    paper_version_id = table.Column<long>(type: "bigint", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    current_stage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "UPLOADED"),
                    current_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "PENDING"),
                    overall_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "PENDING"),
                    progress_percent = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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

            migrationBuilder.CreateTable(
                name: "paper_reference_doi_checks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paper_doi_check_id = table.Column<long>(type: "bigint", nullable: false),
                    reference_ordinal = table.Column<int>(type: "integer", nullable: false),
                    raw_text = table.Column<string>(type: "text", nullable: true),
                    extracted_doi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    extracted_title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    extracted_year = table.Column<int>(type: "integer", nullable: true),
                    normalized_title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    normalized_doi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    normalized_journal = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    normalized_year = table.Column<int>(type: "integer", nullable: true),
                    doi_format_valid = table.Column<bool>(type: "boolean", nullable: false),
                    validation_source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    validation_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    matched_doi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    matched_title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    matched_publisher = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    matched_year = table.Column<int>(type: "integer", nullable: true),
                    title_similarity = table.Column<double>(type: "double precision", nullable: true),
                    year_matched = table.Column<bool>(type: "boolean", nullable: true),
                    issue_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    issue_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    metadata_completeness_score = table.Column<int>(type: "integer", nullable: false),
                    parse_confidence_score = table.Column<double>(type: "double precision", nullable: false),
                    issue_codes_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    raw_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paper_reference_doi_checks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_paper_reference_doi_checks_paper_doi_checks_paper_doi_check~",
                        column: x => x.paper_doi_check_id,
                        principalTable: "paper_doi_checks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "paper_processing_events",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paper_id = table.Column<long>(type: "bigint", nullable: false),
                    paper_version_id = table.Column<long>(type: "bigint", nullable: false),
                    tracker_id = table.Column<long>(type: "bigint", nullable: false),
                    event_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    event_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    stage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    error_message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paper_processing_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_paper_processing_events_paper_processing_trackers_tracker_id",
                        column: x => x.tracker_id,
                        principalTable: "paper_processing_trackers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_paper_processing_events_paper_versions_paper_version_id",
                        column: x => x.paper_version_id,
                        principalTable: "paper_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_paper_processing_events_papers_paper_id",
                        column: x => x.paper_id,
                        principalTable: "papers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_correlation_id",
                table: "audit_logs",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_created_at",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_paper_id",
                table: "audit_logs",
                column: "paper_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_paper_version_id",
                table: "audit_logs",
                column: "paper_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_status",
                table: "audit_logs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_step",
                table: "audit_logs",
                column: "step");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_uploaded_file_id",
                table: "audit_logs",
                column: "uploaded_file_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_status_CreatedAt",
                table: "outbox_messages",
                columns: new[] { "status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_paper_doi_checks_paper_id",
                table: "paper_doi_checks",
                column: "paper_id");

            migrationBuilder.CreateIndex(
                name: "IX_paper_doi_checks_paper_metadata_id",
                table: "paper_doi_checks",
                column: "paper_metadata_id");

            migrationBuilder.CreateIndex(
                name: "IX_paper_doi_checks_paper_version_id",
                table: "paper_doi_checks",
                column: "paper_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_paper_doi_checks_risk_level",
                table: "paper_doi_checks",
                column: "risk_level");

            migrationBuilder.CreateIndex(
                name: "IX_paper_doi_checks_status",
                table: "paper_doi_checks",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_paper_metadata_paper_id",
                table: "paper_metadata",
                column: "paper_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_paper_processing_events_correlation_id",
                table: "paper_processing_events",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_paper_processing_events_CreatedAt",
                table: "paper_processing_events",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_paper_processing_events_event_type",
                table: "paper_processing_events",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "IX_paper_processing_events_paper_id",
                table: "paper_processing_events",
                column: "paper_id");

            migrationBuilder.CreateIndex(
                name: "IX_paper_processing_events_paper_version_id",
                table: "paper_processing_events",
                column: "paper_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_paper_processing_events_stage",
                table: "paper_processing_events",
                column: "stage");

            migrationBuilder.CreateIndex(
                name: "IX_paper_processing_events_status",
                table: "paper_processing_events",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_paper_processing_events_tracker_id",
                table: "paper_processing_events",
                column: "tracker_id");

            migrationBuilder.CreateIndex(
                name: "IX_PaperProcessingEvents_EventId",
                table: "paper_processing_events",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_paper_processing_trackers_correlation_id",
                table: "paper_processing_trackers",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_paper_processing_trackers_current_status",
                table: "paper_processing_trackers",
                column: "current_status");

            migrationBuilder.CreateIndex(
                name: "IX_paper_processing_trackers_paper_id",
                table: "paper_processing_trackers",
                column: "paper_id");

            migrationBuilder.CreateIndex(
                name: "IX_paper_processing_trackers_paper_version_id",
                table: "paper_processing_trackers",
                column: "paper_version_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_paper_reference_doi_checks_extracted_doi",
                table: "paper_reference_doi_checks",
                column: "extracted_doi");

            migrationBuilder.CreateIndex(
                name: "IX_paper_reference_doi_checks_normalized_doi",
                table: "paper_reference_doi_checks",
                column: "normalized_doi");

            migrationBuilder.CreateIndex(
                name: "IX_paper_reference_doi_checks_paper_doi_check_id",
                table: "paper_reference_doi_checks",
                column: "paper_doi_check_id");

            migrationBuilder.CreateIndex(
                name: "IX_paper_reference_doi_checks_validation_status",
                table: "paper_reference_doi_checks",
                column: "validation_status");

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

            migrationBuilder.CreateIndex(
                name: "IX_paper_versions_paper_id_version_number",
                table: "paper_versions",
                columns: new[] { "paper_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_permissions_Name",
                table: "permissions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_role_id",
                table: "role_permissions",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_roles_Name",
                table: "roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_user_id",
                table: "user_roles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "paper_processing_events");

            migrationBuilder.DropTable(
                name: "paper_reference_doi_checks");

            migrationBuilder.DropTable(
                name: "paper_similarity_checks");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "paper_processing_trackers");

            migrationBuilder.DropTable(
                name: "paper_doi_checks");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "paper_metadata");

            migrationBuilder.DropTable(
                name: "paper_versions");

            migrationBuilder.DropTable(
                name: "papers");
        }
    }
}
