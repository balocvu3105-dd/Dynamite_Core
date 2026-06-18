using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dynamite.Infrastructure.Persistence.Migrations
{
    [Migration("20260618140000_EconomyV33_RodDurability")]
    public partial class EconomyV33_RodDurability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // InventoryItem — MaxDurability (định nghĩa rod có bao nhiêu durability khi mới)
            migrationBuilder.AddColumn<int>(
                name: "MaxDurability",
                table: "InventoryItems",
                type: "integer",
                nullable: true);

            // UserInventory — RodDurability (durability hiện tại của rod user đang sở hữu)
            // null = legacy item / non-rod (không track)
            // 0    = gãy, cần repair
            migrationBuilder.AddColumn<int>(
                name: "RodDurability",
                table: "UserInventories",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxDurability",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "RodDurability",
                table: "UserInventories");
        }
    }
}
