using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PublicationQualitySystem.Shared.Common;
using PublicationQualitySystem.Domain.Entities;
using PublicationQualitySystem.Domain.Enums;

namespace PublicationQualitySystem.Infrastructure.Configurations;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Paper> Papers => Set<Paper>();
    public DbSet<PaperVersion> PaperVersions => Set<PaperVersion>();
    public DbSet<PaperMetadata> PaperMetadata => Set<PaperMetadata>();
    public DbSet<PaperSimilarityCheck> PaperSimilarityChecks => Set<PaperSimilarityCheck>();
    public DbSet<PaperProcessingTracker> PaperProcessingTrackers => Set<PaperProcessingTracker>();
    public DbSet<PaperProcessingEvent> PaperProcessingEvents => Set<PaperProcessingEvent>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureBaseEntities(modelBuilder);
        ConfigureUtcDateTimes(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.Property(x => x.Id).HasMaxLength(100).ValueGeneratedNever();
            entity.Property(x => x.FullName).HasColumnName("full_name").IsRequired().HasMaxLength(255);
            entity.Property(x => x.Password).IsRequired();
            entity.Property(x => x.Email).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasMany(x => x.Roles).WithMany(x => x.Users).UsingEntity<Dictionary<string, object>>(
                "user_roles",
                r => r.HasOne<Role>().WithMany().HasForeignKey("role_id"),
                l => l.HasOne<User>().WithMany().HasForeignKey("user_id"));
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasMany(x => x.Permissions).WithMany(x => x.Roles).UsingEntity<Dictionary<string, object>>(
                "role_permissions",
                r => r.HasOne<Permission>().WithMany().HasForeignKey("permission_id"),
                l => l.HasOne<Role>().WithMany().HasForeignKey("role_id"));
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("permissions");
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Paper>(entity =>
        {
            entity.ToTable("papers");
            entity.Property(x => x.Title).HasColumnName("title").IsRequired().HasMaxLength(500);
            entity.Property(x => x.CurrentVersion).HasColumnName("current_version");
            entity.HasMany(x => x.Versions)
                .WithOne(x => x.Paper)
                .HasForeignKey(x => x.PaperId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Metadata)
                .WithOne(x => x.Paper)
                .HasForeignKey<PaperMetadata>(x => x.PaperId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PaperVersion>(entity =>
        {
            entity.ToTable("paper_versions");
            entity.Property(x => x.PaperId).HasColumnName("paper_id");
            entity.Property(x => x.VersionNumber).HasColumnName("version_number");
            entity.Property(x => x.OriginalFileName).HasColumnName("original_file_name").IsRequired().HasMaxLength(500);
            entity.Property(x => x.PdfS3Key).HasColumnName("pdf_s3_key").IsRequired().HasMaxLength(1024);
            entity.Property(x => x.MarkdownS3Key).HasColumnName("markdown_s3_key").HasMaxLength(1024);
            entity.Property(x => x.ConversionStatus)
                .HasColumnName("conversion_status")
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(x => x.ConvertedAt).HasColumnName("converted_at");
            entity.Property(x => x.ConversionError).HasColumnName("conversion_error").HasMaxLength(4000);
            entity.HasIndex(x => new { x.PaperId, x.VersionNumber }).IsUnique();
        });

        modelBuilder.Entity<PaperMetadata>(entity =>
        {
            entity.ToTable("paper_metadata");
            entity.Property(x => x.PaperId).HasColumnName("paper_id");
            entity.Property(x => x.Title).HasColumnName("title").HasMaxLength(500);
            entity.Property(x => x.Abstract).HasColumnName("abstract").HasColumnType("text");
            entity.Property(x => x.Doi).HasColumnName("doi").HasMaxLength(255);
            entity.Property(x => x.ArxivId).HasColumnName("arxiv_id").HasColumnType("text");
            entity.Property(x => x.Journal).HasColumnName("journal").HasMaxLength(500);
            entity.Property(x => x.Publisher).HasColumnName("publisher").HasMaxLength(500);
            entity.Property(x => x.Venue).HasColumnName("venue").HasColumnType("text");
            entity.Property(x => x.ConferenceName).HasColumnName("conference_name").HasColumnType("text");
            entity.Property(x => x.PublicationYear).HasColumnName("publication_year");
            entity.Property(x => x.Volume).HasColumnName("volume").HasMaxLength(100);
            entity.Property(x => x.Issue).HasColumnName("issue").HasMaxLength(100);
            entity.Property(x => x.Pages).HasColumnName("pages").HasMaxLength(100);
            entity.Property(x => x.CorrespondingAuthor).HasColumnName("corresponding_author").HasMaxLength(500);
            entity.Property(x => x.MetadataSource).HasColumnName("metadata_source").HasMaxLength(100);
            entity.Property(x => x.DoiSource).HasColumnName("doi_source").HasMaxLength(100);
            entity.Property(x => x.JournalSource).HasColumnName("journal_source").HasMaxLength(100);
            entity.Property(x => x.KeywordsJson).HasColumnName("keywords_json").HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.FundingOrganizationsJson)
                .HasColumnName("funding_organizations_json")
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'[]'::jsonb")
                .IsRequired();
            entity.Property(x => x.AuthorsJson).HasColumnName("authors_json").HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.ReferencesJson).HasColumnName("references_json").HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.RawGrobidXml).HasColumnName("raw_grobid_xml").HasColumnType("text");
            entity.Property(x => x.ExtractionStatus)
                .HasColumnName("extraction_status")
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(x => x.ExtractionError).HasColumnName("extraction_error").HasMaxLength(4000);
            entity.Property(x => x.ExtractedAt).HasColumnName("extracted_at");
            entity.Property(x => x.MetadataQualityTotalScore).HasColumnName("metadata_quality_total_score");
            entity.Property(x => x.MetadataQualityCoreScore).HasColumnName("metadata_quality_core_score");
            entity.Property(x => x.MetadataQualityExtendedScore).HasColumnName("metadata_quality_extended_score");
            entity.Property(x => x.MetadataQualityEnrichmentScore).HasColumnName("metadata_quality_enrichment_score");
            entity.Property(x => x.MetadataQualityGrade).HasColumnName("metadata_quality_grade").HasMaxLength(50);
            entity.Property(x => x.MetadataQualityCanProceed).HasColumnName("metadata_quality_can_proceed");
            entity.Property(x => x.MetadataQualityMissingFieldsJson).HasColumnName("metadata_quality_missing_fields_json").HasColumnType("jsonb");
            entity.Property(x => x.MetadataQualityWarningsJson).HasColumnName("metadata_quality_warnings_json").HasColumnType("jsonb");
            entity.Property(x => x.MetadataQualityFieldScoresJson).HasColumnName("metadata_quality_field_scores_json").HasColumnType("jsonb");
            entity.Property(x => x.MetadataQualityScoredAt).HasColumnName("metadata_quality_scored_at");
            entity.HasIndex(x => x.PaperId).IsUnique();
        });

        modelBuilder.Entity<PaperSimilarityCheck>(entity =>
        {
            entity.ToTable("paper_similarity_checks");
            entity.Property(x => x.PaperId).HasColumnName("paper_id");
            entity.Property(x => x.PaperMetadataId).HasColumnName("paper_metadata_id");
            entity.Property(x => x.Source).HasColumnName("source").IsRequired().HasMaxLength(100);
            entity.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(x => x.MatchedOpenAlexId).HasColumnName("matched_openalex_id").HasMaxLength(500);
            entity.Property(x => x.MatchedDoi).HasColumnName("matched_doi").HasMaxLength(255);
            entity.Property(x => x.MatchedTitle).HasColumnName("matched_title").HasMaxLength(1000);
            entity.Property(x => x.TitleSimilarity).HasColumnName("title_similarity");
            entity.Property(x => x.AuthorSimilarity).HasColumnName("author_similarity");
            entity.Property(x => x.AbstractSimilarity).HasColumnName("abstract_similarity");
            entity.Property(x => x.ReferenceSimilarity).HasColumnName("reference_similarity");
            entity.Property(x => x.OverallScore).HasColumnName("overall_score");
            entity.Property(x => x.RiskLevel)
                .HasColumnName("risk_level")
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(x => x.SkipReason).HasColumnName("skip_reason").HasMaxLength(1000);
            entity.Property(x => x.ErrorMessage).HasColumnName("error_message").HasMaxLength(4000);
            entity.Property(x => x.RawJson).HasColumnName("raw_json").HasColumnType("jsonb");
            entity.Property(x => x.CheckedAt).HasColumnName("checked_at");
            entity.HasOne(x => x.Paper)
                .WithMany()
                .HasForeignKey(x => x.PaperId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PaperMetadata)
                .WithMany()
                .HasForeignKey(x => x.PaperMetadataId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.PaperId);
            entity.HasIndex(x => x.PaperMetadataId);
            entity.HasIndex(x => x.Source);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.RiskLevel);
            entity.HasIndex(x => x.CheckedAt);
            entity.HasIndex(x => new { x.Source, x.PaperMetadataId }).IsUnique();
        });

        modelBuilder.Entity<PaperProcessingTracker>(entity =>
        {
            entity.ToTable("paper_processing_trackers");
            entity.Property(x => x.PaperId).HasColumnName("paper_id");
            entity.Property(x => x.PaperVersionId).HasColumnName("paper_version_id");
            entity.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
            entity.Property(x => x.CurrentStage).HasColumnName("current_stage").HasConversion<string>().IsRequired().HasDefaultValue(ProcessingStage.UPLOADED).HasMaxLength(100);
            entity.Property(x => x.CurrentStatus).HasColumnName("current_status").HasConversion<string>().IsRequired().HasDefaultValue(ProcessingStatus.PENDING).HasMaxLength(50);
            entity.Property(x => x.OverallStatus).HasColumnName("overall_status").HasConversion<string>().IsRequired().HasDefaultValue(ProcessingStatus.PENDING).HasMaxLength(50);
            entity.Property(x => x.ProgressPercent).HasColumnName("progress_percent");
            entity.Property(x => x.LastError).HasColumnName("last_error").HasMaxLength(1000);
            entity.Property(x => x.RetryCount).HasColumnName("retry_count");
            entity.Property(x => x.StartedAt).HasColumnName("started_at");
            entity.Property(x => x.LastUpdatedAt).HasColumnName("last_updated_at");
            entity.Property(x => x.CompletedAt).HasColumnName("completed_at");

            entity.HasOne(x => x.Paper).WithMany().HasForeignKey(x => x.PaperId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PaperVersion).WithMany().HasForeignKey(x => x.PaperVersionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.PaperId);
            entity.HasIndex(x => x.PaperVersionId).IsUnique();
            entity.HasIndex(x => x.CorrelationId);
            entity.HasIndex(x => x.CurrentStatus);
        });

        modelBuilder.Entity<PaperProcessingEvent>(entity =>
        {
            entity.ToTable("paper_processing_events");
            entity.Property(x => x.PaperId).HasColumnName("paper_id");
            entity.Property(x => x.PaperVersionId).HasColumnName("paper_version_id");
            entity.Property(x => x.TrackerId).HasColumnName("tracker_id");
            entity.Property(x => x.EventId).HasColumnName("event_id").HasMaxLength(100);
            entity.Property(x => x.EventType).HasColumnName("event_type").IsRequired().HasMaxLength(255);
            entity.Property(x => x.Stage).HasColumnName("stage").HasConversion<string>().IsRequired().HasMaxLength(100);
            entity.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);
            entity.Property(x => x.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb");
            entity.Property(x => x.ErrorMessage).HasColumnName("error_message").HasMaxLength(4000);
            entity.HasOne(x => x.Paper).WithMany().HasForeignKey(x => x.PaperId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PaperVersion).WithMany().HasForeignKey(x => x.PaperVersionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Tracker).WithMany(x => x.Events).HasForeignKey(x => x.TrackerId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.PaperId);
            entity.HasIndex(x => x.PaperVersionId);
            entity.HasIndex(x => x.TrackerId);
            entity.HasIndex(x => x.EventId);
            entity.HasIndex(x => x.EventType);
            entity.HasIndex(x => x.Stage);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.Property(x => x.Topic).HasColumnName("topic").IsRequired().HasMaxLength(255);
            entity.Property(x => x.Key).HasColumnName("key").IsRequired().HasMaxLength(255);
            entity.Property(x => x.Type).HasColumnName("type").IsRequired().HasMaxLength(500);
            entity.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(x => x.RetryCount).HasColumnName("retry_count");
            entity.Property(x => x.LastError).HasColumnName("last_error").HasMaxLength(4000);
            entity.Property(x => x.PublishedAt).HasColumnName("published_at");
            entity.HasIndex(x => new { x.Status, x.CreatedAt });
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(x => x.PaperId).HasColumnName("paper_id");
            entity.Property(x => x.PaperVersionId).HasColumnName("paper_version_id");
            entity.Property(x => x.UploadedFileId).HasColumnName("uploaded_file_id");
            entity.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(255);
            entity.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
            entity.Property(x => x.Action).HasColumnName("action").IsRequired().HasMaxLength(255);
            entity.Property(x => x.Step)
                .HasColumnName("step")
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(x => x.Message).HasColumnName("message").HasMaxLength(1000);
            entity.Property(x => x.ErrorMessage).HasColumnName("error_message").HasMaxLength(4000);
            entity.Property(x => x.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
            entity.Property(x => x.StartedAt).HasColumnName("started_at");
            entity.Property(x => x.CompletedAt).HasColumnName("completed_at");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            entity.Property(x => x.Deleted).HasColumnName("deleted");
            entity.Property(x => x.CreatedBy).HasColumnName("created_by");
            entity.Property(x => x.UpdatedBy).HasColumnName("updated_by");
            entity.HasIndex(x => x.PaperId);
            entity.HasIndex(x => x.PaperVersionId);
            entity.HasIndex(x => x.UploadedFileId);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.CorrelationId);
            entity.HasIndex(x => x.Step);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CreatedAt);
        });

   
      
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditTimestamps();
        return base.SaveChanges();
    }

    private static void ConfigureBaseEntities(ModelBuilder modelBuilder)
    {
        var dateOnlyConverter = new ValueConverter<DateOnly?, DateTime?>(
            d => d.HasValue ? DateTime.SpecifyKind(d.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc) : null,
            d => d.HasValue ? DateOnly.FromDateTime(d.Value) : null);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(t => typeof(IAuditableEntity).IsAssignableFrom(t.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType).Property(nameof(IAuditableEntity.CreatedAt)).IsRequired();
            modelBuilder.Entity(entityType.ClrType).Property(nameof(IAuditableEntity.UpdatedAt)).IsRequired();
        }

    }

    private static void ConfigureUtcDateTimes(ModelBuilder modelBuilder)
    {
        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : v.Value.ToUniversalTime()) : null,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var dateTimeProperties = entityType.ClrType
                .GetProperties()
                .Where(p => p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?));

            foreach (var property in dateTimeProperties)
            {
                var propertyBuilder = modelBuilder.Entity(entityType.ClrType).Property(property.Name);
                if (property.PropertyType == typeof(DateTime))
                {
                    propertyBuilder.HasConversion(dateTimeConverter).HasColumnType("timestamp with time zone");
                }
                else
                {
                    propertyBuilder.HasConversion(nullableDateTimeConverter).HasColumnType("timestamp with time zone");
                }
            }
        }
    }

    private void ApplyAuditTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
