# PCH — Setup Guide (Downloads & Configuration)

Follow this before Day 1. Takes about 20–30 minutes.

---

## 1. Downloads & Installs

### Required
| Tool | Download | Notes |
|------|----------|-------|
| **VS Code** | https://code.visualstudio.com | Pick "System Installer" for Windows |
| **.NET 8 SDK** | https://dotnet.microsoft.com/download/dotnet/8.0 | Pick "SDK x64" for Windows |
| **Git** | https://git-scm.com/download/win | For version control |
| **Claude Code extension** | VS Code Extensions panel → search "Claude Code" | Official Anthropic extension |

### Optional but recommended
| Tool | Download | Notes |
|------|----------|-------|
| **DB Browser for SQLite** | https://sqlitebrowser.org | View your database visually |
| **Postman** | https://www.postman.com/downloads | Test your API endpoints |

---

## 2. VS Code Extensions to Install
Open VS Code → press `Ctrl+Shift+X` → search and install each:

- `Claude Code` (by Anthropic) — your AI coding agent
- `C# Dev Kit` (by Microsoft) — .NET support
- `SQLite Viewer` (by qwtel) — view .db files inside VS Code
- `REST Client` (by Huachao Mao) — test APIs without Postman

---

## 3. Verify Installs
Open a terminal in VS Code (`Ctrl+\``) and run:

```bash
dotnet --version      # should show 8.x.x
git --version         # should show git version 2.x
```

---

## 4. Set Up Claude Code
1. Click the **spark icon ⚡** in the VS Code sidebar
2. Sign in with your **Anthropic account** (claude.ai account works)
3. You're ready — Claude Code runs directly in your IDE

---

## 5. Set Up Sub-Agents
The `.claude/agents/` folder is already set up in this project.
VS Code and Claude Code will automatically discover the agents.

To use an agent:
- Type `/agents` in the Claude Code chat panel
- Or say: `"Use the Architect agent to design the Task module"`

---

## 6. Create the Project (Day 1 commands)

Open terminal in VS Code and run in order:

```bash
# Create solution
dotnet new sln -n PCH

# Create projects
dotnet new blazormaui -n PCH.App -o PCH.App
dotnet new webapi -n PCH.Api -o PCH.Api
dotnet new classlib -n PCH.Core -o PCH.Core
dotnet new classlib -n PCH.Data -o PCH.Data
dotnet new classlib -n PCH.Connectors -o PCH.Connectors

# Add projects to solution
dotnet sln add PCH.App/PCH.App.csproj
dotnet sln add PCH.Api/PCH.Api.csproj
dotnet sln add PCH.Core/PCH.Core.csproj
dotnet sln add PCH.Data/PCH.Data.csproj
dotnet sln add PCH.Connectors/PCH.Connectors.csproj

# Add NuGet packages
dotnet add PCH.Data package Microsoft.EntityFrameworkCore.Sqlite
dotnet add PCH.Data package Microsoft.EntityFrameworkCore.Tools
dotnet add PCH.Api package Microsoft.EntityFrameworkCore.Design
dotnet add PCH.Connectors package MailKit

# EF Core tools (global)
dotnet tool install --global dotnet-ef
```

---

## 7. appsettings.json Template
Add this to `PCH.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=pch.db"
  },
  "Email": {
    "Host": "imap.gmail.com",
    "Port": 993,
    "Username": "",
    "Password": ""
  },
  "News": {
    "Feeds": [
      { "Name": "World", "Url": "http://feeds.bbci.co.uk/news/rss.xml" },
      { "Name": "Tech", "Url": "https://feeds.arstechnica.com/arstechnica/index" },
      { "Name": "Sweden", "Url": "https://www.svt.se/nyheter/rss.xml" },
      { "Name": "Gaming", "Url": "https://www.eurogamer.net/feed" },
      { "Name": "Science", "Url": "https://www.sciencedaily.com/rss/top/science.xml" }
    ]
  }
}
```

> ⚠️ **Never commit passwords to Git.** Use `dotnet user-secrets` for real credentials.

---

## 8. Daily Workflow with Claude Code
1. Open VS Code in the PCH folder
2. Open Claude Code panel (⚡ icon)
3. Paste the daily prompt from `docs/BUILD_PLAN.md`
4. Let Claude Code + agents do the work
5. Review, test, check off tasks in BUILD_PLAN.md

---

## You're ready. Start Day 1! 🚀
