using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dynamite.Infrastructure.Persistence.Migrations
{
    [Migration("20260617130000_EconomyV28_AutoFishSpecialPool")]
    public partial class EconomyV28_AutoFishSpecialPool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AutoFishSpecialPoolId",
                table: "UserFishingProfiles",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoFishSpecialPoolId",
                table: "UserFishingProfiles");
        }
    }
}
