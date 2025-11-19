using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoBank.DbContext.Migrations
{
    /// <inheritdoc />
    public partial class AddFaceEmbedding_20251115000001_AddFaceEmbeddingHNSWIndexcs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create HNSW index for fast vector similarity search using cosine distance
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_faces_embedding_hnsw
                ON ""Faces""
                USING hnsw (""Embedding"" vector_cosine_ops)
                WITH (m = 16, ef_construction = 64);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_faces_embedding_hnsw;");
        }
    }
}
