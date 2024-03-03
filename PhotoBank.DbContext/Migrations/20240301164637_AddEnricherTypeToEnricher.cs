using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoBank.DbContext.Migrations
{
    /// <inheritdoc />
    public partial class AddEnricherTypeToEnricher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Enrichers_Name",
                table: "Enrichers");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Enrichers",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EnricherType",
                table: "Enrichers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Enrichers_Name",
                table: "Enrichers",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Enrichers_Name",
                table: "Enrichers");

            migrationBuilder.DropColumn(
                name: "EnricherType",
                table: "Enrichers");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Enrichers",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.CreateIndex(
                name: "IX_Enrichers_Name",
                table: "Enrichers",
                column: "Name",
                unique: true,
                filter: "[Name] IS NOT NULL");
        }
    }
}
