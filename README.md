# Dynamite Core

A feature-rich, modular Discord bot platform built with **.NET 8** and **Clean Architecture**.  
Designed as a production-ready foundation comparable to Dyno, Carl-bot, and MEE6 — with a full REST API and web dashboard *(in progress)*.

> **Nền tảng quản lý Discord** tự host, xây dựng bằng .NET 8 theo Clean Architecture.  
> Tương đương Dyno, Carl-bot, MEE6 — có REST API và web dashboard *(đang phát triển)*.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Discord.Net](https://img.shields.io/badge/Discord.Net-3.19.1-5865F2?logo=discord)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql)
![React](https://img.shields.io/badge/React-19-61DAFB?logo=react)
![Tests](https://img.shields.io/badge/Tests-32%20passing-57F287?logo=xunit)

---

## Features / Tính năng

### 🛡️ Moderation
`/ban` `/kick` `/timeout` `/untimeout` `/warn` `/warnings` `/purge` `/slowmode`  
Role hierarchy validation, permission checks, mod log channel.

### 🎭 Role Management
Auto role on join, button roles, select menu role panels, multi-panel support with persistent config.

### ⚙️ Server Setup
`/setup gaming` `/setup community` `/setup streamer`  
Auto-generates categories, channels, and roles from templates.

### 📋 Logging
Message edit/delete, member join/leave, role changes, voice state changes — configurable per-channel.

### 👋 Welcome & Verification
Welcome embeds with generated images (SkiaSharp), one-click verification with role assignment, account age filtering.

### 🔒 Security
Anti-spam, anti-mention spam, anti-invite, anti-scam link detection.  
Anti-raid engine with escalation: **warn → timeout → ban**.

### 🎉 Giveaway
`/giveaway start` with duration parser (`1d2h30m`), button-based entry with deduplication,  
background timer service (survives bot restarts), `/giveaway reroll`, `/giveaway cancel`.

### 🎫 Ticket System
Panel-based ticket creation, auto channel with permission overwrites,  
full lifecycle: **Open → Close → Delete**.

### 🪙 Economy
`/daily` with streak bonuses, `/fish` minigame (Common → Mythic drop table, cooldown),  
shop, inventory, fishing rod upgrades, `/transfer`, `/leaderboard`, `/balance`.

### 🖥️ Web Dashboard *(In Progress)*
React 19 + Vite dashboard for server admins. Discord OAuth2 login and several configuration pages are functional — remaining modules under active development.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Bot Framework | Discord.Net 3.19.1 |
| Backend | .NET 8, ASP.NET Core |
| ORM | Entity Framework Core + Npgsql |
| Database | PostgreSQL 16 |
| Auth | JWT Bearer + Discord OAuth2 |
| Frontend | React 19, Vite, TailwindCSS *(in progress)* |
| Image Generation | SkiaSharp |
| Logging | Serilog |
| Testing | xUnit + Moq — 32 tests |
| Containerization | Docker + Docker Compose |

---

## Architecture / Kiến trúc

```
src/
├── Dynamite.Core                  # Entities, interfaces (no dependencies)
├── Dynamite.Application           # Service interfaces, DI extensions
├── Dynamite.Infrastructure        # EF Core, repositories, PostgreSQL
├── Dynamite.Shared                # Shared utilities
├── Dynamite.Bot                   # Discord bot host, event handlers
├── Dynamite.API                   # ASP.NET Core REST API
├── Dynamite.Tests                 # xUnit unit tests
├── Dynamite.Modules/
│   ├── Moderation
│   ├── Welcome
│   ├── RoleManagement
│   ├── Setup
│   ├── Logging
│   └── Security
├── Dynamite.Modules.Giveaway
├── Dynamite.Modules.Ticket
├── Dynamite.Modules.Economy
└── dynamite-dashboard             # React + Vite dashboard (in progress)
```

**Clean Architecture layers:**
```
Core  ←  Application  ←  Infrastructure  ←  Bot / API
```
Each module is an independent class library — zero coupling between modules, communication only through shared service interfaces.

**Key architectural decisions:**
- Discord.Net types kept out of the Application layer — service interfaces use primitive `ulong` IDs
- `IServiceScopeFactory` for DB access within Singleton event handlers
- `SafeRun` fire-and-forget pattern isolates handler crashes from the Discord.Net event loop
- Background polling (not `Task.Delay`) for giveaway timers — survives bot restarts
- `IMemoryCache` for fishing cooldowns — no DB round-trips per command

---

## Getting Started / Cài đặt

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- Discord Bot Token ([Discord Developer Portal](https://discord.com/developers/applications))

### Run with Docker Compose

```bash
# 1. Clone
git clone https://github.com/balocvu3105-dd/Dynamite_Core.git
cd Dynamite_Core

# 2. Create env file
cp .env.example .env
# Edit .env — fill in your Discord token, client ID/secret, DB password, JWT secret

# 3. Start everything
docker compose up --build
```

| Service | URL |
|---|---|
| API | http://localhost:5000 |
| Swagger Docs | http://localhost:5000/swagger |
| Dashboard *(in progress)* | http://localhost:3000 |

Docker Compose will automatically:
1. Start PostgreSQL and wait for it to be healthy
2. Run database migrations via `Dynamite.Migrator`
3. Start the Bot and API once migrations complete

### Run Tests / Chạy test

```bash
dotnet test src/Dynamite.Tests --verbosity normal
```

---

## Environment Variables

Copy `.env.example` to `.env` and fill in your values:

| Variable | Description |
|---|---|
| `DISCORD_TOKEN` | Bot token from Discord Developer Portal |
| `DISCORD_CLIENT_ID` | OAuth2 application client ID |
| `DISCORD_CLIENT_SECRET` | OAuth2 application client secret |
| `DISCORD_TEST_GUILD_ID` | Guild ID for debug slash command registration |
| `POSTGRES_DB` | PostgreSQL database name |
| `POSTGRES_USER` | PostgreSQL username |
| `POSTGRES_PASSWORD` | PostgreSQL password |
| `JWT_SECRET` | JWT signing secret (minimum 32 characters) |
| `JWT_ISSUER` | JWT issuer (e.g. `dynamite-api`) |
| `JWT_AUDIENCE` | JWT audience (e.g. `dynamite-dashboard`) |
| `API_PORT` | API port (default: `5000`) |
| `DASHBOARD_PORT` | Dashboard port (default: `3000`) |
| `VITE_API_URL` | API URL for the dashboard to connect to |

---

## License

MIT