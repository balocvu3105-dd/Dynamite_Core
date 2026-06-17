using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dynamite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EconomyV29_SpecialPoolExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AutoFishSpecialPoolExpiresAt",
                table: "UserFishingProfiles",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoFishSpecialPoolExpiresAt",
                table: "UserFishingProfiles");
        }
    }
}
