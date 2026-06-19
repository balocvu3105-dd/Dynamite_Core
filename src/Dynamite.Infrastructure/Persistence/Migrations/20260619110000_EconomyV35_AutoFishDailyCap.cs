using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dynamite.Infrastructure.Persistence.Migrations
{
    [Migration("20260619110000_EconomyV35_AutoFishDailyCap")]
    public partial class EconomyV35_AutoFishDailyCap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // UserFishingProfiles — daily cast cap cho auto-fish.
            // AutoFishCastsToday: số lần auto-fish đã cast trong ngày UTC hiện tại.
            // AutoFishDailyResetAt: ngày UTC cuối cùng counter được reset.
            // Manual fishing KHÔNG tính vào đây.
            migrationBuilder.AddColumn<int>(
                name: "AutoFishCastsToday",
                table: "UserFishingProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "AutoFishDailyResetAt",
                table: "UserFishingProfiles",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoFishCastsToday",
                table: "UserFishingProfiles");

            migrationBuilder.DropColumn(
                name: "AutoFishDailyResetAt",
                table: "UserFishingProfiles");
        }
    }
}
