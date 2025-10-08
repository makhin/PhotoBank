using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoBank.DbContext.AccessControlMigrations
{
    /// <inheritdoc />
    public partial class BaselineResync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserAccessProfiles_ProfileId",
                table: "UserAccessProfiles",
                column: "ProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAccessProfiles_AccessProfiles_ProfileId",
                table: "UserAccessProfiles",
                column: "ProfileId",
                principalTable: "AccessProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserAccessProfiles_AccessProfiles_ProfileId",
                table: "UserAccessProfiles");

            migrationBuilder.DropIndex(
                name: "IX_UserAccessProfiles_ProfileId",
                table: "UserAccessProfiles");
        }
    }
}
