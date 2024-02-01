using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoBank.DbContext.Migrations
{
    /// <inheritdoc />
    public partial class AddFileUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Photos_PhotoId",
                table: "Files");

            migrationBuilder.AlterColumn<int>(
                name: "PhotoId",
                table: "Files",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Files_Name_PhotoId",
                table: "Files",
                columns: new[] { "Name", "PhotoId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Photos_PhotoId",
                table: "Files",
                column: "PhotoId",
                principalTable: "Photos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Photos_PhotoId",
                table: "Files");

            migrationBuilder.DropIndex(
                name: "IX_Files_Name_PhotoId",
                table: "Files");

            migrationBuilder.AlterColumn<int>(
                name: "PhotoId",
                table: "Files",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Photos_PhotoId",
                table: "Files",
                column: "PhotoId",
                principalTable: "Photos",
                principalColumn: "Id");
        }
    }
}
