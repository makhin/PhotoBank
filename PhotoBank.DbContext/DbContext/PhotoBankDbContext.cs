
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;

namespace PhotoBank.DbContext.DbContext
{
    using Microsoft.EntityFrameworkCore;
    using Models;

    public class PhotoBankDbContext : DbContext
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
        public DbSet<Enricher> Enrichers { get; set; }

        public PhotoBankDbContext(DbContextOptions<PhotoBankDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Photo>()
                .HasIndex(p => new { p.IsPrivate });

            modelBuilder.Entity<Photo>()
                .HasIndex(p => new { p.Id, p.IsPrivate });

            modelBuilder.Entity<Photo>()
                .HasIndex(p => new { p.Name, p.RelativePath, p.IsPrivate });

            modelBuilder.Entity<Photo>()
                .HasIndex(p => new { p.TakenDate, p.IsPrivate });

            modelBuilder.Entity<Photo>()
                .HasIndex(p => new { p.IsBW, p.IsPrivate });

            modelBuilder.Entity<Photo>()
                .HasIndex(p => new { p.IsAdultContent, p.IsPrivate });

            modelBuilder.Entity<Photo>()
                .HasIndex(p => new { p.IsRacyContent, p.IsPrivate });

            modelBuilder.Entity<PhotoTag>()
                .HasKey(t => new { t.PhotoId, t.TagId });

            modelBuilder.Entity<PhotoTag>()
                .HasIndex(t => new { t.PhotoId });

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

            modelBuilder.Entity<Face>()
                .HasIndex(p => p.IdentityStatus)
                .IncludeProperties(p =>p.PersonId);

            modelBuilder.Entity<Face>()
                .HasIndex(p => p.PersonId)
                .IncludeProperties(p => p.PhotoId);

            modelBuilder.Entity<Face>()
                .HasIndex(p => new { p.PhotoId, p.Id, p.PersonId });

            modelBuilder.Entity<Photo>()
                .HasIndex(p => p.StorageId)
                .IncludeProperties(p => p.RelativePath);

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
