# stop-demo.ps1
# Stops PCH.Api, PCH.App, and Tailscale Funnel by finding and killing
# whatever is listening on their known ports, then turns Funnel off cleanly.
# Run this from the repo root: .\stop-demo.ps1

Write-Host "Stopping Tailscale Funnel..."
tailscale funnel off

function Stop-ProcessOnPort {
    param([int]$Port, [string]$Name)

    $conn = netstat -ano | Select-String ":$Port\s" | Select-Object -First 1
    if ($conn) {
        $pidValue = ($conn -split '\s+')[-1]
        if ($pidValue -match '^\d+$') {
            Write-Host "Stopping $Name (PID $pidValue, port $Port)..."
            taskkill /PID $pidValue /F | Out-Null
        }
    } else {
        Write-Host "$Name not running on port $Port (nothing to stop)."
    }
}

Stop-ProcessOnPort -Port 5290 -Name "PCH.App"
Stop-ProcessOnPort -Port 5292 -Name "PCH.Api"

Write-Host ""
Write-Host "Demo stopped. Funnel disabled, App and Api processes killed."
Write-Host "Your real local-only setup is back to normal."
