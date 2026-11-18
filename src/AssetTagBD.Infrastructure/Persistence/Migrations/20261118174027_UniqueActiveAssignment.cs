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
            // If a legacy database already has duplicate active assignments,
            // keep the newest one and close the rest so the index can be built.
            migrationBuilder.Sql(@"
                UPDATE ""Assignments"" SET ""ReturnedDate"" = ""AssignedDate""
                WHERE ""ReturnedDate"" IS NULL AND ""Id"" NOT IN (
                    SELECT DISTINCT ON (""AssetId"") ""Id""
                    FROM ""Assignments""
                    WHERE ""ReturnedDate"" IS NULL
                    ORDER BY ""AssetId"", ""AssignedDate"" DESC, ""Id"" DESC
                );");

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
