using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoBank.DbContext.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOldFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Storages_StorageId",
                table: "Files");

            migrationBuilder.DropForeignKey(
                name: "FK_Photos_Storages_StorageId",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_Name_RelativePath",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_StorageId",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_StorageId_TakenDate",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "MigratedAt_Preview",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "MigratedAt_Thumbnail",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "RelativePath",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "MigratedAt_Image",
                table: "Faces");

            migrationBuilder.AlterColumn<int>(
                name: "StorageId",
                table: "Photos",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "StorageId",
                table: "Files",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RelativePath",
                table: "Files",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Photos_StorageId",
                table: "Photos",
                column: "StorageId");

            migrationBuilder.CreateIndex(
                name: "IX_Files_StorageId_RelativePath",
                table: "Files",
                columns: new[] { "StorageId", "RelativePath" });

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Storages_StorageId",
                table: "Files",
                column: "StorageId",
                principalTable: "Storages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Photos_Storages_StorageId",
                table: "Photos",
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

            migrationBuilder.DropForeignKey(
                name: "FK_Photos_Storages_StorageId",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_StorageId",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Files_StorageId_RelativePath",
                table: "Files");

            migrationBuilder.AlterColumn<int>(
                name: "StorageId",
                table: "Photos",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MigratedAt_Preview",
                table: "Photos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MigratedAt_Thumbnail",
                table: "Photos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelativePath",
                table: "Photos",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "StorageId",
                table: "Files",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "RelativePath",
                table: "Files",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<DateTime>(
                name: "MigratedAt_Image",
                table: "Faces",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Photos_Name_RelativePath",
                table: "Photos",
                columns: new[] { "Name", "RelativePath" });

            migrationBuilder.CreateIndex(
                name: "IX_Photos_StorageId",
                table: "Photos",
                column: "StorageId")
                .Annotation("Npgsql:IndexInclude", new[] { "RelativePath" });

            migrationBuilder.CreateIndex(
                name: "IX_Photos_StorageId_TakenDate",
                table: "Photos",
                columns: new[] { "StorageId", "TakenDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Storages_StorageId",
                table: "Files",
                column: "StorageId",
                principalTable: "Storages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Photos_Storages_StorageId",
                table: "Photos",
                column: "StorageId",
                principalTable: "Storages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
