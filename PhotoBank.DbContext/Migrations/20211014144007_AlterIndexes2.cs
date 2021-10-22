using Microsoft.EntityFrameworkCore.Migrations;

namespace PhotoBank.DbContext.Migrations
{
    public partial class AlterIndexes2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Photos_Id_IsPrivate",
                table: "Photos",
                columns: new[] { "Id", "IsPrivate" });

            migrationBuilder.CreateIndex(
                name: "IX_Photos_IsPrivate",
                table: "Photos",
                column: "IsPrivate");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Photos_Id_IsPrivate",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_IsPrivate",
                table: "Photos");
        }
    }
}
