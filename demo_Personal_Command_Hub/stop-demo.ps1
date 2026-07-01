# stop-demo.ps1
# Stops PCH.Api, PCH.App, and Tailscale Funnel completely -- including
# closing the PowerShell windows they were launched in, not just the
# dotnet.exe processes inside them.
# Run this from the repo root: .\stop-demo.ps1

$root = $PSScriptRoot
$pidFile = Join-Path $root ".demo-pids.txt"

Write-Host "Stopping Tailscale Funnel..."
tailscale funnel off

function Stop-ProcessOnPort {
    param([int]$Port, [string]$Name)

    $conn = netstat -ano | Select-String ":$Port\s" | Select-Object -First 1
    if ($conn) {
        $procId = ($conn -split '\s+')[-1]
        if ($procId -match '^\d+$') {
            Write-Host "Stopping $Name (PID $procId, port $Port)..."
            taskkill /PID $procId /F | Out-Null
        }
    } else {
        Write-Host "$Name not running on port $Port (nothing to stop)."
    }
}

# Kill the dotnet.exe processes by port (stops the actual servers)
Stop-ProcessOnPort -Port 5290 -Name "PCH.App"
Stop-ProcessOnPort -Port 5292 -Name "PCH.Api"

# Close the wrapping PowerShell windows that start-demo.ps1 opened
if (Test-Path $pidFile) {
    Write-Host "Closing demo PowerShell windows..."
    Get-Content $pidFile | ForEach-Object {
        $windowPid = $_.Trim()
        if ($windowPid -match '^\d+$') {
            $proc = Get-Process -Id $windowPid -ErrorAction SilentlyContinue
            if ($proc) {
                Write-Host "Closing window (PID $windowPid)..."
                taskkill /PID $windowPid /F /T | Out-Null
            }
        }
    }
    Remove-Item $pidFile
} else {
    Write-Host "No .demo-pids.txt found -- windows opened by start-demo.ps1"
    Write-Host "may need to be closed manually this time."
}

Write-Host ""
Write-Host "Demo stopped. Funnel disabled, all processes and windows closed."
Write-Host "Your real local-only setup is back to normal."
