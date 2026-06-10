using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dynamite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGiveawayTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Giveaways",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<long>(type: "bigint", nullable: false),
                    ChannelId = table.Column<long>(type: "bigint", nullable: false),
                    MessageId = table.Column<long>(type: "bigint", nullable: false),
                    HostId = table.Column<long>(type: "bigint", nullable: false),
                    Prize = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    WinnerCount = table.Column<int>(type: "integer", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsEnded = table.Column<bool>(type: "boolean", nullable: false),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    WinnerIds = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Giveaways", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GiveawayEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GiveawayId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    EnteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiveawayEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiveawayEntries_Giveaways_GiveawayId",
                        column: x => x.GiveawayId,
                        principalTable: "Giveaways",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GiveawayEntries_GiveawayId_UserId",
                table: "GiveawayEntries",
                columns: new[] { "GiveawayId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Giveaways_GuildId_IsEnded",
                table: "Giveaways",
                columns: new[] { "GuildId", "IsEnded" });

            migrationBuilder.CreateIndex(
                name: "IX_Giveaways_MessageId",
                table: "Giveaways",
                column: "MessageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GiveawayEntries");

            migrationBuilder.DropTable(
                name: "Giveaways");
        }
    }
}
