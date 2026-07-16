# ============================================================
#  deploy.ps1 — Commit, push GitHub, deploy VPS (Docker)
#  Chạy: Right-click -> "Run with PowerShell" HOẶC
#         .\deploy.ps1 trong PowerShell tại thư mục repo
# ============================================================

# ── Cấu hình VPS ────────────────────────────────────────────
$VPS_USER    = if ($env:VPS_USER) { $env:VPS_USER } else { "root" }
$VPS_IP      = if ($env:VPS_IP) { $env:VPS_IP } else { "103.77.243.86" }
# ────────────────────────────────────────────────────────────

$REPO = $PSScriptRoot
$COMMIT_MSG = @'
fix: add startup notifications, crash recovery detection, and global exception handling

- BotHostedService: Implement SendStartupNotificationsAsync triggered after OnReadyAsync to notify Discord audit log channels when the bot boots up or restarts.
- Crash State Detection: Add session state markers (bot_running.flag and clean_shutdown.flag) to differentiate between graceful shutdowns and unexpected crashes (Crash Detected vs Online).
- Global Exception Handling: Hook AppDomain.CurrentDomain.UnhandledException and TaskScheduler.UnobservedTaskException in Program.cs to log fatal crashes and execute Log.CloseAndFlush() before process termination.
'@

Set-Location $REPO

Write-Host "`n[1/5] Xoa git lock neu con ton tai..." -ForegroundColor Cyan
$lockFile = Join-Path $REPO ".git\index.lock"
if (Test-Path $lockFile) {
    Remove-Item $lockFile -Force
    Write-Host "     Da xoa index.lock" -ForegroundColor Yellow
} else {
    Write-Host "     Khong co lock file" -ForegroundColor Green
}

Write-Host "`n[2/5] Git add all changes..." -ForegroundColor Cyan
git add -A
if ($LASTEXITCODE -ne 0) { Write-Host "Git add that bai!" -ForegroundColor Red; exit 1 }

Write-Host "`n[3/5] Git commit..." -ForegroundColor Cyan
git commit -m $COMMIT_MSG
if ($LASTEXITCODE -ne 0) { Write-Host "Git commit that bai!" -ForegroundColor Red; exit 1 }

Write-Host "`n[4/5] Git push..." -ForegroundColor Cyan
git push origin main
if ($LASTEXITCODE -ne 0) { Write-Host "Git push that bai!" -ForegroundColor Red; exit 1 }

Write-Host "`n[5/5] Deploy len VPS..." -ForegroundColor Cyan

# Tự động tìm thư mục Dynamite Core trên VPS (bất kể là /root/bot/Dynamite_Core hay /root/Dynamite_Core...)
$VPS_COMMANDS = @'
VPS_PATH=$(find /root /home -maxdepth 4 -iname "*dynamite*core*" -type d 2>/dev/null | head -n 1)
if [ -z "$VPS_PATH" ]; then
    echo "Khong tim thay thu muc Dynamite_Core tren VPS! Vui long kiem tra lai."
    exit 1
fi
echo "==> Tim thay thu muc repo tai: $VPS_PATH"
cd "$VPS_PATH"
git stash && git pull origin main && docker compose -f docker-compose.yml up --build -d && echo '=== Deploy OK ==='
'@

ssh "${VPS_USER}@${VPS_IP}" $VPS_COMMANDS
if ($LASTEXITCODE -ne 0) { Write-Host "Deploy VPS that bai!" -ForegroundColor Red; exit 1 }

Write-Host "`n OK Hoan thanh!" -ForegroundColor Green
