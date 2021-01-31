using Microsoft.EntityFrameworkCore.Migrations;

namespace PhotoBank.DbContext.Migrations
{
    public partial class FacePersonId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Faces_Persons_PersonId",
                table: "Faces");

            migrationBuilder.AlterColumn<int>(
                name: "PersonId",
                table: "Faces",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Faces_Persons_PersonId",
                table: "Faces",
                column: "PersonId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Faces_Persons_PersonId",
                table: "Faces");

            migrationBuilder.AlterColumn<int>(
                name: "PersonId",
                table: "Faces",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Faces_Persons_PersonId",
                table: "Faces",
                column: "PersonId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
