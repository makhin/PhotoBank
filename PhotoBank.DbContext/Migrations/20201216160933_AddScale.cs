using Microsoft.EntityFrameworkCore.Migrations;

namespace PhotoBank.DbContext.Migrations
{
    public partial class AddScale : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Faces");

            migrationBuilder.RenameColumn(
                name: "Path",
                table: "Photos",
                newName: "RelativePath");

            migrationBuilder.AddColumn<double>(
                name: "Scale",
                table: "Photos",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Scale",
                table: "Photos");

            migrationBuilder.RenameColumn(
                name: "RelativePath",
                table: "Photos",
                newName: "Path");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Faces",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
