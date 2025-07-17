using Microsoft.EntityFrameworkCore.Migrations;

namespace PhotoBank.DbContext.Migrations
{
    public partial class AlterIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.DropIndex(
                name: "IX_Faces_PhotoId",
                table: "Faces");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoTags_PhotoId",
                table: "PhotoTags",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_IsAdultContent_IsPrivate",
                table: "Photos",
                columns: new[] { "IsAdultContent", "IsPrivate" });

            migrationBuilder.CreateIndex(
                name: "IX_Photos_IsBW_IsPrivate",
                table: "Photos",
                columns: new[] { "IsBW", "IsPrivate" });

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

            migrationBuilder.CreateIndex(
                name: "IX_Faces_PhotoId_Id_PersonId",
                table: "Faces",
                columns: new[] { "PhotoId", "Id", "PersonId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PhotoTags_PhotoId",
                table: "PhotoTags");

            migrationBuilder.DropIndex(
                name: "IX_Photos_IsAdultContent_IsPrivate",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_IsBW_IsPrivate",
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

            migrationBuilder.DropIndex(
                name: "IX_Faces_PhotoId_Id_PersonId",
                table: "Faces");

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

            migrationBuilder.CreateIndex(
                name: "IX_Faces_PhotoId",
                table: "Faces",
                column: "PhotoId");
        }
    }
}
