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
            // Idempotent: IF NOT EXISTS guards prevent failure when columns were
            // already added by earlier manual migrations (EconomyV22–V29).

            migrationBuilder.Sql(@"
                ALTER TABLE ""GuildPonds""
                    ADD COLUMN IF NOT EXISTS ""FishEscapeRateOverride"" double precision;
                ALTER TABLE ""GuildPonds""
                    ADD COLUMN IF NOT EXISTS ""FishMissRateOverride"" double precision;
            ");

            // RefreshTokens AlterColumn skipped — columns already have correct types in DB.
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
