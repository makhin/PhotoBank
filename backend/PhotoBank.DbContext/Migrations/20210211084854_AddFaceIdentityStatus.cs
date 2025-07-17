using Microsoft.EntityFrameworkCore.Migrations;

namespace PhotoBank.DbContext.Migrations
{
    public partial class AddFaceIdentityStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ListStatus",
                table: "Faces");

            migrationBuilder.AddColumn<int>(
                name: "FaceIdentifyStatus",
                table: "Photos",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FaceIdentifyStatus",
                table: "Photos");

            migrationBuilder.AddColumn<int>(
                name: "ListStatus",
                table: "Faces",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
