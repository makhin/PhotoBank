using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoBank.DbContext.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthDayIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TakenDay",
                table: "Photos",
                type: "int",
                nullable: true,
                computedColumnSql: "CASE WHEN [TakenDate] IS NULL THEN NULL ELSE DAY([TakenDate]) END PERSISTED");

            migrationBuilder.AddColumn<int>(
                name: "TakenMonth",
                table: "Photos",
                type: "int",
                nullable: true,
                computedColumnSql: "CASE WHEN [TakenDate] IS NULL THEN NULL ELSE MONTH([TakenDate]) END PERSISTED");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TakenDay",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "TakenMonth",
                table: "Photos");
        }
    }
}
