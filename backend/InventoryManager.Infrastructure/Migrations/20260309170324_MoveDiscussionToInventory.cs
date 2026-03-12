using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveDiscussionToInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ItemId",
                table: "Discussions",
                newName: "InventoryId");

            migrationBuilder.RenameIndex(
                name: "IX_Discussions_ItemId",
                table: "Discussions",
                newName: "IX_Discussions_InventoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InventoryId",
                table: "Discussions",
                newName: "ItemId");

            migrationBuilder.RenameIndex(
                name: "IX_Discussions_InventoryId",
                table: "Discussions",
                newName: "IX_Discussions_ItemId");
        }
    }
}
