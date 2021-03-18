using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PhotoBank.DbContext.Migrations
{
    public partial class AddEnrichers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaceToFaces");

            migrationBuilder.CreateTable(
                name: "Enrichers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrichers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enrichers_Name",
                table: "Enrichers",
                column: "Name",
                unique: true,
                filter: "[Name] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Enrichers");

            migrationBuilder.CreateTable(
                name: "FaceToFaces",
                columns: table => new
                {
                    Distance = table.Column<double>(type: "float", nullable: false),
                    Face1Id = table.Column<int>(type: "int", nullable: false),
                    Face2Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateIndex(
                name: "IX_FaceToFaces_Distance",
                table: "FaceToFaces",
                column: "Distance")
                .Annotation("SqlServer:Include", new[] { "Face1Id", "Face2Id" });

            migrationBuilder.CreateIndex(
                name: "IX_FaceToFaces_Face1Id",
                table: "FaceToFaces",
                column: "Face1Id");

            migrationBuilder.CreateIndex(
                name: "IX_FaceToFaces_Face2Id",
                table: "FaceToFaces",
                column: "Face2Id");
        }
    }
}
