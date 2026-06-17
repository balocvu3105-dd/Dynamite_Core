CREATE TABLE IF NOT EXISTS "TempVoiceConfigs" (
    "Id" uuid NOT NULL,
    "GuildId" bigint NOT NULL,
    "TriggerChannelId" bigint NOT NULL,
    "CategoryId" bigint NULL,
    "DefaultUserLimit" integer NOT NULL DEFAULT 0,
    "GuildConfigId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NULL,
    CONSTRAINT "PK_TempVoiceConfigs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TempVoiceConfigs_GuildConfigs_GuildConfigId"
        FOREIGN KEY ("GuildConfigId") REFERENCES "GuildConfigs" ("Id") ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_TempVoiceConfigs_GuildConfigId" ON "TempVoiceConfigs" ("GuildConfigId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_TempVoiceConfigs_GuildId" ON "TempVoiceConfigs" ("GuildId");
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260614000000_AddTempVoice', '8.0.11') ON CONFLICT DO NOTHING;
