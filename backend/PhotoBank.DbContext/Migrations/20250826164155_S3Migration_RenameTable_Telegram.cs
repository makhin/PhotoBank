using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace PhotoBank.DbContext.Migrations
{
    /// <inheritdoc />
    public partial class S3Migration_RenameTable_Telegram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ObjectProperties_PropertyNames_PropertyNameId",
                table: "ObjectProperties");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonPersonGroup_PersonGroups_PersonGroupsId",
                table: "PersonPersonGroup");

            migrationBuilder.DropIndex(
                name: "IX_PhotoTags_TagId",
                table: "PhotoTags");

            migrationBuilder.DropIndex(
                name: "IX_Photos_Id",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "Telegram",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Tags",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Hint",
                table: "Tags",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Storages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Folder",
                table: "Storages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "Thumbnail",
                table: "Photos",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RelativePath",
                table: "Photos",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "BlobSize_Preview",
                table: "Photos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "BlobSize_Thumbnail",
                table: "Photos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageHash",
                table: "Photos",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "MigratedAt_Preview",
                table: "Photos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MigratedAt_Thumbnail",
                table: "Photos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "S3ETag_Preview",
                table: "Photos",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "S3ETag_Thumbnail",
                table: "Photos",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "S3Key_Preview",
                table: "Photos",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "S3Key_Thumbnail",
                table: "Photos",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Sha256_Preview",
                table: "Photos",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Sha256_Thumbnail",
                table: "Photos",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Persons",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "Persons",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PropertyNameId",
                table: "ObjectProperties",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FaceAttributes",
                table: "Faces",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "BlobSize_Image",
                table: "Faces",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MigratedAt_Image",
                table: "Faces",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "S3ETag_Image",
                table: "Faces",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "S3Key_Image",
                table: "Faces",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Sha256_Image",
                table: "Faces",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "Captions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "TelegramSendTimeUtc",
                table: "AspNetUsers",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TelegramUserId",
                table: "AspNetUsers",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PersonFace",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    FaceId = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                name: "IX_Tag_Name",
                table: "Tags",
                column: "Name")
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.CreateIndex(
                name: "IX_PhotoTags_TagId_PhotoId",
                table: "PhotoTags",
                columns: new[] { "TagId", "PhotoId" });

            migrationBuilder.CreateIndex(
                name: "IX_Photos_NeedsMigration_Thumbnail",
                table: "Photos",
                column: "Id",
                filter: "[S3Key_Thumbnail] IS NULL")
                .Annotation("SqlServer:Include", new[] { "S3Key_Preview", "S3Key_Thumbnail" });

            migrationBuilder.CreateIndex(
                name: "IX_Photos_StorageId_TakenDate",
                table: "Photos",
                columns: new[] { "StorageId", "TakenDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Faces_NeedsMigration",
                table: "Faces",
                column: "Id",
                filter: "[S3Key_Image] IS NULL")
                .Annotation("SqlServer:Include", new[] { "S3Key_Image" });

            migrationBuilder.CreateIndex(
                name: "IX_Faces_PersonId_PhotoId",
                table: "Faces",
                columns: new[] { "PersonId", "PhotoId" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_TelegramUserId",
                table: "AspNetUsers",
                column: "TelegramUserId",
                unique: true,
                filter: "[TelegramUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PersonFace_FaceId",
                table: "PersonFace",
                column: "FaceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonFace_PersonId",
                table: "PersonFace",
                column: "PersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_ObjectProperties_PropertyNames_PropertyNameId",
                table: "ObjectProperties",
                column: "PropertyNameId",
                principalTable: "PropertyNames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonPersonGroup_PersonGroups_PersonGroupsId",
                table: "PersonPersonGroup",
                column: "PersonGroupsId",
                principalTable: "PersonGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ObjectProperties_PropertyNames_PropertyNameId",
                table: "ObjectProperties");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonPersonGroup_PersonGroups_PersonGroupsId",
                table: "PersonPersonGroup");

            migrationBuilder.DropTable(
                name: "PersonFace");

            migrationBuilder.DropIndex(
                name: "IX_Tag_Name",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_PhotoTags_TagId_PhotoId",
                table: "PhotoTags");

            migrationBuilder.DropIndex(
                name: "IX_Photos_NeedsMigration_Thumbnail",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_StorageId_TakenDate",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Faces_NeedsMigration",
                table: "Faces");

            migrationBuilder.DropIndex(
                name: "IX_Faces_PersonId_PhotoId",
                table: "Faces");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_TelegramUserId",
                table: "AspNetUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PersonGroups",
                table: "PersonGroups");

            migrationBuilder.DropColumn(
                name: "BlobSize_Preview",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "BlobSize_Thumbnail",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "ImageHash",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "MigratedAt_Preview",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "MigratedAt_Thumbnail",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "S3ETag_Preview",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "S3ETag_Thumbnail",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "S3Key_Preview",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "S3Key_Thumbnail",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "Sha256_Preview",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "Sha256_Thumbnail",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "BlobSize_Image",
                table: "Faces");

            migrationBuilder.DropColumn(
                name: "MigratedAt_Image",
                table: "Faces");

            migrationBuilder.DropColumn(
                name: "S3ETag_Image",
                table: "Faces");

            migrationBuilder.DropColumn(
                name: "S3Key_Image",
                table: "Faces");

            migrationBuilder.DropColumn(
                name: "Sha256_Image",
                table: "Faces");

            migrationBuilder.DropColumn(
                name: "TelegramSendTimeUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TelegramUserId",
                table: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "PersonGroups",
                newName: "PersonGroup");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Tags",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Hint",
                table: "Tags",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Storages",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Folder",
                table: "Storages",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<byte[]>(
                name: "Thumbnail",
                table: "Photos",
                type: "varbinary(max)",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)");

            migrationBuilder.AlterColumn<string>(
                name: "RelativePath",
                table: "Photos",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<byte[]>(
                name: "PreviewImage",
                table: "Photos",
                type: "varbinary(max)",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)");

            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "Photos",
                type: "geometry",
                nullable: true,
                oldClrType: typeof(Point),
                oldType: "geometry");

            migrationBuilder.AlterColumn<string>(
                name: "DominantColors",
                table: "Photos",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "DominantColorForeground",
                table: "Photos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "DominantColorBackground",
                table: "Photos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "AccentColor",
                table: "Photos",
                type: "nvarchar(6)",
                maxLength: 6,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(6)",
                oldMaxLength: 6);

            migrationBuilder.AlterColumn<Geometry>(
                name: "Rectangle",
                table: "ObjectProperties",
                type: "geometry",
                nullable: true,
                oldClrType: typeof(Geometry),
                oldType: "geometry");

            migrationBuilder.AlterColumn<int>(
                name: "PropertyNameId",
                table: "ObjectProperties",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<Geometry>(
                name: "Rectangle",
                table: "Faces",
                type: "geometry",
                nullable: true,
                oldClrType: typeof(Geometry),
                oldType: "geometry");

            migrationBuilder.AlterColumn<byte[]>(
                name: "Image",
                table: "Faces",
                type: "varbinary(max)",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FaceAttributes",
                table: "Faces",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "Captions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Telegram",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PersonGroup",
                table: "PersonGroup",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoTags_TagId",
                table: "PhotoTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_Id",
                table: "Photos",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ObjectProperties_PropertyNames_PropertyNameId",
                table: "ObjectProperties",
                column: "PropertyNameId",
                principalTable: "PropertyNames",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonPersonGroup_PersonGroup_PersonGroupsId",
                table: "PersonPersonGroup",
                column: "PersonGroupsId",
                principalTable: "PersonGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
