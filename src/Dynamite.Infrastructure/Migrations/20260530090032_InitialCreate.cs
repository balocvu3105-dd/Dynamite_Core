using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dynamite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<long>(type: "bigint", nullable: false),
                    GuildName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ModerationEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    WelcomeEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LoggingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AutoRoleEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ModLogChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    ServerLogChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warnings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    TargetUserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ModeratorId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    GuildConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warnings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Warnings_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuildConfigs_GuildId",
                table: "GuildConfigs",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warnings_GuildConfigId",
                table: "Warnings",
                column: "GuildConfigId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Warnings");

            migrationBuilder.DropTable(
                name: "GuildConfigs");
        }
    }
}
