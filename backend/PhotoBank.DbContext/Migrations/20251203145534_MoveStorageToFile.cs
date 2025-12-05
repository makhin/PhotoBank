using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace PhotoBank.DbContext.Migrations
{
    /// <inheritdoc />
    public partial class MoveStorageToFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RelativePath",
                table: "Files",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StorageId",
                table: "Files",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<Geometry>(
                name: "Rectangle",
                table: "Faces",
                type: "geometry",
                nullable: true,
                oldClrType: typeof(Geometry),
                oldType: "geometry");

            migrationBuilder.AlterColumn<string>(
                name: "FaceAttributes",
                table: "Faces",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_Files_StorageId",
                table: "Files",
                column: "StorageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Storages_StorageId",
                table: "Files",
                column: "StorageId",
                principalTable: "Storages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Storages_StorageId",
                table: "Files");

            migrationBuilder.DropIndex(
                name: "IX_Files_StorageId",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "RelativePath",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "StorageId",
                table: "Files");

            migrationBuilder.AlterColumn<Geometry>(
                name: "Rectangle",
                table: "Faces",
                type: "geometry",
                nullable: false,
                oldClrType: typeof(Geometry),
                oldType: "geometry",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FaceAttributes",
                table: "Faces",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
