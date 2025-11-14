using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace PhotoBank.DbContext.Migrations.AccessControlDb
{
    /// <inheritdoc />
    public partial class MigrateAccessProfileToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // WARNING: This migration converts RoleAccessProfile.RoleId and UserAccessProfile.UserId
            // from string to Guid to match the Identity tables migration.
            // Ensure the main Identity migration has been applied first.

            // Convert RoleAccessProfile.RoleId from text to uuid
            migrationBuilder.AlterColumn<Guid>(
                name: "RoleId",
                table: "RoleAccessProfiles",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            // Convert UserAccessProfile.UserId from text to uuid
            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "UserAccessProfiles",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Convert back from uuid to text
            migrationBuilder.AlterColumn<string>(
                name: "RoleId",
                table: "RoleAccessProfiles",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserAccessProfiles",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
