---
name: windows-notifications
description: Use when implementing, debugging, or extending Windows Toast notifications in PCH.Notifications or any code that calls ToastContentBuilder / CommunityToolkit.WinUI.Notifications. Load this before writing notification code, and especially when a toast silently fails to appear despite no exceptions.
---

# Windows Toast Notifications — PCH

PCH.Api is an **unpackaged .NET console/web app** (not an MSIX-packaged
desktop app), which is the main source of friction with Windows Toast
notifications. This skill captures the gotchas discovered building Day 5's
notification feature.

## Package
Use `CommunityToolkit.WinUI.Notifications` (successor to the older
`Microsoft.Toolkit.Uwp.Notifications`, which is deprecated). Add via:

```bash
dotnet add PCH.Notifications package CommunityToolkit.WinUI.Notifications
```

## The #1 gotcha: AUMID (Application User Model ID)

Toast notifications are tied to an AUMID so Windows knows which app icon,
name, and notification settings to use. Packaged (MSIX) apps get one
automatically. **Unpackaged apps do not** — and a missing/incorrect AUMID is
the single most common reason a toast silently does nothing: no exception,
no error, just nothing appears on screen.

### Symptom
`ToastContentBuilder.Show()` runs, no exception is thrown, but no
notification appears in the Windows Action Center or as a popup.

### Fix: register an AUMID via the registry on first run

```csharp
// Run once at app startup, before any toast is shown
using Microsoft.Win32;

const string aumid = "Viktor.PCH.Dashboard";
const string displayName = "Personal Command Hub";

using var key = Registry.CurrentUser.CreateSubKey(
    $@"SOFTWARE\Classes\AppUserModelId\{aumid}");
key.SetValue("DisplayName", displayName);
key.SetValue("IconUri", ""); // optional: path to an .ico if you have one
```

Then pass this AUMID explicitly when building toasts:

```csharp
new ToastContentBuilder()
    .AddText("New task created")
    .AddText(task.Title)
    .Show(toast => toast.Group = "pch", customize: t => { });
// AUMID is picked up automatically from the registered identity if using
// the DesktopNotificationManagerCompat helper from the same package —
// always initialize that helper at startup:
DesktopNotificationManagerCompat.RegisterAumidAndComServer<NotificationActivator>(aumid);
```

## Verify, don't trust a clean build

A clean compile and a successful `Show()` call with no exception **does
not mean the toast appeared**. Always verify by physically looking at the
screen / Action Center after triggering a test notification. This mirrors
the SQLite ordering and positional-record gotchas from earlier days —
compiling is not the same as working.

## Background service considerations

- `IHostedService`/`BackgroundService` running inside `PCH.Api` can show
  toasts only if the process has access to the interactive desktop session
  (i.e. running via `dotnet run` or as a console app in the logged-in
  user's session — **not** running as a Windows Service under
  LocalSystem, which has no desktop session and toasts will silently fail).
- If you later want PCH.Api to run as an actual Windows Service for
  auto-start, toast notifications will need to move to a separate small
  process that does run in the user session, or use an alternative
  (email/in-app notification) instead.

## Quick troubleshooting checklist
1. Is the AUMID registered in the registry under the current user?
2. Is the process running in an interactive desktop session (not a
   service/LocalSystem context)?
3. Are Windows notification settings for the app/AUMID enabled
   (Settings → System → Notifications)?
4. Did you call `Show()` on the main/UI-capable thread context, not a
   background thread with no message pump issues? (Less common in .NET
   but worth checking if toasts work sometimes and not others.)
5. Test with the absolute minimal toast first (`AddText` only) before
   adding buttons, images, or deep links — isolate the failure.
