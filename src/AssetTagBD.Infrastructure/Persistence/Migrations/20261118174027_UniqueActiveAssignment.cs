using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetTagBD.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UniqueActiveAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Assignments_AssetId",
                table: "Assignments");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_AssetId_Active",
                table: "Assignments",
                column: "AssetId",
                unique: true,
                filter: "\"ReturnedDate\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Assignments_AssetId_Active",
                table: "Assignments");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_AssetId",
                table: "Assignments",
                column: "AssetId");
        }
    }
}
