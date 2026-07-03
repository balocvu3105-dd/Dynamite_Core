using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Dynamite.Infrastructure.Persistence;

#nullable disable

namespace Dynamite.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260630000000_AddUserBlacklist")]
    public partial class AddUserBlacklist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserBlacklists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<long>(type: "bigint", nullable: false),
                    TargetUserId = table.Column<long>(type: "bigint", nullable: false),
                    TargetUsername = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetAvatarUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ModeratorId = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RemovedByModeratorId = table.Column<long>(type: "bigint", nullable: true),
                    RemoveReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    GuildConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBlacklists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBlacklists_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserBlacklists_GuildId_TargetUserId",
                table: "UserBlacklists",
                columns: new[] { "GuildId", "TargetUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserBlacklists_GuildId_TargetUserId_IsActive",
                table: "UserBlacklists",
                columns: new[] { "GuildId", "TargetUserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserBlacklists");
        }
    }
}
