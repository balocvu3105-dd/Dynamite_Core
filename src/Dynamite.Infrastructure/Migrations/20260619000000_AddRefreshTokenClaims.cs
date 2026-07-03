using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Dynamite.Infrastructure.Persistence;

#nullable disable

namespace Dynamite.Infrastructure.Migrations
{
    /// <summary>
    /// Adds Username and Avatar columns to RefreshTokens so the refresh
    /// endpoint can rebuild access token claims without calling Discord API.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260619000000_AddRefreshTokenClaims")]
    public partial class AddRefreshTokenClaims : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "RefreshTokens",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Avatar",
                table: "RefreshTokens",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Username", table: "RefreshTokens");
            migrationBuilder.DropColumn(name: "Avatar",   table: "RefreshTokens");
        }
    }
}
