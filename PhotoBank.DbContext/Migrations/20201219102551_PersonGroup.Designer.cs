﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;
using PhotoBank.DbContext.DbContext;

namespace PhotoBank.DbContext.Migrations
{
    [DbContext(typeof(PhotoBankDbContext))]
    [Migration("20201219102551_PersonGroup")]
    partial class PersonGroup
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .UseIdentityColumns()
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "5.0.0");

            modelBuilder.Entity("PhotoBank.DbContext.Models.Caption", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

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
                        .UseIdentityColumn();

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
                        .UseIdentityColumn();

                    b.Property<int>("Age")
                        .HasColumnType("int");

                    b.Property<int?>("Gender")
                        .HasColumnType("int");

                    b.Property<byte[]>("Image")
                        .HasColumnType("varbinary(max)");

                    b.Property<int?>("PersonId")
                        .HasColumnType("int");

                    b.Property<int?>("PhotoId")
                        .HasColumnType("int");

                    b.Property<Geometry>("Rectangle")
                        .HasColumnType("geometry");

                    b.HasKey("Id");

                    b.HasIndex("PersonId");

                    b.HasIndex("PhotoId");

                    b.ToTable("Faces");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.FaceList", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<Guid>("ExternalGuid")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.ToTable("FaceLists");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.FaceListFace", b =>
                {
                    b.Property<int>("FaceListId")
                        .HasColumnType("int");

                    b.Property<int>("FaceId")
                        .HasColumnType("int");

                    b.Property<Guid>("ExternalGuid")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("FaceListId", "FaceId");

                    b.HasIndex("FaceId");

                    b.ToTable("FaceListFaces");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.File", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<int?>("PhotoId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("PhotoId");

                    b.ToTable("Files");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.ObjectProperty", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<double>("Confidence")
                        .HasColumnType("float");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("PhotoId")
                        .HasColumnType("int");

                    b.Property<Geometry>("Rectangle")
                        .HasColumnType("geometry");

                    b.HasKey("Id");

                    b.HasIndex("PhotoId");

                    b.ToTable("ObjectProperties");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.Person", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

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
                        .UseIdentityColumn();

                    b.Property<Guid>("ExternalGuid")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("PersonGroups");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.PersonGroupPerson", b =>
                {
                    b.Property<int>("PersonGroupId")
                        .HasColumnType("int");

                    b.Property<int>("PersonId")
                        .HasColumnType("int");

                    b.Property<Guid>("ExternalGuid")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("PersonGroupId", "PersonId");

                    b.HasIndex("PersonId");

                    b.ToTable("PersonGroupPersons");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.Photo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<string>("AccentColor")
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("AdultScore")
                        .HasColumnType("float");

                    b.Property<string>("DominantColorBackground")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DominantColorForeground")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DominantColors")
                        .HasColumnType("nvarchar(max)");

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
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("Scale")
                        .HasColumnType("float");

                    b.Property<int?>("StorageId")
                        .HasColumnType("int");

                    b.Property<DateTime?>("TakenDate")
                        .HasColumnType("datetime2");

                    b.Property<byte[]>("Thumbnail")
                        .HasColumnType("varbinary(max)");

                    b.Property<int?>("Width")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("StorageId");

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

            modelBuilder.Entity("PhotoBank.DbContext.Models.Storage", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

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
                        .UseIdentityColumn();

                    b.Property<string>("Hint")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Tags");
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

                    b.HasOne("PhotoBank.DbContext.Models.Photo", null)
                        .WithMany("Faces")
                        .HasForeignKey("PhotoId");

                    b.Navigation("Person");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.FaceListFace", b =>
                {
                    b.HasOne("PhotoBank.DbContext.Models.Face", "Face")
                        .WithMany("FaceListFaces")
                        .HasForeignKey("FaceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PhotoBank.DbContext.Models.FaceList", "FaceList")
                        .WithMany("FaceListFaces")
                        .HasForeignKey("FaceListId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Face");

                    b.Navigation("FaceList");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.File", b =>
                {
                    b.HasOne("PhotoBank.DbContext.Models.Photo", null)
                        .WithMany("Files")
                        .HasForeignKey("PhotoId");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.ObjectProperty", b =>
                {
                    b.HasOne("PhotoBank.DbContext.Models.Photo", null)
                        .WithMany("ObjectProperties")
                        .HasForeignKey("PhotoId");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.PersonGroupPerson", b =>
                {
                    b.HasOne("PhotoBank.DbContext.Models.PersonGroup", "PersonGroup")
                        .WithMany("PersonGroupPersons")
                        .HasForeignKey("PersonGroupId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PhotoBank.DbContext.Models.Person", "Person")
                        .WithMany("PersonGroupPersons")
                        .HasForeignKey("PersonId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Person");

                    b.Navigation("PersonGroup");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.Photo", b =>
                {
                    b.HasOne("PhotoBank.DbContext.Models.Storage", "Storage")
                        .WithMany("Photos")
                        .HasForeignKey("StorageId");

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
                    b.Navigation("FaceListFaces");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.FaceList", b =>
                {
                    b.Navigation("FaceListFaces");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.Person", b =>
                {
                    b.Navigation("Faces");

                    b.Navigation("PersonGroupPersons");
                });

            modelBuilder.Entity("PhotoBank.DbContext.Models.PersonGroup", b =>
                {
                    b.Navigation("PersonGroupPersons");
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