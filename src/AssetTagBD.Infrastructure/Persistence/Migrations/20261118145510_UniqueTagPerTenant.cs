using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetTagBD.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UniqueTagPerTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Assets_AssetTag",
                table: "Assets");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_TenantId_AssetTag",
                table: "Assets",
                columns: new[] { "TenantId", "AssetTag" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only drop the per-tenant index. Recreating global uniqueness would
            // fail if two tenants legitimately share a tag.
            migrationBuilder.DropIndex(
                name: "IX_Assets_TenantId_AssetTag",
                table: "Assets");
        }
    }
}
