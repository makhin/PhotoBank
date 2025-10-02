using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoBank.DbContext.Migrations
{
    /// <inheritdoc />
    public partial class MergePersonFaceIntoFaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExternalGuid",
                table: "Faces",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Faces",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "Faces",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE f SET f.PersonId = pf.PersonId, f.Provider = pf.Provider, f.ExternalId = pf.ExternalId, f.ExternalGuid = pf.ExternalGuid FROM Faces f INNER JOIN PersonFace pf ON pf.FaceId = f.Id");

            migrationBuilder.CreateIndex(
                name: "IX_Faces_ExternalGuid",
                table: "Faces",
                column: "ExternalGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Faces_Provider_ExternalId",
                table: "Faces",
                columns: new[] { "Provider", "ExternalId" });

            migrationBuilder.DropTable(
                name: "PersonFace");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Faces_ExternalGuid",
                table: "Faces");

            migrationBuilder.DropIndex(
                name: "IX_Faces_Provider_ExternalId",
                table: "Faces");

            migrationBuilder.DropColumn(
                name: "ExternalGuid",
                table: "Faces");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Faces");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "Faces");

            migrationBuilder.CreateTable(
                name: "PersonFace",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FaceId = table.Column<int>(type: "int", nullable: false),
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    ExternalGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonFace", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonFace_Faces_FaceId",
                        column: x => x.FaceId,
                        principalTable: "Faces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonFace_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonFace_FaceId",
                table: "PersonFace",
                column: "FaceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonFace_PersonId",
                table: "PersonFace",
                column: "PersonId");

            migrationBuilder.Sql(
                "INSERT INTO PersonFace (FaceId, PersonId, Provider, ExternalId, ExternalGuid) SELECT Id, PersonId, Provider, ExternalId, ExternalGuid FROM Faces WHERE PersonId IS NOT NULL");
        }
    }
}
