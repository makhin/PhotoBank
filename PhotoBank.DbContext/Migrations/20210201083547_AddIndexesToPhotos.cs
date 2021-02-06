using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PhotoBank.DbContext.Migrations
{
    public partial class AddIndexesToPhotos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Faces_Photos_PhotoId",
                table: "Faces");

            migrationBuilder.DropIndex(
                name: "IX_Faces_PersonId",
                table: "Faces");

            migrationBuilder.AlterColumn<int>(
                name: "PhotoId",
                table: "Faces",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Photos_IsAdultContent",
                table: "Photos",
                column: "IsAdultContent");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_IsBW",
                table: "Photos",
                column: "IsBW");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_IsRacyContent",
                table: "Photos",
                column: "IsRacyContent");

            migrationBuilder.CreateIndex(
                name: "IX_Faces_PersonId",
                table: "Faces",
                column: "PersonId")
                .Annotation("SqlServer:Include", new[] { "PhotoId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Faces_Photos_PhotoId",
                table: "Faces",
                column: "PhotoId",
                principalTable: "Photos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Faces_Photos_PhotoId",
                table: "Faces");

            migrationBuilder.DropIndex(
                name: "IX_Photos_IsAdultContent",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_IsBW",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_IsRacyContent",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Faces_PersonId",
                table: "Faces");

            migrationBuilder.AlterColumn<int>(
                name: "PhotoId",
                table: "Faces",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Faces_PersonId",
                table: "Faces",
                column: "PersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_Faces_Photos_PhotoId",
                table: "Faces",
                column: "PhotoId",
                principalTable: "Photos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
