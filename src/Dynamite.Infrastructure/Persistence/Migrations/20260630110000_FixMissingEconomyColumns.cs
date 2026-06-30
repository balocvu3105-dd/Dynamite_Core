// src/Dynamite.Infrastructure/Persistence/Migrations/20260630110000_FixMissingEconomyColumns.cs
// Consolidates EconomyV26-V36 + UserBlacklists migrations that were never discovered
// by EF because they lacked [DbContext] attribute. Uses IF NOT EXISTS for safety.
using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Dynamite.Infrastructure.Persistence;

#nullable disable

namespace Dynamite.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260630110000_FixMissingEconomyColumns")]
    public partial class FixMissingEconomyColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── V26: GuildConfigs — Economy channel IDs ──────────────────────
            migrationBuilder.Sql(@"
                ALTER TABLE ""GuildConfigs"" ADD COLUMN IF NOT EXISTS ""FishingLeaderboardChannelId"" numeric(20,0);
                ALTER TABLE ""GuildConfigs"" ADD COLUMN IF NOT EXISTS ""ServerLeaderboardChannelId"" numeric(20,0);
                ALTER TABLE ""GuildConfigs"" ADD COLUMN IF NOT EXISTS ""SpecialPoolChannelId"" numeric(20,0);
                ALTER TABLE ""GuildConfigs"" ADD COLUMN IF NOT EXISTS ""ShopChannelId"" numeric(20,0);
                ALTER TABLE ""GuildConfigs"" ADD COLUMN IF NOT EXISTS ""ShopShowcaseMessageId"" numeric(20,0);
                ALTER TABLE ""GuildConfigs"" ADD COLUMN IF NOT EXISTS ""InvoiceChannelId"" numeric(20,0);
                ALTER TABLE ""GuildConfigs"" ADD COLUMN IF NOT EXISTS ""WeatherChannelId"" numeric(20,0);
                ALTER TABLE ""GuildConfigs"" ADD COLUMN IF NOT EXISTS ""WeatherForecastMessageId"" numeric(20,0);
                ALTER TABLE ""GuildConfigs"" ADD COLUMN IF NOT EXISTS ""GuideChannelId"" numeric(20,0);
            ");

            // ── V27: UserFishingProfiles — AutoFishPaused ────────────────────
            migrationBuilder.Sql(@"
                ALTER TABLE ""UserFishingProfiles""
                    ADD COLUMN IF NOT EXISTS ""AutoFishPaused"" boolean NOT NULL DEFAULT false;
            ");

            // ── V28: UserFishingProfiles — AutoFishSpecialPoolId ─────────────
            migrationBuilder.Sql(@"
                ALTER TABLE ""UserFishingProfiles""
                    ADD COLUMN IF NOT EXISTS ""AutoFishSpecialPoolId"" uuid;
            ");

            // ── V29: UserFishingProfiles — AutoFishSpecialPoolExpiresAt ──────
            migrationBuilder.Sql(@"
                ALTER TABLE ""UserFishingProfiles""
                    ADD COLUMN IF NOT EXISTS ""AutoFishSpecialPoolExpiresAt"" timestamp with time zone;
            ");

            // ── V30: GuildConfigs — FishingRoleId ────────────────────────────
            migrationBuilder.Sql(@"
                ALTER TABLE ""GuildConfigs"" ADD COLUMN IF NOT EXISTS ""FishingRoleId"" numeric(20,0);
            ");

            // ── V31: UserFishingProfiles — AutoFishUseBait ───────────────────
            migrationBuilder.Sql(@"
                ALTER TABLE ""UserFishingProfiles""
                    ADD COLUMN IF NOT EXISTS ""AutoFishUseBait"" boolean NOT NULL DEFAULT false;
            ");

            // ── V32: GuildConfigs — FishingEnabled ───────────────────────────
            migrationBuilder.Sql(@"
                ALTER TABLE ""GuildConfigs""
                    ADD COLUMN IF NOT EXISTS ""FishingEnabled"" boolean NOT NULL DEFAULT false;
            ");

            // ── V33: InventoryItems.MaxDurability + UserInventories.RodDurability
            migrationBuilder.Sql(@"
                ALTER TABLE ""InventoryItems""
                    ADD COLUMN IF NOT EXISTS ""MaxDurability"" integer NOT NULL DEFAULT 0;
                ALTER TABLE ""UserInventories""
                    ADD COLUMN IF NOT EXISTS ""RodDurability"" integer NOT NULL DEFAULT 0;
            ");

            // ── V34: InventoryItems — LuckBonus ──────────────────────────────
            migrationBuilder.Sql(@"
                ALTER TABLE ""InventoryItems""
                    ADD COLUMN IF NOT EXISTS ""LuckBonus"" integer NOT NULL DEFAULT 0;
            ");

            // ── V35: UserFishingProfiles — AutoFish daily cap ─────────────────
            migrationBuilder.Sql(@"
                ALTER TABLE ""UserFishingProfiles""
                    ADD COLUMN IF NOT EXISTS ""AutoFishCastsToday"" integer NOT NULL DEFAULT 0;
                ALTER TABLE ""UserFishingProfiles""
                    ADD COLUMN IF NOT EXISTS ""AutoFishDailyResetAt"" timestamp with time zone;
            ");

            // ── V36: FishEncyclopedia table ───────────────────────────────────
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""FishEncyclopedia"" (
                    ""Id""            uuid                        NOT NULL,
                    ""GuildId""       numeric(20,0)               NOT NULL,
                    ""UserId""        numeric(20,0)               NOT NULL,
                    ""FishName""      character varying(100)      NOT NULL,
                    ""Emoji""         character varying(10)       NOT NULL,
                    ""Rarity""        character varying(20)       NOT NULL,
                    ""TimesCaught""   integer                     NOT NULL DEFAULT 1,
                    ""BestCoins""     bigint                      NOT NULL DEFAULT 0,
                    ""FirstCaughtAt"" timestamp with time zone    NOT NULL,
                    ""LastCaughtAt""  timestamp with time zone    NOT NULL,
                    ""CreatedAt""     timestamp with time zone    NOT NULL,
                    ""UpdatedAt""     timestamp with time zone,
                    CONSTRAINT ""PK_FishEncyclopedia"" PRIMARY KEY (""Id"")
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_FishEncyclopedia_GuildId_UserId_FishName""
                    ON ""FishEncyclopedia"" (""GuildId"", ""UserId"", ""FishName"");
                CREATE INDEX IF NOT EXISTS ""IX_FishEncyclopedia_GuildId_UserId""
                    ON ""FishEncyclopedia"" (""GuildId"", ""UserId"");
            ");

            // ── UserBlacklists table (from AddUserBlacklist — wrong folder) ──
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""UserBlacklists"" (
                    ""Id""                   uuid                        NOT NULL,
                    ""GuildId""              bigint                      NOT NULL,
                    ""TargetUserId""         bigint                      NOT NULL,
                    ""TargetUsername""       character varying(100)      NOT NULL,
                    ""TargetAvatarUrl""      character varying(512),
                    ""ModeratorId""          bigint                      NOT NULL,
                    ""Reason""               character varying(500)      NOT NULL,
                    ""Notes""                character varying(2000),
                    ""IsActive""             boolean                     NOT NULL DEFAULT true,
                    ""RemovedAt""            timestamp with time zone,
                    ""RemovedByModeratorId"" bigint,
                    ""RemoveReason""         character varying(500),
                    ""GuildConfigId""        uuid                        NOT NULL,
                    ""CreatedAt""            timestamp with time zone    NOT NULL,
                    ""UpdatedAt""            timestamp with time zone,
                    CONSTRAINT ""PK_UserBlacklists"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_UserBlacklists_GuildConfigs_GuildConfigId""
                        FOREIGN KEY (""GuildConfigId"") REFERENCES ""GuildConfigs"" (""Id"")
                        ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ""IX_UserBlacklists_GuildId_TargetUserId""
                    ON ""UserBlacklists"" (""GuildId"", ""TargetUserId"");
                CREATE INDEX IF NOT EXISTS ""IX_UserBlacklists_GuildId_TargetUserId_IsActive""
                    ON ""UserBlacklists"" (""GuildId"", ""TargetUserId"", ""IsActive"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse in order — only drop tables, leave columns (destructive to roll back)
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""FishEncyclopedia"";");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""UserBlacklists"";");
        }
    }
}
