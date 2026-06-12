using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dynamite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FeatureBatch_Verify_MaxRoles_GiveawayV2_AdminTools_WelcomeStyle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxRoles",
                table: "RolePanels",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "VerifyRemoveRoleId",
                table: "GuildConfigs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WelcomeEmbedColor",
                table: "GuildConfigs",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WelcomeEmbedFooter",
                table: "GuildConfigs",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WelcomeEmbedTitle",
                table: "GuildConfigs",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WelcomeImageEnabled",
                table: "GuildConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaimMessage",
                table: "Giveaways",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinJoinDays",
                table: "Giveaways",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "PingRoleId",
                table: "Giveaways",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxRoles",
                table: "RolePanels");

            migrationBuilder.DropColumn(
                name: "VerifyRemoveRoleId",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "WelcomeEmbedColor",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "WelcomeEmbedFooter",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "WelcomeEmbedTitle",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "WelcomeImageEnabled",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "ClaimMessage",
                table: "Giveaways");

            migrationBuilder.DropColumn(
                name: "MinJoinDays",
                table: "Giveaways");

            migrationBuilder.DropColumn(
                name: "PingRoleId",
                table: "Giveaways");
        }
    }
}
