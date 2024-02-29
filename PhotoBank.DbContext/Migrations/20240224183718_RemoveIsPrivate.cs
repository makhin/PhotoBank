using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoBank.DbContext.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsPrivate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Photos_Id_IsPrivate",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_IsAdultContent_IsPrivate",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_IsBW_IsPrivate",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_IsPrivate",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_IsRacyContent_IsPrivate",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_Name_RelativePath_IsPrivate",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_TakenDate_IsPrivate",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "Photos");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_Id",
                table: "Photos",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_IsAdultContent",
                table: "Photos",
                column: "IsAdultContent");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_IsBW",
                table: "Photos",
                column: "IsBW");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_IsRacyContent",
                table: "Photos",
                column: "IsRacyContent");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_Name_RelativePath",
                table: "Photos",
                columns: new[] { "Name", "RelativePath" });

            migrationBuilder.CreateIndex(
                name: "IX_Photos_TakenDate",
                table: "Photos",
                column: "TakenDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Photos_Id",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_IsAdultContent",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_IsBW",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_IsRacyContent",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_Name_RelativePath",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_TakenDate",
                table: "Photos");

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "Photos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Photos_Id_IsPrivate",
                table: "Photos",
                columns: new[] { "Id", "IsPrivate" });

            migrationBuilder.CreateIndex(
                name: "IX_Photos_IsAdultContent_IsPrivate",
                table: "Photos",
                columns: new[] { "IsAdultContent", "IsPrivate" });

            migrationBuilder.CreateIndex(
                name: "IX_Photos_IsBW_IsPrivate",
                table: "Photos",
                columns: new[] { "IsBW", "IsPrivate" });

            migrationBuilder.CreateIndex(
                name: "IX_Photos_IsPrivate",
                table: "Photos",
                column: "IsPrivate");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_IsRacyContent_IsPrivate",
                table: "Photos",
                columns: new[] { "IsRacyContent", "IsPrivate" });

            migrationBuilder.CreateIndex(
                name: "IX_Photos_Name_RelativePath_IsPrivate",
                table: "Photos",
                columns: new[] { "Name", "RelativePath", "IsPrivate" });

            migrationBuilder.CreateIndex(
                name: "IX_Photos_TakenDate_IsPrivate",
                table: "Photos",
                columns: new[] { "TakenDate", "IsPrivate" });
        }
    }
}
