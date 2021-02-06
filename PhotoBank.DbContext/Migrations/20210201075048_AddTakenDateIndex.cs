using Microsoft.EntityFrameworkCore.Migrations;

namespace PhotoBank.DbContext.Migrations
{
    public partial class AddTakenDateIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Photos_Storages_StorageId",
                table: "Photos");

            migrationBuilder.AlterColumn<int>(
                name: "StorageId",
                table: "Photos",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Photos_TakenDate",
                table: "Photos",
                column: "TakenDate");

            migrationBuilder.AddForeignKey(
                name: "FK_Photos_Storages_StorageId",
                table: "Photos",
                column: "StorageId",
                principalTable: "Storages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Photos_Storages_StorageId",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_TakenDate",
                table: "Photos");

            migrationBuilder.AlterColumn<int>(
                name: "StorageId",
                table: "Photos",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Photos_Storages_StorageId",
                table: "Photos",
                column: "StorageId",
                principalTable: "Storages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
