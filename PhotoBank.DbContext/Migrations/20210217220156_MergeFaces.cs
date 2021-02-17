using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

namespace PhotoBank.DbContext.Migrations
{
    public partial class MergeFaces : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonFaces");

            migrationBuilder.DropColumn(
                name: "Encoding",
                table: "Faces");

            migrationBuilder.DropColumn(
                name: "ExternalGuid",
                table: "Faces");

            migrationBuilder.AlterColumn<bool>(
                name: "Gender",
                table: "Faces",
                type: "bit",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Age",
                table: "Faces",
                type: "float",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "FaceAttributes",
                table: "Faces",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Smile",
                table: "Faces",
                type: "float",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FaceAttributes",
                table: "Faces");

            migrationBuilder.DropColumn(
                name: "Smile",
                table: "Faces");

            migrationBuilder.AlterColumn<int>(
                name: "Gender",
                table: "Faces",
                type: "int",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Age",
                table: "Faces",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Encoding",
                table: "Faces",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExternalGuid",
                table: "Faces",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PersonFaces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Age = table.Column<double>(type: "float", nullable: true),
                    FaceAttributes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Gender = table.Column<bool>(type: "bit", nullable: true),
                    IdentifiedWithConfidence = table.Column<double>(type: "float", nullable: true),
                    IdentityStatus = table.Column<int>(type: "int", nullable: false),
                    Image = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    PersonId = table.Column<int>(type: "int", nullable: true),
                    PhotoId = table.Column<int>(type: "int", nullable: false),
                    Rectangle = table.Column<Geometry>(type: "geometry", nullable: true),
                    Smile = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonFaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonFaces_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonFaces_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonFaces_PersonId",
                table: "PersonFaces",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonFaces_PhotoId",
                table: "PersonFaces",
                column: "PhotoId");
        }
    }
}
