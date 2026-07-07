# ⚡ Dynamite Core

A self-hosted Discord Management Platform built with .NET 8, Clean Architecture, PostgreSQL, and Docker.

> Personal portfolio project developed to demonstrate backend engineering skills, software architecture design, database management, API development, and production-oriented application deployment.

---

## Overview

Dynamite Core is a comprehensive Discord community management platform inspired by popular solutions such as Dyno, Carl-bot, and MEE6.

The project was created to solve common community management problems including moderation, role automation, giveaways, ticket support, logging, security/anti-raid, and server configuration while maintaining a scalable and maintainable architecture.

The primary goal of this project is to showcase practical software engineering skills using modern .NET technologies and industry best practices.

---

## Key Features

### Moderation System
* Ban, Kick, Timeout, and UnTimeout with automated logging
* Warning management and user moderation history tracking
* Message purge and slowmode configuration
* Hierarchical permission validation
* Blacklist management for problematic users and domains

### Security & Anti-Spam System (Anti-Raid)
* Automated spam detection and real-time violation tracking
* Configurable escalation engine (auto-timeout, kick, ban based on violation thresholds)
* Server-specific anti-spam configuration
* Real-time security event monitoring and incident logging

### Temporary Voice Channels (Temp Voice)
* Dynamic "Join-to-Create" voice channels
* Automatic voice channel generation and cleanup when empty
* Custom permissions and channel lifecycle management
* Persistent state tracking across bot restarts

### Community & Role Management
* Automatic role assignment upon server join
* Interactive Button and Select Menu role panels
* Customizable welcome messages and embed notifications
* Interactive user verification system

### Ticket System
* Ticket creation panels with category routing
* Private support channels and thread management
* Ticket lifecycle management and closing workflows
* Permission-based staff access control

### Giveaway System
* Scheduled and multi-winner giveaways
* Persistent database-driven giveaway timers
* Reroll winner functionality
* Restart-safe background processing

### Logging System
* Message edits and deletions tracking
* Member joins, leaves, and role updates
* Voice channel activity and switching tracking
* Comprehensive moderation and security action logging

### Economy & RPG Module
* User profiles, wallet, banking, and invoice system
* Interactive shop system with item showcase and purchasing
* XP progression system with daily rewards and leaderboards
* **Advanced Fishing Mini-game:**
  * Dynamic weather system and weather forecasts affecting fishing outcomes
  * Fish inventory (Bag) and Fish Encyclopedia
  * Special fishing pools and time-based events
  * Automated background fishing scheduler
* Real-time leaderboards with automated background updates
* Interactive user guide and tutorial system

### Server Setup Automation
* Community and gaming server structure templates
* Automatic role hierarchy and permission creation
* Automated channel and category generation

### Web Dashboard
* Discord OAuth2 Authentication & Authorization
* Real-time server overview and analytics
* Comprehensive REST API integration with JWT security
* Module configuration management:
  * Moderation, Logging, and Security settings
  * Welcome messages and Verification setup
  * Economy and Leaderboard management
  * Custom command toggles and permission controls

---

## Project Scope

Current implementation includes:

* **10+ Core Modules** (Moderation, Security, Voice, Economy, Giveaways, Tickets, Logging, Role Management, Setup, Welcome)
* **40+ Slash Commands & Interactive Components**
* **PostgreSQL Database Integration** with Entity Framework Core & Npgsql
* **REST API & Clean Architecture Backend**
* **Modern React 19 + Vite + TailwindCSS Dashboard**
* **Docker & Docker Compose Deployment**
* **Background Hosted Services** (Giveaway timers, Auto-Fishing, Leaderboard updates, Weather simulation)
* **Authentication & Authorization** (JWT & Discord OAuth2)
* **Structured Logging** with Serilog

---

## Technology Stack

### Backend
* C# / .NET 8 / ASP.NET Core
* Entity Framework Core
* PostgreSQL / Npgsql

### Architecture
* Clean Architecture
* Repository Pattern & Dependency Injection
* Domain-Driven Design (DDD) Principles
* Background Hosted Services

