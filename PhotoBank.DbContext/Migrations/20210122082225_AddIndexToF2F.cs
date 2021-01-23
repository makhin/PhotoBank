using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PhotoBank.DbContext.Migrations
{
    public partial class AddIndexToF2F : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FaceToFaces_Distance",
                table: "FaceToFaces");

            migrationBuilder.CreateIndex(
                name: "IX_FaceToFaces_Distance",
                table: "FaceToFaces",
                column: "Distance")
                .Annotation("SqlServer:Include", new[] { "Face1Id", "Face2Id" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FaceToFaces_Distance",
                table: "FaceToFaces");

            migrationBuilder.CreateIndex(
                name: "IX_FaceToFaces_Distance",
                table: "FaceToFaces",
                column: "Distance");
        }
    }
}
