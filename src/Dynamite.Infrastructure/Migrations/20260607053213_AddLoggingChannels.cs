using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dynamite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLoggingChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "MemberLogChannelId",
                table: "GuildConfigs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MessageLogChannelId",
                table: "GuildConfigs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VoiceLogChannelId",
                table: "GuildConfigs",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MemberLogChannelId",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "MessageLogChannelId",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "VoiceLogChannelId",
                table: "GuildConfigs");
        }
    }
}
