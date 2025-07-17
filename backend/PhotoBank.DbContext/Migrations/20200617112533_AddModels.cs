using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

namespace PhotoBank.DbContext.Migrations
{
    public partial class AddModels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Photos");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Photos",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "AccentColor",
                table: "Photos",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AdultScore",
                table: "Photos",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "DominantColorBackground",
                table: "Photos",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DominantColorForeground",
                table: "Photos",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DominantColors",
                table: "Photos",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "Photos",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdultContent",
                table: "Photos",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsBW",
                table: "Photos",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRacyContent",
                table: "Photos",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "Photos",
                type: "geometry",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Orientation",
                table: "Photos",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PreviewImage",
                table: "Photos",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RacyScore",
                table: "Photos",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "Photos",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Caption",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Text = table.Column<string>(nullable: true),
                    Confidence = table.Column<double>(nullable: false),
                    PhotoId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Caption", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Caption_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Category",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Face",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(nullable: true),
                    Rectangle = table.Column<Geometry>(type: "geometry", nullable: true),
                    Age = table.Column<int>(nullable: false),
                    Gender = table.Column<int>(nullable: true),
                    PhotoId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Face", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Face_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ObjectProperty",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Rectangle = table.Column<Geometry>(type: "geometry", nullable: true),
                    Confidence = table.Column<double>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    PhotoId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectProperty", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObjectProperty_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tag",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(nullable: true),
                    Hint = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tag", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhotoCategory",
                columns: table => new
                {
                    PhotoId = table.Column<int>(nullable: false),
                    CategoryId = table.Column<int>(nullable: false),
                    Id = table.Column<int>(nullable: false),
                    Score = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoCategory", x => new { x.PhotoId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_PhotoCategory_Category_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Category",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhotoCategory_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhotoTag",
                columns: table => new
                {
                    PhotoId = table.Column<int>(nullable: false),
                    TagId = table.Column<int>(nullable: false),
                    Id = table.Column<int>(nullable: false),
                    Confidence = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoTag", x => new { x.PhotoId, x.TagId });
                    table.ForeignKey(
                        name: "FK_PhotoTag_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhotoTag_Tag_TagId",
                        column: x => x.TagId,
                        principalTable: "Tag",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Caption_PhotoId",
                table: "Caption",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_Face_PhotoId",
                table: "Face",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectProperty_PhotoId",
                table: "ObjectProperty",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoCategory_CategoryId",
                table: "PhotoCategory",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoTag_TagId",
                table: "PhotoTag",
                column: "TagId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Caption");

            migrationBuilder.DropTable(
                name: "Face");

            migrationBuilder.DropTable(
                name: "ObjectProperty");

            migrationBuilder.DropTable(
                name: "PhotoCategory");

            migrationBuilder.DropTable(
                name: "PhotoTag");

            migrationBuilder.DropTable(
                name: "Category");

            migrationBuilder.DropTable(
                name: "Tag");

            migrationBuilder.DropColumn(
                name: "AccentColor",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "AdultScore",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "DominantColorBackground",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "DominantColorForeground",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "DominantColors",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "IsAdultContent",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "IsBW",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "IsRacyContent",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "Orientation",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "PreviewImage",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "RacyScore",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "Photos");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Photos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Photos",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
