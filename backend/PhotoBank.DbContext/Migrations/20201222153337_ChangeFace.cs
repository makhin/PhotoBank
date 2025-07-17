using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PhotoBank.DbContext.Migrations
{
    public partial class ChangeFace : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaceListFaces");

            migrationBuilder.DropTable(
                name: "FaceLists");

            migrationBuilder.AddColumn<Guid>(
                name: "ExternalGuid",
                table: "Faces",
                type: "uniqueidentifier",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalGuid",
                table: "Faces");

            migrationBuilder.CreateTable(
                name: "FaceLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaceLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FaceListFaces",
                columns: table => new
                {
                    FaceListId = table.Column<int>(type: "int", nullable: false),
                    FaceId = table.Column<int>(type: "int", nullable: false),
                    ExternalGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaceListFaces", x => new { x.FaceListId, x.FaceId });
                    table.ForeignKey(
                        name: "FK_FaceListFaces_FaceLists_FaceListId",
                        column: x => x.FaceListId,
                        principalTable: "FaceLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FaceListFaces_Faces_FaceId",
                        column: x => x.FaceId,
                        principalTable: "Faces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FaceListFaces_FaceId",
                table: "FaceListFaces",
                column: "FaceId");
        }
    }
}
