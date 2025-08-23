using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoBank.DbContext.Migrations
{
    /// <inheritdoc />
    public partial class RenamePersonGroupFaceToPersonFace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PersonGroupFace_Faces_FaceId",
                table: "PersonGroupFace");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonGroupFace_Persons_PersonId",
                table: "PersonGroupFace");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PersonGroupFace",
                table: "PersonGroupFace");

            migrationBuilder.RenameTable(
                name: "PersonGroupFace",
                newName: "PersonFace");

            migrationBuilder.RenameIndex(
                name: "IX_PersonGroupFace_PersonId",
                table: "PersonFace",
                newName: "IX_PersonFace_PersonId");

            migrationBuilder.RenameIndex(
                name: "IX_PersonGroupFace_FaceId",
                table: "PersonFace",
                newName: "IX_PersonFace_FaceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PersonFace",
                table: "PersonFace",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonFace_Faces_FaceId",
                table: "PersonFace",
                column: "FaceId",
                principalTable: "Faces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonFace_Persons_PersonId",
                table: "PersonFace",
                column: "PersonId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PersonFace_Faces_FaceId",
                table: "PersonFace");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonFace_Persons_PersonId",
                table: "PersonFace");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PersonFace",
                table: "PersonFace");

            migrationBuilder.RenameTable(
                name: "PersonFace",
                newName: "PersonGroupFace");

            migrationBuilder.RenameIndex(
                name: "IX_PersonFace_PersonId",
                table: "PersonGroupFace",
                newName: "IX_PersonGroupFace_PersonId");

            migrationBuilder.RenameIndex(
                name: "IX_PersonFace_FaceId",
                table: "PersonGroupFace",
                newName: "IX_PersonGroupFace_FaceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PersonGroupFace",
                table: "PersonGroupFace",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonGroupFace_Faces_FaceId",
                table: "PersonGroupFace",
                column: "FaceId",
                principalTable: "Faces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonGroupFace_Persons_PersonId",
                table: "PersonGroupFace",
                column: "PersonId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
