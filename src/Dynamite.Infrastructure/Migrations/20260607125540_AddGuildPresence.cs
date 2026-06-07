using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dynamite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGuildPresence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildPresences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<long>(type: "bigint", nullable: false),
                    GuildName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IconHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsPresent = table.Column<bool>(type: "boolean", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildPresences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuildPresences_GuildId",
                table: "GuildPresences",
                column: "GuildId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildPresences");
        }
    }
}
