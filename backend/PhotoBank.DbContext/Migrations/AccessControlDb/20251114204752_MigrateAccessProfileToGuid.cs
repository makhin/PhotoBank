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

            // IMPORTANT: The old code stored role NAMES (e.g., "Admin") in RoleAccessProfiles.RoleId,
            // not role IDs. We need to convert role names to role IDs before changing column type.

            // Step 1: Add temporary columns for the new GUIDs
            migrationBuilder.AddColumn<Guid>(
                name: "NewRoleId",
                table: "RoleAccessProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "NewUserId",
                table: "UserAccessProfiles",
                type: "uuid",
                nullable: true);

            // Step 2: Populate NewRoleId by looking up role names in AspNetRoles
            migrationBuilder.Sql(@"
                UPDATE ""RoleAccessProfiles"" rap
                SET ""NewRoleId"" = r.""Id""
                FROM ""AspNetRoles"" r
                WHERE r.""Name"" = rap.""RoleId""
                  OR r.""NormalizedName"" = UPPER(rap.""RoleId"")
                  OR r.""Id""::text = rap.""RoleId"";
            ");

            // Step 3: Populate NewUserId
            // Try to parse existing UserId as UUID, or look up in AspNetUsers
            migrationBuilder.Sql(@"
                UPDATE ""UserAccessProfiles"" uap
                SET ""NewUserId"" =
                    CASE
                        -- If UserId is already a valid UUID, use it
                        WHEN uap.""UserId"" ~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$'
                        THEN uap.""UserId""::uuid
                        -- Otherwise look up by username or email
                        ELSE (
                            SELECT u.""Id""
                            FROM ""AspNetUsers"" u
                            WHERE u.""UserName"" = uap.""UserId""
                               OR u.""Email"" = uap.""UserId""
                               OR u.""Id""::text = uap.""UserId""
                            LIMIT 1
                        )
                    END;
            ");

            // Step 4: Delete any rows that couldn't be mapped (orphaned data)
            migrationBuilder.Sql(@"DELETE FROM ""RoleAccessProfiles"" WHERE ""NewRoleId"" IS NULL;");
            migrationBuilder.Sql(@"DELETE FROM ""UserAccessProfiles"" WHERE ""NewUserId"" IS NULL;");

            // Step 5: Drop old primary keys
            migrationBuilder.DropPrimaryKey(
                name: "PK_RoleAccessProfiles",
                table: "RoleAccessProfiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserAccessProfiles",
                table: "UserAccessProfiles");

            // Step 6: Drop old columns
            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "RoleAccessProfiles");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserAccessProfiles");

            // Step 7: Rename new columns to original names
            migrationBuilder.RenameColumn(
                name: "NewRoleId",
                table: "RoleAccessProfiles",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "NewUserId",
                table: "UserAccessProfiles",
                newName: "UserId");

            // Step 8: Make the columns non-nullable
            migrationBuilder.AlterColumn<Guid>(
                name: "RoleId",
                table: "RoleAccessProfiles",
                type: "uuid",
                nullable: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "UserAccessProfiles",
                type: "uuid",
                nullable: false);

            // Step 9: Recreate primary keys
            migrationBuilder.AddPrimaryKey(
                name: "PK_RoleAccessProfiles",
                table: "RoleAccessProfiles",
                columns: new[] { "RoleId", "ProfileId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserAccessProfiles",
                table: "UserAccessProfiles",
                columns: new[] { "UserId", "ProfileId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // WARNING: Rolling back this migration will lose data.
            // We cannot reverse the conversion from role names to role IDs without knowing
            // which role names were originally stored.

            // Drop primary keys
            migrationBuilder.DropPrimaryKey(
                name: "PK_RoleAccessProfiles",
                table: "RoleAccessProfiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserAccessProfiles",
                table: "UserAccessProfiles");

            // Add temporary columns
            migrationBuilder.AddColumn<string>(
                name: "OldRoleId",
                table: "RoleAccessProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OldUserId",
                table: "UserAccessProfiles",
                type: "text",
                nullable: true);

            // Convert GUIDs back to strings (will be UUID strings, not original role names)
            migrationBuilder.Sql(@"UPDATE ""RoleAccessProfiles"" SET ""OldRoleId"" = ""RoleId""::text;");
            migrationBuilder.Sql(@"UPDATE ""UserAccessProfiles"" SET ""OldUserId"" = ""UserId""::text;");

            // Drop GUID columns
            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "RoleAccessProfiles");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserAccessProfiles");

            // Rename old columns back
            migrationBuilder.RenameColumn(
                name: "OldRoleId",
                table: "RoleAccessProfiles",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "OldUserId",
                table: "UserAccessProfiles",
                newName: "UserId");

            // Make columns non-nullable
            migrationBuilder.AlterColumn<string>(
                name: "RoleId",
                table: "RoleAccessProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserAccessProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Recreate primary keys
            migrationBuilder.AddPrimaryKey(
                name: "PK_RoleAccessProfiles",
                table: "RoleAccessProfiles",
                columns: new[] { "RoleId", "ProfileId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserAccessProfiles",
                table: "UserAccessProfiles",
                columns: new[] { "UserId", "ProfileId" });
        }
    }
}
