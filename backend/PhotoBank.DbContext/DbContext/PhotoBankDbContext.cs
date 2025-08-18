using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;
using System.Reflection;
using PhotoBank.AccessControl;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
        public DbSet<UserStorageAllow> UserStorageAllows => Set<UserStorageAllow>();
        public DbSet<UserPersonGroupAllow> UserPersonGroupAllows => Set<UserPersonGroupAllow>();
        public DbSet<UserDateRangeAllow> UserDateRangeAllows => Set<UserDateRangeAllow>();

        public PhotoBankDbContext(DbContextOptions<PhotoBankDbContext> options, ICurrentUser user) : base(options)
        {
            _user = user;
        }

        public PhotoBankDbContext(DbContextOptions<PhotoBankDbContext> options) : base(options)
        {
            _user = new DummyCurrentUser();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(System.Reflection.Assembly.Load("PhotoBank.Services"));
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>(b =>
            {
                b.Property(x => x.CanSeeNsfw).HasDefaultValue(false);
                b.HasIndex(u => u.TelegramUserId)
                    .IsUnique()
                    .HasFilter("[TelegramUserId] IS NOT NULL");
            });

            modelBuilder.Entity<Photo>()
                .HasIndex(p => p.Id)
                .HasDatabaseName("IX_Photos_NeedsMigration")
                .HasFilter("[S3Key_Preview] IS NULL OR [S3Key_Thumbnail] IS NULL")
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

            modelBuilder.Entity<PersonGroupFace>().HasIndex(q => q.FaceId);
            modelBuilder.Entity<PersonGroupFace>().HasIndex(q => q.PersonId);

            // Relationships
            modelBuilder.Entity<PersonGroupFace>()
                .HasOne(t => t.Person)
                .WithMany(t => t.PersonGroupFaces)
                .HasForeignKey(t => t.PersonId);

            modelBuilder.Entity<PersonGroupFace>()
                .HasOne(t => t.Face)
                .WithOne(t => t.PersonGroupFace);

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

            modelBuilder.Entity<Face>()
                .HasIndex(p => p.Id)
                .HasDatabaseName("IX_Faces_NeedsMigration")
                .HasFilter("[S3Key_Image] IS NULL")
                .IncludeProperties(p => new { p.S3Key_Image });

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
            modelBuilder.Entity<UserStorageAllow>().HasKey(x => new { x.UserId, x.StorageId });
            modelBuilder.Entity<UserPersonGroupAllow>().HasKey(x => new { x.UserId, x.PersonGroupId });
            modelBuilder.Entity<UserDateRangeAllow>().HasKey(x => new { x.UserId, x.FromDate, x.ToDate });
            var dConv = new ValueConverter<DateOnly, DateTime>(
                d => d.ToDateTime(TimeOnly.MinValue),
                dt => DateOnly.FromDateTime(dt));
            modelBuilder.Entity<UserDateRangeAllow>().Property(x => x.FromDate).HasConversion(dConv).HasColumnType("date");
            modelBuilder.Entity<UserDateRangeAllow>().Property(x => x.ToDate).HasConversion(dConv).HasColumnType("date");

            // --- Global filter for Photo ---
            var isAdmin = _user.IsAdmin;
            var storages = _user.AllowedStorageIds?.ToHashSet() ?? new HashSet<int>();
            var groups = _user.AllowedPersonGroupIds?.ToHashSet() ?? new HashSet<int>();
            var ranges = _user.AllowedDateRanges?.ToList() ?? new List<(DateOnly From, DateOnly To)>();
            var canSeeNsfw = _user.CanSeeNsfw;

            modelBuilder.Entity<Photo>().HasQueryFilter(p =>
                isAdmin ||
                (
                    (storages.Count > 0 && storages.Contains(p.StorageId)) &&
                    (
                        !p.Faces.Any() ||
                        p.Faces.Any(f => f.PersonId != null &&
                            f.Person.PersonGroups.Any(pg => groups.Contains(pg.Id)))
                    ) &&
                    (p.TakenDate != null && ranges.Count > 0 &&
                        ranges.Any(r => p.TakenDate.Value.Date >= r.From.ToDateTime(TimeOnly.MinValue).Date &&
                                        p.TakenDate.Value.Date <= r.To.ToDateTime(TimeOnly.MinValue).Date)) &&
                    (canSeeNsfw || !p.IsAdultContent)
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

    public class UserStorageAllow
    {
        public string UserId { get; set; } = default!;
        public int StorageId { get; set; }
    }

    public class UserPersonGroupAllow
    {
        public string UserId { get; set; } = default!;
        public int PersonGroupId { get; set; }
    }

    public class UserDateRangeAllow
    {
        public string UserId { get; set; } = default!;
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
    }
}
