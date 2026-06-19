// src/Dynamite.Infrastructure/Persistence/Migrations/20260619120000_EconomyV36_FishEncyclopedia.cs
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dynamite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EconomyV36_FishEncyclopedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FishEncyclopedia",
                columns: table => new
                {
                    Id            = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId       = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UserId        = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    FishName      = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Emoji         = table.Column<string>(type: "character varying(10)",  maxLength: 10,  nullable: false),
                    Rarity        = table.Column<string>(type: "character varying(20)",  maxLength: 20,  nullable: false),
                    TimesCaught   = table.Column<int>    (type: "integer",               nullable: false, defaultValue: 1),
                    BestCoins     = table.Column<long>   (type: "bigint",                nullable: false, defaultValue: 0L),
                    FirstCaughtAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastCaughtAt  = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt     = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt     = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FishEncyclopedia", x => x.Id);
                });

            // Unique index — một user chỉ có 1 entry per fish per guild
            migrationBuilder.CreateIndex(
                name: "IX_FishEncyclopedia_GuildId_UserId_FishName",
                table: "FishEncyclopedia",
                columns: ["GuildId", "UserId", "FishName"],
                unique: true);

            // Index riêng để query nhanh theo (GuildId, UserId)
            migrationBuilder.CreateIndex(
                name: "IX_FishEncyclopedia_GuildId_UserId",
                table: "FishEncyclopedia",
                columns: ["GuildId", "UserId"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "FishEncyclopedia");
        }
    }
}
