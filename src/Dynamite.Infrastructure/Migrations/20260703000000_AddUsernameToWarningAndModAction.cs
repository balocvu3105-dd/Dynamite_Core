using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Dynamite.Infrastructure.Persistence;

#nullable disable

namespace Dynamite.Infrastructure.Migrations
{
    /// <summary>
    /// Adds TargetUsername and ModeratorUsername columns to Warnings and
    /// ModerationActions tables so the dashboard can show human-readable names
    /// without needing a live Discord API call for each row.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260703000000_AddUsernameToWarningAndModAction")]
    public partial class AddUsernameToWarningAndModAction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Warnings ──────────────────────────────────────────────────────
            migrationBuilder.AddColumn<string>(
                name: "TargetUsername",
                table: "Warnings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ModeratorUsername",
                table: "Warnings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // ── ModerationActions ─────────────────────────────────────────────
            migrationBuilder.AddColumn<string>(
                name: "TargetUsername",
                table: "ModerationActions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ModeratorUsername",
                table: "ModerationActions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "TargetUsername",    table: "Warnings");
            migrationBuilder.DropColumn(name: "ModeratorUsername", table: "Warnings");
            migrationBuilder.DropColumn(name: "TargetUsername",    table: "ModerationActions");
            migrationBuilder.DropColumn(name: "ModeratorUsername", table: "ModerationActions");
        }
    }
}
