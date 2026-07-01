# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview
Personal Command Hub (PCH) — a local-only .NET 8 personal dashboard (tasks, email, news, notifications) built as a 5-day course project and run on a Beelink mini PC. No cloud storage or cloud AI: IMAP is the only external connection, and classification/summarization runs against a locally-hosted GPT4All model. See `README.md` for the feature overview and `docs/BUILD_PLAN.md` for day-by-day status.

## Tech Stack
- **Frontend:** Blazor Web App, Server interactivity (`PCH.App`)
- **Backend:** ASP.NET Core 8 Web API (`PCH.Api`)
- **Database:** SQLite via EF Core (`PCH.Data`)
- **Email:** MailKit (IMAP) (`PCH.Connectors`)
- **News:** RSS via `System.ServiceModel.Syndication` (`PCH.Connectors`)
- **AI:** GPT4All running locally (Llama 3.2 1B Instruct), OpenAI-compatible API on port 4891 — **not** a cloud API
- **Notifications:** Windows Toast via `CommunityToolkit.WinUI.Notifications` (`PCH.Notifications`)
- **Auth:** Single-user cookie auth + BCrypt password hashing (`PCH.Core/Security/PasswordHasher.cs`)

## Commands

```bash
# Build / test
dotnet build
dotnet test
dotnet test --filter FullyQualifiedName~TestName   # run a single test

# EF Core migrations (run from repo root)
dotnet ef migrations add <Name> --project PCH.Data --startup-project PCH.Api
dotnet ef database update --project PCH.Data --startup-project PCH.Api

# Run both apps (separate terminals) — API must be reachable at the URL PCH.App's ApiBaseUrl points to
dotnet run --project PCH.Api    # http://localhost:5292, Swagger at /swagger
dotnet run --project PCH.App    # http://localhost:5290 (https://localhost:7083)

# Generate a password hash for Auth:PasswordHash (user-secrets), then exits
dotnet run --project PCH.App -- hash-password "your-password"
```

There is no local-only demo start/stop script — `start-demo.ps1` / `stop-demo.ps1` launch **and expose** the app publicly via `tailscale funnel`. Only run them when a public demo is actually wanted; for normal dev, use `dotnet run` on each project directly. They deliberately use plain `dotnet run`, not `dotnet watch`, because `dotnet watch` rebinds the port on file changes and breaks the Funnel tunnel — don't "fix" this by switching to `dotnet watch`.

GPT4All's desktop app must be running with its local API server enabled (port 4891) for email classification to work; without it, `LlmClassifier` fails closed to `EmailType.Other` with no summary rather than throwing.

Secrets (IMAP credentials, `Auth:PasswordHash`) are never in `appsettings.json` — they're set via `dotnet user-secrets` from `PCH.Api` / `PCH.App` respectively. `pch.db*` and user-secrets are gitignored.

## Architecture

### Project layout and dependency direction
```
PCH.Core          — Models (EF entities), Dtos, Interfaces, Security. No dependencies on other PCH projects.
PCH.Data          — PchDbContext, migrations, DemoDataSeeder. Depends on PCH.Core.
PCH.Connectors    — EmailConnector (MailKit IMAP), RssConnector, LlmClassifier. Depends on PCH.Core, PCH.Data.
PCH.Notifications — NotificationService (Windows Toast). Depends on PCH.Core.
PCH.Api           — Controllers + NotificationCheckService (BackgroundService). Wires everything via DI in Program.cs.
PCH.App           — Blazor Server UI. Talks to PCH.Api only through typed HttpClient *ApiClient classes — never touches PCH.Data/EF directly.
PCH.Tests         — xUnit.
```
`PCH.App` and `PCH.Api` are two separate ASP.NET Core processes on different ports, connected only over HTTP (CORS-restricted to `PCH.App`'s origin). Any new feature that needs data in the UI requires: an EF entity/DbSet (`PCH.Data`), a controller endpoint (`PCH.Api`), and a typed client method (`PCH.App/Services/*ApiClient.cs`) — there's no shared process or in-proc call path between them.

### Two-app request flow
1. Browser talks to `PCH.App` (Blazor Server, SignalR circuit) — this is the only app-facing surface, protected by cookie auth (`PCH.Auth` cookie, 8h sliding expiration, login rate-limited to 5/2min).
2. `PCH.App` server-side code calls `PCH.Api` over plain HTTP on localhost via typed clients (`TaskApiClient`, `EmailApiClient`, `NewsApiClient`, `SettingsApiClient`).
3. `PCH.Api` has no auth of its own — it trusts CORS + being bound to localhost. Don't expose it beyond localhost/Tailscale without adding real API auth.
4. `PCH.Api` runs `db.Database.Migrate()` and seeds demo data on every startup (see `Program.cs`), so migrations must be idempotent-safe to re-run.

### Connectors
- All connectors implement `IConnector` (`PCH.Core/Interfaces/IConnector.cs`): `Name` + `FetchAsync(...)` returning count of new items ingested. Follow this pattern for any new data source (e.g. the paused Booking/WebUntis connectors).
- `EmailConnector` fetches via IMAP, pre-filters by keyword, then calls `LlmClassifier` (local GPT4All) for type + summary; matching emails auto-create `TaskItem`s.
- `RssConnector` dedupes by `NewsItem.Link` (unique index) and stores top 5 items per feed.
- `NotificationCheckService` (`PCH.Api/Services`) is a `BackgroundService`, not an `IConnector` — it polls every 5 minutes for due-today tasks and a once-daily news summary, each gated by a "notified" flag/date so repeats don't re-fire.

### Data model notes
- Table names are plural snake_case (`tasks`, `news_items`, `bookings`, `emails`, `rss_feeds`, `app_settings`) — set explicitly in `PchDbContext.OnModelCreating`, not inferred.
- `AppSettings` is a deliberate single-row table (`Id` always 1) for IMAP config + notification prefs, edited from `SettingsPage.razor`.
- SQLite can't reliably `ORDER BY`/filter on `DateTimeOffset` columns — existing code materializes to a list first, then filters/sorts in memory (see `NotificationCheckService.CheckDeadlinesAsync`). Follow this pattern for new date-based queries; see the `sqlite-gotchas` skill before writing EF queries involving dates.
- `TaskItem.Status` auto-transitions to `Done` when `Progress` hits 100 (`ApplyProgressStatusSync`, referenced from `TaskItemMappingExtensions`/DTO mapping).

## Coding Rules
- Async/await for all IO.
- DI everywhere (`IServiceCollection`), one connector per class implementing `IConnector`.
- Records for DTOs (`PCH.Core/Dtos`), classes for EF entities (`PCH.Core/Models`).
- XML doc comments on public methods (existing code is consistent about this — match it).
- Naming: PascalCase classes/methods, camelCase fields/locals, `IPascalCase` interfaces.

## Sub-Agents and Skills
`.claude/agents/`: `architect` (plan/design) → `backend` (.NET API/EF Core) → `frontend` (Blazor UI), with `connector` for email/RSS/booking work and `reviewer` for pre-completion checks. Follow this ordering for non-trivial features.

`.claude/skills/` — load before touching the matching area: `dotnet-patterns` (controllers/services/DTOs/EF/Program.cs), `blazor-components` (`.razor` pages, typed API clients, `PCH.App/Program.cs`), `sqlite-gotchas` (EF+SQLite date/migration pitfalls), `windows-notifications` (Toast notification code).