### Frontend
* React 19
* Vite
* TailwindCSS

### Infrastructure
* Docker & Docker Compose
* Serilog (Structured Logging)

### Testing
* xUnit & Moq

---

## Architecture

```text
Core
 ↑
Application
 ↑
Infrastructure
 ↑
Bot / API
```

### Layer Responsibilities

#### Core
Contains domain entities, value objects, domain interfaces, and core business rules.

#### Application
Contains use cases, service contracts, DTOs, and application logic.

#### Infrastructure
Contains database access, EF Core configurations, repositories, and external service integrations.

#### Bot / API
Handles Discord interactions, slash commands, event handlers, and HTTP REST API endpoints.

This structure ensures that core business logic remains independent of frameworks and external UI/infrastructure concerns, maximizing maintainability and testability.

---

## Technical Challenges & Solutions

### Persistent Giveaway Timers
* **Problem:** Traditional in-memory timers are lost when the application restarts or crashes.
* **Solution:** Implemented a database-driven background polling service that continuously evaluates giveaway expiration timestamps and processes winners safely after restarts without data loss.

### Discord Event Handlers & Scoped Services
* **Problem:** Discord.Net event handlers are registered as Singleton services, whereas Entity Framework Core `DbContext` is Scoped.
* **Solution:** Utilized `IServiceScopeFactory` to dynamically generate service scopes within event handlers, ensuring proper dependency lifetimes and preventing thread-safety issues with EF Core.

### Modular Feature Development
* **Problem:** As the number of bot features and dashboard integrations expanded, maintaining a monolithic project structure became difficult to navigate and scale.
* **Solution:** Architected the system into independent feature modules (`Dynamite.Modules.*`) while sharing common application contracts and infrastructure components.

### Security Escalation Engine & Violation Tracking
* **Problem:** Detecting spam and raid attempts requires evaluating user behavior across multiple asynchronous Discord gateway events in real-time without blocking threads or creating database bottlenecks.
* **Solution:** Designed an in-memory violation tracking mechanism paired with an automated escalation engine. When violation thresholds are exceeded, the system progressively escalates automated moderation actions (timeout, kick, ban) and logs actions asynchronously.

---

## What I Learned

During development, I gained hands-on engineering experience with:

* Designing and implementing **Clean Architecture** and **Domain-Driven Design** in .NET 8
* Building scalable REST APIs with ASP.NET Core and integrating **JWT / OAuth2 Authentication**
* Managing relational database schemas, migrations, and queries using **PostgreSQL** and **Entity Framework Core**
* Developing real-time applications and interactive components using the **Discord API (Discord.Net)**
* Designing background processing workflows with .NET **Hosted Services**
* Containerizing multi-service applications using **Docker** and **Docker Compose**
* Implementing structured logging and observability with **Serilog**
* Building modern, responsive single-page applications with **React 19, Vite, and TailwindCSS**

---

## Local Development

### Requirements
* .NET 8 SDK
* PostgreSQL
* Docker Desktop & Node.js (for frontend development)

### Running with Docker Compose

```bash
git clone https://github.com/balocvu3105-dd/Dynamite_Core.git
cd Dynamite_Core
docker compose up --build
```

---

## Future Improvements

* Dashboard coverage for all remaining feature modules
* Plugin System for dynamic third-party feature loading
* Metrics, Monitoring & OpenTelemetry Integration
* Grafana Dashboard Integration for server performance visualization
* Multi-Server Administration & Sharding Support

---

## Screenshots

### Dashboard
*(Add dashboard screenshot)*

### Moderation & Security
*(Add moderation screenshot)*

### Ticket System
*(Add ticket screenshot)*

### Economy & Fishing Mini-game
*(Add economy screenshot)*

---

## Author

**Bá Lộc Vũ (DynamiteV)**

GitHub: https://github.com/balocvu3105-dd

Focused on Backend Development, .NET, PostgreSQL, Clean Architecture, and Software Engineering.
