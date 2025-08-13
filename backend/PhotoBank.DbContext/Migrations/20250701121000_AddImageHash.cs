using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoBank.DbContext.Migrations
{
    /// <inheritdoc />
    public partial class AddImageHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageHash",
                table: "Photos",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageHash",
                table: "Photos");
        }
    }
}
