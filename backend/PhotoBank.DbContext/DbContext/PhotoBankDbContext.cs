using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System;

namespace PhotoBank.DbContext.DbContext
{
    using Microsoft.EntityFrameworkCore;
    using Models;
    
    public class PhotoBankDbContext : IdentityDbContext<ApplicationUser>
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
        public DbSet<UserStorageAllow> UserStorageAllows => Set<UserStorageAllow>();
        public DbSet<UserPersonGroupAllow> UserPersonGroupAllows => Set<UserPersonGroupAllow>();
        public DbSet<UserDateRangeAllow> UserDateRangeAllows => Set<UserDateRangeAllow>();

        public PhotoBankDbContext(DbContextOptions<PhotoBankDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(System.Reflection.Assembly.Load("PhotoBank.Services"));
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>(b =>
            {
                b.HasIndex(u => u.TelegramUserId)
                    .IsUnique()
                    .HasFilter("[TelegramUserId] IS NOT NULL");
            });
            modelBuilder.Entity<ApplicationUser>().Property(x => x.CanSeeNsfw).HasDefaultValue(false);

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
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLoggerFactory(PhotoBankLoggerFactory);
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
