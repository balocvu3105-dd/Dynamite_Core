using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dynamite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EconomyV26_GuildChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Leaderboard channels
            migrationBuilder.AddColumn<decimal>(
                name: "FishingLeaderboardChannelId",
                table: "GuildConfigs",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ServerLeaderboardChannelId",
                table: "GuildConfigs",
                type: "numeric(20,0)",
                nullable: true);

            // Special Pool announcement
            migrationBuilder.AddColumn<decimal>(
                name: "SpecialPoolChannelId",
                table: "GuildConfigs",
                type: "numeric(20,0)",
                nullable: true);

            // Shop showcase
            migrationBuilder.AddColumn<decimal>(
                name: "ShopChannelId",
                table: "GuildConfigs",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ShopShowcaseMessageId",
                table: "GuildConfigs",
                type: "numeric(20,0)",
                nullable: true);

            // Invoice
            migrationBuilder.AddColumn<decimal>(
                name: "InvoiceChannelId",
                table: "GuildConfigs",
                type: "numeric(20,0)",
                nullable: true);

            // Weather forecast
            migrationBuilder.AddColumn<decimal>(
                name: "WeatherChannelId",
                table: "GuildConfigs",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeatherForecastMessageId",
                table: "GuildConfigs",
                type: "numeric(20,0)",
                nullable: true);

            // Guide / Cẩm nang
            migrationBuilder.AddColumn<decimal>(
                name: "GuideChannelId",
                table: "GuildConfigs",
                type: "numeric(20,0)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "FishingLeaderboardChannelId", table: "GuildConfigs");
            migrationBuilder.DropColumn(name: "ServerLeaderboardChannelId",  table: "GuildConfigs");
            migrationBuilder.DropColumn(name: "SpecialPoolChannelId",         table: "GuildConfigs");
            migrationBuilder.DropColumn(name: "ShopChannelId",                table: "GuildConfigs");
            migrationBuilder.DropColumn(name: "ShopShowcaseMessageId",        table: "GuildConfigs");
            migrationBuilder.DropColumn(name: "InvoiceChannelId",             table: "GuildConfigs");
            migrationBuilder.DropColumn(name: "WeatherChannelId",             table: "GuildConfigs");
            migrationBuilder.DropColumn(name: "WeatherForecastMessageId",     table: "GuildConfigs");
            migrationBuilder.DropColumn(name: "GuideChannelId",               table: "GuildConfigs");
        }
    }
}
