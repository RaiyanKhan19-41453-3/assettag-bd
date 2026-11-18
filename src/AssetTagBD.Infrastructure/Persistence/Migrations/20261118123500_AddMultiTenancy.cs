using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetTagBD.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Introduces tenant isolation. Existing rows are backfilled into a single
    /// default tenant so upgrading a v1 database never loses data.
    /// </summary>
    public partial class AddMultiTenancy : Migration
    {
        private static readonly Guid DefaultTenantId = new("11111111-1111-1111-1111-111111111111");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Name",
                table: "Tenants",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants",
                column: "Slug",
                unique: true);

            // Create the tenant that owns all pre-existing rows.
            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[] { "Id", "Name", "Slug", "IsActive", "CreatedAt" },
                values: new object[] { DefaultTenantId, "AssetTag BD", "assettagbd-default", true, DateTime.UtcNow });

            foreach (var table in new[] { "Users", "Assets", "Assignments", "MaintenanceLogs" })
            {
                migrationBuilder.AddColumn<Guid>(
                    name: "TenantId",
                    table: table,
                    type: "uuid",
                    nullable: true);

                migrationBuilder.Sql(
                    $"UPDATE \"{table}\" SET \"TenantId\" = '{DefaultTenantId}' WHERE \"TenantId\" IS NULL;");

                migrationBuilder.AlterColumn<Guid>(
                    name: "TenantId",
                    table: table,
                    type: "uuid",
                    nullable: false,
                    defaultValue: DefaultTenantId,
                    oldClrType: typeof(Guid),
                    oldType: "uuid",
                    oldNullable: true);
            }

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId",
                table: "Users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_TenantId",
                table: "Assets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_TenantId",
                table: "Assignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceLogs_TenantId",
                table: "MaintenanceLogs",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users");

            migrationBuilder.DropTable(name: "Tenants");

            foreach (var table in new[] { "Users", "Assets", "Assignments", "MaintenanceLogs" })
            {
                migrationBuilder.DropIndex(name: $"IX_{table}_TenantId", table: table);
                migrationBuilder.DropColumn(name: "TenantId", table: table);
            }
        }
    }
}
