# ⚡ Dynamite Core — Production-Grade Discord Management Ecosystem

[![.NET 8](https://img.shields.io/badge/.NET%208.0-C%23-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-REST%20API-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![React 19](https://img.shields.io/badge/React%2019-Vite%20+%20TS-61DAFB?logo=react&logoColor=black)](https://react.dev/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16%20Debian-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-Multi--Container-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20%2F%20DDD-success)](https://github.com/balocvu3105-dd/Dynamite_Core)
[![Tests](https://img.shields.io/badge/Unit%20Tests-44%20Passing-brightgreen)](https://github.com/balocvu3105-dd/Dynamite_Core)

> **Engineering Portfolio Note:** `Dynamite Core` is not a simple script or a monolithic bot wrapper. It is a **full-scale, highly distributed C# / .NET 8 enterprise-grade platform** engineered from scratch to demonstrate advanced architectural mastery in **Clean Architecture**, **Domain-Driven Design (DDD)**, **High-Concurrency Gateway Processing**, **Distributed Background Scheduling**, and **Multi-Container Cloud Infrastructure**.

---

## 🏛️ Executive Engineering Summary

Modern real-time communication platforms generate high-velocity, concurrent, and unpredictable event streams. Building a resilient automation and management ecosystem on top of the Discord Gateway requires solving fundamental distributed systems challenges: thread safety across asynchronous pipelines, database connection starvation, stateful scheduling across container restarts, and secure stateless API authorization.

**Dynamite Core** addresses these challenges through an 18-project modular solution composed of:
1. **A High-Throughput Gateway Processor (`Dynamite.Bot`)**: Consumes high-frequency Discord websocket events via multi-threaded asynchronous pipelines, bridging singleton event loops with scoped dependency injection.
2. **A Stateless REST API Gateway (`Dynamite.API`)**: Serves an interactive SPA dashboard with strict JWT / Discord OAuth2 authorization, structured error middlewares, and CORS boundary protection.
3. **An Autonomous Database Migrator (`Dynamite.Migrator`)**: Decouples schema migrations (`EF Core` + `Npgsql`) from application runtime, guaranteeing database schema synchronization inside CI/CD and Docker pipelines prior to service boot.
4. **10+ Decoupled Feature Modules (`Dynamite.Modules.*`)**: Plug-and-play domain modules adhering strictly to interface contracts and modular separation of concerns.

---

## 📐 System Architecture & Data Flow

```mermaid
graph TD
    subgraph Client Layer ["Client Layer"]
        SPA["💻 React 19 + Vite Dashboard<br/>(TailwindCSS / TypeScript)"]
        DiscordClient["📱 Discord App / Users<br/>(Slash Commands & Events)"]
    end

    subgraph Edge / API Layer ["Edge & API Layer (.NET 8)"]
        API["🌐 Dynamite.API (ASP.NET Core)<br/>• JWT / OAuth2 Auth Engine<br/>• ErrorHandlingMiddleware<br/>• DTOs & REST Endpoints"]
        Bot["🤖 Dynamite.Bot (Hosted Service)<br/>• Gateway Event Handlers<br/>• Command Execution Engine<br/>• Crash State Detection"]
    end

    subgraph Modular Domain Layer ["Clean Architecture Application & Modules"]
        App["📦 Dynamite.Application<br/>• Service Contracts / Interfaces<br/>• CQRS / MediatR Handlers<br/>• DTO Mapping"]
        Modules["🧩 Dynamite.Modules.* (10+ Projects)<br/>• Moderation | Security | Economy<br/>• TempVoice | Giveaways | Tickets<br/>• Setup | Welcome | Logging"]
    end

    subgraph Core Domain Layer ["Core Domain Layer"]
        Core["💎 Dynamite.Core<br/>• Domain Entities & Aggregates<br/>• Value Objects & Enums<br/>• Repository & Unit of Work Contracts"]
    end

    subgraph Infrastructure Layer ["Infrastructure & Storage"]
        Infra["⚙️ Dynamite.Infrastructure<br/>• EF Core DbContext & Migrations<br/>• Npgsql / PostgreSQL Repositories<br/>• Discord OAuth & External Services"]
        PG[("🐘 PostgreSQL 16 (Debian)<br/>• ACID Transactions<br/>• Relational Domain Data")]
    end

    SPA <-->|REST / JSON (JWT + Cookie)| API
    DiscordClient <-->|WebSocket Gateway / API v10| Bot
    API --> App
    Bot --> App
    App --> Modules
    Modules --> Core
    App --> Core
    Infra --> Core
    API --> Infra
    Bot --> Infra
    Infra <-->|EF Core / SQL| PG
```

---

## 🔍 Core Architectural Design Patterns

### 1. Strict Clean Architecture & Dependency Inversion (`DIP`)
The workspace is organized to strictly enforce the **Dependency Inversion Principle**. Business logic resides exclusively inside `Dynamite.Core` (Domain Entities, Value Objects, Core Rules) and `Dynamite.Application` (Use Cases, Service Contracts), completely agnostic of SQL, external web APIs, or UI frameworks.
* **Infrastructure Layer (`Dynamite.Infrastructure`)** implements domain interfaces (`IRepository<T>`, `IUnitOfWork`, `IDiscordOAuthService`) and registers them via Extension Methods (`AddInfrastructure()`).
* **Presentation Layers (`Dynamite.API`, `Dynamite.Bot`)** depend solely on `Dynamite.Application` abstractions.

### 2. Modular Monolith Decomposition (`Dynamite.Modules.*`)
Instead of allowing domain logic to degenerate into a tangled monolith, the system isolates each distinct business capability into dedicated assembly projects:
* `Dynamite.Modules.Moderation`: Hierarchical permission validation, warning tracking, purge engines, and penalty enforcers.
* `Dynamite.Modules.Security`: Anti-raid sliding windows, link/invite filters, and automated escalation thresholds.
* `Dynamite.Modules.Economy`: ACID-compliant wallet/banking transactions, item shop inventory, XP/leveling, and dynamic weather-driven fishing simulations.
* `Dynamite.Modules.Voice`: Dynamic "Join-to-Create" channel lifecycle management and transient state tracking.
* `Dynamite.Modules.Giveaway` & `Dynamite.Modules.Ticket`: Background polling timers and thread/channel lifecycle routers.

### 3. Repository Pattern & Unit of Work
To guarantee ACID transaction boundaries across complex multi-table domain operations (e.g., deducting economy currency while simultaneously generating inventory items and creating audit logs), `IUnitOfWork` orchestrates changes across distinct repositories before executing `SaveChangesAsync(ct)`.

---

## 🛠️ Deep Dive: Solving Complex Technical Engineering Challenges

### 1. Thread-Safety Across Singleton WebSocket Loops & Scoped `DbContext`
* **The Engineering Problem:** Discord.Net's `DiscordSocketClient` operates as a high-throughput, multi-threaded `Singleton` service receiving thousands of asynchronous WebSocket gateway events (message creations, voice state changes, button interactions). However, Entity Framework Core’s `DbContext` is fundamentally `Scoped` and **not thread-safe**. Injecting or sharing a database context directly inside gateway event handlers causes severe race conditions (`ConcurrentContextUsageException`) under concurrent server loads.
* **The Solution:** Implemented strict lifetime bridging using `IServiceScopeFactory`. Every gateway event handler (`LoggingEventHandler`, `EconomyEventHandler`, `SecurityEventHandler`) dynamically spawns a transient execution scope, resolves scoped application services (`IUnitOfWork`, domain services) inside isolated execution boundaries, and safely disposes the scope upon completion.

```csharp
// Example: Safe Scoped Execution inside Singleton Event Loop
public async Task HandleMessageReceivedAsync(SocketMessage message)
{
    if (message.Author.IsBot) return;

    using var scope = _scopeFactory.CreateScope();
    var securityService = scope.ServiceProvider.GetRequiredService<ISecurityService>();
    await securityService.EvaluateSpamAndRaidAsync(message);
}
```

### 2. Lock-Free Sliding Window Anti-Raid & Spam Escalation Engine
* **The Engineering Problem:** Performing database lookups on every single message across multiple active Discord servers to check for spam or raid signatures introduces massive latency and saturates PostgreSQL connection pools.
* **The Solution:** Engineered a thread-safe, in-memory sliding window tracking mechanism inside `Dynamite.Modules.Security`. High-frequency message timestamps and similarity hashes are evaluated in memory via `ConcurrentDictionary` rate-limiters. When violation thresholds are breached within a moving time window, an autonomous **Escalation Engine** progressively applies tiered penalties (Warning → Timeout → Kick → Ban) and asynchronously persists violation audit logs without blocking the Gateway thread.

### 3. Fault-Tolerant & Idempotent Background Scheduling (`IHostedService`)
* **The Engineering Problem:** In-memory timers (`System.Threading.Timer` or `Task.Delay`) are volatile; scheduled giveaways, temporary voice channels, or leaderboards vanish if the container restarts or crashes due to OS upgrades (`SIGTERM`).
* **The Solution:** Designed autonomous background worker services (`GiveawayTimerService`, `LeaderboardHostedService`, `AutoFishScheduler`, `WeatherNotifier`) inheriting from `.NET BackgroundService`. These services execute periodic non-blocking database polling (`SELECT ... WHERE EndTime <= NOW() AND IsCompleted = FALSE`). To prevent duplicate payouts or double-processing during horizontal scaling or rapid container restarts, state transitions are executed within atomic database transactions (`IsCompleted = true`).

### 4. Enterprise Auth Engine: SPA OAuth2 Authorization Code Flow & Stateful CSRF Protection
* **The Engineering Problem:** Single-Page Applications (`React 19`) connecting to REST APIs must authenticate via third-party OAuth2 without exposing client secrets or suffering from `invalid_grant` URI mismatch errors between development, staging, and production environments.
* **The Solution:** Engineered an exact-match OAuth2 Authorization Code flow where the SPA explicitly passes its originating `redirect_uri` to the backend. `Dynamite.API` verifies state parameters stored in HTTP-only cookies against CSRF attacks, exchanges authorization codes with Discord v10 REST APIs, issues short-lived JWT Access Tokens (`X-Discord-Token`), and writes encrypted Refresh Tokens (`SHA-256` hashed) directly to PostgreSQL (`RefreshTokens` table) with revocation tracking.

### 5. Linux Kernel & Filesystem Resilience (`Structure needs cleaning` / `ENODATA`)
* **The Engineering Problem:** Running lightweight `postgres:alpine` (`musl libc`) containers under heavy checkpoint write loads on certain Linux VPS kernels (`overlayfs` / `ext4`) triggered OS-level kernel errors (`sync_file_range` returning `ENODATA` or `Error 117 EUCLEAN Structure needs cleaning`), forcing the PostgreSQL checkpointer into a `PANIC` crash loop.
* **The Solution:** Diagnosed kernel-level `dmesg` allocation failures, migrated the storage container to `postgres:16` (`Debian glibc` architecture for rock-solid POSIX I/O flushing compliance), and implemented autonomous crash state detection (`clean_shutdown.flag` vs `bot_running.flag`) paired with `AppDomain.CurrentDomain.UnhandledException` / Serilog `Log.CloseAndFlush()` hooks inside `.NET 8`.

---

## 🚀 Workspace Project Breakdown (`src/`)

| Project Name | Layer | Purpose & Responsibilities |
| :--- | :--- | :--- |
| `Dynamite.Core` | **Core Domain** | Entities (`ModerationAction`, `GuildConfig`, `UserAccount`), Enums, Domain Exceptions, Repository Interfaces (`IRepository<T>`, `IUnitOfWork`). |
| `Dynamite.Application` | **Application** | Use Case DTOs (`AuthResponse`, `DiscordUserDto`), Service Interfaces (`IAuthService`, `ISecurityService`), Business Logic Orchestration. |
| `Dynamite.Infrastructure` | **Infrastructure** | `AppDbContext` (EF Core 8), Entity Configurations (`IEntityTypeConfiguration<T>`), Npgsql Repositories, Discord REST Services. |
| `Dynamite.API` | **Presentation / Edge** | ASP.NET Core REST API Endpoints, JWT Token Generator, `ErrorHandlingMiddleware`, CORS & Rate Limiting, Swagger UI. |
| `Dynamite.Bot` | **Presentation / Worker**| `DiscordSocketClient` Gateway Host, Slash Command Execution Engine (`CommandService`), Event Hubs, Background Schedulers. |
| `Dynamite.Migrator` | **DevOps / Tooling** | Autonomous EF Core Migration Execution Console App. Runs inside Docker pipelines (`docker compose up`) before API/Bot startup. |
| `Dynamite.Modules.*` | **Domain Modules** | 10 independent feature assemblies: `Economy`, `Moderation`, `Security`, `Voice`, `Giveaway`, `Ticket`, `Logging`, `Setup`, `Welcome`, `RoleManagement`. |
| `Dynamite.Tests` | **Quality Assurance** | 44+ Automated Unit & Integration Tests using `xUnit` and `Moq` verifying domain logic, calculation formulas, and service boundaries. |
| `dynamite-dashboard` | **Frontend UI** | Modern React 19 + TypeScript + Vite + TailwindCSS Single-Page Application communicating via Axios with typed API clients. |

---

## 🧪 Quality Assurance & Automated Testing

The codebase maintains a robust testing culture to verify critical financial calculations (Economy wallet math, banking interest, tax deductions), moderation hierarchy rules, and data mappers:

```bash
# Execute unit test suite across all modules
dotnet test src/Dynamite.Tests/Dynamite.Tests.csproj -v minimal

# Test Execution Output:
# Passed!  - Failed: 0, Passed: 44, Skipped: 0, Total: 44, Duration: ~1.0 s
```

---

## 🐳 Containerized Production Deployment (`Docker Compose`)

The platform is designed for zero-configuration, production-ready deployment using **Docker & Docker Compose**. The environment isolates PostgreSQL, executes database schema migrations autonomously (`dynamite_migrator`), and boots the API, Bot, and Dashboard containers cleanly.

```bash
# 1. Clone repository
git clone https://github.com/balocvu3105-dd/Dynamite_Core.git
cd Dynamite_Core

# 2. Configure environment variables (Copy example environment)
cp .env.example .env

# 3. Build and launch the entire enterprise stack in detached mode
docker compose up --build -d

# 4. Verify container health & logs
docker ps
docker logs -f dynamite_api
```

---

## 👨‍💻 Author & Engineering Contact

**Bá Lộc Vũ (DynamiteV)**
* **Focus Areas:** High-Performance C# / .NET 8 Backend Engineering, Distributed Systems, Clean Architecture, Database Optimization (PostgreSQL / EF Core), and DevOps Automation.
* **GitHub Profile:** [github.com/balocvu3105-dd](https://github.com/balocvu3105-dd)
* **Project Repository:** [github.com/balocvu3105-dd/Dynamite_Core](https://github.com/balocvu3105-dd/Dynamite_Core)

---
*Built with unwavering commitment to clean code, performance engineering, and software craftsmanship.*
