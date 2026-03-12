using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Inventories""
                ADD COLUMN ""SearchVector"" tsvector
                GENERATED ALWAYS AS (
                    to_tsvector(
                        'simple',
                        coalesce(""Title"", '') || ' ' || coalesce(""Description"", '')
                    )
                ) STORED;
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Inventories_SearchVector""
                ON ""Inventories""
                USING GIN (""SearchVector"");
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Items""
                ADD COLUMN ""SearchVector"" tsvector
                GENERATED ALWAYS AS (
                    to_tsvector(
                        'simple',
                        coalesce(""CustomId"", '') || ' ' || coalesce(""Name"", '')
                    )
                ) STORED;
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Items_SearchVector""
                ON ""Items""
                USING GIN (""SearchVector"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Items_SearchVector"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" DROP COLUMN IF EXISTS ""SearchVector"";");

            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Inventories_SearchVector"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Inventories"" DROP COLUMN IF EXISTS ""SearchVector"";");
        }
    }
}
