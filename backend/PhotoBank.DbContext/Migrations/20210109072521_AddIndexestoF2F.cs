using Microsoft.EntityFrameworkCore.Migrations;

namespace PhotoBank.DbContext.Migrations
{
    public partial class AddIndexestoF2F : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_FaceToFaces_Distance",
                table: "FaceToFaces",
                column: "Distance");

            migrationBuilder.CreateIndex(
                name: "IX_FaceToFaces_Face1Id",
                table: "FaceToFaces",
                column: "Face1Id");

            migrationBuilder.CreateIndex(
                name: "IX_FaceToFaces_Face2Id",
                table: "FaceToFaces",
                column: "Face2Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FaceToFaces_Distance",
                table: "FaceToFaces");

            migrationBuilder.DropIndex(
                name: "IX_FaceToFaces_Face1Id",
                table: "FaceToFaces");

            migrationBuilder.DropIndex(
                name: "IX_FaceToFaces_Face2Id",
                table: "FaceToFaces");
        }
    }
}
