using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PhotoBank.DbContext.Migrations
{
    public partial class AddIndexToFaces : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Faces_IdentityStatus",
                table: "Faces",
                column: "IdentityStatus")
                .Annotation("SqlServer:Include", new[] { "PersonId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Faces_IdentityStatus",
                table: "Faces");
        }
    }
}
