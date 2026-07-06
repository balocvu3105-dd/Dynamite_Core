using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dynamite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServerActivityLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModeratorUsername",
                table: "Warnings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TargetUsername",
                table: "Warnings",
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

            migrationBuilder.AddColumn<string>(
                name: "TargetUsername",
                table: "ModerationActions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ServerActivityLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    ActorId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ActorUsername = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ActorAvatarUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    TargetId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    TargetUsername = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerActivityLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServerActivityLogs_CreatedAt",
                table: "ServerActivityLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ServerActivityLogs_GuildId_Category_CreatedAt",
                table: "ServerActivityLogs",
                columns: new[] { "GuildId", "Category", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ServerActivityLogs_GuildId_CreatedAt",
                table: "ServerActivityLogs",
                columns: new[] { "GuildId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServerActivityLogs");

            migrationBuilder.DropColumn(
                name: "ModeratorUsername",
                table: "Warnings");

            migrationBuilder.DropColumn(
                name: "TargetUsername",
                table: "Warnings");

            migrationBuilder.DropColumn(
                name: "ModeratorUsername",
                table: "ModerationActions");

            migrationBuilder.DropColumn(
                name: "TargetUsername",
                table: "ModerationActions");
        }
    }
}
