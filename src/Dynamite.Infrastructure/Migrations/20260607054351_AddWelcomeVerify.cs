using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dynamite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWelcomeVerify : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "VerifyChannelId",
                table: "GuildConfigs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VerifyRoleId",
                table: "GuildConfigs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "WelcomeChannelId",
                table: "GuildConfigs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WelcomeMessage",
                table: "GuildConfigs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerifyChannelId",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "VerifyRoleId",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "WelcomeChannelId",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "WelcomeMessage",
                table: "GuildConfigs");
        }
    }
}
