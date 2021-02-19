using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PhotoBank.DbContext.Migrations
{
    public partial class AddRelativepathIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Photos_StorageId",
                table: "Photos");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_StorageId",
                table: "Photos",
                column: "StorageId")
                .Annotation("SqlServer:Include", new[] { "RelativePath" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Photos_StorageId",
                table: "Photos");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_StorageId",
                table: "Photos",
                column: "StorageId");
        }
    }
}
