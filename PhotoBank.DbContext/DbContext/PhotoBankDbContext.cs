
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
        public DbSet<FaceList> FaceLists { get; set; }
        public DbSet<FaceListFace> FaceListFaces { get; set; }
        public DbSet<PersonGroup> PersonGroups { get; set; }
        public DbSet<PersonGroupPerson> PersonGroupPersons { get; set; }

        public PhotoBankDbContext(DbContextOptions<PhotoBankDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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


            modelBuilder.Entity<FaceListFace>().HasKey(q =>
                new {
                    q.FaceListId,
                    q.FaceId
                });

            // Relationships
            modelBuilder.Entity<FaceListFace>()
                .HasOne(t => t.Face)
                .WithMany(t => t.FaceListFaces)
                .HasForeignKey(t => t.FaceId);

            modelBuilder.Entity<FaceListFace>()
                .HasOne(t => t.FaceList)
                .WithMany(t => t.FaceListFaces)
                .HasForeignKey(t => t.FaceListId);


            modelBuilder.Entity<PersonGroupPerson>().HasKey(q =>
                new {
                    q.PersonGroupId,
                    q.PersonId
                });

            // Relationships
            modelBuilder.Entity<PersonGroupPerson>()
                .HasOne(t => t.Person)
                .WithMany(t => t.PersonGroupPersons)
                .HasForeignKey(t => t.PersonId);

            modelBuilder.Entity<PersonGroupPerson>()
                .HasOne(t => t.PersonGroup)
                .WithMany(t => t.PersonGroupPersons)
                .HasForeignKey(t => t.PersonGroupId);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLoggerFactory(PhotoBankLoggerFactory);
        }
    }
}
