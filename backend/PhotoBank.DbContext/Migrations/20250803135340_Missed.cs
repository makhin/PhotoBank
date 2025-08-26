using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoBank.DbContext.Migrations
{
    /// <inheritdoc />
    public partial class Missed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PersonPersonGroup_PersonGroup_PersonGroupsId",
                table: "PersonPersonGroup");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PersonGroup",
                table: "PersonGroup");

            migrationBuilder.RenameTable(
                name: "PersonGroup",
                newName: "PersonGroups");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PersonGroups",
                table: "PersonGroups",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonPersonGroup_PersonGroups_PersonGroupsId",
                table: "PersonPersonGroup",
                column: "PersonGroupsId",
                principalTable: "PersonGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PersonPersonGroup_PersonGroups_PersonGroupsId",
                table: "PersonPersonGroup");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PersonGroups",
                table: "PersonGroups");

            migrationBuilder.RenameTable(
                name: "PersonGroups",
                newName: "PersonGroup");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PersonGroup",
                table: "PersonGroup",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonPersonGroup_PersonGroup_PersonGroupsId",
                table: "PersonPersonGroup",
                column: "PersonGroupsId",
                principalTable: "PersonGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
