using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoBank.DbContext.Migrations
{
    /// <inheritdoc />
    public partial class RemoveImagesFromDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Faces_NeedsMigration",
                table: "Faces");

            migrationBuilder.DropColumn(
                name: "PreviewImage",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "Thumbnail",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "Image",
                table: "Faces");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "PreviewImage",
                table: "Photos",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Thumbnail",
                table: "Photos",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Image",
                table: "Faces",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Faces_NeedsMigration",
                table: "Faces",
                column: "Id",
                filter: "[S3Key_Image] IS NULL")
                .Annotation("SqlServer:Include", new[] { "S3Key_Image" });
        }
    }
}
