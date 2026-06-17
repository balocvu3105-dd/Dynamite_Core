# migrate-local.ps1 — chạy migration thủ công qua Npgsql
$connStr = "Host=localhost;Database=dynamite_core;Username=dynamite;Password=DynamiteV3105@2001."

$sql = @"
ALTER TABLE "Giveaways" RENAME COLUMN "PreSelectedWinnerId" TO "PreSelectedWinnerIds";
ALTER TABLE "Giveaways" ALTER COLUMN "PreSelectedWinnerIds" TYPE character varying(512);
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260613000000_GiveawayMultiPreSelect', '8.0.0')
ON CONFLICT DO NOTHING;
"@

# Dùng dotnet-script hoặc tạo temp project
$tempDir = "$env:TEMP\dynamite-migrate"
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null

@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Npgsql" Version="8.0.0" />
  </ItemGroup>
</Project>
"@ | Set-Content "$tempDir\migrate.csproj"

@"
using Npgsql;
var connStr = "$connStr";
var sql = @`"$sql`";
await using var conn = new NpgsqlConnection(connStr);
await conn.OpenAsync();
await using var cmd = new NpgsqlCommand(sql, conn);
await cmd.ExecuteNonQueryAsync();
Console.WriteLine("Migration applied successfully!");
"@ | Set-Content "$tempDir\Program.cs"

Write-Host "Running migration..."
dotnet run --project $tempDir
