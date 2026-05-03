$logPath = Join-Path $env:USERPROFILE "AppData\LocalLow\It's Happening\PlateUp\Player.log"

if (-not (Test-Path $logPath)) {
    throw "Player.log not found at $logPath"
}

Write-Host "Tailing $logPath"
Get-Content -Path $logPath -Wait -Tail 80
