using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dynamite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEconomyV2Features : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // GuildPonds — per-pond miss/escape override (dùng bởi SpecialPool)
            // V30-V34 đã cover các columns khác; V35 cover AutoFishDailyCap; V36 cover FishEncyclopedia.
            migrationBuilder.AddColumn<double>(
                name: "FishEscapeRateOverride",
                table: "GuildPonds",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FishMissRateOverride",
                table: "GuildPonds",
                type: "double precision",
                nullable: true);

            // RefreshTokens — nới lỏng max-length (varchar → text)
            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "RefreshTokens",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Avatar",
                table: "RefreshTokens",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FishEscapeRateOverride",
                table: "GuildPonds");

            migrationBuilder.DropColumn(
                name: "FishMissRateOverride",
                table: "GuildPonds");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "RefreshTokens",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Avatar",
                table: "RefreshTokens",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
