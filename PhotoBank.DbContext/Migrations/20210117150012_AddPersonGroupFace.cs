using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PhotoBank.DbContext.Migrations
{
    public partial class AddPersonGroupFace : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonGroupPersons");

            migrationBuilder.DropTable(
                name: "PersonGroups");

            migrationBuilder.DropColumn(
                name: "CheckedWithTolerance",
                table: "Faces");

            migrationBuilder.DropColumn(
                name: "IsExcluded",
                table: "Faces");

            migrationBuilder.DropColumn(
                name: "IsSample",
                table: "Faces");

            migrationBuilder.CreateTable(
                name: "PersonGroupFace",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    FaceId = table.Column<int>(type: "int", nullable: false),
                    ExternalGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonGroupFace", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonGroupFace_Faces_FaceId",
                        column: x => x.FaceId,
                        principalTable: "Faces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonGroupFace_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonGroupFace_FaceId",
                table: "PersonGroupFace",
                column: "FaceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonGroupFace_PersonId",
                table: "PersonGroupFace",
                column: "PersonId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonGroupFace");

            migrationBuilder.AddColumn<double>(
                name: "CheckedWithTolerance",
                table: "Faces",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<bool>(
                name: "IsExcluded",
                table: "Faces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSample",
                table: "Faces",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PersonGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersonGroupPersons",
                columns: table => new
                {
                    PersonGroupId = table.Column<int>(type: "int", nullable: false),
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    ExternalGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonGroupPersons", x => new { x.PersonGroupId, x.PersonId });
                    table.ForeignKey(
                        name: "FK_PersonGroupPersons_PersonGroups_PersonGroupId",
                        column: x => x.PersonGroupId,
                        principalTable: "PersonGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonGroupPersons_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonGroupPersons_PersonId",
                table: "PersonGroupPersons",
                column: "PersonId");
        }
    }
}
