using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

namespace PhotoBank.DbContext.Migrations
{
    public partial class InitialPG : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Enrichers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrichers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersonGroup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Persons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ExternalGuid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PropertyNames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyNames", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Storages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Folder = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Storages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Hint = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersonPersonGroup",
                columns: table => new
                {
                    PersonGroupsId = table.Column<int>(type: "integer", nullable: false),
                    PersonsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonPersonGroup", x => new { x.PersonGroupsId, x.PersonsId });
                    table.ForeignKey(
                        name: "FK_PersonPersonGroup_PersonGroup_PersonGroupsId",
                        column: x => x.PersonGroupsId,
                        principalTable: "PersonGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonPersonGroup_Persons_PersonsId",
                        column: x => x.PersonsId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Photos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StorageId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TakenDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsBW = table.Column<bool>(type: "boolean", nullable: false),
                    AccentColor = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    DominantColorBackground = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DominantColorForeground = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DominantColors = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Location = table.Column<Point>(type: "geometry", nullable: true),
                    PreviewImage = table.Column<byte[]>(type: "bytea", nullable: true),
                    Thumbnail = table.Column<byte[]>(type: "bytea", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Orientation = table.Column<int>(type: "integer", nullable: true),
                    IsAdultContent = table.Column<bool>(type: "boolean", nullable: false),
                    AdultScore = table.Column<double>(type: "double precision", nullable: false),
                    IsRacyContent = table.Column<bool>(type: "boolean", nullable: false),
                    RacyScore = table.Column<double>(type: "double precision", nullable: false),
                    RelativePath = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Scale = table.Column<double>(type: "double precision", nullable: false),
                    FaceIdentifyStatus = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Photos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Photos_Storages_StorageId",
                        column: x => x.StorageId,
                        principalTable: "Storages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Captions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Text = table.Column<string>(type: "text", nullable: true),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    PhotoId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Captions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Captions_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Faces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Rectangle = table.Column<Geometry>(type: "geometry", nullable: true),
                    Age = table.Column<double>(type: "double precision", nullable: true),
                    Gender = table.Column<bool>(type: "boolean", nullable: true),
                    Smile = table.Column<double>(type: "double precision", nullable: true),
                    Image = table.Column<byte[]>(type: "bytea", nullable: true),
                    PersonId = table.Column<int>(type: "integer", nullable: true),
                    PhotoId = table.Column<int>(type: "integer", nullable: false),
                    IdentityStatus = table.Column<int>(type: "integer", nullable: false),
                    IdentifiedWithConfidence = table.Column<double>(type: "double precision", nullable: false),
                    FaceAttributes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Faces_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Faces_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PhotoId = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Files_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ObjectProperties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Rectangle = table.Column<Geometry>(type: "geometry", nullable: true),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    PropertyNameId = table.Column<int>(type: "integer", nullable: true),
                    PhotoId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObjectProperties_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ObjectProperties_PropertyNames_PropertyNameId",
                        column: x => x.PropertyNameId,
                        principalTable: "PropertyNames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhotoCategories",
                columns: table => new
                {
                    PhotoId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoCategories", x => new { x.PhotoId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_PhotoCategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhotoCategories_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhotoTags",
                columns: table => new
                {
                    PhotoId = table.Column<int>(type: "integer", nullable: false),
                    TagId = table.Column<int>(type: "integer", nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoTags", x => new { x.PhotoId, x.TagId });
                    table.ForeignKey(
                        name: "FK_PhotoTags_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhotoTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonGroupFace",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonId = table.Column<int>(type: "integer", nullable: false),
                    FaceId = table.Column<int>(type: "integer", nullable: false),
                    ExternalGuid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonGroupFace", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonGroupFace_Faces_FaceId",
                        column: x => x.FaceId,
                        principalTable: "Faces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonGroupFace_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Captions_PhotoId",
                table: "Captions",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrichers_Name",
                table: "Enrichers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Faces_IdentityStatus",
                table: "Faces",
                column: "IdentityStatus")
                .Annotation("Npgsql:IndexInclude", new[] { "PersonId" });

            migrationBuilder.CreateIndex(
                name: "IX_Faces_PersonId",
                table: "Faces",
                column: "PersonId")
                .Annotation("Npgsql:IndexInclude", new[] { "PhotoId" });

            migrationBuilder.CreateIndex(
                name: "IX_Faces_PhotoId",
                table: "Faces",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_Files_Name",
                table: "Files",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Files_PhotoId",
                table: "Files",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectProperties_PhotoId",
                table: "ObjectProperties",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectProperties_PropertyNameId",
                table: "ObjectProperties",
                column: "PropertyNameId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonGroupFace_FaceId",
                table: "PersonGroupFace",
                column: "FaceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonGroupFace_PersonId",
                table: "PersonGroupFace",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonPersonGroup_PersonsId",
                table: "PersonPersonGroup",
                column: "PersonsId");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoCategories_CategoryId",
                table: "PhotoCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_IsAdultContent",
                table: "Photos",
                column: "IsAdultContent");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_IsBW",
                table: "Photos",
                column: "IsBW");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_IsRacyContent",
                table: "Photos",
                column: "IsRacyContent");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_Name_RelativePath",
                table: "Photos",
                columns: new[] { "Name", "RelativePath" });

            migrationBuilder.CreateIndex(
                name: "IX_Photos_StorageId",
                table: "Photos",
                column: "StorageId")
                .Annotation("Npgsql:IndexInclude", new[] { "RelativePath" });

            migrationBuilder.CreateIndex(
                name: "IX_Photos_TakenDate",
                table: "Photos",
                column: "TakenDate");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoTags_TagId",
                table: "PhotoTags",
                column: "TagId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Captions");

            migrationBuilder.DropTable(
                name: "Enrichers");

            migrationBuilder.DropTable(
                name: "Files");

            migrationBuilder.DropTable(
                name: "ObjectProperties");

            migrationBuilder.DropTable(
                name: "PersonGroupFace");

            migrationBuilder.DropTable(
                name: "PersonPersonGroup");

            migrationBuilder.DropTable(
                name: "PhotoCategories");

            migrationBuilder.DropTable(
                name: "PhotoTags");

            migrationBuilder.DropTable(
                name: "PropertyNames");

            migrationBuilder.DropTable(
                name: "Faces");

            migrationBuilder.DropTable(
                name: "PersonGroup");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Persons");

            migrationBuilder.DropTable(
                name: "Photos");

            migrationBuilder.DropTable(
                name: "Storages");
        }
    }
}
