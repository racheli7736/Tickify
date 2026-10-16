using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tickify.Data.Migrations
{
    /// <inheritdoc />
    public partial class CaseInsensitiveCategoryNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Safety check: never delete or rewrite data automatically. If names that differ only by case
            // already exist, stop with a clear message so they can be resolved by hand first.
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM ""Categories"" GROUP BY lower(""Name"") HAVING count(*) > 1) THEN
        RAISE EXCEPTION 'Duplicate category names (ignoring case) exist. Rename or remove them, then re-run the migration.';
    END IF;
END $$;");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Name",
                table: "Categories");

            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX ""IX_Categories_Name_Lower"" ON ""Categories"" (lower(""Name""));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX ""IX_Categories_Name_Lower"";");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);
        }
    }
}
