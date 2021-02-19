﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;
using PhotoBank.DbContext.DbContext;

namespace PhotoBank.DbContext.Migrations
{
    [DbContext(typeof(PhotoBankDbContext))]
    partial class PhotoBankDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "5.0.3")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("PersonPersonGroup", b =>
                {
                    b.Property<int>("PersonGroupsId")
                        .HasColumnType("int");

                    b.Property<int>("PersonsId")
                        .HasColumnType("int");

                    b.HasKey("PersonGroupsId", "PersonsId");

                    b.HasIndex("PersonsId");

                    b.ToTable("PersonPersonGroup");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.Caption", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<double>("Confidence")
                        .HasColumnType("float");

                    b.Property<int?>("PhotoId")
                        .HasColumnType("int");

                    b.Property<string>("Text")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("PhotoId");

                    b.ToTable("Captions");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.Category", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Categories");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.Face", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<double?>("Age")
                        .HasColumnType("float");

                    b.Property<string>("FaceAttributes")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool?>("Gender")
                        .HasColumnType("bit");

                    b.Property<double>("IdentifiedWithConfidence")
                        .HasColumnType("float");

                    b.Property<int>("IdentityStatus")
                        .HasColumnType("int");

                    b.Property<byte[]>("Image")
                        .HasColumnType("varbinary(max)");

                    b.Property<int?>("PersonId")
                        .HasColumnType("int");

                    b.Property<int>("PhotoId")
                        .HasColumnType("int");

                    b.Property<Geometry>("Rectangle")
                        .HasColumnType("geometry");

                    b.Property<double?>("Smile")
                        .HasColumnType("float");

                    b.HasKey("Id");

                    b.HasIndex("IdentityStatus")
                        .HasAnnotation("SqlServer:Include", new[] { "PersonId" });

                    b.HasIndex("PersonId")
                        .HasAnnotation("SqlServer:Include", new[] { "PhotoId" });

                    b.HasIndex("PhotoId");

                    b.ToTable("Faces");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.FaceToFace", b =>
                {
                    b.Property<double>("Distance")
                        .HasColumnType("float");

                    b.Property<int>("Face1Id")
                        .HasColumnType("int");

                    b.Property<int>("Face2Id")
                        .HasColumnType("int");

                    b.HasIndex("Distance")
                        .HasAnnotation("SqlServer:Include", new[] { "Face1Id", "Face2Id" });

                    b.HasIndex("Face1Id");

                    b.HasIndex("Face2Id");

                    b.ToTable("FaceToFaces");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.File", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<int?>("PhotoId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Name");

                    b.HasIndex("PhotoId");

                    b.ToTable("Files");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.ObjectProperty", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<double>("Confidence")
                        .HasColumnType("float");

                    b.Property<int?>("PhotoId")
                        .HasColumnType("int");

                    b.Property<int?>("PropertyNameId")
                        .HasColumnType("int");

                    b.Property<Geometry>("Rectangle")
                        .HasColumnType("geometry");

                    b.HasKey("Id");

                    b.HasIndex("PhotoId");

                    b.HasIndex("PropertyNameId");

                    b.ToTable("ObjectProperties");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.Person", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime?>("DateOfBirth")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("ExternalGuid")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Persons");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.PersonGroup", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("PersonGroup");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.PersonGroupFace", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<Guid>("ExternalGuid")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("FaceId")
                        .HasColumnType("int");

                    b.Property<int>("PersonId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("FaceId")
                        .IsUnique();

                    b.HasIndex("PersonId");

                    b.ToTable("PersonGroupFace");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.Photo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("AccentColor")
                        .HasMaxLength(6)
                        .HasColumnType("nvarchar(6)");

                    b.Property<double>("AdultScore")
                        .HasColumnType("float");

                    b.Property<string>("DominantColorBackground")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("DominantColorForeground")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("DominantColors")
                        .HasMaxLength(150)
                        .HasColumnType("nvarchar(150)");

                    b.Property<int>("FaceIdentifyStatus")
                        .HasColumnType("int");

                    b.Property<int?>("Height")
                        .HasColumnType("int");

                    b.Property<bool>("IsAdultContent")
                        .HasColumnType("bit");

                    b.Property<bool>("IsBW")
                        .HasColumnType("bit");

                    b.Property<bool>("IsRacyContent")
                        .HasColumnType("bit");

                    b.Property<Point>("Location")
                        .HasColumnType("geometry");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<int?>("Orientation")
                        .HasColumnType("int");

                    b.Property<byte[]>("PreviewImage")
                        .HasColumnType("varbinary(max)");

                    b.Property<double>("RacyScore")
                        .HasColumnType("float");

                    b.Property<string>("RelativePath")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<double>("Scale")
                        .HasColumnType("float");

                    b.Property<int>("StorageId")
                        .HasColumnType("int");

                    b.Property<DateTime?>("TakenDate")
                        .HasColumnType("datetime2");

                    b.Property<byte[]>("Thumbnail")
                        .HasColumnType("varbinary(max)");

                    b.Property<int?>("Width")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("IsAdultContent");

                    b.HasIndex("IsBW");

                    b.HasIndex("IsRacyContent");

                    b.HasIndex("StorageId")
                        .HasAnnotation("SqlServer:Include", new[] { "RelativePath" });

                    b.HasIndex("TakenDate");

                    b.HasIndex("Name", "RelativePath");

                    b.ToTable("Photos");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.PhotoCategory", b =>
                {
                    b.Property<int>("PhotoId")
                        .HasColumnType("int");

                    b.Property<int>("CategoryId")
                        .HasColumnType("int");

                    b.Property<double>("Score")
                        .HasColumnType("float");

                    b.HasKey("PhotoId", "CategoryId");

                    b.HasIndex("CategoryId");

                    b.ToTable("PhotoCategories");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.PhotoTag", b =>
                {
                    b.Property<int>("PhotoId")
                        .HasColumnType("int");

                    b.Property<int>("TagId")
                        .HasColumnType("int");

                    b.Property<double>("Confidence")
                        .HasColumnType("float");

                    b.HasKey("PhotoId", "TagId");

                    b.HasIndex("TagId");

                    b.ToTable("PhotoTags");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.PropertyName", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.HasKey("Id");

                    b.ToTable("PropertyNames");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.Storage", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Folder")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Storages");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.Tag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Hint")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("PersonPersonGroup", b =>
                {
                    b.HasOne("PhotoBank.DbContext.Models.PersonGroup", null)
                        .WithMany()
                        .HasForeignKey("PersonGroupsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PhotoBank.DbContext.Models.Person", null)
                        .WithMany()
                        .HasForeignKey("PersonsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.Caption", b =>
                {
                    b.HasOne("PhotoBank.DbContext.Models.Photo", null)
                        .WithMany("Captions")
                        .HasForeignKey("PhotoId");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.Face", b =>
                {
                    b.HasOne("PhotoBank.DbContext.Models.Person", "Person")
                        .WithMany("Faces")
                        .HasForeignKey("PersonId");

                    b.HasOne("PhotoBank.DbContext.Models.Photo", "Photo")
                        .WithMany("Faces")
                        .HasForeignKey("PhotoId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Person");

                    b.Navigation("Photo");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.File", b =>
                {
                    b.HasOne("PhotoBank.DbContext.Models.Photo", "Photo")
                        .WithMany("Files")
                        .HasForeignKey("PhotoId");

                    b.Navigation("Photo");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.ObjectProperty", b =>
                {
                    b.HasOne("PhotoBank.DbContext.Models.Photo", null)
                        .WithMany("ObjectProperties")
                        .HasForeignKey("PhotoId");

                    b.HasOne("PhotoBank.DbContext.Models.PropertyName", "PropertyName")
                        .WithMany()
                        .HasForeignKey("PropertyNameId");

                    b.Navigation("PropertyName");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.PersonGroupFace", b =>
                {
                    b.HasOne("PhotoBank.DbContext.Models.Face", "Face")
                        .WithOne("PersonGroupFace")
                        .HasForeignKey("PhotoBank.DbContext.Models.PersonGroupFace", "FaceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PhotoBank.DbContext.Models.Person", "Person")
                        .WithMany("PersonGroupFaces")
                        .HasForeignKey("PersonId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Face");

                    b.Navigation("Person");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.Photo", b =>
                {
                    b.HasOne("PhotoBank.DbContext.Models.Storage", "Storage")
                        .WithMany("Photos")
                        .HasForeignKey("StorageId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Storage");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.PhotoCategory", b =>
                {
                    b.HasOne("PhotoBank.DbContext.Models.Category", "Category")
                        .WithMany("PhotoCategories")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PhotoBank.DbContext.Models.Photo", "Photo")
                        .WithMany("PhotoCategories")
                        .HasForeignKey("PhotoId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Category");

                    b.Navigation("Photo");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.PhotoTag", b =>
                {
                    b.HasOne("PhotoBank.DbContext.Models.Photo", "Photo")
                        .WithMany("PhotoTags")
                        .HasForeignKey("PhotoId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PhotoBank.DbContext.Models.Tag", "Tag")
                        .WithMany("PhotoTags")
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Photo");

                    b.Navigation("Tag");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.Category", b =>
                {
                    b.Navigation("PhotoCategories");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.Face", b =>
                {
                    b.Navigation("PersonGroupFace");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.Person", b =>
                {
                    b.Navigation("Faces");

                    b.Navigation("PersonGroupFaces");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.Photo", b =>
                {
                    b.Navigation("Captions");

                    b.Navigation("Faces");

                    b.Navigation("Files");

                    b.Navigation("ObjectProperties");

                    b.Navigation("PhotoCategories");

                    b.Navigation("PhotoTags");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.Storage", b =>
                {
                    b.Navigation("Photos");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.Tag", b =>
                {
                    b.Navigation("PhotoTags");
                });
#pragma warning restore 612, 618
        }
    }
}
