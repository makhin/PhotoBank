using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoBank.DbContext.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFaceImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Faces_NeedsMigration",
                table: "Faces");

            migrationBuilder.DropColumn(
                name: "Image",
                table: "Faces");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "Image",
                table: "Faces",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Faces_NeedsMigration",
                table: "Faces",
                column: "Id",
                filter: "[S3Key_Image] IS NULL")
                .Annotation("SqlServer:Include", new[] { "S3Key_Image" });
        }
    }
}
