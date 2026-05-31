using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dynamite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AutoRoleConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<long>(type: "bigint", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    GuildConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoRoleConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutoRoleConfigs_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePanels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<long>(type: "bigint", nullable: false),
                    ChannelId = table.Column<long>(type: "bigint", nullable: false),
                    MessageId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    PanelType = table.Column<int>(type: "integer", nullable: false),
                    GuildConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePanels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePanels_GuildConfigs_GuildConfigId",
                        column: x => x.GuildConfigId,
                        principalTable: "GuildConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePanelItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    Label = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Emoji = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RolePanelId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePanelItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePanelItems_RolePanels_RolePanelId",
                        column: x => x.RolePanelId,
                        principalTable: "RolePanels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutoRoleConfigs_GuildConfigId",
                table: "AutoRoleConfigs",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoRoleConfigs_GuildId_RoleId",
                table: "AutoRoleConfigs",
                columns: new[] { "GuildId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePanelItems_RolePanelId_RoleId",
                table: "RolePanelItems",
                columns: new[] { "RolePanelId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePanels_GuildConfigId",
                table: "RolePanels",
                column: "GuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePanels_GuildId_MessageId",
                table: "RolePanels",
                columns: new[] { "GuildId", "MessageId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutoRoleConfigs");

            migrationBuilder.DropTable(
                name: "RolePanelItems");

            migrationBuilder.DropTable(
                name: "RolePanels");
        }
    }
}
