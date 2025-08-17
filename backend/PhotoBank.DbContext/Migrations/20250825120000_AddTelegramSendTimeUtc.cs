using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoBank.DbContext.Migrations
{
    public partial class AddTelegramSendTimeUtc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "TelegramSendTimeUtc",
                table: "AspNetUsers",
                type: "time",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TelegramSendTimeUtc",
                table: "AspNetUsers");
        }
    }
}
