using Microsoft.EntityFrameworkCore.Migrations;

namespace PhotoBank.DbContext.Migrations
{
    public partial class F2FRemoveId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_FaceToFaces",
                table: "FaceToFaces");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "FaceToFaces");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FaceToFaces",
                table: "FaceToFaces",
                columns: new[] { "Face1Id", "Face2Id" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_FaceToFaces",
                table: "FaceToFaces");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "FaceToFaces",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FaceToFaces",
                table: "FaceToFaces",
                column: "Id");
        }
    }
}
