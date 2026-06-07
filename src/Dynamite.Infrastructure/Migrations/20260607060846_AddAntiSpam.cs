using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dynamite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAntiSpam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AntiSpamConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<long>(type: "bigint", nullable: false),
                    GuildConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    MessageThreshold = table.Column<int>(type: "integer", nullable: false),
                    MessageWindowSeconds = table.Column<int>(type: "integer", nullable: false),
                    MentionThreshold = table.Column<int>(type: "integer", nullable: false),
                    AntiInvite = table.Column<bool>(type: "boolean", nullable: false),
                    AntiScamLink = table.Column<bool>(type: "boolean", nullable: false),
                    AntiRaid = table.Column<bool>(type: "boolean", nullable: false),
                    RaidThreshold = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AntiSpamConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AntiSpamConfigs_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AntiSpamConfigs_GuildConfigId",
                table: "AntiSpamConfigs",
                column: "GuildConfigId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AntiSpamConfigs_GuildId",
                table: "AntiSpamConfigs",
                column: "GuildId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AntiSpamConfigs");
        }
    }
}
