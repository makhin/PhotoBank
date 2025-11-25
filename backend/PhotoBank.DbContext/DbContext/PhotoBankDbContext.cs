using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PhotoBank.AccessControl;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhotoBank.DbContext.DbContext
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Internal;
    using Models;

    public class PhotoBankDbContext : IdentityDbContext<ApplicationUser>, IDbContextPoolable
    {
        public static readonly ILoggerFactory PhotoBankLoggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });

        public DbSet<Storage> Storages { get; set; }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<Caption> Captions { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Face> Faces { get; set; }
        public DbSet<ObjectProperty> ObjectProperties { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<PhotoTag> PhotoTags { get; set; }
        public DbSet<PhotoCategory> PhotoCategories { get; set; }
        public DbSet<Person> Persons { get; set; }
        public DbSet<PersonGroup> PersonGroups { get; set; }
        public DbSet<File> Files { get; set; }
        public DbSet<PropertyName> PropertyNames { get; set; }
        public DbSet<Enricher> Enrichers { get; set; }

        public bool IsAdmin { get; private set; } = true;
        public HashSet<int> AllowedStorageIds { get; private set; } = new();
        public HashSet<int> AllowedPersonGroupIds { get; private set; } = new();
        public List<(DateOnly From, DateOnly To)> AllowedDateRanges { get; private set; } = new();
        public DateTime? AllowedFromDate { get; private set; }
        public DateTime? AllowedToDate { get; private set; }
        public bool CanSeeNsfw { get; private set; } = true;

        public PhotoBankDbContext(DbContextOptions<PhotoBankDbContext> options) : base(options)
        {
        }

        public void ConfigureUser(ICurrentUser user)
        {
            IsAdmin = user.IsAdmin;
            AllowedStorageIds = user.AllowedStorageIds?.ToHashSet() ?? new();
            AllowedPersonGroupIds = user.AllowedPersonGroupIds?.ToHashSet() ?? new();
            AllowedDateRanges = user.AllowedDateRanges?.ToList() ?? new();
            AllowedFromDate = AllowedDateRanges.Count > 0
                ? AllowedDateRanges.Min(r => r.From).ToDateTime(TimeOnly.MinValue)
                : (DateTime?)null;
            AllowedToDate = AllowedDateRanges.Count > 0
                ? AllowedDateRanges.Max(r => r.To).ToDateTime(TimeOnly.MaxValue)
                : (DateTime?)null;
            CanSeeNsfw = user.CanSeeNsfw;
        }

        public void ResetState()
        {
            IsAdmin = true;
            AllowedStorageIds.Clear();
            AllowedPersonGroupIds.Clear();
            AllowedDateRanges.Clear();
            AllowedFromDate = null;
            AllowedToDate = null;
            CanSeeNsfw = true;
            ChangeTracker.Clear();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Enable pgvector extension for vector operations
            modelBuilder.HasPostgresExtension("vector");

            // For in-memory database testing, ignore the Vector property since it's PostgreSQL-specific
            // The in-memory provider doesn't support Pgvector types
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                modelBuilder.Entity<Face>()
                    .Ignore(f => f.Embedding);
            }

            modelBuilder.UseIdentityAlwaysColumns();

            modelBuilder.Entity<ApplicationUser>(b =>
            {
                b.HasIndex(u => u.TelegramUserId)
                    .IsUnique()
                    .HasFilter("\"TelegramUserId\" IS NOT NULL");
            });

            modelBuilder.Entity<Photo>()
                .HasIndex(p => p.Id)
                .HasDatabaseName("IX_Photos_NeedsMigration_Preview")
                .HasFilter("\"S3Key_Preview\" IS NULL")
                .IncludeProperties(p => new { p.S3Key_Preview, p.S3Key_Thumbnail });

            modelBuilder.Entity<Photo>()
                .HasIndex(p => p.Id)
                .HasDatabaseName("IX_Photos_NeedsMigration_Thumbnail")
                .HasFilter("\"S3Key_Thumbnail\" IS NULL")
                .IncludeProperties(p => new { p.S3Key_Preview, p.S3Key_Thumbnail });

            modelBuilder.Entity<Photo>()
                .HasIndex(p => new { p.Name, p.RelativePath });

            modelBuilder.Entity<Photo>()
                .HasIndex(p => new { p.TakenDate });

            modelBuilder.Entity<Photo>()
                .HasIndex(p => new { p.IsBW });

            modelBuilder.Entity<Photo>()
                .HasIndex(p => new { p.IsAdultContent });

            modelBuilder.Entity<Photo>()
                .HasIndex(p => new { p.IsRacyContent });

            modelBuilder.Entity<Photo>()
                .HasIndex(p => new { p.StorageId, p.TakenDate });

            modelBuilder.Entity<Photo>().Property(p => p.TakenMonth)
                .HasComputedColumnSql(@"(EXTRACT(MONTH FROM (""TakenDate"" AT TIME ZONE 'UTC')))::int", stored: true);
            modelBuilder.Entity<Photo>().Property(p => p.TakenDay)
                .HasComputedColumnSql(@"(EXTRACT(DAY FROM (""TakenDate"" AT TIME ZONE 'UTC')))::int", stored: true);

            modelBuilder.Entity<PhotoTag>()
                .HasKey(t => new { t.PhotoId, t.TagId });

            modelBuilder.Entity<PhotoTag>()
                .HasIndex(t => new { t.PhotoId });

            modelBuilder.Entity<PhotoTag>()
                .HasIndex(x => new { x.TagId, x.PhotoId });

            modelBuilder.Entity<PhotoTag>()
                .HasOne(pt => pt.Photo)
                .WithMany(p => p.PhotoTags)
                .HasForeignKey(pt => pt.PhotoId);

            modelBuilder.Entity<PhotoTag>()
                .HasOne(pt => pt.Tag)
                .WithMany(t => t.PhotoTags)
                .HasForeignKey(pt => pt.TagId);

            modelBuilder.Entity<PhotoCategory>()
                .HasKey(t => new { t.PhotoId, t.CategoryId });

            modelBuilder.Entity<PhotoCategory>()
                .HasOne(pt => pt.Photo)
                .WithMany(p => p.PhotoCategories)
                .HasForeignKey(pt => pt.PhotoId);

            modelBuilder.Entity<PhotoCategory>()
                .HasOne(pt => pt.Category)
                .WithMany(t => t.PhotoCategories)
                .HasForeignKey(pt => pt.CategoryId);

            modelBuilder.Entity<File>(e =>
            {
                e.HasQueryFilter(f => !f.IsDeleted);
                e.HasIndex(p => p.Name);
                e.HasIndex(p => new { p.Name, p.PhotoId }).IsUnique();
            });

            modelBuilder.Entity<Face>()
                .HasIndex(p => p.IdentityStatus)
                .IncludeProperties(p => p.PersonId);

            modelBuilder.Entity<Face>()
                .HasIndex(p => p.PersonId)
                .IncludeProperties(p => p.PhotoId);

            modelBuilder.Entity<Face>()
                .HasIndex(x => new { x.PersonId, x.PhotoId });

            modelBuilder.Entity<Face>()
                .HasIndex(p => new { p.PhotoId, p.Id, p.PersonId });

            modelBuilder.Entity<Face>()
                .HasIndex(p => new { p.Provider, p.ExternalId });

            modelBuilder.Entity<Face>()
                .HasIndex(p => p.ExternalGuid);

            modelBuilder.Entity<Photo>()
                .HasIndex(p => p.StorageId)
                .IncludeProperties(p => p.RelativePath);

            modelBuilder.Entity<Tag>(e =>
            {
                e.HasIndex(x => x.Name)
                .HasDatabaseName("IX_Tag_Name");
            });

            modelBuilder.Entity<Enricher>()
                .HasIndex(u => u.Name)
                .IsUnique();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLoggerFactory(PhotoBankLoggerFactory);
        }

    }

}
