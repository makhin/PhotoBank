using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoBank.DbContext.Migrations
{
    /// <inheritdoc />
    public partial class BlobToS3_Metadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Photos_Id",
                table: "Photos");

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
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "S3ETag_Thumbnail",
                table: "Photos",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "S3Key_Preview",
                table: "Photos",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "S3Key_Thumbnail",
                table: "Photos",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sha256_Preview",
                table: "Photos",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sha256_Thumbnail",
                table: "Photos",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

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
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "S3Key_Image",
                table: "Faces",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sha256_Image",
                table: "Faces",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Photos_NeedsMigration",
                table: "Photos",
                column: "Id",
                filter: "[S3Key_Preview] IS NULL OR [S3Key_Thumbnail] IS NULL")
                .Annotation("SqlServer:Include", new[] { "S3Key_Preview", "S3Key_Thumbnail" });

            migrationBuilder.CreateIndex(
                name: "IX_Faces_NeedsMigration",
                table: "Faces",
                column: "Id",
                filter: "[S3Key_Image] IS NULL")
                .Annotation("SqlServer:Include", new[] { "S3Key_Image" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Photos_NeedsMigration",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Faces_NeedsMigration",
                table: "Faces");

            migrationBuilder.DropColumn(
                name: "BlobSize_Preview",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "BlobSize_Thumbnail",
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

            migrationBuilder.CreateIndex(
                name: "IX_Photos_Id",
                table: "Photos",
                column: "Id");
        }
    }
}
