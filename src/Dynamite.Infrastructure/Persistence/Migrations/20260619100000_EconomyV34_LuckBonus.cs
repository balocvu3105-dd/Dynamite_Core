using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dynamite.Infrastructure.Persistence.Migrations
{
    [Migration("20260619100000_EconomyV34_LuckBonus")]
    public partial class EconomyV34_LuckBonus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // InventoryItem — LuckBonus: điểm may mắn cho FishingRod cao cấp.
            // null / 0 = không có hiệu ứng.
            // Cần Câu Kim Cương: 1 → rareMod +0.3, legendaryMod +0.5 khi roll.
            migrationBuilder.AddColumn<int>(
                name: "LuckBonus",
                table: "InventoryItems",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LuckBonus",
                table: "InventoryItems");
        }
    }
}
