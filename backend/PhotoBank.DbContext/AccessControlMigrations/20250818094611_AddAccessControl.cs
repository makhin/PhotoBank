using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoBank.DbContext.AccessControlMigrations
{
    /// <inheritdoc />
    public partial class AddAccessControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Flags_NsfwOnly = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleAccessProfiles",
                columns: table => new
                {
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProfileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleAccessProfiles", x => new { x.RoleId, x.ProfileId });
                });

            migrationBuilder.CreateTable(
                name: "UserAccessProfiles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProfileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccessProfiles", x => new { x.UserId, x.ProfileId });
                });

            migrationBuilder.CreateTable(
                name: "AccessProfileDateRanges",
                columns: table => new
                {
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    FromDate = table.Column<DateTime>(type: "date", nullable: false),
                    ToDate = table.Column<DateTime>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessProfileDateRanges", x => new { x.ProfileId, x.FromDate, x.ToDate });
                    table.ForeignKey(
                        name: "FK_AccessProfileDateRanges_AccessProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "AccessProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccessProfilePersonGroups",
                columns: table => new
                {
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    PersonGroupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessProfilePersonGroups", x => new { x.ProfileId, x.PersonGroupId });
                    table.ForeignKey(
                        name: "FK_AccessProfilePersonGroups_AccessProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "AccessProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccessProfileStorages",
                columns: table => new
                {
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    StorageId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessProfileStorages", x => new { x.ProfileId, x.StorageId });
                    table.ForeignKey(
                        name: "FK_AccessProfileStorages_AccessProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "AccessProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessProfiles_Name",
                table: "AccessProfiles",
                column: "Name",
                unique: true,
                filter: "[Name] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessProfileDateRanges");

            migrationBuilder.DropTable(
                name: "AccessProfilePersonGroups");

            migrationBuilder.DropTable(
                name: "AccessProfileStorages");

            migrationBuilder.DropTable(
                name: "RoleAccessProfiles");

            migrationBuilder.DropTable(
                name: "UserAccessProfiles");

            migrationBuilder.DropTable(
                name: "AccessProfiles");
        }
    }
}
