# Dynamite Core

A feature-rich, modular Discord bot platform built with **.NET 8** and **Clean Architecture**.  
Designed as a production-ready foundation comparable to Dyno, Carl-bot, and MEE6 — with a full REST API and React dashboard.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Discord.Net](https://img.shields.io/badge/Discord.Net-3.19.1-5865F2?logo=discord)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql)
![React](https://img.shields.io/badge/React-19-61DAFB?logo=react)
![Tests](https://img.shields.io/badge/Tests-32%20passing-57F287?logo=xunit)

---

## Features

### 🛡️ Moderation
- `/ban`, `/kick`, `/timeout`, `/untimeout`, `/warn`, `/warnings`, `/purge`, `/slowmode`
- Role hierarchy validation, permission checks, mod log channel

### 🎭 Role Management
- Auto role on join
- Button roles and select menu role panels
- Multi-panel support with persistent configuration

### ⚙️ Server Setup
- `/setup gaming`, `/setup community`, `/setup streamer`
- Auto-generates categories, channels, and roles from templates

### 📋 Logging
- Message edit/delete tracking
- Member join/leave, role changes
- Voice state changes, server events
- Configurable per-channel

### 👋 Welcome & Verification
- Welcome embeds with generated images (SkiaSharp)
- One-click verification button with role assignment
- Account age filtering

### 🔒 Security
- Anti-spam with configurable thresholds
- Anti-mention spam, anti-invite, anti-scam link detection
- Anti-raid with escalation engine (warn → mute → ban)

### 🎉 Giveaway
- `/giveaway start` with duration parser (`1d2h30m`)
- Button-based entry with deduplication
- Background timer service — survives bot restarts
- `/giveaway reroll`, `/giveaway cancel`

### 🎫 Ticket System
- Panel-based ticket creation with staff role permissions
- Auto channel creation with permission overwrites
- Full lifecycle: Open → Close → Delete

### 🪙 Economy
- `/daily` with streak bonuses (capped at +200 coins/day)
- `/fish` minigame with drop table (Common → Mythic), cooldown system
- Shop, inventory, fishing rod upgrades with stat multipliers
- `/transfer`, `/leaderboard`, `/balance`

---

## Tech Stack

| Layer | Technology |
|---|---|
| Bot Framework | Discord.Net 3.19.1 |
| Backend | .NET 8, ASP.NET Core |
| ORM | Entity Framework Core + Npgsql |
| Database | PostgreSQL 16 |
| Auth | JWT Bearer + Discord OAuth2 |
| Frontend | React 19, Vite, TailwindCSS |
| Image Gen | SkiaSharp |
| Logging | Serilog |
| Testing | xUnit + Moq (32 tests) |
| Containerization | Docker + Docker Compose |

---

## Architecture

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
└── dynamite-dashboard             # React + Vite dashboard
```

**Key architectural decisions:**
- Discord.Net types are kept out of the Application layer — service interfaces use primitive `ulong` IDs
- `IServiceScopeFactory` for DB access within Singleton event handlers
- `SafeRun` fire-and-forget pattern isolates handler crashes from the Discord.Net event loop
- Background polling (not `Task.Delay`) for giveaway timers — survives restarts
- `IMemoryCache` for fishing cooldowns — no DB round-trips per command

---

## Getting Started

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- Discord Bot Token ([Discord Developer Portal](https://discord.com/developers/applications))

### Run with Docker Compose

```bash
# 1. Clone the repository
git clone https://github.com/balocvu3105-dd/Dynamite_Core.git
cd Dynamite_Core

# 2. Create environment file
cp .env.example .env

# 3. Fill in your credentials
#    Edit .env with your Discord token, client ID/secret, and passwords
notepad .env   # Windows
# or: nano .env / code .env

# 4. Start the full stack
docker compose up --build
```

Services will be available at:
- **Dashboard** → http://localhost:3000
- **API** → http://localhost:5000
- **API Docs (Swagger)** → http://localhost:5000/swagger

### Run Locally (Development)

**Prerequisites:** .NET 8 SDK, PostgreSQL, Node.js 20+

```bash
# Start PostgreSQL
docker compose up postgres

# Run database migrations
dotnet ef database update --project src/Dynamite.Infrastructure --startup-project src/Dynamite.Bot

# Run the bot
dotnet run --project src/Dynamite.Bot

# Run the API (separate terminal)
dotnet run --project src/Dynamite.API

# Run the dashboard (separate terminal)
cd src/dynamite-dashboard
npm install
npm run dev
```

### Run Tests

```bash
dotnet test src/Dynamite.Tests --verbosity normal
```

---

## Dashboard

The React dashboard provides a web interface for server administrators to configure the bot without using slash commands.

**Features:**
- Discord OAuth2 login
- Module enable/disable toggles
- Moderation settings
- Logging channel configuration
- Welcome & verification setup
- Anti-spam and anti-raid configuration

---

## Environment Variables

Copy `.env.example` to `.env` and fill in your values:

| Variable | Description |
|---|---|
| `DISCORD_TOKEN` | Bot token from Discord Developer Portal |
| `DISCORD_CLIENT_ID` | OAuth2 application client ID |
| `DISCORD_CLIENT_SECRET` | OAuth2 application client secret |
| `DISCORD_TEST_GUILD_ID` | Guild ID for debug command registration |
| `POSTGRES_PASSWORD` | PostgreSQL password |
| `JWT_SECRET` | JWT signing secret (min 32 characters) |
| `VITE_API_URL` | API URL for the dashboard to connect to |

---

## Project Structure Details

### Clean Architecture Layers

```
Core (no dependencies)
  ↑
Application (depends on Core)
  ↑
Infrastructure (depends on Application + Core)
  ↑
Bot / API (depends on all layers)
```

### Module System

Each module is an independent class library:
- **Zero coupling** between modules — they communicate only through shared service interfaces
- Modules register their own slash commands via `InteractionModuleBase`
- New modules can be added by creating a project and registering one assembly reference

---

## License

MIT License — see [LICENSE](LICENSE) for details.