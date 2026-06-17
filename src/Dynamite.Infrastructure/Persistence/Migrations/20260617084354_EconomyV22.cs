using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dynamite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EconomyV22 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "InventoryItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EscapeRate",
                table: "InventoryItems",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MissRate",
                table: "InventoryItems",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsageCount",
                table: "InventoryItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DailyChannelId",
                table: "GuildConfigs",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FishingChannelId",
                table: "GuildConfigs",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FishingActivityLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Event = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FishName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Rarity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CoinsEarned = table.Column<long>(type: "bigint", nullable: false),
                    XpEarned = table.Column<int>(type: "integer", nullable: false),
                    PoolName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RodName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Weather = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    PondRemaining = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FishingActivityLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FishingDataSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Reason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FishingXp = table.Column<long>(type: "bigint", nullable: false),
                    FishingLevel = table.Column<int>(type: "integer", nullable: false),
                    TotalCaught = table.Column<int>(type: "integer", nullable: false),
                    CommonCaught = table.Column<int>(type: "integer", nullable: false),
                    UncommonCaught = table.Column<int>(type: "integer", nullable: false),
                    RareCaught = table.Column<int>(type: "integer", nullable: false),
                    LegendaryCaught = table.Column<int>(type: "integer", nullable: false),
                    MythicCaught = table.Column<int>(type: "integer", nullable: false),
                    ChestsOpened = table.Column<int>(type: "integer", nullable: false),
                    WalletCoins = table.Column<long>(type: "bigint", nullable: false),
                    BagSnapshotJson = table.Column<string>(type: "text", nullable: false),
                    BagCapacity = table.Column<int>(type: "integer", nullable: false),
                    AchievementIds = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FishingDataSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildLevelRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    LevelType = table.Column<string>(type: "text", nullable: false),
                    RequiredLevel = table.Column<int>(type: "integer", nullable: false),
                    RoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildLevelRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildPearlLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    PearlType = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildPearlLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildPonds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    CurrentFish = table.Column<int>(type: "integer", nullable: false),
                    MaxFish = table.Column<int>(type: "integer", nullable: false),
                    DepletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResetAvailableAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CurrentWeather = table.Column<string>(type: "text", nullable: false),
                    WeatherExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DailyChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    FishingChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildPonds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeaderboardSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    WeekStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaderboardSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpecialPools",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    PoolName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DropTable = table.Column<string>(type: "text", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    RemainingFish = table.Column<int>(type: "integer", nullable: false),
                    MinLevel = table.Column<int>(type: "integer", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialPools", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserFishBags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    BagCapacity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFishBags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserFishingProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    FishingXp = table.Column<long>(type: "bigint", nullable: false),
                    FishingLevel = table.Column<int>(type: "integer", nullable: false),
                    LastFishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalCaught = table.Column<int>(type: "integer", nullable: false),
                    CommonCaught = table.Column<int>(type: "integer", nullable: false),
                    UncommonCaught = table.Column<int>(type: "integer", nullable: false),
                    RareCaught = table.Column<int>(type: "integer", nullable: false),
                    LegendaryCaught = table.Column<int>(type: "integer", nullable: false),
                    MythicCaught = table.Column<int>(type: "integer", nullable: false),
                    ChestsOpened = table.Column<int>(type: "integer", nullable: false),
                    AutoFishExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TradesThisWeek = table.Column<int>(type: "integer", nullable: false),
                    TradeWeekResetAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFishingProfiles", x => x.Id);
                    table.UniqueConstraint("AK_UserFishingProfiles_GuildId_UserId", x => new { x.GuildId, x.UserId });
                });

            migrationBuilder.CreateTable(
                name: "UserServerProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ServerXp = table.Column<long>(type: "bigint", nullable: false),
                    ServerLevel = table.Column<int>(type: "integer", nullable: false),
                    LastMessageXpAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoiceJoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalVoiceMinutes = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserServerProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    WeeklyFishCaught = table.Column<int>(type: "integer", nullable: false),
                    WeeklyMessages = table.Column<int>(type: "integer", nullable: false),
                    WeeklyVoiceMinutes = table.Column<int>(type: "integer", nullable: false),
                    WeekResetAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeaderboardEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<long>(type: "bigint", nullable: false),
                    DeltaRank = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaderboardEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaderboardEntries_LeaderboardSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "LeaderboardSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaughtFish",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BagId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    FishName = table.Column<string>(type: "text", nullable: false),
                    FishEmoji = table.Column<string>(type: "text", nullable: false),
                    Rarity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CoinValue = table.Column<long>(type: "bigint", nullable: false),
                    SourcePool = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsSpecialCreature = table.Column<bool>(type: "boolean", nullable: false),
                    IsPearl = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaughtFish", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaughtFish_UserFishBags_BagId",
                        column: x => x.BagId,
                        principalTable: "UserFishBags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserFishingAchievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    AchievementId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFishingAchievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFishingAchievements_UserFishingProfiles_GuildId_UserId",
                        columns: x => new { x.GuildId, x.UserId },
                        principalTable: "UserFishingProfiles",
                        principalColumns: new[] { "GuildId", "UserId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaughtFish_BagId",
                table: "CaughtFish",
                column: "BagId");

            migrationBuilder.CreateIndex(
                name: "IX_CaughtFish_GuildId_UserId",
                table: "CaughtFish",
                columns: new[] { "GuildId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_FishingActivityLogs_CreatedAt",
                table: "FishingActivityLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FishingActivityLogs_GuildId_CreatedAt",
                table: "FishingActivityLogs",
                columns: new[] { "GuildId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FishingActivityLogs_GuildId_UserId_CreatedAt",
                table: "FishingActivityLogs",
                columns: new[] { "GuildId", "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FishingDataSnapshots_GuildId_UserId_CreatedAt",
                table: "FishingDataSnapshots",
                columns: new[] { "GuildId", "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GuildLevelRoles_GuildId_LevelType_RequiredLevel",
                table: "GuildLevelRoles",
                columns: new[] { "GuildId", "LevelType", "RequiredLevel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildPearlLogs_GuildId_PearlType_CreatedAt",
                table: "GuildPearlLogs",
                columns: new[] { "GuildId", "PearlType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GuildPonds_GuildId",
                table: "GuildPonds",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardEntries_SnapshotId_Rank",
                table: "LeaderboardEntries",
                columns: new[] { "SnapshotId", "Rank" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardSnapshots_GuildId_Type_WeekStartDate",
                table: "LeaderboardSnapshots",
                columns: new[] { "GuildId", "Type", "WeekStartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SpecialPools_GuildId_StartsAt_ExpiresAt",
                table: "SpecialPools",
                columns: new[] { "GuildId", "StartsAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserFishBags_GuildId_UserId",
                table: "UserFishBags",
                columns: new[] { "GuildId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserFishingAchievements_GuildId_UserId_AchievementId",
                table: "UserFishingAchievements",
                columns: new[] { "GuildId", "UserId", "AchievementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserFishingProfiles_GuildId_UserId",
                table: "UserFishingProfiles",
                columns: new[] { "GuildId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserServerProfiles_GuildId_UserId",
                table: "UserServerProfiles",
                columns: new[] { "GuildId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyActivities_GuildId_UserId",
                table: "WeeklyActivities",
                columns: new[] { "GuildId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaughtFish");

            migrationBuilder.DropTable(
                name: "FishingActivityLogs");

            migrationBuilder.DropTable(
                name: "FishingDataSnapshots");

            migrationBuilder.DropTable(
                name: "GuildLevelRoles");

            migrationBuilder.DropTable(
                name: "GuildPearlLogs");

            migrationBuilder.DropTable(
                name: "GuildPonds");

            migrationBuilder.DropTable(
                name: "LeaderboardEntries");

            migrationBuilder.DropTable(
                name: "SpecialPools");

            migrationBuilder.DropTable(
                name: "UserFishingAchievements");

            migrationBuilder.DropTable(
                name: "UserServerProfiles");

            migrationBuilder.DropTable(
                name: "WeeklyActivities");

            migrationBuilder.DropTable(
                name: "UserFishBags");

            migrationBuilder.DropTable(
                name: "LeaderboardSnapshots");

            migrationBuilder.DropTable(
                name: "UserFishingProfiles");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "EscapeRate",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "MissRate",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "UsageCount",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "DailyChannelId",
                table: "GuildConfigs");

            migrationBuilder.DropColumn(
                name: "FishingChannelId",
                table: "GuildConfigs");
        }
    }
}
