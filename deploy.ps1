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
chore: security hardening, zero-trust identity binding, and infrastructure optimization

Security Hardening & Bug Fixes:
- REST API & Dashboard: Enforce strict Administrator (0x8) and Server Owner authorization in RequireGuildAdmin and GuildAuthorizationService.
- Zero-Trust Identity Binding: Verify X-Discord-Token ownership against JWT sub/NameIdentifier claim via Discord API with 5-minute caching.
- Broken Access Control / IDOR: Protect ModulesController with RequireGuildAdmin attribute.
- Concurrency & Race Conditions: Enable EF Core Optimistic Concurrency Control on UserWallet and handle duplicate key exceptions in GiveawayService.
- Infrastructure: Restrict PostgreSQL port exposure to local loopback (127.0.0.1:5432) in docker-compose.yml.
- Frontend Security: Eliminate raw DOM manipulation (XSS) in Callback.tsx, implement OAuth state CSRF protection, and safeguard localStorage.
- Audit Logging: Record exact server-side verified admin Discord User IDs in economy balance updates.
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
git stash && git pull origin main && docker compose up --build -d && echo '=== Deploy OK ==='
'@

ssh "${VPS_USER}@${VPS_IP}" $VPS_COMMANDS
if ($LASTEXITCODE -ne 0) { Write-Host "Deploy VPS that bai!" -ForegroundColor Red; exit 1 }

Write-Host "`n OK Hoan thanh!" -ForegroundColor Green
