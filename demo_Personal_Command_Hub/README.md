Personal Command Hub (PCH)

A local personal dashboard that runs on your own machine — no cloud account, no subscription, no third party storing your data. It reads your email, your news, and tracks your tasks, then uses a locally-running AI model to summarize and classify what matters.

Built as a 5-day course project, running on a Beelink mini PC.


What it does


Tasks — create, track progress, set deadlines and categories, auto-completes at 100%
Email — reads your Outlook inbox (IMAP), filters for important messages (bookings, deadlines, meetings), and uses a local AI model to classify and summarize them. Matching emails automatically become tasks.
News — pulls headlines from SVT (Sweden), Tagesschau (Germany), and ScienceDaily, refreshed on demand
Notifications — Windows desktop toast notifications for new tasks, deadlines due today, and a daily news summary
Login — single-user cookie authentication, rate-limited, with security headers and a locked-down API


Everything runs locally. The only thing that leaves your machine is the IMAP connection to your email provider — there are no calls to any cloud AI service.


Tech stack

LayerTechnologyBackend APIASP.NET Core 8 Web APIFrontendBlazor Web App (Server interactivity)DatabaseSQLite + Entity Framework CoreEmailMailKit (IMAP)NewsRSS via System.ServiceModel.SyndicationAIGPT4All running locally (Llama 3.2 1B Instruct), exposed via its OpenAI-compatible local API serverNotificationsCommunityToolkit.WinUI.Notifications (Windows Toast)AuthCookie auth + BCrypt password hashing


Project structure

PCH/
├── PCH.App/           # Blazor dashboard (frontend) — Tasks, Emails, News, Login
├── PCH.Api/            # Web API — controllers, background services, auth
├── PCH.Core/           # Shared models, DTOs, interfaces
├── PCH.Data/            # EF Core DbContext + migrations
├── PCH.Connectors/      # Email (IMAP), RSS, and local LLM client
├── PCH.Notifications/    # Windows Toast notification service
├── PCH.Tests/             # xUnit tests
├── docs/
│   ├── SETUP.md           # Full install/setup guide
│   └── BUILD_PLAN.md       # Day-by-day build plan and progress checklist
└── .claude/
    ├── agents/             # Specialized Claude Code agent instructions
    └── skills/             # Reusable conventions and known gotchas


Running it locally


Install the .NET 8 SDK
Install GPT4All, download a model (Llama 3.2 1B Instruct or similar), and enable its local API server in Settings → Application (port 4891 by default)
Set your email credentials via user-secrets (never commit these to Git):


powershell   cd PCH.Api
   dotnet user-secrets init
   dotnet user-secrets set "Email:Username" "your-email@outlook.com"
   dotnet user-secrets set "Email:Password" "<app password, not your normal password>"


Set your login password hash (see docs/SETUP.md for the full walkthrough)
Apply the database migrations:


powershell   dotnet ef database update --project PCH.Data --startup-project PCH.Api


Run both projects (in separate terminals):


powershell   dotnet run --project PCH.Api
   dotnet run --project PCH.App


Open the Blazor app in your browser and log in

For a public demo via Tailscale Funnel, use the included scripts from the repo root:


powershell   .\start-demo.ps1   # launches Api, App, and Tailscale Funnel in separate windows
   .\stop-demo.ps1    # kills all three cleanly


Full setup instructions, including exact downloads and commands, are in docs/SETUP.md.


Project status

ModuleStatusTasks✅ DoneEmail connector (Outlook)✅ DoneNews connector✅ DoneLocal AI classify/summarize✅ DoneNotifications✅ DoneAuth & security hardening✅ DoneSettings page UI✅ Done — dark/light mode toggle (cookie-persisted), IMAP email config, RSS feed list (add/toggle/delete), notification preferencesBooking connector (fitness classes)⏸ Paused — needs a sample confirmation emailSchool timetable connector (WebUntis)⏸ Paused — no live data until school resumesBackground auto-sync🔲 Not startedDeployment / auto-start on boot🔲 Not started

See docs/BUILD_PLAN.md for the detailed day-by-day breakdown.


Notes on local AI

PCH uses GPT4All running entirely on your own machine, not a cloud API. This means:


No API costs, no rate limits, no internet dependency for the AI step
Slightly less capable than large cloud models, which is fine for short classification and summarization tasks
GPT4All's desktop app must be running (with its API server enabled) whenever email sync happens



License / personal project notice

This is a personal coursework project, not intended for production or public distribution. Email credentials, the SQLite database, and any user-secrets are excluded from version control via .gitignore.V