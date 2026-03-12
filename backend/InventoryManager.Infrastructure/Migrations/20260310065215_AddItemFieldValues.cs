using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddItemFieldValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StringValue = table.Column<string>(type: "text", nullable: true),
                    TextValue = table.Column<string>(type: "text", nullable: true),
                    NumberValue = table.Column<double>(type: "double precision", nullable: true),
                    LinkValue = table.Column<string>(type: "text", nullable: true),
                    BooleanValue = table.Column<bool>(type: "boolean", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemFieldValues", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemFieldValues_ItemId",
                table: "ItemFieldValues",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemFieldValues_ItemId_FieldDefinitionId",
                table: "ItemFieldValues",
                columns: new[] { "ItemId", "FieldDefinitionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemFieldValues");
        }
    }
}
