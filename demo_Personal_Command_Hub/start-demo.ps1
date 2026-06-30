# start-demo.ps1
# Launches PCH.Api, PCH.App, and Tailscale Funnel each in their own window.
# Run this from the repo root: .\start-demo.ps1

$root = $PSScriptRoot
$dotnet = "C:\Program Files\dotnet\dotnet.exe"

Write-Host "Starting PCH.Api..."
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root\PCH.Api'; & '$dotnet' watch run"

Start-Sleep -Seconds 5

Write-Host "Starting PCH.App..."
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root\PCH.App'; & '$dotnet' watch run"

Start-Sleep -Seconds 8

Write-Host "Starting Tailscale Funnel on port 5290..."
Start-Process powershell -ArgumentList "-NoExit", "-Command", "tailscale funnel 5290"

Write-Host ""
Write-Host "All three windows launched. Wait ~10 seconds, then check:"
Write-Host "  https://predator.tailadce4.ts.net/"
Write-Host ""
Write-Host "To stop everything: close all three PowerShell windows, or Ctrl+C each."
