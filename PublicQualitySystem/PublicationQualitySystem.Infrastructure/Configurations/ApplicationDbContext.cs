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
