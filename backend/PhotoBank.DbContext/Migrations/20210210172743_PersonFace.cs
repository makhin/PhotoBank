using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

namespace PhotoBank.DbContext.Migrations
{
    public partial class PersonFace : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersonFace",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonId = table.Column<int>(type: "int", nullable: true),
                    PhotoId = table.Column<int>(type: "int", nullable: false),
                    Age = table.Column<double>(type: "float", nullable: true),
                    Gender = table.Column<bool>(type: "bit", nullable: true),
                    Smile = table.Column<double>(type: "float", nullable: true),
                    Image = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    Rectangle = table.Column<Geometry>(type: "geometry", nullable: true),
                    IdentityStatus = table.Column<int>(type: "int", nullable: false),
                    IdentifiedWithConfidence = table.Column<double>(type: "float", nullable: false),
                    FaceAttributes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonFace", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonFace_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonFace_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonFace_PersonId",
                table: "PersonFace",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonFace_PhotoId",
                table: "PersonFace",
                column: "PhotoId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonFace");
        }
    }
}
