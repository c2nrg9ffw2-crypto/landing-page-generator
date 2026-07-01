# start-demo.ps1
# Launches PCH.Api, PCH.App, and Tailscale Funnel each in their own window.
# Uses plain "dotnet run" (NOT "dotnet watch") because dotnet watch rebinds
# the port on every file change, which breaks the Tailscale Funnel tunnel.
# For a stable public demo link, the port must stay bound the whole time.
# Run this from the repo root: .\start-demo.ps1

$root = $PSScriptRoot
$dotnet = "C:\Program Files\dotnet\dotnet.exe"
$pidFile = Join-Path $root ".demo-pids.txt"

# Clear any old PID file from a previous run
if (Test-Path $pidFile) { Remove-Item $pidFile }

Write-Host "Starting PCH.Api..."
$apiProcess = Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root\PCH.Api'; & '$dotnet' run" -PassThru
"$($apiProcess.Id)" | Out-File -Append $pidFile

Start-Sleep -Seconds 5

Write-Host "Starting PCH.App..."
$appProcess = Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root\PCH.App'; & '$dotnet' run" -PassThru
"$($appProcess.Id)" | Out-File -Append $pidFile

Start-Sleep -Seconds 8

Write-Host "Starting Tailscale Funnel on port 5290..."
$funnelProcess = Start-Process powershell -ArgumentList "-NoExit", "-Command", "tailscale funnel 5290" -PassThru
"$($funnelProcess.Id)" | Out-File -Append $pidFile

Write-Host ""
Write-Host "All three windows launched. Wait ~10 seconds, then check:"
Write-Host "  https://predator.tailadce4.ts.net/"
Write-Host ""
Write-Host "NOTE: This uses plain 'dotnet run', not 'dotnet watch'."
Write-Host "If you edit code during the demo, you'll need to manually stop"
Write-Host "and restart that one window -- auto-reload would break the Funnel link."
Write-Host ""
Write-Host "To stop everything cleanly: .\stop-demo.ps1"
