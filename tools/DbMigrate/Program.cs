using System;
using System.Threading.Tasks;
using Npgsql;

await Run();

const string connStr = "Host=localhost;Database=dynamite_core;Username=dynamite;Password=DynamiteV3105@2001.";

const string sql = """
    ALTER TABLE "Giveaways" RENAME COLUMN "PreSelectedWinnerId" TO "PreSelectedWinnerIds";
    ALTER TABLE "Giveaways" ALTER COLUMN "PreSelectedWinnerIds" TYPE character varying(512);
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260613000000_GiveawayMultiPreSelect', '8.0.0')
    ON CONFLICT DO NOTHING;
    """;

static async Task Run()
{
    try
    {
        await using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine("Migration applied successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}
