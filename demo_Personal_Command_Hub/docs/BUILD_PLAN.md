# PCH — 5-Day Build Plan

Use this file to track daily goals. Check off tasks as you complete them.
Ask Claude Code to "continue from the build plan" each morning.

---

## Day 1 — Core Setup ✅ Start here
**Goal:** Working solution with dashboard shell and task CRUD

- [ ] Create solution: `dotnet new sln -n PCH`
- [ ] Create projects: App, Api, Core, Data
- [ ] Link projects to solution
- [ ] Install EF Core + SQLite
- [ ] Create `AppDbContext` with `Tasks` table
- [ ] Run first migration
- [ ] Create `TasksController` with GET / POST / PUT / DELETE
- [ ] Create Blazor dashboard shell with sidebar nav
- [ ] Create `TasksPage.razor` — list + add task form
- [ ] Test: add a task, see it in the list

**Agent to use:** Architect → Backend → Frontend

---

## Day 2 — Progress & Categories
**Goal:** Tasks have progress, deadlines, and categories

- [x] Add `Progress` (0–100), `Category`, `Deadline` to Task entity — already present in entity + InitialCreate migration
- [x] New migration — not needed; columns already in `InitialCreate` (verified via model snapshot)
- [x] Update TasksController — created full CRUD `TasksController` (GET/GET{id}/POST/PUT/DELETE) + DTOs; wired `AddControllers`/`MapControllers`
- [x] Add progress bar component in Blazor — `Components/Shared/ProgressBar.razor`
- [x] Add category filter/tabs — category tabs on `TasksPage.razor`
- [x] Add deadline highlight (red if overdue) — overdue rows show red due-date + "Overdue" badge
- [x] Auto-mark task complete when progress = 100 — `ApplyProgressStatusSync` (verified at runtime: 100 → Done)

**Agent to use:** Backend → Frontend

---

## Day 3 — Email Connector
**Goal:** App reads your inbox and creates tasks from important emails

- [ ] Install MailKit: `dotnet add package MailKit`
- [ ] Create `EmailConnectorSettings` in appsettings.json
- [ ] Implement `EmailConnector` class (IMAP login, fetch last 20 emails)
- [ ] Extract: subject, sender, date, keywords
- [ ] If keyword detected → auto-create Task
- [ ] Store raw emails in DB (Emails table)
- [ ] Create `EmailsPage.razor` — show inbox list
- [ ] Add background sync (every 15 min)

**Agent to use:** Architect → Connector → Backend → Frontend

---

## Day 4 — Booking Connector
**Goal:** App detects bookings and converts them to tasks

- [ ] Decide: parse booking emails OR connect to platform API
- [ ] Implement `BookingConnector`
- [ ] Extract: booking ID, dates, location, platform
- [ ] Create Booking entity + migration
- [ ] Convert bookings → Tasks automatically
- [ ] Create `BookingsPage.razor`
- [ ] Show upcoming bookings sorted by date

**Agent to use:** Connector → Backend → Frontend

---

## Day 5 — News + Notifications + Polish
**Goal:** Daily news feed + desktop notifications + ready to deploy

- [x] Install RSS package (built-in SyndicationFeed) — `System.ServiceModel.Syndication` already in PCH.Connectors.csproj
- [x] Create `RssConnector` with feeds (Sweden/SVT, Germany/Tagesschau, Science/ScienceDaily) — top 5 per feed, dedup by URL, HTML stripped from summaries
- [x] Store top 5 items per feed in DB — `news_items` table (InitialCreate), verified 15 items inserted at runtime
- [x] Create `NewsPage.razor` with category tabs — All / Sweden / Germany / Science, card grid with live links
- [ ] Add Windows Toast Notifications (WinRT)
- [ ] Trigger notifications for: new task, deadline today, daily news summary
- [ ] Add `NotificationsPage.razor` — notification history
- [ ] Create `SettingsPage.razor` — email config, feeds toggle, notification prefs
- [ ] Final test: full flow end to end
- [ ] Deploy locally on Beelink (set app to start with Windows)

**Agent to use:** Connector → Backend → Frontend → Reviewer

---

## Daily Prompt Template
Use this to start each Claude Code session:

```
I'm building the Personal Command Hub (.NET 8 + Blazor Hybrid + SQLite).
Today is Day [X]. 

Current state: [describe what's already done]
Today's goal: [paste relevant day section above]

Start with the Architect agent to plan, then Backend, then Frontend.
Follow the rules in CLAUDE.md.
```
