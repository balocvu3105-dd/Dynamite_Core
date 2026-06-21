# ⚡ Dynamite Core

A self-hosted Discord Management Platform built with .NET 8, Clean Architecture, PostgreSQL and Docker.

> Personal portfolio project developed to demonstrate backend engineering skills, software architecture design, database management, API development, and production-oriented application deployment.

---

## Overview

Dynamite Core is a Discord community management platform inspired by popular solutions such as Dyno, Carl-bot and MEE6.

The project was created to solve common community management problems including moderation, role automation, giveaways, ticket support, logging and server configuration while maintaining a scalable and maintainable architecture.

The primary goal of this project is to showcase practical software engineering skills using modern .NET technologies.

---

## Key Features

### Moderation System

* Ban, Kick, Timeout, UnTimeout
* Warning management
* Message purge
* Slowmode configuration
* Permission validation
* Moderation history tracking

### Community Management

* Auto role assignment
* Button role panels
* Select menu role panels
* Welcome messages
* Verification system

### Ticket System

* Ticket creation panel
* Private support channels
* Ticket lifecycle management
* Permission-based access control

### Giveaway System

* Scheduled giveaways
* Persistent giveaway timers
* Reroll winners
* Restart-safe background processing

### Logging System

* Message edits
* Message deletions
* Member joins and leaves
* Role updates
* Voice activity tracking

### Economy Module

* User profiles
* Daily rewards
* Leaderboards
* Progression system
* Fishing mini-game

### Server Setup Automation

* Community templates
* Gaming templates
* Automatic role creation
* Channel generation

### Web Dashboard

* Discord OAuth2 Authentication
* Server configuration management
* REST API integration

---

## Project Scope

Current implementation includes:

* 8+ Core Modules
* 30+ Slash Commands
* PostgreSQL Database Integration
* REST API
* React Dashboard
* Docker Deployment
* Background Hosted Services
* Authentication & Authorization
* Structured Logging

---

## Technology Stack

### Backend

* C#
* .NET 8
* ASP.NET Core
* Entity Framework Core
* PostgreSQL
* Npgsql

### Architecture

* Clean Architecture
* Repository Pattern
* Dependency Injection
* Domain Driven Design Principles
* Background Services

### Frontend

* React 19
* Vite
* TailwindCSS

### Infrastructure

* Docker
* Docker Compose
* Serilog

### Testing

* xUnit
* Moq

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

Contains entities, domain models and business rules.

#### Application

Contains use cases, service contracts and application logic.

#### Infrastructure

Contains database access, repositories and external integrations.

#### Bot / API

Handles Discord interactions and HTTP requests.

This structure allows business logic to remain independent from frameworks and external services, improving maintainability and testability.

---

## Technical Challenges

### Persistent Giveaway Timers

Problem:

Traditional in-memory timers are lost when the application restarts.

Solution:

Implemented a database-driven background polling service that continuously checks giveaway expiration times and processes winners safely after restart.

---

### Discord Event Handlers & Scoped Services

Problem:

Discord.Net event handlers are registered as Singleton services while Entity Framework DbContext is Scoped.

Solution:

Used IServiceScopeFactory to create service scopes inside event handlers and maintain proper dependency lifetimes.

---

### Modular Feature Development

Problem:

As the number of features increased, maintaining a single project became difficult.

Solution:

Separated major features into independent modules while sharing common application contracts and infrastructure components.

---

## What I Learned

During development I gained hands-on experience with:

* Clean Architecture
* ASP.NET Core
* Entity Framework Core
* PostgreSQL
* Discord API Integration
* REST API Development
* JWT Authentication
* OAuth2 Authentication
* Dependency Injection
* Background Services
* Docker Deployment
* Structured Logging
* Software Modularization

---

## Local Development

### Requirements

* .NET 8 SDK
* PostgreSQL
* Docker Desktop

### Run

```bash
git clone https://github.com/balocvu3105-dd/Dynamite_Core.git

cd Dynamite_Core

docker compose up --build
```

---

## Future Improvements

* Dashboard coverage for all modules
* Temp Voice Channels
* Plugin System
* Metrics & Monitoring
* Grafana Integration
* Advanced Anti-Raid Protection
* Multi-Server Administration

---

## Screenshots

### Dashboard

(Add dashboard screenshot)

### Moderation

(Add moderation screenshot)

### Ticket System

(Add ticket screenshot)

### Economy System

(Add economy screenshot)

---

## Author

Bá Lộc Vũ (DynamiteV)

GitHub:
https://github.com/balocvu3105-dd

Focused on Backend Development, .NET, PostgreSQL, Clean Architecture and Software Engineering.
