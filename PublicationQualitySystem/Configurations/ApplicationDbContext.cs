using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PublicationQualitySystem.Common;
using PublicationQualitySystem.Entities;
using PublicationQualitySystem.Enums;

namespace PublicationQualitySystem.Configurations;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<ResearchProfile> ResearchProfiles => Set<ResearchProfile>();
    public DbSet<ResearchGroup> ResearchGroups => Set<ResearchGroup>();
    public DbSet<ResearchGroupMember> ResearchGroupMembers => Set<ResearchGroupMember>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Paper> Papers => Set<Paper>();
    public DbSet<PaperAuthor> PaperAuthors => Set<PaperAuthor>();
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

        modelBuilder.Entity<ResearchProfile>(entity =>
        {
            entity.ToTable("research_profiles");
            entity.Property(x => x.Institution).IsRequired().HasMaxLength(255);
            entity.Property(x => x.Affiliation).HasMaxLength(255);
            entity.Property(x => x.Department).HasMaxLength(255);
            entity.Property(x => x.Specialization).HasMaxLength(255);
            entity.Property(x => x.Orcid).HasMaxLength(50);
            entity.HasIndex(x => x.Orcid).IsUnique();
            entity.Property(x => x.ResearchInterests).HasColumnType("text");
            entity.Property(x => x.Biography).HasColumnType("text");
            entity.Property(x => x.AcademicRank).HasConversion<string>();
            entity.Property(x => x.Status).HasConversion<string>();
            entity.HasOne(x => x.User).WithOne(x => x.Profile).HasForeignKey<ResearchProfile>(x => x.UserId).IsRequired();
            entity.HasIndex(x => x.UserId).IsUnique();
        });

        modelBuilder.Entity<ResearchGroup>(entity =>
        {
            entity.ToTable("research_groups");
            entity.Property(x => x.Name).IsRequired().HasMaxLength(255);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Description).HasColumnType("text");
            entity.Property(x => x.ResearchTopics).HasColumnType("text");
            entity.Property(x => x.ActiveProjects).HasColumnType("text");
            entity.Property(x => x.Specialization).HasMaxLength(255);
            entity.Property(x => x.Institution).HasMaxLength(255);
        });

        modelBuilder.Entity<ResearchGroupMember>(entity =>
        {
            entity.ToTable("research_group_members");
            entity.Property(x => x.Role).HasConversion<string>();
            entity.Property(x => x.Status).HasConversion<string>();
            entity.Property(x => x.Responsibilities).HasColumnType("text");
            entity.HasOne(x => x.ResearchGroup).WithMany(x => x.Memberships).HasForeignKey(x => x.ResearchGroupId).IsRequired();
            entity.HasOne(x => x.User).WithMany(x => x.GroupMemberships).HasForeignKey(x => x.UserId).IsRequired();
            entity.HasIndex(x => new { x.ResearchGroupId, x.UserId });
        });

        modelBuilder.Entity<Author>(entity =>
        {
            entity.ToTable("authors");
            entity.Property(x => x.FullName).HasColumnName("full_name").IsRequired();
            entity.Property(x => x.Email).IsRequired();
        });

        modelBuilder.Entity<Paper>(entity =>
        {
            entity.ToTable("papers");
            entity.Property(x => x.PaperCode).HasColumnName("paper_code").IsRequired();
            entity.HasIndex(x => x.PaperCode).IsUnique();
            entity.Property(x => x.Title).IsRequired();
            entity.Property(x => x.AbstractText).HasColumnName("abstract_text").HasColumnType("text");
            entity.Property(x => x.Keywords).HasColumnType("text");
            entity.Property(x => x.ResearchField).HasColumnName("research_field");
            entity.Property(x => x.FileUrl).HasColumnName("file_url");
            entity.Property(x => x.FileType).HasColumnName("file_type");
            entity.Property(x => x.CurrentVersion).HasColumnName("current_version");
            entity.Property(x => x.SubmissionStatus).HasColumnName("submission_status").HasConversion<string>();
        });

        modelBuilder.Entity<PaperAuthor>(entity =>
        {
            entity.ToTable("paper_authors");
            entity.Property(x => x.Role).HasColumnName("role").HasConversion<string>();
            entity.HasOne(x => x.Paper).WithMany(x => x.Authors).HasForeignKey(x => x.PaperId);
            entity.HasOne(x => x.Author).WithMany(x => x.PaperAuthors).HasForeignKey(x => x.AuthorId);
        });

        modelBuilder.Entity<PaperVersion>(entity =>
        {
            entity.ToTable("paper_versions");
            entity.Property(x => x.VersionNumber).HasColumnName("version_number");
            entity.Property(x => x.FileUrl).HasColumnName("file_url").IsRequired();
            entity.Property(x => x.FileType).HasColumnName("file_type");
            entity.HasOne(x => x.Paper).WithMany(x => x.Versions).HasForeignKey(x => x.PaperId);
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

        modelBuilder.Entity<ResearchGroupMember>().Property(x => x.JoinedAt).HasConversion(dateOnlyConverter);
        modelBuilder.Entity<ResearchGroupMember>().Property(x => x.LeftAt).HasConversion(dateOnlyConverter);
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
