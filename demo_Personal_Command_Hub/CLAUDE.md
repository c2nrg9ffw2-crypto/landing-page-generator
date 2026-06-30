# Personal Command Hub (PCH) — Claude Code Project Instructions

## Project Overview
A local .NET 8 application (Blazor Web App, Server interactivity) that serves as a personal daily assistant.
Runs locally on a Beelink mini PC (Windows). Uses SQLite for storage. Built in 5 days.

## Tech Stack
- **Frontend:** Blazor Web App (Server)
- **Backend:** .NET 8 Web API
- **Database:** SQLite via Entity Framework Core
- **Notifications:** Windows Toast Notifications (WinRT API)
- **News:** RSS feeds via SyndicationFeed
- **Email reading:** MailKit (IMAP)
- **AI summaries (optional):** Anthropic Claude API (claude-sonnet-4-6)

## Project Structure
```
PCH/
├── PCH.App/          # Blazor Hybrid frontend (MAUI or Electron)
├── PCH.Api/          # .NET 8 Web API backend
├── PCH.Core/         # Shared models, interfaces, DTOs
├── PCH.Data/         # EF Core + SQLite
├── PCH.Connectors/   # Email, Booking, RSS connectors
├── PCH.Notifications/ # Desktop + email notification service
└── PCH.Tests/        # Unit tests
```

## Coding Rules
- Always use async/await for IO operations
- Use dependency injection everywhere (IServiceCollection)
- Keep each connector in its own class implementing IConnector
- Use records for DTOs, classes for EF entities
- Add XML doc comments on all public methods
- Never hardcode credentials — use appsettings.json + user secrets

## Day-by-Day Build Plan
- **Day 1:** Solution setup, SQLite, Task CRUD, dashboard UI skeleton
- **Day 2:** Progress bars, categories, deadlines, auto-update logic
- **Day 3:** Email connector (MailKit IMAP), auto-task creation from email
- **Day 4:** Booking connector (choose one platform), bookings → tasks
- **Day 5:** RSS news reader, notifications, UI polish, local deploy test

## Naming Conventions
- Classes: PascalCase
- Methods: PascalCase
- Variables/fields: camelCase
- Interfaces: IPascalCase
- DB tables: plural snake_case (tasks, news_items, bookings)

## Environment
- OS: Windows 11 (Beelink mini PC)
- IDE: Visual Studio Code with Claude Code extension
- .NET SDK: 8.0+
- Node: not required (pure .NET)

## Commands to Know
```bash
dotnet new sln -n PCH
dotnet new blazor -n PCH.App --interactivity Server
dotnet new webapi -n PCH.Api
dotnet new classlib -n PCH.Core
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run
```

## Sub-Agents in Use
See `.claude/agents/` for specialized agents:
- `architect.agent.md` — Plan features, design patterns
- `backend.agent.md` — .NET API + EF Core implementation
- `frontend.agent.md` — Blazor UI components
- `connector.agent.md` — Email/RSS/Booking connectors
- `reviewer.agent.md` — Code review and quality checks
