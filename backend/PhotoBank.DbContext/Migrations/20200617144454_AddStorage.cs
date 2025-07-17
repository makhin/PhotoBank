using Microsoft.EntityFrameworkCore.Migrations;

namespace PhotoBank.DbContext.Migrations
{
    public partial class AddStorage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Caption_Photos_PhotoId",
                table: "Caption");

            migrationBuilder.DropForeignKey(
                name: "FK_Face_Photos_PhotoId",
                table: "Face");

            migrationBuilder.DropForeignKey(
                name: "FK_ObjectProperty_Photos_PhotoId",
                table: "ObjectProperty");

            migrationBuilder.DropForeignKey(
                name: "FK_PhotoCategory_Category_CategoryId",
                table: "PhotoCategory");

            migrationBuilder.DropForeignKey(
                name: "FK_PhotoCategory_Photos_PhotoId",
                table: "PhotoCategory");

            migrationBuilder.DropForeignKey(
                name: "FK_PhotoTag_Photos_PhotoId",
                table: "PhotoTag");

            migrationBuilder.DropForeignKey(
                name: "FK_PhotoTag_Tag_TagId",
                table: "PhotoTag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tag",
                table: "Tag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PhotoTag",
                table: "PhotoTag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PhotoCategory",
                table: "PhotoCategory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ObjectProperty",
                table: "ObjectProperty");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Face",
                table: "Face");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Category",
                table: "Category");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Caption",
                table: "Caption");

            migrationBuilder.RenameTable(
                name: "Tag",
                newName: "Tags");

            migrationBuilder.RenameTable(
                name: "PhotoTag",
                newName: "PhotoTags");

            migrationBuilder.RenameTable(
                name: "PhotoCategory",
                newName: "PhotoCategories");

            migrationBuilder.RenameTable(
                name: "ObjectProperty",
                newName: "ObjectProperties");

            migrationBuilder.RenameTable(
                name: "Face",
                newName: "Faces");

            migrationBuilder.RenameTable(
                name: "Category",
                newName: "Categories");

            migrationBuilder.RenameTable(
                name: "Caption",
                newName: "Captions");

            migrationBuilder.RenameIndex(
                name: "IX_PhotoTag_TagId",
                table: "PhotoTags",
                newName: "IX_PhotoTags_TagId");

            migrationBuilder.RenameIndex(
                name: "IX_PhotoCategory_CategoryId",
                table: "PhotoCategories",
                newName: "IX_PhotoCategories_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_ObjectProperty_PhotoId",
                table: "ObjectProperties",
                newName: "IX_ObjectProperties_PhotoId");

            migrationBuilder.RenameIndex(
                name: "IX_Face_PhotoId",
                table: "Faces",
                newName: "IX_Faces_PhotoId");

            migrationBuilder.RenameIndex(
                name: "IX_Caption_PhotoId",
                table: "Captions",
                newName: "IX_Captions_PhotoId");

            migrationBuilder.AddColumn<string>(
                name: "Path",
                table: "Photos",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StorageId",
                table: "Photos",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tags",
                table: "Tags",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PhotoTags",
                table: "PhotoTags",
                columns: new[] { "PhotoId", "TagId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_PhotoCategories",
                table: "PhotoCategories",
                columns: new[] { "PhotoId", "CategoryId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ObjectProperties",
                table: "ObjectProperties",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Faces",
                table: "Faces",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Categories",
                table: "Categories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Captions",
                table: "Captions",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Storages",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(nullable: true),
                    Folder = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Storages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Photos_StorageId",
                table: "Photos",
                column: "StorageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Captions_Photos_PhotoId",
                table: "Captions",
                column: "PhotoId",
                principalTable: "Photos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Faces_Photos_PhotoId",
                table: "Faces",
                column: "PhotoId",
                principalTable: "Photos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ObjectProperties_Photos_PhotoId",
                table: "ObjectProperties",
                column: "PhotoId",
                principalTable: "Photos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhotoCategories_Categories_CategoryId",
                table: "PhotoCategories",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PhotoCategories_Photos_PhotoId",
                table: "PhotoCategories",
                column: "PhotoId",
                principalTable: "Photos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Photos_Storages_StorageId",
                table: "Photos",
                column: "StorageId",
                principalTable: "Storages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhotoTags_Photos_PhotoId",
                table: "PhotoTags",
                column: "PhotoId",
                principalTable: "Photos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PhotoTags_Tags_TagId",
                table: "PhotoTags",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Captions_Photos_PhotoId",
                table: "Captions");

            migrationBuilder.DropForeignKey(
                name: "FK_Faces_Photos_PhotoId",
                table: "Faces");

            migrationBuilder.DropForeignKey(
                name: "FK_ObjectProperties_Photos_PhotoId",
                table: "ObjectProperties");

            migrationBuilder.DropForeignKey(
                name: "FK_PhotoCategories_Categories_CategoryId",
                table: "PhotoCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_PhotoCategories_Photos_PhotoId",
                table: "PhotoCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_Photos_Storages_StorageId",
                table: "Photos");

            migrationBuilder.DropForeignKey(
                name: "FK_PhotoTags_Photos_PhotoId",
                table: "PhotoTags");

            migrationBuilder.DropForeignKey(
                name: "FK_PhotoTags_Tags_TagId",
                table: "PhotoTags");

            migrationBuilder.DropTable(
                name: "Storages");

            migrationBuilder.DropIndex(
                name: "IX_Photos_StorageId",
                table: "Photos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tags",
                table: "Tags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PhotoTags",
                table: "PhotoTags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PhotoCategories",
                table: "PhotoCategories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ObjectProperties",
                table: "ObjectProperties");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Faces",
                table: "Faces");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Categories",
                table: "Categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Captions",
                table: "Captions");

            migrationBuilder.DropColumn(
                name: "Path",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "StorageId",
                table: "Photos");

            migrationBuilder.RenameTable(
                name: "Tags",
                newName: "Tag");

            migrationBuilder.RenameTable(
                name: "PhotoTags",
                newName: "PhotoTag");

            migrationBuilder.RenameTable(
                name: "PhotoCategories",
                newName: "PhotoCategory");

            migrationBuilder.RenameTable(
                name: "ObjectProperties",
                newName: "ObjectProperty");

            migrationBuilder.RenameTable(
                name: "Faces",
                newName: "Face");

            migrationBuilder.RenameTable(
                name: "Categories",
                newName: "Category");

            migrationBuilder.RenameTable(
                name: "Captions",
                newName: "Caption");

            migrationBuilder.RenameIndex(
                name: "IX_PhotoTags_TagId",
                table: "PhotoTag",
                newName: "IX_PhotoTag_TagId");

            migrationBuilder.RenameIndex(
                name: "IX_PhotoCategories_CategoryId",
                table: "PhotoCategory",
                newName: "IX_PhotoCategory_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_ObjectProperties_PhotoId",
                table: "ObjectProperty",
                newName: "IX_ObjectProperty_PhotoId");

            migrationBuilder.RenameIndex(
                name: "IX_Faces_PhotoId",
                table: "Face",
                newName: "IX_Face_PhotoId");

            migrationBuilder.RenameIndex(
                name: "IX_Captions_PhotoId",
                table: "Caption",
                newName: "IX_Caption_PhotoId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tag",
                table: "Tag",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PhotoTag",
                table: "PhotoTag",
                columns: new[] { "PhotoId", "TagId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_PhotoCategory",
                table: "PhotoCategory",
                columns: new[] { "PhotoId", "CategoryId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ObjectProperty",
                table: "ObjectProperty",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Face",
                table: "Face",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Category",
                table: "Category",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Caption",
                table: "Caption",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Caption_Photos_PhotoId",
                table: "Caption",
                column: "PhotoId",
                principalTable: "Photos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Face_Photos_PhotoId",
                table: "Face",
                column: "PhotoId",
                principalTable: "Photos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ObjectProperty_Photos_PhotoId",
                table: "ObjectProperty",
                column: "PhotoId",
                principalTable: "Photos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhotoCategory_Category_CategoryId",
                table: "PhotoCategory",
                column: "CategoryId",
                principalTable: "Category",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PhotoCategory_Photos_PhotoId",
                table: "PhotoCategory",
                column: "PhotoId",
                principalTable: "Photos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PhotoTag_Photos_PhotoId",
                table: "PhotoTag",
                column: "PhotoId",
                principalTable: "Photos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PhotoTag_Tag_TagId",
                table: "PhotoTag",
                column: "TagId",
                principalTable: "Tag",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
