using Microsoft.EntityFrameworkCore.Migrations;

namespace PhotoBank.DbContext.Migrations
{
    public partial class AddIsSample : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSample",
                table: "Faces",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSample",
                table: "Faces");
        }
    }
}
