using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomIdFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_CustomId",
                table: "Items");

            migrationBuilder.AddColumn<int>(
                name: "SequenceNumber",
                table: "Items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomIdFormat",
                table: "Inventories",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateIndex(
                name: "IX_Items_InventoryId_CustomId",
                table: "Items",
                columns: new[] { "InventoryId", "CustomId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_InventoryId_SequenceNumber",
                table: "Items",
                columns: new[] { "InventoryId", "SequenceNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_InventoryId_CustomId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_InventoryId_SequenceNumber",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "SequenceNumber",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "CustomIdFormat",
                table: "Inventories");

            migrationBuilder.CreateIndex(
                name: "IX_Items_CustomId",
                table: "Items",
                column: "CustomId");
        }
    }
}
