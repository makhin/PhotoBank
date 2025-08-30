using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PhotoBank.AccessControl;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhotoBank.DbContext.DbContext
{
    using Microsoft.EntityFrameworkCore;
    using Models;
    
    public class PhotoBankDbContext : IdentityDbContext<ApplicationUser>
    {
        public static readonly ILoggerFactory PhotoBankLoggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });

        private readonly ICurrentUser _user;

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

        private PhotoBankDbContext(DbContextOptions<PhotoBankDbContext> options, ICurrentUser user) : base(options) //TODO: user
        {
            _user = user;
        }

        public PhotoBankDbContext(DbContextOptions<PhotoBankDbContext> options) : base(options)
        {
            _user = new DummyCurrentUser();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>(b =>
            {
                b.HasIndex(u => u.TelegramUserId)
                    .IsUnique()
                    .HasFilter("[TelegramUserId] IS NOT NULL");
            });

            modelBuilder.Entity<Photo>()
                .HasIndex(p => p.Id)
                .HasDatabaseName("IX_Photos_NeedsMigration_Preview")
                .HasFilter("[S3Key_Preview] IS NULL")
                .IncludeProperties(p => new { p.S3Key_Preview, p.S3Key_Thumbnail });

            modelBuilder.Entity<Photo>()
                .HasIndex(p => p.Id)
                .HasDatabaseName("IX_Photos_NeedsMigration_Thumbnail")
                .HasFilter("[S3Key_Thumbnail] IS NULL")
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

            modelBuilder.Entity<PersonFace>().HasIndex(q => q.FaceId);
            modelBuilder.Entity<PersonFace>().HasIndex(q => q.PersonId);

            // Relationships
            modelBuilder.Entity<PersonFace>()
                .HasOne(t => t.Person)
                .WithMany(t => t.PersonFaces)
                .HasForeignKey(t => t.PersonId);

            modelBuilder.Entity<PersonFace>()
                .HasOne(t => t.Face)
                .WithOne(t => t.PersonFace);

            modelBuilder.Entity<File>(e =>
            {
                e.HasQueryFilter(f => !f.IsDeleted);
                e.HasIndex(p => p.Name);
                e.HasIndex(p => new { p.Name, p.PhotoId }).IsUnique();
            });

            modelBuilder.Entity<Face>()
                .HasIndex(p => p.IdentityStatus)
                .IncludeProperties(p =>p.PersonId);

            modelBuilder.Entity<Face>()
                .HasIndex(p => p.PersonId)
                .IncludeProperties(p => p.PhotoId);

            modelBuilder.Entity<Face>()
                .HasIndex(x => new { x.PersonId, x.PhotoId });

            modelBuilder.Entity<Face>()
                .HasIndex(p => new { p.PhotoId, p.Id, p.PersonId });

            modelBuilder.Entity<Photo>()
                .HasIndex(p => p.StorageId)
                .IncludeProperties(p => p.RelativePath);

            modelBuilder.Entity<Tag>(e =>
            {
                e.HasIndex(x => x.Name)
                    .HasDatabaseName("IX_Tag_Name")
                    .IsClustered(false);
            });

            modelBuilder.Entity<Enricher>()
                .HasIndex(u => u.Name)
                .IsUnique();

            // --- Global filter for Photo ---
            var isAdmin = _user.IsAdmin;
            var storages = _user.AllowedStorageIds?.ToHashSet() ?? new HashSet<int>();
            var groups = _user.AllowedPersonGroupIds?.ToHashSet() ?? new HashSet<int>();
            var ranges = _user.AllowedDateRanges?.ToList() ?? new List<(DateOnly From, DateOnly To)>();
            var canSeeNsfw = _user.CanSeeNsfw;

            // Storage — only allowed, admin sees all
            modelBuilder.Entity<Storage>().HasQueryFilter(s =>
                isAdmin || storages.Contains(s.Id));

            // Person — only from allowed groups, admin sees all
            modelBuilder.Entity<Person>().HasQueryFilter(p =>
                isAdmin || p.PersonGroups.Any(pg => groups.Contains(pg.Id)));

            modelBuilder.Entity<Photo>().HasQueryFilter(p =>
                isAdmin ||
                (
                    (storages.Count > 0 && storages.Contains(p.StorageId)) &&
                    (
                        groups.Count == 0
                        || !p.Faces.Any()
                        || p.Faces.Any(f => f.PersonId != null &&
                            f.Person.PersonGroups.Any(pg => groups.Contains(pg.Id)))
                    ) &&
                    (
                        ranges.Count == 0
                        || (p.TakenDate != null && ranges.Any(r =>
                            p.TakenDate.Value.Date >= r.From.ToDateTime(TimeOnly.MinValue).Date &&
                            p.TakenDate.Value.Date <= r.To.ToDateTime(TimeOnly.MinValue).Date))
                    ) &&
                    (canSeeNsfw || (!p.IsAdultContent && !p.IsRacyContent))
                ));
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLoggerFactory(PhotoBankLoggerFactory);
        }

        private sealed class DummyCurrentUser : ICurrentUser
        {
            public string UserId => string.Empty;
            public bool IsAdmin => true;
            public IReadOnlySet<int> AllowedStorageIds => new HashSet<int>();
            public IReadOnlySet<int> AllowedPersonGroupIds => new HashSet<int>();
            public IReadOnlyList<(DateOnly From, DateOnly To)> AllowedDateRanges => new List<(DateOnly From, DateOnly To)>();
            public bool CanSeeNsfw => true;
        }

    }

}
