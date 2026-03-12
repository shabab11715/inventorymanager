using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryAccessControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Inventories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "Inventories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "InventoryWriteAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryWriteAccesses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryWriteAccesses_InventoryId",
                table: "InventoryWriteAccesses",
                column: "InventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryWriteAccesses_InventoryId_UserId",
                table: "InventoryWriteAccesses",
                columns: new[] { "InventoryId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryWriteAccesses_UserId",
                table: "InventoryWriteAccesses",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryWriteAccesses");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Inventories");
        }
    }
}
