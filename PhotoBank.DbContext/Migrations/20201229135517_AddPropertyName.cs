using Microsoft.EntityFrameworkCore.Migrations;

namespace PhotoBank.DbContext.Migrations
{
    public partial class AddPropertyName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PropertyNameId",
                table: "ObjectProperties",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Files",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PropertyNames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyNames", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ObjectProperties_PropertyNameId",
                table: "ObjectProperties",
                column: "PropertyNameId");

            migrationBuilder.AddForeignKey(
                name: "FK_ObjectProperties_PropertyNames_PropertyNameId",
                table: "ObjectProperties",
                column: "PropertyNameId",
                principalTable: "PropertyNames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ObjectProperties_PropertyNames_PropertyNameId",
                table: "ObjectProperties");

            migrationBuilder.DropTable(
                name: "PropertyNames");

            migrationBuilder.DropIndex(
                name: "IX_ObjectProperties_PropertyNameId",
                table: "ObjectProperties");

            migrationBuilder.DropColumn(
                name: "PropertyNameId",
                table: "ObjectProperties");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Files");
        }
    }
}
