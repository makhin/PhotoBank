using Microsoft.EntityFrameworkCore.Migrations;

namespace PhotoBank.DbContext.Migrations
{
    public partial class AddListAndIdentityStatuses : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Faces",
                newName: "ListStatus");

            migrationBuilder.AddColumn<double>(
                name: "IdentifiedWithConfidence",
                table: "Faces",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "IdentityStatus",
                table: "Faces",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdentifiedWithConfidence",
                table: "Faces");

            migrationBuilder.DropColumn(
                name: "IdentityStatus",
                table: "Faces");

            migrationBuilder.RenameColumn(
                name: "ListStatus",
                table: "Faces",
                newName: "Status");
        }
    }
}
