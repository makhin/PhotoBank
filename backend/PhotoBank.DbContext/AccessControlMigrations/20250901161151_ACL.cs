using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoBank.DbContext.AccessControlMigrations
{
    /// <inheritdoc />
    public partial class ACL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccessProfiles_Name",
                table: "AccessProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AccessProfiles",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessProfiles_Name",
                table: "AccessProfiles",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccessProfiles_Name",
                table: "AccessProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AccessProfiles",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.CreateIndex(
                name: "IX_AccessProfiles_Name",
                table: "AccessProfiles",
                column: "Name",
                unique: true,
                filter: "[Name] IS NOT NULL");
        }
    }
}
