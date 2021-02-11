using Microsoft.EntityFrameworkCore.Migrations;

namespace PhotoBank.DbContext.Migrations
{
    public partial class PersonFaceFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PersonFace_Persons_PersonId",
                table: "PersonFace");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonFace_Photos_PhotoId",
                table: "PersonFace");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PersonFace",
                table: "PersonFace");

            migrationBuilder.RenameTable(
                name: "PersonFace",
                newName: "PersonFaces");

            migrationBuilder.RenameIndex(
                name: "IX_PersonFace_PhotoId",
                table: "PersonFaces",
                newName: "IX_PersonFaces_PhotoId");

            migrationBuilder.RenameIndex(
                name: "IX_PersonFace_PersonId",
                table: "PersonFaces",
                newName: "IX_PersonFaces_PersonId");

            migrationBuilder.AlterColumn<double>(
                name: "IdentifiedWithConfidence",
                table: "PersonFaces",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PersonFaces",
                table: "PersonFaces",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonFaces_Persons_PersonId",
                table: "PersonFaces",
                column: "PersonId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonFaces_Photos_PhotoId",
                table: "PersonFaces",
                column: "PhotoId",
                principalTable: "Photos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PersonFaces_Persons_PersonId",
                table: "PersonFaces");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonFaces_Photos_PhotoId",
                table: "PersonFaces");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PersonFaces",
                table: "PersonFaces");

            migrationBuilder.RenameTable(
                name: "PersonFaces",
                newName: "PersonFace");

            migrationBuilder.RenameIndex(
                name: "IX_PersonFaces_PhotoId",
                table: "PersonFace",
                newName: "IX_PersonFace_PhotoId");

            migrationBuilder.RenameIndex(
                name: "IX_PersonFaces_PersonId",
                table: "PersonFace",
                newName: "IX_PersonFace_PersonId");

            migrationBuilder.AlterColumn<double>(
                name: "IdentifiedWithConfidence",
                table: "PersonFace",
                type: "float",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PersonFace",
                table: "PersonFace",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonFace_Persons_PersonId",
                table: "PersonFace",
                column: "PersonId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonFace_Photos_PhotoId",
                table: "PersonFace",
                column: "PhotoId",
                principalTable: "Photos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
