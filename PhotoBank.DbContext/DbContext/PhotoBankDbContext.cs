﻿
using Microsoft.Extensions.Logging;

namespace PhotoBank.DbContext.DbContext
{
    using Microsoft.EntityFrameworkCore;
    using PhotoBank.DbContext.Models;

    public class PhotoBankDbContext : Microsoft.EntityFrameworkCore.DbContext
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
        public DbSet<File> Files { get; set; }
        public DbSet<PropertyName> PropertyNames { get; set; }
        public DbSet<FaceToFace> FaceToFaces { get; set; }

        public PhotoBankDbContext(DbContextOptions<PhotoBankDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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

            modelBuilder.Entity<PhotoTag>()
                .HasKey(t => new { t.PhotoId, t.TagId });

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

            modelBuilder.Entity<File>()
                .HasIndex(p => new { p.Name });

            modelBuilder.Entity<FaceToFace>().HasNoKey();

            modelBuilder.Entity<FaceToFace>()
                .HasIndex(p => new { p.Face1Id });
            modelBuilder.Entity<FaceToFace>()
                .HasIndex(p => new { p.Face2Id });
            modelBuilder.Entity<FaceToFace>()
                .HasIndex(p => new { p.Distance }).IncludeProperties("Face1Id", "Face2Id");

            modelBuilder.Entity<Face>()
                .HasIndex(p => p.IdentityStatus)
                .IncludeProperties(p =>p.PersonId);

            modelBuilder.Entity<Face>()
                .HasIndex(p => p.PersonId)
                .IncludeProperties(p => p.PhotoId);

            modelBuilder.Entity<Photo>()
                .HasIndex(p => p.StorageId)
                .IncludeProperties(p => p.RelativePath);

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLoggerFactory(PhotoBankLoggerFactory);
        }
    }
}
