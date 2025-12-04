using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoBank.DbContext.Migrations
{
    /// <inheritdoc />
    public partial class FileIndexesFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Files_Name",
                table: "Files");

            migrationBuilder.DropIndex(
                name: "IX_Files_Name_PhotoId",
                table: "Files");

            migrationBuilder.CreateIndex(
                name: "IX_Files_Name_StorageId_RelativePath",
                table: "Files",
                columns: new[] { "Name", "StorageId", "RelativePath" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Files_Name_StorageId_RelativePath",
                table: "Files");

            migrationBuilder.CreateIndex(
                name: "IX_Files_Name",
                table: "Files",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Files_Name_PhotoId",
                table: "Files",
                columns: new[] { "Name", "PhotoId" },
                unique: true);
        }
    }
}
