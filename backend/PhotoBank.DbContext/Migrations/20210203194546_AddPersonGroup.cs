using Microsoft.EntityFrameworkCore.Migrations;

namespace PhotoBank.DbContext.Migrations
{
    public partial class AddPersonGroup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersonGroup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersonPersonGroup",
                columns: table => new
                {
                    PersonGroupsId = table.Column<int>(type: "int", nullable: false),
                    PersonsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonPersonGroup", x => new { x.PersonGroupsId, x.PersonsId });
                    table.ForeignKey(
                        name: "FK_PersonPersonGroup_PersonGroup_PersonGroupsId",
                        column: x => x.PersonGroupsId,
                        principalTable: "PersonGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonPersonGroup_Persons_PersonsId",
                        column: x => x.PersonsId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonPersonGroup_PersonsId",
                table: "PersonPersonGroup",
                column: "PersonsId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonPersonGroup");

            migrationBuilder.DropTable(
                name: "PersonGroup");
        }
    }
}
